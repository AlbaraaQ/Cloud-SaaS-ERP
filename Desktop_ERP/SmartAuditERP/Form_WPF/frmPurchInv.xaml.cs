using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPurchInv : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private int ProcCode;
        private int defUnit;
        private int defTreasury;
        private string TreasuryName;
        private double defVAT;
        private double defDelviry;
        private double defInsurance;
        private bool PricIncVAT;
        private bool SaleByMinus;
        private int CostType;
        private string defPrinter;
        private bool isPrintd;
        private string RptName;
        private string RptUrl;
        private Print print;
        private string InvGlobalID;
        private int RowIndex;
        private int SelectedId;
        public string SrchName;
        private int RestPurchCode;
        public int RestrType;
        public int InvType;
        public int ProcType;
        private int DGV_Count;
        private bool ISTRialEnd;
        private bool ItemNotClicked;
        public double Itemsumdiscount;
        private double InvTax;
        private double InvDiscount;
        private double TotAfterdisc;
        private bool linkedInvRet;
        private bool ISreturn;
        private bool flag;

        // مصدر بيانات الجدول
        private ObservableCollection<PurchInvRow> _gridSource;

        #endregion

        #region Constructor

        public frmPurchInv()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            ProcCode = -1;
            SaleByMinus = true;
            CostType = 1;
            isPrintd = false;
            RptName = "";
            RptUrl = "";
            print = new Print(1);
            InvGlobalID = "";
            RowIndex = -1;
            SelectedId = -1;
            SrchName = "";
            RestPurchCode = -1;
            InvType = 1;
            DGV_Count = 0;
            ISTRialEnd = false;
            ItemNotClicked = true;
            InvTax = 0.0;
            InvDiscount = 0.0;
            TotAfterdisc = 0.0;
            linkedInvRet = false;
            ISreturn = false;
            flag = false;

            _gridSource = new ObservableCollection<PurchInvRow>();
            GridControl1.ItemsSource = _gridSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData1();
            txtDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtRefDate.SelectedDate = DateTime.Today;
            txtFromDate.SelectedDate = DateTime.Today;
            cmbProcTypeSrch.SelectedIndex = 0;
            cmbPayType.SelectedIndex = 1;
            txtBarcode.Focus();
            txtInvTaxPer.Text = defVAT.ToString();

            if (ProcType == 2)
                btnInsertLinkedInv.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_gridSource.Count > 0)
            {
                if (DXMessageBox.Show("لم يتم حفظ الفاتورة، هل تريد الاستمرار", "تنبيه",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F1)
                addNewItem();
            else if (e.Key == Key.Return && txtBarcode.IsFocused)
                ReadBarcode();
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
            {
                e.Handled = true;
                txtBarcode.Focus();
            }
        }

        #endregion

        #region Load Data

        private void LoadData1()
        {
            loadMainSettings();
            LoadSafes();
            LoadCustomers();
            LoadTreasury();
            LoadSalesMen();
            LoadBanks();
            LoadCostCenters();
            LoadInvNo();
        }

        private void loadMainSettings()
        {
            try
            {
                var adp = new SqlDataAdapter(
                    "select * from SettingGeneral where Inv_Id=1", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count != 1) return;

                PricIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                defUnit = Convert.ToInt32(dt.Rows[0]["unit"]);
                defDelviry = Convert.ToDouble(dt.Rows[0]["DeliveryVal"]);
                defInsurance = Convert.ToDouble(dt.Rows[0]["InsureVal"]);
                defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                SaleByMinus = Convert.ToBoolean(dt.Rows[0]["SaleByMinus"]);

                if (dt.Rows[0]["CostType"] != null &&
                    double.Parse(dt.Rows[0]["CostType"].ToString()) > 0)
                    CostType = Convert.ToInt32(dt.Rows[0]["CostType"]);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    cmbTreasury.SelectedValue = dt.Rows[0]["Treasury"];
                    defTreasury = Convert.ToInt32(dt.Rows[0]["Treasury"]);
                    txtInvTaxPer.Text = defVAT.ToString();
                }));
            }
            catch { }
        }

        private void LoadBanks()
        {
            try
            {
                var ds = LoadData.Banks();
                cmbBanks.ItemsSource = ds.DefaultView;
                cmbBanks.DisplayMemberPath = "name";
                cmbBanks.SelectedValuePath = "id";
                cmbBanks.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadSalesMen()
        {
            var ds = LoadData.SalesMen();
            cmbSalesMen.ItemsSource = ds.DefaultView;
            cmbSalesMen.DisplayMemberPath = "name";
            cmbSalesMen.SelectedValuePath = "id";
            cmbSalesMen.SelectedIndex = -1;
        }

        private void LoadCustomers()
        {
            int type = 2;
            if (string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase))
            {
                lblClient.Text = "المورد";
                lblClientSrch.Text = "المورد";
            }
            else
            {
                lblClient.Text = "Supplier";
                lblClientSrch.Text = "Supplier";
            }

            var dt = LoadData.Customers(type);
            if (dt.Rows.Count > 0)
            {
                cmbClient.ItemsSource = dt.DefaultView;
                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.SelectedIndex = -1;

                cmbClientSrch.ItemsSource = dt.DefaultView;
                cmbClientSrch.DisplayMemberPath = "name";
                cmbClientSrch.SelectedValuePath = "id";
                cmbClientSrch.SelectedIndex = -1;
            }
        }

        public void LoadTreasury()
        {
            var ds = LoadData.Treasury();
            cmbTreasury.ItemsSource = ds.DefaultView;
            cmbTreasury.DisplayMemberPath = "name";
            cmbTreasury.SelectedValuePath = "id";
            cmbTreasury.SelectedIndex = -1;
        }

        public void LoadSafes()
        {
            var dt = LoadData.Invertories(MainClass.EmpNo);
            cmbStore.ItemsSource = dt.DefaultView;
            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath = "id";
            cmbStore.SelectedIndex = -1;
        }

        private void LoadCostCenters()
        {
            try
            {
                var ds = LoadData.CostCenters();
                cmbCostCenter.ItemsSource = ds.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadInvNo()
        {
            try
            {
                if (conn1.State != ConnectionState.Open) conn1.Open();
                txtNo.Text = "";
                int num = (int)Math.Round(double.Parse(
                    new SqlCommand(
                        $"select max(id) from Inv where branch={MainClass.BranchNo} " +
                        $"and inv_type={InvType} and proc_type={ProcType}", conn1)
                        .ExecuteScalar()?.ToString() ?? "0") + 1.0);
                txtNo.Text = num.ToString();
                txtNo.Background = System.Windows.Media.Brushes.Firebrick;
                txtNo.Foreground = System.Windows.Media.Brushes.White;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Grid Helpers

        private PurchInvRow GetRowAt(int index)
        {
            if (index >= 0 && index < _gridSource.Count)
                return _gridSource[index];
            return null;
        }

        private object GetRowCellValue(int index, string column)
        {
            var row = GetRowAt(index);
            if (row == null) return null;
            return typeof(PurchInvRow).GetProperty(column)?.GetValue(row);
        }

        private void SetRowCellValue(int index, string column, object value)
        {
            var row = GetRowAt(index);
            if (row == null) return;
            typeof(PurchInvRow).GetProperty(column)?.SetValue(row, value?.ToString() ?? "");
        }

        private int GetFocusedRowIndex()
        {
            if (GridControl1.SelectedItem is PurchInvRow row)
                return _gridSource.IndexOf(row);
            return -1;
        }

        private DataTable setData()
        {
            var dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemCodeDgv");
            dt.Columns.Add("ItemIdDgv");
            dt.Columns.Add("ItemNameDgv");
            dt.Columns.Add("DescriptionDgv");
            dt.Columns.Add("barcodeDgv1");
            dt.Columns.Add("UnitDgv");
            dt.Columns.Add("QuantDgv");
            dt.Columns.Add("PercentUnitDgv");
            dt.Columns.Add("totQuantDgv");
            dt.Columns.Add("priceDgv");
            dt.Columns.Add("SumPriceDgv");
            dt.Columns.Add("AvgCostDgv");
            dt.Columns.Add("discountValDgv");
            dt.Columns.Add("discountPerDgv");
            dt.Columns.Add("totPriceDgv");
            dt.Columns.Add("VatPerDgv");
            dt.Columns.Add("VATValDgv");
            dt.Columns.Add("NetValDgv");
            dt.Columns.Add("StockDgv");
            dt.Columns.Add("StoreDgv", typeof(int));
            return dt;
        }

        #endregion

        #region Calculations

        private void CalcRowValues(int index)
        {
            try
            {
                var row = GetRowAt(index);
                if (row == null || string.IsNullOrEmpty(row.ItemIdDgv)) return;

                int.TryParse(row.ItemIdDgv, out int itemId);
                double.TryParse(row.QuantDgv, out double qty);
                double.TryParse(row.PercentUnitDgv, out double perc);
                double.TryParse(row.priceDgv, out double price);
                double.TryParse(row.discountValDgv, out double disc);

                double totQty = qty * perc;
                double sumPrice = Math.Round(qty * price, 2);
                double totPrice = Math.Round(sumPrice - disc, 2);

                double vatVal = 0.0;
                if (totPrice > 0.0)
                    vatVal = CalcVAT(itemId, (decimal)totPrice);

                if (InvType == 9)
                    vatVal = 0.0;

                if (ckZeroVAT.IsChecked == true)
                {
                    vatVal = 0.0;
                    txtInvTaxPer.Text = "0";
                }

                if (PricIncVAT)
                    totPrice -= vatVal;

                row.totQuantDgv = totQty.ToString("0.##");
                row.SumPriceDgv = sumPrice.ToString("0.##");
                row.VATValDgv = Math.Round(vatVal, 2).ToString("0.##");
                row.totPriceDgv = Math.Round(totPrice, 2).ToString("0.##");
                row.NetValDgv = Math.Round(totPrice + vatVal, 2).ToString("0.##");
            }
            catch { }
        }

        public void CalcTot()
        {
            try
            {
                double sumPrice = 0, totQuant = 0, disc = 0;
                double netVal = 0, vatVal = 0, totPrice = 0, avgCost = 0;

                foreach (var row in _gridSource)
                {
                    if (string.IsNullOrEmpty(row.ItemIdDgv)) continue;
                    double.TryParse(row.SumPriceDgv, out double sp);
                    double.TryParse(row.totQuantDgv, out double tq);
                    double.TryParse(row.discountValDgv, out double dv);
                    double.TryParse(row.NetValDgv, out double nv);
                    double.TryParse(row.VATValDgv, out double vv);
                    double.TryParse(row.totPriceDgv, out double tp);
                    double.TryParse(row.AvgCostDgv, out double ac);

                    sumPrice += sp;
                    totQuant += tq;
                    disc += dv;
                    netVal += nv;
                    vatVal += vv;
                    totPrice += tp;
                    avgCost += ac * tq;
                }

                if (string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                    txtInvDiscVal.Text = "0";

                double.TryParse(txtInvDiscVal.Text, out double invDisc);

                if (invDisc > 0)
                {
                    totPrice -= invDisc;
                    if (!PricIncVAT)
                        vatVal = Math.Round(totPrice * (defVAT / 100.0), 3);
                    else
                        vatVal = totPrice - Math.Round(totPrice / (1.0 + defVAT / 100.0), 3);
                    netVal = totPrice + vatVal;
                }

                txtTirmsNo.Text = _gridSource.Count.ToString();
                txtTotQuan.Text = totQuant.ToString("0.##");
                txtSumVal.Text = $"{Math.Round(sumPrice, 2):0.##}";
                txtTotDiscount.Text = $"{Math.Round(disc + invDisc, 2):0.##}";
                txtNetWithoutVAT.Text = $"{Math.Round(totPrice, 2):0.##}";
                txtTotVAT.Text = $"{Math.Round(vatVal, 2):0.##}";
                txtNet.Text = $"{Math.Round(netVal, 2):0.##}";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private double CalcVAT(int itemId, decimal price)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select tax,Purch_price,sale_price from Items where id={itemId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (Convert.ToDouble(dt.Rows[0]["tax"]) == 0) return 0.0;
                    if (decimal.Compare(price, 0m) == 0)
                        price = InvType == 1
                            ? Convert.ToDecimal(dt.Rows[0]["Purch_price"])
                            : Convert.ToDecimal(dt.Rows[0]["sale_price"]);

                    if (PricIncVAT)
                        return (double)price - Math.Round((double)price / (1.0 + defVAT / 100.0), 3);
                    return Math.Round((double)price * (defVAT / 100.0), 3);
                }
            }
            catch { }
            return 0.0;
        }

        private int CalcStock(int itemId, int storeId)
        {
            try
            {
                return (int)Math.Round(Inventory.CalcItemStock(storeId, itemId, MainClass.BranchNo));
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
                return 0;
            }
        }

        #endregion

        #region Item Search & Load

        private void SearchByCode()
        {
            try
            {
                if (RowIndex <= -1) return;
                string code = GetRowAt(RowIndex)?.ItemCodeDgv ?? "";
                SrchName = "";

                var adp = new SqlDataAdapter(
                    $"select name,id from Items where IS_Deleted=0 and Code=N'{code}'", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                    addNewItem();
            }
            catch { }
        }

        private void SearchByName()
        {
            try
            {
                if (RowIndex <= -1) return;
                string name = GetRowAt(RowIndex)?.ItemNameDgv ?? "";
                SrchName = name;

                var adp = new SqlDataAdapter(
                    $"select name,id from Items where IS_Deleted=0 and name=N'{name}'", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                    addNewItem();
            }
            catch { }
        }

        private void SearchByID(int unitId)
        {
            try
            {
                if (RowIndex <= -1 || SelectedId <= 0) return;

                var adp = new SqlDataAdapter(
                    $"select name,id,code,nameEN from Items " +
                    $"where IS_Deleted=0 and id={SelectedId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count == 0) { addNewItem(); return; }

                bool found = false;
                int foundIndex = -1;

                for (int i = 0; i < _gridSource.Count; i++)
                {
                    if (_gridSource[i].ItemIdDgv == dt.Rows[0]["id"].ToString())
                    {
                        found = true;
                        foundIndex = i;
                        break;
                    }
                }

                if (found)
                {
                    var msgExist = new MsgExistItem();
                    msgExist.ShowDialog();
                    if (msgExist.Action == 1)
                    {
                        double.TryParse(_gridSource[foundIndex].QuantDgv, out double q);
                        _gridSource[foundIndex].QuantDgv = (q + 1).ToString();
                        if (RowIndex != foundIndex && RowIndex >= 0 && RowIndex < _gridSource.Count)
                            _gridSource.RemoveAt(RowIndex);
                        CalcRowValues(foundIndex);
                        CalcTot();
                    }
                    else if (msgExist.Action == 2)
                    {
                        RowIndex = _gridSource.Count - 1;
                        FillRowFromDt(RowIndex, dt.Rows[0], unitId);
                    }
                    return;
                }

                // إضافة صنف جديد
                if (RowIndex < 0 || RowIndex >= _gridSource.Count)
                {
                    _gridSource.Add(new PurchInvRow());
                    RowIndex = _gridSource.Count - 1;
                }

                FillRowFromDt(RowIndex, dt.Rows[0], unitId);
            }
            catch { }
        }

        private void FillRowFromDt(int index, DataRow dr, int unitId)
        {
            var row = GetRowAt(index);
            if (row == null) return;

            row.ItemIdDgv = dr["id"].ToString();
            row.ItemCodeDgv = dr["code"].ToString();
            row.ItemNameDgv = string.Equals(MainClass.Language, "ar",
                StringComparison.OrdinalIgnoreCase)
                ? dr["name"].ToString()
                : (dr["nameEN"].ToString() != "" ? dr["nameEN"].ToString() : dr["name"].ToString());
            row.No = (index + 1).ToString();

            LoadItemInf(Convert.ToInt32(dr["id"]), index, unitId);
        }

        private void LoadItemInf(int id, int index, int unitId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select Items.id,Items.name,Items.barcode,units.name as unit," +
                    $"purch_price,sale_price,tax,discount from Items,units " +
                    $"where Items.unit=units.id and items.id={id}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count == 0) return;

                LoadUnitInf(index, unitId);

                var row = GetRowAt(index);
                if (row == null) return;

                row.No = (index + 1).ToString();
                row.AvgCostDgv = ItemOper.Cost(id).ToString();
                row.StockDgv = CalcStock(id,
                    int.TryParse(cmbStore.SelectedValue?.ToString(), out int sv) ? sv : 0)
                    .ToString();
                row.QuantDgv = "1";
                row.StoreDgv = cmbStore.SelectedValue?.ToString() ?? "0";
                row.VatPerDgv = dt.Rows[0]["tax"].ToString();
                row.discountValDgv = dt.Rows[0]["discount"].ToString();

                if (rdAuto.IsChecked == true)
                {
                    var calc = new Frm_Calculator { };
                    calc.TextBox1.Text = "1";
                    calc.ShowDialog();
                    row.QuantDgv = Properties.Settings.Default.QTY.ToString();
                }

                CalcRowValues(index);
                CalcTot();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void LoadUnitInf(int index, int unitId)
        {
            try
            {
                var row = GetRowAt(index);
                if (row == null) return;
                int.TryParse(row.ItemIdDgv, out int itemId);

                if (unitId == 0 && string.IsNullOrEmpty(row.UnitDgv))
                    unitId = CheckItemUnit(itemId);

                var adp = new SqlDataAdapter(
                    $"select purch,sale,barcode,perc,units.name from units,ItemUnits " +
                    $"where ItemUnits.ItemId={itemId} and ItemUnits.unit=units.id " +
                    $"and units.id={unitId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    row.UnitDgv = dt.Rows[0]["name"].ToString();
                    row.barcodeDgv1 = dt.Rows[0]["barcode"].ToString();
                    row.PercentUnitDgv = dt.Rows[0]["perc"].ToString();
                    row.QuantDgv = "1";
                    double.TryParse(row.QuantDgv, out double q);
                    double.TryParse(row.PercentUnitDgv, out double p);
                    row.totQuantDgv = Math.Round(q * p, 2).ToString();
                    row.priceDgv = InvType == 1
                        ? dt.Rows[0]["purch"].ToString()
                        : dt.Rows[0]["sale"].ToString();
                }
                else
                {
                    var adp2 = new SqlDataAdapter(
                        $"select units.name as unit,purch_price as purch,sale_price as sale," +
                        $"Items.barcode as barcode from Items,units " +
                        $"where items.unit=units.id and items.id={itemId}", conn);
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                    {
                        row.UnitDgv = dt2.Rows[0]["unit"].ToString();
                        row.barcodeDgv1 = dt2.Rows[0]["barcode"].ToString();
                        row.PercentUnitDgv = "1";
                        row.QuantDgv = "1";
                        row.totQuantDgv = "1";
                        row.priceDgv = InvType == 1
                            ? dt2.Rows[0]["purch"].ToString()
                            : dt2.Rows[0]["sale"].ToString();
                    }
                }
            }
            catch { }
        }

        private int CheckItemUnit(int itemId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select id,name from units where defaultInv={InvType}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var adp2 = new SqlDataAdapter(
                        $"select purch,sale from ItemUnits " +
                        $"where ItemId={itemId} and unit={dt.Rows[0]["id"]}", conn);
                    var dt2 = new DataTable();
                    adp2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                        return Convert.ToInt32(dt.Rows[0]["id"]);
                }
            }
            catch { }
            return 0;
        }

        private void ShowItemUnit(int index)
        {
            try
            {
                int.TryParse(GetRowCellValue(index, "ItemIdDgv")?.ToString(), out int itemId);
                var frm = new frmItemUnits
                {
                    ItemId = itemId,
                    PreUnit = Common.GetUnitID(GetRowCellValue(index, "UnitDgv")?.ToString() ?? "")
                };
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Unitname))
                    LoadUnitInf(index, frm.UnitId);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الوحدات\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void addNewItem()
        {
            var frm = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.sql = "select id,name,nameEN,sale_price,unit from Items where IS_Deleted=0 order by id";
            frm.search = "select id,name,nameEN,sale_price,unit from Items";
            frm.Itemname = "";
            frm.StoreId = int.TryParse(cmbStore.SelectedValue?.ToString(), out int sv) ? sv : 0;
            frm.txtSrchNm.Text = SrchName;
            if (RowIndex >= 0 && RowIndex < _gridSource.Count)
                frm.txtSrchCode.Text = _gridSource[RowIndex].ItemCodeDgv;
            frm.ShowDialog();

            if (frm.ISDone && frm.ItemId > 0)
            {
                foreach (int id in frm.Itemlist)
                {
                    SelectedId = id;
                    if (RowIndex < 0 || RowIndex >= _gridSource.Count)
                    {
                        _gridSource.Add(new PurchInvRow());
                        RowIndex = _gridSource.Count - 1;
                    }
                    SearchByID(0);
                }
            }
            else if (frm.ISDone && frm.ItemId <= 0)
            {
                if (RowIndex >= 0 && RowIndex < _gridSource.Count)
                    _gridSource.RemoveAt(RowIndex);
            }
            else
            {
                try
                {
                    if (RowIndex >= 0 && RowIndex < _gridSource.Count)
                        _gridSource.RemoveAt(RowIndex);
                }
                catch { }
            }
        }

        #endregion

        #region Barcode

        private void ReadBarcode()
        {
            try
            {
                string barcode = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(barcode)) { addNewItem(); return; }

                // البحث في ItemUnits
                var adp = new SqlDataAdapter(
                    $"select Items.id,ItemUnits.unit from Items,ItemUnits " +
                    $"where Items.IS_Deleted=0 and ItemUnits.ItemId=items.id " +
                    $"and (ItemUnits.barcode=N'{barcode}' or Items.barcode=N'{barcode}')",
                    conn1);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtBarcode.Text = "";
                    _gridSource.Add(new PurchInvRow());
                    RowIndex = _gridSource.Count - 1;
                    SearchByID(Convert.ToInt32(dt.Rows[0]["unit"]));
                    return;
                }

                // البحث في Itembarcodes
                var adp2 = new SqlDataAdapter(
                    $"select items.unit,Itembarcodes.itemId from Items,Itembarcodes " +
                    $"where Items.IS_Deleted=0 and Itembarcodes.ItemId=items.id " +
                    $"and (Itembarcodes.barcode=N'{barcode}' or Items.barcode=N'{barcode}')",
                    conn1);
                var dt2 = new DataTable();
                adp2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt2.Rows[0]["itemId"]);
                    RowIndex = _gridSource.Count - 1;
                    SearchByID(Convert.ToInt32(dt2.Rows[0]["unit"]));
                    if (RowIndex >= 0 && RowIndex < _gridSource.Count)
                        _gridSource[RowIndex].barcodeDgv1 = barcode;
                    txtBarcode.Text = "";
                }
                else
                {
                    DXMessageBox.Show("هذا الباركود غير موجود ضمن بيانات البرنامج", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch { }
        }

        #endregion

        #region Navigate & ReadData

        public void Navigate(string sqlStr)
        {
            dgvSrch.Items.Refresh();
            var cmd = new SqlCommand(sqlStr, conn);
            if (conn.State != ConnectionState.Open) conn.Open();
            ReadData(cmd.ExecuteReader());
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) { CLR(); return; }
                dr.Read();

                CLR();
                _gridSource.Clear();

                txtNo.Background = System.Windows.Media.Brushes.WhiteSmoke;
                txtNo.Foreground = System.Windows.Media.Brushes.Black;
                isPrintd = true;

                ProcCode = Convert.ToInt32(dr["proc_id"]);
                Code = Convert.ToInt32(dr["id"]);
                txtNo.Text = Code.ToString();
                txtDate.SelectedDate = Convert.ToDateTime(dr["date"].ToString());
                InvType = Convert.ToInt32(dr["inv_type"]);

                cmbStore.SelectedValue = dr["safe"].ToString();
                txtSumVal.Text = dr["InvTotal"].ToString();
                txtNet.Text = dr["tot_net"].ToString();
                txtInvDiscVal.Text = dr["minus"].ToString();
                txtInvTax.Text = dr["tax"].ToString();
                txtNote.Text = dr["notes"].ToString();
                RestPurchCode = Convert.ToInt32(dr["EntryID"]);

                try
                {
                    txtRefDate.SelectedDate = Convert.ToDateTime(dr["Reff_date"].ToString());
                    txtRefNo.Text = dr["Reff_No"].ToString();
                }
                catch { }

                try
                {
                    double payType = Convert.ToDouble(dr["pay_type"]);
                    if (payType == -1) cmbPayType.SelectedIndex = 0;
                    else if (payType == 1) cmbPayType.SelectedIndex = 1;
                    else if (payType == 2)
                    {
                        cmbPayType.SelectedIndex = 2;
                        cmbBanks.SelectedValue = dr["bank"];
                    }
                }
                catch { }

                cmbClient.SelectedValue = dr["cust_id"].ToString();
                try { cmbSalesMen.SelectedValue = dr["salesman"].ToString(); } catch { }

                dr.Close();

                // تحميل الأصناف
                var adp = new SqlDataAdapter(
                    $"select * from Inv_Sub where proc_id={ProcCode}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    try
                    {
                        int itemId = Convert.ToInt32(dt.Rows[i]["ItemId"]);
                        int unitId = Convert.ToInt32(dt.Rows[i]["unit"]);
                        double val1 = Convert.ToDouble(dt.Rows[i]["val1"]);
                        double exPr = Convert.ToDouble(dt.Rows[i]["exchange_price"]);
                        double sumP = Math.Round(val1 * exPr, 2);
                        double disc = double.Parse(dt.Rows[i]["discount"].ToString());
                        double taxV = Convert.ToDouble(dt.Rows[i]["taxval"]);
                        double totP = Math.Round(sumP - disc, 2);
                        if (PricIncVAT) totP -= taxV;

                        decimal perc = 1m;
                        string bc = "";
                        var adpU = new SqlDataAdapter(
                            $"select ItemUnits.perc,ItemUnits.barcode from Items,ItemUnits " +
                            $"where Items.id=ItemUnits.ItemId and ItemUnits.unit={dt.Rows[i]["unit"]} " +
                            $"and ItemUnits.ItemId={dt.Rows[i]["ItemId"]}", conn);
                        var dtU = new DataTable();
                        adpU.Fill(dtU);
                        if (dtU.Rows.Count > 0)
                        {
                            perc = new decimal(Convert.ToDouble(dtU.Rows[0][0]));
                            bc = dtU.Rows[0]["barcode"].ToString();
                        }

                        double avrgCost = (dt.Rows[i]["AvrgCost"] == null ||
                            dt.Rows[i]["AvrgCost"] == DBNull.Value)
                            ? 0.0 : Convert.ToDouble(dt.Rows[i]["AvrgCost"]);

                        string storeVal = "-1";
                        if (dt.Rows[i]["store"] != null &&
                            dt.Rows[i]["store"] != DBNull.Value &&
                            double.Parse(dt.Rows[i]["store"].ToString()) > 0)
                            storeVal = Convert.ToInt32(dt.Rows[i]["store"]).ToString();

                        var newRow = new PurchInvRow
                        {
                            No = (i + 1).ToString(),
                            ItemCodeDgv = GetItemCode(itemId),
                            ItemIdDgv = itemId.ToString(),
                            ItemNameDgv = GetItemName(itemId),
                            DescriptionDgv = dt.Rows[i]["Description"].ToString(),
                            barcodeDgv1 = bc,
                            UnitDgv = GetUnitName(unitId),
                            QuantDgv = val1.ToString(),
                            PercentUnitDgv = perc.ToString(),
                            totQuantDgv = dt.Rows[i]["val"].ToString(),
                            priceDgv = $"{exPr:0.00}",
                            SumPriceDgv = $"{sumP:0.00}",
                            AvgCostDgv = $"{avrgCost:0.00}",
                            discountValDgv = $"{disc:0.00}",
                            discountPerDgv = sumP > 0 ? $"{disc / sumP * 100:0.00}" : "0",
                            totPriceDgv = $"{totP:0.00}",
                            VatPerDgv = dt.Rows[i]["taxperc"].ToString(),
                            VATValDgv = $"{taxV:0.00}",
                            NetValDgv = $"{totP + taxV:0.00}",
                            StockDgv = "0",
                            StoreDgv = storeVal
                        };
                        _gridSource.Add(newRow);
                    }
                    catch { }
                }

                // تحميل مركز التكلفة
                int accNo = ProcType == 2 ? 3200002 : 3200001;
                var adpCC = new SqlDataAdapter(
                    $"select CCcode from Entry_Sub where branch={MainClass.BranchNo} " +
                    $"and res_id={RestPurchCode} and acc_no={accNo}", conn);
                var dtCC = new DataTable();
                adpCC.Fill(dtCC);
                if (dtCC.Rows.Count > 0 && dtCC.Rows[0]["CCcode"] != DBNull.Value)
                    cmbCostCenter.SelectedValue = Convert.ToInt32(dtCC.Rows[0]["CCcode"]);

                CalcTot();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الفاتورة\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save

        private void Save()
        {
            if (_gridSource.Count == 0)
            {
                DXMessageBox.Show("لا يوجد بنود في الفاتورة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                isPrintd = false;
                return;
            }
            if (conn.State != ConnectionState.Open) conn.Open();
            if (conn1.State != ConnectionState.Open) conn1.Open();

            var sqlTransaction = conn.BeginTransaction();
            try
            {
                if (MainClass.IsTrial && ProcCode == -1)
                {
                    var adp = new SqlDataAdapter("select id from Entry", conn1);
                    var dt = new DataTable();
                    adp.Fill(dt);
                    if (dt.Rows.Count >= 20)
                    {
                        DXMessageBox.Show("نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية", "",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        isPrintd = false;
                        return;
                    }
                }

                if (cmbStore.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار المخزن", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbStore.Focus();
                    isPrintd = false;
                    return;
                }
                if (cmbClient.SelectedIndex == -1 && InvType == 1)
                {
                    DXMessageBox.Show("يجب اختيار المورد", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClient.Focus();
                    isPrintd = false;
                    return;
                }

                // الحصول على رمز حساب المورد
                int custAccCode = 0;
                var adpAcc = new SqlDataAdapter(
                    $"select Code from Accounts_Index where type=2 and AName=N'{cmbClient.Text}'",
                    conn1);
                var dtAcc = new DataTable();
                adpAcc.Fill(dtAcc);
                if (dtAcc.Rows.Count > 0)
                    custAccCode = Convert.ToInt32(dtAcc.Rows[0][0]);
                else if (cmbPayType.SelectedIndex == 0)
                {
                    DXMessageBox.Show("يجب إختيار مورد آجل", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClient.Focus();
                    isPrintd = false;
                    return;
                }

                // الخزينة
                int treasuryAccCode = 0, treasuryId = -1;
                if (cmbPayType.SelectedIndex == 1 && InvType == 1)
                {
                    if (cmbTreasury.SelectedIndex == -1)
                    {
                        DXMessageBox.Show("يجب اختيار الخزنة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        cmbTreasury.Focus();
                        isPrintd = false;
                        return;
                    }
                    var adpTr = new SqlDataAdapter(
                        $"select Acc_Code from Stocks where id={cmbTreasury.SelectedValue} " +
                        $"and IS_Deleted=0 and branch={MainClass.BranchNo}", conn1);
                    var dtTr = new DataTable();
                    adpTr.Fill(dtTr);
                    if (dtTr.Rows.Count > 0)
                    {
                        treasuryAccCode = Convert.ToInt32(dtTr.Rows[0][0]);
                        treasuryId = Convert.ToInt32(cmbTreasury.SelectedValue);
                    }
                }
                else if (cmbPayType.SelectedIndex == 2 && cmbBanks.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار البنك أو تغيير طريقة الدفع", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbBanks.Focus();
                    isPrintd = false;
                    return;
                }

                if (ISreturn) ProcCode = -1;

                int entryId = 0;
                string branchCond = MainClass.BranchNo != -1
                    ? $" where branch={MainClass.BranchNo}" : "";

                double.TryParse(txtNet.Text, out double netVal);

                if (ProcCode == -1)
                {
                    if (netVal != 0.0)
                        entryId = (int)Math.Round(
                            double.Parse(new SqlCommand(
                                $"select max(id) from Entry{branchCond}",
                                conn, sqlTransaction).ExecuteScalar()?.ToString() ?? "0") + 1.0);
                }
                else
                {
                    StoreEditHistory();
                    entryId = RestPurchCode;
                    new SqlCommand($"delete from Entry where id={entryId}", conn, sqlTransaction).ExecuteNonQuery();
                    new SqlCommand($"delete from Entry_Sub where res_id={entryId}", conn, sqlTransaction).ExecuteNonQuery();
                    if (entryId == 0 && netVal != 0.0)
                        entryId = (int)Math.Round(
                            double.Parse(new SqlCommand(
                                $"select max(id) from Entry{branchCond}",
                                conn, sqlTransaction).ExecuteScalar()?.ToString() ?? "0") + 1.0);
                }

                bool isUpdate = false;
                SqlCommand invCmd;

                if (ProcCode != -1)
                {
                    new SqlCommand($"delete from Inv_Sub where InvGlobalID='{InvGlobalID}'",
                        conn, sqlTransaction).ExecuteNonQuery();
                    isUpdate = true;
                    invCmd = new SqlCommand(StoredQueries.UpdateInv, conn, sqlTransaction);
                }
                else
                {
                    InvoiceOper.GetInvoiceGlobalID(ref InvGlobalID, ref ProcCode);
                    LoadInvNo();
                    int.TryParse(txtNo.Text, out Code);
                    invCmd = new SqlCommand(StoredQueries.InsertInv, conn, sqlTransaction);
                }

                int isBuy = InvType == 2 ? 0 : 1;
                if (ProcCode == -1) { LoadInvNo(); int.TryParse(txtNo.Text, out Code); }
                if (InvType == 9) entryId = 0;

                if (ProcCode != -1)
                    invCmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = ProcCode;

                int custId = cmbClient.SelectedIndex > -1
                    ? Convert.ToInt32(cmbClient.SelectedValue) : -1;
                int salesmanId = cmbSalesMen.SelectedValue != null
                    ? Convert.ToInt32(cmbSalesMen.SelectedValue) : -1;

                double cashVal = 0, visaVal = 0;
                int payTypeCode = -1, bankId = -1, bankAccCode = -1;

                if (cmbPayType.SelectedIndex == 1)
                {
                    payTypeCode = 1;
                    cashVal = Math.Round(netVal, 2);
                }
                else if (cmbPayType.SelectedIndex == 2)
                {
                    payTypeCode = 2;
                    bankId = Convert.ToInt32(cmbBanks.SelectedValue);
                    visaVal = Math.Round(netVal, 2);
                    var adpBk = new SqlDataAdapter(
                        $"select Acc_Code from Banks where id={bankId}", conn1);
                    var dtBk = new DataTable();
                    adpBk.Fill(dtBk);
                    if (dtBk.Rows.Count == 1)
                        bankAccCode = Convert.ToInt32(dtBk.Rows[0]["acc_code"]);
                }

                // إضافة بارامترات الفاتورة الرئيسية
                invCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = InvGlobalID;
                invCmd.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = ProcType;
                invCmd.Parameters.Add("@id", SqlDbType.Int).Value = Code;
                invCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = txtDate.SelectedDate ?? DateTime.Today;
                invCmd.Parameters.Add("@inv_type", SqlDbType.Int).Value = InvType;
                invCmd.Parameters.Add("@OrderType", SqlDbType.Int).Value = 0;
                invCmd.Parameters.Add("@safe", SqlDbType.Int).Value = cmbStore.SelectedValue;
                invCmd.Parameters.Add("@stock", SqlDbType.Int).Value = treasuryId;
                invCmd.Parameters.Add("@cust_id", SqlDbType.Int).Value = custId;
                invCmd.Parameters.Add("@sales_emp", SqlDbType.Int).Value = MainClass.EmpNo;
                invCmd.Parameters.Add("@InvTotal", SqlDbType.Float).Value = txtSumVal.Text;
                invCmd.Parameters.Add("@AdditionsTot", SqlDbType.Float).Value = 0;
                invCmd.Parameters.Add("@Insurance", SqlDbType.Float).Value = 0;
                invCmd.Parameters.Add("@tot_net", SqlDbType.Float).Value = netVal;
                invCmd.Parameters.Add("@InvProfit", SqlDbType.Float).Value = 0;
                invCmd.Parameters.Add("@paid", SqlDbType.Float).Value =
                    cmbPayType.SelectedIndex == 0 ? 0 : netVal;
                invCmd.Parameters.Add("@minus", SqlDbType.Float).Value =
                    double.TryParse(txtInvDiscVal.Text, out double invDisc2) ? invDisc2 : 0;
                invCmd.Parameters.Add("@tax", SqlDbType.Float).Value =
                    double.TryParse(txtInvTaxPer.Text, out double tax) ? tax : 0;
                invCmd.Parameters.Add("@EntryID", SqlDbType.Int).Value = entryId;
                invCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                invCmd.Parameters.Add("@IS_Buy", SqlDbType.Bit).Value = isBuy;
                invCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                invCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    string.IsNullOrWhiteSpace(txtNote.Text) ? " " : txtNote.Text;
                invCmd.Parameters.Add("@Reff_No ", SqlDbType.VarChar).Value = txtRefNo.Text;
                invCmd.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value =
                    txtRefDate.SelectedDate ?? DateTime.Today;
                invCmd.Parameters.Add("@salesman", SqlDbType.Int).Value = salesmanId;
                invCmd.Parameters.Add("@pay_type", SqlDbType.Int).Value = payTypeCode;
                invCmd.Parameters.Add("@cash", SqlDbType.Float).Value = cashVal;
                invCmd.Parameters.Add("@visa", SqlDbType.Float).Value = visaVal;
                invCmd.Parameters.Add("@bank", SqlDbType.Int).Value = bankId;
                invCmd.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = -1;
                invCmd.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = "";
                invCmd.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = false;
                invCmd.Parameters.Add("@TableNo", SqlDbType.NVarChar).Value = "";
                invCmd.ExecuteNonQuery();

                if (ProcCode != -1)
                    new SqlCommand($"delete from Inv_Sub where proc_id={ProcCode}",
                        conn, sqlTransaction).ExecuteNonQuery();

                // إضافة أصناف الفاتورة
                for (int i = 0; i < _gridSource.Count; i++)
                {
                    var row = _gridSource[i];
                    if (string.IsNullOrEmpty(row.ItemIdDgv)) continue;

                    var subCmd = new SqlCommand(StoredQueries.InsertInvSub, conn, sqlTransaction);
                    subCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = InvGlobalID;
                    subCmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = ProcCode;
                    subCmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = InvType;
                    subCmd.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = DBNull.Value;
                    subCmd.Parameters.Add("@Store", SqlDbType.Float).Value =
                        double.TryParse(row.StoreDgv, out double sv2) ? sv2 : 0;
                    subCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value =
                        int.TryParse(row.ItemIdDgv, out int iid) ? iid : 0;
                    subCmd.Parameters.Add("@unit", SqlDbType.Int).Value =
                        GetUnitID(row.UnitDgv);
                    subCmd.Parameters.Add("@UnitEquality", SqlDbType.Float).Value =
                        double.TryParse(row.PercentUnitDgv, out double ue) ? ue : 1;
                    subCmd.Parameters.Add("@val", SqlDbType.Float).Value =
                        double.TryParse(row.totQuantDgv, out double tq) ? tq : 0;
                    subCmd.Parameters.Add("@val1", SqlDbType.Float).Value =
                        double.TryParse(row.QuantDgv, out double q1) ? q1 : 0;
                    subCmd.Parameters.Add("@exchange_price", SqlDbType.Float).Value =
                        double.TryParse(row.priceDgv, out double pr) ? pr : 0;
                    subCmd.Parameters.Add("@discount", SqlDbType.Float).Value =
                        double.TryParse(row.discountValDgv, out double dv) ? dv : 0;
                    subCmd.Parameters.Add("@taxperc", SqlDbType.Float).Value =
                        double.TryParse(row.VatPerDgv, out double vp) ? vp : 0;
                    subCmd.Parameters.Add("@taxval", SqlDbType.Float).Value =
                        double.TryParse(row.VATValDgv, out double vv) ? vv : 0;
                    subCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = row.DescriptionDgv ?? "";
                    subCmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = 0;
                    subCmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value =
                        double.TryParse(row.AvgCostDgv, out double ac) ? ac : 0;
                    subCmd.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = 0;
                    subCmd.Parameters.Add("@BranchID", SqlDbType.Int).Value = MainClass.BranchNo;
                    subCmd.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = 0;
                    subCmd.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value =
                        double.TryParse(row.priceDgv, out double pnv) ? pnv : 0;
                    subCmd.ExecuteNonQuery();
                }

                // قيود محاسبية لفاتورة الشراء
                if (InvType == 1 && netVal != 0.0)
                {
                    int ccCode = cmbCostCenter.SelectedIndex > -1
                        ? Convert.ToInt32(cmbCostCenter.SelectedValue) : -1;

                    InsertPurchaseEntries(
                        sqlTransaction, entryId, custAccCode,
                        treasuryAccCode, bankAccCode, bankId, ccCode,
                        cashVal, visaVal, netVal);
                }

                sqlTransaction.Commit();
                RecalculateCost();

                var savedMsg = new frmSavedMsg();
                if (isUpdate) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    if (!isPrintd)
                    {
                        dgvSrch.Items.Refresh();
                        txtSumVal.Text = "0";
                        txtNet.Text = "0";
                        LoadInvNo();
                        ISreturn = false;
                        CLR();
                    }
                }
                else if (savedMsg.Pressed == 2)
                {
                    txtNo.Background = System.Windows.Media.Brushes.WhiteSmoke;
                    txtNo.Foreground = System.Windows.Media.Brushes.Black;
                }
                else if (savedMsg.Pressed == 3 && !isPrintd)
                {
                    ISreturn = false;
                    CLR();
                    Close();
                }
            }
            catch (Exception ex)
            {
                sqlTransaction.Rollback();
                isPrintd = false;
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void InsertPurchaseEntries(
            SqlTransaction trans, int entryId, int custAccCode,
            int treasuryAccCode, int bankAccCode, int bankId, int ccCode,
            double cashVal, double visaVal, double netVal)
        {
            double.TryParse(txtNetWithoutVAT.Text, out double netWithoutVAT);
            double.TryParse(txtTotDiscount.Text, out double totDisc);
            double.TryParse(txtTotVAT.Text, out double totVAT);

            string payTypeText = (cmbPayType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string custName = cmbClient.Text;

            if (ProcType == 1)
            {
                // إدخال قيد الشراء
                var entryCmd = new SqlCommand(
                    "insert into Entry(id,date,doc_no,type,state,notes,branch,IS_Deleted)" +
                    "values(@id,@date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    conn, trans);
                entryCmd.Parameters.Add("@id", SqlDbType.Int).Value = entryId;
                entryCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = txtDate.SelectedDate ?? DateTime.Today;
                entryCmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = Code;
                entryCmd.Parameters.Add("@type", SqlDbType.Int).Value = RestrType;
                entryCmd.Parameters.Add("@state", SqlDbType.Int).Value = 1;
                entryCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    $"فاتورة شراء {payTypeText} رقم:{Code} خاصة المورد:{custName}";
                entryCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                entryCmd.ExecuteNonQuery();

                // مخزون
                InsertEntrySub(trans, entryId, netWithoutVAT + totDisc, 0, 3200001,
                    $"فاتورة شراء {payTypeText} رقم:{Code} خاصة المورد:{custName}", ccCode);

                // خصومات
                if (totDisc > 0)
                    InsertEntrySub(trans, entryId, 0, totDisc, 3200003, "خصومات مكتسبة", -1);

                // ضريبة
                InsertEntrySub(trans, entryId, totVAT, 0, 2222001,
                    $"ض. القيمة المضافة فاتورة شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);

                // طريقة الدفع
                if (cmbPayType.SelectedIndex == 0)
                    InsertEntrySub(trans, entryId, 0, netVal, custAccCode,
                        $"فاتورة شراء {payTypeText} رقم:{Code} خاصة المورد:{custName}", -1);
                else if (cmbPayType.SelectedIndex == 1)
                    InsertEntrySub(trans, entryId, 0, netVal, treasuryAccCode,
                        $"فاتورة شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);
                else if (cmbPayType.SelectedIndex == 2)
                    InsertEntrySub(trans, entryId, 0, visaVal, bankAccCode,
                        $"فاتورة شراء {cmbBanks.Text} رقم:{Code} خاصة مورد:{custName}", -1);
            }
            else if (ProcType == 2)
            {
                // قيد مرتجع الشراء
                var entryCmd = new SqlCommand(
                    "insert into Entry(id,date,doc_no,type,state,notes,branch,IS_Deleted)" +
                    "values(@id,@date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    conn, trans);
                entryCmd.Parameters.Add("@id", SqlDbType.Int).Value = entryId;
                entryCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = txtDate.SelectedDate ?? DateTime.Today;
                entryCmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = Code;
                entryCmd.Parameters.Add("@type", SqlDbType.Int).Value = RestrType;
                entryCmd.Parameters.Add("@state", SqlDbType.Int).Value = 1;
                entryCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    $"فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}";
                entryCmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                entryCmd.ExecuteNonQuery();

                InsertEntrySub(trans, entryId, 0, netWithoutVAT + totDisc, 3200002,
                    $"فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", ccCode);

                if (totDisc > 0)
                    InsertEntrySub(trans, entryId, totDisc, 0, 3200003, "خصومات مكتسبة", -1);

                InsertEntrySub(trans, entryId, 0, totVAT, 2222001,
                    $"ض. القيمة المضافة فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);

                if (cmbPayType.SelectedIndex == 0)
                    InsertEntrySub(trans, entryId, netVal, 0, custAccCode,
                        $"فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);
                else if (cmbPayType.SelectedIndex == 1)
                    InsertEntrySub(trans, entryId, netVal, 0, treasuryAccCode,
                        $"فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);
                else if (cmbPayType.SelectedIndex == 2)
                    InsertEntrySub(trans, entryId, netVal, 0, bankAccCode,
                        $"فاتورة مرتد شراء {payTypeText} رقم:{Code} خاصة مورد:{custName}", -1);
            }
        }

        private void InsertEntrySub(SqlTransaction trans, int resId,
            double dept, double credit, int accNo, string notes, int ccCode)
        {
            var cmd = new SqlCommand(
                "insert into Entry_Sub(res_id,dept,credit,acc_no,notes,branch,CCcode)" +
                "values(@res_id,@dept,@credit,@acc_no,@notes,@branch,@CCcode)",
                conn, trans);
            cmd.Parameters.Add("@res_id", SqlDbType.Int).Value = resId;
            cmd.Parameters.Add("@dept", SqlDbType.Float).Value = dept;
            cmd.Parameters.Add("@credit", SqlDbType.Float).Value = credit;
            cmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = accNo;
            cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = notes;
            cmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
            cmd.Parameters.Add("@CCcode", SqlDbType.Int).Value = ccCode;
            cmd.ExecuteNonQuery();
        }

        private void StoreEditHistory()
        {
            try
            {
                int type = ProcType == 2 ? 21 : 1;
                if (conn1.State != ConnectionState.Open) conn1.Open();

                new SqlCommand(
                    $"INSERT His_Restrictions(id,date,doc_no,type,state,notes,branch,IS_Deleted,EmpID) " +
                    $"Select id,date,doc_no,type,state,notes,branch,IS_Deleted,EmpID " +
                    $"From Entry Where type={type} and doc_no={txtNo.Text}",
                    conn1).ExecuteNonQuery();

                int procId = Convert.ToInt32(new SqlCommand(
                    $"select max(ProcID) From His_Restrictions " +
                    $"Where type={type} and doc_no={txtNo.Text}",
                    conn1).ExecuteScalar());

                var adp = new SqlDataAdapter(
                    $"Select res_id,dept,credit,acc_no,Entry_Sub.notes,Entry_Sub.branch,CCcode " +
                    $"From Entry_Sub,Entry Where Entry.id=Entry_Sub.res_id " +
                    $"and Entry.type={type} and Entry.doc_no={txtNo.Text}", conn1);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    RestPurchCode = Convert.ToInt32(dt.Rows[0]["res_id"]);
                    foreach (DataRow row in dt.Rows)
                    {
                        var cmd = new SqlCommand(
                            "INSERT into His_Restrictions_Sub(procId,res_id,dept,credit,acc_no,notes,branch,CCcode) " +
                            "values(@procId,@res_id,@dept,@credit,@acc_no,@notes,@branch,@CCcode)", conn1);
                        cmd.Parameters.Add("@procId", SqlDbType.Int).Value = procId;
                        cmd.Parameters.Add("@res_id", SqlDbType.Int).Value = row["res_id"];
                        cmd.Parameters.Add("@dept", SqlDbType.Float).Value = row["dept"];
                        cmd.Parameters.Add("@credit", SqlDbType.Float).Value = row["credit"];
                        cmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = row["acc_no"];
                        cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = row["notes"];
                        cmd.Parameters.Add("@branch", SqlDbType.Int).Value = row["branch"];
                        cmd.Parameters.Add("@CCcode", SqlDbType.Int).Value = row["CCcode"];
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private void RecalculateCost()
        {
            try
            {
                foreach (var row in _gridSource)
                {
                    int.TryParse(row.ItemIdDgv, out int itemId);
                    double.TryParse(row.totQuantDgv, out double newQnty);
                    double avgCost = double.TryParse(row.priceDgv, out double p) ? p : 0;
                    ItemOper.RecalculateCost(itemId, newQnty, ref avgCost, "");
                }
            }
            catch { }
        }

        #endregion

        #region CLR

        private void CLR()
        {
            MainClass.ClearFormFields(this);
            Code = -1;
            ProcCode = -1;
            SelectedId = -1;
            isPrintd = false;
            InvTax = 0.0;
            InvDiscount = 0.0;
            TotAfterdisc = 0.0;

            txtBarcode.Text = "";
            txtSumVal.Text = "0";
            txtNet.Text = "0";
            txtNetWithoutVAT.Text = "0";
            txtTotDiscount.Text = "0";
            txtTotVAT.Text = "0";
            txtInvDiscVal.Text = "0";
            txtInvTax.Text = "0";
            txtInvTaxPer.Text = defVAT.ToString();
            txtNote.Text = "";
            txtSrchDgv.Text = "";
            txtRefNo.Text = "";

            txtDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtRefDate.SelectedDate = DateTime.Today;
            txtFromDate.SelectedDate = DateTime.Today;

            if (cmbStore.Items.Count > 0) cmbStore.SelectedIndex = 0;
            if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;
            if (cmbTreasury.Items.Count > 0) cmbTreasury.SelectedIndex = 0;
            cmbPayType.SelectedIndex = 1;
            cmbCostCenter.SelectedIndex = -1;
            cmbProcTypeSrch.SelectedIndex = 0;

            txtNo.Background = System.Windows.Media.Brushes.Firebrick;
            txtNo.Foreground = System.Windows.Media.Brushes.White;

            _gridSource.Clear();
            LoadInvNo();
            txtBarcode.Focus();
        }

        #endregion

        #region Print

        private void RptPrint(int type)
        {
            if (_gridSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات شراء أو بيع بالجدول", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "RptInvPurchase.repx";

            string path = System.IO.Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(type, print.BindToData(BindToInvoice()),
                print.RptUrl, print.RptName, print.defPrinter,
                print.kitchenprinter, print.PrintNo);
        }

        private Invoice BindToInvoice()
        {
            double sumPrice = 0;
            var list = new List<Item>();

            foreach (var row in _gridSource)
            {
                if (string.IsNullOrEmpty(row.ItemIdDgv)) continue;
                double.TryParse(row.QuantDgv, out double qty);
                double.TryParse(row.priceDgv, out double pr);
                sumPrice += Math.Round(qty * pr, 2);

                var item = new Item
                {
                    InvGlobalID = InvGlobalID,
                    ClientCode = Sync.ClientCode,
                    Name = row.ItemNameDgv,
                    ItemNo = int.TryParse(row.ItemIdDgv, out int iid) ? iid : 0,
                    ProcType = ProcType,
                    Description = row.DescriptionDgv,
                    Quantity = qty,
                    UnitEquality = double.TryParse(row.PercentUnitDgv, out double ue) ? ue : 1,
                    PrimaryQnty = double.TryParse(row.totQuantDgv, out double tq) ? tq : 0,
                    Unit = Common.GetUnitID(row.UnitDgv),
                    Price = pr,
                    AvegCost = double.TryParse(row.AvgCostDgv, out double ac) ? ac : 0,
                    Vat = double.TryParse(row.VATValDgv, out double vv) ? vv : 0,
                    VatPerc = double.TryParse(row.VatPerDgv, out double vp) ? vp : 0,
                    Barcode = row.barcodeDgv1,
                    ItemDiscount = double.TryParse(row.discountValDgv, out double dv) ? dv : 0,
                    ValiableStock = double.TryParse(row.StockDgv, out double st) ? st : 0,
                    Store = double.TryParse(row.StoreDgv, out double sv) ? sv : 0,
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = "",
                    ProductId = 0
                };
                list.Add(item);
            }

            double.TryParse(txtNet.Text, out double net);
            double.TryParse(txtTotVAT.Text, out double vat);
            double.TryParse(txtTotDiscount.Text, out double disc);

            var invoice = new Invoice
            {
                Total = sumPrice,
                SumPrice = sumPrice,
                VAT = Math.Round(vat, 2),
                Net = Math.Round(net, 2),
                Discount = Math.Round(disc, 2),
                Paycash = Math.Round(net, 2),
                InvGlobalID = InvGlobalID,
                EntryGlobalID = "1-2345",
                InvoiceNo = int.TryParse(txtNo.Text, out int invNo) ? invNo : 0,
                InvoiceType = (InvoiceType)InvType,
                ProcType = ProcType,
                InvDate = txtDate.SelectedDate ?? DateTime.Today,
                User = MainClass.EmpNo,
                Branch = MainClass.BranchNo,
                Treasury = MainClass.UserTreasury,
                Saleman = cmbSalesMen.SelectedIndex > -1
                    ? Convert.ToInt32(cmbSalesMen.SelectedValue) : -1,
                IsDeleted = false,
                Customer = cmbClient.SelectedIndex > -1
                    ? Convert.ToInt32(cmbClient.SelectedValue) : 1,
                VATperc = defVAT,
                PayType = cmbPayType.SelectedIndex == 0 ? -1 : 0,
                ReffNo = "-1",
                RefDate = DateTime.Now,
                Items = list
            };

            return invoice;
        }

        #endregion

        #region Search DGV

        private void Search()
        {
            string branchFilter = MainClass.BranchNo != -1
                ? $"inv.branch={MainClass.BranchNo} and " : "";

            string cond;
            if (!string.IsNullOrEmpty(txtSrchNo.Text))
                cond = branchFilter + $" inv.id={txtSrchNo.Text} and ";
            else if (!string.IsNullOrEmpty(txtSrchreffNo.Text))
                cond = $" inv.Reff_No={txtSrchreffNo.Text} and ";
            else
                cond = branchFilter + " date>=@date1 and date<=@date2 and ";

            LoadDG(cond);
        }

        private void LoadDG(string cond)
        {
            var searchRows = new List<PurchSearchRow>();

            var adp = new SqlDataAdapter(
                $"select Inv.proc_id,Inv.id as id,Inv.date as date,Reff_No,cust_id " +
                $"from Inv where inv_type={InvType} and proc_type={ProcType} " +
                $"and {cond} Inv.IS_Deleted=0 order by Inv.id", conn);

            if (!string.IsNullOrEmpty(cond) && cond.Contains("@date"))
            {
                var d1 = txtFromDate.SelectedDate ?? DateTime.Today;
                var d2 = (txtToDate.SelectedDate ?? DateTime.Today).AddHours(24.0);
                adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = d1.ToShortDateString();
                adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = d2;
            }

            var dt = new DataTable();
            adp.Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                searchRows.Add(new PurchSearchRow
                {
                    ProcId = row["proc_id"].ToString(),
                    InvNo = row["id"].ToString(),
                    ReffNo = row["Reff_No"].ToString(),
                    InvDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                    ClientName = GetCustName(Convert.ToInt32(row["cust_id"]))
                });
            }

            dgvSrch.ItemsSource = searchRows;
            dgvSrch.Items.Refresh();
        }

        private string GetCustName(int custId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select name from Customers where id={custId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private void ShowCustBalance()
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select Code from Accounts_Index where AName=N'{cmbClient.Text}' " +
                    Accounting.BranchCondition, conn1);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accFilter = $" and Entry_Sub.acc_no={dt.Rows[0][0]}";
                string branchFilter = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} and Entry_Sub.branch={MainClass.BranchNo} and " : "";

                var adp2 = new SqlDataAdapter(
                    $"select sum(Entry_Sub.dept) as dept,sum(Entry_Sub.credit) as credit " +
                    $"from Entry,Entry_Sub where {branchFilter} " +
                    $"Entry.IS_Deleted=0 and Entry.state=1 and Entry.id=Entry_Sub.res_id " +
                    $"{accFilter} group by Entry_Sub.acc_no", conn1);
                var dt2 = new DataTable();
                adp2.Fill(dt2);

                double dept = 0, credit = 0;
                foreach (DataRow row in dt2.Rows)
                {
                    dept += double.Parse(row["dept"].ToString());
                    credit += double.Parse(row["credit"].ToString());
                }

                if (dept > credit)
                    txtBalance.Text = $"{Math.Round(dept - credit, 3):0.##}";
                else if (credit > dept)
                    txtBalance.Text = $"{Math.Round(credit - dept, 3):0.##} دائن";
                else
                    txtBalance.Text = "0";
            }
            catch { }
        }

        #endregion

        #region Helpers

        private string GetItemName(int id)
        {
            try
            {
                var adp = new SqlDataAdapter($"select name from Items where id={id}", conn1);
                var dt = new DataTable();
                adp.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetItemCode(int id)
        {
            try
            {
                var adp = new SqlDataAdapter($"select Code from Items where id={id}", conn1);
                var dt = new DataTable();
                adp.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetUnitName(int id)
        {
            try
            {
                var adp = new SqlDataAdapter($"select name from units where id={id}", conn1);
                var dt = new DataTable();
                adp.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        public int GetUnitID(string name)
        {
            try
            {
                var adp = new SqlDataAdapter($"select id from units where name='{name}'", conn1);
                var dt = new DataTable();
                adp.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch { return -1; }
        }

        private void LoadItemDetails()
        {
            try
            {
                int idx = GetFocusedRowIndex();
                if (idx < 0) return;

                var row = GetRowAt(idx);
                if (row == null || string.IsNullOrEmpty(row.ItemIdDgv)) return;

                int.TryParse(row.ItemIdDgv, out SelectedId);

                txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(SelectedId).ToString();
                double.TryParse(row.AvgCostDgv, out double avgCost);
                double.TryParse(row.PercentUnitDgv, out double perc);
                txtCostAvrg.Text = (avgCost * perc).ToString();
                txtStock.Text = row.StockDgv;
                txtPerUnit.Text = row.PercentUnitDgv;
                txtTotUnitQuan.Text = row.totQuantDgv;
                txtItemBarcode.Text = row.barcodeDgv1;

                double.TryParse(row.StockDgv, out double stk);
                txtStock.Foreground = stk > -1
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Firebrick;

                LoadPrices(SelectedId);
            }
            catch { }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select * from ItemPrices where Itemid={itemId} order by Proc_id desc", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text = dt.Rows[0]["low_sale_price"].ToString();
                    txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"].ToString();
                }
            }
            catch { }
        }

        #endregion

        #region Button Events

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(txtDate.SelectedDate ?? DateTime.Today))
            {
                DXMessageBox.Show("لا يمكن أن يكون التاريخ خارج الفترة المحاسبية", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (ProcCode > -1 && ProcType == 2 &&
                InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
            {
                DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                return;
            }
            isPrintd = false;
            double.TryParse(txtNet.Text, out double net);
            if (net <= 0 && InvType != 9)
            {
                DXMessageBox.Show("يجب أن يكون الصافي أكبر من الصفر", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Save();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (ProcCode > -1 && ProcType == 2 &&
                InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
            {
                DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                return;
            }
            isPrintd = true;
            Save();
            if (isPrintd) CLR();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (_gridSource.Count > 0 && ProcCode == -1)
            {
                if (DXMessageBox.Show("لم يتم حفظ الفاتورة, هل تريد جديد", "تنبيه",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    CLR();
            }
            else
                CLR();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1) return;
                if (conn.State != ConnectionState.Open) conn.Open();

                if (DXMessageBox.Show("هل أنت متأكد من الحذف", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new SqlCommand(
                        $"update Inv set IS_Deleted=1 where proc_id={ProcCode}", conn)
                        .ExecuteNonQuery();

                    if (RestPurchCode > 0)
                        new SqlCommand(
                            $"update Entry set state=2,IS_Deleted=1 where id={RestPurchCode}", conn)
                            .ExecuteNonQuery();

                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    dgvSrch.Items.Refresh();
                    CLR();
                }
                else
                {
                    DXMessageBox.Show("اختر فاتورة ليتم حذفها");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (isPrintd)
                RptPrint(1);
            else
                DXMessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ..");
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (isPrintd)
                RptPrint(2);
            else
                DXMessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ..");
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                        $"and proc_type={ProcType} and IS_Deleted=0 order by id asc");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                        $"and proc_type={ProcType} and IS_Deleted=0 " +
                        $"and id<{txtNo.Text} order by id desc");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                        $"and proc_type={ProcType} and IS_Deleted=0 " +
                        $"and id>{txtNo.Text} order by id asc");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate($"select top 1 * from Inv where inv_type={InvType} " +
                        $"and proc_type={ProcType} and IS_Deleted=0 order by id desc");

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmCustomers();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Title = InvType == 1 ? "تعريف مورد" : "تعريف عميل";
                frm.Type = InvType == 1 ? 2 : 1;
                frm.ShowDialog();
                if (frm.isDone) LoadCustomers();
            }
            catch { }
        }

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvoiceSrch();
            frm.cmbProcType.IsEnabled = false;
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ProcType = ProcType;
            frm.InvType = InvType;
            frm.ShowDialog();
            if (frm.ISDone && double.Parse(frm.InvGlobalID) > 0)
                Navigate($"select * from Inv where proc_id={frm.InvGlobalID}");
        }

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            frm.Activate();
            frm.ShowDialog();
        }

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e) { }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int sel = cmbSalesMen.SelectedValue != null
                ? Convert.ToInt32(cmbSalesMen.SelectedValue) : -1;
            var frm = new frmSalesMen();
            frm.Activate();
            frm.ShowDialog();
            LoadSalesMen();
            try { cmbSalesMen.SelectedValue = sel; } catch { }
        }

        private void addCostCenter_Click(object sender, RoutedEventArgs e)
        {
            int sel = cmbCostCenter.SelectedValue != null
                ? Convert.ToInt32(cmbCostCenter.SelectedValue) : -1;
            var frm = new frmCostCenter();
            frm.Activate();
            frm.ShowDialog();
            LoadCostCenters();
            try { cmbCostCenter.SelectedValue = sel; } catch { }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                btn.ContextMenu.IsOpen = true;
        }

        private void stripImport_Click(object sender, RoutedEventArgs e) { }
        private void stripExport_Click(object sender, RoutedEventArgs e) { }
        private void btnCustomizeDisplay_Click(object sender, RoutedEventArgs e) { }
        private void btnAddRow_Click(object sender, RoutedEventArgs e)
        {
            _gridSource.Add(new PurchInvRow { No = (_gridSource.Count + 1).ToString() });
            RowIndex = _gridSource.Count - 1;
            GridControl1.SelectedIndex = RowIndex;
        }

        #endregion

        #region TextBox Events

        private void txtInvDiscVal_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                {
                    CalcTot();
                    double.TryParse(txtInvDiscVal.Text, out InvDiscount);
                    InvDiscount = Math.Round(InvDiscount, 2);
                }
                else
                {
                    txtInvDisc.Text = "0";
                    txtInvDiscVal.Text = "0";
                    InvDiscount = 0.0;
                    CalcTot();
                }
            }
            catch { }
        }

        private void txtInvDisc_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text) &&
                    !string.IsNullOrWhiteSpace(txtInvDisc.Text))
                {
                    double.TryParse(txtSumVal.Text, out double sumVal);
                    if (sumVal != 0)
                    {
                        double.TryParse(txtInvDisc.Text, out double discPer);
                        InvDiscount = Math.Round(discPer / 100.0 * sumVal, 2);
                        txtInvDiscVal.Text = InvDiscount.ToString();
                        CalcTot();
                    }
                }
                else
                {
                    txtInvDisc.Text = "0";
                    txtInvDiscVal.Text = "0";
                    InvDiscount = 0.0;
                    CalcTot();
                }
            }
            catch { }
        }

        private void txtInvDiscVal_Leave(object sender, RoutedEventArgs e)
        {
            try
            {
                double.TryParse(txtSumVal.Text, out double sumVal);
                double.TryParse(txtInvDiscVal.Text, out double discVal);
                if (sumVal != 0)
                    txtInvDisc.Text = (discVal / sumVal * 100.0).ToString("0.##");
            }
            catch { }
        }

        private void txtSrchDgv_TextChanged(object sender, TextChangedEventArgs e) { }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text[0]) && e.Text[0] != '.';
        }

        private void txtBarcode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return) ReadBarcode();
        }

        #endregion

        #region ComboBox Events

        private void cmbPayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPayType.SelectedIndex == 2)
            {
                cmbBanks.SelectedIndex = -1;
                cmbTreasury.SelectedIndex = -1;
                cmbTreasury.IsEnabled = false;
                cmbBanks.IsEnabled = true;
                cmbBanks.Focus();
            }
            else
            {
                cmbBanks.IsEnabled = false;
            }

            if (cmbPayType.SelectedIndex == 1)
            {
                try
                {
                    cmbTreasury.IsEnabled = true;
                    cmbTreasury.SelectedIndex = 0;
                    if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;
                }
                catch { }
            }

            if (cmbPayType.SelectedIndex == 0)
            {
                cmbTreasury.SelectedIndex = -1;
                cmbTreasury.IsEnabled = false;
                cmbClient.SelectedIndex = -1;
                cmbClient.Focus();
            }
        }

        private void cmbClient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { ShowCustBalance(); } catch { }
        }

        private void cmbClient_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SrchByNameClint();
            }
        }

        private void SrchByNameClint()
        {
            try
            {
                SrchName = cmbClient.Text;
                var adp = new SqlDataAdapter(
                    $"select id,name from Customers where IS_Deleted=0 " +
                    $"and name=N'{SrchName}' and type=2", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbClient.SelectedValue = dt.Rows[0]["id"];
                else
                    addNewClint();
            }
            catch { }
        }

        private void addNewClint()
        {
            try
            {
                var frm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Type = 2;
                frm.txtClientName.Text = cmbClient.Text;
                if (cmbPayType.SelectedIndex == 0) frm.PostponeClient = true;
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Clientname))
                    cmbClient.SelectedValue = frm.ClientId;
            }
            catch { }
        }

        #endregion

        #region CheckBox Events

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
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
            try
            {
                for (int i = 0; i < _gridSource.Count; i++)
                    CalcRowValues(i);
                CalcTot();
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RowIndex = GetFocusedRowIndex();
            if (RowIndex >= 0)
                LoadItemDetails();
        }

        private void GridControl1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            int index = _gridSource.IndexOf(e.Row.Item as PurchInvRow);
            if (index < 0) return;

            string col = (e.Column.Header?.ToString() ?? "");

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var row = GetRowAt(index);
                if (row == null || string.IsNullOrEmpty(row.ItemIdDgv)) return;

                if (col == "الكمية" || col == "السعر" || col == "الخصم")
                {
                    CalcRowValues(index);
                    CalcTot();
                }

                // البحث بالاسم
                if (col == "الصنف" && !string.IsNullOrEmpty(row.ItemNameDgv))
                    SearchByName();

                // البحث بالرمز
                if (col == "رمز الصنف" && !string.IsNullOrEmpty(row.ItemCodeDgv))
                    SearchByCode();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PurchInvRow row)
            {
                if (DXMessageBox.Show("هل تريد حذف السجل", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _gridSource.Remove(row);
                    CalcTot();
                }
            }
        }

        #endregion

        #region dgvSrch Events

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int idx = dgvSrch.SelectedIndex;
            if (idx < 0) return;

            if (dgvSrch.ItemsSource is List<PurchSearchRow> rows && idx < rows.Count)
            {
                int.TryParse(rows[idx].ProcId, out ProcCode);

                if (ISreturn)
                {
                    var adp = new SqlDataAdapter(
                        $"select id from Inv where proc_type=2 and inv_type={InvType} " +
                        $"and Reff_No={ProcCode} and IS_Deleted=0", conn);
                    var dt = new DataTable();
                    adp.Fill(dt);
                    if (dt.Rows.Count >= 1)
                    {
                        DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                        return;
                    }
                }

                Navigate($"select * from Inv where proc_id={ProcCode}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Context Menu Events

        private void StripItemSearch_Click(object sender, RoutedEventArgs e) => addNewItem();
        private void StripItemDetail_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            int.TryParse(GetRowCellValue(GetFocusedRowIndex(), "ItemIdDgv")?.ToString(), out int id);
            frm.ItemId = id;
            frm.ShowDialog();
        }
        private void StripAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }
        private void StripItemUnits_Click(object sender, RoutedEventArgs e) => ShowItemUnit(GetFocusedRowIndex());
        private void StripAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemCategory();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }
        private void StripdeleteRow_Click(object sender, RoutedEventArgs e)
        {
            int idx = GetFocusedRowIndex();
            if (idx >= 0 &&
                DXMessageBox.Show("هل تريد حذف السجل", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _gridSource.RemoveAt(idx);
                CalcTot();
            }
        }
        private void StripItemProcess_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(GetRowCellValue(GetFocusedRowIndex(), "ItemIdDgv")?.ToString(), out int id);
            var frm = new frmRptItemsActivity { ItemId = id };
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }
        private void StripItemLastActivity_Click(object sender, RoutedEventArgs e)
        {
            int idx = GetFocusedRowIndex();
            int.TryParse(GetRowCellValue(idx, "ItemIdDgv")?.ToString(), out int itemId);
            int.TryParse(GetRowCellValue(idx, "StoreDgv")?.ToString(), out int storeId);
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.LastProcess(itemId, storeId, -1, 1);
        }
        private void StripClientLastActivity_Click(object sender, RoutedEventArgs e)
        {
            int idx = GetFocusedRowIndex();
            int.TryParse(GetRowCellValue(idx, "ItemIdDgv")?.ToString(), out int itemId);
            int.TryParse(GetRowCellValue(idx, "StoreDgv")?.ToString(), out int storeId);
            int custId = cmbClient.SelectedIndex > -1
                ? Convert.ToInt32(cmbClient.SelectedValue) : -1;
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.LastProcess(itemId, storeId, custId, 1);
        }
        private void StripItemCost_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(GetRowCellValue(GetFocusedRowIndex(), "ItemIdDgv")?.ToString(), out int id);
            DXMessageBox.Show(ItemOper.AvgCost(id, MainClass.BranchNo).ToString(),
                "تكلفة المادة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    // ═══════════════════════════════════════════════════════════════
    //                        Model Classes
    // ═══════════════════════════════════════════════════════════════

    public class PurchInvRow : INotifyPropertyChanged
    {
        private string _no = ""; private string _itemCodeDgv = ""; private string _itemIdDgv = "";
        private string _itemNameDgv = ""; private string _descriptionDgv = "";
        private string _barcodeDgv1 = ""; private string _unitDgv = ""; private string _quantDgv = "0";
        private string _percentUnitDgv = "1"; private string _totQuantDgv = "0";
        private string _priceDgv = "0"; private string _sumPriceDgv = "0";
        private string _avgCostDgv = "0"; private string _discountValDgv = "0";
        private string _discountPerDgv = "0"; private string _totPriceDgv = "0";
        private string _vatPerDgv = "0"; private string _vatValDgv = "0";
        private string _netValDgv = "0"; private string _stockDgv = "0"; private string _storeDgv = "0";

        public string No { get => _no; set { _no = value; OnPC(nameof(No)); } }
        public string ItemCodeDgv { get => _itemCodeDgv; set { _itemCodeDgv = value; OnPC(nameof(ItemCodeDgv)); } }
        public string ItemIdDgv { get => _itemIdDgv; set { _itemIdDgv = value; OnPC(nameof(ItemIdDgv)); } }
        public string ItemNameDgv { get => _itemNameDgv; set { _itemNameDgv = value; OnPC(nameof(ItemNameDgv)); } }
        public string DescriptionDgv { get => _descriptionDgv; set { _descriptionDgv = value; OnPC(nameof(DescriptionDgv)); } }
        public string barcodeDgv1 { get => _barcodeDgv1; set { _barcodeDgv1 = value; OnPC(nameof(barcodeDgv1)); } }
        public string UnitDgv { get => _unitDgv; set { _unitDgv = value; OnPC(nameof(UnitDgv)); } }
        public string QuantDgv { get => _quantDgv; set { _quantDgv = value; OnPC(nameof(QuantDgv)); } }
        public string PercentUnitDgv { get => _percentUnitDgv; set { _percentUnitDgv = value; OnPC(nameof(PercentUnitDgv)); } }
        public string totQuantDgv { get => _totQuantDgv; set { _totQuantDgv = value; OnPC(nameof(totQuantDgv)); } }
        public string priceDgv { get => _priceDgv; set { _priceDgv = value; OnPC(nameof(priceDgv)); } }
        public string SumPriceDgv { get => _sumPriceDgv; set { _sumPriceDgv = value; OnPC(nameof(SumPriceDgv)); } }
        public string AvgCostDgv { get => _avgCostDgv; set { _avgCostDgv = value; OnPC(nameof(AvgCostDgv)); } }
        public string discountValDgv { get => _discountValDgv; set { _discountValDgv = value; OnPC(nameof(discountValDgv)); } }
        public string discountPerDgv { get => _discountPerDgv; set { _discountPerDgv = value; OnPC(nameof(discountPerDgv)); } }
        public string totPriceDgv { get => _totPriceDgv; set { _totPriceDgv = value; OnPC(nameof(totPriceDgv)); } }
        public string VatPerDgv { get => _vatPerDgv; set { _vatPerDgv = value; OnPC(nameof(VatPerDgv)); } }
        public string VATValDgv { get => _vatValDgv; set { _vatValDgv = value; OnPC(nameof(VATValDgv)); } }
        public string NetValDgv { get => _netValDgv; set { _netValDgv = value; OnPC(nameof(NetValDgv)); } }
        public string StockDgv { get => _stockDgv; set { _stockDgv = value; OnPC(nameof(StockDgv)); } }
        public string StoreDgv { get => _storeDgv; set { _storeDgv = value; OnPC(nameof(StoreDgv)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPC(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class PurchSearchRow
    {
        public string ProcId { get; set; }
        public string InvNo { get; set; }
        public string ReffNo { get; set; }
        public string InvDate { get; set; }
        public string ClientName { get; set; }
    }
}