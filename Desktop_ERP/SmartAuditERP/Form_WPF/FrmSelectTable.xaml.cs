using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using Brushes = System.Windows.Media.Brushes;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmSelectTable : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public SqlConnection conn;
        public SqlConnection conn1;
        private int _filter = 0;
        public int TableID = 0;
        private bool _Status = false;
        public string _QQ = "";
        public int Cat_ID = 1;

        public static int _CountRow = 0;
        public static decimal _Balance = 0;
        public static string _InvGlobalId = "";
        public static bool Isnew = false;
        public static bool IsDone = false;

        #endregion

        #region Constructor

        public FrmSelectTable()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadall_Dept();

            if (string.IsNullOrEmpty(_InvGlobalId))
                loadTables(_QQ);
            else
            {
                _QQ = $"and InvGlobalID=N'{_InvGlobalId}'";
                loadTables(_QQ);
                _QQ = "";
            }
        }

        #endregion

        #region Load Tables

        public void loadTables(string qq)
        {
            try
            {
                FlowLayoutPanel1.Children.Clear();
                if (conn.State != ConnectionState.Open) conn.Open();

                string sql =
                    $"SELECT t.id, t.TableName, t.Balance, t.Status, " +
                    $"c.Cat_Name " +
                    $"FROM Tables t " +
                    $"LEFT JOIN Cat_table c ON t.Cat_ID=c.Cat_ID " +
                    $"WHERE c.cat_ID IN " +
                    $"(SELECT Cat_ID FROM Cat_Table WHERE emp={MainClass.EmpNo}) " +
                    $"{qq}";

                var dr = new SqlCommand(sql, conn).ExecuteReader();

                while (dr.Read())
                {
                    int tableId = Convert.ToInt32(dr["id"]);
                    string tableName = dr["TableName"]?.ToString() ?? "";
                    string catName = dr["Cat_Name"]?.ToString() ?? "";
                    decimal balance = Convert.ToDecimal(dr["Balance"]);
                    bool isOpen = Convert.ToBoolean(dr["Status"]);

                    // MainPanel
                    var mainPanel = new Border
                    {
                        Width = 110,
                        Height = 110,
                        Margin = new Thickness(4),
                        Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x2B, 0x5B)),
                        CornerRadius = new CornerRadius(8),
                        Cursor = Cursors.Hand,
                        ToolTip = GetItemsForTable(tableId.ToString())
                                           .Any()
                                           ? string.Join("\n", GetItemsForTable(tableId.ToString()))
                                           : "لا توجد أصناف بالطاولة",
                    };
                    mainPanel.Tag = tableId.ToString();

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });

                    // اسم القسم
                    var lblCat = new TextBlock
                    {
                        Text = catName,
                        Foreground = Brushes.Black,
                        Background = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Padding = new Thickness(2),
                    };
                    Grid.SetRow(lblCat, 0);
                    grid.Children.Add(lblCat);

                    // اسم الطاولة
                    var lblName = new TextBlock
                    {
                        Text = tableName,
                        Foreground = Brushes.White,
                        Background = isOpen
                            ? new SolidColorBrush(Colors.Red)
                            : new SolidColorBrush(Color.FromRgb(0x2F, 0x2B, 0x5B)),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    };
                    Grid.SetRow(lblName, 1);
                    grid.Children.Add(lblName);

                    // الرصيد
                    var lblBalance = new TextBlock
                    {
                        Text = $"{balance} ",
                        Foreground = Brushes.Red,
                        Background = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Grid.SetRow(lblBalance, 2);
                    grid.Children.Add(lblBalance);

                    mainPanel.Child = grid;

                    // أحداث النقر
                    string capturedId = tableId.ToString();
                    mainPanel.MouseLeftButtonUp += (s, e) => Select_click(capturedId);
                    grid.MouseLeftButtonUp += (s, e) => Select_click(capturedId);

                    FlowLayoutPanel1.Children.Add(mainPanel);
                }

                dr.Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        public void loadall_Dept()
        {
            FlowLayoutPanelDept.Children.Clear();

            if (conn.State == ConnectionState.Open) conn.Close();
            conn.Open();

            var dr = new SqlCommand("SELECT * FROM Cat_Table", conn).ExecuteReader();

            while (dr.Read())
            {
                string catName = dr["Cat_Name"]?.ToString() ?? "";
                string catId = dr["Cat_ID"]?.ToString() ?? "";

                var btn = new Button
                {
                    Width = 110,
                    Height = 75,
                    Content = catName,
                    Tag = catId,
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(4),
                    Style = (Style)Resources["DeptBtn"],
                };
                btn.Click += (s, e) =>
                {
                    if (int.TryParse(btn.Tag?.ToString(), out int filterId))
                        filter_click(filterId);
                };

                FlowLayoutPanelDept.Children.Add(btn);
            }

            dr.Close();
            conn.Close();
        }

        #endregion

        #region Select / Filter

        public void Select_click(string tableIdStr)
        {
            if (!int.TryParse(tableIdStr, out int tId)) return;
            TableID = tId;

            if (conn.State == ConnectionState.Open) conn.Close();

            var adapter = new SqlDataAdapter(
                $"SELECT * FROM Tables WHERE id={TableID}", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                var row = dt.AsEnumerable().First();
                string invGid = row["InvGlobalId"]?.ToString() ?? "";
                bool status = Convert.ToBoolean(row["Status"]);
                decimal bal = Convert.ToDecimal(row["Balance"]);

                if (!string.IsNullOrWhiteSpace(invGid) && _CountRow > 0)
                {
                    Common._StateTable = status;
                    Common._TableInvglobalid = invGid;
                    Common._BalanceTable = bal;
                    Common._TableNoLocal = TableID.ToString();
                    Common._ISNewTable = false;
                    Common._ISShow = false;
                }
                else if (!string.IsNullOrWhiteSpace(invGid) && _CountRow <= 0)
                {
                    Common._StateTable = status;
                    Common._TableInvglobalid = invGid;
                    Common._BalanceTable = bal;
                    Common._TableNoLocal = TableID.ToString();
                    Common._ISNewTable = false;
                    Common._ISShow = true;
                }
                else
                {
                    Common._StateTable = _Status;
                    Common._TableInvglobalid = _InvGlobalId;
                    Common._BalanceTable = _Balance;
                    Common._TableNoLocal = TableID.ToString();
                    Common._ISNewTable = true;
                    Common._ISShow = false;
                }
            }

            IsDone = true;
            _InvGlobalId = "";
            Close();
        }

        public void filter_click(int catId)
        {
            _filter = catId;
            FlowLayoutPanel1.Children.Clear();

            string qq = string.IsNullOrEmpty(_InvGlobalId)
                ? ""
                : $"WHERE InvGlobalID=N'{_InvGlobalId}'";

            loadTables_By_Dept(qq, _filter);
            _QQ = "";
        }

        public void loadTables_By_Dept(string qq, int catId)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                string sql =
                    $"SELECT id, TableName, Balance, Status FROM Tables " +
                    $"WHERE Cat_ID={catId} AND Cat_ID IN " +
                    $"(SELECT Cat_ID FROM Cat_Table WHERE emp={MainClass.EmpNo}) ";

                var dr = new SqlCommand(sql, conn).ExecuteReader();

                while (dr.Read())
                {
                    int tblId = Convert.ToInt32(dr["id"]);
                    string tblName = dr["TableName"]?.ToString() ?? "";
                    decimal balance = Convert.ToDecimal(dr["Balance"]);
                    bool isOpen = Convert.ToBoolean(dr["Status"]);

                    var mainPanel = new Border
                    {
                        Width = 110,
                        Height = 110,
                        Margin = new Thickness(4),
                        Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x2B, 0x5B)),
                        CornerRadius = new CornerRadius(8),
                        Cursor = Cursors.Hand,
                    };
                    mainPanel.Tag = tblId.ToString();

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

                    var lblName = new TextBlock
                    {
                        Text = tblName,
                        Foreground = Brushes.White,
                        Background = isOpen
                            ? new SolidColorBrush(Colors.Red)
                            : Brushes.Transparent,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Grid.SetRow(lblName, 0);

                    var lblBalance = new TextBlock
                    {
                        Text = $"{balance} ",
                        Foreground = Brushes.Red,
                        Background = Brushes.White,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    Grid.SetRow(lblBalance, 1);

                    grid.Children.Add(lblName);
                    grid.Children.Add(lblBalance);
                    mainPanel.Child = grid;

                    string capturedId = tblId.ToString();
                    mainPanel.MouseLeftButtonUp += (s, e) => Select_click(capturedId);

                    FlowLayoutPanel1.Children.Add(mainPanel);
                }

                dr.Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        private List<string> GetItemsForTable(string tableId)
        {
            var list = new List<string>();
            try
            {
                using var localConn = new SqlConnection(MainClass.connstr);
                string sql =
                    "SELECT name FROM Items WHERE id IN " +
                    "(SELECT itemid FROM inv_Sub WHERE invglobalid IN " +
                    $"(SELECT InvGlobalID FROM Tables WHERE id={tableId}))";

                var cmd = new SqlCommand(sql, localConn);
                localConn.Open();
                var dr = cmd.ExecuteReader();
                while (dr.Read()) list.Add(dr["name"]?.ToString() ?? "");
                dr.Close();
            }
            catch { /* تجاهل */ }
            return list;
        }

        #endregion

        #region Button Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            IsDone = false;
            Close();
        }

        private void btnAddGrp_Click(object sender, RoutedEventArgs e)
        {
            var FrmTables = new FrmTables();
            FrmTables.Show();
        }

        #endregion
    }
}