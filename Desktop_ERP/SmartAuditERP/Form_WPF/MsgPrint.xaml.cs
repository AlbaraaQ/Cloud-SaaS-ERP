using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class MsgPrint : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public int Action = 0;

        #endregion

        #region Constructor

        public MsgPrint()
        {
            InitializeComponent();
            Action = 0;
        }

        #endregion

        #region Button Events

        private void btnNon_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPerview_Click(object sender, RoutedEventArgs e)
        {
            Action = 2;
            this.Hide();
        }

        private void btnPrintA4_Click(object sender, RoutedEventArgs e)
        {
            Action = 3;
            this.Hide();
        }

        private void btnPrintPos_Click(object sender, RoutedEventArgs e)
        {
            Action = 1;
            this.Hide();
        }

        private void btnPrintPDF_Click(object sender, RoutedEventArgs e)
        {
            Action = 4;
            this.Hide();
        }

        #endregion
    }
}