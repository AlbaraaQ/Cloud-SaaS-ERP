using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UtilitiesProj;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmAbout : Window
    {
        #region Constructor
        public frmAbout()
        {
            InitializeComponent();
            this.Loaded += FrmAbout_Loaded;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// تحميل بيانات النافذة عند الفتح
        /// </summary>
        private void FrmAbout_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSystemInformation();
        }

        /// <summary>
        /// السماح بسحب النافذة من الجزء العلوي
        /// </summary>
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        /// <summary>
        /// إغلاق النافذة
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// فتح الموقع الإلكتروني
        /// </summary>
        private void Website_Click(object sender, MouseButtonEventArgs e)
        {
            OpenWebsite();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// تحميل جميع بيانات النظام (الشعار، الوصف، معلومات التواصل، الإصدار)
        /// </summary>
        private void LoadSystemInformation()
        {
            try
            {
                // استدعاء تهيئة بيانات المندوب من الكلاس Common
                Common.SalesmanSetting();

                // تحميل الشعار من Properties.Resources
                LoadLogoFromResources();

                // تحميل بيانات التواصل
                LoadContactInformation();

                // تحميل رقم الإصدار
                LoadVersionInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in LoadSystemInformation: " + ex.Message);
            }
        }

        /// <summary>
        /// تحميل الشعار من Properties.Resources وتحويله إلى BitmapSource
        /// </summary>
        private void LoadLogoFromResources()
        {
            try
            {
                // التحقق من وجود الشعار في الموارد
                if (Properties.Resources.SupportPn != null)
                {
                    // تحويل Bitmap (WinForms) إلى BitmapSource (WPF)
                    imgLogo.Source = ConvertBitmapToBitmapSource(Properties.Resources.SupportPn);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading logo: " + ex.Message);
                // في حالة فشل تحميل الشعار، نعرض نص بديل
            }
        }

        /// <summary>
        /// تحميل معلومات التواصل من الإعدادات المشتركة
        /// </summary>
        private void LoadContactInformation()
        {
            try
            {
                if (MainClass.MainBG != null)
                {
                    // تعيين وصف النظام
                    txtDescription.Text = "نظام المدقق للفوترة الإلكترونية و المحاسبة العامة و إدارة المستودعات و نقاط البيع تميزنا ( قطاع التجزئة و الجملة - السوبرماركت - المطاعم - المقاهي - المراسي )";

                    // تعيين العنوان
                    if (!string.IsNullOrEmpty(Common.AppSalesmaneAdress))
                        txtAddress.Text = Common.AppSalesmaneAdress;

                    // تعيين البريد الإلكتروني
                    if (!string.IsNullOrEmpty(Common.AppSalesmaneEmail))
                        txtEmail.Text = Common.AppSalesmaneEmail;

                    // تعيين رقم الجوال
                    if (!string.IsNullOrEmpty(Common.AppSalesmaneMobie))
                        txtMobile.Text = Common.AppSalesmaneMobie;

                    // تعيين الموقع الإلكتروني
                    if (!string.IsNullOrEmpty(Common.AppSalesmaneWebsite))
                        runWebsite.Text = Common.AppSalesmaneWebsite;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading contact info: " + ex.Message);
            }
        }

        /// <summary>
        /// تحميل معلومات الإصدار الحالي
        /// </summary>
        private void LoadVersionInfo()
        {
            try
            {
                var versionInfo = Common.CurrentVersion();
                if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.VersionText))
                {
                    txtVersion.Text = versionInfo.VersionText;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading version info: " + ex.Message);
            }
        }

        /// <summary>
        /// فتح الموقع الإلكتروني في المتصفح الافتراضي
        /// </summary>
        private void OpenWebsite()
        {
            try
            {
                string targetUrl = !string.IsNullOrEmpty(Common.AppSalesmaneWebsite)
                                   ? Common.AppSalesmaneWebsite
                                   : "https://dhit.com.sa";

                // التأكد من أن الرابط يبدأ بـ http أو https
                if (!targetUrl.StartsWith("http://") && !targetUrl.StartsWith("https://"))
                {
                    targetUrl = "https://" + targetUrl;
                }

                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("لا يمكن فتح المتصفح: " + ex.Message, "خطأ",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// دالة مساعدة: تحويل Bitmap (WinForms) إلى BitmapSource (WPF)
        /// تستخدم لعرض الصور من Properties.Resources في واجهات WPF
        /// </summary>
        /// <param name="bitmap">الصورة بصيغة System.Drawing.Bitmap</param>
        /// <returns>الصورة بصيغة System.Windows.Media.Imaging.BitmapSource</returns>
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                // حفظ الصورة في الذاكرة بصيغة PNG
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;

                // إنشاء BitmapImage من الذاكرة
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // تحسين الأداء

                return bitmapImage;
            }
        }
        #endregion

        #region Public Methods (للتوافق مع الكود القديم)
        // هذه الدوال موجودة للتوافق مع الكود الأصلي
        // تم تركها فارغة لأن الأحداث مربوطة مباشرة في XAML

        private void Label3_Click(object sender, RoutedEventArgs e)
        {
            // موجودة للتوافق مع الكود الأصلي
        }

        private void LinkLabel1_LinkClicked(object sender, RoutedEventArgs e)
        {
            // موجودة للتوافق مع الكود الأصلي
            OpenWebsite();
        }
        #endregion
    }
}