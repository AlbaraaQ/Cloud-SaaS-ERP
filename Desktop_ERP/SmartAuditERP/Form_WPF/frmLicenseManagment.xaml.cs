using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using QLicense;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmLicenseManagment : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public SqlConnection conn;

        private int Code = -1;
        private int RowIndex = -1;

        private string DevicesInfUpdate;
        private string DevicesInfQry;
        private string UpdateLicenseInfQry;
        private string LicenseInfQry;

        private ObservableCollection<DeviceRow> _deviceRows;

        #endregion

        #region Constructor

        public frmLicenseManagment()
        {
            _deviceRows = new ObservableCollection<DeviceRow>();

            DevicesInfUpdate = @"update [dbo].[LicenseDevices]
                set [LicenseDate]=@LicenseDate
                ,[DeviceName]=@DeviceName
                ,[DeviceCode]=@DeviceCode
                ,[DeviceStatus]=@DeviceStatus
                ,[DeviceConnType]=@DeviceConnType
                ,[DeviceLicenseID]=@DeviceLicenseID
                ,IsDeleted=@IsDeleted
                where DeviceID=@DeviceID";

            DevicesInfQry = @"INSERT INTO [dbo].[LicenseDevices]
                ([licenseID],[DeviceID],[LicenseDate],[DeviceName],[DeviceCode],
                [DeviceStatus],[DeviceConnType],[DeviceLicenseID],IsDeleted)
                VALUES(@licenseID,@DeviceID,@LicenseDate,@DeviceName,@DeviceCode,
                @DeviceStatus,@DeviceConnType,@DeviceLicenseID,@IsDeleted)";

            UpdateLicenseInfQry = @"update [dbo].[License]
                set [LicenseDate]=@LicenseDate,[LicenseExpire]=@LicenseExpire,
                [ClientName]=@ClientName,[ClientEmail]=@ClientEmail,
                [LicenseStatus]=@LicenseStatus,[LicenseType]=@LicenseType,
                [LicenseEmp]=@LicenseEmp,[LiciensesNo]=@LiciensesNo,
                [ClientMobile]=@ClientMobile,[LicenseFeatures]=@LicenseFeatures,
                [Salesman]=@Salesman where licenseID=@licenseID";

            LicenseInfQry = @"INSERT INTO [dbo].[License]
                ([licenseID],[LicenseDate],[ClientName],[ClientEmail],[LicenseStatus],
                [LicenseExpire],[LicenseType],[LicenseEmp],[LiciensesNo],[ClientMobile],
                [LicenseFeatures],[bsn_no],[country],[Address],[city],[Salesman])
                VALUES(@licenseID,@LicenseDate,@ClientName,@ClientEmail,@LicenseStatus,
                @LicenseExpire,@LicenseType,@LicenseEmp,@LiciensesNo,@ClientMobile,
                @LicenseFeatures,@bsn_no,@country,@Address,@city,@Salesman)";

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmLicenseManagment_Load(object sender, RoutedEventArgs e)
        {
            try
            {
                txtLicenseDate.SelectedDate = DateTime.Today;
                txtDateExpire.SelectedDate = new DateTime(2050, 1, 1);

                LoadComboBoxes();
                dgvDevices.ItemsSource = _deviceRows;

                if (MainClass.Conn_type == 1)
                {
                    string connectionString = $"server={MainClass.Server.Trim()};database=master;trusted_connection=true";

                    if (MainClass.Conn_type == 1 && MainClass.UseServerAuth)
                        connectionString = $"server={MainClass.Server};database=master;user id={MainClass.NetUserId}; pwd={MainClass.NetPwd}";

                    conn = new SqlConnection(connectionString);
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();
                }
                else if (MainClass.Conn_type == 2)
                {
                    conn = new SqlConnection($"server={MainClass.Server};database=master;MultipleActiveResultSets=True;user id='{MainClass.NetUserId}'; pwd='{MainClass.NetPwd}'");
                    if (conn.State == System.Data.ConnectionState.Closed)
                        conn.Open();

                    btnSave.IsEnabled = false;
                    btnLicense.IsEnabled = false;
                }

                Thread workerThread = new Thread(LoadUID);
                workerThread.SetApartmentState(ApartmentState.STA);
                workerThread.IsBackground = true;
                workerThread.Start();

                LoadLicenseInfo();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadComboBoxes()
        {
            // للعمود Column4 - نوع الجهاز
            List<string> deviceTypes = new List<string> { "أساسي", "طرفي" };
            List<string> deviceStatuses = new List<string> { "نشط", "غير نشط" };

            colDeviceType.ItemsSource = deviceTypes;
            colDeviceStatus.ItemsSource = deviceStatuses;
        }

        #endregion

        #region Load License Info

        private void LoadLicenseInfo()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from License", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    Dispatcher.Invoke(() =>
                    {
                        txtClientName.Text = row["ClientName"]?.ToString() ?? "";
                        txtEmail.Text = row["ClientEmail"]?.ToString() ?? "";
                        txtMobile.Text = row["ClientMobile"]?.ToString() ?? "";
                        txtAddress.Text = row["Address"]?.ToString() ?? "";
                        txtBusnNo.Text = row["bsn_no"]?.ToString() ?? "";
                        txtCountry.Text = row["country"]?.ToString() ?? "";
                        txtCity.Text = row["City"]?.ToString() ?? "";
                        txtSalesMan.Text = row["Salesman"]?.ToString() ?? "";

                        bool licenseActive = row["LicenseStatus"] != DBNull.Value && Convert.ToBoolean(row["LicenseStatus"]);
                        txtStatus.Text = licenseActive ? "نشط" : "";

                        if (MainClass.Conn_type == 2)
                            txtUID.Text = row["licenseID"]?.ToString() ?? "";

                        string features = row["LicenseFeatures"]?.ToString() ?? "";
                        if (features == "000001")
                            cmbLicenseFeatures.SelectedIndex = 1;
                        else if (features == "110100")
                            cmbLicenseFeatures.SelectedIndex = 0;
                    });

                    // تحميل الأجهزة
                    SqlDataAdapter devAdapter = new SqlDataAdapter(
                        "select * from LicenseDevices where IsDeleted=0 order by LicenseDate asc", conn);
                    DataTable devDt = new DataTable();
                    devAdapter.Fill(devDt);

                    Dispatcher.Invoke(() =>
                    {
                        _deviceRows.Clear();
                        int rowNum = 0;

                        foreach (DataRow devRow in devDt.Rows)
                        {
                            rowNum++;
                            bool isActive = devRow["DeviceStatus"] != DBNull.Value && Convert.ToBoolean(devRow["DeviceStatus"]);
                            int connType = devRow["DeviceConnType"] != DBNull.Value ? Convert.ToInt32(devRow["DeviceConnType"]) : 1;
                            string deviceType = connType == 2 ? "طرفي" : "أساسي";
                            string deviceStatus = isActive ? "نشط" : "غير نشط";

                            _deviceRows.Add(new DeviceRow
                            {
                                Column5 = rowNum.ToString(),
                                Column2 = devRow["DeviceId"]?.ToString() ?? "",
                                Column1 = devRow["DeviceName"]?.ToString() ?? "",
                                Column4 = deviceType,
                                Column3 = deviceStatus
                            });
                        }
                    });

                    Code = 1;
                }
                else
                {
                    Code = -1;
                    Dispatcher.Invoke(() => txtNoDevices.IsEnabled = true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Load UID

        private void LoadUID()
        {
            try
            {
                int connType = MainClass.Conn_type == 2 ? 2 : 1;

                string uid = HardwareInfo.GenerateUID("SmartAuditERP", IncMac: true);

                Dispatcher.Invoke(() =>
                {
                    txtUID.Text = uid;
                });

                if (connType == 2)
                {
                    bool licensed = CheckDeviceLicense();
                    if (!licensed)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtNoDevices.IsEnabled = true;
                            txtNoDevices.IsReadOnly = false;
                        });
                    }
                    return;
                }

                string clientName = "";
                string clientEmail = "";
                GetLicenseClient(ref clientName, ref clientEmail);

                if (global::QLicense.License.CheckLicense(clientName, clientEmail))
                {
                    string licenseInfo = global::QLicense.License.ShowLicenseInfo();

                    Dispatcher.Invoke(() =>
                    {
                        SetRichTextContent(txtLicenseDetails, licenseInfo);
                        txtNoDevices.IsReadOnly = true;
                        txtNoDevices.Text = global::QLicense.License._lic.DevicesNo;
                        txtLicenseDate.SelectedDate = global::QLicense.License._lic.CreateDateTime;
                        txtDateExpire.SelectedDate = global::QLicense.License._lic.ExpireDate;

                        if (DateTime.Compare(global::QLicense.License._lic.ExpireDate, DateTime.Now) < 0)
                        {
                            txtStatus.Text = "منتهي";
                            txtNoDevices.IsReadOnly = false;
                            txtNoDevices.IsEnabled = true;
                        }
                        else
                        {
                            txtStatus.Text = "نشط";
                        }
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtNoDevices.IsReadOnly = false;
                        txtNoDevices.IsEnabled = true;
                        txtStatus.Text = "غير مرخص";
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private bool CheckDeviceLicense()
        {
            try
            {
                string uid = "";
                Dispatcher.Invoke(() => uid = txtUID.Text.Trim());

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select License.LicenseDate,License.LicenseExpire,License.LicenseType," +
                    "License.LicenseStatus,DeviceStatus,License.LicenseFeatures " +
                    "from License,LicenseDevices " +
                    "where License.licenseID=LicenseDevices.licenseID " +
                    "and License.LicenseStatus=1 and LicenseDevices.IsDeleted=0 " +
                    "and LicenseDevices.DeviceID=" + uid, conn);

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DateTime expireDate = Convert.ToDateTime(dt.Rows[0]["LicenseExpire"]);

                    if (DateTime.Compare(expireDate, DateTime.Now) < 0)
                    {
                        Dispatcher.Invoke(() => txtStatus.Text = "منتهي");
                        return false;
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtLicenseDate.SelectedDate = Convert.ToDateTime(dt.Rows[0]["LicenseDate"]);
                            txtDateExpire.SelectedDate = expireDate;
                            txtStatus.Text = "نشط";
                        });
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Save License

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (conn.State != System.Data.ConnectionState.Open)
                {
                    DXMessageBox.Show("السيرفر غير متصل", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CheckLicenseTables();
                SaveLicenseInf();
                ManagerOnline.CheckLicenseStatus();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SaveLicenseInf()
        {
            try
            {
                string confirmMsg = Code == -1
                    ? "يرجى التأكد من صحة اسم و إيميل العميل، لا تستطيع تعديله بعد عملية الترخيص"
                    : "هل أنت متأكد من حفظ بيانات الترخيص؟";

                frmAttentionMsg confirmForm = new frmAttentionMsg();
                confirmForm.lblmsg.Text = confirmMsg;
                confirmForm.btnInsure.IsEnabled = true;
                confirmForm.ShowDialog();

                if (!confirmForm.IsSure) return;

                if (!ValidateLicenseFields()) return;

                string licenseFeatures = cmbLicenseFeatures.SelectedIndex == 0 ? "110100" : "000001";

                if (MainClass.EmpNo == 0)
                    new SqlCommand("delete from License", conn).ExecuteNonQuery();

                SqlDataAdapter checkAdapter = new SqlDataAdapter("select * from License", conn);
                DataTable checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                if (checkDt.Rows.Count == 0)
                {
                    SqlCommand insertCmd = new SqlCommand(LicenseInfQry, conn);
                    insertCmd.Parameters.Add("@licenseID", SqlDbType.NVarChar).Value = txtUID.Text;
                    insertCmd.Parameters.Add("@LicenseDate", SqlDbType.DateTime).Value = txtLicenseDate.SelectedDate ?? DateTime.Today;
                    insertCmd.Parameters.Add("@LicenseExpire", SqlDbType.DateTime).Value = txtDateExpire.SelectedDate ?? new DateTime(2050, 1, 1);
                    insertCmd.Parameters.Add("@ClientName", SqlDbType.NVarChar).Value = txtClientName.Text;
                    insertCmd.Parameters.Add("@ClientEmail", SqlDbType.NVarChar).Value = txtEmail.Text;
                    insertCmd.Parameters.Add("@ClientMobile", SqlDbType.NVarChar).Value = txtMobile.Text;
                    insertCmd.Parameters.Add("@LicenseStatus", SqlDbType.Bit).Value = 0;
                    insertCmd.Parameters.Add("@LicenseType", SqlDbType.NVarChar).Value = " ";
                    insertCmd.Parameters.Add("@LicenseEmp", SqlDbType.NVarChar).Value = "";
                    insertCmd.Parameters.Add("@LiciensesNo", SqlDbType.Int).Value = ToIntSafe(txtNoDevices.Text);
                    insertCmd.Parameters.Add("@LicenseFeatures", SqlDbType.NVarChar).Value = licenseFeatures;
                    insertCmd.Parameters.Add("@country", SqlDbType.NVarChar).Value = txtCountry.Text;
                    insertCmd.Parameters.Add("@Address", SqlDbType.NVarChar).Value = txtAddress.Text;
                    insertCmd.Parameters.Add("@City", SqlDbType.NVarChar).Value = txtCity.Text;
                    insertCmd.Parameters.Add("@bsn_no", SqlDbType.NVarChar).Value = txtBusnNo.Text;
                    insertCmd.Parameters.Add("@Salesman", SqlDbType.NVarChar).Value = txtSalesMan.Text;
                    insertCmd.ExecuteNonQuery();

                    string deviceSql = DevicesInfQry;
                    if (MainClass.EmpNo == 0)
                    {
                        new SqlCommand($"delete from LicenseDevices where DeviceID='{txtUID.Text}'", conn).ExecuteNonQuery();
                    }

                    SqlCommand deviceCmd = new SqlCommand(deviceSql, conn);
                    deviceCmd.Parameters.Add("@licenseID", SqlDbType.NVarChar).Value = txtUID.Text;
                    deviceCmd.Parameters.Add("@DeviceID", SqlDbType.NVarChar).Value = txtUID.Text;
                    deviceCmd.Parameters.Add("@LicenseDate", SqlDbType.DateTime).Value = txtDateExpire.SelectedDate ?? DateTime.Today;
                    deviceCmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar).Value = "أساسي";
                    deviceCmd.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = 1;
                    deviceCmd.Parameters.Add("@DeviceStatus", SqlDbType.Bit).Value = 1;
                    deviceCmd.Parameters.Add("@DeviceConnType", SqlDbType.Int).Value = 1;
                    deviceCmd.Parameters.Add("@DeviceLicenseID", SqlDbType.NVarChar).Value = " ";
                    deviceCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
                    deviceCmd.ExecuteNonQuery();

                    Code = 1;
                    DXMessageBox.Show("تم حفظ بيانات الترخيص بنجاح", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private bool ValidateLicenseFields()
        {
            bool isArabic = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال اسم العميل" : "Enter company name", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClientName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                DXMessageBox.Show("يجب إدخال البريد الإلكتروني", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Common.IsValidEmailFormat(txtEmail.Text))
            {
                DXMessageBox.Show("البريد الإلكتروني غير صحيح", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMobile.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال رقم الجوال" : "Enter mobile", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMobile.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCountry.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال البلد" : "Enter your country", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCountry.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtCity.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال المدينة" : "Enter your city", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCity.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال العنوان" : "Enter company address", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAddress.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtBusnNo.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال رقم السجل" : "Enter business number", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtBusnNo.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSalesMan.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال اسم المندوب" : "Enter salesman name", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSalesMan.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNoDevices.Text))
            {
                DXMessageBox.Show(isArabic ? "يجب إدخال عدد الأجهزة" : "Enter number of devices", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNoDevices.Focus();
                return false;
            }

            if (cmbLicenseFeatures.SelectedIndex == -1)
            {
                DXMessageBox.Show("يرجى اختيار ميزات الترخيص", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        #endregion

        #region License Request

        private void btnLicense_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("يجب حفظ بيانات الترخيص أولًا", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtClientName.Text) ||
                string.IsNullOrWhiteSpace(txtNoDevices.Text) ||
                string.IsNullOrWhiteSpace(txtUID.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(txtMobile.Text) ||
                string.IsNullOrWhiteSpace(txtSalesMan.Text))
            {
                DXMessageBox.Show("يرجى إدخال جميع الحقول المطلوبة", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RequestLicense();
        }

        private void RequestLicense()
        {
            try
            {
                string licenseType = ToIntSafe(txtNoDevices.Text) != 1 ? "متعدد" : "عام";

                LicensingOrder order = new LicensingOrder
                {
                    ClientName = txtClientName.Text,
                    ClientEmail = txtEmail.Text,
                    ClientMobile = txtMobile.Text,
                    country = txtCountry.Text,
                    city = txtCity.Text,
                    Address = txtAddress.Text,
                    bsn_no = txtBusnNo.Text,
                    licenseCode = txtUID.Text,
                    LicenseType = licenseType,
                    LiciensesNo = ToIntSafe(txtNoDevices.Text),
                    LicenseDate = txtLicenseDate.SelectedDate ?? DateTime.Today,
                    LicenseExpire = txtDateExpire.SelectedDate ?? new DateTime(2050, 1, 1),
                    Salesman = txtSalesMan.Text
                };

                string contents = JsonConvert.SerializeObject(order);
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string filePath = Path.Combine(desktopPath, $"{txtClientName.Text}_طلب ترخيص.json");

                File.WriteAllText(filePath, contents, Encoding.UTF8);

                DXMessageBox.Show("تم حفظ طلب الترخيص في سطح المكتب", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                global::AuditorAPI.Models.License lic = new global::AuditorAPI.Models.License
                {
                    ClientName = txtClientName.Text,
                    ClientEmail = txtEmail.Text,
                    ClientMobile = txtMobile.Text,
                    country = txtCountry.Text,
                    city = txtCity.Text,
                    Address = txtAddress.Text,
                    bsn_no = txtBusnNo.Text,
                    licenseCode = txtUID.Text,
                    LicenseType = licenseType,
                    LiciensesNo = ToIntSafe(txtNoDevices.Text),
                    LicenseDate = txtLicenseDate.SelectedDate ?? DateTime.Today,
                    LicenseExpire = txtDateExpire.SelectedDate ?? new DateTime(2050, 1, 1),
                    Salesman = txtSalesMan.Text
                };

                lic.RequestLicense(lic);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Validate License

        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            string licText = GetRichTextContent(txtlicense).Trim();

            if (string.IsNullOrWhiteSpace(licText))
            {
                DXMessageBox.Show("Please input license", "",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            var Home = new Home();
            if (global::QLicense.License.ValidateLicense(licText))
            {
                string licPath = Path.Combine(
                    System.Windows.Forms.Application.StartupPath, "license.lic");
                File.WriteAllText(licPath, licText);

                DXMessageBox.Show("تم ترخيص البرنامج بنجاح يرجى إعادة تشغيل البرنامج", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadUID();
                UpdateLicenseInf(active: true);
                Home.CheckLicense();
            }
            else
            {
                DXMessageBox.Show("ترخيص غير صحيح", "ترخيص غير صحيح",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnUploadLicence_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog
                {
                    Filter = "License Files (*.Lic)|*.Lic"
                };

                if (openDialog.ShowDialog() != true) return;

                string clientName = "";
                string clientEmail = "";
                GetLicenseClient(ref clientName, ref clientEmail);
                var Home = new Home();
                if (global::QLicense.License.CheckLicenseFromPath(openDialog.FileName, clientName, clientEmail))
                {
                    string destPath = Path.Combine(
                        System.Windows.Forms.Application.StartupPath, "license.lic");
                    File.Copy(openDialog.FileName, destPath, overwrite: true);

                    DXMessageBox.Show("تم ترخيص البرنامج بنجاح يرجى إعادة تشغيل البرنامج", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadUID();
                    UpdateLicenseInf(active: true);
                    Home.CheckLicense();
                }
                else
                {
                    DXMessageBox.Show("ترخيص غير صحيح", "ترخيص غير صحيح",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void UpdateLicenseInf(bool active)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(UpdateLicenseInfQry, conn);
                cmd.Parameters.Add("@licenseID", SqlDbType.NVarChar).Value = txtUID.Text;
                cmd.Parameters.Add("@LicenseDate", SqlDbType.DateTime).Value = txtLicenseDate.SelectedDate ?? DateTime.Today;
                cmd.Parameters.Add("@LicenseExpire", SqlDbType.DateTime).Value = DateTime.Now.AddYears(10);
                cmd.Parameters.Add("@ClientName", SqlDbType.NVarChar).Value = txtClientName.Text;
                cmd.Parameters.Add("@ClientEmail", SqlDbType.NVarChar).Value = txtEmail.Text;
                cmd.Parameters.Add("@ClientMobile", SqlDbType.NVarChar).Value = txtMobile.Text;
                cmd.Parameters.Add("@LicenseStatus", SqlDbType.Bit).Value = active ? 1 : 0;

                string licType = rbPeriodic.IsChecked == true ? "مؤقت" : rbMulti.IsChecked == true ? "متعدد" : "عام";
                cmd.Parameters.Add("@LicenseType", SqlDbType.NVarChar).Value = licType;
                cmd.Parameters.Add("@LicenseEmp", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@LiciensesNo", SqlDbType.Int).Value = ToIntSafe(txtNoDevices.Text);
                cmd.Parameters.Add("@LicenseFeatures", SqlDbType.NVarChar).Value = global::QLicense.License.LicenseFeatures();
                cmd.Parameters.Add("@Salesman", SqlDbType.NVarChar).Value = txtSalesMan.Text;
                cmd.ExecuteNonQuery();

                new SqlCommand("delete from LicenseDevices", conn).ExecuteNonQuery();

                SqlCommand devCmd = new SqlCommand(DevicesInfQry, conn);
                devCmd.Parameters.Add("@licenseID", SqlDbType.NVarChar).Value = txtUID.Text;
                devCmd.Parameters.Add("@DeviceID", SqlDbType.NVarChar).Value = txtUID.Text;
                devCmd.Parameters.Add("@LicenseDate", SqlDbType.DateTime).Value = txtDateExpire.SelectedDate ?? DateTime.Today;
                devCmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar).Value = "أساسي";
                devCmd.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = 1;
                devCmd.Parameters.Add("@DeviceStatus", SqlDbType.Bit).Value = active ? 1 : 0;
                devCmd.Parameters.Add("@DeviceConnType", SqlDbType.Int).Value = 1;
                devCmd.Parameters.Add("@DeviceLicenseID", SqlDbType.NVarChar).Value = " ";
                devCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
                devCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Devices Grid

        private void dgvDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = dgvDevices.SelectedIndex;
            RowIndex = idx;
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            int maxDevices = ToIntSafe(txtNoDevices.Text);
            if (_deviceRows.Count < maxDevices)
            {
                _deviceRows.Add(new DeviceRow
                {
                    Column5 = (_deviceRows.Count + 1).ToString(),
                    Column2 = "",
                    Column1 = "",
                    Column4 = "طرفي",
                    Column3 = "نشط"
                });
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _deviceRows.Count) return;

                DeviceRow selected = _deviceRows[RowIndex];

                if (selected.Column2?.Trim() == txtUID.Text?.Trim())
                {
                    DXMessageBox.Show("لا يمكن حذف الجهاز الرئيسي", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من حذف الجهاز؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                new SqlCommand(
                    $"delete from LicenseDevices where DeviceID='{selected.Column2}'",
                    conn).ExecuteNonQuery();

                _deviceRows.RemoveAt(RowIndex);
                RowIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnSaveDeviceInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (conn.State != System.Data.ConnectionState.Open)
                {
                    DXMessageBox.Show("السيرفر غير متصل", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CheckLicenseTables();
                SaveDevicesInf();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SaveDevicesInf()
        {
            if (RowIndex == 0)
            {
                DXMessageBox.Show("لا يمكن تعديل بيانات الجهاز الرئيسي", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RowIndex > 0)
            {
                frmAttentionMsg confirmForm = new frmAttentionMsg();
                confirmForm.lblmsg.Text = "هل أنت متأكد من حفظ الجهاز المحدد؟";
                confirmForm.btnInsure.IsEnabled = true;
                confirmForm.ShowDialog();

                if (!confirmForm.IsSure) return;
            }

            UpdateDevicesInf(0);
        }

        private void UpdateDevicesInf(int isDeleted)
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _deviceRows.Count) return;

                DeviceRow selected = _deviceRows[RowIndex];

                if (string.IsNullOrWhiteSpace(selected.Column1))
                {
                    DXMessageBox.Show("يجب إدخال اسم الجهاز", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    $"select * from LicenseDevices where DeviceID='{selected.Column2}'", conn);
                DataTable checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                string deviceSql = checkDt.Rows.Count == 0 ? DevicesInfQry : DevicesInfUpdate;

                SqlCommand cmd = new SqlCommand(deviceSql, conn);
                cmd.Parameters.Add("@licenseID", SqlDbType.NVarChar).Value = txtUID.Text;
                cmd.Parameters.Add("@DeviceID", SqlDbType.NVarChar).Value = selected.Column2 ?? "";
                cmd.Parameters.Add("@LicenseDate", SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar).Value = selected.Column1 ?? "";
                cmd.Parameters.Add("@DeviceCode", SqlDbType.NVarChar).Value = selected.Column5 ?? "0";
                cmd.Parameters.Add("@DeviceStatus", SqlDbType.Bit).Value = selected.Column3 == "نشط" ? 1 : 0;
                cmd.Parameters.Add("@DeviceConnType", SqlDbType.Int).Value = selected.Column4 == "طرفي" ? 2 : 1;
                cmd.Parameters.Add("@DeviceLicenseID", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = isDeleted;
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم إضافة الجهاز بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Check License Tables

        private void CheckLicenseTables()
        {
            try
            {
                string createLicenseTable = @"
                    IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='License')
                    BEGIN
                    CREATE TABLE [dbo].[License](
                    [licenseID] [nvarchar](50) NOT NULL,
                    [LicenseDate] [datetime] NULL,
                    [ClientName] [nvarchar](150) NULL,
                    [ClientEmail] [nvarchar](50) NULL,
                    [LicenseStatus] [bit] NULL,
                    [LicenseExpire] [datetime] NULL,
                    [LicenseType] [nvarchar](50) NULL,
                    [LicenseEmp] [nvarchar](50) NULL,
                    [ClientMobile] [nvarchar](50) NULL,
                    [LicenseFeatures] [nvarchar](50) NULL,
                    [LiciensesNo] [int] NULL,
                    [bsn_no] [nvarchar](50) NULL,
                    [country] [nvarchar](50) NULL,
                    [Address] [nvarchar](100) NULL,
                    [city] [nvarchar](50) NULL,
                    [tax_no] [nvarchar](50) NULL,
                    [Salesman] [nvarchar](100) NULL,
                    CONSTRAINT [PK_License] PRIMARY KEY CLUSTERED ([licenseID] ASC))
                    END";

                new SqlCommand(createLicenseTable, conn).ExecuteNonQuery();

                new SqlCommand(@"
                    If Not EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='License' AND COLUMN_NAME='country')
                    BEGIN
                    ALTER TABLE License Add country [nvarchar](100) NULL
                    ALTER TABLE License Add bsn_no [nvarchar](100) NULL
                    ALTER TABLE License Add city [nvarchar](100) NULL
                    ALTER TABLE License Add Address [nvarchar](100) NULL
                    ALTER TABLE License Add tax_no [nvarchar](100) NULL
                    End", conn).ExecuteNonQuery();

                new SqlCommand(@"
                    If Not EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='License' AND COLUMN_NAME='Salesman')
                    BEGIN ALTER TABLE License Add Salesman [nvarchar](100) NULL End",
                    conn).ExecuteNonQuery();

                new SqlCommand(@"
                    IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='LicenseDevices')
                    BEGIN
                    CREATE TABLE [dbo].[LicenseDevices](
                    [licenseID] [nvarchar](50),
                    [DeviceID] [nvarchar](50) NOT NULL,
                    [LicenseDate] [datetime] NULL,
                    [DeviceName] [nvarchar](50) NULL,
                    [DeviceCode] [nvarchar](50) NULL,
                    [DeviceStatus] [bit] NULL,
                    [DeviceConnType] [int] NULL,
                    [DeviceLicenseID] [nvarchar](50) NULL,
                    [IsDeleted] [bit] NULL,
                    CONSTRAINT [PK_LicenseDevices] PRIMARY KEY CLUSTERED ([DeviceID] ASC))
                    END", conn).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Close / UID KeyDown

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtUID_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                txtUID.Text = HardwareInfo.GenerateUID("SmartAuditERP", IncMac: false);

            if (e.Key == Key.F12)
                txtUID.Text = HardwareInfo.GenerateUID("SmartAuditERP", IncMac: true);
        }

        #endregion

        #region GetLicenseClient

        public void GetLicenseClient(ref string clientName, ref string clientEmail)
        {
            try
            {
                string connectionString = "";

                if (MainClass.Conn_type == 1)
                {
                    connectionString = $"server={MainClass.Server.Trim()};database=master;trusted_connection=true";
                    if (MainClass.UseServerAuth)
                        connectionString = $"server={MainClass.Server};database=master;user id={MainClass.NetUserId}; pwd={MainClass.NetPwd}";
                }
                else if (MainClass.Conn_type == 2)
                {
                    connectionString = $"server={MainClass.Server};database=master;MultipleActiveResultSets=true;user id={MainClass.NetUserId}; pwd={MainClass.NetPwd}";
                }

                SqlConnection tempConn = new SqlConnection(connectionString);
                if (tempConn.State != System.Data.ConnectionState.Open)
                    tempConn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter("select * from License", tempConn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    clientName = dt.Rows[0]["ClientName"].ToString().Trim();
                    clientEmail = dt.Rows[0]["ClientEmail"].ToString().Trim();
                }
                else
                {
                    clientName = "";
                    clientEmail = "";
                }
            }
            catch
            {
                clientName = "";
                clientEmail = "";
            }
        }

        #endregion

        #region Helpers

        private int ToIntSafe(string text)
        {
            int.TryParse(text?.Trim(), out int result);
            return result;
        }

        private void SetRichTextContent(RichTextBox rtb, string text)
        {
            rtb.Document.Blocks.Clear();
            rtb.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        private string GetRichTextContent(RichTextBox rtb)
        {
            return new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class DeviceRow
    {
        public string Column5 { get; set; }
        public string Column2 { get; set; }
        public string Column1 { get; set; }
        public string Column4 { get; set; }
        public string Column3 { get; set; }
    }
}