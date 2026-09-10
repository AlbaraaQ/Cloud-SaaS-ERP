using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmUpdate : ThemedWindow
    {
        #region Fields
       // https://github.com/AlbaraaQ/update_SmartAuditERP/releases/download/26011805/update_SmartAuditERP.zip
        private string FileName = "";
        private string UrlZipFile = "https://dhit.com.sa/wp-content/uploads/update.zip";
        private string date1 = DateTime.Now.ToString("yyyy.MM.dd");
        private string FolderUpdate;
        private string FileDownload;
        public string connString;
        public SqlConnection conn;
        private int _currentPage = 0;

        #endregion

        #region Constructor

        public FrmUpdate()
        {
            InitializeComponent();

            FolderUpdate = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                $"Update_{date1}");
            FileDownload = System.IO.Path.Combine(FolderUpdate, "newFileUpdate.zip");

            connString = $"server={MainClass.Server.Trim()};database=master;trusted_connection=True";
            conn = new SqlConnection(connString);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new Thread(LoadUpdateInfo).Start();

            // تحديد طريقة التحديث بناءً على الاتصال
            bool hasInternet = MainClass.CheckForInternetConnection();
            rbOnline.IsEnabled = hasInternet;
            if (!hasInternet)
            {
                rbManual.IsChecked = true;
            }
            else
            {
                rbOnline.IsChecked = true;
            }
        }

        #endregion

        #region Load Update Info

        private void LoadUpdateInfo()
        {
            try
            {
                // قراءة النسخة الحالية
                string versionFile = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Version.txt");

                if (File.Exists(versionFile))
                {
                    string content = File.ReadAllText(versionFile);
                    // تحليل JSON بسيط (يحتاج JavaScriptSerializer أو Newtonsoft.Json)
                    Dispatcher.Invoke(() =>
                    {
                        TxtCurrentVer.Text = "1.0.0"; // من الملف
                        TxtCurrentVerDate.Text = DateTime.Now.ToShortDateString();
                    });
                }

                // قراءة النسخة الحديثة من الإنترنت
                if (MainClass.CheckForInternetConnection())
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtOnlineVersion.Text = "جارِ التحقق...";
                        txtOnlineVersionDate.Text = "";
                    });
                }
            }
            catch { }
        }

        #endregion

        #region Wizard Navigation

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage == 0)
            {
                // من صفحة الترحيب إلى التحميل
                GoToPage(1);

                if (rbOnline.IsChecked == true)
                {
                    // تحميل أونلاين
                    CreateFolderUpdate();
                    DownloadFiles();
                }
                // إذا يدوي - انتظار اختيار الملف
            }
            else if (_currentPage == 1)
            {
                // من التحميل إلى الاكتمال
                if (rbManual.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(txtPath.Text))
                    {
                        DXMessageBox.Show("الرجاء اختيار الملف", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    FileDownload = txtPath.Text;
                    CreateFolderUpdate();
                    ReplaceFiles();
                }
                GoToPage(2);
                btnNext.Content = "✅ إنتهاء";
            }
            else if (_currentPage == 2)
            {
                // إنهاء
                this.Close();
            }
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
                GoToPage(_currentPage - 1);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void GoToPage(int pageIndex)
        {
            _currentPage = pageIndex;
            wizardPages.SelectedIndex = pageIndex;

            btnPrev.IsEnabled = pageIndex > 0;
            btnNext.IsEnabled = true;

            // تحديث نقاط التقدم
            dot1.Opacity = pageIndex == 0 ? 1.0 : 0.5;
            dot2.Opacity = pageIndex == 1 ? 1.0 : 0.5;
            dot3.Opacity = pageIndex == 2 ? 1.0 : 0.5;

            string[] titles = { "مرحباً", "جارِ تحميل البيانات", "اكتمل التحديث" };
            string[] descs =
            {
                "لتحديث البرنامج يجب أن يكون الجهاز متصلاً بالإنترنت",
                "يُرجى الانتظار حتى اكتمال التحميل",
                "سيتم إغلاق البرنامج لإتمام التحديث"
            };

            lblPageTitle.Text = titles[pageIndex];
            lblPageDesc.Text = descs[pageIndex];
        }

        #endregion

        #region RadioButton Events

        private void rbManual_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlManualPath != null)
                pnlManualPath.Visibility = Visibility.Visible;
        }

        private void rbManual_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pnlManualPath != null)
                pnlManualPath.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region File Operations

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
                FileDownload = FileName;
            }
        }

        private void DownloadFiles()
        {
            try
            {
                using var wc = new WebClient();
                wc.DownloadProgressChanged += (s, ev) =>
                    Dispatcher.Invoke(() => ProgressBar1.Value = ev.ProgressPercentage);
                wc.DownloadFileCompleted += (s, ev) =>
                    Dispatcher.Invoke(() =>
                    {
                        ReplaceFiles();
                        GoToPage(2);
                        btnNext.Content = "✅ إنتهاء";
                    });
                wc.DownloadFileAsync(new Uri(UrlZipFile), FileDownload);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التحميل: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReplaceFiles()
        {
            try
            {
                UtilitiesProj.Updates.unZipFile(FileDownload);
                DXMessageBox.Show("سيتم إغلاق البرنامج لإتمام عملية التحديث", "تحديث",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateLicenseEndDate();
                 Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe"));
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("حدث خطأ أثناء تحميل ملفات النسخة الجديدة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLicenseEndDate()
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                new SqlCommand(
                    "UPDATE License SET LicenseExpire = DATEADD(YEAR, 1, LicenseExpire)",
                    conn).ExecuteNonQuery();
                conn.Close();
            }
            catch { }
        }

        private void CreateFolderUpdate()
        {
            if (Directory.Exists(FolderUpdate))
                Directory.Delete(FolderUpdate, true);
            Directory.CreateDirectory(FolderUpdate);
            Directory.CreateDirectory(System.IO.Path.Combine(FolderUpdate, "Extract"));
        }

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