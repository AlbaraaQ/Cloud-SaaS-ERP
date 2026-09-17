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
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptBalances : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private bool   _printHeader = true;
        private bool   _printFooter = true;
        private bool   _printStamp  = true;
        private int    _printType   = 1;
        private int    _printNo     = 1;
        private string _defPrinter  = "";
        private string _rptName     = "";
        private string _rptUrl      = "";

        private int _parentCode = -1;
        public  int _Type       = 1;

        private ObservableCollection<BalanceRow_frmRptBalances> _balancesSource
            = new ObservableCollection<BalanceRow_frmRptBalances>();

        #endregion

        #region Constructor

        public frmRptBalances()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            GridControl1.ItemsSource = _balancesSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;
            txtStartTime.Text    = "00:00";
            txtEndTime.Text      = "23:59";
            LoadPrintSettings();
            LoadAccounts();
            LoadBranches();
            LoadSalesmen();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadAccounts()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE type=1 {Accounting.BranchCondition}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        cmbAccounts.ItemsSource       = dt.DefaultView;
                        cmbAccounts.DisplayMemberPath = "AName";
                        cmbAccounts.SelectedValuePath = "Code";
                        cmbAccounts.SelectedIndex     = -1;
                    }
                }
            }
            catch { }
        }

        private void LoadSalesmen()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbSalesman.ItemsSource       = dt.DefaultView;
                    cmbSalesman.DisplayMemberPath = "name";
                    cmbSalesman.SelectedValuePath = "id";
                }
            }
            catch { }
        }

        private void LoadBranches()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbBranch.ItemsSource       = dt.DefaultView;
                    cmbBranch.DisplayMemberPath = "name";
                    cmbBranch.SelectedValuePath = "id";
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
                        try { _printType   = Convert.ToInt32(dt.Rows[0]["printType"]);           } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);       } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);       } catch { }
                        try { _printStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);        } catch { }
                        try { _defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();             } catch { }
                        try { _printNo     = Convert.ToInt32(dt.Rows[0]["printNo"]);             } catch { }

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region CheckBox Events

        private void chkAll_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (chkAll.IsChecked != true);
            txtDateFrom.IsEnabled  = enabled;
            txtDateTo.IsEnabled    = enabled;
            txtStartTime.IsEnabled = enabled;
            txtEndTime.IsEnabled   = enabled;
        }

        private void chkAllBranch_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranch.IsEnabled = (chkAllBranch.IsChecked != true);
            if (chkAllBranch.IsChecked == true)
                cmbBranch.Text = "";
        }

        private void chkSalesman_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSalesman.IsEnabled = (chkSalesman.IsChecked != true);
        }

        #endregion

        #region Show Button

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAccounts.SelectedIndex == -1)
            {
                DXMessageBox.Show("يرجى اختيار حساب رئيسي",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowResult();
        }

        #endregion

        #region Show Result

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

                    string parentStr = dt.Rows[0][0].ToString();
                    double parentVal = 0;
                    double.TryParse(parentStr, out parentVal);

                    if (cmbAccounts.SelectedValue != null)
                    {
                        double selectedVal = 0;
                        double.TryParse(cmbAccounts.SelectedValue.ToString(), out selectedVal);

                        if (parentVal == selectedVal)
                            _parentCode = (int)parentVal;
                        else if (string.IsNullOrEmpty(parentStr) || parentVal == 0)
                            _parentCode = -1;
                        else
                            GetParent((int)parentVal);
                    }
                }
            }
            catch { }
        }

        private void ShowResult()
        {
            try
            {
                _balancesSource.Clear();
                txtIsBalanced.Text = "";
                _parentCode = -1;

                string branchCond = (chkAllBranch.IsChecked == true)
                    ? ""
                    : $"Entry.branch={cmbBranch.SelectedValue} AND Entry_sub.branch={cmbBranch.SelectedValue} AND ";

                string dateCond = "";
                if (chkAll.IsChecked != true)
                    dateCond = " AND Entry.date>=@date1 AND Entry.date<=@date2 ";

                string salesmanCond = "";
                if (chkSalesman.IsChecked != true && cmbSalesman.SelectedValue != null)
                    salesmanCond = $" AND Entry_Sub.salesman={cmbSalesman.SelectedValue}";

                // تحويل الأوقات
                DateTime fromDt = DateTime.Parse(
                    txtDateFrom.DateTime.ToShortDateString() + " " + (txtStartTime.Text ?? "00:00"));
                DateTime toDt = DateTime.Parse(
                    txtDateTo.DateTime.ToShortDateString() + " " + (txtEndTime.Text ?? "23:59"));

                // جلب الحركات
                var dtMovements = new DataTable();
                using (var da = new SqlDataAdapter(
                    $"SELECT Entry_sub.acc_no, Accounts_Index.AName, " +
                    $"SUM(Entry_sub.dept) AS dept, SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub, Accounts_Index " +
                    $"WHERE {branchCond} Entry.IS_Deleted=0 AND Entry.type<>0 AND Entry.state=1 " +
                    $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                    $"AND Entry_sub.acc_no=Accounts_Index.Code {dateCond} {salesmanCond} " +
                    $"GROUP BY Entry_sub.acc_no, Accounts_Index.AName",
                    _conn))
                {
                    da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    da.Fill(dtMovements);
                }

                // جلب الأرصدة الافتتاحية
                var dtInitial = new DataTable();
                using (var da2 = new SqlDataAdapter(
                    $"SELECT Entry_sub.acc_no, Accounts_Index.AName, " +
                    $"SUM(Entry_sub.dept) AS dept, SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub, Accounts_Index " +
                    $"WHERE {branchCond} Entry.IS_Deleted=0 AND Entry.type=0 AND Entry.state=1 " +
                    $"{dateCond} {salesmanCond} " +
                    $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                    $"AND Entry_sub.acc_no=Accounts_Index.Code " +
                    $"GROUP BY Entry_sub.acc_no, Accounts_Index.AName",
                    _conn))
                {
                    da2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                        = txtDateFrom.DateTime.ToShortDateString();
                    da2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                        = txtDateTo.DateTime.AddHours(24);
                    da2.Fill(dtInitial);
                }

                // بناء جدول مؤقت
                var tempTable = new DataTable();
                tempTable.Columns.Add("ParentCode");
                tempTable.Columns.Add("Code");
                tempTable.Columns.Add("Name");
                tempTable.Columns.Add("DeptInit");
                tempTable.Columns.Add("CreditInit");
                tempTable.Columns.Add("Dept");
                tempTable.Columns.Add("Credit");

                ProgressBar1.Maximum = dtMovements.Rows.Count > 0 ? dtMovements.Rows.Count : 1;
                ProgressBar1.Value   = 0;

                foreach (DataRow row in dtMovements.Rows)
                {
                    _parentCode = -1;
                    GetParent(Convert.ToInt32(row["acc_no"]));

                    if (_parentCode != -1)
                    {
                        double deptInit = 0, creditInit = 0;
                        foreach (DataRow initRow in dtInitial.Rows)
                        {
                            if (initRow["acc_no"].ToString() == row["acc_no"].ToString())
                            {
                                double.TryParse(initRow["dept"].ToString(),   out deptInit);
                                double.TryParse(initRow["credit"].ToString(), out creditInit);
                                break;
                            }
                        }

                        tempTable.Rows.Add(
                            _parentCode,
                            row["acc_no"],
                            row["AName"],
                            deptInit,
                            creditInit,
                            row["dept"],
                            row["credit"]);
                    }

                    ProgressBar1.Value++;
                }

                // إضافة الحسابات الموجودة في الافتتاحي فقط
                foreach (DataRow initRow in dtInitial.Rows)
                {
                    bool exists = false;
                    foreach (DataRow tr in tempTable.Rows)
                    {
                        if (tr[1].ToString() == initRow["acc_no"].ToString())
                        { exists = true; break; }
                    }

                    if (!exists)
                    {
                        _parentCode = -1;
                        GetParent(Convert.ToInt32(initRow["acc_no"]));
                        if (_parentCode != -1)
                        {
                            double.TryParse(initRow["dept"].ToString(),   out double di);
                            double.TryParse(initRow["credit"].ToString(), out double ci);
                            tempTable.Rows.Add(_parentCode, initRow["acc_no"], initRow["AName"],
                                di, ci, 0, 0);
                        }
                    }
                }

                // بناء النتائج
                ProgressBar1.Maximum = tempTable.Rows.Count > 0 ? tempTable.Rows.Count : 1;
                ProgressBar1.Value   = 0;

                double sumDeptFinal = 0, sumCreditFinal = 0;

                foreach (DataRow row in tempTable.Rows)
                {
                    ProgressBar1.Value++;

                    double deptInit   = Math.Round(Convert.ToDouble(row[3]), 2);
                    double creditInit = Math.Round(Convert.ToDouble(row[4]), 2);
                    double dept       = Math.Round(Convert.ToDouble(row[5]), 2);
                    double credit     = Math.Round(Convert.ToDouble(row[6]), 2);

                    double deptBlc, creditBlc;
                    if (dept >= credit)
                    {
                        deptBlc   = dept - credit;
                        creditBlc = 0;
                    }
                    else
                    {
                        deptBlc   = 0;
                        creditBlc = credit - dept;
                    }

                    double deptFinal   = deptInit + deptBlc;
                    double creditFinal = creditInit + creditBlc;

                    double finalDebt, finalCredit;
                    if (deptFinal >= creditFinal)
                    {
                        finalDebt   = Math.Round(deptFinal - creditFinal, 2);
                        finalCredit = 0;
                    }
                    else
                    {
                        finalDebt   = 0;
                        finalCredit = Math.Round(creditFinal - deptFinal, 2);
                    }

                    sumDeptFinal   += finalDebt;
                    sumCreditFinal += finalCredit;

                    _balancesSource.Add(new BalanceRow_frmRptBalances
                    {
                        DgvAccount          = row[1].ToString(),
                        DgvAccName          = row[2].ToString(),
                        DgvDeptIntial       = deptInit,
                        DgvCreditIntial     = creditInit,
                        DgvDebtorMovement   = dept,
                        DgvCreditorMovement = credit,
                        DgvDeptBlc          = deptBlc,
                        DgvCreditBlc        = creditBlc,
                        DgvDeptFinal        = finalDebt,
                        DgvCreditFinal      = finalCredit
                    });
                }

                // الرصيد
                if (sumCreditFinal > sumDeptFinal)
                {
                    txtBalance.Text = (sumCreditFinal - sumDeptFinal).ToString(Common.DigitsNo);
                    lblStatus.Text  = "دائن";
                }
                else if (sumDeptFinal > sumCreditFinal)
                {
                    txtBalance.Text = (sumDeptFinal - sumCreditFinal).ToString(Common.DigitsNo);
                    lblStatus.Text  = "مدين";
                }
                else
                {
                    txtBalance.Text = "0";
                    lblStatus.Text  = "";
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Detail Button

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string accCode)
                {
                    int.TryParse(accCode, out int num);
                    var frm = new frmAccountBalance();
                    MainClass.ApplyPermissionToForm(frm);
                    MainClass.DoApplyUserSett(frm);
                    frm.Show();
                    frm.cmbAccounts.SelectedValue = num;
                    frm.ShowAccountData();
                    frm.Activate();
                }
            }
            catch { }
        }

        #endregion

        #region Print / Preview / Export

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_balancesSource.Count == 0) return;

                var sb = new StringBuilder();
                sb.AppendLine("الحساب,اسم الحساب,رصيد افتتاحي مدين,رصيد افتتاحي دائن," +
                              "حركة مدين,حركة دائن,رصيد مدين,رصيد دائن," +
                              "رصيد ختامي مدين,رصيد ختامي دائن");

                foreach (var row in _balancesSource)
                {
                    sb.AppendLine(
                        $"{row.DgvAccount},{row.DgvAccName}," +
                        $"{row.DgvDeptIntial:N2},{row.DgvCreditIntial:N2}," +
                        $"{row.DgvDebtorMovement:N2},{row.DgvCreditorMovement:N2}," +
                        $"{row.DgvDeptBlc:N2},{row.DgvCreditBlc:N2}," +
                        $"{row.DgvDeptFinal:N2},{row.DgvCreditFinal:N2}");
                }

                string fileName = "Balances_Export.csv";
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

        private DataSet BindToData()
        {
            var list = new List<RestrictionData>();

            foreach (var row in _balancesSource)
            {
                list.Add(new RestrictionData
                {
                    MainAccountName = cmbAccounts.Text,
                    MainAccountCode = cmbAccounts.SelectedValue?.ToString() ?? "",
                    AccountCode     = row.DgvAccount,
                    AccountName     = row.DgvAccName,
                    DeptIntial      = row.DgvDeptIntial.ToString("N2"),
                    CreditIntial    = row.DgvCreditIntial.ToString("N2"),
                    Dept            = row.DgvDebtorMovement.ToString("N2"),
                    Credit          = row.DgvCreditorMovement.ToString("N2"),
                    DeptBalance     = row.DgvDeptBlc.ToString("N2"),
                    CreditBalance   = row.DgvCreditBlc.ToString("N2"),
                    DeptFinal       = row.DgvDeptFinal.ToString("N2"),
                    CreditFinal     = row.DgvCreditFinal.ToString("N2"),
                    RestrType       = "أرصدة الحسابات",
                    FromDate        = txtDateFrom.DateTime.ToString(),
                    ToDate          = txtDateTo.DateTime.ToString(),
                    User            = Common.GetEmpName(MainClass.EmpNo),
                    Branch          = cmbBranch.Text,
                    PrintDate       = DateTime.Now.ToShortDateString(),
                    Balance         = txtBalance.Text,
                    BalanceType     = lblStatus.Text
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptUrl     = MainClass.ReportsPath;
            _rptName    = "rptAccountBalance.repx";
            _defPrinter = MainClass.ReportsPrinter;

            if (_balancesSource.Count == 0)
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

                // Header
                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
                }

                // Footer
                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var subFooter = report.FindControl("footerRpt", true) as XRSubreport;
                    if (subFooter != null) subFooter.ReportSource = footerRpt;
                }

                report.DataSource = BindToData();

                if (string.IsNullOrEmpty(_defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = _defPrinter;
                if (printType == 1)
                    for (int i = 0; i < _printNo; i++)
                        report.Print();
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
    }

    #region Model

    public class BalanceRow_frmRptBalances
    {
        public string DgvAccount          { get; set; }
        public string DgvAccName          { get; set; }
        public double DgvDeptIntial       { get; set; }
        public double DgvCreditIntial     { get; set; }
        public double DgvDebtorMovement   { get; set; }
        public double DgvCreditorMovement { get; set; }
        public double DgvDeptBlc          { get; set; }
        public double DgvCreditBlc        { get; set; }
        public double DgvDeptFinal        { get; set; }
        public double DgvCreditFinal      { get; set; }
    }

    #endregion
}