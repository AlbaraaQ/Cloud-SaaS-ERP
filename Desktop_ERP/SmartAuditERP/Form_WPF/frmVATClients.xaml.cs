using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmVATClients : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        /// <summary>معرّف العميل الحالي، -1 يعني إضافة جديدة</summary>
        private int Code;

        /// <summary>هل تمت عملية حفظ ناجحة</summary>
        public bool IsDone;

        private ObservableCollection<VatClientRow> _clients;

        #endregion

        #region Constructor

        public frmVATClients()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            Code = -1;
            IsDone = false;
            _clients = new ObservableCollection<VatClientRow>();
            dgvdata.ItemsSource = _clients;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGrid();
        }

        #endregion

        #region Clear / New

        /// <summary>مسح الحقول والعودة لوضع الإضافة</summary>
        private void CLR()
        {
            txtName.Text = string.Empty;
            txtTaxNo.Text = string.Empty;
            Code = -1;
            dgvdata.UnselectAll();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            txtName.Focus();
        }

        #endregion

        #region Load Grid

        private void LoadGrid()
        {
            try
            {
                _clients.Clear();

                using (SqlDataAdapter da = new SqlDataAdapter(
                    "SELECT id, name, taxNo FROM VATClients WHERE IS_Deleted=0 ORDER BY id",
                    conn))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _clients.Add(new VatClientRow
                        {
                            id = row["id"] != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                            name = row["name"]?.ToString() ?? "",
                            taxNo = row["taxNo"]?.ToString() ?? ""
                        });
                    }
                }

                dgvdata.UnselectAll();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "ادخل العميل"
                        : "Enter Client Name";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                if (Code == -1)
                {
                    // التحقق من التكرار
                    using (SqlDataAdapter daCheck = new SqlDataAdapter(
                        "SELECT id FROM VATClients WHERE name=@name AND taxNo=@taxNo AND IS_Deleted=0",
                        conn))
                    {
                        daCheck.SelectCommand.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        daCheck.SelectCommand.Parameters.AddWithValue("@taxNo", txtTaxNo.Text.Trim());
                        DataTable dtCheck = new DataTable();
                        daCheck.Fill(dtCheck);

                        if (dtCheck.Rows.Count > 0)
                        {
                            string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                                ? "العميل تم إدخاله مسبقًا"
                                : "Client is previously inserted";
                            DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtName.Focus();
                            return;
                        }
                    }

                    // إدراج جديد
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO VATClients(name, taxNo, IS_Deleted) VALUES(@name, @taxNo, 0)",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@taxNo", txtTaxNo.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // تحديث موجود
                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE VATClients SET name=@name, taxNo=@taxNo WHERE id=@id",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@taxNo", txtTaxNo.Text.Trim());
                        cmd.Parameters.AddWithValue("@id", Code);
                        cmd.ExecuteNonQuery();
                    }
                }

                IsDone = true;
                LoadGrid();

                // رسالة نجاح
                var savedMsg = new frmSavedMsg();
                if (Code != -1)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";

                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    CLR();
                else if (savedMsg.Pressed == 3)
                    this.Close();
                else
                    txtName.Focus();
            }
            catch (Exception ex)
            {
                string errorMsg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                    ? $"Error during saving\nError details: {ex.Message}"
                    : $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}";
                DXMessageBox.Show(errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "اختر عميلًا ليتم حذفه"
                        : "Choose a Client to be deleted";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                    return;

                // التحقق من الارتباطات
                using (SqlDataAdapter daCheck = new SqlDataAdapter(
                    "SELECT curr FROM ItemUnits WHERE unit=@code",
                    conn))
                {
                    daCheck.SelectCommand.Parameters.AddWithValue("@code", Code.ToString());
                    DataTable dtCheck = new DataTable();
                    daCheck.Fill(dtCheck);

                    if (dtCheck.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "هذا العميل له ارتباطات فرعية لا يمكن حذفه"
                            : "This Client previously used in Sand VAT";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (DXMessageBox.Show("هل تريد حذف هذا العميل؟", "تأكيد الحذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM VATClients WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", Code);
                    cmd.ExecuteNonQuery();
                }

                string successMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "تم الحذف"
                    : "Deleted";
                DXMessageBox.Show(successMsg, "", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadGrid();
                CLR();
            }
            catch (Exception ex)
            {
                string errorMsg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                    ? $"Error during delete\nError details: {ex.Message}"
                    : $"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}";
                DXMessageBox.Show(errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    ReadDataFromReader(dr);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ReadDataFromReader(SqlDataReader dr)
        {
            if (dr.HasRows && dr.Read())
            {
                Code = dr["id"] != DBNull.Value ? Convert.ToInt32(dr["id"]) : -1;
                txtName.Text = dr["name"]?.ToString() ?? "";
                txtTaxNo.Text = dr["taxNo"]?.ToString() ?? "";
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM VATClients WHERE IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM VATClients WHERE IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM VATClients WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM VATClients WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");
        }

        #endregion

        #region DataGrid Events

        private void dgvdata_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvdata.SelectedItem is VatClientRow selectedRow)
            {
                Code = selectedRow.id;
                txtName.Text = selectedRow.name;
                txtTaxNo.Text = selectedRow.taxNo;
            }
        }

        private void dgvdata_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // يكفي SelectionChanged للتعامل مع النقر
        }

        #endregion

        #region Keyboard

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
        }

        #endregion

        #region Print

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("وظيفة الطباعة غير متاحة حالياً.\nيمكن ربطها بتقرير DevExpress لاحقًا.",
                "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}