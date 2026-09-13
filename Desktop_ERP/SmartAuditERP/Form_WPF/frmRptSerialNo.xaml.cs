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
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptSerialNo : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields
        private SqlConnection conn;
        private int SelectedId = -1;
        private ObservableCollection<SerialNoRow> SerialList;
        #endregion

        #region Constructor
        public frmRptSerialNo()
        {
            InitializeComponent();
            conn       = MainClass.ConnObj();
            SerialList = new ObservableCollection<SerialNoRow>();
            GridControl1.ItemsSource = SerialList;
        }
        #endregion

        #region Window Loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Today;
            txtDateTo.DateTime   = DateTime.Today;
            LoadBranches();
        }
        #endregion

        #region Load Helpers
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
                SerialList.Clear();

                var cmd = new SqlCommand(
                    "SELECT * FROM funGetInvoiceItemDetails(" +
                    "@ItemSerialNo, @Branch, @ItemId, @StartDate, @EndDate)",
                    conn);

                // الرقم التسلسلي
                cmd.Parameters.AddWithValue("@ItemSerialNo",
                    string.IsNullOrEmpty(txtSeialNo.Text)
                        ? (object)DBNull.Value
                        : txtSeialNo.Text);

                // الفرع
                cmd.Parameters.AddWithValue("@Branch",
                    (chkAllBranch.IsChecked != true &&
                     cmbBranches.SelectedValue != null)
                        ? cmbBranches.SelectedValue
                        : (object)DBNull.Value);

                // الصنف
                cmd.Parameters.AddWithValue("@ItemId",
                    SelectedId != -1 ? (object)SelectedId : DBNull.Value);

                // التاريخ
                cmd.Parameters.AddWithValue("@StartDate", SqlDbType.Date).Value =
                    ckTotalPeriod.IsChecked == true
                        ? (object)DBNull.Value
                        : txtDateFrom.DateTime.ToShortDateString();

                cmd.Parameters.AddWithValue("@EndDate", SqlDbType.Date).Value =
                    ckTotalPeriod.IsChecked == true
                        ? (object)DBNull.Value
                        : txtDateTo.DateTime.AddHours(24).ToShortDateString();

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    short invTypeNo  = Convert.ToInt16(row["inv_type"]);
                    short procTypeNo = Convert.ToInt16(row["proc_type"]);
                    string invoiceType = InvoiceOper.GetInvoiceType(
                                            invTypeNo, procTypeNo, 0, 1);

                    SerialList.Add(new SerialNoRow
                    {
                        DgvNo        = i + 1,
                        ItemName     = row["ItemName"]?.ToString() ?? "",
                        ItemCode     = row["ItemCode"]?.ToString() ?? "",
                        ItemSerialNo = row["ItemSerialNo"]?.ToString() ?? "",
                        InvType      = invoiceType,
                        InvNo        = row["InvNo"]?.ToString() ?? "",
                        InvDate      = row["InvDate"]?.ToString() ?? "",
                        BranchName   = row["BranchName"]?.ToString() ?? "",
                        InvGlobalID  = row["InvGlobalID"]?.ToString() ?? "",
                        ProcType     = Convert.ToInt32(row["proc_type"]),
                        InvTypeNo    = Convert.ToInt32(row["inv_type"]),
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

        #region Open Invoice Helper (WPF)
        private static void OpenWindow<T>(Action<T> configure)
            where T : System.Windows.Window, new()
        {
            var win = new T();
            configure(win);
            win.WindowState = System.Windows.WindowState.Maximized;
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            win.Activate();
        }

        private void OpenInvoiceByTypeAndProc(int invTypeNo, int procType,
                                               string globalId)
        {
            string navSql =
                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND InvGlobalID=N'{globalId}'";

            switch (invTypeNo)
            {
                case 2 when procType == 1:
                    OpenWindow<frmInvSale>(w =>
                    {
                        w.InvType = 2; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 2 when procType == 2:
                    OpenWindow<frmInvSale>(w =>
                    {
                        w.Title = "مرتجع مبيعات";
                        w.InvType = 2; w.ProcType = 2;
                        w.Navigate(navSql);
                    });
                    break;
                case 1 when procType == 1:
                    OpenWindow<frmInvPurch>(w =>
                    {
                        w.InvType = 1; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 1 when procType == 2:
                    OpenWindow<frmInvPurch>(w =>
                    {
                        w.Title = "مرتجع مشتريات";
                        w.InvType = 1; w.ProcType = 2;
                        w.Navigate(navSql);
                    });
                    break;
                case 3 when procType == 1:
                    OpenWindow<frmInvPOS>(w =>
                    {
                        w.ProcType = 1; w.Navigate(navSql);
                    });
                    break;
                case 3 when procType == 2:
                    OpenWindow<frmInvPOS>(w =>
                    {
                        w.Title = "مرتجع"; w.ProcType = 2;
                        w.Navigate(navSql);
                    });
                    break;
                case 9:
                    OpenWindow<frmInvPurch>(w =>
                    {
                        w.Title = "بضاعة أول مدة";
                        w.InvType = 9; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 4:
                    OpenWindow<frmInvInputOutput>(w =>
                    {
                        w.InvType = 4; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 5:
                    OpenWindow<frmInvInputOutput>(w =>
                    {
                        w.Title = "فاتورة إخراج";
                        w.InvType = 5; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 8 when procType == 1:
                    OpenWindow<frmInventoryTransfer>(w =>
                    {
                        w.InvType = 8; w.ProcType = 1;
                        w.Navigate(navSql);
                    });
                    break;
                case 8 when procType == 2:
                    OpenWindow<frmInventoryTransfer>(w =>
                    {
                        w.InvType = 8; w.ProcType = 2;
                        w.Navigate(navSql);
                    });
                    break;
                default:
                    DXMessageBox.Show("لا يوجد نموذج مخصص لهذا النوع.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
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
            // أيضاً زر البحث
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

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isAll;
            txtDateTo.IsEnabled   = !isAll;
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

        private void btnShowInv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SerialNoRow item)
            {
                try
                {
                    OpenInvoiceByTypeAndProc(item.InvTypeNo, item.ProcType,
                                             item.InvGlobalID);
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (SerialList == null || SerialList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"تسلسلات_{DateTime.Now:yyyyMMdd_HHmm}.csv");

                using var writer = new System.IO.StreamWriter(
                    path, false, System.Text.Encoding.UTF8);

                writer.WriteLine(
                    "م,الصنف,رمز الصنف,التسلسل,نوع الفاتورة," +
                    "رقم الفاتورة,التاريخ,الفرع");

                foreach (var item in SerialList)
                {
                    writer.WriteLine(
                        $"{item.DgvNo},{item.ItemName},{item.ItemCode}," +
                        $"{item.ItemSerialNo},{item.InvType}," +
                        $"{item.InvNo},{item.InvDate},{item.BranchName}");
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
                if (SerialList == null || SerialList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string rptUrl  = MainClass.ReportsPath;
                string printer = MainClass.ReportsPrinter;
                string rptName = "rptSerialNo.repx";
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
                DXMessageBox.Show($"خطأ في الطباعة:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BuildReportDataSet()
        {
            var list = SerialList.Select(item => new InventoryData
            {
                ItemName    = item.ItemName,
                ItemCode    = item.ItemCode,
                ItemSerialNo = item.ItemSerialNo,
                InvType     = item.InvType,
                InvoiceNo   = item.InvNo,
                InvDate     = item.InvDate,
                BranchName  = item.BranchName,
                FromDate    = txtDateFrom.DateTime.ToShortDateString(),
                ToDate      = txtDateTo.DateTime.ToShortDateString(),
                Status      = " ",
                InventoryType = Title,
                PrintDate   = DateTime.Now.ToShortDateString(),
                EmpName     = MainClass.UserName,
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }
        #endregion
    }
}