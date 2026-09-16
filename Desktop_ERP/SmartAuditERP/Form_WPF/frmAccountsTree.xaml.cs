using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة شجرة الحسابات - إضافة وتعديل وحذف الحسابات
    /// </summary>
    public partial class frmAccountsTree : ThemedWindow
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private SqlConnection _connection;
        private int _id = -1;
        private string _searchName = "";
        private int _selectedId = -1;
        private int _selectedCode = -1;

        #endregion

        #region Properties

        /// <summary>
        /// معرف الحساب الحالي
        /// </summary>
        public int ID
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// اسم البحث
        /// </summary>
        public string SrchName
        {
            get => _searchName;
            set => _searchName = value;
        }

        /// <summary>
        /// الكود المحدد
        /// </summary>
        public int SelectedCode
        {
            get => _selectedCode;
            set => _selectedCode = value;
        }

        #endregion

        #region Constructor

        public frmAccountsTree()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ تهيئة txtDate بشكل آمن
                if (txtDate != null)
                    txtDate.DateTime = DateTime.Now;

                LoadBranches();
                LoadParent();
                LoadCostCenters();

                // ✅ تهيئة User بشكل آمن
                if (User != null)
                    User.Text = MainClass.UserName ?? "";

                // ✅ التأكد من الحالة الافتراضية الصحيحة
                if (GBbranchAcc != null)
                    GBbranchAcc.Visibility = Visibility.Collapsed;

                if (AName != null)
                    AName.Focus();
            }
            catch (Exception ex)
            {
                Logger.Error($"Window_Loaded Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Data Methods

        /// <summary>
        /// تحميل الفروع
        /// </summary>
        private void LoadBranches()
        {
            try
            {
                string query = "SELECT id, name FROM Branches ORDER BY id";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";
                    cmbBranches.ItemsSource = dt.DefaultView;
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadBranches Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل الحسابات الرئيسية (الأب)
        /// </summary>
        private void LoadParent()
        {
            try
            {
                string query = "SELECT Code, AName as name FROM Accounts_Index WHERE Type=1 ORDER BY Code";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    ParentCode.DisplayMemberPath = "name";
                    ParentCode.SelectedValuePath = "Code";
                    ParentCode.ItemsSource = dt.DefaultView;

                    if (_selectedCode > -1)
                    {
                        ParentCode.SelectedValue = _selectedCode;
                    }
                    else
                    {
                        ParentCode.SelectedIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadParent Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل مراكز التكلفة
        /// </summary>
        private void LoadCostCenters()
        {
            try
            {
                string query = $"SELECT code, name FROM cost_center WHERE type=2 AND BranchId={MainClass.BranchNo} ORDER BY code";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbCostCenter.DisplayMemberPath = "name";
                    cmbCostCenter.SelectedValuePath = "code";
                    cmbCostCenter.ItemsSource = dt.DefaultView;
                    cmbCostCenter.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCostCenters Error: {ex.Message}");
            }
        }

        #endregion

        #region Clear & Generate Code Methods

        /// <summary>
        /// تنظيف جميع الحقول
        /// </summary>
        public void Clear()
        {
            _id = -1;
            Code.Text = "";
            AName.Text = "";
            debt.IsChecked = true;
            main.IsChecked = true;
            txtDate.DateTime = DateTime.Now;
            User.Text = MainClass.UserName;
            IValue.Text = "";

            // تمكين العناصر
            Code.IsEnabled = true;
            ParentCode.IsEnabled = true;
            txtDate.IsEnabled = true;
            User.IsEnabled = true;
            cmbCostCenter.IsEnabled = true;
            debt.IsEnabled = true;
            credit.IsEnabled = true;
            main.IsEnabled = true;
            sub1.IsEnabled = true;

            cmbCostCenter.SelectedIndex = -1;
            AName.Focus();
        }

        /// <summary>
        /// توليد كود الحساب تلقائياً
        /// </summary>
        private void GenerateCode()
        {
            try
            {
                // ✅ إصلاح: التحقق من تحميل النافذة
                if (!IsLoaded) return;

                // ✅ إصلاح: التحقق من وجود العناصر
                if (ParentCode == null || Code == null) return;

                if (ParentCode.SelectedValue == null)
                    return;

                string parentCodeValue = ParentCode.SelectedValue.ToString();

                if (_id == -1)
                    GenerateNewAccountCode(parentCodeValue);
                else
                    UpdateExistingAccountCode(parentCodeValue);
            }
            catch (Exception ex)
            {
                Logger.Error($"GenerateCode Error: {ex.Message}");
            }
        }

        /// <summary>
        /// توليد كود لحساب جديد
        /// </summary>
        private void GenerateNewAccountCode(string parentCodeValue)
        {
            try
            {
                // ✅ إصلاح: التحقق من وجود العناصر
                if (Code == null || main == null) return;

                string query = $"SELECT MAX(Code) FROM Accounts_Index " +
                               $"WHERE ParentCode=N'{parentCodeValue}'";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                    {
                        long maxCode = Convert.ToInt64(dt.Rows[0][0]);
                        Code.Text = (maxCode + 1).ToString();
                    }
                    else
                    {
                        Code.Text = main.IsChecked == true
                            ? parentCodeValue + "1"
                            : parentCodeValue + "0001";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GenerateNewAccountCode Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث كود حساب موجود عند تغيير النوع
        /// </summary>
        private void UpdateExistingAccountCode(string parentCodeValue)
        {
            try
            {
                // ✅ إصلاح: التحقق من وجود العناصر
                if (Code == null || sub1 == null) return;

                string currentCode = Code.Text ?? "";
                if (string.IsNullOrWhiteSpace(currentCode)) return;

                string query = $"SELECT Code FROM Accounts_Index " +
                               $"WHERE ParentCode=N'{parentCodeValue}' " +
                               $"AND type=1 AND code={currentCode}";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0 && sub1.IsChecked == true)
                    {
                        query = $"SELECT MAX(Code) FROM Accounts_Index " +
                                $"WHERE ParentCode=N'{parentCodeValue}' AND type=2";

                        using (SqlDataAdapter adapter2 = new SqlDataAdapter(query, _connection))
                        {
                            DataTable dt2 = new DataTable();
                            adapter2.Fill(dt2);

                            if (dt2.Rows.Count > 0 && dt2.Rows[0][0] != DBNull.Value)
                            {
                                long maxCode = Convert.ToInt64(dt2.Rows[0][0]);
                                Code.Text = (maxCode + 1).ToString();
                            }
                            else
                            {
                                Code.Text = parentCodeValue + "0001";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"UpdateExistingAccountCode Error: {ex.Message}");
            }
        }

        #endregion

        #region ParentCode Search Button

        /// <summary>
        /// زر البحث بجانب الحساب الأب
        /// </summary>
        private void ParentCode_SearchClick(object sender, RoutedEventArgs e)
        {
            ShowAccountSearchDialog();
        }

        #endregion

        #region Save Methods

        /// <summary>
        /// حفظ الحساب (الطريقة الرئيسية)
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // التحقق من صلاحيات الفرع
            if (Sync.BranchType == 2 && Sync.SyncType > 0)
            {
                MessageBox.Show("لا يمكن إضافة او تعديل الحساب من هذا الفرع", "",
                    MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            try
            {
                // تأكيد الحفظ
                if (MessageBox.Show("هل انت متأكد من حفظ الحساب؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                // التحقق من البيانات
                if (!ValidateInput())
                    return;

                // تحضير البيانات للحفظ
                TreeAccount account = PrepareAccountData();

                // حفظ الحساب
                if (SaveAccount(account))
                {
                    // Sync مع الأنظمة الخارجية
                    SyncAccountWithExternalSystems(account);

                    // رسالة النجاح
                    ShowSuccessMessage();

                    // تحديث الواجهة
                    RefreshAfterSave();
                }
                else
                {
                    MessageBox.Show("خطأ أثناء الحفظ", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"btnSave_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// التحقق من صحة المدخلات
        /// </summary>
        private bool ValidateInput()
        {
            // التحقق من كود الحساب
            if (string.IsNullOrWhiteSpace(Code.Text))
            {
                MessageBox.Show(GetLocalizedMessage("EnterAccountCode", "من فضلك أدخل كود الحساب"));
                Code.Focus();
                return false;
            }

            // منع إضافة حساب رئيسي جديد يبدأ بـ 3
            if (Code.Text.StartsWith("3"))
            {
                MessageBox.Show("من فضلك لا يمكن إضافة حساب رئيسي جديد");
                return false;
            }

            // التحقق من اسم الحساب
            if (string.IsNullOrWhiteSpace(AName.Text))
            {
                MessageBox.Show(GetLocalizedMessage("EnterAccountName", "من فضلك أدخل اسم الحساب"));
                AName.Focus();
                return false;
            }

            // التحقق من الفرع
            if (cmbBranches.SelectedIndex == -1)
            {
                MessageBox.Show(GetLocalizedMessage("SelectBranch", "يجب تحديد الفرع"));
                return false;
            }

            // التحقق من الحساب الجديد
            if (_id == -1)
            {
                if (!ValidateNewAccount())
                    return false;
            }

            // التحقق من الحساب الأب
            if (!ValidateParentAccount())
                return false;

            return true;
        }

        /// <summary>
        /// التحقق من الحساب الجديد
        /// </summary>
        private bool ValidateNewAccount()
        {
            // التحقق من الحسابات الخاصة
            if (ParentCode.SelectedValue != null)
            {
                if (IsSpecialAccount())
                {
                    if (MessageBox.Show("يجب إنشاء الحساب من البطاقة الخاصة به, هل تريد إضافته", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        OpenCardForm();
                    }
                    return false;
                }
            }

            // التحقق من تكرار الكود
            if (IsAccountCodeExists())
            {
                MessageBox.Show(GetLocalizedMessage("CodeExists", "كود الحساب مدخل مسبقا، من فضلك أدخل كود آخر"));
                Code.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق من الحساب الأب
        /// </summary>
        private bool ValidateParentAccount()
        {
            if (ParentCode.SelectedValue != null)
            {
                // التحقق من وجود الحساب الأب
                if (!IsParentAccountExists())
                {
                    MessageBox.Show(GetLocalizedMessage("ParentNotFound", "الحساب الأب غير موجود"));
                    return false;
                }

                // التحقق من نوع الحساب الأب
                if (sub1.IsChecked == true)
                {
                    if (IsParentAccountSubAccount())
                    {
                        MessageBox.Show(GetLocalizedMessage("ParentMustBeMain",
                            "الحساب الأب عبارة عن حساب فرعي، يجب أن يكون الحساب الأب رئيسي"));
                        return false;
                    }
                }
            }
            else if (sub1.IsChecked == true)
            {
                MessageBox.Show(GetLocalizedMessage("EnterParentCode", "من فضلك ادخل كود الحساب الأب"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// التحقق إذا كان الحساب من الحسابات الخاصة
        /// </summary>
        private bool IsSpecialAccount()
        {
            if (ParentCode.SelectedValue == null)
                return false;

            string parentValue = ParentCode.SelectedValue.ToString();

            return parentValue == Common.CurrentBranch.TreasuriesAcc.ToString() ||
                   parentValue == Common.CurrentBranch.BanksAcc.ToString() ||
                   parentValue == Common.CurrentBranch.CustomersAcc.ToString() ||
                   parentValue == Common.CurrentBranch.SupliersAcc.ToString();
        }

        /// <summary>
        /// التحقق من وجود كود الحساب
        /// </summary>
        private bool IsAccountCodeExists()
        {
            try
            {
                string query = $"SELECT * FROM Accounts_Index WHERE Code='{Code.Text}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"IsAccountCodeExists Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// التحقق من وجود الحساب الأب
        /// </summary>
        private bool IsParentAccountExists()
        {
            try
            {
                string query = $"SELECT * FROM Accounts_Index WHERE Code='{ParentCode.SelectedValue}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"IsParentAccountExists Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// التحقق إذا كان الحساب الأب حساب فرعي
        /// </summary>
        private bool IsParentAccountSubAccount()
        {
            try
            {
                string query = $"SELECT Type FROM Accounts_Index WHERE Code='{ParentCode.SelectedValue}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return Convert.ToInt32(dt.Rows[0][0]) == 2;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"IsParentAccountSubAccount Error: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// تحضير بيانات الحساب للحفظ
        /// </summary>
        private TreeAccount PrepareAccountData()
        {
            int accNature = 0;
            int finalAcc = DetermineFinalAccount();
            int accType = main.IsChecked == true ? 1 : 2;
            double value = 0;

            if (sub1.IsChecked == true)
            {
                accNature = debt.IsChecked == true ? 1 : 2;
                if (!string.IsNullOrEmpty(IValue.Text))
                {
                    double.TryParse(IValue.Text, out value);
                }
            }

            int costCenter = 0;
            if (cmbCostCenter.SelectedIndex > -1)
            {
                costCenter = Convert.ToInt32(cmbCostCenter.SelectedValue);
            }

            return new TreeAccount
            {
                AccName = AName.Text.Trim(),
                AccNature = accNature,
                Code = Code.Text.Trim(),
                ParentCode = ParentCode.SelectedValue.ToString(),
                IntialBalance = (decimal)value,
                EmpId = MainClass.EmpNo,
                AccType = accType,
                IsDeleted = false,
                FinalAcc = finalAcc,
                LastUpdateDate = txtDate.DateTime != default(DateTime)
                                        ? txtDate.DateTime : DateTime.Now,
                ClientCode = Sync.ClientCode,
                CostCenter = costCenter,
                BranchId = Convert.ToInt32(cmbBranches.SelectedValue),
                CreateDate = txtDate.DateTime != default(DateTime)
                                        ? txtDate.DateTime : DateTime.Now
            };
        }

        /// <summary>
        /// تحديد نوع الحساب النهائي (ميزانية/قائمة دخل)
        /// </summary>
        private int DetermineFinalAccount()
        {
            if (string.IsNullOrEmpty(Code.Text))
                return 0;

            char firstChar = Code.Text[0];

            if (firstChar == '1' || firstChar == '2')
                return 1; // حسابات الميزانية
            else if (firstChar == '3' || firstChar == '4')
                return 2; // حسابات قائمة الدخل

            return 0;
        }

        /// <summary>
        /// حفظ الحساب في قاعدة البيانات
        /// </summary>
        private bool SaveAccount(TreeAccount account)
        {
            try
            {
                // التحقق من إمكانية تحويل الحساب من رئيسي لفرعي
                if (_id != -1 && !CanConvertToSubAccount())
                {
                    MessageBox.Show("لا يمكن تحويل الحساب من رئيسي إلى فرعي لوجود حسابات فرعية");
                    return false;
                }

                List<TreeAccount> accounts = new List<TreeAccount> { account };
                return new EntityOperations().SaveAccounts(accounts);
            }
            catch (Exception ex)
            {
                Logger.Error($"SaveAccount Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// التحقق من إمكانية تحويل الحساب لحساب فرعي
        /// </summary>
        private bool CanConvertToSubAccount()
        {
            if (sub1.IsChecked == false)
                return true;

            try
            {
                string query = $"SELECT Code FROM Accounts_Index WHERE ParentCode='{_id}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count == 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"CanConvertToSubAccount Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// مزامنة الحساب مع الأنظمة الخارجية
        /// </summary>
        private void SyncAccountWithExternalSystems(TreeAccount account)
        {
            try
            {
                List<TreeAccount> accounts = new List<TreeAccount> { account };

                // Sync مع API
                if (Sync.ActiveSync && Sync.SyncType > 0)
                {
                    try
                    {
                        new AccountCRUD(Sync.APIUrl).AddTreeAccount(accounts);
                        Logger.Info($"Account synced with API: {account.Code}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"API Sync Error: {ex.Message}");
                    }
                }

                // MQTT Broker
                Home Home = new Home();
                if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                {
                    try
                    {
                        string cond = $"where Code={Code.Text}";
                        string payload = SendData.GetAccountIndex(cond);
                        string topic = ConnectBroker.ClientCode + "AccountIndex";

                        ConnectBroker.mqttClient.Publish(
                            topic,
                            Encoding.UTF8.GetBytes(payload),
                            0,
                            retain: true
                        );

                        Logger.Info($"Account published to MQTT: {account.Code}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"MQTT Sync Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SyncAccountWithExternalSystems Error: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض رسالة النجاح
        /// </summary>
        private void ShowSuccessMessage()
        {
            string message = GetLocalizedMessage("SavedSuccessfully", "تمت حفظ البيانات بنجاح");
            string title = GetLocalizedMessage("Save", "حفظ");
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// تحديث الواجهة بعد الحفظ
        /// </summary>
        private void RefreshAfterSave()
        {
            int parentCodeBackup = ParentCode.SelectedValue != null
                ? Convert.ToInt32(ParentCode.SelectedValue)
                : -1;

            Clear();
            LoadParent();

            if (parentCodeBackup > -1)
            {
                ParentCode.SelectedValue = parentCodeBackup;
            }

            GenerateCode();
            AName.Focus();
        }

        #endregion

        #region Delete Methods

        /// <summary>
        /// حذف الحساب
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // تأكيد الحذف
                if (MessageBox.Show("هل انت متأكد من حذف الحساب؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                // التحقق من وجود كود
                if (string.IsNullOrWhiteSpace(Code.Text))
                {
                    MessageBox.Show(GetLocalizedMessage("SelectAccountFirst", "من فضلك اختر حساب أولا أو ادخل كوده"));
                    return;
                }

                // التحقق من الارتباطات
                if (HasAccountTransactions())
                {
                    MessageBox.Show(GetLocalizedMessage("AccountHasTransactions",
                        "هذه الحساب له ارتباطات فرعية لايمكن حذفه"), "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                // التحقق من وجود الحساب
                string accountName = GetAccountName();
                if (accountName == null)
                {
                    MessageBox.Show(GetLocalizedMessage("AccountNotFound", "هذا الحساب غير موجود"));
                    return;
                }

                // التحقق من الحسابات الفرعية
                if (HasSubAccounts())
                {
                    MessageBox.Show(GetLocalizedMessage("AccountHasSubAccounts",
                        "لايمكن حذف الحساب لأن له حسابات فرعية"));
                    return;
                }

                // التحقق من الحسابات الخاصة
                if (IsSpecialAccountType())
                {
                    MessageBox.Show(GetLocalizedMessage("DeleteFromCard",
                        "يجب حذف الحساب من بطاقة الحساب الخاصة به"));
                    return;
                }

                // تنفيذ الحذف
                if (DeleteAccount(accountName))
                {
                    MessageBox.Show(GetLocalizedMessage("DeletedSuccessfully", "تمت حذف البيانات بنجاح"),
                        GetLocalizedMessage("Delete", "حذف"),
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"btnDelete_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// التحقق من وجود معاملات للحساب
        /// </summary>
        private bool HasAccountTransactions()
        {
            try
            {
                string query = $@"SELECT Entry_sub.res_id FROM Entry_sub 
                                 JOIN Entry ON Entry_sub.EntryGlobalID = Entry.GlobalID 
                                 WHERE Entry_sub.acc_no = {Code.Text} 
                                 AND Entry.IS_Deleted = 0";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HasAccountTransactions Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// الحصول على اسم الحساب
        /// </summary>
        private string GetAccountName()
        {
            try
            {
                string query = $"SELECT AName FROM Accounts_Index WHERE Code='{Code.Text}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return dt.Rows[0]["AName"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetAccountName Error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// التحقق من وجود حسابات فرعية
        /// </summary>
        private bool HasSubAccounts()
        {
            try
            {
                string query = $"SELECT * FROM Accounts_Index WHERE ParentCode='{Code.Text}'";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HasSubAccounts Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// التحقق إذا كان الحساب من نوع خاص
        /// </summary>
        private bool IsSpecialAccountType()
        {
            try
            {
                string query = $@"SELECT ParentCode FROM Accounts_Index WHERE Code='{Code.Text}' 
                                 AND (ParentCode={Common.CurrentBranch.TreasuriesAcc} 
                                 OR ParentCode={Common.CurrentBranch.BanksAcc} 
                                 OR ParentCode={Common.CurrentBranch.CustomersAcc} 
                                 OR ParentCode={Common.CurrentBranch.SupliersAcc})";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"IsSpecialAccountType Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// حذف الحساب من قاعدة البيانات
        /// </summary>
        private bool DeleteAccount(string accountName)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                using (SqlCommand cmd1 = new SqlCommand($"DELETE FROM Customers WHERE name=N'{accountName}'", _connection))
                using (SqlCommand cmd2 = new SqlCommand($"DELETE FROM Accounts_Index WHERE Code='{Code.Text}'", _connection))
                {
                    cmd1.ExecuteNonQuery();
                    cmd2.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"DeleteAccount Error: {ex.Message}");
                return false;
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        #endregion

        #region Button Events

        /// <summary>
        /// زر جديد
        /// </summary>
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            if (ParentCode.SelectedValue != null)
            {
                GenerateCode();
            }
        }

        /// <summary>
        /// زر إغلاق
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Account Type Changed

        /// <summary>
        /// عند تغيير نوع الحساب (رئيسي/فرعي)
        /// </summary>
        private void AccountTypeChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ إصلاح: التحقق من أن النافذة محملة بالكامل قبل التنفيذ
                // منع تنفيذ الحدث أثناء InitializeComponent
                if (!IsLoaded) return;

                // ✅ إصلاح: التحقق من أن العناصر موجودة
                if (GBbranchAcc == null || sub1 == null || main == null)
                    return;

                // تحديث طبيعة الحساب بناءً على الكود
                UpdateAccountNature();

                // إظهار/إخفاء بيانات الحساب الفرعي
                if (sub1.IsChecked == true)
                {
                    GBbranchAcc.Visibility = Visibility.Visible;
                }
                else
                {
                    GBbranchAcc.Visibility = Visibility.Collapsed;
                    ClearSubAccountFields();
                }

                GenerateCode();
            }
            catch (Exception ex)
            {
                Logger.Error($"AccountTypeChanged Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث طبيعة الحساب بناءً على الكود
        /// </summary>
        private void UpdateAccountNature()
        {
            // ✅ إصلاح: التحقق من وجود العناصر قبل الاستخدام
            if (Code == null || debt == null || credit == null)
                return;

            string codeText = Code.Text ?? "";

            // ✅ إصلاح: التحقق من أن النص غير فارغ قبل الوصول للحرف الأول
            if (string.IsNullOrEmpty(codeText))
            {
                // القيمة الافتراضية: مدين + مفعل
                debt.IsChecked = true;
                debt.IsEnabled = true;
                credit.IsEnabled = true;
                return;
            }

            char firstChar = codeText[0];

            switch (firstChar)
            {
                case '3':
                    // حسابات المصروفات (مدينة دائماً)
                    debt.IsChecked = true;
                    credit.IsChecked = false;
                    debt.IsEnabled = true;
                    credit.IsEnabled = false;
                    break;

                case '4':
                    // حسابات الإيرادات (دائنة دائماً)
                    credit.IsChecked = true;
                    debt.IsChecked = false;
                    credit.IsEnabled = true;
                    debt.IsEnabled = false;
                    break;

                default:
                    // باقي الحسابات (مدين/دائن - قابل للتغيير)
                    debt.IsChecked = true;
                    debt.IsEnabled = true;
                    credit.IsEnabled = true;
                    break;
            }
        }

        /// <summary>
        /// تنظيف حقول الحساب الفرعي
        /// </summary>
        private void ClearSubAccountFields()
        {
            // ✅ إصلاح: التحقق من وجود العناصر قبل الاستخدام
            if (IValue != null)
                IValue.Text = "";

            if (txtDate != null)
                txtDate.DateTime = DateTime.Now;

            if (User != null)
                User.Text = MainClass.UserName ?? "";

            if (cmbCostCenter != null)
                cmbCostCenter.SelectedIndex = -1;
        }

        #endregion

        #region ParentCode Events

        /// <summary>
        /// عند تغيير الحساب الأب
        /// </summary>
        private void ParentCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ✅ إصلاح: منع التنفيذ أثناء التحميل
            if (!IsLoaded) return;
            GenerateCode();
        }

        /// <summary>
        /// عند النقر المزدوج على الحساب الأب
        /// </summary>
        private void ParentCode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowAccountSearchDialog();
        }

        /// <summary>
        /// عند الضغط على Enter في الحساب الأب
        /// </summary>
        private void ParentCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchByName();
            }
        }

        /// <summary>
        /// البحث بالاسم
        /// </summary>
        private void SearchByName()
        {
            try
            {
                _searchName = ParentCode.Text;

                if (string.IsNullOrEmpty(_searchName))
                    return;

                string query = $"SELECT Code, AName FROM Accounts_Index WHERE AName = N'{_searchName}' AND type=1";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        ParentCode.SelectedValue = Convert.ToInt32(dt.Rows[0]["Code"]);
                    }
                    else
                    {
                        ShowAccountSearchDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SearchByName Error: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض نافذة البحث عن الحسابات
        /// </summary>
        private void ShowAccountSearchDialog()
        {
            try
            {
                var searchForm = new frmMainfrmAccountSrch();

                // ✅ تعيين نص البحث في مربع البحث الموجود في النافذة
                if (!string.IsNullOrEmpty(_searchName))
                {
                    searchForm.SetSearchText(_searchName);
                }

                if (searchForm.ShowDialog() == true && searchForm.Code > -1)
                {
                    string query = $"SELECT Code, AName FROM Accounts_Index WHERE type=1 AND Code={searchForm.Code}";
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            _selectedId = searchForm.Code;
                            ParentCode.SelectedValue = Convert.ToInt32(dt.Rows[0]["Code"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ShowAccountSearchDialog Error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// فتح نافذة البطاقة الخاصة بالحساب
        /// </summary>
        private void OpenCardForm()
        {
            try
            {
                if (ParentCode.SelectedValue == null)
                    return;

                string parentValue = ParentCode.SelectedValue.ToString();

                if (parentValue == Common.CurrentBranch.TreasuriesAcc.ToString())
                {
                    OpenTreasuryForm();
                }
                else if (parentValue == Common.CurrentBranch.BanksAcc.ToString())
                {
                    OpenBankForm();
                }
                else if (parentValue == Common.CurrentBranch.CustomersAcc.ToString())
                {
                    OpenCustomerForm();
                }
                else if (parentValue == Common.CurrentBranch.SupliersAcc.ToString())
                {
                    OpenSupplierForm();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenCardForm Error: {ex.Message}");
            }
        }

        /// <summary>
        /// فتح نافذة الخزائن
        /// </summary>
        private void OpenTreasuryForm()
        {
            try
            {
                
                var treasuryForm = new frmTreasury();
                MainClass.ApplyPermissionToForm(treasuryForm);
                MainClass.DoApplyUserSett(treasuryForm);
                treasuryForm.ShowDialog();
                
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenTreasuryForm Error: {ex.Message}");
            }
        }

        /// <summary>
        /// فتح نافذة البنوك
        /// </summary>
        private void OpenBankForm()
        {
            try
            {
                
                var bankForm = new frmBanks();
                MainClass.ApplyPermissionToForm(bankForm);
                MainClass.DoApplyUserSett(bankForm);
                bankForm.ShowDialog();
                
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenBankForm Error: {ex.Message}");
            }
        }

        /// <summary>
        /// فتح نافذة العملاء
        /// </summary>
        private void OpenCustomerForm()
        {
            try
            {
                
                var customerForm = new frmCustomers();
                customerForm.Title = GetLocalizedMessage("DefineCustomer", "تعريف عميل");
                customerForm.Type = 1;
                customerForm.ShowDialog();
                
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenCustomerForm Error: {ex.Message}");
            }
        }

        /// <summary>
        /// فتح نافذة الموردين
        /// </summary>
        private void OpenSupplierForm()
        {
            try
            {
                
                var supplierForm = new frmCustomers();
                supplierForm.Title = GetLocalizedMessage("DefineSupplier", "تعريف مورد");
                supplierForm.Type = 2;
                supplierForm.ShowDialog();
                
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenSupplierForm Error: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على اسم المستخدم
        /// </summary>
        private string GetUserName(string empId)
        {
            try
            {
                if (string.IsNullOrEmpty(empId))
                    return "";

                string query = $"SELECT username FROM Users WHERE IS_Deleted=0 AND emp={empId}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return dt.Rows[0]["username"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetUserName Error: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// الحصول على اسم الموظف
        /// </summary>
        private string GetEmpName(int emp)
        {
            try
            {
                string query = $"SELECT name FROM Employees WHERE id={emp}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        return dt.Rows[0]["name"]?.ToString() ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"GetEmpName Error: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// الحصول على رسالة محلية (عربي/إنجليزي)
        /// </summary>
        private string GetLocalizedMessage(string key, string defaultArabic)
        {
            if (string.IsNullOrEmpty(MainClass.Language) || MainClass.Language == "ar")
            {
                return defaultArabic;
            }

            // يمكن إضافة ملف موارد للرسائل الإنجليزية
            Dictionary<string, string> englishMessages = new Dictionary<string, string>
            {
                { "EnterAccountCode", "Enter account code" },
                { "EnterAccountName", "Enter Account Name" },
                { "SelectBranch", "You should enter the branch" },
                { "CodeExists", "Account code is previously inserted, Enter another code" },
                { "ParentNotFound", "Parent code not found" },
                { "ParentMustBeMain", "The parent code is sub account, it must be main account" },
                { "EnterParentCode", "Enter the parent code" },
                { "SavedSuccessfully", "Saved" },
                { "Save", "Save" },
                { "SelectAccountFirst", "Choose account to be deleted" },
                { "AccountHasTransactions", "This Account previously used" },
                { "AccountNotFound", "This account not found" },
                { "AccountHasSubAccounts", "Account can not be deleted, it has sub accounts" },
                { "DeleteFromCard", "Account should deleted from account card" },
                { "DeletedSuccessfully", "Deleted" },
                { "Delete", "Delete" },
                { "DefineCustomer", "Define A Customer" },
                { "DefineSupplier", "Define A Supplier" }
            };

            if (englishMessages.ContainsKey(key))
            {
                return englishMessages[key];
            }

            return defaultArabic;
        }

        #endregion

        #region Load Existing Account (للتعديل)

        /// <summary>
        /// تحميل بيانات حساب موجود للتعديل
        /// </summary>
        public void LoadAccount(int accountCode)
        {
            try
            {
                Clear();

                string query = $"SELECT * FROM Accounts_Index WHERE Code={accountCode}";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0)
                        return;

                    DataRow row = dt.Rows[0];

                    _id = Convert.ToInt32(row["Code"]);
                    Code.Text = row["Code"].ToString();
                    AName.Text = row["AName"].ToString();

                    if (row["ParentCode"] != DBNull.Value)
                    {
                        ParentCode.SelectedValue = Convert.ToInt32(row["ParentCode"]);
                    }

                    if (row["Acc_branch"] != DBNull.Value)
                    {
                        cmbBranches.SelectedValue = Convert.ToInt32(row["Acc_branch"]);
                    }
                    else
                    {
                        cmbBranches.SelectedValue = MainClass.BranchNo;
                    }

                    // نوع الحساب
                    int accountType = Convert.ToInt32(row["Type"]);
                    if (accountType == 1)
                    {
                        main.IsChecked = true;
                        GBbranchAcc.Visibility = Visibility.Collapsed;
                    }
                    else if (accountType == 2)
                    {
                        sub1.IsChecked = true;
                        GBbranchAcc.Visibility = Visibility.Visible;

                        // طبيعة الحساب
                        int nature = Convert.ToInt32(row["Nature"]);
                        if (nature == 1)
                        {
                            debt.IsChecked = true;
                        }
                        else
                        {
                            credit.IsChecked = true;
                        }

                        // التاريخ والمستخدم
                        if (row["Date"] != DBNull.Value)
                        {
                            txtDate.DateTime = Convert.ToDateTime(row["Date"]);
                        }

                        if (row["UserName"] != DBNull.Value)
                        {
                            User.Text = GetUserName(row["UserName"].ToString());
                        }

                        // الرصيد الافتتاحي
                        if (row["IValue"] != DBNull.Value)
                        {
                            IValue.Text = row["IValue"].ToString();
                        }

                        // مركز التكلفة
                        if (row["CostCenter"] != DBNull.Value)
                        {
                            cmbCostCenter.SelectedValue = Convert.ToInt32(row["CostCenter"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadAccount Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل بيانات الحساب: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}