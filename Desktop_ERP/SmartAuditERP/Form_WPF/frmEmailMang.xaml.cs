using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEmailMang : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private int _smtpPort;
        private string _smtpServer;
        private string[] _attachmentPaths;

        #endregion

        #region Constructor

        public frmEmailMang()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _smtpPort = 587;
            _smtpServer = "smtp.gmail.com";

            Loaded += FrmEmailMang_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmEmailMang_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEmailSettings();
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnSend.Click += BtnSend_Click;
        }

        #endregion

        #region Load Settings

        private void LoadEmailSettings()
        {
            try
            {
                string userName = MainClass.UserName;

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM SettingEmail WHERE Branch_Id={MainClass.BranchNo}",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0) return;

                DataRow row = dataTable.Rows[0];

                // Server Name
                if (!string.IsNullOrEmpty(row["ServerName"].ToString()))
                    _smtpServer = row["ServerName"].ToString();

                // Sender Email
                if (!string.IsNullOrEmpty(row["SendEmail"].ToString()))
                    txtEmail.Text = row["SendEmail"].ToString();

                // Receiver Email
                if (!string.IsNullOrEmpty(row["RecEmail"].ToString()))
                    txtTo.Text = row["RecEmail"].ToString();

                // Password
                if (!string.IsNullOrEmpty(row["SendPWd"].ToString()))
                    txtPassword.Password = row["SendPWd"].ToString();

                // Port
                if (!string.IsNullOrEmpty(row["port"].ToString()))
                    _smtpPort = Convert.ToInt32(row["port"]);
                var Home = new Home();
                // Subject
                txtSubject.Text =
                    $"إغلاق الوردية للموظف {userName} - {Home.lblBranch1.Text}";

                // Body
                txtBody.Text =
                    $"السلام عليكم ورحمة الله وبركاته:{Environment.NewLine}" +
                    $"إغلاق الوردية للموظف {userName}{Environment.NewLine}" +
                    $"{Home.lblBranch1.Text}{Environment.NewLine}" +
                    $"التاريخ: {DateTime.Now.ToShortDateString()}{Environment.NewLine}" +
                    $"المرسل: {userName}{Environment.NewLine}" +
                    $"مع خالص التحايا...";

                // Default Attachment
                lblAttachments.Text = "📎 إغلاق_الوردية.pdf";
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Send Email

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendEmail();
        }

        public void SendEmail()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtEmail.Text) ||
                    string.IsNullOrWhiteSpace(txtTo.Text))
                {
                    DXMessageBox.Show(
                        "يجب إدخال بريد المرسل والمستقبل",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var mailMessage = new MailMessage(
                    txtEmail.Text.Trim(),
                    txtTo.Text.Trim());

                mailMessage.Subject = txtSubject.Text;
                mailMessage.Body = txtBody.Text;
                mailMessage.IsBodyHtml = false;

                // إرفاق ملف pdf افتراضي
                string defaultAttachment =
                    Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "إغلاق_الوردية.pdf");

                if (File.Exists(defaultAttachment))
                    mailMessage.Attachments.Add(new Attachment(defaultAttachment));

                // إرفاق ملفات مختارة يدوياً
                if (_attachmentPaths != null)
                {
                    foreach (string path in _attachmentPaths)
                    {
                        if (File.Exists(path))
                            mailMessage.Attachments.Add(new Attachment(path));
                    }
                }

                var smtpClient = new SmtpClient
                {
                    Host = _smtpServer.Trim(),
                    EnableSsl = true,
                    Port = _smtpPort,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                                               txtEmail.Text.Trim(),
                                               txtPassword.Password.Trim())
                };

                if (!MainClass.CheckForInternetConnection())
                {
                    DXMessageBox.Show(
                        "الجهاز غير متصل بالإنترنت",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                smtpClient.Send(mailMessage);

                DXMessageBox.Show(
                    MainClass.Language == "ar"
                        ? "✅ تم إرسال البريد بنجاح"
                        : "✅ Email sent successfully.",
                    MainClass.Language == "ar" ? "بريد" : "Message",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الإرسال:\n{ex.Message}");
            }
        }

        #endregion

        #region Attachment

        private void LnkAttachment_Click(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "اختر ملفات المرفقات",
                Multiselect = true,
                Filter = "All Files|*.*|PDF|*.pdf|Images|*.jpg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                _attachmentPaths = dialog.FileNames;

                foreach (string path in _attachmentPaths)
                {
                    if (File.Exists(path))
                        lblAttachments.Text +=
                            $"📎 {Path.GetFileName(path)}{Environment.NewLine}";
                }
            }
        }

        #endregion

        #region Utilities

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}