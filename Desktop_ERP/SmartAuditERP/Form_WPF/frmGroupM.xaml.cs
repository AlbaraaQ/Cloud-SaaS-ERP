using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmGroupM : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Index;

        #endregion

        #region Constructor

        public frmGroupM()
        {
            conn = MainClass.ConnObj();
            Index = -1;
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmActs_Load(object sender, RoutedEventArgs e)
        {
            LoadDG();
            LoadNextNo();
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtCode.Text = string.Empty;
            txtNameEN.Text = string.Empty;
            txtName.Text = string.Empty;
            txtHourPrice.Text = string.Empty;
            txtHalfHOffer.Text = string.Empty;
            txtHourOffer.Text = string.Empty;
            txtHalfHPrice.Text = string.Empty;
            Index = -1;
            picImage.Source = null;
        }

        #endregion

        #region Load Grid

        private void LoadDG()
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select * from GroupMarine where IsDeleted=0", conn);

            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            dgvData.ItemsSource = null;
            dgvData.ItemsSource = dataTable.DefaultView;

            LoadNextNo();
        }

        #endregion

        #region Load Next Number

        private void LoadNextNo()
        {
            txtNo.Text = string.Empty;
            int nextNo = 1;

            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select id from GroupMarine", conn);

            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            if (dataTable.Rows.Count > 0)
            {
                try
                {
                    nextNo = dataTable.Rows.Count + 1;
                }
                catch { }
            }

            txtNo.Text = nextNo.ToString();
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtCode.Text))
                {
                    DXMessageBox.Show("ادخل الفئة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCode.Focus();
                    return;
                }

                EnsureConnectionOpen();

                bool isUpdate = false;
                SqlCommand sqlCommand;

                if (Index == -1)
                {
                    SqlDataAdapter checkAdapter = new SqlDataAdapter(
                        "select id from GroupMarine where name='" + txtCode.Text + "' and IsDeleted=0", conn);

                    DataTable checkTable = new DataTable();
                    checkAdapter.Fill(checkTable);

                    if (checkTable.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "الفئة تم ادخالها مسبقا"
                            : "Group is previously inserted";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtCode.Focus();
                        return;
                    }

                    int newId = 0;
                    double.TryParse(txtNo.Text, out double noVal);
                    newId = (int)noVal;

                    sqlCommand = new SqlCommand(
                        "insert into GroupMarine(id,code,name,nameEN,HourPrice,HalfHPrice," +
                        "OfferHour,OfferHalf,IsDeleted,image) " +
                        "values('" + newId + "','" + txtCode.Text + "','" + txtName.Text +
                        "','" + txtNameEN.Text + "','" + txtHourPrice.Text + "','" +
                        txtHalfHPrice.Text + "','" + txtHourOffer.Text + "','" +
                        txtHalfHOffer.Text + "',0,@image)", conn);
                }
                else
                {
                    isUpdate = true;
                    sqlCommand = new SqlCommand(
                        "update GroupMarine set code='" + txtCode.Text +
                        "',name='" + txtName.Text +
                        "',nameEN='" + txtNameEN.Text +
                        "',HourPrice='" + txtHourPrice.Text +
                        "',HalfHPrice='" + txtHalfHPrice.Text +
                        "',OfferHour='" + txtHourOffer.Text +
                        "',OfferHalf='" + txtHalfHOffer.Text +
                        "',image=@image where id=" + Index, conn);
                }

                // إضافة الصورة
                if (picImage.Source != null)
                    sqlCommand.Parameters.Add("@image", SqlDbType.Image).Value = ImageSourceToBytes(picImage.Source);
                else
                    sqlCommand.Parameters.Add("@image", SqlDbType.Image).Value = DBNull.Value;

                sqlCommand.ExecuteNonQuery();

                // حذف وإعادة إدراج فترات الإيجار
                int groupId = 0;
                double.TryParse(txtNo.Text, out double gidVal);
                groupId = (int)gidVal;

                new SqlCommand(
                    "delete from RentPeriodSub where MGroupID=" + groupId, conn)
                    .ExecuteNonQuery();

                SqlCommand periodCmd1 = new SqlCommand(
                    "insert into RentPeriodSub(MGroupID,code,periodID,rent,offer) " +
                    "values(@MGroupID,@code,@periodID,@rent,@offer)", conn);
                double.TryParse(txtHourPrice.Text, out double hourRent);
                double.TryParse(txtHourOffer.Text, out double hourOffer);
                periodCmd1.Parameters.AddWithValue("@MGroupID", groupId);
                periodCmd1.Parameters.AddWithValue("@code", txtCode.Text);
                periodCmd1.Parameters.AddWithValue("@periodID", 2);
                periodCmd1.Parameters.AddWithValue("@rent", hourRent);
                periodCmd1.Parameters.AddWithValue("@offer", (int)hourOffer);
                periodCmd1.ExecuteNonQuery();

                SqlCommand periodCmd2 = new SqlCommand(
                    "insert into RentPeriodSub(MGroupID,code,periodID,rent,offer) " +
                    "values(@MGroupID,@code,@periodID,@rent,@offer)", conn);
                double.TryParse(txtHalfHPrice.Text, out double halfRent);
                double.TryParse(txtHalfHOffer.Text, out double halfOffer);
                periodCmd2.Parameters.AddWithValue("@MGroupID", groupId);
                periodCmd2.Parameters.AddWithValue("@code", txtCode.Text);
                periodCmd2.Parameters.AddWithValue("@periodID", 1);
                periodCmd2.Parameters.AddWithValue("@rent", halfRent);
                periodCmd2.Parameters.AddWithValue("@offer", (int)halfOffer);
                periodCmd2.ExecuteNonQuery();

                LoadDG();

                frmSavedMsg savedMsgForm = new frmSavedMsg();
                if (isUpdate)
                    savedMsgForm.lblSave.Text = "تم حفظ التعديلات بنجاح...";

                savedMsgForm.ShowDialog();

                if (savedMsgForm.Pressed == 1)
                {
                    CLR();
                    txtCode.Focus();
                }
                else if (savedMsgForm.Pressed == 3)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ" + Environment.NewLine +
                    "تفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Index == -1)
                {
                    DXMessageBox.Show("اختر الفئة ليتم حذفها", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureConnectionOpen();

                if (!string.IsNullOrEmpty(txtCode.Text))
                {
                    SqlDataAdapter checkAdapter = new SqlDataAdapter(
                        "select id from Marine where Groupcode='" + txtCode.Text +
                        "' and IS_Deleted=0", conn);

                    DataTable checkTable = new DataTable();
                    checkAdapter.Fill(checkTable);

                    if (checkTable.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "هذه الفئة لها ارتباطات فرعية لايمكن حذفها"
                            : "This Group previously used in Currencies";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (DXMessageBox.Show("هل انت متأكد من حذف الفئة", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    new SqlCommand(
                        "update GroupMarine set IsDeleted=1 where id=" + Index, conn)
                        .ExecuteNonQuery();

                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDG();
                    CLR();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف" + Environment.NewLine +
                    "تفاصيل الخطأ: " + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from GroupMarine order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from GroupMarine order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from GroupMarine where id>" + Index + " order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from GroupMarine where id<" + Index + " order by id desc");
        }

        private void Navigate(string sqlQuery)
        {
            dgvData.UnselectAll();
            SqlCommand sqlCommand = new SqlCommand(sqlQuery, conn);
            EnsureConnectionOpen();
            ReadData(sqlCommand.ExecuteReader());
            EnsureConnectionClosed();
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows)
                return;

            reader.Read();
            CLR();

            double.TryParse(reader["id"]?.ToString(), out double idVal);
            Index = (int)idVal;

            txtNo.Text = Index.ToString();
            txtCode.Text = reader["code"]?.ToString() ?? string.Empty;

            try
            {
                txtName.Text = reader["name"]?.ToString() ?? string.Empty;
                txtNameEN.Text = reader["nameEN"]?.ToString() ?? string.Empty;
                txtHourPrice.Text = reader["HourPrice"]?.ToString() ?? string.Empty;
                txtHalfHPrice.Text = reader["HalfHPrice"]?.ToString() ?? string.Empty;
                txtHourOffer.Text = reader["OfferHour"]?.ToString() ?? string.Empty;
                txtHalfHOffer.Text = reader["OfferHalf"]?.ToString() ?? string.Empty;
            }
            catch { }

            reader.Close();

            LoadImage(Index);
        }

        private void LoadImage(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select image from GroupMarine where id=" + id, conn);
                DataTable table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0 && table.Rows[0]["image"] != DBNull.Value)
                {
                    byte[] imageBytes = (byte[])table.Rows[0]["image"];
                    picImage.Source = BytesToImageSource(imageBytes);
                }
                else
                {
                    picImage.Source = null;
                }
            }
            catch { }
        }

        #endregion

        #region Grid Click

        private void dgvData_CellClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvData.SelectedItem is DataRowView rowView)
            {
                int.TryParse(rowView["id"]?.ToString(), out int selectedId);
                if (selectedId > 0)
                {
                    Index = selectedId;
                    Navigate("select * from GroupMarine where id=" + Index);
                    txtNo.Text = Index.ToString();
                }
            }
        }

        #endregion

        #region Image

        private void lnkImgAdd_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "All Files|*.*|JPEG|*.jpg|BMP|*.bmp|GIF|*.gif|TIFF|*.tiff|PNG|*.png"
                };

                if (openDialog.ShowDialog() == true)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openDialog.FileName, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    picImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lnkImgClr_LinkClicked(object sender, MouseButtonEventArgs e)
        {
            picImage.Source = null;
        }

        #endregion

        #region Add Period

        private void btnAddPeriod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmAddPeriod addPeriodForm = new frmAddPeriod();
                double.TryParse(txtNo.Text, out double groupIdVal);
                addPeriodForm.GroupId = (int)groupIdVal;
                addPeriodForm.Show();
                addPeriodForm.Activate();
                addPeriodForm.LoadGroupPeriods();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Other Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            LoadDG();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // محجوز للطباعة مستقبلاً
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
        }

        #endregion

        #region Helpers

        private void EnsureConnectionOpen()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        private ImageSource BytesToImageSource(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        private byte[] ImageSourceToBytes(ImageSource source)
        {
            if (source == null)
                return null;

            BitmapSource bitmapSource = source as BitmapSource;
            if (bitmapSource == null)
                return null;

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        #endregion
    }
}