using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCostCenter : ThemedWindow
    {
        #region Fields

        private SqlConnection _connection;
        private int _currentId;

        #endregion

        #region Constructor

        public frmCostCenter()
        {
            InitializeComponent();

            // ✅ إصلاح: استخدام ConnObj() بدلاً من conn
            _connection = MainClass.ConnObj();

            _currentId = -1;
        }

        #endregion

        #region Window Events

        private void frmCostCenter_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ إصلاح: التحقق من الاتصال قبل التحميل
                if (_connection == null)
                {
                    _connection = MainClass.ConnObj();
                    if (_connection == null)
                    {
                        ShowError("خطأ: لم يتم تهيئة الاتصال بقاعدة البيانات",
                            new Exception("Connection is null"));
                        return;
                    }
                }

                // ✅ التحقق من ConnectionString
                if (string.IsNullOrWhiteSpace(_connection.ConnectionString))
                {
                    ShowError("خطأ: سلسلة الاتصال فارغة",
                        new Exception("ConnectionString is empty"));
                    _connection = MainClass.ConnObj();
                    if (_connection == null ||
                        string.IsNullOrWhiteSpace(_connection.ConnectionString))
                    {
                        return;
                    }
                }

                LoadTree();
                LoadParent();
                LoadBranches();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex);
            }
        }

        #endregion

        #region Clear / Reset

        private void ClearForm()
        {
            _currentId = -1;
            Code.Text = "";
            AName.Text = "";
            txtNameEn.Text = "";
            Main.IsChecked = true;

            try
            {
                if (cmbBranches != null)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
            }
            catch { }

            AName.Focus();
        }

        #endregion

        #region Data Loading

        private void LoadParent()
        {
            try
            {
                // ✅ إصلاح: التحقق من الاتصال
                EnsureValidConnection();

                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center " +
                    "WHERE type=1 ORDER BY code",
                    _connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                ParentCode.DisplayMemberPath = "name";
                ParentCode.SelectedValuePath = "code";
                ParentCode.ItemsSource = dt.DefaultView;
                ParentCode.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"LoadParent Error: {ex.Message}");
            }
        }

        public void LoadBranches()
        {
            try
            {
                EnsureValidConnection();

                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches ORDER BY id",
                    _connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.SelectedValue = MainClass.BranchNo;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الفروع", ex);
            }
        }

        #endregion

        #region TreeView

        private void LoadTree()
        {
            try
            {
                EnsureValidConnection();

                TreeView1.Items.Clear();

                var rootNode = new TreeViewItem
                {
                    Header = "🏢 مراكز التكلفة",
                    IsExpanded = true,
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)
                            System.Windows.Media.ColorConverter
                                .ConvertFromString("#1B2642"))
                };

                var adapter = new SqlDataAdapter(
                    "SELECT code, name FROM cost_center " +
                    "WHERE ParentCode='' AND code>0",
                    _connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    BuildTreeNodes(rootNode, dt);

                TreeView1.Items.Add(rootNode);
                rootNode.IsExpanded = true;

                // توسيع المستوى الأول
                foreach (TreeViewItem child in rootNode.Items)
                    child.IsExpanded = true;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الشجرة", ex);
            }
        }

        private void BuildTreeNodes(TreeViewItem parentNode, DataTable subNodesDt)
        {
            foreach (DataRow row in subNodesDt.Rows)
            {
                string nodeCode = row[0]?.ToString() ?? "";
                string nodeName = row[1]?.ToString() ?? "";
                string display = $"{nodeCode}-{nodeName}";

                var childNode = new TreeViewItem
                {
                    Header = display,
                    Tag = nodeCode
                };

                // ✅ إصلاح: التحقق من الاتصال
                EnsureValidConnection();

                var adapter = new SqlDataAdapter(
                    $"SELECT code, name, name_en FROM cost_center " +
                    $"WHERE ParentCode={nodeCode} AND code>0",
                    _connection);
                var dtChildren = new DataTable();
                adapter.Fill(dtChildren);

                if (dtChildren.Rows.Count > 0)
                    BuildTreeNodes(childNode, dtChildren);

                parentNode.Items.Add(childNode);
            }
        }

        private void TreeView1_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (e.NewValue is not TreeViewItem selectedNode)
                    return;

                string nodeText = selectedNode.Header?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(nodeText) ||
                    !nodeText.Contains("-"))
                    return;

                ClearForm();

                string codeText = nodeText.Substring(0, nodeText.IndexOf("-"));

                EnsureValidConnection();

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM cost_center WHERE code={codeText} AND code>0",
                    _connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    _currentId = Convert.ToInt32(dt.Rows[0]["code"]);
                    Code.Text = dt.Rows[0]["code"]?.ToString() ?? "";
                    AName.Text = dt.Rows[0]["name"]?.ToString() ?? "";
                    txtNameEn.Text = dt.Rows[0]["name_en"]?.ToString() ?? "";

                    TrySetComboValue(ParentCode, dt.Rows[0]["ParentCode"]);
                    TrySetComboValue(cmbBranches, dt.Rows[0]["BranchId"]);

                    int typeValue = Convert.ToInt32(dt.Rows[0]["type"]);
                    if (typeValue == 1)
                        Main.IsChecked = true;
                    else
                        sub1.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"TreeView Selection Error: {ex.Message}");
            }
        }

        #endregion

        #region Code Generation

        private void GenerateCode()
        {
            try
            {
                if (_currentId != -1 || ParentCode.SelectedValue == null)
                    return;

                EnsureValidConnection();

                string parentValue = ParentCode.SelectedValue.ToString();

                var adapter = new SqlDataAdapter(
                    $"SELECT MAX(code) FROM cost_center " +
                    $"WHERE ParentCode={parentValue}",
                    _connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dt.Rows[0][0]?.ToString()))
                {
                    double maxCode = ParseDouble(dt.Rows[0][0]?.ToString());
                    Code.Text = ((int)(maxCode + 1)).ToString();
                }
                else
                {
                    string suffix = Main.IsChecked == true ? "1" : "001";
                    Code.Text = $"{parentValue}{suffix}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"GenerateCode Error: {ex.Message}");
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ✅ إصلاح: التحقق من الاتصال
                EnsureValidConnection();
                EnsureConnectionOpen(_connection);

                // التحقق من الكود
                if (string.IsNullOrWhiteSpace(Code.Text))
                {
                    MessageBox.Show("من فضلك أدخل رقم مركز التكلفة",
                        "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الاسم
                if (string.IsNullOrWhiteSpace(AName.Text))
                {
                    MessageBox.Show("من فضلك أدخل اسم مركز التكلفة",
                        "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AName.Focus();
                    return;
                }

                double codeValue = ParseDouble(Code.Text);

                // التحقق من التكرار عند الإضافة
                if (_currentId == -1)
                {
                    var dupAdapter = new SqlDataAdapter(
                        $"SELECT * FROM cost_center WHERE code={codeValue}",
                        _connection);
                    var dupDt = new DataTable();
                    dupAdapter.Fill(dupDt);

                    if (dupDt.Rows.Count > 0)
                    {
                        MessageBox.Show(
                            "رقم مركز التكلفة مدخل مسبقاً",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // التحقق من وجود الأب
                if (ParentCode.SelectedValue != null)
                {
                    var parentAdapter = new SqlDataAdapter(
                        $"SELECT * FROM cost_center WHERE code={ParentCode.SelectedValue}",
                        _connection);
                    var parentDt = new DataTable();
                    parentAdapter.Fill(parentDt);

                    if (parentDt.Rows.Count == 0)
                    {
                        MessageBox.Show("مركز التكلفة الأب غير موجود",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // التحقق من نوع الأب
                if (ParentCode.SelectedValue != null && sub1.IsChecked == true)
                {
                    var typeAdapter = new SqlDataAdapter(
                        $"SELECT Type FROM cost_center WHERE code={ParentCode.SelectedValue}",
                        _connection);
                    var typeDt = new DataTable();
                    typeAdapter.Fill(typeDt);

                    if (typeDt.Rows.Count > 0 &&
                        Convert.ToInt32(typeDt.Rows[0][0]) == 2)
                    {
                        MessageBox.Show(
                            "مركز التكلفة الأب فرعي، يجب أن يكون رئيسي",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else if (ParentCode.SelectedValue == null && sub1.IsChecked == true)
                {
                    MessageBox.Show("من فضلك أدخل رقم مركز التكلفة الأب",
                        "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int costCenterType = sub1.IsChecked == true ? 2 : 1;

                SqlCommand cmd;

                if (_currentId != -1)
                {
                    // تعديل
                    cmd = new SqlCommand(
                        $"UPDATE cost_center SET " +
                        $"name=@name, name_en=@nameEN, ParentCode=@ParentCode, " +
                        $"type=@type, BranchId=@BranchId " +
                        $"WHERE code={codeValue}",
                        _connection);
                }
                else
                {
                    // إضافة
                    cmd = new SqlCommand(
                        @"INSERT INTO cost_center
                          (code, name, name_en, ParentCode, type,
                           is_Deleted, created_date, BranchId)
                          VALUES
                          (@Code, @name, @nameEN, @ParentCode, @type,
                           @IS_Deleted, @created_date, @BranchId)",
                        _connection);

                    cmd.Parameters.Add("@Code", SqlDbType.Int).Value =
                        (int)codeValue;
                    cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                    cmd.Parameters.Add("@created_date", SqlDbType.DateTime).Value =
                        DateTime.Now;
                }

                cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value =
                    AName.Text;
                cmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value =
                    txtNameEn.Text;
                cmd.Parameters.Add("@type", SqlDbType.Int).Value =
                    costCenterType;
                cmd.Parameters.Add("@ParentCode", SqlDbType.Int).Value =
                    ParentCode.SelectedValue ?? (object)DBNull.Value;
                cmd.Parameters.Add("@BranchId", SqlDbType.Int).Value =
                    cmbBranches.SelectedValue ?? (object)DBNull.Value;

                cmd.ExecuteNonQuery();

                LoadTree();

                string msg = MainClass.Language == "ar"
                    ? "✅ تم حفظ البيانات بنجاح"
                    : "✅ Saved successfully";
                MessageBox.Show(msg, "حفظ",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                object savedParent = ParentCode.SelectedValue;
                ClearForm();
                LoadParent();
                TrySetComboValue(ParentCode, savedParent);
                GenerateCode();
                AName.Focus();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    "هل أنت متأكد من حذف مركز التكلفة؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                if (string.IsNullOrWhiteSpace(Code.Text))
                {
                    MessageBox.Show(
                        "من فضلك اختر مركز تكلفة أولاً أو أدخل رقمه",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureValidConnection();
                double codeValue = ParseDouble(Code.Text);

                // التحقق من الارتباطات
                var entryAdapter = new SqlDataAdapter(
                    $"SELECT res_id FROM Entry_sub WHERE CCcode={codeValue} " +
                    $"AND res_id NOT IN (SELECT id FROM entry WHERE IS_Deleted=1)",
                    _connection);
                var entryDt = new DataTable();
                entryAdapter.Fill(entryDt);

                if (entryDt.Rows.Count > 0)
                {
                    MessageBox.Show(
                        "مركز التكلفة له ارتباطات فرعية لا يمكن حذفه",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من وجوده
                var existAdapter = new SqlDataAdapter(
                    $"SELECT * FROM cost_center WHERE code={codeValue}",
                    _connection);
                var existDt = new DataTable();
                existAdapter.Fill(existDt);

                if (existDt.Rows.Count == 0)
                {
                    MessageBox.Show("مركز التكلفة غير موجود",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الأبناء
                var childAdapter = new SqlDataAdapter(
                    $"SELECT * FROM cost_center WHERE ParentCode={codeValue}",
                    _connection);
                var childDt = new DataTable();
                childAdapter.Fill(childDt);

                if (childDt.Rows.Count > 0)
                {
                    MessageBox.Show(
                        "لا يمكن حذف مركز التكلفة لأن له مراكز تكلفة فرعية",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تنفيذ الحذف
                EnsureConnectionOpen(_connection);

                new SqlCommand(
                    $"DELETE FROM cost_center WHERE code={codeValue}",
                    _connection).ExecuteNonQuery();

                LoadTree();
                MessageBox.Show("✅ تم حذف مركز التكلفة بنجاح",
                    "حذف", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
            }
            finally
            {
                EnsureConnectionClosed(_connection);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            if (ParentCode.SelectedValue != null)
                GenerateCode();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ParentCode_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            // ✅ إصلاح: منع التنفيذ أثناء التحميل
            if (!IsLoaded) return;
            GenerateCode();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// ✅ إصلاح: التحقق من صحة الاتصال وإعادة إنشائه إذا لزم
        /// </summary>
        private void EnsureValidConnection()
        {
            if (_connection == null ||
                string.IsNullOrWhiteSpace(_connection.ConnectionString))
            {
                _connection = MainClass.ConnObj();

                if (_connection == null ||
                    string.IsNullOrWhiteSpace(_connection.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "لم يتم تهيئة الاتصال بقاعدة البيانات بشكل صحيح. " +
                        "تأكد من تسجيل الدخول أولاً.");
                }
            }
        }

        private static double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return double.TryParse(text, out double result) ? result : 0;
        }

        private static void TrySetComboValue(ComboBox comboBox, object value)
        {
            try
            {
                if (value != null && value != DBNull.Value)
                    comboBox.SelectedValue = value;
            }
            catch { }
        }

        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Open)
                connection.Open();
        }

        private static void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
                connection.Close();
        }

        private static void ShowError(string title, Exception ex)
        {
            MessageBox.Show(
                $"{title}{Environment.NewLine}تفاصيل الخطأ: {ex.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Window Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureConnectionClosed(_connection);
        }

        #endregion
    }
}