using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class Frmshowdocument : ThemedWindow
    {
        #region Fields

        public int    type      { get; set; } = 0;
        public string GlobalIdDoc { get; set; } = "";

        private SqlConnection conn;
        private ObservableCollection<DocumentItem> _documents;

        #endregion

        #region Constructor

        public Frmshowdocument()
        {
            InitializeComponent();

            conn       = new SqlConnection(MainClass.connstr);
            _documents = new ObservableCollection<DocumentItem>();
            DataGridView1.ItemsSource = _documents;
        }

        #endregion

        #region Public Method to Add Rows

        /// <summary>
        /// إضافة صف مستند من الخارج (بدلاً من DataGridView1.Rows.Add)
        /// </summary>
        public void AddDocument(int rowNo, string fileName, string fileUrl)
        {
            _documents.Add(new DocumentItem
            {
                RowNo    = rowNo,
                FileName = fileName,
                FileUrl  = fileUrl
            });
        }

        #endregion

        #region DataGrid Events

        private void DataGridView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridView1.SelectedItem is DocumentItem doc)
            {
                if (File.Exists(doc.FileUrl))
                {
                    Process.Start(new ProcessStartInfo(doc.FileUrl) { UseShellExecute = true });
                    type       = 0;
                    GlobalIdDoc = "";
                }
                else
                {
                    DXMessageBox.Show("لا يوجد وثائق.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnDeleteDoc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn
                && btn.Tag is DocumentItem doc)
            {
                var confirm = DXMessageBox.Show("هل أنت متأكد من الحذف؟", "المدقق",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                try
                {
                    // حذف من قاعدة البيانات
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();

                    var cmd = new SqlCommand(
                        "DELETE FROM Documents WHERE globalId=@GlobalId AND type=@type",
                        conn);
                    cmd.Parameters.AddWithValue("@GlobalId", GlobalIdDoc);
                    cmd.Parameters.AddWithValue("@type",     type);
                    cmd.ExecuteNonQuery();

                    // حذف الملف من القرص
                    if (File.Exists(doc.FileUrl))
                        File.Delete(doc.FileUrl);

                    // إزالة من القائمة
                    _documents.Remove(doc);

                    // إعادة ترقيم الصفوف
                    int n = 1;
                    foreach (var d in _documents)
                        d.RowNo = n++;

                    DXMessageBox.Show("تم الحذف بنجاح", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    type        = 0;
                    GlobalIdDoc = "";
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show("خطأ في الحذف: " + ex.Message, "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                        conn.Close();
                }
            }
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (conn.State != System.Data.ConnectionState.Closed)
                conn.Close();
        }

        #endregion
    }

    /// <summary>نموذج صف جدول المستندات</summary>
    public class DocumentItem : System.ComponentModel.INotifyPropertyChanged
    {
        private int _rowNo;
        public int    RowNo    { get => _rowNo; set { _rowNo = value; OnPropertyChanged(nameof(RowNo)); } }
        public string FileName { get; set; }
        public string FileUrl  { get; set; }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}