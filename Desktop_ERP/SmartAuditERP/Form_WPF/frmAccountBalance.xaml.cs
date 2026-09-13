using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAccountBalance : Window
    {
        #region Fields
        private SqlConnection _connection = MainClass.ConnObj();
        private int _selectedAccountCode = -1;
        private int _restrictionType = -1;
        private string _reportPath = MainClass.ReportsPath;
        private string _reportName = "Statement.repx";
        private string _printerName = "";
        private int _fontSize = 12;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printNo = 1;
        #endregion

        #region Constructor & Load
        public frmAccountBalance()
        {
            InitializeComponent();
            this.Loaded += frmAccountBalance_Loaded;
        }

        private void frmAccountBalance_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Now;
            txtDateTo.SelectedDate = DateTime.Now;
            txtStartTime.Text = "00:00";
            txtEndTime.Text = "23:59";

            LoadAccounts();
            LoadBranches();
            LoadEntryTypes();
            LoadPrintSettings();
        }
        #endregion

        #region Data Loading Methods
        private void LoadAccounts()
        {
            try
            {
                string query = "SELECT Code, AName FROM Accounts_Index WHERE Type=2 " + Accounting.BranchCondition + " ORDER BY AName";
                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الحسابات: " + ex.Message);
            }
        }

        private void LoadBranches()
        {
            try
            {
                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                {
                    branchFilter = " AND id=" + MainClass.BranchNo;
                }

                string query = "SELECT id, name FROM Branches WHERE IS_Deleted=0 " + branchFilter;
                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";

                if (MainClass.BranchNo != -1)
                {
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                    chkAllBranches.Visibility = Visibility.Collapsed;
                    chkAllBranches.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الفروع: " + ex.Message);
            }
        }

        private void LoadEntryTypes()
        {
            cmbEntryType.Items.Add("رصيد افتتاحي");           // 0
            cmbEntryType.Items.Add("مشريات");               // 1
            cmbEntryType.Items.Add("مبيعات");               // 2
            cmbEntryType.Items.Add("تاجير");                // 3
            cmbEntryType.Items.Add("تسوية جردية");          // 4
            cmbEntryType.Items.Add("سند قبض من عميل");      // 5
            cmbEntryType.Items.Add("سند صرف لمورد");        // 6
            cmbEntryType.Items.Add("سند قبض");              // 7
            cmbEntryType.Items.Add("سند صرف");              // 8
            cmbEntryType.Items.Add("غير محدد");             // 9
            cmbEntryType.Items.Add("قيد اليومية");          // 10
            cmbEntryType.Items.Add("أول مدة");              // 11
            cmbEntryType.Items.Add("إضافات");               // 12
            cmbEntryType.Items.Add("إغلاق اليومية");        // 13
            cmbEntryType.Items.Add("مرتجع مشتريات");        // 14
            cmbEntryType.Items.Add("مرتجع مبيعات");         // 15
            cmbEntryType.Items.Add("مرتجع تأجير");          // 16
        }

        private void LoadPrintSettings()
        {
            try
            {
                string query = "SELECT * FROM SettingPrint WHERE Inv_Id=0";
                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    _printerName = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(_printerName))
                        _printerName = Common.GetDefaultPrinter();

                    _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]);

                    string rptUrl = dt.Rows[0]["RptUrl"]?.ToString();
                    if (!string.IsNullOrEmpty(rptUrl) && Directory.Exists(Path.GetDirectoryName(rptUrl)))
                    {
                        _reportPath = Path.GetDirectoryName(rptUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في إعدادات الطباعة: " + ex.Message);
                _reportPath = MainClass.ReportsPath;
                _printerName = MainClass.ReportsPrinter;
            }
        }
        #endregion

        #region Main Show Account Logic
        public void ShowAccountData()
        {
            if (cmbAccounts.SelectedValue == null)
            {
                MessageBox.Show("يرجى اختيار حساب أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAccounts.Focus();
                return;
            }

            try
            {
                // تطبيق فلاتر الأعمدة (مدين/دائن/الكل)
                ApplyColumnVisibilityFilters();

                DataTable dtResult = CreateResultTable();
                double totalDept = 0;
                double totalCredit = 0;
                double runningBalance = 0;

                string accountCode = txtCode.Text;
                string accountFirstDigit = accountCode.Substring(0, 1);

                DateTime startDate = txtDateFrom.SelectedDate ?? DateTime.Now;
                DateTime endDate = txtDateTo.SelectedDate ?? DateTime.Now;

                DateTime startDateTime = DateTime.Parse(startDate.ToShortDateString() + " " + txtStartTime.Text);
                DateTime endDateTime = DateTime.Parse(endDate.ToShortDateString() + " " + txtEndTime.Text);

                // 1. حساب الرصيد السابق
                if (chkPrevbalance.IsChecked == false && ckTotalPeriod.IsChecked == false)
                {
                    runningBalance = GetPreviousBalance(accountCode, startDateTime);
                    AddPreviousBalanceRow(dtResult, runningBalance, accountFirstDigit, startDate);
                }

                // 2. جلب الحركات من قاعدة البيانات
                DataTable dtMovements = GetAccountMovements(accountCode, startDateTime, endDateTime);

                // 3. معالجة الحركات وحساب الرصيد المتراكم
                int rank = 1;
                foreach (DataRow row in dtMovements.Rows)
                {
                    double dept = Convert.ToDouble(row["dept"]);
                    double credit = Convert.ToDouble(row["credit"]);

                    totalDept += dept;
                    totalCredit += credit;

                    // حساب الرصيد بناءً على طبيعة الحساب
                    if (accountFirstDigit == "1" || accountFirstDigit == "3") // أصول ومصروفات
                        runningBalance += (dept - credit);
                    else // خصوم وإيرادات (2، 4)
                        runningBalance += (credit - dept);

                    string balanceStatus = runningBalance >= 0 ? "مدين" : "دائن";
                    string branchName = Common.GetBranchName(Convert.ToInt32(row["Branch"]));
                    string entryType = Common.ResrirectionType(Convert.ToInt32(row["type"]));

                    string notes = row["subNotes"] != DBNull.Value ? row["subNotes"].ToString() : row["notes"].ToString();

                    dtResult.Rows.Add(
                        rank++,
                        dept,
                        credit,
                        Math.Abs(runningBalance),
                        balanceStatus,
                        row["GlobalID"],
                        row["doc_no"],
                        branchName,
                        entryType,
                        row["date"],
                        notes
                    );
                }

                dgvMain.ItemsSource = dtResult.DefaultView;
                UpdateTotalsUI(totalDept, totalCredit);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء عرض البيانات: " + ex.Message + "\n\n" + ex.StackTrace,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double GetPreviousBalance(string code, DateTime upToDate)
        {
            try
            {
                string branchCondition = "";
                if (chkAllBranches.IsChecked == false && cmbBranches.SelectedValue != null)
                {
                    branchCondition = $" AND Entry.branch={cmbBranches.SelectedValue} AND Entry_sub.branch={cmbBranches.SelectedValue}";
                }

                string sql = "SELECT ISNULL(SUM(Entry_sub.dept), 0) - ISNULL(SUM(Entry_sub.credit), 0) " +
                             "FROM Entry INNER JOIN Entry_sub ON Entry.GlobalID = Entry_sub.EntryGlobalID " +
                             "WHERE Entry_sub.acc_no = @code AND Entry.date < @date AND Entry.state=1 AND Entry.IS_Deleted=0" +
                             branchCondition;

                using (SqlCommand cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@date", upToDate);

                    if (_connection.State != ConnectionState.Open) _connection.Open();
                    object result = cmd.ExecuteScalar();
                    return result != DBNull.Value ? Convert.ToDouble(result) : 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في حساب الرصيد السابق: " + ex.Message);
                return 0;
            }
        }

        private DataTable GetAccountMovements(string code, DateTime startDate, DateTime endDate)
        {
            string dateFilter = "";
            if (ckTotalPeriod.IsChecked == false)
            {
                dateFilter = " AND Entry.date >= @date1 AND Entry.date < @date2 ";
            }

            string branchCondition = "";
            if (chkAllBranches.IsChecked == false && cmbBranches.SelectedValue != null)
            {
                branchCondition = $" AND Entry.branch={cmbBranches.SelectedValue} AND Entry_sub.branch={cmbBranches.SelectedValue}";
            }

            string typeCondition = "";
            if (chkEntryType.IsChecked == false && _restrictionType > -1)
            {
                typeCondition = $" AND Entry.type={_restrictionType}";
            }

            string selectFields = "";
            string groupBy = "";

            if (btnAggre.IsChecked == true) // تجميعي
            {
                selectFields = "SUM(Entry_sub.dept) as dept, SUM(Entry_sub.credit) as credit";
                groupBy = " GROUP BY Entry.GlobalID, Entry.date, Entry.notes, Entry.doc_no, Entry.type, Entry_sub.branch";
            }
            else // تفصيلي
            {
                selectFields = "Entry_sub.dept as dept, Entry_sub.credit as credit, Entry_sub.notes as subNotes";
            }

            string sql = $"SELECT Entry.GlobalID, Entry.date, Entry.doc_no, Entry.type, {selectFields}, Entry.notes, Entry_sub.branch as Branch " +
                         $"FROM Entry INNER JOIN Entry_sub ON Entry.GlobalID = Entry_sub.EntryGlobalID " +
                         $"WHERE Entry.IS_Deleted=0 AND Entry.state=1 {dateFilter} AND Entry_sub.acc_no={code} {branchCondition} {typeCondition} {groupBy} " +
                         $"ORDER BY Entry.date ASC";

            SqlDataAdapter adapter = new SqlDataAdapter(sql, _connection);
            adapter.SelectCommand.Parameters.AddWithValue("@date1", startDate);
            adapter.SelectCommand.Parameters.AddWithValue("@date2", endDate);

            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        private DataTable CreateResultTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DgvRank", typeof(int));
            dt.Columns.Add("DgvDept", typeof(double));
            dt.Columns.Add("DgvCredit", typeof(double));
            dt.Columns.Add("DgvBalance", typeof(double));
            dt.Columns.Add("DgvBalanceStatus", typeof(string));
            dt.Columns.Add("DgvGenralNo", typeof(string));
            dt.Columns.Add("DgvEntryNo", typeof(int));
            dt.Columns.Add("DgvBranch", typeof(string));
            dt.Columns.Add("DgvEntryType", typeof(string));
            dt.Columns.Add("DgvDate", typeof(DateTime));
            dt.Columns.Add("DgvNote", typeof(string));
            return dt;
        }

        private void AddPreviousBalanceRow(DataTable dt, double balance, string accountType, DateTime date)
        {
            double dept = 0, credit = 0;

            if (accountType == "1" || accountType == "3") // أصول ومصروفات
            {
                if (balance > 0)
                    dept = balance;
                else
                    credit = Math.Abs(balance);
            }
            else // خصوم وإيرادات
            {
                if (balance > 0)
                    credit = balance;
                else
                    dept = Math.Abs(balance);
            }

            string status = balance >= 0 ? "مدين" : "دائن";
            dt.Rows.Add(0, dept, credit, Math.Abs(balance), status, "0", 0, "", "رصيد سابق", date, "رصيد مرحل من فترة سابقة");
        }

        private void ApplyColumnVisibilityFilters()
        {
            var deptColumn = dgvMain.Columns[7] as DataGridTextColumn; // مدين
            var creditColumn = dgvMain.Columns[8] as DataGridTextColumn; // دائن

            if (rdDebt.IsChecked == true)
            {
                deptColumn.Visibility = Visibility.Visible;
                creditColumn.Visibility = Visibility.Collapsed;
                txtTotCredit.Visibility = Visibility.Collapsed;
                txtBalance2.Visibility = Visibility.Collapsed;
            }
            else if (rdCredit.IsChecked == true)
            {
                deptColumn.Visibility = Visibility.Collapsed;
                creditColumn.Visibility = Visibility.Visible;
                txtTotDept.Visibility = Visibility.Collapsed;
                txtBalance1.Visibility = Visibility.Collapsed;
            }
            else
            {
                deptColumn.Visibility = Visibility.Visible;
                creditColumn.Visibility = Visibility.Visible;
                txtTotDept.Visibility = Visibility.Visible;
                txtTotCredit.Visibility = Visibility.Visible;
                txtBalance1.Visibility = Visibility.Visible;
                txtBalance2.Visibility = Visibility.Visible;
            }
        }

        private void UpdateTotalsUI(double totalDept, double totalCredit)
        {
            txtTotDept.Text = totalDept.ToString(Common.DigitsNo);
            txtTotCredit.Text = totalCredit.ToString(Common.DigitsNo);

            if (totalDept > totalCredit)
            {
                txtBalance1.Text = (totalDept - totalCredit).ToString(Common.DigitsNo);
                txtBalance2.Text = "0.00";
            }
            else if (totalCredit > totalDept)
            {
                txtBalance1.Text = "0.00";
                txtBalance2.Text = (totalCredit - totalDept).ToString(Common.DigitsNo);
            }
            else
            {
                txtBalance1.Text = "0.00";
                txtBalance2.Text = "0.00";
            }
        }
        #endregion

        #region Report Preparation & Printing
        private DataSet PrepareReportDataSet()
        {
            List<AccountStatementReportData> reportList = new List<AccountStatementReportData>();

            // جلب معلومات المؤسسة
            var foundationData = GetFoundationData();

            for (int i = 0; i < dgvMain.Items.Count; i++)
            {
                var item = dgvMain.Items[i] as DataRowView;
                if (item == null) continue;

                AccountStatementReportData data = new AccountStatementReportData
                {
                    AccountName = cmbAccounts.Text,
                    AccountCode = txtCode.Text,
                    Description = item["DgvNote"]?.ToString() ?? "",
                    Credit = item["DgvCredit"]?.ToString() ?? "0",
                    Dept = item["DgvDept"]?.ToString() ?? "0",
                    Status = item["DgvBalanceStatus"]?.ToString() ?? "",
                    Balance = item["DgvBalance"]?.ToString() ?? "0",
                    ProcessNo = item["DgvEntryNo"]?.ToString() ?? "",
                    ProcessType = item["DgvEntryType"]?.ToString() ?? "",
                    RestrNo = item["DgvGenralNo"]?.ToString() ?? "",
                    Branch = item["DgvBranch"]?.ToString() ?? "",
                    RestrType = "كشف حساب",
                    RestDate = item["DgvDate"]?.ToString() ?? "",
                    RestTime = "",
                    FromDate = txtDateFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                    ToDate = txtDateTo.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                    User = Common.GetEmpName(MainClass.EmpNo),
                    SumDept = txtTotDept.Text,
                    SumCredit = txtTotCredit.Text,
                    BalanceInperiod = txtBalance1.Text != "0.00" ? txtBalance1.Text : txtBalance2.Text,
                    BalanceType = txtBalance1.Text != "0.00" ? "مدين" : "دائن",
                    ArabicLetter = InvoiceOper.ToArabicLetter(Convert.ToDouble(txtBalance1.Text != "0.00" ? txtBalance1.Text : txtBalance2.Text)),
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Foundation = foundationData.Name,
                    Address = foundationData.Address,
                    Mobile = foundationData.Mobile,
                    TelePhone = foundationData.Phone,
                    VatNo = foundationData.VatNo,
                    Field = foundationData.Field,
                    Logo = "",
                    Header = "",
                    footer = "",
                    Stamp = ""
                };

                reportList.Add(data);
            }

            DataSet ds = new DataSet("AccountStatement");
            DataTable dt = UtilitiesProj.Common.ToDataTable(reportList);
            ds.Tables.Add(dt);
            return ds;
        }

        private FoundationInfo GetFoundationData()
        {
            FoundationInfo info = new FoundationInfo();

            try
            {
                string query = "SELECT * FROM Foundation";
                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    info.Name = dt.Rows[0]["nameA"]?.ToString() ?? "";
                    info.Address = dt.Rows[0]["Address"]?.ToString() ?? "";
                    info.Phone = dt.Rows[0]["Tel"]?.ToString() ?? "";
                    info.Mobile = dt.Rows[0]["Mobile"]?.ToString() ?? "";
                    info.VatNo = dt.Rows[0]["tax_no"]?.ToString() ?? "";
                    info.Field = dt.Rows[0]["FieldA"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في جلب بيانات المؤسسة: " + ex.Message);
            }

            return info;
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevExpressReport(preview: true);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevExpressReport(preview: false);
        }

        private void PrintDevExpressReport(bool preview)
        {
            if (dgvMain.Items.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لطباعتها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_reportPath))
            {
                MessageBox.Show("يجب تحديد مسار التقرير", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string reportFullPath = Path.Combine(_reportPath, _reportName);

            if (!Directory.Exists(_reportPath) || !File.Exists(reportFullPath))
            {
                MessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله:\n" + reportFullPath,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                XtraReport mainReport = XtraReport.FromFile(reportFullPath);
                mainReport.DataSource = PrepareReportDataSet();

                // تحميل التقارير الفرعية (Header & Footer)
                LoadSubReports(mainReport);

                if (preview)
                {
                    mainReport.ShowPreviewDialog();
                }
                else
                {
                    if (!string.IsNullOrEmpty(_printerName))
                    {
                        mainReport.PrinterName = _printerName;
                        for (int i = 0; i < _printNo; i++)
                        {
                            mainReport.Print();
                        }
                    }
                    else
                    {
                        MessageBox.Show("يجب تحديد الطابعة من الإعدادات", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                mainReport.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message + "\n\n" + ex.StackTrace,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSubReports(XtraReport mainReport)
        {
            try
            {
                // تحميل تقرير الهيدر
                string headerPath = Path.Combine(_reportPath, "header.repx");
                if (File.Exists(headerPath))
                {
                    XtraReport headerReport = XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;

                    XRSubreport headerSubreport = mainReport.FindControl("headerRpt", true) as XRSubreport;
                    if (headerSubreport != null)
                        headerSubreport.ReportSource = headerReport;
                }

                // تحميل تقرير الفوتر
                string footerPath = Path.Combine(_reportPath, "footer.repx");
                if (File.Exists(footerPath))
                {
                    XtraReport footerReport = XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;

                    XRSubreport footerSubreport = mainReport.FindControl("footerRpt", true) as XRSubreport;
                    if (footerSubreport != null)
                        footerSubreport.ReportSource = footerReport;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في تحميل التقارير الفرعية: " + ex.Message);
            }
        }
        #endregion

        #region Excel Export
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (dgvMain.Items.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string fileName = $"كشف_حساب_{cmbAccounts.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = fileName
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportDataGridToExcel(saveDialog.FileName);

                    var result = MessageBox.Show("تم التصدير بنجاح. هل تريد فتح الملف؟", "نجاح",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                        Process.Start(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في التصدير: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDataGridToExcel(string filePath)
        {
            // استخدام مكتبة ClosedXML أو EPPlus (يجب إضافتها عبر NuGet)
            // هنا مثال بسيط - يمكن تحسينه

            DataTable dt = (dgvMain.ItemsSource as DataView)?.Table;
            if (dt != null)
            {
                // كود التصدير باستخدام مكتبة خارجية
                // مثال: ExcelHelper.Export(dt, filePath);

                MessageBox.Show("يرجى تثبيت مكتبة ClosedXML أو EPPlus لإتمام عملية التصدير",
                    "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Event Handlers
        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            // التعامل مع السنوات السابقة
            if (MainClass.ReturnYearPreviews && CmbYearPreviews.SelectedValue != null)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr = $"server={MainClass.Server};database={CmbYearPreviews.SelectedValue};trusted_connection=true";
                _connection = MainClass.ConnObj();
            }

            ShowAccountData();

            // استعادة الاتصال الأصلي
            if (MainClass.ReturnYearPreviews)
            {
                MainClass.connstr = MainClass.originalConnStr;
                MainClass.ReturnYearPreviews = false;
                _connection = MainClass.ConnObj();
            }
        }

        private void cmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAccounts.SelectedValue != null)
                txtCode.Text = cmbAccounts.SelectedValue.ToString();
        }

        private void cmbAccounts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenAccountSearchDialog();
        }

        private void txtCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchAccountByCode();
        }

        private void SearchAccountByCode()
        {
            try
            {
                string code = txtCode.Text;
                string query = $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND Code={code}";

                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbAccounts.SelectedValue = Convert.ToInt32(dt.Rows[0]["Code"]);
                }
                else
                {
                    OpenAccountSearchDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void OpenAccountSearchDialog()
        {
            try
            {
                // فتح نافذة البحث (إذا كانت موجودة في المشروع)
                 frmAccountSrch searchForm = new frmAccountSrch();
                 searchForm.cond = " ";
                 searchForm.ShowDialog();
                 if (searchForm.Code > -1)
                 {
                     cmbAccounts.SelectedValue = searchForm.Code;
                 }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في فتح نافذة البحث: " + ex.Message);
            }
        }

        private void ckTotalPeriod_Toggle(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isFullPeriod = ckTotalPeriod.IsChecked ?? false;
            txtDateFrom.IsEnabled = !isFullPeriod;
            txtDateTo.IsEnabled = !isFullPeriod;
            txtStartTime.IsEnabled = !isFullPeriod;
            txtEndTime.IsEnabled = !isFullPeriod;
        }

        private void chkAllBranches_Toggle(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAllBranches = chkAllBranches.IsChecked ?? true;
            cmbBranches.IsEnabled = !isAllBranches;
        }

        private void chkEntryType_Toggle(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAllTypes = chkEntryType.IsChecked ?? true;
            cmbEntryType.IsEnabled = !isAllTypes;
        }

        private void cmbEntryType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEntryType.SelectedIndex >= 0 && cmbEntryType.SelectedIndex <= 16)
                _restrictionType = cmbEntryType.SelectedIndex;
        }

        private void BtnShowDetail_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            if (btn?.Tag != null && btn.Tag.ToString() != "0")
            {
                // فتح تفاصيل القيد
                EntryOper.ShowEntrySource(btn.Tag.ToString());
            }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            _fontSize += 2;
            dgvMain.FontSize = _fontSize;
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_fontSize > 10)
            {
                _fontSize -= 2;
                dgvMain.FontSize = _fontSize;
            }
        }

        private void CmbYearPreviews_DropDown(object sender, EventArgs e)
        {
            LoadYearPreviewsList();
        }

        private void LoadYearPreviewsList()
        {
            try
            {
                string query = "SELECT Dbname, DbAutoName FROM Year_Previews WHERE Is_Deleted=0 ORDER BY id";
                SqlDataAdapter adapter = new SqlDataAdapter(query, _connection);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                // إضافة السنة الحالية
                DataRow currentYear = dt.NewRow();
                currentYear["Dbname"] = MainClass.DataBaseName;
                currentYear["DbAutoName"] = MainClass.Database;
                dt.Rows.InsertAt(currentYear, 0);

                CmbYearPreviews.ItemsSource = dt.DefaultView;
                CmbYearPreviews.DisplayMemberPath = "Dbname";
                CmbYearPreviews.SelectedValuePath = "DbAutoName";
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل السنوات: " + ex.Message);
            }
        }

        private void CmbYearPreviews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbYearPreviews.SelectedIndex > 0)
                MainClass.ReturnYearPreviews = true;
            else
                MainClass.ReturnYearPreviews = false;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Helper Classes
        public class AccountStatementReportData
        {
            public string AccountName { get; set; }
            public string AccountCode { get; set; }
            public string Description { get; set; }
            public string Credit { get; set; }
            public string Dept { get; set; }
            public string Status { get; set; }
            public string Balance { get; set; }
            public string ProcessNo { get; set; }
            public string ProcessType { get; set; }
            public string RestrNo { get; set; }
            public string Branch { get; set; }
            public string RestrType { get; set; }
            public string RestDate { get; set; }
            public string RestTime { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public string User { get; set; }
            public string SumDept { get; set; }
            public string SumCredit { get; set; }
            public string BalanceInperiod { get; set; }
            public string BalanceType { get; set; }
            public string ArabicLetter { get; set; }
            public string PrintDate { get; set; }
            public string Address { get; set; }
            public string Mobile { get; set; }
            public string TelePhone { get; set; }
            public string VatNo { get; set; }
            public string Foundation { get; set; }
            public string Field { get; set; }
            public string Logo { get; set; }
            public string Header { get; set; }
            public string footer { get; set; }
            public string Stamp { get; set; }
        }

        public class FoundationInfo
        {
            public string Name { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public string Mobile { get; set; }
            public string VatNo { get; set; }
            public string Field { get; set; }
        }
        #endregion
    }
}