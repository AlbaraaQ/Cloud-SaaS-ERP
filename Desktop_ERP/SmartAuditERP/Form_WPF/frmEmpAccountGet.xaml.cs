using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEmpAccountGet : ThemedWindow
    {
        #region Fields

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

        #region Constructor

        public frmEmpAccountGet()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;

            Loaded += FrmEmpAccountGet_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmEmpAccountGet_Loaded(object sender, RoutedEventArgs e)
        {
            txtHeaderDate.Text = DateTime.Now.ToLongDateString();

            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime = DateTime.Now;

            LoadAccounts();
            LoadPrintSettings();
            LoadBranches();
            ApplyBranchPermissions();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnShow.Click += BtnShow_Click;
            btnClose.Click += (s, e) => Close();
            btnPrint.Click += (s, e) => PrintDevexpress(1);
            btnPreview.Click += (s, e) => PrintDevexpress(2);
            btnExport.Click += BtnExport_Click;

            chkAllBranches.Checked += ChkAllBranches_Changed;
            chkAllBranches.Unchecked += ChkAllBranches_Changed;

            ckTotalPeriod.Checked += CkTotalPeriod_Changed;
            ckTotalPeriod.Unchecked += CkTotalPeriod_Changed;

            cmbBranches.EditValueChanged += CmbBranches_Changed;
        }

        #endregion

        #region Load Data

        private void LoadAccounts()
        {
            try
            {
                string branchFilter = string.Empty;

                if (chkAllBranches.IsChecked != true &&
                    cmbBranches.EditValue != null)
                {
                    branchFilter = $" AND Branch={cmbBranches.EditValue}";
                }

                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Employees " +
                    $"WHERE AccCode <> -1 AND IS_Deleted=0{branchFilter}",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEmp.ItemsSource = dataTable.DefaultView;
                cmbEmp.DisplayMember = "name";
                cmbEmp.ValueMember = "AccCode";
                cmbEmp.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadBranches()
        {
            string branchFilter = string.Empty;

            if (MainClass.BranchNo != -1 &&
                !string.IsNullOrWhiteSpace(Accounting.BranchCondition))
            {
                branchFilter = $" AND id={MainClass.BranchNo}";
            }

            var adapter = new SqlDataAdapter(
                $"SELECT id, name FROM Branches WHERE IS_Deleted=0{branchFilter}",
                _conn);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbBranches.ItemsSource = dataTable.DefaultView;
            cmbBranches.DisplayMember = "name";
            cmbBranches.ValueMember = "id";
            cmbBranches.EditValue = null;
        }

        private void ApplyBranchPermissions()
        {
            if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition))
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.IsEnabled = true;

                if (cmbBranches.ItemsSource is DataView dv && dv.Count > 0)
                    cmbBranches.EditValue = dv[0]["id"];
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
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

        #region Show Account

        public void ShowAccount()
        {
            try
            {
                var resultTable = BuildResultTable();
                double totalDebit = 0.0;
                double totalCredit = 0.0;

                // التحقق من اختيار الموظف
                bool empSelected =
                    cmbEmp.EditValue != null &&
                    !string.IsNullOrEmpty(cmbEmp.Text);

                if (!empSelected)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show("اختر موظف", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        cmbEmp.Focus();
                    });
                    return;
                }

                string empFilter = $" AND Entry_sub.acc_no={cmbEmp.EditValue}";
                string dateFilter = string.Empty;

                if (ckTotalPeriod.IsChecked != true)
                    dateFilter =
                        " AND Entry.date>=@date1 AND Entry.date<=@date2 ";

                string branchFilter = string.Empty;
                if (chkAllBranches.IsChecked != true &&
                    cmbBranches.EditValue != null)
                {
                    branchFilter =
                        $"Entry.branch={cmbBranches.EditValue} " +
                        $"AND Entry_sub.branch={cmbBranches.EditValue} AND ";
                }

                string query =
                    $@"SELECT Entry.GlobalID, Entry.date,
                              SUM(Entry_sub.dept)   AS dept,
                              SUM(Entry_sub.credit) AS credit,
                              Entry_sub.notes       AS notes,
                              Entry_sub.acc_no      AS Emp
                       FROM Entry
                       INNER JOIN Entry_sub      ON Entry.GlobalID = Entry_sub.EntryGlobalID
                       INNER JOIN Accounts_Index ON Entry_sub.acc_no = Accounts_Index.Code
                       WHERE {branchFilter}
                             Entry.IS_Deleted=0 AND Entry.state=1
                             {empFilter} {dateFilter}
                       GROUP BY Entry.GlobalID, Entry.date,
                                Entry_sub.notes, Entry_sub.acc_no
                       ORDER BY Entry.date";

                var adapter = new SqlDataAdapter(query, _conn);

                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value =
                    Dispatcher.Invoke(() => txtDateFrom.DateTime.Date);

                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    Dispatcher.Invoke(() => txtDateTo.DateTime.AddHours(24));

                var rawData = new DataTable();
                adapter.Fill(rawData);

                int rowCount = rawData.Rows.Count;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Maximum = rowCount;
                    ProgressBar1.EditValue = 0;
                    ClearSummary();
                });

                for (int i = 0; i < rowCount; i++)
                {
                    DataRow row = rawData.Rows[i];

                    double debitAmount = Convert.ToDouble(row["dept"]);
                    double creditAmount = Convert.ToDouble(row["credit"]);
                    string entryNumber = row["GlobalID"].ToString();
                    string entryDate =
                        Convert.ToDateTime(row["date"]).ToShortDateString();
                    string entryNote = row["notes"].ToString();
                    string empName =
                        GetEmployeeName(row["Emp"].ToString()) ?? string.Empty;

                    totalDebit += debitAmount;
                    totalCredit += creditAmount;

                    resultTable.Rows.Add(
                        resultTable.Rows.Count + 1,
                        debitAmount, creditAmount,
                        entryNumber, entryDate,
                        entryNote, empName);

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.EditValue = i + 1;
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    GridControl1.ItemsSource = resultTable.DefaultView;
                    UpdateSummary(totalDebit, totalCredit);
                });
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
            table.Columns.Add("DgvEmp", typeof(string));
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
            GridControl1.ItemsSource = null;
            txtTotDept.Text = string.Empty;
            txtTotCredit.Text = string.Empty;
            txtBalance1.Text = string.Empty;
            txtBalance2.Text = string.Empty;
        }

        #endregion

        #region Helpers

        private string GetEmployeeName(string accountCode)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AName FROM Accounts_Index WHERE Code=N'{accountCode}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0].ToString() : string.Empty;
            }
            catch { return string.Empty; }
        }

        #endregion

        #region Event Handlers

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(ShowAccount) { IsBackground = true }.Start();
        }

        private void ChkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
            LoadAccounts();
        }

        private void CkTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            bool isTotalPeriod = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isTotalPeriod;
            txtDateTo.IsEnabled = !isTotalPeriod;
        }

        private void CmbBranches_Changed(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            LoadAccounts();
        }

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

        #region Print & Export

        private DataSet BindToReportData()
        {
            var reportList = new List<RestrictionData>();

            var dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null) return new DataSet("Name");

            foreach (DataRowView row in dataView)
            {
                reportList.Add(new RestrictionData
                {
                    Clint = cmbEmp.Text,
                    ClintCode = cmbEmp.EditValue?.ToString() ?? string.Empty,
                    FromDate = txtDateFrom.DateTime.ToShortDateString(),
                    ToDate = txtDateTo.DateTime.ToShortDateString(),
                    Dept = row["DgvDept"]?.ToString(),
                    Credit = row["DgvCredit"]?.ToString(),
                    RestrNo = row["DgvEntryNo"]?.ToString(),
                    RestDate = row["DgvDate"]?.ToString(),
                    Note = row["DgvNote"]?.ToString(),
                    ClintName = row["DgvEmp"]?.ToString(),
                    Status = " ",
                    RestrType = Title,
                    SumDept = txtTotDept.Text,
                    TotCredit = txtTotCredit.Text,
                    DeptBalance = txtBalance1.Text,
                    CreditBalance = txtBalance2.Text,
                    InventryType = Title,
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
            _reportUrl = MainClass.ReportsPath;
            _reportName = "rptCustAccountGet.repx";
            _defaultPrinter = MainClass.ReportsPrinter;

            var dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null || dataView.Count == 0)
            {
                ShowWarning("لا توجد عمليات بالجدول");
                return;
            }

            if (string.IsNullOrWhiteSpace(_reportUrl))
            {
                ShowWarning("يجب تحديد مسار التقرير");
                return;
            }

            string fullPath = Path.Combine(_reportUrl, _reportName);
            if (!Directory.Exists(_reportUrl) || !File.Exists(fullPath))
            {
                ShowError("المسار الحالي للتقارير غير موجود أو تم تعديله");
                return;
            }

            var report = XtraReport.FromFile(fullPath);
            report.DataSource = BindToReportData();

            var headerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "header.repx"));
            headerReport.DataSource = Common.FoundationInfoDT;
            var headerSub = report.FindControl("headerRpt", true) as XRSubreport;
            if (headerSub != null)
                headerSub.ReportSource = headerReport;

            var footerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "footer.repx"));
            footerReport.DataSource = Common.FoundationInfoDT;
            var footerSub = report.FindControl("footerRpt", true) as XRSubreport;
            if (footerSub != null)
                footerSub.ReportSource = footerReport;

            if (string.IsNullOrEmpty(_defaultPrinter))
            {
                ShowWarning("يجب تحديد الطابعة من الإعدادات");
                return;
            }

            report.PrinterName = _defaultPrinter;

            if (printMode == 1)
            {
                for (int i = 1; i <= _printNo; i++)
                    report.Print();
            }
            else
            {
                report.ShowPreviewDialog();
            }

            report.Dispose();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null || dataView.Count == 0)
            {
                ShowWarning("لا توجد بيانات للتصدير");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"كشف_حساب_موظف_{DateTime.Now:yyyy-MM-dd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    GridView1.ExportToXlsx(saveDialog.FileName);
                    DXMessageBox.Show(
                        $"✅ تم التصدير بنجاح:\n{saveDialog.FileName}",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(saveDialog.FileName)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ShowError($"خطأ في التصدير:\n{ex.Message}");
                }
            }
        }

        #endregion

        #region Utilities

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarning(string message)
        {
            DXMessageBox.Show(message, "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion
    }
}