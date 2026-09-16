using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using log4net;
using UtilitiesProj;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Linq;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCostCenterBalance : ThemedWindow
    {
        #region ── Private Fields ──

        private bool PrintHeader = true;
        private bool PrintFooter = true;
        private bool PrintStamp = true;
        private int PrintType;
        private int PrintNo = 1;
        private string defPrinter = "";
        private string RptName = "";
        private string RptUrl = "";
        private int SelectedId = -1;
        private SqlConnection conn;
        private string StyleFile;
        private string StyleFolder;
        private int Restype = -1;
        private int DgvFontSize = 1;
        private string SettingsFile;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Public Fields ──

        public string SrchName = "";

        #endregion

        #region ── Constructor ──

        public frmCostCenterBalance()
        {
            conn = MainClass.ConnObj();
            StyleFile = Path.Combine(MainClass.ReportsPath,
                          "Styles\\CostCenterBalance.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            Restype = -1;
            DgvFontSize = 1;
            SettingsFile = Path.Combine(MainClass.ReportsPath,
                           "Styles\\AccBlcSettings.xml");

            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDateFrom.SelectedDate = DateTime.Now.Date;
                txtDateTo.SelectedDate = DateTime.Now.Date;

                LoadPrintSettings();
                cmbCostCenter.Focus();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }

            if (File.Exists(StyleFile))
                GridControl1.RestoreLayoutFromXml(StyleFile);

            LoadCostCenter();
            LoadAccounts();
            LoadBranches();
            BranchPermissions();
            LoadSettings();
            LoadResType();
        }

        #endregion

        #region ── Data Loading ──

        private void LoadCostCenter()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM cost_center " +
                    "WHERE Code <> 0 AND Is_Deleted=0 AND type=2",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "Code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadAccounts()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Code, AName FROM Accounts_Index " +
                    $"WHERE Type=2 {Accounting.BranchCondition}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadBranches()
        {
            try
            {
                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                {
                    branchFilter = (Accounting.BranchCondition.Trim() != "")
                        ? $" AND id={MainClass.BranchNo}" : "";
                }

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches " +
                    $"WHERE IS_Deleted=0 {branchFilter}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private void BranchPermissions()
        {
            if (Accounting.BranchCondition.Trim() != "")
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.SelectedIndex = 0;
            }
        }

        private void LoadResType()
        {
            try
            {
                string[] entryTypes =
                {
                    "رصيد افتتاحي", "مشتريات", "مبيعات", "تأجير",
                    "تسوية جردية", "سند قبض من عميل", "سند صرف لمورد",
                    "سند قبض", "سند صرف", "غير محدد", "قيد اليومية",
                    "أول مدة", "إضافات", "إغلاق اليومية",
                    "مرتجع مشتريات", "مرتجع مبيعات", "مرتجع تأجير"
                };

                foreach (string entryType in entryTypes)
                    cmbEntryType.Items.Add(entryType);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Show Account Data ──

        public void ShowAccount()
        {
            try
            {
                if (cmbCostCenter.SelectedValue == null)
                {
                    ShowMsg("اختر مركز الكلفة", "", MessageBoxImage.Warning);
                    cmbCostCenter.Focus();
                    return;
                }

                var dataTable = BuildDataTable();
                double totalDebit = 0.0;
                double totalCredit = 0.0;

                ApplyColumnVisibility();
                ClearTotals();

                string costCenterCode = cmbCostCenter.SelectedValue.ToString();
                string accountType = costCenterCode.Substring(0, 1);

                // بناء فلاتر الاستعلام
                string dateFilter = BuildDateFilter();
                string accFilter = BuildAccountFilter();
                string typeFilter = BuildTypeFilter();
                string branchFilter = BuildBranchFilter();

                // تحديد طريقة التجميع
                string deptExpr = " SUM(Entry_sub.dept) ";
                string creditExpr = " SUM(Entry_sub.credit) ";
                string groupBy = " GROUP BY Entry.GlobalID, Entry.date, " +
                                    "Entry.notes, Entry_sub.acc_no, " +
                                    "Entry.doc_no, Entry.type, " +
                                    "Entry_sub.notes, Entry_sub.branch ";

                if (btnDetails.IsChecked == true)
                {
                    deptExpr = " Entry_sub.dept ";
                    creditExpr = " Entry_sub.credit ";
                    groupBy = " ";
                }

                double runningBalance = 0.0;

                // === رصيد سابق ===
                if (chkPrevbalance.IsChecked != true &&
                    ckTotalPeriod.IsChecked != true)
                {
                    var prevRow = LoadPreviousBalance(
                        branchFilter, costCenterCode,
                        accountType, ref totalDebit, ref totalCredit,
                        ref runningBalance);
                    if (prevRow != null)
                        dataTable.Rows.Add(prevRow);
                }

                // === بيانات الفترة ===
                LoadPeriodData(dataTable, branchFilter, deptExpr, creditExpr,
                    dateFilter, accFilter, typeFilter, groupBy,
                    accountType, ref totalDebit, ref totalCredit,
                    ref runningBalance);

                GridControl1.ItemsSource = dataTable.DefaultView;
                UpdateTotals(totalDebit, totalCredit);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private DataTable BuildDataTable()
        {
            var dt = new DataTable();
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

        private void ApplyColumnVisibility()
        {
            var deptCol = GridControl1.Columns["DgvDept"];
            var creditCol = GridControl1.Columns["DgvCredit"];

            if (rdDebt.IsChecked == true)
            {
                if (deptCol != null) deptCol.Visible = true;
                if (creditCol != null) creditCol.Visible = false;
                txtTotDept.Visibility = Visibility.Visible;
                txtBalance1.Visibility = Visibility.Visible;
                txtTotCredit.Visibility = Visibility.Collapsed;
                txtBalance2.Visibility = Visibility.Collapsed;
            }
            else if (rdCredit.IsChecked == true)
            {
                if (deptCol != null) deptCol.Visible = false;
                if (creditCol != null) creditCol.Visible = true;
                txtTotDept.Visibility = Visibility.Collapsed;
                txtBalance1.Visibility = Visibility.Collapsed;
                txtTotCredit.Visibility = Visibility.Visible;
                txtBalance2.Visibility = Visibility.Visible;
            }
            else
            {
                if (deptCol != null) deptCol.Visible = true;
                if (creditCol != null) creditCol.Visible = true;
                txtTotDept.Visibility = Visibility.Visible;
                txtBalance1.Visibility = Visibility.Visible;
                txtTotCredit.Visibility = Visibility.Visible;
                txtBalance2.Visibility = Visibility.Visible;
            }
        }

        private void ClearTotals()
        {
            txtTotDept.Text = "";
            txtTotCredit.Text = "";
            txtBalance1.Text = "";
            txtBalance2.Text = "";
        }

        private string BuildDateFilter()
        {
            return (ckTotalPeriod.IsChecked != true)
                ? " AND Entry.date>=@date1 AND Entry.date<@date2 "
                : "";
        }

        private string BuildAccountFilter()
        {
            string filter = $" AND Entry_sub.CCcode={cmbCostCenter.SelectedValue}";
            if (chkAll.IsChecked != true && cmbAccounts.SelectedValue != null)
                filter += $" AND Entry_sub.acc_no={cmbAccounts.SelectedValue}";
            return filter;
        }

        private string BuildTypeFilter()
        {
            if (chkEntryType.IsChecked != true && Restype > -1)
                return $" AND Entry.type={Restype}";
            return "";
        }

        private string BuildBranchFilter()
        {
            if (chkAllBranches.IsChecked != true &&
                cmbBranches.SelectedIndex != -1 &&
                cmbBranches.SelectedValue != null)
            {
                return $"Entry.branch={cmbBranches.SelectedValue} " +
                       $"AND Entry_sub.branch={cmbBranches.SelectedValue} AND ";
            }
            return "";
        }

        private object[] LoadPreviousBalance(
            string branchFilter, string costCenterCode,
            string accountType,
            ref double totalDebit, ref double totalCredit,
            ref double runningBalance)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT SUM(Entry_sub.dept) AS dept, " +
                    $"SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE {branchFilter} Entry.IS_Deleted=0 " +
                    $"AND Entry.state=1 " +
                    $"AND Entry.date<@date1 " +
                    $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                    $"AND Entry_sub.CCcode={costCenterCode}",
                    conn);

                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value =
                    (txtDateFrom.SelectedDate ?? DateTime.Now).Date;

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return null;

                double prevDebit = SafeParseDouble(dt.Rows[0]["dept"]);
                double prevCredit = SafeParseDouble(dt.Rows[0]["credit"]);
                totalDebit += prevDebit;
                totalCredit += prevCredit;

                double prevDeptDisp = 0.0;
                double prevCredDisp = 0.0;
                string balanceStatus;
                double balanceValue;

                if (accountType == "1" || accountType == "3")
                {
                    balanceValue = prevDebit - prevCredit;
                    runningBalance += balanceValue;
                    balanceStatus = runningBalance > 0
                        ? ((MainClass.Language == "ar") ? "مدين" : "Debit")
                        : ((MainClass.Language == "ar") ? "دائن" : "Credit");
                }
                else
                {
                    balanceValue = prevCredit - prevDebit;
                    runningBalance += balanceValue;
                    balanceStatus = runningBalance > 0
                        ? ((MainClass.Language == "ar") ? "دائن" : "Credit")
                        : ((MainClass.Language == "ar") ? "مدين" : "Debit");
                }

                if (prevDebit > prevCredit)
                    prevDeptDisp = Math.Round(prevDebit - prevCredit, 2);
                else
                    prevCredDisp = Math.Round(prevCredit - prevDebit, 2);

                return new object[]
                {
                    0, prevDeptDisp, prevCredDisp,
                    Math.Round(balanceValue, 2), balanceStatus,
                    "0", 0, "",
                    "رصيد سابق",
                    (txtDateFrom.SelectedDate ?? DateTime.Now).Date,
                    ""
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
                return null;
            }
        }

        private void LoadPeriodData(
            DataTable dataTable,
            string branchFilter, string deptExpr, string creditExpr,
            string dateFilter, string accFilter, string typeFilter,
            string groupBy, string accountType,
            ref double totalDebit, ref double totalCredit,
            ref double runningBalance)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT Entry.GlobalID, Entry.date, Entry.doc_no, " +
                $"Entry.type, {deptExpr} AS dept, {creditExpr} AS credit, " +
                $"Entry.notes, Entry_sub.notes AS subNotes, " +
                $"Entry_sub.branch AS Branch " +
                $"FROM Entry, Entry_sub " +
                $"WHERE {branchFilter} Entry.IS_Deleted=0 " +
                $"AND Entry.state=1 {dateFilter} " +
                $"AND Entry.GlobalId=Entry_sub.EntryGlobalId " +
                $"{accFilter} {typeFilter} {groupBy} " +
                $"ORDER BY date ASC",
                conn);

            DateTime dateFrom = (txtDateFrom.SelectedDate ?? DateTime.Now).Date;
            DateTime dateTo = (txtDateTo.SelectedDate ?? DateTime.Now).Date
                                .Add(new TimeSpan(23, 59, 59));

            adapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = dateFrom;
            adapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = dateTo;

            var dt = new DataTable();
            adapter.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double rowDebit =
                    Math.Round(SafeParseDouble(dt.Rows[i]["dept"]), 2);
                double rowCredit =
                    Math.Round(SafeParseDouble(dt.Rows[i]["credit"]), 2);
                string globalId = dt.Rows[i]["GlobalID"].ToString();
                int docNo =
                    Convert.ToInt32(dt.Rows[i]["doc_no"]);
                string entryType =
                    Common.ResrirectionType(Convert.ToInt32(dt.Rows[i]["type"]));
                string entryDate =
                    Convert.ToDateTime(dt.Rows[i]["date"]).ToShortDateString();
                string subNotes = dt.Rows[i]["subNotes"].ToString();
                string branch =
                    Common.GetBranchName(Convert.ToInt32(dt.Rows[i]["Branch"]));

                totalDebit += rowDebit;
                totalCredit += rowCredit;

                string balanceStatus;
                if (accountType == "1" || accountType == "3")
                {
                    runningBalance += rowDebit - rowCredit;
                    balanceStatus = runningBalance > 0
                        ? ((MainClass.Language == "ar") ? "مدين" : "Debit")
                        : ((MainClass.Language == "ar") ? "دائن" : "Credit");
                }
                else
                {
                    runningBalance += rowCredit - rowDebit;
                    balanceStatus = runningBalance > 0
                        ? ((MainClass.Language == "ar") ? "دائن" : "Credit")
                        : ((MainClass.Language == "ar") ? "مدين" : "Debit");
                }

                dataTable.Rows.Add(
                    i + 1, rowDebit, rowCredit,
                    Math.Round(runningBalance, 2), balanceStatus,
                    globalId, docNo, branch, entryType,
                    Convert.ToDateTime(dt.Rows[i]["date"]), subNotes);
            }
        }

        private void UpdateTotals(double totalDebit, double totalCredit)
        {
            txtTotDept.Text = $"{Math.Round(totalDebit, 2):0.#,##.##}";
            txtTotCredit.Text = $"{Math.Round(totalCredit, 2):0.#,##.##}";

            if (totalDebit > totalCredit)
            {
                txtBalance1.Text = $"{Math.Round(totalDebit - totalCredit, 2):0.#,##.##}";
                txtBalance2.Text = "";
            }
            else if (totalCredit > totalDebit)
            {
                txtBalance1.Text = "";
                txtBalance2.Text = $"{Math.Round(totalCredit - totalDebit, 2):0.#,##.##}";
            }
            else
            {
                txtBalance1.Text = "0";
                txtBalance2.Text = "0";
            }
        }

        #endregion

        #region ── Print & Report ──

        private DataSet BindToData()
        {
            SqlDataAdapter sqlDataAdapter;
            sqlDataAdapter = new SqlDataAdapter("Select * from Foundation", this.conn);

            DataTable dataTable;
            dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            string address = "";
            string telePhone = "";
            string mobile = "";
            string foundation = "";
            string field = "";
            string vatNo = "";

            if (dataTable.Rows.Count > 0)
            {
                address = dataTable.Rows[0]["Address"].ToString();
                telePhone = dataTable.Rows[0]["Tel"].ToString();
                mobile = dataTable.Rows[0]["Mobile"].ToString();
                foundation = dataTable.Rows[0]["nameA"].ToString();
                field = dataTable.Rows[0]["FieldA"].ToString();
                vatNo = dataTable.Rows[0]["tax_no"].ToString();

                if (dataTable.Rows[0]["Logo"] != DBNull.Value)
                {
                    MainClass.Arr2Image((byte[])dataTable.Rows[0]["Logo"]);
                }
            }

            List<RestrictionData> list = new List<RestrictionData>();

            int rowCount = GetGridRowCount();

            for (int i = 0; i < rowCount; i++)
            {
                RestrictionData restrictionData = new RestrictionData
                {
                    AccountName = cmbCostCenter.Text,
                    AccountCode = cmbCostCenter.SelectedValue?.ToString(),
                    Description = GridControl1.GetCellValue(i, "DgvNote")?.ToString(),
                    Credit = GridControl1.GetCellValue(i, "DgvCredit")?.ToString(),
                    Dept = GridControl1.GetCellValue(i, "DgvDept")?.ToString(),
                    Status = GridControl1.GetCellValue(i, "DgvBalanceStatus")?.ToString(),
                    Balance = GridControl1.GetCellValue(i, "DgvBalance")?.ToString(),
                    ProcessNo = GridControl1.GetCellValue(i, "DgvEntryNo")?.ToString(),
                    ProcessType = GridControl1.GetCellValue(i, "DgvEntryType")?.ToString(),
                    RestrNo = GridControl1.GetCellValue(i, "DgvGenralNo")?.ToString(),
                    Branch = GridControl1.GetCellValue(i, "DgvBranch")?.ToString(),
                    RestrType = this.Title,
                    RestDate = GridControl1.GetCellValue(i, "DgvDate")?.ToString(),
                    RestTime = "",
                    FromDate = txtDateFrom.SelectedDate?.ToString(),
                    ToDate = txtDateTo.SelectedDate?.ToString(),
                    User = Common.GetEmpName(MainClass.EmpNo),
                    SumDept = txtTotDept.Text,
                    SumCredit = txtTotCredit.Text,
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Address = address,
                    Mobile = mobile,
                    TelePhone = telePhone,
                    VatNo = vatNo,
                    Foundation = foundation,
                    Field = field,
                    Logo = "",
                    Header = "",
                    footer = "",
                    Stamp = ""
                };

                if (SafeParseDouble(txtTotDept.Text) > SafeParseDouble(txtTotCredit.Text))
                {
                    restrictionData.BalanceInperiod = txtBalance1.Text + "  ";
                    restrictionData.BalanceType = "مدين";
                }
                else if (SafeParseDouble(txtTotDept.Text) < SafeParseDouble(txtTotCredit.Text))
                {
                    restrictionData.BalanceInperiod = txtBalance2.Text + "  ";
                    restrictionData.BalanceType = "دائن";
                }
                else
                {
                    restrictionData.BalanceInperiod = "0";
                }

                restrictionData.ArabicLetter =
                    InvoiceOper.ToArabicLetter(SafeParseDouble(restrictionData.BalanceInperiod));

                list.Add(restrictionData);
            }

            DataSet dataSet = new DataSet("Name");
            DataTable table = global::UtilitiesProj.Common.ToDataTable(list);
            dataSet.Tables.Add(table);
            list.Clear();

            return dataSet;
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    var row = dt.Rows[0];
                    PrintType = Convert.ToInt32(row["printType"]);
                    PrintFooter = Convert.ToBoolean(row["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(row["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(row["PrintStamp"]);
                    defPrinter = row["CasherPrinter"].ToString();

                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();

                    PrintNo = Convert.ToInt32(row["printNo"]);
                    RptName = row["RptName"].ToString();
                    RptUrl = Path.GetDirectoryName(row["RptUrl"].ToString());

                    if (string.IsNullOrEmpty(RptUrl) ||
                        !Directory.Exists(RptUrl))
                        RptUrl = MainClass.ReportsPath;
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                    RptName = "RptCostCenterStatement.repx";
                    defPrinter = MainClass.ReportsPrinter;
                }
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message, "خطأ", MessageBoxImage.Error);
            }
        }

        private void PrintDevexpress(int type)
        {
            if (GetGridRowCount() == 0)
            {
                ShowMsg("لا توجد عمليات بالجدول", "", MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptUrl))
            {
                ShowMsg("يجب تحديد مسار التقرير", "", MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                ShowMsg("يجب إدخال اسم التقرير من الإعدادات", "", MessageBoxImage.Warning);
                return;
            }

            string fullReportPath = Path.Combine(RptUrl, RptName);

            if (!Directory.Exists(RptUrl) || !File.Exists(fullReportPath))
            {
                ShowMsg("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ", MessageBoxImage.Warning);
                return;
            }

            XtraReport xtraReport = XtraReport.FromFile(fullReportPath);
            xtraReport.DataSource = BindToData();

            string headerPath = Path.Combine(RptUrl, "header.repx");
            if (File.Exists(headerPath))
            {
                XtraReport headerReport = XtraReport.FromFile(headerPath);
                headerReport.DataSource = Common.FoundationInfoDT;
                XRSubreport headerSubreport =
                    xtraReport.FindControl("headerRpt", true) as XRSubreport;
                if (headerSubreport != null)
                    headerSubreport.ReportSource = headerReport;
            }

            string footerPath = Path.Combine(RptUrl, "footer.repx");
            if (File.Exists(footerPath))
            {
                XtraReport footerReport = XtraReport.FromFile(footerPath);
                footerReport.DataSource = Common.FoundationInfoDT;
                XRSubreport footerSubreport =
                    xtraReport.FindControl("footerRpt", true) as XRSubreport;
                if (footerSubreport != null)
                    footerSubreport.ReportSource = footerReport;
            }

            if (!string.IsNullOrEmpty(defPrinter))
            {
                xtraReport.PrinterName = defPrinter;

                if (type == 1)
                {
                    for (int i = 0; i < PrintNo; i++)
                        xtraReport.Print();
                }
                else
                {
                    xtraReport.ShowPreviewDialog();
                }

                xtraReport.Dispose();
            }
            else
            {
                ShowMsg("يجب تحديد الطابعة من الإعدادات", "", MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ── Font Size ──

        private void ChangeFontSize(int fontSizeDelta)
        {
            double newSize = GridControl1.FontSize + fontSizeDelta;

            if (newSize < 8)
                newSize = 8;

            if (newSize > 24)
                newSize = 24;

            GridControl1.FontSize = newSize;
        }

        #endregion

        #region ── Settings ──

        public virtual void SaveSettings()
        {
            try
            {
                CostCenterBalanceLayoutSettings settings =
                    new CostCenterBalanceLayoutSettings
                    {
                        LeftPanelWidth = GetLeftPanelWidth()
                    };

                string folder = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFile, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        public virtual void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                    return;

                string json = File.ReadAllText(SettingsFile, Encoding.UTF8);
                CostCenterBalanceLayoutSettings settings =
                    JsonSerializer.Deserialize<CostCenterBalanceLayoutSettings>(json);

                if (settings != null)
                    SetLeftPanelWidth(settings.LeftPanelWidth);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Account Search ──

        private void SearchByName()
        {
            try
            {
                SrchName = cmbAccounts.Text;
                var adapter = new SqlDataAdapter(
                    $"SELECT Code, AName FROM Accounts_Index " +
                    $"WHERE AName=N'{SrchName}' AND type=2", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbAccounts.SelectedValue =
                        Convert.ToDouble(dt.Rows[0]["Code"]);
                else
                    addNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private void addNewItem()
        {
            try
            {
                var form = new frmAccountSrch();
                form.cond = "";
                form.txtSrchNm.Text = SrchName;
                form.ShowDialog();

                if (form.Code > -1)
                {
                    var adapter = new SqlDataAdapter(
                        $"SELECT Code, AName FROM Accounts_Index " +
                        $"WHERE type=2 AND Code={form.Code}", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        SelectedId = form.Code;
                        cmbAccounts.SelectedValue =
                            Convert.ToDouble(dt.Rows[0]["Code"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── UI Events ──

        private void btnShow_Click(object sender, RoutedEventArgs e)
            => ShowAccount();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (GetGridRowCount() <= 0)
            {
                ShowMsg("لا توجد بيانات للتصدير", "", MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Title = "حفظ الملف",
                Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = this.Title + ".csv",
                DefaultExt = ".csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                ExportCurrentGridToCsv(saveDialog.FileName);

                Process.Start(new ProcessStartInfo(saveDialog.FileName)
                {
                    UseShellExecute = true
                });
            }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            ChangeFontSize(DgvFontSize);
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ChangeFontSize(-DgvFontSize);
        }

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(StyleFolder))
                Directory.CreateDirectory(StyleFolder);
            SaveSettings();
            GridControl1.SaveLayoutToXml(StyleFile);
            ShowMsg("تم حفظ مظهر الجدول");
        }

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(StyleFile))
                File.Delete(StyleFile);
        }

        private void ckTotalPeriod_CheckedChanged(object sender,
            RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isFullPeriod = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isFullPeriod;
            txtDateTo.IsEnabled = !isFullPeriod;
        }

        private void chkAllBranches_CheckedChanged(object sender,
            RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void chkEntryType_CheckedChanged(object sender,
            RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbEntryType.IsEnabled = chkEntryType.IsChecked != true;
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbAccounts.IsEnabled = chkAll.IsChecked != true;
        }

        private void cmbEntryType_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                int selectedIndex = cmbEntryType.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex <= 13)
                    Restype = selectedIndex;
                else if (selectedIndex >= 21 && selectedIndex <= 23)
                    Restype = selectedIndex + 7;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCostCenterBalance {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) SearchByName();
        }

        private void cmbAccounts_Click(object sender,
            MouseButtonEventArgs e)
        {
            if (chkAll.IsChecked != true)
                addNewItem();
        }

        private void BtnDetailCell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string globalId)
            {
                if (!string.IsNullOrEmpty(globalId) && globalId != "0")
                    EntryOper.ShowEntrySource(globalId);
            }
        }

        #endregion

        #region ── Helper Methods ──

        private static double SafeParseDouble(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            return double.TryParse(value.ToString(),
                out double result) ? result : 0.0;
        }

        private static double SafeParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            string cleaned = value.Replace(",", "").Trim();
            return double.TryParse(cleaned,
                out double result) ? result : 0.0;
        }

        private static void ShowMsg(
            string message,
            string title = "SmartAudit ERP",
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, icon,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);
        }

        #endregion

        #region ── Helper Methods For WPF Grid ──

        private int GetGridRowCount()
        {
            if (GridControl1.ItemsSource is DataView dataView)
                return dataView.Count;

            if (GridControl1.ItemsSource is DataTable dataTable)
                return dataTable.Rows.Count;

            if (GridControl1.ItemsSource is System.Collections.ICollection collection)
                return collection.Count;

            return 0;
        }

        private double GetLeftPanelWidth()
        {
            if (this.Content is Grid mainGrid && mainGrid.ColumnDefinitions.Count > 0)
                return mainGrid.ColumnDefinitions[0].Width.Value;

            return 230;
        }

        private void SetLeftPanelWidth(double width)
        {
            if (this.Content is Grid mainGrid && mainGrid.ColumnDefinitions.Count > 0)
                mainGrid.ColumnDefinitions[0].Width = new GridLength(width);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return "";

            if (value.Contains("\""))
                value = value.Replace("\"", "\"\"");

            if (value.Contains(",") || value.Contains("\n") || value.Contains("\r"))
                value = $"\"{value}\"";

            return value;
        }

        private void ExportCurrentGridToCsv(string filePath)
        {
            var builder = new StringBuilder();

            string[] headers =
            {
        "م",
        "مدين",
        "دائن",
        "الرصيد",
        "الحالة",
        "الرقم العام",
        "رقم السند",
        "الفرع",
        "النوع",
        "التاريخ",
        "البيان"
    };

            builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

            int rowCount = GetGridRowCount();

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                string[] values =
                {
            GridControl1.GetCellValue(rowIndex, "DgvRank")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvDept")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvCredit")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvBalance")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvBalanceStatus")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvGenralNo")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvEntryNo")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvBranch")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvEntryType")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvDate")?.ToString() ?? "",
            GridControl1.GetCellValue(rowIndex, "DgvNote")?.ToString() ?? ""
        };

                builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
            }

            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        #endregion
    }

    #region ── Settings Model ──

    [Serializable]
    public class Settings1
    {
        public int SplitterPosition { get; set; }
    }
    public class CostCenterBalanceLayoutSettings
    {
        public double LeftPanelWidth { get; set; } = 230;
    }
    #endregion
}