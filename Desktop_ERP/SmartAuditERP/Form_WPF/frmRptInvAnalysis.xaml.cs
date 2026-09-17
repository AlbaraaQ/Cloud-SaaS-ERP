using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptInvAnalysis : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private bool tf;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        // مصدر بيانات الجدول
        private ObservableCollection<InvAnalysisRow> _itemsSource;

        #endregion

        #region Constructor

        public frmRptInvAnalysis()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            tf = true;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";

            _itemsSource = new ObservableCollection<InvAnalysisRow>();
            dgvItems.ItemsSource = _itemsSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;
            LoadStocks();
            LoadSafes();
            LoadCust();
            LoadEmps();
            LoadGroups();
            LoadSales();
            loadPrintSettings();
        }

        #endregion

        #region Load Combos

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"Select id,name from Stocks where branch={MainClass.BranchNo} And IS_Deleted=0 And status<>2 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSafe.ItemsSource = dt.DefaultView;
                cmbSafe.DisplayMemberPath = "name";
                cmbSafe.SelectedValuePath = "id";
                cmbSafe.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الصناديق: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadStocks()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from Safes where IS_Deleted=0 and status<>2 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbStock.ItemsSource = dt.DefaultView;
                cmbStock.DisplayMemberPath = "name";
                cmbStock.SelectedValuePath = "id";
                cmbStock.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadCust()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from Customers where (type=1 or type=3) and IS_Deleted=0 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbCust.ItemsSource = dt.DefaultView;
                cmbCust.DisplayMemberPath = "name";
                cmbCust.SelectedValuePath = "id";
                cmbCust.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل العملاء: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadEmps()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from Employees where IS_Deleted=0 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbEmp.ItemsSource = dt.DefaultView;
                cmbEmp.DisplayMemberPath = "name";
                cmbEmp.SelectedValuePath = "id";
                cmbEmp.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الموظفين: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSales()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from salesmen where IS_Deleted=0 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSales.ItemsSource = dt.DefaultView;
                cmbSales.DisplayMemberPath = "name";
                cmbSales.SelectedValuePath = "id";
                cmbSales.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المندوبين: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from ItemsCategory order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbGroups.ItemsSource = dt.DefaultView;
                cmbGroups.DisplayMemberPath = "name";
                cmbGroups.SelectedValuePath = "id";
                cmbGroups.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CheckBox Events

        private void chkAllStock_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbStock.IsEnabled = !(chkAllStock.IsChecked == true);
        }

        private void chkAllSafe_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbSafe.IsEnabled = !(chkAllSafe.IsChecked == true);
        }

        private void chkAllCust_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbCust.IsEnabled = !(chkAllCust.IsChecked == true);
        }

        private void chkAllEmp_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbEmp.IsEnabled = !(chkAllEmp.IsChecked == true);
        }

        private void chkAllSales_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbSales.IsEnabled = !(chkAllSales.IsChecked == true);
        }

        private void chkAllGroups_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbGroups.IsEnabled = !(chkAllGroups.IsChecked == true);
        }

        #endregion

        #region RadioButton Events

        private void rdStock_Checked(object sender, RoutedEventArgs e) { }
        private void rdCust_Checked(object sender, RoutedEventArgs e) { }
        private void rdItem_Checked(object sender, RoutedEventArgs e) { }
        private void rdSalesman_Checked(object sender, RoutedEventArgs e) { }
        private void rdEmp_Checked(object sender, RoutedEventArgs e) { }
        private void rdDay_Checked(object sender, RoutedEventArgs e) { }
        private void rdMonth_Checked(object sender, RoutedEventArgs e) { }
        private void rdGroup_Checked(object sender, RoutedEventArgs e) { }

        #endregion

        #region btnShow - عرض النتائج

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(ShowResult) { IsBackground = true }.Start();
        }

        private void ShowResult()
        {
            // التحقق من الاختيارات
            if (!(chkAllStock.IsChecked == true) && cmbStock.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر مخزن", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbStock.Focus();
                });
                return;
            }
            if (!(chkAllSafe.IsChecked == true) && cmbSafe.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر خزنة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbSafe.Focus();
                });
                return;
            }
            if (!(chkAllCust.IsChecked == true) && cmbCust.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر عميل", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbCust.Focus();
                });
                return;
            }
            if (!(chkAllEmp.IsChecked == true) && cmbEmp.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر مستخدم", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbEmp.Focus();
                });
                return;
            }
            if (!(chkAllGroups.IsChecked == true) && cmbGroups.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر مجموعة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbGroups.Focus();
                });
                return;
            }
            if (!(chkAllSales.IsChecked == true) && cmbSales.SelectedValue == null)
            {
                Dispatcher.Invoke(() =>
                {
                    DXMessageBox.Show("اختر مندوب مبيعات", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbSales.Focus();
                });
                return;
            }

            Dispatcher.Invoke(() => _itemsSource.Clear());

            bool isStock = false, isCust = false, isItem = false;
            bool isSalesman = false, isEmp = false, isMonth = false;
            bool isDay = false, isGroup = false;

            Dispatcher.Invoke(() =>
            {
                isStock = rdStock.IsChecked == true;
                isCust = rdCust.IsChecked == true;
                isItem = rdItem.IsChecked == true;
                isSalesman = rdSalesman.IsChecked == true;
                isEmp = rdEmp.IsChecked == true;
                isMonth = rdMonth.IsChecked == true;
                isDay = rdDay.IsChecked == true;
                isGroup = rdGroup.IsChecked == true;
            });

            if (isStock)
            {
                Dispatcher.Invoke(() => SetColumn1Header("المخزن"));
                ShowResultStock();
            }
            else if (isCust)
            {
                Dispatcher.Invoke(() => SetColumn1Header("العميل"));
                ShowResultCust();
            }
            else if (isItem)
            {
                Dispatcher.Invoke(() => SetColumn1Header("الصنف"));
                ShowResultItem();
            }
            else if (isSalesman)
            {
                Dispatcher.Invoke(() => SetColumn1Header("المندوب"));
                ShowResultSalesman();
            }
            else if (isEmp)
            {
                Dispatcher.Invoke(() => SetColumn1Header("المستخدم"));
                ShowResultEmp();
            }
            else if (isMonth)
            {
                Dispatcher.Invoke(() => SetColumn1Header("الفترة"));
                ShowResultMonth();
            }
            else if (isDay)
            {
                Dispatcher.Invoke(() => SetColumn1Header("اليوم"));
                ShowResultDay();
            }
            else if (isGroup)
            {
                Dispatcher.Invoke(() => SetColumn1Header("التصنيف"));
                ShowResultGroup();
            }

            // حساب المجاميع
            double sumQty = 0, sumTotal = 0, sumDiscount = 0, sumCost = 0, sumProfit = 0;

            Dispatcher.Invoke(() =>
            {
                foreach (var row in _itemsSource)
                {
                    if (double.TryParse(row.Column9, out double qty)) sumQty += qty;
                    if (double.TryParse(row.Column5, out double tot)) sumTotal += tot;
                    if (double.TryParse(row.Column10, out double disc)) sumDiscount += disc;
                    if (double.TryParse(row.Column2, out double cost)) sumCost += cost;
                    if (double.TryParse(row.Column3, out double prof)) sumProfit += prof;
                }

                txtSum1.Text = sumQty.ToString("N2");
                txtSum2.Text = sumTotal.ToString("N2");
                txtSum3.Text = sumDiscount.ToString("N2");
                txtSum4.Text = sumCost.ToString("N2");
                txtSum5.Text = sumProfit.ToString("N2");
            });
        }

        private void SetColumn1Header(string header)
        {
            Column1.Header = header;
        }

        #endregion

        #region ShowResult Methods

        /// <summary>
        /// بناء جملة الفلتر المشتركة بناءً على الاختيارات
        /// </summary>
        private string BuildWhereFilter(bool includeSafe, bool includeSafeVal,
                                         bool includeStock, bool includeStockVal,
                                         bool includeCust, bool includeCustVal,
                                         bool includeSales, bool includeSalesVal,
                                         bool includeGroups, bool includeGroupsVal)
        {
            string filter = "";

            if (includeSafe && includeSafeVal)
                filter += $"[stock]={GetSelectedValue(cmbSafe)} and ";

            if (includeStock && includeStockVal)
                filter += $"[safe]={GetSelectedValue(cmbStock)} and ";

            if (includeCust && includeCustVal)
                filter += $"cust_id={GetSelectedValue(cmbCust)} and ";

            if (includeSales && includeSalesVal)
                filter += $"salesman={GetSelectedValue(cmbSales)} and ";

            if (includeGroups && includeGroupsVal)
                filter += $"ItemsCategory.id={GetSelectedValue(cmbGroups)} and ";

            return filter;
        }

        private object GetSelectedValue(ComboBox combo)
        {
            object val = null;
            Dispatcher.Invoke(() => val = combo.SelectedValue);
            return val ?? "0";
        }

        private bool GetCheckState(CheckBox chk)
        {
            bool result = false;
            Dispatcher.Invoke(() => result = chk.IsChecked == true);
            return result;
        }

        private DateTime GetDateFrom()
        {
            DateTime result = DateTime.Today;
            Dispatcher.Invoke(() =>
            {
                if (txtDateFrom.SelectedDate.HasValue)
                    result = txtDateFrom.SelectedDate.Value;
            });
            return result;
        }

        private DateTime GetDateTo()
        {
            DateTime result = DateTime.Today;
            Dispatcher.Invoke(() =>
            {
                if (txtDateTo.SelectedDate.HasValue)
                    result = txtDateTo.SelectedDate.Value;
            });
            return result;
        }

        private void SetProgress(int value)
        {
            Dispatcher.Invoke(() => ProgressBar1.Value = value);
        }

        private void SetProgressMax(int max)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar1.Value = 0;
                ProgressBar1.Maximum = max > 0 ? max : 1;
            });
        }

        /// <summary>
        /// حساب تكلفة الوحدة بناءً على حركات الشراء
        /// </summary>
        private double CalcUnitCost(object itemId, DateTime saleDate)
        {
            double totalCostValue = 0.0;
            int totalQty = 0;
            double unitCost = 0.0;

            // من فواتير الشراء
            var adp = new SqlDataAdapter(
                $"select sum(val),sum(val*exchange_price) from inv,inv_sub " +
                $"where inv_sub.ItemId={itemId} and date <=@date2 " +
                $"and inv.proc_type=1 and inv_sub.proc_type=1 " +
                $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                conn);
            adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = saleDate;
            var dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0 && dt.Rows[0][1].ToString() != "")
            {
                double.TryParse(dt.Rows[0][1].ToString(), out double v);
                totalCostValue += v;
                if (double.TryParse(dt.Rows[0][0].ToString(), out double q))
                    totalQty += (int)Math.Round(q);
            }

            // من مرتجعات الشراء
            adp = new SqlDataAdapter(
                $"select sum(val),sum(val*exchange_price) from inv,inv_sub " +
                $"where ItemId={itemId} and date <=@date2 " +
                $"and inv.proc_type=3 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                conn);
            adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = saleDate;
            dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0 && dt.Rows[0][1].ToString() != "")
            {
                double.TryParse(dt.Rows[0][1].ToString(), out double v);
                totalCostValue += v;
                if (double.TryParse(dt.Rows[0][0].ToString(), out double q))
                    totalQty += (int)Math.Round(q);
            }

            if (totalQty != 0)
            {
                unitCost = Math.Floor(totalCostValue / totalQty * 100000000.0) / 100000000.0;
            }
            else
            {
                // استخدام سعر الشراء الافتراضي
                var adp3 = new SqlDataAdapter(
                    $"select purch_price from Items where id={itemId}", conn);
                var dt4 = new DataTable();
                adp3.Fill(dt4);
                if (dt4.Rows.Count > 0)
                    double.TryParse(dt4.Rows[0][0].ToString(), out unitCost);
            }

            return unitCost;
        }

        /// <summary>
        /// حساب تكلفة الوحدة للمستودعات (inv_type=1)
        /// </summary>
        private double CalcUnitCostStock(object itemId, DateTime saleDate)
        {
            double totalCostValue = 0.0;
            int totalQty = 0;
            double unitCost = 0.0;

            var adp = new SqlDataAdapter(
                $"select sum(val),sum(val*exchange_price) from inv,inv_sub " +
                $"where inv_sub.ItemId={itemId} and date <=@date2 " +
                $"and inv.inv_type=1 and inv.proc_type=1 and inv_sub.proc_type=1 " +
                $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                conn);
            adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = saleDate;
            var dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0 && dt.Rows[0][1].ToString() != "")
            {
                double.TryParse(dt.Rows[0][1].ToString(), out double v);
                totalCostValue += v;
                if (double.TryParse(dt.Rows[0][0].ToString(), out double q))
                    totalQty += (int)Math.Round(q);
            }

            var adp2 = new SqlDataAdapter(
                $"select sum(val),sum(val*exchange_price) from inv,inv_sub " +
                $"where ItemId={itemId} and date <=@date2 " +
                $"and inv.inv_type=1 and inv.proc_type=3 " +
                $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                conn);
            adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = saleDate;
            var dt2 = new DataTable();
            adp2.Fill(dt2);
            if (dt2.Rows.Count > 0 && dt2.Rows[0][1].ToString() != "")
            {
                double.TryParse(dt2.Rows[0][1].ToString(), out double v);
                totalCostValue += v;
                if (double.TryParse(dt2.Rows[0][0].ToString(), out double q))
                    totalQty += (int)Math.Round(q);
            }

            if (totalQty != 0)
            {
                unitCost = Math.Floor(totalCostValue / totalQty * 100000000.0) / 100000000.0;
            }
            else
            {
                var adp3 = new SqlDataAdapter(
                    $"select purch_price from Items where id={itemId}", conn);
                var dt4 = new DataTable();
                adp3.Fill(dt4);
                if (dt4.Rows.Count > 0)
                    double.TryParse(dt4.Rows[0][0].ToString(), out unitCost);
            }

            return unitCost;
        }

        private void AddRowToGrid(InvAnalysisRow row)
        {
            Dispatcher.Invoke(() => _itemsSource.Add(row));
        }

        private void UpdatePercentageColumns(double totalNet, double totalProfit)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var row in _itemsSource)
                {
                    if (double.TryParse(row.Column5, out double netVal) && totalNet != 0)
                        row.Column6 = Math.Round(netVal / totalNet * 100.0, 2).ToString("N2");

                    if (double.TryParse(row.Column3, out double profVal) && totalProfit != 0)
                        row.Column7 = Math.Round(profVal / totalProfit * 100.0, 2).ToString("N2");
                }
                dgvItems.Items.Refresh();
            });
        }

        #endregion

        #region ShowResultGroup

        private void ShowResultGroup()
        {
            try
            {
                var adp = new SqlDataAdapter("select CategoryId,name from ItemsCategory", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;

                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    filter += $"ItemsCategory.CategoryId={dt.Rows[i]["CategoryId"]} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    double netTotal = 0, netDiscount = 0, netQty = 0, netCost = 0;

                    // عدد الفواتير
                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    // الإجمالي
                    adp2 = new SqlDataAdapter(
                        "select sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0]["sum"].ToString(), out netTotal);
                        netDiscount = 0;
                    }

                    // تفاصيل للتكلفة
                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 " +
                        " group by inv_sub.ItemId,inv.date order by inv_sub.ItemId", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    var row = new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    };

                    AddRowToGrid(row);

                    totalNet += netTotal;
                    totalProfit += netProfit;

                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultItem

        private void ShowResultItem()
        {
            try
            {
                var adp = new SqlDataAdapter(
                    "select id,name from Items where IS_Deleted=0", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;

                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    filter += $"inv_sub.ItemId={dt.Rows[i]["id"]} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    // عدد الفواتير
                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    // الإجمالي
                    adp2 = new SqlDataAdapter(
                        "select sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                        double.TryParse(dt2.Rows[0]["sum"].ToString(), out netTotal);

                    // تفاصيل للتكلفة
                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0, totalQtyForCost = 0;
                    double lastUnitCost = 0;

                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        totalQtyForCost += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        lastUnitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                    }

                    netCost = Math.Round(totalQtyForCost * lastUnitCost, 4);

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    var row = new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    };

                    AddRowToGrid(row);

                    totalNet += netTotal;
                    totalProfit += netProfit;

                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultCust

        private void ShowResultCust()
        {
            try
            {
                string sql = "select id,name from customers where (type=1 or type=3) and IS_Deleted=0";
                bool allCust = GetCheckState(chkAllCust);
                if (!allCust)
                    sql += $" and [id]={GetSelectedValue(cmbCust)}";

                var adp = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    filter += $"cust_id={dt.Rows[i]["id"]} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out double grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultSalesman

        private void ShowResultSalesman()
        {
            try
            {
                string sql = "select id,name from salesmen where IS_Deleted=0";
                bool allSales = GetCheckState(chkAllSales);
                if (!allSales)
                    sql += $" and [id]={GetSelectedValue(cmbSales)}";

                var adp = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    filter += $"salesman={dt.Rows[i]["id"]} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out double grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultEmp

        private void ShowResultEmp()
        {
            try
            {
                string sql = "select id,name from employees where IS_Deleted=0";
                bool allEmp = GetCheckState(chkAllEmp);
                if (!allEmp)
                    sql += $" and [id]={GetSelectedValue(cmbEmp)}";

                var adp = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    filter += $"sales_emp={dt.Rows[i]["id"]} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out double grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultMonth

        private void ShowResultMonth()
        {
            try
            {
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();

                var adp = new SqlDataAdapter(
                    "select distinct date=DATEADD(MONTH, DATEDIFF(MONTH, 0, date), 0) " +
                    " from inv,inv_sub where inv.InvGlobalID=inv_sub.InvGlobalID " +
                    " and inv.proc_type=1 and inv_sub.proc_type=2 " +
                    " and date>=@date1 and date<=@date2 and IS_Deleted=0 " +
                    " GROUP BY DATEADD(MONTH, DATEDIFF(MONTH, 0, date),0)", conn);
                adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateTo.AddDays(1.0).ToShortDateString();
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    DateTime monthStart = Convert.ToDateTime(dt.Rows[i]["date"]);
                    DateTime monthEnd = new DateTime(
                        monthStart.Year, monthStart.Month,
                        DateTime.DaysInMonth(monthStart.Year, monthStart.Month));

                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = monthStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = monthEnd;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = monthStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = monthEnd;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out double grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = monthStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = monthEnd;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    string periodLabel = $"{monthStart.Month} - {monthStart.Year}";

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = periodLabel,
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultDay

        private void ShowResultDay()
        {
            try
            {
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();

                var adp = new SqlDataAdapter(
                    "select distinct date=DATEADD(DAY, DATEDIFF(DAY, 0, date), 0) " +
                    " from inv,inv_sub where inv.InvGlobalID=inv_sub.InvGlobalID " +
                    " and inv.proc_type=1 and inv_sub.proc_type=2 " +
                    " and date>=@date1 and date<=@date2 and IS_Deleted=0 " +
                    " GROUP BY DATEADD(DAY, DATEDIFF(DAY, 0, date),0)", conn);
                adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateTo.AddDays(1.0).ToShortDateString();
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;

                bool allSafe = GetCheckState(chkAllSafe);
                bool allStock = GetCheckState(chkAllStock);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object stockVal = GetSelectedValue(cmbStock);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={safeVal} and ";
                    if (!allStock) filter += $"[safe]={stockVal} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    DateTime dayStart = Convert.ToDateTime(dt.Rows[i]["date"]);
                    DateTime dayEnd = dayStart.AddDays(1.0);

                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dayStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dayEnd;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dayStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dayEnd;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out double grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dayStart;
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dayEnd;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCost(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = dayStart.ToShortDateString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region ShowResultStock

        private void ShowResultStock()
        {
            try
            {
                string sql = "select id,name from Safes where IS_Deleted=0 and status<>2";
                bool allStock = GetCheckState(chkAllStock);
                if (!allStock)
                    sql += $" and [id]={GetSelectedValue(cmbStock)}";

                var adp = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adp.Fill(dt);

                SetProgressMax(dt.Rows.Count);

                double totalNet = 0, totalProfit = 0;
                DateTime dateFrom = GetDateFrom();
                DateTime dateTo = GetDateTo();
                DateTime dateToPlus = dateTo.AddHours(24.0);

                bool allSafe = GetCheckState(chkAllSafe);
                bool allCust = GetCheckState(chkAllCust);
                bool allSales = GetCheckState(chkAllSales);
                bool allGroups = GetCheckState(chkAllGroups);

                object safeVal = GetSelectedValue(cmbSafe);
                object custVal = GetSelectedValue(cmbCust);
                object salesVal = GetSelectedValue(cmbSales);
                object groupVal = GetSelectedValue(cmbGroups);
                object stockSelVal = GetSelectedValue(cmbStock);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string filter = "";
                    if (!allSafe) filter += $"[stock]={stockSelVal} and ";
                    filter += $"[safe]={dt.Rows[i]["id"]} and ";
                    if (!allCust) filter += $"cust_id={custVal} and ";
                    if (!allSales) filter += $"salesman={salesVal} and ";
                    if (!allGroups) filter += $"ItemsCategory.id={groupVal} and ";
                    filter += " date>=@date1 and date<=@date2 and ";

                    // فواتير مبيعات
                    var adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.inv_type=2 " +
                        " and inv.proc_type=1 and inv_sub.proc_type=2 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    int invCount = dt2.Rows.Count;

                    // فواتير مرتجعات
                    adp2 = new SqlDataAdapter(
                        "select distinct inv.InvGlobalID from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.inv_type=3 " +
                        " and inv.proc_type=1 and inv_sub.proc_type=3 and inv.is_deleted=0", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);
                    invCount += dt2.Rows.Count;

                    // إجمالي المبيعات
                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv.inv_type=2 and inv_sub.proc_type=2 and inv.is_deleted=0 " +
                        " group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netTotal = 0, grossTotal = 0, netDiscount = 0;
                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out netTotal);
                        double.TryParse(dt2.Rows[0][1].ToString(), out grossTotal);
                        netDiscount = grossTotal - netTotal;
                    }

                    // إجمالي المرتجعات
                    adp2 = new SqlDataAdapter(
                        "select sum(tot_net),sum(tot) from " +
                        "(select min(tot_net) as tot_net,min(InvTotal) as tot " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv.inv_type=3 and inv_sub.proc_type=3 and inv.is_deleted=0 " +
                        " group by inv.InvGlobalID) as tbl", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    if (dt2.Rows.Count > 0)
                    {
                        double.TryParse(dt2.Rows[0][0].ToString(), out double retNetTotal);
                        netTotal += retNetTotal;
                        double.TryParse(dt2.Rows[0][1].ToString(), out double retGross);
                        netDiscount += retGross - retNetTotal;
                    }

                    // تفاصيل للتكلفة
                    adp2 = new SqlDataAdapter(
                        "select inv_sub.ItemId,inv.date,sum(inv_sub.val) as val, sum(val1*exchange_price) as sum " +
                        " from inv,inv_sub,Items,ItemsCategory where " + filter +
                        " inv_sub.ItemId=Items.id and Items.group_id=ItemsCategory.id " +
                        " and inv.InvGlobalID=Inv_Sub.InvGlobalID and inv.proc_type=1 " +
                        " and inv_sub.proc_type=2 and inv.is_deleted=0 group by inv_sub.ItemId,inv.date", conn);
                    adp2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                    adp2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateToPlus;
                    dt2 = new DataTable();
                    adp2.Fill(dt2);

                    double netQty = 0, netCost = 0;
                    for (int j = 0; j < dt2.Rows.Count; j++)
                    {
                        double.TryParse(dt2.Rows[j]["val"].ToString(), out double rowQty);
                        netQty += rowQty;
                        DateTime rowDate = Convert.ToDateTime(dt2.Rows[j]["date"]);
                        double unitCost = CalcUnitCostStock(dt2.Rows[j][0], rowDate);
                        netCost += Math.Round(rowQty * unitCost, 4);
                    }

                    double netProfit = netTotal - netCost;
                    double profitRatio = netCost != 0
                        ? Math.Round(netProfit / netCost * 100.0, 2) : 0;

                    AddRowToGrid(new InvAnalysisRow
                    {
                        Column1 = dt.Rows[i]["name"].ToString(),
                        Column8 = invCount.ToString(),
                        Column9 = netQty.ToString("N2"),
                        Column5 = netTotal.ToString("N2"),
                        Column10 = netDiscount.ToString("N2"),
                        Column2 = netCost.ToString("N2"),
                        Column3 = netProfit.ToString("N2"),
                        Column4 = profitRatio.ToString("N2"),
                        Column6 = "0",
                        Column7 = "0"
                    });

                    totalNet += netTotal;
                    totalProfit += netProfit;
                    SetProgress(i + 1);
                }

                UpdatePercentageColumns(totalNet, totalProfit);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region Print & Preview

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void PrintDevexpress(int type)
        {
            if (_itemsSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string path = Path.Combine(Path.GetDirectoryName(RptUrl) ?? "", RptName);
            if (!Directory.Exists(Path.GetDirectoryName(RptUrl)) || !File.Exists(path))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(path);
                report.DataSource = BindToData();

                var headerReport = XtraReport.FromFile(Path.Combine(RptUrl, "header.repx"));
                headerReport.DataSource = Common.FoundationInfoDT;
                var headerSub = (XRSubreport)report.FindControl("headerRpt", ignoreCase: true);
                if (headerSub != null) headerSub.ReportSource = headerReport;

                var footerReport = XtraReport.FromFile(Path.Combine(RptUrl, "footer.repx"));
                footerReport.DataSource = Common.FoundationInfoDT;
                var footerSub = (XRSubreport)report.FindControl("footerRpt", ignoreCase: true);
                if (footerSub != null) footerSub.ReportSource = footerReport;

                if (!string.IsNullOrEmpty(defPrinter))
                {
                    report.PrinterName = defPrinter;
                    if (type == 1)
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
                else
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region BindToData

        private DataSet BindToData()
        {
            var list = new List<InventoryData>();

            string invType = "";
            Dispatcher.Invoke(() =>
            {
                if (rdStock.IsChecked == true) invType = "المخزن";
                else if (rdCust.IsChecked == true) invType = "العميل";
                else if (rdItem.IsChecked == true) invType = "الصنف";
                else if (rdSalesman.IsChecked == true) invType = "المندوب";
                else if (rdEmp.IsChecked == true) invType = "المستخدم";
                else if (rdMonth.IsChecked == true) invType = "الفترة";
                else if (rdDay.IsChecked == true) invType = "اليوم";
                else if (rdGroup.IsChecked == true) invType = "التصنيف";
            });

            Dispatcher.Invoke(() =>
            {
                foreach (var row in _itemsSource)
                {
                    var item = new InventoryData
                    {
                        Clint = cmbCust.Text,
                        SafeMainCode = cmbSafe.SelectedValue?.ToString() ?? "",
                        SafeMain = (cmbSafe.SelectedItem as DataRowView)?["name"]?.ToString() ?? "",
                        StockCode = cmbStock.SelectedValue?.ToString() ?? "",
                        StockName = (cmbStock.SelectedItem as DataRowView)?["name"]?.ToString() ?? "",
                        CategoryName = (cmbGroups.SelectedItem as DataRowView)?["name"]?.ToString() ?? "",
                        CategoryCode = cmbGroups.SelectedValue?.ToString() ?? "",
                        InvType = invType,
                        SafeName = row.Column1,
                        InvoiceNo = row.Column8,
                        NetQuantity = row.Column9,
                        NetTotal = row.Column5,
                        NetDiscount = row.Column10,
                        NetCost = row.Column2,
                        NetProfit = row.Column3,
                        ProfitCostRatio = row.Column4,
                        PercTotSale = row.Column6,
                        ProfitToGrossProfitRatio = row.Column7,
                        FromDate = txtDateFrom.SelectedDate?.ToShortDateString() ?? "",
                        ToDate = txtDateTo.SelectedDate?.ToShortDateString() ?? "",
                        Status = " ",
                        InventoryType = Title,
                        SumNetQuantity = txtSum1.Text,
                        Total = txtSum2.Text,
                        SumNetDiscount = txtSum3.Text,
                        SumCost = txtSum4.Text,
                        SumNetProfit = txtSum5.Text,
                        User = Common.GetEmpName(MainClass.EmpNo),
                        PrintDate = DateTime.Now.ToShortDateString()
                    };
                    list.Add(item);
                }
            });

            var ds = new DataSet("Name");
            var table = UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;
        }

        #endregion

        #region loadPrintSettings

        private void loadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return;

                try
                {
                    if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pt))
                        PrintType = pt;

                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();

                    if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pn))
                        PrintNo = pn;

                    RptName = dt.Rows[0]["RptName"].ToString();
                    RptUrl = dt.Rows[0]["RptUrl"].ToString();
                }
                catch (Exception ex)
                {
                    // تجاهل خطأ قراءة إعدادات الطباعة
                    System.Diagnostics.Debug.WriteLine("loadPrintSettings inner: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region btnExport - تصدير Excel

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_itemsSource.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls",
                    FileName = "تقرير تحليل المبيعات " + DateTime.Now.ToString("yyyy-MM-dd")
                };

                if (saveDialog.ShowDialog() != true) return;

                string filePath = saveDialog.FileName;

                // استخدام COM Interop للـ Excel
                Type excelType = Type.GetTypeFromCLSID(
                    new Guid("00024500-0000-0000-C000-000000000046"));
                dynamic application = Activator.CreateInstance(excelType);
                dynamic workbook = application.Workbooks.Add(Missing.Value);
                dynamic worksheet = workbook.Sheets[1];

                try
                {
                    // الهيدر
                    var columns = new[]
                    {
                        "المستودع", "فواتير", "صافي الكمية", "صافي الإجمالي",
                        "صافي الخصم", "صافي التكلفة", "صافي الربح",
                        "نسبة الربح للتكلفة", "نسبة الاجمالي لإجمالي البيع",
                        "نسبة الربح لإجمالي الربح"
                    };

                    for (int j = 0; j < columns.Length; j++)
                        worksheet.Cells[1, j + 1] = columns[j];

                    int rowIdx = 2;
                    foreach (var row in _itemsSource)
                    {
                        worksheet.Cells[rowIdx, 1] = row.Column1;
                        worksheet.Cells[rowIdx, 2] = row.Column8;
                        worksheet.Cells[rowIdx, 3] = row.Column9;
                        worksheet.Cells[rowIdx, 4] = row.Column5;
                        worksheet.Cells[rowIdx, 5] = row.Column10;
                        worksheet.Cells[rowIdx, 6] = row.Column2;
                        worksheet.Cells[rowIdx, 7] = row.Column3;
                        worksheet.Cells[rowIdx, 8] = row.Column4;
                        worksheet.Cells[rowIdx, 9] = row.Column6;
                        worksheet.Cells[rowIdx, 10] = row.Column7;
                        rowIdx++;
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show("خطأ في استخراج البيانات: " + ex.Message, "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                workbook.Activate();
                workbook.SaveAs(filePath, Missing.Value, Missing.Value, Missing.Value,
                    Missing.Value, Missing.Value, 1 /*xlNoChange*/,
                    Missing.Value, Missing.Value, Missing.Value,
                    Missing.Value, Missing.Value);
                workbook.Close(Missing.Value, Missing.Value, Missing.Value);
                application.Quit();

                DXMessageBox.Show("تم حفظ الملف في: " + filePath, "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ:\n" + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region btnClose

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }

    // ═══════════════════════════════════════════════════════════════
    //                        Model Classes
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// نموذج صف الجدول - يحتفظ بأسماء الأعمدة الأصلية
    /// </summary>
    public class InvAnalysisRow : System.ComponentModel.INotifyPropertyChanged
    {
        private string _column1 = "";
        private string _column2 = "";
        private string _column3 = "";
        private string _column4 = "";
        private string _column5 = "";
        private string _column6 = "";
        private string _column7 = "";
        private string _column8 = "";
        private string _column9 = "";
        private string _column10 = "";

        /// <summary>المستودع / العميل / الصنف / إلخ</summary>
        public string Column1
        {
            get => _column1;
            set { _column1 = value; OnPropertyChanged(nameof(Column1)); }
        }

        /// <summary>صافي التكلفة</summary>
        public string Column2
        {
            get => _column2;
            set { _column2 = value; OnPropertyChanged(nameof(Column2)); }
        }

        /// <summary>صافي الربح</summary>
        public string Column3
        {
            get => _column3;
            set { _column3 = value; OnPropertyChanged(nameof(Column3)); }
        }

        /// <summary>نسبة الربح للتكلفة</summary>
        public string Column4
        {
            get => _column4;
            set { _column4 = value; OnPropertyChanged(nameof(Column4)); }
        }

        /// <summary>صافي الإجمالي</summary>
        public string Column5
        {
            get => _column5;
            set { _column5 = value; OnPropertyChanged(nameof(Column5)); }
        }

        /// <summary>نسبة الاجمالي لإجمالي البيع</summary>
        public string Column6
        {
            get => _column6;
            set { _column6 = value; OnPropertyChanged(nameof(Column6)); }
        }

        /// <summary>نسبة الربح لإجمالي الربح</summary>
        public string Column7
        {
            get => _column7;
            set { _column7 = value; OnPropertyChanged(nameof(Column7)); }
        }

        /// <summary>فواتير</summary>
        public string Column8
        {
            get => _column8;
            set { _column8 = value; OnPropertyChanged(nameof(Column8)); }
        }

        /// <summary>صافي الكمية</summary>
        public string Column9
        {
            get => _column9;
            set { _column9 = value; OnPropertyChanged(nameof(Column9)); }
        }

        /// <summary>صافي الخصم</summary>
        public string Column10
        {
            get => _column10;
            set { _column10 = value; OnPropertyChanged(nameof(Column10)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// نموذج بيانات التقرير للطباعة - يحتفظ بالأسماء الأصلية
    /// </summary>
    public class InventoryData5
    {
        public string Clint { get; set; }
        public string SafeMainCode { get; set; }
        public string SafeMain { get; set; }
        public string StockCode { get; set; }
        public string StockName { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public string InvType { get; set; }
        public string SafeName { get; set; }
        public string InvoiceNo { get; set; }
        public string NetQuantity { get; set; }
        public string NetTotal { get; set; }
        public string NetDiscount { get; set; }
        public string NetCost { get; set; }
        public string NetProfit { get; set; }
        public string ProfitCostRatio { get; set; }
        public string PercTotSale { get; set; }
        public string ProfitToGrossProfitRatio { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Status { get; set; }
        public string InventoryType { get; set; }
        public string SumNetQuantity { get; set; }
        public string Total { get; set; }
        public string SumNetDiscount { get; set; }
        public string SumCost { get; set; }
        public string SumNetProfit { get; set; }
        public string User { get; set; }
        public string PrintDate { get; set; }
    }
}