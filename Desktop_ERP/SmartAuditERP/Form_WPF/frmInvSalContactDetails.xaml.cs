using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Globalization;
using System.Text;
using Microsoft.Win32;
using System.Windows.Documents;
using System.Windows.Media;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvSalContactDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields

        private SqlConnection _conn;

        public int Invtype = 0;
        private int _procType = 1;
        private int _invType = 0;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType;
        private int _printNo = 1;
        private string _defPrinter = string.Empty;
        private string _rptName = string.Empty;
        private string _rptUrl = string.Empty;
        public int _Client_id;

        private double _defVAT;
        private bool _priceIncVAT;

        private List<InvoiceTxt> _invoicesList;
        private InvoiceObj _invObj;

        private string _styleFile;
        private string _styleFolder;

        // Totals (kept from original)
        private double nTotal, nTotal1;
        private double nVAT, nVAT1;
        private double nNet, nNet1;
        private double sum, sum1;
        private double Discount, Discount1;
        private double Cash, Cash1;
        private double PayNetwork, PayNetwork1;
        private double _ExtraTax, _ExtraTax1;
        private double _TotalTax1, _TotalTax2;

        #endregion

        // ══════════════════════════════════════════════
        #region Property

        public string Client_id => _Client_id.ToString();

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor

        public frmInvSalContactDetails()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _invObj = new InvoiceObj(2, 1);
            _styleFile = Path.Combine(MainClass.ReportsPath, @"Styles\RptInvSalesDetailsLayout.xml");
            _styleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
        }

        #endregion

        #region Grid Helpers

        private int GetInvoicesCount()
        {
            return _invoicesList?.Count ?? 0;
        }

        private List<InvoiceTxt> GetSelectedInvoices()
        {
            var selectedInvoices = new List<InvoiceTxt>();

            try
            {
                int[] selectedRowHandles = GridView2.GetSelectedRowHandles();

                if (selectedRowHandles == null || selectedRowHandles.Length == 0)
                    return selectedInvoices;

                foreach (int rowHandle in selectedRowHandles)
                {
                    object rowObject = GetRowObjectByHandle(rowHandle);
                    if (rowObject is InvoiceTxt invoice)
                        selectedInvoices.Add(invoice);
                }
            }
            catch
            {
                // تجاهل أي خطأ توافق داخلي
            }

            return selectedInvoices;
        }

        private object GetRowObjectByHandle(int rowHandle)
        {
            try
            {
                var gridControlType = GridControl2.GetType();
                var getRowMethod = gridControlType.GetMethod("GetRow", new[] { typeof(int) });
                if (getRowMethod != null)
                    return getRowMethod.Invoke(GridControl2, new object[] { rowHandle });
            }
            catch
            {
            }

            try
            {
                var tableViewType = GridView2.GetType();
                var getRowMethod = tableViewType.GetMethod("GetRow", new[] { typeof(int) });
                if (getRowMethod != null)
                    return getRowMethod.Invoke(GridView2, new object[] { rowHandle });
            }
            catch
            {
            }

            return null;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckOperationsMenuVisibility();

            // تهيئة التواريخ
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtStartTime.Text = "00:00:00";
            txtEndTime.Text = "23:59:00";

            // تحميل قوائم الدفع ونوع الفاتورة
            LoadPaymentTypes();

            // تحميل البيانات
            LoadStores();
            LoadEmps();
            LoadCustomers();
            LoadSalesMen();
            LoadSettings();
            LoadBranches();
            ApplyBranchPermissions();
            LoadBanksMenu();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Data Loading

        private void CheckOperationsMenuVisibility()
        {
            try
            {
                using var da = new SqlDataAdapter(
                    "SELECT OperInvSale FROM SettingGeneral WHERE Inv_Id=2", _conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0 || dt.Rows[0]["OperInvSale"] == DBNull.Value)
                    menuOperations.Visibility = Visibility.Collapsed;
            }
            catch { /* silent */ }
        }

        private void LoadPaymentTypes()
        {
            bool isArabic = string.Equals(MainClass.Language, "ar",
                                          StringComparison.OrdinalIgnoreCase);
            cmbType.Items.Clear();
            if (isArabic)
            {
                cmbType.Items.Add("نقدية");
                cmbType.Items.Add("آجلة");
                cmbType.Items.Add("شبكة");
                cmbType.Items.Add("بنك");
            }
            else
            {
                cmbType.Items.Add("Cash");
                cmbType.Items.Add("Postpone");
                cmbType.Items.Add("ATM");
                cmbType.Items.Add("Bank");
            }
        }

        private void LoadEmps()
        {
            try
            {
                using var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id", _conn);
                var dt = new DataTable();
                da.Fill(dt);
                cmbusers.ItemsSource = dt.DefaultView;
                cmbusers.DisplayMemberPath = "name";
                cmbusers.SelectedValuePath = "id";
                cmbusers.SelectedIndex = -1;
            }
            catch { /* silent */ }
        }

        public void LoadSalesMen()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbSaleman.ItemsSource = dt.DefaultView;
            cmbSaleman.DisplayMemberPath = "name";
            cmbSaleman.SelectedValuePath = "id";
            cmbSaleman.SelectedIndex = -1;
        }

        private void LoadBranches()
        {
            string branchFilter = string.Empty;

            if (MainClass.BranchNo != -1
                && !string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                && Accounting.BranchCondition.Trim() != " ")
            {
                branchFilter = $" AND BranchId={MainClass.BranchNo}";
            }

            using var da = new SqlDataAdapter(
                $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0{branchFilter}", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbBranches.ItemsSource = dt.DefaultView;
            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath = "BranchId";
            cmbBranches.SelectedIndex = -1;
        }

        private void ApplyBranchPermissions()
        {
            bool hasBranchRestriction =
                !string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                && Accounting.BranchCondition.Trim() != " ";

            if (hasBranchRestriction)
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.SelectedIndex = 0;
            }
        }

        private void LoadStores()
        {
            var dt = LoadData.Invertories(MainClass.EmpNo);
            cmbStore_Loaded(dt); // helper below
        }

        /// <summary>
        /// مساعد: تعبئة combobox المستودع (لا يوجد في XAML هنا لأنه مخفي أصلاً)
        /// </summary>
        private void cmbStore_Loaded(DataTable dt)
        {
            // المستودع مخفي في الأصل — نحتفظ بالبيانات للاستخدام في الاستعلام
            _storeDataTable = dt;
        }

        private DataTable _storeDataTable;

        private void LoadCustomers()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Customers WHERE (type=1 OR type=3) ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbClients.ItemsSource = dt.DefaultView;
            cmbClients.DisplayMemberPath = "name";
            cmbClients.SelectedValuePath = "id";
            cmbClients.SelectedIndex = -1;
        }

        private void LoadSettings()
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
                        _printType = Convert.ToInt32(dt.Rows[0]["printType"]);
                        _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        _defPrinter = dt.Rows[0]["CasherPrinter"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();

                        _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                    }
                }

                using (var da = new SqlDataAdapter(
                           "SELECT * FROM SettingGeneral WHERE Inv_Id=23", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        _priceIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                        _defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadBanksMenu()
        {
            menuVisa.Items.Clear();

            EnsureConnectionOpen();

            using var da = new SqlDataAdapter("SELECT id, name FROM Banks", _conn);
            var dt = new DataTable();
            da.Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                var item = new MenuItem
                {
                    Header = row["name"].ToString(),
                    Tag = Convert.ToInt32(row["id"])
                };
                item.Click += BankMenuItem_Click;
                menuVisa.Items.Add(item);
            }

            EnsureConnectionClosed();
        }

        public void LoadCashStocksMenu()
        {
            menuCash.Items.Clear();

            EnsureConnectionOpen();

            using var da = new SqlDataAdapter("SELECT id, name FROM Stocks", _conn);
            var dt = new DataTable();
            da.Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                var item = new MenuItem
                {
                    Header = row["name"].ToString(),
                    Tag = Convert.ToInt32(row["id"])
                };
                item.Click += CashStockMenuItem_Click;
                menuCash.Items.Add(item);
            }

            EnsureConnectionClosed();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Show Invoices

        public void showInvoice()
        {
            try
            {
                GridControl2.ItemsSource = null;
                _invoicesList?.Clear();

                string whereClause = BuildWhereClause(out DateTime dateFrom, out DateTime dateTo);

                string sql = $@"
                    SELECT 
                        inv.InvGlobalID, inv.EntryID, inv.inv_type, inv.proc_type,
                        inv.pay_type, inv.branch, inv.date, inv.id, inv.AdditionalCost,
                        inv.InvoiceStatus, inv.stock, inv.safe, inv.tax,
                        ISNULL(inv.ExtraVAT,0) AS ExtraVAT,
                        inv.minus, inv.paid, inv.tot_net,
                        inv.Reff_No, inv.Reff_date,
                        inv.cash, inv.visa, inv.bank,
                        inv.cust_id, inv.sales_emp, inv.InvTotal, inv.PaymentStatus,
                        (SELECT ISNULL(SUM(val1 * exchange_price),0)
                         FROM InvContratct_Sub
                         WHERE InvGlobalID=inv.InvGlobalID) AS sumPrice,
                        (SELECT ISNULL(SUM(discount),0)
                         FROM InvContratct_Sub
                         WHERE InvGlobalID=inv.InvGlobalID) AS ItemDiscount,
                        (SELECT ISNULL(SUM(CASE WHEN taxval=0 THEN (val1*exchange_price) ELSE 0 END),0)
                         FROM InvContratct_Sub
                         WHERE InvGlobalID=inv.InvGlobalID) AS FreeVATSales,
                        Customers.name       AS CustName,
                        Users.username       AS username,
                        Branches.name        AS BranchName,
                        salesmen.name        AS SalesmanTxt
                    FROM InvContratct inv
                    LEFT JOIN Customers ON inv.cust_id  = Customers.id
                    LEFT JOIN Users     ON inv.sales_emp= Users.emp
                    LEFT JOIN Branches  ON inv.branch   = Branches.BranchId
                    LEFT JOIN salesmen  ON inv.salesman = salesmen.id
                    WHERE {whereClause} Inv.IS_Deleted=0
                    ORDER BY date DESC";

                _invoicesList = BindInvoiceList(sql, dateFrom, dateTo);
                GridControl2.ItemsSource = _invoicesList;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildWhereClause(out DateTime dateFrom, out DateTime dateTo)
        {
            string clause = string.Empty;

            // الفرع
            if (cmbBranches.SelectedIndex > -1 && cmbBranches.SelectedValue != null)
                clause += $"inv.branch={cmbBranches.SelectedValue} AND ";

            // العميل
            if (ckAllClients.IsChecked != true && cmbClients.SelectedIndex > -1)
                clause += $"cust_id={cmbClients.SelectedValue} AND ";

            // المستخدم
            if (ckAllUsers.IsChecked != true && cmbusers.SelectedIndex > -1)
                clause += $"sales_emp={cmbusers.SelectedValue} AND ";

            // المندوب
            if (cbAllSalesmen.IsChecked != true && cmbSaleman.SelectedIndex > -1)
                clause += $"salesman={cmbSaleman.SelectedValue} AND ";

            // نوع الإجراء
            clause += "(inv.proc_type=1 OR inv.proc_type=2) AND ";

            // حالة الدفع
            if (ckAllpaids.IsChecked != true)
            {
                if (rbpaidinvo.IsChecked == true) clause += "inv.PaymentStatus=1 AND ";
                else if (rbunpaidinvo.IsChecked == true) clause += "inv.PaymentStatus=0 AND ";
                else if (rbpartialpaidinvo.IsChecked == true) clause += "inv.PaymentStatus=2 AND ";
            }

            // نوع الفاتورة
            clause += "(inv.inv_type=2 OR inv.inv_type=3 OR inv.inv_type=20 OR inv.inv_type=23) AND ";

            // تحليل الوقت
            DateTime fromDate = txtFromDate.SelectedDate ?? DateTime.Today;
            DateTime toDate = txtToDate.SelectedDate ?? DateTime.Today;

            if (!TimeSpan.TryParse(txtStartTime.Text, out TimeSpan startTs))
                startTs = TimeSpan.Zero;
            if (!TimeSpan.TryParse(txtEndTime.Text, out TimeSpan endTs))
                endTs = new TimeSpan(23, 59, 0);

            dateFrom = fromDate.Date + startTs;
            dateTo = toDate.Date + endTs;

            if (ckTotalPeriod.IsChecked != true)
                clause += "date BETWEEN @date1 AND @date2 AND ";

            return clause;
        }

        public List<InvoiceTxt> BindInvoiceList(string sql, DateTime date1, DateTime date2)
        {
            var result = new List<InvoiceTxt>();
            var connection = MainClass.ConnObj();

            if (connection.State != ConnectionState.Open)
                connection.Open();

            try
            {
                using var da = new SqlDataAdapter(sql, connection);
                da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = date1;
                da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = date2;

                var dt = new DataTable();
                da.Fill(dt);

                // ─── Update progress bars ───
                Dispatcher.Invoke(() =>
                {
                    progressBarTop.Maximum = dt.Rows.Count;
                    progressBarTop.Value = 0;
                    progressBarBottom.Maximum = dt.Rows.Count;
                    progressBarBottom.Value = 0;
                });

                bool isArabic = string.Equals(MainClass.Language, "ar",
                                              StringComparison.OrdinalIgnoreCase);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    try
                    {
                        DataRow row = dt.Rows[i];

                        int invTypeId = Convert.ToInt32(row["inv_type"]);
                        int procTypeId = Convert.ToInt32(row["proc_type"]);
                        int payTypeId = Convert.ToInt32(row["pay_type"]);

                        // Determine customer tax type
                        short taxType = GetCustomerTaxType(connection, row["cust_id"]);

                        string invoiceTypeName = InvoiceOper.GetInvoiceType(
                            invTypeId, procTypeId, payTypeId, taxType);

                        // Build InvoiceTxt
                        var inv = new InvoiceTxt
                        {
                            ISNew = false,
                            IsPrinted = true,
                            IsLoaded = false,
                            InvGlobalID = row["InvGlobalID"].ToString(),
                            EntryGlobalID = $"{row["branch"]}-{row["EntryID"]}",
                            ProcType = procTypeId,
                            InvoiceNo = Convert.ToInt32(row["id"]),
                            InvDate = Convert.ToDateTime(row["date"]),
                            InvoiceType = (InvoiceType)invTypeId,
                            InvoiceTypeTxt = invoiceTypeName,
                            AdditionalCost = Convert.ToDecimal(row["AdditionalCost"]),
                            InvoiceStatus = Convert.ToInt32(row["InvoiceStatus"]),
                            Treasury = Convert.ToInt32(row["stock"].ToString()),
                            Store = Convert.ToInt32(row["safe"].ToString()),
                            InvDiscount = Convert.ToDouble(row["minus"].ToString()),
                            AutoIncrementID = i + 1,
                            ReffNo = row["Reff_No"].ToString(),
                            PayType = payTypeId,
                            Paycash = Convert.ToDouble(row["cash"].ToString()),
                            PayATM = Convert.ToDouble(row["visa"].ToString()),
                            Bank = Convert.ToInt32(row["bank"]),
                            Customer = Convert.ToInt32(row["cust_id"]),
                            ClientTxt = row["CustName"] == DBNull.Value
                                                ? string.Empty : row["CustName"].ToString(),
                            SalesmanTxt = row["SalesmanTxt"].ToString(),
                            User = Convert.ToInt32(row["sales_emp"].ToString()),
                            UserTxt = row["username"] == DBNull.Value
                                                ? string.Empty : row["username"].ToString(),
                            InvertoryName = Common.GetStoreName(Convert.ToInt32(row["safe"])),
                            BranchTxt = row["BranchName"].ToString(),
                            SumPrice = Convert.ToDouble(row["sumPrice"]),
                            FreeVATSales = Convert.ToDouble(row["FreeVATSales"]),
                            Total = Convert.ToDouble(row["InvTotal"]),
                            Net = Convert.ToDouble(row["tot_net"]),
                            Paid = Convert.ToDouble(row["paid"].ToString()),
                        };

                        // وقت الفاتورة
                        inv.InvTime = Convert.ToDateTime(inv.InvDate.ToString("hh:mm:ss tt"));
                        inv.InvoiceTime = inv.InvDate.ToString("hh:mm:ss tt");

                        // الضريبة
                        double totalTax = Convert.ToDouble(row["tax"]);
                        double extraVAT = Convert.ToDouble(row["ExtraVAT"]);
                        inv.ExtraVAT = extraVAT;
                        inv.VAT = totalTax - extraVAT;
                        if (inv.VAT <= 0) { inv.VAT = totalTax; }
                        inv.TotalTax = inv.VAT + inv.ExtraVAT;

                        // الباقي
                        inv.Remainder = inv.Paid - inv.Net;

                        // الخصم
                        inv.TotDiscount = inv.InvDiscount + Convert.ToDouble(row["ItemDiscount"]);

                        // نوع الدفع نصي
                        inv.PaymentTxt = ResolvePaymentText(payTypeId, isArabic);

                        // إذا كانت نقدي ومدفوع بالكامل
                        if (payTypeId == 1 && Convert.ToInt32(row["PaymentStatus"]) == 1)
                            inv.Paycash = inv.Net;

                        // تاريخ المرجع
                        try { inv.RefDate = Convert.ToDateTime(row["Reff_date"].ToString()); }
                        catch { /* قد يكون فارغاً */ }

                        // VATPerc
                        var invObj2 = new InvoiceObj(invTypeId, procTypeId);
                        inv.VATperc = invObj2.VAT;
                        inv.ExtraVATPerc = invObj2.AdditionalTax;
                        inv.PriceIncVAT = invObj2.PriceIncVAT;

                        result.Add(inv);

                        // تحديث شريط التقدم
                        int currentIndex = i + 1;
                        Dispatcher.Invoke(() =>
                        {
                            progressBarTop.Value = currentIndex;
                            progressBarBottom.Value = currentIndex;
                        });
                    }
                    catch { /* تجاوز الصف المعطوب */ }
                }
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return result;
        }

        private static short GetCustomerTaxType(SqlConnection conn, object custId)
        {
            try
            {
                using var cmd = new SqlCommand(
                    $"SELECT ISNULL(tax_no,'') AS tax_no FROM customers WHERE id={custId}", conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string taxNo = reader["tax_no"].ToString();
                    if (string.IsNullOrWhiteSpace(taxNo)) return 1;
                    if (!double.TryParse(taxNo, out double taxNum)) return 2;
                    return taxNum <= 0 ? (short)1 : (short)2;
                }
            }
            catch { }
            return 1;
        }

        private static string ResolvePaymentText(int payType, bool isArabic)
        {
            return payType switch
            {
                -1 => isArabic ? "آجل" : "Credit",
                1 => isArabic ? "نقدي" : "Cash",
                2 => isArabic ? "شبكة" : "Card",
                4 => isArabic ? "متعدد" : "Multi",
                5 => isArabic ? "ضيافة" : "Guest",
                _ => string.Empty
            };
        }

        #endregion

        #region Export Helpers

        private void ExportInvoicesToCsv(string filePath)
        {
            var lines = new List<string>();

            lines.Add(string.Join(",",
                EscapeCsv("م"),
                EscapeCsv("نوع الفاتورة"),
                EscapeCsv("رقم الفاتورة"),
                EscapeCsv("رقم المرجع"),
                EscapeCsv("تاريخ الفاتورة"),
                EscapeCsv("الوقت"),
                EscapeCsv("نوع الدفع"),
                EscapeCsv("العميل"),
                EscapeCsv("نقدي"),
                EscapeCsv("شبكة"),
                EscapeCsv("المجموع"),
                EscapeCsv("الخصم"),
                EscapeCsv("الإجمالي"),
                EscapeCsv("الضريبة"),
                EscapeCsv("الضريبة الإضافية"),
                EscapeCsv("إجمالي الضريبة"),
                EscapeCsv("الصافي"),
                EscapeCsv("المدفوع"),
                EscapeCsv("الباقي"),
                EscapeCsv("المستودع"),
                EscapeCsv("الفرع"),
                EscapeCsv("المندوب"),
                EscapeCsv("المستخدم")
            ));

            if (_invoicesList != null)
            {
                foreach (InvoiceTxt invoice in _invoicesList)
                {
                    lines.Add(string.Join(",",
                        EscapeCsv(invoice.AutoIncrementID.ToString()),
                        EscapeCsv(invoice.InvoiceTypeTxt),
                        EscapeCsv(invoice.InvoiceNo.ToString()),
                        EscapeCsv(invoice.ReffNo),
                        EscapeCsv(invoice.InvDate.ToString("yyyy-MM-dd")),
                        EscapeCsv(invoice.InvoiceTime),
                        EscapeCsv(invoice.PaymentTxt),
                        EscapeCsv(invoice.ClientTxt),
                        EscapeCsv(invoice.Paycash.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.PayATM.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.SumPrice.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.TotDiscount.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.Total.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.VAT.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.ExtraVAT.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.TotalTax.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.Net.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.Paid.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.Remainder.ToString("0.##", CultureInfo.InvariantCulture)),
                        EscapeCsv(invoice.InvertoryName),
                        EscapeCsv(invoice.BranchTxt),
                        EscapeCsv(invoice.SalesmanTxt),
                        EscapeCsv(invoice.UserTxt)
                    ));
                }
            }

            File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
        }

        private string EscapeCsv(string value)
        {
            if (value == null)
                return "\"\"";

            string cleanValue = value.Replace("\"", "\"\"");
            return $"\"{cleanValue}\"";
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Events

        private void Button1_Click(object sender, RoutedEventArgs e)
            => showInvoice();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GetInvoicesCount() == 0)
                {
                    ShowInfo("لا توجد بيانات للتصدير.");
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "تصدير البيانات",
                    Filter = "CSV File (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"{Title}.csv"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                ExportInvoicesToCsv(saveFileDialog.FileName);

                Process.Start(new ProcessStartInfo(saveFileDialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnSaveLayout_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_styleFolder))
                Directory.CreateDirectory(_styleFolder);

            GridControl2.SaveLayoutToXml(_styleFile);
            DXMessageBox.Show("تم حفظ مظهر الجدول بنجاح ✅", "حفظ المظهر",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Grid Cell Buttons

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = (sender as Button)?.Tag as InvoiceTxt;
                if (row == null)
                    return;

                var detailForm = new frmInvoiceDetails
                {
                    ISProfit = false,
                    InvGlobalId = row.InvGlobalID
                };

                detailForm.btnRecalculateCost.Visibility = Visibility.Collapsed;
                MainClass.ApplyPermissionToForm(detailForm);
                MainClass.DoApplyUserSett(detailForm);
                detailForm.ShowDialog();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnShowInv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var row = (sender as Button)?.Tag as InvoiceTxt;
                if (row == null)
                    return;

                string globalId = row.InvGlobalID;
                int procType = row.ProcType;
                int invType = (int)row.InvoiceType;

                if (invType == 23)
                {
                    var contractForm = new frmcontract();

                    if (procType == 2)
                        contractForm.Title = "مرتجع مبيعات";

                    contractForm.InvType = 2;
                    contractForm.ProcType = procType;
                    contractForm.WindowState = WindowState.Maximized;
                    contractForm.Show();

                    contractForm.Navigate(
                        $"SELECT * FROM InvContratct WHERE IS_Deleted=0 " +
                        $"AND inv_type=23 AND proc_type={procType} " +
                        $"AND InvGlobalID=N'{globalId}'");

                    contractForm.Activate();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Menu Events

        private void menuCash_Click(object sender, RoutedEventArgs e)
            => LoadCashStocksMenu();

        private void menuCredit_Click(object sender, RoutedEventArgs e)
            => ChangePaymentTypeToCredit();

        private void menuChangeCustomer_Click(object sender, RoutedEventArgs e)
        {
            SelectNewCustomer();
            ChangeCustomerForSelectedRows();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Bank Menu (شبكة)

        private void BankMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;
            int bankId = Convert.ToInt32(item.Tag);
            ChangePaymentTypeToBank(bankId);
        }

        private void ChangePaymentTypeToBank(int bankId)
        {
            try
            {
                EnsureConnectionOpen();

                List<InvoiceTxt> selectedInvoices = GetSelectedInvoices();
                if (selectedInvoices.Count == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                foreach (InvoiceTxt row in selectedInvoices)
                {
                    string globalId = row.InvGlobalID;
                    if (string.IsNullOrWhiteSpace(globalId))
                        continue;

                    if ((int)row.InvoiceType == 3)
                    {
                        ShowWarning("لا يمكن تعديل فاتورة نقطة البيع");
                        return;
                    }

                    EnsureConnectionOpen();

                    using var cmd = new SqlCommand(
                        "UPDATE inv SET pay_type=2, Bank=@Bank, cash=0, visa=InvTotal, paid=InvTotal " +
                        "WHERE InvGlobalID=@InvGlobalID", _conn);

                    cmd.Parameters.AddWithValue("@InvGlobalID", globalId);
                    cmd.Parameters.AddWithValue("@Bank", bankId);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(globalId);
                }

                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Cash Stocks Menu (نقدي)

        private void CashStockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;
            int stockId = Convert.ToInt32(item.Tag);
            ChangePaymentTypeToCash(stockId);
        }

        private void ChangePaymentTypeToCash(int stockId)
        {
            try
            {
                EnsureConnectionOpen();

                List<InvoiceTxt> selectedInvoices = GetSelectedInvoices();
                if (selectedInvoices.Count == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                foreach (InvoiceTxt row in selectedInvoices)
                {
                    string globalId = row.InvGlobalID;
                    if (string.IsNullOrWhiteSpace(globalId))
                        continue;

                    if ((int)row.InvoiceType == 3)
                    {
                        ShowWarning("لا يمكن تعديل فاتورة نقطة البيع");
                        return;
                    }

                    EnsureConnectionOpen();

                    using var cmd = new SqlCommand(
                        $"UPDATE InvContratct SET pay_type=1, cash=InvTotal, visa=0, paid=InvTotal " +
                        $"WHERE branch={MainClass.BranchNo} AND InvGlobalID=@InvGlobalID", _conn);

                    cmd.Parameters.AddWithValue("@InvGlobalID", globalId);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(globalId);
                }

                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Credit (آجل)

        private void ChangePaymentTypeToCredit()
        {
            try
            {
                EnsureConnectionOpen();

                List<InvoiceTxt> selectedInvoices = GetSelectedInvoices();
                if (selectedInvoices.Count == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                foreach (InvoiceTxt row in selectedInvoices)
                {
                    string globalId = row.InvGlobalID;
                    if (string.IsNullOrWhiteSpace(globalId))
                        continue;

                    using var checkCmd = new SqlCommand(
                        "SELECT Cust_id FROM InvContratct WHERE InvGlobalID=@InvGlobalID", _conn);

                    checkCmd.Parameters.AddWithValue("@InvGlobalID", globalId);

                    var checkDt = new DataTable();
                    new SqlDataAdapter(checkCmd).Fill(checkDt);

                    if (checkDt.Rows.Count > 0 &&
                        checkDt.Rows[0]["Cust_id"].ToString() == "1")
                    {
                        ShowError("لا يمكنك تحويل فاتورة آجلة على عميل نقدي");
                        continue;
                    }

                    EnsureConnectionOpen();

                    using var cmd = new SqlCommand(
                        "UPDATE InvContratct SET pay_type=-1, cash=0, visa=0, paid=0, bank=0 " +
                        "WHERE InvGlobalID=@InvGlobalID", _conn);

                    cmd.Parameters.AddWithValue("@InvGlobalID", globalId);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(globalId);
                }

                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Change Customer

        private void SelectNewCustomer()
        {
            var searchForm = new frmSrchClient
            {
                Type = 1,
                ChangeCust = true
            };
            searchForm.ShowDialog();
            _Client_id = searchForm.ClientId;
        }

        public void ChangeCustomerForSelectedRows()
        {
            try
            {
                EnsureConnectionOpen();

                List<InvoiceTxt> selectedInvoices = GetSelectedInvoices();
                if (selectedInvoices.Count == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                foreach (InvoiceTxt row in selectedInvoices)
                {
                    string globalId = row.InvGlobalID;
                    if (string.IsNullOrWhiteSpace(globalId))
                        continue;

                    using var checkCmd = new SqlCommand(
                        "SELECT Cust_id, pay_type FROM InvContratct WHERE InvGlobalID=@InvGlobalID",
                        _conn);

                    checkCmd.Parameters.AddWithValue("@InvGlobalID", globalId);

                    var checkDt = new DataTable();
                    new SqlDataAdapter(checkCmd).Fill(checkDt);

                    if (checkDt.Rows.Count > 0 &&
                        _Client_id == 1 &&
                        Convert.ToInt32(checkDt.Rows[0]["pay_type"]) == -1)
                    {
                        ShowError("لا يمكنك تحويل فاتورة آجلة على عميل نقدي");
                        return;
                    }

                    if ((int)row.InvoiceType == 3)
                    {
                        ShowWarning("لا يمكن تعديل فاتورة نقطة البيع");
                        return;
                    }

                    EnsureConnectionOpen();

                    using var cmd = new SqlCommand(
                        "UPDATE InvContratct SET Cust_id=@Cust_id WHERE InvGlobalID=@InvGlobalID",
                        _conn);

                    cmd.Parameters.AddWithValue("@InvGlobalID", globalId);
                    cmd.Parameters.AddWithValue("@Cust_id", _Client_id);
                    cmd.ExecuteNonQuery();

                    RegenerateEntry(globalId);
                }

                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print / Report

        private void PrintReport(int printMode)
        {
            _rptUrl = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName = "rptInvSumByClient.repx";

            if (GetInvoicesCount() == 0)
            {
                ShowInfo("لا توجد عمليات بالجدول");
                return;
            }

            // محاولة استخدام كلاس Report إذا كان عنده overload متوافق مع DataTable
            try
            {
                var report = new Report();
                string fromDateStr = (txtFromDate.SelectedDate ?? DateTime.Today).ToShortDateString();
                string toDateStr = (txtToDate.SelectedDate ?? DateTime.Today).ToShortDateString();

                DataTable reportDataTable = BuildReportDataTable();

                var bindMethod = report.GetType()
                    .GetMethods()
                    .FirstOrDefault(method =>
                    {
                        if (method.Name != "BindToData")
                            return false;

                        var parameters = method.GetParameters();
                        if (parameters.Length != 4)
                            return false;

                        return parameters[1].ParameterType == typeof(string)
                               && parameters[2].ParameterType == typeof(string)
                               && parameters[3].ParameterType == typeof(string)
                               && parameters[0].ParameterType.FullName != "DevExpress.XtraGrid.Views.Grid.GridView";
                    });

                if (bindMethod != null)
                {
                    object bindResult = bindMethod.Invoke(report, new object[]
                    {
                reportDataTable,
                Title,
                fromDateStr,
                toDateStr
                    });

                    var printingMethod = report.GetType().GetMethod("Printing");
                    if (printingMethod != null)
                    {
                        printingMethod.Invoke(report, new object[]
                        {
                    printMode,
                    bindResult,
                    _rptUrl,
                    _rptName,
                    _defPrinter,
                    1
                        });

                        return;
                    }
                }
            }
            catch
            {
                // إذا لم يوجد overload مناسب، ننتقل للبديل المحلي
            }

            // بديل WPF عملي وآمن
            PrintInvoicesWithWpfDocument(printMode);
        }

        #endregion

        #region Print Helpers

        private DataTable BuildReportDataTable()
        {
            var dt = new DataTable();

            dt.Columns.Add("AutoIncrementID", typeof(int));
            dt.Columns.Add("InvoiceTypeTxt", typeof(string));
            dt.Columns.Add("InvoiceNo", typeof(int));
            dt.Columns.Add("ReffNo", typeof(string));
            dt.Columns.Add("InvDate", typeof(string));
            dt.Columns.Add("InvoiceTime", typeof(string));
            dt.Columns.Add("PaymentTxt", typeof(string));
            dt.Columns.Add("ClientTxt", typeof(string));
            dt.Columns.Add("Paycash", typeof(double));
            dt.Columns.Add("PayATM", typeof(double));
            dt.Columns.Add("SumPrice", typeof(double));
            dt.Columns.Add("TotDiscount", typeof(double));
            dt.Columns.Add("Total", typeof(double));
            dt.Columns.Add("VAT", typeof(double));
            dt.Columns.Add("ExtraVAT", typeof(double));
            dt.Columns.Add("TotalTax", typeof(double));
            dt.Columns.Add("Net", typeof(double));
            dt.Columns.Add("Paid", typeof(double));
            dt.Columns.Add("Remainder", typeof(double));
            dt.Columns.Add("InvertoryName", typeof(string));
            dt.Columns.Add("BranchTxt", typeof(string));
            dt.Columns.Add("SalesmanTxt", typeof(string));
            dt.Columns.Add("UserTxt", typeof(string));

            if (_invoicesList != null)
            {
                foreach (InvoiceTxt invoice in _invoicesList)
                {
                    dt.Rows.Add(
                        invoice.AutoIncrementID,
                        invoice.InvoiceTypeTxt,
                        invoice.InvoiceNo,
                        invoice.ReffNo,
                        invoice.InvDate.ToString("yyyy-MM-dd"),
                        invoice.InvoiceTime,
                        invoice.PaymentTxt,
                        invoice.ClientTxt,
                        invoice.Paycash,
                        invoice.PayATM,
                        invoice.SumPrice,
                        invoice.TotDiscount,
                        invoice.Total,
                        invoice.VAT,
                        invoice.ExtraVAT,
                        invoice.TotalTax,
                        invoice.Net,
                        invoice.Paid,
                        invoice.Remainder,
                        invoice.InvertoryName,
                        invoice.BranchTxt,
                        invoice.SalesmanTxt,
                        invoice.UserTxt
                    );
                }
            }

            return dt;
        }

        private void PrintInvoicesWithWpfDocument(int printMode)
        {
            FlowDocument document = BuildInvoiceFlowDocument();

            if (printMode == 1)
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    document.PageWidth = printDialog.PrintableAreaWidth;
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PagePadding = new Thickness(25);
                    document.ColumnWidth = printDialog.PrintableAreaWidth;

                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, Title);
                }
            }
            else
            {
                var previewWindow = new Window
                {
                    Title = $"معاينة - {Title}",
                    WindowState = WindowState.Maximized,
                    FlowDirection = FlowDirection.RightToLeft,
                    Background = Brushes.White
                };

                var viewer = new FlowDocumentScrollViewer
                {
                    Document = document,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                previewWindow.Content = viewer;
                previewWindow.ShowDialog();
            }
        }

        private FlowDocument BuildInvoiceFlowDocument()
        {
            var document = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Cairo"),
                FontSize = 12,
                PagePadding = new Thickness(25)
            };

            string fromDate = (txtFromDate.SelectedDate ?? DateTime.Today).ToShortDateString();
            string toDate = (txtToDate.SelectedDate ?? DateTime.Today).ToShortDateString();

            document.Blocks.Add(new Paragraph(new Run(Title))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.DarkBlue
            });

            document.Blocks.Add(new Paragraph(new Run($"الفترة: من {fromDate} إلى {toDate}"))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            });

            var table = new Table
            {
                CellSpacing = 0
            };

            document.Blocks.Add(table);

            for (int index = 0; index < 9; index++)
                table.Columns.Add(new TableColumn());

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerRow = new TableRow
            {
                Background = new SolidColorBrush(Color.FromRgb(33, 58, 122))
            };

            rowGroup.Rows.Add(headerRow);

            AddTableCell(headerRow, "م", true);
            AddTableCell(headerRow, "نوع الفاتورة", true);
            AddTableCell(headerRow, "رقم الفاتورة", true);
            AddTableCell(headerRow, "التاريخ", true);
            AddTableCell(headerRow, "العميل", true);
            AddTableCell(headerRow, "نوع الدفع", true);
            AddTableCell(headerRow, "الإجمالي", true);
            AddTableCell(headerRow, "الضريبة", true);
            AddTableCell(headerRow, "الصافي", true);

            if (_invoicesList != null)
            {
                foreach (InvoiceTxt invoice in _invoicesList)
                {
                    var row = new TableRow();
                    rowGroup.Rows.Add(row);

                    AddTableCell(row, invoice.AutoIncrementID.ToString());
                    AddTableCell(row, invoice.InvoiceTypeTxt);
                    AddTableCell(row, invoice.InvoiceNo.ToString());
                    AddTableCell(row, invoice.InvDate.ToString("yyyy-MM-dd"));
                    AddTableCell(row, invoice.ClientTxt);
                    AddTableCell(row, invoice.PaymentTxt);
                    AddTableCell(row, invoice.Total.ToString("0.##"));
                    AddTableCell(row, invoice.VAT.ToString("0.##"));
                    AddTableCell(row, invoice.Net.ToString("0.##"));
                }
            }

            document.Blocks.Add(new Paragraph(new Run(" "))
            {
                Margin = new Thickness(0, 10, 0, 0)
            });

            double totalSum = _invoicesList?.Sum(x => x.Total) ?? 0;
            double totalVat = _invoicesList?.Sum(x => x.VAT) ?? 0;
            double totalNet = _invoicesList?.Sum(x => x.Net) ?? 0;

            document.Blocks.Add(new Paragraph(new Run(
                $"الإجمالي: {totalSum:0.##}    |    الضريبة: {totalVat:0.##}    |    الصافي: {totalNet:0.##}"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.DarkGreen
            });

            return document;
        }

        private void AddTableCell(TableRow row, string text, bool isHeader = false)
        {
            var paragraph = new Paragraph(new Run(text ?? string.Empty))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2)
            };

            var cell = new TableCell(paragraph)
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4),
                Background = isHeader ? new SolidColorBrush(Color.FromRgb(33, 58, 122)) : Brushes.White
            };

            if (isHeader)
            {
                paragraph.Foreground = Brushes.White;
                paragraph.FontWeight = FontWeights.Bold;
            }

            row.Cells.Add(cell);
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Search Panel Handlers

        private void ckAllType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbType.IsEnabled = ckAllType.IsChecked != true;
        }
        private void cbAllSalesmen_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSaleman.IsEnabled = cbAllSalesmen.IsChecked != true;
        }
        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbusers.IsEnabled = ckAllUsers.IsChecked != true;
        }

        private void ckAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = ckAllClients.IsChecked != true;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allPeriod = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allPeriod;
            txtToDate.IsEnabled = !allPeriod;
            txtStartTime.IsEnabled = !allPeriod;
            txtEndTime.IsEnabled = !allPeriod;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Helpers

        private void RegenerateEntry(string invGlobalId)
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var inv = invoiceOper.BindInvoByID(invGlobalId);
                var entryOper = new EntryOper();

                if (!entryOper.SaveEnty(entryOper.BindInvoiceToEntry(inv)))
                {
                    string msg = string.Equals(MainClass.Language, "en",
                                               StringComparison.OrdinalIgnoreCase)
                        ? "Error in saving"
                        : "خطأ أثناء إعادة التوليد";

                    ShowError(msg);
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في إعادة توليد القيد: {ex.Message}");
            }
        }

        private void EnsureConnectionOpen()
        {
            if (_conn.State != ConnectionState.Open)
                _conn.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (_conn.State == ConnectionState.Open)
                _conn.Close();
        }

        private static void ShowInfo(string msg)
            => DXMessageBox.Show(msg, "تنبيه",
                               MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowSuccess(string msg)
            => DXMessageBox.Show(msg, "نجاح",
                               MessageBoxButton.OK, MessageBoxImage.Information);

        private static void ShowWarning(string msg)
            => DXMessageBox.Show(msg, "تحذير",
                               MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowError(string msg)
            => DXMessageBox.Show(msg, "خطأ",
                               MessageBoxButton.OK, MessageBoxImage.Error);

        #endregion
    }
}