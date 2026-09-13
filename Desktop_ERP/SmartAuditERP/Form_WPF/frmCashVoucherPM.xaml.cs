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
    public partial class frmCashVoucherPM : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;
        private SqlConnection conn1;

        private int _currentCode;
        private int _entryCode;
        private double _exchangeRate;
        private bool _updateMainFromDollar;
        private bool _updateDollarFromMain;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCashVoucherPM()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            _currentCode = -1;
            _entryCode = -1;
            _exchangeRate = 1250.0;
            _updateMainFromDollar = true;
            _updateDollarFromMain = false;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtDate.SelectedDate = DateTime.Today;

            LoadEmployees();
            LoadAccounts();
            LoadSalesEmployees();
            LoadNextVoucherNumber();

            txtBalanceType.Text = string.Empty;
            UpdateRecordIndicator("📍 سجل جديد");
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Clear / Reset ──────────────────────────────

        private void ClearForm()
        {
            txtNo.Text = string.Empty;
            txtVal.Text = string.Empty;
            txtValD.Text = string.Empty;
            txtNotes.Text = string.Empty;
            txtBalance.Text = string.Empty;
            txtBalanceType.Text = string.Empty;
            txtCheckNo.Text = string.Empty;
            txtBankCheck.Text = string.Empty;

            txtDate.SelectedDate = DateTime.Today;
            txtCheckDate.SelectedDate = DateTime.Today;

            rdCash.IsChecked = true;

            cmbAccounts.SelectedIndex = -1;
            cmbEda3.SelectedIndex = -1;
            cmbSalesMan.SelectedIndex = -1;
            cmbCheckType.SelectedIndex = -1;

            panelCheckDetails.Visibility = Visibility.Collapsed;

            _currentCode = -1;

            LoadNextVoucherNumber();
            UpdateRecordIndicator("📍 سجل جديد");
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        private void LoadNextVoucherNumber()
        {
            try
            {
                int nextNumber = 1;
                string branchCondition = MainClass.BranchNo != -1
                    ? $"branch = {MainClass.BranchNo}"
                    : "1=1";

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT MAX(id) FROM SandQD WHERE {branchCondition}",
                    conn);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dataTable.Rows[0][0]?.ToString()))
                {
                    nextNumber = Convert.ToInt32(dataTable.Rows[0][0]) + 1;
                }

                txtNo.Text = nextNumber.ToString();
            }
            catch
            {
            }
        }

        public void LoadEmployees()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT id, name FROM Employees " +
                "JOIN EmpBranches ON Employees.id = EmpBranches.emp " +
                $"WHERE EmpBranches.branch = {MainClass.BranchNo} " +
                "AND IS_Deleted = 0 ORDER BY id",
                conn1);

            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbEmployee.ItemsSource = dataTable.DefaultView;
            cmbEmployee.DisplayMemberPath = "name";
            cmbEmployee.SelectedValuePath = "id";
            cmbEmployee.SelectedIndex = -1;
        }

        public void LoadSalesEmployees()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT id, name FROM Employees " +
                "JOIN EmpBranches ON Employees.id = EmpBranches.emp " +
                $"WHERE EmpBranches.branch = {MainClass.BranchNo} " +
                "AND IS_Deleted = 0 ORDER BY id",
                conn1);

            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbSalesMan.ItemsSource = dataTable.DefaultView;
            cmbSalesMan.DisplayMemberPath = "name";
            cmbSalesMan.SelectedValuePath = "id";
            cmbSalesMan.SelectedIndex = -1;
        }

        private void LoadAccounts()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT Code, AName FROM Accounts_Index " +
                    $"WHERE Type = 2 {Accounting.BranchCondition} " +
                    "AND ParentCode = 123",
                    conn1);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbAccounts.ItemsSource = dataTable.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch
            {
            }
        }

        public void LoadPersonsDeal()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT id, name FROM DealPersons ORDER BY id",
                conn1);

            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbEmployee.ItemsSource = dataTable.DefaultView;
            cmbEmployee.DisplayMemberPath = "name";
            cmbEmployee.SelectedValuePath = "id";
            cmbEmployee.SelectedIndex = -1;
        }

        private void LoadSearchGrid(string condition)
        {
            try
            {
                dgvSrch.ItemsSource = null;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT SandQD.id, SandQD.date, SandQD.val, Employees.name " +
                    "FROM SandQD, Employees " +
                    "WHERE SandQD.IS_Deleted = 0 " +
                    "AND SandQD.emp_person = Employees.id",
                    conn);

                if (!string.IsNullOrEmpty(condition))
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        txtFromDate.SelectedDate?.ToShortDateString();

                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value =
                        txtToDate.SelectedDate?.AddHours(24);
                }

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                var rows = new List<VoucherGridRow>();
                foreach (DataRow row in dataTable.Rows)
                {
                    rows.Add(new VoucherGridRow
                    {
                        VoucherId = Convert.ToInt32(row["id"]),
                        VoucherDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        Amount = row["val"]?.ToString() ?? string.Empty,
                        EmployeeName = row["name"]?.ToString() ?? string.Empty
                    });
                }

                dgvSrch.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل نتائج البحث", ex);
            }
        }

        #endregion

        #region ── Hidden Controls Events ─────────────────────

        private void rdPDeal_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (rdEmp?.IsChecked == true)
                {
                    LoadEmployees();
                }
                else
                {
                    LoadPersonsDeal();
                }
            }
            catch (Exception ex)
            {
                // تجاهل أو تسجيل الخطأ
            }
        }

        #endregion

        #region ── Navigation ─────────────────────────────────

        private string GetBranchCondition()
        {
            return MainClass.BranchNo != -1
                ? $"branch = {MainClass.BranchNo} AND "
                : string.Empty;
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM SandQD " +
                $"WHERE {GetBranchCondition()} IS_Deleted = 0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM SandQD " +
                $"WHERE {GetBranchCondition()} IS_Deleted = 0 " +
                $"AND id < {_currentCode} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM SandQD " +
                $"WHERE {GetBranchCondition()} IS_Deleted = 0 " +
                $"AND id > {_currentCode} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(
                $"SELECT TOP 1 * FROM SandQD " +
                $"WHERE {GetBranchCondition()} IS_Deleted = 0 ORDER BY id DESC");
        }

        public void NavigateTo(string sqlQuery)
        {
            try
            {
                dgvSrch.UnselectAll();
                EnsureConnectionOpen(conn);

                SqlCommand sqlCommand = new SqlCommand(sqlQuery, conn);
                SqlDataReader dataReader = sqlCommand.ExecuteReader();
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

        #endregion

        #region ── Read Data ──────────────────────────────────

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
                _entryCode = Convert.ToInt32(dataReader["rest_id"]);

                txtNo.Text = _currentCode.ToString();

                if (DateTime.TryParse(dataReader["date"]?.ToString(), out DateTime vDate))
                    txtDate.SelectedDate = vDate;

                cmbAccounts.SelectedValue = dataReader["acc_code"];
                txtVal.Text = dataReader["val"]?.ToString() ?? string.Empty;

                int paymentType = Convert.ToInt32(dataReader["type"]);
                if (paymentType == 1)
                {
                    rdCash.IsChecked = true;
                    panelCheckDetails.Visibility = Visibility.Collapsed;
                }
                else
                {
                    rdCheck.IsChecked = true;
                    panelCheckDetails.Visibility = Visibility.Visible;
                    txtCheckNo.Text = dataReader["check_no"]?.ToString() ?? string.Empty;
                    txtBankCheck.Text = dataReader["checkbank"]?.ToString() ?? string.Empty;

                    if (DateTime.TryParse(dataReader["check_date"]?.ToString(), out DateTime cDate))
                        txtCheckDate.SelectedDate = cDate;

                    if (dataReader["check_state"] != DBNull.Value)
                        cmbCheckType.SelectedIndex =
                            Convert.ToInt32(dataReader["check_state"]) - 1;
                }

                txtNotes.Text = dataReader["notes"]?.ToString() ?? string.Empty;
                cmbEda3.SelectedValue = dataReader["safe_bank_id"];
                cmbSalesMan.SelectedValue = dataReader["sales_emp"];

                UpdateRecordIndicator($"📍 سند رقم: {_currentCode}");
            }
            catch
            {
            }
            finally
            {
                dataReader.Close();
            }
        }

        #endregion

        #region ── Save ───────────────────────────────────────

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SqlTransaction transaction = null;

            try
            {
                if (string.IsNullOrWhiteSpace(txtVal.Text))
                {
                    ShowWarning("ادخل القيمة");
                    txtVal.Focus();
                    return;
                }

                if (cmbAccounts.SelectedIndex == -1)
                {
                    ShowWarning("يجب اختيار الحساب");
                    cmbAccounts.Focus();
                    return;
                }

                if (cmbEda3.SelectedIndex == -1)
                {
                    ShowWarning(rdCash.IsChecked == true
                        ? "يجب اختيار الخزنة"
                        : "يجب اختيار البنك");
                    cmbEda3.Focus();
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من حفظ السند؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(conn);
                transaction = conn.BeginTransaction();

                int salesEmployeeId = cmbSalesMan.SelectedIndex != -1
                    ? Convert.ToInt32(cmbSalesMan.SelectedValue)
                    : -1;

                int responsibleEmployeeId = MainClass.EmpNo;

                string branchCondition = MainClass.BranchNo != -1
                    ? $"WHERE branch = {MainClass.BranchNo}"
                    : string.Empty;

                int entryMaxId = Convert.ToInt32(
                    new SqlCommand(
                        $"SELECT ISNULL(MAX(id), 0) FROM Entry {branchCondition}",
                        conn, transaction).ExecuteScalar()) + 1;

                if (_currentCode != -1)
                {
                    entryMaxId = _entryCode;
                    new SqlCommand(
                        $"DELETE FROM Entry WHERE id = {entryMaxId}",
                        conn, transaction).ExecuteNonQuery();
                    new SqlCommand(
                        $"DELETE FROM Entry_sub WHERE res_id = {entryMaxId}",
                        conn, transaction).ExecuteNonQuery();
                }

                SqlCommand voucherCommand;
                if (_currentCode == -1)
                {
                    LoadNextVoucherNumber();
                    voucherCommand = new SqlCommand(
                        "INSERT INTO SandQD(id,date,emp_person,cust_id,acc_code,val," +
                        "type,safe_bank_id,sales_emp,check_no,check_date,checkbank," +
                        "check_state,rest_id,notes,branch,IS_Deleted) " +
                        "VALUES(@id,@date,@emp_person,@cust_id,@acc_code,@val," +
                        "@type,@safe_bank_id,@sales_emp,@check_no,@check_date,@checkbank," +
                        "@check_state,@rest_id,@notes,@branch,@IS_Deleted)",
                        conn, transaction);

                    voucherCommand.Parameters.Add("@id", SqlDbType.Int).Value =
                        Convert.ToInt32(txtNo.Text);
                }
                else
                {
                    voucherCommand = new SqlCommand(
                        "UPDATE SandQD SET date=@date, emp_person=@emp_person, " +
                        "cust_id=@cust_id, acc_code=@acc_code, val=@val, type=@type, " +
                        "safe_bank_id=@safe_bank_id, sales_emp=@sales_emp, " +
                        "check_no=@check_no, check_date=@check_date, " +
                        "checkbank=@checkbank, check_state=@check_state, " +
                        $"notes=@notes, IS_Deleted=@IS_Deleted " +
                        $"WHERE branch = {MainClass.BranchNo} AND id = {_currentCode}",
                        conn, transaction);
                }

                voucherCommand.Parameters.Add("@date", SqlDbType.DateTime).Value =
                    txtDate.SelectedDate ?? DateTime.Today;
                voucherCommand.Parameters.Add("@emp_person", SqlDbType.Int).Value =
                    responsibleEmployeeId;
                voucherCommand.Parameters.Add("@cust_id", SqlDbType.Int).Value =
                    MainClass.UserID;
                voucherCommand.Parameters.Add("@acc_code", SqlDbType.Int).Value =
                    cmbAccounts.SelectedValue;
                voucherCommand.Parameters.Add("@val", SqlDbType.Float).Value =
                    Convert.ToDouble(txtVal.Text);

                int paymentType = rdCheck.IsChecked == true ? 2 : 1;
                voucherCommand.Parameters.Add("@type", SqlDbType.Int).Value = paymentType;

                if (paymentType == 2)
                {
                    voucherCommand.Parameters.Add("@check_no", SqlDbType.NVarChar).Value =
                        txtCheckNo.Text;
                    voucherCommand.Parameters.Add("@check_date", SqlDbType.DateTime).Value =
                        txtCheckDate.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString();
                    voucherCommand.Parameters.Add("@checkbank", SqlDbType.NVarChar).Value =
                        txtBankCheck.Text;
                    voucherCommand.Parameters.Add("@check_state", SqlDbType.Int).Value =
                        cmbCheckType.SelectedIndex + 1;
                }
                else
                {
                    voucherCommand.Parameters.Add("@check_no", SqlDbType.NVarChar).Value = DBNull.Value;
                    voucherCommand.Parameters.Add("@check_date", SqlDbType.DateTime).Value = DBNull.Value;
                    voucherCommand.Parameters.Add("@checkbank", SqlDbType.NVarChar).Value = DBNull.Value;
                    voucherCommand.Parameters.Add("@check_state", SqlDbType.Int).Value = DBNull.Value;
                }

                voucherCommand.Parameters.Add("@safe_bank_id", SqlDbType.Int).Value =
                    cmbEda3.SelectedValue;
                voucherCommand.Parameters.Add("@sales_emp", SqlDbType.Int).Value =
                    salesEmployeeId;
                voucherCommand.Parameters.Add("@rest_id", SqlDbType.Int).Value =
                    entryMaxId;
                voucherCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    txtNotes.Text;
                voucherCommand.Parameters.Add("@branch", SqlDbType.Int).Value =
                    MainClass.BranchNo;
                voucherCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;

                voucherCommand.ExecuteNonQuery();

                if (_currentCode == -1)
                    _currentCode = Convert.ToInt32(txtNo.Text);

                int customerAccountCode = GetAccountCode(cmbAccounts.Text);
                int depositAccountCode = GetAccountCode(cmbEda3.Text);

                InsertJournalEntry(transaction, entryMaxId, customerAccountCode,
                    depositAccountCode);

                transaction.Commit();

                frmSavedMsg savedMsgForm = new frmSavedMsg();
                savedMsgForm.ShowDialog();

                if (savedMsgForm.Pressed == 1)
                {
                    dgvSrch.ItemsSource = null;
                    ClearForm();
                }
                else if (savedMsgForm.Pressed == 3)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private int GetAccountCode(string accountName)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT Code FROM Accounts_Index " +
                $"WHERE Type = 2 {Accounting.BranchCondition} " +
                $"AND AName = '{accountName}'",
                conn1);

            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            return dataTable.Rows.Count > 0
                ? Convert.ToInt32(dataTable.Rows[0][0])
                : 0;
        }

        private void InsertJournalEntry(
            SqlTransaction transaction,
            int entryId,
            int customerAccountCode,
            int depositAccountCode)
        {
            string voucherNote =
                $"سند قبض من عميل رقم: {_currentCode} - تحصيل من حساب: {cmbAccounts.Text}";

            SqlCommand entryCommand = new SqlCommand(
                "INSERT INTO Entry(id,date,doc_no,type,state,notes,branch,IS_Deleted) " +
                "VALUES(@id,@date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                conn, transaction);

            entryCommand.Parameters.Add("@id", SqlDbType.Int).Value = entryId;
            entryCommand.Parameters.Add("@date", SqlDbType.DateTime).Value =
                txtDate.SelectedDate ?? DateTime.Today;
            entryCommand.Parameters.Add("@doc_no", SqlDbType.Int).Value = _currentCode;
            entryCommand.Parameters.Add("@type", SqlDbType.Int).Value = 7;
            entryCommand.Parameters.Add("@state", SqlDbType.Int).Value = 1;
            entryCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = voucherNote;
            entryCommand.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
            entryCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
            entryCommand.ExecuteNonQuery();

            double amount = Convert.ToDouble(txtVal.Text);

            InsertJournalEntryLine(transaction, entryId, amount, 0,
                depositAccountCode, voucherNote);
            InsertJournalEntryLine(transaction, entryId, 0, amount,
                customerAccountCode, voucherNote);
        }

        private void InsertJournalEntryLine(
            SqlTransaction transaction,
            int entryId,
            double debit,
            double credit,
            int accountCode,
            string notes)
        {
            SqlCommand subCommand = new SqlCommand(
                "INSERT INTO Entry_sub(res_id,dept,credit,acc_no,notes,branch) " +
                "VALUES(@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                conn, transaction);

            subCommand.Parameters.Add("@res_id", SqlDbType.Int).Value = entryId;
            subCommand.Parameters.Add("@dept", SqlDbType.Float).Value = debit;
            subCommand.Parameters.Add("@credit", SqlDbType.Float).Value = credit;
            subCommand.Parameters.Add("@acc_no", SqlDbType.Int).Value = accountCode;
            subCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = notes;
            subCommand.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
            subCommand.ExecuteNonQuery();
        }

        #endregion

        #region ── Delete ─────────────────────────────────────

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    ShowWarning("اختر سنداً ليتم حذفه");
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من حذف السند؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(conn);

                new SqlCommand(
                    $"UPDATE SandQD SET IS_Deleted = 1 " +
                    $"WHERE branch = {MainClass.BranchNo} AND id = {_currentCode}",
                    conn).ExecuteNonQuery();

                new SqlCommand(
                    $"UPDATE Entry SET state = 2 " +
                    $"WHERE branch = {MainClass.BranchNo} AND id = {_entryCode}",
                    conn).ExecuteNonQuery();

                ShowSuccess("تم الحذف بنجاح 🗑️");
                dgvSrch.ItemsSource = null;
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

        #endregion

        #region ── Search ─────────────────────────────────────

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void txtSrchNo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PerformSearch();

            if (!char.IsDigit((char)KeyInterop.VirtualKeyFromKey(e.Key)) &&
                e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Left && e.Key != Key.Right)
            {
                e.Handled = true;
            }
        }

        private void PerformSearch()
        {
            string branchCondition = MainClass.BranchNo != -1
                ? $"SandQD.branch = {MainClass.BranchNo} AND "
                : string.Empty;

            string condition = string.IsNullOrWhiteSpace(txtSrchNo.Text)
                ? $"{branchCondition} date >= @date1 AND date <= @date2"
                : $"{branchCondition} SandQD.id = {txtSrchNo.Text} AND ";

            LoadSearchGrid(condition);
        }

        private void dgvSrch_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgvSrch.SelectedItem is VoucherGridRow selectedRow)
            {
                _currentCode = selectedRow.VoucherId;
                NavigateTo($"SELECT * FROM SandQD WHERE id = {_currentCode}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region ── Control Events ─────────────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
        }

        private void rdCash_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdCash?.IsChecked == true)
            {
                panelCheckDetails.Visibility = Visibility.Collapsed;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Stocks " +
                    $"WHERE branch = {MainClass.BranchNo} " +
                    "AND IS_Deleted = 0 AND status <> 2 ORDER BY id",
                    conn1);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEda3.ItemsSource = dataTable.DefaultView;
                cmbEda3.DisplayMemberPath = "name";
                cmbEda3.SelectedValuePath = "id";
                cmbEda3.SelectedIndex = -1;
            }
            else if (rdCheck?.IsChecked == true)
            {
                panelCheckDetails.Visibility = Visibility.Visible;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted = 0 ORDER BY id",
                    conn1);

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEda3.ItemsSource = dataTable.DefaultView;
                cmbEda3.DisplayMemberPath = "name";
                cmbEda3.SelectedValuePath = "id";
                cmbEda3.SelectedIndex = -1;
            }
        }

        private void cmbAccounts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                GetCustomerBalance();
            }
            catch
            {
            }
        }

        #endregion

        #region ── Currency Conversion ────────────────────────

        private void txtVal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (_updateMainFromDollar && txtVal != null &&
                    !string.IsNullOrWhiteSpace(txtVal.Text))
                {
                    double mainValue = Convert.ToDouble(txtVal.Text);
                    txtValD.Text = Math.Round(mainValue / _exchangeRate, 2).ToString("0.##");
                }
            }
            catch
            {
            }
        }

        private void txtValD_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (_updateDollarFromMain && txtValD != null &&
                    !string.IsNullOrWhiteSpace(txtValD.Text))
                {
                    double dollarValue = Convert.ToDouble(txtValD.Text);
                    txtVal.Text = Math.Round(dollarValue * _exchangeRate, 2).ToString("0.##");
                }
            }
            catch
            {
            }
        }

        private void txtVal_GotFocus(object sender, RoutedEventArgs e)
        {
            _updateMainFromDollar = true;
            _updateDollarFromMain = false;
        }

        private void txtValD_GotFocus(object sender, RoutedEventArgs e)
        {
            _updateMainFromDollar = false;
            _updateDollarFromMain = true;
        }

        #endregion

        #region ── Balance ────────────────────────────────────

        private void GetCustomerBalance()
        {
            try
            {
                if (cmbAccounts.SelectedIndex == -1)
                    return;

                SqlDataAdapter codeAdapter = new SqlDataAdapter(
                    "SELECT Code FROM Accounts_Index " +
                    $"WHERE Type = 2 AND AName = '{cmbAccounts.Text}'",
                    conn);

                DataTable codeTable = new DataTable();
                codeAdapter.Fill(codeTable);

                if (codeTable.Rows.Count == 0)
                    return;

                int accountCode = Convert.ToInt32(codeTable.Rows[0][0]);

                string branchFilter = MainClass.BranchNo != -1
                    ? $"Entry.branch = {MainClass.BranchNo} AND " +
                      $"Entry_sub.branch = {MainClass.BranchNo} AND "
                    : string.Empty;

                SqlDataAdapter balanceAdapter = new SqlDataAdapter(
                    "SELECT SUM(Entry_sub.dept) AS dept, " +
                    "SUM(Entry_sub.credit) AS credit " +
                    "FROM Entry, Entry_sub " +
                    $"WHERE {branchFilter} " +
                    "Entry.IS_Deleted = 0 AND Entry.state = 1 " +
                    "AND Entry.date <= @date2 " +
                    "AND Entry.id = Entry_sub.res_id " +
                    $"AND Entry_sub.acc_no = {accountCode}",
                    conn);

                balanceAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    (txtDate.SelectedDate ?? DateTime.Today).AddHours(24);

                DataTable balanceTable = new DataTable();
                balanceAdapter.Fill(balanceTable);

                double totalDebit = 0;
                double totalCredit = 0;

                if (balanceTable.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(balanceTable.Rows[0][0]?.ToString()))
                {
                    totalDebit = Convert.ToDouble(balanceTable.Rows[0]["dept"]);
                    totalCredit = Convert.ToDouble(balanceTable.Rows[0]["credit"]);
                }

                if (totalDebit >= totalCredit)
                {
                    txtBalance.Text = Math.Round(totalDebit - totalCredit, 3).ToString();
                    txtBalanceType.Text = "مدين";
                }
                else
                {
                    txtBalance.Text = Math.Round(totalCredit - totalDebit, 3).ToString();
                    txtBalanceType.Text = "دائن";
                }
            }
            catch
            {
            }
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
                ? $"{Environment.NewLine}تفاصيل الخطأ: {ex.Message}"
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

    #region ── Grid Row Model ─────────────────────────────────

    public class VoucherGridRow
    {
        public int VoucherId { get; set; }
        public string VoucherDate { get; set; }
        public string Amount { get; set; }
        public string EmployeeName { get; set; }
    }

    #endregion
}