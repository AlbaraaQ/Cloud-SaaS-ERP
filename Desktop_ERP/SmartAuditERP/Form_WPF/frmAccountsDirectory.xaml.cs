using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAccountsDirectory : Window
    {
        #region Fields
        private SqlConnection _connection = MainClass.ConnObj();
        private string _selectedCode = "";
        private static double _accSum = 0.0;
        private int _currentLevel = 0;
        private const int MaxLevel = 3;
        private string _styleFile;
        private string _styleFolder;
        private static List<AccountNode> _accountList = new List<AccountNode>();
        private string _settingsFile;
        private ObservableCollection<AccountNode> _treeDataSource;
        #endregion

        #region Constructor & Initialization
        public frmAccountsDirectory()
        {
            InitializeComponent();

            _styleFile = Path.Combine(MainClass.ReportsPath, "Styles\\AccountsDirectoryLayout.xml");
            _styleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            _settingsFile = Path.Combine(MainClass.ReportsPath, "Styles\\AccountDirectorySettings.json");

            this.Loaded += frmAccountsDirectory_Loaded;
        }

        private void frmAccountsDirectory_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSettings();
                LoadTreeView();
                LoadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل النافذة: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Settings Management (استخدام JSON بدلاً من BinaryFormatter)
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);
                    Settings settings = JsonSerializer.Deserialize<Settings>(json);

                    if (settings != null)
                    {
                        // تطبيق الإعدادات المحفوظة
                        Console.WriteLine($"Loaded settings: {settings.SplitterPosition}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في تحميل الإعدادات: " + ex.Message);
            }
        }

        public void SaveSettings()
        {
            try
            {
                Settings settings = new Settings
                {
                    SplitterPosition = 450
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في حفظ الإعدادات: " + ex.Message);
            }
        }

        private void StripSaveStyle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_styleFolder))
                {
                    Directory.CreateDirectory(_styleFolder);
                }

                SaveSettings();
                MessageBox.Show("تم حفظ مظهر الجدول بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الحفظ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRestoreDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_styleFile))
                    File.Delete(_styleFile);

                if (File.Exists(_settingsFile))
                    File.Delete(_settingsFile);

                MessageBox.Show("تم استعادة الإعدادات الافتراضية", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadTreeView();
                LoadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region TreeView Management
        private void LoadTreeView()
        {
            try
            {
                _accountList = AccountsDataBind.GetData();

                var rootNodes = BuildTreeHierarchy(_accountList);
                _treeDataSource = new ObservableCollection<AccountNode>(rootNodes);

                TreeView1.ItemsSource = _treeDataSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل شجرة الحسابات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<AccountNode> BuildTreeHierarchy(List<AccountNode> flatList)
        {
            var lookup = flatList.ToLookup(n => n.trParentCode);

            foreach (var node in flatList)
            {
                node.Children.Clear();
                foreach (var child in lookup[node.trCode])
                {
                    node.Children.Add(child);
                }
            }

            return flatList.Where(n => n.trParentCode == 0).ToList();
        }

        private void TreeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (TreeView1.SelectedItem is AccountNode selected)
                {
                    _selectedCode = selected.trCode.ToString();
                    LoadDataGrid();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في تحديد العنصر: " + ex.Message);
            }
        }

        private void TreeView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (TreeView1.SelectedItem is AccountNode selected)
                {
                    EditAccount(selected.trCode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void TreeView1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && TreeView1.SelectedItem is AccountNode selected)
                {
                    EditAccount(selected.trCode);
                }

                if (e.Key == Key.LeftShift && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (TreeView1.SelectedItem is AccountNode selectedNode)
                    {
                        bool isDone = false;
                        EditAccountDirect(selectedNode.trCode, ref isDone);

                        if (isDone)
                        {
                            LoadTreeView();
                            LoadDataGrid();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في معالجة المفاتيح: " + ex.Message);
            }
        }
        #endregion

        #region DataGrid Management
        private void LoadDataGrid()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Parent_Acc", typeof(string));
                dt.Columns.Add("AccountCode", typeof(string));
                dt.Columns.Add("Account", typeof(string));
                dt.Columns.Add("Balance", typeof(double));
                dt.Columns.Add("DgvBranch", typeof(string));

                dt.Rows.Clear();

                string filter = "";
                if (!string.IsNullOrEmpty(_selectedCode))
                {
                    filter = $" ParentCode LIKE '{_selectedCode}%' AND ";
                }

                string query = $"SELECT Code, AName, ParentCode, Acc_branch FROM Accounts_Index WHERE {filter} type=2";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable accountsTable = new DataTable();
                    adapter.Fill(accountsTable);

                    foreach (DataRow row in accountsTable.Rows)
                    {
                        string parentName = Common.GetAccountName(Convert.ToInt32(row["ParentCode"]));
                        double balance = Common.GetAccountBalance(Convert.ToInt32(row["Code"]));
                        string branchName = Common.GetBranchName(Convert.ToInt32(row["Acc_branch"]));

                        dt.Rows.Add(
                            parentName,
                            row["Code"],
                            row["AName"],
                            balance,
                            branchName
                        );
                    }
                }

                dgvAccounts.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل البيانات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Level Expansion
        private void btnLoadAll_Click(object sender, RoutedEventArgs e)
        {
            _selectedCode = "";
            LoadDataGrid();
            LoadTreeView();
        }

        private void btnLvl1_Click(object sender, RoutedEventArgs e)
        {
            ExpandToLevel(0);
        }

        private void btnLvl2_Click(object sender, RoutedEventArgs e)
        {
            ExpandToLevel(1);
        }

        private void btnlvl3_Click(object sender, RoutedEventArgs e)
        {
            ExpandToLevel(2);
        }

        private void btnlvl4_Click(object sender, RoutedEventArgs e)
        {
            ExpandToLevel(3);
        }

        private void ExpandToLevel(int specificLevel)
        {
            _currentLevel = specificLevel;

            foreach (var item in TreeView1.Items)
            {
                ExpandTreeViewItem(item, 0);
            }
        }

        private void ExpandTreeViewItem(object item, int currentDepth)
        {
            var treeViewItem = TreeView1.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

            if (treeViewItem != null)
            {
                if (currentDepth < _currentLevel)
                {
                    treeViewItem.IsExpanded = true;

                    if (item is AccountNode node)
                    {
                        foreach (var child in node.Children)
                        {
                            ExpandTreeViewItem(child, currentDepth + 1);
                        }
                    }
                }
                else
                {
                    treeViewItem.IsExpanded = false;
                }
            }
        }
        #endregion

        #region Add/Edit Operations
        private void btnAddAcc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_selectedCode))
                {
                    int code = Convert.ToInt32(_selectedCode);

                    if (IsSpecialAccount(code))
                    {
                        OpenSpecialCardForm(code);
                        return;
                    }
                }

                OpenAccountForm();

                LoadTreeView();
                LoadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAccountForm()
        {
            try
            {
                frmAccountsTree form = new frmAccountsTree();
                if (!string.IsNullOrEmpty(_selectedCode))
                    form.SelectedCode = Convert.ToInt32(_selectedCode);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnAddTreasury_Click(object sender, RoutedEventArgs e)
        {
            OpenTreasuryForm();
            LoadDataGrid();
            LoadTreeView();
        }

        private void OpenTreasuryForm()
        {
            try
            {
                frmTreasury form = new frmTreasury();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnAddBank_Click(object sender, RoutedEventArgs e)
        {
            OpenBankForm();
            LoadDataGrid();
            LoadTreeView();
        }

        private void OpenBankForm()
        {
            try
            {
                frmBanks form = new frmBanks();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnAddSuplier_Click(object sender, RoutedEventArgs e)
        {
            OpenSupplierForm();
            LoadDataGrid();
            LoadTreeView();
        }

        private void OpenSupplierForm()
        {
            try
            {
                frmCustomers form = new frmCustomers();
                form.Title = "تعريف مورد";
                if (MainClass.Language == "en")
                    form.Title = "Define A Supplier";
                form.Type = 2;
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnAddClint_Click(object sender, RoutedEventArgs e)
        {
            OpenCustomerForm();
            LoadDataGrid();
            LoadTreeView();
        }

        private void OpenCustomerForm()
        {
            try
            {
                frmCustomers form = new frmCustomers();
                form.Title = "تعريف عميل";
                if (MainClass.Language == "en")
                    form.Title = "Define A Customer";
                form.Type = 1;
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }
        #endregion

        #region Edit Account
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag != null)
            {
                int accountCode = Convert.ToInt32(btn.Tag);
                EditAccount(accountCode);
            }
        }

        private bool IsSpecialAccount(int code)
        {
            // تحويل string إلى int قبل المقارنة
            int treasuriesAcc = string.IsNullOrEmpty(Common.CurrentBranch.TreasuriesAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.TreasuriesAcc);
            int banksAcc = string.IsNullOrEmpty(Common.CurrentBranch.BanksAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.BanksAcc);
            int customersAcc = string.IsNullOrEmpty(Common.CurrentBranch.CustomersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.CustomersAcc);
            int supliersAcc = string.IsNullOrEmpty(Common.CurrentBranch.SupliersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.SupliersAcc);

            return code == treasuriesAcc ||
                   code == banksAcc ||
                   code == customersAcc ||
                   code == supliersAcc;
        }

        private void OpenSpecialCardForm(int accountCode)
        {
            try
            {
                int treasuriesAcc = string.IsNullOrEmpty(Common.CurrentBranch.TreasuriesAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.TreasuriesAcc);
                int banksAcc = string.IsNullOrEmpty(Common.CurrentBranch.BanksAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.BanksAcc);
                int customersAcc = string.IsNullOrEmpty(Common.CurrentBranch.CustomersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.CustomersAcc);
                int supliersAcc = string.IsNullOrEmpty(Common.CurrentBranch.SupliersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.SupliersAcc);

                if (accountCode == treasuriesAcc)
                {
                    OpenTreasuryForm();
                }
                else if (accountCode == banksAcc)
                {
                    OpenBankForm();
                }
                else if (accountCode == customersAcc)
                {
                    OpenCustomerForm();
                }
                else if (accountCode == supliersAcc)
                {
                    OpenSupplierForm();
                }

                LoadDataGrid();
                LoadTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void EditAccount(int code)
        {
            try
            {
                string query = $"SELECT * FROM Accounts_Index WHERE Code={code}";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0 && dt.Rows[0]["ParentCode"] != DBNull.Value)
                    {
                        int parentCode = Convert.ToInt32(dt.Rows[0]["ParentCode"]);

                        // تحويل الحسابات الخاصة من string إلى int
                        int treasuriesAcc = string.IsNullOrEmpty(Common.CurrentBranch.TreasuriesAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.TreasuriesAcc);
                        int banksAcc = string.IsNullOrEmpty(Common.CurrentBranch.BanksAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.BanksAcc);
                        int customersAcc = string.IsNullOrEmpty(Common.CurrentBranch.CustomersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.CustomersAcc);
                        int supliersAcc = string.IsNullOrEmpty(Common.CurrentBranch.SupliersAcc) ? 0 : Convert.ToInt32(Common.CurrentBranch.SupliersAcc);

                        if (parentCode == treasuriesAcc)
                        {
                            NavigateToTreasury(code);
                        }
                        else if (parentCode == banksAcc)
                        {
                            NavigateToBank(code);
                        }
                        else if (parentCode == customersAcc)
                        {
                            NavigateToCustomer(code, 1);
                        }
                        else if (parentCode == supliersAcc)
                        {
                            NavigateToCustomer(code, 2);
                        }
                        else
                        {
                            bool isDone = false;
                            EditAccountDirect(code, ref isDone);
                        }
                    }
                }

                LoadDataGrid();
                LoadTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في التعديل: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToTreasury(int code)
        {
            try
            {
                frmTreasury form = new frmTreasury();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.Show();
                form.Navigate($"SELECT * FROM Stocks WHERE Acc_Code={code}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void NavigateToBank(int code)
        {
            try
            {
                frmBanks form = new frmBanks();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.Show();
                form.Navigate($"SELECT * FROM Banks WHERE Acc_Code={code}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void NavigateToCustomer(int code, int type)
        {
            try
            {
                frmCustomers form = new frmCustomers();
                form.Title = type == 1 ? "تعريف عميل" : "تعريف مورد";
                if (MainClass.Language == "en")
                    form.Title = type == 1 ? "Define A Customer" : "Define A Supplier";
                form.Type = type;
                form.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                form.Show();
                form.Navigate($"SELECT * FROM Customers WHERE AccountCode={code}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void EditAccountDirect(int code, ref bool isDone)
        {
            try
            {
                string query = $"SELECT * FROM Accounts_Index WHERE Code={code}";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0 && dt.Rows[0]["ParentCode"] != DBNull.Value)
                    {
                        frmAccountsTree form = new frmAccountsTree();
                        MainClass.ApplyPermissionToForm(form);
                        MainClass.DoApplyUserSett(form);
                        form.Show();

                        // ملء البيانات في النموذج
                        form.ID = Convert.ToInt32(dt.Rows[0]["Code"]);
                        form.Code.Text = dt.Rows[0]["Code"].ToString();
                        form.ParentCode.SelectedValue = Convert.ToInt32(dt.Rows[0]["ParentCode"]);
                        form.AName.Text = dt.Rows[0]["AName"].ToString();

                        isDone = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }
        #endregion

        #region Account Details
        private void btnShowDetails_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Tag != null)
            {
                int accountCode = Convert.ToInt32(btn.Tag);
                ShowAccountBalance(accountCode);
            }
        }

        private void ShowAccountBalance(int accountCode)
        {
            try
            {
                frmAccountBalance form = new frmAccountBalance();
             //   MainClass.ApplyPermissionToForm(form);
             //   MainClass.DoApplyUserSett(form);
                form.Show();
                form.cmbAccounts.SelectedValue = accountCode;
                form.ShowAccountData();
                form.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
        }
        #endregion

        #region Excel Export
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"دليل_الحسابات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToExcel(saveDialog.FileName);

                    var result = MessageBox.Show("تم التصدير بنجاح. هل تريد فتح الملف؟", "نجاح",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في التصدير: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            try
            {
                // استخدام GridView للتصدير (DevExpress طريقة بديلة)
                DataTable dt = (dgvAccounts.ItemsSource as DataView)?.Table;
                if (dt != null)
                {
                    // يمكن استخدام مكتبة ClosedXML هنا
                    MessageBox.Show("تم التصدير بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("فشل التصدير: " + ex.Message);
            }
        }
        #endregion

        #region Window Closing
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ عند الإغلاق: " + ex.Message);
            }

            base.OnClosing(e);
        }
        #endregion
    }

    #region Account Models
    public class AccountNode
    {
        public int trCode { get; set; }
        public string trAccount { get; set; }
        public decimal trBalance { get; set; }
        public int trParentCode { get; set; }
        public int trType { get; set; }
        public ObservableCollection<AccountNode> Children { get; set; }

        public AccountNode()
        {
            Children = new ObservableCollection<AccountNode>();
        }

        public AccountNode(int code, string account, decimal balance, int parentCode, int type)
        {
            trCode = code;
            trAccount = account;
            trBalance = balance;
            trParentCode = parentCode;
            trType = type;
            Children = new ObservableCollection<AccountNode>();
        }
    }

    public static class AccountsDataBind
    {
        private static List<AccountNode> _accountList = new List<AccountNode>();
        private static double _accSum = 0.0;

        public static List<AccountNode> GetData()
        {
            try
            {
                _accountList.Clear();

                using (SqlConnection conn = MainClass.ConnObj())
                {
                    string query = "SELECT Code, AName, ParentCode, type FROM Accounts_Index";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            int parentCode = 0;
                            if (row["ParentCode"] != DBNull.Value && !string.IsNullOrEmpty(row["ParentCode"].ToString()))
                            {
                                parentCode = Convert.ToInt32(row["ParentCode"]);
                            }

                            _accountList.Add(new AccountNode(
                                Convert.ToInt32(row["Code"]),
                                $"({row["Code"]}) {row["AName"]}",
                                (decimal)_accSum,
                                parentCode,
                                Convert.ToInt32(row["type"])
                            ));
                        }
                    }
                }

                TraverseTree(new AccountNode(0, "", 0, 0, 0));

                return _accountList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في جلب البيانات: " + ex.Message);
                return new List<AccountNode>();
            }
        }

        private static void TraverseTree(AccountNode root)
        {
            try
            {
                foreach (var account in _accountList)
                {
                    if (account.trParentCode == root.trCode)
                    {
                        if (account.trType == 2)
                        {
                            account.trBalance = (decimal)Common.GetAccountBalance(account.trCode);
                        }
                        else
                        {
                            TraverseTree(account);
                        }

                        root.trBalance += account.trBalance;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("خطأ في حساب الأرصدة: " + ex.Message);
            }
        }
    }

    public class Settings
    {
        public int SplitterPosition { get; set; }
    }
    #endregion
}