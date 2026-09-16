using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDesignIcon : ThemedWindow
    {
        #region Constructor

        public frmDesignIcon()
        {
            InitializeComponent();
            WireUpEvents();
        }

        #endregion

        #region Event Wiring

        private void WireUpEvents()
        {
            CheckBox6.Checked += CheckBox_Changed;
            CheckBox6.Unchecked += CheckBox_Changed;
            CheckBox8.Checked += CheckBox_Changed;
            CheckBox8.Unchecked += CheckBox_Changed;
            CheckBox10.Checked += CheckBox_Changed;
            CheckBox10.Unchecked += CheckBox_Changed;

            Button1.Click += BtnApply_Click;
            Button2.Click += (s, e) => Close();
        }

        #endregion

        #region Events

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // منطق التغيير - يمكن توسيعه حسب المتطلبات
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            // تطبيق الإعدادات المحددة
            DXMessageBox.Show("✅ تم تطبيق الإعدادات",
                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}