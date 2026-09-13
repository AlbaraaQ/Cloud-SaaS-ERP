using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using log4net;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmContractSrchPM : ThemedWindow
    {
        #region ── Private Fields ──

        private SqlConnection conn;
        private DataTable dtable;
        private string LoadQry;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Public Fields ──

        public int selectedCli;
        public string Itemname;
        public int ContractNo;

        #endregion

        #region ── Constructor ──

        public frmContractSrchPM()
        {
            conn = MainClass.ConnObj();
            Itemname = "";
            ContractNo = -1;
            LoadQry = "SELECT ContrNo, PM_Status.name, statusPk, ClientPk, RegsDate " +
                         "FROM PM_ContractInv, PM_Status " +
                         "WHERE PM_ContractInv.statusPk = PM_Status.id " +
                         "AND PM_ContractInv.IS_Deleted = 0 " +
                         "ORDER BY ContrNo DESC";
            dtable = new DataTable();

            InitializeComponent();

            this.Loaded += Window_Loaded;
            this.Deactivated += Window_Deactivated;
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDgvItems();
            LoadClient();
            LoadStatus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        #endregion

        #region ── Data Loading ──

        public void LoadClient()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Customers " +
                    "WHERE (type=1 OR type=3) ORDER BY id", conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbClient.ItemsSource = dataTable.DefaultView;
                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        public void LoadStatus()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM PM_Status " +
                    "WHERE IS_Deleted = 0 ORDER BY id", conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbStatus.ItemsSource = dataTable.DefaultView;
                cmbStatus.DisplayMemberPath = "name";
                cmbStatus.SelectedValuePath = "id";
                cmbStatus.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        public void LoadDgvItems()
        {
            try
            {
                dtable.Clear();
                var adapter = new SqlDataAdapter(LoadQry, conn);
                adapter.Fill(dtable);
                dgvItems.ItemsSource = dtable.DefaultView;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Grid Events ──

        private void dgvItems_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                int focusedRow =
                    ((TableView)dgvItems.View).FocusedRowHandle;
                if (focusedRow < 0) return;

                var contractNoValue =
                    dgvItems.GetCellValue(focusedRow, "ContrNo");
                var nameValue =
                    dgvItems.GetCellValue(focusedRow, "name");

                if (contractNoValue != null)
                    ContractNo = Convert.ToInt32(contractNoValue);
                if (nameValue != null)
                    Itemname = nameValue.ToString();

                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Search & Filter Events ──

        private void txtSrchCode_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            try
            {
                if (dtable == null || dtable.Rows.Count == 0) return;

                string searchText = txtSrchCode.Text.Trim();

                if (string.IsNullOrEmpty(searchText))
                {
                    dgvItems.ItemsSource = dtable.DefaultView;
                    return;
                }

                var filteredView = new DataView(dtable)
                {
                    RowFilter = $"ContrNo LIKE '%{searchText}%'"
                };
                dgvItems.ItemsSource = filteredView;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbClient_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbClient.SelectedIndex <= -1) return;

                var selectedValue = cmbClient.SelectedValue;
                if (selectedValue == null) return;

                var filteredView = new DataView(dtable)
                {
                    RowFilter = $"ClientPk = {selectedValue}"
                };
                dgvItems.ItemsSource = filteredView;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbStatus_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbStatus.SelectedIndex <= -1) return;

                var selectedValue = cmbStatus.SelectedValue;
                if (selectedValue == null) return;

                var filteredView = new DataView(dtable)
                {
                    RowFilter = $"statusPk = {selectedValue}"
                };
                dgvItems.ItemsSource = filteredView;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmContractSrchPM {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Button Events ──

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}