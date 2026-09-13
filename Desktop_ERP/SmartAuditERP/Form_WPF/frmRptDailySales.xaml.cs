using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptDailySales : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private int    _printNo    = 1;
        private string _defPrinter = "";
        private string _rptName    = "daysalesPos.repx";
        private string _rptUrl     = "";

        private ObservableCollection<DailySaleRow> _dataSource
            = new ObservableCollection<DailySaleRow>();

        private double _grandTotalBeforeTax = 0;
        private double _grandTotalTax       = 0;
        private double _grandTotal          = 0;

        #endregion

        #region Constructor

        public frmRptDailySales()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            GridControl1.ItemsSource = _dataSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureOpen(_conn);
            cmbInvType.SelectedIndex = 1;
            LoadBranches();

            txtStartDate.DateTime = DateTime.Now;
            txtEndDate.DateTime   = DateTime.Now;
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
                }
            }
            catch { }
        }

        #endregion

        #region CheckBox Events

        private void chkAllPeriod_Changed(object sender, RoutedEventArgs e)
        {
            // لا تعطيل في هذه الواجهة (الكل = لا تاريخ)
        }

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = (chkAllBranches.IsChecked != true);
            if (chkAllBranches.IsChecked == true)
                cmbBranches.SelectedIndex = -1;
        }

        #endregion

        #region Show Results

        private void btnShow_Click(object sender, RoutedEventArgs e) => ShowResult();

        private void ShowResult()
        {
            try
            {
                _dataSource.Clear();
                _grandTotalBeforeTax = 0;
                _grandTotalTax       = 0;
                _grandTotal          = 0;

                int invType = (cmbInvType.SelectedIndex == 0) ? 2 : 3;

                var cmd = new SqlCommand(
                    "SELECT * FROM dbo.SalesByDay(@branch, @InvType, @StartDate, @EndDate)",
                    _conn);

                if (chkAllPeriod.IsChecked == true)
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = DBNull.Value;
                    cmd.Parameters.Add("@EndDate",   SqlDbType.DateTime).Value = DBNull.Value;
                }
                else
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value =
                        txtStartDate.DateTime.Date + new TimeSpan(0, 0, 0);
                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value =
                        txtEndDate.DateTime.Date + new TimeSpan(23, 59, 59);
                }

                cmd.Parameters.Add("@branch", SqlDbType.Int).Value = (chkAllBranches.IsChecked == true)
                    ? (object)DBNull.Value
                    : cmbBranches.SelectedValue ?? (object)DBNull.Value;

                cmd.Parameters.Add("@InvType", SqlDbType.Int).Value = invType;

                EnsureOpen(_conn);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt2 = new DataTable();
                    da.Fill(dt2);

                    if (dt2.Rows.Count == 0)
                    {
                        DXMessageBox.Show("لا توجد بيانات، أدخل الفترة الزمنية الصحيحة",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int rowNo = 1;
                    string langName = (MainClass.Language == "en-us") ? "en" : "ar";

                    foreach (DataRow row in dt2.Rows)
                    {
                        string dateStr = row["InvDate"].ToString();
                        string dayName = "";
                        if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                            dayName = parsedDate.ToString("ddd", new CultureInfo(langName));

                        double saleNet  = Convert.ToDouble(row["SaleNet"]);
                        double total    = Convert.ToDouble(row["Total"]);
                        double totalTax = Convert.ToDouble(row["TotalTax"]);

                        _dataSource.Add(new DailySaleRow
                        {
                            Dgnos    = rowNo++,
                            Ddates   = parsedDate,
                            daydv    = dayName,
                            Total    = total,
                            TotalTax = totalTax,
                            totsal   = saleNet
                        });

                        _grandTotalBeforeTax += total;
                        _grandTotalTax       += totalTax;
                        _grandTotal          += saleNet;
                    }
                }

                lblTotalBeforeTax.Text = _grandTotalBeforeTax.ToString("N2");
                lblTotalTax.Text       = _grandTotalTax.ToString("N2");
                lblGrandTotal.Text     = _grandTotal.ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dataSource.Count == 0) return;

                var sb = new StringBuilder();
                sb.AppendLine("الرقم,التاريخ,اليوم,الإجمالي قبل الضريبة,الضريبة,الإجمالي");

                foreach (var row in _dataSource)
                    sb.AppendLine($"{row.Dgnos},{row.Ddates:d},{row.daydv}," +
                                  $"{row.Total:N2},{row.TotalTax:N2},{row.totsal:N2}");

                sb.AppendLine($",,الإجمالي,{_grandTotalBeforeTax:N2},{_grandTotalTax:N2},{_grandTotal:N2}");

                string fileName = "DailySales.csv";
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(fileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير: " + ex.Message);
            }
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<InventoryData>();

            string foundation = "", vatNo = "";
            if (Common.FoundationInfoDT.Rows.Count > 0)
            {
                foundation = Common.FoundationInfoDT.Rows[0]["nameA"].ToString();
                vatNo      = Common.FoundationInfoDT.Rows[0]["tax_no"].ToString();
            }

            foreach (var row in _dataSource)
            {
                list.Add(new InventoryData
                {
                    Foundation    = foundation,
                    VATNo         = vatNo,
                    InvDate       = row.Ddates.ToShortDateString(),
                    InvDay        = row.daydv,
                    Net           = row.totsal.ToString("N2"),
                    Total         = row.Total.ToString("N2"),
                    Tax           = row.TotalTax.ToString("N2"),
                    FromDate      = txtStartDate.DateTime.ToShortDateString(),
                    ToDate        = txtEndDate.DateTime.ToShortDateString(),
                    PrintDate     = DateTime.Now.ToShortDateString(),
                    InventoryType = this.Title,
                    User          = MainClass.UserName,
                    NetTotal      = _grandTotal.ToString("N2"),
                    SummaryTotalBeforeTax = _grandTotalBeforeTax.ToString("N2"),
                    SummaryTotalTax       = _grandTotalTax.ToString("N2")
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptName    = "daysalesPos.repx";
            _defPrinter = MainClass.ReportsPrinter;
            _rptUrl     = MainClass.ReportsPath;

            if (_dataSource.Count == 0)
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

        #region Model

        public class DailySaleRow
        {
            public int      Dgnos    { get; set; }
            public DateTime Ddates   { get; set; }
            public string   daydv    { get; set; }
            public double   Total    { get; set; }
            public double   TotalTax { get; set; }
            public double   totsal   { get; set; }
        }

        #endregion
    }
}