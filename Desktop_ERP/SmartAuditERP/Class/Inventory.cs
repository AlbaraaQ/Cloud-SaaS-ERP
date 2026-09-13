using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using AuditorAPI.Models;

namespace SmartAuditERP
{
    public class Inventory
    {
        #region Fields

        public static DataTable ItemsStock = new DataTable();

        #endregion

        #region CalcStockInOut

        public static void CalcStockInOut(string condStr, ref DataTable dtr, string date1)
        {
            using var selectConnection = MainClass.ConnObj();

            var stockTable = new DataTable();
            stockTable.Columns.Add("ItemID");
            stockTable.Columns.Add("Instock");
            stockTable.Columns.Add("OutStock");

            // تعريف جميع استعلامات المخزون مع نوعها (وارد/صادر)
            var queries = new[]
            {
                // (inv_type, proc_type, subProcType, isIn)
                // بضاعة أول مدة
                new { Sql = BuildStockSql(condStr, 9, 1, null), IsIn = true  },
                // مشتريات
                new { Sql = BuildStockSql(condStr, 1, 1, null), IsIn = true  },
                // مردود مشتريات
                new { Sql = BuildStockSql(condStr, 1, 2, null), IsIn = false },
                // مبيعات
                new { Sql = BuildStockSql(condStr, 2, 1, null), IsIn = false },
                // مردود مبيعات
                new { Sql = BuildStockSql(condStr, 2, 2, null), IsIn = true  },
                // أمر شراء
                new { Sql = BuildStockSql(condStr, 3, 1, null), IsIn = false },
                // أمر بيع
                new { Sql = BuildStockSql(condStr, 3, 2, null), IsIn = true  },
                // تحويل مستودع صادر
                new { Sql = BuildStockSql(condStr, 4, 1, null), IsIn = true  },
                // تسوية جرد
                new { Sql = BuildStockSql(condStr, 5, 1, null), IsIn = false },
                // نقل مستودع وارد
                new { Sql = BuildStockSql(condStr, 6, 1, 1),    IsIn = true  },
                // نقل مستودع صادر
                new { Sql = BuildStockSql(condStr, 6, 1, 2),    IsIn = false },
                // تحويل وارد
                new { Sql = BuildStockSql(condStr, 7, 1, 1),    IsIn = true  },
                // تحويل صادر
                new { Sql = BuildStockSql(condStr, 7, 1, 2),    IsIn = false },
                // إنتاج وارد
                new { Sql = BuildStockSql(condStr, 8, 1, null), IsIn = true  },
                // إنتاج صادر
                new { Sql = BuildStockSql(condStr, 8, 2, null), IsIn = false },
            };

            foreach (var query in queries)
            {
                var adapter = new SqlDataAdapter(query.Sql, selectConnection);

                if (!string.IsNullOrEmpty(date1) &&
                    DateTime.TryParse(date1, out DateTime parsedDate))
                {
                    adapter.SelectCommand.Parameters
                        .Add("@date1", SqlDbType.DateTime).Value = parsedDate;
                }

                var tempDt = new DataTable();
                adapter.Fill(tempDt);

                foreach (DataRow row in tempDt.Rows)
                {
                    double val = 0;
                    double.TryParse(row[1]?.ToString(), out val);

                    if (query.IsIn)
                        stockTable.Rows.Add(row[0], val, 0);
                    else
                        stockTable.Rows.Add(row[0], 0, val);
                }
            }

            // دمج صفوف نفس الصنف
            MergeStockRows(stockTable);

            // نقل النتيجة إلى dtr
            foreach (DataRow row in stockTable.Rows)
            {
                double val = 0;
                double.TryParse(row[1]?.ToString(), out val);
                dtr.Rows.Add(row[0], val);
            }
        }

        private static string BuildStockSql(string condStr, int invType,
            int procType, int? subProcType)
        {
            string subProc = subProcType.HasValue
                ? $" and inv_sub.proc_type={subProcType.Value}"
                : "";

            return $"select ItemId, sum(val) " +
                   $"from inv, inv_sub, Items, ItemsCategory " +
                   $"where {condStr} " +
                   $"inv_sub.ItemId=Items.id " +
                   $"and Items.group_id=ItemsCategory.id " +
                   $"and inv.inv_type={invType} " +
                   $"and inv.proc_type={procType} " +
                   $"{subProc} " +
                   $"and inv.InvGlobalId=inv_sub.InvGlobalId " +
                   $"and inv.IS_Deleted=0 " +
                   $"group by ItemId";
        }

