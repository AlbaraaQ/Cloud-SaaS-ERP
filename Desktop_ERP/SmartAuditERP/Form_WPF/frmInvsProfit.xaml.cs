using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvsProfit : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════════
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

        private double _invSum;
        private double _invDiscount;
        private double _invVAT;
        private double _invAvrgCost;
        private bool _isEvaluated = true;
        private double _defVAT;
        private bool _priceIncVAT;

        // Custom summary accumulators
        private double Total, Total1;
        private double Discount, Discount1;
        private double Cost, Cost1;
        private double Profit, Profit1;
        private double sum, sum1;

        // DataTable المعروضة حالياً
        private DataTable _currentDataTable;

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Constructor

        public frmInvsProfit()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;

            LoadPaymentTypes();
            LoadInvTypes();
            LoadEmps();
            LoadStores();
            LoadBranches();
            ApplyBranchPermissions();
            LoadCustomers();
            LoadPrintSettings();
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Data Loading

        private void LoadPaymentTypes()
        {
            cmbType.Items.Clear();
            if (IsArabic())
            {
                cmbType.Items.Add("نقدية");
                cmbType.Items.Add("آجلة");
            }
            else
            {
                cmbType.Items.Add("Cash");
                cmbType.Items.Add("Postpone");
            }
        }

        private void LoadInvTypes()
        {
            cmbInvType.Items.Clear();
            if (IsArabic())
            {
                cmbInvType.Items.Add("مبيعات");
                cmbInvType.Items.Add("نقطة بيع");
            }
            else
            {
                cmbInvType.Items.Add("Sales Inv");
                cmbInvType.Items.Add("POS");
            }
        }

        private void LoadEmps()
        {
            try
            {
                using var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees " +
                    "WHERE IS_Deleted=0 ORDER BY id", _conn);
                var dt = new DataTable();
                da.Fill(dt);
                cmbusers.ItemsSource = dt.DefaultView;
                cmbusers.DisplayMemberPath = "name";
                cmbusers.SelectedValuePath = "id";
                cmbusers.SelectedIndex = -1;
            }
            catch { /* صامت */ }
        }

        private void LoadStores()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Safes " +
                "WHERE IS_Deleted=0 AND status<>2 ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbStore.ItemsSource = dt.DefaultView;
            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath = "id";
            cmbStore.SelectedIndex = -1;
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
                $"SELECT BranchId, name FROM Branches " +
                $"WHERE IS_Deleted=0{branchFilter}", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbBranches.ItemsSource = dt.DefaultView;
            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath = "BranchId";
            cmbBranches.SelectedIndex = -1;
        }

        private void ApplyBranchPermissions()
        {
            bool hasRestriction =
                !string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                && Accounting.BranchCondition.Trim() != " ";

            if (hasRestriction)
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                if (cmbBranches.Items.Count > 0)
                    cmbBranches.SelectedIndex = 0;
            }
        }

        private void LoadCustomers()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Customers " +
                "WHERE (type=1 OR type=3) ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbClients.ItemsSource = dt.DefaultView;
            cmbClients.DisplayMemberPath = "name";
            cmbClients.SelectedValuePath = "id";
            cmbClients.SelectedIndex = -1;
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
                        try
                        {
                            _printType = Convert.ToInt32(dt.Rows[0]["printType"]);
                            _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                            _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                            _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                            _defPrinter = dt.Rows[0]["CasherPrinter"]?.ToString()
                                           ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(_defPrinter))
                                _defPrinter = Common.GetDefaultPrinter();
                            _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                        }
                        catch { /* صامت */ }
                    }
                }

                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM SettingGeneral WHERE Inv_Id={Invtype}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try
                        {
                            _priceIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                            _defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                        }
                        catch { /* صامت */ }
                    }
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Show Invoices

        public void ShowInvs(string filterCond)
        {
            try
            {
                // بناء DataTable النتيجة
                var resultTable = new DataTable();
                resultTable.Columns.Add("DgvNo", typeof(int));
                resultTable.Columns.Add("DgvInvType", typeof(string));
                resultTable.Columns.Add("DgvInvNo", typeof(string));
                resultTable.Columns.Add("DgvRefNo", typeof(string));
                resultTable.Columns.Add("DgvDate", typeof(DateTime));
                resultTable.Columns.Add("DgvPayType", typeof(string));
                resultTable.Columns.Add("DgvClient", typeof(string));
                resultTable.Columns.Add("DgvUser", typeof(string));
                resultTable.Columns.Add("DgvTotal", typeof(double));
                resultTable.Columns.Add("DgvDiscount", typeof(double));
                resultTable.Columns.Add("DgvSum", typeof(double));
                resultTable.Columns.Add("DgvCost", typeof(double));
                resultTable.Columns.Add("DgvProfit", typeof(double));
                resultTable.Columns.Add("DgvProfitRatio", typeof(string));
                resultTable.Columns.Add("DgvOperType", typeof(string));
                resultTable.Columns.Add("DgvInvGlobalID", typeof(string));
                resultTable.Columns.Add("DgvProcType", typeof(int));
                resultTable.Columns.Add("DgvStore", typeof(string));
                resultTable.Columns.Add("DgvBranch", typeof(string));
                resultTable.Columns.Add("DgvProfitState", typeof(string));

                GridControl1.ItemsSource = null;

                // تحديث عنوان عمود التكلفة
                UpdateCostColumnHeader();

                // استعلام الفواتير
                using var da = new SqlDataAdapter(
                    $"SELECT * FROM inv WHERE {filterCond} IS_Deleted=0 ORDER BY id",
                    _conn);

                if (ckTotalPeriod.IsChecked != true)
                {
                    DateTime fromDate =
                        (txtFromDate.SelectedDate ?? DateTime.Today).Date;
                    DateTime toDate =
                        (txtToDate.SelectedDate ?? DateTime.Today).AddDays(1).Date;

                    da.SelectCommand.Parameters
                      .Add("@date1", SqlDbType.DateTime).Value = fromDate;
                    da.SelectCommand.Parameters
                      .Add("@date2", SqlDbType.DateTime).Value = toDate;
                }

                var invoicesTable = new DataTable();
                da.Fill(invoicesTable);

                _invSum = _invDiscount = _invVAT = _invAvrgCost = 0;
                _isEvaluated = true;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = invoicesTable.Rows.Count;
                });

                for (int i = 0; i < invoicesTable.Rows.Count; i++)
                {
                    DataRow invRow = invoicesTable.Rows[i];

                    // تفاصيل بنود الفاتورة
                    string globalId = invRow["InvGlobalID"].ToString();
                    using var da2 = new SqlDataAdapter($@"
                        SELECT
                            CurrentQnty,
                            Inv_Sub.val1 * Inv_Sub.exchange_price AS Sum,
                            COALESCE(Inv_Sub.discount, 0) AS ItDiscount,
                            Inv_Sub.taxval AS Vat,
                            Inv_Sub.val * Inv_Sub.AvrgCost AS AvrgCost,
                            Inv_Sub.val1 AS Qty,
                            Inv_Sub.itemId,
                            items.purch_price AS purch_price
                        FROM Inv_Sub
                        JOIN items ON Inv_Sub.ItemId = items.id
                        WHERE Inv_Sub.InvGlobalID = N'{globalId}'",
                        _conn);

                    var itemsTable = new DataTable();
                    da2.Fill(itemsTable);

                    if (itemsTable.Rows.Count > 0)
                    {
                        _invSum = _invVAT = _invAvrgCost = _invDiscount = 0;
                        _isEvaluated = true;

                        foreach (DataRow itemRow in itemsTable.Rows)
                        {
                            double currentQty =
                                Convert.ToDouble(itemRow["CurrentQnty"]);

                            if (currentQty < 0) _isEvaluated = false;

                            _invSum += Convert.ToDouble(itemRow["Sum"]);
                            _invVAT += Convert.ToDouble(itemRow["Vat"]);
                            _invDiscount += Convert.ToDouble(itemRow["ItDiscount"]);

                            if (rbAvgCost.IsChecked == true)
                                _invAvrgCost += Convert.ToDouble(itemRow["AvrgCost"]);
                            else if (rbPurchPrice.IsChecked == true)
                                _invAvrgCost += Convert.ToDouble(itemRow["purch_price"])
                                                * Convert.ToDouble(itemRow["Qty"]);
                            else if (rbLastPurchPrice.IsChecked == true)
                                _invAvrgCost +=
                                    ItemOper.RecentPurchPrice(
                                        Convert.ToInt32(itemRow["itemId"]))
                                    * Convert.ToDouble(itemRow["Qty"]);
                        }

                        // تطبيق فلتر المعالجة
                        if (!_isEvaluated
                            && ckNonProcessed.IsChecked != true)
                        {
                            Dispatcher.Invoke(() => ProgressBar1.Value++);
                            continue;
                        }

                        if (_isEvaluated
                            && ckNonProcessed.IsChecked == true)
                        {
                            Dispatcher.Invoke(() => ProgressBar1.Value++);
                            continue;
                        }

                        // بناء نص نوع الدفع
                        int payTypeVal = Convert.ToInt32(invRow["pay_type"]);
                        string payText = ResolvePaymentText(payTypeVal);

                        // نوع الفاتورة
                        int invTypeId = Convert.ToInt32(invRow["Inv_type"]);
                        int procTypeId = Convert.ToInt32(invRow["proc_type"]);
                        string invTypeName = InvoiceOper.GetInvoiceType(
                            invTypeId, procTypeId, payTypeVal, 1);

                        // القيم المحسوبة
                        double invTotal =
                            Convert.ToDouble(invRow["InvTotal"]);
                        double invSumVal =
                            Convert.ToDouble(invRow["InvSum"] == DBNull.Value
                                ? 0.0
                                : invRow["InvSum"]);
                        double totalDiscount =
                            Convert.ToDouble(invRow["minus"]) + _invDiscount;
                        double profit = invTotal - _invAvrgCost;
                        string profitRatio = "0";

                        if (_invAvrgCost > 0)
                        {
                            profitRatio =
                                Math.Round(profit / _invAvrgCost * 100.0, 2)
                                    .ToString(Common.DigitsNo) + "%";
                        }

                        string clientName =
                            Common.GetClientName(Convert.ToInt32(invRow["cust_id"]));
                        string empName =
                            Common.GetEmpName(Convert.ToInt32(invRow["sales_emp"]));
                        string storeName =
                            Common.GetStoreName(Convert.ToInt32(invRow["safe"]));
                        string branchName =
                            Common.GetBranchName(Convert.ToInt32(invRow["branch"]));

                        string profitState = "Positive";
                        if (profit == 0)
                            profitState = "Zero";
                        else if (profit < 0)
                            profitState = "Negative";
                        
                        resultTable.Rows.Add(
    resultTable.Rows.Count + 1,
    invTypeName,
    invRow["id"].ToString(),
    invRow["Reff_No"].ToString(),
    Convert.ToDateTime(invRow["date"]),
    payText,
    clientName,
    empName,
    invSumVal,
    totalDiscount,
    invTotal,
    _invAvrgCost,
    profit,
    profitRatio,
    string.Empty,
    globalId,
    procTypeId,
    storeName,
    branchName,
    profitState
);
                    }

                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }

                _currentDataTable = resultTable;
                GridControl1.ItemsSource = resultTable.DefaultView;

                // تحديث ملخصات مخصصة
                RecalculateCustomSummaries();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void UpdateCostColumnHeader()
        {
            bool isAr = IsArabic();

            if (rbAvgCost.IsChecked == true)
                colDgvCost.Header = isAr ? "متوسط التكلفة" : "Average cost";
            else if (rbPurchPrice.IsChecked == true)
                colDgvCost.Header = isAr ? "سعر الشراء" : "Purchase price";
            else if (rbLastPurchPrice.IsChecked == true)
                colDgvCost.Header = isAr ? "أخر سعر شراء" : "Last purchase price";
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Custom Summaries

        /// <summary>
        /// يحسب الملخصات المخصصة يدوياً بدلاً من GridView1_CustomSummaryCalculate
        /// لأن WPF DX يختلف في آلية الأحداث
        /// </summary>
        private void RecalculateCustomSummaries()
        {
            if (_currentDataTable == null) return;

            Total = Total1 = 0;
            Discount = Discount1 = 0;
            sum = sum1 = 0;
            Cost = Cost1 = 0;
            Profit = Profit1 = 0;

            foreach (DataRow row in _currentDataTable.Rows)
            {
                int procType = Convert.ToInt32(row["DgvProcType"]);
                bool isSale = procType == 1;

                double rowTotal = GetDouble(row["DgvTotal"]);
                double rowDiscount = GetDouble(row["DgvDiscount"]);
                double rowSum = GetDouble(row["DgvSum"]);
                double rowCost = GetDouble(row["DgvCost"]);
                double rowProfit = GetDouble(row["DgvProfit"]);

                if (isSale)
                {
                    Total += rowTotal;
                    Discount += rowDiscount;
                    sum += rowSum;
                    Cost += rowCost;
                    Profit += rowProfit;
                }
                else
                {
                    Total1 += rowTotal;
                    Discount1 += rowDiscount;
                    sum1 += rowSum;
                    Cost1 += rowCost;
                    Profit1 += rowProfit;
                }
            }

            // تحديث نص الملخصات في Footer ستعرضها DevExpress تلقائياً
            // من خلال TotalSummary التي عرّفناها في XAML بـ Custom
        }

        private static double GetDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0;
            double.TryParse(val.ToString(), out double result);
            return result;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Row Coloring


        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            string whereClause = BuildWhereClause();
            ShowInvs(whereClause);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentDataTable == null
                    || _currentDataTable.Rows.Count == 0)
                {
                    ShowInfo("لا توجد بيانات للتصدير.");
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "تصدير البيانات",
                    Filter = "CSV File (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"{Title}.csv"
                };

                if (dlg.ShowDialog() != true) return;

                ExportToCsv(dlg.FileName);
                Process.Start(new ProcessStartInfo(dlg.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnRecalculateCost_Click(object sender, RoutedEventArgs e)
            => RecalculateCost();

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Grid Cell Button

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is not DataRowView rowView)
                    return;

                string globalId = rowView["DgvInvGlobalID"]?.ToString()
                                  ?? string.Empty;
                if (string.IsNullOrWhiteSpace(globalId)) return;

                var detailForm = new frmInvoiceDetails
                {
                    InvGlobalId = globalId
                };

                if (rbAvgCost.IsChecked == true) detailForm.costPrice = 1;
                else if (rbPurchPrice.IsChecked == true) detailForm.costPrice = 2;
                else if (rbLastPurchPrice.IsChecked == true) detailForm.costPrice = 3;

                MainClass.ApplyPermissionToForm(detailForm);
                MainClass.DoApplyUserSett(detailForm);
                detailForm.ShowDialog();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Search Panel Handlers

        private void ckAllType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllType.IsChecked == true;
            cmbType.IsEnabled = !allChecked;
            if (allChecked) cmbType.SelectedIndex = -1;
        }

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllInvs.IsChecked == true;
            cmbInvType.IsEnabled = !allChecked;
            if (allChecked) cmbInvType.SelectedIndex = -1;
        }

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllStore.IsChecked == true;
            cmbStore.IsEnabled = !allChecked;
            if (allChecked) cmbStore.SelectedIndex = -1;
        }

        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllUsers.IsChecked == true;
            cmbusers.IsEnabled = !allChecked;
            if (allChecked) cmbusers.SelectedIndex = -1;
        }

        private void ckAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllClients.IsChecked == true;
            cmbClients.IsEnabled = !allChecked;
            if (allChecked) cmbClients.SelectedIndex = -1;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = chkAllBranches.IsChecked == true;
            cmbBranches.IsEnabled = !allChecked;
            if (allChecked) cmbBranches.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allPeriod = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allPeriod;
            txtToDate.IsEnabled = !allPeriod;
        }

        private void ckNonProcessed_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            btnRecalculateCost.Visibility =
                ckNonProcessed.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Build Where Clause

        private string BuildWhereClause()
        {
            string clause = string.Empty;

            // الفرع
            if (cmbBranches.SelectedIndex > -1
                && cmbBranches.SelectedValue != null)
                clause += $"inv.branch={cmbBranches.SelectedValue} AND ";

            // العميل
            if (ckAllClients.IsChecked != true
                && cmbClients.SelectedIndex > -1)
                clause += $"cust_id={cmbClients.SelectedValue} AND ";

            // المستخدم
            if (ckAllUsers.IsChecked != true
                && cmbusers.SelectedIndex > -1)
                clause += $"sales_emp={cmbusers.SelectedValue} AND ";

            // المستودع
            if (ckAllStore.IsChecked != true
                && cmbStore.SelectedIndex > -1)
                clause += $"inv.safe={cmbStore.SelectedValue} AND ";

            // نوع الفاتورة
            if (ckAllInvs.IsChecked != true)
            {
                if (cmbInvType.SelectedIndex == 0)
                    clause += "inv.inv_type=2 AND ";
                else if (cmbInvType.SelectedIndex == 1)
                    clause += "inv.inv_type=3 AND ";
            }
            else
            {
                clause += "(inv.inv_type=2 OR inv.inv_type=3) AND ";
            }

            // نوع العملية
            if (rbAllSales.IsChecked == true)
                clause += "(inv.proc_type=1 OR inv.proc_type=2) AND ";
            else if (rbSales.IsChecked == true)
                clause += "inv.proc_type=1 AND ";
            else if (rbReturn.IsChecked == true)
                clause += "inv.proc_type=2 AND ";

            // نوع الدفع
            if (ckAllType.IsChecked != true)
            {
                if (cmbType.SelectedIndex == 1)
                    clause += "inv.pay_type=-1 AND ";
                else if (cmbType.SelectedIndex == 0)
                    clause += "inv.pay_type>0 AND ";
            }

            // الأرباح
            if (rbNegativeProfit.IsChecked == true)
                clause += "inv.Invprofit<0 AND ";
            else if (rbPositiveProfit.IsChecked == true)
                clause += "inv.Invprofit>0 AND ";
            else if (rbZeroProfit.IsChecked == true)
                clause += "inv.Invprofit=0 AND ";

            // التاريخ
            if (ckTotalPeriod.IsChecked != true)
                clause += "date>=@date1 AND date<=@date2 AND ";

            return clause;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Recalculate Cost

        private void RecalculateCost()
        {
            try
            {
                string confirmMsg =
                    IsArabic()
                    ? "هل انت متأكد من إعادة احتساب التكلفة للفواتير المختارة ؟"
                    : "Are you sure to recalculate the cost for the selected invoices?";

                if (DXMessageBox.Show(confirmMsg, "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                int[] handles = GridView1.GetSelectedRowHandles();
                if (handles == null || handles.Length == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                var selectedGlobalIds = new List<string>();

                foreach (int handle in handles)
                {
                    object rowObj = GridControl1.GetRow(handle);
                    if (rowObj is DataRowView drv)
                    {
                        string gid =
                            drv["DgvInvGlobalID"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(gid))
                            selectedGlobalIds.Add(gid);
                    }
                }

                if (selectedGlobalIds.Count == 0) return;

                var invoiceOper = new InvoiceOper();
                var invoiceList = new List<Invoice>();

                foreach (string gid in selectedGlobalIds)
                {
                    Invoice inv = invoiceOper.BindInvoByID(gid);
                    if (inv != null) invoiceList.Add(inv);
                }

                if (invoiceList.Count == 0) return;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = invoiceList.Count;
                });

                int progressIdx = 0;
                foreach (Invoice inv in invoiceList)
                {
                    foreach (Item item in inv.Items)
                    {
                        double currentQty =
                            Inventory.CalcItemStock(
                                (int)Math.Round(item.Store),
                                item.ItemNo, inv.Branch);
                        double avgCost =
                            ItemOper.AvgCost(item.ItemNo, inv.Branch);

                        if (item.ValiableStock > -1.0)
                        {
                            currentQty = item.ValiableStock;
                            if (_conn.State != ConnectionState.Open)
                                _conn.Open();

                            using var cmd = new SqlCommand(
                                StoredQueries.UpdateItemCost, _conn);
                            cmd.Parameters.Add(
                                "@InvGlobalID", SqlDbType.NVarChar)
                               .Value = item.InvGlobalID;
                            cmd.Parameters.Add(
                                "@ItemId", SqlDbType.Int)
                               .Value = item.ItemNo;
                            cmd.Parameters.Add(
                                "@AvrgCost", SqlDbType.Float)
                               .Value = avgCost;
                            cmd.Parameters.Add(
                                "@CurrentQnty", SqlDbType.Float)
                               .Value = currentQty;
                            cmd.ExecuteNonQuery();
                        }
                        else if (item.ValiableStock < 0.0
                                 && currentQty > -1.0)
                        {
                            ItemOper.RecalculateCost(
                                item.ItemNo, currentQty,
                                ref avgCost,
                                $"  AND inv.InvGlobalId=N'{inv.InvGlobalID}'");
                        }
                    }

                    progressIdx++;
                    Dispatcher.Invoke(() => ProgressBar1.Value = progressIdx);
                }

                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                ShowSuccess("تم ✅");
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Print / Report

        private void PrintReport(int printMode)
        {
            _rptUrl = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName = "rptInvsProfitSales.repx";

            if (_currentDataTable == null
                || _currentDataTable.Rows.Count == 0)
            {
                ShowInfo("لا توجد عمليات بالجدول");
                return;
            }

            if (string.IsNullOrWhiteSpace(_rptUrl))
            {
                ShowInfo("يجب تحديد مسار التقرير");
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);
            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                ShowError("المسار الحالي للتقارير غير موجود أو تم تعديله");
                return;
            }

            try
            {
                var xtraReport =
                    DevExpress.XtraReports.UI.XtraReport.FromFile(fullPath);
                xtraReport.DataSource = BuildReportDataSet();

                string headerPath =
                    Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport =
                        DevExpress.XtraReports.UI.XtraReport
                                  .FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;

                    var subReport =
                        xtraReport.FindControl("headerRpt",
                                               ignoreCase: true)
                        as DevExpress.XtraReports.UI.XRSubreport;

                    if (subReport != null)
                        subReport.ReportSource = headerReport;
                }

                if (string.IsNullOrWhiteSpace(_defPrinter))
                {
                    ShowInfo("يجب تحديد الطابعة من الإعدادات");
                    return;
                }

                xtraReport.PrinterName = _defPrinter;

                if (printMode == 1)
                {
                    for (int i = 0; i < _printNo; i++)
                        xtraReport.Print();
                }
                else
                {
                    xtraReport.ShowPreviewDialog();
                }

                xtraReport.Dispose();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            if (_currentDataTable == null) return new DataSet("Name");

            string fromDate =
                (txtFromDate.SelectedDate ?? DateTime.Today).ToShortDateString();
            string toDate =
                (txtToDate.SelectedDate ?? DateTime.Today).ToShortDateString();

            foreach (DataRow row in _currentDataTable.Rows)
            {
                list.Add(new InventoryData
                {
                    InvType = row["DgvInvType"]?.ToString(),
                    InvoiceNo = row["DgvInvNo"]?.ToString(),
                    RefsInvNo = row["DgvRefNo"]?.ToString(),
                    InvDate = row["DgvDate"] is DateTime dt
                                    ? dt.ToShortDateString()
                                    : string.Empty,
                    Clint = row["DgvClient"]?.ToString(),
                    Paytype = row["DgvPayType"]?.ToString(),
                    Total = row["DgvTotal"] != DBNull.Value
                                    ? row["DgvTotal"].ToString()
                                    : "0",
                    NetDiscount = row["DgvDiscount"]?.ToString(),
                    NetCost = row["DgvCost"]?.ToString(),
                    NetTotal = row["DgvSum"]?.ToString(),
                    NetProfit = row["DgvProfit"]?.ToString(),
                    ProfitCostRatio = row["DgvProfitRatio"]?.ToString(),
                    SafeName = row["DgvStore"]?.ToString(),
                    EmpName = row["DgvUser"]?.ToString(),
                    FromDate = fromDate,
                    ToDate = toDate,
                    ProcessType = cmbType.Text,
                    InventoryType = Title,
                    Sum = (sum - sum1).ToString("0.##"),
                    SumNetDiscount = (Discount - Discount1).ToString("0.##"),
                    Total1 = (Total - Total1).ToString("0.##"),
                    SumNetProfit = (Profit - Profit1).ToString("0.##"),
                    TotCost = (Cost - Cost1).ToString("0.##"),
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            var ds = new DataSet("Name");
            var table = global::UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Export CSV

        private void ExportToCsv(string filePath)
        {
            var lines = new List<string>
            {
                string.Join(",",
                    Csv("م"), Csv("نوع الفاتورة"), Csv("رقم الفاتورة"),
                    Csv("رقم المرجع"), Csv("التاريخ"), Csv("نوع الدفع"),
                    Csv("العميل"), Csv("المستخدم"), Csv("المجموع"),
                    Csv("الخصم"), Csv("الإجمالي"), Csv("التكلفة"),
                    Csv("الربح"), Csv("نسبة الربح"), Csv("المستودع"),
                    Csv("الفرع"))
            };

            if (_currentDataTable != null)
            {
                foreach (DataRow row in _currentDataTable.Rows)
                {
                    lines.Add(string.Join(",",
                        Csv(row["DgvNo"].ToString()),
                        Csv(row["DgvInvType"].ToString()),
                        Csv(row["DgvInvNo"].ToString()),
                        Csv(row["DgvRefNo"].ToString()),
                        Csv(row["DgvDate"] is DateTime d
                            ? d.ToShortDateString()
                            : string.Empty),
                        Csv(row["DgvPayType"].ToString()),
                        Csv(row["DgvClient"].ToString()),
                        Csv(row["DgvUser"].ToString()),
                        Csv(Format(row["DgvTotal"])),
                        Csv(Format(row["DgvDiscount"])),
                        Csv(Format(row["DgvSum"])),
                        Csv(Format(row["DgvCost"])),
                        Csv(Format(row["DgvProfit"])),
                        Csv(row["DgvProfitRatio"].ToString()),
                        Csv(row["DgvStore"].ToString()),
                        Csv(row["DgvBranch"].ToString())));
                }
            }

            File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
        }

        private static string Format(object val)
        {
            if (val == null || val == DBNull.Value) return "0";
            if (double.TryParse(val.ToString(), out double d))
                return d.ToString("0.##", CultureInfo.InvariantCulture);
            return val.ToString();
        }

        private static string Csv(string value)
        {
            if (value == null) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Helpers

        private static string ResolvePaymentText(int payType)
        {
            bool isAr = IsArabic();
            return payType switch
            {
                -1 => isAr ? "آجل" : "Credit",
                1 => isAr ? "نقدي" : "Cash",
                2 => isAr ? "شبكة" : "Card",
                _ => string.Empty
            };
        }

        private static bool IsArabic()
            => string.Equals(MainClass.Language, "ar",
                             StringComparison.OrdinalIgnoreCase);

        private static void ShowInfo(string msg)
            => DXMessageBox.Show(msg, "تنبيه",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

        private static void ShowSuccess(string msg)
            => DXMessageBox.Show(msg, "نجاح",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

        private static void ShowError(string msg)
            => DXMessageBox.Show(msg, "خطأ",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

        #endregion
    }
}