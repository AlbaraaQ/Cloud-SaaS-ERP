using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmViolationM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int           Index = -1;
        private ObservableCollection<ViolationModel> violationList;

        #endregion

        #region Constructor

        public frmViolationM()
        {
            InitializeComponent();
            conn          = MainClass.ConnObj();
            violationList = new ObservableCollection<ViolationModel>();
            dgvData.ItemsSource = violationList;
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGrid();
            LoadMarine();
            LoadNextNo();
            txtdate.DateTime   = DateTime.Now.Date;
            this.WindowState   = MainClass.Window_State;
        }

        #endregion

        #region Clear

        private void ClearFields()
        {
            txtPeriod.Text           = "";
            txtViolType.Text         = "";
            txtno.Text               = "";
            txtNote.Text             = "";
            cmbMarine.SelectedIndex  = -1;
            Index = -1;
        }

        #endregion

        #region Load Grid

        private void LoadGrid()
        {
            try
            {
                violationList.Clear();
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Violation WHERE IsDeleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    violationList.Add(new ViolationModel
                    {
                        id         = row["id"].ToString(),
                        marineId   = row["MarineId"].ToString(),
                        period     = row["Period"].ToString(),
                        vdate      = row["Vdate"] != DBNull.Value
                            ? Convert.ToDateTime(row["Vdate"]).ToShortDateString()
                            : "",
                        violatType = row["ViolatType"].ToString()
                    });
                }

                LoadNextNo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل البيانات\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Next No

        private void LoadNextNo()
        {
            try
            {
                txtno.Text = "";
                var adapter = new SqlDataAdapter(
                    "SELECT MAX(id) FROM Violation", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0 && table.Rows[0][0] != DBNull.Value)
                {
                    if (double.TryParse(table.Rows[0][0].ToString(), out double maxId))
                        txtno.Text = ((int)(maxId + 1)).ToString();
                }
                else
                {
                    txtno.Text = "1";
                }
            }
            catch { }
        }

        #endregion

        #region Load Marine

        private void LoadMarine()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, Name, MarineCode FROM Marine WHERE IS_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    cmbMarine.DisplayMemberPath = "Name";
                    cmbMarine.SelectedValuePath = "id";
                    cmbMarine.ItemsSource       = table.DefaultView;
                    cmbMarine.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        #endregion

        #region New

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
            LoadNextNo();
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtno.Text))
                    return;

                if (conn.State != ConnectionState.Open) conn.Open();

                if (cmbMarine.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار المركب",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbMarine.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtPeriod.Text))
                {
                    MessageBox.Show("يجب تحديد مدة المخالفة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPeriod.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtViolType.Text))
                {
                    MessageBox.Show("يجب تحديد نوع المخالفة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtViolType.Focus();
                    return;
                }

                string dateStr = txtdate.DateTime.ToString("yyyy-MM-dd");

                if (Index == -1)
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO Violation(MarineId,Vdate,Period,status,ViolatType,notes,IsDeleted) " +
                        "VALUES(@MarineId,@Vdate,@Period,1,@ViolatType,@notes,0)", conn);
                    cmd.Parameters.AddWithValue("@MarineId",   cmbMarine.SelectedValue);
                    cmd.Parameters.AddWithValue("@Vdate",      dateStr);
                    cmd.Parameters.AddWithValue("@Period",     txtPeriod.Text);
                    cmd.Parameters.AddWithValue("@ViolatType", txtViolType.Text);
                    cmd.Parameters.AddWithValue("@notes",      txtNote.Text);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var cmd = new SqlCommand(
                        $"UPDATE Violation SET " +
                        $"MarineId=@MarineId, Vdate=@Vdate, Period=@Period, " +
                        $"ViolatType=@ViolatType, notes=@notes " +
                        $"WHERE id={Index}", conn);
                    cmd.Parameters.AddWithValue("@MarineId",   cmbMarine.SelectedValue);
                    cmd.Parameters.AddWithValue("@Vdate",      dateStr);
                    cmd.Parameters.AddWithValue("@Period",     txtPeriod.Text);
                    cmd.Parameters.AddWithValue("@ViolatType", txtViolType.Text);
                    cmd.Parameters.AddWithValue("@notes",      txtNote.Text);
                    cmd.ExecuteNonQuery();
                }

                LoadGrid();

                var savedMsg = new frmSavedMsg();
                savedMsg.ShowDialog();
                if (savedMsg.Pressed == 1)
                    ClearFields();
                else if (savedMsg.Pressed == 3)
                    this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Index == -1)
                {
                    MessageBox.Show("اختر المخالفة ليتم حذفها",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show("هل أنت متأكد من حذف المخالفة؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                if (conn.State != ConnectionState.Open) conn.Open();
                new SqlCommand(
                    $"UPDATE Violation SET IsDeleted=1 WHERE id={Index}", conn)
                    .ExecuteNonQuery();

                MessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Grid Selection

        private void dgvData_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvData.SelectedItem is ViolationModel selected)
            {
                if (int.TryParse(selected.id, out int id))
                {
                    Index = id;
                    Navigate($"SELECT * FROM Violation WHERE id={Index}");
                    txtno.Text = Index.ToString();
                }
            }
        }

        #endregion

        #region Navigation

        private void ReadData(SqlDataReader reader)
        {
            if (reader.HasRows && reader.Read())
            {
                ClearFields();

                if (double.TryParse(reader["id"].ToString(), out double idVal))
                    Index = (int)idVal;

                txtno.Text = Index.ToString();

                try
                {
                    cmbMarine.SelectedValue = reader["MarineId"];
                    txtPeriod.Text          = reader["Period"].ToString();
                    txtNote.Text            = reader["notes"].ToString();
                    txtViolType.Text        = reader["ViolatType"].ToString();

                    // ملاحظة: الحقل الأصلي كان "OVdate" في ReadData
                    string dateCol = "Vdate";
                    if (reader["Vdate"] != DBNull.Value &&
                        DateTime.TryParse(reader[dateCol].ToString(), out DateTime dt))
                        txtdate.DateTime = dt;
                }
                catch { }
            }
        }

        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvData.UnselectAll();
                var cmd = new SqlCommand(sqlQuery, conn);
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var reader = cmd.ExecuteReader())
                    ReadData(reader);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التنقل\n{ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Violation WHERE IsDeleted=0 ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Violation WHERE IsDeleted=0 ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Violation WHERE id>{Index} AND IsDeleted=0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Violation WHERE id<{Index} AND IsDeleted=0 ORDER BY id DESC");
        }

        #endregion

        #region Other

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة
        }

        #endregion
    }

    #region Model

    public class ViolationModel
    {
        public string id         { get; set; }
        public string marineId   { get; set; }
        public string period     { get; set; }
        public string vdate      { get; set; }
        public string violatType { get; set; }
    }

    #endregion
}