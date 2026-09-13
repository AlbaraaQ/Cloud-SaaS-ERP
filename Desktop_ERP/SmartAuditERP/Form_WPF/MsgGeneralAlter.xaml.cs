using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AuditorAPI.Models;

namespace SmartAuditERP.Form_WPF
{
    public partial class MsgGeneralAlter : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public PaymentStatus Action;
        private DispatcherTimer timer1;

        #endregion

        #region Constructor

        public MsgGeneralAlter()
        {
            InitializeComponent();
            Action = PaymentStatus.Paid;

            timer1 = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // لا شيء مطلوب
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Return)
                this.Close();
        }

        #endregion

        #region Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            this.Close();
        }

        #endregion

        #region Buttons

        private void btnNon_Click(object sender, RoutedEventArgs e)
        {
            timer1.Stop();
            this.Close();
        }

        #endregion
    }
}