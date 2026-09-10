using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOperPermission : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        #endregion

        #region Constructor

        public frmOperPermission()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmOperPermission_Load(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDate.SelectedDate = DateTime.Today;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select username from users where emp={MainClass.UserID}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    txtUser.Text = dt.Rows[0][0].ToString();
                    txtPass.Clear();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtPass.Password))
                {
                    DXMessageBox.Show("ادخل كلمة المرور", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPass.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    DateTime selectedDate = txtDate.SelectedDate ?? DateTime.Today;
                    string   password     = txtPass.Password;

                    if (ckshiftClose.IsChecked == true)
                        SavePermission(transaction, 1, ckshiftClose.Content.ToString(), password, selectedDate);

                    if (ckReturn.IsChecked == true)
                        SavePermission(transaction, 2, ckReturn.Content.ToString(), password, selectedDate);

                    if (ckHosting.IsChecked == true)
                        SavePermission(transaction, 3, ckHosting.Content.ToString(), password, selectedDate);

                    if (ckDiscount.IsChecked == true)
                        SavePermission(transaction, 4, ckDiscount.Content.ToString(), password, selectedDate);

                    if (ckAbsent.IsChecked == true)
                        SavePermission(transaction, 5, ckAbsent.Content.ToString(), password, selectedDate);

                    transaction.Commit();
                    DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtPass.Clear();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void SavePermission(SqlTransaction transaction, int operNo, string operName,
            string password, DateTime date)
        {
            new SqlCommand($"delete from OperationPermission where OperNo={operNo}", conn, transaction).ExecuteNonQuery();

            SqlCommand cmd = new SqlCommand(
                "insert into OperationPermission(emp,OperName,OperNo,pwd,lastChanged,IS_Deleted) values(@emp,@OperName,@OperNo,@pwd,@lastChanged,@IS_Deleted)",
                conn, transaction);
            cmd.Parameters.Add("@emp",         SqlDbType.Int).Value       = MainClass.UserID;
            cmd.Parameters.Add("@OperName",    SqlDbType.NVarChar).Value  = operName;
            cmd.Parameters.Add("@OperNo",      SqlDbType.Int).Value       = operNo;
            cmd.Parameters.Add("@pwd",         SqlDbType.NVarChar).Value  = password;
            cmd.Parameters.Add("@lastChanged", SqlDbType.DateTime).Value  = date;
            cmd.Parameters.Add("@IS_Deleted",  SqlDbType.Bit).Value       = 0;
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Close & Helpers

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ أثناء الحفظ" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}