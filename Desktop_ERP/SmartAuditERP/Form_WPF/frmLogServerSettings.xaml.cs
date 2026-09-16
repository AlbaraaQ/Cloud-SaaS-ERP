using DevExpress.Xpf.Core;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmLogServerSettings : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public int  operNo    { get; set; }
        public bool Iscorrect { get; set; } = false;
        public int  CheckType { get; set; } = 1;
        public bool isfirst   { get; set; } = true;
        public bool AllowChar { get; set; } = false;
        public bool Secured   { get; set; } = false;

        #endregion

        #region Constructor

        public frmLogServerSettings()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();
        }

        #endregion

        #region Check

        private void Check()
        {
            try
            {
                string passwordText = txtPass.Password;
                string usernameText = txtUser.Text.Trim();

                if (string.IsNullOrEmpty(passwordText))
                {
                    DXMessageBox.Show("يرجى إدخال كلمة المرور", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.Equals(passwordText, "auditor", StringComparison.Ordinal) &&
                    string.Equals(usernameText, "auditor", StringComparison.Ordinal))
                {
                    Iscorrect = true;
                    Close();
                }
                else
                {
                    DXMessageBox.Show("ليس لديك صلاحية، أو أن كلمة المرور المدخلة غير صحيحة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ Error details: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Events

        private void txtPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Check();
        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            Check();
        }

        #endregion
    }
}