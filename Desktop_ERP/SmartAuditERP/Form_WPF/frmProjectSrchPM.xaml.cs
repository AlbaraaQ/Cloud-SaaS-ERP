using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmProjectSrchPM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        public int    selectedCli = 0;
        public string Itemname    = "";
        public int    ProjectNo   = -1;

        private DataTable _dtable  = new DataTable();
        private string    _loadQry =
            "SELECT id, name, ContractorPk, Type, statusPk " +
            "FROM PM_Projects WHERE IS_Deleted=0 ORDER BY id DESC";

        #endregion

        #region Constructor

        public frmProjectSrchPM()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            LoadContractor();
            LoadDgvItems();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        #endregion

        #region Load Data

        public void LoadContractor()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM PM_Contractor ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbContractor.ItemsSource       = dt.DefaultView;
                    cmbContractor.DisplayMemberPath = "name";
                    cmbContractor.SelectedValuePath = "id";
                    cmbContractor.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        public void LoadGroups()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM PM_TirmGroup ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbgroups.ItemsSource       = dt.DefaultView;
                    cmbgroups.DisplayMemberPath = "name";
                    cmbgroups.SelectedValuePath = "id";
                    cmbgroups.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        public void LoadDgvItems()
        {
            try
            {
                _dtable = new DataTable();
                using (var da = new SqlDataAdapter(_loadQry, _conn))
                    da.Fill(_dtable);

                dgvItems.ItemsSource = _dtable.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المشاريع: " + ex.Message);
            }
        }

        #endregion

        #region Filter Events

        private void txtSrchCode_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                var dv = new DataView(_dtable);
                dv.RowFilter = $"id LIKE '%{txtSrchCode.Text}%'";
                dgvItems.ItemsSource = dv;
            }
            catch { }
        }

        private void txtSrchNm_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                var dv = new DataView(_dtable);
                dv.RowFilter = $"name LIKE '%{txtSrchNm.Text}%'";
                dgvItems.ItemsSource = dv;
            }
            catch { }
        }

        private void cmbgroups_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbgroups.SelectedIndex > -1)
                {
                    var dv = new DataView(_dtable);
                    dv.RowFilter = $"Type={cmbgroups.SelectedValue}";
                    dgvItems.ItemsSource = dv;
                }
            }
            catch { }
        }

        private void cmbContractor_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbContractor.SelectedIndex > -1)
                {
                    var dv = new DataView(_dtable);
                    dv.RowFilter = $"ContractorPk={cmbContractor.SelectedValue}";
                    dgvItems.ItemsSource = dv;
                }
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is DataRowView row)
                {
                    ProjectNo = Convert.ToInt32(row["id"]);
                    Itemname  = row["name"].ToString();
                    this.Close();
                }
            }
            catch { }
        }

        #endregion
    }
}