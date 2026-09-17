using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSerial : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int    features = 0;
        private string serial   = "";

        #endregion

        #region Constructor

        public frmSerial()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var regKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", writable: true);
                if (regKey != null)
                {
                    regKey.GetValue("Serial")?.ToString()?.Split('-');
                    txt1.Text = "****";
                    txt2.Text = "****";
                    txt3.Text = "****";
                    txt4.Text = "****";
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region Helpers

        public long MUL(long no, int x)
            => checked(no * x);

        public long GetHasCode(string strObj)
        {
            if (string.IsNullOrWhiteSpace(strObj)) return 0L;

            long num = strObj.Length;
            for (int i = 0; i < strObj.Length; i++)
                num = checked(num * 3 - num + strObj[i]);
            return num;
        }

        private void CheckSerial()
        {
            
        }

        #endregion

        #region TextBox Events

        private void txt1_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt1.Text)) return;

                // إذا كان السيريال كاملاً مع -
                if (txt1.Text.Contains("-"))
                {
                    string[] parts = txt1.Text.Split('-');
                    if (parts.Length >= 4)
                    {
                        txt1.Text = parts[0].Trim();
                        txt2.Text = parts[1].Trim();
                        txt3.Text = parts[2].Trim();
                        txt4.Text = parts[3].Trim();
                    }
                }

                if (txt1.Text.Length == 4) txt2.Focus();
            }
            catch { /* تجاهل */ }
        }

        private void txt2_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txt2.Text) && txt2.Text.Length == 4)
                    txt3.Focus();
            }
            catch { /* تجاهل */ }
        }

        private void txt3_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txt3.Text) && txt3.Text.Length == 4)
                    txt4.Focus();
            }
            catch { /* تجاهل */ }
        }

        private void txt4_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txt4.Text) && txt4.Text.Length == 4)
                    btnActivate_Click(null, null);
            }
            catch { /* تجاهل */ }
        }

        private void SerialBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var next = sender is TextBox tb
                    ? tb.TabIndex < 3
                        ? FindName($"txt{tb.TabIndex + 2}") as TextBox
                        : null
                    : null;

                if (next != null) next.Focus();
                else btnActivate_Click(null, null);
            }
        }

        #endregion

        #region Button Events

        private void btnActivate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txt1.Text) ||
                    string.IsNullOrWhiteSpace(txt2.Text) ||
                    string.IsNullOrWhiteSpace(txt3.Text) ||
                    string.IsNullOrWhiteSpace(txt4.Text))
                {
                    DXMessageBox.Show("أدخل السيريال كاملاً.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Cursor = Cursors.Wait;
                CheckSerial();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
            => Close();

        private void LinkLabel1_Click(object sender, MouseButtonEventArgs e)
        {
            var form = new frmBuyApp();
            form.ShowDialog();
        }

        #endregion

        #region Window KeyDown

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var focused = Keyboard.FocusedElement as UIElement;
                focused?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        #endregion
    }
}