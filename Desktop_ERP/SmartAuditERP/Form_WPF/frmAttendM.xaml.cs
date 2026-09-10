// frmAttendM.xaml.cs
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAttendM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int rowSelected1 = -1;
        private int rowSelected2 = -1;
        private int indx = -1;
        private int indxg = -1;
        public bool IsFound = false;

        private int Aday, AMonth, Ayear, Ahour, Aminute;

        private ObservableCollection<AttendMRowModel> attendRows
            = new ObservableCollection<AttendMRowModel>();

        // Timer لوميض الصفوف
        private DispatcherTimer _blinkTimer;

        #endregion

        #region Constructor

        public frmAttendM()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgvAttendM.ItemsSource = attendRows;
            LoadGroup();
            LoadNextNo();
            txtDate.EditValue = DateTime.Today;

            // تهيئة Timer للوميض
            _blinkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _blinkTimer.Tick += Timer1_Tick;
            _blinkTimer.Start();
        }

        #endregion

        #region Load Methods

        private void LoadGroup()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select id,name,code from GroupMarine order by id", conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbGroup.ItemsSource = dt.DefaultView;
                    cmbGroup.DisplayMemberPath = "code";
                    cmbGroup.SelectedValuePath = "id";
                    cmbGroup.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void LoadPlan()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select * from OperationPlan where IsDeleted=0 and GroupId=" + indxg,
                    conn);
                var dt = new DataTable();
                da.Fill(dt);
                IsFound = dt.Rows.Count > 0;
            }
            catch { }
        }

        private void LoadLastRest()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select max(LastRest) from MarineOperPeriod where groupId=" + indxg,
                    conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0
                    && dt.Rows[0][0] != DBNull.Value
                    && DateTime.TryParse(dt.Rows[0][0].ToString(), out DateTime lastDate))
                    txtLastDate.EditValue = lastDate;
                else
                    txtLastDate.EditValue = DateTime.Today;
            }
            catch { }
        }

        private int LoadOrderNo(int id)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select * from OperPlanSub where MarineID=" + id, conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0
                    ? Convert.ToInt32(dt.Rows[0]["orderNo"])
                    : 0;
            }
            catch { return 0; }
        }

        private void LoadNextNo()
        {
            try
            {
                var da = new SqlDataAdapter("select * from OperationPlan", conn);
                var dt = new DataTable();
                da.Fill(dt);
                int nextNo = dt.Rows.Count + 1;
                // الرقم التالي محسوب ومتاح للاستخدام عند الحاجة
            }
            catch { }
        }

        private void LoadAttend(int gid)
        {
            try
            {
                var selectedDate = GetSelectedDate();
                int year = selectedDate.Year;
                int month = selectedDate.Month;
                int day = selectedDate.Day;

                string sql;
                if (ckOutPlan.IsChecked == true)
                {
                    sql = "select Marine.ID as MarineID from Marine " +
                          "where Marine.Groupcode='" + cmbGroup.Text +
                          "' and Marine.IS_InPlan=0 " +
                          "and Marine.ID IN(" +
                          "SELECT _enrollNumber from Attendance " +
                          "where Attendance._inOutMode=0 " +
                          "And Attendance._year=" + year +
                          " and Attendance._month=" + month +
                          " and Attendance._day=" + day +
                          ") order by Marine.id";
                }
                else
                {
                    sql = "select OperPlanSub.MarineID," +
                          "Attendance._enrollNumber," +
                          "Attendance._statu," +
                          "Attendance._inOutMode " +
                          "from OperPlanSub " +
                          "INNER JOIN Attendance " +
                          "ON Attendance._enrollNumber=OperPlanSub.MarineID " +
                          "where OperPlanSub.groupID=" + gid +
                          " and Attendance._inOutMode=0 " +
                          "And Attendance._year=" + year +
                          " and Attendance._month=" + month +
                          " and Attendance._day=" + day +
                          " order by OperPlanSub.orderNo";
                }

                var da = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                da.Fill(dt);

                int idx = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int marineId = Convert.ToInt32(row["MarineID"]);
                    attendRows.Add(new AttendMRowModel
                    {
                        RoleNumber = idx,
                        BoatName = GetMarineName(marineId),
                        BoatNumber = marineId.ToString(),
                        Id = marineId,
                        InOutMode = 0,
                        RowColor = Brushes.Green,
                        ForeColor = Brushes.Gold
                    });
                    idx++;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void LoadAbsent(int gid)
        {
            try
            {
                var selectedDate = GetSelectedDate();
                int year = selectedDate.Year;
                int month = selectedDate.Month;
                int day = selectedDate.Day;

                string sql;
                if (ckOutPlan.IsChecked == true)
                {
                    sql = "select Marine.ID as MarineID from Marine " +
                          "where Marine.Groupcode='" + cmbGroup.Text +
                          "' and Marine.IS_InPlan=0 " +
                          "and Marine.ID NOT IN(" +
                          "SELECT _enrollNumber from Attendance " +
                          "where Attendance._inOutMode=0 " +
                          "And Attendance._year=" + year +
                          " and Attendance._month=" + month +
                          " and Attendance._day=" + day +
                          ") order by Marine.id";
                }
                else
                {
                    sql = "select MarineID from OperPlanSub " +
                          "where groupID=" + gid +
                          " and MarineID NOT IN(" +
                          "SELECT _enrollNumber from Attendance " +
                          "where Attendance._inOutMode=0 " +
                          "And Attendance._year=" + year +
                          " and Attendance._month=" + month +
                          " and Attendance._day=" + day +
                          ") order by orderNo";
                }

                var da = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                da.Fill(dt);

                int startIndex = attendRows.Count;
                foreach (DataRow row in dt.Rows)
                {
                    int marineId = Convert.ToInt32(row["MarineID"]);
                    attendRows.Add(new AttendMRowModel
                    {
                        RoleNumber = startIndex + 1,
                        BoatName = GetMarineName(marineId),
                        BoatNumber = marineId.ToString(),
                        Id = marineId,
                        InOutMode = 1,
                        RowColor = Brushes.White,
                        ForeColor = Brushes.Black
                    });
                    startIndex++;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;
                dr.Read();

                attendRows.Clear();

                if (cmbGroup.Items.Count > 0)
                    cmbGroup.SelectedValue = dr["GroupId"];

                if (DateTime.TryParse(dr["Date"].ToString(), out DateTime d))
                    txtDate.EditValue = d;

                dr.Close();

                var da = new SqlDataAdapter(
                    "select * from OperPlanSub where PlanID=" + indx, conn);
                var dt = new DataTable();
                da.Fill(dt);

                int i = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int marineId = Convert.ToInt32(row["MarineID"]);
                    attendRows.Add(new AttendMRowModel
                    {
                        RoleNumber = Convert.ToDouble(row["orderNo"]) is double rn
                                      ? (int)rn : i,
                        BoatName = GetMarineName(marineId),
                        BoatNumber = GetMarineCode(marineId),
                        Id = indx,
                        InOutMode = 0,
                        RowColor = Brushes.White,
                        ForeColor = Brushes.Black
                    });
                    i++;
                }
            }
            catch { }
        }

        #endregion

        #region Row Color + Blink (Timer)

        /// <summary>
        /// تلوين الصفوف حسب قيمة RoleNumber (بديل RowsColor() الأصلية)
        /// </summary>
        public void RowsColor()
        {
            foreach (var row in attendRows)
            {
                int val = row.RoleNumber;
                if (val < 5)
                {
                    MessageBox.Show(val.ToString(), "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    row.RowColor = Brushes.Red;
                }
                else if (val >= 5 && val < 10)
                {
                    row.RowColor = Brushes.Orange;
                }
                else if (val >= 10 && val < 15)
                {
                    row.RowColor = Brushes.Yellow;
                }
                else
                {
                    row.RowColor = Brushes.LightGreen;
                }
            }
        }

        /// <summary>
        /// وميض الصفوف التي InOutMode=0 بين الأحمر والأبيض
        /// </summary>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            foreach (var row in attendRows)
            {
                if (row.InOutMode == 0)
                {
                    row.RowColor = row.RowColor == Brushes.Red
                        ? Brushes.White
                        : Brushes.Red;
                }
            }
        }

        #endregion

        #region Move Row Up/Down

        /// <summary>
        /// تحريك صف لأعلى أو أسفل
        /// بديل MoveRow(int i) الأصلية
        /// </summary>
        private void MoveRow(int direction)
        {
            try
            {
                if (rowSelected2 < 0) return;
                int newIndex = rowSelected2 + direction;
                if (newIndex < 0 || newIndex >= attendRows.Count) return;

                var item = attendRows[rowSelected2];
                attendRows.RemoveAt(rowSelected2);
                attendRows.Insert(newIndex, item);
                rowSelected2 = newIndex;
                dgvAttendM.SelectedIndex = newIndex;
                UpdateOrderNo();
            }
            catch { }
        }

        private void btn_Up_Click(object sender, RoutedEventArgs e)
        {
            if (rowSelected2 == 0) return;
            MoveRow(-1);
        }

        private void btn_Down_Click(object sender, RoutedEventArgs e)
        {
            if (rowSelected2 == attendRows.Count - 1) return;
            MoveRow(1);
        }

        /// <summary>
        /// تحديث أرقام الترتيب (RoleNumber) بعد التحريك
        /// </summary>
        private void UpdateOrderNo()
        {
            for (int i = 0; i < attendRows.Count; i++)
                attendRows[i].RoleNumber = i + 1;
        }

        #endregion

        #region btnExclu (إخراج من الخطة)

        private void btnExclu_Click(object sender, RoutedEventArgs e)
        {
            if (rowSelected2 == -1)
            {
                MessageBox.Show("يجب تحديد المركب المراد إخراجها", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedRow();
            if (row == null) return;

            if (!string.IsNullOrWhiteSpace(row.BoatName))
            {
                try { UpdateOrderNo(); }
                catch { }
            }
        }

        #endregion

        #region btnNew

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearDgvs();
            LoadNextNo();
            cmbGroup.SelectedIndex = -1;
        }

        #endregion

        #region Helper Methods

        private DateTime GetSelectedDate()
        {
            return txtDate.EditValue is DateTime dt
                ? dt : DateTime.Today;
        }

        private string GetMarineName(int id)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select Name, MarineCode from Marine " +
                    "where IS_Deleted=0 and id=" + id, conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0
                    ? dt.Rows[0][0] + "  -  " + dt.Rows[0][1]
                    : "";
            }
            catch { return ""; }
        }

        private string GetMarineCode(int id)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select MarineCode from Marine " +
                    "where IS_Deleted=0 and id=" + id, conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetGroupCode(int id)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select code from GroupMarine " +
                    "where id=" + id + " order by id", conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private void ClearDgvs()
        {
            attendRows.Clear();
            rowSelected1 = -1;
            rowSelected2 = -1;
        }

        private void ReloadDgv()
        {
            ClearDgvs();
            if (cmbGroup.SelectedIndex <= -1) return;

            if (RdAttend.IsChecked == true)
                LoadAttend(indxg);
            else
                LoadAbsent(indxg);
        }

        private AttendMRowModel GetSelectedRow()
        {
            if (rowSelected2 < 0 || rowSelected2 >= attendRows.Count)
                return null;
            return attendRows[rowSelected2];
        }

        private static void ShowError(string title, string detail)
        {
            bool isAr = string.Equals(MainClass.Language, "ar",
                StringComparison.OrdinalIgnoreCase);
            string msg = isAr
                ? title + Environment.NewLine + "تفاصيل الخطأ: " + detail
                : "Error in loading" + Environment.NewLine +
                  "Error details: " + detail;
            MessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Marine Operations

        private void MarineExit(int marineId)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var transaction = conn.BeginTransaction();

                var selectedDate = GetSelectedDate();
                Ayear = selectedDate.Year;
                AMonth = selectedDate.Month;
                Aday = selectedDate.Day;

                var cmd = new SqlCommand(
                    "update Attendance set _inOutMode=1 " +
                    "Where _enrollNumber=" + marineId +
                    " And _year=" + Ayear +
                    " And _month=" + AMonth +
                    " And _day=" + Aday,
                    conn, transaction);
                cmd.ExecuteNonQuery();
                transaction.Commit();

                var row = GetSelectedRow();
                if (row != null) attendRows.Remove(row);
                rowSelected2 = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private void MarineAttend(int marineId)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var transaction = conn.BeginTransaction();

                var selectedDate = GetSelectedDate();
                Ayear = selectedDate.Year;
                AMonth = selectedDate.Month;
                Aday = selectedDate.Day;
                Ahour = DateTime.Now.Hour;
                Aminute = DateTime.Now.Minute;

                var cmd = new SqlCommand("AddGdata", conn, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.Add("@_machineNumber", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@_enrollNumber", SqlDbType.Int).Value = marineId;
                cmd.Parameters.Add("@_enrollMachineNumber", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@_verifyMode", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@_inOutMode", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@_year", SqlDbType.Int).Value = Ayear;
                cmd.Parameters.Add("@_month", SqlDbType.Int).Value = AMonth;
                cmd.Parameters.Add("@_day", SqlDbType.Int).Value = Aday;
                cmd.Parameters.Add("@_hour", SqlDbType.Int).Value = Ahour;
                cmd.Parameters.Add("@_minute", SqlDbType.Int).Value = Aminute;
                cmd.Parameters.Add("@takeJoureny", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@_statu", SqlDbType.Bit).Value = 0;
                cmd.ExecuteNonQuery();
                transaction.Commit();

                var row = GetSelectedRow();
                if (row != null) attendRows.Remove(row);
                rowSelected2 = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region Button Events

        private void btnManualAttend_Click(object sender, RoutedEventArgs e)
        {
            if (rowSelected2 == -1)
            {
                MessageBox.Show("يجب تحديد المركب المطلوب تحضيره", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedRow();
            if (row == null) return;

            if (row.InOutMode == 0)
            {
                MessageBox.Show("المركب المطلوب حاضر مسبقاً", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(row.BoatName)) return;

            var result = MessageBox.Show(
                "هل أنت متأكد من تحضير المركب؟", "تحذير",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                MarineAttend(row.Id);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (rowSelected2 == -1)
            {
                MessageBox.Show("يجب تحديد المركب المراد إخراجه", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var row = GetSelectedRow();
            if (row == null) return;

            if (row.InOutMode == 1)
            {
                MessageBox.Show("المركب المراد إخراجه لم يتم تسجيل حضوره", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(row.BoatName)) return;

            var result = MessageBox.Show(
                "هل أنت متأكد من إخراج المركب؟", "تحذير",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                MarineExit(row.Id);
        }

        private void btnResetPeriod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب تحديد الفئة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var frmPwd = new frmCheckPwd { operNo = 6 };
                frmPwd.ShowDialog();
                if (!frmPwd.Iscorrect) return;

                var result = MessageBox.Show(
                    "هل أنت متأكد من إعادة إحتساب الرحلات؟", "تحذير",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                var cmd = new SqlCommand(
                    "INSERT INTO MarineOperPeriod(groupID,LastRest,[user])" +
                    "VALUES(@groupID,@LastRest,@user)", conn);
                cmd.Parameters.Add("@groupID", SqlDbType.Int).Value =
                    cmbGroup.SelectedValue;
                cmd.Parameters.Add("@LastRest", SqlDbType.DateTime).Value =
                    DateTime.Now;
                cmd.Parameters.Add("@user", SqlDbType.Int).Value =
                    MainClass.EmpNo;
                cmd.ExecuteNonQuery();

                MessageBox.Show("تم إعادة إحتساب الفترة للفئة", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region ComboBox / RadioButton Events

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedValue == null) return;
                IsFound = false;
                indxg = Convert.ToInt32(cmbGroup.SelectedValue);
                RdAttend.IsChecked = true;
                ClearDgvs();
                LoadLastRest();
                LoadAttend(indxg);
            }
            catch { }
        }

        private void ckInPlan_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbGroup.SelectedIndex > -1)
                ReloadDgv();
        }

        private void ckOutPlan_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbGroup.SelectedIndex > -1)
                ReloadDgv();
        }

        private void RdAttend_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbGroup.SelectedIndex > -1)
            {
                ClearDgvs();
                LoadAttend(indxg);
            }
        }

        private void RbAbsent_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbGroup.SelectedIndex > -1)
            {
                ClearDgvs();
                LoadAbsent(indxg);
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvAttendM_CellClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvAttendM.SelectedItem is AttendMRowModel row)
                {
                    rowSelected2 = attendRows.IndexOf(row);
                    rowSelected1 = rowSelected2;
                }
            }
            catch { }
        }

        #endregion

        #region Window Closing

        protected override void OnClosed(EventArgs e)
        {
            _blinkTimer?.Stop();
            base.OnClosed(e);
        }

        #endregion
    }

    #region Model Class

    public class AttendMRowModel : System.ComponentModel.INotifyPropertyChanged
    {
        private int _roleNumber;
        private string _boatName;
        private string _boatNumber;
        private int _id;
        private int _inOutMode;
        private Brush _rowColor;
        private Brush _foreColor;

        public int RoleNumber
        {
            get => _roleNumber;
            set { _roleNumber = value; OnPropertyChanged(nameof(RoleNumber)); }
        }
        public string BoatName
        {
            get => _boatName;
            set { _boatName = value; OnPropertyChanged(nameof(BoatName)); }
        }
        public string BoatNumber
        {
            get => _boatNumber;
            set { _boatNumber = value; OnPropertyChanged(nameof(BoatNumber)); }
        }
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }
        public int InOutMode
        {
            get => _inOutMode;
            set { _inOutMode = value; OnPropertyChanged(nameof(InOutMode)); }
        }
        public Brush RowColor
        {
            get => _rowColor;
            set { _rowColor = value; OnPropertyChanged(nameof(RowColor)); }
        }
        public Brush ForeColor
        {
            get => _foreColor;
            set { _foreColor = value; OnPropertyChanged(nameof(ForeColor)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
               new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    #endregion
}