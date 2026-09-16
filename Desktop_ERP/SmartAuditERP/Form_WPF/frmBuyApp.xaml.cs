using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmBuyApp : Window
    {
        #region ── Fields ─────────────────────────────────────

        private string txt1;
        private string txt2;
        private string FoundationInf;
        private SqlConnection conn;
        private string DeviceMAC;
        private string foundationName;
        private DispatcherTimer timer1;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmBuyApp()
        {
            txt1 = "";
            txt2 = "";
            FoundationInf = "";
            conn = MainClass.ConnObj();
            DeviceMAC = "";
            foundationName = "";

            InitializeComponent();

            timer1 = new DispatcherTimer();
            timer1.Interval = TimeSpan.FromSeconds(1);
            timer1.Tick += timer1_Tick;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void frmBuyApp_Load(object sender, RoutedEventArgs e)
        {
            timer1.Start();

            Thread loadingThread = new Thread(loadinfo);
            loadingThread.IsBackground = true;
            loadingThread.Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (timer1 != null)
            {
                timer1.Stop();
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void btnWindowClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Public Methods ─────────────────────────────

        public long GetHasCode(string Strobj)
        {
            if (string.IsNullOrWhiteSpace(Strobj))
            {
                return 0L;
            }

            long hashCode = Strobj.Length;

            for (int characterIndex = 0; characterIndex < Strobj.Length; characterIndex++)
            {
                hashCode = hashCode * 3 - hashCode + Convert.ToInt32(Strobj[characterIndex]);
            }

            return hashCode;
        }

        #endregion

        #region ── Device Info Loading ────────────────────────

        private void loadinfo()
        {
            try
            {
                ManagementObjectSearcher diskDriveSearcher =
                    new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                string firstDiskSerial = string.Empty;

                foreach (ManagementObject diskObject in diskDriveSearcher.Get())
                {
                    if (diskObject["SerialNumber"] != null)
                    {
                        firstDiskSerial = diskObject["SerialNumber"].ToString();
                        break;
                    }
                }

                DeviceMAC = string.IsNullOrWhiteSpace(firstDiskSerial)
                    ? "UNKNOWN_DEVICE"
                    : firstDiskSerial.Trim();

                long deviceHash = Math.Abs(GetHasCode(DeviceMAC));
                long appHash = Math.Abs(GetHasCode("SmartAuditERP"));
                long seedHash = Math.Abs(GetHasCode("StockVB-EngDev74"));

                string activationCode = (deviceHash + seedHash + appHash).ToString();

                StringBuilder descriptionBuilder = new StringBuilder();
                descriptionBuilder.AppendLine(MainClass.AppNameAR + " ( محاسبة - إدارة مخازن ومستودعات )");
                descriptionBuilder.AppendLine("صمم لتلبية كافة احتياجات منشأتك أياً كان نشاطها التجاري سواء كانت تجارة عامة جملة أو تجزئة أو بقالة أو مطعم");
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine("لشراء البرنامج وتفعيله على هذا الجهاز قم بالاتصال بـ");
                descriptionBuilder.AppendLine("دار اتش لتقنية المعلومات");
                descriptionBuilder.AppendLine("الرقم الموحد/ 920009656");
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine("www.dhit.com.sa");
                descriptionBuilder.AppendLine("sales@dhit.com.sa");
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine("وأرسل كود التفعيل الآتي");

                txt1 = descriptionBuilder.ToString();
                txt2 = activationCode;
            }
            catch
            {
            }
        }

        #endregion

        #region ── Foundation Info ────────────────────────────

        private void loadFoundationInf()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select * from Foundation", conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                {
                    var frmFonudation2 = new frmFonudation();
                    MainClass.ApplyPermissionToForm(frmFonudation2);
                    MainClass.DoApplyUserSett(frmFonudation2);
                    frmFonudation2.Topmost = true;
                    frmFonudation2.ShowDialog();
                    return;
                }

                DataRow foundationRow = dataTable.Rows[0];

                FoundationInf = string.Empty;
                foundationName = foundationRow["nameA"]?.ToString() ?? string.Empty;

                StringBuilder foundationBuilder = new StringBuilder();
                foundationBuilder.AppendLine("Device_Code:" + txt2);
                foundationBuilder.AppendLine("DeviceID:" + DeviceMAC);
                foundationBuilder.AppendLine("NameAr:" + (foundationRow["nameA"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("NameEn:" + (foundationRow["nameE"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("FieldAr:" + (foundationRow["FieldA"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("FieldEn:" + (foundationRow["FieldE"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Tel:" + (foundationRow["Tel"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Mobile:" + (foundationRow["Mobile"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Email:" + (foundationRow["Email"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Website:" + (foundationRow["website"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Country:" + (foundationRow["country"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("City:" + (foundationRow["city"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Address:" + (foundationRow["Address"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("CR_No.:" + (foundationRow["bsn_no"]?.ToString() ?? string.Empty));
                foundationBuilder.AppendLine("Tax_No.:" + (foundationRow["tax_no"]?.ToString() ?? string.Empty));

                FoundationInf = foundationBuilder.ToString();
            }
            catch
            {
            }
        }

        #endregion

        #region ── Timer ──────────────────────────────────────

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txt2))
            {
                textBox2.Text = txt1;
                textBox1.Text = txt2;
                activationCodePanel.Visibility = Visibility.Visible;
                textBox1.Visibility = Visibility.Visible;
                timer1.Stop();
            }
            else
            {
                if (textBox2.Text.EndsWith(" . . . ."))
                {
                    textBox2.Text = "⏳ انتظر جاري التحميل";
                }
                else
                {
                    textBox2.Text += " .";
                }
            }
        }

        #endregion

        #region ── Export ─────────────────────────────────────

        private void btnExportdata_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("select * from Foundation", conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                {
                    frmAttentionMsg frmAttentionMsg2 = new frmAttentionMsg();
                    frmAttentionMsg2.btnInsure.IsEnabled = true;
                    frmAttentionMsg2.lblmsg.Text = "يرجى القيام بإدخال بيانات المنشأة أولاً";
                    frmAttentionMsg2.ShowDialog();

                    if (frmAttentionMsg2.IsSure)
                    {
                        loadFoundationInf();
                    }

                    return;
                }

                string confirmMessage = "أرسل ملف الترخيص الموجود في سطح المكتب للدعم الفني لترخيص البرنامج";
                frmAttentionMsg frmAttentionMsg3 = new frmAttentionMsg();
                frmAttentionMsg3.lblmsg.Text = confirmMessage;
                frmAttentionMsg3.btnInsure.IsEnabled = true;
                frmAttentionMsg3.ShowDialog();

                if (frmAttentionMsg3.IsSure)
                {
                    loadFoundationInf();

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    string safeFoundationName = string.IsNullOrWhiteSpace(foundationName) ? "License" : foundationName;

                    foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
                    {
                        safeFoundationName = safeFoundationName.Replace(invalidFileNameChar, '_');
                    }

                    string filePath = Path.Combine(desktopPath, safeFoundationName + " ملف الترخيص.txt");

                    File.WriteAllText(filePath, FoundationInf, Encoding.UTF8);

                    MessageBox.Show(
                        "تم تصدير ملف الترخيص بنجاح ✅",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطأ أثناء تصدير بيانات الترخيص" + Environment.NewLine + ex.Message,
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}