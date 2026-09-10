using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptInventory : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        // ═══ نوع الفاتورة (محافظة على الأسماء الأصلية) ═══
        public int Invtype = 9;
        private int _procType = 1;

        // ═══ إعدادات الطباعة ═══
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType = 1;
        private int _printNo = 1;
        private string _defPrinter = "";
        private string _rptName = "rptInventoryReport.repx";
        private string _rptUrl = "";

        // ═══ متغيرات حسابية ═══
        private double _invSum = 0;
        private double _invDiscount = 0;
        private double _invVAT = 0;
        private double _invAvrgCost = 0;
        private bool _isVAT = true;
        private double _defVAT = 0;
        private bool _priceIncVAT = false;

        // ═══ المجاميع ═══
        private double _nTotal = 0;
        private double _nTotal1 = 0;
        private double _nVAT = 0;
        private double _nVAT1 = 0;
        private double _nNet = 0;
        private double _nNet1 = 0;
        private double _sum = 0;
        private double _sum1 = 0;
        private double _discount = 0;
        private double _discount1 = 0;

        // ═══ قائمة الفواتير ═══
        private InvoiceObj _invObj;
        private List<InvoiceTxt> _invoicesList;

        #endregion

        #region Constructor

        public frmRptInventory()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            _invObj = new InvoiceObj(1, 1);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime = DateTime.Now;

            // ═══ تعبئة أنواع العمليات حسب اللغة ═══
            if (MainClass.Language == "ar")
            {
                cmbType.Items.Add("مناقلة مرسلة (تحويل مخزني إلى)");
                cmbType.Items.Add("مناقلة مستلمة (تحويل مخزني من)");
                cmbType.Items.Add("بضاعة أول مدة");
                cmbType.Items.Add("أمر توريد مخزني (فاتورة إدخال)");
                cmbType.Items.Add("أمر صرف مخزني (فاتورة إخراج)");
                cmbType.Items.Add("أمر إنتاج");
                cmbType.Items.Add("طلب بضاعة");
                cmbType.Items.Add("تسوية جردية");
            }
            else
            {
                cmbType.Items.Add("Inventory transfers sent");
                cmbType.Items.Add("Inventory transfers received");
                cmbType.Items.Add("Beginning Inventory");
                cmbType.Items.Add("Input invoice");
                cmbType.Items.Add("Output invoice");
                cmbType.Items.Add("Production Order");
                cmbType.Items.Add("Items order invoice");
                cmbType.Items.Add("Save adjust");
            }

            LoadEmps();
            LoadCustomers();
            LoadPrintSettings();
            LoadBranches();
            BranchPermissions();
            LoadStores();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data Methods

        private void LoadEmps()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbusers.ItemsSource = dt.DefaultView;
                    cmbusers.DisplayMemberPath = "name";
                    cmbusers.SelectedValuePath = "id";
                    cmbusers.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void LoadCustomers()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Customers WHERE (type=2 OR type=3) ORDER BY id",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbClients.ItemsSource = dt.DefaultView;
                    cmbClients.DisplayMemberPath = "name";
                    cmbClients.SelectedValuePath = "id";
                    cmbClients.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void LoadStores()
        {
            try
            {
                DataTable dt = LoadData.Invertories(MainClass.EmpNo);
                cmbStore.ItemsSource = dt.DefaultView;
                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadBranches()
        {
            try
            {
                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                {
                    if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                        && Accounting.BranchCondition != " ")
                        branchFilter = $" AND id={MainClass.BranchNo}";
                }

                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches WHERE IS_Deleted=0 {branchFilter}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbBranches.ItemsSource = dt.DefaultView;
                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";
                    cmbBranches.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void BranchPermissions()
        {
            if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                && Accounting.BranchCondition != " ")
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.SelectedIndex = 0;
                cmbBranches.IsEnabled = false;
            }
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
                        try { _printType = Convert.ToInt32(dt.Rows[0]["printType"]); } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]); } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]); } catch { }
                        try { _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]); } catch { }
                        try { _defPrinter = dt.Rows[0]["CasherPrinter"].ToString(); } catch { }
                        try { _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]); } catch { }

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                    }
                }

                using (var da2 = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=1", _conn))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    if (dt2.Rows.Count == 1)
                    {
                        try { _priceIncVAT = Convert.ToBoolean(dt2.Rows[0]["PriceIncVAT"]); } catch { }
                        try { _defVAT = Convert.ToDouble(dt2.Rows[0]["MainVAT"]); } catch { }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region ComboBox Type Selection

        private void cmbType_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (cmbType.SelectedIndex)
            {
                case 0: Invtype = 8; _procType = 2; break; // مناقلة مرسلة
                case 1: Invtype = 8; _procType = 1; break; // مناقلة مستلمة
                case 2: Invtype = 9; _procType = 1; break; // بضاعة أول مدة
                case 3: Invtype = 4; _procType = 1; break; // أمر توريد
                case 4: Invtype = 5; _procType = 1; break; // أمر صرف
                case 5: Invtype = 6; _procType = 1; break; // أمر إنتاج
                case 6: Invtype = 14; _procType = 1; break; // طلب بضاعة
                case 7: Invtype = 7; _procType = 1; break; // تسوية جردية
            }
        }

        #endregion

        #region CheckBox Events

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = (chkAllBranches.IsChecked != true);
        }

        private void ckAllStore_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbStore.IsEnabled = (ckAllStore.IsChecked != true);
            if (ckAllStore.IsChecked == true)
                cmbStore.SelectedIndex = -1;
        }

        private void ckAllClients_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = (ckAllClients.IsChecked != true);
            if (ckAllClients.IsChecked == true)
                cmbClients.SelectedIndex = -1;
        }

        private void ckAllUsers_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbusers.IsEnabled = (ckAllUsers.IsChecked != true);
            if (ckAllUsers.IsChecked == true)
                cmbusers.SelectedIndex = -1;
        }

        private void ckTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (ckTotalPeriod.IsChecked != true);
            txtFromDate.IsEnabled = enabled;
            txtToDate.IsEnabled = enabled;
            txtStartTime.IsEnabled = enabled;
            txtEndTime.IsEnabled = enabled;
        }

        #endregion

        #region Show Invoices

        private async void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (cmbType.SelectedIndex == -1 || string.IsNullOrEmpty(cmbType.Text))
            {
                string msg = (MainClass.Language == "ar")
                    ? "الرجاء اختيار نوع العملية"
                    : "Please choose Invoice type.";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnView.IsEnabled = false;
            ProgressBar1.Value = 0;

            await Task.Run(() => ShowInvoice());

            CalculateSummaries();
            btnView.IsEnabled = true;
            ProgressBar1.Value = 100;
        }

        private void ShowInvoice()
        {
            try
            {
                Dispatcher.Invoke(() => GridControl2.ItemsSource = null);

                if (_invoicesList != null)
                    _invoicesList.Clear();

                // ═══ بناء شروط SQL ═══
                string cond = $"inv.Inv_type={Invtype} AND inv.proc_type={_procType} AND ";

                // الفرع
                if (chkAllBranches.IsChecked != true
                    && cmbBranches.SelectedIndex > -1
                    && cmbBranches.SelectedValue != null)
                    cond += $"inv.branch={cmbBranches.SelectedValue} AND ";

                // المورد/العميل
                if (ckAllClients.IsChecked != true
                    && cmbClients.SelectedIndex > -1
                    && cmbClients.SelectedValue != null)
                    cond += $" cust_id={cmbClients.SelectedValue} AND ";

                // المستخدم
                if (ckAllUsers.IsChecked != true
                    && cmbusers.SelectedIndex > -1
                    && cmbusers.SelectedValue != null)
                    cond += $" sales_emp={cmbusers.SelectedValue} AND ";

                // المستودع
                if (ckAllStore.IsChecked != true
                    && cmbStore.SelectedValue != null)
                    cond += $" inv.safe={cmbStore.SelectedValue} AND ";

                // التواريخ
                DateTime fromDate = DateTime.Parse(
                    txtFromDate.DateTime.ToShortDateString() + " " +
                    (txtStartTime.Text ?? "00:00"));
                DateTime toDate = DateTime.Parse(
                    txtToDate.DateTime.ToShortDateString() + " " +
                    (txtEndTime.Text ?? "23:59"));

                if (ckTotalPeriod.IsChecked != true)
                    cond += " date BETWEEN @date1 AND @date2 AND ";

                // ═══ بناء الاستعلام الكامل ═══
                string sqlstr =
                    $"SELECT Inv.*, " +
                    $"(SELECT ISNULL(SUM(Inv_Sub.val1 * Inv_Sub.exchange_price), 0) " +
                    $" FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS sumPrice, " +
                    $"(SELECT ISNULL(SUM(discount), 0) " +
                    $" FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS ItemDiscount, " +
                    $"(SELECT ISNULL(SUM(CASE WHEN taxval=0 THEN (val1*exchange_price) ELSE 0 END), 0) " +
                    $" FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS FreeVATSales, " +
                    $"Customers.name AS CustName, " +
                    $"Users.username AS username, " +
                    $"Branches.name AS BranchName " +
                    $"FROM Inv " +
                    $"LEFT JOIN Customers ON inv.cust_id=Customers.id " +
                    $"LEFT JOIN Users ON inv.sales_emp=Users.emp " +
                    $"LEFT JOIN Branches ON inv.branch=Branches.id " +
                    $"WHERE {cond} Inv.IS_Deleted=0 ORDER BY date DESC";

                // ═══ استدعاء InvoiceOper لجلب البيانات ═══
                _invoicesList = new InvoiceOper().BindingListOfInvoices1(
                    sqlstr, withItems: false, fromDate, toDate);

                // ═══ ربط البيانات بالـ DataGrid ═══
                Dispatcher.Invoke(() =>
                {
                    GridControl2.ItemsSource = _invoicesList;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                        "", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        #endregion

        #region Calculate Summaries

        private void CalculateSummaries()
        {
            if (_invoicesList == null || _invoicesList.Count == 0)
            {
                ResetSummaries();
                return;
            }

            _sum = 0;
            _discount = 0;
            _nTotal = 0;
            _nVAT = 0;
            _nNet = 0;

            foreach (var inv in _invoicesList)
            {
                _sum += inv.SumPrice;
                _discount += inv.TotDiscount;
                _nTotal += inv.Total;
                _nVAT += inv.VAT;
                _nNet += inv.Net;
            }

            lblSumPrice.Text = _sum.ToString("N2");
            lblDiscount.Text = _discount.ToString("N2");
            lblTotal.Text = _nTotal.ToString("N2");
            lblVAT.Text = _nVAT.ToString("N2");
            lblNet.Text = _nNet.ToString("N2");
            lblCount.Text = _invoicesList.Count.ToString();
        }

        private void ResetSummaries()
        {
            lblSumPrice.Text = "0.00";
            lblDiscount.Text = "0.00";
            lblTotal.Text = "0.00";
            lblVAT.Text = "0.00";
            lblNet.Text = "0.00";
            lblCount.Text = "0";
        }

        #endregion

        #region Detail & Show Invoice Buttons

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string globalId)
                {
                    var frm = new frmInvoiceDetails();
                    MainClass.ApplyPermissionToForm(frm);
                    MainClass.DoApplyUserSett(frm);
                    frm.btnRecalculateCost.Visibility = Visibility.Collapsed;
                    frm.ISProfit = false;
                    frm.InvGlobalId = globalId;
                    frm.ShowDialog();
                }
            }
            catch { }
        }

        private void BtnShowInv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn) return;

                var invTxt = btn.Tag as InvoiceTxt;
                if (invTxt == null) return;

                string globalId = invTxt.InvGlobalID;
                int procType = invTxt.ProcType;

                switch (cmbType.SelectedIndex)
                {
                    case 0: // مناقلة مرسلة
                    case 1: // مناقلة مستلمة
                        OpenInventoryTransfer(globalId, procType);
                        break;

                    case 2: // بضاعة أول مدة
                        OpenBeginningInventory(globalId, procType);
                        break;

                    case 3: // أمر توريد مخزني
                        OpenInputOutput(4, globalId, procType,
                            "أمر توريد مخزني (فاتورة إدخال)");
                        break;

                    case 4: // أمر صرف مخزني
                        OpenInputOutput(5, globalId, procType,
                            "أمر صرف مخزني (فاتورة إخراج)");
                        break;

                    case 5: // أمر إنتاج
                        OpenProductionOrder(globalId, procType);
                        break;

                    case 6: // طلب بضاعة
                        OpenInputOutput(14, globalId, procType, "طلب بضاعة");
                        break;

                    case 7: // تسوية جردية
                        OpenSafeAdjust(globalId, procType);
                        break;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        #endregion

        #region Open Forms (Show Invoice Source)

        private void OpenInventoryTransfer(string globalId, int procType)
        {
            var frm = new frmInventoryTransfer();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ProcType = procType;
            frm.InvType = 8;

            if (procType == 2)
                frm.Title = "مناقلة مرسلة (تحويل مخزني إلى)";
            else
            {
                frm.Title = "مناقلة مستلمة (تحويل مخزني من)";
                frm.lblReceiveTransfer.Visibility = Visibility.Visible;
            }

            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Navigate(
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=8 " +
                $"AND proc_type={procType} AND InvGlobalID=N'{globalId}'");
            frm.Activate();
        }

        private void OpenBeginningInventory(string globalId, int procType)
        {
            var frm = new frmInvPurch();
            frm.Tag = "SalePurch3";
            frm.InvType = 9;
            frm.ProcType = 1;
            frm.EntryType = 11;
            frm.Title = (MainClass.Language == "ar")
                ? "بضاعة أول المدة"
                : "Beginning inventory";

            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 2;
            frm.cmbProcTypeSrch.SelectedIndex = 2;
            frm.WindowState = WindowState.Maximized;
            frm.Show();
            frm.Navigate(
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=9 " +
                $"AND proc_type={procType} AND InvGlobalID=N'{globalId}'");
            frm.Activate();
        }

        private void OpenInputOutput(int invType, string globalId,
            int procType, string title)
        {
            var frm = new frmInvInputOutput();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = invType;
            frm.ProcType = 1;
            frm.Title = title;

            if (invType == 14)
                frm.BtnInvertoryOrder.Visibility = Visibility.Visible;

            frm.WindowState = WindowState.Maximized;
            frm.Show();
            frm.Navigate(
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type={invType} " +
                $"AND proc_type={procType} AND InvGlobalID=N'{globalId}'");
            frm.Activate();
        }

        private void OpenProductionOrder(string globalId, int procType)
        {
            var frm = new frmProductionOrder();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 6;
            frm.ProcType = 1;
            frm.Show();
            frm.Navigate(
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=6 " +
                $"AND proc_type={procType} AND InvGlobalID=N'{globalId}'");
            frm.Activate();
        }

        private void OpenSafeAdjust(string globalId, int procType)
        {
            var frm = new frmSafeAdjust();
            frm.Show();
            frm.Navigate(
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=7 " +
                $"AND proc_type={procType} AND InvGlobalID=N'{globalId}'");
            frm.Activate();
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoicesList == null || _invoicesList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("رقم الفاتورة,التاريخ,العميل/المورد,المستخدم,الفرع," +
                              "المجموع,الخصم,الإجمالي,الضريبة,الصافي");

                foreach (var inv in _invoicesList)
                {
                    sb.AppendLine(
                        $"{inv.InvNote},{inv.InvDate:d},{inv.Customer}," +
                        $"{inv.User},{inv.Branch}," +
                        $"{inv.SumPrice:N2},{inv.TotDiscount:N2}," +
                        $"{inv.Total:N2},{inv.VAT:N2},{inv.Net:N2}");
                }

                // إضافة المجاميع
                sb.AppendLine(
                    $",,,,الإجمالي," +
                    $"{_sum:N2},{_discount:N2}," +
                    $"{_nTotal:N2},{_nVAT:N2},{_nNet:N2}");

                string fileName = $"{this.Title}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(fileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في التصدير\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Print

        private void PrintDevexpress(int printType)
        {
            _rptUrl = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName = "rptInventoryReport.repx";

            if (_invoicesList == null || _invoicesList.Count == 0)
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

            if (string.IsNullOrEmpty(_defPrinter))
            {
                DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ═══ استخدام كلاس Report الموجود في المشروع ═══
                var report = new Report();
                report.Printing(
                    printType,
                    report.BindToData(
                        null, // GridView2 غير مستخدم في WPF
                        cmbType.Text,
                        txtFromDate.DateTime.ToString(),
                        txtToDate.DateTime.ToString()),
                    _rptUrl,
                    _rptName,
                    _defPrinter,
                    _printNo);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في الطباعة\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        #endregion
    }
}