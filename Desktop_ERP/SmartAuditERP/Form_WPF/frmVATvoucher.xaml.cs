using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Editors;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmVATvoucher : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int    Code     = -1;
        private double VAT      = 0.0;
        private int    RestCode = -1;

        private ObservableCollection<VATVoucherSearchModel> searchList;

        #endregion

        #region Constructor

        public frmVATvoucher()
        {
            InitializeComponent();
            conn       = MainClass.ConnObj();
            conn1      = MainClass.ConnObj();
            searchList = new ObservableCollection<VATVoucherSearchModel>();
            dgvSrch.ItemsSource = searchList;
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtFromDate.DateTime = DateTime.Now.Date;
                txtToDate.DateTime   = DateTime.Now.Date;
                txtDate.DateTime     = DateTime.Now.Date;
                txtRefDate.DateTime  = DateTime.Now.Date;

                LoadEmps();
                LoadCostCenters();
                LoadAccounts();
                LoadSales();
                LoadNxtNo();
                LoadClients();

                VAT = MainClass.GetVAT();
                lblVat.Text = $"🧾 ضريبة {VAT}%";

                // تحديث قائمة الخزائن (افتراضي نقدي)
                rdCheck_CheckedChanged(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear

        private void ClearFields()
        {
            try
            {
                LoadNxtNo();
                Code = -1;
                txtNetVal.Text        = "";
                txtVal.Text           = "";
                RefNo.Text            = "";
                txtTaxNo.Text         = "";
                txtNotes.Text         = "";
                txtCheckNo.Text       = "";
                txtBankCheck.Text     = "";
                txtRefDate.DateTime   = DateTime.Now.Date;
                cmbPay.SelectedIndex    = -1;
                cmbClient.SelectedIndex = -1;
                cmbCheckType.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Next No

        private void LoadNxtNo()
        {
            try
            {
                if (conn1.State != ConnectionState.Open) conn1.Open();
                var cmd    = new SqlCommand("SELECT MAX(id) FROM SandVAT", conn1);
                var scalar = cmd.ExecuteScalar();
                double maxId = scalar != DBNull.Value && scalar != null
                    ? Convert.ToDouble(scalar) : 0;
                BondNo.Text = ((int)(maxId + 1)).ToString();
                conn1.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Combos

        public void LoadEmps()
        {
            // تحميل الموظفين (للاستخدام الداخلي)
        }

        private void LoadClients()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM VATClients WHERE IS_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbClient.DisplayMemberPath  = "name";
                cmbClient.SelectedValuePath  = "id";
                cmbClient.ItemsSource        = table.DefaultView;
                cmbClient.SelectedIndex      = -1;
            }
            catch { }
        }

        public void LoadSales()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Employees, EmpBranches " +
                    $"WHERE Employees.id=EmpBranches.emp " +
                    $"AND EmpBranches.branch={MainClass.BranchNo} " +
                    $"AND IS_Deleted=0 ORDER BY id", conn1);
                var table = new DataTable();
                adapter.Fill(table);
                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.ItemsSource       = table.DefaultView;
                cmbSalesMan.SelectedIndex     = -1;
            }
            catch { }
        }

        private void LoadAccounts()
        {
            try
            {
                string filter = rbMsarif.IsChecked == true
                    ? "ParentCode>=311 AND ParentCode<=313"
                    : "ParentCode>=111 AND ParentCode<=116";

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE {filter} AND Type=2 " +
                    $"AND (acc_branch={MainClass.BranchNo} OR acc_branch IS NULL)", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbPay.DisplayMemberPath = "AName";
                cmbPay.SelectedValuePath = "Code";
                cmbPay.ItemsSource       = table.DefaultView;
                cmbPay.SelectedIndex     = -1;
            }
            catch { }
        }

        private void LoadCostCenters()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center WHERE type=2 ORDER BY code", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbCost.DisplayMemberPath    = "name";
                cmbCost.SelectedValuePath    = "code";
                cmbCost.ItemsSource          = table.DefaultView;
                cmbCost.SelectedIndex        = -1;

                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.ItemsSource       = table.DefaultView;
                cmbCostCenter.SelectedIndex     = -1;
            }
            catch { }
        }

        #endregion

        #region Payment Type Changed

        private void rdCheck_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdCash == null) return;

            if (rdCash.IsChecked == true)
            {
                Panel1.Visibility = Visibility.Collapsed;

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks " +
                    $"WHERE branch={MainClass.BranchNo} AND IS_Deleted=0 " +
                    $"AND status<>2 ORDER BY id", conn1);
                var table = new DataTable();
                adapter.Fill(table);
                cmbToAcc.DisplayMemberPath = "name";
                cmbToAcc.SelectedValuePath = "id";
                cmbToAcc.ItemsSource       = table.DefaultView;
                cmbToAcc.SelectedIndex     = -1;
            }
            else
            {
                Panel1.Visibility = Visibility.Visible;

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id", conn1);
                var table = new DataTable();
                adapter.Fill(table);
                cmbToAcc.DisplayMemberPath = "name";
                cmbToAcc.SelectedValuePath = "id";
                cmbToAcc.ItemsSource       = table.DefaultView;
                cmbToAcc.SelectedIndex     = -1;
            }
        }

        #endregion

        #region Radio Buttons

        private void rbMsarif_Checked(object sender, RoutedEventArgs e)
        {
            cmbPay.ItemsSource   = null;
            cmbPay.Items.Clear();
            LoadAccounts();
        }

        private void rbAssess_Checked(object sender, RoutedEventArgs e)
        {
            cmbPay.ItemsSource   = null;
            cmbPay.Items.Clear();
            LoadAccounts();
        }

        #endregion

        #region Value Changed

        private void txtVal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtVal.Text))
            {
                if (double.TryParse(txtVal.Text, out double val))
                {
                    double vatAmt = Math.Round(val * VAT / 100.0, 2);
                    double net    = Math.Round(val + vatAmt, 2);
                    txtVAT.Text    = vatAmt.ToString("N2");
                    txtNetVal.Text = net.ToString("N2");
                }
            }
            else
            {
                txtVAT.Text    = "";
                txtNetVal.Text = "";
            }
        }

        #endregion

        #region Client Selection

        private void cmbClient_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbClient.SelectedValue == null) return;

                if (int.TryParse(cmbClient.SelectedValue.ToString(), out int clientId)
                    && clientId > 0)
                {
                    var adapter = new SqlDataAdapter(
                        $"SELECT taxNo FROM VATClients WHERE IS_Deleted=0 AND id={clientId}",
                        conn);
                    var table = new DataTable();
                    adapter.Fill(table);
                    txtTaxNo.Text = table.Rows.Count == 1
                        ? table.Rows[0]["taxNo"].ToString()
                        : "";
                }
            }
            catch { }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var transaction = conn.BeginTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(txtVal.Text))
                {
                    MessageBox.Show("ادخل القيمة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtVal.Focus();
                    return;
                }
                if (cmbPay.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار الحساب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbPay.Focus();
                    return;
                }
                if (cmbToAcc.SelectedIndex == -1)
                {
                    string msg = rdCash.IsChecked == true ? "يجب اختيار الخزنة" : "يجب اختيار البنك";
                    MessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbToAcc.Focus();
                    return;
                }

                var confirm = MessageBox.Show("هل أنت متأكد من حفظ السند؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                string branchCond = MainClass.BranchNo != -1
                    ? $" WHERE branch={MainClass.BranchNo}" : "";

                int entryId;
                if (Code == -1)
                {
                    var scalarCmd = new SqlCommand(
                        $"SELECT MAX(id) FROM Entry{branchCond}", conn, transaction);
                    var scalar = scalarCmd.ExecuteScalar();
                    double maxId = scalar != DBNull.Value && scalar != null
                        ? Convert.ToDouble(scalar) : 0;
                    entryId = (int)(maxId + 1);
                }
                else
                {
                    entryId = RestCode;
                    new SqlCommand($"DELETE FROM Entry WHERE id={entryId}", conn, transaction)
                        .ExecuteNonQuery();
                    new SqlCommand($"DELETE FROM Entry_sub WHERE res_id={entryId}", conn, transaction)
                        .ExecuteNonQuery();
                }

                int salesEmp = cmbSalesMan.SelectedIndex != -1
                    ? int.TryParse(cmbSalesMan.SelectedValue?.ToString(), out int se) ? se : -1
                    : -1;

                // حفظ سند القبض
                SqlCommand cmd;
                if (Code == -1)
                {
                    LoadNxtNo();
                    cmd = new SqlCommand(
                        $"INSERT INTO SandVaT(id,date,Client,emp_id,acc_code,val,VAT,NetVal," +
                        $"type,safe_bank_id,sales_emp,check_no,check_date,checkbank," +
                        $"check_state,notes,rest_id,branch,IS_Deleted,Reff_No,Reff_date)" +
                        $"VALUES({BondNo.Text},@date,@Client,@emp_id,@acc_code,@val,@VAT," +
                        $"@NetVal,@type,@safe_bank_id,@sales_emp,@check_no,@check_date," +
                        $"@checkbank,@check_state,@notes,@rest_id,@branch,@IS_Deleted," +
                        $"@Reff_No,@Reff_date)", conn, transaction);
                }
                else
                {
                    cmd = new SqlCommand(
                        $"UPDATE SandVAT SET date=@date,Client=@Client,emp_id=@emp_id," +
                        $"acc_code=@acc_code,val=@val,VAT=@VAT,NetVal=@NetVal,type=@type," +
                        $"safe_bank_id=@safe_bank_id,sales_emp=@sales_emp,check_no=@check_no," +
                        $"check_date=@check_date,checkbank=@checkbank,check_state=@check_state," +
                        $"notes=@notes,IS_Deleted=@IS_Deleted,Reff_No=@Reff_No,Reff_date=@Reff_date " +
                        $"WHERE branch={MainClass.BranchNo} AND id={Code}", conn, transaction);
                }

                cmd.Parameters.Add("@date",         SqlDbType.DateTime).Value = txtDate.DateTime;
                cmd.Parameters.Add("@Client",        SqlDbType.Int).Value      =
                    cmbClient.SelectedValue ?? (object)DBNull.Value;
                cmd.Parameters.Add("@emp_id",        SqlDbType.Int).Value      = MainClass.UserID;
                cmd.Parameters.Add("@acc_code",      SqlDbType.Int).Value      = cmbPay.SelectedValue;
                cmd.Parameters.Add("@val",           SqlDbType.Float).Value    = txtVal.Text;
                cmd.Parameters.Add("@VAT",           SqlDbType.Float).Value    = txtVAT.Text;
                cmd.Parameters.Add("@NetVal",        SqlDbType.Float).Value    = txtNetVal.Text;
                cmd.Parameters.Add("@Reff_No",       SqlDbType.Int).Value      =
                    double.TryParse(RefNo.Text, out double rn) ? (object)(int)rn : DBNull.Value;
                cmd.Parameters.Add("@Reff_date",     SqlDbType.DateTime).Value = txtRefDate.DateTime;

                int payType = 1;
                if (rdCheck.IsChecked == true)
                {
                    payType = 2;
                    cmd.Parameters.Add("@check_no",    SqlDbType.NVarChar).Value = txtCheckNo.Text;
                    cmd.Parameters.Add("@check_date",  SqlDbType.DateTime).Value = txtCheckDate.DateTime;
                    cmd.Parameters.Add("@checkbank",   SqlDbType.NVarChar).Value = txtBankCheck.Text;
                    cmd.Parameters.Add("@check_state", SqlDbType.Int).Value      =
                        cmbCheckType.SelectedIndex + 1;
                }
                else
                {
                    cmd.Parameters.Add("@check_no",    SqlDbType.NVarChar).Value = DBNull.Value;
                    cmd.Parameters.Add("@check_date",  SqlDbType.DateTime).Value = DBNull.Value;
                    cmd.Parameters.Add("@checkbank",   SqlDbType.NVarChar).Value = DBNull.Value;
                    cmd.Parameters.Add("@check_state", SqlDbType.Int).Value      = DBNull.Value;
                }

                cmd.Parameters.Add("@type",         SqlDbType.Int).Value      = payType;
                cmd.Parameters.Add("@safe_bank_id", SqlDbType.Int).Value      = cmbToAcc.SelectedValue;
                cmd.Parameters.Add("@sales_emp",    SqlDbType.Int).Value      = salesEmp;
                cmd.Parameters.Add("@rest_id",      SqlDbType.Int).Value      = entryId;
                cmd.Parameters.Add("@notes",        SqlDbType.NVarChar).Value = txtNotes.Text;
                cmd.Parameters.Add("@branch",       SqlDbType.Int).Value      = MainClass.BranchNo;
                cmd.Parameters.Add("@IS_Deleted",   SqlDbType.Bit).Value      = 0;
                cmd.ExecuteNonQuery();

                if (Code == -1)
                    if (double.TryParse(BondNo.Text, out double bn))
                        Code = (int)bn;

                // الحصول على كود الحسابات
                int accCodePay = GetAccountCode(cmbPay.Text, conn1);
                int accCodeTo  = GetAccountCode(cmbToAcc.Text, conn1);

                // إدراج القيد
                var entryCmd = new SqlCommand(
                    "INSERT INTO Entry(id,date,doc_no,type,state,notes,branch,IS_Deleted)" +
                    "VALUES(@id,@date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    conn, transaction);
                entryCmd.Parameters.Add("@id",         SqlDbType.Int).Value      = entryId;
                entryCmd.Parameters.Add("@date",       SqlDbType.DateTime).Value = txtDate.DateTime;
                entryCmd.Parameters.Add("@doc_no",     SqlDbType.Int).Value      = Code;
                entryCmd.Parameters.Add("@type",       SqlDbType.Int).Value      = 9;
                entryCmd.Parameters.Add("@state",      SqlDbType.Int).Value      = 1;
                entryCmd.Parameters.Add("@notes",      SqlDbType.NVarChar).Value =
                    $"سند قبض ضريبة رقم: {Code} - سداد دفعة من حساب: {cmbPay.Text}";
                entryCmd.Parameters.Add("@branch",     SqlDbType.Int).Value      = MainClass.BranchNo;
                entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value      = 0;
                entryCmd.ExecuteNonQuery();

                int costCode = cmbCost.SelectedIndex > -1
                    ? int.TryParse(cmbCost.SelectedValue?.ToString(), out int cc) ? cc : -1
                    : -1;

                double.TryParse(txtNetVal.Text, out double netVal);
                double.TryParse(txtVal.Text, out double valAmt);
                double vatDiff = netVal - valAmt;

                InsertEntrySub(entryId, 0, netVal, accCodeTo,
                    $"سند قبض ضريبة رقم: {Code}", -1, conn, transaction);
                InsertEntrySub(entryId, vatDiff, 0, 2222001,
                    $"سند قبض ضريبة رقم: {Code}", costCode, conn, transaction);
                InsertEntrySub(entryId, valAmt, 0, accCodePay,
                    $"سند قبض ضريبة رقم: {Code}", costCode, conn, transaction);

                transaction.Commit();

                var savedMsg = new frmSavedMsg();
                savedMsg.ShowDialog();
                if (savedMsg.Pressed == 1)
                {
                    searchList.Clear();
                    ClearFields();
                }
                else if (savedMsg.Pressed == 3)
                    this.Close();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void InsertEntrySub(int resId, double credit, double debit,
            int accNo, string notes, int ccCode,
            SqlConnection c, SqlTransaction tr)
        {
            var cmd = new SqlCommand(
                "INSERT INTO Entry_sub(res_id,credit,dept,acc_no,notes,branch,CCcode)" +
                "VALUES(@res_id,@credit,@dept,@acc_no,@notes,@branch,@CCcode)", c, tr);
            cmd.Parameters.Add("@res_id",  SqlDbType.Int).Value      = resId;
            cmd.Parameters.Add("@credit",  SqlDbType.Float).Value    = credit;
            cmd.Parameters.Add("@dept",    SqlDbType.Float).Value    = debit;
            cmd.Parameters.Add("@acc_no",  SqlDbType.Int).Value      = accNo;
            cmd.Parameters.Add("@notes",   SqlDbType.NVarChar).Value = notes;
            cmd.Parameters.Add("@branch",  SqlDbType.Int).Value      = MainClass.BranchNo;
            cmd.Parameters.Add("@CCcode",  SqlDbType.Int).Value      = ccCode;
            cmd.ExecuteNonQuery();
        }

        private int GetAccountCode(string accName, SqlConnection c)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT Code FROM Accounts_Index WHERE Type=2 " +
                $"{Accounting.BranchCondition} AND AName='{accName}'", c);
            var table = new DataTable();
            adapter.Fill(table);
            return table.Rows.Count > 0 &&
                   double.TryParse(table.Rows[0][0].ToString(), out double code)
                ? (int)code : 0;
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    MessageBox.Show("اختر سند ليتم حذفه",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show("هل أنت متأكد من حذف السند؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"UPDATE SandVAT SET IS_Deleted=1 " +
                    $"WHERE branch={MainClass.BranchNo} AND id={Code}", conn)
                    .ExecuteNonQuery();
                new SqlCommand(
                    $"UPDATE Entry SET state=2 " +
                    $"WHERE branch={MainClass.BranchNo} AND id={RestCode}", conn)
                    .ExecuteNonQuery();

                MessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                searchList.Clear();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string branchCond = MainClass.BranchNo != -1
                ? $"SandVAT.branch={MainClass.BranchNo} AND " : "";

            string cond;
            if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                cond = $"{branchCond}SandVAT.id={txtSrchNo.Text} AND SandVAT.IS_Deleted=0";
            else
                cond = $"{branchCond}date>=@date1 AND date<=@date2 AND SandVAT.IS_Deleted=0";

            LoadGrid(cond);
        }

        private void LoadGrid(string cond)
        {
            try
            {
                searchList.Clear();

                var adapter = new SqlDataAdapter(
                    $"SELECT SandVAT.id, SandVAT.date, SandVAT.val, Employees.name " +
                    $"FROM SandVAT, Employees " +
                    $"WHERE SandVAT.emp_id=Employees.id AND {cond}", conn);

                if (!cond.Contains("SandVAT.id="))
                {
                    adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                        txtFromDate.DateTime.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                        txtToDate.DateTime.AddHours(24);
                }

                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    searchList.Add(new VATVoucherSearchModel
                    {
                        id      = row["id"].ToString(),
                        date    = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        val     = row["val"] != DBNull.Value
                            ? Convert.ToDouble(row["val"]) : 0,
                        empName = row["name"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في البحث\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is VATVoucherSearchModel selected)
            {
                if (int.TryParse(selected.id, out int id))
                {
                    Code = id;
                    Navigate($"SELECT * FROM SandVAT WHERE id={Code}");
                    TabControl1.SelectedIndex = 0;
                }
            }
        }

        #endregion

        #region Navigation

        private void ReadData(SqlDataReader reader)
        {
            if (reader.HasRows && reader.Read())
            {
                ClearFields();

                if (double.TryParse(reader["id"].ToString(), out double idVal))
                    Code = (int)idVal;

                BondNo.Text   = Code.ToString();

                if (reader["rest_id"] != DBNull.Value)
                    if (double.TryParse(reader["rest_id"].ToString(), out double rid))
                        RestCode = (int)rid;

                txtNotes.Text = reader["notes"].ToString();

                if (DateTime.TryParse(reader["date"].ToString(), out DateTime dt))
                    txtDate.DateTime = dt;

                if (reader["Client"] != DBNull.Value)
                    cmbClient.SelectedValue = reader["Client"];

                cmbPay.SelectedValue    = reader["acc_code"];
                txtVal.Text             = reader["val"].ToString();
                txtNetVal.Text          = reader["NetVal"].ToString();
                txtTaxNo.Text           = reader["VAT"].ToString();

                try
                {
                    if (DateTime.TryParse(reader["Reff_date"].ToString(), out DateTime rd))
                        txtRefDate.DateTime = rd;
                    RefNo.Text = reader["Reff_No"].ToString();
                }
                catch { }

                if (reader["type"].ToString() == "1")
                {
                    rdCash.IsChecked      = true;
                    Panel1.Visibility     = Visibility.Collapsed;
                }
                else
                {
                    rdCheck.IsChecked     = true;
                    Panel1.Visibility     = Visibility.Visible;
                    txtCheckNo.Text       = reader["check_no"].ToString();
                    txtBankCheck.Text     = reader["checkbank"].ToString();
                    txtNotes.Text         = reader["notes"].ToString();

                    if (DateTime.TryParse(reader["check_date"].ToString(), out DateTime cd))
                        txtCheckDate.DateTime = cd;
                    if (double.TryParse(reader["check_state"].ToString(), out double cs))
                        cmbCheckType.SelectedIndex = (int)(cs - 1);
                }

                cmbToAcc.SelectedValue   = reader["safe_bank_id"].ToString();
                cmbSalesMan.SelectedValue= reader["sales_emp"].ToString();
            }
        }

        public void Navigate(string sqlQuery)
        {
            try
            {
                var cmd = new SqlCommand(sqlQuery, conn);
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var reader = cmd.ExecuteReader())
                    ReadData(reader);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل\n{ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private string GetBranchCond()
        {
            return MainClass.BranchNo != -1
                ? $"branch={MainClass.BranchNo} AND " : "";
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM SandVAT WHERE {GetBranchCond()}IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM SandVAT WHERE {GetBranchCond()}IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM SandVAT WHERE {GetBranchCond()}IS_Deleted=0 AND id>{Code} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM SandVAT WHERE {GetBranchCond()}IS_Deleted=0 AND id<{Code} ORDER BY id DESC");
        }

        #endregion

        #region Other Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة
        }

        private void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmVATClients();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
                if (frm.IsDone)
                    LoadClients();
            }
            catch { }
        }

        #endregion
    }

    #region Models

    public class VATVoucherSearchModel
    {
        public string id      { get; set; }
        public string date    { get; set; }
        public double val     { get; set; }
        public string empName { get; set; }
    }

    #endregion
}