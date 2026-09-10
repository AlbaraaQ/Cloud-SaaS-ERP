using ClosedXML.Excel;
using DevExpress.Export.Xl;
using DevExpress.Spreadsheet;
using DevExpress.XtraReports.UI;
using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAccountsStatement : Window
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType;
        private int _printNo = 1;
        private string _defaultPrinter;
        private string _reportName = "";
        private string _reportUrl = "";
        private int _selectedId = -1;
        private string _searchName = "";
        private SqlConnection _connection;
        private int _dgvFontSize = 1;

        // إضافة هذا إذا لم يكن Common.DigitsNo موجوداً
        private const string DefaultDigitsFormat = "N2";

        #endregion

        #region Constructor

        public frmAccountsStatement()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDateFrom.SelectedDate = DateTime.Now;
                txtDateTo.SelectedDate = DateTime.Now;

                LoadPrintSettings();
                LoadAccounts();
                LoadBranches();
                ApplyBranchPermissions();

                cmbAccounts.Focus();
            }
            catch (Exception ex)
            {
                Logger.Error($"{this.Title} - Window_Loaded: {ex.Message} - {MainClass.UserName}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Data Methods

        private void LoadAccounts()
        {
            try
            {
                string query = "SELECT Code, AName FROM Accounts_Index WHERE Type=1 " + Accounting.BranchCondition;
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbAccounts.DisplayMemberPath = "AName";
                    cmbAccounts.SelectedValuePath = "Code";
                    cmbAccounts.ItemsSource = dt.DefaultView;
                    cmbAccounts.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadAccounts Error: {ex.Message}");
            }
        }

        private void LoadBranches()
        {
            try
            {
                string condition = "";
                if (MainClass.BranchNo != -1)
                {
                    condition = !string.IsNullOrEmpty(Accounting.BranchCondition)
                        ? $" AND id={MainClass.BranchNo}"
                        : "";
                }

                string query = $"SELECT id, name FROM Branches WHERE IS_Deleted=0 {condition}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";
                    cmbBranches.ItemsSource = dt.DefaultView;

                    if (MainClass.BranchNo != -1)
                    {
                        cmbBranches.SelectedValue = MainClass.BranchNo;
                    }
                    else
                    {
                        cmbBranches.SelectedIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadBranches Error: {ex.Message}");
            }
        }

        private void ApplyBranchPermissions()
        {
            if (!string.IsNullOrEmpty(Accounting.BranchCondition) &&
                Accounting.BranchCondition.Trim() != "")
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                if (cmbBranches.Items.Count > 0)
                {
                    cmbBranches.SelectedIndex = 0;
                }
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                string query = "SELECT * FROM SettingPrint WHERE Inv_Id=0";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 1)
                    {
                        _printType = Convert.ToInt32(dt.Rows[0]["printType"]);
                        _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        _defaultPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                        if (string.IsNullOrEmpty(_defaultPrinter))
                        {
                            _defaultPrinter = Common.GetDefaultPrinter();
                        }

                        _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                        _reportName = "Statement.repx";
                        _reportUrl = Path.GetDirectoryName(dt.Rows[0]["RptUrl"].ToString());

                        if (string.IsNullOrEmpty(_reportUrl) || !Directory.Exists(_reportUrl))
                        {
                            _reportUrl = MainClass.ReportsPath;
                        }
                    }
                    else
                    {
                        _reportUrl = MainClass.ReportsPath;
                        _reportName = "Statement.repx";
                        _defaultPrinter = MainClass.ReportsPrinter;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadPrintSettings Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل إعدادات الطباعة: {ex.Message}");
            }
        }

        private void LoadYearPreviews()
        {
            try
            {
                using (SqlConnection conn = MainClass.ConnObj())
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    DataTable dt = new DataTable();
                    string query = "SELECT Dbname, DbAutoName FROM Year_Previews WHERE Is_Deleted=0 ORDER BY id";

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                    }

                    DataRow currentRow = dt.NewRow();
                    currentRow["Dbname"] = MainClass.DataBaseName;
                    currentRow["DbAutoName"] = MainClass.Database;
                    dt.Rows.InsertAt(currentRow, 0);

                    CmbYearPreviews.DisplayMemberPath = "Dbname";
                    CmbYearPreviews.SelectedValuePath = "DbAutoName";
                    CmbYearPreviews.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadYearPreviews Error: {ex.Message}");
                MessageBox.Show($"حدث خطأ: {ex.Message}");
            }
        }

        #endregion

        #region Account Statement Methods

        private DataTable GetAccountStatement(string parentCode, DateTime? startDate, DateTime? endDate, string branch)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connection.ConnectionString))
                using (SqlCommand cmd = new SqlCommand("GetAccountStatement", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ParentCode", parentCode);
                    cmd.Parameters.AddWithValue("@StartDate", startDate.HasValue ? (object)startDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@EndDate", endDate.HasValue ? (object)endDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Branch", string.IsNullOrEmpty(branch) ? (object)DBNull.Value : branch);
                    cmd.Parameters.AddWithValue("@ShowPreviousBalance",
                        (chkPrevbalance.IsChecked == true || ckTotalPeriod.IsChecked == true) ? 0 : 1);

                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetAccountStatement Error: {ex.Message}");
                throw new ApplicationException("خطأ في استرجاع كشف الحساب.", ex);
            }

            return dataTable;
        }

        private void ShowResult()
        {
            try
            {
                DateTime? startDate = null;
                DateTime? endDate = null;
                string branch = "";

                if (cmbAccounts.SelectedValue == null)
                {
                    MessageBox.Show("اختر حساب", "", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    cmbAccounts.Focus();
                    return;
                }

                string parentCode = cmbAccounts.SelectedValue.ToString();

                if (chkAllBranches.IsChecked == false && cmbBranches.SelectedIndex != -1)
                {
                    branch = cmbBranches.SelectedValue.ToString();
                }

                if (ckTotalPeriod.IsChecked == false)
                {
                    startDate = txtDateFrom.SelectedDate;
                    endDate = txtDateTo.SelectedDate;
                }

                DataTable accountStatement = GetAccountStatement(parentCode, startDate, endDate, branch);
                dgvMain.ItemsSource = accountStatement.DefaultView;

                if (accountStatement.Rows.Count == 0)
                {
                    MessageBox.Show("لا توجد نتائج");
                    return;
                }

                CalculateTotals(accountStatement);
            }
            catch (Exception ex)
            {
                Logger.Error($"{this.Title} - ShowResult Error: {ex.Message} - {MainClass.UserName}");
                MessageBox.Show($"خطأ في عرض النتائج: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateTotals(DataTable dt)
        {
            try
            {
                double totalDebit = 0;
                double totalCredit = 0;

                foreach (DataRow row in dt.Rows)
                {
                    if (row["DgvDept"] != DBNull.Value)
                        totalDebit += Convert.ToDouble(row["DgvDept"]);

                    if (row["DgvCredit"] != DBNull.Value)
                        totalCredit += Convert.ToDouble(row["DgvCredit"]);
                }

                txtTotDept.Text = totalDebit.ToString("N2");
                txtTotCredit.Text = totalCredit.ToString("N2");

                if (totalCredit > totalDebit)
                {
                    txtBalance1.Text = (totalCredit - totalDebit).ToString("N2");
                    txtBalance2.Text = "رصيد دائن";
                }
                else if (totalDebit > totalCredit)
                {
                    txtBalance1.Text = (totalDebit - totalCredit).ToString("N2");
                    txtBalance2.Text = "رصيد مدين";
                }
                else
                {
                    txtBalance1.Text = "0.00";
                    txtBalance2.Text = "رصيد متوازن";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"CalculateTotals Error: {ex.Message}");
            }
        }

        #endregion

        #region Search Methods

        private void SearchByCode()
        {
            try
            {
                string code = txtCode.Text.Trim();

                if (string.IsNullOrEmpty(code))
                    return;

                _searchName = "";

                string query = $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND Code={code}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbAccounts.SelectedValue = Convert.ToInt32(dt.Rows[0]["Code"]);
                        txtCode.Text = dt.Rows[0]["Code"].ToString();
                    }
                    else
                    {
                        ShowAccountSearchDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SearchByCode Error: {ex.Message}");
            }
        }

        private void SearchByName()
        {
            try
            {
                _searchName = cmbAccounts.Text;

                string query = $"SELECT Code, AName FROM Accounts_Index WHERE AName = N'{_searchName}' AND type=2";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbAccounts.SelectedValue = Convert.ToInt32(dt.Rows[0]["Code"]);
                        txtCode.Text = dt.Rows[0]["Code"].ToString();
                    }
                    else
                    {
                        ShowAccountSearchDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SearchByName Error: {ex.Message}");
            }
        }

        private void ShowAccountSearchDialog()
        {
            try
            {
                // يجب تحويل frmAccountSrchParent إلى WPF أيضاً
                // هذا مثال افتراضي - يحتاج للتعديل حسب التطبيق الفعلي
                MessageBox.Show("وظيفة البحث المتقدم تحتاج تحويل نافذة frmAccountSrchParent إلى WPF",
                    "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"ShowAccountSearchDialog Error: {ex.Message}");
            }
        }

        #endregion

        #region Export & Print Methods

        private void ExportToExcel()
        {
            try
            {
                if (dgvMain.Items.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "حفظ ملف Excel",
                    FileName = $"{this.Title}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    DataTable dt = ((DataView)dgvMain.ItemsSource).Table;

                    using (XLWorkbook workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(dt, "كشف الحساب");

                        // تنسيق الجدول
                        worksheet.RightToLeft = true;
                        worksheet.Columns().AdjustToContents();

                        // تنسيق الهيدر
                        var headerRange = worksheet.Range(1, 1, 1, dt.Columns.Count);
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("تم التصدير بنجاح", "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ExportToExcel Error: {ex.Message}");
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Print & Report Methods

        private void PrintDevexpress(int type)
        {
            try
            {
                _reportName = "Statement2.repx";

                if (dgvMain.Items.Count == 0)
                {
                    MessageBox.Show("لا توجد عمليات بالجدول", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                if (string.IsNullOrEmpty(_reportUrl))
                {
                    MessageBox.Show("يجب تحديد مسار التقرير", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                if (!Directory.Exists(_reportUrl) || !File.Exists(Path.Combine(_reportUrl, _reportName)))
                {
                    MessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                if (string.IsNullOrEmpty(_reportName))
                {
                    MessageBox.Show("يجب إدخال اسم التقرير من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                // تحميل التقرير الرئيسي
                XtraReport mainReport = XtraReport.FromFile(Path.Combine(_reportUrl, _reportName));
                mainReport.DataSource = BindToData();

                // تحميل تقرير الهيدر
                string headerPath = Path.Combine(_reportUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    XtraReport headerReport = XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;

                    XRSubreport headerSubreport = (XRSubreport)mainReport.FindControl("headerRpt", true);
                    if (headerSubreport != null)
                    {
                        headerSubreport.ReportSource = headerReport;
                    }
                }

                // تحميل تقرير الفوتر
                string footerPath = Path.Combine(_reportUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    XtraReport footerReport = XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;

                    XRSubreport footerSubreport = (XRSubreport)mainReport.FindControl("footerRpt", true);
                    if (footerSubreport != null)
                    {
                        footerSubreport.ReportSource = footerReport;
                    }
                }

                if (!string.IsNullOrEmpty(_defaultPrinter))
                {
                    mainReport.PrinterName = _defaultPrinter;

                    if (type == 1) // طباعة مباشرة
                    {
                        for (int i = 0; i < _printNo; i++)
                        {
                            mainReport.Print();
                        }
                    }
                    else // معاينة
                    {
                        mainReport.ShowPreviewDialog();
                    }

                    mainReport.Dispose();
                }
                else
                {
                    MessageBox.Show("يجب تحديد الطابعة من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"PrintDevexpress Error: {ex.Message}");
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BindToData()
        {
            DataSet dataSet = new DataSet("Name");

            try
            {
                // تحميل معلومات المؤسسة
                string query = "SELECT * FROM Foundation";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable foundationDT = new DataTable();
                    adapter.Fill(foundationDT);

                    string address = "";
                    string telephone = "";
                    string mobile = "";
                    string foundation = "";
                    string field = "";
                    string vatNo = "";

                    if (foundationDT.Rows.Count > 0)
                    {
                        address = foundationDT.Rows[0]["Address"]?.ToString() ?? "";
                        telephone = foundationDT.Rows[0]["Tel"]?.ToString() ?? "";
                        mobile = foundationDT.Rows[0]["Mobile"]?.ToString() ?? "";
                        foundation = foundationDT.Rows[0]["nameA"]?.ToString() ?? "";
                        field = foundationDT.Rows[0]["FieldA"]?.ToString() ?? "";
                        vatNo = foundationDT.Rows[0]["tax_no"]?.ToString() ?? "";

                        // تحميل الشعار إذا كان موجوداً
                        if (foundationDT.Rows[0]["Logo"] != DBNull.Value)
                        {
                            try
                            {
                                byte[] logoBytes = (byte[])foundationDT.Rows[0]["Logo"];
                                MainClass.Arr2Image(logoBytes);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Error loading logo: {ex.Message}");
                            }
                        }
                    }

                    // إنشاء قائمة البيانات للتقرير
                    List<RestrictionData> reportDataList = new List<RestrictionData>();

                    if (dgvMain.ItemsSource != null)
                    {
                        DataView dv = (DataView)dgvMain.ItemsSource;
                        DataTable dt = dv.Table;

                        double totalDebit = 0;
                        double totalCredit = 0;

                        // حساب الإجماليات
                        if (!string.IsNullOrEmpty(txtTotDept.Text))
                        {
                            double.TryParse(txtTotDept.Text, out totalDebit);
                        }

                        if (!string.IsNullOrEmpty(txtTotCredit.Text))
                        {
                            double.TryParse(txtTotCredit.Text, out totalCredit);
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            RestrictionData data = new RestrictionData
                            {
                                AccountName = cmbAccounts.Text ?? "",
                                AccountNameBranch = row["DgvAccName"]?.ToString() ?? "",
                                AccountCode = cmbAccounts.SelectedValue?.ToString() ?? "",
                                Description = row["DgvNote"]?.ToString() ?? "",
                                Credit = row["DgvCredit"] == DBNull.Value ? "" : row["DgvCredit"]?.ToString(),
                                Dept = row["DgvDept"] == DBNull.Value ? "" : row["DgvDept"]?.ToString(),
                                Status = "",
                                Balance = "",
                                ProcessNo = row["DgvEntryNo"] == DBNull.Value ? "" : row["DgvEntryNo"]?.ToString(),
                                ProcessType = row["DgvEntryType"] == DBNull.Value ? "" : row["DgvEntryType"]?.ToString(),
                                RestrNo = row["DgvGenralNo"] == DBNull.Value ? "" : row["DgvGenralNo"]?.ToString(),
                                Branch = row["DgvBranch"] == DBNull.Value ? "" : row["DgvBranch"]?.ToString(),
                                RestrType = this.Title,
                                RestDate = row["DgvDate"]?.ToString() ?? "",
                                RestTime = "",
                                FromDate = txtDateFrom.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                                ToDate = txtDateTo.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                                User = Common.GetEmpName(MainClass.EmpNo),
                                SumDept = totalDebit.ToString(Common.DigitsNo),
                                SumCredit = totalCredit.ToString(Common.DigitsNo),
                                PrintDate = DateTime.Now.ToString("yyyy-MM-dd"),
                                Address = address,
                                Mobile = mobile,
                                TelePhone = telephone,
                                VatNo = vatNo,
                                Foundation = foundation,
                                Field = field,
                                Logo = "",
                                Header = "",
                                footer = "",
                                Stamp = ""
                            };

                            // تحديد نوع ورصيد الفترة
                            if (totalCredit > totalDebit)
                            {
                                data.BalanceInperiod = (totalCredit - totalDebit).ToString(Common.DigitsNo) + "  ";
                                data.BalanceType = "دائن ";
                            }
                            else if (totalDebit > totalCredit)
                            {
                                data.BalanceInperiod = (totalDebit - totalCredit).ToString(Common.DigitsNo) + "  ";
                                data.BalanceType = "مدين ";
                            }
                            else
                            {
                                data.BalanceInperiod = "0";
                                data.BalanceType = "";
                            }

                            // تحويل المبلغ إلى حروف عربية
                            if (!string.IsNullOrEmpty(data.BalanceInperiod))
                            {
                                double balance = 0;
                                if (double.TryParse(data.BalanceInperiod.Replace(" ", ""), out balance))
                                {
                                    data.ArabicLetter = InvoiceOper.ToArabicLetter(balance);
                                }
                            }

                            reportDataList.Add(data);
                        }
                    }

                    // تحويل القائمة إلى DataTable
                    DataTable reportTable = ConvertToDataTable(reportDataList);
                    dataSet.Tables.Add(reportTable);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"BindToData Error: {ex.Message}");
                Console.WriteLine(ex.Message);
            }

            return dataSet;
        }

        /// <summary>
        /// تحويل List إلى DataTable
        /// </summary>
        private DataTable ConvertToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            // الحصول على جميع الخصائص
            var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public |
                                                     System.Reflection.BindingFlags.Instance);

            // إضافة الأعمدة
            foreach (var prop in properties)
            {
                Type propertyType = prop.PropertyType;

                // التعامل مع Nullable types
                if (propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                dataTable.Columns.Add(prop.Name, propertyType);
            }

            // إضافة الصفوف
            foreach (T item in items)
            {
                var values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(item, null) ?? DBNull.Value;
                }
                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        #endregion
        #endregion

        #region Event Handlers

        private void cmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbAccounts.SelectedValue != null)
                {
                    txtCode.Text = cmbAccounts.SelectedValue.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"cmbAccounts_SelectionChanged Error: {ex.Message}");
            }
        }

        private void cmbAccounts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowAccountSearchDialog();
        }

        private void txtCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchByCode();
            }
        }

        private void chkAllBranches_Toggle(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked == false;
        }

        private void ckTotalPeriod_Toggle(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isEnabled = ckTotalPeriod.IsChecked == false;
            txtDateFrom.IsEnabled = isEnabled;
            txtDateTo.IsEnabled = isEnabled;
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.ReturnYearPreviews && CmbYearPreviews.SelectedValue != null)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr = $"server={MainClass.Server};database={CmbYearPreviews.SelectedValue};trusted_connection=true";
                _connection = MainClass.ConnObj();
            }

            ShowResult();

            if (MainClass.ReturnYearPreviews)
            {
                MainClass.connstr = MainClass.originalConnStr;
                MainClass.ReturnYearPreviews = false;
                _connection = MainClass.ConnObj();
            }
        }

        private void CmbYearPreviews_DropDown(object sender, EventArgs e)
        {
            LoadYearPreviews();
        }

        private void CmbYearPreviews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbYearPreviews.SelectedIndex >= 0)
            {
                MainClass.ReturnYearPreviews = true;
            }
        }

        private void BtnShowDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn?.Tag != null)
                {
                    string genralNo = btn.Tag.ToString();
                    if (genralNo != "0" && !string.IsNullOrEmpty(genralNo))
                    {
                        EntryOper.ShowEntrySource(genralNo);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"BtnShowDetail_Click Error: {ex.Message}");
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2); // 2 = معاينة
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1); // 1 = طباعة مباشرة
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            ChangeFontSize(_dgvFontSize);
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ChangeFontSize(-_dgvFontSize);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Helper Methods

        private void ChangeFontSize(int delta)
        {
            try
            {
                double currentSize = dgvMain.FontSize;
                double newSize = currentSize + delta;

                if (newSize >= 8 && newSize <= 24)
                {
                    dgvMain.FontSize = newSize;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ChangeFontSize Error: {ex.Message}");
            }
        }

        private string GetEmpUserName(int emp)
        {
            try
            {
                string query = $"SELECT users.username FROM users, Employees WHERE users.emp = Employees.id AND Employees.id = {emp}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return dt.Rows[0][0]?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetEmpUserName Error: {ex.Message}");
            }

            return "";
        }

        #endregion
    }

    #region Helper Classes
    
    

    #endregion
}