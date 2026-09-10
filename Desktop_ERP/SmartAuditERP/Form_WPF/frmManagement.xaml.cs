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
    public partial class frmManagement : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code = -1;

        private ObservableCollection<ManagementRow> _rows;

        #endregion

        #region Constructor

        public frmManagement()
        {
            conn  = MainClass.ConnObj();
            _rows = new ObservableCollection<ManagementRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmManagement_Load(object sender, RoutedEventArgs e)
        {
            LoadDG();

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Clear / New

        private void CLR()
        {
            txtName.Text = "";
            Code = -1;
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            txtName.Focus();
        }

        #endregion

        #region Load Data

        private void LoadDG()
        {
            try
            {
                _rows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter("select * from Managements", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _rows.Add(new ManagementRow
                    {
                        Column2 = row["id"].ToString(),
                        Column1 = row["name"].ToString()
                    });
                }

                dgvManagements.ItemsSource = _rows;
                dgvManagements.UnselectAll();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveData();
        }

        private void SaveData()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show("ادخل الإدارة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (Code == -1)
                {
                    new SqlCommand($"insert into Managements(name) values(N'{txtName.Text}')", conn).ExecuteNonQuery();
                    txtName.Text = "";
                    txtName.Focus();
                }
                else
                {
                    new SqlCommand($"update Managements set name=N'{txtName.Text}' where id={Code}", conn).ExecuteNonQuery();
                    txtName.Focus();
                }

                LoadDG();

                frmSavedMsg savedMsg = new frmSavedMsg();
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    CLR();
                else if (savedMsg.Pressed == 3)
                    Close();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
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
                    DXMessageBox.Show("اختر إدارة ليتم حذفها", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                new SqlCommand($"delete from Managements where id={Code}", conn).ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDG();
                CLR();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            dgvManagements.UnselectAll();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd = new SqlCommand(sqlStr, conn);
                ReadData(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;
                dr.Read();

                int.TryParse(dr["id"].ToString(), out Code);
                txtName.Text = dr["name"].ToString();
            }
            catch { }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Managements order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Managements order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Managements where id>{Code} order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Managements where id<{Code} order by id desc");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Grid

        private void dgvManagements_CellClick(object sender, MouseButtonEventArgs e)
        {
            ManagementRow selected = dgvManagements.SelectedItem as ManagementRow;
            if (selected == null) return;

            txtName.Text = selected.Column1;
            int.TryParse(selected.Column2, out Code);
        }

        #endregion

        #region KeyDown

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SaveData();
        }

        #endregion

        #region Helpers

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class ManagementRow
    {
        public string Column2 { get; set; }
        public string Column1 { get; set; }
    }
}