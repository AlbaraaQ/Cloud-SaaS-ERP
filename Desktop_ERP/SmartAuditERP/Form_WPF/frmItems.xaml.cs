using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AuditorAPI.Models;
using ETA_Invoice.Models;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using SmartAuditERP.Form_WPF;
using UtilitiesProj;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using CheckBox = System.Windows.Controls.CheckBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using DevExpress.Xpf.Core;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItems : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        public int ItemId;
        private int defVAT;
        private int tax;
        public bool IsClosed;
        private int RowIndex;
        private int SelectedId;
        public string SrchName;
        private bool ISNew;
        public int EntryType;
        public int InvType;
        public int ProcType;

        private List<UnitDgv> listItemUnit;
        private ObservableCollection<UnitDgvRow> unitGridSource;
        private ObservableCollection<SafeBalanceRow> safeBalanceSource;
        private ObservableCollection<DgvFirstRow> dgvFirstSource;
        private ObservableCollection<ComponentRow> componentSource;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        private Print print;
        private InvoiceObj InvObj;
        public int ProcCode;

        private double _Exchangeal;
        private bool _purch1;
        private bool _purch2;
        private bool _sale1;
        private bool _sale2;
        private int unitRow;
        private int firstRow;

        // NumericUpDown replacement
        private decimal _withholdingTaxValue = 0m;

        #endregion

        #region Constructor

        public frmItems()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            ItemId = -1;
            defVAT = 0;
            tax = 0;
            IsClosed = false;
            RowIndex = -1;
            SelectedId = -1;
            SrchName = "";
            ISNew = true;
            InvType = 9;
            ProcType = 1;

            listItemUnit = new List<UnitDgv>();
            unitGridSource = new ObservableCollection<UnitDgvRow>();
            safeBalanceSource = new ObservableCollection<SafeBalanceRow>();
            dgvFirstSource = new ObservableCollection<DgvFirstRow>();
            componentSource = new ObservableCollection<ComponentRow>();

            print = new Print(InvType);
            InvObj = new InvoiceObj(InvType, ProcType);

            ProcCode = -1;
            _Exchangeal = 1250.0;
            _purch1 = true;
            _purch2 = false;
            _sale1 = true;
            _sale2 = false;
            unitRow = -1;
            firstRow = -1;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ETA visibility
            if (EtaSetting.Active)
            {
                PanelEta.Visibility = Visibility.Visible;
            }

            LoadNextNo();
            LoadSafes();
            LoadGroups();
            LoadTaxGroups();
            LoadUnits();
            LoadItemsType();

            // bind grids
            GridControl1.ItemsSource = unitGridSource;
            dgvSafeBalance.ItemsSource = safeBalanceSource;
            dgvFirst.ItemsSource = dgvFirstSource;
            dgvComponent.ItemsSource = componentSource;

            // set unit combobox default
            SetComboDefault(cmbUnit, 1);
            SetComboDefault(cmbItemType, 1);

            // Sale price labels
            bool priceIncVAT = false;
            bool priceIncVAT2 = false;
            double taxPerc = 15.0;
            InvoiceOper.InvoiceProperties(2, 1, ref priceIncVAT, ref taxPerc);
            InvoiceOper.InvoiceProperties(3, 1, ref priceIncVAT2, ref taxPerc);

            string saleLbl, saleLbl2;
            if (priceIncVAT || priceIncVAT2)
            {
                saleLbl = (MainClass.Language == "ar") ? "سعر البيع" : "Sale price";
                saleLbl2 = (MainClass.Language == "ar") ? "غير شامل الضريبة\nسعر البيع"
                                                        : "Sale price\nnot incl tax";
            }
            else
            {
                saleLbl2 = (MainClass.Language == "ar") ? "سعر البيع\nشامل الضريبة"
                                                        : "Sale price\nincl Tax";
                saleLbl = (MainClass.Language == "ar") ? "سعر البيع" : "Sale price";
            }
            lblSalePrice.Text = saleLbl;
            lblSalePrice2.Text = saleLbl2;

            if (ItemId > 0)
                Navigate("select * from Items where id=" + ItemId);

            this.WindowState = MainClass.Window_State;
            var Home = new Home();
            if (Home._is_active && !ConnectBroker.CheckConnectionAndBroker())
            {
                DXMessageBox.Show("لا يمكنك إضافة أصناف \r\n يرجى الإتصال بالانترنت أولآ وإعادة تشغيل البرنامج",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            cmbGroups.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11 && Sync.ValidAPIUrl)
            {
                if (DXMessageBox.Show("هل تريد تحديث بيانات الأصناف", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new ItemOper().ReadProductsOnline();
                }
            }

            if (e.Key == Key.F6)
            {
                if (cmbGroups.SelectedIndex > -1 &&
                    !string.IsNullOrWhiteSpace(txtName.Text) &&
                    !string.IsNullOrWhiteSpace(cmbUnit.Text) &&
                    !string.IsNullOrWhiteSpace(txtSalePrice.Text))
                {
                    btnSave_Click(sender, e);
                }
                else
                {
                    if (cmbGroups.SelectedIndex < 0) { cmbGroups.Focus(); return; }
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { txtName.Focus(); return; }
                    if (string.IsNullOrWhiteSpace(cmbUnit.Text)) { cmbUnit.Focus(); return; }
                    txtSalePrice.Focus();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosed = true;
        }

        #endregion

        #region CLR / Reset

        private void CLR()
        {
            LoadNextNo();
            ISNew = true;

            txtBarcode.Text = "";
            txtName.Text = "";
            txtNameEN.Text = "";
            txtProfitPer.Text = "";
            txtPurchPrice.Text = "";
            txtSalePrice.Text = "";
            txtCompetitorPrice.Text = "0";
            txtCostAvrg.Text = "0";
            txtLastSalePrice.Text = "0";
            txtRecentPurchPrice.Text = "0";
            txtLimit.Text = "0";
            txtDiscount.Text = "";
            txtItemCode.Text = "";
            txtMaxQtyLimit.Text = "";
            txtMaxDicountParcent.Text = "0";
            txtMaxDicAmount.Text = "0";
            txtConsumerPrice.Text = "0";
            txtWholesalePrice.Text = "0";
            txtSalePrice2.Text = "0";
            TxtItemSort.Text = "0";
            txtEgyCode.Text = "";
            txtWithholdingTaxBox.Text = "0";
            _withholdingTaxValue = 0m;
            TextBox1.Text = "";
            txtTotal.Text = "";

            SetComboDefault(cmbUnit, 1);
            SetComboDefault(cmbItemType, 1);
            cmbGroups.SelectedIndex = -1;
            cmbItemProperties.SelectedIndex = -1;
            cmbEgyCodeType.SelectedIndex = -1;
            cmbProductionStore.SelectedIndex = -1;

            ckNoTax.IsChecked = false;
            clkShowInPOS.IsChecked = true;
            chkInAndroid.IsChecked = false;
            chkExtraTax.IsChecked = false;
            PnlProperty.Visibility = Visibility.Collapsed;

            picImage.Source = null;

            dgvFirstSource.Clear();
            safeBalanceSource.Clear();
            componentSource.Clear();
            listItemUnit.Clear();
            unitGridSource.Clear();

            Code = -1;
            ProcCode = -1;
            RowIndex = -1;
            SelectedId = -1;
            ItemId = -1;
            SrchName = "";

            TabControl1.SelectedIndex = 0;

            try
            {
                if (cmbGroups.SelectedValue != null &&
                    int.TryParse(cmbGroups.SelectedValue.ToString(), out int grpId))
                {
                    GenerateCode(GetGroupCode(grpId));
                }
            }
            catch { }
        }

        #endregion

        #region Load Data

        private void LoadNextNo()
        {
            try
            {
                int nextId = GetItemId();
                int nextSort = GetItemSort();
                txtNo.Text = nextId.ToString();
                txtItemCode.Text = nextId.ToString();
                TxtItemSort.Text = nextSort.ToString();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetItemId()
        {
            using var sqlConn = MainClass.ConnObj();
            if (sqlConn.State != ConnectionState.Open) sqlConn.Open();

            int count = Convert.ToInt32(
                new SqlCommand("Select COUNT(*) from Items", sqlConn).ExecuteScalar());
            int id = count + (MainClass.BranchNo * 10000) + 1;

            while (true)
            {
                int existing = Convert.ToInt32(
                    new SqlCommand($"Select COUNT(*) from Items where id={id}", sqlConn)
                    .ExecuteScalar());
                if (existing == 0) break;
                id++;
            }
            return id;
        }

        private int GetItemSort()
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var cmd = new SqlCommand("Select max(Item_Sort) as Item_Sort from Items", conn);
            var val = cmd.ExecuteScalar();
            if (val != DBNull.Value && val != null)
                return (int)Math.Round(Convert.ToDouble(val)) + 1;
            return 1;
        }

        private void LoadUnits()
        {
            try
            {
                var adapter = new SqlDataAdapter("select id,name from units order by id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbUnit.ItemsSource = dt.DefaultView;
                cmbUnit.DisplayMemberPath = "name";
                cmbUnit.SelectedValuePath = "id";
                cmbUnit.SelectedIndex = -1;

                cmbUnit1.ItemsSource = dt.DefaultView;
                cmbUnit1.DisplayMemberPath = "name";
                cmbUnit1.SelectedValuePath = "id";
                cmbUnit1.SelectedIndex = -1;

                // For unit grid comboboxes – store in Tag
                GridControl1.Tag = dt;

                // For component grid unit column
                dgvComponent.Tag = dt;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadItemsType()
        {
            try
            {
                var adapter = new SqlDataAdapter("select * from ItemsType order by id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbItemType.ItemsSource = dt.DefaultView;
                cmbItemType.DisplayMemberPath = "name";
                cmbItemType.SelectedValuePath = "id";
                cmbItemType.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSafes()
        {
            try
            {
                var dt = LoadData.Invertories(1);

                // dgvSafeBalance – ComboBox column (StoreName = display)
                // dgvFirst – ComboBox column
                // dgvComponent store column
                // cmbProductionStore

                cmbProductionStore.ItemsSource = dt.DefaultView;
                cmbProductionStore.DisplayMemberPath = "name";
                cmbProductionStore.SelectedValuePath = "id";
                cmbProductionStore.SelectedIndex = -1;

                // Store reference for runtime use
                dgvFirst.Tag = dt;
                dgvComponent.Resources["SafesDt"] = dt;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadGroups()
        {
            try
            {
                string sql = (MainClass.Language == "ar")
                    ? "select id,name from ItemsCategory where type=2 order by id"
                    : "Select id, Case WHEN len(nameEn)=0 then [name] ELSE nameEn END AS name from ItemsCategory";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbGroups.ItemsSource = dt.DefaultView;
                cmbGroups.DisplayMemberPath = "name";
                cmbGroups.SelectedValuePath = "id";
                cmbGroups.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadTaxGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select MainVAT from SettingGeneral where Inv_Id=2", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    defVAT = Convert.ToInt32(dt.Rows[0]["MainVAT"]);
            }
            catch { }
        }

        private void LoadPrices()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select ISNULL(low_sale_price,0) low_sale_price," +
                    $"ISNULL(CompetitorPrice,0) CompetitorPrice," +
                    $"ISNULL(ConsumerPrice,0) ConsumerPrice," +
                    $"ISNULL(WholesalePrice,0) WholesalePrice " +
                    $"from ItemPrices where ItemID={txtNo.Text} " +
                    $"order by Proc_id desc", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text = dt.Rows[0]["low_sale_price"].ToString();
                    txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"].ToString();
                    txtWholesalePrice.Text = dt.Rows[0]["WholesalePrice"].ToString();
                    txtConsumerPrice.Text = dt.Rows[0]["ConsumerPrice"].ToString();
                }
            }
            catch { }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var cmd = new SqlCommand(sqlStr, conn);
                using var dr = cmd.ExecuteReader();
                ReadData(dr);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate("select top 1 * from Items where IS_Deleted=0 order by id asc");

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate("select top 1 * from Items where IS_Deleted=0 order by id desc");

        private void btnNext_Click(object sender, RoutedEventArgs e) =>
            Navigate($"select top 1 * from Items where IS_Deleted=0 and id>{Code} order by id asc");

        private void btnPrevious_Click(object sender, RoutedEventArgs e) =>
            Navigate($"select top 1 * from Items where IS_Deleted=0 and id<{Code} order by id desc");

        #endregion

        #region ReadData

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;
                dr.Read();
                CLR();
                ISNew = false;

                Code = Convert.ToInt32(dr["id"]);
                txtNo.Text = dr["id"].ToString();

                txtName.Text = dr["name"].ToString();
                txtItemCode.Text = dr["Code"]?.ToString() ?? "";

                try { txtNameEN.Text = dr["nameEN"]?.ToString() ?? ""; } catch { }
                txtBarcode.Text = dr["barcode"]?.ToString() ?? "";

                try { SetComboValue(cmbUnit, dr["unit"]); } catch { }

                try
                {
                    txtPurchPrice.Text = FormatPrice(dr["purch_price"]);
                    txtSalePrice.Text = FormatPrice(dr["sale_price"]);
                }
                catch { }

                try { txtDiscount.Text = dr["discount"]?.ToString() ?? ""; } catch { }

                // صورة
                if (dr["image"] != DBNull.Value && dr["image"] != null)
                {
                    var arr = (byte[])dr["image"];
                    picImage.Source = ByteArrayToBitmapImage(arr);
                }
                else picImage.Source = null;

                // ضريبة
                try { tax = Convert.ToInt32(dr["tax"]); } catch { }
                ckNoTax.IsChecked = (tax == 0);

                txtLimit.Text = dr["limit"]?.ToString() ?? "0";
                clkShowInPOS.IsChecked = Convert.ToBoolean(dr["ShowInPOS"]);

                chkInAndroid.IsChecked = (dr["showInAndroid"] != DBNull.Value)
                    && Convert.ToBoolean(dr["showInAndroid"]);

                // EgyItemCode
                txtEgyCode.Text = (dr["EgyItemCode"] != DBNull.Value)
                    ? dr["EgyItemCode"].ToString() : "";

                // WithholdingTax
                if (dr["WithholdingTax"] != DBNull.Value)
                {
                    _withholdingTaxValue = Convert.ToDecimal(dr["WithholdingTax"]);
                    txtWithholdingTaxBox.Text = _withholdingTaxValue.ToString();
                }

                // EgyCodeType
                if (dr["EgyCodeType"] != DBNull.Value)
                {
                    string ct = dr["EgyCodeType"].ToString().Trim();
                    if (ct == "EGS") cmbEgyCodeType.SelectedIndex = 0;
                    else if (ct == "GS1") cmbEgyCodeType.SelectedIndex = 1;
                }

                // Item_Sort
                TxtItemSort.Text = (dr["Item_Sort"] != DBNull.Value)
                    ? Convert.ToDouble(dr["Item_Sort"]).ToString() : "0";

                // ItemType
                try
                {
                    if (dr["Itemtype"] != DBNull.Value)
                        SetComboValue(cmbItemType, dr["Itemtype"]);
                    else
                        SetComboDefault(cmbItemType, 1);
                }
                catch { }

                // ItemProperty
                try
                {
                    if (dr["ItemProperty"] != DBNull.Value)
                    {
                        int propIdx = (int)Math.Round(Convert.ToDouble(dr["ItemProperty"]));
                        cmbItemProperties.SelectedIndex = propIdx;

                        if (dr["FillValue"] != DBNull.Value)
                            txtFillVal.Text = Convert.ToDouble(dr["FillValue"]).ToString();

                        if (dr["Wscale"] != DBNull.Value)
                        {
                            double wscale = Convert.ToDouble(dr["Wscale"]);
                            if (wscale == 1) ckWeightScale.IsChecked = true;
                            else if (wscale == 3) chksalWeightScale.IsChecked = true;
                            else ckScalePrice.IsChecked = true;
                        }
                    }
                }
                catch { }

                // MaxQtyLimit / discounts
                txtMaxQtyLimit.Text = (dr["MaxQtyLimit"] != DBNull.Value) ? dr["MaxQtyLimit"].ToString() : "";
                txtMaxDicountParcent.Text = (dr["MaxDicountParcent"] != DBNull.Value) ? dr["MaxDicountParcent"].ToString() : "0";
                txtMaxDicAmount.Text = (dr["MaxDiscountAmount"] != DBNull.Value) ? dr["MaxDiscountAmount"].ToString() : "0";

                if (dr["is_extra_tax_applied"] != DBNull.Value)
                    chkExtraTax.IsChecked = Convert.ToBoolean(dr["is_extra_tax_applied"]);

                // cmbGroups
                try { SetComboValue(cmbGroups, dr["group_id"]); } catch { }

                dr.Close();

                // وحدات إضافية
                LoadItemUnits();

                // مكونات
                LoadItemComponents();

                CalcTot();
                CalcStock();

                int itemIdVal = (int)Math.Round(Convert.ToDouble(txtNo.Text));
                txtCostAvrg.Text = ItemOper.AvgCost(itemIdVal, MainClass.BranchNo).ToString(Common.DigitsNo);
                txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(itemIdVal).ToString();

                LoadPrices();
                txtSrchBarcode.IsReadOnly = true;
                CalcPrice(1);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadItemUnits()
        {
            listItemUnit.Clear();
            unitGridSource.Clear();

            var adapter = new SqlDataAdapter(
                $"select * from ItemUnits where ItemId={txtNo.Text} and unit <> {cmbUnit.SelectedValue}",
                conn1);
            var dt = new DataTable();
            adapter.Fill(dt);

            int idx = 0;
            foreach (DataRow row in dt.Rows)
            {
                idx++;
                var u = new UnitDgv
                {
                    DgvNo = idx,
                    DgvUnit = Convert.ToInt32(row["unit"]),
                    DgvUnitEquality = Convert.ToDouble(row["perc"]),
                    DgvUnitPurchasePrice = Convert.ToDouble(row["purch"]),
                    DgvUnitSalePrice = Convert.ToDouble(row["sale"]),
                    DgvUnitBarcode = row["barcode"].ToString()
                };
                listItemUnit.Add(u);
                unitGridSource.Add(new UnitDgvRow
                {
                    DgvNo = u.DgvNo,
                    DgvUnit = u.DgvUnit,
                    DgvUnitName = GetUnitName(u.DgvUnit),
                    DgvUnitEquality = u.DgvUnitEquality,
                    DgvUnitPurchasePrice = u.DgvUnitPurchasePrice,
                    DgvUnitSalePrice = u.DgvUnitSalePrice,
                    DgvUnitBarcode = u.DgvUnitBarcode
                });
            }
        }

        private void LoadItemComponents()
        {
            componentSource.Clear();

            var adapter = new SqlDataAdapter(
                "Select ItemComponents.Id, ItemComponents.ComponentId, ItemComponents.store," +
                " ItemComponents.itemId, ItemComponents.quantity, ItemComponents.price," +
                " ItemComponents.total, ItemComponents.type, ItemComponents.unit as unit_id," +
                " items.id as item_ID, items.Code as item_Code, items.name as Item_name" +
                " From ItemComponents left join items on ItemComponents.ComponentId=items.id" +
                $" where itemId={Convert.ToDouble(txtNo.Text)}", conn1);
            var dt = new DataTable();
            adapter.Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToDouble(row["ComponentId"]) <= 0) continue;

                int unitId = (row["unit_id"] != DBNull.Value && Convert.ToDouble(row["unit_id"]) > 0)
                    ? Convert.ToInt32(row["unit_id"]) : 1;
                int storeId = (row["store"] != DBNull.Value && Convert.ToDouble(row["store"]) > 0)
                    ? Convert.ToInt32(row["store"]) : 1;

                componentSource.Add(new ComponentRow
                {
                    RowNo = componentSource.Count + 1,
                    ItemCode = row["item_Code"].ToString(),
                    ItemId = Convert.ToInt32(row["ComponentId"]),
                    ItemName = row["Item_name"].ToString(),
                    UnitId = unitId,
                    UnitName = GetUnitName(unitId),
                    Quantity = Convert.ToDouble(row["quantity"]),
                    Price = Convert.ToDouble(row["price"]),
                    Total = Convert.ToDouble(row["total"]),
                    StoreCompId = storeId,
                    StoreCompName = GetStoreName(storeId),
                    IsAdded = Convert.ToBoolean(row["type"])
                });
            }
        }

        #endregion

        #region Save

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!User.EditItemInfo)
            {
                DXMessageBox.Show("لا يوجد لديك صلاحية هذه العملية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Sync.SyncType == 4)
            {
                DXMessageBox.Show("لا يمكن إضافة او تعديل المادة من هذا الفرع", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Sync.ActiveSync && Sync.SyncType > 0 && !MainClass.CheckForInternetConnection())
            {
                DXMessageBox.Show(MainClass.Language == "ar"
                    ? "لا يمكنك إضافة أصناف \r\n يرجى الإتصال بالانترنت أولآ"
                    : "Please connect to internet",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var Home = new Home();
            if (Home._is_active && !ConnectBroker.CheckConnectionAndBroker())
            {
                DXMessageBox.Show("لا يمكنك إضافة أصناف \r\n يرجى الإتصال بالانترنت أولآ",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Validate barcode
                if (!string.IsNullOrEmpty(txtBarcode.Text) &&
                    !ISValidBarcode(txtBarcode.Text, GetSelectedIntValue(cmbUnit)))
                {
                    txtBarcode.Focus(); return;
                }

                // Validate unit barcodes
                foreach (var u in listItemUnit)
                {
                    if (!ISValidBarcode(u.DgvUnitBarcode, u.DgvUnit))
                    {
                        TabControl1.SelectedIndex = 1; return;
                    }
                }

                // Validate dgvFirst
                if (dgvFirstSource.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(txtPurchPrice.Text))
                    {
                        DXMessageBox.Show(MainClass.Language == "ar"
                            ? "ادخل سعر الشراء" : "Enter purchase price", "",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPurchPrice.Focus(); return;
                    }

                    foreach (var row in dgvFirstSource)
                    {
                        if (row.Quantity == 0)
                        {
                            DXMessageBox.Show("يجب إدخال كمية بضاعة المدة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            TabControl1.SelectedIndex = 2; return;
                        }
                        if (double.TryParse(txtCostAvrg.Text, out double avgCost))
                            row.TotalCost = avgCost * row.Quantity;
                    }
                }

                // Check duplicate name
                var nameAdapter = new SqlDataAdapter(
                    "select id from Items where name=@ItemName and id<>@id", conn);
                nameAdapter.SelectCommand.Parameters.AddWithValue("@ItemName", txtName.Text);
                nameAdapter.SelectCommand.Parameters.AddWithValue("@id", Code);
                var nameDt = new DataTable();
                nameAdapter.Fill(nameDt);
                if (nameDt.Rows.Count > 0)
                {
                    var res = DXMessageBox.Show(
                        MainClass.Language == "ar" ? "اسم الصنف تم ادخاله مسبقا" : "Name is previously inserted",
                        "تنبيه", MessageBoxButton.YesNoCancel);
                    if (res != MessageBoxResult.Yes) { txtName.Focus(); return; }
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(txtNo.Text))
                {
                    DXMessageBox.Show(MainClass.Language == "ar" ? "ادخل رقم الصنف" : "Enter item No.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Code == -1)
                {
                    var idAdapter = new SqlDataAdapter("select id from Items where id=@id", conn);
                    idAdapter.SelectCommand.Parameters.AddWithValue("@id", txtNo.Text);
                    var idDt = new DataTable();
                    idAdapter.Fill(idDt);
                    if (idDt.Rows.Count > 0)
                    {
                        DXMessageBox.Show(MainClass.Language == "ar"
                            ? "رقم الصنف تم ادخاله مسبقا" : "item No. is previously inserted",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Check code duplicate
                var codeAdapter = new SqlDataAdapter(
                    "select id from Items where Code=@Code and IS_Deleted=0 and id<>@id", conn);
                codeAdapter.SelectCommand.Parameters.AddWithValue("@Code", txtItemCode.Text);
                codeAdapter.SelectCommand.Parameters.AddWithValue("@id", Code);
                var codeDt = new DataTable();
                codeAdapter.Fill(codeDt);
                if (codeDt.Rows.Count > 0)
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "رمز الصنف تم ادخاله مسبقا" : "item code is previously inserted",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbGroups.SelectedValue == null)
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "اختر مجموعة الصنف" : "choose the item group",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbGroups.Focus(); return;
                }

                if (cmbUnit.SelectedValue == null)
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "اختر الوحدة الافتراضية" : "choose the default unit",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbUnit.Focus(); return;
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show(MainClass.Language == "ar" ? "ادخل اسم الصنف" : "Enter item name",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus(); return;
                }

                if (string.IsNullOrWhiteSpace(txtSalePrice.Text))
                {
                    DXMessageBox.Show(MainClass.Language == "ar" ? "ادخل سعر البيع" : "Enter sale price",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSalePrice.Focus(); return;
                }

                if (string.IsNullOrWhiteSpace(txtPurchPrice.Text))
                    txtPurchPrice.Text = "0";
                if (string.IsNullOrWhiteSpace(txtMaxQtyLimit.Text))
                    txtMaxQtyLimit.Text = "0";

                // Build SQL
                if (conn.State != ConnectionState.Open) conn.Open();
                var transaction = conn.BeginTransaction();

                int showInPOS = (clkShowInPOS.IsChecked == true) ? 1 : 0;
                int showAndroid = (chkInAndroid.IsChecked == true) ? 1 : 0;
                int itemTypeVal = GetSelectedIntValue(cmbItemType, 1);
                int propIdx = cmbItemProperties.SelectedIndex > 0 ? cmbItemProperties.SelectedIndex : 0;

                int wscaleVal = 0;
                if (propIdx == 1)
                {
                    if (ckWeightScale.IsChecked == true) wscaleVal = 1;
                    else if (chksalWeightScale.IsChecked == true) wscaleVal = 3;
                    else wscaleVal = 2;
                }

                double fillVal = 0.0;
                if (propIdx == 2)
                {
                    if (!double.TryParse(txtFillVal.Text, out fillVal) || fillVal == 0)
                    {
                        DXMessageBox.Show(MainClass.Language == "ar"
                            ? "ادخل قيمة التعبئة" : "Enter Filling Value",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtFillVal.Focus(); return;
                    }
                }

                int storeVal = 1;
                if (cmbProductionStore.SelectedIndex > -1 && cmbProductionStore.SelectedValue != null)
                    int.TryParse(cmbProductionStore.SelectedValue.ToString(), out storeVal);

                int taxGroupVal = 1;
                int taxVal = (defVAT == 0) ? 15 : defVAT;
                if (ckNoTax.IsChecked == true) { taxVal = 0; taxGroupVal = 0; }

                bool isExtraTax = chkExtraTax.IsChecked == true;

                string insertSql =
                    "insert into Items(id,Code,GrpCode,name,nameEN,barcode,group_id,unit,purch_price,sale_price," +
                    "limit,discount,tax_group,tax,IS_Deleted,image,ShowInPOS,Wscale,store,ItemType,ItemProperty," +
                    "FillValue,MaxQtyLimit,EgyItemCode,WithholdingTax,EgyCodeType,MaxDicountParcent," +
                    "is_extra_tax_applied,MaxDiscountAmount,showInAndroid,Item_sort,CreatedDate,CreatedBy)" +
                    " values(@id,@Code,@GrpCode,@name,@nameEN,@barcode,@group_id,@unit,@purch_price,@sale_price," +
                    "@limit,@discount,@tax_group,@tax,@IS_Deleted,@image,@ShowInPOS,@Wscale,@store,@ItemType," +
                    "@ItemProperty,@FillValue,@MaxQtyLimit,@EgyItemCode,@WithholdingTax,@EgyCodeType," +
                    "@MaxDicountParcent,@is_extra_tax_applied,@MaxDiscountAmount,@showInAndroid,@Item_sort," +
                    "@CreatedDate,@CreatedBy)";

                string updateSql =
                    $"update Items set Code=@Code,GrpCode=@GrpCode,name=@name,nameEN=@nameEN,barcode=@barcode," +
                    "group_id=@group_id,unit=@unit,purch_price=@purch_price,sale_price=@sale_price,limit=@limit," +
                    "discount=@discount,tax_group=@tax_group,tax=@tax,IS_Deleted=@IS_Deleted,image=@image," +
                    "ShowInPOS=@ShowInPOS,Wscale=@Wscale,store=@store,ItemType=@ItemType,ItemProperty=@ItemProperty," +
                    "FillValue=@FillValue,MaxQtyLimit=@MaxQtyLimit,EgyItemCode=@EgyItemCode," +
                    "WithholdingTax=@WithholdingTax,EgyCodeType=@EgyCodeType,MaxDicountParcent=@MaxDicountParcent," +
                    "is_extra_tax_applied=@is_extra_tax_applied,MaxDiscountAmount=@MaxDiscountAmount," +
                    $"showInAndroid=@showInAndroid,Item_sort=@Item_sort,updatedDate=@updatedDate,updatedBy=@updatedBy" +
                    $" where id={Code}";

                var cmd = new SqlCommand(
                    Code == -1 ? insertSql : updateSql, conn, transaction);

                if (Code == -1)
                    cmd.Parameters.Add("@id", SqlDbType.NVarChar).Value = txtNo.Text;

                int grpId = GetSelectedIntValue(cmbGroups);
                cmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = txtItemCode.Text;
                cmd.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = GetGroupCode(grpId);
                cmd.Parameters.Add("@group_id", SqlDbType.Int).Value = grpId;
                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtName.Text;
                cmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = txtNameEN.Text;
                cmd.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = txtBarcode.Text;
                cmd.Parameters.Add("@unit", SqlDbType.Int).Value = GetSelectedIntValue(cmbUnit);
                cmd.Parameters.Add("@purch_price", SqlDbType.Float).Value = ParseDouble(txtPurchPrice.Text);
                cmd.Parameters.Add("@sale_price", SqlDbType.Float).Value = ParseDouble(txtSalePrice.Text);
                cmd.Parameters.Add("@limit", SqlDbType.Int).Value = (int)ParseDouble(txtLimit.Text);
                cmd.Parameters.Add("@discount", SqlDbType.Float).Value = ParseDouble(txtDiscount.Text);
                cmd.Parameters.Add("@tax_group", SqlDbType.Int).Value = taxGroupVal;
                cmd.Parameters.Add("@tax", SqlDbType.Float).Value = taxVal;
                cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                cmd.Parameters.Add("@ShowInPOS", SqlDbType.Bit).Value = showInPOS;
                cmd.Parameters.Add("@ItemType", SqlDbType.Int).Value = itemTypeVal;
                cmd.Parameters.Add("@Wscale", SqlDbType.Int).Value = wscaleVal;
                cmd.Parameters.Add("@ItemProperty", SqlDbType.Int).Value = propIdx;
                cmd.Parameters.Add("@FillValue", SqlDbType.Float).Value = fillVal;
                cmd.Parameters.Add("@store", SqlDbType.Int).Value = storeVal;
                cmd.Parameters.Add("@MaxQtyLimit", SqlDbType.Int).Value = (int)ParseDouble(txtMaxQtyLimit.Text);
                cmd.Parameters.Add("@MaxDicountParcent", SqlDbType.Float).Value = ParseDouble(txtMaxDicountParcent.Text);
                cmd.Parameters.Add("@MaxDiscountAmount", SqlDbType.Float).Value = ParseDouble(txtMaxDicAmount.Text);
                cmd.Parameters.Add("@is_extra_tax_applied", SqlDbType.Bit).Value = isExtraTax;
                cmd.Parameters.Add("@showInAndroid", SqlDbType.Bit).Value = showAndroid;
                cmd.Parameters.Add("@Item_sort", SqlDbType.Int).Value = (int)ParseDouble(TxtItemSort.Text);
                cmd.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar).Value = MainClass.UserName;
                cmd.Parameters.Add("@updatedDate", SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@updatedBy", SqlDbType.NVarChar).Value = MainClass.UserName;
                cmd.Parameters.Add("@EgyItemCode", SqlDbType.NVarChar).Value = txtEgyCode.Text;
                cmd.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = ParseDouble(txtWithholdingTaxBox.Text);

                string egyCodeType = cmbEgyCodeType.SelectedIndex > -1
                    ? (cmbEgyCodeType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "" : "";
                cmd.Parameters.Add("@EgyCodeType", SqlDbType.NVarChar).Value = egyCodeType;

                // Image
                if (picImage.Source is BitmapImage bmp)
                {
                    byte[] imgArr = BitmapImageToByteArray(bmp);
                    cmd.Parameters.Add("@image", SqlDbType.Image).Value = imgArr;
                }
                else
                {
                    cmd.Parameters.Add("@image", SqlDbType.Image).Value = DBNull.Value;
                }

                cmd.ExecuteNonQuery();

                // Delete old units
                if (Code != -1)
                    new SqlCommand($"delete from ItemUnits where ItemId={Code}",
                        conn, transaction).ExecuteNonQuery();

                // Insert default unit
                int itemIdInt = (int)ParseDouble(txtNo.Text);
                var unitCmd = new SqlCommand(
                    "insert into ItemUnits(ItemId,unit,perc,purch,sale,barcode)" +
                    "values(@ItemId,@unit,@perc,@purch,@sale,@barcode)", conn, transaction);
                unitCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemIdInt;
                unitCmd.Parameters.Add("@unit", SqlDbType.Int).Value = GetSelectedIntValue(cmbUnit);
                unitCmd.Parameters.Add("@perc", SqlDbType.Float).Value = 1;
                unitCmd.Parameters.Add("@purch", SqlDbType.Float).Value = ParseDouble(txtPurchPrice.Text);
                unitCmd.Parameters.Add("@sale", SqlDbType.Float).Value = ParseDouble(txtSalePrice.Text);
                unitCmd.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = txtBarcode.Text;
                unitCmd.ExecuteNonQuery();

                // Insert extra units
                foreach (var u in listItemUnit)
                {
                    var uc = new SqlCommand(
                        "insert into ItemUnits(ItemId,unit,perc,purch,sale,barcode)" +
                        "values(@ItemId,@unit,@perc,@purch,@sale,@barcode)", conn, transaction);
                    uc.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemIdInt;
                    uc.Parameters.Add("@unit", SqlDbType.Int).Value = u.DgvUnit;
                    uc.Parameters.Add("@perc", SqlDbType.Float).Value = u.DgvUnitEquality;
                    uc.Parameters.Add("@purch", SqlDbType.Float).Value = u.DgvUnitPurchasePrice;
                    uc.Parameters.Add("@sale", SqlDbType.Float).Value = u.DgvUnitSalePrice;
                    uc.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = u.DgvUnitBarcode ?? "";
                    uc.ExecuteNonQuery();
                }

                // ItemPrices
                if (Code != -1)
                    new SqlCommand($"delete from ItemPrices where ItemId={Code}",
                        conn, transaction).ExecuteNonQuery();

                var priceCmd = new SqlCommand(
                    "insert into ItemPrices(ItemId,UnitId,time,date,purch_price,sale_price," +
                    "low_purch_price,high_purch_price,low_sale_price,high_sale_price," +
                    "CompetitorPrice,IS_Deleted,Emp,WholesalePrice,ConsumerPrice)" +
                    " values(@ItemId,@UnitId,@time,@date,@purch_price,@sale_price," +
                    "@low_purch_price,@high_purch_price,@low_sale_price,@high_sale_price," +
                    "@CompetitorPrice,@IS_Deleted,@Emp,@WholesalePrice,@ConsumerPrice)",
                    conn, transaction);
                priceCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = txtNo.Text;
                priceCmd.Parameters.Add("@UnitId", SqlDbType.Int).Value = GetSelectedIntValue(cmbUnit);
                priceCmd.Parameters.Add("@time", SqlDbType.NVarChar).Value = DateTime.Now.ToShortTimeString();
                priceCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                priceCmd.Parameters.Add("@purch_price", SqlDbType.Float).Value = ParseDouble(txtPurchPrice.Text);
                priceCmd.Parameters.Add("@sale_price", SqlDbType.Float).Value = ParseDouble(txtSalePrice.Text);
                priceCmd.Parameters.Add("@low_purch_price", SqlDbType.Float).Value = ParseDouble(txtPurchPrice.Text);
                priceCmd.Parameters.Add("@high_purch_price", SqlDbType.Float).Value = ParseDouble(txtPurchPrice.Text);
                priceCmd.Parameters.Add("@low_sale_price", SqlDbType.Float).Value = ParseDouble(txtLastSalePrice.Text);
                priceCmd.Parameters.Add("@high_sale_price", SqlDbType.Float).Value = ParseDouble(txtSalePrice.Text);
                priceCmd.Parameters.Add("@CompetitorPrice", SqlDbType.Float).Value = ParseDouble(txtCompetitorPrice.Text);
                priceCmd.Parameters.Add("@WholesalePrice", SqlDbType.Float).Value = ParseDouble(txtWholesalePrice.Text);
                priceCmd.Parameters.Add("@ConsumerPrice", SqlDbType.Float).Value = ParseDouble(txtConsumerPrice.Text);
                priceCmd.Parameters.Add("@Emp", SqlDbType.Int).Value = MainClass.EmpNo;
                priceCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                priceCmd.ExecuteNonQuery();

                // Components
                int selTypeVal = GetSelectedIntValue(cmbItemType);
                if (selTypeVal == 2 || selTypeVal == 3 || selTypeVal == 4)
                {
                    if (componentSource.Count == 0)
                    {
                        DXMessageBox.Show(MainClass.Language == "ar"
                            ? "ادخل مكونات المادة" : "Enter the items components",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TabControl1.SelectedIndex = 3;
                        return;
                    }

                    new SqlCommand($"delete from ItemComponents where itemId={txtNo.Text.Trim()}",
                        conn, transaction).ExecuteNonQuery();

                    foreach (var comp in componentSource)
                    {
                        double qty = comp.Quantity;
                        double price = comp.Price;
                        double total = qty * price;

                        var compCmd = new SqlCommand(
                            "insert into ItemComponents(itemId,ComponentId,price,quantity,unit,total,type,store)" +
                            "values(@ItemID,@compID,@price,@quantity,@unit,@total,@type,@store)",
                            conn, transaction);
                        compCmd.Parameters.Add("@ItemID", SqlDbType.Int).Value = ParseDouble(txtNo.Text);
                        compCmd.Parameters.Add("@compID", SqlDbType.Int).Value = comp.ItemId;
                        compCmd.Parameters.Add("@unit", SqlDbType.Int).Value = comp.UnitId;
                        compCmd.Parameters.Add("@price", SqlDbType.Float).Value = price;
                        compCmd.Parameters.Add("@quantity", SqlDbType.Float).Value = qty;
                        compCmd.Parameters.Add("@total", SqlDbType.Float).Value = total;
                        compCmd.Parameters.Add("@store", SqlDbType.Int).Value = comp.StoreCompId;
                        compCmd.Parameters.Add("@type", SqlDbType.Bit).Value = comp.IsAdded;
                        compCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                // Sync
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    await SyncProduct();

                // بضاعة أول المدة
                if (Code == -1 && dgvFirstSource.Count > 0)
                    SaveAndPrint();

                // Log
                Logger.Info(Code == -1
                    ? $"تم حفظ صنف جديد {txtName.Text} بواسطة: {MainClass.UserName}"
                    : $"تم تعديل صنف {txtName.Text} بواسطة: {MainClass.UserName}");

                // MQTT Publish
                if (Home._is_active)
                {
                    if (!ConnectBroker.IsConnectedToInternet())
                        SaveItemIDOffLine();
                    else if (ConnectBroker.CheckConnectionAndBroker())
                    {
                        int itemNo = Convert.ToInt32(txtNo.Text);
                        string cond = $"where id={itemNo}";
                        string cond2 = $"where Itemid={itemNo} ";
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "Items",
                            Encoding.UTF8.GetBytes(SendData.GetItems(cond)), 0, true);
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "ItemsUnit",
                            Encoding.UTF8.GetBytes(SendData.GetItemsUnit(cond2)), 0, true);
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "ItemPrices",
                            Encoding.UTF8.GetBytes(SendData.GetItemPrices(cond2)), 0, true);
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "ItemComponents",
                            Encoding.UTF8.GetBytes(SendData.GetItemComponents(cond2)), 0, true);
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "ItemSerialNo",
                            Encoding.UTF8.GetBytes(SendData.GetItemSerialNo(cond2)), 0, true);
                        ConnectBroker.mqttClient.Publish(
                            ConnectBroker.ClientCode + "ItemBarcode",
                            Encoding.UTF8.GetBytes(SendData.GetItemBarcode(cond2)), 0, true);
                    }
                }

                // رسالة الحفظ
                string savedMsg = Code != -1
                    ? (MainClass.Language == "ar" ? "تم حفظ التعديلات بنجاح..." : "Modifications saved successfully...")
                    : (MainClass.Language == "ar" ? "تم الحفظ بنجاح..." : "Saved successfully...");

                var dlgResult = DXMessageBox.Show(
                    savedMsg + "\n\nهل تريد إدخال صنف جديد؟",
                    "تم الحفظ",
                    MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (dlgResult == MessageBoxResult.Yes)
                {
                    CLR();
                    txtName.Focus();
                }
                else
                {
                    Code = (int)ParseDouble(txtNo.Text);
                    ISNew = false;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "اختر مادة ليتم حذفها" : "Select an item to be deleted",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                // Check invoices
                if (!string.IsNullOrEmpty(txtNo.Text))
                {
                    var chkAdapter = new SqlDataAdapter(
                        "select Inv_Sub.InvGlobalID from Inv_Sub " +
                        "left join Inv on Inv.InvGlobalID=Inv_Sub.InvGlobalID " +
                        "where Inv_Sub.ItemId=@ItemId and Inv.IS_Deleted=0", conn);
                    chkAdapter.SelectCommand.Parameters.AddWithValue("@ItemId", txtNo.Text.Trim());
                    var chkDt = new DataTable();
                    chkAdapter.Fill(chkDt);
                    if (chkDt.Rows.Count > 0)
                    {
                        DXMessageBox.Show(MainClass.Language == "ar"
                            ? "لا يمكن حذف مادة مرتبط بفواتير"
                            : "Item previously used in Invoices",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var result = DXMessageBox.Show(
                    MainClass.Language == "ar" ? "هل انت متأكد من حذف الصنف؟" : "Are you sure to delete the item?",
                    MainClass.Language == "ar" ? "تحذير" : "warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                void DeleteFrom(string table, string field)
                {
                    var c = new SqlCommand($"delete from {table} where {field}=@id", conn);
                    c.Parameters.Add("@id", SqlDbType.Int).Value = Code;
                    c.ExecuteNonQuery();
                }

                DeleteFrom("Items", "id");
                DeleteFrom("ItemUnits", "ItemId");
                DeleteFrom("Itembarcodes", "ItemId");
                DeleteFrom("ItemComponents", "ItemId");
                DeleteFrom("ItemPrices", "ItemId");

                DXMessageBox.Show(MainClass.Language == "ar" ? "تم الحذف" : "Deleted",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Info($"تم حذف الصنف {txtName.Text} بواسطة: {MainClass.UserName}");
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region New

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (Code != -1 &&
                (!string.IsNullOrWhiteSpace(txtName.Text) ||
                 !string.IsNullOrWhiteSpace(txtSalePrice.Text)))
            {
                var res = DXMessageBox.Show("لم يتم حفظ الصنف، هل تريد الاستمرار؟",
                    "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No) { txtName.Focus(); return; }
            }
            CLR();
            txtSrchBarcode.IsReadOnly = false;
            txtSrchBarcode.Text = "";
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            IsClosed = true;
            Close();
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.sql = "select id, name ,nameEN , sale_price , unit from Items where IS_Deleted=0 order by id";
            frm.search = "select id, name ,nameEN , sale_price , unit from Items";
            frm.Itemname = "";
            frm.txtSrchNm.Text = "";
            frm.ShowDialog();
            if (frm.ISDone && frm.ItemId > 0)
            {
                CLR();
                dgvRowChng(frm.ItemId);
            }
        }

        private void SearchByBarcode(string barcode)
        {
            try
            {
                var adapter = new SqlDataAdapter("select id from Items where barcode=@barcode", conn);
                adapter.SelectCommand.Parameters.AddWithValue("@barcode", barcode);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    int id = Convert.ToInt32(dt.Rows[0]["id"]);
                    Navigate("select * from Items where id=" + id);

                    var imgAdapter = new SqlDataAdapter($"select image from Items where id={id}", conn);
                    var imgDt = new DataTable();
                    imgAdapter.Fill(imgDt);
                    if (imgDt.Rows[0]["image"] != DBNull.Value)
                        picImage.Source = ByteArrayToBitmapImage((byte[])imgDt.Rows[0]["image"]);
                    else
                        picImage.Source = null;

                    txtSrchBarcode.Text = "";
                    txtSrchBarcode.IsReadOnly = true;
                }
            }
            catch { }
        }

        public void dgvRowChng(int id)
        {
            Code = id;
            Navigate("select * from Items where id=" + Code);

            var imgAdapter = new SqlDataAdapter($"select image from Items where id={Code}", conn);
            var imgDt = new DataTable();
            imgAdapter.Fill(imgDt);
            if (imgDt.Rows.Count > 0 && imgDt.Rows[0]["image"] != DBNull.Value)
                picImage.Source = ByteArrayToBitmapImage((byte[])imgDt.Rows[0]["image"]);
            else
                picImage.Source = null;
        }

        #endregion

        #region Image

        private void lnkImgAdd_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "All Files|*.*|JPEG|*.jpg|BMP|*.bmp|GIF|*.gif|TIFF|*.tiff|PNG|*.png"
                };
                if (dlg.ShowDialog() == true)
                {
                    picImage.Source = new BitmapImage(new Uri(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lnkImgClr_Click(object sender, MouseButtonEventArgs e)
        {
            picImage.Source = null;
        }

        #endregion

        #region Price Calculation

        private void txtPurchPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (Code == -1 && !string.IsNullOrWhiteSpace(txtPurchPrice.Text))
                {
                    if (double.TryParse(txtPurchPrice.Text, out double purchVal))
                    {
                        txtRecentPurchPrice.Text = Math.Round(purchVal, 2).ToString("0.##");
                        txtCostAvrg.Text = txtPurchPrice.Text;
                    }
                }

                if (!string.IsNullOrWhiteSpace(txtSalePrice.Text) &&
                    !string.IsNullOrWhiteSpace(txtPurchPrice.Text) &&
                    double.TryParse(txtSalePrice.Text, out double saleP) &&
                    double.TryParse(txtPurchPrice.Text, out double purchP) && purchP != 0)
                {
                    txtProfitPer.Text = Math.Round((saleP - purchP) / purchP * 100, 2).ToString("0.##");
                }
            }
            catch { }
        }

        private void txtSalePrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_sale1 &&
                    !string.IsNullOrWhiteSpace(txtSalePrice.Text) &&
                    !string.IsNullOrWhiteSpace(txtPurchPrice.Text) &&
                    double.TryParse(txtSalePrice.Text, out double saleP) &&
                    double.TryParse(txtPurchPrice.Text, out double purchP) && purchP != 0)
                {
                    txtProfitPer.Text = Math.Round((saleP - purchP) / purchP * 100, 2).ToString("0.##");
                }
            }
            catch { }
        }

        private void txtProfitPer_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_sale2 &&
                    double.TryParse(txtPurchPrice.Text, out double purchP) &&
                    double.TryParse(txtProfitPer.Text, out double profP))
                {
                    txtSalePrice.Text = Math.Round(purchP + purchP * profP / 100, 2).ToString("0.##");
                }
            }
            catch { }
        }

        private void CalcPrice(int type)
        {
            bool priceIncVAT = false;
            bool priceIncVAT2 = false;
            double taxPerc = 15.0;
            InvoiceOper.InvoiceProperties(2, 1, ref priceIncVAT, ref taxPerc);
            InvoiceOper.InvoiceProperties(3, 1, ref priceIncVAT2, ref taxPerc);

            if (!double.TryParse(txtSalePrice.Text, out double saleVal)) saleVal = 0;
            if (!double.TryParse(txtSalePrice2.Text, out double saleVal2)) saleVal2 = 0;

            switch (type)
            {
                case 1:
                    saleVal2 = (priceIncVAT || priceIncVAT2)
                        ? saleVal / (1.0 + taxPerc / 100.0)
                        : saleVal * (taxPerc / 100.0) + saleVal;
                    break;
                case 2:
                    saleVal = (priceIncVAT || priceIncVAT2)
                        ? saleVal2 * (taxPerc / 100.0) + saleVal2
                        : saleVal2 / (1.0 + taxPerc / 100.0);
                    break;
            }

            txtSalePrice.Text = Math.Round(saleVal, 5, MidpointRounding.AwayFromZero).ToString();
            txtSalePrice2.Text = Math.Round(saleVal2, 5, MidpointRounding.AwayFromZero).ToString();
        }

        private void txtSalePrice_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrEmpty(txtSalePrice.Text))
                CalcPrice(1);
        }

        private void txtSalePrice2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrEmpty(txtSalePrice2.Text))
                CalcPrice(2);
        }

        private void txtSalePrice_Leave(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSalePrice.Text)) CalcPrice(1);
        }

        private void txtSalePrice2_Leave(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSalePrice2.Text)) CalcPrice(2);
        }

        private void txtPurchPrice_Enter(object sender, RoutedEventArgs e)
        { _purch1 = true; _purch2 = false; }

        private void txtSalePrice_Enter(object sender, RoutedEventArgs e)
        { _sale1 = true; _sale2 = false; }

        private void txtSaleDollar_Enter(object sender, RoutedEventArgs e)
        { _sale1 = false; _sale2 = true; }

        #endregion

        #region Barcode

        private void txtBarcode_TextChanged(object sender, TextChangedEventArgs e) { }

        private void btnGenerateBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string barcode = UtilitiesProj.Common.RandomNumber(100000000, 999999999).ToString();
                int unitId = GetSelectedIntValue(cmbUnit, 0);
                if (ISValidBarcode(barcode, unitId))
                    txtBarcode.Text = barcode;
            }
            catch { }
        }

        private string GenerateBarcode(int unitId)
        {
            try
            {
                string bc = UtilitiesProj.Common.RandomNumber(100000000, 999999999).ToString();
                return ISValidBarcode(bc, unitId) ? bc : "";
            }
            catch { return ""; }
        }

        private bool ISValidBarcode(string barcode, int unitId)
        {
            if (string.IsNullOrEmpty(barcode)) return true;

            var a1 = new SqlDataAdapter(
                "select ItemId,unit from ItemUnits where barcode=@barcode", conn1);
            a1.SelectCommand.Parameters.AddWithValue("@barcode", barcode);
            var dt1 = new DataTable();
            a1.Fill(dt1);

            if (dt1.Rows.Count > 0)
            {
                bool sameItem = Convert.ToDouble(dt1.Rows[0]["ItemId"]) == ParseDouble(txtNo.Text);
                bool sameUnit = Convert.ToInt32(dt1.Rows[0]["unit"]) == unitId;
                if (!(sameItem && sameUnit))
                {
                    DXMessageBox.Show(MainClass.Language == "ar"
                        ? "الباركود تم ادخاله مسبقا" : "Barcode is previously inserted",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            var a2 = new SqlDataAdapter(
                "select ItemId from Itembarcodes where barcode=@barcode and ItemId<>@ItemId", conn1);
            a2.SelectCommand.Parameters.AddWithValue("@barcode", barcode);
            a2.SelectCommand.Parameters.AddWithValue("@ItemId", txtNo.Text.Trim());
            var dt2 = new DataTable();
            a2.Fill(dt2);
            if (dt2.Rows.Count > 0)
            {
                DXMessageBox.Show(MainClass.Language == "ar"
                    ? "الباركود تم ادخاله مسبقا" : "Barcode is previously inserted",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        #endregion

        #region Units Grid

        private void BtnEditGenerateBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UnitDgvRow row)
            {
                string bc = GenerateBarcode(row.DgvUnit);
                if (!string.IsNullOrEmpty(bc)) row.DgvUnitBarcode = bc;

                // Sync back to listItemUnit
                foreach (var u in listItemUnit)
                {
                    if (u.DgvNo == row.DgvNo)
                    { u.DgvUnitBarcode = row.DgvUnitBarcode; break; }
                }
                GridControl1.Items.Refresh();
            }
        }

        private void BtnDeleteUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UnitDgvRow row)
            {
                var res = DXMessageBox.Show("هل انت متأكد من الحذف؟", "رسالة تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;

                unitGridSource.Remove(row);
                listItemUnit.RemoveAll(u => u.DgvNo == row.DgvNo);

                // Renumber
                int n = 1;
                foreach (var u in unitGridSource) u.DgvNo = n++;
                foreach (var u in listItemUnit) u.DgvNo = n++;
            }
        }

        private void GridControl1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is UnitDgvRow row)
            {
                // Sync to listItemUnit
                foreach (var u in listItemUnit)
                {
                    if (u.DgvNo == row.DgvNo)
                    {
                        u.DgvUnit = row.DgvUnit;
                        u.DgvUnitEquality = row.DgvUnitEquality;
                        u.DgvUnitPurchasePrice = row.DgvUnitPurchasePrice;
                        u.DgvUnitSalePrice = row.DgvUnitSalePrice;
                        u.DgvUnitBarcode = row.DgvUnitBarcode;
                        break;
                    }
                }

                // Auto-calc prices when equality changes
                if (e.Column.Header?.ToString() == "التعادل" || e.Column.Header?.ToString() == "الوحدة")
                {
                    if (double.TryParse(txtPurchPrice.Text, out double pp) &&
                        double.TryParse(txtSalePrice.Text, out double sp))
                    {
                        row.DgvUnitPurchasePrice = pp * row.DgvUnitEquality;
                        row.DgvUnitSalePrice = sp * row.DgvUnitEquality;
                    }
                }
            }
        }

        private void cmbUnitInGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cmb && cmb.DataContext is UnitDgvRow row)
            {
                row.DgvUnitName = GetUnitName(row.DgvUnit);
            }
        }

        #endregion

        #region dgvFirst (بضاعة أول المدة)

        private void dgvFirst_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is DgvFirstRow row)
            {
                if (double.TryParse(txtPurchPrice.Text, out double purchP))
                    row.TotalCost = row.Quantity * purchP;
            }
        }

        private void dgvFirst_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DgvFirstRow row)
            {
                var res = DXMessageBox.Show("هل تريد الحذف؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                    dgvFirstSource.Remove(row);
            }
        }

        #endregion

        #region Component Grid

        private void dgvComponent_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is ComponentRow row)
            {
                row.Total = row.Quantity * row.Price;
                CalcTot();
            }
        }

        private void dgvComponent_SelectedCellsChanged(object sender,
            SelectedCellsChangedEventArgs e)
        {
            if (dgvComponent.SelectedItem is ComponentRow row)
                RowIndex = componentSource.IndexOf(row);
        }

        private void dgvComponent_DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ComponentRow row)
            {
                var res = DXMessageBox.Show("هل تريد حذف الصف؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    componentSource.Remove(row);
                    CalcTot();
                }
            }
        }

        public void CalcTot()
        {
            try
            {
                double total = 0.0;
                foreach (var row in componentSource)
                {
                    if (!string.IsNullOrEmpty(row.ItemName))
                        total += row.Total;
                }
                txtTotal.Text = Math.Round(total, 2).ToString("0.##");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CalcStock

        private void CalcStock()
        {
            try
            {
                safeBalanceSource.Clear();
                int itemIdVal = (int)ParseDouble(txtNo.Text);

                var adapter = new SqlDataAdapter(
                    $"select id,name from Safes where branch={MainClass.BranchNo}" +
                    " and status=1 and IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    double qty = Inventory.CalcItemStock(
                        Convert.ToInt32(row["id"]), itemIdVal, MainClass.BranchNo);
                    if (qty != 0)
                    {
                        safeBalanceSource.Add(new SafeBalanceRow
                        {
                            StoreName = row["name"].ToString(),
                            Quantity = qty
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء حساب المخزون\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region GroupBox1 Controls

        private void cmbItemProperties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = cmbItemProperties.SelectedIndex;
            if (idx == 1)
            {
                PnlProperty.Visibility = Visibility.Visible;
                txtFillVal.Visibility = Visibility.Collapsed;
                ckWeightScale.Visibility = Visibility.Visible;
                ckScalePrice.Visibility = Visibility.Visible;
                chksalWeightScale.Visibility = Visibility.Visible;
                ckWeightScale.IsChecked = true;
            }
            else if (idx == 2)
            {
                PnlProperty.Visibility = Visibility.Visible;
                ckWeightScale.Visibility = Visibility.Collapsed;
                ckScalePrice.Visibility = Visibility.Collapsed;
                chksalWeightScale.Visibility = Visibility.Collapsed;
                txtFillVal.Visibility = Visibility.Visible;
                txtFillVal.Focus();
            }
            else
            {
                PnlProperty.Visibility = Visibility.Collapsed;
                txtFillVal.Text = "0";
            }
        }

        private void btnAddGrp_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemCategory();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
            LoadGroups();
        }

        private void btnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmUnits();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
            LoadUnits();
        }

        private void cmbGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroups.SelectedIndex > -1 && cmbGroups.SelectedValue != null)
                {
                    int grpId = GetSelectedIntValue(cmbGroups);
                    GenerateCode(GetGroupCode(grpId));
                }
            }
            catch { }
        }

        #endregion

        #region txtSrchBarcode Placeholder

        private void txtSrchBarcode_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSrchBarcodePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void txtSrchBarcode_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSrchBarcode.Text))
                txtSrchBarcodePlaceholder.Visibility = Visibility.Visible;
        }

        private void txtSrchBarcode_TextChanged(object sender, TextChangedEventArgs e) { }

        private void txtSrchBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtSrchBarcode.Text) &&
                txtSrchBarcode.Text != "بحث باركود")
            {
                SearchByBarcode(txtSrchBarcode.Text);
            }
        }

        #endregion

        #region Button1 / Button2

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // توليد باركود عشوائي (hamode)
            try
            {
                var rnd = new Random();
                int num = rnd.Next(4344, 34355353);
                txtBarcode.Text = (ParseDouble(txtBarcode.Text) + num).ToString();
            }
            catch { }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmMultiBarcode();
                frm.txtNo.Text = txtNo.Text;
                frm.txtSymbol.Text = txtName.Text;
                frm.LoadDG();
                frm.ShowDialog();
            }
            catch { }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        #endregion

        #region Print / Invoice

        private void SaveAndPrint()
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                invoiceOper.SaveInvoice(BindToInvoice(), null, true);
                CLR();
            }
            catch { }
        }

        private Invoice BindToInvoice()
        {
            var invoice = new Invoice();
            double total = 0.0;

            if (conn.State != ConnectionState.Open) conn.Open();
            if (conn1.State != ConnectionState.Open) conn1.Open();

            int invoiceNo = (int)Math.Round(Convert.ToDouble(
                new SqlCommand(
                    $"select max(id) from Inv where branch={MainClass.BranchNo}" +
                    " and inv_type=9 and proc_type=1", conn1).ExecuteScalar()) + 1.0);

            foreach (var row in dgvFirstSource)
            {
                total += Math.Round(row.TotalCost, 2);
            }

            if (ProcCode == -1) GetInvoiceIDs();

            string invGlobalID = "";
            string entryGlobalID = (-1).ToString();
            InvoiceOper.GetInvoiceGlobalID(ref invGlobalID, ref ProcCode);

            invoice.AutoIncrementID = ProcCode;
            invoice.ClientCode = Sync.ClientCode;
            invoice.Total = total;
            invoice.Net = 0.0;
            invoice.VAT = 0.0;
            invoice.Discount = 0.0;
            invoice.InvGlobalID = invGlobalID;
            invoice.EntryGlobalID = entryGlobalID;
            invoice.InvoiceNo = invoiceNo;
            invoice.InvoiceType = InvoiceType.BeginingInventory;
            invoice.ProcType = 1;
            invoice.InvDate = DateTime.Now;
            invoice.Store = 1;
            invoice.Branch = MainClass.BranchNo;
            invoice.DistBranch = Sync.DistBranch;
            invoice.BranchType = Sync.BranchType;
            invoice.User = MainClass.EmpNo;
            invoice.Treasury = MainClass.UserTreasury;
            invoice.Saleman = -1;
            invoice.Customer = 1;
            invoice.InvNote = "بضاعة اول مدة صنف" + txtNo.Text;
            invoice.IsDeleted = false;
            invoice.VATperc = defVAT;
            invoice.ReffNo = (-1).ToString();
            invoice.RefDate = DateTime.Now;
            invoice.InvCombinedId =
                MainClass.BranchCode + "F1" + invoiceNo;

            foreach (var row in dgvFirstSource)
            {
                var item = new Item
                {
                    InvGlobalID = invGlobalID,
                    ClientCode = Sync.ClientCode,
                    Name = txtName.Text,
                    ItemNo = Convert.ToInt32(txtNo.Text),
                    ProcType = 1,
                    Quantity = row.Quantity,
                    UnitEquality = 1.0,
                    PrimaryQnty = row.Quantity,
                    Unit = GetSelectedIntValue(cmbUnit),
                    Price = ParseDouble(txtCostAvrg.Text),
                    AvegCost = ParseDouble(txtCostAvrg.Text),
                    Vat = 0.0,
                    VatPerc = 0.0,
                    Barcode = txtBarcode.Text,
                    ItemDiscount = 0.0,
                    ValiableStock = row.Quantity,
                    Store = row.StoreId,
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = "",
                    ProductId = 0
                };
                invoice.Items.Add(item);
            }
            return invoice;
        }

        private void GetInvoiceIDs()
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            if (ProcCode == -1)
            {
                ProcCode = (int)Math.Round(Convert.ToDouble(
                    new SqlCommand("select ISNULL(MAX(proc_id),0) from Inv", conn)
                    .ExecuteScalar()) + 1.0);
                Code = (int)Math.Round(ParseDouble(txtNo.Text));
            }
        }

        #endregion

        #region Sync

        private async Task<bool> SyncProduct()
        {
            var product = BindToProduct();
            var productCRUD = new ProductCRUD(Sync.APIUrl);
            if (Sync.ActiveSync)
                productCRUD.PostProductsOnline(product, ISNew);
            else
                productCRUD.AddProductLocally(product, ISNew);
            return true;
        }

        private Product BindToProduct()
        {
            var product = new Product
            {
                ClientCode = Sync.ClientCode,
                ProductId = (int)ParseDouble(txtNo.Text),
                Code = txtItemCode.Text,
                Name = txtName.Text,
                NameEN = txtNameEN.Text,
                Category = cmbGroups.Text,
                CategoryID = cmbGroups.SelectedValue?.ToString(),
                Type = (ProductTypes)GetSelectedIntValue(cmbItemType),
                Property = (ProductProperty)cmbItemProperties.SelectedIndex,
                ShowInPOS = clkShowInPOS.IsChecked == true,
                showInAndroid = chkInAndroid.IsChecked == true,
                PurchPrice = ParseDouble(txtPurchPrice.Text),
                SalePrice = ParseDouble(txtSalePrice.Text),
                limitStock = (int)ParseDouble(txtLimit.Text),
                VAT = ckNoTax.IsChecked == true ? 0.0 : defVAT,
                CreateDate = DateTime.Now,
                LastUpdateDate = DateTime.Now,
                Branch = MainClass.BranchNo,
                DistBranch = Sync.DistBranch,
                BranchType = Sync.BranchType,
                Active = true,
                PackingValue = ParseDouble(txtFillVal.Text),
                MainUnit = GetSelectedIntValue(cmbUnit),
                MainBarcode = txtBarcode.Text
            };

            int propIdx = cmbItemProperties.SelectedIndex > 0 ? cmbItemProperties.SelectedIndex : 0;
            int wscale = 0;
            if (propIdx == 1)
                wscale = ckWeightScale.IsChecked == true ? 1 : 2;
            product.Wscale = wscale;

            // Main unit
            product.ProductUnits.Add(new ProductUnit
            {
                ProductId = product.ProductId,
                UnitId = GetSelectedIntValue(cmbUnit),
                UnitEquality = 1.0,
                PurchasePrice = product.PurchPrice,
                SalePrice = product.SalePrice,
                Barcode = txtBarcode.Text,
                ClientCode = Sync.ClientCode
            });

            // Extra units
            foreach (var u in listItemUnit)
            {
                product.ProductUnits.Add(new ProductUnit
                {
                    ProductId = product.ProductId,
                    UnitId = u.DgvUnit,
                    UnitName = Common.GetUnitName(u.DgvUnit) ?? "",
                    UnitEquality = u.DgvUnitEquality,
                    PurchasePrice = u.DgvUnitPurchasePrice,
                    SalePrice = u.DgvUnitSalePrice,
                    Barcode = u.DgvUnitBarcode ?? "",
                    ClientCode = Sync.ClientCode
                });
            }

            // Components
            foreach (var comp in componentSource)
            {
                product.ProductComponents.Add(new ProductComponent
                {
                    ProductId = product.ProductId,
                    UnitId = comp.UnitId,
                    ComponentId = comp.ItemId,
                    ComponentCost = (float)comp.Price,
                    Quantity = comp.Quantity.ToString(),
                    InvertoryId = comp.StoreCompId,
                    ComponentType = comp.IsAdded ? 1 : 0,
                    ClientCode = Sync.ClientCode
                });
            }

            // Barcodes
            var bcAdapter = new SqlDataAdapter(
                $"select * from Itembarcodes where ItemId={ParseDouble(txtNo.Text)}", conn);
            var bcDt = new DataTable();
            bcAdapter.Fill(bcDt);
            foreach (DataRow row in bcDt.Rows)
            {
                product.ProductBarcodes.Add(new ProductBarcode
                {
                    Barcode = row["barcode"].ToString(),
                    ProductId = Convert.ToInt32(row["ItemId"]),
                    UnitId = GetSelectedIntValue(cmbUnit),
                    ClientCode = Sync.ClientCode
                });
            }

            // Prices
            product.productPrices = new ProductPrices
            {
                ProductId = product.ProductId,
                ClientCode = Sync.ClientCode,
                UnitId = GetSelectedIntValue(cmbUnit),
                date = DateTime.Now.Date,
                SalePrice = product.SalePrice,
                PurchasePrice = product.PurchPrice,
                LowSalePrice = ParseDouble(txtLastSalePrice.Text),
                HighSalePrice = product.SalePrice,
                CompetitorPrice = ParseDouble(txtCompetitorPrice.Text),
                ISDeleted = false
            };

            return product;
        }

        #endregion

        #region Offline / MQTT

        private void SaveItemIDOffLine()
        {
            var item = new ItemIdOff { itemid = Convert.ToInt32(txtNo.Text) };
            AddItemLocally(item);
        }

        private void AddItemLocally(ItemIdOff newItem)
        {
            var list = new List<ItemIdOff>();
            string path = Path.Combine(
                System.Windows.Forms.Application.StartupPath, "Data", "Itemid.json");
            if (File.Exists(path))
            {
                string val = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(val))
                    list = JsonConvert.DeserializeObject<List<ItemIdOff>>(val);
            }
            list.Add(newItem);
            File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
        }

        #endregion

        #region Helpers

        private void SetComboDefault(ComboBox cmb, int value)
        {
            if (cmb.ItemsSource == null) return;
            foreach (var item in cmb.Items)
            {
                if (item is DataRowView drv && drv["id"]?.ToString() == value.ToString())
                {
                    cmb.SelectedItem = item;
                    return;
                }
            }
        }

        private void SetComboValue(ComboBox cmb, object value)
        {
            cmb.SelectedValue = value;
        }

        private int GetSelectedIntValue(ComboBox cmb, int defaultVal = -1)
        {
            if (cmb.SelectedValue == null) return defaultVal;
            return int.TryParse(cmb.SelectedValue.ToString(), out int v) ? v : defaultVal;
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0.0;
            return double.TryParse(text, out double v) ? v : 0.0;
        }

        private string FormatPrice(object val)
        {
            if (val == null || val == DBNull.Value) return "0";
            if (decimal.TryParse(val.ToString(), out decimal d))
                return d.ToString("0.##");
            return val.ToString();
        }

        private string GetGroupCode(int id)
        {
            var adapter = new SqlDataAdapter(
                $"select ic.code, b.BranchId from ItemsCategory ic, Branches b" +
                $" where ic.id={id} and b.BranchId={MainClass.BranchNo}", conn1);
            var dt = new DataTable();
            adapter.Fill(dt);
            if (dt.Rows.Count > 0)
                return (Convert.ToInt32(dt.Rows[0]["BranchId"].ToString() + dt.Rows[0]["code"].ToString())).ToString();
            return "";
        }

        private void GenerateCode(string grpCode)
        {
            txtItemCode.Text = ItemOper.GenerateItemCode(grpCode);
        }

        private string GetUnitName(int unitId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select name from units where id={unitId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetStoreName(int storeId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select name from Safes where id={storeId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch { return ""; }
        }

        private BitmapImage ByteArrayToBitmapImage(byte[] arr)
        {
            using var ms = new MemoryStream(arr);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private byte[] BitmapImageToByteArray(BitmapImage bmp)
        {
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(ms);
            return ms.ToArray();
        }

        #endregion

        // ─── Unused references needed by Panel5 hidden controls ───
        private ComboBox cmbUnit1 = new ComboBox();
        private TextBox txtPurchUnit = new TextBox();
        private TextBox txtSaleUnit = new TextBox();
        private TextBox txtUnitBarcode = new TextBox();
        private TextBox txtPerUnit = new TextBox();
    }
}