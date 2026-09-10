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
using System.Windows.Media;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using log4net;
using Microsoft.Win32;
using UtilitiesProj;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSandD : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int    Code         = -1;
        private string EntryGlobalID = "-1";
        public  string GlobalID      = "";
        public  string SrchName      = "";
        private int    ReceiptType   = 6;

        private Print print;
        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private bool _isInitialized = false;

        private double _Exchangeal = 1250.0;
        private bool   _change1    = true;
        private bool   _change2    = false;

        private ObservableCollection<ReceiptSearchRow> SearchList;

        #endregion

        #region Constructor

        public frmSandD()
        {
            // ✅ تهيئة الاتصالات أولاً قبل InitializeComponent
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            // ✅ فتح الاتصالات مسبقاً لضمان الجاهزية
            EnsureConnectionOpen(conn);
            EnsureConnectionOpen(conn1);

            print = new Print(10);
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";

            SearchList = new ObservableCollection<ReceiptSearchRow>();

            InitializeComponent();

            dgvSrch.ItemsSource = SearchList;

            // ✅ الآن الاتصال جاهز، نسمح بتنفيذ الأحداث
            _isInitialized = true;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(conn);
                EnsureConnectionOpen(conn1);

                txtFromDate.DateTime = DateTime.Today;
                txtToDate.DateTime = DateTime.Today;
                txtDate.DateTime = DateTime.Today;
                txtRefDate.DateTime = DateTime.Today;

                LoadEmps();
                LoadCostCenters();
                LoadClients();
                LoadSales();
                LoadBranches();

                RestorePreviousReceipt();

                txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();

                Editereciept.Text = MainClass.UserName;
                Editereciept.Foreground = Brushes.Red;

                // ✅ تحميل الصناديق هنا فقط (بعد اكتمال التهيئة)
                // rdCash_CheckedChanged ستُنفَّذ تلقائياً لأن _isInitialized=true الآن
                if (rdCash.IsChecked == true)
                    LoadTreasuriesCash();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل النافذة:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// يضمن أن الاتصال مفتوح قبل تنفيذ أي استعلام
        /// </summary>
        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection == null)
                throw new InvalidOperationException(
                    "الاتصال بقاعدة البيانات غير مهيأ.");

            if (connection.State == ConnectionState.Broken)
                connection.Close();

            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        #endregion

        #region CLR

        private void CLR()
        {
            cmbSupplies.SelectedIndex  = -1;
            cmbDepositTo.SelectedIndex = -1;
            cmbSalesMan.SelectedIndex  = -1;
            cmbCostCenter.SelectedIndex = -1;
            cmbEmp.SelectedIndex       = -1;

            txtVal.Text         = "";
            txtValD.Text        = "";
            txtNotes.Text       = "";
            txtBalance.Text     = "";
            txtBalanceType.Text = "";
            TxtRecived.Text     = "";
            txtRef.Text         = "";
            txtCheckNo.Text     = "";
            txtBankCheck.Text   = "";

            txtDate.DateTime    = DateTime.Today;
            txtRefDate.DateTime = DateTime.Today;
            txtCheckDate.DateTime = DateTime.Today;

            txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();
            Code       = -1;
            SrchName   = "";
            rdCash.IsChecked = true;

            Editereciept.Text       = MainClass.UserName;
            Editereciept.Foreground = Brushes.Red;
        }

        #endregion

        #region Load Helpers

        public void LoadEmps()
        {
            try
            {
                EnsureConnectionOpen(conn1);

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Employees " +
                    $"INNER JOIN EmpBranches ON Employees.id = EmpBranches.emp " +
                    $"WHERE EmpBranches.branch = {MainClass.BranchNo} " +
                    $"AND IS_Deleted = 0 " +
                    $"ORDER BY id",
                    conn1);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbEmp.DisplayMemberPath = "name";
                cmbEmp.SelectedValuePath = "id";
                cmbEmp.ItemsSource = dt.DefaultView;
                cmbEmp.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadEmps Error: {ex.Message}");
            }
        }

        public void LoadSales()
        {
            try
            {
                EnsureConnectionOpen(conn1);

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen " +
                    "WHERE IS_Deleted = 0 ORDER BY id",
                    conn1);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.ItemsSource = dt.DefaultView;
                cmbSalesMan.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSales Error: {ex.Message}");
            }
        }

        public void LoadClients()
        {
            try
            {
                EnsureConnectionOpen(conn);

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Customers " +
                    "WHERE (type = 2 OR type = 3) " +
                    "AND IS_Deleted = 0 ORDER BY id",
                    conn);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSupplies.DisplayMemberPath = "name";
                cmbSupplies.SelectedValuePath = "id";
                cmbSupplies.ItemsSource = dt.DefaultView;
                cmbSupplies.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadClients Error: {ex.Message}");
            }
        }

        private void LoadCostCenters()
        {
            try
            {
                EnsureConnectionOpen(conn);

                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center " +
                    "WHERE type = 2 AND Is_Deleted = 0 ORDER BY code",
                    conn);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCostCenters Error: {ex.Message}");
            }
        }

        private void LoadBranches()
        {
            try
            {
                EnsureConnectionOpen(conn);

                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches " +
                    "WHERE IS_Deleted = 0",
                    conn);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";
                cmbBranches.ItemsSource = dt.DefaultView;

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadBranches Error: {ex.Message}");
            }
        }

        private void LoadTreasuriesCash()
        {
            try
            {
                EnsureConnectionOpen(conn1);

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks " +
                    $"INNER JOIN Stock_Emps ON Stocks.id = Stock_Emps.stock_id " +
                    $"WHERE emp_id = {MainClass.EmpNo} " +
                    $"AND IS_Deleted = 0 " +
                    $"AND status <> 2 " +
                    $"ORDER BY id",
                    conn1);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "name";
                cmbDepositTo.SelectedValuePath = "id";
                cmbDepositTo.ItemsSource = dt.DefaultView;
                cmbDepositTo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل الصناديق:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBanks()
        {
            try
            {
                EnsureConnectionOpen(conn1);

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks " +
                    "WHERE IS_Deleted = 0 " +
                    "ORDER BY id",
                    conn1);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "name";
                cmbDepositTo.SelectedValuePath = "id";
                cmbDepositTo.ItemsSource = dt.DefaultView;
                cmbDepositTo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل البنوك:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAccounts()
        {
            try
            {
                EnsureConnectionOpen(conn1);

                var adapter = new SqlDataAdapter(
                    $"SELECT Code, AName FROM Accounts_Index " +
                    $"WHERE Type = 2 {Accounting.BranchCondition}",
                    conn1);

                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "AName";
                cmbDepositTo.SelectedValuePath = "Code";
                cmbDepositTo.ItemsSource = dt.DefaultView;
                cmbDepositTo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل الحسابات:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlStr)
        {
            dgvSrch.UnselectAll();
            var cmd = new SqlCommand(sqlStr, conn);
            if (conn.State != ConnectionState.Open) conn.Open();
            var dr = cmd.ExecuteReader();
            ReadData(dr);
            dr.Close();
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();
            CLR();

            Code            = Convert.ToInt32(dr["ReceiptNo"]);
            txtNo.Text      = Code.ToString();
            EntryGlobalID   = dr["EntryGlobalID"]?.ToString() ?? "-1";
            GlobalID        = dr["GlobalID"]?.ToString() ?? "";
            txtDate.DateTime = Convert.ToDateTime(dr["Receiptdate"]);

            if (cmbEmp.Items.Count > 0)
                cmbEmp.SelectedValue = dr["EmpId"].ToString();
            if (cmbSupplies.Items.Count > 0)
                cmbSupplies.SelectedValue = dr["ClientID"].ToString();

            txtVal.Text   = dr["Payment"]?.ToString() ?? "";
            txtNotes.Text = dr["Notes"]?.ToString() ?? "";

            if (cmbCostCenter.Items.Count > 0)
                cmbCostCenter.SelectedValue = dr["CCcode"];

            int payType = Convert.ToInt32(dr["Paymenttype"]);
            if (payType == 1)
            {
                rdCash.IsChecked = true;
                Panel1.Visibility = Visibility.Collapsed;
            }
            else if (payType == 2)
            {
                rdCheck.IsChecked = true;
                Panel1.Visibility = Visibility.Visible;
                txtCheckNo.Text   = dr["CheckNo"]?.ToString() ?? "";
                txtCheckDate.DateTime = Convert.ToDateTime(dr["CheckDate"]);
                txtBankCheck.Text = dr["Checkbank"]?.ToString() ?? "";
            }
            else
            {
                rdCheckAll.IsChecked = true;
            }

            if (cmbDepositTo.Items.Count > 0)
                cmbDepositTo.SelectedValue = dr["TreasuryID"].ToString();
            if (cmbSalesMan.Items.Count > 0)
                cmbSalesMan.SelectedValue = dr["SalesManID"].ToString();

            TxtRecived.Text = dr["Recipient"]?.ToString() ?? "";
            txtRef.Text     = dr["ReffNo"]?.ToString() ?? "";

            if (dr["Reffdate"] != DBNull.Value)
                txtRefDate.DateTime = Convert.ToDateTime(dr["Reffdate"]);

            if (dr["BranchID"] != DBNull.Value)
                cmbBranches.SelectedValue = dr["BranchID"];

            // اسم المحرر
            int empId = Convert.ToInt32(dr["EmpId"]);
            dr.Close();

            if (conn.State != ConnectionState.Closed) conn.Close();
            conn.Open();

            var empDr = new SqlCommand(
                $"SELECT username FROM users WHERE emp={empId}", conn)
                .ExecuteReader();
            if (empDr.Read())
            {
                Editereciept.Text       = empDr["username"]?.ToString() ?? "";
                Editereciept.Foreground = Brushes.Green;
            }
            empDr.Close();
            conn.Close();
        }

        private string GetCondBranch()
        {
            return MainClass.BranchNo != -1
                ? $"branchID={MainClass.BranchNo} and "
                : "";
        }

        private void Search()
        {
            SearchList.Clear();

            string cond = $" Receipts.ReceiptType={ReceiptType} and ";

            if (MainClass.BranchNo != -1)
                cond += $"Receipts.branchID={MainClass.BranchNo} and ";

            if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                cond += $" Receipts.ReceiptNo={txtSrchNo.Text} and ";
            else if (!string.IsNullOrWhiteSpace(txtSrchRef.Text))
                cond += $" Receipts.ReffNo={txtSrchRef.Text} and ";

            if (chkall.IsChecked != true)
                cond += " ReceiptDate between @StartDate and @EndDate and ";

            var dt = ReceiptOper.LoadReceipts(
                cond,
                txtFromDate.DateTime,
                txtToDate.DateTime).Copy();

            foreach (DataRow row in dt.Rows)
            {
                SearchList.Add(new ReceiptSearchRow
                {
                    GlobalId    = row["GlobalID"]?.ToString() ?? "",
                    ReceiptNo   = Convert.ToInt32(row["ReceiptNo"]),
                    ReceiptDate = Convert.ToDateTime(row["ReceiptDate"])
                                         .ToShortDateString(),
                    Payment     = Convert.ToDouble(row["Payment"]),
                    UserName    = row["name"]?.ToString() ?? "",
                });
            }

            dgvSrch.UnselectAll();
        }

        #endregion

        #region GetCustBalance

        private void GetCustBalance()
        {
            try
            {
                if (cmbSupplies.SelectedValue == null) return;

                int accNo = -1;
                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index " +
                    $"WHERE Type=2 AND AName=N'{cmbSupplies.Text}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    accNo = Convert.ToInt32(dt.Rows[0][0]);

                string condBranch = "";
                if (MainClass.BranchNo != -1)
                    condBranch =
                        $"Entry.branch={MainClass.BranchNo} AND " +
                        $"Entry_sub.branch={MainClass.BranchNo} AND ";

                var balAdapter = new SqlDataAdapter(
                    $"SELECT SUM(Entry_sub.dept) AS dept, " +
                    $"SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE {condBranch} Entry.IS_Deleted=0 AND Entry.state=1 " +
                    $"AND Entry.date<=@date2 " +
                    $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                    $"AND Entry_sub.acc_no={accNo}", conn);
                balAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    txtDate.DateTime.AddHours(24);

                var balDt = new DataTable();
                balAdapter.Fill(balDt);

                double dept   = 0, credit = 0;
                if (balDt.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(balDt.Rows[0][0]?.ToString()))
                {
                    dept   = Convert.ToDouble(balDt.Rows[0]["dept"]);
                    credit = Convert.ToDouble(balDt.Rows[0]["credit"]);
                }

                if (dept >= credit)
                {
                    txtBalance.Text     = Math.Round(dept - credit, 3).ToString();
                    txtBalanceType.Text = "مدين";
                }
                else
                {
                    txtBalance.Text     = Math.Round(credit - dept, 3).ToString();
                    txtBalanceType.Text = "دائن";
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region RestorePreviousReceipt

        private void RestorePreviousReceipt()
        {
            // نفس منطق WinForms — استعادة الوثائق المعلقة
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SandD WHERE branch={MainClass.BranchNo}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    // استعادة ومزامنة السند
                    var receipt = new Receipt
                    {
                        ReceiptNo   = Convert.ToInt32(row["id"]),
                        BranchID    = Convert.ToInt32(row["branch"]),
                        ReceiptType = ReceiptType,
                        State       = 1,
                        Payment     = Convert.ToDouble(row["val"]),
                        VAT         = 0,
                        NetVal      = Convert.ToDouble(row["val"]),
                        ISDeleted   = Convert.ToBoolean(row["IS_Deleted"]),
                        Notes       = row["notes"]?.ToString() ?? "",
                        ClientCode  = Sync.ClientCode,
                        BranchType  = Sync.BranchType,
                        DistBranch  = Sync.DistBranch,
                    };

                    if (new ReceiptOper().SaveReceipt2(receipt))
                    {
                        new SqlCommand(
                            $"DELETE FROM SandD WHERE id={receipt.ReceiptNo}",
                            conn).ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في RestorePreviousReceipt: {ex.Message}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region CheckBox / RadioButton Events

        private void rdCash_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // ✅ حماية: لا تنفذ قبل اكتمال التهيئة
            if (!_isInitialized) return;

            // ✅ ضمان أن الاتصال مفتوح
            EnsureConnectionOpen(conn1);

            try
            {
                if (rdCash?.IsChecked == true)
                {
                    if (Panel1 != null)
                        Panel1.Visibility = Visibility.Collapsed;
                    LoadTreasuriesCash();
                }
                else if (rdCheck?.IsChecked == true)
                {
                    if (Panel1 != null)
                        Panel1.Visibility = Visibility.Visible;
                    LoadBanks();
                }
                else if (rdCheckAll?.IsChecked == true)
                {
                    if (Panel1 != null)
                        Panel1.Visibility = Visibility.Collapsed;
                    LoadAccounts();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"rdCash_CheckedChanged Error: {ex.Message}");
            }
        }

        private void chkall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = chkall.IsChecked == true;
            txtFromDate.IsEnabled = !isAll;
            txtToDate.IsEnabled   = !isAll;
        }

        private void rdCheckAll_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1 && rdCheckAll.IsChecked == true)
                SearhcTreasury();
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Common.CheckProiedAcc(txtDate.DateTime))
                {
                    DXMessageBox.Show("لا يمكن أن يكون التاريخ خارج الفترة المحاسبية.",
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtVal.Text))
                {
                    DXMessageBox.Show("أدخل القيمة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtVal.Focus(); return;
                }

                if (MainClass.IsTrial && Code == -1 && Common.CheckIsTrial())
                {
                    DXMessageBox.Show("نأسف، وصلت لأقصى حد في النسخة التجريبية.",
                                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbSupplies.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار المورد.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbSupplies.Focus(); return;
                }

                if (cmbDepositTo.SelectedIndex == -1)
                {
                    DXMessageBox.Show(rdCash.IsChecked == true
                        ? "يجب اختيار الخزنة." : "يجب اختيار البنك.",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbDepositTo.Focus(); return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من حفظ السند؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                    txtNotes.Text = $"سند صرف رقم: {txtNo.Text} خاصة المورد: {cmbSupplies.Text}";

                bool isNew = Code == -1;

                int entryNo = 0;
                if (isNew)
                {
                    EntryOper.GetEntryGlobalID(ref EntryGlobalID, ref entryNo);

                    if (conn1.State != ConnectionState.Open) conn1.Open();
                    GlobalID = $"{MainClass.BranchNo}-" +
                        (Convert.ToInt32(new SqlCommand(
                            $"SELECT ISNULL(MAX(AutoID),0) FROM Receipts " +
                            $"WHERE BranchID={MainClass.BranchNo}",
                            conn1).ExecuteScalar()) + 1);

                    txtNo.Text = ReceiptOper.ReceiptNo(ReceiptType).ToString();
                }

                if (Code == -1)
                    Code = Convert.ToInt32(txtNo.Text);

                // جلب أكواد الحسابات
                int supplierAcc = 0, depositAcc = 0;

                var accAdapter1 = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 " +
                    $"{Accounting.BranchCondition} AND AName=N'{cmbSupplies.Text}'",
                    conn1);
                var dt1 = new DataTable();
                accAdapter1.Fill(dt1);
                if (dt1.Rows.Count > 0)
                    supplierAcc = Convert.ToInt32(dt1.Rows[0][0]);

                var accAdapter2 = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 " +
                    $"{Accounting.BranchCondition} AND AName=N'{cmbDepositTo.Text}'",
                    conn1);
                var dt2 = new DataTable();
                accAdapter2.Fill(dt2);
                if (dt2.Rows.Count > 0)
                    depositAcc = Convert.ToInt32(dt2.Rows[0][0]);

                int costCenterCode = -1;
                if (cmbCostCenter.SelectedIndex > -1)
                    costCenterCode = Convert.ToInt32(cmbCostCenter.SelectedValue);

                int salesManId = -1;
                if (cmbSalesMan.SelectedIndex != -1)
                    salesManId = Convert.ToInt32(cmbSalesMan.SelectedValue);

                var receipt = new Receipt
                {
                    GlobalID      = GlobalID,
                    ReceiptNo     = Convert.ToInt32(txtNo.Text),
                    BranchID      = isNew ? MainClass.BranchNo
                                          : Convert.ToInt32(cmbBranches.SelectedValue),
                    EntryGlobalID = EntryGlobalID,
                    ReceiptType   = 6,
                    State         = 1,
                    Payment       = Convert.ToDouble(txtVal.Text),
                    VAT           = 0,
                    NetVal        = Convert.ToDouble(txtVal.Text),
                    ReceiptDate   = txtDate.DateTime,
                    ReffNo        = txtRef.Text,
                    Reffdate      = txtRefDate.DateTime,
                    ISDeleted     = false,
                    Notes         = txtNotes.Text,
                    EmpId         = MainClass.EmpNo,
                    CreditAcc     = depositAcc.ToString(),
                    DebitAcc      = supplierAcc.ToString(),
                    ClientID      = Convert.ToInt32(cmbSupplies.SelectedValue),
                    BankId        = depositAcc,
                    TreasuryID    = Convert.ToInt32(cmbDepositTo.SelectedValue),
                    SalesManID    = salesManId,
                    Cccode        = costCenterCode.ToString(),
                    Sync          = false,
                    ClientCode    = Sync.ClientCode,
                    BranchType    = Sync.BranchType,
                    DistBranch    = Sync.DistBranch,
                    Recipient     = TxtRecived.Text,
                };

                // طريقة الدفع
                if (rdCheck.IsChecked == true)
                {
                    receipt.PaymentType = 2;
                    receipt.CheckNo     = txtCheckNo.Text;
                    receipt.CheckDate   = txtCheckDate.DateTime;
                    receipt.Checkbank   = txtBankCheck.Text;
                    receipt.CheckState  = cmbCheckType.SelectedIndex + 1;
                }
                else if (rdCash.IsChecked == true)
                {
                    receipt.PaymentType = 1;
                    receipt.CheckState  = 1;
                    receipt.CheckDate   = txtDate.DateTime;
                    receipt.CheckNo     = "-1";
                    receipt.Checkbank   = "";
                }
                else
                {
                    receipt.PaymentType = 3;
                    receipt.CheckState  = 1;
                    receipt.CheckDate   = txtDate.DateTime;
                    receipt.CheckNo     = "-1";
                    receipt.Checkbank   = "";
                }

                var receiptOper = new ReceiptOper();
                var entry       = receiptOper.BindReceiptToEntry(receipt);

                if (!receiptOper.SaveReceipt(receipt, entry, isNew))
                {
                    DXMessageBox.Show("خطأ أثناء الحفظ.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(
                    (isNew ? "تم حفظ" : "تم تعديل") +
                    $" سند صرف لمورد برقم {receipt.ReceiptNo} " +
                    $"بواسطة المستخدم {MainClass.UserName}");

                if (Sync.ActiveSync && Sync.SyncType > 0)
                    await receiptOper.SyncReceipt(receipt, entry, isNew);
                var Home = new Home();
                if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                {
                    SendData.SendDataa("Receipts", receipt.GlobalID,
                        SendData.GetReceipts($"where GlobalID=N'{receipt.GlobalID}'"));
                    SendData.SendDataa("entry", receipt.EntryGlobalID,
                        SendData.GetEntryData($"where GlobalID=N'{receipt.EntryGlobalID}'"));
                    SendData.SendDataa("entryDetails", receipt.EntryGlobalID,
                        SendData.GetEntrySubData($"where EntryGlobalID=N'{receipt.EntryGlobalID}'"));
                }

                Code = Convert.ToInt32(txtNo.Text);

                var result = DXMessageBox.Show(
                    "تم الحفظ بنجاح.\n\nهل تريد إدخال سند جديد؟",
                    "تم", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    SearchList.Clear();
                    CLR();
                }
                else if (result == MessageBoxResult.Cancel)
                    Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State  != ConnectionState.Closed) conn.Close();
                if (conn1.State != ConnectionState.Closed) conn1.Close();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر سنداً ليتم حذفه.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من حذف السند؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.No) return;

            if (new ReceiptOper().DeleteReceipt(GlobalID, EntryGlobalID))
            {
                DXMessageBox.Show("تم الحذف بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(
                    $"تم حذف سند صرف لمورد برقم {txtNo.Text} " +
                    $"بواسطة المستخدم {MainClass.UserName}");
                SearchList.Clear();
                CLR();
            }
            else
            {
                DXMessageBox.Show("خطأ أثناء الحذف.", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Receipts WHERE " +
                        $"{GetCondBranch()} ReceiptType={ReceiptType} " +
                        $"AND ISDeleted=0 ORDER BY ReceiptNo ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Receipts WHERE " +
                        $"{GetCondBranch()} ReceiptType={ReceiptType} " +
                        $"AND ISDeleted=0 AND ReceiptNo<{Convert.ToInt32(txtNo.Text)} " +
                        $"ORDER BY ReceiptNo DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Receipts WHERE " +
                        $"{GetCondBranch()} ReceiptType={ReceiptType} " +
                        $"AND ISDeleted=0 AND ReceiptNo>{Convert.ToInt32(txtNo.Text)} " +
                        $"ORDER BY ReceiptNo ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Receipts WHERE " +
                        $"{GetCondBranch()} ReceiptType={ReceiptType} " +
                        $"AND ISDeleted=0 ORDER BY ReceiptNo DESC");

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => Search();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => RptPrint(1);

        private void btnView_Click(object sender, RoutedEventArgs e)
            => RptPrint(2);

        private void btnClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmCustomers();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Title = "تعريف مورد";
                form.Type = 2;
                form.ShowDialog();
                if (form.isDone) LoadClients();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSearchAcc_Click(object sender, RoutedEventArgs e)
            => addNewItem();

        private void BtnImportDocument_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter      = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF Files (*.pdf)|*.pdf",
                Title       = "تحديد الملفات",
                Multiselect = true,
            };

            string dataPath = System.IO.Path.Combine(
                System.Windows.Forms.Application.StartupPath, "Data");
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            if (dlg.ShowDialog() == true)
                ReceiptOper.filePathDocument = dlg.FileNames;
        }

        private void Btnshowdocument_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("يجب حفظ السند أولاً.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dtt = new DataTable();
            ReceiptOper.getdocuments(ref dtt, GlobalID);

            if (dtt.Rows.Count > 0)
            {
                var form = new Frmshowdocument();

                // تجهيز جدول عرض مناسب لـ WPF DataGrid
                var viewTable = new DataTable();
                viewTable.Columns.Add("No", typeof(int));
                viewTable.Columns.Add("FileName", typeof(string));
                viewTable.Columns.Add("FileUrl", typeof(string));

                int i = 0;
                foreach (DataRow row in dtt.Rows)
                {
                    viewTable.Rows.Add(
                        ++i,
                        row["FileName"]?.ToString(),
                        row["FileUrl"]?.ToString());
                }

                form.DataGridView1.ItemsSource = viewTable.DefaultView;
                form.type = 3;
                form.GlobalIdDoc = GlobalID;
                form.Show();
            }
            else
            {
                DXMessageBox.Show("لا يوجد مستندات.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region ComboBox / TextBox Events

        private void cmbSupplies_SelectionChanged(object sender,
                                                    SelectionChangedEventArgs e)
        {
            GetCustBalance();
        }

        private void cmbSupplies_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SearchByName();
            }
        }

        private void txtVal_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_change1 && double.TryParse(txtVal.Text, out double v))
                    txtValD.Text = Math.Round(v * _Exchangeal, 2).ToString();
            }
            catch { /* تجاهل */ }
        }

        private void txtValD_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_change2 && double.TryParse(txtValD.Text, out double v))
                    txtVal.Text = Math.Round(v / _Exchangeal, 2).ToString();
            }
            catch { /* تجاهل */ }
        }

        private void txtVal_Enter(object sender, RoutedEventArgs e)
        {
            _change1 = true;
            _change2 = false;
        }

        private void txtValD_Enter(object sender, RoutedEventArgs e)
        {
            _change1 = false;
            _change2 = true;
        }

        private void cmbDepositTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (rdCheckAll.IsChecked == true && e.Key == Key.Return)
                SearhcTreasury();
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is ReceiptSearchRow row)
            {
                GlobalID = row.GlobalId;
                Navigate($"SELECT * FROM receipts WHERE GlobalID=N'{GlobalID}'");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Window KeyDown

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var element = Keyboard.FocusedElement as UIElement;
                element?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        #endregion

        #region Item / Account Search

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE IS_Deleted=0 AND name=N'{cmbSupplies.Text}' " +
                    $"AND (type=1 OR type=3)", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbSupplies.SelectedValue = dt.Rows[0]["id"];
                else
                    addNewItem();
            }
            catch { /* تجاهل */ }
        }

        private void addNewItem()
        {
            try
            {
                var form = new frmSrchClient();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.Background = System.Windows.Media.Brushes.WhiteSmoke;
                form.lblName.Foreground = System.Windows.Media.Brushes.Black;
                form.lblMobile.Foreground = System.Windows.Media.Brushes.Black;

                form.Type = 2;
                form.txtClientName.Text = cmbSupplies.Text;
                form.ShowDialog();

                if (!string.IsNullOrEmpty(form.Clientname))
                {
                    LoadClients();
                    cmbSupplies.SelectedValue = form.ClientId;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearhcTreasury()
        {
            try
            {
                var form = new frmAccountSrch();
                form.ShowDialog();
                if (form.Code > -1)
                {
                    var adapter = new SqlDataAdapter(
                        $"SELECT Code, AName FROM Accounts_Index " +
                        $"WHERE type=2 AND Code={form.Code}", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                        cmbDepositTo.SelectedValue = dt.Rows[0]["Code"];
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region Print

        public void RptPrint(int printType)
        {
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "Payment.repx";

            string path = System.IO.Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !System.IO.File.Exists(path))
            {
                DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(printType, BindToData(), print.RptUrl,
                           print.RptName, print.defPrinter,
                           print.kitchenprinter, print.PrintNo);
        }

        private DataSet BindToData()
        {
            var adapter = new SqlDataAdapter("SELECT * FROM Foundation", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            string address = "", tel = "", mobile = "", foundation = "",
                   field   = "", vatNo = "";

            if (dt.Rows.Count > 0)
            {
                address    = dt.Rows[0]["Address"]?.ToString() ?? "";
                tel        = dt.Rows[0]["Tel"]?.ToString() ?? "";
                mobile     = dt.Rows[0]["Mobile"]?.ToString() ?? "";
                foundation = dt.Rows[0]["nameA"]?.ToString() ?? "";
                field      = dt.Rows[0]["FieldA"]?.ToString() ?? "";
                vatNo      = dt.Rows[0]["tax_no"]?.ToString() ?? "";
            }

            string bondType = rdCash.IsChecked == true ? "نقدي" : "بنكي";
            double netVal   = double.TryParse(txtVal.Text, out double v) ? v : 0;
            int    currency = Common.GetCurrencytosand(13);

            var list = new List<Bond>
            {
                new Bond
                {
                    BondName       = Title,
                    PayToCode      = cmbSupplies.SelectedValue?.ToString() ?? "",
                    PayFromCode    = cmbDepositTo.SelectedValue?.ToString() ?? "",
                    PayTo          = cmbSupplies.Text,
                    Payfrom        = cmbDepositTo.Text,
                    CostCenter     = cmbCostCenter.Text,
                    BondVal        = txtVal.Text,
                    BondDate       = txtDate.DateTime.ToShortDateString(),
                    BondNo         = txtNo.Text,
                    PrintDate      = DateTime.Now.ToShortDateString(),
                    Note           = txtNotes.Text,
                    AccountStatus  = txtBalanceType.Text,
                    CurrentBalance = txtBalance.Text,
                    BondType       = bondType,
                    SalesMan       = cmbSalesMan.Text,
                    User           = Editereciept.Text,
                    ArabicLetter   = InvoiceOper.ToArabicLetter(netVal, (Currency)currency),
                    reffNo         = txtRef.Text,
                    reffDate       = txtRefDate.DateTime.ToShortDateString(),
                    Branch         = MainClass.BranchName,
                    Address        = address,
                    Mobile         = mobile,
                    Telephone      = tel,
                    VATNo          = vatNo,
                    Foundation     = foundation,
                    Field          = field,
                    Logo           = "",
                    Header         = "",
                    Footer         = "",
                    Stamp          = "",
                }
            };

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion
    }
}