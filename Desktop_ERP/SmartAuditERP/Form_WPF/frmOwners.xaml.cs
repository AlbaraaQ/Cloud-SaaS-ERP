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
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOwners : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private int _currentCode = -1;
        private int _cutType   = 1;
        private int _ownerType = 1;
        private string _currentName = "";

        public int  ClientId  = -1;
        public bool isDone    = false;

        private bool _updated = false;
        private int  _restNo  = 0;

        public static string _selectedacc = "";

        private ObservableCollection<OwnerRow> _ownersSource
            = new ObservableCollection<OwnerRow>();

        #endregion

        #region Constructor

        public frmOwners()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            dgvOwners.ItemsSource = _ownersSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IValue.IsReadOnly = false;
            LoadCountries();
            LoadActs();
            LoadAccounts();
            txtName.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region Clear / Reset

        private void CLR()
        {
            txtName.Text       = "";
            txtMobile.Text     = "";
            txtNationalID.Text = "";
            txtTaxNo.Text      = "";
            txtTel.Text        = "";
            txtFax.Text        = "";
            txtEmail.Text      = "";
            txtNotes.Text      = "";
            IValue.Text        = "";
            IDscan.Source      = null;

            cmbCountry.SelectedIndex = -1;
            cmbCity.SelectedIndex    = -1;
            cmbArea.SelectedIndex    = -1;
            cmbActs.SelectedIndex    = -1;

            debt.IsChecked = true;

            txtAccCode.Text     = MainClass.GenerateCode(2212);
            txtAccCode.IsReadOnly = false;

            _currentCode = -1;
            _currentName = "";
        }

        #endregion

        #region Load Data

        private void LoadAccounts()
        {
            txtAccCode.Text = MainClass.GenerateCode(2212);
        }

        public void LoadCountries()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Countries ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCountry.ItemsSource       = dt.DefaultView;
                    cmbCountry.DisplayMemberPath = "name";
                    cmbCountry.SelectedValuePath = "id";
                    cmbCountry.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الدول: " + ex.Message);
            }
        }

        public void LoadActs()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Acts ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbActs.ItemsSource       = dt.DefaultView;
                    cmbActs.DisplayMemberPath = "name";
                    cmbActs.SelectedValuePath = "id";
                    cmbActs.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل مجالات العمل: " + ex.Message);
            }
        }

        public void LoadCities(int countryId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM Cities WHERE country={countryId} ORDER BY id",
                    _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCity.ItemsSource       = dt.DefaultView;
                    cmbCity.DisplayMemberPath = "name";
                    cmbCity.SelectedValuePath = "id";
                    cmbCity.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المدن: " + ex.Message);
            }
        }

        public void LoadAreas(int cityId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT id, name FROM areas WHERE city={cityId} ORDER BY id",
                    _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbArea.ItemsSource       = dt.DefaultView;
                    cmbArea.DisplayMemberPath = "name";
                    cmbArea.SelectedValuePath = "id";
                    cmbArea.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المناطق: " + ex.Message);
            }
        }

        private void LoadGrid(string condition)
        {
            _ownersSource.Clear();
            try
            {
                EnsureConnectionOpen(_conn);
                string sql = $@"SELECT o.id, o.name, o.national_id, o.tel, o.mobile,
                                       o.country, o.city, o.area
                                FROM Owners o
                                WHERE {condition} o.IS_Deleted=0 AND o.type={_cutType}
                                ORDER BY o.id";

                using (var da = new SqlDataAdapter(sql, _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _ownersSource.Add(new OwnerRow
                        {
                            id          = Convert.ToInt32(row["id"]),
                            name        = row["name"].ToString(),
                            national_id = row["national_id"].ToString(),
                            tel         = row["tel"].ToString(),
                            mobile      = row["mobile"].ToString(),
                            countryName = GetNameById("Countries", SafeInt(row["country"])),
                            cityName    = GetNameById("Cities",    SafeInt(row["city"])),
                            areaName    = GetNameById("areas",     SafeInt(row["area"]))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            try
            {
                EnsureConnectionOpen(_conn);
                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var dr = cmd.ExecuteReader())
                {
                    ReadData(dr);
                }
                IValue.IsReadOnly = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();
            CLR();

            _currentCode = Convert.ToInt32(dr["id"]);
            txtName.Text = dr["name"].ToString();
            _currentName = txtName.Text;

            // الرصيد الافتتاحي من جدول الحسابات
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE AName=N'{_currentName}'", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        IValue.Text = dt.Rows[0]["IValue"].ToString();
                        if (dt.Rows[0]["Nature"].ToString() == "1")
                            debt.IsChecked = true;
                        else
                            credit.IsChecked = true;
                    }
                }
            }
            catch { }

            // الدولة
            try
            {
                int countryId = SafeInt(dr["country"]);
                if (countryId != -1)
                {
                    cmbCountry.SelectedValue = countryId;
                    LoadCities(countryId);
                }
            }
            catch { }

            // المدينة
            try
            {
                int cityId = SafeInt(dr["city"]);
                if (cityId != -1)
                {
                    cmbCity.SelectedValue = cityId;
                    LoadAreas(cityId);
                }
            }
            catch { }

            // المنطقة
            try
            {
                int areaId = SafeInt(dr["area"]);
                if (areaId != -1)
                    cmbArea.SelectedValue = areaId;
            }
            catch { }

            // مجال العمل
            try
            {
                int actId = SafeInt(dr["act"]);
                if (actId != -1)
                    cmbActs.SelectedValue = actId;
            }
            catch { }

            txtNationalID.Text = dr["national_id"].ToString();
            txtTel.Text        = dr["tel"].ToString();
            txtMobile.Text     = dr["mobile"].ToString();
            txtEmail.Text      = dr["email"].ToString();
            txtFax.Text        = dr["fax"].ToString();
            txtNotes.Text      = dr["notes"].ToString();

            try { txtTaxNo.Text = dr["tax_no"].ToString(); } catch { }

            // صورة الهوية
            try
            {
                if (dr["IdScan"] != DBNull.Value)
                {
                    byte[] imageBytes = (byte[])dr["IdScan"];
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption  = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        IDscan.Source = bmp;
                    }
                }
            }
            catch { }

            // رقم الحساب
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE AName=N'{txtName.Text}'", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        txtAccCode.Text     = dt.Rows[0][0].ToString();
                        txtAccCode.IsReadOnly = true;
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            TabControl1.SelectedIndex = 0;
            txtName.Focus();
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Owners WHERE type={_ownerType} AND IS_Deleted=0 ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Owners WHERE type={_ownerType} AND IS_Deleted=0 AND id<{_currentCode} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Owners WHERE type={_ownerType} AND IS_Deleted=0 AND id>{_currentCode} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Owners WHERE type={_ownerType} AND IS_Deleted=0 ORDER BY id DESC");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region التحقق من النسخة التجريبية

                if (MainClass.IsTrial)
                {
                    using (var da = new SqlDataAdapter("SELECT id FROM Owners", _conn1))
                    {
                        var trialDt = new DataTable();
                        da.Fill(trialDt);
                        if (trialDt.Rows.Count >= 10)
                        {
                            DXMessageBox.Show(
                                "نأسف لقد وصلت لأقصى حد إدخال للنسخة التجريبية.",
                                "", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                }

                #endregion

                #region التحقق من الاسم

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show("يرجى إدخال الاسم", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                #endregion

                EnsureConnectionOpen(_conn);

                int ownerTypeValue = 1;
                SqlCommand cmd;

                if (_currentCode == -1)
                {
                    // تحقق من التكرار
                    using (var da = new SqlDataAdapter(
                        $"SELECT id FROM Owners WHERE type={ownerTypeValue} AND name=N'{txtName.Text.Trim()}'",
                        _conn1))
                    {
                        var dupDt = new DataTable();
                        da.Fill(dupDt);
                        if (dupDt.Rows.Count > 0)
                        {
                            var answer = DXMessageBox.Show(
                                "هذا الاسم مدخل من قبل، هل تريد الاستمرار؟",
                                "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (answer == MessageBoxResult.No)
                            {
                                txtName.Focus();
                                return;
                            }
                        }
                    }

                    // الحصول على المعرف الجديد
                    using (var maxCmd = new SqlCommand("SELECT MAX(id) FROM Owners", _conn))
                    {
                        object maxResult = maxCmd.ExecuteScalar();
                        int maxId = (maxResult != DBNull.Value) ? Convert.ToInt32(maxResult) : 0;
                        ClientId = maxId + 1;
                    }

                    cmd = new SqlCommand(@"INSERT INTO Owners
                        (name,country,city,area,act,national_id,tel,mobile,fax,email,
                         notes,type,tax_no,IS_Deleted,AccountCode,IdScan)
                        VALUES
                        (@name,@country,@city,@area,@act,@national_id,@tel,@mobile,@fax,
                         @email,@notes,@type,@tax_no,@IS_Deleted,@AccountCode,@IdScan)",
                        _conn);
                }
                else
                {
                    ClientId = _currentCode;
                    cmd = new SqlCommand(@"UPDATE Owners SET
                        name=@name,country=@country,city=@city,area=@area,act=@act,
                        national_id=@national_id,tel=@tel,mobile=@mobile,fax=@fax,
                        email=@email,notes=@notes,type=@type,tax_no=@tax_no,
                        AccountCode=@AccountCode,IdScan=@IdScan
                        WHERE id=" + _currentCode, _conn);
                }

                #region إضافة Parameters

                cmd.Parameters.Add("@name",        SqlDbType.NVarChar).Value = txtName.Text;
                cmd.Parameters.Add("@country",     SqlDbType.Int).Value      = GetSelectedId(cmbCountry);
                cmd.Parameters.Add("@city",        SqlDbType.Int).Value      = GetSelectedId(cmbCity);
                cmd.Parameters.Add("@area",        SqlDbType.Int).Value      = GetSelectedId(cmbArea);
                cmd.Parameters.Add("@act",         SqlDbType.Int).Value      = GetSelectedId(cmbActs);
                cmd.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = txtNationalID.Text;
                cmd.Parameters.Add("@tel",         SqlDbType.NVarChar).Value = txtTel.Text;
                cmd.Parameters.Add("@mobile",      SqlDbType.NVarChar).Value = txtMobile.Text;
                cmd.Parameters.Add("@fax",         SqlDbType.NVarChar).Value = txtFax.Text;
                cmd.Parameters.Add("@email",       SqlDbType.NVarChar).Value = txtEmail.Text;
                cmd.Parameters.Add("@notes",       SqlDbType.NVarChar).Value = txtNotes.Text;
                cmd.Parameters.Add("@type",        SqlDbType.Int).Value      = ownerTypeValue;
                cmd.Parameters.Add("@tax_no",      SqlDbType.NVarChar).Value = txtTaxNo.Text;
                cmd.Parameters.Add("@IS_Deleted",  SqlDbType.Bit).Value      = 0;

                double.TryParse(txtAccCode.Text, out double accCodeVal);
                cmd.Parameters.Add("@AccountCode", SqlDbType.Int).Value = (int)accCodeVal;

                if (IDscan.Source != null)
                    cmd.Parameters.Add("@IdScan", SqlDbType.Image).Value = ImageSourceToBytes(IDscan.Source);
                else
                    cmd.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;

                #endregion

                cmd.ExecuteNonQuery();

                #region حفظ الحساب

                int natureValue = (debt.IsChecked == true) ? 1 : 2;
                string parentCode = "2212";

                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM Accounts_Index WHERE code=N'{txtAccCode.Text}'", _conn))
                {
                    var accDt = new DataTable();
                    da.Fill(accDt);

                    double.TryParse(IValue.Text, out double iValueAmount);

                    if (accDt.Rows.Count == 0 || _currentCode == -1)
                    {
                        using (var insCmd = new SqlCommand(@"INSERT INTO Accounts_Index
                            (Code,AName,Type,ParentCode,FinalAcc,Acc_branch,Nature,IValue,UserName,date)
                            VALUES (N'" + txtAccCode.Text + $@"',N'{txtName.Text}',
                            2,N'{parentCode}',1,{MainClass.BranchNo},{natureValue},
                            {iValueAmount},{MainClass.UserID},@date)", _conn))
                        {
                            insCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                            insCmd.ExecuteNonQuery();
                        }

                        if (!string.IsNullOrWhiteSpace(IValue.Text) && _currentCode == -1)
                        {
                            _updated = true;
                            InsertRestriction();
                        }
                    }
                    else
                    {
                        using (var updCmd = new SqlCommand(
                            $"UPDATE Accounts_Index SET AName=N'{txtName.Text}',Nature={natureValue},IValue={iValueAmount} WHERE code=N'{txtAccCode.Text}'",
                            _conn))
                        {
                            updCmd.ExecuteNonQuery();
                        }
                    }
                }

                #endregion

                DXMessageBox.Show("تم الحفظ بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);

                if (_currentCode == -1)
                {
                    _currentCode = ClientId;
                    CLR();
                }

                isDone = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    DXMessageBox.Show("اختر مالكًا ليتم حذفه", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var answer = DXMessageBox.Show("هل أنت متأكد من الحذف؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (answer != MessageBoxResult.Yes) return;

                EnsureConnectionOpen(_conn);

                using (var cmd = new SqlCommand(
                    $"UPDATE Owners SET IS_Deleted=1 WHERE id={_currentCode}", _conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // حذف الحساب المرتبط
                using (var da = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE Type=2 AND AName='{txtName.Text}'", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        double.TryParse(dt.Rows[0][0].ToString(), out double accCode);
                        using (var delCmd = new SqlCommand(
                            $"DELETE FROM Accounts_Index WHERE Code={(int)accCode}", _conn))
                        {
                            delCmd.ExecuteNonQuery();
                        }
                    }
                }

                CLR();
                DXMessageBox.Show("تم الحذف بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            // منطق المسح الضوئي - يحتاج TwainHandler
            DXMessageBox.Show("وظيفة المسح الضوئي غير متاحة حاليًا في WPF.",
                "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region ComboBox Events

        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;
                cmbCity.ItemsSource = null;
                if (cmbCountry.SelectedValue != null)
                {
                    if (int.TryParse(cmbCountry.SelectedValue.ToString(), out int countryId))
                        LoadCities(countryId);
                }
            }
            catch { }
        }

        private void cmbCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                cmbArea.ItemsSource = null;
                if (cmbCity.SelectedValue != null)
                {
                    if (int.TryParse(cmbCity.SelectedValue.ToString(), out int cityId))
                        LoadAreas(cityId);
                }
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void dgvOwners_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvOwners.SelectedItem is OwnerRow selectedRow)
            {
                Navigate($"SELECT * FROM Owners WHERE id={selectedRow.id}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Search

        private void txtNameSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search();
        }

        private void Search()
        {
            string condition = "";
            if (!string.IsNullOrWhiteSpace(txtNameSrch.Text))
                condition = $"name LIKE N'%{txtNameSrch.Text}%' AND ";

            LoadGrid(condition);
            TabControl1.SelectedIndex = 2;
        }

        #endregion

        #region Sub-Form Buttons

        private void btnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedCountryId = -1;
            if (cmbCountry.SelectedValue != null)
                int.TryParse(cmbCountry.SelectedValue.ToString(), out savedCountryId);

            var frm = new frmCountries();
            frm.ShowDialog();

            LoadCountries();

            try
            {
                if (savedCountryId != -1)
                    cmbCountry.SelectedValue = savedCountryId;
            }
            catch { }
        }

        private void btnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                DXMessageBox.Show("يجب اختيار الدولة أولًا", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            int savedCityId = -1;
            if (cmbCity.SelectedValue != null)
                int.TryParse(cmbCity.SelectedValue.ToString(), out savedCityId);

            var frm = new frmCities();
            frm.ShowDialog();

            if (int.TryParse(cmbCountry.SelectedValue.ToString(), out int cId))
                LoadCities(cId);

            try
            {
                if (savedCityId != -1)
                    cmbCity.SelectedValue = savedCityId;
            }
            catch { }
        }

        private void btnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.SelectedValue == null)
            {
                DXMessageBox.Show("يجب اختيار الدولة أولًا", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cmbCity.SelectedValue == null)
            {
                DXMessageBox.Show("يجب اختيار المدينة أولًا", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int savedAreaId = -1;
            if (cmbArea.SelectedValue != null)
                int.TryParse(cmbArea.SelectedValue.ToString(), out savedAreaId);

            var frm = new frmAreas();
            frm.ShowDialog();

            if (int.TryParse(cmbCity.SelectedValue.ToString(), out int cityId))
                LoadAreas(cityId);

            try
            {
                if (savedAreaId != -1)
                    cmbArea.SelectedValue = savedAreaId;
            }
            catch { }
        }

        private void btnActAdd_Click(object sender, RoutedEventArgs e)
        {
            int savedActId = -1;
            if (cmbActs.SelectedValue != null)
                int.TryParse(cmbActs.SelectedValue.ToString(), out savedActId);

            var frm = new frmActs();
            frm.ShowDialog();

            LoadActs();

            try
            {
                if (savedActId != -1)
                    cmbActs.SelectedValue = savedActId;
            }
            catch { }
        }

        #endregion

        #region InsertRestriction

        private void InsertRestriction()
        {
            SqlTransaction transaction = null;
            try
            {
                EnsureConnectionOpen(_conn);
                transaction = _conn.BeginTransaction();

                string branchFilter = (MainClass.BranchNo != -1)
                    ? $" WHERE branch={MainClass.BranchNo}" : "";

                int maxEntryId;
                using (var maxCmd = new SqlCommand($"SELECT MAX(id) FROM Entry{branchFilter}",
                    _conn, transaction))
                {
                    object result = maxCmd.ExecuteScalar();
                    maxEntryId = (result != DBNull.Value) ? Convert.ToInt32(result) + 1 : 1;
                }

                double debtAmount   = 0;
                double creditAmount = 0;

                if (debt.IsChecked == true)
                {
                    double.TryParse(IValue.Text, out debtAmount);
                }
                else
                {
                    double.TryParse(IValue.Text, out creditAmount);
                }

                // إدخال القيد الرئيسي
                using (var entryCmd = new SqlCommand(@"INSERT INTO Entry
                    (id,Date,doc_no,type,state,notes,branch,IS_Deleted)
                    VALUES(@id,@Date,@doc_no,@type,@state,@notes,@branch,@IS_Deleted)",
                    _conn, transaction))
                {
                    entryCmd.Parameters.Add("@id",         SqlDbType.Int).Value      = maxEntryId;
                    entryCmd.Parameters.Add("@Date",       SqlDbType.DateTime).Value = DateTime.Now;
                    entryCmd.Parameters.Add("@doc_no",     SqlDbType.Int).Value      = 30;
                    entryCmd.Parameters.Add("@type",       SqlDbType.Int).Value      = 0;
                    entryCmd.Parameters.Add("@state",      SqlDbType.Int).Value      = 1;
                    entryCmd.Parameters.Add("@notes",      SqlDbType.NVarChar).Value =
                        $" قيد الرصيد الافتتاحي ل: {txtName.Text}  بتاريخ  {DateTime.Now}";
                    entryCmd.Parameters.Add("@branch",     SqlDbType.Int).Value      = MainClass.BranchNo;
                    entryCmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value      = 0;
                    entryCmd.ExecuteNonQuery();
                }

                // السطر الأول
                double.TryParse(txtAccCode.Text, out double accCodeDouble);
                using (var sub1Cmd = new SqlCommand(@"INSERT INTO Entry_sub
                    (res_id,dept,credit,acc_no,notes,branch)
                    VALUES(@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                    _conn, transaction))
                {
                    sub1Cmd.Parameters.Add("@res_id", SqlDbType.Int).Value      = maxEntryId;
                    sub1Cmd.Parameters.Add("@dept",   SqlDbType.Float).Value    = debtAmount;
                    sub1Cmd.Parameters.Add("@credit", SqlDbType.Float).Value    = creditAmount;
                    sub1Cmd.Parameters.Add("@acc_no", SqlDbType.Int).Value      = (int)accCodeDouble;
                    sub1Cmd.Parameters.Add("@notes",  SqlDbType.NVarChar).Value =
                        $" قيد الرصيد الافتتاحي ل: {txtName.Text}  بتاريخ  {DateTime.Now}";
                    sub1Cmd.Parameters.Add("@branch", SqlDbType.Int).Value      = MainClass.BranchNo;
                    sub1Cmd.ExecuteNonQuery();
                }

                // السطر الثاني
                using (var sub2Cmd = new SqlCommand(@"INSERT INTO Entry_sub
                    (res_id,dept,credit,acc_no,notes,branch)
                    VALUES(@res_id,@dept,@credit,@acc_no,@notes,@branch)",
                    _conn, transaction))
                {
                    sub2Cmd.Parameters.Add("@res_id", SqlDbType.Int).Value      = maxEntryId;
                    sub2Cmd.Parameters.Add("@dept",   SqlDbType.Float).Value    = creditAmount;
                    sub2Cmd.Parameters.Add("@credit", SqlDbType.Float).Value    = debtAmount;
                    sub2Cmd.Parameters.Add("@acc_no", SqlDbType.Int).Value      = 2110001;
                    sub2Cmd.Parameters.Add("@notes",  SqlDbType.NVarChar).Value =
                        $" قيد الرصيد الافتتاحي ل: 2110001  بتاريخ  {DateTime.Now}";
                    sub2Cmd.Parameters.Add("@branch", SqlDbType.Int).Value      = MainClass.BranchNo;
                    sub2Cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                DXMessageBox.Show("خطأ أثناء حفظ القيد\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private void EnsureConnectionOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void CloseConnection(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        private int SafeInt(object value)
        {
            if (value == null || value == DBNull.Value) return -1;
            if (int.TryParse(value.ToString(), out int result)) return result;
            return -1;
        }

        private int GetSelectedId(ComboBox cmb)
        {
            if (cmb.SelectedValue == null) return -1;
            if (int.TryParse(cmb.SelectedValue.ToString(), out int id)) return id;
            return -1;
        }

        private string GetNameById(string table, int id)
        {
            if (id == -1) return "";
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM {table} WHERE id={id}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private byte[] ImageSourceToBytes(System.Windows.Media.ImageSource imageSource)
        {
            try
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(
                    (System.Windows.Media.Imaging.BitmapSource)imageSource));
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    return ms.ToArray();
                }
            }
            catch { return null; }
        }

        #endregion
    }

    #region Model

    public class OwnerRow
    {
        public int    id          { get; set; }
        public string name        { get; set; }
        public string national_id { get; set; }
        public string tel         { get; set; }
        public string mobile      { get; set; }
        public string countryName { get; set; }
        public string cityName    { get; set; }
        public string areaName    { get; set; }
    }

    #endregion
}