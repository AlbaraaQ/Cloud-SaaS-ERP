using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvBySalesMen : ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────────────

        private readonly SqlConnection _conn;

        private string _empName = string.Empty;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType = 0;
        private int _printNo = 1;
        private string _defPrinter = string.Empty;
        private string _rptName = string.Empty;
        private string _rptUrl = string.Empty;
        private double _defaultVAT = 0.0;
        private bool _priceIncVAT = false;

        private ObservableCollection<SalesManRowItem> _itemsList;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmInvBySalesMen()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            _itemsList = new ObservableCollection<SalesManRowItem>();
        }

        #endregion

        #region ── Window Loaded ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;

            LoadSalesmen();
            LoadPrintSettings();

            dgvItems.ItemsSource = _itemsList;
        }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        private void LoadSalesmen()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbEmps.DisplayMemberPath = "name";
                cmbEmps.SelectedValuePath = "id";
                cmbEmps.ItemsSource = table.DefaultView;
                cmbEmps.SelectedIndex = -1;
            }
            catch (Exception ex) { ShowError("خطأ في تحميل المندوبين", ex); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                // إعدادات الطباعة
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=14", _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count == 1)
                {
                    try
                    {
                        _printType = SafeInt(table.Rows[0]["printType"]);
                        _printFooter = Convert.ToBoolean(table.Rows[0]["PrintFooter"]);
                        _printHeader = Convert.ToBoolean(table.Rows[0]["PrintHeader"]);
                        _printStamp = Convert.ToBoolean(table.Rows[0]["PrintStamp"]);
                        _defPrinter = table.Rows[0]["CasherPrinter"]?.ToString() ?? string.Empty;
                        _printNo = SafeInt(table.Rows[0]["printNo"], 1);
                        _rptName = table.Rows[0]["RptName"]?.ToString() ?? string.Empty;
                        _rptUrl = table.Rows[0]["RptUrl"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                    }
                    catch { /* تجاهل */ }
                }

                // إعدادات عامة (ضريبة)
                var adapter2 = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=2", _conn);
                var table2 = new DataTable();
                adapter2.Fill(table2);

                if (table2.Rows.Count == 1)
                {
                    try
                    {
                        _priceIncVAT = Convert.ToBoolean(table2.Rows[0]["PriceIncVAT"]);
                        _defaultVAT = SafeDouble(table2.Rows[0]["MainVAT"]);
                    }
                    catch { /* تجاهل */ }
                }
            }
            catch (Exception ex) { ShowError("خطأ في تحميل الإعدادات", ex); }
        }

        #endregion

        #region ── Show Results ────────────────────────────────────────────

        private async void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (!chkAll.IsChecked == true && cmbEmps.SelectedValue == null)
            {
                DXMessageBox.Show("اختر مندوبًا",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                cmbEmps.Focus();
                return;
            }

            btnShow.IsEnabled = false;

            try
            {
                _itemsList.Clear();
                ProgressBar1.Value = 0;
                ProgressBar1.Maximum = 100;

                await Task.Run(() =>
                {
                    ShowInvoiceResults();
                    LoadClientReceiptVouchers();
                    LoadGeneralReceiptVouchers();
                });

                RecalculateSummary();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في عرض النتائج", ex);
            }
            finally
            {
                btnShow.IsEnabled = true;
            }
        }

        private void ShowInvoiceResults()
        {
            try
            {
                string empFilter = string.Empty;
                string periodFilter = string.Empty;
                string branchFilter = string.Empty;

                if (chkAll.IsChecked != true && cmbEmps.SelectedValue != null)
                    empFilter = $" AND Inv.salesman = {cmbEmps.SelectedValue}";

                if (chkAllPeriod.IsChecked != true)
                    periodFilter = " AND date>=@date1 AND date<=@date2 ";

                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                string sql = $@"
                    SELECT * FROM Inv
                    WHERE {branchFilter}
                          Inv.salesman > 0
                      AND Inv.IS_Deleted = 0
                      AND (Inv.inv_type=2 OR Inv.inv_type=3)
                      AND (proc_type=1 OR proc_type=2)
                          {empFilter}
                          {periodFilter}
                    ORDER BY date";

                var adapter = new SqlDataAdapter(sql, _conn);

                if (!string.IsNullOrEmpty(periodFilter))
                {
                    DateTime fromDate = Dispatcher.Invoke(()
                        => txtDateFrom.SelectedDate ?? DateTime.Today);
                    DateTime toDate = Dispatcher.Invoke(()
                        => txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);

                    adapter.SelectCommand.Parameters
                        .Add("@date1", SqlDbType.DateTime).Value = fromDate;
                    adapter.SelectCommand.Parameters
                        .Add("@date2", SqlDbType.DateTime).Value = toDate;
                }

                var mainTable = new DataTable();
                adapter.Fill(mainTable);

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Maximum = mainTable.Rows.Count;
                    ProgressBar1.Value = 0;
                });

                _empName = chkAll.IsChecked == true
                    ? "الكل"
                    : Dispatcher.Invoke(() => cmbEmps.Text);

                foreach (DataRow row in mainTable.Rows)
                {
                    ProcessInvoiceRow(row);
                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError("خطأ في تحميل الفواتير", ex));
            }
        }

        private void ProcessInvoiceRow(DataRow row)
        {
            try
            {
                // تفاصيل بنود الفاتورة
                string globalId = row["InvGlobalID"]?.ToString() ?? string.Empty;

                var subAdapter = new SqlDataAdapter($@"
                    SELECT SUM(Inv_Sub.val1 * Inv_Sub.exchange_price) AS total,
                           SUM(Inv_Sub.discount)                      AS ItDiscount,
                           SUM(Inv_Sub.taxval)                        AS TaxVal,
                           SUM(Inv_Sub.val * Inv_Sub.AvrgCost)        AS AvrgCost
                    FROM   Inv_Sub
                    WHERE  Inv_Sub.InvGlobalID = N'{globalId}'",
                    _conn);

                var subTable = new DataTable();
                subAdapter.Fill(subTable);

                if (subTable.Rows.Count == 0) return;

                DataRow sub = subTable.Rows[0];
                double avgCost = SafeDouble(sub["AvrgCost"]);
                double taxVal = SafeDouble(sub["TaxVal"]);
                double netVal = SafeDouble(sub["total"]);
                double discount = SafeDouble(sub["ItDiscount"]);

                if (_priceIncVAT) netVal -= taxVal;

                // نوع الحركة
                string processType = string.Empty;
                int isPlus = 1;

                double invType = SafeDouble(row["inv_type"]);
                double procType = SafeDouble(row["proc_type"]);

                if (invType == 2.0)
                {
                    if (procType == 1.0) { processType = "فاتورة بيع"; isPlus = 1; }
                    else { processType = "فاتورة مرتجع بيع"; isPlus = -1; }
                }
                else if (invType == 3.0)
                {
                    if (procType == 1.0) { processType = "فاتورة نقطة بيع"; isPlus = 1; }
                    else { processType = "فاتورة مرتجع نقطة بيع"; isPlus = -1; }
                }

                // بيانات المندوب
                var salAdapter = new SqlDataAdapter($@"
                    SELECT comm, name, Profit_Comm, Colle_Comm
                    FROM   salesmen WHERE id={row["salesman"]}",
                    _conn);

                var salTable = new DataTable();
                salAdapter.Fill(salTable);
                if (salTable.Rows.Count == 0) return;

                DataRow salRow = salTable.Rows[0];
                string salName = salRow["name"]?.ToString() ?? string.Empty;
                double commPct = SafeDouble(salRow["comm"]);
                double collCommPct = SafeDouble(salRow["Colle_Comm"]);
                double profitCommPct = SafeDouble(salRow["Profit_Comm"]);

                double minus = SafeDouble(row["minus"]);
                double netForComm = netVal - minus - discount;
                double profitBase = Math.Round(netForComm - avgCost, 2);

                double salesComm = Math.Round((commPct / 100.0) * netForComm, 2);
                double collComm = 0.0;
                double profitComm = 0.0;

                // عمولة التحصيل فقط إذا كان نوع الدفع موجودًا
                if (!Equals(row["pay_type"], DBNull.Value) &&
                    SafeInt(row["pay_type"]) != -1)
                    collComm = Math.Round((collCommPct / 100.0) * netForComm, 2);

                if (profitBase > 0.0)
                    profitComm = Math.Round((profitCommPct / 100.0) * profitBase, 2);

                var item = new SalesManRowItem
                {
                    ProcessType = processType,
                    InvDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                    InvoiceNo = SafeInt(row["id"]),
                    RefNo = SafeInt(row["Reff_No"]),
                    SalesManName = salName,
                    Value = Math.Round(netForComm, 2),
                    SalesComm = salesComm,
                    CollectionComm = collComm,
                    ProfitComm = profitComm,
                    IsPlus = isPlus
                };

                Dispatcher.Invoke(() => _itemsList.Add(item));

                // تحميل إشعارات المدين إذا كانت فاتورة بيع
                if (procType == 1.0)
                    LoadCreditNotes(SafeInt(row["id"]), SafeInt(row["salesman"]));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError("خطأ في معالجة صف الفاتورة", ex));
            }
        }

        private void LoadCreditNotes(int invoiceId, int salesmanId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Notes WHERE Doc_Type=2 AND Inv_No={invoiceId}",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count == 0) return;

                var salAdapter = new SqlDataAdapter(
                    $"SELECT comm, name, Profit_Comm, Colle_Comm FROM salesmen WHERE id={salesmanId}",
                    _conn);
                var salTable = new DataTable();
                salAdapter.Fill(salTable);
                if (salTable.Rows.Count == 0) return;

                DataRow salRow = salTable.Rows[0];
                string salName = salRow["name"]?.ToString() ?? string.Empty;
                double commPct = SafeDouble(salRow["comm"]);
                double collCommPct = SafeDouble(salRow["Colle_Comm"]);

                foreach (DataRow row in table.Rows)
                {
                    double discountVal = SafeDouble(row["Discount"]);
                    double salesComm = Math.Round((commPct / 100.0) * discountVal, 2);
                    double collComm = Math.Round((collCommPct / 100.0) * discountVal, 2);

                    var item = new SalesManRowItem
                    {
                        ProcessType = "إشعار مدين",
                        InvDate = Convert.ToDateTime(row["Date"]).ToShortDateString(),
                        InvoiceNo = SafeInt(row["Doc_No"]),
                        RefNo = SafeInt(row["Inv_No"]),
                        SalesManName = salName,
                        Value = Math.Round(discountVal, 2),
                        SalesComm = salesComm,
                        CollectionComm = collComm,
                        ProfitComm = 0.0,
                        IsPlus = -1
                    };

                    Dispatcher.Invoke(() => _itemsList.Add(item));
                }
            }
            catch { /* تجاهل */ }
        }

        private void LoadClientReceiptVouchers()
        {
            LoadReceiptsByType(5, "سند قبض عميل");
        }

        private void LoadGeneralReceiptVouchers()
        {
            LoadReceiptsByType(7, "سند قبض");
        }

        private void LoadReceiptsByType(int receiptType, string label)
        {
            try
            {
                string empFilter = string.Empty;

                if (chkAll.IsChecked != true && cmbEmps.SelectedValue != null)
                    empFilter = $" AND SalesManID={cmbEmps.SelectedValue}";

                DateTime fromDate = Dispatcher.Invoke(()
                    => txtDateFrom.SelectedDate ?? DateTime.Today);
                DateTime toDate = Dispatcher.Invoke(()
                    => txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);

                var adapter = new SqlDataAdapter($@"
                    SELECT * FROM Receipts
                    WHERE  SalesManID <> -1
                      AND  ReceiptDate >= @date1
                      AND  ReceiptDate <= @date2
                      AND  ISDeleted = 0
                      AND  ReceiptType = {receiptType}
                           {empFilter}",
                    _conn);

                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDate;
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate;

                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    var salAdapter = new SqlDataAdapter(
                        $"SELECT comm, name, Profit_Comm, Colle_Comm FROM salesmen WHERE id={row["SalesManID"]}",
                        _conn);
                    var salTable = new DataTable();
                    salAdapter.Fill(salTable);
                    if (salTable.Rows.Count == 0) continue;

                    DataRow salRow = salTable.Rows[0];
                    string salName = salRow["name"]?.ToString() ?? string.Empty;
                    double collCommPct = SafeDouble(salRow["Colle_Comm"]);

                    double netVal = SafeDouble(row["NetVal"]);
                    double baseVal = Math.Round(netVal * 100.0 / 115.0, 2);
                    double collComm = Math.Round((collCommPct / 100.0) * netVal, 2);

                    var item = new SalesManRowItem
                    {
                        ProcessType = label,
                        InvDate = Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString(),
                        InvoiceNo = SafeInt(row["ReceiptNo"]),
                        RefNo = 0,
                        SalesManName = salName,
                        Value = baseVal,
                        SalesComm = 0.0,
                        CollectionComm = collComm,
                        ProfitComm = 0.0,
                        IsPlus = 1
                    };

                    Dispatcher.Invoke(() => _itemsList.Add(item));
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region ── Summary Calculation ─────────────────────────────────────

        private void RecalculateSummary()
        {
            double sumVal = 0.0;
            double sumComm = 0.0;
            double sumColl = 0.0;
            double sumProfit = 0.0;

            foreach (var row in _itemsList)
            {
                sumVal += row.Value;

                if (row.IsPlus == 1)
                {
                    sumComm += row.SalesComm;
                    sumColl += row.CollectionComm;
                    sumProfit += row.ProfitComm;
                }
                else
                {
                    sumComm -= row.SalesComm;
                    sumColl -= row.CollectionComm;
                    sumProfit -= row.ProfitComm;
                }
            }

            txtSumVal.Text = sumVal.ToString("N2");
            txtSumComm.Text = sumComm.ToString("N2");
            txtSumColle_Comm.Text = sumColl.ToString("N2");
            txtSumProfit_Comm.Text = sumProfit.ToString("N2");
        }

        #endregion

        #region ── View Invoice (Grid Button) ───────────────────────────────

        private void ViewInvoiceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not SalesManRowItem row) return;

            try
            {
                switch (row.ProcessType)
                {
                    case "فاتورة بيع":
                        {
                            var frm = new frmSalesInvoice();
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=1 AND inv_type=2 AND id={row.InvoiceNo}");
                            break;
                        }
                    case "فاتورة مرتجع بيع":
                        {
                            var frm = new frmSalesInvoice();
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=2 AND inv_type=2 AND id={row.InvoiceNo}");
                            break;
                        }
                    case "سند قبض عميل":
                        {
                            var frm = new frmSandQ();
                            frm.Show();
                            frm.Navigate(
                                $"SELECT * FROM Receipts WHERE ISDeleted=0 AND ReceiptType=5 AND ReceiptNo={row.InvoiceNo}");
                            break;
                        }
                    case "سند قبض":
                        {
                            var frm = new frmSandQD();
                            frm.Show();
                            frm.Navigate(
                                $"SELECT * FROM Receipts WHERE ISDeleted=0 AND ReceiptType=7 AND ReceiptNo={row.InvoiceNo}");
                            break;
                        }
                    case "إشعار مدين":
                        {
                            var frm = new frmDebtNote();
                            frm.Show();
                            frm.Navigate(
                                $"SELECT * FROM Notes WHERE IS_Deleted=0 AND Doc_Type=2 AND Doc_No={row.InvoiceNo}");
                            break;
                        }
                }
            }
            catch (Exception ex) { ShowError("خطأ في فتح الفاتورة", ex); }
        }

        #endregion

        #region ── Print & Export ───────────────────────────────────────────

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void PrintReport(int type)
        {
            _rptUrl = MainClass.ReportsPath;
            _rptName = "rptInvBySalesMen.repx";
            _defPrinter = MainClass.ReportsPrinter;

            if (_itemsList.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }

            string reportPath = Path.Combine(_rptUrl, _rptName);
            if (!File.Exists(reportPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(reportPath);
                report.DataSource = BuildReportDataSet();

                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport = XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;
                    var sub = report.FindControl("headerRpt", true) as XRSubreport;
                    if (sub != null) sub.ReportSource = headerReport;
                }

                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerReport = XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;
                    var sub = report.FindControl("footerRpt", true) as XRSubreport;
                    if (sub != null) sub.ReportSource = footerReport;
                }

                if (!string.IsNullOrEmpty(_defPrinter))
                {
                    report.PrinterName = _defPrinter;
                    if (type == 1)
                        for (int i = 0; i < _printNo; i++) report.Print();
                    else
                        report.ShowPreviewDialog();
                }
                else
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات");

                report.Dispose();
            }
            catch (Exception ex) { ShowError("خطأ في الطباعة", ex); }
        }

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _itemsList)
            {
                list.Add(new InventoryData
                {
                    EmpName = _empName,
                    FromDate = txtDateFrom.SelectedDate?.ToShortDateString() ?? string.Empty,
                    ToDate = txtDateTo.SelectedDate?.ToShortDateString() ?? string.Empty,
                    ProcessType = row.ProcessType,
                    InvDate = row.InvDate,
                    InvoiceNo = row.InvoiceNo.ToString(),
                    RefsInvNo = row.RefNo.ToString(),
                    Supplier = row.SalesManName,
                    Price = row.Value.ToString("N2"),
                    comm = row.SalesComm.ToString("N2"),
                    Colle_Comm = row.CollectionComm.ToString("N2"),
                    Profit_Comm = row.ProfitComm.ToString("N2"),
                    InventoryType = this.Title,
                    Sum = txtSumComm.Text,
                    Total = txtSumColle_Comm.Text,
                    SumNetProfit = txtSumProfit_Comm.Text,
                    Total1 = txtSumVal.Text,
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_itemsList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"مبيعات_مندوب_{DateTime.Now:yyyyMMdd}"
                };

                if (saveDialog.ShowDialog() != true) return;

                string filePath = saveDialog.FileName;

                ExportToExcelClosedXml(_itemsList.ToList(), filePath);

                DXMessageBox.Show($"✅ تم حفظ الملف في:\n{filePath}",
                    "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(filePath)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التصدير", ex);
            }
        }

        /// <summary>
        /// تصدير القائمة إلى Excel باستخدام ClosedXML.
        /// </summary>
        private void ExportToExcelClosedXml(
            List<SalesManRowItem> items, string filePath)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("مبيعات المندوبين");

            // ═══ رؤوس الأعمدة ═══
            string[] headers =
            {
        "نوع الحركة", "التاريخ", "رقم السند",
        "رقم المرجع", "المندوب", "القيمة",
        "عمولة المبيعات", "عمولة التحصيل", "عمولة الربح"
    };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor =
                    ClosedXML.Excel.XLColor.FromArgb(33, 58, 122);
                cell.Style.Font.FontColor =
                    ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal =
                    ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            // ═══ البيانات ═══
            for (int i = 0; i < items.Count; i++)
            {
                SalesManRowItem row = items[i];
                int r = i + 2;

                worksheet.Cell(r, 1).Value = row.ProcessType;
                worksheet.Cell(r, 2).Value = row.InvDate;
                worksheet.Cell(r, 3).Value = row.InvoiceNo;
                worksheet.Cell(r, 4).Value = row.RefNo;
                worksheet.Cell(r, 5).Value = row.SalesManName;
                worksheet.Cell(r, 6).Value = row.Value;
                worksheet.Cell(r, 7).Value = row.SalesComm;
                worksheet.Cell(r, 8).Value = row.CollectionComm;
                worksheet.Cell(r, 9).Value = row.ProfitComm;

                // تنسيق الأرقام
                string numFormat = "#,##0.00";
                worksheet.Cell(r, 6).Style.NumberFormat.Format = numFormat;
                worksheet.Cell(r, 7).Style.NumberFormat.Format = numFormat;
                worksheet.Cell(r, 8).Style.NumberFormat.Format = numFormat;
                worksheet.Cell(r, 9).Style.NumberFormat.Format = numFormat;

                // تلوين الصفوف بالتناوب
                if (i % 2 == 0)
                {
                    worksheet.Row(r).Style.Fill.BackgroundColor =
                        ClosedXML.Excel.XLColor.FromArgb(248, 249, 253);
                }
            }

            // ═══ صف الإجماليات ═══
            int summaryRow = items.Count + 2;
            worksheet.Cell(summaryRow, 5).Value = "الإجمالي";
            worksheet.Cell(summaryRow, 5).Style.Font.Bold = true;

            worksheet.Cell(summaryRow, 6).Value =
                items.Sum(x => x.Value);
            worksheet.Cell(summaryRow, 7).Value =
                items.Where(x => x.IsPlus == 1).Sum(x => x.SalesComm)
                - items.Where(x => x.IsPlus == -1).Sum(x => x.SalesComm);
            worksheet.Cell(summaryRow, 8).Value =
                items.Where(x => x.IsPlus == 1).Sum(x => x.CollectionComm)
                - items.Where(x => x.IsPlus == -1).Sum(x => x.CollectionComm);
            worksheet.Cell(summaryRow, 9).Value =
                items.Where(x => x.IsPlus == 1).Sum(x => x.ProfitComm)
                - items.Where(x => x.IsPlus == -1).Sum(x => x.ProfitComm);

            // تنسيق صف الإجماليات
            for (int col = 6; col <= 9; col++)
            {
                var cell = worksheet.Cell(summaryRow, col);
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.DarkRed;
                cell.Style.NumberFormat.Format = "#,##0.00";
            }

            // ضبط عرض الأعمدة تلقائياً
            worksheet.Columns().AdjustToContents();

            // حفظ الملف
            workbook.SaveAs(filePath);
        }

        #endregion

        #region ── Control Events ───────────────────────────────────────────

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbEmps.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isChecked = chkAllPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isChecked;
            txtDateTo.IsEnabled = !isChecked;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        #endregion

        #region ── Helpers ──────────────────────────────────────────────────

        private static int SafeInt(object value, int defaultVal = 0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return int.TryParse(value.ToString(), out int r) ? r : defaultVal;
        }

        private static double SafeDouble(object value, double defaultVal = 0.0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return double.TryParse(value.ToString(), out double r) ? r : defaultVal;
        }

        private void ShowError(string title, Exception ex)
        {
            DXMessageBox.Show($"{title}\nتفاصيل الخطأ: {ex.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}