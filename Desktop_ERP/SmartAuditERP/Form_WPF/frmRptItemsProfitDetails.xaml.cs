using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptItemsProfitDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;

        public int Invtype  { get; set; }
        private int Proc_Type;
        private int Inv_Type;
        public  int SelectedId { get; set; } = -1;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private double defVAT;
        private bool PricIncVAT;

        // مجاميع مخصصة (DgvIsPlus=1 بيع، DgvIsPlus=2 مرتجع)
        private double AvgCostTot,  AvgCostTot1;
        private double TotAvgTot,   TotAvgTot1;
        private double Total_,      Total1_;
        private double sum_,        sum1_;
        private double DiscTot,     DiscTot1;
        private double ProfTot,     ProfTot1;
        private double QtyTot,      QtyTot1;
        
        // مصدر البيانات
        private ObservableCollection<ItemProfitRow_ProfitDetails> ProfitList;

        private int DgvFontSize = 11;

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmRptItemsProfitDetails()
        {
            InitializeComponent();

            conn       = MainClass.ConnObj();
            Invtype    = 0;
            Proc_Type  = 1;
            Inv_Type   = 0;
            SelectedId = -1;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";

            ProfitList = new ObservableCollection<ItemProfitRow_ProfitDetails>();
            GridControl1.ItemsSource = ProfitList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Today;
            txtDateTo.DateTime   = DateTime.Today;

            LoadStores();
            LoadSettings();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Load Helpers
        // ══════════════════════════════════════════════

        private void LoadStores()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Safes " +
                "WHERE IS_Deleted=0 AND status<>2 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSafes.DisplayMemberPath = "name";
            cmbSafes.SelectedValuePath  = "id";
            cmbSafes.ItemsSource        = dt.DefaultView;
            cmbSafes.SelectedIndex      = -1;
        }

        private void LoadSettings()
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
                    }
                    catch { /* تجاهل */ }
                }

                adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=2", conn);
                dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        PricIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                        defVAT     = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
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

        #endregion

        // ══════════════════════════════════════════════
        #region ShowResult
        // ══════════════════════════════════════════════

        private void ShowResult()
        {
            try
            {
                ProfitList.Clear();

                string condWhere = "";

                // المستودع
                if (chkAllSafes.IsChecked != true &&
                    cmbSafes.SelectedValue != null)
                    condWhere += $" and Inv.safe={cmbSafes.SelectedValue}";

                // الفترة
                if (ckTotalPeriod.IsChecked != true)
                    condWhere += " and date>=@date1 and date<=@date2 ";

                // الصنف
                if (chkAll.IsChecked != true)
                {
                    if (string.IsNullOrWhiteSpace(txtItemName.Text))
                    {
                        DXMessageBox.Show("اختر مادة.", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtItemName.Focus();
                        return;
                    }
                    condWhere += $" and Inv_Sub.ItemId={SelectedId}";
                }

                condWhere += " and inv_sub.ProductId=0";

                // الفرع
                string condBranch = "";
                if (MainClass.BranchNo != -1)
                    condBranch = $"inv.branch={MainClass.BranchNo} and ";

                // التواريخ
                DateTime dateFrom = txtDateFrom.DateTime != DateTime.MinValue
                    ? txtDateFrom.DateTime : DateTime.Today;
                DateTime dateTo   = txtDateTo.DateTime != DateTime.MinValue
                    ? txtDateTo.DateTime : DateTime.Today;

                TimeSpan startTime = ParseTime(txtStartTime.Text, TimeSpan.Zero);
                TimeSpan endTime   = ParseTime(txtEndTime.Text,
                                               new TimeSpan(23, 59, 59));

                DateTime dt1 = dateFrom.Date + startTime;
                DateTime dt2 = dateTo.Date   + endTime;

                string sql =
                    "SELECT Inv.InvGlobalID, Inv_sub.store, Inv.proc_type, date, " +
                    "Inv.id, Inv_sub.proc_type AS ProcTypeSub, ItemId, val, val1, " +
                    "exchange_price, " +
                    "(exchange_price/UnitEquality) AS price, " +
                    "exchange_price, " +
                    "(val*(exchange_price/UnitEquality)) AS sum, " +
                    "Inv.Reff_No, Inv.inv_type, inv_sub.unit, " +
                    "Inv_sub.AvrgCost AS AvrgCost, discount, " +
                    "((val1*exchange_price)/InvSum)*minus AS InvDiscount, " +
                    "taxval, taxperc, Inv.PriceIncVAT " +
                    "FROM Inv, Inv_Sub " +
                    $"WHERE {condBranch} " +
                    "Inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    "AND (Inv.inv_type=2 OR Inv.inv_type=3) " +
                    "AND (inv.proc_type=1 OR inv.proc_type=2) " +
                    "AND Inv_Sub.ItemId > 0 " +
                    $"AND Inv.IS_Deleted=0 {condWhere} " +
                    "ORDER BY date DESC";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = dt1;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dt2;

                var dt = new DataTable();
                adapter.Fill(dt);

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    double invDiscount = Math.Round(
                        Convert.ToDouble(row["InvDiscount"]), 2);

                    int invType  = Convert.ToInt32(row["inv_type"]);
                    int procType = Convert.ToInt32(row["proc_type"]);

                    string rightCond = invType == 7
                        ? $"AND ProcType={row["ProcTypeSub"]}"
                        : $"AND ProcType={procType}";

                    var typeAdapter = new SqlDataAdapter(
                        $"SELECT name, IsInput FROM InvTypes " +
                        $"WHERE InvType={invType} {rightCond}", conn);
                    var typeDt = new DataTable();
                    typeAdapter.Fill(typeDt);

                    if (typeDt.Rows.Count == 0) continue;

                    string invTypeName = typeDt.Rows[0]["name"]?.ToString() ?? "";
                    string isPlus = procType == 1 ? "1" : "2";

                    double val1      = Convert.ToDouble(row["val1"]);
                    double exchPrice = Convert.ToDouble(row["exchange_price"]);
                    double avgCost   = Convert.ToDouble(row["AvrgCost"]);
                    double val       = Convert.ToDouble(row["val"]);

                    double totInv = Math.Round(val1 * exchPrice, 2);
                    double totAvg = Math.Round(val  * avgCost,   2);

                    bool priceIncVAT = Convert.ToBoolean(row["PriceIncVAT"]);

                    if (priceIncVAT)
                        totInv = Math.Round(val1 * exchPrice
                                            - Convert.ToDouble(row["taxval"]), 2);

                    double taxAdjDisc = 0.0;
                    if (priceIncVAT)
                    {
                        double taxPerc = Convert.ToDouble(row["taxperc"]);
                        taxAdjDisc = Math.Round(invDiscount
                            - invDiscount / (1 + taxPerc / 100), 2);
                    }

                    double discount = Math.Round(
                        Convert.ToDouble(row["discount"]) + invDiscount - taxAdjDisc, 2);

                    double netSum  = Math.Round(totInv - discount, 2);
                    double profit  = Math.Round(netSum - totAvg, 2);
                    string profPct = totAvg > 0
                        ? $"{Math.Round((netSum - totAvg) / totAvg * 100, 2)}%"
                        : "0";

                    var item = new ItemProfitRow_ProfitDetails
                    {
                        DgvNo        = ProfitList.Count + 1,
                        DgvStore     = Common.GetStoreName(
                                           Convert.ToInt32(row["store"])),
                        DgvInvType   = invTypeName,
                        DgvDate      = Convert.ToDateTime(row["date"])
                                               .ToShortDateString(),
                        DgvInvNo     = Convert.ToInt32(row["id"]),
                        DgvItemCode  = Common.GetItemCode(
                                           Convert.ToInt32(row["ItemId"])),
                        DgvItem      = Common.GetItemName(
                                           Convert.ToInt32(row["ItemId"])),
                        DgvUnit      = Common.GetUnitName(
                                           Convert.ToInt32(row["unit"])),
                        DgvQty       = Math.Round(val1, 2),
                        DgvAvgCost   = Math.Round(avgCost, 2),
                        DgvTotAvg    = Math.Round(totAvg, 2),
                        DgvPrice     = Math.Round(exchPrice, 2),
                        DgvTotal     = Math.Round(totInv, 2),
                        DgvDiscount  = Math.Round(discount, 2),
                        DgvSum       = Math.Round(netSum, 2),
                        DgvProfit    = Math.Round(profit, 2),
                        DgvProfitPerc = profPct,
                        DgvInvGlobalID = row["InvGlobalID"]?.ToString() ?? "",
                        DgvIsPlus    = isPlus == "1" ? 1 : 2,
                    };

                    ProfitList.Add(item);
                    ProgressBar1.Value = i + 1;
                }

                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في عرض البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TimeSpan ParseTime(string text, TimeSpan def)
        {
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"hh\:mm", null, out var r1)) return r1;
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"h\:mm",  null, out var r2)) return r2;
            return def;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Summary Cards
        // ══════════════════════════════════════════════

        private void UpdateSummaryCards()
        {
            if (ProfitList == null || ProfitList.Count == 0)
            {
                lblCount.Text      = "0";
                lblTotQty.Text     = "0.00";
                lblTotAvgCost.Text = "0.00";
                lblTotTotal.Text   = "0.00";
                lblTotSum.Text     = "0.00";
                lblTotDiscount.Text = "0.00";
                lblTotProfit.Text  = "0.00";
                return;
            }

            var sales   = ProfitList.Where(x => x.DgvIsPlus == 1).ToList();
            var returns = ProfitList.Where(x => x.DgvIsPlus == 2).ToList();

            double Calc(Func<ItemProfitRow_ProfitDetails, double> s)
                => sales.Sum(s) - returns.Sum(s);

            lblCount.Text       = ProfitList.Count.ToString("N0");
            lblTotQty.Text      = Calc(x => x.DgvQty).ToString("N2");
            lblTotAvgCost.Text  = Calc(x => x.DgvTotAvg).ToString("N2");
            lblTotTotal.Text    = Calc(x => x.DgvTotal).ToString("N2");
            lblTotSum.Text      = Calc(x => x.DgvSum).ToString("N2");
            lblTotDiscount.Text = Calc(x => x.DgvDiscount).ToString("N2");
            lblTotProfit.Text   = Calc(x => x.DgvProfit).ToString("N2");
        }

        #endregion

        // ══════════════════════════════════════════════
        #region DataGrid Events
        // ══════════════════════════════════════════════

        /// <summary>تلوين صفوف المرتجعات بلون برتقالي</summary>
        private void GridControl1_LoadingRow(object sender,
                                              DataGridRowEventArgs e)
        {
            if (e.Row.Item is ItemProfitRow_ProfitDetails item &&
                item.DgvInvType.Contains("مرتجع"))
            {
                e.Row.Background = new SolidColorBrush(
                    Color.FromArgb(80, 255, 165, 0));
            }
            else
            {
                e.Row.Background = Brushes.Transparent;
            }
        }

        private void btnOpenInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ItemProfitRow_ProfitDetails item)
            {
                try
                {
                    string globalId = item.DgvInvGlobalID;

                    if (item.DgvInvType == "فاتورة مبيعات")
                    {
                        OpenWindow<frmInvSale>(win =>
                        {
                            win.ProcType = 1;
                            win.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                         $"AND inv_type=2 AND InvGlobalID=N'{globalId}'");
                        });
                    }
                    else if (item.DgvInvType.TrimEnd() == "مرتجع مبيعات")
                    {
                        OpenWindow<frmInvSale>(win =>
                        {
                            win.Title    = "مرتجع مبيعات";
                            win.ProcType = 2;
                            win.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                         $"AND inv_type=2 AND InvGlobalID=N'{globalId}'");
                        });
                    }
                    else if (item.DgvInvType == "نقطة بيع" ||
                             item.DgvInvType.TrimEnd() == "مرتجع نقطة بيع")
                    {
                        OpenWindow<frmInvPOS>(win =>
                        {
                            win.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                                         $"AND inv_type=3 AND InvGlobalID=N'{globalId}'");
                        });
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static void OpenWindow<T>(Action<T> configure)
            where T : System.Windows.Window, new()
        {
            var win = new T();
            configure(win);
            win.WindowState = System.Windows.WindowState.Maximized;
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            win.Activate();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox / RadioButton Events
        // ══════════════════════════════════════════════

        private void chkAllSafes_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSafes.IsEnabled = chkAllSafes.IsChecked != true;
            if (chkAllSafes.IsChecked == true)
                cmbSafes.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled  = !isAll;
            txtDateTo.IsEnabled    = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled   = !isAll;
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

        // ══════════════════════════════════════════════
        #region Time TextBox Events
        // ══════════════════════════════════════════════

        private void TimeBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void TimeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                bool ok =
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"hh\:mm", null, out _) ||
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"h\:mm",  null, out _);
                if (!ok)
                    tb.Text = tb.Name == "txtStartTime" ? "00:00" : "23:59";
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Item Search
        // ══════════════════════════════════════════════

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

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllSafes.IsChecked != true &&
                cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }

            chkAll.IsChecked = false;
            addNewItem();
        }

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{txtItemName.Text}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"]?.ToString() ?? "";
                    txtItemName.Text = dt.Rows[0]["name"]?.ToString() ?? "";
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

        private void SearchByCode()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND Code=N'{txtItemCode.Text}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"]?.ToString() ?? "";
                    txtItemName.Text = dt.Rows[0]["name"]?.ToString() ?? "";
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
                var form = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.sql    = "SELECT id, name, nameEN, sale_price, unit " +
                              "FROM Items WHERE IS_Deleted=0 ORDER BY id";
                form.search = "SELECT id, name, nameEN, sale_price, unit FROM Items";
                form.Itemname       = "";
                form.txtSrchNm.Text = txtItemName.Text;
                form.ShowDialog();

                if (form.ISDone && form.ItemId > 0)
                {
                    SelectedId       = form.ItemId;
                    txtItemCode.Text = Common.GetItemCode(SelectedId);
                    txtItemName.Text = form.Itemname;
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
        #region Button Events
        // ══════════════════════════════════════════════

        private void bntShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllSafes.IsChecked != true &&
                cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }

            if (chkAll.IsChecked != true &&
                string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                DXMessageBox.Show("يجب اختيار الصنف.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtItemName.Focus();
                return;
            }

            ShowResult();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (ProfitList == null || ProfitList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"أرباح_مواد_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using var writer = new System.IO.StreamWriter(
                    path, false, System.Text.Encoding.UTF8);

                writer.WriteLine(
                    "م,المستودع,نوع العملية,التاريخ,الرقم,رمز المادة," +
                    "المادة,الوحدة,الكمية,متوسط التكلفة,إجمالي التكلفة," +
                    "السعر,المجموع,الإجمالي,الخصم,الربح,نسبة الربح");

                foreach (var item in ProfitList)
                {
                    writer.WriteLine(
                        $"{item.DgvNo},{item.DgvStore},{item.DgvInvType}," +
                        $"{item.DgvDate},{item.DgvInvNo},{item.DgvItemCode}," +
                        $"{item.DgvItem},{item.DgvUnit}," +
                        $"{item.DgvQty:N2},{item.DgvAvgCost:N2},{item.DgvTotAvg:N2}," +
                        $"{item.DgvPrice:N2},{item.DgvTotal:N2},{item.DgvSum:N2}," +
                        $"{item.DgvDiscount:N2},{item.DgvProfit:N2},{item.DgvProfitPerc}");
                }

                Process.Start(new ProcessStartInfo(path)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في التصدير:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print
        // ══════════════════════════════════════════════

        private void PrintReport(int printMode)
        {
            try
            {
                if (ProfitList == null || ProfitList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                RptUrl     = MainClass.ReportsPath;
                defPrinter = MainClass.ReportsPrinter;
                RptName    = "RptItemsProfitDetails.repx";

                string fullPath = Path.Combine(RptUrl, RptName);

                if (string.IsNullOrEmpty(RptUrl) ||
                    !Directory.Exists(RptUrl)    ||
                    !File.Exists(fullPath))
                {
                    DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                // Header
                string hPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(hPath))
                {
                    var hRpt = XtraReport.FromFile(hPath);
                    hRpt.DataSource = Common.FoundationInfoDT;
                    var hSub = (XRSubreport)report.FindControl(
                                   "headerRpt", ignoreCase: true);
                    if (hSub != null) hSub.ReportSource = hRpt;
                }

                // Footer
                string fPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(fPath))
                {
                    var fRpt = XtraReport.FromFile(fPath);
                    fRpt.DataSource = Common.FoundationInfoDT;
                    var fSub = (XRSubreport)report.FindControl(
                                   "footerRpt", ignoreCase: true);
                    if (fSub != null) fSub.ReportSource = fRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;

                if (printMode == 1)
                    for (int i = 1; i <= PrintNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

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
            var list = ProfitList.Select(item => new InventoryData
            {
                InvType          = item.DgvInvType,
                SafeName         = item.DgvStore,
                InvoiceNo        = item.DgvInvNo.ToString(),
                RefsInvNo        = "",
                ItemName         = item.DgvItem,
                Unit             = item.DgvUnit,
                Quantity         = item.DgvQty.ToString(),
                AvgCostPrice     = item.DgvAvgCost.ToString(),
                Price            = item.DgvPrice.ToString(),
                InvDate          = item.DgvDate,
                NetBeforeTax     = item.DgvSum.ToString(),
                Total            = item.DgvTotal.ToString(),
                NetDiscount      = item.DgvDiscount.ToString(),
                NetProfit        = item.DgvProfit.ToString(),
                ProfitCostRatio  = item.DgvProfitPerc,
                InventoryType    = Title,
                FromDate = txtDateFrom.DateTime.ToShortDateString(),
                ToDate   = txtDateTo.DateTime.ToShortDateString(),
                StoreFilter      = cmbSafes.SelectedIndex != -1
                                    ? cmbSafes.Text : "",
                MainItemCode     = txtItemCode.Text,
                MainItemName     = txtItemName.Text,
                User             = Common.GetEmpName(MainClass.EmpNo),
                PrintDate        = DateTime.Now.ToShortDateString(),
                Sum              = lblTotSum.Text,
                Total1           = lblTotTotal.Text,
                Quantity1        = lblTotQty.Text,
                TotCost          = lblTotAvgCost.Text,
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion
    }
}