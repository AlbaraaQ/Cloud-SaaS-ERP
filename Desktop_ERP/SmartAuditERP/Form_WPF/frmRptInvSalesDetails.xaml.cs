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
using System.Windows.Media;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptInvSalesDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields & Properties
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

        // Client_id: محافظة على الاسم الأصلي
        public int _Client_id { get; set; }

        public string Client_id
        {
            get => _Client_id.ToString();
            set
            {
                if (int.TryParse(value, out int result))
                    _Client_id = result;
            }
        }

        private double defVAT;
        private bool PricIncVAT;

        private InvoiceObj InvObj;

        // مصدر بيانات الجدول — نفس كلاس InvoiceTxt الموجود لديك
        private ObservableCollection<InvoiceTxt> InvoicesList;

        private string StyleFile;
        private string StyleFolder;

        // مجاميع مخصصة
        private double nTotal,  nTotal1;
        private double nVAT,    nVAT1;
        private double nNet,    nNet1;
        private double sum,     sum1;
        private double Discount, Discount1;
        private double Cash,    Cash1;
        private double PayNetwork, PayNetwork1;
        private double _ExtraTax,  _ExtraTax1;
        private double _TotalTax1, _TotalTax2;
        private double _Paid1,     _Paid2;

        private int DgvFontSize = 11;

        public string _ToolNames { get; set; }

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmRptInvSalesDetails()
        {
            InitializeComponent();

            conn         = MainClass.ConnObj();
            Invtype      = 0;
            Proc_Type    = 1;
            Inv_Type     = 0;
            PrintHeader  = true;
            PrintFooter  = true;
            PrintStamp   = true;
            PrintNo      = 1;
            RptName      = "";
            RptUrl       = "";
            InvObj       = new InvoiceObj(2, 1);
            StyleFile    = Path.Combine(MainClass.ReportsPath,
                                         "Styles", "RptInvSalesDetailsLayout.xml");
            StyleFolder  = Path.Combine(MainClass.ReportsPath, "Styles");

            InvoicesList = new ObservableCollection<InvoiceTxt>();
            GridControl2.ItemsSource = InvoicesList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تحقق من إعداد ToolMenuProcces
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=2", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                // إذا كان OperInvSale فارغاً نخفي زر العمليات
                if (dt.Rows.Count > 0 &&
                    dt.Rows[0]["OperInvSale"] == DBNull.Value)
                {
                    btnOperations.Visibility = Visibility.Collapsed;
                }
            }
            catch { /* تجاهل */ }

            txtFromDate.DateTime = DateTime.Today;
            txtToDate.DateTime   = DateTime.Today;

            // تعبئة نوع الدفع
            bool isAr = string.Equals(MainClass.Language, "ar",
                                       StringComparison.OrdinalIgnoreCase);

            cmbType.Items.Add(isAr ? "نقدية"  : "Cash");
            cmbType.Items.Add(isAr ? "آجلة"   : "Postpone");
            cmbType.Items.Add(isAr ? "شبكة"   : "ATM");
            cmbType.Items.Add(isAr ? "بنك"    : "Bank");

            // تعبئة نوع الفاتورة
            cmbInvType.Items.Add(isAr ? "مبيعات"   : "Sales Inv");
            cmbInvType.Items.Add(isAr ? "نقطة بيع" : "POS");
            cmbInvType.Items.Add(isAr ? "أندرويد"  : "Android");

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
                $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0{extra}",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath  = "BranchId";
            cmbBranches.ItemsSource        = dt.DefaultView;
            cmbBranches.SelectedIndex      = -1;
        }

        private void BranchPermissions()
        {
            if (!string.Equals(Accounting.BranchCondition, " ",
                               StringComparison.Ordinal))
            {
                chkAllBranches.IsChecked    = false;
                chkAllBranches.Visibility   = Visibility.Collapsed;
                cmbBranches.IsEnabled       = true;
                if (cmbBranches.Items.Count > 0)
                    cmbBranches.SelectedIndex = 0;
            }
        }

        private void LoadEmps()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id",
                    conn);
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
                "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSaleman.DisplayMemberPath = "name";
            cmbSaleman.SelectedValuePath  = "id";
            cmbSaleman.ItemsSource        = dt.DefaultView;
            cmbSaleman.SelectedIndex      = -1;
        }

        private void LoadCustomers()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Customers WHERE (type=1 OR type=3) ORDER BY id",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbClients.DisplayMemberPath = "name";
            cmbClients.SelectedValuePath  = "id";
            cmbClients.ItemsSource        = dt.DefaultView;
            cmbClients.SelectedIndex      = -1;
        }

        public void LoadClients()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Customers WHERE (type=1 OR type=3) " +
                "AND IS_Deleted=0 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbClients.DisplayMemberPath = "name";
            cmbClients.SelectedValuePath  = "id";
            cmbClients.ItemsSource        = dt.DefaultView;
            cmbClients.SelectedIndex      = -1;
        }

        private void LoadStores()
        {
            var dt = LoadData.Invertories(MainClass.EmpNo);

            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath  = "id";
            cmbStore.ItemsSource        = dt.DefaultView;
            cmbStore.SelectedIndex      = -1;
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
                    catch { /* تجاهل الحقول الفردية */ }
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
        #region Show Invoice
        // ══════════════════════════════════════════════

        public void showInvoice()
        {
            try
            {
                InvoicesList.Clear();

                string where = "";

                // الفرع
                if (cmbBranches.SelectedIndex > -1 &&
                    cmbBranches.SelectedValue != null)
                    where += $"inv.branch={cmbBranches.SelectedValue} and ";

                // العميل
                if (ckAllClients.IsChecked != true &&
                    cmbClients.SelectedIndex > -1 &&
                    cmbClients.SelectedValue != null)
                    where += $"cust_id={cmbClients.SelectedValue} and ";

                // المستخدم
                if (ckAllUsers.IsChecked != true &&
                    cmbusers.SelectedIndex > -1 &&
                    cmbusers.SelectedValue != null)
                    where += $"sales_emp={cmbusers.SelectedValue} and ";

                // المندوب
                if (cbAllSalesmen.IsChecked != true &&
                    cmbSaleman.SelectedIndex > -1 &&
                    cmbSaleman.SelectedValue != null)
                    where += $"salesman={cmbSaleman.SelectedValue} and ";

                // المستودع
                if (ckAllStore.IsChecked != true &&
                    cmbStore.SelectedIndex > -1 &&
                    cmbStore.SelectedValue != null)
                    where += $"inv.safe={cmbStore.SelectedValue} and ";

                // نوع العملية
                if (rbAllSales.IsChecked == true)
                    where += " (inv.proc_type=1 or inv.proc_type=2) and ";
                else if (rbSales.IsChecked == true)
                    where += " inv.proc_type=1 and ";
                else if (rbReturn.IsChecked == true)
                    where += " inv.proc_type=2 and ";
                //where += " (inv.proc_type=1 or inv.proc_type=2) and ";

                // حالة الدفع
                if (ckAllpaids.IsChecked != true)
                {
                    if (rbpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=1 and ";
                    else if (rbunpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=0 and ";
                    else if (rbpartialpaidinvo.IsChecked == true)
                        where += " inv.PaymentStatus=2 and ";
                }

                // نوع الفاتورة
                if (ckAllInvs.IsChecked != true)
                {
                    switch (cmbInvType.SelectedIndex)
                    {
                        case 0: where += " inv.inv_type=2 and ";           break;
                        case 1: where += " inv.inv_type=3 and ";           break;
                        case 2: where += " inv.inv_type=20 and ";          break;
                    }
                }
                else
                {
                    where += " (inv.inv_type=2 or inv.inv_type=3 " +
                             "or inv.inv_type=20) and ";
                }

                // نوع الدفع
                if (ckAllType.IsChecked != true && cmbType.SelectedIndex >= 0)
                {
                    switch (cmbType.SelectedIndex)
                    {
                        case 0: where += " inv.pay_type=1 and ";                   break;
                        case 1: where += " inv.pay_type=-1 and ";                  break;
                        case 2: where += " inv.pay_type=2 and bank<=1 and ";       break;
                        case 3: where += " inv.pay_type=2 and bank>1 and ";        break;
                    }
                }

                // بناء التواريخ
                DateTime dateFrom = DateTime.Today;
                DateTime dateTo   = DateTime.Today;

                if (ckTotalPeriod.IsChecked != true)
                {
                    DateTime baseFrom = txtFromDate.DateTime != DateTime.MinValue
                        ? txtFromDate.DateTime : DateTime.Today;
                    DateTime baseTo   = txtToDate.DateTime != DateTime.MinValue
                        ? txtToDate.DateTime : DateTime.Today;

                    TimeSpan startTime = ParseTime(txtStartTime.Text, TimeSpan.Zero);
                    TimeSpan endTime   = ParseTime(txtEndTime.Text,
                                                    new TimeSpan(23, 59, 59));

                    dateFrom = baseFrom.Date + startTime;
                    dateTo   = baseTo.Date   + endTime;
                    where   += " date between @date1 and @date2 and ";
                }

                string sql =
                    "SELECT inv.InvGlobalID, inv.EntryID, inv.inv_type, inv.proc_type, " +
                    "inv.pay_type, inv.branch, inv.date, inv.id, inv.AdditionalCost, " +
                    "inv.InvoiceStatus, inv.stock, inv.safe, inv.tax, " +
                    "ISNULL(inv.ExtraVAT,0) AS ExtraVAT, inv.minus, inv.paid, " +
                    "inv.tot_net, inv.Reff_No, inv.Reff_date, " +
                    "inv.cash, inv.visa, inv.bank, inv.cust_id, inv.sales_emp, " +
                    "inv.InvTotal, inv.PaymentStatus, " +
                    "(SELECT ISNULL(SUM(Inv_Sub.val1 * Inv_Sub.exchange_price),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS sumPrice, " +
                    "(SELECT ISNULL(SUM(discount),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS ItemDiscount, " +
                    "(SELECT ISNULL(SUM(CASE WHEN taxval=0 " +
                    " THEN (val1*exchange_price) ELSE 0 END),0) " +
                    " FROM Inv_Sub WHERE InvGlobalID=Inv.InvGlobalID) AS FreeVATSales, " +
                    "Customers.name AS CustName, Users.username AS username, " +
                    "Branches.name AS BranchName, salesmen.name AS SalesmanTxt " +
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

                    // جلب رقم ضريبي للعميل لتحديد نوع الفاتورة
                    short taxType = 1;
                    try
                    {
                        var taxAdapter = new SqlDataAdapter(
                            $"SELECT ISNULL(tax_no,'') AS tax_no " +
                            $"FROM customers WHERE id={row["cust_id"]}",
                            sqlConn);
                        var taxDt = new DataTable();
                        taxAdapter.Fill(taxDt);

                        if (taxDt.Rows.Count > 0)
                        {
                            string taxNo = taxDt.Rows[0]["tax_no"].ToString();
                            if (string.IsNullOrEmpty(taxNo))
                                taxType = 1;
                            else if (!double.TryParse(taxNo, out double taxVal)
                                     || taxVal <= 0)
                                taxType = 2;
                            else
                                taxType = 2;
                        }
                    }
                    catch { taxType = 1; }

                    string invoiceType = InvoiceOper.GetInvoiceType(
                        invTypeInt, procTypeInt, payTypeInt, taxType);

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
                    double totalTax  = Convert.ToDouble(row["tax"]);
                    double extraVAT  = Convert.ToDouble(row["ExtraVAT"]);
                    double mainVAT   = totalTax - extraVAT;

                    item.VAT      = mainVAT > 0 ? mainVAT : totalTax;
                    item.ExtraVAT = extraVAT;
                    item.TotalTax = item.VAT > 0
                                    ? item.VAT + extraVAT
                                    : totalTax + extraVAT;

                    // الدفع
                    item.PayType  = payTypeInt;
                    item.Bank     = Convert.ToInt32(row["bank"]);
                    item.Paid     = Convert.ToDouble(row["paid"]);
                    item.Remainder = item.Paid - Convert.ToDouble(row["tot_net"]);

                    // النقدي / الشبكة
                    item.Paycash = Convert.ToDouble(row["cash"]);
                    if (payTypeInt == 1 &&
                        Convert.ToInt32(row["PaymentStatus"]) == 1)
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

                    // المجموع والخصم مع البنود
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
            bool isAr = string.Equals(MainClass.Language, "ar",
                                       StringComparison.OrdinalIgnoreCase);
            return payType switch
            {
                -1 => isAr ? "آجل"    : "Credit",
                 1 => isAr ? "نقدي"   : "Cash",
                 2 => isAr ? "شبكة"   : "Card",
                 4 => isAr ? "متعدد"  : "Multi",
                 5 => isAr ? "ضيافة"  : "Guest",
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
                lblTotalPaid.Text     = "0.00";
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
            lblTotalPaid.Text     = Calc(x => x.Paid).ToString("N2");
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox / RadioButton Events
        // ══════════════════════════════════════════════

        private void ckAllType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbType.IsEnabled = ckAllType.IsChecked != true;
            if (ckAllType.IsChecked == true)
                cmbType.SelectedIndex = -1;
        }

        private void cbAllSalesmen_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSaleman.IsEnabled = cbAllSalesmen.IsChecked != true;
            if (cbAllSalesmen.IsChecked == true)
                cmbSaleman.SelectedIndex = -1;
        }

        private void ckAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = ckAllClients.IsChecked != true;
            if (ckAllClients.IsChecked == true)
                cmbClients.SelectedIndex = -1;
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

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbInvType.IsEnabled = ckAllInvs.IsChecked != true;
            if (ckAllInvs.IsChecked == true)
                cmbInvType.SelectedIndex = -1;
        }

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbStore.IsEnabled = ckAllStore.IsChecked != true;
            if (ckAllStore.IsChecked == true)
                cmbStore.SelectedIndex = -1;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
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
                    $"فواتير_مبيعات_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using (var writer = new System.IO.StreamWriter(
                           outputPath, false,
                           System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(
                        "#,نوع الفاتورة,رقم الفاتورة,التاريخ,الوقت," +
                        "نوع الدفع,المدفوع,العميل,نقدي,شبكة," +
                        "المجموع,الخصم,الإجمالي,الضريبة,ضريبة إضافية," +
                        "إجمالي الضريبة,الصافي,المستودع,الفرع,المندوب,المستخدم");

                    foreach (var item in InvoicesList)
                    {
                        writer.WriteLine(
                            $"{item.AutoIncrementID}," +
                            $"{item.InvoiceTypeTxt}," +
                            $"{item.InvoiceNo}," +
                            $"{item.InvDate:yyyy/MM/dd}," +
                            $"{item.InvoiceTime}," +
                            $"{item.PaymentTxt}," +
                            $"{item.Paid:N2}," +
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

                using (var writer = new System.Xml.XmlTextWriter(
                           StyleFile, System.Text.Encoding.UTF8))
                {
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
                }

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

        // ── زر العمليات ── قائمة سياق
        private void btnOperations_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            // تغيير طريقة الدفع
            var mnuChangePay = new MenuItem { Header = "💳 تغيير طريقة الدفع" };

            var mnuCash   = new MenuItem { Header = "💵 نقدي" };
            var mnuCredit = new MenuItem { Header = "📋 آجل" };
            var mnuVisa   = new MenuItem { Header = "💳 شبكة" };

            mnuCash.Click   += ToolMenuCash_Click;
            mnuCredit.Click += ToolMenuCredit_Click;
            mnuVisa.Click   += ToolMenuVisa_Click;

            mnuChangePay.Items.Add(mnuCash);
            mnuChangePay.Items.Add(mnuCredit);
            mnuChangePay.Items.Add(mnuVisa);

            // تغيير حساب
            var mnuChangeCust = new MenuItem { Header = "👥 تغيير حساب" };
            mnuChangeCust.Click += toolCustomers_Click;

            menu.Items.Add(mnuChangePay);
            menu.Items.Add(mnuChangeCust);
            menu.IsOpen = true;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region DataGrid Events
        // ══════════════════════════════════════════════

        private void GridControl2_SelectionChanged(
            object sender, SelectionChangedEventArgs e) { }

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
                    int    invType  = item.InvoiceType == 0
                                      ? (int)item.InvoiceType
                                      : Convert.ToInt32(item.InvoiceType);

                    if (invType == 2)
                    {
                        var form = new frmInvSale();
                        if (procType == 2) form.Title = "مرتجع مبيعات";
                        form.Show();
                        form.InvType     = 2;
                        form.ProcType    = procType;
                        form.WindowState = WindowState.Maximized;
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
                        form.WindowState = WindowState.Maximized;
                        form.Navigate(
                            $"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                            $"AND (inv_type=3 OR inv_type=20) " +
                            $"AND proc_type={procType} " +
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
        #region Operations (تغيير طريقة الدفع / الحساب)
        // ══════════════════════════════════════════════

        /// <summary>
        /// تحديث صف واحد أو مجموعة صفوف محددة
        /// </summary>
        private List<InvoiceTxt> GetSelectedItems()
        {
            return GridControl2.SelectedItems
                               .OfType<InvoiceTxt>()
                               .ToList();
        }

        // ── تحويل إلى نقدي
        private void ToolMenuCash_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                DXMessageBox.Show("لا توجد صفوف محددة.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                foreach (var item in selected)
                {
                    if (string.IsNullOrEmpty(item.InvGlobalID)) continue;

                    if (Convert.ToInt32(item.InvoiceType) == 3)
                    {
                        DXMessageBox.Show("لا يمكن تعديل فاتورة نقطة البيع.",
                                        "تنبيه", MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        continue;
                    }

                    var cmd = new SqlCommand(
                        "UPDATE inv SET pay_type=1, cash=InvTotal, " +
                        "visa=0, paid=InvTotal " +
                        "WHERE branch=@Branch AND InvGlobalID=@InvGlobalID",
                        conn);
                    cmd.Parameters.AddWithValue("@Branch", MainClass.BranchNo);
                    cmd.Parameters.AddWithValue("@InvGlobalID", item.InvGlobalID);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(item.InvGlobalID);
                }

                conn.Close();
                DXMessageBox.Show("تمت العملية بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                conn.Close();
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── تحويل إلى آجل
        private void ToolMenuCredit_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                DXMessageBox.Show("لا توجد صفوف محددة.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                foreach (var item in selected)
                {
                    if (string.IsNullOrEmpty(item.InvGlobalID)) continue;

                    // التحقق من العميل النقدي
                    var checkCmd = new SqlCommand(
                        "SELECT Cust_id FROM inv WHERE InvGlobalID=@InvGlobalID",
                        conn);
                    checkCmd.Parameters.AddWithValue("@InvGlobalID", item.InvGlobalID);
                    var checkDt = new DataTable();
                    new SqlDataAdapter(checkCmd).Fill(checkDt);

                    if (checkDt.Rows.Count > 0 &&
                        checkDt.Rows[0]["Cust_id"].ToString() == "1")
                    {
                        DXMessageBox.Show(
                            "لا يمكنك تحويل فاتورة آجلة على عميل نقدي.",
                            "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    var cmd = new SqlCommand(
                        "UPDATE inv SET pay_type=-1, cash=0, visa=0, " +
                        "paid=0, bank=0 WHERE InvGlobalID=@InvGlobalID",
                        conn);
                    cmd.Parameters.AddWithValue("@InvGlobalID", item.InvGlobalID);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(item.InvGlobalID);
                }

                conn.Close();
                DXMessageBox.Show("تمت العملية بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                conn.Close();
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── تحويل إلى شبكة (يُفتح dialog لاختيار البنك)
        private void ToolMenuVisa_Click(object sender, RoutedEventArgs e)
        {
            // تحميل قائمة البنوك وعرضها في نافذة اختيار
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                DXMessageBox.Show("لا توجد صفوف محددة.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                var bankDt = new DataTable();
                new SqlDataAdapter("SELECT id, name FROM Banks", conn)
                    .Fill(bankDt);
                conn.Close();

                if (bankDt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بنوك مسجلة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // نافذة اختيار البنك
                var dlg = new System.Windows.Window
                {
                    Title  = "اختر البنك",
                    Width  = 300,
                    Height = 200,
                    FlowDirection = System.Windows.FlowDirection.RightToLeft,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner  = this
                };

                var lb  = new ListBox { Margin = new Thickness(8) };
                var btn = new System.Windows.Controls.Button
                {
                    Content = "✅ تأكيد",
                    Margin  = new Thickness(8),
                    Height  = 34
                };

                foreach (DataRow r in bankDt.Rows)
                    lb.Items.Add(new { Id = r["id"], Name = r["name"].ToString() });

                lb.DisplayMemberPath = "Name";

                var sp = new StackPanel();
                sp.Children.Add(lb);
                sp.Children.Add(btn);
                dlg.Content = sp;

                btn.Click += (s, ev) =>
                {
                    if (lb.SelectedItem == null)
                    {
                        DXMessageBox.Show("يرجى اختيار بنك.", "تنبيه",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                        return;
                    }

                    dynamic sel = lb.SelectedItem;
                    int bankId = Convert.ToInt32(sel.Id);

                    try
                    {
                        if (conn.State == ConnectionState.Open) conn.Close();
                        conn.Open();

                        foreach (var item in selected)
                        {
                            if (string.IsNullOrEmpty(item.InvGlobalID)) continue;

                            if (Convert.ToInt32(item.InvoiceType) == 3)
                            {
                                DXMessageBox.Show(
                                    "لا يمكن تعديل فاتورة نقطة البيع.",
                                    "تنبيه", MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                continue;
                            }

                            var cmd = new SqlCommand(
                                "UPDATE inv SET pay_type=2, Bank=@Bank, " +
                                "cash=0, visa=InvTotal, paid=InvTotal " +
                                "WHERE InvGlobalID=@InvGlobalID",
                                conn);
                            cmd.Parameters.AddWithValue("@InvGlobalID",
                                item.InvGlobalID);
                            cmd.Parameters.AddWithValue("@Bank", bankId);
                            cmd.ExecuteNonQuery();

                            RegenerateEntry(item.InvGlobalID);
                        }

                        conn.Close();
                        DXMessageBox.Show("تمت العملية بنجاح.", "تم",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                        dlg.Close();
                    }
                    catch (Exception ex2)
                    {
                        conn.Close();
                        DXMessageBox.Show($"خطأ:\n{ex2.Message}", "خطأ",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── تغيير الحساب (العميل)
        private void toolCustomers_Click(object sender, RoutedEventArgs e)
        {
            addNewItem();
            Chang_Customer();
        }

        private void addNewItem()
        {
            try
            {
                var form = new frmSrchClient
                {
                    Background = Brushes.WhiteSmoke,
                    Type         = 1,
                    ChangeCust   = true,
                };
                form.lblName.Foreground = Brushes.Black;
                form.lblMobile.Foreground = Brushes.Black;
                form.ShowDialog();
                Client_id = form.ClientId.ToString();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Chang_Customer()
        {
            var selected = GetSelectedItems();
            if (selected.Count == 0)
            {
                DXMessageBox.Show("لا توجد صفوف محددة.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                foreach (var item in selected)
                {
                    if (string.IsNullOrEmpty(item.InvGlobalID)) continue;

                    // التحقق
                    var checkCmd = new SqlCommand(
                        "SELECT Cust_id, pay_type FROM inv " +
                        "WHERE InvGlobalID=@InvGlobalID", conn);
                    checkCmd.Parameters.AddWithValue("@InvGlobalID", item.InvGlobalID);
                    var checkDt = new DataTable();
                    new SqlDataAdapter(checkCmd).Fill(checkDt);

                    if (checkDt.Rows.Count > 0)
                    {
                        int payType = Convert.ToInt32(checkDt.Rows[0]["pay_type"]);
                        if (_Client_id == 1 && payType == -1)
                        {
                            DXMessageBox.Show(
                                "لا يمكنك تحويل فاتورة آجلة على عميل نقدي.",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue;
                        }
                    }

                    if (Convert.ToInt32(item.InvoiceType) == 3)
                    {
                        DXMessageBox.Show(
                            "لا يمكن تعديل فاتورة نقطة البيع.",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    var cmd = new SqlCommand(
                        "UPDATE inv SET Cust_id=@Cust_id " +
                        "WHERE InvGlobalID=@InvGlobalID", conn);
                    cmd.Parameters.AddWithValue("@InvGlobalID", item.InvGlobalID);
                    cmd.Parameters.AddWithValue("@Cust_id", _Client_id);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(item.InvGlobalID);
                }

                conn.Close();
                DXMessageBox.Show("تمت العملية بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                conn.Close();
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إعادة توليد القيد المحاسبي بعد التعديل
        /// </summary>
        private void RegenerateEntry(string invGlobalID)
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var inv         = invoiceOper.BindInvoByID(invGlobalID);
                var entryOper   = new EntryOper();

                if (!entryOper.SaveEnty(entryOper.BindInvoiceToEntry(inv)))
                {
                    string msg = string.Equals(MainClass.Language, "en",
                                               StringComparison.OrdinalIgnoreCase)
                                  ? "Error in saving"
                                  : "خطأ أثناء إعادة التوليد";
                    DXMessageBox.Show(msg, "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في القيد:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
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

                RptUrl = MainClass.ReportsPath;
                defPrinter = MainClass.ReportsPrinter;
                RptName = "rptInvSumByClient.repx";

                // ══════════════════════════════════════════════════════════════
                // الحل: إنشاء GridControl و GridView وهمي (WinForms) لتمريره للطباعة
                // ══════════════════════════════════════════════════════════════
                using (var tempGrid = new DevExpress.XtraGrid.GridControl())
                {
                    var tempView = new DevExpress.XtraGrid.Views.Grid.GridView(tempGrid);
                    tempGrid.MainView = tempView;
                    tempGrid.ViewCollection.Add(tempView);

                    // ربط البيانات بالشبكة الوهمية لتوليد الأعمدة
                    tempGrid.DataSource = InvoicesList.ToList();
                    tempGrid.ForceInitialize();
                    tempView.PopulateColumns();

                    // الآن نمرر الشبكة الوهمية لدالة الطباعة القديمة
                    var report = new Report();
                    report.Printing(
                        printType,
                        report.BindToData(tempView,
                                          Title,
                                          txtFromDate.DateTime.ToShortDateString(),
                                          txtToDate.DateTime.ToShortDateString()),
                        RptUrl, RptName, defPrinter, 1);
                }
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