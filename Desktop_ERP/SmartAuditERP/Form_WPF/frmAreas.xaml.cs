using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using log4net;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة تعريف المناطق
    /// </summary>
    public partial class frmAreas : Window
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private SqlConnection _connection;
        private int _currentCode = -1;
        private ObservableCollection<AreaItem_frmAreas> _areasCollection;

        #endregion

        #region Constructor

        public frmAreas()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
            _areasCollection = new ObservableCollection<AreaItem_frmAreas>();
            dgvAreas.ItemsSource = _areasCollection;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadCountries();
                LoadData();
                this.WindowState = MainClass.Window_State;
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
        /// تحميل الدول
        /// </summary>
        public void LoadCountries()
        {
            try
            {
                string query = "SELECT id, name FROM Countries ORDER BY id";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbCountry.DisplayMemberPath = "name";
                    cmbCountry.SelectedValuePath = "id";
                    cmbCountry.ItemsSource = dt.DefaultView;
                    cmbCountry.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCountries Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل المدن حسب الدولة
        /// </summary>
        public void LoadCities(int countryId)
        {
            try
            {
                string query = $"SELECT id, name FROM Cities WHERE country={countryId} ORDER BY id";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbCity.DisplayMemberPath = "name";
                    cmbCity.SelectedValuePath = "id";
                    cmbCity.ItemsSource = dt.DefaultView;
                    cmbCity.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCities Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل جميع المناطق
        /// </summary>
        private void LoadData()
        {
            try
            {
                _areasCollection.Clear();

                string query = @"SELECT Areas.id as area_id, 
                                       Countries.name as country, 
                                       Cities.name as city, 
                                       Areas.name as area, 
                                       Countries.id as country_id, 
                                       Cities.id as city_id 
                                FROM Areas, Cities, Countries  
                                WHERE Areas.city = Cities.id 
                                AND Cities.country = Countries.id";

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _areasCollection.Add(new AreaItem_frmAreas
                        {
                            Id = Convert.ToInt32(row["area_id"]),
                            CountryName = row["country"].ToString(),
                            CityName = row["city"].ToString(),
                            AreaName = row["area"].ToString(),
                            CountryId = Convert.ToInt32(row["country_id"]),
                            CityId = Convert.ToInt32(row["city_id"])
                        });
                    }
                }

                dgvAreas.Items.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadData Error: {ex.Message}");
            }
        }

        #endregion

        #region Clear & Reset Methods

        /// <summary>
        /// مسح الحقول
        /// </summary>
        private void Clear()
        {
            txtName.Clear();
            _currentCode = -1;
            dgvAreas.SelectedItem = null;
            txtName.Focus();
        }

        #endregion

        #region Save Methods

        /// <summary>
        /// حفظ البيانات
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من البيانات
                if (!ValidateInput())
                    return;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                if (_currentCode == -1)
                {
                    // إضافة جديد
                    InsertArea();
                }
                else
                {
                    // تعديل موجود
                    UpdateArea();
                }

                LoadData();
                Clear();

                MessageBox.Show("تم الحفظ بنجاح", "حفظ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"btnSave_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        /// <summary>
        /// التحقق من صحة البيانات
        /// </summary>
        private bool ValidateInput()
        {
            if (cmbCountry.SelectedIndex == -1)
            {
                MessageBox.Show("يجب اختيار دولة أولا", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return false;
            }

            if (cmbCity.SelectedIndex == -1)
            {
                MessageBox.Show("يجب اختيار مدينة أولا", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCity.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("يجب ادخال اسم المنطقة", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// إضافة منطقة جديدة
        /// </summary>
        private void InsertArea()
        {
            string query = "INSERT INTO Areas(name, city) VALUES(@name, @city)";
            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@city", cmbCity.SelectedValue);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// تعديل منطقة موجودة
        /// </summary>
        private void UpdateArea()
        {
            string query = "UPDATE Areas SET name=@name, city=@city WHERE id=@id";
            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@city", cmbCity.SelectedValue);
                cmd.Parameters.AddWithValue("@id", _currentCode);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Delete Methods

        /// <summary>
        /// حذف المنطقة
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    MessageBox.Show("اختر منطقة ليتم حذفها", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"هل أنت متأكد من حذف '{txtName.Text}'؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                string query = "DELETE FROM Areas WHERE id=@id";
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@id", _currentCode);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("تم الحذف بنجاح", "حذف",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                Clear();
            }
            catch (Exception ex)
            {
                Logger.Error($"btnDelete_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ أثناء الحذف{Environment.NewLine}تفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// التنقل في السجلات
        /// </summary>
        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvAreas.SelectedItem = null;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                using (SqlCommand cmd = new SqlCommand(sqlQuery, _connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                    {
                        _currentCode = Convert.ToInt32(reader["id"]);
                        int cityId = Convert.ToInt32(reader["city"]);
                        txtName.Text = reader["name"].ToString();

                        reader.Close();

                        // تحميل الدولة والمدينة
                        LoadCountryAndCity(cityId);

                        // تحديد العنصر في الجدول
                        var item = _areasCollection.FirstOrDefault(x => x.Id == _currentCode);
                        if (item != null)
                        {
                            dgvAreas.SelectedItem = item;
                            dgvAreas.ScrollIntoView(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Navigate Error: {ex.Message}");
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        /// <summary>
        /// تحميل الدولة والمدينة للسجل
        /// </summary>
        private void LoadCountryAndCity(int cityId)
        {
            try
            {
                string query = "SELECT country FROM Cities WHERE id=@cityId";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@cityId", cityId);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        int countryId = Convert.ToInt32(dt.Rows[0]["country"]);
                        cmbCountry.SelectedValue = countryId;
                        cmbCity.SelectedValue = cityId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCountryAndCity Error: {ex.Message}");
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Areas ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Areas ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode != -1)
            {
                Navigate($"SELECT TOP 1 * FROM Areas WHERE id > {_currentCode} ORDER BY id ASC");
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode != -1)
            {
                Navigate($"SELECT TOP 1 * FROM Areas WHERE id < {_currentCode} ORDER BY id DESC");
            }
        }

        #endregion

        #region ComboBox Events

        /// <summary>
        /// عند تغيير الدولة
        /// </summary>
        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbCountry.SelectedValue != null)
                {
                    int countryId = Convert.ToInt32(cmbCountry.SelectedValue);
                    LoadCities(countryId);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"cmbCountry_SelectionChanged Error: {ex.Message}");
            }
        }

        #endregion

        #region DataGrid Events

        /// <summary>
        /// عند تغيير التحديد في الجدول
        /// </summary>
        private void dgvAreas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvAreas.SelectedItem is AreaItem_frmAreas item)
                {
                    _currentCode = item.Id;
                    cmbCountry.SelectedValue = item.CountryId;
                    cmbCity.SelectedValue = item.CityId;
                    txtName.Text = item.AreaName;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"dgvAreas_SelectionChanged Error: {ex.Message}");
            }
        }

        #endregion

        #region TextBox Events

        /// <summary>
        /// عند الضغط على Enter في اسم المنطقة
        /// </summary>
        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSave_Click(null, null);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// زر إضافة دولة
        /// </summary>
        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int selectedCountryId = -1;
                if (cmbCountry.SelectedValue != null)
                {
                    selectedCountryId = Convert.ToInt32(cmbCountry.SelectedValue);
                }

                var countriesForm = new frmCountries();
                countriesForm.ShowDialog();

                LoadCountries();

                if (selectedCountryId > 0)
                {
                    cmbCountry.SelectedValue = selectedCountryId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"btnCountryAdd_Click Error: {ex.Message}");
            }
        }

        /// <summary>
        /// زر إضافة مدينة
        /// </summary>
        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCountry.SelectedValue == null)
                {
                    MessageBox.Show("يجب اختيار الدولة التابع لها المدينة أولا", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbCountry.Focus();
                    return;
                }

                int selectedCityId = -1;
                if (cmbCity.SelectedValue != null)
                {
                    selectedCityId = Convert.ToInt32(cmbCity.SelectedValue);
                }

                var citiesForm = new frmCities();
                citiesForm.LoadCountries();
                citiesForm.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
                citiesForm.ShowDialog();

                LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));

                if (selectedCityId > 0)
                {
                    cmbCity.SelectedValue = selectedCityId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"btnCityAdd_Click Error: {ex.Message}");
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// عنصر المنطقة
    /// </summary>
    public class AreaItem_frmAreas
    {
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CityName { get; set; }
        public string AreaName { get; set; }
        public int CountryId { get; set; }
        public int CityId { get; set; }
    }

    #endregion
}