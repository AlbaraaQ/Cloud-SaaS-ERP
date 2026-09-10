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
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class IncomeStatementForm : ThemedWindow
    {
        #region Fields

        private readonly string connStr;

        private decimal revTotal;
        private decimal cogsTotal;
        private decimal grossProfit;
        private decimal expTotal;
        private decimal netIncome;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private SqlConnection conn;

        public int _Type;
        public int _FormType;

        private ObservableCollection<IncomeStatementRow> _incomeRows;

        #endregion

        #region Constructor

        public IncomeStatementForm()
        {
            InitializeComponent();

            connStr = MainClass.connstr;
            conn = MainClass.ConnObj();

            revTotal = 0m;
            cogsTotal = 0m;
            grossProfit = 0m;
            expTotal = 0m;
            netIncome = 0m;

            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = string.Empty;
            RptUrl = string.Empty;

            _Type = 1;
            _FormType = 1;

            _incomeRows = new ObservableCollection<IncomeStatementRow>();
            dgvIncomeData.ItemsSource = _incomeRows;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDates();
            LoadBranchList();
            LoadPrintSettings();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Initialize

        private void InitializeDates()
        {
            txtFromDate.DateTime = new DateTime(DateTime.Now.Year, 1, 1);
            txtToDate.DateTime = DateTime.Now;
            txtStartTime.Text = "00:00";
            txtEndTime.Text = "23:59";
        }

        private void LoadBranchList()
        {
            try
            {
                using (SqlConnection sqlConn = new SqlConnection(connStr))
                {
                    sqlConn.Open();
                    using (SqlDataAdapter da = new SqlDataAdapter(
                        "SELECT id, name FROM Branches WHERE IS_Deleted=0 ORDER BY name", sqlConn))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        cmbBranches.ItemsSource = dt.DefaultView;
                        cmbBranches.DisplayMemberPath = "name";
                        cmbBranches.SelectedValuePath = "id";

                        if (dt.Rows.Count > 0)
                            cmbBranches.SelectedIndex = 0;
                    }
                }
            }
            catch
            {
                // تجاهل الخطأ إذا لم يكن هناك اتصال
            }
        }

        #endregion

        #region Sidebar Controls Events

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isEnabled = ckTotalPeriod.IsChecked != true;
            txtFromDate.IsEnabled = isEnabled;
            txtStartTime.IsEnabled = isEnabled;
            txtToDate.IsEnabled = isEnabled;
            txtEndTime.IsEnabled = isEnabled;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void TimeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }

        private void TimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!TimeSpan.TryParse(tb.Text, out _))
                    tb.Text = tb.Name == "txtStartTime" ? "00:00" : "23:59";
            }
        }

        #endregion

        #region Show Data

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _incomeRows.Clear();
                ClearFinancials();
                UpdateSummaryLabels();

                DateTime fromDate = txtFromDate.DateTime.Date;
                DateTime toDate = txtToDate.DateTime.Date;

                TimeSpan startTime = TimeSpan.TryParse(txtStartTime.Text, out TimeSpan ts1) ? ts1 : TimeSpan.Zero;
                TimeSpan endTime = TimeSpan.TryParse(txtEndTime.Text, out TimeSpan ts2) ? ts2 : new TimeSpan(23, 59, 59);

                DateTime fromDt = fromDate.Add(startTime);
                DateTime toDt = toDate.Add(endTime);

                int branchId = -1;
                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedIndex != -1)
                {
                    if (cmbBranches.SelectedValue != null && int.TryParse(cmbBranches.SelectedValue.ToString(), out int bid))
                        branchId = bid;
                }

                ProgressBar1.Value = 20;

                DataTable dt = FetchIncomeData(fromDt, toDt, branchId);

                ProgressBar1.Value = 70;

                if (dt == null || dt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات في الفترة المحددة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ProgressBar1.Value = 0;
                    return;
                }

                foreach (DataRow row in dt.Rows)
                {
                    string accountCode = row["AccountCode"].ToString();
                    string category = row["Category"].ToString();
                    decimal balance = row["balance"] != DBNull.Value ? Convert.ToDecimal(row["balance"]) : 0m;

                    string rowColor = GetRowColorName(accountCode, category);

                    _incomeRows.Add(new IncomeStatementRow
                    {
                        Category = category,
                        AccountCode = accountCode,
                        AccountName = row["AccountName"].ToString(),
                        Balance = balance,
                        BalanceFormatted = balance.ToString("N2"),
                        Notes = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "",
                        RowColor = rowColor
                    });
                }

                ComputeFinancials(dt);
                UpdateSummaryLabels();

                ProgressBar1.Value = 100;

                // Reset after delay
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1.5)
                };
                timer.Tick += (s, args) => { ProgressBar1.Value = 0; timer.Stop(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                ProgressBar1.Value = 0;
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetRowColorName(string accountCode, string category)
        {
            switch (accountCode)
            {
                case "COST-AVAILABLE":
                case "COGS":
                    return "LightYellow";
                case "NET-INCOME":
                    return "LightGreen";
                case "GROSS-PROFIT":
                    return "LightCyan";
                case "41":
                    return "LightGreen";
                default:
                    if (category.Contains("الإيرادات"))
                        return "LightBlue";
                    if (category.Contains("المصروفات"))
                        return "Normal";
                    return "Normal";
            }
        }

        private void dgvIncomeData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق عند تغيير الصف المختار
        }

        #endregion

        #region Data Fetching

        public DataTable FetchIncomeData(DateTime fromDt, DateTime toDt, int branchId)
        {
            try
            {
                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("Category", typeof(string));
                dataTable.Columns.Add("AccountCode", typeof(string));
                dataTable.Columns.Add("AccountName", typeof(string));
                dataTable.Columns.Add("balance", typeof(decimal));
                dataTable.Columns.Add("Notes", typeof(string));

                using (SqlConnection sqlConn = new SqlConnection(connStr))
                {
                    sqlConn.Open();

                    // 1. الإيرادات
                    AddAccountGroup(sqlConn, dataTable, "1. الإيرادات", "41%", fromDt, toDt, branchId);

                    decimal salesReturns = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("الإيرادات")
                                 && r["AccountCode"].ToString().StartsWith("4100002"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal salesDiscounts = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("الإيرادات")
                                 && r["AccountCode"].ToString().StartsWith("4100003"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal totalRevenue = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("الإيرادات")
                                 && r["AccountCode"].ToString().StartsWith("41"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal netSales = totalRevenue - salesReturns - salesDiscounts;

                    if (netSales != 0m)
                        dataTable.Rows.Add("1. الإيرادات", "41", "صافي المبيعات", netSales, "");

                    // 2. تكلفة البضاعة المباعة - مخزون أول المدة
                    AddAccountGroup(sqlConn, dataTable, "2. تكلفة البضاعة المباعة", "1270002%", fromDt, toDt, branchId);

                    decimal openStock = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("تكلفة البضاعة المباعة")
                                 && r["AccountCode"].ToString().StartsWith("1270002"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    if (openStock <= 0m)
                        openStock = GetOpenstock();

                    if (openStock != 0m)
                        dataTable.Rows.Add("2. تكلفة البضاعة المباعة", "1270002", "مخزون أول المدة", openStock, "");

                    // المشتريات
                    AddAccountGroup(sqlConn, dataTable, "2. تكلفة البضاعة المباعة", "32%", fromDt, toDt, branchId);

                    decimal purchases = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("تكلفة البضاعة المباعة")
                                 && r["AccountCode"].ToString().StartsWith("3200001"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal purchaseReturns = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("تكلفة البضاعة المباعة")
                                 && r["AccountCode"].ToString().StartsWith("3200002"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal purchaseDiscounts = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("تكلفة البضاعة المباعة")
                                 && r["AccountCode"].ToString().StartsWith("3200003"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal availableForSale = openStock + purchases - purchaseReturns - purchaseDiscounts;

                    dataTable.Rows.Add("2. تكلفة البضاعة المباعة", "COST-AVAILABLE",
                        "تكلفة البضاعة المتاحة للبيع", availableForSale,
                        "بضاعة أول المدة + مشتريات - مرتجع - خصم مكتسب");

                    // مخزون آخر المدة
                    AddAccountGroup(sqlConn, dataTable, "2. تكلفة البضاعة المباعة", "1270001%", fromDt, toDt, branchId);

                    decimal closingStock = CalcStockCost();
                    if (closingStock != 0m)
                        dataTable.Rows.Add("2. تكلفة البضاعة المباعة", "1270001", "مخزون آخر المدة", closingStock, "");

                    decimal cogs = availableForSale - closingStock;
                    dataTable.Rows.Add("2. تكلفة البضاعة المباعة", "COGS",
                        "تكلفة البضاعة المباعة", cogs, "البضاعة المتاحة للبيع - مخزون آخر المدة");

                    decimal grossProfitVal = netSales - cogs;
                    dataTable.Rows.Add("3. مجمل الربح", "GROSS-PROFIT",
                        "مجمل الربح", grossProfitVal, "الإيرادات - تكلفة البضاعة المباعة");

                    // المصروفات
                    AddAccountGroupEx(sqlConn, dataTable, "4. المصروفات", fromDt, toDt, branchId);

                    decimal totalExpenses = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("المصروفات"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    if (totalExpenses != 0m)
                        dataTable.Rows.Add("4. المصروفات", "3", "صافي المصروفات", totalExpenses, "");

                    decimal netIncomeVal = grossProfitVal - totalExpenses;
                    dataTable.Rows.Add("5. صافي الدخل", "NET-INCOME",
                        "صافي الربح / (الخسارة)", netIncomeVal, "مجمل الربح - المصروفات");

                    // إيرادات أخرى
                    AddAccountGroup(sqlConn, dataTable, "6. إيرادات أخرى", "42%", fromDt, toDt, branchId);

                    decimal otherRevenue = dataTable.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("إيرادات أخرى")
                                 && r["AccountCode"].ToString().StartsWith("42"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                    decimal finalNetIncome = netIncomeVal + otherRevenue;
                    dataTable.Rows.Add("7. صافي الدخل", "NET-INCOME",
                        "صافي الربح / (الخسارة)", finalNetIncome, "صافي الدخل + إيرادات أخرى");
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في جلب البيانات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void AddAccountGroup(SqlConnection sqlConn, DataTable dt, string category,
            string codePattern, DateTime fromDt, DateTime toDt, int branchId)
        {
            string whereClause = "WHERE E.IS_Deleted = 0 AND E.[state] = 1 \n";

            if (ckTotalPeriod.IsChecked != true)
                whereClause += "AND E.[date] BETWEEN @date1 AND @date2 \n";

            if (branchId != -1)
                whereClause += "AND ES.branch = @branch \n";

            if (!codePattern.Contains("%") && !codePattern.Contains("_"))
                codePattern += "%";

            string sql = $@"
                SELECT 
                    ES.acc_no AS AccountCode, 
                    AI.AName AS AccountName, 
                    COALESCE(SUM(ES.dept),0) AS TotalDebit, 
                    COALESCE(SUM(ES.credit),0) AS TotalCredit, 
                    ABS(COALESCE(SUM(ES.dept),0) - COALESCE(SUM(ES.credit),0)) AS Balance 
                FROM Entry E 
                INNER JOIN Entry_sub ES ON E.GlobalId = ES.EntryGlobalId 
                INNER JOIN Accounts_Index AI ON ES.acc_no = AI.Code 
                {whereClause}
                AND AI.Code LIKE @pattern 
                GROUP BY ES.acc_no, AI.AName 
                HAVING COALESCE(SUM(ES.dept),0) <> 0 OR COALESCE(SUM(ES.credit),0) <> 0 
                ORDER BY ES.acc_no;";

            using (SqlCommand cmd = new SqlCommand(sql, sqlConn))
            {
                if (ckTotalPeriod.IsChecked != true)
                {
                    cmd.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    cmd.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                }

                if (branchId != -1)
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branchId;

                cmd.Parameters.Add("@pattern", SqlDbType.NVarChar, 200).Value = codePattern;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal balance = reader["Balance"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Balance"])
                            : 0m;

                        dt.Rows.Add(
                            category,
                            reader["AccountCode"].ToString(),
                            reader["AccountName"].ToString(),
                            balance,
                            "");
                    }
                }
            }
        }

        private void AddAccountGroupEx(SqlConnection sqlConn, DataTable dt, string category,
            DateTime fromDt, DateTime toDt, int branchId)
        {
            string whereClause = "WHERE E.IS_Deleted = 0 AND E.[state] = 1 \n";

            if (ckTotalPeriod.IsChecked != true)
                whereClause += "AND E.[date] BETWEEN @date1 AND @date2 \n";

            if (branchId != -1)
                whereClause += "AND ES.branch = @branch \n";

            string sql = $@"
                SELECT 
                    ES.acc_no AS AccountCode, 
                    AI.AName AS AccountName, 
                    COALESCE(SUM(ES.dept),0) AS TotalDebit, 
                    COALESCE(SUM(ES.credit),0) AS TotalCredit, 
                    ABS(COALESCE(SUM(ES.dept),0) - COALESCE(SUM(ES.credit),0)) AS Balance 
                FROM Entry E 
                INNER JOIN Entry_sub ES ON E.GlobalId = ES.EntryGlobalId 
                INNER JOIN Accounts_Index AI ON ES.acc_no = AI.Code 
                {whereClause}
                AND (
                    LEFT(AI.Code, 2) = '31' 
                    OR (TRY_CAST(LEFT(AI.Code, 2) AS INT) BETWEEN 33 AND 39) 
                ) 
                GROUP BY ES.acc_no, AI.AName 
                HAVING COALESCE(SUM(ES.dept),0) <> 0 OR COALESCE(SUM(ES.credit),0) <> 0 
                ORDER BY ES.acc_no;";

            using (SqlCommand cmd = new SqlCommand(sql, sqlConn))
            {
                if (ckTotalPeriod.IsChecked != true)
                {
                    cmd.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                    cmd.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;
                }

                if (branchId != -1)
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branchId;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        decimal balance = reader["Balance"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Balance"])
                            : 0m;

                        dt.Rows.Add(
                            category,
                            reader["AccountCode"].ToString(),
                            reader["AccountName"].ToString(),
                            balance,
                            "");
                    }
                }
            }
        }

        private decimal GetSumByPattern(SqlConnection sqlConn, string codePattern,
            DateTime fromDt, DateTime toDt, int branchId)
        {
            try
            {
                string whereClause = "WHERE E.IS_Deleted=0 AND E.state=1 ";

                if (ckTotalPeriod.IsChecked != true)
                    whereClause += "AND E.date BETWEEN @date1 AND @date2 ";

                if (branchId != -1)
                    whereClause += "AND ES.branch=@branch ";

                string sql = $@"
                    SELECT 
                        SUM(ES.credit) AS SumCredit,
                        SUM(ES.dept) AS SumDebit
                    FROM Entry E
                    INNER JOIN Entry_sub ES ON E.GlobalId = ES.EntryGlobalId
                    INNER JOIN Accounts_Index AI ON ES.acc_no = AI.Code
                    {whereClause}
                    AND AI.Code LIKE @pattern";

                using (SqlCommand cmd = new SqlCommand(sql, sqlConn))
                {
                    if (ckTotalPeriod.IsChecked != true)
                    {
                        cmd.Parameters.AddWithValue("@date1", fromDt);
                        cmd.Parameters.AddWithValue("@date2", toDt);
                    }

                    if (branchId != -1)
                        cmd.Parameters.AddWithValue("@branch", branchId);

                    cmd.Parameters.AddWithValue("@pattern", codePattern);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal credit = reader["SumCredit"] != DBNull.Value
                                ? Convert.ToDecimal(reader["SumCredit"]) : 0m;
                            decimal debit = reader["SumDebit"] != DBNull.Value
                                ? Convert.ToDecimal(reader["SumDebit"]) : 0m;
                            return credit - debit;
                        }
                    }
                }
            }
            catch { }

            return 0m;
        }

        #endregion

        #region Financial Calculations

        private void ClearFinancials()
        {
            revTotal = 0m;
            cogsTotal = 0m;
            grossProfit = 0m;
            expTotal = 0m;
            netIncome = 0m;
        }

        private void ComputeFinancials(DataTable dt)
        {
            try
            {
                revTotal = dt.AsEnumerable()
                    .Where(r => r["Category"].ToString().Contains("الإيرادات")
                             && !string.IsNullOrEmpty(r["AccountCode"].ToString())
                             && r["AccountCode"].ToString().StartsWith("4"))
                    .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                decimal cogsFromCode = dt.AsEnumerable()
                    .Where(r => string.Equals(r["AccountCode"].ToString(), "COGS"))
                    .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                if (cogsFromCode != 0m)
                    cogsTotal = cogsFromCode;
                else
                    cogsTotal = dt.AsEnumerable()
                        .Where(r => r["Category"].ToString().Contains("تكلفة البضاعة")
                                 && !string.Equals(r["AccountCode"].ToString(), "GROSS-PROFIT"))
                        .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                grossProfit = revTotal - cogsTotal;

                expTotal = dt.AsEnumerable()
                    .Where(r => r["Category"].ToString().Contains("المصروفات")
                             && !string.IsNullOrEmpty(r["AccountCode"].ToString())
                             && r["AccountCode"].ToString().StartsWith("31"))
                    .Sum(r => r["balance"] != DBNull.Value ? Convert.ToDecimal(r["balance"]) : 0m);

                netIncome = grossProfit - expTotal;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في حساب النتائج: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryLabels()
        {
            lblRevTotal.Text = revTotal.ToString("N2");
            lblCogsTotal.Text = cogsTotal.ToString("N2");
            lblGrossProfit.Text = grossProfit.ToString("N2");
            lblExpTotal.Text = expTotal.ToString("N2");
            lblNetIncome.Text = netIncome.ToString("N2");

            lblNetIncome.Foreground = netIncome >= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }

        private decimal GetOpenstock()
        {
            decimal result = 0m;
            try
            {
                using (SqlConnection sqlConn = new SqlConnection(MainClass.connstr))
                {
                    sqlConn.Open();
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT SUM(invsum) AS sumopenstock FROM inv WHERE inv_type=9 AND proc_type=1 AND Is_Deleted=0",
                        sqlConn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && reader["sumopenstock"] != DBNull.Value)
                            result = Convert.ToDecimal(reader["sumopenstock"]);
                    }
                }
            }
            catch { }

            return result;
        }

        public decimal CalcStockCost()
        {
            try
            {
                int branch = MainClass.BranchNo;

                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedIndex != -1
                    && cmbBranches.SelectedValue != null)
                {
                    if (int.TryParse(cmbBranches.SelectedValue.ToString(), out int bid))
                        branch = bid;
                }

                return (decimal)Inventory.InventoryCost(branch, txtToDate.DateTime);
            }
            catch
            {
                return 0m;
            }
        }

        #endregion

        #region Print & Export

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_incomeRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "تصدير قائمة الدخل",
                    FileName = "قائمة_الدخل_" + DateTime.Now.ToString("yyyy-MM-dd")
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveDialog.FileName);
                    DXMessageBox.Show("تم التصدير بنجاح!\n" + saveDialog.FileName, "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (DXMessageBox.Show("هل تريد فتح الملف؟", "فتح",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName)
                        {
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            // تصدير CSV بدلًا من XLSX (يفتح في Excel)
            using (StreamWriter sw = new StreamWriter(filePath.Replace(".xlsx", ".csv"),
                false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("المجموعة,رقم الحساب,اسم الحساب,الرصيد,ملاحظات");
                foreach (var row in _incomeRows)
                {
                    sw.WriteLine(
                        $"\"{row.Category}\"," +
                        $"\"{row.AccountCode}\"," +
                        $"\"{row.AccountName}\"," +
                        $"{row.Balance}," +
                        $"\"{row.Notes}\"");
                }
            }
        }

        private void PrintDevexpress(int printType)
        {
            RptName = "RptIncomeStatementNew.repx";

            if (_incomeRows.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(RptUrl) || !File.Exists(Path.Combine(RptUrl, RptName)))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(defPrinter))
            {
                DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var rpt = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, RptName));

                rpt.DataSource = BindToData();

                if (printType == 1)
                {
                    rpt.PrinterName = defPrinter;
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
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BindToData()
        {
            List<RestrictionData> list = new List<RestrictionData>();

            foreach (var row in _incomeRows)
            {
                list.Add(new RestrictionData
                {
                    AccountCode = row.AccountCode,
                    AccountName = row.AccountName,
                    Balance = row.Balance.ToString("N2"),
                    RestrType = "قائمة الدخل الرئيسية",
                    RestTime = "",
                    User = Common.GetEmpName(MainClass.EmpNo),
                    FromDate = txtFromDate.DateTime.ToShortDateString(),
                    ToDate = txtToDate.DateTime.ToShortDateString(),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            DataSet dataSet = new DataSet("Name");
            DataTable table = UtilitiesProj.Common.ToDataTable(list);
            dataSet.Tables.Add(table);
            return dataSet;
        }

        #endregion

        #region Print Settings

        private void LoadPrintSettings()
        {
            try
            {
                using (SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=0", conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count != 1) return;

                    try
                    {
                        if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pType))
                            PrintType = pType;

                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();

                        if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pNo))
                            PrintNo = pNo;

                        RptName = dt.Rows[0]["RptName"].ToString();
                        RptUrl = dt.Rows[0]["RptUrl"].ToString();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}