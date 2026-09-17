using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptInvNotfic : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        // ═══ Public Fields (محافظة على الأسماء الأصلية) ═══
        public int  _Client_id = 0;
        public int  invType    = 21;

        // ═══ Private Fields ═══
        private List<InvoiceTxt> _invoicesList;

        private bool   _printHeader = true;
        private bool   _printFooter = true;
        private bool   _printStamp  = true;
        private int    _printType   = 1;
        private int    _printNo     = 1;
        private string _defPrinter  = "";
        private string _rptName     = "rptInvSumByClient.repx";
        private string _rptUrl      = "";

        // ═══ مجاميع ═══
        private double _sumPaycash   = 0;
        private double _sumPayATM    = 0;
        private double _sumSumPrice  = 0;
        private double _sumDiscount  = 0;
        private double _sumTotal     = 0;
        private double _sumVAT       = 0;
        private double _sumExtraVAT  = 0;
        private double _sumTotalTax  = 0;
        private double _sumNet       = 0;

        #endregion

        #region Constructor

        public frmRptInvNotfic()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;

            // ═══ تعبئة أنواع الإشعارات حسب نوع الفاتورة واللغة ═══
            if (invType == 21)
            {
                if (MainClass.Language == "ar")
                {
                    cmbInvType.Items.Add("إشعار دائن");
                    cmbInvType.Items.Add("إشعار مدين");
                }
                else
                {
                    cmbInvType.Items.Add("Notice Credit");
                    cmbInvType.Items.Add("Notice Dept");
                }
                lblGridTitle.Text = "📋 إشعارات المبيعات";
            }
            else if (invType == 22)
            {
                if (MainClass.Language == "ar")
                {
                    cmbInvType.Items.Add("إشعار مدين");
                    cmbInvType.Items.Add("إشعار دائن");
                }
                else
                {
                    cmbInvType.Items.Add("Notice Dept");
                    cmbInvType.Items.Add("Notice Credit");
                }
                lblGridTitle.Text = "📋 إشعارات المشتريات";
                this.Title = "تقرير إشعارات المشتريات";
            }

            LoadEmps();
            LoadSalesmen();
            LoadClients();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadEmps()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbusers.ItemsSource       = dt.DefaultView;
                    cmbusers.DisplayMemberPath = "name";
                    cmbusers.SelectedValuePath = "id";
                    cmbusers.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadSalesmen()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbSaleman.ItemsSource       = dt.DefaultView;
                    cmbSaleman.DisplayMemberPath = "name";
                    cmbSaleman.SelectedValuePath = "id";
                    cmbSaleman.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadClients()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Customers WHERE IS_Deleted=0 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbClients.ItemsSource       = dt.DefaultView;
                    cmbClients.DisplayMemberPath = "name";
                    cmbClients.SelectedValuePath = "id";
                    cmbClients.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        #endregion

        #region CheckBox Events

        private void ckAllInvs_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbInvType.IsEnabled = (ckAllInvs.IsChecked != true);
            if (ckAllInvs.IsChecked == true)
                cmbInvType.SelectedIndex = -1;
        }

        private void ckAllUsers_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbusers.IsEnabled = (ckAllUsers.IsChecked != true);
            if (ckAllUsers.IsChecked == true)
                cmbusers.SelectedIndex = -1;
        }

        private void cbAllSalesmen_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSaleman.IsEnabled = (cbAllSalesmen.IsChecked != true);
            if (cbAllSalesmen.IsChecked == true)
                cmbSaleman.SelectedIndex = -1;
        }

        private void ckAllClients_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = (ckAllClients.IsChecked != true);
            if (ckAllClients.IsChecked == true)
                cmbClients.SelectedIndex = -1;
        }

        private void ckTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (ckTotalPeriod.IsChecked != true);
            txtFromDate.IsEnabled  = enabled;
            txtToDate.IsEnabled    = enabled;
            txtStartTime.IsEnabled = enabled;
            txtEndTime.IsEnabled   = enabled;
        }

        #endregion

        #region Show Invoices

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowInvoice();
        }

        public void ShowInvoice()
        {
            try
            {
                GridControl2.ItemsSource = null;
                _invoicesList?.Clear();
                ResetSummaries();

                // ═══ بناء شروط SQL مطابقة للكود الأصلي ═══
                string cond = "";

                // نوع الفاتورة
                if (invType == 21)
                    cond = "(inv.inv_type = 21) And ";
                else if (invType == 22)
                    cond = "(inv.inv_type = 22) And ";

                // الفرع (مخفي في الأصل - احتفظ بالمنطق)
                // لا يوجد cmbBranches في هذا النموذج

                // العميل
                if (ckAllClients.IsChecked != true
                    && cmbClients.SelectedIndex > -1
                    && cmbClients.SelectedValue != null)
                    cond += $"cust_id = @ClientId And ";

                // المستخدم
                if (ckAllUsers.IsChecked != true
                    && cmbusers.SelectedIndex > -1
                    && cmbusers.SelectedValue != null)
                    cond += $"sales_emp = @UserId And ";

                // المندوب
                if (cbAllSalesmen.IsChecked != true
                    && cmbSaleman.SelectedIndex > -1
                    && cmbSaleman.SelectedValue != null)
                    cond += $"salesman = @SalesmanId And ";

                // نوع العملية (مبيعات / مرتجع / الكل)
                if (rbAllSales.IsChecked == true)
                    cond += "(inv.proc_type = 1 Or inv.proc_type = 2) And ";
                else if (rbSales.IsChecked == true)
                    cond += "inv.proc_type = 1 And ";
                else if (rbReturn.IsChecked == true)
                    cond += "inv.proc_type = 2 And ";

                // نوع الإشعار
                if (ckAllInvs.IsChecked != true)
                {
                    if (cmbInvType.SelectedIndex == 0)
                        cond += "(inv.proc_type = 2) And ";
                    else if (cmbInvType.SelectedIndex == 1)
                        cond += "(inv.proc_type = 1) And ";
                }

                // الفترة الزمنية
                DateTime fromDate = DateTime.Parse(
                    txtFromDate.DateTime.ToShortDateString() + " " +
                    (txtStartTime.Text ?? "00:00"));
                DateTime toDate = DateTime.Parse(
                    txtToDate.DateTime.ToShortDateString() + " " +
                    (txtEndTime.Text ?? "23:59"));

                if (ckTotalPeriod.IsChecked != true)
                    cond += "inv.date BETWEEN @DateFrom AND @DateTo And ";

                // إزالة " And " الأخيرة
                if (cond.EndsWith(" And "))
                    cond = cond.Substring(0, cond.Length - 5);

                // ═══ الاستعلام الكامل (مطابق للأصل) ═══
                string sqlstr = @"
                    SELECT inv.InvGlobalID, inv.EntryID, inv.inv_type, inv.proc_type, inv.pay_type,
                           inv.branch, inv.date, inv.id, inv.AdditionalCost, inv.InvoiceStatus,
                           inv.stock, inv.safe, inv.tax,
                           ISNULL(inv.ExtraVAT, 0) AS ExtraVAT, inv.minus, inv.paid, inv.tot_net,
                           inv.Reff_No, inv.Reff_date, inv.cash, inv.visa, inv.bank, inv.cust_id,
                           inv.sales_emp, inv.InvTotal, inv.salesman,
                           (SELECT ISNULL(SUM(Inv_Sub.val1 * Inv_Sub.exchange_price), 0)
                            FROM Inv_Sub WHERE InvGlobalID = inv.InvGlobalID) AS sumPrice,
                           (SELECT ISNULL(SUM(discount), 0)
                            FROM Inv_Sub WHERE InvGlobalID = inv.InvGlobalID) AS ItemDiscount,
                           (SELECT ISNULL(SUM(CASE WHEN taxval = 0 THEN (val1 * exchange_price) ELSE 0 END), 0)
                            FROM Inv_Sub WHERE InvGlobalID = inv.InvGlobalID) AS FreeVATSales,
                           Customers.name AS CustName,
                           Users.username AS username,
                           Branches.name AS BranchName,
                           salesmen.name AS SalesmanTxt
                    FROM Inv
                    LEFT JOIN Customers ON inv.cust_id = Customers.id
                    LEFT JOIN Users ON inv.sales_emp = Users.emp
                    LEFT JOIN Branches ON inv.branch = Branches.BranchId
                    LEFT JOIN salesmen ON inv.salesman = salesmen.id
                    WHERE " + (string.IsNullOrEmpty(cond) ? "1=1" : cond) +
                    " AND inv.IS_Deleted = 0 ORDER BY inv.date DESC";

                // ═══ استدعاء دالة البناء (مطابقة للأصل) ═══
                _invoicesList = BindingListOfInvoices1(sqlstr, withItems: false, fromDate, toDate);

                // ربط البيانات بالـ DataGrid
                GridControl2.ItemsSource = _invoicesList;

                // حساب المجاميع
                CalculateSummaries();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region BindingListOfInvoices1 (مطابقة للكود الأصلي)

        public List<InvoiceTxt> BindingListOfInvoices1(
            string sqlstr, bool withItems, DateTime date1, DateTime date2)
        {
            var sqlConn = MainClass.ConnObj();
            if (sqlConn.State != ConnectionState.Open)
                sqlConn.Open();

            var da = new SqlDataAdapter(sqlstr, sqlConn);
            da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = date1;
            da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = date2;

            // إضافة Parameters للفلترة
            if (ckAllClients.IsChecked != true
                && cmbClients.SelectedIndex > -1
                && cmbClients.SelectedValue != null)
                da.SelectCommand.Parameters.Add("@ClientId", SqlDbType.Int).Value
                    = cmbClients.SelectedValue;

            if (ckAllUsers.IsChecked != true
                && cmbusers.SelectedIndex > -1
                && cmbusers.SelectedValue != null)
                da.SelectCommand.Parameters.Add("@UserId", SqlDbType.Int).Value
                    = cmbusers.SelectedValue;

            if (cbAllSalesmen.IsChecked != true
                && cmbSaleman.SelectedIndex > -1
                && cmbSaleman.SelectedValue != null)
                da.SelectCommand.Parameters.Add("@SalesmanId", SqlDbType.Int).Value
                    = cmbSaleman.SelectedValue;

            if (ckTotalPeriod.IsChecked != true)
            {
                da.SelectCommand.Parameters.Add("@DateFrom", SqlDbType.DateTime).Value = date1;
                da.SelectCommand.Parameters.Add("@DateTo",   SqlDbType.DateTime).Value = date2;
            }

            var dt = new DataTable();
            var list = new List<InvoiceTxt>();

            try
            {
                da.Fill(dt);
                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        try
                        {
                            var row      = dt.Rows[i];
                            var invObj   = new InvoiceObj(
                                Convert.ToInt32(row["inv_type"]),
                                Convert.ToInt32(row["proc_type"]));

                            string invoiceType = InvoiceOper.GetInvoiceType(
                                Convert.ToInt32(row["Inv_type"]),
                                Convert.ToInt32(row["Proc_type"]),
                                Convert.ToInt32(row["pay_type"]), 1);

                            var inv = new InvoiceTxt
                            {
                                ISNew         = false,
                                IsPrinted     = true,
                                IsLoaded      = false,
                                InvGlobalID   = row["InvGlobalID"].ToString(),
                                EntryGlobalID = $"{row["branch"]}-{row["EntryID"]}",
                                ProcType      = Convert.ToInt32(row["proc_type"]),
                                InvoiceNo     = Convert.ToInt32(row["id"]),
                                InvoiceType   = (InvoiceType)Convert.ToInt32(row["inv_type"]),
                                InvoiceTypeTxt = invoiceType,
                                AutoIncrementID = i + 1,
                                ReffNo        = row["Reff_No"].ToString(),
                                PayType       = Convert.ToInt32(row["pay_type"]),
                                Bank          = Convert.ToInt32(row["bank"]),
                                Customer      = Convert.ToInt32(row["cust_id"]),
                                User          = Convert.ToInt32(row["sales_emp"]),
                                BranchTxt     = row["BranchName"].ToString(),
                                SalesmanTxt   = row["SalesmanTxt"]?.ToString() ?? "",
                                AdditionalCost = Convert.ToDecimal(row["AdditionalCost"]),
                                InvoiceStatus  = Convert.ToInt32(row["InvoiceStatus"]),
                                Treasury       = Convert.ToInt32(row["stock"]),
                                Store          = Convert.ToInt32(row["safe"]),
                                InvDiscount    = Convert.ToDouble(row["minus"]),
                                TotDiscount    = Convert.ToDouble(row["minus"]),
                                Paid           = Convert.ToDouble(row["paid"]),
                                VATperc        = invObj.VAT,
                                ExtraVATPerc   = invObj.AdditionalTax,
                                PriceIncVAT    = invObj.PriceIncVAT
                            };

                            // التاريخ والوقت
                            var invDate = Convert.ToDateTime(row["date"]);
                            inv.InvDate    = invDate;
                            inv.InvTime    = Convert.ToDateTime(invDate.ToString("hh:mm:ss tt"));
                            inv.InvoiceTime = invDate.ToString("hh:mm:ss tt");

                            // المرجع
                            try { inv.RefDate = Convert.ToDateTime(row["Reff_date"]); } catch { }

                            // الضريبة
                            double taxTotal  = Math.Round(Convert.ToDouble(row["tax"]), 2);
                            double extraVAT  = Math.Round(Convert.ToDouble(row["ExtraVAT"]), 2);
                            inv.ExtraVAT     = extraVAT;
                            inv.TotalTax     = taxTotal;
                            inv.VAT          = (taxTotal - extraVAT > 0)
                                ? Math.Round(taxTotal - extraVAT, 2)
                                : taxTotal;

                            if (inv.VAT <= 0)
                            {
                                inv.VAT      = taxTotal;
                                inv.TotalTax = Math.Round(inv.VAT + extraVAT, 2);
                            }

                            // المدفوعات
                            inv.Paycash  = Convert.ToDouble(row["cash"]);
                            inv.PayATM   = Convert.ToDouble(row["visa"]);
                            inv.Remainder = Convert.ToDouble(row["paid"])
                                          - Convert.ToDouble(row["tot_net"]);

                            // تحديد نص طريقة الدفع
                            double payTypeVal = Convert.ToDouble(row["pay_type"]);
                            if (payTypeVal == 1)
                            {
                                inv.Paycash    = Convert.ToDouble(row["tot_net"]);
                                inv.PaymentTxt = (MainClass.Language == "ar") ? "نقدي" : "Cash";
                            }
                            else if (payTypeVal == -1)
                                inv.PaymentTxt = (MainClass.Language == "ar") ? "آجل"  : "Credit";
                            else if (payTypeVal == 2)
                                inv.PaymentTxt = (MainClass.Language == "ar") ? "شبكة" : "Card";
                            else if (payTypeVal == 4)
                                inv.PaymentTxt = (MainClass.Language == "ar") ? "متعدد" : "Multi";
                            else if (payTypeVal == 5)
                                inv.PaymentTxt = (MainClass.Language == "ar") ? "ضيافة" : "Guest";

                            // العميل والمستخدم
                            inv.ClientTxt = (row["CustName"] == DBNull.Value)
                                ? "" : row["CustName"].ToString();
                            inv.UserTxt = (row["username"] == DBNull.Value)
                                ? "" : row["username"].ToString();

                            // المستودع
                            inv.InvertoryName = Common.GetStoreName(Convert.ToInt32(row["safe"]));

                            // المجاميع
                            if (!withItems)
                            {
                                double itemDiscount = Convert.ToDouble(row["ItemDiscount"]);
                                inv.TotDiscount += itemDiscount;
                                inv.SumPrice = Math.Round(Convert.ToDouble(row["sumPrice"]), 2);
                            }

                            inv.FreeVATSales = Math.Round(Convert.ToDouble(row["FreeVATSales"]), 2);
                            inv.Total = Math.Round(Convert.ToDouble(row["InvTotal"]), 2);
                            inv.Net   = Math.Round(Convert.ToDouble(row["tot_net"]), 2);

                            list.Add(inv);
                            ProgressBar1.Value++;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
            finally
            {
                sqlConn?.Close();
            }

            return list;
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

            _sumPaycash  = 0; _sumPayATM   = 0; _sumSumPrice = 0;
            _sumDiscount = 0; _sumTotal    = 0; _sumVAT      = 0;
            _sumExtraVAT = 0; _sumTotalTax = 0; _sumNet      = 0;

            foreach (var inv in _invoicesList)
            {
                _sumPaycash  += inv.Paycash;
                _sumPayATM   += inv.PayATM;
                _sumSumPrice += inv.SumPrice;
                _sumDiscount += inv.TotDiscount;
                _sumTotal    += inv.Total;
                _sumVAT      += inv.VAT;
                _sumExtraVAT += inv.ExtraVAT;
                _sumTotalTax += inv.TotalTax;
                _sumNet      += inv.Net;
            }

            lblPaycash.Text  = _sumPaycash.ToString("N2");
            lblPayATM.Text   = _sumPayATM.ToString("N2");
            lblSumPrice.Text = _sumSumPrice.ToString("N2");
            lblDiscount.Text = _sumDiscount.ToString("N2");
            lblTotal.Text    = _sumTotal.ToString("N2");
            lblVAT.Text      = _sumVAT.ToString("N2");
            lblExtraVAT.Text = _sumExtraVAT.ToString("N2");
            lblTotalTax.Text = _sumTotalTax.ToString("N2");
            lblNet.Text      = _sumNet.ToString("N2");
            lblCount.Text    = _invoicesList.Count.ToString();
        }

        private void ResetSummaries()
        {
            lblPaycash.Text  = "0.00"; lblPayATM.Text   = "0.00";
            lblSumPrice.Text = "0.00"; lblDiscount.Text = "0.00";
            lblTotal.Text    = "0.00"; lblVAT.Text      = "0.00";
            lblExtraVAT.Text = "0.00"; lblTotalTax.Text = "0.00";
            lblNet.Text      = "0.00"; lblCount.Text    = "0";
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
                    frm.btnRecalculateCost.Visibility = Visibility.Visible;
                    frm.ISProfit    = false;
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
                var inv = btn.Tag as InvoiceTxt;
                if (inv == null) return;

                string globalId = inv.InvGlobalID;
                int    procType = inv.ProcType;
                int    invType_ = (int)inv.InvoiceType;

                // ═══ مطابقة منطق RepositoryItemBtnShowInv_ButtonClick الأصلي ═══
                if (invType_ == 21 || invType_ == 22)
                {
                    if (invType == 21)
                    {
                        // إشعارات المبيعات
                        var frm = new frmInvSale();
                        MainClass.ApplyPermissionToForm(frm);

                        if (invType == 21 && procType == 2)
                        {
                            frm.Title = "إشعار دائن";
                            frm.Name  = "FrmNoticCreditSale";
                        }
                        else if (invType == 21 && procType == 1)
                        {
                            frm.Title = "إشعار مدين";
                            frm.Name  = "FrmNoticDeptSale";
                        }

                        frm.InvType   = invType;
                        frm.ProcType  = procType;
                        frm.Show();
                        frm.WindowState = WindowState.Maximized;
                        frm.Navigate(
                            $"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                            $"AND (inv_type=21 OR inv_type=22) " +
                            $"AND proc_type={procType} " +
                            $"AND InvGlobalID=N'{globalId}'");
                        frm.Activate();
                    }
                    else if (invType == 22)
                    {
                        // إشعارات المشتريات
                        var frm = new frmInvPurch();
                        MainClass.ApplyPermissionToForm(frm);
                        //احتمال
                        //frm.GroupBox2.Text = "إشعارات المشتريات";
                        frm.InvType       = invType;
                        frm.ProcType      = procType;

                        if (invType == 22 && procType == 1)
                        {
                            frm.Title = "إشعار دائن";
                            frm.Name  = "FrmNoticCreditPurch";
                            frm.LblReturn.Visibility = Visibility.Visible;
                            frm.LblReturn.Text    = "إشعار دائن";
                        }
                        else if (invType == 22 && procType == 2)
                        {
                            frm.Title = "إشعار مدين";
                            frm.Name  = "FrmNoticDeptPurch";
                            frm.LblReturn.Visibility = Visibility.Visible;
                            frm.LblReturn.Text    = "إشعار مدين";
                        }

                        frm.Show();
                        frm.WindowState = WindowState.Maximized;
                        frm.Navigate(
                            $"SELECT * FROM Inv WHERE IS_Deleted=0 " +
                            $"AND (inv_type=21 OR inv_type=22) " +
                            $"AND proc_type={procType} " +
                            $"AND InvGlobalID=N'{globalId}'");
                        frm.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
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
                sb.AppendLine("م,نوع الإشعار,رقم الإشعار,رقم المرجع,التاريخ,الوقت," +
                              "العميل,نقدي,شبكة,المجموع,الخصم,الإجمالي," +
                              "الضريبة,ضريبة إضافية,إجمالي الضريبة,الصافي," +
                              "المستودع,الفرع,المندوب,المستخدم");

                foreach (var inv in _invoicesList)
                {
                    sb.AppendLine(
                        $"{inv.AutoIncrementID}," +
                        $"{inv.InvoiceTypeTxt}," +
                        $"{inv.InvoiceNo}," +
                        $"{inv.ReffNo}," +
                        $"{inv.InvDate:d}," +
                        $"{inv.InvoiceTime}," +
                        $"{inv.ClientTxt}," +
                        $"{inv.Paycash:N2}," +
                        $"{inv.PayATM:N2}," +
                        $"{inv.SumPrice:N2}," +
                        $"{inv.TotDiscount:N2}," +
                        $"{inv.Total:N2}," +
                        $"{inv.VAT:N2}," +
                        $"{inv.ExtraVAT:N2}," +
                        $"{inv.TotalTax:N2}," +
                        $"{inv.Net:N2}," +
                        $"{inv.InvertoryName}," +
                        $"{inv.BranchTxt}," +
                        $"{inv.SalesmanTxt}," +
                        $"{inv.UserTxt}");
                }

                // إضافة صف المجاميع
                sb.AppendLine(
                    $",,,,,,المجاميع," +
                    $"{_sumPaycash:N2},{_sumPayATM:N2}," +
                    $"{_sumSumPrice:N2},{_sumDiscount:N2}," +
                    $"{_sumTotal:N2},{_sumVAT:N2}," +
                    $"{_sumExtraVAT:N2},{_sumTotalTax:N2}," +
                    $"{_sumNet:N2}");

                string fileName = $"InvNotifications_{DateTime.Now:yyyyMMdd_HHmm}.csv";
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
            _rptUrl     = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName    = "rptInvSumByClient.repx";

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
                var report = new Report();
                report.Printing(
                    printType,
                    report.BindToData(
                        null,
                        "",
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
    }
}