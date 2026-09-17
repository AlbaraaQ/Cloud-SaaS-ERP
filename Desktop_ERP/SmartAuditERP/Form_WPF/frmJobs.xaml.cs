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
    public partial class frmJobs : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code = -1;

        private ObservableCollection<JobRow> _jobRows;

        #endregion

        #region Constructor

        public frmJobs()
        {
            conn = MainClass.ConnObj();
            _jobRows = new ObservableCollection<JobRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmJobs_Load(object sender, RoutedEventArgs e)
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
                _jobRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter("select * from jobs", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _jobRows.Add(new JobRow
                    {
                        Column2 = row["id"].ToString(),
                        Column1 = row["name"].ToString()
                    });
                }

                dgvJobs.ItemsSource = _jobRows;
                dgvJobs.UnselectAll();
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
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show("ادخل الوظيفة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (Code == -1)
                {
                    new SqlCommand($"insert into jobs(name) values('{txtName.Text}')", conn).ExecuteNonQuery();
                    txtName.Text = "";
                    txtName.Focus();
                }
                else
                {
                    new SqlCommand($"update jobs set name='{txtName.Text}' where id={Code}", conn).ExecuteNonQuery();
                    txtName.Focus();
                }

                LoadDG();
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
                    DXMessageBox.Show("اختر وظيفة ليتم حذفها", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                new SqlCommand($"delete from jobs where id={Code}", conn).ExecuteNonQuery();

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
            dgvJobs.UnselectAll();

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
            Navigate("select top 1 * from jobs order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from jobs order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from jobs where id>{Code} order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from jobs where id<{Code} order by id desc");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // للتوسع المستقبلي
        }

        #endregion

        #region Grid

        private void dgvJobs_CellClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            JobRow selected = dgvJobs.SelectedItem as JobRow;
            if (selected == null) return;

            txtName.Text = selected.Column1;
            int.TryParse(selected.Column2, out Code);
        }

        #endregion

        #region KeyDown

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
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

    public class JobRow
    {
        public string Column2 { get; set; }
        public string Column1 { get; set; }
    }
}