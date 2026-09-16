using System;
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
    public partial class frmRptCategorySaleByDay : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        private ObservableCollection<CategorySaleRow> _dataSource
            = new ObservableCollection<CategorySaleRow>();

        private double _grandTotal = 0;

        #endregion

        #region Constructor

        public frmRptCategorySaleByDay()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            GridControl1.ItemsSource = _dataSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBranches();
            cmbInvType.SelectedIndex = 1;
            LoadGroup();

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

        private void LoadGroup()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT CategoryId, name FROM ItemsCategory WHERE IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCategry.ItemsSource       = dt.DefaultView;
                    cmbCategry.DisplayMemberPath = "name";
                    cmbCategry.SelectedValuePath = "CategoryId";
                    cmbCategry.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        #endregion

        #region CheckBox Events

        private void chkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = (chkAllBranches.IsChecked != true);
            if (chkAllBranches.IsChecked == true)
                cmbBranches.SelectedIndex = -1;
        }

        private void chkAllPeriod_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enabled = (chkAllPeriod.IsChecked != true);
            txtStartDate.IsEnabled = enabled;
            txtEndDate.IsEnabled   = enabled;
        }

        private void CheckBoxgroup_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbCategry.IsEnabled = (CheckBoxgroup.IsChecked != true);
            if (CheckBoxgroup.IsChecked == true)
                cmbCategry.SelectedIndex = -1;
        }

        #endregion

        #region Show Results

        private void btnShowResults_Click(object sender, RoutedEventArgs e) => ShowResults();

        private void ShowResults()
        {
            try
            {
                _dataSource.Clear();
                _grandTotal    = 0;
                lblCount.Text  = "0";
                lblTotal.Text  = "0.00";
                ProgressBar1.Value = 0;

                var cmd = new SqlCommand("proGetCategorySaleByDay");

                // الفرع
                if (chkAllBranches.IsChecked == true)
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value =
                        cmbBranches.SelectedValue ?? (object)DBNull.Value;

                // نوع الفاتورة
                int invType = (cmbInvType.SelectedIndex == 0) ? 2 : 3;
                cmd.Parameters.Add("@inv_type", SqlDbType.Int).Value = invType;

                // الفترة
                if (chkAllPeriod.IsChecked != true)
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = txtStartDate.DateTime;
                    cmd.Parameters.Add("@EndDate",   SqlDbType.Date).Value = txtEndDate.DateTime.AddDays(1);
                }
                else
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.Date).Value = DBNull.Value;
                    cmd.Parameters.Add("@EndDate",   SqlDbType.Date).Value = DBNull.Value;
                }

                // المجموعة
                if (CheckBoxgroup.IsChecked == true)
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = DBNull.Value;
                else
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value =
                        cmbCategry.SelectedValue ?? (object)DBNull.Value;

                cmd.Connection   = _conn;
                cmd.CommandType  = CommandType.StoredProcedure;

                using (var da = new SqlDataAdapter(cmd))
                {
                    da.SelectCommand.CommandTimeout = 300;
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        DXMessageBox.Show("لا توجد بيانات، أدخل الفترة الزمنية الصحيحة",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    ProgressBar1.Maximum = dt.Rows.Count;
                    string langName = (MainClass.Language == "en-us") ? "en" : "ar";

                    int rowNo = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        string code     = row["CtgyCode"].ToString();
                        string catName  = row["CategoryName"].ToString();
                        string dateStr  = row["InvDate"].ToString();
                        double saleNet  = 0;
                        double.TryParse(row["SaleNet"].ToString(), out saleNet);

                        string dayName = "";
                        if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                            dayName = parsedDate.ToString("ddd", new CultureInfo(langName));

                        _dataSource.Add(new CategorySaleRow
                        {
                            RowIndex   = rowNo,
                            CtgyCode   = code,
                            CtgyName   = catName,
                            SaleDay    = dayName,
                            CtgyDate   = parsedDate,
                            CtgyTotale = saleNet,
                            IsOdd      = rowNo % 2 != 0
                        });

                        _grandTotal     += saleNet;
                        ProgressBar1.Value = rowNo++;
                    }
                }

                lblCount.Text = _dataSource.Count.ToString();
                lblTotal.Text = _grandTotal.ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Print / Export

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);
        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dataSource.Count == 0) return;

                var sb = new StringBuilder();
                sb.AppendLine("#,الرمز,المجموعة,اليوم,التاريخ,الإجمالي");

                foreach (var row in _dataSource)
                {
                    sb.AppendLine(
                        $"{row.RowIndex},{row.CtgyCode},{row.CtgyName}," +
                        $"{row.SaleDay},{row.CtgyDate:d},{row.CtgyTotale:N2}");
                }

                sb.AppendLine($",,,,الإجمالي,{_grandTotal:N2}");

                string fileName = $"CategorySaleByDay_{DateTime.Now:yyyyMMdd_HHmm}.csv";
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

        private void PrintDevexpress(int printType)
        {
            string rptName    = "RptCategorySaleByDay.repx";
            string printer    = MainClass.ReportsPrinter;
            string reportsPath = MainClass.ReportsPath;

            if (_dataSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(reportsPath))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(reportsPath, rptName);
            if (!Directory.Exists(reportsPath) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BindToData();

                string headerPath = Path.Combine(reportsPath, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
                }

                string footerPath = Path.Combine(reportsPath, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var subFooter = report.FindControl("footerRpt", true) as XRSubreport;
                    if (subFooter != null) subFooter.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(printer))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = printer;
                if (printType == 1)
                    report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message);
            }
        }

        private DataSet BindToData()
        {
            var list = new System.Collections.Generic.List<InventoryData>();

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
                    InvDate       = row.CtgyDate.ToString("d"),
                    InvDay        = row.SaleDay,
                    CategoryCode  = row.CtgyCode,
                    CategoryName  = row.CtgyName,
                    Net           = row.CtgyTotale.ToString("N2"),
                    FromDate      = txtStartDate.DateTime.ToShortDateString(),
                    ToDate        = txtEndDate.DateTime.ToShortDateString(),
                    PrintDate     = DateTime.Now.ToShortDateString(),
                    InventoryType = this.Title,
                    User          = MainClass.UserName,
                    NetTotal      = _grandTotal.ToString("N2")
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion

        #region Model

        public class CategorySaleRow
        {
            public int      RowIndex   { get; set; }
            public string   CtgyCode   { get; set; }
            public string   CtgyName   { get; set; }
            public string   SaleDay    { get; set; }
            public DateTime CtgyDate   { get; set; }
            public double   CtgyTotale { get; set; }
            public bool     IsOdd      { get; set; }
        }

        #endregion
    }
}