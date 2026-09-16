using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEmpSalaryAddSub : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private int _currentCode;
        private string _entryGlobalId;
        private string _globalId;
        private int _receiptType;

        public string SrchName;
        public int resNo;
        public bool IsNew;
        public resvdPrint resv;

        private Print _print;

        #endregion

        #region Constructor

        public frmEmpSalaryAddSub()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _currentCode = -1;
            _entryGlobalId = "-1";
            _globalId = "-1";
            _receiptType = 28;
            SrchName = string.Empty;
            IsNew = true;
            resv = new resvdPrint();
            _print = new Print(10);

            Loaded += FrmEmpSalaryAddSub_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmEmpSalaryAddSub_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.DateTime = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime = DateTime.Now;
            txtTime.Text = DateTime.Now.ToString("HH:mm");

            LoadEmployees();
            LoadTypes();

            WireUpEvents();
            LoadNextNumber();
        }

        private void WireUpEvents()
        {
            // Toolbar
            IconButton7.Click += (s, e) => Close();
            IconButton4.Click += (s, e) => ClearForm();
            IconButton6.Click += IconButton6_Click;
            IconButton9.Click += IconButton9_Click;
            IconButton8.Click += (s, e) => RptPrint(1);
            btnView.Click += (s, e) => RptPrint(2);

            // Navigation
            IconButton1.Click += (s, e) => Navigate(
                "SELECT TOP 1 * FROM EmpSalaryAddSub WHERE IS_Deleted=0 ORDER BY id ASC");
            IconButton3.Click += (s, e) => Navigate(
                $"SELECT TOP 1 * FROM EmpSalaryAddSub WHERE IS_Deleted=0 AND id<{_currentCode} ORDER BY id DESC");
            IconButton2.Click += (s, e) => Navigate(
                $"SELECT TOP 1 * FROM EmpSalaryAddSub WHERE IS_Deleted=0 AND id>{_currentCode} ORDER BY id ASC");
            IconButton5.Click += (s, e) => Navigate(
                "SELECT TOP 1 * FROM EmpSalaryAddSub WHERE IS_Deleted=0 ORDER BY id DESC");

            // Payment Type
            rdCash.Checked += RdPaymentType_Changed;
            rdCheck.Checked += RdPaymentType_Changed;

            // Employee
            cmbEmp.EditValueChanged += CmbEmp_EditValueChanged;

            // Type
            cmbType.EditValueChanged += CmbType_EditValueChanged;

            // Search
            btnSearch.Click += BtnSearch_Click;

            chkAllEmp.Checked += ChkAllEmp_Changed;
            chkAllEmp.Unchecked += ChkAllEmp_Changed;
        }

        #endregion

        #region Clear Form

        private void ClearForm()
        {
            _currentCode = -1;
            IsNew = true;
            _entryGlobalId = "-1";
            _globalId = "-1";

            cmbEmp.EditValue = null;
            cmbType.EditValue = null;
            cmbDepositTo.EditValue = null;
            txtVal.Text = string.Empty;
            txtNotes.Text = string.Empty;
            txtAccCode.Text = string.Empty;
            txtEntryGlobalId.Text = string.Empty;
            TxtGlobalID.Text = string.Empty;

            txtDate.DateTime = DateTime.Now;
            txtTime.Text = DateTime.Now.ToString("HH:mm");

            rdCash.IsChecked = true;
            ChkSubSalary.IsChecked = false;

            LoadNextNumber();
        }

        #endregion

        #region Load Data

        public void LoadEmployees()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE is_deleted=0 ORDER BY id",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbEmp.ItemsSource = dataTable.DefaultView;
                cmbEmp.DisplayMember = "name";
                cmbEmp.ValueMember = "id";
                cmbEmp.EditValue = null;

                // نسخة للبحث
                cmbEmpSrch.ItemsSource = dataTable.DefaultView;
                cmbEmpSrch.DisplayMember = "name";
                cmbEmpSrch.ValueMember = "id";
                cmbEmpSrch.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadTypes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM SalaryAddSubTypes ORDER BY id",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbType.ItemsSource = dataTable.DefaultView;
                cmbType.DisplayMember = "name";
                cmbType.ValueMember = "id";
                cmbType.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadDepositSources(bool isCash)
        {
            try
            {
                string query;

                if (isCash)
                {
                    query =
                        $"SELECT id, name FROM Stocks " +
                        $"INNER JOIN Stock_Emps ON Stocks.id = Stock_Emps.stock_id " +
                        $"WHERE emp_id={MainClass.EmpNo} " +
                        $"AND IS_Deleted=0 AND status<>2 ORDER BY id";

                    var adapter = new SqlDataAdapter(query, _conn);
                    var table = new DataTable();
                    adapter.Fill(table);

                    cmbDepositTo.ItemsSource = table.DefaultView;
                    cmbDepositTo.DisplayMember = "name";
                    cmbDepositTo.ValueMember = "id";
                }
                else
                {
                    query = "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id";

                    var adapter = new SqlDataAdapter(query, _conn);
                    var table = new DataTable();
                    adapter.Fill(table);

                    cmbDepositTo.ItemsSource = table.DefaultView;
                    cmbDepositTo.DisplayMember = "name";
                    cmbDepositTo.ValueMember = "id";
                }

                cmbDepositTo.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadNextNumber()
        {
            try
            {
                if (!IsNew) return;

                int nextNo = 1;

                var adapter = new SqlDataAdapter(
                    "SELECT ISNULL(MAX(id),0) AS id FROM EmpSalaryAddSub",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dataTable.Rows[0][0].ToString()))
                {
                    nextNo = Convert.ToInt32(dataTable.Rows[0][0]) + 1;
                }

                txtNo.Text = nextNo.ToString();
                resNo = nextNo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Load Grid (Search)

        private void LoadGrid(string condition)
        {
            try
            {
                string query =
                    "SELECT e.id, e.emp, emp2.name, e.date, e.val, s.name AS type " +
                    "FROM EmpSalaryAddSub e " +
                    "INNER JOIN SalaryAddSubTypes s ON e.type = s.id " +
                    "INNER JOIN Employees emp2 ON e.emp = emp2.id " +
                    $"WHERE {condition} e.IS_Deleted=0 ORDER BY e.id";

                var adapter = new SqlDataAdapter(query, _conn);

                if (!string.IsNullOrEmpty(condition) &&
                    condition.Contains("date>="))
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        txtFromDate.DateTime.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value =
                        txtToDate.DateTime.ToShortDateString();
                }

                var rawData = new DataTable();
                adapter.Fill(rawData);

                var display = new DataTable();
                display.Columns.Add("RecordId", typeof(int));
                display.Columns.Add("EmpId", typeof(int));
                display.Columns.Add("EmpName", typeof(string));
                display.Columns.Add("Value", typeof(object));
                display.Columns.Add("TypeName", typeof(string));
                display.Columns.Add("RecordDate", typeof(string));

                foreach (DataRow row in rawData.Rows)
                {
                    display.Rows.Add(
                        row["id"],
                        row["emp"],
                        row["name"],
                        row["val"],
                        row["type"],
                        Convert.ToDateTime(row["date"]).ToShortDateString());
                }

                dgvSrch.ItemsSource = display.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Navigate & Read Data

        public void Navigate(string sql)
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                using var cmd = new SqlCommand(sql, _conn);
                using var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            try
            {
                if (!reader.HasRows) return;

                reader.Read();
                ClearForm();

                _currentCode = Convert.ToInt32(reader["id"]);
                txtNo.Text = _currentCode.ToString();

                cmbEmp.EditValue = reader["emp"];
                cmbType.EditValue = reader["type"];

                txtVal.Text = reader["val"].ToString();
                txtNotes.Text = reader["notes"].ToString();
                ChkSubSalary.IsChecked = Convert.ToBoolean(reader["SubFromSalary"]);
                txtEntryGlobalId.Text = reader["EntryGlobalId"].ToString();

                // Payment type
                bool isCash = reader["Cash"] != DBNull.Value &&
                               Convert.ToInt32(reader["Cash"]) > 0;
                bool isBank = reader["Bank"] != DBNull.Value &&
                               Convert.ToInt32(reader["Bank"]) > 0;

                if (isCash)
                {
                    rdCash.IsChecked = true;
                    LoadDepositSources(isCash: true);
                }
                else if (isBank)
                {
                    rdCheck.IsChecked = true;
                    LoadDepositSources(isCash: false);
                }

                GetEmployeeCode(Convert.ToInt32(reader["emp"]));

                try { txtDate.DateTime = Convert.ToDateTime(reader["date"]); }
                catch { /* ignore */ }

                TabControl1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Save

        private void IconButton6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                GetAccountCodeInternal(cmbEmp.Text);
                int depositAcc = GetAccountCodeInternal(cmbDepositTo.Text);

                PrepareEntryIdentifiers();

                var receipt = CreateReceiptObject(depositAcc);
                var entry = BindReceiptToEntry(
                    receipt,
                    cmbType.EditValue != null
                        ? Convert.ToInt32(cmbType.EditValue) : 0,
                    ChkSubSalary.IsChecked == true);

                if (SaveReceipt(receipt, entry, IsNew))
                {
                    ClearForm();
                    LoadGrid(string.Empty);
                    UpdateCodeFromDatabase();

                    DXMessageBox.Show(
                        GetMessage("✅ تم الحفظ", "✅ Saved successfully"),
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HandleError("خطأ أثناء الحفظ", "Error saving", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Delete

        private void IconButton9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    ShowWarning("اختر سجلاً ليتم حذفه");
                    return;
                }

                var result = DXMessageBox.Show(
                    "⚠️ هل أنت متأكد من الحذف؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                new SqlCommand(
                    $"UPDATE EmpSalaryAddSub SET IS_Deleted=1 WHERE id={_currentCode}",
                    _conn).ExecuteNonQuery();

                LoadGrid(string.Empty);
                ClearForm();

                DXMessageBox.Show("✅ تم الحذف", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                HandleError("خطأ أثناء الحذف", "Error deleting", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Search

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllEmp.IsChecked != true && cmbEmpSrch.EditValue == null)
            {
                ShowWarning("يجب اختيار موظف");
                cmbEmpSrch.Focus();
                return;
            }

            Search();
        }

        private void Search()
        {
            string condition = string.Empty;

            if (chkAllEmp.IsChecked != true && cmbEmpSrch.EditValue != null)
                condition = $" emp={cmbEmpSrch.EditValue} AND ";

            LoadGrid(condition + "date>=@date1 AND date<=@date2 AND ");
        }

        #endregion

        #region Grid Events

        private void DgvSrch_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is DataRowView rowView)
            {
                int selectedId = Convert.ToInt32(rowView["RecordId"]);
                Navigate(
                    $"SELECT * FROM EmpSalaryAddSub WHERE id={selectedId}");
            }
        }

        #endregion

        #region ComboBox Events

        private void CmbEmp_EditValueChanged(
            object sender, EditValueChangedEventArgs e)
        {
            if (cmbEmp.EditValue != null)
                GetEmployeeCode(Convert.ToInt32(cmbEmp.EditValue));
        }

        private void CmbType_EditValueChanged(
            object sender, EditValueChangedEventArgs e)
        {
            if (cmbType.EditValue == null) return;

            int typeId = Convert.ToInt32(cmbType.EditValue);

            ChkSubSalary.Content = typeId switch
            {
                1 => "✅ تضاف على الراتب",
                2 => "✂️ تخصم من الراتب",
                3 => "✂️ تخصم من الراتب",
                _ => "✂️ تخصم من الراتب"
            };
        }

        private void ChkAllEmp_Changed(object sender, RoutedEventArgs e)
        {
            cmbEmpSrch.IsEnabled = chkAllEmp.IsChecked != true;
        }

        private void RdPaymentType_Changed(object sender, RoutedEventArgs e)
        {
            LoadDepositSources(rdCash.IsChecked == true);
        }

        #endregion

        #region Validation & Helpers

        private bool ValidateInputs()
        {
            if (cmbEmp.EditValue == null)
            {
                ShowWarning(GetMessage("يجب اختيار موظف",
                    "Please select an employee"));
                cmbEmp.Focus();
                return false;
            }

            if (cmbType.EditValue == null)
            {
                ShowWarning(GetMessage("يجب اختيار نوع الإجراء",
                    "Please select action type"));
                cmbType.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                ShowWarning(GetMessage("يجب إدخال مبلغ",
                    "Please enter an amount"));
                txtVal.Focus();
                return false;
            }

            if (cmbDepositTo.EditValue == null)
            {
                ShowWarning(GetMessage("يجب اختيار الصندوق أو البنك",
                    "Please select cash or bank"));
                cmbDepositTo.Focus();
                return false;
            }

            return true;
        }

        private int GetAccountCodeInternal(string accountName)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "SELECT Code FROM Accounts_Index WHERE Type=2 " +
                    $"{Accounting.BranchCondition} AND AName=@AccountName",
                    _conn);
                adapter.SelectCommand.Parameters.AddWithValue(
                    "@AccountName", accountName);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0
                    ? Convert.ToInt32(dataTable.Rows[0][0]) : 0;
            }
            catch
            {
                return 0;
            }
        }

        private void GetEmployeeCode(int empId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AccCode FROM Employees WHERE Is_deleted=0 AND id={empId}",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    txtAccCode.Text = dataTable.Rows[0][0].ToString();
            }
            catch { /* ignore */ }
        }

        private void PrepareEntryIdentifiers()
        {
            if (string.IsNullOrWhiteSpace(txtEntryGlobalId.Text) ||
                txtEntryGlobalId.Text == "-1")
            {
                IsNew = true;
                int entryNo = 28;
                EntryOper.GetEntryGlobalID(ref _entryGlobalId, ref entryNo);
                _globalId = $"{MainClass.BranchNo}-{txtNo.Text}";
                txtEntryGlobalId.Text = _entryGlobalId;
                resNo = _currentCode;
            }
            else
            {
                IsNew = false;
                _entryGlobalId = txtEntryGlobalId.Text;
                _globalId = TxtGlobalID.Text;
                resNo = Convert.ToInt32(txtNo.Text);
            }
        }

        private AddSubEmployee CreateReceiptObject(int depositAcc)
        {
            string employeeName = cmbEmp.Text;
            int typeId = cmbType.EditValue != null
                ? Convert.ToInt32(cmbType.EditValue)
                : 0;

            string notes = string.IsNullOrWhiteSpace(txtNotes.Text)
                ? typeId switch
                {
                    1 => $"مكافأة للموظف {employeeName}",
                    2 => $"خصم للموظف {employeeName}",
                    3 => $"سلفة للموظف {employeeName}",
                    _ => string.Empty
                }
                : txtNotes.Text;

            DateTime receiptDate = txtDate.DateTime.Date;

            if (TimeSpan.TryParse(txtTime.Text, out TimeSpan selectedTime))
            {
                receiptDate = receiptDate.Add(selectedTime);
            }

            return new AddSubEmployee
            {
                GlobalID = _globalId,
                _id = resNo,
                BranchID = MainClass.BranchNo,
                EntryGlobalID = _entryGlobalId,
                ReceiptType = 28,
                State = true,
                _Net = Convert.ToDecimal(txtVal.Text),
                ReceiptDate = receiptDate,
                ISDeleted = false,
                Notes = notes,
                _name = employeeName,
                CreditAcc = depositAcc.ToString(),
                DebitAcc = txtAccCode.Text
            };
        }

        public Entry BindReceiptToEntry(
            AddSubEmployee receipt, int type, bool subFromSalary)
        {
            try
            {
                var accounts = new List<Account>();
                double amount = Convert.ToDouble(txtVal.Text);

                var entry = new Entry
                {
                    EntryGlobalID = receipt.EntryGlobalID,
                    ClientCode = Sync.ClientCode,
                    EntryNo = Convert.ToInt32(
                        receipt.EntryGlobalID.Substring(
                            receipt.EntryGlobalID.LastIndexOf('-') + 1)),
                    EntryDate = receipt.ReceiptDate,
                    ReffNo = receipt._id.ToString(),
                    RefDate = receipt.ReceiptDate,
                    Type = (EntryType)receipt.ReceiptType,
                    State = 1,
                    Note = receipt.Notes,
                    Branch = receipt.BranchID,
                    EmpID = MainClass.EmpNo,
                    DistBranch = Sync.DistBranch,
                    BranchType = Sync.BranchType
                };

                if (receipt._Net > 0)
                {
                    if (type == 1)
                    {
                        string name = subFromSalary
                            ? "راتب أساسي" : cmbDepositTo.Text;
                        string value = subFromSalary
                            ? "3122001" : receipt.CreditAcc;

                        accounts.Add(CreateAccountItem(
                            entry.EntryGlobalID, entry.EntryNo.ToString(),
                            name, Convert.ToInt32(value),
                            amount, 0.0, receipt.Notes));

                        accounts.Add(CreateAccountItem(
                            entry.EntryGlobalID, entry.EntryNo.ToString(),
                            cmbEmp.Text,
                            Convert.ToInt32(receipt.DebitAcc),
                            0.0, amount, receipt.Notes));
                    }
                    else
                    {
                        accounts.Add(CreateAccountItem(
                            entry.EntryGlobalID, entry.EntryNo.ToString(),
                            cmbDepositTo.Text,
                            Convert.ToInt32(receipt.CreditAcc),
                            0.0, amount, receipt.Notes));

                        accounts.Add(CreateAccountItem(
                            entry.EntryGlobalID, entry.EntryNo.ToString(),
                            cmbEmp.Text,
                            Convert.ToInt32(receipt.DebitAcc),
                            amount, 0.0, receipt.Notes));
                    }
                }

                entry.Accounts = accounts;
                return entry;
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في ربط البيانات:\n{ex.Message}");
                return null;
            }
        }

        private Account CreateAccountItem(
            string globalId, string entryNo, string name,
            int code, double debt, double credit, string note)
        {
            return new Account
            {
                EntryGlobalID = globalId,
                EntryNo = Convert.ToInt32(entryNo),
                Name = name,
                Code = code.ToString(),
                Debt = debt,
                Credit = credit,
                Note = note,
                CCcode = "-1"
            };
        }

        public bool SaveReceipt(
            AddSubEmployee receipt, Entry entry, bool isNew)
        {
            var conn = MainClass.ConnObj();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using var cmd = CreateSaveCommand(conn);
                PopulateParameters(cmd, receipt);
                cmd.ExecuteNonQuery();

                if (entry != null && !new EntryOper().SaveEnty(entry))
                {
                    ShowError(GetMessage(
                        "خطأ أثناء الحفظ", "Error in saving"));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                HandleError("خطأ أثناء حفظ القيد", "Error saving entry", ex);
                return false;
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private SqlCommand CreateSaveCommand(SqlConnection conn)
        {
            string sql = _currentCode != -1
                ? $"UPDATE EmpSalaryAddSub SET " +
                  $"emp=@emp, type=@type, date=@date, val=@val, " +
                  $"notes=@notes, user_id=@user_id, IS_Deleted=@IS_Deleted, " +
                  $"SubFromSalary=@SubFromSalary, EntryGlobalId=@EntryGlobalId, " +
                  $"Cash=@Cash, Bank=@Bank WHERE id={_currentCode}"
                : "INSERT INTO EmpSalaryAddSub" +
                  "(emp,type,date,val,notes,user_id,IS_Deleted," +
                  "SubFromSalary,EntryGlobalId,Cash,Bank) " +
                  "VALUES(@emp,@type,@date,@val,@notes,@user_id,@IS_Deleted," +
                  "@SubFromSalary,@EntryGlobalId,@Cash,@Bank)";

            return new SqlCommand(sql, conn);
        }

        private void PopulateParameters(
            SqlCommand cmd, AddSubEmployee receipt)
        {
            cmd.Parameters.Add("@emp", SqlDbType.Int).Value
                = cmbEmp.EditValue;
            cmd.Parameters.Add("@type", SqlDbType.Int).Value
                = cmbType.EditValue;
            cmd.Parameters.Add("@date", SqlDbType.DateTime).Value
                = txtDate.DateTime.ToShortDateString();
            cmd.Parameters.Add("@val", SqlDbType.Float).Value
                = Convert.ToDouble(txtVal.Text);
            cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value
                = txtNotes.Text;
            cmd.Parameters.Add("@user_id", SqlDbType.Int).Value
                = MainClass.UserID;
            cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
            cmd.Parameters.Add("@SubFromSalary", SqlDbType.Bit).Value
                = ChkSubSalary.IsChecked == true ? 1 : 0;
            cmd.Parameters.Add("@EntryGlobalId", SqlDbType.NVarChar).Value
                = txtEntryGlobalId.Text;

            if (rdCash.IsChecked == true)
            {
                cmd.Parameters.Add("@Cash", SqlDbType.Int).Value
                    = cmbDepositTo.EditValue ?? 0;
                cmd.Parameters.Add("@Bank", SqlDbType.Int).Value = 0;
            }
            else
            {
                cmd.Parameters.Add("@Cash", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@Bank", SqlDbType.Int).Value
                    = cmbDepositTo.EditValue ?? 0;
            }
        }

        private void UpdateCodeFromDatabase()
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                using var cmd = new SqlCommand(
                    "SELECT MAX(id) FROM EmpSalaryAddSub", _conn);
                _currentCode = Convert.ToInt32(cmd.ExecuteScalar());
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Print

        public void RptPrint(int type)
        {
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                ShowWarning("لا توجد قيمة بالسند");
                return;
            }

            if (string.IsNullOrWhiteSpace(_print.RptUrl))
            {
                ShowWarning("يجب تحديد مسار التقرير");
                return;
            }

            _print.RptName = "rptsandBond.repx";

            string fullPath = Path.Combine(_print.RptUrl, _print.RptName);

            if (!Directory.Exists(_print.RptUrl) || !File.Exists(fullPath))
            {
                ShowError("المسار الحالي للتقارير غير موجود أو تم تعديله");
                return;
            }

            if (string.IsNullOrEmpty(_print.defPrinter))
                _print.defPrinter = MainClass.ReportsPrinter;

            _print.Printing(type, BindToData(),
                _print.RptUrl, _print.RptName,
                _print.defPrinter, _print.kitchenprinter, _print.PrintNo);
        }

        private DataSet BindToData()
        {
            var adapter = new SqlDataAdapter(
                "SELECT * FROM Foundation", _conn);
            var foundation = new DataTable();
            adapter.Fill(foundation);

            string address = string.Empty;
            string telephone = string.Empty;
            string mobile = string.Empty;
            string foundName = string.Empty;
            string field = string.Empty;
            string vatNo = string.Empty;

            if (foundation.Rows.Count > 0)
            {
                var row = foundation.Rows[0];
                address = row["Address"].ToString();
                telephone = row["Tel"].ToString();
                mobile = row["Mobile"].ToString();
                foundName = row["nameA"].ToString();
                field = row["FieldA"].ToString();
                vatNo = row["tax_no"].ToString();
            }

            string bondType = rdCash.IsChecked == true
                ? "نقدي" : "بنكي";

            var bond = new Bond
            {
                BondName = Title,
                PayToCode = txtAccCode.Text,
                PayTo = cmbEmp.Text,
                Payfrom = cmbDepositTo.Text,
                supplier = cmbEmp.Text,
                BondDate = txtDate.DateTime.ToShortDateString(),
                BondNo = txtNo.Text,
                PrintDate = DateTime.Now.ToShortDateString(),
                Note = txtNotes.Text,
                BondVal = txtVal.Text,
                AccountStatus = string.Empty,
                CurrentBalance = string.Empty,
                BondType = bondType,
                User = Common.GetEmpName(MainClass.EmpNo),
                ArabicLetter = InvoiceOper.ToArabicLetter(
                    Convert.ToDouble(txtVal.Text)),
                Address = address,
                Mobile = mobile,
                Telephone = telephone,
                VATNo = vatNo,
                Foundation = foundName,
                Field = field,
                Logo = string.Empty,
                Header = string.Empty,
                Footer = string.Empty,
                Stamp = string.Empty
            };

            var list = new List<Bond> { bond };
            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        #endregion

        #region Utilities

        private string GetMessage(string arabic, string english)
        {
            return MainClass.Language == "ar" ? arabic : english;
        }

        private void HandleError(
            string arabic, string english, Exception ex)
        {
            ShowError(
                $"{GetMessage(arabic, english)}\n" +
                $"{(MainClass.Language == "ar" ? "تفاصيل الخطأ: " : "Error details: ")}" +
                $"{ex.Message}");
        }

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowWarning(string message)
        {
            DXMessageBox.Show(message, "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion
    }
}