        private static void MergeStockRows(DataTable table)
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = i + 1; j < table.Rows.Count; j++)
                {
                    if (table.Rows[i][0]?.ToString() ==
                        table.Rows[j][0]?.ToString())
                    {
                        double.TryParse(
                            table.Rows[j][1]?.ToString(), out double inVal);
                        double.TryParse(
                            table.Rows[i][1]?.ToString(), out double baseVal);

                        table.Rows[i][1] = baseVal + inVal;
                        table.Rows.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        #endregion

        #region CalcItemStock

        public static double CalcItemStock(int storeId, int itemID, int branchID)
        {
            using var selectConnection = MainClass.ConnObj();

            string spName = (Sync.ActiveSync && Sync.BranchType == 4)
                ? "CalcProductStocks"
                : "CalcItemStock";

            var adapter = new SqlDataAdapter(spName, selectConnection)
            {
                SelectCommand =
                {
                    CommandType = CommandType.StoredProcedure
                }
            };

            adapter.SelectCommand.Parameters
                .Add("@branch", SqlDbType.Int).Value =
                branchID == 0 ? (object)DBNull.Value : branchID;

            adapter.SelectCommand.Parameters
                .Add("@Inventory", SqlDbType.Int).Value = storeId;

            adapter.SelectCommand.Parameters
                .Add("@item", SqlDbType.Int).Value = itemID;

            adapter.SelectCommand.Parameters
                .Add("@stock", SqlDbType.Float).Direction =
                ParameterDirection.Output;

            var dt = new DataTable();
            adapter.Fill(dt);

            string stockVal = adapter.SelectCommand
                .Parameters["@stock"].Value?.ToString() ?? "0";

            return double.TryParse(stockVal, out double result) ? result : 0.0;
        }

        #endregion

        #region CalcItemsStockPOS

        public static void CalcItemsStockPOS(int storeId, int branchID)
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                const string sql =
                    "Select * FROM dbo.ItemsStockInventoryPOS(@branch, @Inventory)";

                var cmd = new SqlCommand(sql, sqlConnection);
                cmd.Parameters.Add(new SqlParameter("@branch", branchID));
                cmd.Parameters.Add(new SqlParameter("@Inventory", storeId));

                var ds = new DataSet();
                new SqlDataAdapter(cmd).Fill(ds, sql);

                ItemsStock.Clear();
                ItemsStock = ds.Tables[0].Copy();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في CalcItemsStockPOS: {ex.Message}");
            }
        }

        #endregion

        #region GetItemstocks

        public static List<ProductStock> GetItemstocks(int inventoryId)
        {
            var list = new List<ProductStock>();
            try
            {
                using var sqlConnection = MainClass.ConnObj();
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                var adapter = new SqlDataAdapter(
                    "select ProductId, UnitId, Quantity " +
                    $"from ProductStocks where InventoryId={inventoryId}",
                    sqlConnection);

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new ProductStock
                    {
                        ProductId = Convert.ToInt32(row["ProductId"]),
                        BranchId = 0,
                        InventoryId = 0,
                        UnitId = Convert.ToInt32(row["UnitId"]),
                        Quantity = Convert.ToDecimal(row["Quantity"])
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في GetItemstocks: {ex.Message}");
            }
            return list;
        }

        #endregion

        #region UpdateItemsQty

        public static void UpdateItemsQty(List<Item> items,
            int branchId, int inventoryId, int procType)
        {
            try
            {
                using var sqlConnection = MainClass.ConnObj();
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                foreach (var item in items)
                {
                    var adapter = new SqlDataAdapter(
                        $"select ProductId, BranchId, InventoryId, UnitId, Quantity " +
                        $"from ProductStocks " +
                        $"where ProductId={item.ItemNo} " +
                        $"and BranchId={branchId} " +
                        $"and InventoryId={inventoryId} " +
                        $"and UnitId={item.Unit}", sqlConnection);

                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) continue;

                    double currentQty = Convert.ToDouble(dt.Rows[0]["Quantity"]);

                    double newQty = procType switch
                    {
                        1 => currentQty - item.Quantity,
                        2 => currentQty + item.Quantity,
                        _ => currentQty
                    };

                    var cmd = new SqlCommand(
                        $"update ProductStocks set Quantity=@Quantity " +
                        $"where ProductId={item.ItemNo} " +
                        $"and BranchId={branchId} " +
                        $"and InventoryId={inventoryId} " +
                        $"and UnitId={item.Unit}", sqlConnection);

                    cmd.Parameters.Add("@Quantity", SqlDbType.Float).Value = newQty;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في UpdateItemsQty: {ex.Message}");
            }
        }

        #endregion

        #region UpdateItemStock

        public static void UpdateItemStock(object inv)
        {
            try
            {
                // استخدام dynamic بدل NewLateBinding
                dynamic invDynamic = inv;
                foreach (Item item in invDynamic.Items)
                {
                    DataRow[] rows = ItemsStock.Select(
                        $"itemId = {item.ItemNo}");

                    if (rows.Length == 0) continue;

                    double currentStock = Convert.ToDouble(rows[0]["Stock"]);
                    double storeValue = Convert.ToDouble(rows[0]["Inventory"]);

                    if (item.ProcType == 1 && item.Store == storeValue)
                        rows[0]["Stock"] = currentStock + item.PrimaryQnty;
                    else if (item.ProcType == 2 && item.Store == storeValue)
                        rows[0]["Stock"] = currentStock - item.PrimaryQnty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في UpdateItemStock: {ex.Message}");
            }
        }

        #endregion

        #region ItemsExpirationStock

        public static DataTable ItemsExpirationStock(string cond)
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                string condStr = (cond != null && cond != DBNull.Value.ToString())
                    ? cond : "";

                string sql =
                    "Select * FROM dbo.ItemsExpirationStock() " +
                    $"where ItemStock > 0 {condStr}";

                var ds = new DataSet();
                new SqlDataAdapter(new SqlCommand(sql, sqlConnection))
                    .Fill(ds, sql);

                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في ItemsExpirationStock: {ex.Message}");
                return new DataTable();
            }
        }

