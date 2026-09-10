using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using ECRPaymentsAPI;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPOSBill : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        // ═══ Public Fields (محفوظة كما في الأصل) ═══
        public InvoiceDGV    Invoic          { get; set; }
        public string        DigitsNo        { get; set; } = "N2";
        public bool          IsCashPay       { get; set; } = true;
        public string        txtOrderType    { get; set; } = "";
        public double        discountVal     { get; set; } = 0;
        public double        ExtraDiscounts  { get; set; } = 0;
        public double        AdditionalTaxVal{ get; set; } = 0;
        public double        RahenVal        { get; set; } = 0;
        public double        DeliveryVal     { get; set; } = 0;
        public int           InvType         { get; set; } = 3;
        public bool          isDone          { get; set; } = false;
        public DataTable     resBanks        { get; set; }

        // ═══ Private Fields ═══
        private double        _discPer        = 0;
        private bool          _isATM          = false;
        private InvoicePayment _payment;
        private int           _rowSelected    = 0;
        private bool          _isfirst        = true;
        private bool          _isDouble       = false;
        private double        _defDelv        = 0;
        private double        _defInsu        = 0;
        private bool          _delvEditable   = true;
        private bool          _insEditable    = true;
        private bool          _dScndTime      = false;
        private bool          _vScndTime      = false;
        private bool          _rScndTime      = false;
        private bool          _hScndTime      = false;
        private bool          _dperScndTime   = false;

        #endregion

        #region Constructor

        public frmPOSBill()
        {
            InitializeComponent();
            _conn    = MainClass.ConnObj();
            Invoic   = new InvoiceDGV();
            _payment = new InvoicePayment();
            resBanks = new DataTable();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMainSettings();
            LoadDisplaySettings();

            IsCashPay = true;
            txtCashpay.Focus();
            txtCashpay.Text  = "";
            txtOrderType     = btnlocal.Content.ToString();

            if (Invoic.PayType == -1)
            {
                _payment.PayType = -1;
                btnPayPostpone_Click(null, null);
                IsCashPay = false;
            }
            else
            {
                _payment.PayType = 1;
            }

            CalcDiff();
            CheckForOffer();

            if (MainClass.Language == "ar")
                btnNetwork.Content = MainClass.PaymentName;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (txtDiscoPerc.IsFocused || txtInvDiscount.IsFocused) return;

            if (e.Key == Key.Return)
                _ = SaveRes();
            else if (e.Key == Key.F6)
                btnSame_Click(null, null);
            else if (e.Key == Key.Escape)
                this.Close();
        }

        #endregion

        #region Load Settings

        private void LoadMainSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT DeliveryVal, InsureVal FROM SettingGeneral WHERE Inv_Id=3",
                    _conn))
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
                // ═══ إعدادات العناصر المرئية ═══
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingDisplayCotrl WHERE Inv_Id=3", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        if (IsZero(dt.Rows[0]["discountVal"]))
                        {
                            btnDiscount.Visibility  = Visibility.Collapsed;
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
                        if (IsZero(dt.Rows[0]["Editdelivery"]))
                            _delvEditable = false;
                        if (IsZero(dt.Rows[0]["editinsurance"]))
                            _insEditable = false;
                    }
                }

                // ═══ إعدادات طرق الطلب ═══
                using (var da = new SqlDataAdapter(
                    @"SELECT ISNULL(local,1) AS local, ISNULL(takeaway,1) AS takeaway,
                             ISNULL(family,0) AS family, ISNULL(car,0) AS car,
                             ISNULL([table],0) AS [table], ISNULL(hosting,0) AS hosting,
                             ISNULL(DefaultOrderType,'local') AS DefaultOrderType
                      FROM SettingOrderMethods WHERE Inv_Id=3", _conn))
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

                        string defOrder = dt.Rows[0]["DefaultOrderType"].ToString();
                        Invoic.OrderType = defOrder switch
                        {
                            "local"    => 0,
                            "takeaway" => 1,
                            "family"   => 2,
                            "table"    => 3,
                            "car"      => 4,
                            "hosting"  => 5,
                            _          => 0
                        };
                    }
                }

                // ═══ إعدادات طرق الدفع ═══
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

                        if (dt.Rows[0]["Bank"] == DBNull.Value || IsZero(dt.Rows[0]["Bank"]))
                            BtnBank.Visibility = Visibility.Collapsed;

                        if (dt.Rows[0]["CreditMulti"] != DBNull.Value
                            && Convert.ToBoolean(dt.Rows[0]["CreditMulti"]))
                            BtnMultiCredit.Visibility = Visibility.Visible;
                        else
                            BtnMultiCredit.Visibility = Visibility.Collapsed;
                    }
                }

                // ═══ إعدادات لوحة الفئات النقدية ═══
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
                            btnOne.Content     = "1";
                            btnFive.Content    = "5";
                            btnTen.Content     = "10";
                            btnTwe.Content     = "50";
                            btnOneHan.Content  = "100";
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

        #endregion

        #region Offer Check

        private void CheckForOffer()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    @"SELECT * FROM Offer
                      WHERE OfferStartDate<=@CurrentDate AND OfferExpire>=@CurrentDate
                      AND offerType=1 AND ISDeleted=0 ORDER BY OfferId DESC",
                    _conn))
                {
                    da.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value
                        = DateTime.Now;

                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0) return;

                    double invId  = Convert.ToDouble(dt.Rows[0]["InvoiceID"]);
                    bool   match  = (invId == InvType || invId == 0.0);
                    if (!match) return;

                    double netVal   = Invoic.PriceIncVAT
                        ? Invoic.Net
                        : Convert.ToDouble(txtInvSum.Text);
                    double target   = Convert.ToDouble(dt.Rows[0]["OfferValueTarget"]);
                    double offerVal = Convert.ToDouble(dt.Rows[0]["OfferValue"]);
                    double offerPer = Convert.ToDouble(dt.Rows[0]["OfferPercentage"]);

                    if (offerVal > 0)
                    {
                        if (netVal >= target)
                        {
                            txtInvDiscount.Text = offerVal.ToString();
                            double.TryParse(txtInvDiscount.Text, out double d);
                            txtTotDiscounts.Text = (ExtraDiscounts + d).ToString();
                        }
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
            try
            {
                double.TryParse(txtDevM.Text, out double devVal);
                Invoic.Additions = devVal;

                // ════ حل المشكلة هنا ════
                // 1. أخذ نسخة في متغير محلي
                var tempInvoice = Invoic;

                // 2. تمرير المتغير المحلي كـ ref
                ItemOper.CalcRows(ref tempInvoice);

                // 3. إعادة القيمة المُحدثة إلى الـ Property الأصلية
                Invoic = tempInvoice;
                // ═════════════════════════

                Invoic.Net += devVal;

                _payment.Paid = _payment.CashPayment + _payment.MadaPayment;
                double rahnVal = 0;
                double.TryParse(txtRahn.Text, out rahnVal);
                _payment.Remainder = Invoic.Net + rahnVal - Invoic.Paid - _payment.Paid;

                txtInvDiscount.Text = Invoic.InvDiscount.ToString(DigitsNo);
                txtDiscoPerc.Text = InvoiceOper
                    .GetDiscountPercentage(Invoic.SumPrice, Invoic.InvDiscount)
                    .ToString(DigitsNo);

                lblNetVal.Text = Invoic.Net.ToString(DigitsNo);
                txtTotDiscounts.Text = Invoic.TotDiscount.ToString(DigitsNo);

                txtInvSum.Text = Invoic.PriceIncVAT
                    ? (Invoic.Net - Invoic.VAT).ToString(DigitsNo)
                    : Invoic.Total.ToString(DigitsNo);

                txtrecrem1.Text = _payment.Remainder.ToString(DigitsNo);
            }
            catch { }
        }

        private void CalcDiscount()
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscount.Text))
            {
                double.TryParse(txtInvDiscount.Text, out double disc);
                Invoic.InvDiscount = disc;
                CalcDiff();
            }
            else
            {
                Invoic.InvDiscount = 0;
                txtDiscoPerc.Text  = "0";
                CalcDiff();
                Invoic.IsUpdated = false;
            }
        }

        private void CalcDiscountPerc()
        {
            if (!string.IsNullOrWhiteSpace(txtDiscoPerc.Text))
            {
                double.TryParse(txtDiscoPerc.Text, out double perc);
                Invoic.InvDiscount = Convert.ToDouble(
                    InvoiceOper.GetInvDiscountByPercentage(Invoic.SumPrice, perc));
                CalcDiff();
            }
            else
            {
                txtDiscoPerc.Text  = "0";
                Invoic.InvDiscount = 0;
                CalcDiff();
                Invoic.IsUpdated = false;
            }
        }

        private void CalcDiscount2()
        {
            double.TryParse(txtDiscoPerc.Text, out double percVal);
            if (percVal > 0)
            {
                double.TryParse(txtInvDiscount.Text, out double discVal);
                if (!string.IsNullOrEmpty(lblNetVal.Text) && discVal == 0)
                {
                    discountVal          = discVal;
                    txtTotDiscounts.Text = (ExtraDiscounts + discountVal).ToString();

                    double netBase = Invoic.PriceIncVAT
                        ? Invoic.Net
                        : Convert.ToDouble(txtInvSum.Text);

                    if (netBase != 0)
                    {
                        double newDisc = Math.Round(percVal / 100.0 * netBase, 4);
                        txtInvDiscount.Text  = newDisc.ToString();
                        Invoic.InvDiscount   = newDisc;
                        CalcDiff();
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

        #region Save

        private void save_Click(object sender, RoutedEventArgs e)
            => _ = SaveRes();

        private async Task SaveRes()
        {
            try
            {
                if (_payment.PayType == -1)
                {
                    _payment.Paid        = 0;
                    _payment.CashPayment = 0;
                    _payment.MadaPayment = 0;
                    isDone               = true;
                    txtCashpay.Text      = "0";
                    txtNetwork.Text      = "0";
                    this.Hide();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCashpay.Text)) return;

                double netVal = 0;
                double.TryParse(lblNetVal.Text, out netVal);
                double rahnVal = 0;
                double.TryParse(txtRahn.Text, out rahnVal);

                // التحقق ATM/Geidea
                if (_payment.MadaPayment > 0 && _isATM)
                {
                    double.TryParse(txtNetwork.Text, out double networkAmt);

                    if (!MainSetting.IsGediaActive)
                    {
                        byte[] resp = global::UtilitiesProj.Geidea.Pay(networkAmt);
                        string code = $"{(char)resp[0]}{(char)resp[1]}{(char)resp[2]}";
                        string[] validCodes = { "000","001","003","007","087","089" };
                        if (!Array.Exists(validCodes, c => c == code))
                        {
                            DXMessageBox.Show("العملية مرفوضة، يرجى إعادة الدفع",
                                "", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                    else
                    {
                        var resp = await GeideaPament.Pay(new PaymentRequestViewModel
                        {
                            EnableReceiptPrint = MainSetting.GediaEnableReceiptPrint,
                            Amount  = networkAmt,
                            ComPort = MainSetting.GediaPort
                        });
                        if (!resp.IsSucess)
                        {
                            DXMessageBox.Show(resp.ResposeMsg,
                                "", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                _payment.Paid = _payment.CashPayment + _payment.MadaPayment;

                // تحقق الدفع المتعدد
                if (_payment.PayType == 4 && Invoic.PaymentStatus == PaymentStatus.Paid
                    && _payment.Paid != netVal + rahnVal - Invoic.Paid)
                {
                    DXMessageBox.Show("يجب أن يكون المجموع يساوي الباقي",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCashpay.Focus();
                    return;
                }

                if (_payment.PayType == 2 && Invoic.PaymentStatus == PaymentStatus.Paid
                    && _payment.Paid != netVal + rahnVal - Invoic.Paid)
                {
                    DXMessageBox.Show("قيمة مدفوع الشبكة يجب أن تساوي صافي الفاتورة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNetwork.Focus();
                    return;
                }

                if (netVal <= 0 && _payment.PayType != 5)
                {
                    DXMessageBox.Show("يجب أن يكون الصافي أكبر من الصفر",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_payment.PayType == 5)
                {
                    _payment.Paid      = 0;
                    _payment.CashPayment = 0;
                    _payment.MadaPayment = 0;
                    _payment.Remainder   = 0;
                }

                // إتمام الدفع
                _payment.VisaPayment  = 0;
                _payment.Paid         = _payment.CashPayment + _payment.MadaPayment;
                _payment.BankId       = -1;
                _payment.Treasury     = User.TreasuryID;
                _payment.DueDate      = DateTime.Now;
                _payment.InvGlobalID  = Invoic.InvGlobalID;
                _payment.PaymentDate  = DateTime.Now;
                _payment.PaymentId    = 0;
                _payment.PaymentStatus = (int)Invoic.PaymentStatus;

                isDone          = true;
                Invoic.PayType  = _payment.PayType;
                Invoic.Paid    += _payment.Paid;
                Invoic.Net      = netVal;

                _payment.Remainder = Invoic.Net - Invoic.Paid;

                double.TryParse(txtInvDiscount.Text, out double invDisc);
                double.TryParse(txtDevM.Text,        out double devM);
                double.TryParse(txtRahn.Text,         out double rahn);
                double.TryParse(txtTotDiscounts.Text, out double totDisc);

                Invoic.Paycash     += _payment.CashPayment;
                Invoic.PayATM      += _payment.MadaPayment;
                Invoic.InvDiscount  = invDisc;
                Invoic.Delivery     = devM;
                Invoic.Insurance    = rahn;
                Invoic.Additions    = devM;
                Invoic.TotDiscount  = totDisc;
                Invoic.IsUpdated    = false;
                Invoic.Remainder    = _payment.Remainder;

                Invoic.InvoicePayments.Clear();
                Invoic.InvoicePayments.Add(_payment);

                if (Invoic.PayType == 5)
                {
                    Invoic.Paid      = 0;
                    Invoic.Remainder = 0;
                }

                this.Hide();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ أثناء الدفع\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Numpad Buttons

        private void NumpadDigit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string digit = btn.Content.ToString();
                AppendToActiveInput(digit);
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
            if (_payment.PayType == 2 || _payment.PayType == 3)
                txtNetwork.Text = "0";
            else
                txtCashpay.Text = "0";

            _isfirst = true;
        }

        private void AppendToActiveInput(string value)
        {
            if (_payment.PayType == 2 || _payment.PayType == 3)
            {
                if (txtNetwork.Text == "0") txtNetwork.Text = value;
                else txtNetwork.Text += value;
            }
            else
            {
                if (txtCashpay.Text == "0") txtCashpay.Text = value;
                else txtCashpay.Text += value;
            }
        }

        #endregion

        #region Coin Buttons

        private void SetCoinValue(double amount)
        {
            string val = amount.ToString();
            if (_payment.PayType == 2 || _payment.PayType == 3)
                txtNetwork.Text = val;
            else
                txtCashpay.Text = val;
            _isfirst = false;
        }

        private void btnOne_Click(object sender, RoutedEventArgs e)    => SetCoinValue(1);
        private void btnFive_Click(object sender, RoutedEventArgs e)   => SetCoinValue(5);
        private void btnTen_Click(object sender, RoutedEventArgs e)    => SetCoinValue(10);
        private void btnTwe_Click(object sender, RoutedEventArgs e)    => SetCoinValue(50);
        private void btnOneHan_Click(object sender, RoutedEventArgs e) => SetCoinValue(100);
        private void btnTwoHan_Click(object sender, RoutedEventArgs e) => SetCoinValue(200);
        private void btnFiveHand_Click(object sender, RoutedEventArgs e) => SetCoinValue(500);

        #endregion

        #region Payment Type Buttons

        private void ResetPaywayColors()
        {
            foreach (UIElement el in FindPaywayButtons())
            {
                if (el is Button b)
                {
                    b.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#BDC3C7"));
                    b.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#2C3E50"));
                }
            }
        }

        private UIElement[] FindPaywayButtons() => new UIElement[]
        {
            btnCash, btnNetwork, btnATM, btnVisa,
            btnMulti, btnHosting, BtnBank, BtnMultiCredit, btnPayPostpone
        };

        private void ActivatePayBtn(Button btn)
        {
            ResetPaywayColors();
            btn.Background = new SolidColorBrush(Color.FromRgb(46,204,113));
            btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void btnCash_Click(object sender, RoutedEventArgs e)
        {
            _payment.PayType = 1;
            _isATM           = false;
            ActivatePayBtn(btnCash);
            txtNetwork.Text      = "0";
            txtNetwork.IsReadOnly = true;
            txtCashpay.IsReadOnly = false;
            txtCashpay.Text       = "";
            txtCashpay.Focus();
            Invoic.Bank          = -1;
            _payment.MadaPayment = 0;
            _payment.CashPayment = GetNetVal() - Invoic.Paid;
        }

        private void btnNetwork_Click(object sender, RoutedEventArgs e)
        {
            _payment.PayType = 2;
            _isATM           = false;
            ActivatePayBtn(btnNetwork);
            txtCashpay.IsReadOnly = true;
            txtCashpay.Text       = "0";
            Invoic.Bank           = 1;
            _payment.CashPayment  = 0;
            _payment.MadaPayment  = GetNetVal() - Invoic.Paid;
            txtNetwork.Text       = _payment.MadaPayment.ToString();
            txtNetwork.IsReadOnly = (Invoic.PaymentStatus == PaymentStatus.Paid);
            txtNetwork.Focus();
        }

        private void btnATM_Click(object sender, RoutedEventArgs e)
        {
            _payment.PayType = 2;
            _isATM           = true;
            ActivatePayBtn(btnATM);
            txtCashpay.IsReadOnly  = false;
            txtCashpay.Text        = "0";
            Invoic.Bank            = 1;
            _payment.MadaPayment   = GetNetVal() - Invoic.Paid;
            txtNetwork.Text        = _payment.MadaPayment.ToString();
            txtNetwork.IsReadOnly  = false;
            txtNetwork.Focus();
        }

        private void btnVisa_Click(object sender, RoutedEventArgs e)
        {
            _payment.PayType = 3;
            ActivatePayBtn(btnVisa);
            txtCashpay.Text        = "0";
            txtCashpay.IsReadOnly  = true;
            _payment.CashPayment   = 0;
            _payment.MadaPayment   = GetNetVal() - Invoic.Paid;
            txtNetwork.Text        = _payment.MadaPayment.ToString();
            txtNetwork.IsReadOnly  = false;
            Invoic.Bank            = 1;
            txtNetwork.Focus();
        }

        private void btnMulti_Click(object sender, RoutedEventArgs e)
        {
            _payment.PayType = 4;
            _isATM           = false;
            ActivatePayBtn(btnMulti);
            txtNetwork.Text        = "0";
            txtCashpay.Text        = "";
            txtNetwork.IsReadOnly  = true;
            txtCashpay.IsReadOnly  = false;
            txtCashpay.Focus();
            Invoic.Bank            = 1;
            _payment.MadaPayment   = 0;
        }

        private void btnPayPostpone_Click(object sender, RoutedEventArgs e)
        {
            ResetPaywayColors();
            if (btnPayPostpone.Visibility == Visibility.Visible)
            {
                btnPayPostpone.Background = new SolidColorBrush(Color.FromRgb(46,204,113));
                btnPayPostpone.Foreground = new SolidColorBrush(Colors.White);
            }
            IsCashPay        = false;
            _payment.PayType = -1;
        }

        private void BtnMultiCredit_Click(object sender, RoutedEventArgs e)
        {
            ResetPaywayColors();
            BtnMultiCredit.Background = new SolidColorBrush(Color.FromRgb(46,204,113));
            BtnMultiCredit.Foreground = new SolidColorBrush(Colors.White);

            double.TryParse(lblNetVal.Text, out double net);

            var frm = new frmPayMultiCredit { InvoiceNet = net };
            frm.ShowDialog();

            if (frm.IsDone)
            {
                Invoic.Customer      = frm.InvCustomer;
                Invoic.PayType       = 7;
                _payment.PayType     = 7;
                _payment.CashPayment = frm.payment.CashPayment ?? 0;
                _payment.VisaPayment = frm.payment.VisaPayment ?? 0;
                _payment.Paid        = frm.payment.Paid ?? 0;
                txtCashpay.Text      = (_payment.CashPayment).ToString();
                txtNetwork.Text      = (_payment.VisaPayment).ToString();
                _ = SaveRes();
            }
        }

        private void BtnBank_Click(object sender, RoutedEventArgs e)
        {
            var bankFrm = new frmPayBank();
            bankFrm.ShowDialog();

            if (bankFrm.SelectedBankId == 0) return;

            _payment.PayType = 2;
            _isATM           = false;
            ActivatePayBtn(BtnBank);

            txtCashpay.IsReadOnly = true;
            txtCashpay.Text       = "0";
            Invoic.Bank           = bankFrm.SelectedBankId;
            _payment.BankId       = bankFrm.SelectedBankId;
            _payment.CashPayment  = 0;
            _payment.MadaPayment  = GetNetVal() - Invoic.Paid;
            txtNetwork.Text       = _payment.MadaPayment.ToString();
            txtNetwork.IsReadOnly = (Invoic.PaymentStatus == PaymentStatus.Paid);
            txtNetwork.Focus();
        }

        #endregion

        #region Order Type Buttons

        private void ResetOrderColors()
        {
            foreach (var b in new[] { btnlocal, btnAway, btnTable, btnCar, btnFamily })
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
            btn.Background = new SolidColorBrush(Color.FromRgb(46,204,113));
            btn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void btnlocal_Click(object sender, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnlocal);
            Invoic.OrderType = 0;
            txtOrderType     = btnlocal.Content.ToString();
        }

        private void btnAway_Click(object sender, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnAway);
            Invoic.OrderType = 1;
            txtOrderType     = btnAway.Content.ToString();
        }

        private void btnFamily_Click(object sender, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnFamily);
            Invoic.OrderType = 2;
            txtOrderType     = btnFamily.Content.ToString();
        }

        private void btnTable_Click(object sender, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnTable);
            Invoic.OrderType = 3;
            txtOrderType     = btnTable.Content.ToString();

            var inp = new frmInputs();
            inp.ShowDialog();
            if (inp.isDone)
                Invoic.TableNo = inp.tableNo;
        }

        private void btnCar_Click(object sender, RoutedEventArgs e)
        {
            ActivateOrderBtn(btnCar);
            Invoic.OrderType = 4;
            txtOrderType     = btnCar.Content.ToString();
        }

        #endregion

        #region Discount / Delivery / Insurance Buttons

        private void btnDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.PaymentStatus == PaymentStatus.RePaid)
                _dScndTime = true;

            if (!_dScndTime)
            {
                if (!User.DisableDiscPassword)
                {
                    var pwd = new frmCheckPwd { operNo = 4 };
                    pwd.ShowDialog();
                    if (!pwd.Iscorrect)
                    {
                        DXMessageBox.Show("كلمة المرور خطأ");
                        return;
                    }
                }

                SetBtnActive(btnDiscount);
                txtInvDiscount.IsReadOnly = false;
                _dScndTime = true;

                SetBtnInactive(btnDiscoPerc);
                txtDiscoPerc.IsReadOnly = true;
                _dperScndTime = false;
                txtDiscoPerc.Text   = "0";
                CalcDiff();
                txtInvDiscount.Text = "";
                txtInvDiscount.Focus();

                decimal maxDisc = LoadUserMaxDiscount();

                var calc = new Frm_Calculator { AllowChar = true };
                calc.txtMsg.Text = "إدخل الخصم";
                calc.ShowDialog();

                if (!calc.is_close)
                {
                    double.TryParse(calc.TextBox1.Text, out double dVal);
                    if (dVal > (double)maxDisc)
                    {
                        DXMessageBox.Show("عذرًا لقد تجاوزت حد الخصم المسموح به",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    txtInvDiscount.Text = dVal.ToString();
                    CalcDiscount();
                }
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
            if (Invoic.PaymentStatus == PaymentStatus.RePaid)
                _dperScndTime = true;

            if (!_dperScndTime)
            {
                if (!User.DisableDiscPassword)
                {
                    var pwd = new frmCheckPwd { operNo = 4 };
                    pwd.ShowDialog();
                    if (!pwd.Iscorrect)
                    {
                        DXMessageBox.Show("كلمة المرور خطأ");
                        return;
                    }
                }

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

                decimal maxPerc = LoadUserMaxDiscountPerc();

                var calc = new Frm_Calculator { AllowChar = true };
                calc.txtMsg.Text = "إدخل نسبة الخصم";
                calc.ShowDialog();

                if (!calc.is_close)
                {
                    double.TryParse(calc.TextBox1.Text, out double pVal);
                    if (pVal > (double)maxPerc)
                    {
                        DXMessageBox.Show("عذرًا لقد تجاوزت حد الخصم المسموح به",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    txtDiscoPerc.Text = pVal.ToString();
                    CalcDiscountPerc();
                }
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
            if (Invoic.PaymentStatus == PaymentStatus.RePaid)
                _vScndTime = true;

            if (!_vScndTime)
            {
                SetBtnActive(btnDelv);
                _vScndTime          = true;
                txtDevM.Text        = _defDelv.ToString();
                txtDevM.IsReadOnly  = false;
                txtDevM.Focus();

                var calc = new Frm_Calculator { AllowChar = true };
                calc.txtMsg.Text = "أدخل قيمة التوصيل";
                calc.ShowDialog();

                if (!calc.is_close)
                {
                    txtDevM.Text = calc.TextBox1.Text;
                    if (!_delvEditable)
                        txtDevM.IsReadOnly = true;
                    CalcDiff();
                }
            }
            else
            {
                SetBtnInactive(btnDelv);
                txtDevM.IsReadOnly = true;
                _vScndTime         = false;
                txtDevM.Text       = "0";
                CalcDiff();
            }
        }

        private void btnRahn_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.PaymentStatus == PaymentStatus.RePaid)
                _rScndTime = true;

            if (!_rScndTime)
            {
                SetBtnActive(btnRahn);
                _rScndTime          = true;
                txtRahn.Text        = _defInsu.ToString();
                txtRahn.IsReadOnly  = !_insEditable;
                txtRahn.Focus();
                CalcDiff();
            }
            else
            {
                SetBtnInactive(btnRahn);
                txtRahn.IsReadOnly = true;
                _rScndTime         = false;
                txtRahn.Text       = "0";
                CalcDiff();
            }
        }

        private void btnHosting_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.PaymentStatus == PaymentStatus.RePaid)
                _hScndTime = true;

            if (!_hScndTime)
            {
                var pwd = new frmCheckPwd { operNo = 3 };
                pwd.ShowDialog();
                if (!pwd.Iscorrect) return;

                ResetOrderColors();
                SetBtnActive(btnHosting);
                _hScndTime             = true;
                txtCashpay.Text        = "0";
                txtNetwork.Text        = "0";
                txtrecrem1.Text        = "0";
                _payment.PayType       = 5;
                Invoic.PayType         = 5;
                _payment.CashPayment   = 0;
                _payment.MadaPayment   = 0;
                _payment.Remainder     = 0;
                Invoic.InvDiscount     = Invoic.SumPrice;
                CalcDiff();
            }
            else
            {
                SetBtnInactive(btnHosting);
                _hScndTime         = false;
                Invoic.InvDiscount = 0;
            }
        }

        #endregion

        #region Same Button

        private void btnSame_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(lblNetVal.Text, out double net);
            double.TryParse(txtRahn.Text,   out double rahn);
            double remaining = net - Invoic.Paid + rahn;

            if (_payment.PayType == 1)
            {
                txtCashpay.Text      = remaining.ToString();
                txtNetwork.Text      = "0";
                _payment.CashPayment = remaining;
                _payment.MadaPayment = 0;
                CalcDiff();
                _ = SaveRes();
            }
            else if (_payment.PayType == 2 || _payment.PayType == 3)
            {
                txtCashpay.Text      = "0";
                txtNetwork.Text      = remaining.ToString();
                _payment.MadaPayment = remaining;
                _payment.CashPayment = 0;
                CalcDiff();
                _ = SaveRes();
            }
            else if (_payment.PayType == 4)
            {
                DXMessageBox.Show("يجب اختيار طريقة دفع أخرى",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCashpay.Text      = "0";
                txtNetwork.Text      = "0";
                _payment.MadaPayment = 0;
                _payment.CashPayment = 0;
            }
        }

        #endregion

        #region TextChanged Events

        private void txtCashpay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtCashpay.Text))
            {
                double.TryParse(txtCashpay.Text, out double cashVal);
                _payment.CashPayment = cashVal;

                if (_payment.CashPayment > 0 && _payment.MadaPayment > 0)
                    _payment.PayType = 4;

                if (_payment.PayType == 4 && cashVal > 0)
                {
                    _payment.MadaPayment = GetNetVal() - cashVal - Invoic.Paid;
                    txtNetwork.Text      = _payment.MadaPayment.ToString();
                }

                CalcDiff();
            }
            else
            {
                _payment.CashPayment = 0;
            }
        }

        private void txtNetwork_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtNetwork.Text))
            {
                double.TryParse(txtNetwork.Text, out double netPay);
                _payment.MadaPayment = netPay;
                CalcDiff();
            }
            else
            {
                _payment.MadaPayment = 0;
            }
        }

        #endregion

        #region Discount TextBox Events

        private void txtInvDiscVal_Leave(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal maxDisc = LoadUserMaxDiscount();
                double.TryParse(txtInvDiscount.Text, out double dVal);
                if (dVal > (double)maxDisc)
                    DXMessageBox.Show("عذرًا لقد تجاوزت حد الخصم المسموح به",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    CalcDiscount();
            }
            catch { }
        }

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) txtInvDiscVal_Leave(sender, null);
        }

        private void txtInvDiscPerc_Leave(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal maxPerc = LoadUserMaxDiscountPerc();
                double.TryParse(txtDiscoPerc.Text, out double pVal);
                if (pVal > (double)maxPerc)
                    DXMessageBox.Show("عذرًا لقد تجاوزت حد الخصم المسموح به",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    CalcDiscountPerc();
            }
            catch { }
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) txtInvDiscPerc_Leave(sender, null);
        }

        private void txtRahn_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRahn.Text))
                txtRahn.Text = "0";
            CalcDiff();
        }

        private void txtRahn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (string.IsNullOrWhiteSpace(txtRahn.Text))
                    txtRahn.Text = "0";
                CalcDiff();
            }
        }

        #endregion

        #region Cancel

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Load Max Discount

        private decimal LoadUserMaxDiscount()
        {
            try
            {
                if (_conn.State != ConnectionState.Open) _conn.Open();
                using (var da = new SqlDataAdapter(
                    $"SELECT MaxDicount FROM OperMaxDiscount WHERE emp={MainClass.EmpNo}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0
                        ? Convert.ToDecimal(dt.Rows[0]["MaxDicount"])
                        : 0;
                }
            }
            catch { return 0; }
        }

        private decimal LoadUserMaxDiscountPerc()
        {
            try
            {
                if (_conn.State != ConnectionState.Open) _conn.Open();
                using (var da = new SqlDataAdapter(
                    $"SELECT MaxDicountParcent FROM OperMaxDiscount WHERE emp={MainClass.EmpNo}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0
                        ? Convert.ToDecimal(dt.Rows[0]["MaxDicountParcent"])
                        : 0;
                }
            }
            catch { return 0; }
        }

        #endregion

        #region Helpers

        private double GetNetVal()
        {
            double.TryParse(lblNetVal.Text, out double v);
            return v;
        }

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