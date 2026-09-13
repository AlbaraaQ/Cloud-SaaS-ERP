using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using DevExpress.XtraReports.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSandVAT : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        private double VAT;
        private string EntryGlobalID;
        private string GlobalID;
        public string SrchName;
        private int ReceiptType;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<VATReceiptItem> _searchItems;
        //منع الأحداث من العمل قبل اكتمال تحميل النافذة
        private bool _isLoaded;

        #endregion

        #region Constructor

        public frmSandVAT()
        {
            

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code = -1;
            VAT = 0.0;
            EntryGlobalID = "-1";
            GlobalID = "";
            SrchName = "";
            ReceiptType = 9;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            InitializeComponent();
            _searchItems = new ObservableCollection<VATReceiptItem>();
           
            dgvSrch.ItemsSource = _searchItems;
            _isLoaded = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;
                txtToDate.DateTime = DateTime.Now;

                LoadEmps();
                LoadCostCenters();
                LoadAccounts();
                LoadSales();
                LoadSuppliers();
                LoadBranches();
                LoadPrintSettings();
                RestorePreviousReceipt();

                VAT = GetVATRate();
                lblVat.Text = $"💹 ضريبة {VAT}%";

                txtNo.Text = GetNextReceiptNo().ToString();
                Editereciept.Text = MainClass.UserName;
                Editereciept.Foreground = System.Windows.Media.Brushes.Red;

                _isLoaded = true;
                rdCash.IsChecked = true;
                LoadCashSources();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear / Reset

        private void CLR()
        {
            try
            {
                txtNo.Text = GetNextReceiptNo().ToString();
                Code = -1;
                txtNetVal.Text = "";
                txtVal.Text = "";
                txtRefNo.Text = "";
                txtVAT.Text = "";
                txtNotes.Text = "";
                txtCheckNo.Text = "";
                txtBankCheck.Text = "";
                GlobalID = "";
                EntryGlobalID = "-1";
                SrchName = "";

                cmbAccounts.SelectedIndex = -1;
                cmbSuppliers.SelectedIndex = -1;
                cmbDepositfrom.SelectedIndex = -1;
                cmbSalesMan.SelectedIndex = -1;
                cmbCostCenter.SelectedIndex = -1;
                cmbCheckType.SelectedIndex = -1;

                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;

                rdCash.IsChecked = true;
                Panel1.Visibility = Visibility.Collapsed;

                ckTotalPeriod.IsChecked = true;
                chkAllSuppliers.IsChecked = false;
                _searchItems.Clear();
                txtRefNoSearch.Text = "";

                if (cmbBranches.Items.Count > 0 && MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;

                Editereciept.Text = MainClass.UserName;
                Editereciept.Foreground = System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

        private int GetNextReceiptNo()
        {
            try
            {
                EnsureOpen(conn1);
                string sql = $@"SELECT ISNULL(MAX(ReceiptNo), 0) + 1
                                FROM Receipts
                                WHERE ReceiptType={ReceiptType}
                                  AND BranchID={MainClass.BranchNo}";
                var result = new SqlCommand(sql, conn1).ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 1;
            }
            catch { return 1; }
            finally { EnsureClose(conn1); }
        }

        private double GetVATRate()
        {
            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand("SELECT ISNULL(VATRate, 15) FROM Settings", conn);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToDouble(result)
                    : 15.0;
            }
            catch { return 15.0; }
            finally { EnsureClose(conn); }
        }

        public void LoadEmps()
        {
            try
            {
                EnsureOpen(conn1);
                var adapter = new SqlDataAdapter(
                    $@"SELECT id, name FROM Employees
                       INNER JOIN EmpBranches ON Employees.id = EmpBranches.emp
                       WHERE EmpBranches.branch={MainClass.BranchNo} AND IS_Deleted=0 ORDER BY id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                // مجرد تحميل دون استخدام في هذه الواجهة
            }
            catch { }
            finally { EnsureClose(conn1); }
        }

        private void LoadSuppliers()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Suppliers WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSuppliers.ItemsSource = dt.DefaultView;
                cmbSuppliers.DisplayMemberPath = "name";
                cmbSuppliers.SelectedValuePath = "id";
                cmbSuppliers.SelectedIndex = -1;

                var dt2 = dt.Copy();
                cmbSuppliersSearch.ItemsSource = dt2.DefaultView;
                cmbSuppliersSearch.DisplayMemberPath = "name";
                cmbSuppliersSearch.SelectedValuePath = "id";
                cmbSuppliersSearch.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الموردين: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadSales()
        {
            try
            {
                EnsureOpen(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMan.ItemsSource = dt.DefaultView;
                cmbSalesMan.DisplayMemberPath = "name";
                cmbSalesMan.SelectedValuePath = "id";
                cmbSalesMan.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل المندوبين: " + ex.Message); }
            finally { EnsureClose(conn1); }
        }

        private void LoadAccounts()
        {
            try
            {
                EnsureOpen(conn);
                string branchCond = MainClass.BranchNo != -1
                    ? $" AND Branch={MainClass.BranchNo}" : "";
                var adapter = new SqlDataAdapter(
                    $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {branchCond}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الحسابات: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadCostCenters()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center WHERE type=2 ORDER BY code", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل مراكز التكلفة: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الفروع: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadCashSources()
        {
            try
            {
                EnsureOpen(conn1);
                string sql = $@"SELECT id, name FROM Stocks
                                INNER JOIN Stock_Emps ON Stocks.id = Stock_Emps.stock_id
                                WHERE emp_id={MainClass.EmpNo}
                                  AND IS_Deleted=0 AND status<>2 ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositfrom.ItemsSource = dt.DefaultView;
                cmbDepositfrom.DisplayMemberPath = "name";
                cmbDepositfrom.SelectedValuePath = "id";
                cmbDepositfrom.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الخزن: " + ex.Message); }
            finally { EnsureClose(conn1); }
        }

        private void LoadBankSources()
        {
            try
            {
                EnsureOpen(conn1);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositfrom.ItemsSource = dt.DefaultView;
                cmbDepositfrom.DisplayMemberPath = "name";
                cmbDepositfrom.SelectedValuePath = "id";
                cmbDepositfrom.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل البنوك: " + ex.Message); }
            finally { EnsureClose(conn1); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT * FROM SettingPrint WHERE Inv_Id=10", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pt)) PrintType = pt;
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"].ToString();
                    if (string.IsNullOrWhiteSpace(defPrinter)) defPrinter = MainClass.ReportsPrinter;
                    if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pn)) PrintNo = pn;
                    RptName = dt.Rows[0]["RptName"].ToString();
                    RptUrl = System.IO.Path.GetDirectoryName(dt.Rows[0]["RptUrl"].ToString());
                    if (string.IsNullOrWhiteSpace(RptUrl) || !Directory.Exists(RptUrl))
                        RptUrl = MainClass.ReportsPath;
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                    defPrinter = MainClass.ReportsPrinter;
                    RptName = "RptBond.repx";
                }
            }
            catch { RptName = "RptBond.repx"; }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Restore Previous

        private void RestorePreviousReceipt()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SandVAT WHERE branch={MainClass.BranchNo}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        int receiptNo = Convert.ToInt32(row["id"]);
                        new SqlCommand($"DELETE FROM SandVAT WHERE id={receiptNo}", conn)
                            .ExecuteNonQuery();
                    }
                    catch { }
                }
            }
            catch (Exception ex) { SetStatus("خطأ في الاستعادة: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Supplier Name Helper

        private string GetSupplierName(int id)
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Suppliers WHERE IS_Deleted=0 AND id={id}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Search

        private void Search()
        {
            try
            {
                _searchItems.Clear();

                string condition = $" Receipts.ReceiptType={ReceiptType} AND ";
                if (MainClass.BranchNo != -1)
                    condition += $" Receipts.BranchID={MainClass.BranchNo} AND ";

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    condition += $" Receipts.ReceiptNo={txtSrchNo.Text} AND ";

                if (ckTotalPeriod.IsChecked != true)
                    condition += " ReceiptDate BETWEEN @StartDate AND @EndDate AND ";

                if (!string.IsNullOrWhiteSpace(txtRefNoSearch.Text))
                    condition += $" Receipts.ReffNo=N'{txtRefNoSearch.Text}' AND ";

                if (chkAllSuppliers.IsChecked != true
                    && cmbSuppliersSearch.SelectedIndex != -1
                    && cmbSuppliersSearch.SelectedValue != null)
                    condition += $" Receipts.ClientID={cmbSuppliersSearch.SelectedValue} AND ";

                string sql = $@"SELECT Receipts.GlobalID, Receipts.ReceiptNo,
                                       Receipts.ReceiptDate, Receipts.Payment,
                                       Receipts.ClientID
                                FROM Receipts
                                WHERE {condition} Receipts.ISDeleted=0
                                ORDER BY Receipts.ReceiptNo DESC";

                EnsureOpen(conn);
                var cmd = new SqlCommand(sql, conn);

                if (ckTotalPeriod.IsChecked != true)
                {
                    cmd.Parameters.AddWithValue("@StartDate",
    txtFromDate.DateTime != DateTime.MinValue
        ? txtFromDate.DateTime
        : DateTime.Today);

                    cmd.Parameters.AddWithValue("@EndDate",
                        txtToDate.DateTime != DateTime.MinValue
                            ? txtToDate.DateTime
                            : DateTime.Today);
                }

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int suppId = row["ClientID"] != DBNull.Value ? Convert.ToInt32(row["ClientID"]) : 0;
                    _searchItems.Add(new VATReceiptItem
                    {
                        GlobalId = row["GlobalID"].ToString(),
                        ReceiptNo = row["ReceiptNo"].ToString(),
                        ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString(),
                        Payment = row["Payment"].ToString(),
                        SupplierName = GetSupplierName(suppId)
                    });
                }

                SetStatus($"تم العثور على {_searchItems.Count} نتيجة");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var reader = new SqlCommand(sqlQuery, conn).ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) { reader.Close(); SetStatus("لا توجد سجلات"); return; }

            reader.Read();
            CLR();

            Code = Convert.ToInt32(reader["ReceiptNo"]);
            txtNo.Text = Code.ToString();
            EntryGlobalID = reader["EntryGlobalID"].ToString();
            GlobalID = reader["GlobalID"].ToString();
            txtVal.Text = reader["Payment"].ToString();
            txtNotes.Text = reader["Notes"].ToString();

            if (reader["Receiptdate"] != DBNull.Value)
                txtDate.DateTime = Convert.ToDateTime(reader["Receiptdate"]);

            if (reader["DebitAcc"] != DBNull.Value)
                cmbAccounts.SelectedValue = reader["DebitAcc"];

            if (reader["ClientID"] != DBNull.Value)
                cmbSuppliers.SelectedValue = reader["ClientID"];

            // رقم المرجع
            if (reader["ReffNo"] != DBNull.Value
                && reader["ReffNo"].ToString() != ""
                && reader["ReffNo"].ToString() != "-1")
                txtRefNo.Text = reader["ReffNo"].ToString();

            if (reader["Reffdate"] != DBNull.Value)
                txtRefDate.DateTime = Convert.ToDateTime(reader["Reffdate"]);

            // نوع الدفع
            int payType = reader["Paymenttype"] != DBNull.Value
                ? Convert.ToInt32(reader["Paymenttype"]) : 1;

            if (payType == 1)
            {
                rdCash.IsChecked = true;
                Panel1.Visibility = Visibility.Collapsed;
            }
            else
            {
                rdCheck.IsChecked = true;
                Panel1.Visibility = Visibility.Visible;

                txtCheckNo.Text = reader["CheckNo"] != DBNull.Value ? reader["CheckNo"].ToString() : "";
                txtBankCheck.Text = reader["Checkbank"] != DBNull.Value ? reader["Checkbank"].ToString() : "";

                if (reader["CheckDate"] != DBNull.Value)
                    txtCheckDate.DateTime = Convert.ToDateTime(reader["CheckDate"]);

                if (reader["CheckState"] != DBNull.Value)
                    cmbCheckType.SelectedIndex = Convert.ToInt32(reader["CheckState"]) - 1;
            }

            if (reader["TreasuryID"] != DBNull.Value)
                cmbDepositfrom.SelectedValue = reader["TreasuryID"];

            if (reader["SalesManID"] != DBNull.Value)
                cmbSalesMan.SelectedValue = reader["SalesManID"];

            // مركز التكلفة
            if (reader["Cccode"] != DBNull.Value
                && reader["Cccode"].ToString() != "-1"
                && reader["Cccode"].ToString() != "")
                cmbCostCenter.SelectedValue = reader["Cccode"];

            if (reader["BranchID"] != DBNull.Value)
                cmbBranches.SelectedValue = reader["BranchID"];

            int empId = reader["EmpId"] != DBNull.Value ? Convert.ToInt32(reader["EmpId"]) : 0;
            reader.Close();

            LoadEditorName(empId);
            SetStatus($"السند رقم {Code}");
            TabControl1.SelectedIndex = 0;
        }

        private void LoadEditorName(int empId)
        {
            try
            {
                EnsureOpen(conn);
                var r = new SqlCommand($"SELECT username FROM users WHERE emp={empId}", conn)
                    .ExecuteReader();
                if (r.HasRows)
                {
                    r.Read();
                    Editereciept.Text = r["username"].ToString();
                    Editereciept.Foreground = System.Windows.Media.Brushes.Green;
                }
                r.Close();
            }
            catch { }
            finally { EnsureClose(conn); }
        }

        private string GetBranchCondition() =>
            MainClass.BranchNo != -1 ? $"BranchID={MainClass.BranchNo} AND " : "";

        #endregion

        #region Button Events - Navigation

        private void btnNew_Click(object sender, RoutedEventArgs e) { CLR(); SetStatus("سند جديد"); }

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Receipts WHERE {GetBranchCondition()} ReceiptType={ReceiptType} AND ISDeleted=0 ORDER BY ReceiptNo ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($"SELECT TOP 1 * FROM Receipts WHERE {GetBranchCondition()} ReceiptType={ReceiptType} AND ISDeleted=0 AND ReceiptNo<{no} ORDER BY ReceiptNo DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtNo.Text, out int no)) return;
            Navigate($"SELECT TOP 1 * FROM Receipts WHERE {GetBranchCondition()} ReceiptType={ReceiptType} AND ISDeleted=0 AND ReceiptNo>{no} ORDER BY ReceiptNo ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Receipts WHERE {GetBranchCondition()} ReceiptType={ReceiptType} AND ISDeleted=0 ORDER BY ReceiptNo DESC");

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Button Events - Operations

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // ── التحقق ──
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("ادخل القيمة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtVal.Focus(); return;
            }
            if (!double.TryParse(txtVal.Text, out double payVal) || payVal <= 0)
            {
                DXMessageBox.Show("القيمة غير صحيحة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtVal.Focus(); return;
            }
            if (cmbAccounts.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار الحساب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbAccounts.Focus(); return;
            }
            if (cmbSuppliers.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المورد", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSuppliers.Focus(); return;
            }
            if (cmbDepositfrom.SelectedIndex == -1)
            {
                string msg = rdCash.IsChecked == true ? "يجب اختيار الخزنة" : "يجب اختيار البنك";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbDepositfrom.Focus(); return;
            }

            var confirm = DXMessageBox.Show("هل أنت متأكد من حفظ السند؟", "تأكيد",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                EnsureOpen(conn1);
                bool isNew = (Code == -1);

                if (isNew) txtNo.Text = GetNextReceiptNo().ToString();
                if (!int.TryParse(txtNo.Text, out int receiptNo)) receiptNo = 1;
                Code = receiptNo;

                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                    txtNotes.Text = $"سند صرف ضريبة رقم: {txtNo.Text} - سداد دفعة من حساب: {(cmbAccounts.SelectedItem as DataRowView)?["AName"]}";

                int paymentType = rdCash.IsChecked == true ? 1 : 2;
                DateTime receiptDate = txtDate.DateTime != DateTime.MinValue
    ? txtDate.DateTime
    : DateTime.Now;

                int salesManId = -1;
                if (cmbSalesMan.SelectedValue != null)
                    int.TryParse(cmbSalesMan.SelectedValue.ToString(), out salesManId);

                int costCenterCode = -1;
                if (cmbCostCenter.SelectedValue != null)
                    int.TryParse(cmbCostCenter.SelectedValue.ToString(), out costCenterCode);

                double.TryParse(txtVAT.Text, out double vatVal);
                double.TryParse(txtNetVal.Text, out double netVal);

                // حساب كود الخزنة/البنك
                int depositCode = 0;
                try
                {
                    EnsureOpen(conn);
                    string depositName = (cmbDepositfrom.SelectedItem as DataRowView)?["name"]?.ToString() ?? "";
                    string branchCond = MainClass.BranchNo != -1 ? $" AND Branch={MainClass.BranchNo}" : "";
                    var depAdapter = new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index WHERE Type=2 {branchCond} AND AName=N'{depositName}'",
                        conn);
                    var depDt = new DataTable();
                    depAdapter.Fill(depDt);
                    if (depDt.Rows.Count > 0)
                        int.TryParse(depDt.Rows[0]["Code"].ToString(), out depositCode);
                }
                catch { }
                finally { EnsureClose(conn); }

                var transaction = conn1.BeginTransaction();
                try
                {
                    string upsertSql = isNew
                        ? @"INSERT INTO Receipts
                            (ReceiptNo,BranchID,ReceiptType,State,Payment,VAT,NetVal,
                             ReceiptDate,ReffNo,Reffdate,ISDeleted,Notes,EmpId,
                             CreditAcc,DebitAcc,TreasuryID,SalesManID,PaymentType,
                             CheckNo,CheckDate,Checkbank,CheckState,CCcode,ClientID)
                            VALUES
                            (@ReceiptNo,@BranchID,@ReceiptType,1,@Payment,@VAT,@NetVal,
                             @ReceiptDate,@ReffNo,@Reffdate,0,@Notes,@EmpId,
                             @CreditAcc,@DebitAcc,@TreasuryID,@SalesManID,@PaymentType,
                             @CheckNo,@CheckDate,@Checkbank,@CheckState,@CCcode,@ClientID)"
                        : @"UPDATE Receipts SET
                            Payment=@Payment,VAT=@VAT,NetVal=@NetVal,
                            ReceiptDate=@ReceiptDate,ReffNo=@ReffNo,Reffdate=@Reffdate,
                            Notes=@Notes,CreditAcc=@CreditAcc,DebitAcc=@DebitAcc,
                            TreasuryID=@TreasuryID,SalesManID=@SalesManID,
                            PaymentType=@PaymentType,CheckNo=@CheckNo,CheckDate=@CheckDate,
                            Checkbank=@Checkbank,CheckState=@CheckState,CCcode=@CCcode,
                            ClientID=@ClientID
                            WHERE ReceiptNo=@ReceiptNo AND BranchID=@BranchID";

                    var cmd = new SqlCommand(upsertSql, conn1, transaction);
                    cmd.Parameters.AddWithValue("@ReceiptNo", Code);
                    cmd.Parameters.AddWithValue("@BranchID", MainClass.BranchNo);
                    cmd.Parameters.AddWithValue("@ReceiptType", ReceiptType);
                    cmd.Parameters.AddWithValue("@Payment", payVal);
                    cmd.Parameters.AddWithValue("@VAT", vatVal);
                    cmd.Parameters.AddWithValue("@NetVal", netVal);
                    cmd.Parameters.AddWithValue("@ReceiptDate", receiptDate);
                    cmd.Parameters.AddWithValue("@ReffNo", string.IsNullOrWhiteSpace(txtRefNo.Text) ? "-1" : txtRefNo.Text);
                    cmd.Parameters.AddWithValue("@Reffdate",
    txtRefDate.DateTime != DateTime.MinValue
        ? (object)txtRefDate.DateTime
        : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", txtNotes.Text);
                    cmd.Parameters.AddWithValue("@EmpId", MainClass.EmpNo);
                    cmd.Parameters.AddWithValue("@CreditAcc", depositCode);
                    cmd.Parameters.AddWithValue("@DebitAcc", cmbAccounts.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TreasuryID", cmbDepositfrom.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SalesManID", salesManId == -1 ? (object)DBNull.Value : salesManId);
                    cmd.Parameters.AddWithValue("@PaymentType", paymentType);
                    cmd.Parameters.AddWithValue("@CCcode", costCenterCode == -1 ? (object)DBNull.Value : costCenterCode);
                    cmd.Parameters.AddWithValue("@ClientID", cmbSuppliers.SelectedValue ?? DBNull.Value);

                    if (paymentType == 2)
                    {
                        cmd.Parameters.AddWithValue("@CheckNo", txtCheckNo.Text);
                        cmd.Parameters.AddWithValue("@CheckDate",
    txtCheckDate.DateTime != DateTime.MinValue
        ? (object)txtCheckDate.DateTime
        : DBNull.Value);
                        cmd.Parameters.AddWithValue("@Checkbank", txtBankCheck.Text);
                        cmd.Parameters.AddWithValue("@CheckState", cmbCheckType.SelectedIndex + 1);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@CheckNo", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CheckDate", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Checkbank", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CheckState", DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    SetStatus(isNew ? $"تم حفظ سند ضريبي رقم {Code}" : $"تم تعديل سند ضريبي رقم {Code}");

                    var savedMsg = new frmSavedMsg();
                    savedMsg.ShowDialog();

                    if (savedMsg.Pressed == 1) { _searchItems.Clear(); CLR(); }
                    else if (savedMsg.Pressed == 3) { this.Close(); }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاتصال: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn1); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر سنداً ليتم حذفه", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var confirm = DXMessageBox.Show("هل أنت متأكد من حذف السند؟", "تأكيد الحذف",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                EnsureOpen(conn);
                int affected = new SqlCommand(
                    $"UPDATE Receipts SET ISDeleted=1 WHERE ReceiptNo={Code} AND BranchID={MainClass.BranchNo}",
                    conn).ExecuteNonQuery();

                if (affected > 0)
                {
                    DXMessageBox.Show("تم الحذف بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    _searchItems.Clear(); CLR();
                }
                else
                    DXMessageBox.Show("خطأ أثناء الحذف", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReport(1);

        private void btnView_Click(object sender, RoutedEventArgs e) => PrintReport(2);

        private void BtnImportDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF Files (*.pdf)|*.pdf",
                Title = "تحديد الملفات",
                Multiselect = true
            };
            string dataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            if (dialog.ShowDialog() == true)
                SetStatus($"تم اختيار {dialog.FileNames.Length} ملف");
        }

        private void Btnshowdocument_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("يجب حفظ السند أولاً", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information); return;
            }
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT FileName, FileUrl FROM Documents WHERE GlobalID=N'{GlobalID}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0) SetStatus($"يوجد {dt.Rows.Count} مستند مرفق");
                else DXMessageBox.Show("لا توجد مستندات مرفقة", "تنبيه",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region RadioButton Events

        private void rdCash_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (Panel1 != null) Panel1.Visibility = Visibility.Collapsed;
            LoadCashSources();
        }

        private void rdCheck_Checked(object sender, RoutedEventArgs e)
        {
            if (Panel1 != null) Panel1.Visibility = Visibility.Visible;
            LoadBankSources();
        }

        #endregion

        #region ComboBox Events

        private void cmbAccounts_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة البحث عن الحسابات
             frmAccountSrch frm = new frmAccountSrch();
             frm.ShowDialog();
             if (frm.Code > -1) cmbAccounts.SelectedValue = frm.Code;
           // DXMessageBox.Show("افتح نافذة البحث عن الحسابات هنا", "بحث",
           //                 MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void cmbSuppliers_Click(object sender, RoutedEventArgs e)
        {
             frmSuppliersSrch frm = new frmSuppliersSrch();
             frm.ShowDialog();
             if (frm.SelectedSupplierNo != null)
             {
                 LoadSuppliers();
                 cmbSuppliers.SelectedValue = frm.SelectedSupplierNo;
             }
           // DXMessageBox.Show("افتح نافذة البحث عن الموردين هنا", "بحث",
           //                 MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSearchAcc_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة البحث عن الموردين وتعيين القيمة في cmbSuppliers
            DXMessageBox.Show("افتح نافذة البحث عن الموردين هنا", "بحث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void cmbSuppliersSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) { e.Handled = true; SrchByNameSupplier(); }
        }

        private void SrchByNameSupplier()
        {
            try
            {
                string searchName = (cmbSuppliersSearch.SelectedItem as DataRowView)?["name"]?.ToString()
                                    ?? cmbSuppliersSearch.Text;
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Suppliers WHERE IS_Deleted=0 AND name=N'{searchName}'",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                if (dt.Rows.Count > 0)
                    cmbSuppliersSearch.SelectedValue = dt.Rows[0]["id"];
                else
                    OpenSupplierSearch();
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
        }

        private void OpenSupplierSearch()
        {
            DXMessageBox.Show("افتح نافذة البحث عن الموردين هنا", "بحث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region TextBox Events

        private void txtVal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtVal.Text))
            {
                try
                {
                    if (double.TryParse(txtVal.Text, out double val))
                    {
                        double vatAmount = Math.Round(val * VAT / 100.0, 2);
                        double netAmount = Math.Round(val + vatAmount, 2);
                        txtVAT.Text = vatAmount.ToString("N2");
                        txtNetVal.Text = netAmount.ToString("N2");
                    }
                }
                catch { }
            }
            else
            {
                txtVAT.Text = "";
                txtNetVal.Text = "";
            }
        }

        private void txtSrchNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (!char.IsDigit(c)) { e.Handled = true; return; }
        }

        #endregion

        #region CheckBox Events

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allChecked;
            txtToDate.IsEnabled = !allChecked;
        }

        private void chkAllSuppliers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbSuppliersSearch != null)
                cmbSuppliersSearch.IsEnabled = chkAllSuppliers.IsChecked != true;
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is VATReceiptItem selected)
            {
                GlobalID = selected.GlobalId;
                Navigate($"SELECT * FROM Receipts WHERE GlobalID=N'{GlobalID}'");
            }
        }

        #endregion

        #region Print

        private void PrintReport(int printMode)
        {
            if (string.IsNullOrWhiteSpace(txtVal.Text))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptName))
                RptName = "RptBond.repx";

            string fullPath = Path.Combine(RptUrl, RptName);

            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetStatus(printMode == 1 ? "جارِ الطباعة..." : "جارِ المعاينة...");

                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportData();

                // ربط subreport الهيدر
                string headerPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport = XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;
                    var headerSub = report.FindControl("headerRpt", ignoreCase: true)
                        as XRSubreport;
                    if (headerSub != null)
                        headerSub.ReportSource = headerReport;
                }

                // ربط subreport الفوتر
                string footerPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerReport = XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;
                    var footerSub = report.FindControl("footerRpt", ignoreCase: true)
                        as XRSubreport;
                    if (footerSub != null)
                        footerSub.ReportSource = footerReport;
                }

                // تعيين الطابعة
                if (!string.IsNullOrWhiteSpace(defPrinter))
                    report.PrinterName = defPrinter;

                int copies = PrintNo > 0 ? PrintNo : 1;

                for (int i = 0; i < copies; i++)
                {
                    if (printMode == 1)
                        report.Print();
                    else
                        report.ShowPreviewDialog();
                }

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الطباعة:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetStatus("");
            }
        }

        private DataSet BuildReportData()
        {
            #region Foundation Info

            string address = "";
            string telephone = "";
            string mobile = "";
            string foundation = "";
            string field = "";
            string vATNo = "";

            try
            {
                EnsureOpen(conn);
                var foundDt = new DataTable();
                new SqlDataAdapter("SELECT * FROM Foundation", conn).Fill(foundDt);

                if (foundDt.Rows.Count > 0)
                {
                    address = foundDt.Rows[0]["Address"]?.ToString() ?? "";
                    telephone = foundDt.Rows[0]["Tel"]?.ToString() ?? "";
                    mobile = foundDt.Rows[0]["Mobile"]?.ToString() ?? "";
                    foundation = foundDt.Rows[0]["nameA"]?.ToString() ?? "";
                    field = foundDt.Rows[0]["FieldA"]?.ToString() ?? "";
                    vATNo = foundDt.Rows[0]["tax_no"]?.ToString() ?? "";
                }
            }
            catch { }
            finally { EnsureClose(conn); }

            #endregion

            #region Bond Type

            string bondType = rdCash.IsChecked == true ? "نقدي" : "تحويل بنكي";

            #endregion

            #region Arabic Letter (تفقيط)

            double netValDouble = 0;
            double.TryParse(txtNetVal.Text, out netValDouble);

            string arabicLetter = "";
            try
            {
                int currencyCode = Common.GetCurrencytosand(13);
                arabicLetter = InvoiceOper.ToArabicLetter(
                    netValDouble, (Currency)currencyCode);
            }
            catch
            {
                arabicLetter = netValDouble.ToString("N2");
            }

            #endregion

            #region Supplier Name

            string supplierName = "";
            try
            {
                if (cmbSuppliers.SelectedValue != null)
                {
                    EnsureOpen(conn);
                    var suppDt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT name FROM Suppliers " +
                        $"WHERE IS_Deleted=0 AND id={cmbSuppliers.SelectedValue}",
                        conn).Fill(suppDt);

                    if (suppDt.Rows.Count > 0)
                        supplierName = suppDt.Rows[0]["name"]?.ToString() ?? "";
                }
            }
            catch { }
            finally { EnsureClose(conn); }

            #endregion

            #region Deposit From Account Code

            string depositFromCode = "";
            try
            {
                string depositName = GetComboDisplayText(cmbDepositfrom, "name");
                if (!string.IsNullOrWhiteSpace(depositName))
                {
                    EnsureOpen(conn);
                    string branchCond = MainClass.BranchNo != -1
                        ? $" AND Branch={MainClass.BranchNo}" : "";
                    var depDt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT Code FROM Accounts_Index " +
                        $"WHERE Type=2 {branchCond} AND AName=N'{depositName}'",
                        conn).Fill(depDt);

                    if (depDt.Rows.Count > 0)
                        depositFromCode = depDt.Rows[0]["Code"]?.ToString() ?? "";
                }
            }
            catch { }
            finally { EnsureClose(conn); }

            #endregion

            #region Dates

            DateTime receiptDate = txtDate.DateTime != DateTime.MinValue
                ? txtDate.DateTime : DateTime.Now;

            DateTime refDate = txtRefDate.DateTime != DateTime.MinValue
                ? txtRefDate.DateTime : DateTime.Now;

            DateTime checkDate = DateTime.Now;
            try
            {
                if (txtCheckDate.DateTime != DateTime.MinValue)
                    checkDate = txtCheckDate.DateTime;
            }
            catch { }

            #endregion

            #region Report Paths

            string headerPath = !string.IsNullOrWhiteSpace(RptUrl) &&
                                File.Exists(Path.Combine(RptUrl, "header.repx"))
                ? Path.Combine(RptUrl, "header.repx") : "";

            string footerPath = !string.IsNullOrWhiteSpace(RptUrl) &&
                                File.Exists(Path.Combine(RptUrl, "footer.repx"))
                ? Path.Combine(RptUrl, "footer.repx") : "";

            string stampPath = !string.IsNullOrWhiteSpace(RptUrl) &&
                                File.Exists(Path.Combine(RptUrl, "stamp.png"))
                ? Path.Combine(RptUrl, "stamp.png") : "";

            #endregion

            #region Notes & Description
            // كلاس Bond لا يحتوي على Checkbank أو CheckState
            // لذلك نضمّن بيانات الشيك داخل Description
            string notesText = txtNotes.Text ?? "";
            string descriptionText = "";

            if (rdCheck.IsChecked == true)
            {
                string checkBank = txtBankCheck.Text ?? "";
                string checkState = GetComboDisplayText(cmbCheckType) ?? "";

                if (!string.IsNullOrWhiteSpace(checkBank))
                    descriptionText += $"مسحوب على: {checkBank}";

                if (!string.IsNullOrWhiteSpace(checkState))
                {
                    if (!string.IsNullOrWhiteSpace(descriptionText))
                        descriptionText += " | ";
                    descriptionText += $"حالة الشيك: {checkState}";
                }
            }

            #endregion

            #region Build Bond Object

            var bond = new Bond
            {
                // ── رقم السند والتواريخ ──
                BondNo = txtNo.Text ?? "",
                BondDate = receiptDate.ToShortDateString(),
                BondTime = receiptDate.ToString("HH:mm"),
                PrintDate = DateTime.Now.ToShortDateString(),

                // ── نوع السند واسمه ──
                BondName = "سند صرف ضريبة قيمة مضافة",
                BondType = bondType,

                // ── القيم المالية ──
                BondVal = txtVal.Text ?? "",
                taxVal = txtVAT.Text ?? "",
                netVal = txtNetVal.Text ?? "",
                vatPerc = VAT.ToString("N2"),
                ArabicLetter = arabicLetter,

                // ── الحسابات ──
                PayToCode = cmbAccounts.SelectedValue?.ToString() ?? "",
                PayTo = GetComboDisplayText(cmbAccounts, "AName", "name"),
                PayFromCode = depositFromCode,
                Payfrom = GetComboDisplayText(cmbDepositfrom, "name"),

                // ── المرجع ──
                reffNo = string.IsNullOrWhiteSpace(txtRefNo.Text)
                             ? "" : txtRefNo.Text,
                reffDate = txtRefDate.DateTime != DateTime.MinValue
                             ? refDate.ToShortDateString() : "",

                // ── الشيك ──
                // Bond يحتوي على CheckNo و CheckDate فقط
                CheckNo = rdCheck.IsChecked == true
                              ? (txtCheckNo.Text ?? "") : "",
                CheckDate = rdCheck.IsChecked == true
                              ? checkDate.ToShortDateString() : "",

                // ── بيانات الشيك الإضافية في Description ──
                // لأن Bond لا يحتوي على Checkbank أو CheckState
                Description = descriptionText,

                // ── ملاحظات ──
                Note = notesText,
                Notes = notesText,

                // ── الأطراف ──
                supplier = supplierName,
                SalesMan = GetComboDisplayText(cmbSalesMan, "name"),
                CostCenter = GetComboDisplayText(cmbCostCenter, "name"),

                // ── المستخدم والفرع ──
                User = Editereciept.Text ?? MainClass.UserName ?? "",
                Branch = MainClass.BranchName ?? "",

                // ── حالة الحساب ──
                AccountStatus = "",
                CurrentBalance = "",

                // ── بيانات المنشأة ──
                Address = address,
                Telephone = telephone,
                Mobile = mobile,
                VATNo = vATNo,
                Foundation = foundation,
                Field = field,

                // ── مسارات التقرير ──
                Logo = "",
                Header = headerPath,
                Footer = footerPath,
                Stamp = stampPath,

                // ── تاريخ (شهر / سنة) ──
                Month = receiptDate.Month.ToString(),
                Year = receiptDate.Year.ToString(),

                // ── فترة البحث ──
                FromDate = txtFromDate.DateTime != DateTime.MinValue
                             ? txtFromDate.DateTime.ToShortDateString()
                             : DateTime.Now.ToShortDateString(),
                ToDate = txtToDate.DateTime != DateTime.MinValue
                             ? txtToDate.DateTime.ToShortDateString()
                             : DateTime.Now.ToShortDateString()
            };

            #endregion

            #region Build DataSet

            var list = new List<Bond> { bond };
            var ds = new DataSet("Name");
            var table = global::UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;

            #endregion
        }

        /// <summary>
        /// يجلب النص المعروض من ComboBox سواء كان DataRowView أو ComboBoxItem
        /// </summary>
        private string GetComboDisplayText(ComboBox combo,
    params string[] columnNames)
        {
            if (combo == null) return "";

            if (combo.SelectedItem is DataRowView drv)
            {
                foreach (string col in columnNames)
                {
                    if (drv.DataView.Table.Columns.Contains(col) &&
                        drv[col] != DBNull.Value)
                        return drv[col]?.ToString() ?? "";
                }
            }

            if (combo.SelectedItem is ComboBoxItem ci)
                return ci.Content?.ToString() ?? "";

            return combo.Text ?? "";
        }

        #endregion

        #region Helpers
        
        private void EnsureOpen(SqlConnection c)
        { if (c.State != ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != ConnectionState.Closed) c.Close(); }

        private void SetStatus(string msg) { /* يمكن إضافة StatusBar لاحقاً */ }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
            EnsureClose(conn1);
        }

        #endregion
    }
}