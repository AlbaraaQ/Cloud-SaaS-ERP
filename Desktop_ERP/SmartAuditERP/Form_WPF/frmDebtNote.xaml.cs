using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDebtNote : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private int _currentCode;
        private double _invoiceDiscount;
        private int _bondId;
        private int _receiptCode;
        private string _searchName;
        private string _entryGlobalId;

        private InvoiceObj _invoiceObj;

        private bool _printHeader;
        private bool _printFooter;
        private bool _printStamp;
        private int _printType;
        private int _printNo;
        private string _defaultPrinter;
        private string _reportName;
        private string _reportUrl;
        private string _invCombinedId;

        #endregion

        #region Constructor

        public frmDebtNote()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _currentCode = -1;
            _invoiceDiscount = 0.0;
            _receiptCode = -1;
            _searchName = string.Empty;
            _entryGlobalId = "-1";
            _invoiceObj = new InvoiceObj(2, 1);
            _printHeader = true;
            _printFooter = true;
            _printStamp = true;
            _printNo = 1;
            _reportName = string.Empty;
            _reportUrl = string.Empty;
            _invCombinedId = string.Empty;

            Loaded += FrmDebtNote_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDebtNote_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.DateTime = DateTime.Now;

            LoadClients();
            LoadNextNumber();
            LoadPrintSettings();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnClose.Click += (s, e) => Close();
            btnNew.Click += BtnNew_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnFirst.Click += BtnFirst_Click;
            btnPrevious.Click += BtnPrevious_Click;
            btnNext.Click += BtnNext_Click;
            btnLast.Click += BtnLast_Click;
            btnPrint.Click += (s, e) => PrintDevexpress(1);
            btnView.Click += (s, e) => PrintDevexpress(2);
            btnSearch.Click += BtnSearch_Click;

            rdInvNote.Checked += RdNoteType_Changed;
            rdMainNote.Checked += RdNoteType_Changed;

            txtDiscountVal.LostFocus += TxtDiscountVal_LostFocus;
            txtDiscountPerc.LostFocus += TxtDiscountPerc_LostFocus;
            txtDiscountVal.KeyDown += TxtDiscountVal_KeyDown;
            txtDiscountPerc.KeyDown += TxtDiscountPerc_KeyDown;
            txtInvNo.KeyDown += TxtInvNo_KeyDown;

            cmbClients.EditValueChanged += CmbClients_EditValueChanged;
        }

        #endregion

        #region Load Data

        public void LoadClients()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Customers " +
                "WHERE (type=1 OR type=3) AND IS_Deleted=0 ORDER BY id",
                _conn);

            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            cmbClients.ItemsSource = dataTable.DefaultView;
            cmbClients.DisplayMember = "name";
            cmbClients.ValueMember = "id";
            cmbClients.EditValue = null;
        }

        private void LoadNextNumber()
        {
            try
            {
                int nextNo = 1;

                var adapter = new SqlDataAdapter(
                    "SELECT MAX(Doc_No) FROM CreditDeptNotes WHERE Doc_Type=2",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(table.Rows[0][0].ToString()))
                {
                    nextNo = Convert.ToInt32(
                        Convert.ToDouble(table.Rows[0][0].ToString())) + 1;
                }

                txtNo.Text = nextNo.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=11", _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 1) return;

                DataRow row = table.Rows[0];
                _printType = Convert.ToInt32(row["printType"]);
                _printFooter = Convert.ToBoolean(row["PrintFooter"]);
                _printHeader = Convert.ToBoolean(row["PrintHeader"]);
                _printStamp = Convert.ToBoolean(row["PrintStamp"]);
                _defaultPrinter = row["CasherPrinter"].ToString();
                _printNo = Convert.ToInt32(row["printNo"]);

                if (string.IsNullOrEmpty(_defaultPrinter))
                    _defaultPrinter = Common.GetDefaultPrinter();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Clear Form

        private void ClearForm()
        {
            _currentCode = -1;

            cmbClients.EditValue = null;
            txtInvVal.EditValue = "0";
            txtDiscountPerc.EditValue = "0";
            txtDiscountVal.EditValue = "0";
            txtPreviousDiscVal.EditValue = "0";
            txtNet.EditValue = "0";
            txtTax.EditValue = "0";
            txtNotes.EditValue = null;

            LoadNextNumber();
            UpdateStatusBar("سند جديد");
        }

        #endregion

        #region Navigation

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
            if (!reader.HasRows) return;

            reader.Read();
            ClearForm();

            _currentCode = Convert.ToInt32(reader["Doc_No"]);

            txtNo.Text = _currentCode.ToString();

            txtDate.DateTime = Convert.ToDateTime(reader["Date"]);

            _entryGlobalId = EntryOper.ReadEntryGlobalID(
                _currentCode.ToString(), 17, MainClass.BranchNo);

            // تحديد نوع الإشعار
            double invNo = Convert.ToDouble(reader["Inv_No"]);

            if (invNo != -1)
            {
                rdInvNote.IsChecked = true;
                txtInvNo.EditValue = reader["Inv_No"].ToString();
                LoadInvoiceData(Convert.ToInt32(invNo));

                // الخصومات السابقة
                var prevAdapter = new SqlDataAdapter(
                    $"SELECT SUM(Discount) AS sum FROM CreditDeptNotes " +
                    $"WHERE Doc_Type=2 AND IS_Deleted=0 " +
                    $"AND Inv_No={invNo} AND Doc_No<{_currentCode}",
                    _conn1);
                var prevTable = new DataTable();
                prevAdapter.Fill(prevTable);

                txtPreviousDiscVal.EditValue =
                    prevTable.Rows.Count > 0 &&
                    prevTable.Rows[0]["sum"] != DBNull.Value
                        ? Convert.ToDouble(prevTable.Rows[0]["sum"]).ToString()
                        : "0";
            }
            else
            {
                rdMainNote.IsChecked = true;
            }

            // العميل والبيان
            try { cmbClients.EditValue = reader["cust_id"]; }
            catch { /* ignore */ }

            txtDiscountVal.EditValue = reader["Discount"].ToString();
            txtNotes.EditValue = reader["notes"].ToString();

            CalcTotals();
            CalcDiscountPercent();

            UpdateStatusBar($"سند رقم: {_currentCode}");
        }

        #endregion

        #region Calculations

        public void CalcDiscount()
        {
            double invVal = SafeDouble(txtInvVal.EditValue);
            double prevDisc = SafeDouble(txtPreviousDiscVal.EditValue);
            double discountVal = SafeDouble(txtDiscountVal.EditValue);
            double netBase = invVal - prevDisc;

            if (netBase == 0) return;

            txtDiscountPerc.EditValue =
                (discountVal / netBase * 100.0).ToString(_invoiceObj.DigitsNo);

            CalcTotals();
        }

        private void CalcDiscountPercent()
        {
            double invVal = SafeDouble(txtInvVal.EditValue);
            double prevDisc = SafeDouble(txtPreviousDiscVal.EditValue);
            double discVal = SafeDouble(txtDiscountVal.EditValue);
            double netBase = invVal - prevDisc;

            if (netBase == 0) return;

            txtDiscountPerc.EditValue =
                (discVal / netBase * 100.0).ToString(_invoiceObj.DigitsNo);
        }

        private void CalcTotals()
        {
            double invVal = SafeDouble(txtInvVal.EditValue);
            double prevDisc = SafeDouble(txtPreviousDiscVal.EditValue);
            double discountVal = SafeDouble(txtDiscountVal.EditValue);
            double netBase = invVal - prevDisc;

            if (discountVal == 0)
            {
                txtDiscountPerc.EditValue = "0";
                txtNet.EditValue = "0";
                txtTax.EditValue = "0";
                return;
            }

            if (discountVal > netBase)
            {
                DXMessageBox.Show("قيمة الخصم أكبر من قيمة الفاتورة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double net = netBase - discountVal;
            double vat = CalcVAT(discountVal);

            txtNet.EditValue = net.ToString(_invoiceObj.DigitsNo);
            txtTax.EditValue = vat.ToString(_invoiceObj.DigitsNo);
        }

        private double CalcVAT(double value)
        {
            return value - Math.Round(
                value / (1.0 + _invoiceObj.VAT / 100.0), 3);
        }

        private double SafeDouble(object value)
        {
            return double.TryParse(value?.ToString(), out double result)
                ? result : 0.0;
        }

        #endregion

        #region Search & Lookup

        private void InvoiceSearch()
        {
            try
            {
                var searchForm = new frmInvoiceSrch();
                searchForm.cmbProcType.IsEnabled = false;
                MainClass.ApplyPermissionToForm(searchForm);
                MainClass.DoApplyUserSett(searchForm);

                searchForm.ProcType = 1;
                searchForm.InvType = 2;
                searchForm.btnPostPone.Visibility = Visibility.Collapsed;
                searchForm.btnPostPone.IsChecked = true;
                searchForm.ShowDialog();

                if (!searchForm.ISDone ||
                    searchForm.InvGlobalID == "-1")
                    return;

                var adapter = new SqlDataAdapter(
                    $"SELECT cust_id, id, tot_net FROM Inv " +
                    $"WHERE inv_type=2 AND IS_Deleted=0 " +
                    $"AND InvGlobalID=N'{searchForm.InvGlobalID}' " +
                    $"AND proc_type=1",
                    _conn);

                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    cmbClients.EditValue = table.Rows[0]["cust_id"];
                    txtInvNo.EditValue = table.Rows[0]["id"].ToString();
                    txtInvVal.EditValue = table.Rows[0]["tot_net"].ToString();
                    CheckPreviousDiscount(
                        Convert.ToInt32(table.Rows[0]["id"]));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SearchByInvoiceNumber(int invNo)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT cust_id, tot_net FROM Inv " +
                    $"WHERE pay_type=-1 AND inv_type=2 " +
                    $"AND IS_Deleted=0 AND id={invNo} AND proc_type=1",
                    _conn);

                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    cmbClients.EditValue = table.Rows[0]["cust_id"];
                    txtInvVal.EditValue = table.Rows[0]["tot_net"].ToString();
                    CheckPreviousDiscount(invNo);
                }
                else
                {
                    ClearForm();
                    InvoiceSearch();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void SearchByClientName()
        {
            try
            {
                string name = cmbClients.Text;
                _searchName = name;

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE IS_Deleted=0 AND name=N'{name}' " +
                    $"AND (type=1 OR type=3)",
                    _conn);

                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                    cmbClients.EditValue = table.Rows[0]["id"];
                else
                    OpenAccountSearch();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OpenAccountSearch()
        {
            var searchForm = new frmAccountSrch();
            searchForm.cond = " AND ParentCode=123 ";
            _searchName = cmbClients.Text;
            searchForm.txtSrchNm.Text = _searchName;
            searchForm.ShowDialog();

            if (searchForm.Code > -1)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE (type=1 OR type=3) " +
                    $"AND AccountCode={searchForm.Code} AND IS_Deleted=0",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                    cmbClients.EditValue = table.Rows[0]["id"];
            }
        }

        private void CheckPreviousDiscount(int invNo)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT Doc_No FROM CreditDeptNotes " +
                $"WHERE Doc_Type=2 AND IS_Deleted=0 AND Inv_No={invNo}",
                _conn1);
            var table = new DataTable();
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                var result = DXMessageBox.Show(
                    "تم الخصم من هذه الفاتورة مسبقاً. هل تريد الخصم مرة أخرى؟",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var sumAdapter = new SqlDataAdapter(
                        $"SELECT SUM(Discount) AS sum FROM CreditDeptNotes " +
                        $"WHERE Doc_Type=2 AND IS_Deleted=0 AND Inv_No={invNo}",
                        _conn1);
                    var sumTable = new DataTable();
                    sumAdapter.Fill(sumTable);

                    if (sumTable.Rows.Count > 0)
                        txtPreviousDiscVal.EditValue =
                            sumTable.Rows[0]["sum"].ToString();
                }
                else
                {
                    ClearForm();
                }
            }
            else
            {
                txtPreviousDiscVal.EditValue = "0";
            }
        }

        private void GetClientBalance()
        {
            try
            {
                // جلب كود الحساب
                var accAdapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index " +
                    $"WHERE Type=2 AND AName=N'{cmbClients.Text}'",
                    _conn);
                var accTable = new DataTable();
                accAdapter.Fill(accTable);

                int accCode = -1;
                if (accTable.Rows.Count > 0)
                    accCode = Convert.ToInt32(accTable.Rows[0][0]);

                string branchFilter = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} " +
                      $"AND Entry_sub.branch={MainClass.BranchNo} AND "
                    : string.Empty;

                var balAdapter = new SqlDataAdapter(
                    $"SELECT SUM(Entry_sub.dept) AS dept, " +
                    $"       SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE {branchFilter} " +
                    $"Entry.IS_Deleted=0 AND Entry.state=1 " +
                    $"AND Entry.date<=@date2 " +
                    $"AND Entry.GlobalId=Entry_sub.EntryGlobalId " +
                    $"AND Entry_sub.acc_no={accCode}",
                    _conn);

                balAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    txtDate.DateTime.AddHours(24);

                var balTable = new DataTable();
                balAdapter.Fill(balTable);

                if (balTable.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(balTable.Rows[0][0].ToString()))
                {
                    double debit = Convert.ToDouble(balTable.Rows[0][0]);
                    double credit = Convert.ToDouble(balTable.Rows[0][1]);
                    double balance = Math.Abs(debit - credit);
                    txtInvVal.EditValue = Math.Round(balance, 3).ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadInvoiceData(int invId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT cust_id, tot_net FROM Inv " +
                    $"WHERE inv_type=2 AND IS_Deleted=0 " +
                    $"AND id={invId} AND proc_type=1",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                    txtInvVal.EditValue = table.Rows[0]["tot_net"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private int GetClientAccountCode(int customerId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AccountCode FROM Customers " +
                    $"WHERE (type=1 OR type=3) AND IS_Deleted=0 " +
                    $"AND id={customerId}",
                    _conn1);
                var table = new DataTable();
                adapter.Fill(table);

                return table.Rows.Count > 0
                    ? Convert.ToInt32(table.Rows[0][0]) : -1;
            }
            catch
            {
                return -1;
            }
        }

        #endregion

        #region Save

        private void Save()
        {
            SqlTransaction transaction = null;

            try
            {
                if (cmbClients.EditValue == null)
                {
                    DXMessageBox.Show("يجب اختيار العميل",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (rdInvNote.IsChecked == true &&
                    string.IsNullOrWhiteSpace(txtInvNo.EditValue?.ToString()))
                {
                    DXMessageBox.Show("أدخل رقم الفاتورة",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtInvNo.Focus();
                    return;
                }

                var confirmResult = DXMessageBox.Show(
                    "هل أنت متأكد من حفظ السند؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                transaction = _conn.BeginTransaction();

                SqlCommand cmd;

                if (_currentCode == -1)
                {
                    LoadNextNumber();
                    cmd = new SqlCommand(
                        $"INSERT INTO CreditDeptNotes " +
                        $"(Doc_No,Doc_Type,Inv_No,Discount,Net,Date," +
                        $"IS_Deleted,Edit_Date,emp_id,notes,cust_id,Inv_Val) " +
                        $"VALUES({txtNo.Text},2,@Inv_No,@Discount,@Net,@Date," +
                        $"0,@Edit_Date,@emp_id,@notes,@cust_id,@Inv_Val)",
                        _conn, transaction);
                }
                else
                {
                    cmd = new SqlCommand(
                        $"UPDATE CreditDeptNotes SET " +
                        $"Inv_No=@Inv_No,Discount=@Discount,Net=@Net," +
                        $"Edit_Date=@Edit_Date,emp_id=@emp_id,notes=@notes," +
                        $"cust_id=@cust_id,Inv_Val=@Inv_Val " +
                        $"WHERE Doc_Type=2 AND Doc_No={_currentCode}",
                        _conn, transaction);
                }

                cmd.Parameters.Add("@Inv_No", SqlDbType.Int).Value =
                    rdMainNote.IsChecked == true
                        ? (object)-1
                        : Convert.ToInt32(
                            Convert.ToDouble(txtInvNo.EditValue?.ToString() ?? "0"));

                cmd.Parameters.Add("@Discount", SqlDbType.Float).Value =
                    SafeDouble(txtDiscountVal.EditValue);
                cmd.Parameters.Add("@Net", SqlDbType.Float).Value =
                    SafeDouble(txtNet.EditValue);
                cmd.Parameters.Add("@Date", SqlDbType.DateTime).Value =
                    txtDate.DateTime;
                cmd.Parameters.Add("@Edit_Date", SqlDbType.DateTime).Value =
                    DateTime.Now.ToShortDateString();
                cmd.Parameters.Add("@emp_id", SqlDbType.Int).Value =
                    MainClass.UserID;
                cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    txtNotes.EditValue?.ToString() ?? string.Empty;
                cmd.Parameters.Add("@cust_id", SqlDbType.Int).Value =
                    cmbClients.EditValue;
                cmd.Parameters.Add("@Inv_Val", SqlDbType.Float).Value =
                    SafeDouble(txtNet.EditValue);

                cmd.ExecuteNonQuery();

                // إنشاء قيد محاسبي
                if (_currentCode == -1)
                    EntryOper.GetEntryGlobalID(
                        ref _entryGlobalId, ref _bondId);

                var entry = BuildEntry();

                if (entry != null)
                {
                    if (!new EntryOper().SaveEnty(entry))
                    {
                        transaction.Rollback();
                        ShowError("خطأ أثناء الحفظ");
                        return;
                    }

                    transaction.Commit();
                }

                if (_currentCode == -1)
                    _currentCode = Convert.ToInt32(
                        Convert.ToDouble(txtNo.Text ?? "0"));

                DXMessageBox.Show("✅ تم الحفظ بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ShowError($"خطأ أثناء الحفظ:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private Entry BuildEntry()
        {
            var entry = new Entry();
            var accounts = new List<Account>();

            entry.EntryGlobalID = _entryGlobalId;
            entry.ClientCode = Sync.ClientCode;
            entry.EntryNo = Convert.ToInt32(
                _entryGlobalId.Substring(
                    _entryGlobalId.LastIndexOf('-') + 1));
            entry.EntryDate = txtDate.DateTime;
            entry.ReffNo = txtNo.Text;
            entry.RefDate = txtDate.DateTime;
            entry.Type = EntryType.DebtNote;
            entry.State = 1;
            entry.Note = $"إشعار مدين رقم:{txtNo.Text} لعميل:{cmbClients.Text}";
            entry.Branch = MainClass.BranchNo;
            entry.EmpID = MainClass.EmpNo;
            entry.BranchType = Sync.BranchType;
            entry.DistBranch = Sync.DistBranch;

            double discVal = SafeDouble(txtDiscountVal.EditValue);
            double taxVal = SafeDouble(txtTax.EditValue);

            if (discVal > 0)
            {
                int custAccCode = GetClientAccountCode(
                    Convert.ToInt32(cmbClients.EditValue));

                accounts.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo = entry.EntryNo,
                    Name = cmbClients.Text,
                    Code = custAccCode.ToString(),
                    Debt = 0.0,
                    Credit = discVal,
                    Note = entry.Note,
                    CCcode = "-1"
                });

                accounts.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo = entry.EntryNo,
                    Name = "خصم ممنوح",
                    Code = "4100003",
                    Debt = discVal - taxVal,
                    Credit = 0.0,
                    Note = "خصم ممنوح",
                    CCcode = "-1"
                });

                accounts.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo = entry.EntryNo,
                    Name = "الضريبة المضافة",
                    Code = "2222001",
                    Debt = taxVal,
                    Credit = 0.0,
                    Note = $"ض.القيمة المضافة - إشعار مدين رقم:{_currentCode} - عميل:{cmbClients.Text}",
                    CCcode = "-1"
                });
            }

            entry.Accounts = accounts;
            return entry;
        }

        #endregion

        #region Delete

        private void DeleteNote()
        {
            if (_currentCode == -1)
            {
                DXMessageBox.Show("اختر سنداً ليتم حذفه",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = DXMessageBox.Show(
                "هل أنت متأكد من حذف السند؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (new ReceiptOper().DeleteCreditNote(
                        _currentCode, 2, _entryGlobalId))
                {
                    DXMessageBox.Show("✅ تم الحذف بنجاح",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                }
                else
                {
                    ShowError("خطأ أثناء الحذف");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحذف:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Print

        private DataSet BindToReportData()
        {
            string custVatNo = string.Empty;
            string custAccCode = string.Empty;
            string custMobile = string.Empty;

            if (cmbClients.EditValue != null)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT tax_no, name, AccountCode, mobile FROM customers " +
                    $"WHERE id={cmbClients.EditValue}",
                    _conn1);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    custVatNo = table.Rows[0][0].ToString();
                    custAccCode = table.Rows[0]["AccountCode"].ToString();
                    custMobile = table.Rows[0]["mobile"].ToString();
                }
            }

            var data = new InvoiceData
            {
                InvoiceType = Title,
                InvoiceNo = txtInvNo.EditValue?.ToString() ?? string.Empty,
                Total = txtInvVal.EditValue?.ToString() ?? string.Empty,
                Discount = txtDiscountVal.EditValue?.ToString() ?? string.Empty,
                SubDiscount = txtDiscountPerc.EditValue?.ToString() ?? string.Empty,
                Net = txtNet.EditValue?.ToString() ?? string.Empty,
                InvDate = txtDate.DateTime.ToShortDateString(),
                OrderNo = txtNo.Text,
                Customer = cmbClients.Text,
                InvNote = txtNotes.EditValue?.ToString() ?? string.Empty,
                CustVATno = custVatNo,
                CustAccCode = custAccCode,
                CustMobile = custMobile,
                Tax = txtTax.EditValue?.ToString() ?? string.Empty,
                User = Common.GetEmpName(MainClass.EmpNo)
            };

            var list = new List<InvoiceData> { data };
            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        private void PrintDevexpress(int printMode)
        {
            _reportUrl = MainClass.ReportsPath;
            _reportName = "CreditNote.repx";
            _defaultPrinter = MainClass.ReportsPrinter;

            if (string.IsNullOrWhiteSpace(
                    txtDiscountVal.EditValue?.ToString()))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_reportUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(_reportUrl, _reportName);

            if (!Directory.Exists(_reportUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show(
                    "المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var report = XtraReport.FromFile(fullPath);
            report.DataSource = BindToReportData();

            // Header subreport
            var headerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "header.repx"));
            headerReport.DataSource = Common.FoundationInfoDT;
            var headerSub = report.FindControl(
                "headerRpt", ignoreCase: true) as XRSubreport;
            if (headerSub != null)
                headerSub.ReportSource = headerReport;

            // Footer subreport
            var footerReport = XtraReport.FromFile(
                Path.Combine(_reportUrl, "footer.repx"));
            footerReport.DataSource = Common.FoundationInfoDT;
            var footerSub = report.FindControl(
                "footerRpt", ignoreCase: true) as XRSubreport;
            if (footerSub != null)
                footerSub.ReportSource = footerReport;

            if (string.IsNullOrEmpty(_defaultPrinter))
            {
                DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            report.PrinterName = _defaultPrinter;

            if (printMode == 1)
            {
                for (int i = 1; i <= _printNo; i++)
                    report.Print();
            }
            else
            {
                report.ShowPreviewDialog();
            }

            report.Dispose();
        }

        #endregion

        #region Button Handlers

        private void BtnNew_Click(object sender, RoutedEventArgs e)
            => ClearForm();

        private void BtnSave_Click(object sender, RoutedEventArgs e)
            => Save();

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
            => DeleteNote();

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate(
                "SELECT TOP 1 * FROM CreditDeptNotes " +
                "WHERE Doc_Type=2 AND IS_Deleted=0 ORDER BY Doc_No ASC");

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate(
                "SELECT TOP 1 * FROM CreditDeptNotes " +
                $"WHERE Doc_Type=2 AND IS_Deleted=0 AND Doc_No<{_currentCode} " +
                "ORDER BY Doc_No DESC");

        private void BtnNext_Click(object sender, RoutedEventArgs e)
            => Navigate(
                "SELECT TOP 1 * FROM CreditDeptNotes " +
                $"WHERE Doc_Type=2 AND IS_Deleted=0 AND Doc_No>{_currentCode} " +
                "ORDER BY Doc_No ASC");

        private void BtnLast_Click(object sender, RoutedEventArgs e)
            => Navigate(
                "SELECT TOP 1 * FROM CreditDeptNotes " +
                "WHERE Doc_Type=2 AND IS_Deleted=0 ORDER BY Doc_No DESC");

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (rdMainNote.IsChecked == true)
                OpenAccountSearch();
            else
                InvoiceSearch();
        }

        #endregion

        #region TextBox Events

        private void TxtDiscountVal_LostFocus(object sender, RoutedEventArgs e)
        {
            CalcDiscount();
        }

        private void TxtDiscountPerc_LostFocus(object sender, RoutedEventArgs e)
        {
            string percText = txtDiscountPerc.EditValue?.ToString().Trim()
                              ?? string.Empty;
            if (string.IsNullOrEmpty(percText)) return;

            double invVal = SafeDouble(txtInvVal.EditValue);
            double prevDisc = SafeDouble(txtPreviousDiscVal.EditValue);
            double perc = SafeDouble(percText);
            double netBase = invVal - prevDisc;

            txtDiscountVal.EditValue =
                (perc / 100.0 * netBase).ToString(_invoiceObj.DigitsNo);

            CalcTotals();
        }

        private void TxtDiscountVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                CalcDiscount();
        }

        private void TxtDiscountPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                TxtDiscountPerc_LostFocus(sender, e);
        }

        private void TxtInvNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            string invText = txtInvNo.EditValue?.ToString() ?? string.Empty;
            if (int.TryParse(invText, out int invNo))
                SearchByInvoiceNumber(invNo);
        }

        #endregion

        #region ComboBox Events

        private void CmbClients_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            try
            {
                if (rdMainNote.IsChecked == true)
                    GetClientBalance();
            }
            catch { /* ignore */ }
        }

        #endregion

        #region Note Type Changed

        private void RdNoteType_Changed(object sender, RoutedEventArgs e)
        {
            ClearForm();

            bool isMainNote = rdMainNote.IsChecked == true;

            lblInvNo.Visibility = isMainNote ? Visibility.Collapsed : Visibility.Visible;
            txtInvNo.Visibility = isMainNote ? Visibility.Collapsed : Visibility.Visible;
            txtTax.EditValue = "0";

            lblInvVal.Text = isMainNote
                ? (MainClass.Language == "en-us" ? "Balance" : "رصيد الحساب")
                : (MainClass.Language == "en-us" ? "Invoice Value" : "قيمة الفاتورة");

            if (_currentCode == -1)
                LoadNextNumber();
        }

        #endregion

        #region Status Bar & Utilities

        private void UpdateStatusBar(string message)
        {
            txtStatusBar.Text = message;
        }

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}