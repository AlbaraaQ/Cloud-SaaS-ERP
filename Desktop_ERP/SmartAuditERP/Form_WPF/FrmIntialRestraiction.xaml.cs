using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using log4net;
using Microsoft.Win32;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmIntialRestraiction : ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────────────

        private readonly SqlConnection _conn;
        private readonly SqlConnection _conn2;

        private int _entryNo = 0;
        private double _debitSum = 0.0;
        private double _creditSum = 0.0;
        private double _debitValue = 0.0;
        private double _creditValue = 0.0;
        private int _entryId = 0;
        private int _entryType = 0;
        private bool _isNew = true;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType = 0;
        private int _printNo = 1;
        private string _defPrinter = string.Empty;
        private string _rptName = string.Empty;
        private string _rptUrl = string.Empty;
        private int _selectedCode = -1;
        private bool _isUpdateDG = false;

        // DataTable للقوائم المنسدلة
        private DataTable _costCentersTable = new DataTable();
        private DataTable _salesmenTable = new DataTable();

        // قائمة بنود القيد
        private ObservableCollection<DgvAccount2> _entryAccList;

        // Public fields (محافظ على الأسماء الأصلية)
        public string SrchName = string.Empty;
        public string SrchCode = string.Empty;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public FrmIntialRestraiction()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn2 = MainClass.ConnObj();
            _entryAccList = new ObservableCollection<DgvAccount2>();
            _entryAccList.CollectionChanged += (s, e) => UpdateSummaryLabels();
        }

        #endregion

        #region ── Window Events ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Today;
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;

            LoadEntryNumber();
            LoadPrintSettings();
            LoadCostCenters();
            LoadSalesmen();

            GridControl1.ItemsSource = _entryAccList;

            if (MainClass.Window_State == WindowState.Maximized)
                this.WindowState = WindowState.Maximized;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F6: Save(); break;
                case Key.F8: DeleteEntry(); break;
                case Key.F10: PrintReport(1); break;
                case Key.F12: NewEntry(); break;
                case Key.F1: AddNewItem(); break;
            }
        }

        private void TabControl1_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        { }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        private void LoadEntryNumber()
        {
            try
            {
                string entryGlobalId = string.Empty;
                EntryOper.GetEntryGlobalID(ref entryGlobalId, ref _entryId);
                EntryOper.GetEntryNobyType(_entryType, ref _entryNo);

                txtEntryGID.Text = entryGlobalId;
                txtNo.Text = _entryNo.ToString();
            }
            catch (Exception ex) { ShowError("خطأ في تحميل الرقم", ex); }
        }

        private void LoadCostCenters()
        {
            try
            {
                _costCentersTable = LoadData.CostCenters();

                // تعيين مصدر البيانات لعمود مركز التكلفة في الجدول
                foreach (var row in _entryAccList) { }
            }
            catch (Exception ex) { ShowError("خطأ في تحميل مراكز التكلفة", ex); }
        }

        private void LoadSalesmen()
        {
            try
            {
                _salesmenTable = LoadData.SalesMen();
            }
            catch (Exception ex) { ShowError("خطأ في تحميل المندوبين", ex); }
        }

        private void LoadSearchGrid(string condition)
        {
            try
            {
                string sql = "SELECT GlobalID, id, date, doc_no, notes " +
                             "FROM Entry WHERE IS_Deleted=0 AND type=0 " +
                             condition + " ORDER BY id";

                var adapter = new SqlDataAdapter(sql, _conn);

                if (!string.IsNullOrEmpty(condition) && condition.Contains("@date"))
                {
                    adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime)
                        .Value = txtFromDate.SelectedDate ?? DateTime.Today;
                    adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime)
                        .Value = txtToDate.SelectedDate ?? DateTime.Today;
                }

                var table = new DataTable();
                adapter.Fill(table);

                var rows = new List<SearchEntryRow>();
                foreach (DataRow row in table.Rows)
                {
                    rows.Add(new SearchEntryRow
                    {
                        GlobalID = row["GlobalID"]?.ToString() ?? string.Empty,
                        EntryNo = SafeInt(row["id"]),
                        EntryDate = SafeDate(row["date"]).ToShortDateString(),
                        Notes = row["notes"]?.ToString() ?? string.Empty
                    });
                }

                dgvSrch.ItemsSource = rows;
            }
            catch (Exception ex) { ShowError("خطأ في البحث", ex); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=9", _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count == 1)
                {
                    _printType = SafeInt(table.Rows[0]["printType"]);
                    _printFooter = Convert.ToBoolean(table.Rows[0]["PrintFooter"]);
                    _printHeader = Convert.ToBoolean(table.Rows[0]["PrintHeader"]);
                    _printStamp = Convert.ToBoolean(table.Rows[0]["PrintStamp"]);
                    _defPrinter = table.Rows[0]["CasherPrinter"]?.ToString() ?? string.Empty;
                    _printNo = SafeInt(table.Rows[0]["printNo"], 1);
                    _rptName = table.Rows[0]["RptName"]?.ToString() ?? string.Empty;
                    _rptUrl = Path.GetDirectoryName(
                                       table.Rows[0]["RptUrl"]?.ToString() ?? string.Empty)
                                   ?? string.Empty;

                    if (string.IsNullOrEmpty(_defPrinter))
                        _defPrinter = Common.GetDefaultPrinter();

                    if (string.IsNullOrEmpty(_rptUrl) || !Directory.Exists(_rptUrl))
                        _rptUrl = MainClass.ReportsPath;
                }
                else
                {
                    _rptUrl = MainClass.ReportsPath;
                    _defPrinter = MainClass.ReportsPrinter;
                    _rptName = "Entry.repx";
                }
            }
            catch (Exception ex) { ShowError("خطأ في إعدادات الطباعة", ex); }
        }

        #endregion

        #region ── Record Navigation ────────────────────────────────────────

        public void Navigate(string sql)
        {
            try
            {
                dgvSrch.UnselectAll();

                if (_conn.State != ConnectionState.Open) _conn.Open();

                using var cmd = new SqlCommand(sql, _conn);
                using var reader = cmd.ExecuteReader();
                ReadDataFromReader(reader);
            }
            catch (Exception ex) { ShowError("خطأ في التنقل", ex); }
            finally
            {
                if (_conn.State != ConnectionState.Closed) _conn.Close();
            }
        }

        private void ReadDataFromReader(SqlDataReader reader)
        {
            if (!reader.HasRows) return;

            try
            {
                reader.Read();

                ClearAll();
                NewEntry();

                _entryId = SafeInt(reader["id"]);
                _entryNo = SafeInt(reader["doc_no"]);
                _isNew = false;

                txtEntryGID.Text = reader["GlobalID"]?.ToString() ?? string.Empty;
                txtNo.Text = _entryNo.ToString();
                txtNotes.Text = reader["notes"]?.ToString() ?? string.Empty;

                if (DateTime.TryParse(reader["date"]?.ToString(), out DateTime dt))
                    txtDate.SelectedDate = dt;

                reader.Close();

                // تحميل بنود القيد
                string sql = $@"SELECT * FROM Entry_sub
                                WHERE branch = {MainClass.BranchNo}
                                  AND EntryGlobalId = N'{txtEntryGID.Text}'
                                ORDER BY id ASC";

                var adapter = new SqlDataAdapter(sql, _conn);
                var table = new DataTable();
                adapter.Fill(table);

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    DataRow row = table.Rows[i];
                    var account = new DgvAccount2
                    {
                        AccRowIndex = i + 1,
                        AccCode = row["acc_no"]?.ToString() ?? string.Empty,
                        AccName = GetAccountName(SafeInt(row["acc_no"])),
                        AccDebit = Convert.ToDouble(row["dept"] == DBNull.Value ? 0 : row["dept"]),
                        AccCredit = Convert.ToDouble(row["credit"] == DBNull.Value ? 0 : row["credit"]),
                        AccCostCenter = string.IsNullOrEmpty(row["CCcode"]?.ToString()) ? -1
                                         : SafeInt(row["CCcode"]),
                        salesman = string.IsNullOrEmpty(row["salesman"]?.ToString()) ? "-1"
                                         : row["salesman"].ToString()!,
                        AccDescription = row["notes"]?.ToString() ?? string.Empty
                    };
                    _entryAccList.Add(account);
                }

                UpdateGridView();
            }
            catch (Exception ex) { ShowError("خطأ في قراءة البيانات", ex); }
        }

        private string GetBranchCondition()
        {
            return MainClass.BranchNo != -1
                   ? $"branch={MainClass.BranchNo} AND "
                   : string.Empty;
        }

        #endregion

        #region ── CRUD ────────────────────────────────────────────────────

        private void NewEntry()
        {
            ClearAll();
            GridControl1.ItemsSource = _entryAccList;
        }

        private void ClearAll()
        {
            GridControl1.ItemsSource = null;
            _entryAccList = new ObservableCollection<DgvAccount2>();
            _entryAccList.CollectionChanged += (s, e) => UpdateSummaryLabels();
            _isNew = true;
            _entryNo = -1;
            _entryId = -1;
            _debitValue = 0.0;
            _creditValue = 0.0;
            txtNotes.Text = string.Empty;
            LoadEntryNumber();
        }

        private void Save()
        {
            if (_conn.State != ConnectionState.Open) _conn.Open();

            try
            {
                // التحقق من صحة البيانات
                foreach (var acc in _entryAccList)
                {
                    if (string.IsNullOrEmpty(acc.AccCode))
                    {
                        DXMessageBox.Show("يجب إدخال اسم ورقم الحساب",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (acc.AccDebit == 0.0 && acc.AccCredit == 0.0)
                    {
                        DXMessageBox.Show($"يوجد بند رقم {acc.AccRowIndex} بدون قيمة",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (_entryAccList.Count == 0)
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "لا يوجد بيانات" : "There are no data",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                double totalDebit = _entryAccList.Sum(a => a.AccDebit);
                double totalCredit = _entryAccList.Sum(a => a.AccCredit);

                if (Math.Abs(totalDebit - totalCredit) > 0.001)
                {
                    DXMessageBox.Show("لا يمكن حفظ قيد غير متوازن",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEntryGID.Text))
                {
                    DXMessageBox.Show("الرقم العام للقيد غير صحيح",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من حفظ القيد؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                    txtNotes.Text = $"سند رصيد افتتاحي رقم {txtNo.Text}" +
                                    $" بتاريخ {txtDate.SelectedDate:d}";

                if (_isNew) LoadEntryNumber();

                // بناء كائن القيد
                var entry = new Entry
                {
                    EntryGlobalID = txtEntryGID.Text,
                    ClientCode = Sync.ClientCode,
                    EntryNo = _entryId,
                    EntryDate = txtDate.SelectedDate ?? DateTime.Today,
                    ReffNo = txtNo.Text,
                    RefDate = txtDate.SelectedDate ?? DateTime.Today,
                    Type = EntryType.IntialEntry,
                    State = 1,
                    Note = txtNotes.Text,
                    Branch = MainClass.BranchNo,
                    EmpID = MainClass.EmpNo,
                    DistBranch = Sync.DistBranch,
                    BranchType = Sync.BranchType,
                    Accounts = BuildAccountsList()
                };

                var entryOper = new EntryOper();
                if (!entryOper.SaveEnty(entry))
                {
                    DXMessageBox.Show(MainClass.Language == "en"
                        ? "error in saving" : "خطأ أثناء الحفظ",
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Logging
                string logMsg = _isNew
                    ? $"تم حفظ قيد إفتتاحي برقم {_entryId} بواسطة {MainClass.UserName}"
                    : $"تم تعديل قيد إفتتاحي برقم {_entryId} بواسطة {MainClass.UserName}";
                Logger.Info(logMsg);

                // Sync
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    entryOper.SyncEntry(entry, _isNew);

                // رسالة النجاح
                var savedMsg = new frmSavedMsg();
                if (!_isNew) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1) NewEntry();
                else if (savedMsg.Pressed == 2) _isNew = false;
                else if (savedMsg.Pressed == 3) this.Close();
            }
            catch (Exception ex) { ShowError("خطأ أثناء الحفظ", ex); }
            finally
            {
                if (_conn.State != ConnectionState.Closed) _conn.Close();
            }
        }

        private List<Account> BuildAccountsList()
        {
            var list = new List<Account>();
            foreach (var acc in _entryAccList)
            {
                string note = string.IsNullOrEmpty(acc.AccDescription)
                    ? $"سند قيد افتتاحي رقم: {_entryNo} - حساب: {acc.AccName}"
                    : acc.AccDescription;

                list.Add(new Account
                {
                    EntryGlobalID = txtEntryGID.Text,
                    EntryNo = _entryId,
                    Name = acc.AccName,
                    Code = acc.AccCode,
                    Debt = acc.AccDebit,
                    Credit = acc.AccCredit,
                    Note = note,
                    CCcode = (acc.AccCostCenter > 0)
                                    ? acc.AccCostCenter.ToString() : "-1",
                    salesman = (double.TryParse(acc.salesman, out double sv) && sv > 0)
                                    ? (int)sv : -1
                });
            }
            return list;
        }

        private void DeleteEntry()
        {
            if (DXMessageBox.Show("هل تريد حذف القيد؟",
                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                if (_conn.State != ConnectionState.Open) _conn.Open();

                new SqlCommand(
                    $"UPDATE Entry SET IS_Deleted=1 WHERE GlobalId=N'{txtEntryGID.Text}'",
                    _conn).ExecuteNonQuery();

                Logger.Info($"تم حذف سند قيد افتتاحي رقم {txtNo.Text} بواسطة {MainClass.UserName}");
                ClearAll();
            }
            catch (Exception ex) { ShowError("خطأ في الحذف", ex); }
            finally
            {
                if (_conn.State != ConnectionState.Closed) _conn.Close();
            }
        }

        #endregion

        #region ── Account Search & Add ─────────────────────────────────────

        private void AddNewItem()
        {
            var accountSearchForm = new frmAccountSrch
            {
                cond = "  ",
                txtSrchNm = { Text = SrchName }
            };
            MainClass.ApplyPermissionToForm(accountSearchForm);
            MainClass.DoApplyUserSett(accountSearchForm);
            accountSearchForm.ShowDialog();

            if (accountSearchForm.IsDone && accountSearchForm.Code > 0)
            {
                _selectedCode = accountSearchForm.Code;
                GetAccountById(accountSearchForm.Code.ToString());
            }
        }

        private void GetAccountById(string accountCode)
        {
            try
            {
                if (string.IsNullOrEmpty(accountCode))
                {
                    AddNewItem();
                    return;
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT AName, Code, ISNULL(CostCenter,-1) AS CostCenter " +
                    $"FROM Accounts_Index WHERE type=2 AND Code=N'{accountCode}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count == 0) return;

                // التحقق من التكرار
                var existing = _entryAccList
                    .FirstOrDefault(a => a.AccCode == accountCode);

                if (existing != null)
                {
                    var msgExist = new MsgExistItem();
                    msgExist.lbltext.Text = "الحساب مدرج سابقاً!";
                    msgExist.btnAddquantity.Visibility = Visibility.Collapsed;
                    msgExist.ShowDialog();
                    if (msgExist.Action != 2) return;
                }

                var newAcc = new DgvAccount2
                {
                    AccRowIndex = _entryAccList.Count + 1,
                    AccCode = table.Rows[0]["Code"]?.ToString() ?? string.Empty,
                    AccName = table.Rows[0]["AName"]?.ToString() ?? string.Empty,
                    AccDebit = 0.0,
                    AccCredit = 0.0,
                    AccCostCenter = SafeInt(table.Rows[0]["CostCenter"], -1),
                    AccDescription = string.Empty
                };

                _entryAccList.Add(newAcc);
                UpdateGridView();
            }
            catch (Exception ex) { ShowError("خطأ في إضافة الحساب", ex); }
        }

        private string GetAccountName(int code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AName FROM Accounts_Index WHERE type=2 AND Code={code}",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0 ? table.Rows[0][0]?.ToString() ?? string.Empty
                                            : string.Empty;
            }
            catch { return string.Empty; }
        }

        private bool IsAccountValid(int code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AName FROM Accounts_Index WHERE type=2 AND Code={code}",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0;
            }
            catch { return false; }
        }

        #endregion

        #region ── Grid Events ──────────────────────────────────────────────

        private void AccSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            AddNewItem();
        }

        private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not DgvAccount2 rowData) return;

            string msg = MainClass.Language == "en"
                             ? "Are you sure to delete?" : "هل أنت متأكد من الحذف؟";
            string caption = MainClass.Language == "en"
                             ? "Confirmation" : "رسالة تأكيد";

            if (DXMessageBox.Show(msg, caption,
                MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            _entryAccList.Remove(rowData);

            // إعادة ترقيم الصفوف
            for (int i = 0; i < _entryAccList.Count; i++)
                _entryAccList[i].AccRowIndex = i + 1;

            UpdateGridView(moveLast: false);
        }

        private void GridView1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_entryAccList.Count == 0) return;

            if (e.Key == Key.Delete)
            {
                if (GridControl1.CurrentItem is DgvAccount2 row)
                {
                    string msg = MainClass.Language == "en"
                                 ? "Are you sure to delete?" : "هل أنت متأكد من الحذف؟";
                    if (DXMessageBox.Show(msg, "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                    {
                        _entryAccList.Remove(row);
                        for (int i = 0; i < _entryAccList.Count; i++)
                            _entryAccList[i].AccRowIndex = i + 1;
                        UpdateGridView(moveLast: false);
                    }
                }
            }
        }

        private void GridView1_CellValueChanged(object sender,
            CellValueChangedEventArgs e)
        {
            if (e.Column.FieldName == "AccDebit" && GridControl1.CurrentItem is DgvAccount2 r1)
                r1.AccCredit = 0.0;

            if (e.Column.FieldName == "AccCredit" && GridControl1.CurrentItem is DgvAccount2 r2)
                r2.AccDebit = 0.0;

            UpdateSummaryLabels();
        }

        private void UpdateGridView(bool moveLast = true)
        {
            GridControl1.ItemsSource = null;
            GridControl1.ItemsSource = _entryAccList;
            UpdateSummaryLabels();
        }

        private void UpdateSummaryLabels()
        {
            double totalDebit = _entryAccList.Sum(a => a.AccDebit);
            double totalCredit = _entryAccList.Sum(a => a.AccCredit);

            lblDebitSum.Text = totalDebit.ToString("N2");
            lblCreditSum.Text = totalCredit.ToString("N2");
        }

        private void UpdateSummaryLabels1()
        {
            double totalDebit = _entryAccList.Sum(a => a.AccDebit);
            double totalCredit = _entryAccList.Sum(a => a.AccCredit);

            lblDebitSum.Text = totalDebit.ToString("N2");
            lblCreditSum.Text = totalCredit.ToString("N2");

            // لون يُشير للتوازن
            bool balanced = Math.Abs(totalDebit - totalCredit) < 0.001;
            lblDebitSum.Foreground = balanced
                ? System.Windows.Media.Brushes.DarkGreen
                : System.Windows.Media.Brushes.DarkRed;
            lblCreditSum.Foreground = balanced
                ? System.Windows.Media.Brushes.DarkGreen
                : System.Windows.Media.Brushes.DarkRed;
        }

        #endregion

        #region ── Search Tab Events ─────────────────────────────────────────

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            string branchCond = MainClass.BranchNo != -1
                                ? $"AND branch={MainClass.BranchNo}" : string.Empty;

            string condition = string.IsNullOrWhiteSpace(txtNoSrch.Text)
                ? $"{branchCond} AND date>=@date1 AND date<=@date2 "
                : $"{branchCond} AND type=0 AND doc_no={txtNoSrch.Text}";

            LoadSearchGrid(condition);
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is not SearchEntryRow row) return;

            txtEntryGID.Text = row.GlobalID;
            _entryId = row.EntryNo;

            Navigate($"SELECT * FROM Entry WHERE globalID=N'{row.GlobalID}'");
            TabControl1.SelectedIndex = 0;
        }

        #endregion

        #region ── Button Events ────────────────────────────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e) => NewEntry();
        private void btnSave_Click(object sender, RoutedEventArgs e) => Save();
        private void btnDelete_Click(object sender, RoutedEventArgs e) => DeleteEntry();
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnView_Click(object sender, RoutedEventArgs e) => PrintReport(2);
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReport(1);

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} type=0 AND IS_Deleted=0 ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} type=0 AND IS_Deleted=0 ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} type=0 AND IS_Deleted=0 AND id>{_entryId} ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM Entry WHERE {GetBranchCondition()} type=0 AND IS_Deleted=0 AND id<{_entryId} ORDER BY id DESC");

        private void btnExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAll();
                NewEntry();

                if (DXMessageBox.Show("هل أنت متأكد من استيراد البيانات؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                var importForm = new frmImportDataGeneral();
                importForm.cmbInv.SelectedIndex = 3;
                importForm.cmbInv.Visibility = Visibility.Visible;
                importForm.cmbInv.IsEnabled = false;
                importForm.cmbDataTable.Visibility = Visibility.Collapsed;
                importForm.ShowDialog();

                if (!importForm.ISDone) return;

                for (int i = 0; i < importForm.dtIniRes.Rows.Count; i++)
                {
                    DataRow row = importForm.dtIniRes.Rows[i];
                    _entryAccList.Add(new DgvAccount2
                    {
                        AccRowIndex = i + 1,
                        AccCode = row["AccCode"]?.ToString() ?? string.Empty,
                        AccName = GetAccountName(SafeInt(row["AccCode"])),
                        AccDebit = Convert.ToDouble(row["Dept"] == DBNull.Value ? 0 : row["Dept"]),
                        AccCredit = Convert.ToDouble(row["Credit"] == DBNull.Value ? 0 : row["Credit"]),
                        AccCostCenter = -1,
                        AccDescription = row["Note"]?.ToString() ?? string.Empty
                    });
                }

                UpdateGridView();
            }
            catch (Exception ex) { ShowError("خطأ في الاستيراد", ex); }
        }

        private void btnImportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsEntryExist())
                {
                    DXMessageBox.Show("يجب حفظ القيد قبل التصدير");
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من تصدير البيانات؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };

                if (saveDialog.ShowDialog() != true) return;

                string filePath = saveDialog.FileName;

                if (_entryAccList.Count > 0)
                {
                    // تصدير باستخدام DevExpress GridControl
                    (GridControl1.View as DevExpress.Xpf.Grid.TableView)?.ExportToXlsx(filePath);
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(filePath)
                        { UseShellExecute = true });
                    DXMessageBox.Show($"تم حفظ الملف في {filePath}");
                }
            }
            catch (Exception ex) { ShowError("خطأ في التصدير", ex); }
        }

        #endregion

        #region ── Print ────────────────────────────────────────────────────

        private void PrintReport(int type)
        {
            if (_entryAccList.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }

            string reportPath = Path.Combine(_rptUrl, _rptName);
            if (!File.Exists(reportPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = DevExpress.XtraReports.UI.XtraReport.FromFile(reportPath);
                report.DataSource = BuildReportDataSet();

                // Header subreport
                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport = DevExpress.XtraReports.UI.XtraReport.FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;
                    var headerSub = report.FindControl("headerRpt", true)
                                    as DevExpress.XtraReports.UI.XRSubreport;
                    if (headerSub != null) headerSub.ReportSource = headerReport;
                }

                // Footer subreport
                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerReport = DevExpress.XtraReports.UI.XtraReport.FromFile(footerPath);
                    footerReport.DataSource = Common.FoundationInfoDT;
                    var footerSub = report.FindControl("footerRpt", true)
                                    as DevExpress.XtraReports.UI.XRSubreport;
                    if (footerSub != null) footerSub.ReportSource = footerReport;
                }

                if (!string.IsNullOrEmpty(_defPrinter))
                {
                    report.PrinterName = _defPrinter;
                    if (type == 1)
                        for (int i = 0; i < _printNo; i++) report.Print();
                    else
                        report.ShowPreviewDialog();
                }
                else
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات");
                }

                report.Dispose();
            }
            catch (Exception ex) { ShowError("خطأ في الطباعة", ex); }
        }

        private DataSet BuildReportDataSet()
        {
            var foundation = LoadFoundationData();
            var reportDataList = new List<RestrictionData>();
            string dateStr = (txtDate.SelectedDate ?? DateTime.Today).ToShortDateString();
            string empName = Common.GetEmpName(MainClass.EmpNo);

            foreach (var acc in _entryAccList)
            {
                reportDataList.Add(new RestrictionData
                {
                    AccountName = acc.AccName,
                    AccountCode = acc.AccCode,
                    Description = acc.AccDescription,
                    Credit = acc.AccCredit.ToString("N2"),
                    Dept = acc.AccDebit.ToString("N2"),
                    CostCenter = acc.AccCostCenter.ToString(),
                    Status = " ",
                    RestrNo = txtNo.Text,
                    RestrType = "سند قيد إفتتاحي",
                    RestDate = dateStr,
                    RestTime = dateStr,
                    User = empName,
                    SumDept = _entryAccList.Sum(a => a.AccDebit).ToString("N2"),
                    SumCredit = _entryAccList.Sum(a => a.AccCredit).ToString("N2"),
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Note = txtNotes.Text,
                    Address = foundation.address,
                    Mobile = foundation.mobile,
                    TelePhone = foundation.phone,
                    VatNo = foundation.vatNo,
                    Foundation = foundation.name,
                    Field = foundation.field,
                    Logo = "",
                    Header = "",
                    footer = "",
                    Stamp = ""
                });
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(reportDataList));
            return dataSet;
        }

        private (string name, string address, string phone,
                  string mobile, string field, string vatNo)
        LoadFoundationData()
        {
            try
            {
                var adapter = new SqlDataAdapter("SELECT * FROM Foundation", _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    DataRow r = table.Rows[0];
                    return (
                        r["nameA"]?.ToString() ?? string.Empty,
                        r["Address"]?.ToString() ?? string.Empty,
                        r["Tel"]?.ToString() ?? string.Empty,
                        r["Mobile"]?.ToString() ?? string.Empty,
                        r["FieldA"]?.ToString() ?? string.Empty,
                        r["tax_no"]?.ToString() ?? string.Empty
                    );
                }
            }
            catch { }
            return (string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty);
        }

        #endregion

        #region ── Helpers ──────────────────────────────────────────────────

        private bool IsEntryExist()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id FROM Entry WHERE type=0 AND doc_no={txtNo.Text}",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0;
            }
            catch { return false; }
        }

        private static int SafeInt(object value, int defaultVal = 0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return int.TryParse(value.ToString(), out int r) ? r : defaultVal;
        }

        private static DateTime SafeDate(object value)
        {
            if (value == null || value == DBNull.Value) return DateTime.MinValue;
            return DateTime.TryParse(value.ToString(), out DateTime d) ? d : DateTime.MinValue;
        }

        private void ShowError(string title, Exception ex)
        {
            DXMessageBox.Show($"{title}\nتفاصيل الخطأ: {ex.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}