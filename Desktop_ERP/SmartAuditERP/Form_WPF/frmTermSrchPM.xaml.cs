using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTermSrchPM : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        public int    TermId   { get; set; } = -1;
        public string Itemname { get; set; } = "";

        private DataTable _fullData;
        private ObservableCollection<TermSearchItem> _items;

        #endregion

        #region Constructor

        public frmTermSrchPM()
        {
            InitializeComponent();
            conn      = MainClass.ConnObj();
            _fullData = new DataTable();
            _items    = new ObservableCollection<TermSearchItem>();
            dgvItems.ItemsSource = _items;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDgvItems();
            LoadGroup();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Data Loading

        private void LoadDgvItems()
        {
            try
            {
                _fullData.Clear();
                _fullData = new DataTable();

                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT Code, name, nameEN, price, type, ParentCode FROM PM_Terms WHERE IS_Deleted=0 AND Type=2 ORDER BY Code",
                    conn);
                adapter.Fill(_fullData);
                EnsureClose(conn);

                RefreshItems(_fullData.DefaultView);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البنود: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGroup()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms WHERE type=1 ORDER BY Code", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbTermGrp.ItemsSource       = dt.DefaultView;
                cmbTermGrp.DisplayMemberPath = "name";
                cmbTermGrp.SelectedValuePath = "Code";
                cmbTermGrp.SelectedIndex     = -1;
            }
            catch (Exception ex) { }
            finally { EnsureClose(conn); }
        }

        private void RefreshItems(DataView view)
        {
            _items.Clear();
            foreach (DataRowView row in view)
            {
                _items.Add(new TermSearchItem
                {
                    Code   = row["Code"].ToString(),
                    Name   = row["name"].ToString(),
                    NameEn = row["nameEN"].ToString(),
                    Price  = row["price"] != DBNull.Value ? Convert.ToDouble(row["price"]) : 0
                });
            }
        }

        #endregion

        #region Filter Events

        private void txtSrchNm_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                var view = new DataView(_fullData);
                view.RowFilter = $"name LIKE '%{txtSrchNm.Text}%'";
                RefreshItems(view);
            }
            catch { }
        }

        private void txtSrchNmEn_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                var view = new DataView(_fullData);
                view.RowFilter = $"nameEN LIKE '%{txtSrchNmEn.Text}%'";
                RefreshItems(view);
            }
            catch { }
        }

        private void txtSrchCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtSrchCode.Text))
                {
                    RefreshItems(_fullData.DefaultView);
                    return;
                }
                var view = new DataView(_fullData);
                view.RowFilter = $"Code = '{txtSrchCode.Text}'";
                RefreshItems(view);
            }
            catch { }
        }

        private void cmbTermGrp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbTermGrp.SelectedIndex > -1 && cmbTermGrp.SelectedValue != null)
                {
                    var view = new DataView(_fullData);
                    view.RowFilter = $"ParentCode={cmbTermGrp.SelectedValue}";
                    RefreshItems(view);
                }
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvItems.SelectedItem is TermSearchItem item)
            {
                if (int.TryParse(item.Code, out int termId))
                    TermId = termId;
                Itemname = item.Name;
                this.Close();
            }
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

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

    public class TermSearchItem
    {
        public string Code   { get; set; }
        public string Name   { get; set; }
        public string NameEn { get; set; }
        public double Price  { get; set; }
    }
}