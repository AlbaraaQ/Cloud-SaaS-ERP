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

// UtilitiesProj: يحتوي على MainClass, Common, InventoryData, frmItemsSrch
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptItemsActivity : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int selectedItemId;
        public  int ItemId;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        // مصدر بيانات الجدول
        private ObservableCollection<ItemActivityRow> _gridData
            = new ObservableCollection<ItemActivityRow>();

        #endregion

        #region Constructor

        public frmRptItemsActivity()
        {
            InitializeComponent();

            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            selectedItemId = -1;
            ItemId         = -1;
            PrintHeader    = true;
            PrintFooter    = true;
            PrintStamp     = true;
            PrintNo        = 1;
            RptName        = "";
            RptUrl         = "";
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تهيئة التواريخ
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;

            // تحميل الإعدادات والبيانات
            LoadPrintSettings();
            LoadSafes();
            LoadGroups();
            LoadBranch();

            // ربط الجدول
            GridControl1.ItemsSource = _gridData;

            // إذا تم تمرير معرف صنف مباشرة
            if (ItemId > 0)
            {
                selectedItemId = ItemId;
                CalcStock();
            }
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            conn?.Close();
            conn1?.Close();
        }

        #endregion

        #region Load Lookups

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Safes " +
                    "WHERE status=1 AND IS_Deleted=0 ORDER BY id", conn);
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

        public void LoadBranch()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches " +
                    "WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranch.DisplayMemberPath = "name";
                cmbBranch.SelectedValuePath = "id";
                cmbBranch.ItemsSource       = dt.DefaultView;
                cmbBranch.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الفروع:\n" + ex.Message);
            }
        }

        public void LoadGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM ItemsCategory " +
                    "WHERE type=2 ORDER BY id", conn);
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

        #region CalcStock - حساب حركة الأصناف

        public void CalcStock()
        {
            try
            {
                // ─── بناء جملة الأصناف ───
                string itemsSql = "SELECT id, name, code FROM Items WHERE IS_Deleted=0";

                if (!chkAll.IsChecked == true)
                    itemsSql += $" AND id={selectedItemId}";

                if (chkAllGrp.IsChecked != true && cmbGrp.SelectedValue != null)
                    itemsSql += $" AND group_id={cmbGrp.SelectedValue}";

                itemsSql += " ORDER BY id";

                var adapterItems = new SqlDataAdapter(itemsSql, conn);
                var dtItems      = new DataTable();
                adapterItems.Fill(dtItems);

                // ─── بناء شروط إضافية ───
                string branchFilter = "";
                string extraFilter  = "";

                if (ChkAllBranch.IsChecked != true && cmbBranch.SelectedValue != null)
                    branchFilter = $"inv.branch={cmbBranch.SelectedValue} AND ";

                if (chlAllSafe.IsChecked != true && cmbSafes.SelectedValue != null)
                    extraFilter += $" AND Inv_sub.store={cmbSafes.SelectedValue}";

                if (ckTotalPeriod.IsChecked != true)
                    extraFilter += " AND Inv.date>=@date1 AND Inv.date<=@date2";

                if (chkAllClients.IsChecked != true && cmbClients.SelectedIndex > -1)
                {
                    // cmbClients هنا static items (نص) وليس id
                    // إذا كان مرتبطًا بـ DataSource يحتوي id، استخدم SelectedValue
                    // هنا نتجاهله لأن القائمة نصية في XAML
                }

                // ─── تهيئة الجدول ───
                _gridData.Clear();
                ProgressBar1.Maximum = dtItems.Rows.Count;
                ProgressBar1.Value   = 0;

                double grandTotalCost    = 0.0;
                double grandTotalBalance = 0.0;

                DateTime dateFrom = txtDateFrom.DateTime;
                DateTime dateTo   = txtDateTo.DateTime;

                for (int i = 0; i < dtItems.Rows.Count; i++)
                {
                    ProgressBar1.Value = i + 1;
                    // تحديث الـ UI أثناء المعالجة
                    System.Windows.Application.Current.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => { }));

                    DataRow itemRow   = dtItems.Rows[i];
                    int     itemDbId  = Convert.ToInt32(itemRow["id"]);
                    string  itemCode  = itemRow["code"].ToString();
                    string  itemName  = itemRow["name"].ToString();

                    // ─── جلب كل أنواع الحركات بـ query واحد ───
                    double firstVal   = GetSumVal(itemDbId, 9, 1, branchFilter, extraFilter, dateFrom, dateTo);
                    double purchVal   = GetSumVal(itemDbId, 1, 1, branchFilter, extraFilter, dateFrom, dateTo);
                    double rePurchVal = GetSumVal(itemDbId, 1, 2, branchFilter, extraFilter, dateFrom, dateTo);
                    double salesVal   = GetSumVal(itemDbId, 2, 1, branchFilter, extraFilter, dateFrom, dateTo);
                    double reSalesVal = GetSumVal(itemDbId, 2, 2, branchFilter, extraFilter, dateFrom, dateTo);
                    double posVal     = GetSumVal(itemDbId, 3, 1, branchFilter, extraFilter, dateFrom, dateTo);
                    double rePosVal   = GetSumVal(itemDbId, 3, 2, branchFilter, extraFilter, dateFrom, dateTo);
                    double transToVal = GetSumVal(itemDbId, 7, 1, branchFilter, extraFilter, dateFrom, dateTo, isProcOnSub: true);
                    double transFrVal = GetSumVal(itemDbId, 7, 2, branchFilter, extraFilter, dateFrom, dateTo, isProcOnSub: true);
                    double safeAdjVal = GetSumVal(itemDbId, 8, 1, branchFilter, extraFilter, dateFrom, dateTo, isProcOnInv: true);
                    double safeAdjRet = GetSumVal(itemDbId, 8, 2, branchFilter, extraFilter, dateFrom, dateTo, isProcOnInv: true);
                    double entryInv   = GetSumVal(itemDbId, 4, -1, branchFilter, extraFilter, dateFrom, dateTo);
                    double outInv     = GetSumVal(itemDbId, 5, -1, branchFilter, extraFilter, dateFrom, dateTo);

                    // حساب الرصيد
                    double balance = firstVal + purchVal - rePurchVal
                                   - salesVal + reSalesVal
                                   - posVal   + rePosVal
                                   + transToVal - transFrVal
                                   + safeAdjVal - safeAdjRet
                                   + entryInv   - outInv;

                    // تجاهل الأصناف بدون أي حركة
                    bool hasMovement = (firstVal   != 0 || purchVal  != 0 ||
                                        rePurchVal != 0 || salesVal  != 0 ||
                                        reSalesVal != 0 || posVal    != 0 ||
                                        rePosVal   != 0 || transToVal!= 0 ||
                                        transFrVal != 0 || safeAdjVal!= 0||
                                        safeAdjRet != 0 || entryInv  != 0 ||
                                        outInv     != 0);
                    if (!hasMovement) continue;

                    // ─── حساب متوسط التكلفة ───
                    double avgCost  = CalcAvgCost(itemDbId, branchFilter);
                    double totCost  = Math.Round(avgCost * balance, 2);

                    grandTotalBalance += Math.Round(balance, 2);
                    grandTotalCost    += totCost;

                    _gridData.Add(new ItemActivityRow
                    {
                        DgvItemCode   = itemCode,
                        DgvItemName   = itemName,
                        DgvBalnce     = Math.Round(balance,   2),
                        DgvAvgCost    = Math.Round(avgCost,   8),
                        DgvTotalCost  = totCost,
                        DgvFirstVal   = firstVal,
                        DgvPurch      = purchVal,
                        DgvRePurch    = rePurchVal,
                        DgvSales      = salesVal,
                        DgvReSales    = reSalesVal,
                        DgvPOS        = posVal,
                        DgvRePOS      = rePosVal,
                        DgvTransTo    = transToVal,
                        DgvTransFrom  = transFrVal,
                        DgvEntryInv   = entryInv,
                        DgvOutInv     = outInv,
                        DgvSafeAdj    = safeAdjVal,
                        DgvSafeAdjRet = safeAdjRet,
                    });
                }

                // تحديث بطاقات المجاميع
                lblSumBalance.Text   = grandTotalBalance.ToString("N2");
                lblSumTotalCost.Text = grandTotalCost.ToString("N2");
                lblCountItems.Text   = _gridData.Count.ToString();

                ProgressBar1.Value = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في حساب الحركة:\n" + ex.Message);
            }
        }

        // ─── Helper: جلب مجموع val بحسب inv_type و proc_type ───
        private double GetSumVal(
            int    itemId,
            int    invType,
            int    procType,        // -1 = بدون شرط proc_type
            string branchFilter,
            string extraFilter,
            DateTime dateFrom,
            DateTime dateTo,
            bool isProcOnSub = false,
            bool isProcOnInv = false)
        {
            try
            {
                string procField = isProcOnSub ? "Inv_sub.proc_type"
                                 : isProcOnInv ? "Inv.proc_type"
                                 :               "inv.proc_type";

                string procCond = procType >= 0
                    ? $" AND {procField}={procType}"
                    : "";

                string sql =
                    $"SELECT SUM(val) " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter} Inv_Sub.ItemId={itemId} " +
                    $"  AND inv.inv_type={invType}" +
                    $"  {procCond} " +
                    $"  AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"  AND IS_Deleted=0" +
                    $"  {extraFilter}";

                var adapter = new SqlDataAdapter(sql, conn);
                if (extraFilter.Contains("@date1"))
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value = dateFrom.Date;
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value = dateTo.Date;
                }

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 &&
                    dt.Rows[0][0] != DBNull.Value &&
                    !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                {
                    if (double.TryParse(dt.Rows[0][0].ToString(), out double result))
                        return result;
                }
            }
            catch { /* تجاهل الأخطاء الفردية */ }
            return 0.0;
        }

        // ─── Helper: حساب متوسط التكلفة ───
        private double CalcAvgCost(int itemId, string branchFilter)
        {
            try
            {
                // الوارد (proc_type=1)
                string sql1 =
                    $"SELECT SUM(val), SUM(val * exchange_price) " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter} ItemId={itemId} " +
                    $"  AND inv.proc_type=1 AND inv_sub.proc_type=1 " +
                    $"  AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"  AND IS_Deleted=0";

                var dt1 = new DataTable();
                new SqlDataAdapter(sql1, conn).Fill(dt1);

                double totalCostValue = 0.0;
                double totalQty       = 0.0;

                if (dt1.Rows.Count > 0 &&
                    dt1.Rows[0][1] != DBNull.Value &&
                    !string.IsNullOrEmpty(dt1.Rows[0][1].ToString()))
                {
                    double.TryParse(dt1.Rows[0][1].ToString(), out totalCostValue);
                    double.TryParse(dt1.Rows[0][0].ToString(), out totalQty);
                }

                // المنتج (proc_type=3)
                string sql2 =
                    $"SELECT SUM(val), SUM(val * exchange_price) " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter} ItemId={itemId} " +
                    $"  AND inv.proc_type=3 " +
                    $"  AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"  AND IS_Deleted=0";

                var dt2 = new DataTable();
                new SqlDataAdapter(sql2, conn).Fill(dt2);

                if (dt2.Rows.Count > 0 &&
                    dt2.Rows[0][1] != DBNull.Value &&
                    !string.IsNullOrEmpty(dt2.Rows[0][1].ToString()))
                {
                    if (double.TryParse(dt2.Rows[0][1].ToString(), out double c))
                        totalCostValue += c;
                    if (double.TryParse(dt2.Rows[0][0].ToString(), out double q))
                        totalQty += q;
                }

                if (totalQty != 0)
                    return Math.Floor(totalCostValue / totalQty * 100000000.0) / 100000000.0;

                // fallback: سعر آخر شراء
                string sqlLast =
                    $"SELECT purch_price FROM Currency_Lastprice_Sub " +
                    $"WHERE currency1={itemId}";
                var dtLast = new DataTable();
                new SqlDataAdapter(sqlLast, conn).Fill(dtLast);
                if (dtLast.Rows.Count > 0 &&
                    double.TryParse(dtLast.Rows[0][0].ToString(), out double lastPrice))
                    return lastPrice;
            }
            catch { }
            return 0.0;
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
                    selectedItemId    = Convert.ToInt32(dt.Rows[0]["id"]);
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
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
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
                    selectedItemId    = Convert.ToInt32(dt.Rows[0]["id"]);
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
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void OpenItemSearch()
        {
            var dlg = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.sql    = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
            dlg.search = "SELECT id, name, nameEN, sale_price, unit FROM Items";
            dlg.Itemname        = "";
            dlg.txtSrchNm.Text  = txtItemName.Text;
            dlg.ShowDialog();

            if (dlg.ISDone && dlg.ItemId > 0)
            {
                selectedItemId   = dlg.ItemId;
                txtItemCode.Text = Common.GetItemCode(selectedItemId);
                txtItemName.Text = dlg.Itemname;
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

                    defPrinter = row["CasherPrinter"].ToString();
                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();

                    if (int.TryParse(row["printNo"].ToString(), out int pn))
                        PrintNo = pn;

                    RptName = row["RptName"].ToString();
                    RptUrl  = row["RptUrl"].ToString();
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

        private void PrintDevexpress(int printMode)
        {
            RptUrl  = MainClass.ReportsPath;
            RptName = "rptItemsTotalGrd.repx";
            defPrinter = MainClass.ReportsPrinter;

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

                // Header subreport
                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var headerCtrl = report.FindControl("headerRpt", ignoreCase: true) as XRSubreport;
                    if (headerCtrl != null)
                        headerCtrl.ReportSource = headerRpt;
                }

                // Footer subreport
                string footerPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var footerCtrl = report.FindControl("footerRpt", ignoreCase: true) as XRSubreport;
                    if (footerCtrl != null)
                        footerCtrl.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;

                if (printMode == 1) // طباعة مباشرة
                {
                    for (int copies = 1; copies <= PrintNo; copies++)
                        report.Print();
                }
                else // معاينة
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

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _gridData)
            {
                var inv = new InventoryData
                {
                    MainItemCode  = txtItemCode.Text,
                    MainItemName  = txtItemName.Text,
                    Clint         = cmbClients.SelectedIndex > -1
                                    ? (cmbClients.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ""
                                    : "",
                    ItemCode      = row.DgvItemCode,
                    ItemName      = row.DgvItemName,
                    Quantity      = row.DgvBalnce.ToString("N2"),
                    AvgCostPrice  = row.DgvAvgCost.ToString("N8"),
                    TotCost       = row.DgvTotalCost.ToString("N2"),
                    FirstStock    = row.DgvFirstVal.ToString("N2"),
                    Purch         = row.DgvPurch.ToString("N2"),
                    ReturnPurch   = row.DgvRePurch.ToString("N2"),
                    Sale          = row.DgvSales.ToString("N2"),
                    ReturnSell    = row.DgvReSales.ToString("N2"),
                    Pos           = row.DgvPOS.ToString("N2"),
                    RePos         = row.DgvRePOS.ToString("N2"),
                    TransFrom     = row.DgvTransFrom.ToString("N2"),
                    TransTo       = row.DgvTransTo.ToString("N2"),
                    InputInv      = row.DgvEntryInv.ToString("N2"),
                    OutputInv     = row.DgvOutInv.ToString("N2"),
                    AdjustIn      = row.DgvSafeAdj.ToString("N2"),
                    AdjustOut     = row.DgvSafeAdjRet.ToString("N2"),
                    Status        = " ",
                    InventoryType = this.Title,
                    SumCost       = lblSumTotalCost.Text,
                    Sum           = lblSumBalance.Text,
                    User          = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate     = DateTime.Now.ToShortDateString(),
                };
                list.Add(inv);
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
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

                // بناء DataTable من _gridData وتصديره إلى Excel
                var dt = new DataTable("ItemsActivity");
                dt.Columns.Add("الرمز",          typeof(string));
                dt.Columns.Add("الصنف",          typeof(string));
                dt.Columns.Add("الرصيد",         typeof(double));
                dt.Columns.Add("متوسط التكلفة",  typeof(double));
                dt.Columns.Add("إجمالي التكلفة", typeof(double));
                dt.Columns.Add("أول مدة",        typeof(double));
                dt.Columns.Add("مشتريات",        typeof(double));
                dt.Columns.Add("مرتجع مشتريات", typeof(double));
                dt.Columns.Add("المبيعات",       typeof(double));
                dt.Columns.Add("مرتجع المبيعات",typeof(double));
                dt.Columns.Add("نقطة البيع",     typeof(double));
                dt.Columns.Add("مرتجع POS",      typeof(double));
                dt.Columns.Add("مناقلة مرسلة",   typeof(double));
                dt.Columns.Add("مناقلة مستلمة",  typeof(double));
                dt.Columns.Add("فاتورة إدخال",   typeof(double));
                dt.Columns.Add("فاتورة إخراج",   typeof(double));
                dt.Columns.Add("تسوية إدخال",    typeof(double));
                dt.Columns.Add("تسوية إخراج",    typeof(double));

                foreach (var r in _gridData)
                {
                    dt.Rows.Add(
                        r.DgvItemCode, r.DgvItemName,
                        r.DgvBalnce,   r.DgvAvgCost,  r.DgvTotalCost,
                        r.DgvFirstVal, r.DgvPurch,    r.DgvRePurch,
                        r.DgvSales,    r.DgvReSales,
                        r.DgvPOS,      r.DgvRePOS,
                        r.DgvTransTo,  r.DgvTransFrom,
                        r.DgvEntryInv, r.DgvOutInv,
                        r.DgvSafeAdj,  r.DgvSafeAdjRet);
                }

                // حفظ كـ CSV (بديل بسيط لـ Excel بدون مكتبات خارجية)
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ItemsActivity_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    // رأس الأعمدة
                    sw.WriteLine(string.Join(",",
                        dt.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\"")));
                    // البيانات
                    foreach (DataRow row in dt.Rows)
                        sw.WriteLine(string.Join(",",
                            row.ItemArray.Select(v => $"\"{v}\"")));
                }

                // فتح الملف
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

        #region Button Click Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked != true &&
                string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                DXMessageBox.Show("يجب اختيار الصنف",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                txtItemName.Focus();
                return;
            }
            CalcStock();
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

        private void btnSearchItem_Click(object sender, RoutedEventArgs e)
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

        #endregion

        #region TextBox KeyDown Events

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                string trimmed = txtItemName.Text.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    SearchByName();
            }
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                string trimmed = txtItemCode.Text.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    SearchByCode();
            }
        }

        #endregion

        #region CheckBox / RadioButton Events

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
                selectedItemId   = -1;
            }
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isAll;
            txtDateTo.IsEnabled   = !isAll;
        }

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

        private void ChkAllBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranch.IsEnabled = ChkAllBranch.IsChecked != true;
        }

        private void chkAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = false;
        }

        private void chkClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkClients.IsChecked == true)
            {
                LoadClients(1);
                cmbClients.IsEnabled = true;
            }
        }

        private void chkSuplier_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkSuplier.IsChecked == true)
            {
                LoadClients(2);
                cmbClients.IsEnabled = true;
            }
        }

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق عند تغيير الصف المحدد
        }

        #endregion

        #region Load Clients

        private void LoadClients(short clientType)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE (type={clientType} OR type=3) ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbClients.DisplayMemberPath = "name";
                    cmbClients.SelectedValuePath = "id";
                    cmbClients.ItemsSource       = dt.DefaultView;
                    cmbClients.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل العملاء:\n" + ex.Message);
            }
        }

        #endregion

        #region Helper: GetItemName (غير مستخدم خارجيًا - للمرجعية)

        private string GetItemName(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Items WHERE id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        #endregion
    }
}