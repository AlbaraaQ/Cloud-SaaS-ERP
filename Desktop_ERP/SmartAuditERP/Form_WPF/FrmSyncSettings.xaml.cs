using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmSyncSettings : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        #endregion

        #region Constructor

        public FrmSyncSettings()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        #endregion

        #region Load Data

        private void LoadData()
        {
            try
            {
                var adapter = new SqlDataAdapter("SELECT * FROM SettingSync", conn);
                var table   = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 1)
                    return;

                var row = table.Rows[0];

                txtClientCode.Text = row["ClientCode"].ToString();
                txtAPIUrl.Text     = row["APIUrl"].ToString();
                txtUser.Text       = row["Username"].ToString();
                txtPassword.Text   = row["Password"].ToString();

                // Branch Type
                if (row["BranchType"] != DBNull.Value)
                {
                    if (double.TryParse(row["BranchType"].ToString(), out double branchType))
                    {
                        if (branchType == 1.0)
                            rbDataCenter.IsChecked = true;
                        else
                            rbSecondaryBranch.IsChecked = true;
                    }
                }

                // Sync Type
                if (row["SyncType"] != DBNull.Value)
                {
                    if (double.TryParse(row["SyncType"].ToString(), out double syncType))
                    {
                        if      (syncType == 1.0) rbAutoSync.IsChecked     = true;
                        else if (syncType == 2.0) rbPeriodicSync.IsChecked = true;
                        else if (syncType == 3.0) rbEndDay.IsChecked       = true;
                        else if (syncType == 4.0) rbEndShift.IsChecked     = true;
                    }
                }
            }
            catch
            {
                // تجاهل أخطاء التحميل الأولي بصمت
            }
        }

        #endregion

        #region Save

        private void btnSyncSave_Click(object sender, RoutedEventArgs e)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            SqlTransaction transaction = conn.BeginTransaction();
            try
            {
                if (MainClass.EmpNo > 1)
                {
                    DXMessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = DXMessageBox.Show(
                    "هل أنت متأكد من حفظ الإعدادات؟\nينصح بالتواصل مع الدعم الفني قبل إجراء أي تعديل.",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.No)
                    return;

                // تحديد نوع الفرع
                int branchType = rbSecondaryBranch.IsChecked == true ? 2 : 1;

                // تحديد نوع المزامنة
                int syncType = 1;
                if      (rbAutoSync.IsChecked     == true) syncType = 1;
                else if (rbPeriodicSync.IsChecked == true) syncType = 2;
                else if (rbEndDay.IsChecked       == true) syncType = 3;
                else if (rbEndShift.IsChecked     == true) syncType = 4;

                // حذف القديم
                new SqlCommand("DELETE FROM SettingSync", conn, transaction)
                    .ExecuteNonQuery();

                // إدراج الجديد
                var insertCmd = new SqlCommand(
                    "INSERT INTO SettingSync(Id,ClientCode,APIUrl,UserName,Password," +
                    "BranchId,BranchType,SyncType,EmpId) " +
                    "VALUES(@Id,@ClientCode,@APIUrl,@UserName,@Password," +
                    "@BranchId,@BranchType,@SyncType,@EmpId)",
                    conn, transaction);

                insertCmd.Parameters.Add("@Id",          SqlDbType.Int).Value       = 1;
                insertCmd.Parameters.Add("@ClientCode",  SqlDbType.NVarChar).Value  = txtClientCode.Text;
                insertCmd.Parameters.Add("@APIUrl",      SqlDbType.NVarChar).Value  = txtAPIUrl.Text;
                insertCmd.Parameters.Add("@UserName",    SqlDbType.NVarChar).Value  = txtUser.Text;
                insertCmd.Parameters.Add("@Password",    SqlDbType.NVarChar).Value  = txtPassword.Text;
                insertCmd.Parameters.Add("@BranchId",    SqlDbType.Int).Value       = MainClass.BranchNo;
                insertCmd.Parameters.Add("@BranchType",  SqlDbType.Int).Value       = branchType;
                insertCmd.Parameters.Add("@SyncType",    SqlDbType.Int).Value       = syncType;
                insertCmd.Parameters.Add("@EmpId",       SqlDbType.Int).Value       = MainClass.EmpNo;
                insertCmd.ExecuteNonQuery();

                transaction.Commit();
                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion
    }
}