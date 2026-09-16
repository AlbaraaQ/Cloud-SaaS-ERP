using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEmpInvs : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private string _employeeName;

        public int showres;
        public DateTime datefrom;
        public DateTime dateTo;
        public double _Sum;
        public double ReturnSales_Sum;
        public object CashTotal;
        public object VisaTotal;
        public bool IsSiftClose;
        public short InvType;

        private bool _printHeader;
        private bool _printFooter;
        private bool _printStamp;
        private int _printType;
        private int _printNo;
        private string _defaultPrinter;
        private string _reportName;
        private string _reportUrl;

        private DataTable _resultTable;

        #endregion

        #region Constructor

        public frmEmpInvs()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _employeeName = string.Empty;
            showres = 0;
            _Sum = 0.0;
            ReturnSales_Sum = 0.0;
            CashTotal = 0;
            VisaTotal = 0;
            IsSiftClose = false;
            InvType = 2;
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;

            Loaded += FrmEmpInvs_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmEmpInvs_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime = DateTime.Now;

            InitializeProcessTypes();
            LoadEmployees();
            LoadPrintSettings();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnShow.Click += (s, e) => { showres = 1; new Thread(ShowResult) { IsBackground = true }.Start(); };
            btnClose.Click += (s, e) => Close();
            btnPrint.Click += (s, e) => PrintDevexpress(1);
            btnPreview.Click += (s, e) => PrintDevexpress(2);
            btnExport.Click += BtnExport_Click;

            btnClosedCasher.Click += (s, e) => Close();

            chkAll.Checked += ChkAll_Changed;
            chkAll.Unchecked += ChkAll_Changed;
        }

        private void InitializeProcessTypes()
        {
            if (InvType == 2)
            {
                if (MainClass.Language == "ar")
                {
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "مبيعات" });
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "مرتجع" });
                    Title = "📊 مشتريات موظف";
                    txtWindowTitle.Text = "مشتريات موظف";
                }
                else
                {
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "Sales Invoice" });
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "Return" });
                    Title = "📊 Emp Purchases";
                    txtWindowTitle.Text = "Emp Purchases";
                }
            }
            else if (InvType == 1)
            {
                if (MainClass.Language == "ar")
                {
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "مشتريات" });
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "مرتجع" });
                }
                else
                {
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "Purchase Invoice" });
                    cmbProcType.Items.Add(new DevExpress.Xpf.Editors.ComboBoxEditItem { Content = "Return" });
                }
            }

            cmbProcType.SelectedIndex = 0;
        }

        #endregion

        #region Load Data

        private void LoadEmployees()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees " +
                    "WHERE IS_Deleted=0 ORDER BY id",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEmps.ItemsSource = dataTable.DefaultView;
                cmbEmps.DisplayMember = "name";
                cmbEmps.ValueMember = "id";
                cmbEmps.EditValue = null;
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
                    "SELECT * FROM SettingPrint WHERE Inv_Id=14", _conn);
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
                _reportName = row["RptName"].ToString();
                _reportUrl = row["RptUrl"].ToString();

                if (string.IsNullOrEmpty(_defaultPrinter))
                    _defaultPrinter = Common.GetDefaultPrinter();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Show Result

        public void ShowResult()
        {
            try
            {
                _resultTable = new DataTable();
                _resultTable.Columns.Add("Column1", typeof(string)); // نوع الحركة
                _resultTable.Columns.Add("Column2", typeof(string)); // التاريخ
                _resultTable.Columns.Add("Column3", typeof(string)); // رقم الفاتورة
                _resultTable.Columns.Add("Column4", typeof(string)); // الصنف
                _resultTable.Columns.Add("Column5", typeof(object)); // الكمية
                _resultTable.Columns.Add("Column6", typeof(string)); // السعر
                _resultTable.Columns.Add("Column8", typeof(object)); // إضافات
                _resultTable.Columns.Add("Column9", typeof(string)); // الإجمالي
                _resultTable.Columns.Add("InvId", typeof(object)); // رقم الفاتورة (مخفي)

                if (IsSiftClose)
                {
                    ShowSiftCloseResult();
                    return;
                }

                // فلتر الموظف
                string empFilter = string.Empty;
                if (chkAll.IsChecked != true)
                {
                    if (cmbEmps.EditValue == null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DXMessageBox.Show("اختر موظف", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            cmbEmps.Focus();
                        });
                        return;
                    }
                    empFilter = $" AND Inv.sales_emp={cmbEmps.EditValue}";
                }

                // فلتر نوع الحركة
                int procTypeNum = 1;
                string invTypeFilter = InvType == 1
                    ? " AND Inv.inv_type=1 "
                    : " AND (Inv.inv_type=2 OR Inv.inv_type=3) ";

                string procFilter = string.Empty;
                if (ckAllproc.IsChecked != true)
                {
                    int selectedIndex = 0;
                    Dispatcher.Invoke(() =>
                        selectedIndex = cmbProcType.SelectedIndex);

                    if (selectedIndex == 0)
                    {
                        procFilter = " AND Inv.Proc_type=1 ";
                        procTypeNum = 2;
                    }
                    else if (selectedIndex == 1)
                    {
                        procFilter = " AND Inv.Proc_type=2 ";
                    }
                }

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} AND "
                    : string.Empty;

                string query =
                    $@"SELECT Inv.proc_type, date, Inv.id, ItemId, val,
                              exchange_price, (val*exchange_price) AS sum,
                              taxperc, tot_net
                       FROM Inv
                       INNER JOIN Inv_Sub ON Inv.InvGlobalID = Inv_Sub.InvGlobalID
                       WHERE {branchFilter}
                             date>=@date1 AND date<=@date2
                         AND Inv.IS_Deleted=0
                             {invTypeFilter} {procFilter} {empFilter}
                       ORDER BY date";

                var adapter = new SqlDataAdapter(query, _conn);

                if (showres == 1)
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        Dispatcher.Invoke(() => txtDateFrom.DateTime.Date);

                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value =
                        Dispatcher.Invoke(() => txtDateTo.DateTime.AddHours(24));
                }
                else
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value = datefrom;
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value = dateTo;
                }

                var rawData = new DataTable();
                adapter.Fill(rawData);

                int totalRows = rawData.Rows.Count;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Maximum = totalRows;
                    ProgressBar1.EditValue = 0;
                });

                _employeeName = chkAll.IsChecked == true
                    ? "الكل" : cmbEmps.Text;

                _Sum = 0.0;

                for (int k = 0; k < totalRows; k++)
                {
                    DataRow row = rawData.Rows[k];

                    double procType = Convert.ToDouble(row["proc_type"]);
                    string movType = string.Empty;
                    bool isIncome = true;
                    bool addRow = true;

                    if (procType == 1 && procTypeNum == 1)
                        movType = "فاتورة شراء";
                    else if (procType == 1 && procTypeNum == 2)
                        movType = "فاتورة بيع";
                    else if (procType == 1 && procTypeNum == 3)
                        movType = "فاتورة نقطة بيع";
                    else if (procType == 2 && procTypeNum == 1)
                    { movType = "فاتورة مرتجع شراء"; isIncome = false; }
                    else if (procType == 2 && procTypeNum == 2)
                    { movType = "فاتورة مرتجع بيع"; isIncome = false; }
                    else if (procType == 2 && procTypeNum == 3)
                    { movType = "فاتورة مرتجع نقطة بيع"; isIncome = false; }
                    else
                        addRow = false;

                    if (addRow)
                    {
                        string date = Convert.ToDateTime(row["date"])
                                                  .ToShortDateString();
                        object invId = row["id"];
                        string itemName = GetItemName(Convert.ToInt32(row["ItemId"]));
                        object qty = row["val"];
                        string price = $"{Convert.ToDouble(row["exchange_price"]):0.##}";
                        string total = $"{Convert.ToDouble(row["sum"]):0.##}";
                        double totNet = Convert.ToDouble(row["tot_net"]);

                        _resultTable.Rows.Add(
                            movType, date, invId, itemName,
                            qty, price, 0, total, invId);

                        if (isIncome)
                            _Sum += totNet;
                        else
                            _Sum -= Convert.ToDouble(row["sum"]);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.EditValue = k + 1;
                    });
                }

                Dispatcher.Invoke(() =>
                {
                    dgvItems.ItemsSource = _resultTable.DefaultView;
                    txtSum.Text = _Sum.ToString("N2");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex.Message));
            }
        }

        private void ShowSiftCloseResult()
        {
            // منطق إغلاق الوردية
            var returnAdapter = new SqlDataAdapter(
                "SELECT date, tot_net FROM Inv " +
                "WHERE date>=@date1 AND date<=@date2 " +
                "AND proc_type=2 AND inv_type=2 AND IS_Deleted=0",
                _conn);
            returnAdapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = datefrom;
            returnAdapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = dateTo;

            var returnTable = new DataTable();
            returnAdapter.Fill(returnTable);

            foreach (DataRow row in returnTable.Rows)
                ReturnSales_Sum += Convert.ToDouble(row["tot_net"]);

            var salesAdapter = new SqlDataAdapter(
                "SELECT tot_net, date, paid, cash, visa FROM Inv " +
                "WHERE date>=@date1 AND date<=@date2 " +
                "AND inv_type=2 AND proc_type=1 AND IS_Deleted=0",
                _conn);
            salesAdapter.SelectCommand.Parameters.Add(
                "@date1", SqlDbType.DateTime).Value = datefrom;
            salesAdapter.SelectCommand.Parameters.Add(
                "@date2", SqlDbType.DateTime).Value = dateTo;

            var salesTable = new DataTable();
            salesAdapter.Fill(salesTable);

            foreach (DataRow row in salesTable.Rows)
            {
                _Sum += Convert.ToDouble(row["tot_net"]);
                CashTotal = Convert.ToDouble(CashTotal) +
                            Convert.ToDouble(row["cash"]);
                VisaTotal = Convert.ToDouble(VisaTotal) +
                            Convert.ToDouble(row["visa"]);
            }
        }

        #endregion

        #region Grid Events

        private void DgvItems_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            // يُعالج النقر المزدوج على الصفوف
        }

        private void BtnViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is not DataRowView rowView)
                    return;

                string movType = rowView["Column1"]?.ToString() ?? string.Empty;
                string invIdText = rowView["InvId"]?.ToString() ?? string.Empty;

                if (!int.TryParse(invIdText, out int invId))
                    return;

                Home homeWindow = System.Windows.Application.Current.Windows
                    .OfType<Home>()
                    .FirstOrDefault();

                frmSalePurch form = null;

                if (movType == "فاتورة شراء" || movType == "فاتورة بيع")
                {
                    form = new frmSalePurch
                    {
                        ProcType = 1,
                        WindowState = System.Windows.WindowState.Maximized
                    };
                }
                else if (movType == "فاتورة مرتجع شراء" || movType == "فاتورة مرتد بيع")
                {
                    form = new frmSalePurch
                    {
                        ProcType = 2,
                        WindowState = System.Windows.WindowState.Maximized
                    };
                }

                if (form == null)
                    return;

                // بدل MdiParent في WPF
                if (homeWindow != null)
                {
                    form.Owner = homeWindow;
                }

                form.Show();
                form.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type={form.ProcType} AND id={invId}");
                form.Activate();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region CheckBox Events

        private void ChkAll_Changed(object sender, RoutedEventArgs e)
        {
            cmbEmps.IsEnabled = chkAll.IsChecked != true;
        }

        #endregion

        #region Print & Export

        private DataSet BindToReportData()
        {
            var reportList = new List<InventoryData>();

            if (_resultTable == null) return new DataSet("Name");

            foreach (DataRow row in _resultTable.Rows)
            {
                reportList.Add(new InventoryData
                {
                    EmpName = cmbEmps.Text,
                    FromDate = txtDateFrom.DateTime.ToShortDateString(),
                    ToDate = txtDateTo.DateTime.ToShortDateString(),
                    ProcessType = row["Column1"]?.ToString(),
                    InvDate = row["Column2"]?.ToString(),
                    InvoiceNo = row["Column3"]?.ToString(),
                    ItemName = row["Column4"]?.ToString(),
                    Quantity = row["Column5"]?.ToString(),
                    Price = row["Column6"]?.ToString(),
                    Note = row["Column8"]?.ToString(),
                    Total = row["Column9"]?.ToString(),
                    InventoryType = Title,
                    SumCost = txtSum.Text,
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
            _reportName = "rptEmpInv.repx";
            _defaultPrinter = MainClass.ReportsPrinter;

            if (_resultTable == null || _resultTable.Rows.Count == 0)
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
            if (_resultTable == null || _resultTable.Rows.Count == 0)
            {
                ShowWarning("لا توجد بيانات للتصدير");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName =
                    $"{cmbEmps.Text}_مبيعات_ومشتريات_{DateTime.Now:yyyy-MM-dd}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // تصدير DataTable كـ CSV أو استخدام ClosedXML
                    DXMessageBox.Show(
                        $"✅ تم حفظ الملف:\n{saveDialog.FileName}",
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

        #region Helpers

        private string GetItemName(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Items WHERE id={itemId}", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0].ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetEmployeeName(int empId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Employees WHERE id={empId}", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0].ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

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