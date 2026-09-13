using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using log4net;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEditItems : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        public List<int> itemList;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region Constructor

        public frmEditItems()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            itemList = new List<int>();

            Loaded += FrmEditItems_Loaded;
            Closing += FrmEditItems_Closing;
        }

        #endregion

        #region Window Events

        private void FrmEditItems_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUnits();
            LoadGroups();
            WireUpEvents();
        }

        private void FrmEditItems_Closing(
            object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            itemList?.Clear();
        }

        private void WireUpEvents()
        {
            btnSave.Click += BtnSave_Click;
            BtnClose.Click += (s, e) => Close();
            btnApplyPrice.Click += (s, e) => ItemOperations(1);
            btnApplyInc.Click += (s, e) => ItemOperations(2);
            btnIncPerc.Click += (s, e) => ItemOperations(3);
            btnDecPerc.Click += (s, e) => ItemOperations(4);

            cmbItemProperties.EditValueChanged
                += CmbItemProperties_EditValueChanged;
        }

        #endregion

        #region Load Data

        public void LoadGroups()
        {
            try
            {
                string query = MainClass.Language != "ar"
                    ? "SELECT id, name FROM ItemsCategory"
                    : "SELECT id, name FROM ItemsCategory WHERE type=2 ORDER BY id";

                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbGroups.ItemsSource = dataTable.DefaultView;
                cmbGroups.DisplayMember = "name";
                cmbGroups.ValueMember = "id";
                cmbGroups.EditValue = null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LoadUnits()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM units ORDER BY id", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbUnit.ItemsSource = dataTable.DefaultView;
                cmbUnit.DisplayMember = "name";
                cmbUnit.ValueMember = "id";
                cmbUnit.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Item Properties ComboBox

        private void CmbItemProperties_EditValueChanged(
            object sender,
            EditValueChangedEventArgs e)
        {
            int selectedIndex = cmbItemProperties.SelectedIndex;

            if (selectedIndex == 1)
            {
                PnlProperty.Visibility = Visibility.Visible;
                txtFillVal.Visibility = Visibility.Collapsed;
                ckWeightScale.Visibility = Visibility.Visible;
                ckScalePrice.Visibility = Visibility.Visible;
                ckWeightScale.IsChecked = true;
            }
            else if (selectedIndex == 2)
            {
                PnlProperty.Visibility = Visibility.Visible;
                ckWeightScale.Visibility = Visibility.Collapsed;
                ckScalePrice.Visibility = Visibility.Collapsed;
                txtFillVal.Visibility = Visibility.Visible;
                txtFillVal.Focus();
            }
            else
            {
                txtFillVal.Visibility = Visibility.Collapsed;
                PnlProperty.Visibility = Visibility.Collapsed;
                txtFillVal.Text = "0";
            }
        }

        #endregion

        #region Save

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!User.EditItemInfo)
            {
                DXMessageBox.Show("لا يوجد لديك صلاحية هذه العملية",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Sync.BranchType == 2 && Sync.SyncType > 0)
            {
                DXMessageBox.Show(
                    "لا يمكن إضافة أو تعديل المادة من هذا الفرع",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int itemProperty = 0;
            if (cmbItemProperties.SelectedIndex > 0)
                itemProperty = cmbItemProperties.SelectedIndex;

            int wscale = 0;
            if (itemProperty == 1)
                wscale = ckWeightScale.IsChecked == true ? 1 : 2;

            if (string.IsNullOrWhiteSpace(txtFillVal.Text))
                txtFillVal.Text = "0";

            if (itemProperty == 2 &&
                !double.TryParse(txtFillVal.Text, out double fillVal) ||
                (itemProperty == 2 && Convert.ToDouble(txtFillVal.Text) == 0))
            {
                DXMessageBox.Show(
                    MainClass.Language != "ar"
                        ? "Enter Filling Value"
                        : "أدخل قيمة التعبئة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFillVal.Focus();
                return;
            }

            if (itemList == null || itemList.Count == 0)
                return;

            var itemOper = new ItemOper();
            var products = itemOper.BindProduct(itemList);

            foreach (var item in products)
            {
                // سعر البيع
                if (!string.IsNullOrWhiteSpace(txtSalePrice.Text))
                    item.SalePrice = Convert.ToDouble(txtSalePrice.Text);

                // سعر الشراء
                if (!string.IsNullOrWhiteSpace(txtPurchPrice.Text))
                    item.PurchPrice = Convert.ToDouble(txtPurchPrice.Text);

                // الضريبة
                if (!string.IsNullOrWhiteSpace(txtTaxPrec.Text))
                    item.VAT = Convert.ToDouble(txtTaxPrec.Text);

                // المجموعة
                if (cmbGroups.SelectedIndex != -1 &&
                    !string.IsNullOrEmpty(cmbGroups.Text))
                    item.CategoryID = cmbGroups.EditValue?.ToString();

                // الوحدة
                if (cmbUnit.SelectedIndex != -1)
                {
                    foreach (var unit in item.ProductUnits)
                    {
                        if (unit.UnitId == item.MainUnit)
                        {
                            unit.UnitId = Convert.ToInt32(cmbUnit.EditValue);
                            unit.SalePrice = item.SalePrice;
                            unit.PurchasePrice = item.PurchPrice;
                        }
                    }
                    item.MainUnit = Convert.ToInt32(cmbUnit.EditValue);
                }

                // الحدود
                if (!string.IsNullOrWhiteSpace(txtLimit.Text))
                    item.limitStock = Convert.ToInt32(txtLimit.Text);

                if (!string.IsNullOrWhiteSpace(txtMaxQtyLimit.Text))
                    item.MaxQtyLimit = Convert.ToDouble(txtMaxQtyLimit.Text);

                // الظهور
                item.ShowInPOS = clkShowInPOS.IsChecked == true;
                item.showInAndroid = chkshowInAndroid.IsChecked == true;

                // معفي من الضريبة
                if (ckNoTax.IsChecked == true)
                    item.VAT = 0.0;

                // خواص الصنف
                if (cmbItemProperties.SelectedIndex != -1)
                {
                    item.Property = (ProductProperty)itemProperty;

                    if (itemProperty == 2)
                        item.PackingValue = 2.0;

                    if (itemProperty == 1)
                        item.Wscale = wscale;
                }
            }

            if (new EntityOperations().SaveProducts(products))
            {
                DXMessageBox.Show("✅ تم تطبيق التغييرات بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Info(
                    $"تم حفظ الأصناف بواسطة المستخدم: {MainClass.UserName}");
            }
        }

        #endregion

        #region Item Operations (Price Apply)

        private void ItemOperations(int operType)
        {
            if (!User.EditItemInfo)
            {
                DXMessageBox.Show("لا يوجد لديك صلاحية هذه العملية",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Sync.BranchType == 2 && Sync.SyncType > 0)
            {
                DXMessageBox.Show(
                    "لا يمكن إضافة أو تعديل المادة من هذا الفرع",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (itemList == null || itemList.Count == 0)
                return;

            var itemOper = new ItemOper();
            var products = itemOper.BindProduct(itemList);

            foreach (var item in products)
            {
                // تطبيق على سعر البيع
                if (chkSalePrice.IsChecked == true &&
                    !string.IsNullOrWhiteSpace(txtSalePrice.Text))
                {
                    double saleVal = Convert.ToDouble(txtSalePrice.Text);
                    ApplyOperation(ref saleVal, item.SalePrice, operType);
                    item.SalePrice = saleVal;

                    foreach (var unit in item.ProductUnits)
                    {
                        double unitSale = unit.SalePrice;
                        ApplyOperation(ref unitSale, unit.SalePrice, operType,
                            Convert.ToDouble(txtSalePrice.Text));
                        unit.SalePrice = unitSale;
                    }
                }

                // تطبيق على سعر الشراء
                if (chkPurchPrice.IsChecked == true &&
                    !string.IsNullOrWhiteSpace(txtPurchPrice.Text))
                {
                    double purchVal = Convert.ToDouble(txtPurchPrice.Text);
                    ApplyOperation(ref purchVal, item.PurchPrice, operType);
                    item.PurchPrice = purchVal;

                    foreach (var unit in item.ProductUnits)
                    {
                        double unitPurch = unit.PurchasePrice;
                        ApplyOperation(ref unitPurch, unit.PurchasePrice, operType,
                            Convert.ToDouble(txtPurchPrice.Text));
                        unit.PurchasePrice = unitPurch;
                    }
                }
            }

            if (new EntityOperations().SaveProducts(products))
            {
                DXMessageBox.Show("✅ تم تطبيق التغييرات بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// تطبيق العملية على القيمة حسب نوع العملية
        /// </summary>
        private void ApplyOperation(
            ref double result,
            double currentValue,
            int operType,
            double? inputValue = null)
        {
            double val = inputValue ?? result;

            result = operType switch
            {
                1 => val,                                  // تطبيق سعر
                2 => currentValue + val,                   // زيادة
                3 => currentValue * (1.0 + val / 100.0),   // زيادة %
                4 => currentValue - currentValue * (val / 100.0), // خصم %
                _ => result
            };
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