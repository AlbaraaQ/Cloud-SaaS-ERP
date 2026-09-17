using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSafeGrdToDate : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields
        private SqlConnection conn;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<StockRow> StockList;
        #endregion

        #region Constructor
        public frmSafeGrdToDate()
        {
            InitializeComponent();
            conn      = MainClass.ConnObj();
            StockList = new ObservableCollection<StockRow>();
            dgvItems.ItemsSource = StockList;
        }
        #endregion

        #region Window Loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.DateTime = DateTime.Today;
            LoadSafes();
            LoadPrintSettings();
        }
        #endregion

        #region Load Safes
        public void LoadSafes()
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id, name FROM Safes " +
                $"WHERE branch={MainClass.BranchNo} " +
                $"AND status=1 AND IS_Deleted=0 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSafes.DisplayMemberPath = "name";
            cmbSafes.SelectedValuePath  = "id";
            cmbSafes.ItemsSource        = dt.DefaultView;
            cmbSafes.SelectedIndex      = -1;
        }
        #endregion

        #region Load Print Settings
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

        #region CalcStock
        private async void CalcStock()
        {
            try
            {
                StockList.Clear();
                txtSum.Text = "0.00";

                string condStore = "";
                string condBranch = "";

                if (chkAll.IsChecked != true &&
                    cmbSafes.SelectedValue != null)
                    condStore = $" inv.safe={cmbSafes.SelectedValue} and ";

                if (MainClass.BranchNo != -1)
                    condBranch = $"inv.branch={MainClass.BranchNo} and ";

                DateTime toDate = txtDate.DateTime != DateTime.MinValue
                    ? txtDate.DateTime : DateTime.Today;

                DateTime toDatePlus = toDate.AddHours(24);

                // جدول تجميع الكميات بالصنف
                var summaryDt = await Task.Run(() =>
                    AggregateStock(condBranch, condStore, toDate, toDatePlus));

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value   = 0;
                    ProgressBar1.Maximum = summaryDt.Rows.Count > 0
                        ? summaryDt.Rows.Count : 1;
                });

                double totalCost = 0.0;
                var resultRows   = new System.Collections.Generic.List<StockRow>();

                await Task.Run(() =>
                {
                    for (int i = 0; i < summaryDt.Rows.Count; i++)
                    {
                        DataRow row    = summaryDt.Rows[i];
                        int itemId     = Convert.ToInt32(row[0]);
                        double qty     = Convert.ToDouble(row[1]);

                        // حساب متوسط التكلفة
                        double totCostAll = 0.0;
                        int    totQtyAll  = 0;

                        using (var localConn = MainClass.ConnObj())
                        {
                            localConn.Open();

                            // مشتريات مع proc_type=1
                            var adp1 = new SqlDataAdapter(
                                $"SELECT SUM(val), SUM(val1*exchange_price) " +
                                $"FROM inv, inv_sub " +
                                $"WHERE {condBranch} ItemId={itemId} " +
                                $"AND inv.proc_type=1 AND inv_sub.proc_type=1 " +
                                $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                                $"AND IS_Deleted=0", localConn);
                            var dt1 = new DataTable();
                            adp1.Fill(dt1);
                            if (dt1.Rows.Count > 0 &&
                                dt1.Rows[0][1]?.ToString() != "")
                            {
                                totCostAll += Convert.ToDouble(dt1.Rows[0][1]);
                                totQtyAll  += Convert.ToInt32(
                                    Math.Round(Convert.ToDouble(dt1.Rows[0][0])));
                            }

                            // بضاعة أول مدة مع proc_type=3
                            var adp2 = new SqlDataAdapter(
                                $"SELECT SUM(val), SUM(val1*exchange_price) " +
                                $"FROM inv, inv_sub " +
                                $"WHERE {condBranch} ItemId={itemId} " +
                                $"AND inv.proc_type=3 " +
                                $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                                $"AND IS_Deleted=0", localConn);
                            var dt2 = new DataTable();
                            adp2.Fill(dt2);
                            if (dt2.Rows.Count > 0 &&
                                dt2.Rows[0][1]?.ToString() != "")
                            {
                                totCostAll += Convert.ToDouble(dt2.Rows[0][1]);
                                totQtyAll  += Convert.ToInt32(
                                    Math.Round(Convert.ToDouble(dt2.Rows[0][0])));
                            }
                        }

                        double avgCost = totQtyAll > 0
                            ? Math.Floor(totCostAll / totQtyAll * 1e8) / 1e8
                            : 0.0;
                        double itemTotCost = Math.Round(avgCost * qty, 4);

                        totalCost += itemTotCost;

                        resultRows.Add(new StockRow
                        {
                            ItemId   = itemId,
                            ItemName = GetItemName(itemId),
                            Qty      = Math.Round(qty, 2),
                            AvgCost  = avgCost,
                            TotCost  = itemTotCost,
                        });

                        Dispatcher.Invoke(() => ProgressBar1.Value = i + 1);
                    }
                });

                Dispatcher.Invoke(() =>
                {
                    foreach (var r in resultRows)
                        StockList.Add(r);
                    txtSum.Text = totalCost.ToString("N2");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// تجميع الكميات من جميع حركات المخزن
        /// </summary>
        private DataTable AggregateStock(
            string condBranch, string condStore,
            DateTime toDate, DateTime toDatePlus)
        {
            // جدول مؤقت: (ItemId, qty, type)
            var stagingDt = new DataTable();
            stagingDt.Columns.Add("ItemId");
            stagingDt.Columns.Add("qty");
            stagingDt.Columns.Add("type");

            using var localConn = MainClass.ConnObj();
            localConn.Open();

            void AddRows(string sql, int type)
            {
                var adp = new SqlDataAdapter(sql, localConn);
                adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime)
                   .Value = toDatePlus;
                var dt = new DataTable();
                adp.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    stagingDt.Rows.Add(r[0],
                        double.TryParse(r[1].ToString(), out double v) ? v : 0.0,
                        type);
                }
            }

            // بضاعة أول مدة (proc_type=3) → داخل
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=1 AND inv.proc_type=3 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 1);

            // مشتريات (proc_type=1) → داخل
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=1 AND inv.proc_type=1 " +
                    $"AND inv_sub.proc_type=1 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 1);

            // مرتجع مشتريات (proc_type=2) → خارج
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=1 AND inv.proc_type=2 " +
                    $"AND inv_sub.proc_type=1 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 2);

            // مبيعات (proc_type=1, inv_sub.proc_type=2) → خارج
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=2 AND inv.proc_type=1 " +
                    $"AND inv_sub.proc_type=2 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 2);

            // مرتجع مبيعات (proc_type=2, inv_sub.proc_type=2) → داخل
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=2 AND inv.proc_type=2 " +
                    $"AND inv_sub.proc_type=2 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 1);

            // نقطة بيع (proc_type=1, inv_sub.proc_type=3) → خارج
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=3 AND inv.proc_type=1 " +
                    $"AND inv_sub.proc_type=3 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 2);

            // مرتجع نقطة بيع (proc_type=2, inv_sub.proc_type=3) → داخل
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv.inv_type=3 AND inv.proc_type=2 " +
                    $"AND inv_sub.proc_type=3 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND IS_Deleted=0 GROUP BY ItemId", 1);

            // فاتورة إدخال (proc_type=1) → داخل
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub, Items, ItemsCategory " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv_sub.ItemId=Items.id " +
                    $"AND Items.group_id=ItemsCategory.id " +
                    $"AND inv.inv_type=4 AND inv.proc_type=1 " +
                    $"AND inv_sub.proc_type=1 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND inv.IS_Deleted=0 GROUP BY ItemId", 1);

            // فاتورة إخراج (proc_type=1) → خارج
            AddRows($"SELECT ItemId, SUM(val) FROM inv, inv_sub, Items, ItemsCategory " +
                    $"WHERE {condBranch}{condStore}" +
                    $"inv_sub.ItemId=Items.id " +
                    $"AND Items.group_id=ItemsCategory.id " +
                    $"AND inv.inv_type=4 AND inv.proc_type=1 " +
                    $"AND inv_sub.proc_type=2 " +
                    $"AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $"AND date<=@date2 AND inv.IS_Deleted=0 GROUP BY ItemId", 2);

            // دمج الصفوف حسب ItemId
            var resultDt = new DataTable();
            resultDt.Columns.Add("ItemId");
            resultDt.Columns.Add("qty");

            var groups = stagingDt.AsEnumerable()
                .GroupBy(r => r["ItemId"].ToString())
                .Select(g => new
                {
                    ItemId = g.Key,
                    Qty    = g.Sum(r => Convert.ToInt32(r["type"]) == 1
                                        ? Convert.ToDouble(r["qty"])
                                        : -Convert.ToDouble(r["qty"]))
                });

            foreach (var g in groups)
                resultDt.Rows.Add(g.ItemId, g.Qty);

            return resultDt;
        }

        private string GetItemName(int itemId)
        {
            try
            {
                using var c = MainClass.ConnObj();
                c.Open();
                bool isAr = string.Equals(MainClass.Language, "ar",
                                           StringComparison.OrdinalIgnoreCase);
                var adapter = new SqlDataAdapter(
                    $"SELECT name, nameEN FROM Items WHERE id={itemId}", c);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return isAr
                        ? dt.Rows[0]["name"]?.ToString() ?? ""
                        : dt.Rows[0]["nameEN"]?.ToString() ?? "";
            }
            catch { /* تجاهل */ }
            return "";
        }
        #endregion

        #region CheckBox Events
        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbSafes.IsEnabled = chkAll.IsChecked != true;
            if (chkAll.IsChecked == true)
                cmbSafes.SelectedIndex = -1;
        }
        #endregion

        #region Button Events
        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }

            StockList.Clear();
            CalcStock();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // تصدير CSV (بديل آمن عن Interop.Excel)
            if (StockList == null || StockList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter   = "CSV Files (*.csv)|*.csv",
                    FileName = $"رصيد_مخزن_{DateTime.Now:yyyyMMdd_HHmm}"
                };

                if (dlg.ShowDialog() != true) return;

                using var writer = new System.IO.StreamWriter(
                    dlg.FileName, false, System.Text.Encoding.UTF8);

                writer.WriteLine(
                    "رقم الصنف,الصنف,الكمية,متوسط سعر التكلفة,إجمالي التكلفة");

                foreach (var item in StockList)
                {
                    writer.WriteLine(
                        $"{item.ItemId},{item.ItemName}," +
                        $"{item.Qty:N2},{item.AvgCost:N4},{item.TotCost:N2}");
                }

                DXMessageBox.Show($"تم الحفظ في:\n{dlg.FileName}", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(dlg.FileName)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Print
        private void PrintReport(int printMode)
        {
            try
            {
                if (StockList == null || StockList.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للطباعة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                RptUrl     = MainClass.ReportsPath;
                RptName    = "rptSafeGrdToDate.repx";
                defPrinter = MainClass.ReportsPrinter;

                string full = System.IO.Path.Combine(RptUrl, RptName);

                if (string.IsNullOrEmpty(RptUrl) ||
                    !Directory.Exists(RptUrl)    ||
                    !File.Exists(full))
                {
                    DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var report = XtraReport.FromFile(full);
                report.DataSource = BuildReportDataSet();

                string hPath = System.IO.Path.Combine(RptUrl, "header.repx");
                if (File.Exists(hPath))
                {
                    var hRpt = XtraReport.FromFile(hPath);
                    hRpt.DataSource = Common.FoundationInfoDT;
                    var hSub = (XRSubreport)report.FindControl(
                                   "headerRpt", ignoreCase: true);
                    if (hSub != null) hSub.ReportSource = hRpt;
                }

                string fPath = System.IO.Path.Combine(RptUrl, "footer.repx");
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
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataSet BuildReportDataSet()
        {
            var list = StockList.Select(item => new InventoryData
            {
                SafeName     = cmbSafes.Text,
                SafeCode     = cmbSafes.SelectedValue?.ToString() ?? "",
                ItemCode     = item.ItemId.ToString(),
                ItemName     = item.ItemName,
                Quantity     = item.Qty.ToString("N2"),
                AvgCostPrice = item.AvgCost.ToString("N4"),
                TotCost      = item.TotCost.ToString("N2"),
                ToDate       = txtDate.DateTime.ToShortDateString(),
                Status       = " ",
                InventoryType = Title,
                SumCost      = txtSum.Text,
                User         = Common.GetEmpName(MainClass.EmpNo),
                PrintDate    = DateTime.Now.ToShortDateString(),
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }
        #endregion
    }
}