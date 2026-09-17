using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AuditorAPI.Models;
using Newtonsoft.Json;
using QLicense;
using Ws_Auditor;

namespace SmartAuditERP
{
    public class Sync
    {
        #region Static Connection
        // ✅ إصلاح: استخدام factory method بدلاً من connection ثابت
        // لأن الـ connection الثابت يُغلق بعد أول استخدام ويسبب NullRef
        private static SqlConnection GetConnection() => MainClass.ConnObj();

        // ✅ للتوافق مع الكود القديم الذي يستخدم conn مباشرة
        private static SqlConnection conn
        {
            get
            {
                var c = MainClass.ConnObj();
                if (c == null)
                    throw new InvalidOperationException(
                        "تعذّر إنشاء اتصال بقاعدة البيانات");
                return c;
            }
        }
        #endregion

        #region Properties

        public static int ID { get; set; }
        public static string ClientCode { get; set; } = "";
        public static string APIUrl { get; set; } = "";
        public static string UserName { get; set; } = "";
        public static string Password { get; set; } = "";
        public static int BranchId { get; set; } = 1;
        public static int BranchType { get; set; } = 1;
        public static int SyncType { get; set; } = 0;
        public static double ReadInterval { get; set; }
        public static double PostInterval { get; set; }
        public static bool ActiveSync { get; set; } = false;
        public static bool ValidAPIUrl { get; set; } = false;
        public static int DistBranch { get; set; } = 0;

        public static DateTime CardsLastSync { get; set; } = DateTime.Now;
        public static DateTime InvoicesLastSync { get; set; } = DateTime.Now;

        public static bool SyncQuantity { get; set; } = false;
        public static int InvCounttoSync { get; set; } = 0;
        public static bool SyncCloudFirst { get; set; } = false;
        public static bool BranchSpecialSalePrice { get; set; } = false;

        #endregion

        #region CheckSync

