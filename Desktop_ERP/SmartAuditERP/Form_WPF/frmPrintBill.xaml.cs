using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة طباعة الفاتورة - WPF Version
    /// </summary>
    public partial class frmPrintBill : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Constructor

        public frmPrintBill()
        {
            InitializeComponent();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تعيين التاريخ الحالي
            txtDate.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        #endregion

        #region Button1 - Print

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Button1.Visibility = Visibility.Collapsed;
            print();
        }

        /// <summary>
        /// منطق الطباعة - يمكن توسيعه لاحقاً
        /// </summary>
        public void print()
        {
            // منطق الطباعة يُضاف هنا
        }

        #endregion

        #region Button2 - Image

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            // منطق زر الصورة يُضاف هنا
        }

        #endregion

        #region TextBox Events (kept for compatibility)

        private void txthand1_TextChanged(object sender, TextChangedEventArgs e)
        {
            // منطق تغيير النص يُضاف هنا
        }

        private void typeCB_TextChanged(object sender, TextChangedEventArgs e)
        {
            // منطق تغيير نوع الثوب يُضاف هنا
        }

        private void lblCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            // منطق تغيير الكود يُضاف هنا
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// تحميل بيانات الفاتورة من الخارج
        /// </summary>
        public void LoadBillData(PrintBillModel model)
        {
            if (model == null) return;

            // Header
            lblCode.Text = model.Code;
            txtDate.Text = model.Date;
            txtName.Text = model.ClientName;
            txtPhoneNum.Text = model.ClientPhone;
            typeCB.Text = model.GarmentType;

            // Prices
            txtPrice.Text = model.Price.ToString("N2");
            txtQuantity.Text = model.Quantity.ToString();
            txtPrice_sale.Text = model.TotalPrice.ToString("N2");

            // Measurements
            txtheight1.Text = model.Height1;
            txtheight2.Text = model.Height2;
            txtshoulder.Text = model.Shoulder;
            txtneck1.Text = model.Neck1;
            txtneck2.Text = model.Neck2;
            txthand1.Text = model.Hand1;
            txthand2.Text = model.Hand2;
            txtexpand1.Text = model.Expand1;
            txtexpand2.Text = model.Expand2;
            txtexpand3.Text = model.Expand3;
            txtexpandHand1.Text = model.ExpandHand1;
            txtexpandHand2.Text = model.ExpandHand2;

            // Shape Info
            cmbCharShape.Text = model.CharShape;
            txtCahrSize.Text = model.CharSize;
            shoulderCB.Text = model.ShoulderCB;
            PocketCB.Text = model.PocketCB;
            pocketCheck.Text = model.PocketCheck;
            cmbHandShape.Text = model.HandShape;
            txtHandShape1.Text = model.HandShape1;
            txtHandShape2.Text = model.HandShape2;
            handShapeCB.Text = model.HandShapeCB;
            cmbNeckShape.Text = model.NeckShape;
            txtNeckShape1.Text = model.NeckShape1;
            txtNeckShape2.Text = model.NeckShape2;
            nickShapeCB.Text = model.NickShapeCB;

            // Pocket Details
            txtPoketSize1.Text = model.PocketSize1.ToString();
            txtPoketSize2.Text = model.PocketSize2.ToString();
            txtPoketLong.Text = model.PocketLong.ToString();

            // CheckBoxes
            pocketPenCB.IsChecked = model.HasPocketPen;
            disappearCB.IsChecked = model.HasDisappearPocket;
            squareRB.IsChecked = model.IsSquareShape;
            tranRB.IsChecked = model.IsTriangleShape;

            // Bottom sizes
            down.Text = model.Down.ToString();
            handDownTB.Text = model.HandDown.ToString();
            pho1.Text = model.Pho1.ToString();
            pho2.Text = model.Pho2.ToString();
            save1.Text = model.Save1.ToString();
            save2.Text = model.Save2.ToString();

            // Notes
            title.Text = model.Notes;

            // Invoice summary
            txtQuantity2.Text = model.Quantity.ToString();
            txtPriceWithTax.Text = model.TotalPrice.ToString("N2");
            txtPaid.Text = model.Paid.ToString("N2");
            txtRest.Text = model.Rest.ToString("N2");
            lblCode2.Text = model.Code;
            name2.Text = model.ClientName;
            phone2.Text = model.ClientPhone;

            // Header labels (static but kept for binding compatibility)
            txthand242.Text = model.Hand242Label;
            txthand1447.Text = model.Hand1447Label;
            TextBox10.Text = model.TextBox10Label;
            TextBox11.Text = model.TextBox11Label;
        }

        #endregion
    }
    /// <summary>
    /// نموذج بيانات فاتورة الطباعة
    /// </summary>
    public class PrintBillModel
    {
        #region Header

        /// <summary>رقم الفاتورة</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>التاريخ</summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>اسم العميل</summary>
        public string ClientName { get; set; } = string.Empty;

        /// <summary>رقم جوال العميل</summary>
        public string ClientPhone { get; set; } = string.Empty;

        /// <summary>نوع الثوب</summary>
        public string GarmentType { get; set; } = string.Empty;

        #endregion

        #region Prices

        /// <summary>السعر الأساسي</summary>
        public double Price { get; set; }

        /// <summary>الكمية</summary>
        public int Quantity { get; set; }

        /// <summary>السعر الإجمالي</summary>
        public double TotalPrice { get; set; }

        /// <summary>المبلغ المدفوع</summary>
        public double Paid { get; set; }

        /// <summary>المبلغ المتبقي</summary>
        public double Rest { get; set; }

        #endregion

        #region Measurements

        /// <summary>الطول 1</summary>
        public string Height1 { get; set; } = string.Empty;

        /// <summary>الطول 2</summary>
        public string Height2 { get; set; } = string.Empty;

        /// <summary>الكتف</summary>
        public string Shoulder { get; set; } = string.Empty;

        /// <summary>الرقبة 1</summary>
        public string Neck1 { get; set; } = string.Empty;

        /// <summary>الرقبة 2</summary>
        public string Neck2 { get; set; } = string.Empty;

        /// <summary>اليد 1</summary>
        public string Hand1 { get; set; } = string.Empty;

        /// <summary>اليد 2</summary>
        public string Hand2 { get; set; } = string.Empty;

        /// <summary>الوسع 1</summary>
        public string Expand1 { get; set; } = string.Empty;

        /// <summary>الوسع 2</summary>
        public string Expand2 { get; set; } = string.Empty;

        /// <summary>الوسع 3</summary>
        public string Expand3 { get; set; } = string.Empty;

        /// <summary>وسع الكم 1</summary>
        public string ExpandHand1 { get; set; } = string.Empty;

        /// <summary>وسع الكم 2</summary>
        public string ExpandHand2 { get; set; } = string.Empty;

        #endregion

        #region Shape Info

        /// <summary>شكل الجبزور</summary>
        public string CharShape { get; set; } = string.Empty;

        /// <summary>مقاس الجبزور</summary>
        public string CharSize { get; set; } = string.Empty;

        /// <summary>الكتف CB</summary>
        public string ShoulderCB { get; set; } = string.Empty;

        /// <summary>الجيب CB</summary>
        public string PocketCB { get; set; } = string.Empty;

        /// <summary>فحص الجيب</summary>
        public string PocketCheck { get; set; } = string.Empty;

        /// <summary>شكل اليد</summary>
        public string HandShape { get; set; } = string.Empty;

        /// <summary>شكل اليد 1</summary>
        public string HandShape1 { get; set; } = string.Empty;

        /// <summary>شكل اليد 2</summary>
        public string HandShape2 { get; set; } = string.Empty;

        /// <summary>شكل اليد CB</summary>
        public string HandShapeCB { get; set; } = string.Empty;

        /// <summary>شكل الرقبة</summary>
        public string NeckShape { get; set; } = string.Empty;

        /// <summary>شكل الرقبة 1</summary>
        public string NeckShape1 { get; set; } = string.Empty;

        /// <summary>شكل الرقبة 2</summary>
        public string NeckShape2 { get; set; } = string.Empty;

        /// <summary>شكل الرقبة CB</summary>
        public string NickShapeCB { get; set; } = string.Empty;

        #endregion

        #region Pocket Details

        /// <summary>مقاس الجيب 1</summary>
        public double PocketSize1 { get; set; }

        /// <summary>مقاس الجيب 2</summary>
        public double PocketSize2 { get; set; }

        /// <summary>بعد الجيب</summary>
        public double PocketLong { get; set; }

        /// <summary>جيب قلم</summary>
        public bool HasPocketPen { get; set; }

        /// <summary>جيب مخفي</summary>
        public bool HasDisappearPocket { get; set; }

        #endregion

        #region Shape Options

        /// <summary>شكل مربع</summary>
        public bool IsSquareShape { get; set; }

        /// <summary>شكل مثلث</summary>
        public bool IsTriangleShape { get; set; }

        #endregion

        #region Bottom Sizes

        /// <summary>أسفل</summary>
        public double Down { get; set; }

        /// <summary>كفة تحت</summary>
        public double HandDown { get; set; }

        /// <summary>محفظة 1</summary>
        public double Pho1 { get; set; }

        /// <summary>محفظة 2</summary>
        public double Pho2 { get; set; }

        /// <summary>جوال 1</summary>
        public double Save1 { get; set; }

        /// <summary>جوال 2</summary>
        public double Save2 { get; set; }

        #endregion

        #region Notes

        /// <summary>الملاحظات</summary>
        public string Notes { get; set; } = string.Empty;

        #endregion

        #region Label Defaults

        /// <summary>تسمية txthand242</summary>
        public string Hand242Label { get; set; } = "ك";

        /// <summary>تسمية txthand1447</summary>
        public string Hand1447Label { get; set; } = "س";

        /// <summary>تسمية TextBox10</summary>
        public string TextBox10Label { get; set; } = "س";

        /// <summary>تسمية TextBox11</summary>
        public string TextBox11Label { get; set; } = "ص";

        #endregion
    }
}