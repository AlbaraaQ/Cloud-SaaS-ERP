using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AuditorAPI.Models.DGVmodels;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSelectInv : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public List<InvoiceItem> itemList;

        #endregion

        #region Constructor

        public frmSelectInv()
        {
            InitializeComponent();
            itemList = new List<InvoiceItem>();
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        #endregion

        #region Helper Methods

        private void ShowWindowAndCloseCurrent(Window targetWindow)
        {
            if (targetWindow == null)
                return;

            targetWindow.WindowState = System.Windows.WindowState.Maximized;
            targetWindow.Show();
            targetWindow.Activate();
            Close();
        }

        private T FindOpenedWindow<T>() where T : Window
        {
            return System.Windows.Application.Current.Windows
                .OfType<T>()
                .FirstOrDefault();
        }

        #endregion

        #region GenerateInvoice

        private void GenerateInvoice(int invType, int procType)
        {
            bool isAr = string.Equals(
                MainClass.Language,
                "ar",
                StringComparison.OrdinalIgnoreCase);

            // ═══════════════════════════════════════
            // مشتريات / مرتجع مشتريات
            // ═══════════════════════════════════════
            if (invType == 1)
            {
                var form = new frmInvPurch();

                if (procType == 1)
                {
                    form.InvType = 1;
                    form.ProcType = 1;
                    form.EntryType = 1;
                    form.Tag = "SalePurch";
                    form.cmbProcType.SelectedIndex = 0;
                    form.cmbProcTypeSrch.SelectedIndex = 0;
                }
                else if (procType == 2)
                {
                    form.InvType = 1;
                    form.ProcType = 2;
                    form.EntryType = 21;
                    form.Tag = "SalePurch4";
                    form.Title = isAr ? "مرتجع مشتريات" : "Purchase Return";
                    form.cmbProcType.SelectedIndex = 1;
                    form.cmbProcTypeSrch.SelectedIndex = 1;
                }

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
                return;
            }

            // ═══════════════════════════════════════
            // مبيعات / مرتجع مبيعات / عرض سعر
            // ═══════════════════════════════════════
            if (invType == 2)
            {
                var form = new frmInvSale();

                if (procType == 1)
                {
                    form.InvType = 2;
                    form.ProcType = 1;
                    form.EntryType = 2;
                    form.Tag = "InvSale";
                    form.Title = isAr ? "فاتورة مبيعات" : "Sales Invoice";

                    if (form.cmbProcTypeSrch != null)
                        form.cmbProcTypeSrch.SelectedIndex = 0;
                }
                else if (procType == 2)
                {
                    form.InvType = 2;
                    form.ProcType = 2;
                    form.EntryType = 22;
                    form.Tag = "SalePurch5";
                    form.Title = isAr ? "مرتجع مبيعات" : "Return Sale";

                    if (form.cmbProcTypeSrch != null)
                        form.cmbProcTypeSrch.SelectedIndex = 1;
                }
                else if (procType == 4)
                {
                    form.InvType = 2;
                    form.ProcType = 4;
                    form.Tag = "Quotation";
                    form.Title = isAr ? "عرض سعر" : "Quotation";

                    if (form.cmbProcTypeSrch != null)
                        form.cmbProcTypeSrch.SelectedIndex = 2;
                }

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
                return;
            }

            // ═══════════════════════════════════════
            // بضاعة أول المدة
            // ═══════════════════════════════════════
            if (invType == 9)
            {
                var form = new frmInvPurch
                {
                    Tag = "SalePurch3",
                    InvType = 9,
                    ProcType = 1,
                    EntryType = 11,
                    Title = isAr ? "بضاعة أول المدة" : "Beginning Inventory"
                };

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.cmbProcType.SelectedIndex = 2;
                form.cmbProcTypeSrch.SelectedIndex = 2;

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
                return;
            }

            // ═══════════════════════════════════════
            // فاتورة إدخال
            // ═══════════════════════════════════════
            if (invType == 4)
            {
                var form = new frmInvInputOutput
                {
                    InvType = 4,
                    ProcType = 1,
                    Title = isAr ? "فاتورة إدخال" : "Input Invoice"
                };

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
                return;
            }

            // ═══════════════════════════════════════
            // فاتورة إخراج
            // ═══════════════════════════════════════
            if (invType == 5)
            {
                var form = new frmInvInputOutput
                {
                    InvType = 5,
                    ProcType = 1,
                    Title = isAr ? "فاتورة إخراج" : "Output Invoice"
                };

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
                return;
            }

            // ═══════════════════════════════════════
            // نقطة البيع POS
            // ═══════════════════════════════════════
            if (invType == 3)
            {
                var openedPosWindow = FindOpenedWindow<frmInvPOS>();
                if (openedPosWindow != null)
                {
                    if (openedPosWindow.WindowState == System.Windows.WindowState.Minimized)
                        openedPosWindow.WindowState = System.Windows.WindowState.Normal;

                    openedPosWindow.WindowState = System.Windows.WindowState.Maximized;
                    openedPosWindow.Activate();
                    openedPosWindow.BringIntoView();

                    // لو أردت الحفاظ على السلوك القديم فقط احذف السطر التالي
                    openedPosWindow.ImportInventoryItems(itemList);

                    Close();
                    return;
                }

                var form = new frmInvPOS
                {
                    InvType = 3,
                    ProcType = 1
                };

                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);

                form.ImportInventoryItems(itemList);
                ShowWindowAndCloseCurrent(form);
            }
        }

        #endregion

        #region Button Events

        private void BtnSale_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(2, 1);

        private void BtnQutation_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(2, 4);

        private void BtnReturnSale_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(2, 2);

        private void BtnPurchase_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(1, 1);

        private void BtnReturnPurch_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(1, 2);

        private void BtnInventory_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(9, 1);

        private void BtnInputInv_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(4, 1);

        private void BtnOutputInv_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(5, 1);

        private void BtnPOS_Click(object sender, RoutedEventArgs e)
            => GenerateInvoice(3, 1);

        private void BtnExit_Click(object sender, RoutedEventArgs e)
            => Close();

        #endregion
    }
}