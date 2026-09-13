using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSandQ : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        public bool ISTailor;
        private int SelectedId;
        public string SrchName;
        public string GlobalID;
        private string EntryGlobalID;
        private int ReceiptType;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        // معدل التحويل للدولار
        private double _Exchangeal;
        private bool _change1;
        private bool _change2;

        // مصدر بيانات جدول البحث
        private ObservableCollection<ReceiptSrchItem> _searchItems;

        #endregion

        #region Constructor

        public frmSandQ()
        {
            InitializeComponent();

            conn          = MainClass.ConnObj();
            conn1         = MainClass.ConnObj();
            Code          = -1;
            ISTailor      = false;
            SelectedId    = -1;
            SrchName      = "";
            GlobalID      = "";
            EntryGlobalID = "";
            ReceiptType   = 5;
            PrintHeader   = true;
            PrintFooter   = true;
            PrintStamp    = true;
            PrintNo       = 1;
            RptName       = "";
            RptUrl        = "";
            _Exchangeal   = 1250.0;
            _change1      = true;
            _change2      = false;

            _searchItems = new ObservableCollection<ReceiptSrchItem>();
            dgvSrch.ItemsSource = _searchItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDate.DateTime     = DateTime.Now;
                txtRefDate.DateTime  = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;
                txtToDate.DateTime   = DateTime.Now;
                txtTime.Text         = DateTime.Now.ToString("HH:mm");

                LoadClients();
                LoadCostCenters();
                LoadSalesMen();
                LoadBranches();
                LoadPrintSettings();
                RestorePreviousReceipt();

                txtNo.Text              = GetNextReceiptNo().ToString();
                Editereciept.Text       = MainClass.UserName;
                Editereciept.Foreground = System.Windows.Media.Brushes.Red;

                // تحميل الخزن الافتراضي (نقدي)
                rdCash.IsChecked = true;
                LoadCashSources();

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
            txtNo.Text       = GetNextReceiptNo().ToString();
            txtVal.Text      = "";
            txtValD.Text     = "";
            txtRef.Text      = "";
            txtNotes.Text    = "";
            txtBalance.Text  = "";
            txtBalanceType.Text = "";
            txtCheckNo.Text  = "";
            txtBankCheck.Text = "";
            SrchName         = "";
            GlobalID         = "";
            EntryGlobalID    = "";
            Code             = -1;

            txtDate.DateTime    = DateTime.Now;
            txtRefDate.DateTime = DateTime.Now;
            txtTime.Text        = DateTime.Now.ToString("HH:mm");

            cmbClients.SelectedIndex    = -1;
            cmbDepositTO.SelectedIndex  = -1;
            cmbSalesMan.SelectedIndex   = -1;
            cmbCostCenter.SelectedIndex = -1;
            cmbBranches.SelectedValue   = MainClass.BranchNo;
            cmbCheckType.SelectedIndex  = -1;

            rdCash.IsChecked    = true;
            Panel1.Visibility   = Visibility.Collapsed;

            Editereciept.Text       = MainClass.UserName;
            Editereciept.Foreground = System.Windows.Media.Brushes.Red;
        }

        #endregion

        #region Data Loading

        private int GetNextReceiptNo()
        {
            try
            {
                EnsureOpen(conn1);
                string sql = $@"SELECT ISNULL(MAX(ReceiptNo), 0) + 1
                                FROM Receipts
                                WHERE ReceiptType = {ReceiptType}
                                  AND BranchID = {MainClass.BranchNo}";
                var cmd    = new SqlCommand(sql, conn1);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToInt32(result)
                    : 1;
            }
            catch
            {
                return 1;
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        public void LoadClients()
        {
            try
            {
                EnsureOpen(conn);
                string sql = @"SELECT id, name FROM Customers
                               WHERE (type=1 OR type=3) AND IS_Deleted=0
                               ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbClients.ItemsSource       = dt.DefaultView;
                cmbClients.DisplayMemberPath = "name";
                cmbClients.SelectedValuePath = "id";
                cmbClients.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل العملاء: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadSalesMen()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMan.ItemsSource       = dt.DefaultView;
                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل المندوبين: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadCostCenters()
        {
            try
            {
                EnsureOpen(conn);
                string sql = @"SELECT code, name FROM cost_center
                               WHERE type=2 AND Is_Deleted=0 ORDER BY code";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.ItemsSource       = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل مراكز التكلفة: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";

                cmbBranches.SelectedValue = MainClass.BranchNo != -1
                    ? (object)MainClass.BranchNo
                    : null;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الفروع: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadCashSources()
        {
            try
            {
                EnsureOpen(conn1);
                string sql = $@"SELECT id, name FROM Stocks
                                INNER JOIN Stock_Emps ON Stocks.id = Stock_Emps.stock_id
                                WHERE emp_id = {MainClass.EmpNo}
                                  AND IS_Deleted = 0
                                  AND status <> 2
                                ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTO.ItemsSource       = dt.DefaultView;
                cmbDepositTO.DisplayMemberPath = "name";
                cmbDepositTO.SelectedValuePath = "id";
                cmbDepositTO.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الخزن: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        private void LoadBankSources()
        {
            try
            {
                EnsureOpen(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTO.ItemsSource       = dt.DefaultView;
                cmbDepositTO.DisplayMemberPath = "name";
                cmbDepositTO.SelectedValuePath = "id";
                cmbDepositTO.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل البنوك: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        private void LoadAccountSources()
        {
            try
            {
                EnsureOpen(conn1);
                string branchCond = MainClass.BranchNo != -1
                    ? $" AND Branch = {MainClass.BranchNo}"
                    : "";
                string sql = $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {branchCond}";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTO.ItemsSource       = dt.DefaultView;
                cmbDepositTO.DisplayMemberPath = "AName";
                cmbDepositTO.SelectedValuePath = "Code";
                cmbDepositTO.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الحسابات: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=11", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pType))
                        PrintType = pType;
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();
                    if (string.IsNullOrWhiteSpace(defPrinter))
                        defPrinter = MainClass.ReportsPrinter;
                    if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pNo))
                        PrintNo = pNo;
                    RptName = dt.Rows[0]["RptName"].ToString();
                    RptUrl  = MainClass.ReportsPath;
                }
                else
                {
                    RptUrl     = MainClass.ReportsPath;
                    defPrinter = MainClass.ReportsPrinter;
                    RptName    = "Receipt.repx";
                }
            }
            catch
            {
                RptName = "Receipt.repx";
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Restore Previous

        private void RestorePreviousReceipt()
        {
            try
            {
                EnsureOpen(conn);
                string sql = $"SELECT * FROM SandQ WHERE branch={MainClass.BranchNo}";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        int receiptNo = Convert.ToInt32(row["id"]);
                        new SqlCommand(
                            $"DELETE FROM SandQ WHERE id={receiptNo}",
                            conn).ExecuteNonQuery();
                    }
                    catch
                    {
                        // تجاهل خطأ الصف الواحد
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في الاستعادة: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Customer Balance

        private void GetCustBalance()
        {
            try
            {
                if (cmbClients.SelectedIndex == -1) return;

                string clientName = (cmbClients.SelectedItem as DataRowView)?["name"]?.ToString()
                                    ?? cmbClients.Text;

                EnsureOpen(conn);

                var codeAdapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName=N'{clientName}'", conn);
                var codeDt = new DataTable();
                codeAdapter.Fill(codeDt);

                int accCode = -1;
                if (codeDt.Rows.Count > 0)
                    int.TryParse(codeDt.Rows[0]["Code"].ToString(), out accCode);

                if (accCode == -1)
                {
                    txtBalance.Text = "";
                    txtBalanceType.Text = "";
                    EnsureClose(conn);
                    return;
                }

                string branchCond = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} AND Entry_sub.branch={MainClass.BranchNo} AND "
                    : "";

                string balanceSql = $@"SELECT SUM(Entry_sub.dept) AS dept,
                                      SUM(Entry_sub.credit) AS credit
                               FROM Entry
                               INNER JOIN Entry_sub ON Entry.GlobalID = Entry_sub.EntryGlobalID
                               WHERE {branchCond}
                                     Entry.IS_Deleted = 0
                                 AND Entry.state = 1
                                 AND Entry.date <= @date2
                                 AND Entry_sub.acc_no = {accCode}";

                var balanceCmd = new SqlCommand(balanceSql, conn);

                // ✅ إصلاح: DateTime لا يدعم ??
                DateTime selectedDate = txtDate.DateTime != default(DateTime)
                    ? txtDate.DateTime
                    : DateTime.Now;
                balanceCmd.Parameters.AddWithValue("@date2", selectedDate.AddHours(24));

                var balanceAdapter = new SqlDataAdapter(balanceCmd);
                var balanceDt = new DataTable();
                balanceAdapter.Fill(balanceDt);

                double dept = 0;
                double credit = 0;

                if (balanceDt.Rows.Count > 0
                    && !string.IsNullOrWhiteSpace(balanceDt.Rows[0][0].ToString()))
                {
                    double.TryParse(balanceDt.Rows[0]["dept"].ToString(), out dept);
                    double.TryParse(balanceDt.Rows[0]["credit"].ToString(), out credit);
                }

                if (dept >= credit)
                {
                    txtBalance.Text = Math.Round(dept - credit, 3).ToString();
                    txtBalanceType.Text = "مدين";
                    txtBalanceType.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    txtBalance.Text = Math.Round(credit - dept, 3).ToString();
                    txtBalanceType.Text = "دائن";
                    txtBalanceType.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في حساب الرصيد: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Search

        private void Search()
        {
            try
            {
                _searchItems.Clear();

                string condition = $" Receipts.ReceiptType = {ReceiptType} AND ";

                if (MainClass.BranchNo != -1)
                    condition += $" Receipts.BranchID = {MainClass.BranchNo} AND ";

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    condition += $" Receipts.ReceiptNo = {txtSrchNo.Text} AND ";
                else if (!string.IsNullOrWhiteSpace(txtSrchRef.Text))
                    condition += $" Receipts.ReffNo = '{txtSrchRef.Text}' AND ";

                if (chkall.IsChecked != true)
                    condition += " ReceiptDate BETWEEN @StartDate AND @EndDate AND ";

                string sql = $@"SELECT Receipts.GlobalID,
                               Receipts.ReceiptNo,
                               Receipts.ReceiptDate,
                               Receipts.Payment,
                               ISNULL(Customers.name, '') AS name
                        FROM Receipts
                        LEFT JOIN Customers ON Receipts.ClientID = Customers.id
                        WHERE {condition} Receipts.ISDeleted = 0
                        ORDER BY Receipts.ReceiptNo DESC";

                EnsureOpen(conn);
                var cmd = new SqlCommand(sql, conn);

                if (chkall.IsChecked != true)
                {
                    // ✅ إصلاح: DateTime لا يدعم ??
                    cmd.Parameters.AddWithValue("@StartDate",
                        txtFromDate.DateTime != default(DateTime)
                            ? txtFromDate.DateTime
                            : DateTime.Today);

                    cmd.Parameters.AddWithValue("@EndDate",
                        txtToDate.DateTime != default(DateTime)
                            ? txtToDate.DateTime
                            : DateTime.Today);
                }

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _searchItems.Add(new ReceiptSrchItem
                    {
                        GlobalId = row["GlobalID"].ToString(),
                        ReceiptNo = row["ReceiptNo"].ToString(),
                        ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString(),
                        Payment = row["Payment"].ToString(),
                        ClientName = row["name"].ToString()
                    });
                }

                SetStatus($"تم العثور على {_searchItems.Count} نتيجة");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var cmd    = new SqlCommand(sqlQuery, conn);
                var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

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

            Code             = Convert.ToInt32(reader["ReceiptNo"]);
            txtNo.Text       = Code.ToString();
            EntryGlobalID    = reader["EntryGlobalID"].ToString();
            GlobalID         = reader["GlobalID"].ToString();
            txtNotes.Text    = reader["Notes"].ToString();
            txtVal.Text      = reader["Payment"].ToString();

            if (reader["Receiptdate"] != DBNull.Value)
                txtDate.DateTime = Convert.ToDateTime(reader["Receiptdate"]);

            if (reader["ClientID"] != DBNull.Value)
                cmbClients.SelectedValue = reader["ClientID"];

            if (reader["CCcode"] != DBNull.Value)
                cmbCostCenter.SelectedValue = reader["CCcode"];

            if (reader["ReffNo"] != DBNull.Value)
                txtRef.Text = reader["ReffNo"].ToString();

            if (reader["Reffdate"] != DBNull.Value)
                txtRefDate.DateTime = Convert.ToDateTime(reader["Reffdate"]);

            if (reader["BranchID"] != DBNull.Value)
                cmbBranches.SelectedValue = reader["BranchID"];

            if (reader["TreasuryID"] != DBNull.Value)
                cmbDepositTO.SelectedValue = reader["TreasuryID"];

            if (reader["SalesManID"] != DBNull.Value)
                cmbSalesMan.SelectedValue = reader["SalesManID"];

            // نوع الدفع
            if (reader["Paymenttype"] != DBNull.Value)
            {
                int payType = Convert.ToInt32(reader["Paymenttype"]);
                if (payType == 1)
                {
                    rdCash.IsChecked    = true;
                    Panel1.Visibility   = Visibility.Collapsed;
                }
                else if (payType == 2)
                {
                    rdCheck.IsChecked   = true;
                    Panel1.Visibility   = Visibility.Visible;

                    txtCheckNo.Text = reader["CheckNo"] != DBNull.Value
                        ? reader["CheckNo"].ToString() : "";
                    txtBankCheck.Text = reader["Checkbank"] != DBNull.Value
                        ? reader["Checkbank"].ToString() : "";

                    if (reader["CheckDate"] != DBNull.Value)
                        txtCheckDate.DateTime = Convert.ToDateTime(reader["CheckDate"]);
                }
                else
                {
                    rdCheckAll.IsChecked = true;
                    Panel1.Visibility    = Visibility.Collapsed;
                }
            }

            reader.Close();

            // تحميل اسم المحرر
            LoadEditorName(reader["EmpId"] != DBNull.Value
                ? Convert.ToInt32(reader["EmpId"]) : 0);

            SetStatus($"السند رقم {Code}");
            TabControl1.SelectedIndex = 0;
        }

        private void LoadEditorName(int empId)
        {
            try
            {
                EnsureOpen(conn);
                var cmd    = new SqlCommand(
                    $"SELECT username FROM users WHERE emp={empId}", conn);
                var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    Editereciept.Text       = reader["username"].ToString();
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
                EnsureClose(conn);
            }
        }

        private string GetBranchCondition()
        {
            return MainClass.BranchNo != -1
                ? $"BranchID = {MainClass.BranchNo} AND "
                : "";
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
                        WHERE {GetBranchCondition()} ReceiptType={ReceiptType}
                          AND ISDeleted=0
                        ORDER BY ReceiptNo ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType={ReceiptType}
                          AND ISDeleted=0 AND ReceiptNo < {no}
                        ORDER BY ReceiptNo DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType={ReceiptType}
                          AND ISDeleted=0 AND ReceiptNo > {no}
                        ORDER BY ReceiptNo ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM Receipts
                        WHERE {GetBranchCondition()} ReceiptType={ReceiptType}
                          AND ISDeleted=0
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
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("ادخل القيمة", "",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                txtVal.Focus();
                return;
            }

            if (!double.TryParse(txtVal.Text, out double paymentVal) || paymentVal <= 0)
            {
                DXMessageBox.Show("القيمة المدخلة غير صحيحة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtVal.Focus();
                return;
            }

            if (cmbClients.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار العميل", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbClients.Focus();
                return;
            }

            if (cmbDepositTO.SelectedIndex == -1)
            {
                string msg = rdCash.IsChecked == true ? "يجب اختيار الخزنة" : "يجب اختيار البنك";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbDepositTO.Focus();
                return;
            }

            var confirm = DXMessageBox.Show("هل أنت متأكد من حفظ السند؟", "تأكيد",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                EnsureOpen(conn1);

                bool isNew = (Code == -1);

                if (isNew)
                    txtNo.Text = GetNextReceiptNo().ToString();

                if (!int.TryParse(txtNo.Text, out int receiptNo))
                    receiptNo = 1;

                Code = receiptNo;

                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                    txtNotes.Text = $"سند قبض رقم: {txtNo.Text} خاصة العميل: " +
                                    $"{(cmbClients.SelectedItem as DataRowView)?["name"]}";

                int paymentType = rdCash.IsChecked == true ? 1
                                : rdCheck.IsChecked == true ? 2
                                : 3;

                // ✅ إصلاح: DateTime لا يدعم HasValue و Value
                DateTime selectedDate = txtDate.DateTime != default(DateTime)
                    ? txtDate.DateTime
                    : DateTime.Now;
                TimeSpan.TryParse(txtTime.Text, out TimeSpan timeVal);
                DateTime receiptDateTime = selectedDate.Date + timeVal;

                int salesManId = -1;
                if (cmbSalesMan.SelectedValue != null)
                    int.TryParse(cmbSalesMan.SelectedValue.ToString(), out salesManId);

                int costCenterCode = -1;
                if (cmbCostCenter.SelectedValue != null)
                    int.TryParse(cmbCostCenter.SelectedValue.ToString(), out costCenterCode);

                SqlTransaction transaction = conn1.BeginTransaction();

                try
                {
                    string upsertSql = isNew
                        ? @"INSERT INTO Receipts
                    (ReceiptNo, BranchID, ReceiptType, State, Payment, VAT, NetVal,
                     ReceiptDate, ReffNo, Reffdate, ISDeleted, Notes, EmpId,
                     DebitAcc, TreasuryID, SalesManID, PaymentType, CheckNo,
                     CheckDate, Checkbank, CheckState, CCcode, ClientID)
                    VALUES
                    (@ReceiptNo,@BranchID,@ReceiptType,1,@Payment,0,@Payment,
                     @ReceiptDate,@ReffNo,@Reffdate,0,@Notes,@EmpId,
                     @DebitAcc,@TreasuryID,@SalesManID,@PaymentType,@CheckNo,
                     @CheckDate,@Checkbank,@CheckState,@CCcode,@ClientID)"
                        : @"UPDATE Receipts SET
                    Payment=@Payment, NetVal=@Payment,
                    ReceiptDate=@ReceiptDate, ReffNo=@ReffNo, Reffdate=@Reffdate,
                    Notes=@Notes, DebitAcc=@DebitAcc, TreasuryID=@TreasuryID,
                    SalesManID=@SalesManID, PaymentType=@PaymentType,
                    CheckNo=@CheckNo, CheckDate=@CheckDate, Checkbank=@Checkbank,
                    CheckState=@CheckState, CCcode=@CCcode, ClientID=@ClientID
                    WHERE ReceiptNo=@ReceiptNo AND BranchID=@BranchID";

                    var cmd = new SqlCommand(upsertSql, conn1, transaction);

                    cmd.Parameters.AddWithValue("@ReceiptNo", Code);
                    cmd.Parameters.AddWithValue("@BranchID", MainClass.BranchNo);
                    cmd.Parameters.AddWithValue("@ReceiptType", ReceiptType);
                    cmd.Parameters.AddWithValue("@Payment", paymentVal);
                    cmd.Parameters.AddWithValue("@ReceiptDate", receiptDateTime);
                    cmd.Parameters.AddWithValue("@ReffNo", txtRef.Text);

                    // ✅ إصلاح: txtRefDate.DateTime لا يدعم HasValue و Value
                    cmd.Parameters.AddWithValue("@Reffdate",
                        txtRefDate.DateTime != default(DateTime)
                            ? (object)txtRefDate.DateTime
                            : DBNull.Value);

                    cmd.Parameters.AddWithValue("@Notes", txtNotes.Text);
                    cmd.Parameters.AddWithValue("@EmpId", MainClass.EmpNo);
                    cmd.Parameters.AddWithValue("@DebitAcc", cmbDepositTO.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TreasuryID", cmbDepositTO.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SalesManID", salesManId == -1
                                                                    ? (object)DBNull.Value
                                                                    : salesManId);
                    cmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    cmd.Parameters.AddWithValue("@CCcode", costCenterCode == -1
                                                                    ? (object)DBNull.Value
                                                                    : costCenterCode);
                    cmd.Parameters.AddWithValue("@ClientID", cmbClients.SelectedValue ?? DBNull.Value);

                    if (paymentType == 2)
                    {
                        cmd.Parameters.AddWithValue("@CheckNo", txtCheckNo.Text);

                        // ✅ إصلاح: txtCheckDate.DateTime لا يدعم HasValue و Value
                        cmd.Parameters.AddWithValue("@CheckDate",
                            txtCheckDate.DateTime != default(DateTime)
                                ? (object)txtCheckDate.DateTime
                                : DBNull.Value);

                        cmd.Parameters.AddWithValue("@Checkbank", txtBankCheck.Text);
                        cmd.Parameters.AddWithValue("@CheckState", cmbCheckType.SelectedIndex + 1);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@CheckNo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CheckDate", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Checkbank", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CheckState", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    SetStatus(isNew
                        ? $"تم حفظ سند قبض رقم {Code} بنجاح"
                        : $"تم تعديل سند قبض رقم {Code} بنجاح");

                    var saved = DXMessageBox.Show(
                        $"تم الحفظ بنجاح\nهل تريد إضافة سند جديد؟",
                        "تم الحفظ", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (saved == MessageBoxResult.Yes)
                    {
                        _searchItems.Clear();
                        CLR();
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاتصال\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn1);
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

            var confirm = DXMessageBox.Show("هل أنت متأكد من حذف السند؟", "تأكيد الحذف",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand(
                    $"UPDATE Receipts SET ISDeleted=1 WHERE ReceiptNo={Code} AND BranchID={MainClass.BranchNo}",
                    conn);
                int affected = cmd.ExecuteNonQuery();

                if (affected > 0)
                {
                    DXMessageBox.Show("تم الحذف بنجاح", "",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    _searchItems.Clear();
                    CLR();
                    SetStatus("تم حذف السند");
                }
                else
                {
                    DXMessageBox.Show("خطأ أثناء الحذف", "",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
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

        private void btnClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // فتح نافذة إضافة عميل جديد
                // frmCustomers frm = new frmCustomers();
                // frm.Type = 1;
                // frm.ShowDialog();
                // if (frm.isDone) LoadClients();
                DXMessageBox.Show("افتح نافذة تعريف العميل هنا", "إضافة عميل",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus("خطأ: " + ex.Message);
            }
        }

        private void btnSearchAcc_Click(object sender, RoutedEventArgs e)
        {
            SearchClientByName();
        }

        private void BtnImportDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter      = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF Files (*.pdf)|*.pdf",
                Title       = "تحديد الملفات",
                Multiselect = true
            };

            string dataPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            if (dialog.ShowDialog() == true)
            {
                // ReceiptOper.filePathDocument = dialog.FileNames;
                SetStatus($"تم اختيار {dialog.FileNames.Length} ملف");
            }
        }

        private void Btnshowdocument_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("يجب حفظ السند أولاً", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                EnsureOpen(conn);
                string sql = $"SELECT FileName, FileUrl FROM Documents WHERE GlobalID=N'{GlobalID}'";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    SetStatus($"يوجد {dt.Rows.Count} مستند مرفق");
                else
                    DXMessageBox.Show("لا توجد مستندات مرفقة", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في عرض المستندات: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region RadioButton Events

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

        private void rdCheckAll_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null)
                Panel1.Visibility = Visibility.Collapsed;
            if (Code == -1)
                SearchTreasury();
            else
                LoadAccountSources();
        }

        #endregion

        #region ComboBox Events

        private void cmbClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetCustBalance();
        }

        private void cmbClients_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SearchClientByName();
            }
        }

        private void cmbDepositTO_KeyDown(object sender, KeyEventArgs e)
        {
            if (rdCheckAll.IsChecked == true && e.Key == Key.Return)
                SearchTreasury();
        }

        #endregion

        #region TextBox Events

        private void txtVal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_change1 && !string.IsNullOrWhiteSpace(txtVal.Text))
            {
                try
                {
                    if (double.TryParse(txtVal.Text, out double val))
                        txtValD.Text = Math.Round(val / _Exchangeal, 2).ToString("N2");
                }
                catch { }
            }
        }

        private void txtValD_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_change2 && !string.IsNullOrWhiteSpace(txtValD.Text))
            {
                try
                {
                    if (double.TryParse(txtValD.Text, out double val))
                        txtVal.Text = Math.Round(val * _Exchangeal, 2).ToString("N2");
                }
                catch { }
            }
        }

        private void txtVal_GotFocus(object sender, RoutedEventArgs e)
        {
            _change1 = true;
            _change2 = false;
        }

        private void txtValD_GotFocus(object sender, RoutedEventArgs e)
        {
            _change1 = false;
            _change2 = true;
        }

        private void txtTime_GotFocus(object sender, RoutedEventArgs e)
        {
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

        #endregion

        #region CheckBox Events

        private void chkall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = chkall.IsChecked == true;
            txtFromDate.IsEnabled = !allChecked;
            txtToDate.IsEnabled   = !allChecked;
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is ReceiptSrchItem selected)
            {
                GlobalID = selected.GlobalId;
                Navigate($"SELECT * FROM Receipts WHERE GlobalID=N'{GlobalID}'");
            }
        }

        #endregion

        #region Client Search

        private void SearchClientByName()
        {
            try
            {
                string searchName = (cmbClients.SelectedItem as DataRowView)?["name"]?.ToString()
                                    ?? cmbClients.Text;
                SrchName = searchName;

                EnsureOpen(conn);
                string sql = $@"SELECT id, name FROM Customers
                                WHERE IS_Deleted=0
                                  AND name=N'{searchName}'
                                  AND (type=1 OR type=3)";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                if (dt.Rows.Count > 0)
                    cmbClients.SelectedValue = dt.Rows[0]["id"];
                else
                    OpenClientSearch();
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في البحث عن العميل: " + ex.Message);
            }
        }

        private void OpenClientSearch()
        {
            // frmSrchClient frm = new frmSrchClient();
            // frm.Type = 1;
            // frm.txtClientName.Text = SrchName;
            // frm.ShowDialog();
            // if (!string.IsNullOrEmpty(frm.Clientname))
            // {
            //     LoadClients();
            //     cmbClients.SelectedValue = frm.ClientId;
            // }
            DXMessageBox.Show("افتح نافذة البحث عن العملاء هنا", "بحث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTreasury()
        {
            // frmAccountSrch frm = new frmAccountSrch();
            // frm.ShowDialog();
            // if (frm.Code > -1)
            // {
            //     cmbDepositTO.SelectedValue = frm.Code;
            // }
            DXMessageBox.Show("افتح نافذة البحث عن الحساب هنا", "بحث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Print

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
                RptName = "Receipt.repx";

            string fullPath = System.IO.Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetStatus(printMode == 1 ? "جارِ الطباعة..." : "جارِ المعاينة...");
            // تنفيذ الطباعة عبر print.Printing(...)
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection connection)
        {
            if (!IsLoaded) return;
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void EnsureClose(SqlConnection connection)
        {
            if (!IsLoaded) return;
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        private void SetStatus(string message)
        {
            if (lblStatus != null)
                lblStatus.Text = message;
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
            EnsureClose(conn1);
        }

        #endregion
    }
}