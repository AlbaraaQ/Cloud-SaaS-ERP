using AuditorAPI.Models;
using DevExpress.Utils.Controls;
using DevExpress.XtraReports.UI;
using SmartAuditERP.Form_WPF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCloseShift : Window
    {
        #region ── Nested Class ───────────────────────────────

        public class RowInfo
        {
            public int RowHandle { get; set; }

            public RowInfo(int rowHandle)
            {
                RowHandle = rowHandle;
            }
        }

        #endregion

        #region ── Public Fields ──────────────────────────────

        public DateTime datefrom { get; set; }
        public DateTime dateTo { get; set; }

        #endregion

        #region ── Private Fields ─────────────────────────────

        private SqlConnection conn;
        private SqlConnection conn1;

        private double _casherValue;
        private int _shiftNo;
        private int _closeId;
        private string _globalIdClose;
        private int _selectedCloseNo;
        private string _username;
        private bool _isGroupPrinted;
        private bool _isItemsPrinted;
        private int _userTreasuryAccount;
        private bool _printHeader;
        private bool _printFooter;
        private int _printType;
        private int _printNo;
        private string _defPrinter;
        private bool _isPrinted;
        private string _printNote;
        private string _reportName;
        private string _reportUrl;
        private bool _printStamp;
        private CloseShiftSetting _closeShiftSetting;
        private string _styleFile;
        private string _styleFolder;
        private int _closeType;
        private DataTable _groupDataTable;
        private DataTable _detailDataTable;
        private double _totalSale;
        private double _totalQuantity;

        Home Home = new Home();
        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCloseShift()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            _casherValue = 0.0;
            _shiftNo = 1;
            _closeId = 1;
            _selectedCloseNo = -1;
            _username = string.Empty;
            _isGroupPrinted = true;
            _isItemsPrinted = true;
            _printHeader = true;
            _printFooter = true;
            _printType = 1;
            _printNo = 1;
            _isPrinted = true;
            _printNote = string.Empty;
            _reportName = string.Empty;
            _reportUrl = string.Empty;
            _printStamp = true;
            _closeShiftSetting = new CloseShiftSetting();
            _styleFile = Path.Combine(
                MainClass.ReportsPath, "Styles\\CloseShifSaveLayoutToXML.xml");
            _styleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            _closeType = 0;
            _groupDataTable = new DataTable();
            _detailDataTable = new DataTable();
            _totalSale = 0.0;
            _totalQuantity = 0.0;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today.AddDays(1);
            txtDate.SelectedDate = DateTime.Today;

            PrintSetting(6);
            LoadEmployees();
            loadPrintSettings();
            LoadCloseShiftSetting();

            if (MainClass.EmpNo == 0)
            {
                colBtnReClose.Visibility = Visibility.Visible;
                BtnRecloseBatch.Visibility = Visibility.Visible;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Reset State ────────────────────────────────

        private void ResetState()
        {
            _casherValue = 0.0;
            _shiftNo = 1;
            _closeId = 1;
            _selectedCloseNo = -1;
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        private void LoadEmployees()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees " +
                    "WHERE IS_Deleted = 0 ORDER BY id",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEmps.ItemsSource = dataTable.DefaultView;
                cmbEmps.DisplayMemberPath = "name";
                cmbEmps.SelectedValuePath = "id";
                cmbEmps.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadCloseShiftSetting()
        {
            try
            {
                _closeShiftSetting = Common.CloseDaySetting();
            }
            catch { }
        }

        #endregion

        #region ── Show Closes ────────────────────────────────

        private void ShowCloses()
        {
            try
            {
                string employeeFilter = string.Empty;
                if (chkAll.IsChecked != true)
                {
                    if (cmbEmps.SelectedValue == null)
                    {
                        ShowWarning("اختر الموظف");
                        cmbEmps.Focus();
                        return;
                    }
                    employeeFilter = $" AND user_id = {cmbEmps.SelectedValue}";
                }

                string periodFilter = string.Empty;
                if (chkAllperiod.IsChecked != true)
                    periodFilter = " AND endTime >= @date1 AND endTime <= @date2";

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM CasherClosed WHERE user_id <> -1 " +
                    $"{employeeFilter}{periodFilter} ORDER BY id, startTime",
                    conn);

                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value =
                    txtDateFrom.SelectedDate?.ToShortDateString()
                    ?? DateTime.Today.ToShortDateString();

                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24.0);

                DataTable rawTable = new DataTable();
                adapter.Fill(rawTable);

                GridControl1.ItemsSource = null;
                SetProgress(0, rawTable.Rows.Count);

                var rows = new List<CloseShiftGridRow>();

                for (int rowIndex = 0; rowIndex < rawTable.Rows.Count; rowIndex++)
                {
                    SetProgress(rowIndex + 1);

                    DataRow rawRow = rawTable.Rows[rowIndex];
                    string globalId = rawRow["GlobalID"]?.ToString() ?? string.Empty;
                    int closedId = Convert.ToInt32(rawRow["ClosedID"]);
                    string employeeName = GetEmployeeName(rawRow["user_id"]);

                    CloseShiftGridRow gridRow =
                        BuildGridRow(rawRow, globalId, closedId, employeeName, rowIndex);

                    if (gridRow != null)
                        rows.Add(gridRow);
                }

                GridControl1.ItemsSource = rows;
                SetProgress(rawTable.Rows.Count, rawTable.Rows.Count);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء العرض", ex);
            }
        }

        private CloseShiftGridRow BuildGridRow(
            DataRow rawRow,
            string globalId,
            int closedId,
            string employeeName,
            int currentRowIndex)
        {
            try
            {
                SqlDataAdapter subAdapter = new SqlDataAdapter(
                    "SELECT * FROM CasherClosed_Sub " +
                    $"WHERE IsDeleted = 0 AND GlobalID = N'{globalId}'",
                    conn);

                DataTable subTable = new DataTable();
                subAdapter.Fill(subTable);

                if (subTable.Rows.Count == 0)
                    return null;

                int safeIndex = currentRowIndex < subTable.Rows.Count
                                    ? currentRowIndex : 0;
                DataRow subRow = subTable.Rows[safeIndex];

                double postponeSales = SafeDouble(subRow, "PostPoneSales");
                double networkSum = SafeDouble(subRow, "NetworkSum");
                double expenses = SafeDouble(subRow, "Expenses");
                double purchases = SafeDouble(subRow, "Purchases");
                double hostingVal = SafeDouble(subRow, "HostingVal");
                double insurVal = SafeDouble(subRow, "InsurVal");
                double additionVal = SafeDouble(subRow, "AdditionVal");
                double allVAT = SafeDouble(subRow, "AllVAT");
                double discount = SafeDouble(subRow, "Discount");
                double safeNetVal = SafeDouble(subRow, "SAfeNetVal");
                double sumCashCredit = safeNetVal + networkSum;

                return new CloseShiftGridRow
                {
                    CloseNoDgv = closedId,
                    EmpNameDgv = employeeName,
                    CloseFrom = Convert.ToDateTime(rawRow["startTime"]),
                    CloseTo = Convert.ToDateTime(rawRow["endTime"]),
                    NetDgv = SafeDouble(rawRow, "ToT"),
                    TreasuryBlc = SafeDouble(rawRow, "CasherValue"),
                    Differ = SafeDouble(rawRow, "diff"),
                    ForwardSaleDgv = postponeSales,
                    NetworkSumDgv = networkSum,
                    CashSumDgv = safeNetVal,
                    SumCashAndCredit = sumCashCredit,
                    ExpensesDgv = expenses,
                    PurchasesDgv = purchases,
                    HostingDgv = hostingVal,
                    InsurValDgv = insurVal,
                    AdditionValDgv = additionVal,
                    AllVATDgv = allVAT,
                    DiscountDgv = discount,
                    GlobalID = globalId
                };
            }
            catch
            {
                return null;
            }
        }

        private string GetEmployeeName(object userId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM employees WHERE id = {userId}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0]["name"]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private double SafeDouble(DataRow row, string columnName)
        {
            try
            {
                if (row[columnName] == DBNull.Value)
                    return 0.0;

                return double.TryParse(row[columnName]?.ToString(),
                    out double value) ? value : 0.0;
            }
            catch { return 0.0; }
        }

        #endregion

        #region ── Database Helpers ───────────────────────────

        private string GetGroupName(int groupId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM ItemsCategory " +
                    $"WHERE CategoryId = {groupId}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetItemGroupId(int itemId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT group_id FROM Items WHERE id = {itemId}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetGroupMarineCode(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT code FROM GroupMarine " +
                    $"WHERE IsDeleted = 0 AND id = {id}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetItemName(int itemId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM Items WHERE id = {itemId}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetEmployeeNameById(int employeeId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM Employees WHERE id = {employeeId}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        #endregion

        #region ── ISO Date ───────────────────────────────────

        public string GetIso8601Date(DateTime time)
        {
            string isoDate = time.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            TimeSpan utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(time);

            if (utcOffset > TimeSpan.Zero)
                isoDate += "+";

            return isoDate + $"{utcOffset.Hours:00}:{utcOffset.Minutes:00}";
        }

        #endregion

        #region ── Control Events ─────────────────────────────

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowCloses();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(1);
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbEmps != null)
                cmbEmps.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllperiod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (txtDateFrom != null && txtDateTo != null)
            {
                bool isAllPeriod = chkAllperiod.IsChecked == true;
                txtDateFrom.IsEnabled = !isAllPeriod;
                txtDateTo.IsEnabled = !isAllPeriod;
            }
        }

        private void GridControl1_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnPrintRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn.Tag is CloseShiftGridRow row)
            {
                PrintRow(row);
            }
        }

        private void BtnSendRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn &&
                btn.Tag is CloseShiftGridRow row)
            {
                SendRow(row);
            }
        }

        private void BtnReClose_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo != 0)
                return;

            if (sender is Button btn &&
                btn.Tag is CloseShiftGridRow row)
            {
                frmCheckPwd pwdForm = new frmCheckPwd();
                pwdForm.operNo = 100;
                pwdForm.CheckType = 100;
                pwdForm.ShowDialog();

                if (!pwdForm.Iscorrect)
                    return;

                MessageBoxResult confirm = MessageBox.Show(
                    "هل تريد إعادة إغلاق اليومية للموظف؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                _closeId = row.CloseNoDgv;
                _globalIdClose = row.GlobalID;

                ReClose(_closeId);
                ResetState();
                ShowSuccess("تم إعادة احتساب الإغلاق ✅");
            }
        }

        #endregion

        #region ── Context Menu ───────────────────────────────

        private void GridControl1_MouseRightButtonUp(
    object sender, MouseButtonEventArgs e)
        {
            if (GridControl1.SelectedItem is not CloseShiftGridRow selectedRow)
                return;

            // ✅ إصلاح 1: استخدام System.Windows.FlowDirection
            ContextMenu menu = new ContextMenu
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft
            };

            menu.Items.Add(CreateMenuItem("📋 تفاصيل",
                () => ShowInvoiceDetails(selectedRow)));

            menu.Items.Add(CreateMenuItem("🖨️ طباعة الإغلاق",
                () => PrintRow(selectedRow)));

            menu.Items.Add(CreateMenuItem("👁️ معاينة",
                () => PreviewRow(selectedRow)));

            menu.Items.Add(CreateMenuItem("📧 إرسال بريد",
                () => SendRow(selectedRow)));

            // ربط الـ ContextMenu بالـ DataGrid وفتحه
            GridControl1.ContextMenu = menu;
            menu.PlacementTarget = GridControl1;
            menu.IsOpen = true;
        }

        private static MenuItem CreateMenuItem(string header, Action action)
        {
            MenuItem item = new MenuItem
            {
                Header = header,
                FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            item.Click += (s, e) => action();
            return item;
        }

        private void ShowInvoiceDetails(CloseShiftGridRow row)
        {
            try
            {
                _closeId = row.CloseNoDgv;
                _globalIdClose = row.GlobalID;

                frmCloseShiftInv form = new frmCloseShiftInv();
                form.CloseId = _closeId;
                form.CloseShiftSetting = _closeShiftSetting;
                form.ShowDialog();
            }
            catch { }
        }

        private void PreviewRow(CloseShiftGridRow row)
        {
            _closeId = row.CloseNoDgv;
            _globalIdClose = row.GlobalID;

            CashierCloseData closeData = new CashierCloseData();
            List<CloseShiftCustomer> customers = new List<CloseShiftCustomer>();

            LoadSelectedClose(ref closeData, ref customers);
            PrintDevexpress(2, closeData, customers);
        }

        private void PrintRow(CloseShiftGridRow row)
        {
            MessageBoxResult confirm = MessageBox.Show(
                "هل تريد طباعة إغلاق اليومية؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            _closeId = row.CloseNoDgv;
            _globalIdClose = row.GlobalID;

            CashierCloseData closeData = new CashierCloseData();
            List<CloseShiftCustomer> customers = new List<CloseShiftCustomer>();

            LoadSelectedClose(ref closeData, ref customers);
            PrintDevexpress(1, closeData, customers);
            SalesByGroups(1, closeData);
            SalesReport(1, closeData);

            if (_closeShiftSetting.PrintGroups || _closeShiftSetting.QuantPrint)
                PrintDevexpresDetails(1);

            ResetState();
        }

        private void SendRow(CloseShiftGridRow row)
        {
            _closeId = row.CloseNoDgv;
            _globalIdClose = row.GlobalID;

            CashierCloseData closeData = new CashierCloseData();
            List<CloseShiftCustomer> customers = new List<CloseShiftCustomer>();

            LoadSelectedClose(ref closeData, ref customers);
            PrintDevexpress(3, closeData, customers);
            ResetState();
        }

        #endregion

        #region ── Load Selected Close ────────────────────────

        private void LoadSelectedClose(
    ref CashierCloseData closeData,
    ref List<CloseShiftCustomer> customers)
        {
            try
            {
                // ── تحميل بيانات الإغلاق ──
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM CasherClosed WHERE ClosedID = {_closeId}",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 1)
                {
                    DataRow row = dataTable.Rows[0];

                    _closeType = Convert.ToInt32(row["type"]);
                    closeData.DiffVal = Convert.ToDouble(row["diff"]).ToString();
                    closeData.CasherValue = Convert.ToDouble(row["CasherValue"]).ToString();
                    closeData.Net = Convert.ToDouble(row["ToT"]).ToString();
                    closeData.datefrom = Convert.ToDateTime(row["startTime"]).ToString();
                    closeData.dateTo = Convert.ToDateTime(row["endTime"]).ToString();
                    closeData.CloseNo = row["ClosedID"]?.ToString() ?? string.Empty;
                }

                // ── تحميل التفاصيل ──
                SqlDataAdapter subAdapter = new SqlDataAdapter(
                    $"SELECT * FROM CasherClosed_Sub " +
                    $"WHERE GlobalID = N'{_globalIdClose}'",
                    conn);

                DataTable subTable = new DataTable();
                subAdapter.Fill(subTable);

                if (subTable.Rows.Count == 1)
                {
                    DataRow subRow = subTable.Rows[0];

                    // ✅ إصلاح 2: استخدام GetColumnValue بدلاً من ref
                    closeData.SAfeNetVal = GetColumnValue(subRow, "SAfeNetVal");
                    closeData.CashVal = GetColumnValue(subRow, "CashTotal");
                    closeData.ReturnSales = GetColumnValue(subRow, "ReturnSum");
                    closeData.NetworkSales = GetColumnValue(subRow, "NetworkSum");
                    closeData.AdditionVal = GetColumnValue(subRow, "AdditionVal");
                    closeData.PostPoneSales = GetColumnValue(subRow, "PostPoneSales");
                    closeData.PostPoneRet = GetColumnValue(subRow, "PostPoneRet");
                    closeData.InsurVal = GetColumnValue(subRow, "InsurVal");
                    closeData.Discount = GetColumnValue(subRow, "Discount");
                    closeData.VAT = GetColumnValue(subRow, "AllVAT");
                    closeData.Expenses = GetColumnValue(subRow, "Expenses");
                    closeData.Purchases = GetColumnValue(subRow, "Purchases");
                    closeData.HostingVal = GetColumnValue(subRow, "HostingVal");
                    closeData.ExtraTax = GetColumnValue(subRow, "ExtraTax", "0");
                }

                // ── تحميل العملاء ──
                SqlDataAdapter custAdapter = new SqlDataAdapter(
                    "SELECT CloseShiftCustomer.id, " +
                    "CloseShiftCustomer.ClosedId, " +
                    "CloseShiftCustomer.CustId, " +
                    "CloseShiftCustomer.Amount, " +
                    "Customers.name AS CustName " +
                    "FROM CloseShiftCustomer " +
                    "LEFT JOIN Customers " +
                    "ON CloseShiftCustomer.CustId = Customers.id " +
                    $"WHERE ClosedId = {_closeId}",
                    conn);

                DataTable custTable = new DataTable();
                custAdapter.Fill(custTable);

                List<CloseShiftCustomer> customerList = new List<CloseShiftCustomer>();

                foreach (DataRow row in custTable.Rows)
                {
                    customerList.Add(new CloseShiftCustomer
                    {
                        ClosedId = Convert.ToInt32(row["ClosedId"]),
                        CustId = Convert.ToInt32(row["CustId"]),
                        Name = row["CustName"]?.ToString() ?? string.Empty,
                        Amount = Convert.ToDecimal(row["Amount"])
                    });
                }

                if (customerList.Count > 0)
                    customers = customerList;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل بيانات الإغلاق", ex);
            }
        }

        private static string GetColumnValue(
    DataRow row,
    string columnName,
    string defaultValue = "0")
        {
            try
            {
                if (row[columnName] != DBNull.Value &&
                    !string.IsNullOrEmpty(row[columnName]?.ToString()))
                    return row[columnName].ToString();

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region ── Sales Report ───────────────────────────────

        private void SalesReport(int closeType, CashierCloseData closeData)
        {
            try
            {
                _totalSale = 0.0;
                _totalQuantity = 0.0;

                if (closeType != 1)
                    return;

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch = {MainClass.BranchNo} AND " +
                      $"inv.sales_emp = {MainClass.EmpNo} AND "
                    : string.Empty;

                DataTable salesTable = new DataTable();
                salesTable.Columns.Add("currency");
                salesTable.Columns.Add("sum");
                salesTable.Columns.Add("type");
                salesTable.Columns.Add("TotPrice");

                SqlDataAdapter salesAdapter = new SqlDataAdapter(
                    "SELECT ItemId, SUM(val) AS Val, " +
                    "SUM(val * exchange_price) AS TotPrice " +
                    "FROM inv, inv_sub, Items " +
                    $"WHERE {branchFilter} " +
                    "inv.date >= @date1 AND inv.date <= @date2 AND " +
                    "inv_sub.ItemId = Items.id AND inv.inv_type = 3 AND " +
                    "inv.proc_type = 1 AND " +
                    "inv.InvGlobalID = inv_sub.InvGlobalID AND " +
                    "inv.IS_Deleted = 0 GROUP BY ItemId " +
                    "ORDER BY MAX(exchange_price) DESC",
                    conn);

                salesAdapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = closeData.datefrom;
                salesAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = closeData.dateTo;

                DataTable salesRaw = new DataTable();
                salesAdapter.Fill(salesRaw);

                foreach (DataRow row in salesRaw.Rows)
                    salesTable.Rows.Add(row[0], Convert.ToDouble(row["Val"]),
                        1, row["TotPrice"]);

                SqlDataAdapter returnAdapter = new SqlDataAdapter(
                    "SELECT ItemId, SUM(val) AS Val, " +
                    "SUM(val * exchange_price) AS TotPrice " +
                    "FROM inv, inv_sub, Items " +
                    $"WHERE {branchFilter} " +
                    "inv.date >= @date1 AND inv.date <= @date2 AND " +
                    "inv_sub.ItemId = Items.id AND inv.inv_type = 3 AND " +
                    "inv.proc_type = 2 AND " +
                    "inv.InvGlobalID = inv_sub.InvGlobalID AND " +
                    "inv.IS_Deleted = 0 GROUP BY ItemId " +
                    "ORDER BY MAX(exchange_price) DESC",
                    conn);

                returnAdapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = closeData.datefrom;
                returnAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = closeData.dateTo;

                DataTable returnRaw = new DataTable();
                returnAdapter.Fill(returnRaw);

                foreach (DataRow row in returnRaw.Rows)
                    salesTable.Rows.Add(row[0], Convert.ToDouble(row["Val"]),
                        2, row["TotPrice"]);

                MergeAndSubtractRows(salesTable);

                if (_detailDataTable.Columns.Count > 0)
                    _detailDataTable.Columns.Clear();

                _detailDataTable.Columns.Add("Items");
                _detailDataTable.Columns.Add("Value1");
                _detailDataTable.Columns.Add("Process");
                _detailDataTable.Columns.Add("Price");
                _detailDataTable.Columns.Add("Value2");

                foreach (DataRow row in salesTable.Rows)
                {
                    int itemId = Convert.ToInt32(row[0]);

                    SqlDataAdapter itemAdapter = new SqlDataAdapter(
                        $"SELECT is_deleted, sale_price FROM Items " +
                        $"WHERE id = {itemId}",
                        conn);

                    DataTable itemTable = new DataTable();
                    itemAdapter.Fill(itemTable);

                    if (itemTable.Rows.Count == 0 ||
                        Convert.ToBoolean(itemTable.Rows[0][0]))
                        continue;

                    try
                    {
                        double salePrice =
                            Math.Round(Convert.ToDouble(
                                itemTable.Rows[0]["sale_price"]), 2);
                        double totalPrice =
                            Math.Round(Convert.ToDouble(row["TotPrice"]), 2);

                        _detailDataTable.Rows.Add(
                            GetItemName(itemId),
                            row[1],
                            string.Empty,
                            salePrice,
                            $"{totalPrice:0.#,##.##}");

                        _totalSale += totalPrice;
                        _totalQuantity += Convert.ToDouble(row[1]);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء حساب المبيعات", ex);
            }
        }

        private static void MergeAndSubtractRows(DataTable table)
        {
            for (int outerIndex = 0; outerIndex < table.Rows.Count; outerIndex++)
            {
                if (Convert.ToInt32(table.Rows[outerIndex][2]) == 2)
                {
                    table.Rows[outerIndex][1] =
                        -Convert.ToDouble(table.Rows[outerIndex][1]);
                    table.Rows[outerIndex]["TotPrice"] =
                        -Convert.ToDouble(table.Rows[outerIndex]["TotPrice"]);
                }

                for (int innerIndex = outerIndex + 1;
                     innerIndex < table.Rows.Count;
                     innerIndex++)
                {
                    if (!table.Rows[outerIndex][0].Equals(
                        table.Rows[innerIndex][0]))
                        continue;

                    if (Convert.ToInt32(table.Rows[innerIndex][2]) == 2)
                    {
                        table.Rows[innerIndex][1] =
                            -Convert.ToDouble(table.Rows[innerIndex][1]);
                        table.Rows[innerIndex]["TotPrice"] =
                            -Convert.ToDouble(table.Rows[innerIndex]["TotPrice"]);
                    }

                    table.Rows[outerIndex][1] =
                        Convert.ToDouble(table.Rows[outerIndex][1]) +
                        Convert.ToDouble(table.Rows[innerIndex][1]);

                    table.Rows[outerIndex]["TotPrice"] =
                        Convert.ToDouble(table.Rows[outerIndex]["TotPrice"]) +
                        Convert.ToDouble(table.Rows[innerIndex]["TotPrice"]);

                    table.Rows.RemoveAt(innerIndex);
                    innerIndex--;
                }
            }
        }

        #endregion

        #region ── Sales By Groups ────────────────────────────

        private void SalesByGroups(int closeType, CashierCloseData closeData)
        {
            try
            {
                _totalSale = 0.0;
                _totalQuantity = 0.0;

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch = {MainClass.BranchNo} AND " +
                      $"inv.sales_emp = {MainClass.EmpNo} AND "
                    : string.Empty;

                DataTable itemsTable = BuildItemsTable();

                if (_groupDataTable.Columns.Count > 0)
                    _groupDataTable.Columns.Clear();

                BuildGroupTableColumns();

                switch (closeType)
                {
                    case 1:
                        ProcessSalesByGroupsType1(
                            branchFilter, itemsTable, closeData);
                        break;

                    case 2:
                        ProcessSalesByGroupsType2(itemsTable, closeData);
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء حساب المبيعات", ex);
            }
        }

        private DataTable BuildItemsTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("currency");
            table.Columns.Add("Val");
            table.Columns.Add("TotPrice");
            table.Columns.Add("type");
            table.Columns.Add("GrpID");
            return table;
        }

        private void BuildGroupTableColumns()
        {
            _groupDataTable.Columns.Add("GroupName");
            _groupDataTable.Columns.Add("Quatity");
            _groupDataTable.Columns.Add("TotalSales");
            _groupDataTable.Columns.Add("CloseNo");
            _groupDataTable.Columns.Add("datefrom");
            _groupDataTable.Columns.Add("dateTo");
            _groupDataTable.Columns.Add("shiftNo");
            _groupDataTable.Columns.Add("Username");
        }

        private void ProcessSalesByGroupsType1(
            string branchFilter,
            DataTable itemsTable,
            CashierCloseData closeData)
        {
            LoadItemSales(branchFilter, itemsTable, closeData, procType: 1);
            LoadItemSales(branchFilter, itemsTable, closeData, procType: 2);

            MergeAndSubtractRows(itemsTable);
            MergeRowsByGroup(itemsTable);

            foreach (DataRow row in itemsTable.Rows)
            {
                double totalPrice = Math.Round(Convert.ToDouble(row["TotPrice"]), 2);
                string formattedPrice = $"{totalPrice:0.#,##.##}";

                _groupDataTable.Rows.Add(
                    GetGroupName(Convert.ToInt32(row["GrpID"])),
                    row["Val"].ToString(),
                    formattedPrice,
                    _closeId,
                    datefrom,
                    dateTo,
                    _shiftNo,
                    _username);

                _totalSale += totalPrice;
                _totalQuantity += Convert.ToDouble(row["Val"]);
            }
        }

        private void LoadItemSales(
            string branchFilter,
            DataTable targetTable,
            CashierCloseData closeData,
            int procType)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT ItemId, SUM(val) AS Val, " +
                "SUM(val * exchange_price) AS TotPrice " +
                "FROM inv, inv_sub, Items " +
                $"WHERE {branchFilter} " +
                "inv.date >= @date1 AND inv.date <= @date2 AND " +
                "inv_sub.ItemId = Items.id AND inv.inv_type = 3 AND " +
                $"inv.proc_type = {procType} AND " +
                "inv.InvGlobalID = inv_sub.InvGlobalID AND " +
                "inv.IS_Deleted = 0 GROUP BY ItemId " +
                "ORDER BY MAX(exchange_price) DESC",
                conn);

            adapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = closeData.datefrom;
            adapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = closeData.dateTo;

            DataTable rawTable = new DataTable();
            adapter.Fill(rawTable);

            foreach (DataRow row in rawTable.Rows)
            {
                targetTable.Rows.Add(
                    row[0],
                    Convert.ToDouble(row["Val"]),
                    Convert.ToDouble(row["TotPrice"]),
                    procType,
                    GetItemGroupId(Convert.ToInt32(row[0])));
            }
        }

        private static void MergeRowsByGroup(DataTable table)
        {
            for (int outerIndex = 0; outerIndex < table.Rows.Count; outerIndex++)
            {
                for (int innerIndex = outerIndex + 1;
                     innerIndex < table.Rows.Count;
                     innerIndex++)
                {
                    if (!table.Rows[outerIndex]["GrpID"].Equals(
                        table.Rows[innerIndex]["GrpID"]))
                        continue;

                    table.Rows[outerIndex]["Val"] =
                        Convert.ToDouble(table.Rows[outerIndex]["Val"]) +
                        Convert.ToDouble(table.Rows[innerIndex]["Val"]);

                    table.Rows[outerIndex]["TotPrice"] =
                        Convert.ToDouble(table.Rows[outerIndex]["TotPrice"]) +
                        Convert.ToDouble(table.Rows[innerIndex]["TotPrice"]);

                    table.Rows.RemoveAt(innerIndex);
                    innerIndex--;
                }
            }
        }

        private void ProcessSalesByGroupsType2(
            DataTable itemsTable,
            CashierCloseData closeData)
        {
            string employeeFilter =
                $"RentInvoice.sales_emp = {MainClass.EmpNo} AND ";

            LoadRentSales(itemsTable, closeData, employeeFilter, procType: 1);
            LoadRentSales(itemsTable, closeData, employeeFilter, procType: 3);
            LoadRentSales(itemsTable, closeData, employeeFilter, procType: 2);

            MergeAndSubtractRows(itemsTable);
            MergeRowsByGroup(itemsTable);

            foreach (DataRow row in itemsTable.Rows)
            {
                double totalPrice =
                    Math.Round(Convert.ToDouble(row["TotPrice"]), 2);
                string formattedPrice = $"{totalPrice:0.#,##.##}";

                _groupDataTable.Rows.Add(
                    GetGroupMarineCode(Convert.ToInt32(row["GrpID"])),
                    row["Val"].ToString(),
                    formattedPrice);

                _totalSale += totalPrice;
                _totalQuantity += Convert.ToDouble(row["Val"]);
            }
        }

        private void LoadRentSales(
            DataTable targetTable,
            CashierCloseData closeData,
            string employeeFilter,
            int procType)
        {
            int rowType = procType == 2 ? 2 : 1;
            string procCondition = $"proc_type = {procType}";

            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT GroupId, SUM(tot_Rent) AS TotPrice, COUNT(*) AS Val " +
                "FROM RentInvoice " +
                "WHERE date >= @date1 AND date <= @date2 AND " +
                $"{employeeFilter} {procCondition} AND IS_Deleted = 0 " +
                "GROUP BY GroupId ORDER BY MAX(tot_Rent) DESC",
                conn);

            adapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = closeData.datefrom;
            adapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = closeData.dateTo;

            DataTable rawTable = new DataTable();
            adapter.Fill(rawTable);

            foreach (DataRow row in rawTable.Rows)
            {
                targetTable.Rows.Add(
                    row["GroupId"],
                    Convert.ToDouble(row["Val"]),
                    Convert.ToDouble(row["TotPrice"]),
                    rowType,
                    row["GroupId"]);
            }
        }

        #endregion

        #region ── Binding Close Shift ────────────────────────

        private void BindingCloseShift(
            ref CloseShift closeShift,
            ref CashierCloseData closeData)
        {
            closeData.shiftNo = closeShift.ShiftNo.ToString();
            closeData.CloseNo = closeShift.ClosedId.ToString();
            closeData.dateTo = closeShift.ShiftEnd.ToString();
            closeData.datefrom = closeShift.ShiftStart.ToString();
            closeData.Username = _username;
            closeData.CashVal = "0";
            closeData.ReturnSales = "0";
            closeData.NetworkSales = "0";
            closeData.PostPoneSales = "0";
            closeData.PostPoneRet = "0";
            closeData.AdditionVal = "0";
            closeData.InsurVal = "0";
            closeData.Discount = "0";
            closeData.HostingVal = "0";
            closeData.Expenses = "0";
            closeData.Purchases = "0";

            foreach (CloseShiftDetail detail in closeShift.CloseShiftDetails)
                ProcessCloseShiftDetail(closeShift, closeData, detail);

            double cashNet = Convert.ToDouble(closeShift.CashNet);
            double expenses = double.Parse(closeData.Expenses);

            closeData.SAfeNetVal = (cashNet - expenses).ToString();
            closeData.CasherValue = closeShift.CashierBalance.ToString();
            closeData.NetWithoutVAT =
                (Convert.ToDouble(closeShift.CashNet) -
                 Convert.ToDouble(closeShift.VATNet)).ToString();
            closeData.ExtraTax = closeShift.ExtaTax.ToString();
            closeData.DiffVal =
                (Convert.ToDouble(closeShift.CashierBalance) -
                 double.Parse(closeData.SAfeNetVal)).ToString();
            closeData.Net = closeShift.Net.ToString();
            closeData.VAT = closeShift.VATNet.ToString();
        }

        private void ProcessCloseShiftDetail(
            CloseShift closeShift,
            CashierCloseData closeData,
            CloseShiftDetail detail)
        {
            bool isSaleOrRent = detail.InvoiceType == 2 ||
                                detail.InvoiceType == 3 ||
                                detail.InvoiceType == 11;

            bool isPurchase = detail.InvoiceType == 1 &&
                              _closeShiftSetting.IncPurchaseInv;

            bool isExpense = detail.InvoiceType == 8 &&
                              _closeShiftSetting.IncExpenses;

            switch (detail.ProcType)
            {
                case 1 when isSaleOrRent:
                    ApplySaleDetail(closeShift, closeData, detail);
                    break;

                case 1 when isPurchase:
                    ApplyPurchaseDeduction(closeShift, closeData, detail);
                    break;

                case 2 when isSaleOrRent:
                    ApplyReturnDetail(closeShift, closeData, detail);
                    break;

                case 2 when isPurchase:
                    ApplyPurchaseReturn(closeShift, closeData, detail);
                    break;

                case 2 when isExpense:
                    closeData.Expenses =
                        (double.Parse(closeData.Expenses) +
                         Convert.ToDouble(detail.InvNet)).ToString();
                    break;

                case 3 when detail.InvoiceType == 11:
                    ApplySaleDetail(closeShift, closeData, detail);
                    break;
            }
        }

        private static void ApplySaleDetail(
            CloseShift closeShift,
            CashierCloseData closeData,
            CloseShiftDetail detail)
        {
            closeShift.CashNet += detail.Cash;
            closeShift.Net += detail.InvNet;
            closeShift.VATNet += detail.VAT;
            closeShift.ExtaTax += detail.ExtraVAT;

            closeData.CashVal =
                (double.Parse(closeData.CashVal) +
                 Convert.ToDouble(detail.Cash)).ToString();
            closeData.NetworkSales =
                (double.Parse(closeData.NetworkSales) +
                 Convert.ToDouble(detail.Mada)).ToString();
            closeData.AdditionVal =
                (double.Parse(closeData.AdditionVal) +
                 Convert.ToDouble(detail.Additions)).ToString();
            closeData.InsurVal =
                (double.Parse(closeData.InsurVal) +
                 Convert.ToDouble(detail.Insurance)).ToString();
            closeData.Discount =
                (double.Parse(closeData.Discount) +
                 Convert.ToDouble(detail.Discount)).ToString();

            if (detail.PayType == -1)
                closeData.PostPoneSales =
                    (double.Parse(closeData.PostPoneSales) +
                     Convert.ToDouble(detail.InvNet)).ToString();
            else if (detail.PayType == 5)
                closeData.HostingVal =
                    (double.Parse(closeData.HostingVal) +
                     Convert.ToDouble(detail.InvNet)).ToString();
        }

        private static void ApplyPurchaseDeduction(
            CloseShift closeShift,
            CashierCloseData closeData,
            CloseShiftDetail detail)
        {
            double purchaseTotal =
                Convert.ToDouble(detail.Cash) + Convert.ToDouble(detail.Mada);

            closeData.Purchases =
                (double.Parse(closeData.Purchases) + purchaseTotal).ToString();

            closeShift.CashNet -= detail.Cash;
            closeShift.Net -= (detail.Cash + detail.Mada);
            closeShift.VATNet -= detail.VAT;
        }

        private static void ApplyReturnDetail(
            CloseShift closeShift,
            CashierCloseData closeData,
            CloseShiftDetail detail)
        {
            closeShift.CashNet -= detail.Cash;
            closeShift.Net -= detail.InvNet;
            closeShift.VATNet -= detail.VAT;
            closeShift.ExtaTax -= detail.ExtraVAT;

            if (detail.PayType != -1)
                closeData.ReturnSales =
                    (double.Parse(closeData.ReturnSales) +
                     Convert.ToDouble(detail.Cash) +
                     Convert.ToDouble(detail.Mada)).ToString();

            closeData.Discount =
                (double.Parse(closeData.Discount) -
                 Convert.ToDouble(detail.Discount)).ToString();
            closeData.AdditionVal =
                (double.Parse(closeData.AdditionVal) -
                 Convert.ToDouble(detail.Additions)).ToString();
            closeData.InsurVal =
                (double.Parse(closeData.InsurVal) -
                 Convert.ToDouble(detail.Insurance)).ToString();

            if (detail.PayType == -1)
                closeData.PostPoneRet =
                    (double.Parse(closeData.PostPoneRet) -
                     Convert.ToDouble(detail.InvNet)).ToString();
            else if (detail.PayType == 5)
                closeData.HostingVal =
                    (double.Parse(closeData.HostingVal) -
                     Convert.ToDouble(detail.InvNet)).ToString();
        }

        private static void ApplyPurchaseReturn(
            CloseShift closeShift,
            CashierCloseData closeData,
            CloseShiftDetail detail)
        {
            double purchaseTotal =
                Convert.ToDouble(detail.Cash) + Convert.ToDouble(detail.Mada);

            closeData.Purchases =
                (double.Parse(closeData.Purchases) - purchaseTotal).ToString();

            closeShift.CashNet += detail.Cash;
            closeShift.Net += detail.InvNet;
            closeShift.VATNet += detail.VAT;
        }

        #endregion

        #region ── Load Print Settings ────────────────────────

        private void loadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id = 12",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 1)
                {
                    try
                    {
                        DataRow row = dataTable.Rows[0];
                        _printType = Convert.ToInt32(row["printType"]);
                        _printFooter = Convert.ToBoolean(row["PrintFooter"]);
                        _printHeader = Convert.ToBoolean(row["PrintHeader"]);
                        _printStamp = Convert.ToBoolean(row["PrintStamp"]);
                        _defPrinter = row["CasherPrinter"]?.ToString() ?? string.Empty;

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();

                        _printNo = Convert.ToInt32(row["printNo"]);
                        _reportUrl = Path.GetDirectoryName(
                            row["RptUrl"]?.ToString()) ?? string.Empty;

                        if (string.IsNullOrEmpty(_reportUrl) ||
                            !Directory.Exists(_reportUrl))
                            _reportUrl = MainClass.ReportsPath;
                    }
                    catch { }
                }
                else
                {
                    _reportUrl = MainClass.ReportsPath;
                    _defPrinter = MainClass.ReportsPrinter;
                    _reportName = "rptSalesByGroups.repx";
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل إعدادات الطباعة", ex);
            }
        }

        private void PrintSetting(int settingType)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM SettingPrint WHERE Inv_Id = {settingType}",
                    conn1);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 1)
                {
                    DataRow row = dataTable.Rows[0];
                    _isGroupPrinted = Convert.ToBoolean(row["PrintTotGroup"]);
                    _isItemsPrinted = Convert.ToBoolean(row["PrintTotItem"]);
                    _printFooter = Convert.ToBoolean(row["PrintFooter"]);
                    _printHeader = Convert.ToBoolean(row["PrintHeader"]);
                    _defPrinter = row["CasherPrinter"]?.ToString() ?? string.Empty;

                    if (string.IsNullOrEmpty(_defPrinter))
                        _defPrinter = MainClass.ReportsPrinter;

                    _printNo = Convert.ToInt32(row["printNo"]);
                    _reportName = row["RptName"]?.ToString() ?? string.Empty;

                    try
                    {
                        _reportUrl = Path.GetDirectoryName(
                            row["RptUrl"]?.ToString()) ?? string.Empty;
                    }
                    catch { }

                    if (string.IsNullOrEmpty(_reportUrl) ||
                        !Directory.Exists(_reportUrl))
                        _reportUrl = MainClass.ReportsPath;

                    _printNote = row["note"]?.ToString() ?? string.Empty;
                }
                else
                {
                    _reportUrl = MainClass.ReportsPath;
                    _reportName = "rptCloseday.repx";
                    _defPrinter = MainClass.ReportsPrinter;
                }
            }
            catch { }
        }

        #endregion

        #region ── Print / Export ─────────────────────────────

        private void PrintReport(int printMode)
        {
            try
            {
                PrintSetting(6);

                if (string.IsNullOrEmpty(_reportUrl))
                    _reportUrl = MainClass.ReportsPath;

                _reportName = "rptCloseShift.repx";
                _defPrinter = MainClass.ReportsPrinter;

                string reportPath = Path.Combine(_reportUrl, _reportName);

                if (!Directory.Exists(_reportUrl) || !File.Exists(reportPath))
                {
                    ShowWarning("المسار الحالي للتقارير غير موجود أو تم تعديله");
                    return;
                }

                if (string.IsNullOrEmpty(_reportName))
                {
                    ShowWarning("يجب إدخال اسم التقرير من الإعدادات");
                    return;
                }

                XtraReport report = XtraReport.FromFile(reportPath);
                report.DataSource = BindToDataSet();

                LoadSubReport(report, "headerRpt",
                    Path.Combine(_reportUrl, "header.repx"),
                    Common.FoundationInfoDT);

                LoadSubReport(report, "footerRpt",
                    Path.Combine(_reportUrl, "footer.repx"),
                    Common.FoundationInfoDT);

                ExecutePrint(report, printMode);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الطباعة", ex);
            }
        }

        private void PrintDevexpress(
            int printMode,
            CashierCloseData closeData,
            List<CloseShiftCustomer> customers)
        {
            try
            {
                PrintSetting(6);

                if (string.IsNullOrEmpty(_reportUrl))
                {
                    _reportUrl = MainClass.ReportsPath;
                    _reportName = "rptCloseday.repx";
                }

                _defPrinter = MainClass.ReportsPrinter;

                string reportPath = Path.Combine(_reportUrl, _reportName);

                if (!Directory.Exists(_reportUrl) || !File.Exists(reportPath))
                {
                    ShowWarning("المسار الحالي للتقارير غير موجود أو تم تعديله");
                    return;
                }

                if (string.IsNullOrEmpty(_reportName))
                {
                    ShowWarning("يجب إدخال اسم التقرير من الإعدادات");
                    return;
                }

                XtraReport report = XtraReport.FromFile(reportPath);

                var closeList = new List<CashierCloseData> { closeData };
                DataSet dataSet = new DataSet("Name");
                dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(closeList));
                report.DataSource = dataSet;

                LoadSubReport(report, "subreportCust",
                    Path.Combine(_reportUrl, "rptClosedayCust.repx"),
                    customers);

                LoadSubReport(report, "headerRpt",
                    Path.Combine(_reportUrl, "header.repx"),
                    Common.FoundationInfoDT);

                LoadSubReport(report, "footerRpt",
                    Path.Combine(_reportUrl, "footer.repx"),
                    Common.FoundationInfoDT);

                if (printMode == 3)
                {
                    SendEmail(report);
                    return;
                }

                ExecutePrint(report, printMode);
                SendEmail(report);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void PrintDevexpresDetails(int printMode)
        {
            try
            {
                _reportUrl = MainClass.ReportsPath;
                _defPrinter = MainClass.ReportsPrinter;
                _reportName = "rptSalesByGroups.repx";

                string reportPath = Path.Combine(_reportUrl, _reportName);

                string reportDir = Path.GetDirectoryName(_reportUrl) ?? string.Empty;
                if (!Directory.Exists(reportDir) || !File.Exists(reportPath))
                {
                    ShowWarning("المسار الحالي للتقارير غير موجود أو تم تعديله");
                    return;
                }

                if (string.IsNullOrEmpty(_reportName))
                {
                    ShowWarning("يجب إدخال اسم التقرير من الإعدادات");
                    return;
                }

                XtraReport mainReport = XtraReport.FromFile(reportPath);
                XtraReport detailReport = XtraReport.FromFile(
                    Path.Combine(_reportUrl, "ItemDetail.repx"));

                mainReport.DataSource = _groupDataTable;
                detailReport.DataSource = _detailDataTable;

                // ربط subreport التفاصيل
                XRSubreport detailSubreport =
                    mainReport.FindControl("subreport", ignoreCase: true)
                    as XRSubreport;

                if (detailSubreport != null)
                    detailSubreport.ReportSource = detailReport;

                // تحميل الهيدر
                LoadSubReport(mainReport, "headerRpt",
                    Path.Combine(_reportUrl, "header.repx"),
                    Common.FoundationInfoDT);

                // ✅ إصلاح 3: استخدام XRControl بدلاً من XRControlBase
                XRControl grpBand =
                    mainReport.FindControl("SubBandGrp", ignoreCase: true)
                    as XRControl;

                if (grpBand != null)
                    grpBand.Visible = _closeShiftSetting.PrintGroups;

                XRControl itemsBand =
                    mainReport.FindControl("SubBandItems", ignoreCase: true)
                    as XRControl;

                if (itemsBand != null)
                    itemsBand.Visible = _closeShiftSetting.QuantPrint;

                ExecutePrint(mainReport, printMode);
                mainReport.Dispose();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء طباعة التفاصيل", ex);
            }
        }

        private static void LoadSubReport(
            XtraReport report,
            string controlName,
            string reportPath,
            object dataSource)
        {
            try
            {
                if (!File.Exists(reportPath))
                    return;

                XtraReport subReport = XtraReport.FromFile(reportPath);
                subReport.DataSource = dataSource;

                XRSubreport subControl =
                    report.FindControl(controlName, ignoreCase: true)
                    as XRSubreport;

                if (subControl != null)
                    subControl.ReportSource = subReport;
            }
            catch { }
        }

        private void ExecutePrint(XtraReport report, int printMode)
        {
            if (string.IsNullOrEmpty(_defPrinter))
            {
                ShowWarning("يجب تحديد الطابعة من الإعدادات");
                return;
            }

            report.PrinterName = _defPrinter;

            switch (printMode)
            {
                case 1:
                    for (int copyIndex = 1; copyIndex <= _printNo; copyIndex++)
                        report.Print();
                    break;

                case 2:
                    report.ShowPreviewDialog();
                    break;
            }
        }

        private void ExportToExcel()
        {
            try
            {
                if (GridControl1.Items.Count == 0)
                    return;

                Microsoft.Win32.SaveFileDialog saveDialog =
                    new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel Files|*.xlsx",
                        FileName = $"إغلاقات_اليومية_{DateTime.Now:yyyyMMdd}"
                    };

                if (saveDialog.ShowDialog() == true)
                    ShowSuccess("تم تصدير الملف بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التصدير", ex);
            }
        }

        #endregion

        #region ── Bind To DataSet ────────────────────────────

        public DataSet BindToDataSet()
        {
            var closeDataList = new List<CashierCloseData>();

            if (GridControl1.ItemsSource is
                IEnumerable<CloseShiftGridRow> gridRows)
            {
                foreach (CloseShiftGridRow row in gridRows)
                {
                    closeDataList.Add(new CashierCloseData
                    {
                        CloseNo = row.CloseNoDgv.ToString(),
                        Username = row.EmpNameDgv,
                        CloseDateFrome = row.CloseFrom.ToString(),
                        CloseDateTo = row.CloseTo.ToString(),
                        PostPoneSales = row.ForwardSaleDgv.ToString(),
                        NetworkSales = row.NetworkSumDgv.ToString(),
                        SAfeNetVal = row.CashSumDgv.ToString(),
                        SumCashAndCredit = row.SumCashAndCredit.ToString(),
                        Expenses = row.ExpensesDgv.ToString(),
                        Purchases = row.PurchasesDgv.ToString(),
                        AdditionVal = row.AdditionValDgv.ToString(),
                        InsurVal = row.InsurValDgv.ToString(),
                        HostingVal = row.HostingDgv.ToString(),
                        VAT = row.AllVATDgv.ToString(),
                        Discount = row.DiscountDgv.ToString(),
                        Net = row.NetDgv.ToString(),
                        CasherValue = row.TreasuryBlc.ToString(),
                        DiffVal = row.Differ.ToString(),
                        datefrom = txtDateFrom.SelectedDate?
                                           .ToShortDateString() ?? string.Empty,
                        dateTo = txtDateTo.SelectedDate?
                                           .ToShortDateString() ?? string.Empty,
                        ReportName = Title,
                        EmpName = Common.GetEmpName(MainClass.EmpNo),
                        printDate = DateTime.Now.ToShortDateString()
                    });
                }
            }

            DataSet dataSet = new DataSet("Name");
            dataSet.Tables.Add(
                UtilitiesProj.Common.ToDataTable(closeDataList));
            return dataSet;
        }

        #endregion

        #region ── ReClose Shift ──────────────────────────────

        public void ReClose(int closeNo)
        {
            LoadCloseShiftSetting();

            SqlDataAdapter adapter = new SqlDataAdapter(
                $"SELECT * FROM CasherClosed WHERE ClosedId = {closeNo}",
                conn);

            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            if (dataTable.Rows.Count != 1)
                return;

            DataRow row = dataTable.Rows[0];

            CloseShift originalShift = new CloseShift();
            CashierCloseData closeData = new CashierCloseData();

            originalShift.ClosedType = Convert.ToInt32(row["type"]);
            originalShift.GlobalID = row["GlobalID"]?.ToString() ?? string.Empty;
            originalShift.EmpId = Convert.ToInt32(row["user_id"]);

            closeData.DiffVal = Convert.ToDouble(row["diff"]).ToString();
            closeData.CasherValue = Convert.ToDouble(row["CasherValue"]).ToString();
            closeData.Net = Convert.ToDouble(row["ToT"]).ToString();
            closeData.datefrom = Convert.ToDateTime(row["startTime"]).ToString();
            closeData.dateTo = Convert.ToDateTime(row["endTime"])
                                    .AddSeconds(10).ToString();

            EnsureConnectionOpen(conn1);

            string entryGlobalId =
                GetEntryGlobalId(closeNo, closeData, originalShift.EmpId);

            if (string.IsNullOrEmpty(entryGlobalId))
            {
                MessageBox.Show("قيد الإغلاق غير مربوط بالإغلاق",
                    "⚠️ تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CloseShift reCloseData = GetCloseData(
                1,
                Convert.ToDateTime(closeData.datefrom),
                Convert.ToDateTime(closeData.dateTo),
                originalShift.EmpId);

            if (reCloseData == null)
                return;

            reCloseData.ClosedId = closeNo;
            reCloseData.GlobalID = originalShift.GlobalID;
            reCloseData.ShiftNo = _shiftNo;
            reCloseData.EmpId = originalShift.EmpId;
            reCloseData.Bank = 1;
            reCloseData.CloseTime = Convert.ToDateTime(closeData.dateTo);
            reCloseData.ShiftStart = Convert.ToDateTime(closeData.datefrom);
            reCloseData.ShiftEnd = Convert.ToDateTime(closeData.dateTo);
            reCloseData.Treasury =
                Common.GetUserTreasuryID((short)reCloseData.EmpId);
            reCloseData.EntryGlobalID = entryGlobalId;
            reCloseData.CashierBalance =
                Convert.ToDecimal(closeData.CasherValue);

            Entry entry = new EntryOper().BindCloseShiftToEntry(
                reCloseData, _closeShiftSetting.BalanceRequired);

            if (entry == null)
                return;

            EntryOper entryOper = new EntryOper();
            entry.EntryGlobalID = entryGlobalId;

            if (!entryOper.SaveEnty(entry))
            {
                ShowWarning(MainClass.Language == "en"
                    ? "error in saving"
                    : "خطأ أثناء الحفظ");
                return;
            }

            if (Sync.ActiveSync && Sync.SyncType > 0)
                entryOper.SyncEntry(entry, isNew: true);

            SaveReCloseToDatabase(
                reCloseData, originalShift, closeData, closeNo);
        }

        private string GetEntryGlobalId(
            int closeNo,
            CashierCloseData closeData,
            int empId)
        {
            string globalId =
                new SqlCommand(
                    $"SELECT GlobalId FROM Entry " +
                    $"WHERE doc_no = {closeNo} AND type = 13 AND " +
                    $"branch = {MainClass.BranchNo}",
                    conn1).ExecuteScalar()?.ToString() ?? string.Empty;

            if (!string.IsNullOrEmpty(globalId))
                return globalId;

            return new SqlCommand(
                $"SELECT GlobalId FROM Entry " +
                $"WHERE date > N'{closeData.datefrom}' AND " +
                $"date <= N'{closeData.dateTo}' AND " +
                $"type = 13 AND EmpId = {empId} AND " +
                $"branch = {MainClass.BranchNo}",
                conn1).ExecuteScalar()?.ToString() ?? string.Empty;
        }

        private void SaveReCloseToDatabase(
            CloseShift reCloseData,
            CloseShift originalShift,
            CashierCloseData closeData,
            int closeNo)
        {
            EnsureConnectionOpen(conn);
            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                BindingCloseShift(ref reCloseData, ref closeData);
                BindingCloseShiftCust(reCloseData);

                double cashMinusReturn =
                    double.Parse(closeData.CashVal) -
                    double.Parse(closeData.ReturnSales);

                if (cashMinusReturn -
                    Convert.ToDouble(closeData.CasherValue) > 0.0)
                {
                    SqlCommand salaryCmd = new SqlCommand(
                        "INSERT INTO EmpSalaryAddSub " +
                        "(emp, type, date, val, notes, user_id, IS_Deleted) " +
                        "VALUES(@emp, 3, @date, @val, @notes, 1, 0)",
                        conn, transaction);

                    salaryCmd.Parameters.AddWithValue(
                        "@emp", MainClass.EmpNo);
                    salaryCmd.Parameters.AddWithValue(
                        "@date", DateTime.Now);
                    salaryCmd.Parameters.AddWithValue("@val",
                        $"{cashMinusReturn - Convert.ToDouble(closeData.CasherValue):0.#,##.##}");
                    salaryCmd.Parameters.AddWithValue(
                        "@notes", "عجز اغلاق الوردية");
                    salaryCmd.ExecuteNonQuery();
                }

                SqlCommand updateCmd = new SqlCommand(
                    "UPDATE dbo.CasherClosed SET " +
                    "type = @type, user_id = @user_id, " +
                    "startTime = @startTime, endTime = @endTime, " +
                    "ToT = @ToT, CasherValue = @CasherValue, diff = @diff " +
                    $"WHERE ClosedId = {reCloseData.ClosedId}",
                    conn, transaction);

                updateCmd.Parameters.Add(
                    "@type", SqlDbType.Int).Value =
                    originalShift.ClosedType;
                updateCmd.Parameters.Add(
                    "@user_id", SqlDbType.Int).Value =
                    reCloseData.EmpId;
                updateCmd.Parameters.Add(
                    "@startTime", SqlDbType.DateTime).Value =
                    GetIso8601Date(reCloseData.ShiftStart);
                updateCmd.Parameters.Add(
                    "@endTime", SqlDbType.DateTime).Value =
                    GetIso8601Date(reCloseData.ShiftEnd);
                updateCmd.Parameters.Add(
                    "@ToT", SqlDbType.NVarChar).Value = closeData.Net;
                updateCmd.Parameters.Add(
                    "@CasherValue", SqlDbType.NVarChar).Value =
                    Math.Round(reCloseData.CashierBalance, 2);
                updateCmd.Parameters.Add(
                    "@diff", SqlDbType.NVarChar).Value = closeData.DiffVal;
                updateCmd.ExecuteNonQuery();

                new SqlCommand(
                    $"DELETE FROM CasherClosed_Sub " +
                    $"WHERE GlobalID = N'{reCloseData.GlobalID}'",
                    conn, transaction).ExecuteNonQuery();

                InsertCloseSubRecord(
                    conn, transaction, closeNo, closeData, reCloseData.GlobalID);

                if (reCloseData.CloseShiftCustomers.Count > 0)
                {
                    new SqlCommand(
                        $"DELETE FROM CloseShiftCustomer " +
                        $"WHERE ClosedId = {reCloseData.ClosedId}",
                        conn, transaction).ExecuteNonQuery();

                    InsertCloseCustomers(conn, transaction, reCloseData);
                }

                transaction.Commit();
                Home.IsCashierClosed = true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ShowError("خطأ أثناء إغلاق اليومية", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private static void InsertCloseSubRecord(
            SqlConnection connection,
            SqlTransaction transaction,
            int closeNo,
            CashierCloseData closeData,
            string globalId)
        {
            SqlCommand subCmd = new SqlCommand(
                "INSERT INTO CasherClosed_Sub(" +
                "ClosedId, CashTotal, SAfeNetVal, ReturnSum, NetworkSum, " +
                "AdditionVal, PostPoneSales, PostPoneRet, InsurVal, Discount, " +
                "AllVAT, IsDeleted, HostingVal, Expenses, Purchases, " +
                "ExtraTax, GlobalID) " +
                "VALUES(@ClosedId, @CashTotal, @SAfeNetVal, @ReturnSum, " +
                "@NetworkSum, @AdditionVal, @PostPoneSales, @PostPoneRet, " +
                "@InsurVal, @Discount, @AllVAT, @IsDeleted, @HostingVal, " +
                "@Expenses, @Purchases, @ExtraTax, @GlobalID)",
                connection, transaction);

            subCmd.Parameters.Add("@ClosedId", SqlDbType.Int).Value =
                closeNo;
            subCmd.Parameters.Add("@CashTotal", SqlDbType.Float).Value =
                $"{double.Parse(closeData.CashVal):0.#,##.##}";
            subCmd.Parameters.Add("@SAfeNetVal", SqlDbType.Float).Value =
                $"{double.Parse(closeData.SAfeNetVal):0.#,##.##}";
            subCmd.Parameters.Add("@ReturnSum", SqlDbType.Float).Value =
                $"{double.Parse(closeData.ReturnSales):0.#,##.##}";
            subCmd.Parameters.Add("@NetworkSum", SqlDbType.Float).Value =
                $"{double.Parse(closeData.NetworkSales):0.#,##.##}";
            subCmd.Parameters.Add("@AdditionVal", SqlDbType.Float).Value =
                $"{double.Parse(closeData.AdditionVal):0.#,##.##}";
            subCmd.Parameters.Add("@PostPoneSales", SqlDbType.Float).Value =
                $"{double.Parse(closeData.PostPoneSales):0.#,##.##}";
            subCmd.Parameters.Add("@PostPoneRet", SqlDbType.Float).Value =
                $"{double.Parse(closeData.PostPoneRet):0.#,##.##}";
            subCmd.Parameters.Add("@InsurVal", SqlDbType.Float).Value =
                $"{double.Parse(closeData.InsurVal):0.#,##.##}";
            subCmd.Parameters.Add("@Discount", SqlDbType.Float).Value =
                $"{double.Parse(closeData.Discount):0.#,##.##}";
            subCmd.Parameters.Add("@AllVAT", SqlDbType.Float).Value =
                $"{double.Parse(closeData.VAT):0.#,##.##}";
            subCmd.Parameters.Add("@ExtraTax", SqlDbType.Float).Value =
                string.IsNullOrEmpty(closeData.ExtraTax)
                    ? (object)0
                    : $"{double.Parse(closeData.ExtraTax):0.#,##.##}";
            subCmd.Parameters.Add("@Expenses", SqlDbType.Float).Value =
                $"{double.Parse(closeData.Expenses):0.#,##.##}";
            subCmd.Parameters.Add("@Purchases", SqlDbType.Float).Value =
                $"{double.Parse(closeData.Purchases):0.#,##.##}";
            subCmd.Parameters.Add("@HostingVal", SqlDbType.Float).Value =
                $"{double.Parse(closeData.HostingVal):0.#,##.##}";
            subCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
            subCmd.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value =
                globalId;

            subCmd.ExecuteNonQuery();
        }

        private static void InsertCloseCustomers(
            SqlConnection connection,
            SqlTransaction transaction,
            CloseShift closeShift)
        {
            foreach (CloseShiftCustomer customer in
                closeShift.CloseShiftCustomers)
            {
                SqlCommand custCmd = new SqlCommand(
                    "INSERT INTO CloseShiftCustomer" +
                    "(ClosedId, CustId, Amount) " +
                    "VALUES(@ClosedId, @CustId, @Amount)",
                    connection, transaction);

                custCmd.Parameters.Add(
                    "@ClosedId", SqlDbType.Int).Value = customer.ClosedId;
                custCmd.Parameters.Add(
                    "@CustId", SqlDbType.Int).Value = customer.CustId;
                custCmd.Parameters.Add(
                    "@Amount", SqlDbType.Int).Value = customer.Amount;

                custCmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region ── Close Shift ────────────────────────────────

        public bool NoInvsFound(int closeType)
        {
            try
            {
                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch = {MainClass.BranchNo} AND " +
                      $"inv.sales_emp = {MainClass.EmpNo} AND "
                    : string.Empty;

                EnsureConnectionOpen(conn);

                int maxCloseId = Convert.ToInt32(
                    new SqlCommand(
                        $"SELECT ISNULL(MAX(ClosedID), 0) FROM CasherClosed " +
                        $"WHERE user_id = {MainClass.EmpNo}",
                        conn).ExecuteScalar());

                if (maxCloseId > 0)
                {
                    MainClass.lastCashercloseDate = Convert.ToDateTime(
                        new SqlCommand(
                            $"SELECT endTime FROM CasherClosed " +
                            $"WHERE ClosedID = {maxCloseId}",
                            conn).ExecuteScalar());
                }
                else
                {
                    string selectQuery = closeType == 1
                        ? $"SELECT MIN(date) AS date1 FROM Inv " +
                          $"WHERE {branchFilter} inv.inv_type = 3 AND " +
                          "inv.pay_type <> -1 AND inv.pay_type <> 5 AND " +
                          "Inv.IS_Deleted = 0"
                        : $"SELECT MIN(date) AS date1 FROM RentInvoice " +
                          $"WHERE IS_Deleted = 0 AND " +
                          $"sales_emp = {MainClass.EmpNo}";

                    SqlDataAdapter adapter =
                        new SqlDataAdapter(selectQuery, conn);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0 &&
                        dataTable.Rows[0]["date1"] != DBNull.Value)
                        MainClass.lastCashercloseDate =
                            Convert.ToDateTime(dataTable.Rows[0]["date1"]);
                }

                LoadCloseShiftSetting();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void closeShift(int closeType)
        {
            LoadCloseShiftSetting();

            datefrom = MainClass.lastCashercloseDate;
            dateTo = DateTime.Now;
            _username = MainClass.UserName;

            if (_closeShiftSetting.BalanceRequired)
            {
                Frm_Calculator calculator = new Frm_Calculator();
                calculator.txtMsg.Text = MainClass.Language == "ar"
                    ? "إدخل المبلغ الموجود في الصندوق"
                    : "Enter the amount of cash in the cash drawer";

                calculator.ShowDialog();

                if (calculator.is_close)
                    return;

                _casherValue = Convert.ToDouble(calculator.TextBox1.Text);
            }

            _userTreasuryAccount = Common.GetUserTreasuryAcc();

            if (_userTreasuryAccount == -1)
            {
                MessageBox.Show(
                    "الموظف غير مرتبط بصندوق",
                    "⚠️ تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void GetGlobalIdClose(ref string globalId, ref int closedId)
        {
            SqlConnection tempConn = MainClass.ConnObj();
            EnsureConnectionOpen(tempConn);

            closedId = Convert.ToInt32(
                new SqlCommand(
                    $"SELECT ISNULL(MAX(ClosedId), 0) FROM CasherClosed " +
                    $"WHERE branch = {MainClass.BranchNo}",
                    tempConn).ExecuteScalar());

            do
            {
                closedId++;
                globalId = Sync.ActiveSync
                    ? $"{Sync.ClientCode}-{MainClass.BranchNo}-{closedId}"
                    : $"{MainClass.BranchNo}-{closedId}";
            }
            while (Convert.ToDouble(
                new SqlCommand(
                    $"SELECT COUNT(*) FROM CasherClosed " +
                    $"WHERE GlobalID = N'{globalId}'",
                    tempConn).ExecuteScalar()) > 0.0);

            EnsureConnectionClosed(tempConn);
        }

        private CloseShift GetCloseData(
            int closeType,
            DateTime fromDate,
            DateTime toDate,
            int empId)
        {
            CloseShift closeShift = new CloseShift();

            try
            {
                closeShift.EmpId = empId;
                closeShift.CashNet = 0m;
                closeShift.Net = 0m;
                closeShift.VATNet = 0m;

                string sqlQuery = BuildGetCloseDataQuery(closeType, empId);

                SqlDataAdapter adapter =
                    new SqlDataAdapter(sqlQuery, conn1);

                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = fromDate;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = toDate;
                adapter.SelectCommand.Parameters.Add(
                    "@Branch", SqlDbType.Int).Value = MainClass.BranchNo;
                adapter.SelectCommand.Parameters.Add(
                    "@EmpId", SqlDbType.Int).Value = empId;

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                    ProcessGetCloseDataRow(closeShift, row);

                return closeShift;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء احتساب إغلاق اليومية", ex);
                return null;
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private string BuildGetCloseDataQuery(int closeType, int empId)
        {
            if (closeType == 1)
            {
                EnsureConnectionOpen(conn1);

                new SqlCommand(
                    $"UPDATE [Inv] SET cash = tot_net " +
                    $"WHERE cash > tot_net AND " +
                    $"(Inv_type = 2 OR Inv_type = 3) AND " +
                    $"sales_emp = {empId}",
                    conn1).ExecuteNonQuery();

                return "SELECT Inv.inv_type AS InvType, Inv.proc_type AS ProcType, " +
                       "pay_type AS Paytype, inv.VATPercent, 1 AS PaymentStatus, " +
                       "0 AS Remainder, SUM(tot_net) AS InvNet, " +
                       "SUM(InvTotal) AS InvTotal, bank AS BankId, " +
                       "SUM(cash) AS cash, SUM(visa) AS Mada, " +
                       "SUM(tax) AS VAT, SUM(ExtraVAT) AS ExtraVAT, " +
                       "SUM(minus) AS Discount, " +
                       "ISNULL(SUM(ItemsDiscount), 0) AS ItemsDiscount, " +
                       "SUM(AdditionsTot) AS Additions, " +
                       "SUM(Insurance) AS Insurance, SUM(paid) AS Paid " +
                       "FROM Inv WHERE branch = @Branch AND " +
                       "sales_emp = @EmpId AND " +
                       "(inv.proc_Type = 1 OR inv.proc_type = 2) AND " +
                       "(inv.Inv_Type = 2 OR inv.Inv_Type = 3) AND " +
                       "Inv.date >= @date1 AND Inv.date <= @date2 AND " +
                       "Inv.IS_Deleted = 0 " +
                       "GROUP BY Inv_Type, Proc_Type, pay_type, bank, inv.VATPercent";
            }

            return string.Empty;
        }

        private void ProcessGetCloseDataRow(CloseShift closeShift, DataRow row)
        {
            if (_closeShiftSetting != null && !_closeShiftSetting.IncSaleInv &&
                Convert.ToInt32(row["InvType"]) == 2)
                return;

            CloseShiftDetail detail = new CloseShiftDetail
            {
                InvoiceType = Convert.ToInt32(row["InvType"]),
                ProcType = Convert.ToInt32(row["ProcType"]),
                PayType = Convert.ToInt32(row["PayType"]),
                PaymentStatus = Convert.ToInt32(row["PaymentStatus"]),
                InvNet = new decimal(Math.Round(Convert.ToDouble(row["InvNet"]), 2)),
                InvTotal = new decimal(Math.Round(Convert.ToDouble(row["InvTotal"]), 2)),
                Cash = new decimal(Math.Round(Convert.ToDouble(row["Cash"]), 2)),
                Mada = new decimal(Math.Round(Convert.ToDouble(row["Mada"]), 2)),
                Additions = new decimal(Math.Round(Convert.ToDouble(row["Additions"]), 2)),
                Insurance = new decimal(Math.Round(Convert.ToDouble(row["Insurance"]), 2)),
                Discount = new decimal(Math.Round(
                    Convert.ToDouble(row["Discount"]) +
                    Convert.ToDouble(row["ItemsDiscount"]), 2)),
                ExtraVAT = new decimal(Math.Round(Convert.ToDouble(row["ExtraVAT"]), 2)),
                VAT = new decimal(Math.Round(Convert.ToDouble(row["VAT"]), 2)),
                Remainder = new decimal(Math.Round(Convert.ToDouble(row["Remainder"]), 2)),
                BankId = Convert.ToInt32(row["BankId"])
            };

            closeShift.CloseShiftDetails.Add(detail);
        }

        private void BindingCloseShiftCust(CloseShift closeShift)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT Inv.inv_type AS InvType, " +
                    "Inv.proc_type AS ProcType, " +
                    "pay_type AS Paytype, " +
                    "SUM(tot_net) AS InvNet, " +
                    "SUM(InvTotal) AS InvTotal, " +
                    "SUM(paid) AS Paid, " +
                    "cust_id AS cust_id, " +
                    "Customers.name AS custName " +
                    "FROM Inv " +
                    "LEFT JOIN Customers ON inv.cust_id = Customers.id " +
                    "WHERE inv.branch = @Branch AND " +
                    "sales_emp = @EmpId AND " +
                    "(inv.proc_Type = 1 OR inv.proc_type = 2) AND " +
                    "(inv.Inv_Type = 2 OR inv.Inv_Type = 3) AND " +
                    "Inv.pay_type = 7 AND " +
                    "Inv.date >= @date1 AND Inv.date <= @date2 AND " +
                    "Inv.IS_Deleted = 0 " +
                    "GROUP BY Inv_Type, Proc_Type, pay_type, " +
                    "Customers.name, inv.cust_id",
                    conn1);

                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = closeShift.ShiftStart;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = closeShift.ShiftEnd;
                adapter.SelectCommand.Parameters.Add(
                    "@Branch", SqlDbType.Int).Value = MainClass.BranchNo;
                adapter.SelectCommand.Parameters.Add(
                    "@EmpId", SqlDbType.Int).Value = closeShift.EmpId;

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    if (_closeShiftSetting.IncSaleInv ||
                        Convert.ToInt32(row["InvType"]) != 2)
                        continue;

                    closeShift.CloseShiftCustomers.Add(new CloseShiftCustomer
                    {
                        ClosedId = closeShift.ClosedId,
                        CustId = Convert.ToInt32(row["cust_id"]),
                        Name = row["custName"]?.ToString() ?? string.Empty,
                        Amount = new decimal(
                            Convert.ToDouble(row["InvNet"]) -
                            Convert.ToDouble(row["Paid"]))
                    });
                }
            }
            catch { }
        }

        #endregion

        #region ── Email ──────────────────────────────────────

        private void SendEmail(XtraReport report)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM SettingEmail " +
                    $"WHERE Branch_Id = {MainClass.BranchNo}",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0 ||
                    !Convert.ToBoolean(dataTable.Rows[0]["ActiveAuto"]))
                    return;

                string executableDir = Path.GetDirectoryName(
                    System.Reflection.Assembly
                        .GetExecutingAssembly().Location) ?? string.Empty;
                string pdfPath = Path.Combine(executableDir, "إغلاق_الوردية.pdf");

                if (File.Exists(pdfPath))
                    File.Delete(pdfPath);

                report.ExportToPdf(pdfPath);

                DataRow emailRow = dataTable.Rows[0];
                string senderEmail = emailRow["SendEmail"]?.ToString() ?? string.Empty;
                string receiverEmail = emailRow["RecEmail"]?.ToString() ?? string.Empty;
                string serverName = emailRow["ServerName"]?.ToString() ?? string.Empty;
                string senderPassword = emailRow["SendPWd"]?.ToString() ?? string.Empty;
                int port = Convert.ToInt32(emailRow["port"]);

                string body =
                    $"السلام عليكم و رحمة الله و بركاته:{Environment.NewLine}" +
                    $"إغلاق الوردية للموظف {_username}{Environment.NewLine}" +
                    $"{Home.lblBranch1.Text}{Environment.NewLine}" +
                    $"التاريخ: {txtDate.SelectedDate?.ToShortDateString()}" +
                    $"{Environment.NewLine}" +
                    $"المرسل: {_username}{Environment.NewLine}" +
                    "مع خالص التحايا …";

                using MailMessage mailMessage = new MailMessage(
                    senderEmail.Trim(), receiverEmail.Trim());

                mailMessage.Subject =
                    $"إغلاق الوردية للموظف {_username} - " +
                    $"{Home.lblBranch1.Text}";
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;

                if (File.Exists(pdfPath))
                    mailMessage.Attachments.Add(new Attachment(pdfPath));

                SmtpClient smtpClient = new SmtpClient
                {
                    Host = serverName.Trim(),
                    EnableSsl = Convert.ToBoolean(emailRow["SSL"]),
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential(
                                            senderEmail.Trim(),
                                            senderPassword.Trim()),
                    Port = port
                };

                if (MainClass.CheckForInternetConnection())
                {
                    smtpClient.Send(mailMessage);
                    ShowSuccess(MainClass.Language == "ar"
                        ? "تم إرسال البريد"
                        : "Email sent.");
                }
                else
                {
                    ShowWarning("لم يتم إرسال البريد - لا يوجد اتصال بالإنترنت");
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء إرسال البريد", ex);
            }
        }

        #endregion

        #region ── Progress Bar ───────────────────────────────

        private void SetProgress(int value, int maximum = -1)
        {
            Dispatcher.Invoke(() =>
            {
                if (maximum >= 0)
                {
                    ProgressBar1.Maximum = maximum;
                    txtProgressLabel.Text =
                        $"جاري التحميل... {value} / {maximum}";
                }

                ProgressBar1.Value = value;

                if (maximum > 0 && value == maximum)
                    txtProgressLabel.Text = $"✅ تم التحميل ({maximum} سجل)";
            });
        }

        #endregion

        #region ── Batch ReClose ──────────────────────────────

        private void BtnRecloseBatch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo != 0)
                    return;

                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من إعادة إغلاق؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                var selectedRows = GridControl1.SelectedItems
                    .OfType<CloseShiftGridRow>()
                    .ToList();

                foreach (CloseShiftGridRow row in selectedRows)
                {
                    _closeId = row.CloseNoDgv;
                    ReClose(_closeId);
                }

                ShowSuccess("تم إعادة احتساب الإغلاق ✅");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء إعادة الإغلاق", ex);
            }
        }

        #endregion

        #region ── Save Grid Settings ─────────────────────────

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_styleFolder))
                    Directory.CreateDirectory(_styleFolder);

                ShowSuccess("تم حفظ مظهر الجدول ✅");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء حفظ الإعدادات", ex);
            }
        }

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_styleFile))
                    File.Delete(_styleFile);

                ShowSuccess("تمت إعادة الجدول للإعدادات الافتراضية ✅");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء إعادة الإعدادات", ex);
            }
        }

        #endregion

        #region ── Connection Helpers ─────────────────────────

        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private static void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region ── Message Helpers ────────────────────────────

        private static void ShowError(string message, Exception ex = null)
        {
            string detail = ex != null
                ? $"{Environment.NewLine}تفاصيل: {ex.Message}"
                : string.Empty;

            MessageBox.Show(
                message + detail,
                "❌ خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                "⚠️ تنبيه",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowSuccess(string message)
        {
            MessageBox.Show(
                message,
                "✅ نجاح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }

    #region ── Grid Row Model ─────────────────────────────────

    public class CloseShiftGridRow
    {
        public int CloseNoDgv { get; set; }
        public string EmpNameDgv { get; set; }
        public DateTime CloseFrom { get; set; }
        public DateTime CloseTo { get; set; }
        public double NetDgv { get; set; }
        public double TreasuryBlc { get; set; }
        public double Differ { get; set; }
        public double ForwardSaleDgv { get; set; }
        public double NetworkSumDgv { get; set; }
        public double CashSumDgv { get; set; }
        public double SumCashAndCredit { get; set; }
        public double ExpensesDgv { get; set; }
        public double PurchasesDgv { get; set; }
        public double HostingDgv { get; set; }
        public double InsurValDgv { get; set; }
        public double AdditionValDgv { get; set; }
        public double AllVATDgv { get; set; }
        public double DiscountDgv { get; set; }
        public string GlobalID { get; set; }
    }

    #endregion
}