using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmMainfrmAccountSrch : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Inner Classes

        public class AccountNode
        {
            public int trCode { get; set; }
            public string trAccount { get; set; }
            public int trParentCode { get; set; }
            public string DisplayText { get; set; }

            public List<AccountNode> Children { get; set; } = new List<AccountNode>();

            public AccountNode(int code, string account, int parentCode)
            {
                trCode = code;
                trAccount = account;
                trParentCode = parentCode;
                DisplayText = $"({code}) {account}";
            }
        }

        #endregion

        #region Fields

        private SqlConnection conn;

        private string SelectedCode = "";
        private static string cond = "";

        public int Code { get; set; } = -1;
        public bool ISDone { get; set; } = false;

        private static List<AccountNode> AccountList = new List<AccountNode>();

        private List<AccountNode> _allNodes;
        private ObservableCollection<AccountNode> _treeSource;

        #endregion

        #region Constructor

        public frmMainfrmAccountSrch()
        {
            conn = MainClass.ConnObj();
            _allNodes = new List<AccountNode>();
            _treeSource = new ObservableCollection<AccountNode>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmMainfrmAccountSrch_Load(object sender, RoutedEventArgs e)
        {
            LoadTreeList();
            UpdateSearchWatermark();
        }

        private void LoadTreeList()
        {
            try
            {
                _allNodes.Clear();
                AccountList.Clear();

                string sql = "select Code,AName,ParentCode,type from Accounts_Index where type=1 " + cond;
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                Dictionary<int, AccountNode> nodeMap = new Dictionary<int, AccountNode>();

                foreach (DataRow row in dt.Rows)
                {
                    int code = Convert.ToInt32(row["Code"]);
                    string name = row["AName"]?.ToString() ?? "";
                    int parentCode = 0;

                    string parentStr = row["ParentCode"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(parentStr))
                        int.TryParse(parentStr, out parentCode);

                    AccountNode node = new AccountNode(code, name, parentCode);
                    nodeMap[code] = node;
                    _allNodes.Add(node);
                    AccountList.Add(node);
                }

                List<AccountNode> roots = new List<AccountNode>();

                foreach (AccountNode node in _allNodes)
                {
                    if (node.trParentCode == 0 || !nodeMap.ContainsKey(node.trParentCode))
                    {
                        roots.Add(node);
                    }
                    else
                    {
                        nodeMap[node.trParentCode].Children.Add(node);
                    }
                }

                _treeSource.Clear();
                foreach (AccountNode root in roots)
                    _treeSource.Add(root);

                TreeList1.ItemsSource = _treeSource;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الشجرة: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ✅ تعيين نص البحث من خارج النافذة
        /// </summary>
        public void SetSearchText(string searchText)
        {
            if (txtSearch != null && !string.IsNullOrEmpty(searchText))
            {
                txtSearch.Text = searchText;
                ApplySearchFilter(searchText);
            }
        }

        #endregion

        #region Search

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchWatermark();

            string keyword = txtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(keyword))
            {
                LoadTreeList();
                return;
            }

            ApplySearchFilter(keyword);
        }

        /// <summary>
        /// ✅ تطبيق فلتر البحث مباشرة
        /// </summary>
        private void ApplySearchFilter(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                LoadTreeList();
                return;
            }

            keyword = keyword.Trim().ToLower();

            List<AccountNode> filtered = FilterNodes(_allNodes, keyword);
            _treeSource.Clear();
            foreach (AccountNode node in filtered)
                _treeSource.Add(node);

            TreeList1.ItemsSource = _treeSource;
        }

        private List<AccountNode> FilterNodes(List<AccountNode> nodes, string keyword)
        {
            List<AccountNode> result = new List<AccountNode>();

            foreach (AccountNode node in nodes)
            {
                if (node.DisplayText?.ToLower().Contains(keyword) == true)
                    result.Add(node);

                List<AccountNode> childMatches = FilterNodes(node.Children, keyword);
                result.AddRange(childMatches);
            }

            return result;
        }

        private void UpdateSearchWatermark()
        {
            if (txtSearchWatermark == null || txtSearch == null) return;

            txtSearchWatermark.Visibility =
                string.IsNullOrWhiteSpace(txtSearch.Text) && !txtSearch.IsKeyboardFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Selection

        private void SelectCurrentNode()
        {
            try
            {
                AccountNode selected = TreeList1.SelectedItem as AccountNode;
                if (selected == null) return;

                ISDone = true;
                Code = selected.trCode;
                Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TreeList1_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentNode();
        }

        private void TreeList1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SelectCurrentNode();
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}