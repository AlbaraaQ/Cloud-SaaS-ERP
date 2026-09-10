using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCashCustomer : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;

        private enum SearchType { ByMobile = 1, ByName = 2 }

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCashCustomer()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void frmCashCustomer_Load(object sender, RoutedEventArgs e)
        {
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Insert / Exit ──────────────────────────────

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يجب إدخال الاسم",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMobileNo.Text))
            {
                MessageBox.Show("يجب إدخال رقم الجوال",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMobileNo.Focus();
                return;
            }

            Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Search ─────────────────────────────────────

        private void btnSearchName_Click(object sender, RoutedEventArgs e)
        {
            SearchCustomers(SearchType.ByName);
        }

        private void btnSearchMobile_Click(object sender, RoutedEventArgs e)
        {
            SearchCustomers(SearchType.ByMobile);
        }

        private void txtSearchName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtSearchNane.Text))
                SearchCustomers(SearchType.ByName);
        }

        private void txtSearchMobile_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(txtSearchMobile.Text))
                SearchCustomers(SearchType.ByMobile);
        }

        private void SearchCustomers(SearchType searchType)
        {
            try
            {
                GridControl1.ItemsSource = null;

                string sqlQuery = searchType == SearchType.ByMobile
                    ? "SELECT CashCustomerName, CashCustomerMobile FROM inv " +
                      "WHERE (CashCustomerMobile = @Mobile) AND CashCustomerName <> ''"
                    : "SELECT CashCustomerName, CashCustomerMobile FROM inv " +
                      "WHERE CashCustomerName LIKE '%' + @Name + '%' AND CashCustomerName <> ''";

                SqlCommand sqlCommand = new SqlCommand(sqlQuery, conn);
                sqlCommand.Parameters.Add("@Name", SqlDbType.NVarChar).Value =
                    txtSearchNane.Text.Trim();
                sqlCommand.Parameters.Add("@Mobile", SqlDbType.NVarChar).Value =
                    txtSearchMobile.Text.Trim();

                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    GridControl1.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء البحث: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ── Grid Choose ────────────────────────────────

        private void BtnChoose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button chooseButton &&
                chooseButton.Tag is DataRowView selectedRow)
            {
                string customerName = selectedRow["CashCustomerName"]?.ToString() ?? string.Empty;
                string customerMobile = selectedRow["CashCustomerMobile"]?.ToString() ?? string.Empty;

                txtName.Text = customerName;
                txtMobileNo.Text = customerMobile;

                frmInvPOS posForm = new frmInvPOS();
                posForm.TxtCashCustName.Text = customerName;
                posForm.TxtCashCustMobile.Text = customerMobile;

                Close();
            }
        }

        #endregion
    }
}