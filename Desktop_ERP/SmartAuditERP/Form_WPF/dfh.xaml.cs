using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class dfh : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        /// <summary>
        /// مصدر بيانات الجدول
        /// </summary>
        private ObservableCollection<RakabaItem> _rakabaList;

        /// <summary>
        /// مصدر بيانات عمود الصنف (ComboBox)
        /// </summary>
        private DataTable _itemsSource;

        #endregion

        #region Constructor

        public dfh()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();

            _rakabaList = new ObservableCollection<RakabaItem>();
            dgvCurrencies.ItemsSource = _rakabaList;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadItemsSource();
                LoadDG();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل النافذة:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// تحميل مصدر بيانات عمود الصنف (ComboBox) - بديل frmRekabaSetting_Load
        /// </summary>
        private void LoadItemsSource()
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name as currency from Items order by id", conn);
                _itemsSource = new DataTable();
                adapter.Fill(_itemsSource);

                // تعيين مصدر البيانات لعمود ComboBox
                Column4.ItemsSource = _itemsSource.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الأصناف:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        /// <summary>
        /// تحميل بيانات جدول Rekaba - بديل LoadDG
        /// </summary>
        private void LoadDG()
        {
            try
            {
                _rakabaList.Clear();

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from Rekaba", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        _rakabaList.Add(new RakabaItem
                        {
                            ItemId = row["curr_id"] != DBNull.Value
                                ? Convert.ToInt32(row["curr_id"]) : 0,
                            IsControlled = row["chk"] != DBNull.Value
                                && Convert.ToInt32(row["chk"]) == 1
                        });
                    }

                    // تحميل القيمة من أول سجل
                    txtVal.Text = dt.Rows[0]["val"] != DBNull.Value
                        ? dt.Rows[0]["val"].ToString()
                        : "0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البيانات:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region TextBox Events

        private void txtVal_TextChanged(object sender, TextChangedEventArgs e)
        {
            // الكود الأصلي كان فارغاً
        }

        #endregion

        #region Button Events

        /// <summary>
        /// حفظ البيانات - بديل btnSave_Click
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من وجود أصناف
                int dataCount = 0;
                foreach (RakabaItem item in _rakabaList)
                {
                    if (item.ItemId > 0)
                        dataCount++;
                }

                if (dataCount == 0)
                {
                    MessageBox.Show("لا توجد أصناف مدخلة",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // التحقق من الأصناف المدخلة
                foreach (RakabaItem item in _rakabaList)
                {
                    if (item.ItemId == 0)
                    {
                        MessageBox.Show("ادخل الصنف",
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // التحقق من المبلغ
                if (!double.TryParse(txtVal.Text, out double amount) || amount == 0)
                {
                    MessageBox.Show("ادخل المبلغ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtVal.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                // حذف القديم وإدخال الجديد
                SqlCommand deleteCmd = new SqlCommand("delete from Rekaba", conn);
                deleteCmd.ExecuteNonQuery();

                foreach (RakabaItem item in _rakabaList)
                {
                    if (item.ItemId == 0) continue;

                    SqlCommand insertCmd = new SqlCommand(
                        "insert into Rekaba(curr_id, chk, val) values (@curr_id, @chk, @val)",
                        conn);

                    insertCmd.Parameters.Add("@curr_id", SqlDbType.Int).Value = item.ItemId;
                    insertCmd.Parameters.Add("@chk", SqlDbType.Bit).Value =
                        item.IsControlled ? 1 : 0;
                    insertCmd.Parameters.Add("@val", SqlDbType.Float).Value = amount;
                    insertCmd.ExecuteNonQuery();
                }

                MessageBox.Show("تم الحفظ",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        /// <summary>
        /// إغلاق النافذة - بديل btnClose_Click
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }

    #region Model

    /// <summary>
    /// نموذج بيانات صف الرقابة
    /// Column4 = ItemId (ComboBox)
    /// Column1 = IsControlled (CheckBox)
    /// </summary>
    public class RakabaItem : System.ComponentModel.INotifyPropertyChanged
    {
        private int _itemId;
        private bool _isControlled;

        /// <summary>Column4: معرّف الصنف</summary>
        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        /// <summary>Column1: خاضع للرقابة</summary>
        public bool IsControlled
        {
            get => _isControlled;
            set { _isControlled = value; OnPropertyChanged(nameof(IsControlled)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    #endregion
}