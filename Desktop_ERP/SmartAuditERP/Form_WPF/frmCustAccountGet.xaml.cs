using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCustAccountGet : ThemedWindow
    {
        #region ══════════════════ Fields ══════════════════

        private SqlConnection _conn;

        private bool _printHeader;
        private bool _printFooter;
        private bool _printStamp;
        private int _printType;
        private int _printNo;
        private string _defaultPrinter;
        private string _reportName;
        private string _reportUrl;

        #endregion

        #region ══════════════════ Inner Model ══════════════════

        /// <summary>
        /// يحمل كل قيم UI اللازمة للاستعلام.
        /// يُملأ على UI Thread ثم يُمرَّر للـ Background Thread.
        /// </summary>
        private class ShowAccountParams
        {
            public bool ClientSelected { get; set; }
            public bool IsSupplier { get; set; }
            public object ClientAccountCode { get; set; }
            public string PeriodFilter { get; set; }
            public string BranchFilter { get; set; }
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
        }

        #endregion

        #region ══════════════════ Constructor ══════════════════

        public frmCustAccountGet()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;
            _defaultPrinter = string.Empty;

            Loaded += FrmCustAccountGet_Loaded;
        }

        #endregion

        #region ══════════════════ Window Loaded ══════════════════

        private void FrmCustAccountGet_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureConnectionOpen(_conn);

                txtHeaderDate.Text = DateTime.Now.ToLongDateString();
                txtDateFrom.DateTime = DateTime.Now;
                txtDateTo.DateTime = DateTime.Now;

                LoadBranches();
                LoadPrintSettings();
                ApplyBranchPermissions();

                // ربط الأحداث
                btnShow.Click += BtnShow_Click;
                btnPreview.Click += BtnPreview_Click;
                btnPrint.Click += BtnPrint_Click;
                btnExport.Click += BtnExport_Click;
                btnClose.Click += BtnClose_Click;

                rdAll.Checked += RdAll_Checked;
                rdAllClients.Checked += RdAllClients_Checked;
                rdAllSuppliers.Checked += RdAllSuppliers_Checked;

                chkAllBranches.Checked += ChkAllBranches_Changed;
                chkAllBranches.Unchecked += ChkAllBranches_Changed;

                ckTotalPeriod.Checked += CkTotalPeriod_Changed;
                ckTotalPeriod.Unchecked += CkTotalPeriod_Changed;

                cmbBranches.EditValueChanged += CmbBranches_EditValueChanged;
                cmbClients.MouseDown += CmbClients_MouseDown;

                // القيم الافتراضية — تُطلق أحداث Checked تلقائياً
                rdAll.IsChecked = true;
                chkAll.IsChecked = true;
                chkAllBranches.IsChecked = true;
                ckTotalPeriod.IsChecked = true;

                // تحميل الحسابات بعد ضبط الحالة الافتراضية
                LoadAccounts();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ Data Loading ══════════════════

        /// <summary>
        /// يجب استدعاؤها من UI Thread فقط
        /// </summary>
        private void LoadAccounts()
        {
            try
            {
                EnsureConnectionOpen(_conn);

                string branchFilter = BuildBranchFilter();
                string query = BuildAccountsQuery(branchFilter);
                if (string.IsNullOrEmpty(query)) return;

                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbClients.ItemsSource = dataTable.DefaultView;
                cmbClients.DisplayMember = "name";
                cmbClients.ValueMember = "AccountCode";
                cmbClients.EditValue = null;

                // تحديث عنوان عمود الجدول
                if (DgvClient != null)
                {
                    DgvClient.Header =
                        rdAllSuppliers.IsChecked == true
                            ? (MainClass.Language == "ar" ? "المورد" : "Supplier")
                            : (MainClass.Language == "ar" ? "العميل" : "Client");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private string BuildAccountsQuery(string branchFilter)
        {
            const string baseCondition =
                "AND AccountCode <> -1 AND IS_Deleted = 0 ";

            if (rdAll.IsChecked == true)
                return $"SELECT * FROM Customers " +
                       $"WHERE 1=1 {baseCondition}{branchFilter}";

            if (rdAllClients.IsChecked == true)
                return $"SELECT * FROM Customers " +
                       $"WHERE (type=1 OR type=3) {baseCondition}{branchFilter}";

            if (rdAllSuppliers.IsChecked == true)
                return $"SELECT * FROM Customers " +
                       $"WHERE (type=2 OR type=3) {baseCondition}{branchFilter}";

            return string.Empty;
        }

        private void LoadBranches()
        {
            try
            {
                EnsureConnectionOpen(_conn);

                string branchCondition = string.Empty;
                if (MainClass.BranchNo != -1 &&
                    !string.IsNullOrWhiteSpace(Accounting.BranchCondition))
                {
                    branchCondition = $" AND id={MainClass.BranchNo}";
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches " +
                    $"WHERE IS_Deleted=0{branchCondition}",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbBranches.ItemsSource = dataTable.DefaultView;
                cmbBranches.DisplayMember = "name";
                cmbBranches.ValueMember = "id";
                cmbBranches.EditValue = null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ApplyBranchPermissions()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition))
                {
                    chkAllBranches.IsChecked = false;
                    chkAllBranches.Visibility = Visibility.Collapsed;
                    cmbBranches.IsEnabled = true;

                    var dv = cmbBranches.ItemsSource as DataView;
                    if (dv != null && dv.Count > 0)
                        cmbBranches.EditValue = dv[0]["id"];
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureConnectionOpen(_conn);

                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count != 1) return;

                DataRow row = dataTable.Rows[0];
                _printType = Convert.ToInt32(row["printType"]);
                _printFooter = Convert.ToBoolean(row["PrintFooter"]);
                _printHeader = Convert.ToBoolean(row["PrintHeader"]);
                _printStamp = Convert.ToBoolean(row["PrintStamp"]);
                _defaultPrinter = row["CasherPrinter"]?.ToString() ?? string.Empty;
                _printNo = Convert.ToInt32(row["printNo"]);

                if (string.IsNullOrEmpty(_defaultPrinter))
                    _defaultPrinter = Common.GetDefaultPrinter();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ Show Account Logic ══════════════════

        /// <summary>
        /// يجمع كل قيم UI على UI Thread بأمان
        /// </summary>
        private ShowAccountParams CollectUIParameters()
        {
            bool clientSelected =
                cmbClients.EditValue != null &&
                !string.IsNullOrEmpty(cmbClients.Text);

            bool isSupplier = rdAllSuppliers.IsChecked == true;

            object clientCode = cmbClients.EditValue;

            // فلتر الفترة
            string periodFilter = string.Empty;
            if (ckTotalPeriod.IsChecked != true)
                periodFilter =
                    " AND Entry.date >= @date1 AND Entry.date <= @date2";

            // فلتر الفرع
            string branchFilter = string.Empty;
            if (chkAllBranches.IsChecked != true &&
                cmbBranches.EditValue != null)
            {
                branchFilter =
                    $"Entry.branch={cmbBranches.EditValue} " +
                    $"AND Entry_sub.branch={cmbBranches.EditValue} AND ";
            }

            DateTime dateFrom = txtDateFrom.DateTime.Date;
            DateTime dateTo = txtDateTo.DateTime.AddHours(24);

            return new ShowAccountParams
            {
                ClientSelected = clientSelected,
                IsSupplier = isSupplier,
                ClientAccountCode = clientCode,
                PeriodFilter = periodFilter,
                BranchFilter = branchFilter,
                DateFrom = dateFrom,
                DateTo = dateTo,
            };
        }

        /// <summary>
        /// تنفيذ الاستعلام والمعالجة على Background Thread
        /// </summary>
        private void ShowAccount(ShowAccountParams p)
        {
            try
            {
                // التحقق من اختيار العميل
                if (!p.ClientSelected)
                {
                    Dispatcher.Invoke(() =>
                    {
                        string msg = p.IsSupplier ? "اختر المورد" : "اختر عميل";
                        DXMessageBox.Show(msg, "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        cmbClients.Focus();
                    });
                    return;
                }

                // بناء الاستعلام بالقيم المُمرَّرة فقط (لا نلمس UI هنا)
                string accountFilter =
                    $" AND Entry_sub.acc_no={p.ClientAccountCode}";

                string query =
                    $@"SELECT Entry.GlobalID,
                              Entry.date,
                              SUM(Entry_sub.dept)   AS dept,
                              SUM(Entry_sub.credit) AS credit,
                              Entry_sub.notes       AS notes,
                              Entry_sub.acc_no      AS client
                       FROM Entry
                       INNER JOIN Entry_sub
                               ON Entry.GlobalID = Entry_sub.EntryGlobalID
                       INNER JOIN Accounts_Index
                               ON Entry_sub.acc_no = Accounts_Index.Code
                       WHERE {p.BranchFilter}
                             Entry.IS_Deleted = 0
                         AND Entry.state      = 1
                             {accountFilter}
                             {p.PeriodFilter}
                       GROUP BY Entry.GlobalID,
                                Entry.date,
                                Entry_sub.notes,
                                Entry_sub.acc_no
                       ORDER BY Entry.date";

                // ✅ اتصال منفصل مخصص للـ Background Thread
                using (var bgConn = MainClass.ConnObj())
                {
                    EnsureConnectionOpen(bgConn);

                    var adapter = new SqlDataAdapter(query, bgConn);

                    // إضافة بارامترات التاريخ فقط إذا كان الفلتر موجوداً
                    if (!string.IsNullOrEmpty(p.PeriodFilter))
                    {
                        adapter.SelectCommand.Parameters.Add(
                            "@date1", SqlDbType.DateTime).Value = p.DateFrom;
                        adapter.SelectCommand.Parameters.Add(
                            "@date2", SqlDbType.DateTime).Value = p.DateTo;
                    }

                    var rawData = new DataTable();
                    adapter.Fill(rawData);

                    int rowCount = rawData.Rows.Count;

                    // تهيئة ProgressBar على UI Thread
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.Maximum = rowCount > 0 ? rowCount : 1;
                        ProgressBar1.EditValue = 0;
                        ClearSummary();
                    });

                    // بناء جدول النتائج بالكامل على Background Thread
                    var resultTable = BuildResultTable();
                    double totalDebit = 0.0;
                    double totalCredit = 0.0;

                    for (int i = 0; i < rowCount; i++)
                    {
                        DataRow row = rawData.Rows[i];

                        double debitAmount = Convert.ToDouble(row["dept"]);
                        double creditAmount = Convert.ToDouble(row["credit"]);
                        string entryNumber = row["GlobalID"]?.ToString() ?? string.Empty;
                        string entryDate =
                            Convert.ToDateTime(row["date"]).ToShortDateString();
                        string entryNote = row["notes"]?.ToString() ?? string.Empty;

                        // ✅ استعلام منفصل يستخدم bgConn (آمن على Background Thread)
                        string clientName =
                            GetClientNameBg(row["client"]?.ToString(), bgConn);

                        totalDebit += debitAmount;
                        totalCredit += creditAmount;

                        resultTable.Rows.Add(
                            resultTable.Rows.Count + 1,
                            debitAmount,
                            creditAmount,
                            entryNumber,
                            entryDate,
                            entryNote,
                            clientName);

                        // تحديث ProgressBar بأمان
                        int progress = i + 1;
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar1.EditValue = progress;
                        });
                    }

                    // تحديث الجدول والملخص على UI Thread
                    double finalDebit = totalDebit;
                    double finalCredit = totalCredit;
                    int finalCount = rowCount;
                    var finalTable = resultTable;

                    Dispatcher.Invoke(() =>
                    {
                        GridControl1.ItemsSource = finalTable.DefaultView;
                        txtRecordCount.Text = finalCount.ToString();
                        UpdateSummary(finalDebit, finalCredit);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex.Message));
            }
        }

        private DataTable BuildResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("DgvNo", typeof(int));
            table.Columns.Add("DgvDept", typeof(double));
            table.Columns.Add("DgvCredit", typeof(double));
            table.Columns.Add("DgvEntryNo", typeof(string));
            table.Columns.Add("DgvDate", typeof(string));
            table.Columns.Add("DgvNote", typeof(string));
            table.Columns.Add("DgvClient", typeof(string));
            return table;
        }

        private void UpdateSummary(double totalDebit, double totalCredit)
        {
            txtTotDept.Text = totalDebit.ToString(Common.DigitsNo);
            txtTotCredit.Text = totalCredit.ToString(Common.DigitsNo);

            double balance = Math.Abs(totalDebit - totalCredit);

            if (totalDebit > totalCredit)
            {
                txtBalance1.Text = balance.ToString(Common.DigitsNo);
                txtBalance2.Text = "0";
            }
            else if (totalCredit > totalDebit)
            {
                txtBalance2.Text = balance.ToString(Common.DigitsNo);
                txtBalance1.Text = "0";
            }
            else
            {
                txtBalance1.Text = "0";
                txtBalance2.Text = "0";
            }
        }

        private void ClearSummary()
        {
            // يُستدعى دائماً داخل Dispatcher.Invoke → آمن
            GridControl1.ItemsSource = null;
            txtTotDept.Text = string.Empty;
            txtTotCredit.Text = string.Empty;
            txtBalance1.Text = string.Empty;
            txtBalance2.Text = string.Empty;
            txtRecordCount.Text = "0";
        }

        #endregion

        #region ══════════════════ Query Helpers (UI Thread Only) ══════════════════

        /// <summary>استدعِ من UI Thread فقط</summary>
        private string BuildBranchFilter()
        {
            if (chkAllBranches.IsChecked != true &&
                cmbBranches.EditValue != null)
                return $" AND Branch={cmbBranches.EditValue}";

            return string.Empty;
        }

        /// <summary>استدعِ من UI Thread فقط</summary>
        private string BuildEntryBranchFilter()
        {
            if (chkAllBranches.IsChecked != true &&
                cmbBranches.EditValue != null)
            {
                return $"Entry.branch={cmbBranches.EditValue} " +
                       $"AND Entry_sub.branch={cmbBranches.EditValue} AND ";
            }
            return string.Empty;
        }

        /// <summary>استدعِ من UI Thread فقط</summary>
        private string BuildPeriodFilter()
        {
            if (ckTotalPeriod.IsChecked != true)
                return " AND Entry.date >= @date1 AND Entry.date <= @date2";

            return string.Empty;
        }

        /// <summary>استدعِ من UI Thread فقط</summary>
        private object GetClientAccountCode()
        {
            return cmbClients.EditValue;
        }

        #endregion

        #region ══════════════════ Database Helpers ══════════════════

        /// <summary>
        /// يعمل على UI Thread — يستخدم _conn
        /// </summary>
        private string GetClientName(string accountCode)
        {
            try
            {
                EnsureConnectionOpen(_conn);

                var adapter = new SqlDataAdapter(
                    $"SELECT AName FROM Accounts_Index " +
                    $"WHERE Code=N'{accountCode}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// ✅ يعمل على Background Thread — يستخدم bgConn المنفصل
        /// </summary>
        private string GetClientNameBg(string accountCode, SqlConnection bgConn)
        {
            try
            {
                if (string.IsNullOrEmpty(accountCode)) return string.Empty;

                var adapter = new SqlDataAdapter(
                    $"SELECT AName FROM Accounts_Index " +
                    $"WHERE Code=N'{accountCode}'",
                    bgConn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int GetClientCodeById(object clientId)
        {
            try
            {
                EnsureConnectionOpen(_conn);

                var adapter = new SqlDataAdapter(
                    $"SELECT AccountCode FROM Customers " +
                    $"WHERE Id=N'{clientId}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? Convert.ToInt32(table.Rows[0][0])
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// يضمن أن الاتصال مفتوح ويعيد فتحه إذا انقطع
        /// </summary>
        private static void EnsureConnectionOpen(SqlConnection conn)
        {
            if (conn == null)
                throw new InvalidOperationException("الاتصال بقاعدة البيانات غير مهيأ.");

            if (conn.State == ConnectionState.Broken)
                conn.Close();

            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        #endregion

        #region ══════════════════ Print & Export ══════════════════

        private DataSet BindToReportData()
        {
            var reportList = new List<RestrictionData>();

            var dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null) return new DataSet("Name");

            DataTable sourceTable = dataView.Table;

            foreach (DataRow row in sourceTable.Rows)
            {
                reportList.Add(new RestrictionData
                {
                    Clint = cmbClients.Text,
                    ClintCode = cmbClients.EditValue?.ToString() ?? string.Empty,
                    FromDate = txtDateFrom.DateTime.ToShortDateString(),
                    ToDate = txtDateTo.DateTime.ToShortDateString(),
                    Dept = row["DgvDept"]?.ToString(),
                    Credit = row["DgvCredit"]?.ToString(),
                    RestrNo = row["DgvEntryNo"]?.ToString(),
                    RestDate = row["DgvDate"]?.ToString(),
                    Note = row["DgvNote"]?.ToString(),
                    ClintName = row["DgvClient"]?.ToString(),
                    Status = " ",
                    RestrType = this.Title,
                    SumDept = txtTotDept.Text,
                    TotCredit = txtTotCredit.Text,
                    DeptBalance = txtBalance1.Text,
                    CreditBalance = txtBalance2.Text,
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(reportList));
            return dataSet;
        }

        private void PrintDevexpress(int printMode)
        {
            try
            {
                _reportUrl = MainClass.ReportsPath;
                _reportName = "rptCustAccountGet.repx";
                _defaultPrinter = MainClass.ReportsPrinter;

                var dataView = GridControl1.ItemsSource as DataView;
                if (dataView == null || dataView.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_reportUrl))
                {
                    DXMessageBox.Show("يجب تحديد مسار التقرير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fullPath = Path.Combine(_reportUrl, _reportName);
                if (!Directory.Exists(_reportUrl) || !File.Exists(fullPath))
                {
                    DXMessageBox.Show(
                        "المسار الحالي للتقارير غير موجود أو تم تعديله",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(_defaultPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var mainReport = XtraReport.FromFile(fullPath);
                mainReport.DataSource = BindToReportData();

                // إرفاق تقرير الرأس
                string headerPath = Path.Combine(_reportUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport = XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;
                    var headerSub = mainReport.FindControl(
                        "headerRpt", ignoreCase: true) as XRSubreport;
                    if (headerSub != null)
                        headerSub.ReportSource = headerReport;
                }

                // إرفاق تقرير التذييل
                string footerPath = Path.Combine(_reportUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerReport = XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;
                    var footerSub = mainReport.FindControl(
                        "footerRpt", ignoreCase: true) as XRSubreport;
                    if (footerSub != null)
                        footerSub.ReportSource = footerReport;
                }

                mainReport.PrinterName = _defaultPrinter;

                if (printMode == 1)
                {
                    for (int copy = 1; copy <= _printNo; copy++)
                        mainReport.Print();
                }
                else
                {
                    mainReport.ShowPreviewDialog();
                }

                mainReport.Dispose();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                var dataView = GridControl1.ItemsSource as DataView;
                if (dataView == null || dataView.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FileName = $"{this.Title}_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (dlg.ShowDialog() != true) return;

                string filePath = dlg.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".csv")
                {
                    ExportToCsv(dataView, filePath);
                }
                else
                {
                    // محاولة التصدير عبر GridControl مباشرة إذا كانت النسخة تدعمه
                    try
                    {
                        // GridControl1.View.ExportToXlsx(filePath);
                        // في حال عدم دعم الخاصية → تصدير CSV بامتداد xlsx
                        ExportToCsv(dataView, filePath);
                    }
                    catch
                    {
                        ExportToCsv(dataView, filePath);
                    }
                }

                Process.Start(new ProcessStartInfo(filePath)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private static void ExportToCsv(DataView dv, string filePath)
        {
            using (var writer = new StreamWriter(
                filePath, false, System.Text.Encoding.UTF8))
            {
                // ترويسة الأعمدة
                var columns = new List<string>();
                foreach (DataColumn col in dv.Table.Columns)
                    columns.Add(col.ColumnName);
                writer.WriteLine(string.Join(",", columns));

                // البيانات
                foreach (DataRowView row in dv)
                {
                    var values = new List<string>();
                    foreach (string col in columns)
                    {
                        string cellValue =
                            row[col]?.ToString()?.Replace("\"", "\"\"") ?? "";
                        values.Add($"\"{cellValue}\"");
                    }
                    writer.WriteLine(string.Join(",", values));
                }
            }
        }

        #endregion

        #region ══════════════════ Button Event Handlers ══════════════════

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            // ✅ اقرأ كل قيم UI على UI Thread أولاً
            ShowAccountParams parameters = null;
            Dispatcher.Invoke(() =>
            {
                parameters = CollectUIParameters();
            });

            if (parameters == null) return;

            // ✅ شغّل العملية الثقيلة على Background Thread
            var thread = new Thread(() => ShowAccount(parameters))
            {
                IsBackground = true
            };
            thread.Start();
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ══════════════════ Filter Event Handlers ══════════════════

        private void RdAll_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadAccounts();
        }

        private void RdAllClients_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (LblSupplierName != null)
                LblSupplierName.Text =
                    MainClass.Language == "ar" ? "اسم العميل" : "Client Name";

            LoadAccounts();
        }

        private void RdAllSuppliers_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (LblSupplierName != null)
                LblSupplierName.Text =
                    MainClass.Language == "ar" ? "اسم المورد" : "Supplier Name";

            LoadAccounts();
        }

        private void ChkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            bool isAllBranches = chkAllBranches.IsChecked == true;
            cmbBranches.IsEnabled = !isAllBranches;
            LoadAccounts();
        }

        private void CkTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            bool isTotalPeriod = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isTotalPeriod;
            txtDateTo.IsEnabled = !isTotalPeriod;
        }

        private void CmbBranches_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadAccounts();
        }

        private void CmbClients_MouseDown(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenClientSearchDialog();
        }

        #endregion

        #region ══════════════════ Grid Event Handlers ══════════════════

        private void BtnDetail_CellClick(object sender, RoutedEventArgs e)
        {
            if (GridControl1.CurrentItem is DataRowView rowView)
            {
                string entryNumber = rowView["DgvEntryNo"]?.ToString();
                if (!string.IsNullOrEmpty(entryNumber))
                    EntryOper.ShowEntrySource(entryNumber);
            }
        }

        #endregion

        #region ══════════════════ Client Search Dialog ══════════════════

        private void OpenClientSearchDialog()
        {
            try
            {
                var searchForm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(searchForm);
                MainClass.DoApplyUserSett(searchForm);

                if (rdAllClients.IsChecked == true)
                    searchForm.Type = 1;
                else if (rdAllSuppliers.IsChecked == true)
                    searchForm.Type = 2;
                else
                    searchForm.Type = 1;

                searchForm.PostponeClient = true;
                searchForm.ShowDialog();

                if (!string.IsNullOrEmpty(searchForm.Clientname))
                {
                    LoadAccounts();
                    int accountCode = GetClientCodeById(searchForm.ClientId);
                    if (accountCode > 0)
                        cmbClients.EditValue = accountCode;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ Utility Methods ══════════════════

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private int GetGridRowCount()
        {
            if (GridControl1.ItemsSource is DataView dv)
                return dv.Count;
            if (GridControl1.ItemsSource is DataTable dt)
                return dt.Rows.Count;
            return 0;
        }

        #endregion
    }
}