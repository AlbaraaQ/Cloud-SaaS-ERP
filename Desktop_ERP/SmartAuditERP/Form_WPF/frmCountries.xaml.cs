using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core;
using log4net;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCountries : ThemedWindow
    {
        #region ── Private Fields ──

        private SqlConnection conn;
        private int Code = -1;
        private DataTable dtCountries;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Constructor ──

        public frmCountries()
        {
            conn = MainClass.ConnObj();
            Code = -1;
            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDG();
        }

        #endregion

        #region ── Clear ──

        private void CLR()
        {
            txtName.Text = "";
            txtNationality.Text = "";
            picFlag.Source = null;
            Code = -1;
            dgvCountries.UnselectAll();
        }

        #endregion

        #region ── Load Data ──

        private void LoadDG()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name, nationality FROM Countries", conn);
                dtCountries = new DataTable();
                adapter.Fill(dtCountries);
                dgvCountries.ItemsSource = dtCountries.DefaultView;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Navigate ──

        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvCountries.UnselectAll();

                if (conn.State != ConnectionState.Open) conn.Open();

                var cmd = new SqlCommand(sqlQuery, conn);
                using var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) return;

            reader.Read();

            Code = Convert.ToInt32(reader["id"]);
            txtName.Text = reader["name"].ToString();
            txtNationality.Text = reader["nationality"].ToString();

            if (reader["flag_img"] != DBNull.Value)
            {
                byte[] imgBytes = (byte[])reader["flag_img"];
                picFlag.Source = BytesToBitmapImage(imgBytes);
            }
            else
            {
                picFlag.Source = null;
            }

            reader.Close();
        }

        #endregion

        #region ── Save ──

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowMsg("أدخل اسم الدولة", "", MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNationality.Text))
                {
                    ShowMsg("أدخل اسم الجنسية", "", MessageBoxImage.Warning);
                    txtNationality.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                byte[] imgBytes = null;
                if (picFlag.Source != null)
                    imgBytes = BitmapImageToBytes(picFlag.Source as BitmapImage);

                if (Code == -1)
                {
                    var cmd = new SqlCommand(
                        $"INSERT INTO Countries(name, nationality, flag_img) " +
                        $"VALUES(N'{txtName.Text}', N'{txtNationality.Text}', @img)",
                        conn);

                    cmd.Parameters.Add("@img", SqlDbType.Image).Value =
                        (object)imgBytes ?? DBNull.Value;

                    cmd.ExecuteNonQuery();
                    CLR();
                    txtName.Focus();
                }
                else
                {
                    var cmd = new SqlCommand(
                        $"UPDATE Countries SET " +
                        $"name=N'{txtName.Text}', " +
                        $"nationality=N'{txtNationality.Text}', " +
                        $"flag_img=@img WHERE id={Code}",
                        conn);

                    cmd.Parameters.Add("@img", SqlDbType.Image).Value =
                        (object)imgBytes ?? DBNull.Value;

                    cmd.ExecuteNonQuery();
                    txtName.Focus();
                }

                LoadDG();
            }
            catch (Exception ex)
            {
                ShowMsg(
                    $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ── Delete ──

        private void Delete()
        {
            try
            {
                if (Code == -1)
                {
                    ShowMsg("اختر دولة ليتم حذفها", "", MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "هل أنت متأكد من حذف هذه الدولة؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (result != MessageBoxResult.Yes) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"DELETE FROM Countries WHERE id={Code}", conn)
                    .ExecuteNonQuery();

                ShowMsg("تم الحذف");
                LoadDG();
                CLR();
            }
            catch (Exception ex)
            {
                ShowMsg(
                    $"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ── Button Events ──

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => CLR();

        private void btnSave_Click(object sender, RoutedEventArgs e)
            => Save();

        private void btnDelete_Click(object sender, RoutedEventArgs e)
            => Delete();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Countries ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM Countries ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Countries " +
                        $"WHERE id>{Code} ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM Countries " +
                        $"WHERE id<{Code} ORDER BY id DESC");

        #endregion

        #region ── Grid Events ──

        private void dgvCountries_MouseLeftButtonUp(object sender,
            MouseButtonEventArgs e)
        {
            try
            {
                if (dgvCountries.SelectedItem is DataRowView row)
                {
                    Code = Convert.ToInt32(row["id"]);
                    Navigate($"SELECT * FROM Countries WHERE id={Code}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Image Links ──

        private void lnkAdd_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "All Files|*.*|JPEG|*.jpg|" +
                             "BMP|*.bmp|GIF|*.gif|TIFF|*.tiff|PNG|*.png",
                    Title = "اختر صورة العلم"
                };

                if (openDialog.ShowDialog() == true)
                {
                    picFlag.Source = new BitmapImage(
                        new Uri(openDialog.FileName));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCountries {ex.Message} {MainClass.UserName}");
            }
        }

        private void lnkClear_Click(object sender, MouseButtonEventArgs e)
        {
            picFlag.Source = null;
        }

        #endregion

        #region ── Image Helpers ──

        private static BitmapImage BytesToBitmapImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            using var ms = new System.IO.MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static byte[] BitmapImageToBytes(BitmapImage bitmap)
        {
            if (bitmap == null) return null;

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var ms = new System.IO.MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }

        #endregion

        #region ── Helper ──

        private static void ShowMsg(
            string message,
            string title = "SmartAudit ERP",
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, icon,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);
        }

        #endregion
    }
}