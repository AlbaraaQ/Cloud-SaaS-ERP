using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvDetails : ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────────────

        private readonly SqlConnection _conn;

        private int _selectedItemId = -1;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType = 0;
        private int _printNo = 1;
        private string _defPrinter = string.Empty;
        private string _rptName = string.Empty;
        private string _rptUrl = string.Empty;

        // إجماليات مخصصة (تراعي الإشارة + / -)
        private double _sumQty = 0.0;
        private double _sumPrimaryQnty = 0.0;
        private double _sumVAT = 0.0;
        private double _sumItemSum = 0.0;
        private double _sumItemDiscount = 0.0;
        private double _sumBeforeTax = 0.0;
        private double _sumAddTax = 0.0;
        private double _sumTotal = 0.0;

        private ObservableCollection<InvDetailRow> _rowsList;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmInvDetails()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            _rowsList = new ObservableCollection<InvDetailRow>();
        }

        #endregion

        #region ── Window Loaded ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;
            cmbProcType.SelectedIndex = 0;

            LoadSafes();
            LoadPrintSettings();
            LoadBranches();

            GridControl1.ItemsSource = _rowsList;
        }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Safes WHERE branch={MainClass.BranchNo} AND status=1 AND IS_Deleted=0 ORDER BY id",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbSafes.DisplayMemberPath = "name";
                cmbSafes.SelectedValuePath = "id";
                cmbSafes.ItemsSource = table.DefaultView;
                cmbSafes.SelectedIndex = -1;
            }
            catch (Exception ex) { ShowError("خطأ في تحميل المستودعات", ex); }
        }

        public void LoadGroup()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM ItemsCategory WHERE IS_Deleted=0 ORDER BY id",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                CmbGroup.DisplayMemberPath = "name";
                CmbGroup.SelectedValuePath = "id";
                CmbGroup.ItemsSource = table.DefaultView;
                CmbGroup.SelectedIndex = -1;
            }
            catch (Exception ex) { ShowError("خطأ في تحميل المجموعات", ex); }
        }

        private void LoadBranches()
        {
            try
            {
                string branchFilter = string.Empty;
                if (MainClass.BranchNo != -1)
                {
                    bool hasCond = !string.IsNullOrWhiteSpace(Accounting.BranchCondition)
                                   && Accounting.BranchCondition.Trim() != " ";
                    branchFilter = hasCond
                        ? $" AND BranchId={MainClass.BranchNo}"
                        : string.Empty;
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT BranchId, name FROM Branches WHERE IS_Deleted=0{branchFilter}",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "BranchId";
                cmbBranches.ItemsSource = table.DefaultView;

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex) { ShowError("خطأ في تحميل الفروع", ex); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count != 1) return;

                _printType = SafeInt(table.Rows[0]["printType"]);
                _printFooter = Convert.ToBoolean(table.Rows[0]["PrintFooter"]);
                _printHeader = Convert.ToBoolean(table.Rows[0]["PrintHeader"]);
                _printStamp = Convert.ToBoolean(table.Rows[0]["PrintStamp"]);
                _defPrinter = table.Rows[0]["CasherPrinter"]?.ToString() ?? string.Empty;
                _printNo = SafeInt(table.Rows[0]["printNo"], 1);

                if (string.IsNullOrEmpty(_defPrinter))
                    _defPrinter = Common.GetDefaultPrinter();
            }
            catch (Exception ex) { ShowError("خطأ في إعدادات الطباعة", ex); }
        }

        #endregion

        #region ── Show Results ────────────────────────────────────────────

        private void btnShow_Click(object sender, RoutedEventArgs e)
            => ShowResult();

        private void ShowResult()
        {
            try
            {
                _rowsList.Clear();
                ResetSummaries();

                // ── بناء شروط الفلتر ──
                string filter = string.Empty;

                if (chkAllSafes.IsChecked != true)
                {
                    if (cmbSafes.SelectedValue == null)
                    {
                        DXMessageBox.Show("اختر مخزنًا",
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    filter += $" AND Inv.safe={cmbSafes.SelectedValue}";
                }

                if (ChkAllGroup.IsChecked != true)
                {
                    if (CmbGroup.SelectedValue == null)
                    {
                        DXMessageBox.Show("اختر المجموعة",
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    filter += $" AND Inv_Sub.ItemId IN (SELECT id FROM items WHERE group_id={CmbGroup.SelectedValue})";
                }

                if (ckTotalPeriod.IsChecked != true)
                    filter += " AND date>=@date1 AND date<=@date2 ";

                if (chkAll.IsChecked != true)
                {
                    if (string.IsNullOrWhiteSpace(txtItemName.Text))
                    {
                        DXMessageBox.Show("اختر مادة",
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    filter += $" AND Inv_Sub.ItemId={_selectedItemId}";
                }

                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedIndex >= 0)
                    filter += $" AND inv.branch={cmbBranches.SelectedValue}";

                filter += " AND inv_sub.ProductId=0 ";

                // نوع الفاتورة (1=مشتريات، 2=مبيعات، 3=POS)
                int invType = cmbProcType.SelectedIndex switch
                {
                    1 => 2,
                    2 => 3,
                    _ => 1
                };

                string sql = $@"
                    SELECT Inv.safe, Inv.proc_type, Inv.inv_type,
                           date, Inv.id, ItemId,
                           ISNULL(val,0)  AS val,
                           ISNULL(val1,0) AS val1,
                           ISNULL(exchange_price,0) AS exchange_price,
                           ISNULL(discount,0) AS discount,
                           ((val1*exchange_price) - discount) AS sum,
                           sales_emp, cust_id, unit, taxval,
                           ISNULL(ItemAdditionalTax,0)      AS ItemAdditionalTax,
                           ISNULL(ItemPriceWithoutVAT,0)    AS ItemPriceWithoutVAT,
                           Inv.InvGlobalID
                    FROM   Inv, Inv_Sub
                    WHERE  Inv.InvGlobalID = Inv_Sub.InvGlobalID
                      AND  Inv.inv_type    = {invType}
                      AND  Inv_Sub.ItemId  > 0
                      AND  Inv.IS_Deleted  = 0
                           {filter}
                    ORDER BY date DESC";

                var adapter = new SqlDataAdapter(sql, _conn);

                // تواريخ بالوقت
                DateTime fromDt = DateTime.Parse(
                    $"{txtDateFrom.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString()} " +
                    $"{GetTimeValue(txtStartTime)}");

                DateTime toDt = DateTime.Parse(
                    $"{txtDateTo.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString()} " +
                    $"{GetTimeValue(txtEndTime)}");

                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDt;
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDt;

                var rawTable = new DataTable();
                adapter.Fill(rawTable);

                ProgressBar1.Maximum = rawTable.Rows.Count;
                ProgressBar1.Value = 0;

                int rowIndex = 1;

                foreach (DataRow row in rawTable.Rows)
                {
                    var invObj = new InvoiceObj(SafeInt(row["inv_type"]),
                                               SafeInt(row["proc_type"]));

                    // تحديد نوع الحركة والإشارة
                    string processType = DetermineProcessType(
                        SafeDouble(row["proc_type"]), invType,
                        out bool isPositive);

                    if (string.IsNullOrEmpty(processType))
                    {
                        ProgressBar1.Value++;
                        continue;
                    }

                    // الحسابات
                    double netVal = SafeDouble(row["sum"]);
                    double taxVal = SafeDouble(row["taxval"]);
                    double addTax = SafeDouble(row["ItemAdditionalTax"]);
                    double priceNoVat = SafeDouble(row["ItemPriceWithoutVAT"]);
                    int itemId = SafeInt(row["ItemId"]);

                    double totalBeforeTax = invObj.PriceIncVAT
                        ? netVal - taxVal
                        : netVal;

                    // حساب الضريبة الإضافية
                    if (addTax == 0.0 && CheckItemAdditionalTax(itemId))
                    {
                        addTax = priceNoVat < 25.0
                            ? 25.0 * SafeDouble(row["val1"])
                            : totalBeforeTax * (invObj.AdditionalTax / 100.0);
                    }

                    double total = invObj.PriceIncVAT
                        ? netVal + addTax
                        : netVal + taxVal + addTax;

                    // بناء الصف
                    int isPlus = isPositive ? 0 : 1;

                    var detailRow = new InvDetailRow
                    {
                        DgvNo = rowIndex++,
                        DgvStore = GetSafeName(SafeInt(row["safe"])),
                        DgvInvType = processType,
                        DgvClient = GetCustName(SafeInt(row["cust_id"])),
                        DgvDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        DgvInvNo = row["id"]?.ToString() ?? string.Empty,
                        DgvItem = GetCurrencyName(itemId),
                        DgvUnit = Common.GetUnitName(SafeInt(row["unit"])),
                        DgvQty = Math.Round(SafeDouble(row["val1"]), 2),
                        DgvPrimaryQnty = SafeDouble(row["val"]),
                        DgvPrice = SafeDouble(row["exchange_price"]),
                        DgvItemSum = SafeDouble(row["sum"]),
                        DgvItemDiscount = SafeDouble(row["discount"]),
                        DgvTotalBeforeTax = Math.Round(totalBeforeTax, 2),
                        DgvVAT = Math.Round(taxVal, 2),
                        DgvItemAdditionalTax = Math.Round(addTax, 2),
                        DgvTotal = Math.Round(total, 2),
                        DgvUser = GetEmpName(SafeInt(row["sales_emp"])),
                        DgvInvGlobalID = row["InvGlobalID"]?.ToString() ?? string.Empty,
                        DgvIsPlus = isPlus
                    };

                    // تحديث الإجماليات
                    UpdateSummaries(detailRow);

                    // تطبيق فلتر الضريبة الإضافية
                    if (chkOnlyItemAdditionalTax.IsChecked == true &&
                        detailRow.DgvItemAdditionalTax == 0.0)
                    {
                        ProgressBar1.Value++;
                        continue;
                    }

                    _rowsList.Add(detailRow);
                    ProgressBar1.Value++;
                }

                GridControl1.ItemsSource = null;
                GridControl1.ItemsSource = _rowsList;

                RefreshSummaryLabels();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في عرض النتائج", ex);
            }
        }

        private string DetermineProcessType(double procType, int invType,
            out bool isPositive)
        {
            isPositive = true;

            if (procType == 1.0 && invType == 1)
            { isPositive = true; return "فاتورة شراء"; }
            if (procType == 1.0 && invType == 2)
            { isPositive = true; return "فاتورة بيع"; }
            if (procType == 1.0 && invType == 3)
            { isPositive = true; return "نقطة بيع"; }
            if (procType == 2.0 && invType == 1)
            { isPositive = false; return "فاتورة مرتجع شراء"; }
            if (procType == 2.0 && invType == 2)
            { isPositive = false; return "فاتورة مرتجع بيع"; }
            if (procType == 2.0 && invType == 3)
            { isPositive = false; return "مرتجع نقطة البيع"; }

            return string.Empty;
        }

        #endregion

        #region ── Summary Calculation ─────────────────────────────────────

        private void ResetSummaries()
        {
            _sumQty = _sumPrimaryQnty = _sumVAT = 0.0;
            _sumItemSum = _sumItemDiscount = 0.0;
            _sumBeforeTax = _sumAddTax = _sumTotal = 0.0;
        }

        private void UpdateSummaries(InvDetailRow row)
        {
            int sign = (row.DgvIsPlus == 0) ? 1 : -1;

            _sumQty += sign * row.DgvQty;
            _sumPrimaryQnty += sign * row.DgvPrimaryQnty;
            _sumItemSum += sign * row.DgvItemSum;
            _sumItemDiscount += sign * row.DgvItemDiscount;
            _sumBeforeTax += sign * row.DgvTotalBeforeTax;
            _sumVAT += sign * row.DgvVAT;
            _sumAddTax += sign * row.DgvItemAdditionalTax;
            _sumTotal += sign * row.DgvTotal;
        }

        private void RefreshSummaryLabels()
        {
            lblSumQty.Text = _sumQty.ToString("N2");
            lblSumItemSum.Text = _sumItemSum.ToString("N2");
            lblSumDiscount.Text = _sumItemDiscount.ToString("N2");
            lblSumBeforeTax.Text = _sumBeforeTax.ToString("N2");
            lblSumVAT.Text = _sumVAT.ToString("N2");
            lblSumAddTax.Text = _sumAddTax.ToString("N2");
            lblSumTotal.Text = _sumTotal.ToString("N2");
        }

        #endregion

        #region ── Grid Button Event ────────────────────────────────────────

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not InvDetailRow row) return;

            try
            {
                switch (row.DgvInvType)
                {
                    case "فاتورة شراء":
                        {
                            var frm = new frmInvPurch { InvType = 1, ProcType = 1 };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=1 AND proc_type=1 AND id={row.DgvInvNo}");
                            break;
                        }
                    case "فاتورة بيع":
                        {
                            var frm = new frmInvSale { InvType = 2, ProcType = 1 };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=2 AND proc_type=1 AND id={row.DgvInvNo}");
                            break;
                        }
                    case "نقطة بيع":
                        {
                            var frm = new frmInvPOS { InvType = 3, ProcType = 1 };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=3 AND proc_type=1 AND id={row.DgvInvNo}");
                            break;
                        }
                    case "فاتورة مرتجع شراء":
                        {
                            var frm = new frmInvPurch
                            { InvType = 1, ProcType = 2, Title = "مرتجع مشتريات" };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=1 AND proc_type=2 AND id={row.DgvInvNo}");
                            break;
                        }
                    case "فاتورة مرتجع بيع":
                        {
                            var frm = new frmInvSale
                            { InvType = 2, ProcType = 2, Title = "مرتجع مبيعات" };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=2 AND proc_type=2 AND id={row.DgvInvNo}");
                            break;
                        }
                    case "مرتجع نقطة البيع":
                        {
                            var frm = new frmInvPOS
                            { InvType = 3, ProcType = 2, Title = "مرتجع" };
                            frm.Show();
                            frm.WindowState = WindowState.Maximized;
                            frm.Navigate(
                                $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type=3 AND proc_type=2 AND id={row.DgvInvNo}");
                            break;
                        }
                }
            }
            catch (Exception ex) { ShowError("خطأ في فتح الفاتورة", ex); }
        }

        #endregion

        #region ── Item Search ──────────────────────────────────────────────

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllSafes.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            chkAll.IsChecked = false;
            AddNewItem();
        }

        private void AddNewItem()
        {
            var searchForm = new frmItemsSrch
            {
                sql = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id",
                search = "SELECT id, name, nameEN, sale_price, unit FROM Items ",
                Itemname = string.Empty
            };
            searchForm.txtSrchNm.Text = txtItemName.Text;

            MainClass.ApplyPermissionToForm(searchForm);
            MainClass.DoApplyUserSett(searchForm);
            searchForm.ShowDialog();

            if (searchForm.ISDone && searchForm.ItemId > 0)
            {
                _selectedItemId = searchForm.ItemId;
                txtItemCode.Text = Common.GetItemCode(_selectedItemId);
                txtItemName.Text = searchForm.Itemname;
            }
        }

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items WHERE IS_Deleted=0 AND name=N'{txtItemName.Text}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    _selectedItemId = SafeInt(table.Rows[0]["id"]);
                    txtItemCode.Text = table.Rows[0]["Code"]?.ToString() ?? string.Empty;
                    txtItemName.Text = table.Rows[0]["name"]?.ToString() ?? string.Empty;
                }
                else
                    AddNewItem();
            }
            catch (Exception ex) { ShowError("خطأ في البحث بالاسم", ex); }
        }

        private void SearchByCode()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items WHERE IS_Deleted=0 AND Code=N'{txtItemCode.Text}'",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    _selectedItemId = SafeInt(table.Rows[0]["id"]);
                    txtItemCode.Text = table.Rows[0]["Code"]?.ToString() ?? string.Empty;
                    txtItemName.Text = table.Rows[0]["name"]?.ToString() ?? string.Empty;
                }
                else
                    AddNewItem();
            }
            catch (Exception ex) { ShowError("خطأ في البحث بالكود", ex); }
        }

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemName.Text))
                SearchByName();
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemCode.Text))
                SearchByCode();
        }

        #endregion

        #region ── CheckBox Events ──────────────────────────────────────────

        private void chkAllSafes_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSafes.IsEnabled = chkAllSafes.IsChecked != true;
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAll.IsChecked == true;
            txtItemName.IsEnabled = !isAll;
            txtItemCode.IsEnabled = !isAll;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isAll;
            txtDateTo.IsEnabled = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled = !isAll;
        }

        private void ChkAllGroup_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            CmbGroup.IsEnabled = ChkAllGroup.IsChecked != true;
        }

        private void chkOnlyItemAdditionalTax_Changed(object sender, RoutedEventArgs e)
        {
            // إعادة عرض النتائج مع تطبيق الفلتر الجديد
            if (_rowsList.Count > 0) ShowResult();
        }

        private void CmbGroup_DropDownOpened(object sender, EventArgs e)
            => LoadGroup();

        #endregion

        #region ── Print & Export ───────────────────────────────────────────

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private void PrintReport(int type)
        {
            _rptUrl = MainClass.ReportsPath;
            _rptName = "rptInvDetails.repx";
            _defPrinter = MainClass.ReportsPrinter;

            if (_rowsList.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string reportPath = Path.Combine(_rptUrl, _rptName);
            if (!File.Exists(reportPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(reportPath);
                report.DataSource = BuildReportDataSet();

                AttachSubReport(report, "headerRpt",
                    Path.Combine(_rptUrl, "header.repx"), Common.FoundationInfoDT);
                AttachSubReport(report, "footerRpt",
                    Path.Combine(_rptUrl, "footer.repx"), Common.FoundationInfoDT);

                if (!string.IsNullOrEmpty(_defPrinter))
                {
                    report.PrinterName = _defPrinter;
                    if (type == 1)
                        for (int i = 0; i < _printNo; i++) report.Print();
                    else
                        report.ShowPreviewDialog();
                }
                else
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات");

                report.Dispose();
            }
            catch (Exception ex) { ShowError("خطأ في الطباعة", ex); }
        }

        private void AttachSubReport(XtraReport parent, string controlName,
            string repxPath, object dataSource)
        {
            if (!File.Exists(repxPath)) return;

            var subReport = XtraReport.FromFile(repxPath);
            subReport.DataSource = dataSource;

            var subControl = parent.FindControl(controlName, true) as XRSubreport;
            if (subControl != null) subControl.ReportSource = subReport;
        }

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _rowsList)
            {
                list.Add(new InventoryData
                {
                    ProcessType = cmbProcType.Text,
                    SafeMain = (cmbSafes.SelectedItem as DataRowView)?["name"]?.ToString() ?? string.Empty,
                    MainItemName = txtItemName.Text,
                    FromDate = txtDateFrom.SelectedDate?.ToShortDateString() ?? string.Empty,
                    ToDate = txtDateTo.SelectedDate?.ToShortDateString() ?? string.Empty,
                    SafeName = row.DgvStore,
                    Process = row.DgvInvType,
                    Supplier = row.DgvClient,
                    InvDate = row.DgvDate,
                    InvoiceNo = row.DgvInvNo,
                    ItemName = row.DgvItem,
                    Quantity = row.DgvQty.ToString("N2"),
                    Price = row.DgvPrice.ToString("N2"),
                    Total = row.DgvTotal.ToString("N2"),
                    ItemSum = row.DgvItemSum.ToString("N2"),
                    ItemDiscount = row.DgvItemDiscount.ToString("N2"),
                    NetBeforeTax = row.DgvTotalBeforeTax.ToString("N2"),
                    Tax = row.DgvVAT.ToString("N2"),
                    EmpName = row.DgvUser,
                    Unit = row.DgvUnit,
                    Quantity1 = row.DgvPrimaryQnty.ToString("N2"),
                    ItemAdditionalTax = row.DgvItemAdditionalTax.ToString("N2"),
                    InventoryType = this.Title,
                    NetQuantity = _sumQty.ToString("N2"),
                    SumNetQuantity = _sumPrimaryQnty.ToString("N2"),
                    Sum = _sumTotal.ToString("N2"),
                    SumTax = _sumVAT.ToString("N2"),
                    SummaryItemSum = _sumItemSum.ToString("N2"),
                    SummaryItemDiscount = _sumItemDiscount.ToString("N2"),
                    SummaryTotalBeforeTax = _sumBeforeTax.ToString("N2"),
                    SummaryItemAdditionalTax = _sumAddTax.ToString("N2"),
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_rowsList.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_تفصيلي_{DateTime.Now:yyyyMMdd}"
                };

                if (saveDialog.ShowDialog() != true) return;

                ExportToExcelClosedXml(_rowsList.ToList(), saveDialog.FileName);

                DXMessageBox.Show($"✅ تم حفظ الملف في:\n{saveDialog.FileName}",
                    "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(saveDialog.FileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex) { ShowError("خطأ في التصدير", ex); }
        }

        private void ExportToExcelClosedXml(List<InvDetailRow> items, string filePath)
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير تفصيلي");

            string[] headers =
            {
                "م","المستودع","نوع العملية","المورد/العميل","التاريخ",
                "الرقم","المادة","الوحدة","الكمية","كمية أساسية",
                "السعر","المجموع","الخصم","قبل الضريبة","الضريبة",
                "ضريبة إضافية","الإجمالي","المستخدم"
            };

            for (int c = 0; c < headers.Length; c++)
            {
                var cell = worksheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor =
                    ClosedXML.Excel.XLColor.FromArgb(33, 58, 122);
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal =
                    ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var row = items[i];
                int r = i + 2;

                worksheet.Cell(r, 1).Value = row.DgvNo;
                worksheet.Cell(r, 2).Value = row.DgvStore;
                worksheet.Cell(r, 3).Value = row.DgvInvType;
                worksheet.Cell(r, 4).Value = row.DgvClient;
                worksheet.Cell(r, 5).Value = row.DgvDate;
                worksheet.Cell(r, 6).Value = row.DgvInvNo;
                worksheet.Cell(r, 7).Value = row.DgvItem;
                worksheet.Cell(r, 8).Value = row.DgvUnit;
                worksheet.Cell(r, 9).Value = row.DgvQty;
                worksheet.Cell(r, 10).Value = row.DgvPrimaryQnty;
                worksheet.Cell(r, 11).Value = row.DgvPrice;
                worksheet.Cell(r, 12).Value = row.DgvItemSum;
                worksheet.Cell(r, 13).Value = row.DgvItemDiscount;
                worksheet.Cell(r, 14).Value = row.DgvTotalBeforeTax;
                worksheet.Cell(r, 15).Value = row.DgvVAT;
                worksheet.Cell(r, 16).Value = row.DgvItemAdditionalTax;
                worksheet.Cell(r, 17).Value = row.DgvTotal;
                worksheet.Cell(r, 18).Value = row.DgvUser;

                string nf = "#,##0.00";
                foreach (int col in new[] { 9, 10, 11, 12, 13, 14, 15, 16, 17 })
                    worksheet.Cell(r, col).Style.NumberFormat.Format = nf;

                if (i % 2 == 0)
                    worksheet.Row(r).Style.Fill.BackgroundColor =
                        ClosedXML.Excel.XLColor.FromArgb(248, 249, 253);
            }

            // صف الإجماليات
            int sumRow = items.Count + 2;
            worksheet.Cell(sumRow, 8).Value = "الإجمالي";
            worksheet.Cell(sumRow, 9).Value = _sumQty;
            worksheet.Cell(sumRow, 10).Value = _sumPrimaryQnty;
            worksheet.Cell(sumRow, 12).Value = _sumItemSum;
            worksheet.Cell(sumRow, 13).Value = _sumItemDiscount;
            worksheet.Cell(sumRow, 14).Value = _sumBeforeTax;
            worksheet.Cell(sumRow, 15).Value = _sumVAT;
            worksheet.Cell(sumRow, 16).Value = _sumAddTax;
            worksheet.Cell(sumRow, 17).Value = _sumTotal;

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        #endregion

        #region ── Database Helpers ─────────────────────────────────────────

        private string GetCurrencyName(int itemId)
        {
            try
            {
                bool isAr = MainClass.Language == "ar";
                string field = isAr ? "name" : "nameEN";
                var adapter = new SqlDataAdapter(
                    $"SELECT {field} FROM Items WHERE id={itemId}", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetEmpName(int empId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Employees WHERE id={empId}", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetSafeName(int safeId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Safes WHERE id={safeId}", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private string GetCustName(int custId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Customers WHERE id={custId}", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch { return string.Empty; }
        }

        private bool CheckItemAdditionalTax(int itemId)
        {
            try
            {
                using var conn = MainClass.ConnObj();
                conn.Open();
                using var cmd = new SqlCommand(
                    "SELECT ISNULL(is_extra_tax_applied,0) FROM items WHERE id=@id",
                    conn);
                cmd.Parameters.AddWithValue("@id", itemId);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    && Convert.ToBoolean(result);
            }
            catch { return false; }
        }

        #endregion

        #region ── Time Helpers ─────────────────────────────────────────────

        /// <summary>
        /// يقرأ قيمة الوقت من DevExpress TimeEdit.
        /// </summary>
        private string GetTimeValue(TextBox timeTextBox)
        {
            try
            {
                string timeText = timeTextBox.Text?.Trim() ?? "00:00";

                if (TimeSpan.TryParse(timeText, out TimeSpan parsedTime))
                    return parsedTime.ToString(@"hh\:mm");

                return "00:00";
            }
            catch
            {
                return "00:00";
            }
        }

        #endregion

        #region ── Safe Converters ──────────────────────────────────────────

        private static int SafeInt(object value, int defaultVal = 0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return int.TryParse(value.ToString(), out int r) ? r : defaultVal;
        }

        private static double SafeDouble(object value, double defaultVal = 0.0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return double.TryParse(value.ToString(), out double r) ? r : defaultVal;
        }

        private void ShowError(string title, Exception ex)
        {
            DXMessageBox.Show($"{title}\nتفاصيل الخطأ: {ex.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}