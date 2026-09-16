using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SmartAuditERP.Form_WPF;
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmContractorPM : Window
    {
        #region Fields

        private SqlConnection _connection;
        private SqlConnection _connectionSecondary;
        private int _currentCode;
        private int _contractorType;
        private string _currentName;
        public bool isDone;
        private bool _isUpdated;
        private int _restrictionNumber;
        private int _entityType;
        public static string _selectedacc = "";

        #endregion

        #region Constructor

        public frmContractorPM()
        {
            InitializeComponent();
            Loaded += frmContractorPM_Loaded;

            _connection = MainClass.ConnObj();
            _connectionSecondary = MainClass.ConnObj();
            _currentCode = -1;
            _contractorType = 1;
            _currentName = "";
            isDone = false;
            _isUpdated = false;
            _restrictionNumber = 0;
            _entityType = 1;
        }

        #endregion

        #region Window Events

        private void frmContractorPM_Loaded(object sender, RoutedEventArgs e)
        {
            IValue.IsReadOnly = false;
            LoadCountries();
            LoadActs();
            LoadAccounts();
            credit.IsChecked = true;
            WindowState = MainClass.Window_State;
        }

        #endregion

        #region Clear / Reset

        private void ClearForm()
        {
            MainClass.CLRForm(this);
            txtAccCode.Text = MainClass.GenerateCode(2223);
            txtAccCode.IsReadOnly = false;
            chAddBankInf.IsChecked = false;
            _currentCode = -1;
            _currentName = "";
        }

        #endregion

        #region Data Loading

        private void LoadAccounts()
        {
            txtAccCode.Text = MainClass.GenerateCode(2223);
        }

        public void LoadCountries()
        {
            try
            {
                var adapter = new SqlDataAdapter("SELECT id, name FROM Countries ORDER BY id", _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbCountry.DisplayMemberPath = "name";
                cmbCountry.SelectedValuePath = "id";
                cmbCountry.ItemsSource = dataTable.DefaultView;
                cmbCountry.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الدول", ex);
            }
        }

        public void LoadActs()
        {
            try
            {
                var adapter = new SqlDataAdapter("SELECT id, name FROM Acts ORDER BY id", _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbActs.DisplayMemberPath = "name";
                cmbActs.SelectedValuePath = "id";
                cmbActs.ItemsSource = dataTable.DefaultView;
                cmbActs.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل مجالات العمل", ex);
            }
        }

        public void LoadCities(int countryId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Cities WHERE country={countryId} ORDER BY id",
                    _connectionSecondary);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbCity.DisplayMemberPath = "name";
                cmbCity.SelectedValuePath = "id";
                cmbCity.ItemsSource = dataTable.DefaultView;
                cmbCity.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المدن", ex);
            }
        }

        public void LoadAreas(int cityId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM areas WHERE city={cityId} ORDER BY id",
                    _connectionSecondary);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbArea.DisplayMemberPath = "name";
                cmbArea.SelectedValuePath = "id";
                cmbArea.ItemsSource = dataTable.DefaultView;
                cmbArea.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المناطق", ex);
            }
        }

        private void LoadDataGrid(string condition)
        {
            try
            {
                dgvCustomers.ItemsSource = null;

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM PM_Contractor WHERE {condition} IS_Deleted=0 ORDER BY id",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                var resultList = new System.Collections.Generic.List<ContractorRowModel>();

                foreach (DataRow row in dataTable.Rows)
                {
                    int countryId = GetSafeInt(row["country"]);
                    int cityId = GetSafeInt(row["city"]);
                    int areaId = GetSafeInt(row["area"]);

                    resultList.Add(new ContractorRowModel
                    {
                        ID = row["id"]?.ToString(),
                        Name = row["name"]?.ToString(),
                        NationalID = row["national_id"]?.ToString(),
                        Phone = row["tel"]?.ToString(),
                        Mobile = row["mobile"]?.ToString(),
                        Country = countryId != -1 ? GetNameById("Countries", countryId) : "",
                        City = cityId != -1 ? GetNameById("Cities", cityId) : "",
                        Area = areaId != -1 ? GetNameById("areas", areaId) : ""
                    });
                }

                dgvCustomers.ItemsSource = resultList;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            dgvCustomers.UnselectAll();

            try
            {
                EnsureConnectionOpen(_connection);

                using var command = new SqlCommand(sqlQuery, _connection);
                using var reader = command.ExecuteReader();
                ReadData(reader);

                IValue.IsReadOnly = true;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM PM_Contractor WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM PM_Contractor WHERE IS_Deleted=0 AND id>{_currentCode} ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM PM_Contractor WHERE IS_Deleted=0 AND id<{_currentCode} ORDER BY id DESC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM PM_Contractor WHERE IS_Deleted=0 ORDER BY id DESC");

        #endregion

        #region Read Data

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) return;
            reader.Read();

            ClearForm();

            _currentCode = Convert.ToInt32(reader["id"]);
            txtName.Text = reader["name"]?.ToString() ?? "";
            _currentName = txtName.Text;

            // Load account data
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE AName='{_currentName}'",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    IValue.Text = dataTable.Rows[0]["IValue"]?.ToString() ?? "";
                    if (Convert.ToInt32(dataTable.Rows[0]["Nature"]) == 1)
                        debt.IsChecked = true;
                    else
                        credit.IsChecked = true;
                }
            }
            catch { /* Ignore account load errors */ }

            // Country
            try
            {
                int countryId = GetSafeInt(reader["country"]);
                if (countryId != -1)
                {
                    cmbCountry.SelectedValue = countryId;
                    LoadCities(countryId);
                }
            }
            catch { }

            // City
            try
            {
                int cityId = GetSafeInt(reader["city"]);
                if (cityId != -1)
                {
                    cmbCity.SelectedValue = cityId;
                    LoadAreas(cityId);
                }
            }
            catch { }

            // Area
            try
            {
                int areaId = GetSafeInt(reader["area"]);
                if (areaId != -1)
                    cmbArea.SelectedValue = areaId;
            }
            catch { }

            // Activity
            try
            {
                int actId = GetSafeInt(reader["act"]);
                if (actId != -1)
                    cmbActs.SelectedValue = actId;
            }
            catch { }

            txtNationalID.Text = reader["national_id"]?.ToString() ?? "";
            txtTel.Text = reader["tel"]?.ToString() ?? "";
            txtMobile.Text = reader["mobile"]?.ToString() ?? "";
            txtEmail.Text = reader["email"]?.ToString() ?? "";
            txtNotes.Text = reader["notes"]?.ToString() ?? "";
            txtBankName.Text = reader["bankName"]?.ToString() ?? "";
            txtBankAccount.Text = reader["bankAccount"]?.ToString() ?? "";
            txtBankAddress.Text = reader["bankAddress"]?.ToString() ?? "";
            txtSwiftCode.Text = reader["swiftCode"]?.ToString() ?? "";
            txtIBAN.Text = reader["IBAN"]?.ToString() ?? "";

            chAddBankInf.IsChecked = !string.IsNullOrWhiteSpace(txtBankName.Text);

            // Tax number
            try { txtTaxNo.Text = reader["tax_no"]?.ToString() ?? ""; }
            catch { }

            // Account code
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE AName='{txtName.Text}'",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    txtAccCode.Text = dataTable.Rows[0][0]?.ToString() ?? "";

                txtAccCode.IsReadOnly = true;
            }
            catch { }

            // ID Scan Image
            try
            {
                byte[] imageBytes = reader["IdScan"] as byte[];
                if (imageBytes != null && imageBytes.Length > 0)
                    IDscan.Source = LoadBitmapFromBytes(imageBytes);
            }
            catch { }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Trial check
                if (MainClass.IsTrial)
                {
                    var trialAdapter = new SqlDataAdapter("SELECT id FROM PM_Contractor", _connectionSecondary);
                    var trialTable = new DataTable();
                    trialAdapter.Fill(trialTable);

                    if (trialTable.Rows.Count >= 10)
                    {
                        string trialMsg = MainClass.Language == "en"
                            ? "Sorry, You reached the maximum entries for the trial version."
                            : "نأسف، لقد وصلت لأقصى حد إدخال للنسخة التجريبية.";
                        MessageBox.Show(trialMsg, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // Validate name
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("يرجى إدخال الاسم", "تحقق من البيانات", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                // Validate bank fields if checkbox checked
                if (chAddBankInf.IsChecked == true)
                {
                    foreach (var element in GetBankTextBoxes())
                    {
                        if (string.IsNullOrWhiteSpace(element.Text))
                        {
                            MessageBox.Show("يرجى إدخال كل بيانات البنك", "تحقق من البيانات",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            element.Focus();
                            return;
                        }
                    }
                }

                EnsureConnectionOpen(_connection);

                SqlCommand command;

                if (_currentCode == -1)
                {
                    // Validate tax number
                    if (!string.IsNullOrEmpty(txtTaxNo.Text) && !Common.ISValidTaxNo(txtTaxNo.Text))
                    {
                        txtTaxNo.Focus();
                        return;
                    }

                    // Check duplicate
                    var dupAdapter = new SqlDataAdapter(
                        $"SELECT id FROM PM_Contractor WHERE name=N'{txtName.Text.Trim()}' AND mobile=N'{txtMobile.Text.Trim()}'",
                        _connectionSecondary);
                    var dupTable = new DataTable();
                    dupAdapter.Fill(dupTable);

                    if (dupTable.Rows.Count > 0)
                    {
                        MessageBox.Show("هذا الإسم مدخل من قبل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtName.Focus();
                        return;
                    }

                    command = new SqlCommand(
                        @"INSERT INTO PM_Contractor
                          (name,country,city,area,act,national_id,tel,mobile,email,notes,type,tax_no,
                           IS_Deleted,AccountCode,IdScan,bankName,bankAccount,bankAddress,swiftCode,IBAN)
                          VALUES
                          (@name,@country,@city,@area,@act,@national_id,@tel,@mobile,@email,@notes,@type,@tax_no,
                           @IS_Deleted,@AccountCode,@IdScan,@bankName,@bankAccount,@bankAddress,@swiftCode,@IBAN)",
                        _connection);
                }
                else
                {
                    command = new SqlCommand(
                        $@"UPDATE PM_Contractor SET
                           name=@name,country=@country,city=@city,area=@area,act=@act,
                           national_id=@national_id,tel=@tel,mobile=@mobile,email=@email,
                           notes=@notes,type=@type,tax_no=@tax_no,AccountCode=@AccountCode,
                           IdScan=@IdScan,bankName=@bankName,bankAccount=@bankAccount,
                           bankAddress=@bankAddress,swiftCode=@swiftCode,IBAN=@IBAN
                           WHERE id={_currentCode}",
                        _connection);
                }

                // Build parameters
                command.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtName.Text;
                command.Parameters.Add("@country", SqlDbType.Int).Value = GetSelectedId(cmbCountry);
                command.Parameters.Add("@city", SqlDbType.Int).Value = GetSelectedId(cmbCity);
                command.Parameters.Add("@area", SqlDbType.Int).Value = GetSelectedId(cmbArea);
                command.Parameters.Add("@act", SqlDbType.Int).Value = GetSelectedId(cmbActs);
                command.Parameters.Add("@AccountCode", SqlDbType.Int).Value = ParseInt(txtAccCode.Text);
                command.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = txtNationalID.Text;
                command.Parameters.Add("@tel", SqlDbType.NVarChar).Value = txtTel.Text;
                command.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = txtMobile.Text;
                command.Parameters.Add("@email", SqlDbType.NVarChar).Value = txtEmail.Text;
                command.Parameters.Add("@notes", SqlDbType.NVarChar).Value = txtNotes.Text;
                command.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                command.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = txtTaxNo.Text;
                command.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                command.Parameters.Add("@bankName", SqlDbType.NVarChar).Value = txtBankName.Text;
                command.Parameters.Add("@bankAccount", SqlDbType.VarChar).Value = txtBankAccount.Text;
                command.Parameters.Add("@bankAddress", SqlDbType.NVarChar).Value = txtBankAddress.Text;
                command.Parameters.Add("@swiftCode", SqlDbType.VarChar).Value = txtSwiftCode.Text;
                command.Parameters.Add("@IBAN", SqlDbType.VarChar).Value = txtIBAN.Text;

                // ID Scan image
                if (IDscan.Source != null)
                    command.Parameters.Add("@IdScan", SqlDbType.Image).Value = ImageSourceToBytes(IDscan.Source);
                else
                    command.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;

                command.ExecuteNonQuery();

                // Account index handling
                if (_currentCode == -1)
                {
                    SaveNewAccountIndex();
                }
                else
                {
                    UpdateExistingAccountIndex();
                }

                isDone = true;
                ShowSaveSuccessMessage();
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

        private void SaveNewAccountIndex()
        {
            int accountNature = debt.IsChecked == true ? 1 : 2;
            const string parentCode = "2223";

            var adapter = new SqlDataAdapter(
                $"SELECT * FROM Accounts_Index WHERE code='{txtAccCode.Text}'",
                _connection);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            if (dataTable.Rows.Count == 0)
            {
                var insertCommand = new SqlCommand(
                    $@"INSERT INTO Accounts_Index
                       (Code,AName,Type,ParentCode,FinalAcc,Acc_branch,Nature,IValue,UserName,date)
                       VALUES
                       ('{txtAccCode.Text}','{txtName.Text}',2,'{parentCode}',1,
                        {MainClass.BranchNo},{accountNature},{ParseDouble(IValue.Text)},
                        {MainClass.EmpNo},@date)",
                    _connection);
                insertCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                insertCommand.ExecuteNonQuery();

                if (!string.IsNullOrEmpty(IValue.Text))
                {
                    _isUpdated = true;
                    InsertRestriction();
                }
            }
        }

        private void UpdateExistingAccountIndex()
        {
            int accountNature = debt.IsChecked == true ? 1 : 2;

            var adapter = new SqlDataAdapter(
                $"SELECT * FROM Accounts_Index WHERE AName='{_currentName}'",
                _connection);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            if (dataTable.Rows.Count > 0)
            {
                new SqlCommand(
                    $"UPDATE Accounts_Index SET AName='{txtName.Text}', Nature={accountNature}, " +
                    $"IValue={ParseDouble(IValue.Text)} WHERE Code='{dataTable.Rows[0]["Code"]}'",
                    _connection).ExecuteNonQuery();
            }
        }

        private void ShowSaveSuccessMessage()
        {
            var saveMsg = new frmSavedMsg();
            if (_currentCode != -1)
                saveMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";

            saveMsg.ShowDialog();

            if (saveMsg.Pressed == 1)
            {
                _currentCode = GetLastInsertedId();
                ClearForm();
            }
            else if (saveMsg.Pressed == 3)
            {
                Close();
            }
        }

        private int GetLastInsertedId()
        {
            using var cmd = new SqlCommand("SELECT MAX(id) FROM PM_Contractor", _connection);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        #endregion

        #region Insert Restriction (Journal Entry)

        private void InsertRestriction()
        {
            SqlTransaction transaction = null;
            try
            {
                transaction = _connection.BeginTransaction();

                string branchCondition = MainClass.BranchNo != -1
                    ? $" WHERE branch={MainClass.BranchNo}"
                    : "";

                double debitAmount = 0;
                double creditAmount = 0;

                if (debt.IsChecked == true)
                {
                    debitAmount = ParseDouble(IValue.Text);
                    creditAmount = 0;
                }
                else
                {
                    debitAmount = 0;
                    creditAmount = ParseDouble(IValue.Text);
                }

                int entryId;
                using (var maxCmd = new SqlCommand(
                    $"SELECT MAX(id) FROM Entry{branchCondition}",
                    _connection, transaction))
                {
                    entryId = Convert.ToInt32(maxCmd.ExecuteScalar()) + 1;
                }

                string entryNotes = $" قيد الرصيد الافتتاحي ل: {txtName.Text}  بتاريخ  {DateTime.Now}";

                // Insert main entry
                var entryCmd = new SqlCommand(
                    @"INSERT INTO Entry(id,Date,doc_no,type,state,notes,branch,IS_Deleted)
                      VALUES(@id,@Date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    _connection, transaction);

                entryCmd.Parameters.Add("@id", SqlDbType.Int).Value = entryId;
                entryCmd.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
                entryCmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = 30;
                entryCmd.Parameters.Add("@type", SqlDbType.Int).Value = 0;
                entryCmd.Parameters.Add("@state", SqlDbType.Int).Value = 1;
                entryCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = entryNotes;
                entryCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                entryCmd.ExecuteNonQuery();

                // Insert sub-entry 1 (contractor account)
                InsertEntrySubLine(_connection, transaction, entryId,
                    debitAmount, creditAmount,
                    ParseDouble(txtAccCode.Text),
                    entryNotes, MainClass.BranchNo);

                // Insert sub-entry 2 (opening balance account)
                InsertEntrySubLine(_connection, transaction, entryId,
                    creditAmount, debitAmount,
                    12110001,
                    $" قيد الرصيد الافتتاحي ل: 12110001  بتاريخ  {DateTime.Now}",
                    MainClass.BranchNo);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ShowError("خطأ أثناء حفظ القيد", ex);
            }
        }

        private static void InsertEntrySubLine(
            SqlConnection connection, SqlTransaction transaction,
            int entryId, double debit, double credit,
            double accountNumber, string notes, int branchNumber)
        {
            var subCmd = new SqlCommand(
                @"INSERT INTO Entry_sub(res_id,dept,credit,acc_no,notes,branch)
                  VALUES(@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                connection, transaction);

            subCmd.Parameters.Add("@res_id", SqlDbType.Int).Value = entryId;
            subCmd.Parameters.Add("@dept", SqlDbType.Float).Value = debit;
            subCmd.Parameters.Add("@credit", SqlDbType.Float).Value = credit;
            subCmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = (int)accountNumber;
            subCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = notes;
            subCmd.Parameters.Add("@branch", SqlDbType.Int).Value = branchNumber;
            subCmd.ExecuteNonQuery();
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode == -1)
            {
                MessageBox.Show("اختر مقاولاً ليتم حذفه", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("هل أنت متأكد من حذف هذا السجل؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                EnsureConnectionOpen(_connection);

                new SqlCommand(
                    $"UPDATE PM_Contractor SET IS_Deleted=1 WHERE id={_currentCode}",
                    _connection).ExecuteNonQuery();

                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName='{txtName.Text}'",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    int accountCode = ParseInt(dataTable.Rows[0][0]?.ToString());
                    new SqlCommand(
                        $"DELETE FROM Accounts_Index WHERE Code={accountCode}",
                        _connection).ExecuteNonQuery();
                }

                ClearForm();
                MessageBox.Show("✅ تم الحذف بنجاح", "تمّ", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #endregion

        #region Search

        private void Search()
        {
            string condition = "";
            if (!string.IsNullOrWhiteSpace(txtNameSrch.Text))
                condition = $"name LIKE '%{txtNameSrch.Text}%' AND ";

            LoadDataGrid(condition);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => Search();

        private void txtNameSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }

        private void txtNameSrch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-search can be enabled here if needed
        }

        private void dgvCustomers_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvCustomers.SelectedItem is ContractorRowModel selectedRow
                && int.TryParse(selectedRow.ID, out int rowId))
            {
                _currentCode = rowId;
                Navigate($"SELECT * FROM PM_Contractor WHERE id={_currentCode}");
            }
        }

        #endregion

        #region ComboBox Events

        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;
                cmbCity.ItemsSource = null;

                if (cmbCountry.SelectedValue is int countryId && countryId > 0)
                    LoadCities(countryId);
            }
            catch { }
        }

        private void cmbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;

                if (cmbCity.SelectedValue is int cityId && cityId > 0)
                    LoadAreas(cityId);
            }
            catch { }
        }

        #endregion

        #region Add New Lookup Dialogs

        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedCountryId = GetSelectedId(cmbCountry);

            var form = new frmCountries();
            form.ShowDialog();

            LoadCountries();
            TryRestoreSelection(cmbCountry, savedCountryId);
        }

        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                MessageBox.Show("يجب اختيار الدولة التابع لها المدينة أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            int savedCityId = GetSelectedId(cmbCity);
            int countryId = GetSelectedId(cmbCountry);

            var form = new frmCities();
            form.LoadCountries();
            form.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            form.ShowDialog();

            LoadCities(countryId);
            TryRestoreSelection(cmbCity, savedCityId);
        }

        private void btnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                MessageBox.Show("يجب اختيار الدولة التابع لها المنطقة أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            if (cmbCity.SelectedValue == null)
            {
                MessageBox.Show("يجب اختيار المدينة التابع لها المنطقة أولاً", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCity.Focus();
                return;
            }

            int savedAreaId = GetSelectedId(cmbArea);
            int cityId = GetSelectedId(cmbCity);
            int countryId = GetSelectedId(cmbCountry);

            var form = new frmAreas();
            form.LoadCountries();
            form.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            form.LoadCities(countryId);
            form.cmbCity.SelectedValue = cmbCity.SelectedValue;
            form.ShowDialog();

            LoadAreas(cityId);
            TryRestoreSelection(cmbArea, savedAreaId);
        }

        private void btnActAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedActId = GetSelectedId(cmbActs);

            var form = new frmActs();
            form.ShowDialog();

            LoadActs();
            TryRestoreSelection(cmbActs, savedActId);
        }

        #endregion

        #region Account Code TextChanged

        private void txtAccCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT aname, parentcode FROM Accounts_Index WHERE code='{txtAccCode.Text}'",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0
                    && dataTable.Rows[0][1].ToString() == "2223")
                {
                    txtName.Text = dataTable.Rows[0][0].ToString();
                }
            }
            catch { }
        }

        #endregion

        #region Button1 (Select Account)

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _selectedacc = "";
                new frmSelectClient().ShowDialog();
                txtAccCode.Text = _selectedacc;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في اختيار الحساب", ex);
            }
        }

        #endregion

        #region Scan

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
            //    foreach (object item in TwainHandler.ScanImages())
            //    {
           //         string imagePath = item?.ToString();
           //         if (!string.IsNullOrEmpty(imagePath))
           //         {
           //             IDscan.Source = new BitmapImage(new Uri(imagePath));
           //             break;
           //         }
           //     }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء المسح الضوئي", ex);
            }
        }

        #endregion

        #region Bank Info Checkbox

        private void chAddBankInf_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = chAddBankInf.IsChecked == true;
            BankInfPanel.IsEnabled = isChecked;

            foreach (var textBox in GetBankTextBoxes())
                textBox.IsEnabled = isChecked;
        }

        #endregion

        #region Toolbar Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => ClearForm();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // Print logic here
        }

        #endregion

        #region Helper Methods

        private string GetNameById(string tableName, int id)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM {tableName} WHERE id={id}",
                    _connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable.Rows.Count > 0 ? dataTable.Rows[0][0]?.ToString() ?? "" : "";
            }
            catch { return ""; }
        }

        private static int GetSafeInt(object value)
        {
            if (value == null || value == DBNull.Value) return -1;
            return int.TryParse(value.ToString(), out int result) ? result : -1;
        }

        private static int GetSelectedId(ComboBox comboBox)
        {
            if (comboBox.SelectedValue == null) return -1;
            return int.TryParse(comboBox.SelectedValue.ToString(), out int id) ? id : -1;
        }

        private static void TryRestoreSelection(ComboBox comboBox, int savedId)
        {
            try
            {
                if (savedId != -1)
                    comboBox.SelectedValue = savedId;
            }
            catch { }
        }

        private static int ParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return int.TryParse(text, out int result) ? result : 0;
        }

        private static double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return double.TryParse(text, out double result) ? result : 0;
        }

        private System.Collections.Generic.IEnumerable<TextBox> GetBankTextBoxes()
        {
            yield return txtBankName;
            yield return txtBankAccount;
            yield return txtBankAddress;
            yield return txtSwiftCode;
            yield return txtIBAN;
        }

        private static BitmapImage LoadBitmapFromBytes(byte[] imageBytes)
        {
            using var stream = new MemoryStream(imageBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static byte[] ImageSourceToBytes(System.Windows.Media.ImageSource imageSource)
        {
            if (imageSource is BitmapSource bitmapSource)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                return stream.ToArray();
            }
            return null;
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

        private static void ShowError(string title, Exception ex)
        {
            MessageBox.Show(
                $"{title}{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion

        #region Data Model

        private class ContractorRowModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string NationalID { get; set; }
            public string Phone { get; set; }
            public string Mobile { get; set; }
            public string Country { get; set; }
            public string City { get; set; }
            public string Area { get; set; }
        }

        #endregion
    }
}