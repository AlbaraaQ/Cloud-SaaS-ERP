using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptDailyProcess : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private bool   _printHeader = true;
        private bool   _printFooter = true;
        private bool   _printStamp  = true;
        private int    _printType   = 1;
        private int    _printNo     = 1;
        private string _defPrinter  = "";
        private string _rptName     = "rptDailyProcess.repx";
        private string _rptUrl      = "";

        private ObservableCollection<DailyRow> _dataSource
            = new ObservableCollection<DailyRow>();

        #endregion

        #region Constructor

        public frmRptDailyProcess()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            dgvItems.ItemsSource = _dataSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;
            txtStartTime.Text    = "00:00";
            txtEndTime.Text      = "23:59";
            LoadSafes();
            LoadStocks();
            LoadPrintSettings();
            LoadEmps();
            LoadBranches();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        public void LoadSafes()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks WHERE branch={MainClass.BranchNo} " +
                    $"AND IS_Deleted=0 AND status<>2 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbSafe.ItemsSource       = dt.DefaultView;
                    cmbSafe.DisplayMemberPath = "name";
                    cmbSafe.SelectedValuePath = "id";
                    cmbSafe.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadStocks()
        {
            // مخفي في الأصل - احتفظ بالمنطق
        }

        private void LoadEmps()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbusers.ItemsSource       = dt.DefaultView;
                    cmbusers.DisplayMemberPath = "name";
                    cmbusers.SelectedValuePath = "id";
                    cmbusers.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadBranches()
        {
            try
            {
                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $" AND BranchId={MainClass.BranchNo}";

                using (var da = new SqlDataAdapter(
                    $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0 {branchFilter}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbBranches.ItemsSource       = dt.DefaultView;
                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "BranchId";
                    cmbBranches.SelectedIndex     = -1;
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

        private void ckTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (ckTotalPeriod.IsChecked != true);
            txtDateFrom.IsEnabled  = enabled;
            txtDateTo.IsEnabled    = enabled;
            txtStartTime.IsEnabled = enabled;
            txtEndTime.IsEnabled   = enabled;
        }

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = (chkAllBranches.IsChecked != true);
        }

        private void chkAllSafe_Changed(object sender, RoutedEventArgs e)
        {
            cmbSafe.IsEnabled = (chkAllSafe.IsChecked != true);
        }

        private void ckAllUsers_Changed(object sender, RoutedEventArgs e)
        {
            cmbusers.IsEnabled = (ckAllUsers.IsChecked != true);
        }

        #endregion

        #region Show Button

        private async void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue == null)
            {
                DXMessageBox.Show("اختر الفرع", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkAllSafe.IsChecked != true && cmbSafe.SelectedValue == null)
            {
                DXMessageBox.Show("اختر صندوق", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnShow.IsEnabled = false;
            ProgressBar1.Value = 0;
            ProgressBar1.Maximum = 100;

            await Task.Run(() => ShowResult());

            btnShow.IsEnabled = true;
        }

        private void ShowResult()
        {
            try
            {
                Dispatcher.Invoke(() => _dataSource.Clear());

                DoProcess("مبيعات", 2, 1);
                DoProcess("مرتجع مبيعات", 2, 2);
                DoProcess("نقطة البيع", 3, 1);
                DoProcess("مرتجع نقطة البيع", 3, 2);
                DoProcess("مشتريات", 1, 1);
                DoProcess("مرتجع مشتريات", 1, 2);
                DoProcess2(5);
                DoProcess2(6);
                DoProcess2(7);
                DoProcess2(8);
                DoProcess2(9);

                Dispatcher.Invoke(() => ProgressBar1.Value = 100);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message));
            }
        }

        private void DoProcess(string name, int type1, int type2)
        {
            try
            {
                string cond = BuildCondition(isInv: true);

                DateTime fromDt = DateTime.Parse(
                    txtDateFrom.DateTime.ToShortDateString() + " " + (txtStartTime.Text ?? "00:00"));
                DateTime toDt = DateTime.Parse(
                    txtDateTo.DateTime.ToShortDateString() + " " + (txtEndTime.Text ?? "23:59"));

                int    count = 0;
                double total = 0, cash = 0, stall = 0;

                // عدد الفواتير
                using (var da = new SqlDataAdapter(
                    $"SELECT DISTINCT inv.InvGlobalID FROM inv, inv_sub " +
                    $"WHERE {cond} inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    $"AND inv.inv_type={type1} AND inv.proc_type={type2} AND is_deleted=0",
                    _conn))
                {
                    da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    var dt = new DataTable();
                    da.Fill(dt);
                    count = dt.Rows.Count;
                }

                // الإجمالي
                using (var da = new SqlDataAdapter(
                    $"SELECT SUM(tot_net) FROM " +
                    $"(SELECT MIN(tot_net) AS tot_net FROM inv, inv_sub " +
                    $"WHERE {cond} inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    $"AND inv.inv_type={type1} AND inv.proc_type={type2} AND is_deleted=0 " +
                    $"GROUP BY inv.InvGlobalID) AS tbl", _conn))
                {
                    da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        double.TryParse(dt.Rows[0][0]?.ToString(), out total);
                }

                // نقدي
                using (var da = new SqlDataAdapter(
                    $"SELECT SUM(tot_net) FROM " +
                    $"(SELECT MIN(tot_net) AS tot_net FROM inv, inv_sub " +
                    $"WHERE {cond} inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    $"AND inv.inv_type={type1} AND inv.proc_type={type2} " +
                    $"AND inv.pay_type>0 AND is_deleted=0 " +
                    $"GROUP BY inv.InvGlobalID) AS tbl", _conn))
                {
                    da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        double.TryParse(dt.Rows[0][0]?.ToString(), out cash);
                }

                // آجل
                using (var da = new SqlDataAdapter(
                    $"SELECT SUM(tot_net) FROM " +
                    $"(SELECT MIN(tot_net) AS tot_net FROM inv, inv_sub " +
                    $"WHERE {cond} inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    $"AND inv.inv_type={type1} AND inv.proc_type={type2} " +
                    $"AND inv.pay_type=-1 AND is_deleted=0 " +
                    $"GROUP BY inv.InvGlobalID) AS tbl", _conn))
                {
                    da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        double.TryParse(dt.Rows[0][0]?.ToString(), out stall);
                }

                Dispatcher.Invoke(() => _dataSource.Add(new DailyRow
                {
                    InvoiceType = name,
                    Count       = count,
                    Total       = total,
                    Cash        = cash,
                    Stall       = stall
                }));
            }
            catch { }
        }

        private void DoProcess2(int receiptType)
        {
            try
            {
                string cond = BuildSafeCondition();
                string typeName = Common.ResrirectionType(receiptType);
                DateTime toDate = txtDateTo.DateTime.AddHours(24);

                using (var da = new SqlDataAdapter(
                    $"SELECT COUNT(*) AS RowCounts, SUM(NetVal) AS Payments " +
                    $"FROM receipts " +
                    $"WHERE ReceiptType={receiptType} AND {cond} ISdeleted=0",
                    _conn))
                {
                    if (ckTotalPeriod.IsChecked != true)
                    {
                        da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtDateFrom.DateTime.ToShortDateString();
                        da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = toDate;
                    }

                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        double.TryParse(dt.Rows[0]["Payments"]?.ToString(), out double payments);
                        int.TryParse(dt.Rows[0]["RowCounts"]?.ToString(), out int rowCount);

                        Dispatcher.Invoke(() => _dataSource.Add(new DailyRow
                        {
                            InvoiceType = typeName,
                            Count       = rowCount,
                            Total       = payments,
                            Cash        = 0,
                            Stall       = 0
                        }));
                    }
                }
            }
            catch { }
        }

        private string BuildCondition(bool isInv = false)
        {
            string cond = "";

            if (chkAllSafe.IsChecked != true && cmbSafe.SelectedValue != null)
                cond += $"[stock]={cmbSafe.SelectedValue} AND ";

            if (ckTotalPeriod.IsChecked != true)
                cond += " date>=@date1 AND date<=@date2 AND ";

            if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue != null)
                cond += $"[branch]={cmbBranches.SelectedValue} AND ";

            if (ckAllUsers.IsChecked != true && cmbusers.SelectedValue != null)
                cond += $"[sales_emp]={cmbusers.SelectedValue} AND ";

            return cond;
        }

        private string BuildSafeCondition()
        {
            string cond = "";

            if (chkAllSafe.IsChecked != true && cmbSafe.SelectedValue != null)
                cond += $"TreasuryID={cmbSafe.SelectedValue} AND ";

            if (ckTotalPeriod.IsChecked != true)
                cond += " ReceiptDate>=@date1 AND ReceiptDate<@date2 AND ";

            return cond;
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<RestrictionData>();

            DateTime fromDt = txtDateFrom.DateTime.Date +
                TimeSpan.Parse(txtStartTime.Text ?? "00:00");
            DateTime toDt = txtDateTo.DateTime.Date +
                TimeSpan.Parse(txtEndTime.Text ?? "23:59");

            foreach (var row in _dataSource)
            {
                list.Add(new RestrictionData
                {
                    Safe         = cmbSafe.Text,
                    SafeCode     = cmbSafe.SelectedValue?.ToString() ?? "",
                    InvoiceType  = row.InvoiceType,
                    RestrType    = "تقرير الحركة اليومية",
                    InvoiceCount = row.Count.ToString(),
                    Total        = row.Total.ToString("N2"),
                    Cash         = row.Cash.ToString("N2"),
                    Stall        = row.Stall.ToString("N2"),
                    FromDate     = fromDt.ToString("dd-MM-yyyy HH:mm:ss"),
                    ToDate       = toDt.ToString("dd-MM-yyyy HH:mm:ss"),
                    User         = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate    = DateTime.Now.ToShortDateString()
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptUrl     = MainClass.ReportsPath;
            _rptName    = "rptDailyProcess.repx";
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

        #region Model

        public class DailyRow
        {
            public string InvoiceType { get; set; }
            public int    Count       { get; set; }
            public double Total       { get; set; }
            public double Cash        { get; set; }
            public double Stall       { get; set; }
        }

        #endregion
    }
}