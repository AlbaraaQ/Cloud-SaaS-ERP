using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvoiceDeliveries : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int Invtype;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;

        private int _procType;
        private int _invType;
        private bool _printHeader;
        private bool _printFooter;
        private bool _printStamp;
        private int _printType;
        private int _printNo;
        private string _defPrinter;
        private string _rptName;
        private string _rptUrl;
        private bool _priceIncVAT;
        private double _defVAT;

        // Summary accumulators
        private double _total, _total1;
        private double _discount, _discount1;
        private double _cost, _cost1;
        private double _profit, _profit1;
        private double _sum, _sum1;

        private List<InvoiceDGV> _invoicesList;
        private ObservableCollection<InvoiceDisplayRow> _displayRows;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvoiceDeliveries()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            Invtype = 0;
            _procType = 1;
            _invType = 0;
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _rptName = string.Empty;
            _rptUrl = string.Empty;
            _invoicesList = new List<InvoiceDGV>();
            _displayRows = new ObservableCollection<InvoiceDisplayRow>();

            GridControl1.ItemsSource = _displayRows;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;

            LoadComboItems();
            LoadEmployees();
            LoadStores();
            LoadCustomers();
            LoadBranches();
            LoadPrintSettings();

            // تحديد "غير مستلمة" افتراضياً
            rbNotDelivered.IsChecked = true;

            BindInvoices();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Footer Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            BindInvoices();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_displayRows.Count == 0)
                {
                    DXMessageBox.Show(
                        "لا توجد بيانات للتصدير",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                string outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"InvoiceDeliveries_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");

                // تصدير DataGrid إلى Excel عبر DataTable
                ExportToExcel(outputPath);
                Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في التصدير:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = DXMessageBox.Show(
                    "هل أنت متأكد من مزامنة الفواتير المختارة؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.No)
                    return;

                var selectedInvoices = _displayRows
                    .Where(r => r.IsSelected)
                    .ToList();

                if (selectedInvoices.Count == 0)
                {
                    DXMessageBox.Show(
                        "لم يتم تحديد أي فواتير",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                ProgressBar1.Visibility = Visibility.Visible;
                ProgressBar1.Maximum = selectedInvoices.Count;
                ProgressBar1.Value = 0;

                var invoiceOper = new InvoiceOper();
                var invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);
                var invoiceToSync = new List<Invoice>();

                foreach (var row in selectedInvoices)
                {
                    var invoice = invoiceOper.BindInvoByID(row.InvGlobalID);
                    if (invoice != null)
                    {
                        invoice.Received = false;
                        invoice.DistBranch = Sync.DistBranch;
                        invoiceToSync.Add(invoice);
                    }
                }

                if (Sync.ValidAPIUrl)
                {
                    invoiceToSync = (List<Invoice>)await invoiceCRUD.PostInvoices(invoiceToSync);
                }

                if (invoiceToSync.Count > 0)
                {
                    using var conn = MainClass.ConnObj();
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    foreach (var invoice in invoiceToSync)
                    {
                        if (invoice == null) continue;

                        new SqlCommand(
                            $"UPDATE Inv SET Sync = 1 WHERE InvGlobalID = N'{invoice.InvGlobalID}'",
                            conn).ExecuteNonQuery();

                        ProgressBar1.Value++;
                    }
                }

                ProgressBar1.Visibility = Visibility.Collapsed;

                DXMessageBox.Show(
                    "تمت المزامنة بنجاح ✅",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ProgressBar1.Visibility = Visibility.Collapsed;
                DXMessageBox.Show(
                    $"خطأ في المزامنة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Grid Button Events

        private void BtnDeliveryRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            if (btn.Tag is not InvoiceDisplayRow displayRow)
                return;

            var invoice = _invoicesList.FirstOrDefault(i =>
                string.Equals(i.InvGlobalID, displayRow.InvGlobalID, StringComparison.Ordinal));

            if (invoice == null)
                return;

            var deliveryForm = new frmInvItemsDeliveries
            {
                Inv = invoice,
                Owner = this
            };

            deliveryForm.ShowDialog();

            if (deliveryForm.ISDone)
            {
                BindInvoices();
            }
        }

        private void BtnDeliveryAllRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            if (btn.Tag is not InvoiceDisplayRow displayRow)
                return;

            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من التسليم الكلي للفاتورة المختارة؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            if (string.IsNullOrWhiteSpace(displayRow.InvGlobalID))
                return;

            var invoice = _invoicesList.FirstOrDefault(i =>
                string.Equals(i.InvGlobalID, displayRow.InvGlobalID, StringComparison.Ordinal));

            if (invoice == null)
                return;

            DeliverAllItemsForInvoice(invoice);
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Filter Events

        private void CheckBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbBranches == null) return;
            cmbBranches.IsEnabled = !(CheckBox1.IsChecked ?? true);
            if (!cmbBranches.IsEnabled)
                cmbBranches.SelectedIndex = -1;
        }

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbInvType == null) return;
            cmbInvType.IsEnabled = !(ckAllInvs.IsChecked ?? true);
            if (!cmbInvType.IsEnabled)
                cmbInvType.SelectedIndex = -1;
        }

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbStore == null) return;
            cmbStore.IsEnabled = !(ckAllStore.IsChecked ?? true);
            if (!cmbStore.IsEnabled)
                cmbStore.SelectedIndex = -1;
        }

        private void ckAllType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbType == null) return;
            cmbType.IsEnabled = !(ckAllType.IsChecked ?? true);
            if (!cmbType.IsEnabled)
                cmbType.SelectedIndex = -1;
        }

        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbusers == null) return;
            cmbusers.IsEnabled = !(ckAllUsers.IsChecked ?? true);
            if (!cmbusers.IsEnabled)
                cmbusers.SelectedIndex = -1;
        }

        private void ckAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbClients == null) return;
            cmbClients.IsEnabled = !(ckAllClients.IsChecked ?? true);
            if (!cmbClients.IsEnabled)
                cmbClients.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (txtFromDate == null || txtToDate == null) return;

            bool isAllPeriod = ckTotalPeriod.IsChecked ?? true;
            txtFromDate.IsEnabled = !isAllPeriod;
            txtToDate.IsEnabled = !isAllPeriod;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        private void LoadComboItems()
        {
            if (MainClass.Language == "ar")
            {
                cmbType.Items.Add("نقدية");
                cmbType.Items.Add("آجلة");
                cmbInvType.Items.Add("مبيعات");
                cmbInvType.Items.Add("نقطة بيع");
            }
            else
            {
                cmbType.Items.Add("Cash");
                cmbType.Items.Add("Postpone");
                cmbInvType.Items.Add("Sales Inv");
                cmbInvType.Items.Add("POS");
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted = 0 ORDER BY id",
                    _conn).Fill(table);

                cmbusers.ItemsSource = table.DefaultView;
                cmbusers.DisplayMemberPath = "name";
                cmbusers.SelectedValuePath = "id";
                cmbusers.SelectedIndex = -1;
            }
            catch { /* تجاهل خطأ التحميل */ }
        }

        private void LoadStores()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT id, name FROM Safes WHERE IS_Deleted = 0 AND status <> 2 ORDER BY id",
                    _conn).Fill(table);

                cmbStore.ItemsSource = table.DefaultView;
                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.SelectedIndex = -1;
            }
            catch { /* تجاهل */ }
        }

        private void LoadCustomers()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT id, name FROM Customers WHERE (type = 1 OR type = 3) ORDER BY id",
                    _conn).Fill(table);

                cmbClients.ItemsSource = table.DefaultView;
                cmbClients.DisplayMemberPath = "name";
                cmbClients.SelectedValuePath = "id";
                cmbClients.SelectedIndex = -1;
            }
            catch { /* تجاهل */ }
        }

        private void LoadBranches()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted = 0",
                    _conn).Fill(table);

                cmbBranches.ItemsSource = table.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.SelectedIndex = -1;
            }
            catch { /* تجاهل */ }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id = 12",
                    _conn).Fill(table);

                if (table.Rows.Count == 1)
                {
                    _printType = Convert.ToInt32(table.Rows[0]["printType"]);
                    _printFooter = Convert.ToBoolean(table.Rows[0]["PrintFooter"]);
                    _printHeader = Convert.ToBoolean(table.Rows[0]["PrintHeader"]);
                    _printStamp = Convert.ToBoolean(table.Rows[0]["PrintStamp"]);
                    _defPrinter = table.Rows[0]["CasherPrinter"]?.ToString() ?? "";
                    _printNo = Convert.ToInt32(table.Rows[0]["printNo"]);

                    if (string.IsNullOrWhiteSpace(_defPrinter))
                        _defPrinter = Common.GetDefaultPrinter();
                }

                var genTable = new DataTable();
                new SqlDataAdapter(
                    $"SELECT * FROM SettingGeneral WHERE Inv_Id = {Invtype}",
                    _conn).Fill(genTable);

                if (genTable.Rows.Count == 1)
                {
                    _priceIncVAT = Convert.ToBoolean(genTable.Rows[0]["PriceIncVAT"]);
                    _defVAT = Convert.ToDouble(genTable.Rows[0]["MainVAT"]);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ في إعدادات الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Helper Methods
        private string ResolveCustomerName(InvoiceDGV invoice)
        {
            string customerName =
                GetStringProperty(invoice, "CustomerName") ??
                GetStringProperty(invoice, "DgvClient") ??
                GetStringProperty(invoice, "ClientName") ??
                GetStringProperty(invoice, "CustName");

            if (!string.IsNullOrWhiteSpace(customerName))
                return customerName;

            if (invoice.Customer > 0)
                return GetCustomerNameById(invoice.Customer);

            return string.Empty;
        }

        private double ResolveInvoiceTotal(InvoiceDGV invoice)
        {
            double? total =
                GetDoubleProperty(invoice, "InvSum") ??
                GetDoubleProperty(invoice, "DgvSum") ??
                GetDoubleProperty(invoice, "DgvTotal") ??
                GetDoubleProperty(invoice, "Total");

            return total ?? 0.0;
        }

        private string GetCustomerNameById(int customerId)
        {
            try
            {
                string query = $"SELECT name FROM Customers WHERE id = {customerId}";
                DataTable dataTable = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    return dataTable.Rows[0]["name"]?.ToString() ?? string.Empty;
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetStringProperty(object source, string propertyName)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            object value = property.GetValue(source, null);
            return value?.ToString();
        }

        private double? GetDoubleProperty(object source, string propertyName)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            object value = property.GetValue(source, null);
            if (value == null || value == DBNull.Value)
                return null;

            if (double.TryParse(value.ToString(), out double result))
                return result;

            return null;
        }
        #endregion

        // ══════════════════════════════════════════════════════════
        #region Business Logic

        /// <summary>
        /// بناء شرط SQL من الفلاتر المحددة وجلب الفواتير
        /// </summary>
        private void BindInvoices()
        {
            try
            {
                _displayRows.Clear();
                _invoicesList?.Clear();

                string whereClause = BuildWhereClause();
                string sqlQuery = $"SELECT * FROM inv WHERE {whereClause} IS_Deleted = 0 ORDER BY id";

                _invoicesList = new InvoiceOper().BindingListOfInvoices(sqlQuery);

                foreach (var inv in _invoicesList)
                {
                    _displayRows.Add(new InvoiceDisplayRow
                    {
                        InvGlobalID = inv.InvGlobalID,
                        InvoiceNo = inv.InvoiceNo,
                        InvDate = inv.InvDate,
                        CustomerName = ResolveCustomerName(inv),
                        InvTotal = ResolveInvoiceTotal(inv),
                        InvoiceStatus = inv.InvoiceStatus,
                        DeliveryStatusText = GetDeliveryStatusText(inv.InvoiceStatus),
                        IsSelected = false
                    });
                }

                UpdateFooterStatus();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في تحميل الفواتير:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// بناء جملة WHERE من الفلاتر المحددة
        /// </summary>
        private string BuildWhereClause()
        {
            string clause = "";

            // فرع الفاتورة
            if (!(CheckBox1.IsChecked ?? true) && cmbBranches.SelectedValue != null)
                clause += $" inv.branch = {cmbBranches.SelectedValue} AND ";

            // العميل
            if (!(ckAllClients.IsChecked ?? true) && cmbClients.SelectedValue != null)
                clause += $" cust_id = {cmbClients.SelectedValue} AND ";

            // المستخدم
            if (!(ckAllUsers.IsChecked ?? true) && cmbusers.SelectedValue != null)
                clause += $" sales_emp = {cmbusers.SelectedValue} AND ";

            // المستودع
            if (!(ckAllStore.IsChecked ?? true) && cmbStore.SelectedValue != null)
                clause += $" inv.safe = {cmbStore.SelectedValue} AND ";

            // نوع الفاتورة
            if (!(ckAllInvs.IsChecked ?? true))
            {
                if (cmbInvType.SelectedIndex == 0)
                    clause += " inv.inv_type = 2 AND ";
                else if (cmbInvType.SelectedIndex == 1)
                    clause += " inv.inv_type = 3 AND ";
            }
            else
            {
                clause += " (inv.inv_type = 2 OR inv.inv_type = 3) AND ";
            }

            // حالة التسليم
            if (rbDelivered.IsChecked == true)
                clause += " (inv.InvoiceStatus = 3 OR inv.InvoiceStatus = 4) AND ";
            else if (rbNotDelivered.IsChecked == true)
                clause += " inv.InvoiceStatus = 2 AND ";

            // نوع الدفع
            if (!(ckAllType.IsChecked ?? true))
            {
                if (cmbType.SelectedIndex == 1)
                    clause += " inv.pay_type = -1 AND ";
                else if (cmbType.SelectedIndex == 0)
                    clause += " inv.pay_type > 0 AND ";
            }

            // الفترة الزمنية
            if (!(ckTotalPeriod.IsChecked ?? true))
            {
                string fromDate = txtFromDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
                string toDate = txtToDate.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
                clause += $" date >= '{fromDate}' AND date <= '{toDate}' AND ";
            }

            return clause;
        }

        /// <summary>
        /// تسليم كلي لفاتورة واحدة مباشرة
        /// </summary>
        private void DeliverAllItemsForInvoice(InvoiceDGV invoice)
        {
            try
            {
                using (SqlConnection conn = MainClass.ConnObj())
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    int batchDelivIncId = GetNextId(conn,
                        "SELECT ISNULL(MAX(BatchDelivIncId), 0) FROM ItemBatchDeliveries");

                    int batchDelivNo = GetNextId(conn,
                        $"SELECT ISNULL(MAX(BatchDelivNo), 0) FROM ItemBatchDeliveries WHERE InvGlobalID = N'{invoice.InvGlobalID}'");

                    List<ItemBatchDelivery> deliveryList = new List<ItemBatchDelivery>();

                    foreach (InvoiceItem item in invoice.InvoiceItems)
                    {
                        deliveryList.Add(new ItemBatchDelivery
                        {
                            BatchDelivNo = batchDelivNo,
                            BatchDelivIncId = batchDelivIncId,
                            InvGlobalID = invoice.InvGlobalID,
                            ItemId = item.ItemId,
                            ItemUnitId = item.UnitID,
                            ItemQuantity = (float)item.ItemQuantity,
                            BatchDeliveryDate = DateTime.Now,
                            InvertoryEmp = MainClass.EmpNo,
                            RecipientID = invoice.Customer,
                            Note = "تسليم فاتورة رقم " + invoice.InvoiceNo
                        });
                    }

                    new ItemOper().SaveInvoiceItemDeliveries(deliveryList, true);

                    DXMessageBox.Show(
                        "تم التسليم الكلي بنجاح ✅",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    BindInvoices();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    "خطأ في التسليم الكلي:\n" + ex.Message,
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// جلب القيمة التالية من استعلام MAX
        /// </summary>
        private int GetNextId(SqlConnection conn, string query)
        {
            return Convert.ToInt32(new SqlCommand(query, conn).ExecuteScalar()) + 1;
        }

        /// <summary>
        /// ترجمة حالة الفاتورة إلى نص
        /// </summary>
        private string GetDeliveryStatusText(int status)
        {
            return status switch
            {
                1 => "🟡 جديدة",
                2 => "🔴 لم تُسلَّم",
                3 => "🟢 مسلّمة جزئياً",
                4 => "✅ مسلّمة بالكامل",
                _ => "—"
            };
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Reporting

        private DataSet BuildReportDataSet()
        {
            var reportItems = _displayRows.Select(row => new InventoryData
            {
                InvoiceNo = row.InvoiceNo.ToString(),
                InvDate = row.InvDate.ToString("yyyy-MM-dd"),
                Clint = row.CustomerName,
                Total = row.InvTotal.ToString("N2"),
                FromDate = txtFromDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                ToDate = txtToDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                ProcessType = cmbType.SelectedItem?.ToString() ?? "",
                User = Common.GetEmpName(MainClass.EmpNo),
                PrintDate = DateTime.Now.ToShortDateString()
            }).ToList();

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(reportItems));
            return dataSet;
        }

        private void PrintDevexpress(int printType)
        {
            _rptUrl = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName = "rptInvsProfitSales.repx";

            if (_displayRows.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);

            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                var headerReport = XtraReport.FromFile(Path.Combine(_rptUrl, "header.repx"));
                headerReport.DataSource = Common.FoundationInfoDT;

                var subReport = report.FindControl("headerRpt", ignoreCase: true) as XRSubreport;
                if (subReport != null)
                    subReport.ReportSource = headerReport;

                if (!string.IsNullOrWhiteSpace(_defPrinter))
                {
                    report.PrinterName = _defPrinter;

                    if (printType == 1)
                    {
                        for (int i = 0; i < _printNo; i++)
                            report.Print();
                    }
                    else
                    {
                        report.ShowPreviewDialog();
                    }
                }
                else
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            var table = new DataTable("Invoices");
            table.Columns.Add("رقم الفاتورة");
            table.Columns.Add("التاريخ");
            table.Columns.Add("العميل");
            table.Columns.Add("الإجمالي");
            table.Columns.Add("حالة التسليم");

            foreach (var row in _displayRows)
            {
                table.Rows.Add(
                    row.InvoiceNo,
                    row.InvDate.ToString("yyyy-MM-dd"),
                    row.CustomerName,
                    row.InvTotal.ToString("N2"),
                    row.DeliveryStatusText);
            }

            table.WriteXml(filePath);
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helpers

        private void UpdateFooterStatus()
        {
            int count = _displayRows.Count;
            lblFooterStatus.Text = MainClass.Language == "ar"
                ? $"📋 إجمالي الفواتير: {count}"
                : $"📋 Total Invoices: {count}";
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Display Model: InvoiceDisplayRow

    /// <summary>
    /// نموذج عرض الفاتورة في DataGrid
    /// </summary>
    public class InvoiceDisplayRow : INotifyPropertyChanged
    {
        public string InvGlobalID { get; set; }
        public int InvoiceNo { get; set; }
        public DateTime InvDate { get; set; }
        public string CustomerName { get; set; }
        public double InvTotal { get; set; }
        public int InvoiceStatus { get; set; }
        public string DeliveryStatusText { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion
}