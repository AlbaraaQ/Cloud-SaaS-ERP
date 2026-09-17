using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSafes : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code;

        private ObservableCollection<SafeListItem>     _safesItems;
        private ObservableCollection<SafeEmployeeItem> _safeEmpsItems;

        #endregion

        #region Constructor

        public frmSafes()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            Code = -1;

            _safesItems    = new ObservableCollection<SafeListItem>();
            _safeEmpsItems = new ObservableCollection<SafeEmployeeItem>();

            dgvSafes.ItemsSource    = _safesItems;
            dgvSafeEmps.ItemsSource = _safeEmpsItems;
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

                if (cmbBranches.ItemsSource != null)
                    cmbBranches.SelectedValue = MainClass.BranchNo;

                cmbStatus.SelectedIndex = 0;
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

            _safeEmpsItems.Clear();

            if (cmbBranches.ItemsSource != null)
                cmbBranches.SelectedValue = MainClass.BranchNo;

            cmbStatus.SelectedIndex = 0;
            txtName.Focus();
        }

        #endregion

        #region Data Loading

        private void LoadDG()
        {
            try
            {
                _safesItems.Clear();

                EnsureOpen(conn);
                string sql = @"SELECT Safes.id AS Safe_id,
                                      Safes.name AS safe,
                                      Branches.name AS branch
                               FROM Safes
                               INNER JOIN Branches ON Safes.branch = Branches.BranchId
                               WHERE Safes.IS_Deleted = 0";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _safesItems.Add(new SafeListItem
                    {
                        SafeId     = Convert.ToInt32(row["Safe_id"]),
                        SafeName   = row["safe"].ToString(),
                        BranchName = row["branch"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadEmps()
        {
            try
            {
                EnsureOpen(conn);
                string sql = "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                // تعيين مصدر بيانات عمود الكومبو في dgvSafeEmps
                colEmpId.ItemsSource       = dt.DefaultView;
                colEmpId.DisplayMemberPath = "name";
                colEmpId.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الموظفين: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                string sql = "SELECT BranchId, name FROM Branches ORDER BY BranchId";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";
                cmbBranches.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الفروع: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadStates()
        {
            try
            {
                EnsureOpen(conn);
                // محاولة تحميل الحالات من قاعدة البيانات
                var adapter = new SqlDataAdapter("SELECT id, name FROM States ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbStatus.ItemsSource       = dt.DefaultView;
                    cmbStatus.DisplayMemberPath = "name";
                    cmbStatus.SelectedValuePath = "id";
                }
                else
                {
                    // قيم افتراضية
                    cmbStatus.Items.Add(new { id = 1, name = "نشط" });
                    cmbStatus.Items.Add(new { id = 0, name = "مغلق" });
                    cmbStatus.DisplayMemberPath = "name";
                    cmbStatus.SelectedValuePath = "id";
                }

                cmbStatus.SelectedIndex = -1;
            }
            catch
            {
                // قيم افتراضية عند الخطأ
                cmbStatus.Items.Clear();
                cmbStatus.Items.Add("نشط");
                cmbStatus.Items.Add("مغلق");
                cmbStatus.SelectedIndex = -1;
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // ── التحقق من البيانات ──
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DXMessageBox.Show("ادخل اسم المخزن", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (cmbBranches.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار الفرع", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbBranches.Focus();
                return;
            }

            if (cmbStatus.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار حالة المخزن", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbStatus.Focus();
                return;
            }

            // التحقق من وجود موظف مسئول
            bool hasValidEmp = false;
            foreach (var emp in _safeEmpsItems)
            {
                if (emp.EmpId > 0)
                {
                    hasValidEmp = true;
                    break;
                }
            }

            if (!hasValidEmp)
            {
                DXMessageBox.Show("يجب اختيار موظف مسئول واحد على الأقل", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                dgvSafeEmps.Focus();
                return;
            }

            // التحقق من تكرار الموظفين
            for (int i = 0; i < _safeEmpsItems.Count; i++)
            {
                for (int j = i + 1; j < _safeEmpsItems.Count; j++)
                {
                    if (_safeEmpsItems[i].EmpId == _safeEmpsItems[j].EmpId
                        && _safeEmpsItems[i].EmpId > 0)
                    {
                        DXMessageBox.Show("لقد كررت إدخال الموظف", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            try
            {
                EnsureOpen(conn);
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // تحديد الرقم
                    if (Code == -1)
                    {
                        var maxCmd = new SqlCommand(
                            "SELECT ISNULL(MAX(id), 0) + 1 FROM Safes", conn, transaction);
                        Code = Convert.ToInt32(maxCmd.ExecuteScalar());
                    }

                    int branchId = Convert.ToInt32(cmbBranches.SelectedValue);
                    int statusId = cmbStatus.SelectedValue != null
                        ? Convert.ToInt32(cmbStatus.SelectedValue)
                        : (cmbStatus.SelectedIndex == 0 ? 1 : 0);
                    bool isDefault = chkDefault.IsChecked == true;

                    // حفظ / تحديث المخزن
                    string checkSql = $"SELECT COUNT(*) FROM Safes WHERE id = {Code}";
                    var checkCmd = new SqlCommand(checkSql, conn, transaction);
                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (exists == 0)
                    {
                        // إدراج جديد
                        string insertSql = @"INSERT INTO Safes
                            (id, name, branch, status, IS_Default, notes, IS_Deleted, CreateDate, LastUpdateDate)
                            VALUES
                            (@id, @name, @branch, @status, @isDefault, @notes, 0, @createDate, @updateDate)";
                        var insertCmd = new SqlCommand(insertSql, conn, transaction);
                        insertCmd.Parameters.AddWithValue("@id",         Code);
                        insertCmd.Parameters.AddWithValue("@name",       txtName.Text);
                        insertCmd.Parameters.AddWithValue("@branch",     branchId);
                        insertCmd.Parameters.AddWithValue("@status",     statusId);
                        insertCmd.Parameters.AddWithValue("@isDefault",  isDefault);
                        insertCmd.Parameters.AddWithValue("@notes",      txtNotes.Text);
                        insertCmd.Parameters.AddWithValue("@createDate", DateTime.Now);
                        insertCmd.Parameters.AddWithValue("@updateDate", DateTime.Now);
                        insertCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // تحديث
                        string updateSql = @"UPDATE Safes SET
                            name = @name, branch = @branch, status = @status,
                            IS_Default = @isDefault, notes = @notes,
                            LastUpdateDate = @updateDate
                            WHERE id = @id";
                        var updateCmd = new SqlCommand(updateSql, conn, transaction);
                        updateCmd.Parameters.AddWithValue("@id",         Code);
                        updateCmd.Parameters.AddWithValue("@name",       txtName.Text);
                        updateCmd.Parameters.AddWithValue("@branch",     branchId);
                        updateCmd.Parameters.AddWithValue("@status",     statusId);
                        updateCmd.Parameters.AddWithValue("@isDefault",  isDefault);
                        updateCmd.Parameters.AddWithValue("@notes",      txtNotes.Text);
                        updateCmd.Parameters.AddWithValue("@updateDate", DateTime.Now);
                        updateCmd.ExecuteNonQuery();
                    }

                    // حذف المسئولين القديمين وإعادة إدراجهم
                    var deleteEmpsCmd = new SqlCommand(
                        $"DELETE FROM Safe_Emps WHERE safe_id = {Code}", conn, transaction);
                    deleteEmpsCmd.ExecuteNonQuery();

                    foreach (var empItem in _safeEmpsItems)
                    {
                        if (empItem.EmpId <= 0) continue;

                        string insertEmpSql = @"INSERT INTO Safe_Emps (safe_id, emp_id)
                                                VALUES (@safeId, @empId)";
                        var insertEmpCmd = new SqlCommand(insertEmpSql, conn, transaction);
                        insertEmpCmd.Parameters.AddWithValue("@safeId", Code);
                        insertEmpCmd.Parameters.AddWithValue("@empId",  empItem.EmpId);
                        insertEmpCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    LoadDG();

                    var result = DXMessageBox.Show(
                        "تم الحفظ بنجاح\nهل تريد إضافة مخزن جديد؟",
                        "تم الحفظ",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                        CLR();
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
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر مخزناً ليتم حذفه", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EnsureOpen(conn);

                // التحقق من وجود فواتير مرتبطة
                if (!string.IsNullOrWhiteSpace(txtName.Text))
                {
                    var checkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM Inv WHERE safe = {Code} AND IS_Deleted = 0",
                        conn);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        DXMessageBox.Show("لا يمكن حذف مخزن مرتبط بفواتير", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var confirm = DXMessageBox.Show("هل أنت متأكد من حذف المخزن؟", "تأكيد الحذف",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                // حذف ناعم
                new SqlCommand(
                    $"UPDATE Safes SET IS_Deleted=1 WHERE id={Code}", conn)
                    .ExecuteNonQuery();

                // حذف المسئولين
                new SqlCommand(
                    $"DELETE FROM Safe_Emps WHERE safe_id={Code}", conn)
                    .ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDG();
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // طباعة (محجوز للتنفيذ لاحقاً)
        }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Safes WHERE IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Safes WHERE IS_Deleted=0 AND id < {Code} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Safes WHERE IS_Deleted=0 AND id > {Code} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Safes WHERE IS_Deleted=0 ORDER BY id DESC");
        }

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var cmd    = new SqlCommand(sqlQuery, conn);
                var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            reader.Read();
            CLR();

            Code          = Convert.ToInt32(reader["id"]);
            txtName.Text  = reader["name"].ToString();
            txtNotes.Text = reader["notes"] != DBNull.Value ? reader["notes"].ToString() : "";

            if (reader["branch"] != DBNull.Value)
                cmbBranches.SelectedValue = reader["branch"];

            if (reader["status"] != DBNull.Value)
                cmbStatus.SelectedValue = reader["status"];

            chkDefault.IsChecked = reader["IS_Default"] != DBNull.Value
                                   && Convert.ToBoolean(reader["IS_Default"]);

            reader.Close();

            // تحميل مسئولي المخزن
            LoadSafeEmployees();
        }

        private void LoadSafeEmployees()
        {
            try
            {
                EnsureOpen(conn);
                string sql = $@"SELECT Safe_Emps.emp_id
                                FROM Safe_Emps
                                INNER JOIN Employees ON Safe_Emps.emp_id = Employees.id
                                WHERE Employees.IS_Deleted = 0
                                  AND Safe_Emps.safe_id = {Code}";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                _safeEmpsItems.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _safeEmpsItems.Add(new SafeEmployeeItem
                    {
                        EmpId = Convert.ToInt32(row["emp_id"])
                    });
                }
            }
            catch
            {
                // تجاهل الخطأ
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvSafes_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSafes.SelectedItem is SafeListItem selected)
            {
                Code = selected.SafeId;
                Navigate($"SELECT * FROM Safes WHERE id = {Code}");
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void EnsureClose(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }
}