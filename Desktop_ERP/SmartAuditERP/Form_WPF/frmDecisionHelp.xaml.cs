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
using DevExpress.Xpf.Editors;
using Microsoft.Win32;
using UtilitiesProj;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDecisionHelp : ThemedWindow
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
        private int _selectedItemId;

        #endregion

        #region Constructor

        public frmDecisionHelp()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;
            _selectedItemId = -1;

            Loaded += FrmDecisionHelp_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDecisionHelp_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
            LoadGroups();
            LoadSafes();
            LoadPrintSettings();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnShow.Click += BtnShow_Click;
            btnClose.Click += (s, e) => Close();
            btnPrint.Click += (s, e) => PrintDevexpress(1);
            btnPreview.Click += (s, e) => PrintDevexpress(2);
            btnExport.Click += BtnExport_Click;
            btnSearch.Click += BtnSearch_Click;

            chkAllSafes.Checked += ChkAllSafes_Changed;
            chkAllSafes.Unchecked += ChkAllSafes_Changed;

            chkAllItems.Checked += ChkAllItems_Changed;
            chkAllItems.Unchecked += ChkAllItems_Changed;

            chkAllGroups.Checked += ChkAllGroups_Changed;
            chkAllGroups.Unchecked += ChkAllGroups_Changed;
        }

        #endregion

        #region Data Loading

        public void LoadItems()
        {
            FillComboBox(txtItemName,
                "SELECT id, name FROM Items WHERE IS_Deleted=0 ORDER BY id",
                "name", "id");
        }

        public void LoadSafes()
        {
            string branchFilter = MainClass.BranchNo != -1
                ? $" AND branch={MainClass.BranchNo}"
                : string.Empty;

            FillComboBox(cmbSafes,
                $"SELECT id, name FROM Safes " +
                $"WHERE status=1 AND IS_Deleted=0{branchFilter} ORDER BY id",
                "name", "id");
        }

        public void LoadGroups()
        {
            string query = MainClass.Language == "ar"
                ? "SELECT id, name FROM ItemsCategory WHERE type=2 ORDER BY id"
                : "SELECT id, CASE WHEN LEN(nameEn)=0 THEN name ELSE nameEn END AS name FROM ItemsCategory";

            FillComboBox(cmbGroups, query, "name", "id");
        }

        private void FillComboBox(
            ComboBoxEdit combo,
            string query,
            string display,
            string value)
        {
            try
            {
                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                combo.ItemsSource = dataTable.DefaultView;
                combo.DisplayMember = display;
                combo.ValueMember = value;
                combo.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        #region Calc Stock

        public void CalcStock()
        {
            try
            {
                var resultTable = BuildResultTable();

                // بناء فلتر الأصناف
                string itemFilter = "SELECT id, name, code FROM Items WHERE IS_Deleted=0 ";

                if (chkAllItems.IsChecked != true && txtItemName.EditValue != null)
                    itemFilter += $" AND id={txtItemName.EditValue}";

                if (chkAllGroups.IsChecked != true && cmbGroups.EditValue != null)
                    itemFilter += $" AND Grpcode={cmbGroups.EditValue}";

                itemFilter += " ORDER BY id";

                var itemAdapter = new SqlDataAdapter(itemFilter, _conn);
                var itemsTable = new DataTable();
                itemAdapter.Fill(itemsTable);

                // فلتر المستودع
                string safeFilter = string.Empty;
                if (chkAllSafes.IsChecked != true && cmbSafes.EditValue != null)
                    safeFilter = $" AND inv_sub.store={cmbSafes.EditValue}";

                int totalRows = itemsTable.Rows.Count;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Maximum = totalRows;
                    ProgressBar1.EditValue = 0;
                });

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} AND "
                    : string.Empty;

                for (int i = 0; i < totalRows; i++)
                {
                    DataRow item = itemsTable.Rows[i];
                    object itemId = item["id"];

                    double openingStock = GetSumVal(branchFilter, itemId, safeFilter, 9, null, null);
                    double purchaseIncome = GetSumVal(branchFilter, itemId, safeFilter, 1, 1, null);
                    double purchaseReturn = GetSumVal(branchFilter, itemId, safeFilter, 1, 2, null);
                    double saleOutcome = GetSumVal(branchFilter, itemId, safeFilter, 2, 1, null);
                    double saleReturn = GetSumVal(branchFilter, itemId, safeFilter, 2, 2, null);
                    double rentOutcome = GetSumVal(branchFilter, itemId, safeFilter, 3, 1, null);
                    double rentReturn = GetSumVal(branchFilter, itemId, safeFilter, 3, 2, null);
                    double adjIncome = GetSumVal(branchFilter, itemId, null, 8, 1, null);
                    double adjOutcome = GetSumVal(branchFilter, itemId, null, 8, 2, null);
                    double transferIn = GetSumVal(branchFilter, itemId, null, 7, 1, 1);
                    double transferOut = GetSumVal(branchFilter, itemId, null, 7, 1, 2);

                    bool hasMovement =
                        openingStock != 0 || purchaseIncome != 0 ||
                        saleOutcome != 0 || adjIncome != 0 ||
                        adjOutcome != 0 || transferIn != 0 ||
                        transferOut != 0;

                    if (hasMovement)
                    {
                        double totalIncome = openingStock + purchaseIncome - purchaseReturn
                                             + adjIncome + transferIn;
                        double totalOutcome = saleOutcome - saleReturn + rentOutcome - rentReturn
                                             + adjOutcome + transferOut;
                        double balance = totalIncome - totalOutcome;

                        double slackRate = totalIncome != 0
                            ? Math.Round(balance / totalIncome * 100.0, 1) : 0;
                        double rotationRate = totalIncome != 0
                            ? Math.Round(totalOutcome / totalIncome * 100.0, 1) : 0;

                        resultTable.Rows.Add(
                            resultTable.Rows.Count + 1,
                            item["code"],
                            item["name"],
                            totalIncome,
                            totalOutcome,
                            balance,
                            $"{slackRate} %",
                            $"{rotationRate} %"
                        );
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.EditValue = i + 1;
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    GridControl1.ItemsSource = resultTable.DefaultView;
                    int count = resultTable.Rows.Count;
                    txtRecordCount.Text = $"| {count} سجل";
                    txtFooterCount.Text = count.ToString();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex.Message));
            }
        }

        private double GetSumVal(
            string branchFilter,
            object itemId,
            string safeFilter,
            int invType,
            int? procType,
            int? subProcType)
        {
            try
            {
                string query =
                    $"SELECT SUM(val) FROM inv, inv_sub " +
                    $"WHERE {branchFilter} " +
                    $"inv_sub.ItemId={itemId} " +
                    $"AND inv.inv_type={invType} " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND inv.IS_Deleted=0";

                if (procType.HasValue)
                    query += $" AND inv.proc_type={procType.Value}";

                if (subProcType.HasValue)
                    query += $" AND inv_sub.proc_type={subProcType.Value}";

                if (!string.IsNullOrEmpty(safeFilter))
                    query += safeFilter;

                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dataTable.Rows[0][0].ToString()))
                    return Convert.ToDouble(dataTable.Rows[0][0]);

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        private DataTable BuildResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("DgvNo", typeof(int));
            table.Columns.Add("DgvItemNo", typeof(object));
            table.Columns.Add("DgvItemName", typeof(string));
            table.Columns.Add("DgvIncome", typeof(double));
            table.Columns.Add("DgvOutcome", typeof(double));
            table.Columns.Add("DgvBalance", typeof(double));
            table.Columns.Add("DgvSlackRate", typeof(string));
            table.Columns.Add("DgvRotationRate", typeof(string));
            return table;
        }

        #endregion

        #region Search

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllSafes.IsChecked != true && cmbSafes.EditValue == null)
            {
                ShowWarning("يجب اختيار المستودع");
                cmbSafes.Focus();
                return;
            }

            chkAllItems.IsChecked = false;
            OpenItemSearch();
        }

        private void OpenItemSearch()
        {
            try
            {
                var searchForm = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(searchForm);
                MainClass.DoApplyUserSett(searchForm);
                searchForm.sql = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
                searchForm.search = "SELECT id, name, nameEN, sale_price, unit FROM Items ";
                searchForm.Itemname = string.Empty;
                searchForm.txtSrchNm.Text = txtItemName.Text;
                searchForm.ShowDialog();

                if (searchForm.ISDone && searchForm.ItemId > 0)
                {
                    _selectedItemId = searchForm.ItemId;
                    txtItemName.EditValue = searchForm.ItemId;
                    chkAllItems.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SearchByItemName()
        {
            try
            {
                string name = txtItemName.Text;
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{name}'",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    txtItemName.EditValue = dataTable.Rows[0]["id"];
                else
                    OpenItemSearch();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region CheckBox Events

        private void ChkAllSafes_Changed(object sender, RoutedEventArgs e)
        {
            cmbSafes.IsEnabled = chkAllSafes.IsChecked != true;
        }

        private void ChkAllItems_Changed(object sender, RoutedEventArgs e)
        {
            txtItemName.IsEnabled = chkAllItems.IsChecked != true;
        }

        private void ChkAllGroups_Changed(object sender, RoutedEventArgs e)
        {
            cmbGroups.IsEnabled = chkAllGroups.IsChecked != true;
        }

        #endregion

        #region Button Events

        private void BtnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllItems.IsChecked != true &&
                txtItemName.EditValue == null)
            {
                ShowWarning("يجب اختيار الصنف");
                txtItemName.Focus();
                return;
            }

            if (chkAllSafes.IsChecked != true &&
                cmbSafes.EditValue == null)
            {
                ShowWarning("يجب اختيار المخزن");
                cmbSafes.Focus();
                return;
            }

            GridControl1.ItemsSource = null;
            new Thread(CalcStock) { IsBackground = true }.Start();
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
                FileName = $"مساعدة_القرار_{DateTime.Now:yyyy-MM-dd}",
                Title = "حفظ ملف Excel"
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

        #region Print

        private DataSet BindToReportData()
        {
            var reportList = new List<InventoryData>();

            var dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null) return new DataSet("Name");

            foreach (DataRowView row in dataView)
            {
                reportList.Add(new InventoryData
                {
                    SafeName = cmbSafes.Text,
                    SafeCode = cmbSafes.EditValue?.ToString() ?? string.Empty,
                    MainItemName = txtItemName.Text,
                    MainItemCode = txtItemName.EditValue?.ToString() ?? string.Empty,
                    ItemCode = row["DgvItemNo"]?.ToString(),
                    ItemName = row["DgvItemName"]?.ToString(),
                    Income = row["DgvIncome"]?.ToString(),
                    Outcome = row["DgvOutcome"]?.ToString(),
                    Balance = row["DgvBalance"]?.ToString(),
                    RecessionRate = row["DgvSlackRate"]?.ToString(),
                    TurnoverRate = row["DgvRotationRate"]?.ToString(),
                    Status = " ",
                    InventoryType = Title,
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
            _reportName = "rptDecisionHelp.repx";
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

            var headerReport = XtraReport.FromFile(Path.Combine(_reportUrl, "header.repx"));
            headerReport.DataSource = Common.FoundationInfoDT;
            var headerSub = report.FindControl("headerRpt", true) as XRSubreport;
            if (headerSub != null)
                headerSub.ReportSource = headerReport;

            var footerReport = XtraReport.FromFile(Path.Combine(_reportUrl, "footer.repx"));
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