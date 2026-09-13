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
using System.Windows.Input;

using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptItemsProfit : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int    SelectedId;
        private bool   PrintHeader;
        private bool   PrintFooter;
        private bool   PrintStamp;
        private int    PrintType;
        private int    PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<ItemProfitRow> _gridData
            = new ObservableCollection<ItemProfitRow>();

        #endregion

        #region Constructor

        public frmRptItemsProfit()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            conn1       = MainClass.ConnObj();
            SelectedId  = -1;
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
            txtDateTo.DateTime   = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now.AddMonths(-1);
            GridControl1.ItemsSource = _gridData;
            LoadSafes();
            LoadGroups();
        }

        #endregion

        #region Load Lookups

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Safes " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND status=1 AND IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSafes.DisplayMemberPath = "name";
                cmbSafes.SelectedValuePath = "id";
                cmbSafes.ItemsSource       = dt.DefaultView;
                cmbSafes.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات:\n" + ex.Message);
            }
        }

        public void LoadGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM ItemsCategory WHERE type=2 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbGrp.DisplayMemberPath = "name";
                cmbGrp.SelectedValuePath = "id";
                cmbGrp.ItemsSource       = dt.DefaultView;
                cmbGrp.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات:\n" + ex.Message);
            }
        }

        #endregion

        #region ShowResults

        private void ShowResults()
        {
            try
            {
                _gridData.Clear();
                GridControl1.ItemsSource = _gridData;

                // ─── بناء شروط الفلتر ───
                string itemFilter   = "";
                string branchFilter = "";
                string safeFilter   = "";

                if (chkAll.IsChecked != true)
                    itemFilter = $" Items.id={SelectedId} AND ";

                if (chkAllGrp.IsChecked != true && cmbGrp.SelectedValue != null)
                    itemFilter += $" Items.group_id={cmbGrp.SelectedValue} AND ";

                string itemsSql =
                    $"SELECT Items.id, Items.name, Items.code " +
                    $"FROM Items, Inv_Sub " +
                    $"WHERE {itemFilter} Items.Id = Inv_Sub.ItemId " +
                    $"AND Items.IS_Deleted=0 " +
                    $"GROUP BY Inv_Sub.ItemId, Items.Id, Items.name, Items.code";

                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                if (chlAllSafe.IsChecked != true && cmbSafes.SelectedValue != null)
                    safeFilter = $" AND Inv.safe={cmbSafes.SelectedValue}";

                string dateFilter = $" AND Inv.date BETWEEN @date1 AND @date2 " +
                                    $" AND Inv_Sub.ItemId > 0";

                string fullFilter = safeFilter + dateFilter;

                var adapterItems = new SqlDataAdapter(itemsSql, conn);
                var dtItems      = new DataTable();
                adapterItems.Fill(dtItems);

                ProgressBar1.Maximum = dtItems.Rows.Count;
                ProgressBar1.Value   = 0;

                DateTime dateFrom   = txtFromDate.DateTime;
                DateTime dateTo     = txtDateTo.DateTime.AddHours(24);
                int      rowCounter = 1;

                double grandTotalCost    = 0.0;
                double grandTotalNetSale = 0.0;
                double grandTotalProfit  = 0.0;

                for (int i = 0; i < dtItems.Rows.Count; i++)
                {
                    ProgressBar1.Value = i + 1;
                    System.Windows.Application.Current.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => { }));

                    DataRow itemRow = dtItems.Rows[i];
                    int     itemId  = Convert.ToInt32(itemRow["id"]);

                    // مبيعات عادية (inv_type=2, proc_type=1)
                    var r1 = GetSaleData(itemId, 2, 1, branchFilter, fullFilter, dateFrom, dateTo);
                    double salesQty      = r1.qty;
                    double salesTotal    = r1.itemTotal;
                    double salesDiscount = r1.discount;
                    double salesInvDisc  = r1.invDiscount;
                    double salesAvgCost  = r1.avgCost;
                    double salesTotalCost= r1.totalCost;

                    // مرتجع مبيعات (inv_type=2, proc_type=2)
                    var r2 = GetSaleData(itemId, 2, 2, branchFilter, fullFilter, dateFrom, dateTo);
                    salesQty       -= r2.qty;
                    salesTotal     -= r2.itemTotal;
                    salesDiscount  -= r2.discount;
                    salesInvDisc   -= r2.invDiscount;
                    salesAvgCost   -= r2.avgCost;
                    salesTotalCost -= r2.totalCost;

                    // نقطة بيع (inv_type=3, proc_type=1)
                    var r3 = GetSaleData(itemId, 3, 1, branchFilter, fullFilter, dateFrom, dateTo);
                    salesQty       += r3.qty;
                    salesTotal     += r3.itemTotal;
                    salesDiscount  += r3.discount * 2; // كما في الكود الأصلي
                    salesInvDisc   += r3.invDiscount;
                    salesAvgCost   += r3.avgCost;
                    salesTotalCost += r3.totalCost;

                    // مرتجع نقطة بيع (inv_type=3, proc_type=2)
                    var r4 = GetSaleData(itemId, 3, 2, branchFilter, fullFilter, dateFrom, dateTo);
                    salesQty       -= r4.qty;
                    salesTotal     -= r4.itemTotal;
                    salesDiscount  -= r4.discount;
                    salesInvDisc   -= r4.invDiscount;
                    salesAvgCost   -= r4.avgCost;
                    salesTotalCost -= r4.totalCost;

                    // تجاهل الصنف إذا لم تكن له حركة
                    bool hasMovement = (r1.qty != 0 || r2.qty != 0 ||
                                        r3.qty != 0 || r4.qty != 0);
                    if (!hasMovement) continue;

                    double netQty     = salesQty;
                    double netSale    = Math.Round(salesTotal - salesDiscount - salesInvDisc, 2);
                    double netProfit  = Math.Round(netSale - salesTotalCost, 2);

                    string profitRatio = "0%";
                    if (salesTotalCost > 0)
                    {
                        decimal ratio = new decimal(
                            Math.Round((netSale - salesTotalCost) / salesTotalCost * 100.0, 2));
                        profitRatio = ratio.ToString("N2") + "%";
                    }

                    _gridData.Add(new ItemProfitRow
                    {
                        DgvNo          = rowCounter++,
                        DgvItemCode    = itemRow["code"].ToString(),
                        DgvItem        = itemRow["name"].ToString(),
                        DgvQty         = netQty,
                        DgvAvgCost     = salesTotalCost,
                        DgvNetSale     = netSale,
                        DgvProfit      = netProfit,
                        DgvProfitRatio = profitRatio,
                        DgvItemNo      = itemId.ToString(),
                    });

                    grandTotalCost    += salesAvgCost;
                    grandTotalNetSale += netSale;
                    grandTotalProfit  += netProfit;
                }

                // تحديث بطاقات المجاميع
                lblCountItems.Text  = _gridData.Count.ToString();
                lblSumNetSale.Text  = grandTotalNetSale.ToString("N2");
                lblSumProfit.Text   = grandTotalProfit.ToString("N2");
                ProgressBar1.Value  = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض النتائج:\n" + ex.Message);
            }
        }

        // ─── Helper: جلب بيانات المبيعات لصنف واحد ───
        private (double qty, double itemTotal, double discount,
                 double invDiscount, double avgCost, double totalCost)
            GetSaleData(int itemId, int invType, int procType,
                        string branchFilter, string extraFilter,
                        DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                string sql =
                    $"SELECT SUM(val) AS Qnty, " +
                    $"SUM(val1 * ItemPriceWithoutVAT) AS ItemTotal, " +
                    $"SUM(discount) AS Discount, " +
                    $"SUM((((val1*ItemPriceWithoutVAT)-discount)/" +
                    $"(NULLIF(InvTotal,0)+minus))*minus) AS InvDiscount, " +
                    $"SUM(AvrgCost) AS AvrgCost, " +
                    $"SUM(val1 * AvrgCost) AS TotalCost " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter} Inv_Sub.ItemId={itemId} " +
                    $"AND inv.inv_type={invType} AND inv.proc_type={procType} " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND inv_sub.ProductId=0 AND IS_Deleted=0 " +
                    $"{extraFilter}";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = dateFrom.Date;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dateTo.Date;

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0 ||
                    string.IsNullOrEmpty(dt.Rows[0]["Qnty"].ToString()) ||
                    dt.Rows[0]["Qnty"] == DBNull.Value)
                    return (0, 0, 0, 0, 0, 0);

                double.TryParse(dt.Rows[0]["Qnty"].ToString(),       out double qty);
                double.TryParse(dt.Rows[0]["ItemTotal"].ToString(),   out double itemTotal);
                double.TryParse(dt.Rows[0]["Discount"].ToString(),    out double discount);
                double.TryParse(dt.Rows[0]["InvDiscount"].ToString(), out double invDiscount);
                double.TryParse(dt.Rows[0]["AvrgCost"].ToString(),    out double avgCost);
                double.TryParse(dt.Rows[0]["TotalCost"].ToString(),   out double totalCost);

                return (qty, itemTotal, discount, invDiscount, avgCost, totalCost);
            }
            catch
            {
                return (0, 0, 0, 0, 0, 0);
            }
        }

        #endregion

        #region Item Search

        private void SearchByName()
        {
            try
            {
                string name = txtItemName.Text.Trim();
                if (string.IsNullOrEmpty(name)) { OpenItemSearch(); return; }

                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{name}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId        = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text  = dt.Rows[0]["Code"].ToString();
                    txtItemName.Text  = dt.Rows[0]["name"].ToString();
                }
                else
                {
                    OpenItemSearch();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث:\n" + ex.Message);
            }
        }

        private void SearchByCode()
        {
            try
            {
                string code = txtItemCode.Text.Trim();
                if (string.IsNullOrEmpty(code)) return;

                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND Code=N'{code}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId        = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text  = dt.Rows[0]["Code"].ToString();
                    txtItemName.Text  = dt.Rows[0]["name"].ToString();
                }
                else
                {
                    OpenItemSearch();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث:\n" + ex.Message);
            }
        }

        private void OpenItemSearch()
        {
            var dlg = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.sql         = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
            dlg.search      = "SELECT id, name, nameEN, sale_price, unit FROM Items";
            dlg.Itemname    = "";
            dlg.txtSrchNm.Text = txtItemName.Text;
            dlg.ShowDialog();

            if (dlg.ISDone && dlg.ItemId > 0)
            {
                SelectedId        = dlg.ItemId;
                txtItemCode.Text  = Common.GetItemCode(SelectedId);
                txtItemName.Text  = dlg.Itemname;
            }
        }

        #endregion

        #region Print Settings

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

        #region Print / Preview / Export

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _gridData)
            {
                list.Add(new InventoryData
                {
                    ItemCode      = row.DgvItemCode,
                    ItemName      = row.DgvItem,
                    Quantity      = row.DgvQty.ToString("N2"),
                    AvgCostPrice  = row.DgvAvgCost.ToString("N2"),
                    NetCost       = row.DgvNetSale.ToString("N2"),
                    NetProfit     = row.DgvProfit.ToString("N2"),
                    Profit_Comm   = row.DgvProfitRatio,
                    InventoryType = this.Title,
                    User          = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate     = DateTime.Now.ToShortDateString(),
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
            RptName    = "rptItemsProfit.repx";

            if (_gridData.Count == 0)
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

            string fullRptPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullRptPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullRptPath);
                report.DataSource = BuildReportDataSet();

                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var headerCtrl = report.FindControl("headerRpt", ignoreCase: true) as XRSubreport;
                    if (headerCtrl != null)
                        headerCtrl.ReportSource = headerRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;

                if (printMode == 1)
                {
                    for (int copies = 1; copies <= PrintNo; copies++)
                        report.Print();
                }
                else
                {
                    report.ShowPreviewDialog();
                }

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
                if (_gridData.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ItemsProfit_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("\"م\",\"رمز المادة\",\"المادة\",\"الكمية\"," +
                                 "\"متوسط التكلفة\",\"صافي البيع\",\"الربح\",\"نسبة الربح\"");

                    foreach (var r in _gridData)
                    {
                        sw.WriteLine(
                            $"\"{r.DgvNo}\"," +
                            $"\"{r.DgvItemCode}\"," +
                            $"\"{r.DgvItem}\"," +
                            $"\"{r.DgvQty:N2}\"," +
                            $"\"{r.DgvAvgCost:N2}\"," +
                            $"\"{r.DgvNetSale:N2}\"," +
                            $"\"{r.DgvProfit:N2}\"," +
                            $"\"{r.DgvProfitRatio}\"");
                    }
                }

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

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chlAllSafe.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                cmbSafes.Focus();
                return;
            }

            if (chkAll.IsChecked != true &&
                string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                DXMessageBox.Show("يجب اختيار الصنف",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                txtItemName.Focus();
                return;
            }

            ShowResults();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chlAllSafe.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                cmbSafes.Focus();
                return;
            }
            chkAll.IsChecked = false;
            OpenItemSearch();
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (int.TryParse(btn.Tag.ToString(), out int itemId))
                {
                    var dlg = new frmItemsDetails();
                    MainClass.ApplyPermissionToForm(dlg);
                    MainClass.DoApplyUserSett(dlg);
                    dlg.SelectedId = itemId;
                    dlg.ShowDialog();
                }
            }
        }

        #endregion

        #region CheckBox Events

        private void chlAllSafe_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSafes.IsEnabled = chlAllSafe.IsChecked != true;
        }

        private void chkAllGrp_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbGrp.IsEnabled = chkAllGrp.IsChecked != true;
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAll.IsChecked == true;
            txtItemName.IsEnabled = !isAll;
            txtItemCode.IsEnabled = !isAll;
            if (isAll)
            {
                txtItemName.Text = "";
                txtItemCode.Text = "";
                SelectedId       = -1;
            }
        }

        #endregion

        #region TextBox Events

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtItemName.Text))
                SearchByName();
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtItemCode.Text))
                SearchByCode();
        }

        #endregion
    }
}