using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmBanks : Window
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        private ObservableCollection<BankDataItem> banksData;

        #endregion

        #region Constructor

        public frmBanks()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code = -1;

            banksData = new ObservableCollection<BankDataItem>();
            dgvBanks.ItemsSource = banksData;
        }

        #endregion

        #region Event Handlers - Window

        private void frmBanks_Load(object sender, RoutedEventArgs e)
        {
            LoadCountries();
            LoadDG("");
            WindowState = MainClass.Window_State;
        }

        #endregion

        #region Event Handlers - ComboBoxes

        private void cmbCountry_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;
                cmbCity.ItemsSource = null;

                if (cmbCountry.SelectedValue != null)
                {
                    LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));
                }
            }
            catch { }
        }

        private void cmbCity_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;

                if (cmbCity.SelectedValue != null)
                {
                    LoadAreas(Convert.ToInt32(cmbCity.SelectedValue));
                }
            }
            catch { }
        }

        #endregion

        #region Event Handlers - Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("⚠️ يجب ادخال اسم البنك", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDis.Text))
                {
                    txtDis.Text = "0";
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                int accId = -1;

                if (Code != -1)
                {
                    using (var adapter = new SqlDataAdapter(
                        $"SELECT Accounts_Index.Code FROM Accounts_Index, banks WHERE Accounts_Index.AName=banks.name AND Accounts_Index.Type=2 AND banks.id={Code}",
                        conn))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            accId = Convert.ToInt32(dataTable.Rows[0][0]);
                        }
                    }
                }
                else
                {
                    accId = Convert.ToInt32(MainClass.GenerateCode(Convert.ToInt32(Common.CurrentBranch.BanksAcc)));
                    Code = Common.GetBankId();
                }

                int country = -1;
                if (cmbCountry.SelectedValue != null)
                    country = Convert.ToInt32(cmbCountry.SelectedValue);

                int city = -1;
                if (cmbCity.SelectedValue != null)
                    city = Convert.ToInt32(cmbCity.SelectedValue);

                AuditorAPI.Models.Bank bank = new AuditorAPI.Models.Bank
                {
                    BankId = Code,
                    Bankname = txtName.Text,
                    Country = country,
                    City = city,
                    AccId = accId,
                    telphone = txtTel.Text,
                    mobile = txtMobile.Text,
                    IS_Deleted = false,
                    BranchId = MainClass.BranchNo,
                    CreateDate = DateTime.Now,
                    LastUpdateDate = DateTime.Now,
                    ClientCode = Sync.ClientCode,
                    ChangeInPOS = chkChangeInPOS.IsChecked == true
                };

                List<AuditorAPI.Models.Bank> banks = new List<AuditorAPI.Models.Bank> { bank };
                var Home = new Home();
                if (new EntityOperations().SaveBanks(banks, AddedLocally: true))
                {
                    if (Sync.ActiveSync && Sync.SyncType > 0)
                    {
                        new BankCRUD(Sync.APIUrl).AddBank(banks);
                    }

                    if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                    {
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "Banks",
                            Encoding.UTF8.GetBytes(SendData.GetBanks()),
                            0,
                            retain: true);
                    }

                    var frmSaved = new frmSavedMsg();
                    if (Code != -1)
                    {
                        frmSaved.lblSave.Text = "تم الحفظ بنجاح...";
                    }
                    frmSaved.ShowDialog();

                    if (frmSaved.Pressed == 1)
                    {
                        CLR();
                    }
                    else if (frmSaved.Pressed == 2)
                    {
                        CLR();
                        return;
                    }
                    else if (frmSaved.Pressed == 3)
                    {
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("❌ خطأ أثناء الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                LoadDG("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    MessageBox.Show("⚠️ اختر بنك ليتم حذفه", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Code == 1 || Code == 2)
                {
                    MessageBox.Show("⚠️ نأسف لا يمكن حذف هذا البنك", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn1.State != ConnectionState.Open)
                    conn1.Open();

                using (var cmd = new SqlCommand($"SELECT Acc_Code FROM Banks WHERE id={Code}", conn1))
                {
                    var accCode = cmd.ExecuteScalar();

                    using (var adapter = new SqlDataAdapter(
                        $"SELECT res_id FROM Entry_sub WHERE acc_no={accCode}", conn))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            MessageBox.Show("⚠️ هذا البنك عليه عمليات لا يمكن حذفه", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                var result = MessageBox.Show("❓ هل أنت متأكد من حذف البنك؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    new SqlCommand($"UPDATE Banks SET IS_Deleted=1 WHERE id={Code}", conn).ExecuteNonQuery();

                    using (var adapter = new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName=N'{txtName.Text}'", conn))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            new SqlCommand($"DELETE FROM Accounts_Index WHERE Code={Convert.ToInt32(dataTable.Rows[0][0])}",
                                conn).ExecuteNonQuery();
                        }
                    }

                    LoadDG("");
                    CLR();
                    MessageBox.Show("✅ تم الحذف بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Banks WHERE IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Banks WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Banks WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Banks WHERE IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int selectedCountry = -1;
            if (cmbCountry.SelectedValue != null)
                selectedCountry = Convert.ToInt32(cmbCountry.SelectedValue);

            var frmCountry = new frmCountries();
            frmCountry.ShowDialog();

            LoadCountries();
            try
            {
                cmbCountry.SelectedValue = selectedCountry;
            }
            catch { }
        }

        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                MessageBox.Show("⚠️ يجب اختيار الدولة التابع لها المدينة أولا", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            int selectedCity = -1;
            if (cmbCity.SelectedValue != null)
                selectedCity = Convert.ToInt32(cmbCity.SelectedValue);

            var frmCity = new frmCities();
            frmCity.LoadCountries();
            frmCity.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            frmCity.ShowDialog();

            LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));
            try
            {
                cmbCity.SelectedValue = selectedCity;
            }
            catch { }
        }

        private void btnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                MessageBox.Show("⚠️ يجب اختيار الدولة التابع لها المنطقة أولا", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            if (cmbCity.SelectedValue == null)
            {
                MessageBox.Show("⚠️ يجب اختيار المدينة التابع لها المنطقة أولا", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCity.Focus();
                return;
            }

            int selectedArea = -1;
            if (cmbArea.SelectedValue != null)
                selectedArea = Convert.ToInt32(cmbArea.SelectedValue);

            var frmArea = new frmAreas();
            frmArea.LoadCountries();
            frmArea.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            frmArea.LoadCities(Convert.ToInt32(frmArea.cmbCountry.SelectedValue));
            frmArea.cmbCity.SelectedValue = cmbCity.SelectedValue;
            frmArea.ShowDialog();

            LoadAreas(Convert.ToInt32(cmbCity.SelectedValue));
            try
            {
                cmbArea.SelectedValue = selectedArea;
            }
            catch { }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for printing
        }

        #endregion

        #region Event Handlers - DataGrid

        private void dgvBanks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvBanks.SelectedItem != null)
            {
                var selectedItem = dgvBanks.SelectedItem as BankDataItem;
                if (selectedItem != null)
                {
                    Code = selectedItem.Column3;
                    Navigate($"SELECT * FROM Banks WHERE id={Code}");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void CLR()
        {
            MainClass.CLRForm(this);
            Code = -1;
            chkChangeInPOS.Visibility = Visibility.Collapsed;
            chkChangeInPOS.IsChecked = false;
        }

        private void LoadDG(string cond)
        {
            banksData.Clear();

            try
            {
                using (var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Banks WHERE {cond} IS_Deleted=0 ORDER BY id", conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var item = new BankDataItem
                        {
                            Column3 = Convert.ToInt32(row["id"]),
                            Column1 = row["name"].ToString(),
                            Column5 = row["tel"].ToString(),
                            Column6 = row["mobile"].ToString()
                        };

                        // Get Country Name
                        if (Convert.ToInt32(row["country"]) != -1)
                        {
                            using (var countryAdapter = new SqlDataAdapter(
                                $"SELECT name FROM Countries WHERE id={row["country"]}", conn))
                            {
                                var countryTable = new DataTable();
                                countryAdapter.Fill(countryTable);
                                if (countryTable.Rows.Count > 0)
                                    item.Column2 = countryTable.Rows[0][0].ToString();
                            }
                        }

                        // Get City Name
                        if (Convert.ToInt32(row["city"]) != -1)
                        {
                            using (var cityAdapter = new SqlDataAdapter(
                                $"SELECT name FROM Cities WHERE id={row["city"]}", conn))
                            {
                                var cityTable = new DataTable();
                                cityAdapter.Fill(cityTable);
                                if (cityTable.Rows.Count > 0)
                                    item.Column4 = cityTable.Rows[0][0].ToString();
                            }
                        }

                        // Get Area Name
                        if (Convert.ToInt32(row["area"]) != -1)
                        {
                            using (var areaAdapter = new SqlDataAdapter(
                                $"SELECT name FROM areas WHERE id={row["area"]}", conn))
                            {
                                var areaTable = new DataTable();
                                areaAdapter.Fill(areaTable);
                                if (areaTable.Rows.Count > 0)
                                    item.Column7 = areaTable.Rows[0][0].ToString();
                            }
                        }

                        banksData.Add(item);
                    }
                }

                dgvBanks.SelectedItem = null;
            }
            catch { }
        }

        public void LoadCountries()
        {
            try
            {
                using (var adapter = new SqlDataAdapter("SELECT id, name FROM Countries ORDER BY id", conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    cmbCountry.ItemsSource = dataTable.DefaultView;
                    cmbCountry.SelectedIndex = -1;
                }
            }
            catch { }
        }

        public void LoadCities(int country)
        {
            try
            {
                using (var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Cities WHERE country={country} ORDER BY id", conn1))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    cmbCity.ItemsSource = dataTable.DefaultView;
                    cmbCity.SelectedIndex = -1;
                }
            }
            catch { }
        }

        public void LoadAreas(int city)
        {
            try
            {
                using (var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM areas WHERE city={city} ORDER BY id", conn1))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    cmbArea.ItemsSource = dataTable.DefaultView;
                    cmbArea.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (dr.HasRows)
            {
                dr.Read();
                CLR();

                Code = Convert.ToInt32(dr["id"]);
                txtName.Text = dr["name"].ToString();

                if (Convert.ToInt32(dr["country"]) != -1)
                {
                    cmbCountry.SelectedValue = Convert.ToInt32(dr["country"]);
                    LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));
                }

                if (Convert.ToInt32(dr["city"]) != -1)
                {
                    cmbCity.SelectedValue = Convert.ToInt32(dr["city"]);
                    LoadAreas(Convert.ToInt32(cmbCity.SelectedValue));
                }

                if (Convert.ToInt32(dr["area"]) != -1)
                {
                    cmbArea.SelectedValue = Convert.ToInt32(dr["area"]);
                }

                if (Code == 1)
                {
                    chkChangeInPOS.Visibility = Visibility.Visible;
                }

                txtTel.Text = dr["tel"].ToString();
                txtMobile.Text = dr["mobile"].ToString();
                txtNotes.Text = dr["notes"].ToString();
                txtDis.Text = dr["DisPre"].ToString();

                if (dr["ChangeInPOS"] != DBNull.Value)
                {
                    chkChangeInPOS.IsChecked = Convert.ToBoolean(dr["ChangeInPOS"]);
                }
            }
        }

        public void Navigate(string sqlstr)
        {
            dgvBanks.SelectedItem = null;

            try
            {
                using (var cmd = new SqlCommand(sqlstr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    ReadData(cmd.ExecuteReader());
                }
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion
    }

    #region Helper Class

    public class BankDataItem
    {
        public int Column3 { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column4 { get; set; }
        public string Column7 { get; set; }
        public string Column5 { get; set; }
        public string Column6 { get; set; }
    }

    #endregion
}