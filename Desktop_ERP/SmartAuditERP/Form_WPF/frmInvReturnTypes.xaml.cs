using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvReturnTypes : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int ReturnType;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvReturnTypes()
        {
            InitializeComponent();
            ReturnType = 0;
        }

        private void btnLinked_Click(object sender, RoutedEventArgs e)
        {
            ReturnType = 1;
            Hide();
        }

        private void btnNotLinked_Click(object sender, RoutedEventArgs e)
        {
            ReturnType = 2;
            Hide();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ReturnType = 0;
            Hide();
        }

        #endregion
    }
}