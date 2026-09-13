using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAccountSrch : Window
    {
        #region Fields
        private SqlConnection _connection = MainClass.ConnObj();
        private string _condition = "";
        private string _loadQuery = "";
        private DataTable _dataTable = new DataTable();
        private DataView _dataView;

        // Public Properties (للتوافق مع الكود الأصلي)
        public int Code { get; set; } = -1;
        public string Itemname { get; set; } = "";
        public bool IsDone { get; set; } = false;
        public string cond
        {
            get => _condition;
            set => _condition = value;
        }
        #endregion

        #region Constructor & Load
        public frmAccountSrch()
        {
            InitializeComponent();
            this.Loaded += frmAccountSrch_Loaded;
        }

        private void frmAccountSrch_Loaded(object sender, RoutedEventArgs e)
        {
            BuildQuery();
            LoadMainAccounts();
            LoadDataGrid();
            txtSrchNm.Focus();
        }

        private void BuildQuery()
        {
            if (MainClass.EmpNo == 0)
            {
                _loadQuery = $"SELECT Code, AName, ParentCode FROM Accounts_Index WHERE Type=2 {_condition} ORDER BY Code";
            }
            else
            {
                _loadQuery = $"SELECT Code, AName, ParentCode FROM Accounts_Index WHERE Type=2 {_condition} {Accounting.BranchCondition} ORDER BY Code";
            }
        }
        #endregion

        #region Data Loading
        private void LoadMainAccounts()
        {
            try
            {
                string query = "SELECT Code, AName FROM Accounts_Index WHERE type=1 ORDER BY Code";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbMainAccount.ItemsSource = dt.DefaultView;
                    cmbMainAccount.DisplayMemberPath = "AName";
                    cmbMainAccount.SelectedValuePath = "Code";
                    cmbMainAccount.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الحسابات الرئيسية: " + ex.Message);
            }
        }

        private void LoadDataGrid()
        {
            try
            {
                _dataTable.Clear();

                using (SqlDataAdapter adapter = new SqlDataAdapter(_loadQuery, _connection))
                {
                    adapter.Fill(_dataTable);
                }

                _dataView = new DataView(_dataTable);
                dgvAccounts.ItemsSource = _dataView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
        }
        #endregion

        #region Search & Filter
        private void txtSrchNm_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_dataView != null)
                {
                    _dataView.RowFilter = $"AName LIKE '%{txtSrchNm.Text}%'";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("خطأ في البحث: " + ex.Message);
            }
        }

        private void txtSrchCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_dataView != null && !string.IsNullOrEmpty(txtSrchCode.Text))
                {
                    _dataView.RowFilter = $"Code = {txtSrchCode.Text}";
                }
                else if (_dataView != null)
                {
                    _dataView.RowFilter = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("خطأ في البحث بالكود: " + ex.Message);
            }
        }

        private void cmbMainAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbMainAccount.SelectedIndex > -1 && _dataView != null)
                {
                    _dataView.RowFilter = $"ParentCode = {cmbMainAccount.SelectedValue}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("خطأ في الفلترة: " + ex.Message);
            }
        }
        #endregion

        #region DataGrid Events
        private void dgvAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvAccounts.SelectedItem is DataRowView row)
                {
                    Code = Convert.ToInt32(row["Code"]);
                    Itemname = row["AName"].ToString();

                    UpdateBalance();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void dgvAccounts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSelection();
        }

        private void dgvAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmSelection();
            }
            else if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void ConfirmSelection()
        {
            try
            {
                if (dgvAccounts.SelectedItem is DataRowView row)
                {
                    Code = Convert.ToInt32(row["Code"]);
                    Itemname = row["AName"].ToString();
                    IsDone = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void UpdateBalance()
        {
            try
            {
                double balance = Common.GetAccountBalance(Code);

                if (balance >= 0)
                {
                    txtBalance.Text = balance.ToString("N2");
                    txtNature.Text = "دائن";
                    txtNature.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtBalance.Text = Math.Abs(balance).ToString("N2");
                    txtNature.Text = "مدين";
                    txtNature.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("خطأ في حساب الرصيد: " + ex.Message);
            }
        }
        #endregion

        #region Keyboard Navigation
        private void txtSrchNm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                dgvAccounts.Focus();
                if (dgvAccounts.Items.Count > 0)
                {
                    dgvAccounts.SelectedIndex = 0;
                }
            }
        }

        private void txtSrchCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                dgvAccounts.Focus();
                if (dgvAccounts.Items.Count > 0)
                {
                    dgvAccounts.SelectedIndex = 0;
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
        #endregion

        #region Buttons
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAddNewAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmAccountsTree form = new frmAccountsTree();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.ShowDialog();

                // إعادة تحميل البيانات بعد الإضافة
                LoadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }
        #endregion
    }
}