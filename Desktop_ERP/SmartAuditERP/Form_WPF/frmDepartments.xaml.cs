using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDepartments : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private int _currentCode;

        #endregion

        #region Constructor

        public frmDepartments()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _currentCode = -1;

            Loaded += FrmDepartments_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDepartments_Loaded(object sender, RoutedEventArgs e)
        {
            LoadManagements();
            LoadGrid();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnNew.Click += (s, e) => ClearForm();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClose.Click += (s, e) => Close();
            btnFirst.Click += BtnFirst_Click;
            btnPrevious.Click += BtnPrevious_Click;
            btnNext.Click += BtnNext_Click;
            btnLast.Click += BtnLast_Click;
            btnManagAdd.Click += BtnManagAdd_Click;

            txtDepName.KeyDown += TxtDepName_KeyDown;
        }

        #endregion

        #region Clear Form

        private void ClearForm()
        {
            txtDepName.Text = string.Empty;
            _currentCode = -1;
        }

        #endregion

        #region Data Loading

        public void LoadManagements()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Managements ORDER BY id",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbManags.ItemsSource = dataTable.DefaultView;
                cmbManags.DisplayMember = "name";
                cmbManags.ValueMember = "id";
                cmbManags.EditValue = null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LoadGrid()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    @"SELECT
                        d.id      AS dep_id,
                        m.id      AS manag_id,
                        m.name    AS manag,
                        d.name    AS dep
                      FROM Departments d
                      INNER JOIN Managements m ON d.manag_id = m.id",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                dgvDeps.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region CRUD Operations

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbManags.EditValue == null)
                {
                    DXMessageBox.Show("يجب اختيار إدارة أولاً", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbManags.Focus();
                    return;
                }

                string depName = txtDepName.Text.Trim();

                if (string.IsNullOrEmpty(depName))
                {
                    DXMessageBox.Show("يجب إدخال اسم القسم", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDepName.Focus();
                    return;
                }

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                if (_currentCode == -1)
                {
                    new SqlCommand(
                        $"INSERT INTO Departments(manag_id, name) " +
                        $"VALUES({cmbManags.EditValue}, N'{depName}')",
                        _conn).ExecuteNonQuery();

                    txtDepName.Text = string.Empty;
                    txtDepName.Focus();
                }
                else
                {
                    new SqlCommand(
                        $"UPDATE Departments SET " +
                        $"manag_id={cmbManags.EditValue}, " +
                        $"name=N'{depName}' " +
                        $"WHERE id={_currentCode}",
                        _conn).ExecuteNonQuery();

                    txtDepName.Focus();
                }

                LoadGrid();

                DXMessageBox.Show("✅ تم الحفظ بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحفظ:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode == -1)
            {
                DXMessageBox.Show("اختر قسماً ليتم حذفه", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = DXMessageBox.Show(
                "⚠️ هل أنت متأكد من حذف هذا القسم؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                new SqlCommand(
                    $"DELETE FROM Departments WHERE id={_currentCode}",
                    _conn).ExecuteNonQuery();

                DXMessageBox.Show("✅ تم الحذف بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadGrid();
                ClearForm();
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

        #region Grid Events

        private void DgvDeps_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvDeps.SelectedItem is DataRowView rowView)
                {
                    _currentCode = Convert.ToInt32(rowView["dep_id"]);

                    cmbManags.EditValue = rowView["manag_id"];
                    txtDepName.Text = rowView["dep"].ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

            _currentCode = Convert.ToInt32(reader["id"]);
            cmbManags.EditValue = reader["manag_id"];
            txtDepName.Text = reader["name"].ToString();
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Departments ORDER BY id ASC");

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Departments " +
                $"WHERE id<{_currentCode} ORDER BY id DESC");

        private void BtnNext_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Departments " +
                $"WHERE id>{_currentCode} ORDER BY id ASC");

        private void BtnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Departments ORDER BY id DESC");

        #endregion

        #region Add Management

        private void BtnManagAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = -1;

            if (cmbManags.EditValue != null)
                previousId = Convert.ToInt32(cmbManags.EditValue);

            var managForm = new frmManagement();
            managForm.Activate();
            managForm.ShowDialog();

            LoadManagements();

            try
            {
                if (previousId != -1)
                    cmbManags.EditValue = previousId;
            }
            catch { /* ignore */ }
        }

        #endregion

        #region TextBox Events

        private void TxtDepName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                BtnSave_Click(sender, new RoutedEventArgs());
        }

        #endregion

        #region Utilities

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}