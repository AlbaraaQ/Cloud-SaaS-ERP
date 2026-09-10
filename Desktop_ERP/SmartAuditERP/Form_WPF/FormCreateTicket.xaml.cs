using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualBasic;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class FormCreateTicket : Window
    {
        #region Private Fields

        private SupportTicketApiClient apiClient;
        public static string[] filePathDocument;

        #endregion

        #region Constructor

        public FormCreateTicket()
        {
            InitializeComponent();
            this.Loaded += FormCreateTicket_Load;
        }

        #endregion

        #region Window Events

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Load

        private void FormCreateTicket_Load(object sender, RoutedEventArgs e)
        {
            apiClient = new SupportTicketApiClient("https://app-cloud-rmxb.onrender.com");

            ComboBoxPriority.ItemsSource = new[] { "Low", "Medium", "High", "Critical" };
            ComboBoxPriority.SelectedIndex = 1;

            ComboBoxCategory.ItemsSource = new[] { "Technical", "Billing", "General", "Feature Request" };
            ComboBoxCategory.SelectedIndex = 0;

            TextBoxName.Text = Common.FoundationInfoDT.Rows[0]["nameA"]?.ToString();
            TextBoxEmail.Text = Common.FoundationInfoDT.Rows[0]["Email"]?.ToString();
            TextBoxPhone.Text = Common.FoundationInfoDT.Rows[0]["Mobile"]?.ToString();
        }

        #endregion

        #region Submit

        private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxName.Text))
            {
                MessageBox.Show("يرجى إدخال الاسم");
                return;
            }

            if (string.IsNullOrWhiteSpace(TextBoxSubject.Text))
            {
                MessageBox.Show("يرجى إدخال موضوع التذكرة");
                return;
            }

            if (string.IsNullOrWhiteSpace(TextBoxDescription.Text))
            {
                MessageBox.Show("يرجى إدخال وصف المشكلة");
                return;
            }

            try
            {
                ButtonSubmit.IsEnabled = false;

                string email = string.IsNullOrEmpty(TextBoxEmail.Text)
                    ? "Default@gmail.com"
                    : TextBoxEmail.Text;

                var response = await apiClient.CreateTicketAsync(
                    TextBoxName.Text,
                    email,
                    TextBoxPhone.Text,
                    TextBoxSubject.Text,
                    TextBoxDescription.Text,
                    ComboBoxPriority.SelectedItem.ToString(),
                    ComboBoxCategory.SelectedItem.ToString(),
                    "auditor",
                    1,
                    filePathDocument?.ToList()
                );

                if (response.Success)
                {
                    MessageBox.Show($"تم إنشاء التذكرة ✅\nرقمها: {response.TicketNumber}");
                    ClearForm();
                }
                else
                {
                    MessageBox.Show(response.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ButtonSubmit.IsEnabled = true;
            }
        }

        #endregion

        #region Clear

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            TextBoxSubject.Clear();
            TextBoxDescription.Clear();
            ComboBoxPriority.SelectedIndex = 1;
            ComboBoxCategory.SelectedIndex = 0;
        }

        #endregion

        #region View Ticket

        private async void ButtonViewTicket_Click(object sender, RoutedEventArgs e)
        {
            string ticketNumber = Interaction.InputBox("أدخل رقم التذكرة:");

            if (string.IsNullOrWhiteSpace(ticketNumber)) return;

            try
            {
                var result = await apiClient.GetTicketAsync(ticketNumber);

                if (result.Success)
                {
                    MessageBox.Show($"الموضوع: {result.Ticket.Subject}\nالحالة: {result.Ticket.Status}");
                }
                else
                {
                    MessageBox.Show(result.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region My Tickets

        private async void ButtonMyTickets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = Common.FoundationInfoDT.Rows[0]["nameA"]?.ToString();
                var tickets = await apiClient.GetCustomerTicketsAsync(name);

                GridTickets.ItemsSource = tickets;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Attach Images

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                filePathDocument = dialog.FileNames;
                MessageBox.Show($"تم اختيار {filePathDocument.Length} صورة");
            }
        }

        #endregion
    }
}