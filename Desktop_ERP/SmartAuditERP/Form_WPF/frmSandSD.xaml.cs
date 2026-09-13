using AuditorAPI.Models;
using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.Xpf.Core;
using log4net;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSandSD : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        private string EntryGlobalID;
        public string GlobalID;
        public string SrchName;
        private int ReceiptType;
        private Print print;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<ReceiptSearchItem> _searchItems;

        // ✅ إصلاح: إزالة _isLoaded من EnsureConnection
        // لأنه كان يمنع فتح الاتصال أثناء الـ Constructor والـ Load

        #endregion

        #region Constructor

        public frmSandSD()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            EntryGlobalID = "-1";
            GlobalID = "-1";
            ReceiptType = 8;
            SrchName = "";
            print = new Print(10);
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";

            _searchItems = new ObservableCollection<ReceiptSearchItem>();
            dgvSrch.ItemsSource = _searchItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;
                txtToDate.DateTime = DateTime.Now;
                txtTime.Text = DateTime.Now.ToString("HH:mm");

                LoadEmps();
                LoadCostCenters();
                LoadAccounts();
                LoadSales();
                LoadBranches();
                LoadPrintSettings();

                // ✅ تحميل الخزن الافتراضي (نقدي)
                LoadCashSources();

                RestorePreviousReceipt();

                txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();

                Editereciept.Text = MainClass.UserName;
                Editereciept.Foreground = System.Windows.Media.Brushes.Red;

                SetStatus("جاهز");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear / Reset

        private void CLR()
        {
            txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();
            txtVal.Text = "";
            txtRef.Text = "";
            txtNotes.Text = "";
            txtCheckNo.Text = "";
            txtBankCheck.Text = "";
            TxtRecived.Text = "";
            SrchName = "";

            txtDate.DateTime = DateTime.Now;
            txtRefDate.DateTime = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime = DateTime.Now;
            txtTime.Text = DateTime.Now.ToString("HH:mm");

            cmbAccounts.SelectedIndex = -1;
            cmbPayFrom.SelectedIndex = -1;
            cmbSalesMan.SelectedIndex = -1;
            cmbCostCenter.SelectedIndex = -1;
            cmbBranches.SelectedIndex = -1;
            cmbCheckType.SelectedIndex = -1;

            rdCash.IsChecked = true;
            Panel1.Visibility = Visibility.Collapsed;

            Code = -1;
            GlobalID = "-1";
            EntryGlobalID = "-1";

            Editereciept.Text = MainClass.UserName;
            Editereciept.Foreground = System.Windows.Media.Brushes.Red;
        }

        #endregion

        #region Data Loading

        public void LoadEmps()
        {
            try
            {
                SafeOpenConnection(conn1);
                string sql = $@"SELECT id, name
                                FROM Employees
                                INNER JOIN EmpBranches ON Employees.id = EmpBranches.emp
                                WHERE EmpBranches.branch = {MainClass.BranchNo}
                                  AND IS_Deleted = 0
                                ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                // يمكن ربطها بـ ComboBox إذا احتجت لاحقاً
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الموظفين: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        public void LoadSales()
        {
            try
            {
                SafeOpenConnection(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMan.ItemsSource = dt.DefaultView;
                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل المندوبين: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        private void LoadCostCenters()
        {
            try
            {
                SafeOpenConnection(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center WHERE type=2 AND Is_Deleted=0 ORDER BY code",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل مراكز التكلفة: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        private void LoadAccounts()
        {
            try
            {
                SafeOpenConnection(conn1);
                string branchCond = Accounting.BranchCondition; // ✅ مطابق للأصل
                string sql = $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {branchCond}";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الحسابات: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        private void LoadBranches()
        {
            try
            {
                SafeOpenConnection(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches WHERE IS_Deleted=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الفروع: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        private void LoadCashSources()
        {
            try
            {
                SafeOpenConnection(conn1);
                string sql = $@"SELECT id, name
                                FROM Stocks
                                INNER JOIN Stock_Emps ON Stocks.id = Stock_Emps.stock_id
                                WHERE emp_id = {MainClass.EmpNo}
                                  AND IS_Deleted = 0
                                  AND status <> 2
                                ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbPayFrom.ItemsSource = dt.DefaultView;
                cmbPayFrom.DisplayMemberPath = "name";
                cmbPayFrom.SelectedValuePath = "id";
                cmbPayFrom.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الخزن: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        private void LoadBankSources()
        {
            try
            {
                SafeOpenConnection(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbPayFrom.ItemsSource = dt.DefaultView;
                cmbPayFrom.DisplayMemberPath = "name";
                cmbPayFrom.SelectedValuePath = "id";
                cmbPayFrom.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل البنوك: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                SafeOpenConnection(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=10", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pType))
                            PrintType = pType;

                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                        if (string.IsNullOrWhiteSpace(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();

                        if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pNo))
                            PrintNo = pNo;

                        RptName = dt.Rows[0]["RptName"].ToString();

                        // ✅ إصلاح: قراءة RptUrl من قاعدة البيانات كما في الأصل
                        string rptUrlFromDb = dt.Rows[0]["RptUrl"]?.ToString();
                        RptUrl = string.IsNullOrWhiteSpace(rptUrlFromDb)
                            ? MainClass.ReportsPath
                            : rptUrlFromDb;
                    }
                    catch { /* تجاهل خطأ القراءة */ }
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                    defPrinter = MainClass.ReportsPrinter;
                    RptName = "Payment.repx";
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        #endregion

        #region Restore Previous Receipts

        // ✅ إصلاح كامل: مطابق للأصل بالكامل
        private void RestorePreviousReceipt()
        {
            try
            {
                SafeOpenConnection(conn);

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SandSD WHERE branch = {MainClass.BranchNo}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var receipt = new Receipt();

                        // جلب GlobalID من Entry
                        var subAdapter = new SqlDataAdapter(
                            $@"SELECT GlobalID FROM Entry
                       WHERE id = {row["rest_id"]}
                         AND branch = {row["branch"]}",
                            conn);
                        var subDt = new DataTable();
                        subAdapter.Fill(subDt);

                        if (subDt.Rows.Count > 0)
                            receipt.EntryGlobalID = subDt.Rows[0]["GlobalID"].ToString();

                        // حساب GlobalID الجديد
                        var maxCmd = new SqlCommand(
                            $"SELECT ISNULL(MAX(AutoID), 0) FROM Receipts WHERE BranchID={MainClass.BranchNo}",
                            conn);
                        int nextId = Convert.ToInt32(maxCmd.ExecuteScalar()) + 1;
                        receipt.GlobalID = $"{MainClass.BranchNo}-{nextId}";

                        receipt.ReceiptNo = Convert.ToInt32(row["id"]);
                        receipt.BranchID = Convert.ToInt32(row["branch"]);
                        receipt.ReceiptType = ReceiptType;
                        receipt.State = 1;
                        receipt.Payment = Convert.ToDouble(row["val"]);
                        receipt.VAT = 0.0;
                        receipt.NetVal = Convert.ToDouble(row["val"]);
                        receipt.ReceiptDate = Convert.ToDateTime(row["date"]);
                        receipt.ReffNo = "-1";
                        receipt.Reffdate = Convert.ToDateTime(row["date"]);
                        receipt.ISDeleted = Convert.ToBoolean(row["IS_Deleted"]);
                        receipt.Notes = row["notes"].ToString();
                        receipt.EmpId = Convert.ToInt32(row["emp_id"]);

                        var treasury = new Treasury(Convert.ToInt32(row["safe_bank_id"]));
                        receipt.CreditAcc = treasury.AccCode;
                        receipt.DebitAcc = row["acc_code"].ToString();
                        receipt.ClientID = Convert.ToInt32(row["acc_code"]);
                        receipt.BankId = -1;
                        receipt.TreasuryID = Convert.ToInt32(row["safe_bank_id"]);
                        receipt.SalesManID = Convert.ToInt32(row["sales_emp"]);
                        receipt.PaymentType = 1;
                        receipt.CheckState = 1;
                        receipt.CheckDate = Convert.ToDateTime(row["date"]);
                        receipt.CheckNo = "-1";
                        receipt.Checkbank = "";
                        receipt.Cccode = row["Cccode"]?.ToString() ?? "";
                        receipt.Sync = false;
                        receipt.ClientCode = Sync.ClientCode;
                        receipt.BranchType = Sync.BranchType;
                        receipt.DistBranch = Sync.DistBranch;

                        var receiptOper = new ReceiptOper();
                        bool saved = receiptOper.SaveReceipt2(receipt);

                        if (saved)
                        {
                            var deleteCmd = new SqlCommand(
                                $"DELETE FROM SandSD WHERE id = {receipt.ReceiptNo}",
                                conn);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // استمر مع الصف التالي
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في استعادة السندات: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        #endregion

        #region Button Events - Navigation

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            SetStatus("سند جديد");
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                        ORDER BY ReceiptNo ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int currentNo)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                          AND ReceiptNo < {currentNo}
                        ORDER BY ReceiptNo DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int currentNo)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                          AND ReceiptNo > {currentNo}
                        ORDER BY ReceiptNo ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                        ORDER BY ReceiptNo DESC");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Button Events - Operations

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // ─── التحقق من الفترة المحاسبية ───────────────────────────
            if (!Common.CheckProiedAcc(txtDate.DateTime))
            {
                DXMessageBox.Show(
                    "لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ─── التحقق من القيمة ─────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("ادخل القيمة", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                txtVal.Focus();
                return;
            }

            // ─── النسخة التجريبية ─────────────────────────────────────
            if (MainClass.IsTrial && Code == -1)
            {
                if (Common.CheckIsTrial())
                {
                    string trialMsg = MainClass.Language == "en"
                        ? "Sorry, You reach the maximum of entries"
                        : "نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية";
                    DXMessageBox.Show(trialMsg, "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    MainClass.IsTrial = true;
                    return;
                }
            }

            // ─── التحقق من الحساب ─────────────────────────────────────
            if (cmbAccounts.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار الحساب", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAccounts.Focus();
                return;
            }

            // ─── التحقق من الخزنة/البنك ───────────────────────────────
            if (cmbPayFrom.SelectedIndex == -1)
            {
                string payMsg = rdCash.IsChecked == true
                    ? "يجب اختيار الخزنة"
                    : "يجب اختيار البنك";
                DXMessageBox.Show(payMsg, "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbPayFrom.Focus();
                return;
            }

            // ─── تأكيد الحفظ ──────────────────────────────────────────
            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من حفظ السند؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                SafeOpenConnection(conn1);

                // المندوب
                int salesManId = -1;
                if (cmbSalesMan.SelectedValue != null)
                    int.TryParse(cmbSalesMan.SelectedValue.ToString(), out salesManId);

                // مركز التكلفة
                int costCenterCode = -1;
                if (cmbCostCenter.SelectedValue != null)
                    int.TryParse(cmbCostCenter.SelectedValue.ToString(), out costCenterCode);

                // الملاحظات الافتراضية
                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    string accName = (cmbAccounts.SelectedItem as DataRowView)
                                        ?["AName"]?.ToString() ?? cmbAccounts.Text;
                    txtNotes.Text = $"سند صرف رقم: {txtNo.Text} - سداد دفعة من حساب: {accName}";
                }

                bool isNew = (Code == -1);

                // ── الحصول على GlobalID و EntryGlobalID ───────────────
                if (isNew)
                {
                    // ✅ إصلاح: تمرير المتغيرين بـ ref
                    int sandDRestId = 0;
                    EntryOper.GetEntryGlobalID(ref EntryGlobalID, ref sandDRestId);

                    var maxCmd = new SqlCommand(
                        $"SELECT ISNULL(MAX(AutoID), 0) FROM Receipts WHERE BranchID={MainClass.BranchNo}",
                        conn1);
                    int sandDId = Convert.ToInt32(maxCmd.ExecuteScalar()) + 1;
                    GlobalID = $"{MainClass.BranchNo}-{sandDId}";
                    txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();
                    int.TryParse(txtNo.Text, out Code);
                }

                // ── حساب الكود المحاسبي للحساب ───────────────────────
                int accCode = 0;
                {
                    string accName = (cmbAccounts.SelectedItem as DataRowView)
                                        ?["AName"]?.ToString() ?? "";
                    var daCustAcc = new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND AName=N'{accName}'",
                        conn1);
                    var dtCustAcc = new DataTable();
                    daCustAcc.Fill(dtCustAcc);
                    if (dtCustAcc.Rows.Count > 0)
                        accCode = Convert.ToInt32(dtCustAcc.Rows[0][0]);
                }

                // ── حساب الكود المحاسبي للخزنة/البنك ─────────────────
                int stockOrBankAcc = 0;
                {
                    string payFromName = (cmbPayFrom.SelectedItem as DataRowView)
                                            ?["name"]?.ToString() ?? "";
                    var daStock = new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND AName=N'{payFromName}'",
                        conn1);
                    var dtStock = new DataTable();
                    daStock.Fill(dtStock);
                    if (dtStock.Rows.Count > 0)
                        stockOrBankAcc = Convert.ToInt32(dtStock.Rows[0][0]);
                }

                // ── تاريخ ووقت السند ──────────────────────────────────
                TimeSpan.TryParse(txtTime.Text, out TimeSpan timeValue);
                DateTime receiptDateTime = txtDate.DateTime.Date + timeValue;

                // ── بناء كائن Receipt ─────────────────────────────────
                var receipt = new Receipt
                {
                    GlobalID = GlobalID,
                    ReceiptNo = Code,
                    BranchID = isNew
                                        ? MainClass.BranchNo
                                        : Convert.ToInt32(cmbBranches.SelectedValue),
                    EntryGlobalID = EntryGlobalID,
                    ReceiptType = ReceiptType,
                    State = 1,
                    Payment = Convert.ToDouble(txtVal.Text),
                    VAT = 0.0,
                    NetVal = Convert.ToDouble(txtVal.Text),
                    ReceiptDate = receiptDateTime,
                    ReffNo = txtRef.Text,
                    Reffdate = txtRefDate.DateTime,
                    ISDeleted = false,
                    Notes = txtNotes.Text,
                    EmpId = MainClass.EmpNo,
                    CreditAcc = stockOrBankAcc.ToString(),
                    DebitAcc = accCode.ToString(),
                    ClientID = cmbAccounts.SelectedValue != null
                                        ? Convert.ToInt32(cmbAccounts.SelectedValue)
                                        : (int?)null,
                    BankId = stockOrBankAcc,
                    TreasuryID = cmbPayFrom.SelectedValue != null
                                        ? Convert.ToInt32(cmbPayFrom.SelectedValue)
                                        : (int?)null,
                    SalesManID = salesManId,
                    Cccode = costCenterCode.ToString(),
                    Sync = false,
                    ClientCode = Sync.ClientCode,
                    BranchType = Sync.BranchType,
                    DistBranch = Sync.DistBranch,
                    Recipient = TxtRecived.Text
                };

                // ── نوع الدفع ─────────────────────────────────────────
                if (rdCheck.IsChecked == true)
                {
                    receipt.PaymentType = 2;
                    receipt.CheckNo = txtCheckNo.Text;
                    receipt.CheckDate = txtCheckDate.DateTime;
                    receipt.Checkbank = txtBankCheck.Text;
                    receipt.CheckState = cmbCheckType.SelectedIndex + 1;
                }
                else
                {
                    receipt.PaymentType = 1;
                    receipt.CheckState = 1;
                    receipt.CheckDate = txtDate.DateTime;
                    receipt.CheckNo = "-1";
                    receipt.Checkbank = "";
                }

                // ── الحفظ عبر ReceiptOper ─────────────────────────────
                var receiptOp = new ReceiptOper();
                var entry = receiptOp.BindReceiptToEntry(receipt);
                bool saved = receiptOp.SaveReceipt(receipt, entry, isNew);

                if (!saved)
                {
                    string errMsg = MainClass.Language == "en"
                        ? "error in saving"
                        : "خطأ أثناء الحفظ";
                    DXMessageBox.Show(errMsg, "",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ── تسجيل في اللوج ────────────────────────────────────
                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(isNew
                    ? $"تم حفظ سند صرف برقم {receipt.ReceiptNo} بواسطة {MainClass.UserName}"
                    : $"تم تعديل سند صرف برقم {receipt.ReceiptNo} بواسطة {MainClass.UserName}");

                // ── المزامنة ──────────────────────────────────────────
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    await receiptOp.SyncReceipt(receipt, entry, isNew);
                var Home = new Home();
                // ── إرسال البيانات عبر Broker ─────────────────────────
                if (Home._is_active
                    && ConnectBroker.CheckConnectionAndBroker())
                {
                    string cond = $"WHERE GlobalID=N'{receipt.GlobalID}'";
                    string condEntry = $"WHERE GlobalID=N'{receipt.EntryGlobalID}'";
                    string condEntrySub = $"WHERE EntryGlobalID=N'{receipt.EntryGlobalID}'";
                    SendData.SendDataa("Receipts", receipt.GlobalID, SendData.GetReceipts(cond));
                    SendData.SendDataa("entry", receipt.EntryGlobalID, SendData.GetEntryData(condEntry));
                    SendData.SendDataa("entryDetails", receipt.EntryGlobalID, SendData.GetEntrySubData(condEntrySub));
                }

                // ── رسالة النجاح ──────────────────────────────────────
                Code = receipt.ReceiptNo;
                SetStatus(isNew
                    ? $"تم حفظ سند صرف رقم {Code} بنجاح"
                    : $"تم تعديل سند صرف رقم {Code} بنجاح");

                var savedForm = new frmSavedMsg();
                savedForm.ShowDialog();

                if (savedForm.Pressed == 1)
                {
                    // ✅ إصلاح: WPF DataGrid يستخدم Clear() على ItemsSource
                    _searchItems.Clear();
                    CLR();
                }
                else if (savedForm.Pressed == 3)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    "خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SafeCloseConnection(conn1);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر سنداً ليتم حذفه", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من حذف السند؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            var receiptOper = new ReceiptOper();
            bool deleted = receiptOper.DeleteReceipt(GlobalID, EntryGlobalID);

            if (deleted)
            {
                DXMessageBox.Show("تم الحذف", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info($"تم حذف سند صرف برقم {txtNo.Text} بواسطة {MainClass.UserName}");

                // ✅ إصلاح: WPF DataGrid
                _searchItems.Clear();
                CLR();
                SetStatus("تم حذف السند");
            }
            else
            {
                DXMessageBox.Show("خطأ أثناء الحذف", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintReceipt(1);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            PrintReceipt(2);
        }

        private void BtnImportDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF Files (*.pdf)|*.pdf",
                Title = "تحديد الملفات",
                Multiselect = true
            };

            string dataPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data");

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            if (dialog.ShowDialog() == true)
            {
                // ✅ إصلاح: مطابق للأصل
                ReceiptOper.filePathDocument = dialog.FileNames;
                SetStatus($"تم اختيار {dialog.FileNames.Length} ملف");
            }
        }

        private void Btnshowdocument_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("يجب حفظ السند أولاً", "المدقق",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dt = new DataTable();
            ReceiptOper.getdocuments(ref dt, GlobalID);

            if (dt.Rows.Count > 0)
            {
                var frmDoc = new Frmshowdocument();

                // ✅ إصلاح: frmshowdocument محولة لـ WPF
                // استخدام ItemsSource بدلاً من Rows.Add
                var docItems = new ObservableCollection<DocumentItem>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    docItems.Add(new DocumentItem
                    {
                        RowNo = i + 1,
                        FileName = dt.Rows[i]["FileName"].ToString(),
                        FileUrl = dt.Rows[i]["FileUrl"].ToString()
                    });
                }

                frmDoc.DataGridView1.ItemsSource = docItems;
                frmDoc.type = 3;
                frmDoc.GlobalIdDoc = GlobalID;
                frmDoc.Show();
            }
            else
            {
                DXMessageBox.Show("لا يوجد مستندات", "المدقق",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnSearchAcc_Click(object sender, RoutedEventArgs e)
        {
            AddNewAccount();
        }

        #endregion

        #region Search

        private void Search()
        {
            try
            {
                // ✅ إصلاح: WPF DataGrid يستخدم ObservableCollection
                _searchItems.Clear();

                string condition = $"Receipts.ReceiptType = {ReceiptType} AND ";

                if (MainClass.BranchNo != -1)
                    condition += $"Receipts.BranchID = {MainClass.BranchNo} AND ";

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    condition += $"Receipts.ReceiptNo = {txtSrchNo.Text} AND ";
                else if (!string.IsNullOrWhiteSpace(txtSrchRef.Text))
                    condition += $"Receipts.ReffNo = '{txtSrchRef.Text}' AND ";

                if (chkall.IsChecked != true)
                    condition += "ReceiptDate BETWEEN @StartDate AND @EndDate AND ";

                // استخدام ReceiptOper.LoadReceipts مثل الأصل
                var dt = ReceiptOper.LoadReceipts(
                    condition,
                    txtFromDate.DateTime,
                    txtToDate.DateTime).Copy();

                foreach (DataRow row in dt.Rows)
                {
                    _searchItems.Add(new ReceiptSearchItem
                    {
                        GlobalId = row["GlobalID"].ToString(),
                        ReceiptNo = row["ReceiptNo"].ToString(),
                        ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString(),
                        Payment = row["Payment"].ToString(),
                        SalesManName = row["name"]?.ToString() ?? ""
                    });
                }

                SetStatus($"تم العثور على {_searchItems.Count} نتيجة");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    "خطأ في البحث\nتفاصيل الخطأ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlQuery)
        {
            try
            {
                SafeOpenConnection(conn);
                var cmd = new SqlCommand(sqlQuery, conn);
                var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        // ✅ الإصلاح الرئيسي: قراءة EmpId قبل إغلاق reader
        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows)
            {
                reader.Close();
                SetStatus("لا توجد سجلات");
                return;
            }

            reader.Read();
            CLR();

            // ── قراءة كل الحقول قبل إغلاق reader ────────────────────
            Code = Convert.ToInt32(reader["ReceiptNo"]);
            GlobalID = reader["GlobalID"]?.ToString() ?? "-1";
            EntryGlobalID = reader["EntryGlobalID"] != DBNull.Value
                                ? reader["EntryGlobalID"].ToString()
                                : "-1";

            DateTime receiptDate = reader["ReceiptDate"] != DBNull.Value
                ? Convert.ToDateTime(reader["ReceiptDate"])
                : DateTime.Now;

            string notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : "";
            string payment = reader["Payment"] != DBNull.Value ? reader["Payment"].ToString() : "";
            string reffNo = reader["ReffNo"] != DBNull.Value ? reader["ReffNo"].ToString() : "";
            string recipient = reader["Recipient"] != DBNull.Value ? reader["Recipient"].ToString() : "";

            DateTime reffDate = reader["Reffdate"] != DBNull.Value
                ? Convert.ToDateTime(reader["Reffdate"])
                : DateTime.Now;

            object debitAcc = reader["DebitAcc"] != DBNull.Value ? reader["DebitAcc"] : null;
            object ccCode = reader["CCcode"] != DBNull.Value ? reader["CCcode"] : null;
            object branchId = reader["BranchID"] != DBNull.Value ? reader["BranchID"] : null;
            object salesManId = reader["SalesManID"] != DBNull.Value ? reader["SalesManID"] : null;
            object treasuryId = reader["TreasuryID"] != DBNull.Value ? reader["TreasuryID"] : null;

            int paymentType = reader["PaymentType"] != DBNull.Value
                ? Convert.ToInt32(reader["PaymentType"])
                : 1;

            string checkNo = reader["CheckNo"] != DBNull.Value ? reader["CheckNo"].ToString() : "";
            string checkbank = reader["Checkbank"] != DBNull.Value ? reader["Checkbank"].ToString() : "";
            DateTime checkDate = reader["CheckDate"] != DBNull.Value
                ? Convert.ToDateTime(reader["CheckDate"])
                : DateTime.Now;
            int checkState = reader["CheckState"] != DBNull.Value
                ? Convert.ToInt32(reader["CheckState"])
                : 1;

            // ✅ إصلاح: قراءة EmpId قبل إغلاق reader
            int empId = reader["EmpId"] != DBNull.Value
                ? Convert.ToInt32(reader["EmpId"])
                : 0;

            reader.Close(); // ← الآن آمن للإغلاق

            // ── تطبيق البيانات على الـ UI ─────────────────────────────
            txtNo.Text = Code.ToString();
            txtDate.DateTime = receiptDate;
            txtNotes.Text = notes;
            txtVal.Text = payment;
            txtRef.Text = reffNo;
            TxtRecived.Text = recipient;
            txtRefDate.DateTime = reffDate;

            if (debitAcc != null) cmbAccounts.SelectedValue = debitAcc;
            if (ccCode != null) cmbCostCenter.SelectedValue = ccCode;
            if (branchId != null) cmbBranches.SelectedValue = branchId;
            if (salesManId != null) cmbSalesMan.SelectedValue = salesManId;
            if (treasuryId != null) cmbPayFrom.SelectedValue = treasuryId;

            if (paymentType == 1)
            {
                rdCash.IsChecked = true;
                Panel1.Visibility = Visibility.Collapsed;
            }
            else
            {
                rdCheck.IsChecked = true;
                Panel1.Visibility = Visibility.Visible;
                txtCheckNo.Text = checkNo;
                txtBankCheck.Text = checkbank;
                txtCheckDate.DateTime = checkDate;
                cmbCheckType.SelectedIndex = checkState - 1;
            }

            // ── تحميل اسم المحرر (بعد إغلاق reader) ──────────────────
            LoadEditorName(empId);

            SetStatus($"السند رقم {Code}");
            TabControl1.SelectedIndex = 0;
        }

        private void LoadEditorName(int empId)
        {
            try
            {
                SafeOpenConnection(conn);
                var cmd = new SqlCommand(
                    $"SELECT username FROM users WHERE emp = {empId}", conn);
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    Editereciept.Text = reader["username"].ToString();
                    Editereciept.Foreground = System.Windows.Media.Brushes.Green;
                }
                reader.Close();
            }
            catch
            {
                // تجاهل
            }
            finally
            {
                SafeCloseConnection(conn);
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // ✅ إصلاح: WPF DataGrid يستخدم SelectedItem
            if (dgvSrch.SelectedItem is ReceiptSearchItem selected)
            {
                GlobalID = selected.GlobalId;
                Navigate($"SELECT * FROM Receipts WHERE GlobalID = N'{GlobalID}'");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Radio Button Events

        private void rdCash_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null)
                Panel1.Visibility = Visibility.Collapsed;
            LoadCashSources();
        }

        private void rdCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null)
                Panel1.Visibility = Visibility.Visible;
            LoadBankSources();
        }

        #endregion

        #region CheckBox Events

        private void chkall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = chkall.IsChecked == true;
            txtFromDate.IsEnabled = !allChecked;
            txtToDate.IsEnabled = !allChecked;
        }

        #endregion

        #region TextBox Events

        private void txtTime_GotFocus(object sender, RoutedEventArgs e)
        {
            // تحديد النص عند التركيز
            txtTime.SelectAll();
        }

        private void txtTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!TimeSpan.TryParse(txtTime.Text, out _))
                txtTime.Text = DateTime.Now.ToString("HH:mm");
        }

        private void txtSrchNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void cmbAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SearchByAccountName();
            }
        }

        #endregion

        #region Account Search

        private void SearchByAccountName()
        {
            try
            {
                SrchName = (cmbAccounts.SelectedItem as DataRowView)
                               ?["AName"]?.ToString() ?? cmbAccounts.Text;

                SafeOpenConnection(conn);
                var adapter = new SqlDataAdapter(
                    $@"SELECT Code, AName, CostCenter
                       FROM Accounts_Index
                       WHERE AName = N'{SrchName}' AND Type = 2",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                SafeCloseConnection(conn);

                if (dt.Rows.Count > 0)
                {
                    cmbAccounts.SelectedValue = dt.Rows[0]["Code"];

                    if (dt.Rows[0]["CostCenter"] != DBNull.Value
                        && Convert.ToInt32(dt.Rows[0]["CostCenter"]) != 0)
                    {
                        cmbCostCenter.SelectedValue = dt.Rows[0]["CostCenter"];
                    }
                }
                else
                {
                    AddNewAccount();
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في البحث عن الحساب: " + ex.Message);
            }
        }

        // ✅ إصلاح: تفعيل نافذة البحث فعلياً
        private void AddNewAccount()
        {
            var frm = new frmAccountSrch();
            frm.txtSrchNm.Text = SrchName;
            frm.ShowDialog();

            if (frm.Code > -1)
            {
                try
                {
                    SafeOpenConnection(conn);
                    var adapter = new SqlDataAdapter(
                        $@"SELECT Code, AName, CostCenter
                           FROM Accounts_Index
                           WHERE Type=2 AND Code={frm.Code}",
                        conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbAccounts.SelectedValue = dt.Rows[0]["Code"];

                        if (dt.Rows[0]["CostCenter"] != DBNull.Value
                            && Convert.ToInt32(dt.Rows[0]["CostCenter"]) != 0)
                        {
                            cmbCostCenter.SelectedValue = dt.Rows[0]["CostCenter"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetStatus("خطأ: " + ex.Message);
                }
                finally
                {
                    SafeCloseConnection(conn);
                }
            }
        }

        #endregion

        #region Print

        // ✅ إصلاح: تفعيل BindToData و PrintReceipt
        private DataSet BindToData()
        {
            try
            {
                SafeOpenConnection(conn);
                var adapter = new SqlDataAdapter("SELECT * FROM Foundation", conn);
                var dtFoundation = new DataTable();
                adapter.Fill(dtFoundation);
                SafeCloseConnection(conn);

                string address = "", tel = "", mobile = "",
                       nameA = "", fieldA = "", taxNo = "";

                if (dtFoundation.Rows.Count > 0)
                {
                    address = dtFoundation.Rows[0]["Address"]?.ToString() ?? "";
                    tel = dtFoundation.Rows[0]["Tel"]?.ToString() ?? "";
                    mobile = dtFoundation.Rows[0]["Mobile"]?.ToString() ?? "";
                    nameA = dtFoundation.Rows[0]["nameA"]?.ToString() ?? "";
                    fieldA = dtFoundation.Rows[0]["FieldA"]?.ToString() ?? "";
                    taxNo = dtFoundation.Rows[0]["tax_no"]?.ToString() ?? "";
                }

                string bondType = rdCash.IsChecked == true
                    ? (rdCash.Content?.ToString() ?? "نقدي")
                    : (rdCheck.Content?.ToString() ?? "شيك");

                int currencyCode = Common.GetCurrencytosand(13);

                var bond = new Bond
                {
                    BondName = this.Title,
                    PayToCode = cmbAccounts.SelectedValue?.ToString() ?? "",
                    PayFromCode = cmbPayFrom.SelectedValue?.ToString() ?? "",
                    PayTo = (cmbAccounts.SelectedItem as DataRowView)?["AName"]?.ToString()
                                   ?? cmbAccounts.Text,
                    Payfrom = (cmbPayFrom.SelectedItem as DataRowView)?["name"]?.ToString()
                                   ?? cmbPayFrom.Text,
                    CostCenter = (cmbCostCenter.SelectedItem as DataRowView)?["name"]?.ToString()
                                   ?? cmbCostCenter.Text,
                    BondVal = txtVal.Text,
                    BondDate = txtDate.DateTime.ToShortDateString(),
                    BondNo = txtNo.Text,
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Note = txtNotes.Text,
                    AccountStatus = "",
                    CurrentBalance = "",
                    BondType = bondType,
                    SalesMan = (cmbSalesMan.SelectedItem as DataRowView)?["name"]?.ToString()
                                   ?? cmbSalesMan.Text,
                    User = Editereciept.Text,
                    ArabicLetter = InvoiceOper.ToArabicLetter(
                        Convert.ToDouble(txtVal.Text == "" ? "0" : txtVal.Text),
                        (Currency)currencyCode),
                    reffDate = txtRefDate.DateTime.ToString(),
                    reffNo = txtRef.Text,
                    Branch = MainClass.BranchName,
                    Address = address,
                    Mobile = mobile,
                    Telephone = tel,
                    VATNo = taxNo,
                    Foundation = nameA,
                    Field = fieldA,
                    Logo = "",
                    Header = "",
                    Footer = "",
                    Stamp = ""
                };

                var list = new List<Bond> { bond };
                var ds = new DataSet("Name");
                ds.Tables.Add(UtilitiesProj.Common.ToDataTable<Bond>(list));
                return ds;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في بناء بيانات الطباعة: " + ex.Message);
                return null;
            }
        }

        private void PrintReceipt(int printMode)
        {
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptName))
                RptName = "Payment.repx";

            string fullPath = System.IO.Path.Combine(RptUrl, RptName);

            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show(
                    "المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(defPrinter))
                defPrinter = MainClass.ReportsPrinter;

            var data = BindToData();
            if (data == null) return;

            // ✅ تفعيل الطباعة الفعلية
            print.Printing(printMode, data, RptUrl, RptName,
                           defPrinter, print.kitchenprinter, print.PrintNo);
        }

        #endregion

        #region Helpers

        private string GetBranchCondition()
        {
            return MainClass.BranchNo != -1
                ? $"BranchID = {MainClass.BranchNo} AND "
                : "";
        }

        // ✅ إصلاح: إزالة شرط _isLoaded الذي كان يمنع فتح الاتصال
        private void SafeOpenConnection(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection),
                    "الاتصال بقاعدة البيانات غير مهيأ");
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void SafeCloseConnection(SqlConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
                connection.Close();
        }

        private void SetStatus(string message)
        {
            if (lblStatus != null)
                lblStatus.Text = message;
        }

        #endregion

        #region Window Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            SafeCloseConnection(conn);
            SafeCloseConnection(conn1);
        }

        #endregion
    }

    // ─── Model للبحث ───────────────────────────────────────────────────────────
    public class ReceiptSearchItem1
    {
        public string GlobalId { get; set; }
        public string ReceiptNo { get; set; }
        public string ReceiptDate { get; set; }
        public string Payment { get; set; }
        public string SalesManName { get; set; }
    }
}