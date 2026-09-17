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
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptItemsSalesDetailsPOS : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int    SelectedId;
        private bool   PrintHeader;
        private bool   PrintFooter;
        private bool   PrintStamp;
        private int    PrintType;
        private int    PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        /// <summary>1=مبيعات POS، 2=مشتريات POS</summary>
        public int OperType;

        private ObservableCollection<ItemSalesPosRow> _gridData
            = new ObservableCollection<ItemSalesPosRow>();

        #endregion

        #region Constructor

        public frmRptItemsSalesDetailsPOS()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            conn1       = MainClass.ConnObj();
            SelectedId  = -1;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
            OperType    = 1;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;

            GridControl1.ItemsSource = _gridData;
            LoadSafes();
            LoadGroups();
            LoadEmps();
            ApplyOperTypeSettings();
        }

        private void ApplyOperTypeSettings()
        {
            if (OperType == 2)
            {
                colDgvNetSales.Header    = "صافي الشراء";
                lblNetSalesTitle.Text    = "💵 إجمالي صافي الشراء";
                lblGridTitle.Text        = "🛒 مشتريات الأصناف - نقطة البيع";
                this.Title               = "مشتريات الأصناف تجميعي - نقطة البيع";
            }
            else
            {
                colDgvNetSales.Header    = "صافي البيع";
                lblNetSalesTitle.Text    = "💵 إجمالي صافي البيع";
                lblGridTitle.Text        = "🛒 مبيعات الأصناف - نقطة البيع";
                this.Title               = "مبيعات الأصناف تجميعي - نقطة البيع";
            }
        }

        #endregion

        #region Load Lookups

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Safes " +
                    $"WHERE branch={MainClass.BranchNo} AND status=1 AND IS_Deleted=0 ORDER BY id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                // cmbSafes مخفية في هذه النسخة، لكن نحتفظ بالدالة للتوافق
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات:\n" + ex.Message);
            }
        }

        public void LoadGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM ItemsCategory WHERE type=2 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                // cmbGrp مخفية في هذه النسخة، لكن نحتفظ بالدالة للتوافق
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات:\n" + ex.Message);
            }
        }

        private void LoadEmps()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbusers.DisplayMemberPath = "name";
                cmbusers.SelectedValuePath = "id";
                cmbusers.ItemsSource       = dt.DefaultView;
                cmbusers.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الموظفين:\n" + ex.Message);
            }
        }

        #endregion

        #region ShowResults - الاستعلام الموحد لنقطة البيع

        private void ShowResults()
        {
            try
            {
                _gridData.Clear();

                DateTime dateTimeFrom = BuildDateTime(
                    txtDateFrom.DateTime, txtStartTime.Text, "00:00");
                DateTime dateTimeTo   = BuildDateTime(
                    txtDateTo.DateTime,   txtEndTime.Text,   "23:59");

                // ─── بناء شروط الفلتر ───
                string whereClause = "";

                // فلتر الوقت/التاريخ
                if (chkAllPeriod.IsChecked != true)
                    whereClause += " AND Inv.date BETWEEN @date1 AND @date2";

                // فلتر المستخدم
                if (ckAllUsers.IsChecked != true && cmbusers.SelectedValue != null)
                    whereClause += $" AND inv.sales_emp={cmbusers.SelectedValue}";

                // ─── SQL موحد لنقطة البيع (inv_type=3) ───
                string sql =
                    @"SELECT Items.id, Items.name, Items.code,
                             ItemsCategory.name AS CatName,
                             SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=1 THEN val ELSE 0 END) AS PosVal,
                             SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=2 THEN val ELSE 0 END) AS PosRetVal,
                             SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=1 THEN val1*exchange_price ELSE 0 END) AS SumPos,
                             SUM(CASE WHEN inv.inv_type=3 AND inv.proc_type=2 THEN val1*exchange_price ELSE 0 END) AS SumPosRet,
                             ISNULL(Items.additional_tax, 0) AS AdditionalTax
                      FROM Items
                      INNER JOIN Inv_Sub ON Items.Id = Inv_Sub.ItemId
                      INNER JOIN ItemsCategory ON Items.group_id = ItemsCategory.id
                      INNER JOIN inv ON inv.InvGlobalID = inv_sub.InvGlobalID
                      WHERE Items.IS_Deleted=0
                        AND inv_sub.ProductId=0
                        AND inv.inv_type=3";

                if (!string.IsNullOrEmpty(whereClause))
                    sql += whereClause;

                sql += " GROUP BY Items.id, Items.name, Items.code, ItemsCategory.name, Items.additional_tax";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value = dateTimeFrom;
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dateTimeTo;

                var dt = new DataTable();
                adapter.Fill(dt);

                ProgressBar1.Maximum = Math.Max(dt.Rows.Count, 1);
                ProgressBar1.Value   = 0;

                int rowNum = 1;
                foreach (DataRow row in dt.Rows)
                {
                    double posVal    = ParseDouble(row["PosVal"]);
                    double posRetVal = ParseDouble(row["PosRetVal"]);
                    double sumPos    = ParseDouble(row["SumPos"]);
                    double sumPosRet = ParseDouble(row["SumPosRet"]);
                    double addTax    = ParseDouble(row["AdditionalTax"]);

                    bool hasMovement = (posVal != 0 || posRetVal != 0);
                    if (!hasMovement) continue;

                    // فلتر الضريبة الإضافية
                    if (chkOnlyItemAdditionalTax.IsChecked == true && addTax == 0.0)
                        continue;

                    double netQty   = posVal - posRetVal;
                    double netValue = Math.Round(sumPos - sumPosRet, 2);

                    _gridData.Add(new ItemSalesPosRow
                    {
                        DgvNo              = rowNum++,
                        DgvItemNo          = Convert.ToInt32(row["id"]).ToString(),
                        DgvItemCode        = row["code"].ToString(),
                        DgvItem            = row["name"].ToString(),
                        DgvQty             = netQty,
                        DgvNetSales        = netValue,
                        DgvSaleRatio       = 0,
                        DgvCategory        = row["CatName"].ToString(),
                        DgvItemAdditionalTax = addTax,
                    });

                    ProgressBar1.Value = rowNum;
                }

                UpdateSummary();
                ProgressBar1.Value = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض النتائج:\n" + ex.Message);
            }
        }

        private DateTime BuildDateTime(DateTime date, string timeText, string defaultTime)
        {
            try
            {
                string time = string.IsNullOrWhiteSpace(timeText)
                    ? defaultTime : timeText.Trim();
                return DateTime.Parse($"{date.ToShortDateString()} {time}");
            }
            catch { return date; }
        }

        private double ParseDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0.0;
            return double.TryParse(val.ToString(), out double d) ? d : 0.0;
        }

        private void UpdateSummary()
        {
            lblCountItems.Text  = _gridData.Count.ToString();
            lblSumNetSales.Text = _gridData.Sum(r => r.DgvNetSales).ToString("N2");
            lblSumQty.Text      = _gridData.Sum(r => r.DgvQty).ToString("N2");
        }

        #endregion

        #region Print / Preview / Export

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            string address = "", telephone = "", mobile = "", foundation = "",
                   field = "", vATNo = "";

            if (Common.FoundationInfoDT?.Rows.Count > 0)
            {
                var r = Common.FoundationInfoDT.Rows[0];
                address    = r["Address"].ToString();
                telephone  = r["Tel"].ToString();
                mobile     = r["Mobile"].ToString();
                foundation = r["nameA"].ToString();
                field      = r["FieldA"].ToString();
                vATNo      = r["tax_no"].ToString();
            }

            foreach (var row in _gridData)
            {
                list.Add(new InventoryData
                {
                    ItemCode      = row.DgvItemCode,
                    ItemName      = row.DgvItem,
                    Quantity      = row.DgvQty.ToString("N2"),
                    Total         = row.DgvNetSales.ToString("N2"),
                    Ratio         = row.DgvSaleRatio.ToString("N2"),
                    CategoryName  = row.DgvCategory,
                    FromDate      = chkAllPeriod.IsChecked != true
                                    ? txtDateFrom.DateTime.ToShortDateString() : "",
                    ToDate        = chkAllPeriod.IsChecked != true
                                    ? txtDateTo.DateTime.ToShortDateString() : "",
                    InventoryType = this.Title,
                    Sum           = lblSumNetSales.Text,
                    User          = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate     = DateTime.Now.ToShortDateString(),
                    Address       = address,
                    Mobile        = mobile,
                    Telephone     = telephone,
                    VATNo         = vATNo,
                    Foundation    = foundation,
                    Field         = field,
                    Logo          = "",
                    Header        = "",
                    Footer        = "",
                    Stamp         = "",
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printMode)
        {
            RptUrl     = MainClass.ReportsPath;
            defPrinter = MainClass.ReportsPrinter;
            RptName    = OperType == 2
                         ? "RptItemsPurchDetailsPos.repx"
                         : "RptItemsSalesDetailsPos.repx";

            if (_gridData.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;
                if (printMode == 1)
                    for (int c = 1; c <= PrintNo; c++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة:\n" + ex.Message);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                if (_gridData.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"POSSales_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("\"م\",\"رمز الصنف\",\"الصنف\",\"الكمية\",\"صافي البيع\",\"الفئة\"");
                    foreach (var r in _gridData)
                        sw.WriteLine(
                            $"\"{r.DgvNo}\"," +
                            $"\"{r.DgvItemCode}\"," +
                            $"\"{r.DgvItem}\"," +
                            $"\"{r.DgvQty:N2}\"," +
                            $"\"{r.DgvNetSales:N2}\"," +
                            $"\"{r.DgvCategory}\"");
                }

                Process.Start(new ProcessStartInfo(filePath)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير:\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
            => ShowResults();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnExport_Click(object sender, RoutedEventArgs e)
            => ExportToExcel();

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null &&
                int.TryParse(btn.Tag.ToString(), out int itemId))
            {
                var dlg = new frmItemsDetails();
                MainClass.ApplyPermissionToForm(dlg);
                MainClass.DoApplyUserSett(dlg);
                dlg.SelectedId = itemId;
                dlg.ShowDialog();
            }
        }

        #endregion

        #region CheckBox Events

        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckAllUsers.IsChecked == true;
            cmbusers.IsEnabled = !isAll;
            if (isAll) cmbusers.SelectedIndex = -1;
        }

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAllPeriod.IsChecked == true;
            txtDateFrom.IsEnabled  = !isAll;
            txtDateTo.IsEnabled    = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled   = !isAll;
        }

        private void chkOnlyItemAdditionalTax_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            // إعادة تطبيق الفلتر عند تغيير الخيار
            if (_gridData.Count > 0) ShowResults();
        }

        #endregion
    }
}