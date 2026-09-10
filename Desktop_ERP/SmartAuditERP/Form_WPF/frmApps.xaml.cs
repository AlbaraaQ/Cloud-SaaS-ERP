using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    #region Models
    public class AppModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FoundationName { get; set; }
        public string License { get; set; }
        public string Tel { get; set; }
        public string Mobile { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public string Notes { get; set; }
        public string TaxNo { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
        public int AreaId { get; set; }
        public int Type { get; set; }
        public string AccountCode { get; set; }
    }

    public class CountryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CityItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AreaItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AppDataItem
    {
        public string Name { get; set; }
    }
    #endregion

    public partial class frmApps : Window
    {
        #region Fields
        private SqlConnection _dbConnection;
        private SqlConnection _dbConnection1;
        private int _currentAppId;
        private int _appType;
        private string _savedName;
        private string _savedFoundationName;
        private ObservableCollection<AppModel> _searchResults;
        private ObservableCollection<AppDataItem> _appData;

        public int AppId { get; set; }
        public bool IsDone { get; set; }
        public static string SelectedAccount { get; set; }
        #endregion

        #region Constructor
        public frmApps()
        {
            InitializeComponent();

            _dbConnection = MainClass.ConnObj();
            _dbConnection1 = MainClass.ConnObj();
            _currentAppId = -1;
            _appType = 1;
            _savedName = "";
            _savedFoundationName = "";
            AppId = -1;
            IsDone = false;

            _searchResults = new ObservableCollection<AppModel>();
            _appData = new ObservableCollection<AppDataItem>();

            dgvSrch.ItemsSource = _searchResults;
            dgvApp.ItemsSource = _appData;

            Loaded += FrmApps_Loaded;
        }
        #endregion

        #region Form Events
        private void FrmApps_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCountries();
            ClearAll();
        }
        #endregion

        #region Data Loading Methods
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
                        cmbCountry.ItemsSource = dataTable.AsEnumerable().Select(row => new CountryItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الدول", ex);
            }
        }

        private void LoadCities(int countryId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Cities WHERE country={countryId} ORDER BY id", _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbCity.ItemsSource = dataTable.AsEnumerable().Select(row => new CityItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل المدن", ex);
            }
        }

        private void LoadAreas(int cityId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM areas WHERE city={cityId} ORDER BY id", _dbConnection1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbArea.ItemsSource = dataTable.AsEnumerable().Select(row => new AreaItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل المناطق", ex);
            }
        }
        #endregion

        #region ComboBox Events
        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbCity.ItemsSource = null;
                cmbArea.ItemsSource = null;

                if (cmbCountry.SelectedValue != null)
                {
                    int countryId = Convert.ToInt32(cmbCountry.SelectedValue);
                    LoadCities(countryId);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في اختيار الدولة", ex);
            }
        }

        private void cmbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;

                if (cmbCity.SelectedValue != null)
                {
                    int cityId = Convert.ToInt32(cmbCity.SelectedValue);
                    LoadAreas(cityId);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في اختيار المدينة", ex);
            }
        }
        #endregion

        #region Button Click Events - CRUD
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
            txtName.Focus();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveApp();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteApp();
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
        #endregion

        #region Save Method
        private void SaveApp()
        {
            try
            {
                // Trial version check
                if (MainClass.IsTrial)
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "SELECT id FROM Apps", _dbConnection1))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count >= 10)
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
                if (string.IsNullOrWhiteSpace(txtName.Text.Trim()))
                {
                    MessageBox.Show("يرجى إدخال اسم التطبيق ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtFoundation.Text.Trim()))
                {
                    MessageBox.Show("يرجى إدخال اسم المؤسسة ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFoundation.Focus();
                    return;
                }

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                SqlCommand command;

                if (_currentAppId == -1)
                {
                    // Check for duplicate
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        $"SELECT id FROM Apps WHERE type={_appType} AND name=N'{txtName.Text.Trim()}' " +
                        $"AND mobile=N'{txtMobile.Text.Trim()}' AND Foundname=N'{txtFoundation.Text.Trim()}'",
                        _dbConnection1))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            MessageBox.Show("هذا الإسم مدخل من قبل ⚠️", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtName.Focus();
                            return;
                        }
                    }

                    // Get next ID
                    using (SqlCommand maxCmd = new SqlCommand("SELECT MAX(id) FROM Apps", _dbConnection))
                    {
                        object result1 = maxCmd.ExecuteScalar();
                        AppId = (result1 == DBNull.Value) ? 1 : Convert.ToInt32(result1) + 1;
                    }

                    command = new SqlCommand(
                        "INSERT INTO Apps(name, Foundname, country, city, area, license, tel, mobile, " +
                        "fax, email, notes, type, tax_no, IS_Deleted, AccountCode) " +
                        "VALUES(@name, @Foundname, @country, @city, @area, @license, @tel, @mobile, " +
                        "@fax, @email, @notes, @type, @tax_no, @IS_Deleted, @AccountCode)",
                        _dbConnection);
                }
                else
                {
                    AppId = _currentAppId;

                    command = new SqlCommand(
                        $"UPDATE Apps SET name=@name, Foundname=@Foundname, country=@country, city=@city, " +
                        $"area=@area, license=@license, tel=@tel, mobile=@mobile, fax=@fax, email=@email, " +
                        $"notes=@notes, type=@type, tax_no=@tax_no, AccountCode=@AccountCode " +
                        $"WHERE id={_currentAppId}",
                        _dbConnection);
                }

                // Set parameters
                command.Parameters.AddWithValue("@name", txtName.Text);
                command.Parameters.AddWithValue("@Foundname", txtFoundation.Text);
                command.Parameters.AddWithValue("@country", cmbCountry.SelectedValue ?? -1);
                command.Parameters.AddWithValue("@city", cmbCity.SelectedValue ?? -1);
                command.Parameters.AddWithValue("@area", cmbArea.SelectedValue ?? -1);
                command.Parameters.AddWithValue("@license", txtLicense.Text);
                command.Parameters.AddWithValue("@tel", txtTel.Text);
                command.Parameters.AddWithValue("@mobile", txtMobile.Text);
                command.Parameters.AddWithValue("@fax", txtFax.Text);
                command.Parameters.AddWithValue("@email", txtEmail.Text);
                command.Parameters.AddWithValue("@notes", txtNotes.Text);
                command.Parameters.AddWithValue("@type", _appType);
                command.Parameters.AddWithValue("@tax_no", txtTaxNo.Text);
                command.Parameters.AddWithValue("@IS_Deleted", 0);

                string accountCode = string.IsNullOrWhiteSpace(txtAccCode.Text) ? "-1" : txtAccCode.Text;
                command.Parameters.AddWithValue("@AccountCode", accountCode);

                command.ExecuteNonQuery();

                // Handle account index
                if (_currentAppId == -1)
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        $"SELECT * FROM Accounts_Index WHERE code=N'{txtAccCode.Text}'", _dbConnection))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count == 0)
                        {
                            using (SqlCommand accCmd = new SqlCommand(
                                "INSERT INTO Accounts_Index(Code, AName, Type, Acc_branch, UserName, date) " +
                                $"VALUES(N'{txtAccCode.Text}', N'{txtName.Text}', 2, {MainClass.BranchNo}, " +
                                $"{MainClass.UserID}, @date)",
                                _dbConnection))
                            {
                                accCmd.Parameters.AddWithValue("@date", DateTime.Now);
                                accCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                else
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        $"SELECT * FROM Accounts_Index WHERE AName=N'{_savedName}'", _dbConnection))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            string code = dataTable.Rows[0]["Code"].ToString();
                            using (SqlCommand updateCmd = new SqlCommand(
                                $"UPDATE Accounts_Index SET AName=N'{txtName.Text}' WHERE Code=N'{code}'",
                                _dbConnection))
                            {
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand accCmd = new SqlCommand(
                                "INSERT INTO Accounts_Index(Code, AName, Type, Acc_branch, UserName, date) " +
                                $"VALUES(N'{txtAccCode.Text}', N'{txtName.Text}', 2, {MainClass.BranchNo}, " +
                                $"{MainClass.UserID}, @date)",
                                _dbConnection))
                            {
                                accCmd.Parameters.AddWithValue("@date", DateTime.Now);
                                accCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                _dbConnection.Close();

                string successMessage = (_currentAppId == -1)
                    ? "✅ تم الحفظ بنجاح"
                    : "✅ تم حفظ التعديلات بنجاح";

                MessageBoxResult result = MessageBox.Show(
                    successMessage + "\n\nهل تريد إدخال سجل جديد؟",
                    "نجاح",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Information);

                IsDone = true;

                if (result == MessageBoxResult.Yes)
                {
                    _currentAppId = AppId;
                    ClearAll();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                {
                    _dbConnection.Close();
                }
            }
        }
        #endregion

        #region Delete Method
        private void DeleteApp()
        {
            try
            {
                if (_currentAppId == -1)
                {
                    MessageBox.Show("اختر تطبيق ليتم حذفه ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "هل أنت متأكد من حذف التطبيق؟ 🗑️",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                // Delete app
                using (SqlCommand command = new SqlCommand(
                    $"UPDATE Apps SET IS_Deleted=1 WHERE id={_currentAppId}", _dbConnection))
                {
                    command.ExecuteNonQuery();
                }

                // Delete account
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName=N'{txtName.Text}'",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        int code = Convert.ToInt32(dataTable.Rows[0]["Code"]);
                        using (SqlCommand deleteCmd = new SqlCommand(
                            $"DELETE FROM Accounts_Index WHERE Code={code}", _dbConnection))
                        {
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                }

                _dbConnection.Close();

                MessageBox.Show("✅ تم الحذف بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearAll();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ أثناء الحذف", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                {
                    _dbConnection.Close();
                }
            }
        }
        #endregion

        #region Navigation Methods
        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvSrch.SelectedItem = null;

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                using (SqlCommand command = new SqlCommand(sqlQuery, _dbConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows && reader.Read())
                        {
                            ReadData(reader);
                        }
                    }
                }

                _dbConnection.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في التنقل", ex);
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            ClearAll();

            _currentAppId = Convert.ToInt32(reader["id"]);
            txtName.Text = reader["name"].ToString();
            _savedName = reader["name"].ToString();
            txtFoundation.Text = reader["Foundname"].ToString();
            _savedFoundationName = reader["Foundname"].ToString();

            try
            {
                int countryId = Convert.ToInt32(reader["country"]);
                if (countryId != -1)
                {
                    cmbCountry.SelectedValue = countryId;
                    LoadCities(countryId);
                }
            }
            catch { }

            try
            {
                int cityId = Convert.ToInt32(reader["city"]);
                if (cityId != -1)
                {
                    cmbCity.SelectedValue = cityId;
                    LoadAreas(cityId);
                }
            }
            catch { }

            try
            {
                int areaId = Convert.ToInt32(reader["area"]);
                if (areaId != -1)
                {
                    cmbArea.SelectedValue = areaId;
                }
            }
            catch { }

            txtLicense.Text = reader["license"].ToString();
            txtTel.Text = reader["tel"].ToString();
            txtMobile.Text = reader["mobile"].ToString();
            txtEmail.Text = reader["email"].ToString();
            txtFax.Text = reader["fax"].ToString();
            txtNotes.Text = reader["notes"].ToString();

            try
            {
                txtTaxNo.Text = reader["tax_no"].ToString();
            }
            catch { }

            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE AName=N'{txtName.Text}'", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        rbPostPone.IsChecked = true;
                        txtAccCode.Text = dataTable.Rows[0]["Code"].ToString();
                    }
                    else
                    {
                        rbCash.IsChecked = true;
                    }

                    txtAccCode.IsReadOnly = true;
                }
            }
            catch { }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Apps WHERE type={_appType} AND IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Apps WHERE type={_appType} AND IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Apps WHERE type={_appType} AND IS_Deleted=0 AND id>{_currentAppId} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Apps WHERE type={_appType} AND IS_Deleted=0 AND id<{_currentAppId} ORDER BY id DESC");
        }
        #endregion

        #region Add Buttons Events
        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int selectedCountryId = -1;
                if (cmbCountry.SelectedValue != null)
                {
                    selectedCountryId = Convert.ToInt32(cmbCountry.SelectedValue);
                }

                // فتح نموذج الدول
                MessageBox.Show("فتح نموذج إضافة دولة... 🌍", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadCountries();

                try
                {
                    cmbCountry.SelectedValue = selectedCountryId;
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في إضافة دولة", ex);
            }
        }

        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCountry.SelectedValue == null)
                {
                    MessageBox.Show("يجب اختيار الدولة التابع لها المدينة أولاً ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCountry.Focus();
                    return;
                }

                int selectedCityId = -1;
                if (cmbCity.SelectedValue != null)
                {
                    selectedCityId = Convert.ToInt32(cmbCity.SelectedValue);
                }

                // فتح نموذج المدن
                MessageBox.Show("فتح نموذج إضافة مدينة... 🏙️", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));

                try
                {
                    cmbCity.SelectedValue = selectedCityId;
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في إضافة مدينة", ex);
            }
        }

        private void btnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCountry.SelectedValue == null)
                {
                    MessageBox.Show("يجب اختيار الدولة التابع لها المنطقة أولاً ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCountry.Focus();
                    return;
                }

                if (cmbCity.SelectedValue == null)
                {
                    MessageBox.Show("يجب اختيار المدينة التابع لها المنطقة أولاً ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCity.Focus();
                    return;
                }

                int selectedAreaId = -1;
                if (cmbArea.SelectedValue != null)
                {
                    selectedAreaId = Convert.ToInt32(cmbArea.SelectedValue);
                }

                // فتح نموذج المناطق
                MessageBox.Show("فتح نموذج إضافة منطقة... 📍", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadAreas(Convert.ToInt32(cmbCity.SelectedValue));

                try
                {
                    cmbArea.SelectedValue = selectedAreaId;
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في إضافة منطقة", ex);
            }
        }
        #endregion

        #region Search Methods
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void txtNameSrch_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            try
            {
                string condition = "";

                if (!string.IsNullOrWhiteSpace(txtNameSrch.Text.Trim()))
                {
                    condition = $"name LIKE N'%{txtNameSrch.Text}%' AND ";
                }

                LoadSearchResults(condition);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في البحث", ex);
            }
        }

        private void LoadSearchResults(string condition)
        {
            try
            {
                _searchResults.Clear();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Apps WHERE {condition} IS_Deleted=0 AND type={_appType} ORDER BY id",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var app = new AppModel
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString(),
                            FoundationName = row["Foundname"].ToString(),
                            License = row["license"].ToString(),
                            Tel = row["tel"].ToString(),
                            Mobile = row["mobile"].ToString()
                        };

                        // Get country name
                        try
                        {
                            int countryId = Convert.ToInt32(row["country"]);
                            if (countryId != -1)
                            {
                                using (SqlDataAdapter countryAdapter = new SqlDataAdapter(
                                    $"SELECT name FROM Countries WHERE id={countryId}", _dbConnection))
                                {
                                    DataTable countryTable = new DataTable();
                                    countryAdapter.Fill(countryTable);
                                    if (countryTable.Rows.Count > 0)
                                    {
                                        app.Country = countryTable.Rows[0]["name"].ToString();
                                    }
                                }
                            }
                        }
                        catch { }

                        // Get city name
                        try
                        {
                            int cityId = Convert.ToInt32(row["city"]);
                            if (cityId != -1)
                            {
                                using (SqlDataAdapter cityAdapter = new SqlDataAdapter(
                                    $"SELECT name FROM Cities WHERE id={cityId}", _dbConnection))
                                {
                                    DataTable cityTable = new DataTable();
                                    cityAdapter.Fill(cityTable);
                                    if (cityTable.Rows.Count > 0)
                                    {
                                        app.City = cityTable.Rows[0]["name"].ToString();
                                    }
                                }
                            }
                        }
                        catch { }

                        // Get area name
                        try
                        {
                            int areaId = Convert.ToInt32(row["area"]);
                            if (areaId != -1)
                            {
                                using (SqlDataAdapter areaAdapter = new SqlDataAdapter(
                                    $"SELECT name FROM areas WHERE id={areaId}", _dbConnection))
                                {
                                    DataTable areaTable = new DataTable();
                                    areaAdapter.Fill(areaTable);
                                    if (areaTable.Rows.Count > 0)
                                    {
                                        app.Area = areaTable.Rows[0]["name"].ToString();
                                    }
                                }
                            }
                        }
                        catch { }

                        _searchResults.Add(app);
                    }
                }

                dgvSrch.SelectedItem = null;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل نتائج البحث", ex);
            }
        }
        #endregion

        #region TextBox Events
        private void txtAccCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT aname, parentcode FROM Accounts_Index WHERE code=N'{txtAccCode.Text}'",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        string parentCode = dataTable.Rows[0]["parentcode"].ToString();

                        if (parentCode == Common.CurrentBranch.CustomersAcc ||
                            parentCode == Common.CurrentBranch.SupliersAcc ||
                            parentCode == "12212")
                        {
                            txtName.Text = dataTable.Rows[0]["aname"].ToString();
                        }
                    }
                }
            }
            catch { }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedAccount = "";

                // فتح نموذج اختيار العميل
                MessageBox.Show("فتح نموذج اختيار العميل... 👤", "اختيار",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                txtAccCode.Text = SelectedAccount;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في اختيار الحساب", ex);
            }
        }
        #endregion

        #region Helper Methods
        private void ClearAll()
        {
            txtName.Clear();
            txtFoundation.Clear();
            txtLicense.Clear();
            txtMobile.Clear();
            txtTel.Clear();
            txtEmail.Clear();
            txtFax.Clear();
            txtTaxNo.Clear();
            txtNotes.Clear();

            cmbCountry.SelectedIndex = -1;
            cmbCity.SelectedIndex = -1;
            cmbArea.SelectedIndex = -1;
            cmbPrices.SelectedIndex = -1;

            rbCash.IsChecked = true;

            if (rbCash.IsChecked == true)
            {
                txtAccCode.Text = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.CustomersAcc));
                _appType = 1;
            }
            else if (rbPostPone.IsChecked == true)
            {
                txtAccCode.Text = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.SupliersAcc));
                _appType = 2;
            }

            txtAccCode.IsReadOnly = false;
            _currentAppId = -1;
            _savedName = "";
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