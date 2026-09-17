using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPaymentVoucher : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private int _currentCode = -1;
        private int _restCode    = -1;

        private ObservableCollection<VoucherRow> _searchSource
            = new ObservableCollection<VoucherRow>();

        #endregion

        #region Constructor

        public frmPaymentVoucher()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            dgvSrch.ItemsSource = _searchSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;
            txtDate.DateTime     = DateTime.Now;

            LoadAccounts();
            LoadSales();
            LoadNxtNo();
            txtBalanceType.Text = "";
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region Clear / Reset

        private void CLR()
        {
            txtNo.Text       = "";
            txtVal.Text      = "";
            txtBalance.Text  = "";
            txtNotes.Text    = "";
            txtCheckNo.Text  = "";
            txtBankCheck.Text = "";
            txtValD.Text     = "";

            txtDate.DateTime = DateTime.Now;
            txtCheckDate.DateTime = DateTime.Now;

            rdCash.IsChecked = true;
            Panel1.Visibility = Visibility.Collapsed;

            cmbAccounts.SelectedIndex  = -1;
            cmbEda3.SelectedIndex      = -1;
            cmbSalesMan.SelectedIndex  = -1;
            cmbCheckType.SelectedIndex = -1;

            txtBalanceType.Text = "";
            _currentCode = -1;
            _restCode    = -1;
            LoadNxtNo();
        }

        #endregion

        #region Load Data

        private void LoadNxtNo()
        {
            try
            {
                int nextNo = 1;
                using (var da = new SqlDataAdapter(
                    $"SELECT MAX(id) FROM SandSD WHERE branch={MainClass.BranchNo}",
                    _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                    {
                        nextNo = Convert.ToInt32(dt.Rows[0][0]) + 1;
                    }
                }
                txtNo.Text = nextNo.ToString();
            }
            catch { }
        }

        private void LoadAccounts()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND ParentCode=2223",
                    _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbAccounts.ItemsSource       = dt.DefaultView;
                    cmbAccounts.DisplayMemberPath = "AName";
                    cmbAccounts.SelectedValuePath = "Code";
                    cmbAccounts.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ تحميل الحسابات: " + ex.Message);
            }
        }

        public void LoadSales()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM Employees, EmpBranches " +
                    $"WHERE Employees.id=EmpBranches.emp AND EmpBranches.branch={MainClass.BranchNo} " +
                    $"AND IS_Deleted=0 ORDER BY id",
                    _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbSalesMan.ItemsSource       = dt.DefaultView;
                    cmbSalesMan.DisplayMemberPath = "name";
                    cmbSalesMan.SelectedValuePath = "id";
                    cmbSalesMan.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadPaymentSource(bool isCash)
        {
            try
            {
                string sql = isCash
                    ? $"SELECT id, name FROM Stocks WHERE branch={MainClass.BranchNo} AND IS_Deleted=0 AND status<>2 ORDER BY id"
                    : "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id";

                using (var da = new SqlDataAdapter(sql, _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbEda3.ItemsSource       = dt.DefaultView;
                    cmbEda3.DisplayMemberPath = "name";
                    cmbEda3.SelectedValuePath = "id";
                    cmbEda3.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadGrid(string condition)
        {
            _searchSource.Clear();
            try
            {
                string sql = $@"SELECT SandSD.id, SandSD.date, SandSD.val, Employees.name
                                FROM SandSD, Employees
                                WHERE SandSD.IS_Deleted=0 AND {condition}
                                SandSD.emp_person=Employees.id";

                using (var da = new SqlDataAdapter(sql, _conn))
                {
                    if (condition.Contains("@date1"))
                    {
                        da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtFromDate.DateTime.ToShortDateString();
                        da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = txtToDate.DateTime.AddHours(24);
                    }

                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _searchSource.Add(new VoucherRow
                        {
                            id          = Convert.ToInt32(row["id"]),
                            voucherDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                            val         = row["val"].ToString(),
                            empName     = row["name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        #endregion

        #region Balance Calculation

        private void GetCustBalance()
        {
            try
            {
                txtBalanceType.Text = "";
                txtBalance.Text     = "";

                if (cmbAccounts.SelectedIndex == -1) return;

                int accCode = -1;
                using (var da = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName='{cmbAccounts.Text}'",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        accCode = Convert.ToInt32(dt.Rows[0][0]);
                }

                if (accCode == -1) return;

                string branchCond = (MainClass.BranchNo != -1)
                    ? $"Entry.branch={MainClass.BranchNo} AND Entry_sub.branch={MainClass.BranchNo} AND "
                    : "";

                using (var da = new SqlDataAdapter(
                    $"SELECT SUM(Entry_sub.dept) AS dept, SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE {branchCond} Entry.IS_Deleted=0 AND Entry.state=1 " +
                    $"AND Entry.date<=@date2 AND Entry.id=Entry_sub.res_id " +
                    $"AND Entry_sub.acc_no={accCode}",
                    _conn))
                {
                    da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                        = txtDate.DateTime.AddHours(24);

                    var dt = new DataTable();
                    da.Fill(dt);

                    double dept   = 0;
                    double credit = 0;

                    if (dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                    {
                        double.TryParse(dt.Rows[0]["dept"].ToString(),   out dept);
                        double.TryParse(dt.Rows[0]["credit"].ToString(), out credit);
                    }

                    if (dept >= credit)
                    {
                        txtBalance.Text     = Math.Round(dept - credit, 3).ToString();
                        txtBalanceType.Text = "مدين";
                    }
                    else
                    {
                        txtBalance.Text     = Math.Round(credit - dept, 3).ToString();
                        txtBalanceType.Text = "دائن";
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var dr = cmd.ExecuteReader())
                    ReadData(dr);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();
            CLR();

            _currentCode   = Convert.ToInt32(dr["id"]);
            txtNo.Text     = _currentCode.ToString();
            _restCode      = Convert.ToInt32(dr["rest_id"]);

            if (DateTime.TryParse(dr["date"].ToString(), out DateTime vDate))
                txtDate.DateTime = vDate;

            if (cmbAccounts.SelectedValue != null)
                cmbAccounts.SelectedValue = dr["acc_code"];

            txtVal.Text = dr["val"].ToString();

            int payType = Convert.ToInt32(dr["type"]);
            if (payType == 1)
            {
                rdCash.IsChecked      = true;
                Panel1.Visibility     = Visibility.Collapsed;
            }
            else
            {
                rdCheck.IsChecked     = true;
                Panel1.Visibility     = Visibility.Visible;
                txtCheckNo.Text       = dr["check_no"].ToString();

                if (DateTime.TryParse(dr["check_date"].ToString(), out DateTime chkDate))
                    txtCheckDate.DateTime = chkDate;

                txtBankCheck.Text = dr["checkbank"].ToString();

                if (int.TryParse(dr["check_state"].ToString(), out int chkState))
                    cmbCheckType.SelectedIndex = chkState - 1;

                txtNotes.Text = dr["notes"].ToString();
            }

            try { cmbEda3.SelectedValue    = dr["safe_bank_id"]; } catch { }
            try { cmbSalesMan.SelectedValue = dr["sales_emp"];    } catch { }

            GetCustBalance();
        }

        private string GetBranchCondition()
        {
            return (MainClass.BranchNo != -1)
                ? $"branch={MainClass.BranchNo} AND "
                : "";
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SandSD WHERE {GetBranchCondition()} IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SandSD WHERE {GetBranchCondition()} IS_Deleted=0 AND id<{_currentCode} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SandSD WHERE {GetBranchCondition()} IS_Deleted=0 AND id>{_currentCode} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SandSD WHERE {GetBranchCondition()} IS_Deleted=0 ORDER BY id DESC");

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SqlTransaction transaction = null;
            try
            {
                #region التحقق

                if (string.IsNullOrWhiteSpace(txtVal.Text))
                {
                    DXMessageBox.Show("أدخل القيمة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtVal.Focus();
                    return;
                }
                if (cmbAccounts.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار الحساب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbAccounts.Focus();
                    return;
                }
                if (cmbEda3.SelectedIndex == -1)
                {
                    string msg = (rdCash.IsChecked == true) ? "يجب اختيار الخزنة" : "يجب اختيار البنك";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbEda3.Focus();
                    return;
                }

                var confirm = DXMessageBox.Show("هل أنت متأكد من حفظ السند؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                #endregion

                EnsureOpen(_conn);
                transaction = _conn.BeginTransaction();

                string branchFilter = (MainClass.BranchNo != -1)
                    ? $" WHERE branch={MainClass.BranchNo}" : "";

                int entryId;
                if (_currentCode == -1)
                {
                    using (var maxCmd = new SqlCommand(
                        $"SELECT MAX(id) FROM Entry{branchFilter}", _conn, transaction))
                    {
                        object r = maxCmd.ExecuteScalar();
                        entryId = (r != DBNull.Value) ? Convert.ToInt32(r) + 1 : 1;
                    }
                }
                else
                {
                    entryId = _restCode;
                    using (var delCmd = new SqlCommand(
                        $"DELETE FROM Entry WHERE id={entryId}", _conn, transaction))
                        delCmd.ExecuteNonQuery();
                    using (var delCmd2 = new SqlCommand(
                        $"DELETE FROM Entry_sub WHERE res_id={entryId}", _conn, transaction))
                        delCmd2.ExecuteNonQuery();
                }

                int salesManId = -1;
                if (cmbSalesMan.SelectedIndex != -1 && cmbSalesMan.SelectedValue != null)
                    int.TryParse(cmbSalesMan.SelectedValue.ToString(), out salesManId);

                SqlCommand cmd;
                if (_currentCode == -1)
                {
                    LoadNxtNo();
                    cmd = new SqlCommand(
                        $"INSERT INTO SandSD (id,date,emp_person,emp_id,acc_code,val,type," +
                        $"safe_bank_id,sales_emp,check_no,check_date,checkbank,check_state," +
                        $"notes,rest_id,branch,IS_Deleted) VALUES ({txtNo.Text}," +
                        $"@date,@emp_person,@emp_id,@acc_code,@val,@type," +
                        $"@safe_bank_id,@sales_emp,@check_no,@check_date,@checkbank,@check_state," +
                        $"@notes,@rest_id,@branch,@IS_Deleted)",
                        _conn, transaction);
                }
                else
                {
                    cmd = new SqlCommand(
                        $"UPDATE SandSD SET date=@date,emp_person=@emp_person,emp_id=@emp_id," +
                        $"acc_code=@acc_code,val=@val,type=@type,safe_bank_id=@safe_bank_id," +
                        $"sales_emp=@sales_emp,check_no=@check_no,check_date=@check_date," +
                        $"checkbank=@checkbank,check_state=@check_state,notes=@notes,IS_Deleted=@IS_Deleted " +
                        $"WHERE branch={MainClass.BranchNo} AND id={_currentCode}",
                        _conn, transaction);
                }

                cmd.Parameters.Add("@date",        SqlDbType.DateTime).Value = txtDate.DateTime;
                cmd.Parameters.Add("@emp_person",  SqlDbType.Int).Value      = 1;
                cmd.Parameters.Add("@emp_id",      SqlDbType.Int).Value      = MainClass.UserID;
                cmd.Parameters.Add("@acc_code",    SqlDbType.Int).Value      = cmbAccounts.SelectedValue ?? DBNull.Value;
                cmd.Parameters.Add("@val",         SqlDbType.Float).Value    = txtVal.Text;

                int payTypeVal = (rdCash.IsChecked == true) ? 1 : 2;

                if (rdCheck.IsChecked == true)
                {
                    cmd.Parameters.Add("@check_no",    SqlDbType.NVarChar).Value  = txtCheckNo.Text;
                    cmd.Parameters.Add("@check_date",  SqlDbType.DateTime).Value  = txtCheckDate.DateTime.ToShortDateString();
                    cmd.Parameters.Add("@checkbank",   SqlDbType.NVarChar).Value  = txtBankCheck.Text;
                    cmd.Parameters.Add("@check_state", SqlDbType.Int).Value       = cmbCheckType.SelectedIndex + 1;
                }
                else
                {
                    cmd.Parameters.Add("@check_no",    SqlDbType.NVarChar).Value  = DBNull.Value;
                    cmd.Parameters.Add("@check_date",  SqlDbType.DateTime).Value  = DBNull.Value;
                    cmd.Parameters.Add("@checkbank",   SqlDbType.NVarChar).Value  = DBNull.Value;
                    cmd.Parameters.Add("@check_state", SqlDbType.Int).Value       = DBNull.Value;
                }

                cmd.Parameters.Add("@type",        SqlDbType.Int).Value      = payTypeVal;
                cmd.Parameters.Add("@safe_bank_id",SqlDbType.Int).Value      = cmbEda3.SelectedValue ?? DBNull.Value;
                cmd.Parameters.Add("@sales_emp",   SqlDbType.Int).Value      = salesManId;
                cmd.Parameters.Add("@rest_id",     SqlDbType.Int).Value      = entryId;
                cmd.Parameters.Add("@notes",       SqlDbType.NVarChar).Value = txtNotes.Text;
                cmd.Parameters.Add("@branch",      SqlDbType.Int).Value      = MainClass.BranchNo;
                cmd.Parameters.Add("@IS_Deleted",  SqlDbType.Bit).Value      = 0;
                cmd.ExecuteNonQuery();

                if (_currentCode == -1)
                {
                    int.TryParse(txtNo.Text, out _currentCode);
                }

                // بحث كود الحساب المحدد
                int accCodeVal = 0;
                int safeBankCodeVal = 0;

                using (var da1 = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND AName='{cmbAccounts.Text}'",
                    _conn1))
                {
                    var dt1 = new DataTable();
                    da1.Fill(dt1);
                    if (dt1.Rows.Count > 0)
                        int.TryParse(dt1.Rows[0][0].ToString(), out accCodeVal);
                }

                using (var da2 = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 {Accounting.BranchCondition} AND AName='{cmbEda3.Text}'",
                    _conn1))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                        int.TryParse(dt2.Rows[0][0].ToString(), out safeBankCodeVal);
                }

                string entryNote = $"سند صرف لمقاول رقم: {_currentCode} - سداد دفعة من حساب: {cmbAccounts.Text}";

                // إدراج القيد
                using (var entryCmd = new SqlCommand(
                    "INSERT INTO Entry (id,date,doc_no,type,state,notes,branch,IS_Deleted) " +
                    "VALUES (@id,@date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    _conn, transaction))
                {
                    entryCmd.Parameters.Add("@id",         SqlDbType.Int).Value      = entryId;
                    entryCmd.Parameters.Add("@date",       SqlDbType.DateTime).Value = txtDate.DateTime;
                    entryCmd.Parameters.Add("@doc_no",     SqlDbType.Int).Value      = _currentCode;
                    entryCmd.Parameters.Add("@type",       SqlDbType.Int).Value      = 8;
                    entryCmd.Parameters.Add("@state",      SqlDbType.Int).Value      = 1;
                    entryCmd.Parameters.Add("@notes",      SqlDbType.NVarChar).Value = entryNote;
                    entryCmd.Parameters.Add("@branch",     SqlDbType.Int).Value      = MainClass.BranchNo;
                    entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value      = 0;
                    entryCmd.ExecuteNonQuery();
                }

                double.TryParse(txtVal.Text, out double voucherAmount);

                // السطر الأول: credit من الخزنة/البنك
                using (var sub1 = new SqlCommand(
                    "INSERT INTO Entry_sub (res_id,dept,credit,acc_no,notes,branch) VALUES (@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                    _conn, transaction))
                {
                    sub1.Parameters.Add("@res_id", SqlDbType.Int).Value      = entryId;
                    sub1.Parameters.Add("@dept",   SqlDbType.Float).Value    = 0;
                    sub1.Parameters.Add("@credit", SqlDbType.Float).Value    = voucherAmount;
                    sub1.Parameters.Add("@acc_no", SqlDbType.Int).Value      = safeBankCodeVal;
                    sub1.Parameters.Add("@notes",  SqlDbType.NVarChar).Value = entryNote;
                    sub1.Parameters.Add("@branch", SqlDbType.Int).Value      = MainClass.BranchNo;
                    sub1.ExecuteNonQuery();
                }

                // السطر الثاني: dept للحساب
                using (var sub2 = new SqlCommand(
                    "INSERT INTO Entry_sub (res_id,dept,credit,acc_no,notes,branch) VALUES (@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                    _conn, transaction))
                {
                    sub2.Parameters.Add("@res_id", SqlDbType.Int).Value      = entryId;
                    sub2.Parameters.Add("@dept",   SqlDbType.Float).Value    = voucherAmount;
                    sub2.Parameters.Add("@credit", SqlDbType.Float).Value    = 0;
                    sub2.Parameters.Add("@acc_no", SqlDbType.Int).Value      = accCodeVal;
                    sub2.Parameters.Add("@notes",  SqlDbType.NVarChar).Value = entryNote;
                    sub2.Parameters.Add("@branch", SqlDbType.Int).Value      = MainClass.BranchNo;
                    sub2.ExecuteNonQuery();
                }

                transaction.Commit();

                DXMessageBox.Show("تم الحفظ بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
                _searchSource.Clear();
                CLR();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    DXMessageBox.Show("اختر سندًا ليتم حذفه", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var answer = DXMessageBox.Show("هل أنت متأكد من حذف السند؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;

                EnsureOpen(_conn);

                using (var cmd1 = new SqlCommand(
                    $"UPDATE SandSD SET IS_Deleted=1 WHERE branch={MainClass.BranchNo} AND id={_currentCode}",
                    _conn))
                    cmd1.ExecuteNonQuery();

                using (var cmd2 = new SqlCommand(
                    $"UPDATE Entry SET state=2 WHERE branch={MainClass.BranchNo} AND id={_restCode}",
                    _conn))
                    cmd2.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                _searchSource.Clear();
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        #endregion

        #region ComboBox / Radio Events

        private void rdPayType_Changed(object sender, RoutedEventArgs e)
        {
            bool isCash = (rdCash.IsChecked == true);
            Panel1.Visibility = isCash ? Visibility.Collapsed : Visibility.Visible;
            LoadPaymentSource(isCash);
        }

        private void cmbAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { GetCustBalance(); } catch { }
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is VoucherRow row)
            {
                Navigate($"SELECT * FROM SandSD WHERE id={row.id}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Search

        private void txtSrchNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void Search()
        {
            string branchCond = (MainClass.BranchNo != -1)
                ? $"SandSD.branch={MainClass.BranchNo} AND " : "";

            string condition;
            if (string.IsNullOrWhiteSpace(txtSrchNo.Text))
                condition = branchCond + " date>=@date1 AND date<=@date2 AND ";
            else
                condition = branchCond + $" SandSD.id={txtSrchNo.Text} AND ";

            LoadGrid(condition);
            TabControl1.SelectedIndex = 1;
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion
    }

    #region Model

    public class VoucherRow
    {
        public int    id          { get; set; }
        public string voucherDate { get; set; }
        public string val         { get; set; }
        public string empName     { get; set; }
    }

    #endregion
}