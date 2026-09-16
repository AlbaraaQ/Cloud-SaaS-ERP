using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Windows;
using log4net;
using Newtonsoft.Json;
using UtilitiesProj;
using ZatcaIntegrationSDK;
using ZatcaIntegrationSDK.APIHelper;
using ZatcaIntegrationSDK.BLL;
using ZatcaIntegrationSDK.CSRGenerate;
using ZatcaIntegrationSDK.HelperContracts;
using AuditorAPI.Models;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Core;
using AuditorAPI.Models.DGVmodels;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmZatcaSetting : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private CSRRequest    request;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        // حقول مساعدة للتعبئة التلقائية
        private string _CommonName            = "";
        private string _OrganizationName      = "";
        private string _OrganizationIdentifier= "";
        private string _OrganizationUnitName  = "";
        private string _Industry              = "";
        private string _CR_NO                 = "";

        #endregion

        #region Constructor

        public frmZatcaSetting()
        {
            InitializeComponent();
            conn    = MainClass.ConnObj();
            request = new CSRRequest();
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.DateTime = DateTime.Now.Date;
            chkActivate.IsChecked  = true;
            rdProduction.IsChecked = true;
            rdCompliance.IsChecked = false;

            GenerateSerialNo();
            LoadData();
            GetzatcaOnproduction();

            BtnOnOff.Content = Common.GetZatcaActive()
                ? "⏸ إيقاف الربط"
                : "▶ تشغيل";
        }

        #endregion

        #region Load Data

        private void LoadData()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT ISNULL(filePath,'') AS filePath," +
                    "ISNULL(isProduction,0) AS isProduction," +
                    "ISNULL(IsActive,0) AS IsActive," +
                    "ISNULL(IsSimulation,0) AS IsSimulation," +
                    "ISNULL(SyncManual,0) AS SyncManual," +
                    "ISNULL(StartDate,'') AS StartDate " +
                    "FROM SettingZatca WHERE ID=1", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    var row = table.Rows[0];
                    chkActivate.IsChecked = Convert.ToBoolean(row["IsActive"]);
                    txtPath.Text = row["filePath"].ToString();

                    if (row["isProduction"].ToString() == "0")
                        rdCompliance.IsChecked = true;
                    else
                        rdProduction.IsChecked = true;

                    if (DateTime.TryParse(row["StartDate"].ToString(), out DateTime startDate))
                        txtDate.DateTime = startDate;

                    chkSimulation.IsChecked = Convert.ToBoolean(row["IsSimulation"]);
                    chkSyncManual.IsChecked = Convert.ToBoolean(row["SyncManual"]);
                }

                LoadCsrProperties();
            }
            catch { }
        }

        private void LoadCsrProperties()
        {
            try
            {
                using var sqlConn = new SqlConnection(MainClass.connstr);
                sqlConn.Open();
                using var cmd = new SqlCommand(
                    "SELECT TOP 1 commonName,serialNumber,organizationIdentifier," +
                    "organizationUnitName,organizationName,countryName,invoiceType," +
                    "address,industry FROM CsrProperties", sqlConn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtCommonName.Text             = reader["commonName"].ToString();
                    txtSerialNumber.Text           = reader["serialNumber"].ToString();
                    txtOrganizationIdentifier.Text = reader["organizationIdentifier"].ToString();
                    txtOrganizationUnitName.Text   = reader["organizationUnitName"].ToString();
                    txtOrganizationName.Text       = reader["organizationName"].ToString();
                    txtCountryName.Text            = reader["countryName"].ToString();
                    txtInvoiceType.Text            = reader["invoiceType"].ToString();
                    txtAddress.Text               = reader["address"].ToString();
                    txtIndustry.Text              = reader["industry"].ToString();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("حدث خطأ أثناء تحميل بيانات CSR: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Generate Serial

        private void GenerateSerialNo()
        {
            var versionInfo = Common.CurrentVersion();
            string guid     = Guid.NewGuid().ToString();
            txtSerialNumber.Text =
                $"1-Auditor|2-{versionInfo.VersionText}|3-{guid}";
        }

        #endregion

        #region Save Settings

        private void btnSavePath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    DXMessageBox.Show("نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var pwdWindow = new frmCheckPwd
                    { operNo = 400, CheckType = 400 };
                pwdWindow.ShowDialog();
                if (!pwdWindow.Iscorrect) return;

                if (string.IsNullOrWhiteSpace(txtOTP.Text))
                {
                    DXMessageBox.Show("أدخل OTP", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (MainClass.IsTrial)
                {
                    string msg = string.Equals(MainClass.Language, "en-us")
                        ? "This is a trial version. Please activate the license to continue."
                        : "هذه نسخة تجريبية. يرجى ترخيص النظام للاستمرار.";
                    DXMessageBox.Show(msg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Generate();
                SaveCSR();
                ComplianceCSID();

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand("DELETE FROM SettingZatca", conn).ExecuteNonQuery();

                var insertCmd = new SqlCommand(
                    "INSERT INTO SettingZatca " +
                    "(ID,filePath,isProduction,IsActive,IsSimulation,SyncManual,StartDate,EndDate) " +
                    "VALUES (1,@filePath,@isProduction,@IsActive,@IsSimulation,@SyncManual,@StartDate,@EndDate)",
                    conn);
                insertCmd.Parameters.Add("@filePath",     SqlDbType.NVarChar).Value = txtPath.Text;
                insertCmd.Parameters.Add("@isProduction", SqlDbType.Bit).Value      =
                    rdCompliance.IsChecked == true ? 0 : 1;
                insertCmd.Parameters.Add("@IsActive",     SqlDbType.Bit).Value      =
                    chkActivate.IsChecked == true;
                insertCmd.Parameters.Add("@IsSimulation", SqlDbType.Bit).Value      =
                    chkSimulation.IsChecked == true;
                insertCmd.Parameters.Add("@SyncManual",   SqlDbType.Bit).Value      =
                    chkSyncManual.IsChecked == true;
                insertCmd.Parameters.Add("@StartDate",    SqlDbType.DateTime).Value =
                    txtDate.DateTime;
                insertCmd.Parameters.Add("@EndDate",      SqlDbType.DateTime).Value =
                    txtDate.DateTime.AddYears(1);
                insertCmd.ExecuteNonQuery();

                // CSR Properties
                var checkAdapter = new SqlDataAdapter(
                    $"SELECT serialNumber FROM CSRProperties WHERE serialNumber=N'{txtSerialNumber.Text}'",
                    conn);
                var checkTable = new DataTable();
                checkAdapter.Fill(checkTable);

                SqlCommand csrCmd;
                if (checkTable.Rows.Count == 0)
                {
                    csrCmd = new SqlCommand(
                        "INSERT INTO CSRProperties(Id,commonName,serialNumber," +
                        "organizationIdentifier,organizationUnitName,organizationName," +
                        "countryName,invoiceType,address,industry) " +
                        "VALUES(@Id,@commonName,@serialNumber,@organizationIdentifier," +
                        "@organizationUnitName,@organizationName,@countryName," +
                        "@invoiceType,@address,@industry)", conn);
                    csrCmd.Parameters.Add("@Id", SqlDbType.Int).Value = 1;
                }
                else
                {
                    csrCmd = new SqlCommand(
                        "UPDATE CSRProperties SET commonName=@commonName," +
                        "organizationIdentifier=@organizationIdentifier," +
                        "organizationUnitName=@organizationUnitName," +
                        "organizationName=@organizationName,countryName=@countryName," +
                        "invoiceType=@invoiceType,address=@address,industry=@industry", conn);
                }

                csrCmd.Parameters.Add("@commonName",              SqlDbType.NVarChar).Value = txtCommonName.Text;
                csrCmd.Parameters.Add("@serialNumber",            SqlDbType.NVarChar).Value = txtSerialNumber.Text;
                csrCmd.Parameters.Add("@organizationIdentifier",  SqlDbType.NVarChar).Value = txtOrganizationIdentifier.Text;
                csrCmd.Parameters.Add("@organizationUnitName",    SqlDbType.NVarChar).Value = txtOrganizationUnitName.Text;
                csrCmd.Parameters.Add("@organizationName",        SqlDbType.NVarChar).Value = txtOrganizationName.Text;
                csrCmd.Parameters.Add("@countryName",             SqlDbType.NVarChar).Value = txtCountryName.Text;
                csrCmd.Parameters.Add("@invoiceType",             SqlDbType.NVarChar).Value = txtInvoiceType.Text;
                csrCmd.Parameters.Add("@address",                 SqlDbType.NVarChar).Value = txtAddress.Text;
                csrCmd.Parameters.Add("@industry",                SqlDbType.NVarChar).Value = txtIndustry.Text;
                csrCmd.ExecuteNonQuery();

                // ZatcaCredential
                new SqlCommand("DELETE FROM ZatcaCredential", conn).ExecuteNonQuery();
                var credCmd = new SqlCommand(
                    "INSERT INTO ZatcaCredential " +
                    "(ID,CSR,PrivateKey,CSID,Secret,RequestID) " +
                    "VALUES(1,@CSR,@PrivateKey,@CSID,@Secret,@RequestID)", conn);
                credCmd.Parameters.Add("@CSR",       SqlDbType.NVarChar).Value = request.Csr;
                credCmd.Parameters.Add("@PrivateKey", SqlDbType.NVarChar).Value = request.PrivateKey;
                credCmd.Parameters.Add("@CSID",      SqlDbType.NVarChar).Value = request.CSID;
                credCmd.Parameters.Add("@Secret",    SqlDbType.NVarChar).Value = request.Secret;
                credCmd.Parameters.Add("@RequestID", SqlDbType.NVarChar).Value = request.RequestID;
                credCmd.ExecuteNonQuery();

                if (conn.State != ConnectionState.Closed) conn.Close();

                MainClass.ZatcafilePath = txtPath.Text;
                MainSetting.IsProductionZatca       = rdProduction.IsChecked == true;
                MainSetting.ZatcaIntegerationActive = chkActivate.IsChecked == true;
                MainSetting.IsSimulationZatca       = chkSimulation.IsChecked == true;
                MainSetting.ZatcaSyncManual         = chkSyncManual.IsChecked == true;

                string savedMsg = string.Equals(MainClass.Language, "ar") ? "تم الحفظ" : "Saved";
                DXMessageBox.Show(savedMsg, "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} btnSavePath_Click {ex.Message} {MainClass.UserName}");
                DXMessageBox.Show("خطأ\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region PCSID

        private void btnPCSID_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.IsTrial)
            {
                DXMessageBox.Show("هذه نسخة تجريبية. يرجى ترخيص النظام للاستمرار.",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MainClass.EmpNo > 0)
            {
                DXMessageBox.Show("نأسف ليس لديك الصلاحية لهذه العملية",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!MainClass.CheckForInternetConnection())
            {
                DXMessageBox.Show("الرجاء الاتصال بالإنترنت",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool isProduction = rdProduction.IsChecked == true;
                var cred          = LoadZatcaCredential();
                var result        = new CSRGenerator().ProductionCSIDGenerate(
                    BinarySecurityToken: GlobalVariables.ToBase64Encode(cred.CSID),
                    compliance_request_id: cred.RequestID,
                    Secret: cred.Secret,
                    isProduction: isProduction,
                    IsSimulation: chkSimulation.IsChecked == true);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    DXMessageBox.Show(result.ErrorMessage,
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Error($"{Title} btnPCSID {result.ErrorMessage} {MainClass.UserName}");
                    return;
                }

                request.P_CSID      = result.CSID.Trim();
                request.P_Secret    = result.Secret.Trim();
                request.P_RequestID = result.RequestID;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"{Title} btnPCSID {ex.Message} {MainClass.UserName}");
                return;
            }

            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                int count = Convert.ToInt32(
                    new SqlCommand("SELECT COUNT(*) FROM ZatcaCredential WHERE ID=1", conn)
                    .ExecuteScalar());

                SqlCommand cmd;
                if (count == 0)
                    cmd = new SqlCommand(
                        "INSERT INTO ZatcaCredential(ID,CSR,RequestID,CSID,Secret,P_RequestID,P_CSID,P_Secret) " +
                        "VALUES(1,@RequestID,@CSID,@Secret,@P_RequestID,@P_CSID,@P_Secret)", conn);
                else
                    cmd = new SqlCommand(
                        "UPDATE ZatcaCredential SET P_RequestID=@P_RequestID," +
                        "P_CSID=@P_CSID,P_Secret=@P_Secret WHERE ID=1", conn);

                cmd.Parameters.Add("@P_RequestID", SqlDbType.NVarChar).Value = request.P_RequestID;
                cmd.Parameters.Add("@P_CSID",      SqlDbType.NVarChar).Value = request.P_CSID;
                cmd.Parameters.Add("@P_Secret",    SqlDbType.NVarChar).Value = request.P_Secret;
                cmd.ExecuteNonQuery();

                if (conn.State != ConnectionState.Closed) conn.Close();
                DXMessageBox.Show("تم تحديث البيانات بنجاح", "Done!",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"{Title} btnPCSID_Save {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region Radio Buttons

        private void rdCompliance_Checked(object sender, RoutedEventArgs e)
        {
            if (rdCompliance.IsChecked == true && rdProduction != null)
                rdProduction.IsChecked = false;
        }

        private void rdProduction_Checked(object sender, RoutedEventArgs e)
        {
            if (rdProduction.IsChecked == true && rdCompliance != null)
                rdCompliance.IsChecked = false;
        }

        #endregion

        #region Compliance Test

        private void BtnComplianceInvoiceDebitNote_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.IsTrial)
                {
                    DXMessageBox.Show("هذه نسخة تجريبية. يرجى ترخيص النظام للاستمرار.",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool isProduction = rdProduction.IsChecked == true;

                var r1 = ComplianceCheck(2, 1, isProduction, false, false, false);
                if (!r1.IsSuccess) { ShowComplianceError("Standard Invoice", r1); return; }

                var r2 = ComplianceCheck(2, 2, isProduction, false, true, false);
                if (!r2.IsSuccess) { ShowComplianceError("Standard Debit Note", r2); return; }

                var r3 = ComplianceCheck(2, 2, isProduction, false, false, false);
                if (!r3.IsSuccess) { ShowComplianceError("Standard Credit Note", r3); return; }

                var r4 = ComplianceCheck(2, 1, isProduction, false, false, true);
                if (!r4.IsSuccess) { ShowComplianceError("Simplified Invoice", r4); return; }

                var r5 = ComplianceCheck(2, 2, isProduction, false, true, true);
                if (!r5.IsSuccess) { ShowComplianceError("Simplified Debit Note", r5); return; }

                var r6 = ComplianceCheck(2, 2, isProduction, false, false, true);
                if (!r6.IsSuccess) { ShowComplianceError("Simplified Credit Note", r6); return; }

                DXMessageBox.Show("تم بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"{Title} BtnComplianceInvoiceDebitNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void ShowComplianceError(string checkName, SystemResponse response)
        {
            DXMessageBox.Show(
                $"{checkName} compliance check failed.\n{response.ResponseObject}",
                "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion

        #region Generate CSR

        private void Generate()
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    DXMessageBox.Show("نأسف ليس لديك الصلاحية لهذه العملية",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var props = new CSRProperties
                {
                    commonName             = txtCommonName.Text,
                    serialNumber           = txtSerialNumber.Text,
                    organizationIdentifier = txtOrganizationIdentifier.Text,
                    organizationUnitName   = txtOrganizationUnitName.Text,
                    organizationName       = txtOrganizationName.Text,
                    countryName            = txtCountryName.Text,
                    invoiceType            = txtInvoiceType.Text,
                    address                = txtAddress.Text,
                    industry               = txtIndustry.Text
                };

                bool isProduction  = rdProduction.IsChecked == true;
                bool isSimulation  = chkSimulation.IsChecked == true;

                var result = new CSRGenerator().Generate(props, isProduction, isSimulation);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    Logger.Error($"{Title} Generate {result.ErrorMessage} {MainClass.UserName}");

                txtCSR.Text        = result.Csr;
                txtPrivateKey.Text = result.PrivateKey;
                request.Csr        = result.Csr;
                request.PrivateKey = result.PrivateKey;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} Generate {ex.Message} {MainClass.UserName}");
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            Generate();
        }

        #endregion

        #region Save CSR

        private void SaveCSR()
        {
            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();
                new SqlCommand("DELETE FROM ZatcaCredential", conn).ExecuteNonQuery();
                var cmd = new SqlCommand(
                    "INSERT INTO ZatcaCredential(ID,CSR,PrivateKey,CSID,Secret) " +
                    "VALUES(1,@CSR,@PrivateKey,@CSID,@Secret)", conn);
                cmd.Parameters.Add("@CSR",       SqlDbType.NVarChar).Value = request.Csr;
                cmd.Parameters.Add("@PrivateKey", SqlDbType.NVarChar).Value = request.PrivateKey;
                cmd.Parameters.Add("@CSID",      SqlDbType.NVarChar).Value = request.CSID ?? "";
                cmd.Parameters.Add("@Secret",    SqlDbType.NVarChar).Value = request.Secret ?? "";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.InnerException?.ToString() ?? ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"{Title} SaveCSR {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region Compliance CSID

        private void ComplianceCSID()
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    DXMessageBox.Show("نأسف ليس لديك الصلاحية لهذه العملية",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrEmpty(txtOTP.Text))
                {
                    DXMessageBox.Show("يجب إدخال OTP",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!MainClass.CheckForInternetConnection())
                {
                    DXMessageBox.Show("الرجاء الاتصال بالإنترنت",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtCSR.Text))
                {
                    DXMessageBox.Show("يجب عليك إنشاء CSR أولاً!",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool isProduction = rdProduction.IsChecked == true;
                bool isSimulation = chkSimulation.IsChecked == true;

                var result = new CSRGenerator().ComplianceCSIDGenerate(
                    txtOTP.Text.Trim(), txtCSR.Text, isProduction, isSimulation);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    Logger.Error($"{Title} ComplianceCSID {result.ErrorMessage} {MainClass.UserName}");

                if (string.IsNullOrEmpty(result.CSID))
                {
                    DXMessageBox.Show(result.ErrorMessage,
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                request.CSID      = result.CSID.Trim();
                request.Secret    = result.Secret.Trim();
                request.RequestID = result.RequestID;

                try
                {
                    if (conn.State == ConnectionState.Open) conn.Close();
                    conn.Open();
                    new SqlCommand("DELETE FROM ZatcaCredential", conn).ExecuteNonQuery();
                    var cmd = new SqlCommand(
                        "INSERT INTO ZatcaCredential(ID,CSR,PrivateKey,CSID,Secret,RequestID) " +
                        "VALUES(1,@CSR,@PrivateKey,@CSID,@Secret,@RequestID)", conn);
                    cmd.Parameters.Add("@CSR",       SqlDbType.NVarChar).Value = request.Csr;
                    cmd.Parameters.Add("@PrivateKey", SqlDbType.NVarChar).Value = request.PrivateKey;
                    cmd.Parameters.Add("@CSID",      SqlDbType.NVarChar).Value = request.CSID;
                    cmd.Parameters.Add("@Secret",    SqlDbType.NVarChar).Value = request.Secret;
                    cmd.Parameters.Add("@RequestID", SqlDbType.NVarChar).Value = request.RequestID;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show(ex.InnerException?.ToString() ?? ex.Message,
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Error($"{Title} ComplianceCSID_Save {ex.Message} {MainClass.UserName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} ComplianceCSID {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region Compliance Check

        private SystemResponse ComplianceCheck(
            int InvoiceType, int ProcType,
            bool isProduction, bool IsSimulation,
            bool isDebit, bool IsIsSimplified)
        {
            var response = new SystemResponse();
            try
            {
                if (!MainClass.CheckForInternetConnection())
                {
                    response.Message   = "الرجاء الاتصال بالإنترنت";
                    response.IsSuccess = false;
                    return response;
                }

                var invoice = new AuditorAPI.Models.Invoice
                {
                    InvoiceNo        = 1,
                    AutoIncrementID  = 1,
                    UUID             = "8d487816-70b8-4ade-a618-9d620b73814a",
                    InvDate          = DateTime.Now,
                    InvoiceType      = (AuditorAPI.Models.InvoiceType)InvoiceType,
                    ProcType         = ProcType,
                    PIH              = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==",
                    PayType          = 1,
                    VAT              = 0.6,
                    Total            = 4.0,
                    Net              = 4.6,
                    VATperc          = 15.0,
                    Discount         = 0.0
                };
                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    ItemName     = "قلم رصاص",
                    ItemQuantity = 2.0,
                    ItemId       = 1,
                    ItemVatPerc  = 15.0,
                    ItemPrice    = 2.0,
                    ItemDiscount = 0.0
                });

                var customer = new Customer(0)
                {
                    Name                 = "Acme Widget's LTD 2",
                    CrNo                 = "4020000000",
                    City                 = "Jeddah",
                    Country              = "SA",
                    District             = "TST",
                    StreetName           = "TST",
                    AdditionalStreetName = "TST",
                    BuildingNumber       = "1234",
                    PlotIdentification   = "1224",
                    VATno                = "311111111111113"
                };

                var result = IntegrateInvoice(
                    ref invoice, isProduction, IsSimulation, isDebit, customer, IsIsSimplified);

                response.IsSuccess     = result.success;
                response.ResponseObject= result.ReportingStatus;

                string invJson = JsonConvert.SerializeObject(invoice);
                Logger.Error($"{Title} ComplianceCheck {invJson} {MainClass.UserName}");

                if (!result.success)
                {
                    response.ResponseObject = result.validationResults != null
                        ? JsonConvert.SerializeObject(result.validationResults.ErrorMessages)
                        : result.ErrorMessage;
                    response.IsSuccess = false;
                }
                else
                {
                    response.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                Logger.Error($"{Title} ComplianceCheck {ex.Message} {MainClass.UserName}");
            }
            return response;
        }

        #endregion

        #region Integrate Invoice

        public InvoiceReportingResponse IntegrateInvoice(
            ref AuditorAPI.Models.Invoice invoice,
            bool IsProduction, bool IsSimulation,
            bool isDebit, Customer customer, bool IsSimplified)
        {
            var invoiceReportingResponse = new InvoiceReportingResponse();
            try
            {
                if (!MainClass.CheckForInternetConnection())
                {
                    invoiceReportingResponse.ErrorMessage = "الرجاء الاتصال بالانترنت";
                    return invoiceReportingResponse;
                }

                string name  = IsSimplified ? "0200000" : "0100000";
                string value = invoice.ProcType != 2 ? "388" : "381";
                if (isDebit) value = "383";

                var uBLXML   = new UBLXML();
                var invoice2 = new ZatcaIntegrationSDK.Invoice();

                invoice2.ID           = invoice.InvoiceNo.ToString();
                invoice2.PIH          = invoice.PIH;
                invoice2.IssueDate    = invoice.InvDate.ToString("yyyy-MM-dd");
                invoice2.IssueTime    = invoice.InvDate.ToString("HH:mm:ss");
                invoice2.invoiceTypeCode.id   = int.Parse(value);
                invoice2.invoiceTypeCode.Name = name;
                invoice2.DocumentCurrencyCode = "SAR";
                invoice2.TaxCurrencyCode      = "SAR";

                if (invoice.ProcType == 2)
                    invoice2.billingReference.InvoiceDocumentReferenceID = invoice.ReffNo;

                invoice2.AdditionalDocumentReferenceICV.UUID = invoice.AutoIncrementID;

                string paymentMeansCode = "10";
                if (invoice.PayType == -1) paymentMeansCode = "30";
                else if (invoice.PayType == 2)
                    paymentMeansCode = invoice.Bank <= 2 ? "48" : "42";
                invoice2.paymentmeans.PaymentMeansCode = paymentMeansCode;

                if (invoice.ProcType == 2)
                    invoice2.paymentmeans.InstructionNote = " Refund.";

                invoice2.InvTaxAmount          = Convert.ToDecimal(invoice.VAT.ToString(Common.DigitsNo));
                invoice2.InvTaxExclusiveAmount = Convert.ToDecimal(invoice.Total.ToString(Common.DigitsNo));
                invoice2.InvNet                = Convert.ToDecimal(invoice.Net.ToString(Common.DigitsNo));
                invoice2.TaxTotal.TaxSubtotal.taxCategory.ID      = "S";
                invoice2.TaxTotal.TaxSubtotal.taxCategory.Percent = new decimal(invoice.VATperc);

                if (Common.FoundationInfoDT.Rows.Count > 0)
                {
                    var fr = Common.FoundationInfoDT.Rows[0];
                    invoice2.SupplierParty.partyLegalEntity.RegistrationName = fr["nameA"].ToString();
                    invoice2.SupplierParty.partyTaxScheme.CompanyID          = fr["tax_no"].ToString();
                    invoice2.SupplierParty.partyIdentification.ID            = fr["bsn_no"].ToString();
                    invoice2.SupplierParty.partyIdentification.schemeID      = "CRN";
                    invoice2.SupplierParty.postalAddress.StreetName          = fr["StreetName"].ToString();
                    invoice2.SupplierParty.postalAddress.BuildingNumber      = fr["BuildingNumber"].ToString();
                    invoice2.SupplierParty.postalAddress.PlotIdentification  = fr["PlotIdentification"].ToString();
                    invoice2.SupplierParty.postalAddress.CitySubdivisionName = fr["District"].ToString();
                    invoice2.SupplierParty.postalAddress.PostalZone          = fr["PostalZone"].ToString();
                    invoice2.SupplierParty.postalAddress.CityName            = fr["city"].ToString();
                    invoice2.SupplierParty.postalAddress.country.IdentificationCode = "SA";
                    invoice2.SupplierParty.postalAddress.CountrySubentity    = fr["Area"].ToString();
                }

                if (customer != null && customer != (object)DBNull.Value)
                {
                    string reg = customer.Region, cntry = customer.Country, city = customer.City;
                    Common.GetCityName(ref reg, ref cntry, ref city,
                        customer.Region, customer.Country, customer.City);
                    customer.Region  = reg;
                    customer.Country = cntry;
                    customer.City    = city;
                }

                invoice2.CustomerParty.partyIdentification.ID             = customer?.CrNo ?? "";
                invoice2.CustomerParty.partyIdentification.schemeID       = "CRN";
                invoice2.CustomerParty.postalAddress.StreetName           = customer?.StreetName ?? "";
                invoice2.CustomerParty.postalAddress.AdditionalStreetName = customer?.AdditionalStreetName ?? "";
                invoice2.CustomerParty.postalAddress.BuildingNumber       = customer?.BuildingNumber ?? "";
                invoice2.CustomerParty.postalAddress.PlotIdentification   = customer?.PlotIdentification ?? "";
                invoice2.CustomerParty.postalAddress.CityName             = customer?.City ?? "";
                invoice2.CustomerParty.postalAddress.PostalZone           = customer?.PostalZone ?? "";
                invoice2.CustomerParty.postalAddress.CountrySubentity     = customer?.Region ?? "";
                invoice2.CustomerParty.postalAddress.CitySubdivisionName  = customer?.District ?? "";
                invoice2.CustomerParty.postalAddress.country.IdentificationCode = "SA";
                invoice2.CustomerParty.partyLegalEntity.RegistrationName  = customer?.Name ?? "";
                invoice2.CustomerParty.partyTaxScheme.CompanyID           = customer?.VATno ?? "";

                var ac = new AllowanceCharge
                {
                    Amount = new decimal(invoice.Discount),
                    AllowanceChargeReason = "discount"
                };
                ac.taxCategory.ID      = "S";
                ac.taxCategory.Percent = 15m;
                invoice2.allowanceCharges.Add(ac);

                double totalDiscount = 0;
                foreach (var a in invoice2.allowanceCharges)
                    if (decimal.Compare(a.Amount, 0m) > 0)
                        totalDiscount += Convert.ToDouble(a.Amount);
                totalDiscount = Math.Round(totalDiscount, 2);

                double lineTotal = 0, lineTax = 0;
                foreach (var item in invoice.InvoiceItems)
                {
                    var line = new InvoiceLine();
                    line.ID = item.ItemId.ToString();
                    line.item.Name = item.ItemName;
                    line.item.classifiedTaxCategory.ID      =
                        item.ItemVatPerc == 0 ? "Z" : "S";
                    line.item.classifiedTaxCategory.Percent = (float)item.ItemVatPerc;
                    line.price.EncludingVat = false;

                    double price = invoice.PriceIncVAT
                        ? Math.Round(item.ItemPrice / (1 + item.ItemVatPerc / 100.0), 2)
                        : item.ItemPrice;

                    double qty    = Math.Round(item.ItemQuantity, 2);
                    double net    = Math.Round(price * qty - Math.Round(item.ItemDiscount, 2), 2);
                    double vat    = Math.Round(net * (item.ItemVatPerc / 100.0), 2);
                    double gross  = Math.Round(net + vat, 2);

                    line.InvoiceQuantity                             = new decimal(qty);
                    line.price.PriceAmount                           = new decimal(price);
                    line.price.allowanceCharge.AllowanceChargeReason = "discount";
                    line.price.allowanceCharge.Amount                = new decimal(item.ItemDiscount);
                    line.taxTotal.TaxSubtotal.taxCategory.ID         = item.ItemVatPerc == 0 ? "Z" : "S";
                    line.taxTotal.TaxSubtotal.taxCategory.Percent    = new decimal(item.ItemVatPerc);

                    if (item.ItemVatPerc == 0)
                    {
                        line.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason     = "Medicines and medical equipment";
                        line.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-35";
                    }

                    line.taxTotal.TaxSubtotal.TaxableAmount = new decimal(net);
                    line.taxTotal.TaxSubtotal.TaxAmount     = new decimal(vat);
                    line.LineExtensionAmount                = (float)net;
                    line.taxTotal.TaxSubtotal.RoundingAmount= new decimal(gross);

                    invoice2.InvoiceLines.Add(line);
                    lineTotal += net;
                    lineTax   += vat;
                    invoice2.LineExtensionAmount =
                        new decimal(Convert.ToDouble(invoice2.LineExtensionAmount) + net);
                }

                double rawTotal = lineTotal;
                lineTotal = Math.Round(lineTotal - totalDiscount, 2);

                double adjTax = 0;
                foreach (var line in invoice2.InvoiceLines)
                {
                    double lnet = Convert.ToDouble(
                        decimal.Subtract(
                            decimal.Multiply(line.InvoiceQuantity, line.price.PriceAmount),
                            line.price.allowanceCharge.Amount));
                    adjTax += (lnet - lnet / rawTotal * totalDiscount) *
                              Convert.ToDouble(decimal.Divide(
                                  line.taxTotal.TaxSubtotal.taxCategory.Percent, 100m));
                }

                invoice2.InvTaxExclusiveAmount = new decimal(lineTotal);
                invoice2.TaxTotal.TaxAmount    = new decimal(Math.Round(adjTax, 2));
                invoice2.InvNet                = new decimal(Math.Round(lineTotal + adjTax, 2));

                var cred   = LoadZatcaCredential();
                var xmlRes = uBLXML.GenerateInvoiceXML(invoice2, "", cred.CSID, cred.PrivateKey);

                if (xmlRes.IsValid)
                {
                    invoice.InvoiceHash   = xmlRes.InvoiceHash;
                    invoice.UUID          = xmlRes.UUID;
                    invoice.QRCode        = xmlRes.QRCode;
                    invoice.PIH           = xmlRes.PIH;
                    invoice.EncodedInvoice= xmlRes.EncodedInvoice;
                }
                else
                {
                    DXMessageBox.Show(xmlRes.ErrorMessage,
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                    invoiceReportingResponse.success = false;
                    Logger.Error($"{Title} IntegrateInvoice {xmlRes.ErrorMessage} {MainClass.UserName}");
                }

                var apiLogic = new ApiRequestLogic();
                var apiReq   = new InvoiceReportingRequest
                {
                    invoiceHash = xmlRes.InvoiceHash,
                    uuid        = invoice.UUID,
                    invoice     = xmlRes.EncodedInvoice
                };

                var apiResp = apiLogic.CallComplianceInvoiceAPI(
                    GlobalVariables.ToBase64Encode(cred.CSID),
                    cred.Secret, apiReq, IsSimulation, IsProduction);

                invoiceReportingResponse = apiResp;
                if (string.Equals(apiResp.ClearanceStatus, "CLEARED") ||
                    string.Equals(apiResp.ReportingStatus, "REPORTED"))
                {
                    invoice.ZatcaSent              = true;
                    invoiceReportingResponse.success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} IntegrateInvoice {ex.Message} {MainClass.UserName}");
            }
            return invoiceReportingResponse;
        }

        #endregion

        #region Load Credential

        public CSRRequest LoadZatcaCredential()
        {
            var cred    = new CSRRequest();
            var adapter = new SqlDataAdapter(
                "SELECT TOP 1 RequestID,CSR,PrivateKey,CSID,Secret,RequestID " +
                "FROM ZatcaCredential", conn);
            var table   = new DataTable();
            adapter.Fill(table);
            if (table.Rows.Count > 0)
            {
                var row       = table.Rows[0];
                cred.RequestID = row["RequestID"].ToString();
                cred.Csr       = row["CSR"].ToString();
                cred.PrivateKey= row["PrivateKey"].ToString();
                cred.CSID      = row["CSID"].ToString();
                cred.Secret    = row["Secret"].ToString();
            }
            return cred;
        }

        #endregion

        #region Load Data from Foundation

        private void btngetData_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("يرجى التأكد من تعبئة كافة البيانات في بطاقة المنشأة",
                "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            GetFoundation();
        }

        private void GetFoundation()
        {
            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                using var reader = new SqlCommand(
                    "SELECT * FROM Foundation", conn).ExecuteReader();

                if (!reader.Read()) return;

                string nameA = reader["nameA"].ToString();
                if (string.IsNullOrWhiteSpace(nameA))
                { DXMessageBox.Show("يرجى تعبئة اسم المنشأة"); return; }

                string taxNo = reader["tax_no"].ToString();
                if (string.IsNullOrWhiteSpace(taxNo))
                { DXMessageBox.Show("يرجى تعبئة الرقم الضريبي"); return; }

                string bsnNo = reader["bsn_no"].ToString();
                if (string.IsNullOrWhiteSpace(bsnNo))
                { DXMessageBox.Show("يرجى تعبئة السجل التجاري"); return; }

                string fieldA = reader["FieldA"].ToString();
                if (string.IsNullOrWhiteSpace(fieldA))
                { DXMessageBox.Show("يرجى تعبئة النشاط التجاري"); return; }

                string nameE = reader["nameE"].ToString();
                if (string.IsNullOrWhiteSpace(nameE))
                { DXMessageBox.Show("يرجى تعبئة اسم المنشأة انجليزي"); return; }

                _CommonName             = $"{nameA}-{bsnNo}-{taxNo}";
                _OrganizationName       = nameA;
                _OrganizationIdentifier = taxNo;
                _CR_NO                  = bsnNo;
                _Industry               = fieldA;
                _OrganizationUnitName   = nameE;

                txtDate.DateTime               = DateTime.Now.Date;
                txtCommonName.Text             = _CommonName;
                txtOrganizationIdentifier.Text = _OrganizationIdentifier;
                txtOrganizationName.Text       = _OrganizationName;
                txtIndustry.Text               = _Industry;
                txtOrganizationUnitName.Text   = _OrganizationUnitName;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Load Data File

        private void btnLoadData_Click(object sender, RoutedEventArgs e)
        {
            ZatcaCredentialFromFile();
        }

        private void ZatcaCredentialFromFile()
        {
            try
            {
                if (MainClass.Conn_type != 1)
                {
                    DXMessageBox.Show("الرجاء عمل الإعدادات من الجهاز الرئيسي",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                string dir  = Path.GetDirectoryName(txtPath.Text) ?? "";
                string prod = Path.Combine(dir, "ProductionCsrResponse.txt");
                string comp = Path.Combine(dir, "ComplianceCsrResponse.txt");

                if (!File.Exists(prod))
                {
                    DXMessageBox.Show($"The Production CSR response file was not found at: {prod}",
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!File.Exists(comp))
                {
                    DXMessageBox.Show($"The Compliance CSR response file was not found at: {comp}",
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var prodCred = JsonConvert.DeserializeObject<CSRRequest>(File.ReadAllText(prod));
                var compCred = JsonConvert.DeserializeObject<CSRRequest>(File.ReadAllText(comp));

                if (prodCred == null || compCred == null)
                {
                    DXMessageBox.Show("Failed to parse CSR response files.",
                        "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                int count = Convert.ToInt32(
                    new SqlCommand("SELECT COUNT(*) FROM ZatcaCredential WHERE ID=1", conn)
                    .ExecuteScalar());

                SqlCommand cmd;
                if (count == 0)
                    cmd = new SqlCommand(
                        "INSERT INTO ZatcaCredential " +
                        "(ID,CSR,RequestID,CSID,Secret,P_RequestID,P_CSID,P_Secret) " +
                        "VALUES(1,@RequestID,@CSID,@Secret,@P_RequestID,@P_CSID,@P_Secret)", conn);
                else
                    cmd = new SqlCommand(
                        "UPDATE ZatcaCredential SET " +
                        "RequestID=@RequestID,CSID=@CSID,Secret=@Secret," +
                        "P_RequestID=@P_RequestID,P_CSID=@P_CSID,P_Secret=@P_Secret " +
                        "WHERE ID=1", conn);

                cmd.Parameters.Add("@RequestID",   SqlDbType.NVarChar).Value = compCred.RequestID;
                cmd.Parameters.Add("@CSID",        SqlDbType.NVarChar).Value = compCred.CSID;
                cmd.Parameters.Add("@Secret",      SqlDbType.NVarChar).Value = compCred.Secret;
                cmd.Parameters.Add("@P_RequestID", SqlDbType.NVarChar).Value = prodCred.RequestID;
                cmd.Parameters.Add("@P_CSID",      SqlDbType.NVarChar).Value = prodCred.CSID;
                cmd.Parameters.Add("@P_Secret",    SqlDbType.NVarChar).Value = prodCred.Secret;
                cmd.ExecuteNonQuery();

                if (conn.State != ConnectionState.Closed) conn.Close();
                DXMessageBox.Show("تم تحديث البيانات بنجاح", "Done!",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "Exception Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region OnOff + Renew

        private void GetzatcaOnproduction()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                int count = Convert.ToInt32(
                    new SqlCommand("SELECT COUNT(*) FROM ZatcaCredential WHERE ID=1", conn)
                    .ExecuteScalar());
                BtnOnOff.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void BtnOnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                bool isActive = false;
                using (var reader = new SqlCommand(
                    "SELECT IsActive FROM SettingZatca WHERE ID=1", conn).ExecuteReader())
                {
                    if (reader.Read())
                        isActive = Convert.ToBoolean(reader["IsActive"]);
                }

                if (!isActive)
                {
                    new SqlCommand("UPDATE SettingZatca SET IsActive=1", conn)
                        .ExecuteNonQuery();
                    DXMessageBox.Show("تم التشغيل بنجاح",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnOnOff.Content = "⏸ إيقاف الربط";
                }
                else
                {
                    new SqlCommand("UPDATE SettingZatca SET IsActive=0", conn)
                        .ExecuteNonQuery();
                    DXMessageBox.Show("تم الإيقاف بنجاح",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    BtnOnOff.Content = "▶ تشغيل";
                }
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void BtnRenewsCSID_Click(object sender, RoutedEventArgs e)
        {
            // منطق تجديد الشهادة
            DXMessageBox.Show("سيتم تجديد الشهادة - Renews CSID",
                "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}