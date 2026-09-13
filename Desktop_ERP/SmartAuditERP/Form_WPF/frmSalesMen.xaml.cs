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
    public partial class frmSalesMen : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int  Code   = -1;
        public  bool isDone = false;

        private ObservableCollection<SalesmanRow> SalesmenList;

        #endregion

        #region Constructor

        public frmSalesMen()
        {
            InitializeComponent();
            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            SalesmenList = new ObservableCollection<SalesmanRow>();
            dgvData.ItemsSource = SalesmenList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // لا يوجد تحميل أولي — البيانات تُعرض عند البحث
        }

        #endregion

        #region CLR

        private void CLR()
        {
            txtName.Text      = "";
            txtComm.Text      = "";
            txtColleComm.Text = "";
            txtProfitComm.Text = "";
            txtTel.Text       = "";
            txtMobile.Text    = "";
            txtEmail.Text     = "";
            txtNotes.Text     = "";
            Code = -1;
        }

        #endregion

        #region LoadDG

        private void LoadDG(string cond)
        {
            SalesmenList.Clear();
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM salesmen " +
                    $"WHERE {cond} IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    SalesmenList.Add(new SalesmanRow
                    {
                        SalesmanId   = Convert.ToInt32(row["id"]),
                        SalesmanName = row["name"]?.ToString() ?? "",
                        Commission   = Convert.ToDouble(row["comm"]),
                        Email        = row["email"]?.ToString() ?? "",
                        Tel          = row["tel"]?.ToString() ?? "",
                        Mobile       = row["mobile"]?.ToString() ?? "",
                    });
                }

                dgvData.UnselectAll();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigate / ReadData

        private void Navigate(string sqlStr)
        {
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

            Code              = Convert.ToInt32(dr["id"]);
            txtName.Text      = dr["name"]?.ToString() ?? "";
            txtComm.Text      = dr["comm"]?.ToString() ?? "";
            txtTel.Text       = dr["tel"]?.ToString() ?? "";
            txtMobile.Text    = dr["mobile"]?.ToString() ?? "";
            txtEmail.Text     = dr["email"]?.ToString() ?? "";
            txtNotes.Text     = dr["notes"]?.ToString() ?? "";
            txtColleComm.Text = dr["Colle_Comm"]?.ToString() ?? "";
            txtProfitComm.Text = dr["Profit_Comm"]?.ToString() ?? "";

            dr.Close();
            TabControl1.SelectedIndex = 0;
        }

        private void Search()
        {
            LoadDG($"name LIKE N'%{txtNameSrch.Text}%' AND ");
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlCommand cmd;
                if (Code == -1)
                {
                    cmd = new SqlCommand(
                        "INSERT INTO salesmen(name, comm, tel, mobile, email, " +
                        "notes, IS_Deleted, Profit_Comm, Colle_Comm) " +
                        "VALUES (@name, @comm, @tel, @mobile, @email, " +
                        "@notes, @IS_Deleted, @Profit_Comm, @Colle_Comm)",
                        conn);
                }
                else
                {
                    cmd = new SqlCommand(
                        $"UPDATE salesmen SET name=@name, comm=@comm, " +
                        $"tel=@tel, mobile=@mobile, email=@email, " +
                        $"notes=@notes, Profit_Comm=@Profit_Comm, " +
                        $"Colle_Comm=@Colle_Comm WHERE id={Code}",
                        conn);
                }

                cmd.Parameters.Add("@name",        SqlDbType.NVarChar).Value = txtName.Text;
                cmd.Parameters.Add("@comm",        SqlDbType.Decimal ).Value = double.TryParse(txtComm.Text, out double c) ? c : 0;
                cmd.Parameters.Add("@Profit_Comm", SqlDbType.Decimal ).Value = double.TryParse(txtProfitComm.Text, out double pc) ? pc : 0;
                cmd.Parameters.Add("@Colle_Comm",  SqlDbType.Decimal ).Value = double.TryParse(txtColleComm.Text,  out double cc) ? cc : 0;
                cmd.Parameters.Add("@tel",         SqlDbType.NVarChar).Value = txtTel.Text;
                cmd.Parameters.Add("@mobile",      SqlDbType.NVarChar).Value = txtMobile.Text;
                cmd.Parameters.Add("@email",       SqlDbType.NVarChar).Value = txtEmail.Text;
                cmd.Parameters.Add("@notes",       SqlDbType.NVarChar).Value = txtNotes.Text;
                cmd.Parameters.Add("@IS_Deleted",  SqlDbType.Bit     ).Value = 0;
                cmd.ExecuteNonQuery();

                isDone = true;

                string msg = Code == -1 ? "تم حفظ المندوب بنجاح." : "تم تحديث بيانات المندوب.";
                var result = DXMessageBox.Show(
                    $"{msg}\n\nهل تريد إدخال مندوب جديد؟", "تم",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    CLR();
                else if (result == MessageBoxResult.Cancel)
                    Close();
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
                    DXMessageBox.Show("اختر مندوباً ليتم حذفه.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"UPDATE salesmen SET IS_Deleted=1 WHERE id={Code}",
                    conn).ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                CLR();
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
            => Navigate("SELECT TOP 1 * FROM salesmen WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM salesmen WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM salesmen WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM salesmen WHERE IS_Deleted=0 ORDER BY id DESC");

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => Search();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // يمكن إضافة منطق الطباعة لاحقاً
            DXMessageBox.Show("الطباعة غير مفعّلة في هذا الإصدار.", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmInvBySalesMen();
                form.Show();
                form.Activate();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvData.SelectedItem is SalesmanRow row)
            {
                Code = row.SalesmanId;
                Navigate($"SELECT * FROM salesmen WHERE id={Code}");
            }
        }

        #endregion

        #region TextBox Events

        private void txtNameSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search();
        }

        #endregion
    }
}