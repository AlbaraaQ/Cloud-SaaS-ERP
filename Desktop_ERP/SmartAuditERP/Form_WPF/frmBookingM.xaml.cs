using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmBookingM : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int ProcID;
        private int Code;
        private int rowSelected1;
        private int rowSelected2;
        private int _index;
        private bool ISfound;

        private double DefVAT;
        private bool PricIncVAT;
        private double defDelviry;
        private double defInsurance;
        private double defTreasury;
        private bool PrintHeader;
        private bool PrintFooter;
        private int Printtype;
        private int PrintNo;
        private string defPrinter;
        private int ClientN;

        /// <summary>مصدر بيانات جدول الإضافات</summary>
        private ObservableCollection<BookingAdditionItem> _dgvDataList;

        /// <summary>مصدر بيانات جدول البحث</summary>
        private ObservableCollection<BookingSearchItem> _dgvSrchList;

        #endregion

        #region Constructor

        public frmBookingM()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            ProcID = -1;
            Code = -1;
            rowSelected1 = -1;
            rowSelected2 = -1;
            _index = -1;
            ISfound = false;
            DefVAT = 0.0;
            PricIncVAT = false;
            defDelviry = 0.0;
            defInsurance = 0.0;
            defTreasury = 0.0;
            PrintHeader = true;
            PrintFooter = true;
            PrintNo = 1;
            ClientN = 0;

            InitializeComponent();

            _dgvDataList = new ObservableCollection<BookingAdditionItem>();
            dgvData.ItemsSource = _dgvDataList;

            _dgvSrchList = new ObservableCollection<BookingSearchItem>();
            dgvSrch.ItemsSource = _dgvSrchList;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadBokNo();
                LoadClient();
                LoadAdditions();
                LoadGroup();
                LoadMainSettings();

                txtDate.EditValue = DateTime.Now.Date;
                txtToDate.EditValue = DateTime.Now.Date;
                txtFromDate.EditValue = DateTime.Now.Date;

                cmbBookingStatu.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل النافذة:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// بديل frmBookingM_KeyDown - Enter يضيف للجدول
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Add2Dgv();
        }

        #endregion

        #region Data Loading

        private void LoadGroup()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name, code from GroupMarine " +
                    "where IsDeleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbGroup.ItemsSource = dt.DefaultView;
                    cmbGroup.DisplayMemberPath = "code";
                    cmbGroup.SelectedValuePath = "id";
                    cmbGroup.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الفئات:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadMarine()
        {
            try
            {
                if (string.IsNullOrEmpty(cmbGroup.Text)) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, Name, MarineCode from Marine " +
                    "where IS_Deleted=0 and Groupcode='" + cmbGroup.Text + "'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbMarine.ItemsSource = dt.DefaultView;
                    cmbMarine.DisplayMemberPath = "Name";
                    cmbMarine.SelectedValuePath = "id";
                    cmbMarine.SelectedIndex = -1;
                }
            }
            catch
            {
                // تجاهل الخطأ كما في الكود الأصلي
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadClient()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name from Customers " +
                    "where (type=1 or type=3) and IS_Deleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbClients.ItemsSource = dt.DefaultView;
                    cmbClients.DisplayMemberPath = "name";
                    cmbClients.SelectedValuePath = "id";
                    cmbClients.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل العملاء:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadAdditions()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, Name from Additions where IsDeleted=0", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbAdditions.ItemsSource = dt.DefaultView;
                    cmbAdditions.DisplayMemberPath = "name";
                    cmbAdditions.SelectedValuePath = "id";
                    cmbAdditions.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل الإضافات:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadBokNo()
        {
            txtNo.Text = "";
            int nextNum = 1;

            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Bid from Booking where IsDeleted=0", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    try { nextNum = dt.Rows.Count + 1; }
                    catch { }
                }
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }

            txtNo.Text = nextNum.ToString();
            Code = -1;
        }

        private void LoadMainSettings()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from SettingGeneral where Inv_Id=4", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    PricIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                    defDelviry = Convert.ToDouble(dt.Rows[0]["DeliveryVal"]);
                    defInsurance = Convert.ToDouble(dt.Rows[0]["InsureVal"]);
                    DefVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                    defTreasury = Convert.ToDouble(dt.Rows[0]["Treasury"]);
                }

                adapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=4", conn);
                dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        Printtype = Convert.ToInt32(dt.Rows[0]["printType"]);
                        PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                        defPrinter = dt.Rows[0]["CasherPrinter"].ToString();
                        PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Helper Methods

        private string GetGrpid(int marineId)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select GroupMarine.id from GroupMarine, Marine " +
                    "where GroupMarine.code=Marine.GroupCode and Marine.id=" + marineId, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }

            return "";
        }

        private string GetGroupCode(int id)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select code from GroupMarine where id=" + id + " order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }

            return "";
        }

        private string GetAdditionsName(int id)
        {
            try
            {
                if (conn1.State != ConnectionState.Open) conn1.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "Select name from Additions where id=" + id, conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
            }
            catch { }
            finally
            {
                if (conn1.State != ConnectionState.Closed) conn1.Close();
            }

            return "";
        }

        private DateTime GetEditValue(DevExpress.Xpf.Editors.DateEdit dateEdit)
        {
            if (dateEdit.EditValue is DateTime dt) return dt;
            return DateTime.Today;
        }

        #endregion

        #region Calculation

        /// <summary>
        /// إضافة صنف لجدول dgvData - بديل Add2Dgv
        /// </summary>
        private void Add2Dgv()
        {
            try
            {
                if (string.IsNullOrEmpty(txtQuant.Text))
                {
                    MessageBox.Show("يجب إدخال الكمية  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtQuant.Focus();
                    return;
                }

                if (cmbAdditions.SelectedValue == null) return;

                int selectedAddId = Convert.ToInt32(cmbAdditions.SelectedValue);

                // هل الصنف موجود مسبقاً؟
                ISfound = false;
                _index = -1;

                for (int i = 0; i < _dgvDataList.Count; i++)
                {
                    if (_dgvDataList[i].Id == selectedAddId)
                    {
                        ISfound = true;
                        _index = i;
                        break;
                    }
                }

                double.TryParse(txtQuant.Text, out double quant);
                double.TryParse(txtAdditPrice.Text, out double price);
                double.TryParse(txtAdditiTot.Text, out double total);

                if (ISfound)
                {
                    _dgvDataList[_index].Quantity += quant;
                    _dgvDataList[_index].TotalPrice =
                        _dgvDataList[_index].Quantity * _dgvDataList[_index].UnitPrice;
                }
                else
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(
                        "select * from Additions where IsDeleted=0 and id=" + selectedAddId, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (conn.State != ConnectionState.Closed) conn.Close();

                    if (dt.Rows.Count > 0)
                    {
                        _dgvDataList.Add(new BookingAdditionItem
                        {
                            Id = Convert.ToInt32(dt.Rows[0]["id"]),
                            Description = dt.Rows[0]["name"].ToString(),
                            Quantity = quant,
                            UnitPrice = price,
                            TotalPrice = total
                        });
                    }
                }

                CalcuAll();
                ISfound = false;
            }
            catch { }
        }

        /// <summary>
        /// حساب المجاميع - بديل calcuALL
        /// </summary>
        private void CalcuAll()
        {
            double totalAdditions = 0.0;

            foreach (BookingAdditionItem item in _dgvDataList)
                totalAdditions += item.TotalPrice;

            double.TryParse(txtPrice.Text, out double rentPrice);

            txttotAdditions.Text = totalAdditions.ToString();

            double grandTotal = totalAdditions + rentPrice;
            txtTotal.Text = grandTotal.ToString("0.##");

            double.TryParse(txtTotal.Text, out double totalVal);
            double tax = Math.Round(totalVal * DefVAT / 100.0, 2);
            txtTotTax.Text = tax.ToString();

            double net = totalVal + tax;
            txtTotNet.Text = net.ToString("0.##");
        }

        private void ClearAll()
        {
            _dgvDataList.Clear();
            _dgvSrchList.Clear();

            if (int.TryParse("0", out _))
            {
                hour.Text = "0";
                minute.Text = "0";
            }

            cmbClients.SelectedIndex = -1;
            cmbMarine.SelectedIndex = -1;
            cmbGroup.SelectedIndex = -1;
            cmbBookingStatu.SelectedIndex = 1;
            cmbAdditions.SelectedIndex = -1;

            txtNotes.Text = "";
            txtPrice.Text = "0";
            txtQuant.Text = "0";
            txtTime.Text = "";
            txtAdditiTot.Text = "";
            txtSrchNo.Text = "";
            txtClientName.Text = "";
            txtCustPhone.Text = "";
        }

        #endregion

        #region Save

        private void Save()
        {
            SqlTransaction transaction = null;

            try
            {
                if (string.IsNullOrWhiteSpace(txtNo.Text)) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                transaction = conn.BeginTransaction();

                if (cmbClients.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار العميل  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbClients.Focus();
                    return;
                }

                if (cmbGroup.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار الفئة  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbGroup.Focus();
                    return;
                }

                if (cmbMarine.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار المركب  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbMarine.Focus();
                    return;
                }

                if (cmbBookingStatu.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب تحديد حالة الحجز  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    cmbBookingStatu.Focus();
                    return;
                }

                int.TryParse(hour.Text, out int hourVal);
                int.TryParse(minute.Text, out int minuteVal);

                if (hourVal == 0 && minuteVal == 0)
                {
                    MessageBox.Show("يجب تحديد مدة الحجز  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string bookingType = rbNormal.IsChecked == true
                    ? "حجز عادي" : "بحر مفتوح";

                int empNo = MainClass.EmpNo;
                int procType = 4;
                int invId = Code;

                if (Code == -1)
                {
                    if (conn1.State != ConnectionState.Open) conn1.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(
                        "select max(id), max(proc_id) from RentInvoice " +
                        "where branch=" + MainClass.BranchNo, conn1);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (conn1.State != ConnectionState.Closed) conn1.Close();

                    if (dt.Rows.Count > 0)
                    {
                        double.TryParse(dt.Rows[0][0].ToString(), out double maxId);
                        double.TryParse(dt.Rows[0][1].ToString(), out double maxProcId);
                        invId = (int)Math.Round(maxId + 1);
                        ProcID = (int)Math.Round(maxProcId + 1);
                    }
                    else
                    {
                        ProcID = 1;
                    }
                }

                double rentPeriod = hourVal + (minuteVal / 60.0);
                double.TryParse(txtPrice.Text, out double priceVal);
                double.TryParse(txttotAdditions.Text, out double totAdditions);
                double.TryParse(txtTotNet.Text, out double totNet);
                double.TryParse(txtTotTax.Text, out double totTax);

                // إدراج أو تحديث RentInvoice
                SqlCommand cmd = Code == -1
                    ? new SqlCommand("InsertRentInv", conn, transaction)
                    : new SqlCommand("UpdateRentInv", conn, transaction);

                cmd.CommandType = CommandType.StoredProcedure;

                if (Code != -1)
                    cmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = ProcID;

                cmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = procType;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = invId;
                cmd.Parameters.Add("@date", SqlDbType.DateTime).Value =
                    GetEditValue(txtDate);
                cmd.Parameters.Add("@stock", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@MarineId", SqlDbType.Int).Value =
                    cmbMarine.SelectedValue;
                cmd.Parameters.Add("@GroupId", SqlDbType.Int).Value =
                    cmbGroup.SelectedValue;
                cmd.Parameters.Add("@cust_id", SqlDbType.Int).Value =
                    cmbClients.SelectedValue;
                cmd.Parameters.Add("@sales_emp", SqlDbType.Int).Value = MainClass.EmpNo;
                cmd.Parameters.Add("@tot_rent", SqlDbType.Float).Value = priceVal;
                cmd.Parameters.Add("@tot_Additions", SqlDbType.Float).Value = totAdditions;
                cmd.Parameters.Add("@tot_net", SqlDbType.Float).Value = totNet;
                cmd.Parameters.Add("@RentPeriod", SqlDbType.Int).Value = rentPeriod;
                cmd.Parameters.Add("@paid", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@Discount", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@Insurance", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@tax", SqlDbType.Float).Value = totTax;
                cmd.Parameters.Add("@Restraction_id", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@cash", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@visa", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                cmd.Parameters.Add("@note", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@pay_type", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@bank", SqlDbType.Int).Value = -1;
                cmd.ExecuteNonQuery();

                // إدراج أو تحديث Booking
                if (Code == -1)
                {
                    cmd = new SqlCommand(
                        "insert into Booking(InvID,MarineId,UserId,ClientId,Bdate," +
                        "dateIn,PeriodHour,PeriodMinute,Price,status,BookingType," +
                        "notes,IsDeleted) values(@invId,@MarineId,@UserId,@ClientId," +
                        "@Bdate,@dateIn,@PeriodHour,@PeriodMinute,@Price,@status," +
                        "@BookingType,@notes,@IsDeleted)", conn, transaction);
                    cmd.Parameters.Add("@InvID", SqlDbType.Int).Value = ProcID;
                }
                else
                {
                    cmd = new SqlCommand(
                        "update Booking set MarineId=@MarineId,UserId=@UserId," +
                        "ClientId=@ClientId,Bdate=@Bdate,dateIn=@dateIn," +
                        "PeriodHour=@PeriodHour,PeriodMinute=@PeriodMinute," +
                        "Price=@Price,status=@status,BookingType=@BookingType," +
                        "notes=@notes,IsDeleted=@IsDeleted where InvId=" + ProcID,
                        conn, transaction);
                }

                cmd.Parameters.Add("@MarineId", SqlDbType.Int).Value =
                    cmbMarine.SelectedValue;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = empNo;
                cmd.Parameters.Add("@ClientId", SqlDbType.Int).Value =
                    cmbClients.SelectedValue;
                cmd.Parameters.Add("@Bdate", SqlDbType.DateTime).Value =
                    GetEditValue(txtDate);
                cmd.Parameters.Add("@dateIn", SqlDbType.DateTime).Value =
                    GetEditValue(cmbBookingDate);
                cmd.Parameters.Add("@status", SqlDbType.NVarChar).Value =
                    (cmbBookingStatu.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                cmd.Parameters.Add("@BookingType", SqlDbType.NVarChar).Value = bookingType;
                cmd.Parameters.Add("@PeriodHour", SqlDbType.Float).Value = hourVal;
                cmd.Parameters.Add("@PeriodMinute", SqlDbType.Float).Value = minuteVal;
                cmd.Parameters.Add("@Price", SqlDbType.Float).Value = priceVal;
                cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    " حجز رقم" + txtNo.Text;
                cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
                cmd.ExecuteNonQuery();

                // حذف الإضافات القديمة
                new SqlCommand(
                    "delete BookingAddition where bookId=" + Code,
                    conn, transaction).ExecuteNonQuery();

                // إدراج الإضافات الجديدة
                if (_dgvDataList.Count > 0)
                {
                    double.TryParse(txtNo.Text, out double bookNo);

                    foreach (BookingAdditionItem item in _dgvDataList)
                    {
                        SqlCommand insertCmd = new SqlCommand(
                            "insert into BookingAddition(bookId,AditionID,Price," +
                            "quanty,notes,IsDeleted) values(@bookId,@AditionID," +
                            "@Price,@quanty,@notes,@IsDeleted)", conn, transaction);

                        insertCmd.Parameters.Add("@bookId", SqlDbType.Int).Value =
                            (int)bookNo;
                        insertCmd.Parameters.Add("@AditionID", SqlDbType.Int).Value =
                            item.Id;
                        insertCmd.Parameters.Add("@Price", SqlDbType.Float).Value =
                            item.UnitPrice;
                        insertCmd.Parameters.Add("@quanty", SqlDbType.Int).Value =
                            (int)item.Quantity;
                        insertCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                        insertCmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
                        insertCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                MessageBox.Show(" تم حفظ الحجز",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearAll();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                MessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// تنقل بين السجلات - بديل Navigate
        /// </summary>
        public void Navigate(string sqlQuery)
        {
            dgvSrch.UnselectAll();

            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                SqlCommand cmd = new SqlCommand(sqlQuery, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                ReadData(dr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في التنقل:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (dr.HasRows)
                {
                    dr.Read();
                    ClearAll();

                    ProcID = (int)Math.Round(Convert.ToDouble(dr["Invid"].ToString()));
                    Code = (int)Math.Round(Convert.ToDouble(dr["Bid"].ToString()));

                    txtNo.Text = Code.ToString();

                    string grpId = GetGrpid(Convert.ToInt32(dr["MarineId"].ToString()));
                    if (!string.IsNullOrEmpty(grpId))
                        cmbGroup.SelectedValue = grpId;

                    cmbMarine.SelectedValue = dr["MarineId"].ToString();
                    cmbClients.SelectedValue = dr["ClientId"].ToString();

                    txtDate.EditValue = Convert.ToDateTime(dr["Bdate"].ToString());
                    cmbBookingDate.EditValue = Convert.ToDateTime(dr["dateIn"].ToString());

                    cmbBookingStatu.Text = dr["status"].ToString();

                    int.TryParse(dr["PeriodHour"].ToString(), out int h);
                    int.TryParse(dr["PeriodMinute"].ToString(), out int m);
                    hour.Text = h.ToString();
                    minute.Text = m.ToString();

                    txtPrice.Text = dr["price"].ToString();

                    string bookType = dr["BookingType"].ToString();
                    if (bookType == "حجز عادي") rbNormal.IsChecked = true;
                    if (bookType == "بحر مفتوح") rbOpenSea.IsChecked = true;

                    dr.Close();

                    // تحميل الإضافات
                    if (conn1.State != ConnectionState.Open) conn1.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(
                        "Select * from BookingAddition " +
                        "where IsDeleted=0 And bookId=" + Code, conn1);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (conn1.State != ConnectionState.Closed) conn1.Close();

                    foreach (DataRow row in dt.Rows)
                    {
                        int addId = (int)Math.Round(Convert.ToDouble(row["AditionID"]));
                        double qty = Convert.ToDouble(row["quanty"]);
                        double price = Convert.ToDouble(row["price"]);

                        _dgvDataList.Add(new BookingAdditionItem
                        {
                            Id = addId,
                            Description = GetAdditionsName(addId),
                            Quantity = qty,
                            UnitPrice = price,
                            TotalPrice = price * qty
                        });
                    }

                    CalcuAll();
                }
                else
                {
                    ClearAll();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ\nتفاصيل الخطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search

        private void Search(int type)
        {
            string cond = "";
            string branchCond = "";

            if (MainClass.BranchNo != -1)
                branchCond = "branch=" + MainClass.BranchNo + " and ";

            if (!string.IsNullOrEmpty(txtSrchNo.Text))
            {
                if (conn1.State != ConnectionState.Open) conn1.Open();

                double.TryParse(txtSrchNo.Text, out double srchNo);

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "Select InvId from Booking where IsDeleted=0 And bId=" + (int)srchNo, conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (conn1.State != ConnectionState.Closed) conn1.Close();

                if (dt.Rows.Count > 0)
                    cond = branchCond + " RentInvoice.Proc_Id=" +
                           dt.Rows[0][0].ToString() + " and ";
            }
            else
            {
                cond = branchCond + " date>=@date1 and date<=@date2 and ";
            }

            if (type == 3)
                cond = branchCond + " RentInvoice.cust_id=" + ClientN + " and ";

            LoadDG(cond);
        }

        private void LoadDG(string cond)
        {
            _dgvSrchList.Clear();

            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select RentInvoice.proc_id, RentInvoice.id as id, " +
                    "RentInvoice.cust_id, RentInvoice.date as date, " +
                    "Customers.name as cust, Customers.mobile " +
                    "from RentInvoice, Customers " +
                    "where proc_type=4 and RentInvoice.IS_Deleted=0 and " +
                    cond +
                    " RentInvoice.cust_id=Customers.id " +
                    "order by RentInvoice.id", conn);

                if (!string.IsNullOrEmpty(cond))
                {
                    DateTime toDate = GetEditValue(txtToDate).AddHours(24.0);
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value =
                        GetEditValue(txtFromDate).ToShortDateString();
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value = toDate;
                }

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _dgvSrchList.Add(new BookingSearchItem
                    {
                        ProcId = Convert.ToInt32(row["proc_id"]),
                        InvoiceId = Convert.ToInt32(row["id"]),
                        BookDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        CustomerName = row["cust"].ToString(),
                        Mobile = row["mobile"].ToString(),
                        CustomerId = Convert.ToInt32(row["cust_id"])
                    });
                }

                dgvSrch.UnselectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في البحث:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void DgvRowChange(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= _dgvSrchList.Count) return;

                ProcID = _dgvSrchList[rowIndex].ProcId;
                Navigate("select * from Booking where InvId=" + ProcID);
            }
            catch { }
        }

        #endregion

        #region Events - ComboBox

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex != -1)
                    LoadMarine();
            }
            catch { }
        }

        private void cmbBookingStatu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // الكود الأصلي كان فارغاً
        }

        private void cmbAdditions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbAdditions.SelectedIndex <= -1) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select SalePrice from Additions " +
                    "where IsDeleted=0 and id=" + cmbAdditions.SelectedValue, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (conn.State != ConnectionState.Closed) conn.Close();

                if (dt.Rows.Count > 0)
                    txtAdditPrice.Text = dt.Rows[0]["SalePrice"].ToString();

                txtQuant.Focus();
            }
            catch { }
        }

        #endregion

        #region Events - TextBox

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPrice.Text != null)
                CalcuAll();
        }

        private void txtPrice_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Tab && e.Key != Key.Enter)
            {
                bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                               (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
                if (!isDigit)
                {
                    MessageBox.Show(" الحقل لا يقبل الا الارقام فقط ",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Handled = true;
                    txtPrice.Focus();
                }
            }
        }

        private void txtQuant_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Back && e.Key != Key.Delete &&
                e.Key != Key.Tab && e.Key != Key.Enter)
            {
                bool isDigit = (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                               (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
                if (!isDigit)
                {
                    MessageBox.Show(" الحقل لا يقبل الا الارقام فقط ",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Handled = true;
                    txtQuant.Focus();
                }
            }
        }

        private void txtQuant_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double.TryParse(txtQuant.Text, out double quant);
                double.TryParse(txtAdditPrice.Text, out double price);

                if (quant >= 1 && cmbAdditions.SelectedIndex != -1)
                    txtAdditiTot.Text = Math.Round(price * quant, 2).ToString();
            }
            catch { }
        }

        private void txtAdditPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double.TryParse(txtAdditPrice.Text, out double price);
                double.TryParse(txtQuant.Text, out double quant);

                if (price > 0 && cmbAdditions.SelectedIndex != -1)
                    txtAdditiTot.Text = Math.Round(price * quant, 2).ToString();
            }
            catch { }
        }

        #endregion

        #region Events - DataGrid

        private void dgvData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvData.SelectedItem is BookingAdditionItem item)
                {
                    cmbAdditions.SelectedValue = item.Id;
                    txtQuant.Text = item.Quantity.ToString();
                }
            }
            catch { }
        }

        private void dgvData_CellClick(object sender, MouseButtonEventArgs e)
        {
            // المنطق تم نقله إلى dgvData_DeleteClick و dgvData_SelectionChanged
        }

        /// <summary>
        /// حذف صف من جدول الإضافات - بديل Column6 Button click
        /// </summary>
        private void dgvData_DeleteClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BookingAdditionItem item)
            {
                string msg = string.Equals(MainClass.Language, "en")
                    ? "Do you want to delete this record"
                    : "هل تريد حذف السجل";

                if (MessageBox.Show(msg, "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                {
                    _dgvDataList.Remove(item);
                    CalcuAll();
                }
            }
        }

        private void dgvSrch_CellClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedIndex >= 0)
            {
                DgvRowChange(dgvSrch.SelectedIndex);
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Events - Buttons

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
            LoadGroup();
            LoadBokNo();
            LoadClient();
            LoadAdditions();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // الكود الأصلي كان فارغاً
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // TODO: تنفيذ الطباعة
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Booking where ISDeleted=0 order by Bid asc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Booking where ISDeleted=0 " +
                     "and Bid>" + Code + " order by Bid asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Booking where ISDeleted=0 " +
                     "and Bid<" + Code + " order by Bid desc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Booking where ISDeleted=0 order by Bid desc");
        }

        private void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            Form_WPF.frmCustomers customersForm = new Form_WPF.frmCustomers();
            MainClass.ApplyPermissionToForm(customersForm);
            MainClass.DoApplyUserSett(customersForm);
            customersForm.Title = string.Equals(MainClass.Language, "en")
                ? "Define A Customer" : "تعريف عميل";
            customersForm.Type = 1;
            customersForm.Show();
            customersForm.Activate();
        }

        private void btnSave2DG_Click(object sender, RoutedEventArgs e)
        {
            Add2Dgv();
            dgvData.UnselectAll();
        }

        private void btnAddObj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Form_WPF.frmAdditions().ShowDialog();
                LoadAdditions();
            }
            catch { }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search(2);

            if (!string.IsNullOrWhiteSpace(txtClientName.Text))
            {
                foreach (BookingSearchItem item in _dgvSrchList)
                {
                    if (string.Equals(item.CustomerName, txtClientName.Text))
                    {
                        ClientN = item.CustomerId;
                        Search(3);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(txtCustPhone.Text)) return;

            foreach (BookingSearchItem item in _dgvSrchList)
            {
                if (string.Equals(item.Mobile, txtCustPhone.Text))
                {
                    ClientN = item.CustomerId;
                    Search(3);
                }
            }
        }

        private void Label14_Click(object sender, MouseButtonEventArgs e)
        {
            // الكود الأصلي كان فارغاً
        }

        private void GroupBox1_Enter(object sender, RoutedEventArgs e)
        {
            // الكود الأصلي كان فارغاً
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// نموذج بيانات صف الإضافات في dgvData
    /// Column5=Id | Column3=Description | Column1=Quantity
    /// Column2=UnitPrice | Column4=TotalPrice | Column6=Delete
    /// </summary>
    public class BookingAdditionItem : System.ComponentModel.INotifyPropertyChanged
    {
        private int _id;
        private string _description = "";
        private double _quantity;
        private double _unitPrice;
        private double _totalPrice;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public double Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public double UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); }
        }

        public double TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(nameof(TotalPrice)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string p) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(p));
    }

    /// <summary>
    /// نموذج بيانات صف نتائج البحث في dgvSrch
    /// Column10=ProcId | Col1=InvoiceId | Col2=BookDate
    /// Col3=CustomerName | mobile=Mobile | Column14=CustomerId
    /// </summary>
    public class BookingSearchItem : System.ComponentModel.INotifyPropertyChanged
    {
        private int _procId;
        private int _invoiceId;
        private string _bookDate = "";
        private string _customerName = "";
        private string _mobile = "";
        private int _customerId;

        public int ProcId { get => _procId; set { _procId = value; OnPropertyChanged(nameof(ProcId)); } }
        public int InvoiceId { get => _invoiceId; set { _invoiceId = value; OnPropertyChanged(nameof(InvoiceId)); } }
        public string BookDate { get => _bookDate; set { _bookDate = value; OnPropertyChanged(nameof(BookDate)); } }
        public string CustomerName { get => _customerName; set { _customerName = value; OnPropertyChanged(nameof(CustomerName)); } }
        public string Mobile { get => _mobile; set { _mobile = value; OnPropertyChanged(nameof(Mobile)); } }
        public int CustomerId { get => _customerId; set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string p) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(p));
    }

    #endregion
}