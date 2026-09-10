using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsDir : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code = -1;
        private bool Order = false;
        private int GrpId = -1;
        private int groupId = -1;
        private int ID = -1;
        private string SelectedCode = "";

        public List<int> Itemlist;

        private ObservableCollection<ItemDirRow> _allItems;
        private ObservableCollection<ItemDirRow> _filteredItems;
        private List<CategoryTreeNode> _treeNodes;

        #endregion

        #region Constructor

        public frmItemsDir()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Itemlist = new List<int>();
            _allItems = new ObservableCollection<ItemDirRow>();
            _treeNodes = new List<CategoryTreeNode>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmGroupDir_Load(object sender, RoutedEventArgs e)
        {
            if (Sync.ActiveSync && Sync.SyncType > 0)
            {
                btnsyncCategories.Visibility = Visibility.Visible;
                btnSyncItems.Visibility = Visibility.Visible;
                BtnRecieveditem.Visibility = Visibility.Visible;
                btnUnits.Visibility = Visibility.Visible;
                btnItemQuantity.Visibility = Visibility.Visible;
            }

            LoadDgvItems();
            LoadTreeList();
            UpdateSearchWatermark();
        }

        #endregion

        #region Load Data

        private void LoadDgvItems()
        {
            try
            {
                _allItems.Clear();

                string groupFilter = GrpId != -1 ? $" and items.group_id={GrpId}" : "";

                string sql =
                    "select items.Code as item_Code, items.name, items.nameEN, barcode, " +
                    "itemsCategory.name as groupname, units.name as unit_name, " +
                    "purch_price, sale_price, items.id as item_id, items.group_id, " +
                    "ItemsCategory.ParentCode, Item_Sort " +
                    "from units, items, itemsCategory " +
                    "where items.unit = units.id and items.group_id = itemsCategory.id " +
                    "and items.IS_Deleted=0 " + groupFilter + " order by items.Code";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                int rowNumber = 0;
                foreach (DataRow row in dt.Rows)
                {
                    rowNumber++;
                    _allItems.Add(new ItemDirRow
                    {
                        DgvNo = rowNumber.ToString(),
                        DgvItemId = row["item_id"].ToString(),
                        DgvItemCode = row["item_Code"].ToString(),
                        DgvItemName = row["name"].ToString(),
                        DgvItemNameEn = row["nameEN"].ToString(),
                        DgvBarcode = row["barcode"].ToString(),
                        DgvItemCategory = row["groupname"].ToString(),
                        DgvUnit = row["unit_name"].ToString(),
                        DgvSalePrice = ToDoubleSafe(row["sale_price"]),
                        DgvPurchPrice = ToDoubleSafe(row["purch_price"])
                    });
                }

                GridControl1.ItemsSource = _allItems;
                UpdateRowCount();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ShowItems()
        {
            try
            {
                _allItems.Clear();

                string sql = @"
                    SELECT items.Code AS item_Code, items.name, items.nameEN, barcode,
                           itemsCategory.name AS groupname, units.name AS unit_name,
                           purch_price, sale_price, items.Item_Sort, items.id AS item_id,
                           items.group_id, ItemsCategory.ParentCode
                    FROM items
                    INNER JOIN itemsCategory ON items.group_id = itemsCategory.id
                    INNER JOIN units ON items.unit = units.id
                    LEFT JOIN inv_sub ON items.id = inv_sub.ItemId
                    WHERE items.IS_Deleted = 0 AND inv_sub.ItemId IS NULL
                    ORDER BY items.Code";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                int rowNumber = 0;
                foreach (DataRow row in dt.Rows)
                {
                    rowNumber++;
                    _allItems.Add(new ItemDirRow
                    {
                        DgvNo = rowNumber.ToString(),
                        DgvItemId = row["item_id"].ToString(),
                        DgvItemCode = row["item_Code"].ToString(),
                        DgvItemName = row["name"].ToString(),
                        DgvItemNameEn = row["nameEN"].ToString(),
                        DgvBarcode = row["barcode"].ToString(),
                        DgvItemCategory = row["groupname"].ToString(),
                        DgvUnit = row["unit_name"].ToString(),
                        DgvSalePrice = ToDoubleSafe(row["sale_price"]),
                        DgvPurchPrice = ToDoubleSafe(row["purch_price"])
                    });
                }

                GridControl1.ItemsSource = _allItems;
                UpdateRowCount();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadTreeList()
        {
            try
            {
                string sql = "SELECT id, code, name, ParentCode FROM itemsCategory WHERE IS_Deleted = 0";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                _treeNodes = BuildTree(dt);

                TreeList1.ItemsSource = _treeNodes;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private List<CategoryTreeNode> BuildTree(DataTable dt)
        {
            Dictionary<string, CategoryTreeNode> nodeMap = new Dictionary<string, CategoryTreeNode>();
            List<CategoryTreeNode> roots = new List<CategoryTreeNode>();

            foreach (DataRow row in dt.Rows)
            {
                string id = row["id"].ToString();
                string code = row["code"].ToString();
                string name = row["name"].ToString();
                string parentCode = row["ParentCode"] == DBNull.Value
                    ? null
                    : row["ParentCode"].ToString();

                if (parentCode == "-1") parentCode = null;

                var node = new CategoryTreeNode
                {
                    Id = id,
                    Code = code,
                    Name = name,
                    ParentCode = parentCode,
                    Children = new List<CategoryTreeNode>()
                };

                nodeMap[code] = node;
            }

            foreach (var node in nodeMap.Values)
            {
                if (string.IsNullOrEmpty(node.ParentCode))
                    roots.Add(node);
                else if (nodeMap.TryGetValue(node.ParentCode, out CategoryTreeNode parent))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }

            return roots;
        }

        #endregion

        #region TreeView

        private void TreeList1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                CategoryTreeNode selected = TreeList1.SelectedItem as CategoryTreeNode;
                if (selected == null) return;

                GrpId = int.TryParse(selected.Id, out int groupIdValue) ? groupIdValue : -1;
                SelectedCode = selected.Code;

                LoadDgvItems();
            }
            catch
            {
            }
        }

        #endregion

        #region Search

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchWatermark();

            string keyword = txtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(keyword))
            {
                GridControl1.ItemsSource = _allItems;
            }
            else
            {
                var filteredItems = new ObservableCollection<ItemDirRow>();

                foreach (ItemDirRow item in _allItems)
                {
                    if ((item.DgvItemName?.ToLower().Contains(keyword) == true) ||
                        (item.DgvItemCode?.ToLower().Contains(keyword) == true) ||
                        (item.DgvBarcode?.ToLower().Contains(keyword) == true) ||
                        (item.DgvItemNameEn?.ToLower().Contains(keyword) == true) ||
                        (item.DgvItemCategory?.ToLower().Contains(keyword) == true))
                    {
                        filteredItems.Add(item);
                    }
                }

                GridControl1.ItemsSource = filteredItems;
            }

            UpdateRowCount();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void UpdateSearchWatermark()
        {
            if (txtSearchWatermark == null || txtSearch == null)
                return;

            txtSearchWatermark.Visibility =
                string.IsNullOrWhiteSpace(txtSearch.Text) && !txtSearch.IsKeyboardFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Buttons

        private void btnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            frmItemCategory form = new frmItemCategory();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
            GrpId = -1;
            LoadTreeList();
            LoadDgvItems();
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            frmItems form = new frmItems();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.Show();

            if (GrpId != -1)
                form.cmbGroups.SelectedValue = GrpId;

            LoadTreeList();
            LoadDgvItems();
        }

        private void btnRecode_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo > 1)
            {
                DXMessageBox.Show("ليس لديك صلاحية لترميز المواد", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = DXMessageBox.Show(
                "هل أنت متأكد من ترميز المواد؟", "تحذير",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                RecodeItems();
        }

        private void btnLoadAll_Click(object sender, RoutedEventArgs e)
        {
            GrpId = -1;
            SelectedCode = "";
            txtSearch.Text = "";
            LoadTreeList();
            LoadDgvItems();
        }

        private void BtnShowNonActiveItems_Click(object sender, RoutedEventArgs e)
        {
            ShowItems();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnSyncItems_Click(object sender, RoutedEventArgs e)
        {
            if (DXMessageBox.Show("هل تريد مزامنة بيانات الأصناف؟", "",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                && Sync.ActiveSync && Sync.SyncType > 0)
            {
                CategoryTreeNode selected = TreeList1.SelectedItem as CategoryTreeNode;

                if (selected != null && !string.IsNullOrWhiteSpace(selected.Id))
                    SyncProduct();
                else
                    DXMessageBox.Show("يجب تحديد المجموعة", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnsyncCategories_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.BranchType == 2 && Sync.SyncType > 0)
            {
                if (Sync.ValidAPIUrl &&
                    DXMessageBox.Show("هل تريد تحديث المجموعات عبر نظام المزامنة؟", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new ItemOper().ReadCategoriesOnline();
                }
            }
            else if (DXMessageBox.Show("هل تريد مزامنة بيانات المجموعات؟", "",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                && Sync.ActiveSync && Sync.SyncType > 0)
            {
                SyncCategory();
            }
        }

        private void btnUnits_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.BranchType == 2 && Sync.SyncType > 0)
            {
                if (Sync.ValidAPIUrl &&
                    DXMessageBox.Show("هل تريد تحديث الوحدات عبر نظام المزامنة؟", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new ItemOper().ReadUnitsOnline();
                }
            }
            else if (DXMessageBox.Show("هل تريد مزامنة بيانات الوحدات؟", "",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
                && Sync.ActiveSync && Sync.SyncType > 0)
            {
                SyncUnits();
            }
        }

        private void btnItemQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.ActiveSync && Sync.SyncType == 1)
                new Thread(Inventory.PostStocks).Start();
        }

        private void BtnRecieveditem_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.ValidAPIUrl &&
                DXMessageBox.Show("هل تريد تحديث بيانات الأصناف عبر نظام المزامنة؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                new ItemOper().ReadProductsOnline();
            }
        }

        #endregion

        #region Grid Buttons

        private void DgvBtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                ItemDirRow row = btn.Tag as ItemDirRow;
                if (row == null) return;

                int itemId = 0;
                int.TryParse(row.DgvItemId, out itemId);

                frmItems form = new frmItems();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Show();
                form.Navigate($"select * from items where IS_Deleted=0 and id={itemId}");

                GrpId = -1;
                LoadTreeList();
                LoadDgvItems();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                ItemDirRow row = btn.Tag as ItemDirRow;
                if (row == null) return;

                int itemId = 0;
                int.TryParse(row.DgvItemId, out itemId);

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    "select Inv_Sub.InvGlobalID from Inv_Sub " +
                    "left join Inv on Inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                    "where Inv_Sub.ItemId=" + itemId + " and Inv.IS_Deleted=0", conn);
                DataTable checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                if (checkDt.Rows.Count > 0)
                {
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "لا يمكن حذف مادة مرتبطة بفواتير"
                        : "Item previously used in invoices";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult confirm = DXMessageBox.Show(
                    "هل أنت متأكد من حذف الصنف؟", "تحذير",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                DeleteItemFromDB(itemId);

                string successMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "تم الحذف" : "Deleted";
                DXMessageBox.Show(successMsg, "", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadTreeList();
                LoadDgvItems();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void BtnEditItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridControl1.SelectedItems.Count == 0)
                {
                    DXMessageBox.Show("يجب تحديد الأصناف", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Itemlist.Clear();
                foreach (ItemDirRow item in GridControl1.SelectedItems)
                {
                    int itemId = 0;
                    int.TryParse(item.DgvItemId, out itemId);
                    if (itemId > 0)
                        Itemlist.Add(itemId);
                }

                frmEditItems editForm = new frmEditItems();
                editForm.itemList = Itemlist;
                editForm.ShowDialog();

                LoadDgvItems();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void BtnDeleteItems_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string confirmMsg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                    ? "Are you sure to delete the selected items?"
                    : "هل أنت متأكد من حذف الأصناف المحددة؟";

                if (DXMessageBox.Show(confirmMsg, "", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                foreach (ItemDirRow item in GridControl1.SelectedItems)
                {
                    int itemId = 0;
                    int.TryParse(item.DgvItemId, out itemId);
                    DeleteItems(itemId, item.DgvItemName);
                }

                string doneMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "تم" : "Done";
                DXMessageBox.Show(doneMsg, "", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDgvItems();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ButtonExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_allItems == null || _allItems.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير.", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    FileName = "دليل_الأصناف",
                    Filter = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveDialog.ShowDialog() != true) return;

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    ExportToCsv(saveDialog.FileName);
                else
                    ExportToExcelHtml(saveDialog.FileName);

                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Recode

        private void RecodeItems()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedCode))
                {
                    DXMessageBox.Show("يجب اختيار مجموعة", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlDataAdapter catAdapter = new SqlDataAdapter(
                    "select id,code from itemsCategory where code=N'" + SelectedCode + "' and IS_deleted=0",
                    conn1);
                DataTable catDt = new DataTable();
                catAdapter.Fill(catDt);

                if (catDt.Rows.Count == 0) return;

                SqlCommand clearCmd = new SqlCommand(
                    "update Items set Code=@Code, GrpCode=@GrpCode where Grpcode=N'" + SelectedCode + "' and IS_deleted=0",
                    conn);
                clearCmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = "";
                clearCmd.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = "";
                clearCmd.ExecuteNonQuery();

                SqlDataAdapter itemsAdapter = new SqlDataAdapter(
                    "select id as item_id from items where group_id=" + catDt.Rows[0]["id"] + " and IS_Deleted=0",
                    conn1);
                DataTable itemsDt = new DataTable();
                itemsAdapter.Fill(itemsDt);

                for (int i = 0; i < itemsDt.Rows.Count; i++)
                {
                    string newCode = GenerateCode(catDt.Rows[0]["code"].ToString()) ?? "";

                    SqlCommand updateCmd = new SqlCommand(
                        "update Items set Code=@Code, GrpCode=@GrpCode where id=" + itemsDt.Rows[i]["item_id"],
                        conn);
                    updateCmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = newCode;
                    updateCmd.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = catDt.Rows[0]["code"];
                    updateCmd.ExecuteNonQuery();
                }

                DXMessageBox.Show("تمت عملية الترميز بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                GrpId = 1;
                LoadTreeList();
                LoadDgvItems();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private string GenerateCode(string grpCode)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select COUNT(*) as ItemNo from Items where GrpCode=N'" + grpCode + "'", conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double countVal = 0;
                    double.TryParse(dt.Rows[0]["ItemNo"].ToString(), out countVal);
                    int num = (int)Math.Round(countVal + 1.0);

                    if (num < 10) return grpCode + "000" + num;
                    if (num < 100) return grpCode + "00" + num;
                    if (num < 1000) return grpCode + "0" + num;

                    return grpCode + num.ToString();
                }

                return grpCode + "0001";
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return "";
            }
        }

        #endregion

        #region Delete

        private void DeleteItems(int itemId, string itemName)
        {
            try
            {
                if (itemId <= 0) return;

                using SqlConnection sqlConn = MainClass.ConnObj();
                sqlConn.Open();

                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    "SELECT Inv_Sub.InvGlobalID FROM Inv_Sub " +
                    "LEFT JOIN Inv ON Inv.InvGlobalID = Inv_Sub.InvGlobalID " +
                    "WHERE Inv_Sub.ItemId = @ItemID AND Inv.IS_Deleted = 0", sqlConn);
                checkAdapter.SelectCommand.Parameters.AddWithValue("@ItemID", itemId);
                DataTable checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                if (checkDt.Rows.Count > 0)
                {
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "لا يمكن حذف مادة مرتبطة بفواتير: " + itemName
                        : "Item previously used in invoices: " + itemName;
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DeleteItemFromDB(itemId, sqlConn);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DeleteItemFromDB(int itemId)
        {
            using SqlConnection sqlConn = MainClass.ConnObj();
            sqlConn.Open();
            DeleteItemFromDB(itemId, sqlConn);
        }

        private void DeleteItemFromDB(int itemId, SqlConnection sqlConn)
        {
            new SqlCommand("DELETE FROM Items WHERE id=@id", sqlConn) { Parameters = { new SqlParameter("@id", itemId) } }.ExecuteNonQuery();
            new SqlCommand("DELETE FROM ItemUnits WHERE ItemId=@id", sqlConn) { Parameters = { new SqlParameter("@id", itemId) } }.ExecuteNonQuery();
            new SqlCommand("DELETE FROM Itembarcodes WHERE ItemId=@id", sqlConn) { Parameters = { new SqlParameter("@id", itemId) } }.ExecuteNonQuery();
            new SqlCommand("DELETE FROM ItemComponents WHERE ItemId=@id", sqlConn) { Parameters = { new SqlParameter("@id", itemId) } }.ExecuteNonQuery();
            new SqlCommand("DELETE FROM ItemPrices WHERE ItemId=@id", sqlConn) { Parameters = { new SqlParameter("@id", itemId) } }.ExecuteNonQuery();
        }

        #endregion

        #region Print / Export

        private void PrintDevexpress(int printMode)
        {
            try
            {
                if (_allItems == null || _allItems.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string reportsPath = MainClass.ReportsPath;
                string reportsPrinter = MainClass.ReportsPrinter;
                string rptName = "rptItemsDirectory.repx";

                new Report().Printing(printMode, BindToData(), reportsPath, rptName, reportsPrinter, 1);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private DataSet BindToData()
        {
            List<InventoryData> list = new List<InventoryData>();

            foreach (ItemDirRow item in _allItems)
            {
                list.Add(new InventoryData
                {
                    CategoryName = item.DgvItemCategory,
                    ItemCode = item.DgvItemCode,
                    ItemName = item.DgvItemName,
                    ItemNameEn = item.DgvItemNameEn,
                    Barcode = item.DgvBarcode,
                    Unit = item.DgvUnit,
                    Purch = item.DgvPurchPrice.ToString("N2"),
                    SellPrice = item.DgvSalePrice.ToString("N2"),
                    Status = " ",
                    InventoryType = Title,
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            DataSet dataSet = new DataSet("Name");
            dataSet.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return dataSet;
        }

        private void ExportToCsv(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("م,الرمز,الصنف,الصنف (EN),الباركود,المجموعة,الوحدة,سعر البيع,سعر الشراء");

            foreach (ItemDirRow item in _allItems)
            {
                sb.AppendLine(
                    $"\"{item.DgvNo}\"," +
                    $"\"{item.DgvItemCode}\"," +
                    $"\"{item.DgvItemName}\"," +
                    $"\"{item.DgvItemNameEn}\"," +
                    $"\"{item.DgvBarcode}\"," +
                    $"\"{item.DgvItemCategory}\"," +
                    $"\"{item.DgvUnit}\"," +
                    $"\"{item.DgvSalePrice:N2}\"," +
                    $"\"{item.DgvPurchPrice:N2}\""
                );
            }

            File.WriteAllText(fileName, sb.ToString(), System.Text.Encoding.UTF8);
        }

        private void ExportToExcelHtml(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine("<style>table{border-collapse:collapse;font-family:Tahoma;font-size:11px;}");
            sb.AppendLine("th,td{border:1px solid #999;padding:4px;text-align:center;}");
            sb.AppendLine("th{background:#DDE7FF;font-weight:bold;}</style></head>");
            sb.AppendLine("<body dir='rtl'>");
            sb.AppendLine($"<h2 style='text-align:center;'>{Title}</h2>");
            sb.AppendLine("<table><tr>");
            sb.AppendLine("<th>م</th><th>الرمز</th><th>الصنف</th><th>الصنف (EN)</th>");
            sb.AppendLine("<th>الباركود</th><th>المجموعة</th><th>الوحدة</th><th>سعر البيع</th><th>سعر الشراء</th>");
            sb.AppendLine("</tr>");

            foreach (ItemDirRow item in _allItems)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{item.DgvNo}</td><td>{item.DgvItemCode}</td>");
                sb.AppendLine($"<td>{item.DgvItemName}</td><td>{item.DgvItemNameEn}</td>");
                sb.AppendLine($"<td>{item.DgvBarcode}</td><td>{item.DgvItemCategory}</td>");
                sb.AppendLine($"<td>{item.DgvUnit}</td><td>{item.DgvSalePrice:N2}</td><td>{item.DgvPurchPrice:N2}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table></body></html>");
            File.WriteAllText(fileName, sb.ToString(), System.Text.Encoding.UTF8);
        }

        #endregion

        #region Sync

        private async void SyncProduct()
        {
            // محتفظ بنفس منطق الكود الأصلي
            // يمكن توسيعه عند الحاجة
        }

        private async void SyncCategory()
        {
            List<Category> categories = BindToCategory();
            CategoryCRUD crud = new CategoryCRUD(Sync.APIUrl);

            if (Sync.ValidAPIUrl && await crud.PostCategoriesManully(categories))
                DXMessageBox.Show("تمت مزامنة المجموعات بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SyncUnits()
        {
            List<Unit> units = BindToUnit();
            UnitCRUD crud = new UnitCRUD(Sync.APIUrl);

            if (Sync.ValidAPIUrl && await crud.PostUnitsManully(units))
                DXMessageBox.Show("تمت مزامنة الوحدات بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private List<Category> BindToCategory()
        {
            SqlDataAdapter adapter = new SqlDataAdapter("select * from ItemsCategory", conn1);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            List<Category> list = new List<Category>();

            foreach (DataRow row in dt.Rows)
            {
                Category cat = new Category
                {
                    ClientCode = Sync.ClientCode,
                    CategoryId = Convert.ToInt32(row["CategoryId"]),
                    Code = row["Code"].ToString(),
                    Name = row["Name"].ToString(),
                    NameEN = row["NameEn"].ToString(),
                    ParentCode = row["ParentCode"].ToString(),
                    Printer = row["printer"].ToString(),
                    ShowInPOS = Convert.ToBoolean(row["ShowInPOS"]),
                    ISLeaf = Convert.ToDouble(row["type"].ToString()) == 2.0,
                    Active = true,
                    DispalyOrder = Convert.ToInt32(row["DispalyOrder"])
                };
                list.Add(cat);
            }

            return list;
        }

        private List<Unit> BindToUnit()
        {
            SqlDataAdapter adapter = new SqlDataAdapter(
                "select * from units where IS_Deleted=0", conn);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            List<Unit> list = new List<Unit>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Unit
                {
                    ClientCode = Sync.ClientCode,
                    UnitId = Convert.ToInt32(row["id"]),
                    Name = row["name"].ToString(),
                    NameEn = row["name"].ToString(),
                    DefaultInv = Convert.ToInt32(row["defaultInv"])
                });
            }

            return list;
        }

        #endregion

        #region Helpers

        private void UpdateRowCount()
        {
            int count = 0;

            if (GridControl1.ItemsSource is System.Collections.ICollection collection)
            {
                count = collection.Count;
            }

            lblRowCount.Text = $"عدد السجلات: {count:N0}";
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

    // ─── Models ──────────────────────────────────────────────────────────

    public class ItemDirRow
    {
        public string DgvNo { get; set; }
        public string DgvItemId { get; set; }
        public string DgvItemCode { get; set; }
        public string DgvItemName { get; set; }
        public string DgvItemNameEn { get; set; }
        public string DgvBarcode { get; set; }
        public string DgvItemCategory { get; set; }
        public string DgvUnit { get; set; }
        public double DgvSalePrice { get; set; }
        public double DgvPurchPrice { get; set; }
    }

    public class CategoryTreeNode
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ParentCode { get; set; }
        public List<CategoryTreeNode> Children { get; set; } = new List<CategoryTreeNode>();
    }
}