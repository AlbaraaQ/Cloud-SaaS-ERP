using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using log4net;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSalaryPay : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int    Code        = -1;
        private int    Emp_No     = -1;
        private bool   _loaded;
        private Print  print;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        private string EntryGlobalID_Field = "-1";
        private string GlobalID            = "";

        private ObservableCollection<SalaryPayRow> PayList;

        #endregion

        #region Constructor

        public frmSalaryPay()
        {
            InitializeComponent();
            conn   = MainClass.ConnObj();
            conn1  = MainClass.ConnObj();
            print  = new Print(10);

            PayList = new ObservableCollection<SalaryPayRow>();
            dgvSrch.ItemsSource = PayList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Today;
            txtToDate.DateTime   = DateTime.Today;
            txtDate.DateTime     = DateTime.Today;

            // تعبئة السنوات
            for (int y = 2000; y <= 2100; y++)
                cmbYear.Items.Add(y.ToString());

            // تعبئة الشهور
            for (int m = 1; m <= 12; m++)
                cmbMonth.Items.Add(m.ToString());

            CLR();
            LoadBranches();
            LoadEmps(-1);
            LoadEmpSrch();
            LoadTreasuries();

            if (MainClass.BranchNo != -1)
                cmbBranch.SelectedValue = MainClass.BranchNo;

            LoadEmp(MainClass.EmpNo);
        }

        #endregion

        #region Load Helpers

        private void CLR()
        {
            cmbEmp.SelectedIndex = -1;
            txtTotSalary.Text    = "";
            txtSalaryAdd.Text    = "";
            txtSalarySub.Text    = "";
            txtNet.Text          = "";
            txtEntryGlobalId.Text = "";
            TxtResGlobalID.Text  = "";
            txtNotes.Text        = "";
            PayList.Clear();
            LoadNxtNo();
            cmbEmp.Focus();
            Code = -1;

            if (cmbYear.Items.Count > 0)
            {
                string yearStr = DateTime.Now.Year.ToString();
                cmbYear.SelectedItem = yearStr;
            }

            txtDate.DateTime = DateTime.Today;

            if (cmbMonth.Items.Count > 0)
                cmbMonth.SelectedIndex = DateTime.Now.Month - 1;

            TxtHous.Text    = "";
            TxtTravel.Text  = "";
        }

        private void LoadNxtNo()
        {
            try
            {
                int nextNo = 1;
                var adapter = new SqlDataAdapter(
                    "SELECT MAX(id) FROM SalaryPay", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dt.Rows[0][0]?.ToString()))
                {
                    nextNo = Convert.ToInt32(dt.Rows[0][0]) + 1;
                }

                txtNo.Text = nextNo.ToString();
            }
            catch { /* تجاهل */ }
        }

        private void LoadDG(string cond)
        {
            PayList.Clear();
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT SalaryPay.id, Employees.name, salary_net, " +
                    "month, year, SalaryPay.date, SalaryPay.emp_resp " +
                    "FROM SalaryPay, Employees " +
                    "WHERE SalaryPay.emp=Employees.id AND " +
                    cond + " SalaryPay.IS_Deleted=0 ORDER BY id", conn);

                if (!string.IsNullOrEmpty(cond) && cond.Contains("@date"))
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        txtFromDate.DateTime.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value =
                        txtToDate.DateTime.ToShortDateString();
                }

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    string respName = "";
                    if (row["emp_resp"] != DBNull.Value &&
                        Convert.ToInt32(row["emp_resp"]) != -1)
                    {
                        var empAdapter = new SqlDataAdapter(
                            $"SELECT name FROM Employees WHERE id={row["emp_resp"]}",
                            conn);
                        var empDt = new DataTable();
                        empAdapter.Fill(empDt);
                        if (empDt.Rows.Count > 0)
                            respName = empDt.Rows[0]["name"]?.ToString() ?? "";
                    }

                    PayList.Add(new SalaryPayRow
                    {
                        PayId          = Convert.ToInt32(row["id"]),
                        EmpName        = row["name"]?.ToString() ?? "",
                        NetAmount      = Convert.ToDouble(row["salary_net"]),
                        Month          = row["month"]?.ToString() ?? "",
                        Year           = row["year"]?.ToString() ?? "",
                        PayDate        = Convert.ToDateTime(row["date"])
                                                .ToShortDateString(),
                        ResponsibleEmp = respName,
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadEmps(int branch)
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id",
                conn1);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbEmp.DisplayMemberPath = "name";
            cmbEmp.SelectedValuePath  = "id";
            cmbEmp.ItemsSource        = dt.DefaultView;
            cmbEmp.SelectedIndex      = -1;
        }

        public void LoadEmpSrch()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbEmpSrch.DisplayMemberPath = "name";
            cmbEmpSrch.SelectedValuePath  = "id";
            cmbEmpSrch.ItemsSource        = dt.DefaultView;
            cmbEmpSrch.SelectedIndex      = -1;
        }

        public void LoadBranches()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM Branches ORDER BY id", conn1);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbBranch.DisplayMemberPath = "name";
            cmbBranch.SelectedValuePath  = "id";
            cmbBranch.ItemsSource        = dt.DefaultView;
            cmbBranch.SelectedIndex      = -1;
        }

        private void LoadEmp(int empId)
        {
            if (empId == -1) return;
            var adapter = new SqlDataAdapter(
                $"SELECT name FROM Employees WHERE id={empId}", conn1);
            var dt = new DataTable();
            adapter.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                Emp_No = empId;
                txtEmp.Text = dt.Rows[0][0]?.ToString() ?? "";
            }
        }

        private void LoadTreasuries()
        {
            // يتم تحميل الصناديق عند اختيار طريقة الدفع
            if (rdCash.IsChecked == true)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND IS_Deleted=0 AND status<>2 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "name";
                cmbDepositTo.SelectedValuePath  = "id";
                cmbDepositTo.ItemsSource        = dt.DefaultView;
                cmbDepositTo.SelectedIndex      = -1;
            }
        }

        #endregion

        #region CalcEmpSalary

        private void CalcEmpSalary()
        {
            try
            {
                if (cmbEmp.SelectedValue == null) return;
                int empId = Convert.ToInt32(cmbEmp.SelectedValue);
                string month = cmbMonth.Text;
                string year  = cmbYear.Text;

                var adapter = new SqlDataAdapter(
                    $"SELECT ISNULL(E.id,0) AS id, ISNULL(E.name,'') AS name, " +
                    $"ISNULL(E.AccCode,'') AS AccCode, " +
                    $"ISNULL(SD.Basic,0) AS Basic, " +
                    $"ISNULL(SD.Houses,0) AS Houses, " +
                    $"ISNULL(SD.travel,0) AS travel, " +
                    $"ISNULL(SD.sal_add,0) AS sal_add, " +
                    $"ISNULL(SD.sal_Sub,0) AS sal_Sub, " +
                    $"ISNULL(SD.Net,0) AS Net, " +
                    $"ISNULL(RS.Notes,0) AS Notes, " +
                    $"ISNULL(RS.GlobalID,'') AS GlobalID " +
                    $"FROM Salary_Res RS " +
                    $"LEFT JOIN Salary_Res_Details SD ON SD.GlobalId=RS.GlobalId " +
                    $"LEFT JOIN Employees E ON SD.res_emp=E.id " +
                    $"WHERE E.id={empId} " +
                    $"AND RS.month={month} AND RS.year={year}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    double basic   = Convert.ToDouble(row["Basic"]);
                    double houses  = Convert.ToDouble(row["Houses"]);
                    double travel  = Convert.ToDouble(row["travel"]);
                    double salAdd  = Convert.ToDouble(row["sal_add"]);
                    double salSub  = Convert.ToDouble(row["sal_Sub"]);

                    TxtAccCode.Text     = row["AccCode"]?.ToString() ?? "";
                    TxtResGlobalID.Text = row["GlobalID"]?.ToString() ?? "";

                    txtTotSalary.Text = basic.ToString();
                    txtSalaryAdd.Text = salAdd.ToString();
                    txtSalarySub.Text = salSub.ToString();
                    TxtHous.Text      = houses.ToString();
                    TxtTravel.Text    = travel.ToString();
                    RecalcNet();
                }
                else
                {
                    DXMessageBox.Show("لا يوجد رواتب مستحقة للموظف.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RecalcNet()
        {
            try
            {
                double tot    = double.TryParse(txtTotSalary.Text, out double t) ? t : 0;
                double add    = double.TryParse(txtSalaryAdd.Text, out double a) ? a : 0;
                double hous   = double.TryParse(TxtHous.Text,      out double h) ? h : 0;
                double travel = double.TryParse(TxtTravel.Text,     out double tr) ? tr : 0;
                double sub    = double.TryParse(txtSalarySub.Text,  out double s) ? s : 0;

                txtNet.Text = (tot + add + hous + travel - sub).ToString("N2");
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlStr)
        {
            dgvSrch.UnselectAll();
            var cmd = new SqlCommand(sqlStr, conn);
            if (conn.State != ConnectionState.Open) conn.Open();
            ReadData(cmd.ExecuteReader());
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();
            CLR();

            Code         = Convert.ToInt32(dr["id"]);
            txtNo.Text   = Code.ToString();
            EntryGlobalID_Field = dr["EntryGlobalID"]?.ToString() ?? "";
            txtDate.DateTime    = Convert.ToDateTime(dr["date"]);

            LoadEmp(Convert.ToInt32(dr["emp_resp"]));
            cmbMonth.Text = dr["month"]?.ToString() ?? "";
            cmbYear.Text  = dr["year"]?.ToString() ?? "";

            if (dr["branch"] != DBNull.Value)
                cmbBranch.SelectedValue = dr["branch"];
            if (dr["emp"] != DBNull.Value)
                cmbEmp.SelectedValue = dr["emp"];

            txtTotSalary.Text  = dr["tot_salary"]?.ToString() ?? "";
            txtSalaryAdd.Text  = dr["salary_add"]?.ToString() ?? "";
            txtSalarySub.Text  = dr["salary_sub"]?.ToString() ?? "";
            txtNet.Text        = dr["salary_net"]?.ToString() ?? "";
            txtNotes.Text      = dr["notes"]?.ToString() ?? "";
            TxtHous.Text       = dr["Houses"]?.ToString() ?? "";
            TxtTravel.Text     = dr["Travel"]?.ToString() ?? "";
            txtEntryGlobalId.Text = dr["EntryGlobalID"]?.ToString() ?? "";
            TxtResGlobalID.Text   = dr["ResGlobalID"]?.ToString() ?? "";

            dr.Close();
        }

        private void Search()
        {
            string cond = "";

            if (!string.IsNullOrWhiteSpace(txtNoSrch.Text))
                cond += $" SalaryPay.id={txtNoSrch.Text} and ";
            else if (!string.IsNullOrWhiteSpace(cmbEmpSrch.Text) &&
                     cmbEmpSrch.SelectedIndex != -1)
                cond += $" SalaryPay.emp={cmbEmpSrch.SelectedValue} and ";

            if (chkall.IsChecked != true)
                cond += " date>=@date1 and date<=@date2 and ";

            LoadDG(cond);
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbBranch.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار الفرع.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbBranch.Focus(); return;
                }
                if (cmbEmp.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار الموظف.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbEmp.Focus(); return;
                }
                if (cmbDepositTo.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار الصندوق.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbDepositTo.Focus(); return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                // جلب معرف الموظف المسؤول
                int respEmpId = -1;
                if (!string.IsNullOrWhiteSpace(txtEmp.Text))
                {
                    var empAdapter = new SqlDataAdapter(
                        $"SELECT id FROM Employees WHERE name='{txtEmp.Text}'",
                        conn);
                    var empDt = new DataTable();
                    empAdapter.Fill(empDt);
                    if (empDt.Rows.Count > 0)
                        respEmpId = Convert.ToInt32(empDt.Rows[0][0]);
                }

                bool isNew = Code == -1;

                if (!isNew)
                {
                    // تحديث
                    var cmd = new SqlCommand(
                        $"UPDATE SalaryPay SET date=@date, " +
                        $"month={cmbMonth.Text}, year={cmbYear.Text}, " +
                        $"branch={cmbBranch.SelectedValue}, " +
                        $"emp={cmbEmp.SelectedValue}, " +
                        $"tot_salary={txtTotSalary.Text}, " +
                        $"salary_add={txtSalaryAdd.Text}, " +
                        $"salary_sub={txtSalarySub.Text}, " +
                        $"salary_net={txtNet.Text}, " +
                        $"emp_resp={respEmpId}, " +
                        $"notes=N'{txtNotes.Text}', " +
                        $"Houses=N'{TxtHous.Text}', " +
                        $"Travel=N'{TxtTravel.Text}' " +
                        $"WHERE id={Code}", conn);
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value =
                        txtDate.DateTime.ToShortDateString();
                    cmd.ExecuteNonQuery();
                    DXMessageBox.Show("تم تحديث السند بنجاح.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    CLR();
                    return;
                }

                // التحقق من الراتب المكرر
                var checkAdapter = new SqlDataAdapter(
                    $"SELECT id FROM SalaryPay " +
                    $"WHERE IS_Deleted=0 " +
                    $"AND year=N'{cmbYear.Text}' " +
                    $"AND month={cmbMonth.Text} " +
                    $"AND emp={cmbEmp.SelectedValue}", conn);
                var checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                if (checkDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("لقد تم دفع راتب الموظف سابقاً.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoadNxtNo();

                // البحث عن كود الصندوق
                var stockAdapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index " +
                    $"WHERE Type=2 {Accounting.BranchCondition} " +
                    $"AND AName LIKE N'%{cmbDepositTo.Text}%'", conn1);
                var stockDt = new DataTable();
                stockAdapter.Fill(stockDt);

                if (stockDt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لم يتم العثور على الحساب المقابل للصندوق.",
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // إدراج سند الراتب
                var insertCmd = new SqlCommand(
                    $"INSERT INTO SalaryPay(" +
                    $"id, date, month, year, branch, emp, " +
                    $"tot_salary, salary_add, salary_sub, salary_net, " +
                    $"emp_resp, notes, IS_Deleted, Houses, Travel, " +
                    $"EntryGlobalID, ResGlobalID) " +
                    $"VALUES({txtNo.Text}, @date, {cmbMonth.Text}, " +
                    $"{cmbYear.Text}, {cmbBranch.SelectedValue}, " +
                    $"{cmbEmp.SelectedValue}, " +
                    $"{txtTotSalary.Text}, {txtSalaryAdd.Text}, " +
                    $"{txtSalarySub.Text}, {txtNet.Text}, " +
                    $"{respEmpId}, N'{txtNotes.Text}', 0, " +
                    $"N'{TxtHous.Text}', N'{TxtTravel.Text}', " +
                    $"@EntryGlobalID, @ResGlobalID)", conn);
                insertCmd.Parameters.Add("@date", SqlDbType.DateTime).Value =
                    txtDate.DateTime.ToShortDateString();
                insertCmd.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value =
                    EntryGlobalID_Field;
                insertCmd.Parameters.Add("@ResGlobalID", SqlDbType.NVarChar).Value =
                    TxtResGlobalID.Text;
                insertCmd.ExecuteNonQuery();

                Code = Convert.ToInt32(txtNo.Text);

                DXMessageBox.Show("تم حفظ السند بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                CLR();
                LoadDG("");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر سنداً ليتم حذفه.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"UPDATE SalaryPay SET IS_Deleted=1 WHERE id={Code}",
                    conn).ExecuteNonQuery();

                new SqlCommand(
                    $"UPDATE Receipts SET ISDeleted=1 " +
                    $"WHERE EntryGlobalID=N'{EntryGlobalID_Field}'",
                    conn).ExecuteNonQuery();

                var receiptOper = new ReceiptOper();
                if (receiptOper.DeleteReceipt(Code.ToString(), EntryGlobalID_Field))
                {
                    DXMessageBox.Show("تم الحذف بنجاح.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                    Logger.Info(
                        $"تم حذف سند صرف راتب برقم {txtNo.Text} " +
                        $"بواسطة المستخدم {MainClass.UserName}");
                    CLR();
                }
                else
                {
                    DXMessageBox.Show("خطأ أثناء الحذف.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM SalaryPay WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SalaryPay WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM SalaryPay WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM SalaryPay WHERE IS_Deleted=0 ORDER BY id DESC");

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => Search();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => RptPrint(1);

        private void btnView_Click(object sender, RoutedEventArgs e)
            => RptPrint(2);

        private void BtnAll_Click(object sender, RoutedEventArgs e)
        {
            TxtHous.Text    = "";
            txtNet.Text     = "";
            txtNotes.Text   = "";
            txtSalaryAdd.Text = "";
            txtSalarySub.Text = "";
            txtTotSalary.Text = "";
            CalcEmpSalary();
        }

        private void btnviewEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtEntryGlobalId.Text))
                {
                    DXMessageBox.Show("لا توجد بيانات للعرض.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var form = new frmRptEntries();
                form.Show();
                form.Navigate(
                    $"SELECT * FROM Entry WHERE IS_Deleted=0 " +
                    $"AND GlobalID=N'{txtEntryGlobalId.Text}'");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ComboBox / TextBox Events

        private void cmbBranch_SelectionChanged(object sender,
                                                  SelectionChangedEventArgs e)
        {
            try
            {
                cmbEmp.Text = "";
                if (cmbBranch.SelectedValue != null)
                    LoadEmps(Convert.ToInt32(cmbBranch.SelectedValue));
            }
            catch { /* تجاهل */ }
        }

        private void txtTotSalary_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcNet();

        private void TxtHous_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcNet();

        private void TxtTravel_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcNet();

        private void rdCash_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdCash.IsChecked == true)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Stocks " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND IS_Deleted=0 AND status<>2 ORDER BY id", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "name";
                cmbDepositTo.SelectedValuePath  = "id";
                cmbDepositTo.ItemsSource        = dt.DefaultView;
                cmbDepositTo.SelectedIndex      = -1;
            }
            else
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Banks WHERE IS_Deleted=0 ORDER BY id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDepositTo.DisplayMemberPath = "name";
                cmbDepositTo.SelectedValuePath  = "id";
                cmbDepositTo.ItemsSource        = dt.DefaultView;
                cmbDepositTo.SelectedIndex      = -1;
            }
        }

        private void chkall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = chkall.IsChecked == true;
            txtFromDate.IsEnabled = !isAll;
            txtToDate.IsEnabled   = !isAll;
        }

        private void cmbDepositTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                try
                {
                    var form = new frmAccountSrch();
                    form.ShowDialog();
                    if (form.Code > -1)
                        cmbDepositTo.SelectedValue = form.Code;
                }
                catch { /* تجاهل */ }
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is SalaryPayRow row)
            {
                Code = row.PayId;
                Navigate($"SELECT * FROM SalaryPay WHERE id={Code}");
                TabControl1.SelectedIndex = 0;
                txtEntryGlobalId.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Window KeyDown

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // انتقال للعنصر التالي بدل SendKeys.Send("{TAB}")
                var element = Keyboard.FocusedElement as UIElement;
                element?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        #endregion

        #region Print

        public void RptPrint(int printType)
        {
            if (string.IsNullOrWhiteSpace(txtNet.Text))
            {
                DXMessageBox.Show("لا توجد قيمة بالسند.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "\u200f\u200fPaySalary.repx";

            string path = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !System.IO.File.Exists(path))
            {
                DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(printType, BindToData(), print.RptUrl,
                           print.RptName, print.defPrinter,
                           print.kitchenprinter, print.PrintNo);
        }

        private DataSet BindToData()
        {
            var adapter = new SqlDataAdapter("SELECT * FROM Foundation", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            string address = "", tel = "", mobile = "", foundation = "",
                   field   = "", vatNo = "";

            if (dt.Rows.Count > 0)
            {
                address    = dt.Rows[0]["Address"]?.ToString() ?? "";
                tel        = dt.Rows[0]["Tel"]?.ToString() ?? "";
                mobile     = dt.Rows[0]["Mobile"]?.ToString() ?? "";
                foundation = dt.Rows[0]["nameA"]?.ToString() ?? "";
                field      = dt.Rows[0]["FieldA"]?.ToString() ?? "";
                vatNo      = dt.Rows[0]["tax_no"]?.ToString() ?? "";
            }

            string bondType = rdCash.IsChecked == true ? "نقدي" : "تحويل بنكي";

            double netVal = double.TryParse(txtNet.Text, out double n) ? n : 0;

            var list = new List<Bond>
            {
                new Bond
                {
                    BondName      = Title,
                    PayToCode     = TxtAccCode.Text,
                    PayTo         = cmbEmp.Text,
                    Payfrom       = cmbDepositTo.Text,
                    supplier      = cmbEmp.Text,
                    BondDate      = txtDate.DateTime.ToShortDateString(),
                    BondNo        = txtNo.Text,
                    PrintDate     = DateTime.Now.ToShortDateString(),
                    Note          = txtNotes.Text,
                    BondVal       = txtNet.Text,
                    AccountStatus = "",
                    CurrentBalance = "",
                    BondType      = bondType,
                    Month         = cmbMonth.Text,
                    Year          = cmbYear.Text,
                    User          = Common.GetEmpName(MainClass.EmpNo),
                    ArabicLetter  = InvoiceOper.ToArabicLetter(netVal),
                    Address       = address,
                    Mobile        = mobile,
                    Telephone     = tel,
                    VATNo         = vatNo,
                    Foundation    = foundation,
                    Field         = field,
                    Logo          = "",
                    Header        = "",
                    Footer        = "",
                    Stamp         = "",
                }
            };

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion
    }
}