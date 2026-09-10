using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using Microsoft.Win32;
using SmartAuditERP.Form_WPF;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using CheckBox = System.Windows.Controls.CheckBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSalesInvoice : DevExpress.Xpf.Core.ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private int ProcCode;
        private string InvGlobalID;
        private string EntryGlobalID;
        private int RestSaleCode;

        public int RestrType;
        public int InvType;
        public int ProcType;

        private Print print;
        private InvoiceObj InvObj;

        private bool IsPrinted;
        private bool ISNew;
        private bool IsUpdated;
        private int RowIndex;
        private int SelectedId;

        public string SrchName;

        private DataTable dtSerialNo;
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
        private bool IsLoaded;

        private double PayNetwork;
        private double Paycash;
        private double PayType;
        private bool flag;

        // ObservableCollection للجدول
        private ObservableCollection<InvoiceItemRow_frmSalesInvoice> _itemsSource;

        #endregion

        #region ── Constructor ─────────────────────────────────────

        public frmSalesInvoice()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            ProcCode = -1;
            InvGlobalID = "";
            EntryGlobalID = "";
            RestSaleCode = -1;
            InvType = 2;

            print = new Print(InvType);
            InvObj = new InvoiceObj(InvType, ProcType);

            IsPrinted = false;
            ISNew = true;
            IsUpdated = false;
            RowIndex = -1;
            SelectedId = -1;
            SrchName = "";
            dtSerialNo = new DataTable();
            DGV_Count = 0;
            ISTRialEnd = false;
            ItemNotClicked = true;
            InvTax = 0.0;
            InvDiscount = 0.0;
            TotAfterdisc = 0.0;
            linkedInvRet = false;
            ISreturn = false;
            IsLoaded = false;
            PayNetwork = 0.0;
            Paycash = 0.0;
            PayType = 0.0;
            flag = false;

            _itemsSource = new ObservableCollection<InvoiceItemRow_frmSalesInvoice>();
            dgvItems.ItemsSource = _itemsSource;

            dtSerialNo.Columns.Add("ItemId");
            dtSerialNo.Columns.Add("SerialNo");
        }

        #endregion

        #region ── Window Events ───────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSettings();
                LoadData1();

                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;
                txtToDate.DateTime = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;

                cmbStore.SelectedValue = 1;
                cmbPayType.SelectedIndex = 1;
                txtInvTax.Text = InvObj.VAT.ToString();

                CheckForOffer();

                if (cmbTreasury.Items.Count > 0)
                    cmbTreasury.SelectedIndex = 0;

                txtBarcode.Focus();

                if (ProcType == 4)
                    btnInsertLinkedInv.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التحميل: " + ex.Message);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if ((_itemsSource.Count > 0 && ProcCode == -1) || IsUpdated)
            {
                var result = DXMessageBox.Show(
                    "لم يتم حفظ الفاتورة، هل تريد الخروج؟",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
            {
                e.Handled = true;
                txtBarcode.Focus();
            }

            if (e.Key == Key.F1)
            {
                RowIndex = _itemsSource.Count;
                AddNewItem();
            }

            if (e.Key == Key.Enter &&
                txtBarcode.IsFocused)
            {
                ReadBarcode();
            }
        }

        #endregion

        #region ── Load Settings & Data ────────────────────────────

        private void LoadSettings()
        {
            LoadDGvSetting();
        }

        private void LoadData1()
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
                    "SELECT * FROM CustomizedDGV WHERE Form_id=" + InvType +
                    " ORDER BY Column_id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                for (int i = 0; i < dt.Rows.Count && i < dgvItems.Columns.Count; i++)
                {
                    bool isVisible = Convert.ToBoolean(dt.Rows[i]["IS_Visible"]);
                    dgvItems.Columns[i].Visibility =
                        isVisible ? Visibility.Visible : Visibility.Collapsed;

                    string headerText = (string.Equals(
                        MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase))
                        ? dt.Rows[i]["col_name"].ToString()
                        : dt.Rows[i]["col_nameEn"].ToString();

                    dgvItems.Columns[i].Header = headerText;

                    if (int.TryParse(dt.Rows[i]["Width"].ToString(), out int w))
                        dgvItems.Columns[i].Width = new DataGridLength(w);

                    if (Convert.ToBoolean(dt.Rows[i]["IS_filled"]))
                        dgvItems.Columns[i].Width =
                            new DataGridLength(1, DataGridLengthUnitType.Star);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل إعدادات الجدول: " + ex.Message);
            }
        }

        private void LoadInvNo()
        {
            txtNo.Text = InvoiceOper
                .InvoiceNo(InvType, ProcType, InvObj.Prefixe)
                .ToString();
            txtNo.Background = new SolidColorBrush(
                Color.FromRgb(178, 34, 34));
            txtNo.Foreground = Brushes.White;
        }

        public void LoadSafes()
        {
            var dataSource = LoadData.Invertories(MainClass.EmpNo);
            cmbStore.ItemsSource = dataSource.DefaultView;
            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath = "id";
            cmbStore.SelectedIndex = -1;

            // المستودع في الجدول - البحث عن عمود ComboBox بالـ Header
            foreach (var col in dgvItems.Columns)
            {
                if (col is DataGridComboBoxColumn comboCol &&
                    comboCol.Header?.ToString() == "المستودع")
                {
                    comboCol.ItemsSource = dataSource.DefaultView;
                    comboCol.DisplayMemberPath = "name";
                    comboCol.SelectedValuePath = "id";
                    break;
                }
            }
        }

        private void LoadCustomers()
        {
            int custType = (InvType == 1) ? 2 : 1;

            if (InvType == 1)
            {
                string label = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? "المورد" : "Supplier";
                lblClient.Text = label;
                lblClientSrch.Text = label;
            }

            var adapter = new SqlDataAdapter(
                "SELECT id,name FROM Customers WHERE (type=" + custType +
                " OR type=3) ORDER BY id", conn);
            var dt = new DataTable();
            var dt2 = new DataTable();
            adapter.Fill(dt);
            adapter.Fill(dt2);

            cmbClient.ItemsSource = dt.DefaultView;
            cmbClient.DisplayMemberPath = "name";
            cmbClient.SelectedValuePath = "id";
            cmbClient.SelectedIndex = -1;

            cmbClientSrch.ItemsSource = dt2.DefaultView;
            cmbClientSrch.DisplayMemberPath = "name";
            cmbClientSrch.SelectedValuePath = "id";
            cmbClientSrch.SelectedIndex = -1;
        }

        public void LoadTreasury()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id,name FROM Stocks,Stock_Emps " +
                "WHERE Stocks.id=Stock_Emps.stock_id AND emp_id=" +
                MainClass.EmpNo +
                " AND IS_Deleted=0 AND status<>2 ORDER BY id", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbTreasury.ItemsSource = dt.DefaultView;
            cmbTreasury.DisplayMemberPath = "name";
            cmbTreasury.SelectedValuePath = "id";
            cmbTreasury.SelectedIndex = -1;
        }

        public void LoadSalesMen()
        {
            var adapter = new SqlDataAdapter(
                "SELECT id,name FROM salesmen WHERE IS_Deleted=0 ORDER BY id",
                conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            cmbSalesMen.ItemsSource = dt.DefaultView;
            cmbSalesMen.DisplayMemberPath = "name";
            cmbSalesMen.SelectedValuePath = "id";
            cmbSalesMen.SelectedIndex = -1;
        }

        private void LoadBanks()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Banks WHERE IS_Deleted=0", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBanks.ItemsSource = dt.DefaultView;
                cmbBanks.DisplayMemberPath = "name";
                cmbBanks.SelectedValuePath = "id";
                cmbBanks.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadCostCenters()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT code,name FROM cost_center WHERE type=2 ORDER BY code",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch { }
        }

        private void CheckForOffer()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Offer WHERE OfferStartDate<=@CurrentDate " +
                    "AND OfferExpire>=@CurrentDate AND offerType=1 " +
                    "AND ISDeleted=0 ORDER BY OfferId DESC", conn);
                adapter.SelectCommand.Parameters
                    .Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double invId = Convert.ToDouble(dt.Rows[0]["InvoiceID"]);
                    if (invId == InvType || invId == 0)
                    {
                        double offerVal = Convert.ToDouble(dt.Rows[0]["OfferValue"]);
                        double offerPerc = Convert.ToDouble(dt.Rows[0]["OfferPercentage"]);

                        if (offerVal > 0)
                            txtInvDiscVal.Text = offerVal.ToString();
                        else if (offerPerc > 0)
                            txtInvDiscPerc.Text = offerPerc.ToString();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region ── Navigation Buttons ──────────────────────────────

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type=" + InvType +
                         " AND proc_type=" + ProcType +
                         " AND IS_Deleted=0 AND branch=" +
                         MainClass.BranchNo + " ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type=" + InvType +
                         " AND proc_type=" + ProcType +
                         " AND IS_Deleted=0 AND id<" +
                         txtNo.Text.Trim() +
                         " AND branch=" + MainClass.BranchNo +
                         " ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type=" + InvType +
                         " AND proc_type=" + ProcType +
                         " AND IS_Deleted=0 AND id>" +
                         txtNo.Text.Trim() +
                         " AND branch=" + MainClass.BranchNo +
                         " ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type=" + InvType +
                         " AND proc_type=" + ProcType +
                         " AND IS_Deleted=0 AND branch=" +
                         MainClass.BranchNo + " ORDER BY id DESC");
        }

        #endregion

        #region ── Navigate & ReadData ─────────────────────────────

        public void Navigate(string sqlStr)
        {
            dgvSrch?.UnselectAll();
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using var cmd = new SqlCommand(sqlStr, conn);
                using var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows)
                {
                    CLR();
                    return;
                }

                dr.Read();
                CLR();

                ISNew = false;
                txtNo.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                txtNo.Foreground = Brushes.Black;
                IsPrinted = true;

                ProcCode = Convert.ToInt32(dr["proc_id"]);
                InvGlobalID = dr["InvGlobalID"].ToString();
                RestSaleCode = Convert.ToInt32(dr["EntryID"].ToString() == "" ? "0" : dr["EntryID"].ToString());
                EntryGlobalID = dr["branch"].ToString() + "-" + RestSaleCode;
                Code = Convert.ToInt32(dr["id"]);

                txtNo.Text = Code.ToString();
                txtDate.DateTime = Convert.ToDateTime(dr["date"]);

                InvType = Convert.ToInt32(dr["inv_type"]);
                ProcType = Convert.ToInt32(dr["proc_type"]);

                cmbStore.SelectedValue = dr["safe"].ToString();
                cmbTreasury.SelectedValue = dr["stock"].ToString();

                txtSumVal.Text = dr["InvTotal"].ToString();
                txtNet.Text = dr["tot_net"].ToString();
                txtInvDiscVal.Text = dr["minus"].ToString();
                txtInvTax.Text = dr["tax"].ToString();
                txtNote.Text = dr["notes"].ToString();

                try
                {
                    txtRefDate.DateTime = Convert.ToDateTime(dr["Reff_date"]);
                    txtRefNo.Text = dr["Reff_No"].ToString();
                }
                catch { }

                try
                {
                    double payTypeVal = Convert.ToDouble(dr["pay_type"]);
                    if (payTypeVal == -1) cmbPayType.SelectedIndex = 0;
                    else if (payTypeVal == 1) cmbPayType.SelectedIndex = 1;
                    else if (payTypeVal == 2)
                    {
                        cmbPayType.SelectedIndex = 2;
                        cmbBanks.SelectedValue = dr["bank"];
                    }
                }
                catch { }

                cmbClient.SelectedValue = dr["cust_id"].ToString();
                try { cmbSalesMen.SelectedValue = dr["salesman"].ToString(); } catch { }

                dr.Close();
                IsLoaded = true;

                // Load items
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM Inv_Sub WHERE productId=0 AND InvGlobalID=N'" +
                    InvGlobalID + "'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                _itemsSource.Clear();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    try
                    {
                        int itemId = Convert.ToInt32(dt.Rows[i]["ItemId"]);
                        int storeId = Convert.ToInt32(dt.Rows[i]["store"]);
                        double currentStock = CalcStock(itemId, storeId);

                        decimal perc = 1m;
                        var percAdapter = new SqlDataAdapter(
                            "SELECT ItemUnits.perc FROM Items,ItemUnits " +
                            "WHERE Items.id=ItemUnits.ItemId " +
                            "AND ItemUnits.unit=" + dt.Rows[i]["unit"] +
                            " AND ItemUnits.ItemId=" + itemId, conn);
                        var percDt = new DataTable();
                        percAdapter.Fill(percDt);
                        if (percDt.Rows.Count > 0)
                            perc = Convert.ToDecimal(percDt.Rows[0][0]);

                        double qty = Convert.ToDouble(dt.Rows[i]["val1"]);
                        double price = Convert.ToDouble(dt.Rows[i]["exchange_price"]);
                        double total = Math.Round(qty * price, 3);
                        double disc = Convert.ToDouble(dt.Rows[i]["discount"]);
                        double discPerc = total != 0 ? disc / total * 100.0 : 0;
                        double netAfter = Math.Round(total - disc, 2);
                        double taxVal = Convert.ToDouble(dt.Rows[i]["taxval"]);
                        if (InvObj.PriceIncVAT) netAfter -= taxVal;
                        double netWithVat = netAfter + taxVal;

                        double avgCost = (dt.Rows[i]["AvrgCost"] == DBNull.Value)
                            ? 0
                            : Convert.ToDouble(dt.Rows[i]["AvrgCost"]);

                        var row = new InvoiceItemRow_frmSalesInvoice
                        {
                            RowNum = i + 1,
                            ItemCode = Common.GetItemCode(itemId),
                            ItemId = itemId,
                            ItemName = Common.GetItemName(itemId),
                            Description = dt.Rows[i]["Description"].ToString(),
                            Barcode = "",
                            UnitName = Common.GetUnitName(
                                               Convert.ToInt32(dt.Rows[i]["unit"])),
                            Quantity = qty,
                            UnitEquality = Convert.ToDouble(perc),
                            PrimaryQnty = Convert.ToDouble(dt.Rows[i]["val"]),
                            Price = price,
                            Total = total,
                            AvgCost = avgCost,
                            ItemDiscount = disc,
                            DiscPerc = Math.Round(discPerc, 2),
                            NetAfterDisc = netAfter,
                            VatPerc = Convert.ToDouble(dt.Rows[i]["taxperc"]),
                            VatVal = taxVal,
                            NetWithVat = netWithVat,
                            CurrentStock = currentStock,
                            StoreId = storeId
                        };

                        _itemsSource.Add(row);
                    }
                    catch { }
                }

                IsLoaded = false;

                // Load cost center
                int accCode = (ProcType == 2) ? 4100002 : 4100001;
                var ccAdapter = new SqlDataAdapter(
                    "SELECT CCcode FROM Entry_sub WHERE branch=" +
                    MainClass.BranchNo +
                    " AND res_id=" + RestSaleCode +
                    " AND acc_no=" + accCode, conn);
                var ccDt = new DataTable();
                ccAdapter.Fill(ccDt);
                if (ccDt.Rows.Count > 0 && ccDt.Rows[0]["CCcode"] != DBNull.Value)
                    cmbCostCenter.SelectedValue =
                        Convert.ToInt32(ccDt.Rows[0]["CCcode"]);

                CalcTot();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في قراءة البيانات: " + ex.Message);
            }
        }

        #endregion

        #region ── CLR (Clear Form) ────────────────────────────────

        private void CLR()
        {
            try
            {
                _itemsSource.Clear();

                Code = -1;
                RestSaleCode = -1;
                ProcCode = -1;
                InvGlobalID = "";
                EntryGlobalID = "";
                SelectedId = -1;

                if (cmbStore.Items.Count > 0) cmbStore.SelectedIndex = 0;
                if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;
                if (cmbTreasury.Items.Count > 0) cmbTreasury.SelectedIndex = 0;

                txtSumVal.Text = "0";
                txtNet.Text = "0";
                txtInvProfit.Text = "0";
                txtSumCostAvrg.Text = "0";
                txtTotDiscount.Text = "0";
                txtNetWithoutVAT.Text = "0";
                txtTotVAT.Text = "0";
                txtInvDiscVal.Text = "0";
                txtInvDiscPerc.Text = "0";
                txtInvTax.Text = InvObj.VAT.ToString();
                txtNote.Text = "";
                txtSrchDgv.Text = "";
                txtBarcode.Text = "";
                txtRefNo.Text = "";
                txtBalance.Text = "";
                txtTirmsNo.Text = "";
                txtTotQuan.Text = "";
                txtStock.Text = "";
                txtPerUnit.Text = "";
                txtRecentPurchPrice.Text = "";
                txtLastSalePrice.Text = "";
                txtTotUnitQuan.Text = "";
                txtCostAvrg.Text = "";
                txtCompetitorPrice.Text = "";
                txtItemBarcode.Text = "";

                IsPrinted = false;
                ISNew = true;
                IsUpdated = false;
                IsLoaded = false;

                PayNetwork = 0.0;
                Paycash = 0.0;
                PayType = -1.0;

                InvTax = 0.0;
                InvDiscount = 0.0;
                TotAfterdisc = 0.0;

                PictureBox1.Source = null;
                dtSerialNo.Rows.Clear();

                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;

                cmbPayType.SelectedIndex = 1;
                cmbCostCenter.SelectedIndex = -1;
                cmbTreasury.IsEnabled = true;

                txtNo.Background = new SolidColorBrush(Color.FromRgb(178, 34, 34));
                txtNo.Foreground = Brushes.White;

                LoadInvNo();
                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء المسح: " + ex.Message);
            }
        }

        private bool CheckBeforeClear()
        {
            if ((_itemsSource.Count > 0 && ProcCode == -1) || IsUpdated)
            {
                var result = DXMessageBox.Show(
                    string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "لم يتم حفظ الفاتورة، هل تريد جديد؟"
                        : "You did not save the invoice. New?",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                return result == MessageBoxResult.No;
            }
            return false;
        }

        #endregion

        #region ── New / Save / Delete / Print ─────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ProcCode > -1 && ProcType == 2 &&
                InvoiceOper.IsPreviousReturned(InvGlobalID, InvGlobalID))
            {
                DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var row in _itemsSource)
            {
                if (row.Quantity <= 0)
                {
                    DXMessageBox.Show(
                        "لا يمكن حفظ الفاتورة، يوجد كميات أقل أو تساوي الصفر",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من الحفظ؟", "",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            if (MainClass.IsTrial && ProcCode == -1)
            {
                var trialAdapter = new SqlDataAdapter(
                    "SELECT id FROM Entry", conn1);
                var trialDt = new DataTable();
                trialAdapter.Fill(trialDt);

                if (trialDt.Rows.Count >= 20)
                {
                    DXMessageBox.Show(
                        "نأسف، لقد وصلت لأقصى حد إدخال للنسخة التجريبية",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsPrinted = false;
                    return;
                }
            }

            if (conn1.State != ConnectionState.Open)
                conn1.Open();

            IsPrinted = false;

            if (!double.TryParse(txtNet.Text, out double netVal) || netVal <= 0)
            {
                DXMessageBox.Show("يجب أن يكون الصافي أكبر من الصفر",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbPayType.SelectedIndex == 0 &&
                cmbClient.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار العميل",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveAndPrint();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            IsPrinted = true;
            if (InvObj.ShowPayForm && ProcType == 1 &&
                cmbPayType.SelectedIndex > 0)
                PayBill();
            else
                SaveAndPrint();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر فاتورة ليتم حذفها",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                var confirm = DXMessageBox.Show(
                    "هل أنت متأكد من حذف الفاتورة؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                new SqlCommand(
                    "UPDATE Inv SET IS_Deleted=1 WHERE InvGlobalID=N'" +
                    InvGlobalID + "'", conn).ExecuteNonQuery();

                if (RestSaleCode != 0)
                    new SqlCommand(
                        "UPDATE Entry SET IS_Deleted=1,state=2 WHERE GlobalID=N'" +
                        EntryGlobalID + "'", conn).ExecuteNonQuery();

                DXMessageBox.Show(
                    string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "تم الحذف" : "Deleted",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    "خطأ أثناء الحذف\nتفاصيل: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (IsPrinted)
            {
                var inv = BindToInvoice();
                RptPrint(inv, 1);
            }
            else
            {
                DXMessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            var inv = BindToInvoice();
            RptPrint(inv, 2);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Save & Print Logic ──────────────────────────────

        private async void SaveAndPrint()
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var inv = BindToInvoice();
                var entry = ((ProcType == 1 || ProcType == 2))
                    ? invoiceOper.BindToEntry(inv)
                    : null;

                bool saved = invoiceOper.SaveInvoice(inv, entry, ISNew);

                if (saved && IsPrinted && print.PrintNo > 0)
                    RptPrint(inv, 1);

                if (saved && Sync.ActiveSync &&
                    InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!InvObj.SyncEntry) entry = null;
                    await invoiceOper.SyncInvoice(inv, entry, ISNew);
                }

                Inventory.UpdateItemStock(inv);

                if (!saved) return;

                var savedMsg = new frmSavedMsg();
                if (!ISNew)
                    savedMsg.lblSave.Text =
                        string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? "تم حفظ التعديلات بنجاح..."
                            : "Successfully updated...";

                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    CLR();
                else if (savedMsg.Pressed == 2)
                {
                    ISNew = false;
                    IsUpdated = false;
                    txtNo.Background = new SolidColorBrush(
                        Color.FromRgb(245, 245, 245));
                    txtNo.Foreground = Brushes.Black;
                }
                else if (savedMsg.Pressed == 3)
                {
                    CLR();
                    Close();
                }
                else
                    CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ: " + ex.Message);
            }
        }

        private void GetInvoiceIDs()
        {
            if (!double.TryParse(txtNetWithoutVAT.Text, out _))
                txtNetWithoutVAT.Text = "0";

            if (conn.State != ConnectionState.Open)
                conn.Open();

            if (ProcCode == -1)
            {
                ProcCode = Convert.ToInt32(
                    new SqlCommand(
                        "SELECT ISNULL(MAX(proc_id),0) FROM Inv",
                        conn).ExecuteScalar()) + 1;

                LoadInvNo();

                if (int.TryParse(txtNo.Text, out int invoiceNo))
                    Code = invoiceNo;

                if (Convert.ToDouble(txtNetWithoutVAT.Text) != 0)
                    RestSaleCode = Convert.ToInt32(
                        new SqlCommand(
                            "SELECT ISNULL(MAX(id),0) FROM Entry WHERE branch=" +
                            MainClass.BranchNo, conn).ExecuteScalar()) + 1;
            }
        }

        private Invoice BindToInvoice()
        {
            double paycash = 0;
            double payATM = 0;
            int bank = -1;
            int payTypeI = -1;

            if (cmbPayType.SelectedIndex == 1)
            {
                payTypeI = 1;
                paycash = double.TryParse(txtNet.Text, out double n) ? n : 0;
            }
            else if (cmbPayType.SelectedIndex == 2)
            {
                payTypeI = 2;
                bank = cmbBanks.SelectedValue != null
                    ? Convert.ToInt32(cmbBanks.SelectedValue) : -1;
                payATM = double.TryParse(txtNet.Text, out double n2) ? n2 : 0;
            }

            if (ProcCode == -1)
            {
                GetInvoiceIDs();
                EntryOper.GetEntryGlobalID(ref EntryGlobalID, ref RestSaleCode);
                InvoiceOper.GetInvoiceGlobalID(ref InvGlobalID, ref ProcCode);
            }

            var invoice = new Invoice
            {
                AutoIncrementID = ProcCode,
                Total = double.TryParse(txtNetWithoutVAT.Text, out double tot) ? tot : 0,
                SumPrice = double.TryParse(txtSumVal.Text, out double sump) ? sump : 0,
                VAT = Math.Round(double.TryParse(txtTotVAT.Text, out double vat) ? vat : 0, 2),
                Net = Math.Round(double.TryParse(txtNet.Text, out double net) ? net : 0, 2),
                Discount = Math.Round(double.TryParse(txtInvDiscVal.Text, out double disc) ? disc : 0, 2),
                TotDiscount = Math.Round(double.TryParse(txtTotDiscount.Text, out double td) ? td : 0, 2),
                Additions = 0,
                Insurance = 0,
                Delivery = 0,
                Paycash = paycash,
                PayATM = payATM,
                PayType = payTypeI,
                Remainder = 0,
                InvProfit = double.TryParse(txtInvProfit.Text, out double p) ? p : 0,
                InvGlobalID = InvGlobalID,
                EntryGlobalID = EntryGlobalID,
                InvoiceNo = int.TryParse(txtNo.Text, out int ino) ? ino : 0,
                InvoiceType = (InvoiceType)InvType,
                Bank = bank,
                ProcType = ProcType,
                OrderNo = -1,
                OrderType = -1,
                InvDate = txtDate.DateTime,
                User = MainClass.EmpNo,
                Branch = MainClass.BranchNo,
                DistBranch = Sync.DistBranch,
                ClientCode = Sync.ClientCode,
                BranchType = Sync.BranchType,
                Currency = InvObj.Currency,
                Treasury = (cmbTreasury.SelectedIndex > -1)
                    ? Convert.ToInt32(cmbTreasury.SelectedValue)
                    : MainClass.UserTreasury,
                InvCCcode = (cmbCostCenter.SelectedIndex > -1)
                    ? cmbCostCenter.SelectedValue.ToString()
                    : "-1",
                Saleman = (cmbSalesMen.SelectedIndex > -1)
                    ? Convert.ToInt32(cmbSalesMen.SelectedValue)
                    : -1,
                Customer = (cmbClient.SelectedIndex > -1)
                    ? Convert.ToInt32(cmbClient.SelectedValue)
                    : 1,
                IsDeleted = false,
                VATperc = InvObj.VAT,
                ReffNo = (!string.IsNullOrWhiteSpace(txtRefNo.Text))
                    ? txtRefNo.Text : "-1",
                RefDate = txtRefDate.DateTime,
                InvAccCode = (ProcType == 1)
                    ? InvObj.InvAcc : InvObj.InvReturnAcc,
                Store = (cmbStore.SelectedIndex > -1)
                    ? Convert.ToInt32(cmbStore.SelectedValue) : 1
            };

            invoice.InvNote = (!string.IsNullOrWhiteSpace(txtNote.Text))
                ? txtNote.Text
                : InvoiceOper.GetInvoiceType(
                    (int)invoice.InvoiceType,
                    invoice.ProcType,
                    invoice.PayType, 1) +
                  " برقم " + invoice.InvoiceNo;

            var items = new List<Item>();

            foreach (var row in _itemsSource)
            {
                var item = new Item
                {
                    InvGlobalID = InvGlobalID,
                    ClientCode = Sync.ClientCode,
                    AutoIncrementID = ProcCode,
                    Name = row.ItemName,
                    ItemNo = row.ItemId,
                    Code = row.ItemCode,
                    UnitName = row.UnitName,
                    StoreName = row.StoreId.ToString(),
                    ProcType = (ProcType == 1) ? 2 :
                                     (ProcType == 2) ? 1 : 0,
                    Description = row.Description ?? "",
                    Quantity = row.Quantity,
                    UnitEquality = row.UnitEquality,
                    PrimaryQnty = row.PrimaryQnty,
                    Unit = Common.GetUnitID(row.UnitName),
                    Price = row.Price,
                    AvegCost = row.AvgCost,
                    Vat = row.VatVal,
                    VatPerc = row.VatPerc,
                    Barcode = row.Barcode,
                    ItemDiscount = row.ItemDiscount,
                    ValiableStock = row.CurrentStock - row.PrimaryQnty,
                    Store = row.StoreId,
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = "",
                    ProductId = 0
                };

                // Check components
                var compAdapter = new SqlDataAdapter(
                    "SELECT ItemComponents.Id,ItemComponents.ComponentId," +
                    "ItemComponents.store,ItemComponents.itemId," +
                    "ItemComponents.quantity,ItemComponents.price," +
                    "ItemComponents.total,ItemComponents.type," +
                    "ItemComponents.unit AS unit_id," +
                    "items.id AS item_ID,items.Code AS item_Code," +
                    "items.name AS Item_name," +
                    "ItemUnits.perc AS UnitEquality " +
                    "FROM ItemComponents " +
                    "LEFT JOIN items ON ItemComponents.ComponentId=items.id " +
                    "LEFT JOIN ItemUnits ON ItemUnits.ItemId=ItemComponents.ComponentId " +
                    "WHERE ItemUnits.unit=ItemComponents.unit " +
                    "AND ItemComponents.itemId=" + item.ItemNo, conn1);
                var compDt = new DataTable();
                compAdapter.Fill(compDt);

                if (compDt.Rows.Count > 0)
                {
                    decimal totalAvg = 0m;
                    foreach (DataRow compRow in compDt.Rows)
                    {
                        var subItem = new Item
                        {
                            InvGlobalID = InvGlobalID,
                            ClientCode = Sync.ClientCode,
                            AutoIncrementID = ProcCode,
                            Name = compRow["Item_name"].ToString(),
                            ItemNo = Convert.ToInt32(compRow["ComponentId"]),
                            Code = compRow["item_Code"].ToString(),
                            UnitName = compRow["unit_id"].ToString(),
                            StoreName = item.StoreName,
                            ProcType = item.ProcType,
                            Description = "",
                            Quantity = Convert.ToDouble(compRow["quantity"]) * item.PrimaryQnty,
                            UnitEquality = Convert.ToDouble(compRow["UnitEquality"]),
                            PrimaryQnty = Convert.ToDouble(compRow["quantity"]) *
                                             item.PrimaryQnty *
                                             Convert.ToDouble(compRow["UnitEquality"]),
                            Unit = Convert.ToInt32(compRow["unit_id"]),
                            Price = Convert.ToDouble(compRow["price"]),
                            AvegCost = ItemOper.AvgCost(
                                                Convert.ToInt32(compRow["ComponentId"]),
                                                MainClass.BranchNo),
                            Vat = 0,
                            VatPerc = 0,
                            Barcode = "",
                            ItemDiscount = 0,
                            ValiableStock = Inventory.CalcItemStock(
                                                (int)Math.Round(item.Store),
                                                Convert.ToInt32(compRow["ComponentId"]),
                                                MainClass.BranchNo),
                            Store = item.Store,
                            ExpireDate = DateTime.Now.AddYears(1),
                            Note = "",
                            ProductId = item.ItemNo
                        };
                        items.Add(subItem);
                        totalAvg += new decimal(subItem.AvegCost * subItem.PrimaryQnty);
                    }
                    item.AvegCost = Convert.ToDouble(totalAvg / new decimal(item.PrimaryQnty));
                }

                items.Add(item);

                // Serial numbers
                try
                {
                    if (dtSerialNo.Rows.Count > 0)
                    {
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        new SqlCommand(
                            "DELETE FROM ItemSerialNo WHERE ItemId=" +
                            row.ItemId + " AND InvGlobalID=N'" +
                            InvGlobalID + "'", conn).ExecuteNonQuery();

                        foreach (DataRow sRow in dtSerialNo.Rows)
                        {
                            if (sRow["ItemId"].ToString() ==
                                row.ItemId.ToString())
                                SaveSerialNo(
                                    Convert.ToInt32(sRow["ItemId"]),
                                    sRow["SerialNo"].ToString(),
                                    InvGlobalID);
                        }
                    }
                }
                catch { }
            }

            invoice.Items = items;
            return invoice;
        }

        private void RptPrint(Invoice inv, int type)
        {
            if (_itemsSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = (ProcType == 4)
                    ? "rptPricingInv.repx" : "rptInvSales.repx";

            string fullPath = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(type,
                print.BindToData(inv),
                print.RptUrl,
                print.RptName,
                print.defPrinter,
                print.kitchenprinter,
                print.PrintNo);
        }

        #endregion

        #region ── Barcode & Item Search ───────────────────────────

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ReadBarcode();
        }

        private void ReadBarcode()
        {
            try
            {
                string barcode = txtBarcode.Text.Trim();
                if (string.IsNullOrEmpty(barcode))
                {
                    AddNewItem();
                    return;
                }

                // Search in ItemUnits
                var adapter = new SqlDataAdapter(
                    "SELECT Items.id,ItemUnits.unit FROM Items,ItemUnits " +
                    "WHERE Items.IS_Deleted=0 " +
                    "AND ItemUnits.ItemId=items.id " +
                    "AND ItemUnits.barcode=N'" + barcode + "'", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtBarcode.Text = "";
                    RowIndex = _itemsSource.Count;
                    SearchByID(Convert.ToInt32(dt.Rows[0]["unit"]));
                    return;
                }

                // Search in Itembarcodes
                var adapter2 = new SqlDataAdapter(
                    "SELECT items.unit,Itembarcodes.itemId " +
                    "FROM Items,Itembarcodes " +
                    "WHERE Items.IS_Deleted=0 " +
                    "AND Itembarcodes.ItemId=items.id " +
                    "AND (Itembarcodes.barcode=N'" + barcode +
                    "' OR Items.barcode=N'" + barcode + "')", conn1);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt2.Rows[0]["itemId"]);
                    RowIndex = _itemsSource.Count;
                    SearchByID(Convert.ToInt32(dt2.Rows[0]["unit"]));
                    txtBarcode.Text = "";
                }
                else
                {
                    DXMessageBox.Show("هذا الباركود غير موجود ضمن بيانات البرنامج",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في قراءة الباركود: " + ex.Message);
            }
        }

        private void SearchByName()
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;

                SrchName = _itemsSource[RowIndex].ItemName;

                var adapter = new SqlDataAdapter(
                    "SELECT name,id FROM Items WHERE IS_Deleted=0 AND name=N'" +
                    SrchName + "'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                    AddNewItem();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث بالاسم: " + ex.Message);
            }
        }

        private void SearchByCode()
        {
            try
            {
                if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;

                string code = _itemsSource[RowIndex].ItemCode;
                SrchName = "";

                var adapter = new SqlDataAdapter(
                    "SELECT name,id FROM Items WHERE IS_Deleted=0 AND Code=N'" +
                    code + "'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SelectedId = Convert.ToInt32(dt.Rows[0]["id"]);
                    SearchByID(0);
                }
                else
                    AddNewItem();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث بالكود: " + ex.Message);
            }
        }

        private void SearchByID(int unitId)
        {
            try
            {
                if (SelectedId <= 0) return;

                var adapter = new SqlDataAdapter(
                    "SELECT name,id,Code,nameEn FROM Items " +
                    "WHERE IS_Deleted=0 AND id=" + SelectedId, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    AddNewItem();
                    return;
                }

                string itemName = string.Equals(
                    MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? dt.Rows[0]["name"].ToString()
                    : (string.IsNullOrEmpty(dt.Rows[0]["nameEn"].ToString())
                        ? dt.Rows[0]["name"].ToString()
                        : dt.Rows[0]["nameEn"].ToString());

                int itemId = Convert.ToInt32(dt.Rows[0]["id"]);

                // Check if item already exists in grid
                bool exists = false;
                int existIndex = -1;
                for (int i = 0; i < _itemsSource.Count; i++)
                {
                    if (_itemsSource[i].ItemId == itemId)
                    {
                        exists = true;
                        existIndex = i;
                        break;
                    }
                }

                if (exists)
                {
                    var msgExist = new MsgExistItem();
                    msgExist.ShowDialog();

                    if (msgExist.Action == 1)
                    {
                        _itemsSource[existIndex].Quantity += 1;
                        CalcRowValues(existIndex);
                        if (!InvObj.SaleByMinus)
                            IsValidQuantity(existIndex);
                        CalcTot();
                        return;
                    }
                    if (msgExist.Action != 2) return;
                }

                // Add new row
                if (RowIndex < _itemsSource.Count)
                    _itemsSource.RemoveAt(RowIndex);

                var newRow = new InvoiceItemRow_frmSalesInvoice
                {
                    RowNum = _itemsSource.Count + 1,
                    ItemId = itemId,
                    ItemCode = dt.Rows[0]["Code"].ToString(),
                    ItemName = itemName,
                    Quantity = 1,
                    Price = 0,
                    StoreId = (cmbStore.SelectedValue != null)
                        ? Convert.ToInt32(cmbStore.SelectedValue) : 0
                };

                _itemsSource.Add(newRow);
                RowIndex = _itemsSource.Count - 1;

                LoadItemInf(itemId, RowIndex, unitId);
                CheckForItemOffer(itemId);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void AddNewItem()
        {
            try
            {
                var frm = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);

                frm.sql = "SELECT id,name,nameEN,sale_price,unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
                frm.search = "SELECT id,name,nameEN,sale_price,unit FROM Items";
                frm.Itemname = "";
                frm.StoreId = (cmbStore.SelectedValue != null)
                    ? Convert.ToInt32(cmbStore.SelectedValue) : 0;
                frm.txtSrchNm.Text = SrchName;
                frm.ShowDialog();

                if (frm.ISDone && frm.ItemId > 0)
                {
                    foreach (int id in frm.Itemlist)
                    {
                        SelectedId = id;
                        SearchByID(0);
                        RowIndex = _itemsSource.Count - 1;
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في إضافة صنف: " + ex.Message);
            }
        }

        #endregion

        #region ── Load Item Information ───────────────────────────

        private void LoadItemInf(int itemId, int index, int unitId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Items.id,Items.name,Items.barcode,units.name AS unit," +
                    "purch_price,sale_price,tax,discount,ItemProperty,FillValue " +
                    "FROM Items,units WHERE Items.unit=units.id AND items.id=" +
                    itemId, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                LoadUnitInf(index, unitId);

                var row = _itemsSource[index];
                row.AvgCost = ItemOper.Cost(itemId);
                row.CurrentStock = CalcStock(itemId,
                    (cmbStore.SelectedValue != null)
                        ? Convert.ToInt32(cmbStore.SelectedValue) : 0);
                row.Quantity = 1;
                row.StoreId = (cmbStore.SelectedValue != null)
                    ? Convert.ToInt32(cmbStore.SelectedValue) : 0;
                row.VatPerc = Convert.ToDouble(dt.Rows[0]["tax"]);
                row.ItemDiscount = Convert.ToDouble(dt.Rows[0]["discount"]);

                if (rdAuto.IsChecked == true)
                {
                    var calc = new Frm_Calculator();
                    calc.TextBox1.Text = "1";
                    calc.ShowDialog();
                    row.Quantity = Convert.ToDouble(Properties.Settings.Default.QTY);
                }

                CalcRowValues(index);
                CalcTot();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل بيانات الصنف: " + ex.Message);
            }
        }

        private void LoadUnitInf(int index, int unitId)
        {
            try
            {
                var row = _itemsSource[index];
                int itemId = row.ItemId;

                if (unitId == 0 && string.IsNullOrEmpty(row.UnitName))
                    unitId = CheckItemUnit(itemId);

                var adapter = new SqlDataAdapter(
                    "SELECT purch,sale,barcode,perc,units.name " +
                    "FROM units,ItemUnits " +
                    "WHERE ItemUnits.ItemId=" + itemId +
                    " AND ItemUnits.unit=units.id AND units.id=" + unitId,
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    row.UnitName = dt.Rows[0]["name"].ToString();
                    row.Barcode = dt.Rows[0]["barcode"].ToString();
                    row.UnitEquality = Convert.ToDouble(dt.Rows[0]["perc"]);
                    row.Quantity = 1;
                    row.PrimaryQnty = row.Quantity * row.UnitEquality;

                    row.Price = InvObj.Pricing switch
                    {
                        Pricing.SalePrice => Convert.ToDouble(dt.Rows[0]["purch"]),
                        Pricing.PurchasePrice => ItemOper.RecentPurchPrice(itemId) * Convert.ToDouble(dt.Rows[0]["perc"]),
                        Pricing.LastPurchasePrice => ItemOper.Cost(itemId) * Convert.ToDouble(dt.Rows[0]["perc"]),
                        _ => Convert.ToDouble(dt.Rows[0]["sale"])
                    };
                    return;
                }

                // Fallback to item default unit
                var adapter2 = new SqlDataAdapter(
                    "SELECT units.name AS unit,purch_price AS purch," +
                    "sale_price AS sale,Items.barcode AS barcode " +
                    "FROM Items,units WHERE items.unit=units.id AND items.id=" +
                    itemId, conn);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    row.UnitName = dt2.Rows[0]["unit"].ToString();
                    row.Barcode = dt2.Rows[0]["barcode"].ToString();
                    row.UnitEquality = 1;
                    row.Quantity = 1;
                    row.PrimaryQnty = 1;

                    row.Price = InvObj.Pricing switch
                    {
                        Pricing.SalePrice => Convert.ToDouble(dt2.Rows[0]["purch"]),
                        Pricing.PurchasePrice => ItemOper.RecentPurchPrice(itemId),
                        Pricing.LastPurchasePrice => ItemOper.Cost(itemId),
                        _ => Convert.ToDouble(dt2.Rows[0]["sale"])
                    };
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل وحدة الصنف: " + ex.Message);
            }
        }

        private int CheckItemUnit(int itemId)
        {
            var adapter = new SqlDataAdapter(
                "SELECT id,name FROM units WHERE defaultInv=" + InvType, conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count == 0) return 0;

            var adapter2 = new SqlDataAdapter(
                "SELECT purch,sale FROM ItemUnits WHERE ItemId=" + itemId +
                " AND unit=" + dt.Rows[0]["id"], conn);
            var dt2 = new DataTable();
            adapter2.Fill(dt2);

            return dt2.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["id"]) : 0;
        }

        private int CalcStock(int itemId, int storeId)
        {
            try
            {
                return (int)Math.Round(
                    Inventory.CalcItemStock(storeId, itemId, MainClass.BranchNo));
            }
            catch { return 0; }
        }

        private void LoadItemDetails(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _itemsSource.Count) return;
                var row = _itemsSource[rowIndex];
                SelectedId = row.ItemId;

                txtLastSalePrice.Text = "";
                txtCompetitorPrice.Text = "";
                txtCostAvrg.Text = "";
                txtStock.Text = "";
                txtItemBarcode.Text = "";

                txtRecentPurchPrice.Text =
                    ItemOper.RecentPurchPrice(SelectedId).ToString();
                txtCostAvrg.Text =
                    (row.AvgCost * row.UnitEquality).ToString();

                LoadPrices(SelectedId);

                txtStock.Text = row.CurrentStock.ToString();
                txtStock.Foreground = (row.CurrentStock > -1)
                    ? Brushes.Green : Brushes.Firebrick;

                txtPerUnit.Text = row.UnitEquality.ToString();
                txtTotUnitQuan.Text = row.PrimaryQnty.ToString();
                txtItemBarcode.Text = row.Barcode;
            }
            catch { }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT low_sale_price,CompetitorPrice FROM ItemPrices " +
                    "WHERE Itemid=" + itemId + " ORDER BY Proc_id DESC", conn);
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

        #endregion

        #region ── Calculation Methods ─────────────────────────────

        private void CalcRowValues(int index)
        {
            if (index < 0 || index >= _itemsSource.Count) return;
            var row = _itemsSource[index];
            if (row.ItemId <= 0) return;

            if (!IsLoaded) IsUpdated = true;

            row.PrimaryQnty = row.Quantity * row.UnitEquality;
            row.Total = Math.Round(row.Quantity * row.Price, 2);

            double netAfterDisc = Math.Round(row.Total - row.ItemDiscount, 2);
            double vatVal = 0;

            if (netAfterDisc > 0)
                vatVal = CalcVAT(row.ItemId, new decimal(netAfterDisc));

            if (InvObj.PriceIncVAT)
                netAfterDisc -= vatVal;

            row.VatVal = Math.Round(vatVal, 2);
            row.NetAfterDisc = Math.Round(netAfterDisc, 2);
            row.NetWithVat = Math.Round(netAfterDisc + vatVal, 2);
            row.DiscPerc = (row.Total != 0)
                ? Math.Round(row.ItemDiscount / row.Total * 100, 2) : 0;
        }

        public void CalcTot()
        {
            try
            {
                double sumVal = 0, totDiscount = 0, netNoVat = 0;
                double totVat = 0, net = 0, sumCost = 0;
                double totQuan = 0;

                foreach (var row in _itemsSource)
                {
                    if (row.ItemId <= 0) continue;
                    sumVal += row.Total;
                    totDiscount += row.ItemDiscount;
                    netNoVat += row.NetAfterDisc;
                    totVat += row.VatVal;
                    net += row.NetWithVat;
                    sumCost += row.AvgCost * row.PrimaryQnty;
                    totQuan += row.PrimaryQnty;
                }

                if (cmbClient.SelectedIndex > -1)
                    CheckForClientOffer(
                        Convert.ToInt32(cmbClient.SelectedValue));

                if (string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
                    txtInvDiscPerc.Text = "0";

                double discVal = double.TryParse(txtInvDiscVal.Text, out double dv) ? dv : 0;
                double discPerc = double.TryParse(txtInvDiscPerc.Text, out double dp) ? dp : 0;

                if (discVal == 0 && discPerc > 0)
                {
                    discVal = netNoVat * (discPerc / 100.0);
                    txtInvDiscVal.Text = discVal.ToString("0.##");
                }

                if (string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                    txtInvDiscVal.Text = "0";

                if (discVal > 0)
                {
                    if (InvObj.PriceIncVAT)
                    {
                        double discTax = discVal -
                            Math.Round(discVal / (1.0 + InvObj.VAT / 100.0), 3);
                        netNoVat -= (discVal - discTax);
                        totVat = Math.Round(netNoVat * (InvObj.VAT / 100.0), 3);
                    }
                    else
                    {
                        netNoVat -= discVal;
                        totVat = Math.Round(netNoVat * (InvObj.VAT / 100.0), 3);
                    }
                    net = netNoVat + totVat;
                    totDiscount += discVal;
                }

                txtTirmsNo.Text = _itemsSource.Count.ToString();
                txtTotQuan.Text = totQuan.ToString("0.##");
                txtSumCostAvrg.Text = Math.Round(sumCost, 2).ToString();
                txtSumVal.Text = Math.Round(sumVal, 2).ToString("0.##");
                txtTotDiscount.Text = Math.Round(totDiscount, 2).ToString("0.##");
                txtNetWithoutVAT.Text = Math.Round(netNoVat, 2).ToString("0.##");
                txtInvProfit.Text = Math.Round(
                    netNoVat - sumCost, 2).ToString();
                txtTotVAT.Text = Math.Round(totVat, 2).ToString("0.##");
                txtNet.Text = Math.Round(net, 2).ToString("0.##");

                GenerateQR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الحساب: " + ex.Message);
            }
        }

        private double CalcVAT(int itemId, decimal price)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT tax,Purch_price,sale_price FROM Items WHERE id=" +
                    itemId, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return 0;

                if (Convert.ToInt32(dt.Rows[0]["tax"]) == 0) return 0;

                if (price == 0)
                    price = (InvType != 1)
                        ? Convert.ToDecimal(dt.Rows[0]["sale_price"])
                        : Convert.ToDecimal(dt.Rows[0]["Purch_price"]);

                return InvObj.PriceIncVAT
                    ? Convert.ToDouble(price) -
                      Math.Round(Convert.ToDouble(price) /
                          (1.0 + InvObj.VAT / 100.0), 3)
                    : Math.Round(Convert.ToDouble(price) *
                          (InvObj.VAT / 100.0), 3);
            }
            catch { return 0; }
        }

        private void CalcDiscount()
        {
            if (!double.TryParse(txtInvDiscVal.Text, out double discVal) ||
                discVal <= 0)
            {
                txtInvDiscPerc.Text = "0";
                txtInvDiscVal.Text = "0";
                InvDiscount = 0;
                CalcTot();
                return;
            }

            if (double.TryParse(txtSumVal.Text, out double sumVal) &&
                sumVal != 0)
            {
                txtInvDiscPerc.Text =
                    Math.Round(discVal / sumVal * 100.0, 2).ToString();
                CalcTot();
            }
        }

        private void CalcDiscount2()
        {
            if (!double.TryParse(txtInvDiscPerc.Text, out double discPerc) ||
                discPerc <= 0)
            {
                txtInvDiscPerc.Text = "0";
                txtInvDiscVal.Text = "0";
                InvDiscount = 0;
                CalcTot();
                return;
            }

            if (double.TryParse(txtSumVal.Text, out double sumVal) &&
                sumVal != 0 &&
                (!double.TryParse(txtInvDiscVal.Text, out double dv) ||
                 dv == 0))
            {
                InvDiscount = Math.Round(discPerc / 100.0 * sumVal, 4);
                txtInvDiscVal.Text = InvDiscount.ToString("0.####");
                CalcTot();
            }
        }

        private bool IsValidQuantity(int index)
        {
            if (index < 0 || index >= _itemsSource.Count) return true;
            var row = _itemsSource[index];

            double available = row.CurrentStock;
            double totQty = 0;

            for (int i = 0; i < _itemsSource.Count; i++)
                if (_itemsSource[i].ItemId == row.ItemId)
                    available -= _itemsSource[i].PrimaryQnty;

            if (ProcCode != -1)
            {
                var adapter = new SqlDataAdapter(
                    "SELECT val FROM Inv_Sub WHERE ItemId=" + row.ItemId +
                    " AND InvGlobalID='" + InvGlobalID + "'", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    available += Convert.ToDouble(dt.Rows[0][0]);
            }

            if (available >= 0) return true;

            string msg = "الكمية المدخلة للصنف أكبر من رصيد المستودع\n" +
                         "الكمية الإجمالية المدخلة: " +
                         row.PrimaryQnty + "\n" +
                         "رصيد المستودع: " + cmbStore.Text +
                         " = " + (available + row.PrimaryQnty);

            DXMessageBox.Show(msg, "", MessageBoxButton.OK,
                MessageBoxImage.Warning);

            if (!InvObj.SaleByMinus && ProcType == 1)
                row.Quantity = Math.Max(0,
                    available + row.PrimaryQnty);

            return false;
        }

        private void IsValidPrice(int index)
        {
            if (index < 0 || index >= _itemsSource.Count) return;
            var row = _itemsSource[index];

            var adapter = new SqlDataAdapter(
                "SELECT sale_price,low_sale_price,high_sale_price " +
                "FROM ItemPrices WHERE ItemId=" + row.ItemId, conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count == 0) return;

            double highPrice = Convert.ToDouble(dt.Rows[0]["high_sale_price"]);
            double lowPrice = Convert.ToDouble(dt.Rows[0]["low_sale_price"]);
            double salePrice = Convert.ToDouble(dt.Rows[0]["sale_price"]);

            if (highPrice > 0 && !User.PassHeighestSalePrice &&
                row.Price > highPrice)
                DXMessageBox.Show("لقد أدخلت السعر أكبر من أعلى سعر",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);

            if (lowPrice > 0 && !User.PassLowestSalePrice &&
                row.Price < lowPrice)
            {
                DXMessageBox.Show("لقد أدخلت السعر أقل من أدنى سعر",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                row.Price = salePrice;
                return;
            }

            if (!User.BuyLessAvrgCost && row.Price < row.AvgCost)
                DXMessageBox.Show("لقد أدخلت سعراً أقل من سعر التكلفة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #endregion

        #region ── DataGrid Events ─────────────────────────────────

        private void dgvItems_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                int idx = dgvItems.SelectedIndex;
                if (idx < 0 || idx >= _itemsSource.Count) return;
                RowIndex = idx;
                if (_itemsSource[idx].ItemId > 0)
                    LoadItemDetails(idx);
            }
            catch { }
        }

        private void dgvItems_CellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            int rowIdx = e.Row.GetIndex();
            if (rowIdx < 0 || rowIdx >= _itemsSource.Count) return;

            RowIndex = rowIdx;
            var row = _itemsSource[rowIdx];
            string colHeader = e.Column.Header?.ToString() ?? "";

            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    try
                    {
                        // Recalc after any edit
                        if (row.ItemId > 0)
                        {
                            IsValidPrice(rowIdx);
                            CalcRowValues(rowIdx);
                            if (!IsLoaded && !InvObj.SaleByMinus)
                                IsValidQuantity(rowIdx);
                            CalcTot();
                        }
                    }
                    catch { }
                }));
        }

        private void dgvItems_BeginningEdit(object sender,
            DataGridBeginningEditEventArgs e)
        {
            RowIndex = e.Row.GetIndex();
        }

        private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceItemRow_frmSalesInvoice row)
            {
                var confirm = DXMessageBox.Show(
                    "هل تريد حذف البند؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    _itemsSource.Remove(row);
                    RenumberRows();
                    CalcTot();
                }
            }
        }

        private void RenumberRows()
        {
            for (int i = 0; i < _itemsSource.Count; i++)
                _itemsSource[i].RowNum = i + 1;
        }

        #endregion

        #region ── ComboBox Events ─────────────────────────────────

        private void cmbPayType_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                int idx = cmbPayType.SelectedIndex;

                if (idx == 2) // بنك
                {
                    cmbBanks.SelectedIndex = -1;
                    cmbTreasury.SelectedIndex = -1;
                    cmbTreasury.IsEnabled = false;
                    cmbBanks.IsEnabled = true;
                    cmbClient.SelectedIndex = 0;
                    BtnReceipts.Visibility = Visibility.Collapsed;
                    cmbBanks.Focus();
                }
                else
                {
                    cmbBanks.IsEnabled = false;
                }

                if (idx == 1) // نقدية
                {
                    cmbTreasury.IsEnabled = true;
                    if (cmbTreasury.Items.Count > 0)
                        cmbTreasury.SelectedIndex = 0;
                    cmbClient.SelectedIndex = 0;
                    BtnReceipts.Visibility = Visibility.Collapsed;
                }

                if (idx == 0) // آجلة
                {
                    cmbTreasury.SelectedIndex = -1;
                    cmbTreasury.IsEnabled = false;
                    cmbClient.SelectedIndex = -1;
                    BtnReceipts.Visibility = Visibility.Visible;
                    cmbClient.Focus();
                }
            }
            catch { }
        }

        private void cmbClient_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try { ShowCustBalance(); } catch { }
        }

        private void cmbClient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SearchByNameClient();
            }
        }

        private void SearchByNameClient()
        {
            try
            {
                string clientName = (cmbClient.SelectedItem as DataRowView)?
                    ["name"]?.ToString() ?? cmbClient.Text;

                var adapter = new SqlDataAdapter(
                    "SELECT id,name FROM Customers WHERE IS_Deleted=0 " +
                    "AND name=N'" + clientName + "' AND (type=1 OR type=3)",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbClient.SelectedValue = dt.Rows[0]["id"];
                else
                    AddNewClient();
            }
            catch { }
        }

        private void AddNewClient()
        {
            try
            {
                var frm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Type = 1;
                frm.txtClientName.Text = cmbClient.Text;
                if (cmbPayType.SelectedIndex == 0)
                    frm.PostponeClient = true;
                frm.ShowDialog();

                if (!string.IsNullOrEmpty(frm.Clientname))
                {
                    LoadCustomers();
                    cmbClient.SelectedValue = frm.ClientId;
                }
            }
            catch { }
        }

        private void ShowCustBalance()
        {
            try
            {
                if (cmbClient.SelectedIndex < 0) return;

                var accAdapter = new SqlDataAdapter(
                    "SELECT Code FROM Accounts_Index WHERE AName=N'" +
                    cmbClient.Text + "' " + Accounting.BranchCondition, conn1);
                var accDt = new DataTable();
                accAdapter.Fill(accDt);
                if (accDt.Rows.Count == 0) return;

                string accCond = " AND Entry_sub.acc_no=" + accDt.Rows[0][0];
                string branchCond = (MainClass.BranchNo != -1)
                    ? "Entry.branch=" + MainClass.BranchNo +
                      " AND Entry_sub.branch=" + MainClass.BranchNo + " AND "
                    : "";

                var adapter = new SqlDataAdapter(
                    "SELECT sum(Entry_sub.dept) AS dept," +
                    "sum(Entry_sub.credit) AS credit " +
                    "FROM Entry,Entry_sub WHERE " + branchCond +
                    "Entry.IS_Deleted=0 AND Entry.state=1 " +
                    "AND Entry.GlobalId=Entry_sub.EntryGlobalId " +
                    accCond +
                    " GROUP BY Entry.id,Entry.date," +
                    "Entry.notes,Entry_sub.acc_no", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                double totalDept = 0;
                double totalCredit = 0;
                foreach (DataRow r in dt.Rows)
                {
                    totalDept += Convert.ToDouble(r["dept"]);
                    totalCredit += Convert.ToDouble(r["credit"]);
                }

                if (totalDept > totalCredit)
                    txtBalance.Text =
                        Math.Round(totalDept - totalCredit, 3).ToString("0.##");
                else if (totalCredit > totalDept)
                    txtBalance.Text =
                        Math.Round(totalCredit - totalDept, 3).ToString("0.##") +
                        " دائن";
                else
                    txtBalance.Text = "0";
            }
            catch { }
        }

        #endregion

        #region ── Discount TextBox Events ─────────────────────────

        private void txtInvDiscVal_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                CalcDiscount();
        }

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                !string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
            {
                txtInvDiscPerc.Text = "0";
                CalcDiscount();
            }
        }

        private void txtInvDiscPerc_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
                CalcDiscount2();
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter &&
                !string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
                CalcDiscount2();
        }

        #endregion

        #region ── Buttons (Add / CostCenter / SalesMen) ───────────

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            frm.Activate();
            frm.ShowDialog();
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmCustomers();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);

                if (InvType == 2) { frm.Title = "تعريف عميل"; frm.Type = 1; }
                else if (InvType == 1) { frm.Title = "تعريف مورد"; frm.Type = 2; }

                frm.ShowDialog();
                if (frm.isDone) LoadCustomers();
            }
            catch { }
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int prevId = (cmbSalesMen.SelectedValue != null)
                ? Convert.ToInt32(cmbSalesMen.SelectedValue) : -1;

            var frm = new frmSalesMen();
            frm.ShowDialog();
            LoadSalesMen();

            try { cmbSalesMen.SelectedValue = prevId; } catch { }
        }

        private void btnAddCostCenter_Click(object sender, RoutedEventArgs e)
        {
            int prevId = (cmbCostCenter.SelectedValue != null)
                ? Convert.ToInt32(cmbCostCenter.SelectedValue) : -1;

            var frm = new frmCostCenter();
            frm.ShowDialog();
            LoadCostCenters();

            try { cmbCostCenter.SelectedValue = prevId; } catch { }
        }

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            btnNew_Click(sender, e);
            if (ProcType == 2 || ProcType == 1)
            {
                ProcCode = -1;
                ISNew = true;
            }
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

            if (frm.ISDone && frm.InvGlobalID != "-1")
                Navigate("SELECT * FROM Inv WHERE branch=" +
                         MainClass.BranchNo +
                         " AND InvGlobalID=N'" + frm.InvGlobalID + "'");
        }

        #endregion

        #region ── Search Tab ───────────────────────────────────────

        // Reference fields for search tab


        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            try
            {
                string branchCond = (MainClass.BranchNo != -1)
                    ? "inv.branch=" + MainClass.BranchNo + " AND "
                    : "";

                string cond;

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    cond = branchCond + " inv.id=" + txtSrchNo.Text + " AND ";
                else if (!string.IsNullOrWhiteSpace(txtSrchreffNo.Text))
                    cond = " inv.Reff_No=" + txtSrchreffNo.Text + " AND ";
                else
                    cond = branchCond + " date>=@date1 AND date<=@date2 AND ";

                if (cmbClientSrch.SelectedIndex > -1)
                    cond += " inv.cust_id=" + cmbClientSrch.SelectedValue + " AND ";

                LoadDG(cond);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void LoadDG(string cond)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Inv.InvGlobalID,Inv.id AS id,Inv.date AS date," +
                    "Reff_No,cust_id FROM Inv WHERE inv_type=" + InvType +
                    " AND proc_type=" + ProcType +
                    " AND " + cond +
                    " Inv.IS_Deleted=0 ORDER BY Inv.id", conn);

                DateTime toDate = txtToDate.DateTime.AddHours(24);
                DateTime fromDate = txtFromDate.DateTime;

                adapter.SelectCommand.Parameters
                    .Add("@date1", SqlDbType.DateTime).Value =
                    fromDate.ToShortDateString();
                adapter.SelectCommand.Parameters
                    .Add("@date2", SqlDbType.DateTime).Value = toDate;

                var dt = new DataTable();
                adapter.Fill(dt);

                var srchItems = new ObservableCollection<SearchResultRow_frmSalesInvoice>();
                foreach (DataRow r in dt.Rows)
                {
                    srchItems.Add(new SearchResultRow_frmSalesInvoice
                    {
                        InvGlobalID = r["InvGlobalID"].ToString(),
                        InvNo = r["id"].ToString(),
                        ReffNo = r["Reff_No"].ToString(),
                        InvDate = Convert.ToDateTime(r["date"])
                                             .ToShortDateString(),
                        ClientName = GetCustName(Convert.ToInt32(r["cust_id"]))
                    });
                }

                dgvSrch.ItemsSource = srchItems;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void dgvSrch_MouseLeftButtonUp(object sender,
            MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is SearchResultRow row)
            {
                if (ISreturn)
                {
                    InvGlobalID = row.InvGlobalID;
                    var adapter = new SqlDataAdapter(
                        "SELECT id FROM Inv WHERE proc_type=2 AND inv_type=" +
                        InvType + " AND Reff_No=N'" + InvGlobalID +
                        "' AND IS_Deleted=0", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count >= 1)
                    {
                        DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                        return;
                    }
                }

                Navigate("SELECT * FROM Inv WHERE branch=" +
                         MainClass.BranchNo +
                         " AND InvGlobalID=N'" + row.InvGlobalID + "'");

                TabControl1.SelectedIndex = 0;
            }
        }

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

        #endregion

        #region ── DGV Search TextBox ───────────────────────────────

        private void txtSrchDgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string searchText = txtSrchDgv.Text.Trim();
            if (string.IsNullOrEmpty(searchText)) return;

            bool found = false;
            for (int i = 0; i < _itemsSource.Count; i++)
            {
                if (_itemsSource[i].ItemCode == searchText ||
                    _itemsSource[i].Barcode == searchText)
                {
                    dgvItems.SelectedIndex = i;
                    dgvItems.ScrollIntoView(dgvItems.SelectedItem);
                    dgvItems.Focus();
                    txtSrchDgv.Text = "";
                    found = true;
                    break;
                }
            }

            if (!found)
                DXMessageBox.Show("غير موجود", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region ── Context Menu Handlers ───────────────────────────

        private void StripItemSearch_Click(object sender, RoutedEventArgs e)
        {
            RowIndex = _itemsSource.Count;
            AddNewItem();
        }

        private void StripItemDetail_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ItemId = _itemsSource[RowIndex].ItemId;
            frm.ShowDialog();
        }

        private void StripAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void StripItemUnits_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            try
            {
                var frm = new frmItemUnits();
                frm.ItemId = _itemsSource[RowIndex].ItemId;
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Unitname))
                    LoadUnitInf(RowIndex, frm.UnitId);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الوحدات: " + ex.Message);
            }
        }

        private void StripAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemCategory();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void StripdeleteRow_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRow();
        }

        private void StripItemProcess_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var frm = new frmRptItemsActivityDetailed();
            frm.SelectedId = _itemsSource[RowIndex].ItemId;
            frm.txtItemName.Text = _itemsSource[RowIndex].ItemName;
            frm.txtItemCode.Text = _itemsSource[RowIndex].ItemCode;
            frm.ShowResult();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void StripClientLastItem_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = _itemsSource[RowIndex].ItemName;
            frm.txtItemCode.Text = _itemsSource[RowIndex].ItemCode;
            frm.Show();
            if (cmbClient.SelectedIndex > -1)
                frm.LastProcess(
                    _itemsSource[RowIndex].ItemId,
                    _itemsSource[RowIndex].StoreId,
                    Convert.ToInt32(cmbClient.SelectedValue), 1);
        }

        private void StripLastItem_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = _itemsSource[RowIndex].ItemName;
            frm.txtItemCode.Text = _itemsSource[RowIndex].ItemCode;
            frm.Show();
            frm.LastProcess(
                _itemsSource[RowIndex].ItemId,
                _itemsSource[RowIndex].StoreId, -1, 1);
        }

        private void StripItemAvrgCost_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            if (_itemsSource[RowIndex].ItemId > 0)
                DXMessageBox.Show(
                    ItemOper.AvgCost(
                        _itemsSource[RowIndex].ItemId,
                        MainClass.BranchNo).ToString(),
                    "متوسط التكلفة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }

        private void StripSerialNo_Click(object sender, RoutedEventArgs e)
        {
            if (RowIndex < 0 || RowIndex >= _itemsSource.Count) return;
            try
            {
                var frm = new frmItemSerialNo();
                int itemId = _itemsSource[RowIndex].ItemId;
                frm.lblItemName.Text = _itemsSource[RowIndex].ItemName;
                frm.ShowDialog();

                if (frm.ISDone && frm.TotQty > 0)
                {
                    dtSerialNo.Rows.Clear();
                    _itemsSource[RowIndex].Quantity = frm.TotQty;

                    // ✅ إصلاح: استخدام var بدل string
                    // لأن ItemSerialNolist قد يحتوي int أو string
                    foreach (var sn in frm.ItemSerialNolist)
                    {
                        dtSerialNo.Rows.Add(itemId, sn.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void DeleteSelectedRow()
        {
            if (dgvItems.SelectedIndex < 0) return;
            RowIndex = dgvItems.SelectedIndex;

            var confirm = DXMessageBox.Show(
                string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? "هل تريد حذف السجل؟"
                    : "Do you want to delete this record?",
                "", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                _itemsSource.RemoveAt(RowIndex);
                RenumberRows();
                CalcTot();
            }
        }

        #endregion

        #region ── Tools Menu Handlers ─────────────────────────────

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DXMessageBox.Show("هل أنت متأكد من استيراد البيانات؟", "",
                    MessageBoxButton.YesNo) == MessageBoxResult.No) return;

                var frm = new frmImportDataGeneral();
                frm.cmbInv.SelectedIndex = 0;
                frm.cmbInv.Visibility = Visibility.Visible;
                frm.cmbInv.IsEnabled = false;
                frm.cmbDataTable.Visibility = Visibility.Collapsed;
                frm.ShowDialog();

                if (frm.ISDone && frm.dtInvSel.Rows.Count > 0)
                {
                    for (int i = 0; i < frm.dtInvSel.Rows.Count; i++)
                    {
                        var newRow = new InvoiceItemRow_frmSalesInvoice
                        {
                            RowNum = _itemsSource.Count + 1,
                            ItemCode = frm.dtInvSel.Rows[i]["ItemCode"].ToString(),
                            Description = frm.dtInvSel.Rows[i]["description"].ToString(),
                            UnitName = Common.GetUnitName(
                                Convert.ToInt32(frm.dtInvSel.Rows[i]["unit"])),
                            Quantity = Convert.ToDouble(
                                frm.dtInvSel.Rows[i]["quntity"])
                        };

                        double importPrice =
                            Convert.ToDouble(frm.dtInvSel.Rows[i]["price"]);
                        if (importPrice > 0) newRow.Price = importPrice;

                        _itemsSource.Add(newRow);
                        RowIndex = _itemsSource.Count - 1;
                        SearchByCode();
                    }
                    DXMessageBox.Show("تم الاستيراد بنجاح");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    DXMessageBox.Show("يجب حفظ الفاتورة قبل التصدير");
                    return;
                }
                if (DXMessageBox.Show("هل أنت متأكد من تصدير البيانات؟", "",
                    MessageBoxButton.YesNo) == MessageBoxResult.No) return;

                var dlg = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };
                if (dlg.ShowDialog() != true) return;

                // Export via Excel Interop
                var excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    DXMessageBox.Show("Excel غير مثبت على هذا الجهاز");
                    return;
                }

                dynamic excel = Activator.CreateInstance(excelType);
                dynamic workbook = excel.Workbooks.Add();
                dynamic sheet = workbook.Sheets[1];

                // Headers
                var headers = new[]
                {
                    "#","رمز الصنف","اسم الصنف","الوصف","الوحدة",
                    "الكمية","السعر","المجموع","الخصم","الإجمالي","الصافي"
                };
                for (int c = 0; c < headers.Length; c++)
                    sheet.Cells[1, c + 1] = headers[c];

                // Data
                for (int i = 0; i < _itemsSource.Count; i++)
                {
                    var r = _itemsSource[i];
                    sheet.Cells[i + 2, 1] = r.RowNum;
                    sheet.Cells[i + 2, 2] = r.ItemCode;
                    sheet.Cells[i + 2, 3] = r.ItemName;
                    sheet.Cells[i + 2, 4] = r.Description;
                    sheet.Cells[i + 2, 5] = r.UnitName;
                    sheet.Cells[i + 2, 6] = r.Quantity;
                    sheet.Cells[i + 2, 7] = r.Price;
                    sheet.Cells[i + 2, 8] = r.Total;
                    sheet.Cells[i + 2, 9] = r.ItemDiscount;
                    sheet.Cells[i + 2, 10] = r.NetAfterDisc;
                    sheet.Cells[i + 2, 11] = r.NetWithVat;
                }

                workbook.SaveAs(dlg.FileName);
                workbook.Close();
                excel.Quit();

                DXMessageBox.Show("تم حفظ الملف في: " + dlg.FileName);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void btnCustomizeDisplay_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomizeShow();
            frm.cmbInv.SelectedIndex = 1;
            frm.cmbInv.IsEnabled = false;
            frm.ShowDialog();
            if (frm.ISDone) LoadDGvSetting();
        }

        private void StripProfit_Click(object sender, RoutedEventArgs e)
        {
            pnlInvProfit.Visibility = (pnlInvProfit.Visibility == Visibility.Visible)
                ? Visibility.Collapsed : Visibility.Visible;
            pnlSumCost.Visibility = pnlInvProfit.Visibility;
        }

        private void BTnShowEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    DXMessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var frm = new frmRptEntries();
                frm.Show();
                frm.Navigate(
                    "SELECT * FROM Entry WHERE IS_Deleted=0 AND GlobalID=N'" +
                    EntryGlobalID + "'");
            }
            catch { }
        }

        #endregion

        #region ── Receipts ─────────────────────────────────────────

        private void BtnReceipts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    DXMessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var frm = new frmSandQ();
                frm.Show();
                frm.cmbClients.SelectedValue = cmbClient.SelectedValue;
                frm.txtVal.Text = txtNet.Text;
                frm.txtNotes.Text = txtNote.Text + " خاصة العميل: " +
                    cmbClient.Text;
            }
            catch { }
        }

        #endregion

        #region ── txtNet DoubleClick (VAT Calc) ───────────────────

        private void txtNet_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                var frm = new FrmCalcVAT();
                frm.ItemVAT = (int)Math.Round(InvObj.VAT);
                frm.ShowDialog();

                if (frm.Price > 0)
                {
                    if (double.TryParse(txtInvDiscVal.Text, out double cur) &&
                        double.TryParse(txtSumVal.Text, out double sum) &&
                        double.TryParse(
                            frm.txtPriceWithOutVAT.Text, out double priceNoVat))
                    {
                        txtInvDiscVal.Text =
                            (cur + (sum - priceNoVat)).ToString("0.##");
                        CalcTot();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region ── QR Code Generation ──────────────────────────────

        private void GenerateQR()
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("رقم الفاتورة: " + txtNo.Text + " | ");
                sb.Append("التاريخ: " +
                    txtDate.DateTime.ToShortDateString() + " | ");
                sb.Append("المجموع: " + txtSumVal.Text + " | ");
                sb.Append("الضريبة: " + txtTotVAT.Text + " | ");
                sb.Append("الصافي: " + txtNet.Text);

                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(
                    sb.ToString(),
                    QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrData);

                byte[] qrBytes = qrCode.GetGraphic(4);

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
                // QR generation is non-critical
                PictureBox1.Source = null;
            }
        }

        #endregion

        #region ── CheckForClientOffer ─────────────────────────────

        private void CheckForClientOffer(int clientId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Offer.offerID,Offer.InvoiceID," +
                    "OfferForClient.IsForAllItems," +
                    "OfferForClient.OfferItemPercentage," +
                    "OfferForClient.OfferTargetQnty," +
                    "OfferForClient.OfferItemValue " +
                    "FROM Offer,OfferForClient " +
                    "WHERE OfferForClient.offerId=Offer.OfferId " +
                    "AND Offer.OfferStartDate<=@CurrentDate " +
                    "AND Offer.OfferExpire>=@CurrentDate " +
                    "AND Offer.offerType=3 AND Offer.ISDeleted=0 " +
                    "AND OfferForClient.ClientId=" + clientId +
                    " ORDER BY Offer.OfferId DESC", conn);
                adapter.SelectCommand.Parameters
                    .Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double invId = Convert.ToDouble(dt.Rows[0]["InvoiceID"]);
                    if (invId == InvType || invId == 0)
                    {
                        if (Convert.ToBoolean(dt.Rows[0]["IsForAllItems"]))
                        {
                            double offerPerc =
                                Convert.ToDouble(dt.Rows[0]["OfferItemPercentage"]);
                            if (offerPerc > 0)
                                txtInvDiscPerc.Text = offerPerc.ToString();
                        }
                    }
                }
            }
            catch { }
        }

        private bool CheckForItemOffer(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Offer.offerID,Offer.InvoiceID," +
                    "OfferItems.OfferNatural,OfferItems.IsGroupedItems," +
                    "OfferItems.unit,OfferItems.OfferTargetQnty," +
                    "OfferItems.OfferItemValue,OfferItems.OfferItemPercentage " +
                    "FROM Offer,OfferItems " +
                    "WHERE OfferItems.offerId=Offer.OfferId " +
                    "AND Offer.OfferStartDate<=@CurrentDate " +
                    "AND Offer.OfferExpire>=@CurrentDate " +
                    "AND Offer.offerType=2 AND Offer.ISDeleted=0 " +
                    "AND OfferItems.ItemID=" + itemId +
                    " ORDER BY Offer.OfferId DESC", conn);
                adapter.SelectCommand.Parameters
                    .Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        #endregion

        #region ── Helpers ──────────────────────────────────────────

        private string GetCustName(int custId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT name FROM Customers WHERE id=" + custId, conn);
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
                    "SELECT id FROM Inv WHERE branch=" + MainClass.BranchNo +
                    " AND inv_type=" + InvType +
                    " AND proc_type=" + ProcType +
                    " AND id=" + txtNo.Text, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void SaveSerialNo(int itemId, string serialNo,
            string invGlobalId)
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                var cmd = new SqlCommand(
                    "INSERT INTO ItemSerialNo(ItemId,SerialNo,InvGlobalID) " +
                    "VALUES(@ItemId,@SerialNo,@InvGlobalID)", conn);
                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                cmd.Parameters.Add("@SerialNo", SqlDbType.NVarChar).Value = serialNo;
                cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalId;
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void PayBill()
        {
            try
            {
                if (InvObj.SaleManIsRequire &&
                    cmbSalesMen.SelectedIndex < 0)
                {
                    DXMessageBox.Show("يجب اختيار المندوب",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbSalesMen.Focus();
                    return;
                }
                SaveAndPrint();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء إدخال الدفع: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSalesEmpNameByInvNo()
        {
            try
            {
                if (ProcCode != -1)
                {
                    var adapter = new SqlDataAdapter(
                        "SELECT users.username FROM Employees,Inv,users " +
                        "WHERE users.emp=Employees.id " +
                        "AND Employees.id=Inv.sales_emp " +
                        "AND InvGlobalID=N'" + InvGlobalID + "'", conn1);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
                return MainClass.UserName;
            }
            catch { return ""; }
        }

        #endregion
    }
    
    // ═══════════════════════════════════════════════════════════
    //                     Model Classes
    // ═══════════════════════════════════════════════════════════

    #region ── InvoiceItemRow_frmSalesInvoice Model ────────────────────────────────

    public class InvoiceItemRow_frmSalesInvoice : INotifyPropertyChanged
    {
        private int _rowNum;
        private string _itemCode = "";
        private int _itemId;
        private string _itemName = "";
        private string _description = "";
        private string _barcode = "";
        private string _unitName = "";
        private double _quantity;
        private double _unitEquality = 1;
        private double _primaryQnty;
        private double _price;
        private double _total;
        private double _avgCost;
        private double _itemDiscount;
        private double _discPerc;
        private double _netAfterDisc;
        private double _vatPerc;
        private double _vatVal;
        private double _netWithVat;
        private double _currentStock;
        private int _storeId;

        public int RowNum { get => _rowNum; set { _rowNum = value; OnPropertyChanged(); } }
        public string ItemCode { get => _itemCode; set { _itemCode = value; OnPropertyChanged(); } }
        public int ItemId { get => _itemId; set { _itemId = value; OnPropertyChanged(); } }
        public string ItemName { get => _itemName; set { _itemName = value; OnPropertyChanged(); } }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public string Barcode { get => _barcode; set { _barcode = value; OnPropertyChanged(); } }
        public string UnitName { get => _unitName; set { _unitName = value; OnPropertyChanged(); } }
        public double Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); } }
        public double UnitEquality { get => _unitEquality; set { _unitEquality = value; OnPropertyChanged(); } }
        public double PrimaryQnty { get => _primaryQnty; set { _primaryQnty = value; OnPropertyChanged(); } }
        public double Price { get => _price; set { _price = value; OnPropertyChanged(); } }
        public double Total { get => _total; set { _total = value; OnPropertyChanged(); } }
        public double AvgCost { get => _avgCost; set { _avgCost = value; OnPropertyChanged(); } }
        public double ItemDiscount { get => _itemDiscount; set { _itemDiscount = value; OnPropertyChanged(); } }
        public double DiscPerc { get => _discPerc; set { _discPerc = value; OnPropertyChanged(); } }
        public double NetAfterDisc { get => _netAfterDisc; set { _netAfterDisc = value; OnPropertyChanged(); } }
        public double VatPerc { get => _vatPerc; set { _vatPerc = value; OnPropertyChanged(); } }
        public double VatVal { get => _vatVal; set { _vatVal = value; OnPropertyChanged(); } }
        public double NetWithVat { get => _netWithVat; set { _netWithVat = value; OnPropertyChanged(); } }
        public double CurrentStock { get => _currentStock; set { _currentStock = value; OnPropertyChanged(); } }
        public int StoreId { get => _storeId; set { _storeId = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(
            [CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this,
               new PropertyChangedEventArgs(name));
    }

    #endregion

    #region ── SearchResultRow_frmSalesInvoice Model ───────────────────────────────

    public class SearchResultRow_frmSalesInvoice
    {
        public string InvGlobalID { get; set; }
        public string InvNo { get; set; }
        public string ReffNo { get; set; }
        public string InvDate { get; set; }
        public string ClientName { get; set; }
    }

    #endregion
}