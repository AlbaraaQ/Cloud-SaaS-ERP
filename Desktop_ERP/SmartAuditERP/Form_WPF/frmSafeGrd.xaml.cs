using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSafeGrd : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int SelectedId;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        public bool ISDone;
        public double _stockTotal;
        private bool _Finished;

        private ObservableCollection<InventoryGridItem> _inventoryItems;

        #endregion

        #region Constructor

        public frmSafeGrd()
        {
            InitializeComponent();

            conn       = MainClass.ConnObj();
            SelectedId = -1;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
            ISDone      = false;
            _stockTotal = 0.0;
            _Finished   = false;

            _inventoryItems = new ObservableCollection<InventoryGridItem>();
            GridControl1.ItemsSource = _inventoryItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtLastDate.DateTime = DateTime.Now;
                LoadBranches();
                LoadSafes((short)MainClass.BranchNo);
                LoadGroups();
                LoadUnits();
                LoadPrintSettings();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

        public void LoadSafes(short branchId)
        {
            try
            {
                if (branchId == 0 || branchId == -1)
                    branchId = 1;

                EnsureOpen(conn);
                string sql = "SELECT id, name FROM Safes WHERE status=1 AND IS_Deleted=0 ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSafes.ItemsSource       = dt.DefaultView;
                cmbSafes.DisplayMemberPath = "name";
                cmbSafes.SelectedValuePath = "id";

                cmbSafes.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل المستودعات: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadGroups()
        {
            try
            {
                EnsureOpen(conn);
                string sql = "SELECT id, name FROM ItemsCategory ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbGroup.ItemsSource       = dt.DefaultView;
                cmbGroup.DisplayMemberPath = "name";
                cmbGroup.SelectedValuePath = "id";
                cmbGroup.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل المجموعات: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        public void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                string sql = "SELECT id, name FROM Branches ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.SelectedValue     = MainClass.BranchNo;
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الفروع: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadUnits()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT * FROM units", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbUnits.ItemsSource       = dt.DefaultView;
                    cmbUnits.DisplayMemberPath = "name";
                    cmbUnits.SelectedValuePath = "id";
                    cmbUnits.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في تحميل الوحدات: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pType))
                        PrintType = pType;

                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();

                    if (string.IsNullOrWhiteSpace(defPrinter))
                        defPrinter = MainClass.ReportsPrinter;

                    if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pNo))
                        PrintNo = pNo;
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في إعدادات الطباعة: " + ex.Message);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Stock Calculation

        public void CalcStock1()
        {
            try
            {
                _inventoryItems.Clear();

                if (!chkAll.IsChecked == true && cmbSafes.SelectedIndex == -1)
                {
                    DXMessageBox.Show("الرجاء اختيار المستودع", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تجهيز DataTable الجرد
                var resultTable = new DataTable();
                resultTable.Columns.Add("DgvNo",           typeof(int));
                resultTable.Columns.Add("DgvItemCode",     typeof(string));
                resultTable.Columns.Add("DgvItemName",     typeof(string));
                resultTable.Columns.Add("DgvBarcode",      typeof(string));
                resultTable.Columns.Add("DgvQty",          typeof(double));
                resultTable.Columns.Add("DgvAveCost",      typeof(double));
                resultTable.Columns.Add("DgvTotalCost",    typeof(double));
                resultTable.Columns.Add("DgvSalePrice",    typeof(double));
                resultTable.Columns.Add("DgvTotalSalePrice", typeof(double));
                resultTable.Columns.Add("DgvItemId",       typeof(int));

                // تحديد المستودع والفرع
                int safeId   = -1;
                int branchId = 0;

                if (cmbSafes.SelectedIndex > -1 && cmbSafes.SelectedValue != null)
                    int.TryParse(cmbSafes.SelectedValue.ToString(), out safeId);

                if (cmbBranches.SelectedIndex > -1 && cmbBranches.SelectedValue != null)
                    int.TryParse(cmbBranches.SelectedValue.ToString(), out branchId);

                // تحديد نص عمود سعر التكلفة
                if (rbAvgCost.IsChecked == true)
                    colAveCost.Header = "💲 متوسط التكلفة";
                else if (rbPurchPrice.IsChecked == true)
                    colAveCost.Header = "💲 سعر الشراء";
                else if (rbLastPurchPrice.IsChecked == true)
                    colAveCost.Header = "💲 آخر سعر شراء";

                // الحصول على بيانات الجرد
                DataTable stockData = GetStockData(safeId, branchId);

                // بناء شرط الوحدة
                string unitCondition = cmbUnits.SelectedIndex == -1
                    ? " AND ItemUnits.unit = Items.unit"
                    : $" AND ItemUnits.unit = {cmbUnits.SelectedValue}";

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = stockData.Rows.Count > 0 ? stockData.Rows.Count : 1;

                int rowIndex = 0;

                foreach (DataRow stockRow in stockData.Rows)
                {
                    ProgressBar1.Value++;

                    // تصفية المجموعة
                    if (cmbGroup.SelectedIndex != -1
                        && stockRow.Table.Columns.Contains("Category"))
                    {
                        if (!stockRow["Category"].ToString()
                                .Equals(cmbGroup.SelectedValue?.ToString()))
                            continue;
                    }

                    if (!int.TryParse(stockRow[0].ToString(), out int itemId))
                        continue;

                    // جلب بيانات الصنف
                    EnsureOpen(conn);
                    string itemSql = $@"SELECT Items.id, Items.code, Items.name,
                                               ItemUnits.sale AS sale_price,
                                               ItemUnits.barcode,
                                               ItemUnits.purch AS purch_price,
                                               ItemUnits.perc
                                        FROM Items
                                        INNER JOIN ItemUnits ON Items.id = ItemUnits.ItemId
                                        WHERE Items.id = {itemId}
                                          AND Items.is_deleted = 0
                                          {unitCondition}";

                    var itemAdapter = new SqlDataAdapter(itemSql, conn);
                    var itemDt = new DataTable();
                    itemAdapter.Fill(itemDt);
                    EnsureClose(conn);

                    if (itemDt.Rows.Count == 0)
                        continue;

                    double perc = 1.0;
                    if (double.TryParse(itemDt.Rows[0]["perc"].ToString(), out double percVal))
                        perc = percVal > 0 ? percVal : 1.0;

                    double rawQty = 0.0;
                    if (double.TryParse(stockRow[1].ToString(), out double rawQ))
                        rawQty = rawQ;

                    double qty = rawQty / perc;

                    // تصفية الكمية
                    if (rbPositiveQty.IsChecked == true && qty <= 0) continue;
                    if (rbNegativeQnty.IsChecked == true && qty >= 0) continue;
                    if (rbZeroQnty.IsChecked == true && qty != 0)     continue;

                    // حساب التكلفة
                    double costPrice = 0.0;
                    if (rbAvgCost.IsChecked == true)
                    {
                        // ItemOper.AvgCost - استدعاء دالة المشروع
                        // costPrice = ItemOper.AvgCost(itemId, MainClass.BranchNo, safeId) * perc;
                        costPrice = GetAvgCost(itemId, branchId, safeId) * perc;
                    }
                    else if (rbPurchPrice.IsChecked == true)
                    {
                        double.TryParse(itemDt.Rows[0]["purch_price"].ToString(), out costPrice);
                    }
                    else if (rbLastPurchPrice.IsChecked == true)
                    {
                        costPrice = GetRecentPurchPrice(itemId);
                    }

                    if (qty < 0) costPrice = 0.0;

                    double totalCost = qty * costPrice;
                    double salePrice = 0.0;
                    double.TryParse(itemDt.Rows[0]["sale_price"].ToString(), out salePrice);
                    double totalSalePrice = salePrice * qty;

                    rowIndex++;
                    _inventoryItems.Add(new InventoryGridItem
                    {
                        DgvNo            = rowIndex,
                        DgvItemCode      = itemDt.Rows[0]["code"].ToString(),
                        DgvItemName      = itemDt.Rows[0]["name"].ToString(),
                        DgvBarcode       = itemDt.Rows[0]["barcode"].ToString(),
                        DgvQty           = qty,
                        DgvAveCost       = costPrice,
                        DgvTotalCost     = totalCost,
                        DgvSalePrice     = salePrice,
                        DgvTotalSalePrice = totalSalePrice,
                        DgvItemId        = itemId
                    });
                }

                SelectedId = -1;

                // تحديث المجاميع
                UpdateSummaries();
                _Finished = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء حساب المخزون\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable GetStockData(int safeId, int branchId)
        {
            try
            {
                EnsureOpen(conn);

                // إذا كان صنف محدد
                if (SelectedId > 0 && safeId > -1)
                {
                    var singleDt = new DataTable();
                    singleDt.Columns.Add("ItemID");
                    singleDt.Columns.Add("Stock");
                    singleDt.Columns.Add("Category");
                    singleDt.Columns.Add("Store");
                    singleDt.Rows.Add(SelectedId, 0, 0, safeId);
                    return singleDt;
                }

                // جلب المخزون الكلي
                string sql = $@"SELECT ItemId AS ItemID,
                                       SUM(CASE WHEN type IN (1,3,5) THEN qty ELSE -qty END) AS Stock,
                                       0 AS Category,
                                       safe_id AS Store
                                FROM Inv_Sub
                                WHERE safe_id = {(safeId == -1 ? "safe_id" : safeId.ToString())}
                                GROUP BY ItemId, safe_id";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
            catch
            {
                return new DataTable();
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private double GetAvgCost(int itemId, int branchId, int safeId)
        {
            try
            {
                EnsureOpen(conn);
                string sql = $@"SELECT ISNULL(SUM(cost * qty) / NULLIF(SUM(qty), 0), 0) AS AvgCost
                                FROM Inv_Sub
                                WHERE ItemId = {itemId}
                                  AND type IN (1,3,5)
                                  {(safeId > -1 ? $"AND safe_id = {safeId}" : "")}";
                var cmd = new SqlCommand(sql, conn);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToDouble(result)
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private double GetRecentPurchPrice(int itemId)
        {
            try
            {
                EnsureOpen(conn);
                string sql = $@"SELECT TOP 1 unit_price FROM Inv_Sub
                                WHERE ItemId = {itemId} AND type = 1
                                ORDER BY inv_id DESC";
                var cmd    = new SqlCommand(sql, conn);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToDouble(result)
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void UpdateSummaries()
        {
            lblTotalItems.Text = _inventoryItems.Count.ToString();
            lblTotalQty.Text   = _inventoryItems.Sum(x => x.DgvQty).ToString("N2");
            lblTotalCost.Text  = _inventoryItems.Sum(x => x.DgvTotalCost).ToString("N2");
            lblTotalSale.Text  = _inventoryItems.Sum(x => x.DgvTotalSalePrice).ToString("N2");
            _stockTotal        = _inventoryItems.Sum(x => x.DgvTotalCost);
        }

        private void ShowItems()
        {
            try
            {
                btnShow.IsEnabled = false;
                CalcStock1();
                txtItemCode.Text = "";
                txtItemName.Text = "";
            }
            catch (Exception ex)
            {
                SetStatus("خطأ: " + ex.Message);
            }
            finally
            {
                btnShow.IsEnabled = true;
            }
        }

        #endregion

        #region Search

        private void SearchByName()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtItemName.Text)) return;

                EnsureOpen(conn);
                string sql = $@"SELECT name, id, Code FROM Items
                                WHERE IS_Deleted = 0
                                  AND name = N'{txtItemName.Text}'";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                if (dt.Rows.Count > 0)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"].ToString();
                    txtItemName.Text = dt.Rows[0]["name"].ToString();
                    ShowItems();
                }
                else
                {
                    OpenItemSearch();
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في البحث: " + ex.Message);
            }
        }

        private void SearchByCode()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtItemCode.Text)) return;

                EnsureOpen(conn);
                string sql = $@"SELECT name, id, Code FROM Items
                                WHERE IS_Deleted = 0
                                  AND Code = N'{txtItemCode.Text}'";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                if (dt.Rows.Count > 0)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"].ToString();
                    txtItemName.Text = dt.Rows[0]["name"].ToString();
                    ShowItems();
                }
                else
                {
                    OpenItemSearch();
                }
            }
            catch (Exception ex)
            {
                SetStatus("خطأ في البحث: " + ex.Message);
            }
        }

        private void OpenItemSearch()
        {
            // فتح نافذة البحث عن الأصناف
            // frmItemsSrch frm = new frmItemsSrch();
            // frm.ShowDialog();
            // if (frm.ISDone && frm.ItemId > 0)
            // {
            //     SelectedId = frm.ItemId;
            //     txtItemCode.Text = GetItemCode(SelectedId);
            //     txtItemName.Text = frm.Itemname;
            //     ShowItems();
            // }
            DXMessageBox.Show("افتح نافذة البحث عن الأصناف هنا", "بحث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GetItemsWithNoInv(ref DataTable dtr)
        {
            try
            {
                dtr.Columns.Clear();
                dtr.Columns.Add("ItemID");
                dtr.Columns.Add("Stock");
                dtr.Columns.Add("Category");
                dtr.Columns.Add("Store");

                EnsureOpen(conn);
                string sql = @"SELECT p.id AS ItemsId
                               FROM Items AS p
                               LEFT JOIN Inv_Sub AS od ON od.ItemId = p.id
                               WHERE od.ItemId IS NULL";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                foreach (DataRow row in dt.Rows)
                    dtr.Rows.Add(row["ItemsId"], 0, 0, 0);
            }
            catch
            {
                // تجاهل الخطأ
            }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowItems();
            _stockTotal = _inventoryItems.Sum(x => x.DgvTotalCost);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            cmbGroup.SelectedIndex = -1;
            txtItemCode.Text       = "";
            txtItemName.Text       = "";
            txtGroupNo.Text        = "";
            cmbSafes.SelectedIndex = -1;
            cmbUnits.SelectedIndex = -1;
            chkAll.IsChecked       = false;
            chkItemsNoInv.IsChecked       = false;
            chkOnlyItemsWithoudInv.IsChecked = false;
            _inventoryItems.Clear();
            UpdateSummaries();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked != true && cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafes.Focus();
                return;
            }
            OpenItemSearch();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(1);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_inventoryItems.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileName = $"كشف_جرد_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var writer = new System.IO.StreamWriter(filePath,
                       false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("#,رمز الصنف,اسم الصنف,الباركود,الكمية,التكلفة,إجمالي التكلفة,سعر البيع,إجمالي سعر البيع");
                    foreach (var item in _inventoryItems)
                    {
                        writer.WriteLine($"{item.DgvNo},{item.DgvItemCode},{item.DgvItemName}," +
                                         $"{item.DgvBarcode},{item.DgvQty:N2},{item.DgvAveCost:N2}," +
                                         $"{item.DgvTotalCost:N2},{item.DgvSalePrice:N2},{item.DgvTotalSalePrice:N2}");
                    }
                }

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة التسوية الجردية
            // frmSafeAdjust frm = new frmSafeAdjust();
            // frm.cmbSafes.SelectedValue = cmbSafes.SelectedValue;
            // frm.Show();
            DXMessageBox.Show("افتح نافذة التسوية الجردية هنا", "تسوية",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGenerateInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_inventoryItems.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات لتوليد فاتورة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // تجهيز قائمة الأصناف للفاتورة
            // List<InvoiceItem> list = _inventoryItems.Select(x => new InvoiceItem
            // {
            //     ItemId       = x.DgvItemId,
            //     ItemQuantity = Math.Abs(x.DgvQty),
            //     ItemPrice    = Math.Abs(x.DgvAveCost)
            // }).ToList();
            // frmSelectInv frm = new frmSelectInv();
            // frm.itemList = list;
            // frm.Show();
            DXMessageBox.Show("افتح نافذة اختيار الفاتورة هنا", "توليد فاتورة",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAprroved_Click(object sender, RoutedEventArgs e)
        {
            ISDone      = true;
            _stockTotal = _inventoryItems.Sum(x => x.DgvTotalCost);
            this.Close();
        }

        private void BtnShowItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InventoryGridItem item)
            {
                ShowItemCard(item.DgvItemId);
            }
        }

        private void BtnAdjust_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked == true || cmbSafes.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب تحديد مستودع محدد لإجراء التسوية", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                chkAll.IsChecked = false;
                cmbSafes.Focus();
                return;
            }

            if (sender is Button btn && btn.Tag is InventoryGridItem item)
            {
                // فتح نافذة التسوية للصنف المحدد
                // frmSafeAdjust frm = new frmSafeAdjust();
                // frm.type = 2;
                // frm.SelectedId = item.DgvItemId;
                // frm.cmbSafes.SelectedValue = cmbSafes.SelectedValue;
                // frm.Show();
                DXMessageBox.Show($"تسوية الصنف: {item.DgvItemName}", "تسوية",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowItemCard(int itemId)
        {
            // if (User.EditItemInfo)
            // {
            //     frmItems frm = new frmItems();
            //     frm.ItemId = itemId;
            //     frm.ShowDialog();
            // }
            // else
            //     DXMessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
            DXMessageBox.Show($"عرض بيانات الصنف رقم: {itemId}", "عرض الصنف",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region ComboBox / CheckBox Events

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbSafes != null)
            {
                cmbSafes.IsEnabled = chkAll.IsChecked != true;
                if (chkAll.IsChecked == true)
                    cmbSafes.SelectedIndex = -1;
            }
            ClearGrid();
        }

        private void chkAllBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbBranches != null)
                cmbBranches.IsEnabled = chkAllBranch.IsChecked != true;
        }

        private void cmbSafes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearGrid();
        }

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGroup.SelectedIndex >= 0 && cmbGroup.SelectedValue != null)
            {
                txtGroupNo.Text  = cmbGroup.SelectedValue.ToString();
                txtItemCode.Text = "";
            }
        }

        private void cmbBranches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBranches.SelectedIndex > -1 && cmbBranches.SelectedValue != null)
            {
                if (short.TryParse(cmbBranches.SelectedValue.ToString(), out short branchId))
                    LoadSafes(branchId);
            }
        }

        private void txtGroupNo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (cmbGroup != null && !string.IsNullOrWhiteSpace(txtGroupNo.Text))
                    cmbGroup.SelectedValue = txtGroupNo.Text;
            }
            catch
            {
                // تجاهل خطأ عدم التطابق
            }
        }

        #endregion

        #region TextBox KeyDown Events

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                e.Handled = true;
                SearchByName();
            }
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtItemCode.Text))
            {
                e.Handled = true;
                SearchByCode();
            }
        }

        #endregion

        #region Print

        private void PrintReport(int printMode)
        {
            RptUrl  = MainClass.ReportsPath;
            RptName = "rptSafeGrd.repx";

            if (_inventoryItems.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات بالجدول", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = System.IO.Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetStatus(printMode == 1 ? "جارِ الطباعة..." : "جارِ المعاينة...");
            // تنفيذ الطباعة عبر XtraReport
        }

        #endregion

        #region Helpers

        private void ClearGrid()
        {
            _inventoryItems?.Clear();
            UpdateSummaries();
        }

        private void SetStatus(string message)
        {
            // يمكن إضافة status bar لاحقاً
        }

        private void EnsureOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void EnsureClose(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }
}