using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using UtilitiesProj;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDataRecycling : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private int _currentStep;

        private const int TotalSteps = 8;

        #endregion

        #region Constructor

        public frmDataRecycling()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _currentStep = 0;

            Loaded += FrmDataRecycling_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDataRecycling_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDatabases();

            cmbDBs.EditValue = MainClass.Database;
            cmbDBs.IsEnabled = false;

            btnPrevious.IsEnabled = false;
            btnNext.IsEnabled = true;

            txtStartdate.DateTime = DateTime.Now;
            txtEnddate.DateTime = DateTime.Now.AddYears(1);

            // Wire up events
            btnNext.Click += BtnNext_Click;
            btnPrevious.Click += BtnPrevious_Click;
            btnExit.Click += BtnExit_Click;
            btnPath.Click += BtnPath_Click;

            cmbDBs.EditValueChanged += CmbDBs_EditValueChanged;

            UpdateStepIndicator();
        }

        #endregion

        #region Load Databases

        public void LoadDatabases()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    @"SELECT s.name, d.Dbname
                      FROM master.dbo.DatabasesManagment d
                      INNER JOIN master.dbo.sysdatabases s ON d.dbid = s.dbid
                      WHERE d.IsActive=1 AND d.IsDeleted=0
                      ORDER BY d.ImportanceOrder",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbDBs.ItemsSource = dataTable.DefaultView;
                cmbDBs.DisplayMember = "Dbname";
                cmbDBs.ValueMember = "name";
                cmbDBs.EditValue = MainClass.Database;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Navigation

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentStep++;

                if (_currentStep == 1)
                    btnPrevious.IsEnabled = true;

                // التحقق من الخطوات
                if (_currentStep == 1 && cmbDBs.EditValue == null)
                {
                    btnNext.IsEnabled = false;
                }
                else if (_currentStep == 2 &&
                         !string.IsNullOrWhiteSpace(txtDbName.EditValue?.ToString()))
                {
                    cmbDBs.EditValue = MainClass.Database;
                    btnNext.IsEnabled = false;
                }
                else if (_currentStep == 7)
                {
                    var confirmResult = DXMessageBox.Show(
                        "⚠️ هل أنت متأكد من تدوير قاعدة البيانات المختارة؟",
                        "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirmResult != MessageBoxResult.Yes)
                    {
                        _currentStep--;
                        return;
                    }

                    CreateDatabase();
                    ApplyRecycling();

                    NavigateToStep(_currentStep);
                }
                else
                {
                    btnNext.IsEnabled = true;
                }

                if (_currentStep < TotalSteps)
                    NavigateToStep(_currentStep);

                UpdateStepIndicator();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentStep--;

                if (_currentStep >= 0)
                    NavigateToStep(_currentStep);

                if (_currentStep == 0)
                    btnPrevious.IsEnabled = false;

                btnNext.IsEnabled = true;
                UpdateStepIndicator();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NavigateToStep(int step)
        {
            if (step >= 0 && step < TabControl.Items.Count)
                TabControl.SelectedIndex = step;
        }

        private void UpdateStepIndicator()
        {
            txtStepIndicator.Text = (_currentStep + 1).ToString();
        }

        private void CmbDBs_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            btnNext.IsEnabled = cmbDBs.EditValue != null;
        }

        private void BtnPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "اختر مسار حفظ قاعدة البيانات",
                Filter = "SQL Database Files (*.mdf)|*.mdf",
                DefaultExt = ".mdf",
                FileName = txtDbName.EditValue?.ToString() ?? "NewDatabase"
            };

            if (dialog.ShowDialog() == true)
                txtPath.EditValue = dialog.FileName.Replace(".mdf", string.Empty);
        }

        #endregion

        #region Create Database

        private void CreateDatabase()
        {
            var errorDetails = new StringBuilder();

            try
            {
                var masterConn = new SqlConnection(
                    $"server={MainClass.Server};trusted_connection=true;");

                if (masterConn.State != ConnectionState.Open)
                    masterConn.Open();

                if (masterConn.State != ConnectionState.Open)
                {
                    DXMessageBox.Show("السيرفر غير متصل", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string dbDisplayName = txtDbName.EditValue?.ToString().Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(dbDisplayName))
                {
                    DXMessageBox.Show("يرجى إدخال اسم قاعدة البيانات",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من عدم تكرار الاسم
                var dupAdapter = new SqlDataAdapter(
                    $"SELECT * FROM master.dbo.DatabasesManagment " +
                    $"WHERE Dbname='{dbDisplayName}'",
                    masterConn);
                var dupTable = new DataTable();
                dupAdapter.Fill(dupTable);

                if (dupTable.Rows.Count > 0)
                {
                    DXMessageBox.Show(
                        "اسم قاعدة البيانات مستخدم من قبل، يرجى إدخال اسم آخر",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // الحصول على رقم DB التالي
                var maxIdAdapter = new SqlDataAdapter(
                    "SELECT MAX(dbid) FROM master.dbo.sysdatabases",
                    masterConn);
                var maxIdTable = new DataTable();
                maxIdAdapter.Fill(maxIdTable);

                int nextDbId = Convert.ToInt32(
                    Convert.ToDouble(maxIdTable.Rows[0][0].ToString())) + 1;

                // توليد اسم تلقائي فريد
                string autoName = $"Data{nextDbId}";
                bool isUnique = false;

                while (!isUnique)
                {
                    var checkAdapter = new SqlDataAdapter(
                        $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoName}'",
                        masterConn);
                    var checkTable = new DataTable();
                    checkAdapter.Fill(checkTable);

                    if (checkTable.Rows.Count == 0)
                        isUnique = true;
                    else
                    {
                        nextDbId++;
                        autoName = $"Data{nextDbId}";
                    }
                }

                // بناء جملة CREATE DATABASE
                string createSql = $"CREATE DATABASE {autoName.Trim()}";
                string pathValue = txtPath.EditValue?.ToString().Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(pathValue))
                {
                    createSql +=
                        $" CONTAINMENT = NONE ON PRIMARY " +
                        $"( NAME = N'{autoName}', FILENAME=N'{pathValue}.mdf', " +
                        $"SIZE=9280KB, MAXSIZE=UNLIMITED, FILEGROWTH=1024KB ) " +
                        $"LOG ON ( NAME = N'{autoName}_log', " +
                        $"FILENAME=N'{pathValue}.ldf', SIZE=3904KB, " +
                        $"MAXSIZE=2048GB, FILEGROWTH=10%)";
                }

                try
                {
                    new SqlCommand(createSql, masterConn).ExecuteNonQuery();

                    // إعداد المستخدم auditor
                    string[] setupCommands =
                    {
                        "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name=N'auditor') " +
                        "CREATE LOGIN [auditor] WITH PASSWORD=N'auditor', DEFAULT_DATABASE=[master]",
                        "ALTER SERVER ROLE [sysadmin] ADD MEMBER [auditor]",
                        "IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name=N'auditor') " +
                        "CREATE USER [auditor] FOR LOGIN [auditor] WITH DEFAULT_SCHEMA=[dbo]",
                        "ALTER ROLE [db_owner] ADD MEMBER [auditor]",
                        "ALTER LOGIN sa ENABLE",
                        "ALTER LOGIN sa WITH PASSWORD = 'auditor'"
                    };

                    foreach (string setupCmd in setupCommands)
                    {
                        try
                        {
                            new SqlCommand(setupCmd, masterConn).ExecuteNonQuery();
                        }
                        catch { /* ignore */ }
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show(ex.ToString(), "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    if (masterConn.State == ConnectionState.Open)
                        masterConn.Close();
                }

                // الاتصال بقاعدة البيانات الجديدة وإنشاء الجداول
                var newDbConn = new SqlConnection(
                    $"server={MainClass.Server};database={autoName.Trim()};trusted_connection=true");

                if (newDbConn.State == ConnectionState.Closed)
                    newDbConn.Open();

                // تشغيل سكريبت الجداول
                string[] scripts = Properties.Resources.CrystalLiteDB
                    .Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string script in scripts)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(script))
                            new SqlCommand(script, newDbConn).ExecuteNonQuery();
                    }
                    catch { /* ignore individual script errors */ }
                }

                // الحصول على dbid للقاعدة الجديدة
                var dbIdAdapter = new SqlDataAdapter(
                    $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoName}'",
                    masterConn);

                if (masterConn.State == ConnectionState.Closed)
                    masterConn.Open();

                var dbIdTable = new DataTable();
                dbIdAdapter.Fill(dbIdTable);

                string newDbId = dbIdTable.Rows.Count > 0
                    ? dbIdTable.Rows[0]["dbid"].ToString()
                    : nextDbId.ToString();

                // إدراج معلومات قاعدة البيانات
                var insertCmd = new SqlCommand(
                    @"INSERT INTO master.dbo.DatabasesManagment
                      (DbId,DbAutoName,Dbname,CreateTime,UserCreate,
                       AccountingPeriodStart,AccountingPeriodEnd,
                       UserName,UseFullName,passward,DefaultCoin,
                       IsActive,IsDeleted,ImportanceOrder)
                      VALUES
                      (@DbId,@DbAutoName,@Dbname,@CreateTime,@UserCreate,
                       @AccountingPeriodStart,@AccountingPeriodEnd,
                       @UserName,@UseFullName,@passward,@DefaultCoin,
                       @IsActive,@IsDeleted,@ImportanceOrder)",
                    newDbConn);

                insertCmd.Parameters.AddWithValue("@DbId", newDbId);
                insertCmd.Parameters.AddWithValue("@DbAutoName", autoName);
                insertCmd.Parameters.AddWithValue("@Dbname", dbDisplayName);
                insertCmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                insertCmd.Parameters.AddWithValue("@UserCreate", MainClass.EmpNo);
                insertCmd.Parameters.AddWithValue("@AccountingPeriodStart", txtStartdate.DateTime);
                insertCmd.Parameters.AddWithValue("@AccountingPeriodEnd", txtEnddate.DateTime);
                insertCmd.Parameters.AddWithValue("@UserName", "1");
                insertCmd.Parameters.AddWithValue("@UseFullName", "1");
                insertCmd.Parameters.AddWithValue("@passward", "-1");
                insertCmd.Parameters.AddWithValue("@DefaultCoin", string.Empty);
                insertCmd.Parameters.AddWithValue("@IsActive", 1);
                insertCmd.Parameters.AddWithValue("@IsDeleted", 0);
                insertCmd.Parameters.AddWithValue("@ImportanceOrder", 1);
                insertCmd.ExecuteNonQuery();

                newDbConn.Close();

                txtDatabaseName.EditValue = autoName;

                DXMessageBox.Show("✅ تمت عملية إنشاء قاعدة البيانات بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    errorDetails.AppendLine(
                        $"[{i}] {ex.Errors[i].Message} " +
                        $"(Line: {ex.Errors[i].LineNumber})");
                }
                ShowError(errorDetails.ToString());
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Apply Recycling

        private void ApplyRecycling()
        {
            string targetDb = txtDatabaseName.EditValue?.ToString().Trim() ?? string.Empty;
            string sourceDb = cmbDBs.EditValue?.ToString().Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(sourceDb) ||
                string.IsNullOrEmpty(targetDb))
            {
                DXMessageBox.Show(
                    "لا يمكن تنفيذ عملية التدوير لعدم تحديد قاعدة البيانات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ─── تدوير المواد ───
            if (ckImportItems.IsChecked == true)
            {
                var itemsTables = new[]
                {
                    ("Items",       true,  $"SELECT * FROM {sourceDb}.dbo.Items"),
                    ("ItemUnits",   true,  $"SELECT * FROM {sourceDb}.dbo.ItemUnits"),
                    ("units",       false,
                     $"SELECT name,defaultInv,IS_Deleted FROM {sourceDb}.dbo.Units"),
                    ("Safes",       false,
                     $"SELECT name,branch,status,IS_Default,notes,IS_Deleted FROM {sourceDb}.dbo.Safes"),
                    ("Branches",    false,
                     $"SELECT name,Tel,Mobile,Fax,Email,Address,notes,IS_Deleted FROM {sourceDb}.dbo.Branches"),
                    ("Itembarcodes",true,  $"SELECT * FROM {sourceDb}.dbo.Itembarcodes"),
                    ("Safe_Emps",   true,  $"SELECT * FROM {sourceDb}.dbo.Safe_Emps"),
                    ("Stocks",      false,
                     $"SELECT name,Acc_Code,branch,status,IS_Default,notes,IS_Deleted FROM {sourceDb}.dbo.Stocks"),
                    ("Stock_Emps",  true,  $"SELECT * FROM {sourceDb}.dbo.Stock_Emps"),
                    ("EmpBranches", true,  $"SELECT * FROM {sourceDb}.dbo.EmpBranches"),
                };

                foreach (var (table, useSelectAll, insertSelect) in itemsTables)
                {
                    ExecuteWithErrorHandling(
                        $"TRUNCATE TABLE {targetDb}.dbo.{table}",
                        $"خطأ أثناء مسح جدول {table}");

                    ExecuteWithErrorHandling(
                        $"INSERT INTO {targetDb}.dbo.{table} {insertSelect}",
                        $"خطأ أثناء استيراد بيانات {table}");
                }

                // ItemsCategory بأعمدة محددة
                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.ItemsCategory",
                    "خطأ أثناء مسح المجموعات");

                ExecuteWithErrorHandling(
                    $@"INSERT INTO {targetDb}.dbo.ItemsCategory
                       (Id,CategoyId,Code,ParentCode,type,name,nameEN,
                        color,printer,image,ShowInPOS,DispalyOrder,IS_Deleted)
                       SELECT Id,CategoyId,Code,ParentCode,type,name,nameEN,
                        color,printer,image,ShowInPOS,DispalyOrder,IS_Deleted
                       FROM {sourceDb}.dbo.ItemsCategory",
                    "خطأ أثناء استيراد المجموعات");
            }

            // ─── تدوير المستخدمين ───
            if (ckImportUsers.IsChecked == true)
            {
                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.Users",
                    "خطأ أثناء مسح المستخدمين");

                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.User_Permissions",
                    "خطأ أثناء مسح الصلاحيات");

                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.Employees",
                    "خطأ أثناء مسح الموظفين");

                ExecuteWithErrorHandling(
                    $@"INSERT INTO {targetDb}.dbo.Users
                       (emp,username,pwd,IS_Deleted,SecureCode,LoginSecode)
                       SELECT emp,username,pwd,IS_Deleted,SecureCode,LoginSecode
                       FROM {sourceDb}.dbo.Users",
                    "خطأ أثناء استيراد المستخدمين");

                ExecuteWithErrorHandling(
                    $"INSERT INTO {targetDb}.dbo.User_Permissions " +
                    $"SELECT * FROM {sourceDb}.dbo.User_Permissions",
                    "خطأ أثناء استيراد الصلاحيات");
            }

            // ─── تدوير العملاء والموردين ───
            if (ckcustomers.IsChecked == true)
            {
                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.Customers",
                    "خطأ أثناء مسح العملاء");

                ExecuteWithErrorHandling(
                    $@"INSERT INTO {targetDb}.dbo.Customers
                       (name,country,city,area,act,national_id,tel,mobile,
                        fax,email,notes,IS_Deleted,maxdepit,type,tax_no,
                        AccountCode,IdScan)
                       SELECT name,country,city,area,act,national_id,tel,mobile,
                        fax,email,notes,IS_Deleted,maxdepit,type,tax_no,
                        AccountCode,IdScan
                       FROM {sourceDb}.dbo.Customers",
                    "خطأ أثناء استيراد العملاء");

                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.salesmen",
                    "خطأ أثناء مسح المندوبين");

                ExecuteWithErrorHandling(
                    $@"INSERT INTO {targetDb}.dbo.salesmen
                       (name,comm,tel,mobile,email,notes,IS_Deleted)
                       SELECT name,comm,tel,mobile,email,notes,IS_Deleted
                       FROM {sourceDb}.dbo.salesmen",
                    "خطأ أثناء استيراد المندوبين");
            }

            // ─── تدوير الحسابات ───
            if (ckAccounts.IsChecked == true)
            {
                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.Accounts_Index",
                    "خطأ أثناء مسح الحسابات");

                ExecuteWithErrorHandling(
                    $"INSERT INTO {targetDb}.dbo.Accounts_Index " +
                    $"SELECT * FROM {sourceDb}.dbo.Accounts_Index",
                    "خطأ أثناء استيراد الحسابات");

                ExecuteWithErrorHandling(
                    $"TRUNCATE TABLE {targetDb}.dbo.Cost_Center",
                    "خطأ أثناء مسح مراكز التكلفة");

                ExecuteWithErrorHandling(
                    $@"INSERT INTO {targetDb}.dbo.Cost_Center
                       (Code,name,name_en,ParentCode,type,Is_Deleted,Created_date)
                       SELECT Code,name,name_en,ParentCode,type,Is_Deleted,Created_date
                       FROM {sourceDb}.dbo.Cost_Center",
                    "خطأ أثناء استيراد مراكز التكلفة");
            }

            // ─── توليد فاتورة أول المدة ───
            if (rbFirstStockInv.IsChecked == true)
                _ = ExportFirstStockAsync();

            // ─── توليد قيد افتتاحي ───
            if (rbInitialBalance.IsChecked == true)
                GenerateInitialEntry();
        }

        /// <summary>
        /// تنفيذ استعلام SQL مع معالجة الخطأ
        /// </summary>
        private void ExecuteWithErrorHandling(string sql, string errorMessage)
        {
            try
            {
                if (_conn.State == ConnectionState.Closed)
                    _conn.Open();

                new SqlCommand(sql, _conn).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"{errorMessage}:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region First Stock Invoice

        public async Task ExportFirstStockAsync()
        {
            try
            {
                var stockData = Inventory.ItemsStocks().Copy();
                var itemsList = new List<Item>();
                string invGlobalId = $"{MainClass.BranchNo}-1";

                var invoice = new Invoice
                {
                    InvCombinedId = "1F11",
                    InvoiceNo = 1,
                    InvGlobalID = invGlobalId,
                    EntryGlobalID = "-1",
                    AutoIncrementID = 1,
                    InvoiceType = InvoiceType.BeginingInventory,
                    ProcType = 1,
                    InvoiceStatus = 3,
                    ClientCode = Sync.ClientCode,
                    Total = 0.0,
                    Net = 0.0,
                    PriceIncVAT = true,
                    PayType = 0,
                    Bank = -1,
                    OrderNo = 1,
                    InvDate = DateTime.Now,
                    User = MainClass.EmpNo,
                    Branch = MainClass.BranchNo,
                    DistBranch = Sync.DistBranch,
                    BranchType = Sync.BranchType,
                    Treasury = -1,
                    InvCCcode = "-1",
                    Saleman = -1,
                    Customer = 1,
                    InvNote = "بضاعة أول المدة",
                    IsDeleted = false,
                    ReffNo = "-1",
                    RefDate = DateTime.Now,
                    InvAccCode = "-1",
                    Store = 1,
                    Currency = Currency.SAR
                };

                foreach (DataRow row in stockData.Rows)
                {
                    double quantity = Convert.ToDouble(row["Quantity"]);
                    double avgCost = Convert.ToDouble(row["AvrgCost"]);
                    int inventoryId = Convert.ToInt32(row["InventoryId"]);
                    int itemId = Convert.ToInt32(row["ItemId"]);

                    itemsList.Add(new Item
                    {
                        InvGlobalID = invGlobalId,
                        ClientCode = Sync.ClientCode,
                        ItemNo = itemId,
                        ProcType = 1,
                        Quantity = quantity,
                        UnitEquality = 1.0,
                        PrimaryQnty = quantity,
                        Unit = 1,
                        Price = 0.0,
                        Vat = 0.0,
                        VatPerc = 0.0,
                        ValiableStock = quantity,
                        AvegCost = avgCost,
                        Store = inventoryId,
                        ExpireDate = DateTime.Now.AddYears(2),
                        Note = "بضاعة أول المدة"
                    });
                }

                invoice.Items = itemsList;

                var invoiceOper = new InvoiceOper();
                Entry entry = null;

                bool saved = invoiceOper.SaveInvoice(invoice, entry, IsNew: true);

                if (saved && Sync.ActiveSync && Sync.SyncType > 0)
                    await invoiceOper.SyncInvoice(invoice, entry, IsNew: true);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء حساب بضاعة أول المدة:\n{ex.Message}");
            }
        }

        #endregion

        #region Generate Initial Entry

        private void GenerateInitialEntry()
        {
            string targetDb = txtDatabaseName.EditValue?.ToString().Trim() ?? string.Empty;

            var targetConn = new SqlConnection(
                $"server={MainClass.Server};Database={targetDb};trusted_connection=true;");

            try
            {
                if (targetConn.State != ConnectionState.Open)
                    targetConn.Open();

                // منطق توليد القيد الافتتاحي
                // يمكن توسيعه حسب متطلبات العمل
            }
            catch (Exception ex)
            {
                string errorMsg = MainClass.Language == "ar"
                    ? $"خطأ أثناء توليد القيد الافتتاحي\nتفاصيل الخطأ: {ex.Message}"
                    : $"Error generating initial entry\nError details: {ex.Message}";
                ShowError(errorMsg);
            }
            finally
            {
                if (targetConn.State != ConnectionState.Closed)
                    targetConn.Close();
            }
        }

        #endregion

        #region Utility Methods

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}