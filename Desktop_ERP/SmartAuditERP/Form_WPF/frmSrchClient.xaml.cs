using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSrchClient : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private DataTable _fullData;

        public int    selectedCli     { get; set; }
        public string sql             { get; set; }
        public string search          { get; set; }
        public string Clientname      { get; set; } = "";
        public int    ClientId        { get; set; }
        public string Salemanname     { get; set; } = "";
        public int    SalemanId       { get; set; }
        public int    offer           { get; set; }
        public bool   PostponeClient  { get; set; } = false;
        public int    Type            { get; set; }
        public bool   ChangeCust      { get; set; } = false;

        private ObservableCollection<ClientItem_frmSrchClient> _items;

        #endregion

        #region Constructor

        public frmSrchClient()
        {
            InitializeComponent();
            conn     = MainClass.ConnObj();
            _fullData = new DataTable();
            _items    = new ObservableCollection<ClientItem_frmSrchClient>();
            DataGridView1.ItemsSource = _items;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (Type)
            {
                case 1:
                    lblName.Text = "👤 اسم العميل";
                    LoadDgvClient();
                    break;
                case 2:
                    lblName.Text = "🏭 اسم المورد";
                    LoadDgvClient();
                    break;
                case 3:
                    lblName.Text = "👔 اسم المندوب";
                    LoadDgvSaleman();
                    break;
                case 4:
                    lblName.Text = "🏭 اسم المورد";
                    LoadVATSuppliers();
                    break;
                case 5:
                    lblName.Text = "👤 اسم العميل";
                    LoadDgvClient();
                    break;
                case 12:
                    LoadDgvClient();
                    break;
            }

            // التركيز على حقل الاسم
            if (!string.IsNullOrWhiteSpace(txtClientName.Text))
                FilterByName(txtClientName.Text);

            txtClientName.Focus();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        #endregion

        #region Data Loading

        public void LoadDgvClient()
        {
            try
            {
                _fullData.Clear();
                _fullData = new DataTable();

                string sql = PostponeClient
                    ? $"SELECT id, name, mobile FROM Customers WHERE (type={Type} OR type=3) AND IS_Deleted=0 AND AccountCode<>-1 ORDER BY id DESC"
                    : $"SELECT id, name, mobile FROM Customers WHERE (type={Type} OR type=3) AND IS_Deleted=0 ORDER BY id DESC";

                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(sql, conn);
                adapter.Fill(_fullData);
                EnsureClose(conn);

                // تحديث رؤوس الأعمدة
                colName.Header   = Type == 1 ? "👤 العميل"   : "🏭 المورد";
                colMobile.Header = "📱 الجوال";

                RefreshGridItems(_fullData.DefaultView);

                if (!string.IsNullOrWhiteSpace(txtClientName.Text))
                    FilterByName(txtClientName.Text);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadDgvSaleman()
        {
            try
            {
                _fullData.Clear();
                _fullData = new DataTable();

                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name, mobile FROM salesmen WHERE IS_Deleted=0 ORDER BY id",
                    conn);
                adapter.Fill(_fullData);
                EnsureClose(conn);

                colName.Header   = "👔 المندوب";
                colMobile.Header = "📱 الجوال";

                RefreshGridItems(_fullData.DefaultView);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVATSuppliers()
        {
            try
            {
                _fullData.Clear();
                _fullData = new DataTable();

                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name, taxNo AS mobile FROM Suppliers WHERE IS_Deleted=0",
                    conn);
                adapter.Fill(_fullData);
                EnsureClose(conn);

                colName.Header   = "🏭 الاسم";
                colMobile.Header = "🔢 الرقم الضريبي";

                RefreshGridItems(_fullData.DefaultView);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshGridItems(DataView view)
        {
            _items.Clear();
            foreach (DataRowView row in view)
            {
                _items.Add(new ClientItem_frmSrchClient
                {
                    Id     = row["id"]     != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                    Name   = row["name"]   != DBNull.Value ? row["name"].ToString()   : "",
                    Mobile = row["mobile"] != DBNull.Value ? row["mobile"].ToString() : ""
                });
            }
        }

        #endregion

        #region Filter

        private void FilterByName(string searchText)
        {
            try
            {
                var view = new DataView(_fullData);
                view.RowFilter = $"name LIKE '%{searchText}%'";
                RefreshGridItems(view);
            }
            catch { }
        }

        private void FilterByPhone(string searchText)
        {
            try
            {
                var view = new DataView(_fullData);
                view.RowFilter = $"mobile LIKE '%{searchText}%'";
                RefreshGridItems(view);
            }
            catch { }
        }

        #endregion

        #region TextBox Events

        private void txtClientName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterByName(txtClientName.Text);
        }

        private void txtCustPhone_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FilterByPhone(txtCustPhone.Text);
        }

        private void txtClientName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
                DataGridView1.Focus();
            }
            else if (e.Key == Key.Return)
            {
                e.Handled = true;
                AddClient();
            }
        }

        #endregion

        #region DataGrid Events

        private void DataGridView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }

        private void DataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SelectCurrentItem();
            }
        }

        private void SelectCurrentItem()
        {
            if (DataGridView1.SelectedItem is ClientItem_frmSrchClient item)
            {
                switch (Type)
                {
                    case 1:
                    case 2:
                    case 4:
                    case 5:
                    case 12:
                        ClientId   = item.Id;
                        Clientname = item.Name;
                        break;
                    case 3:
                        SalemanId   = item.Id;
                        Salemanname = item.Name;
                        break;
                }
                this.Close();
            }
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnAddClient_Click(object sender, RoutedEventArgs e) => AddClient();

        private void AddClient()
        {
            // فتح نافذة الإضافة حسب النوع
            DXMessageBox.Show($"افتح نافذة إضافة (Type={Type}) هنا", "إضافة جديد",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            // بعد الإضافة: LoadDgvClient() أو LoadDgvSaleman()
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }

    /// <summary>نموذج صف جدول العملاء/الموردين/المندوبين</summary>
    public class ClientItem_frmSrchClient
    {
        public int    Id     { get; set; }
        public string Name   { get; set; }
        public string Mobile { get; set; }
    }
}