using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class Frm_Calculator : Window
    {
        #region Fields

        public bool isfirst;
        public bool AllowChar;
        private string calcFunc;
        private bool hasDecimal;
        private double valHolder1;
        private double valHolder2;
        public bool is_close;
        private DispatcherTimer Timer1;

        #endregion

        #region Constructor

        public Frm_Calculator()
        {
            InitializeComponent();

            isfirst = true;
            AllowChar = false;
            is_close = false;
            calcFunc = string.Empty;
            hasDecimal = false;
            valHolder1 = 0.0;
            valHolder2 = 0.0;

            Timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            Timer1.Tick += Timer1_Tick;

            Loaded += Frm_Calculator_Load;
            MouseDown += Frm_Calculator_Click;
        }

        #endregion

        #region Event Handlers - Window

        private void Frm_Calculator_Load(object sender, RoutedEventArgs e)
        {
            TextBox1.Focus();
            Timer1.Start();
        }

        private void Frm_Calculator_Click(object sender, MouseButtonEventArgs e)
        {
            TextBox1.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox1.Focus();
        }

        #endregion

        #region Event Handlers - Number Buttons

        private void cmd0_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("0");
        }

        private void cmd135_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("1");
        }

        private void cmd2_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("2");
        }

        private void cmd3_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("3");
        }

        private void cmd4_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("4");
        }

        private void cmd5_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("5");
        }

        private void cmd6_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("6");
        }

        private void cmd7_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("7");
        }

        private void cmd8_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("8");
        }

        private void cmd9_Click(object sender, RoutedEventArgs e)
        {
            AppendNumber("9");
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            if (!TextBox1.Text.Contains("."))
            {
                TextBox1.Text += ".";
                TextBox1.Focus();
            }
        }

        #endregion

        #region Event Handlers - Action Buttons

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            TextBox1.Text = "0";
            valHolder1 = 0.0;
            valHolder2 = 0.0;
            calcFunc = string.Empty;
            hasDecimal = false;
            isfirst = true;
            TextBox1.Focus();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (AllowChar)
            {
                if (!string.IsNullOrEmpty(TextBox1.Text))
                {
                    Properties.Settings.Default.Barcode = Convert.ToChar(TextBox1.Text);
                    Properties.Settings.Default.Save();
                    Close();
                }
            }
            else
            {
                ok2();
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            is_close = true;
            Hide();
        }

        #endregion

        #region Event Handlers - TextBox

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                ok2();
                e.Handled = true;
            }
        }

        private void TextBox1_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!AllowChar)
            {
                // Allow only numbers and decimal point
                if (!char.IsDigit(e.Text, 0) && e.Text != ".")
                {
                    MessageBox.Show("⚠️ الحقل لا يقبل إلا الأرقام فقط", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Handled = true;
                    TextBox1.Focus();
                    return;
                }

                // Prevent multiple decimal points
                if (e.Text == "." && TextBox1.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        #endregion

        #region Event Handlers - Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            TextBox1.Focus();
            Timer1.Stop();
        }

        #endregion

        #region Helper Methods

        private void AppendNumber(string number)
        {
            if (isfirst || TextBox1.Text == "0")
            {
                TextBox1.Text = number;
                isfirst = false;
            }
            else
            {
                TextBox1.Text += number;
            }
            TextBox1.Focus();
        }

        public void ok2()
        {
            if (string.IsNullOrWhiteSpace(TextBox1.Text))
            {
                MessageBox.Show("⚠️ من فضلك ادخل الكمية", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                decimal value = Convert.ToDecimal(TextBox1.Text);

                if (value >= 0 && value <= 1000000)
                {
                    Properties.Settings.Default.QTY = value;
                    Properties.Settings.Default.Save();
                    Close();
                }
                else
                {
                    MessageBox.Show("⚠️ الرقم يجب أن يكون بين 0 و 1,000,000", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TextBox1.SelectAll();
                    TextBox1.Focus();
                }
            }
            catch
            {
                MessageBox.Show("⚠️ من فضلك ادخل أرقام صحيحة فقط", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TextBox1.SelectAll();
                TextBox1.Focus();
            }
        }

        #endregion
    }
}