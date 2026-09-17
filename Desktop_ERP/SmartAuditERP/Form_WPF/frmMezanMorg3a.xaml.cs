using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmMezanMorg3a : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Inner Class (AccountNode)

        public class AccountNode
        {
            public int TrParentAcc { get; set; }
            public string TrAccName { get; set; }
            public int TrAccount { get; set; }
            public decimal TrDeptIntial { get; set; }
            public decimal TrCreditIntial { get; set; }
            public decimal TrDeptPervious { get; set; }
            public decimal TrCreditPervious { get; set; }
            public decimal TrDebtorMovement { get; set; }
            public decimal TrCreditorMovement { get; set; }
            public decimal TrDeptBlc { get; set; }
            public decimal TrCreditBlc { get; set; }
            public decimal TrDeptFinal { get; set; }
            public decimal TrCreditFinal { get; set; }
            public decimal TrType { get; set; }

            public AccountNode(int parentAcc, string accName, int account,
                decimal deptInitial, decimal creditInitial,
                decimal deptPervious, decimal creditPervious,
                decimal debtorMovement, decimal creditorMovement,
                decimal deptBlc, decimal creditBlc,
                decimal deptFinal, decimal creditFinal,
                decimal accType)
            {
                TrParentAcc = parentAcc;
                TrAccName = accName;
                TrAccount = account;
                TrDeptIntial = deptInitial;
                TrCreditIntial = creditInitial;
                TrDeptPervious = deptPervious;
                TrCreditPervious = creditPervious;
                TrDebtorMovement = debtorMovement;
                TrCreditorMovement = creditorMovement;
                TrDeptBlc = deptBlc;
                TrCreditBlc = creditBlc;
                TrDeptFinal = deptFinal;
                TrCreditFinal = creditFinal;
                TrType = accType;
            }
        }

        #endregion

        #region Fields

        private SqlConnection conn;
        private int _ParentCode = -1;
        public int _Type = 1;
        private int CurrentLevel = 0;
        private int MaxLevel = 3;

        private bool PrintHeader = true;
        private bool PrintFooter = true;
        private bool PrintStamp = true;
        private int PrintType = 0;
        private int PrintNo = 1;
        private string defPrinter = "";
        private string RptName = "";
        private string RptUrl = "";
        public bool chkType = false;

        private static List<AccountNode> AccountList = new List<AccountNode>();
        private ObservableCollection<AccountNode> _displayRows;

        #endregion

        #region Constructor & Load

        public frmMezanMorg3a()
        {
            conn = MainClass.ConnObj();
            _displayRows = new ObservableCollection<AccountNode>();
            InitializeComponent();
        }

        private void frmMezanMorg3a_Load(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;

            TreeList1.ItemsSource = _displayRows;

            LoadBranches();
            LoadPrintSettings();
            LoadtTreeList();
            CalcSum();
        }

        public void LoadBranches()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select id,name from Branches order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.SelectedValue = MainClass.BranchNo;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region CheckBoxes

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isPeriodEnabled = chkAll.IsChecked != true;
            if (txtDateFrom != null) txtDateFrom.IsEnabled = isPeriodEnabled;
            if (txtDateTo != null) txtDateTo.IsEnabled = isPeriodEnabled;
        }

        private void ckAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbBranches == null) return;

            if (ckAllBranches.IsChecked == true)
            {
                cmbBranches.SelectedIndex = -1;
                cmbBranches.IsEnabled = false;
            }
            else
            {
                cmbBranches.IsEnabled = true;
            }
        }

        #endregion

        #region Core Calculation (The 1359 lines logic)

        private void GetParent(int Code)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter($"select ParentCode from Accounts_Index where Code={Code}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double parentCodeVal = 0;
                    double.TryParse(dt.Rows[0][0].ToString(), out parentCodeVal);

                    if (parentCodeVal < 10.0)
                        _ParentCode = Code;
                    else
                        GetParent((int)parentCodeVal);
                }
            }
            catch { }
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            LoadtTreeList();
            CalcSum();
        }

        private void LoadtTreeList()
        {
            try
            {
                _displayRows.Clear();
                AccountList.Clear();

                GetData();

                // فلترة وعرض حسب الخيارات
                foreach (AccountNode node in AccountList)
                {
                    bool show = true;

                    if (chkActiveAcc.IsChecked == true)
                    {
                        if (node.TrDeptIntial == 0 && node.TrCreditIntial == 0 &&
                            node.TrDebtorMovement == 0 && node.TrCreditorMovement == 0 &&
                            node.TrDeptBlc == 0 && node.TrCreditBlc == 0 &&
                            node.TrDeptFinal == 0 && node.TrCreditFinal == 0)
                        {
                            show = false;
                        }
                    }

                    if (show)
                        _displayRows.Add(node);
                }

                UpdateRowCount();
                CurrentLevel = 0;
                MaxLevel = 3;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public List<AccountNode> GetData()
        {
            try
            {
                string branchFilter = "";
                if (cmbBranches.SelectedIndex > -1)
                {
                    branchFilter = $"Entry.branch={cmbBranches.SelectedValue} and Entry_sub.branch={cmbBranches.SelectedValue} and ";
                }

                AccountList.Clear();

                string dateFilter = "";
                if (chkAll.IsChecked != true)
                    dateFilter += " and Entry.date>=@date1 and Entry.date<@date2 ";

                string typeFilter = chkSecondery.IsChecked == true ? " where type = 2 order by ParentCode" : "";

                SqlDataAdapter adapter = new SqlDataAdapter($"select Code,AName,ParentCode,type from Accounts_Index {typeFilter}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    if (chkSecondery.IsChecked != true)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            int parentAcc = 0;
                            if (!string.IsNullOrEmpty(dt.Rows[i]["ParentCode"].ToString()))
                                int.TryParse(dt.Rows[i]["ParentCode"].ToString(), out parentAcc);

                            string accName = $"({dt.Rows[i]["Code"]}){dt.Rows[i]["AName"]}";
                            int accCode = Convert.ToInt32(dt.Rows[i]["Code"]);
                            decimal accType = Convert.ToDecimal(dt.Rows[i]["type"]);

                            AccountList.Add(new AccountNode(parentAcc, accName, accCode, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, accType));
                        }

                        TravarseTree(new AccountNode(0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
                    }
                    else
                    {
                        // فقط الحسابات الفرعية
                        DateTime dtFrom = txtDateFrom.SelectedDate ?? DateTime.Today;
                        DateTime dtTo = (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);

                        for (int j = 0; j < dt.Rows.Count; j++)
                        {
                            int currentCode = Convert.ToInt32(dt.Rows[j]["Code"]);

                            SqlDataAdapter movAdapter = new SqlDataAdapter(
                                $"select Entry_sub.acc_no, sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                                $"from Entry,Entry_sub where {branchFilter} Entry_sub.acc_no={currentCode} " +
                                $"and Entry.IS_Deleted=0 and Entry.type<>0 and Entry.state=1 and Entry.GlobalID=Entry_sub.EntryGlobalID {dateFilter} group by Entry_sub.acc_no", conn);
                            movAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                            movAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;

                            DataTable movDt = new DataTable();
                            movAdapter.Fill(movDt);

                            SqlDataAdapter initAdapter = new SqlDataAdapter(
                                $"select Entry_sub.acc_no, sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                                $"from Entry,Entry_sub where {branchFilter} Entry_sub.acc_no={currentCode} " +
                                $"and Entry.IS_Deleted=0 and Entry.type=0 and Entry.state=1 and Entry.GlobalID=Entry_sub.EntryGlobalID {dateFilter} group by Entry_sub.acc_no", conn);
                            initAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                            initAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;

                            DataTable initDt = new DataTable();
                            initAdapter.Fill(initDt);

                            double num3 = 0.0, num4 = 0.0, num5 = 0.0, num6 = 0.0;

                            if (initDt.Rows.Count > 0)
                            {
                                double.TryParse(initDt.Rows[0][1].ToString(), out num3);
                                double.TryParse(initDt.Rows[0][2].ToString(), out num4);
                            }

                            if (movDt.Rows.Count > 0 &&
                                (!string.IsNullOrEmpty(movDt.Rows[0][0].ToString()) || !string.IsNullOrEmpty(movDt.Rows[0][1].ToString())))
                            {
                                double.TryParse(movDt.Rows[0][1].ToString(), out num5);
                                double.TryParse(movDt.Rows[0][2].ToString(), out num6);
                            }

                            double num7 = 0, num8 = 0;
                            if (num5 >= num6)
                            { num7 = num5 - num6; num8 = 0; }
                            else
                            { num8 = num6 - num5; num7 = 0; }

                            double num9 = num3 + num7;
                            double num10 = num4 + num8;

                            if (num9 >= num10)
                            { num9 -= num10; num10 = 0; }
                            else
                            { num10 -= num9; num9 = 0; }

                            int parentAcc2 = 0;
                            if (!string.IsNullOrEmpty(dt.Rows[j]["ParentCode"].ToString()))
                                int.TryParse(dt.Rows[j]["ParentCode"].ToString(), out parentAcc2);

                            string accName = $"({currentCode}){dt.Rows[j]["AName"]}";
                            decimal accType = Convert.ToDecimal(dt.Rows[j]["type"]);

                            AccountList.Add(new AccountNode(
                                parentAcc2, accName, currentCode,
                                (decimal)num3, (decimal)num4,
                                0, 0,
                                (decimal)num5, (decimal)num6,
                                (decimal)num7, (decimal)num8,
                                (decimal)num9, (decimal)num10,
                                accType));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            return AccountList;
        }

        private void TravarseTree(AccountNode root)
        {
            try
            {
                string branchFilter = "";
                if (cmbBranches.SelectedIndex > -1)
                    branchFilter = $"Entry.branch={cmbBranches.SelectedValue} and Entry_sub.branch={cmbBranches.SelectedValue} and ";

                string dateFilter = "";
                if (chkAll.IsChecked != true)
                    dateFilter += " and Entry.date>=@date1 and Entry.date<@date2 ";

                DateTime dtFrom = txtDateFrom.SelectedDate ?? DateTime.Today;
                DateTime dtTo = (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);

                foreach (AccountNode account in AccountList)
                {
                    if (account.TrParentAcc != root.TrAccount) continue;

                    if (account.TrType == 2m)
                    {
                        SqlDataAdapter movAdapter = new SqlDataAdapter(
                            $"select Entry_sub.acc_no, sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                            $"from Entry,Entry_sub where {branchFilter} Entry_sub.acc_no={account.TrAccount} " +
                            $"and Entry.IS_Deleted=0 and Entry.type<>0 and Entry.state=1 and Entry.GlobalID=Entry_sub.EntryGlobalID {dateFilter} group by Entry_sub.acc_no", conn);
                        movAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                        movAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;
                        DataTable movDt = new DataTable();
                        movAdapter.Fill(movDt);

                        SqlDataAdapter initAdapter = new SqlDataAdapter(
                            $"select Entry_sub.acc_no, sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                            $"from Entry,Entry_sub where {branchFilter} Entry_sub.acc_no={account.TrAccount} " +
                            $"and Entry.IS_Deleted=0 and Entry.type=0 and Entry.state=1 and Entry.GlobalID=Entry_sub.EntryGlobalID {dateFilter} group by Entry_sub.acc_no", conn);
                        initAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                        initAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;
                        DataTable initDt = new DataTable();
                        initAdapter.Fill(initDt);

                        SqlDataAdapter prevAdapter = new SqlDataAdapter(
                            $"select sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                            $"from Entry,Entry_sub where {branchFilter} Entry.IS_Deleted=0 and Entry.state=1 " +
                            $"and Entry.date<@date1 and Entry.GlobalID=Entry_sub.EntryGlobalID and Entry_sub.acc_no={account.TrAccount}", conn);
                        prevAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                        DataTable prevDt = new DataTable();
                        prevAdapter.Fill(prevDt);

                        double num = 0.0, num2 = 0.0, num3 = 0.0, num4 = 0.0, num5 = 0.0, num6 = 0.0;

                        if (chkAll.IsChecked != true && prevDt.Rows.Count > 0 && chkPrevbalance.IsChecked != true)
                        {
                            if (prevDt.Rows[0]["dept"] != DBNull.Value) double.TryParse(prevDt.Rows[0]["dept"].ToString(), out num);
                            if (prevDt.Rows[0]["credit"] != DBNull.Value) double.TryParse(prevDt.Rows[0]["credit"].ToString(), out num2);
                        }

                        if (initDt.Rows.Count > 0)
                        {
                            double.TryParse(initDt.Rows[0][1].ToString(), out num3);
                            double.TryParse(initDt.Rows[0][2].ToString(), out num4);
                        }

                        if (movDt.Rows.Count > 0 &&
                           (!string.IsNullOrEmpty(movDt.Rows[0][0].ToString()) || !string.IsNullOrEmpty(movDt.Rows[0][1].ToString())))
                        {
                            double.TryParse(movDt.Rows[0][1].ToString(), out num5);
                            double.TryParse(movDt.Rows[0][2].ToString(), out num6);
                        }

                        double num7 = 0, num8 = 0;
                        if (num5 >= num6)
                        { num7 = num5 - num6; num8 = 0; }
                        else
                        { num8 = num6 - num5; num7 = 0; }

                        double num9 = num3 + num7 + num;
                        double num10 = num4 + num8 + num2;

                        if (num9 >= num10)
                        { num9 -= num10; num10 = 0; }
                        else
                        { num10 -= num9; num9 = 0; }

                        account.TrDeptIntial = (decimal)num3;
                        account.TrCreditIntial = (decimal)num4;
                        account.TrDeptPervious = (decimal)num;
                        account.TrCreditPervious = (decimal)num2;
                        account.TrDebtorMovement = (decimal)num5;
                        account.TrCreditorMovement = (decimal)num6;
                        account.TrDeptBlc = (decimal)num7;
                        account.TrCreditBlc = (decimal)num8;
                        account.TrDeptFinal = (decimal)num9;
                        account.TrCreditFinal = (decimal)num10;
                    }
                    else
                    {
                        TravarseTree(account);
                    }

                    root.TrDeptIntial += Math.Round(account.TrDeptIntial, 2);
                    root.TrCreditIntial += Math.Round(account.TrCreditIntial, 2);
                    root.TrDeptPervious += Math.Round(account.TrDeptPervious, 2);
                    root.TrCreditPervious += Math.Round(account.TrCreditPervious, 2);
                    root.TrDebtorMovement += Math.Round(account.TrDebtorMovement, 2);
                    root.TrCreditorMovement += Math.Round(account.TrCreditorMovement, 2);

                    if (root.TrType != 2m)
                    {
                        if (root.TrDebtorMovement >= root.TrCreditorMovement)
                        {
                            root.TrDeptBlc = root.TrDebtorMovement - root.TrCreditorMovement;
                            root.TrCreditBlc = 0;
                        }
                        else
                        {
                            root.TrDeptBlc = 0;
                            root.TrCreditBlc = root.TrCreditorMovement - root.TrDebtorMovement;
                        }

                        root.TrDeptFinal = root.TrDeptIntial + root.TrDeptBlc;
                        root.TrCreditFinal = root.TrCreditIntial + root.TrCreditBlc;

                        if (root.TrDeptFinal >= root.TrCreditFinal)
                        {
                            root.TrDeptFinal -= root.TrCreditFinal;
                            root.TrCreditFinal = 0;
                        }
                        else
                        {
                            root.TrCreditFinal -= root.TrDeptFinal;
                            root.TrDeptFinal = 0;
                        }
                    }
                    else
                    {
                        root.TrDeptBlc += Math.Round(account.TrDeptBlc, 2);
                        root.TrCreditBlc += Math.Round(account.TrCreditBlc, 2);
                        root.TrDeptFinal += Math.Round(account.TrDeptFinal, 2);
                        root.TrCreditFinal += Math.Round(account.TrCreditFinal, 2);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region CalcSum

        private void CalcSum()
        {
            decimal sumDebt = 0, sumCredit = 0, sumFinalDebt = 0, sumFinalCredit = 0;

            foreach (AccountNode row in _displayRows)
            {
                sumDebt += row.TrDebtorMovement;
                sumCredit += row.TrCreditorMovement;
                sumFinalDebt += row.TrDeptFinal;
                sumFinalCredit += row.TrCreditFinal;
            }

            txtSumDebtorMovement.Text = sumDebt.ToString("N2");
            txtSumCreditorMovement.Text = sumCredit.ToString("N2");
            txtSumDeptFinal.Text = sumFinalDebt.ToString("N2");
            txtSumCreditFinal.Text = sumFinalCredit.ToString("N2");

            bool isBalanced = Math.Round(sumDebt, 1) == Math.Round(sumCredit, 1);

            txtIsBalanced.Text = isBalanced
                ? "✅ ميزان المراجعة متوازن"
                : "❌ ميزان المراجعة غير متوازن";

            txtIsBalanced.Foreground = isBalanced
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);
        }

        #endregion

        #region UI Interactions

        private void btnLvl1_Click(object sender, RoutedEventArgs e) { FilterByLevel(0); }
        private void btnLvl2_Click(object sender, RoutedEventArgs e) { FilterByLevel(1); }
        private void btnlvl3_Click(object sender, RoutedEventArgs e) { FilterByLevel(2); }
        private void btnlvl4_Click(object sender, RoutedEventArgs e)
        {
            LoadtTreeList();
            CalcSum();
        }

        private void FilterByLevel(int maxLevel)
        {
            _displayRows.Clear();

            foreach (AccountNode node in AccountList)
            {
                if (node.TrParentAcc == 0 && maxLevel >= 0)
                {
                    _displayRows.Add(node);
                }
                else if (maxLevel >= 1)
                {
                    bool parentIsRoot = AccountList.Exists(p => p.TrAccount == node.TrParentAcc && p.TrParentAcc == 0);
                    if (parentIsRoot) _displayRows.Add(node);
                }
            }

            UpdateRowCount();
            CalcSum();
        }

        private void TreeList1_RightClick(object sender, MouseButtonEventArgs e)
        {
            AccountNode selected = TreeList1.SelectedItem as AccountNode;
            if (selected == null) return;

            ContextMenu menu = new ContextMenu();
            MenuItem menuItem = new MenuItem { Header = "📊 كشف الحساب" };
            menuItem.Click += (s, args) => ShowDetails(selected);
            menu.Items.Add(menuItem);
            menu.IsOpen = true;
        }

        private void ShowDetails(AccountNode node)
        {
            if (node.TrType == 2m)
            {
                frmAccountBalance balanceForm = new frmAccountBalance();
                MainClass.ApplyPermissionToForm(balanceForm);
                MainClass.DoApplyUserSett(balanceForm);
                balanceForm.Show();
                balanceForm.cmbAccounts.SelectedValue = node.TrAccount;
                balanceForm.ShowAccountData();
                balanceForm.Activate();
            }
            else
            {
                DXMessageBox.Show("كشف الحساب فقط للحسابات الفرعية", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Print & Export

        private void btnPreview_Click(object sender, RoutedEventArgs e) { PrintDevexpress(2); }
        private void btnPrint_Click(object sender, RoutedEventArgs e) { PrintDevexpress(1); }
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_displayRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    FileName = "ميزان_المراجعة",
                    Filter = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveDialog.ShowDialog() != true) return;

                ExportToExcelHtml(saveDialog.FileName);
                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportToExcelHtml(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine("<style>table{border-collapse:collapse;font-family:Tahoma;font-size:11px;}");
            sb.AppendLine("th,td{border:1px solid #999;padding:4px;text-align:center;}");
            sb.AppendLine("th{background:#DDE7FF;font-weight:bold;}</style></head>");
            sb.AppendLine("<body dir='rtl'>");
            sb.AppendLine($"<h2 style='text-align:center;'>{Title}</h2>");
            sb.AppendLine("<table><tr>");
            sb.AppendLine("<th>الحساب</th><th>اسم الحساب</th>");
            sb.AppendLine("<th>افتتاحي مدين</th><th>افتتاحي دائن</th>");
            sb.AppendLine("<th>رصيد سابق مدين</th><th>رصيد سابق دائن</th>");
            sb.AppendLine("<th>حركة مدين</th><th>حركة دائن</th>");
            sb.AppendLine("<th>رصيد مدين</th><th>رصيد دائن</th>");
            sb.AppendLine("<th>ختامي مدين</th><th>ختامي دائن</th>");
            sb.AppendLine("</tr>");

            foreach (AccountNode row in _displayRows)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{row.TrAccount}</td><td>{row.TrAccName}</td>");
                sb.AppendLine($"<td>{row.TrDeptIntial:N2}</td><td>{row.TrCreditIntial:N2}</td>");
                sb.AppendLine($"<td>{row.TrDeptPervious:N2}</td><td>{row.TrCreditPervious:N2}</td>");
                sb.AppendLine($"<td>{row.TrDebtorMovement:N2}</td><td>{row.TrCreditorMovement:N2}</td>");
                sb.AppendLine($"<td>{row.TrDeptBlc:N2}</td><td>{row.TrCreditBlc:N2}</td>");
                sb.AppendLine($"<td>{row.TrDeptFinal:N2}</td><td>{row.TrCreditFinal:N2}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table></body></html>");
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        private void LoadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=12", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return;

                PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                defPrinter = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(defPrinter))
                    defPrinter = Common.GetDefaultPrinter();

                PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                RptName = dt.Rows[0]["RptName"]?.ToString() ?? "";
                RptUrl = dt.Rows[0]["RptUrl"]?.ToString() ?? "";
            }
            catch { }
        }

        private void PrintDevexpress(int printMode)
        {
            try
            {
                RptUrl = MainClass.ReportsPath;
                RptName = "rptMezanMorag3a.repx";
                defPrinter = MainClass.ReportsPrinter;

                if (_displayRows.Count == 0)
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
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FlowDocument document = BuildPrintDocument();

                if (printMode == 2)
                {
                    System.Windows.Window previewWindow = new System.Windows.Window
                    {
                        Title = "معاينة قبل الطباعة",
                        Width = 1100,
                        Height = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        FlowDirection = System.Windows.FlowDirection.RightToLeft,
                        Background = System.Windows.Media.Brushes.White
                    };
                    DocumentViewer viewer = new DocumentViewer { Document = document };
                    previewWindow.Content = viewer;
                    previewWindow.ShowDialog();
                }
                else
                {
                    PrintDialog printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        document.PageHeight = printDialog.PrintableAreaHeight;
                        document.PageWidth = printDialog.PrintableAreaWidth;
                        document.PagePadding = new Thickness(25);
                        document.ColumnGap = 0;
                        document.ColumnWidth = printDialog.PrintableAreaWidth;

                        IDocumentPaginatorSource source = document;
                        for (int i = 0; i < PrintNo; i++)
                            printDialog.PrintDocument(source.DocumentPaginator, Title);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private FlowDocument BuildPrintDocument()
        {
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 9,
                PagePadding = new Thickness(15),
                ColumnWidth = 1400
            };

            Paragraph title = new Paragraph(new Run(Title))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#213A7A"))
            };
            doc.Blocks.Add(title);

            string periodText = chkAll.IsChecked == true
                ? "كل الفترة"
                : $"من {txtDateFrom.SelectedDate?.ToShortDateString()} إلى {txtDateTo.SelectedDate?.ToShortDateString()}";

            Paragraph info = new Paragraph(new Run($"📅 الفترة: {periodText}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(info);

            Table table = new Table { CellSpacing = 0 };
            for (int i = 0; i < 10; i++)
                table.Columns.Add(new TableColumn());

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow headerRow = new TableRow();
            rowGroup.Rows.Add(headerRow);

            AddHeaderCell(headerRow, "الحساب");
            AddHeaderCell(headerRow, "اسم الحساب");
            AddHeaderCell(headerRow, "افتتاحي مدين");
            AddHeaderCell(headerRow, "افتتاحي دائن");
            AddHeaderCell(headerRow, "حركة مدين");
            AddHeaderCell(headerRow, "حركة دائن");
            AddHeaderCell(headerRow, "رصيد مدين");
            AddHeaderCell(headerRow, "رصيد دائن");
            AddHeaderCell(headerRow, "ختامي مدين");
            AddHeaderCell(headerRow, "ختامي دائن");

            foreach (AccountNode row in _displayRows)
            {
                TableRow dataRow = new TableRow();
                rowGroup.Rows.Add(dataRow);
                AddBodyCell(dataRow, row.TrAccount.ToString());
                AddBodyCell(dataRow, row.TrAccName);
                AddBodyCell(dataRow, row.TrDeptIntial.ToString("N2"));
                AddBodyCell(dataRow, row.TrCreditIntial.ToString("N2"));
                AddBodyCell(dataRow, row.TrDebtorMovement.ToString("N2"));
                AddBodyCell(dataRow, row.TrCreditorMovement.ToString("N2"));
                AddBodyCell(dataRow, row.TrDeptBlc.ToString("N2"));
                AddBodyCell(dataRow, row.TrCreditBlc.ToString("N2"));
                AddBodyCell(dataRow, row.TrDeptFinal.ToString("N2"));
                AddBodyCell(dataRow, row.TrCreditFinal.ToString("N2"));
            }

            doc.Blocks.Add(table);

            Paragraph summary = new Paragraph
            {
                Margin = new Thickness(0, 10, 0, 0),
                FontWeight = FontWeights.Bold
            };

            summary.Inlines.Add(new Run($"مجموع مدين الحركة: {txtSumDebtorMovement.Text}   |   "));
            summary.Inlines.Add(new Run($"مجموع دائن الحركة: {txtSumCreditorMovement.Text}   |   "));
            summary.Inlines.Add(new Run($"مجموع ختامي مدين: {txtSumDeptFinal.Text}   |   "));
            summary.Inlines.Add(new Run($"مجموع ختامي دائن: {txtSumCreditFinal.Text}"));
            doc.Blocks.Add(summary);

            return doc;
        }

        private void AddHeaderCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDE7FF")),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4)
            };
            cell.Blocks.FirstBlock.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        private void AddBodyCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text ?? "")))
            {
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(3)
            };
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        #endregion

        #region Helpers

        private void UpdateRowCount()
        {
            lblRowCount.Text = $"عدد السجلات: {_displayRows.Count:N0}";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}