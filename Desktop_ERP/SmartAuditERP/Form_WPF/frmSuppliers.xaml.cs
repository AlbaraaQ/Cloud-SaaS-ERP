using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSuppliers : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code;
        public bool IsDone;

        #endregion

        #region Constructor

        public frmSuppliers()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            Code = -1;
            IsDone = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGrid();
            this.WindowState = MainClass.Window_State;
        }

        #endregion

        #region Clear / New

        private void ClearFields()
        {
            txtName.Text = "";
            txtTaxNo.Text = "";
            Code = -1;
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        #endregion

        #region Load Grid

        private void LoadGrid()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name, taxNo FROM Suppliers WHERE IS_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);

                var list = new ObservableCollection<SupplierModel>();
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new SupplierModel
                    {
                        id      = row["id"].ToString(),
                        name    = row["name"].ToString(),
                        taxNo   = row["taxNo"].ToString()
                    });
                }
                dgvdata.ItemsSource = list;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل البيانات\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DataGrid Selection

        private void dgvdata_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvdata.SelectedItem is SupplierModel selected)
            {
                txtName.Text  = selected.name;
                txtTaxNo.Text = selected.taxNo;
                if (int.TryParse(selected.id, out int parsedId))
                    Code = parsedId;
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    string msg = string.Equals(MainClass.Language, "ar")
                        ? "ادخل المورد"
                        : "Enter supplier";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                if (Code == -1)
                {
                    // التحقق من التكرار
                    var checkAdapter = new SqlDataAdapter(
                        $"SELECT id FROM Suppliers WHERE name=N'{txtName.Text}' " +
                        $"AND taxNo=N'{txtTaxNo.Text}' AND IS_Deleted=0", conn);
                    var checkTable = new DataTable();
                    checkAdapter.Fill(checkTable);

                    if (checkTable.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar")
                            ? "المورد تم ادخاله مسبقا"
                            : "Supplier is previously inserted";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Information);
                        txtName.Focus();
                        return;
                    }

                    // إضافة جديد
                    var insertCmd = new SqlCommand(
                        $"INSERT INTO Suppliers(name,taxNo,IS_Deleted) " +
                        $"VALUES(N'{txtName.Text}',N'{txtTaxNo.Text}',0)", conn);
                    insertCmd.ExecuteNonQuery();
                    txtName.Text = "";
                    txtName.Focus();
                }
                else
                {
                    // تعديل
                    var updateCmd = new SqlCommand(
                        $"UPDATE Suppliers SET name=N'{txtName.Text}', " +
                        $"taxNo=N'{txtTaxNo.Text}' WHERE id={Code}", conn);
                    updateCmd.ExecuteNonQuery();
                    txtName.Focus();
                }

                IsDone = true;
                LoadGrid();

                // نافذة رسالة الحفظ
                var savedMsg = new frmSavedMsg();
                if (Code != -1)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1 || savedMsg.Pressed == 2)
                    ClearFields();
                else if (savedMsg.Pressed == 3)
                    this.Close();
            }
            catch (Exception ex)
            {
                string msg = string.Equals(MainClass.Language, "en")
                    ? $"Error during saving\nError details: {ex.Message}"
                    : $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    string msg = string.Equals(MainClass.Language, "ar")
                        ? "اختر مورد ليتم حذفه"
                        : "Choose supplier to be deleted";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(txtName.Text))
                {
                    // التحقق من الارتباطات
                    var checkAdapter = new SqlDataAdapter(
                        $"SELECT ItemId FROM ItemUnits WHERE unit=N'{Code}'", conn);
                    var checkTable = new DataTable();
                    checkAdapter.Fill(checkTable);

                    if (checkTable.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar")
                            ? "هذه المورد له ارتباطات فرعية لايمكن حذفه"
                            : "This Suppliers previously used in Sand VAT";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var confirm = DXMessageBox.Show("هل تريد حذف هذا المورد", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    var deleteCmd = new SqlCommand(
                        $"DELETE FROM Suppliers WHERE id={Code}", conn);
                    deleteCmd.ExecuteNonQuery();

                    string doneMsg = string.Equals(MainClass.Language, "ar") ? "تم الحذف" : "Deleted";
                    DXMessageBox.Show(doneMsg, "", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadGrid();
                    ClearFields();
                }
            }
            catch (Exception ex)
            {
                string msg = string.Equals(MainClass.Language, "en")
                    ? $"Error during delete\nError details: {ex.Message}"
                    : $"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvdata.UnselectAll();
                var cmd = new SqlCommand(sqlQuery, conn);
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        if (int.TryParse(reader["id"].ToString(), out int parsedId))
                            Code = parsedId;
                        txtName.Text  = reader["name"].ToString();
                        txtTaxNo.Text = reader["taxNo"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التنقل\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Suppliers WHERE IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Suppliers WHERE IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Suppliers WHERE id>{Code} AND IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Suppliers WHERE id<{Code} AND IS_Deleted=0 ORDER BY id DESC");
        }

        #endregion

        #region Other Events

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة هنا
        }

        #endregion
    }

    #region Model

    public class SupplierModel
    {
        public string id    { get; set; }
        public string name  { get; set; }
        public string taxNo { get; set; }
    }

    #endregion
}