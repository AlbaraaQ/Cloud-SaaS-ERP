using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPOSPay : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        // ═══ Public Fields ═══
        public InvoiceDGV Invoic        { get; set; }
        public string     DigitsNo      { get; set; } = "N2";
        public double     resTotal      { get; set; } = 0;
        public bool       IsCashPay     { get; set; } = true;
        public string     txtOrderType  { get; set; } = "";
        public double     discountVal   { get; set; } = 0;
        public double     ExtraDiscounts{ get; set; } = 0;
        public double     TaxVal        { get; set; } = 0;
        public double     AdditionalTaxVal { get; set; } = 0;
        public double     RahenVal      { get; set; } = 0;
        public double     DeliveryVal   { get; set; } = 0;
        public int        InvType       { get; set; } = 3;
        public bool       isDone        { get; set; } = false;
        public DataTable  resBanks      { get; set; }
        public double     resNetwork    { get; set; } = 0;
        public double     resCash       { get; set; } = 0;

        // ═══ Private Fields ═══
        private double _cash       = 0;
        private double _network    = 0;
        private double _discPer    = 0;
        private bool   _isATM      = false;
        private int    _rowSelected = 0;
        private bool   _isfirst    = true;
        private bool   _isDouble   = false;
        private double _defDelv    = 0;
        private double _defInsu    = 0;
        private bool   _delvEditable = true;
        private bool   _insEditable  = true;
        private bool   _dScndTime   = false;
        private bool   _vScndTime   = false;
        private bool   _rScndTime   = false;
        private bool   _hScndTime   = false;
        private bool   _dperScndTime = false;

        #endregion

        #region Constructor

        public frmPOSPay()
        {
            InitializeComponent();
            _conn    = MainClass.ConnObj();
            Invoic   = new InvoiceDGV();
            resBanks = new DataTable();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تهيئة القيم
            txtInvSum.Text = Invoic.PriceIncVAT
                ? (Invoic.Net - Invoic.VAT).ToString()
                : Invoic.SumPrice.ToString();

            txtInvDiscount.Text = Invoic.InvDiscount.ToString(DigitsNo);
            ExtraDiscounts      = Convert.ToDouble(Invoic.TotDiscount.ToString(DigitsNo));
            lblNetVal.Text      = Invoic.Net.ToString(DigitsNo);
            txtTotDiscounts.Text = (Invoic.InvDiscount + Invoic.ItemsDiscount).ToString(DigitsNo);

            LoadBanks();
            LoadMainSettings();
            LoadDisplaySettings();

            txtrecrem1.Text = $"{Invoic.Remainder:0.00}";
            IsCashPay       = true;
            txtCashpay.Focus();
            txtCashpay.Text = "";
            txtOrderType    = btnlocal.Content.ToString();

            if (Invoic.PayType == -1)
            {
                btnPayPostpone_Click(null, null);
                IsCashPay = false;
            }
            else
            {
                Invoic.PayType = 1;
            }

            CheckForOffer();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (txtDiscoPerc.IsFocused || txtInvDiscount.IsFocused) return;

            if      (e.Key == Key.Return) SaveRes();
            else if (e.Key == Key.F6)     btnSame_Click(null, null);
            else if (e.Key == Key.Escape) this.Close();
        }

        #endregion

        #region Load Settings

        private void LoadMainSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT DeliveryVal, InsureVal FROM SettingGeneral WHERE Inv_Id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        double.TryParse(dt.Rows[0]["DeliveryVal"].ToString(), out _defDelv);
                        double.TryParse(dt.Rows[0]["InsureVal"].ToString(),   out _defInsu);
                    }
                }
            }
            catch { }
        }

        private void LoadDisplaySettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingDisplayCotrl WHERE Inv_Id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        if (IsZero(dt.Rows[0]["discountVal"]))
                        {
                            btnDiscount.Visibility    = Visibility.Collapsed;
                            txtInvDiscount.Visibility = Visibility.Collapsed;
                        }
                        if (IsZero(dt.Rows[0]["discountPer"]))
                        {
                            btnDiscoPerc.Visibility = Visibility.Collapsed;
                            txtDiscoPerc.Visibility = Visibility.Collapsed;
                        }
                        if (IsZero(dt.Rows[0]["delivery"]))
                        {
                            btnDelv.Visibility = Visibility.Collapsed;
                            txtDevM.Visibility = Visibility.Collapsed;
                        }
                        if (IsZero(dt.Rows[0]["insurance"]))
                        {
                            btnRahn.Visibility = Visibility.Collapsed;
                            txtRahn.Visibility = Visibility.Collapsed;
                        }
                        if (IsZero(dt.Rows[0]["Editdelivery"]))  _delvEditable = false;
                        if (IsZero(dt.Rows[0]["editinsurance"])) _insEditable  = false;
                    }
                }

                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingOrderMethods WHERE Inv_Id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        if (IsZero(dt.Rows[0]["local"]))    btnlocal.Visibility  = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["takeaway"])) btnAway.Visibility   = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["family"]))   btnFamily.Visibility = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["car"]))      btnCar.Visibility    = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["table"]))    btnTable.Visibility  = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["hosting"]))  btnHosting.Visibility = Visibility.Collapsed;
                    }
                }

                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPayMethods WHERE Inv_id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        if (IsZero(dt.Rows[0]["cash"]))    btnCash.Visibility    = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["network"])) btnNetwork.Visibility = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["ATM"]))     btnATM.Visibility     = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["visa"]))    btnVisa.Visibility    = Visibility.Collapsed;
                        if (IsZero(dt.Rows[0]["multi"]))   btnMulti.Visibility   = Visibility.Collapsed;
                    }
                }

                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPayForm WHERE Inv_Id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        if (!Convert.ToBoolean(dt.Rows[0]["ShowCoinPnl"]))
                            FpnlCoin.Visibility = Visibility.Collapsed;

                        if (Convert.ToBoolean(dt.Rows[0]["ShowCoinValue"]))
                        {
                            btnOne.Content      = "1";
                            btnFive.Content     = "5";
                            btnTen.Content      = "10";
                            btnTwe.Content      = "50";
                            btnOneHan.Content   = "100";
                            btnFiveHand.Content = "500";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الإعدادات\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBanks()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM Banks WHERE IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    resBanks = dt;
                }
            }
            catch { }
        }

        #endregion

        #region Offer Check

        private void CheckForOffer()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    @"SELECT * FROM Offer
                      WHERE OfferStartDate<=@CurrentDate AND OfferExpire>=@CurrentDate
                      AND offerType=1 AND ISDeleted=0 ORDER BY OfferId DESC", _conn))
                {
                    da.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 0) return;

                    double invId  = Convert.ToDouble(dt.Rows[0]["InvoiceID"]);
                    if (!(invId == InvType || invId == 0)) return;

                    double netVal   = Invoic.PriceIncVAT
                        ? Invoic.Net
                        : Convert.ToDouble(txtInvSum.Text);
                    double target   = Convert.ToDouble(dt.Rows[0]["OfferValueTarget"]);
                    double offerVal = Convert.ToDouble(dt.Rows[0]["OfferValue"]);
                    double offerPer = Convert.ToDouble(dt.Rows[0]["OfferPercentage"]);

                    if (offerVal > 0 && netVal >= target)
                    {
                        txtInvDiscount.Text  = offerVal.ToString();
                        double.TryParse(txtInvDiscount.Text, out double d);
                        txtTotDiscounts.Text = (ExtraDiscounts + d).ToString();
                    }
                    else if (offerPer > 0 && netVal >= target)
                    {
                        txtDiscoPerc.Text = offerPer.ToString();
                        CalcDiscount2();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Calculation

        public void CalcDiff()
        {
            if (string.IsNullOrWhiteSpace(lblNetVal.Text))
                lblNetVal.Text = "0";
            if (string.IsNullOrWhiteSpace(txtInvSum.Text))
                txtInvSum.Text = "0";
            if (string.IsNullOrWhiteSpace(txtInvDiscount.Text))
                txtInvDiscount.Text = "0";

            try
            {
                double num3 = 0;

                if (Invoic.PriceIncVAT)
                {
                    double.TryParse(txtInvDiscount.Text, out double discVal);
                    double.TryParse(txtInvSum.Text,      out double invSumVal);

                    if (discVal > 0)
                    {
                        double taxPart = discVal - discVal / (1.0 + Invoic.VATperc / 100.0);
                        double base2   = invSumVal + DeliveryVal - (discVal - taxPart);
                        AdditionalTaxVal = base2 * (Invoic.ExtraVATPerc / 100.0);
                        base2 += AdditionalTaxVal;
                        TaxVal = base2 * (Invoic.VATperc / 100.0) + AdditionalTaxVal;
                        num3   = base2 - AdditionalTaxVal;
                    }
                    else
                    {
                        double netV = Invoic.Net - discVal + DeliveryVal;
                        TaxVal = netV - netV / (1.0 + Invoic.VATperc / 100.0);
                        num3   = netV - AdditionalTaxVal - TaxVal;
                    }

                    Invoic.Total = num3;
                }
                else
                {
                    double.TryParse(txtInvSum.Text,      out double invSum);
                    double.TryParse(txtInvDiscount.Text, out double disc);

                    double base2 = invSum - disc - Invoic.ItemsDiscount + DeliveryVal;
                    AdditionalTaxVal = base2 * (Invoic.ExtraVATPerc / 100.0);
                    base2 += AdditionalTaxVal;
                    TaxVal = base2 * (Invoic.VATperc / 100.0) + AdditionalTaxVal;
                    num3   = base2 - AdditionalTaxVal;
                    Invoic.Total = num3;
                }

                double netTotal = num3 + TaxVal + RahenVal;
                lblNetVal.Text  = netTotal.ToString(DigitsNo);
                double.TryParse(lblNetVal.Text, out double lbl);
                txtrecrem1.Text = $"{Invoic.Paid + _cash + _network - lbl:0.00}";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء العملية الحسابية\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalcDiscount2()
        {
            double.TryParse(txtDiscoPerc.Text, out double perc);
            if (perc > 0)
            {
                if (!string.IsNullOrWhiteSpace(lblNetVal.Text))
                {
                    double.TryParse(txtInvDiscount.Text, out double discVal);
                    if (discVal == 0)
                    {
                        discountVal          = discVal;
                        txtTotDiscounts.Text = (ExtraDiscounts + discountVal).ToString();
                        double base2 = Invoic.PriceIncVAT
                            ? Invoic.Net
                            : Convert.ToDouble(txtInvSum.Text);
                        if (base2 != 0)
                        {
                            double newDisc = perc / 100.0 * base2;
                            txtInvDiscount.Text = newDisc.ToString(DigitsNo);
                            CalcDiff();
                        }
                    }
                }
            }
            else
            {
                txtDiscoPerc.Text   = "0";
                txtInvDiscount.Text = "0";
                CalcDiff();
            }
        }

        #endregion

        #region SaveRes

        private void save_Click(object sender, RoutedEventArgs e) => SaveRes();

        private void SaveRes()
        {
            try
            {
                if (Invoic.PayType == -1)
                {
                    resTotal = 0; resCash = 0; resNetwork = 0;
                    isDone   = true;
                    txtCashpay.Text  = "0";
                    txtNetwork.Text  = "0";
                    this.Hide();
                    return;
                }

                double.TryParse(txtrecrem1.Text, out double rem);
                if (rem < 0 && Invoic.PaymentStatus == PaymentStatus.Paid)
                {
                    DXMessageBox.Show("القيمة المدخلة أصغر من الصافي",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCashpay.Text)) return;

                resCash    = Convert.ToDouble(txtCashpay.Text);
                resNetwork = Convert.ToDouble(txtNetwork.Text);

                // ATM/Geidea
                if (resNetwork > 0 && _isATM)
                {
                    byte[] resp = global::UtilitiesProj.Geidea.Pay(resNetwork);
                    string code = $"{(char)resp[0]}{(char)resp[1]}{(char)resp[2]}";
                    string[] validCodes = { "000","001","003","007","087","089" };
                    if (!Array.Exists(validCodes, c => c == code))
                    {
                        DXMessageBox.Show("العملية مرفوضة، يرجى إعادة الدفع",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                resTotal = Math.Round(resNetwork + resCash, 3);
                double.TryParse(lblNetVal.Text, out double netVal);

                if (Invoic.PayType == 4 && Invoic.PaymentStatus == PaymentStatus.Paid
                    && resTotal != netVal - Invoic.Paid)
                {
                    DXMessageBox.Show("يجب أن يكون المجموع يساوي الباقي",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCashpay.Focus();
                    return;
                }

                if (Invoic.PayType == 2 && Invoic.PaymentStatus == PaymentStatus.Paid
                    && resTotal != netVal - Invoic.Paid)
                {
                    DXMessageBox.Show("يجب أن يكون قيمة الشبكة نفس صافي الفاتورة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNetwork.Focus();
                    return;
                }

                if (resTotal < netVal - Invoic.Paid && Invoic.PaymentStatus == PaymentStatus.Paid)
                    return;

                if (Invoic.PayType == 5)
                {
                    resTotal = 0; resCash = 0; resNetwork = 0;
                }

                var payment = new InvoicePayment
                {
                    PayType       = Invoic.PayType,
                    CashPayment   = resCash,
                    MadaPayment   = resNetwork,
                    VisaPayment   = 0,
                    Paid          = resNetwork + resCash,
                    BankId        = -1,
                    Treasury      = User.TreasuryID,
                    DueDate       = DateTime.Now,
                    InvGlobalID   = Invoic.InvGlobalID,
                    PaymentDate   = DateTime.Now,
                    PaymentId     = 0,
                    PaymentStatus = (int)Invoic.PaymentStatus
                };

                isDone       = true;
                Invoic.Paid += payment.Paid;
                Invoic.Net   = netVal;
                payment.Remainder = Invoic.Net - Invoic.Paid;

                double.TryParse(txtInvDiscount.Text, out double invDisc);
                double.TryParse(txtTotDiscounts.Text, out double totDisc);

                Invoic.Paycash    += payment.CashPayment;
                Invoic.PayATM     += payment.MadaPayment;
                Invoic.InvDiscount = invDisc;
                Invoic.Delivery    = DeliveryVal;
                Invoic.Insurance   = RahenVal;
                Invoic.Additions   = DeliveryVal;
                Invoic.TotDiscount = totDisc;
                Invoic.VAT         = TaxVal;
                Invoic.Remainder   = payment.Remainder;

                Invoic.InvoicePayments.Clear();
                Invoic.InvoicePayments.Add(payment);

                if (Invoic.PayType == 5)
                {
                    Invoic.Paid = 0;
                    Invoic.Remainder = 0;
                }

                this.Hide();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الدفع\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Numpad

        private void NumpadDigit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                AppendToActiveInput(btn.Content.ToString());
                _isfirst = false;
            }
        }

        private void NumpadDot_Click(object sender, RoutedEventArgs e)
        {
            AppendToActiveInput(".");
            _isfirst = false;
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.PayType == 2 || Invoic.PayType == 3)
                txtNetwork.Text = "0";
            else
                txtCashpay.Text = "0";
            _isfirst = true;
        }

        private void AppendToActiveInput(string val)
        {
            if (Invoic.PayType == 2 || Invoic.PayType == 3)
            {
                if (txtNetwork.Text == "0") txtNetwork.Text = val;
                else txtNetwork.Text += val;
            }
            else
            {
                if (txtCashpay.Text == "0") txtCashpay.Text = val;
                else txtCashpay.Text += val;
            }
        }

        #endregion

        #region Coin Buttons

        private void SetCoinValue(double amount)
        {
            string v = amount.ToString();
            if (Invoic.PayType == 2 || Invoic.PayType == 3)
                txtNetwork.Text = v;
            else
                txtCashpay.Text = v;
            _isfirst = false;
        }

        private void btnOne_Click(object s, RoutedEventArgs e)     => SetCoinValue(1);
        private void btnFive_Click(object s, RoutedEventArgs e)    => SetCoinValue(5);
        private void btnTen_Click(object s, RoutedEventArgs e)     => SetCoinValue(10);
        private void btnTwe_Click(object s, RoutedEventArgs e)     => SetCoinValue(50);
        private void btnOneHan_Click(object s, RoutedEventArgs e)  => SetCoinValue(100);
        private void btnTwoHan_Click(object s, RoutedEventArgs e)  => SetCoinValue(200);
        private void btnFiveHand_Click(object s, RoutedEventArgs e)=> SetCoinValue(500);

        #endregion

        #region Payment Type Buttons

        private Button[] GetPaywayButtons() => new[]
        {
            btnCash, btnNetwork, btnATM, btnVisa,
            btnMulti, btnHosting, btnPayPostpone, btnPayCash
        };

        private void ResetPaywayColors()
        {
            foreach (var b in GetPaywayButtons())
            {
                b.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#BDC3C7"));
                b.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2C3E50"));
            }
        }

        private void ActivatePayBtn(Button btn)
        {
            ResetPaywayColors();
            btn.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void btnCash_Click(object sender, RoutedEventArgs e)
        {
            Invoic.PayType = 1;
            ActivatePayBtn(btnCash);
            txtNetwork.Text       = "0";
            _network              = 0;
            txtCashpay.Text       = "";
            txtNetwork.IsReadOnly = true;
            txtCashpay.IsReadOnly = false;
            txtCashpay.Focus();
        }

        private void btnNetwork_Click(object sender, RoutedEventArgs e)
        {
            Invoic.PayType = 2;
            _isATM         = false;
            ActivatePayBtn(btnNetwork);
            txtCashpay.IsReadOnly = true;
            txtCashpay.Text       = "0";
            _cash                 = 0;
            double.TryParse(lblNetVal.Text, out double net);
            txtNetwork.Text       = (net - Invoic.Paid).ToString();
            txtNetwork.IsReadOnly = (Invoic.PaymentStatus == PaymentStatus.Paid);
            txtNetwork.Focus();
        }

        private void btnATM_Click(object sender, RoutedEventArgs e)
        {
            Invoic.PayType = 2;
            _isATM         = true;
            ActivatePayBtn(btnATM);
            txtCashpay.IsReadOnly = false;
            txtCashpay.Text       = "0";
            _cash                 = 0;
            double.TryParse(lblNetVal.Text, out double net);
            _network              = net - Invoic.Paid;
            txtNetwork.Text       = _network.ToString();
            txtNetwork.IsReadOnly = false;
            txtNetwork.Focus();
        }

        private void btnVisa_Click(object sender, RoutedEventArgs e)
        {
            Invoic.PayType = 3;
            ActivatePayBtn(btnVisa);
            txtCashpay.Text       = "0";
            _cash                 = 0;
            double.TryParse(lblNetVal.Text, out double net);
            txtNetwork.Text       = (net - Invoic.Paid).ToString();
            txtNetwork.IsReadOnly = false;
            txtCashpay.IsReadOnly = true;
            txtNetwork.Focus();
        }

        private void btnMulti_Click(object sender, RoutedEventArgs e)
        {
            Invoic.PayType = 4;
            _isATM         = false;
            ActivatePayBtn(btnMulti);
            txtNetwork.Text       = "0";
            txtCashpay.Text       = "";
            txtNetwork.IsReadOnly = false;
            txtCashpay.IsReadOnly = false;
            txtCashpay.Focus();
        }

        private void btnPayPostpone_Click(object sender, RoutedEventArgs e)
        {
            ResetPaywayColors();
            if (btnPayPostpone.Visibility == Visibility.Visible)
            {
                btnPayPostpone.Background = new SolidColorBrush(Color.FromRgb(46,204,113));
                btnPayPostpone.Foreground = new SolidColorBrush(Colors.White);
            }
            IsCashPay      = false;
            Invoic.PayType = -1;
        }

        private void btnPayCash_Click(object sender, RoutedEventArgs e)
        {
            ActivatePayBtn(btnPayCash);
            IsCashPay = true;
        }

        #endregion

        #region Order Type Buttons

        private Button[] GetOrderButtons() =>
            new[] { btnlocal, btnAway, btnTable, btnCar, btnFamily };

        private void ResetOrderColors()
        {
            foreach (var b in GetOrderButtons())
            {
                if (b.Visibility != Visibility.Collapsed)
                {
                    b.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#BDC3C7"));
                    b.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#2C3E50"));
                }
            }
        }

        private void ActivateOrderBtn(Button btn)
        {
            ResetOrderColors();
            btn.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void btnlocal_Click(object s, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnlocal);
            Invoic.OrderType = 0;
            txtOrderType     = btnlocal.Content.ToString();
        }

        private void btnAway_Click(object s, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnAway);
            Invoic.OrderType = 1;
            txtOrderType     = btnAway.Content.ToString();
        }

        private void btnFamily_Click(object s, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnFamily);
            Invoic.OrderType = 2;
            txtOrderType     = btnFamily.Content.ToString();
        }

        private void btnTable_Click(object s, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnTable);
            Invoic.OrderType = 3;
            txtOrderType     = btnTable.Content.ToString();
        }

        private void btnCar_Click(object s, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnCar);
            Invoic.OrderType = 4;
            txtOrderType     = btnCar.Content.ToString();
        }

        #endregion

        #region Discount / Delivery / Insurance

        private void btnDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (!_dScndTime)
            {
                var pwd = new frmCheckPwd { operNo = 4 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) return;

                SetBtnActive(btnDiscount);
                txtInvDiscount.IsReadOnly = false;
                _dScndTime    = true;
                SetBtnInactive(btnDiscoPerc);
                txtDiscoPerc.IsReadOnly = true;
                _dperScndTime = false;
                txtDiscoPerc.Text   = "0";
                CalcDiff();
                txtInvDiscount.Text = "";
                txtInvDiscount.Focus();
            }
            else
            {
                SetBtnInactive(btnDiscount);
                txtInvDiscount.IsReadOnly = true;
                _dScndTime              = false;
                txtInvDiscount.Text     = "0";
                CalcDiff();
            }
        }

        private void btnDiscoPerc_Click(object sender, RoutedEventArgs e)
        {
            if (!_dperScndTime)
            {
                var pwd = new frmCheckPwd { operNo = 4 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) return;

                SetBtnActive(btnDiscoPerc);
                txtDiscoPerc.IsReadOnly = false;
                _dperScndTime = true;
                SetBtnInactive(btnDiscount);
                txtInvDiscount.IsReadOnly = true;
                _dScndTime            = false;
                txtInvDiscount.Text   = "0";
                txtDiscoPerc.Text     = "";
                txtDiscoPerc.Focus();
                CalcDiff();
            }
            else
            {
                SetBtnInactive(btnDiscoPerc);
                txtDiscoPerc.IsReadOnly = true;
                _dperScndTime           = false;
                txtDiscoPerc.Text       = "0";
                CalcDiff();
            }
        }

        private void btnDelv_Click(object sender, RoutedEventArgs e)
        {
            if (!_vScndTime)
            {
                SetBtnActive(btnDelv);
                _vScndTime         = true;
                txtDevM.Text       = _defDelv.ToString();
                txtDevM.IsReadOnly = !_delvEditable;
                txtDevM.Focus();
                CalcDiff();
            }
            else
            {
                SetBtnInactive(btnDelv);
                txtDevM.IsReadOnly = true;
                _vScndTime         = false;
                txtDevM.Text       = "0";
                DeliveryVal        = 0;
                CalcDiff();
            }
        }

        private void btnRahn_Click(object sender, RoutedEventArgs e)
        {
            if (!_rScndTime)
            {
                SetBtnActive(btnRahn);
                _rScndTime         = true;
                txtRahn.Text       = _defInsu.ToString();
                txtRahn.IsReadOnly = !_insEditable;
                txtRahn.Focus();
                CalcDiff();
            }
            else
            {
                SetBtnInactive(btnRahn);
                txtRahn.IsReadOnly = true;
                _rScndTime         = false;
                txtRahn.Text       = "0";
                RahenVal           = 0;
                CalcDiff();
            }
        }

        private void btnHosting_Click(object sender, RoutedEventArgs e)
        {
            if (!_hScndTime)
            {
                var pwd = new frmCheckPwd { operNo = 3 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) return;

                ResetOrderColors();
                SetBtnActive(btnHosting);
                _hScndTime           = true;
                txtCashpay.Text      = "0";
                txtNetwork.Text      = "0";
                txtrecrem1.Text      = "0";
                Invoic.PayType       = 5;
            }
            else
            {
                SetBtnInactive(btnHosting);
                _hScndTime = false;
            }
        }

        #endregion

        #region Same Button

        private void btnSame_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(lblNetVal.Text, out double net);

            if (Invoic.PayType == 1)
            {
                txtCashpay.Text = (net - Invoic.Paid).ToString();
                txtNetwork.Text = "0";
                _cash           = net - Invoic.Paid;
                _network        = 0;
                CalcDiff();
                SaveRes();
            }
            else if (Invoic.PayType == 2 || Invoic.PayType == 3)
            {
                txtNetwork.Text = (net - Invoic.Paid).ToString();
                txtCashpay.Text = "0";
                _network        = net - Invoic.Paid;
                _cash           = 0;
                CalcDiff();
                SaveRes();
            }
            else if (Invoic.PayType == 4)
            {
                DXMessageBox.Show("يجب اختيار طريقة دفع أخرى",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCashpay.Text = "0";
                txtNetwork.Text = "0";
                _cash = 0; _network = 0;
            }
        }

        #endregion

        #region TextChanged Events

        private void txtCashpay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtCashpay.Text))
            {
                double.TryParse(txtCashpay.Text, out _cash);

                if (_cash > 0 && _network > 0)
                    Invoic.PayType = 4;

                if (Invoic.PayType == 4 && _cash > 0)
                {
                    double.TryParse(lblNetVal.Text, out double net);
                    _network        = net - _cash - Invoic.Paid;
                    txtNetwork.Text = _network.ToString();
                }

                CalcDiff();
            }
            else
            {
                _cash = 0;
            }
        }

        private void txtNetwork_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtNetwork.Text))
            {
                double.TryParse(txtNetwork.Text, out _network);
                CalcDiff();
            }
            else
            {
                _network = 0;
            }
        }

        private void txtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscount.Text))
            {
                // 1. استخدام متغير محلي للـ out
                double.TryParse(txtInvDiscount.Text, out double tempDisc);

                // 2. إسناد القيمة للخاصية
                discountVal = tempDisc;

                txtTotDiscounts.Text = (ExtraDiscounts + discountVal).ToString();
                CalcDiff();
            }
            else
            {
                discountVal = 0;
            }
        }

        private void txtDevM_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtDevM.Text))
            {
                // 1. استخدام متغير محلي للـ out
                double.TryParse(txtDevM.Text, out double tempDelv);

                // 2. إسناد القيمة للخاصية
                DeliveryVal = tempDelv;

                CalcDiff();
            }
            else
            {
                DeliveryVal = 0;
            }
        }

        private void txtRahn_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtRahn.Text))
            {
                // 1. استخدام متغير محلي للـ out
                double.TryParse(txtRahn.Text, out double tempRahn);

                // 2. إسناد القيمة للخاصية
                RahenVal = tempRahn;

                CalcDiff();
            }
            else
            {
                RahenVal = 0;
            }
        }

        private void txtDiscoPerc_Leave(object sender, RoutedEventArgs e)
            => CalcDiscount2();

        private void txtDiscoPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscount2();
        }

        #endregion

        #region Cancel

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Helpers

        private bool IsZero(object val)
            => val != DBNull.Value && Convert.ToInt32(val) == 0;

        private void SetBtnActive(Button btn)
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void SetBtnInactive(Button btn)
        {
            btn.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#BDC3C7"));
            btn.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#2C3E50"));
        }

        #endregion
    }
}