        public static bool CheckSync(int bId)
        {
            if (bId <= -1) return false;

            try
            {
                // ✅ إصلاح: إنشاء connection جديد لكل استدعاء
                using var sqlConn = MainClass.ConnObj();
                if (sqlConn == null) return false;

                var adapter = new SqlDataAdapter("SELECT * FROM SettingSync", sqlConn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1)
                {
                    SyncType = 0;
                    ActiveSync = false;
                    ValidAPIUrl = false;
                    DistBranch = 0;
                    return false;
                }

                DataRow row = dt.Rows[0];

                ClientCode = SafeString(row, "ClientCode");
                APIUrl = SafeString(row, "APIUrl");

                // ✅ إصلاح: مطابق للأصل في استخدام Application.StartupPath
                // مع توافق WPF عبر AppDomain
                string apiFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data", "ApiPath.txt");

                if (File.Exists(apiFilePath))
                {
                    string fileContent = File.ReadAllText(apiFilePath);
                    if (!string.IsNullOrWhiteSpace(fileContent))
                        APIUrl = fileContent.Trim();
                }

                UserName = SafeString(row, "Username");
                Password = SafeString(row, "Password");
                ReadInterval = SafeDouble(row, "ReadingInterval");
                PostInterval = SafeDouble(row, "POSTInterval");
                BranchType = SafeInt(row, "BranchType");
                SyncType = SafeInt(row, "SyncType");
                BranchId = SafeInt(row, "BranchId");
                DistBranch = SafeInt(row, "DistBranch");

                SyncQuantity = SafeBool(row, "SyncQuantity");

                CardsLastSync = row["CardsLastSync"] != DBNull.Value
                    ? Convert.ToDateTime(row["CardsLastSync"])
                    : DateTime.Now;

                InvoicesLastSync = row["InvoicesLastSync"] != DBNull.Value
                    ? Convert.ToDateTime(row["InvoicesLastSync"])
                    : DateTime.Now;

                InvCounttoSync = row["InvCountToSync"] != DBNull.Value
                    ? SafeInt(row, "InvCountToSync")
                    : 0;

                SyncCloudFirst = row["SyncCloudFirst"] != DBNull.Value
                    && SafeBool(row, "SyncCloudFirst");

                BranchSpecialSalePrice = row["BranchSpecialSalePrice"] != DBNull.Value
                    && SafeBool(row, "BranchSpecialSalePrice");

                ActiveSync = SyncType > 0;
                return ActiveSync;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في CheckSync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region CheckBranchSync

        public static bool CheckBranchSync()
        {
            try
            {
                // ✅ إصلاح: التحقق من MainClass.conn قبل الاستخدام
                if (MainClass.conn == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "CheckBranchSync: MainClass.conn = null");
                    return false;
                }

                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingSync", MainClass.conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return false;

                DataRow row = dt.Rows[0];

                ClientCode = SafeString(row, "ClientCode");
                APIUrl = SafeString(row, "APIUrl");
                UserName = SafeString(row, "Username");
                Password = SafeString(row, "Password");
                ReadInterval = SafeDouble(row, "ReadingInterval");
                PostInterval = SafeDouble(row, "POSTInterval");
                BranchType = SafeInt(row, "BranchType");
                SyncType = SafeInt(row, "SyncType");
                BranchId = SafeInt(row, "BranchId");
                DistBranch = SafeInt(row, "DistBranch");

                CardsLastSync = row["CardsLastSync"] != DBNull.Value
                    ? Convert.ToDateTime(row["CardsLastSync"])
                    : DateTime.Now;

                InvoicesLastSync = row["InvoicesLastSync"] != DBNull.Value
                    ? Convert.ToDateTime(row["InvoicesLastSync"])
                    : DateTime.Now;

                // ✅ مطابق للأصل: القيمة الافتراضية 10
                InvCounttoSync =
                    !string.IsNullOrWhiteSpace(row["InvCountToSync"]?.ToString())
                        ? SafeInt(row, "InvCountToSync")
                        : 10;

                ActiveSync = SyncType > 0;
                return ActiveSync;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في CheckBranchSync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ISValidAPIUrlAsync

        public static async Task<bool> ISValidAPIUrlAsync()
        {
            // ✅ مطابق للأصل
            if (BranchType == 4) return true;

            try
            {
                // ✅ إصلاح: التحقق من GetLicenseClient قبل الاستخدام
                LicensingOrder licenseClient = Common.GetLicenseClient();
                if (licenseClient == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "ISValidAPIUrlAsync: licenseClient = null");
                    return false;
                }

                var apiClient = new APIClient(APIUrl)
                {
                    licenseCode = licenseClient.licenseCode,
                    ClientCode = ClientCode,
                    StartDate = DateTime.Now,
                    ClientName = licenseClient.ClientName,
                    SyncStatus = false,
                    SyncExpire = DateTime.Now.AddYears(1),
                    SyncType = "1",
                    BranchesNo = 2,
                    DevicesNo = 2
                };

                // ✅ مطابق للأصل: apiClient2 = apiClient ثم CheckClientCode
                var apiClient2 = apiClient;
                var checkedClient = await apiClient.CheckClientCode(apiClient);

                if (checkedClient != null)
                    return checkedClient.SyncStatus;

                // ✅ مطابق للأصل: GetClientCode في حالة الفشل
                var fetchedClient = await apiClient2.GetClientCode(apiClient2);
                return fetchedClient?.SyncStatus ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في ISValidAPIUrlAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ReadCloudDataManully

        public static async Task<bool> ReadCloudDataManully()
        {
            try
            {
                var syncOperation = new SyncOperation();
                // ✅ مطابق للأصل
                new Thread(() => syncOperation.ReadCardsDataCloud()).Start();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في ReadCloudDataManully: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ReadDataPeriodically

        public static async Task<bool> ReadDataPeriodically()
        {
            try
            {
                var invoiceCRUD = new InvoiceCRUD(APIUrl);
                var entryCRUD = new EntryCRUD(APIUrl);
                var receiptCRUD = new ReceiptCRUD(APIUrl);
                var invoiceOper = new InvoiceOper();
                var entryOper = new EntryOper();
                var receiptOper = new ReceiptOper();
                var entityOps = new EntityOperations();

                // ✅ إصلاح: التحقق من النتائج قبل التكرار
                var invoices = (List<Invoice>)
                    await invoiceCRUD.GetInvoices(ClientCode, BranchId, BranchType)
                    ?? new List<Invoice>();

                var entries = await entryCRUD.GetEntries(
                    ClientCode, BranchId, BranchType)
                    ?? new List<Entry>();

                var receipts = (List<Receipt>)
                    await receiptCRUD.GetReceipts(ClientCode, BranchId, BranchType)
                    ?? new List<Receipt>();

                // ── معالجة الفواتير ────────────────────────────────
                foreach (var invoice in invoices)
                {
                    // ✅ إصلاح: التحقق من null قبل المقارنة
                    if (invoice == null) continue;
                    if (invoice.ClientCode != ClientCode) continue;
                    if (invoice.DistBranch != BranchId) continue;

                    try
                    {
                        var customerCRUD = new CustomerCRUD(APIUrl);

                        if (invoice.InvoiceType == (InvoiceType)20)
                        {
                            var customers = (List<AuditorAPI.Models.Customer>)
                                await customerCRUD.GetCustomersByID(
                                    ClientCode, invoice.Customer)
                                ?? new List<AuditorAPI.Models.Customer>();

                            if (customers.Count > 0)
                            {
                                entityOps.SaveCustomer(customers,
                                    AddedLocally: false);
                                await SendLastCodeToApiAsync(1, ClientCode);
                            }
                        }

                        invoiceOper.ReadInvoiceOnline(invoice);

                        if (BranchType == 3)
                        {
                            invoice.DistBranch = DistBranch;
                            invoice.BranchType = BranchType;
                            invoice.Received = false;
                        }
                        else
                        {
                            invoice.Received = true;
                        }

                        if (invoice.InvoiceType == (InvoiceType)20)
                        {
                            await invoiceCRUD.UpdateInvoiceAsyncreceived(
                                invoice.InvGlobalID, invoice.ClientCode);
                            await customerCRUD.DeleteCustomerByid(
                                invoice.Customer.ToString(), invoice.ClientCode);
                        }
                        else
                        {
                            invoiceCRUD.UpdateInvoice(invoice.InvGlobalID, invoice);
                        }

                        if (invoice.InvoiceType == (InvoiceType)20
                            && invoice.ZatcaSent)
                        {
                            await invoiceCRUD.SendQrCode(
                                invoice.InvGlobalID, invoice.ClientCode,
                                invoice.QRCode, invoice.ZatcaSent);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"خطأ في معالجة فاتورة {invoice.InvGlobalID}: {ex.Message}");
                    }
                }

                // ── معالجة القيود ──────────────────────────────────
                foreach (var entry in entries)
                {
                    if (entry == null) continue;
                    if (entry.ClientCode != ClientCode) continue;
                    if (entry.DistBranch != BranchId) continue;

                    try
                    {
                        entryOper.ReadEntyOnline(entry);

                        if (BranchType == 3)
                        {
                            entry.DistBranch = DistBranch;
                            entry.BranchType = BranchType;
                            entry.Received = false;
                        }
                        else
                        {
                            entry.Received = true;
                        }

                        entryCRUD.UpdateEntry(entry.EntryGlobalID, entry);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"خطأ في معالجة قيد {entry.EntryGlobalID}: {ex.Message}");
                    }
                }

                // ── معالجة الإيصالات ───────────────────────────────
                foreach (var receipt in receipts)
                {
                    if (receipt == null) continue;

                    try
                    {
                        // ✅ إصلاح: مطابق للأصل في المقارنة
                        bool matchClient = string.Equals(
                            receipt.ClientCode, ClientCode,
                            StringComparison.Ordinal);

                        bool matchBranch = receipt.DistBranch.HasValue
                            && receipt.DistBranch.Value == BranchId;

                        if (!matchClient || !matchBranch) continue;

                        // ✅ إصلاح DebitAcc الفارغ
                        if (string.IsNullOrWhiteSpace(receipt.DebitAcc)
                            && receipt.TreasuryID.HasValue)
                        {
                            int accCode = GenerateAccCodeById(
                                receipt.TreasuryID.Value);
                            if (accCode > 0)
                                receipt.DebitAcc = accCode.ToString();
                        }

                        receiptOper.ReadReceiptOnline(receipt);

                        if (BranchType == 3)
                        {
                            receipt.DistBranch = DistBranch;
                            receipt.BranchType = BranchType;
                            receipt.Received = false;
                        }
                        else
                        {
                            receipt.Received = true;
                        }

                        await receiptCRUD.UpdateReceiptAsyncreceived(
                            receipt.GlobalID, receipt.ClientCode);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"خطأ في معالجة إيصال {receipt.GlobalID}: {ex.Message}");
                    }
                }

                // ── تقارير الإغلاق ─────────────────────────────────
                try
                {
                    var closingReport = new ClosingReport();
                    var closings = await closingReport.FetchCasherClosedsFromApi(
                        ClientCode);
                    if (closings != null)
                        closingReport.SaveCasherClosedsToLocal(closings);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"خطأ في ClosingReport: {ex.Message}");
                }

                // ── تنظيف السحابة ──────────────────────────────────
                if (BranchType == 1)
                {
                    invoiceCRUD.RemoveReceivedInvoice(ClientCode);
                    entryCRUD.RemoveReceivedEntry(ClientCode);
                    receiptCRUD.RemoveReceivedReceipt(ClientCode);
                }

                // ── مزامنة البيانات الرئيسية ───────────────────────
                // ✅ إصلاح: مطابق للأصل في المقارنة (AddHours(2) >= Now)
                if (DateTime.Compare(
                        CardsLastSync.AddHours(2.0), DateTime.Now) >= 0)
                {
                    var syncOp = new SyncOperation();
                    new Thread(() => syncOp.ReadCardsDataOnline()).Start();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في ReadDataPeriodically: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region GenerateAccCodeById

        public static int GenerateAccCodeById(int id)
        {
            try
            {
                // ✅ إصلاح: التحقق من connstr
                if (string.IsNullOrWhiteSpace(MainClass.connstr))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "GenerateAccCodeById: connstr فارغة");
                    return 0;
                }

                using var sqlConnection = new SqlConnection(MainClass.connstr);
                sqlConnection.Open();

                using var cmd = new SqlCommand(
                    "SELECT Acc_code FROM Stocks WHERE id = @id",
                    sqlConnection);
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في GenerateAccCodeById: {ex.Message}");
            }
            return 0;
        }

        #endregion

        #region PostDataPeriodically

        public static async Task<bool> PostDataPeriodically()
        {
            try
            {
                await SendLastCodeToApiAsync(1, ClientCode);

                var invoiceCRUD = new InvoiceCRUD(APIUrl);
                var entryCRUD = new EntryCRUD(APIUrl);

                var invoices = (List<Invoice>)
                    await invoiceCRUD.PostInvoicesPeriodically()
                    ?? new List<Invoice>();

                var entries = (List<Entry>)
                    await entryCRUD.PostEntriesPeriodically()
                    ?? new List<Entry>();

                // ✅ إصلاح: إنشاء connection جديد لكل استدعاء
                using var localConn = MainClass.ConnObj();
                if (localConn == null) return false;

                if (localConn.State != ConnectionState.Open)
                    localConn.Open();

                foreach (var invoice in invoices)
                {
                    if (invoice == null) continue;

                    new SqlCommand(
                        "UPDATE Inv SET Sync=1 WHERE InvGlobalID=N'" +
                        invoice.InvGlobalID + "'",
                        localConn).ExecuteNonQuery();
                }

                foreach (var entry in entries)
                {
                    if (entry == null) continue;

                    new SqlCommand(
                        "UPDATE Entry SET Sync=1 WHERE GlobalID=N'" +
                        entry.EntryGlobalID + "'",
                        localConn).ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في PostDataPeriodically: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region PostDataPeriodicallyCloud

        public static async Task<bool> PostDataPeriodicallyCloud()
        {
            // ✅ إصلاح: تهيئة القيمة الافتراضية
            bool result = false;

            try
            {
                if (!MainClass.CheckForInternetConnection())
                    return false;

                var invoiceCRUD = new InvoiceCRUD(APIUrl);
                var allInvoices = invoiceCRUD.PostInvoicesPeriodicallyCloud()
                                  ?? new List<Invoice>();

                if (allInvoices.Count == 0)
                    return false;

                List<Invoice> toSend;
                List<Invoice> remaining;

                if (InvCounttoSync < allInvoices.Count)
                {
                    toSend = allInvoices
                        .OrderBy(x => x.InvDate)
                        .Take(InvCounttoSync)
                        .ToList();
                    remaining = allInvoices
                        .OrderBy(x => x.InvDate)
                        .Skip(InvCounttoSync)
                        .ToList();
                }
                else
                {
                    toSend = allInvoices.ToList();
                    remaining = new List<Invoice>();
                }

                // ✅ إصلاح: التحقق من CurrentCloudUser قبل الاستخدام
                if (User.CurrentCloudUser == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "PostDataPeriodicallyCloud: CurrentCloudUser = null");
                    return false;
                }

                var clsInvoice = new ClsInvoice(APIUrl, "");
                var paymentTypes = InvoiceOper.GetInvoiceCloudPaymentType()
                                   ?? new List<PaymentType>();

                var saveResult = await clsInvoice.Save(
                    invoices: toSend,
                    paymentTypes: paymentTypes,
                    currentUser: User.CurrentCloudUser);

                if (saveResult?.IsValid == true)
                {
                    // ✅ إصلاح: connection جديد لكل استدعاء
                    using var localConn = MainClass.ConnObj();
                    if (localConn != null)
                    {
                        if (localConn.State != ConnectionState.Open)
                            localConn.Open();

                        foreach (var invoice in toSend)
                        {
                            if (invoice == null) continue;
                            new SqlCommand(
                                "UPDATE Inv SET Sync=1 WHERE InvGlobalID=N'" +
                                invoice.InvGlobalID + "'",
                                localConn).ExecuteNonQuery();
                        }

                        new SqlCommand(
                            "UPDATE SettingSync SET InvoicesLastSync=N'" +
                            DateTime.Now + "'",
                            localConn).ExecuteNonQuery();
                    }

                    result = true;
                }

                InvoicesLastSync = DateTime.Now;
                invoiceCRUD.CLearfiles();

                if (remaining.Count > 0)
                    invoiceCRUD.AddinvoicesLocally(remaining, IsNew: false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في PostDataPeriodicallyCloud: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region SendLastInvoice (Private)

        private async Task SendLastInvoice(int userId, string clientCode)
        {
            try
            {
                int lastInvNo = 0;

                // ✅ إصلاح: using يضمن إغلاق الاتصال
                using (var sqlConnection = MainClass.ConnObj())
                {
                    if (sqlConnection == null)
                        throw new InvalidOperationException("لا يمكن فتح الاتصال");

                    if (sqlConnection.State != ConnectionState.Open)
                        sqlConnection.Open();

                    using var cmd = new SqlCommand(
                        @"SELECT ISNULL(MAX(id), 0) FROM Inv
                          WHERE inv_type  = 20
                            AND proc_type = 1
                            AND sales_emp = @sales_emp
                            AND IS_Deleted = 0",
                        sqlConnection);

                    cmd.Parameters.AddWithValue("@sales_emp", userId);
                    lastInvNo = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (string.IsNullOrWhiteSpace(APIUrl))
                {
                    MessageBox.Show("APIUrl غير مهيأ", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var payload = new
                {
                    userId,
                    clientCode,
                    lastInvoiceNo = lastInvNo
                };

                using var httpClient = new HttpClient();
                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"{APIUrl}/api/Invoice/SetLastInvoice/set-last-invoice",
                    content);

                string msg = response.IsSuccessStatusCode
                    ? "تم إرسال آخر فاتورة بنجاح: " + lastInvNo
                    : "فشل إرسال آخر فاتورة";

                MessageBox.Show(msg, "معلومة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region SendLastCodeToApiAsync

        public static async Task<bool> SendLastCodeToApiAsync(
            int type, string clientCode)
        {
            try
            {
                // ✅ إصلاح: التحقق من CurrentBranch قبل الاستخدام
                if (Common.CurrentBranch == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "SendLastCodeToApiAsync: CurrentBranch = null");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(APIUrl))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "SendLastCodeToApiAsync: APIUrl فارغ");
                    return false;
                }

                string lastCustomersNo = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.CustomersAcc));

                var payload = new
                {
                    type,
                    clientCode,
                    LastCustomersNo = lastCustomersNo
                };

                using var httpClient = new HttpClient();
                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8, "application/json");

                await httpClient.PostAsync(
                    $"{APIUrl}/api/Customers/set-last-customers",
                    content);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في SendLastCodeToApiAsync: {ex.Message}");
                // ✅ لا نعرض MessageBox هنا لأنها تُستدعى بشكل دوري
                return false;
            }
        }

        #endregion

        #region GetQrcodezatca

        // ✅ إصلاح: استخدام Parameterized Query بدلاً من String Concatenation
        public static async Task<string> GetQrcodezatca(string invGlobalID)
        {
            // ✅ إصلاح: التحقق من المدخل
            if (string.IsNullOrWhiteSpace(invGlobalID))
                return "";

            try
            {
                using var sqlConnection = MainClass.ConnObj();
                if (sqlConnection == null) return "";

                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                // ✅ إصلاح: Parameterized Query لمنع SQL Injection
                using var cmd = new SqlCommand(
                    @"SELECT ZE.EncodedInvoice
                      FROM ZatcaEncodedInvoice ZE
                      LEFT JOIN INV I ON ZE.InvGlobalID = I.InvGlobalID
                      WHERE ZE.InvGlobalID = @InvGlobalID",
                    sqlConnection);

                cmd.Parameters.AddWithValue("@InvGlobalID", invGlobalID);

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0]["EncodedInvoice"]?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في GetQrcodezatca: {ex.Message}");
            }

            return "";
        }

        #endregion

        #region Helper Methods

        // ✅ دوال مساعدة للقراءة الآمنة من DataRow
        private static string SafeString(DataRow row, string column)
        {
            try
            {
                return row[column] != DBNull.Value
                    ? row[column]?.ToString() ?? ""
                    : "";
            }
            catch { return ""; }
        }

        private static int SafeInt(DataRow row, string column)
        {
            try
            {
                return row[column] != DBNull.Value
                    ? Convert.ToInt32(row[column])
                    : 0;
            }
            catch { return 0; }
        }

        private static double SafeDouble(DataRow row, string column)
        {
            try
            {
                return row[column] != DBNull.Value
                    ? Convert.ToDouble(row[column])
                    : 0.0;
            }
            catch { return 0.0; }
        }

        private static bool SafeBool(DataRow row, string column)
        {
            try
            {
                return row[column] != DBNull.Value
                    && Convert.ToBoolean(row[column]);
            }
            catch { return false; }
        }

        #endregion
    }
}