using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmNewEntry : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Inner Classes

        public class RowInfo
        {
            public DataGrid Grid { get; set; }
            public int RowIndex { get; set; }

            public RowInfo(DataGrid grid, int rowIndex)
            {
                Grid = grid;
                RowIndex = rowIndex;
            }
        }

        #endregion

        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int EntryNo = -1;
        private int EntryID = -1;
        private double DeptSum = 0.0;
        private double CreditSum = 0.0;
        private double debtVal = 0.0;
        private double CredVal = 0.0;
        private bool Isload = true;
        private bool IsNew = true;
        private int EntryType = 10;

        private List<DgvAccount> EntryAccList;
        private ObservableCollection<DgvAccount> _entryRows;
        private ObservableCollection<EntrySearchRow> _searchRows;

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private bool PrintHeader = true;
        private bool PrintFooter = true;
        private bool PrintStamp = true;
        private int PrintType = 0;
        private int PrintNo = 1;
        private string defPrinter = "";
        private string RptName = "";
        private string RptUrl = "";

        private int RowIndex = -1;
        private int SelectedCode = -1;
        public string SrchName = "";
        private bool NotChanged = true;
        private bool NotLoad = true;

        #endregion

        #region Constructor

        public FrmNewEntry()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            EntryAccList = new List<DgvAccount>();
            _entryRows = new ObservableCollection<DgvAccount>();
            _searchRows = new ObservableCollection<EntrySearchRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void FrmAddRestriction_Load(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Today;
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtTime.Text = DateTime.Now.ToString("HH:mm");
            txtBranch.Text = MainClass.BranchNo.ToString();

            GridControl1.ItemsSource = _entryRows;
            dgvSrch.ItemsSource = _searchRows;

            LoadCostCenters();
            LoadSalsmen();
            LoadResNo();
            LoadPrintSettings();

            Isload = false;
            Editereciept.Text = MainClass.UserName;
            Editereciept.Foreground = new SolidColorBrush(Colors.OrangeRed);

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Load Data

        private void LoadCostCenters()
        {
            try
            {
                DataTable dt = LoadData.CostCenters();
                colCostCenter.ItemsSource = dt.DefaultView;
                colCostCenter.DisplayMemberPath = "name";
                colCostCenter.SelectedValuePath = "code";
            }
            catch { }
        }

        private void LoadSalsmen()
        {
            try
            {
                DataTable dt = LoadData.SalesMen();
                colSalesman.ItemsSource = dt.DefaultView;
                colSalesman.DisplayMemberPath = "name";
                colSalesman.SelectedValuePath = "id";
            }
            catch { }
        }

        private void LoadResNo()
        {
            try
            {
                string entryGlobalId = "";
                EntryOper.GetEntryGlobalID(ref entryGlobalId, ref EntryID);
                EntryOper.GetEntryNobyType(EntryType, ref EntryNo);
                txtEntryGID.Text = entryGlobalId;
                txtNo.Text = EntryNo.ToString();
                txtNo.Background = new SolidColorBrush(Colors.Firebrick);
                txtNo.Foreground = new SolidColorBrush(Colors.White);
                txtEntryGID.Background = new SolidColorBrush(Colors.Firebrick);
                txtEntryGID.Foreground = new SolidColorBrush(Colors.White);
            }
            catch { }
        }

        #endregion

        #region ReadData & Navigate

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;

                dr.Read();
                ClearAll();
                NewEntry();

                txtNo.Background = new SolidColorBrush(Colors.WhiteSmoke);
                txtNo.Foreground = new SolidColorBrush(Colors.Black);
                txtEntryGID.Background = new SolidColorBrush(Colors.WhiteSmoke);
                txtEntryGID.Foreground = new SolidColorBrush(Colors.Black);

                int.TryParse(dr["doc_no"].ToString(), out EntryNo);
                int.TryParse(dr["id"].ToString(), out EntryID);

                txtBranch.Text = dr["branch"].ToString();
                txtNo.Text = EntryNo.ToString();
                txtEntryGID.Text = dr["GlobalID"].ToString();

                DateTime entryDate = Convert.ToDateTime(dr["date"]);
                txtDate.SelectedDate = entryDate;
                txtTime.Text = entryDate.ToString("HH:mm");

                if (dr["IsVAT"] != DBNull.Value)
                    chkIsVAT.IsChecked = Convert.ToBoolean(dr["IsVAT"]);

                txtNotes.Text = dr["notes"]?.ToString() ?? "";
                IsNew = false;

                int empId = 0;
                int.TryParse(dr["EmpId"].ToString(), out empId);

                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                conn.Open();
                SqlDataReader empReader = new SqlCommand(
                    $"select username from users where emp={empId}", conn).ExecuteReader();

                empReader.Read();
                if (empReader.HasRows)
                {
                    Editereciept.Text = empReader["username"].ToString();
                    Editereciept.Foreground = new SolidColorBrush(Colors.Green);
                }
                empReader.Close();
                conn.Close();
                dr.Close();

                SqlDataAdapter subAdapter = new SqlDataAdapter(
                    $"select * from Entry_sub where EntryGlobalId=N'{txtEntryGID.Text}' order by id asc", conn);
                DataTable subDt = new DataTable();
                subAdapter.Fill(subDt);

                _entryRows.Clear();
                EntryAccList.Clear();

                for (int i = 0; i < subDt.Rows.Count; i++)
                {
                    int accCode = 0;
                    int.TryParse(subDt.Rows[i]["acc_no"].ToString(), out accCode);

                    DgvAccount account = new DgvAccount
                    {
                        AccRowIndex = i + 1,
                        AccCode = subDt.Rows[i]["acc_no"].ToString(),
                        AccName = GetAccountName(accCode),
                        AccDebit = Convert.ToDouble(subDt.Rows[i]["dept"]),
                        AccCredit = Convert.ToDouble(subDt.Rows[i]["credit"]),
                        AccCostCenter = string.IsNullOrEmpty(subDt.Rows[i]["CCcode"].ToString()) ? -1 : Convert.ToInt32(subDt.Rows[i]["CCcode"]),
                        AccDescription = subDt.Rows[i]["notes"]?.ToString() ?? "",
                        salsmen = subDt.Rows[i]["salesman"]?.ToString() ?? ""
                    };

                    EntryAccList.Add(account);
                    _entryRows.Add(account);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void Navigate(string sqlStr)
        {
            dgvSrch.UnselectAll();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd = new SqlCommand(sqlStr, conn);
                ReadData(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void txtNoSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search();
        }

        private void Search()
        {
            string branchFilter = MainClass.BranchNo != -1 ? $"and branch={MainClass.BranchNo}" : "";

            string cond = string.IsNullOrWhiteSpace(txtNoSrch.Text)
                ? $"{branchFilter} and date>=@date1 and date<=@date2"
                : $"{branchFilter} and type=10 and doc_no={txtNoSrch.Text}";

            LoadDG(cond);
        }

        private void LoadDG(string cond)
        {
            try
            {
                _searchRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select GlobalID,id,date,doc_no,notes from Entry where type=10 and IS_Deleted=0 {cond} order by id",
                    conn);

                if (!string.IsNullOrEmpty(cond) && cond.Contains("@date1"))
                {
                    adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = txtFromDate.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = txtToDate.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString();
                }

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    _searchRows.Add(new EntrySearchRow
                    {
                        Column7 = dt.Rows[i]["GlobalID"].ToString(),
                        Column3 = dt.Rows[i]["id"].ToString(),
                        Column1 = dt.Rows[i]["doc_no"].ToString(),
                        Column2 = Convert.ToDateTime(dt.Rows[i]["date"]).ToShortDateString(),
                        Column4 = dt.Rows[i]["notes"]?.ToString() ?? ""
                    });
                }

                lblSearchCount.Text = $"عدد السجلات: {_searchRows.Count:N0}";

                txtNo.Background = new SolidColorBrush(Colors.WhiteSmoke);
                txtNo.Foreground = new SolidColorBrush(Colors.Black);
                txtEntryGID.Background = new SolidColorBrush(Colors.WhiteSmoke);
                txtEntryGID.Foreground = new SolidColorBrush(Colors.Black);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void dgvSrch_CellClick(object sender, MouseButtonEventArgs e)
        {
            EntrySearchRow selected = dgvSrch.SelectedItem as EntrySearchRow;
            if (selected == null) return;

            txtEntryGID.Text = selected.Column7;
            int.TryParse(selected.Column3, out EntryID);

            Navigate($"select * from Entry where GlobalID=N'{selected.Column7}'");
            TabControl1.SelectedIndex = 0;
        }

        #endregion

        #region Account Search

        private string GetAccountName(int code)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select AName from Accounts_Index where type=2 and Code={code}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private bool IsAccount(int code)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select AName from Accounts_Index where type=2 and Code={code}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void SearchByName(string accountName)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select code from Accounts_Index where type=2 and IsDeleted=0 and AName=N'{accountName}'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    GetAccByID(dt.Rows[0]["Code"].ToString());
                else
                    AddNewItem();
            }
            catch { }
        }

        private void GetAccByID(string accountCode)
        {
            try
            {
                if (string.IsNullOrEmpty(accountCode))
                {
                    AddNewItem();
                    return;
                }

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select AName, Code, ISNULL(CostCenter,-1) as CostCenter from Accounts_Index where type=2 and Code=N'{accountCode}'",
                    conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                string code = dt.Rows[0]["Code"].ToString();
                string name = dt.Rows[0]["AName"].ToString();
                int costCenter = Convert.ToInt32(dt.Rows[0]["CostCenter"]);

                // فحص تكرار الحساب
                foreach (DgvAccount acc in EntryAccList)
                {
                    if (acc.AccCode == code)
                    {
                        DXMessageBox.Show("الحساب مدرج سابقًا!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                DgvAccount newAcc = new DgvAccount
                {
                    AccRowIndex = EntryAccList.Count + 1,
                    AccCode = code,
                    AccName = name,
                    AccDebit = 0.0,
                    AccCredit = 0.0,
                    AccCostCenter = costCenter,
                    AccDescription = "",
                    salsmen = ""
                };

                EntryAccList.Add(newAcc);
                _entryRows.Add(newAcc);
                UpdateSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddNewItem()
        {
            frmAccountSrch srchForm = new frmAccountSrch();
            srchForm.cond = "  ";
            MainClass.ApplyPermissionToForm(srchForm);
            MainClass.DoApplyUserSett(srchForm);
            srchForm.ShowDialog();

            if (srchForm.IsDone && srchForm.Code > 0)
            {
                SelectedCode = srchForm.Code;
                GetAccByID(srchForm.Code.ToString());
            }
        }

        #endregion

        #region Grid Events

        private void GridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteCurrentRow();
                e.Handled = true;
            }
            else if (e.Key == Key.Return)
            {
                HandleEnterKey();
                e.Handled = true;
            }
        }

        private void HandleEnterKey()
        {
            if (GridControl1.CurrentColumn == null) return;

            string colName = GridControl1.CurrentColumn.SortMemberPath;

            if (colName == "AccCode" || colName == "AccName")
            {
                AddNewItem();
            }
        }

        private void GridControl1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                string colName = e.Column.SortMemberPath;

                if (colName == "AccDebit")
                {
                    DgvAccount acc = e.Row.Item as DgvAccount;
                    if (acc != null && acc.AccDebit > 0)
                        acc.AccCredit = 0;
                }
                else if (colName == "AccCredit")
                {
                    DgvAccount acc = e.Row.Item as DgvAccount;
                    if (acc != null && acc.AccCredit > 0)
                        acc.AccDebit = 0;
                }

                Dispatcher.BeginInvoke(new Action(UpdateSummary));
            }
            catch { }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            DgvAccount acc = btn.Tag as DgvAccount;
            if (acc == null) return;

            string msg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? "Are you sure to delete?" : "هل أنت متأكد من الحذف؟";

            if (DXMessageBox.Show(msg, "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                EntryAccList.Remove(acc);
                _entryRows.Remove(acc);
                ReIndexRows();
                UpdateSummary();
            }
        }

        private void DeleteCurrentRow()
        {
            DgvAccount selected = GridControl1.SelectedItem as DgvAccount;
            if (selected == null) return;

            string msg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? "Are you sure to delete?" : "هل أنت متأكد من الحذف؟";

            if (DXMessageBox.Show(msg, "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                EntryAccList.Remove(selected);
                _entryRows.Remove(selected);
                ReIndexRows();
                UpdateSummary();
            }
        }

        private void GridControl1_RightClick(object sender, MouseButtonEventArgs e)
        {
            DgvAccount selected = GridControl1.SelectedItem as DgvAccount;
            if (selected == null) return;

            ContextMenu menu = new ContextMenu();
            MenuItem showItem = new MenuItem { Header = "📊 كشف حساب" };
            showItem.Click += (s, args) => ShowAccountCard(selected);
            menu.Items.Add(showItem);
            menu.IsOpen = true;
        }

        private void ShowAccountCard(DgvAccount acc)
        {
            try
            {
                frmAccountBalance balanceForm = new frmAccountBalance();
                MainClass.ApplyPermissionToForm(balanceForm);
                MainClass.DoApplyUserSett(balanceForm);
                balanceForm.Show();
                balanceForm.cmbAccounts.SelectedValue = acc.AccCode;
                balanceForm.ShowAccountData();
                balanceForm.Activate();
            }
            catch { }
        }

        private void ReIndexRows()
        {
            int index = 1;
            foreach (DgvAccount acc in EntryAccList)
            {
                acc.AccRowIndex = index++;
            }
        }

        #endregion

        #region New / Clear / Delete

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            NewEntry();
        }

        private void NewEntry()
        {
            ClearAll();
        }

        private void ClearAll()
        {
            _entryRows.Clear();
            EntryAccList = new List<DgvAccount>();
            _entryRows = new ObservableCollection<DgvAccount>();
            GridControl1.ItemsSource = _entryRows;

            txtNotes.Text = "";
            IsNew = true;
            EntryNo = -1;
            EntryID = -1;
            CredVal = 0.0;
            debtVal = 0.0;

            txtDate.SelectedDate = DateTime.Today;
            txtTime.Text = DateTime.Now.ToString("HH:mm");
            chkIsVAT.IsChecked = false;
            txtBranch.Text = MainClass.BranchNo.ToString();

            Editereciept.Text = MainClass.UserName;
            Editereciept.Foreground = new SolidColorBrush(Colors.OrangeRed);

            LoadResNo();
            UpdateSummary();
        }

        private void DeleteEntry()
        {
            if (DXMessageBox.Show("هل تريد حذف القيد؟", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();

                    new SqlCommand($"update Entry set IS_Deleted=1 where GlobalId=N'{txtEntryGID.Text}'", conn).ExecuteNonQuery();

                    ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                    Logger.Info($"تم حذف سند قيد برقم {txtNo.Text} بواسطة المستخدم {MainClass.UserName}");

                    ClearAll();
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                        conn.Close();
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteEntry();
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(txtDate.SelectedDate ?? DateTime.Today))
            {
                DXMessageBox.Show("لا يمكن أن يكون التاريخ خارج الفترة المحاسبية", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Save();
        }

        private void Save()
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (EntryAccList.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد بيانات", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEntryGID.Text))
                {
                    DXMessageBox.Show("الرقم العام للقيد غير صحيح", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (DgvAccount acc in EntryAccList)
                {
                    if (string.IsNullOrEmpty(acc.AccCode))
                    {
                        DXMessageBox.Show("يجب إدخال اسم ورقم الحساب", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (acc.AccDebit == 0.0 && acc.AccCredit == 0.0)
                    {
                        DXMessageBox.Show($"يوجد بند رقم {acc.AccRowIndex} بدون قيمة، يجب إدخال القيمة أو حذفه", "",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }

                double sumDebit = 0, sumCredit = 0;
                foreach (DgvAccount acc in EntryAccList)
                {
                    sumDebit += acc.AccDebit;
                    sumCredit += acc.AccCredit;
                }

                if (Math.Round(sumDebit, 2) != Math.Round(sumCredit, 2))
                {
                    DXMessageBox.Show("لا يمكن حفظ قيد غير متوازن", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من حفظ القيد؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                if (string.IsNullOrWhiteSpace(txtNotes.Text))
                    txtNotes.Text = $"سند قيد يومية رقم: {EntryNo} بتاريخ {txtDate.SelectedDate?.ToShortDateString()}";

                if (IsNew) LoadResNo();

                DateTime entryDateTime = BuildDateTime(txtDate.SelectedDate ?? DateTime.Today, txtTime.Text);

                Entry entry = new Entry
                {
                    EntryGlobalID = txtEntryGID.Text,
                    ClientCode = Sync.ClientCode,
                    EntryNo = EntryID,
                    EntryDate = entryDateTime,
                    ReffNo = txtNo.Text,
                    RefDate = txtDate.SelectedDate ?? DateTime.Today,
                    Type = (EntryType)10,
                    State = 1,
                    Note = txtNotes.Text,
                    Branch = !string.IsNullOrEmpty(txtBranch.Text) ? Convert.ToInt32(txtBranch.Text) : MainClass.BranchNo,
                    EmpID = MainClass.EmpNo,
                    DistBranch = Sync.DistBranch,
                    BranchType = Sync.BranchType,
                    IsVAT = chkIsVAT.IsChecked == true
                };

                List<Account> accounts = new List<Account>();
                foreach (DgvAccount acc in EntryAccList)
                {
                    string note = string.IsNullOrEmpty(acc.AccDescription)
                        ? $"سند قيد يومية رقم: {EntryNo} - سداد دفعة من حساب: {acc.AccName}"
                        : acc.AccDescription;

                    int salesman = 0;
                    int.TryParse(acc.salsmen, out salesman);

                    accounts.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = acc.AccName,
                        Code = acc.AccCode,
                        Debt = acc.AccDebit,
                        Credit = acc.AccCredit,
                        Note = note,
                        CCcode = (acc.AccCostCenter > 0 ? acc.AccCostCenter : -1).ToString(),
                        salesman = salesman
                    });
                }

                entry.Accounts = accounts;

                EntryOper entryOper = new EntryOper();
                if (!entryOper.SaveEnty(entry))
                {
                    DXMessageBox.Show("خطأ أثناء الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info($"{(IsNew ? "تم حفظ" : "تم تعديل")} سند قيد برقم {EntryNo} بواسطة المستخدم {MainClass.UserName}");

                if (Sync.ActiveSync && Sync.SyncType > 0)
                    entryOper.SyncEntry(entry, IsNew);

                frmSavedMsg savedMsg = new frmSavedMsg();
                if (!IsNew)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";

                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    NewEntry();
                else if (savedMsg.Pressed == 2)
                {
                    txtNo.Background = new SolidColorBrush(Colors.WhiteSmoke);
                    txtNo.Foreground = new SolidColorBrush(Colors.Black);
                    txtEntryGID.Background = new SolidColorBrush(Colors.WhiteSmoke);
                    txtEntryGID.Foreground = new SolidColorBrush(Colors.Black);
                    IsNew = false;
                }
                else if (savedMsg.Pressed == 3)
                    Close();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Entry where {GetCondBranch()} type=10 and IS_Deleted=0 order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Entry where {GetCondBranch()} type=10 and IS_Deleted=0 order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Entry where {GetCondBranch()} type=10 and IS_Deleted=0 and id>{EntryID} order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"select top 1 * from Entry where {GetCondBranch()} type=10 and IS_Deleted=0 and id<{EntryID} order by id desc");
        }

        private string GetCondBranch()
        {
            return MainClass.BranchNo != -1 ? $"branch={MainClass.BranchNo} and " : "";
        }

        #endregion

        #region Print

        private void btnPrint_Click(object sender, RoutedEventArgs e) { PrintDevexpress(1); }
        private void btnView_Click(object sender, RoutedEventArgs e) { PrintDevexpress(2); }

        private void PrintDevexpress(int printMode)
        {
            try
            {
                if (EntryAccList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RptUrl))
                {
                    DXMessageBox.Show("يجب تحديد مسار التقرير", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fullPath = Path.Combine(RptUrl, RptName);
                if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
                {
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // استخدام التقرير الأصلي بنفس البيانات
                DataSet dataSource = BindToData();

                var xtraReport = DevExpress.XtraReports.UI.XtraReport.FromFile(fullPath);
                xtraReport.DataSource = dataSource;
                xtraReport.PrinterName = defPrinter;

                if (printMode == 1)
                {
                    for (int i = 0; i < PrintNo; i++)
                        xtraReport.Print();
                }
                else
                {
                    xtraReport.ShowPreviewDialog();
                }

                xtraReport.Dispose();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=9", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();

                    PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                    RptName = dt.Rows[0]["RptName"]?.ToString() ?? "";
                    RptUrl = Path.GetDirectoryName(dt.Rows[0]["RptUrl"]?.ToString() ?? "") ?? "";

                    if (string.IsNullOrEmpty(RptUrl) || !Directory.Exists(RptUrl))
                        RptUrl = MainClass.ReportsPath;
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                    defPrinter = MainClass.ReportsPrinter;
                    RptName = "Entry.repx";
                }
            }
            catch { }
        }

        private DataSet BindToData()
        {
            string dateText = txtDate.SelectedDate?.ToShortDateString() ?? "";
            List<RestrictionData> list = new List<RestrictionData>();

            foreach (DgvAccount acc in EntryAccList)
            {
                list.Add(new RestrictionData
                {
                    AccountName = acc.AccName,
                    AccountCode = acc.AccCode,
                    Description = acc.AccDescription,
                    Credit = acc.AccCredit.ToString(Common.DigitsNo),
                    Dept = acc.AccDebit.ToString(Common.DigitsNo),
                    Status = " ",
                    Branch = MainClass.BranchName,
                    RestrNo = txtNo.Text,
                    RestrType = "سند قيد يومية",
                    RestDate = dateText,
                    RestTime = dateText,
                    User = Editereciept.Text,
                    SumDept = _entryRows.Sum(r => r.AccDebit).ToString(Common.DigitsNo),
                    SumCredit = _entryRows.Sum(r => r.AccCredit).ToString(Common.DigitsNo),
                    PrintDate = DateTime.Now.ToShortDateString(),
                    Note = txtNotes.Text
                });
            }

            DataSet dataSet = new DataSet("Name");
            dataSet.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        #endregion

        #region Import / Export / Copy

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAll();
                NewEntry();

                if (DXMessageBox.Show("هل أنت متأكد من استيراد البيانات؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                frmImportDataGeneral importForm = new frmImportDataGeneral();
                importForm.cmbInv.SelectedIndex = 3;
                importForm.cmbInv.Visibility = Visibility.Visible;
                importForm.cmbInv.IsEnabled = false;
                importForm.cmbDataTable.Visibility = Visibility.Collapsed;
                importForm.ShowDialog();

                if (!importForm.ISDone || importForm.dtIniRes.Rows.Count == 0) return;

                for (int i = 0; i < importForm.dtIniRes.Rows.Count; i++)
                {
                    int accCode = 0;
                    int.TryParse(importForm.dtIniRes.Rows[i]["AccCode"].ToString(), out accCode);

                    DgvAccount acc = new DgvAccount
                    {
                        AccRowIndex = i + 1,
                        AccCode = importForm.dtIniRes.Rows[i]["AccCode"].ToString(),
                        AccName = GetAccountName(accCode),
                        AccDebit = Convert.ToDouble(importForm.dtIniRes.Rows[i]["Dept"]),
                        AccCredit = Convert.ToDouble(importForm.dtIniRes.Rows[i]["Credit"]),
                        AccCostCenter = -1,
                        AccDescription = importForm.dtIniRes.Rows[i]["Note"]?.ToString() ?? ""
                    };

                    EntryAccList.Add(acc);
                    _entryRows.Add(acc);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsEntryExist())
                {
                    DXMessageBox.Show("يجب حفظ القيد قبل التصدير", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من تصدير البيانات؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "قيد_يومية",
                    Filter = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveDialog.ShowDialog() != true) return;

                ExportToHtml(saveDialog.FileName);
                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportToHtml(string fileName)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine("<style>table{border-collapse:collapse;font-family:Tahoma;font-size:11px;}");
            sb.AppendLine("th,td{border:1px solid #999;padding:4px;text-align:center;}");
            sb.AppendLine("th{background:#DDE7FF;font-weight:bold;}</style></head>");
            sb.AppendLine("<body dir='rtl'>");
            sb.AppendLine($"<h2 style='text-align:center;'>سند قيد يومية رقم {txtNo.Text}</h2>");
            sb.AppendLine("<table><tr><th>#</th><th>رمز الحساب</th><th>اسم الحساب</th><th>مدين</th><th>دائن</th><th>الشرح</th></tr>");

            foreach (DgvAccount acc in EntryAccList)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{acc.AccRowIndex}</td><td>{acc.AccCode}</td><td>{acc.AccName}</td>");
                sb.AppendLine($"<td>{acc.AccDebit:N2}</td><td>{acc.AccCredit:N2}</td><td>{acc.AccDescription}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table></body></html>");
            File.WriteAllText(fileName, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsEntryExist())
                {
                    DXMessageBox.Show("يجب حفظ القيد قبل النسخ", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل تريد عمل نسخة من هذا القيد إلى قيد جديد؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                LoadResNo();
                txtDate.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private bool IsEntryExist()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select id from Entry where type=10 and doc_no={txtNo.Text}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        #endregion

        #region Documents

        private void ToolImportDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|PDF Files (*.pdf)|*.pdf",
                    Title = "تحديد الملفات",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                    EntryOper.filePathDocument = dialog.FileNames;
            }
            catch { }
        }

        private void ToolShowDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsNew)
                {
                    DXMessageBox.Show("يجب حفظ السند أولًا", "المدقق",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                DataTable dtt = new DataTable();
                EntryOper.getdocuments(ref dtt, txtEntryGID.Text);

                if (dtt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد مستندات", "المدقق",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Frmshowdocument docForm = new Frmshowdocument();

                DataTable displayTable = new DataTable();
                displayTable.Columns.Add("RowNo", typeof(int));
                displayTable.Columns.Add("FileName", typeof(string));
                displayTable.Columns.Add("FileUrl", typeof(string));

                for (int i = 0; i < dtt.Rows.Count; i++)
                {
                    displayTable.Rows.Add(
                        i + 1,
                        dtt.Rows[i]["FileName"]?.ToString() ?? "",
                        dtt.Rows[i]["FileUrl"]?.ToString() ?? "");
                }

                docForm.DataGridView1.ItemsSource = displayTable.DefaultView;
                docForm.type = 1;
                docForm.GlobalIdDoc = txtEntryGID.Text;
                docForm.Show();
            }
            catch
            {
            }
        }

        #endregion

        #region Context Menu Button

        private void BtnContextMenu_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = FindResource("EntryContextMenu") as ContextMenu;
            if (menu == null) return;

            menu.PlacementTarget = btnContextMenu;
            menu.IsOpen = true;
        }

        #endregion

        #region Summary & Helpers

        private void UpdateSummary()
        {
            double sumDebit = 0, sumCredit = 0;
            foreach (DgvAccount acc in _entryRows)
            {
                sumDebit += acc.AccDebit;
                sumCredit += acc.AccCredit;
            }

            txtSumDebit.Text = sumDebit.ToString("N2");
            txtSumCredit.Text = sumCredit.ToString("N2");

            double diff = Math.Abs(Math.Round(sumDebit, 2) - Math.Round(sumCredit, 2));
            LBLDiff.Text = diff.ToString("N2");

            if (Math.Round(sumDebit, 2) == Math.Round(sumCredit, 2))
            {
                LBLDiff.Foreground = new SolidColorBrush(Colors.LimeGreen);
                Label8.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
            else
            {
                LBLDiff.Foreground = new SolidColorBrush(Colors.OrangeRed);
                Label8.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
        }

        private DateTime BuildDateTime(DateTime date, string timeText)
        {
            if (TimeSpan.TryParse(timeText?.Trim(), out TimeSpan ts))
                return date.Date.Add(ts);
            return date.Date;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    // ─── Model Classes ────────────────────────────────────────────────────────

    public class EntrySearchRow
    {
        public string Column7 { get; set; }
        public string Column3 { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column4 { get; set; }
    }
}