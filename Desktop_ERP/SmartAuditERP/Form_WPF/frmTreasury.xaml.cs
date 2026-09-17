using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTreasury : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;

        private ObservableCollection<TreasuryListItem>     _stocks;
        private ObservableCollection<TreasuryEmployeeItem> _empItems;

        #endregion

        #region Constructor

        public frmTreasury()
        {
            InitializeComponent();
            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code  = -1;

            _stocks   = new ObservableCollection<TreasuryListItem>();
            _empItems = new ObservableCollection<TreasuryEmployeeItem>();
            dgvStocks.ItemsSource   = _stocks;
            dgvSafeEmps.ItemsSource = _empItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadBranches();
                LoadStates();
                LoadEmps();
                LoadDG();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtName.Text  = "";
            txtNotes.Text = "";
            chkDefault.IsChecked = false;
            Code = -1;
            _empItems.Clear();
            cmbBranches.SelectedIndex = -1;
            cmbStatus.SelectedIndex   = -1;
            txtName.Focus();
        }

        #endregion

        #region Data Loading

        private void LoadDG()
        {
            try
            {
                _stocks.Clear();
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    @"SELECT Stocks.id AS stock_id, Stocks.name AS stock, Branches.name AS branch
                      FROM Stocks INNER JOIN Branches ON Stocks.branch = Branches.BranchId
                      WHERE Stocks.IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _stocks.Add(new TreasuryListItem
                    {
                        StockId    = Convert.ToInt32(row["stock_id"]),
                        StockName  = row["stock"].ToString(),
                        BranchName = row["branch"].ToString()
                    });
                }
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadEmps()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                colEmpId.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT BranchId, name FROM Branches ORDER BY BranchId", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";
                cmbBranches.SelectedIndex     = -1;
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadStates()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT id, name FROM States ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbStatus.ItemsSource       = dt.DefaultView;
                cmbStatus.DisplayMemberPath = "name";
                cmbStatus.SelectedValuePath = "id";
                cmbStatus.SelectedIndex     = -1;
            }
            catch
            {
                // قيم افتراضية
                cmbStatus.Items.Clear();
                cmbStatus.Items.Add("نشط");
                cmbStatus.Items.Add("مغلق");
                cmbStatus.SelectedIndex = -1;
            }
            finally { EnsureClose(conn); }
        }

        private void LoadSafeEmployees()
        {
            try
            {
                _empItems.Clear();
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $@"SELECT Stock_Emps.emp_id
                       FROM Stock_Emps
                       INNER JOIN Employees ON Stock_Emps.emp_id = Employees.id
                       WHERE Employees.IS_Deleted=0 AND Stock_Emps.stock_id={Code}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                    _empItems.Add(new TreasuryEmployeeItem
                    { EmpId = Convert.ToInt32(row["emp_id"]) });
            }
            catch { }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DXMessageBox.Show("ادخل اسم الخزينة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus(); return;
            }

            if (cmbBranches.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار الفرع", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbBranches.Focus(); return;
            }

            if (_empItems.Count == 0)
            {
                DXMessageBox.Show("يجب اختيار موظف مسئول", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EnsureOpen(conn);
                var transaction = conn.BeginTransaction();

                try
                {
                    int branchId = cmbBranches.SelectedValue != null
                        ? Convert.ToInt32(cmbBranches.SelectedValue) : -1;

                    int statusId = 1;
                    if (cmbStatus.SelectedValue != null)
                        int.TryParse(cmbStatus.SelectedValue.ToString(), out statusId);

                    bool isDefault = chkDefault.IsChecked == true;

                    string sql = Code != -1
                        ? @"UPDATE Stocks SET name=@name, branch=@branch, status=@status,
                            IS_Default=@isDefault, notes=@notes WHERE id=@id"
                        : @"INSERT INTO Stocks(name,branch,status,IS_Default,notes,IS_Deleted)
                            VALUES(@name,@branch,@status,@isDefault,@notes,0)";

                    var cmd = new SqlCommand(sql, conn, transaction);

                    if (Code != -1) cmd.Parameters.AddWithValue("@id", Code);
                    cmd.Parameters.AddWithValue("@name",      txtName.Text);
                    cmd.Parameters.AddWithValue("@branch",    branchId);
                    cmd.Parameters.AddWithValue("@status",    statusId);
                    cmd.Parameters.AddWithValue("@isDefault", isDefault);
                    cmd.Parameters.AddWithValue("@notes",     txtNotes.Text);
                    cmd.ExecuteNonQuery();

                    if (Code == -1)
                    {
                        var maxCmd = new SqlCommand("SELECT MAX(id) FROM Stocks", conn, transaction);
                        Code = Convert.ToInt32(maxCmd.ExecuteScalar());
                    }

                    // حذف الموظفين القديمين وإعادة إدراجهم
                    new SqlCommand($"DELETE FROM Stock_Emps WHERE stock_id={Code}", conn, transaction)
                        .ExecuteNonQuery();

                    foreach (var emp in _empItems)
                    {
                        if (emp.EmpId <= 0) continue;
                        var empCmd = new SqlCommand(
                            "INSERT INTO Stock_Emps(stock_id, emp_id) VALUES(@stock_id, @emp_id)",
                            conn, transaction);
                        empCmd.Parameters.AddWithValue("@stock_id", Code);
                        empCmd.Parameters.AddWithValue("@emp_id",   emp.EmpId);
                        empCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    LoadDG();

                    var savedMsg = new frmSavedMsg();
                    if (Code != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                    savedMsg.ShowDialog();

                    if (savedMsg.Pressed == 1) CLR();
                    else if (savedMsg.Pressed == 3) this.Close();
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
            finally { EnsureClose(conn); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر خزينة ليتم حذفها", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            if (Code == 1 || Code == 2)
            {
                DXMessageBox.Show("لا يمكن حذف هذا الصندوق", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var confirm = DXMessageBox.Show("هل أنت متأكد من حذف الصندوق؟", "تأكيد الحذف",
                                          MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                EnsureOpen(conn);
                new SqlCommand($"UPDATE Stocks SET IS_Deleted=1 WHERE id={Code}", conn)
                    .ExecuteNonQuery();
                new SqlCommand($"DELETE FROM Stock_Emps WHERE stock_id={Code}", conn)
                    .ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDG(); CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM Stocks WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Stocks WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Stocks WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM Stocks WHERE IS_Deleted=0 ORDER BY id DESC");

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
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) { reader.Close(); return; }

            reader.Read();
            CLR();

            Code         = Convert.ToInt32(reader["id"]);
            txtName.Text = reader["name"].ToString();
            txtNotes.Text = reader["notes"] != DBNull.Value ? reader["notes"].ToString() : "";

            if (reader["branch"] != DBNull.Value)
                cmbBranches.SelectedValue = reader["branch"];

            if (reader["status"] != DBNull.Value)
                cmbStatus.SelectedValue = reader["status"];

            chkDefault.IsChecked = reader["IS_Default"] != DBNull.Value
                                   && Convert.ToBoolean(reader["IS_Default"]);
            reader.Close();

            LoadSafeEmployees();
        }

        #endregion

        #region DataGrid Events

        private void dgvStocks_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvStocks.SelectedItem is TreasuryListItem item)
            {
                Code = item.StockId;
                Navigate($"SELECT * FROM Stocks WHERE id={Code}");
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        private void SetStatus(string msg) { }

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

    public class TreasuryListItem
    {
        public int    StockId    { get; set; }
        public string StockName  { get; set; }
        public string BranchName { get; set; }
    }

    public class TreasuryEmployeeItem
    {
        public int EmpId { get; set; }
    }
}