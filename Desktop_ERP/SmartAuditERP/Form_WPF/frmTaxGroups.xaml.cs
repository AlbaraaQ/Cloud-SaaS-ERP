using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTaxGroups : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code = -1;
        private ObservableCollection<TaxGroupItemModel> itemsList;

        #endregion

        #region Constructor

        public frmTaxGroups()
        {
            InitializeComponent();
            conn      = MainClass.ConnObj();
            itemsList = new ObservableCollection<TaxGroupItemModel>();
            dgvData.ItemsSource = itemsList;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllCurrency();
            this.WindowState = MainClass.Window_State;
        }

        #endregion

        #region Clear / New

        private void ClearFields()
        {
            GetNextId();
            txtNameAr.Text = "";
            txtNameEN.Text = "";
            txtValue.Text  = "";
            cmbCurrency.SelectedIndex = -1;
            itemsList.Clear();
            Code = -1;
        }

        private void GetNextId()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT MAX(id) FROM tax_groups", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0 && table.Rows[0][0] != DBNull.Value)
                {
                    if (double.TryParse(table.Rows[0][0].ToString(), out double maxId))
                        txtNo.Text = ((int)(maxId + 1)).ToString();
                    else
                        txtNo.Text = "1";
                }
                else
                {
                    txtNo.Text = "1";
                }
            }
            catch
            {
                txtNo.Text = "1";
            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        #endregion

        #region Load Items Grid

        private void LoadItemsGrid(int groupId)
        {
            try
            {
                itemsList.Clear();
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name, nameEN FROM Items " +
                    $"WHERE is_deleted=0 AND tax_group={groupId}", conn);
                var table = new DataTable();
                adapter.Fill(table);

                int rowNo = 1;
                foreach (DataRow row in table.Rows)
                {
                    string displayName = string.Equals(MainClass.Language, "ar")
                        ? row["name"].ToString()
                        : row["nameEN"].ToString();

                    itemsList.Add(new TaxGroupItemModel
                    {
                        RowNo    = rowNo++,
                        ItemId   = row["id"].ToString(),
                        ItemName = displayName
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الأصناف\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Combo (Items)

        public void LoadAllCurrency()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Items WHERE IS_Deleted=0 ORDER BY id", conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbCurrency.DisplayMemberPath = "name";
                cmbCurrency.SelectedValuePath = "id";
                cmbCurrency.ItemsSource       = table.DefaultView;
                cmbCurrency.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الأصناف\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNo.Text))
                {
                    DXMessageBox.Show("ادخل الرقم", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNo.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtNameAr.Text))
                {
                    DXMessageBox.Show("ادخل الاسم", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNameAr.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtValue.Text))
                {
                    DXMessageBox.Show("ادخل القيمة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValue.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                double.TryParse(txtValue.Text, out double taxValue);
                int valueHaveTax = chkValueHaveTax.IsChecked == true ? 1 : 0;

                SqlCommand cmd;
                if (Code == -1)
                {
                    cmd = new SqlCommand(
                        "INSERT INTO tax_groups(id,nameAR,nameEN,Value,valueHaveTax) " +
                        "VALUES(@id,@nameAR,@nameEN,@Value,@valueHaveTax)", conn);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = int.Parse(txtNo.Text);
                }
                else
                {
                    cmd = new SqlCommand(
                        $"UPDATE tax_groups SET nameAR=@nameAR, nameEN=@nameEN, " +
                        $"Value=@Value, valueHaveTax=@valueHaveTax WHERE id={txtNo.Text}", conn);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = Code;
                }

                cmd.Parameters.Add("@nameAR",       SqlDbType.NVarChar).Value = txtNameAr.Text;
                cmd.Parameters.Add("@nameEN",       SqlDbType.NVarChar).Value = txtNameEN.Text;
                cmd.Parameters.Add("@Value",        SqlDbType.Float).Value    = taxValue;
                cmd.Parameters.Add("@valueHaveTax", SqlDbType.Bit).Value      = valueHaveTax;
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete Group

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر مجموعة ليتم حذفها", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"DELETE FROM tax_groups WHERE id={Code}", conn)
                    .ExecuteNonQuery();

                new SqlCommand(
                    $"UPDATE Items SET tax_group=1 WHERE tax_group={Code}", conn)
                    .ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete Row from Grid (زر الحذف داخل الجدول)

        private void dgvData_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn &&
                btn.Tag is TaxGroupItemModel item)
            {
                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    new SqlCommand(
                        $"UPDATE Items SET tax_group=1 WHERE id={item.ItemId}", conn)
                        .ExecuteNonQuery();

                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    itemsList.Remove(item);

                    // إعادة ترقيم
                    int rowNo = 1;
                    foreach (var row in itemsList)
                        row.RowNo = rowNo++;
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (conn.State != ConnectionState.Closed) conn.Close();
                }
            }
        }

        #endregion

        #region Add Item to Group

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCurrency.SelectedValue == null)
                {
                    DXMessageBox.Show("اختر الصنف", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCurrency.Focus();
                    return;
                }
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر المجموعة الضريبية أولاً", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"UPDATE Items SET tax_group={Code} WHERE id={cmbCurrency.SelectedValue}", conn)
                    .ExecuteNonQuery();

                itemsList.Add(new TaxGroupItemModel
                {
                    RowNo    = itemsList.Count + 1,
                    ItemId   = cmbCurrency.SelectedValue.ToString(),
                    ItemName = (cmbCurrency.SelectedItem as DataRowView)?["name"]?.ToString() ?? ""
                });

                cmbCurrency.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ\n{ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            try
            {
                var cmd = new SqlCommand(sqlQuery, conn);
                if (conn.State != ConnectionState.Open) conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    ReadData(reader);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التنقل\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (reader.HasRows)
            {
                reader.Read();
                ClearFields();

                if (int.TryParse(reader["id"].ToString(), out int parsedId))
                    Code = parsedId;

                txtNo.Text     = reader["id"].ToString();
                txtNameAr.Text = reader["nameAR"].ToString();
                txtNameEN.Text = reader["nameEN"].ToString();
                txtValue.Text  = reader["value"].ToString();

                chkValueHaveTax.IsChecked =
                    reader["valueHaveTax"] != DBNull.Value &&
                    Convert.ToBoolean(reader["valueHaveTax"]);

                reader.Close();
                if (conn.State != ConnectionState.Closed) conn.Close();

                LoadItemsGrid(Code);
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM tax_groups ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM tax_groups ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM tax_groups WHERE id>{Code} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM tax_groups WHERE id<{Code} ORDER BY id DESC");
        }

        #endregion

        #region Other Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة هنا
        }

        #endregion
    }

    #region Model

    public class TaxGroupItemModel : System.ComponentModel.INotifyPropertyChanged
    {
        private int _rowNo;
        public int RowNo
        {
            get => _rowNo;
            set { _rowNo = value; OnPropertyChanged(nameof(RowNo)); }
        }
        public string ItemId   { get; set; }
        public string ItemName { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    #endregion
}