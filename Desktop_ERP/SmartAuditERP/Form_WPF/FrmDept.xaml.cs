using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmDept : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;
        private int _currentCode;
        private bool _isNew;

        #endregion

        #region Constructor

        public FrmDept()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _currentCode = -1;
            _isNew = true;

            Loaded += FrmDept_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDept_Loaded(object sender, RoutedEventArgs e)
        {
            ClearForm();
            LoadCategoryTable();
            LoadEmployees();

            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnNew.Click += (s, e) => { ClearForm(); LoadCategoryTable(); };
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClose.Click += (s, e) => Close();
        }

        #endregion

        #region Clear Form

        public void ClearForm()
        {
            txtCat_ID.Text = Common.GetMaX_ID("Cat_ID", "Cat_Table").ToString();
            txtCat_ID.Background = System.Windows.Media.Brushes.LightCoral;
            TxtCat_Name.Text = string.Empty;
            _currentCode = -1;
        }

        #endregion

        #region Load Data

        private void LoadEmployees()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees ORDER BY id", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbUser.ItemsSource = dataTable.DefaultView;
                cmbUser.DisplayMember = "name";
                cmbUser.ValueMember = "id";
                cmbUser.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void LoadCategoryTable()
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                var adapter = new SqlDataAdapter(
                    @"SELECT c.Cat_ID, c.Cat_Name, E.name AS EmpName
                      FROM Cat_Table c
                      LEFT JOIN Employees E ON E.id = c.emp",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                dgv_Table.ItemsSource = dataTable.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        #endregion

        #region CRUD Operations

        public void InsertCategory()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                var cmd = new SqlCommand(
                    "INSERT INTO Cat_Table(Cat_Name, emp) VALUES(@Cat_Name, @emp)",
                    _conn);

                cmd.Parameters.AddWithValue("@Cat_Name", TxtCat_Name.Text);
                cmd.Parameters.AddWithValue("@emp",
                    cmbUser.EditValue ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                DXMessageBox.Show("✅ تمت الإضافة بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        public void UpdateCategory()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                var cmd = new SqlCommand(
                    "UPDATE Cat_Table SET Cat_Name=@Cat_Name, emp=@emp " +
                    "WHERE Cat_ID=@Cat_ID",
                    _conn);

                cmd.Parameters.AddWithValue("@Cat_ID", txtCat_ID.Text);
                cmd.Parameters.AddWithValue("@Cat_Name", TxtCat_Name.Text);
                cmd.Parameters.AddWithValue("@emp",
                    cmbUser.EditValue ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                DXMessageBox.Show("✅ تم التعديل بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        public void DeleteCategory()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                var cmd = new SqlCommand(
                    "DELETE FROM Cat_Table WHERE Cat_ID=@Cat_ID",
                    _conn);

                cmd.Parameters.AddWithValue("@Cat_ID", txtCat_ID.Text);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("✅ تم الحذف بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        #endregion

        #region Button Handlers

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtCat_Name.Text))
                {
                    DXMessageBox.Show("أدخل اسم القسم", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentCode == -1)
                {
                    InsertCategory();
                    ClearForm();
                    LoadCategoryTable();
                    RefreshParentCombo();
                }
                else
                {
                    UpdateCategory();
                    ClearForm();
                    LoadCategoryTable();
                    RefreshParentCombo();
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحفظ:\n{ex.Message}");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode == -1)
            {
                DXMessageBox.Show("لا يمكن الحذف، الرجاء اختيار القسم",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = DXMessageBox.Show(
                "⚠️ هل أنت متأكد من حذف هذا القسم؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteCategory();
                ClearForm();
                LoadCategoryTable();
            }
        }

        #endregion

        #region Grid Events

        private void DgvTable_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgv_Table.SelectedItem is DataRowView rowView)
                {
                    txtCat_ID.Text = rowView["Cat_ID"].ToString();
                    TxtCat_Name.Text = rowView["Cat_Name"].ToString();
                    _currentCode = Convert.ToInt32(rowView["Cat_ID"]);

                    // البحث عن الموظف في القائمة
                    cmbUser.Text = rowView["EmpName"]?.ToString() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void RefreshParentCombo()
        {
            try
            {
                FrmTables frmTables = System.Windows.Application.Current.Windows
                    .OfType<FrmTables>()
                    .FirstOrDefault();

                if (frmTables?.cmbCat == null)
                    return;

                frmTables.cmbCat.ItemsSource = null;
                frmTables.cmbCat.Items.Clear();

                Common.fillcmb_All(frmTables.cmbCat, "Cat_Name", "Cat_ID", "Cat_Table");
            }
            catch
            {
                // ignore
            }
        }

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}