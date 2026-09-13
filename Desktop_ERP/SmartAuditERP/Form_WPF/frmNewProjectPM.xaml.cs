using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmNewProjectPM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private ObservableCollection<ProjectSearchRow> _searchRows;
        private int _projectId = -1;

        #endregion

        #region Constructor

        public frmNewProjectPM()
        {
            _searchRows = new ObservableCollection<ProjectSearchRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmNewProjectPM_Load(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate      = DateTime.Today;
            txtStartDate.SelectedDate = DateTime.Today;
            txtEndDate.SelectedDate   = DateTime.Today;
            txtFromDate.SelectedDate  = DateTime.Today;
            txtToDate.SelectedDate    = DateTime.Today;

            dgvItems.ItemsSource = _searchRows;

            try
            {
                new GetDataAPI().GetItems();
            }
            catch { }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchProjects();
        }

        private void SearchProjects()
        {
            try
            {
                _searchRows.Clear();
                // بيانات البحث تُضاف هنا عند تطوير الكود الخلفي للمشاريع
                // مثال: SQL + ObservableCollection
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbClientSrch == null) return;
            cmbClientSrch.IsEnabled = chkAll.IsChecked != true;
        }

        private void dgvItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProjectSearchRow selected = dgvItems.SelectedItem as ProjectSearchRow;
            if (selected == null) return;

            // فتح المشروع المحدد
            TabControl1.SelectedIndex = 0;
        }

        #endregion

        #region Image

        private void lnkImgAdd_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };

                if (dialog.ShowDialog() == true)
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource  = new Uri(dialog.FileName);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    PictureBox1.Source = bmp;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void lnkImgClr_Click(object sender, MouseButtonEventArgs e)
        {
            PictureBox1.Source = null;
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            // بحث عن تكلفة المشروع
        }

        #endregion

        #region Navigation Buttons (Stubs)

        private void btnNew_Click(object sender, RoutedEventArgs e) { }
        private void btnSave_Click(object sender, RoutedEventArgs e) { }
        private void btnDelete_Click(object sender, RoutedEventArgs e) { }
        private void btnPrint_Click(object sender, RoutedEventArgs e) { }
        private void btnFirst_Click(object sender, RoutedEventArgs e) { }
        private void btnPrevious_Click(object sender, RoutedEventArgs e) { }
        private void btnNext_Click(object sender, RoutedEventArgs e) { }
        private void btnLast_Click(object sender, RoutedEventArgs e) { }
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Helpers

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class ProjectSearchRow
    {
        public string DataGridViewTextBoxColumn3 { get; set; }
        public string Column20  { get; set; }
        public string Column4   { get; set; }
        public string Column13  { get; set; }
        public string Column6   { get; set; }
        public string Column8   { get; set; }
        public string Column14  { get; set; }
        public string Column17  { get; set; }
        public string ItemStock { get; set; }
    }
}