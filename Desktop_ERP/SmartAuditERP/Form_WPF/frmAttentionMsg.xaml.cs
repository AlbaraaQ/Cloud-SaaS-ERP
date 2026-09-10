using System.Windows;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة رسالة التنبيه مع تأكيد - frmAttentionMsg
    /// </summary>
    public partial class frmAttentionMsg : ThemedWindow
    {
        #region Fields

        /// <summary>
        /// يحدد ما إذا كان المستخدم قد ضغط تأكيد
        /// </summary>
        public bool IsSure;

        #endregion

        #region Constructor

        public frmAttentionMsg()
        {
            IsSure = false;
            InitializeComponent();
        }

        #endregion

        #region Button Events

        /// <summary>
        /// إغلاق النافذة بدون تأكيد (بديل btnClose_Click)
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// تأكيد الإجراء (بديل btnInsure_Click)
        /// </summary>
        private void btnInsure_Click(object sender, RoutedEventArgs e)
        {
            IsSure = true;
            Hide();
        }

        #endregion
    }
}