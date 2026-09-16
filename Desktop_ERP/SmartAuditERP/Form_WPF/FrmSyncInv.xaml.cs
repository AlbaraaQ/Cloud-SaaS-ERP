using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmSyncInv : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string SelectedCode = "";
        private static double AccSum = 0.0;

        private ObservableCollection<InvoiceViewModel> invoiceList;
        private ObservableCollection<ItemViewModel> itemList;

        #endregion

        #region Constructor

        public FrmSyncInv()
        {
            InitializeComponent();
            invoiceList = new ObservableCollection<InvoiceViewModel>();
            itemList = new ObservableCollection<ItemViewModel>();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadDGAsync();
        }

        #endregion

        #region Load Data

        private async Task LoadDGAsync()
        {
            try
            {
                var invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);
                var invoices = (List<Invoice>)await invoiceCRUD.GetInvoices(
                    Sync.ClientCode, Sync.DistBranch, Sync.BranchType);

                invoiceList.Clear();
                itemList.Clear();

                foreach (var invoice in invoices)
                {
                    invoiceList.Add(new InvoiceViewModel
                    {
                        InvGlobalID = invoice.InvGlobalID,
                        InvoiceName = InvoiceOper.GetInvoiceType(2, invoice.ProcType, 0, 1).ToString(),
                        Date = invoice.InvDate.ToString("yyyy-MM-dd"),
                        BranchId = invoice.Branch.ToString(),
                        ProcType = invoice.ProcType.ToString()
                    });

                    if (invoice.Items == null)
                        continue;

                    foreach (var item in invoice.Items)
                    {
                        itemList.Add(new ItemViewModel
                        {
                            InvGlobalID = invoice.InvGlobalID,
                            ProductName = string.IsNullOrWhiteSpace(item.Name)
                                ? item.ItemNo.ToString()
                                : item.Name,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            Sum = item.Quantity * item.Price
                        });
                    }
                }

                GridControl1.ItemsSource = invoiceList;
                GridView2.ItemsSource = new ObservableCollection<ItemViewModel>();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الفواتير\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Grid Events

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridControl1.SelectedItem is InvoiceViewModel selectedInvoice)
            {
                var filteredItems = new ObservableCollection<ItemViewModel>(
                    itemList.Where(i => i.InvGlobalID == selectedInvoice.InvGlobalID));
                GridView2.ItemsSource = filteredItems;
            }
        }

        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel invoice)
            {
                DXMessageBox.Show($"تفاصيل الفاتورة: {invoice.InvGlobalID}",
                    "تفاصيل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel invoice)
            {
                DXMessageBox.Show($"تعديل الفاتورة: {invoice.InvGlobalID}",
                    "تعديل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Toolbar Buttons

        private void btnImportInv_Click(object sender, RoutedEventArgs e)
        {
            _ = SaveDGAsync();
        }

        private void btnAddAcc_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadDGAsync();
        }

        private async Task SaveDGAsync()
        {
            try
            {
                var invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);
                var invoiceOper = new InvoiceOper();
                Entry entr = null;

                var invoices = (List<Invoice>)await invoiceCRUD.GetInvoices(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType);

                foreach (var invoice in invoices)
                    invoiceOper.SaveInvoice(invoice, entr, IsNew: true);

                if (Sync.BranchType == 1)
                    invoiceCRUD.RemoveReceivedInvoice(Sync.ClientCode);

                DXMessageBox.Show("تم الاستيراد بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.None);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الاستيراد\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region Models

    public class InvoiceViewModel
    {
        public string InvGlobalID { get; set; }
        public string InvoiceName { get; set; }
        public string Date { get; set; }
        public string BranchId { get; set; }
        public string ProcType { get; set; }
    }

    public class ItemViewModel
    {
        public string InvGlobalID { get; set; }
        public string ProductName { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }
        public double Sum { get; set; }
    }

    #endregion
}