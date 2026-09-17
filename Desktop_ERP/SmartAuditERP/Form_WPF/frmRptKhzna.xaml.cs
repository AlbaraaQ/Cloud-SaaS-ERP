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
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptKhzna : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        // مصدر بيانات الجدول
        private ObservableCollection<KhznaRow> KhznaList;

        // قيم الرصيد
        private double totAll;
        private double totPeriod;

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmRptKhzna()
        {
            InitializeComponent();

            conn        = MainClass.ConnObj();
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";

            KhznaList = new ObservableCollection<KhznaRow>();
            dgvItems.ItemsSource = KhznaList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Today;
            txtDateTo.DateTime   = DateTime.Today;

            LoadSafes();
            LoadPrintSettings();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Load Helpers
        // ══════════════════════════════════════════════

        public void LoadSafes()
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id, name FROM Stocks " +
                $"WHERE branch={MainClass.BranchNo} " +
                $"AND IS_Deleted=0 AND status<>2 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSafe.DisplayMemberPath = "name";
            cmbSafe.SelectedValuePath  = "id";
            cmbSafe.ItemsSource        = dt.DefaultView;
            cmbSafe.SelectedIndex      = -1;
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        PrintType   = Convert.ToInt32(dt.Rows[0]["printType"]);
                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                        defPrinter  = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();
                        PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                        RptName = dt.Rows[0]["RptName"]?.ToString() ?? "";
                        RptUrl  = dt.Rows[0]["RptUrl"]?.ToString() ?? "";
                    }
                    catch { /* تجاهل */ }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region ShowResult
        // ══════════════════════════════════════════════

        private void ShowResult()
        {
            try
            {
                if (cmbSafe.SelectedValue == null)
                {
                    DXMessageBox.Show("اختر صندوقاً.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbSafe.Focus();
                    return;
                }

                KhznaList.Clear();
                totAll = 0; totPeriod = 0;

                // جلب رقم الحساب المرتبط بالصندوق
                int accNo = -1;
                var accAdapter = new SqlDataAdapter(
                    $"SELECT Accounts_Index.Code " +
                    $"FROM Accounts_Index, Stocks " +
                    $"WHERE Accounts_Index.AName=Stocks.name " +
                    $"AND Accounts_Index.acc_branch=Stocks.branch " +
                    $"AND Accounts_Index.Type=2 " +
                    $"AND Stocks.id={cmbSafe.SelectedValue}", conn);
                var accDt = new DataTable();
                accAdapter.Fill(accDt);

                if (accDt.Rows.Count > 0)
                    accNo = Convert.ToInt32(accDt.Rows[0][0]);

                if (accNo == -1)
                {
                    DXMessageBox.Show("لم يتم العثور على حساب مرتبط بهذا الصندوق.",
                                    "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string condDate = "";
                if (chkAll.IsChecked != true)
                    condDate = "AND Entry.date>=@date1 AND Entry.date<=@date2 ";

                // التواريخ
                DateTime dateFrom = txtDateFrom.DateTime != DateTime.MinValue
                    ? txtDateFrom.DateTime : DateTime.Today;
                DateTime dateTo = txtDateTo.DateTime != DateTime.MinValue
                    ? txtDateTo.DateTime : DateTime.Today;

                TimeSpan startTime = ParseTime(txtStartTime.Text, TimeSpan.Zero);
                TimeSpan endTime   = ParseTime(txtEndTime.Text,
                                               new TimeSpan(23, 59, 59));

                DateTime dt1 = dateFrom.Date + startTime;
                DateTime dt2 = dateTo.Date   + endTime;

                double prevIncome  = 0, prevExpense = 0;
                double runBalance  = 0;
                double priorBalance = 0;

                // ══ الرصيد السابق (فقط إذا فترة محددة) ══
                if (chkAll.IsChecked != true)
                {
                    var prevAdapter = new SqlDataAdapter(
                        $"SELECT SUM(Entry_sub.dept) AS dept, " +
                        $"SUM(Entry_sub.credit) AS credit " +
                        $"FROM Entry, Entry_sub " +
                        $"WHERE Entry.IS_Deleted=0 AND Entry.state=1 " +
                        $"AND Entry.date<@date1 " +
                        $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                        $"AND Entry_sub.acc_no={accNo}", conn);
                    prevAdapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        dateFrom.ToShortDateString();

                    var prevDt = new DataTable();
                    prevAdapter.Fill(prevDt);

                    if (prevDt.Rows.Count > 0)
                    {
                        prevIncome  = SafeDouble(prevDt.Rows[0]["dept"]);
                        prevExpense = SafeDouble(prevDt.Rows[0]["credit"]);
                        priorBalance = prevIncome - prevExpense;
                        runBalance   = priorBalance;
                    }

                    // إضافة سطر الرصيد السابق
                    KhznaList.Add(new KhznaRow
                    {
                        RowNo       = KhznaList.Count + 1,
                        ProcessType = "رصيد سابق",
                        ProcessNo   = "",
                        RestDate    = dateFrom.AddDays(-1).ToShortDateString(),
                        Income      = prevIncome,
                        Outcome     = prevExpense,
                        Balance     = priorBalance,
                        Note        = "",
                    });
                }

                // ══ الحركات في الفترة ══
                var mainAdapter = new SqlDataAdapter(
                    $"SELECT Entry.GlobalID, Entry.type, Entry.date, Entry.notes, " +
                    $"SUM(Entry_sub.dept) AS dept, " +
                    $"SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE Entry.IS_Deleted=0 AND Entry.state=1 " +
                    $"{condDate}" +
                    $"AND Entry.GlobalID=Entry_sub.EntryGlobalID " +
                    $"AND Entry_sub.acc_no={accNo} " +
                    $"GROUP BY Entry.GlobalID, Entry.type, " +
                    $"Entry.date, Entry.notes " +
                    $"ORDER BY Entry.date", conn);

                mainAdapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = dt1;
                mainAdapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dt2;

                var mainDt = new DataTable();
                mainAdapter.Fill(mainDt);

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = mainDt.Rows.Count > 0
                    ? mainDt.Rows.Count : 1;

                double periodRunBalance = runBalance;

                for (int i = 0; i < mainDt.Rows.Count; i++)
                {
                    DataRow row = mainDt.Rows[i];

                    double dept   = SafeDouble(row["dept"]);
                    double credit = SafeDouble(row["credit"]);

                    runBalance += dept - credit;

                    KhznaList.Add(new KhznaRow
                    {
                        RowNo       = KhznaList.Count + 1,
                        ProcessType = Common.ResrirectionType(
                                          Convert.ToInt32(row["type"])),
                        ProcessNo   = row["GlobalID"]?.ToString() ?? "",
                        RestDate    = Convert.ToDateTime(row["date"])
                                              .ToShortDateString(),
                        Income      = dept,
                        Outcome     = credit,
                        Balance     = runBalance,
                        Note        = row["notes"]?.ToString() ?? "",
                    });

                    ProgressBar1.Value = i + 1;
                }

                // حساب الأرصدة
                totAll    = runBalance;
                totPeriod = runBalance - priorBalance;

                // تحديث بطاقات الرصيد
                txtTotAll.Text    = $"{totAll:#,##0.##}";
                txtTotPeriod.Text = $"{totPeriod:#,##0.##}";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في عرض البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double SafeDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0.0;
            return double.TryParse(val.ToString(), out double r) ? r : 0.0;
        }

        private TimeSpan ParseTime(string text, TimeSpan def)
        {
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"hh\:mm", null, out var r1)) return r1;
            if (TimeSpan.TryParseExact(text?.Trim() ?? "",
                    @"h\:mm",  null, out var r2)) return r2;
            return def;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox Events
        // ══════════════════════════════════════════════

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAll.IsChecked == true;
            txtDateFrom.IsEnabled  = !isAll;
            txtDateTo.IsEnabled    = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled   = !isAll;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Time TextBox Events
        // ══════════════════════════════════════════════

        private void TimeBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void TimeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                bool ok =
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"hh\:mm", null, out _) ||
                    TimeSpan.TryParseExact(tb.Text.Trim(), @"h\:mm",  null, out _);
                if (!ok)
                    tb.Text = tb.Name == "txtStartTime" ? "00:00" : "23:59";
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Events
        // ══════════════════════════════════════════════

        private void btnShow_Click(object sender, RoutedEventArgs e)
            => ShowResult();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // تصدير CSV (بديل آمن عن Interop.Excel)
            if (KhznaList == null || KhznaList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter   = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"حركة_صندوق_{DateTime.Now:yyyyMMdd_HHmm}"
                };

                if (dlg.ShowDialog() != true) return;

                string path = dlg.FileName;

                using var writer = new System.IO.StreamWriter(
                    path, false, System.Text.Encoding.UTF8);

                writer.WriteLine("م,العملية,الرقم,التاريخ,وارد,صادر,الرصيد,البيان");

                foreach (var row in KhznaList)
                {
                    writer.WriteLine(
                        $"{row.RowNo},{row.ProcessType},{row.ProcessNo}," +
                        $"{row.RestDate},{row.Income:N2},{row.Outcome:N2}," +
                        $"{row.Balance:N2},{row.Note}");
                }

                DXMessageBox.Show($"تم حفظ الملف في:\n{path}", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(path)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في التصدير:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print
        // ══════════════════════════════════════════════

        private void PrintReport(int printMode)
        {
            try
            {
                if (KhznaList == null || KhznaList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                RptUrl     = MainClass.ReportsPath;
                RptName    = "RptKhzna.repx";
                defPrinter = MainClass.ReportsPrinter;

                string fullPath = Path.Combine(RptUrl, RptName);

                if (string.IsNullOrEmpty(RptUrl)  ||
                    !Directory.Exists(RptUrl)      ||
                    !File.Exists(fullPath))
                {
                    DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                // Header
                string hPath = Path.Combine(RptUrl, "header.repx");
                if (File.Exists(hPath))
                {
                    var hRpt = XtraReport.FromFile(hPath);
                    hRpt.DataSource = Common.FoundationInfoDT;
                    var hSub = (XRSubreport)report.FindControl(
                                   "headerRpt", ignoreCase: true);
                    if (hSub != null) hSub.ReportSource = hRpt;
                }

                // Footer
                string fPath = Path.Combine(RptUrl, "footer.repx");
                if (File.Exists(fPath))
                {
                    var fRpt = XtraReport.FromFile(fPath);
                    fRpt.DataSource = Common.FoundationInfoDT;
                    var fSub = (XRSubreport)report.FindControl(
                                   "footerRpt", ignoreCase: true);
                    if (fSub != null) fSub.ReportSource = fRpt;
                }

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;

                if (printMode == 1)
                    for (int i = 1; i <= PrintNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BuildReportDataSet()
        {
            var list = KhznaList.Select(row => new RestrictionData
            {
                Safe        = cmbSafe.Text,
                SafeCode    = cmbSafe.SelectedValue?.ToString() ?? "",
                ProcessType = row.ProcessType,
                ProcessNo   = row.ProcessNo,
                RestDate    = row.RestDate,
                Note        = row.Note,
                RestrType   = Title,
                RestTime    = "",
                Income      = row.Income.ToString("N2"),
                Outcome     = row.Outcome.ToString("N2"),
                Balance     = row.Balance.ToString("N2"),
                Total       = txtTotAll.Text,
                TotPeriod   = txtTotPeriod.Text,
                FromDate    = txtDateFrom.DateTime.ToShortDateString(),
                ToDate      = txtDateTo.DateTime.ToShortDateString(),
                User        = Common.GetEmpName(MainClass.EmpNo),
                PrintDate   = DateTime.Now.ToShortDateString(),
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion
    }
}