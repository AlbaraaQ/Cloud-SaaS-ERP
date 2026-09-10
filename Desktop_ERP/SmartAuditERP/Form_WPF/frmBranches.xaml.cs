using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using SmartAuditERP.Form_WPF;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة تعريف وإدارة الفروع
    /// </summary>
    public partial class frmBranches : Window
    {
        #region ── Fields ──────────────────────────────────────────────────

        private SqlConnection _connection;
        private SqlConnection _connection2;

        private int _currentCode;
        private int _currentBranchId;
        private string _currentBranchCode;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmBranches()
        {
            InitializeComponent();

            _connection = MainClass.ConnObj();
            _connection2 = MainClass.ConnObj();
            _currentCode = -1;
            _currentBranchId = 0;
            _currentBranchCode = "0";
        }

        #endregion

        #region ── Window Events ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBranchesGrid();
            LoadNextBranchNumber();
            LoadAccountsComboBoxes();
            LoadCostCenters();
            ApplyAccountPermissions();
        }

        #endregion

        #region ── Helpers ─────────────────────────────────────────────────

        /// <summary>
        /// تطبيق صلاحيات الحسابات بناءً على رقم الموظف
        /// </summary>
        private void ApplyAccountPermissions()
        {
            bool isAdmin = MainClass.EmpNo == 0;

            cmbBankAcc.IsEnabled = isAdmin;
            cmbCustAcc.IsEnabled = isAdmin;
            cmbInventoryAcc.IsEnabled = isAdmin;
            cmbSuplierAcc.IsEnabled = isAdmin;
            cmbTreasury.IsEnabled = isAdmin;
            cmbAppAcc.IsEnabled = isAdmin;
        }

        /// <summary>
        /// مسح الحقول وإعادة التهيئة
        /// </summary>
        private void ClearForm()
        {
            txtName.Text = string.Empty;
            txtCode.Text = string.Empty;
            txtTel.Text = string.Empty;
            txtMobile.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtAddress.Text = string.Empty;
            txtNotes.Text = string.Empty;

            chkActive.IsChecked = true;
            ckDefaultBranch.IsChecked = false;

            _currentCode = -1;

            LoadNextBranchNumber();
            LoadCostCenters();

            cmbBankAcc.SelectedIndex = -1;
            cmbCustAcc.SelectedIndex = -1;
            cmbInventoryAcc.SelectedIndex = -1;
            cmbSuplierAcc.SelectedIndex = -1;
            cmbTreasury.SelectedIndex = -1;
            cmbCostCenter.SelectedIndex = -1;
            cmbAppAcc.SelectedIndex = -1;
            cmbsalary.SelectedIndex = -1;

            dgvBranches.UnselectAll();
            UpdateRecordIndicator("📍 سجل جديد");
        }

        /// <summary>
        /// تحديث مؤشر السجل الحالي
        /// </summary>
        private void UpdateRecordIndicator(string text)
        {
            txtRecordIndicator.Text = text;
        }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        /// <summary>
        /// تحميل بيانات الفروع في الجدول
        /// </summary>
        private void LoadBranchesGrid()
        {
            try
            {
                EnsureConnectionOpen(_connection);

                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Branches WHERE IS_Deleted = 0 ORDER BY id",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                var branchList = new List<BranchGridRow>();

                foreach (DataRow row in dataTable.Rows)
                {
                    branchList.Add(new BranchGridRow
                    {
                        BranchId = Convert.ToInt32(row["BranchId"]),
                        BranchName = row["name"]?.ToString() ?? string.Empty,
                        BranchTel = row["tel"]?.ToString() ?? string.Empty,
                        BranchMobile = row["mobile"]?.ToString() ?? string.Empty,
                        Fax = row["fax"]?.ToString() ?? string.Empty,
                        BranchEmail = row["email"]?.ToString() ?? string.Empty,
                    });
                }

                dgvBranches.ItemsSource = branchList;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل بيانات الفروع", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        /// <summary>
        /// الحصول على رقم الفرع التالي من قاعدة البيانات
        /// </summary>
        private int GetNextBranchNumber()
        {
            using var tempConnection = MainClass.ConnObj();
            EnsureConnectionOpen(tempConnection);

            var command = new SqlCommand(
                "SELECT ISNULL(MAX(BranchId), 0) FROM Branches WHERE IS_Deleted = 0",
                tempConnection);

            int nextNumber = Convert.ToInt32(command.ExecuteScalar()) + 1;
            EnsureConnectionClosed(tempConnection);
            return nextNumber;
        }

        /// <summary>
        /// تحميل رقم الفرع التالي في حقل الرمز
        /// </summary>
        private void LoadNextBranchNumber()
        {
            int nextNumber = GetNextBranchNumber();
            _currentBranchCode = nextNumber.ToString();
            _currentBranchId = nextNumber;
            txtCode.Text = _currentBranchCode;
        }

        /// <summary>
        /// تحميل الحسابات في الـ ComboBoxes
        /// </summary>
        private void LoadAccountsComboBoxes()
        {
            try
            {
                EnsureConnectionOpen(_connection);

                var adapter = new SqlDataAdapter(
                    "SELECT AName, Code FROM Accounts_Index WHERE Type = 1",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                BindComboBox(cmbCustAcc, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbSuplierAcc, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbBankAcc, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbInventoryAcc, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbTreasury, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbAppAcc, dataTable.Copy(), "AName", "Code");
                BindComboBox(cmbsalary, dataTable.Copy(), "AName", "Code");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل الحسابات", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        /// <summary>
        /// ربط ComboBox بمصدر بيانات
        /// </summary>
        private static void BindComboBox(
            ComboBox comboBox,
            DataTable dataTable,
            string displayMember,
            string valueMember)
        {
            comboBox.DisplayMemberPath = displayMember;
            comboBox.SelectedValuePath = valueMember;
            comboBox.ItemsSource = dataTable.DefaultView;
            comboBox.SelectedIndex = -1;
        }

        /// <summary>
        /// تحميل مراكز التكلفة
        /// </summary>
        private void LoadCostCenters()
        {
            try
            {
                EnsureConnectionOpen(_connection);

                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center " +
                    "WHERE type = 2 AND Is_Deleted = 0 ORDER BY code",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.ItemsSource = dataTable.DefaultView;
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل مراكز التكلفة", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        #endregion

        #region ── Navigation ──────────────────────────────────────────────

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("SELECT TOP 1 * FROM Branches WHERE IS_Deleted = 0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM Branches " +
                $"WHERE IS_Deleted = 0 AND BranchId < {_currentCode} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM Branches " +
                $"WHERE IS_Deleted = 0 AND BranchId > {_currentCode} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("SELECT TOP 1 * FROM Branches WHERE IS_Deleted = 0 ORDER BY id DESC");
        }

        /// <summary>
        /// تنفيذ الانتقال وقراءة البيانات
        /// </summary>
        private void NavigateTo(string sqlQuery)
        {
            try
            {
                EnsureConnectionOpen(_connection);

                var command = new SqlCommand(sqlQuery, _connection);
                var dataReader = command.ExecuteReader();
                ReadDataFromReader(dataReader);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التنقل", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        #endregion

        #region ── Read Data ───────────────────────────────────────────────

        /// <summary>
        /// قراءة بيانات الفرع من SqlDataReader وملء الحقول
        /// </summary>
        private void ReadDataFromReader(SqlDataReader dataReader)
        {
            if (!dataReader.HasRows)
            {
                dataReader.Close();
                return;
            }

            dataReader.Read();
            ClearForm();

            try
            {
                _currentCode = Convert.ToInt32(
                    double.Parse(
                        string.Concat("", dataReader["BranchId"])));

                txtName.Text = string.Concat("", dataReader["name"]);
                txtTel.Text = string.Concat("", dataReader["tel"]);
                txtMobile.Text = string.Concat("", dataReader["mobile"]);
                txtEmail.Text = string.Concat("", dataReader["email"]);
                txtAddress.Text = string.Concat("", dataReader["address"]);
                txtNotes.Text = string.Concat("", dataReader["notes"]);

                _currentBranchId = Convert.ToInt32(
                    string.Concat("", dataReader["BranchId"]));
                _currentBranchCode = string.Concat("", dataReader["code"]);
                txtCode.Text = _currentBranchCode;

                SetComboBoxValue(cmbTreasury, dataReader, "TreasuriesAcc");
                SetComboBoxValue(cmbCustAcc, dataReader, "CustomersAcc");
                SetComboBoxValue(cmbSuplierAcc, dataReader, "SupliersAcc");
                SetComboBoxValue(cmbBankAcc, dataReader, "BanksAcc");

                if (dataReader["InventoryAcc"] != DBNull.Value &&
                    !string.IsNullOrEmpty(dataReader["InventoryAcc"]?.ToString()))
                {
                    cmbInventoryAcc.SelectedValue = dataReader["InventoryAcc"];
                }

                if (dataReader["AppAcc"] != DBNull.Value &&
                    !string.IsNullOrEmpty(dataReader["AppAcc"]?.ToString()))
                {
                    cmbAppAcc.SelectedValue = dataReader["AppAcc"];
                }

                ckDefaultBranch.IsChecked = Convert.ToBoolean(dataReader["IsDefault"]);

                // IsActive: 0 = نشط، 1 = غير نشط (منطق الكود الأصلي)
                chkActive.IsChecked = Convert.ToInt32(dataReader["IsActive"]) == 0;

                string costCenter = dataReader["CostCenter"]?.ToString();
                if (dataReader["CostCenter"] != DBNull.Value &&
                    !string.IsNullOrEmpty(costCenter) &&
                    costCenter != "-1")
                {
                    cmbCostCenter.SelectedValue = dataReader["CostCenter"];
                }

                if (dataReader["EmployeeAcc"] != DBNull.Value &&
                    !string.IsNullOrEmpty(dataReader["EmployeeAcc"]?.ToString()))
                {
                    cmbsalary.SelectedValue = dataReader["EmployeeAcc"];
                }

                UpdateRecordIndicator($"📍 الفرع: {txtName.Text}");
            }
            catch
            {
                // تجاهل أخطاء القراءة الجزئية
            }
            finally
            {
                dataReader.Close();
            }
        }

        /// <summary>
        /// تعيين قيمة ComboBox من DataReader إذا لم تكن DBNull
        /// </summary>
        private static void SetComboBoxValue(
            ComboBox comboBox,
            SqlDataReader reader,
            string columnName)
        {
            if (reader[columnName] != DBNull.Value)
                comboBox.SelectedValue = reader[columnName];
        }

        #endregion

        #region ── CRUD Operations ─────────────────────────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ── التحقق من صلاحية الفرع ──
                if (Sync.BranchType == 2 && Sync.SyncType > 0)
                {
                    ShowInfo("لا يمكن إضافة أو تعديل البيانات من هذا الفرع");
                    return;
                }

                // ── التحقق من إدخال الاسم ──
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowWarning("يجب إدخال اسم الفرع");
                    txtName.Focus();
                    return;
                }

                // ── بناء كائن Branch ──
                var branch = BuildBranchObject();

                // ── حفظ جديد: إنشاء الحسابات ──
                if (_currentCode == -1)
                {
                    if (!CreateDefaultAccounts(branch))
                        return;
                }
                else
                {
                    // ── تعديل: استخدام الحسابات المحددة ──
                    AssignSelectedAccounts(branch);
                }

                // ── إضافة موظفي الفرع ──
                LoadBranchEmployees(branch);

                // ── الحفظ في قاعدة البيانات ──
                var branchList = new List<Branch> { branch };
                if (new EntityOperations().SaveBranch(branchList))
                {
                    SyncAfterSave(branchList);

                    ShowSuccess("تم الحفظ بنجاح ✅");
                    ClearForm();
                    LoadBranchesGrid();
                    LoadAccountsComboBoxes();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Sync.BranchType == 2 && Sync.SyncType > 0)
                {
                    ShowInfo("لا يمكن حذف البيانات من هذا الفرع");
                    return;
                }

                if (_currentCode <= 1)
                {
                    ShowWarning("اختر فرعاً ليتم حذفه");
                    return;
                }

                var result = MessageBox.Show(
                    "هل أنت متأكد من حذف هذا الفرع؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(_connection);

                new SqlCommand(
                    $"UPDATE Branches SET IS_Deleted = 1 WHERE BranchId = {_currentCode}",
                    _connection).ExecuteNonQuery();

                ShowSuccess("تم الحذف بنجاح 🗑️");
                LoadBranchesGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Grid Events ─────────────────────────────────────────────

        private void dgvBranches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvBranches.SelectedItem is BranchGridRow selectedRow)
            {
                _currentCode = selectedRow.BranchId;
                NavigateTo(
                    $"SELECT * FROM Branches WHERE BranchId = {_currentCode}");
            }
        }

        #endregion

        #region ── Build Objects ───────────────────────────────────────────

        /// <summary>
        /// بناء كائن Branch من قيم الحقول الحالية
        /// </summary>
        private Branch BuildBranchObject()
        {
            var branch = new Branch
            {
                BranchId = _currentBranchId,
                Code = txtCode.Text,
                BranchName = txtName.Text,
                BranchTel = txtTel.Text,
                BranchMobile = txtMobile.Text,
                ClientCode = Sync.ClientCode,
                BranchEmail = txtEmail.Text,
                BranchAddress = txtAddress.Text,
                IsDefault = ckDefaultBranch.IsChecked == true,
                Notes = txtNotes.Text,
                CreateDate = DateTime.Now,
                LastUpdateDate = DateTime.Now,
                IsDeleted = false,
                // IsActive: false = نشط (منطق الكود الأصلي المعكوس)
                IsActive = chkActive.IsChecked != true,
                CostCenter = cmbCostCenter.SelectedIndex > -1
                                    ? cmbCostCenter.SelectedValue?.ToString()
                                    : "-1"
            };

            return branch;
        }

        /// <summary>
        /// إنشاء الحسابات الافتراضية عند إضافة فرع جديد
        /// </summary>
        private bool CreateDefaultAccounts(Branch branch)
        {
            string customersCode = MainClass.GenerateCode(123);
            string suppliersCode = MainClass.GenerateCode(2211);
            string banksCode = MainClass.GenerateCode(122);
            string inventoryCode = string.Empty;
            string treasuriesCode = MainClass.GenerateCode(121);
            string appAccountCode = MainClass.GenerateCode(12);
            string employeeAccCode = MainClass.GenerateCode(22);

            branch.CustomersAcc = customersCode;
            branch.SupliersAcc = suppliersCode;
            branch.BanksAcc = banksCode;
            branch.InventoryAcc = inventoryCode;
            branch.TreasuriesAcc = treasuriesCode;
            branch.EmployeeAcc = employeeAccCode;

            string branchName = txtName.Text;

            var accountsList = new List<TreeAccount>
            {
                BuildTreeAccount($"عملاء {branchName}",  customersCode,  "123"),
                BuildTreeAccount($"موردين {branchName}",  suppliersCode,  "2211"),
                BuildTreeAccount($"بنوك {branchName}",    banksCode,      "122"),
                BuildTreeAccount($"تطبيقات {branchName}", appAccountCode, "12"),
                BuildTreeAccount($"صندوق {branchName}",   treasuriesCode, "121"),
            };

            if (!new EntityOperations().SaveAccounts(accountsList))
            {
                ShowError("خطأ أثناء إنشاء الحسابات الافتراضية");
                return false;
            }

            return true;
        }

        /// <summary>
        /// بناء كائن TreeAccount
        /// </summary>
        private TreeAccount BuildTreeAccount(
            string accName,
            string code,
            string parentCode)
        {
            return new TreeAccount
            {
                AccName = accName,
                AccNature = 1,
                Code = code,
                ParentCode = parentCode,
                IntialBalance = 0m,
                EmpId = MainClass.EmpNo,
                AccType = 1,
                IsDeleted = false,
                BranchId = _currentBranchId,
                CreateDate = DateTime.Now,
                ClientCode = Sync.ClientCode,
                LastUpdateDate = DateTime.Now
            };
        }

        /// <summary>
        /// تعيين الحسابات المحددة للفرع عند التعديل
        /// </summary>
        private void AssignSelectedAccounts(Branch branch)
        {
            branch.CustomersAcc = cmbCustAcc.SelectedValue?.ToString() ?? string.Empty;
            branch.SupliersAcc = cmbSuplierAcc.SelectedValue?.ToString() ?? string.Empty;
            branch.BanksAcc = cmbBankAcc.SelectedValue?.ToString() ?? string.Empty;
            branch.InventoryAcc = string.Empty;
            branch.TreasuriesAcc = cmbTreasury.SelectedValue?.ToString() ?? string.Empty;
            branch.AppAccount = cmbAppAcc.SelectedIndex > -1
                                       ? cmbAppAcc.SelectedValue?.ToString()
                                       : string.Empty;
            branch.EmployeeAcc = cmbsalary.SelectedIndex > -1
                                       ? cmbsalary.SelectedValue?.ToString()
                                       : string.Empty;
        }

        /// <summary>
        /// تحميل موظفي الفرع وإضافتهم للكائن
        /// </summary>
        private void LoadBranchEmployees(Branch branch)
        {
            try
            {
                EnsureConnectionOpen(_connection);

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM EmpBranches WHERE branch = {branch.BranchId}",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    branch.Employees.Add(new BranchEmployee
                    {
                        BranchId = branch.BranchId,
                        ClientCode = branch.ClientCode,
                        EmployeeId = row["emp"]?.ToString() ?? string.Empty
                    });
                }
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        #endregion

        #region ── Sync & Broker ───────────────────────────────────────────

        /// <summary>
        /// مزامنة البيانات بعد الحفظ
        /// </summary>
        private void SyncAfterSave(List<Branch> branchList)
        {
            try
            {
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    new BranchCRUD(Sync.APIUrl).AddBranch(branchList);
                var Home = new Home();
                if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                {
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "Branches",
                        Encoding.UTF8.GetBytes(SendData.GetBranches()),
                        0,
                        retain: true);
                }
            }
            catch
            {
                // تجاهل أخطاء المزامنة - لا توقف الحفظ
            }
        }

        #endregion

        #region ── Connection Helpers ──────────────────────────────────────

        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private static void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region ── Message Helpers ─────────────────────────────────────────

        private static void ShowError(string message, Exception ex = null)
        {
            string detail = ex != null
                ? $"{Environment.NewLine}تفاصيل الخطأ: {ex.Message}"
                : string.Empty;

            MessageBox.Show(
                message + detail,
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(
                message,
                "تنبيه",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(
                message,
                "معلومة",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static void ShowSuccess(string message)
        {
            MessageBox.Show(
                message,
                "نجاح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }

    #region ── Helper Classes ──────────────────────────────────────────────

    /// <summary>
    /// نموذج عرض صف الفرع في الـ DataGrid
    /// </summary>
    public class BranchGridRow
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchTel { get; set; }
        public string BranchMobile { get; set; }
        public string Fax { get; set; }
        public string BranchEmail { get; set; }
    }

    #endregion
}