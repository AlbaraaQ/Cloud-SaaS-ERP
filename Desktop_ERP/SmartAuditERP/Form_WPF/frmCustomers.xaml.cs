using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using log4net;
using Microsoft.Win32;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCustomers : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private int _code;
        private int _customerType;

        public int Type;
        public int ClientId;
        public bool isDone;
        public bool isClient;

        public static string[] filePathDocument;
        public static string _selectedacc = "";

        private string _customerName;
        private bool _isUpdated;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region Constructor

        public frmCustomers()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _code = -1;
            _customerType = 1;
            Type = 1;
            _customerName = string.Empty;
            ClientId = -1;
            isDone = false;
            isClient = false;
            _isUpdated = false;

            Loaded += FrmCustomers_Loaded;
            KeyDown += FrmCustomers_KeyDown;
        }

        #endregion

        #region Window Events

        private void FrmCustomers_Loaded(object sender, RoutedEventArgs e)
        {
            txtHeaderDate.Text = DateTime.Now.ToLongDateString();

            IValue.IsReadOnly = false;

            LoadCountries();
            LoadActs();
            LoadBranches();
            cmbBranch.EditValue = MainClass.BranchNo;

            // إعداد الواجهة حسب النوع
            if (Type == 2)
            {
                _customerType = 2;
                rbPostPone.IsChecked = true;
                ckDealAsSupp.Content = "🔄 يعامل كعميل";
            }
            else if (Type == 1)
            {
                _customerType = 1;
                rbCash.IsChecked = true;
                ckDealAsSupp.Content = "🔄 يعامل كمورد";
                isClient = true;
                GetActiveCustMeasurements();
            }
            else if (Type == 5)
            {
                _customerType = 5;
                rbPostPone.IsChecked = true;
                ckDealAsSupp.Content = "🔄 يعامل كمورد";
            }

            LoadAccounts();

            if (Type == 2)
                credit.IsChecked = true;

            txtName.Focus();
            LoadCostCenter();

            // Wire up events
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            btnClose.Click += BtnClose_Click;
            btnNew.Click += BtnNew_Click;
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnFirst.Click += BtnFirst_Click;
            btnPrevious.Click += BtnPrevious_Click;
            btnNext.Click += BtnNext_Click;
            btnLast.Click += BtnLast_Click;
            btnSearch.Click += BtnSearch_Click;
            btnExport.Click += BtnExport_Click;
            btnScan.Click += BtnScan_Click;
            Button2.Click += BtnViewDocuments_Click;

            btnCountryAdd.Click += BtnCountryAdd_Click;
            btnCityAdd.Click += BtnCityAdd_Click;
            btnAreaAdd.Click += BtnAreaAdd_Click;
            btnActAdd.Click += BtnActAdd_Click;

            rbCash.Checked += RbCash_Checked;
            rbPostPone.Checked += RbPostPone_Checked;

            ckDealAsSupp.Checked += CkDealAsSupp_Changed;
            ckDealAsSupp.Unchecked += CkDealAsSupp_Changed;

            chkAllBranches.Checked += ChkAllBranches_Changed;
            chkAllBranches.Unchecked += ChkAllBranches_Changed;

            cmbCountry.EditValueChanged += CmbCountry_EditValueChanged;
            cmbCity.EditValueChanged += CmbCity_EditValueChanged;

            txtAccCode.EditValueChanged += TxtAccCode_EditValueChanged;

            GridView1.RowDoubleClick += GridView1_RowDoubleClick;
        }

        private void FrmCustomers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6)
                BtnSave_Click(sender, new RoutedEventArgs());
        }

        #endregion

        #region Clear Form

        private void ClearForm()
        {
            _code = -1;

            txtName.EditValue = null;
            txtMobile.EditValue = null;
            txtTaxNo.EditValue = null;
            txtNationalID.EditValue = null;
            txtCrNo.EditValue = null;
            txtNo.EditValue = null;
            txtAccCode.EditValue = null;
            txtTel.EditValue = null;
            txtEmail.EditValue = null;
            txtFax.EditValue = null;
            txtNotes.EditValue = null;
            IValue.EditValue = null;

            txtStreetName.EditValue = null;
            txtBuildingNumber.EditValue = null;
            txtPlotIdentification.EditValue = null;
            txtDistrict.EditValue = null;
            txtAdditionalStreetName.EditValue = null;
            txtPostalZone.EditValue = null;
            txtcity2.EditValue = null;
            txtarea2.EditValue = null;

            cmbCountry.EditValue = null;
            cmbCity.EditValue = null;
            cmbArea.EditValue = null;
            cmbActs.EditValue = null;
            cmbCostCenter.EditValue = null;
            cmbPrice.EditValue = null;

            ckDealAsSupp.IsChecked = false;
            IDscan.Source = null;

            // Measurements
            txtNeck.EditValue = null;
            txtShoulder.EditValue = null;
            txtSleeve.EditValue = null;
            txtChest.EditValue = null;
            txtWaist.EditValue = null;
            txtArmWidth.EditValue = null;
            txtStepWidth.EditValue = null;
            txtLength.EditValue = null;
            MeasurementNote.EditValue = null;
            txtpageno.EditValue = null;

            _customerName = string.Empty;

            if (Type == 1 && isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.CustomersAcc));
                rbCash.IsChecked = true;
            }
            else if (Type == 2 && !isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.SupliersAcc));
                rbPostPone.IsChecked = true;
            }
            else if (Type == 5)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.AppAccount));
                rbPostPone.IsChecked = true;
            }

            txtAccCode.IsReadOnly = false;
            cmbBranch.EditValue = MainClass.BranchNo;
        }

        #endregion

        #region Data Loading

        public void LoadCountries()
        {
            FillComboBox(cmbCountry,
                "SELECT id, name FROM Countries ORDER BY id",
                "name", "id");
        }

        public void LoadActs()
        {
            FillComboBox(cmbActs,
                "SELECT id, name FROM Acts ORDER BY id",
                "name", "id");
        }

        public void LoadCities(int countryId)
        {
            FillComboBox(cmbCity,
                $"SELECT id, name FROM Cities WHERE country={countryId} ORDER BY id",
                "name", "id");
        }

        public void LoadAreas(int cityId)
        {
            FillComboBox(cmbArea,
                $"SELECT id, name FROM areas WHERE city={cityId} ORDER BY id",
                "name", "id");
        }

        private void LoadBranches()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbBranch.ItemsSource = dataTable.DefaultView;
                cmbBranch.DisplayMember = "name";
                cmbBranch.ValueMember = "id";
                cmbBranch.EditValue = null;

                cmbBranches.ItemsSource = dataTable.DefaultView;
                cmbBranches.DisplayMember = "name";
                cmbBranches.ValueMember = "id";
                cmbBranches.EditValue = null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void LoadCostCenter()
        {
            try
            {
                FillComboBox(cmbCostCenter,
                    "SELECT code, name FROM cost_center " +
                    "WHERE Code<>0 AND Is_Deleted=0 AND type=2",
                    "name", "code");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadAccounts()
        {
            if (Type == 1 && isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.CustomersAcc));
            }
            else if (Type == 2 && !isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.SupliersAcc));
            }
            else if (Type == 5)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.AppAccount));
            }
        }

        public void GetActiveCustMeasurements()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                using var cmd = new SqlCommand(
                    "SELECT AtiveCustMeasur FROM SettingGeneral WHERE Inv_Id=2",
                    _conn);
                using var reader = cmd.ExecuteReader();

                if (reader.Read() && reader.HasRows)
                {
                    // إظهار / إخفاء تبويب المقاسات حسب الإعداد
                    bool isActive = Convert.ToBoolean(reader["AtiveCustMeasur"]);
                    // TabControl1.Items[3] هو تبويب المقاسات
                    if (TabControl1.Items.Count > 3)
                    {
                        var measureTab =
                            TabControl1.Items[3] as DevExpress.Xpf.Core.DXTabItem;
                        if (measureTab != null)
                            measureTab.Visibility = isActive
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        /// <summary>
        /// دالة مساعدة لتعبئة ComboBox
        /// </summary>
        private void FillComboBox(
            DevExpress.Xpf.Editors.ComboBoxEdit combo,
            string query,
            string displayMember,
            string valueMember)
        {
            try
            {
                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                combo.ItemsSource = dataTable.DefaultView;
                combo.DisplayMember = displayMember;
                combo.ValueMember = valueMember;
                combo.EditValue = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FillComboBox Error: {ex.Message}");
            }
        }

        #endregion

        #region Load Grid (Search Tab)

        private void LoadGrid(string condition)
        {
            try
            {
                string query =
                    $@"SELECT c.id, c.name, c.national_id, c.tel, c.mobile,
                              c.tax_no, c.AccountCode, c.Branch,
                              co.name AS CountryName,
                              ci.name AS CityName,
                              a.name  AS AreaName
                       FROM Customers c
                       LEFT JOIN Countries co ON c.country = co.id
                       LEFT JOIN Cities    ci ON c.city    = ci.id
                       LEFT JOIN areas      a ON c.area    = a.id
                       WHERE {condition} c.IS_Deleted=0
                       AND (c.type={Type} OR c.type=3)
                       ORDER BY c.id DESC";

                var adapter = new SqlDataAdapter(query, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                // تحويل إلى DataView للعرض في Grid
                var resultTable = new DataTable();
                resultTable.Columns.Add("Id", typeof(int));
                resultTable.Columns.Add("Name", typeof(string));
                resultTable.Columns.Add("NationalId", typeof(string));
                resultTable.Columns.Add("Phone", typeof(string));
                resultTable.Columns.Add("Mobile", typeof(string));
                resultTable.Columns.Add("Country", typeof(string));
                resultTable.Columns.Add("City", typeof(string));
                resultTable.Columns.Add("Area", typeof(string));
                resultTable.Columns.Add("TaxNo", typeof(string));
                resultTable.Columns.Add("AccountCode", typeof(string));
                resultTable.Columns.Add("Branch", typeof(string));

                foreach (DataRow row in dataTable.Rows)
                {
                    resultTable.Rows.Add(
                        row["id"],
                        row["name"],
                        row["national_id"],
                        row["tel"],
                        row["mobile"],
                        row["CountryName"],
                        row["CityName"],
                        row["AreaName"],
                        row["tax_no"],
                        row["AccountCode"],
                        Common.GetBranchName(Convert.ToInt32(row["Branch"]))
                    );
                }

                dgvCustomers.ItemsSource = resultTable.DefaultView;

                lblCustCount.Text = dataTable.Rows.Count.ToString();
                lblCustCountNo.Text = $"عدد العملاء: {dataTable.Rows.Count}";
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل البيانات:\n{ex.Message}");
            }
        }

        #endregion

        #region Navigate & Read Data

        public void Navigate(string sqlQuery)
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                using var cmd = new SqlCommand(sqlQuery, _conn);
                using var reader = cmd.ExecuteReader();

                ReadData(reader);

                IValue.IsReadOnly = true;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows)
                return;

            reader.Read();
            ClearForm();

            _code = Convert.ToInt32(reader["id"]);

            txtName.EditValue = reader["name"].ToString();
            _customerName = reader["name"].ToString();
            txtNationalID.EditValue = reader["national_id"].ToString();
            txtTel.EditValue = reader["tel"].ToString();
            txtMobile.EditValue = reader["mobile"].ToString();
            txtEmail.EditValue = reader["email"].ToString();
            txtFax.EditValue = reader["fax"].ToString();
            txtNotes.EditValue = reader["notes"].ToString();
            txtNo.EditValue = _code;

            // Address fields
            txtStreetName.EditValue = reader["StreetName"].ToString();
            txtAdditionalStreetName.EditValue = reader["AdditionalStreetName"].ToString();
            txtBuildingNumber.EditValue = reader["BuildingNumber"].ToString();
            txtPlotIdentification.EditValue = reader["PlotIdentification"].ToString();
            txtDistrict.EditValue = reader["District"].ToString();
            txtPostalZone.EditValue = reader["PostalZone"].ToString();
            txtCrNo.EditValue = reader["CrNo"].ToString();

            try { txtarea2.EditValue = reader["area2"].ToString(); }
            catch { /* ignore */ }

            try { txtcity2.EditValue = reader["city2"].ToString(); }
            catch { /* ignore */ }

            try { txtTaxNo.EditValue = reader["tax_no"].ToString(); }
            catch { /* ignore */ }

            // Branch
            cmbBranch.EditValue = reader["Branch"];

            // Pricing
            try
            {
                if (reader["Pricing"] != DBNull.Value)
                    cmbPrice.EditValue = Convert.ToInt32(reader["Pricing"]);
            }
            catch { /* ignore */ }

            // Country → City → Area cascade
            try
            {
                if (!reader["country"].Equals(-1))
                {
                    cmbCountry.EditValue = reader["country"];
                    LoadCities(Convert.ToInt32(reader["country"]));
                }
            }
            catch { /* ignore */ }

            try
            {
                if (!reader["city"].Equals(-1))
                {
                    cmbCity.EditValue = reader["city"];
                    LoadAreas(Convert.ToInt32(reader["city"]));
                }
            }
            catch { /* ignore */ }

            try
            {
                if (!reader["area"].Equals(-1))
                    cmbArea.EditValue = reader["area"];
            }
            catch { /* ignore */ }

            try
            {
                if (!reader["act"].Equals(-1))
                    cmbActs.EditValue = reader["act"];
            }
            catch { /* ignore */ }

            // Deal as supplier
            try
            {
                ckDealAsSupp.IsChecked =
                    reader["type"].ToString() == "3";
            }
            catch { /* ignore */ }

            // Account code
            try
            {
                if (!reader["AccountCode"].Equals(-1))
                {
                    var accAdapter = new SqlDataAdapter(
                        $"SELECT * FROM Accounts_Index WHERE Code={reader["AccountCode"]}",
                        _conn);
                    var accTable = new DataTable();
                    accAdapter.Fill(accTable);

                    if (accTable.Rows.Count > 0)
                    {
                        txtAccCode.EditValue = accTable.Rows[0]["Code"];
                        IValue.EditValue = accTable.Rows[0]["IValue"];

                        bool isDebit =
                            Convert.ToInt32(accTable.Rows[0]["Nature"]) == 1;
                        debt.IsChecked = isDebit;
                        credit.IsChecked = !isDebit;

                        rbPostPone.IsChecked = true;
                    }
                    else
                    {
                        txtAccCode.EditValue = null;
                        rbCash.IsChecked = true;
                    }

                    txtAccCode.IsReadOnly = true;
                }
                else
                {
                    txtAccCode.EditValue = null;
                    rbCash.IsChecked = true;
                }
            }
            catch { /* ignore */ }

            // Measurements
            LoadMeasurements(_code);

            UpdateRecordCounter();
        }

        private void LoadMeasurements(int customerId)
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                using var cmd = new SqlCommand(
                    "SELECT * FROM CustomerMeasurements WHERE CustId=@custid",
                    _conn);
                cmd.Parameters.AddWithValue("@custid", customerId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read() && reader.HasRows)
                {
                    txtLength.EditValue = reader["Length"];
                    txtChest.EditValue = reader["Chest"];
                    txtShoulder.EditValue = reader["Shoulder"];
                    txtWaist.EditValue = reader["Waist"];
                    txtSleeve.EditValue = reader["Sleeve"];
                    txtNeck.EditValue = reader["Neck"];
                    txtArmWidth.EditValue = reader["ArmWidth"];
                    txtStepWidth.EditValue = reader["StepWidth"];
                    txtpageno.EditValue = reader["pageNo"];
                    MeasurementNote.EditValue = reader["MeasurementNote"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        private void UpdateRecordCounter()
        {
            txtCurrentRecord.Text = _code > 0 ? _code.ToString() : "0";
        }

        #endregion

        #region Navigation Button Handlers

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                $"SELECT TOP 1 * FROM Customers " +
                $"WHERE (type={Type} OR type=3) AND IS_Deleted=0 " +
                $"ORDER BY id ASC");
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                $"SELECT TOP 1 * FROM Customers " +
                $"WHERE (type={Type} OR type=3) AND IS_Deleted=0 " +
                $"AND id < {_code} ORDER BY id DESC");
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                $"SELECT TOP 1 * FROM Customers " +
                $"WHERE (type={Type} OR type=3) AND IS_Deleted=0 " +
                $"AND id > {_code} ORDER BY id ASC");
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate(
                $"SELECT TOP 1 * FROM Customers " +
                $"WHERE (type={Type} OR type=3) AND IS_Deleted=0 " +
                $"ORDER BY id DESC");
        }

        #endregion

        #region CRUD Operations

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtName.Focus();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من الحد التجريبي
                if (MainClass.IsTrial)
                {
                    var trialAdapter = new SqlDataAdapter(
                        "SELECT id FROM Customers", _conn1);
                    var trialTable = new DataTable();
                    trialAdapter.Fill(trialTable);

                    if (trialTable.Rows.Count >= 10)
                    {
                        DXMessageBox.Show(
                            "نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية",
                            "تنبيه",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                // التحقق من الاسم
                string customerName = txtName.EditValue?.ToString().Trim()
                                      ?? string.Empty;
                if (string.IsNullOrEmpty(customerName))
                {
                    DXMessageBox.Show("يرجى إدخال الاسم", "تحقق",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                // التحقق من الفرع
                if (cmbBranch.EditValue == null)
                    cmbBranch.EditValue = MainClass.BranchNo;

                // تحديد نوع العميل
                _customerType = Type;
                if (ckDealAsSupp.IsChecked == true)
                    _customerType = 3;

                // التحقق من التكرار للسجلات الجديدة
                if (_code == -1)
                {
                    var dupAdapter = new SqlDataAdapter(
                        $"SELECT id FROM Customers " +
                        $"WHERE (type={Type} OR type=3) " +
                        $"AND name=N'{customerName}' AND IS_Deleted=0",
                        _conn1);
                    var dupTable = new DataTable();
                    dupAdapter.Fill(dupTable);

                    if (dupTable.Rows.Count > 0)
                    {
                        var dupResult = DXMessageBox.Show(
                            "هذا الاسم مدخل من قبل، هل أنت متأكد من الاستمرار؟",
                            "تأكيد",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (dupResult == MessageBoxResult.No)
                        {
                            txtName.Focus();
                            return;
                        }
                    }

                    // التحقق من الرقم الضريبي
                    string taxNo = txtTaxNo.EditValue?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(taxNo) &&
                        !Common.ISValidTaxNo(taxNo))
                    {
                        txtTaxNo.Focus();
                        return;
                    }

                    ClientId = Common.GetCustomerId();
                }
                else
                {
                    ClientId = _code;
                }

                // التحقق من بيانات الزكاة
                if (!ValidateZatcaFields())
                    return;

                // إعداد كائن العميل
                int countryId = cmbCountry.EditValue != null
                    ? Convert.ToInt32(cmbCountry.EditValue) : -1;
                int cityId = -1;
                int areaId = -1;

                string accCode = txtAccCode.EditValue?.ToString().Trim()
                                 ?? string.Empty;
                if (string.IsNullOrEmpty(accCode) || rbCash.IsChecked == true)
                    accCode = "-1";

                // التحقق من الرمز البريدي
                string postalZone = txtPostalZone.EditValue?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(postalZone) && postalZone.Length != 5)
                {
                    DXMessageBox.Show("الرمز البريدي يجب أن يكون خمسة أرقام",
                        "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var customer = new AuditorAPI.Models.Customer
                {
                    CustomerID = ClientId.ToString(),
                    Name = customerName,
                    Country = countryId,
                    City = cityId,
                    Region = areaId,
                    AccCode = accCode,
                    NatianalID = txtNationalID.EditValue?.ToString() ?? string.Empty,
                    Telephone = txtTel.EditValue?.ToString() ?? string.Empty,
                    Mobile = txtMobile.EditValue?.ToString() ?? string.Empty,
                    Email = txtEmail.EditValue?.ToString() ?? string.Empty,
                    Note = txtNotes.EditValue?.ToString() ?? string.Empty,
                    Type = _customerType,
                    VATno = txtTaxNo.EditValue?.ToString() ?? string.Empty,
                    IsDeleted = false,
                    BranchId = Convert.ToInt32(cmbBranch.EditValue),
                    IsCredit = rbPostPone.IsChecked == true,
                    CreateDate = DateTime.Now,
                    LastUpdateDate = DateTime.Now,
                    ClientCode = Sync.ClientCode,
                    isClient = isClient,
                    PlotIdentification = txtPlotIdentification.EditValue?.ToString() ?? string.Empty,
                    BuildingNumber = txtBuildingNumber.EditValue?.ToString() ?? string.Empty,
                    StreetName = txtStreetName.EditValue?.ToString() ?? string.Empty,
                    AdditionalStreetName = txtAdditionalStreetName.EditValue?.ToString() ?? string.Empty,
                    District = txtDistrict.EditValue?.ToString() ?? string.Empty,
                    PostalZone = postalZone,
                    CrNo = txtCrNo.EditValue?.ToString() ?? string.Empty,
                    Pricing = cmbPrice.EditValue != null
                                          ? Convert.ToInt32(cmbPrice.EditValue) : 0,
                    area2 = txtarea2.EditValue?.ToString() ?? string.Empty,
                    city2 = txtcity2.EditValue?.ToString() ?? string.Empty
                };

                var customerList = new List<AuditorAPI.Models.Customer> { customer };

                if (SaveCustomer(customerList, addedLocally: true))
                {
                    InsertCustomerMeasurements();

                    if (filePathDocument != null)
                        InsertDocument(customer.CustomerID);

                    if (Sync.ActiveSync && Sync.SyncType > 0)
                    {
                        new CustomerCRUD(Sync.APIUrl).AddCustomer(customerList);
                        Sync.SendLastCodeToApiAsync(1, Sync.ClientCode);
                    }

                    string logAction = _code == -1
                        ? $"تم إضافة عميل جديد: {customerName}"
                        : $"تم تعديل بطاقة عميل: {customerName}";

                    Logger.Info($"{logAction} بواسطة: {MainClass.UserName}");

                    DXMessageBox.Show("✅ تم الحفظ بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _code = ClientId;
                    isDone = true;
                    ClearForm();
                }
                else
                {
                    ShowError("خطأ أثناء الحفظ");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحفظ:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        /// <summary>
        /// التحقق من حقول هيئة الزكاة
        /// </summary>
        private bool ValidateZatcaFields()
        {
            string taxNo = txtTaxNo.EditValue?.ToString() ?? string.Empty;

            if (!Common.GetZatcaActive() ||
                string.IsNullOrEmpty(taxNo) || Type != 1)
                return true;

            if (taxNo.Length != 15)
            {
                DXMessageBox.Show("الرقم الضريبي يجب أن يكون 15 رقماً",
                    "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                TabControl1.SelectedIndex = 0;
                txtTaxNo.Focus();
                return false;
            }

            var validations = new[]
            {
                (string.IsNullOrWhiteSpace(txtPlotIdentification.EditValue?.ToString()),
                 "يرجى إدخال الرقم الفرعي", txtPlotIdentification),
                (string.IsNullOrWhiteSpace(txtBuildingNumber.EditValue?.ToString()),
                 "يرجى إدخال رقم المبنى", txtBuildingNumber),
                (string.IsNullOrWhiteSpace(txtStreetName.EditValue?.ToString()),
                 "يرجى إدخال الشارع", txtStreetName),
                (string.IsNullOrWhiteSpace(txtDistrict.EditValue?.ToString()),
                 "يرجى إدخال الحي", txtDistrict),
                (string.IsNullOrWhiteSpace(txtPostalZone.EditValue?.ToString()),
                 "يرجى إدخال الرمز البريدي", txtPostalZone),
                (string.IsNullOrWhiteSpace(txtcity2.EditValue?.ToString()),
                 "يرجى إدخال المدينة", txtcity2),
                (string.IsNullOrWhiteSpace(txtarea2.EditValue?.ToString()),
                 "يرجى إدخال المنطقة", txtarea2),
            };

            foreach (var (isInvalid, message, control) in validations)
            {
                if (isInvalid)
                {
                    DXMessageBox.Show(message, "تحقق",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TabControl1.SelectedIndex = 0;
                    control.Focus();
                    return false;
                }
            }

            return true;
        }

        public bool SaveCustomer(List<AuditorAPI.Models.Customer> customers, bool addedLocally)
        {
            var sqlConn = MainClass.ConnObj();
            try
            {
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                foreach (var customer in customers)
                {
                    if (customer == null) continue;

                    // التحقق من وجود السجل
                    using var countCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM Customers WHERE id={customer.CustomerID}",
                        sqlConn);
                    int count = Convert.ToInt32(countCmd.ExecuteScalar());

                    SqlCommand cmd;
                    if (count > 0)
                    {
                        cmd = new SqlCommand(
                            @"UPDATE Customers SET
                              name=@name, country=@country, city=@city, area=@area,
                              national_id=@national_id, tel=@tel, mobile=@mobile,
                              email=@email, notes=@notes, type=@type, tax_no=@tax_no,
                              AccountCode=@AccountCode, Branch=@Branch,
                              ISCredit=@ISCredit, PlotIdentification=@PlotIdentification,
                              BuildingNumber=@BuildingNumber, StreetName=@StreetName,
                              AdditionalStreetName=@AdditionalStreetName,
                              District=@District, PostalZone=@PostalZone,
                              CrNo=@CrNo, Pricing=@Pricing, area2=@area2, city2=@city2
                              WHERE id=@Id",
                            sqlConn);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            @"SET IDENTITY_INSERT [dbo].[Customers] ON
                              INSERT INTO Customers(id,name,country,city,area,
                              national_id,tel,mobile,email,notes,type,tax_no,
                              IS_Deleted,AccountCode,Branch,ISCredit,
                              PlotIdentification,BuildingNumber,StreetName,
                              AdditionalStreetName,District,PostalZone,
                              CrNo,Pricing,area2,city2)
                              VALUES(@Id,@name,@country,@city,@area,
                              @national_id,@tel,@mobile,@email,@notes,@type,@tax_no,
                              0,@AccountCode,@Branch,@ISCredit,
                              @PlotIdentification,@BuildingNumber,@StreetName,
                              @AdditionalStreetName,@District,@PostalZone,
                              @CrNo,@Pricing,@area2,@city2)
                              SET IDENTITY_INSERT [dbo].[Customers] OFF",
                            sqlConn);
                    }

                    AddSqlParameters(cmd, customer);
                    cmd.ExecuteNonQuery();

                    // حفظ الحساب في دليل الحسابات
                    if (addedLocally && customer.IsCredit)
                    {
                        string parentCode = customer.isClient
                            ? Common.CurrentBranch.CustomersAcc
                            : Common.CurrentBranch.SupliersAcc;

                        if (Type == 5)
                            parentCode = Common.CurrentBranch.AppAccount;

                        var account = new TreeAccount
                        {
                            AccName = customer.Name,
                            AccNature = 1,
                            Code = customer.AccCode,
                            AccountID = Convert.ToInt32(customer.AccCode),
                            ParentCode = parentCode,
                            IntialBalance = 0m,
                            EmpId = MainClass.EmpNo,
                            AccType = 2,
                            IsDeleted = false,
                            BranchId = customer.BranchId,
                            CreateDate = DateTime.Now,
                            ClientCode = Sync.ClientCode,
                            LastUpdateDate = DateTime.Now
                        };

                        if (!SaveAccounts(new List<TreeAccount> { account }))
                            return false;

                        if (Sync.ActiveSync && Sync.SyncType > 0)
                            new AccountCRUD(Sync.APIUrl)
                                .AddTreeAccount(new List<TreeAccount> { account });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (sqlConn.State != ConnectionState.Closed)
                    sqlConn.Close();
            }
        }

        private void AddSqlParameters(SqlCommand cmd, AuditorAPI.Models.Customer customer)
        {
            cmd.Parameters.AddWithValue("@Id", customer.CustomerID);
            cmd.Parameters.AddWithValue("@name", customer.Name);
            cmd.Parameters.AddWithValue("@country", customer.Country);
            cmd.Parameters.AddWithValue("@city", customer.City);
            cmd.Parameters.AddWithValue("@area", customer.Region);
            cmd.Parameters.AddWithValue("@AccountCode", customer.AccCode);
            cmd.Parameters.AddWithValue("@national_id", customer.NatianalID ?? string.Empty);
            cmd.Parameters.AddWithValue("@tel", customer.Telephone ?? string.Empty);
            cmd.Parameters.AddWithValue("@mobile", customer.Mobile ?? string.Empty);
            cmd.Parameters.AddWithValue("@email", customer.Email ?? string.Empty);
            cmd.Parameters.AddWithValue("@notes", customer.Note ?? string.Empty);
            cmd.Parameters.AddWithValue("@type", customer.Type);
            cmd.Parameters.AddWithValue("@tax_no", customer.VATno ?? string.Empty);
            cmd.Parameters.AddWithValue("@Branch", customer.BranchId);
            cmd.Parameters.AddWithValue("@ISCredit",
                customer.AccCode != "-1" && customer.AccCode != null ? 1 : 0);
            cmd.Parameters.AddWithValue("@PlotIdentification",
                customer.PlotIdentification ?? string.Empty);
            cmd.Parameters.AddWithValue("@BuildingNumber",
                customer.BuildingNumber ?? string.Empty);
            cmd.Parameters.AddWithValue("@StreetName",
                customer.StreetName ?? string.Empty);
            cmd.Parameters.AddWithValue("@AdditionalStreetName",
                customer.AdditionalStreetName ?? string.Empty);
            cmd.Parameters.AddWithValue("@District",
                customer.District ?? string.Empty);
            cmd.Parameters.AddWithValue("@PostalZone",
                customer.PostalZone ?? string.Empty);
            cmd.Parameters.AddWithValue("@CrNo",
                customer.CrNo ?? string.Empty);
            cmd.Parameters.AddWithValue("@Pricing", customer.Pricing);
            cmd.Parameters.AddWithValue("@area2", customer.area2 ?? string.Empty);
            cmd.Parameters.AddWithValue("@city2", customer.city2 ?? string.Empty);
        }

        public bool SaveAccounts(List<TreeAccount> accounts)
        {
            var sqlConn = MainClass.ConnObj();
            try
            {
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                foreach (var account in accounts)
                {
                    if (account == null) continue;

                    using var countCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM Accounts_Index WHERE code={account.Code}",
                        sqlConn);
                    int count = Convert.ToInt32(countCmd.ExecuteScalar());

                    SqlCommand cmd;
                    if (count > 0)
                    {
                        cmd = new SqlCommand(
                            $"UPDATE Accounts_Index SET AName=N'{account.AccName}', " +
                            $"IValue=0, IsDeleted={Convert.ToInt16(account.IsDeleted)}, " +
                            $"CostCenter={account.CostCenter} " +
                            $"WHERE Code={account.Code}",
                            sqlConn);
                    }
                    else
                    {
                        cmd = new SqlCommand(
                            $"INSERT INTO Accounts_Index" +
                            $"(Code,AName,Type,ParentCode,FinalAcc,Acc_branch," +
                            $"Nature,IValue,UserName,date,CostCenter,IsDeleted) " +
                            $"VALUES(N'{account.Code}',N'{account.AccName}'," +
                            $"{account.AccType},N'{account.ParentCode}'," +
                            $"{account.FinalAcc},{account.BranchId},1,0," +
                            $"{account.EmpId},@date,{account.CostCenter},0)",
                            sqlConn);
                        cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                    }

                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (sqlConn.State != ConnectionState.Closed)
                    sqlConn.Close();
            }
        }

        public object InsertCustomerMeasurements()
        {
            try
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();

                _conn.Open();

                using var cmd = new SqlCommand(
                    @"INSERT INTO CustomerMeasurements
                      (CustId,Length,Shoulder,Chest,Waist,Sleeve,
                       Neck,ArmWidth,StepWidth,MeasurementNote,pageNo)
                      VALUES
                      (@CustId,@Length,@Shoulder,@Chest,@Waist,@Sleeve,
                       @Neck,@ArmWidth,@StepWidth,@MeasurementNote,@pageNo)",
                    _conn);

                cmd.Parameters.AddWithValue("@CustId",
                    txtNo.EditValue?.ToString() ?? "0");
                cmd.Parameters.AddWithValue("@Length",
                    txtLength.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@Shoulder",
                    txtShoulder.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@Chest",
                    txtChest.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@Waist",
                    txtWaist.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@Sleeve",
                    txtSleeve.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@Neck",
                    txtNeck.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@ArmWidth",
                    txtArmWidth.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@StepWidth",
                    txtStepWidth.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@MeasurementNote",
                    MeasurementNote.EditValue?.ToString() ?? string.Empty);
                cmd.Parameters.AddWithValue("@pageNo",
                    txtpageno.EditValue?.ToString() ?? string.Empty);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }

            return null;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_code == -1)
                    return;

                if (Sync.BranchType == 4 && Sync.SyncType > 0)
                {
                    DXMessageBox.Show(
                        "لا يمكن حذف أو تعديل البيانات من هذا الفرع",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string custName = txtName.EditValue?.ToString().Trim()
                                  ?? string.Empty;

                if (string.IsNullOrEmpty(custName))
                {
                    DXMessageBox.Show("يجب إدخال الاسم", "تحقق",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_code == 1 || _code == 2)
                {
                    DXMessageBox.Show("لا يمكن حذف هذا العميل/المورد",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var deleteResult = DXMessageBox.Show(
                    "⚠️ هل أنت متأكد من الحذف؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (deleteResult != MessageBoxResult.Yes)
                    return;

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                // التحقق من الفواتير المرتبطة
                var invAdapter = new SqlDataAdapter(
                    $"SELECT id FROM Inv WHERE IS_Deleted=0 AND cust_id={_code}",
                    _conn);
                var invTable = new DataTable();
                invAdapter.Fill(invTable);

                if (invTable.Rows.Count > 0)
                {
                    DXMessageBox.Show("لا يمكن الحذف، مرتبط بفواتير",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من السندات المرتبطة
                var receiptAdapter = new SqlDataAdapter(
                    $"SELECT ReceiptNo FROM Receipts " +
                    $"WHERE ISDeleted=0 AND ClientID={_code}",
                    _conn);
                var receiptTable = new DataTable();
                receiptAdapter.Fill(receiptTable);

                if (receiptTable.Rows.Count > 0)
                {
                    DXMessageBox.Show("لا يمكن الحذف، العميل مرتبط بسندات",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // الحذف المنطقي
                new SqlCommand(
                    $"UPDATE Customers SET IS_Deleted=1 WHERE id={_code}",
                    _conn).ExecuteNonQuery();

                // حذف الحساب من دليل الحسابات
                var accAdapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index " +
                    $"WHERE Type=2 AND AName=N'{custName}'",
                    _conn);
                var accTable = new DataTable();
                accAdapter.Fill(accTable);

                if (accTable.Rows.Count > 0)
                {
                    new SqlCommand(
                        $"DELETE FROM Accounts_Index " +
                        $"WHERE Code={accTable.Rows[0][0]}",
                        _conn).ExecuteNonQuery();
                }

                Logger.Info(
                    $"تم حذف عميل: {custName} بواسطة: {MainClass.UserName}");

                DXMessageBox.Show("✅ تم الحذف بنجاح", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء الحذف:\n{ex.Message}");
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Search Operations

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            string condition = string.Empty;

            string nameSrch = txtNameSrch.EditValue?.ToString().Trim() ?? string.Empty;
            string mobileSrch = txtMobileSrch.EditValue?.ToString().Trim() ?? string.Empty;
            string vatSrch = txtVATSrch.EditValue?.ToString().Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(nameSrch))
                condition += $"name LIKE N'%{nameSrch}%' AND ";

            if (!string.IsNullOrEmpty(mobileSrch))
                condition += $"mobile LIKE N'%{mobileSrch}%' AND ";

            if (!string.IsNullOrEmpty(vatSrch))
                condition += $"tax_no LIKE N'%{vatSrch}%' AND ";

            if (chkAllBranches.IsChecked != true &&
                cmbBranches.EditValue != null)
            {
                condition += $"Branch={cmbBranches.EditValue} AND ";
            }

            LoadGrid(condition);
        }

        private void GridView1_RowDoubleClick(
            object sender,
            DevExpress.Xpf.Grid.RowDoubleClickEventArgs e)
        {
            if (dgvCustomers.CurrentItem is DataRowView rowView)
            {
                int selectedId = Convert.ToInt32(rowView["Id"]);
                Navigate(
                    $"SELECT * FROM Customers WHERE id={selectedId}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Export

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dataView = dgvCustomers.ItemsSource as DataView;

            if (dataView == null || dataView.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                FileName = $"العملاء - {DateTime.Now:yyyy-MM-dd}",
                Title = "حفظ ملف Excel"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    GridView1.ExportToXlsx(saveDialog.FileName);
                    DXMessageBox.Show(
                        $"✅ تم التصدير بنجاح:\n{saveDialog.FileName}",
                        "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    ShowError($"خطأ في التصدير:\n{ex.Message}");
                }
            }
        }

        #endregion

        #region ComboBox Cascade Events

        private void CmbCountry_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            try
            {
                cmbArea.EditValue = null;
                cmbCity.EditValue = null;

                if (cmbCountry.EditValue != null)
                    LoadCities(Convert.ToInt32(cmbCountry.EditValue));
            }
            catch { /* ignore */ }
        }

        private void CmbCity_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            try
            {
                cmbArea.EditValue = null;

                if (cmbCity.EditValue != null)
                    LoadAreas(Convert.ToInt32(cmbCity.EditValue));
            }
            catch { /* ignore */ }
        }

        #endregion

        #region Account Type Events

        private void RbCash_Checked(object sender, RoutedEventArgs e)
        {
            if (rbCash.IsChecked != true)
                return;

            if (_code == -1)
            {
                txtAccCode.EditValue = null;
            }
            else if (_code > -1 &&
                     !string.IsNullOrEmpty(txtAccCode.EditValue?.ToString()))
            {
                if (!IsValidCashClient())
                {
                    rbPostPone.IsChecked = true;
                    return;
                }
                txtAccCode.EditValue = null;
            }
        }

        private void RbPostPone_Checked(object sender, RoutedEventArgs e)
        {
            if (rbPostPone.IsChecked != true)
                return;

            if (_code == -1 ||
                string.IsNullOrEmpty(txtAccCode.EditValue?.ToString()))
            {
                GenerateAccountCode();
            }
        }

        private void GenerateAccountCode()
        {
            if (Type == 1 && isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.CustomersAcc));
            }
            else if (Type == 2 && !isClient)
            {
                txtAccCode.EditValue = MainClass.GenerateCode(
                    Convert.ToInt32(Common.CurrentBranch.SupliersAcc));
            }
        }

        private bool IsValidCashClient()
        {
            try
            {
                string accCode = txtAccCode.EditValue?.ToString() ?? string.Empty;

                var adapter = new SqlDataAdapter(
                    $"SELECT res_id FROM Entry_sub WHERE acc_no={accCode}",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                {
                    var result = DXMessageBox.Show(
                        "سيتم حذف الحساب من دليل الحسابات، هل أنت متأكد؟",
                        "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_conn.State != ConnectionState.Open)
                            _conn.Open();

                        new SqlCommand(
                            $"DELETE FROM Accounts_Index WHERE Code={accCode}",
                            _conn).ExecuteNonQuery();

                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void TxtAccCode_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            try
            {
                string code = txtAccCode.EditValue?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(code))
                    return;

                var adapter = new SqlDataAdapter(
                    $"SELECT aname, parentcode FROM Accounts_Index " +
                    $"WHERE code=N'{code}'",
                    _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    string parentCode = dataTable.Rows[0][1].ToString();
                    bool isLinked =
                        parentCode == Common.CurrentBranch.CustomersAcc ||
                        parentCode == Common.CurrentBranch.SupliersAcc ||
                        parentCode == "12212";

                    if (isLinked)
                        txtName.EditValue = dataTable.Rows[0][0].ToString();
                }
            }
            catch { /* ignore */ }
        }

        private void CkDealAsSupp_Changed(object sender, RoutedEventArgs e)
        {
            _customerType = ckDealAsSupp.IsChecked == true ? 3 : Type;
        }

        private void ChkAllBranches_Changed(object sender, RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        #endregion

        #region Add New Lookup Items

        private void BtnCountryAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = cmbCountry.EditValue != null
                ? Convert.ToInt32(cmbCountry.EditValue) : -1;

            var frm = new frmCountries();
            frm.Activate();
            frm.ShowDialog();

            LoadCountries();

            if (previousId != -1)
                cmbCountry.EditValue = previousId;
        }

        private void BtnCityAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.EditValue == null)
            {
                DXMessageBox.Show(
                    "يجب اختيار الدولة أولاً",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCountry.Focus();
                return;
            }

            int previousId = cmbCity.EditValue != null
                ? Convert.ToInt32(cmbCity.EditValue) : -1;

            var frm = new frmCities();
            frm.Activate();
            frm.LoadCountries();
            frm.cmbCountry.SelectedValue = cmbCountry.EditValue;
            frm.ShowDialog();

            LoadCities(Convert.ToInt32(cmbCountry.EditValue));

            if (previousId != -1)
                cmbCity.EditValue = previousId;
        }

        private void BtnAreaAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCountry.EditValue == null)
            {
                DXMessageBox.Show("يجب اختيار الدولة أولاً",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbCity.EditValue == null)
            {
                DXMessageBox.Show("يجب اختيار المدينة أولاً",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int previousId = cmbArea.EditValue != null
                ? Convert.ToInt32(cmbArea.EditValue) : -1;

            var frm = new frmAreas();
            frm.Activate();
            frm.LoadCountries();
            frm.cmbCountry.SelectedValue = cmbCountry.EditValue;
            frm.LoadCities(Convert.ToInt32(cmbCountry.EditValue));
            frm.cmbCity.SelectedValue = cmbCity.EditValue;
            frm.ShowDialog();

            LoadAreas(Convert.ToInt32(cmbCity.EditValue));

            if (previousId != -1)
                cmbArea.EditValue = previousId;
        }

        private void BtnActAdd_Click(object sender, RoutedEventArgs e)
        {
            int previousId = cmbActs.EditValue != null
                ? Convert.ToInt32(cmbActs.EditValue) : -1;

            var frm = new frmActs();
            frm.Activate();
            frm.ShowDialog();

            LoadActs();

            if (previousId != -1)
                cmbActs.EditValue = previousId;
        }

        #endregion

        #region Documents

        private void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Images & PDF|*.jpg;*.jpeg;*.png;*.pdf|All Files|*.*",
                Title = "تحديد الملفات",
                Multiselect = true
            };

            if (openDialog.ShowDialog() == true)
                filePathDocument = openDialog.FileNames;
        }

        private void BtnViewDocuments_Click(object sender, RoutedEventArgs e)
        {
            if (_code == -1)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show(
                    "يجب حفظ العميل أولاً",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            DataTable docTable = new DataTable();
            GetDocuments(ref docTable, _code.ToString());

            if (docTable.Rows.Count > 0)
            {
                Frmshowdocument docForm = new Frmshowdocument();
                docForm.type = 4;
                docForm.GlobalIdDoc = _code.ToString();

                var documentItems = new System.Collections.ObjectModel.ObservableCollection<DocumentDisplayItem>();

                for (int rowIndex = 0; rowIndex < docTable.Rows.Count; rowIndex++)
                {
                    documentItems.Add(new DocumentDisplayItem
                    {
                        RowNumber = rowIndex + 1,
                        FileName = docTable.Rows[rowIndex]["FileName"]?.ToString() ?? string.Empty,
                        FileUrl = docTable.Rows[rowIndex]["FileUrl"]?.ToString() ?? string.Empty
                    });
                }

                docForm.DataGridView1.ItemsSource = documentItems;
                docForm.Show();
                docForm.Activate();
            }
            else
            {
                DevExpress.Xpf.Core.DXMessageBox.Show(
                    "لا يوجد وثائق",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        public static void InsertDocument(string customerId)
        {
            string docsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Documents");

            if (!Directory.Exists(docsPath))
                Directory.CreateDirectory(docsPath);

            using var sqlConn = new SqlConnection(MainClass.connstr);

            foreach (string filePath in filePathDocument)
            {
                if (sqlConn.State == ConnectionState.Open)
                    sqlConn.Close();

                sqlConn.Open();

                using var maxCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(id), 0) + 1 FROM Documents",
                    sqlConn);
                int nextId = Convert.ToInt32(maxCmd.ExecuteScalar());

                string destPath = Path.Combine(docsPath,
                    $"Cust{nextId}{Path.GetExtension(filePath)}");

                File.Copy(filePath, destPath, overwrite: true);

                using var insertCmd = new SqlCommand(
                    "INSERT INTO Documents(FileName, FileUrl, GlobalID, type) " +
                    "VALUES(@FileName, @FileUrl, @GlobalID, 4)",
                    sqlConn);
                insertCmd.Parameters.AddWithValue("@FileName",
                    Path.GetFileName(filePath));
                insertCmd.Parameters.AddWithValue("@FileUrl", destPath);
                insertCmd.Parameters.AddWithValue("@GlobalID", customerId);
                insertCmd.ExecuteNonQuery();
            }
        }

        public static void GetDocuments(ref DataTable docTable, string customerId)
        {
            using var sqlConn = new SqlConnection(MainClass.connstr);
            using var cmd = new SqlCommand(
                "SELECT Id, FileName, FileUrl FROM Documents " +
                "WHERE type=4 AND GlobalID=@GlobalID",
                sqlConn);
            cmd.Parameters.AddWithValue("@GlobalID", customerId);

            try
            {
                sqlConn.Open();
                var adapter = new SqlDataAdapter(cmd);
                var result = new DataTable();
                adapter.Fill(result);

                if (result.Rows.Count > 0)
                    docTable = result;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ أثناء استرجاع الوثائق:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Close

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Utility Methods

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
    public class DocumentDisplayItem
    {
        public int RowNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
    }
}