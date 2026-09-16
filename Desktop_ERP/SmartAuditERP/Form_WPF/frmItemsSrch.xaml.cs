using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsSrch : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public int selectedCli { get; set; }
        public int StoreId { get; set; } = 1;
        public string sql { get; set; }
        public string search { get; set; }
        public string Itemname { get; set; } = "";
        public int ItemId { get; set; } = -1;
        public bool ISDone { get; set; } = false;

        private int offset = 0;
        private int limit = 1000;
        private bool isLoading = false;
        private string cond = "";
        private int DgvFontSize = 1;

        public List<int> Itemlist;

        private string StyleFile;
        private string StyleFolder;

        private ObservableCollection<ItemSearchRow> _allItems;
        private ObservableCollection<SafeBalanceRow_frmItemsSrch> _safeRows;

        #endregion

        #region Constructor

        public frmItemsSrch()
        {
            conn = MainClass.ConnObj();
            Itemlist = new List<int>();
            _allItems = new ObservableCollection<ItemSearchRow>();
            _safeRows = new ObservableCollection<SafeBalanceRow_frmItemsSrch>();
            StyleFile = Path.Combine(MainClass.ReportsPath, "Styles\\ItemSearchLayout.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            InitializeComponent();
        }

        #endregion

        #region Load

        private void AllCurr_Load(object sender, RoutedEventArgs e)
        {
            dgvSafeBalance.ItemsSource = _safeRows;
            LoadDgvItems(cond, 1);
            LoadGroups();
            LoadSafes();
            UpdateSearchWatermark();

            if (!string.IsNullOrWhiteSpace(txtSrchNm.Text))
            {
                txtSrchNm_TextChanged(null, null);
                txtSrchNm.Focus();
            }

            if (!string.IsNullOrWhiteSpace(txtSrchCode.Text))
                txtSrchCode_TextChanged(null, null);
        }

        #endregion

        #region Load Data

        public void LoadGroups()
        {
            try
            {
                string sql = $"select id,name from ItemsCategory where BranchId={MainClass.BranchNo} OR AllBranch=1 order by id";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbItemGrp.ItemsSource = dt.DefaultView;
                cmbItemGrp.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void LoadSafes()
        {
            try
            {
                // Safes loaded dynamically when item selected
            }
            catch { }
        }

        public void LoadDgvItems(string condition, int loadType)
        {
            try
            {
                if (isLoading) return;
                isLoading = true;

                string pageSql = $@"
                    SELECT i.id, i.code, i.name, i.nameEN, i.barcode, i.group_id, i.sale_price
                    FROM Items i
                    JOIN ItemsCategory ic ON i.group_id = ic.id
                    WHERE i.IS_Deleted = 0
                    AND (ic.BranchId = {MainClass.BranchNo} OR ic.AllBranch = 1)
                    {condition}
                    ORDER BY i.id
                    OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";

                string fullSql = $@"
                    SELECT i.id, i.code, i.name, i.nameEN, i.barcode, i.group_id, i.sale_price
                    FROM Items i
                    JOIN ItemsCategory ic ON i.group_id = ic.id
                    WHERE i.IS_Deleted = 0
                    AND (ic.BranchId = {MainClass.BranchNo} OR ic.AllBranch = 1)
                    {condition}
                    ORDER BY i.id";

                string executeSql = loadType == 1 ? pageSql : fullSql;

                SqlDataAdapter adapter = new SqlDataAdapter(executeSql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (loadType == 2)
                {
                    _allItems.Clear();
                    offset = 0;
                }

                foreach (DataRow row in dt.Rows)
                {
                    _allItems.Add(new ItemSearchRow
                    {
                        id = row["id"].ToString(),
                        code = row["code"].ToString(),
                        name = row["name"].ToString(),
                        nameEN = row["nameEN"].ToString(),
                        barcode = row["barcode"].ToString(),
                        group_id = row["group_id"].ToString(),
                        sale_price = ToDoubleSafe(row["sale_price"])
                    });
                }

                GridControl1.ItemsSource = _allItems;
                UpdateRowCount();

                offset += limit;
                isLoading = false;
            }
            catch
            {
                isLoading = false;
            }
        }

        #endregion

        #region Search Filters

        private void txtSrchNm_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchWatermark();
            try
            {
                string filterCondition = "";
                if (!string.IsNullOrWhiteSpace(txtSrchNm.Text))
                {
                    filterCondition = $"AND (i.name LIKE N'%{txtSrchNm.Text}%' " +
                                      $"OR i.code LIKE N'%{txtSrchNm.Text}%' " +
                                      $"OR i.barcode LIKE N'%{txtSrchNm.Text}%' " +
                                      $"OR i.nameEN LIKE N'%{txtSrchNm.Text}%')";
                }
                LoadDgvItems(filterCondition, 2);
            }
            catch { }
        }

        private void txtSrchNmEn_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filterCondition = "";
                if (!string.IsNullOrWhiteSpace(txtSrchNmEn.Text))
                    filterCondition = $"AND (i.nameEn LIKE N'%{txtSrchNmEn.Text}%')";
                LoadDgvItems(filterCondition, 2);
            }
            catch { }
        }

        private void barcode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filterCondition = "";
                if (!string.IsNullOrWhiteSpace(txtSrchBarcode.Text))
                    filterCondition = $"AND (i.barcode LIKE N'%{txtSrchBarcode.Text}%')";
                LoadDgvItems(filterCondition, 2);
            }
            catch { }
        }

        private void txtSrchCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filterCondition = "";
                if (!string.IsNullOrWhiteSpace(txtSrchCode.Text))
                    filterCondition = $"AND (i.Code LIKE N'%{txtSrchCode.Text}%')";
                LoadDgvItems(filterCondition, 2);
            }
            catch { }
        }

        private void cmbItemGrp_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbItemGrp.SelectedValue != null)
                    LoadDgvItems($"AND (i.group_Id={cmbItemGrp.SelectedValue})", 2);
            }
            catch { }
        }

        private void txtMaxPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string filterCondition = "";
                if (!string.IsNullOrWhiteSpace(txtMaxPrice.Text))
                {
                    string minVal = string.IsNullOrWhiteSpace(txtMinPrice.Text) ? "0" : txtMinPrice.Text;
                    double minPrice = 0, maxPrice = 0;
                    double.TryParse(minVal, out minPrice);
                    double.TryParse(txtMaxPrice.Text, out maxPrice);

                    filterCondition = $"AND (i.sale_price>={minPrice} and i.sale_price<={maxPrice})";
                    LoadDgvItems(filterCondition, 2);
                }
            }
            catch { }
        }

        #endregion

        #region Grid Events

        private void GridControl1_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ItemSearchRow selected = GridControl1.SelectedItem as ItemSearchRow;
                if (selected == null) return;

                int.TryParse(selected.id, out int itemId);
                ItemId = itemId;
                CalcStock(itemId);

                if (!User.ShowCosts)
                {
                    txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(itemId).ToString("0.00");
                    txtCostAvrg.Text = ItemOper.Cost(itemId).ToString("0.00");
                }
                else
                {
                    txtRecentPurchPrice.Text = "0";
                    txtCostAvrg.Text = "0";
                }

                LoadPrices(itemId);
            }
            catch { }
        }

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                foreach (ItemSearchRow item in GridControl1.SelectedItems)
                {
                    int.TryParse(item.id, out int itemId);
                    if (itemId > 0 && !Itemlist.Contains(itemId))
                        Itemlist.Add(itemId);
                }
            }
            catch { }
        }

        private void GridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SelectCurrentItem();
        }

        private void SelectCurrentItem()
        {
            try
            {
                ItemSearchRow selected = GridControl1.SelectedItem as ItemSearchRow;
                if (selected == null) return;

                int.TryParse(selected.id, out int itemId);

                if (Itemlist.Contains(itemId))
                    Itemlist.Remove(itemId);

                ISDone = true;
                ItemId = itemId;
                Itemname = selected.name;
                Itemlist.Add(itemId);

                Close();
            }
            catch { }
        }

        #endregion

        #region Stock

        private void CalcStock(int itemID)
        {
            try
            {
                _safeRows.Clear();
                double total = 0.0;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select id,name from Safes where branch={MainClass.BranchNo} and status=1 and IS_Deleted=0",
                    conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int safeId = Convert.ToInt32(row["id"]);
                    double qty = Inventory.CalcItemStock(safeId, itemID, MainClass.BranchNo);

                    if (qty != 0.0)
                    {
                        _safeRows.Add(new SafeBalanceRow_frmItemsSrch
                        {
                            SafeName = row["name"].ToString(),
                            SafeQty = qty.ToString("N2")
                        });
                        total += qty;
                    }
                }

                txtTotalStock.Text = total.ToString(Common.DigitsNo);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select * from ItemPrices where Itemid={itemId} order by Proc_id desc", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text = dt.Rows[0]["low_sale_price"]?.ToString() ?? "";
                    txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"]?.ToString() ?? "";
                }
            }
            catch { }
        }

        #endregion

        #region Buttons

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtMaxPrice.Text = "";
            txtMinPrice.Text = "";
            txtSrchBarcode.Text = "";
            txtSrchNm.Text = "";
            txtSrchCode.Text = "";
            txtSrchNmEn.Text = "";
            txtCompetitorPrice.Text = "";
            txtCostAvrg.Text = "";
            txtLastSalePrice.Text = "";
            txtRecentPurchPrice.Text = "";
            cmbItemGrp.SelectedIndex = -1;

            _allItems.Clear();
            offset = 0;
            LoadDgvItems(cond, 1);
        }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (User.EditItemInfo)
            {
                frmItems form = new frmItems();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.ShowDialog();

                _allItems.Clear();
                offset = 0;
                LoadDgvItems(cond, 1);
            }
            else
            {
                DXMessageBox.Show("لا يوجد لديك صلاحية هذه العملية", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void IconButton1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(StyleFolder))
                    Directory.CreateDirectory(StyleFolder);

                DXMessageBox.Show("تم حفظ مظهر الجدول", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(StyleFile))
                    File.Delete(StyleFile);
            }
            catch { }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            DgvFontSize++;
            ApplyFontSize();
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (DgvFontSize > 1) DgvFontSize--;
            ApplyFontSize();
        }

        private void ApplyFontSize()
        {
            double baseFontSize = 11.0 + (DgvFontSize - 1);
            GridControl1.FontSize = Math.Max(9, Math.Min(20, baseFontSize));
        }

        #endregion

        #region KeyDown

        private void frmItemsSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void txtSrchNm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void txtSrchNmEn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void cmbItemGrp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void txtSrchBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void txtSrchCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void txtMinPrice_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        private void txtMaxPrice_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
                GridControl1.Focus();
        }

        #endregion

        #region Watermark

        private void txtSrchNm_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void txtSrchNm_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void UpdateSearchWatermark()
        {
            if (txtSrchNmWatermark == null || txtSrchNm == null) return;

            txtSrchNmWatermark.Visibility =
                string.IsNullOrWhiteSpace(txtSrchNm.Text) && !txtSrchNm.IsKeyboardFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Closing

        private void frmItemsSrch_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // للتوسع المستقبلي
        }

        #endregion

        #region Helpers

        private void UpdateRowCount()
        {
            lblRowCount.Text = $"عدد السجلات: {_allItems.Count:N0}";
        }

        private double ToDoubleSafe(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            double.TryParse(value.ToString(), out double result);
            return result;
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class ItemSearchRow
    {
        public string id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public string nameEN { get; set; }
        public string barcode { get; set; }
        public string group_id { get; set; }
        public double sale_price { get; set; }
    }

    public class SafeBalanceRow_frmItemsSrch
    {
        public string SafeName { get; set; }
        public string SafeQty { get; set; }
    }
}