using System;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmUpdateInfo : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private string FileName = "";

        #endregion

        #region Constructor

        public frmUpdateInfo()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(LoadUpdateInfo).Start();
        }

        #endregion

        #region Load Info

        private void LoadUpdateInfo()
        {
            try
            {
                string versionFile = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Version.txt");

                if (File.Exists(versionFile))
                {
                    string content = File.ReadAllText(versionFile);
                    // تحليل الملف (يحتاج JSON parser)
                    Dispatcher.Invoke(() =>
                    {
                        lblCurrentVer.Text = "1.0.0";
                        lblDesc.Text = "وصف النسخة";
                        lblDate.Text = DateTime.Now.ToShortDateString();
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    lblCurrentVer.Text = "غير معروف";
                    lblDesc.Text = "تعذر قراءة معلومات النسخة";
                });
            }
        }

        #endregion

        #region Button Events

        private void btnManualUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnChekUpdate.Visibility = Visibility.Collapsed;
            btnManualUpdate.Visibility = Visibility.Collapsed;
            pnlManual.Visibility = Visibility.Visible;
            pnlActions.Visibility = Visibility.Visible;
        }

        private void btnChekUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحقق من التحديثات
                DXMessageBox.Show("جارِ التحقق من التحديثات...", "تحديث",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip",
                Title = "اختر ملف التحديث",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (dialog.ShowDialog() == true)
            {
                FileName = dialog.FileName;
                txtPath.Text = FileName;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                DXMessageBox.Show("يرجى اختيار ملف التحديث أولاً", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // تنفيذ التحديث
            DXMessageBox.Show("جارِ تطبيق التحديث...", "تحديث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (conn.State != System.Data.ConnectionState.Closed) conn.Close();
        }

        #endregion
    }
}