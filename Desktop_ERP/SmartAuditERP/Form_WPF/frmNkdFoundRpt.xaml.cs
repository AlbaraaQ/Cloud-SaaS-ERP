using DevExpress.Xpf.Core;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmNkdFoundRpt : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Constructor

        public frmNkdFoundRpt()
        {
            InitializeComponent();
        }

        #endregion

        #region Buttons

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmActsInvTotal form = new frmActsInvTotal();
                form.Show();
                form.Activate();
            }
            catch (System.Exception ex)
            {
                DXMessageBox.Show("خطأ" + System.Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmNkdTotSalePurch form = new frmNkdTotSalePurch();
                form.Show();
                form.Activate();
            }
            catch (System.Exception ex)
            {
                DXMessageBox.Show("خطأ" + System.Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}