using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmQueueM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private int  _rowSelected1 = -1;
        private int  _rowSelected2 = -1;
        private int  _indx         = -1;
        private int  _indxg        = -1;
        public  bool IsFound        = false;

        private ObservableCollection<OrderedMarineRow>   _orderedSource
            = new ObservableCollection<OrderedMarineRow>();
        private ObservableCollection<UnorderedMarineRow> _unorderedSource
            = new ObservableCollection<UnorderedMarineRow>();

        #endregion

        #region Constructor

        public frmQueueM()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            dgvOrdered.ItemsSource   = _orderedSource;
            dgvUnordered.ItemsSource = _unorderedSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroup();
            LoadNextNo();
            txtDate.DateTime = DateTime.Now;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadGroup()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name, code FROM GroupMarine ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        cmbGroup.ItemsSource       = dt.DefaultView;
                        cmbGroup.DisplayMemberPath = "code";
                        cmbGroup.SelectedValuePath = "id";
                        cmbGroup.SelectedIndex     = -1;
                    }
                }
            }
            catch { }
        }

        private void LoadNextNo()
        {
            txtNo.Text = "";
            int num = 1;
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM OperationPlan", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        num = dt.Rows.Count + 1;
                }
            }
            catch { }
            txtNo.Text = num.ToString();
        }

        private void LoadPlan()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM OperationPlan WHERE IsDeleted=0 AND GroupId={_indxg}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        IsFound    = true;
                        txtNo.Text = dt.Rows[0]["id"].ToString();
                    }
                }
            }
            catch { }
        }

        private void LoadGItems(string gCode)
        {
            try
            {
                ClearDgvs();

                // المرتبطة بالخطة
                using (var da = new SqlDataAdapter(
                    $"SELECT Marine.id, Marine.Name, Marine.MarineCode, OperPlanSub.orderNo " +
                    $"FROM Marine INNER JOIN OperPlanSub ON OperPlanSub.MarineID=Marine.id " +
                    $"WHERE Marine.Groupcode='{gCode}' AND Marine.IS_InPlan=1 " +
                    $"AND Marine.IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        LoadPlan();
                        foreach (DataRow row in dt.Rows)
                        {
                            _orderedSource.Add(new OrderedMarineRow
                            {
                                OrderNo    = Convert.ToInt32(row["orderNo"]),
                                Name       = row["Name"].ToString(),
                                MarineCode = row["MarineCode"].ToString(),
                                MarineId   = Convert.ToInt32(row["id"])
                            });
                        }
                    }
                }

                // غير المرتبطة
                int seqNo = 1;
                using (var da2 = new SqlDataAdapter(
                    $"SELECT id, Name, MarineCode FROM Marine " +
                    $"WHERE Groupcode='{gCode}' AND IS_InPlan=1 AND IS_Deleted=0 " +
                    $"AND Marine.id NOT IN (SELECT OperPlanSub.MarineID FROM OperPlanSub)",
                    _conn))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    foreach (DataRow row in dt2.Rows)
                    {
                        _unorderedSource.Add(new UnorderedMarineRow
                        {
                            SeqNo      = seqNo++,
                            Name       = row["Name"].ToString(),
                            MarineCode = row["MarineCode"].ToString(),
                            MarineId   = Convert.ToInt32(row["id"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private string GetGroupCode(int groupId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT code FROM GroupMarine WHERE id={groupId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetMarineName(int marineId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT Name FROM Marine WHERE IS_Deleted=0 AND id={marineId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetMarineCode(int marineId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT MarineCode FROM Marine WHERE IS_Deleted=0 AND id={marineId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        #endregion

        #region ComboBox Events

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                IsFound = false;
                if (cmbGroup.SelectedValue == null) return;
                _indxg = Convert.ToInt32(cmbGroup.SelectedValue);
                LoadNextNo();
                LoadGItems(GetGroupCode(_indxg));
            }
            catch { }
        }

        #endregion

        #region DataGrid Selection

        private void dgvOrdered_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rowSelected2 = dgvOrdered.SelectedIndex;
        }

        private void dgvUnordered_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rowSelected1 = dgvUnordered.SelectedIndex;
        }

        #endregion

        #region Transfer Buttons

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_rowSelected1 == -1 || _rowSelected1 >= _unorderedSource.Count)
            {
                DXMessageBox.Show("يجب تحديد المركب المراد إضافته",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selected = _unorderedSource[_rowSelected1];

            if (IsNotInPlan(selected.MarineId))
                InsertRowToPlan();
            else
                DXMessageBox.Show("المركب تم إدخاله في خطة سابقة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private bool IsNotInPlan(int marineId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT PlanId FROM OperPlanSub WHERE MarineId={marineId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        var answer = DXMessageBox.Show(
                            "المركب تم إدخاله في خطة سابقة، هل تريد إزالته من الخطة السابقة أولًا؟",
                            "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (answer == MessageBoxResult.Yes)
                        {
                            EnsureOpen(_conn);
                            using (var cmd = new SqlCommand(
                                $"DELETE FROM OperPlanSub " +
                                $"WHERE MarineId={marineId} AND PlanId={dt.Rows[0]["PlanId"]}",
                                _conn))
                                cmd.ExecuteNonQuery();
                            CloseConn(_conn);
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch { }
            return true;
        }

        private void InsertRowToPlan()
        {
            var selected = _unorderedSource[_rowSelected1];
            int newOrder = _orderedSource.Count + 1;

            _orderedSource.Add(new OrderedMarineRow
            {
                OrderNo    = newOrder,
                Name       = selected.Name,
                MarineCode = selected.MarineCode,
                MarineId   = selected.MarineId
            });

            _unorderedSource.Remove(selected);
            _rowSelected1 = -1;
        }

        private void btnExclu_Click(object sender, RoutedEventArgs e)
        {
            if (_rowSelected2 == -1 || _rowSelected2 >= _orderedSource.Count)
            {
                DXMessageBox.Show("يجب تحديد المركب المراد إخراجها",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            RemoveRowFromPlan();
            UpdateOrderNo();
        }

        private void RemoveRowFromPlan()
        {
            var selected = _orderedSource[_rowSelected2];
            int seqNo    = _unorderedSource.Count + 1;

            _unorderedSource.Add(new UnorderedMarineRow
            {
                SeqNo      = seqNo,
                Name       = selected.Name,
                MarineCode = selected.MarineCode,
                MarineId   = selected.MarineId
            });

            _orderedSource.Remove(selected);
            _rowSelected2 = -1;
        }

        private void btnInclAll_Click(object sender, RoutedEventArgs e) => InsertAllToPlan();
        private void btnExclAll_Click(object sender, RoutedEventArgs e) => RemoveAllFromPlan();

        private void InsertAllToPlan()
        {
            int orderNo = _orderedSource.Count + 1;
            foreach (var row in _unorderedSource)
            {
                _orderedSource.Add(new OrderedMarineRow
                {
                    OrderNo    = orderNo++,
                    Name       = row.Name,
                    MarineCode = row.MarineCode,
                    MarineId   = row.MarineId
                });
            }
            _unorderedSource.Clear();
        }

        private void RemoveAllFromPlan()
        {
            int seqNo = _unorderedSource.Count + 1;
            foreach (var row in _orderedSource)
            {
                _unorderedSource.Add(new UnorderedMarineRow
                {
                    SeqNo      = seqNo++,
                    Name       = row.Name,
                    MarineCode = row.MarineCode,
                    MarineId   = row.MarineId
                });
            }
            _orderedSource.Clear();
        }

        #endregion

        #region Up/Down Buttons

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgvOrdered.SelectedIndex;
            if (idx <= 0) return;
            MoveRow(idx, -1);
            UpdateOrderNo();
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgvOrdered.SelectedIndex;
            if (idx < 0 || idx >= _orderedSource.Count - 1) return;
            MoveRow(idx, 1);
            UpdateOrderNo();
        }

        private void MoveRow(int idx, int direction)
        {
            try
            {
                var item = _orderedSource[idx];
                _orderedSource.RemoveAt(idx);
                _orderedSource.Insert(idx + direction, item);
                dgvOrdered.SelectedIndex = idx + direction;
            }
            catch { }
        }

        private void UpdateOrderNo()
        {
            for (int i = 0; i < _orderedSource.Count; i++)
                _orderedSource[i].OrderNo = i + 1;
        }

        #endregion

        #region Action Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearDgvs();
            LoadNextNo();
            cmbGroup.SelectedIndex = -1;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) { }
        private void btnFirst_Click(object sender, RoutedEventArgs e) { }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { }
        private void btnNext_Click(object sender, RoutedEventArgs e) { }
        private void btnLast_Click(object sender, RoutedEventArgs e) { }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DXMessageBox.Show("هل أنت متأكد من حذف الخطة؟", "حذف",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

                EnsureOpen(_conn);
                using (var cmd1 = new SqlCommand(
                    $"DELETE FROM OperationPlan WHERE id={txtNo.Text}", _conn))
                    cmd1.ExecuteNonQuery();

                using (var cmd2 = new SqlCommand(
                    $"DELETE FROM OperPlanSub WHERE PlanID={txtNo.Text}", _conn))
                    cmd2.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearDgvs();
                LoadNextNo();
                cmbGroup.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف: " + ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) => Save();

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNo.Text)) return;

                int userId = MainClass.UserID;

                if (IsFound)
                {
                    if (DXMessageBox.Show("هل أنت متأكد من تعديل الخطة؟", "Title",
                        MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

                    EnsureOpen(_conn);

                    // تحديث الخطة
                    using (var updCmd = new SqlCommand(
                        $"UPDATE OperationPlan SET " +
                        $"GroupId='{cmbGroup.SelectedValue}', " +
                        $"userID='{userId}', " +
                        $"Date='{txtDate.DateTime.ToShortDateString()}', " +
                        $"notes='{txtNotes.Text}' " +
                        $"WHERE id={txtNo.Text}", _conn))
                        updCmd.ExecuteNonQuery();

                    // حذف التفاصيل القديمة
                    using (var delCmd = new SqlCommand(
                        $"DELETE FROM OperPlanSub WHERE PlanID={txtNo.Text}", _conn))
                        delCmd.ExecuteNonQuery();

                    // إدراج التفاصيل الجديدة
                    foreach (var row in _orderedSource)
                    {
                        using (var insCmd = new SqlCommand(
                            "INSERT INTO OperPlanSub(PlanID,MarineID,groupID,orderNo) " +
                            "VALUES(@PlanID,@MarineID,@groupID,@orderNo)", _conn))
                        {
                            insCmd.Parameters.Add("@PlanID",   SqlDbType.Int).Value = Convert.ToDouble(txtNo.Text);
                            insCmd.Parameters.Add("@MarineID", SqlDbType.Int).Value = (double)row.MarineId;
                            insCmd.Parameters.Add("@groupID",  SqlDbType.Int).Value = cmbGroup.SelectedValue;
                            insCmd.Parameters.Add("@orderNo",  SqlDbType.Int).Value = (double)row.OrderNo;
                            insCmd.ExecuteNonQuery();
                        }
                    }

                    DXMessageBox.Show("تم حفظ الخطة",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    EnsureOpen(_conn);

                    // إدراج الخطة الجديدة
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO OperationPlan(id,GroupId,userID,Date,notes,IsDeleted) " +
                        "VALUES(@id,@GroupId,@userID,@Date,@notes,@IsDeleted)", _conn))
                    {
                        insCmd.Parameters.Add("@id",        SqlDbType.Int).Value      = Convert.ToDouble(txtNo.Text);
                        insCmd.Parameters.Add("@GroupId",   SqlDbType.Int).Value      = cmbGroup.SelectedValue;
                        insCmd.Parameters.Add("@userID",    SqlDbType.Int).Value      = userId;
                        insCmd.Parameters.Add("@Date",      SqlDbType.DateTime).Value = txtDate.DateTime;
                        insCmd.Parameters.Add("@notes",     SqlDbType.NVarChar).Value = $" خطة {txtNo.Text}";
                        insCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value      = 0;
                        insCmd.ExecuteNonQuery();
                    }

                    // إدراج التفاصيل
                    foreach (var row in _orderedSource)
                    {
                        using (var subCmd = new SqlCommand(
                            "INSERT INTO OperPlanSub(PlanID,MarineID,groupID,orderNo) " +
                            "VALUES(@PlanID,@MarineID,@groupID,@orderNo)", _conn))
                        {
                            subCmd.Parameters.Add("@PlanID",   SqlDbType.Int).Value = Convert.ToDouble(txtNo.Text);
                            subCmd.Parameters.Add("@MarineID", SqlDbType.Int).Value = (double)row.MarineId;
                            subCmd.Parameters.Add("@groupID",  SqlDbType.Int).Value = cmbGroup.SelectedValue;
                            subCmd.Parameters.Add("@orderNo",  SqlDbType.Int).Value = (double)row.OrderNo;
                            subCmd.ExecuteNonQuery();
                        }
                    }

                    DXMessageBox.Show("تم حفظ الخطة",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearDgvs();
                LoadNextNo();
                cmbGroup.SelectedIndex = -1;
                IsFound = false;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { CloseConn(_conn); }
        }

        #endregion

        #region CLR

        private void ClearDgvs()
        {
            _orderedSource.Clear();
            _unorderedSource.Clear();
            txtNotes.Text = "";
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

        #region Models

        public class OrderedMarineRow : INotifyPropertyChanged
        {
            private int _orderNo;

            public int OrderNo
            {
                get => _orderNo;
                set
                {
                    _orderNo = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(OrderNo)));
                }
            }

            public string Name       { get; set; }
            public string MarineCode { get; set; }
            public int    MarineId   { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class UnorderedMarineRow : INotifyPropertyChanged
        {
            public int    SeqNo      { get; set; }
            public string Name       { get; set; }
            public string MarineCode { get; set; }
            public int    MarineId   { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        #endregion
    }
}