using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CheckBox = System.Windows.Controls.CheckBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmUsersPermissions : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int           Code      = -1;
        private bool          _Finished = true;

        // قائمة CheckBoxes للعمليات
        private List<CheckBox> operCheckBoxes;

        #endregion

        #region Constructor

        public frmUsersPermissions()
        {
            InitializeComponent();
            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
        }

        #endregion

        #region Window Load

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // بناء قائمة checkboxes العمليات
            operCheckBoxes = new List<CheckBox>
            {
                ckDiscount, CheckBox1, CheckBox2, CheckBox3,
                CheckBox4, CheckBox5, CheckBox6, CheckBox7,
                CheckBox8, CheckBox9, CheckBox10,
                ckShowBranchsAccounts, chkDiscPassword
            };

            LoadEmps();
            BuildPermissionsTree();
            this.WindowState = MainClass.Window_State;
        }

        #endregion

        #region Clear

        private void ClearFields()
        {
            cmbEmp.SelectedIndex       = -1;
            txtUser.Text               = "";
            txtPass.Text               = "";
            MaxDicount.Text            = "0";
            MaxDicountParcent.Text     = "0";
            Code = -1;

            foreach (var cb in operCheckBoxes)
                cb.IsChecked = false;

            // مسح التحديد من الشجرة
            ClearTreeCheckboxes(treeView1.Items);
        }

        // احتفظ بالدالة العامة للـ IEnumerable
        private void ClearTreeCheckboxes<T>(IEnumerable<T> items) where T : PermissionTreeNode
        {
            if (items == null) return;

            foreach (var node in items)
            {
                node.IsChecked = false;
                if (node.Children != null && node.Children.Any())
                {
                    ClearTreeCheckboxes(node.Children);
                }
            }
        }

        // أضف دالة جديدة لـ ItemCollection
        private void ClearTreeCheckboxes(ItemCollection items)
        {
            if (items == null) return;

            foreach (var item in items)
            {
                if (item is PermissionTreeNode node)
                {
                    node.IsChecked = false;
                    if (node.Children != null && node.Children.Any())
                    {
                        ClearTreeCheckboxes(node.Children);
                    }
                }
            }
        }

        #endregion

        #region Load Employees

        public void LoadEmps()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT Users.emp, Employees.name " +
                    "FROM Users, Employees " +
                    "WHERE Users.emp=Employees.id AND Users.IS_Deleted=0 " +
                    "AND Employees.IS_Deleted=0 ORDER BY Users.emp", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbEmp.DisplayMemberPath = "name";
                cmbEmp.SelectedValuePath = "emp";
                cmbEmp.ItemsSource       = table.DefaultView;
                cmbEmp.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الموظفين\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Build Permissions Tree

        private void BuildPermissionsTree()
        {
            treeView1.Items.Clear();

            string rootText = string.Equals(MainClass.Language, "en") ? "screens" : "الشاشات";
            var rootNode    = new PermissionTreeNode("0", rootText);

            LoadTreeNodes(rootNode, 0);
            treeView1.Items.Add(rootNode);
            rootNode.IsExpanded = true;
        }

        private void LoadTreeNodes(PermissionTreeNode parentNode, int parentId)
        {
            try
            {
                string featStr = Convert.ToString(MainClass.Features, 2).PadLeft(4, '0');

                // فلترة المجموعات حسب الميزات
                if ((featStr[3] == '0' && parentId >= 3 && parentId <= 5) ||
                    (featStr[2] == '0' && parentId == 2) ||
                    (featStr[1] == '0' && parentId == 6) ||
                    (featStr[0] == '0' && parentId == 7))
                    return;

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Forms WHERE parent_id={parentId}", conn);
                var table   = new DataTable();
                adapter.Fill(table);

                bool isEn = string.Equals(MainClass.Language, "en");

                foreach (DataRow row in table.Rows)
                {
                    string colName = isEn ? "nameEn" : "name";
                    string nodeId  = row["id"].ToString();
                    string nodeName= row[colName].ToString();

                    var node = new PermissionTreeNode(nodeId, nodeName);

                    if (Convert.ToBoolean(row["IS_Parent"]))
                    {
                        LoadTreeNodes(node, int.Parse(nodeId));
                    }
                    else
                    {
                        node.Tag = "Child";

                        if (Convert.ToBoolean(row["IS_New_Visible"]))
                            node.Children.Add(new PermissionTreeNode("", isEn ? "Save" : "حفظ"));
                        if (Convert.ToBoolean(row["IS_Save_Visible"]))
                            node.Children.Add(new PermissionTreeNode("", isEn ? "Edit" : "تعديل"));
                        if (Convert.ToBoolean(row["IS_Delete_Visible"]))
                            node.Children.Add(new PermissionTreeNode("", isEn ? "delete" : "حذف"));
                        if (Convert.ToBoolean(row["IS_Search_Visible"]))
                            node.Children.Add(new PermissionTreeNode("", isEn ? "search" : "بحث"));
                        if (Convert.ToBoolean(row["IS_Print_Visible"]))
                            node.Children.Add(new PermissionTreeNode("", isEn ? "print" : "طباعة"));
                    }

                    parentNode.Children.Add(node);
                }
            }
            catch { }
        }

        #endregion

        #region Load User Permissions

        private void LoadUserPermissions()
        {
            try
            {
                Code = int.TryParse(cmbEmp.SelectedValue?.ToString(), out int c) ? c : -1;

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM User_Permissions WHERE user_id={cmbEmp.SelectedValue}",
                    conn);
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    string formId = row["Form_id"].ToString();
                    var node      = FindTreeNode(treeView1.Items, formId);
                    if (node == null) continue;

                    foreach (var child in node.Children)
                    {
                        string txt = child.Name.ToLower();
                        if ((txt == "حفظ" || txt == "save") &&
                            Convert.ToBoolean(row["IS_Save"]))
                            child.IsChecked = true;
                        if ((txt == "تعديل" || txt == "edit") &&
                            Convert.ToBoolean(row["IS_Edit"]))
                            child.IsChecked = true;
                        if ((txt == "حذف" || txt == "delete") &&
                            Convert.ToBoolean(row["IS_Delete"]))
                            child.IsChecked = true;
                        if ((txt == "بحث" || txt == "search") &&
                            Convert.ToBoolean(row["IS_Search"]))
                            child.IsChecked = true;
                        if ((txt == "طباعة" || txt == "print") &&
                            Convert.ToBoolean(row["IS_Print"]))
                            child.IsChecked = true;

                        if (child.IsChecked)
                            node.IsChecked = true;
                    }
                }
            }
            catch { }
        }

        private PermissionTreeNode FindTreeNode(ItemCollection items, string id)
        {
            foreach (var item in items)
            {
                if (item is PermissionTreeNode node)
                {
                    if (node.Id == id) return node;
                    var found = FindTreeNode(node.Children, id);
                    if (found != null) return found;
                }
            }
            return null;
        }

        // ItemCollection overload for Children
        private PermissionTreeNode FindTreeNode(
            System.Collections.ObjectModel.ObservableCollection<PermissionTreeNode> items,
            string id)
        {
            foreach (var node in items)
            {
                if (node.Id == id) return node;
                var found = FindTreeNode(node.Children, id);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region Load Operation Permissions

        private void LoadUserOperPermiss()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                foreach (var cb in operCheckBoxes)
                    cb.IsChecked = false;

                foreach (var cb in operCheckBoxes)
                {
                    var idCmd = new SqlCommand(
                        $"SELECT id FROM OperPermissTypes " +
                        $"WHERE name=N'{cb.Content}' OR name_en=N'{cb.Content}'", conn);
                    var scalarId = idCmd.ExecuteScalar();
                    if (scalarId == null) continue;

                    var adapter = new SqlDataAdapter(
                        $"SELECT Activated FROM OperationPermission " +
                        $"WHERE OperNo={scalarId} AND emp={cmbEmp.SelectedValue}", conn);
                    var table   = new DataTable();
                    adapter.Fill(table);

                    if (table.Rows.Count > 0)
                        cb.IsChecked = Convert.ToBoolean(table.Rows[0][0]);
                }
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void LoadUserOperMaxDiscount()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var adapter = new SqlDataAdapter(
                    $"SELECT MaxDicount, MaxDicountParcent FROM OperMaxDiscount " +
                    $"WHERE emp={cmbEmp.SelectedValue}", conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    MaxDicount.Text        = table.Rows[0]["MaxDicount"].ToString();
                    MaxDicountParcent.Text = table.Rows[0]["MaxDicountParcent"].ToString();
                }
            }
            catch { }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ComboBox Events

        private void cmbEmp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                txtUser.Text           = "";
                txtPass.Text           = "";
                MaxDicount.Text        = "0";
                MaxDicountParcent.Text = "0";

                if (cmbEmp.SelectedValue == null) return;

                var adapter = new SqlDataAdapter(
                    $"SELECT id, username, pwd FROM Users WHERE emp={cmbEmp.SelectedValue}",
                    conn);
                var table = new DataTable();
                adapter.Fill(table);

                if (table.Rows.Count > 0)
                {
                    Code        = int.TryParse(table.Rows[0][0].ToString(), out int c) ? c : -1;
                    txtUser.Text = table.Rows[0][1].ToString();
                    txtPass.Text = table.Rows[0][2].ToString();

                    LoadUserPermissions();
                    LoadUserOperPermiss();
                    LoadUserOperMaxDiscount();
                }
            }
            catch { }
        }

        #endregion

        #region TreeView Events

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // يمكن إضافة منطق عند تغيير التحديد
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl1.SelectedIndex == 0)
                SaveFormsPermission();
            else
                SaveOperPermission();
            var Home = new Home();
            // مزامنة MQTT
            if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
            {
                string cond = "";
                ConnectBroker.mqttClient.Publish(
                    ConnectBroker.ClientCode + "Users",
                    Encoding.UTF8.GetBytes(SendData.GetUsers(cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(
                    ConnectBroker.ClientCode + "UserPermissions",
                    Encoding.UTF8.GetBytes(SendData.GetUserPermissions(cond)), 0, retain: true);
                ConnectBroker.mqttClient.Publish(
                    ConnectBroker.ClientCode + "OperationPermission",
                    Encoding.UTF8.GetBytes(SendData.GetOperationPermission(cond)), 0, retain: true);
            }
        }

        private void SaveFormsPermission()
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var transaction = conn.BeginTransaction();
            try
            {
                if (cmbEmp.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب اختيار الموظف",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbEmp.Focus();
                    return;
                }

                var confirm = DXMessageBox.Show("هل أنت متأكد من عملية الحفظ؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                new SqlCommand(
                    $"DELETE FROM User_Permissions WHERE user_id={cmbEmp.SelectedValue}",
                    conn, transaction).ExecuteNonQuery();

                if (treeView1.Items.Count > 0 && treeView1.Items[0] is PermissionTreeNode root)
                    SaveTreeNodes(root, transaction);

                transaction.Commit();
                Code = int.TryParse(cmbEmp.SelectedValue?.ToString(), out int c) ? c : -1;
                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void SaveTreeNodes(PermissionTreeNode parentNode, SqlTransaction tr)
        {
            foreach (var node in parentNode.Children)
            {
                if (node.Tag?.ToString() != "Child")
                {
                    SaveTreeNodes(node, tr);
                }
                else
                {
                    if (!node.IsChecked) continue;

                    int isSave = 0, isEdit = 0, isDel = 0, isSearch = 0, isPrint = 0;

                    foreach (var child in node.Children)
                    {
                        string txt = child.Name.ToLower();
                        if ((txt == "حفظ" || txt == "new" || txt == "save") && child.IsChecked)
                            isSave = 1;
                        if ((txt == "تعديل" || txt == "edit") && child.IsChecked)
                            isEdit = 1;
                        if ((txt == "حذف" || txt == "delete") && child.IsChecked)
                            isDel = 1;
                        if ((txt == "بحث" || txt == "search") && child.IsChecked)
                            isSearch = 1;
                        if ((txt == "طباعة" || txt == "print") && child.IsChecked)
                            isPrint = 1;
                    }

                    var cmd = new SqlCommand(
                        "INSERT INTO User_Permissions" +
                        "(user_id,Form_id,IS_New,IS_Save,IS_Delete,IS_Search,IS_Print,IS_Edit)" +
                        $"VALUES({cmbEmp.SelectedValue},{node.Id}," +
                        $"{isSave},{isSave},{isDel},{isSearch},{isPrint},{isEdit})",
                        conn, tr);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveOperPermission()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtPass.Text))
                {
                    DXMessageBox.Show("ادخل كلمة المرور",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPass.Focus();
                    return;
                }

                if (conn.State != ConnectionState.Open) conn.Open();
                var transaction = conn.BeginTransaction();

                new SqlCommand(
                    $"DELETE FROM OperationPermission WHERE OperNo>7 AND emp={cmbEmp.SelectedValue}",
                    conn, transaction).ExecuteNonQuery();
                new SqlCommand(
                    $"UPDATE OperationPermission SET Activated=0 WHERE emp={cmbEmp.SelectedValue}",
                    conn, transaction).ExecuteNonQuery();

                foreach (var cb in operCheckBoxes)
                {
                    if (cb.IsChecked != true) continue;

                    var idCmd = new SqlCommand(
                        $"SELECT id FROM OperPermissTypes " +
                        $"WHERE name=N'{cb.Content}' OR name_en=N'{cb.Content}'", conn, transaction);
                    var scalarId = idCmd.ExecuteScalar();
                    if (scalarId == null) continue;

                    int operId = Convert.ToInt32(scalarId);

                    var insertCmd = new SqlCommand(
                        "INSERT INTO OperationPermission" +
                        "(emp,OperNo,pwd,lastChanged,IS_Deleted,OperVal,Activated)" +
                        "VALUES(@emp,@OperNo,@pwd,@lastChanged,@IS_Deleted,@OperVal,@Activated)",
                        conn, transaction);
                    insertCmd.Parameters.Add("@emp",         SqlDbType.Int).Value       =
                        cmbEmp.SelectedValue;
                    insertCmd.Parameters.Add("@OperNo",      SqlDbType.Int).Value       = operId;
                    insertCmd.Parameters.Add("@OperVal",     SqlDbType.Float).Value     = 1;
                    insertCmd.Parameters.Add("@Activated",   SqlDbType.Bit).Value       = 1;
                    insertCmd.Parameters.Add("@pwd",         SqlDbType.NVarChar).Value  = txtPass.Text;
                    insertCmd.Parameters.Add("@lastChanged", SqlDbType.DateTime).Value  = DateTime.Now;
                    insertCmd.Parameters.Add("@IS_Deleted",  SqlDbType.Bit).Value       = 0;
                    insertCmd.ExecuteNonQuery();
                }

                // حفظ حدود الخصم
                try
                {
                    new SqlCommand(
                        $"DELETE FROM OperMaxDiscount WHERE emp={cmbEmp.SelectedValue}",
                        conn, transaction).ExecuteNonQuery();

                    var discCmd = new SqlCommand(
                        "INSERT INTO OperMaxDiscount(MaxDicount,MaxDicountParcent,emp)" +
                        "VALUES(@MaxDicount,@MaxDicountParcent,@emp)", conn, transaction);
                    discCmd.Parameters.Add("@MaxDicount",      SqlDbType.Int).Value = MaxDicount.Text;
                    discCmd.Parameters.Add("@MaxDicountParcent",SqlDbType.Int).Value = MaxDicountParcent.Text;
                    discCmd.Parameters.Add("@emp",             SqlDbType.Int).Value =
                        cmbEmp.SelectedValue;
                    discCmd.ExecuteNonQuery();
                }
                catch { }

                transaction.Commit();
                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                txtPass.Text = "";
                User.LoadUserOperPermission();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = DXMessageBox.Show("هل أنت متأكد من عملية الحذف؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                if (Code != -1)
                {
                    if (conn.State != ConnectionState.Open) conn.Open();
                    new SqlCommand(
                        $"DELETE FROM User_Permissions WHERE user_id={cmbEmp.SelectedValue}",
                        conn).ExecuteNonQuery();
                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearFields();
                }
                else
                {
                    DXMessageBox.Show("قم بعرض صلاحيات موظف أولاً",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            try { cmbEmp.SelectedIndex = 0; } catch { }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            try { cmbEmp.SelectedIndex = cmbEmp.Items.Count - 1; } catch { }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbEmp.SelectedIndex < cmbEmp.Items.Count - 1)
                    cmbEmp.SelectedIndex++;
            }
            catch { }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbEmp.SelectedIndex > 0)
                    cmbEmp.SelectedIndex--;
            }
            catch { }
        }

        #endregion

        #region Other Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // منطق الطباعة
        }

        #endregion
    }

    #region PermissionTreeNode Model

    public class PermissionTreeNode : System.ComponentModel.INotifyPropertyChanged
    {
        public string Id   { get; set; }
        public string Name { get; set; }
        public object Tag  { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public System.Collections.ObjectModel.ObservableCollection<PermissionTreeNode> Children
        { get; } = new System.Collections.ObjectModel.ObservableCollection<PermissionTreeNode>();

        public PermissionTreeNode(string id, string name)
        {
            Id   = id;
            Name = name;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(n));
    }

    #endregion
}