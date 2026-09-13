using System.Windows;
using System.Windows.Input;
using AuditorAPI.Models;

namespace SmartAuditERP.Form_WPF
{
    public partial class MsgPayment : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public PaymentStatus Action;

        #endregion

        #region Constructor

        public MsgPayment()
        {
            InitializeComponent();
            Action = PaymentStatus.Paid;
        }

        #endregion

        #region Key Events

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        #endregion

        #region Button Events

        private void btnNon_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPaid_Click(object sender, RoutedEventArgs e)
        {
            Action = PaymentStatus.Paid;
            this.Hide();
        }

        private void btnNotPaid_Click(object sender, RoutedEventArgs e)
        {
            Action = PaymentStatus.Unpaid;
            this.Hide();
        }

        private void btnPaidPartially_Click(object sender, RoutedEventArgs e)
        {
            Action = PaymentStatus.PaidPartially;
            this.Hide();
        }

        private void btnRepaid_Click(object sender, RoutedEventArgs e)
        {
            Action = PaymentStatus.RePaid;
            this.Hide();
        }

        private void btnPostPaid_Click(object sender, RoutedEventArgs e)
        {
            Action = PaymentStatus.PostPaid;
            this.Hide();
        }

        #endregion
    }
}