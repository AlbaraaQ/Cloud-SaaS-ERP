using System;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmShortCutInv : ThemedWindow
    {
        #region Fields

        public double InvVAT     { get; set; } = 0.0;
        public bool   InvIncluVAT { get; set; } = false;

        // القيم النهائية للاستخدام الخارجي
        public double ResultTotal    { get; private set; }
        public double ResultVAT      { get; private set; }
        public double ResultNet      { get; private set; }
        public double ResultDiscount { get; private set; }

        #endregion

        #region Constructor

        public FrmShortCutInv()
        {
            InitializeComponent();
        }

        #endregion

        #region Calculation

        private void Calc()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTotal.Text))    txtTotal.Text    = "0";
                if (string.IsNullOrWhiteSpace(txtNet.Text))      txtNet.Text      = "0";
                if (string.IsNullOrWhiteSpace(txtVAT.Text))      txtVAT.Text      = "0";
                if (string.IsNullOrWhiteSpace(txtSumVal.Text))   txtSumVal.Text   = "0";
                if (string.IsNullOrWhiteSpace(txtTotDiscount.Text)) txtTotDiscount.Text = "0";

                double.TryParse(txtSumVal.Text,    out double sumVal);
                double.TryParse(txtTotDiscount.Text, out double discount);

                double total = sumVal - discount;
                txtTotal.Text = total.ToString("N2");

                double vatAmount;
                if (InvIncluVAT)
                    vatAmount = total - Math.Round(total / (1.0 + InvVAT / 100.0), 3);
                else
                    vatAmount = Math.Round(total * (InvVAT / 100.0), 3);

                txtVAT.Text = vatAmount.ToString("N3");

                double net = total + vatAmount;
                txtNet.Text = net.ToString("N2");

                // حفظ القيم للاستخدام الخارجي
                ResultTotal    = total;
                ResultVAT      = vatAmount;
                ResultNet      = net;
                ResultDiscount = discount;
            }
            catch
            {
                // تجاهل أخطاء التحويل
            }
        }

        #endregion

        #region TextBox Events

        private void txtSumVal_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtSumVal.Text))
                Calc();
        }

        private void txtTotDiscount_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTotDiscount.Text))
                Calc();
        }

        private void txtSumVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                Calc();
                btnOk.Focus();
            }
        }

        private void txtDiscount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                Calc();
                btnOk.Focus();
            }
        }

        #endregion

        #region Button Events

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTotal.Text))
                Calc();
            this.Close();
        }

        #endregion
    }
}