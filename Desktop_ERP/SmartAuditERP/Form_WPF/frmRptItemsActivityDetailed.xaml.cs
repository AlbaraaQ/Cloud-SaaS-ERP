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
    public partial class frmRptItemsActivityDetailed : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;
        private SqlConnection conn1;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        public int SelectedId { get; set; } = -1;

        private string StyleFile;
        private string StyleFolder;
        private string cond;

        // مصدر البيانات — DataTable معبأ في الكود
        private ObservableCollection<ItemActivityRow_ActivityDetailed> ActivityList;

        private int DgvFontSize = 11;

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmRptItemsActivityDetailed()
        {
            InitializeComponent();

            conn        = MainClass.ConnObj();
            conn1       = MainClass.ConnObj();
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
            SelectedId  = -1;
            cond        = "";

            StyleFile   = Path.Combine(MainClass.ReportsPath,
                                        "Styles",
                                        "RptItemsActivityDetailed.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");

            ActivityList = new ObservableCollection<ItemActivityRow_ActivityDetailed>();
            GridControl1.ItemsSource = ActivityList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Today;
            txtDateTo.DateTime   = DateTime.Today;

            LoadSafes();
            LoadBranches();
            LoadPrintSettings();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Load Helpers
        // ══════════════════════════════════════════════

        public void LoadSafes()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Safes " +
                "WHERE status=1 AND IS_Deleted=0 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSafes.DisplayMemberPath = "name";
            cmbSafes.SelectedValuePath  = "id";
            cmbSafes.ItemsSource        = dt.DefaultView;
            cmbSafes.SelectedIndex      = -1;
        }

        private void LoadBranches()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Branches WHERE IS_Deleted=0",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath  = "id";
            cmbBranches.ItemsSource        = dt.DefaultView;

            if (MainClass.BranchNo != -1)
            {
                cmbBranches.SelectedValue = MainClass.BranchNo;
            }
            else
            {
                cmbBranches.SelectedIndex = -1;
            }
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
                    try
                    {
                        PrintType   = Convert.ToInt32(dt.Rows[0]["printType"]);
                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        defPrinter  = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();
                        PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                        RptName = dt.Rows[0]["RptName"]?.ToString() ?? "";
                        RptUrl  = dt.Rows[0]["RptUrl"]?.ToString() ?? "";
                    }
                    catch { /* تجاهل */ }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في تحميل الإعدادات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void loadclients(short clientType)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id, name FROM Customers " +
                $"WHERE (type={clientType} OR type=3) ORDER BY id",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                cmbClients.DisplayMemberPath = "name";
                cmbClients.SelectedValuePath  = "id";
                cmbClients.ItemsSource        = dt.DefaultView;
                cmbClients.SelectedIndex      = -1;
                cmbClients.IsEnabled          = true;
            }
        }

        private string GetsafeName(int safeId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Safes WHERE id={safeId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Last Process (public API)
        // ══════════════════════════════════════════════

        public void LastProcess(int itemId, int storeId,
                                 int clientId, int clientType)
        {
            if (itemId > 0) SelectedId = itemId;

            if (clientId > 0)
            {
                if (clientType == 1)
                    chkClients.IsChecked = true;
                else
                    chkSuplier.IsChecked = true;

                cmbClients.SelectedValue = clientId;
            }

            if (storeId > 0)
                cmbSafes.SelectedValue = storeId;

            if (itemId > 0 || clientId > 0)
            {
                cbLastProcess.IsChecked = true;
                ShowResult();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region ShowResult (Core Logic)
        // ══════════════════════════════════════════════

        public void ShowResult()
        {
            try
            {
                ActivityList.Clear();

                double incomeTotal  = 0.0;
                double outcomeTotal = 0.0;
                double balance      = 0.0;

                string condItems = "";
                string condTop   = "";

                // المستودع
                if (chkAll.IsChecked != true &&
                    cmbSafes.SelectedValue != null)
                    condItems += $" and Inv_sub.store={cmbSafes.SelectedValue}";

                // الصنف
                if (SelectedId != -1)
                    condItems += $" and Inv_Sub.ItemId={SelectedId}";

                // الفترة
                if (cbLastProcess.IsChecked == true)
                    condTop = " top (1) ";
                else if (ckTotalPeriod.IsChecked != true)
                    condItems += " and date>=@date1 and date<=@date2 ";

                // العميل
                if (chkAllClients.IsChecked != true &&
                    cmbClients.SelectedIndex > -1 &&
                    cmbClients.SelectedValue != null)
                    condItems += $" and Inv.cust_id={cmbClients.SelectedValue}";

                // الفرع
                string condBranch = "";
                if (chkAllBranches.IsChecked != true &&
                    cmbBranches.SelectedIndex != -1 &&
                    cmbBranches.SelectedValue != null)
                    condBranch = $"inv.branch={cmbBranches.SelectedValue} and ";

                // نوع العملية
                if (chkAllProcType.IsChecked != true &&
                    cmbProcType.SelectedIndex != -1)
                {
                    condItems += BuildProcTypeCondition(
                        cmbProcType.SelectedIndex);
                }

                string sql =
                    $"SELECT {condTop} " +
                    "Inv_sub.store AS safe, Inv.proc_type, " +
                    "Inv_sub.proc_type AS ProcTypeSub, date, Inv.id, " +
                    "ItemId, val, val1, exchange_price, " +
                    "(exchange_price/NULLIF(UnitEquality,0)) AS price, " +
                    "(val*(exchange_price/NULLIF(UnitEquality,0))) AS sum, " +
                    "Inv.Reff_No, Inv.inv_type, inv_sub.unit, " +
                    "Inv.cust_id, items.name AS ItemName, " +
                    "items.code AS ItemCode " +
                    "FROM Inv, Inv_Sub " +
                    "LEFT JOIN items ON inv_sub.ItemId = items.id " +
                    $"WHERE {condBranch} Inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    "AND Inv_Sub.proc_type IN(1,2) " +
                    $"AND Inv.IS_Deleted=0 {condItems} " +
                    "ORDER BY inv.date ASC";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value =
                    txtDateFrom.DateTime.Date;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    txtDateTo.DateTime.Date.AddHours(24);

                var dt = new DataTable();
                adapter.Fill(dt);

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    ProgressBar1.Value = i + 1;

                    int    invType     = Convert.ToInt32(row["inv_type"]);
                    int    procType    = Convert.ToInt32(row["proc_type"]);
                    int    procTypeSub = Convert.ToInt32(row["ProcTypeSub"]);

                    string rightCond = invType == 7
                        ? $"AND ProcType={procTypeSub}"
                        : $"AND ProcType={procType}";

                    var typeAdapter = new SqlDataAdapter(
                        $"SELECT name, IsInput, NameEn FROM InvTypes " +
                        $"WHERE InvType={invType} {rightCond}", conn1);
                    var typeDt = new DataTable();
                    typeAdapter.Fill(typeDt);

                    if (typeDt.Rows.Count == 0) continue;

                    int isInput = Convert.ToInt32(typeDt.Rows[0]["IsInput"]);
                    if (isInput == 0) continue;   // تجاهل الصفوف التي IsInput=0

                    double incQty = 0.0;
                    double outQty = 0.0;
                    double valRow = Convert.ToDouble(row["val"]);

                    if (isInput == 1)
                    {
                        incQty   = valRow;
                        balance += valRow;
                    }
                    else
                    {
                        outQty   = valRow;
                        balance -= valRow;
                    }

                    // نص نوع الفاتورة
                    bool isAr = string.Equals(MainClass.Language, "ar",
                                              StringComparison.OrdinalIgnoreCase);
                    string invTypeTxt;
                    if (isAr)
                        invTypeTxt = typeDt.Rows[0]["name"]?.ToString() ?? "";
                    else
                    {
                        invTypeTxt = typeDt.Rows[0]["NameEn"] != DBNull.Value
                            ? typeDt.Rows[0]["NameEn"].ToString()
                            : typeDt.Rows[0]["name"]?.ToString() ?? "";
                    }

                    double qty1  = Convert.ToDouble(row["val1"]);
                    double price = Convert.ToDouble(row["exchange_price"]);
                    double totInv = qty1 * price;

                    var item = new ItemActivityRow_ActivityDetailed
                    {
                        DgvNo        = ActivityList.Count + 1,
                        DgvInvType   = invTypeTxt,
                        DgvStore     = GetsafeName(Convert.ToInt32(row["safe"])),
                        DgvItemCode  = row["ItemCode"]?.ToString() ?? "",
                        DgvItemName  = row["ItemName"]?.ToString() ?? "",
                        DgvInvNo     = row["id"]?.ToString() ?? "",
                        DgvRefNo     = row["Reff_No"]?.ToString() ?? "",
                        DgvDate      = Convert.ToDateTime(row["date"])
                                               .ToShortDateString(),
                        DgvClient    = Common.GetClientName(
                                           Convert.ToInt32(row["cust_id"])),
                        DgvUnit      = Common.GetUnitName(
                                           Convert.ToInt32(row["unit"])),
                        DgvQtyInv    = Math.Round(qty1,  2),
                        DgvPriceInv  = Math.Round(price, 2),
                        DgvTotInv    = Math.Round(totInv, 2),
                        DgvIncomeQty = Math.Round(incQty, 2),
                        DgvOutcomeQty = Math.Round(outQty, 2),
                        DgvBlc       = Math.Round(balance, 2),
                        DgvPrice     = Math.Round(
                                           Convert.ToDouble(row["price"]), 2),
                        DgvTot       = Math.Round(
                                           Convert.ToDouble(row["sum"]),   2),
                    };

                    ActivityList.Add(item);
                }

                txtTotStock.Text = balance.ToString(Common.DigitsNo);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في عرض البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// بناء شرط SQL لنوع العملية
        /// </summary>
        private string BuildProcTypeCondition(int selectedIndex)
        {
            bool rdAll  = rdAllInv.IsChecked  == true;
            bool rdIn   = rdInvType1.IsChecked == true;
            bool rdOut  = rdInvType2.IsChecked == true;

            return selectedIndex switch
            {
                0  => rdAll ? " and Inv.inv_type=1 "
                    : rdIn  ? " and inv.proc_type=1 and Inv.inv_type=1 "
                    :         " and inv.proc_type=2 and Inv.inv_type=1 ",

                1  => rdAll ? " and Inv.inv_type=2 "
                    : rdIn  ? " and inv.proc_type=1 and Inv.inv_type=2 "
                    :         " and inv.proc_type=2 and Inv.inv_type=2 ",

                2  => rdAll ? " and Inv.inv_type=3 "
                    : rdIn  ? " and inv.proc_type=1 and Inv.inv_type=3 "
                    :         " and inv.proc_type=2 and Inv.inv_type=3 ",

                3  => " and Inv.inv_type=4 ",
                4  => " and Inv.inv_type=5 ",

                5  => rdAll ? " and Inv.inv_type=6 "
                    : rdIn  ? " and inv.proc_type=1 and Inv.inv_type=6 "
                    :         " and inv.proc_type=2 and Inv.inv_type=6 ",

                6  => rdAll ? " and Inv.inv_type=7 "
                    : rdIn  ? " and inv_sub.proc_type=1 and Inv.inv_type=7 "
                    :         " and inv_sub.proc_type=2 and Inv.inv_type=7 ",

                7  => rdAll ? " and Inv.inv_type=8 "
                    : rdIn  ? " and inv.proc_type=1 and Inv.inv_type=8 "
                    :         " and inv.proc_type=2 and Inv.inv_type=8 ",

                8  => " and Inv.inv_type=9 ",
                9  => " and inv.proc_type=2 and Inv.inv_type=21 ",
                10 => " and inv.proc_type=1 and Inv.inv_type=21 ",
                11 => " and inv.proc_type=1 and Inv.inv_type=22 ",
                12 => " and inv.proc_type=2 and Inv.inv_type=22 ",
                _  => ""
            };
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Search Item
        // ══════════════════════════════════════════════

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{txtItemName.Text}'",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId         = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text   = dt.Rows[0]["Code"]?.ToString() ?? "";
                    txtItemName.Text   = dt.Rows[0]["name"]?.ToString() ?? "";
                }
                else
                {
                    addNewItem();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void addNewItem()
        {
            try
            {
                var searchForm = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(searchForm);
                MainClass.DoApplyUserSett(searchForm);

                searchForm.sql    = "SELECT id, name, nameEN, sale_price, unit " +
                                    "FROM Items WHERE IS_Deleted=0 ORDER BY id";
                searchForm.search = "SELECT id, name, nameEN, sale_price, unit " +
                                    "FROM Items ";
                searchForm.Itemname    = "";
                searchForm.txtSrchNm.Text = txtItemName.Text;
                searchForm.ShowDialog();

                if (searchForm.ISDone && searchForm.ItemId > 0)
                {
                    SelectedId       = searchForm.ItemId;
                    txtItemCode.Text = Common.GetItemCode(SelectedId);
                    txtItemName.Text = searchForm.Itemname;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في البحث:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print / Export
        // ══════════════════════════════════════════════

        private void PrintDevexpress(int printMode)
        {
            try
            {
                RptUrl     = MainClass.ReportsPath;
                RptName    = "rptItemDetails.repx";
                defPrinter = MainClass.ReportsPrinter;

                if (ActivityList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(RptUrl))
                {
                    DXMessageBox.Show("يجب تحديد مسار التقرير.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fullPath = Path.Combine(RptUrl, RptName);
                if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
                {
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله.",
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                // ربط التذييل
                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var headerSub = (XRSubreport)report.FindControl(
                                        "headerRpt", ignoreCase: true);
                    if (headerSub != null)
                        headerSub.ReportSource = headerRpt;
                }

                string footerPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var footerSub = (XRSubreport)report.FindControl(
                                        "footerRpt", ignoreCase: true);
                    if (footerSub != null)
                        footerSub.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;

                if (printMode == 1)
                {
                    for (int i = 1; i <= PrintNo; i++)
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
                DXMessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BuildReportDataSet()
        {
            var list = ActivityList.Select(item => new InventoryData
            {
                SafeMain      = cmbSafes.Text,
                SafeMainCode  = cmbSafes.SelectedValue?.ToString() ?? "",
                Clint         = cmbClients.SelectedIndex > -1
                                 ? cmbClients.Text : "",
                MainItemName  = txtItemName.Text,
                MainItemCode  = txtItemCode.Text,
                ProcessType   = item.DgvInvType,
                SafeName      = item.DgvStore,
                ItemCode      = item.DgvItemCode,
                ItemName      = item.DgvItemName,
                InvoiceNo     = item.DgvInvNo,
                RefsInvNo     = item.DgvRefNo,
                InvDate       = item.DgvDate,
                Unit          = item.DgvUnit,
                Quantity1     = item.DgvQtyInv.ToString(),
                SellPrice     = item.DgvPriceInv.ToString(),
                Total1        = item.DgvTotInv.ToString(),
                ReceivedQnty  = item.DgvIncomeQty.ToString(),
                Quantity2     = item.DgvOutcomeQty.ToString(),
                Quantity      = "",
                Price         = item.DgvPrice.ToString(),
                Total         = item.DgvTot.ToString(),
                FromDate      = txtDateFrom.DateTime.ToShortDateString(),
                ToDate        = txtDateTo.DateTime.ToShortDateString(),
                Status        = " ",
                InventoryType = Title,
                SumBalance    = txtTotStock.Text,
                User          = Common.GetEmpName(MainClass.EmpNo),
                PrintDate     = DateTime.Now.ToShortDateString(),
                Sum           = ActivityList.Sum(x => x.DgvTot).ToString(),
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox / RadioButton Events
        // ══════════════════════════════════════════════

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSafes.IsEnabled = chkAll.IsChecked != true;
            if (chkAll.IsChecked == true)
                cmbSafes.SelectedIndex = -1;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void chkAllProcType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbProcType.IsEnabled    = chkAllProcType.IsChecked != true;
            pnlInvType.Visibility    = Visibility.Collapsed;

            if (chkAllProcType.IsChecked == true)
            {
                cmbProcType.SelectedIndex = -1;
            }
        }

        private void chkAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = false;
        }

        private void chkClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (chkClients.IsChecked == true)
            {
                loadclients(1);
                cmbClients.IsEnabled = true;
            }
        }

        private void chkSuplier_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (chkSuplier.IsChecked == true)
            {
                loadclients(2);
                cmbClients.IsEnabled = true;
            }
        }

        private void ChekAllItems_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = ChekAllItems.IsChecked == true;
            txtItemName.IsEnabled = !isAll;
            txtItemCode.IsEnabled = !isAll;

            if (isAll)
            {
                txtItemName.Text = "";
                txtItemCode.Text = "";
                SelectedId       = -1;
            }
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (ckTotalPeriod.IsChecked == true)
            {
                txtDateFrom.IsEnabled   = false;
                txtDateTo.IsEnabled     = false;
                cbLastProcess.IsChecked = false;
            }
            else if (cbLastProcess.IsChecked != true)
            {
                txtDateFrom.IsEnabled = true;
                txtDateTo.IsEnabled   = true;
            }
        }

        private void cbLastProcess_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cbLastProcess.IsChecked == true)
            {
                txtDateFrom.IsEnabled    = false;
                txtDateTo.IsEnabled      = false;
                ckTotalPeriod.IsChecked  = false;
            }
            else if (ckTotalPeriod.IsChecked != true)
            {
                txtDateFrom.IsEnabled = true;
                txtDateTo.IsEnabled   = true;
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region ComboBox Events
        // ══════════════════════════════════════════════

        private void cmbProcType_SelectionChanged(object sender,
                                                   SelectionChangedEventArgs e)
        {
            if (chkAllProcType.IsChecked == true) return;

            int idx = cmbProcType.SelectedIndex;
            bool isAr = string.Equals(MainClass.Language, "ar",
                                       StringComparison.OrdinalIgnoreCase);

            // تحديد ظهور لوحة أنماط الفواتير
            bool showPanel = idx is 0 or 1 or 2 or 5 or 6 or 7;
            pnlInvType.Visibility = showPanel
                                    ? Visibility.Visible
                                    : Visibility.Collapsed;

            if (!showPanel) return;

            // تحديث نصوص RadioButtons
            switch (idx)
            {
                case 0:
                    rdInvType1.Content = isAr ? "مشتريات" : "Purchase";
                    rdInvType2.Content = isAr ? "مرتجع مشتريات" : "Return Purchase";
                    break;
                case 1:
                case 2:
                    rdInvType1.Content = isAr ? "مبيعات" : "Sale";
                    rdInvType2.Content = isAr ? "مرتجع مبيعات" : "Return Sale";
                    break;
                case 5:
                    rdInvType1.Content = "إنتاج";
                    rdInvType2.Content = "مستهلك في الإنتاج";
                    break;
                case 6:
                    rdInvType1.Content = isAr
                        ? "تسوية جردية مدخلة" : "Store Adjustment In";
                    rdInvType2.Content = isAr
                        ? "تسوية جردية مخرجة" : "Store Adjustment Out";
                    break;
                case 7:
                    rdInvType1.Content = isAr ? "مناقلة من" : "Store Transfer Sent";
                    rdInvType2.Content = isAr ? "مناقلة إلى" : "Store Transfer Received";
                    break;
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region TextBox KeyDown Events
        // ══════════════════════════════════════════════

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchByName();
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            // يمكن إضافة بحث بالكود هنا
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Click Events
        // ══════════════════════════════════════════════

        private void bntShow_Click(object sender, RoutedEventArgs e)
        {
            bool isAr = string.Equals(MainClass.Language, "ar",
                                       StringComparison.OrdinalIgnoreCase);

            // التحقق من اختيار المستودع
            if (chkAll.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show(isAr ? "يجب اختيار المستودع"
                                     : "Please select store",
                                "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }

            // التحقق من اختيار الصنف
            if (ChekAllItems.IsChecked != true &&
                string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                DXMessageBox.Show(isAr ? "يجب اختيار الصنف"
                                     : "Please select item",
                                "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtItemName.Focus();
                return;
            }

            cond = "";
            ShowResult();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }

            ChekAllItems.IsChecked = false;
            addNewItem();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ActivityList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"حركة_صنف_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using var writer = new System.IO.StreamWriter(
                    outputPath, false, System.Text.Encoding.UTF8);

                writer.WriteLine(
                    "م,نوع الفاتورة,المستودع,رمز الصنف,الصنف," +
                    "رقم الفاتورة,رقم المرجع,التاريخ,الحساب,الوحدة," +
                    "الكمية في الفاتورة,السعر في الفاتورة,الإجمالي في الفاتورة," +
                    "الكمية الداخلة,الكمية الخارجة,الرصيد,السعر,الإجمالي");

                foreach (var item in ActivityList)
                {
                    writer.WriteLine(
                        $"{item.DgvNo},{item.DgvInvType},{item.DgvStore}," +
                        $"{item.DgvItemCode},{item.DgvItemName}," +
                        $"{item.DgvInvNo},{item.DgvRefNo},{item.DgvDate}," +
                        $"{item.DgvClient},{item.DgvUnit}," +
                        $"{item.DgvQtyInv:N2},{item.DgvPriceInv:N2}," +
                        $"{item.DgvTotInv:N2},{item.DgvIncomeQty:N2}," +
                        $"{item.DgvOutcomeQty:N2},{item.DgvBlc:N2}," +
                        $"{item.DgvPrice:N2},{item.DgvTot:N2}");
                }

                Process.Start(new ProcessStartInfo(outputPath)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في التصدير:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var saveItem  = new MenuItem { Header = "💾 حفظ مظهر الجدول" };
            var resetItem = new MenuItem { Header = "🔄 إعادة الإعدادات الافتراضية" };

            saveItem.Click  += MenuItemSaveSetting_Click;
            resetItem.Click += MenuItemDefualtSetting_Click;

            menu.Items.Add(saveItem);
            menu.Items.Add(resetItem);
            menu.IsOpen = true;
        }

        private void MenuItemSaveSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(StyleFolder))
                    Directory.CreateDirectory(StyleFolder);

                using var writer = new System.Xml.XmlTextWriter(
                    StyleFile, System.Text.Encoding.UTF8);
                writer.WriteStartDocument();
                writer.WriteStartElement("GridLayout");

                foreach (DataGridColumn col in GridControl1.Columns)
                {
                    writer.WriteStartElement("Column");
                    writer.WriteAttributeString("Header",
                        col.Header?.ToString() ?? "");
                    writer.WriteAttributeString("Width",
                        col.ActualWidth.ToString("F1"));
                    writer.WriteAttributeString("Visibility",
                        col.Visibility.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();

                DXMessageBox.Show("تم حفظ مظهر الجدول بنجاح.", "حفظ",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItemDefualtSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(StyleFile)) File.Delete(StyleFile);
                DXMessageBox.Show("تم إعادة الإعدادات الافتراضية.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region DataGrid — فتح الفاتورة
        // ══════════════════════════════════════════════

        private void btnOpenInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ItemActivityRow_ActivityDetailed item)
            {
                try
                {
                    string invType = item.DgvInvType;
                    string invNoStr = item.DgvInvNo;

                    if (string.IsNullOrEmpty(invType) ||
                        string.IsNullOrEmpty(invNoStr))
                        return;

                    OpenInvoiceByType(invType, invNoStr);
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenInvoiceByType(string invType, string invNo)
        {
            switch (invType)
            {
                case "فاتورة مبيعات":
                    OpenForm<frmInvSale>(f =>
                    {
                        f.InvType  = 2; f.ProcType = 1;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=2 AND proc_type=1 AND id={invNo}");
                    });
                    break;

                case "مرتجع مبيعات":
                case "مرتجع مبيعات ":
                    OpenForm<frmInvSale>(f =>
                    {
                        f.Title = "مرتجع مبيعات";
                        f.InvType  = 2; f.ProcType = 2;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=2 AND proc_type=2 AND id={invNo}");
                    });
                    break;

                case "فاتورة مشتريات":
                    OpenForm<frmInvPurch>(f =>
                    {
                        f.InvType  = 1; f.ProcType = 1;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=1 AND proc_type=1 AND id={invNo}");
                    });
                    break;

                case "مرتجع مشتريات":
                    OpenForm<frmInvPurch>(f =>
                    {
                        f.Title = "مرتجع مشتريات";
                        f.InvType  = 1; f.ProcType = 2;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=1 AND proc_type=2 AND id={invNo}");
                    });
                    break;

                case "نقطة بيع":
                    OpenForm<frmInvPOS>(f =>
                    {
                        f.ProcType = 1; f.InvType = 3;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=3 AND proc_type=1 AND id={invNo}");
                    });
                    break;

                case "مرتجع نقطة بيع":
                    OpenForm<frmInvPOS>(f =>
                    {
                        f.Title = "مرتجع"; f.ProcType = 2; f.InvType = 3;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=3 AND proc_type=2 AND id={invNo}");
                    });
                    break;

                case "بضاعة أول مدة":
                    OpenForm<frmInvPurch>(f =>
                    {
                        f.Title = "بضاعة أول مدة";
                        f.InvType  = 9; f.ProcType = 1;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND inv_type=9 AND proc_type=1 AND id={invNo}");
                    });
                    break;

                case "فاتورة الإدخال":
                    OpenForm<frmInvInputOutput>(f =>
                    {
                        f.InvType = 4; f.ProcType = 1;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=1 AND inv_type=4 AND id={invNo}");
                    });
                    break;

                case "فاتورة الإخراج":
                    OpenForm<frmInvInputOutput>(f =>
                    {
                        f.Title = "فاتورة إخراج";
                        f.InvType = 5; f.ProcType = 1;
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=1 AND inv_type=5 AND id={invNo}");
                    });
                    break;

                case "إنتاج":
                    OpenWindow<frmProductionOrder>(f =>
                    {
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=1 AND inv_type=6 AND id={invNo}");
                    });
                    break;

                case "مستهلك في الإنتاج":
                    OpenWindow<frmProductionOrder>(f =>
                    {
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=2 AND inv_type=6 AND id={invNo}");
                    });
                    break;

                case "مناقلة إلى":
                    OpenWindow<frmInventoryTransfer>(f =>
                    {
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=1 AND inv_type=8 AND id={invNo}");
                    });
                    break;

                case "مناقلة من":
                    OpenWindow<frmInventoryTransfer>(f =>
                    {
                        f.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                   $"AND proc_type=2 AND inv_type=8 AND id={invNo}");
                    });
                    break;

                case "تسوية جردية مدخلة":
                case "تسوية جردية مخرجة":
                    OpenForm<frmSafeAdjust>(f =>
                    {
                        f.Navigate($"SELECT * FROM SafesAdjust WHERE id={invNo}");
                    });
                    break;
            }
        }

        /// <summary>Helper لفتح نموذج WinForms بشكل موحد</summary>
        private static void OpenForm<T>(Action<T> configure)
            where T : System.Windows.Window, new()
        {
            var form = new T();
            configure(form);
            form.WindowState = WindowState.Maximized;
            form.Show();
            form.Activate();
        }

        private static void OpenWindow<T>(Action<T> configure)
    where T : System.Windows.Window, new()
        {
            // إنشاء نسخة من النافذة (WPF Window)
            var window = new T();

            // تنفيذ الإعدادات المرسلة (مثل التوجيه أو تمرير البيانات)
            configure(window);

            // إعدادات العرض الخاصة بـ WPF
            window.WindowState = System.Windows.WindowState.Maximized;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            window.Show();
            window.Activate();
        }

        #endregion
    }
}