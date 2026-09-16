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
    public partial class frmSendEmail : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private string[] attachmentFiles = Array.Empty<string>();

        #endregion

        #region Constructor

        public frmSendEmail()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
            => LoadData();

        #endregion

        #region Load Data

        private void LoadData()
        {
            try
            {
                // بيانات الموظف
                var empAdapter = new SqlDataAdapter(
                    $"SELECT name, email FROM Employees " +
                    $"WHERE IS_Deleted=0 AND id={MainClass.UserID}", conn);
                var empDt = new DataTable();
                empAdapter.Fill(empDt);

                if (empDt.Rows.Count > 0)
                {
                    string email = empDt.Rows[0]["email"]?.ToString() ?? "";
                    string name = empDt.Rows[0]["name"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(email))
                        txtEmail.Text = email;

                    txtSubject.Text =
                        $"إغلاق الوردية للموظف {name} {MainClass.BranchName}";

                    txtBody.Text =
                        $"السلام عليكم و رحمة الله و بركاته:\n" +
                        $"الفرع : {MainClass.BranchName}\n" +
                        $"التاريخ: {DateTime.Now.ToShortDateString()}\n" +
                        $"الموظف: {name}\n" +
                        $"مع خالص التحايا …";
                }

                // إيميل المؤسسة
                var fndAdapter = new SqlDataAdapter(
                    "SELECT Email FROM Foundation", conn);
                var fndDt = new DataTable();
                fndAdapter.Fill(fndDt);

                if (fndDt.Rows.Count > 0)
                {
                    string toEmail = fndDt.Rows[0]["Email"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(toEmail))
                        txtTo.Text = toEmail;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var message = new MailMessage(
                    txtEmail.Text.Trim(),
                    txtTo.Text.Trim())
                {
                    Subject = txtSubject.Text,
                    Body = txtBody.Text,
                    IsBodyHtml = false,
                };

                // إرفاق الملفات
                foreach (string file in attachmentFiles)
                {
                    if (File.Exists(file))
                        message.Attachments.Add(new Attachment(file));
                }

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    EnableSsl = true,
                    Port = 587,
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential(
                        txtEmail.Text.Trim(),
                        txtPassword.Password.Trim()),
                };

                smtp.Send(message);

                bool isAr = string.Equals(MainClass.Language, "ar",
                                           StringComparison.OrdinalIgnoreCase);
                DXMessageBox.Show(
                    isAr ? "تم إرسال البريد." : "Email sent.",
                    isAr ? "بريد" : "Message",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في الإرسال:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lnkAttachment_Click(object sender,
                                          System.Windows.Input.MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "All Files (*.*)|*.*",
            };

            if (dlg.ShowDialog() == true)
            {
                attachmentFiles = dlg.FileNames;

                string names = string.Join(", ",
                    System.Linq.Enumerable.Select(dlg.FileNames,
                        f => Path.GetFileName(f)));
                lblAttachments.Text = names;
            }
        }

        #endregion
    }
}