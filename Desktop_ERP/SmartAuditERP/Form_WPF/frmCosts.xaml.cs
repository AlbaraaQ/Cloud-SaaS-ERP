using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using log4net;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCosts : ThemedWindow
    {
        #region ── Private Fields ──

        private SqlConnection conn;
        private int selectedId = -1;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Constructor ──

        public frmCosts()
        {
            conn = MainClass.ConnObj();
            selectedId = -1;
            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAccounts();
            LoadCost();
            LoadCostCenter();
        }

        #endregion

        #region ── Data Loading ──

        private void LoadAccounts()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Code, AName FROM Accounts_Index " +
                    $"WHERE Type=2 {Accounting.BranchCondition}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadCostCenter()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT ID, name FROM Cost_Center " +
                    "WHERE type=2 AND Is_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbCostCenter.ItemsSource = dt.DefaultView;
                    cmbCostCenter.DisplayMemberPath = "name";
                    cmbCostCenter.SelectedValuePath = "ID";
                    cmbCostCenter.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadCost()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT costs.id, costs.name, costs.nameEn, " +
                    "account, Accounts_Index.AName, " +
                    "Cost_Center.name AS CCName, costs.costCenterId " +
                    "FROM costs " +
                    "LEFT JOIN Accounts_Index " +
                    "  ON costs.account = Accounts_Index.Code " +
                    "LEFT JOIN Cost_Center " +
                    "  ON costs.costCenterId = Cost_Center.ID",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                GridControl1.ItemsSource = dt.DefaultView;

                // تعيين عناوين الأعمدة
                bool isArabic = (MainClass.Language == "ar");

                SetColumnHeader("id", isArabic ? "الرقم" : "No");
                SetColumnHeader("name", isArabic ? "الاسم" : "Name Ar");
                SetColumnHeader("nameEn", isArabic ? "الاسمEn" : "Name En");
                SetColumnHeader("account", isArabic ? "الحساب" : "Account");
                SetColumnHeader("AName", isArabic ? "الحساب" : "Account");
                SetColumnHeader("CCName", isArabic ? "مركز تكلفة" : "Cost center");
                SetColumnHeader("costCenterId", isArabic ? "مركز تكلفة" : "Cost center");
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
        }

        private void SetColumnHeader(string fieldName, string caption)
        {
            var col = GridControl1.Columns[fieldName];
            if (col != null) col.Header = caption;
        }

        #endregion

        #region ── Get Next ID ──

        private int CostNo()
        {
            try
            {
                using var sqlConn = MainClass.ConnObj();
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                var cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(id), 0) FROM costs", sqlConn);
                return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
                return 1;
            }
        }

        #endregion

        #region ── Save ──

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtCost.Text))
                {
                    ShowMsg("الرجاء إدخال اسم للتكلفة", "", MessageBoxImage.Warning);
                    txtCost.Focus();
                    return;
                }

                if (cmbAccounts.SelectedIndex == -1)
                {
                    ShowMsg("الرجاء اختيار حساب للتكلفة", "", MessageBoxImage.Warning);
                    cmbAccounts.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd;

                if (selectedId != -1)
                {
                    cmd = new SqlCommand(
                        "UPDATE costs SET name=@name, nameEn=@nameEn, " +
                        "account=@account, costCenterId=@costCenterId " +
                        $"WHERE id={selectedId}", conn);
                }
                else
                {
                    cmd = new SqlCommand(
                        "INSERT INTO costs(id, name, nameEn, account, costCenterId) " +
                        "VALUES(@id, @name, @nameEn, @account, @costCenterId)", conn);
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = CostNo();
                }

                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtCost.Text;
                cmd.Parameters.Add("@nameEn", SqlDbType.NVarChar).Value = txtCostEn.Text;
                cmd.Parameters.Add("@account", SqlDbType.NVarChar).Value =
                    cmbAccounts.SelectedValue ?? (object)DBNull.Value;

                if (string.IsNullOrEmpty(cmbCostCenter.Text) ||
                    cmbCostCenter.SelectedValue == null)
                    cmd.Parameters.Add("@costCenterId", SqlDbType.Int).Value = -1;
                else
                    cmd.Parameters.Add("@costCenterId", SqlDbType.Int).Value =
                        cmbCostCenter.SelectedValue;

                cmd.ExecuteNonQuery();
                ShowMsg("تم الحفظ");
            }
            catch (Exception ex)
            {
                ShowMsg("حدث خطأ أثناء الحفظ\n" + ex.Message,
                        "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
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
                if (selectedId == -1) return;

                // فحص الارتباط بالفواتير
                var checkAdapter = new SqlDataAdapter(
                    "SELECT Inv.InvGlobalID FROM Inv " +
                    "WHERE IS_Deleted=0 AND InvGlobalID IN " +
                    "(SELECT InvGlobalID FROM InvoiceCost " +
                    $"WHERE InvoiceCostId={selectedId})", conn);
                var dtCheck = new DataTable();
                checkAdapter.Fill(dtCheck);

                if (dtCheck.Rows.Count > 0)
                {
                    ShowMsg(
                        (MainClass.Language == "ar")
                            ? "لا يمكن حذف تكلفة مرتبطة بفواتير"
                            : "Cost previously used in Invoices",
                        "", MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "هل أنت متأكد من حذف التكلفة؟",
                    "تحذير",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

                if (result != MessageBoxResult.Yes) return;

                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(
                    $"DELETE FROM costs WHERE id={selectedId}", conn)
                    .ExecuteNonQuery();

                ShowMsg((MainClass.Language == "ar") ? "تم الحذف" : "Deleted");
                CLR();
                LoadCost();
            }
            catch (Exception ex)
            {
                string errMsg = (MainClass.Language == "en")
                    ? $"Error in delete\nError details: {ex.Message}"
                    : $"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}";
                ShowMsg(errMsg, "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ── Clear ──

        private void CLR()
        {
            txtCost.Text = "";
            txtCostEn.Text = "";
            selectedId = -1;
            cmbAccounts.SelectedIndex = -1;
            cmbCostCenter.SelectedIndex = -1;
        }

        #endregion

        #region ── Account Search ──

        private void SearchByName()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Code, AName, CostCenter FROM Accounts_Index " +
                    $"WHERE AName=N'{cmbAccounts.Text}' AND type=2", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbAccounts.SelectedValue = dt.Rows[0]["Code"];
                else
                    AddNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
        }

        private void AddNewItem()
        {
            try
            {
                var form = new frmAccountSrch
                {
                    cond = ""
                };
                form.txtSrchNm.Text = cmbAccounts.Text;
                form.ShowDialog();

                if (form.Code > -1)
                {
                    var adapter = new SqlDataAdapter(
                        "SELECT Code, AName FROM Accounts_Index " +
                        $"WHERE type=2 AND Code={form.Code}", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                        cmbAccounts.SelectedValue = dt.Rows[0]["Code"];
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Button Events ──

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
            LoadCost();
            CLR();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region ── ComboBox Events ──

        private void cmbAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) SearchByName();
        }

        private void cmbAccounts_PreviewClick(object sender,
            MouseButtonEventArgs e)
        {
            if (cmbAccounts.SelectedIndex == -1)
                AddNewItem();
        }

        #endregion

        #region ── Grid Events ──

        private void GridView1_FocusedRowChanged(object sender,
    FocusedRowChangedEventArgs e)
        {
            try
            {
                // ✅ الطريقة الصحيحة في DevExpress WPF
                var tableView = (TableView)GridControl1.View;
                int focusedRow = tableView.FocusedRowHandle;

                if (focusedRow < 0) return;

                selectedId = Convert.ToInt32(
                    GridControl1.GetCellValue(focusedRow, "id") ?? -1);

                txtCost.Text =
                    GridControl1.GetCellValue(focusedRow, "name")?.ToString() ?? "";

                txtCostEn.Text =
                    GridControl1.GetCellValue(focusedRow, "nameEn")?.ToString() ?? "";

                var accountVal =
                    GridControl1.GetCellValue(focusedRow, "account");
                if (accountVal != null)
                    cmbAccounts.SelectedValue = accountVal;

                var costCenterVal =
                    GridControl1.GetCellValue(focusedRow, "costCenterId");
                if (costCenterVal != null)
                    cmbCostCenter.SelectedValue = costCenterVal;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCosts {ex.Message} {MainClass.UserName}");
            }
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