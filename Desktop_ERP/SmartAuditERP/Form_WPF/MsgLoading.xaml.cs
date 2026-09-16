using System;
using System.Windows;
using System.Windows.Threading;

namespace SmartAuditERP.Form_WPF
{
    public partial class MsgLoading : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private DispatcherTimer Timer1;

        #endregion

        #region Constructor

        public MsgLoading()
        {
            InitializeComponent();

            Timer1 = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            Timer1.Tick += Timer1_Tick;
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer1.Start();
            UpdateProgress();
        }

        #endregion

        #region Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (ProgressBar1.Value < 100)
            {
                ProgressBar1.Value++;
                Label1.Text = $"{(int)ProgressBar1.Value}%";
            }
            else
            {
                Timer1.Stop();
            }
        }

        #endregion
    }
}