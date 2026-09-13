using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCities : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;
        private int _currentCode;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCities()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            _currentCode = -1;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.Items.Count == 0)
                LoadCountries();

            LoadCitiesGrid();
            WindowState = MainClass.Window_State;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Clear ──────────────────────────────────────

        private void ClearForm()
        {
            txtName.Text = string.Empty;
            _currentCode = -1;
            UpdateRecordIndicator("📍 سجل جديد");
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        private void LoadCitiesGrid()
        {
            try
            {
                dgvCities.ItemsSource = null;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT Cities.id AS city_id, Countries.id AS country_id, " +
                    "Countries.name AS country, Cities.name AS city " +
                    "FROM Cities, Countries " +
                    "WHERE Cities.country = Countries.id",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                var rows = new List<CityGridRow>();
                foreach (DataRow row in dataTable.Rows)
                {
                    rows.Add(new CityGridRow
                    {
                        CityId = Convert.ToInt32(row["city_id"]),
                        CountryId = Convert.ToInt32(row["country_id"]),
                        CountryName = row["country"]?.ToString() ?? string.Empty,
                        CityName = row["city"]?.ToString() ?? string.Empty
                    });
                }

                dgvCities.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل بيانات المدن", ex);
            }
        }

        public void LoadCountries()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Countries ORDER BY id",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbCountry.ItemsSource = dataTable.DefaultView;
                cmbCountry.DisplayMemberPath = "name";
                cmbCountry.SelectedValuePath = "id";
                cmbCountry.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل الدول", ex);
            }
        }

        #endregion

        #region ── Navigation ─────────────────────────────────

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("SELECT TOP 1 * FROM Cities ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM Cities " +
                $"WHERE id < {_currentCode} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM Cities " +
                $"WHERE id > {_currentCode} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("SELECT TOP 1 * FROM Cities ORDER BY id DESC");
        }

        private void NavigateTo(string sqlQuery)
        {
            try
            {
                dgvCities.UnselectAll();
                EnsureConnectionOpen(conn);

                SqlCommand command = new SqlCommand(sqlQuery, conn);
                SqlDataReader dataReader = command.ExecuteReader();
                ReadDataFromReader(dataReader);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التنقل", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private void ReadDataFromReader(SqlDataReader dataReader)
        {
            if (!dataReader.HasRows)
            {
                dataReader.Close();
                return;
            }

            dataReader.Read();

            _currentCode = Convert.ToInt32(dataReader["id"]);
            cmbCountry.SelectedValue = dataReader["country"];
            txtName.Text = dataReader["name"]?.ToString() ?? string.Empty;

            UpdateRecordIndicator($"📍 مدينة: {txtName.Text}");
            dataReader.Close();
        }

        #endregion

        #region ── CRUD ───────────────────────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbCountry.SelectedIndex == -1)
                {
                    ShowWarning("يجب اختيار دولة أولاً");
                    cmbCountry.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowWarning("يجب إدخال اسم المدينة");
                    txtName.Focus();
                    return;
                }

                EnsureConnectionOpen(conn);

                if (_currentCode == -1)
                {
                    new SqlCommand(
                        $"INSERT INTO Cities(name, country) " +
                        $"VALUES(N'{txtName.Text}', {cmbCountry.SelectedValue})",
                        conn).ExecuteNonQuery();

                    txtName.Text = string.Empty;
                    txtName.Focus();
                }
                else
                {
                    new SqlCommand(
                        $"UPDATE Cities SET name = N'{txtName.Text}', " +
                        $"country = {cmbCountry.SelectedValue} " +
                        $"WHERE id = {_currentCode}",
                        conn).ExecuteNonQuery();

                    txtName.Focus();
                }

                LoadCitiesGrid();
                ShowSuccess("تم الحفظ بنجاح ✅");
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    ShowWarning("اختر مدينة ليتم حذفها");
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من حذف هذه المدينة؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(conn);

                new SqlCommand(
                    $"DELETE FROM Cities WHERE id = {_currentCode}",
                    conn).ExecuteNonQuery();

                ShowSuccess("تم الحذف بنجاح 🗑️");
                LoadCitiesGrid();
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Grid Events ────────────────────────────────

        private void dgvCities_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgvCities.SelectedItem is CityGridRow selectedRow)
            {
                _currentCode = selectedRow.CityId;
                cmbCountry.SelectedValue = selectedRow.CountryId;
                txtName.Text = selectedRow.CityName;
                UpdateRecordIndicator($"📍 مدينة: {selectedRow.CityName}");
            }
        }

        private void txtName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                btnSave_Click(sender, new RoutedEventArgs());
            }
        }

        #endregion

        #region ── Country Add ────────────────────────────────

        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedCountryId = cmbCountry.SelectedValue != null
                ? Convert.ToInt32(cmbCountry.SelectedValue)
                : -1;

            Form_WPF.frmCountries countriesForm = new Form_WPF.frmCountries();
            countriesForm.ShowDialog();

            LoadCountries();

            try
            {
                if (savedCountryId != -1)
                    cmbCountry.SelectedValue = savedCountryId;
            }
            catch { }
        }

        #endregion

        #region ── Helpers ────────────────────────────────────

        private void UpdateRecordIndicator(string text)
        {
            if (txtRecordIndicator != null)
                txtRecordIndicator.Text = text;
        }

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

        private static void ShowError(string message, Exception ex = null)
        {
            string detail = ex != null
                ? $"{Environment.NewLine}تفاصيل: {ex.Message}"
                : string.Empty;
            MessageBox.Show(message + detail, "❌ خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, "⚠️ تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void ShowSuccess(string message)
        {
            MessageBox.Show(message, "✅ نجاح",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    #region ── Model ──────────────────────────────────────────

    public class CityGridRow
    {
        public int CityId { get; set; }
        public int CountryId { get; set; }
        public string CountryName { get; set; }
        public string CityName { get; set; }
    }

    #endregion
}