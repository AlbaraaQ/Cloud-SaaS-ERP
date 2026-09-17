using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using log4net;
using Newtonsoft.Json;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using Formatting = Newtonsoft.Json.Formatting;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSafeAdjust : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════
        #region Fields
        // ══════════════════════════════════════════════

        private SqlConnection conn;
        private SqlConnection conn1;

        public double InputVal;
        public double OutputVal;
        public double AverageVal;
        public double TotVal;
        public double _FirstVal;
        public double _PurchVal;
        public double _RetPurchVal;
        public double _SaleVal;
        public double _RetSaleVal;

        public int Rowindex    = -1;
        public int ProcCode    = -1;
        public int type        = 1;  // 1=إدخال, 2=عرض فقط
        private int Code       = -1;
        private string ItemCode = "-1";
        public  int SelectedId = -1;
        private int currStock  = 0;

        private InvoiceObj InvObj;
        private Print print;
        private bool Isload;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int  PrintType;
        private int  PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        private bool _Finished;

        // مصادر البيانات
        private ObservableCollection<AdjustItemRow>   ItemsList;
        private ObservableCollection<AdjustSearchRow> SearchList;

        // Timer لعرض الوقت
        private DispatcherTimer _clockTimer;

        #endregion

        // ══════════════════════════════════════════════
        #region Constructor
        // ══════════════════════════════════════════════

        public frmSafeAdjust()
        {
            InitializeComponent();

            conn       = MainClass.ConnObj();
            conn1      = MainClass.ConnObj();
            InvObj     = new InvoiceObj(7, 1);
            print      = new Print(7);
            Isload     = false;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp  = true;
            PrintNo     = 1;
            RptName     = "";
            RptUrl      = "";
            _Finished   = false;

            ItemsList  = new ObservableCollection<AdjustItemRow>();
            SearchList = new ObservableCollection<AdjustSearchRow>();

            dgvItems.ItemsSource = ItemsList;
            dgvSrch.ItemsSource  = SearchList;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window Loaded
        // ══════════════════════════════════════════════

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تهيئة الساعة
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, ev) =>
                lblTime.Text = DateTime.Now.ToString("hh:mm tt");
            _clockTimer.Start();

            if (type != 2)
            {
                LoadAdjustNo();
                LoadSafes();
                LoadGroups();
            }

            txtDate.DateTime      = DateTime.Today;
            txtFromDate.DateTime  = DateTime.Today;
            txtToDate.DateTime    = DateTime.Today;

            txtSumQCurr.Text = "0.00";
            txtsumQReal.Text = "0.00";
            txtsumInput.Text = "0.00";
            txtsumOut.Text   = "0.00";
            txtSum.Text      = "0.00";

            LoadBranches();

            cmbSatus.Items.Add("غير معتمدة");
            cmbSatus.Items.Add("معتمدة");

            if (MainClass.UserID == 0)
                BtnCancel.Visibility = Visibility.Visible;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Load Helpers
        // ══════════════════════════════════════════════

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

        public void LoadGroups()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id, name FROM ItemsCategory ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbGroup.DisplayMemberPath = "name";
            cmbGroup.SelectedValuePath  = "id";
            cmbGroup.ItemsSource        = dt.DefaultView;
            cmbGroup.SelectedIndex      = -1;
        }

        private void LoadBranches()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                var dtCopy = dt.Copy();

                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath  = "id";
                cmbBranches.ItemsSource        = dt.DefaultView;
                cmbBranches.SelectedIndex      = -1;

                cmbInvBranch.DisplayMemberPath = "name";
                cmbInvBranch.SelectedValuePath  = "id";
                cmbInvBranch.ItemsSource        = dtCopy.DefaultView;
                cmbInvBranch.SelectedValue      = MainClass.BranchNo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في LoadBranches: {ex.Message}");
            }
        }

        public void LoadAdjustNo()
        {
            try
            {
                txtNo.Text = "";
                int nextNo = 1;

                var adapter = new SqlDataAdapter(
                    "SELECT id FROM SafesAdjust", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    nextNo = dt.Rows.Count + 1;

                txtNo.Text = nextNo.ToString();
            }
            catch { /* تجاهل */ }
        }

        private string GetUnitName(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT units.name AS UnitName FROM items " +
                    $"LEFT JOIN units ON items.unit=units.Id " +
                    $"WHERE items.id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["UnitName"]?.ToString() ?? "" : "";
            }
            catch { return ""; }
        }

        private int GetUnitId(string name)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id FROM units WHERE name='{name}'", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch { return -1; }
        }

        private string GetCurrencyName(int itemId)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT name, nameEN FROM Items " +
                $"WHERE IS_Deleted=0 AND id={itemId}", conn);
            var dt = new DataTable();
            adapter.Fill(dt);
            if (dt.Rows.Count == 0) return "";
            return string.Equals(MainClass.Language, "ar",
                                  StringComparison.OrdinalIgnoreCase)
                    ? dt.Rows[0]["name"]?.ToString() ?? ""
                    : dt.Rows[0]["nameEN"]?.ToString() ?? "";
        }

        private string GetItemCode(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["Code"]?.ToString() ?? "" : "";
            }
            catch { return ""; }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CalcStock (حساب المخزون)
        // ══════════════════════════════════════════════

        public void CalcStock()
        {
            try
            {
                if (cmbSafes.SelectedIndex == -1)
                {
                    DXMessageBox.Show(
                        string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? "يجب اختيار المستودع"
                            : "Please select store",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string condBranch = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} and " : "";

                DataTable stockTable;
                int safeId = cmbSafes.SelectedValue != null
                    ? Convert.ToInt32(cmbSafes.SelectedValue) : -1;

                if (SelectedId > 0)
                {
                    stockTable = new DataTable();
                    stockTable.Columns.Add("ItemID");
                    stockTable.Columns.Add("Stock");
                    stockTable.Columns.Add("Category");

                    double stock = Inventory.CalcItemStock(
                        safeId, SelectedId, MainClass.BranchNo);
                    stockTable.Rows.Add(SelectedId, stock, 0);
                }
                else
                {
                    stockTable = Inventory.TotalItemStock(
                        safeId, txtDate.DateTime, MainClass.BranchNo).Copy();
                }

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = stockTable.Rows.Count > 0
                                        ? stockTable.Rows.Count : 1;

                double totalCost = 0.0;

                if (stockTable.Rows.Count > 0)
                {
                    for (int i = 0; i < stockTable.Rows.Count; i++)
                    {
                        int itemIdInt = Convert.ToInt32(stockTable.Rows[i][0]);
                        double stockQty = Convert.ToDouble(stockTable.Rows[i][1]);

                        // تجاهل الأصناف بكمية موجبة إذا تم اختيار "الأصناف السالبة"
                        if (chkNegItems.IsChecked == true && stockQty >= 0)
                        {
                            ProgressBar1.Value++;
                            continue;
                        }

                        // تصفية حسب المجموعة
                        if (cmbGroup.SelectedIndex != -1 &&
                            cmbGroup.SelectedValue != null)
                        {
                            var catAdapter = new SqlDataAdapter(
                                $"SELECT group_id FROM Items WHERE id={itemIdInt}",
                                conn);
                            var catDt = new DataTable();
                            catAdapter.Fill(catDt);
                            if (catDt.Rows.Count > 0 &&
                                catDt.Rows[0]["group_id"].ToString() !=
                                cmbGroup.SelectedValue.ToString())
                            {
                                ProgressBar1.Value++;
                                continue;
                            }
                        }

                        // فحص إذا الصنف محذوف
                        var itemAdapter = new SqlDataAdapter(
                            $"SELECT is_deleted, sale_price, purch_price " +
                            $"FROM Items WHERE id={itemIdInt}", conn);
                        var itemDt = new DataTable();
                        itemAdapter.Fill(itemDt);

                        if (itemDt.Rows.Count == 0 ||
                            Convert.ToBoolean(itemDt.Rows[0]["is_deleted"]))
                        {
                            ProgressBar1.Value++;
                            continue;
                        }

                        // حساب متوسط التكلفة
                        double price;
                        if (InvObj.Pricing == Pricing.Default)
                            price = ItemOper.AvgCost(itemIdInt, MainClass.BranchNo);
                        else if (InvObj.Pricing == Pricing.SalePrice)
                            price = Convert.ToDouble(itemDt.Rows[0]["purch_price"]);
                        else if (InvObj.Pricing == Pricing.PurchasePrice)
                            price = ItemOper.RecentPurchPrice(itemIdInt);
                        else
                            price = ItemOper.AvgCost(itemIdInt, MainClass.BranchNo);

                        double inQty = stockQty < 0 ? Math.Abs(stockQty) : 0;
                        double outQty = stockQty >= 0 ? stockQty : 0;

                        var row = new AdjustItemRow
                        {
                            ItemCode     = GetItemCode(itemIdInt),
                            ItemName     = GetCurrencyName(itemIdInt),
                            Unit         = GetUnitName(itemIdInt),
                            CurrentStock = Math.Round(stockQty, 2),
                            RealStock    = 0,
                            Price        = Math.Round(price, 2),
                            Note         = "",
                            ItemId       = itemIdInt,
                        };

                        // تجميد الحدث أثناء التعيين الأولي
                        row.PropertyChanged -= OnRowPropertyChanged;
                        row.InQty  = Math.Round(inQty, 2);
                        row.OutQty = Math.Round(outQty, 2);
                        row.Total  = Math.Round(price * Math.Abs(stockQty), 4);
                        row.PropertyChanged += OnRowPropertyChanged;

                        ItemsList.Add(row);
                        totalCost += row.Total;
                        ProgressBar1.Value = i + 1;
                    }
                }
                else if (SelectedId > 0)
                {
                    // إضافة صنف واحد بكمية صفر
                    double price = Convert.ToDouble(
                        Common.GetItemPrice(SelectedId));

                    ItemsList.Add(new AdjustItemRow
                    {
                        ItemCode     = GetItemCode(SelectedId),
                        ItemName     = GetCurrencyName(SelectedId),
                        Unit         = GetUnitName(SelectedId),
                        CurrentStock = 0,
                        RealStock    = 0,
                        Price        = price,
                        Note         = "",
                        ItemId       = SelectedId,
                    });
                }

                Isload    = false;
                txtSum.Text = totalCost.ToString("N2");
                SelectedId  = -1;

                CalSums();
                UpdateRowCount();
                _Finished = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ أثناء حساب المخزون:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _Finished = true;
            }
        }

        /// <summary>يُستدعى عند تغيير RealStock أو Price في صف</summary>
        private void OnRowPropertyChanged(object sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdjustItemRow.RealStock) ||
                e.PropertyName == nameof(AdjustItemRow.Price))
            {
                CalSums();
            }
        }

        public void CalSums()
        {
            try
            {
                double sumCurrent = ItemsList.Sum(x => x.CurrentStock);
                double sumReal    = ItemsList.Sum(x => x.RealStock);
                double sumIn      = ItemsList.Sum(x => x.InQty);
                double sumOut     = ItemsList.Sum(x => x.OutQty);
                double sumTotal   = ItemsList.Sum(x => x.Total);

                txtSumQCurr.Text = sumCurrent.ToString("N2");
                txtsumQReal.Text = sumReal.ToString("N2");
                txtsumInput.Text = sumIn.ToString("N2");
                txtsumOut.Text   = sumOut.ToString("N2");
                txtSum.Text      = sumTotal.ToString("N2");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في CalSums: {ex.Message}");
            }
        }

        private void UpdateRowCount()
        {
            lblTotalRows.Text = $"{ItemsList.Count} صنف";
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CLR (تفريغ الفاتورة)
        // ══════════════════════════════════════════════

        private void CLR()
        {
            ProcCode = -1;
            LoadAdjustNo();
            SelectedId = -1;
            txtBarcode.Text   = "";
            txtItemName.Text  = "";
            txtItemCode.Text  = "";
            ItemsList.Clear();
            Code = -1;
            txtSumQCurr.Text = "0.00";
            txtsumQReal.Text = "0.00";
            txtsumInput.Text = "0.00";
            txtsumOut.Text   = "0.00";
            txtSum.Text      = "0.00";
            txtDate.DateTime = DateTime.Today;
            Isload           = false;
            cmbSatus.SelectedIndex = -1;
            chkNegItems.IsChecked  = false;
            UpdateRowCount();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Item Search
        // ══════════════════════════════════════════════

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

                if (form.ISDone && form.ItemId > 0 &&
                    cmbSatus.SelectedIndex < 1)
                {
                    foreach (int itemId in form.Itemlist)
                    {
                        SelectedId       = itemId;
                        txtItemCode.Text = GetItemCode(SelectedId);
                        txtItemName.Text = form.Itemname;
                        CalcStock();
                        CalSums();
                        txtItemCode.Text = "";
                        txtItemName.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{txtItemName.Text}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 && cmbSatus.SelectedIndex < 1)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"]?.ToString() ?? "";
                    txtItemName.Text = dt.Rows[0]["name"]?.ToString() ?? "";
                    CalcStock();
                    CalSums();
                }
                else
                {
                    addNewItem();
                }
            }
            catch { addNewItem(); }
        }

        private void SearchByCode()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND Code=N'{txtItemCode.Text}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 && cmbSatus.SelectedIndex < 1)
                {
                    SelectedId       = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemCode.Text = dt.Rows[0]["Code"]?.ToString() ?? "";
                    txtItemName.Text = dt.Rows[0]["name"]?.ToString() ?? "";
                    CalcStock();
                    CalSums();
                }
                else
                {
                    addNewItem();
                }
            }
            catch { addNewItem(); }
        }

        private void ReadBarcode(string barcode, bool isMultiText)
        {
            try
            {
                int itemId = 0, unitId = 0;
                decimal itemPrice = 0, itemQty = 0;
                ItemOper.SearchForBarcode(barcode.Trim(), ref itemId,
                    ref unitId, ref itemPrice, ref itemQty, isMultiText);

                if (itemId > 0)
                {
                    SelectedId       = itemId;
                    txtItemCode.Text = "";
                    txtItemName.Text = "";
                    CalcStock();
                    CalSums();
                    txtItemCode.Text = "";
                    txtItemName.Text = "";
                }
                else
                {
                    addNewItem();
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Save / Execute / Delete / Cancel
        // ══════════════════════════════════════════════

        private void AdjustStock()
        {
            if (ItemsList.Count == 0)
            {
                DXMessageBox.Show(
                    string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "يجب إدخال أصناف للتسوية"
                        : "Please select items",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // التحقق من الوحدة
            foreach (var row in ItemsList)
            {
                if (string.IsNullOrWhiteSpace(row.Unit))
                {
                    DXMessageBox.Show("يجب تحديد الوحدة.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (conn.State != ConnectionState.Open) conn.Open();
            var tx = conn.BeginTransaction();

            try
            {
                string confirmMsg = "هل أنت متأكد من حفظ التسوية كمسودة؟";
                if (DXMessageBox.Show(confirmMsg, "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No)
                    return;

                if (Code == -1)
                {
                    LoadAdjustNo();
                    var cmd = new SqlCommand(
                        $"INSERT INTO SafesAdjust(id, date, safe, EmpId, " +
                        $"branch, Is_Approved, IS_Deleted) " +
                        $"VALUES(@id, @date, {cmbSafes.SelectedValue}, " +
                        $"{MainClass.EmpNo}, @branch, 0, 0)",
                        conn, tx);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value =
                        Convert.ToInt32(txtNo.Text);
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value =
                        txtDate.DateTime;
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value =
                        MainClass.BranchNo;
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var cmd = new SqlCommand(
                        "UPDATE SafesAdjust " +
                        "SET date=@date, safe=@safe, EmpId=@EmpId, " +
                        "branch=@branch, Is_Approved=0 " +
                        $"WHERE id={Code}", conn, tx);
                    cmd.Parameters.Add("@safe", SqlDbType.Int).Value =
                        cmbSafes.SelectedValue;
                    cmd.Parameters.Add("@EmpId", SqlDbType.Int).Value =
                        MainClass.EmpNo;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value =
                        txtDate.DateTime;
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value =
                        MainClass.BranchNo;
                    cmd.ExecuteNonQuery();

                    new SqlCommand(
                        $"DELETE FROM SafesAdjust_Sub " +
                        $"WHERE Adjust_id={Convert.ToInt32(txtNo.Text)}",
                        conn, tx).ExecuteNonQuery();
                }

                // إدراج تفاصيل الأصناف
                foreach (var item in ItemsList)
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO SafesAdjust_Sub(" +
                        "Adjust_id, ItemId, unit, InQuant, OutQuant, " +
                        "Price, Note, CurrentStock, RealStock) " +
                        "VALUES(@Adjust_id, @ItemId, @unit, @InQuant, " +
                        "@OutQuant, @Price, @Note, @CurrentStock, @RealStock)",
                        conn, tx);
                    cmd.Parameters.Add("@Adjust_id", SqlDbType.Int).Value =
                        Convert.ToInt32(txtNo.Text);
                    cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value =
                        item.ItemId;
                    cmd.Parameters.Add("@unit", SqlDbType.Int).Value =
                        GetUnitId(item.Unit);
                    cmd.Parameters.Add("@CurrentStock", SqlDbType.Float).Value =
                        item.CurrentStock;
                    cmd.Parameters.Add("@RealStock", SqlDbType.Float).Value =
                        item.RealStock;
                    cmd.Parameters.Add("@InQuant", SqlDbType.Float).Value =
                        item.InQty;
                    cmd.Parameters.Add("@OutQuant", SqlDbType.Float).Value =
                        item.OutQty;
                    cmd.Parameters.Add("@Price", SqlDbType.Float).Value =
                        item.Price;
                    cmd.Parameters.Add("@Note", SqlDbType.NVarChar).Value =
                        item.Note ?? "";
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();

                string approveMsg =
                    "تم حفظ التسوية كمسودة. هل تريد اعتماد التسوية؟";
                if (DXMessageBox.Show(approveMsg, "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    SaveAndPrint();
                }
            }
            catch (Exception ex)
            {
                tx.Rollback();
                DXMessageBox.Show(
                    $"خطأ في حفظ التسوية:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn1.State != ConnectionState.Closed) conn1.Close();
                CLR();
            }
        }

        private void ExecuteAdjustment()
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var tx = conn.BeginTransaction();

            try
            {
                new SqlCommand(
                    $"UPDATE SafesAdjust SET Is_Approved=1 " +
                    $"WHERE id={Convert.ToInt32(txtNo.Text)}",
                    conn, tx).ExecuteNonQuery();

                string invGlobalID  = "";
                int    autoIncrID   = 0;
                InvoiceOper.GetInvoiceGlobalID(ref invGlobalID, ref autoIncrID);

                // إدراج الفاتورة الأساسية
                var invCmd = new SqlCommand(
                    StoredQueries.InsertInv, conn, tx);
                invCmd.Parameters.Add("@InvGlobalID",  SqlDbType.NVarChar).Value = invGlobalID;
                invCmd.Parameters.Add("@CloudID",      SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@proc_type",    SqlDbType.Int     ).Value = 1;
                invCmd.Parameters.Add("@id",           SqlDbType.Int     ).Value = Convert.ToInt32(txtNo.Text);
                invCmd.Parameters.Add("@date",         SqlDbType.DateTime).Value = txtDate.DateTime;
                invCmd.Parameters.Add("@inv_type",     SqlDbType.Int     ).Value = 7;
                invCmd.Parameters.Add("@OrderType",    SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@safe",         SqlDbType.Int     ).Value = cmbSafes.SelectedValue;
                invCmd.Parameters.Add("@stock",        SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@cust_id",      SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@sales_emp",    SqlDbType.Int     ).Value = MainClass.EmpNo;
                invCmd.Parameters.Add("@InvTotal",     SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@AdditionsTot", SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@Insurance",    SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@tot_net",      SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@InvProfit",    SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@paid",         SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@minus",        SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@tax",          SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@EntryID",      SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@branch",       SqlDbType.Int     ).Value = MainClass.BranchNo;
                invCmd.Parameters.Add("@IS_Buy",       SqlDbType.Bit     ).Value = 0;
                invCmd.Parameters.Add("@IS_Deleted",   SqlDbType.Bit     ).Value = 0;
                invCmd.Parameters.Add("@notes",        SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@Reff_No ",     SqlDbType.VarChar ).Value = -1;
                invCmd.Parameters.Add("@Reff_date ",   SqlDbType.DateTime).Value = txtDate.DateTime;
                invCmd.Parameters.Add("@salesman",     SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@pay_type",     SqlDbType.Int     ).Value = 0;
                invCmd.Parameters.Add("@cash",         SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@visa",         SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@bank",         SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@ExtraVAT",     SqlDbType.Float   ).Value = 0;
                invCmd.Parameters.Add("@AdditionalCost",SqlDbType.Float  ).Value = 0;
                invCmd.Parameters.Add("@InvoiceStatus",SqlDbType.Int     ).Value = 3;
                invCmd.Parameters.Add("@PriceIncVAT",  SqlDbType.Bit     ).Value = 1;
                invCmd.Parameters.Add("@PaymentStatus",SqlDbType.Int     ).Value = -1;
                invCmd.Parameters.Add("@CashCustomerName",  SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@CashCustomerMobile",SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@QRCode",       SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@InvoiceHash",  SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@UUID",         SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@ZatcaSent",    SqlDbType.Bit     ).Value = false;
                invCmd.Parameters.Add("@TableNo",      SqlDbType.NVarChar).Value = "";
                invCmd.ExecuteNonQuery();

                // إدراج تفاصيل الأصناف
                foreach (var item in ItemsList)
                {
                    double inQty  = item.InQty;
                    double outQty = item.OutQty;

                    int procType;
                    double qty;

                    if (inQty > 0)      { procType = 1; qty = inQty; }
                    else if (outQty > 0){ procType = 2; qty = outQty; }
                    else continue;

                    var subCmd = new SqlCommand(
                        StoredQueries.InsertInvSub, conn, tx);
                    subCmd.Parameters.Add("@InvGlobalID",         SqlDbType.NVarChar).Value = invGlobalID;
                    subCmd.Parameters.Add("@proc_id",             SqlDbType.Int     ).Value = autoIncrID;
                    subCmd.Parameters.Add("@proc_type",           SqlDbType.Int     ).Value = procType;
                    subCmd.Parameters.Add("@ItemId",              SqlDbType.Int     ).Value = item.ItemId;
                    subCmd.Parameters.Add("@unit",                SqlDbType.Int     ).Value = Common.GetUnitID(item.Unit);
                    subCmd.Parameters.Add("@val",                 SqlDbType.Float   ).Value = qty;
                    subCmd.Parameters.Add("@val1",                SqlDbType.Float   ).Value = qty;
                    subCmd.Parameters.Add("@exchange_price",      SqlDbType.Float   ).Value = item.Price;
                    subCmd.Parameters.Add("@expire_date",         SqlDbType.DateTime).Value = DBNull.Value;
                    subCmd.Parameters.Add("@discount",            SqlDbType.Float   ).Value = 0;
                    subCmd.Parameters.Add("@taxperc",             SqlDbType.Float   ).Value = 0;
                    subCmd.Parameters.Add("@taxval",              SqlDbType.Float   ).Value = 0;
                    subCmd.Parameters.Add("@Description",         SqlDbType.NVarChar).Value = "";
                    subCmd.Parameters.Add("@Store",               SqlDbType.Float   ).Value = cmbSafes.SelectedValue;
                    subCmd.Parameters.Add("@UnitEquality",        SqlDbType.Float   ).Value = 1;
                    subCmd.Parameters.Add("@ProductId",           SqlDbType.Int     ).Value = 0;
                    subCmd.Parameters.Add("@notes",               SqlDbType.NVarChar).Value = item.Note ?? "";
                    subCmd.Parameters.Add("@AvrgCost",            SqlDbType.Float   ).Value = item.Price;
                    subCmd.Parameters.Add("@CurrentQnty",         SqlDbType.Float   ).Value = item.CurrentStock;
                    subCmd.Parameters.Add("@BranchID",            SqlDbType.Int     ).Value = MainClass.BranchNo;
                    subCmd.Parameters.Add("@ItemAddedCost",       SqlDbType.Float   ).Value = 0;
                    subCmd.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float   ).Value = item.Price;
                    subCmd.Parameters.Add("@ItemCostCenter",      SqlDbType.NVarChar).Value = "";
                    subCmd.Parameters.Add("@ItemAdditionalTax",   SqlDbType.Int     ).Value = 0;
                    subCmd.Parameters.Add("@ItemAdditionalTaxPerc",SqlDbType.Int    ).Value = 0;
                    subCmd.ExecuteNonQuery();
                }

                tx.Commit();

                DXMessageBox.Show(
                    string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "تم اعتماد التسوية"
                        : "The invoice has been approved",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                CLR();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                DXMessageBox.Show(
                    $"خطأ أثناء اعتماد التسوية:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAndPrint()
        {
            var invoiceOper = new InvoiceOper();
            var invoice     = BindToInvoice();

            if (!invoiceOper.SaveInvoice(invoice, null, IsNew: true))
                return;

            ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
            Logger.Info(
                $" تم حفظ تسوية جردية برقم {invoice.InvoiceNo} " +
                $"بواسطة المستخدم: {MainClass.UserName}");
            var Home = new Home();
            // Broker
            if (Home._is_active)
            {
                if (ConnectBroker.IsConnectedToInternet() &&
                    ConnectBroker.CheckConnectionAndBroker())
                {
                    string cond  = $"where InvGlobalID=N'{invoice.InvGlobalID}'";
                    string invGl = $"WHERE InvGlobalID=N'{invoice.InvGlobalID}'";
                    string ec2   = $"where GlobalID=N'{invoice.EntryGlobalID}'";
                    string ec3   = $"where EntryGlobalID=N'{invoice.EntryGlobalID}'";

                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "Inv",
                        Encoding.UTF8.GetBytes(SendData.GetInv(cond)), 0, true);
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "InvSub",
                        Encoding.UTF8.GetBytes(SendData.GetInvSub(invGl)), 0, true);
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "Entry",
                        Encoding.UTF8.GetBytes(SendData.GetEntryData(ec2)), 0, true);
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "EntrySub",
                        Encoding.UTF8.GetBytes(SendData.GetEntrySubData(ec3)), 0, true);
                }
                else
                {
                    SaveGlobalIDOffLine(invoice.InvGlobalID, invoice.EntryGlobalID);
                }
            }

            CLR();
        }

        private Invoice BindToInvoice()
        {
            double totalCost = 0.0;
            var items = new List<Item>();

            string entryGlobalID = "-1";

            if (ProcCode == -1)
            {
                GetInvoiceIDs();
                entryGlobalID = $"{MainClass.BranchNo}-{ProcCode}";
            }

            string invGlobalID = entryGlobalID;

            new SqlCommand(
                $"UPDATE SafesAdjust SET Is_Approved=1 " +
                $"WHERE id={Convert.ToInt32(txtNo.Text)}", conn)
                .ExecuteNonQuery();

            foreach (var row in ItemsList)
            {
                double inQty  = row.InQty;
                double outQty = row.OutQty;

                double procType, qty;
                if (inQty > 0)      { procType = 1; qty = inQty; }
                else if (outQty > 0){ procType = 2; qty = outQty; }
                else continue;

                totalCost += Math.Round(row.Price * qty, 2);

                items.Add(new Item
                {
                    InvGlobalID  = invGlobalID,
                    ClientCode   = Sync.ClientCode,
                    Name         = row.ItemName,
                    ItemNo       = row.ItemId,
                    ProcType     = (int)procType,
                    Description  = row.Note ?? "",
                    Quantity     = qty,
                    UnitEquality = 1.0,
                    PrimaryQnty  = qty,
                    Unit         = Common.GetUnitID(row.Unit),
                    Price        = row.Price,
                    AvegCost     = row.Price,
                    Vat          = 0,
                    VatPerc      = 0,
                    ItemDiscount = 0,
                    ValiableStock = row.CurrentStock,
                    Store        = Convert.ToDouble(cmbSafes.SelectedValue),
                    ExpireDate   = DateTime.Now.AddYears(1),
                    Note         = "",
                    ProductId    = 0,
                });
            }

            return new Invoice
            {
                VAT           = 0,
                Net           = 0,
                Discount      = 0,
                Additions     = 0,
                Insurance     = 0,
                Paycash       = 0,
                PayATM        = 0,
                PayType       = -1,
                Remainder     = 0,
                InvGlobalID   = invGlobalID,
                EntryGlobalID = entryGlobalID,
                InvoiceNo     = Convert.ToInt32(txtNo.Text),
                InvoiceType   = InvoiceType.InventoryCorrectionIN,
                Bank          = -1,
                ProcType      = 1,
                OrderNo       = -1,
                OrderType     = -1,
                InvDate       = txtDate.DateTime,
                User          = MainClass.EmpNo,
                Branch        = MainClass.BranchNo,
                Treasury      = MainClass.UserTreasury,
                Saleman       = -1,
                Customer      = -1,
                InvNote       = "",
                IsDeleted     = false,
                VATperc       = 0,
                ReffNo        = "-1",
                RefDate       = DateTime.Now,
                InvCombinedId = $"{MainClass.BranchCode}A1{Convert.ToInt32(txtNo.Text)}",
                Store         = Convert.ToInt32(cmbSafes.SelectedValue),
                Total         = totalCost,
                Delivery      = 0,
                SumPrice      = totalCost,
                Items         = items,
            };
        }

        private void GetInvoiceIDs()
        {
            if (conn.State != ConnectionState.Open) conn.Open();

            if (ProcCode == -1)
            {
                ProcCode = (int)Math.Round(Convert.ToDouble(
                    new SqlCommand(
                        "SELECT ISNULL(MAX(proc_id), 0) FROM Inv",
                        conn).ExecuteScalar()) + 1.0);
                Code = Convert.ToInt32(txtNo.Text);
            }
        }

        private void SaveGlobalIDOffLine(string invGlobalID, string entryGlobalID)
        {
            var item = new InvGLobalIDOff
            {
                InvGlobalID    = invGlobalID,
                GlobalID       = entryGlobalID,
                EntryGLobalID  = entryGlobalID,
            };

            var list = new List<InvGLobalIDOff>();
            string path = Path.Combine(
                System.Windows.Forms.Application.StartupPath,
                "Data", "InvGlobalID.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                    list = JsonConvert.DeserializeObject<List<InvGLobalIDOff>>(json);
            }

            list.Add(item);
            File.WriteAllText(path,
                JsonConvert.SerializeObject(list, Formatting.Indented));
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Navigate / ReadData
        // ══════════════════════════════════════════════

        public void Navigate(string sqlStr)
        {
            dgvSrch.UnselectAll();
            var cmd = new SqlCommand(sqlStr, conn);
            if (conn.State != ConnectionState.Open) conn.Open();
            ReadData(cmd.ExecuteReader());
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;

                dr.Read();
                CLR();
                Isload = true;

                Code         = Convert.ToInt32(dr["id"]);
                txtNo.Text   = Code.ToString();
                txtDate.DateTime = Convert.ToDateTime(dr["date"]);
                lblTime.Text = Convert.ToDateTime(dr["date"]).ToString("hh:mm tt");

                cmbSatus.SelectedIndex =
                    Convert.ToBoolean(dr["Is_Approved"]) ? 1 : 0;

                if (cmbSafes.SelectedValue != null)
                    cmbSafes.SelectedValue = dr["safe"].ToString();

                if (dr["branch"] != DBNull.Value &&
                    Convert.ToInt32(dr["branch"]) != -1)
                    cmbInvBranch.SelectedValue = dr["branch"];

                dr.Close();

                // تحميل الأصناف
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SafesAdjust_Sub WHERE Adjust_id={Code}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                double totCost = 0.0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    int itemId = Convert.ToInt32(row["ItemId"]);

                    double inQ    = Convert.ToDouble(row["InQuant"]);
                    double outQ   = Convert.ToDouble(row["OutQuant"]);
                    double price  = Convert.ToDouble(row["Price"]);
                    double total  = (inQ + outQ) * price;

                    var adjRow = new AdjustItemRow
                    {
                        ItemCode     = GetItemCode(itemId),
                        ItemName     = GetCurrencyName(itemId),
                        Unit         = GetUnitName(itemId),
                        CurrentStock = Convert.ToDouble(row["CurrentStock"]),
                        RealStock    = Convert.ToDouble(row["RealStock"]),
                        Price        = price,
                        Note         = row["Note"]?.ToString() ?? "",
                        ItemId       = itemId,
                    };

                    adjRow.PropertyChanged -= OnRowPropertyChanged;
                    adjRow.InQty  = inQ;
                    adjRow.OutQty = outQ;
                    adjRow.Total  = Math.Round(total, 4);
                    adjRow.PropertyChanged += OnRowPropertyChanged;

                    ItemsList.Add(adjRow);
                    totCost += adjRow.Total;
                }

                txtSum.Text = totCost.ToString("N2");
                Isload      = false;
                CalSums();
                UpdateRowCount();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Search
        // ══════════════════════════════════════════════

        private void Search()
        {
            string cond = "";

            if (chkAllBranch.IsChecked != true &&
                cmbBranches.SelectedIndex != -1 &&
                cmbBranches.SelectedValue != null)
                cond = $" SafesAdjust.branch={cmbBranches.SelectedValue} and ";

            if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                cond += $" SafesAdjust.id={txtSrchNo.Text} and ";
            else
                cond += " date>=@date1 and date<=@date2 and ";

            LoadDG(cond);
        }

        private void LoadDG(string cond)
        {
            SearchList.Clear();
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT SafesAdjust.id, SafesAdjust.date, " +
                    "SafesAdjust.Is_Approved, Safes.name AS safe, " +
                    "Employees.name AS Employ " +
                    "FROM SafesAdjust, Safes, Employees " +
                    "WHERE SafesAdjust.IS_Deleted=0 AND " + cond +
                    "SafesAdjust.safe=Safes.id AND " +
                    "SafesAdjust.EmpId=Employees.id " +
                    "ORDER BY SafesAdjust.id", conn);

                if (cond.Contains("@date"))
                {
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        txtFromDate.DateTime.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value =
                        txtToDate.DateTime.AddHours(24);
                }

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    var date = Convert.ToDateTime(row["date"]);
                    SearchList.Add(new AdjustSearchRow
                    {
                        AdjId      = Convert.ToInt32(row["id"]),
                        AdjDate    = date.ToShortDateString(),
                        AdjTime    = date.ToLongTimeString(),
                        SafeName   = row["safe"]?.ToString() ?? "",
                        EmpName    = row["Employ"]?.ToString() ?? "",
                        IsApproved = Convert.ToBoolean(row["Is_Approved"]),
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في LoadDG: {ex.Message}");
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Print
        // ══════════════════════════════════════════════

        private DataSet BindToData()
        {
            var list = ItemsList.Select(item => new InvoiceData
            {
                InvoiceType   = Title,
                InvoiceNo     = txtNo.Text,
                ItemNo        = item.ItemCode,
                ItemName      = item.ItemName,
                Unit          = item.Unit,
                CurrentQty    = item.CurrentStock.ToString("N2"),
                Quantity      = item.RealStock.ToString("N2"),
                Qty1          = item.InQty.ToString("N2"),
                Price         = item.Price.ToString("N2"),
                Total         = item.Total.ToString("N2"),
                Description   = item.Note,
                Store         = cmbSafes.Text,
                InvDate       = txtDate.DateTime.ToShortDateString(),
                sum           = txtSum.Text,
                BranchName    = cmbInvBranch.Text,
                User          = Common.GetEmpName(MainClass.EmpNo),
            }).ToList();

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            RptUrl     = MainClass.ReportsPath;
            RptName    = "rptSafeAdjust.repx";
            defPrinter = MainClass.ReportsPrinter;

            if (ItemsList.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string full = Path.Combine(RptUrl, RptName);
            if (string.IsNullOrEmpty(RptUrl) ||
                !Directory.Exists(RptUrl)    ||
                !File.Exists(full))
            {
                DXMessageBox.Show("مسار التقرير غير موجود.", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var report = XtraReport.FromFile(full);
            report.DataSource = BindToData();

            string hPath = Path.Combine(RptUrl, "header.repx");
            if (File.Exists(hPath))
            {
                var hRpt = XtraReport.FromFile(hPath);
                hRpt.DataSource = Common.FoundationInfoDT;
                var hSub = (XRSubreport)report.FindControl(
                               "headerRpt", ignoreCase: true);
                if (hSub != null) hSub.ReportSource = hRpt;
            }

            string fPath = Path.Combine(RptUrl, "footer.repx");
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

            if (printType == 1)
                for (int i = 1; i <= PrintNo; i++) report.Print();
            else
                report.ShowPreviewDialog();

            report.Dispose();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Button Events
        // ══════════════════════════════════════════════

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            cmbSafes.SelectedIndex = -1;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSatus.SelectedIndex == 1)
            {
                DXMessageBox.Show(
                    "لا يمكن تعديل تسوية معتمدة مسبقاً.", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AdjustStock();
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ItemsList.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد أصناف للتسوية.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT id FROM SafesAdjust " +
                    $"WHERE id={Convert.ToInt32(txtNo.Text)} " +
                    $"AND Is_Approved=0 AND IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1)
                {
                    DXMessageBox.Show(
                        "التسوية تم اعتمادها أو أن التسوية غير محفوظة.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من اعتماد التسوية؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    SaveAndPrint();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var tx = conn.BeginTransaction();
            try
            {
                if (DXMessageBox.Show("هل أنت متأكد من حذف التسوية؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No || Code == -1)
                    return;

                new SqlCommand(
                    $"UPDATE SafesAdjust SET Is_Deleted=1 WHERE id={Code}",
                    conn, tx).ExecuteNonQuery();

                new SqlCommand(
                    $"DELETE FROM SafesAdjust_Sub WHERE Adjust_id={Code}",
                    conn, tx).ExecuteNonQuery();

                new SqlCommand(
                    $"UPDATE Inv SET Is_Deleted=1 " +
                    $"WHERE inv_type=7 AND proc_type=1 AND id={Code}",
                    conn, tx).ExecuteNonQuery();

                new SqlCommand(
                    $"DELETE FROM Inv_Sub WHERE InvGlobalID IN " +
                    $"(SELECT InvGlobalID FROM Inv " +
                    $"WHERE inv_type=7 AND id={Code})",
                    conn, tx).ExecuteNonQuery();

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(
                    $" تم حذف تسوية جردية برقم: {Code} " +
                    $"بواسطة المستخدم: {MainClass.UserName}");

                DXMessageBox.Show("تم حذف التسوية بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                tx.Commit();
                CLR();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var tx = conn.BeginTransaction();
            try
            {
                if (DXMessageBox.Show("هل أنت متأكد من إلغاء اعتماد التسوية؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No || Code == -1)
                    return;

                new SqlCommand(
                    $"UPDATE SafesAdjust SET Is_Approved=0 WHERE id={Code}",
                    conn, tx).ExecuteNonQuery();
                new SqlCommand(
                    $"UPDATE Inv SET Is_Deleted=1 " +
                    $"WHERE inv_type=7 AND proc_type=1 AND id={Code}",
                    conn, tx).ExecuteNonQuery();
                new SqlCommand(
                    $"DELETE FROM Inv_Sub WHERE InvGlobalID IN " +
                    $"(SELECT InvGlobalID FROM Inv " +
                    $"WHERE inv_type=7 AND id={Code})",
                    conn, tx).ExecuteNonQuery();

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info(
                    $" تم إلغاء اعتماد تسوية جردية برقم: {Code} " +
                    $"بواسطة المستخدم: {MainClass.UserName}");

                DXMessageBox.Show("تم إلغاء اعتماد التسوية بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                tx.Commit();
                CLR();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ItemsList.Clear();
                CalcStock();
                CalSums();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnSrch_Click(object sender, RoutedEventArgs e)
            => addNewItem();

        private void btnSearch_Click(object sender, RoutedEventArgs e)
            => Search();

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DXMessageBox.Show("هل أنت متأكد من استيراد البيانات؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                if (cmbSafes.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار المستودع.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var importForm = new frmImportDataGeneral();
                importForm.cmbInv.SelectedIndex = 4;
                importForm.cmbInv.Visibility = Visibility.Visible;
                importForm.cmbInv.IsEnabled       = false;
                importForm.cmbDataTable.Visibility = Visibility.Collapsed;
                importForm.ShowDialog();

                if (importForm.ISDone &&
                    importForm.dtIniRes.Rows.Count > 0)
                {
                    foreach (DataRow row in importForm.dtIniRes.Rows)
                    {
                        txtItemCode.Text = row["ItemCode"]?.ToString() ?? "";
                        SearchByCode();
                    }
                }

                DXMessageBox.Show("تم الاستيراد بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AdjustItemRow row)
            {
                ItemsList.Remove(row);
                CalSums();
                UpdateRowCount();
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region DataGrid Events
        // ══════════════════════════════════════════════

        private void dgvItems_CellEditEnding(object sender,
                                              DataGridCellEditEndingEventArgs e)
        {
            if (cmbSatus.SelectedIndex == 1 || Isload) return;
            CalSums();
        }

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is AdjustSearchRow row)
            {
                Code = row.AdjId;
                Navigate($"SELECT * FROM SafesAdjust WHERE id={Code}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region ComboBox Events
        // ══════════════════════════════════════════════

        private void cmbSafes_SelectionChanged(object sender,
                                                SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbSafes.SelectedValue != null &&
                    !cmbSafes.SelectedValue.Equals(-1))
                {
                    LoadGroups();
                    txtGroupNo.Text = "";
                    ItemsList.Clear();
                    UpdateRowCount();
                }
            }
            catch { /* تجاهل */ }
        }

        private void cmbGroup_SelectionChanged(object sender,
                                                SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex >= 0)
                {
                    txtGroupNo.Text  = cmbGroup.SelectedValue?.ToString() ?? "";
                    SelectedId       = -1;
                    txtItemName.Text = "";
                    ItemsList.Clear();
                    UpdateRowCount();
                }
            }
            catch { /* تجاهل */ }
        }

        private void txtGroupNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtGroupNo.Text))
                    cmbGroup.SelectedValue = txtGroupNo.Text;
            }
            catch { /* تجاهل */ }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region TextBox Events
        // ══════════════════════════════════════════════

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtItemName.Text))
                SearchByName();
        }

        private void txtItemCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtItemCode.Text))
                SearchByCode();
        }

        #endregion

        // ══════════════════════════════════════════════
        #region CheckBox Events
        // ══════════════════════════════════════════════

        private void chkAllBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranch.IsChecked != true;
        }

        private void chkNegItems_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isNeg = chkNegItems.IsChecked == true;
            txtItemName.IsEnabled = !isNeg;
            txtItemCode.IsEnabled = !isNeg;
            txtBarcode.IsEnabled  = !isNeg;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Window KeyDown (Barcode)
        // ══════════════════════════════════════════════

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                txtBarcode.IsFocused &&
                !string.IsNullOrWhiteSpace(txtBarcode.Text))
            {
                if (cmbSafes.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار المستودع.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBarcode.Text = "";
                    return;
                }

                ReadBarcode(txtBarcode.Text, false);
                txtBarcode.Text = "";
            }
        }

        #endregion
    }
}