using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEtaSetting : DXWindow
    {
        #region Fields

        private SqlConnection conn;

        private static readonly string[] EinvoieTypeItems = new[]
        {
            "pre production",
            "production",
            "phase one"
        };

        private static readonly string[] SignTypeItems = new[]
        {
            "Egypt Trust Sealing CA",
            "MCDR 2019",
            "Egypt Trust CA G6"
        };

        private static readonly string[] DocumentTypeVersionItems = new[]
        {
            "V1.0",
            "V0.9"
        };

        #endregion

        #region Constructor

        public frmEtaSetting()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();
            Loaded += frmEtaSetting_Load;
        }

        #endregion

        #region Load

        private void frmEtaSetting_Load(object sender, RoutedEventArgs e)
        {
            InitializeComboBoxes();
            loadData();
        }

        private void InitializeComboBoxes()
        {
            cmbEinvoieType.ItemsSource = EinvoieTypeItems;
            cmbSignType.ItemsSource = SignTypeItems;
            cmbDocumentTypeVersion.ItemsSource = DocumentTypeVersionItems;
        }

        #endregion

        #region Load Data

        private void loadData()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select * from EtaSetting where Id=1 and Active=1", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    chkActivate.IsChecked = true;

                    string einvoieTypeValue = dataTable.Rows[0]["EinvoieType"]?.ToString().Trim()
                                             ?? string.Empty;

                    switch (einvoieTypeValue.ToLower())
                    {
                        case "phase one":
                            cmbEinvoieType.SelectedIndex = 2;
                            break;
                        case "production":
                            cmbEinvoieType.SelectedIndex = 1;
                            break;
                        default:
                            cmbEinvoieType.SelectedIndex = 0;
                            break;
                    }

                    string signTypeValue = dataTable.Rows[0]["SignType"]?.ToString().Trim()
                                          ?? string.Empty;

                    switch (signTypeValue)
                    {
                        case "Egypt Trust Sealing CA":
                            cmbSignType.SelectedIndex = 0;
                            break;
                        case "MCDR 2019":
                            cmbSignType.SelectedIndex = 1;
                            break;
                        case "Egypt Trust CA G6":
                            cmbSignType.SelectedIndex = 2;
                            break;
                        default:
                            cmbSignType.SelectedIndex = 0;
                            break;
                    }

                    string docVersionValue = dataTable.Rows[0]["DocumentTypeVersion"]?.ToString().Trim()
                                            ?? string.Empty;

                    cmbDocumentTypeVersion.SelectedIndex =
                        string.Equals(docVersionValue, "V1.0", StringComparison.OrdinalIgnoreCase)
                        ? 0 : 1;

                    string receiptValue = dataTable.Rows[0]["Receipt"]?.ToString() ?? string.Empty;
                    chkReceipt.IsChecked =
                        string.Equals(receiptValue, "True", StringComparison.OrdinalIgnoreCase);

                    txtEtaCode.Text = SafeString(dataTable.Rows[0]["EtaCode"]);
                    txtEtaClientId.Text = SafeString(dataTable.Rows[0]["EtaClientId"]);
                    txtEtaClientSecret1.Text = SafeString(dataTable.Rows[0]["EtaClientSecret1"]);
                    txtEtaClientSecret2.Text = SafeString(dataTable.Rows[0]["EtaClientSecret2"]);
                    txtBranchCode.Text = SafeString(dataTable.Rows[0]["BranchCode"]);
                    txtActivityCode.Text = SafeString(dataTable.Rows[0]["ActivityCode"]);
                    txtPinPass.Text = SafeString(dataTable.Rows[0]["Pinpass"]);

                    string serialFilePath = @"C:\EtaConfigFile\PosEtaSerial.txt";
                    txtPosEtaSerial.Text = File.Exists(serialFilePath)
                        ? File.ReadAllText(serialFilePath).Trim()
                        : "1000";
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات: " + ex.Message);
            }
        }

        #endregion

        #region Save

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo > 0)
            {
                MessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (cmbEinvoieType.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار نوع الفاتورة الإلكترونية.",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbSignType.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار نوع الختم الالكتروني.",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbDocumentTypeVersion.SelectedItem == null)
                {
                    MessageBox.Show("يرجى اختيار إصدار نوع المستند.",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureConnectionOpen();

                SqlCommand sqlCommand = new SqlCommand(
                    "update EtaSetting set " +
                    "Active=@Active, " +
                    "EinvoieType=@EinvoieType, " +
                    "SignType=@SignType, " +
                    "DocumentTypeVersion=@DocumentTypeVersion, " +
                    "EtaCode=@EtaCode, " +
                    "EtaClientId=@EtaClientId, " +
                    "EtaClientSecret1=@EtaClientSecret1, " +
                    "EtaClientSecret2=@EtaClientSecret2, " +
                    "BranchCode=@BranchCode, " +
                    "ActivityCode=@ActivityCode, " +
                    "Receipt=@Receipt, " +
                    "Pinpass=@Pinpass " +
                    "where Id=1", conn);

                sqlCommand.Parameters.Add("@Active", SqlDbType.Bit).Value =
                    chkActivate.IsChecked == true ? 1 : 0;

                sqlCommand.Parameters.Add("@Receipt", SqlDbType.Bit).Value =
                    chkReceipt.IsChecked == true ? 1 : 0;

                sqlCommand.Parameters.Add("@EinvoieType", SqlDbType.NVarChar).Value =
                    cmbEinvoieType.SelectedItem?.ToString().Trim() ?? string.Empty;

                sqlCommand.Parameters.Add("@SignType", SqlDbType.NVarChar).Value =
                    cmbSignType.SelectedItem?.ToString().Trim() ?? string.Empty;

                sqlCommand.Parameters.Add("@DocumentTypeVersion", SqlDbType.NVarChar).Value =
                    cmbDocumentTypeVersion.SelectedItem?.ToString().Trim() ?? string.Empty;

                sqlCommand.Parameters.Add("@EtaCode", SqlDbType.NVarChar).Value =
                    txtEtaCode.Text.Trim();

                sqlCommand.Parameters.Add("@EtaClientId", SqlDbType.NVarChar).Value =
                    txtEtaClientId.Text.Trim();

                sqlCommand.Parameters.Add("@EtaClientSecret1", SqlDbType.NVarChar).Value =
                    txtEtaClientSecret1.Text.Trim();

                sqlCommand.Parameters.Add("@EtaClientSecret2", SqlDbType.NVarChar).Value =
                    txtEtaClientSecret2.Text.Trim();

                sqlCommand.Parameters.Add("@BranchCode", SqlDbType.NVarChar).Value =
                    txtBranchCode.Text.Trim();

                sqlCommand.Parameters.Add("@ActivityCode", SqlDbType.NVarChar).Value =
                    txtActivityCode.Text.Trim();

                sqlCommand.Parameters.Add("@Pinpass", SqlDbType.NVarChar).Value =
                    txtPinPass.Text.Trim();

                sqlCommand.ExecuteNonQuery();

                string etaConfigFolder = @"C:\EtaConfigFile\";
                string serialFileName = "PosEtaSerial.txt";
                string serialFilePath = Path.Combine(etaConfigFolder, serialFileName);

                if (!Directory.Exists(etaConfigFolder))
                {
                    Directory.CreateDirectory(etaConfigFolder);
                    File.Create(serialFilePath).Dispose();
                }

                File.WriteAllText(serialFilePath, txtPosEtaSerial.Text.Trim());

                string saveMessage = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? "تم الحفظ"
                    : "Saved";

                MessageBox.Show(saveMessage, "", MessageBoxButton.OK, MessageBoxImage.Information);

                Common.LoadEtaSetting();
            }
            catch (Exception ex)
            {
                ShowError("حصل خطأ أثناء الحفظ" + Environment.NewLine + ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void EnsureConnectionOpen()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private string SafeString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;
            return value.ToString().Trim();
        }

        private void ShowError(string message, string title = "خطأ")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}