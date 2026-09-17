using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptSalesInPeriod : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        private DateTime DateTimeFrom;
        private DateTime DateTimeTo;

        public double _Sum;
        private double SumSaleVal;
        private double SumSale;

        private bool   PrintHeader;
        private bool   PrintFooter;
        private bool   PrintStamp;
        private int    PrintType;
        private int    PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<SaleItemRow>    _itemsData
            = new ObservableCollection<SaleItemRow>();
        private ObservableCollection<SaleInvoiceRow> _invoiceData
            = new ObservableCollection<SaleInvoiceRow>();

        private double _sumSale2 = 0.0;

        #endregion

        #region Constructor

        public frmRptSalesInPeriod()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            date1.DateTime = DateTime.Now;
            date2.DateTime = DateTime.Now.AddHours(24);
            cmbInvType.SelectedIndex = 0;
            LoadPrintSettings();

            dgvItems.ItemsSource  = _itemsData;
            dgvSrch.ItemsSource   = _invoiceData;
        }

        #endregion

        #region CalcStock - حساب إجمالي حركة المواد

        private void CalcStock()
        {
            try
            {
                _itemsData.Clear();
                SumSaleVal = 0.0;
                SumSale    = 0.0;

                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                var adapter = new SqlDataAdapter(
                    "SELECT id, name, nameEN, sale_price FROM Items " +
                    "WHERE IS_Deleted=0 ORDER BY id", conn);
                var dtItems = new DataTable();
                adapter.Fill(dtItems);

                ProgressBar1.Maximum = Math.Max(dtItems.Rows.Count, 1);
                ProgressBar1.Value   = 0;

                int invTypeIndex = cmbInvType.SelectedIndex; // 0=POS(3), 1=Sales(2)

                for (int i = 0; i < dtItems.Rows.Count; i++)
                {
                    ProgressBar1.Value = i + 1;
                    System.Windows.Application.Current.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => { }));

                    DataRow itemRow = dtItems.Rows[i];
                    int     itemId  = Convert.ToInt32(itemRow["id"]);

                    double qty   = 0.0;
                    double retQty= 0.0;
                    double total = 0.0;
                    double retTot= 0.0;

                    int invType = invTypeIndex == 1 ? 2 : 3;

                    // مبيعات
                    var (q1, t1) = GetSaleData(itemId, invType, 1, branchFilter);
                    qty   += q1;
                    total += t1;

                    // مرتجع
                    var (q2, t2) = GetSaleData(itemId, invType, 2, branchFilter);
                    retQty += q2;
                    retTot += t2;

                    if (qty == 0.0) continue;

                    double netQty   = qty - retQty;
                    double netTotal = Math.Round(total - retTot, 2);

                    SumSaleVal += netQty;
                    SumSale    += netTotal;

                    string itemName = string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? itemRow["name"].ToString()
                        : itemRow["nameEN"].ToString();

                    _itemsData.Add(new SaleItemRow
                    {
                        ItemId   = itemId,
                        ItemName = itemName,
                        Quantity = netQty,
                        Total    = netTotal,
                    });
                }

                txtSumSale.Text = Math.Round(SumSale, 2).ToString("N2");
                ProgressBar1.Value = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في حساب حركة المواد:\n" + ex.Message);
            }
        }

        private (double qty, double total) GetSaleData(
            int itemId, int invType, int procType, string branchFilter)
        {
            try
            {
                string sql =
                    $"SELECT SUM(val), SUM(val * exchange_price) " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter} Inv_Sub.ItemId={itemId} " +
                    $"AND date>=@date1 AND date<=@date2 " +
                    $"AND inv.inv_type={invType} AND inv.proc_type={procType} " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND inv.IS_Deleted=0";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = DateTimeFrom.ToString();
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = DateTimeTo.ToString();

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dt.Rows[0][0].ToString()) &&
                    dt.Rows[0][0] != DBNull.Value)
                {
                    double.TryParse(dt.Rows[0][0].ToString(), out double q);
                    double.TryParse(dt.Rows[0][1].ToString(), out double t);
                    return (q, Math.Round(t, 4));
                }
            }
            catch { }
            return (0.0, 0.0);
        }

        #endregion

        #region ShowResults - عرض الفواتير

        private void ShowResults()
        {
            try
            {
                _invoiceData.Clear();
                _sumSale2 = 0.0;

                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                int invType = cmbInvType.SelectedIndex == 1 ? 2 : 3;

                string sql =
                    $"SELECT * FROM Inv " +
                    $"WHERE Inv_type={invType} " +
                    $"AND Proc_type<>3 AND Proc_type<>4 " +
                    $"AND IS_Buy=0 AND IS_Deleted=0 " +
                    $"AND {branchFilter} date>=@date1 AND date<=@date2 " +
                    $"ORDER BY Inv.id";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = DateTimeFrom.ToString();
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = DateTimeTo.ToString();

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int procType = Convert.ToInt32(row["proc_type"]);
                    string procName = procType == 1 ? "بيع" : "مرتجع";

                    bool isPostpone = false;
                    if (row["pay_type"] != DBNull.Value &&
                        Convert.ToInt32(row["pay_type"]) == -1)
                        isPostpone = true;

                    double totNet = ParseDouble(row["tot_net"]);
                    if (procType == 1)      _sumSale2 += totNet;
                    else if (procType == 2) _sumSale2 -= totNet;

                    _invoiceData.Add(new SaleInvoiceRow
                    {
                        InvGlobalID = row["InvGlobalID"].ToString(),
                        id          = row["id"].ToString(),
                        ProcTypeName= procName,
                        InvDate     = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        InvTime     = Convert.ToDateTime(row["date"]).ToLongTimeString(),
                        IsPostpone  = isPostpone,
                        Cash        = ParseDouble(row["cash"]),
                        Visa        = ParseDouble(row["visa"]),
                        InvTotal    = ParseDouble(row["InvTotal"]),
                        Tax         = ParseDouble(row["tax"]),
                        Minus       = ParseDouble(row["minus"]),
                        TotNet      = totNet,
                    });
                }

                txtSumSale2.Text = Math.Round(_sumSale2, 2).ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض الفواتير:\n" + ex.Message);
            }
        }

        private double ParseDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0.0;
            return double.TryParse(val.ToString(), out double d) ? d : 0.0;
        }

        #endregion

        #region Clear

        private void Clr()
        {
            _itemsData.Clear();
            _invoiceData.Clear();
            txtSumSale.Text  = "";
            txtSumSale2.Text = "";
            _sumSale2        = 0.0;
            SumSale          = 0.0;
            SumSaleVal       = 0.0;
            ProgressBar1.Value = 0;
        }

        #endregion

        #region Print / Preview / Export

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _itemsData)
            {
                list.Add(new InventoryData
                {
                    ProcessType  = cmbInvType.Text,
                    FromDate     = date1.DateTime.ToShortDateString(),
                    ToDate       = date2.DateTime.ToShortDateString(),
                    ItemCode     = row.ItemId.ToString(),
                    ItemName     = row.ItemName,
                    Quantity     = row.Quantity.ToString("N2"),
                    Total        = row.Total.ToString("N2"),
                    InventoryType= this.Title,
                    SumSell      = txtSumSale.Text,
                    User         = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate    = DateTime.Now.ToShortDateString(),
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private DataSet BuildReportDataSetInv()
        {
            var list = new List<InventoryData>();

            foreach (var row in _invoiceData)
            {
                list.Add(new InventoryData
                {
                    ProcessType  = cmbInvType.Text,
                    FromDate     = date1.DateTime.ToShortDateString(),
                    ToDate       = date2.DateTime.ToShortDateString(),
                    InvoiceNo    = row.InvGlobalID,
                    InvType      = row.id,
                    InvDate      = row.ProcTypeName,
                    InvTime      = row.InvDate,
                    Sale         = row.InvTime,
                    Cash         = row.Cash.ToString("N2"),
                    Network      = row.Visa.ToString("N2"),
                    Total        = row.InvTotal.ToString("N2"),
                    Tax          = row.Tax.ToString("N2"),
                    NetDiscount  = row.Minus.ToString("N2"),
                    NetTotal     = row.TotNet.ToString("N2"),
                    InventoryType= this.Title,
                    SumSell      = txtSumSale.Text,
                    User         = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate    = DateTime.Now.ToShortDateString(),
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printMode)
        {
            RptUrl     = MainClass.ReportsPath;
            defPrinter = MainClass.ReportsPrinter;
            RptName    = TabControl1.SelectedIndex == 0
                         ? "RptSalesInPeriod1.repx"
                         : "RptSalesInPeriod2.repx";

            if (_itemsData.Count == 0 && _invoiceData.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = TabControl1.SelectedIndex == 0
                    ? BuildReportDataSet()
                    : BuildReportDataSetInv();

                // Header subreport
                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var hCtrl = report.FindControl("headerRpt", true) as XRSubreport;
                    if (hCtrl != null) hCtrl.ReportSource = headerRpt;
                }

                // Footer subreport
                string footerPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var fCtrl = report.FindControl("footerRpt", true) as XRSubreport;
                    if (fCtrl != null) fCtrl.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;
                if (printMode == 1)
                    for (int c = 1; c <= PrintNo; c++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة:\n" + ex.Message);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter     = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName   = $"SalesInPeriod_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dlg.ShowDialog() != true) return;

                string filePath = dlg.FileName;

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    if (TabControl1.SelectedIndex == 0)
                    {
                        sw.WriteLine("\"رقم الصنف\",\"الصنف\",\"الكمية\",\"الإجمالي\"");
                        foreach (var r in _itemsData)
                            sw.WriteLine(
                                $"\"{r.ItemId}\"," +
                                $"\"{r.ItemName}\"," +
                                $"\"{r.Quantity:N2}\"," +
                                $"\"{r.Total:N2}\"");
                    }
                    else
                    {
                        sw.WriteLine("\"رقم الحركة\",\"رقم الفاتورة\",\"النوع\"," +
                                     "\"التاريخ\",\"الوقت\",\"نقدي\",\"شبكة\"," +
                                     "\"الإجمالي\",\"الضريبة\",\"الخصم\",\"الصافي\"");
                        foreach (var r in _invoiceData)
                            sw.WriteLine(
                                $"\"{r.InvGlobalID}\"," +
                                $"\"{r.id}\"," +
                                $"\"{r.ProcTypeName}\"," +
                                $"\"{r.InvDate}\"," +
                                $"\"{r.InvTime}\"," +
                                $"\"{r.Cash:N2}\"," +
                                $"\"{r.Visa:N2}\"," +
                                $"\"{r.InvTotal:N2}\"," +
                                $"\"{r.Tax:N2}\"," +
                                $"\"{r.Minus:N2}\"," +
                                $"\"{r.TotNet:N2}\"");
                    }
                }

                DXMessageBox.Show($"تم حفظ الملف:\n{filePath}",
                    "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(filePath)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير:\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Print Settings

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count != 1) return;
                try
                {
                    DataRow row = dt.Rows[0];
                    if (int.TryParse(row["printType"].ToString(), out int pt))
                        PrintType = pt;
                    PrintFooter = row["PrintFooter"] != DBNull.Value &&
                                  Convert.ToBoolean(row["PrintFooter"]);
                    PrintHeader = row["PrintHeader"] != DBNull.Value &&
                                  Convert.ToBoolean(row["PrintHeader"]);
                    PrintStamp  = row["PrintStamp"]  != DBNull.Value &&
                                  Convert.ToBoolean(row["PrintStamp"]);
                    defPrinter  = row["CasherPrinter"].ToString();
                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();
                    if (int.TryParse(row["printNo"].ToString(), out int pn))
                        PrintNo = pn;
                }
                catch { }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل إعدادات الطباعة:\n" + ex.Message);
            }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            DateTimeFrom = BuildDateTime(date1.DateTime, time1.Text, "00:00:00");
            DateTimeTo   = BuildDateTime(date2.DateTime, time2.Text, "23:59:59");
            Clr();
            TabControl1.SelectedIndex = 0;
            CalcStock();
        }

        private void btnShowInvs_Click(object sender, RoutedEventArgs e)
        {
            DateTimeFrom = BuildDateTime(date1.DateTime, time1.Text, "00:00:00");
            DateTimeTo   = BuildDateTime(date2.DateTime, time2.Text, "23:59:59");
            Clr();
            TabControl1.SelectedIndex = 1;
            ShowResults();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnExport_Click(object sender, RoutedEventArgs e)
            => ExportToExcel();

        #endregion

        #region Helper

        private DateTime BuildDateTime(DateTime date, string timeText, string defaultTime)
        {
            try
            {
                string time = string.IsNullOrWhiteSpace(timeText)
                    ? defaultTime : timeText.Trim();
                return DateTime.Parse($"{date.ToShortDateString()} {time}");
            }
            catch { return date; }
        }

        #endregion
    }
}