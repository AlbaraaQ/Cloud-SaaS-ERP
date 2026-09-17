using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Microsoft.Win32;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEmployees : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;
        private int _currentCode;
        public bool IsDone;
        private List<BranchRowData> _employeeBranches = new List<BranchRowData>();

        #endregion

        #region Constructor

        public frmEmployees()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _currentCode = -1;
            IsDone = false;

            Loaded += FrmEmployees_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmEmployees_Loaded(object sender, RoutedEventArgs e)
        {
            txtBirthDate.DateTime = DateTime.Now;
            txtWorkDate.DateTime = DateTime.Now;

            ClearForm();
            LoadManagements();
            LoadJobs();
            LoadMaritalStatuses();
            LoadStatuses();
            LoadNationalities();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnNew.Click += (s, e) => ClearForm();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClose.Click += (s, e) => Close();
            btnFirst.Click += BtnFirst_Click;
            btnPrevious.Click += BtnPrevious_Click;
            btnNext.Click += BtnNext_Click;
            btnLast.Click += BtnLast_Click;
            btnSearch.Click += BtnSearch_Click;

            btnManagAdd.Click += BtnManagAdd_Click;
            btnDepAdd.Click += BtnDepAdd_Click;
            btnJobAdd.Click += BtnJobAdd_Click;
            btnNationalityAdd.Click += BtnNationalityAdd_Click;

            cmbManags.EditValueChanged += CmbManags_EditValueChanged;

            txtNameSrch.KeyDown += TxtNameSrch_KeyDown;
            txtBasicSalary.TextChanged += TxtSalary_Changed;
            txtHouse.TextChanged += TxtSalary_Changed;
            txtTravel.TextChanged += TxtSalary_Changed;
            txtFood.TextChanged += TxtSalary_Changed;
            txtMedical.TextChanged += TxtSalary_Changed;
            txtSalaryAdd.TextChanged += TxtSalary_Changed;
            txtSalaryOther.TextChanged += TxtSalary_Changed;
        }

        #endregion

        #region Clear Form

        private void ClearForm()
        {
            _currentCode = -1;

            txtName.Text = string.Empty;
            txtAccCode.Text = MaxId("Code", "Accounts_Index",
                                     Common.CurrentBranch.EmployeeAcc).ToString();
            txtInsuranceNo.Text = string.Empty;
            txtTel.Text = string.Empty;
            txtMobile.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtAddress.Text = string.Empty;
            txtNotes.Text = string.Empty;
            txtBasicSalary.Text = "0";
            txtHouse.Text = "0";
            txtTravel.Text = "0";
            txtFood.Text = "0";
            txtMedical.Text = "0";
            txtSalaryAdd.Text = "0";
            txtSalaryOther.Text = "0";
            txtSalaryTotal.Text = "0";

            TxtCard.Text = string.Empty;
            TxtBankNo.Text = string.Empty;
            TxtBankName.Text = string.Empty;

            cmbManags.EditValue = null;
            cmbDeps.EditValue = null;
            cmbStatus.EditValue = null;
            cmbJob.EditValue = null;
            cmbMaritalStatus.EditValue = null;
            cmbNationality.EditValue = null;

            rdMale.IsChecked = true;
            picImage.Source = null;

            txtBirthDate.DateTime = DateTime.Now;
            txtWorkDate.DateTime = DateTime.Now;

            dgvBranches.Items.Clear();
            LoadBranches();

            txtCurrentEmpId.Text = "--";
            txtRecordIndicator.Text = "--";
        }

        #endregion

        #region Load Lookup Data

        public void LoadManagements()
        {
            FillComboBox(cmbManags,
                "SELECT id, name FROM Managements ORDER BY id",
                "name", "id");
        }

        public void LoadDeps(int managId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Departments WHERE manag_id={managId} ORDER BY id",
                    _conn1);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbDeps.ItemsSource = dataTable.DefaultView;
                cmbDeps.DisplayMember = "name";
                cmbDeps.ValueMember = "id";
                cmbDeps.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadJobs()
        {
            FillComboBox(cmbJob,
                "SELECT id, name FROM jobs ORDER BY id",
                "name", "id");
        }

        public void LoadBranches()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches ORDER BY BranchId",
                    _conn);

                var branchTable = new DataTable();
                adapter.Fill(branchTable);

                // تعيين مصدر بيانات عمود الفرع في DataGrid
                if (dgvBranches.Columns.Count > 0 &&
                    dgvBranches.Columns[0] is DataGridComboBoxColumn col)
                {
                    col.ItemsSource = branchTable.DefaultView;
                    col.DisplayMemberPath = "name";
                    col.SelectedValuePath = "BranchId";
                }

                // تعبئة صفوف الفروع الخاصة بالموظف
                _employeeBranches.Clear();

                if (branchTable.Rows.Count > 0)
                {
                    _employeeBranches.Add(new BranchRowData
                    {
                        BranchId = MainClass.BranchNo
                    });
                }

                dgvBranches.ItemsSource = null;
                dgvBranches.ItemsSource = _employeeBranches;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadNationalities()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, nationality FROM Countries ORDER BY id", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbNationality.ItemsSource = dataTable.DefaultView;
                cmbNationality.DisplayMember = "nationality";
                cmbNationality.ValueMember = "id";
                cmbNationality.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadMaritalStatuses()
        {
            FillComboBox(cmbMaritalStatus,
                "SELECT id, name FROM Marital_status ORDER BY id",
                "name", "id");
        }

        public void LoadStatuses()
        {
            FillComboBox(cmbStatus,
                "SELECT id, name FROM States ORDER BY id",
                "name", "id");
        }

        private void FillComboBox(
            ComboBoxEdit combo, string query,
            string display, string value)
        {
            try
            {
                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                combo.ItemsSource = dataTable.DefaultView;
                combo.DisplayMember = display;
                combo.ValueMember = value;
                combo.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Load Grid (Search)

        private void LoadGrid(string condition)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name, work_date, mobile, " +
                    $"salary_basic, salary_add, salary_other, " +
                    $"house, travel, food, medical " +
                    $"FROM Employees WHERE {condition} IS_Deleted=0 ORDER BY id",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                var display = new DataTable();
                display.Columns.Add("EmpId", typeof(int));
                display.Columns.Add("EmpName", typeof(string));
                display.Columns.Add("TotalSalary", typeof(double));
                display.Columns.Add("WorkDate", typeof(string));
                display.Columns.Add("Mobile", typeof(string));

                foreach (DataRow row in dataTable.Rows)
                {
                    double total =
                        ToDouble(row["salary_basic"]) +
                        ToDouble(row["salary_add"]) +
                        ToDouble(row["salary_other"]) +
                        ToDouble(row["house"]) +
                        ToDouble(row["travel"]) +
                        ToDouble(row["food"]) +
                        ToDouble(row["medical"]);

                    display.Rows.Add(
                        row["id"],
                        row["name"],
                        total,
                        Convert.ToDateTime(row["work_date"])
                               .ToShortDateString(),
                        row["mobile"]);
                }

                dgvEmps.ItemsSource = display.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Navigate & Read Data

        public void Navigate(string sql)
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                using var cmd = new SqlCommand(sql, _conn);
                using var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) return;

            reader.Read();
            ClearForm();

            _currentCode = Convert.ToInt32(reader["id"]);
            txtCurrentEmpId.Text = _currentCode.ToString();
            txtRecordIndicator.Text = _currentCode.ToString();

            // Account Code
            txtAccCode.Text = reader["AccCode"] != DBNull.Value
                ? reader["AccCode"].ToString() : string.Empty;

            // Bank Info
            TxtCard.Text = reader["CardNo"] != DBNull.Value ? reader["CardNo"].ToString() : string.Empty;
            TxtBankNo.Text = reader["BankNo"] != DBNull.Value ? reader["BankNo"].ToString() : string.Empty;
            TxtBankName.Text = reader["BankName"] != DBNull.Value ? reader["BankName"].ToString() : string.Empty;

            // Basic Info
            txtName.Text = reader["name"].ToString();
            txtInsuranceNo.Text = reader["insurance_no"].ToString();

            try { txtBirthDate.DateTime = Convert.ToDateTime(reader["birth_date"]); }
            catch { /* ignore */ }

            try { txtWorkDate.DateTime = Convert.ToDateTime(reader["work_date"]); }
            catch { /* ignore */ }

            // Combos
            try
            {
                if (!reader["manag"].Equals(-1))
                {
                    cmbManags.EditValue = reader["manag"];
                    LoadDeps(Convert.ToInt32(reader["manag"]));
                }
            }
            catch { /* ignore */ }

            try { if (!reader["dep"].Equals(-1)) cmbDeps.EditValue = reader["dep"]; }
            catch { /* ignore */ }

            try { if (!reader["state"].Equals(-1)) cmbStatus.EditValue = reader["state"]; }
            catch { /* ignore */ }

            try { if (!reader["job"].Equals(-1)) cmbJob.EditValue = reader["job"]; }
            catch { /* ignore */ }

            try { if (!reader["marital_state"].Equals(-1)) cmbMaritalStatus.EditValue = reader["marital_state"]; }
            catch { /* ignore */ }

            try { if (!reader["nationality"].Equals(-1)) cmbNationality.EditValue = reader["nationality"]; }
            catch { /* ignore */ }

            // Gender
            rdMale.IsChecked = reader["sex"].ToString() == "M";
            rdFemale.IsChecked = reader["sex"].ToString() != "M";

            // Contact
            txtTel.Text = reader["tel"].ToString();
            txtMobile.Text = reader["mobile"].ToString();
            txtEmail.Text = reader["email"].ToString();
            txtAddress.Text = reader["address"].ToString();
            txtNotes.Text = reader["notes"].ToString();

            // Image
            try
            {
                if (reader["image"] != DBNull.Value &&
                    reader["image"] is byte[] imgBytes)
                {
                    picImage.Source = ByteArrayToBitmapImage(imgBytes);
                }
            }
            catch { /* ignore */ }

            // Salary
            txtBasicSalary.Text = reader["salary_basic"].ToString();
            txtSalaryAdd.Text = reader["salary_add"].ToString();
            txtSalaryOther.Text = reader["salary_other"].ToString();

            try
            {
                txtHouse.Text = reader["house"].ToString();
                txtTravel.Text = reader["travel"].ToString();
                txtFood.Text = reader["food"].ToString();
                txtMedical.Text = reader["medical"].ToString();
            }
            catch { /* ignore */ }

            reader.Close();

            // Load Branches
            try
            {
                var branchAdapter = new SqlDataAdapter(
                    $"SELECT * FROM EmpBranches WHERE emp={_currentCode}",
                    _conn);
                var branchTable = new DataTable();
                branchAdapter.Fill(branchTable);

                dgvBranches.Items.Clear();
                foreach (DataRow row in branchTable.Rows)
                {
                    dgvBranches.Items.Add(
                        new BranchRowData
                        {
                            BranchId = Convert.ToInt32(row["branch"])
                        });
                }
            }
            catch { /* ignore */ }

            CalculateTotalSalary();
        }

        #endregion

        #region Salary Calculation

        private void TxtSalary_Changed(object sender, TextChangedEventArgs e)
        {
            CalculateTotalSalary();
        }

        private void CalculateTotalSalary()
        {
            try
            {
                double total =
                    ToDouble(txtBasicSalary.Text) +
                    ToDouble(txtSalaryAdd.Text) +
                    ToDouble(txtSalaryOther.Text) +
                    ToDouble(txtHouse.Text) +
                    ToDouble(txtTravel.Text) +
                    ToDouble(txtFood.Text) +
                    ToDouble(txtMedical.Text);

                txtSalaryTotal.Text = total.ToString("N2");
            }
            catch { /* ignore */ }
        }

        #endregion

        #region Save

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string empName = txtName.Text.Trim();

                if (string.IsNullOrEmpty(empName))
                {
                    ShowWarning(MainClass.Language == "ar"
                        ? "يجب إدخال اسم الموظف"
                        : "Enter employee name");
                    txtName.Focus();
                    return;
                }

                if (_currentCode == -1)
                    _currentCode = Common.GetEmployeeId();

                int managId = cmbManags.EditValue != null
                    ? Convert.ToInt32(cmbManags.EditValue) : -1;
                int depId = cmbDeps.EditValue != null
                    ? Convert.ToInt32(cmbDeps.EditValue) : -1;
                int statusId = cmbStatus.EditValue != null
                    ? Convert.ToInt32(cmbStatus.EditValue) : -1;
                int jobId = cmbJob.EditValue != null
                    ? Convert.ToInt32(cmbJob.EditValue) : -1;
                int maritalId = cmbMaritalStatus.EditValue != null
                    ? Convert.ToInt32(cmbMaritalStatus.EditValue) : -1;
                int nationalityId = cmbNationality.EditValue != null
                    ? Convert.ToInt32(cmbNationality.EditValue) : -1;
                short gender = rdFemale.IsChecked == true ? (short)2 : (short)1;

                string accCode = txtAccCode.Text.Trim();
                if (string.IsNullOrEmpty(accCode) || accCode == "0")
                    accCode = MaxId("Code", "Accounts_Index",
                                    Common.CurrentBranch.EmployeeAcc).ToString();

                var employee = new Employee
                {
                    EmpId = _currentCode,
                    EmpName = empName,
                    Department = depId,
                    Status = statusId != 0,
                    Role = jobId,
                    BrithDate = txtBirthDate.DateTime,
                    InsuranceNo = txtInsuranceNo.Text,
                    JobStartDate = txtWorkDate.DateTime,
                    LastUpdateDate = DateTime.Now,
                    MaritalStatus = maritalId,
                    Nationality = nationalityId,
                    NationalId = string.Empty,
                    Gender = gender,
                    EmpTel = txtTel.Text,
                    EmpMobile = txtMobile.Text,
                    EmpEmail = txtEmail.Text,
                    EmpAddress = txtAddress.Text,
                    ClientCode = Sync.ClientCode,
                    BranchId = MainClass.BranchNo,
                    AccCode = Convert.ToDecimal(accCode),
                    Salary = ToDecimal(txtBasicSalary.Text),
                    salary_other = ToDecimal(txtSalaryOther.Text),
                    Housing = ToDecimal(txtHouse.Text),
                    Travel = ToDecimal(txtTravel.Text),
                    Living = ToDecimal(txtFood.Text),
                    CardNo = TxtCard.Text,
                    BankNo = TxtBankNo.Text,
                    BankName = TxtBankName.Text,
                    IsDeleted = false
                };

                var branchList = new List<BranchEmployee>();

                foreach (var item in dgvBranches.Items)
                {
                    if (item is BranchRowData branchRow &&
                        branchRow.BranchId > 0)
                    {
                        branchList.Add(new BranchEmployee
                        {
                            BranchId = branchRow.BranchId,
                            EmployeeId = _currentCode.ToString(),
                            ClientCode = Sync.ClientCode
                        });
                    }
                }

                var employeeList = new List<Employee> { employee };

                if (new EntityOperations().SaveEmployee(
                        employeeList, true, branchList))
                {
                    if (Sync.ActiveSync && Sync.SyncType > 0)
                        new EmployeeCRUD().AddImployee(employeeList);

                    // حفظ في دليل الحسابات
                    var account = new TreeAccount
                    {
                        AccName = employee.EmpName,
                        AccNature = 1,
                        Code = accCode,
                        AccountID = Convert.ToInt32(accCode),
                        ParentCode = Common.CurrentBranch.EmployeeAcc,
                        IntialBalance = 0m,
                        EmpId = MainClass.EmpNo,
                        AccType = 2,
                        IsDeleted = false,
                        BranchId = employee.BranchId,
                        CreateDate = DateTime.Now,
                        ClientCode = Sync.ClientCode,
                        LastUpdateDate = DateTime.Now
                    };

                    SaveAccounts(new List<TreeAccount> { account });

                    IsDone = true;

                    DXMessageBox.Show("✅ تم الحفظ بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearForm();
                    LoadGrid(string.Empty);
                }
                else
                {
                    ShowError("خطأ أثناء الحفظ");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحفظ:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        public bool SaveAccounts(List<TreeAccount> accounts)
        {
            var conn = MainClass.ConnObj();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                foreach (var account in accounts)
                {
                    if (account == null) continue;

                    using var countCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM Accounts_Index WHERE code={account.Code}",
                        conn);
                    int count = Convert.ToInt32(countCmd.ExecuteScalar());

                    SqlCommand cmd;
                    if (count > 0)
                    {
                        cmd = new SqlCommand(
                            $"UPDATE Accounts_Index SET " +
                            $"AName=N'{account.AccName}', IValue=0, " +
                            $"IsDeleted={Convert.ToInt16(account.IsDeleted)}, " +
                            $"CostCenter={account.CostCenter} " +
                            $"WHERE Code={account.Code}",
                            conn);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            $"INSERT INTO Accounts_Index" +
                            $"(Code,AName,Type,ParentCode,FinalAcc," +
                            $"Acc_branch,Nature,IValue,UserName,date," +
                            $"CostCenter,IsDeleted) VALUES" +
                            $"(N'{account.Code}',N'{account.AccName}'," +
                            $"{account.AccType},N'{account.ParentCode}'," +
                            $"{account.FinalAcc},{account.BranchId},1,0," +
                            $"{account.EmpId},@date,{account.CostCenter},0)",
                            conn);
                        cmd.Parameters.Add("@date", SqlDbType.DateTime)
                           .Value = DateTime.Now;
                    }

                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Delete

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode == -1)
            {
                ShowWarning(MainClass.Language == "ar"
                    ? "اختر موظفاً ليتم حذفه"
                    : "Choose employee to be deleted");
                return;
            }

            var confirmResult = DXMessageBox.Show(
                "⚠️ هل أنت متأكد من حذف هذا الموظف؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                // التحقق من ارتباط الموظف بمستخدم
                if (CheckLinked(
                    "IF EXISTS(SELECT 1 FROM Users WHERE emp=@id AND IS_Deleted=0) SELECT 1 ELSE SELECT 0"))
                {
                    ShowWarning(MainClass.Language == "ar"
                        ? "لا يمكن حذف موظف مرتبط بمستخدم"
                        : "Cannot delete employee associated with User");
                    return;
                }

                // التحقق من ارتباط الموظف بفواتير
                if (CheckLinked(
                    "IF EXISTS(SELECT 1 FROM inv WHERE sales_emp=@id AND IS_Deleted=0) SELECT 1 ELSE SELECT 0"))
                {
                    ShowWarning(MainClass.Language == "ar"
                        ? "لا يمكن حذف موظف مرتبط بفواتير"
                        : "Cannot delete employee associated with invoices");
                    return;
                }

                new SqlCommand(
                    $"UPDATE Employees SET IS_Deleted=1 WHERE id={_currentCode}",
                    _conn).ExecuteNonQuery();

                DXMessageBox.Show(
                    MainClass.Language == "ar" ? "✅ تم الحذف" : "✅ Deleted",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadGrid(string.Empty);
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحذف:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private bool CheckLinked(string checkQuery)
        {
            try
            {
                using var cmd = new SqlCommand(checkQuery, _conn);
                cmd.Parameters.AddWithValue("@id", _currentCode);
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Navigation

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Employees WHERE IS_Deleted=0 ORDER BY id ASC");

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate(
               $"SELECT TOP 1 * FROM Employees WHERE IS_Deleted=0 AND id<{_currentCode} ORDER BY id DESC");

        private void BtnNext_Click(object sender, RoutedEventArgs e)
            => Navigate(
               $"SELECT TOP 1 * FROM Employees WHERE IS_Deleted=0 AND id>{_currentCode} ORDER BY id ASC");

        private void BtnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Employees WHERE IS_Deleted=0 ORDER BY id DESC");

        #endregion

        #region Grid Events

        private void DgvEmps_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (dgvEmps.SelectedItem is DataRowView rowView)
            {
                int selectedId = Convert.ToInt32(rowView["EmpId"]);
                Navigate($"SELECT * FROM Employees WHERE id={selectedId}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Search

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void TxtNameSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search();
        }

        private void Search()
        {
            string filter = string.IsNullOrWhiteSpace(txtNameSrch.Text)
                ? string.Empty
                : $"name LIKE N'%{txtNameSrch.Text}%' AND ";

            LoadGrid(filter);
        }

        #endregion

        #region ComboBox Events

        private void CmbManags_EditValueChanged(
            object sender, EditValueChangedEventArgs e)
        {
            try
            {
                if (cmbManags.EditValue != null)
                    LoadDeps(Convert.ToInt32(cmbManags.EditValue));
            }
            catch { /* ignore */ }
        }

        #endregion

        #region Add Lookup Items

        private void BtnManagAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = cmbManags.EditValue != null
                ? Convert.ToInt32(cmbManags.EditValue) : -1;

            var form = new frmManagement();
            form.Activate();
            form.ShowDialog();

            LoadManagements();
            if (previousId != -1)
                cmbManags.EditValue = previousId;
        }

        private void BtnDepAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbManags.EditValue == null || string.IsNullOrWhiteSpace(cmbManags.EditValue.ToString()))
            {
                ShowWarning(MainClass.Language == "ar"
                    ? "يجب اختيار الإدارة أولاً"
                    : "You must select management first");
                return;
            }

            int previousDepartmentId = -1;
            if (cmbDeps.EditValue != null)
            {
                int.TryParse(cmbDeps.EditValue.ToString(), out previousDepartmentId);
            }

            object selectedManagementId = cmbManags.EditValue;

            var form = new frmDepartments();
            form.LoadManagements();

            // في DevExpress WPF استخدم EditValue بدل SelectedValue
            form.cmbManags.EditValue = selectedManagementId;

            form.Owner = System.Windows.Window.GetWindow(this);
            form.ShowDialog();

            if (int.TryParse(selectedManagementId.ToString(), out int managementId))
            {
                LoadDeps(managementId);

                if (previousDepartmentId != -1)
                {
                    cmbDeps.EditValue = previousDepartmentId;
                }
            }
        }

        private void BtnJobAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = cmbJob.EditValue != null
                ? Convert.ToInt32(cmbJob.EditValue) : -1;

            var form = new frmJobs();
            form.Activate();
            form.ShowDialog();

            LoadJobs();
            if (previousId != -1)
                cmbJob.EditValue = previousId;
        }

        private void BtnNationalityAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = cmbNationality.EditValue != null
                ? Convert.ToInt32(cmbNationality.EditValue) : -1;

            var form = new frmCountries();
            form.Activate();
            form.ShowDialog();

            LoadNationalities();
            if (previousId != -1)
                cmbNationality.EditValue = previousId;
        }

        #endregion

        #region Image Handling

        private void LnkImgAdd_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
                };

                if (dialog.ShowDialog() == true)
                    picImage.Source = new BitmapImage(new Uri(dialog.FileName));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LnkImgClr_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            picImage.Source = null;
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] bytes)
        {
            var image = new BitmapImage();
            using var ms = new System.IO.MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        #endregion

        #region Helpers

        public object MaxId(
            string column, string table, string parentCode)
        {
            try
            {
                var conn = MainClass.conn;

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                var adapter = new SqlDataAdapter(
                    $"SELECT MAX({column}) FROM {table} " +
                    $"WHERE ParentCode='{parentCode}'",
                    conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0 &&
                    dataTable.Rows[0][0] != DBNull.Value)
                    return Convert.ToInt32(dataTable.Rows[0][0]) + 1;

                return Convert.ToInt32(parentCode + "0001");
            }
            catch
            {
                return 0;
            }
        }

        private double ToDouble(object value)
        {
            return double.TryParse(value?.ToString(), out double result)
                ? result : 0.0;
        }

        private decimal ToDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result)
                ? result : 0m;
        }

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarning(string message)
        {
            DXMessageBox.Show(message, "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion
    }

    // ─── Helper Class for Branch DataGrid ─────────────────────────────────────
    public class BranchRowData
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
    }
}