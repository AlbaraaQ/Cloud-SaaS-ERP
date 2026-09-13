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
    public partial class frmRptItemsSalesDetails : ThemedWindow
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

        /// <summary>1=مبيعات، 2=مشتريات</summary>
        public int OperType;

        private ObservableCollection<ItemSalesRow> _gridData
            = new ObservableCollection<ItemSalesRow>();

        #endregion

        #region Constructor

        public frmRptItemsSalesDetails()
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
            OperType    = 1;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateTo.DateTime   = DateTime.Now;
            txtDateFrom.DateTime = DateTime.Now;

            GridControl1.ItemsSource = _gridData;
            LoadSafes();
            LoadGroups();
            LoadBranches();
            ApplyOperTypeSettings();
        }

        /// <summary>تطبيق إعدادات نوع العملية (مبيعات / مشتريات)</summary>
        private void ApplyOperTypeSettings()
        {
            if (OperType == 2)
            {
                this.Title                = "مشتريات الأصناف تجميعي";
                colDgvNetSales.Header     = "صافي الشراء";
                lblNetSalesTitle.Text     = "💵 إجمالي صافي الشراء";
                lblGridTitle.Text         = "📊 مشتريات الأصناف تجميعي";
                lblSidebarTitle.Text      = "🔧 خيارات البحث";
            }
            else
            {
                this.Title                = "مبيعات الأصناف تجميعي";
                colDgvNetSales.Header     = "صافي البيع";
                lblNetSalesTitle.Text     = "💵 إجمالي صافي البيع";
                lblGridTitle.Text         = "📊 مبيعات الأصناف تجميعي";
                lblSidebarTitle.Text      = "🔧 خيارات البحث";
            }
        }

        #endregion

        #region Load Lookups

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Safes WHERE IS_Deleted=0 ORDER BY id", conn);
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

        private void LoadBranches()
        {
            try
            {
                string branchCond = "";
                if (MainClass.BranchNo != -1)
                {
                    branchCond = string.Equals(Accounting.BranchCondition, " ",
                        StringComparison.Ordinal)
                        ? ""
                        : $" AND BranchId={MainClass.BranchNo}";
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0{branchCond}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";
                cmbBranches.ItemsSource       = dt.DefaultView;

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الفروع:\n" + ex.Message);
            }
        }

        #endregion

        #region ShowResults - الاستعلام الموحد

        private void ShowResults()
        {
            try
            {
                _gridData.Clear();

                // ─── بناء التواريخ مع الوقت ───
                DateTime dateTimeFrom = BuildDateTime(
                    txtDateFrom.DateTime, txtStartTime.Text, "00:00");
                DateTime dateTimeTo   = BuildDateTime(
                    txtDateTo.DateTime,   txtEndTime.Text,   "23:59");

                // ─── بناء شروط الفلتر ───
                string branchFilter = "";
                string itemFilter   = "";
                string whereClause  = "";

                if (!chkAllBranches.IsChecked == true && cmbBranches.SelectedValue != null)
                    branchFilter = $" inv.branch={cmbBranches.SelectedValue} AND ";

                if (chkAll.IsChecked != true)
                    itemFilter += $" Items.id={SelectedId} AND ";

                if (chkAllGrp.IsChecked != true && cmbGrp.SelectedValue != null)
                    itemFilter += $" Items.group_id={cmbGrp.SelectedValue} AND ";

                // شرط المستودع
                if (chlAllSafe.IsChecked != true && cmbSafes.SelectedValue != null)
                {
                    whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                    whereClause += $"Inv.safe={cmbSafes.SelectedValue}";
                }

                // شرط الفترة
                if (chkAllPeriod.IsChecked != true)
                {
                    whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                    whereClause += "Inv.date BETWEEN @date1 AND @date2";
                }

                // ─── الاستعلام الموحد (ShowResults1 النسخة المحسّنة) ───
                string sql = @"
                    SELECT Items.id, Items.name, Items.code, ItemsCategory.name AS CatName,
                           SUM(CASE WHEN inv.inv_type=2 AND inv.proc_type=1 THEN val ELSE 0 END) AS SaleVal,
                           SUM(CASE WHEN inv.inv_type=2 AND inv.proc_type=2 THEN val ELSE 0 END) AS RetSaleVal,
                           SUM(CASE WHEN inv.inv_type=1 AND inv.proc_type=1 THEN val ELSE 0 END) AS PurchVal,
                           SUM(CASE WHEN inv.inv_type=1 AND inv.proc_type=2 THEN val ELSE 0 END) AS RePurchVal,
                           SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=1 THEN val ELSE 0 END) AS PosVal,
                           SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=2 THEN val ELSE 0 END) AS PosRetVal,
                           SUM(CASE WHEN inv.inv_type=2 AND inv.proc_type=1 THEN val1*exchange_price ELSE 0 END) AS SumSale,
                           SUM(CASE WHEN inv.inv_type=2 AND inv.proc_type=2 THEN val1*exchange_price ELSE 0 END) AS SumRetSaleVal,
                           SUM(CASE WHEN inv.inv_type=1 AND inv.proc_type=1 THEN val1*exchange_price ELSE 0 END) AS SumPurchVal,
                           SUM(CASE WHEN inv.inv_type=1 AND inv.proc_type=2 THEN val1*exchange_price ELSE 0 END) AS SumRePurchVal,
                           SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=1 THEN val1*exchange_price ELSE 0 END) AS SumPosVal,
                           SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=2 THEN val1*exchange_price ELSE 0 END) AS SumPosRetVal
                    FROM inv
                    INNER JOIN inv_sub ON inv.InvGlobalID = inv_sub.InvGlobalID
                    INNER JOIN Items ON inv_sub.ItemId = Items.id
                    INNER JOIN ItemsCategory ON Items.group_id = ItemsCategory.id
                    WHERE inv.IS_Deleted=0 AND inv_sub.ProductId=0";

                // إضافة فلتر الفرع والصنف والمجموعة
                if (!string.IsNullOrEmpty(branchFilter))
                    sql += $" AND {branchFilter.TrimEnd(' ', 'A', 'N', 'D')}";
                if (!string.IsNullOrEmpty(itemFilter))
                    sql += $" AND {itemFilter.TrimEnd(' ', 'A', 'N', 'D')}";
                if (!string.IsNullOrEmpty(whereClause))
                    sql += $" AND {whereClause}";

                sql += " GROUP BY Items.id, Items.name, Items.code, ItemsCategory.name";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = dateTimeFrom;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dateTimeTo;

                var dt = new DataTable();
                adapter.Fill(dt);

                ProgressBar1.Maximum = Math.Max(dt.Rows.Count, 1);
                ProgressBar1.Value   = 0;

                int rowNum = 1;
                foreach (DataRow row in dt.Rows)
                {
                    ProgressBar1.Value = rowNum;

                    double saleVal    = ParseDouble(row["SaleVal"]);
                    double retSaleVal = ParseDouble(row["RetSaleVal"]);
                    double purchVal   = ParseDouble(row["PurchVal"]);
                    double rePurchVal = ParseDouble(row["RePurchVal"]);
                    double posVal     = ParseDouble(row["PosVal"]);
                    double posRetVal  = ParseDouble(row["PosRetVal"]);
                    double sumSale    = ParseDouble(row["SumSale"]);
                    double sumRetSale = ParseDouble(row["SumRetSaleVal"]);
                    double sumPurch   = ParseDouble(row["SumPurchVal"]);
                    double sumRePurch = ParseDouble(row["SumRePurchVal"]);
                    double sumPos     = ParseDouble(row["SumPosVal"]);
                    double sumPosRet  = ParseDouble(row["SumPosRetVal"]);

                    // لا حركة → تجاهل
                    bool hasMovement = (saleVal != 0 || retSaleVal != 0 ||
                                        posVal  != 0 || posRetVal  != 0);
                    if (!hasMovement) continue;

                    double netQty;
                    double netValue;

                    if (OperType == 1) // مبيعات
                    {
                        netQty   = saleVal - retSaleVal + posVal - posRetVal;
                        netValue = sumSale + sumPos - sumRetSale - sumPosRet;
                    }
                    else // مشتريات
                    {
                        netQty   = purchVal - rePurchVal;
                        netValue = sumPurch - sumRePurch;
                    }

                    _gridData.Add(new ItemSalesRow
                    {
                        DgvNo       = rowNum++,
                        ItemId      = Convert.ToInt32(row["id"]),
                        DgvItemCode = row["code"].ToString(),
                        DgvItem     = row["name"].ToString(),
                        DgvQty      = netQty,
                        DgvNetSales = Math.Round(netValue, 2),
                        DgvSaleRatio= 0,
                        DgvCategory = row["CatName"].ToString(),
                    });

                    if (ProgressBar1.Value < ProgressBar1.Maximum)
                        ProgressBar1.Value++;
                }

                UpdateSummary();
                ProgressBar1.Value = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض النتائج:\n" + ex.Message);
            }
        }

        private DateTime BuildDateTime(DateTime date, string timeText, string defaultTime)
        {
            try
            {
                string time = string.IsNullOrWhiteSpace(timeText) ? defaultTime : timeText.Trim();
                return DateTime.Parse($"{date.ToShortDateString()} {time}");
            }
            catch
            {
                return date;
            }
        }

        private double ParseDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0.0;
            return double.TryParse(val.ToString(), out double d) ? d : 0.0;
        }

        private void UpdateSummary()
        {
            lblCountItems.Text  = _gridData.Count.ToString();
            lblSumNetSales.Text = _gridData.Sum(r => r.DgvNetSales).ToString("N2");
            lblSumQty.Text      = _gridData.Sum(r => r.DgvQty).ToString("N2");
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
                    OpenItemSearch();
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
                    OpenItemSearch();
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

        private void ShowItemCard()
        {
            if (GridControl1.SelectedItem is ItemSalesRow row)
            {
                if (User.EditItemInfo)
                {
                    var dlg = new frmItems();
                    MainClass.ApplyPermissionToForm(dlg);
                    MainClass.DoApplyUserSett(dlg);
                    dlg.ItemId = row.ItemId;
                    dlg.ShowDialog();
                }
                else
                {
                    DXMessageBox.Show("لا يوجد لديك صلاحية لهذه العملية",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
                    Total         = row.DgvNetSales.ToString("N2"),
                    Ratio         = row.DgvSaleRatio.ToString("N2"),
                    CategoryName  = row.DgvCategory,
                    FromDate      = chkAllPeriod.IsChecked != true
                                    ? txtDateFrom.DateTime.ToShortDateString() : "",
                    ToDate        = chkAllPeriod.IsChecked != true
                                    ? txtDateTo.DateTime.ToShortDateString() : "",
                    InventoryType = this.Title,
                    Sum           = lblSumNetSales.Text,
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
            RptName    = OperType == 2
                         ? "RptItemsPurchDetails.repx"
                         : "RptItemsSalesDetails.repx";

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

            try
            {
                var report = XtraReport.FromFile(fullRptPath);
                report.DataSource = BuildReportDataSet();

                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var hCtrl = report.FindControl("headerRpt", true) as XRSubreport;
                    if (hCtrl != null) hCtrl.ReportSource = headerRpt;
                }

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
                if (_gridData.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ItemsSales_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("\"م\",\"رمز الصنف\",\"الصنف\",\"الكمية\",\"صافي البيع\",\"الفئة\"");
                    foreach (var r in _gridData)
                        sw.WriteLine(
                            $"\"{r.DgvNo}\"," +
                            $"\"{r.DgvItemCode}\"," +
                            $"\"{r.DgvItem}\"," +
                            $"\"{r.DgvQty:N2}\"," +
                            $"\"{r.DgvNetSales:N2}\"," +
                            $"\"{r.DgvCategory}\"");
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

            if (chkAll.IsChecked != true && string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                DXMessageBox.Show("يجب اختيار الصنف",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                txtItemName.Focus();
                return;
            }

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
            if (sender is Button btn && btn.Tag != null &&
                int.TryParse(btn.Tag.ToString(), out int itemId))
            {
                var dlg = new frmItemsDetails();
                MainClass.ApplyPermissionToForm(dlg);
                MainClass.DoApplyUserSett(dlg);
                dlg.SelectedId = itemId;
                dlg.ShowDialog();
            }
        }

        private void MenuShowItem_Click(object sender, RoutedEventArgs e)
            => ShowItemCard();

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => ShowItemCard();

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

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }
        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAllPeriod.IsChecked == true;
            txtDateFrom.IsEnabled  = !isAll;
            txtDateTo.IsEnabled    = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled   = !isAll;
        }

        private void cmbBranches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق عند تغيير الفرع
        }

        #endregion

        #region TextBox Events

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemName.Text))
                SearchByName();
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemCode.Text))
                SearchByCode();
        }

        #endregion
    }
}