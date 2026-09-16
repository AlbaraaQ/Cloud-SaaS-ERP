using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptSerialNoSummary : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields
        private SqlConnection conn;
        private int SelectedId = -1;
        private ObservableCollection<SerialSummaryRow> SummaryList;
        #endregion

        #region Constructor
        public frmRptSerialNoSummary()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            SummaryList = new ObservableCollection<SerialSummaryRow>();
            GridControl1.ItemsSource = SummaryList;
        }
        #endregion

        #region Window Loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
            => LoadBranches();
        #endregion

        #region Load Branches
        private void LoadBranches()
        {
            string extra = "";
            if (MainClass.BranchNo != -1 &&
                !string.Equals(Accounting.BranchCondition, " ",
                               StringComparison.Ordinal))
                extra = $" AND BranchId={MainClass.BranchNo}";

            var adapter = new SqlDataAdapter(
                $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0{extra}",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath  = "BranchId";
            cmbBranches.ItemsSource        = dt.DefaultView;
            cmbBranches.SelectedIndex      = -1;
        }
        #endregion

        #region ShowResults
        private void ShowResults()
        {
            try
            {
                SummaryList.Clear();

                string filterWhere = chkShowOnlyExisting.IsChecked == true
                    ? " WHERE result > 0 "
                    : "";

                var cmd = new SqlCommand(
                    "SELECT * FROM funCalculateSerialNoSummary(" +
                    "@ItemSerialNo, @itemid, @Branch)" + filterWhere,
                    conn);

                cmd.Parameters.AddWithValue("@ItemSerialNo",
                    string.IsNullOrEmpty(txtSeialNo.Text)
                        ? (object)DBNull.Value
                        : txtSeialNo.Text);

                cmd.Parameters.AddWithValue("@itemid",
                    SelectedId != -1 ? (object)SelectedId : DBNull.Value);

                cmd.Parameters.AddWithValue("@Branch",
                    (chkAllBranch.IsChecked != true &&
                     cmbBranches.SelectedValue != null)
                        ? cmbBranches.SelectedValue
                        : (object)DBNull.Value);

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    int itemId = Convert.ToInt32(row["itemid"]);

                    SummaryList.Add(new SerialSummaryRow
                    {
                        DgvNo        = i + 1,
                        ItemName     = Common.GetItemName(itemId),
                        ItemCode     = Common.GetItemCode(itemId),
                        ItemSerialNo = row["ItemSerialNo"]?.ToString() ?? "",
                        SerialQty    = Convert.ToDouble(row["result"]),
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Item Search
        private void addNewItem()
        {
            try
            {
                var form = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.sql    = "SELECT id, name, nameEN, sale_price, unit " +
                              "FROM Items WHERE IS_Deleted=0 ORDER BY id";
                form.search = "SELECT id, name, nameEN, sale_price, unit FROM Items";
                form.Itemname       = "";
                form.txtSrchNm.Text = txtItemName.Text;
                form.ShowDialog();

                if (form.ISDone && form.ItemId > 0)
                {
                    SelectedId       = form.ItemId;
                    txtItemCode.Text = Common.GetItemCode(SelectedId);
                    txtItemName.Text = form.Itemname;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region CheckBox Events
        private void chkAllBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranch.IsChecked != true;
            if (chkAllBranch.IsChecked == true)
                cmbBranches.SelectedIndex = -1;
        }

        private void ckAllItems_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckAllItems.IsChecked == true;
            txtItemCode.IsEnabled = !isAll;
            txtItemName.IsEnabled = !isAll;
            if (isAll)
            {
                txtItemCode.Text = "";
                txtItemName.Text = "";
                SelectedId       = -1;
            }
        }

        private void chkAllSeialNo_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            txtSeialNo.IsEnabled = chkAllSeialNo.IsChecked != true;
            if (chkAllSeialNo.IsChecked == true)
                txtSeialNo.Text = "";
        }
        #endregion

        #region Button Events
        private void BtnShow_Click(object sender, RoutedEventArgs e)
            => ShowResults();

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            addNewItem();
            ckAllItems.IsChecked = false;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (SummaryList == null || SummaryList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"أرصدة_تسلسلات_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using var writer = new System.IO.StreamWriter(
                    path, false, System.Text.Encoding.UTF8);

                writer.WriteLine("م,الصنف,رمز الصنف,التسلسل,العدد");

                foreach (var item in SummaryList)
                {
                    writer.WriteLine(
                        $"{item.DgvNo},{item.ItemName},{item.ItemCode}," +
                        $"{item.ItemSerialNo},{item.SerialQty:N2}");
                }

                Process.Start(new ProcessStartInfo(path)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);
        #endregion

        #region Print
        private void PrintReport(int printMode)
        {
            try
            {
                if (SummaryList == null || SummaryList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string rptUrl  = MainClass.ReportsPath;
                string printer = MainClass.ReportsPrinter;
                string rptName = "rptSerialNoSummary.repx";
                string full    = System.IO.Path.Combine(rptUrl, rptName);

                if (string.IsNullOrEmpty(rptUrl) ||
                    !Directory.Exists(rptUrl)    ||
                    !File.Exists(full))
                {
                    DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var report = XtraReport.FromFile(full);
                report.DataSource = BuildReportDataSet();

                string hPath = System.IO.Path.Combine(rptUrl, "header.repx");
                if (File.Exists(hPath))
                {
                    var hRpt = XtraReport.FromFile(hPath);
                    hRpt.DataSource = Common.FoundationInfoDT;
                    var hSub = (XRSubreport)report.FindControl(
                                   "headerRpt", ignoreCase: true);
                    if (hSub != null) hSub.ReportSource = hRpt;
                }

                string fPath = System.IO.Path.Combine(rptUrl, "footer.repx");
                if (File.Exists(fPath))
                {
                    var fRpt = XtraReport.FromFile(fPath);
                    fRpt.DataSource = Common.FoundationInfoDT;
                    var fSub = (XRSubreport)report.FindControl(
                                   "footerRpt", ignoreCase: true);
                    if (fSub != null) fSub.ReportSource = fRpt;
                }

                if (string.IsNullOrEmpty(printer))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = printer;
                if (printMode == 1) report.Print();
                else report.ShowPreviewDialog();
                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BuildReportDataSet()
        {
            var list = SummaryList.Select(item => new InventoryData
            {
                ItemName     = item.ItemName,
                ItemCode     = item.ItemCode,
                ItemSerialNo = item.ItemSerialNo,
                Quantity     = item.SerialQty.ToString("N2"),
                Status       = " ",
                InventoryType = Title,
                PrintDate    = DateTime.Now.ToShortDateString(),
                EmpName      = MainClass.UserName,
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }
        #endregion
    }
}