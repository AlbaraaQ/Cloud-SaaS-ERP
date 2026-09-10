using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmClientPM : Window
    {
        #region ── Public Fields ──────────────────────────────

        public bool isDone { get; set; } = false;
        public static string _selectedacc { get; set; } = string.Empty;

        #endregion

        #region ── Private Fields ─────────────────────────────

        private SqlConnection conn;
        private SqlConnection conn1;

        private int _currentCode;
        private int _customerType;
        private string _currentName;
        private bool _isUpdated;
        private int _entryNo;
        private int _recordType;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmClientPM()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            _currentCode = -1;
            _customerType = 1;
            _currentName = string.Empty;
            isDone = false;
            _isUpdated = false;
            _entryNo = 0;
            _recordType = 1;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IValue.IsReadOnly = false;

            LoadCountries();
            LoadActs();

            if (rdSupplier.IsChecked == true)
                _customerType = 2;
            else if (rbOwner.IsChecked == true)
                _customerType = 3;
            else
                _customerType = 1;

            LoadAccounts();

            if (rdSupplier.IsChecked == true || rbOwner.IsChecked == true)
                credit.IsChecked = true;

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
            txtMobile.Text = string.Empty;
            txtNationalID.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtTel.Text = string.Empty;
            txtFax.Text = string.Empty;
            txtNotes.Text = string.Empty;
            txtTaxNo.Text = string.Empty;
            IValue.Text = string.Empty;
            IDscan.Source = null;

            cmbCountry.SelectedIndex = -1;
            cmbCity.SelectedIndex = -1;
            cmbArea.SelectedIndex = -1;
            cmbActs.SelectedIndex = -1;

            rdClient.IsChecked = true;
            debt.IsChecked = true;

            _currentCode = -1;
            _currentName = string.Empty;

            LoadAccounts();
            UpdateRecordIndicator("📍 سجل جديد");
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        public void LoadCountries()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT id, name FROM Countries ORDER BY id", conn);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbCountry.ItemsSource = dataTable.DefaultView;
            cmbCountry.DisplayMemberPath = "name";
            cmbCountry.SelectedValuePath = "id";
            cmbCountry.SelectedIndex = -1;
        }

        public void LoadActs()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT id, name FROM Acts ORDER BY id", conn);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbActs.ItemsSource = dataTable.DefaultView;
            cmbActs.DisplayMemberPath = "name";
            cmbActs.SelectedValuePath = "id";
            cmbActs.SelectedIndex = -1;
        }

        public void LoadCities(int countryId)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                $"SELECT id, name FROM Cities WHERE country = {countryId} ORDER BY id",
                conn1);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbCity.ItemsSource = dataTable.DefaultView;
            cmbCity.DisplayMemberPath = "name";
            cmbCity.SelectedValuePath = "id";
            cmbCity.SelectedIndex = -1;
        }

        public void LoadAreas(int cityId)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                $"SELECT id, name FROM areas WHERE city = {cityId} ORDER BY id",
                conn1);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbArea.ItemsSource = dataTable.DefaultView;
            cmbArea.DisplayMemberPath = "name";
            cmbArea.SelectedValuePath = "id";
            cmbArea.SelectedIndex = -1;
        }

        private void LoadAccounts()
        {
            if (rdClient.IsChecked == true)
                txtAccCode.Text = MainClass.GenerateCode(123);

            if (rdSupplier.IsChecked == true)
                txtAccCode.Text = MainClass.GenerateCode(2211);

            if (rbOwner.IsChecked == true)
                txtAccCode.Text = MainClass.GenerateCode(2212);
        }

        private void LoadCustomersGrid(string condition)
        {
            try
            {
                dgvCustomers.ItemsSource = null;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Customers WHERE {condition} " +
                    $"IS_Deleted = 0 AND (type = {_customerType} OR type = 3) ORDER BY id",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                var rows = new List<CustomerGridRow>();

                foreach (DataRow row in dataTable.Rows)
                {
                    rows.Add(new CustomerGridRow
                    {
                        CustomerId = Convert.ToInt32(row["id"]),
                        CustomerName = row["name"]?.ToString() ?? string.Empty,
                        NationalId = row["national_id"]?.ToString() ?? string.Empty,
                        Tel = row["tel"]?.ToString() ?? string.Empty,
                        Mobile = row["mobile"]?.ToString() ?? string.Empty,
                        CountryName = GetNameById("Countries", SafeToInt(row["country"])),
                        CityName = GetNameById("Cities", SafeToInt(row["city"])),
                        AreaName = GetNameById("areas", SafeToInt(row["area"]))
                    });
                }

                dgvCustomers.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل بيانات العملاء", ex);
            }
        }

        #endregion

        #region ── Navigation ─────────────────────────────────

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM customers " +
                $"WHERE type = {_recordType} AND IS_Deleted = 0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM customers " +
                $"WHERE type = {_recordType} AND IS_Deleted = 0 " +
                $"AND id < {_currentCode} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM customers " +
                $"WHERE type = {_recordType} AND IS_Deleted = 0 " +
                $"AND id > {_currentCode} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM customers " +
                $"WHERE type = {_recordType} AND IS_Deleted = 0 ORDER BY id DESC");
        }

        private void NavigateTo(string sqlQuery)
        {
            try
            {
                dgvCustomers.UnselectAll();
                EnsureConnectionOpen(conn);

                SqlCommand command = new SqlCommand(sqlQuery, conn);
                SqlDataReader dataReader = command.ExecuteReader();
                ReadDataFromReader(dataReader);

                IValue.IsReadOnly = true;
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
            ClearForm();

            try
            {
                _currentCode = Convert.ToInt32(dataReader["id"]);
                _currentName = dataReader["name"]?.ToString() ?? string.Empty;

                txtName.Text = _currentName;
                txtNationalID.Text = dataReader["national_id"]?.ToString() ?? string.Empty;
                txtTel.Text = dataReader["tel"]?.ToString() ?? string.Empty;
                txtMobile.Text = dataReader["mobile"]?.ToString() ?? string.Empty;
                txtEmail.Text = dataReader["email"]?.ToString() ?? string.Empty;
                txtFax.Text = dataReader["fax"]?.ToString() ?? string.Empty;
                txtNotes.Text = dataReader["notes"]?.ToString() ?? string.Empty;

                try { txtTaxNo.Text = dataReader["tax_no"]?.ToString() ?? string.Empty; } catch { }

                // حساب الرصيد
                LoadAccountData(_currentName);

                // الدولة والمدينة
                int countryId = SafeToInt(dataReader["country"]);
                if (countryId != -1)
                {
                    cmbCountry.SelectedValue = countryId;
                    LoadCities(countryId);
                }

                int cityId = SafeToInt(dataReader["city"]);
                if (cityId != -1)
                {
                    cmbCity.SelectedValue = cityId;
                    LoadAreas(cityId);
                }

                int areaId = SafeToInt(dataReader["area"]);
                if (areaId != -1)
                    cmbArea.SelectedValue = areaId;

                int actId = SafeToInt(dataReader["act"]);
                if (actId != -1)
                    cmbActs.SelectedValue = actId;

                // نوع الحساب
                rdClient.IsChecked = true;
                int type = Convert.ToInt32(dataReader["type"]);
                if (type == 2)
                    rdSupplier.IsChecked = true;

                // رقم الحساب
                LoadAccountCode(_currentName);

                UpdateRecordIndicator($"📍 {_currentName}");
            }
            catch { }
            finally
            {
                dataReader.Close();
            }
        }

        private void LoadAccountData(string name)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE AName = '{name}'", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    IValue.Text = dataTable.Rows[0]["IValue"]?.ToString() ?? string.Empty;
                    int nature = Convert.ToInt32(dataTable.Rows[0]["Nature"]);
                    if (nature == 1)
                        debt.IsChecked = true;
                    else
                        credit.IsChecked = true;
                }
            }
            catch { }
        }

        private void LoadAccountCode(string name)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE AName = '{name}'", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    txtAccCode.Text = dataTable.Rows[0][0]?.ToString() ?? string.Empty;
                    txtAccCode.IsReadOnly = true;
                }
            }
            catch { }
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
                if (CheckTrialLimit())
                    return;

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowWarning("يرجى إدخال الاسم");
                    txtName.Focus();
                    return;
                }

                EnsureConnectionOpen(conn);

                int customerType = 1;
                if (rdSupplier.IsChecked == true) customerType = 2;
                if (rbOwner.IsChecked == true) customerType = 3;

                SqlCommand command;

                if (_currentCode == -1)
                {
                    if (!string.IsNullOrWhiteSpace(txtTaxNo.Text) &&
                        !Common.ISValidTaxNo(txtTaxNo.Text))
                    {
                        txtTaxNo.Focus();
                        return;
                    }

                    // التحقق من التكرار
                    SqlDataAdapter dupAdapter = new SqlDataAdapter(
                        $"SELECT id FROM Customers " +
                        $"WHERE type = {customerType} " +
                        $"AND name = N'{txtName.Text.Trim()}' " +
                        $"AND mobile = N'{txtMobile.Text.Trim()}'",
                        conn1);
                    DataTable dupTable = new DataTable();
                    dupAdapter.Fill(dupTable);

                    if (dupTable.Rows.Count > 0)
                    {
                        ShowWarning("هذا الإسم مدخل من قبل");
                        txtName.Focus();
                        return;
                    }

                    command = new SqlCommand(
                        "INSERT INTO Customers(name,country,city,area,act,national_id," +
                        "tel,mobile,fax,email,notes,type,tax_no,IS_Deleted,AccountCode,IdScan) " +
                        "VALUES(@name,@country,@city,@area,@act,@national_id," +
                        "@tel,@mobile,@fax,@email,@notes,@type,@tax_no,@IS_Deleted,@AccountCode,@IdScan)",
                        conn);
                }
                else
                {
                    command = new SqlCommand(
                        "UPDATE Customers SET name=@name,country=@country,city=@city," +
                        "area=@area,act=@act,national_id=@national_id,tel=@tel,mobile=@mobile," +
                        "fax=@fax,email=@email,notes=@notes,type=@type,tax_no=@tax_no," +
                        $"AccountCode=@AccountCode,IdScan=@IdScan WHERE id = {_currentCode}",
                        conn);
                }

                AddCustomerParameters(command, customerType);
                command.ExecuteNonQuery();

                if (_currentCode == -1)
                    SaveNewAccount(customerType);
                else
                    UpdateExistingAccount();

                ShowSavedDialog();
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

        private void AddCustomerParameters(SqlCommand command, int customerType)
        {
            command.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtName.Text;
            command.Parameters.Add("@country", SqlDbType.Int).Value =
                cmbCountry.SelectedValue != null ? Convert.ToInt32(cmbCountry.SelectedValue) : -1;
            command.Parameters.Add("@city", SqlDbType.Int).Value =
                cmbCity.SelectedValue != null ? Convert.ToInt32(cmbCity.SelectedValue) : -1;
            command.Parameters.Add("@area", SqlDbType.Int).Value =
                cmbArea.SelectedValue != null ? Convert.ToInt32(cmbArea.SelectedValue) : -1;
            command.Parameters.Add("@act", SqlDbType.Int).Value =
                cmbActs.SelectedValue != null ? Convert.ToInt32(cmbActs.SelectedValue) : -1;
            command.Parameters.Add("@AccountCode", SqlDbType.Int).Value =
                (int)Math.Round(Convert.ToDouble(txtAccCode.Text));
            command.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = txtNationalID.Text;
            command.Parameters.Add("@tel", SqlDbType.NVarChar).Value = txtTel.Text;
            command.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = txtMobile.Text;
            command.Parameters.Add("@fax", SqlDbType.NVarChar).Value = txtFax.Text;
            command.Parameters.Add("@email", SqlDbType.NVarChar).Value = txtEmail.Text;
            command.Parameters.Add("@notes", SqlDbType.NVarChar).Value = txtNotes.Text;
            command.Parameters.Add("@type", SqlDbType.Int).Value = customerType;
            command.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = txtTaxNo.Text;
            command.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
            command.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;
        }

        private void SaveNewAccount(int customerType)
        {
            try
            {
                int nature = debt.IsChecked == true ? 1 : 2;
                string parentCode = customerType == 2 ? Common.CurrentBranch.SupliersAcc
                                  : customerType == 3 ? "2212"
                                  : Common.CurrentBranch.CustomersAcc;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE code = N'{txtAccCode.Text}'",
                    conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                {
                    SqlCommand insertCmd = new SqlCommand(
                        $"INSERT INTO Accounts_Index(Code,AName,Type,ParentCode,FinalAcc," +
                        $"Acc_branch,Nature,IValue,UserName,date) " +
                        $"VALUES(N'{txtAccCode.Text}',N'{txtName.Text}',2,N'{parentCode}',1," +
                        $"{MainClass.BranchNo},{nature},{ConvertToDouble(IValue.Text)}," +
                        $"{MainClass.UserID},@date)",
                        conn);
                    insertCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                    insertCmd.ExecuteNonQuery();

                    if (!string.IsNullOrWhiteSpace(IValue.Text))
                    {
                        _isUpdated = true;
                        InsertOpeningBalanceEntry();
                    }
                }
            }
            catch { }
        }

        private void UpdateExistingAccount()
        {
            try
            {
                int nature = debt.IsChecked == true ? 1 : 2;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE AName = N'{_currentName}'",
                    conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    new SqlCommand(
                        $"UPDATE Accounts_Index SET AName = N'{txtName.Text}', " +
                        $"Nature = {nature}, " +
                        $"IValue = {ConvertToDouble(IValue.Text)} " +
                        $"WHERE Code = N'{dataTable.Rows[0]["Code"]}'",
                        conn).ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void InsertOpeningBalanceEntry()
        {
            SqlTransaction transaction = null;
            try
            {
                transaction = conn.BeginTransaction();

                string branchCondition = MainClass.BranchNo != -1
                    ? $" WHERE branch = {MainClass.BranchNo}"
                    : string.Empty;

                int entryId = Convert.ToInt32(
                    new SqlCommand(
                        $"SELECT ISNULL(MAX(id), 0) FROM Entry{branchCondition}",
                        conn, transaction).ExecuteScalar()) + 1;

                double debit = debt.IsChecked == true ? ConvertToDouble(IValue.Text) : 0.0;
                double credit = debt.IsChecked == true ? 0.0 : ConvertToDouble(IValue.Text);

                string note = $" قيد الرصيد الافتتاحي ل: {txtName.Text}  بتاريخ  {DateTime.Now}";

                SqlCommand entryCmd = new SqlCommand(
                    "INSERT INTO Entry(id,Date,doc_no,type,state,notes,branch,IS_Deleted) " +
                    "VALUES(@id,@Date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    conn, transaction);

                entryCmd.Parameters.Add("@id", SqlDbType.Int).Value = entryId;
                entryCmd.Parameters.Add("@Date", SqlDbType.DateTime).Value = DateTime.Now;
                entryCmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = 30;
                entryCmd.Parameters.Add("@type", SqlDbType.Int).Value = 0;
                entryCmd.Parameters.Add("@state", SqlDbType.Int).Value = 1;
                entryCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = note;
                entryCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                entryCmd.ExecuteNonQuery();

                InsertEntryLine(conn, transaction, entryId, debit, credit,
                    (int)ConvertToDouble(txtAccCode.Text), note);
                InsertEntryLine(conn, transaction, entryId, credit, debit,
                    12110001, $" قيد الرصيد الافتتاحي ل: 12110001  بتاريخ  {DateTime.Now}");

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ShowError("خطأ أثناء حفظ القيد", ex);
            }
        }

        private void InsertEntryLine(
            SqlConnection connection,
            SqlTransaction transaction,
            int entryId,
            double debit,
            double credit,
            int accountCode,
            string notes)
        {
            SqlCommand subCmd = new SqlCommand(
                "INSERT INTO Entry_sub(res_id,dept,credit,acc_no,notes,branch) " +
                "VALUES(@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                connection, transaction);

            subCmd.Parameters.Add("@res_id", SqlDbType.Int).Value = entryId;
            subCmd.Parameters.Add("@dept", SqlDbType.Float).Value = debit;
            subCmd.Parameters.Add("@credit", SqlDbType.Float).Value = credit;
            subCmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = accountCode;
            subCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = notes;
            subCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
            subCmd.ExecuteNonQuery();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    ShowWarning("اختر عميلاً ليتم حذفه");
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من حذف هذا العميل؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(conn);

                new SqlCommand(
                    $"UPDATE Customers SET IS_Deleted = 1 WHERE id = {_currentCode}",
                    conn).ExecuteNonQuery();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type = 2 AND AName = '{txtName.Text}'",
                    conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    new SqlCommand(
                        $"DELETE FROM Accounts_Index WHERE Code = {Convert.ToInt32(dataTable.Rows[0][0])}",
                        conn).ExecuteNonQuery();
                }

                ClearForm();
                ShowSuccess("تم الحذف بنجاح 🗑️");
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

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region ── Search ─────────────────────────────────────

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void txtNameSrch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                PerformSearch();
        }

        private void txtNameSrch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }

        private void PerformSearch()
        {
            string condition = string.Empty;
            if (!string.IsNullOrWhiteSpace(txtNameSrch.Text))
                condition = $"name LIKE N'%{txtNameSrch.Text}%' AND ";

            LoadCustomersGrid(condition);
        }

        private void dgvCustomers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgvCustomers.SelectedItem is CustomerGridRow selectedRow)
            {
                _currentCode = selectedRow.CustomerId;
                NavigateTo($"SELECT * FROM Customers WHERE id = {_currentCode}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region ── ComboBox Events ────────────────────────────

        private void cmbCountry_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;
                cmbCity.ItemsSource = null;

                if (cmbCountry.SelectedValue != null)
                    LoadCities(Convert.ToInt32(cmbCountry.SelectedValue));
            }
            catch { }
        }

        private void cmbCity_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;

                if (cmbCity.SelectedValue != null)
                    LoadAreas(Convert.ToInt32(cmbCity.SelectedValue));
            }
            catch { }
        }

        #endregion

        #region ── RadioButton Events ─────────────────────────

        private void rdClient_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try { LoadAccounts(); } catch { }
        }

        private void rdSupplier_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try { LoadAccounts(); } catch { }
        }

        private void rbOwner_CheckedChanged(object sender, RoutedEventArgs e)
        {
            LoadAccounts();
        }

        #endregion

        #region ── Add Location Events ────────────────────────

        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedId = cmbCountry.SelectedValue != null
                ? Convert.ToInt32(cmbCountry.SelectedValue) : -1;

            frmCountries countriesForm = new frmCountries();
            countriesForm.ShowDialog();
            LoadCountries();

            try { if (savedId != -1) cmbCountry.SelectedValue = savedId; } catch { }
        }

        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                ShowWarning("يجب اختيار الدولة التابع لها المدينة أولاً");
                cmbCountry.Focus();
                return;
            }

            int savedId = cmbCity.SelectedValue != null
                ? Convert.ToInt32(cmbCity.SelectedValue) : -1;
            int countryId = Convert.ToInt32(cmbCountry.SelectedValue);

            Form_WPF.frmCities citiesForm = new Form_WPF.frmCities();
            citiesForm.LoadCountries();
            citiesForm.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            citiesForm.ShowDialog();

            LoadCities(countryId);
            try { if (savedId != -1) cmbCity.SelectedValue = savedId; } catch { }
        }

        private void btnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                ShowWarning("يجب اختيار الدولة التابع لها المنطقة أولاً");
                cmbCountry.Focus();
                return;
            }

            if (cmbCity.SelectedValue == null)
            {
                ShowWarning("يجب اختيار المدينة التابع لها المنطقة أولاً");
                cmbCity.Focus();
                return;
            }

            int savedId = cmbArea.SelectedValue != null
                ? Convert.ToInt32(cmbArea.SelectedValue) : -1;
            int cityId = Convert.ToInt32(cmbCity.SelectedValue);

            Form_WPF.frmAreas areasForm = new Form_WPF.frmAreas();
            areasForm.LoadCountries();
            areasForm.cmbCountry.SelectedValue = cmbCountry.SelectedValue;
            areasForm.LoadCities(Convert.ToInt32(areasForm.cmbCountry.SelectedValue));
            areasForm.cmbCity.SelectedValue = cmbCity.SelectedValue;
            areasForm.ShowDialog();

            LoadAreas(cityId);
            try { if (savedId != -1) cmbArea.SelectedValue = savedId; } catch { }
        }

        private void btnActAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedId = cmbActs.SelectedValue != null
                ? Convert.ToInt32(cmbActs.SelectedValue) : -1;

            Form_WPF.frmActs actsForm = new Form_WPF.frmActs();
            actsForm.ShowDialog();
            LoadActs();

            try { if (savedId != -1) cmbActs.SelectedValue = savedId; } catch { }
        }

        #endregion

        #region ── Account Code ───────────────────────────────

        private void txtAccCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtAccCode.Text))
                    return;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT aname, parentcode FROM Accounts_Index " +
                    $"WHERE code = '{txtAccCode.Text}'",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    string parentCode = dataTable.Rows[0][1]?.ToString() ?? string.Empty;
                    if (parentCode == Common.CurrentBranch.CustomersAcc ||
                        parentCode == Common.CurrentBranch.SupliersAcc ||
                        parentCode == "12212")
                    {
                        txtName.Text = dataTable.Rows[0][0]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch { }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmClientPM._selectedacc = string.Empty;
                new frmSelectClient().ShowDialog();
                txtAccCode.Text = frmClientPM._selectedacc;
            }
            catch { }
        }

        #endregion

        #region ── Scan ───────────────────────────────────────

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
              //  foreach (object item in TwainHandler.ScanImages())
            //    {
            //        string imagePath = item?.ToString() ?? string.Empty;
            //        if (!string.IsNullOrEmpty(imagePath))
            //        {
            //            IDscan.Source = new BitmapImage(new Uri(imagePath));
            //        }
             //   }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء المسح الضوئي", ex);
            }
        }

        #endregion

        #region ── Trial / SavedMsg ───────────────────────────

        private bool CheckTrialLimit()
        {
            if (!MainClass.IsTrial)
                return false;

            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id FROM Customers", conn1);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count >= 10)
                {
                    ShowWarning(
                        MainClass.Language == "en"
                            ? "Sorry, You reach the maximum of entries, you can purchase the app."
                            : "نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية");
                    return true;
                }
            }
            catch { }

            return false;
        }

        private void ShowSavedDialog()
        {
            frmSavedMsg savedMsgForm = new frmSavedMsg();
            if (_currentCode != -1)
                savedMsgForm.lblSave.Text = "تم حفظ التعديلات بنجاح...";

            savedMsgForm.ShowDialog();

            if (savedMsgForm.Pressed == 1)
            {
                SqlCommand getMaxId = new SqlCommand(
                    "SELECT MAX(id) FROM Customers", conn);
                _currentCode = Convert.ToInt32(getMaxId.ExecuteScalar());
                ClearForm();
                isDone = true;
            }
            else if (savedMsgForm.Pressed == 2)
            {
                isDone = true;
            }
            else if (savedMsgForm.Pressed == 3)
            {
                isDone = true;
                Close();
            }
        }

        #endregion

        #region ── Helper Methods ─────────────────────────────

        private string GetNameById(string tableName, int id)
        {
            try
            {
                if (id <= 0)
                    return string.Empty;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT name FROM {tableName} WHERE id = {id}", conn);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? dataTable.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int SafeToInt(object value)
        {
            try { return Convert.ToInt32(value); }
            catch { return -1; }
        }

        private static double ConvertToDouble(string text)
        {
            return double.TryParse(text, out double result) ? result : 0.0;
        }

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
            string detail = ex != null ? $"{Environment.NewLine}تفاصيل: {ex.Message}" : string.Empty;
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

    #region ── Models ─────────────────────────────────────────

    public class CustomerGridRow
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string NationalId { get; set; }
        public string Tel { get; set; }
        public string Mobile { get; set; }
        public string CountryName { get; set; }
        public string CityName { get; set; }
        public string AreaName { get; set; }
    }

    #endregion
}