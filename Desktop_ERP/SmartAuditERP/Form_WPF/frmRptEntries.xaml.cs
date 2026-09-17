using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptEntries : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private int    _code          = -1;
        private int    _resType       = -1;
        private string _globalId      = "-1";
        private string _receiptGlobalId = "";

        private bool   _printHeader   = true;
        private bool   _printFooter   = true;
        private bool   _printStamp    = true;
        private int    _printType     = 1;
        private int    _printNo       = 1;
        private string _defPrinter    = "";
        private string _rptName       = "Entry.repx";
        private string _rptUrl        = "";

        private ObservableCollection<EntrySearchRow> _searchSource
            = new ObservableCollection<EntrySearchRow>();
        private ObservableCollection<EntryDetailRow> _detailSource
            = new ObservableCollection<EntryDetailRow>();

        #endregion

        #region Constructor

        public frmRptEntries()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            dgvSrch.ItemsSource    = _searchSource;
            dgvDetails.ItemsSource = _detailSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.DateTime     = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;
            LoadCostCenters();
            LoadResType();
            LoadLastEntry();
            LoadPrintSettings();
            LoadBranches();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadBranches()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbBranches.ItemsSource       = dt.DefaultView;
                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";
                    cmbBranches.SelectedIndex     = -1;

                    cmbSrcBranch.ItemsSource       = dt.DefaultView;
                    cmbSrcBranch.DisplayMemberPath = "name";
                    cmbSrcBranch.SelectedValuePath = "id";
                    cmbSrcBranch.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadResType()
        {
            try
            {
                using (var da = new SqlDataAdapter("SELECT * FROM EntryTypes", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        cmbType.ItemsSource       = dt.DefaultView;
                        cmbType.DisplayMemberPath = "Name";
                        cmbType.SelectedValuePath = "Id";
                    }
                }
            }
            catch { }
        }

        private void LoadCostCenters()
        {
            // يُستخدم فقط في عمود مركز التكلفة بالجدول
        }

        private void LoadPrintSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=9", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try { _printType   = Convert.ToInt32(dt.Rows[0]["printType"]);     } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]); } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]); } catch { }
                        try { _printStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);  } catch { }
                        try { _defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();       } catch { }
                        try { _printNo     = Convert.ToInt32(dt.Rows[0]["printNo"]);       } catch { }
                        try { _rptName     = dt.Rows[0]["RptName"].ToString();             } catch { }
                        try { _rptUrl      = dt.Rows[0]["RptUrl"].ToString();              } catch { }

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                        return;
                    }
                    _rptUrl     = MainClass.ReportsPath;
                    _rptName    = "Entry.repx";
                    _defPrinter = MainClass.ReportsPrinter;
                }
            }
            catch { }
        }

        private void LoadLastEntry()
        {
            Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} IS_Deleted=0 ORDER BY date DESC");
        }

        private string GetBranchCondition()
        {
            return (MainClass.BranchNo > 1)
                ? $"branch={MainClass.BranchNo} AND " : "";
        }

        private string GetCCNameByCode(int ccCode)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM cost_center WHERE IS_deleted=0 AND code={ccCode}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : " ";
                }
            }
            catch { return " "; }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlQuery)
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
            finally { CloseConn(_conn); }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;
                dr.Read();

                _detailSource.Clear();
                _code     = Convert.ToInt32(dr["id"]);
                _globalId = dr["GlobalId"].ToString();

                txtEntryGID.Text = _globalId;
                txtNo.Text       = _code.ToString();
                txtDate.DateTime = Convert.ToDateTime(dr["date"]);
                _resType         = Convert.ToInt32(dr["type"]);
                txtResType.Text  = Common.ResrirectionType(_resType);

                try
                {
                    cmbBranches.SelectedValue = Convert.ToDouble(dr["branch"]);
                }
                catch { }

                try
                {
                    cmbState.SelectedIndex = Convert.ToInt32(dr["state"]) - 1;
                }
                catch { }

                txtNotes.Text = dr["notes"].ToString();

                // الحصول على رقم إيصال السند
                dr.Close();

                using (var da = new SqlDataAdapter(
                    $"SELECT GlobalId FROM Receipts WHERE EntryGlobalID=N'{_globalId}'",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    _receiptGlobalId = dt.Rows.Count > 0 ? dt.Rows[0]["GlobalId"].ToString() : "";
                }

                // تحميل بنود القيد
                double totDebt = 0, totCredit = 0;

                using (var da2 = new SqlDataAdapter(
                    $"SELECT * FROM Entry_sub WHERE EntryGlobalId=N'{_globalId}' ORDER BY id ASC",
                    _conn))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    int rowNo = 1;

                    foreach (DataRow row in dt2.Rows)
                    {
                        double dept   = Convert.ToDouble(row["dept"]);
                        double credit = Convert.ToDouble(row["credit"]);

                        string ccName = "";
                        if (row["CCcode"] != DBNull.Value)
                            ccName = GetCCNameByCode(Convert.ToInt32(row["CCcode"]));

                        _detailSource.Add(new EntryDetailRow
                        {
                            RowNo      = rowNo++,
                            Dept       = $"{dept:N2}",
                            Credit     = $"{credit:N2}",
                            AccCode    = row["acc_no"].ToString(),
                            AccName    = "", // يُحمَّل من اسم الحساب
                            CostCenter = ccName,
                            Notes      = row["notes"].ToString()
                        });

                        totDebt   += dept;
                        totCredit += credit;
                    }
                }

                txtTotDebt.Text   = $"{Math.Round(totDebt,   2):N2}";
                txtTotCredit.Text = $"{Math.Round(totCredit, 2):N2}";

                if (Math.Round(totDebt, 2) == Math.Round(totCredit, 2))
                {
                    txtISBalanced.Text       = "✅ قيد متوازن";
                    txtISBalanced.Background = new SolidColorBrush(
                        Color.FromRgb(173, 255, 47)); // GreenYellow
                    txtISBalanced.Foreground = new SolidColorBrush(Colors.DarkGreen);
                }
                else
                {
                    txtISBalanced.Text       = "❌ قيد غير متوازن";
                    txtISBalanced.Background = new SolidColorBrush(Colors.OrangeRed);
                    txtISBalanced.Foreground = new SolidColorBrush(Colors.White);
                }

                // تحميل اسم الحساب لكل بند
                foreach (var row in _detailSource)
                {
                    try
                    {
                        using (var da3 = new SqlDataAdapter(
                            $"SELECT AName FROM Accounts_Index WHERE Code={row.AccCode}", _conn))
                        {
                            var dt3 = new DataTable();
                            da3.Fill(dt3);
                            if (dt3.Rows.Count > 0)
                                row.AccName = dt3.Rows[0]["AName"].ToString();
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التحميل: " + ex.Message);
            }
        }

        #endregion

        #region Navigation Buttons

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} IS_Deleted=0 ORDER BY date ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} IS_Deleted=0 AND id<{_code} ORDER BY date DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} IS_Deleted=0 AND id>{_code} ORDER BY date ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} IS_Deleted=0 ORDER BY date DESC");

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void Search()
        {
            string cond = "";

            if (chkAllTypes.IsChecked != true && _resType > -1)
            {
                cond += $" AND type={_resType}";
                if (!string.IsNullOrWhiteSpace(txtReffNo.Text))
                    cond += $" AND doc_no={txtReffNo.Text}";
            }

            if (chkAll.IsChecked != true && !string.IsNullOrWhiteSpace(txtNoSrch.Text))
                cond += $" AND id={txtNoSrch.Text}";

            if (chkAllPeriod.IsChecked != true)
                cond += " AND date>=@date1 AND date<@date2 ";

            if (chkAllBranches.IsChecked != true && cmbSrcBranch.SelectedValue != null)
                cond += $" AND branch={cmbSrcBranch.SelectedValue}";

            LoadGrid(cond);
        }

        private void LoadGrid(string cond)
        {
            _searchSource.Clear();
            try
            {
                EnsureOpen(_conn);
                using (var da = new SqlDataAdapter(
                    $"SELECT GlobalID, id, date, doc_no, type, state, notes " +
                    $"FROM Entry WHERE IS_Deleted=0 {cond} ORDER BY id",
                    _conn))
                {
                    if (cond.Contains("@date1"))
                    {
                        da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtFromDate.DateTime.ToShortDateString();
                        da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = txtToDate.DateTime.AddHours(24).ToShortDateString();
                    }

                    var dt = new DataTable();
                    da.Fill(dt);

                    ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;
                    ProgressBar1.Value   = 0;

                    foreach (DataRow row in dt.Rows)
                    {
                        int typVal = Convert.ToInt32(row["type"]);
                        int state  = Convert.ToInt32(row["state"]);
                        string stateName = "";
                        try { stateName = cmbState.Items[state - 1] is ComboBoxItem ci
                            ? ci.Content.ToString() : state.ToString(); }
                        catch { stateName = state.ToString(); }

                        _searchSource.Add(new EntrySearchRow
                        {
                            GlobalID  = row["GlobalID"].ToString(),
                            DocNo     = row["doc_no"].ToString(),
                            EntryDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                            EntryType = Common.ResrirectionType(typVal),
                            EntryState= stateName,
                            Notes     = row["notes"].ToString(),
                            EntryId   = row["id"].ToString()
                        });

                        ProgressBar1.Value++;
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء البحث: " + ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is EntrySearchRow row)
            {
                _globalId = row.GlobalID;
                Navigate($"SELECT * FROM Entry WHERE GlobalID=N'{_globalId}'");
                TabControl1.SelectedIndex = 0;
                BtnShowSuorce.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region CheckBox Events

        private void chkAllPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (chkAllPeriod.IsChecked != true);
            txtFromDate.IsEnabled = enabled;
            txtToDate.IsEnabled   = enabled;
        }

        private void chkAllTypes_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool disabled = (chkAllTypes.IsChecked == true);
            cmbType.IsEnabled  = !disabled;
            txtReffNo.IsEnabled = !disabled;
        }

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSrcBranch.IsEnabled = (chkAllBranches.IsChecked != true);
        }

        private void chkAll_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            txtNoSrch.IsEnabled = (chkAll.IsChecked != true);
        }

        private void cmbType_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbType.SelectedValue != null)
                    _resType = Convert.ToInt32(cmbType.SelectedValue);
            }
            catch { }
        }

        private void txtReffNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtReffNo.Text))
                Search();
        }

        #endregion

        #region Action Buttons

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnView_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        private void BtnShowSuorce_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_code == -1)
                {
                    DXMessageBox.Show("لا توجد بيانات للعرض",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                switch (_resType)
                {
                    case 2:
                        OpenInvSale(2, 1);
                        break;
                    case 22:
                        OpenInvSale(2, 2);
                        break;
                    case 1:
                        OpenInvPurch(1, 1);
                        break;
                    case 21:
                        OpenInvPurch(1, 2);
                        break;
                    case 8:
                        OpenReceipt("frmSandSD");
                        break;
                    case 6:
                        OpenReceipt("frmSandD");
                        break;
                    case 7:
                        OpenReceipt("frmSandQD");
                        break;
                    case 5:
                        OpenReceipt("frmSandQ");
                        break;
                    case 10:
                        OpenNewEntry();
                        break;
                    case 27:
                        OpenSalaryReserved();
                        break;
                    default:
                        DXMessageBox.Show("لا يوجد نموذج مرتبط لهذا النوع من القيود");
                        break;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void OpenInvSale(int invType, int procType)
        {
            var frm = new frmInvSale
            {
                InvType  = invType,
                ProcType = procType
            };
            frm.Show();
            frm.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type={invType} " +
                         $"AND proc_type={procType} AND EntryID=N'{txtNo.Text.Trim()}'");
            frm.Activate();
        }

        private void OpenInvPurch(int invType, int procType)
        {
            var frm = new frmInvPurch
            {
                InvType  = invType,
                ProcType = procType
            };
            frm.Show();
            frm.Navigate($"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type={invType} " +
                         $"AND proc_type={procType} AND EntryID=N'{txtNo.Text.Trim()}'");
            frm.Activate();
        }

        private void OpenReceipt(string formName)
        {
            // فتح نموذج السند المناسب
            DXMessageBox.Show($"فتح {formName} برقم: {_receiptGlobalId}",
                "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenNewEntry()
        {
            var frm = new FrmNewEntry();
            frm.Show();
            frm.Navigate($"SELECT * FROM Entry WHERE GlobalID=N'{_globalId}'");
            frm.Activate();
        }

        private void OpenSalaryReserved()
        {
            var frm = new FrmReseved();
            frm.Show();
            frm.Navigate(
                $"SELECT SR.id, SR.Date, SR.EntryGlobalID, SR.GlobalID, B.name, " +
                $"SR.month, SR.year, SR.Notes " +
                $"FROM Salary_Res SR LEFT JOIN Branches B ON B.code=SR.branch " +
                $"WHERE SR.IS_Deleted=0 AND SR.EntryGlobalID=N'{_globalId}'");
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<RestrictionData>();

            string address = "", telePhone = "", mobile = "";
            string foundation = "", field = "", vatNo = "";

            try
            {
                using (var da = new SqlDataAdapter("SELECT * FROM Foundation", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        address    = dt.Rows[0]["Address"].ToString();
                        telePhone  = dt.Rows[0]["Tel"].ToString();
                        mobile     = dt.Rows[0]["Mobile"].ToString();
                        foundation = dt.Rows[0]["nameA"].ToString();
                        field      = dt.Rows[0]["FieldA"].ToString();
                        vatNo      = dt.Rows[0]["tax_no"].ToString();
                    }
                }
            }
            catch { }

            string dateStr = txtDate.DateTime.ToShortDateString();

            foreach (var row in _detailSource)
            {
                list.Add(new RestrictionData
                {
                    AccountName  = row.AccName,
                    AccountCode  = row.AccCode,
                    Description  = row.Notes,
                    Credit       = row.Credit,
                    Dept         = row.Dept,
                    CostCenter   = row.CostCenter,
                    Status       = (cmbState.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                    RestrNo      = txtNo.Text,
                    RestrType    = txtResType.Text,
                    RestDate     = dateStr,
                    User         = Common.GetEmpName(MainClass.EmpNo),
                    SumDept      = txtTotDebt.Text,
                    SumCredit    = txtTotCredit.Text,
                    PrintDate    = DateTime.Now.ToShortDateString(),
                    Address      = address,
                    Mobile       = mobile,
                    TelePhone    = telePhone,
                    VatNo        = vatNo,
                    Foundation   = foundation,
                    Field        = field
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            if (_detailSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);
            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BindToData();

                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
                }

                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var subFooter = report.FindControl("footerRpt", true) as XRSubreport;
                    if (subFooter != null) subFooter.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(_defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = _defPrinter;
                if (printType == 1)
                    for (int i = 0; i < _printNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message);
            }
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

        public class EntrySearchRow
        {
            public string GlobalID   { get; set; }
            public string DocNo      { get; set; }
            public string EntryDate  { get; set; }
            public string EntryType  { get; set; }
            public string EntryState { get; set; }
            public string Notes      { get; set; }
            public string EntryId    { get; set; }
        }

        public class EntryDetailRow : INotifyPropertyChanged
        {
            private string _accName;

            public int    RowNo      { get; set; }
            public string Dept       { get; set; }
            public string Credit     { get; set; }
            public string AccCode    { get; set; }
            public string AccName
            {
                get => _accName;
                set
                {
                    _accName = value;
                    PropertyChanged?.Invoke(this,
                        new PropertyChangedEventArgs(nameof(AccName)));
                }
            }
            public string CostCenter { get; set; }
            public string Notes      { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        #endregion
    }
}