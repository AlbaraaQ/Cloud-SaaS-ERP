using System;
using System.Windows;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmCalcVAT : Window
    {
        #region ── Public Fields ───────────────────────────────

        public int ItemVAT { get; set; } = 0;
        public double Price { get; set; } = 0.0;
        public double PriceWithOutVAT { get; set; } = 0.0;
        public double VAT { get; set; } = 0.0;

        #endregion

        #region ── Constructor ────────────────────────────────

        public FrmCalcVAT()
        {
            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Calculation ────────────────────────────────

        private void CalculateVAT()
        {
            if (string.IsNullOrWhiteSpace(txtPriceWithVAT.Text))
                return;

            if (string.IsNullOrWhiteSpace(txtPriceWithOutVAT.Text))
                txtPriceWithOutVAT.Text = "0";

            if (string.IsNullOrWhiteSpace(txtVAT.Text))
                txtVAT.Text = "0";

            if (!decimal.TryParse(txtPriceWithVAT.Text, out decimal priceWithVAT))
                return;

            double vatRate = 1.0 + ItemVAT / 100.0;

            VAT = (double)priceWithVAT - (double)priceWithVAT / vatRate;
            PriceWithOutVAT = (double)priceWithVAT - VAT;

            txtVAT.Text = VAT.ToString(Common.DigitsNo);
            txtPriceWithOutVAT.Text = PriceWithOutVAT.ToString(Common.DigitsNo);
        }

        #endregion

        #region ── Control Events ─────────────────────────────

        private void txtPriceWithVAT_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPriceWithVAT?.Text))
                CalculateVAT();
        }

        private void txtPriceWithVAT_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                CalculateVAT();
                btnOk.Focus();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPriceWithOutVAT.Text))
                Price = PriceWithOutVAT;

            Close();
        }

        #endregion
    }
}