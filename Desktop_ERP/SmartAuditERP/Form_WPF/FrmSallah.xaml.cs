using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmSallah : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SallaAPI sallaApi;
        private ProductsManager productsManager;
        private OrdersManager ordersManager;

        #endregion

        #region Constructor

        public FrmSallah()
        {
            InitializeComponent();
        }

        #endregion

        #region Window Loaded

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sallaApi         = new SallaAPI("2adcaba8-c5a8-426c-8fc9-3281fa4b056d");
            productsManager  = new ProductsManager(sallaApi);
            ordersManager    = new OrdersManager(sallaApi);
        }

        #endregion

        #region Button Events

        private async void btnGetProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await productsManager.GetProducts();
                int count  = result["data"].ToObject<List<object>>().Count;
                DXMessageBox.Show($"تم جلب {count} منتج.", "نجح",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnGetOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ordersManager.GetOrders();
                int count  = result["data"].ToObject<List<object>>().Count;
                DXMessageBox.Show($"تم جلب {count} طلب.", "نجح",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productData = new
                {
                    name        = "اسم المنتج",
                    price       = 100,
                    quantity    = 50,
                    description = "وصف المنتج"
                };

                await productsManager.CreateProduct(productData);
                DXMessageBox.Show("تم إضافة المنتج بنجاح.", "نجح",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Button3_Click(object sender, RoutedEventArgs e)
        {
            // نفس منطق جلب الطلبات
            await Task.Run(() => { });
            DXMessageBox.Show("جلب الطلبات (2) — يمكن تخصيصه لاحقاً.", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}