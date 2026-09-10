using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using ETA_Invoice.Models;
using SmartAuditERP.Form_WPF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ws_Auditor;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP
{
    public class ItemOpercontract
    {
        #region Private Fields

        private int ID;
        private string Itename;
        private string CateName;
        private string Barcode;
        private string Unit;
        private decimal DefPrice;
        private int CategoryID;
        private int UOMID;
        private string PrBarcode;
        private static double AvrgCost;
        private static double ValidStock;

        #endregion

        #region Helper

        private static string GetErrorMessage(string arabicMsg, string englishMsg, string details)
        {
            if (string.Equals(MainClass.Language, "ar", StringComparison.Ordinal))
                return $"{arabicMsg}{Environment.NewLine}تفاصيل الخطأ: {details}";

            return $"{englishMsg}{Environment.NewLine}Error details: {details}";
        }

        private static double SafeToDouble(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0.0;

            return double.TryParse(value.ToString(), out double result) ? result : 0.0;
        }

        private static int SafeToInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        private static bool SafeToBool(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;

            return value.ToString() == "1" ||
                   string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value.ToString(), "yes", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Properties

        public string ItemName
        {
            get => Itename;
            set => Itename = value;
        }

        public string CategoryName
        {
            get => CateName;
            set => CateName = value;
        }

        public string ItemID
        {
            get => ID.ToString();
            set => int.TryParse(value, out ID);
        }

        public string UnitName
        {
            get => Unit;
            set => Unit = value;
        }

        public string ItemBarcode
        {
            get => Barcode;
            set => Barcode = value;
        }

        public string DefaultPrice
        {
            get => DefPrice.ToString();
            set => decimal.TryParse(value, out DefPrice);
        }

        public string Category_ID
        {
            get => CategoryID.ToString();
            set => int.TryParse(value, out CategoryID);
        }

        public string UOM_ID
        {
            get => UOMID.ToString();
            set => int.TryParse(value, out UOMID);
        }

        public string BarcodePrice
        {
            get => PrBarcode;
            set => PrBarcode = value;
        }

        #endregion

        #region Cost Methods

        public static double Cost(int ItemId)
        {
            if (Common.CostType == 1)
                return AvgCost(ItemId, MainClass.BranchNo);

            if (Common.CostType == 2)
                return SafeToDouble(PurchPrice(ItemId));

            if (Common.CostType == 3)
                return RecentPurchPrice(ItemId);

            return 0.0;
        }

        public static double AvgCost(int ItemId, int BranchID, int StoreID = -1)
        {
            try
            {
                using SqlDataAdapter adapter = new SqlDataAdapter("ItemAvrgCost", MainClass.ConnObj());
                DataTable table = new DataTable();
                adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                adapter.SelectCommand.Parameters.Add("@branch", SqlDbType.Int).Value = BranchID;
                adapter.SelectCommand.Parameters.Add("@item", SqlDbType.Int).Value = ItemId;
                adapter.SelectCommand.Parameters.Add("@store", SqlDbType.Int).Value =
                    (StoreID == -1) ? (object)DBNull.Value : StoreID;
                adapter.SelectCommand.Parameters.Add("@ItemAvrgCost", SqlDbType.Float).Direction =
                    ParameterDirection.Output;

                adapter.Fill(table);

                double cost = SafeToDouble(adapter.SelectCommand.Parameters["@ItemAvrgCost"].Value);
                return cost < 0.0 ? 0.0 : cost;
            }
            catch
            {
                return 0.0;
            }
        }

        public static double RecentPurchPrice(int ItemId)
        {
            SqlConnection conn = MainClass.ConnObj();
            string branchFilter = MainClass.BranchNo != -1
                ? $" branch={MainClass.BranchNo} and "
                : string.Empty;

            string query = $"SELECT val, val1, exchange_price FROM inv, inv_sub " +
                           $"WHERE {branchFilter} Inv_Sub.ItemId={ItemId} " +
                           $"AND inv.proc_type=1 AND inv.inv_type=1 " +
                           $"AND Inv_Sub.exchange_price <> 0 AND inv_sub.proc_type=1 " +
                           $"AND inv.InvGlobalID=inv_sub.InvGlobalID AND IS_Deleted=0 " +
                           $"ORDER BY inv.id DESC";

            DataTable dt1 = new DataTable();
            new SqlDataAdapter(query, conn).Fill(dt1);

            if (dt1.Rows.Count > 0)
            {
                return Math.Round(
                    SafeToDouble(dt1.Rows[0]["val1"]) *
                    SafeToDouble(dt1.Rows[0]["exchange_price"]) /
                    SafeToDouble(dt1.Rows[0]["val"]), 2);
            }

            DataTable dt2 = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT purch_price FROM Items WHERE id = @id AND IS_Deleted = 0", conn))
            {
                adapter.SelectCommand.Parameters.AddWithValue("@id", ItemId);
                adapter.Fill(dt2);
            }

            return dt2.Rows.Count > 0
                ? Convert.ToDouble(Convert.ToDecimal(dt2.Rows[0]["purch_price"]))
                : 0.0;
        }

        public static object PurchPrice(int ItemId)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT purch_price FROM Items WHERE id={ItemId}",
                MainClass.ConnObj()).Fill(dt);

            return dt.Rows.Count > 0 ? dt.Rows[0]["purch_price"] : (object)0;
        }

        #endregion

        #region Item Info Methods

        public static void ItemProperty(int ItemId, ref int ItemPrty, ref double FillValue)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT ItemProperty, FillValue FROM Items WHERE items.id={ItemId}",
                MainClass.ConnObj()).Fill(dt);

            if (dt.Rows.Count > 0)
            {
                ItemPrty = SafeToInt(dt.Rows[0]["ItemProperty"]);
                FillValue = SafeToDouble(dt.Rows[0]["FillValue"]);
            }
            else
            {
                ItemPrty = 0;
                FillValue = 0.0;
            }
        }

        public static void CheckForValidStock(int ItemId, ref double ValidQnty, ref double Cost)
        {
            Cost = AvgCost(ItemId, MainClass.BranchNo);
        }

        public static void RecalculateCost(int ItemId, double NewQnty, ref double AvgCost, string Cond)
        {
            SqlConnection conn = MainClass.ConnObj();

            string query = $"SELECT CurrentQnty, Inv_Sub.InvGlobalID, Inv_Sub.AvrgCost AS AvrgCost, " +
                           $"ItemId AS ItemID, inv.minus AS Invdiscount " +
                           $"FROM inv, Inv_Sub " +
                           $"WHERE inv.inv_type>=2 AND inv.inv_type<=3 AND inv.proc_type=1 " +
                           $"AND inv.IS_Deleted=0 AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                           $"AND Inv_Sub.ItemId={ItemId} AND CurrentQnty<0 {Cond} " +
                           $"ORDER BY inv.date ASC";

            DataTable dt = new DataTable();
            new SqlDataAdapter(query, conn).Fill(dt);

            if (dt.Rows.Count == 0)
                return;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!(NewQnty > 0.0))
                    break;

                double newQtyAfter = NewQnty + SafeToDouble(dt.Rows[i]["CurrentQnty"]);

                using SqlCommand cmd = new SqlCommand(StoredQueries.UpdateItemCost, conn);
                cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = dt.Rows[i]["InvGlobalID"];
                cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = dt.Rows[i]["ItemID"];
                cmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = AvgCost;
                cmd.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = newQtyAfter;
                cmd.ExecuteNonQuery();

                NewQnty = newQtyAfter;

                string subQuery = $"SELECT CurrentQnty, Inv_Sub.val1 * Inv_Sub.exchange_price AS Sum, " +
                                  $"ISNULL(Inv_Sub.discount, 0) AS ItDiscount, Inv_Sub.taxval AS Vat, " +
                                  $"Inv_Sub.val * Inv_Sub.AvrgCost AS AvrgCost " +
                                  $"FROM Inv_Sub WHERE Inv_Sub.InvGlobalID = N'{dt.Rows[i]["InvGlobalID"]}'";

                DataTable subDt = new DataTable();
                new SqlDataAdapter(subQuery, conn).Fill(subDt);

                double sumVal = 0.0, avgCostVal = 0.0, discountVal = 0.0;
                bool isValid = true;

                foreach (DataRow subRow in subDt.Rows)
                {
                    if (SafeToDouble(subRow["CurrentQnty"]) < 0.0)
                        isValid = false;

                    sumVal += SafeToDouble(subRow["Sum"]);
                    avgCostVal += SafeToDouble(subRow["AvrgCost"]);
                    discountVal += SafeToDouble(subRow["ItDiscount"]);
                }

                discountVal += SafeToDouble(dt.Rows[i]["Invdiscount"]);
                double profit = sumVal - discountVal - avgCostVal;

                if (isValid)
                {
                    using SqlCommand profitCmd = new SqlCommand(StoredQueries.UpdateInvProfit, conn);
                    profitCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = dt.Rows[i]["InvGlobalID"];
                    profitCmd.Parameters.Add("@InvProfit", SqlDbType.Int).Value = profit;
                    profitCmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetGroupCode(int id)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT code FROM ItemsCategory WHERE id={id}",
                MainClass.ConnObj()).Fill(dt);

            return dt.Rows.Count > 0 ? dt.Rows[0][0]?.ToString() ?? string.Empty : string.Empty;
        }

        public static string GetItemName(int id)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT name FROM Items WHERE id={id}",
                MainClass.ConnObj()).Fill(dt);

            return dt.Rows.Count > 0 ? dt.Rows[0][0]?.ToString() ?? string.Empty : string.Empty;
        }

        public static string GetItemCode(int id)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT ISNULL(Code,'') FROM Items WHERE id={id}",
                MainClass.ConnObj()).Fill(dt);

            return dt.Rows.Count > 0 ? dt.Rows[0][0]?.ToString() ?? string.Empty : string.Empty;
        }

        #endregion

        #region Online Sync

        public async void ReadProductsOnline()
        {
            ProductCRUD crud = new ProductCRUD(Sync.APIUrl);
            List<Product> list = (List<Product>)await crud.GetProducts(
                Sync.ClientCode, Sync.BranchId, Sync.BranchType, DateTime.MinValue);

            MessageBox.Show($"عدد الأصناف المستوردة {list.Count}");
            new EntityOperations().SaveProducts(list);
        }

        public async Task<bool> ReadUnitsOnline()
        {
            List<Unit> list = (Sync.BranchType != 4)
                ? (List<Unit>)await new UnitCRUD(Sync.APIUrl).GetUnits(
                    Sync.ClientCode, Sync.BranchId, Sync.BranchType)
                : new ClsUnit(Sync.APIUrl, "").GetUnits();

            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                foreach (Unit item in list)
                {
                    using SqlCommand checkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM units WHERE id={item.UnitId}",
                        conn, transaction);

                    bool exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                    if (!exists)
                    {
                        using SqlCommand insertCmd = new SqlCommand(
                            $"INSERT INTO units (id, name, defaultInv, IS_Deleted, UnitCode, UnitId) " +
                            $"VALUES ({item.UnitId}, N'{item.Name}', {item.DefaultInv}, 0, N'', {item.UnitId})",
                            conn, transaction);
                        insertCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using SqlCommand updateCmd = new SqlCommand(
                            $"UPDATE units SET name=N'{item.Name}', defaultInv={item.DefaultInv} " +
                            $"WHERE id={item.UnitId}",
                            conn, transaction);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show(
                    GetErrorMessage("خطأ أثناء استيراد الوحدات", "error in saving", ex.Message),
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public async void ReadCategoriesOnline()
        {
            CategoryCRUD crud = new CategoryCRUD(Sync.APIUrl);
            List<Category> list = (List<Category>)await crud.GetCategories(
                Sync.ClientCode, Sync.BranchId, Sync.BranchType);

            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            try
            {
                foreach (Category item in list)
                {
                    item.ParentCode = item.ParentCode ?? string.Empty;
                    item.Printer = item.Printer ?? string.Empty;

                    using SqlCommand checkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM itemsCategory WHERE id={item.CategoryId}", conn);
                    bool exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                    int typeVal = item.ISLeaf ? 2 : 1;

                    if (!exists)
                    {
                        using SqlCommand insertCmd = new SqlCommand(
                            $"INSERT INTO itemsCategory " +
                            $"(id,CategoryId,name,nameEN,printer,ShowInPOS,DispalyOrder,IS_Deleted,code,parentCode,type,BranchId,AllBranch) " +
                            $"VALUES(@id,@CategoryId,@name,@nameEN,'{item.Printer}'," +
                            $"{Convert.ToInt16(item.ShowInPOS)},{item.DispalyOrder},0,@code,@parentCode,@type,-1,1)",
                            conn);

                        insertCmd.Parameters.Add("@id", SqlDbType.Int).Value = item.CategoryId;
                        insertCmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = item.CategoryId;
                        insertCmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = item.Name;
                        insertCmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = item.NameEN;
                        insertCmd.Parameters.Add("@code", SqlDbType.NVarChar).Value = item.Code;
                        insertCmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar).Value = item.ParentCode;
                        insertCmd.Parameters.Add("@type", SqlDbType.Int).Value = typeVal;
                        insertCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using SqlCommand updateCmd = new SqlCommand(
                            $"UPDATE itemsCategory SET name=@name, nameEN=@nameEN, " +
                            $"printer='{item.Printer}', ShowInPOS={Convert.ToInt16(item.ShowInPOS)}, " +
                            $"DispalyOrder={item.DispalyOrder}, IS_Deleted=0, code=@code, " +
                            $"ParentCode=@ParentCode, type=@type, BranchId=-1, AllBranch=1 " +
                            $"WHERE id={item.CategoryId}",
                            conn);

                        updateCmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = item.Name;
                        updateCmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = item.NameEN;
                        updateCmd.Parameters.Add("@code", SqlDbType.NVarChar).Value = item.Code;
                        updateCmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar).Value = item.ParentCode;
                        updateCmd.Parameters.Add("@type", SqlDbType.Int).Value = typeVal;
                        updateCmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم تحديث بيانات المجموعات بنجاح");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetErrorMessage("خطأ أثناء استيراد المجموعات", "error in saving", ex.Message),
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Calculation Methods

        public static double ItemAdditonalCos(double InvTotal, double InvAdditonalCost,
            double ItemTotal, double ItemPrimaryQnty)
        {
            double result = InvAdditonalCost * (ItemTotal / InvTotal) / ItemPrimaryQnty;
            return (double.IsInfinity(result) || double.IsNaN(result)) ? 0.0 : result;
        }

        public static double ItemDiscountValue(decimal InvSumPrice, decimal InvDiscount,
            double ItemTotal, decimal ItemQnty, decimal ItemDiscount)
        {
            try
            {
                double result = (ItemTotal - Convert.ToDouble(ItemDiscount) -
                                (ItemTotal - Convert.ToDouble(ItemDiscount)) /
                                Convert.ToDouble(InvSumPrice) * Convert.ToDouble(InvDiscount)) /
                                Convert.ToDouble(ItemQnty);

                return (double.IsInfinity(result) || double.IsNaN(result)) ? 0.0 : result;
            }
            catch
            {
                return 0.0;
            }
        }

        public static double ItemDiscountRateFromInvDiscount(decimal InvSumPrice, decimal InvDiscount,
            double ItemTotal, double PriceIncVat, double VATPerc)
        {
            try
            {
                double result = ItemTotal * Convert.ToDouble(InvDiscount) / Convert.ToDouble(InvSumPrice);
                return (double.IsInfinity(result) || double.IsNaN(result)) ? 0.0 : result;
            }
            catch
            {
                return 0.0;
            }
        }

        public static void CalcRows(ref InvoiceDGV Invo)
        {
            try
            {
                Invo.Total = 0.0;
                Invo.SumPrice = 0.0;
                Invo.TotDiscount = 0.0;
                Invo.ItemsDiscount = 0.0;
                Invo.VAT = 0.0;
                Invo.ExtraVAT = 0.0;
                Invo.Net = 0.0;
                Invo.SumCost = 0.0;
                Invo.TotalQty = 0m;
                Invo.TotalWithholdingTax = 0.0;

                for (int i = 0; i < Invo.InvoiceItems.Count; i++)
                {
                    InvoiceItem NewItem = Invo.InvoiceItems[i];
                    double vatAmount = 0.0;
                    double extraVat = 0.0;

                    if (NewItem.ItemId > 0)
                    {
                        vatAmount = 0.0;
                        extraVat = 0.0;

                        NewItem.ItemPriceWithoutVAT = Invo.PriceIncVAT
                            ? NewItem.ItemPrice / (1.0 + NewItem.ItemVatPerc / 100.0)
                            : NewItem.ItemPrice;

                        NewItem.ItemPrimaryQnty = NewItem.ItemQuantity * NewItem.UnitEquality;
                        NewItem.ItemSumPrice = NewItem.ItemQuantity * NewItem.ItemPrice;

                        CheckForItemOffer(Invo, NewItem);
                        CheckForCategoryOffer(Invo, ref NewItem);

                        NewItem.ItemTotalPrice = NewItem.ItemSumPrice - NewItem.ItemDiscount;
                        NewItem.ItemPriceAfterDiscount = NewItem.ItemTotalPrice / NewItem.ItemQuantity;

                        double itemTotal = NewItem.ItemTotalPrice;

                        bool hasVat = itemTotal > 0.0 &&
                                      (NewItem.ItemVatPerc > 0.0 ||
                                       (long)Math.Round(Invo.ExtraVATPerc) != 0);

                        if (hasVat)
                        {
                            if (Invo.PriceIncVAT)
                            {
                                vatAmount = itemTotal - itemTotal / (1.0 + NewItem.ItemVatPerc / 100.0);
                                itemTotal -= vatAmount;

                                if (NewItem.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                                {
                                    extraVat = NewItem.ItemPriceWithoutVAT < 25.0
                                        ? 25.0 * NewItem.ItemQuantity
                                        : itemTotal * (Invo.ExtraVATPerc / 100.0);

                                    vatAmount += extraVat * (NewItem.ItemVatPerc / 100.0);
                                }

                                NewItem.ItemTotalPrice = itemTotal;
                            }
                            else
                            {
                                if (NewItem.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                                {
                                    extraVat = NewItem.ItemPriceWithoutVAT < 25.0
                                        ? 25.0 * NewItem.ItemQuantity
                                        : itemTotal * (Invo.ExtraVATPerc / 100.0);
                                }

                                vatAmount = (itemTotal + extraVat) * (NewItem.ItemVatPerc / 100.0);
                            }
                        }

                        if (Invo.InvoiceType == InvoiceType.BeginingInventory)
                            vatAmount = 0.0;

                        if (Invo.VATperc == 0.0)
                            vatAmount = 0.0;

                        if (EtaSetting.Active)
                            NewItem.WithholdingTax = NewItem.ItemTotalPrice * (NewItem.WithholdingTaxPerc / 100.0);

                        NewItem.ItemVat = vatAmount;
                        NewItem.ItemNetPrice = EtaSetting.Active
                            ? itemTotal + vatAmount + extraVat - NewItem.WithholdingTax
                            : itemTotal + vatAmount + extraVat;
                    }

                    NewItem.ItemAdditionalTaxPerc = NewItem.isExtraTaxApplied ? Invo.ExtraVATPerc : 0.0;
                    NewItem.ItemAdditionalTax = NewItem.isExtraTaxApplied ? extraVat : 0.0;

                    Invo.SumPrice += NewItem.ItemSumPrice;
                    Invo.Net += NewItem.ItemNetPrice;
                    Invo.ExtraVAT += extraVat;
                    Invo.VAT += vatAmount;
                    Invo.Total += NewItem.ItemTotalPrice;
                    Invo.SumCost += NewItem.ItemCost * NewItem.ItemPrimaryQnty;
                    Invo.InvProfit += Invo.Total - Invo.SumCost;
                    Invo.ItemsDiscount += NewItem.ItemDiscount;
                    Invo.TotalQty = new decimal(Convert.ToDouble(Invo.TotalQty) + NewItem.ItemPrimaryQnty);
                    Invo.TotalWithholdingTax += NewItem.WithholdingTax;
                    Invo.Additions += NewItem.Additionsitem;
                }

                Invo.IsUpdated = true;
                Invo.TotDiscount = Invo.ItemsDiscount;

                if ((Invo.InvoiceType == InvoiceType.Sale ||
                     Invo.InvoiceType == InvoiceType.POS) &&
                    Invo.ProcType == 1 && Invo.PayType != 5)
                {
                    InvoiceOper.CheckForOffer(ref Invo);
                }

                if (Invo.InvDiscount > 0.0)
                    CalcTotal(ref Invo);

                if (Invo.Additions > 0.0)
                {
                    if (!Invo.PriceIncVAT)
                    {
                        double addVat = Invo.Additions * (Invo.VATperc / 100.0);
                        Invo.Net += addVat;
                    }
                }
            }
            catch { /* keep silent like original */ }
        }

        public static void CalcRowscontratct(ref Invoicecontract Invo)
        {
            try
            {
                Invo.Total = 0.0;
                Invo.SumPrice = 0.0;
                Invo.TotDiscount = 0.0;
                Invo.ItemsDiscount = 0.0;
                Invo.VAT = 0.0;
                Invo.ExtraVAT = 0.0;
                Invo.Net = 0.0;
                Invo.SumCost = 0.0;
                Invo.TotalQty = 0m;
                Invo.TotalWithholdingTax = 0.0;

                for (int i = 0; i < Invo.InvoiceItems.Count; i++)
                {
                    InvoiceItem NewItem = Invo.InvoiceItems[i];
                    double vatAmount = 0.0;
                    double extraVat = 0.0;

                    if (NewItem.ItemId > 0)
                    {
                        vatAmount = 0.0;
                        extraVat = 0.0;

                        NewItem.ItemPriceWithoutVAT = Invo.PriceIncVAT
                            ? NewItem.ItemPrice / (1.0 + NewItem.ItemVatPerc / 100.0)
                            : NewItem.ItemPrice;

                        NewItem.ItemPrimaryQnty = NewItem.ItemQuantity * NewItem.UnitEquality;
                        NewItem.ItemSumPrice = NewItem.ItemQuantity * NewItem.ItemPrice;

                        CheckForItemOffercontract(Invo, NewItem);
                        CheckForCategoryOffercontrat(Invo, ref NewItem);

                        NewItem.ItemTotalPrice = NewItem.ItemSumPrice - NewItem.ItemDiscount;
                        NewItem.ItemPriceAfterDiscount = NewItem.ItemTotalPrice / NewItem.ItemQuantity;

                        double itemTotal = NewItem.ItemTotalPrice;
                        bool hasVat = itemTotal > 0.0 &&
                                      (NewItem.ItemVatPerc > 0.0 ||
                                       (long)Math.Round(Invo.ExtraVATPerc) != 0);

                        if (hasVat)
                        {
                            if (Invo.PriceIncVAT)
                            {
                                vatAmount = itemTotal - itemTotal / (1.0 + NewItem.ItemVatPerc / 100.0);
                                itemTotal -= vatAmount;

                                if (NewItem.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                                {
                                    extraVat = NewItem.ItemPriceWithoutVAT < 25.0
                                        ? 25.0 * NewItem.ItemQuantity
                                        : itemTotal * (Invo.ExtraVATPerc / 100.0);

                                    vatAmount += extraVat * (NewItem.ItemVatPerc / 100.0);
                                }

                                NewItem.ItemTotalPrice = itemTotal;
                            }
                            else
                            {
                                if (NewItem.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                                {
                                    extraVat = NewItem.ItemPriceWithoutVAT < 25.0
                                        ? 25.0 * NewItem.ItemQuantity
                                        : itemTotal * (Invo.ExtraVATPerc / 100.0);
                                }

                                vatAmount = (itemTotal + extraVat) * (NewItem.ItemVatPerc / 100.0);
                            }
                        }

                        if (Invo.InvoiceType == InvoiceType.BeginingInventory)
                            vatAmount = 0.0;

                        if (Invo.VATperc == 0.0)
                            vatAmount = 0.0;

                        if (EtaSetting.Active)
                            NewItem.WithholdingTax = NewItem.ItemTotalPrice * (NewItem.WithholdingTaxPerc / 100.0);

                        NewItem.ItemVat = vatAmount;
                        NewItem.ItemNetPrice = EtaSetting.Active
                            ? itemTotal + vatAmount + extraVat - NewItem.WithholdingTax
                            : itemTotal + vatAmount + extraVat;
                    }

                    NewItem.ItemAdditionalTaxPerc = NewItem.isExtraTaxApplied ? Invo.ExtraVATPerc : 0.0;
                    NewItem.ItemAdditionalTax = NewItem.isExtraTaxApplied ? extraVat : 0.0;

                    Invo.SumPrice += NewItem.ItemSumPrice;
                    Invo.Net += NewItem.ItemNetPrice;
                    Invo.ExtraVAT += extraVat;
                    Invo.VAT += vatAmount;
                    Invo.Total += NewItem.ItemTotalPrice;
                    Invo.SumCost += NewItem.ItemCost * NewItem.ItemPrimaryQnty;
                    Invo.InvProfit += Invo.Total - Invo.SumCost;
                    Invo.ItemsDiscount += NewItem.ItemDiscount;
                    Invo.TotalQty = new decimal(Convert.ToDouble(Invo.TotalQty) + NewItem.ItemPrimaryQnty);
                    Invo.TotalWithholdingTax += NewItem.WithholdingTax;
                    Invo.Additions += NewItem.Additionsitem;
                }

                if (Invo.InvDiscount > 0.0)
                    CalcTotalcontract(ref Invo);

                if (Invo.Additions > 0.0 && !Invo.PriceIncVAT)
                {
                    double addVat = Invo.Additions * (Invo.VATperc / 100.0);
                    Invo.Net += addVat;
                }

                Invo.original_work_amount = Invo.SumPrice;
                Invo.special_discount = Invo.InvDiscount;
                Invo.amount_after_discount = Invo.Total;
                Invo.vat_amount = Invo.VAT;
                Invo.total_with_vat = Invo.Net;
                Invo.contract_total_value = 0.0;
                Invo.work_guarantee = 0.0;
                Invo.net_due_this_payment = Invo.total_with_vat;
                Invo.previously_paid_amount = 0.0;
                Invo.remaining_contract_balance = 0.0;
                Invo.IsUpdated = true;
                Invo.TotDiscount = Invo.ItemsDiscount;
            }
            catch { /* keep silent like original */ }
        }

        public static void CalcTotal(ref InvoiceDGV Invo)
        {
            try
            {
                double savedSumPrice = Invo.SumPrice;
                Invo.SumPrice = 0.0;
                Invo.Net = 0.0;
                Invo.ExtraVAT = 0.0;
                Invo.VAT = 0.0;
                Invo.Total = 0.0;

                foreach (InvoiceItem item in Invo.InvoiceItems)
                {
                    double vatAmount = 0.0;
                    double extraVat = 0.0;
                    double itemTotal;

                    if (Invo.InvDiscount == savedSumPrice)
                    {
                        item.ItemDiscount = 0.0;
                        itemTotal = 0.0;
                    }
                    else
                    {
                        double invDiscountPortion = Invo.InvDiscount * item.ItemSumPrice / savedSumPrice;
                        itemTotal = item.ItemSumPrice - item.ItemDiscount - invDiscountPortion;
                    }

                    double totalForVat = itemTotal;
                    bool hasVat = totalForVat > 0.0 &&
                                  (item.ItemVatPerc > 0.0 ||
                                   (long)Math.Round(Invo.ExtraVATPerc) != 0);

                    if (hasVat)
                    {
                        if (Invo.PriceIncVAT)
                        {
                            vatAmount = totalForVat - totalForVat / (1.0 + item.ItemVatPerc / 100.0);
                            totalForVat -= vatAmount;

                            if (item.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                            {
                                extraVat = item.ItemPriceWithoutVAT < 25.0
                                    ? 25.0 * item.ItemQuantity
                                    : totalForVat * (Invo.ExtraVATPerc / 100.0);

                                vatAmount += extraVat * (item.ItemVatPerc / 100.0);
                            }

                            itemTotal = totalForVat;
                        }
                        else
                        {
                            if (item.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                            {
                                extraVat = item.ItemPriceWithoutVAT < 25.0
                                    ? 25.0 * item.ItemQuantity
                                    : totalForVat * (Invo.ExtraVATPerc / 100.0);
                            }

                            vatAmount = (totalForVat + extraVat) * (item.ItemVatPerc / 100.0);
                        }
                    }

                    if (Invo.InvoiceType == InvoiceType.BeginingInventory)
                        vatAmount = 0.0;

                    if (Invo.VATperc == 0.0)
                        vatAmount = 0.0;

                    item.ItemNetPrice = EtaSetting.Active
                        ? totalForVat + vatAmount + extraVat - item.WithholdingTax
                        : totalForVat + vatAmount + extraVat;

                    item.ItemAdditionalTaxPerc = item.isExtraTaxApplied ? Invo.ExtraVATPerc : 0.0;
                    item.ItemAdditionalTax = item.isExtraTaxApplied ? extraVat : 0.0;

                    Invo.SumPrice += item.ItemSumPrice;
                    Invo.Net += item.ItemNetPrice;
                    Invo.ExtraVAT += extraVat;
                    Invo.VAT += vatAmount;
                    Invo.Total += itemTotal;
                }

                Invo.Net = Invo.Total + Invo.VAT + Invo.ExtraVAT;
                Invo.TotDiscount += Invo.InvDiscount;
            }
            catch { /* keep silent like original */ }
        }

        public static void CalcTotalcontract(ref Invoicecontract Invo)
        {
            try
            {
                double savedSumPrice = Invo.SumPrice;
                Invo.SumPrice = 0.0;
                Invo.Net = 0.0;
                Invo.ExtraVAT = 0.0;
                Invo.VAT = 0.0;
                Invo.Total = 0.0;

                foreach (InvoiceItem item in Invo.InvoiceItems)
                {
                    double vatAmount = 0.0;
                    double extraVat = 0.0;
                    double itemTotal;

                    if (Invo.InvDiscount == savedSumPrice)
                    {
                        item.ItemDiscount = 0.0;
                        itemTotal = 0.0;
                    }
                    else
                    {
                        double invDiscountPortion = Invo.InvDiscount * item.ItemSumPrice / savedSumPrice;
                        itemTotal = item.ItemSumPrice - item.ItemDiscount - invDiscountPortion;
                    }

                    double totalForVat = itemTotal;
                    bool hasVat = totalForVat > 0.0 &&
                                  (item.ItemVatPerc > 0.0 ||
                                   (long)Math.Round(Invo.ExtraVATPerc) != 0);

                    if (hasVat)
                    {
                        if (Invo.PriceIncVAT)
                        {
                            vatAmount = totalForVat - totalForVat / (1.0 + item.ItemVatPerc / 100.0);
                            totalForVat -= vatAmount;

                            if (item.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                            {
                                extraVat = item.ItemPriceWithoutVAT < 25.0
                                    ? 25.0 * item.ItemQuantity
                                    : totalForVat * (Invo.ExtraVATPerc / 100.0);

                                vatAmount += extraVat * (item.ItemVatPerc / 100.0);
                            }

                            itemTotal = totalForVat;
                        }
                        else
                        {
                            if (item.isExtraTaxApplied && Invo.ExtraVATPerc > 0.0)
                            {
                                extraVat = item.ItemPriceWithoutVAT < 25.0
                                    ? 25.0 * item.ItemQuantity
                                    : totalForVat * (Invo.ExtraVATPerc / 100.0);
                            }

                            vatAmount = (totalForVat + extraVat) * (item.ItemVatPerc / 100.0);
                        }
                    }

                    if (Invo.InvoiceType == InvoiceType.BeginingInventory)
                        vatAmount = 0.0;

                    if (Invo.VATperc == 0.0)
                        vatAmount = 0.0;

                    item.ItemNetPrice = EtaSetting.Active
                        ? totalForVat + vatAmount + extraVat - item.WithholdingTax
                        : totalForVat + vatAmount + extraVat;

                    item.ItemAdditionalTaxPerc = item.isExtraTaxApplied ? Invo.ExtraVATPerc : 0.0;
                    item.ItemAdditionalTax = item.isExtraTaxApplied ? extraVat : 0.0;

                    Invo.SumPrice += item.ItemSumPrice;
                    Invo.Net += item.ItemNetPrice;
                    Invo.ExtraVAT += extraVat;
                    Invo.VAT += vatAmount;
                    Invo.Total += itemTotal;
                }

                Invo.Net = Invo.Total + Invo.VAT + Invo.ExtraVAT;
                Invo.TotDiscount += Invo.InvDiscount;
            }
            catch { /* keep silent like original */ }
        }

        #endregion

        #region Item Loading Methods

        public void GetItemByIDcontract(int ItemId, int UnitId, ref Invoicecontract Invo,
            bool IsScale, string ItemBarcode, string SerialNo)
        {
            try
            {
                DataTable settingsTable = new DataTable();
                new SqlDataAdapter(
                    "SELECT ISNULL(RepeatItem,0) AS RepeatItem FROM SettingsPOSDisplay WHERE Inv_id=3",
                    MainClass.ConnObj()).Fill(settingsTable);

                if (ItemId <= 0) return;

                DataTable itemTable = new DataTable();
                new SqlDataAdapter(
                    $"SELECT name, id, code, nameEN, group_id AS CatagoryId, EgyCodeType, EgyItemCode, " +
                    $"ISNULL(ItemType,0) AS ItemType FROM Items WHERE IS_Deleted=0 AND id={ItemId}",
                    MainClass.ConnObj()).Fill(itemTable);

                if (itemTable.Rows.Count == 0) return;

                if (Invo.InvoiceItems.Count > 0)
                {
                    foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
                    {
                        if (invoiceItem.ItemId == 0)
                        {
                            Invo.InvoiceItems.Remove(invoiceItem);
                        }
                        else if (invoiceItem.ItemId == ItemId && !invoiceItem.IsScaleItem)
                        {
                            if (Invo.InvoiceType == InvoiceType.POS)
                            {
                                if (SafeToBool(settingsTable.Rows[0]["RepeatItem"]))
                                {
                                    MsgExistItem msg = new MsgExistItem();
                                    msg.ShowDialog();
                                    if (msg.Action == 1) invoiceItem.ItemQuantity += 1.0;
                                    else if (msg.Action == 2) break;
                                    return;
                                }

                                if ((invoiceItem.UnitID == UnitId || UnitId == 0) &&
                                    (string.Equals(invoiceItem.ItemBarcode, ItemBarcode, StringComparison.Ordinal) ||
                                     string.IsNullOrEmpty(ItemBarcode)))
                                {
                                    if (!IsScale) { invoiceItem.ItemQuantity += 1.0; return; }
                                }
                            }
                            else if ((Invo.InvoiceType != InvoiceType.POS &&
                                      invoiceItem.UnitID == UnitId) || UnitId == 0)
                            {
                                MsgExistItem msg2 = new MsgExistItem();
                                msg2.ShowDialog();
                                if (msg2.Action == 1) invoiceItem.ItemQuantity += 1.0;
                                else if (msg2.Action == 2) break;
                                return;
                            }
                        }
                    }
                }

                InvoiceItem NewItem = new InvoiceItem
                {
                    ItemId = SafeToInt(itemTable.Rows[0]["id"]),
                    ItemCode = itemTable.Rows[0]["code"]?.ToString() ?? string.Empty,
                    EgyCodeType = itemTable.Rows[0]["EgyCodeType"]?.ToString() ?? string.Empty,
                    EgyItemCode = itemTable.Rows[0]["EgyItemCode"]?.ToString() ?? string.Empty,
                    CatagoryId = SafeToInt(itemTable.Rows[0]["CatagoryId"]),
                    IsScaleItem = IsScale,
                    ItemRowIndex = Invo.InvoiceItems.Count + 1,
                    UnitID = UnitId,
                    ItemBarcode = ItemBarcode,
                    ItemExpireDate = DateTime.Now
                };

                NewItem.ItemName = string.Equals(MainClass.Language, "ar", StringComparison.Ordinal)
                    ? itemTable.Rows[0]["name"]?.ToString() ?? string.Empty
                    : (string.IsNullOrEmpty(itemTable.Rows[0]["nameEN"]?.ToString())
                        ? itemTable.Rows[0]["name"]?.ToString() ?? string.Empty
                        : itemTable.Rows[0]["nameEN"]?.ToString() ?? string.Empty);

                int itemType = SafeToInt(itemTable.Rows[0]["ItemType"]);
                NewItem.InvertoryImpact = (itemType == 5 || itemType == 3) ? 0 : Invo.InvertoryImpact;
                NewItem.InvType = (int)Invo.InvoiceType;
                NewItem.InvertoryId = Invo.Store;
                NewItem.InvertoryName = Invo.InvertoryName;

                if (!string.IsNullOrEmpty(SerialNo))
                {
                    NewItem.InvoiceItemDetails.Add(new InvoiceItemDetail
                    {
                        ItemProperty = 5,
                        ItemId = NewItem.ItemId,
                        ItemSerialNo = SerialNo,
                        ItemExpireDate = DateTime.Now.AddMonths(12),
                        ItemProductionDate = DateTime.Now,
                        ItemQuantity = 1.0
                    });
                }

                LoadItemInfcontract(Invo, ref NewItem);
                Invo.InvoiceItems.Add(NewItem);

                if (Invo.InvoiceType == InvoiceType.POS)
                    Invo.InvoiceItems.OrderByDescending(x => x.ItemRowIndex);
            }
            catch { /* keep silent like original */ }
        }

        public void GetItemByID(int ItemId, int UnitId, ref InvoiceDGV Invo,
            bool IsScale, string ItemBarcode, string SerialNo)
        {
            try
            {
                DataTable settingsTable = new DataTable();
                new SqlDataAdapter(
                    "SELECT ISNULL(RepeatItem,0) AS RepeatItem FROM SettingsPOSDisplay WHERE Inv_id=3",
                    MainClass.ConnObj()).Fill(settingsTable);

                if (ItemId <= 0) return;

                DataTable itemTable = new DataTable();
                new SqlDataAdapter(
                    $"SELECT name, id, code, nameEN, group_id AS CatagoryId, EgyCodeType, EgyItemCode, " +
                    $"ISNULL(ItemType,0) AS ItemType FROM Items WHERE IS_Deleted=0 AND id={ItemId}",
                    MainClass.ConnObj()).Fill(itemTable);

                if (itemTable.Rows.Count == 0) return;

                if (Invo.InvoiceItems.Count > 0)
                {
                    foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
                    {
                        if (invoiceItem.ItemId == 0)
                        {
                            Invo.InvoiceItems.Remove(invoiceItem);
                        }
                        else if (invoiceItem.ItemId == ItemId && !invoiceItem.IsScaleItem)
                        {
                            if (Invo.InvoiceType == InvoiceType.POS)
                            {
                                if (SafeToBool(settingsTable.Rows[0]["RepeatItem"]))
                                {
                                    MsgExistItem msg = new MsgExistItem();
                                    msg.ShowDialog();
                                    if (msg.Action == 1) invoiceItem.ItemQuantity += 1.0;
                                    else if (msg.Action == 2) break;
                                    return;
                                }

                                if ((invoiceItem.UnitID == UnitId || UnitId == 0) &&
                                    (string.Equals(invoiceItem.ItemBarcode, ItemBarcode, StringComparison.Ordinal) ||
                                     string.IsNullOrEmpty(ItemBarcode)))
                                {
                                    if (!IsScale) { invoiceItem.ItemQuantity += 1.0; return; }
                                }
                            }
                            else if ((Invo.InvoiceType != InvoiceType.POS &&
                                      invoiceItem.UnitID == UnitId) || UnitId == 0)
                            {
                                MsgExistItem msg2 = new MsgExistItem();
                                msg2.ShowDialog();
                                if (msg2.Action == 1) invoiceItem.ItemQuantity += 1.0;
                                else if (msg2.Action == 2) break;
                                return;
                            }
                        }
                    }
                }

                InvoiceItem NewItem = new InvoiceItem
                {
                    ItemId = SafeToInt(itemTable.Rows[0]["id"]),
                    ItemCode = itemTable.Rows[0]["code"]?.ToString() ?? string.Empty,
                    EgyCodeType = itemTable.Rows[0]["EgyCodeType"]?.ToString() ?? string.Empty,
                    EgyItemCode = itemTable.Rows[0]["EgyItemCode"]?.ToString() ?? string.Empty,
                    CatagoryId = SafeToInt(itemTable.Rows[0]["CatagoryId"]),
                    IsScaleItem = IsScale,
                    ItemRowIndex = Invo.InvoiceItems.Count + 1,
                    UnitID = UnitId,
                    ItemBarcode = ItemBarcode,
                    ItemExpireDate = DateTime.Now
                };

                NewItem.ItemName = string.Equals(MainClass.Language, "ar", StringComparison.Ordinal)
                    ? itemTable.Rows[0]["name"]?.ToString() ?? string.Empty
                    : (string.IsNullOrEmpty(itemTable.Rows[0]["nameEN"]?.ToString())
                        ? itemTable.Rows[0]["name"]?.ToString() ?? string.Empty
                        : itemTable.Rows[0]["nameEN"]?.ToString() ?? string.Empty);

                int itemType = SafeToInt(itemTable.Rows[0]["ItemType"]);
                NewItem.InvertoryImpact = (itemType == 5 || itemType == 3) ? 0 : Invo.InvertoryImpact;
                NewItem.InvType = (int)Invo.InvoiceType;
                NewItem.InvertoryId = Invo.Store;
                NewItem.InvertoryName = Invo.InvertoryName;

                if (!string.IsNullOrEmpty(SerialNo))
                {
                    NewItem.InvoiceItemDetails.Add(new InvoiceItemDetail
                    {
                        ItemProperty = 5,
                        ItemId = NewItem.ItemId,
                        ItemSerialNo = SerialNo,
                        ItemExpireDate = DateTime.Now.AddMonths(12),
                        ItemProductionDate = DateTime.Now,
                        ItemQuantity = 1.0
                    });
                }

                LoadItemInf(Invo, ref NewItem);
                Invo.InvoiceItems.Add(NewItem);

                if (Invo.InvoiceType == InvoiceType.POS)
                    Invo.InvoiceItems.OrderByDescending(x => x.ItemRowIndex);
            }
            catch { /* keep silent like original */ }
        }

        public static void LoadItemInfcontract(Invoicecontract Invo, ref InvoiceItem NewItem)
        {
            try
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT Items.id, Items.name, Items.barcode, units.name AS unit, " +
                    $"purch_price, sale_price, tax, discount, ItemProperty, FillValue, " +
                    $"WithholdingTax, ISNULL(is_extra_tax_applied,0) AS is_extra_tax_applied " +
                    $"FROM Items, units WHERE Items.unit=units.id AND items.id={NewItem.ItemId}",
                    MainClass.ConnObj()).Fill(dt);

                if (dt.Rows.Count == 0) return;

                LoadUnitInf(ref NewItem, (int)Invo.InvoiceType, Invo.Pricing);
                LoadCustomerPrice(ref NewItem, Invo.Customer);

                NewItem.ItemCost = (Sync.ActiveSync && Sync.BranchType == 4 &&
                                    Invo.InvoiceType == InvoiceType.POS)
                    ? SafeToDouble(dt.Rows[0]["purch_price"])
                    : ItemOper.Cost(NewItem.ItemId);

                NewItem.ValiableInvertory = Inventory.CalcItemStock(
                    NewItem.InvertoryId, NewItem.ItemId, MainClass.BranchNo);

                NewItem.ItemVatPerc = Invo.VATperc;

                if (SafeToInt(dt.Rows[0]["tax"]) == 0)
                    NewItem.ItemVatPerc = SafeToDouble(dt.Rows[0]["tax"]);

                NewItem.WithholdingTaxPerc =
                    (dt.Rows[0]["WithholdingTax"] != DBNull.Value &&
                     !string.IsNullOrEmpty(dt.Rows[0]["WithholdingTax"].ToString()))
                    ? SafeToDouble(dt.Rows[0]["WithholdingTax"])
                    : 0.0;

                NewItem.isExtraTaxApplied = Convert.ToBoolean(dt.Rows[0]["is_extra_tax_applied"]);
                NewItem.ItemDiscount = SafeToDouble(dt.Rows[0]["discount"]);
                NewItem.Description = string.Empty;

                if (dt.Rows[0]["ItemProperty"] == DBNull.Value) return;

                NewItem.ItemProperty = SafeToInt(dt.Rows[0]["ItemProperty"]);

                int prop = NewItem.ItemProperty;
                bool shouldAddDetail = (prop >= 2 && prop <= 5) || prop == 8;

                if (shouldAddDetail && NewItem.InvoiceItemDetails.Count == 0)
                {
                    InvoiceItemDetail detail = new InvoiceItemDetail
                    {
                        ItemProperty = prop,
                        ItemId = NewItem.ItemId
                    };

                    if (dt.Rows[0]["FillValue"] != DBNull.Value)
                        detail.FillRatio = SafeToDouble(dt.Rows[0]["FillValue"]);

                    NewItem.InvoiceItemDetails.Add(detail);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void LoadItemInf(InvoiceDGV Invo, ref InvoiceItem NewItem)
        {
            try
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT Items.id, Items.name, Items.barcode, units.name AS unit, " +
                    $"purch_price, sale_price, tax, discount, ItemProperty, FillValue, " +
                    $"WithholdingTax, ISNULL(is_extra_tax_applied,0) AS is_extra_tax_applied " +
                    $"FROM Items, units WHERE Items.unit=units.id AND items.id={NewItem.ItemId}",
                    MainClass.ConnObj()).Fill(dt);

                if (dt.Rows.Count == 0) return;

                LoadUnitInf(ref NewItem, (int)Invo.InvoiceType, Invo.Pricing);
                LoadCustomerPrice(ref NewItem, Invo.Customer);

                NewItem.ItemCost = (Sync.ActiveSync && Sync.BranchType == 4 &&
                                    Invo.InvoiceType == InvoiceType.POS)
                    ? SafeToDouble(dt.Rows[0]["purch_price"])
                    : ItemOper.Cost(NewItem.ItemId);

                NewItem.ValiableInvertory = Inventory.CalcItemStock(
                    NewItem.InvertoryId, NewItem.ItemId, MainClass.BranchNo);

                NewItem.ItemVatPerc = Invo.VATperc;

                if (SafeToInt(dt.Rows[0]["tax"]) == 0)
                    NewItem.ItemVatPerc = SafeToDouble(dt.Rows[0]["tax"]);

                NewItem.WithholdingTaxPerc =
                    (dt.Rows[0]["WithholdingTax"] != DBNull.Value &&
                     !string.IsNullOrEmpty(dt.Rows[0]["WithholdingTax"].ToString()))
                    ? SafeToDouble(dt.Rows[0]["WithholdingTax"])
                    : 0.0;

                NewItem.isExtraTaxApplied = Convert.ToBoolean(dt.Rows[0]["is_extra_tax_applied"]);
                NewItem.ItemDiscount = SafeToDouble(dt.Rows[0]["discount"]);
                NewItem.Description = string.Empty;

                if (dt.Rows[0]["ItemProperty"] == DBNull.Value) return;

                NewItem.ItemProperty = SafeToInt(dt.Rows[0]["ItemProperty"]);

                int prop = NewItem.ItemProperty;
                bool shouldAddDetail = (prop >= 2 && prop <= 5) || prop == 8;

                if (shouldAddDetail && NewItem.InvoiceItemDetails.Count == 0)
                {
                    InvoiceItemDetail detail = new InvoiceItemDetail
                    {
                        ItemProperty = prop,
                        ItemId = NewItem.ItemId
                    };

                    if (dt.Rows[0]["FillValue"] != DBNull.Value)
                        detail.FillRatio = SafeToDouble(dt.Rows[0]["FillValue"]);

                    NewItem.InvoiceItemDetails.Add(detail);
                }

                LoadItemDetails(Invo, ref NewItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void LoadCustomerPrice(ref InvoiceItem NewItem, int CustomerId)
        {
            SqlConnection conn = MainClass.ConnObj();

            DataTable dt1 = new DataTable();
            new SqlDataAdapter(
                $"SELECT ISNULL(Pricing, -1) AS Pricing FROM Customers " +
                $"WHERE id={CustomerId} AND IS_Deleted=0", conn).Fill(dt1);

            if (dt1.Rows.Count == 0) return;

            int pricing = SafeToInt(dt1.Rows[0]["Pricing"]);

            DataTable dt2 = new DataTable();
            new SqlDataAdapter(
                $"SELECT ISNULL(purch_price, 0) AS purch_price, " +
                $"ISNULL(sale_price, 0) AS sale_price, " +
                $"ISNULL(CompetitorPrice, sale_price) AS CompetitorPrice, " +
                $"ISNULL(WholesalePrice, sale_price) AS WholesalePrice, " +
                $"ISNULL(ConsumerPrice, sale_price) AS ConsumerPrice " +
                $"FROM ItemPrices WHERE ItemID={NewItem.ItemId}", conn).Fill(dt2);

            if (dt2.Rows.Count == 0) return;

            switch (pricing)
            {
                case 1: NewItem.ItemPrice = SafeToDouble(dt2.Rows[0]["sale_price"]); break;
                case 2: NewItem.ItemPrice = SafeToDouble(dt2.Rows[0]["purch_price"]); break;
                case 3: NewItem.ItemPrice = ItemOper.RecentPurchPrice(NewItem.ItemId); break;
                case 4: NewItem.ItemPrice = ItemOper.Cost(NewItem.ItemId); break;
                case 5: NewItem.ItemPrice = SafeToDouble(dt2.Rows[0]["CompetitorPrice"]); break;
                case 6: NewItem.ItemPrice = SafeToDouble(dt2.Rows[0]["WholesalePrice"]); break;
                case 7: NewItem.ItemPrice = SafeToDouble(dt2.Rows[0]["ConsumerPrice"]); break;
            }
        }

        public static void LoadItemDetails(InvoiceDGV Invo, ref InvoiceItem NewItem)
        {
            if (NewItem.InvoiceItemDetails.Count == 0) return;

            InvoiceItemDetail firstDetail = NewItem.InvoiceItemDetails.ElementAtOrDefault(0);
            if (firstDetail == null) return;

            int prop = firstDetail.ItemProperty;

            if (prop >= 2 && prop <= 4)
            {
                frmItemProperties propForm = new frmItemProperties
                {
                    ItemProperty = prop
                };
                propForm.cmbItemProperties.SelectedIndex = prop - 1;
                propForm.GroupBox1.Tag = propForm.cmbItemProperties.Text;

                if (prop == 2)
                {
                    propForm.txtNetQnty.Text = "1";
                    propForm.lblWidth.Text = "التعبئة";
                    propForm.lblResultQnty.Text = " الكمية بالمتر ";
                    propForm.lblHeight.Text = " كرتون ";
                    propForm.txtWidth.Text = firstDetail.FillRatio.ToString();
                }

                propForm.ShowDialog();

                string desc = $"{propForm.lblHeight.Text}: {propForm.txtHeight.Text.Trim()}" +
                              $"{Environment.NewLine}{propForm.lblWidth.Text}: {propForm.txtWidth.Text.Trim()}" +
                              $"{Environment.NewLine}العدد: {propForm.txtQty.Text.Trim()}";

                NewItem.Description = desc;

                if (prop == 2)
                {
                    NewItem.ItemQuantity = propForm.NetQnty;
                    firstDetail.ItemQuantity = propForm.NetQnty;
                    firstDetail.FillValue = double.TryParse(propForm.txtHeight.Text.Trim(), out double fv) ? fv : 0.0;
                }
                else
                {
                    NewItem.ItemQuantity = propForm.NetQnty;
                    firstDetail.ItemQuantity = propForm.NetQnty;
                    firstDetail.FillValue = 0.0;
                    firstDetail.ItemHeight = double.TryParse(propForm.txtHeight.Text.Trim(), out double h) ? h : 0.0;
                    firstDetail.ItemWidth = double.TryParse(propForm.txtWidth.Text.Trim(), out double w) ? w : 0.0;
                }
            }
            else if (prop == 5)
            {
                if (!string.IsNullOrEmpty(firstDetail.ItemSerialNo))
                {
                    NewItem.ItemNotes = string.Empty;
                    foreach (InvoiceItemDetail detail in NewItem.InvoiceItemDetails)
                    {
                        NewItem.ItemQuantity = 1.0;
                        NewItem.ItemNotes += $",{detail.ItemSerialNo} ";
                    }
                    return;
                }

                frmItemSerialNo serialForm = new frmItemSerialNo
                {
                    InvItem = NewItem,
                    Invo = Invo
                };
                serialForm.lblItemName.Text = NewItem.ItemName;
                NewItem.InvoiceItemDetails.Clear();
                serialForm.ShowDialog();
                NewItem.ItemQuantity = 1.0;

                if (serialForm.ISDone)
                {
                    NewItem = serialForm.InvItem;
                    NewItem.ItemQuantity = 0.0;
                    NewItem.ItemNotes = string.Empty;

                    foreach (InvoiceItemDetail detail in NewItem.InvoiceItemDetails)
                    {
                        NewItem.ItemQuantity += detail.ItemQuantity;
                        NewItem.ItemNotes += $",{detail.ItemSerialNo} ";
                    }
                    return;
                }

                serialForm.InvItem.InvoiceItemDetails.Clear();
            }
            else if (prop == 7)
            {
                MessageBox.Show("الرجاء كتابة وصف للصنف في عامود الوصف");
            }
            else if (prop == 8)
            {
                NewItem.ItemExpireDate = ItemExpire(NewItem.ItemId);
            }
        }

        public static DateTime ItemExpire(int ItemId)
        {
            string condition = string.Empty;

            if (ItemId != 0)
                condition += $" AND ItemId={ItemId}";

            DataTable dt = Inventory.ItemsExpirationStock(
                condition + $" AND BranchId={MainClass.BranchNo}").Copy();

            return dt.Rows.Count > 0
                ? Convert.ToDateTime(dt.Rows[0]["ItemExpire"])
                : DateTime.Now;
        }

        public static void LoadUnitInf(ref InvoiceItem NewItem, int InvType, Pricing Pricing)
        {
            try
            {
                if (NewItem.UnitID == 0)
                    NewItem.UnitID = CheckItemUnit(NewItem.ItemId, NewItem.InvType);

                SqlConnection conn = MainClass.ConnObj();

                DataTable dt1 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT purch, sale, barcode, perc, units.name AS UnitName, units.UnitCode " +
                    $"FROM units, ItemUnits " +
                    $"WHERE ItemUnits.ItemId={NewItem.ItemId} AND ItemUnits.unit=units.id " +
                    $"AND units.id={NewItem.UnitID}", conn).Fill(dt1);

                if (dt1.Rows.Count > 0)
                {
                    NewItem.UnitName = dt1.Rows[0]["UnitName"]?.ToString() ?? string.Empty;
                    NewItem.UnitCode = dt1.Rows[0]["UnitCode"]?.ToString() ?? string.Empty;
                    NewItem.ItemBarcode = dt1.Rows[0]["barcode"]?.ToString() ?? string.Empty;
                    NewItem.UnitEquality = SafeToDouble(dt1.Rows[0]["perc"]);
                    NewItem.ItemQuantity = 1.0;
                    NewItem.ItemPrimaryQnty = NewItem.ItemQuantity * NewItem.UnitEquality;

                    NewItem.ItemPrice = (NewItem.InvType == 2 || NewItem.InvType == 3)
                        ? SafeToDouble(dt1.Rows[0]["sale"])
                        : SafeToDouble(dt1.Rows[0]["purch"]);
                    return;
                }

                DataTable dt2 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT units.id AS unitId, units.name AS unitName, " +
                    $"purch_price AS purch, sale_price AS sale, Items.barcode AS barcode, units.UnitCode " +
                    $"FROM Items, units WHERE items.unit=units.id AND items.id={NewItem.ItemId}", conn).Fill(dt2);

                if (dt2.Rows.Count == 0) return;

                NewItem.UnitID = SafeToInt(dt2.Rows[0]["unitId"]);
                NewItem.UnitName = dt2.Rows[0]["unitName"]?.ToString() ?? string.Empty;
                NewItem.UnitCode = dt2.Rows[0]["UnitCode"]?.ToString() ?? string.Empty;
                NewItem.ItemBarcode = dt2.Rows[0]["barcode"]?.ToString() ?? string.Empty;
                NewItem.UnitEquality = 1.0;
                NewItem.ItemQuantity = 1.0;
                NewItem.ItemPrimaryQnty = NewItem.ItemQuantity * NewItem.UnitEquality;

                bool isSale = NewItem.InvType == 2 || NewItem.InvType == 3 || NewItem.InvType == 23;
                bool isPurch = NewItem.InvType == 1;

                if (isSale)
                {
                    NewItem.ItemPrice = Pricing == Pricing.Default
                        ? SafeToDouble(dt2.Rows[0]["sale"])
                        : GetPriceByPricing(Pricing, dt2, NewItem);

                    return;
                }

                if (isPurch)
                {
                    NewItem.ItemPrice = Pricing == Pricing.Default
                        ? SafeToDouble(dt2.Rows[0]["purch"])
                        : GetPurchPriceByPricing(Pricing, dt2, NewItem);

                    return;
                }

                DataTable settingsDt = new DataTable();
                new SqlDataAdapter(
                    "SELECT PriceIncVAT, MainVAT FROM SettingGeneral WHERE Inv_Id=1",
                    conn).Fill(settingsDt);

                if (settingsDt.Rows.Count == 0)
                {
                    NewItem.ItemPrice = Pricing == Pricing.Default
                        ? SafeToDouble(dt2.Rows[0]["sale"])
                        : GetPurchPriceByPricing(Pricing, dt2, NewItem);
                    return;
                }

                bool priceIncVat = Convert.ToBoolean(settingsDt.Rows[0]["PriceIncVAT"]);
                double mainVat = SafeToDouble(settingsDt.Rows[0]["MainVAT"]);

                if (priceIncVat)
                {
                    double basePrice = SafeToDouble(dt2.Rows[0]["purch"]) / (1.0 + mainVat / 100.0);
                    NewItem.ItemPrice = Pricing == Pricing.Default
                        ? SafeToDouble(dt2.Rows[0]["sale"])
                        : GetPurchPriceWithVatByPricing(Pricing, dt2, NewItem, basePrice);
                }
                else
                {
                    NewItem.ItemPrice = Pricing == Pricing.Default
                        ? SafeToDouble(dt2.Rows[0]["sale"])
                        : GetPurchPriceByPricing(Pricing, dt2, NewItem);
                }
            }
            catch { /* keep silent like original */ }
        }

        private static double GetPriceByPricing(Pricing pricing, DataTable dt, InvoiceItem item)
        {
            return pricing switch
            {
                Pricing.SalePrice => SafeToDouble(dt.Rows[0]["purch"]),
                Pricing.PurchasePrice => ItemOper.RecentPurchPrice(item.ItemId),
                Pricing.LastPurchasePrice => ItemOper.Cost(item.ItemId),
                _ => SafeToDouble(dt.Rows[0]["sale"])
            };
        }

        private static double GetPurchPriceByPricing(Pricing pricing, DataTable dt, InvoiceItem item)
        {
            return pricing switch
            {
                Pricing.SalePrice => SafeToDouble(dt.Rows[0]["purch"]),
                Pricing.PurchasePrice => ItemOper.RecentPurchPrice(item.ItemId),
                Pricing.LastPurchasePrice => ItemOper.Cost(item.ItemId),
                _ => SafeToDouble(dt.Rows[0]["purch"])
            };
        }

        private static double GetPurchPriceWithVatByPricing(Pricing pricing, DataTable dt,
            InvoiceItem item, double basePrice)
        {
            return pricing switch
            {
                Pricing.SalePrice => basePrice,
                Pricing.PurchasePrice => ItemOper.RecentPurchPrice(item.ItemId),
                Pricing.LastPurchasePrice => ItemOper.Cost(item.ItemId),
                _ => basePrice
            };
        }

        #endregion

        #region Validation Methods

        public static void IsValidDiscount(ref InvoiceDGV Invo, int ItemID, int RowIndex, bool IsPercentDiscount)
        {
            foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
            {
                if (!(invoiceItem.ItemId == ItemID && invoiceItem.ItemRowIndex == RowIndex))
                    continue;

                DataTable dt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT ISNULL(MaxDicountParcent,0) AS MaxDicountParcent, " +
                    $"ISNULL(MaxDiscountAmount,0) AS MaxDiscountAmount " +
                    $"FROM items WHERE IS_Deleted=0 AND id={invoiceItem.ItemId}",
                    MainClass.ConnObj()).Fill(dt);

                if (dt.Rows.Count == 0) continue;

                string warningMsg = string.Equals(MainClass.Language, "ar", StringComparison.Ordinal)
                    ? "لقد تجاوزت  حد الخصم "
                    : "You have exceeded your discount limit ";

                if (IsPercentDiscount)
                {
                    double maxPerc = SafeToDouble(dt.Rows[0]["MaxDicountParcent"]);
                    if (maxPerc > 0.0 && !User.PassDefDiscount &&
                        invoiceItem.ItemDiscountPerc > maxPerc)
                    {
                        MessageBox.Show(warningMsg, "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        invoiceItem.ItemDiscountPerc = 0.0;
                        invoiceItem.ItemDiscount = 0.0;
                    }
                }
                else
                {
                    double maxAmount = SafeToDouble(dt.Rows[0]["MaxDiscountAmount"]);
                    if (maxAmount > 0.0 && !User.PassDefDiscount &&
                        invoiceItem.ItemDiscount > maxAmount)
                    {
                        MessageBox.Show(warningMsg, "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        invoiceItem.ItemDiscountPerc = 0.0;
                        invoiceItem.ItemDiscount = 0.0;
                    }
                }
            }
        }

        public static void IsValidPrice(ref InvoiceDGV Invo, int ItemID, int RowIndex)
        {
            foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
            {
                if (!(invoiceItem.ItemId == ItemID && invoiceItem.ItemRowIndex == RowIndex))
                    continue;

                DataTable dt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT sale_price, low_sale_price, high_sale_price " +
                    $"FROM ItemPrices WHERE ItemId={invoiceItem.ItemId}",
                    MainClass.ConnObj()).Fill(dt);

                if (dt.Rows.Count == 0) continue;

                double highPrice = SafeToDouble(dt.Rows[0]["high_sale_price"]);
                double lowPrice = SafeToDouble(dt.Rows[0]["low_sale_price"]);

                if (highPrice > 0.0 && !User.PassHeighestSalePrice &&
                    invoiceItem.ItemPrice > highPrice)
                {
                    MessageBox.Show("لقد أدخلت السعر أكبر من أعلى سعر ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (lowPrice > 0.0 && !User.PassLowestSalePrice &&
                    invoiceItem.ItemPrice < lowPrice)
                {
                    MessageBox.Show("لقد أدخلت السعر أقل من أدنى سعر ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    invoiceItem.ItemPrice = SafeToDouble(dt.Rows[0]["sale_price"]);
                    break;
                }

                if (!User.BuyLessAvrgCost && invoiceItem.ItemPrice < invoiceItem.ItemCost)
                {
                    MessageBox.Show("لقد أدخلت سعر أقل من سعر التكلفة  ",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }
            }
        }

        public static void checkQtyLimi(ref InvoiceDGV Invo, int ItemID)
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT limit, ISNULL(MaxQtyLimit, 0) AS MaxLimit " +
                $"FROM items WHERE Id={ItemID} AND IS_Deleted=0",
                MainClass.ConnObj()).Fill(dt);

            if (dt.Rows.Count == 0) return;

            foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
            {
                if (invoiceItem.ItemId != ItemID) continue;

                double minLimit = SafeToDouble(dt.Rows[0][0]);
                double maxLimit = SafeToDouble(dt.Rows[0][1]);

                if (invoiceItem.InvertoryImpact == 2 && Invo.ProcType == 1)
                {
                    double afterQty = invoiceItem.ValiableInvertory - invoiceItem.ItemPrimaryQnty;
                    if (minLimit >= afterQty && minLimit != 0.0)
                        MessageBox.Show("لقد وصلت الحد الأدنى للكمية لهذا الصنف");
                }
                else if (invoiceItem.InvertoryImpact == 1 && Invo.ProcType == 1)
                {
                    double afterQty = invoiceItem.ValiableInvertory + invoiceItem.ItemPrimaryQnty;
                    if (maxLimit <= afterQty && maxLimit > 0.0)
                        MessageBox.Show("لقد وصلت الحد الأعلى للكمية لهذا الصنف");
                }
            }
        }

        public static void ISvalidQuantity(ref InvoiceDGV Invo, int ItemID, int RowIndex, bool SalebyMinus)
        {
            foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
            {
                if (!(invoiceItem.ItemId == ItemID && invoiceItem.ItemRowIndex == RowIndex))
                    continue;

                bool isValid = true;
                double available = invoiceItem.ValiableInvertory;
                invoiceItem.ItemPrimaryQnty = invoiceItem.ItemQuantity * invoiceItem.UnitEquality;
                double primaryQty = invoiceItem.ItemPrimaryQnty;

                foreach (InvoiceItem other in Invo.InvoiceItems)
                {
                    if (other.ItemId == invoiceItem.ItemId)
                    {
                        other.ItemPrimaryQnty = other.ItemQuantity * other.UnitEquality;
                        available -= other.ItemPrimaryQnty;
                    }
                }

                if (!Invo.ISNew)
                {
                    DataTable dt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT val FROM Inv_Sub " +
                        $"WHERE ItemId={ItemID} AND InvGlobalID='{Invo.InvGlobalID}'",
                        MainClass.ConnObj()).Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        available += SafeToDouble(dt.Rows[0][0]);
                        if (available < 0.0) isValid = false;
                    }
                    else if (available < 0.0)
                    {
                        isValid = false;
                    }
                }
                else if (available < 0.0 && !SalebyMinus)
                {
                    isValid = false;
                    invoiceItem.ItemPrimaryQnty = invoiceItem.ValiableInvertory;
                    invoiceItem.ItemQuantity = invoiceItem.ItemPrimaryQnty / invoiceItem.UnitEquality;
                }

                if (!isValid)
                {
                    string msg = string.Equals(MainClass.Language, "en", StringComparison.Ordinal)
                        ? $"Quantity is greater than store balance{Environment.NewLine}" +
                          $"total quantity: {primaryQty}{Environment.NewLine}" +
                          $"store balance: {invoiceItem.InvertoryName} = {available + primaryQty}"
                        : $"الكمية المدخلة للصنف أكبر من رصيد المستودع{Environment.NewLine}" +
                          $"الكمية الاجمالية المدخلة: {primaryQty}{Environment.NewLine}" +
                          $"رصيد المستودع: {invoiceItem.InvertoryName} = {available + primaryQty}";

                    MessageBox.Show($"{msg}{Environment.NewLine}",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Barcode Search

        public static void SearchForBarcode(string Barcode, ref int ItemID, ref int UnitId,
            ref decimal ItemPrice, ref decimal ItemQuantity, bool IsMultiText)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();

                if (string.IsNullOrEmpty(Barcode)) return;

                DataTable dt1 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT Items.id, Items.group_id, ItemUnits.unit, Items.tax, " +
                    $"Items.tax_group, Items.Wscale " +
                    $"FROM Items, ItemUnits " +
                    $"WHERE Items.IS_Deleted=0 AND ItemUnits.ItemId=items.id " +
                    $"AND ItemUnits.barcode=N'{Barcode}'", conn).Fill(dt1);

                if (dt1.Rows.Count > 0)
                {
                    ItemID = SafeToInt(dt1.Rows[0]["id"]);
                    UnitId = SafeToInt(dt1.Rows[0]["unit"]);
                    ItemPrice = default;
                    ItemQuantity = default;
                    return;
                }

                DataTable dt2 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT Itembarcodes.itemId FROM Items, Itembarcodes " +
                    $"WHERE Items.IS_Deleted=0 AND Itembarcodes.ItemId=items.id " +
                    $"AND Itembarcodes.barcode=N'{Barcode}'", conn).Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    ItemID = SafeToInt(dt2.Rows[0]["itemId"]);
                    UnitId = 0;
                    ItemPrice = default;
                    ItemQuantity = default;
                    return;
                }

                if (Barcode.Length == 13)
                {
                    try
                    {
                        string shortCode = Barcode.Substring(0, 7);

                        DataTable dt3 = new DataTable();
                        new SqlDataAdapter(
                            $"SELECT Items.id, Items.group_id, ItemUnits.sale AS SalePrice, " +
                            $"ItemUnits.unit, Items.tax, Items.tax_group, Items.Wscale " +
                            $"FROM Items, ItemUnits " +
                            $"WHERE Items.IS_Deleted=0 AND ItemUnits.ItemId=items.id " +
                            $"AND (ItemUnits.barcode=N'{shortCode}' OR Items.barcode=N'{shortCode}')",
                            conn).Fill(dt3);

                        if (dt3.Rows.Count > 0 && dt3.Rows[0]["Wscale"] != DBNull.Value)
                        {
                            double wscale = SafeToDouble(dt3.Rows[0]["Wscale"]);

                            if (wscale == 1.0)
                            {
                                double qty = (int)Math.Round(SafeToDouble(Barcode.Substring(7, 2))) +
                                             (int)Math.Round(SafeToDouble(Barcode.Substring(9, 3))) / 1000.0;
                                ItemID = SafeToInt(dt3.Rows[0]["id"]);
                                UnitId = SafeToInt(dt3.Rows[0]["unit"]);
                                ItemPrice = default;
                                ItemQuantity = new decimal(qty);
                            }
                            else if (wscale == 2.0)
                            {
                                double price = (int)Math.Round(SafeToDouble(Barcode.Substring(7, 3))) +
                                               (int)Math.Round(SafeToDouble(Barcode.Substring(10, 2))) / 100.0;
                                ItemID = SafeToInt(dt3.Rows[0]["id"]);
                                UnitId = SafeToInt(dt3.Rows[0]["unit"]);
                                ItemPrice = new decimal(price);
                                ItemQuantity = default;
                            }
                        }
                        else if (!IsMultiText)
                        {
                            frmAttentionMsg attMsg = new frmAttentionMsg();
                            attMsg.lblmsg.Text = "هذا الباركود غير موجود ضمن بيانات البرنامج";
                            attMsg.btnClose.Visibility = Visibility.Collapsed;
                            attMsg.btnInsure.Visibility = Visibility.Collapsed;
                            attMsg.ShowDialog();
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

                if (!IsMultiText)
                {
                    frmAttentionMsg attMsg2 = new frmAttentionMsg();
                    attMsg2.lblmsg.Text = "هذا الباركود غير موجود ضمن بيانات البرنامج";
                    attMsg2.btnClose.Visibility = Visibility.Collapsed;
                    attMsg2.btnInsure.Visibility = Visibility.Collapsed;
                    attMsg2.ShowDialog();
                }
            }
            catch { /* keep silent like original */ }
        }

        #endregion

        #region Offer Checking (Private)

        private static bool CheckForCategoryOffer(InvoiceDGV Invo, ref InvoiceItem NewItem)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();

                DataTable dt = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT InvoiceID FROM Offer " +
                    "WHERE OfferStartDate<=@CurrentDate AND OfferExpire>=@CurrentDate " +
                    "AND offerType=3 AND ISDeleted=0 ORDER BY OfferId DESC", conn))
                {
                    adapter.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0) return false;

                if (!Convert.ToInt32(dt.Rows[0]["InvoiceID"]).Equals((int)Invo.InvoiceType))
                    return false;

                if (Invo.ProcType == 2) return false;

                DataTable dt2 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT id, OfferId, OfferItemValue, OfferItemPercentage, CatatogryID " +
                    $"FROM OfferForClient WHERE CatatogryID={NewItem.CatagoryId}", conn).Fill(dt2);

                if (dt2.Rows.Count == 0) return false;

                double offerVal = SafeToDouble(dt2.Rows[0]["OfferItemValue"]);
                double offerPerc = SafeToDouble(dt2.Rows[0]["OfferItemPercentage"]);

                if (offerVal > 0.0)
                {
                    NewItem.ItemDiscount = offerVal;
                }
                else if (offerPerc > 0.0)
                {
                    NewItem.ItemDiscount = NewItem.ItemSumPrice * offerPerc / 100.0;
                    NewItem.ItemDiscountPerc = offerPerc;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckForCategoryOffercontrat(Invoicecontract Invo, ref InvoiceItem NewItem)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();

                DataTable dt = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT InvoiceID FROM Offer " +
                    "WHERE OfferStartDate<=@CurrentDate AND OfferExpire>=@CurrentDate " +
                    "AND offerType=3 AND ISDeleted=0 ORDER BY OfferId DESC", conn))
                {
                    adapter.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0) return false;
                if (!Convert.ToInt32(dt.Rows[0]["InvoiceID"]).Equals((int)Invo.InvoiceType))
                    return false;
                if (Invo.ProcType == 2) return false;

                DataTable dt2 = new DataTable();
                new SqlDataAdapter(
                    $"SELECT id, OfferId, OfferItemValue, OfferItemPercentage, CatatogryID " +
                    $"FROM OfferForClient WHERE CatatogryID={NewItem.CatagoryId}", conn).Fill(dt2);

                if (dt2.Rows.Count == 0) return false;

                double offerVal = SafeToDouble(dt2.Rows[0]["OfferItemValue"]);
                double offerPerc = SafeToDouble(dt2.Rows[0]["OfferItemPercentage"]);

                if (offerVal > 0.0)
                    NewItem.ItemDiscount = offerVal;
                else if (offerPerc > 0.0)
                {
                    NewItem.ItemDiscount = NewItem.ItemSumPrice * offerPerc / 100.0;
                    NewItem.ItemDiscountPerc = offerPerc;
                }

                return true;
            }
            catch { return false; }
        }

        private static bool CheckForItemOffer(InvoiceDGV Invo, InvoiceItem NewItem)
        {
            SqlConnection conn = MainClass.ConnObj();
            DataTable dt = new DataTable();

            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT Offer.offerID, Offer.InvoiceID, OfferItems.OfferNatural, " +
                "OfferItems.IsGroupedItems, OfferItems.unit, OfferItems.OfferTargetQnty, " +
                "OfferItems.OfferItemValue, OfferItems.OfferItemPercentage " +
                "FROM Offer, OfferItems " +
                "WHERE OfferItems.offerId=Offer.OfferId " +
                "AND Offer.OfferStartDate<=@CurrentDate AND Offer.OfferExpire>=@CurrentDate " +
                $"AND Offer.offerType=2 AND Offer.ISDeleted=0 AND OfferItems.ItemID={NewItem.ItemId} " +
                "ORDER BY Offer.OfferId DESC", conn))
            {
                adapter.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                adapter.Fill(dt);
            }

            if (dt.Rows.Count == 0) return false;

            double invId = SafeToDouble(dt.Rows[0]["InvoiceID"]);
            if (invId != (double)NewItem.InvType && invId != 0.0) return false;

            double targetQty = SafeToDouble(dt.Rows[0]["OfferTargetQnty"]);
            double totalQty = Invo.InvoiceItems
                .Where(x => x.ItemId == NewItem.ItemId)
                .Sum(x => x.ItemQuantity);

            if (Convert.ToBoolean(dt.Rows[0]["IsGroupedItems"]))
            {
                DataTable groupDt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT ItemID FROM OfferItems " +
                    $"WHERE OfferItems.offerId={dt.Rows[0]["OfferId"]} AND ItemID<>{NewItem.ItemId}",
                    conn).Fill(groupDt);

                foreach (DataRow gr in groupDt.Rows)
                {
                    totalQty += Invo.InvoiceItems
                        .Where(x => x.ItemId == SafeToInt(gr["ItemId"]))
                        .Sum(x => x.ItemQuantity);
                }
            }

            NewItem.ItemDiscount = 0.0;

            double offerNatural = SafeToDouble(dt.Rows[0]["OfferNatural"]);
            double offerVal = SafeToDouble(dt.Rows[0]["OfferItemValue"]);
            double offerPerc = SafeToDouble(dt.Rows[0]["OfferItemPercentage"]);

            if (totalQty >= targetQty && totalQty != 0.0 && offerNatural == 1.0)
            {
                if (offerVal > 0.0)
                    NewItem.ItemDiscount = (int)(totalQty / targetQty) * offerVal;
                else if (offerPerc > 0.0)
                {
                    NewItem.ItemDiscount = NewItem.ItemSumPrice * offerPerc / 100.0;
                    NewItem.ItemDiscountPerc = offerPerc;
                }
                return true;
            }

            if (totalQty % targetQty == 0.0 && totalQty > targetQty && offerNatural == 2.0)
            {
                AddItemOffer(NewItem, offerVal, (int)Math.Round(offerNatural), true);
                return true;
            }

            return false;
        }

        private static bool CheckForItemOffercontract(Invoicecontract Invo, InvoiceItem NewItem)
        {
            SqlConnection conn = MainClass.ConnObj();
            DataTable dt = new DataTable();

            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT Offer.offerID, Offer.InvoiceID, OfferItems.OfferNatural, " +
                "OfferItems.IsGroupedItems, OfferItems.unit, OfferItems.OfferTargetQnty, " +
                "OfferItems.OfferItemValue, OfferItems.OfferItemPercentage " +
                "FROM Offer, OfferItems " +
                "WHERE OfferItems.offerId=Offer.OfferId " +
                "AND Offer.OfferStartDate<=@CurrentDate AND Offer.OfferExpire>=@CurrentDate " +
                $"AND Offer.offerType=2 AND Offer.ISDeleted=0 AND OfferItems.ItemID={NewItem.ItemId} " +
                "ORDER BY Offer.OfferId DESC", conn))
            {
                adapter.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
                adapter.Fill(dt);
            }

            if (dt.Rows.Count == 0) return false;

            double invId = SafeToDouble(dt.Rows[0]["InvoiceID"]);
            if (invId != (double)NewItem.InvType && invId != 0.0) return false;

            double targetQty = SafeToDouble(dt.Rows[0]["OfferTargetQnty"]);
            double totalQty = Invo.InvoiceItems
                .Where(x => x.ItemId == NewItem.ItemId)
                .Sum(x => x.ItemQuantity);

            if (Convert.ToBoolean(dt.Rows[0]["IsGroupedItems"]))
            {
                DataTable groupDt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT ItemID FROM OfferItems " +
                    $"WHERE OfferItems.offerId={dt.Rows[0]["OfferId"]} AND ItemID<>{NewItem.ItemId}",
                    conn).Fill(groupDt);

                foreach (DataRow gr in groupDt.Rows)
                {
                    totalQty += Invo.InvoiceItems
                        .Where(x => x.ItemId == SafeToInt(gr["ItemId"]))
                        .Sum(x => x.ItemQuantity);
                }
            }

            NewItem.ItemDiscount = 0.0;

            double offerNatural = SafeToDouble(dt.Rows[0]["OfferNatural"]);
            double offerVal = SafeToDouble(dt.Rows[0]["OfferItemValue"]);
            double offerPerc = SafeToDouble(dt.Rows[0]["OfferItemPercentage"]);

            if (totalQty >= targetQty && totalQty != 0.0 && offerNatural == 1.0)
            {
                if (offerVal > 0.0)
                    NewItem.ItemDiscount = (int)(totalQty / targetQty) * offerVal;
                else if (offerPerc > 0.0)
                {
                    NewItem.ItemDiscount = NewItem.ItemSumPrice * offerPerc / 100.0;
                    NewItem.ItemDiscountPerc = offerPerc;
                }
                return true;
            }

            if (totalQty % targetQty == 0.0 && totalQty > targetQty && offerNatural == 2.0)
            {
                AddItemOffer(NewItem, offerVal, (int)Math.Round(offerNatural), true);
                return true;
            }

            return false;
        }

        private static void AddItemOffer(InvoiceItem NewItem, double offerValue, int offerType, bool ISValOffer)
        {
            try
            {
                if (NewItem.ItemId <= 0) return;

                if (offerType == 1)
                {
                    NewItem.ItemDiscount += ISValOffer
                        ? offerValue
                        : NewItem.ItemSumPrice * offerValue / 100.0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        private static void AddItemOffer(ref InvoiceItem NewItem, double offerValue,
            int offerType, bool ISValOffer)
        {
            try
            {
                if (NewItem.ItemId <= 0) return;

                if (offerType == 1)
                {
                    NewItem.ItemDiscount += ISValOffer
                        ? offerValue
                        : NewItem.ItemSumPrice * offerValue / 100.0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Unit Helper

        private static int CheckItemUnit(int itemID, int InvType)
        {
            if (InvType == 10) InvType = 1;
            else if (InvType == 3) InvType = 2;

            SqlConnection conn = MainClass.ConnObj();

            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT id, name FROM units WHERE defaultInv={InvType}", conn).Fill(dt);

            if (dt.Rows.Count == 0) return 0;

            DataTable dt2 = new DataTable();
            new SqlDataAdapter(
                $"SELECT purch, sale FROM ItemUnits " +
                $"WHERE ItemId={itemID} AND unit={SafeToInt(dt.Rows[0]["id"])}",
                conn).Fill(dt2);

            return dt2.Rows.Count > 0 ? SafeToInt(dt.Rows[0]["id"]) : 0;
        }

        #endregion

        #region Other Methods

        public void SaveInvoiceItemDeliveries(List<ItemBatchDelivery> ItemBatchDeliveries, bool IsDeliveriedAll)
        {
            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            SqlTransaction transaction = conn.BeginTransaction();
            string lastGlobalId = string.Empty;

            try
            {
                foreach (ItemBatchDelivery delivery in ItemBatchDeliveries)
                {
                    using SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[ItemBatchDeliveries] " +
                        "([BatchDelivIncId],[BatchDelivNo],[InvGlobalID],[ItemId],[ItemUnitId]," +
                        "[ItemQuantity],[BatchDeliveryDate],[InvertoryEmp],[RecipientID],[Note]) " +
                        "VALUES(@BatchDelivIncId,@BatchDelivNo,@InvGlobalID,@ItemId,@ItemUnitId," +
                        "@ItemQuantity,@BatchDeliveryDate,@InvertoryEmp,@RecipientID,@Note)",
                        conn, transaction);

                    cmd.Parameters.Add("@BatchDelivIncId", SqlDbType.Int).Value = delivery.BatchDelivIncId;
                    cmd.Parameters.Add("@BatchDelivNo", SqlDbType.Int).Value = delivery.BatchDelivNo;
                    cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = delivery.InvGlobalID;
                    cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = delivery.ItemId;
                    cmd.Parameters.Add("@ItemUnitId", SqlDbType.Int).Value = delivery.ItemUnitId;
                    cmd.Parameters.Add("@ItemQuantity", SqlDbType.Float).Value = delivery.ItemQuantity;
                    cmd.Parameters.Add("@BatchDeliveryDate", SqlDbType.DateTime).Value = delivery.BatchDeliveryDate;
                    cmd.Parameters.Add("@InvertoryEmp", SqlDbType.Int).Value = delivery.InvertoryEmp;
                    cmd.Parameters.Add("@RecipientID", SqlDbType.Int).Value = delivery.RecipientID;
                    cmd.Parameters.Add("@Note", SqlDbType.NVarChar).Value = delivery.Note;
                    cmd.ExecuteNonQuery();

                    lastGlobalId = delivery.InvGlobalID;
                }

                if (IsDeliveriedAll && !string.IsNullOrEmpty(lastGlobalId))
                {
                    using SqlCommand updateCmd = new SqlCommand(
                        $"UPDATE Inv SET InvoiceStatus=3 WHERE InvGlobalID=N'{lastGlobalId}'",
                        conn, transaction);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                MessageBox.Show("تم تسليم الكميات بنجاح ");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show(
                    GetErrorMessage("خطأ أثناء تسليم الكميات", "error in saving", ex.Message),
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void GetInventoryOrder(ref InvoiceDGV inv)
        {
            DataTable dt = Inventory.CalcItemsStockLimits(inv.Branch, inv.Store);
            if (dt.Rows.Count == 0) return;

            foreach (DataRow row in dt.Rows)
            {
                double stock = SafeToDouble(row["ItemStock"]);
                double minLimit = SafeToDouble(row["MinQntyLimt"]);

                bool belowMin = stock < 0.0 || (stock <= minLimit && minLimit > 0.0);
                if (!belowMin) continue;

                try
                {
                    GetItemByID(SafeToInt(row["ItemId"]), 0, ref inv, false, "", "");
                }
                catch { /* keep silent */ }
            }
        }

        public List<Product> BindProduct(List<int> itemList)
        {
            List<Product> list = new List<Product>();
            SqlConnection conn1 = MainClass.ConnObj();
            SqlConnection conn2 = MainClass.ConnObj();

            foreach (int itemId in itemList)
            {
                DataTable dt = new DataTable();
                new SqlDataAdapter($"SELECT * FROM items WHERE id={itemId}", conn1).Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    Product product = new Product
                    {
                        ClientCode = Sync.ClientCode,
                        ProductId = SafeToInt(row["Id"]),
                        Code = row["code"]?.ToString() ?? string.Empty,
                        Name = row["name"]?.ToString() ?? string.Empty,
                        NameEN = row["nameEN"]?.ToString() ?? string.Empty,
                        Category = row["Grpcode"]?.ToString() ?? string.Empty,
                        CategoryID = row["group_id"]?.ToString() ?? string.Empty,
                        MainUnit = SafeToInt(row["unit"]),
                        MainBarcode = row["barcode"]?.ToString() ?? string.Empty,
                        ShowInPOS = SafeToBool(row["ShowInPOS"]),
                        PurchPrice = SafeToDouble(row["purch_price"]),
                        SalePrice = SafeToDouble(row["sale_price"]),
                        limitStock = SafeToInt(row["limit"]),
                        VAT = SafeToDouble(row["tax"]),
                        CreateDate = DateTime.Now,
                        LastUpdateDate = DateTime.Now,
                        Branch = MainClass.BranchNo,
                        DistBranch = Sync.DistBranch,
                        BranchType = Sync.BranchType,
                        Active = true
                    };

                    if (row["ItemProperty"] != DBNull.Value)
                        product.Property = (ProductProperty)SafeToInt(row["ItemProperty"]);

                    if (row["ItemType"] != DBNull.Value)
                        product.Type = (ProductTypes)SafeToInt(row["ItemType"]);

                    if (row["FillValue"] != DBNull.Value)
                        product.PackingValue = SafeToDouble(row["FillValue"]);

                    if (row["MaxQtyLimit"] != DBNull.Value)
                        product.MaxQtyLimit = SafeToDouble(row["MaxQtyLimit"]);

                    DataTable unitsDt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT ItemUnits.unit, ItemUnits.perc, ItemUnits.purch, ItemUnits.sale, " +
                        $"ItemUnits.barcode, Units.name FROM ItemUnits " +
                        $"LEFT JOIN units ON ItemUnits.unit=units.id WHERE ItemId={product.ProductId}",
                        conn2).Fill(unitsDt);

                    foreach (DataRow ur in unitsDt.Rows)
                    {
                        product.ProductUnits.Add(new ProductUnit
                        {
                            ProductId = product.ProductId,
                            UnitId = SafeToInt(ur["unit"]),
                            UnitName = ur["name"]?.ToString() ?? string.Empty,
                            UnitEquality = SafeToDouble(ur["perc"]),
                            PurchasePrice = SafeToDouble(ur["purch"]),
                            SalePrice = SafeToDouble(ur["sale"]),
                            Barcode = ur["barcode"]?.ToString() ?? string.Empty,
                            ClientCode = Sync.ClientCode
                        });
                    }

                    DataTable compDt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT ItemComponents.Id, ItemComponents.ComponentId, ItemComponents.store, " +
                        $"ItemComponents.itemId, ItemComponents.quantity, ItemComponents.price, " +
                        $"ItemComponents.total, ItemComponents.type, ItemComponents.unit AS unit_id, " +
                        $"items.id AS item_ID, items.Code AS item_Code, items.name AS Item_name " +
                        $"FROM ItemComponents LEFT JOIN items ON ItemComponents.ComponentId=items.id " +
                        $"WHERE itemId={product.ProductId}", conn2).Fill(compDt);

                    foreach (DataRow cr in compDt.Rows)
                    {
                        if (SafeToInt(cr["ComponentId"]) <= 0) continue;

                        product.ProductComponents.Add(new ProductComponent
                        {
                            ProductId = product.ProductId,
                            UnitId = SafeToInt(cr["unit_id"]),
                            ComponentId = SafeToInt(cr["ComponentId"]),
                            ComponentCost = Convert.ToSingle(cr["price"]),
                            Quantity = cr["quantity"]?.ToString() ?? string.Empty,
                            InvertoryId = SafeToInt(cr["store"]),
                            ComponentType = SafeToInt(cr["type"]),
                            ClientCode = Sync.ClientCode
                        });
                    }

                    DataTable barDt = new DataTable();
                    new SqlDataAdapter(
                        $"SELECT * FROM Itembarcodes WHERE ItemId={product.ProductId}",
                        conn1).Fill(barDt);

                    foreach (DataRow br in barDt.Rows)
                    {
                        product.ProductBarcodes.Add(new ProductBarcode
                        {
                            Barcode = br["barcode"]?.ToString() ?? string.Empty,
                            ProductId = SafeToInt(br["ItemId"]),
                            UnitId = 1,
                            ClientCode = Sync.ClientCode
                        });
                    }

                    list.Add(product);
                }
            }

            return list;
        }

        public List<Product> BindingProducts(int offset)
        {
            SqlConnection conn = MainClass.ConnObj();
            List<Product> list = new List<Product>();

            DataTable dt = new DataTable();
            new SqlDataAdapter(
                "SELECT items.id AS itemId, Items.code AS ItemCode, Items.name AS ItemName, " +
                "Items.nameEn AS ItemNameEn, Items.ItemType, items.unit AS MainUnit, " +
                "items.barcode AS MainBarcode, Items.ItemProperty, items.ShowInPOS, Items.FillValue, " +
                "items.discount, items.tax, items.limit, items.sale_price, Items.purch_price, " +
                "ItemsCategory.name AS Categoryname, ItemsCategory.Id AS CategoryId, " +
                "ItemPrices.low_sale_price, ItemPrices.high_sale_price, " +
                "ItemPrices.CompetitorPrice, ItemPrices.WholesalePrice, ItemPrices.ConsumerPrice " +
                "FROM Items " +
                "LEFT JOIN ItemsCategory ON Items.group_id=ItemsCategory.id " +
                "LEFT JOIN ItemPrices ON Items.id=ItemPrices.ItemID " +
                $"ORDER BY Items.id OFFSET {offset} ROWS FETCH NEXT 100 ROWS ONLY",
                conn).Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                if (row["CategoryName"] == DBNull.Value) continue;

                Product product = new Product
                {
                    ClientCode = Sync.ClientCode,
                    ProductId = SafeToInt(row["itemId"]),
                    Code = row["ItemCode"]?.ToString() ?? string.Empty,
                    Name = row["ItemName"]?.ToString() ?? string.Empty,
                    NameEN = row["ItemNameEn"]?.ToString() ?? string.Empty,
                    Category = row["CategoryName"]?.ToString() ?? string.Empty,
                    CategoryID = row["CategoryId"]?.ToString() ?? string.Empty,
                    MainUnit = SafeToInt(row["MainUnit"]),
                    MainBarcode = row["MainBarcode"]?.ToString() ?? string.Empty,
                    ShowInPOS = SafeToBool(row["ShowInPOS"]),
                    PurchPrice = SafeToDouble(row["purch_price"]),
                    SalePrice = SafeToDouble(row["sale_price"]),
                    limitStock = SafeToInt(row["limit"]),
                    VAT = SafeToDouble(row["tax"]),
                    CreateDate = DateTime.Now,
                    LastUpdateDate = DateTime.Now,
                    Branch = MainClass.BranchNo,
                    DistBranch = Sync.DistBranch,
                    BranchType = Sync.BranchType,
                    Active = true
                };

                if (row["ItemProperty"] != DBNull.Value)
                    product.Property = (ProductProperty)SafeToInt(row["ItemProperty"]);

                if (row["ItemType"] != DBNull.Value)
                    product.Type = (ProductTypes)SafeToInt(row["ItemType"]);

                if (row["FillValue"] != DBNull.Value)
                    product.PackingValue = SafeToDouble(row["FillValue"]);

                ProductPrices prices = new ProductPrices
                {
                    ProductId = product.ProductId,
                    ClientCode = Sync.ClientCode,
                    UnitId = product.MainUnit,
                    date = Convert.ToDateTime(DateTime.Now.ToShortDateString()),
                    SalePrice = product.SalePrice,
                    PurchasePrice = product.PurchPrice,
                    LowPurchasePrice = 0.0,
                    HighPurchasePrice = 0.0,
                    LowSalePrice = row["low_sale_price"] != DBNull.Value
                        ? SafeToDouble(row["low_sale_price"]) : 0.0,
                    HighSalePrice = product.SalePrice,
                    CompetitorPrice = row["CompetitorPrice"] != DBNull.Value
                        ? SafeToDouble(row["CompetitorPrice"]) : 0.0,
                    ISDeleted = false
                };
                product.productPrices = prices;

                DataTable unitsDt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT ItemUnits.unit, ItemUnits.perc, ItemUnits.purch, ItemUnits.sale, " +
                    $"ItemUnits.barcode, Units.name FROM ItemUnits " +
                    $"LEFT JOIN units ON ItemUnits.unit=units.id WHERE ItemId={product.ProductId}",
                    conn).Fill(unitsDt);

                foreach (DataRow ur in unitsDt.Rows)
                {
                    product.ProductUnits.Add(new ProductUnit
                    {
                        ProductId = product.ProductId,
                        UnitId = SafeToInt(ur["unit"]),
                        UnitName = ur["name"]?.ToString() ?? string.Empty,
                        UnitEquality = SafeToDouble(ur["perc"]),
                        PurchasePrice = SafeToDouble(ur["purch"]),
                        SalePrice = SafeToDouble(ur["sale"]),
                        Barcode = ur["barcode"]?.ToString() ?? string.Empty,
                        ClientCode = Sync.ClientCode
                    });
                }

                DataTable compDt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT ItemComponents.Id, ItemComponents.ComponentId, ItemComponents.store, " +
                    $"ItemComponents.itemId, ItemComponents.quantity, ItemComponents.price, " +
                    $"ItemComponents.total, ItemComponents.type, ItemComponents.unit AS unit_id, " +
                    $"items.id AS item_ID, items.Code AS item_Code, items.name AS Item_name " +
                    $"FROM ItemComponents LEFT JOIN items ON ItemComponents.ComponentId=items.id " +
                    $"WHERE itemId={product.ProductId}", conn).Fill(compDt);

                foreach (DataRow cr in compDt.Rows)
                {
                    if (SafeToInt(cr["ComponentId"]) <= 0) continue;

                    product.ProductComponents.Add(new ProductComponent
                    {
                        ProductId = product.ProductId,
                        UnitId = SafeToInt(cr["unit_id"]),
                        ComponentId = SafeToInt(cr["ComponentId"]),
                        ComponentCost = Convert.ToSingle(cr["price"]),
                        Quantity = cr["quantity"]?.ToString() ?? string.Empty,
                        InvertoryId = SafeToInt(cr["store"]),
                        ComponentType = SafeToInt(cr["type"]),
                        ClientCode = Sync.ClientCode
                    });
                }

                DataTable barDt = new DataTable();
                new SqlDataAdapter(
                    $"SELECT * FROM Itembarcodes WHERE ItemId={product.ProductId}",
                    conn).Fill(barDt);

                foreach (DataRow br in barDt.Rows)
                {
                    product.ProductBarcodes.Add(new ProductBarcode
                    {
                        Barcode = br["barcode"]?.ToString() ?? string.Empty,
                        ProductId = SafeToInt(br["ItemId"]),
                        UnitId = 1,
                        ClientCode = Sync.ClientCode
                    });
                }

                list.Add(product);
            }

            return list;
        }

        public List<Category> BindToCategory()
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter(
                "SELECT CategoryId, Code, Name, NameEn, ParentCode, printer, ShowInPOS, type, " +
                "DispalyOrder, ISNULL(AllBranch,1) AS AllBranch, ISNULL(BranchId,-1) AS BranchId " +
                "FROM ItemsCategory", MainClass.ConnObj()).Fill(dt);

            List<Category> list = new List<Category>();

            foreach (DataRow row in dt.Rows)
            {
                Category cat = new Category
                {
                    ClientCode = Sync.ClientCode,
                    CategoryId = SafeToInt(row["CategoryId"]),
                    Code = row["Code"]?.ToString() ?? string.Empty,
                    Name = row["Name"]?.ToString() ?? string.Empty,
                    NameEN = row["NameEn"]?.ToString() ?? string.Empty,
                    ParentCode = row["ParentCode"]?.ToString() ?? string.Empty,
                    Printer = row["printer"]?.ToString() ?? string.Empty,
                    ShowInPOS = SafeToBool(row["ShowInPOS"]),
                    ISLeaf = SafeToDouble(row["type"]) == 2.0,
                    Active = true,
                    DispalyOrder = SafeToInt(row["DispalyOrder"]),
                    AllBranch = SafeToBool(row["AllBranch"]),
                    BranchId = SafeToInt(row["BranchId"])
                };

                list.Add(cat);
            }

            return list;
        }

        public List<Unit> BindToUnit()
        {
            DataTable dt = new DataTable();
            new SqlDataAdapter("SELECT * FROM units WHERE IS_Deleted=0",
                MainClass.ConnObj()).Fill(dt);

            List<Unit> list = new List<Unit>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Unit
                {
                    ClientCode = Sync.ClientCode,
                    UnitId = SafeToInt(row["id"]),
                    Name = row["name"]?.ToString() ?? string.Empty,
                    NameEn = row["name"]?.ToString() ?? string.Empty,
                    DefaultInv = SafeToInt(row["defaultInv"])
                });
            }

            return list;
        }

        public static string GenerateItemCode(string GrpCode)
        {
            using SqlConnection conn = MainClass.ConnObj();

            DataTable dt = new DataTable();
            new SqlDataAdapter(
                $"SELECT ISNULL(MAX(CAST(code AS INT)), 0) AS MaxCode " +
                $"FROM Items WHERE GrpCode=N'{GrpCode}'", conn).Fill(dt);

            if (dt.Rows.Count == 0)
                return GrpCode + "0001";

            string maxCodeStr = dt.Rows[0]["MaxCode"]?.ToString() ?? "0";
            int nextNum = 1;

            if (maxCodeStr != "0" && maxCodeStr.Length > GrpCode.Length)
            {
                string suffix = maxCodeStr.Substring(GrpCode.Length);
                nextNum = (int)Math.Round(SafeToDouble(suffix) + 1.0);
            }

            string GenerateCode(int n) =>
                n < 10 ? GrpCode + "000" + n :
                n < 100 ? GrpCode + "00" + n :
                n < 1000 ? GrpCode + "0" + n :
                            GrpCode + n;

            string candidate = GenerateCode(nextNum);

            if (conn.State != ConnectionState.Open)
                conn.Open();

            while (true)
            {
                using SqlCommand checkCmd = new SqlCommand(
                    $"SELECT COUNT(*) FROM Items WHERE code=N'{candidate}'", conn);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                    return candidate;

                nextNum++;
                candidate = GenerateCode(nextNum);
            }
        }

        #endregion
    }
}