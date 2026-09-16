using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvoiceRentSrch : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int selectedCli;
        public int StoreId;
        public string sql;
        public string Cond;
        public int ProcType;
        public int InvID;
        public int InvType;
        public bool ISDone;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;
        private string _loadQuery;
        private DataTable _masterTable;
        private DataView _currentView;
        private int _rowIndex;
        private ObservableCollection<InvoiceRentRow> _displayRows;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvoiceRentSrch()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            StoreId = 1;
            ProcType = 1;
            InvID = -1;
            InvType = 1;
            ISDone = false;
            _masterTable = new DataTable();
            _rowIndex = -1;
            _displayRows = new ObservableCollection<InvoiceRentRow>();

            dgvItems.ItemsSource = _displayRows;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BuildLoadQuery();

            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;

            LoadInvoices();
            LoadUsers();
            LoadProcessTypes();

            // افتراضي: فاتورة
            if (cmbProcType.Items.Count > 1)
                cmbProcType.SelectedIndex = 1;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (_rowIndex > -1 && _rowIndex < _displayRows.Count)
                {
                    try
                    {
                        var row = _displayRows[_rowIndex];
                        ISDone = true;
                        InvID = row.Id;
                        ProcType = row.ProcType;
                        Close();
                    }
                    catch { }
                    return;
                }

                if (_displayRows.Count > 0)
                {
                    dgvItems.SelectedIndex = 0;
                    dgvItems.ScrollIntoView(_displayRows[0]);
                }
            }
            else if (e.Key == Key.Up)
            {
                NavigateUp();
            }
            else if (e.Key == Key.Down)
            {
                NavigateDown();
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Header Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Filter Events

        private void cmbProcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtInvNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtClientName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtClientMobile_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtMinPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtMaxPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtMaxPrice.Text))
            {
                if (string.IsNullOrWhiteSpace(txtMinPrice.Text))
                    txtMinPrice.Text = "0";

                ApplyFilter();
            }
        }

        private void txtFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void cmbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) NavigateUp();
            else if (e.Key == Key.Down) NavigateDown();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtMaxPrice.Text = "";
            txtMinPrice.Text = "";
            txtInvNo.Text = "";
            txtClientName.Text = "";
            txtClientMobile.Text = "";
            cmbUsers.SelectedIndex = -1;

            if (cmbProcType.Items.Count > 1)
                cmbProcType.SelectedIndex = 1;

            ApplyFilter();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Grid Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rowIndex = dgvItems.SelectedIndex;
        }

        private void dgvItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var row = dgvItems.SelectedItem as InvoiceRentRow;
                if (row == null) return;

                ISDone = true;
                InvID = row.Id;
                ProcType = row.ProcType;
                Hide();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        private void BuildLoadQuery()
        {
            if (InvType == 1)
            {
                _loadQuery = @"
                    SELECT inv.proc_id   AS proc_id,
                           inv.proc_type,
                           inv.id        AS id,
                           inv.tot_net   AS Net,
                           inv.date      AS date,
                           Customers.name AS cust,
                           Users.username AS emp,
                           Customers.mobile
                    FROM   inv, Customers, Users
                    WHERE  inv.cust_id    = Customers.id
                      AND  inv.sales_emp  = Users.emp
                      AND  inv.IS_Deleted = 0
                    ORDER BY inv.id DESC";
            }
            else
            {
                _loadQuery = @"
                    SELECT RentInvoice.proc_id   AS proc_id,
                           RentInvoice.proc_type,
                           RentInvoice.id        AS id,
                           RentInvoice.tot_net   AS Net,
                           RentInvoice.date      AS date,
                           Customers.name        AS cust,
                           Users.username        AS emp,
                           Customers.mobile
                    FROM   RentInvoice, Customers, Users
                    WHERE  RentInvoice.cust_id    = Customers.id
                      AND  RentInvoice.sales_emp  = Users.emp
                      AND  RentInvoice.IS_Deleted = 0
                    ORDER BY RentInvoice.id DESC";
            }
        }

        public void LoadInvoices()
        {
            try
            {
                _masterTable.Clear();
                new SqlDataAdapter(_loadQuery, _conn).Fill(_masterTable);
                RefreshDisplayRows(new DataView(_masterTable));
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في تحميل الفواتير:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadUsers()
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    "SELECT emp, username FROM Users WHERE IS_Deleted = 0 AND id > 0 ORDER BY emp",
                    _conn).Fill(table);

                cmbUsers.ItemsSource = table.DefaultView;
                cmbUsers.DisplayMemberPath = "username";
                cmbUsers.SelectedValuePath = "emp";
                cmbUsers.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadProcessTypes()
        {
            cmbProcType.Items.Clear();

            if (InvType == 1)
            {
                cmbProcType.Items.Add("الكل");
                cmbProcType.Items.Add("فاتورة");
                cmbProcType.Items.Add("مرتجع");
            }
            else
            {
                cmbProcType.Items.Add("الكل");
                cmbProcType.Items.Add("تأجير");
                cmbProcType.Items.Add("مرتجع");
                cmbProcType.Items.Add("معلق");
                cmbProcType.Items.Add("حجوزات");
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Filter Logic

        private void ApplyFilter()
        {
            if (_masterTable == null || _masterTable.Rows.Count == 0)
                return;

            try
            {
                string filter = BuildFilterExpression();
                var view = new DataView(_masterTable)
                {
                    RowFilter = filter
                };

                RefreshDisplayRows(view);
            }
            catch { /* تجاهل أخطاء الفلترة */ }
        }

        private string BuildFilterExpression()
        {
            var conditions = new System.Collections.Generic.List<string>();

            // نوع العملية
            int procIndex = cmbProcType.SelectedIndex;
            if (procIndex > 0)
                conditions.Add($"proc_type = {procIndex}");

            // رقم الفاتورة
            if (!string.IsNullOrWhiteSpace(txtInvNo.Text) &&
                int.TryParse(txtInvNo.Text.Trim(), out int invNo))
                conditions.Add($"id = {invNo}");

            // اسم العميل
            if (!string.IsNullOrWhiteSpace(txtClientName.Text))
                conditions.Add($"cust LIKE '%{txtClientName.Text.Trim()}%'");

            // جوال العميل
            if (!string.IsNullOrWhiteSpace(txtClientMobile.Text))
                conditions.Add($"mobile LIKE '%{txtClientMobile.Text.Trim()}%'");

            // المستخدم
            if (cmbUsers.SelectedIndex > -1 && cmbUsers.Text != null)
                conditions.Add($"emp LIKE '%{cmbUsers.Text}%'");

            // نطاق السعر
            if (!string.IsNullOrWhiteSpace(txtMinPrice.Text) &&
                double.TryParse(txtMinPrice.Text, out double minPrice))
            {
                conditions.Add($"Net >= {minPrice}");
            }

            if (!string.IsNullOrWhiteSpace(txtMaxPrice.Text) &&
                double.TryParse(txtMaxPrice.Text, out double maxPrice))
            {
                conditions.Add($"Net <= {maxPrice}");
            }

            // نطاق التاريخ
            if (txtFromDate.SelectedDate.HasValue)
            {
                string fromDate = txtFromDate.SelectedDate.Value.ToString("yyyy-MM-dd");
                conditions.Add($"date >= #{fromDate}#");
            }

            if (txtToDate.SelectedDate.HasValue)
            {
                string toDate = txtToDate.SelectedDate.Value.ToString("yyyy-MM-dd");
                conditions.Add($"date <= #{toDate}#");
            }

            return string.Join(" AND ", conditions);
        }

        private void RefreshDisplayRows(DataView view)
        {
            _displayRows.Clear();

            foreach (DataRowView rowView in view)
            {
                _displayRows.Add(new InvoiceRentRow
                {
                    ProcId = rowView["proc_id"] == DBNull.Value ? 0 : Convert.ToInt32(rowView["proc_id"]),
                    ProcType = rowView["proc_type"] == DBNull.Value ? 0 : Convert.ToInt32(rowView["proc_type"]),
                    Id = rowView["id"] == DBNull.Value ? 0 : Convert.ToInt32(rowView["id"]),
                    Net = rowView["Net"] == DBNull.Value ? 0 : Convert.ToDouble(rowView["Net"]),
                    InvDate = rowView["date"] == DBNull.Value
                                    ? DateTime.MinValue
                                    : Convert.ToDateTime(rowView["date"]),
                    CustomerName = rowView["cust"]?.ToString() ?? "",
                    EmpName = rowView["emp"]?.ToString() ?? "",
                    Mobile = rowView["mobile"]?.ToString() ?? ""
                });
            }

            UpdateStatus();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Navigation

        private void NavigateUp()
        {
            if (dgvItems.SelectedIndex > 0)
                dgvItems.SelectedIndex--;
        }

        private void NavigateDown()
        {
            if (dgvItems.SelectedIndex < _displayRows.Count - 1)
                dgvItems.SelectedIndex++;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helpers

        private void UpdateStatus()
        {
            lblStatus.Text = $"🧾 إجمالي الفواتير: {_displayRows.Count}";
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Model: InvoiceRentRow

    public class InvoiceRentRow : INotifyPropertyChanged
    {
        public int ProcId { get; set; }
        public int ProcType { get; set; }
        public int Id { get; set; }
        public double Net { get; set; }
        public DateTime InvDate { get; set; }
        public string CustomerName { get; set; }
        public string EmpName { get; set; }
        public string Mobile { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion
}