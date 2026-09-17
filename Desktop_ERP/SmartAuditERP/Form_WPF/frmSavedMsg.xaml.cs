using System.Windows;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة رسالة "تم الحفظ" مع خيارات: جديد / استمرار / خروج
    /// </summary>
    public partial class frmSavedMsg : ThemedWindow
    {
        #region Fields

        /// <summary>
        /// القيمة المحددة من المستخدم:
        /// 0 = لم يضغط شيء
        /// 1 = جديد
        /// 2 = استمرار
        /// 3 = خروج
        /// </summary>
        public int Pressed { get; private set; }

        #endregion

        #region Constructor

        public frmSavedMsg()
        {
            InitializeComponent();
            Pressed = 0;
        }

        #endregion

        #region Button Events

        /// <summary>جديد - إضافة سجل جديد</summary>
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            Pressed = 1;
            this.Close();
        }

        /// <summary>استمرار - البقاء في السجل الحالي</summary>
        private void btnContu_Click(object sender, RoutedEventArgs e)
        {
            Pressed = 2;
            this.Close();
        }

        /// <summary>خروج - إغلاق النافذة الرئيسية</summary>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Pressed = 3;
            this.Close();
        }

        #endregion
    }
}