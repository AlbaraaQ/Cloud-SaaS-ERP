using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCompanions : Window
    {
        #region ── Public Fields ──────────────────────────────

        public bool ISdone;
        public bool isfirst;
        public bool AllowChar;
        public bool is_close;

        #endregion

        #region ── Private Fields ─────────────────────────────

        private string calcFunc;
        private bool hasDecimal;
        private double valHolder1;
        private double valHolder2;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCompanions()
        {
            InitializeComponent();

            ISdone = false;
            isfirst = true;
            AllowChar = false;
            is_close = false;
            calcFunc = string.Empty;
            hasDecimal = false;
            valHolder1 = 0.0;
            valHolder2 = 0.0;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Frm_Calculator_Load(object sender, RoutedEventArgs e)
        {
            txtCompanionsNo.Focus();
        }

        private void Frm_Calculator_Click(object sender, MouseButtonEventArgs e)
        {
            txtCompanionsNo.Focus();
        }

        private void Frm_Calculator_KeyDown(object sender, KeyEventArgs e)
        {
            txtCompanionsNo.Focus();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Confirm / Close ────────────────────────────

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            ISdone = true;

            if (AllowChar)
            {
                if (!string.IsNullOrWhiteSpace(txtCompanionsNo.Text))
                {
                    Properties.Settings.Default.Barcode =
                        Convert.ToChar(txtCompanionsNo.Text);
                    Properties.Settings.Default.Save();
                }

                Close();
            }
            else
            {
                ok2();
            }
        }

        public void ok2()
        {
            if (string.IsNullOrWhiteSpace(txtCompanionsNo.Text))
            {
                MessageBox.Show(
                    "من فضلك ادخل الكمية",
                    "تحذير",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else if (decimal.TryParse(txtCompanionsNo.Text, out decimal qty) &&
                     qty >= 0m &&
                     qty <= 1000000m)
            {
                Properties.Settings.Default.QTY = qty;
                Properties.Settings.Default.Save();
                Close();
            }
            else
            {
                MessageBox.Show(
                    "من فضلك ادخل أرقام فقط",
                    "تحذير",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            is_close = true;
            Hide();
        }

        #endregion

        #region ── Number Pad Helpers ─────────────────────────

        private void AppendValue(string value)
        {
            if (isfirst)
            {
                txtCompanionsNo.Text = string.Empty;
            }

            txtCompanionsNo.Text += value;
            txtCompanionsNo.Focus();
            txtCompanionsNo.CaretIndex = txtCompanionsNo.Text.Length;
            isfirst = false;
        }

        #endregion

        #region ── Number Pad Events ──────────────────────────

        private void cmd135_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd135.Content?.ToString() ?? "1");
        }

        private void cmd2_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd2.Content?.ToString() ?? "2");
        }

        private void cmd3_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd3.Content?.ToString() ?? "3");
        }

        private void cmd4_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd4.Content?.ToString() ?? "4");
        }

        private void cmd5_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd5.Content?.ToString() ?? "5");
        }

        private void cmd6_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd6.Content?.ToString() ?? "6");
        }

        private void cmd7_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd7.Content?.ToString() ?? "7");
        }

        private void cmd8_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd8.Content?.ToString() ?? "8");
        }

        private void cmd9_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd9.Content?.ToString() ?? "9");
        }

        private void cmd0_Click(object sender, RoutedEventArgs e)
        {
            AppendValue(cmd0.Content?.ToString() ?? "0");
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtCompanionsNo.Text = string.Empty;
            valHolder1 = 0.0;
            valHolder2 = 0.0;
            calcFunc = string.Empty;
            hasDecimal = false;
            isfirst = true;
            txtCompanionsNo.Focus();
        }

        #endregion

        #region ── Text Input Events ──────────────────────────

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                ok2();
                e.Handled = true;
            }
        }

        private void TextBox1_KeyPress(object sender, TextCompositionEventArgs e)
        {
            if (!AllowChar)
            {
                foreach (char inputChar in e.Text)
                {
                    if (!char.IsDigit(inputChar))
                    {
                        MessageBox.Show(
                            "الحقل لا يقبل إلا الأرقام فقط",
                            "تنبيه",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        e.Handled = true;
                        txtCompanionsNo.Focus();
                        return;
                    }
                }
            }
        }

        #endregion
    }
}