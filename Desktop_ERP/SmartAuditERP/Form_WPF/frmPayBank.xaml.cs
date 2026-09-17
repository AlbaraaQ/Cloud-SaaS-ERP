using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmPayBank : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        /// <summary>
        /// يحمل معرف البنك المختار (0 = لم يُختر بعد)
        /// </summary>
        public int SelectedBankId { get; private set; } = 0;

        private Button _selectedTileButton = null;

        #endregion

        #region Constructor

        public frmPayBank()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBanks();
        }

        #endregion

        #region Load Banks as Tiles

        private void LoadBanks()
        {
            wrapBanks.Children.Clear();
            SelectedBankId = 0;
            _selectedTileButton = null;

            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id AS BankId, name AS BankName FROM Banks WHERE IS_Deleted=0 AND id != 1 AND id != 2",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        wrapBanks.Children.Add(new TextBlock
                        {
                            Text              = "لا توجد بنوك متاحة",
                            FontSize          = 13,
                            FontWeight        = FontWeights.Bold,
                            Foreground        = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment   = VerticalAlignment.Center,
                            Margin            = new Thickness(20)
                        });
                        return;
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        int    bankId   = Convert.ToInt32(row["BankId"]);
                        string bankName = row["BankName"].ToString();

                        var tileBtn = new Button
                        {
                            Content = $"🏦\n{bankName}",
                            Tag     = bankId,
                            Style   = (Style)this.Resources["BankTileBtn"]
                        };

                        tileBtn.Click += BankTile_Click;
                        wrapBanks.Children.Add(tileBtn);
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البنوك: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Tile Click

        private void BankTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn)
            {
                // إعادة تصميم الزر السابق المحدد
                if (_selectedTileButton != null)
                    _selectedTileButton.Style = (Style)this.Resources["BankTileBtn"];

                // تحديد الزر الجديد
                _selectedTileButton = clickedBtn;
                _selectedTileButton.Style = (Style)this.Resources["BankTileBtnSelected"];

                SelectedBankId = Convert.ToInt32(clickedBtn.Tag);
            }
        }

        #endregion

        #region Button Events

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedBankId == 0)
            {
                DXMessageBox.Show("يرجى اختيار بنك أولًا",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            SelectedBankId = 0;
            this.DialogResult = false;
            this.Close();
        }

        #endregion
    }
}