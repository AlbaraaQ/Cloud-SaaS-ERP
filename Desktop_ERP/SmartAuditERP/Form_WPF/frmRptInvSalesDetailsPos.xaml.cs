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
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptInvSalesDetailsPos : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;

        public int Invtype { get; set; }
        private int Proc_Type;
        private int Inv_Type;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private double defVAT;
        private bool PricIncVAT;

        private InvoiceObj InvObj;

        // مصدر البيانات — نفس كلاس InvoiceTxt الموجود
        private ObservableCollection<InvoiceTxt> InvoicesList;

        private string StyleFile;
        private string StyleFolder;

        // مجاميع
        private double nTotal,  nTotal1;
        private double nVAT,    nVAT1;
        private double nNet,    nNet1;
        private double sum,     sum1;
        private double Discount, Discount1;
        private double Cash,    Cash1;
        private double PayNetwork, PayNetwork1;
        private double _ExtraTax,  _ExtraTax1;
        private double _TotalTax1, _TotalTax2;

        private int DgvFontSize = 11;

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmRptInvSalesDetailsPos()
        {
            InitializeComponent();

            conn        = MainClass.ConnObj();
            Invtype     = 0;
            Proc_Type   = 1;
            Inv_Type    = 0;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
            InvObj      = new InvoiceObj(2, 1);

            StyleFile   = Path.Combine(MainClass.ReportsPath,
                                        "Styles",
                                        "RptInvSalesDetailsPOSLayout.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");

            InvoicesList = new ObservableCollection<InvoiceTxt>();
            GridControl2.ItemsSource = InvoicesList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Today;
            txtToDate.DateTime   = DateTime.Today;

            LoadStores();
            LoadEmps();
            LoadCustomers();
            LoadSalesMen();
            LoadSettings();
            LoadBranches();
            BranchPermissions();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Load Helpers
        // ══════════════════════════════════════════════

        private void LoadStores()
        {
            var dt = LoadData.Invertories(MainClass.EmpNo);
            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath  = "id";
            cmbStore.ItemsSource        = dt.DefaultView;
            cmbStore.SelectedIndex      = -1;
        }

        private void LoadEmps()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees " +
                    "WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbusers.DisplayMemberPath = "name";
                cmbusers.SelectedValuePath  = "id";
                cmbusers.ItemsSource        = dt.DefaultView;
                cmbusers.SelectedIndex      = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في تحميل الموظفين:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSalesMen()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM salesmen " +
                "WHERE IS_Deleted=0 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            // المندوب غير مُظهر في هذه الواجهة لكن نحافظ على الدالة
        }

        private void LoadBranches()
        {
            string extra = "";
            if (MainClass.BranchNo != -1 &&
                !string.Equals(Accounting.BranchCondition, " ",
                               StringComparison.Ordinal))
            {
                extra = $" AND BranchId={MainClass.BranchNo}";
            }

            var adapter = new SqlDataAdapter(
                $"SELECT BranchId, name FROM Branches " +
                $"WHERE IS_Deleted=0{extra}", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            // الفرع غير مُظهر في هذه الواجهة لكن نحافظ على البيانات
        }

        private void BranchPermissions()
        {
            // تطبيق صلاحية الفرع إذا لزم
            if (!string.Equals(Accounting.BranchCondition, " ",
                               StringComparison.Ordinal))
            {
                // الفرع مقيّد — لا حاجة لتغيير لأنه مخفي أصلاً
            }
        }

        private void LoadCustomers()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Customers " +
                "WHERE (type=1 OR type=3) ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            // العميل غير مُظهر في هذه الواجهة
        }

        private void LoadSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT printType, " +
                    "ISNULL(PrintFooter,0) AS PrintFooter, " +
                    "ISNULL(PrintHeader,0) AS PrintHeader " +
                    "FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        PrintType   = Convert.ToInt32(dt.Rows[0]["printType"]);
                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        defPrinter  = "";

                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();
                    }
                    catch { /* تجاهل */ }
                }

                adapter = new SqlDataAdapter(
                    "SELECT ISNULL(PriceIncVAT,0) AS PriceIncVAT, MainVAT " +
                    "FROM SettingGeneral WHERE Inv_Id=3", conn);
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
        #region Show Invoice
        // ══════════════════════════════════════════════

        private void showInvoice()
        {
            try
            {
                InvoicesList.Clear();

                string where = "";

                // نوع العملية
                if (rbAllSales.IsChecked == true)
                    where += " (inv.proc_type=1 or inv.proc_type=2) and ";
                else if (rbSales.IsChecked == true)
                    where += " inv.proc_type=1 and ";
                else if (rbReturn.IsChecked == true)
                    where += " inv.proc_type=2 and ";

                // حالة الدفع (إن ظهرت)
                if (ckAllpaids.IsChecked != true)
                {
                    if (rbpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=1 and ";
                    else if (rbunpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=0 and ";
                    else if (rbpartialpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=2 and ";
                }

                // فاتورة نقطة البيع فقط
                where += " inv.inv_type=3 and ";

                // المستودع
                if (ckAllStore.IsChecked != true &&
                    cmbStore.SelectedIndex > -1 &&
                    cmbStore.SelectedValue != null)
                    where += $" inv.safe={cmbStore.SelectedValue} and ";

                // المستخدم
                if (ckAllUsers.IsChecked != true &&
                    cmbusers.SelectedIndex > -1 &&
                    cmbusers.SelectedValue != null)
                    where += $" sales_emp={cmbusers.SelectedValue} and ";

                // بناء التواريخ
                DateTime dateFrom = DateTime.Today;
                DateTime dateTo   = DateTime.Today;

                if (ckTotalPeriod.IsChecked != true)
                {
                    DateTime baseFrom = txtFromDate.DateTime != DateTime.MinValue
                        ? txtFromDate.DateTime : DateTime.Today;
                    DateTime baseTo = txtToDate.DateTime != DateTime.MinValue
                        ? txtToDate.DateTime : DateTime.Today;

                    TimeSpan startTime = ParseTime(txtStartTime.Text, TimeSpan.Zero);
                    TimeSpan endTime   = ParseTime(txtEndTime.Text,
                                                    new TimeSpan(23, 59, 59));

                    dateFrom = baseFrom.Date + startTime;
                    dateTo   = baseTo.Date   + endTime;
                    where   += " date between @date1 and @date2 and ";
                }

                string sql =
                    "SELECT Inv.*, " +
                    "(SELECT ISNULL(SUM(Inv_Sub.val1 * Inv_Sub.exchange_price),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS sumPrice, " +
                    "(SELECT ISNULL(SUM(discount),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS ItemDiscount, " +
                    "(SELECT ISNULL(SUM(CASE WHEN taxval=0 " +
                    " THEN (val1*exchange_price) ELSE 0 END),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS FreeVATSales, " +
                    "Customers.name AS CustName, " +
                    "Users.username AS username, " +
                    "Branches.name AS BranchName, " +
                    "salesmen.name AS SalesmanTxt " +
                    "FROM Inv " +
                    "LEFT JOIN Customers ON inv.cust_id=Customers.id " +
                    "LEFT JOIN Users ON inv.sales_emp=Users.emp " +
                    "LEFT JOIN Branches ON inv.branch=Branches.BranchId " +
                    "LEFT JOIN salesmen ON inv.salesman=salesmen.id " +
                    $"WHERE {where} Inv.IS_Deleted=0 ORDER BY date DESC";

                var list = BindingListOfInvoices1(sql, false, dateFrom, dateTo);

                foreach (var item in list)
                    InvoicesList.Add(item);

                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في العرض:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<InvoiceTxt> BindingListOfInvoices1(
            string sqlstr, bool withItems,
            DateTime Date1, DateTime date2)
        {
            var sqlConn = MainClass.ConnObj();
            if (sqlConn.State != ConnectionState.Open)
                sqlConn.Open();

            var adapter = new SqlDataAdapter(sqlstr, sqlConn);
            adapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = Date1;
            adapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = date2;

            var dt   = new DataTable();
            var list = new List<InvoiceTxt>();

            adapter.Fill(dt);

            ProgressBar1.Value   = 0;
            ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    DataRow row = dt.Rows[i];

                    int invTypeInt  = Convert.ToInt32(row["inv_type"]);
                    int procTypeInt = Convert.ToInt32(row["proc_type"]);
                    int payTypeInt  = Convert.ToInt32(row["pay_type"]);

                    string invoiceType = InvoiceOper.GetInvoiceType(
                        invTypeInt, procTypeInt, payTypeInt, 1);

                    var item = new InvoiceTxt
                    {
                        ISNew          = false,
                        IsPrinted      = true,
                        IsLoaded       = false,
                        InvGlobalID    = row["InvGlobalID"]?.ToString() ?? "",
                        EntryGlobalID  = $"{row["branch"]}-{row["EntryID"]}",
                        ProcType       = procTypeInt,
                        InvoiceNo      = Convert.ToInt32(row["id"]),
                        ReffNo         = row["Reff_No"]?.ToString() ?? "",
                        InvoiceType    = (InvoiceType)invTypeInt,
                        InvoiceTypeTxt = invoiceType,
                        InvoiceStatus  = Convert.ToInt32(row["InvoiceStatus"]),
                        Treasury       = Convert.ToInt32(row["stock"]),
                        Store          = Convert.ToInt32(row["safe"]),
                        AdditionalCost = Convert.ToDecimal(row["AdditionalCost"]),
                        AutoIncrementID = i + 1,
                    };

                    // التاريخ والوقت
                    item.InvDate     = Convert.ToDateTime(row["date"]);
                    item.InvTime     = item.InvDate;
                    item.InvoiceTime = item.InvDate.ToString("hh:mm:ss tt");

                    // المرجع
                    try
                    {
                        item.RefDate = Convert.ToDateTime(row["Reff_date"]);
                    }
                    catch { /* تجاهل */ }

                    // الضريبة
                    double taxVal   = Convert.ToDouble(row["tax"]);
                    double extraVAT = row.Table.Columns.Contains("ExtraVAT")
                                      ? Convert.ToDouble(row["ExtraVAT"])
                                      : 0.0;

                    item.VAT      = taxVal;
                    item.ExtraVAT = extraVAT;
                    item.TotalTax = taxVal + extraVAT;

                    // الدفع
                    item.PayType  = payTypeInt;
                    item.Paid     = Convert.ToDouble(row["paid"]);
                    item.Remainder = item.Paid - Convert.ToDouble(row["tot_net"]);
                    item.Bank     = Convert.ToInt32(row["bank"]);

                    // النقدي / الشبكة
                    item.Paycash = Convert.ToDouble(row["cash"]);
                    if (payTypeInt == 1)
                        item.Paycash = Convert.ToDouble(row["tot_net"]);
                    item.PayATM = Convert.ToDouble(row["visa"]);

                    // نوع الدفع نصي
                    item.PaymentTxt = GetPaymentText(payTypeInt);

                    // الخصم
                    item.InvDiscount = Convert.ToDouble(row["minus"]);
                    item.TotDiscount = item.InvDiscount;

                    // المبالغ
                    item.Total = Convert.ToDouble(row["InvTotal"]);
                    item.Net   = Convert.ToDouble(row["tot_net"]);

                    if (!withItems)
                    {
                        item.TotDiscount += Convert.ToDouble(row["ItemDiscount"]);
                        item.SumPrice     = Convert.ToDouble(row["sumPrice"]);
                    }

                    item.FreeVATSales = Convert.ToDouble(row["FreeVATSales"]);

                    // العميل
                    item.Customer  = Convert.ToInt32(row["cust_id"]);
                    item.ClientTxt = row["CustName"] != DBNull.Value
                                      ? row["CustName"].ToString() : "";

                    // المندوب
                    item.Saleman     = Convert.ToInt32(row["salesman"]);
                    item.SalesmanTxt = row["SalesmanTxt"]?.ToString() ?? "";

                    // المستخدم
                    item.User    = Convert.ToInt32(row["sales_emp"]);
                    item.UserTxt = row["username"] != DBNull.Value
                                    ? row["username"].ToString() : "";

                    // المستودع والفرع
                    item.InvertoryTxt = Common.GetStoreName(
                                            Convert.ToInt32(row["safe"]));
                    item.BranchTxt = row["BranchName"]?.ToString() ?? "";

                    // إعدادات VAT
                    var invObj = new InvoiceObj(invTypeInt, procTypeInt);
                    item.VATperc      = invObj.VAT;
                    item.ExtraVATPerc = invObj.AdditionalTax;
                    item.PriceIncVAT  = invObj.PriceIncVAT;

                    list.Add(item);
                    ProgressBar1.Value = i + 1;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"خطأ في صف {i}: {ex.Message}");
                }
            }

            sqlConn.Close();
            return list;
        }

        private string GetPaymentText(int payType)
        {
            return payType switch
            {
                -1 => "آجل",
                 1 => "نقدي",
                 2 => "شبكة",
                 4 => "متعدد",
                 5 => "ضيافة",
                 _ => ""
            };
        }

        private TimeSpan ParseTime(string text, TimeSpan defaultValue)
        {
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"hh\:mm", null, out TimeSpan r1)) return r1;
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"h\:mm",  null, out TimeSpan r2)) return r2;
            return defaultValue;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Summary Cards
        // ══════════════════════════════════════════════

        private void UpdateSummaryCards()
        {
            if (InvoicesList == null || InvoicesList.Count == 0)
            {
                lblTotalCount.Text    = "0";
                lblTotalSumPrice.Text = "0.00";
                lblTotalDiscount.Text = "0.00";
                lblTotalTotal.Text    = "0.00";
                lblTotalVAT.Text      = "0.00";
                lblTotalExtraVAT.Text = "0.00";
                lblTotalTax.Text      = "0.00";
                lblTotalNet.Text      = "0.00";
                lblTotalCash.Text     = "0.00";
                lblTotalATM.Text      = "0.00";
                return;
            }

            var purchases = InvoicesList.Where(x => x.ProcType == 1).ToList();
            var returns   = InvoicesList.Where(x => x.ProcType == 2).ToList();

            double Calc(Func<InvoiceTxt, double> selector)
                => purchases.Sum(selector) - returns.Sum(selector);

            lblTotalCount.Text    = InvoicesList.Count.ToString("N0");
            lblTotalSumPrice.Text = Calc(x => x.SumPrice).ToString("N2");
            lblTotalDiscount.Text = Calc(x => x.TotDiscount).ToString("N2");
            lblTotalTotal.Text    = Calc(x => x.Total).ToString("N2");
            lblTotalVAT.Text      = Calc(x => x.VAT).ToString("N2");
            lblTotalExtraVAT.Text = Calc(x => x.ExtraVAT).ToString("N2");
            lblTotalTax.Text      = Calc(x => x.TotalTax).ToString("N2");
            lblTotalNet.Text      = Calc(x => x.Net).ToString("N2");
            lblTotalCash.Text     = Calc(x => x.Paycash).ToString("N2");
            lblTotalATM.Text      = Calc(x => x.PayATM).ToString("N2");
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox Events
        // ══════════════════════════════════════════════

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbStore.IsEnabled = ckAllStore.IsChecked != true;
            if (ckAllStore.IsChecked == true)
                cmbStore.SelectedIndex = -1;
        }

        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbusers.IsEnabled = ckAllUsers.IsChecked != true;
            if (ckAllUsers.IsChecked == true)
                cmbusers.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled  = !isAll;
            txtToDate.IsEnabled    = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled   = !isAll;
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
                bool valid =
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"hh\:mm", null, out _) ||
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"h\:mm",  null, out _);
                if (!valid)
                    tb.Text = tb.Name == "txtStartTime" ? "00:00" : "23:59";
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Events
        // ══════════════════════════════════════════════

        private void Button1_Click(object sender, RoutedEventArgs e)
            => showInvoice();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            DgvFontSize = Math.Min(DgvFontSize + 1, 22);
            GridControl2.FontSize = DgvFontSize;
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            DgvFontSize = Math.Max(DgvFontSize - 1, 8);
            GridControl2.FontSize = DgvFontSize;
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (InvoicesList == null || InvoicesList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"مبيعات_POS_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using var writer = new System.IO.StreamWriter(
                    outputPath, false, System.Text.Encoding.UTF8);

                writer.WriteLine(
                    "#,نوع الفاتورة,رقم الفاتورة,التاريخ,الوقت," +
                    "نوع الدفع,العميل,نقدي,شبكة," +
                    "المجموع,الخصم,الإجمالي,الضريبة," +
                    "ضريبة إضافية,إجمالي الضريبة,الصافي," +
                    "المستودع,الفرع,المندوب,المستخدم");

                foreach (var item in InvoicesList)
                {
                    writer.WriteLine(
                        $"{item.AutoIncrementID}," +
                        $"{item.InvoiceTypeTxt}," +
                        $"{item.InvoiceNo}," +
                        $"{item.InvDate:dd/MM/yyyy}," +
                        $"{item.InvoiceTime}," +
                        $"{item.PaymentTxt}," +
                        $"{item.ClientTxt}," +
                        $"{item.Paycash:N2}," +
                        $"{item.PayATM:N2}," +
                        $"{item.SumPrice:N2}," +
                        $"{item.TotDiscount:N2}," +
                        $"{item.Total:N2}," +
                        $"{item.VAT:N2}," +
                        $"{item.ExtraVAT:N2}," +
                        $"{item.TotalTax:N2}," +
                        $"{item.Net:N2}," +
                        $"{item.InvertoryTxt}," +
                        $"{item.BranchTxt}," +
                        $"{item.SalesmanTxt}," +
                        $"{item.UserTxt}");
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

            saveItem.Click  += BtnSaveDgvSettings_Click;
            resetItem.Click += BtnDefaultSetting_Click;

            menu.Items.Add(saveItem);
            menu.Items.Add(resetItem);
            menu.IsOpen = true;
        }

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(StyleFolder))
                    Directory.CreateDirectory(StyleFolder);

                using var writer = new System.Xml.XmlTextWriter(
                    StyleFile, System.Text.Encoding.UTF8);

                writer.WriteStartDocument();
                writer.WriteStartElement("GridLayout");

                foreach (DataGridColumn col in GridControl2.Columns)
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

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
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
        #region DataGrid Button Events
        // ══════════════════════════════════════════════

        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceTxt item)
            {
                try
                {
                    var form = new frmInvoiceDetails();
                    MainClass.ApplyPermissionToForm(form);
                    MainClass.DoApplyUserSett(form);
                    form.btnRecalculateCost.Visibility = Visibility.Collapsed;
                    form.ISProfit    = false;
                    form.InvGlobalId = item.InvGlobalID;
                    form.ShowDialog();
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnShowInv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceTxt item)
            {
                try
                {
                    string globalId = item.InvGlobalID;
                    int    procType = item.ProcType;
                    int    invType  = Convert.ToInt32(item.InvoiceType);

                    if (invType == 2)
                    {
                        var form = new frmInvSale();
                        if (procType == 2) form.Title = "مرتجع مبيعات";
                        form.Show();
                        form.InvType     = 2;
                        form.ProcType    = procType;
                        form.WindowState =
                            WindowState.Maximized;
                        form.Navigate(
                            $"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                            $"AND inv_type=2 AND proc_type={procType} " +
                            $"AND InvGlobalID=N'{globalId}'");
                        form.Activate();
                    }
                    else
                    {
                        var form = new frmInvPOS();
                        if (procType == 2) form.Title = "مرتجع";
                        form.Show();
                        form.ProcType    = procType;
                        form.WindowState =
                            WindowState.Maximized;
                        form.Navigate(
                            $"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                            $"AND inv_type=3 AND proc_type={procType} " +
                            $"AND InvGlobalID=N'{globalId}'");
                        form.Activate();
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print
        // ══════════════════════════════════════════════

        private void PrintReport(int printType)
        {
            try
            {
                if (InvoicesList == null || InvoicesList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                RptUrl     = MainClass.ReportsPath;
                defPrinter = MainClass.ReportsPrinter;
                RptName    = "rptInvSumtPos.repx";

                // Bridge Pattern: GridView وهمي لتمريره لكلاس Report القديم
                using var tempGrid = new DevExpress.XtraGrid.GridControl();
                var tempView = new DevExpress.XtraGrid.Views.Grid.GridView(tempGrid);
                tempGrid.MainView = tempView;
                tempGrid.ViewCollection.Add(tempView);
                tempGrid.DataSource = InvoicesList.ToList();
                tempGrid.ForceInitialize();
                tempView.PopulateColumns();

                var report = new Report();
                report.Printing(
                    printType,
                    report.BindToData(
                        tempView,
                        Title,
                        txtFromDate.DateTime.ToShortDateString(),
                        txtToDate.DateTime.ToShortDateString()),
                    RptUrl, RptName, defPrinter, 1);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}