        #endregion

        #region TotalItemStock

        public static DataTable TotalItemStock(int storeId,
            DateTime toDate, int branchID)
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                string sql = storeId > 0
                    ? "Select * FROM dbo.TotalItemStockInventory(@branch, @date, @Inventory)"
                    : "Select * FROM dbo.TotalItemStock(@branch, @date)";

                var cmd = new SqlCommand(sql, sqlConnection);
                cmd.Parameters.Add(new SqlParameter("@branch",
                    branchID == 0 ? (object)DBNull.Value : branchID));
                cmd.Parameters.Add(new SqlParameter("@date", toDate));

                if (storeId > -1)
                    cmd.Parameters.Add(new SqlParameter("@Inventory", storeId));

                var ds = new DataSet();
                new SqlDataAdapter(cmd).Fill(ds, sql);
                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في TotalItemStock: {ex.Message}");
                return new DataTable();
            }
        }

        #endregion

        #region Inventorybalance

        public static DataTable Inventorybalance()
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                const string sql = "Select * FROM dbo.Inventorybalance()";
                var ds = new DataSet();
                new SqlDataAdapter(new SqlCommand(sql, sqlConnection))
                    .Fill(ds, sql);
                return ds.Tables[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في Inventorybalance: {ex.Message}");
                return new DataTable();
            }
        }

        #endregion

        #region InventoryCost Methods

        public static double InventoryCostByType(int branch,
            int invType, int procType)
        {
            return ExecuteStoredProcDouble("InventoryCostByType",
                cmd =>
                {
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branch;
                    cmd.Parameters.Add("@InvType", SqlDbType.Int).Value = invType;
                    cmd.Parameters.Add("@ProcType", SqlDbType.Int).Value = procType;
                    cmd.Parameters.Add("@StockCost", SqlDbType.Float)
                        .Direction = ParameterDirection.Output;
                },
                "@StockCost");
        }

        public static double InventoryCost(int branch, DateTime toDate)
        {
            return ExecuteStoredProcDouble("InventoryCost",
                cmd =>
                {
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branch;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = toDate;
                    cmd.Parameters.Add("@AvrgCost", SqlDbType.Float)
                        .Direction = ParameterDirection.Output;
                },
                "@AvrgCost");
        }

        public static double InventoryCostForBalanceSheet(int branch, DateTime toDate)
        {
            return ExecuteStoredProcDouble("InventoryCostForBalanceSheet",
                cmd =>
                {
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branch;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = toDate;
                    cmd.Parameters.Add("@AvrgCost", SqlDbType.Float)
                        .Direction = ParameterDirection.Output;
                },
                "@AvrgCost");
        }

        public static double TotalProfit(int branch, DateTime toDate)
        {
            return ExecuteStoredProcDouble("InventoryCost",
                cmd =>
                {
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = branch;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = toDate;
                    cmd.Parameters.Add("@AvrgCost", SqlDbType.Float)
                        .Direction = ParameterDirection.Output;
                },
                "@AvrgCost");
        }

        /// <summary>
        /// دالة مساعدة لتنفيذ Stored Procedure وإرجاع Output Parameter
        /// </summary>
        private static double ExecuteStoredProcDouble(string spName,
            Action<SqlCommand> addParams, string outputParam)
        {
            try
            {
                using var conn = MainClass.ConnObj();
                var adapter = new SqlDataAdapter(spName, conn)
                {
                    SelectCommand = { CommandType = CommandType.StoredProcedure }
                };
                addParams(adapter.SelectCommand);
                var dt = new DataTable();
                adapter.Fill(dt);

                string val = adapter.SelectCommand
                    .Parameters[outputParam].Value?.ToString() ?? "0";

                return double.TryParse(val, out double result) ? result : 0.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في {spName}: {ex.Message}");
                return 0.0;
            }
        }

        #endregion

        #region CalcItemsStockLimits

        public static DataTable CalcItemsStockLimits(int branchID, int invertoryId)
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                const string sql =
                    "Select * FROM dbo.[ItemStockLimits](@branch, @Inventory)";

                var cmd = new SqlCommand(sql, sqlConnection);
                cmd.Parameters.Add(new SqlParameter("@branch", branchID));
                cmd.Parameters.Add(new SqlParameter("@Inventory", invertoryId));

                var ds = new DataSet();
                new SqlDataAdapter(cmd).Fill(ds, sql);
                return ds.Tables[0].Copy();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في CalcItemsStockLimits: {ex.Message}");
                return new DataTable();
            }
        }

        #endregion

        #region ItemsStocks

        public static DataTable ItemsStocks()
        {
            using var sqlConnection = MainClass.ConnObj();
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                const string sql =
                    "SELECT * FROM [dbo].[ItemsStockByBranch]()";

                var ds = new DataSet();
                new SqlDataAdapter(new SqlCommand(sql, sqlConnection))
                    .Fill(ds, sql);

                return ds.Tables[0].Copy();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في ItemsStocks: {ex.Message}");
                return new DataTable();
            }
        }

        #endregion

        #region PostStocks

        public static async void PostStocks()
        {
            var list = new List<ProductStock>();

            try
            {
                using (var sqlConnection = MainClass.ConnObj())
                {
                    if (sqlConnection.State != ConnectionState.Open)
                        sqlConnection.Open();

                    const string sql =
                        "SELECT * FROM [dbo].[ItemsStockByBranch]()";

                    var ds = new DataSet();
                    new SqlDataAdapter(new SqlCommand(sql, sqlConnection))
                        .Fill(ds);

                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        foreach (var invertory in Common.CurrentBranch.Invertories)
                        {
                            if (Convert.ToInt32(row["InventoryId"]) ==
                                Convert.ToInt32(invertory.InvertoryId))
                            {
                                list.Add(new ProductStock
                                {
                                    ClientCode = Sync.ClientCode,
                                    ProductId = Convert.ToInt32(row["ItemId"]),
                                    BranchId = Convert.ToInt32(row["BranchId"]),
                                    InventoryId = Convert.ToInt32(row["InventoryId"]),
                                    Quantity = Convert.ToDecimal(row["Quantity"]),
                                    AvrgCost = Convert.ToDecimal(row["AvrgCost"]),
                                    UnitId = 1,
                                    LastUpdate = DateTime.Now
                                });
                            }
                        }
                    }
                }

                // تجميع المخزون بالمجموعات
                var groupedList = list
                    .GroupBy(s => new
                    {
                        s.ClientCode,
                        s.ProductId,
                        s.BranchId,
                        s.InventoryId
                    })
                    .Select(g => new ProductStock
                    {
                        ClientCode = g.Key.ClientCode,
                        ProductId = g.Key.ProductId,
                        BranchId = g.Key.BranchId,
                        InventoryId = g.Key.InventoryId,
                        UnitId = g.First().UnitId,
                        Quantity = g.Sum(x => x.Quantity),
                        AvrgCost = g.Average(x => x.AvrgCost),
                        LastUpdate = DateTime.Now
                    })
                    .ToList();

                if (groupedList.Count > 0)
                    new EntityOperations().SaveCurrentStocks(groupedList);

                if (Sync.ActiveSync && Sync.SyncType == 1)
                    await new StockCRUD(Sync.APIUrl).PostStocksOnline(groupedList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"❌ خطأ في PostStocks: {ex.Message}");
            }
        }

        #endregion
    }
}