using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCloudPaymentSetting : Window
    {
        #region ── Private Fields ─────────────────────────────

        private SqlConnection conn;

        // مصادر البيانات للـ ComboBoxes في الجدول
        private DataTable _banksTable;
        private DataTable _payTypesTable;

        // مصدر بيانات الجدول
        private ObservableCollection<BankPayTypeRow> _bankRows;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCloudPaymentSetting()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            _bankRows = new ObservableCollection<BankPayTypeRow>();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPaymentTypes();
            loadBanks();
            LoadPayTypesForGrid();
            GridControl1.ItemsSource = _bankRows;
            loadSavedData();
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

        #region ── Data Loading ───────────────────────────────

        private void LoadPaymentTypes()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT CloudID, Name FROM CloudPayment", conn);

                DataTable mainTable = new DataTable();
                adapter.Fill(mainTable);

                cmbCredit.ItemsSource = mainTable.DefaultView;
                cmbCredit.DisplayMemberPath = "Name";
                cmbCredit.SelectedValuePath = "CloudID";
                cmbCredit.SelectedIndex = -1;

                cmbCash.ItemsSource = mainTable.Copy().DefaultView;
                cmbCash.DisplayMemberPath = "Name";
                cmbCash.SelectedValuePath = "CloudID";
                cmbCash.SelectedIndex = -1;

                cmbCreditCard.ItemsSource = mainTable.Copy().DefaultView;
                cmbCreditCard.DisplayMemberPath = "Name";
                cmbCreditCard.SelectedValuePath = "CloudID";
                cmbCreditCard.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل طرق الدفع", ex);
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

                _banksTable = new DataTable();
                adapter.Fill(_banksTable);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل البنوك", ex);
            }
        }

        private void LoadPayTypesForGrid()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT CloudID, Name FROM CloudPayment", conn);

                _payTypesTable = new DataTable();
                adapter.Fill(_payTypesTable);
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل أنواع الدفع", ex);
            }
        }

        private void loadSavedData()
        {
            try
            {
                // تحميل إعدادات الدفع الرئيسية
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT ID, CloudPaytypeID, Name, BankId " +
                    "FROM CloudPaytypeSetting",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["CloudPaytypeID"] == DBNull.Value ||
                        string.IsNullOrEmpty(row["CloudPaytypeID"]?.ToString()))
                        continue;

                    int settingId = Convert.ToInt32(row["ID"]);

                    switch (settingId)
                    {
                        case 1:
                            cmbCredit.SelectedValue = row["CloudPaytypeID"];
                            break;
                        case 2:
                            cmbCash.SelectedValue = row["CloudPaytypeID"];
                            break;
                        case 3:
                            cmbCreditCard.SelectedValue = row["CloudPaytypeID"];
                            break;
                    }
                }

                // تحميل إعدادات البنوك
                SqlDataAdapter bankAdapter = new SqlDataAdapter(
                    "SELECT ID, CloudPaytypeID, Name, BankId " +
                    "FROM CloudPaytypeSetting WHERE ID > 4",
                    conn);

                DataTable bankTable = new DataTable();
                bankAdapter.Fill(bankTable);

                _bankRows.Clear();

                foreach (DataRow row in bankTable.Rows)
                {
                    _bankRows.Add(new BankPayTypeRow
                    {
                        BankId = row["BankId"]?.ToString() ?? string.Empty,
                        PayTypeId = row["CloudPaytypeID"]?.ToString() ?? string.Empty,
                        BankName = GetBankName(row["BankId"]?.ToString()),
                        PayTypeName = GetPayTypeName(row["CloudPaytypeID"]?.ToString())
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل البيانات", ex);
            }
        }

        private string GetBankName(string bankId)
        {
            if (_banksTable == null) return bankId;
            foreach (DataRow row in _banksTable.Rows)
                if (row["id"]?.ToString() == bankId)
                    return row["name"]?.ToString() ?? bankId;
            return bankId;
        }

        private string GetPayTypeName(string cloudId)
        {
            if (_payTypesTable == null) return cloudId;
            foreach (DataRow row in _payTypesTable.Rows)
                if (row["CloudID"]?.ToString() == cloudId)
                    return row["Name"]?.ToString() ?? cloudId;
            return cloudId;
        }

        #endregion

        #region ── Grid Events ────────────────────────────────

        private void BankComboInGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && _banksTable != null)
            {
                comboBox.ItemsSource = _banksTable.DefaultView;
                comboBox.DisplayMemberPath = "name";
                comboBox.SelectedValuePath = "id";
            }
        }

        private void PayTypeComboInGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && _payTypesTable != null)
            {
                comboBox.ItemsSource = _payTypesTable.DefaultView;
                comboBox.DisplayMemberPath = "Name";
                comboBox.SelectedValuePath = "CloudID";
            }
        }

        private void btnAddBankRow_Click(object sender, RoutedEventArgs e)
        {
            _bankRows.Add(new BankPayTypeRow
            {
                BankId = string.Empty,
                PayTypeId = string.Empty,
                BankName = string.Empty,
                PayTypeName = string.Empty
            });
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BankPayTypeRow row)
            {
                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من الحذف؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                    _bankRows.Remove(row);
            }
        }

        #endregion

        #region ── Save ───────────────────────────────────────

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 0)
                {
                    MessageBox.Show(
                        "نأسف ليس لديك الصلاحية لتغيير الإعدادات",
                        "⚠️ تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                EnsureConnectionOpen(conn);

                // حفظ إعدادات الدفع الرئيسية
                if (cmbCredit.SelectedIndex != -1)
                    ExecuteNonQuery(conn,
                        $"UPDATE CloudPaytypeSetting " +
                        $"SET CloudPaytypeID = {cmbCredit.SelectedValue} " +
                        $"WHERE id = 1");

                if (cmbCash.SelectedIndex != -1)
                {
                    ExecuteNonQuery(conn,
                        $"UPDATE CloudPaytypeSetting " +
                        $"SET CloudPaytypeID = {cmbCash.SelectedValue} " +
                        $"WHERE id = 2");

                    ExecuteNonQuery(conn,
                        $"UPDATE CloudPaytypeSetting " +
                        $"SET CloudPaytypeID = {cmbCash.SelectedValue} " +
                        $"WHERE id = 4");
                }

                if (cmbCreditCard.SelectedIndex != -1)
                    ExecuteNonQuery(conn,
                        $"UPDATE CloudPaytypeSetting " +
                        $"SET CloudPaytypeID = {cmbCreditCard.SelectedValue}, " +
                        $"BankId = 1 WHERE ID = 3");

                // حفظ إعدادات البنوك
                if (_bankRows.Count > 0)
                {
                    ExecuteNonQuery(conn,
                        "DELETE FROM CloudPaytypeSetting WHERE ID > 4");

                    foreach (BankPayTypeRow row in _bankRows)
                    {
                        if (string.IsNullOrEmpty(row.BankId) ||
                            string.IsNullOrEmpty(row.PayTypeId))
                            continue;

                        int nextId = GetNextSettingId(conn);

                        SqlCommand insertCmd = new SqlCommand(
                            "INSERT INTO CloudPaytypeSetting " +
                            "(ID, Name, NameEn, Paytype, CloudPaytypeID, BankId) " +
                            "VALUES(@ID, @Name, @NameEn, @Paytype, @CloudPaytypeID, @BankId)",
                            conn);

                        insertCmd.Parameters.Add("@ID", SqlDbType.Int).Value = nextId;
                        insertCmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = row.BankName;
                        insertCmd.Parameters.Add("@NameEn", SqlDbType.NVarChar).Value = row.BankName;
                        insertCmd.Parameters.Add("@Paytype", SqlDbType.Int).Value = 2;
                        insertCmd.Parameters.Add("@CloudPaytypeID", SqlDbType.NVarChar).Value =
                            row.PayTypeId;
                        insertCmd.Parameters.Add("@BankId", SqlDbType.NVarChar).Value = row.BankId;
                        insertCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "تم حفظ الإعدادات بنجاح ✅",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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

        private static int GetNextSettingId(SqlConnection connection)
        {
            return Convert.ToInt32(
                new SqlCommand(
                    "SELECT ISNULL(MAX(ID), 0) + 1 FROM CloudPaytypeSetting",
                    connection).ExecuteScalar());
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            new SqlCommand(sql, connection).ExecuteNonQuery();
        }

        #endregion

        #region ── Helpers ────────────────────────────────────

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

            MessageBox.Show(
                message + detail,
                "❌ خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }

    #region ── Models ─────────────────────────────────────────

    public class BankPayTypeRow
    {
        public string BankId { get; set; }
        public string PayTypeId { get; set; }
        public string BankName { get; set; }
        public string PayTypeName { get; set; }
    }

    #endregion
}