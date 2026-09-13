using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRecyclingData : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private int    _currentPage = 1;
        private int    _totalPages  = 8;
        private string _toDbName    = "";

        // Page names for wizard steps
        private readonly string[] _stepTitles = new[]
        {
            "مرحباً",
            "إعداد قاعدة البيانات",
            "خيارات التدوير",
            "القيد الافتتاحي",
            "بضاعة أول مدة",
            "تأكيد التدوير",
            "جاري التدوير",
            "اكتمال"
        };

        #endregion

        #region Constructor

        public frmRecyclingData()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtStartdate.DateTime = DateTime.Now;
            txtEnddate.DateTime   = DateTime.Now;
            ShowPage(1);
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Page Navigation

        private void ShowPage(int pageNumber)
        {
            // إخفاء كل الصفحات
            Page1.Visibility          = Visibility.Collapsed;
            CreateDbPage2.Visibility  = Visibility.Collapsed;
            OptionsPage3.Visibility   = Visibility.Collapsed;
            IntialReceiptsPage4.Visibility = Visibility.Collapsed;
            FirstStockPage5.Visibility = Visibility.Collapsed;
            SubmittedPage6.Visibility  = Visibility.Collapsed;
            RecyclingPage.Visibility   = Visibility.Collapsed;
            CompletionPage7.Visibility = Visibility.Collapsed;

            // إظهار الصفحة المطلوبة
            switch (pageNumber)
            {
                case 1: Page1.Visibility          = Visibility.Visible; break;
                case 2: CreateDbPage2.Visibility  = Visibility.Visible; break;
                case 3: OptionsPage3.Visibility   = Visibility.Visible; break;
                case 4: IntialReceiptsPage4.Visibility = Visibility.Visible; break;
                case 5: FirstStockPage5.Visibility = Visibility.Visible; break;
                case 6: SubmittedPage6.Visibility  = Visibility.Visible; break;
                case 7: RecyclingPage.Visibility   = Visibility.Visible; break;
                case 8: CompletionPage7.Visibility = Visibility.Visible; break;
            }

            // تحديث العنوان وأزرار التنقل
            _currentPage = pageNumber;
            lblWizardTitle.Text = $"🔄 {_stepTitles[pageNumber - 1]}";
            lblWizardStep.Text  = $"الخطوة {pageNumber} من {_totalPages}";
            ProgressBarGeneral.Value = pageNumber;
            lblStepIndicator.Text = GetStepIndicator(pageNumber);

            // أزرار التنقل
            btnPrevious.IsEnabled = (pageNumber > 1 && pageNumber != 7 && pageNumber != 8);
            btnNext.Visibility    = (pageNumber < _totalPages - 1) ? Visibility.Visible : Visibility.Collapsed;
            btnFinish.Visibility  = (pageNumber == _totalPages) ? Visibility.Visible : Visibility.Collapsed;

            // صفحة الإنهاء
            if (pageNumber == 8)
            {
                btnCancel.Content = "✖ إغلاق";
                btnNext.Visibility = Visibility.Collapsed;
            }
        }

        private string GetStepIndicator(int current)
        {
            var sb = new StringBuilder();
            for (int i = 1; i <= _totalPages; i++)
                sb.Append(i == current ? "● " : "○ ");
            return sb.ToString().Trim();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentPage()) return;

            if (_currentPage == 6)
            {
                // صفحة التأكيد → التنفيذ
                ShowPage(7);
                _ = ApplyRecyclingAsync();
            }
            else if (_currentPage < _totalPages)
            {
                ShowPage(_currentPage + 1);
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
                ShowPage(_currentPage - 1);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnFinish_Click(object sender, RoutedEventArgs e) => this.Close();

        private bool ValidateCurrentPage()
        {
            if (_currentPage == 2)
            {
                if (string.IsNullOrWhiteSpace(txtDbName.Text))
                {
                    DXMessageBox.Show("يرجى إدخال اسم قاعدة البيانات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDbName.Focus();
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Browse Path

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName   = txtDbName.Text,
                DefaultExt = ".mdf",
                Filter     = "SQL Database Files (*.mdf)|*.mdf"
            };

            if (dlg.ShowDialog() == true)
                txtPath.Text = dlg.FileName.Replace(".mdf", "");
        }

        #endregion

        #region Create Database

        private async Task CreateDatabaseAsync()
        {
            try
            {
                using (var masterConn = new SqlConnection(
                    $"server={MainClass.Server};trusted_connection=true;"))
                {
                    await masterConn.OpenAsync();

                    if (masterConn.State != ConnectionState.Open)
                    {
                        DXMessageBox.Show("السيرفر غير متصل",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtDbName.Text))
                    {
                        DXMessageBox.Show("يرجى إدخال اسم قاعدة البيانات");
                        return;
                    }

                    // التحقق من التكرار
                    using (var da = new SqlDataAdapter(
                        $"SELECT * FROM master.dbo.DatabasesManagment WHERE Dbname='{txtDbName.Text.Trim()}'",
                        masterConn))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            DXMessageBox.Show("اسم قاعدة البيانات تم استخدامه من قبل، يرجى إدخال اسم آخر");
                            return;
                        }
                    }

                    // الحصول على اسم تلقائي فريد
                    string autoDbName = await GetUniqueDbNameAsync(masterConn);
                    _toDbName = autoDbName;

                    // إنشاء قاعدة البيانات
                    await CreateDatabaseOnServerAsync(masterConn, autoDbName);

                    // تطبيق الـ Schema
                    using (var newConn = new SqlConnection(
                        $"server={MainClass.Server};database={autoDbName.Trim()};trusted_connection=true"))
                    {
                        await newConn.OpenAsync();
                        string[] scripts = Properties.Resources.CrystalLiteDB.Split(
                            new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var script in scripts)
                        {
                            if (!string.IsNullOrWhiteSpace(script))
                            {
                                using (var cmd = new SqlCommand(script, newConn))
                                {
                                    try { await cmd.ExecuteNonQueryAsync(); } catch { }
                                }
                            }
                        }

                        // تسجيل في جدول الإدارة
                        using (var da = new SqlDataAdapter(
                            $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoDbName}'",
                            masterConn))
                        {
                            var dt = new DataTable();
                            da.Fill(dt);
                            string dbId = dt.Rows.Count > 0 ? dt.Rows[0]["dbid"].ToString() : "0";

                            using (var regCmd = new SqlCommand(
                                @"INSERT INTO master.dbo.DatabasesManagment
                                  (DbId,DbAutoName,Dbname,CreateTime,UserCreate,
                                   AccountingPeriodStart,AccountingPeriodEnd,
                                   UserName,UseFullName,passward,DefaultCoin,
                                   IsActive,IsDeleted,ImportanceOrder)
                                  VALUES (@DbId,@DbAutoName,@Dbname,@CreateTime,@UserCreate,
                                  @AccountingPeriodStart,@AccountingPeriodEnd,
                                  @UserName,@UseFullName,@passward,@DefaultCoin,
                                  @IsActive,@IsDeleted,@ImportanceOrder)",
                                newConn))
                            {
                                regCmd.Parameters.Add("@DbId",                 SqlDbType.Int).Value      = Convert.ToInt32(dbId);
                                regCmd.Parameters.Add("@DbAutoName",           SqlDbType.NVarChar).Value = autoDbName;
                                regCmd.Parameters.Add("@Dbname",               SqlDbType.NVarChar).Value = txtDbName.Text;
                                regCmd.Parameters.Add("@CreateTime",           SqlDbType.DateTime).Value = DateTime.Now;
                                regCmd.Parameters.Add("@UserCreate",           SqlDbType.Int).Value      = MainClass.EmpNo;
                                regCmd.Parameters.Add("@AccountingPeriodStart",SqlDbType.DateTime).Value = txtStartdate.DateTime;
                                regCmd.Parameters.Add("@AccountingPeriodEnd",  SqlDbType.DateTime).Value = txtEnddate.DateTime;
                                regCmd.Parameters.Add("@UserName",             SqlDbType.NVarChar).Value = "1";
                                regCmd.Parameters.Add("@UseFullName",          SqlDbType.NVarChar).Value = "1";
                                regCmd.Parameters.Add("@passward",             SqlDbType.NVarChar).Value = "-1";
                                regCmd.Parameters.Add("@DefaultCoin",          SqlDbType.NVarChar).Value = "";
                                regCmd.Parameters.Add("@IsActive",             SqlDbType.Bit).Value      = 1;
                                regCmd.Parameters.Add("@IsDeleted",            SqlDbType.Float).Value    = 0;
                                regCmd.Parameters.Add("@ImportanceOrder",      SqlDbType.Int).Value      = 1;
                                await regCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    UpdateProgress(10);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في إنشاء قاعدة البيانات: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GetUniqueDbNameAsync(SqlConnection conn)
        {
            string autoName;
            using (var da = new SqlDataAdapter(
                "SELECT MAX(dbid) FROM master.dbo.sysdatabases", conn))
            {
                var dt = new DataTable();
                da.Fill(dt);
                int nextId = Convert.ToInt32(dt.Rows[0][0]) + 1;
                autoName = "Data" + nextId;
            }

            bool isUnique = false;
            while (!isUnique)
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoName}'",
                    conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 0)
                        isUnique = true;
                    else
                    {
                        int num = Convert.ToInt32(autoName.Replace("Data", "")) + 1;
                        autoName = "Data" + num;
                    }
                }
            }

            return autoName;
        }

        private async Task CreateDatabaseOnServerAsync(SqlConnection conn, string dbName)
        {
            string sql = $"CREATE DATABASE {dbName.Trim()}";

            if (!string.IsNullOrWhiteSpace(txtPath.Text))
            {
                sql += $" CONTAINMENT = NONE ON PRIMARY " +
                       $"( NAME = N'{dbName.Trim()}', " +
                       $"FILENAME = N'{txtPath.Text}.mdf', " +
                       $"SIZE = 9280KB, MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB ) " +
                       $"LOG ON ( NAME = N'{dbName.Trim()}_log', " +
                       $"FILENAME = N'{txtPath.Text}.ldf', " +
                       $"SIZE = 3904KB, MAXSIZE = 2048GB, FILEGROWTH = 10%)";
            }

            using (var cmd = new SqlCommand(sql, conn))
                await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Apply Recycling

        private async Task ApplyRecyclingAsync()
        {
            try
            {
                // التحقق من كلمة المرور
                var pwd = new frmCheckPwd { operNo = 100, CheckType = 0 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) { ShowPage(6); return; }

                UpdateProgress(5);

                // إنشاء قاعدة البيانات
                await CreateDatabaseAsync();

                if (string.IsNullOrEmpty(_toDbName))
                {
                    DXMessageBox.Show("لا يمكن تنفيذ عملية التدوير لعدم تحديد قاعدة البيانات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowPage(6);
                    return;
                }

                EnsureOpen(_conn);

                // تدوير الإعدادات الأساسية
                await CopySettingsTablesAsync();
                UpdateProgress(20);

                // تدوير المواد
                if (ckImportItems.IsChecked == true)
                {
                    await CopyItemsAsync();
                    UpdateProgress(40);
                }

                // تدوير المستخدمين
                if (ckImportUsers.IsChecked == true)
                {
                    await CopyUsersAsync();
                    UpdateProgress(55);
                }

                // تدوير العملاء والموردين
                if (ckcustomers.IsChecked == true)
                {
                    await CopyCustomersAsync();
                    UpdateProgress(70);
                }

                // تدوير الحسابات
                if (ckAccounts.IsChecked == true)
                {
                    await CopyAccountsAsync();
                    UpdateProgress(80);
                }

                // تدوير الفواتير
                if (RePlaceInvoicesInLastPeriod.IsChecked == true)
                {
                    await CopyInvoicesAsync();
                    UpdateProgress(88);
                }

                // توليد فاتورة أول مدة
                if (rbFirstStockInv.IsChecked == true)
                    await RecycleFirstStockAsync();

                UpdateProgress(94);

                // توليد القيد الافتتاحي
                if (rbInitialBalance.IsChecked == true)
                    BindToEntry();

                UpdateProgress(100);

                DXMessageBox.Show("تم تدوير البيانات بنجاح",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                ShowPage(8);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التدوير: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowPage(6);
            }
            finally { CloseConn(_conn); }
        }

        private async Task CopySettingsTablesAsync()
        {
            string[] dropTables = {
                "CustomizedDGV","Departments","Foundation","jobs","Managements",
                "Offer","OfferForClient","OfferItems","OperationPermission",
                "SettingBarcode","SettingCloseShift","SettingDisplay","SettingDisplayCotrl",
                "SettingEmail","SettingFingurePrint","SettingGeneral","SettingOrderMethods",
                "SettingPayForm","SettingPayMethods","SettingPrint","SettingScale",
                "SettingsPOSDisplay","SettingSync","Safe_Emps","tax_groups","EmpBranches",
                "Banks","Safes","Branches","Stocks","Stock_Emps","SettingZatca","Settingmqtt"
            };

            string dropSql = "";
            foreach (var t in dropTables)
                dropSql += $"DROP TABLE {_toDbName}.dbo.{t} ";

            if (Common.GetZatcaActive())
                dropSql += $"DROP TABLE {_toDbName}.dbo.CSRProperties DROP TABLE {_toDbName}.dbo.ZatcaCredential ";

            await ExecuteSqlAsync(dropSql);

            string copySql = "";
            foreach (var t in dropTables)
                copySql += $"SELECT * INTO {_toDbName}.dbo.{t} FROM {MainClass.Database}.dbo.{t} ";

            if (Common.GetZatcaActive())
            {
                copySql += $"SELECT * INTO {_toDbName}.dbo.ZatcaCredential FROM {MainClass.Database}.dbo.ZatcaCredential ";
                copySql += $"SELECT * INTO {_toDbName}.dbo.CSRProperties FROM {MainClass.Database}.dbo.CSRProperties ";
            }

            await ExecuteSqlAsync(copySql);
        }

        private async Task CopyItemsAsync()
        {
            string dropSql = $"DROP TABLE {_toDbName}.dbo.Items " +
                             $"DROP TABLE {_toDbName}.dbo.ItemUnits " +
                             $"DROP TABLE {_toDbName}.dbo.ItemsCategory " +
                             $"DROP TABLE {_toDbName}.dbo.ItemPrices " +
                             $"DROP TABLE {_toDbName}.dbo.ItemComponents " +
                             $"DROP TABLE {_toDbName}.dbo.units " +
                             $"DROP TABLE {_toDbName}.dbo.Itembarcodes ";

            await ExecuteSqlAsync(dropSql);

            string copySql = $"SELECT * INTO {_toDbName}.dbo.Items FROM {MainClass.Database}.dbo.Items " +
                             $"SELECT * INTO {_toDbName}.dbo.ItemUnits FROM {MainClass.Database}.dbo.ItemUnits " +
                             $"SELECT * INTO {_toDbName}.dbo.ItemsCategory FROM {MainClass.Database}.dbo.ItemsCategory " +
                             $"SELECT * INTO {_toDbName}.dbo.ItemPrices FROM {MainClass.Database}.dbo.ItemPrices " +
                             $"SELECT * INTO {_toDbName}.dbo.Units FROM {MainClass.Database}.dbo.Units " +
                             $"SELECT * INTO {_toDbName}.dbo.Itembarcodes FROM {MainClass.Database}.dbo.Itembarcodes " +
                             $"SELECT * INTO {_toDbName}.dbo.ItemComponents FROM {MainClass.Database}.dbo.ItemComponents ";

            await ExecuteSqlAsync(copySql);
        }

        private async Task CopyUsersAsync()
        {
            string dropSql = $"DROP TABLE {_toDbName}.dbo.Users " +
                             $"DROP TABLE {_toDbName}.dbo.User_Permissions " +
                             $"DROP TABLE {_toDbName}.dbo.Employees ";
            await ExecuteSqlAsync(dropSql);

            string copySql = $"SELECT * INTO {_toDbName}.dbo.Users FROM {MainClass.Database}.dbo.Users " +
                             $"SELECT * INTO {_toDbName}.dbo.User_Permissions FROM {MainClass.Database}.dbo.User_Permissions " +
                             $"SELECT * INTO {_toDbName}.dbo.Employees FROM {MainClass.Database}.dbo.Employees ";
            await ExecuteSqlAsync(copySql);
        }

        private async Task CopyCustomersAsync()
        {
            string dropSql = $"DROP TABLE {_toDbName}.dbo.Customers " +
                             $"DROP TABLE {_toDbName}.dbo.salesmen " +
                             $"DROP TABLE {_toDbName}.dbo.Suppliers ";
            await ExecuteSqlAsync(dropSql);

            string copySql = $"SELECT * INTO {_toDbName}.dbo.Customers FROM {MainClass.Database}.dbo.Customers " +
                             $"SELECT * INTO {_toDbName}.dbo.salesmen FROM {MainClass.Database}.dbo.salesmen " +
                             $"SELECT * INTO {_toDbName}.dbo.Suppliers FROM {MainClass.Database}.dbo.Suppliers ";
            await ExecuteSqlAsync(copySql);
        }

        private async Task CopyAccountsAsync()
        {
            string dropSql = $"DROP TABLE {_toDbName}.dbo.Accounts_Index " +
                             $"DROP TABLE {_toDbName}.dbo.Cost_Center ";
            await ExecuteSqlAsync(dropSql);

            string copySql = $"SELECT * INTO {_toDbName}.dbo.Accounts_Index FROM {MainClass.Database}.dbo.Accounts_Index " +
                             $"SELECT * INTO {_toDbName}.dbo.Cost_Center FROM {MainClass.Database}.dbo.Cost_Center ";
            await ExecuteSqlAsync(copySql);
        }

        private async Task CopyInvoicesAsync()
        {
            string dropSql = $"DROP TABLE {_toDbName}.dbo.Inv " +
                             $"DROP TABLE {_toDbName}.dbo.Inv_sub " +
                             $"DROP TABLE {_toDbName}.dbo.Entry " +
                             $"DROP TABLE {_toDbName}.dbo.Entry_sub " +
                             $"DROP TABLE {_toDbName}.dbo.Receipts ";

            await ExecuteSqlAsync(dropSql, true);

            string from = txtStartdate.DateTime.ToShortDateString();
            string to   = txtEnddate.DateTime.ToShortDateString();

            string copySql =
                $"SELECT * INTO {_toDbName}.dbo.Inv FROM {MainClass.Database}.dbo.Inv WHERE Date>=N'{from}' AND Date<=N'{to}' " +
                $"SELECT Inv_Sub.* INTO {_toDbName}.dbo.Inv_sub FROM {MainClass.Database}.dbo.Inv AS Inv " +
                $"LEFT JOIN {MainClass.Database}.dbo.Inv_Sub AS Inv_Sub ON Inv.InvGlobalID=Inv_sub.InvGlobalID " +
                $"WHERE Inv.Date>=N'{from}' AND Inv.Date<=N'{to}' " +
                $"SELECT * INTO {_toDbName}.dbo.Entry FROM {MainClass.Database}.dbo.Entry WHERE Date>=N'{from}' AND Date<=N'{to}' " +
                $"SELECT Entry_sub.* INTO {_toDbName}.dbo.Entry_sub FROM {MainClass.Database}.dbo.Entry AS Entry " +
                $"LEFT JOIN {MainClass.Database}.dbo.Entry_sub AS Entry_Sub ON Entry.GlobalID=Entry_Sub.EntryGlobalID " +
                $"WHERE Entry.IS_Deleted=0 AND Entry.Date>=N'{from}' AND Entry.Date<=N'{to}' " +
                $"SELECT * INTO {_toDbName}.dbo.Receipts FROM {MainClass.Database}.dbo.Receipts " +
                $"WHERE ReceiptDate>=N'{from}' AND ReceiptDate<=N'{to}'";

            await ExecuteSqlAsync(copySql);
        }

        private async Task RecycleFirstStockAsync()
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var invoice     = BindToInvoice();
                if (invoice != null)
                    SaveInvoice(invoice, null, true);
            }
            catch { }
        }

        private async Task ExecuteSqlAsync(string sql, bool ignoreErrors = false)
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(sql, _conn))
                    await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                if (!ignoreErrors)
                    DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar1.Value = value;
                lblProgress.Text   = $"{value}%";
            });
        }

        #endregion

        #region BindToInvoice / BindToEntry / SaveInvoice / SaveEnty

        private Invoice BindToInvoice()
        {
            var invoice = new Invoice
            {
                AutoIncrementID    = 1,
                InvoiceNo          = 1,
                ClientCode         = Sync.ClientCode,
                InvCombinedId      = "1F11",
                Total              = 0, SumPrice = 0, VAT = 0, Net = 0,
                Discount           = 0, TotDiscount = 0, Additions = 0,
                Insurance          = 0, Paycash = 0, PayATM = 0,
                PayType            = 1, Remainder = 0,
                InvGlobalID        = $"{MainClass.BranchNo}-1",
                EntryGlobalID      = "-1",
                InvoiceType        = InvoiceType.BeginingInventory,
                Bank               = -1, ProcType = 1, OrderNo = -1, OrderType = -1,
                InvDate            = DateTime.Now,
                User               = MainClass.EmpNo,
                Branch             = MainClass.BranchNo,
                DistBranch         = Sync.DistBranch,
                BranchType         = Sync.BranchType,
                Treasury           = -1, Saleman = -1, Customer = -1,
                IsDeleted          = false, VATperc = 0,
                ReffNo             = "-1", RefDate = DateTime.Now,
                InvAccCode         = "-1", Store = 1, CloudID = "",
                CashCustomerName   = "", CashCustomerMobile = "",
                Balance_previews   = 0
            };

            invoice.InvNote = InvoiceOper.GetInvoiceType(
                (int)invoice.InvoiceType, invoice.ProcType, invoice.PayType, 1) +
                " برقم " + invoice.InvoiceNo;

            var list = new System.Collections.Generic.List<Item>();

            if (Common.CurrentBranch.Invertories.Count > 0)
            {
                var firstInventory = Common.CurrentBranch.Invertories.FirstOrDefault();
                if (firstInventory == null) return null;

                if (!int.TryParse(firstInventory.InvertoryId, out int inventoryId))
                    return null;

                var dataTable = Inventory.TotalItemStock(
                    inventoryId,
                    txtEnddate.DateTime != DateTime.MinValue
                        ? txtEnddate.DateTime.Date
                        : DateTime.Now.Date,
                    Common.CurrentBranch.BranchId).Copy();

                foreach (DataRow row in dataTable.Rows)
                {
                    double qty = Convert.ToDouble(row["Stock"]);
                    list.Add(new Item
                    {
                        InvGlobalID    = invoice.InvGlobalID,
                        ClientCode     = Sync.ClientCode,
                        AutoIncrementID = 1,
                        ItemNo         = Convert.ToInt32(row["itemId"]),
                        ProcType       = 1,
                        Quantity       = qty, UnitEquality = 1, PrimaryQnty = qty,
                        Unit           = 1,
                        Price          = ItemOper.AvgCost(Convert.ToInt32(row["itemId"]), MainClass.BranchNo),
                        Vat            = 0, VatPerc = 0, ItemDiscount = 0,
                        ValiableStock  = qty,
                        Store          = Convert.ToDouble(row["Inventory"]),
                        ExpireDate     = DateTime.Now.AddYears(1),
                        ProductId      = 0
                    });
                }

                invoice.Items = list;
                return invoice;
            }

            return null;
        }

        private void BindToEntry()
        {
            string note = "سند قيد افتتاحي رقم: 1";

            var entry = new Entry
            {
                EntryGlobalID = $"{MainClass.BranchNo}-1",
                ClientCode    = Sync.ClientCode,
                EntryNo       = 1,
                EntryDate     = DateTime.Now,
                ReffNo        = "-1", RefDate = DateTime.Now,
                Type          = EntryType.IntialEntry,
                State         = 1, Note = note,
                Branch        = MainClass.BranchNo,
                EmpID         = MainClass.EmpNo,
                DistBranch    = Sync.DistBranch,
                BranchType    = Sync.BranchType,
                IsVAT         = false
            };

            var list = new System.Collections.Generic.List<Account>();
            var dt = Accounting.BalanceSheet(
                MainClass.BranchNo,
                txtEnddate.DateTime.Date + new TimeSpan(23, 59, 59)).Copy();

            foreach (DataRow row in dt.Rows)
            {
                double debt   = Convert.ToDouble(row["Debet"]);
                double credit = Convert.ToDouble(row["Credit"]);

                var account = new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entry.EntryNo,
                    Name          = row["AccountName"].ToString(),
                    Code          = row["AccountCode"].ToString(),
                    salesman      = -1,
                    Note          = note,
                    CCcode        = "-1"
                };

                if (debt >= credit)
                {
                    account.Debt   = debt - credit;
                    account.Credit = 0;
                }
                else
                {
                    account.Debt   = 0;
                    account.Credit = credit - debt;
                }

                if (account.Debt + account.Credit != 0)
                    list.Add(account);
            }

            entry.Accounts = list;
            SaveEnty(entry);
        }

        public bool SaveInvoice(Invoice inv, Entry entr, bool isNew)
        {
            SqlTransaction transaction = null;
            try
            {
                using (var sqlConn = new SqlConnection(
                    $"server={MainClass.Server};Database={_toDbName.Trim()};trusted_connection=true;"))
                {
                    sqlConn.Open();
                    transaction = sqlConn.BeginTransaction();

                    SqlCommand cmd;
                    if (isNew)
                        cmd = new SqlCommand(StoredQueries.InsertInv, sqlConn, transaction);
                    else
                    {
                        new SqlCommand(
                            $"DELETE FROM Inv_Sub WHERE InvGlobalID=N'{inv.InvGlobalID}'",
                            sqlConn, transaction).ExecuteNonQuery();
                        cmd = new SqlCommand(StoredQueries.UpdateInv, sqlConn, transaction);
                    }

                    cmd.Parameters.Add("@InvGlobalID",         SqlDbType.NVarChar).Value = inv.InvGlobalID;
                    cmd.Parameters.Add("@CloudID",             SqlDbType.NVarChar).Value = "";
                    cmd.Parameters.Add("@proc_type",           SqlDbType.Int).Value      = inv.ProcType;
                    cmd.Parameters.Add("@id",                  SqlDbType.Int).Value      = inv.InvoiceNo;
                    cmd.Parameters.Add("@date",                SqlDbType.DateTime).Value = inv.InvDate;
                    cmd.Parameters.Add("@inv_type",            SqlDbType.Int).Value      = inv.InvoiceType;
                    cmd.Parameters.Add("@OrderType",           SqlDbType.Int).Value      = inv.OrderType;
                    cmd.Parameters.Add("@safe",                SqlDbType.Int).Value      = inv.Store;
                    cmd.Parameters.Add("@stock",               SqlDbType.Int).Value      = inv.Treasury;
                    cmd.Parameters.Add("@cust_id",             SqlDbType.Int).Value      = inv.Customer;
                    cmd.Parameters.Add("@sales_emp",           SqlDbType.Int).Value      = inv.User;
                    cmd.Parameters.Add("@InvTotal",            SqlDbType.Float).Value    = inv.Total;
                    cmd.Parameters.Add("@AdditionsTot",        SqlDbType.Float).Value    = inv.Additions;
                    cmd.Parameters.Add("@Insurance",           SqlDbType.Float).Value    = inv.Insurance;
                    cmd.Parameters.Add("@tot_net",             SqlDbType.Float).Value    = inv.Net;
                    cmd.Parameters.Add("@InvProfit",           SqlDbType.Float).Value    = 0;
                    cmd.Parameters.Add("@paid",                SqlDbType.Float).Value    = inv.Paycash + inv.PayATM;
                    cmd.Parameters.Add("@minus",               SqlDbType.Float).Value    = inv.Discount;
                    cmd.Parameters.Add("@tax",                 SqlDbType.Float).Value    = inv.VAT;
                    cmd.Parameters.Add("@EntryID",             SqlDbType.Int).Value      = (entr != null ? entr.EntryNo : -1);
                    cmd.Parameters.Add("@cash",                SqlDbType.Float).Value    = inv.Paycash;
                    cmd.Parameters.Add("@visa",                SqlDbType.Float).Value    = inv.PayATM;
                    cmd.Parameters.Add("@branch",              SqlDbType.Int).Value      = inv.Branch;
                    cmd.Parameters.Add("@IS_Buy",              SqlDbType.Bit).Value      = 0;
                    cmd.Parameters.Add("@IS_Deleted",          SqlDbType.Bit).Value      = inv.IsDeleted;
                    cmd.Parameters.Add("@notes",               SqlDbType.NVarChar).Value = inv.InvNote;
                    cmd.Parameters.Add("@Reff_No ",            SqlDbType.NVarChar).Value = inv.ReffNo;
                    cmd.Parameters.Add("@Reff_date ",          SqlDbType.DateTime).Value = inv.RefDate;
                    cmd.Parameters.Add("@salesman",            SqlDbType.Int).Value      = inv.Saleman;
                    cmd.Parameters.Add("@pay_type",            SqlDbType.Int).Value      = inv.PayType;
                    cmd.Parameters.Add("@bank",                SqlDbType.Int).Value      = inv.Bank;
                    cmd.Parameters.Add("@Sync",                SqlDbType.Bit).Value      = 0;
                    cmd.Parameters.Add("@ExtraVAT",            SqlDbType.Float).Value    = 0;
                    cmd.Parameters.Add("@AdditionalCost",      SqlDbType.Float).Value    = 0;
                    cmd.Parameters.Add("@InvoiceStatus",       SqlDbType.Int).Value      = 3;
                    cmd.Parameters.Add("@PriceIncVAT",         SqlDbType.Bit).Value      = 1;
                    cmd.Parameters.Add("@PaymentStatus",       SqlDbType.Int).Value      = -1;
                    cmd.Parameters.Add("@InvCombinedId ",      SqlDbType.NVarChar).Value = inv.InvCombinedId;
                    cmd.Parameters.Add("@CurrencyCode ",       SqlDbType.NVarChar).Value = inv.Currency.ToString();
                    cmd.Parameters.Add("@ItemsDiscount",       SqlDbType.Float).Value    = 0;
                    cmd.Parameters.Add("@InvCost",             SqlDbType.Float).Value    = inv.InvoiceCost;
                    cmd.Parameters.Add("@FreeVATSales",        SqlDbType.Float).Value    = inv.FreeVATSales;
                    cmd.Parameters.Add("@InvSum",              SqlDbType.Float).Value    = inv.SumPrice;
                    cmd.Parameters.Add("@VATPercent",          SqlDbType.Float).Value    = inv.VATperc;
                    cmd.Parameters.Add("@CashCustomerName",    SqlDbType.NVarChar).Value = inv.CashCustomerName;
                    cmd.Parameters.Add("@CashCustomerMobile",  SqlDbType.NVarChar).Value = inv.CashCustomerMobile;
                    cmd.Parameters.Add("@QRCode",              SqlDbType.NVarChar).Value = inv.QRCode ?? "";
                    cmd.Parameters.Add("@InvoiceHash",         SqlDbType.NVarChar).Value = inv.InvoiceHash ?? "";
                    cmd.Parameters.Add("@UUID",                SqlDbType.NVarChar).Value = inv.UUID ?? "";
                    cmd.Parameters.Add("@ZatcaSent",           SqlDbType.Bit).Value      = inv.ZatcaSent;
                    cmd.Parameters.Add("@TotalWithholdingTax", SqlDbType.Float).Value    = inv.TotalWithholdingTax;
                    cmd.Parameters.Add("@TableNo",             SqlDbType.NVarChar).Value = inv.TableNo ?? "";
                    cmd.Parameters.Add("@Balance_previews",    SqlDbType.NVarChar).Value = inv.Balance_previews.ToString();
                    cmd.ExecuteNonQuery();

                    foreach (var item in inv.Items)
                    {
                        var subCmd = new SqlCommand(StoredQueries.InsertInvSub, sqlConn, transaction);
                        subCmd.Parameters.Add("@InvGlobalID",         SqlDbType.NVarChar).Value = inv.InvGlobalID;
                        subCmd.Parameters.Add("@proc_id",             SqlDbType.Int).Value      = inv.AutoIncrementID;
                        subCmd.Parameters.Add("@proc_type",           SqlDbType.Int).Value      = item.ProcType;
                        subCmd.Parameters.Add("@expire_date",         SqlDbType.DateTime).Value = item.ExpireDate;
                        subCmd.Parameters.Add("@Store",               SqlDbType.Float).Value    = item.Store;
                        subCmd.Parameters.Add("@ItemId",              SqlDbType.Int).Value      = item.ItemNo;
                        subCmd.Parameters.Add("@unit",                SqlDbType.Int).Value      = item.Unit;
                        subCmd.Parameters.Add("@UnitEquality",        SqlDbType.Float).Value    = item.UnitEquality;
                        subCmd.Parameters.Add("@val",                 SqlDbType.Float).Value    = item.PrimaryQnty;
                        subCmd.Parameters.Add("@val1",                SqlDbType.Float).Value    = item.Quantity;
                        subCmd.Parameters.Add("@exchange_price",      SqlDbType.Float).Value    = item.Price;
                        subCmd.Parameters.Add("@discount",            SqlDbType.Float).Value    = item.ItemDiscount;
                        subCmd.Parameters.Add("@taxperc",             SqlDbType.Float).Value    = item.VatPerc;
                        subCmd.Parameters.Add("@taxval",              SqlDbType.Float).Value    = item.Vat;
                        subCmd.Parameters.Add("@Description",         SqlDbType.NVarChar).Value = item.Description ?? "";
                        subCmd.Parameters.Add("@ProductId",           SqlDbType.Int).Value      = item.ProductId;
                        subCmd.Parameters.Add("@AvrgCost",            SqlDbType.Float).Value    = item.AvegCost;
                        subCmd.Parameters.Add("@CurrentQnty",         SqlDbType.Float).Value    = item.ValiableStock;
                        subCmd.Parameters.Add("@notes",               SqlDbType.NVarChar).Value = "";
                        subCmd.Parameters.Add("@ItemAddedCost",       SqlDbType.Float).Value    = item.ItemAddedCost;
                        subCmd.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value    = item.Price;
                        subCmd.Parameters.Add("@WithholdingTax",      SqlDbType.Float).Value    = item.WithholdingTax;
                        subCmd.Parameters.Add("@WithholdingTaxPerc",  SqlDbType.Float).Value    = item.WithholdingTaxPerc;
                        subCmd.Parameters.Add("@ItemCostCenter",      SqlDbType.NVarChar).Value = item.ItemCostCenter ?? "";
                        subCmd.Parameters.Add("@ItemAdditionalTax",   SqlDbType.Int).Value      = item.ItemAdditionalTax;
                        subCmd.Parameters.Add("@ItemAdditionalTaxPerc",SqlDbType.Int).Value     = item.ItemAdditionalTaxPerc;
                        subCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public object SaveEnty(Entry entry)
        {
            SqlTransaction transaction = null;
            try
            {
                using (var sqlConn = new SqlConnection(
                    $"server={MainClass.Server};Database={_toDbName.Trim()};trusted_connection=true;"))
                {
                    sqlConn.Open();
                    transaction = sqlConn.BeginTransaction();

                    bool isNew = true;
                    using (var chkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM Entry WHERE GlobalID=N'{entry.EntryGlobalID}'",
                        sqlConn, transaction))
                    {
                        isNew = Convert.ToInt32(chkCmd.ExecuteScalar()) == 0;
                    }

                    SqlCommand cmd;
                    if (isNew)
                        cmd = new SqlCommand(StoredQueries.InsertEntry, sqlConn, transaction);
                    else
                    {
                        new SqlCommand(
                            $"DELETE FROM Entry_Sub WHERE EntryGlobalID='{entry.EntryGlobalID}'",
                            sqlConn, transaction).ExecuteNonQuery();
                        cmd = new SqlCommand(StoredQueries.UpdateEntry, sqlConn, transaction);
                    }

                    cmd.Parameters.Add("@GlobalID",   SqlDbType.VarChar).Value  = entry.EntryGlobalID;
                    cmd.Parameters.Add("@id",         SqlDbType.Int).Value      = entry.EntryNo;
                    cmd.Parameters.Add("@date",       SqlDbType.DateTime).Value = entry.EntryDate;
                    cmd.Parameters.Add("@doc_no",     SqlDbType.Int).Value      = entry.EntryNo;
                    cmd.Parameters.Add("@type",       SqlDbType.Int).Value      = (int)entry.Type;
                    cmd.Parameters.Add("@state",      SqlDbType.Int).Value      = entry.State;
                    cmd.Parameters.Add("@notes",      SqlDbType.NVarChar).Value = entry.Note;
                    cmd.Parameters.Add("@branch",     SqlDbType.Int).Value      = entry.Branch;
                    cmd.Parameters.Add("@EmpID",      SqlDbType.Int).Value      = entry.EmpID;
                    cmd.Parameters.Add("@Sync",       SqlDbType.Bit).Value      = 0;
                    cmd.Parameters.Add("@IsVAT",      SqlDbType.Bit).Value      = entry.IsVAT;
                    cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value      = entry.ISDeleted;
                    cmd.ExecuteNonQuery();

                    foreach (var account in entry.Accounts)
                    {
                        var subCmd = new SqlCommand(StoredQueries.InsertEntrySub, sqlConn, transaction);
                        subCmd.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = entry.EntryGlobalID;
                        subCmd.Parameters.Add("@res_id",        SqlDbType.Int).Value      = account.EntryNo;
                        subCmd.Parameters.Add("@dept",          SqlDbType.Float).Value    = account.Debt;
                        subCmd.Parameters.Add("@credit",        SqlDbType.Float).Value    = account.Credit;
                        subCmd.Parameters.Add("@acc_no",        SqlDbType.Int).Value      = account.Code;
                        subCmd.Parameters.Add("@CCcode",        SqlDbType.Int).Value      = account.CCcode;
                        subCmd.Parameters.Add("@notes",         SqlDbType.NVarChar).Value = account.Note;
                        subCmd.Parameters.Add("@branch",        SqlDbType.Int).Value      = entry.Branch;
                        subCmd.Parameters.Add("@salesman",      SqlDbType.Int).Value      = account.salesman;
                        subCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                DXMessageBox.Show($"خطأ أثناء حفظ القيد\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion
    }
}