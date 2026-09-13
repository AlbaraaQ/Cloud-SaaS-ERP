using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmProjectStagesPM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private int  _code        = -1;
        private int  _rowSelected1 = -1;
        private int  _rowSelected2 = -1;
        public  bool IsDone        = false;

        private ObservableCollection<StageRow>    _stagesSource    = new ObservableCollection<StageRow>();
        private ObservableCollection<GrpStageRow> _grpStagesSource = new ObservableCollection<GrpStageRow>();

        public bool IsAdmin => (MainClass.EmpNo == 0);

        #endregion

        #region Constructor

        public frmProjectStagesPM()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            dgvStages.ItemsSource    = _stagesSource;
            dgvGrpStages.ItemsSource = _grpStagesSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStages();
            LoadGroups();

            _stagesSource.Clear();
            _grpStagesSource.Clear();

            if (MainClass.EmpNo == 0)
            {
                btnAddstage.Visibility = Visibility.Visible;
                LblStatus.Visibility   = Visibility.Visible;
            }
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region Load Data

        private void LoadGroups()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms " +
                    "WHERE Type=1 ORDER BY Code", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbGroup.ItemsSource       = dt.DefaultView;
                    cmbGroup.DisplayMemberPath = "name";
                    cmbGroup.SelectedValuePath = "Code";
                    cmbGroup.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadStages()
        {
            _stagesSource.Clear();
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM PM_Stages WHERE IS_Deleted=0 ORDER BY id",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        _stagesSource.Add(new StageRow
                        {
                            id     = Convert.ToInt32(row["id"]),
                            name   = row["name"].ToString(),
                            nameEn = row["nameEn"].ToString()
                        });
                    }
                }
            }
            catch { }
        }

        private void LoadGrpStages()
        {
            _grpStagesSource.Clear();
            int groupCode = 1;
            if (cmbGroup.SelectedIndex > -1)
            {
                try { groupCode = Convert.ToInt32(cmbGroup.SelectedValue); } catch { }
            }

            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT PM_GroupStages.StageID, PM_GroupStages.StageOrder, " +
                    $"PM_Stages.name, PM_Stages.nameEn " +
                    $"FROM PM_GroupStages, PM_Stages " +
                    $"WHERE PM_Stages.IS_Deleted=0 " +
                    $"AND PM_Stages.id=PM_GroupStages.StageID " +
                    $"AND PM_GroupStages.ProjGropID={groupCode} " +
                    $"ORDER BY StageOrder",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        _grpStagesSource.Add(new GrpStageRow
                        {
                            StageID    = Convert.ToInt32(row["StageID"]),
                            StageOrder = Convert.ToInt32(row["StageOrder"]),
                            name       = row["name"].ToString(),
                            nameEn     = row["nameEn"].ToString()
                        });
                    }
                }
            }
            catch { }
        }

        #endregion

        #region ComboBox Events

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex > -1)
                {
                    LoadStages();
                    LoadGrpStages();
                }
            }
            catch { }
        }

        #endregion

        #region DataGrid Selection Events

        private void dgvStages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rowSelected1 = dgvStages.SelectedIndex;
        }

        private void dgvGrpStages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _rowSelected2 = dgvGrpStages.SelectedIndex;
        }

        #endregion

        #region Transfer Buttons

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGroup.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب تحديد المجموعة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbGroup.Focus();
                return;
            }

            if (_rowSelected1 == -1 || _rowSelected1 >= _stagesSource.Count)
            {
                DXMessageBox.Show("يجب تحديد المرحلة المراد إضافتها",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InsertRowToPlan();
        }

        private void btnExclu_Click(object sender, RoutedEventArgs e)
        {
            if (_rowSelected2 == -1 || _rowSelected2 >= _grpStagesSource.Count)
            {
                DXMessageBox.Show("يجب تحديد المرحلة المراد إلغاها",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RemoveRowFromPlan();
            UpdateOrderNo();
        }

        private void btnInclAll_Click(object sender, RoutedEventArgs e) => InsertAllToPlan();
        private void btnExclAll_Click(object sender, RoutedEventArgs e) => RemoveAllFromPlan();

        private void InsertRowToPlan()
        {
            var selectedStage = _stagesSource[_rowSelected1];

            // التحقق من عدم التكرار
            foreach (var row in _grpStagesSource)
            {
                if (row.StageID == selectedStage.id)
                {
                    DXMessageBox.Show("المرحلة المحددة موجودة ضمن مراحل المجموعة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            int newOrder = _grpStagesSource.Count + 1;
            _grpStagesSource.Add(new GrpStageRow
            {
                StageID    = selectedStage.id,
                StageOrder = newOrder,
                name       = selectedStage.name,
                nameEn     = selectedStage.nameEn
            });

            _stagesSource.Remove(selectedStage);
            _rowSelected1 = -1;
        }

        private void RemoveRowFromPlan()
        {
            var selectedGrp = _grpStagesSource[_rowSelected2];

            // البحث هل الصنف موجود في المراحل الأصلية
            bool found = false;
            foreach (var s in _stagesSource)
            {
                if (s.id == selectedGrp.StageID) { found = true; break; }
            }

            if (!found)
            {
                _stagesSource.Add(new StageRow
                {
                    id     = selectedGrp.StageID,
                    name   = selectedGrp.name,
                    nameEn = selectedGrp.nameEn
                });
            }

            _grpStagesSource.Remove(selectedGrp);
            _rowSelected2 = -1;
        }

        private void InsertAllToPlan()
        {
            if (cmbGroup.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب تحديد المجموعة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbGroup.Focus();
                return;
            }

            int orderNo = _grpStagesSource.Count + 1;
            foreach (var stage in _stagesSource)
            {
                _grpStagesSource.Add(new GrpStageRow
                {
                    StageID    = stage.id,
                    StageOrder = orderNo++,
                    name       = stage.name,
                    nameEn     = stage.nameEn
                });
            }
            _stagesSource.Clear();
        }

        private void RemoveAllFromPlan()
        {
            foreach (var grp in _grpStagesSource)
            {
                _stagesSource.Add(new StageRow
                {
                    id     = grp.StageID,
                    name   = grp.name,
                    nameEn = grp.nameEn
                });
            }
            _grpStagesSource.Clear();
        }

        #endregion

        #region Up/Down Buttons

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgvGrpStages.SelectedIndex;
            if (idx <= 0) return;
            MoveRow(idx, -1);
            UpdateOrderNo();
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            int idx = dgvGrpStages.SelectedIndex;
            if (idx < 0 || idx >= _grpStagesSource.Count - 1) return;
            MoveRow(idx, 1);
            UpdateOrderNo();
        }

        private void MoveRow(int idx, int direction)
        {
            try
            {
                var item = _grpStagesSource[idx];
                _grpStagesSource.RemoveAt(idx);
                _grpStagesSource.Insert(idx + direction, item);
                dgvGrpStages.SelectedIndex = idx + direction;
            }
            catch { }
        }

        private void UpdateOrderNo()
        {
            for (int i = 0; i < _grpStagesSource.Count; i++)
                _grpStagesSource[i].StageOrder = i + 1;
        }

        #endregion

        #region Action Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            _stagesSource.Clear();
            _grpStagesSource.Clear();
            cmbGroup.SelectedIndex = -1;
            _code = -1;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        private void btnFirst_Click(object sender, RoutedEventArgs e) { }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { }
        private void btnNext_Click(object sender, RoutedEventArgs e) { }
        private void btnLast_Click(object sender, RoutedEventArgs e) { }

        private void btnDelete_Click(object sender, RoutedEventArgs e) { }

        private void btnAddstage_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmStagePM();
            frm.ShowDialog();
            if (frm.IsDone) LoadStages();
        }

        private void btnAddStatus_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmStatusPM();
            frm.Show();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار المجموعة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string confirmMsg = MainClass.Language == "en"
                    ? "Do you want to save group stages?"
                    : "هل تريد حفظ مراحل المجموعة؟";

                if (DXMessageBox.Show(confirmMsg, "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                EnsureOpen(_conn);
                int groupCode = Convert.ToInt32(cmbGroup.SelectedValue);

                using (var delCmd = new SqlCommand(
                    $"DELETE FROM PM_GroupStages WHERE ProjGropID={groupCode} AND IS_Deleted=0",
                    _conn))
                    delCmd.ExecuteNonQuery();

                foreach (var row in _grpStagesSource)
                {
                    using (var insCmd = new SqlCommand(
                        "INSERT INTO PM_GroupStages(StageId,ProjGropID,StageOrder,IS_Deleted) " +
                        "VALUES(@StageId,@ProjGropID,@StageOrder,0)", _conn))
                    {
                        insCmd.Parameters.Add("@StageId",    SqlDbType.NVarChar).Value = row.StageID;
                        insCmd.Parameters.Add("@ProjGropID", SqlDbType.NVarChar).Value = groupCode;
                        insCmd.Parameters.Add("@StageOrder", SqlDbType.Int).Value      = row.StageOrder;
                        insCmd.ExecuteNonQuery();
                    }
                }

                IsDone = true;

                var savedMsg = new frmSavedMsg();
                if (_code != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    _stagesSource.Clear();
                    _grpStagesSource.Clear();
                    _code = -1;
                }
                else if (savedMsg.Pressed == 3)
                    this.Close();
            }
            catch (Exception ex)
            {
                string errMsg = MainClass.Language == "en"
                    ? $"Error during saving\nError details: {ex.Message}"
                    : $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}";
                DXMessageBox.Show(errMsg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { CloseConn(_conn); }
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

        public class StageRow : System.ComponentModel.INotifyPropertyChanged
        {
            public int    id     { get; set; }
            public string name   { get; set; }
            public string nameEn { get; set; }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        }

        public class GrpStageRow : System.ComponentModel.INotifyPropertyChanged
        {
            private int _stageOrder;

            public int    StageID    { get; set; }
            public string name       { get; set; }
            public string nameEn     { get; set; }

            public int StageOrder
            {
                get => _stageOrder;
                set
                {
                    _stageOrder = value;
                    PropertyChanged?.Invoke(this,
                        new System.ComponentModel.PropertyChangedEventArgs(nameof(StageOrder)));
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        }

        #endregion
    }
}