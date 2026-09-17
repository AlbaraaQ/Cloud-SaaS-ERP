using DevExpress.Xpf.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSecurityPnl : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Properties

        public bool isfirst    = true;
        public bool AllowChar  = false;
        public bool Secured    = false;
        public bool IsExit     = false;
        public bool is_close   = false;

        #endregion

        #region Constructor

        public frmSecurityPnl()
        {
            InitializeComponent();
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtPass.Focus();
        }

        #endregion

        #region Helpers

        private string GetPassword()
            => txtPass.Password;

        private void AppendDigit(string digit)
        {
            if (isfirst)
            {
                txtPass.Password = "";
                isfirst          = false;
            }
            txtPass.Password += digit;
            txtPass.Focus();
        }

        public void ok2()
        {
            try
            {
                string pwd = GetPassword();

                if (string.IsNullOrEmpty(pwd))
                {
                    DXMessageBox.Show("من فضلك أدخل الرقم السري.", "تحذير",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (decimal.TryParse(pwd, out decimal val) &&
                    val >= 0 && val <= 1000000)
                {
                    var frmUserLogin = new frmUserLogin();
                    frmUserLogin.Activate();
                    frmUserLogin.SecureCode = Convert.ToInt32(val);
                    frmUserLogin.LoadData();
                    frmUserLogin.Login(2);

                    if (Secured)
                    {
                        IsExit = false;
                        txtPass.Password = "";
                        Close();
                    }
                    else
                    {
                        txtPass.Password = "";
                    }
                }
                else
                {
                    DXMessageBox.Show("من فضلك أدخل أرقام فقط.", "تحذير",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch
            {
                DXMessageBox.Show("من فضلك أدخل أرقام فقط.", "تحذير",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Button Events

        private void NumBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                AppendDigit(btn.Content.ToString() ?? "");
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtPass.Password = "";
            txtPass.Focus();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (AllowChar)
            {
                Properties.Settings.Default.Barcode =
                    GetPassword().Length > 0
                        ? GetPassword()[0]
                        : '\0';
                Properties.Settings.Default.Save();
                Close();
            }
            else
            {
                ok2();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var Home = new Home();
            Home.IsCashierClosed = true;
            Home.Close();
        }

        private void LinkLabel1_Click(object sender, MouseButtonEventArgs e)
        {
            Topmost  = false;
            IsExit   = true;
            Hide();
        }

        #endregion

        #region KeyDown Events

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            txtPass.Focus();
        }

        private void txtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ok2();
        }

        #endregion
    }
}