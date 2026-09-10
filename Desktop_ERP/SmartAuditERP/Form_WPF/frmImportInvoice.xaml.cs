using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmImportInvoice : ThemedWindow
    {
        #region ── Inner Model ──────────────────────────────────────────────

        private class InvDgv
        {
            public string InvNo { get; set; } = string.Empty;
            public double InvNet { get; set; }
            public DateTime InvDate { get; set; }
            public string InvClient { get; set; } = string.Empty;
            public string InvGlobalId { get; set; } = string.Empty;
        }

        #endregion

        #region ── Fields ──────────────────────────────────────────────────

        private readonly SqlConnection _conn;
        private readonly List<InvDgv> _invoiceList;

        // أنواع العمليات (فهرس → شرط SQL)
        private static readonly string[] _processConditions =
        {
            "inv_type = 2 AND proc_type = 4",  // عرض سعر
            "inv_type = 2 AND proc_type = 1",  // فاتورة مبيعات
            "inv_type = 2 AND proc_type = 2",  // مرتجع مبيعات
            "inv_type = 1 AND proc_type = 1",  // فاتورة مشتريات
            "inv_type = 1 AND proc_type = 2",  // مرتجع مشتريات
            "inv_type = 9 AND proc_type = 1",  // بضاعة أول مدة
            "inv_type = 4 AND proc_type = 1",  // فاتورة إدخال
            "inv_type = 5 AND proc_type = 1",  // فاتورة إخراج
            "inv_type = 3 AND proc_type = 1",  // نقطة بيع
            "inv_type = 3 AND proc_type = 2"   // مرتجع نقطة البيع
        };

        // Public fields (محافظ على الأسماء الأصلية)
        public string InvGID = "-1";
        public bool isDone = false;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmImportInvoice()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            _invoiceList = new List<InvDgv>();
        }

        #endregion

        #region ── Window Loaded ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProcessTypes();
            LoadBranches();

            txtFromDate.SelectedDate = DateTime.Now;
            txtToDate.SelectedDate = DateTime.Now;

            GridControl1.ItemsSource = _invoiceList;
        }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        /// <summary>
        /// تحميل أنواع العمليات في cmbProcType حسب لغة النظام.
        /// </summary>
        public void LoadProcessTypes()
        {
            cmbProcType.Items.Clear();

            bool isArabic = MainClass.Language == "ar";

            string[] items = isArabic
                ? new[]
                {
                    "عرض سعر", "فاتورة مبيعات", "مرتجع مبيعات",
                    "فاتورة مشتريات", "مرتجع مشتريات", "بضاعة أول مدة",
                    "فاتورة إدخال", "فاتورة إخراج", "فاتورة نقطة بيع",
                    "مرتجع نقطة البيع"
                }
                : new[]
                {
                    "Quotation", "Sale Invoice", "Return Sale",
                    "Purchase Invoice", "Return Purchase", "Beginning Inventory",
                    "Input Invoice", "Output Invoice", "POS",
                    "Return POS"
                };

            foreach (string item in items)
                cmbProcType.Items.Add(item);

            cmbProcType.SelectedIndex = 0;
        }

        /// <summary>
        /// تحميل الفروع في cmbBranches.
        /// </summary>
        private void LoadBranches()
        {
            try
            {
                string branchFilter = string.Empty;

                if (MainClass.BranchNo != -1)
                {
                    bool hasBranchCondition =
                        !string.IsNullOrWhiteSpace(Accounting.BranchCondition) &&
                        Accounting.BranchCondition.Trim() != " ";

                    branchFilter = hasBranchCondition
                        ? $" AND id = {MainClass.BranchNo}"
                        : string.Empty;
                }

                string sql = $"SELECT id, name FROM Branches WHERE IS_Deleted = 0{branchFilter}";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.ItemsSource = dataTable.DefaultView;

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else if (dataTable.Rows.Count > 0)
                    cmbBranches.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الفروع", ex);
            }
        }

        /// <summary>
        /// تحميل الفواتير من قاعدة البيانات بناءً على الفلاتر الحالية.
        /// </summary>
        private void LoadInvoices()
        {
            try
            {
                _invoiceList.Clear();
                GridControl1.ItemsSource = null;

                // تحديد شرط الفرع
                string branchCondition = $"inv.branch = {MainClass.BranchNo}";

                if (cmbBranches.SelectedIndex >= 0 && cmbBranches.SelectedValue != null)
                    branchCondition = $"inv.branch = {cmbBranches.SelectedValue}";

                // تحديد نوع العملية
                int procIndex = cmbProcType.SelectedIndex;
                if (procIndex < 0 || procIndex >= _processConditions.Length)
                    procIndex = 0;

                string processCondition = _processConditions[procIndex];

                string sql = $@"
                    SELECT inv.id          AS InvNo,
                           inv.tot_net     AS InvNet,
                           inv.date        AS InvDate,
                           Customers.name  AS InvClient,
                           inv.InvGlobalID AS InvGlobalId
                    FROM   Inv
                    JOIN   Customers ON inv.cust_id = Customers.id
                    WHERE  {branchCondition}
                      AND  inv.IS_Deleted = 0
                      AND  {processCondition}
                    ORDER BY inv.id DESC";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    _invoiceList.Add(new InvDgv
                    {
                        InvNo = row["InvNo"]?.ToString() ?? string.Empty,
                        InvNet = SafeDouble(row["InvNet"]),
                        InvDate = SafeDate(row["InvDate"]),
                        InvClient = row["InvClient"]?.ToString() ?? string.Empty,
                        InvGlobalId = row["InvGlobalId"]?.ToString() ?? string.Empty
                    });
                }

                RefreshGrid(_invoiceList);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الفواتير", ex);
            }
        }

        #endregion

        #region ── Grid Refresh ────────────────────────────────────────────

        private void RefreshGrid(IEnumerable<InvDgv> source)
        {
            GridControl1.ItemsSource = null;
            GridControl1.ItemsSource = source.ToList();
        }

        #endregion

        #region ── Filter Methods ───────────────────────────────────────────

        private void FilterByInvoiceNo(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshGrid(_invoiceList);
                return;
            }

            var filtered = _invoiceList
                .Where(inv => inv.InvNo.Contains(searchText))
                .ToList();

            RefreshGrid(filtered);
        }

        private void FilterByDate()
        {
            if (ckTotalPeriod.IsChecked == true)
            {
                RefreshGrid(_invoiceList);
                return;
            }

            DateTime fromDate = txtFromDate.SelectedDate ?? DateTime.Now;
            DateTime toDate = txtToDate.SelectedDate ?? DateTime.Now;

            var filtered = _invoiceList
                .Where(inv => inv.InvDate >= fromDate && inv.InvDate <= toDate)
                .ToList();

            RefreshGrid(filtered);
        }

        private void FilterByTotal()
        {
            bool hasMin = double.TryParse(txtMinPrice.Text, out double minPrice);
            bool hasMax = double.TryParse(txtMaxPrice.Text, out double maxPrice);

            if (!hasMin || !hasMax)
            {
                RefreshGrid(_invoiceList);
                return;
            }

            var filtered = _invoiceList
                .Where(inv => inv.InvNet >= minPrice && inv.InvNet <= maxPrice)
                .ToList();

            RefreshGrid(filtered);
        }

        private void FilterByClientName(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshGrid(_invoiceList);
                return;
            }

            var filtered = _invoiceList
                .Where(inv => inv.InvClient.Contains(searchText))
                .ToList();

            RefreshGrid(filtered);
        }

        #endregion

        #region ── Control Events ───────────────────────────────────────────

        private void cmbProcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInvoices();
        }

        private void cmbBranches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInvoices();
        }

        private void txtInvNo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try { FilterByInvoiceNo(txtInvNo.Text); }
            catch (Exception ex) { ShowError("خطأ في فلتر رقم الفاتورة", ex); }
        }

        private void txtClientName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try { FilterByClientName(txtClientName.Text); }
            catch (Exception ex) { ShowError("خطأ في فلتر اسم العميل", ex); }
        }

        private void txtFromDate_SelectedDateChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try { FilterByDate(); }
            catch (Exception ex) { ShowError("خطأ في فلتر التاريخ", ex); }
        }

        private void txtToDate_SelectedDateChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try { FilterByDate(); }
            catch (Exception ex) { ShowError("خطأ في فلتر التاريخ", ex); }
        }

        private void txtMinPrice_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try { FilterByTotal(); }
            catch (Exception ex) { ShowError("خطأ في فلتر الصافي", ex); }
        }

        private void txtMaxPrice_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try { FilterByTotal(); }
            catch (Exception ex) { ShowError("خطأ في فلتر الصافي", ex); }
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = ckTotalPeriod.IsChecked == true;

            txtFromDate.IsEnabled = !isChecked;
            txtToDate.IsEnabled = !isChecked;

            LoadInvoices();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtInvNo.Text = string.Empty;
            txtClientName.Text = string.Empty;
            txtMinPrice.Text = string.Empty;
            txtMaxPrice.Text = string.Empty;
            ckTotalPeriod.IsChecked = true;
            txtFromDate.SelectedDate = DateTime.Now;
            txtToDate.SelectedDate = DateTime.Now;
        }

        /// <summary>
        /// حدث ضغط زر "إدراج" داخل صف الجدول.
        /// يقوم بقراءة InvGlobalId من Tag الزر.
        /// </summary>
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string globalId)
                {
                    if (!string.IsNullOrEmpty(globalId))
                    {
                        InvGID = globalId;
                        isDone = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إدراج الفاتورة", ex);
            }
        }

        #endregion

        #region ── Helpers ──────────────────────────────────────────────────

        private static double SafeDouble(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            return double.TryParse(value.ToString(), out double result) ? result : 0.0;
        }

        private static DateTime SafeDate(object value)
        {
            if (value == null || value == DBNull.Value) return DateTime.MinValue;
            return DateTime.TryParse(value.ToString(), out DateTime result)
                   ? result : DateTime.MinValue;
        }

        private void ShowError(string title, Exception ex)
        {
            DXMessageBox.Show(
                $"{title}\nتفاصيل الخطأ: {ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }
}