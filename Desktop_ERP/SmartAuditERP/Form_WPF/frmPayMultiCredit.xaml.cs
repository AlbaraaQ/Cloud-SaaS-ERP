using AuditorAPI;
using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPayMultiCredit : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public InvoicePayment payment    { get; private set; }
        public double         InvoiceNet { get; set; } = 0;
        public int            InvCustomer { get; set; } = 0;
        public bool           IsDone      { get; private set; } = false;

        #endregion

        #region Constructor

        public frmPayMultiCredit()
        {
            InitializeComponent();
            payment = new InvoicePayment();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFormData();
        }

        #endregion

        #region Load

        private void LoadFormData()
        {
            try
            {
                lblNetInvoice.Text = InvoiceNet.ToString(Common.DigitsNo);
                txtCredit.Text     = InvoiceNet.ToString(Common.DigitsNo);
                txtCash.Text       = "0";
                txtVisa.Text       = "0";
                LoadCustomers();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
        }

        private void LoadCustomers()
        {
            try
            {
                DataTable dt = LoadData.Customers(5);
                cmbClient.ItemsSource       = dt.DefaultView;
                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.SelectedIndex     = -1;
            }
            catch { }
        }

        #endregion

        #region Calculation

        private void Calc()
        {
            try
            {
                double.TryParse(txtCash.Text, out double cashAmount);
                double.TryParse(txtVisa.Text, out double visaAmount);
                double remainder = InvoiceNet - (cashAmount + visaAmount);
                txtCredit.Text = remainder.ToString(Common.DigitsNo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Button Events

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSelectCust_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var srchForm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(srchForm);
                srchForm.Type = 5;

                if (string.Equals(cmbClient.Text, "عميل عام",
                    StringComparison.OrdinalIgnoreCase))
                    srchForm.txtClientName.Text = "";
                else
                    srchForm.txtClientName.Text = cmbClient.Text;

                srchForm.PostponeClient = true;
                srchForm.ShowDialog();

                if (!string.IsNullOrEmpty(srchForm.Clientname))
                {
                    LoadCustomers();
                    cmbClient.SelectedValue = srchForm.ClientId;
                }
            }
            catch { }
        }

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if (cmbClient.SelectedIndex == -1)
            {
                DXMessageBox.Show("الرجاء اختيار عميل",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtCredit.Text == "0")
            {
                DXMessageBox.Show("الرجاء اختيار طريقة دفع أخرى",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = Pay();

            if (IsDone && result.Remainder.HasValue && result.Remainder.Value > 0
                && InvCustomer > 0)
            {
                payment = result;
                this.Close();
            }
            else
            {
                payment = result;
                this.Close();
            }
        }

        #endregion

        #region Pay Logic

        private InvoicePayment Pay()
        {
            try
            {
                double.TryParse(txtCash.Text,   out double cash);
                double.TryParse(txtVisa.Text,   out double visa);
                double.TryParse(txtCredit.Text, out double remainder);

                var inv = new InvoicePayment
                {
                    CashPayment = cash,
                    VisaPayment = visa,
                    Remainder   = remainder,
                    Paid        = cash + visa
                };

                if (cmbClient.SelectedValue != null)
                    int.TryParse(cmbClient.SelectedValue.ToString(), out int custId);

                IsDone = true;
                return inv;
            }
            catch
            {
                return new InvoicePayment();
            }
        }

        #endregion

        #region TextChanged Events

        private void txtCash_TextChanged(object sender, TextChangedEventArgs e) => Calc();
        private void txtVisa_TextChanged(object sender, TextChangedEventArgs e) => Calc();

        #endregion
    }
}