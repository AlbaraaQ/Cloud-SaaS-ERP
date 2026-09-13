using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTermsPM : ThemedWindow
    {
        #region Fields

        private SqlConnection con;
        private SqlConnection conn;
        private int ID;
        private double DefVAT;

        #endregion

        #region Constructor

        public frmTermsPM()
        {
            InitializeComponent();
            con    = MainClass.ConnObj();
            conn   = MainClass.ConnObj();
            ID     = -1;
            DefVAT = 0.0;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTree();
            LoadParent();
            AName.Focus();
        }

        #endregion

        #region Clear

        private void CLR()
        {
            ID             = -1;
            Code.Text      = "";
            AName.Text     = "";
            txtNameEn.Text = "";
            main.IsChecked = true;
            txtAprxCost.Text   = "0";
            txtprice.Text      = "0";
            txtLastPrice.Text  = "0";
            txtAprxPeriod.Text = "0";
            AName.Focus();
        }

        #endregion

        #region Tree

        private void LoadTree()
        {
            try
            {
                treeView1.Items.Clear();

                var rootItem = new TreeNodeItem { DisplayText = "البنود" };

                EnsureOpen(con);
                var adapter = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms WHERE ParentCode='' AND Code>0", con);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(con);

                if (dt.Rows.Count > 0)
                    FillTreeNodes(rootItem, dt);

                treeView1.Items.Add(rootItem);
                rootItem.IsExpanded = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الشجرة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillTreeNodes(TreeNodeItem parentNode, DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                string code    = row[0].ToString();
                string name    = row[1].ToString();
                string display = $"{code}-{name}";

                var childNode = new TreeNodeItem { DisplayText = display, Code = code };
                parentNode.Children.Add(childNode);

                // تحميل الأبناء
                try
                {
                    EnsureOpen(con);
                    var childAdapter = new SqlDataAdapter(
                        $"SELECT Code, name, nameEN FROM PM_Terms WHERE ParentCode={code} AND Code>0",
                        con);
                    var childDt = new DataTable();
                    childAdapter.Fill(childDt);
                    EnsureClose(con);

                    if (childDt.Rows.Count > 0)
                        FillTreeNodes(childNode, childDt);
                }
                catch { EnsureClose(con); }
            }
        }

        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNodeItem item && item.DisplayText != "البنود"
                && !string.IsNullOrEmpty(item.Code))
            {
                try
                {
                    CLR();
                    EnsureOpen(con);
                    var adapter = new SqlDataAdapter(
                        $"SELECT * FROM PM_Terms WHERE Code={item.Code} AND Code>0", con);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    EnsureClose(con);

                    if (dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        ID = Convert.ToInt32(row["Code"]);
                        Code.Text      = row["Code"].ToString();
                        AName.Text     = row["name"].ToString();
                        txtNameEn.Text = row["nameEn"].ToString();
                        ParentCode.SelectedValue = row["ParentCode"];

                        int type = Convert.ToInt32(row["Type"]);
                        if (type == 1)
                        {
                            main.IsChecked        = true;
                            GBbranchAcc.Visibility = Visibility.Collapsed;
                        }
                        else if (type == 2)
                        {
                            sub1.IsChecked        = true;
                            GBbranchAcc.Visibility = Visibility.Visible;
                            txtAprxCost.Text   = row["cost"].ToString();
                            txtprice.Text      = row["price"].ToString();
                            txtLastPrice.Text  = row["Lastprice"].ToString();
                            txtAprxPeriod.Text = row["ExecutionPeriod"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    EnsureClose(con);
                }
            }
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && treeView1.SelectedItem is TreeNodeItem item)
            {
                e.Handled = true;
                item.IsExpanded = !item.IsExpanded;
            }
        }

        #endregion

        #region Data Loading

        private void LoadParent()
        {
            try
            {
                EnsureOpen(con);
                var adapter = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms WHERE Type<=1 ORDER BY Code", con);
                var dt = new DataTable();
                adapter.Fill(dt);

                ParentCode.ItemsSource       = dt.DefaultView;
                ParentCode.DisplayMemberPath = "name";
                ParentCode.SelectedValuePath = "Code";
                ParentCode.SelectedIndex     = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل البنود الرئيسية: " + ex.Message); }
            finally { EnsureClose(con); }
        }

        private void GenerateCode()
        {
            try
            {
                if (ID != -1 || ParentCode.SelectedValue == null) return;

                EnsureOpen(con);
                var cmd = new SqlCommand(
                    $"SELECT MAX(Code) FROM PM_Terms WHERE ParentCode={ParentCode.SelectedValue}",
                    con);
                var result = cmd.ExecuteScalar();
                EnsureClose(con);

                if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                    Code.Text = (Convert.ToDouble(result) + 1).ToString();
                else
                    Code.Text = main.IsChecked == true
                        ? $"{ParentCode.SelectedValue}1"
                        : $"{ParentCode.SelectedValue}001";
            }
            catch { EnsureClose(con); }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            if (ParentCode.SelectedValue != null) GenerateCode();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Code.Text))
            {
                DXMessageBox.Show("من فضلك أدخل رقم البند", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }
            if (string.IsNullOrWhiteSpace(AName.Text))
            {
                DXMessageBox.Show("من فضلك أدخل اسم البند", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            try
            {
                EnsureOpen(conn);
                var transaction = conn.BeginTransaction();

                try
                {
                    // التحقق من التكرار للإدراج الجديد
                    if (ID == -1)
                    {
                        var chkCmd = new SqlCommand(
                            $"SELECT * FROM PM_Terms WHERE Code={Convert.ToDouble(Code.Text)}",
                            conn, transaction);
                        var chkDt = new DataTable();
                        new SqlDataAdapter(chkCmd).Fill(chkDt);
                        if (chkDt.Rows.Count > 0)
                        {
                            DXMessageBox.Show("كود البند مدخل مسبقاً", "",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            transaction.Rollback(); return;
                        }
                    }

                    int termType = sub1.IsChecked == true ? 2 : 1;
                    int vatVal   = ckNoTax.IsChecked == true ? 0 : (int)DefVAT;

                    string sql = ID != -1
                        ? $@"UPDATE PM_Terms SET name=@name, nameEN=@nameEN, type=@type,
                             ParentCode=@ParentCode, ExecutionPeriod=@ExecutionPeriod,
                             price=@price, lastprice=@lastprice, cost=@cost,
                             VAT=@VAT, discount=@discount, IS_Deleted=0
                             WHERE Code={Convert.ToDouble(Code.Text)}"
                        : @"INSERT INTO PM_Terms(Code,name,nameEN,type,ParentCode,
                             ExecutionPeriod,price,lastprice,cost,VAT,discount,IS_Deleted)
                             VALUES(@Code,@name,@nameEN,@type,@ParentCode,
                             @ExecutionPeriod,@price,@lastprice,@cost,@VAT,@discount,0)";

                    var cmd = new SqlCommand(sql, conn, transaction);

                    if (ID == -1)
                        cmd.Parameters.AddWithValue("@Code", Convert.ToDouble(Code.Text));

                    cmd.Parameters.AddWithValue("@name",            AName.Text);
                    cmd.Parameters.AddWithValue("@nameEN",          txtNameEn.Text);
                    cmd.Parameters.AddWithValue("@type",            termType);
                    cmd.Parameters.AddWithValue("@ParentCode",      ParentCode.SelectedValue ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@price",           double.TryParse(txtprice.Text,      out double p)   ? p   : 0.0);
                    cmd.Parameters.AddWithValue("@Lastprice",       double.TryParse(txtLastPrice.Text,  out double lp)  ? lp  : 0.0);
                    cmd.Parameters.AddWithValue("@cost",            double.TryParse(txtAprxCost.Text,   out double c)   ? c   : 0.0);
                    cmd.Parameters.AddWithValue("@ExecutionPeriod", int.TryParse(txtAprxPeriod.Text,    out int ep)     ? ep  : 0);
                    cmd.Parameters.AddWithValue("@discount",        0.0);
                    cmd.Parameters.AddWithValue("@VAT",             vatVal);
                    cmd.Parameters.AddWithValue("@IS_Deleted",      false);

                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    LoadTree();
                    int savedParent = ParentCode.SelectedValue != null
                        ? Convert.ToInt32(ParentCode.SelectedValue) : -1;
                    LoadParent();
                    if (savedParent > -1) ParentCode.SelectedValue = savedParent;
                    GenerateCode();

                    var savedMsg = new frmSavedMsg();
                    if (ID != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                    savedMsg.ShowDialog();

                    if (savedMsg.Pressed == 1) { CLR(); AName.Focus(); }
                    else if (savedMsg.Pressed == 3) this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاتصال: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Code.Text))
            {
                DXMessageBox.Show("من فضلك اختر بنداً أولاً أو أدخل كوده", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            try
            {
                // التحقق من الارتباطات
                EnsureOpen(con);
                var chkSub = new SqlDataAdapter(
                    $"SELECT * FROM PM_Terms WHERE ParentCode={Convert.ToDouble(Code.Text)}", con);
                var chkSubDt = new DataTable();
                chkSub.Fill(chkSubDt);

                if (chkSubDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("لا يمكن حذف البند لأن له بنود فرعية", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    EnsureClose(con); return;
                }

                var confirm = DXMessageBox.Show("هل تريد حذف هذا البند؟", "تأكيد الحذف",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) { EnsureClose(con); return; }

                new SqlCommand(
                    $"DELETE FROM PM_Terms WHERE Code={Convert.ToDouble(Code.Text)}", con)
                    .ExecuteNonQuery();

                DXMessageBox.Show("تمت حذف البند بنجاح", "حذف",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTree();
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(con); }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region RadioButton / ComboBox Events

        private void sub1_Checked(object sender, RoutedEventArgs e)
        {
            if (GBbranchAcc != null)
                GBbranchAcc.Visibility = Visibility.Visible;
            GenerateCode();
        }

        private void main_Checked(object sender, RoutedEventArgs e)
        {
            if (GBbranchAcc != null)
                GBbranchAcc.Visibility = Visibility.Collapsed;

            if (txtAprxCost != null)
            {
                txtAprxCost.Text   = "0";
                txtprice.Text      = "0";
                txtAprxPeriod.Text = "0";
                txtLastPrice.Text  = "0";
            }
        }

        private void ParentCode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateCode();
        }

        private void txtprice_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtprice.Text) && txtLastPrice != null)
                txtLastPrice.Text = txtprice.Text;
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        private void SetStatus(string msg) { }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(con);
            EnsureClose(conn);
        }

        #endregion
    }

    /// <summary>نموذج عقدة شجرة البنود</summary>
    public class TreeNodeItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string DisplayText { get; set; }
        public string Code        { get; set; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public ObservableCollection<TreeNodeItem> Children { get; set; }
            = new ObservableCollection<TreeNodeItem>();

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}