using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    #region Models
    public class ProjectModel : INotifyPropertyChanged
    {
        private int _projectNo;
        private string _projectName;
        private string _status;

        public int ProjectNo
        {
            get => _projectNo;
            set { _projectNo = value; OnPropertyChanged(nameof(ProjectNo)); }
        }

        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(nameof(ProjectName)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SearchResultModel
    {
        public int Id { get; set; }
        public string ContractNo { get; set; }
        public string ContractDate { get; set; }
        public string ContractorName { get; set; }
        public string Status { get; set; }
    }

    public class CountryItemPM
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ContractorItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class StatusItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TermItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BankItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TreasuryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ClientItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    #endregion

    public partial class frmAssignProjectPM : Window
    {
        #region Fields
        private SqlConnection _dbConnection;
        private SqlConnection _dbConnection1;
        private DispatcherTimer _timer;
        private int _currentContractCode;
        private int _restrNo;
        private int _procType;
        private double _defaultVAT;
        private bool _priceIncludesVAT;

        private ObservableCollection<ProjectModel> _projects;
        private ObservableCollection<SearchResultModel> _searchResults;
        #endregion

        #region Constructor
        public frmAssignProjectPM()
        {
            InitializeComponent();

            _dbConnection = MainClass.ConnObj();
            _dbConnection1 = MainClass.ConnObj();
            _currentContractCode = -1;
            _restrNo = -1;
            _procType = 1;
            _defaultVAT = 0.0;
            _priceIncludesVAT = false;

            _projects = new ObservableCollection<ProjectModel>();
            _searchResults = new ObservableCollection<SearchResultModel>();

            dgvProjects.ItemsSource = _projects;
            dgvSrch.ItemsSource = _searchResults;

            InitializeTimer();
            Loaded += FrmAssignProjectPM_Loaded;
        }
        #endregion

        #region Initialization Region
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            txtTime.Text = DateTime.Now.ToString("hh:mm tt");
        }

        private void FrmAssignProjectPM_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadStocks();
                LoadBanks();
                LoadNextInvNo();
                LoadStatuses();
                LoadCountries();
                LoadTerms();
                LoadClients();
                LoadContractors();

                txtStartDate.SelectedDate = DateTime.Now;
                txtEndDate.SelectedDate = DateTime.Now;

                if (cmbStatus.Items.Count > 0)
                    cmbStatus.SelectedValue = 1;

                txtuser.Text = MainClass.UserName;
                txtContrNo.Focus();

                LoadMainSettings();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل البيانات", ex);
            }
        }
        #endregion

        #region Data Loading Methods Region
        private void LoadMainSettings()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=5", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                    {
                        _priceIncludesVAT = Convert.ToBoolean(dataTable.Rows[0]["PriceIncVAT"]);
                        _defaultVAT = Convert.ToDouble(dataTable.Rows[0]["MainVAT"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الإعدادات", ex);
            }
        }

        private void LoadTerms()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms WHERE IS_Deleted=0 AND Type=2 ORDER BY Code",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbTerms.ItemsSource = dataTable.AsEnumerable().Select(row => new TermItem
                        {
                            Id = Convert.ToInt32(row["Code"]),
                            Name = row["name"].ToString()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل البنود", ex);
            }
        }

        private void LoadNextInvNo()
        {
            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                    _dbConnection.Open();

                using (SqlCommand command = new SqlCommand(
                    "SELECT MAX(ContrNo) FROM PM_ContractInv", _dbConnection))
                {
                    object result = command.ExecuteScalar();
                    int nextNo = (result == null || result == DBNull.Value) ? 1 : Convert.ToInt32(result) + 1;

                    txtNo.Text = nextNo.ToString();
                    txtContrNo.Text = nextNo.ToString();
                    _currentContractCode = nextNo;
                }

                _dbConnection.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل رقم العقد", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                    _dbConnection.Close();
            }
        }

        private void LoadCountries()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Countries ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        var countries = dataTable.AsEnumerable().Select(row => new CountryItemPM
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();

                        cmbCountry.ItemsSource = countries;
                        cmbCountries.ItemsSource = countries.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الدول", ex);
            }
        }

        private void LoadStocks()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks, Stock_Emps " +
                    $"WHERE Stocks.id=Stock_Emps.stock_id AND emp_id={MainClass.EmpNo} " +
                    $"AND IS_Deleted=0 AND status<>2 ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        // cmbTreasury.ItemsSource = ... if exists in XAML
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الخزن", ex);
            }
        }

        private void LoadClients()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Customers WHERE (type=1 OR type=3) ORDER BY id",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        var clients = dataTable.AsEnumerable().Select(row => new ClientItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();

                        // Used when rbContract is checked
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل العملاء", ex);
            }
        }

        private void LoadStatuses()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM PM_Status WHERE IS_Deleted=0 ORDER BY id",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        var statuses = dataTable.AsEnumerable().Select(row => new StatusItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();

                        cmbStatus.ItemsSource = statuses;
                        cmbStatus1.ItemsSource = statuses.ToList();
                        cmbStatusSrch.ItemsSource = statuses.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الحالات", ex);
            }
        }

        private void LoadContractors()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM PM_Contractor ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        var contractors = dataTable.AsEnumerable().Select(row => new ContractorItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();

                        cmbContractor.ItemsSource = contractors;
                        cmbClientSrch.ItemsSource = contractors.ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل المقاولين", ex);
            }
        }

        private void LoadBanks()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM Banks WHERE IS_Deleted=0", _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        var banks = dataTable.AsEnumerable().Select(row => new BankItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();

                        // cmbBanks.ItemsSource = banks; if exists
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل البنوك", ex);
            }
        }

        private void LoadProjects()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtContrNo.Text))
                    return;

                _projects.Clear();

                string condition = $"ContractPK={_currentContractCode} AND ";

                if (MainClass.BranchNo != -1)
                {
                    condition += $"BranchPK={MainClass.BranchNo} AND ";
                }

                if (ckNewProjects.IsChecked == true)
                {
                    condition += "statusPK=1 AND ";
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT PM_Projects.id AS id, PM_Projects.name AS ProjName, PM_Status.name AS statusName " +
                    "FROM PM_Projects, PM_Status " +
                    $"WHERE {condition} PM_Projects.IS_Deleted=0 AND PM_Projects.statusPK=PM_Status.id " +
                    "ORDER BY PM_Projects.id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        _projects.Add(new ProjectModel
                        {
                            ProjectNo = Convert.ToInt32(row["id"]),
                            ProjectName = row["ProjName"].ToString(),
                            Status = row["statusName"].ToString()
                        });
                    }
                }

                dgvProjects.SelectedItem = null;

                // Load contract status
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT PM_Status.id AS status FROM PM_Status, PM_ContractInv " +
                    "WHERE PM_ContractInv.IS_Deleted=0 AND PM_ContractInv.statusPk=PM_Status.id " +
                    $"AND PM_ContractInv.ContrNo={Convert.ToInt32(txtContrNo.Text)}", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["status"] != DBNull.Value)
                    {
                        cmbStatus.SelectedValue = Convert.ToInt32(dataTable.Rows[0]["status"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل المشاريع", ex);
            }
        }

        private void LoadProjectDetails(int projectNo)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM PM_Projects WHERE id={projectNo} AND IS_Deleted=0",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 0)
                        return;

                    DataRow row = dataTable.Rows[0];

                    txtProjectNo.Text = row["id"].ToString();
                    txtPname.Text = row["name"].ToString();
                    txtPnameEn.Text = row["nameEN"].ToString();

                    if (row["ContractorPk"] != DBNull.Value)
                        cmbContractor.SelectedValue = Convert.ToInt32(row["ContractorPk"]);

                    if (row["statusPk"] != DBNull.Value)
                    {
                        int statusId = Convert.ToInt32(row["statusPk"]);
                        cmbStatus1.SelectedValue = (statusId == 1) ? 2 : statusId;
                    }

                    if (row["countryPk"] != DBNull.Value)
                        cmbCountry.SelectedValue = Convert.ToInt32(row["countryPk"]);

                    if (row["TermPk"] != DBNull.Value)
                        cmbTerms.SelectedValue = Convert.ToInt32(row["TermPk"]);

                    txtLocation.Text = row["location"].ToString();
                    txtPeriod.Text = row["ExecutionPeriod"].ToString();
                    txtProjCost.Text = row["cost"].ToString();

                    if (row["StartDate"] != DBNull.Value)
                        txtStartDate.SelectedDate = Convert.ToDateTime(row["StartDate"]);

                    if (row["EndDate"] != DBNull.Value)
                        txtEndDate.SelectedDate = Convert.ToDateTime(row["EndDate"]);

                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل تفاصيل المشروع", ex);
            }
        }
        #endregion

        #region Button Click Events Region
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveProject();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteProject();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("جارٍ الطباعة... 🖨️", "طباعة",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAddControctor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("فتح نموذج إضافة مقاول... 👷", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadContractors();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في إضافة مقاول", ex);
            }
        }

        private void btnAddCountry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("فتح نموذج إضافة بلد... 🌍", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCountries();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في إضافة بلد", ex);
            }
        }

        private void btnSrch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtContrNo.Text.Trim()))
            {
                _currentContractCode = Convert.ToInt32(txtContrNo.Text);
                LoadProjects();
                txtContrNo.Clear();
            }
            else
            {
                MessageBox.Show("فتح نموذج البحث عن العقود... 🔍", "بحث",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (rbContract.IsChecked == true)
            {
                SearchContracts();
            }
            else
            {
                SearchProjects();
            }
        }

        private void btnAccreditate_Click(object sender, RoutedEventArgs e)
        {
            AccreditateProject();
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM PM_ContractInv WHERE IS_Deleted=0 ORDER BY ContrNo ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            txtContrNo.Text = txtNo.Text;
            Navigate("SELECT TOP 1 * FROM PM_ContractInv WHERE IS_Deleted=0 ORDER BY ContrNo DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM PM_ContractInv WHERE IS_Deleted=0 AND ContrNo>{_currentContractCode} ORDER BY ContrNo ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM PM_ContractInv WHERE IS_Deleted=0 AND ContrNo<{_currentContractCode} ORDER BY ContrNo DESC");
        }
        #endregion

        #region Save Method Region
        private void SaveProject()
        {
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            if (_dbConnection1.State != ConnectionState.Open)
                _dbConnection1.Open();

            SqlTransaction transaction = _dbConnection.BeginTransaction();

            try
            {
                // Trial version check
                if (MainClass.IsTrial && _currentContractCode == -1)
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "SELECT id FROM Entry", _dbConnection1))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count >= 20)
                        {
                            MessageBox.Show(
                                "نأسف لقد وصلت لأقصى حد إدخال للنسخة التجريبية ⚠️\n" +
                                "يمكنك شراء البرنامج وتفعيله من خلال بيانات الدعم الفني",
                                "تنبيه",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // Validation
                if (cmbContractor.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار المقاول ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbContractor.Focus();
                    return;
                }

                if (cmbTerms.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب تحديد نوع المشروع ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbTerms.Focus();
                    return;
                }

                if (cmbCountry.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب تحديد البلد ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCountry.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtProjectNo.Text))
                {
                    MessageBox.Show("يجب تحديد المشروع أولاً ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update Contract Status
                using (SqlCommand command = new SqlCommand(
                    $"UPDATE PM_ContractInv SET statusPk=@statusPk " +
                    $"WHERE Is_Deleted=0 AND ContrNo={_currentContractCode}",
                    _dbConnection, transaction))
                {
                    command.Parameters.AddWithValue("@statusPk", 2);
                    command.ExecuteNonQuery();
                }

                // Update Project
                using (SqlCommand command = new SqlCommand(
                    "UPDATE PM_Projects SET name=@name, nameEN=@nameEN, ContractorPk=@ContractorPk, " +
                    "location=@location, countryPk=@countryPk, statusPk=@statusPk, StartDate=@StartDate, " +
                    "EndDate=@EndDate, Type=@Type, ExecutionPeriod=@ExecutionPeriod, cost=@cost, VAT=@VAT, " +
                    $"image=@image WHERE id={Convert.ToInt32(txtProjectNo.Text)}",
                    _dbConnection, transaction))
                {
                    command.Parameters.AddWithValue("@name", txtPname.Text);
                    command.Parameters.AddWithValue("@nameEN", txtPnameEn.Text);
                    command.Parameters.AddWithValue("@ContractorPk", cmbContractor.SelectedValue);
                    command.Parameters.AddWithValue("@location", txtLocation.Text);
                    command.Parameters.AddWithValue("@countryPk", cmbCountry.SelectedValue);
                    command.Parameters.AddWithValue("@statusPk", cmbStatus1.SelectedValue ?? 1);
                    command.Parameters.AddWithValue("@StartDate", txtStartDate.SelectedDate ?? DateTime.Now);
                    command.Parameters.AddWithValue("@EndDate", txtEndDate.SelectedDate ?? DateTime.Now);
                    command.Parameters.AddWithValue("@Type", cmbTerms.SelectedValue);
                    command.Parameters.AddWithValue("@ExecutionPeriod", txtPeriod.Text);
                    command.Parameters.AddWithValue("@cost", Convert.ToDouble(string.IsNullOrEmpty(txtTotCost.Text) ? "0" : txtTotCost.Text));
                    command.Parameters.AddWithValue("@VAT", Convert.ToDouble(string.IsNullOrEmpty(txtNetVAT.Text) ? "0" : txtNetVAT.Text));
                    command.Parameters.AddWithValue("@image", DBNull.Value);

                    command.ExecuteNonQuery();
                }

                transaction.Commit();

                _projects.Clear();

                MessageBox.Show("✅ تم الحفظ بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                MessageBoxResult result = MessageBox.Show(
                    "هل تريد إدخال سجل جديد؟",
                    "استفسار",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ClearAll();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    ClearAll();
                    Close();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ShowErrorMessage("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                    _dbConnection.Close();
                if (_dbConnection1.State != ConnectionState.Closed)
                    _dbConnection1.Close();
            }
        }
        #endregion

        #region Delete Method Region
        private void DeleteProject()
        {
            MessageBox.Show("وظيفة الحذف قيد التطوير 🔧", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Accreditate Method Region
        private void AccreditateProject()
        {
            if (cmbContractor.SelectedIndex == -1)
            {
                MessageBox.Show("يجب ربط المشروع بمقاول ⚠️", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbStatus1.SelectedValue != null && Convert.ToInt32(cmbStatus1.SelectedValue) > 2)
            {
                MessageBox.Show("المشروع تم تعميده سابقاً ⚠️", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "هل أنت متأكد من تعميد المشروع؟ ✅",
                "تأكيد التعميد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            if (_dbConnection1.State != ConnectionState.Open)
                _dbConnection1.Open();

            SqlTransaction transaction = _dbConnection.BeginTransaction();

            try
            {
                // Update project status to accredited (3)
                using (SqlCommand command = new SqlCommand(
                    $"UPDATE PM_Projects SET statusPk=@statusPk WHERE id={Convert.ToInt32(txtProjectNo.Text)}",
                    _dbConnection, transaction))
                {
                    command.Parameters.AddWithValue("@statusPk", 3);
                    command.ExecuteNonQuery();
                }

                // Branch condition
                string branchCondition = "";
                if (MainClass.BranchNo != -1)
                {
                    branchCondition = $" WHERE branch={MainClass.BranchNo}";
                }

                // Get contractor account
                int contractorAccount = 0;
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE type=2 AND AName='{cmbContractor.Text}'",
                    _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        contractorAccount = Convert.ToInt32(dataTable.Rows[0]["Code"]);
                    }
                }

                // Get next entry ID
                int entryId = 0;
                using (SqlCommand command = new SqlCommand(
                    $"SELECT MAX(id) FROM Entry {branchCondition}", _dbConnection, transaction))
                {
                    object result2 = command.ExecuteScalar();
                    entryId = (result2 == null || result2 == DBNull.Value) ? 1 : Convert.ToInt32(result2) + 1;
                }

                double totalCost = Convert.ToDouble(string.IsNullOrEmpty(txtTotCost.Text) ? "0" : txtTotCost.Text);

                if (totalCost != 0)
                {
                    // Insert Entry Header
                    using (SqlCommand command = new SqlCommand(
                        "INSERT INTO Entry(id, date, doc_no, type, state, notes, branch, IS_Deleted) " +
                        "VALUES(@id, @date, @doc_no, @type, @state, @notes, @branch, @IS_Deleted)",
                        _dbConnection, transaction))
                    {
                        command.Parameters.AddWithValue("@id", entryId);
                        command.Parameters.AddWithValue("@date", txtDate.SelectedDate ?? DateTime.Now);
                        command.Parameters.AddWithValue("@doc_no", _currentContractCode);
                        command.Parameters.AddWithValue("@type", 15);
                        command.Parameters.AddWithValue("@state", 1);
                        command.Parameters.AddWithValue("@notes",
                            $"قيد تنفيذ رقم:{_currentContractCode} خاصة المقاول:{cmbContractor.Text}");
                        command.Parameters.AddWithValue("@branch", MainClass.BranchNo);
                        command.Parameters.AddWithValue("@IS_Deleted", 0);
                        command.ExecuteNonQuery();
                    }

                    // Debit Entry (Projects Under Construction)
                    using (SqlCommand command = new SqlCommand(
                        "INSERT INTO Entry_sub(res_id, dept, credit, acc_no, notes, branch) " +
                        "VALUES(@res_id, @dept, @credit, @acc_no, @notes, @branch)",
                        _dbConnection, transaction))
                    {
                        command.Parameters.AddWithValue("@res_id", entryId);
                        command.Parameters.AddWithValue("@dept", totalCost);
                        command.Parameters.AddWithValue("@credit", 0);
                        command.Parameters.AddWithValue("@acc_no", 3140001); // Projects under construction account
                        command.Parameters.AddWithValue("@notes",
                            $"قيد تنفيذ رقم:{_currentContractCode} خاصة المقاول:{cmbContractor.Text}");
                        command.Parameters.AddWithValue("@branch", MainClass.BranchNo);
                        command.ExecuteNonQuery();
                    }

                    // Credit Entry (Contractor Account)
                    using (SqlCommand command = new SqlCommand(
                        "INSERT INTO Entry_sub(res_id, dept, credit, acc_no, notes, branch) " +
                        "VALUES(@res_id, @dept, @credit, @acc_no, @notes, @branch)",
                        _dbConnection, transaction))
                    {
                        command.Parameters.AddWithValue("@res_id", entryId);
                        command.Parameters.AddWithValue("@dept", 0);
                        command.Parameters.AddWithValue("@credit", totalCost);
                        command.Parameters.AddWithValue("@acc_no", contractorAccount);
                        command.Parameters.AddWithValue("@notes",
                            $"قيد تنفيذ رقم:{_currentContractCode} خاصة المقاول:{cmbContractor.Text}");
                        command.Parameters.AddWithValue("@branch", MainClass.BranchNo);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                _projects.Clear();

                MessageBox.Show("✅ تم تعميد المشروع بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                MessageBoxResult confirmResult = MessageBox.Show(
                    "هل تريد إدخال سجل جديد؟",
                    "استفسار",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    ClearAll();
                }
                else if (confirmResult == MessageBoxResult.Cancel)
                {
                    ClearAll();
                    Close();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ShowErrorMessage("خطأ أثناء التعميد", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                    _dbConnection.Close();
                if (_dbConnection1.State != ConnectionState.Closed)
                    _dbConnection1.Close();
            }
        }
        #endregion

        #region Search Methods Region
        private void SearchContracts()
        {
            try
            {
                if (!chkAll.IsChecked.Value && cmbClientSrch.SelectedValue == null)
                {
                    MessageBox.Show("اختر العميل أو الكل ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClientSrch.Focus();
                    return;
                }

                string condition = "";
                string branchCondition = "";

                if (MainClass.BranchNo != -1)
                {
                    branchCondition = $"BranchPK={MainClass.BranchNo} AND ";
                }

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                {
                    condition = branchCondition + $"PM_ContractInv.ContrNo={txtSrchNo.Text} AND ";
                }

                if (!chkAll.IsChecked.Value)
                {
                    condition += $"ClientPk={cmbClientSrch.SelectedValue} AND ";
                }

                if (cmbStatusSrch.SelectedIndex > -1)
                {
                    condition += $"statusPK={cmbStatusSrch.SelectedValue} AND ";
                }

                LoadContractSearchResults(condition);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في البحث", ex);
            }
        }

        private void SearchProjects()
        {
            try
            {
                if (!chkAll.IsChecked.Value && cmbClientSrch.SelectedValue == null)
                {
                    MessageBox.Show("اختر المقاول أو الكل ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClientSrch.Focus();
                    return;
                }

                string condition = "";

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                {
                    condition += $"PM_Projects.id={txtSrchNo.Text} AND ";
                }

                if (!chkAll.IsChecked.Value)
                {
                    condition += $"ContractorPk={cmbClientSrch.SelectedValue} AND ";
                }

                if (cmbStatusSrch.SelectedIndex > -1)
                {
                    condition += $"statusPK={cmbStatusSrch.SelectedValue} AND ";
                }

                if (cmbCountries.SelectedIndex > -1)
                {
                    condition += $"PM_Contractor.country={cmbCountries.SelectedValue} AND ";
                }

                LoadProjectSearchResults(condition);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في البحث", ex);
            }
        }

        private void LoadContractSearchResults(string condition)
        {
            try
            {
                _searchResults.Clear();

                string query = "SELECT PM_ContractInv.Contr_id, ContrNo AS NO, PM_ContractInv.Regsdate AS date, " +
                              "PM_ContractInv.statusPK, Customers.name AS cust FROM PM_ContractInv, Customers " +
                              $"WHERE PM_ContractInv.IS_Deleted=0 AND {condition} PM_ContractInv.ClientPK=Customers.id " +
                              "ORDER BY Contr_id";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        _searchResults.Add(new SearchResultModel
                        {
                            Id = Convert.ToInt32(row["NO"]),
                            ContractNo = row["NO"].ToString(),
                            ContractDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                            ContractorName = row["cust"].ToString(),
                            Status = GetStatusName(Convert.ToInt32(row["statusPK"]))
                        });
                    }
                }

                dgvSrch.SelectedItem = null;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل نتائج البحث", ex);
            }
        }

        private void LoadProjectSearchResults(string condition)
        {
            try
            {
                _searchResults.Clear();

                string query = "SELECT PM_Projects.id AS NO, PM_Projects.Regsdate AS date, PM_Projects.statusPK, " +
                              "PM_Contractor.name AS cust FROM PM_Projects, PM_Contractor " +
                              $"WHERE {condition} PM_Projects.IS_Deleted=0 AND PM_Projects.ContractorPk=PM_Contractor.id " +
                              "ORDER BY PM_Projects.id";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        _searchResults.Add(new SearchResultModel
                        {
                            Id = Convert.ToInt32(row["NO"]),
                            ContractNo = row["NO"].ToString(),
                            ContractDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                            ContractorName = row["cust"].ToString(),
                            Status = GetStatusName(Convert.ToInt32(row["statusPK"]))
                        });
                    }
                }

                dgvSrch.SelectedItem = null;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل نتائج البحث", ex);
            }
        }

        private string GetStatusName(int statusId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM PM_Status WHERE id={statusId}", _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable.Rows.Count > 0 ? dataTable.Rows[0]["name"].ToString() : "";
                }
            }
            catch
            {
                return "";
            }
        }

        private string GetTermName(int termId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM PM_Terms WHERE id={termId}", _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable.Rows.Count > 0 ? dataTable.Rows[0]["name"].ToString() : "";
                }
            }
            catch
            {
                return "";
            }
        }
        #endregion

        #region Navigation Methods Region
        private void Navigate(string query)
        {
            try
            {
                dgvSrch.SelectedItem = null;

                if (_dbConnection.State != ConnectionState.Open)
                    _dbConnection.Open();

                using (SqlCommand command = new SqlCommand(query, _dbConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        ReadContractData(reader);
                    }
                }

                _dbConnection.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في التنقل", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                    _dbConnection.Close();
            }
        }

        private void ReadContractData(SqlDataReader reader)
        {
            try
            {
                if (reader.HasRows)
                {
                    reader.Read();

                    _currentContractCode = Convert.ToInt32(reader["ContrNo"]);
                    _restrNo = Convert.ToInt32(reader["RestractionPk"]);

                    cmbStatus.SelectedValue = Convert.ToInt32(reader["statusPk"]);
                    txtNo.Text = _currentContractCode.ToString();
                    txtDate.SelectedDate = Convert.ToDateTime(reader["Regsdate"]);

                    // Load payment details if exists
                    // cmbType.SelectedIndex = Convert.ToInt32(reader["PayType"]);
                    // cmbTreasury.SelectedValue = reader["TreasuryPk"];
                    // cmbBanks.SelectedValue = reader["BankId"];

                    cmbContractor.SelectedValue = reader["ClientPK"];
                    txtTotCost.Text = reader["TotVal"].ToString();
                    txtNetVal.Text = reader["TermsCost"].ToString();
                    txtNetVAT.Text = reader["VATval"].ToString();

                    reader.Close();

                    // Load sub items
                    LoadContractSubItems();
                    CalculateTotals();
                }
                else
                {
                    ClearAll();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في قراءة بيانات العقد", ex);
            }
        }

        private void LoadContractSubItems()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM PM_ContractInvSub WHERE ContrNo={_currentContractCode}",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    // Process sub items if needed
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل عناصر العقد", ex);
            }
        }
        #endregion

        #region Calculation Methods Region
        private void CalculateTotals()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtProjCost.Text))
                {
                    txtTotCost.Text = "0";
                    txtNetVAT.Text = "0";
                    txtNetVal.Text = "0";
                    return;
                }

                double cost = Convert.ToDouble(txtProjCost.Text);
                double vatPercentage = _defaultVAT > 0 ? _defaultVAT : 15.0;
                double vatAmount = Math.Round(vatPercentage / 100.0 * cost, 2);
                double total = cost + vatAmount;

                txtTotCost.Text = $"{cost:N2}";
                txtNetVAT.Text = $"{vatAmount:N2}";
                txtNetVal.Text = $"{total:N2}";
            }
            catch
            {
                txtTotCost.Text = "0";
                txtNetVAT.Text = "0";
                txtNetVal.Text = "0";
            }
        }

        private void txtProjCost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtProjCost.Text))
            {
                txtTotCost.Text = txtProjCost.Text;
                CalculateTotals();
            }
        }
        #endregion

        #region DataGrid Events Region
        private void dgvProjects_CellClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvProjects.SelectedItem is ProjectModel selectedProject)
                {
                    LoadProjectDetails(selectedProject.ProjectNo);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحديد المشروع", ex);
            }
        }

        private void dgvSrch_CellClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvSrch.SelectedItem is SearchResultModel selectedResult)
                {
                    if (selectedResult.Id > 0)
                    {
                        TabControl1.SelectedIndex = 0;

                        if (rbContract.IsChecked == true)
                        {
                            txtContrNo.Text = selectedResult.Id.ToString();
                            btnSrch_Click(null, null);
                        }
                        else
                        {
                            LoadProjectDetails(selectedResult.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحديد السجل", ex);
            }
        }
        #endregion

        #region CheckBox Events Region
        private void ckAllProj_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckAllProj.IsChecked == true)
            {
                ckNewProjects.IsChecked = false;
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked == true)
            {
                cmbClientSrch.SelectedIndex = -1;
                cmbClientSrch.IsEnabled = false;
            }
            else
            {
                cmbClientSrch.IsEnabled = true;
            }
        }

        private void rbContract_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbContract.IsChecked == true)
            {
                lblClientSrch.Text = "العميل";
                lblcountry.Visibility = Visibility.Collapsed;
                cmbCountries.Visibility = Visibility.Collapsed;
                LoadClients();
            }
        }

        private void rbProject_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbProject.IsChecked == true)
            {
                lblClientSrch.Text = "المقاول";
                lblcountry.Visibility = Visibility.Visible;
                cmbCountries.Visibility = Visibility.Visible;
                LoadContractors();
            }
        }
        #endregion

        #region Keyboard Events Region
        private void txtContrNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSrch_Click(null, null);
            }
        }
        #endregion

        #region Helper Methods Region
        private void ClearAll()
        {
            txtProjectNo.Clear();
            txtPname.Clear();
            txtPnameEn.Clear();
            txtLocation.Clear();
            txtPeriod.Clear();
            txtProjCost.Clear();
            txtReff.Clear();
            txtTotCost.Clear();
            txtNetVAT.Text = "0";
            txtNetVal.Clear();

            cmbContractor.SelectedIndex = -1;
            cmbCountry.SelectedIndex = -1;
            cmbTerms.SelectedIndex = -1;
            cmbStatus1.SelectedIndex = -1;

            txtStartDate.SelectedDate = DateTime.Now;
            txtEndDate.SelectedDate = DateTime.Now;

            _projects.Clear();
            _currentContractCode = -1;

            LoadNextInvNo();
            txtDate.SelectedDate = DateTime.Now;
            txtContrNo.Focus();
        }

        private void ShowErrorMessage(string message, Exception ex)
        {
            MessageBox.Show(
                $"{message} ❌\n\nتفاصيل الخطأ:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        #endregion
    }
}