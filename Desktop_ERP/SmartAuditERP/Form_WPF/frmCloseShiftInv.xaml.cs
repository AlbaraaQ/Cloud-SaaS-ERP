using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AuditorAPI.Models;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCloseShiftInv : Window
    {
        #region ── Public Fields ──────────────────────────────

        public DateTime DateTimeFrom { get; set; }
        public DateTime DateTimeTo { get; set; }
        public int CloseId { get; set; } = -1;
        public CloseShiftSetting CloseShiftSetting { get; set; }

        #endregion

        #region ── Private Fields ─────────────────────────────

        private SqlConnection conn;

        // مجاميع الأعمدة (مبيعات - مرتجعات)
        private double _sumSales, _sumReturns;
        private double _discSales, _discReturns;
        private double _totalSales, _totalReturns;
        private double _vatSales, _vatReturns;
        private double _netSales, _netReturns;
        private double _cashSales, _cashReturns;
        private double _networkSales, _networkReturns;
        private double _postPoneSales, _postPoneReturns;
        private double _hostingSales, _hostingReturns;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCloseShiftInv()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            CloseShiftSetting = new CloseShiftSetting();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowResults();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Show Results ───────────────────────────────

        private void ShowResults()
        {
            try
            {
                var rows = new List<CloseShiftInvRow>();

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch = {MainClass.BranchNo} AND "
                    : string.Empty;

                string dateFilter = $"{branchFilter} date >= @date1 AND date <= @date2";

                string invoiceTypeFilter = "inv.inv_type = 3 AND ";
                if (CloseShiftSetting.IncSaleInv)
                    invoiceTypeFilter = "(inv.inv_type = 3 OR inv.inv_type = 2 OR " +
                                        "inv.inv_type = 20) AND ";

                // جلب بيانات الإغلاق
                SqlDataAdapter closeAdapter = new SqlDataAdapter(
                    "SELECT startTime, endTime, user_id, Expenses, Purchases " +
                    "FROM CasherClosed, CasherClosed_Sub " +
                    "WHERE CasherClosed.ClosedId = CasherClosed_Sub.ClosedId " +
                    $"AND CasherClosed.ClosedId = {CloseId}",
                    conn);

                DataTable closeTable = new DataTable();
                closeAdapter.Fill(closeTable);

                if (closeTable.Rows.Count == 0)
                {
                    GridControl1.ItemsSource = rows;
                    return;
                }

                DataRow closeRow = closeTable.Rows[0];
                int employeeId = Convert.ToInt32(closeRow["user_id"]);
                string fullFilter = $"{dateFilter} AND sales_emp = {employeeId}";

                // جلب الفواتير
                SqlDataAdapter invAdapter = new SqlDataAdapter(
                    $"SELECT * FROM Inv WHERE {invoiceTypeFilter} " +
                    "(Proc_type = 1 OR proc_type = 2) AND IS_Deleted = 0 AND " +
                    $"{fullFilter} ORDER BY Inv.id",
                    conn);

                invAdapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = closeRow["startTime"];
                invAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = closeRow["endTime"];

                DataTable invTable = new DataTable();
                invAdapter.Fill(invTable);

                foreach (DataRow invRow in invTable.Rows)
                {
                    CloseShiftInvRow gridRow = BuildInvoiceRow(invRow);
                    if (gridRow != null)
                        rows.Add(gridRow);
                }

                // إضافة سندات الصرف (المصاريف)
                if (closeRow["Expenses"] != DBNull.Value)
                {
                    List<CloseShiftInvRow> expenseRows =
                        LoadExpenseRows(closeRow, branchFilter, employeeId);
                    rows.AddRange(expenseRows);
                }

                GridControl1.ItemsSource = rows;
                CalculateAndDisplaySummary(rows);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"خطأ أثناء تحميل البيانات{Environment.NewLine}{ex.Message}",
                    "❌ خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private CloseShiftInvRow BuildInvoiceRow(DataRow invRow)
        {
            try
            {
                int invType = Convert.ToInt32(invRow["inv_type"]);
                int procType = Convert.ToInt32(invRow["proc_type"]);
                string entryType = GetEntryTypeName(invType, procType);

                SqlDataAdapter subAdapter = new SqlDataAdapter(
                    "SELECT CurrentQnty, " +
                    "Inv_Sub.val1 * Inv_Sub.exchange_price AS Sum, " +
                    "ISNULL(Inv_Sub.discount, 0) AS ItDiscount, " +
                    "Inv_Sub.taxval AS Vat, " +
                    "Inv_Sub.val * Inv_Sub.AvrgCost AS AvrgCost " +
                    $"FROM Inv_Sub WHERE Inv_Sub.InvGlobalID = N'{invRow["InvGlobalID"]}'",
                    conn);

                DataTable subTable = new DataTable();
                subAdapter.Fill(subTable);

                if (subTable.Rows.Count == 0)
                    return null;

                double totalSum = 0.0;
                double totalVat = 0.0;
                double totalDisc = 0.0;

                foreach (DataRow subRow in subTable.Rows)
                {
                    totalSum += SafeDouble(subRow["Sum"]);
                    totalVat += SafeDouble(subRow["Vat"]);
                    totalDisc += SafeDouble(subRow["ItDiscount"]);
                }

                double hostingVal = 0.0;
                double postPoneVal = 0.0;

                double payType = SafeDouble(invRow["pay_type"]);
                if (payType == 5)
                    hostingVal = SafeDouble(invRow["tot_net"]);
                if (payType == -1)
                    postPoneVal = SafeDouble(invRow["tot_net"]);

                double totalDiscount = Math.Round(
                    SafeDouble(invRow["minus"]) + totalDisc, 2);

                DateTime entryDate = Convert.ToDateTime(invRow["date"]);

                return new CloseShiftInvRow
                {
                    EntryType = entryType,
                    EntryNo = Convert.ToInt32(invRow["id"]),
                    EntryDate = entryDate.ToShortDateString(),
                    EntryTime = entryDate.ToLongTimeString(),
                    SumDgv = totalSum,
                    DiscountDgv = totalDiscount,
                    TotalDgv = totalSum - totalDiscount,
                    VATDgv = Math.Round(totalVat, 2),
                    NetDgv = Math.Round(totalSum - totalDiscount + totalVat, 2),
                    CashDgv = SafeDouble(invRow["cash"]),
                    NetworkDgv = SafeDouble(invRow["visa"]),
                    PostPoneSalesDgv = postPoneVal,
                    HostingDgv = hostingVal,
                    ProcType = procType
                };
            }
            catch
            {
                return null;
            }
        }

        private static string GetEntryTypeName(int invType, int procType)
        {
            return (invType, procType) switch
            {
                (2, 1) => "فاتورة مبيعات",
                (2, 2) => "مرتجع مبيعات",
                (3, 1) => "نقطة بيع",
                (3, 2) => "مرتجع نقطة بيع",
                (20, 1) => "مبيع أندرويد",
                (20, 2) => "مرتجع أندرويد",
                _ => "نقطة بيع"
            };
        }

        private List<CloseShiftInvRow> LoadExpenseRows(
            DataRow closeRow,
            string branchFilter,
            int employeeId)
        {
            var expenseRows = new List<CloseShiftInvRow>();

            try
            {
                SqlDataAdapter expAdapter = new SqlDataAdapter(
                    $"SELECT * FROM Receipts " +
                    $"WHERE BranchID = {MainClass.BranchNo} " +
                    $"AND EmpId = {employeeId} " +
                    "AND ReceiptDate >= @date1 AND ReceiptDate <= @date2 " +
                    "AND ISDeleted = 0 AND ReceiptType = 8",
                    conn);

                expAdapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = closeRow["startTime"];
                expAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = closeRow["endTime"];

                DataTable expTable = new DataTable();
                expAdapter.Fill(expTable);

                foreach (DataRow expRow in expTable.Rows)
                {
                    double netVal = Math.Round(SafeDouble(expRow["NetVal"]), 2);
                    DateTime receiptDate = Convert.ToDateTime(expRow["ReceiptDate"]);

                    expenseRows.Add(new CloseShiftInvRow
                    {
                        EntryType = "سند صرف",
                        EntryNo = Convert.ToInt32(expRow["ReceiptNo"]),
                        EntryDate = receiptDate.ToShortDateString(),
                        EntryTime = receiptDate.ToLongTimeString(),
                        SumDgv = netVal,
                        DiscountDgv = 0,
                        TotalDgv = netVal,
                        VATDgv = 0,
                        NetDgv = netVal,
                        CashDgv = netVal,
                        NetworkDgv = 0,
                        PostPoneSalesDgv = 0,
                        HostingDgv = 0,
                        ProcType = 1
                    });
                }
            }
            catch { }

            return expenseRows;
        }

        #endregion

        #region ── Summary Calculation ────────────────────────

        private void CalculateAndDisplaySummary(List<CloseShiftInvRow> rows)
        {
            ResetSummaryTotals();

            foreach (CloseShiftInvRow row in rows)
            {
                bool isSale = row.ProcType == 1;

                if (isSale)
                {
                    _sumSales += row.SumDgv;
                    _discSales += row.DiscountDgv;
                    _totalSales += row.TotalDgv;
                    _vatSales += row.VATDgv;
                    _netSales += row.NetDgv;
                    _cashSales += row.CashDgv;
                    _networkSales += row.NetworkDgv;
                    _postPoneSales += row.PostPoneSalesDgv;
                    _hostingSales += row.HostingDgv;
                }
                else
                {
                    _sumReturns += row.SumDgv;
                    _discReturns += row.DiscountDgv;
                    _totalReturns += row.TotalDgv;
                    _vatReturns += row.VATDgv;
                    _netReturns += row.NetDgv;
                    _cashReturns += row.CashDgv;
                    _networkReturns += row.NetworkDgv;
                    _postPoneReturns += row.PostPoneSalesDgv;
                    _hostingReturns += row.HostingDgv;
                }
            }

            lblSumTotal.Text = FormatNum(_sumSales - _sumReturns);
            lblDiscountTotal.Text = FormatNum(_discSales - _discReturns);
            lblTotalTotal.Text = FormatNum(_totalSales - _totalReturns);
            lblVATTotal.Text = FormatNum(_vatSales - _vatReturns);
            lblNetTotal.Text = FormatNum(_netSales - _netReturns);
            lblCashTotal.Text = FormatNum(_cashSales - _cashReturns);
            lblNetworkTotal.Text = FormatNum(_networkSales - _networkReturns);
            lblPostPoneTotal.Text = FormatNum(_postPoneSales - _postPoneReturns);
            lblHostingTotal.Text = FormatNum(_hostingSales - _hostingReturns);
        }

        private void ResetSummaryTotals()
        {
            _sumSales = _sumReturns = 0.0;
            _discSales = _discReturns = 0.0;
            _totalSales = _totalReturns = 0.0;
            _vatSales = _vatReturns = 0.0;
            _netSales = _netReturns = 0.0;
            _cashSales = _cashReturns = 0.0;
            _networkSales = _networkReturns = 0.0;
            _postPoneSales = _postPoneReturns = 0.0;
            _hostingSales = _hostingReturns = 0.0;
        }

        private static string FormatNum(double value) =>
            $"{value:0.##}";

        private static double SafeDouble(object value)
        {
            try { return Convert.ToDouble(value); }
            catch { return 0.0; }
        }

        #endregion
    }

    #region ── Row Model ──────────────────────────────────────

    public class CloseShiftInvRow
    {
        public string EntryType { get; set; }
        public int EntryNo { get; set; }
        public string EntryDate { get; set; }
        public string EntryTime { get; set; }
        public double SumDgv { get; set; }
        public double DiscountDgv { get; set; }
        public double TotalDgv { get; set; }
        public double VATDgv { get; set; }
        public double NetDgv { get; set; }
        public double CashDgv { get; set; }
        public double NetworkDgv { get; set; }
        public double PostPoneSalesDgv { get; set; }
        public double HostingDgv { get; set; }
        public int ProcType { get; set; }
    }

    #endregion
}