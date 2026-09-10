using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Ws_Auditor;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmmanagementSync : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        #endregion

        #region Constructor

        public frmmanagementSync()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void Form1_Load(object sender, RoutedEventArgs e)
        {
            LoadBranch();
        }

        private void LoadBranch()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from Branches where IS_Deleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbBranch.ItemsSource   = dt.DefaultView;
                cmbBranch.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region CheckBox

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbBranch.IsEnabled = chkAll.IsChecked != true;
        }

        #endregion

        #region Upload

        private async void btnUploadData_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (DXMessageBox.Show("هل تريد مزامنة بيانات التعاريف؟", "",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await PostCardDataOnline();
            }
        }

        public async Task<bool> PostCardDataOnline()
        {
            try
            {
                if (!Sync.ValidAPIUrl) return false;

                EntityOperations entityOperations = new EntityOperations();

                if (ckInventories.IsChecked == true)
                {
                    InvertoryCRUD invertoryCRUD = new InvertoryCRUD(Sync.APIUrl);
                    List<Invertory> inventories = entityOperations.ReadInvertory();
                    if (inventories.Count > 0)
                        await invertoryCRUD.AddInvertory(inventories);
                }

                if (ckBranches.IsChecked == true)
                {
                    BranchCRUD branchCRUD = new BranchCRUD(Sync.APIUrl);
                    List<Branch> branches = entityOperations.ReadBranch();
                    if (branches.Count > 0)
                        await branchCRUD.AddBranch(branches);
                }

                if (ckclients.IsChecked == true)
                {
                    CustomerCRUD customerCRUD = new CustomerCRUD(Sync.APIUrl);
                    List<global::AuditorAPI.Models.Customer> customers = entityOperations.ReadCustomer();
                    if (customers.Count > 0)
                        await customerCRUD.AddCustomer(customers);
                }

                if (ckEmp.IsChecked == true)
                {
                    EmployeeCRUD employeeCRUD = new EmployeeCRUD(Sync.APIUrl);
                    List<Employee> employees = entityOperations.ReadEmployee();
                    if (employees.Count > 0)
                        await employeeCRUD.AddImployee(employees);
                }

                if (ckaccounts.IsChecked == true)
                {
                    AccountCRUD accountCRUD = new AccountCRUD(Sync.APIUrl);
                    List<TreeAccount> accounts = entityOperations.ReadAccounts();
                    if (accounts.Count > 0)
                        await accountCRUD.AddTreeAccount(accounts);
                }

                if (ckboxes.IsChecked == true)
                {
                    TreasuryCRUD treasuryCRUD = new TreasuryCRUD(Sync.APIUrl);
                    List<global::AuditorAPI.Models.Treasury> treasuries = entityOperations.ReadTreasuries();
                    if (treasuries.Count > 0)
                        await treasuryCRUD.AddTreasury(treasuries);
                }

                if (ckbanks.IsChecked == true)
                {
                    BankCRUD bankCRUD = new BankCRUD(Sync.APIUrl);
                    List<global::AuditorAPI.Models.Bank> banks = entityOperations.ReadBanks();
                    if (banks.Count > 0)
                        await bankCRUD.AddBank(banks);
                }

                if (ckitems.IsChecked == true)
                    entityOperations.PostProducts();

                ItemOper itemOper = new ItemOper();

                if (ckgroups.IsChecked == true)
                {
                    List<Category> categories = itemOper.BindToCategory();
                    if (await new CategoryCRUD(Sync.APIUrl).PostCategoriesManully(categories))
                        DXMessageBox.Show("تمت مزامنة المجموعات بنجاح", "",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (ckunits.IsChecked == true)
                {
                    List<Unit> units = itemOper.BindToUnit();
                    if (Sync.ValidAPIUrl && await new UnitCRUD(Sync.APIUrl).PostUnitsManully(units))
                        DXMessageBox.Show("تمت مزامنة الوحدات بنجاح", "",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (ChkUser.IsChecked == true)
                {
                    List<Userapi> users = itemOper.BindToUser();
                    if (Sync.ValidAPIUrl && await new UserCRUD(Sync.APIUrl).PostUsersManully(users))
                        DXMessageBox.Show("تمت مزامنة المستخدمين بنجاح", "",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (chksettingpos.IsChecked == true)
                {
                    List<SettingGeneralapi> settingGenerals = itemOper.BindToSettingGeneral();
                    if (Sync.ValidAPIUrl && await new SettingGeneral(Sync.APIUrl).PostSettingGeneralsManully(settingGenerals))
                        DXMessageBox.Show("تمت مزامنة الإعدادات بنجاح", "",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (chkFoundation.IsChecked == true)
                {
                    Foundationapi foundation = itemOper.BindToFoundation();
                    if (foundation != null)
                    {
                        if (Sync.ValidAPIUrl && await new CompanyCRUD(Sync.APIUrl).PostFoundManully(foundation))
                            DXMessageBox.Show("تمت مزامنة بيانات المنشأة بنجاح", "",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        DXMessageBox.Show("⚠ لا توجد بيانات منشأة في الجدول المحلي", "",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                await Sync.SendLastCodeToApiAsync(1, Sync.ClientCode);
                DXMessageBox.Show("تم رفع البيانات بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return false;
            }
        }

        #endregion

        #region Download

        private async void btnDownloadData_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isValidApi = await Sync.ISValidAPIUrlAsync();

                if (!isValidApi || Sync.SyncType != 1) return;

                if (Sync.BranchType != 4)
                {
                    if (DXMessageBox.Show("هل تريد تحديث بيانات التعاريف عبر نظام المزامنة؟", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await ReadCardsDataOnline();
                    }
                }
                else
                {
                    await DownloadFromCloud();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task DownloadFromCloud()
        {
            EntityOperations entityOperations = new EntityOperations();
            ItemOper itemOper = new ItemOper();

            if (ckitems.IsChecked == true)
            {
                ClsItem clsItem = new ClsItem(Sync.APIUrl, "");
                List<Product> products = new List<Product>();

                if (User.CurrentCloudUser != null)
                    products.AddRange(clsItem.GetItems(User.CurrentCloudUser.UserID, MainClass.BranchNo, BranchSpecialSalePrice: true));
                else
                    DXMessageBox.Show("الرجاء الدخول بمستخدم آخر");

                if (new EntityOperations().SaveProducts(products))
                    DXMessageBox.Show("تم تحميل الأصناف", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (ckgroups.IsChecked == true)
            {
                ClsCategory clsCategory = new ClsCategory(Sync.APIUrl, "");
                List<Category> categories = new List<Category>();
                categories.AddRange(clsCategory.GetCategories());

                if (new EntityOperations().SaveCategories(categories))
                    DXMessageBox.Show("تم تحميل المجموعات", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (ckclients.IsChecked == true)
            {
                ClsCustomer clsCustomer = new ClsCustomer(Sync.APIUrl, "");
                List<global::AuditorAPI.Models.Customer> customers = new List<global::AuditorAPI.Models.Customer>();
                customers.AddRange(clsCustomer.GetCustomers(MainClass.BranchNo));

                if (new EntityOperations().SaveCustomer(customers, AddedLocally: false))
                    DXMessageBox.Show("تم تحميل العملاء", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (ckBranches.IsChecked == true)
            {
                ClsBranch clsBranch = new ClsBranch(Sync.APIUrl, "");
                List<Branch> branches = new List<Branch>();
                branches.AddRange(clsBranch.GetBranches());

                if (branches.Count > 0 && entityOperations.SaveBranch(branches))
                    DXMessageBox.Show("تم تحميل الفروع", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (ckunits.IsChecked == true)
                itemOper.ReadUnitsOnline();
        }

        public async Task<bool> ReadCardsDataOnline()
        {
            if (!Sync.ValidAPIUrl) return false;

            EntityOperations entityOperations = new EntityOperations();
            ItemOper itemOper = new ItemOper();

            if (ckBranches.IsChecked == true)
            {
                BranchCRUD branchCRUD = new BranchCRUD(Sync.APIUrl);
                List<Branch> branches = (List<Branch>)await branchCRUD.GetBranches(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (branches.Count > 0)
                    entityOperations.SaveBranch(branches);
            }

            if (ckInventories.IsChecked == true)
            {
                InvertoryCRUD invertoryCRUD = new InvertoryCRUD(Sync.APIUrl);
                List<Invertory> inventories = (List<Invertory>)await invertoryCRUD.GetInvertories(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (inventories.Count > 0)
                    entityOperations.SaveInvertory(inventories, Addlocally: false);
            }

            if (ckEmp.IsChecked == true)
            {
                EmployeeCRUD employeeCRUD = new EmployeeCRUD(Sync.APIUrl);
                BranchCRUD branchCRUD2 = new BranchCRUD(Sync.APIUrl);
                List<Employee> employees = (List<Employee>)await employeeCRUD.GetEmployees(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                List<BranchEmployee> branchEmployees = (List<BranchEmployee>)await branchCRUD2.GetBranchEmployee(Sync.ClientCode);
                if (employees.Count > 0)
                    entityOperations.SaveEmployee(employees, AddedLocally: false, branchEmployees);
            }

            if (ckclients.IsChecked == true)
            {
                CustomerCRUD customerCRUD = new CustomerCRUD(Sync.APIUrl);
                List<global::AuditorAPI.Models.Customer> customers = (List<global::AuditorAPI.Models.Customer>)await customerCRUD.GetCustomers(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (customers.Count > 0)
                    entityOperations.SaveCustomer(customers, AddedLocally: false);
            }

            if (ckaccounts.IsChecked == true)
            {
                AccountCRUD accountCRUD = new AccountCRUD(Sync.APIUrl);
                List<TreeAccount> accounts = (List<TreeAccount>)await accountCRUD.GetTreeAccounts(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (accounts.Count > 0)
                    entityOperations.SaveAccounts(accounts);
            }

            if (ckboxes.IsChecked == true)
            {
                TreasuryCRUD treasuryCRUD = new TreasuryCRUD(Sync.APIUrl);
                List<global::AuditorAPI.Models.Treasury> treasuries = (List<global::AuditorAPI.Models.Treasury>)await treasuryCRUD.GetTreasurys(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (treasuries.Count > 0)
                    entityOperations.SaveTreasuries(treasuries, AddedLocally: false);
            }

            if (ckbanks.IsChecked == true)
            {
                BankCRUD bankCRUD = new BankCRUD(Sync.APIUrl);
                List<global::AuditorAPI.Models.Bank> banks = (List<global::AuditorAPI.Models.Bank>)await bankCRUD.GetBanks(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);
                if (banks.Count > 0)
                    entityOperations.SaveBanks(banks, AddedLocally: false);
            }

            if (ckunits.IsChecked == true)    itemOper.ReadUnitsOnline();
            if (ckgroups.IsChecked == true)   itemOper.ReadCategoriesOnline();
            if (ckitems.IsChecked == true)    itemOper.ReadProductsOnline();

            DXMessageBox.Show("تم تحميل البيانات بنجاح", "",
                MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        #endregion

        #region Helpers

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}