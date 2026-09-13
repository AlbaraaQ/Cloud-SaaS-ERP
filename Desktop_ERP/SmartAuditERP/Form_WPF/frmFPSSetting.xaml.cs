using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmFPSSetting : ThemedWindow
    {
        #region Fields

        public object bIsConnected;
        private SqlConnection conn;

        #endregion

        #region Constructor

        public frmFPSSetting()
        {
            bIsConnected = false;
            conn = MainClass.ConnObj();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmSetting_Load(object sender, RoutedEventArgs e)
        {
            Home Home = new Home();
            txtIP.Text = "192.168.8.135";
            txtPort.Text = "4370";
            LoadDeviceSetting();

            if (Home.lblConnectBar != null)
                lblState.Text = Home.lblConnectBar.Text;
        }

        #endregion

        #region Connect

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIP.Text) ||
                string.IsNullOrWhiteSpace(txtPort.Text))
            {
                DXMessageBox.Show("IP and Port cannot be null", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FingerPrintDevice.ConnectFPscanner(txtIP.Text.Trim(), txtPort.Text.Trim());

            if (FingerPrintDevice.ISconnectedBar)
            {
                bIsConnected = true;
                lblState.Text = "✔ متصل";
            }
            else
            {
                DXMessageBox.Show("جهاز البصمة غير متصل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Load Manual

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (FingerPrintDevice.ISconnectedBar)
            {
                FingerPrintDevice.ConnectFPscanner(
                    txtIP.Text.Trim(),
                    txtPort.Text.Trim());
            }
            else
            {
                DXMessageBox.Show("جهاز البصمة غير متصل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo > 1)
            {
                DXMessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPort.Text))
            {
                DXMessageBox.Show("يجب إدخال المنفذ", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIP.Text))
            {
                DXMessageBox.Show("يجب إدخال IP الجهاز", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EnsureConnectionOpen();
            SqlTransaction sqlTransaction = conn.BeginTransaction();

            try
            {
                int autoConnect = ckConnectAuto.IsChecked == true ? 1 : 0;

                new SqlCommand(
                    "delete from SettingFingurePrint where Branch_Id=" + MainClass.BranchNo,
                    conn, sqlTransaction).ExecuteNonQuery();

                new SqlCommand(
                    "insert into SettingFingurePrint(Branch_Id,ConnectAuto,DeviceIP,Port)" +
                    "values(" + MainClass.BranchNo + "," + autoConnect +
                    ",'" + txtIP.Text.Trim() +
                    "','" + txtPort.Text.Trim() + "')",
                    conn, sqlTransaction).ExecuteNonQuery();

                sqlTransaction.Commit();

                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                DXMessageBox.Show("خطأ أثناء الحفظ" + Environment.NewLine +
                    "تفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Load Device Settings

        private void LoadDeviceSetting()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "Select * from SettingFingurePrint where Branch_Id=" + MainClass.BranchNo, conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    string deviceIp = dataTable.Rows[0]["DeviceIP"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(deviceIp))
                        txtIP.Text = deviceIp;

                    string port = dataTable.Rows[0]["Port"]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(port))
                        txtPort.Text = port;

                    bool autoConnect = false;
                    if (dataTable.Rows[0]["ConnectAuto"] != DBNull.Value)
                        autoConnect = Convert.ToBoolean(dataTable.Rows[0]["ConnectAuto"]);

                    ckConnectAuto.IsChecked = autoConnect;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        private void EnsureConnectionOpen()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        #endregion
    }
}