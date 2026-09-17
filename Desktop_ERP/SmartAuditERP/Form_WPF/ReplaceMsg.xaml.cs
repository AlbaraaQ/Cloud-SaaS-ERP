using System.Windows;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة اختيار نوع الاستبدال
    /// Type = 0: لم يتم الاختيار
    /// Type = 1: ضمن خطة الدور
    /// Type = 2: خارج خطة الدور
    /// </summary>
    public partial class ReplaceMsg : ThemedWindow
    {
        #region Fields

        public int Type { get; private set; } = 0;

        #endregion

        #region Constructor

        public ReplaceMsg()
        {
            InitializeComponent();
        }

        #endregion

        #region Button Events

        private void btnLinked_Click(object sender, RoutedEventArgs e)
        {
            Type = 1;
            this.Hide();
        }

        private void btnNotLinked_Click(object sender, RoutedEventArgs e)
        {
            Type = 2;
            this.Hide();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Type = 0;
            this.Hide();
        }

        #endregion
    }
}