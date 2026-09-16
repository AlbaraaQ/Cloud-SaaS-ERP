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
using System.Windows.Media;
using System.Windows.Threading;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using UtilitiesProj;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPOS : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private const string Format = "#.##";

        private SqlConnection conn;
        private SqlConnection conn1;

        public bool ISTailor;
        private string InvGlobalID;
        private string EntryGlobalID;
        private Invoice Invo;
        public int InvType;
        public int ProcType;
        public int EntryType;
        private InvoiceObj InvObj;
        private List<Item> Items;
        private bool IsBarcode;
        private bool ISNew;
        private Print print;
        private int ScaleDigits;
        private int ValueDigits;
        private int DecimalDigits;
        private int ItemCodeDigits;
        private int Code;
        private int ProcCode;
        private int RestSaleCode;
        private int OrderType;
        private bool ISTRialEnd;
        private int ClientId;
        private int SalemanId;
        public double PayNetwork;
        public double Paycash;
        public double Payvisa;
        private double AdditionCost;
        private bool ISreturn;
        private bool IsPrinted;
        private bool ISLinkedreturn;
        public double InvDiscount;
        public int Paytype;
        public bool SrchPrint;
        private double TotalVal;
        private double DeliVal;
        private double RahenVal;
        public bool IsEnterNo;
        public DateTime txtInvTime;
        private int live;
        public int[] HoldList;
        public string comment;
        private bool linkedInvRet;
        private int GrpPageNo;
        private int ItemPageNo;
        private int ItemsInPage;
        private int GrpsInPage;
        private bool ISShowGrps;
        private bool IsShowItemsPage;
        private int GrpsColmNo;
        private int GrpRowsNo;
        private int ItemsColNo;
        private int ItemsRowsNo;
        private bool holdbool;
        private bool savebool;
        public bool byVisa;
        private DataTable resBanks;
        private string txtOrderType;
        public bool _IsUpdateDG;
        private int rowSelected;
        private int RowIndex;
        private DataTable dtItems;
        private int ItemsIndex;
        private int GroupsIndex;
        private DateTime RefrDate;
        private string RefrNo;
        private DataTable dtGrp;
        private string StyleFile;
        private DataTable dtSeialNo;

        // مؤقتات الصف الحالي
        private string _ItemCodeDgv1;
        private string _ItemIdDgv1;
        private string _ItemNameDgv1;
        private string _DescriptionDgv1;
        private string _barcodeDgv11;
        private string _UnitDgv1;
        private string _QuantDgv1;
        private string _PercentUnitDgv1;
        private string _totQuantDgv1;
        private string _priceDgv1;
        private string _SumPriceDgv1;
        private string _AvgCostDgv1;
        private string _discountValDgv1;
        private string _discountPerDgv1;
        private string _totPriceDgv1;
        private string _VatPerDgv1;
        private string _VATValDgv1;
        private string _NetValDgv1;
        private string _StockDgv1;
        private string _StoreDgv1;
        private bool decimalQuan;
        private string decimalVal;

        // مصدر بيانات الجدول
        private ObservableCollection<PosInvoiceRow> _gridSource;

        // Timer
        private DispatcherTimer Timer1;

        #endregion

        #region Constructor

        public frmPOS()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            ISTailor = false;
            InvGlobalID = "";
            EntryGlobalID = "";
            Invo = new Invoice();
            InvType = 3;
            ProcType = 1;
            EntryType = 2;
            InvObj = new InvoiceObj(InvType, ProcType);
            Items = new List<Item>();
            IsBarcode = false;
            ISNew = true;
            print = new Print(3);
            ScaleDigits = 2;
            ValueDigits = 4;
            DecimalDigits = 2;
            ItemCodeDigits = 7;
            Code = -1;
            ProcCode = -1;
            RestSaleCode = -1;
            OrderType = 0;
            ISTRialEnd = false;
            ClientId = -1;
            SalemanId = -1;
            PayNetwork = 0.0;
            AdditionCost = 0.0;
            ISreturn = false;
            IsPrinted = false;
            ISLinkedreturn = false;
            Paytype = 0;
            SrchPrint = false;
            TotalVal = 0.0;
            DeliVal = 0.0;
            RahenVal = 0.0;
            IsEnterNo = false;
            txtInvTime = DateTime.Now;
            live = 0;
            HoldList = new int[9];
            comment = "";
            linkedInvRet = false;
            GrpPageNo = 0;
            ItemPageNo = 0;
            ItemsInPage = 24;
            GrpsInPage = 5;
            holdbool = false;
            savebool = false;
            byVisa = false;
            resBanks = new DataTable();
            txtOrderType = "";
            _IsUpdateDG = false;
            RowIndex = -1;
            dtItems = new DataTable();
            ItemsIndex = 0;
            GroupsIndex = 0;
            RefrDate = DateTime.Now;
            dtGrp = new DataTable();
            StyleFile = System.IO.Path.Combine(MainClass.ReportsPath, "Styles\\PosSaveLayoutToXML.xml");
            dtSeialNo = new DataTable();
            decimalQuan = false;
            decimalVal = ".0";

            ClearTempFields();

            _gridSource = new ObservableCollection<PosInvoiceRow>();
            GridControl1.ItemsSource = _gridSource;

            // Timer
            Timer1 = new DispatcherTimer();
            Timer1.Interval = TimeSpan.FromSeconds(1);
            Timer1.Tick += Timer1_Tick;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer1.Start();

            // اللغة
            if (string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase))
            {
                cmbProcTypeSrch.Items.Clear();
                cmbProcTypeSrch.Items.Add(new ComboBoxItem { Content = "Sales inv." });
                cmbProcTypeSrch.Items.Add(new ComboBoxItem { Content = "Retrieve inv." });
                cmbProcTypeSrch.Items.Add(new ComboBoxItem { Content = "Hold Inv" });
                cmbProcTypeSrch.Items.Add(new ComboBoxItem { Content = "Price offer inv." });
                btnPay.Content = "Pay Now F6";
            }

            txtrec1.Text = "0";
            txtNetWithoutVAT.Text = "0";
            ProcType = 1;
            InvType = 3;

            CheckHold();
            var Home = new Home();
            Title = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                ? $"برنامج Auditor - نقطة بيع {Home.lblBranch1.Text}"
                : "cashier";

            LoadInvNo();

            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtDate.SelectedDate = DateTime.Today;
            lblDate.Text = DateTime.Today.ToShortDateString();
            Label15.Text = DateTime.Now.ToString("hh:mm tt");

            LoadSafes();
            LoadSalesMen();

            if (cmbSafe.Items.Count > 0)
                cmbSafe.SelectedIndex = 0;

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized : WindowState.Normal;

            txtBarcode.Focus();

            loadMainSettings();
            LoadGroups();
            ShowGroups(0);

            LoadDGvSetting();
            btnNew_Click(null, null);

            GridControl1.ItemsSource = _gridSource;

            Inventory.CalcItemsStockPOS(
                int.TryParse(cmbSafe.SelectedValue?.ToString(), out int sv) ? sv : 0,
                MainClass.BranchNo);

            dtSeialNo.Columns.Add("ItemId");
            dtSeialNo.Columns.Add("SerialNo");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (!ISTRialEnd && ProcType == 1 &&
                    ((ProcCode == -1 && _gridSource.Count > 0) ||
                     (ProcCode != -1 && _IsUpdateDG)))
                {
                    if (DXMessageBox.Show("لم يتم حفظ الفاتورة, هل تريد الخروج  ", "",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                var frmItemsBalances = new frmItemsBalances();
                frmItemsBalances.Close();
            }
            catch { }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1: addNewItem(); break;
                case Key.F2: quantityEdit(); break;
                case Key.F3: priceEdit(); break;
                case Key.F4: AddUnit(); break;
                case Key.F5: deleteRow(); break;
                case Key.F6: PayBill(); break;
                case Key.F7: btnHoldOrd_Click(null, null); break;
                case Key.F8: DeleteInv(); break;
                case Key.F9: btnCloseDay_Click(null, null); break;
                case Key.F12: btnNew_Click(null, null); break;
                case Key.Return: ReadBarcode(); break;
                case Key.OemPipe: btnMunalBarcode_Click(null, null); break;
            }
        }

        #endregion

        #region Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            Title = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");
            Label15.Text = DateTime.Now.ToString("hh:mm tt");
        }

        #endregion

        #region Load Methods

        private void loadMainSettings()
        {
            try
            {
                var adp = new SqlDataAdapter(
                    "select * from SettingsPOSDisplay where Inv_id=3", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        ISShowGrps = Convert.ToBoolean(dt.Rows[0]["ShowGrps"]);
                        IsShowItemsPage = Convert.ToBoolean(dt.Rows[0]["ShowItemsPage"]);
                        GrpsColmNo = Convert.ToInt32(dt.Rows[0]["GrpsColmNo"]);
                        GrpRowsNo = Convert.ToInt32(dt.Rows[0]["GrpRowsNo"]);
                        ItemsColNo = Convert.ToInt32(dt.Rows[0]["ItemsColNo"]);
                        ItemsRowsNo = Convert.ToInt32(dt.Rows[0]["ItemsRowsNo"]);
                        GrpsInPage = GrpsColmNo * GrpRowsNo;
                        ItemsInPage = ItemsColNo * ItemsRowsNo;
                        InvObj.SaleManIsRequire = Convert.ToBoolean(dt.Rows[0]["AddSaleman"]);
                    }
                    catch { }
                }

                var adp2 = new SqlDataAdapter("select * from SettingScale", conn);
                var dt2 = new DataTable();
                adp2.Fill(dt2);
                if (dt2.Rows.Count == 1)
                {
                    try
                    {
                        ScaleDigits = Convert.ToInt32(dt2.Rows[0]["ScaleCodeDigits"]);
                        ValueDigits = Convert.ToInt32(dt2.Rows[0]["ValueDigits"]);
                        DecimalDigits = Convert.ToInt32(dt2.Rows[0]["DecimlDigits"]);
                        ItemCodeDigits = Convert.ToInt32(dt2.Rows[0]["ItemCodeDigits"]);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الإعدادات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDGvSetting()
        {
            try
            {
                // تفعيل تعديل السعر إذا كان مسموحاً
                if (User.EditPrice)
                    ColPrice.IsReadOnly = false;

                var adp = new SqlDataAdapter(
                    $"select * from CustomizedDGV where Form_id={InvType} order by Column_id",
                    conn);
                var dt = new DataTable();
                adp.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToDouble(row["Column_id"]) == 100.0)
                    {
                        Hold6Btn.Visibility = Visibility.Visible;
                        Hold7Btn.Visibility = Visibility.Visible;
                        Hold8Btn.Visibility = Visibility.Visible;
                    }
                }
            }
            catch { }
        }

        public void LoadSalesMen()
        {
            try
            {
                var adp = new SqlDataAdapter(
                    "select id,name from salesmen where IS_Deleted=0 order by id", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                cmbSalesMen.ItemsSource = dt.DefaultView;
                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ تحميل المندوبين: " + ex.Message);
            }
        }

        public void LoadSafes()
        {
            try
            {
                int empNo = MainClass.EmpNo == 0 ? 1 : MainClass.EmpNo;
                var adp = new SqlDataAdapter(
                    $"select id,name from Safes,Safe_Emps where Safes.id=Safe_Emps.safe_id " +
                    $"and emp_id={empNo} and IS_Deleted=0 and status<>2 order by id",
                    conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count == 0)
                    DXMessageBox.Show("البائع غير مرتبط بمستودع", "", MessageBoxButton.OK, MessageBoxImage.Information);

                cmbSafe.ItemsSource = dt.DefaultView;
                cmbSafe.DisplayMemberPath = "name";
                cmbSafe.SelectedValuePath = "id";
                cmbSafe.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ تحميل الصناديق: " + ex.Message);
            }
        }

        private void LoadInvNo()
        {
            txtNo.Text = InvoiceOper.InvoiceNo(InvType, ProcType, InvObj.Prefixe).ToString();
            LoadOrderNo();
        }

        private void LoadOrderNo()
        {
            try
            {
                if (Code == -1)
                {
                    var adp = new SqlDataAdapter(
                        $"select * from inv where date>=@date1 and date<=@date2 " +
                        $"and inv_type=3 and proc_type={ProcType}", conn1);
                    adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                        MainClass.lastCashercloseDate;
                    adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                        (txtDate.SelectedDate ?? DateTime.Today).AddDays(1.0);
                    var dt = new DataTable();
                    adp.Fill(dt);
                    txtOrderNo.Text = (dt.Rows.Count + 1).ToString();
                }
                else
                {
                    var adp = new SqlDataAdapter(
                        $"select * from inv where date>=@date1 and date<=@date2 " +
                        $"and inv_type=3 and proc_type={ProcType}", conn1);
                    var d = txtDate.SelectedDate ?? DateTime.Today;
                    adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                        d.ToShortDateString();
                    adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                        d.AddDays(1.0).ToShortDateString();
                    var dt = new DataTable();
                    adp.Fill(dt);
                    if (double.TryParse(txtNo.Text, out double no) &&
                        dt.Rows.Count > 0 &&
                        double.TryParse(dt.Rows[0]["id"].ToString(), out double firstId))
                        txtOrderNo.Text = (no - firstId + 1).ToString();
                }
            }
            catch { }
        }

        public void LoadGroups()
        {
            try
            {
                new SqlDataAdapter(
                    "Select id, Name, nameEN, Image from ItemsCategory " +
                    "where IS_Deleted=0 And ShowInPOS=1 order by DispalyOrder",
                    conn).Fill(dtGrp);

                if (dtGrp.Rows.Count == 0)
                    FlowGrpPnl.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التحميل\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Groups & Items Display

        public void ShowGroups(int pageNo)
        {
            if (pageNo < 0) { GrpPageNo = 0; return; }

            FlowGrpPnl.Children.Clear();
            if (dtGrp.Rows.Count == 0) return;

            int start = GrpsInPage * pageNo;
            int btnH = Math.Max(60, (int)(FlowGrpPnl.ActualHeight / GrpsInPage) - 10);

            for (int i = start; i < Math.Min(start + GrpsInPage, dtGrp.Rows.Count); i++)
            {
                int capturedIndex = i;
                var grpId = Convert.ToInt32(dtGrp.Rows[i]["id"]);

                var btn = new Button
                {
                    Content = dtGrp.Rows[i]["name"].ToString(),
                    Tag = grpId,
                    Height = btnH,
                    Width = double.NaN,
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Cursor = Cursors.Hand
                };
                btn.Click += (s, _) =>
                {
                    clearItemsPage();
                    click_group((int)((Button)s).Tag);
                };
                FlowGrpPnl.Children.Add(btn);
            }
        }

        private void ShowItem(ref int pageNo)
        {
            if (pageNo < 0) { ItemPageNo = 0; return; }

            txtPageNo.Content = (ItemPageNo + 1).ToString();
            FlowItemsPnl.Children.Clear();
            if (dtItems.Rows.Count == 0) return;

            int start = ItemsInPage * pageNo;
            int btnH = Math.Max(80, (int)(FlowItemsPnl.ActualHeight / ItemsRowsNo) - 10);
            int btnW = Math.Max(85, (int)(FlowItemsPnl.ActualWidth / ItemsColNo) - 10);

            for (int i = start; i < Math.Min(start + ItemsInPage, dtItems.Rows.Count); i++)
            {
                var itemId = Convert.ToInt32(dtItems.Rows[i]["id"]);
                var name = dtItems.Rows[i]["name"].ToString();
                var price = dtItems.Rows[i]["sale_price"].ToString();

                var btn = new Button
                {
                    Tag = itemId,
                    Height = btnH,
                    Width = btnW,
                    Margin = new Thickness(0, 5, 5, 0),
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    Cursor = Cursors.Hand,
                    Content = $"{name}\n{price}"
                };
                btn.Click += (s, _) => SearchByID((int)((Button)s).Tag, 0);
                FlowItemsPnl.Children.Add(btn);
            }
        }

        private void clearItemsPage()
        {
            ItemPageNo = 0;
            GrpPageNo = 0;
            FlowItemsPnl.Children.Clear();
        }

        private void click_group(int id)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select id, name, nameEN, image, sale_price from Items " +
                    $"where group_id={id} and IS_Deleted=0 and ShowInPOS=1 order by id",
                    conn);
                dtItems = new DataTable();
                adp.Fill(dtItems);
                int p = 0;
                ShowItem(ref p);
            }
            catch { }
        }

        #endregion

        #region Grid Helper Methods

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

        private PosInvoiceRow GetRowAt(int index)
        {
            if (index >= 0 && index < _gridSource.Count)
                return _gridSource[index];
            return null;
        }

        private object GetRowCellValue(int index, string column)
        {
            var row = GetRowAt(index);
            if (row == null) return null;
            return typeof(PosInvoiceRow).GetProperty(column)?.GetValue(row);
        }

        private void SetRowCellValue(int index, string column, object value)
        {
            var row = GetRowAt(index);
            if (row == null) return;
            typeof(PosInvoiceRow).GetProperty(column)?.SetValue(row, value?.ToString() ?? "");
        }

        private int GetFocusedRowIndex()
        {
            if (GridControl1.SelectedItem is PosInvoiceRow row)
                return _gridSource.IndexOf(row);
            return -1;
        }

        private object GetFocusedRowCellValue(string column)
            => GetRowCellValue(GetFocusedRowIndex(), column);

        private void SetFocusedRowCellValue(string column, object value)
            => SetRowCellValue(GetFocusedRowIndex(), column, value);

        private void AddNewGridRow(PosInvoiceRow row)
        {
            _gridSource.Add(row);
        }

        private void DeleteRowAt(int index)
        {
            if (index >= 0 && index < _gridSource.Count)
                _gridSource.RemoveAt(index);
        }

        private void ClearTempFields()
        {
            _ItemCodeDgv1 = "";
            _ItemIdDgv1 = "";
            _ItemNameDgv1 = "";
            _DescriptionDgv1 = "";
            _barcodeDgv11 = "";
            _UnitDgv1 = "";
            _QuantDgv1 = "";
            _PercentUnitDgv1 = "";
            _totQuantDgv1 = "";
            _priceDgv1 = "";
            _SumPriceDgv1 = "";
            _AvgCostDgv1 = "";
            _discountValDgv1 = "";
            _discountPerDgv1 = "";
            _totPriceDgv1 = "";
            _VatPerDgv1 = "";
            _VATValDgv1 = "";
            _NetValDgv1 = "";
            _StockDgv1 = "";
            _StoreDgv1 = "";
        }

        #endregion

        #region Calc Methods

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

                if (InvObj.PriceIncVAT)
                    totPrice -= vatVal;

                row.totQuantDgv = totQty.ToString("0.##");
                row.SumPriceDgv = sumPrice.ToString("0.##");
                row.VATValDgv = Math.Round(vatVal, 2).ToString("0.##");
                row.totPriceDgv = Math.Round(totPrice, 2).ToString("0.##");
                row.NetValDgv = Math.Round(totPrice + vatVal, 2).ToString("0.##");
            }
            catch { }
        }

        private double CalcVAT(int itemId, decimal price)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select tax,Purch_price,sale_price from Items where id={itemId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double taxVal = Convert.ToDouble(dt.Rows[0]["tax"]);
                    if (taxVal == 0) return 0.0;

                    if (!InvObj.PriceIncVAT)
                        return Math.Round((double)price * (InvObj.VAT / 100.0), 3);
                    else
                        return (double)price - Math.Round((double)price / (1.0 + InvObj.VAT / 100.0), 3);
                }
            }
            catch { }
            return 0.0;
        }

        public void CalcTot()
        {
            try
            {
                double sumPriceTotal = 0, totQuantTotal = 0, discTotal = 0;
                double netTotal = 0, vatTotal = 0, totPriceTotal = 0, avgCostTotal = 0;

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

                    sumPriceTotal += sp;
                    totQuantTotal += tq;
                    discTotal += dv;
                    netTotal += nv;
                    vatTotal += vv;
                    totPriceTotal += tp;
                    avgCostTotal += ac * tq;
                }

                double invDisc = 0;
                double.TryParse(txtInvDiscount.Text, out invDisc);

                double totalDisc = discTotal + invDisc;
                double netWithoutVAT = totPriceTotal;
                double finalNet = netTotal;

                if (InvObj.AdditionalTax > 0)
                {
                    double addTax = Math.Round(netWithoutVAT * (InvObj.AdditionalTax / 100.0), 3);
                    netWithoutVAT += addTax;
                    vatTotal = Math.Round(netWithoutVAT * (InvObj.VAT / 100.0), 3);
                    finalNet = netWithoutVAT + vatTotal;
                }
                else if (invDisc > 0)
                {
                    if (InvObj.PriceIncVAT)
                    {
                        double vatOnDisc = invDisc - Math.Round(invDisc / (1.0 + InvObj.VAT / 100.0), 3);
                        netWithoutVAT -= (invDisc - vatOnDisc);
                        vatTotal = Math.Round(netWithoutVAT * (InvObj.VAT / 100.0), 3);
                    }
                    else
                    {
                        netWithoutVAT -= invDisc;
                        vatTotal = Math.Round(netWithoutVAT * (InvObj.VAT / 100.0), 3);
                    }
                    finalNet = netWithoutVAT + vatTotal;
                }

                txtItemsNo1.Text = _gridSource.Count.ToString();
                txtTotQuant.Text = totQuantTotal.ToString("0.##");
                txtSumCostAvrg.Text = Math.Round(avgCostTotal, 2).ToString();
                txtSumVal.Text = $"{Math.Round(sumPriceTotal, 2):0.##}";
                txtDiscountVal.Text = $"{Math.Round(totalDisc, 2):0.##}";
                txtNetWithoutVAT.Text = $"{Math.Round(netWithoutVAT, 2):0.##}";
                txtInvProfit.Text = Math.Round(netWithoutVAT - avgCostTotal, 2).ToString();
                txtTotVAT.Text = $"{Math.Round(vatTotal, 2):0.##}";
                txtNet.Text = $"{Math.Round(finalNet, 2):0.##}";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private int CalcStock(int itemId)
        {
            try
            {
                if (cmbSafe.SelectedIndex > -1 &&
                    int.TryParse(cmbSafe.SelectedValue?.ToString(), out int safeId))
                    return (int)Math.Round(
                        Inventory.CalcItemStock(safeId, itemId, MainClass.BranchNo));
            }
            catch { }
            return 0;
        }

        #endregion

        #region Search & Add Item

        private void SearchByID(int itemId, int unitId)
        {
            try
            {
                if (!ISNew && ProcType != 3)
                {
                    DXMessageBox.Show("لا يمكن تعديل فاتورة سابقة");
                    return;
                }

                if (itemId <= 0) return;

                ClearTempFields();

                var adp = new SqlDataAdapter(
                    $"select name,id,Code,nameEn,unit as unitId from Items " +
                    $"where IS_Deleted=0 and id={itemId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count == 0) { addNewItem(); return; }

                bool found = false;
                int foundIndex = -1;

                if (unitId == 0)
                    int.TryParse(dt.Rows[0]["unitId"].ToString(), out unitId);

                for (int i = 0; i < _gridSource.Count; i++)
                {
                    if (_gridSource[i].ItemIdDgv == dt.Rows[0]["id"].ToString() &&
                        Common.GetUnitID(_gridSource[i].UnitDgv) == unitId)
                    {
                        found = true;
                        foundIndex = i;
                        break;
                    }
                }

                if (found && !IsBarcode)
                {
                    double.TryParse(_gridSource[foundIndex].QuantDgv, out double q);
                    _gridSource[foundIndex].QuantDgv = (q + 1).ToString();
                    CalcRowValues(foundIndex);
                    CalcTot();
                    CheckForItemOffer(itemId);
                }
                else
                {
                    var newRow = new PosInvoiceRow
                    {
                        No = (_gridSource.Count + 1).ToString(),
                        ItemCodeDgv = dt.Rows[0]["code"].ToString(),
                        ItemIdDgv = dt.Rows[0]["id"].ToString(),
                        ItemNameDgv = string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? dt.Rows[0]["name"].ToString()
                            : (dt.Rows[0]["nameEN"].ToString() != ""
                                ? dt.Rows[0]["nameEN"].ToString()
                                : dt.Rows[0]["name"].ToString()),
                        QuantDgv = "1",
                        discountValDgv = "0",
                        discountPerDgv = "0",
                        AvgCostDgv = "0",
                        StockDgv = "0",
                        StoreDgv = cmbSafe.SelectedValue?.ToString() ?? "0"
                    };

                    _gridSource.Add(newRow);
                    RowIndex = _gridSource.Count - 1;
                    LoadItemInf(itemId, RowIndex, unitId);
                    CalcRowValues(RowIndex);
                    CalcTot();
                    CheckForItemOffer(itemId);
                }

                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void LoadItemInf(int id, int index, int unitId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select Items.id,Items.name,Items.barcode,units.name as unit," +
                    $"purch_price,sale_price,tax,discount,ItemProperty " +
                    $"from Items,units where Items.unit=units.id and items.id={id}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count == 0) return;

                LoadUnitInf(index, unitId);

                var row = GetRowAt(index);
                if (row == null) return;

                row.AvgCostDgv = ItemOper.Cost(id).ToString();
                row.discountValDgv = dt.Rows[0]["discount"].ToString();
                row.discountPerDgv = dt.Rows[0]["discount"].ToString();
                row.VatPerDgv = dt.Rows[0]["tax"].ToString();
                row.StockDgv = CalcStock(id).ToString();
                row.StoreDgv = cmbSafe.SelectedValue?.ToString() ?? "0";
                row.DescriptionDgv = " ";
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
                    if (double.TryParse(row.QuantDgv, out double q) &&
                        double.TryParse(row.PercentUnitDgv, out double p))
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

        private void addNewItem()
        {
            var frm = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.sql = "select id,name,nameEN,sale_price,unit from Items where IS_Deleted=0 order by id";
            frm.search = "select id,name,nameEN,sale_price,unit from Items";
            frm.StoreId = int.TryParse(cmbSafe.SelectedValue?.ToString(), out int sv) ? sv : 0;
            frm.ShowDialog();

            if (frm.ISDone && frm.ItemId > 0)
                foreach (int id in frm.Itemlist)
                    SearchByID(id, 0);
            else if (frm.ISDone && frm.ItemId <= 0 && RowIndex > -1)
                DeleteRowAt(RowIndex);
        }

        #endregion

        #region Offer Methods

        private bool CheckForItemOffer(int itemId)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select Offer.offerID,Offer.InvoiceID,OfferItems.OfferNatural," +
                    $"OfferItems.IsGroupedItems,OfferItems.unit,OfferItems.OfferTargetQnty," +
                    $"OfferItems.OfferItemValue,OfferItems.OfferItemPercentage " +
                    $"from Offer,OfferItems where OfferItems.offerId=Offer.OfferId " +
                    $"and Offer.OfferStartDate<=@CurrentDate and Offer.OfferExpire>=@CurrentDate " +
                    $"and Offer.offerType=2 and Offer.ISDeleted=0 and OfferItems.ItemID={itemId} " +
                    $"order by Offer.OfferId Desc", conn);
                adp.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double targetQty = Convert.ToDouble(dt.Rows[0]["OfferTargetQnty"]);
                    double existQty = 0;

                    foreach (var row in _gridSource)
                        if (row.ItemIdDgv == itemId.ToString() &&
                            double.TryParse(row.QuantDgv, out double q))
                            existQty += q;

                    int times = (int)(existQty / targetQty);
                    if (times > 0 && Convert.ToDouble(dt.Rows[0]["OfferNatural"]) == 1.0)
                    {
                        double offerVal = Convert.ToDouble(dt.Rows[0]["OfferItemValue"]);
                        double offerPer = Convert.ToDouble(dt.Rows[0]["OfferItemPercentage"]);
                        int unitId = Convert.ToInt32(dt.Rows[0]["unit"]);

                        if (offerVal > 0)
                            AddItemOffer(itemId, unitId, offerVal * times, 1, true);
                        else if (offerPer > 0)
                            AddItemOffer(itemId, unitId, offerPer, 1, false);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void AddItemOffer(int itemId, int unitId, double offerValue, int offerType, bool isValOffer)
        {
            try
            {
                var adp = new SqlDataAdapter(
                    $"select name,id,Code,nameEn,unit as unitId,barcode " +
                    $"from Items where IS_Deleted=0 and id={itemId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count == 0) return;

                switch (offerType)
                {
                    case 1:
                        int foundIdx = -1;
                        for (int i = 0; i < _gridSource.Count; i++)
                            if (_gridSource[i].ItemIdDgv == itemId.ToString())
                            { foundIdx = i; break; }

                        if (foundIdx >= 0)
                        {
                            double discAmt = isValOffer
                                ? offerValue
                                : (double.TryParse(_gridSource[foundIdx].SumPriceDgv, out double sp)
                                    ? sp * offerValue / 100.0 : 0);
                            _gridSource[foundIdx].discountValDgv = discAmt.ToString();
                            CalcRowValues(foundIdx);
                            CalcTot();
                        }
                        break;

                    case 2:
                        var offerRow = new PosInvoiceRow
                        {
                            No = (_gridSource.Count + 1).ToString(),
                            ItemCodeDgv = dt.Rows[0]["code"].ToString(),
                            ItemIdDgv = dt.Rows[0]["id"].ToString(),
                            ItemNameDgv = $"عرض-{dt.Rows[0]["name"]}",
                            barcodeDgv1 = dt.Rows[0]["barcode"].ToString(),
                            UnitDgv = Common.GetUnitName(unitId),
                            QuantDgv = offerValue.ToString(),
                            PercentUnitDgv = "1",
                            totQuantDgv = offerValue.ToString(),
                            priceDgv = "0",
                            SumPriceDgv = "0",
                            AvgCostDgv = "0",
                            discountValDgv = "0",
                            discountPerDgv = "0",
                            totPriceDgv = "0",
                            VatPerDgv = "0",
                            VATValDgv = "0",
                            NetValDgv = "0",
                            StockDgv = "0",
                            StoreDgv = "1"
                        };
                        _gridSource.Add(offerRow);
                        CalcTot();
                        break;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region NumPad Events

        private void CmdNum_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                txtVal1.Text += btn.Content.ToString();
                if (decimalQuan)
                {
                    decimalVal += btn.Content.ToString();
                    QuantCal(decimalVal);
                }
                else
                {
                    QuantCal(txtVal1.Text);
                }
                decimalQuan = false;
            }
        }

        private void cmdDot_Click(object sender, RoutedEventArgs e)
        {
            decimalQuan = true;
            decimalVal = txtVal1.Text + ".";
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtVal1.Text = "";
            QuantCal("0");
        }

        private void QuantCal(string no)
        {
            if (_gridSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد اصناف");
                return;
            }
            try
            {
                if (CheckEditable())
                {
                    RowIndex = GetFocusedRowIndex();
                    txtVal1.Text = no;
                    int itemId = 0;
                    int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out itemId);
                    double.TryParse(no, out double qty);
                    SetFocusedRowCellValue("QuantDgv", qty.ToString());
                    _IsUpdateDG = true;
                    CalcTot();
                    CheckForItemOffer(itemId);
                }
            }
            catch { }
        }

        #endregion

        #region Quantity / Price / Unit / Row Operations

        private void AddQnty(int val)
        {
            try
            {
                if (_gridSource.Count == 0) { DXMessageBox.Show("لا توجد اصناف"); return; }
                RowIndex = GetFocusedRowIndex();
                int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out int itemId);
                double.TryParse(GetFocusedRowCellValue("QuantDgv")?.ToString(), out double qty);
                SetFocusedRowCellValue("QuantDgv", (qty + val).ToString());
                _IsUpdateDG = true;
                CalcRowValues(RowIndex);
                CalcTot();
                CheckForItemOffer(itemId);
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void DecreaseQnty(int val)
        {
            try
            {
                if (_gridSource.Count == 0) { DXMessageBox.Show("لا توجد اصناف"); return; }
                RowIndex = GetFocusedRowIndex();
                int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out int itemId);
                double.TryParse(GetFocusedRowCellValue("QuantDgv")?.ToString(), out double qty);
                _IsUpdateDG = true;

                if (qty == 1.0)
                {
                    if (DXMessageBox.Show("هل تريد حذف السجل", "", MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        DeleteRowAt(RowIndex);
                        txtSumVal.Text = "0";
                        txtTotVAT.Text = "0";
                    }
                    _IsUpdateDG = false;
                }
                else
                {
                    SetFocusedRowCellValue("QuantDgv", (qty - val).ToString());
                    CalcRowValues(RowIndex);
                }
                CalcTot();
                CheckForItemOffer(itemId);
            }
            catch (Exception ex) { DXMessageBox.Show(ex.Message); }
        }

        private void AddUnit()
        {
            try
            {
                RowIndex = GetFocusedRowIndex();
                ShowItemUnit(RowIndex);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الوحدات\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemUnit(int index)
        {
            try
            {
                int.TryParse(GetRowCellValue(index, "ItemIdDgv")?.ToString(), out int itemId);
                var frm = new frmItemUnits();
                frm.ItemId = itemId;
                frm.PreUnit = Common.GetUnitID(GetRowCellValue(index, "UnitDgv")?.ToString() ?? "");
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

        private void deleteRow()
        {
            if (_gridSource.Count == 0) return;
            RowIndex = GetFocusedRowIndex();
            if (RowIndex == -1) { DXMessageBox.Show("برجى اختيار السجل الذى تريد حذفه"); return; }

            if (DXMessageBox.Show("هل تريد حذف السجل", "", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                DeleteRowAt(RowIndex);
                CalcTot();
            }
        }

        private void quantityEdit()
        {
            try
            {
                if (_gridSource.Count == 0) { DXMessageBox.Show("لا توجد اصناف"); return; }
                RowIndex = GetFocusedRowIndex();
                int.TryParse(GetRowCellValue(RowIndex, "ItemIdDgv")?.ToString(), out int itemId);

                var calc = new Frm_Calculator();
                calc.ShowDialog();
                if (!calc.is_close)
                {
                    double.TryParse(calc.TextBox1.Text, out double qty);
                    SetRowCellValue(RowIndex, "QuantDgv", qty.ToString());
                }
                _IsUpdateDG = true;
                CalcRowValues(RowIndex);
                CalcTot();
                CheckForItemOffer(itemId);
            }
            catch { }
        }

        private void priceEdit()
        {
            if (_gridSource.Count == 0) { DXMessageBox.Show("لا يوجد اصناف"); return; }
            if (!User.EditPrice) { DXMessageBox.Show("لا يمكن تعديل السعر"); return; }
            RowIndex = GetFocusedRowIndex();
            var calc = new Frm_Calculator();
            calc.ShowDialog();
            if (!calc.is_close)
            {
                double.TryParse(calc.TextBox1.Text, out double price);
                SetRowCellValue(RowIndex, "priceDgv", price.ToString());
                IsValidPrice(RowIndex);
                CalcRowValues(RowIndex);
                CalcTot();
            }
        }

        private void IsValidPrice(int index)
        {
            try
            {
                int.TryParse(GetRowCellValue(index, "ItemIdDgv")?.ToString(), out int itemId);
                var adp = new SqlDataAdapter(
                    $"select sale_price,low_sale_price,high_sale_price from ItemPrices " +
                    $"where ItemId={itemId}", conn);
                var dt = new DataTable();
                adp.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    double.TryParse(GetRowCellValue(index, "priceDgv")?.ToString(), out double curPrice);
                    double highPrice = Convert.ToDouble(dt.Rows[0]["high_sale_price"]);
                    double lowPrice = Convert.ToDouble(dt.Rows[0]["low_sale_price"]);

                    if (highPrice > 0 && !User.PassHeighestSalePrice && curPrice > highPrice)
                        DXMessageBox.Show("لقد أدخلت السعر أكبر من أعلى سعر");

                    if (lowPrice > 0 && !User.PassLowestSalePrice && curPrice < lowPrice)
                    {
                        DXMessageBox.Show("لقد أدخلت السعر أقل من أدنى سعر");
                        SetRowCellValue(index, "priceDgv", dt.Rows[0]["sale_price"].ToString());
                    }
                }
            }
            catch { }
        }

        private bool CheckEditable()
        {
            if (Code != -1 && ProcType != 3)
            {
                DXMessageBox.Show("لا يمكن التعديل على الفاتورة");
                return false;
            }
            if (ISreturn && linkedInvRet)
            {
                DXMessageBox.Show("لا يمكن التعديل على الفاتورة");
                return false;
            }
            return true;
        }

        #endregion

        #region CLR (Clear / New)

        private void CLR()
        {
            string savedSrcInv = txtSrcInvNo.Text;

            cmbProcTypeSrch.SelectedIndex = 0;
            Code = -1;
            RefrNo = "-1";
            ProcCode = -1;
            _IsUpdateDG = false;
            InvGlobalID = "";
            EntryGlobalID = "";
            txtClient.Text = "";
            ClientId = -1;

            if (cmbSafe.Items.Count > 0)
                cmbSafe.SelectedIndex = 0;

            InvDiscount = 0.0;
            txtrec1.Text = "0";
            TotalVal = 0.0;
            Paytype = 0;
            OrderType = 0;
            linkedInvRet = false;
            ISreturn = false;
            IsBarcode = false;
            ProcCode = -1;
            InvType = 3;
            ProcType = 1;
            ISTRialEnd = false;
            RowIndex = -1;
            PayNetwork = 0.0;
            Paycash = 0.0;
            Payvisa = 0.0;
            InvDiscount = 0.0;
            AdditionCost = 0.0;
            SrchPrint = false;
            DeliVal = 0.0;
            RahenVal = 0.0;
            IsEnterNo = false;
            txtInvTime = DateTime.Now;
            ClientId = -1;
            SalemanId = -1;
            live = 0;
            holdbool = false;
            savebool = false;

            txtAdditionVal.Text = "0";
            txtSumVal.Text = "0";
            txtTotQuant.Text = "0";
            txtItemsNo1.Text = "0";
            txtTotVAT.Text = "0";
            txtrec1.Text = "0";
            txtrecrem1.Text = "0";
            txtNet.Text = "0";
            txtDiscountVal.Text = "0";
            txtInvProfit.Text = "0";

            // تحديث لون زر الدفع
            btnPay.Background = new SolidColorBrush(Colors.Green);
            txtNet.Foreground = Brushes.White;
            btnPay.Content = string.Equals(MainClass.Language, "ar",
                StringComparison.OrdinalIgnoreCase)
                ? "💰 إدفع الآن  F6" : "Pay Now F6";

            ISreturn = false;
            ISNew = true;

            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;
            txtDate.SelectedDate = DateTime.Today;
            lblDate.Text = DateTime.Today.ToShortDateString();

            _gridSource.Clear();
            LoadInvNo();
            GrpPageNo = 0;
            ItemPageNo = 0;
            ShowGroups(0);

            txtBarcode.Text = "";
            txtSrcInvNo.Text = savedSrcInv;
            Invo = null;

            ClearTempFields();
            dtSeialNo.Rows.Clear();

            txtBarcode.Focus();
        }

        #endregion

        #region Pay Bill & Save

        public void PayBill()
        {
            try
            {
                if (InvObj.SaleManIsRequire && SalemanId < 1)
                {
                    DXMessageBox.Show("يجب اختيار المندوب");
                    txtSaleman.Focus();
                    return;
                }

                double.TryParse(txtNet.Text, out double netVal);
                if (netVal <= 0.0) return;

                if (!InvObj.ShowPayfrmReturn && ISreturn)
                {
                    Paytype = 1;
                    Paycash = netVal;
                    InvDiscount = 0.0;
                    txtDiscountVal.Text = "0";
                    IsPrinted = true;
                    SaveAndPrint();
                    return;
                }

                var payFrm = new frmPOSPay();
                MainClass.ApplyPermissionToForm(payFrm);
                MainClass.DoApplyUserSett(payFrm);

                double.TryParse(txtDiscountVal.Text, out double existDisc);
                payFrm.ExtraDiscounts = existDisc;
                payFrm.lblNetVal.Text = Math.Round(netVal, 2).ToString();
                payFrm.Invoic.Net = netVal;
                payFrm.Invoic.VATperc = InvObj.VAT;
                payFrm.Invoic.ExtraVATPerc = InvObj.AdditionalTax;
                payFrm.Invoic.PriceIncVAT = InvObj.PriceIncVAT;
                double.TryParse(txtrec1.Text, out double paid);
                payFrm.Invoic.Paid = paid;

                if (Paytype == -1)
                    payFrm.Invoic.PayType = -1;
                else
                    Paytype = 1;

                payFrm.ShowDialog();
                if (!payFrm.isDone) return;

                Paytype = payFrm.Invoic.PayType;
                byVisa = payFrm.Invoic.PayType == 2;
                PayNetwork = payFrm.resNetwork;
                Paycash = payFrm.resCash;
                InvDiscount = double.Parse(payFrm.txtInvDiscount.Text);
                double.TryParse(payFrm.lblNetVal.Text, out double newNet);
                txtNet.Text = Math.Round(newNet, 2).ToString();
                DeliVal = payFrm.DeliveryVal;
                RahenVal = payFrm.RahenVal;
                AdditionCost = payFrm.DeliveryVal + payFrm.RahenVal;
                txtDiscountVal.Text = payFrm.txtTotDiscounts.Text;
                txtTotVAT.Text = payFrm.TaxVal.ToString();
                txtrec1.Text = payFrm.resTotal.ToString();
                OrderType = payFrm.Invoic.OrderType;
                txtOrderType = OrderType >= 0 ? $"({payFrm.txtOrderType})" : "";

                payFrm.Close();

                if (string.IsNullOrEmpty(txtrec1.Text))
                {
                    DXMessageBox.Show("برجاء ادخال القيمة المستلمة");
                    return;
                }

                IsPrinted = true;
                SaveAndPrint();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء إدخال الدفع\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveAndPrint()
        {
            var invoiceOper = new InvoiceOper();
            var inv = BindToInvoice();
            var entry = (Paytype == 5 || Paytype == -1) ? invoiceOper.BindToEntry(inv) : null;

            bool saved = invoiceOper.SaveInvoice(inv, entry, ISNew);
            Inventory.UpdateItemStock(inv);

            if (saved && IsPrinted && print.PrintNo > 0)
                RptPrint(inv, 1);

            if (saved && Sync.ActiveSync && InvObj.SyncInv && Sync.SyncType > 0 && !holdbool)
            {
                if (!InvObj.SyncEntry) entry = null;
                await invoiceOper.SyncInvoice(inv, entry, ISNew);
            }
            CLR();
        }

        #endregion

        #region Navigate & ReadData

        public void Navigate(string sqlStr)
        {
            dgvSrch.Items.Clear();

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
                ISNew = false;
               // Label9.Visibility = Visibility.Visible;
                Label15.Visibility = Visibility.Collapsed;

                ProcCode = Convert.ToInt32(dr["proc_id"] ?? 0);
                InvGlobalID = dr["InvGlobalID"].ToString();
                RestSaleCode = Convert.ToInt32(dr["EntryID"] ?? 0);
                EntryGlobalID = $"{dr["branch"]}-{RestSaleCode}";
                ProcType = Convert.ToInt32(dr["proc_type"]);
                Code = Convert.ToInt32(dr["id"]);
                txtNo.Text = Code.ToString();

                var invDate = Convert.ToDateTime(dr["date"]);
                txtDate.SelectedDate = invDate;
                lblDate.Text = invDate.ToShortDateString();
                //احتمال
                //Label9.Text = invDate.ToLongTimeString();
                Label15.Text = invDate.ToLongTimeString();

                Paytype = Convert.ToInt32(dr["pay_type"]);
                ClientId = Convert.ToInt32(dr["cust_id"]);
                txtClient.Text = Common.GetClientName(ClientId);

                cmbSafe.SelectedValue = dr["safe"].ToString();
                cmbSalesMen.SelectedValue = dr["salesman"].ToString();
                SalemanId = Convert.ToInt32(dr["salesman"]);
                txtSaleman.Text = (cmbSalesMen.SelectedItem as DataRowView)?["name"]?.ToString() ?? "";

                txtNetWithoutVAT.Text = $"{Convert.ToDouble(dr["InvTotal"]):0.##}";
                txtSumVal.Text = $"{Convert.ToDouble(dr["InvTotal"]):0.##}";
                txtNet.Text = $"{Convert.ToDouble(dr["tot_net"]):0.##}";
                txtTotVAT.Text = $"{Math.Round(Convert.ToDouble(dr["tax"]), 2):0.##}";
                txtInvDiscount.Text = dr["minus"].ToString();
                txtDiscountVal.Text = dr["minus"].ToString();
                InvDiscount = double.Parse(txtDiscountVal.Text);
                txtrec1.Text = $"{Convert.ToDouble(dr["paid"]):0.##}";
                txtrecrem1.Text = $"{Math.Round(Convert.ToDouble(dr["paid"]) - Convert.ToDouble(dr["tot_net"]), 2):0.##}";
                Paycash = Convert.ToDouble(dr["cash"]);
                PayNetwork = Convert.ToDouble(dr["visa"]);
                OrderType = Convert.ToInt32(dr["OrderType"]);

                if (ProcType == 1 || ProcType == 2) SrchPrint = true;
                if (ProcType == 2) txtSrcInvNo.Text = dr["Reff_No"].ToString();

                dr.Close();

                // تحميل الأصناف
                var adp = new SqlDataAdapter(
                    $"select * from Inv_Sub where productId=0 and InvGlobalID=N'{InvGlobalID}' order by id desc",
                    conn);
                var dt = new DataTable();
                adp.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        int itemId = Convert.ToInt32(row["ItemId"]);
                        int unitId = Convert.ToInt32(row["unit"]);
                        double val1 = Convert.ToDouble(row["val1"]);
                        double exPr = Convert.ToDouble(row["exchange_price"]);
                        double sumP = Math.Round(val1 * exPr, 3);
                        double disc = double.Parse(row["discount"].ToString());
                        double taxV = Convert.ToDouble(row["taxval"]);
                        double totP = Math.Round(sumP - disc, 2);
                        if (InvObj.PriceIncVAT) totP -= taxV;

                        decimal perc = 1m;
                        var adpU = new SqlDataAdapter(
                            $"select ItemUnits.perc,ItemUnits.barcode from Items,ItemUnits " +
                            $"where Items.id=ItemUnits.ItemId and ItemUnits.unit={row["unit"]} " +
                            $"and ItemUnits.ItemId={row["ItemId"]}", conn);
                        var dtU = new DataTable();
                        adpU.Fill(dtU);
                        string bc = "";
                        if (dtU.Rows.Count > 0)
                        {
                            perc = new decimal(Convert.ToDouble(dtU.Rows[0][0]));
                            bc = dtU.Rows[0]["barcode"].ToString();
                        }

                        string storeVal = "0";
                        if (row["store"] != null && row["store"] != DBNull.Value &&
                            double.TryParse(row["store"].ToString(), out double sv2) && sv2 > 0)
                            storeVal = Convert.ToInt32(sv2).ToString();

                        var gr = new PosInvoiceRow
                        {
                            No = (dt.Rows.IndexOf(row) + 1).ToString(),
                            ItemCodeDgv = Common.GetItemCode(itemId),
                            ItemIdDgv = row["ItemId"].ToString(),
                            ItemNameDgv = Common.GetItemName(itemId),
                            DescriptionDgv = row["Description"].ToString(),
                            barcodeDgv1 = bc,
                            UnitDgv = Common.GetUnitName(unitId),
                            QuantDgv = row["val1"].ToString(),
                            PercentUnitDgv = perc.ToString(),
                            totQuantDgv = row["val"].ToString(),
                            priceDgv = $"{exPr:0.00}",
                            SumPriceDgv = $"{sumP:0.00}",
                            AvgCostDgv = (row["AvrgCost"] == null || row["AvrgCost"] == DBNull.Value)
                                             ? "0" : $"{row["AvrgCost"]:0.00}",
                            discountValDgv = $"{disc:0.00}",
                            discountPerDgv = sumP > 0 ? $"{disc / sumP * 100:0.00}" : "0",
                            totPriceDgv = $"{totP:0.00}",
                            VatPerDgv = $"{row["taxperc"]:0.00}",
                            VATValDgv = $"{taxV:0.00}",
                            NetValDgv = $"{totP + taxV:0.00}",
                            StockDgv = "0",
                            StoreDgv = storeVal
                        };
                        _gridSource.Add(gr);
                    }
                    catch { }
                }

                CalcTot();

                // تلوين زر الدفع حسب نوع الحركة
                UpdatePayButtonColor();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الفاتورة\nتفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePayButtonColor()
        {
            if (ProcType == 2 && Paytype != -1)
            {
                btnPay.Background = new SolidColorBrush(Colors.Brown);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "فاتورة مرتجع" : "Return Inv";
            }
            else if (ProcType == 1 && Paytype != -1)
            {
                btnPay.Background = new SolidColorBrush(Colors.Green);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "فاتورة بيع" : "Sales Inv";
            }
            else if (Paytype == -1 && ProcType == 1)
            {
                btnPay.Background = new SolidColorBrush(Colors.Goldenrod);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "فاتورة بيع آجل" : "POSTPONED SALES Inv";
            }
            else if (Paytype == -1 && ProcType == 2)
            {
                btnPay.Background = new SolidColorBrush(Colors.Goldenrod);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "فاتورة مرتجع آجل" : "POSTPONED RETURN SALES Inv";
            }
        }

        #endregion

        #region Delete Invoice

        private void DeleteInv()
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر فاتورة ليتم حذفها", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                var adp = new SqlDataAdapter(
                    "select id from CasherClosed where startTime<=@date and endTime>=@date", conn);
                adp.SelectCommand.Parameters.Add("@date", SqlDbType.DateTime).Value =
                    txtDate.SelectedDate ?? DateTime.Today;
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DXMessageBox.Show("نأسف! لا يمكن حذف فاتورة بعد إغلاق اليومية", "",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                for (int i = 0; i <= 7; i++)
                    if (Code == HoldList[i]) { HoldList[i] = 0; UpdateHoldOrder(); }

                new SqlCommand(
                    $"update Inv set IS_Deleted=1 where InvGlobalID=N'{InvGlobalID}'", conn)
                    .ExecuteNonQuery();

                if (RestSaleCode != 0)
                    new SqlCommand(
                        $"update Entry set IS_Deleted=1 where GlobalID=N'{EntryGlobalID}'", conn)
                        .ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                dgvSrch.Items.Clear();
                CLR();
                ShowGroups(0);
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

        #endregion

        #region Hold Orders

        public void CheckHold()
        {
            try
            {
                for (int i = 0; i <= 8; i++) HoldList[i] = 0;
                var frmUserLogin = new frmUserLogin();
                var adp = new SqlDataAdapter(
                    "select InvGlobalID,id from inv where date>=@date1 and date<=@date2 " +
                    "and inv_type=3 and proc_type=3 and IS_Deleted=0 order by id", conn1);
                adp.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                    frmUserLogin.datelogin;
                adp.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                    (txtDate.SelectedDate ?? DateTime.Today).AddDays(1.0);
                var dt = new DataTable();
                adp.Fill(dt);

                for (int i = 0; i < dt.Rows.Count && i <= 8; i++)
                    HoldList[i] = Convert.ToInt32(dt.Rows[i]["id"]);

                UpdateHoldOrder();
            }
            catch { }
        }

        public void UpdateHoldOrder()
        {
            var holdBtns = new[] { Hold1Btn, Hold2Btn, Hold3Btn, Hold4Btn,
                                   Hold5Btn, Hold6Btn, Hold7Btn, Hold8Btn, Hold9Btn };

            for (int i = 0; i < holdBtns.Length; i++)
            {
                if (HoldList[i] != 0)
                {
                    holdBtns[i].IsEnabled = true;
                    holdBtns[i].Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    holdBtns[i].IsEnabled = false;
                    holdBtns[i].Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                }
            }
        }

        private void order_Hold()
        {
            try
            {
                holdbool = true;
                IsPrinted = false;
                ProcType = 3;
                LoadInvNo();

                int holdSlot = -1;
                for (int i = 0; i <= 8; i++)
                    if (HoldList[i] == 0) { holdSlot = i; break; }

                if (holdSlot == -1)
                {
                    DXMessageBox.Show("لقد وصلت للحد الاقصي من عمليات الايقاف المؤقت");
                    ProcType = 1;
                    return;
                }

                int.TryParse(txtNo.Text, out int invNo);
                HoldList[holdSlot] = invNo;
                SaveAndPrint();
                UpdateHoldOrder();
                btnNew_Click(null, null);
            }
            catch { }
        }

        private void HoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (_gridSource.Count > 0)
                {
                    DXMessageBox.Show("يوجد أصناف في الجدول");
                    return;
                }
                int idx = new List<Button>
                    { Hold1Btn, Hold2Btn, Hold3Btn, Hold4Btn,
                      Hold5Btn, Hold6Btn, Hold7Btn, Hold8Btn, Hold9Btn }
                    .IndexOf(btn);
                if (idx >= 0 && HoldList[idx] != 0)
                {
                    dgvRowChng(HoldList[idx], 1);
                    live = 1;
                }
            }
        }

        #endregion

        #region Search Methods

        private void LoadDG(string cond)
        {
            var searchRows = new List<PosSearchRow>();
            int isBuy = InvType == 3 ? 0 : 1;
            int selIdx = cmbProcTypeSrch.SelectedIndex;

            var adp = new SqlDataAdapter(
                $"select Inv.InvGlobalID,Inv.id as id,Inv.date as date," +
                $"Customers.name as cust from Inv,Customers " +
                $"where inv_type=3 and proc_type={(selIdx + 1)} and IS_Buy={isBuy} " +
                $"and Inv.IS_Deleted=0 and {cond} Inv.cust_id=Customers.id " +
                $"order by Inv.id DESC", conn);

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
                searchRows.Add(new PosSearchRow
                {
                    InvGlobalID = row["InvGlobalID"].ToString(),
                    InvNo = row["id"].ToString(),
                    InvDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                    InvTime = Convert.ToDateTime(row["date"]).ToLongTimeString(),
                    ClientName = row["cust"].ToString()
                });
            }

            dgvSrch.ItemsSource = searchRows;
            dgvSrch.Items.Refresh();
        }

        private void Search(int type)
        {
            string branchFilter = MainClass.BranchNo != -1
                ? $"inv.branch={MainClass.BranchNo} and " : "";

            string cond = string.IsNullOrEmpty(txtSrchNo.Text)
                ? branchFilter + " date>=@date1 and date<=@date2 and "
                : branchFilter + $" inv.id={txtSrchNo.Text} and ";

            LoadDG(cond);
        }

        private void dgvRowChng(int rowIndex, int type)
        {
            try
            {
                if (type == 2)
                {
                    if (dgvSrch.ItemsSource is List<PosSearchRow> rows &&
                        rowIndex >= 0 && rowIndex < rows.Count)
                    {
                        InvGlobalID = rows[rowIndex].InvGlobalID;
                        Navigate($"select * from Inv where Inv_type={InvType} " +
                                 $"AND InvGlobalID=N'{InvGlobalID}'");
                    }
                }
                else
                {
                    ProcCode = rowIndex;
                    Navigate($"select * from Inv where id={ProcCode} " +
                             $"AND Inv_type={InvType} AND proc_type=3");
                }
            }
            catch { }
        }

        private void InsertFromInv(int ptype) { /* يُكمَل حسب المنطق التجاري */ }

        #endregion

        #region Print Methods

        private void RptPrint(Invoice inv, int type)
        {
            if (_gridSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات شراء أو بيع بالجدول", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "RptPOS.repx";

            string path = System.IO.Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            string rptName = print.RptName;
            string defPrinter = print.defPrinter;
            if (type == 3) rptName = "RptPOSA4.repx";
            if (type == 4) defPrinter = "Microsoft Print to PDF";

            var ds = print.BindToData(inv);
            print.Printing(type, ds, print.RptUrl, rptName, defPrinter,
                print.kitchenprinter, print.PrintNo);

            if (!ISreturn)
                Print_SubDev(ds.Tables[0]);
        }

        private void Print_SubDev(DataTable dtt)
        {
            try
            {
                string rptName = "rptPOSsub.repx";
                var adp = new SqlDataAdapter(
                    "Select ItemsCategory.id, ItemsCategory.printer from ItemsCategory " +
                    "where IS_Deleted=0 and printer is not Null and len(printer)>0", conn1);
                var dt = new DataTable();
                adp.Fill(dt);

                foreach (DataRow catRow in dt.Rows)
                {
                    string printer = catRow[1].ToString();
                    if (string.IsNullOrEmpty(printer)) continue;

                    var subDt = dtt.Clone();
                    subDt.TableName = catRow[0].ToString();

                    foreach (DataRow itemRow in dtt.Rows)
                    {
                        var adp2 = new SqlDataAdapter(
                            $"Select id from Items where Items.group_id={catRow[0]} " +
                            $"And Items.id={itemRow["ItemID"]}", conn1);
                        var dt2 = new DataTable();
                        adp2.Fill(dt2);
                        if (dt2.Rows.Count > 0)
                            subDt.Rows.Add(itemRow.ItemArray);
                    }

                    if (subDt.Rows.Count > 0)
                    {
                        try
                        {
                            var ds = new DataSet();
                            ds.Tables.Add(subDt);
                            print.Printing(1, ds, print.RptUrl, rptName, printer, "", 1);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region BindToInvoice

        private void GetInvoiceIDs()
        {
            if (string.IsNullOrEmpty(txtNetWithoutVAT.Text))
                txtNetWithoutVAT.Text = "0";

            if (conn.State != ConnectionState.Open) conn.Open();

            if (ProcCode != -1)
            {
                if (ProcType == 2) return;
                for (int i = 0; i <= 8; i++)
                    if (Code == HoldList[i])
                    {
                        HoldList[i] = 0;
                        UpdateHoldOrder();
                        ProcType = 1;
                        LoadInvNo();
                        int.TryParse(txtNo.Text, out Code);
                    }
            }
            else
            {
                ProcCode = (int)Math.Round(
                    Convert.ToDouble(
                        new SqlCommand("select ISNULL(MAX(proc_id),0) from Inv", conn)
                            .ExecuteScalar()) + 1.0);
                LoadInvNo();
                int.TryParse(txtNo.Text, out Code);

                if (Convert.ToDouble(txtNetWithoutVAT.Text) != 0 &&
                    (Paytype == 5 || Paytype == -1))
                {
                    RestSaleCode = (int)Math.Round(
                        Convert.ToDouble(
                            new SqlCommand(
                                $"select ISNULL(MAX(id),0) from Entry where branch={MainClass.BranchNo}",
                                conn).ExecuteScalar()) + 1.0);
                }
            }
        }

        private Invoice BindToInvoice()
        {
            var invoice = new Invoice();
            double.TryParse(txtrec1.Text, out double paid);
            double.TryParse(txtNet.Text, out double net);
            double remainder = Math.Round(paid - net, 2);

            if (ProcCode == -1)
            {
                GetInvoiceIDs();
                InvGlobalID = $"{MainClass.BranchNo}-{ProcCode}";
                EntryGlobalID = (Paytype == 5 || Paytype == -1)
                    ? $"{MainClass.BranchNo}-{RestSaleCode}" : "-1";
            }
            else if (ProcCode != -1 && ProcType == 3)
            {
                GetInvoiceIDs();
            }

            invoice.AutoIncrementID = ProcCode;
            invoice.ClientCode = Sync.ClientCode;
            invoice.Total = Convert.ToDouble(txtNetWithoutVAT.Text);
            invoice.SumPrice = Convert.ToDouble(txtSumVal.Text);
            invoice.VAT = Math.Round(double.Parse(txtTotVAT.Text), 2);
            invoice.Net = Math.Round(net, 2);
            invoice.Discount = Math.Round(InvDiscount, 2);
            invoice.TotDiscount = double.Parse(txtDiscountVal.Text);
            invoice.Additions = Math.Round(AdditionCost, 2);
            invoice.Insurance = Math.Round(RahenVal, 2);
            invoice.Paycash = Paycash;
            invoice.PayATM = Math.Round(PayNetwork, 2);
            invoice.Paid = paid;
            invoice.Remainder = remainder;
            invoice.InvProfit = double.Parse(txtInvProfit.Text);
            invoice.InvGlobalID = InvGlobalID;
            invoice.EntryGlobalID = EntryGlobalID;
            invoice.InvoiceNo = int.TryParse(txtNo.Text, out int invNo) ? invNo : 0;
            invoice.InvoiceType = (InvoiceType)InvType;
            invoice.ProcType = ProcType;
            invoice.OrderType = OrderType;
            invoice.InvDate = txtDate.SelectedDate ?? DateTime.Today;
            invoice.User = MainClass.EmpNo;
            invoice.Branch = MainClass.BranchNo;
            invoice.Treasury = MainClass.UserTreasury;
            invoice.Saleman = SalemanId;
            invoice.IsDeleted = false;
            invoice.Customer = ClientId == -1 ? 1 : ClientId;
            invoice.VATperc = InvObj.VAT;
            invoice.PayType = Paytype;
            invoice.Store = cmbSafe.SelectedIndex > -1
                ? (int.TryParse(cmbSafe.SelectedValue?.ToString(), out int st) ? st : 1) : 1;

            if (ProcType == 2)
            {
                invoice.ReffNo = RefrNo;
                invoice.RefDate = RefrDate;
            }
            else
            {
                invoice.ReffNo = "-1";
                invoice.RefDate = DateTime.Now;
            }

            invoice.InvAccCode = ProcType == 1 ? InvObj.InvAcc : InvObj.InvReturnAcc;

            var list = new List<Item>();
            foreach (var row in _gridSource)
            {
                int.TryParse(row.ItemIdDgv, out int rowItemId);
                var item = new Item
                {
                    InvGlobalID = InvGlobalID,
                    AutoIncrementID = ProcCode,
                    Code = row.ItemCodeDgv,
                    Name = row.ItemNameDgv,
                    ItemNo = rowItemId,
                    ProcType = ProcType == 1 ? 2 : (ProcType == 2 ? 1 : 0),
                    Description = row.DescriptionDgv,
                    Quantity = double.Parse(row.QuantDgv),
                    UnitEquality = double.Parse(row.PercentUnitDgv),
                    PrimaryQnty = double.Parse(row.totQuantDgv),
                    Unit = Common.GetUnitID(row.UnitDgv),
                    Price = double.Parse(row.priceDgv),
                    AvegCost = double.Parse(row.AvgCostDgv),
                    Vat = double.Parse(row.VATValDgv),
                    VatPerc = double.Parse(row.VatPerDgv),
                    Barcode = row.barcodeDgv1,
                    ItemDiscount = double.Parse(row.discountValDgv),
                    Store = double.Parse(row.StoreDgv),
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = "",
                    ProductId = 0
                };
                list.Add(item);
            }
            invoice.Items = list;
            return invoice;
        }

        #endregion

        #region Serial Numbers

        private void SaveSeialNo(int itemId, string serialNo, string invGlobalId)
        {
            try
            {
                if (dtSeialNo.Rows.Count > 0)
                {
                    if (conn.State != ConnectionState.Open) conn.Open();
                    var cmd = new SqlCommand(
                        "INSERT into ItemSerialNo(ItemId,SerialNo,InvGlobalID) " +
                        "values(@ItemId,@SerialNo,@InvGlobalID)", conn);
                    cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                    cmd.Parameters.Add("@SerialNo", SqlDbType.NVarChar).Value = serialNo;
                    cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalId;
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        #endregion

        #region Button Click Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (_gridSource.Count > 0 && ProcCode == -1)
            {
                if (DXMessageBox.Show("لم يتم حفظ الفاتورة, هل تريد جديد", "تنبيه",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CLR(); LoadInvNo(); txtNet.Text = "0";
                }
            }
            else
            {
                CLR(); LoadInvNo(); txtNet.Text = "0";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtNet.Text)) txtNet.Text = "0";

            if (Paytype == -1 && ClientId < 2)
            {
                DXMessageBox.Show("يجب إختيار عميل آجل");
                return;
            }
            if (Code != -1 && ProcType != 3) return;

            if (ISreturn)
            {
                if (ISLinkedreturn && InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
                    DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                else if (DXMessageBox.Show("هل تريد إرجاع الفاتورة", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    PayBill();
                    btnPay.Background = new SolidColorBrush(Colors.Green);
                    btnPay.Content = "💰 إدفع الآن  F6";
                    ISreturn = false;
                }
            }
            else
            {
                PayBill();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search(2);

        private void btnAddItem_Click(object sender, RoutedEventArgs e) => addNewItem();

        private void btnClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmSrchClient { Type = 1 };
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Clientname))
                {
                    txtClient.Text = frm.Clientname;
                    ClientId = frm.ClientId;
                }
            }
            catch { }
        }

        private void btnPostPon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmSrchClient { Type = 1, PostponeClient = true };
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Clientname))
                {
                    txtClient.Text = frm.Clientname;
                    ClientId = frm.ClientId;
                    Paytype = -1;
                    btnPay.Background = new SolidColorBrush(Colors.Goldenrod);
                    btnPay.Content = string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase) ? "بيع آجل" : "POSTPONED SALE";
                }
            }
            catch { }
        }

        private void btnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GetFocusedRowIndex() >= 0 && CheckEditable())
                    AddUnit();
            }
            catch { }
        }

        private void btnQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEditable()) quantityEdit();
        }

        private void btnPrice_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEditable()) priceEdit();
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e) => deleteRow();

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_gridSource.Count > 0 &&
                DXMessageBox.Show("هل انت متأكد من حذف الفاتورة", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                DeleteInv();
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            txtVal1.Text = "";
            int idx = GetFocusedRowIndex();
            if (idx > 0)
                GridControl1.SelectedIndex = idx - 1;
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            txtVal1.Text = "";
            int idx = GetFocusedRowIndex();
            if (idx < _gridSource.Count - 1)
                GridControl1.SelectedIndex = idx + 1;
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEditable()) AddQnty(1);
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEditable()) DecreaseQnty(1);
        }

        private void RetSales_Click(object sender, RoutedEventArgs e)
        {
            if (_gridSource.Count > 0 &&
                DXMessageBox.Show("لم يتم حفظ الفاتورة, هل تريد الاستمرار", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CLR();

            var pwd = new frmCheckPwd { operNo = 2 };
            pwd.ShowDialog();
            if (!pwd.Iscorrect) return;

            short retType = 1;
            if (InvObj.LinkedReturn && InvObj.UnLinkedReturn)
            {
                var typeFrm = new frmInvReturnTypes();
                typeFrm.ShowDialog();
                retType = (short)typeFrm.ReturnType;
            }
            else if (!InvObj.LinkedReturn && InvObj.UnLinkedReturn)
                retType = 2;

            if (retType == 1)
            {
                btnPay.Background = new SolidColorBrush(Colors.Brown);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "إرجاع" : "Return";
                ISreturn = true;
                linkedInvRet = true;
                ProcType = 2;
                ISNew = true;
                InsertFromInv(1);
            }
            else if (retType == 2)
            {
                ProcType = 2;
                ProcCode = -1;
                ISreturn = true;
                ISNew = true;
                btnPay.Background = new SolidColorBrush(Colors.Brown);
                btnPay.Content = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? "إرجاع" : "Return";
            }
        }

        private void btnHoldOrd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_gridSource.Count > 0)
                {
                    if (Code == -1)
                    {
                        if (DXMessageBox.Show("هل تريد تعليق الفاتورة", "",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            order_Hold();
                    }
                    else
                        DXMessageBox.Show("لا يمكن تعليق الفاتورة");
                }
                else
                    DXMessageBox.Show("من فضلك ضف الأصناف اولاً");
            }
            catch { }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            var msg = new MsgPrint();
            msg.ShowDialog();
            int action = msg.Action;
            if (SrchPrint && action > 0 && (ProcType == 1 || ProcType == 2))
                RptPrint(BindToInvoice(), action);
        }

        private void btnCloseDay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pwd = new frmCheckPwd { operNo = 1 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) return;

                CheckHold();
                for (int i = 0; i <= 8; i++)
                    if (HoldList[i] != 0)
                    {
                        DXMessageBox.Show("لا تستطيع إغلاق اليومية !! يوجد فواتير معلقة");
                        return;
                    }
                var Home = new Home();
                var shift = new frmCloseShift();
                shift.Activate();
                if (shift.NoInvsFound(1))
                {
                    DXMessageBox.Show("لا يوجد فواتير جديدة يمكن إغلاقها");
                    Home.IsCashierClosed = true;
                    return;
                }
                shift.closeShift(1);
                if (Home.IsCashierClosed) Close();
            }
            catch { }
        }

        private void btnMunalBarcode_Click(object sender, RoutedEventArgs e)
        {
            var calc = new Frm_Calculator { AllowChar = true };
            calc.ShowDialog();
            if (!calc.is_close)
                txtBarcode.Text = calc.TextBox1.Text;
        }

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvoiceSrch();
            frm.cmbProcType.IsEnabled = true;
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ProcType = ProcType;
            frm.InvType = InvType;
            frm.ShowDialog();
            if (frm.ISDone && frm.InvGlobalID != "-1")
                Navigate($"select * from Inv where branch={MainClass.BranchNo} " +
                         $"and InvGlobalID=N'{frm.InvGlobalID}'");
        }

        private void btnNextInv_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtNo.Text, out double curNo);
            Navigate($"select top 1 * from Inv where inv_type={InvType} and IS_Deleted=0 " +
                     $"and id>{curNo} and (proc_type=1 or proc_type=2) " +
                     $"and branch={MainClass.BranchNo} order by id asc");
        }

        private void btnPreviousInv_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtNo.Text, out double curNo);
            Navigate($"select top 1 * from Inv where inv_type={InvType} and IS_Deleted=0 " +
                     $"and id<{curNo} and (proc_type=1 or proc_type=2) " +
                     $"and branch={MainClass.BranchNo} order by id desc");
        }

        private void btnGrpRight_Click(object sender, RoutedEventArgs e)
        {
            if (GrpPageNo <= dtGrp.Rows.Count / GrpsInPage)
            {
                GrpPageNo++;
                ShowGroups(GrpPageNo);
            }
        }

        private void btnGrpLeft_Click(object sender, RoutedEventArgs e)
        {
            GrpPageNo--;
            ShowGroups(GrpPageNo);
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            ItemPageNo = 0;
            ShowItem(ref ItemPageNo);
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            ItemPageNo = Math.Max(0,
                (int)Math.Round((double)dtItems.Rows.Count / ItemsInPage - 1.0));
            ShowItem(ref ItemPageNo);
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            ItemPageNo--;
            ShowItem(ref ItemPageNo);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            ItemPageNo++;
            if (dtItems.Rows.Count >= ItemPageNo * ItemsInPage)
                ShowItem(ref ItemPageNo);
            else
                ItemPageNo--;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        #endregion

        #region Barcode

        private void txtBarcode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return) ReadBarcode();
        }

        private void txtBarcode_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBarcode.Text == "بحث باركود")
            {
                txtBarcode.Text = "";
                txtBarcode.Foreground = Brushes.Black;
            }
        }

        private void txtBarcode_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtBarcode.Text))
            {
                txtBarcode.Text = "بحث باركود";
                txtBarcode.Foreground = Brushes.Gray;
            }
        }

        private void ReadBarcode()
        {
            try
            {
                string barcode = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(barcode)) { addNewItem(); return; }

                // البحث في ItemUnits
                var adp = new SqlDataAdapter(
                    $"select Items.id,Items.group_id,ItemUnits.unit,Items.tax," +
                    $"Items.tax_group,Items.Wscale from Items,ItemUnits " +
                    $"where Items.IS_Deleted=0 and ItemUnits.ItemId=items.id " +
                    $"and ItemUnits.barcode=N'{barcode}'", conn1);
                var dt = new DataTable();
                adp.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SearchByID(Convert.ToInt32(dt.Rows[0]["id"]),
                               Convert.ToInt32(dt.Rows[0]["unit"]));
                    txtBarcode.Text = "";
                    txtBarcode.Focus();
                    return;
                }

                // البحث في Itembarcodes
                var adp2 = new SqlDataAdapter(
                    $"select Itembarcodes.itemId from Items,Itembarcodes " +
                    $"where Items.IS_Deleted=0 and Itembarcodes.ItemId=items.id " +
                    $"and Itembarcodes.barcode=N'{barcode}'", conn1);
                var dt2 = new DataTable();
                adp2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    SearchByID(Convert.ToInt32(dt2.Rows[0]["itemId"]), 0);
                    txtBarcode.Text = "";
                    txtBarcode.Focus();
                    return;
                }

                // باركود الميزان (13 رقم)
                if (barcode.Length == 13)
                {
                    string prefix = barcode.Substring(0, 7);
                    var adp3 = new SqlDataAdapter(
                        $"Select Items.id,Items.group_id,ItemUnits.sale as SalePrice," +
                        $"ItemUnits.unit,Items.tax,Items.tax_group,Items.Wscale " +
                        $"from Items,ItemUnits where Items.IS_Deleted=0 " +
                        $"And ItemUnits.ItemId=items.id " +
                        $"And (ItemUnits.barcode=N'{prefix}' or Items.barcode=N'{prefix}')",
                        conn1);
                    var dt3 = new DataTable();
                    adp3.Fill(dt3);

                    if (dt3.Rows.Count > 0 &&
                        dt3.Rows[0]["Wscale"] != DBNull.Value)
                    {
                        double wScale = Convert.ToDouble(dt3.Rows[0]["Wscale"]);
                        int rowItemId = Convert.ToInt32(dt3.Rows[0]["id"]);
                        int unitId2 = Convert.ToInt32(dt3.Rows[0]["unit"]);

                        if (wScale == 1.0)
                        {
                            double qty = Convert.ToInt32(barcode.Substring(7, 2)) +
                                         Convert.ToInt32(barcode.Substring(9, 3)) / 1000.0;
                            IsBarcode = true;
                            SearchByID(rowItemId, unitId2);
                            if (_gridSource.Count > 0)
                            {
                                _gridSource[0].QuantDgv = qty.ToString();
                                CalcRowValues(0);
                                CalcTot();
                            }
                            IsBarcode = false;
                            txtBarcode.Text = "";
                            txtBarcode.Focus();
                        }
                        else if (wScale == 2.0)
                        {
                            double price = Convert.ToInt32(barcode.Substring(7, 3)) +
                                           Convert.ToInt32(barcode.Substring(10, 2)) / 100.0;
                            IsBarcode = true;
                            SearchByID(rowItemId, unitId2);
                            if (_gridSource.Count > 0)
                            {
                                _gridSource[0].priceDgv = price.ToString();
                                CalcRowValues(0);
                                CalcTot();
                            }
                            IsBarcode = false;
                            txtBarcode.Text = "";
                            txtBarcode.Focus();
                        }
                    }
                    else
                    {
                        DXMessageBox.Show("هذا الباركود غير موجود ضمن بيانات البرنامج");
                        txtBarcode.Text = "";
                        txtBarcode.Focus();
                    }
                    return;
                }

                DXMessageBox.Show("هذا الباركود غير موجود ضمن بيانات البرنامج");
            }
            catch { }
        }

        #endregion

        #region Search Panel & Context Menu Events

        private void cmbProcTypeSrch_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => Search(2);

        private void txtSrchNo_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return) Search(2);
        }

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int idx = dgvSrch.SelectedIndex;
            if (idx < 0) return;

            if (ISreturn)
            {
                if (dgvSrch.ItemsSource is List<PosSearchRow> rows && idx < rows.Count)
                {
                    double.TryParse(rows[idx].InvGlobalID, out double gid);
                    ProcCode = (int)Math.Round(gid);
                }
                dgvRowChng(idx, 2);
                ISreturn = true;
                linkedInvRet = true;
            }
            else
            {
                dgvRowChng(idx, 2);
            }

            SearchPanel.Visibility = Visibility.Collapsed;
        }

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RowIndex = GetFocusedRowIndex();
        }

        private void BtnAddNote_Row_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PosInvoiceRow row)
            {
                var frm = new frmCategoryNotes();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                int.TryParse(row.ItemIdDgv, out int itemId);

                string catAr = "", catEn = "", catCode = "", printer = "";
                int catId = 0;
                Common.GetItemCategory(ref catAr, ref catEn, ref catCode, ref catId,
                                       ref printer, itemId);
                frm.lblCategory.Text = catAr;
                frm.cond = $" where CatID=N'{catId}'";
                frm.ShowDialog();

                if (frm.ISDone)
                {
                    int idx = _gridSource.IndexOf(row);
                    if (idx >= 0)
                    {
                        string existing = _gridSource[idx].DescriptionDgv;
                        _gridSource[idx].DescriptionDgv = existing != " "
                            ? $"{existing} - {frm.Note}" : frm.Note;

                        if (frm.AddPrice > 0)
                        {
                            double.TryParse(_gridSource[idx].priceDgv, out double curP);
                            _gridSource[idx].priceDgv = (frm.AddPrice + curP).ToString();
                            CalcRowValues(idx);
                        }
                    }
                }
            }
        }

        private void BtnDeleteRow_Row_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PosInvoiceRow row)
            {
                int idx = _gridSource.IndexOf(row);
                if (idx >= 0 &&
                    DXMessageBox.Show("هل تريد حذف السجل", "", MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _gridSource.RemoveAt(idx);
                    CalcTot();
                }
            }
        }

        // Context Menu Events
        private void StripItemSearch_Click(object sender, RoutedEventArgs e) => addNewItem();
        private void StripItemDetail_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out int id);
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
        private void StripAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemCategory();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }
        private void StripItemUnits_Click(object sender, RoutedEventArgs e)
        {
            if (GetFocusedRowIndex() >= 0 && CheckEditable())
                AddUnit();
        }
        private void StripdeleteRow_Click(object sender, RoutedEventArgs e) => deleteRow();
        private void StripItemProcess_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out int id);
            var frm = new frmRptItemsActivity { ItemId = id };
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }
        private void StripClientLastItem_Click(object sender, RoutedEventArgs e) { }
        private void StripLastItem_Click(object sender, RoutedEventArgs e) { }
        private void StripItemAvrgCost_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(GetFocusedRowCellValue("ItemIdDgv")?.ToString(), out int id);
            DXMessageBox.Show(ItemOper.AvgCost(id, MainClass.BranchNo).ToString(),
                "تكلفة المادة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ToolStrip Menu Events
        private void btnCustomizeDisplay_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomizeShow();
            frm.cmbInv.SelectedIndex = 2;
            frm.cmbInv.IsEnabled = false;
            frm.ShowDialog();
            if (frm.ISDone) LoadDGvSetting();
        }
        private void stripImport_Click(object sender, RoutedEventArgs e) { }
        private void stripExport_Click(object sender, RoutedEventArgs e) { }
        private void btnInvProfit_Click(object sender, RoutedEventArgs e) { }
        private void stripDesignInvoice_Click(object sender, RoutedEventArgs e)
            => new frm_ReportDesign().ShowDialog();
        private void btnQuotationInvs_Click(object sender, RoutedEventArgs e)
            => InsertFromInv(4);
        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("تم حفظ مظهر الجدول", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BtnRestoreDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(StyleFile))
                File.Delete(StyleFile);
        }

        #endregion
    }

    // ═══════════════════════════════════════════════════════════════
    //                        Model Classes
    // ═══════════════════════════════════════════════════════════════

    /// <summary>صف الفاتورة في الجدول</summary>
    public class PosInvoiceRow : INotifyPropertyChanged
    {
        private string _no = ""; private string _itemCodeDgv = "";
        private string _itemIdDgv = ""; private string _itemNameDgv = "";
        private string _descriptionDgv = ""; private string _barcodeDgv1 = "";
        private string _unitDgv = ""; private string _quantDgv = "0";
        private string _percentUnitDgv = "1"; private string _totQuantDgv = "0";
        private string _priceDgv = "0"; private string _sumPriceDgv = "0";
        private string _avgCostDgv = "0"; private string _discountValDgv = "0";
        private string _discountPerDgv = "0"; private string _totPriceDgv = "0";
        private string _vatPerDgv = "0"; private string _vatValDgv = "0";
        private string _netValDgv = "0"; private string _stockDgv = "0";
        private string _storeDgv = "0";

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

    /// <summary>صف نتائج البحث</summary>
    public class PosSearchRow
    {
        public string InvGlobalID { get; set; }
        public string InvNo { get; set; }
        public string InvDate { get; set; }
        public string InvTime { get; set; }
        public string ClientName { get; set; }
    }
}