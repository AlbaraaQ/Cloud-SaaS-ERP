using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Core;
using log4net;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmFonudation : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code;
        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);
        private bool _isLoaded;
        private static readonly string[] CrTypeItems = new[]
        {
            "CRN", "MOM", "MLS", "700", "SAG", "OTH"
        };

        #endregion

        #region Constructor

        public frmFonudation()
        {
            conn = MainClass.ConnObj();
            Code = -1;
            InitializeComponent();
            _isLoaded = false;
            CmbCrtype.ItemsSource = CrTypeItems;
            CmbCrtype.SelectedIndex = 0;
        }

        #endregion

        #region Load

        private void frmFonudation_Load(object sender, RoutedEventArgs e)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select * from Foundation", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    DataRow row = dataTable.Rows[0];

                    Code = SafeInt(row["id"]);

                    txtNameAr.Text = SafeString(row["nameA"]);
                    txtNameEn.Text = SafeString(row["nameE"]);
                    txtField.Text = SafeString(row["FieldA"]);
                    txtFieldEn.Text = SafeString(row["FieldE"]);
                    txtBusnNo.Text = SafeString(row["bsn_no"]);
                    txtcountry.Text = SafeString(row["country"]);
                    txtAddress.Text = SafeString(row["Address"]);
                    txtTel.Text = SafeString(row["Tel"]);
                    txtMobile.Text = SafeString(row["Mobile"]);
                    txtcity.Text = SafeString(row["city"]);
                    txtEmail.Text = SafeString(row["Email"]);
                    txtWebsite.Text = SafeString(row["website"]);
                    TxtArea.Text = SafeString(row["Area"]);
                    txtPlotIdentification.Text = SafeString(row["PlotIdentification"]);
                    txtBuildingNumber.Text = SafeString(row["BuildingNumber"]);
                    txtStreetName.Text = SafeString(row["StreetName"]);
                    txtAdditionalStreetName.Text = SafeString(row["AdditionalStreetName"]);
                    txtDistrict.Text = SafeString(row["District"]);
                    txtPostalZone.Text = SafeString(row["PostalZone"]);

                    string crtypeValue = SafeString(row["crtype"]);
                    if (!string.IsNullOrEmpty(crtypeValue))
                        CmbCrtype.SelectedItem = crtypeValue;

                    if (row["EnableE_Invoice"] != DBNull.Value)
                        chkEInvoice.IsChecked = Convert.ToBoolean(row["EnableE_Invoice"]);
                    else
                        chkEInvoice.IsChecked = true;

                    if (row["EgyEInvoice"] != DBNull.Value)
                        chkEgyEInvoice.IsChecked = Convert.ToBoolean(row["EgyEInvoice"]);
                    else
                        chkEgyEInvoice.IsChecked = false;

                    try
                    {
                        txtTaxNo.Text = SafeString(row["tax_no"]);
                    }
                    catch { }

                    if (row["Logo"] != DBNull.Value)
                    {
                        byte[] logoBytes = (byte[])row["Logo"];
                        picLogo.Source = ByteArrayToImage(logoBytes);
                    }
                }

                CheckInvoiceStatus();

                _isLoaded = true;

                chkEgyEInvoice.IsEnabled = MainClass.EmpNo <= 0;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isArabic = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(txtNameAr.Text))
                {
                    MessageBox.Show(isArabic ? "يجب ادخال اسم المنشأة" : "Enter company name",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNameAr.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtField.Text))
                {
                    MessageBox.Show(isArabic ? "يجب ادخال النشاط التجاري" : "Enter the field",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtField.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("يجب ادخال البريد الإلكتروني",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Common.IsValidEmailFormat(txtEmail.Text))
                {
                    MessageBox.Show("البريد الإلكتروني غير صحيح",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtMobile.Text))
                {
                    MessageBox.Show(isArabic ? "يجب ادخال رقم الجوال" : "Enter mobile",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtMobile.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtcountry.Text))
                {
                    MessageBox.Show(isArabic ? "يجب ادخال البلد" : "Enter country",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtcountry.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtcity.Text))
                {
                    MessageBox.Show(isArabic ? "يجب ادخال المدينة" : "Enter city",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtcity.Focus();
                    return;
                }

                EnsureConnectionOpen();

                SqlCommand sqlCommand;

                if (Code != -1)
                {
                    sqlCommand = new SqlCommand(
                        "update Foundation set " +
                        "nameA=@nameA, nameE=@nameE, FieldA=@FieldA, FieldE=@FieldE, " +
                        "bsn_no=@bsn_no, country=@country, Address=@Address, " +
                        "Tel=@Tel, Mobile=@Mobile, city=@city, " +
                        "Email=@Email, website=@website, Logo=@Logo, " +
                        "tax_no=@tax_no, EnableE_Invoice=@EnableE_Invoice, " +
                        "Area=@Area, PlotIdentification=@PlotIdentification, " +
                        "BuildingNumber=@BuildingNumber, StreetName=@StreetName, " +
                        "AdditionalStreetName=@AdditionalStreetName, " +
                        "District=@District, PostalZone=@PostalZone, " +
                        "EgyEInvoice=@EgyEInvoice, crtype=@crtype " +
                        "where id=" + Code, conn);

                    Logger.Info("تم تعديل بيانات المنشأة بواسطة " + MainClass.UserName);
                }
                else
                {
                    sqlCommand = new SqlCommand(
                        "insert into Foundation(" +
                        "id, nameA, nameE, FieldA, FieldE, bsn_no, country, " +
                        "Address, Tel, Mobile, city, Email, website, Logo, " +
                        "tax_no, EnableE_Invoice, Area, PlotIdentification, " +
                        "BuildingNumber, StreetName, AdditionalStreetName, " +
                        "District, PostalZone, EgyEInvoice, crtype) " +
                        "values (1, @nameA, @nameE, @FieldA, @FieldE, @bsn_no, " +
                        "@country, @Address, @Tel, @Mobile, @city, @Email, " +
                        "@website, @Logo, @tax_no, @EnableE_Invoice, " +
                        "@Area, @PlotIdentification, @BuildingNumber, " +
                        "@StreetName, @AdditionalStreetName, @District, " +
                        "@PostalZone, @EgyEInvoice, @crtype)", conn);

                    Logger.Info("تم حفظ بيانات المنشأة بواسطة " + MainClass.UserName);
                }

                sqlCommand.Parameters.Add("@nameA", SqlDbType.NVarChar).Value = txtNameAr.Text;
                sqlCommand.Parameters.Add("@nameE", SqlDbType.NVarChar).Value = txtNameEn.Text;
                sqlCommand.Parameters.Add("@FieldA", SqlDbType.NVarChar).Value = txtField.Text;
                sqlCommand.Parameters.Add("@FieldE", SqlDbType.NVarChar).Value = txtFieldEn.Text;
                sqlCommand.Parameters.Add("@bsn_no", SqlDbType.NVarChar).Value = txtBusnNo.Text;
                sqlCommand.Parameters.Add("@country", SqlDbType.NVarChar).Value = txtcountry.Text;
                sqlCommand.Parameters.Add("@Address", SqlDbType.NVarChar).Value = txtAddress.Text;
                sqlCommand.Parameters.Add("@Tel", SqlDbType.NVarChar).Value = txtTel.Text;
                sqlCommand.Parameters.Add("@Mobile", SqlDbType.NVarChar).Value = txtMobile.Text;
                sqlCommand.Parameters.Add("@city", SqlDbType.NVarChar).Value = txtcity.Text;
                sqlCommand.Parameters.Add("@Email", SqlDbType.NVarChar).Value = txtEmail.Text;
                sqlCommand.Parameters.Add("@website", SqlDbType.NVarChar).Value = txtWebsite.Text;
                sqlCommand.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = txtTaxNo.Text;
                sqlCommand.Parameters.Add("@Area", SqlDbType.NVarChar).Value = TxtArea.Text;
                sqlCommand.Parameters.Add("@PlotIdentification", SqlDbType.NVarChar).Value = txtPlotIdentification.Text;
                sqlCommand.Parameters.Add("@BuildingNumber", SqlDbType.NVarChar).Value = txtBuildingNumber.Text;
                sqlCommand.Parameters.Add("@StreetName", SqlDbType.NVarChar).Value = txtStreetName.Text;
                sqlCommand.Parameters.Add("@AdditionalStreetName", SqlDbType.NVarChar).Value = txtAdditionalStreetName.Text;
                sqlCommand.Parameters.Add("@District", SqlDbType.NVarChar).Value = txtDistrict.Text;
                sqlCommand.Parameters.Add("@PostalZone", SqlDbType.NVarChar).Value = txtPostalZone.Text;
                sqlCommand.Parameters.Add("@crtype", SqlDbType.NVarChar).Value =
                    CmbCrtype.SelectedItem?.ToString() ?? string.Empty;

                // الشعار
                if (picLogo.Source != null)
                {
                    sqlCommand.Parameters.Add("@Logo", SqlDbType.Image).Value = ImageToByteArray(picLogo.Source);
                }
                else
                {
                    sqlCommand.Parameters.Add("@Logo", SqlDbType.Image).Value = DBNull.Value;
                }

                sqlCommand.Parameters.Add("@EnableE_Invoice", SqlDbType.Bit).Value =
                    chkEInvoice.IsChecked == true ? 1 : 0;

                sqlCommand.Parameters.Add("@EgyEInvoice", SqlDbType.Bit).Value =
                    chkEgyEInvoice.IsChecked == true ? 1 : 0;

                sqlCommand.ExecuteNonQuery();

                MessageBox.Show(isArabic ? "تم الحفظ بنجاح" : "Saved",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                Code = 1;
            }
            catch (Exception ex)
            {
                bool isEnglish = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase);

                string errorMsg = isEnglish
                    ? "Error during saving" + Environment.NewLine + "Error details: " + ex.Message
                    : "خطأ أثناء الحفظ" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message;

                MessageBox.Show(errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Open Logo File

        private void btnOpenFile_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "All Files|*.*|JPEG|*.jpg|BMP|*.bmp|GIF|*.gif|TIFF|*.tiff|PNG|*.png"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    txtLogoPath.Text = openFileDialog.FileName;

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    picLogo.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Delete Logo

        private void BtnDeleteLogo_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    picLogo.Source = null;
                    txtLogoPath.Text = string.Empty;
                    return;
                }

                EnsureConnectionOpen();

                SqlCommand sqlCommand = new SqlCommand(
                    "update Foundation set Logo=@logo where id=" + Code, conn);
                sqlCommand.Parameters.Add("@logo", SqlDbType.Image).Value = DBNull.Value;
                sqlCommand.ExecuteNonQuery();

                picLogo.Source = null;
                txtLogoPath.Text = string.Empty;

                MessageBox.Show("تمت العملية بنجاح", "",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region CheckBox Events

        private void chkEInvoice_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (chkEInvoice == null || chkEgyEInvoice == null) return;

            if (chkEInvoice.IsChecked == true)
                chkEgyEInvoice.IsChecked = false;
        }

        private void chkEgyEInvoice_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkEgyEInvoice.IsChecked == true)
                chkEInvoice.IsChecked = false;
        }

        #endregion

        #region Check Invoice Status

        private void CheckInvoiceStatus()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select id, tax from Inv where branch=" + MainClass.BranchNo +
                    " and IS_deleted=0 and (inv_type=2 or inv_type=3) and tax > 0", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                chkEInvoice.IsEnabled = dataTable.Rows.Count <= 0;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Close / Print

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // محجوز للطباعة مستقبلاً
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

        private string SafeString(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;
            return value.ToString().Trim();
        }

        private int SafeInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return -1;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            if (double.TryParse(value.ToString(), out double dblResult))
                return (int)dblResult;

            return -1;
        }

        private BitmapImage ByteArrayToImage(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
                return null;

            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        private byte[] ImageToByteArray(ImageSource imageSource)
        {
            if (imageSource == null)
                return null;

            BitmapSource bitmapSource = imageSource as BitmapSource;
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

        private void ShowError(string message, string title = "خطأ")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}