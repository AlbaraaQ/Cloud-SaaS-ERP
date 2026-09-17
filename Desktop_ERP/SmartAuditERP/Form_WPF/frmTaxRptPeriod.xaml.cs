using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Brushes = System.Windows.Media.Brushes;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTaxRptPeriod : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Private Structs
        // ══════════════════════════════════════════════

        private struct InvoicePartTotals
        {
            public double BaseBeforeVAT;
            public double VATAmount;
            public double ExtraVATAmount;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;

        private double NetTotSale;
        private double NetTaxSale;
        private double Sale;
        private double SaleDiscount;
        private double TaxSale;
        private double ExtraVAT;
        private double RetSale;
        private double TaxRetSale;
        private double ExtraVATRet;
        private double NetTotPurch;
        private double NetTaxPurch;
        private double Purch;
        private double TaxPurch;
        private double RetPurch;
        private double TaxRetPurch;

        private DateTime DateFrom;
        private DateTime DateTo;
        private int currYear;
        private string currDate;

        private double Expense;
        private double VATExpense;

        private bool PricIncVATPurch;
        private bool PricIncVATSale;
        private bool PricIncVATPOS;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        // WPF bindable date values (used by DateEdit bindings)
        public DateTime DateFromValue { get; set; }
        public DateTime DateToValue { get; set; }

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmTaxRptPeriod()
        {
            InitializeComponent();
            DataContext = this;

            conn = MainClass.ConnObj();
            currYear = DateTime.Today.Year;
            currDate = string.Empty;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = string.Empty;
            RptUrl = string.Empty;

            // Initialise date properties
            DateFromValue = DateTime.Now;
            DateToValue = DateTime.Now;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Events
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime = DateTime.Now;

            LoadVATtype();

            cmbQuartars.IsEnabled = false;
            cmbMonthly.IsEnabled = false;

            loadPrintSettings();
            LoadBranches();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Data Loading
        // ══════════════════════════════════════════════

        private void LoadBranches()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId AS Id, name FROM Branches WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "Id";

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

        private void LoadVATtype()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral ORDER BY Inv_Id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    try
                    {
                        PricIncVATPurch = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                        PricIncVATSale = Convert.ToBoolean(dt.Rows[1]["PriceIncVAT"]);
                        PricIncVATPOS = Convert.ToBoolean(dt.Rows[2]["PriceIncVAT"]);
                    }
                    catch { /* silent – settings may be incomplete */ }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل إعدادات الضريبة:\n" + ex.Message);
            }
        }

        private void loadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=14", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return;

                try
                {
                    PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();

                    PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                    RptName = dt.Rows[0]["RptName"].ToString();
                    RptUrl = dt.Rows[0]["RptUrl"].ToString();
                }
                catch { /* silent */ }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Date Helpers
        // ══════════════════════════════════════════════

        private void SetDate()
        {
            try
            {
                if (!ckQuart.IsChecked.GetValueOrDefault() &&
                    !ckMonthly.IsChecked.GetValueOrDefault())
                {
                    DateFrom = txtDateFrom.DateTime.Date;
                    DateTo = txtDateTo.DateTime.Date.AddDays(1);
                    return;
                }

                if (ckQuart.IsChecked.GetValueOrDefault())
                {
                    switch (cmbQuartars.SelectedIndex)
                    {
                        case 0:
                            txtDateFrom.DateTime = new DateTime(currYear, 1, 1);
                            txtDateTo.DateTime = new DateTime(currYear, 3, 31);
                            break;
                        case 1:
                            txtDateFrom.DateTime = new DateTime(currYear, 4, 1);
                            txtDateTo.DateTime = new DateTime(currYear, 6, 30);
                            break;
                        case 2:
                            txtDateFrom.DateTime = new DateTime(currYear, 7, 1);
                            txtDateTo.DateTime = new DateTime(currYear, 9, 30);
                            break;
                        case 3:
                            txtDateFrom.DateTime = new DateTime(currYear, 10, 1);
                            txtDateTo.DateTime = new DateTime(currYear, 12, 31);
                            break;
                    }
                }
                else if (ckMonthly.IsChecked.GetValueOrDefault())
                {
                    int idx = cmbMonthly.SelectedIndex; // 0-based
                    int month = idx + 1;
                    int daysInMonth = DateTime.DaysInMonth(currYear, month);
                    txtDateFrom.DateTime = new DateTime(currYear, month, 1);
                    txtDateTo.DateTime = new DateTime(currYear, month, daysInMonth);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ\n" + ex.Message);
            }
        }

        private DateTime GetDateFrom() => txtDateFrom.DateTime.Date;
        private DateTime GetDateTo() => txtDateTo.DateTime.Date.AddDays(1);

        #endregion

        // ══════════════════════════════════════════════
        #region Branch Condition Helpers
        // ══════════════════════════════════════════════

        private string GetBranchConditionInv()
        {
            if (!chkAllBranches.IsChecked.GetValueOrDefault() &&
                cmbBranches.SelectedIndex != -1)
                return $"inv.branch={cmbBranches.SelectedValue} AND ";
            return string.Empty;
        }

        private string GetBranchConditionPlain()
        {
            if (!chkAllBranches.IsChecked.GetValueOrDefault() &&
                cmbBranches.SelectedIndex != -1)
                return $"branch={cmbBranches.SelectedValue} AND ";
            return string.Empty;
        }

        private string GetBranchConditionBranchID()
        {
            if (!chkAllBranches.IsChecked.GetValueOrDefault() &&
                cmbBranches.SelectedIndex != -1)
                return $"BranchID={cmbBranches.SelectedValue} AND ";
            return string.Empty;
        }

        private string GetBranchConditionEntry()
        {
            if (!chkAllBranches.IsChecked.GetValueOrDefault() &&
                cmbBranches.SelectedIndex != -1)
                return $"Entry.branch={cmbBranches.SelectedValue} AND ";
            return string.Empty;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region SQL Helper
        // ══════════════════════════════════════════════

        private DataTable LoadDataTable(string query, DateTime date1, DateTime date2)
        {
            using (var adapter = new SqlDataAdapter(query, conn))
            {
                adapter.SelectCommand.Parameters.AddWithValue("@date1", date1.ToShortDateString());
                adapter.SelectCommand.Parameters.AddWithValue("@date2", date2.ToShortDateString());
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        private static double SafeDouble(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            return double.TryParse(value.ToString(), out double result) ? result : 0.0;
        }

        private static int SafeInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CalcInvoicePart
        // ══════════════════════════════════════════════

        private InvoicePartTotals CalcInvoicePart(DataRow invRow, string filterVatMode)
        {
            var result = new InvoicePartTotals();

            string invGlobalId = invRow["InvGlobalID"] != DBNull.Value
                ? invRow["InvGlobalID"].ToString() : string.Empty;

            double vatPercent = SafeDouble(invRow["VATPercent"]);
            double invMinus = SafeDouble(invRow["minus"]);
            bool priceIncVat = invRow["PriceIncVAT"] != DBNull.Value
                && Convert.ToBoolean(invRow["PriceIncVAT"]);

            string filterClause = string.Empty;
            if (filterVatMode == "TAXED")
                filterClause = "Inv_Sub.taxval <> 0";
            else if (filterVatMode == "NOTAX")
                filterClause = "Inv_Sub.taxval = 0 AND Inv_Sub.ProductId = 0";

            string sql =
                "SELECT SUM(Inv_Sub.val1 * Inv_Sub.exchange_price) AS totalBaseNoVAT, " +
                "ISNULL(SUM(Inv_Sub.discount),0) AS ItemsDiscountRaw, " +
                "SUM(Inv_Sub.taxval) AS TaxValSum, " +
                "SUM(ISNULL(Inv_Sub.ItemAdditionalTax,0)) AS ExtraTaxSum " +
                "FROM Inv_Sub " +
                $"WHERE Inv_Sub.InvGlobalID=@gid AND {filterClause}";

            using (var adapter = new SqlDataAdapter(sql, conn))
            {
                adapter.SelectCommand.Parameters.AddWithValue("@gid", invGlobalId);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return result;

                double totalBase = SafeDouble(dt.Rows[0]["totalBaseNoVAT"]);
                double itemsDiscount = SafeDouble(dt.Rows[0]["ItemsDiscountRaw"]);
                double taxValSum = SafeDouble(dt.Rows[0]["TaxValSum"]);
                double extraVatAmt = SafeDouble(dt.Rows[0]["ExtraTaxSum"]);

                double minusBase = 0.0, minusTax = 0.0;
                if (filterVatMode == "TAXED" && invMinus > 0.0)
                {
                    if (priceIncVat && vatPercent > 0.0)
                    {
                        minusBase = invMinus / (1.0 + vatPercent / 100.0);
                        minusTax = invMinus - minusBase;
                    }
                    else
                    {
                        minusBase = invMinus;
                        minusTax = invMinus * (vatPercent / 100.0);
                    }
                }

                double discountNet = itemsDiscount;
                if (priceIncVat && itemsDiscount > 0.0 && vatPercent > 0.0)
                    discountNet = itemsDiscount / (1.0 + vatPercent / 100.0);

                if (priceIncVat && vatPercent > 0.0)
                    totalBase /= 1.0 + vatPercent / 100.0;

                double baseNet = totalBase - discountNet;
                if (filterVatMode == "TAXED") baseNet -= minusBase;
                if (baseNet < 0.0) baseNet = 0.0;

                double taxNet = filterVatMode == "TAXED" ? taxValSum - minusTax : 0.0;
                if (taxNet < 0.0) taxNet = 0.0;

                result.BaseBeforeVAT = baseNet;
                result.VATAmount = taxNet;
                result.ExtraVATAmount = extraVatAmt;
            }

            return result;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region ShowDetails22  (main calculation used by btnShow)
        // ══════════════════════════════════════════════

        private void ShowDetails22()
        {
            try
            {
                // Sales accumulators
                double saleBase = 0, saleTax = 0, saleExtra = 0;
                double saleRetBase = 0, saleRetTax = 0, saleRetExtra = 0;
                double saleNoTaxBase = 0, saleNoTaxTax = 0;
                double saleRetNoTaxBase = 0, saleRetNoTaxTax = 0;

                // Purchase accumulators
                double purchBase = 0, purchTax = 0;
                double purchRetBase = 0, purchRetTax = 0;
                double purchNoTaxBase = 0, purchNoTaxTax = 0;
                double purchRetNoTaxBase = 0, purchRetNoTaxTax = 0;

                // Expense accumulators
                double expTotal = 0, expVAT = 0;

                string branchInv = GetBranchConditionInv();
                string branchBranch = GetBranchConditionBranchID();
                string branchEntry = GetBranchConditionEntry();

                DateTime date1 = GetDateFrom();
                DateTime date2 = GetDateTo();

                // ── Sales invoices ──────────────────────────────────────────
                string sqlSales =
                    $"SELECT InvGlobalID, inv_type, proc_type, minus, tax, VATPercent, InvSum, PriceIncVAT " +
                    $"FROM inv WHERE {branchInv}" +
                    "(inv_type IN (2,3,20,21,22)) AND (proc_type IN (1,2)) " +
                    "AND date>=@date1 AND date<@date2 AND IS_Deleted=0";

                using (var adapter = new SqlDataAdapter(sqlSales, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        int procType = SafeInt(row["proc_type"]);
                        var taxed = CalcInvoicePart(row, "TAXED");
                        var noTax = CalcInvoicePart(row, "NOTAX");

                        if (procType == 1)
                        {
                            saleBase += taxed.BaseBeforeVAT;
                            saleTax += taxed.VATAmount;
                            saleExtra += taxed.ExtraVATAmount;
                            saleNoTaxBase += noTax.BaseBeforeVAT;
                            saleNoTaxTax += noTax.VATAmount;
                        }
                        else if (procType == 2)
                        {
                            saleRetBase += taxed.BaseBeforeVAT;
                            saleRetTax += taxed.VATAmount;
                            saleRetExtra += taxed.ExtraVATAmount;
                            saleRetNoTaxBase += noTax.BaseBeforeVAT;
                            saleRetNoTaxTax += noTax.VATAmount;
                        }
                    }
                }

                SetCellText(txtNetSale, Math.Round(saleBase - saleRetBase, 2).ToString());
                SetCellText(txtTaxNetSale, Math.Round(saleTax - saleRetTax, 2).ToString());
                SetCellText(txtExtraVAT, Math.Round(saleExtra - saleRetExtra
                                                      + saleNoTaxTax - saleRetNoTaxTax, 2).ToString());
                SetCellText(txtSalesOutTax, Math.Round(saleNoTaxBase - saleRetNoTaxBase, 2).ToString());
                SetCellText(txtSalesNoTax, Math.Round(saleNoTaxTax - saleRetNoTaxTax, 2).ToString());

                // ── Receipts (type 10) ──────────────────────────────────────
                string sqlReceipts =
                    $"SELECT SUM(Payment) AS total, SUM(NetVal) AS NetVal " +
                    $"FROM Receipts WHERE ReceiptType=10 AND branchID={MainClass.BranchNo} " +
                    "AND Paymenttype=1 AND ReceiptDate>=@date1 AND ReceiptDate<@date2 AND ISDeleted=0";

                using (var adapter = new SqlDataAdapter(sqlReceipts, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    double total = dt.Rows.Count > 0 ? SafeDouble(dt.Rows[0]["total"]) : 0;
                    double netVal = dt.Rows.Count > 0 ? SafeDouble(dt.Rows[0]["NetVal"]) : 0;
                    SetCellText(txtReceipts, total.ToString());
                    SetCellText(txtReceiptsVAT, (netVal - total).ToString());
                }

                // ── Rent invoices ───────────────────────────────────────────
                string branchPlain = GetBranchConditionPlain();

                string sqlRentIn =
                    $"SELECT SUM(tot_net) AS tot_net, SUM(tax) AS TaxVal " +
                    $"FROM RentInvoice WHERE {branchPlain}" +
                    "(proc_type=1 OR proc_type=3) AND date>=@date1 AND date<@date2 AND IS_Deleted=0";

                using (var adapter = new SqlDataAdapter(sqlRentIn, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    SetCellText(txtIncomes, dt.Rows.Count > 0 ? SafeDouble(dt.Rows[0]["tot_net"]).ToString() : "0");
                    SetCellText(txtIncomesVAT, dt.Rows.Count > 0 ? SafeDouble(dt.Rows[0]["TaxVal"]).ToString() : "0");
                }

                string sqlRentRet =
                    $"SELECT SUM(tot_net) AS tot_net, SUM(tax) AS TaxVal " +
                    $"FROM RentInvoice WHERE {branchPlain}" +
                    "proc_type=2 AND date>=@date1 AND date<@date2 AND IS_Deleted=0";

                using (var adapter = new SqlDataAdapter(sqlRentRet, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        double curIncomes = SafeDouble(txtIncomes.Text.Trim());
                        double curIncomesVAT = SafeDouble(txtIncomesVAT.Text.Trim());
                        SetCellText(txtIncomes, (curIncomes - SafeDouble(dt.Rows[0]["tot_net"])).ToString());
                        SetCellText(txtIncomesVAT, (curIncomesVAT - SafeDouble(dt.Rows[0]["TaxVal"])).ToString());
                    }
                }

                // ── Sales totals ────────────────────────────────────────────
                double netSales = Math.Round(
                    SafeDouble(txtNetSale.Text) + SafeDouble(txtReceipts.Text) + SafeDouble(txtIncomes.Text), 2);
                double netSalesTax = Math.Round(
                    SafeDouble(txtTaxNetSale.Text) + SafeDouble(txtReceiptsVAT.Text) + SafeDouble(txtIncomesVAT.Text), 2);
                SetCellText(txtNetSales, netSales.ToString());
                SetCellText(txtNetSaleTax, netSalesTax.ToString());

                // ── Purchase invoices ───────────────────────────────────────
                string sqlPurch =
                    $"SELECT InvGlobalID, inv_type, proc_type, minus, tax, VATPercent, InvSum, PriceIncVAT " +
                    $"FROM inv WHERE {branchInv}" +
                    "inv_type=1 AND (proc_type IN (1,2)) " +
                    "AND date>=@date1 AND date<@date2 AND IS_Deleted=0";

                using (var adapter = new SqlDataAdapter(sqlPurch, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        int procType = SafeInt(row["proc_type"]);
                        var taxed = CalcInvoicePart(row, "TAXED");
                        var noTax = CalcInvoicePart(row, "NOTAX");

                        if (procType == 1)
                        {
                            purchBase += taxed.BaseBeforeVAT;
                            purchTax += taxed.VATAmount;
                            purchNoTaxBase += noTax.BaseBeforeVAT;
                            purchNoTaxTax += noTax.VATAmount;
                        }
                        else if (procType == 2)
                        {
                            purchRetBase += taxed.BaseBeforeVAT;
                            purchRetTax += taxed.VATAmount;
                            purchRetNoTaxBase += noTax.BaseBeforeVAT;
                            purchRetNoTaxTax += noTax.VATAmount;
                        }
                    }
                }

                SetCellText(txtNetPurch, Math.Round(purchBase - purchRetBase, 2).ToString());
                SetCellText(txtTaxNetPurch, Math.Round(purchTax - purchRetTax, 2).ToString());
                SetCellText(txtPurchOutTax, Math.Round(purchNoTaxBase - purchRetNoTaxBase, 2).ToString());
                SetCellText(txtNoTaxPurch, Math.Round(purchNoTaxTax - purchRetNoTaxTax, 2).ToString());

                // ── Expense receipts (type 9) ───────────────────────────────
                string sqlExpReceipts =
                    $"SELECT SUM(Payment) AS total, SUM(NetVal) AS NetVal, SUM(VAT) AS TotalVAT " +
                    $"FROM Receipts WHERE ReceiptType=9 AND {branchBranch}" +
                    "ReceiptDate>=@date1 AND ReceiptDate<@date2 AND ISDeleted=0";

                using (var adapter = new SqlDataAdapter(sqlExpReceipts, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        expTotal += SafeDouble(dt.Rows[0]["total"]);
                        expVAT += SafeDouble(dt.Rows[0]["TotalVAT"]);
                    }
                }

                // ── Journal entries (type 10, IsVAT=1) ─────────────────────
                string sqlEntry =
                    $"SELECT " +
                    "(SELECT SUM(dept) FROM Entry_sub WHERE acc_no=N'2222001' AND Entry.GlobalID=Entry_sub.EntryGlobalID) AS DeptVAT, " +
                    "(SELECT SUM(dept) FROM Entry_sub WHERE acc_no<>N'2222001' AND Entry.GlobalID=Entry_sub.EntryGlobalID) AS Total " +
                    $"FROM Entry WHERE Entry.type=10 AND Entry.IsVAT=1 AND {branchEntry}" +
                    "Entry.date>=@date1 AND Entry.date<@date2 AND Entry.IS_Deleted=0";

                using (var adapter = new SqlDataAdapter(sqlEntry, conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@date1", date1);
                    adapter.SelectCommand.Parameters.AddWithValue("@date2", date2);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        expTotal += SafeDouble(row["Total"]);
                        expVAT += SafeDouble(row["DeptVAT"]);
                    }
                }

                SetCellText(txtExpenseNet, expTotal.ToString(Common.DigitsNo));
                SetCellText(txtExpenseVAT, expVAT.ToString(Common.DigitsNo));

                // ── Purchase totals ─────────────────────────────────────────
                double netPurches = Math.Round(
                    SafeDouble(txtNetPurch.Text) + expTotal, 2);
                double netPurchesTax = Math.Round(
                    SafeDouble(txtTaxNetPurch.Text) + expVAT, 2);
                SetCellText(txtNetPurches, netPurches.ToString());
                SetCellText(txtNetPurchTax, netPurchesTax.ToString());

                // ── Net VAT ─────────────────────────────────────────────────
                double netVAT = SafeDouble(txtNetSaleTax.Text) - SafeDouble(txtNetPurchTax.Text);
                string netVATText = netVAT.ToString(Common.DigitsNo);

                if (netVAT > 0.0)
                {
                    txtNetTax.Text = netVATText + "    (مستحق الدفع للهيئة)    ";
                    txtNetTax.Foreground = Brushes.Red;
                }
                else
                {
                    txtNetTax.Text = netVATText + "   (غير مستحق)    ";
                    txtNetTax.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ\n" + ex.Message);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region UI Helper
        // ══════════════════════════════════════════════

        /// <summary>Sets the Text of a display TextBlock (read-only label cell).</summary>
        private static void SetCellText(System.Windows.Controls.TextBlock tb, string value)
        {
            if (tb != null) tb.Text = value;
        }

        /// <summary>Reads text from a display TextBlock safely.</summary>
        private static string GetCellText(System.Windows.Controls.TextBlock tb)
            => tb?.Text?.Trim() ?? "0";

        #endregion

        // ══════════════════════════════════════════════
        #region BindToData (for reports)
        // ══════════════════════════════════════════════

        private DataSet BindToData()
        {
            var list = new List<RptTaxData>();
            var item = new RptTaxData
            {
                FromDate = txtDateFrom.DateTime.ToShortDateString(),
                ToDate = txtDateTo.DateTime.ToShortDateString(),
                Quartars = (cmbQuartars.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                Monthly = (cmbMonthly.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? string.Empty,

                A1 = GetCellText(txtNetSale),
                A2 = GetCellText(txtIncomes),
                A3 = GetCellText(txtReceipts),
                A4 = GetCellText(Label31),
                A5 = GetCellText(txtSalesOutTax),
                A6 = GetCellText(txtNetSales),

                B1 = GetCellText(txtTaxNetSale),
                B2 = GetCellText(txtIncomesVAT),
                B3 = GetCellText(txtReceiptsVAT),
                B4 = GetCellText(Label30),
                B5 = GetCellText(txtSalesNoTax),
                B6 = GetCellText(txtNetSaleTax),

                C1 = GetCellText(txtNetPurch),
                C2 = GetCellText(Label52),
                C3 = GetCellText(txtExpenseNet),
                C4 = GetCellText(Label48),
                C5 = GetCellText(txtPurchOutTax),
                C6 = GetCellText(txtNetPurches),

                D1 = GetCellText(txtTaxNetPurch),
                D2 = GetCellText(Label51),
                D3 = GetCellText(txtExpenseVAT),
                D4 = GetCellText(Label47),
                D5 = GetCellText(txtNoTaxPurch),
                D6 = GetCellText(txtNetPurchTax),

                RestrType = Title,

                E = GetCellText(Label64),
                F = GetCellText(Label63),
                G = GetCellText(Label14),
                H = GetCellText(txtNetTax),

                User = Common.GetEmpName(MainClass.EmpNo),
                PrintDate = DateTime.Now.ToShortDateString()
            };

            list.Add(item);

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print / Preview
        // ══════════════════════════════════════════════

        private void PrintDevexpress(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "TaxRptPeriod.repx";
            defPrinter = MainClass.ReportsPrinter;

            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }

            if (!Directory.Exists(RptUrl) || !File.Exists(Path.Combine(RptUrl, RptName)))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ");
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات");
                return;
            }

            var report = XtraReport.FromFile(Path.Combine(RptUrl, RptName));
            report.DataSource = BindToData();

            // Header sub-report
            var headerRpt = XtraReport.FromFile(Path.Combine(RptUrl, "header.repx"));
            headerRpt.DataSource = Common.FoundationInfoDT;
            var headerCtrl = report.FindControl("headerRpt", ignoreCase: true) as XRSubreport;
            if (headerCtrl != null) headerCtrl.ReportSource = headerRpt;

            // Footer sub-report
            var footerRpt = XtraReport.FromFile(Path.Combine(RptUrl, "footer.repx"));
            footerRpt.DataSource = Common.FoundationInfoDT;
            var footerCtrl = report.FindControl("footerRpt", ignoreCase: true) as XRSubreport;
            if (footerCtrl != null) footerCtrl.ReportSource = footerRpt;

            if (!string.IsNullOrEmpty(defPrinter))
            {
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
            else
            {
                DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات");
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Events
        // ══════════════════════════════════════════════

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try { ShowDetails22(); }
            catch (Exception ex) { DXMessageBox.Show("خطأ\n" + ex.Message); }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPOSdetails_Click(object sender, RoutedEventArgs e)
        {
            // Visibility="Collapsed" by default – no action needed currently
        }

        private void btnSalesDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmTaxInvDetails();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Invtype = 2;
                frm.ckTotalPeriod.IsChecked = false;
                frm.txtDateFrom.DateTime = txtDateFrom.DateTime;
                frm.txtDateTo.DateTime = txtDateTo.DateTime;
                frm.Show();
                frm.ShowInvs();
                frm.Activate();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ\n" + ex.Message);
            }
        }

        private void btnPurchesDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmTaxInvDetails();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Invtype = 1;
                frm.ckTotalPeriod.IsChecked = false;
                frm.txtDateFrom.DateTime = txtDateFrom.DateTime;
                frm.txtDateTo.DateTime = txtDateTo.DateTime;
                frm.Show();
                frm.ShowInvs();
                frm.Activate();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ\n" + ex.Message);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox / ComboBox Events
        // ══════════════════════════════════════════════

        private void ckQuart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckQuart.IsChecked.GetValueOrDefault())
            {
                cmbQuartars.IsEnabled = true;
                cmbMonthly.IsEnabled = false;
                ckMonthly.IsChecked = false;
            }
            else
            {
                cmbQuartars.IsEnabled = false;
                cmbMonthly.IsEnabled = false;
            }
        }

        private void ckMonthly_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckMonthly.IsChecked.GetValueOrDefault())
            {
                cmbQuartars.IsEnabled = false;
                txtDateFrom.IsEnabled = false;
                txtDateTo.IsEnabled = false;
                cmbMonthly.IsEnabled = true;
                ckQuart.IsChecked = false;
            }
            else
            {
                cmbMonthly.IsEnabled = false;
                cmbQuartars.IsEnabled = false;
                txtDateTo.IsEnabled = true;
                txtDateFrom.IsEnabled = true;
            }
        }

        private void cmbQuartars_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
            => SetDate();

        private void cmbMonthly_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
            => SetDate();

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkAllBranches.IsChecked.GetValueOrDefault())
                cmbBranches.SelectedIndex = -1;
            else
                cmbBranches.SelectedValue = MainClass.BranchNo;
        }

        private void cmbBranches_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Reserved for future branch-change logic
        }

        #endregion
    }
}