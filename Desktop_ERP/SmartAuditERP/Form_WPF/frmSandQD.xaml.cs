using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using log4net;
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
    public partial class frmSandQD : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private SqlConnection conn2;
        private int SelectedId;
        public string SrchName;
        private int Code;
        private int ReceiptType;
        public string GlobalID;
        private string EntryGlobalID;
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
        private double _Exchangeal;
        private bool _change1;
        private bool _change2;

        private ObservableCollection<ReceiptQDItem> _searchItems;

        #endregion

        #region Constructor

        public frmSandQD()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            conn2 = MainClass.ConnObj();
            SelectedId = -1;
            SrchName = "";
            Code = -1;
            ReceiptType = 7;
            GlobalID = "-1";
            EntryGlobalID = "-1";
            print = new Print(11);
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            _Exchangeal = 1250.0;
            _change1 = true;
            _change2 = false;

            _searchItems = new ObservableCollection<ReceiptQDItem>();
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
                RestorePreviousReceipt();

                txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();
                Editereciept.Text = MainClass.UserName;
                Editereciept.Foreground = System.Windows.Media.Brushes.Red;

                // تحميل الخزن الافتراضي
                rdCash.IsChecked = true;
                LoadCashSources();
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
            txtNotes.Text = "";
            txtRef.Text = "";
            txtBalance.Text = "";
            txtBalanceType.Text = "";
            txtCheckNo.Text = "";
            txtBankCheck.Text = "";

            // ✅ إصلاح: txtValD ليس null
            if (txtValD != null) txtValD.Text = "";

            SrchName = "";
            GlobalID = "-1";
            EntryGlobalID = "-1";
            Code = -1;

            txtDate.DateTime = DateTime.Now;
            txtRefDate.DateTime = DateTime.Now;
            txtTime.Text = DateTime.Now.ToString("HH:mm");

            cmbAccounts.SelectedIndex = -1;
            cmbDepositTO.SelectedIndex = -1;
            cmbSalesMan.SelectedIndex = -1;
            cmbCostCenter.SelectedIndex = -1;
            cmbCheckType.SelectedIndex = -1;

            // ✅ إصلاح: cmbEmployee
            if (cmbEmployee != null)
                cmbEmployee.SelectedIndex = -1;

            rdCash.IsChecked = true;
            Panel1.Visibility = Visibility.Collapsed;

            Editereciept.Text = MainClass.UserName;
            Editereciept.Foreground = System.Windows.Media.Brushes.Red;

            if (MainClass.BranchNo != -1 && cmbBranches.Items.Count > 0)
                cmbBranches.SelectedValue = MainClass.BranchNo;
        }

        #endregion

        #region Data Loading

        public void LoadEmps()
        {
            try
            {
                SafeOpen(conn1);
                string sql = $@"SELECT id, name
                                FROM Employees
                                INNER JOIN EmpBranches ON Employees.id = EmpBranches.emp
                                WHERE EmpBranches.branch = {MainClass.BranchNo}
                                  AND IS_Deleted = 0
                                ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                // ✅ إصلاح: ربط cmbEmployee مثل الأصل
                if (cmbEmployee != null)
                {
                    cmbEmployee.ItemsSource = dt.DefaultView;
                    cmbEmployee.DisplayMemberPath = "name";
                    cmbEmployee.SelectedValuePath = "id";
                    cmbEmployee.SelectedIndex = -1;
                }
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الموظفين: " + ex.Message); }
            finally { SafeClose(conn1); }
        }

        // ✅ إصلاح: إضافة LoadPersonsDeal المفقودة
        public void LoadPersonsDeal()
        {
            try
            {
                SafeOpen(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM DealPersons ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (cmbEmployee != null)
                {
                    cmbEmployee.ItemsSource = dt.DefaultView;
                    cmbEmployee.DisplayMemberPath = "name";
                    cmbEmployee.SelectedValuePath = "id";
                    cmbEmployee.SelectedIndex = -1;
                }
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل أشخاص المعاملات: " + ex.Message); }
            finally { SafeClose(conn1); }
        }

        public void LoadSales()
        {
            try
            {
                SafeOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMan.ItemsSource = dt.DefaultView;
                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل المندوبين: " + ex.Message); }
            finally { SafeClose(conn); }
        }

        private void LoadAccounts()
        {
            try
            {
                SafeOpen(conn1);
                // ✅ استخدام Accounting.BranchCondition مثل الأصل
                string sql = $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition}";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الحسابات: " + ex.Message); }
            finally { SafeClose(conn1); }
        }

        private void LoadCostCenters()
        {
            try
            {
                SafeOpen(conn2);
                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center WHERE type=2 AND Is_Deleted=0 ORDER BY code",
                    conn2);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل مراكز التكلفة: " + ex.Message); }
            finally { SafeClose(conn2); }
        }

        private void LoadBranches()
        {
            try
            {
                SafeOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches WHERE IS_Deleted=0", conn);
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
            catch (Exception ex) { SetStatus("خطأ في تحميل الفروع: " + ex.Message); }
            finally { SafeClose(conn); }
        }

        private void LoadCashSources()
        {
            try
            {
                SafeOpen(conn1);
                // ✅ مطابق للأصل: Stocks مع branch
                string sql = $@"SELECT id, name FROM Stocks
                                WHERE branch = {MainClass.BranchNo}
                                  AND IS_Deleted = 0
                                  AND status <> 2
                                ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTO.ItemsSource = dt.DefaultView;
                cmbDepositTO.DisplayMemberPath = "name";
                cmbDepositTO.SelectedValuePath = "id";
                cmbDepositTO.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الخزن: " + ex.Message); }
            finally { SafeClose(conn1); }
        }

        private void LoadBankSources()
        {
            try
            {
                SafeOpen(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTO.ItemsSource = dt.DefaultView;
                cmbDepositTO.DisplayMemberPath = "name";
                cmbDepositTO.SelectedValuePath = "id";
                cmbDepositTO.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل البنوك: " + ex.Message); }
            finally { SafeClose(conn1); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                SafeOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=11", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pt))
                            PrintType = pt;

                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                        if (string.IsNullOrWhiteSpace(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();

                        if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pn))
                            PrintNo = pn;

                        RptName = dt.Rows[0]["RptName"].ToString();

                        // ✅ قراءة RptUrl من DB
                        string rptUrlDb = dt.Rows[0]["RptUrl"]?.ToString();
                        RptUrl = string.IsNullOrWhiteSpace(rptUrlDb)
                            ? MainClass.ReportsPath
                            : rptUrlDb;
                    }
                    catch { /* تجاهل خطأ قراءة حقل واحد */ }
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                    defPrinter = MainClass.ReportsPrinter;
                    RptName = "Receipt.repx";
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { SafeClose(conn); }
        }

        #endregion

        #region Restore Previous Receipts

        // ✅ إصلاح كامل: مطابق للأصل مع SaveReceipt2
        private void RestorePreviousReceipt()
        {
            try
            {
                SafeOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SandQD WHERE branch = {MainClass.BranchNo}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        var receipt = new Receipt();

                        // جلب EntryGlobalID
                        var subAdapter = new SqlDataAdapter(
                            $@"SELECT GlobalID FROM Entry
                               WHERE id = {row["rest_id"]}
                                 AND branch = {row["branch"]}",
                            conn);
                        var subDt = new DataTable();
                        subAdapter.Fill(subDt);

                        if (subDt.Rows.Count > 0)
                            receipt.EntryGlobalID = subDt.Rows[0]["GlobalID"].ToString();

                        // GlobalID الجديد
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

                        // ✅ في SandQD الأصل: EmpId = emp_person
                        receipt.EmpId = Convert.ToInt32(row["emp_person"]);

                        var treasury = new Treasury(Convert.ToInt32(row["safe_bank_id"]));

                        // ✅ في SandQD: CreditAcc=acc_code, DebitAcc=treasury.AccCode
                        receipt.CreditAcc = row["acc_code"].ToString();
                        receipt.DebitAcc = treasury.AccCode;
                        receipt.ClientID = Convert.ToInt32(row["cust_id"]);
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
                            new SqlCommand(
                                $"DELETE FROM SandQD WHERE id = {receipt.ReceiptNo}",
                                conn).ExecuteNonQuery();
                        }
                    }
                    catch { /* استمر مع الصف التالي */ }
                }
            }
            catch (Exception ex) { SetStatus("خطأ في استعادة السندات: " + ex.Message); }
            finally { SafeClose(conn); }
        }

        #endregion

        #region Account Balance

        // ✅ إصلاح: استخدام txtDate مثل الأصل وليس txtToDate
        private void GetCustBalance()
        {
            try
            {
                if (cmbAccounts.SelectedIndex == -1
                    || cmbAccounts.SelectedValue == null) return;

                int accCode = Convert.ToInt32(cmbAccounts.SelectedValue);

                string branchCond = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} AND Entry_sub.branch={MainClass.BranchNo} AND "
                    : "";

                string sql = $@"SELECT SUM(Entry_sub.dept)   AS dept,
                                       SUM(Entry_sub.credit) AS credit
                                FROM Entry
                                INNER JOIN Entry_sub
                                        ON Entry.GlobalID = Entry_sub.EntryGlobalID
                                WHERE {branchCond}
                                      Entry.IS_Deleted = 0
                                  AND Entry.state = 1
                                  AND Entry.date <= @date2
                                  AND Entry_sub.acc_no = {accCode}";

                SafeOpen(conn);
                var cmd = new SqlCommand(sql, conn);

                // ✅ إصلاح: استخدام txtDate مثل الأصل
                DateTime dateVal = txtDate.DateTime != default(DateTime)
                    ? txtDate.DateTime
                    : DateTime.Now;
                cmd.Parameters.AddWithValue("@date2", dateVal.AddHours(24));

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                double dept = 0;
                double credit = 0;

                if (dt.Rows.Count > 0
                    && !string.IsNullOrWhiteSpace(dt.Rows[0][0]?.ToString()))
                {
                    double.TryParse(dt.Rows[0]["dept"].ToString(), out dept);
                    double.TryParse(dt.Rows[0]["credit"].ToString(), out credit);
                }

                if (dept >= credit)
                {
                    txtBalance.Text = Math.Round(dept - credit, 3).ToString();
                    txtBalanceType.Text = MainClass.Language == "ar" ? "مدين" : "Debit";
                    txtBalanceType.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    txtBalance.Text = Math.Round(credit - dept, 3).ToString();
                    txtBalanceType.Text = MainClass.Language == "ar" ? "دائن" : "Credit";
                    txtBalanceType.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex) { SetStatus("خطأ في حساب الرصيد: " + ex.Message); }
            finally { SafeClose(conn); }
        }

        #endregion

        #region Search

        private void Search()
        {
            try
            {
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

                // ✅ استخدام ReceiptOper.LoadReceipts مثل الأصل
                DateTime fromDate = txtFromDate.DateTime != default(DateTime)
                    ? txtFromDate.DateTime : DateTime.Today;
                DateTime toDate = txtToDate.DateTime != default(DateTime)
                    ? txtToDate.DateTime : DateTime.Today;

                var dt = ReceiptOper.LoadReceipts(condition, fromDate, toDate).Copy();

                foreach (DataRow row in dt.Rows)
                {
                    _searchItems.Add(new ReceiptQDItem
                    {
                        GlobalId = row["GlobalID"].ToString(),
                        ReceiptNo = row["ReceiptNo"].ToString(),
                        ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString(),
                        Payment = row["Payment"].ToString(),
                        EmployeeName = row["name"]?.ToString() ?? ""
                    });
                }

                SetStatus($"تم العثور على {_searchItems.Count} نتيجة");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث\nتفاصيل الخطأ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlQuery)
        {
            try
            {
                SafeOpen(conn);
                var reader = new SqlCommand(sqlQuery, conn).ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { SafeClose(conn); }
        }

        // ✅ إصلاح كامل لـ ReadData: قراءة كل الحقول قبل إغلاق reader
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

            // ── قراءة كل الحقول في متغيرات محلية قبل الإغلاق ──────────
            int code = Convert.ToInt32(reader["ReceiptNo"]);
            string globalId = reader["GlobalID"]?.ToString() ?? "-1";
            string entryGlobalId = reader["EntryGlobalID"]?.ToString() ?? "-1";
            string notes = reader["Notes"]?.ToString() ?? "";
            string payment = reader["Payment"]?.ToString() ?? "";
            string reffNo = reader["ReffNo"]?.ToString() ?? "";

            DateTime receiptDate = reader["Receiptdate"] != DBNull.Value
                ? Convert.ToDateTime(reader["Receiptdate"]) : DateTime.Now;

            DateTime reffDate = reader["Reffdate"] != DBNull.Value
                ? Convert.ToDateTime(reader["Reffdate"]) : DateTime.Now;

            object creditAcc = reader["CreditAcc"] != DBNull.Value ? reader["CreditAcc"] : null;
            object ccCode = reader["CCcode"] != DBNull.Value ? reader["CCcode"] : null;
            object branchId = reader["BranchID"] != DBNull.Value ? reader["BranchID"] : null;
            object treasuryId = reader["TreasuryID"] != DBNull.Value ? reader["TreasuryID"] : null;
            object salesManId = reader["SalesManID"] != DBNull.Value ? reader["SalesManID"] : null;
            object empIdObj = reader["EmpId"] != DBNull.Value ? reader["EmpId"] : null;

            // ✅ في SandQD: EmpId موجود في العمود
            object empIdField = reader["EmpID"] != DBNull.Value ? reader["EmpID"] : null;

            int payType = reader["Paymenttype"] != DBNull.Value
                ? Convert.ToInt32(reader["Paymenttype"]) : 1;

            string checkNo = reader["CheckNo"] != DBNull.Value ? reader["CheckNo"].ToString() : "";
            string checkbank = reader["Checkbank"] != DBNull.Value ? reader["Checkbank"].ToString() : "";

            DateTime checkDate = reader["CheckDate"] != DBNull.Value
                ? Convert.ToDateTime(reader["CheckDate"]) : DateTime.Now;

            int checkState = reader["CheckState"] != DBNull.Value
                ? Convert.ToInt32(reader["CheckState"]) : 1;

            // ✅ إصلاح NullRef: قراءة EmpId قبل إغلاق reader
            int empId = empIdObj != null
                ? Convert.ToInt32(empIdObj)
                : (empIdField != null ? Convert.ToInt32(empIdField) : 0);

            reader.Close(); // ← الآن آمن

            // ── تطبيق البيانات على UI ──────────────────────────────────
            Code = code;
            GlobalID = globalId;
            EntryGlobalID = entryGlobalId;

            txtNo.Text = Code.ToString();
            txtNotes.Text = notes;
            txtVal.Text = payment;
            txtRef.Text = reffNo;
            txtDate.DateTime = receiptDate;
            txtRefDate.DateTime = reffDate;

            if (creditAcc != null) cmbAccounts.SelectedValue = creditAcc;
            if (ccCode != null) cmbCostCenter.SelectedValue = ccCode;
            if (branchId != null) cmbBranches.SelectedValue = branchId;
            if (treasuryId != null) cmbDepositTO.SelectedValue = treasuryId;
            if (salesManId != null) cmbSalesMan.SelectedValue = salesManId;

            // ✅ ربط cmbEmployee مثل الأصل
            if (empIdField != null && cmbEmployee != null)
                cmbEmployee.SelectedValue = empIdField.ToString();

            // نوع الدفع
            if (payType == 1)
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

            LoadEditorName(empId);
            SetStatus($"السند رقم {Code}");
            TabControl1.SelectedIndex = 0;
        }

        private void LoadEditorName(int empId)
        {
            try
            {
                SafeOpen(conn);
                var reader = new SqlCommand(
                    $"SELECT username FROM users WHERE emp = {empId}", conn).ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    Editereciept.Text = reader["username"].ToString();
                    Editereciept.Foreground = System.Windows.Media.Brushes.Green;
                }
                reader.Close();
            }
            catch { }
            finally { SafeClose(conn); }
        }

        private string GetBranchCondition() =>
            MainClass.BranchNo != -1 ? $"BranchID = {MainClass.BranchNo} AND " : "";

        #endregion

        #region Button Events - Navigation

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            SetStatus("سند جديد");
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                        ORDER BY ReceiptNo ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0 AND ReceiptNo < {no}
                        ORDER BY ReceiptNo DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0 AND ReceiptNo > {no}
                        ORDER BY ReceiptNo ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType = {ReceiptType}
                          AND ISDeleted = 0
                        ORDER BY ReceiptNo DESC");

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Button Events - Operations

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // ─── التحقق من الفترة المحاسبية ───────────────────────────
            DateTime dateVal = txtDate.DateTime != default(DateTime)
                ? txtDate.DateTime : DateTime.Now;

            if (!Common.CheckProiedAcc(dateVal))
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
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (cmbDepositTO.SelectedIndex == -1)
            {
                string msg = rdCash.IsChecked == true
                    ? "يجب اختيار الخزنة"
                    : "يجب اختيار البنك";
                DXMessageBox.Show(msg, "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbDepositTO.Focus();
                return;
            }

            // ─── تأكيد الحفظ ──────────────────────────────────────────
            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من حفظ السند؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                SafeOpen(conn1);

                bool isNew = (Code == -1);

                // ── GlobalID و EntryGlobalID ──────────────────────────
                if (isNew)
                {
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

                // ── الملاحظات الافتراضية ──────────────────────────────
                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    string accName = (cmbAccounts.SelectedItem as DataRowView)
                                        ?["AName"]?.ToString() ?? cmbAccounts.Text;
                    txtNotes.Text = $"سند قبض رقم: {txtNo.Text} - تحصيل من حساب: {accName}";
                }

                // ── المندوب ───────────────────────────────────────────
                int salesManId = -1;
                if (cmbSalesMan.SelectedValue != null)
                    int.TryParse(cmbSalesMan.SelectedValue.ToString(), out salesManId);

                // ── مركز التكلفة ──────────────────────────────────────
                int costCenterCode = -1;
                if (cmbCostCenter.SelectedValue != null)
                    int.TryParse(cmbCostCenter.SelectedValue.ToString(), out costCenterCode);

                // ── حساب الخزنة/البنك من Accounts_Index ──────────────
                int stockOrBankAcc = 0;
                {
                    string depositName = (cmbDepositTO.SelectedItem as DataRowView)
                                            ?["name"]?.ToString() ?? "";
                    var daStock = new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND AName=N'{depositName}'",
                        conn1);
                    var dtStock = new DataTable();
                    daStock.Fill(dtStock);
                    if (dtStock.Rows.Count > 0)
                        stockOrBankAcc = Convert.ToInt32(dtStock.Rows[0][0]);
                }

                // ── AccCode من SelectedValue مباشرة مثل الأصل ─────────
                int accCode = cmbAccounts.SelectedValue != null
                    ? Convert.ToInt32(cmbAccounts.SelectedValue)
                    : 0;

                // ── التاريخ والوقت ────────────────────────────────────
                TimeSpan.TryParse(txtTime.Text, out TimeSpan ts);
                DateTime receiptDateTime = dateVal.Date + ts;

                // ── بناء كائن Receipt ─────────────────────────────────
                var receipt = new Receipt
                {
                    GlobalID = GlobalID,
                    ReceiptNo = Code,
                    BranchID = isNew
                                        ? MainClass.BranchNo
                                        : (cmbBranches.SelectedValue != null
                                            ? Convert.ToInt32(cmbBranches.SelectedValue)
                                            : MainClass.BranchNo),
                    EntryGlobalID = EntryGlobalID,
                    ReceiptType = 7,       // ✅ مطابق للأصل
                    State = 1,
                    Payment = Convert.ToDouble(txtVal.Text),
                    VAT = 0.0,
                    NetVal = Convert.ToDouble(txtVal.Text),
                    ReceiptDate = receiptDateTime,
                    ReffNo = txtRef.Text,
                    Reffdate = txtRefDate.DateTime != default(DateTime)
                                        ? txtRefDate.DateTime
                                        : receiptDateTime,
                    ISDeleted = false,
                    Notes = txtNotes.Text,
                    EmpId = MainClass.EmpNo,
                    // ✅ في SandQD: CreditAcc=AccCode, DebitAcc=StockORBankAcc
                    CreditAcc = accCode.ToString(),
                    DebitAcc = stockOrBankAcc.ToString(),
                    ClientID = accCode,
                    BankId = stockOrBankAcc,
                    TreasuryID = cmbDepositTO.SelectedValue != null
                                        ? Convert.ToInt32(cmbDepositTO.SelectedValue)
                                        : (int?)null,
                    SalesManID = salesManId,
                    Cccode = costCenterCode.ToString(),
                    Sync = false,
                    ClientCode = Sync.ClientCode,
                    BranchType = Sync.BranchType,
                    DistBranch = Sync.DistBranch
                };

                // ── نوع الدفع ─────────────────────────────────────────
                if (rdCheck.IsChecked == true)
                {
                    receipt.PaymentType = 2;
                    receipt.CheckNo = txtCheckNo.Text;
                    receipt.CheckDate = txtCheckDate.DateTime != default(DateTime)
                                            ? txtCheckDate.DateTime
                                            : receiptDateTime;
                    receipt.Checkbank = txtBankCheck.Text;
                    receipt.CheckState = cmbCheckType.SelectedIndex + 1;
                }
                else
                {
                    receipt.PaymentType = 1;
                    receipt.CheckState = 1;
                    receipt.CheckDate = receiptDateTime;
                    receipt.CheckNo = "-1";
                    receipt.Checkbank = "";
                }

                // ── الحفظ عبر ReceiptOper (مطابق للأصل) ──────────────
                var receiptOp = new ReceiptOper();
                var entry = receiptOp.BindReceiptToEntry(receipt);
                bool saved = receiptOp.SaveReceipt(receipt, entry, isNew);

                if (!saved)
                {
                    string errMsg = MainClass.Language == "en"
                        ? "error in saving" : "خطأ أثناء الحفظ";
                    DXMessageBox.Show(errMsg, "",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ── تسجيل اللوج ───────────────────────────────────────
                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(isNew
                    ? $"تم حفظ سند قبض برقم {receipt.ReceiptNo} بواسطة {MainClass.UserName}"
                    : $"تم تعديل سند قبض برقم {receipt.ReceiptNo} بواسطة {MainClass.UserName}");

                // ── المزامنة ──────────────────────────────────────────
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    await receiptOp.SyncReceipt(receipt, entry, isNew);
                var Home = new Home();
                // ── إرسال البيانات عبر Broker ─────────────────────────
                if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                {
                    string cond = $"WHERE GlobalID=N'{receipt.GlobalID}'";
                    string condEntry = $"WHERE GlobalID=N'{receipt.EntryGlobalID}'";
                    string condEntrySub = $"WHERE EntryGlobalID=N'{receipt.EntryGlobalID}'";
                    SendData.SendDataa("Receipts", receipt.GlobalID, SendData.GetReceipts(cond));
                    SendData.SendDataa("entry", receipt.EntryGlobalID, SendData.GetEntryData(condEntry));
                    SendData.SendDataa("entryDetails", receipt.EntryGlobalID, SendData.GetEntrySubData(condEntrySub));
                }

                // ── نافذة النجاح ──────────────────────────────────────
                Code = receipt.ReceiptNo;
                SetStatus(isNew
                    ? $"تم حفظ سند قبض رقم {Code} بنجاح"
                    : $"تم تعديل سند قبض رقم {Code} بنجاح");

                var savedForm = new frmSavedMsg();
                savedForm.ShowDialog();

                if (savedForm.Pressed == 1)
                {
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
            finally { SafeClose(conn1); }
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

            // ✅ إصلاح: استخدام ReceiptOper.DeleteReceipt مثل الأصل
            var receiptOper = new ReceiptOper();
            bool deleted = receiptOper.DeleteReceipt(GlobalID, EntryGlobalID);

            if (deleted)
            {
                DXMessageBox.Show("تم الحذف", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info($"تم حذف سند قبض برقم {txtNo.Text} بواسطة {MainClass.UserName}");

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

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReceipt(1);

        private void btnView_Click(object sender, RoutedEventArgs e) => PrintReceipt(2);

        // ✅ إصلاح: حفظ مسارات الملفات مثل الأصل
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
                ReceiptOper.filePathDocument = dialog.FileNames;
                SetStatus($"تم اختيار {dialog.FileNames.Length} ملف");
            }
        }

        // ✅ إصلاح: فتح frmshowdocument مثل الأصل
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

        private void IconButton1_Click(object sender, RoutedEventArgs e) =>
            OpenAccountSearch();

        #endregion

        #region RadioButton Events

        private void rdCash_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null) Panel1.Visibility = Visibility.Collapsed;
            LoadCashSources();
        }

        private void rdCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null) Panel1.Visibility = Visibility.Visible;
            LoadBankSources();
        }

        // ✅ إصلاح: إضافة rdPDeal_CheckedChanged المفقودة
        private void rdPDeal_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (rdEmp != null && rdEmp.IsChecked == true)
                    LoadEmps();
                else
                    LoadPersonsDeal();
            }
            catch { }
        }

        #endregion

        #region ComboBox / TextBox Events

        private void cmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { GetCustBalance(); }
            catch { }
        }

        private void cmbAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SearchByAccountName();
            }
        }

        private void txtVal_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            // ✅ إصلاح: التحقق من null قبل الاستخدام
            if (!_change1) return;
            if (txtValD == null) return;
            try
            {
                if (double.TryParse(txtVal.Text, out double val))
                    txtValD.Text = Math.Round(val / _Exchangeal, 2).ToString("N2");
            }
            catch { }
        }

        // ✅ إصلاح: إضافة txtValD_TextChanged المفقودة
        private void txtValD_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_change2) return;
            if (txtValD == null) return;
            try
            {
                if (double.TryParse(txtValD.Text, out double val))
                    txtVal.Text = Math.Round(val * _Exchangeal, 2).ToString("N2");
            }
            catch { }
        }

        private void txtVal_GotFocus(object sender, RoutedEventArgs e)
        {
            _change1 = true;
            _change2 = false;
        }

        // ✅ إصلاح: إضافة txtValD_GotFocus المفقودة
        private void txtValD_GotFocus(object sender, RoutedEventArgs e)
        {
            _change1 = false;
            _change2 = true;
        }

        private void txtTime_GotFocus(object sender, RoutedEventArgs e) =>
            txtTime.SelectAll();

        private void txtTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!TimeSpan.TryParse(txtTime.Text, out _))
                txtTime.Text = DateTime.Now.ToString("HH:mm");
        }

        private void txtSrchNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (!char.IsDigit(c)) { e.Handled = true; return; }
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

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is ReceiptQDItem selected)
            {
                GlobalID = selected.GlobalId;
                Navigate($"SELECT * FROM Receipts WHERE GlobalID = N'{GlobalID}'");
                TabControl1.SelectedIndex = 0;
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

                SafeOpen(conn);
                var adapter = new SqlDataAdapter(
                    $@"SELECT Code, AName, CostCenter
                       FROM Accounts_Index
                       WHERE AName = N'{SrchName}' AND type = 2",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                SafeClose(conn);

                if (dt.Rows.Count > 0)
                {
                    cmbAccounts.SelectedValue = dt.Rows[0]["Code"];
                    if (dt.Rows[0]["CostCenter"] != DBNull.Value
                        && Convert.ToInt32(dt.Rows[0]["CostCenter"]) != 0)
                        cmbCostCenter.SelectedValue = dt.Rows[0]["CostCenter"];
                }
                else
                    OpenAccountSearch();
            }
            catch (Exception ex) { SetStatus("خطأ في البحث: " + ex.Message); }
        }

        // ✅ إصلاح: تفعيل frmAccountSrch مثل الأصل
        private void OpenAccountSearch()
        {
            var frm = new frmAccountSrch();
            frm.txtSrchNm.Text = SrchName;
            frm.ShowDialog();

            if (frm.Code > -1)
            {
                try
                {
                    SafeOpen(conn);
                    var adapter = new SqlDataAdapter(
                        $@"SELECT Code, AName, CostCenter
                           FROM Accounts_Index
                           WHERE type=2 AND Code={frm.Code}",
                        conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbAccounts.SelectedValue = dt.Rows[0]["Code"];
                        if (dt.Rows[0]["CostCenter"] != DBNull.Value
                            && Convert.ToInt32(dt.Rows[0]["CostCenter"]) != 0)
                            cmbCostCenter.SelectedValue = dt.Rows[0]["CostCenter"];
                    }
                }
                catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
                finally { SafeClose(conn); }
            }
        }

        #endregion

        #region Print

        // ✅ إصلاح: إضافة BindToData المفقودة
        private DataSet BindToData()
        {
            try
            {
                SafeOpen(conn);
                var adapter = new SqlDataAdapter("SELECT * FROM Foundation", conn);
                var dtFoundation = new DataTable();
                adapter.Fill(dtFoundation);
                SafeClose(conn);

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

                // ✅ نوع الدفع مثل الأصل
                string bondType = rdCash.IsChecked == true
                    ? (rdCash.Content?.ToString() ?? "نقدي")
                    : (rdCheck.Content?.ToString() ?? "شيك");

                int currencyCode = Common.GetCurrencytosand(13);

                var bond = new Bond
                {
                    BondName = this.Title,
                    // ✅ في SandQD: PayTo=cmbDepositTO, PayFrom=cmbAccounts
                    PayToCode = cmbDepositTO.SelectedValue?.ToString() ?? "",
                    PayFromCode = cmbAccounts.SelectedValue?.ToString() ?? "",
                    PayTo = (cmbDepositTO.SelectedItem as DataRowView)
                                       ?["name"]?.ToString() ?? cmbDepositTO.Text,
                    Payfrom = (cmbAccounts.SelectedItem as DataRowView)
                                       ?["AName"]?.ToString() ?? cmbAccounts.Text,
                    CostCenter = (cmbCostCenter.SelectedItem as DataRowView)
                                       ?["name"]?.ToString() ?? cmbCostCenter.Text,
                    BondVal = txtVal.Text,
                    BondDate = (txtDate.DateTime != default(DateTime)
                                       ? txtDate.DateTime
                                       : DateTime.Now).ToShortDateString(),
                    BondNo = txtNo.Text,
                    ArabicLetter = InvoiceOper.ToArabicLetter(
                        Convert.ToDouble(
                            string.IsNullOrWhiteSpace(txtVal.Text) ? "0" : txtVal.Text),
                        (Currency)currencyCode),
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Note = txtNotes.Text,
                    AccountStatus = txtBalanceType.Text,
                    CurrentBalance = txtBalance.Text,
                    BondType = bondType,
                    SalesMan = (cmbSalesMan.SelectedItem as DataRowView)
                                        ?["name"]?.ToString() ?? cmbSalesMan.Text,
                    User = Editereciept.Text,
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

        // ✅ إصلاح: تفعيل الطباعة الفعلية مثل الأصل
        private void PrintReceipt(int printMode)
        {
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(print.RptName))
                print.RptName = "Receipt.repx";

            string fullPath = System.IO.Path.Combine(print.RptUrl, print.RptName);

            if (!Directory.Exists(print.RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show(
                    "المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            var data = BindToData();
            if (data == null) return;

            print.Printing(printMode, data, print.RptUrl, print.RptName,
                           print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        #endregion

        #region Helpers

        // ✅ إصلاح: SafeOpen/SafeClose مع التحقق من null
        private void SafeOpen(SqlConnection c)
        {
            if (c == null)
                throw new ArgumentNullException(nameof(c), "الاتصال غير مهيأ");
            if (c.State != ConnectionState.Open)
                c.Open();
        }

        private void SafeClose(SqlConnection c)
        {
            if (c != null && c.State != ConnectionState.Closed)
                c.Close();
        }

        private void SetStatus(string msg)
        {
            // يمكن ربطها بـ StatusBar أو Label في الـ XAML
            if (lblStatus != null)
                lblStatus.Text = msg;
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            SafeClose(conn);
            SafeClose(conn1);
            SafeClose(conn2);
        }

        #endregion
    }

    // ─── Models ─────────────────────────────────────────────────────────────────

    public class ReceiptQDItem1
    {
        public string GlobalId { get; set; }
        public string ReceiptNo { get; set; }
        public string ReceiptDate { get; set; }
        public string Payment { get; set; }
        public string EmployeeName { get; set; }
    }

    public class DocumentItem1
    {
        public int RowNum { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }
}