using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using SmartAuditERP.Form_WPF;
using Ws_Auditor;
using Ws_Auditor.CashierSerivce;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCloudPriceSetting : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;

        private DataTable bankTable;
        private DataTable payTypeTable;
        private ObservableCollection<CloudBankSettingRow> bankRows;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCloudPriceSetting()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            bankRows = new ObservableCollection<CloudBankSettingRow>();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPaymentType();
            GridControl1.ItemsSource = SetDgvDataSource();
            loadBanks();
            LoadPaytypeToGrid();
            loadData();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Load Data ──────────────────────────────────

        private void LoadPaymentType()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT TypeId, TypeName FROM priceTypes",
                    conn);

                DataTable priceTypesTable = new DataTable();
                adapter.Fill(priceTypesTable);

                DataTable wholesaleTable = priceTypesTable.Copy();
                DataTable consumerTable = priceTypesTable.Copy();

                SetComboBoxProperties(cmbCompetitorPrice, priceTypesTable);
                SetComboBoxProperties(cmbWholesalePrice, wholesaleTable);
                SetComboBoxProperties(cmbConsumerPrice, consumerTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void loadBanks()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks " +
                    "WHERE IS_Deleted = 0 AND id != 1 AND id != 2",
                    conn);

                bankTable = new DataTable();
                adapter.Fill(bankTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء تحميل البنوك: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadPaytypeToGrid()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT CloudID, Name FROM CloudPayment",
                    conn);

                payTypeTable = new DataTable();
                adapter.Fill(payTypeTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء تحميل طرق الدفع: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ObservableCollection<CloudBankSettingRow> SetDgvDataSource()
        {
            return bankRows;
        }

        private void loadData()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT ID, Name, ISNULL(CloudID, 0) AS CloudID, BankId " +
                    "FROM CloudPriceSetting",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                    return;

                foreach (DataRow row in dataTable.Rows)
                {
                    int id = Convert.ToInt32(row["ID"]);

                    if (id == 1 && row["CloudID"] != DBNull.Value)
                        cmbCompetitorPrice.SelectedValue = row["CloudID"];

                    if (id == 2 && row["CloudID"] != DBNull.Value)
                        cmbWholesalePrice.SelectedValue = row["CloudID"];

                    if (id == 3 && row["CloudID"] != DBNull.Value)
                        cmbConsumerPrice.SelectedValue = row["CloudID"];
                }

                SqlDataAdapter bankSettingsAdapter = new SqlDataAdapter(
                    "SELECT ID, CloudPaytypeID, Name, BankId " +
                    "FROM CloudPaytypeSetting WHERE ID > 4",
                    conn);

                DataTable bankSettingsTable = new DataTable();
                bankSettingsAdapter.Fill(bankSettingsTable);

                bankRows.Clear();

                foreach (DataRow row in bankSettingsTable.Rows)
                {
                    bankRows.Add(new CloudBankSettingRow
                    {
                        BankId = row["BankId"]?.ToString() ?? string.Empty,
                        PayTypeId = row["CloudPaytypeID"]?.ToString() ?? string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء قراءة البيانات: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SetComboBoxProperties(ComboBox cmb, DataTable dt)
        {
            cmb.ItemsSource = dt.DefaultView;
            cmb.DisplayMemberPath = "TypeName";
            cmb.SelectedValuePath = "TypeId";
            cmb.SelectedIndex = -1;
        }

        #endregion

        #region ── Grid Combo Loading ─────────────────────────

        private void BankComboInGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && bankTable != null)
            {
                comboBox.ItemsSource = bankTable.DefaultView;
                comboBox.DisplayMemberPath = "name";
                comboBox.SelectedValuePath = "id";
            }
        }

        private void PayTypeComboInGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && payTypeTable != null)
            {
                comboBox.ItemsSource = payTypeTable.DefaultView;
                comboBox.DisplayMemberPath = "Name";
                comboBox.SelectedValuePath = "CloudID";
            }
        }

        private void btnAddBankRow_Click(object sender, RoutedEventArgs e)
        {
            bankRows.Add(new CloudBankSettingRow());
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button deleteButton &&
                deleteButton.Tag is CloudBankSettingRow row)
            {
                MessageBoxResult result = MessageBox.Show(
                    "هل أنت متأكد من الحذف ؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    bankRows.Remove(row);
            }
        }

        #endregion

        #region ── Save ───────────────────────────────────────

        private void SaveSetting()
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                UpdateSetting(1, cmbCompetitorPrice);
                UpdateSetting(2, cmbWholesalePrice);
                UpdateSetting(3, cmbConsumerPrice);

                MessageBox.Show(
                    "تم الحفظ بنجاح ✅",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateSetting(int id, ComboBox comboBox)
        {
            if (comboBox.SelectedIndex != -1)
            {
                using SqlCommand sqlCommand = new SqlCommand(
                    $"UPDATE CloudPriceSetting " +
                    $"SET CloudID = {comboBox.SelectedValue} " +
                    $"WHERE ID = {id}",
                    conn);

                sqlCommand.ExecuteNonQuery();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    MessageBox.Show(
                        "نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                SaveMainPaymentSettings();
                SaveBankPaymentSettings();

                MessageBox.Show(
                    "تم حفظ الإعدادات بنجاح ✅",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء الحفظ: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void SaveMainPaymentSettings()
        {
            if (cmbCompetitorPrice.SelectedIndex != -1)
            {
                new SqlCommand(
                    $"UPDATE CloudPriceSetting SET CloudID = {cmbCompetitorPrice.SelectedValue} WHERE id = 1",
                    conn).ExecuteNonQuery();
            }

            if (cmbWholesalePrice.SelectedIndex != -1)
            {
                new SqlCommand(
                    $"UPDATE CloudPriceSetting SET CloudID = {cmbWholesalePrice.SelectedValue} WHERE id = 2",
                    conn).ExecuteNonQuery();
            }

            if (cmbConsumerPrice.SelectedIndex != -1)
            {
                new SqlCommand(
                    $"UPDATE CloudPriceSetting SET CloudID = {cmbConsumerPrice.SelectedValue} WHERE id = 3",
                    conn).ExecuteNonQuery();
            }
        }

        private void SaveBankPaymentSettings()
        {
            new SqlCommand(
                "DELETE FROM CloudPaytypeSetting WHERE ID > 4",
                conn).ExecuteNonQuery();

            foreach (CloudBankSettingRow row in bankRows)
            {
                if (string.IsNullOrWhiteSpace(row.BankId) ||
                    string.IsNullOrWhiteSpace(row.PayTypeId))
                    continue;

                int newId = GetNextCloudPayTypeSettingId();

                string bankName = GetBankNameById(row.BankId);

                SqlCommand insertCommand = new SqlCommand(
                    "INSERT INTO CloudPaytypeSetting " +
                    "(ID, Name, NameEn, Paytype, CloudPaytypeID, BankId) " +
                    "VALUES (@ID, @Name, @NameEn, @Paytype, @CloudPaytypeID, @BankId)",
                    conn);

                insertCommand.Parameters.Add("@ID", SqlDbType.Int).Value = newId;
                insertCommand.Parameters.Add("@Name", SqlDbType.NVarChar).Value = bankName;
                insertCommand.Parameters.Add("@NameEn", SqlDbType.NVarChar).Value = bankName;
                insertCommand.Parameters.Add("@Paytype", SqlDbType.Int).Value = 2;
                insertCommand.Parameters.Add("@CloudPaytypeID", SqlDbType.NVarChar).Value =
                    row.PayTypeId;
                insertCommand.Parameters.Add("@BankId", SqlDbType.NVarChar).Value = row.BankId;

                insertCommand.ExecuteNonQuery();
            }
        }

        private int GetNextCloudPayTypeSettingId()
        {
            return Convert.ToInt32(
                new SqlCommand(
                    "SELECT ISNULL(MAX(ID), 0) + 1 FROM CloudPaytypeSetting",
                    conn).ExecuteScalar());
        }

        private string GetBankNameById(string bankId)
        {
            if (bankTable == null)
                return string.Empty;

            foreach (DataRow row in bankTable.Rows)
            {
                if ((row["id"]?.ToString() ?? string.Empty) == bankId)
                    return row["name"]?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        #endregion

        #region ── Update Items ───────────────────────────────

        private void BtnUpdateItems_Click(object sender, RoutedEventArgs e)
        {
            UpdateItems();
        }

        private void UpdateItems()
        {
            try
            {
                usp_ItemsPrices_SelectResult[] cloudItemPrices =
                    new ClsItem(Sync.APIUrl, "").GetCloudItemPrices();

                SqlDataAdapter settingsAdapter = new SqlDataAdapter(
                    "SELECT ID, Name, ISNULL(CloudID, 0) AS CloudID FROM CloudPriceSetting",
                    conn);

                DataTable settingsTable = new DataTable();
                settingsAdapter.Fill(settingsTable);

                if (settingsTable.Rows.Count == 0 || cloudItemPrices == null)
                    return;

                List<ProductPrices> productsPrices = new List<ProductPrices>();

                foreach (usp_ItemsPrices_SelectResult cloudPrice in cloudItemPrices)
                {
                    if (!cloudPrice.Item_ID.HasValue ||
                        !cloudPrice.Uom_ID.HasValue ||
                        !cloudPrice.PriceName_ID.HasValue ||
                        !cloudPrice.Price.HasValue)
                        continue;

                    ProductPrices productPrice = productsPrices
                        .FirstOrDefault(p => p.ProductId == cloudPrice.Item_ID.Value);

                    if (productPrice == null)
                    {
                        productPrice = new ProductPrices
                        {
                            ProductId = cloudPrice.Item_ID.Value,
                            UnitId = cloudPrice.Uom_ID.Value,
                            ClientCode = string.Empty,
                            date = DateTime.Now,
                            ISDeleted = false
                        };

                        productsPrices.Add(productPrice);
                    }

                    DataRow matchedSettingRow = settingsTable
                        .AsEnumerable()
                        .FirstOrDefault(row =>
                            Convert.ToInt32(row["CloudID"]) == cloudPrice.PriceName_ID.Value);

                    if (matchedSettingRow == null)
                        continue;

                    int settingId = Convert.ToInt32(matchedSettingRow["ID"]);
                    double priceValue = Convert.ToDouble(cloudPrice.Price.Value);

                    switch (settingId)
                    {
                        case 1:
                            productPrice.CompetitorPrice = priceValue;
                            break;
                        case 2:
                            productPrice.WholesalePrice = priceValue;
                            break;
                        case 3:
                            productPrice.ConsumerPrice = priceValue;
                            break;
                    }
                }

                if (productsPrices.Count > 0 &&
                    new EntityOperations().SaveProductsPrice(productsPrices))
                {
                    MessageBox.Show(
                        "تم تحديث الأصناف بنجاح ✅",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء تحديث الأصناف: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region ── Row Model ──────────────────────────────────────

    public class CloudBankSettingRow
    {
        public string BankId { get; set; } = string.Empty;
        public string PayTypeId { get; set; } = string.Empty;
    }

    #endregion
}