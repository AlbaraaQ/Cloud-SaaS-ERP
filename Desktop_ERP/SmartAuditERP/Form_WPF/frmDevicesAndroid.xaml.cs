using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Xpf.Core;
using Newtonsoft.Json;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDevicesAndroid : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private DataTable _devicesTable;

        #endregion

        #region Constructor

        public frmDevicesAndroid()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();

            Loaded += FrmDevicesAndroid_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDevicesAndroid_Loaded(object sender, RoutedEventArgs e)
        {
            BtnGetDevice.Click += BtnGetDevice_Click;
            UpdateStatusBar("جاهز - اضغط 'عرض الأجهزة' لتحميل البيانات");
        }

        #endregion

        #region Load Devices

        private async void BtnGetDevice_Click(object sender, RoutedEventArgs e)
        {
            await LoadDevicesAsync();
        }

        private async Task LoadDevicesAsync()
        {
            try
            {
                BtnGetDevice.IsEnabled = false;
                txtLoadingMsg.Text = "⏳ جاري تحميل البيانات...";
                UpdateStatusBar("جاري تحميل الأجهزة المتصلة...");

                // ✅ IsActive كـ string مباشرة بدون Converter
                _devicesTable = new DataTable();
                _devicesTable.Columns.Add("id", typeof(int));
                _devicesTable.Columns.Add("UserId", typeof(int));
                _devicesTable.Columns.Add("UserName", typeof(string));
                _devicesTable.Columns.Add("ClientCode", typeof(string));
                _devicesTable.Columns.Add("DeviceId", typeof(string));
                _devicesTable.Columns.Add("DeviceName", typeof(string));
                _devicesTable.Columns.Add("CreatedAt", typeof(DateTime));
                _devicesTable.Columns.Add("IsActive", typeof(string)); // ✅ string

                using var client = new HttpClient();

                var response = await client.GetAsync(
                    $"{Sync.APIUrl}/api/user/devices" +
                    $"?clientCode={Sync.ClientCode}&userId={MainClass.EmpNo}");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var devices = JsonConvert.DeserializeObject<List<UserDevice>>(json);

                    int rowNumber = 0;

                    foreach (var device in devices)
                    {
                        rowNumber++;

                        // ✅ تحويل bool إلى نص هنا مباشرة
                        string statusText = device.IsActive
                            ? "✅ نشط"
                            : "❌ غير نشط";

                        _devicesTable.Rows.Add(
                            rowNumber,
                            device.UserId,
                            GetUsernameById(device.UserId),
                            device.ClientCode,
                            device.DeviceId,
                            device.DeviceName,
                            device.CreatedAt,
                            statusText);
                    }

                    GridControl1.ItemsSource = _devicesTable.DefaultView;

                    int count = _devicesTable.Rows.Count;
                    txtDeviceCount.Text = count.ToString();
                    txtLoadingMsg.Text = $"✅ تم تحميل {count} جهاز";
                    UpdateStatusBar($"تم تحميل {count} جهاز بنجاح");
                }
                else
                {
                    DXMessageBox.Show(
                        "❌ فشل تحميل الأجهزة",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    txtLoadingMsg.Text = "❌ فشل التحميل";
                    UpdateStatusBar("فشل تحميل البيانات");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ أثناء التحميل:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                txtLoadingMsg.Text = "❌ خطأ في التحميل";
                UpdateStatusBar($"خطأ: {ex.Message}");
            }
            finally
            {
                BtnGetDevice.IsEnabled = true;
            }
        }

        #endregion

        #region Unlink Device

        private async void BtnUnlink_Click(object sender, RoutedEventArgs e)
        {
            if (GridControl1.CurrentItem is not DataRowView rowView)
                return;

            // قراءة بيانات الصف المحدد
            var device = new UserDevice
            {
                Id = Convert.ToInt32(rowView["id"]),
                UserId = Convert.ToInt32(rowView["UserId"]),
                UserName = rowView["UserName"]?.ToString() ?? string.Empty,
                ClientCode = rowView["ClientCode"]?.ToString() ?? string.Empty,
                DeviceId = rowView["DeviceId"]?.ToString() ?? string.Empty,
                DeviceName = rowView["DeviceName"]?.ToString() ?? string.Empty,
                CreatedAt = Convert.ToDateTime(rowView["CreatedAt"]),
                IsActive = rowView["IsActive"]?.ToString() == "✅ نشط"
            };

            // تأكيد الحذف
            var result = DXMessageBox.Show(
                $"⚠️ هل تريد إلغاء ربط الجهاز '{device.DeviceName}' للمستخدم {device.UserName}؟",
                "تأكيد إلغاء الربط",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            await UnlinkDeviceAsync(device.Id, device.UserId, device.ClientCode);
            await SendLastInvoice(device.UserId, device.ClientCode);
            await LoadDevicesAsync();
        }

        private async Task UnlinkDeviceAsync(
            int deviceId, int userId, string clientCode)
        {
            try
            {
                UpdateStatusBar("جاري إلغاء ربط الجهاز...");

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(new
                {
                    userId,
                    clientCode
                });

                var response = await client.PostAsync(
                    $"{Sync.APIUrl}/api/user/remove-device",
                    new StringContent(
                        payload,
                        Encoding.UTF8,
                        "application/json"));

                string responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseDict =
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            responseText);

                    string message =
                        responseDict?.ContainsKey("message") == true
                            ? responseDict["message"].ToString()
                            : "✅ تم إلغاء ربط الجهاز بنجاح";

                    DXMessageBox.Show(
                        message,
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    UpdateStatusBar("✅ تم إلغاء ربط الجهاز بنجاح");
                }
                else
                {
                    DXMessageBox.Show(
                        $"❌ فشل إلغاء الربط:\n{responseText}",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    UpdateStatusBar("❌ فشل إلغاء ربط الجهاز");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                UpdateStatusBar($"خطأ: {ex.Message}");
            }
        }

        #endregion

        #region Send Last Invoice

        public static async Task SendLastInvoice(int userId, string clientCode)
        {
            try
            {
                int lastInvoiceNo = 0;

                using (var conn = MainClass.ConnObj())
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    using var cmd = new SqlCommand(
                        "SELECT ISNULL(MAX(id), 0) " +
                        "FROM Inv " +
                        "WHERE inv_type=20 AND proc_type=1 " +
                        "AND sales_emp=@sales_emp AND IS_Deleted=0",
                        conn);

                    cmd.Parameters.AddWithValue("@sales_emp", userId);
                    lastInvoiceNo = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using var client = new HttpClient();

                var payload = JsonConvert.SerializeObject(new
                {
                    userId,
                    clientCode,
                    lastInvoiceNo,
                    invType = 1
                });

                var response = await client.PostAsync(
                    $"{Sync.APIUrl}/api/Invoice/SetLastInvoice/set-last-invoice",
                    new StringContent(
                        payload,
                        Encoding.UTF8,
                        "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    DXMessageBox.Show(
                        $"✅ تم إرسال آخر فاتورة بنجاح: {lastInvoiceNo}",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    DXMessageBox.Show(
                        "❌ فشل إرسال آخر فاتورة",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// يُعيد اسم المستخدم بناءً على رقم الموظف
        /// </summary>
        private string GetUsernameById(int employeeId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT username FROM Users WHERE emp={employeeId}",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0]["username"].ToString()
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// تحديث شريط الحالة
        /// </summary>
        private void UpdateStatusBar(string message)
        {
            txtStatusBar.Text = message;
        }

        #endregion
    }
}