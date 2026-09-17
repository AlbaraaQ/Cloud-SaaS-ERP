using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Index;
        private int Ower;
        private ObservableCollection<MarineRow> _marineRows;

        #endregion

        #region Constructor

        public frmItemM()
        {
            conn = MainClass.ConnObj();
            Index = -1;
            _marineRows = new ObservableCollection<MarineRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmItemM_Load(object sender, RoutedEventArgs e)
        {
            LoadDG();
            LoadGroup();
            LoadNextNo();
            LoadOwners();

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtName.Text = "";
            txtNo.Text = "";
            cmbGroup.SelectedIndex = -1;
            cmbOwners.Text = "";
            txtPsngerNo.Text = "";
            txtMarinacode.Text = "";
            txtOwnerPerc.Text = "";
            txtSrchGrp.Text = "";
            txtSrchNo.Text = "";
            txtSrchOwn.Text = "";
            dgvData.UnselectAll();
            clk_Inplan.IsChecked = true;
            Index = -1;
            LoadNextNo();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            LoadNextNo();
        }

        #endregion

        #region Load Data

        private void LoadDG()
        {
            LoadDGWithCondition("select * from Marine where IS_Deleted=0");
        }

        private void LoadDGWithCondition(string sql)
        {
            try
            {
                _marineRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                bool isArabic = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    bool inPlan = false;
                    bool.TryParse(row["Is_InPlan"].ToString(), out inPlan);

                    int ownerId = 0;
                    int.TryParse(row["OwnerId"].ToString(), out ownerId);

                    _marineRows.Add(new MarineRow
                    {
                        Column3 = row["Groupcode"].ToString(),
                        Column1 = row["id"].ToString(),
                        Column2 = row["MarineCode"].ToString(),
                        Column5 = isArabic ? row["Name"].ToString() : row["nameEN"].ToString(),
                        Column4 = GetOwnerName(ownerId),
                        Column6 = inPlan
                    });
                }

                dgvData.ItemsSource = _marineRows;
                dgvData.UnselectAll();
                LoadNextNo();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex);
            }
        }

        private void LoadDG(string condition)
        {
            LoadDGWithCondition(condition);
        }

        private void LoadGroup()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name,code from GroupMarine where IsDeleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbGroup.ItemsSource = dt.DefaultView;
                    cmbGroup.DisplayMemberPath = "code";
                    cmbGroup.SelectedValuePath = "name";
                    cmbGroup.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ تحميل المجموعات", ex);
            }
        }

        private void LoadNextNo()
        {
            try
            {
                txtNo.Text = "";
                int nextNo = 1;

                SqlDataAdapter adapter = new SqlDataAdapter("select id from Marine", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    nextNo = dt.Rows.Count + 1;

                txtNo.Text = nextNo.ToString();
                txtOwnerPerc.Text = "70";
            }
            catch { txtNo.Text = "1"; }
        }

        public void LoadOwners()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from Owners where IS_Deleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbOwners.ItemsSource = dt.DefaultView;
                cmbOwners.DisplayMemberPath = "name";
                cmbOwners.SelectedValuePath = "id";
                cmbOwners.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ تحميل الملاك", ex);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmbGroup.Text))
                {
                    DXMessageBox.Show("ادخل المركب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                bool inPlan = clk_Inplan.IsChecked == true;
                bool isUpdate = false;

                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    "select id from Marine where id='" + txtNo.Text + "'", conn);
                DataTable checkDt = new DataTable();
                checkAdapter.Fill(checkDt);

                if (checkDt.Rows.Count == 0)
                {
                    // فحص تكرار رقم المركب
                    SqlDataAdapter codeCheck = new SqlDataAdapter(
                        "select id from Marine where IS_Deleted=0 and MarineCode='" + txtMarinacode.Text + "'", conn);
                    DataTable codeDt = new DataTable();
                    codeCheck.Fill(codeDt);

                    if (codeDt.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "رقم المركب تم إدخاله مسبقًا" : "Group is previously inserted";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtMarinacode.Focus();
                        return;
                    }

                    // فحص تكرار الاسم
                    SqlDataAdapter nameCheck = new SqlDataAdapter(
                        "select id from Marine where IS_Deleted=0 and Name='" + txtName.Text + "'", conn);
                    DataTable nameDt = new DataTable();
                    nameCheck.Fill(nameDt);

                    if (nameDt.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "اسم المركب تم إدخاله مسبقًا" : "Group is previously inserted";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtName.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        DXMessageBox.Show("يجب إدخال اسم المركب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtName.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtgroupCode.Text))
                    {
                        DXMessageBox.Show("يجب اختيار الفئة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtgroupCode.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtMarinacode.Text))
                    {
                        DXMessageBox.Show("يجب إدخال رقم المركب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtMarinacode.Focus();
                        return;
                    }

                    double ownerPerc = 0;
                    double.TryParse(txtOwnerPerc.Text, out ownerPerc);

                    if (ownerPerc < 0)
                    {
                        DXMessageBox.Show("يجب إدخال نسبة مالك المركب", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtOwnerPerc.Focus();
                        return;
                    }

                    object ownerValue = cmbOwners.SelectedValue ?? (object)DBNull.Value;

                    SqlCommand insertCmd = new SqlCommand(
                        "insert into Marine(id,MarineCode,Groupcode,Name,OwnerId,OwnerPercent,PassengerNo,Is_InPlan,IS_Deleted) " +
                        "values(@id,@MarineCode,@Groupcode,@Name,@OwnerId,@OwnerPercent,@PassengerNo,@Is_InPlan,0)", conn);

                    insertCmd.Parameters.AddWithValue("@id", txtNo.Text);
                    insertCmd.Parameters.AddWithValue("@MarineCode", txtMarinacode.Text);
                    insertCmd.Parameters.AddWithValue("@Groupcode", cmbGroup.Text);
                    insertCmd.Parameters.AddWithValue("@Name", txtName.Text);
                    insertCmd.Parameters.AddWithValue("@OwnerId", ownerValue);
                    insertCmd.Parameters.AddWithValue("@OwnerPercent", ownerPerc);
                    insertCmd.Parameters.AddWithValue("@PassengerNo", txtPsngerNo.Text);
                    insertCmd.Parameters.AddWithValue("@Is_InPlan", inPlan ? 1 : 0);
                    insertCmd.ExecuteNonQuery();
                }
                else
                {
                    isUpdate = true;
                    object ownerValue = cmbOwners.SelectedValue ?? (object)DBNull.Value;

                    SqlCommand updateCmd = new SqlCommand(
                        "update Marine set MarineCode=@MarineCode, Groupcode=@Groupcode, Name=@Name, " +
                        "OwnerId=@OwnerId, OwnerPercent=@OwnerPercent, PassengerNo=@PassengerNo, Is_InPlan=@Is_InPlan " +
                        "where id=@id", conn);

                    updateCmd.Parameters.AddWithValue("@id", txtNo.Text);
                    updateCmd.Parameters.AddWithValue("@MarineCode", txtMarinacode.Text);
                    updateCmd.Parameters.AddWithValue("@Groupcode", cmbGroup.Text);
                    updateCmd.Parameters.AddWithValue("@Name", txtName.Text);
                    updateCmd.Parameters.AddWithValue("@OwnerId", ownerValue);
                    double ownerPerc = 0;
                    double.TryParse(txtOwnerPerc.Text, out ownerPerc);
                    updateCmd.Parameters.AddWithValue("@OwnerPercent", ownerPerc);
                    updateCmd.Parameters.AddWithValue("@PassengerNo", txtPsngerNo.Text);
                    updateCmd.Parameters.AddWithValue("@Is_InPlan", inPlan ? 1 : 0);
                    updateCmd.ExecuteNonQuery();
                }

                frmSavedMsg savedMsg = new frmSavedMsg();
                if (isUpdate)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";

                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    CLR();
                    LoadDG();
                    txtName.Focus();
                }
                else if (savedMsg.Pressed == 3)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
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
                    DXMessageBox.Show("اختر مركبًا ليتم حذفه", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                MessageBoxResult confirm = DXMessageBox.Show(
                    "هل أنت متأكد من حذف المركب؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    new SqlCommand("update Marine set IS_Deleted=1 where id=" + Index, conn).ExecuteNonQuery();
                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDG();
                    CLR();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            dgvData.UnselectAll();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd = new SqlCommand(sqlStr, conn);
                ReadData(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;

                dr.Read();
                CLR();

                int.TryParse(dr["id"].ToString(), out Index);
                txtNo.Text = dr["id"].ToString();
                txtName.Text = dr["name"].ToString();
                txtMarinacode.Text = dr["MarineCode"].ToString();
                txtOwnerPerc.Text = dr["OwnerPercent"].ToString();
                txtPsngerNo.Text = dr["PassengerNo"].ToString();

                int ownerId = 0;
                int.TryParse(dr["OwnerId"].ToString(), out ownerId);
                cmbOwners.Text = GetOwnerName(ownerId);

                cmbGroup.Text = dr["Groupcode"].ToString();
                clk_Inplan.IsChecked = Convert.ToBoolean(dr["IS_InPlan"]);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في قراءة البيانات", ex);
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Marine order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Marine order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Marine where id>" + Index + " order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Marine where id<" + Index + " order by id desc");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // للتوسع مستقبلًا
        }

        #endregion

        #region Grid

        private void dgvData_CellClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MarineRow selected = dgvData.SelectedItem as MarineRow;
            if (selected == null) return;

            int rowIndex = _marineRows.IndexOf(selected);
            if (rowIndex < 0) return;

            dgv_RowChng(rowIndex);
        }

        private void dgv_RowChng(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _marineRows.Count) return;

            MarineRow row = _marineRows[rowIndex];
            int.TryParse(row.Column1, out Index);
            Navigate("select * from Marine where id=" + Index);
        }

        #endregion

        #region Search

        private void btnSrch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            if (!string.IsNullOrWhiteSpace(txtSrchGrp.Text))
            {
                LoadDG("Select * from Marine where IS_Deleted=0 and Groupcode='" + txtSrchGrp.Text + "'");
                txtSrchGrp.Text = "";
            }
            else if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
            {
                LoadDG("Select * from Marine where IS_Deleted=0 and id=" + txtSrchNo.Text);
                txtSrchNo.Text = "";
            }
            else if (!string.IsNullOrWhiteSpace(txtSrchOwn.Text))
            {
                int ownerId = GetOwnerId(txtSrchOwn.Text.Trim());
                LoadDG("Select * from Marine where IS_Deleted=0 and OwnerId=" + ownerId);
                txtSrchOwn.Text = "";
            }
        }

        #endregion

        #region Combo Events

        private void cmbGroup_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedValue != null)
                    txtgroupCode.Text = cmbGroup.SelectedValue.ToString();
            }
            catch { }
        }

        private void txtgroupCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtgroupCode.Text))
                    cmbGroup.SelectedValue = txtgroupCode.Text;
            }
            catch { }
        }

        private void btnAddOwner_Click(object sender, RoutedEventArgs e)
        {
            int previousOwnerId = -1;
            if (cmbOwners.SelectedValue != null)
                int.TryParse(cmbOwners.SelectedValue.ToString(), out previousOwnerId);

            frmOwners ownersForm = new frmOwners();
            ownersForm.Title = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? "Define an Owner" : "تعريف مالك";

            ownersForm.ShowDialog();
            LoadOwners();

            try
            {
                if (previousOwnerId != -1)
                    cmbOwners.SelectedValue = previousOwnerId;
            }
            catch { }
        }

        #endregion

        #region KeyDown

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
        }

        #endregion

        #region Helpers

        private string GetOwnerName(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Owners where IS_Deleted=0 and id='" + id + "'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private int GetOwnerId(string name)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id from Owners where IS_Deleted=0 and name='" + name + "'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch { return -1; }
        }

        private string GetGroupName(string code)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from GroupMarine where IsDeleted=0 and code=" + code, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch { return ""; }
        }

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class MarineRow
    {
        public string Column3 { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column5 { get; set; }
        public string Column4 { get; set; }
        public bool Column6 { get; set; }
    }
}