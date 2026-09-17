using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPersonsDealWith : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;
        private int _currentCode = -1;

        private ObservableCollection<PersonRow> _personsSource
            = new ObservableCollection<PersonRow>();

        #endregion

        #region Constructor

        public frmPersonsDealWith()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            dgvPersons.ItemsSource = _personsSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadJobs();
            LoadGrid();
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtName.Text    = "";
            txtTel.Text     = "";
            txtMobile.Text  = "";
            txtAddress.Text = "";
            txtNotes.Text   = "";
            cmbJob.SelectedIndex = -1;
            _currentCode = -1;
        }

        #endregion

        #region Load Data

        public void LoadJobs()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM jobs ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbJob.ItemsSource       = dt.DefaultView;
                    cmbJob.DisplayMemberPath = "name";
                    cmbJob.SelectedValuePath = "id";
                    cmbJob.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ تحميل الوظائف: " + ex.Message);
            }
        }

        private void LoadGrid()
        {
            _personsSource.Clear();
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM DealPersons ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        int jobId = 0;
                        int.TryParse(row["job"].ToString(), out jobId);

                        _personsSource.Add(new PersonRow
                        {
                            id      = Convert.ToInt32(row["id"]),
                            name    = row["name"].ToString(),
                            tel     = row["tel"].ToString(),
                            mobile  = row["mobile"].ToString(),
                            jobName = GetJobName(jobId)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
        }

        public string GetJobName(int jobId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM jobs WHERE id={jobId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var dr  = cmd.ExecuteReader())
                    ReadData(dr);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();
            CLR();

            _currentCode    = Convert.ToInt32(dr["id"]);
            txtName.Text    = dr["name"].ToString();
            txtTel.Text     = dr["tel"].ToString();
            txtMobile.Text  = dr["mobile"].ToString();
            txtAddress.Text = dr["address"].ToString();
            txtNotes.Text   = dr["notes"].ToString();

            try
            {
                int jobId = Convert.ToInt32(dr["job"]);
                if (jobId != -1)
                    cmbJob.SelectedValue = jobId;
            }
            catch { }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM DealPersons ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM DealPersons WHERE id<{_currentCode} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM DealPersons WHERE id>{_currentCode} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM DealPersons ORDER BY id DESC");

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show("يجب إدخال اسم الجهة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                int jobId = -1;
                if (cmbJob.SelectedValue != null)
                    int.TryParse(cmbJob.SelectedValue.ToString(), out jobId);

                EnsureOpen(_conn);

                SqlCommand cmd = (_currentCode != -1)
                    ? new SqlCommand(
                        $"UPDATE DealPersons SET name=@name,tel=@tel,mobile=@mobile," +
                        $"job=@job,address=@address,notes=@notes WHERE id={_currentCode}",
                        _conn)
                    : new SqlCommand(
                        "INSERT INTO DealPersons (name,tel,mobile,job,address,notes) " +
                        "VALUES (@name,@tel,@mobile,@job,@address,@notes)",
                        _conn);

                cmd.Parameters.Add("@name",    SqlDbType.NVarChar).Value = txtName.Text;
                cmd.Parameters.Add("@tel",     SqlDbType.NVarChar).Value = txtTel.Text;
                cmd.Parameters.Add("@mobile",  SqlDbType.NVarChar).Value = txtMobile.Text;
                cmd.Parameters.Add("@job",     SqlDbType.NVarChar).Value = jobId;
                cmd.Parameters.Add("@address", SqlDbType.NVarChar).Value = txtAddress.Text;
                cmd.Parameters.Add("@notes",   SqlDbType.NVarChar).Value = txtNotes.Text;
                cmd.ExecuteNonQuery();

                // الحصول على المعرف بعد الحفظ
                using (var maxCmd = new SqlCommand(
                    "SELECT MAX(id) FROM DealPersons", _conn))
                {
                    object r = maxCmd.ExecuteScalar();
                    if (r != DBNull.Value) _currentCode = Convert.ToInt32(r);
                }

                LoadGrid();
                DXMessageBox.Show("تم الحفظ بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    DXMessageBox.Show("اختر جهة ليتم حذفها",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureOpen(_conn);

                using (var cmd = new SqlCommand(
                    $"DELETE FROM DealPersons WHERE id={_currentCode}", _conn))
                    cmd.ExecuteNonQuery();

                LoadGrid();
                CLR();
                DXMessageBox.Show("تم الحذف بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void btnJobAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedJobId = -1;
            if (cmbJob.SelectedValue != null)
                int.TryParse(cmbJob.SelectedValue.ToString(), out savedJobId);

            var frm = new frmJobs();
            frm.ShowDialog();

            LoadJobs();

            try
            {
                if (savedJobId != -1)
                    cmbJob.SelectedValue = savedJobId;
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void dgvPersons_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvPersons.SelectedItem is PersonRow row)
                Navigate($"SELECT * FROM DealPersons WHERE id={row.id}");
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion
    }

    #region Model

    public class PersonRow
    {
        public int    id      { get; set; }
        public string name    { get; set; }
        public string tel     { get; set; }
        public string mobile  { get; set; }
        public string jobName { get; set; }
    }

    #endregion
}