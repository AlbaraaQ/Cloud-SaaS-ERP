using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmProjCyclePM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Stage Inner Class

        private class Stage
        {
            public string Name  { get; set; }
            public string Id    { get; set; }
            public string Order { get; set; }
        }

        #endregion

        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int        ProjNo;
        private int        StageID;
        private List<Stage> Stageslist;
        private int        lastStage;
        private int        SelectedRow;

        // DataSources
        private ObservableCollection<ProjFileRow>    _filesSource;
        private ObservableCollection<ProjProjectRow> _projectsSource;
        private ObservableCollection<ProjSearchRow>  _searchSource;

        // FileTypes (للـ ComboBox داخل الجدول)
        private DataTable _fileTypesTable;

        #endregion

        #region Constructor

        public frmProjCyclePM()
        {
            InitializeComponent();

            conn       = MainClass.ConnObj();
            conn1      = MainClass.ConnObj();
            ProjNo     = 0;
            StageID    = -1;
            Stageslist = new List<Stage>();
            lastStage  = 1;
            SelectedRow = -1;

            _filesSource    = new ObservableCollection<ProjFileRow>();
            _projectsSource = new ObservableCollection<ProjProjectRow>();
            _searchSource   = new ObservableCollection<ProjSearchRow>();
            _fileTypesTable = new DataTable();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgvFiles.ItemsSource    = _filesSource;
            dgvProjects.ItemsSource = _projectsSource;
            dgvItems.ItemsSource    = _searchSource;

            rbProject.IsChecked = true;
            LoadStatus();
            LoadFileType();
        }

        #endregion

        #region Load Data

        private void LoadStatus()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id, name from PM_Status where IS_Deleted=0 order by id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbStatus.DisplayMemberPath = "name";
                cmbStatus.SelectedValuePath = "id";
                cmbStatus.ItemsSource       = dt.DefaultView;
                cmbStatus.SelectedIndex     = -1;

                var dt2 = new DataTable();
                adapter.Fill(dt2);
                cmbStatus1.DisplayMemberPath = "name";
                cmbStatus1.SelectedValuePath = "id";
                cmbStatus1.ItemsSource       = dt2.DefaultView;
                cmbStatus1.SelectedIndex     = -1;
            }
            catch { }
        }

        private void LoadFileType()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id, name from PM_FileType order by id", conn);
                _fileTypesTable = new DataTable();
                adapter.Fill(_fileTypesTable);
            }
            catch { }
        }

        private void LoadProjects()
        {
            try
            {
                string cond = $"ContractPK={txtNo.Text} and ";

                _projectsSource.Clear();

                var adapter = new SqlDataAdapter(
                    $"select PM_Projects.id as id, PM_Projects.name as ProjName, " +
                    $"PM_Status.name as statusName " +
                    $"from PM_Projects, PM_Status " +
                    $"where {cond} PM_Projects.IS_Deleted=0 " +
                    $"and PM_Projects.statusPK=PM_Status.id " +
                    $"order by PM_Projects.id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow r in dt.Rows)
                {
                    _projectsSource.Add(new ProjProjectRow
                    {
                        ProjId     = r["id"].ToString(),
                        ProjName   = r["ProjName"].ToString(),
                        StatusName = r["statusName"].ToString()
                    });
                }
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void LoadStageFile(int btnNo)
        {
            try
            {
                StageID = Convert.ToInt32(Stageslist[btnNo].Id);
                string stageName = Stageslist[btnNo].Name;

                _filesSource.Clear();

                var adapter = new SqlDataAdapter(
                    $"select * from PM_ProjectFiles where Proj_ID={ProjNo} " +
                    $"and StageNo={StageID} order by FileNo",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow r in dt.Rows)
                {
                    _filesSource.Add(new ProjFileRow
                    {
                        ProjId      = ProjNo,
                        StageName   = stageName,
                        FileName    = r["Fname"].ToString(),
                        FileTypeId  = r["Ftype"] != DBNull.Value ? Convert.ToInt32(r["Ftype"]) : 0,
                        FileNo      = r["FileNO"] != DBNull.Value ? Convert.ToInt32(r["FileNO"]) : 0,
                        UploadDate  = r["UploadDate"] != DBNull.Value
                                        ? Convert.ToDateTime(r["UploadDate"]).ToShortDateString() : "",
                        IsMain      = r["mainFile"] != DBNull.Value && Convert.ToBoolean(r["mainFile"]),
                        FilePath    = ""
                    });
                }
            }
            catch { }
        }

        private void LoadParentStages(int code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select PM_Stages.name, PM_Stages.nameEn, PM_GroupStages.StageID, " +
                    $"PM_GroupStages.StageOrder " +
                    $"from PM_GroupStages, PM_Stages " +
                    $"where PM_Stages.IS_Deleted=0 " +
                    $"and PM_Stages.id=PM_GroupStages.StageID " +
                    $"and PM_GroupStages.ProjGropID={code} " +
                    $"order by StageOrder",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    if (conn.State != ConnectionState.Open) conn.Open();
                    foreach (DataRow r in dt.Rows)
                    {
                        var cmd = new SqlCommand(
                            "insert into PM_ProjStages(StageId,ProjID,StageOrder,IsAccredit,IS_Deleted) " +
                            "values(@StageId,@ProjID,@StageOrder,0,0)", conn);
                        cmd.Parameters.Add("@StageId",    SqlDbType.Int).Value = r["StageID"];
                        cmd.Parameters.Add("@ProjID",     SqlDbType.Int).Value = ProjNo;
                        cmd.Parameters.Add("@StageOrder", SqlDbType.Int).Value = r["StageOrder"];
                        cmd.ExecuteNonQuery();
                    }
                    LoadProjectStage(code);
                }
                else
                {
                    int parent = GetParent(code);
                    if (parent == 0)
                        DXMessageBox.Show("يجب إدخال مراحل لمجموعة المشروع");
                    else
                        LoadParentStages(parent);
                }
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadProjectStage(int code)
        {
            Stageslist.Clear();

            try
            {
                var adapter = new SqlDataAdapter(
                    $"select PM_Stages.name, PM_Stages.nameEn, PM_ProjStages.StageID, " +
                    $"PM_ProjStages.StageOrder " +
                    $"from PM_ProjStages, PM_Stages " +
                    $"where PM_Stages.IS_Deleted=0 " +
                    $"and PM_Stages.id=PM_ProjStages.StageID " +
                    $"and PM_ProjStages.ProjID={ProjNo} " +
                    $"order by StageOrder",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        string stageName = string.Equals(MainClass.Language, "en",
                            StringComparison.OrdinalIgnoreCase)
                            ? r["nameEn"].ToString()
                            : r["name"].ToString();

                        Stageslist.Add(new Stage
                        {
                            Name  = stageName,
                            Id    = r["StageID"].ToString(),
                            Order = r["StageOrder"].ToString()
                        });
                    }

                    // إخفاء كل الأزرار أولاً
                    foreach (UIElement child in StagesPanel.Children)
                    {
                        if (child is Button btn)
                        {
                            btn.Content    = "";
                            btn.Visibility = Visibility.Collapsed;
                            btn.Background = Brushes.White;
                        }
                    }

                    // تفعيل أزرار المراحل
                    var stageBtns = new List<Button>();
                    foreach (UIElement child in StagesPanel.Children)
                    {
                        if (child is Button b) stageBtns.Add(b);
                    }

                    bool isClosed = cmbStatus.SelectedValue != null &&
                                    Convert.ToInt32(cmbStatus.SelectedValue) == 3;

                    for (int i = 0; i < Stageslist.Count && i < stageBtns.Count; i++)
                    {
                        stageBtns[i].Content    = Stageslist[i].Name;
                        stageBtns[i].Visibility = Visibility.Visible;
                        lastStage               = i + 1;

                        if (isClosed)
                            stageBtns[i].Background = Brushes.LightGreen;
                    }
                }
                else
                {
                    LoadParentStages(code);
                }
            }
            catch { }
        }

        #endregion

        #region Helper Methods

        private int GetParent(int code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"Select ParentCode from PM_Terms where Code={code}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch { return 0; }
        }

        private string GetContractorName(int id)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"Select name from PM_Contractor where id={id}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch { return ""; }
        }

        private void ChangeBtnColor(Button clickedBtn)
        {
            foreach (UIElement child in StagesPanel.Children)
            {
                if (child is Button btn)
                    btn.Background = Brushes.White;
            }
            if (clickedBtn != null)
                clickedBtn.Background = Brushes.LightGreen;
        }

        private bool CheckPreviousStage()
        {
            var adapter = new SqlDataAdapter(
                $"select * from PM_ProjectFiles where Proj_ID={ProjNo} and StageNo={StageID}",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count > 0) return true;
            DXMessageBox.Show("يجب إرفاق على الأقل ملف لكل مرحلة سابقة");
            return false;
        }

        private bool CheckAccreditatedStages()
        {
            foreach (var stage in Stageslist)
            {
                var adapter = new SqlDataAdapter(
                    $"select ProjID from PM_ProjStages where ProjID={ProjNo} " +
                    $"and StageId={stage.Id} and IsAccredit=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DXMessageBox.Show($"{stage.Order} يوجد مراحل غير معتمدة، مرحلة رقم");
                    return false;
                }
            }
            return true;
        }

        private void OpenFileForRow(ProjFileRow row)
        {
            try
            {
                if (cmbStatus.SelectedValue != null &&
                    Convert.ToInt32(cmbStatus.SelectedValue) == 3)
                {
                    DXMessageBox.Show("المشروع مغلق، لا يسمح بإضافة ملف جديد",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (row.FileTypeId <= 0)
                {
                    DXMessageBox.Show("يجب تحديد نوع الملف",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dlg = new Microsoft.Win32.OpenFileDialog();
                if (dlg.ShowDialog() == true)
                {
                    row.FilePath = dlg.FileName;
                    if (string.IsNullOrEmpty(row.FileName))
                        row.FileName = Path.GetFileName(dlg.FileName);
                }
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void SaveFileRow(ProjFileRow row)
        {
            try
            {
                if (row.FileNo == 0)
                {
                    // إضافة جديدة
                    if (cmbStatus.SelectedValue != null &&
                        Convert.ToInt32(cmbStatus.SelectedValue) == 3)
                    {
                        DXMessageBox.Show("المشروع مغلق، لا يسمح بإضافة ملف جديد",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (row.FileTypeId <= 0)
                    {
                        DXMessageBox.Show("يجب تحديد نوع الملف",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrEmpty(row.FilePath))
                    {
                        DXMessageBox.Show("يجب تحديد مسار الملف",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (DXMessageBox.Show("هل أنت متأكد من حفظ الملف؟", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    string fileName = string.IsNullOrEmpty(row.FileName)
                        ? Path.GetFileName(row.FilePath)
                        : row.FileName;

                    int mainFile = row.IsMain ? 1 : 0;

                    var sb = new StringBuilder();
                    sb.AppendLine("Declare @Ufile As VARBINARY(MAX)");
                    sb.AppendLine($"Select @Ufile = CAST(bulkcolumn As VARBINARY(MAX)) " +
                                  $"FROM OPENROWSET(BULK N'{row.FilePath}', SINGLE_BLOB) AS Document");
                    sb.AppendLine("INSERT INTO PM_ProjectFiles (FileId,UploadedFile,Fname,Ftype," +
                                  "Proj_ID,StageNo,UploadDate,MainFile) " +
                                  "SELECT NEWID(),@Ufile,@Fname,@Ftype,@Proj_ID,@StageNo,@UploadDate,@MainFile");

                    var cmd = new SqlCommand(sb.ToString(), conn);
                    cmd.Parameters.Add("@Fname",      SqlDbType.NVarChar).Value  = fileName;
                    cmd.Parameters.Add("@Ftype",      SqlDbType.Int).Value       = row.FileTypeId;
                    cmd.Parameters.Add("@Proj_ID",    SqlDbType.Int).Value       = ProjNo;
                    cmd.Parameters.Add("@StageNo",    SqlDbType.Int).Value       = StageID;
                    cmd.Parameters.Add("@UploadDate", SqlDbType.DateTime).Value  = DateTime.Now;
                    cmd.Parameters.Add("@MainFile",   SqlDbType.Bit).Value       = mainFile;
                    cmd.ExecuteNonQuery();

                    DXMessageBox.Show(string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "تم رفع الملف بنجاح"
                        : "File Saved Successfully..");
                }
                else
                {
                    // تعديل
                    if (DXMessageBox.Show("هل أنت متأكد من تعديل معلومات الملف؟", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    int mainFile = row.IsMain ? 1 : 0;
                    var cmd = new SqlCommand(
                        $"Update PM_ProjectFiles set Fname=@Fname, MainFile=@MainFile " +
                        $"where FileNo={row.FileNo}", conn);
                    cmd.Parameters.Add("@Fname",    SqlDbType.NVarChar).Value = row.FileName;
                    cmd.Parameters.Add("@MainFile", SqlDbType.Bit).Value      = mainFile;
                    cmd.ExecuteNonQuery();

                    DXMessageBox.Show("تم تعديل معلومات الملف");
                }
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadSavedFile(ProjFileRow row)
        {
            if (row.FileNo <= 0) return;

            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT UploadedFile,Fname,Ftype FROM PM_ProjectFiles where FileNo={row.FileNo}",
                    conn);
                var ds = new DataSet();
                adapter.Fill(ds);
                var dt = ds.Tables[0];

                if (dt.Rows.Count <= 0) return;

                byte[] data  = (byte[])dt.Rows[0]["UploadedFile"];
                int    ftype = Convert.ToInt32(dt.Rows[0]["Ftype"]);

                string ext = ftype switch
                {
                    1 => ".png",
                    2 => ".pdf",
                    3 => ".docx",
                    4 => ".mp4",
                    5 => ".xls",
                    _ => ".bin"
                };

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    $"file{ext}");

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                fs.Write(data, 0, data.Length);

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void LoadContractData()
        {
            try
            {
                double.TryParse(txtNo.Text, out double contrVal);
                var adapter = new SqlDataAdapter(
                    $"select * from PM_ContractInv where IS_Deleted=0 and ContrNo={contrVal}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return;

                txtContrNo.Text     = dt.Rows[0]["ContrNo"].ToString();
                txtNetCost.Text     = dt.Rows[0]["NetCost"].ToString();
                txtExecutePeriod.Text = dt.Rows[0]["ExcutePeriod"].ToString();

                if (dt.Rows[0]["statusPk"] != DBNull.Value)
                    cmbStatus1.SelectedValue = dt.Rows[0]["statusPk"];

                if (dt.Rows[0]["StartDate"] != DBNull.Value)
                    txtStartDate1.EditValue = Convert.ToDateTime(dt.Rows[0]["StartDate"]);

                if (dt.Rows[0]["EndDate"] != DBNull.Value)
                    txtEndDate1.EditValue = Convert.ToDateTime(dt.Rows[0]["EndDate"]);

                LoadProjects();
                TabControl1.SelectedIndex = 1;
            }
            catch { }
        }

        private void LoadProjectData()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select * from PM_Projects where id={ProjNo} and IS_Deleted=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return;

                txtProjectNo.Text = dt.Rows[0]["id"].ToString();
                txtPname.Text = string.Equals(MainClass.Language, "en",
                    StringComparison.OrdinalIgnoreCase)
                    ? dt.Rows[0]["nameEN"].ToString()
                    : dt.Rows[0]["name"].ToString();

                if (dt.Rows[0]["ContractorPk"] != DBNull.Value)
                    txtContractor.Text = GetContractorName(Convert.ToInt32(dt.Rows[0]["ContractorPk"]));

                if (dt.Rows[0]["statusPk"] != DBNull.Value)
                    cmbStatus.SelectedValue = dt.Rows[0]["statusPk"];

                if (dt.Rows[0]["TermPk"] != DBNull.Value)
                    cmbTerms.SelectedValue = dt.Rows[0]["TermPk"];

                if (dt.Rows[0]["StartDate"] != DBNull.Value)
                    txtStartDate.EditValue = Convert.ToDateTime(dt.Rows[0]["StartDate"]);

                if (dt.Rows[0]["EndDate"] != DBNull.Value)
                    txtEndDate.EditValue = Convert.ToDateTime(dt.Rows[0]["EndDate"]);

                if (dt.Rows[0]["TermPk"] != DBNull.Value)
                    LoadProjectStage(Convert.ToInt32(dt.Rows[0]["TermPk"]));
            }
            catch { }
        }

        private void SearchProjects()
        {
            try
            {
                _searchSource.Clear();

                string srchNo    = txtSrchNo.Text.Trim();
                string fromDate  = txtFromDate.EditValue != null
                    ? Convert.ToDateTime(txtFromDate.EditValue).ToShortDateString() : "";
                string toDate    = txtToDate.EditValue != null
                    ? Convert.ToDateTime(txtToDate.EditValue).AddHours(24).ToString() : "";

                string cond = "PM_Projects.IS_Deleted=0 ";

                if (!string.IsNullOrEmpty(srchNo))
                    cond += $" and PM_Projects.id={srchNo}";
                else if (!string.IsNullOrEmpty(fromDate))
                    cond += $" and PM_Projects.StartDate>='{fromDate}' and PM_Projects.StartDate<='{toDate}'";

                if (cmbClientSrch.SelectedIndex > -1)
                    cond += $" and PM_Projects.ContractorPk={cmbClientSrch.SelectedValue}";

                var adapter = new SqlDataAdapter(
                    $"select PM_Projects.id, PM_Projects.name as ProjName, " +
                    $"PM_Status.name as statusName " +
                    $"from PM_Projects " +
                    $"left join PM_Status on PM_Projects.statusPK=PM_Status.id " +
                    $"where {cond} order by PM_Projects.id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow r in dt.Rows)
                {
                    _searchSource.Add(new ProjSearchRow
                    {
                        ItemId     = r["id"].ToString(),
                        ProjNo     = r["id"].ToString(),
                        ProjName   = r["ProjName"].ToString(),
                        StatusName = r["statusName"].ToString()
                    });
                }
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        #endregion

        #region Button Handlers

        private void txtNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNo.Text)) return;

            if (!double.TryParse(txtNo.Text, out double val)) return;
            ProjNo = (int)Math.Round(val);

            if (rbProject.IsChecked == true)
                LoadProjectData();
            else
                LoadContractData();
        }

        private void btnSrch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNo.Text))
            {
                txtNo.Text = "";
                return;
            }

            if (rbContract.IsChecked == true)
            {
                var form = new frmContractSrchPM { Name = "" };
                form.ShowDialog();
                if (form.ContractNo > -1)
                    txtNo.Text = form.ContractNo.ToString();
            }
            else if (rbProject.IsChecked == true)
            {
                var form = new frmProjectSrchPM { Name = "" };
                form.ShowDialog();
                if (form.ProjectNo > -1)
                    txtNo.Text = form.ProjectNo.ToString();
            }
        }

        private void btnStage1_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(0);
        }

        private void btnStage2_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(1);
        }

        private void btnStage3_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(2);
        }

        private void btnStage4_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(3);
        }

        private void btnStage5_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(4);
        }

        private void btnStage6_Click(object sender, RoutedEventArgs e)
        {
            _filesSource.Clear();
            ChangeBtnColor(sender as Button);
            LoadStageFile(5);
        }

        private void btnAccreditateStage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckPreviousStage()) return;

                var adapter = new SqlDataAdapter(
                    $"select ProjID from PM_ProjStages where ProjID={ProjNo} " +
                    $"and StageId={StageID} and IsAccredit=1",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                { DXMessageBox.Show("المرحلة تم اعتمادها سابقًا"); return; }

                if (DXMessageBox.Show("هل أنت متأكد من اعتماد المرحلة؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                if (conn.State == ConnectionState.Closed) conn.Open();

                var cmd = new SqlCommand(
                    $"UPDATE PM_ProjStages SET IsAccredit=1, CloseDate=@CloseDate " +
                    $"where ProjID={ProjNo} and StageId={StageID}", conn);
                cmd.Parameters.Add("@CloseDate", SqlDbType.DateTime).Value =
                    DateTime.Now.ToShortDateString();
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم اعتماد المرحلة");
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnCloseProject_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckAccreditatedStages()) return;

            if (DXMessageBox.Show("هل أنت متأكد من إغلاق المشروع؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                if (conn.State == ConnectionState.Closed) conn.Open();

                var cmd = new SqlCommand(
                    $"UPDATE PM_Projects SET statusPk=@statusPk, EndDate=@EndDate " +
                    $"WHERE id={txtProjectNo.Text}", conn);
                cmd.Parameters.Add("@statusPK", SqlDbType.Int).Value      = 3;
                cmd.Parameters.Add("@EndDate",  SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم إغلاق المشروع");
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnCloseContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State == ConnectionState.Closed) conn.Open();

                string cond = $"ContractPK={txtContrNo.Text} and statusPK<3 and ";

                var adapter = new SqlDataAdapter(
                    $"select id from PM_Projects where {cond} IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                { DXMessageBox.Show("يوجد مشروع أو أكثر تحت التنفيذ ضمن العقد"); return; }

                if (DXMessageBox.Show("هل أنت متأكد من إغلاق العقد؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                var cmd = new SqlCommand(
                    $"UPDATE PM_ContractInv SET statusPk=@statusPk, EndDate=@EndDate " +
                    $"WHERE ContrNo={txtContrNo.Text}", conn);
                cmd.Parameters.Add("@statusPK", SqlDbType.Int).Value      = 3;
                cmd.Parameters.Add("@EndDate",  SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم إغلاق العقد");
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => SearchProjects();

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            // تنقل: يُكمل حسب منطق التطبيق
        }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { }
        private void btnNext_Click(object sender, RoutedEventArgs e) { }
        private void btnLast_Click(object sender, RoutedEventArgs e) { }
        private void btnSave_Click(object sender, RoutedEventArgs e) { }
        private void btnPrint_Click(object sender, RoutedEventArgs e) { }
        private void btnDelete_Click(object sender, RoutedEventArgs e) { }
        private void btnNew_Click(object sender, RoutedEventArgs e) { }
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // استعراض المشروع
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            // استعراض العقد
        }

        private void rbProject_Checked(object sender, RoutedEventArgs e) { }
        private void rbContract_Checked(object sender, RoutedEventArgs e) { }

        #endregion

        #region DataGrid Events

        // dgvFiles
        private void dgvFiles_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

        private void dgvFiles_ViewFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjFileRow row)
            {
                if (row.FileNo > 0)
                    LoadSavedFile(row);
                else
                    OpenFileForRow(row);
            }
        }

        private void dgvFiles_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjFileRow row)
            {
                if (StageID >= 1)
                    SaveFileRow(row);
            }
        }

        private void dgvFiles_DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjFileRow row)
            {
                if (DXMessageBox.Show("هل تريد حذف الملف؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _filesSource.Remove(row);
            }
        }

        // ComboBox نوع الملف داخل الجدول
        private void cmbFileType_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cmb)
            {
                cmb.DisplayMemberPath = "name";
                cmb.SelectedValuePath = "id";
                cmb.ItemsSource       = _fileTypesTable.DefaultView;
            }
        }

        // dgvProjects
        private void dgvProjects_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvProjects.SelectedItem is ProjProjectRow row)
            {
                if (int.TryParse(row.ProjId, out int id))
                {
                    rbProject.IsChecked = true;
                    txtNo.Text          = id.ToString();
                    TabControl1.SelectedIndex = 0;
                }
            }
        }

        // dgvItems (البحث)
        private void dgvItems_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

        private void dgvItems_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProjSearchRow row)
            {
                if (DXMessageBox.Show("هل تريد حذف السجل؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _searchSource.Remove(row);
            }
        }

        // chkAll
        private void chkAll_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbClientSrch != null)
            {
                cmbClientSrch.SelectedIndex = -1;
                cmbClientSrch.IsEnabled     = false;
            }
        }

        private void chkAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (cmbClientSrch != null)
                cmbClientSrch.IsEnabled = true;
        }

        #endregion
    }

    #region Model Classes

    public class ProjFileRow : INotifyPropertyChanged
    {
        private string _stageName = "", _fileName = "", _filePath = "", _uploadDate = "";
        private int    _projId, _fileNo, _fileTypeId;
        private bool   _isMain;
        private string _fileTypeName = "";

        public int    ProjId       { get => _projId;       set { _projId = value;       OnPC(nameof(ProjId)); } }
        public string StageName    { get => _stageName;    set { _stageName = value;    OnPC(nameof(StageName)); } }
        public string FileName     { get => _fileName;     set { _fileName = value;     OnPC(nameof(FileName)); } }
        public int    FileTypeId   { get => _fileTypeId;   set { _fileTypeId = value;   OnPC(nameof(FileTypeId)); } }
        public string FileTypeName { get => _fileTypeName; set { _fileTypeName = value; OnPC(nameof(FileTypeName)); } }
        public int    FileNo       { get => _fileNo;       set { _fileNo = value;       OnPC(nameof(FileNo)); } }
        public string UploadDate   { get => _uploadDate;   set { _uploadDate = value;   OnPC(nameof(UploadDate)); } }
        public bool   IsMain       { get => _isMain;       set { _isMain = value;       OnPC(nameof(IsMain)); } }
        public string FilePath     { get => _filePath;     set { _filePath = value;     OnPC(nameof(FilePath)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPC(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class ProjProjectRow : INotifyPropertyChanged
    {
        public string ProjId     { get; set; } = "";
        public string ProjName   { get; set; } = "";
        public string Quantity   { get; set; } = "";
        public string Cost       { get; set; } = "";
        public string Total      { get; set; } = "";
        public string TaxVal     { get; set; } = "";
        public string NetVal     { get; set; } = "";
        public string StatusName { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ProjSearchRow : INotifyPropertyChanged
    {
        public string ItemId     { get; set; } = "";
        public string ProjNo     { get; set; } = "";
        public string ProjName   { get; set; } = "";
        public string Quantity   { get; set; } = "";
        public string Cost       { get; set; } = "";
        public string Total      { get; set; } = "";
        public string TaxVal     { get; set; } = "";
        public string NetVal     { get; set; } = "";
        public string StatusName { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
    }

    #endregion
}