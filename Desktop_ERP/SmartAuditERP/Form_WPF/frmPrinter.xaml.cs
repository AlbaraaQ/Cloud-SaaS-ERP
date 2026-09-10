using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPrinter : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private int    _invType       = -1;

        public string  SelectedRptName { get; set; } = "";
        public string  SelectedPrinter { get; set; } = "";

        public List<InvPrinter> PrintersList { get; set; }

        private Button _selectedTile = null;

        #endregion

        #region Constructor

        public frmPrinter()
        {
            InitializeComponent();
            PrintersList = new List<InvPrinter>();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تحديد نص العنوان حسب اللغة
            lblTital.Text = (MainClass.Language == "ar")
                ? "🖨️ اختر الطابعة"
                : "🖨️ Choose Printer";

            LoadPrinterTiles();
        }

        #endregion

        #region Load Printer Tiles

        private void LoadPrinterTiles()
        {
            wrapPrinters.Children.Clear();
            _selectedTile = null;

            if (PrintersList == null || PrintersList.Count == 0)
            {
                wrapPrinters.Children.Add(new TextBlock
                {
                    Text                = "لا توجد طابعات متاحة",
                    FontSize            = 13,
                    FontWeight          = FontWeights.Bold,
                    Foreground          = new SolidColorBrush(Color.FromRgb(100,100,100)),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Margin              = new Thickness(20)
                });
                return;
            }

            foreach (var printer in PrintersList)
            {
                var tileBtn = new Button
                {
                    Content = $"🖨️\n{printer.PrintName}",
                    Tag     = printer,
                    Style   = (Style)this.Resources["TileBtn"]
                };

                tileBtn.Click += PrinterTile_Click;
                wrapPrinters.Children.Add(tileBtn);
            }
        }

        #endregion

        #region Tile Click

        private void PrinterTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn && clickedBtn.Tag is InvPrinter printer)
            {
                // إعادة تصميم البلاطة السابقة
                if (_selectedTile != null)
                    _selectedTile.Style = (Style)this.Resources["TileBtn"];

                // تحديد البلاطة الجديدة
                _selectedTile             = clickedBtn;
                _selectedTile.Background  = new SolidColorBrush(Color.FromRgb(46,204,113));
                _selectedTile.Foreground  = new SolidColorBrush(Colors.White);

                SelectedRptName = printer.RptName;
                SelectedPrinter = printer.Printer;

                // إغلاق فوري عند الاختيار (مثل TileView1_ItemClick الأصلية)
                this.Close();
            }
        }

        #endregion
    }

    #region Model

    /// <summary>
    /// نموذج بيانات الطابعة
    /// </summary>
    public class InvPrinter
    {
        public string PrintName { get; set; }
        public string RptName   { get; set; }
        public string Printer   { get; set; }
    }

    #endregion
}