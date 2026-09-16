using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCustLastPay : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        public int invtype;
        private int _clientType;
        private bool _printHeader;
        private bool _printFooter;
        private bool _printStamp;
        private int _printType;
        private int _printNo;
        private string _defaultPrinter;
        private string _reportName;
        private string _reportUrl;

        /// <summary>
        /// جدول نتائج العرض - يُستخدم كمصدر بيانات للـ Grid
        /// </summary>
        private DataTable _resultTable;

        #endregion

        #region Constructor

        public frmCustLastPay()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            invtype = 1;
            _clientType = 1;
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;

            Loaded += FrmCustLastPay_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmCustLastPay_Loaded(object sender, RoutedEventArgs e)
        {
            txtHeaderDate.Text = DateTime.Now.ToLongDateString();

            // تحديد النوع حسب اللغة
            if (MainClass.Language == "ar")
            {
                LblAccName.Text = "المورد";
                Title = "💰 سداد الموردين";
                txtHeaderTitle.Text = "حركة آخر سداد للموردين";
            }
            else
            {
                LblAccName.Text = "Supplier";
                Title = "💰 Pay Supplier";
                txtHeaderTitle.Text = "Last Payment - Suppliers";
            }

            InitializeResultTable();
            LoadClients();
            LoadPrintSettings();

            // Wire up events
            btnShow.Click += BtnShow_Click;
            btnPreview.Click += BtnPreview_Click;
            btnPrint.Click += BtnPrint_Click;
            btnExport.Click += BtnExport_Click;
            btnClose.Click += BtnClose_Click;

            chkAll.Checked += ChkAll_Changed;
            chkAll.Unchecked += ChkAll_Changed;
        }

        #endregion

        #region Data Table Initialization

        /// <summary>
        /// تهيئة جدول النتائج بالأعمدة المطلوبة
        /// </summary>
        private void InitializeResultTable()
        {
            _resultTable = new DataTable();
            _resultTable.Columns.Add("AccountCode", typeof(int));
            _resultTable.Columns.Add("ClientName", typeof(string));
            _resultTable.Columns.Add("Phone", typeof(string));
            _resultTable.Columns.Add("Mobile", typeof(string));
            _resultTable.Columns.Add("LastPayAmount", typeof(double));
            _resultTable.Columns.Add("LastPayDate", typeof(string));
            _resultTable.Columns.Add("EntryType", typeof(string));
            _resultTable.Columns.Add("Balance", typeof(double));
            _resultTable.Columns.Add("Status", typeof(string));
            _resultTable.Columns.Add("EntryNo", typeof(string));
        }

        #endregion

        #region Data Loading

        public void LoadClients()
        {
            if (invtype == 1)
                _clientType = 2;
            else if (invtype == 2)
                _clientType = 1;

            string query =
                $@"SELECT id, name FROM Customers 
                   WHERE IS_Deleted=0 AND (type={_clientType} OR type=3) 
                   AND AccountCode<>-1 ORDER BY id";

            var adapter = new SqlDataAdapter(query, _conn);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbClients.ItemsSource = dataTable.DefaultView;
            cmbClients.DisplayMember = "name";
            cmbClients.ValueMember = "id";
            cmbClients.EditValue = null;
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count != 1)
                    return;

                DataRow row = dataTable.Rows[0];
                _printType = Convert.ToInt32(row["printType"]);
                _printFooter = Convert.ToBoolean(row["PrintFooter"]);
                _printHeader = Convert.ToBoolean(row["PrintHeader"]);
                _printStamp = Convert.ToBoolean(row["PrintStamp"]);
                _defaultPrinter = row["CasherPrinter"].ToString();
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

        #region Show Result Logic

        private void ShowResult()
        {
            try
            {
                // بناء استعلام العملاء
                string clientQuery =
                    $@"SELECT id, name, tel, mobile, AccountCode 
                       FROM Customers 
                       WHERE (type={_clientType} OR type=3) 
                       AND AccountCode<>-1 AND IS_Deleted=0";

                if (chkAll.IsChecked != true && cmbClients.EditValue != null)
                {
                    clientQuery += $" AND id={cmbClients.EditValue}";
                }

                clientQuery += " ORDER BY id";

                var clientAdapter = new SqlDataAdapter(clientQuery, _conn);
                var clientsTable = new DataTable();
                clientAdapter.Fill(clientsTable);

                // تهيئة الجدول والتقدم
                _resultTable.Rows.Clear();
                ProgressBar1.Maximum = clientsTable.Rows.Count;
                ProgressBar1.EditValue = 0;

                string branchFilter = BuildBranchFilter();

                for (int i = 0; i < clientsTable.Rows.Count; i++)
                {
                    DataRow clientRow = clientsTable.Rows[i];
                    int accountCode = Convert.ToInt32(clientRow["AccountCode"]);
                    string accFilter = $" AND Entry_sub.acc_no={accountCode}";

                    // جلب آخر قيد
                    string lastEntryQuery =
                        $@"SELECT TOP 1 Entry.GlobalID, Entry.type, Entry.date,
                                  Entry_sub.dept, Entry_sub.credit
                           FROM Entry
                           INNER JOIN Entry_sub ON Entry.GlobalID = Entry_sub.EntryGlobalID
                           WHERE {branchFilter} state=1 AND IS_Deleted=0
                                 {accFilter}
                           ORDER BY id DESC";

                    var lastEntryAdapter = new SqlDataAdapter(lastEntryQuery, _conn);
                    var lastEntryTable = new DataTable();
                    lastEntryAdapter.Fill(lastEntryTable);

                    if (lastEntryTable.Rows.Count > 0)
                    {
                        DataRow entryRow = lastEntryTable.Rows[0];

                        string entryTypeName = GetEntryTypeName(
                            Convert.ToInt32(Convert.ToDouble(entryRow["type"].ToString())));

                        double lastPayAmount = Convert.ToDouble(entryRow["dept"]) == 0
                            ? Convert.ToDouble(entryRow["credit"])
                            : Convert.ToDouble(entryRow["dept"]);

                        string lastPayDate = Convert.ToDateTime(entryRow["date"]).ToShortDateString();
                        string entryGlobalId = entryRow["GlobalID"].ToString();

                        // جلب مجموع المدين والدائن
                        string sumQuery =
                            $@"SELECT SUM(Entry_sub.dept) AS dept, 
                                      SUM(Entry_sub.credit) AS credit
                               FROM Entry
                               INNER JOIN Entry_sub ON Entry.GlobalID = Entry_sub.EntryGlobalID
                               WHERE {branchFilter} Entry.IS_Deleted=0 
                                     AND Entry.state=1 {accFilter}";

                        var sumAdapter = new SqlDataAdapter(sumQuery, _conn);
                        var sumTable = new DataTable();
                        sumAdapter.Fill(sumTable);

                        if (sumTable.Rows.Count > 0 &&
                            !string.IsNullOrEmpty(sumTable.Rows[0][0].ToString()))
                        {
                            double totalDebit = Convert.ToDouble(sumTable.Rows[0]["dept"]);
                            double totalCredit = Convert.ToDouble(sumTable.Rows[0]["credit"]);

                            double balanceValue;
                            string statusText;

                            if (totalDebit >= totalCredit)
                            {
                                balanceValue = Math.Round(totalDebit - totalCredit, 3);
                                statusText = "مدين";
                            }
                            else
                            {
                                balanceValue = Math.Round(totalCredit - totalDebit, 3);
                                statusText = "دائن";
                            }

                            _resultTable.Rows.Add(
                                accountCode,
                                clientRow["name"].ToString(),
                                clientRow["tel"].ToString(),
                                clientRow["mobile"].ToString(),
                                lastPayAmount,
                                lastPayDate,
                                entryTypeName,
                                balanceValue,
                                statusText,
                                entryGlobalId
                            );
                        }
                    }

                    ProgressBar1.EditValue = i + 1;
                }

                // تحديث مصدر البيانات
                dgvItems.ItemsSource = _resultTable.DefaultView;
                txtRecordCount.Text = _resultTable.Rows.Count.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Entry Type Helper

        /// <summary>
        /// يُعيد اسم نوع القيد بناءً على رقم النوع
        /// </summary>
        private string GetEntryTypeName(int entryType)
        {
            return entryType switch
            {
                0 => "قيد افتتاحي",
                1 => "فاتورة شراء",
                2 => "فاتورة بيع",
                3 => "فاتورة تأجير",
                4 => "تسوية جردية",
                5 => "قبض خارجي",
                6 => "صرف خارجي",
                7 => "قبض داخلي",
                8 => "صرف داخلي",
                12 => "إضافات",
                13 => "إغلاق الوردية",
                21 => "مرتد فاتورة شراء",
                22 => "مرتد فاتورة بيع",
                _ => "غير محدد"
            };
        }

        #endregion

        #region Query Helpers

        /// <summary>
        /// يبني فلتر الفرع إذا كان المستخدم محدد بفرع
        /// </summary>
        private string BuildBranchFilter()
        {
            if (MainClass.BranchNo != -1)
            {
                return $"Entry.branch={MainClass.BranchNo} " +
                       $"AND Entry_sub.branch={MainClass.BranchNo} AND ";
            }
            return string.Empty;
        }

        #endregion

        #region Button Event Handlers

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            // التحقق من اختيار العميل
            if (chkAll.IsChecked != true && cmbClients.EditValue == null)
            {
                string message = _clientType == 1
                    ? "يجب اختيار العميل"
                    : "يجب اختيار المورد";

                DXMessageBox.Show(message, "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbClients.Focus();
                return;
            }

            // مسح البيانات السابقة
            _resultTable.Rows.Clear();
            dgvItems.ItemsSource = null;
            Label3.Text = "0";
            Label6.Text = "0";
            txtRecordCount.Text = "0";

            // عرض النتائج
            ShowResult();

            // حساب المجاميع
            CalculateTotals();
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

        #region CheckBox Event Handlers

        private void ChkAll_Changed(object sender, RoutedEventArgs e)
        {
            bool isAllChecked = chkAll.IsChecked == true;
            cmbClients.IsEnabled = !isAllChecked;
        }

        #endregion

        #region Grid Event Handlers

        private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvItems.CurrentItem is DataRowView rowView)
                {
                    string entryNumber = rowView["EntryNo"]?.ToString();
                    if (!string.IsNullOrEmpty(entryNumber))
                        EntryOper.ShowEntrySource(entryNumber);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Calculations

        /// <summary>
        /// حساب مجموع الإجمالي والرصيد
        /// </summary>
        private void CalculateTotals()
        {
            double totalPayments = 0.0;
            double totalBalance = 0.0;

            foreach (DataRow row in _resultTable.Rows)
            {
                totalPayments += Convert.ToDouble(row["LastPayAmount"]);
                totalBalance += Convert.ToDouble(row["Balance"]);
            }

            Label6.Text = totalPayments.ToString("N2");
            Label3.Text = totalBalance.ToString("N2");
        }

        #endregion

        #region Print & Export

        /// <summary>
        /// ربط بيانات التقرير
        /// </summary>
        private DataSet BindToReportData()
        {
            var reportList = new List<RestrictionData>();

            foreach (DataRow row in _resultTable.Rows)
            {
                var item = new RestrictionData
                {
                    Clint = cmbClients.Text,
                    ClintCode = cmbClients.EditValue?.ToString() ?? string.Empty,
                    AccountCode = row["AccountCode"]?.ToString(),
                    ClintName = row["ClientName"]?.ToString(),
                    ClintPhoneNo = row["Phone"]?.ToString(),
                    ClintMobileNo = row["Mobile"]?.ToString(),
                    LastPayment = row["LastPayAmount"]?.ToString(),
                    InvDate = row["LastPayDate"]?.ToString(),
                    InvoiceType = row["EntryType"]?.ToString(),
                    Balance = row["Balance"]?.ToString(),
                    Status = row["Status"]?.ToString(),
                    RestrType = Title,
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                };

                reportList.Add(item);
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(reportList));
            return dataSet;
        }

        private void PrintDevexpress(int printMode)
        {
            _reportUrl = MainClass.ReportsPath;
            _reportName = "rptCustLastPay.repx";
            _defaultPrinter = MainClass.ReportsPrinter;

            if (_resultTable.Rows.Count == 0)
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

            string fullReportPath = Path.Combine(_reportUrl, _reportName);

            if (!Directory.Exists(_reportUrl) || !File.Exists(fullReportPath))
            {
                DXMessageBox.Show(
                    "المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(_reportName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mainReport = XtraReport.FromFile(fullReportPath);
            mainReport.DataSource = BindToReportData();

            // Header subreport
            var headerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "header.repx"));
            headerReport.DataSource = Common.FoundationInfoDT;

            var headerSub = mainReport.FindControl(
                "headerRpt", ignoreCase: true) as XRSubreport;
            if (headerSub != null)
                headerSub.ReportSource = headerReport;

            // Footer subreport
            var footerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "footer.repx"));
            footerReport.DataSource = Common.FoundationInfoDT;

            var footerSub = mainReport.FindControl(
                "footerRpt", ignoreCase: true) as XRSubreport;
            if (footerSub != null)
                footerSub.ReportSource = footerReport;

            if (string.IsNullOrEmpty(_defaultPrinter))
            {
                DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
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

        /// <summary>
        /// تصدير البيانات إلى ملف Excel عبر DevExpress
        /// </summary>
        private void ExportToExcel()
        {
            if (_resultTable.Rows.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|Excel Files (*.xls)|*.xls",
                DefaultExt = ".xlsx",
                FileName = $"حركة آخر سداد - {DateTime.Now:yyyy-MM-dd}",
                Title = "حفظ ملف Excel",
                OverwritePrompt = true
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = saveDialog.FileName;
                    GridView1.ExportToXlsx(filePath);

                    DXMessageBox.Show(
                        $"✅ تم حفظ الملف بنجاح في:\n{filePath}",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    // فتح الملف
                    Process.Start(new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show(
                        $"خطأ أثناء التصدير:\n{ex.Message}",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Utility Methods

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}