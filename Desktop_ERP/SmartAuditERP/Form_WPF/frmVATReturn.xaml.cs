using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmVATReturn : ThemedWindow
    {
        #region Nested Structs

        private struct SalesTotalsResult
        {
            public double SalesBase;
            public double SalesVAT;
            public double ExtraVAT;
            public double OtherIncomeBase;
            public double OtherIncomeVAT;
            public double Netsale;
            public double salesnotax;
        }

        private struct PurchTotalsResult
        {
            public double PurchBase;
            public double PurchVAT;
            public double purchnotax;
        }

        private struct ExpenseTotalsResult
        {
            public double ExpenseBase;
            public double ExpenseVAT;
        }

        #endregion

        #region Fields

        private SqlConnection conn;
        private bool isInitializing;

        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private int PrintNo;
        private bool hasLoadedData;

        #endregion

        #region Constructor

        public frmVATReturn()
        {
            InitializeComponent();
            conn = new SqlConnection(MainClass.connstr);
            isInitializing = false;
            RptName = string.Empty;
            RptUrl = string.Empty;
            PrintNo = 1;
            hasLoadedData = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isInitializing = true;
            try
            {
                LoadBranches();
                LoadQuarters();
                LoadMonths();

                numYearQ.Text = DateTime.Now.Year.ToString();
                numYearM.Text = DateTime.Now.Year.ToString();

                if (cmbQuarter.Items.Count > 0)
                    cmbQuarter.SelectedIndex = 0;

                if (cmbMonth.Items.Count > 0)
                {
                    int monthIdx = DateTime.Now.Month - 1;
                    if (monthIdx < 0) monthIdx = 0;
                    if (monthIdx >= cmbMonth.Items.Count)
                        monthIdx = cmbMonth.Items.Count - 1;
                    cmbMonth.SelectedIndex = monthIdx;
                }

                // الافتراضي: شهري
                rbMonthly.IsChecked = true;
                ShowMonthControls();
                ApplyMonthDates();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء التحميل: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isInitializing = false;
            }
        }

        #endregion

        #region Load Branches / Quarters / Months

        private void LoadBranches()
        {
            bool prevInit = isInitializing;
            isInitializing = true;
            try
            {
                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT BranchId AS Id, name FROM Branches WHERE IS_Deleted=0 ORDER BY name",
                    conn))
                {
                    da.Fill(dt);
                }

                cmbBranch.ItemsSource = dt.DefaultView;
                cmbBranch.DisplayMemberPath = "name";
                cmbBranch.SelectedValuePath = "Id";

                if (MainClass.BranchNo != -1)
                {
                    cmbBranch.SelectedValue = MainClass.BranchNo;
                    chkAllBranches.IsChecked = false;
                    cmbBranch.IsEnabled = true;
                }
                else
                {
                    chkAllBranches.IsChecked = true;
                    cmbBranch.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("فشل تحميل الفروع: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isInitializing = prevInit;
            }
        }

        private void LoadQuarters()
        {
            cmbQuarter.Items.Clear();
            cmbQuarter.Items.Add("الربع 1 (يناير - مارس)");
            cmbQuarter.Items.Add("الربع 2 (أبريل - يونيو)");
            cmbQuarter.Items.Add("الربع 3 (يوليو - سبتمبر)");
            cmbQuarter.Items.Add("الربع 4 (أكتوبر - ديسمبر)");
            cmbQuarter.SelectedIndex = 0;
        }

        private void LoadMonths()
        {
            cmbMonth.Items.Clear();
            var monthNames = new CultureInfo("ar-SA").DateTimeFormat.MonthNames;
            for (int i = 0; i <= 11; i++)
            {
                if (!string.IsNullOrWhiteSpace(monthNames[i]))
                    cmbMonth.Items.Add($"{monthNames[i]} ({i + 1})");
            }

            int currentMonth = DateTime.Now.Month - 1;
            if (currentMonth < 0) currentMonth = 0;
            cmbMonth.SelectedIndex = currentMonth;
        }

        #endregion

        #region Period Controls Visibility

        private void ShowQuarterControls()
        {
            pnlQuarterControls.Visibility = Visibility.Visible;
            pnlMonthControls.Visibility = Visibility.Collapsed;
        }

        private void ShowMonthControls()
        {
            pnlMonthControls.Visibility = Visibility.Visible;
            pnlQuarterControls.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Apply Dates

        private void ApplyQuarterDates()
        {
            if (cmbQuarter.Items.Count == 0) return;

            int quarterIdx = cmbQuarter.SelectedIndex;
            if (quarterIdx < 0) quarterIdx = 0;

            int quarterNum = quarterIdx + 1;

            if (!int.TryParse(numYearQ.Text, out int year) || year < 1900 || year > 2100)
                year = DateTime.Now.Year;

            int startMonth, endMonth;
            switch (quarterNum)
            {
                case 1: startMonth = 1; endMonth = 3; break;
                case 2: startMonth = 4; endMonth = 6; break;
                case 3: startMonth = 7; endMonth = 9; break;
                case 4: startMonth = 10; endMonth = 12; break;
                default: startMonth = 1; endMonth = 3; break;
            }

            dtFrom.DateTime = new DateTime(year, startMonth, 1);
            dtTo.DateTime = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
        }

        private void ApplyMonthDates()
        {
            int monthNum = cmbMonth.SelectedIndex + 1;
            if (monthNum < 1 || monthNum > 12)
                monthNum = DateTime.Now.Month;

            if (!int.TryParse(numYearM.Text, out int year) || year < 1900 || year > 2100)
                year = DateTime.Now.Year;

            dtFrom.DateTime = new DateTime(year, monthNum, 1);
            dtTo.DateTime = new DateTime(year, monthNum, DateTime.DaysInMonth(year, monthNum));
        }

        #endregion

        #region Filter Events

        private void rbQuarterly_Checked(object sender, RoutedEventArgs e)
        {
            ShowQuarterControls();
            if (!isInitializing)
                ApplyQuarterDates();
        }

        private void rbMonthly_Checked(object sender, RoutedEventArgs e)
        {
            ShowMonthControls();
            if (!isInitializing)
                ApplyMonthDates();
        }

        private void cmbQuarter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitializing && rbQuarterly.IsChecked == true)
                ApplyQuarterDates();
        }

        private void numYearQ_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitializing && rbQuarterly.IsChecked == true)
                ApplyQuarterDates();
        }

        private void cmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitializing && rbMonthly.IsChecked == true)
                ApplyMonthDates();
        }

        private void numYearM_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitializing && rbMonthly.IsChecked == true)
                ApplyMonthDates();
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbBranch.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void cmbBranch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق إضافي هنا
        }

        #endregion

        #region Refresh Data

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            DateTime fromDt = new DateTime(
                dtFrom.DateTime.Year,
                dtFrom.DateTime.Month,
                dtFrom.DateTime.Day, 0, 0, 0);

            DateTime toDt = new DateTime(
                dtTo.DateTime.Year,
                dtTo.DateTime.Month,
                dtTo.DateTime.Day, 23, 59, 59);

            int branchId = -1;
            if (chkAllBranches.IsChecked != true && cmbBranch.SelectedValue != null)
            {
                if (int.TryParse(cmbBranch.SelectedValue.ToString(), out int bid))
                    branchId = bid;
            }

            string branchCondition = branchId != -1
                ? $" AND inv.branch = {branchId}"
                : "";

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                // ══════════════════════════════
                // استعلام المبيعات
                // ══════════════════════════════
                string salesSql = $@"
                    WITH InvoiceCalc AS (
                        SELECT 
                            inv.proc_type,
                            inv.VATPercent,
                            inv.PriceIncVAT,
                            inv.minus,
                            SUM(CASE 
                                WHEN sub.taxval <> 0 THEN 
                                    CASE 
                                        WHEN inv.PriceIncVAT = 1 AND inv.VATPercent > 0 
                                        THEN (sub.val1 * sub.exchange_price) / (1 + (inv.VATPercent / 100.0))
                                        ELSE (sub.val1 * sub.exchange_price)
                                    END
                                ELSE 0 
                            END) AS TaxableBase,
                            SUM(CASE 
                                WHEN sub.taxval = 0 AND sub.ProductId = 0 THEN 
                                    CASE 
                                        WHEN inv.PriceIncVAT = 1 AND inv.VATPercent > 0 
                                        THEN (sub.val1 * sub.exchange_price) / (1 + (inv.VATPercent / 100.0))
                                        ELSE (sub.val1 * sub.exchange_price)
                                    END
                                ELSE 0 
                            END) AS NonTaxableBase,
                            SUM(ISNULL(sub.discount, 0)) AS ItemsDiscount,
                            SUM(sub.taxval) AS TaxAmount,
                            SUM(ISNULL(sub.ItemAdditionalTax, 0)) AS ExtraTax
                        FROM inv
                        INNER JOIN Inv_Sub sub ON inv.InvGlobalID = sub.InvGlobalID
                        WHERE inv.IS_Deleted = 0
                            AND inv.inv_type IN (2, 3, 20, 21, 22)
                            AND inv.proc_type IN (1, 2)
                            AND inv.date >= @fromDate 
                            AND inv.date < @toDate
                            {branchCondition}
                        GROUP BY inv.InvGlobalID, inv.proc_type, inv.VATPercent, inv.PriceIncVAT, inv.minus
                    )
                    SELECT 
                        proc_type,
                        SUM(
                            TaxableBase 
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN ItemsDiscount / (1 + (VATPercent / 100.0))
                              ELSE ItemsDiscount END
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN minus / (1 + (VATPercent / 100.0))
                              ELSE minus END
                        ) AS NetTaxableBase,
                        SUM(
                            TaxAmount 
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN 0
                              ELSE minus * (VATPercent / 100.0) END
                        ) AS TotalVAT,
                        SUM(ExtraTax) AS TotalExtraVAT,
                        SUM(NonTaxableBase) AS TotalNonTaxable
                    FROM InvoiceCalc
                    GROUP BY proc_type";

                DataTable salesDt = new DataTable();
                using (SqlCommand cmd = new SqlCommand(salesSql, conn))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDt);
                    cmd.Parameters.AddWithValue("@toDate", toDt);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        da.Fill(salesDt);
                }

                double salesBase = 0, salesVAT = 0, extraVAT = 0, salesNotTax = 0;

                foreach (DataRow row in salesDt.Rows)
                {
                    int procType = row["proc_type"] != DBNull.Value
                        ? Convert.ToInt32(row["proc_type"]) : 0;
                    double netBase = row["NetTaxableBase"] != DBNull.Value
                        ? Convert.ToDouble(row["NetTaxableBase"]) : 0.0;
                    double vatAmt = row["TotalVAT"] != DBNull.Value
                        ? Convert.ToDouble(row["TotalVAT"]) : 0.0;
                    double extraVatAmt = row["TotalExtraVAT"] != DBNull.Value
                        ? Convert.ToDouble(row["TotalExtraVAT"]) : 0.0;
                    double nonTaxable = row["TotalNonTaxable"] != DBNull.Value
                        ? Convert.ToDouble(row["TotalNonTaxable"]) : 0.0;

                    switch (procType)
                    {
                        case 1:
                            salesBase += netBase;
                            salesVAT += vatAmt;
                            extraVAT += extraVatAmt;
                            salesNotTax += nonTaxable;
                            break;
                        case 2:
                            salesBase -= netBase;
                            salesVAT -= vatAmt;
                            extraVAT -= extraVatAmt;
                            break;
                    }
                }

                // قيود الضريبة الإضافية (دائن)
                string entryExtraVATSql = $@"
                    SELECT SUM(CASE WHEN es.acc_no = N'2222001' THEN es.credit ELSE 0 END) AS creditVAT
                    FROM Entry e
                    JOIN Entry_sub es ON e.GlobalID = es.EntryGlobalID
                    WHERE e.type = 10
                      AND e.IsVAT = 1
                      AND e.IS_Deleted = 0
                      AND e.[date] >= @date1
                      AND e.[date] < @date2
                      {(branchId != -1 ? " AND e.branch = @branchId" : "")}";

                using (SqlDataAdapter daExtra = new SqlDataAdapter(entryExtraVATSql, conn))
                {
                    daExtra.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    daExtra.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    if (branchId != -1)
                        daExtra.SelectCommand.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;

                    DataTable dtExtra = new DataTable();
                    daExtra.Fill(dtExtra);

                    if (dtExtra.Rows.Count > 0 && dtExtra.Rows[0]["creditVAT"] != DBNull.Value)
                        extraVAT += Convert.ToDouble(dtExtra.Rows[0]["creditVAT"]);
                }

                // ══════════════════════════════
                // استعلام المشتريات
                // ══════════════════════════════
                string purchSql = $@"
                    WITH PurchaseCalc AS (
                        SELECT 
                            inv.proc_type,
                            inv.VATPercent,
                            inv.PriceIncVAT,
                            inv.minus,
                            SUM(CASE 
                                WHEN sub.taxval <> 0 THEN 
                                    CASE 
                                        WHEN inv.PriceIncVAT = 1 AND inv.VATPercent > 0 
                                        THEN (sub.val1 * sub.exchange_price) / (1 + (inv.VATPercent / 100.0))
                                        ELSE (sub.val1 * sub.exchange_price)
                                    END
                                ELSE 0 
                            END) AS TaxableBase,
                            SUM(CASE 
                                WHEN sub.taxval = 0 AND sub.ProductId = 0 
                                THEN (sub.val1 * sub.exchange_price)
                                ELSE 0 
                            END) AS NonTaxableBase,
                            SUM(ISNULL(sub.discount, 0)) AS ItemsDiscount,
                            SUM(sub.taxval) AS TaxAmount
                        FROM inv
                        INNER JOIN Inv_Sub sub ON inv.InvGlobalID = sub.InvGlobalID
                        WHERE inv.IS_Deleted = 0
                            AND inv.inv_type = 1
                            AND inv.proc_type IN (1, 2)
                            AND inv.date >= @fromDate 
                            AND inv.date < @toDate
                            {branchCondition}
                        GROUP BY inv.InvGlobalID, inv.proc_type, inv.VATPercent, inv.PriceIncVAT, inv.minus
                    )
                    SELECT 
                        proc_type,
                        SUM(
                            TaxableBase 
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN ItemsDiscount / (1 + (VATPercent / 100.0))
                              ELSE ItemsDiscount END
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN minus / (1 + (VATPercent / 100.0))
                              ELSE minus END
                        ) AS NetTaxableBase,
                        SUM(
                            TaxAmount 
                            - CASE WHEN PriceIncVAT = 1 AND VATPercent > 0 
                              THEN 0
                              ELSE minus * (VATPercent / 100.0) END
                        ) AS TotalVAT,
                        SUM(NonTaxableBase) AS TotalNonTaxable
                    FROM PurchaseCalc
                    GROUP BY proc_type";

                DataTable purchDt = new DataTable();
                using (SqlCommand cmd2 = new SqlCommand(purchSql, conn))
                {
                    cmd2.Parameters.AddWithValue("@fromDate", fromDt);
                    cmd2.Parameters.AddWithValue("@toDate", toDt);
                    using (SqlDataAdapter da2 = new SqlDataAdapter(cmd2))
                        da2.Fill(purchDt);
                }

                double purchBase = 0, purchVAT = 0, purchNotTax = 0;

                foreach (DataRow row in purchDt.Rows)
                {
                    int procType = row["proc_type"] != DBNull.Value
                        ? Convert.ToInt32(row["proc_type"]) : 0;
                    double netBase = row["NetTaxableBase"] != DBNull.Value
                        ? Convert.ToDouble(row["NetTaxableBase"]) : 0.0;
                    double vatAmt = row["TotalVAT"] != DBNull.Value
                        ? Convert.ToDouble(row["TotalVAT"]) : 0.0;
                    double nonTaxable = row["TotalNonTaxable"] != DBNull.Value
                        ? Convert.ToDouble(row["TotalNonTaxable"]) : 0.0;

                    switch (procType)
                    {
                        case 1:
                            purchBase += netBase;
                            purchVAT += vatAmt;
                            purchNotTax += nonTaxable;
                            break;
                        case 2:
                            purchBase -= netBase;
                            purchVAT -= vatAmt;
                            break;
                    }
                }

                // ══════════════════════════════
                // استعلام المصروفات (Receipts)
                // ══════════════════════════════
                double expenseBase = 0, expenseVAT = 0;

                string receiptSql = $@"
                    SELECT SUM(Payment) AS total, SUM(VAT) AS vatVal
                    FROM Receipts 
                    WHERE ReceiptType = 9 
                        AND ReceiptDate >= @fromDate 
                        AND ReceiptDate < @toDate 
                        AND ISDeleted = 0
                        {(branchId != -1 ? $" AND BranchID = {branchId}" : "")}";

                using (SqlCommand cmd3 = new SqlCommand(receiptSql, conn))
                {
                    cmd3.Parameters.AddWithValue("@fromDate", fromDt);
                    cmd3.Parameters.AddWithValue("@toDate", toDt);
                    using (SqlDataReader dr = cmd3.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            expenseBase = dr["total"] != DBNull.Value
                                ? Convert.ToDouble(dr["total"]) : 0.0;
                            expenseVAT = dr["vatVal"] != DBNull.Value
                                ? Convert.ToDouble(dr["vatVal"]) : 0.0;
                        }
                    }
                }

                // قيود المصروفات (Entry)
                string entryExpSql = $@"
                    SELECT
                        SUM(CASE WHEN es.acc_no = N'2222001' THEN es.dept ELSE 0 END) AS DeptVAT,
                        SUM(CASE WHEN es.acc_no <> N'2222001' THEN es.dept ELSE 0 END) AS Total
                    FROM Entry e
                    JOIN Entry_sub es ON e.GlobalID = es.EntryGlobalID
                    WHERE e.type = 10
                      AND e.IsVAT = 1
                      AND e.IS_Deleted = 0
                      AND e.[date] >= @date1
                      AND e.[date] < @date2
                      {(branchId != -1 ? " AND e.branch = @branchId" : "")}";

                using (SqlDataAdapter daEntryExp = new SqlDataAdapter(entryExpSql, conn))
                {
                    daEntryExp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    daEntryExp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                    if (branchId != -1)
                        daEntryExp.SelectCommand.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;

                    DataTable dtEntryExp = new DataTable();
                    daEntryExp.Fill(dtEntryExp);

                    if (dtEntryExp.Rows.Count > 0)
                    {
                        expenseBase += dtEntryExp.Rows[0]["Total"] != DBNull.Value
                            ? Convert.ToDouble(dtEntryExp.Rows[0]["Total"]) : 0.0;
                        expenseVAT += dtEntryExp.Rows[0]["DeptVAT"] != DBNull.Value
                            ? Convert.ToDouble(dtEntryExp.Rows[0]["DeptVAT"]) : 0.0;
                    }
                }

                // ══════════════════════════════
                // تحديث واجهة المستخدم
                // ══════════════════════════════
                lblSalesTaxableValue.Text = salesBase.ToString("N2");
                lblSalesVATValue.Text = salesVAT.ToString("N2");
                lblExtraVATValue.Text = extraVAT.ToString("N2");
                lblsalesnotax.Text = salesNotTax.ToString("N2");
                LblNetsale.Text = salesBase.ToString("N2");
                lblOtherIncomeValue.Text = "0.00";

                lblPurchTaxableValue.Text = purchBase.ToString("N2");
                lblPurchVATValue.Text = purchVAT.ToString("N2");
                lblpurchnotax.Text = purchNotTax.ToString("N2");
                lblExpensesValue.Text = expenseBase.ToString("N2");
                lblExpensesVATValue.Text = expenseVAT.ToString("N2");
                LblNetPurch.Text = (purchBase + expenseBase).ToString("N2");

                // الملخص
                double totalOutputVAT = salesVAT + extraVAT;
                double totalInputVAT = purchVAT + expenseVAT;
                double netResult = totalOutputVAT - totalInputVAT;

                lblOutputTotalValue.Text = totalOutputVAT.ToString("N2");
                lblInputTotalValue.Text = totalInputVAT.ToString("N2");
                lblNetResultValue.Text = netResult.ToString("N2");

                if (netResult >= 0)
                {
                    lblNetResultValue.Foreground = new SolidColorBrush(
                        Color.FromRgb(192, 0, 0));
                    lblNetResultLabel.Text = "صافي الضريبة المستحقة للسداد:";
                }
                else
                {
                    lblNetResultValue.Foreground = new SolidColorBrush(
                        Color.FromRgb(0, 150, 0));
                    lblNetResultLabel.Text = "رصيد دائن (قابل للترحيل):";
                }

                hasLoadedData = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        #endregion

        #region Details Buttons

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmTaxInvDetails();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Invtype = 1;
                form.ckTotalPeriod.IsChecked = false;
                form.txtDateFrom.DateTime = dtFrom.DateTime;
                form.txtDateTo.DateTime = dtTo.DateTime;
                form.Show();
                form.ShowInvs();
                form.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmTaxInvDetails();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Invtype = 2;
                form.ckTotalPeriod.IsChecked = false;
                form.txtDateFrom.DateTime = dtFrom.DateTime;
                form.txtDateTo.DateTime = dtTo.DateTime;
                form.Show();
                form.ShowInvs();
                form.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Print

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void PrintDevexpress(int printType)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "TaxRptPeriodNew.repx";
            defPrinter = MainClass.ReportsPrinter;

            if (string.IsNullOrEmpty(RptUrl))
            {
                MessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(RptUrl) || !File.Exists(Path.Combine(RptUrl, RptName)))
            {
                MessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                MessageBox.Show("يجب إدخال اسم التقرير من الإعدادات", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(defPrinter))
            {
                MessageBox.Show("يجب تحديد الطابعة من الإعدادات", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var rpt = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, RptName));
                rpt.DataSource = BindToData();

                var header = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, "header.repx"));
                header.DataSource = Common.FoundationInfoDT;

                var headerCtrl = rpt.FindControl("headerRpt", true)
                    as DevExpress.XtraReports.UI.XRSubreport;
                if (headerCtrl != null)
                    headerCtrl.ReportSource = header;

                var footer = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, "footer.repx"));
                footer.DataSource = Common.FoundationInfoDT;

                var footerCtrl = rpt.FindControl("footerRpt", true)
                    as DevExpress.XtraReports.UI.XRSubreport;
                if (footerCtrl != null)
                    footerCtrl.ReportSource = footer;

                rpt.PrinterName = defPrinter;

                if (printType == 1)
                {
                    for (int i = 0; i < PrintNo; i++)
                        rpt.Print();
                }
                else
                {
                    rpt.ShowPreviewDialog();
                }

                rpt.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BindToData()
        {
            var list = new List<RptTaxData>
            {
                new RptTaxData
                {
                    FromDate   = dtFrom.DateTime.ToShortDateString(),
                    ToDate     = dtTo.DateTime.ToShortDateString(),
                    Quartars   = cmbQuarter.SelectedItem?.ToString() ?? "",
                    Monthly    = cmbMonth.SelectedItem?.ToString() ?? "",
                    A1 = lblSalesTaxableValue.Text,
                    A2 = lblSalesVATValue.Text,
                    A3 = lblExtraVATValue.Text,
                    A4 = lblOtherIncomeValue.Text,
                    A5 = lblsalesnotax.Text,
                    A6 = LblNetsale.Text,
                    B1 = lblSalesTaxableValue.Text,
                    B2 = lblSalesVATValue.Text,
                    B3 = lblExtraVATValue.Text,
                    B4 = lblOtherIncomeValue.Text,
                    B5 = lblsalesnotax.Text,
                    B6 = LblNetsale.Text,
                    C1 = lblPurchTaxableValue.Text,
                    C2 = lblPurchVATValue.Text,
                    C3 = lblExpensesValue.Text,
                    C4 = lblExpensesVATValue.Text,
                    C5 = lblpurchnotax.Text,
                    C6 = LblNetPurch.Text,
                    RestrType = this.Title,
                    E  = lblOutputTotalValue.Text,
                    F  = lblInputTotalValue.Text,
                    H  = lblNetResultValue.Text,
                    User      = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                }
            };

            DataSet ds = new DataSet("Name");
            DataTable table = UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;
        }

        #endregion
    }
}