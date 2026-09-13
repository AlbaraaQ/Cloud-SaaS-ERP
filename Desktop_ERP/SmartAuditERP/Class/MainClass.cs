using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartAuditERP
{
    /// <summary>
    /// الكلاس الرئيسي للإعدادات العامة والاتصال بقاعدة البيانات
    /// متوافق مع WPF بالكامل مع دعم التوافق العكسي لـ WinForms القديم
    /// </summary>
    public static class MainClass
    {
        // ══════════════════════════════════════════════
        #region Database Connection
        // ══════════════════════════════════════════════

        public static SqlConnection conn;

        public static string Server = @".\sqlexpress";
        public static string Database = "";
        public static bool UseServerAuth = false;
        public static string NetPwd = "";
        public static string NetUserId = "";
        public static int UserTreasury = 0;
        public static int Conn_type = 1;
        public static string DataBaseName = "";

        public static string connstr =
            $"server={Server};database={Database};trusted_connection=true";

        public static string originalConnStr = connstr;
        // في MainClass.cs أضف هذا الحقل
        public static DateTime LoginDate { get; set; } = DateTime.Now;
        /// <summary>
        /// إنشاء اتصال جديد بقاعدة البيانات
        /// </summary>
        public static SqlConnection ConnObj()
        {
            conn = new SqlConnection(connstr);
            return conn;
        }

        #endregion

        // في MainClass.cs

        public static string CurrentVersion;// = "2.0.0.41";

        public static string GITHUB_USER
            => SecureVault.GetGitHubUser();

        public static string GITHUB_REPO
            => SecureVault.GetGitHubRepo();

        // ══════════════════════════════════════════════
        #region User & Branch Info
        // ══════════════════════════════════════════════

        public static bool IsTrial = true;
        public static int Features = 15;
        public static int UserID = 1;
        public static string UserName = "";
        public static int EmpNo = -1;
        public static int BranchNo = -1;
        public static string BranchCode = "1";
        public static string BranchName = "";

        public static DateTime lastCashercloseDate;

        #endregion

        // ══════════════════════════════════════════════
        #region Window State (WPF)
        // ══════════════════════════════════════════════

        /// <summary>
        /// حالة النافذة - WPF WindowState
        /// الاستخدام: this.WindowState = MainClass.Window_State;
        /// </summary>
        public static System.Windows.WindowState Window_State = System.Windows.WindowState.Normal;

        #endregion

        // ══════════════════════════════════════════════
        #region Language & Reports
        // ══════════════════════════════════════════════

        public static string Language = "ar";
        public static string ReportsPath = "";
        public static string ReportsPrinter = "";

        #endregion

        // ══════════════════════════════════════════════
        #region Application Info
        // ══════════════════════════════════════════════

        public static string AppNameAR =
            "نظام Auditor E-Invoicing للمحاسبة العامة";
        public static string AppNameEN =
            "Auditor E-Invoicing Accounting Application";

        public static string PaymentName = "شبكة";

        #endregion

        // ══════════════════════════════════════════════
        #region License
        // ══════════════════════════════════════════════

        public static DateTime LicenseExpire;
        public static int RemainingDays = 0;
        public static string LicenseFeature = "000000";
        public static bool ReturnYearPreviews = false;

        #endregion

        // ══════════════════════════════════════════════
        #region NeoLeap Integration
        // ══════════════════════════════════════════════

        public static bool IsNeoLeapActive = false;
        public static string NeoLeapPort = "";
        public static bool NeoLeapEnableReceiptPrint = false;
        public static string NeoLeapMerchantToken = "";

        #endregion

        // ══════════════════════════════════════════════
        #region ZATCA
        // ══════════════════════════════════════════════

        public static string ZatcafilePath = "";

        #endregion

        // ══════════════════════════════════════════════
        #region Images (WPF + WinForms Compatible)
        // ══════════════════════════════════════════════

        /// <summary>
        /// خلفية التطبيق الرئيسية (WPF BitmapImage)
        /// </summary>
        public static BitmapImage MainBG { get; set; }

        /// <summary>
        /// خلفية شاشة تسجيل الدخول (WPF BitmapImage)
        /// </summary>
        public static BitmapImage LoginBG { get; set; }

        /// <summary>
        /// تحويل System.Drawing.Image إلى مصفوفة بايت (للتوافق مع الكود القديم)
        /// </summary>
        public static byte[] Image2Arr(System.Drawing.Image img)
        {
            if (img == null) return null;
            try
            {
                using var memoryStream = new MemoryStream();
                img.Save(memoryStream, ImageFormat.Jpeg);
                return memoryStream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل BitmapSource (WPF) إلى مصفوفة بايت
        /// استخدم هذا عند الحفظ في قاعدة البيانات من WPF Image control
        /// مثال: MainClass.Image2Arr(picImage.Source as BitmapSource)
        /// </summary>
        public static byte[] Image2Arr(BitmapSource imageSource)
        {
            if (imageSource == null) return null;
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageSource));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل مصفوفة بايت إلى System.Drawing.Image (للتوافق مع الكود القديم)
        /// </summary>
        public static System.Drawing.Image Arr2Image(byte[] arr)
        {
            if (arr == null || arr.Length == 0) return null;
            try
            {
                return System.Drawing.Image.FromStream(new MemoryStream(arr));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل مصفوفة بايت إلى BitmapImage (WPF)
        /// استخدم هذا عند عرض الصورة في WPF Image control
        /// مثال: picImage.Source = MainClass.Arr2BitmapImage(arr);
        /// </summary>
        public static BitmapImage Arr2BitmapImage(byte[] arr)
        {
            if (arr == null || arr.Length == 0) return null;
            try
            {
                using var ms = new MemoryStream(arr);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل مصفوفة بايت إلى BitmapImage (WPF)
        /// نفس Arr2BitmapImage لكن باسم مختلف للتوافق
        /// </summary>
        public static BitmapImage ByteArrayToBitmapImage(byte[] imageData)
        {
            return Arr2BitmapImage(imageData);
        }

        /// <summary>
        /// تحويل BitmapImage إلى مصفوفة بايت
        /// </summary>
        public static byte[] BitmapImageToByteArray(BitmapImage bitmapImage)
        {
            if (bitmapImage == null) return null;
            try
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل System.Drawing.Bitmap إلى BitmapImage (WPF)
        /// استخدم هذا عند تحميل صور من Resources
        /// مثال: MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.MainBG1);
        /// </summary>
        public static BitmapImage DrawingBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) return null;
            try
            {
                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تحويل System.Drawing.Image إلى BitmapImage (WPF)
        /// استخدم هذا إذا كان المصدر Image وليس Bitmap
        /// مثال: MainClass.DrawingImageToBitmapImage(someDrawingImage);
        /// </summary>
        public static BitmapImage DrawingImageToBitmapImage(System.Drawing.Image image)
        {
            if (image == null) return null;
            try
            {
                using var bitmap = new System.Drawing.Bitmap(image);
                return DrawingBitmapToBitmapImage(bitmap);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region User Settings
        // ══════════════════════════════════════════════

        private static string _name = "Tahoma";
        private static string _size = "8";
        private static int _fcolor = System.Drawing.Color.Black.ToArgb();
        private static int _bcolor = System.Drawing.Color.WhiteSmoke.ToArgb();
        private static string _style = "عادي";

        /// <summary>
        /// تطبيق إعدادات المستخدم على نافذة WPF
        /// </summary>
        public static void DoApplyUserSett(System.Windows.Window window)
        {
            // يمكن تطبيق الخط والحجم والاتجاه هنا لاحقاً
        }

        /// <summary>
        /// تطبيق إعدادات المستخدم على نموذج WinForms (للتوافق مع النوافذ القديمة)
        /// </summary>
        public static void DoApplyUserSett(System.Windows.Forms.Form frm)
        {
            // فارغة — للتوافق مع الواجهات القديمة
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Permissions
        // ══════════════════════════════════════════════

        /// <summary>
        /// تطبيق الصلاحيات على نافذة WPF
        /// </summary>
        public static void ApplyPermissionToForm(System.Windows.Window window)
        {
            try
            {
                string formName = window.GetType().Name;

                // معالجة أسماء النماذج المخصصة
                if (window.Tag != null)
                {
                    formName = window.Tag.ToString() switch
                    {
                        "InvSale" => "frmInvSale",
                        "SalePurch3" => "frmSalePurch3",
                        "SalePurch4" => "frmSalePurch4",
                        "SalePurch5" => "frmSalePurch5",
                        "frmMezanMorg3a2" => "frmMezanMorg3a2",
                        "frmMezanyaArba72" => "frmMezanyaArba72",
                        "frmMezanyaArba73" => "frmMezanyaArba73",
                        "frmMezanyaArba74" => "frmMezanyaArba74",
                        "Quotation" => "frmSalePurch6",
                        "frmcontract" => "frmcontract",
                        _ => formName
                    };
                }

                if (string.Equals(formName.Trim(), "frmItems",
                    StringComparison.OrdinalIgnoreCase))
                    formName = "frmcurrency";

                // جلب الصلاحيات من قاعدة البيانات
                using var localConn = ConnObj();
                localConn.Open();

                var adapter = new SqlDataAdapter(
                    $"SELECT IS_New, IS_Save, IS_Delete, IS_Search, IS_Print, Forms.id " +
                    $"FROM User_Permissions, Forms " +
                    $"WHERE User_Permissions.Form_id=Forms.id " +
                    $"AND Forms.FormName='{formName.Trim()}' " +
                    $"AND user_id={UserID}", localConn);

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                DataRow perm = dt.Rows[0];

                bool canNew = Convert.ToBoolean(perm["IS_New"]);
                bool canSave = Convert.ToBoolean(perm["IS_Save"]);
                bool canDelete = Convert.ToBoolean(perm["IS_Delete"]);
                bool canSearch = Convert.ToBoolean(perm["IS_Search"]);
                bool canPrint = Convert.ToBoolean(perm["IS_Print"]);

                // تطبيق الصلاحيات على أزرار WPF
                ApplyWpfPermissions(window, canNew, canSave, canDelete, canSearch, canPrint);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تطبيق الصلاحيات: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق الصلاحيات على أزرار WPF بالبحث في الشجرة المرئية
        /// </summary>
        private static void ApplyWpfPermissions(DependencyObject parent,
            bool canNew, bool canSave, bool canDelete, bool canSearch, bool canPrint)
        {
            if (parent == null) return;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.Button btn &&
                    !string.IsNullOrEmpty(btn.Name))
                {
                    string name = btn.Name;
                    bool disable = false;

                    if (!canNew && name == "btnNew") disable = true;
                    if (!canSave && (name == "btnSave" || name == "btnSavePrint"))
                        disable = true;
                    if (!canDelete && name == "btnDelete") disable = true;
                    if (!canPrint && (name == "btnPrint" || name == "btnPreview" ||
                                     name == "btnView"))
                        disable = true;
                    if (!canSearch && (name == "btnSearch" || name == "btnLast" ||
                                      name == "btnNext" || name == "btnPrevious" ||
                                      name == "btnShow" || name == "btnFirst" ||
                                      name == "btnInvSrch"))
                        disable = true;

                    if (disable)
                    {
                        btn.IsEnabled = false;
                        btn.Opacity = 0.5;
                    }
                }

                // تكرار للعناصر الفرعية
                ApplyWpfPermissions(child, canNew, canSave, canDelete, canSearch, canPrint);
            }
        }

        /// <summary>
        /// تطبيق الصلاحيات على نموذج WinForms القديم (للتوافق)
        /// </summary>
        public static void ApplyPermissionToForm(System.Windows.Forms.Form frm)
        {
            try
            {
                string formName = frm.Name;

                if (frm.Tag != null)
                {
                    formName = frm.Tag.ToString() switch
                    {
                        "InvSale" => "frmInvSale",
                        "SalePurch3" => "frmSalePurch3",
                        "SalePurch4" => "frmSalePurch4",
                        "SalePurch5" => "frmSalePurch5",
                        "frmMezanMorg3a2" => "frmMezanMorg3a2",
                        "frmMezanyaArba72" => "frmMezanyaArba72",
                        "frmMezanyaArba73" => "frmMezanyaArba73",
                        "frmMezanyaArba74" => "frmMezanyaArba74",
                        "Quotation" => "frmSalePurch6",
                        "frmcontract" => "frmcontract",
                        _ => formName
                    };
                }

                if (string.Equals(formName.Trim(), "frmItems",
                    StringComparison.OrdinalIgnoreCase))
                    formName = "frmcurrency";

                using var localConn = ConnObj();
                localConn.Open();

                var adapter = new SqlDataAdapter(
                    $"SELECT IS_New, IS_Save, IS_Delete, IS_Search, IS_Print, Forms.id " +
                    $"FROM User_Permissions, Forms " +
                    $"WHERE User_Permissions.Form_id=Forms.id " +
                    $"AND Forms.FormName='{formName.Trim()}' " +
                    $"AND user_id={UserID}", localConn);

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                DataRow perm = dt.Rows[0];
                bool canNew = Convert.ToBoolean(perm["IS_New"]);
                bool canSave = Convert.ToBoolean(perm["IS_Save"]);
                bool canDelete = Convert.ToBoolean(perm["IS_Delete"]);
                bool canSearch = Convert.ToBoolean(perm["IS_Search"]);
                bool canPrint = Convert.ToBoolean(perm["IS_Print"]);

                // البحث عن لوحة الأزرار
                var pnlControls = frm.Controls
                    .Find("pnlOperBtns", searchAllChildren: true);

                if (pnlControls.Length > 0)
                {
                    foreach (System.Windows.Forms.Control ctrl in pnlControls[0].Controls)
                    {
                        if (ctrl is FontAwesome.Sharp.IconButton iconBtn)
                        {
                            ApplyWinFormsIconBtnPermission(
                                iconBtn, canNew, canSave, canDelete, canSearch, canPrint);
                        }
                    }
                }

                // نقطة البيع
                if (string.Equals(formName.Trim(), "frmInvPOS",
                    StringComparison.OrdinalIgnoreCase))
                {
                    var flowPanels = frm.Controls
                        .Find("BtnFlowPnl", searchAllChildren: true);

                    if (flowPanels.Length > 0)
                    {
                        foreach (System.Windows.Forms.Control ctrl in flowPanels[0].Controls)
                        {
                            if (ctrl is System.Windows.Forms.Button btn)
                            {
                                ApplyWinFormsBtnPermission(
                                    btn, canNew, canSave, canDelete, canSearch, canPrint);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تطبيق الصلاحيات: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق صلاحية على IconButton (WinForms)
        /// </summary>
        private static void ApplyWinFormsIconBtnPermission(
            FontAwesome.Sharp.IconButton btn,
            bool canNew, bool canSave, bool canDelete,
            bool canSearch, bool canPrint)
        {
            string name = btn.Name;
            bool disable = false;

            if (!canNew && name == "btnSave") disable = true;
            if (!canSave && (name == "btnSave" || name == "btnSavePrint"))
                disable = true;
            if (!canDelete && name == "btnDelete") disable = true;
            if (!canPrint && (name == "btnPrint" || name == "btnPreview" ||
                              name == "btnView"))
                disable = true;
            if (!canSearch && (name == "btnSearch" || name == "btnLast" ||
                               name == "btnNext" || name == "btnPrevious" ||
                               name == "btnShow" || name == "btnFirst" ||
                               name == "btnInvSrch"))
                disable = true;

            if (disable)
            {
                btn.Enabled = false;
                btn.BackColor = System.Drawing.Color.Gray;
                btn.IconColor = System.Drawing.Color.White;
            }
        }

        /// <summary>
        /// تطبيق صلاحية على Button (WinForms)
        /// </summary>
        private static void ApplyWinFormsBtnPermission(
            System.Windows.Forms.Button btn,
            bool canNew, bool canSave, bool canDelete,
            bool canSearch, bool canPrint)
        {
            string name = btn.Name;
            bool disable = false;

            if (!canNew && name == "btnSave") disable = true;
            if (!canSave && (name == "btnSave" || name == "btnSavePrint"))
                disable = true;
            if (!canDelete && name == "btnDelete") disable = true;
            if (!canPrint && (name == "btnPrint" || name == "btnPreview"))
                disable = true;
            if (!canSearch && (name == "btnSearch" || name == "btnLast" ||
                               name == "btnNext" || name == "btnPrevious" ||
                               name == "btnShow" || name == "btnFirst" ||
                               name == "btnInvSrch"))
                disable = true;

            if (disable)
            {
                btn.Enabled = false;
                btn.BackColor = System.Drawing.Color.Gray;
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region UI Helpers
        // ══════════════════════════════════════════════

        /// <summary>
        /// تفعيل / تعطيل جميع العناصر الفرعية داخل عنصر WPF
        /// </summary>
        public static void EnableControls(UIElement parent, bool IsEnable)
        {
            parent.IsEnabled = IsEnable;
        }

        /// <summary>
        /// تفعيل / تعطيل عناصر WinForms (للتوافق)
        /// </summary>
        public static void EnableControls(System.Windows.Forms.Control Parent, bool IsEnable)
        {
            try
            {
                foreach (System.Windows.Forms.Control control in Parent.Controls)
                {
                    control.Enabled = IsEnable;
                    EnableControls(control, IsEnable);
                }
            }
            catch { }
        }

        /// <summary>
        /// مسح جميع حقول النافذة (WPF)
        /// </summary>
        public static void ClearFormFields(DependencyObject parent)
        {
            if (parent == null) return;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.TextBox tb)
                    tb.Text = "";
                else if (child is System.Windows.Controls.ComboBox cb)
                    cb.SelectedIndex = -1;
                else if (child is DevExpress.Xpf.Editors.DateEdit de)
                    de.DateTime = DateTime.Now;

                ClearFormFields(child);
            }
        }

        /// <summary>
        /// مسح حقول نموذج WinForms القديم (للتوافق)
        /// </summary>
        public static void CLRForm(DependencyObject parent)
        {
            if (parent == null) return;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is System.Windows.Controls.TextBox tb)
                    tb.Text = "";
                else if (child is System.Windows.Controls.ComboBox cb)
                    cb.SelectedIndex = -1;
                else if (child is DevExpress.Xpf.Editors.DateEdit de)
                    de.DateTime = DateTime.Now;

                ClearFormFields(child);
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Input Validation
        // ══════════════════════════════════════════════

        /// <summary>
        /// يسمح فقط بالأرقام والنقطة (WPF - PreviewTextInput)
        /// </summary>
        public static bool IsValidFloatInput(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// يسمح فقط بالأرقام الصحيحة (WPF - PreviewTextInput)
        /// </summary>
        public static bool IsValidIntegerInput(string text)
        {
            return text.All(char.IsDigit);
        }

        /// <summary>
        /// IsFloat - WinForms KeyPress (للتوافق)
        /// </summary>
        public static void IsFloat(System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!char.IsNumber(e.KeyChar) && e.KeyChar != '\b' && e.KeyChar != '.')
                e.Handled = true;
        }

        /// <summary>
        /// ISInteger - WinForms KeyPress (للتوافق)
        /// </summary>
        public static void ISInteger(System.Windows.Forms.KeyPressEventArgs e)
        {
            if (!char.IsNumber(e.KeyChar) && e.KeyChar != '\b')
                e.Handled = true;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Account Code Generation
        // ══════════════════════════════════════════════

        /// <summary>
        /// توليد كود حساب جديد
        /// </summary>
        public static string GenerateCode(int Parent_Code)
        {
            try
            {
                using var localConn = ConnObj();
                localConn.Open();

                string parentStr = Parent_Code.ToString();

                // حالة خاصة: الحسابات التي تبدأ بـ 123 أو 2211
                if (parentStr.StartsWith("123") || parentStr.StartsWith("2211"))
                {
                    var adapter = new SqlDataAdapter(
                        $"SELECT ISNULL(MAX(Code),0) AS Code " +
                        $"FROM Accounts_Index " +
                        $"WHERE ParentCode='{Parent_Code}'", localConn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    int maxCode = Convert.ToInt32(dt.Rows[0]["Code"]);

                    if (maxCode == 0)
                        return $"{Parent_Code}0001";

                    // البحث عن كود غير مستخدم
                    while (true)
                    {
                        var checkAdapter1 = new SqlDataAdapter(
                            $"SELECT Code FROM Accounts_Index WHERE Code='{maxCode}'",
                            localConn);
                        var checkDt1 = new DataTable();
                        checkAdapter1.Fill(checkDt1);

                        var checkAdapter2 = new SqlDataAdapter(
                            $"SELECT id FROM Customers WHERE AccountCode='{maxCode}'",
                            localConn);
                        var checkDt2 = new DataTable();
                        checkAdapter2.Fill(checkDt2);

                        if (checkDt1.Rows.Count == 0 && checkDt2.Rows.Count == 0)
                            return maxCode.ToString();

                        maxCode++;
                    }
                }

                // الحالة العامة
                string sql;
                if (parentStr.StartsWith("224"))
                {
                    sql = $"SELECT MAX(Acc.Code) FROM (" +
                          $"SELECT Code, ParentCode, MAX(Code)+1 AS AccCode " +
                          $"FROM Accounts_Index GROUP BY Code, ParentCode" +
                          $") AS Acc " +
                          $"WHERE Acc.ParentCode='{Parent_Code}' " +
                          $"AND Acc.AccCode NOT IN " +
                          $"(SELECT AccCode FROM Employees WHERE AccCode!=-1) " +
                          $"AND Acc.AccCode NOT IN " +
                          $"(SELECT Code FROM Accounts_Index)";

                    var adp = new SqlDataAdapter(sql, localConn);
                    var dtCheck = new DataTable();
                    adp.Fill(dtCheck);

                    if (dtCheck.Rows.Count > 0 &&
                        (string.IsNullOrEmpty(dtCheck.Rows[0][0]?.ToString()) ||
                         dtCheck.Rows[0][0] == DBNull.Value))
                    {
                        sql = $"SELECT MAX(Code) FROM Accounts_Index " +
                              $"WHERE ParentCode='{Parent_Code}'";
                    }
                }
                else
                {
                    sql = $"SELECT MAX(Code) FROM Accounts_Index " +
                          $"WHERE ParentCode='{Parent_Code}'";
                }

                var finalAdapter = new SqlDataAdapter(sql, localConn);
                var finalDt = new DataTable();
                finalAdapter.Fill(finalDt);

                if (finalDt.Rows.Count == 0 ||
                    string.IsNullOrEmpty(finalDt.Rows[0][0]?.ToString()))
                    return $"{Parent_Code}0001";

                double nextCode = Convert.ToDouble(finalDt.Rows[0][0]) + 1;
                return nextCode.ToString("F0");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في GenerateCode: {ex.Message}");
                return "";
            }
        }

        #endregion

        // ══════════════════════════════════════════════
        #region VAT
        // ══════════════════════════════════════════════

        /// <summary>
        /// جلب نسبة الضريبة الافتراضية
        /// </summary>
        public static double GetVAT()
        {
            try
            {
                using var localConn = ConnObj();
                localConn.Open();

                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=1", localConn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                    return Convert.ToDouble(dt.Rows[0]["MainVAT"]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في GetVAT: {ex.Message}");
            }
            return 0.0;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Screen Resolution
        // ══════════════════════════════════════════════

        /// <summary>
        /// الحصول على دقة الشاشة (WPF)
        /// </summary>
        public static void GetScreenResolution(ref int width, ref int height)
        {
            width = (int)SystemParameters.PrimaryScreenWidth;
            height = (int)SystemParameters.PrimaryScreenHeight;
        }

        #endregion

        // ══════════════════════════════════════════════
        #region Internet Check
        // ══════════════════════════════════════════════

        /// <summary>
        /// فحص الاتصال بالإنترنت
        /// </summary>
        public static bool CheckForInternetConnection()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("www.google.com", 3000);
                return reply?.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}