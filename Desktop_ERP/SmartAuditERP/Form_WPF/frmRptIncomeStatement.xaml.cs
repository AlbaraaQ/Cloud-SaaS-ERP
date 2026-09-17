using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptIncomeStatement : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private bool   _printHeader = true;
        private bool   _printFooter = true;
        private bool   _printStamp  = true;
        private int    _printType   = 1;
        private int    _printNo     = 1;
        private string _defPrinter  = "";
        private string _rptName     = "RptIncomeStatement.repx";
        private string _rptUrl      = "";

        private int _parentCode = -1;
        public  int _Type       = 1;
        public  int _FormType   = 1;

        private ObservableCollection<IncomeRow> _dataSource
            = new ObservableCollection<IncomeRow>();

        #endregion

        #region Constructor

        public frmRptIncomeStatement()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            dgvSrch.ItemsSource = _dataSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;
            LoadPrintSettings();
            LoadBranches();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadBranches()
        {
            try
            {
                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $" AND id={MainClass.BranchNo}";

                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches WHERE IS_Deleted=0 {branchFilter}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbBranches.ItemsSource       = dt.DefaultView;
                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";

                    if (MainClass.BranchNo != -1)
                        cmbBranches.SelectedValue = MainClass.BranchNo;
                    else
                        cmbBranches.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void LoadPrintSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try { _printType   = Convert.ToInt32(dt.Rows[0]["printType"]);     } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]); } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]); } catch { }
                        try { _printStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);  } catch { }
                        try { _defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();       } catch { }
                        try { _printNo     = Convert.ToInt32(dt.Rows[0]["printNo"]);       } catch { }
                        try { _rptName     = dt.Rows[0]["RptName"].ToString();             } catch { }
                        try { _rptUrl      = dt.Rows[0]["RptUrl"].ToString();              } catch { }

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region CheckBox Events

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = (chkAllBranches.IsChecked != true);
            if (chkAllBranches.IsChecked == true)
                cmbBranches.SelectedIndex = -1;
        }

        private void ckTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (ckTotalPeriod.IsChecked != true);
            txtFromDate.IsEnabled  = enabled;
            txtToDate.IsEnabled    = enabled;
            txtStartTime.IsEnabled = enabled;
            txtEndTime.IsEnabled   = enabled;
        }

        #endregion

        #region GetParent

        private void GetParent(int code)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT ParentCode FROM Accounts_Index WHERE Code={code}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 0) return;

                    double parentVal = Convert.ToDouble(dt.Rows[0][0]);
                    if (parentVal < 10)
                        _parentCode = code;
                    else
                        GetParent((int)parentVal);
                }
            }
            catch { }
        }

        #endregion

        #region Show Result

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime fromDate = DateTime.Parse(
                    txtFromDate.DateTime.ToShortDateString() + " " +
                    (txtStartTime.Text ?? "00:00"));
                DateTime toDate = DateTime.Parse(
                    txtToDate.DateTime.ToShortDateString() + " " +
                    (txtEndTime.Text ?? "23:59"));

                int branch = MainClass.BranchNo;
                if (cmbBranches.SelectedIndex != -1 && cmbBranches.SelectedValue != null)
                    branch = Convert.ToInt32(cmbBranches.SelectedValue);

                ShowResult(fromDate, toDate, branch);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        public void ShowResult(DateTime fromDate, DateTime toDate, int branch)
        {
            try
            {
                _dataSource.Clear();
                txtProfit1.Text     = "";
                txtProfit2.Text     = "";
                txtStockVal.Text    = "";
                txtStockVal1.Text   = "";
                txtCreditDiff.Text  = "";
                txtDeptDiff.Text    = "";
                txtIsBalanced.Text  = "";

                double totalDept   = 0;
                double totalCredit = 0;

                string dateCond = "";
                if (ckTotalPeriod.IsChecked != true)
                    dateCond = " AND Entry.date BETWEEN @date1 AND @date2 ";

                string branchCond = "";
                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedIndex != -1)
                    branchCond = $" AND Entry_sub.branch={cmbBranches.SelectedValue} ";

                string baseQuery =
                    $"SELECT Entry_sub.acc_no AS AccountCode, Accounts_Index.AName AS AccountName, " +
                    $"SUM(Entry_sub.dept) AS Debet, SUM(Entry_sub.credit) AS Credit " +
                    $"FROM Entry, Entry_sub, Accounts_Index " +
                    $"WHERE Entry.IS_Deleted=0 AND Entry.state=1 " +
                    $"AND Entry.GlobalId=Entry_sub.EntryGlobalId " +
                    $"AND Entry_sub.acc_no=Accounts_Index.Code " +
                    $"AND Accounts_Index.FinalAcc=2 " +
                    $"{dateCond} {branchCond} " +
                    $"GROUP BY Entry_sub.acc_no, Accounts_Index.AName";

                EnsureOpen(_conn);

                using (var cmd = new SqlCommand(baseQuery, _conn))
                {
                    cmd.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDate;
                    cmd.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate;

                    var dt2 = new DataTable();
                    new SqlDataAdapter(cmd).Fill(dt2);

                    // بناء جدول مؤقت مجمّع
                    var tempTable = new DataTable();
                    tempTable.Columns.Add("code");
                    tempTable.Columns.Add("name");
                    tempTable.Columns.Add("dept");
                    tempTable.Columns.Add("credit");

                    if (_Type == 1)
                    {
                        foreach (DataRow row in dt2.Rows)
                        {
                            _parentCode = -1;

                            using (var da2 = new SqlDataAdapter(
                                $"SELECT Parent.Code AS ParentCode, Parent.AName AS ParentAccountName " +
                                $"FROM Accounts_Index AS Child " +
                                $"JOIN Accounts_Index AS Parent ON Child.ParentCode=Parent.Code " +
                                $"WHERE Child.Code='{Convert.ToDouble(row["AccountCode"])}'", _conn))
                            {
                                var dt3 = new DataTable();
                                da2.Fill(dt3);
                                if (dt3.Rows.Count > 0)
                                {
                                    _parentCode = Convert.ToInt32(dt3.Rows[0]["ParentCode"]);
                                    tempTable.Rows.Add(
                                        _parentCode,
                                        dt3.Rows[0]["ParentAccountName"],
                                        row["Debet"],
                                        row["Credit"]);
                                }
                            }
                        }

                        // دمج الصفوف المتكررة
                        for (int i = 0; i < tempTable.Rows.Count; i++)
                        {
                            for (int k = i + 1; k < tempTable.Rows.Count; k++)
                            {
                                if (tempTable.Rows[i][0].ToString() ==
                                    tempTable.Rows[k][0].ToString())
                                {
                                    double d1 = Convert.ToDouble(tempTable.Rows[i][2]);
                                    double c1 = Convert.ToDouble(tempTable.Rows[i][3]);
                                    double d2 = Convert.ToDouble(tempTable.Rows[k][2]);
                                    double c2 = Convert.ToDouble(tempTable.Rows[k][3]);
                                    tempTable.Rows[i][2] = d1 + d2;
                                    tempTable.Rows[i][3] = c1 + c2;
                                    tempTable.Rows.RemoveAt(k);
                                    k--;
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (DataRow row in dt2.Rows)
                            tempTable.Rows.Add(row["AccountCode"], row["AccountName"],
                                row["Debet"], row["Credit"]);
                    }

                    // إضافة الصفوف للجدول
                    foreach (DataRow row in tempTable.Rows)
                    {
                        double dept   = Convert.ToDouble(row[2]);
                        double credit = Convert.ToDouble(row[3]);

                        double deptBalance   = 0;
                        double creditBalance = 0;

                        if (dept >= credit)
                            deptBalance = dept - credit;
                        else
                            creditBalance = credit - dept;

                        _dataSource.Add(new IncomeRow
                        {
                            Code          = row[0].ToString(),
                            Name          = row[1].ToString(),
                            DeptBalance   = deptBalance,
                            CreditBalance = creditBalance
                        });

                        totalDept   += deptBalance;
                        totalCredit += creditBalance;
                    }
                }

                // حساب المخزون
                CalcStockCost();
                CalcStockFirstVal();

                if (double.TryParse(txtStockVal.Text, out double stockVal))
                    totalCredit += stockVal;
                if (double.TryParse(txtStockVal1.Text, out double stockVal1))
                    totalDept += stockVal1;

                // حساب الأرباح
                totalCredit = Math.Round(totalCredit, 2);
                totalDept   = Math.Round(totalDept, 2);

                if (totalCredit >= totalDept)
                {
                    txtProfitLbl.Text = (_FormType == 1) ? "صافي أرباح العام" : "صافي خسائر العام";
                    txtProfit1.Text = $"{Math.Round(totalCredit - totalDept, 2):N2}";
                    totalDept += Convert.ToDouble(txtProfit1.Text.Replace(",", ""));
                    txtProfit2.Text = "0";
                }
                else
                {
                    txtProfitLbl.Text = (_FormType == 2) ? "صافي أرباح العام" : "صافي خسائر العام";
                    txtProfit2.Text = $"{Math.Round(totalDept - totalCredit, 3):N2}";
                    totalCredit += Convert.ToDouble(txtProfit2.Text.Replace(",", ""));
                    txtProfit1.Text = "0";
                }

                txtDeptDiff.Text   = $"{totalDept:N2}";
                txtCreditDiff.Text = $"{totalCredit:N2}";
                txtIsBalanced.Text = "✅ الحسابات متوازنة";
                txtIsBalanced.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        private void CalcStockCost()
        {
            try
            {
                int branch = MainClass.BranchNo;
                if (cmbBranches.SelectedIndex != -1 && cmbBranches.SelectedValue != null)
                    branch = Convert.ToInt32(cmbBranches.SelectedValue);

                double invCost = Inventory.InventoryCost(branch, txtToDate.DateTime);

                if (_FormType == 1)
                {
                    txtStockVal.Text  = invCost.ToString(Common.DigitsNo);
                    txtStockVal1.Text = "0";
                }
                else
                {
                    txtStockVal1.Text = invCost.ToString(Common.DigitsNo);
                    txtStockVal.Text  = "0";
                }
            }
            catch { }
        }

        private void CalcStockFirstVal()
        {
            try
            {
                string branchCond = "";
                int branch = MainClass.BranchNo;
                if (cmbBranches.SelectedIndex != -1 && cmbBranches.SelectedValue != null)
                    branch = Convert.ToInt32(cmbBranches.SelectedValue);

                if (branch != -1)
                    branchCond = $"inv.branch={branch} AND ";

                using (var da = new SqlDataAdapter(
                    $"SELECT ItemId, SUM(val), SUM(val1*exchange_price) " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchCond} inv_type=9 AND inv.proc_type=1 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 " +
                    $"GROUP BY ItemId",
                    _conn))
                {
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                        = txtToDate.DateTime.AddHours(24);

                    var dt = new DataTable();
                    da.Fill(dt);

                    double totalFirst = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        try { totalFirst += Math.Round(Convert.ToDouble(row[2]), 3); } catch { }
                    }

                    if (_FormType == 1)
                    {
                        txtFirstVal.Text  = totalFirst.ToString();
                        txtFirstVal1.Text = "0";
                    }
                    else
                    {
                        txtFirstVal1.Text = totalFirst.ToString();
                        txtFirstVal.Text  = "0";
                    }
                }
            }
            catch { }
        }

        private decimal GetOpenstock()
        {
            try
            {
                using (var conn2 = new SqlConnection(MainClass.connstr))
                {
                    conn2.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT SUM(invsum) AS sumopenstock FROM inv WHERE inv_type=9 AND proc_type=1",
                        conn2))
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read() && dr["sumopenstock"] != DBNull.Value)
                            return Convert.ToDecimal(dr["sumopenstock"]);
                    }
                }
            }
            catch { }
            return 0;
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dataSource.Count == 0) return;

                var sb = new StringBuilder();
                sb.AppendLine("الحساب,اسم الحساب,رصيد مدين,رصيد دائن");
                foreach (var row in _dataSource)
                    sb.AppendLine($"{row.Code},{row.Name},{row.DeptBalance:N2},{row.CreditBalance:N2}");

                string fileName = "IncomeStatement.csv";
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(fileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير: " + ex.Message);
            }
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<RestrictionData>();

            foreach (var row in _dataSource)
            {
                list.Add(new RestrictionData
                {
                    AccountCode = row.Code,
                    AccountName = row.Name,
                    Dept        = row.DeptBalance.ToString("N2"),
                    Credit      = row.CreditBalance.ToString("N2"),
                    RestrType   = "قائمة الدخل الرئيسية",
                    stockVal    = txtStockVal.Text,
                    Stock       = txtStockVal1.Text,
                    Profit1     = txtProfit1.Text,
                    Profit2     = txtProfit2.Text,
                    Profit      = txtProfitLbl.Text,
                    DeptDiff    = txtDeptDiff.Text,
                    CreditDiff  = txtCreditDiff.Text,
                    ToDate      = txtToDate.DateTime.ToString(),
                    Balance     = txtIsBalanced.Text,
                    User        = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate   = DateTime.Now.ToShortDateString()
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptUrl     = MainClass.ReportsPath;
            _rptName    = "RptIncomeStatement.repx";
            _defPrinter = MainClass.ReportsPrinter;

            if (_dataSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);
            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BindToData();

                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
                }

                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var subFooter = report.FindControl("footerRpt", true) as XRSubreport;
                    if (subFooter != null) subFooter.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(_defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = _defPrinter;
                if (printType == 1)
                    for (int i = 0; i < _printNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message);
            }
        }

        #endregion

        #region Hidden fields (from XAML)
        // هذه الحقول مرتبطة بعناصر في XAML ولكن لم يتم استخدامها في المنطق الأصلي
        private System.Windows.Controls.TextBox txtFirstVal  => FindName("txtFirstVal")  as System.Windows.Controls.TextBox;
        private System.Windows.Controls.TextBox txtFirstVal1 => FindName("txtFirstVal1") as System.Windows.Controls.TextBox;
        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion

        #region Model

        public class IncomeRow
        {
            public string Code          { get; set; }
            public string Name          { get; set; }
            public double DeptBalance   { get; set; }
            public double CreditBalance { get; set; }
        }

        #endregion
    }
}