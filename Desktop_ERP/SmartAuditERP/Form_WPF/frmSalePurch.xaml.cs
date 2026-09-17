using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.BarCode;
using QRCoder;
using SmartAuditERP.Form_WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSalePurch : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private int ProcCode;
        private int RestPurchCode;

        public int RestrType;
        public int InvType;
        public int ProcType;

        private string InvGlobalID;
        private string EntryGlobalID;

        private Print print;
        private InvoiceObj InvObj;

        private bool IsPrinted;
        private bool ISNew;
        private bool IsUpdated;
        private bool IsLoaded;

        private int RowIndex;
        private int SelectedId;

        public string SrchName;
        private int DGV_Count;
        private bool ISTRialEnd;
        private string hamode;
        private bool ItemNotClicked;

        public double Itemsumdiscount;
        private double InvTax;
        private double InvDiscount;
        private double TotAfterdisc;

        private bool linkedInvRet;
        private bool ISreturn;

        public FormPermission FrmPermission;
        private DataTable dtSeialNo;
        private bool flag;

        private ObservableCollection<InvItemRow_frmSale> _itemsSource;
        private ObservableCollection<SrchInvRow> _srchSource;

        #endregion

        #region Constructor

        public frmSalePurch()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            ProcCode = -1;
            RestPurchCode = -1;
            InvType = 1;
            ProcType = 1;
            InvGlobalID = "";
            EntryGlobalID = "";
            print = new Print(InvType);
            InvObj = new InvoiceObj(InvType, ProcType);
            IsPrinted = false;
            ISNew = true;
            IsUpdated = false;
            IsLoaded = false;
            RowIndex = -1;
            SelectedId = -1;
            SrchName = "";
            DGV_Count = 0;
            ISTRialEnd = false;
            ItemNotClicked = true;
            InvTax = 0.0;
            InvDiscount = 0.0;
            TotAfterdisc = 0.0;
            linkedInvRet = false;
            ISreturn = false;
            FrmPermission = new FormPermission();
            dtSeialNo = new DataTable();
            flag = false;

            _itemsSource = new ObservableCollection<InvItemRow_frmSale>();
            _srchSource = new ObservableCollection<SrchInvRow>();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgvItems.ItemsSource = _itemsSource;
            dgvSrch.ItemsSource = _srchSource;

            LoadSettings();
            LoadData();

            txtDate.EditValue = DateTime.Now;
            txtToDate.EditValue = DateTime.Now;
            txtRefDate.EditValue = DateTime.Now;
            txtFromDate.EditValue = DateTime.Now;

            cmbPayType.SelectedIndex = 1;
            cmbProcTypeSrch.SelectedIndex = 0;

            txtInvTaxPer.Text = InvObj.VAT.ToString();

            if (ProcType == 2)
                btnInsertLinkedInv.Visibility = Visibility.Visible;

            FrmPermission.ApplyFrmPermission(this);
            Common.ApplyEditAddPermission(this, ProcCode != 0, FrmPermission);

            dtSeialNo.Columns.Add("ItemId");
            dtSeialNo.Columns.Add("SerialNo");

            txtBarcode.Focus();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if ((_itemsSource.Count > 0 && ProcCode == -1) || IsUpdated)
            {
                var result = DXMessageBox.Show(
                    "·„ Ì „ Õ›Ÿ «·›« Ê—…° Â·  —Ìœ «·«” „—«—ø",
                    " ‰»ÌÂ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.Key == Key.B)
            {
                e.Handled = true;
                txtBarcode.Focus();
            }

            if (e.Key == Key.F1)
                AddNewItem();

            if (e.Key == Key.Return && txtBarcode.IsFocused)
                ReadBarcode();
        }

        #endregion

        #region Load Data

        private void LoadSettings()
        {
            LoadDGvSetting();
        }

        private void LoadData()
        {
            LoadSafes();
            LoadCustomers();
            LoadTreasury();
            LoadSalesMen();
            LoadBanks();
            LoadCostCenters();
            LoadInvNo();
        }

        private void LoadDGvSetting()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select * from CustomizedDGV where Form_id={InvType} order by Column_id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return;

                for (int i = 0; i < dt.Rows.Count && i < dgvItems.Columns.Count; i++)
                {
                    dgvItems.Columns[i].Visibility =
                        Convert.ToBoolean(dt.Rows[i]["IS_Visible"])
                            ? Visibility.Visible
                            : Visibility.Collapsed;

                    string colName = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? dt.Rows[i]["col_name"].ToString()
                        : dt.Rows[i]["col_nameEn"].ToString();

                    dgvItems.Columns[i].Header = colName;

                    int width = Convert.ToInt32(dt.Rows[i]["Width"]);
                    dgvItems.Columns[i].Width =
                        Convert.ToBoolean(dt.Rows[i]["IS_filled"])
                            ? new DataGridLength(1, DataGridLengthUnitType.Star)
                            : new DataGridLength(width);
                }
            }
            catch { }
        }

        public void LoadSafes()
        {
            try
            {
                int empNo = MainClass.EmpNo == 0 ? 1 : MainClass.EmpNo;
                var adapter = new SqlDataAdapter(
                    $"select id,name from Safes,Safe_Emps " +
                    $"where Safes.id=Safe_Emps.safe_id and emp_id={empNo} " +
                    $"and IS_Deleted=0 and status<>2 order by id",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.ItemsSource = dt.DefaultView;
                cmbStore.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadBanks()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select * from Banks where IS_Deleted=0", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBanks.DisplayMemberPath = "name";
                cmbBanks.SelectedValuePath = "id";
                cmbBanks.ItemsSource = dt.DefaultView;
                cmbBanks.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadCustomers()
        {
            try
            {
                int custType = 2;

                if (string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase))
                {
                    lblClient.Text = "«·„Ê—œ";
                    lblClientSrch.Text = "«·„Ê—œ";
                    if (dgvSrch.Columns.Count > 4)
                        dgvSrch.Columns[4].Header = "«·„Ê—œ";
                }
                else
                {
                    lblClient.Text = "Supplier";
                    lblClientSrch.Text = "Supplier";
                    if (dgvSrch.Columns.Count > 4)
                        dgvSrch.Columns[4].Header = "Supplier";
                }

                var adapter = new SqlDataAdapter(
                    $"select id,name from Customers " +
                    $"where (type={custType} or type=3) order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.ItemsSource = dt.DefaultView;
                cmbClient.SelectedIndex = -1;

                var dt2 = new DataTable();
                adapter.Fill(dt2);
                cmbClientSrch.DisplayMemberPath = "name";
                cmbClientSrch.SelectedValuePath = "id";
                cmbClientSrch.ItemsSource = dt2.DefaultView;
                cmbClientSrch.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadTreasury()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select id,name from Stocks,Stock_Emps " +
                    $"where Stocks.id=Stock_Emps.stock_id and emp_id={MainClass.EmpNo} " +
                    $"and IS_Deleted=0 and status<>2 order by id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbTreasury.DisplayMemberPath = "name";
                cmbTreasury.SelectedValuePath = "id";
                cmbTreasury.ItemsSource = dt.DefaultView;
                cmbTreasury.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadSalesMen()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select id,name from salesmen where IS_Deleted=0 order by id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.ItemsSource = dt.DefaultView;
                cmbSalesMen.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadCostCenters()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "select code, name from cost_center where type=2 order by code", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadInvNo()
        {
            txtNo.Text = InvoiceOper.InvoiceNo(InvType, ProcType, InvObj.Prefixe).ToString();
        }

        #endregion

        #region Item Grid Logic

        private void SearchByName()
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;

                SrchName = _itemsSource[RowIndex].ItemName ?? "";

                var adapter = new SqlDataAdapter(
                    $"select name,id from Items where IS_Deleted=0 and name=N'{SrchName}'",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                {
                    AddNewItem();
                }
            }
            catch { }
        }

        private void SearchByCode()
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;

                string code = _itemsSource[RowIndex].ItemCode ?? "";
                SrchName = "";

                var adapter = new SqlDataAdapter(
                    $"select name,id from Items where IS_Deleted=0 and Code=N'{code}'",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                {
                    AddNewItem();
                }
            }
            catch { }
        }

        private void SearchByID(int unitId)
        {
            try
            {
                if (RowIndex < 0 || SelectedId <= 0) return;

                var adapter = new SqlDataAdapter(
                    $"select name,id,code,nameEN from Items where IS_Deleted=0 and id={SelectedId}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) { AddNewItem(); return; }

                // Õ–› «·’› «·Õ«·Ì ≈‰ ﬂ«‰ ›«—€«
                if (RowIndex < _itemsSource.Count && _itemsSource[RowIndex].ItemId <= 0)
                    _itemsSource.RemoveAt(RowIndex);

                //  Õﬁﬁ „‰  ﬂ—«— «·„«œ…
                bool found = false;
                int foundIndex = -1;
                for (int i = 0; i < _itemsSource.Count; i++)
                {
                    if (_itemsSource[i].ItemId == Convert.ToInt32(dt.Rows[0]["id"]))
                    {
                        found = true;
                        foundIndex = i;
                        break;
                    }
                }

                if (found)
                {
                    var msgForm = new MsgExistItem();
                    msgForm.ShowDialog();

                    if (msgForm.Action == 1)
                    {
                        _itemsSource[foundIndex].Quantity += 1;
                        CalcRowValues(foundIndex);
                        CalcTot();
                        return;
                    }
                    else if (msgForm.Action != 2)
                        return;
                }

                // ≈÷«›… ’› ÃœÌœ
                var newRow = new InvItemRow_frmSale
                {
                    ItemId = Convert.ToInt32(dt.Rows[0]["id"]),
                    ItemCode = dt.Rows[0]["code"].ToString(),
                    ItemName = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                                   ? dt.Rows[0]["name"].ToString()
                                   : (string.IsNullOrEmpty(dt.Rows[0]["nameEN"].ToString())
                                       ? dt.Rows[0]["name"].ToString()
                                       : dt.Rows[0]["nameEN"].ToString())
                };

                _itemsSource.Add(newRow);
                RowIndex = _itemsSource.Count - 1;
                LoadItemInf(newRow.ItemId, RowIndex, unitId);
                UpdateRowNumbers();
            }
            catch { }
        }

        private void LoadItemInf(int id, int index, int unitID)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select Items.id,Items.name,Items.barcode,units.name as unit," +
                    $"purch_price,sale_price,tax,discount,ItemProperty,FillValue " +
                    $"from Items,units where Items.unit=units.id and items.id={id}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return;

                LoadUnitInf(index, unitID);

                _itemsSource[index].AvrgCost = ItemOper.Cost(id);
                _itemsSource[index].Stock = CalcStock(id, GetStoreId());
                _itemsSource[index].Quantity = 1;
                _itemsSource[index].StoreId = GetStoreId();
                _itemsSource[index].VatPerc = Convert.ToDouble(dt.Rows[0]["tax"]);
                _itemsSource[index].ItemDiscount = Convert.ToDouble(dt.Rows[0]["discount"]);

                if (rdAuto.IsChecked == true)
                {
                    var calc = new Frm_Calculator();
                    calc.TextBox1.Text = "1";
                    calc.ShowDialog();
                    _itemsSource[index].Quantity = Convert.ToDouble(Properties.Settings.Default.QTY);
                }

                CalcRowValues(index);
                CalcTot();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Œÿ√ √À‰«¡  Õ„Ì· »Ì«‰«  «·’‰›\n" + ex.Message);
            }
        }

        private void LoadUnitInf(int index, int unitId)
        {
            try
            {
                int itemId = _itemsSource[index].ItemId;

                if (unitId == 0)
                    unitId = CheckItemUnit(itemId);

                var adapter = new SqlDataAdapter(
                    $"select purch,sale,barcode,perc,units.name from units,ItemUnits " +
                    $"where ItemUnits.ItemId={itemId} and ItemUnits.unit=units.id and units.id={unitId}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    _itemsSource[index].UnitName = dt.Rows[0]["name"].ToString();
                    _itemsSource[index].Barcode = dt.Rows[0]["barcode"].ToString();
                    _itemsSource[index].UnitEquality = Convert.ToDouble(dt.Rows[0]["perc"]);
                    _itemsSource[index].Quantity = 1;
                    _itemsSource[index].PrimaryQnty =
                        _itemsSource[index].Quantity * _itemsSource[index].UnitEquality;

                    double price = 0;
                    if (InvObj.Pricing == Pricing.Default)
                        price = Convert.ToDouble(dt.Rows[0]["sale"]);
                    else if (InvObj.Pricing == Pricing.SalePrice)
                        price = Convert.ToDouble(dt.Rows[0]["purch"]);
                    else if (InvObj.Pricing == Pricing.PurchasePrice)
                        price = ItemOper.RecentPurchPrice(itemId) *
                                Convert.ToDouble(dt.Rows[0]["perc"]);
                    else if (InvObj.Pricing == Pricing.LastPurchasePrice)
                        price = ItemOper.Cost(itemId) * Convert.ToDouble(dt.Rows[0]["perc"]);
                    else
                        price = Convert.ToDouble(dt.Rows[0]["purch"]);

                    _itemsSource[index].Price = price;
                }
                else
                {
                    // «·ÊÕœ… «·√”«”Ì… ··’‰›
                    var adapter2 = new SqlDataAdapter(
                        $"select units.name as unit,purch_price as purch,sale_price as sale," +
                        $"Items.barcode as barcode from Items,units " +
                        $"where items.unit=units.id and items.id={itemId}",
                        conn);
                    var dt2 = new DataTable();
                    adapter2.Fill(dt2);

                    if (dt2.Rows.Count > 0)
                    {
                        _itemsSource[index].UnitName = dt2.Rows[0]["unit"].ToString();
                        _itemsSource[index].Barcode = dt2.Rows[0]["barcode"].ToString();
                        _itemsSource[index].UnitEquality = 1;
                        _itemsSource[index].Quantity = 1;
                        _itemsSource[index].PrimaryQnty = 1;

                        double price = 0;
                        if (InvObj.Pricing == Pricing.Default)
                            price = Convert.ToDouble(dt2.Rows[0]["sale"]);
                        else if (InvObj.Pricing == Pricing.SalePrice)
                            price = Convert.ToDouble(dt2.Rows[0]["purch"]);
                        else if (InvObj.Pricing == Pricing.PurchasePrice)
                            price = ItemOper.RecentPurchPrice(itemId);
                        else if (InvObj.Pricing == Pricing.LastPurchasePrice)
                            price = ItemOper.Cost(itemId);
                        else
                            price = Convert.ToDouble(dt2.Rows[0]["purch"]);

                        _itemsSource[index].Price = price;
                    }
                }
            }
            catch { }
        }

        private int CheckItemUnit(int itemID)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select id,name from units where defaultInv={InvType}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    int uId = Convert.ToInt32(dt.Rows[0]["id"]);
                    var adapter2 = new SqlDataAdapter(
                        $"select purch,sale from ItemUnits where ItemId={itemID} and unit={uId}",
                        conn);
                    var dt2 = new DataTable();
                    adapter2.Fill(dt2);

                    return dt2.Rows.Count > 0 ? uId : 0;
                }
            }
            catch { }
            return 0;
        }

        private void CalcRowValues(int index)
        {
            if (index < 0 || index >= _itemsSource.Count) return;
            var row = _itemsSource[index];
            if (row.ItemId <= 0) return;

            if (!IsLoaded) IsUpdated = true;

            row.PrimaryQnty = row.Quantity * row.UnitEquality;
            row.SumPrice = Math.Round(row.Quantity * row.Price, 2);

            double afterDisc = Math.Round(row.SumPrice - row.ItemDiscount, 2);
            double vat = 0;

            if (afterDisc > 0)
                vat = CalcVAT(row.ItemId, (decimal)afterDisc);

            if (InvType == 9) vat = 0;
            if (ckZeroVAT.IsChecked == true)
            {
                vat = 0;
                txtInvTaxPer.Text = "0";
            }

            if (InvObj.PriceIncVAT)
                afterDisc -= vat;

            row.VatValue = Math.Round(vat, 2);
            row.TotalPrice = Math.Round(afterDisc, 2);
            row.NetValue = Math.Round(afterDisc + vat, 2);
            row.DiscountPerc = row.SumPrice > 0
                ? Math.Round(row.ItemDiscount / row.SumPrice * 100, 2)
                : 0;
        }

        private double CalcVAT(int itemID, decimal price)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select tax,Purch_price,sale_price from Items where id={itemID}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (Convert.ToInt32(dt.Rows[0]["tax"]) == 0) return 0;

                    if (InvObj.PriceIncVAT)
                        return Convert.ToDouble(price) -
                               Math.Round(Convert.ToDouble(price) / (1 + InvObj.VAT / 100.0), 3);

                    return Math.Round(Convert.ToDouble(price) * (InvObj.VAT / 100.0), 3);
                }
            }
            catch { }
            return 0;
        }

        private int CalcStock(int itemID, int storeId)
        {
            try
            {
                return (int)Math.Round(Inventory.CalcItemStock(storeId, itemID, MainClass.BranchNo));
            }
            catch { return 0; }
        }

        private int GetStoreId()
        {
            try
            {
                if (cmbStore.SelectedValue != null)
                    return Convert.ToInt32(cmbStore.SelectedValue);
            }
            catch { }
            return 1;
        }

        public void CalcTot()
        {
            try
            {
                double sumPrice = 0, totQty = 0, totDisc = 0;
                double netWithoutVat = 0, totVat = 0, netTotal = 0;
                int rowCount = 0;

                foreach (var row in _itemsSource)
                {
                    if (row.ItemId <= 0) continue;
                    sumPrice += row.SumPrice;
                    totQty += row.PrimaryQnty;
                    totDisc += row.ItemDiscount;
                    netWithoutVat += row.TotalPrice;
                    totVat += row.VatValue;
                    netTotal += row.NetValue;
                    rowCount++;
                }

                if (string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                    txtInvDiscVal.Text = "0";

                if (!double.TryParse(txtInvDiscVal.Text, out double invDisc))
                    invDisc = 0;

                if (invDisc > 0)
                {
                    netWithoutVat -= invDisc;
                    totVat = InvObj.PriceIncVAT
                        ? netWithoutVat - Math.Round(netWithoutVat / (1 + InvObj.VAT / 100.0), 3)
                        : Math.Round(netWithoutVat * (InvObj.VAT / 100.0), 3);
                    netTotal = netWithoutVat + totVat;
                }

                txtTirmsNo.Text = rowCount.ToString();
                txtTotQuan.Text = totQty.ToString("N2");
                txtSumVal.Text = sumPrice.ToString("N2");
                txtTotDiscount.Text = (totDisc + invDisc).ToString("N2");
                txtNetWithoutVAT.Text = netWithoutVat.ToString("N2");
                txtTotVAT.Text = Math.Round(totVat, 2).ToString("N2");
                txtNet.Text = Math.Round(netTotal, 2).ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < _itemsSource.Count; i++)
                _itemsSource[i].RowNo = i + 1;
        }

        private void AddNewItem()
        {
            var form = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.sql = "select id, name, nameEN, sale_price, unit from Items where IS_Deleted=0 order by id";
            form.search = "select id, name, nameEN, sale_price, unit from Items";
            form.Itemname = "";
            form.StoreId = GetStoreId();
            form.txtSrchNm.Text = SrchName;

            if (RowIndex >= 0 && RowIndex < _itemsSource.Count)
                form.txtSrchCode.Text = _itemsSource[RowIndex].ItemCode ?? "";

            form.ShowDialog();

            if (form.ISDone && form.ItemId > 0)
            {
                foreach (int itemId in form.Itemlist)
                {
                    SelectedId = itemId;
                    var emptyRow = new InvItemRow_frmSale();
                    _itemsSource.Add(emptyRow);
                    RowIndex = _itemsSource.Count - 1;
                    SearchByID(0);
                }
                RowIndex = _itemsSource.Count - 1;
            }
            else if (RowIndex >= 0 && RowIndex < _itemsSource.Count
                     && _itemsSource[RowIndex].ItemId <= 0)
            {
                _itemsSource.RemoveAt(RowIndex);
            }
        }

        private void DeleteRow()
        {
            if (_itemsSource.Count <= 0) return;

            if (RowIndex < 0 || RowIndex >= _itemsSource.Count)
            {
                DXMessageBox.Show("»—ÃÏ «Œ Ì«— «·”Ã· «·–Ì  —Ìœ Õ–›Â");
                return;
            }

            string msg = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? "Do you want to delete this record?"
                : "Â·  —Ìœ Õ–› «·”Ã·ø";

            if (DXMessageBox.Show(msg, "", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
            {
                _itemsSource.RemoveAt(RowIndex);
                UpdateRowNumbers();
                CalcTot();
            }
        }

        private void ShowItemUnit(int index)
        {
            try
            {
                var form = new frmItemUnits();
                form.ItemId = _itemsSource[index].ItemId;
                form.PreUnit = GetUnitID(_itemsSource[index].UnitName);
                form.ShowDialog();

                if (!string.IsNullOrEmpty(form.Unitname))
                    LoadUnitInf(index, form.UnitId);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Œÿ√ √À‰«¡  Õ„Ì· «·ÊÕœ« \n" + ex.Message);
            }
        }

        private void LoadItemDetails()
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
                var row = _itemsSource[RowIndex];
                if (row.ItemId <= 0) return;

                SelectedId = row.ItemId;

                txtLastSalePrice.Text = "";
                txtCompetitorPrice.Text = "";
                txtCostAvrg.Text = "";
                txtStock.Text = "";
                txtItemBarcode.Text = "";
                txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(SelectedId).ToString();
                txtCostAvrg.Text = (row.AvrgCost * row.UnitEquality).ToString("N2");

                LoadPrices(SelectedId);

                txtStock.Text = row.Stock.ToString();
                txtPerUnit.Text = row.UnitEquality.ToString();
                txtTotUnitQuan.Text = row.PrimaryQnty.ToString();
                txtItemBarcode.Text = row.Barcode ?? "";

                txtStock.Foreground = row.Stock >= 0
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Firebrick;
            }
            catch { }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select * from ItemPrices where Itemid={itemId} order by Proc_id desc",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text = dt.Rows[0]["low_sale_price"].ToString();
                    txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"].ToString();
                }
            }
            catch { }
        }

        private void RecentPurchPrice(int itemId)
        {
            try
            {
                string branchCond = MainClass.BranchNo != -1
                    ? $" branch={MainClass.BranchNo} and " : "";

                var adapter = new SqlDataAdapter(
                    $"select val,val1,exchange_price from inv,inv_sub " +
                    $"where {branchCond} Inv_Sub.ItemId={itemId} and inv.proc_type=1 " +
                    $"and inv.inv_type=1 and inv_sub.proc_type=1 " +
                    $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 order by inv.id desc",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double price = Convert.ToDouble(dt.Rows[0]["val1"])
                                   * Convert.ToDouble(dt.Rows[0]["exchange_price"])
                                   / Convert.ToDouble(dt.Rows[0]["val"]);
                    txtRecentPurchPrice.Text = price.ToString("N2");
                }
            }
            catch { }
        }

        private void ReadBarcode()
        {
            try
            {
                string barcode = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(barcode)) { AddNewItem(); return; }

                // »ÕÀ ›Ì ItemUnits
                var adapter = new SqlDataAdapter(
                    $"select Items.id,ItemUnits.unit from Items,ItemUnits " +
                    $"where Items.IS_Deleted=0 and ItemUnits.ItemId=items.id " +
                    $"and ItemUnits.barcode=N'{barcode}'",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtBarcode.Text = "";
                    var newRow = new InvItemRow_frmSale();
                    _itemsSource.Add(newRow);
                    RowIndex = _itemsSource.Count - 1;
                    SearchByID(Convert.ToInt32(dt.Rows[0]["unit"]));
                    return;
                }

                // »ÕÀ ›Ì Itembarcodes
                var adapter2 = new SqlDataAdapter(
                    $"select items.unit,Itembarcodes.itemId from Items,Itembarcodes " +
                    $"where Items.IS_Deleted=0 and Itembarcodes.ItemId=items.id " +
                    $"and (Itembarcodes.barcode=N'{barcode}' or Items.barcode=N'{barcode}')",
                    conn1);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt2.Rows[0]["itemId"]);
                    var newRow = new InvItemRow_frmSale();
                    _itemsSource.Add(newRow);
                    RowIndex = _itemsSource.Count - 1;
                    SearchByID(Convert.ToInt32(dt2.Rows[0]["unit"]));
                    if (RowIndex < _itemsSource.Count)
                        _itemsSource[RowIndex].Barcode = barcode;
                    txtBarcode.Text = "";
                }
                else
                {
                    var msg = new frmAttentionMsg();
                    msg.lblmsg.Text = "Â–« «·»«—ﬂÊœ €Ì— „ÊÃÊœ ÷„‰ »Ì«‰«  «·»—‰«„Ã";
                    msg.btnClose.Visibility = Visibility.Collapsed;
                    msg.btnInsure.Visibility = Visibility.Collapsed;
                    msg.ShowDialog();
                }
            }
            catch { }
        }

        #endregion

        #region Discount Calculation

        private void CalcDiscount()
        {
            if (!double.TryParse(txtInvDiscVal.Text, out double discVal) || discVal <= 0)
            {
                txtInvDiscPerc.Text = "0";
                txtInvDiscVal.Text = "0";
                InvDiscount = 0;
                CalcTot();
                return;
            }

            if (double.TryParse(txtSumVal.Text, out double sumVal) && sumVal > 0)
            {
                txtInvDiscPerc.Text = (discVal / sumVal * 100).ToString("N4");
                CalcTot();
            }
        }

        private void CalcDiscount2()
        {
            if (!double.TryParse(txtInvDiscPerc.Text, out double discPerc) || discPerc <= 0)
            {
                txtInvDiscPerc.Text = "0";
                txtInvDiscVal.Text = "0";
                InvDiscount = 0;
                CalcTot();
                return;
            }

            if (double.TryParse(txtSumVal.Text, out double sumVal) && sumVal > 0)
            {
                InvDiscount = Math.Round(discPerc / 100.0 * sumVal, 4);
                txtInvDiscVal.Text = InvDiscount.ToString("N4");
                CalcTot();
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            _srchSource.Clear();

            var cmd = new SqlCommand(sqlStr, conn);
            if (conn.State != ConnectionState.Open) conn.Open();
            ReadData(cmd.ExecuteReader());
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) { CLR(); return; }

            dr.Read();
            CLR();

            ISNew = false;
            IsPrinted = true;
            InvGlobalID = dr["InvGlobalID"].ToString();
            RestPurchCode = Convert.ToInt32(dr["EntryID"] != DBNull.Value ? dr["EntryID"] : 0);
            EntryGlobalID = $"{dr["branch"]}-{RestPurchCode}";
            ProcCode = Convert.ToInt32(dr["proc_id"]);
            Code = Convert.ToInt32(dr["id"]);
            txtNo.Text = Code.ToString();
            txtDate.EditValue = Convert.ToDateTime(dr["date"]);
            InvType = Convert.ToInt32(dr["inv_type"]);

            // ’‰œÊﬁ / „” Êœ⁄
            if (dr["stock"] != DBNull.Value)
                cmbTreasury.SelectedValue = dr["stock"].ToString();
            if (dr["safe"] != DBNull.Value)
                cmbStore.SelectedValue = dr["safe"].ToString();

            txtSumVal.Text = dr["InvTotal"].ToString();
            txtInvDiscVal.Text = dr["minus"].ToString();
            txtNet.Text = dr["tot_net"].ToString();
            txtInvTax.Text = dr["tax"].ToString();
            txtTotVAT.Text = dr["tax"].ToString();

            if (double.TryParse(txtNet.Text, out double net) &&
                double.TryParse(txtInvTax.Text, out double tax))
                txtNetWithoutVAT.Text = (net - tax).ToString("N2");

            txtNote.Text = dr["notes"].ToString();

            try
            {
                if (dr["Reff_date"] != DBNull.Value)
                    txtRefDate.EditValue = Convert.ToDateTime(dr["Reff_date"]);
                txtRefNo.Text = dr["Reff_No"].ToString();
            }
            catch { }

            // ‰Ê⁄ «·œ›⁄
            try
            {
                int payType = Convert.ToInt32(dr["pay_type"]);
                if (payType == -1) cmbPayType.SelectedIndex = 0;
                else if (payType == 1) cmbPayType.SelectedIndex = 1;
                else if (payType == 2)
                {
                    cmbPayType.SelectedIndex = 2;
                    if (dr["bank"] != DBNull.Value)
                        cmbBanks.SelectedValue = dr["bank"];
                }
            }
            catch { }

            if (dr["cust_id"] != DBNull.Value)
                cmbClient.SelectedValue = dr["cust_id"].ToString();

            try
            {
                if (dr["salesman"] != DBNull.Value)
                    cmbSalesMen.SelectedValue = dr["salesman"].ToString();
            }
            catch { }

            dr.Close();
            IsLoaded = true;

            //  Õ„Ì· »‰Êœ «·›« Ê—…
            LoadInvSubItems();

            IsLoaded = false;

            // „—ﬂ“ «· ﬂ·›…
            LoadInvCostCenter();

            GenerateQR();
            Common.ApplyEditAddPermission(this, ProcCode != 0, FrmPermission);
        }

        private void LoadInvSubItems()
        {
            var adapter = new SqlDataAdapter(
                $"select * from Inv_Sub where InvGlobalID=N'{InvGlobalID}'", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            foreach (DataRow r in dt.Rows)
            {
                try
                {
                    int itemId = Convert.ToInt32(r["ItemId"]);
                    int unitId = Convert.ToInt32(r["unit"]);

                    double val1 = Convert.ToDouble(r["val1"]);
                    double price = Convert.ToDouble(r["exchange_price"]);
                    double qty = val1;
                    double sum = Math.Round(qty * price, 3);
                    double disc = Convert.ToDouble(r["discount"]);
                    double discP = sum > 0 ? disc / sum * 100 : 0;
                    double vatV = Convert.ToDouble(r["taxval"]);
                    double vatP = Convert.ToDouble(r["taxperc"]);
                    double tot = Math.Round(sum - disc, 2);
                    double net = Math.Round(tot + vatV, 2);

                    if (InvObj.PriceIncVAT) tot -= vatV;

                    double avrg = r["AvrgCost"] != DBNull.Value
                        ? Convert.ToDouble(r["AvrgCost"]) : 0;

                    int storeId = r["store"] != DBNull.Value ? Convert.ToInt32(r["store"]) : 0;

                    //  ⁄«œ· «·ÊÕœ…
                    double perc = 1;
                    var adp2 = new SqlDataAdapter(
                        $"select ItemUnits.perc from Items,ItemUnits " +
                        $"where Items.id=ItemUnits.ItemId and ItemUnits.unit={unitId} " +
                        $"and ItemUnits.ItemId={itemId}",
                        conn);
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                        perc = Convert.ToDouble(dt2.Rows[0][0]);

                    var row = new InvItemRow_frmSale
                    {
                        ItemId = itemId,
                        ItemCode = GetItemCode(itemId),
                        ItemName = GetItemName(itemId),
                        Description = r["Description"].ToString(),
                        UnitName = GetUnitName(unitId),
                        UnitEquality = perc,
                        Quantity = qty,
                        PrimaryQnty = Convert.ToDouble(r["val"]),
                        Price = price,
                        SumPrice = sum,
                        AvrgCost = avrg,
                        ItemDiscount = disc,
                        DiscountPerc = discP,
                        TotalPrice = tot,
                        VatPerc = vatP,
                        VatValue = vatV,
                        NetValue = net,
                        Stock = 0,
                        StoreId = storeId,
                        Barcode = r["Description"].ToString()
                    };

                    _itemsSource.Add(row);
                }
                catch { }
            }

            UpdateRowNumbers();
            CalcTot();
        }

        private void LoadInvCostCenter()
        {
            try
            {
                int accNo = ProcType == 2 ? 3200002 : 3200001;
                var adapter = new SqlDataAdapter(
                    $"select CCcode from Entry_sub where branch={MainClass.BranchNo} " +
                    $"and res_id={RestPurchCode} and acc_no={accNo}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 && dt.Rows[0]["CCcode"] != DBNull.Value)
                    cmbCostCenter.SelectedValue = dt.Rows[0]["CCcode"].ToString();
            }
            catch { }
        }

        #endregion

        #region Search

        private void Search()
        {
            string branchCond = "";
            if (MainClass.BranchNo != -1)
                branchCond = $"inv.branch={MainClass.BranchNo} and ";

            string cond;
            if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                cond = branchCond + $" inv.id={txtSrchNo.Text} and ";
            else if (!string.IsNullOrWhiteSpace(txtSrchreffNo.Text))
                cond = $" inv.Reff_No={txtSrchreffNo.Text} and ";
            else
                cond = branchCond + " date>=@date1 and date<=@date2 and ";

            if (cmbClientSrch.SelectedIndex > -1)
                cond += $" inv.cust_id={cmbClientSrch.SelectedValue} and ";

            LoadDG(cond);
        }

        private void LoadDG(string cond)
        {
            _srchSource.Clear();

            var adapter = new SqlDataAdapter(
                $"select Inv.InvGlobalID,Inv.id as id,Inv.date as date," +
                $"Reff_No,cust_id from Inv " +
                $"where inv_type={InvType} and proc_type={ProcType} " +
                $"and {cond} Inv.IS_Deleted=0 order by Inv.id",
                conn);

            if (!string.IsNullOrWhiteSpace(cond) && cond.Contains("@date"))
            {
                var toDate = Convert.ToDateTime(txtToDate.EditValue).AddHours(24);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                    Convert.ToDateTime(txtFromDate.EditValue).ToShortDateString();
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate;
            }

            var dt = new DataTable();
            adapter.Fill(dt);

            foreach (DataRow r in dt.Rows)
            {
                _srchSource.Add(new SrchInvRow
                {
                    InvGlobalID = r["InvGlobalID"].ToString(),
                    InvNo = r["id"].ToString(),
                    ReffNo = r["Reff_No"].ToString(),
                    InvDate = Convert.ToDateTime(r["date"]).ToShortDateString(),
                    ClientName = GetCustName(Convert.ToInt32(r["cust_id"]))
                });
            }
        }

        #endregion

        #region CLR (Reset Form)

        private void CLR()
        {
            _itemsSource.Clear();
            Code = -1;
            ProcCode = -1;
            SelectedId = -1;
            IsPrinted = false;
            ISNew = true;
            InvGlobalID = "";
            EntryGlobalID = "";
            InvTax = 0;
            InvDiscount = 0;
            TotAfterdisc = 0;

            txtNo.Text = "";
            txtBarcode.Text = "";
            txtNote.Text = "";
            txtSrchDgv.Text = "";
            txtSumVal.Text = "0";
            txtNet.Text = "0";
            txtNetWithoutVAT.Text = "0";
            txtTotDiscount.Text = "0";
            txtTotVAT.Text = "0";
            txtInvDiscVal.Text = "0";
            txtInvDiscPerc.Text = "0";
            txtInvTax.Text = "0";
            txtInvTaxPer.Text = InvObj.VAT.ToString();
            txtTirmsNo.Text = "";
            txtStock.Text = "";
            txtPerUnit.Text = "";
            txtRecentPurchPrice.Text = "";
            txtLastSalePrice.Text = "";
            txtTotUnitQuan.Text = "";
            txtCostAvrg.Text = "";
            txtCompetitorPrice.Text = "";
            txtItemBarcode.Text = "";

            PictureBox1.Source = null;

            txtDate.EditValue = DateTime.Now;
            txtToDate.EditValue = DateTime.Now;
            txtRefDate.EditValue = DateTime.Now;
            txtFromDate.EditValue = DateTime.Now;

            cmbPayType.SelectedIndex = 1;
            cmbProcTypeSrch.SelectedIndex = 0;
            cmbCostCenter.SelectedIndex = -1;

            if (cmbStore.Items.Count > 0) cmbStore.SelectedIndex = 0;
            if (cmbTreasury.Items.Count > 0) cmbTreasury.SelectedIndex = 0;
            if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;

            dtSeialNo.Rows.Clear();
            if (dtSeialNo.Columns.Count > 0) dtSeialNo.Columns.Clear();
            dtSeialNo.Columns.Add("ItemId");
            dtSeialNo.Columns.Add("SerialNo");

            LoadInvNo();
            IsLoaded = false;
            IsUpdated = false;

            Common.ApplyEditAddPermission(this, ProcCode != 0, FrmPermission);

            txtBarcode.Focus();
        }

        private bool CheckBeforeClear()
        {
            if ((_itemsSource.Count > 0 && ProcCode == -1) || IsUpdated)
            {
                string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "·„ Ì „ Õ›Ÿ «·›« Ê—…° Â·  —Ìœ ÃœÌœø"
                    : "You have not saved the invoice. Continue?";

                return DXMessageBox.Show(msg, " ‰»ÌÂ",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No;
            }
            return false;
        }

        #endregion

        #region Save & Print

        private async void SaveAndPrint()
        {
            var invoiceOper = new InvoiceOper();
            var inv = BindToInvoice();
            Entry entry = null;

            if ((ProcType == 1 || ProcType == 2) && InvType == 1)
            {
                entry = invoiceOper.BindToEntry(inv);
                if (entry == null) return;
            }

            Inventory.UpdateItemStock(inv);
            bool saved = invoiceOper.SaveInvoice(inv, entry, ISNew);

            if (saved && IsPrinted && print.PrintNo > 0)
                RptPrint(inv, 1);

            if (saved && Sync.ActiveSync && InvObj.SyncInv && Sync.SyncType > 0)
            {
                if (!InvObj.SyncEntry) entry = null;
                await invoiceOper.SyncInvoice(inv, entry, ISNew);
            }

            if (!saved) return;

            RecalculateCost();

            var savedMsg = new frmSavedMsg();
            if (!ISNew)
            {
                savedMsg.lblSave.Text = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? " „ Õ›Ÿ «· ⁄œÌ·«  »‰Ã«Õ..."
                    : "Successfully updated ...";
            }
            savedMsg.ShowDialog();

            if (savedMsg.Pressed == 1)
                CLR();
            else if (savedMsg.Pressed == 2)
            {
                ISNew = false;
                IsUpdated = false;
            }
            else if (savedMsg.Pressed == 3)
            {
                CLR();
                Close();
            }
            else
                CLR();
        }

        private Invoice BindToInvoice()
        {
            double paycash = 0, payATM = 0;
            int bank = -1, payType = -1;

            if (cmbPayType.SelectedIndex == 0)
            {
                paycash = 0; payATM = 0;
            }
            else if (cmbPayType.SelectedIndex == 1)
            {
                payType = 1;
                double.TryParse(txtNet.Text, out paycash);
                payATM = 0;
            }
            else if (cmbPayType.SelectedIndex == 2)
            {
                payType = 2;
                if (cmbBanks.SelectedValue != null)
                    int.TryParse(cmbBanks.SelectedValue.ToString(), out bank);
                double.TryParse(txtNet.Text, out payATM);
                paycash = 0;
            }

            if (ProcCode == -1)
            {
                GetInvoiceIDs();
                EntryOper.GetEntryGlobalID(ref EntryGlobalID, ref ProcCode);
            }

            double.TryParse(txtNetWithoutVAT.Text, out double netWithout);
            double.TryParse(txtSumVal.Text, out double sumPrice);
            double.TryParse(txtTotVAT.Text, out double totVat);
            double.TryParse(txtNet.Text, out double netTotal);
            double.TryParse(txtInvDiscVal.Text, out double invDisc);
            double.TryParse(txtTotDiscount.Text, out double totDisc);

            var inv = new Invoice
            {
                AutoIncrementID = ProcCode,
                ClientCode = Sync.ClientCode,
                Total = netWithout,
                Delivery = 0,
                SumPrice = sumPrice,
                VAT = Math.Round(totVat, 2),
                Net = Math.Round(netTotal, 2),
                Discount = Math.Round(invDisc, 2),
                TotDiscount = Math.Round(totDisc, 2),
                Additions = 0,
                Insurance = 0,
                Paycash = paycash,
                PayATM = payATM,
                PayType = payType,
                Remainder = 0,
                InvGlobalID = InvGlobalID,
                EntryGlobalID = EntryGlobalID,
                InvoiceNo = int.TryParse(txtNo.Text, out int invNo) ? invNo : 0,
                InvoiceType = (InvoiceType)InvType,
                Bank = bank,
                ProcType = ProcType,
                OrderNo = -1,
                OrderType = -1,
                InvDate = Convert.ToDateTime(txtDate.EditValue),
                User = MainClass.EmpNo,
                Branch = MainClass.BranchNo,
                DistBranch = Sync.DistBranch,
                BranchType = Sync.BranchType,
                Treasury = cmbTreasury.SelectedIndex > -1
                                  ? Convert.ToInt32(cmbTreasury.SelectedValue) : -1,
                InvCCcode = cmbCostCenter.SelectedIndex > -1
                                  ? cmbCostCenter.SelectedValue.ToString() : "-1",
                Saleman = cmbSalesMen.SelectedIndex > -1
                                  ? Convert.ToInt32(cmbSalesMen.SelectedValue) : -1,
                Customer = cmbClient.SelectedIndex > -1
                                  ? Convert.ToInt32(cmbClient.SelectedValue) : 1,
                IsDeleted = false,
                VATperc = InvObj.VAT,
                ReffNo = !string.IsNullOrWhiteSpace(txtRefNo.Text)
                                  ? txtRefNo.Text : "-1",
                RefDate = Convert.ToDateTime(txtRefDate.EditValue),
                InvAccCode = ProcType == 1 ? InvObj.InvAcc : InvObj.InvReturnAcc,
                Store = cmbStore.SelectedIndex > -1
                                  ? Convert.ToInt32(cmbStore.SelectedValue) : 1,
                Currency = InvObj.Currency
            };

            inv.InvNote = !string.IsNullOrWhiteSpace(txtNote.Text)
                ? txtNote.Text
                : InvoiceOper.GetInvoiceType((int)inv.InvoiceType, inv.ProcType,
                      inv.PayType, 1) + " »—ﬁ„ " + inv.InvoiceNo;

            var items = new List<Item>();
            foreach (var row in _itemsSource)
            {
                if (row.ItemId <= 0) continue;

                var item = new Item
                {
                    InvGlobalID = InvGlobalID,
                    ClientCode = Sync.ClientCode,
                    AutoIncrementID = ProcCode,
                    Code = row.ItemCode,
                    Name = row.ItemName,
                    ItemNo = row.ItemId,
                    ProcType = ProcType,
                    Description = row.Description ?? "",
                    Quantity = row.Quantity,
                    UnitEquality = row.UnitEquality,
                    PrimaryQnty = row.PrimaryQnty,
                    Unit = GetUnitID(row.UnitName),
                    Price = row.Price,
                    ItemDiscount = row.ItemDiscount,
                    AvegCost = ItemOper.ItemDiscountValue(
                                        new decimal(inv.SumPrice),
                                        new decimal(inv.Discount),
                                        row.Price * row.Quantity,
                                        new decimal(row.PrimaryQnty),
                                        new decimal(row.ItemDiscount)),
                    Vat = row.VatValue,
                    VatPerc = row.VatPerc,
                    Barcode = row.Barcode ?? "",
                    ValiableStock = row.Stock + row.PrimaryQnty,
                    Store = row.StoreId,
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = "",
                    ProductId = 0
                };

                row.AvrgCost = item.AvegCost;
                items.Add(item);

                // Õ›Ÿ «·√—ﬁ«„ «· ”·”·Ì…
                try
                {
                    if (dtSeialNo.Rows.Count > 0)
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
                        new SqlCommand(
                            $"delete from ItemSerialNo where ItemId={row.ItemId} " +
                            $"and InvGlobalID=N'{InvGlobalID}'",
                            conn).ExecuteNonQuery();

                        foreach (DataRow sr in dtSeialNo.Rows)
                        {
                            if (sr["ItemId"].ToString() == row.ItemId.ToString())
                                SaveSeialNo(Convert.ToInt32(sr["ItemId"]),
                                            sr["SerialNo"].ToString(), InvGlobalID);
                        }
                    }
                }
                catch { }
            }

            inv.Items = items;
            return inv;
        }

        private void GetInvoiceIDs()
        {
            if (string.IsNullOrWhiteSpace(txtNetWithoutVAT.Text))
                txtNetWithoutVAT.Text = "0";

            if (conn.State != ConnectionState.Open) conn.Open();

            if (ProcCode == -1)
            {
                ProcCode = Convert.ToInt32(
                    new SqlCommand("select ISNULL(MAX(proc_id),0) from Inv", conn).ExecuteScalar()) + 1;
                LoadInvNo();
                int.TryParse(txtNo.Text, out Code);

                if (double.TryParse(txtNetWithoutVAT.Text, out double v) && v != 0)
                {
                    RestPurchCode = Convert.ToInt32(
                        new SqlCommand(
                            $"select ISNULL(MAX(id),0) from Entry where branch={MainClass.BranchNo}",
                            conn).ExecuteScalar()) + 1;
                }
            }
        }

        private void RptPrint(Invoice inv, int type)
        {
            if (_itemsSource.Count == 0)
            {
                DXMessageBox.Show("·«  ÊÃœ ⁄„·Ì«  ‘—«¡ √Ê »Ì⁄ »«·ÃœÊ·");
                return;
            }

            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("ÌÃ»  ÕœÌœ „”«— «· ﬁ—Ì—");
                return;
            }

            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "RptInvPurchase.repx";

            string path = Path.Combine(print.RptUrl, print.RptName);

            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                DXMessageBox.Show("«·„”«— «·Õ«·Ì ·· ﬁ«—Ì— €Ì— „ÊÃÊœ √Ê  „  ⁄œÌ·Â", "Œÿ√");
                return;
            }

            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(type, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        private void RecalculateCost()
        {
            foreach (var row in _itemsSource)
            {
                if (row.ItemId <= 0) continue;
                double avgCost = row.AvrgCost;
                ItemOper.RecalculateCost(row.ItemId, row.PrimaryQnty, ref avgCost, "");
            }
        }

        private void SaveSeialNo(int itemId, string serialNo, string invGlobalID)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var cmd = new SqlCommand(
                    "INSERT into ItemSerialNo(ItemId,SerialNo,InvGlobalID) " +
                    "values(@ItemId,@SerialNo,@InvGlobalID)", conn);
                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                cmd.Parameters.Add("@SerialNo", SqlDbType.NVarChar).Value = serialNo;
                cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalID;
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        #endregion

        #region QR Code

        private void GenerateQR()
        {
            try
            {
                // »‰«¡ ‰’ QR
                var sb = new StringBuilder();
                sb.Append($"—ﬁ„ «·›« Ê—…: {txtNo.Text} | ");
                sb.Append($"«· «—ÌŒ: {Convert.ToDateTime(txtDate.EditValue).ToShortDateString()} | ");
                sb.Append($"«·„Ã„Ê⁄: {txtSumVal.Text} | ");
                sb.Append($"«·÷—Ì»…: {txtInvTax.Text} | ");
                sb.Append($"«·’«›Ì: {txtNet.Text}");

                // ≈‰‘«¡ QR
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(
                                            sb.ToString(),
                                            QRCoder.QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCoder.PngByteQRCode(qrData);

                byte[] qrBytes = qrCode.GetGraphic(4); // ÕÃ„ ﬂ· ÊÕœ… »ﬂ”·

                using var ms = new MemoryStream(qrBytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                PictureBox1.Source = bmp;
            }
            catch
            {
                PictureBox1.Source = null;
            }
        }

        #endregion

        #region Helper Methods

        private string GetItemName(int id)
        {
            try
            {
                var adapter = new SqlDataAdapter($"select name from Items where id={id}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetItemCode(int id)
        {
            try
            {
                var adapter = new SqlDataAdapter($"select Code from Items where id={id}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetUnitName(int id)
        {
            try
            {
                var adapter = new SqlDataAdapter($"select name from units where id={id}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        public int GetUnitID(string name)
        {
            try
            {
                var adapter = new SqlDataAdapter($"select id from units where name='{name}'", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch { return -1; }
        }

        private string GetCustName(int cust)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select name from Customers where id={cust}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private bool IsInvExist()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select id from Inv where branch={MainClass.BranchNo} " +
                    $"and inv_type={InvType} and proc_type={ProcType} and id={txtNo.Text}",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void ShowCustBalance()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"select Code from Accounts_Index where AName=N'{cmbClient.Text}' " +
                    $"{Accounting.BranchCondition}",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accCond = $" and Entry_sub.acc_no={dt.Rows[0][0]}";
                string branchCond = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} and Entry_sub.branch={MainClass.BranchNo} and "
                    : "";

                var adapter2 = new SqlDataAdapter(
                    $"select sum(Entry_sub.dept) as dept, sum(Entry_sub.credit) as credit " +
                    $"from Entry,Entry_sub where {branchCond} Entry.IS_Deleted=0 " +
                    $"and Entry.state=1 and Entry.GlobalId=Entry_sub.EntryGlobalId {accCond}",
                    conn1);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    double dept = Convert.ToDouble(dt2.Rows[0]["dept"]);
                    double credit = Convert.ToDouble(dt2.Rows[0]["credit"]);

                    if (dept > credit)
                        txtBalance.Text = Math.Round(dept - credit, 3).ToString("N3");
                    else if (credit > dept)
                        txtBalance.Text = Math.Round(credit - dept, 3).ToString("N3") + " œ«∆‰";
                    else
                        txtBalance.Text = "0";
                }
            }
            catch { }
        }

        private string GetSalesEmpNameByInvNo()
        {
            try
            {
                if (ProcCode != -1)
                {
                    var adapter = new SqlDataAdapter(
                        $"select users.username from Employees,Inv,users " +
                        $"where users.emp=Employees.id and Employees.id=Inv.sales_emp " +
                        $"and proc_id={ProcCode}",
                        conn1);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
                return MainClass.UserName;
            }
            catch { return ""; }
        }

        private void SrchByNameClient()
        {
            try
            {
                SrchName = cmbClient.Text;
                var adapter = new SqlDataAdapter(
                    $"select id,name from Customers where IS_Deleted=0 and name=N'{SrchName}' " +
                    $"and (type=1 or type=3)",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbClient.SelectedValue = dt.Rows[0]["id"].ToString();
                else
                    AddNewClient();
            }
            catch { }
        }

        private void AddNewClient()
        {
            try
            {
                var form = new frmSrchClient();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Type = 2;
                form.txtClientName.Text = cmbClient.Text;
                if (cmbPayType.SelectedIndex == 0)
                    form.PostponeClient = true;
                form.ShowDialog();

                if (!string.IsNullOrEmpty(form.Clientname))
                {
                    LoadCustomers();
                    cmbClient.SelectedValue = form.ClientId.ToString();
                }
            }
            catch { }
        }

        private void CalcDiscPerc(int index)
        {
            if (index < 0 || index >= _itemsSource.Count) return;
            var row = _itemsSource[index];
            row.DiscountPerc = row.SumPrice > 0
                ? row.ItemDiscount / row.SumPrice * 100
                : 0;
        }

        #endregion

        #region Button Click Handlers

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear()) CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if ((ProcCode > -1 && ProcType == 2) &&
                InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
            {
                DXMessageBox.Show("«·›« Ê—…  „ ≈—Ã«⁄Â« ”«»ﬁ«");
                return;
            }

            IsPrinted = false;

            if (!double.TryParse(txtNet.Text, out double net) || (net <= 0 && InvType != 9))
            {
                DXMessageBox.Show("ÌÃ» √‰ ÌﬂÊ‰ «·’«›Ì √ﬂ»— „‰ «·’›—");
                return;
            }

            if (cmbPayType.SelectedIndex == 0 && cmbClient.SelectedIndex == -1)
            {
                DXMessageBox.Show("ÌÃ» «Œ Ì«— «·„Ê—œ");
                return;
            }

            SaveAndPrint();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            if ((ProcCode > -1 && ProcType == 2) &&
                InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
            {
                DXMessageBox.Show("«·›« Ê—…  „ ≈—Ã«⁄Â« ”«»ﬁ«");
                return;
            }

            IsPrinted = true;

            if (cmbPayType.SelectedIndex == 0 && cmbClient.SelectedIndex == -1)
            {
                DXMessageBox.Show("ÌÃ» «Œ Ì«— «·„Ê—œ");
                return;
            }

            SaveAndPrint();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (IsPrinted)
                RptPrint(BindToInvoice(), 1);
            else
                DXMessageBox.Show("·« Ì„ﬂ‰ ÿ»«⁄… «·›« Ê—… ﬁ»· «·Õ›Ÿ");
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            RptPrint(BindToInvoice(), 2);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("«Œ — ›« Ê—… ·Ì „ Õ–›Â«");
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                if (DXMessageBox.Show("Â· √‰  „ √ﬂœ „‰ «·Õ–›ø", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new SqlCommand(
                        $"update Inv set IS_Deleted=1 where InvGlobalID=N'{InvGlobalID}'",
                        conn).ExecuteNonQuery();

                    if (RestPurchCode != 0)
                        new SqlCommand(
                            $"update Entry set IS_Deleted=1,state=2 where GlobalID=N'{EntryGlobalID}'",
                            conn).ExecuteNonQuery();

                    string msg = string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase) ? " „ «·Õ–›" : "Deleted";
                    DXMessageBox.Show(msg);

                    _srchSource.Clear();
                    CLR();
                }
            }
            catch (Exception ex)
            {
                string msg = string.Equals(MainClass.Language, "en",
                    StringComparison.OrdinalIgnoreCase)
                    ? $"error in delete\nerror details: {ex.Message}"
                    : $"Œÿ√ √À‰«¡ «·Õ–›\n ›«’Ì· «·Œÿ√: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                         $"and proc_type={ProcType} and IS_Deleted=0 " +
                         $"and branch={MainClass.BranchNo} order by id asc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int no);
                Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                         $"and proc_type={ProcType} and IS_Deleted=0 and id>{no} " +
                         $"and branch={MainClass.BranchNo} order by id asc");
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int no);
                Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                         $"and proc_type={ProcType} and IS_Deleted=0 and id<{no} " +
                         $"and branch={MainClass.BranchNo} order by id desc");
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                         $"and proc_type={ProcType} and IS_Deleted=0 " +
                         $"and branch={MainClass.BranchNo} order by id desc");
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            var form = new frmInvoiceSrch();
            form.cmbProcType.IsEnabled = false;
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ProcType = ProcType;
            form.InvType = InvType;
            form.ShowDialog();

            if (form.ISDone &&
                !string.Equals(form.InvGlobalID, "-1", StringComparison.OrdinalIgnoreCase))
            {
                Navigate($"select * from Inv where branch={MainClass.BranchNo} " +
                         $"and InvGlobalID=N'{form.InvGlobalID}'");
            }
        }

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new frmItems();
            form.Activate();
            form.ShowDialog();
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmCustomers();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                if (InvType == 2) { form.Title = " ⁄—Ì› ⁄„Ì·"; form.Type = 1; }
                else if (InvType == 1) { form.Title = " ⁄—Ì› „Ê—œ"; form.Type = 2; }

                form.ShowDialog();
                if (form.isDone) LoadCustomers();
            }
            catch { }
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int prevId = -1;
            if (cmbSalesMen.SelectedValue != null)
                int.TryParse(cmbSalesMen.SelectedValue.ToString(), out prevId);

            var form = new frmSalesMen();
            form.Activate();
            form.ShowDialog();
            LoadSalesMen();

            try { if (prevId > -1) cmbSalesMen.SelectedValue = prevId.ToString(); }
            catch { }
        }

        private void addCostCenter_Click(object sender, RoutedEventArgs e)
        {
            int prevId = -1;
            if (cmbCostCenter.SelectedValue != null)
                int.TryParse(cmbCostCenter.SelectedValue.ToString(), out prevId);

            var form = new frmCostCenter();
            form.Activate();
            form.ShowDialog();
            LoadCostCenters();

            try { if (prevId > -1) cmbCostCenter.SelectedValue = prevId.ToString(); }
            catch { }
        }

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            // „‰ÿﬁ «·≈œ—«Ã - Ìıﬂ„· Õ”» «·„ ÿ·»« 
        }

        private void BtnReceipts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist()) { DXMessageBox.Show("·„ Ì „ Õ›Ÿ «·›« Ê—…"); return; }

                var form = new frmSandD();
                form.Show();
                form.cmbSupplies.SelectedValue = cmbClient.SelectedValue;
                form.txtVal.Text = txtNet.Text;
                form.txtNotes.Text = txtNote.Text + "   Œ«’… «·„Ê—œ:" + cmbClient.Text;
            }
            catch { }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvItemRow_frmSale row)
            {
                int idx = _itemsSource.IndexOf(row);
                if (idx >= 0)
                {
                    if (DXMessageBox.Show("Â·  —Ìœ Õ–› «·»‰œø", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                    {
                        _itemsSource.RemoveAt(idx);
                        UpdateRowNumbers();
                        CalcTot();
                    }
                }
            }
        }

        private void btnItemOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.ContextMenu ??= dgvItems.ContextMenu;
                if (btn.ContextMenu != null)
                {
                    btn.ContextMenu.PlacementTarget = btn;
                    btn.ContextMenu.IsOpen = true;
                }
            }
        }

        #endregion

        #region Context Menu Handlers

        private void StripItemSearch_Click(object sender, RoutedEventArgs e)
        {
            var emptyRow = new InvItemRow_frmSale();
            _itemsSource.Add(emptyRow);
            RowIndex = _itemsSource.Count - 1;
            AddNewItem();
        }

        private void StripItemDetail_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var form = new frmItems();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ItemId = _itemsSource[RowIndex].ItemId;
            form.ShowDialog();
        }

        private void StripAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var form = new frmItems();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        private void StripAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var form = new frmItemCategory();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        private void StripItemUnits_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex >= 0 && RowIndex < _itemsSource.Count)
                ShowItemUnit(RowIndex);
        }

        private void StripdeleteRow_Click(object sender, RoutedEventArgs e) => DeleteRow();

        private void StripItemProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var row = _itemsSource[RowIndex];
            var form = new frmRptItemsActivityDetailed();
            form.SelectedId = row.ItemId;
            form.txtItemName.Text = row.ItemName;
            form.txtItemCode.Text = row.ItemCode;
            form.ShowResult();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        private void StripClientLastItem_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var row = _itemsSource[RowIndex];
            var form = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.Activate();
            form.Show();
            if (cmbClient.SelectedIndex > -1)
                form.LastProcess(row.ItemId, row.StoreId,
                    Convert.ToInt32(cmbClient.SelectedValue), 1);
        }

        private void StripLastItem_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var row = _itemsSource[RowIndex];
            var form = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.Activate();
            form.Show();
            form.LastProcess(row.ItemId, row.StoreId, -1, 1);
        }

        private void StripItemAvrgCost_Click1(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var row = _itemsSource[RowIndex];
           // if (row.ItemId > 0)
              //  DXMessageBox.Show(ItemOper.AvgCost(row.ItemId, MainClass.BranchNo),
             //       "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StripItemAvrgCost_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var row = _itemsSource[RowIndex];
            if (row.ItemId > 0)
            {
                double avgCost = ItemOper.AvgCost(row.ItemId, MainClass.BranchNo);
                DXMessageBox.Show(avgCost.ToString("F2"),
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StripSerialNo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
                var row = _itemsSource[RowIndex];
                var form = new frmItemSerialNo();
                var dt = new DataTable();
                dt.Columns.Add("DgvNo");
                dt.Columns.Add("DgvSerialNo");

                if (Code == -1)
                {
                    int i = 0;
                    foreach (DataRow sr in dtSeialNo.Rows)
                    {
                        if (sr["ItemId"].ToString() == row.ItemId.ToString())
                            dt.Rows.Add(i++, sr["SerialNo"]);
                    }
                }
                else
                {
                    var adapter = new SqlDataAdapter(
                        $"Select SerialNo from ItemSerialNo where ItemId={row.ItemId} " +
                        $"and InvGlobalID=N'{InvGlobalID}'", conn);
                    var dt2 = new DataTable();
                    adapter.Fill(dt2);
                    int i = 0;
                    foreach (DataRow r in dt2.Rows)
                        dt.Rows.Add(i++, r["SerialNo"]);
                }

                form.operType = 3;
                form.LoadDgvEdit(dt);
                form.ShowDialog();

                if (form.ISDone && form.TotQty > 0)
                {
                    dtSeialNo.Rows.Clear();
                    row.Quantity = form.TotQty;
                    foreach (var s in form.ItemSerialNolist)
                        dtSeialNo.Rows.Add(row.ItemId, s);
                    CalcRowValues(RowIndex);
                    CalcTot();
                }
            }
            catch { }
        }

        private void btnCustomizeDisplay_Click(object sender, RoutedEventArgs e)
        {
            var form = new frmCustomizeShow();
            form.cmbInv.SelectedIndex = InvType == 9 ? 3 : 0;
            form.cmbInv.IsEnabled = false;
            form.ShowDialog();
            if (form.ISDone) LoadDGvSetting();
        }

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DXMessageBox.Show("Â· √‰  „ √ﬂœ „‰ «” Ì—«œ «·»Ì«‰« ø", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

                var form = new frmImportDataGeneral();
                form.cmbInv.SelectedIndex = 1;
                form.cmbInv.Visibility = Visibility.Visible;
                form.cmbInv.IsEnabled = false;
                form.cmbDataTable.Visibility = Visibility.Collapsed;
                form.Activate();
                form.ShowDialog();

                if (form.ISDone && form.dtInvSel.Rows.Count > 0)
                {
                    foreach (DataRow r in form.dtInvSel.Rows)
                    {
                        var emptyRow = new InvItemRow_frmSale
                        {
                            ItemCode = r["ItemCode"].ToString(),
                            Description = r["description"].ToString(),
                            UnitName = Common.GetUnitName(Convert.ToInt32(r["unit"])),
                            Quantity = Convert.ToDouble(r["quntity"])
                        };

                        if (double.TryParse(r["price"].ToString(), out double price) && price > 0)
                            emptyRow.Price = price;

                        _itemsSource.Add(emptyRow);
                        RowIndex = _itemsSource.Count - 1;
                        SearchByCode();
                    }
                }
            }
            catch { }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            object excel = null;
            object workbook = null;
            object worksheet = null;

            try
            {
                if (!IsInvExist())
                {
                    DXMessageBox.Show("ÌÃ» Õ›Ÿ «·›« Ê—… ﬁ»· «· ’œÌ—");
                    return;
                }

                if (DXMessageBox.Show("Â· √‰  „ √ﬂœ „‰  ’œÌ— «·»Ì«‰« ø", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = "Export.xlsx"
                };

                if (dlg.ShowDialog() != true)
                    return;

                string filePath = dlg.FileName;

                var excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    DXMessageBox.Show("Excel €Ì— „À»  ⁄·Ï Â–« «·ÃÂ«“");
                    return;
                }

                excel = Activator.CreateInstance(excelType);
                dynamic excelApp = excel;
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;

                workbook = excelApp.Workbooks.Add();
                dynamic wb = workbook;

                worksheet = wb.Sheets[1];
                dynamic ws = worksheet;

                // —ƒÊ” «·√⁄„œ…
                string[] headers =
                {
            "#",
            "—„“ «·’‰›",
            "—ﬁ„ «·’‰›",
            "«”„ «·’‰›",
            "«·Ê’›",
            "«·ÊÕœ…",
            "«·ﬂ„Ì…",
            "«·”⁄—",
            "«·„Ã„Ê⁄",
            "Œ’„ «·’‰›",
            "«·≈Ã„«·Ì",
            "«·÷—Ì»…",
            "«·’«›Ì"
        };

                for (int columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                {
                    ws.Cells[1, columnIndex + 1] = headers[columnIndex];
                }

                // «·»Ì«‰« 
                for (int rowIndex = 0; rowIndex < _itemsSource.Count; rowIndex++)
                {
                    var row = _itemsSource[rowIndex];

                    ws.Cells[rowIndex + 2, 1] = row.RowNo;
                    ws.Cells[rowIndex + 2, 2] = row.ItemCode;
                    ws.Cells[rowIndex + 2, 3] = row.ItemId;
                    ws.Cells[rowIndex + 2, 4] = row.ItemName;
                    ws.Cells[rowIndex + 2, 5] = row.Description;
                    ws.Cells[rowIndex + 2, 6] = row.UnitName;
                    ws.Cells[rowIndex + 2, 7] = row.Quantity;
                    ws.Cells[rowIndex + 2, 8] = row.Price;
                    ws.Cells[rowIndex + 2, 9] = row.SumPrice;
                    ws.Cells[rowIndex + 2, 10] = row.ItemDiscount;
                    ws.Cells[rowIndex + 2, 11] = row.TotalPrice;
                    ws.Cells[rowIndex + 2, 12] = row.VatValue;
                    ws.Cells[rowIndex + 2, 13] = row.NetValue;
                }

                //  ‰”Ìﬁ »”Ìÿ
                ws.Columns.AutoFit();

                wb.SaveAs(filePath);
                wb.Close(false);
                excelApp.Quit();

                DXMessageBox.Show(" „ Õ›Ÿ «·„·› ›Ì: " + filePath);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("Œÿ√\n" + ex.Message);
            }
            finally
            {
                ReleaseComObject(worksheet);
                ReleaseComObject(workbook);
                ReleaseComObject(excel);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private void ReleaseComObject(object comObject)
        {
            try
            {
                if (comObject != null && Marshal.IsComObject(comObject))
                {
                    Marshal.FinalReleaseComObject(comObject);
                }
            }
            catch
            {
            }
        }

        private void BtnShowEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist()) { DXMessageBox.Show("·„ Ì „ Õ›Ÿ «·›« Ê—…"); return; }
                var form = new frmRptEntries();
                form.Show();
                form.Navigate($"select * from Entry where IS_Deleted=0 and GlobalID=N'{EntryGlobalID}'");
            }
            catch { }
        }

        private void btnShortCutInv_Click(object sender, RoutedEventArgs e)
        {
            var form = new FrmShortCutInv();
            form.InvVAT = InvObj.VAT;
            form.InvIncluVAT = InvObj.PriceIncVAT;
            form.ShowDialog();

            if (double.TryParse(form.txtNet.Text, out double n) && n > 0)
            {
                txtSumVal.Text = form.txtSumVal.Text;
                txtTotDiscount.Text = form.txtTotDiscount.Text;
                txtTotVAT.Text = form.txtVAT.Text;
                txtNetWithoutVAT.Text = form.txtTotal.Text;
                txtNet.Text = form.txtNet.Text;
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvItems.SelectedIndex >= 0)
            {
                RowIndex = dgvItems.SelectedIndex;
                LoadItemDetails();
            }
        }

        private void dgvItems_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvItems.SelectedIndex >= 0)
            {
                RowIndex = dgvItems.SelectedIndex;
                LoadItemDetails();
            }
        }

        private void dgvItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is not InvItemRow_frmSale row) return;
            int index = _itemsSource.IndexOf(row);
            RowIndex = index;

            string colHeader = e.Column.Header?.ToString() ?? "";

            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (colHeader.Contains("«·ﬂ„Ì…") || colHeader.Contains("«·”⁄—") ||
                        colHeader.Contains("«·Œ’„") || colHeader.Contains("‰”»… «·Œ’„"))
                    {
                        CalcRowValues(index);
                        CalcTot();
                    }

                    if (colHeader.Contains("«·„” Êœ⁄"))
                    {
                        if (row.ItemId > 0 && row.StoreId > 0)
                            row.Stock = CalcStock(row.ItemId, row.StoreId);
                    }

                    if (colHeader.Contains("«·’‰›") && row.ItemId <= 0)
                        SearchByName();

                    if (colHeader.Contains("—„“") && row.ItemId <= 0)
                        SearchByCode();
                }
                catch { }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void dgvItems_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgvItems.SelectedIndex >= 0)
                RowIndex = dgvItems.SelectedIndex;
        }

        private void dgvItems_PreparingCellForEdit(object sender,
            DataGridPreparingCellForEditEventArgs e)
        {
            // „‰ÿﬁ ≈÷«›Ì ⁄‰œ »œ¡ «· Õ—Ì— ≈‰ ·“„
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is not SrchInvRow row) return;

            if (ISreturn)
            {
                InvGlobalID = row.InvGlobalID;
                var adapter = new SqlDataAdapter(
                    $"select id from Inv where proc_type=2 and inv_type={InvType} " +
                    $"and Reff_No=N'{InvGlobalID}' and IS_Deleted=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count >= 1)
                {
                    DXMessageBox.Show("«·›« Ê—…  „ ≈—Ã«⁄Â« ”«»ﬁ«");
                    return;
                }
            }

            Navigate($"select * from Inv where branch={MainClass.BranchNo} " +
                     $"and InvGlobalID=N'{row.InvGlobalID}'");
            TabControl1.SelectedIndex = 0;
        }

        #endregion

        #region Toolbar / Input Events

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) ReadBarcode();
        }

        private void txtSrchDgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            string srchText = txtSrchDgv.Text;

            for (int i = 0; i < _itemsSource.Count; i++)
            {
                if (_itemsSource[i].ItemCode == srchText ||
                    _itemsSource[i].Barcode == srchText)
                {
                    dgvItems.SelectedIndex = i;
                    dgvItems.ScrollIntoView(_itemsSource[i]);
                    txtSrchDgv.Text = "";
                    return;
                }
            }
            DXMessageBox.Show("€Ì— „ÊÃÊœ");
        }

        private void cmbPayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBanks == null || cmbTreasury == null) return;

            switch (cmbPayType.SelectedIndex)
            {
                case 0: // ¬Ã·
                    cmbBanks.IsEnabled = false;
                    cmbTreasury.IsEnabled = false;
                    cmbTreasury.SelectedIndex = -1;
                    cmbClient.SelectedIndex = -1;
                    BtnReceipts.Visibility = Visibility.Visible;
                    break;
                case 1: // ‰ﬁœÌ
                    cmbBanks.IsEnabled = false;
                    cmbTreasury.IsEnabled = true;
                    if (cmbTreasury.Items.Count > 0) cmbTreasury.SelectedIndex = 0;
                    BtnReceipts.Visibility = Visibility.Collapsed;
                    break;
                case 2: // »‰ﬂ
                    cmbBanks.IsEnabled = true;
                    cmbTreasury.IsEnabled = false;
                    cmbTreasury.SelectedIndex = -1;
                    cmbBanks.SelectedIndex = -1;
                    BtnReceipts.Visibility = Visibility.Collapsed;
                    cmbBanks.Focus();
                    break;
            }
        }

        private void cmbClient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowCustBalance();
        }

        private void cmbClient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SrchByNameClient();
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbClientSrch == null) return;
            if (chkAll.IsChecked == true)
            {
                cmbClientSrch.SelectedIndex = -1;
                cmbClientSrch.IsEnabled = false;
            }
            else
            {
                cmbClientSrch.IsEnabled = true;
            }
        }

        private void ckZeroVAT_CheckedChanged(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _itemsSource.Count; i++)
                CalcRowValues(i);
            CalcTot();
        }

        private void txtInvDiscVal_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                CalcDiscount();
        }

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
            {
                txtInvDiscPerc.Text = "0";
                CalcDiscount();
            }
        }

        private void txtInvDiscPerc_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
            {
                txtInvDiscVal.Text = "0";
                CalcDiscount2();
            }
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && !string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
            {
                txtInvDiscVal.Text = "0";
                CalcDiscount2();
            }
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    DXMessageBox.Show("«·Õﬁ· ·« Ìﬁ»· ≈·« «·√—ﬁ«„ ›ﬁÿ");
                    return;
                }
            }
        }

        private void txtNet_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var form = new FrmCalcVAT { ItemVAT = (int)Math.Round(InvObj.VAT) };
            form.ShowDialog();

            if (form.Price > 0 &&
                double.TryParse(txtSumVal.Text, out double sum) &&
                double.TryParse(form.txtPriceWithOutVAT.Text, out double withoutVat))
            {
                txtInvDiscVal.Text = (sum - withoutVat).ToString("N4");
                CalcDiscount();
            }
        }

        #endregion
    }

    #region Model Classes

    public class InvItemRow_frmSale : INotifyPropertyChanged
    {
        private int _rowNo;
        private string _itemCode = "";
        private int _itemId;
        private string _itemName = "";
        private string _description = "";
        private string _barcode = "";
        private string _unitName = "";
        private double _unitEquality = 1;
        private double _quantity = 1;
        private double _primaryQnty;
        private double _price;
        private double _sumPrice;
        private double _avrgCost;
        private double _itemDiscount;
        private double _discountPerc;
        private double _totalPrice;
        private double _vatPerc;
        private double _vatValue;
        private double _netValue;
        private double _stock;
        private int _storeId;

        public int RowNo { get => _rowNo; set { _rowNo = value; OnPropertyChanged(nameof(RowNo)); } }
        public string ItemCode { get => _itemCode; set { _itemCode = value; OnPropertyChanged(nameof(ItemCode)); } }
        public int ItemId { get => _itemId; set { _itemId = value; OnPropertyChanged(nameof(ItemId)); } }
        public string ItemName { get => _itemName; set { _itemName = value; OnPropertyChanged(nameof(ItemName)); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(nameof(Description)); } }
        public string Barcode { get => _barcode; set { _barcode = value; OnPropertyChanged(nameof(Barcode)); } }
        public string UnitName { get => _unitName; set { _unitName = value; OnPropertyChanged(nameof(UnitName)); } }
        public double UnitEquality { get => _unitEquality; set { _unitEquality = value; OnPropertyChanged(nameof(UnitEquality)); } }
        public double Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(nameof(Quantity)); } }
        public double PrimaryQnty { get => _primaryQnty; set { _primaryQnty = value; OnPropertyChanged(nameof(PrimaryQnty)); } }
        public double Price { get => _price; set { _price = value; OnPropertyChanged(nameof(Price)); } }
        public double SumPrice { get => _sumPrice; set { _sumPrice = value; OnPropertyChanged(nameof(SumPrice)); } }
        public double AvrgCost { get => _avrgCost; set { _avrgCost = value; OnPropertyChanged(nameof(AvrgCost)); } }
        public double ItemDiscount { get => _itemDiscount; set { _itemDiscount = value; OnPropertyChanged(nameof(ItemDiscount)); } }
        public double DiscountPerc { get => _discountPerc; set { _discountPerc = value; OnPropertyChanged(nameof(DiscountPerc)); } }
        public double TotalPrice { get => _totalPrice; set { _totalPrice = value; OnPropertyChanged(nameof(TotalPrice)); } }
        public double VatPerc { get => _vatPerc; set { _vatPerc = value; OnPropertyChanged(nameof(VatPerc)); } }
        public double VatValue { get => _vatValue; set { _vatValue = value; OnPropertyChanged(nameof(VatValue)); } }
        public double NetValue { get => _netValue; set { _netValue = value; OnPropertyChanged(nameof(NetValue)); } }
        public double Stock { get => _stock; set { _stock = value; OnPropertyChanged(nameof(Stock)); } }
        public int StoreId { get => _storeId; set { _storeId = value; OnPropertyChanged(nameof(StoreId)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SrchInvRow
    {
        public string InvGlobalID { get; set; } = "";
        public string InvNo { get; set; } = "";
        public string ReffNo { get; set; } = "";
        public string InvDate { get; set; } = "";
        public string ClientName { get; set; } = "";
    }

    #endregion
}