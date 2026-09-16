using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using log4net;
using Microsoft.Win32;
using UtilitiesProj;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCustAccount : ThemedWindow
    {
        #region ── Private Fields ──

        private SqlConnection conn;
        private bool PrintHeader = true;
        private bool PrintFooter = true;
        private bool PrintStamp = true;
        private int PrintType;
        private int PrintNo = 1;
        private string defPrinter = "";
        private string RptName = "";
        private string RptUrl = "";
        private double DgvDebitSum = 0.0;
        private double DgvCreditSum = 0.0;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Public Fields ──

        public int clientType = 1;

        #endregion

        #region ── Constructor ──

        public frmCustAccount()
        {
            conn = MainClass.ConnObj();
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            clientType = 1;
            DgvDebitSum = 0.0;
            DgvCreditSum = 0.0;

            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Now.Date;
            txtDateTo.SelectedDate = DateTime.Now.Date;

            // تحديث عنوان الشاشة بناءً على نوع العميل
            if (clientType == 2)
            {
                LblAcc.Text = (MainClass.Language == "ar") ? "🏭 مورد" : "Supplier";
                this.Title = (MainClass.Language == "ar")
                    ? "أرصدة الموردين" : "Supplier account balances";
            }
            else
            {
                LblAcc.Text = (MainClass.Language == "ar") ? "👤 اسم العميل" : "Client";
            }

            LoadClients();
            LoadSalesmen();
            LoadPrintSettings();
        }

        #endregion

        #region ── Data Loading ──

        public void LoadClients()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE (type={clientType} OR type=3) " +
                    $"AND IS_Deleted=0 AND AccountCode > 0 ORDER BY id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbClients.ItemsSource = dt.DefaultView;
                cmbClients.DisplayMemberPath = "name";
                cmbClients.SelectedValuePath = "id";
                cmbClients.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCustAccount {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadSalesmen()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesman.ItemsSource = dt.DefaultView;
                cmbSalesman.DisplayMemberPath = "name";
                cmbSalesman.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCustAccount {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count != 1) return;

                PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                if (string.IsNullOrEmpty(defPrinter))
                    defPrinter = Common.GetDefaultPrinter();

                PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                RptName = dt.Rows[0]["RptName"].ToString();
                RptUrl = dt.Rows[0]["RptUrl"].ToString();
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message, "خطأ", MessageBoxImage.Error);
            }
        }

        #endregion

        #region ── Show Results ──

        private void ShowResult()
        {
            try
            {
                var resultTable = new DataTable();
                resultTable.Columns.Add("DgvNo", typeof(int));
                resultTable.Columns.Add("DgvAccCode", typeof(string));
                resultTable.Columns.Add("DgvAccName", typeof(string));
                resultTable.Columns.Add("DgvDebit", typeof(double));
                resultTable.Columns.Add("DgvCredit", typeof(double));
                resultTable.Columns.Add("DgvBalance", typeof(double));
                resultTable.Columns.Add("DgvStatus", typeof(string));

                string clientQuery =
                    $"SELECT id, name, AccountCode FROM Customers " +
                    $"WHERE (type={clientType} OR type=3) " +
                    $"AND IS_Deleted=0 AND AccountCode > 0";

                if (chkAll.IsChecked != true)
                    clientQuery += $" AND id={cmbClients.SelectedValue}";

                clientQuery += " ORDER BY id";

                var clientAdapter = new SqlDataAdapter(clientQuery, conn);
                var clientTable = new DataTable();
                clientAdapter.Fill(clientTable);

                resultTable.Rows.Clear();
                GridControl1.ItemsSource = null;

                double totalDebit = 0.0;
                double totalCredit = 0.0;

                ProgressBar1.Value = 0;
                ProgressBar1.Maximum = clientTable.Rows.Count;

                string dateFilter = BuildDateFilter();
                string branchFilter = BuildBranchFilter();
                string salesmanFilter = BuildSalesmanFilter();

                for (int i = 0; i < clientTable.Rows.Count; i++)
                {
                    ProgressBar1.Value = i + 1;

                    int accCode = Convert.ToInt32(clientTable.Rows[i]["AccountCode"]);
                    string name = clientTable.Rows[i]["name"].ToString();

                    var entryAdapter = new SqlDataAdapter(
                        $"SELECT SUM(Entry_sub.dept) AS dept, " +
                        $"SUM(Entry_sub.credit) AS credit " +
                        $"FROM Entry, Entry_sub " +
                        $"WHERE {branchFilter} Entry.IS_Deleted=0 " +
                        $"AND Entry.state=1 " +
                        $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                        $"AND Entry_sub.acc_no={accCode} " +
                        $"{dateFilter} {salesmanFilter}",
                        conn);

                    if (ckTotalPeriod.IsChecked != true)
                    {
                        entryAdapter.SelectCommand.Parameters.Add(
                            "@date1", SqlDbType.DateTime).Value =
                            (txtDateFrom.SelectedDate ?? DateTime.Now).Date.ToShortDateString();
                        entryAdapter.SelectCommand.Parameters.Add(
                            "@date2", SqlDbType.DateTime).Value =
                            (txtDateTo.SelectedDate ?? DateTime.Now).AddHours(24);
                    }

                    var entryTable = new DataTable();
                    entryAdapter.Fill(entryTable);

                    if (entryTable.Rows.Count > 0 &&
                        !string.IsNullOrEmpty(entryTable.Rows[0][0].ToString()))
                    {
                        double rowDebit = SafeParseDouble(entryTable.Rows[0][0].ToString());
                        double rowCredit = SafeParseDouble(entryTable.Rows[0][1].ToString());
                        totalDebit += rowDebit;
                        totalCredit += rowCredit;

                        double balance;
                        string status;

                        if (rowDebit >= rowCredit)
                        {
                            balance = Math.Round(rowDebit - rowCredit, 3);
                            status = (MainClass.Language == "ar") ? "مدين" : "Debit";
                        }
                        else
                        {
                            balance = Math.Round(rowCredit - rowDebit, 3);
                            status = (MainClass.Language == "ar") ? "دائن" : "Credit";
                        }

                        resultTable.Rows.Add(
                            resultTable.Rows.Count + 1,
                            accCode.ToString(),
                            name,
                            rowDebit,
                            rowCredit,
                            balance,
                            status);
                    }
                }

                GridControl1.ItemsSource = resultTable.DefaultView;

                // تحديث حالة الإجمالي
                totalDebit = Math.Round(totalDebit, 2);
                totalCredit = Math.Round(totalCredit, 2);

               // txtSumState.Text = totalDebit >= totalCredit
               //     ? ((MainClass.Language == "ar") ? "مدين" : "Debit")
               //     : ((MainClass.Language == "ar") ? "دائن" : "Credit");
                UpdateTotalLabels(totalDebit, totalCredit);
                ProgressBar1.Value = 0;
            }
            catch (Exception ex)
            {
                ShowMsg($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCustAccount {ex.Message} {MainClass.UserName}");
            }
        }

        private void UpdateTotalLabels(double totalDebit, double totalCredit)
        {
            double totalBalance = Math.Abs(totalDebit - totalCredit);

            // تحديث Labels الإجمالي
            if (FindName("lblTotalDebit") is TextBlock lblDebit)
                lblDebit.Text = totalDebit.ToString("n2");

            if (FindName("lblTotalCredit") is TextBlock lblCredit)
                lblCredit.Text = totalCredit.ToString("n2");

            if (FindName("lblTotalBalance") is TextBlock lblBalance)
                lblBalance.Text = totalBalance.ToString("n2");

            txtSumState.Text = totalDebit >= totalCredit
                ? ((MainClass.Language == "ar") ? "مدين" : "Debit")
                : ((MainClass.Language == "ar") ? "دائن" : "Credit");

            // تحديث قيم للـ BindToData
            DgvDebitSum = totalDebit;
            DgvCreditSum = totalCredit;
        }

        private string BuildDateFilter()
        {
            return (ckTotalPeriod.IsChecked != true)
                ? " AND Entry.date>=@date1 AND Entry.date<=@date2 "
                : "";
        }

        private string BuildBranchFilter()
        {
            return (MainClass.BranchNo != -1)
                ? $"Entry.branch={MainClass.BranchNo} " +
                  $"AND Entry_sub.branch={MainClass.BranchNo} AND "
                : "";
        }

        private string BuildSalesmanFilter()
        {
            if (chkSalesman.IsChecked != true &&
                cmbSalesman.SelectedValue != null)
                return $" AND Entry_sub.salesman={cmbSalesman.SelectedValue}";
            return "";
        }

        #endregion

        #region ── Print & Export ──

        private DataSet BindToData()
        {
            var list = new List<RestrictionData>();
            int rowCount = GetGridRowCount();

            // جلب بيانات المنشأة
            string address = "", telePhone = "", mobile = "";
            string foundation = "", field = "", vatNo = "";

            var foundationAdapter = new SqlDataAdapter(
                "SELECT * FROM Foundation", conn);
            var foundationDt = new DataTable();
            foundationAdapter.Fill(foundationDt);

            if (foundationDt.Rows.Count > 0)
            {
                var row = foundationDt.Rows[0];
                address = row["Address"].ToString();
                telePhone = row["Tel"].ToString();
                mobile = row["Mobile"].ToString();
                foundation = row["nameA"].ToString();
                field = row["FieldA"].ToString();
                vatNo = row["tax_no"].ToString();
            }

            for (int i = 0; i < rowCount; i++)
            {
                double rowDebit = SafeParseDouble(
                    GridControl1.GetCellValue(i, "DgvDebit")?.ToString());
                double rowCredit = SafeParseDouble(
                    GridControl1.GetCellValue(i, "DgvCredit")?.ToString());

                var data = new RestrictionData
                {
                    // ✅ بيانات العميل
                    AccountName = cmbClients.Text,
                    AccountCode = GridControl1.GetCellValue(i, "DgvAccCode")?.ToString(),
                    Description = GridControl1.GetCellValue(i, "DgvAccName")?.ToString(),

                    // ✅ بيانات الأرقام
                    Dept = rowDebit.ToString("n2"),
                    Credit = rowCredit.ToString("n2"),
                    Balance = GridControl1.GetCellValue(i, "DgvBalance")?.ToString(),
                    Status = GridControl1.GetCellValue(i, "DgvStatus")?.ToString(),

                    // ✅ بيانات التقرير
                    RestrType = this.Title,
                    FromDate = txtDateFrom.SelectedDate?.ToString("d") ?? "",
                    ToDate = txtDateTo.SelectedDate?.ToString("d") ?? "",
                    SumDept = DgvDebitSum.ToString("n2"),
                    SumCredit = DgvCreditSum.ToString("n2"),

                    // ✅ إجمالي الرصيد
                    // اختر واحدة من هاتين بناءً على ما هو موجود في RestrictionData:
                     BalanceInperiod = Math.Abs(DgvDebitSum - DgvCreditSum).ToString("n2"),
                    // أو:
                    // Total = Math.Abs(DgvDebitSum - DgvCreditSum).ToString("n2"),

                    // ✅ حالة الرصيد
                    // اختر واحدة:
                     BalanceType = txtSumState.Text,
                    // أو:
                    // Stall = txtSumState.Text,

                    // ✅ بيانات الطباعة
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString(),
                    RestDate = DateTime.Now.ToShortDateString(),
                    RestTime = "",

                    // ✅ بيانات المنشأة
                    Address = address,
                    Mobile = mobile,
                    TelePhone = telePhone,
                    VatNo = vatNo,
                    Foundation = foundation,
                    Field = field,
                    Logo = "",
                    Header = "",
                    footer = "",
                    Stamp = ""
                };
                list.Add(data);
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            list.Clear();
            return dataSet;
        }

        private void PrintDevexpress(int printType)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "rptCustAccount.repx";
            defPrinter = MainClass.ReportsPrinter;

            if (GetGridRowCount() == 0)
            {
                ShowMsg("لا توجد عمليات بالجدول", "", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(RptUrl))
            {
                ShowMsg("يجب تحديد مسار التقرير", "", MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                ShowMsg("المسار الحالي للتقارير غير موجود أو تم تعديله",
                        "خطأ", MessageBoxImage.Warning);
                return;
            }

            var report = XtraReport.FromFile(fullPath);
            report.DataSource = BindToData();

            string headerPath = Path.Combine(RptUrl, "header.repx");
            if (File.Exists(headerPath))
            {
                var headerReport = XtraReport.FromFile(headerPath);
                headerReport.DataSource = Common.FoundationInfoDT;
                var headerCtrl =
                    report.FindControl("headerRpt", true) as XRSubreport;
                if (headerCtrl != null) headerCtrl.ReportSource = headerReport;
            }

            string footerPath = Path.Combine(RptUrl, "footer.repx");
            if (File.Exists(footerPath))
            {
                var footerReport = XtraReport.FromFile(footerPath);
                footerReport.DataSource = Common.FoundationInfoDT;
                var footerCtrl =
                    report.FindControl("footerRpt", true) as XRSubreport;
                if (footerCtrl != null) footerCtrl.ReportSource = footerReport;
            }

            if (!string.IsNullOrEmpty(defPrinter))
            {
                report.PrinterName = defPrinter;
                if (printType == 1)
                    for (int i = 0; i < PrintNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            else
            {
                ShowMsg("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxImage.Warning);
            }
        }

        #endregion

        #region ── Add New Client ──

        private void AddNewClient()
        {
            try
            { 
                var form = new frmSrchClient();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Background = System.Windows.Media.Brushes.WhiteSmoke;
                form.lblName.Foreground = System.Windows.Media.Brushes.Black;
                form.lblMobile.Foreground = System.Windows.Media.Brushes.Black;
                form.Type = clientType;
                form.PostponeClient = true;
                form.ShowDialog();

                if (!string.IsNullOrEmpty(form.Clientname))
                {
                    LoadClients();
                    cmbClients.SelectedValue = form.ClientId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCustAccount {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── UI Events ──

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked != true && cmbClients.SelectedIndex == -1)
            {
                string msg = (clientType == 1)
                    ? "يجب اختيار العميل"
                    : "يجب اختيار مورد";
                ShowMsg(msg, "", MessageBoxImage.Warning);
                cmbClients.Focus();
                return;
            }
            ShowResult();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GetGridRowCount() <= 0)
                {
                    ShowMsg("لا توجد بيانات للتصدير", "", MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Title = "حفظ الملف",
                    Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = $"أرصدة_العملاء_{DateTime.Now:yyyyMMdd}.csv",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                    Process.Start(new ProcessStartInfo(saveDialog.FileName)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMsg($"خطأ\nتفاصيل الخطأ: {ex.Message}", "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCustAccount {ex.Message} {MainClass.UserName}");
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkSalesman_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSalesman.IsEnabled = chkSalesman.IsChecked != true;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isFullPeriod = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isFullPeriod;
            txtDateTo.IsEnabled = !isFullPeriod;
        }

        private void cmbClients_Click(object sender, MouseButtonEventArgs e)
        {
            if (chkAll.IsChecked != true)
                AddNewClient();
        }

        #endregion

        #region ── Helper Methods ──

        private int GetGridRowCount()
        {
            if (GridControl1.ItemsSource is DataView dv) return dv.Count;
            if (GridControl1.ItemsSource is DataTable dt) return dt.Rows.Count;
            return 0;
        }

        private void ExportToCsv(string filePath)
        {
            string[] headers =
            {
                "#", "رقم الحساب", "اسم العميل",
                "حركة مدين", "حركة دائن", "الرصيد", "الحالة"
            };

            string[] fields =
            {
                "DgvNo", "DgvAccCode", "DgvAccName",
                "DgvDebit", "DgvCredit", "DgvBalance", "DgvStatus"
            };

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            int rowCount = GetGridRowCount();
            for (int i = 0; i < rowCount; i++)
            {
                var values = new string[fields.Length];
                for (int j = 0; j < fields.Length; j++)
                {
                    string val =
                        GridControl1.GetCellValue(i, fields[j])?.ToString() ?? "";
                    values[j] = val.Contains(",")
                        ? $"\"{val}\"" : val;
                }
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static double SafeParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            return double.TryParse(value, out double result) ? result : 0.0;
        }

        private static void ShowMsg(
            string message,
            string title = "SmartAudit ERP",
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, icon,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);
        }

        #endregion
    }
}