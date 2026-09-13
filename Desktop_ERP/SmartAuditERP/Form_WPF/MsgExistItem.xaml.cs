using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class MsgExistItem : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public int Action = 0;

        #endregion

        #region Constructor

        public MsgExistItem()
        {
            InitializeComponent();
            Action = 0;
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // لا شيء مطلوب عند التحميل
        }

        #endregion

        #region Button Events

        private void btnAddquantity_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            Action = 2;
            this.Hide();
        }

        private void btnNewTirm_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            Action = 1;
            this.Hide();
        }

        private void btnNon_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = false;
            Action = 3;
            this.Hide();
        }

        #endregion
    }
}