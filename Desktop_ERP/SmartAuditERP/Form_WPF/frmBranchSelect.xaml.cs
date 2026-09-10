using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmBranchSelect : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;
        Home Home = new Home();

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmBranchSelect()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void frmBranchSelect_Load(object sender, RoutedEventArgs e)
        {
            LoadBranches();

            if (cmbBranches.Items.Count > 0)
            {
                cmbBranches.SelectedIndex = 0;
            }

            chkAll_CheckedChanged(chkAll, new RoutedEventArgs());
        }

        private void frmBranchSelect_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F4 || e.SystemKey == Key.F4)
                {
                    e.Handled = true;
                }
            }
            catch
            {
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void WindowCloseIcon_Click(object sender, RoutedEventArgs e)
        {
            btnClose_Click(sender, e);
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        public void LoadBranches()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter =
                    new SqlDataAdapter("select id,name from Branches order by id", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                cmbBranches.ItemsSource = dataTable.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطأ أثناء تحميل الفروع" + Environment.NewLine + ex.Message,
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region ── Control Events ─────────────────────────────

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isAllSelected = chkAll.IsChecked == true;

                cmbBranches.IsEnabled = !isAllSelected;

                if (isAllSelected)
                {
                    cmbBranches.SelectedIndex = -1;
                }
                else if (cmbBranches.Items.Count > 0 && cmbBranches.SelectedIndex == -1)
                {
                    cmbBranches.SelectedIndex = 0;
                }
            }
            catch
            {
            }
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chkAll.IsChecked != true && cmbBranches.SelectedValue == null)
                {
                    MessageBox.Show(
                        "اختر فرع أو الكل",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    cmbBranches.Focus();
                    return;
                }

                string branchText = "الكل";
                int branchNo = -1;

                if (chkAll.IsChecked != true)
                {
                    branchText = cmbBranches.Text;
                    branchNo = Convert.ToInt32(cmbBranches.SelectedValue);
                }
                var Home = new Home();
                Home.lblBranch1.Text = "الفرع: " + branchText;
                MainClass.BranchNo = branchNo;
                Home.Activate();

                Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "حدث خطأ أثناء اختيار الفرع" + Environment.NewLine + ex.Message,
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Home.Close();
            }
            catch
            {
                Close();
            }
        }

        #endregion
    }
}