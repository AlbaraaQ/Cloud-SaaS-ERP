using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTermGrpPM : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        private byte[] _imageBytes;

        private ObservableCollection<TermGroupItem> _groups;

        #endregion

        #region Constructor

        public frmTermGrpPM()
        {
            InitializeComponent();
            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code  = -1;
            _groups = new ObservableCollection<TermGroupItem>();
            dgvGrps.ItemsSource = _groups;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDG("");
            LoadNextGrpNo();
        }

        #endregion

        #region Clear

        private void CLR()
        {
            LoadNextGrpNo();
            Code = -1;
            txtNameAr.Text = "";
            txtNameEn.Text = "";
            picImage.Source = null;
            _imageBytes = null;
        }

        #endregion

        #region Data Loading

        private void LoadNextGrpNo()
        {
            try
            {
                EnsureOpen(conn);
                var cmd    = new SqlCommand("SELECT ISNULL(MAX(id), 0) + 1 FROM PM_TermGroups", conn);
                var result = cmd.ExecuteScalar();
                txtGrpNO.Text = result?.ToString() ?? "1";
            }
            catch { txtGrpNO.Text = "1"; }
            finally { EnsureClose(conn); }
        }

        private void LoadDG(string condition)
        {
            try
            {
                _groups.Clear();
                EnsureOpen(conn);
                string cond = string.IsNullOrWhiteSpace(condition) ? "" : condition + " AND ";
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name, nameEN FROM PM_TermGroups WHERE {cond}IS_Deleted=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                foreach (DataRow row in dt.Rows)
                {
                    _groups.Add(new TermGroupItem
                    {
                        Id   = Convert.ToInt32(row["id"]),
                        Name = MainClass.Language == "ar"
                            ? row["name"].ToString()
                            : row["nameEN"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNameAr.Text))
            {
                DXMessageBox.Show("ادخل اسم المجموعة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNameAr.Focus(); return;
            }

            try
            {
                EnsureOpen(conn);
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // التحقق من التكرار
                    if (Code == -1)
                    {
                        var chkCmd = new SqlCommand(
                            $"SELECT id FROM PM_TermGroups WHERE name='{txtNameAr.Text}' AND IS_Deleted=0",
                            conn, transaction);
                        var chkDt = new DataTable();
                        new SqlDataAdapter(chkCmd).Fill(chkDt);

                        if (chkDt.Rows.Count > 0)
                        {
                            var answer = DXMessageBox.Show("اسم المجموعة تم إدخاله مسبقاً. هل تريد المتابعة؟",
                                                          "تنبيه", MessageBoxButton.YesNoCancel,
                                                          MessageBoxImage.Warning);
                            if (answer != MessageBoxResult.Yes)
                            {
                                transaction.Rollback();
                                txtNameAr.Focus();
                                return;
                            }
                        }
                    }

                    string sql = Code != -1
                        ? $"UPDATE PM_TermGroups SET name=@name, nameEN=@nameEN, IS_Deleted=0, image=@image WHERE id={Code}"
                        : "INSERT INTO PM_TermGroups(name,nameEN,IS_Deleted,image) VALUES(@name,@nameEN,0,@image)";

                    var cmd = new SqlCommand(sql, conn, transaction);
                    cmd.Parameters.AddWithValue("@name",   txtNameAr.Text);
                    cmd.Parameters.AddWithValue("@nameEN", txtNameEn.Text);

                    if (_imageBytes != null && _imageBytes.Length > 0)
                        cmd.Parameters.Add("@image", SqlDbType.Image).Value = _imageBytes;
                    else
                        cmd.Parameters.Add("@image", SqlDbType.Image).Value = DBNull.Value;

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    LoadDG("");

                    var savedMsg = new frmSavedMsg();
                    if (Code != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                    savedMsg.ShowDialog();

                    if (savedMsg.Pressed == 1) { CLR(); txtNameAr.Focus(); }
                    else if (savedMsg.Pressed == 3) this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاتصال: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // (لا يوجد زر حذف في الكود الأصلي - يمكن إضافته لاحقاً)
            DXMessageBox.Show("حذف المجموعة غير مدعوم حالياً", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("الطباعة غير مدعومة حالياً", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string condition = "";
                if (!string.IsNullOrWhiteSpace(txtNameSrch.Text))
                    condition += $"name LIKE '%{txtNameSrch.Text}%'";
                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                {
                    if (!string.IsNullOrWhiteSpace(condition)) condition += " AND ";
                    condition += $"id LIKE '%{txtSrchNo.Text}%'";
                }
                LoadDG(string.IsNullOrWhiteSpace(condition) ? "" : condition + " AND ");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM PM_TermGroups WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM PM_TermGroups WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM PM_TermGroups WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM PM_TermGroups WHERE IS_Deleted=0 ORDER BY id DESC");

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var reader = new SqlCommand(sqlQuery, conn).ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) { reader.Close(); return; }

            reader.Read();
            CLR();

            Code           = Convert.ToInt32(reader["id"]);
            txtGrpNO.Text  = Code.ToString();
            txtNameAr.Text = reader["name"].ToString();
            try { txtNameEn.Text = reader["nameEN"].ToString(); } catch { }
            reader.Close();

            // تحميل الصورة
            try
            {
                EnsureOpen(conn);
                var imgAdapter = new SqlDataAdapter(
                    $"SELECT image FROM PM_TermGroups WHERE id={Code}", conn);
                var imgDt = new DataTable();
                imgAdapter.Fill(imgDt);
                EnsureClose(conn);

                if (imgDt.Rows.Count > 0 && imgDt.Rows[0]["image"] != DBNull.Value)
                {
                    _imageBytes = (byte[])imgDt.Rows[0]["image"];
                    using (var ms = new MemoryStream(_imageBytes))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        picImage.Source = bmp;
                    }
                }
                else
                {
                    picImage.Source = null;
                    _imageBytes = null;
                }
            }
            catch { }

            TabControl1.SelectedIndex = 0;
        }

        #endregion

        #region DataGrid Events

        private void dgvGrps_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvGrps.SelectedItem is TermGroupItem item)
            {
                Code = item.Id;
                Navigate($"SELECT * FROM PM_TermGroups WHERE id={Code}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Image Events

        private void lnkImgAdd_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Images|*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.png|All Files|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    _imageBytes = File.ReadAllBytes(dialog.FileName);
                    var bmp = new BitmapImage(new Uri(dialog.FileName));
                    picImage.Source = bmp;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الصورة: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lnkImgClr_Click(object sender, MouseButtonEventArgs e)
        {
            picImage.Source = null;
            _imageBytes     = null;
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
            EnsureClose(conn1);
        }

        #endregion
    }

    public class TermGroupItem
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
    }
}