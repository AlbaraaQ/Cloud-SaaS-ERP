using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmStagePM : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code;
        public bool IsDone { get; private set; }

        private ObservableCollection<StageItem> _stages;

        #endregion

        #region Constructor

        public frmStagePM()
        {
            InitializeComponent();
            conn    = MainClass.ConnObj();
            Code    = -1;
            IsDone  = false;
            _stages = new ObservableCollection<StageItem>();
            dgvdata.ItemsSource = _stages;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDG();
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtName.Text   = "";
            txtNameEn.Text = "";
            Code = -1;
        }

        #endregion

        #region Data Loading

        private void LoadDG()
        {
            try
            {
                _stages.Clear();
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM PM_Stages WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                foreach (DataRow row in dt.Rows)
                {
                    _stages.Add(new StageItem
                    {
                        Id     = Convert.ToInt32(row["id"]),
                        Name   = row["name"].ToString(),
                        NameEn = row["nameEn"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DXMessageBox.Show("ادخل اسم المرحلة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            try
            {
                EnsureOpen(conn);

                if (Code == -1)
                {
                    // التحقق من التكرار
                    var chkAdapter = new SqlDataAdapter(
                        $"SELECT id FROM PM_Stages WHERE name='{txtName.Text}' AND nameEn='{txtNameEn.Text}' AND IS_Deleted=0",
                        conn);
                    var chkDt = new DataTable();
                    chkAdapter.Fill(chkDt);

                    if (chkDt.Rows.Count > 0)
                    {
                        DXMessageBox.Show("المرحلة تم إدخالها مسبقاً", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtName.Focus();
                        return;
                    }

                    // إدراج
                    new SqlCommand(
                        $"INSERT INTO PM_Stages(name,nameEn,IS_Deleted) VALUES('{txtName.Text}','{txtNameEn.Text}',0)",
                        conn).ExecuteNonQuery();
                    txtName.Text = "";
                    txtName.Focus();
                }
                else
                {
                    // تحديث
                    new SqlCommand(
                        $"UPDATE PM_Stages SET name='{txtName.Text}', nameEn='{txtNameEn.Text}' WHERE id={Code}",
                        conn).ExecuteNonQuery();
                    txtName.Focus();
                }

                IsDone = true;
                LoadDG();

                var savedMsg = new frmSavedMsg();
                if (Code != -1)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1) CLR();
                else if (savedMsg.Pressed == 3) this.Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر مرحلة ليتم حذفها", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = DXMessageBox.Show("هل تريد حذف هذه المرحلة؟", "تأكيد الحذف",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                EnsureOpen(conn);
                new SqlCommand($"DELETE FROM PM_Stages WHERE id={Code}", conn).ExecuteNonQuery();
                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDG();
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region DataGrid Events

        private void dgvdata_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvdata.SelectedItem is StageItem item)
            {
                txtName.Text   = item.Name;
                txtNameEn.Text = item.NameEn;
                Code           = item.Id;
            }
        }

        #endregion

        #region TextBox Events

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                btnSave_Click(null, null);
            }
        }

        private void txtNameEn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                btnSave_Click(null, null);
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }

    /// <summary>نموذج صف جدول المراحل</summary>
    public class StageItem
    {
        public int    Id     { get; set; }
        public string Name   { get; set; }
        public string NameEn { get; set; }
    }
}