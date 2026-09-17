using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using ETA_Invoice.Models;
using SmartAuditERP.Form_WPF;
using Ws_Auditor;

namespace SmartAuditERP
{
    public class ItemOper
    {
        #region Fields & Properties

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

        public static bool IsPriceFromBarcode = false;

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

        #region Helpers

        private static double ToDouble(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return double.TryParse(value.ToString(), out double result) ? result : 0;
        }

        private static int ToInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        private static string ToStr(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        private static bool ToBool(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool b) return b;
            return value.ToString() == "1" || value.ToString().ToLower() == "true";
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return decimal.TryParse(value.ToString(), out decimal result) ? result : 0;
        }

        private static float ToFloat(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return float.TryParse(value.ToString(), out float result) ? result : 0;
        }

        private static void ShowAttentionMsg(string message)
        {
            try
            {
                frmAttentionMsg attentionWindow = new frmAttentionMsg();
                attentionWindow.lblmsg.Text = message;
                attentionWindow.btnClose.Visibility = Visibility.Collapsed;
                attentionWindow.btnInsure.Visibility = Visibility.Collapsed;
                attentionWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show(message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string GetLangMessage(string arabicMsg, string englishMsg)
        {
            return MainClass.Language == "en" ? englishMsg : arabicMsg;
        }

        #endregion

        #region Cost Methods

        public static double Cost(int itemId)
        {
            if (Common.CostType == 1) return AvgCost(itemId, MainClass.BranchNo);
            if (Common.CostType == 2) return ToDouble(PurchPrice(itemId));
            if (Common.CostType == 3) return RecentPurchPrice(itemId);
            return 0;
        }

        public static double AvgCost(int itemId, int branchId, int storeId = -1)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter("ItemAvrgCost", MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.Add("@branch", SqlDbType.Int).Value = branchId;
                    adapter.SelectCommand.Parameters.Add("@item", SqlDbType.Int).Value = itemId;
                    adapter.SelectCommand.Parameters.Add("@store", SqlDbType.Int).Value =
                        storeId == -1 ? (object)DBNull.Value : storeId;
                    adapter.SelectCommand.Parameters.Add("@ItemAvrgCost", SqlDbType.Float)
                        .Direction = ParameterDirection.Output;
                    adapter.Fill(dt);

                    double result = ToDouble(
                        adapter.SelectCommand.Parameters["@ItemAvrgCost"].Value);
                    return result < 0 ? 0 : result;
                }
            }
            catch { return 0; }
        }

        public static double RecentPurchPrice(int itemId)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();
                string branchCond = MainClass.BranchNo != -1
                    ? $" branch={MainClass.BranchNo} and "
                    : string.Empty;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select val,val1,exchange_price from inv,inv_sub where {branchCond}" +
                    $" Inv_Sub.ItemId={itemId} and inv.proc_type=1 and inv.inv_type=1" +
                    $" and Inv_Sub.exchange_price <> 0 and inv_sub.proc_type=1" +
                    $" and inv.InvGlobalID=inv_sub.InvGlobalID" +
                    $" and IS_Deleted=0 order by inv.id desc", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        double val = ToDouble(dt.Rows[0]["val"]);
                        if (val == 0) return 0;
                        return Math.Round(
                            ToDouble(dt.Rows[0]["val1"]) *
                            ToDouble(dt.Rows[0]["exchange_price"]) / val, 2);
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT purch_price FROM Items WHERE id=@id AND IS_Deleted=0", conn))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@id", itemId);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                        return Convert.ToDouble(
                            Convert.ToDecimal(dt.Rows[0]["purch_price"]));
                }
            }
            catch { }
            return 0;
        }

        public static object PurchPrice(int itemId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select purch_price from Items where id={itemId}", MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0) return dt.Rows[0]["purch_price"];
                }
            }
            catch { }
            return 0;
        }

        #endregion

        #region Item Property Methods

        public static void ItemProperty(int itemId, ref int itemPrty, ref double fillValue)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select ItemProperty,FillValue from Items where items.id={itemId}",
                    MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        itemPrty = ToInt(dt.Rows[0]["ItemProperty"]);
                        fillValue = ToDouble(dt.Rows[0]["FillValue"]);
                    }
                    else
                    {
                        itemPrty = 0;
                        fillValue = 0;
                    }
                }
            }
            catch
            {
                itemPrty = 0;
                fillValue = 0;
            }
        }

        public static void CheckForValidStock(int itemId, ref double validQnty, ref double cost)
        {
            cost = AvgCost(itemId, MainClass.BranchNo);
        }

        #endregion

        #region Cost Recalculation

        public static void RecalculateCost(int itemId, double newQnty,
            ref double avgCost, string cond)
        {
            SqlConnection conn = MainClass.ConnObj();
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select CurrentQnty,Inv_Sub.InvGlobalID," +
                    $" Inv_Sub.AvrgCost as AvrgCost," +
                    $" ItemId as ItemID, inv.minus as Invdiscount" +
                    $" from inv, Inv_Sub" +
                    $" where inv.inv_type>=2 and inv.inv_type<=3" +
                    $" and inv.proc_type=1 and inv.IS_Deleted=0" +
                    $" and inv.InvGlobalID=inv_sub.InvGlobalID" +
                    $" and Inv_Sub.ItemId={itemId}" +
                    $" and CurrentQnty<0 {cond}" +
                    $" order by inv.date Asc", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) return;
                    if (conn.State != ConnectionState.Open) conn.Open();

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        if (!(newQnty > 0)) break;

                        double newCurrentQnty =
                            newQnty + ToDouble(dt.Rows[i]["CurrentQnty"]);
                        string invGlobalId = ToStr(dt.Rows[i]["InvGlobalID"]);
                        int dbItemId = ToInt(dt.Rows[i]["ItemID"]);
                        double invDiscount = ToDouble(dt.Rows[i]["Invdiscount"]);

                        using (SqlCommand cmd = new SqlCommand(
                            StoredQueries.UpdateItemCost, conn))
                        {
                            cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar)
                                .Value = invGlobalId;
                            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = dbItemId;
                            cmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = avgCost;
                            cmd.Parameters.Add("@CurrentQnty", SqlDbType.Float)
                                .Value = newCurrentQnty;
                            cmd.ExecuteNonQuery();
                        }

                        newQnty = newCurrentQnty;

                        double sumVal = 0;
                        double itDiscount = 0;
                        double avgCostVal = 0;
                        bool isValid = true;

                        using (SqlDataAdapter subAdapter = new SqlDataAdapter(
                            $"select CurrentQnty," +
                            $" Inv_Sub.val1*Inv_Sub.exchange_price as Sum," +
                            $" ISNULL(Inv_Sub.discount,0) as ItDiscount," +
                            $" Inv_Sub.taxval as Vat," +
                            $" Inv_Sub.val*Inv_Sub.AvrgCost as AvrgCost" +
                            $" from Inv_Sub" +
                            $" where Inv_Sub.InvGlobalID=N'{invGlobalId}'", conn))
                        {
                            DataTable subDt = new DataTable();
                            subAdapter.Fill(subDt);

                            foreach (DataRow subRow in subDt.Rows)
                            {
                                if (ToDouble(subRow["CurrentQnty"]) < 0)
                                    isValid = false;
                                sumVal += ToDouble(subRow["Sum"]);
                                itDiscount += ToDouble(subRow["ItDiscount"]);
                                avgCostVal += ToDouble(subRow["AvrgCost"]);
                            }
                        }

                        itDiscount += invDiscount;
                        double profit = sumVal - itDiscount - avgCostVal;

                        if (isValid)
                        {
                            using (SqlCommand profitCmd = new SqlCommand(
                                StoredQueries.UpdateInvProfit, conn))
                            {
                                profitCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar)
                                    .Value = invGlobalId;
                                profitCmd.Parameters.Add("@InvProfit", SqlDbType.Int)
                                    .Value = profit;
                                profitCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region Group / Item Info

        public static string GetGroupCode(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select code from ItemsCategory where id={id}", MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0) return ToStr(dt.Rows[0][0]);
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetItemName(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select name from Items where id={id}", MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0) return ToStr(dt.Rows[0][0]);
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GetItemCode(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select isnull(Code,'') from Items where id={id}", MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0) return dt.Rows[0][0].ToString() ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }

        public static string GenerateItemCode(string grpCode)
        {
            SqlConnection conn = MainClass.ConnObj();
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select ISNULL(MAX(CAST(code AS INT)),0) as MaxCode" +
                    $" from Items where GrpCode=N'{grpCode}'", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) return grpCode + "0001";

                    string codeText = ToStr(dt.Rows[0]["MaxCode"]);
                    int num = 1;

                    if (codeText != "0")
                    {
                        num = (int)Math.Round(
                            ToDouble(codeText.Substring(
                                grpCode.Length,
                                codeText.Length - grpCode.Length)) + 1);
                    }

                    string newCode = num < 10 ? grpCode + "000" + num
                        : num < 100 ? grpCode + "00" + num
                        : num < 1000 ? grpCode + "0" + num
                        : grpCode + num;

                    if (conn.State != ConnectionState.Open) conn.Open();

                    while (true)
                    {
                        using (SqlCommand check = new SqlCommand(
                            $"Select COUNT(*) from Items where code=N'{newCode}'", conn))
                        {
                            if (ToDouble(check.ExecuteScalar()) == 0) break;
                        }
                        num++;
                        newCode = grpCode + num;
                    }

                    return newCode;
                }
            }
            catch { return grpCode + "0001"; }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region Load Item Inf

        public static void LoadItemInf(InvoiceDGV invo, ref InvoiceItem newItem)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Items.id, Items.name, Items.barcode," +
                    " units.name as unit, purch_price, sale_price," +
                    " tax, discount, ItemProperty, FillValue," +
                    " WithholdingTax," +
                    " isnull(is_extra_tax_applied,0) as is_extra_tax_applied" +
                    $" from Items, units" +
                    $" where Items.unit=units.id and items.id={newItem.ItemId}",
                    MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) return;

                    LoadUnitInf(ref newItem,
                        newItem.InvType,
                        invo.Pricing);

                    LoadCustomerPrice(ref newItem, invo.Customer);

                    if (Sync.ActiveSync &&
                        Sync.BranchType == 4 &&
                        invo.InvoiceType == InvoiceType.POS)
                        newItem.ItemCost = ToDouble(dt.Rows[0]["purch_price"]);
                    else
                        newItem.ItemCost = Cost(newItem.ItemId);

                    newItem.ValiableInvertory = Inventory.CalcItemStock(
                        newItem.InvertoryId, newItem.ItemId, MainClass.BranchNo);

                    newItem.ItemVatPerc = invo.VATperc;

                    if (ToDouble(dt.Rows[0]["tax"]) == 0)
                        newItem.ItemVatPerc = ToDouble(dt.Rows[0]["tax"]);

                    if (dt.Rows[0]["WithholdingTax"] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(ToStr(dt.Rows[0]["WithholdingTax"])))
                        newItem.WithholdingTaxPerc = ToDouble(dt.Rows[0]["WithholdingTax"]);
                    else
                        newItem.WithholdingTaxPerc = 0;

                    newItem.isExtraTaxApplied =
                        ToBool(dt.Rows[0]["is_extra_tax_applied"]);

                    newItem.ItemDiscount = ToDouble(dt.Rows[0]["discount"]);
                    newItem.Description = string.Empty;

                    if (dt.Rows[0]["ItemProperty"] == DBNull.Value) return;

                    newItem.ItemProperty = ToInt(dt.Rows[0]["ItemProperty"]);

                    int prop = newItem.ItemProperty;
                    bool needsDetails =
                        (prop >= 2 && prop <= 5) || prop == 8;

                    if (!needsDetails) return;

                    if (newItem.InvoiceItemDetails.Count == 0)
                    {
                        InvoiceItemDetail detail = new InvoiceItemDetail
                        {
                            ItemProperty = prop,
                            ItemId = newItem.ItemId
                        };

                        if (dt.Rows[0]["FillValue"] != DBNull.Value)
                            detail.FillRatio = ToDouble(dt.Rows[0]["FillValue"]);

                        newItem.InvoiceItemDetails.Add(detail);
                    }

                    LoadItemDetails(invo, ref newItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Item Details (مطابق 100% للكود الأصلي)

        public static void LoadItemDetails(InvoiceDGV invo, ref InvoiceItem newItem)
        {
            if (newItem.InvoiceItemDetails.Count <= 0) return;

            InvoiceItemDetail firstDetail =
                newItem.InvoiceItemDetails.ElementAtOrDefault(0);
            if (firstDetail == null) return;

            int itemProp = firstDetail.ItemProperty;

            // ═══ حالة: مساحة أو تعبئة أو متر طولي (2,3,4) ═══
            if (itemProp >= 2 && itemProp <= 4)
            {
                frmItemProperties frmItemProperties2 = new frmItemProperties();
                frmItemProperties2.ItemProperty = itemProp;

                // تعيين الخاصية في القائمة
                if (frmItemProperties2.cmbItemProperties.Items.Count >= itemProp)
                    frmItemProperties2.cmbItemProperties.SelectedIndex = itemProp - 1;

                // تعيين عنوان GroupBox1 عبر Tag
                // لأن Border في WPF لا يدعم خاصية Text
                frmItemProperties2.GroupBox1.Tag =
                    frmItemProperties2.cmbItemProperties.Text;

                if (itemProp == 2) // تعبئة
                {
                    frmItemProperties2.txtNetQnty.Text = "1";
                    frmItemProperties2.lblWidth.Text = "التعبئة";
                    frmItemProperties2.lblResultQnty.Text = "الكمية بالمتر";
                    frmItemProperties2.lblHeight.Text = "كرتون";
                    frmItemProperties2.txtWidth.Text =
                        firstDetail.FillRatio.ToString();
                }

                frmItemProperties2.ShowDialog();

                // بناء الوصف كما في الكود الأصلي
                string description =
                    frmItemProperties2.lblHeight.Text + ": " +
                    frmItemProperties2.txtHeight.Text.Trim() +
                    Environment.NewLine +
                    frmItemProperties2.lblWidth.Text + ": " +
                    frmItemProperties2.txtWidth.Text.Trim() +
                    Environment.NewLine +
                    "العدد: " +
                    frmItemProperties2.txtQty.Text.Trim();

                newItem.Description = description;

                if (itemProp == 2) // تعبئة
                {
                    newItem.ItemQuantity = frmItemProperties2.NetQnty;
                    firstDetail.ItemQuantity = frmItemProperties2.NetQnty;
                    firstDetail.FillValue =
                        ToDouble(frmItemProperties2.txtHeight.Text.Trim());
                }
                else // مساحة أو متر طولي
                {
                    newItem.ItemQuantity = frmItemProperties2.NetQnty;
                    firstDetail.ItemQuantity = frmItemProperties2.NetQnty;
                    firstDetail.FillValue = 0;
                    firstDetail.ItemHeight =
                        ToDouble(frmItemProperties2.txtHeight.Text.Trim());
                    firstDetail.ItemWidth =
                        ToDouble(frmItemProperties2.txtWidth.Text.Trim());
                }
            }

            // ═══ حالة: رقم سيريال (5) ═══
            else if (itemProp == 5)
            {
                if (!string.IsNullOrEmpty(firstDetail.ItemSerialNo))
                {
                    newItem.ItemNotes = string.Empty;
                    foreach (InvoiceItemDetail detail in newItem.InvoiceItemDetails)
                    {
                        newItem.ItemQuantity = 1;
                        newItem.ItemNotes += "," + detail.ItemSerialNo + " ";
                    }
                    return;
                }

                frmItemSerialNo frmItemSerialNo2 = new frmItemSerialNo();
                frmItemSerialNo2.lblItemName.Text = newItem.ItemName;
                frmItemSerialNo2.InvItem = newItem;
                frmItemSerialNo2.Invo = invo;
                newItem.InvoiceItemDetails.Clear();
                frmItemSerialNo2.ShowDialog();
                newItem.ItemQuantity = 1;

                if (frmItemSerialNo2.ISDone)
                {
                    newItem = frmItemSerialNo2.InvItem;
                    newItem.ItemQuantity = 0;
                    newItem.ItemNotes = string.Empty;
                    foreach (InvoiceItemDetail detail in newItem.InvoiceItemDetails)
                    {
                        newItem.ItemQuantity += detail.ItemQuantity;
                        newItem.ItemNotes += "," + detail.ItemSerialNo + " ";
                    }
                    return;
                }

                frmItemSerialNo2.InvItem.InvoiceItemDetails.Clear();
            }

            // ═══ حالة: وصف يدوي (7) ═══
            else if (itemProp == 7)
            {
                MessageBox.Show(
                    "الرجاء كتابة وصف للصنف في عامود الوصف",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // ═══ حالة: تاريخ انتهاء (8) ═══
            else if (itemProp == 8)
            {
                newItem.ItemExpireDate = ItemExpire(newItem.ItemId);
            }
        }

        public static DateTime ItemExpire(int itemId)
        {
            try
            {
                string cond = string.Empty;
                if (itemId != 0)
                    cond = cond + " and ItemId=" + itemId;

                DataTable dt = Inventory.ItemsExpirationStock(
                    cond + " and BranchId=" + MainClass.BranchNo).Copy();

                if (dt.Rows.Count > 0)
                    return Convert.ToDateTime(dt.Rows[0]["ItemExpire"]);
            }
            catch { }
            return DateTime.Now;
        }

        #endregion

        #region Unit Info

        public static void LoadUnitInf(ref InvoiceItem newItem,
            int invType, Pricing pricing)
        {
            try
            {
                if (newItem.UnitID == 0)
                    newItem.UnitID = CheckItemUnit(newItem.ItemId, newItem.InvType);

                SqlConnection conn = MainClass.ConnObj();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select purch, sale, barcode, perc," +
                    " units.name as UnitName, units.UnitCode" +
                    $" from units, ItemUnits" +
                    $" where ItemUnits.ItemId={newItem.ItemId}" +
                    $" and ItemUnits.unit=units.id" +
                    $" and units.id={newItem.UnitID}", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        newItem.UnitName = ToStr(row["UnitName"]);
                        newItem.UnitCode = ToStr(row["UnitCode"]);
                        newItem.ItemBarcode = ToStr(row["barcode"]);
                        newItem.UnitEquality = ToDouble(row["perc"]);
                        newItem.ItemQuantity = 1;
                        newItem.ItemPrimaryQnty =
                            newItem.ItemQuantity * newItem.UnitEquality;

                        if (newItem.InvType == 2 || newItem.InvType == 3)
                            newItem.ItemPrice = ToDouble(row["sale"]);
                        else
                            newItem.ItemPrice = ToDouble(row["purch"]);

                        return;
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select units.id as unitId," +
                    " units.name as unitName," +
                    " purch_price as purch," +
                    " sale_price as sale," +
                    " Items.barcode as barcode," +
                    " units.UnitCode" +
                    $" from Items, units" +
                    $" where items.unit=units.id" +
                    $" and items.id={newItem.ItemId}", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) return;

                    DataRow row = dt.Rows[0];
                    newItem.UnitID = ToInt(row["unitId"]);
                    newItem.UnitName = ToStr(row["unitName"]);
                    newItem.UnitCode = ToStr(row["UnitCode"]);
                    newItem.ItemBarcode = ToStr(row["barcode"]);
                    newItem.UnitEquality = 1;
                    newItem.ItemQuantity = 1;
                    newItem.ItemPrimaryQnty =
                        newItem.ItemQuantity * newItem.UnitEquality;

                    double sale = ToDouble(row["sale"]);
                    double purch = ToDouble(row["purch"]);

                    // InvType = 2 أو 3 (بيع)
                    if (newItem.InvType == 2 || newItem.InvType == 3)
                    {
                        newItem.ItemPrice = ResolvePrice(
                            pricing, newItem.ItemId, sale, purch);
                        if (pricing == Pricing.Default)
                            newItem.ItemPrice = sale;
                        return;
                    }

                    // InvType = 1 (شراء)
                    if (newItem.InvType == 1)
                    {
                        newItem.ItemPrice = ResolvePricePurch(
                            pricing, newItem.ItemId, sale, purch);
                        if (pricing == Pricing.Default)
                            newItem.ItemPrice = purch;
                        return;
                    }

                    // InvType أخرى
                    using (SqlDataAdapter vatAdapter = new SqlDataAdapter(
                        "select PriceIncVAT, MainVAT" +
                        " from SettingGeneral where Inv_Id=1", conn))
                    {
                        DataTable vatDt = new DataTable();
                        vatAdapter.Fill(vatDt);

                        if (vatDt.Rows.Count > 0)
                        {
                            if (ToBool(vatDt.Rows[0]["PriceIncVAT"]))
                            {
                                double vatRate =
                                    ToDouble(vatDt.Rows[0]["MainVAT"]);
                                double basePurch =
                                    purch / (1 + vatRate / 100);

                                switch (pricing)
                                {
                                    case Pricing.Default:
                                        newItem.ItemPrice = sale;
                                        break;
                                    case Pricing.SalePrice:
                                        newItem.ItemPrice = basePurch;
                                        break;
                                    case Pricing.PurchasePrice:
                                        newItem.ItemPrice =
                                            RecentPurchPrice(newItem.ItemId);
                                        break;
                                    case Pricing.LastPurchasePrice:
                                        newItem.ItemPrice =
                                            Cost(newItem.ItemId);
                                        break;
                                    default:
                                        newItem.ItemPrice = basePurch;
                                        break;
                                }

                                if (pricing == Pricing.Default)
                                    newItem.ItemPrice = basePurch;
                            }
                            else
                            {
                                switch (pricing)
                                {
                                    case Pricing.Default:
                                        newItem.ItemPrice = sale;
                                        break;
                                    case Pricing.SalePrice:
                                        newItem.ItemPrice = purch;
                                        break;
                                    case Pricing.PurchasePrice:
                                        newItem.ItemPrice =
                                            RecentPurchPrice(newItem.ItemId);
                                        break;
                                    case Pricing.LastPurchasePrice:
                                        newItem.ItemPrice =
                                            Cost(newItem.ItemId);
                                        break;
                                    default:
                                        newItem.ItemPrice = purch;
                                        break;
                                }

                                if (pricing == Pricing.Default)
                                    newItem.ItemPrice = purch;
                            }
                        }
                        else
                        {
                            switch (pricing)
                            {
                                case Pricing.Default:
                                    newItem.ItemPrice = sale;
                                    break;
                                case Pricing.SalePrice:
                                    newItem.ItemPrice = purch;
                                    break;
                                case Pricing.PurchasePrice:
                                    newItem.ItemPrice =
                                        Cost(newItem.ItemId);
                                    break;
                                default:
                                    newItem.ItemPrice = purch;
                                    break;
                            }

                            if (pricing == Pricing.Default)
                                newItem.ItemPrice = purch;
                        }
                    }
                }
            }
            catch { }
        }

        private static double ResolvePrice(Pricing pricing,
            int itemId, double sale, double purch)
        {
            switch (pricing)
            {
                case Pricing.Default: return sale;
                case Pricing.SalePrice: return purch;
                case Pricing.PurchasePrice: return RecentPurchPrice(itemId);
                case Pricing.LastPurchasePrice: return Cost(itemId);
                default: return sale;
            }
        }

        private static double ResolvePricePurch(Pricing pricing,
            int itemId, double sale, double purch)
        {
            switch (pricing)
            {
                case Pricing.Default: return purch;
                case Pricing.SalePrice: return purch;
                case Pricing.PurchasePrice: return RecentPurchPrice(itemId);
                case Pricing.LastPurchasePrice: return Cost(itemId);
                default: return purch;
            }
        }

        private static int CheckItemUnit(int itemId, int invType)
        {
            if (invType == 10) invType = 1;
            else if (invType == 3) invType = 2;

            try
            {
                SqlConnection conn = MainClass.ConnObj();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select id,name from units where defaultInv={invType}", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        int unitId = ToInt(dt.Rows[0]["id"]);

                        using (SqlDataAdapter adapter2 = new SqlDataAdapter(
                            $"select purch,sale from ItemUnits" +
                            $" where ItemId={itemId} and unit={unitId}", conn))
                        {
                            DataTable dt2 = new DataTable();
                            adapter2.Fill(dt2);
                            if (dt2.Rows.Count > 0) return unitId;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        #endregion

        #region Customer Price

        public static void LoadCustomerPrice(ref InvoiceItem newItem, int customerId)
        {
            SqlConnection conn = MainClass.ConnObj();
            try
            {
                int pricing = -1;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT ISNULL(Pricing,-1) AS Pricing" +
                    $" FROM Customers" +
                    $" WHERE id={customerId} AND IS_Deleted=0", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0) return;
                    pricing = ToInt(dt.Rows[0]["Pricing"]);
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT ISNULL(purch_price,0) AS purch_price," +
                    " ISNULL(sale_price,0) AS sale_price," +
                    " ISNULL(CompetitorPrice,sale_price) AS CompetitorPrice," +
                    " ISNULL(WholesalePrice,sale_price) AS WholesalePrice," +
                    " ISNULL(ConsumerPrice,sale_price) AS ConsumerPrice" +
                    $" FROM ItemPrices WHERE ItemID={newItem.ItemId}", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0) return;

                    DataRow row = dt.Rows[0];
                    switch (pricing)
                    {
                        case 1:
                            newItem.ItemPrice = ToDouble(row["sale_price"]);
                            break;
                        case 2:
                            newItem.ItemPrice = ToDouble(row["purch_price"]);
                            break;
                        case 3:
                            newItem.ItemPrice = RecentPurchPrice(newItem.ItemId);
                            break;
                        case 4:
                            newItem.ItemPrice = Cost(newItem.ItemId);
                            break;
                        case 5:
                            newItem.ItemPrice = ToDouble(row["CompetitorPrice"]);
                            break;
                        case 6:
                            newItem.ItemPrice = ToDouble(row["WholesalePrice"]);
                            break;
                        case 7:
                            newItem.ItemPrice = ToDouble(row["ConsumerPrice"]);
                            break;
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Get Item By ID

        public void GetItemByID(int itemId, int unitId,
            ref InvoiceDGV invo, bool isScale,
            string itemBarcode, string serialNo)
        {
            try
            {
                if (itemId <= 0) return;

                bool repeatItem = false;
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select ISNULL(RepeatItem,0) as RepeatItem" +
                    " from SettingsPOSDisplay where Inv_id=3",
                    MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                        repeatItem = ToBool(dt.Rows[0]["RepeatItem"]);
                }

                DataTable itemDt = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name, id, code, nameEN," +
                    " group_id as CatagoryId," +
                    " EgyCodeType, EgyItemCode," +
                    " ISNULL(ItemType,0) ItemType" +
                    $" from Items where IS_Deleted=0 and id={itemId}",
                    MainClass.ConnObj()))
                {
                    adapter.Fill(itemDt);
                }

                if (itemDt.Rows.Count == 0) return;

                // فحص التكرار كما في الكود الأصلي
                if (invo.InvoiceItems.Count > 0)
                {
                    foreach (InvoiceItem existingItem in
                        invo.InvoiceItems.ToList())
                    {
                        if (existingItem.ItemId == 0)
                        {
                            invo.InvoiceItems.Remove(existingItem);
                            continue;
                        }

                        if (!(existingItem.ItemId == itemId &&
                              !existingItem.IsScaleItem))
                            continue;

                        if (invo.InvoiceType == InvoiceType.POS)
                        {
                            if (repeatItem)
                            {
                                MsgExistItem msgExist = new MsgExistItem();
                                msgExist.ShowDialog();
                                if (msgExist.Action == 1)
                                    existingItem.ItemQuantity += 1;
                                else if (msgExist.Action == 2)
                                    break;
                                return;
                            }

                            if (existingItem.UnitID == unitId || unitId == 0)
                            {
                                if (!(existingItem.ItemBarcode == itemBarcode ||
                                      itemBarcode == string.Empty))
                                    break;

                                if (!isScale)
                                {
                                    existingItem.ItemQuantity += 1;
                                    return;
                                }
                            }
                        }
                        else if ((invo.InvoiceType != InvoiceType.POS &&
                                  existingItem.UnitID == unitId) ||
                                 unitId == 0)
                        {
                            MsgExistItem msgExist2 = new MsgExistItem();
                            msgExist2.ShowDialog();
                            if (msgExist2.Action == 1)
                                existingItem.ItemQuantity += 1;
                            else if (msgExist2.Action == 2)
                                break;
                            return;
                        }
                    }
                }

                DataRow itemRow = itemDt.Rows[0];
                InvoiceItem newItem = new InvoiceItem();
                newItem.ItemId = ToInt(itemRow["id"]);
                newItem.ItemCode = ToStr(itemRow["code"]);
                newItem.EgyCodeType = ToStr(itemRow["EgyCodeType"]);
                newItem.EgyItemCode = ToStr(itemRow["EgyItemCode"]);
                newItem.CatagoryId = ToInt(itemRow["CatagoryId"]);
                newItem.IsScaleItem = isScale;
                newItem.ItemRowIndex = invo.InvoiceItems.Count + 1;
                newItem.UnitID = unitId;
                newItem.ItemBarcode = itemBarcode;
                newItem.ItemType = ToInt(itemRow["ItemType"]);
                newItem.InvType = (int)invo.InvoiceType;
                newItem.InvertoryId = invo.Store;
                newItem.InvertoryName = invo.InvertoryName;
                newItem.ItemExpireDate = DateTime.Now;

                if (MainClass.Language == "ar")
                    newItem.ItemName = ToStr(itemRow["name"]);
                else
                {
                    newItem.ItemName = ToStr(itemRow["nameEN"]);
                    if (string.IsNullOrEmpty(newItem.ItemName))
                        newItem.ItemName = ToStr(itemRow["name"]);
                }

                int itemType = newItem.ItemType;
                newItem.InvertoryImpact =
                    (itemType == 5 || itemType == 3) ? 0 : invo.InvertoryImpact;

                if (!string.IsNullOrEmpty(serialNo))
                {
                    newItem.InvoiceItemDetails.Add(new InvoiceItemDetail
                    {
                        ItemProperty = 5,
                        ItemId = newItem.ItemId,
                        ItemSerialNo = serialNo,
                        ItemExpireDate = DateTime.Now.AddMonths(12),
                        ItemProductionDate = DateTime.Now,
                        ItemQuantity = 1
                    });
                }

                LoadItemInf(invo, ref newItem);
                invo.InvoiceItems.Add(newItem);

                if (invo.InvoiceType == InvoiceType.POS)
                    invo.InvoiceItems.OrderByDescending(x => x.ItemRowIndex);
            }
            catch { }
        }

        #endregion

        #region Offers

        private static bool CheckForCategoryOffer(
            InvoiceDGV invo, ref InvoiceItem newItem)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select InvoiceID from Offer" +
                    " where OfferStartDate<=@CurrentDate" +
                    " and OfferExpire>=@CurrentDate" +
                    " and offerType=3 and ISDeleted=0" +
                    " order by OfferId Desc", conn))
                {
                    adapter.SelectCommand.Parameters
                        .Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 0) return false;
                    if (ToInt(dt.Rows[0]["InvoiceID"]) != (int)invo.InvoiceType)
                        return false;
                    if (invo.ProcType == 2) return false;

                    using (SqlDataAdapter adapter2 = new SqlDataAdapter(
                        "select id, OfferId, OfferItemValue," +
                        " OfferItemPercentage, CatatogryID" +
                        $" from OfferForClient" +
                        $" where CatatogryID={newItem.CatagoryId}", conn))
                    {
                        DataTable dt2 = new DataTable();
                        adapter2.Fill(dt2);

                        if (dt2.Rows.Count == 0) return false;

                        double offerVal = ToDouble(dt2.Rows[0]["OfferItemValue"]);
                        double offerPerc =
                            ToDouble(dt2.Rows[0]["OfferItemPercentage"]);

                        if (offerVal > 0)
                        {
                            newItem.ItemDiscount = offerVal;
                        }
                        else if (offerPerc > 0)
                        {
                            newItem.ItemDiscount =
                                newItem.ItemSumPrice * offerPerc / 100;
                            newItem.ItemDiscountPerc = offerPerc;
                        }
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        private static bool CheckForItemOffer(
            InvoiceDGV invo, ref InvoiceItem newItem)
        {
            SqlConnection conn = MainClass.ConnObj();

            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select Offer.offerID, Offer.InvoiceID," +
                " OfferItems.OfferNatural," +
                " OfferItems.IsGroupedItems," +
                " OfferItems.unit," +
                " OfferItems.OfferTargetQnty," +
                " OfferItems.OfferItemValue," +
                " OfferItems.OfferItemPercentage" +
                " from Offer, OfferItems" +
                " where OfferItems.offerId=Offer.OfferId" +
                " and Offer.OfferStartDate<=@CurrentDate" +
                " and Offer.OfferExpire>=@CurrentDate" +
                " and Offer.offerType=2" +
                " and Offer.ISDeleted=0" +
                $" and OfferItems.ItemID={newItem.ItemId}" +
                " order by Offer.OfferId Desc", conn))
            {
                adapter.SelectCommand.Parameters
                    .Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return false;

                double offerInvType = ToDouble(dt.Rows[0]["InvoiceID"]);
                if (offerInvType != newItem.InvType && offerInvType != 0)
                    return false;

                double targetQty = ToDouble(dt.Rows[0]["OfferTargetQnty"]);
                double totalQty = 0;

                foreach (InvoiceItem item in invo.InvoiceItems)
                    if (item.ItemId == newItem.ItemId)
                        totalQty += item.ItemQuantity;

                if (ToBool(dt.Rows[0]["IsGroupedItems"]))
                {
                    using (SqlDataAdapter gAdapter = new SqlDataAdapter(
                        $"select ItemID from OfferItems" +
                        $" where OfferItems.offerId={ToInt(dt.Rows[0]["OfferId"])}" +
                        $" and ItemID<>{newItem.ItemId}", conn))
                    {
                        DataTable gDt = new DataTable();
                        gAdapter.Fill(gDt);
                        foreach (DataRow gRow in gDt.Rows)
                            foreach (InvoiceItem item in invo.InvoiceItems)
                                if (item.ItemId == ToInt(gRow["ItemId"]))
                                    totalQty += item.ItemQuantity;
                    }
                }

                newItem.ItemDiscount = 0;
                double offerNatural = ToDouble(dt.Rows[0]["OfferNatural"]);
                double offerVal = ToDouble(dt.Rows[0]["OfferItemValue"]);
                double offerPerc = ToDouble(dt.Rows[0]["OfferItemPercentage"]);

                if (totalQty >= targetQty &&
                    totalQty != 0 &&
                    offerNatural == 1)
                {
                    if (offerVal > 0)
                        newItem.ItemDiscount =
                            (int)(totalQty / targetQty) * offerVal;
                    else if (offerPerc > 0)
                    {
                        newItem.ItemDiscount =
                            newItem.ItemSumPrice * offerPerc / 100;
                        newItem.ItemDiscountPerc = offerPerc;
                    }
                    return true;
                }

                if (totalQty % targetQty == 0 &&
                    totalQty > targetQty &&
                    offerNatural == 2)
                {
                    AddItemOffer(ref newItem, offerVal,
                        (int)Math.Round(offerNatural), true);
                    return true;
                }
            }
            return false;
        }

        private static void AddItemOffer(ref InvoiceItem newItem,
            double offerValue, int offerType, bool isValOffer)
        {
            try
            {
                if (newItem.ItemId <= 0) return;
                if (offerType == 1)
                {
                    if (isValOffer)
                        newItem.ItemDiscount += offerValue;
                    else
                        newItem.ItemDiscount =
                            newItem.ItemSumPrice * offerValue / 100;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Calc Rows / Total

        public static void CalcRows(ref InvoiceDGV invo)
        {
            try
            {
                invo.Total = 0;
                invo.SumPrice = 0;
                invo.TotDiscount = 0;
                invo.ItemsDiscount = 0;
                invo.VAT = 0;
                invo.ExtraVAT = 0;
                invo.Net = 0;
                invo.SumCost = 0;
                invo.InvProfit = 0;
                invo.TotalQty = 0;
                invo.TotalWithholdingTax = 0;

                foreach (InvoiceItem invoiceItem in invo.InvoiceItems)
                {
                    InvoiceItem item = invoiceItem;
                    double extraVat = 0;
                    double vat = 0;

                    if (item.ItemId > 0)
                    {
                        if (invo.PriceIncVAT)
                            item.ItemPriceWithoutVAT =
                                item.ItemPrice / (1 + item.ItemVatPerc / 100);
                        else
                            item.ItemPriceWithoutVAT = item.ItemPrice;

                        item.ItemPrimaryQnty =
                            item.ItemQuantity * item.UnitEquality;

                        if (item.IsScaleItem && IsPriceFromBarcode)
                            item.ItemSumPrice = item.ItemPrice;
                        else
                            item.ItemSumPrice =
                                item.ItemQuantity * item.ItemPrice;

                        CheckForItemOffer(invo, ref item);
                        CheckForCategoryOffer(invo, ref item);

                        item.ItemTotalPrice =
                            item.ItemSumPrice - item.ItemDiscount;
                        item.ItemPriceAfterDiscount =
                            item.ItemQuantity > 0
                                ? item.ItemTotalPrice / item.ItemQuantity
                                : 0;

                        double lineTotal = item.ItemTotalPrice;

                        if (lineTotal > 0 &&
                            (item.ItemVatPerc > 0 ||
                             (long)Math.Round(invo.ExtraVATPerc) != 0))
                        {
                            if (invo.PriceIncVAT)
                            {
                                vat = lineTotal -
                                      lineTotal / (1 + item.ItemVatPerc / 100);
                                lineTotal -= vat;

                                if (item.isExtraTaxApplied &&
                                    invo.ExtraVATPerc > 0)
                                {
                                    if (item.ItemPriceWithoutVAT < 25)
                                    {
                                        extraVat = 25.0 * item.ItemQuantity;
                                        vat += extraVat *
                                               (item.ItemVatPerc / 100);
                                    }
                                    else
                                    {
                                        extraVat = lineTotal *
                                                   (invo.ExtraVATPerc / 100);
                                        vat += extraVat *
                                               (item.ItemVatPerc / 100);
                                    }
                                }

                                item.ItemTotalPrice = lineTotal;
                            }
                            else
                            {
                                if (item.isExtraTaxApplied &&
                                    invo.ExtraVATPerc > 0)
                                {
                                    extraVat = item.ItemPriceWithoutVAT < 25
                                        ? 25.0 * item.ItemQuantity
                                        : lineTotal * (invo.ExtraVATPerc / 100);
                                }

                                vat = (lineTotal + extraVat) *
                                      (item.ItemVatPerc / 100);
                            }
                        }

                        if (invo.InvoiceType == InvoiceType.BeginingInventory ||
                            invo.VATperc == 0)
                            vat = 0;

                        if (EtaSetting.Active)
                            item.WithholdingTax =
                                item.ItemTotalPrice *
                                (item.WithholdingTaxPerc / 100);

                        item.ItemVat = vat;

                        if (EtaSetting.Active)
                            item.ItemNetPrice =
                                item.ItemTotalPrice + vat + extraVat -
                                item.WithholdingTax;
                        else
                            item.ItemNetPrice =
                                item.ItemTotalPrice + vat + extraVat;
                    }

                    if (item.isExtraTaxApplied)
                    {
                        item.ItemAdditionalTaxPerc = invo.ExtraVATPerc;
                        item.ItemAdditionalTax = extraVat;
                    }
                    else
                    {
                        item.ItemAdditionalTaxPerc = 0;
                        item.ItemAdditionalTax = 0;
                    }

                    invo.SumPrice += item.ItemSumPrice;
                    invo.Net += item.ItemNetPrice;
                    invo.ExtraVAT += extraVat;
                    invo.VAT += vat;
                    invo.Total += item.ItemTotalPrice;
                    invo.SumCost +=
                        item.ItemCost * item.ItemPrimaryQnty;
                    invo.InvProfit += invo.Total - invo.SumCost;
                    invo.ItemsDiscount += item.ItemDiscount;
                    invo.TotalQty +=
                        new decimal(item.ItemPrimaryQnty);
                    invo.TotalWithholdingTax += item.WithholdingTax;
                }

                invo.IsUpdated = true;
                invo.TotDiscount = invo.ItemsDiscount;

                if ((invo.InvoiceType == InvoiceType.Sale ||
                     invo.InvoiceType == InvoiceType.POS) &&
                    invo.ProcType == 1 &&
                    invo.PayType != 5)
                    InvoiceOper.CheckForOffer(ref invo);

                if (invo.InvDiscount > 0)
                    CalcTotal(ref invo);

                if (invo.Additions > 0)
                {
                    if (!invo.PriceIncVAT)
                    {
                        double vatOnAdd =
                            invo.Additions * (invo.VATperc / 100);
                        invo.Net += vatOnAdd;
                    }
                }
            }
            catch { }
        }

        public static void CalcTotal(ref InvoiceDGV invo)
        {
            try
            {
                double sumPrice = invo.SumPrice;
                invo.SumPrice = 0;
                invo.Net = 0;
                invo.ExtraVAT = 0;
                invo.VAT = 0;
                invo.Total = 0;

                foreach (InvoiceItem item in invo.InvoiceItems)
                {
                    double vat = 0;
                    double extraVat = 0;
                    double lineTotal;

                    if (invo.InvDiscount == sumPrice)
                    {
                        item.ItemDiscount = 0;
                        lineTotal = 0;
                    }
                    else
                    {
                        double itemDisc =
                            invo.InvDiscount *
                            item.ItemSumPrice / sumPrice;
                        lineTotal =
                            item.ItemSumPrice -
                            item.ItemDiscount -
                            itemDisc;
                    }

                    double linePriceForVat = lineTotal;

                    if (linePriceForVat > 0 &&
                        (item.ItemVatPerc > 0 ||
                         (long)Math.Round(invo.ExtraVATPerc) != 0))
                    {
                        if (invo.PriceIncVAT)
                        {
                            vat = linePriceForVat -
                                  linePriceForVat /
                                  (1 + item.ItemVatPerc / 100);
                            linePriceForVat -= vat;

                            if (item.isExtraTaxApplied &&
                                invo.ExtraVATPerc > 0)
                            {
                                if (item.ItemPriceWithoutVAT < 25)
                                {
                                    extraVat =
                                        25.0 * item.ItemQuantity;
                                    vat += extraVat *
                                           (item.ItemVatPerc / 100);
                                }
                                else
                                {
                                    extraVat = linePriceForVat *
                                               (invo.ExtraVATPerc / 100);
                                    vat += extraVat *
                                           (item.ItemVatPerc / 100);
                                }
                            }

                            lineTotal = linePriceForVat;
                        }
                        else
                        {
                            if (item.isExtraTaxApplied &&
                                invo.ExtraVATPerc > 0)
                            {
                                extraVat =
                                    item.ItemPriceWithoutVAT < 25
                                        ? 25.0 * item.ItemQuantity
                                        : linePriceForVat *
                                          (invo.ExtraVATPerc / 100);
                            }

                            vat = (linePriceForVat + extraVat) *
                                  (item.ItemVatPerc / 100);
                        }
                    }

                    if (invo.InvoiceType == InvoiceType.BeginingInventory ||
                        invo.VATperc == 0)
                        vat = 0;

                    if (EtaSetting.Active)
                        item.ItemNetPrice =
                            linePriceForVat + vat + extraVat -
                            item.WithholdingTax;
                    else
                        item.ItemNetPrice =
                            linePriceForVat + vat + extraVat;

                    item.ItemAdditionalTaxPerc =
                        item.isExtraTaxApplied ? invo.ExtraVATPerc : 0;
                    item.ItemAdditionalTax =
                        item.isExtraTaxApplied ? extraVat : 0;

                    invo.SumPrice += item.ItemSumPrice;
                    invo.Net += item.ItemNetPrice;
                    invo.ExtraVAT += extraVat;
                    invo.VAT += vat;
                    invo.Total += lineTotal;
                }

                invo.Net = invo.Total + invo.VAT + invo.ExtraVAT;
                invo.TotDiscount += invo.InvDiscount;
            }
            catch { }
        }

        #endregion

        #region Validation Methods

        public static void IsValidDiscount(ref InvoiceDGV invo,
            int itemId, int rowIndex, bool isPercentDiscount)
        {
            foreach (InvoiceItem item in invo.InvoiceItems)
            {
                if (item.ItemId != itemId ||
                    item.ItemRowIndex != rowIndex) continue;

                try
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "select ISNULL(MaxDicountParcent,0) MaxDicountParcent," +
                        " ISNULL(MaxDiscountAmount,0) MaxDiscountAmount" +
                        $" from items where IS_Deleted=0 and id={item.ItemId}",
                        MainClass.ConnObj()))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count == 0) continue;

                        double maxPerc = ToDouble(dt.Rows[0]["MaxDicountParcent"]);
                        double maxAmount =
                            ToDouble(dt.Rows[0]["MaxDiscountAmount"]);

                        if (isPercentDiscount)
                        {
                            if (maxPerc > 0 &&
                                !User.PassDefDiscount &&
                                item.ItemDiscountPerc > maxPerc)
                            {
                                MessageBox.Show(
                                    GetLangMessage(
                                        "لقد تجاوزت حد الخصم",
                                        "You have exceeded your discount limit"),
                                    GetLangMessage("تنبيه", "Warning"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                item.ItemDiscountPerc = 0;
                                item.ItemDiscount = 0;
                            }
                        }
                        else
                        {
                            if (maxAmount > 0 &&
                                !User.PassDefDiscount &&
                                item.ItemDiscount > maxAmount)
                            {
                                MessageBox.Show(
                                    GetLangMessage(
                                        "لقد تجاوزت حد الخصم",
                                        "You have exceeded your discount limit"),
                                    GetLangMessage("تنبيه", "Warning"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                item.ItemDiscountPerc = 0;
                                item.ItemDiscount = 0;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static void IsValidPrice(ref InvoiceDGV invo,
            int itemId, int rowIndex)
        {
            foreach (InvoiceItem item in invo.InvoiceItems)
            {
                if (item.ItemId != itemId ||
                    item.ItemRowIndex != rowIndex) continue;

                try
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "select sale_price, low_sale_price, high_sale_price" +
                        $" from ItemPrices where ItemId={item.ItemId}",
                        MainClass.ConnObj()))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            double highPrice =
                                ToDouble(dt.Rows[0]["high_sale_price"]);
                            double lowPrice =
                                ToDouble(dt.Rows[0]["low_sale_price"]);
                            double salePrice =
                                ToDouble(dt.Rows[0]["sale_price"]);

                            if (highPrice > 0 &&
                                !User.PassHeighestSalePrice &&
                                item.ItemPrice > highPrice)
                                MessageBox.Show(
                                    "لقد أدخلت السعر أكبر من أعلى سعر",
                                    string.Empty,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);

                            if (lowPrice > 0 &&
                                !User.PassLowestSalePrice &&
                                item.ItemPrice < lowPrice)
                            {
                                MessageBox.Show(
                                    "لقد أدخلت السعر أقل من أدنى سعر",
                                    string.Empty,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                item.ItemPrice = salePrice;
                                break;
                            }
                        }
                    }
                }
                catch { }

                if (!User.BuyLessAvrgCost &&
                    item.ItemPrice < item.ItemCost)
                {
                    MessageBox.Show(
                        "لقد أدخلت سعر أقل من سعر التكلفة",
                        string.Empty,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    break;
                }
            }
        }

        public static void checkQtyLimi(ref InvoiceDGV invo, int itemId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select limit, ISNULL(MaxQtyLimit,0) as MaxLimit" +
                    $" from items where Id={itemId} and IS_Deleted=0",
                    MainClass.ConnObj()))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0) return;

                    double minLimit = ToDouble(dt.Rows[0]["limit"]);
                    double maxLimit = ToDouble(dt.Rows[0]["MaxLimit"]);

                    foreach (InvoiceItem item in invo.InvoiceItems)
                    {
                        if (item.ItemId != itemId) continue;

                        if (item.InvertoryImpact == 2 && invo.ProcType == 1)
                        {
                            double remaining =
                                item.ValiableInvertory - item.ItemPrimaryQnty;
                            if (minLimit >= remaining && minLimit != 0)
                                MessageBox.Show(
                                    "لقد وصلت الحد الأدنى للكمية لهذا الصنف",
                                    "تنبيه",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                        }
                        else if (item.InvertoryImpact == 1 && invo.ProcType == 1)
                        {
                            double total =
                                item.ValiableInvertory + item.ItemPrimaryQnty;
                            if (maxLimit <= total && maxLimit > 0)
                                MessageBox.Show(
                                    "لقد وصلت الحد الأعلى للكمية لهذا الصنف",
                                    "تنبيه",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch { }
        }

        public static void ISvalidQuantity(ref InvoiceDGV invo,
            int itemId, int rowIndex, bool saleByMinus)
        {
            foreach (InvoiceItem item in invo.InvoiceItems)
            {
                if (item.ItemId != itemId ||
                    item.ItemRowIndex != rowIndex) continue;

                bool isValid = true;
                double available = item.ValiableInvertory;
                item.ItemPrimaryQnty = item.ItemQuantity * item.UnitEquality;
                double enteredQty = item.ItemPrimaryQnty;

                foreach (InvoiceItem other in invo.InvoiceItems)
                {
                    if (other.ItemId == item.ItemId)
                    {
                        other.ItemPrimaryQnty =
                            other.ItemQuantity * other.UnitEquality;
                        available -= other.ItemPrimaryQnty;
                    }
                }

                if (!invo.ISNew)
                {
                    try
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(
                            $"select val from Inv_Sub" +
                            $" where ItemId={itemId}" +
                            $" and InvGlobalID='{invo.InvGlobalID}'",
                            MainClass.ConnObj()))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);
                            if (dt.Rows.Count > 0)
                                available += ToDouble(dt.Rows[0][0]);
                        }
                    }
                    catch { }

                    if (available < 0) isValid = false;
                }
                else if (available < 0 &&
                         !saleByMinus &&
                         item.ItemType != 5)
                {
                    isValid = false;
                    item.ItemPrimaryQnty = item.ValiableInvertory;
                    item.ItemQuantity = item.UnitEquality > 0
                        ? item.ItemPrimaryQnty / item.UnitEquality : 0;
                }

                if (!isValid && item.ItemType != 5)
                {
                    string msg = MainClass.Language == "en"
                        ? $"Quantity is greater than store balance" +
                          $"{Environment.NewLine}" +
                          $"total quantity: {enteredQty}" +
                          $"{Environment.NewLine}" +
                          $"store balance: {item.InvertoryName}" +
                          $" = {available + enteredQty}"
                        : $"الكمية المدخلة للصنف أكبر من رصيد المستودع" +
                          $"{Environment.NewLine}" +
                          $"الكمية الاجمالية المدخلة: {enteredQty}" +
                          $"{Environment.NewLine}" +
                          $"رصيد المستودع: {item.InvertoryName}" +
                          $" = {available + enteredQty}";

                    MessageBox.Show(msg, string.Empty,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region Barcode Search (مطابق 100% للكود الأصلي)

        public static void SearchForBarcode(string barcode,
            ref int itemId, ref int unitId,
            ref decimal itemPrice, ref decimal itemQuantity,
            bool isMultiText)
        {
            try
            {
                SqlConnection conn = MainClass.ConnObj();

                if (string.IsNullOrEmpty(barcode)) return;

                // بحث في ItemUnits
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Items.id, Items.group_id," +
                    " ItemUnits.unit, Items.tax," +
                    " Items.tax_group, Items.Wscale" +
                    " from Items, ItemUnits" +
                    " where Items.IS_Deleted=0" +
                    " and ItemUnits.ItemId=items.id" +
                    $" and ItemUnits.barcode=N'{barcode}'", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        itemId = ToInt(dt.Rows[0]["id"]);
                        unitId = ToInt(dt.Rows[0]["unit"]);
                        itemPrice = default;
                        itemQuantity = default;
                        return;
                    }
                }

                // بحث في Itembarcodes
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Itembarcodes.itemId" +
                    " from Items, Itembarcodes" +
                    " where Items.IS_Deleted=0" +
                    " and Itembarcodes.ItemId=items.id" +
                    $" and Itembarcodes.barcode=N'{barcode}'", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        itemId = ToInt(dt.Rows[0]["itemId"]);
                        unitId = 0;
                        itemPrice = default;
                        itemQuantity = default;
                        return;
                    }
                }

                // باركود ميزان (13 رقم)
                if (barcode.Length == 13)
                {
                    try
                    {
                        string shortBarcode = barcode.Substring(0, 7);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(
                            "Select Items.id, Items.group_id," +
                            " ItemUnits.sale as SalePrice," +
                            " ItemUnits.unit, Items.tax," +
                            " Items.tax_group, Items.Wscale" +
                            " from Items, ItemUnits" +
                            " where Items.IS_Deleted=0" +
                            " and ItemUnits.ItemId=items.id" +
                            $" and (ItemUnits.barcode=N'{shortBarcode}'" +
                            $" or Items.barcode=N'{shortBarcode}')", conn))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                if (dt.Rows[0]["Wscale"] != DBNull.Value)
                                {
                                    double wscale =
                                        ToDouble(dt.Rows[0]["Wscale"]);

                                    if (wscale == 1)
                                    {
                                        // وزن (كيلو)
                                        double weightVal =
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(7, 2))) +
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(9, 3))) /
                                            1000.0;

                                        itemId = ToInt(dt.Rows[0]["id"]);
                                        unitId = ToInt(dt.Rows[0]["unit"]);
                                        itemPrice = default;
                                        itemQuantity = new decimal(weightVal);
                                    }
                                    else if (wscale == 2)
                                    {
                                        // سعر مضمّن
                                        double priceVal =
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(7, 3))) +
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(10, 2))) /
                                            100.0;

                                        itemId = ToInt(dt.Rows[0]["id"]);
                                        unitId = ToInt(dt.Rows[0]["unit"]);
                                        itemPrice = new decimal(priceVal);
                                        itemQuantity = default;
                                    }
                                    else if (wscale == 3)
                                    {
                                        // سعر مضمّن مع حساب الكمية
                                        double priceVal =
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(7, 3))) +
                                            (int)Math.Round(
                                                ToDouble(
                                                    barcode.Substring(10, 2))) /
                                            100.0;

                                        itemId = ToInt(dt.Rows[0]["id"]);
                                        unitId = ToInt(dt.Rows[0]["unit"]);
                                        itemPrice =
                                            new decimal(Math.Round(priceVal, 2));

                                        decimal salePrice = 0;
                                        if (dt.Rows[0]["SalePrice"] != DBNull.Value)
                                            salePrice = new decimal(
                                                ToDouble(dt.Rows[0]["SalePrice"]));

                                        itemQuantity = salePrice > 0
                                            ? Math.Round(
                                                decimal.Divide(itemPrice, salePrice), 3)
                                            : default;

                                        IsPriceFromBarcode = true;
                                    }
                                }
                            }
                            else
                            {
                                // لم يوجد في الجهاز
                                if (!isMultiText)
                                    ShowAttentionMsg(
                                        "هذا الباركود غير موجود" +
                                        " ضمن بيانات البرنامج");
                            }
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // باركود عادي غير موجود
                if (!isMultiText)
                    ShowAttentionMsg(
                        "هذا الباركود غير موجود ضمن بيانات البرنامج");
            }
            catch { }
        }

        #endregion

        #region Discount Calculations

        public static double ItemAdditonalCos(double invTotal,
            double invAdditonalCost, double itemTotal,
            double itemPrimaryQnty)
        {
            if (invTotal == 0 || itemPrimaryQnty == 0) return 0;
            double result =
                invAdditonalCost * (itemTotal / invTotal) / itemPrimaryQnty;
            return double.IsInfinity(result) || double.IsNaN(result)
                ? 0 : result;
        }

        public static double ItemDiscountValue(decimal invSumPrice,
            decimal invDiscount, double itemTotal,
            decimal itemQnty, decimal itemDiscount)
        {
            try
            {
                double sumP = (double)invSumPrice;
                if (sumP == 0) return 0;
                double result =
                    (itemTotal - (double)itemDiscount -
                     (itemTotal - (double)itemDiscount) /
                     sumP * (double)invDiscount) / (double)itemQnty;
                return double.IsInfinity(result) || double.IsNaN(result)
                    ? 0 : result;
            }
            catch { return 0; }
        }

        public static double ItemDiscountRateFromInvDiscount(
            decimal invSumPrice, decimal invDiscount,
            double itemTotal, double priceIncVat, double vatPerc)
        {
            try
            {
                double sumP = (double)invSumPrice;
                if (sumP == 0) return 0;
                double result =
                    itemTotal * (double)invDiscount / sumP;
                return double.IsInfinity(result) || double.IsNaN(result)
                    ? 0 : result;
            }
            catch { return 0; }
        }

        #endregion

        #region Inventory / Deliveries

        public void GetInventoryOrder(ref InvoiceDGV inv)
        {
            DataTable dt =
                Inventory.CalcItemsStockLimits(inv.Branch, inv.Store);
            if (dt.Rows.Count == 0) return;

            foreach (DataRow row in dt.Rows)
            {
                double stock = ToDouble(row["ItemStock"]);
                double minLimit = ToDouble(row["MinQntyLimt"]);

                if (stock < 0 || (stock <= minLimit && minLimit > 0))
                {
                    try
                    {
                        GetItemByID(ToInt(row["ItemId"]), 0,
                            ref inv, false, string.Empty, string.Empty);
                    }
                    catch { }
                }
            }
        }

        public void SaveInvoiceItemDeliveries(
            List<ItemBatchDelivery> itemBatchDeliveries,
            bool isDeliveriedAll)
        {
            SqlConnection conn = MainClass.ConnObj();
            SqlTransaction transaction = null;
            string lastGlobalId = string.Empty;

            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                transaction = conn.BeginTransaction();

                foreach (ItemBatchDelivery delivery in itemBatchDeliveries)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "INSERT INTO [dbo].[ItemBatchDeliveries]" +
                        " ([BatchDelivIncId],[BatchDelivNo],[InvGlobalID]," +
                        " [ItemId],[ItemUnitId],[ItemQuantity]," +
                        " [BatchDeliveryDate],[InvertoryEmp]," +
                        " [RecipientID],[Note])" +
                        " VALUES(@BatchDelivIncId,@BatchDelivNo,@InvGlobalID," +
                        " @ItemId,@ItemUnitId,@ItemQuantity," +
                        " @BatchDeliveryDate,@InvertoryEmp," +
                        " @RecipientID,@Note)",
                        conn, transaction))
                    {
                        cmd.Parameters.Add("@BatchDelivIncId", SqlDbType.Int)
                            .Value = delivery.BatchDelivIncId;
                        cmd.Parameters.Add("@BatchDelivNo", SqlDbType.Int)
                            .Value = delivery.BatchDelivNo;
                        cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar)
                            .Value = delivery.InvGlobalID;
                        cmd.Parameters.Add("@ItemId", SqlDbType.Int)
                            .Value = delivery.ItemId;
                        cmd.Parameters.Add("@ItemUnitId", SqlDbType.Int)
                            .Value = delivery.ItemUnitId;
                        cmd.Parameters.Add("@ItemQuantity", SqlDbType.Float)
                            .Value = delivery.ItemQuantity;
                        cmd.Parameters.Add("@BatchDeliveryDate", SqlDbType.DateTime)
                            .Value = delivery.BatchDeliveryDate;
                        cmd.Parameters.Add("@InvertoryEmp", SqlDbType.Int)
                            .Value = delivery.InvertoryEmp;
                        cmd.Parameters.Add("@RecipientID", SqlDbType.Int)
                            .Value = delivery.RecipientID;
                        cmd.Parameters.Add("@Note", SqlDbType.NVarChar)
                            .Value = delivery.Note;
                        cmd.ExecuteNonQuery();
                        lastGlobalId = delivery.InvGlobalID;
                    }
                }

                if (isDeliveriedAll && !string.IsNullOrEmpty(lastGlobalId))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        $"update Inv set InvoiceStatus=3" +
                        $" where InvGlobalID=N'{lastGlobalId}'",
                        conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();

                MessageBox.Show("تم تسليم الكميات بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                transaction?.Rollback();

                string errMsg = MainClass.Language == "ar"
                    ? "خطأ أثناء تسليم الكميات" +
                      Environment.NewLine +
                      "تفاصيل الخطأ: " + ex.Message
                    : "error in saving" +
                      Environment.NewLine +
                      "Error details: " + ex.Message;

                MessageBox.Show(errMsg, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region Online Sync Methods

        public async void ReadProductsOnline()
        {
            try
            {
                ProductCRUD productCRUD = new ProductCRUD(Sync.APIUrl);
                List<Product> products = (List<Product>)await productCRUD
                    .GetProducts(Sync.ClientCode, Sync.BranchId,
                        Sync.BranchType, DateTime.MinValue);

                MessageBox.Show(
                    "عدد الأصناف المستوردة " + products.Count,
                    "معلومات",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                new EntityOperations().SaveProducts(products);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task<bool> ReadUnitsOnline()
        {
            SqlConnection conn = MainClass.ConnObj();
            SqlTransaction transaction = null;

            try
            {
                List<Unit> units = Sync.BranchType != 4
                    ? (List<Unit>)await new UnitCRUD(Sync.APIUrl)
                        .GetUnits(Sync.ClientCode, Sync.BranchId, Sync.BranchType)
                    : new ClsUnit(Sync.APIUrl, "").GetUnits();

                if (conn.State != ConnectionState.Open) conn.Open();
                transaction = conn.BeginTransaction();

                foreach (Unit unit in units)
                {
                    double count = ToDouble(new SqlCommand(
                        $"Select COUNT(*) from units where id={unit.UnitId}",
                        conn, transaction).ExecuteScalar());

                    if (count == 0)
                    {
                        new SqlCommand(
                            $"insert into units(id,name,defaultInv," +
                            $"IS_Deleted,UnitCode,UnitId)" +
                            $" values({unit.UnitId},N'{unit.Name}'," +
                            $"{unit.DefaultInv},0,N'',{unit.UnitId})",
                            conn, transaction).ExecuteNonQuery();
                    }
                    else
                    {
                        new SqlCommand(
                            $"update units set name=N'{unit.Name}'," +
                            $" defaultInv={unit.DefaultInv}" +
                            $" where id={unit.UnitId}",
                            conn, transaction).ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();

                string errMsg = MainClass.Language == "ar"
                    ? "خطأ أثناء استيراد الوحدات" +
                      Environment.NewLine +
                      "تفاصيل الخطأ: " + ex.Message
                    : "error in saving" +
                      Environment.NewLine +
                      "Error details: " + ex.Message;

                MessageBox.Show(errMsg, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        public async void ReadCategoriesOnline()
        {
            SqlConnection conn = MainClass.ConnObj();

            try
            {
                CategoryCRUD categoryCRUD = new CategoryCRUD(Sync.APIUrl);
                List<Category> categories =
                    (List<Category>)await categoryCRUD
                        .GetCategories(Sync.ClientCode,
                            Sync.BranchId, Sync.BranchType);

                if (conn.State != ConnectionState.Open) conn.Open();

                foreach (Category item in categories)
                {
                    if (item.ParentCode == null)
                        item.ParentCode = string.Empty;
                    if (item.Printer == null)
                        item.Printer = string.Empty;

                    double count = ToDouble(new SqlCommand(
                        $"Select COUNT(*) from itemsCategory" +
                        $" where id={item.CategoryId}",
                        conn).ExecuteScalar());

                    int typeVal = item.ISLeaf ? 2 : 1;

                    if (count == 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "insert into itemsCategory" +
                            "(id,CategoryId,name,nameEN,printer," +
                            "ShowInPOS,DispalyOrder,IS_Deleted," +
                            "code,parentCode,type,BranchId,AllBranch)" +
                            " values(@id,@CategoryId,@name,@nameEN," +
                            $"'{item.Printer}'," +
                            $"{(item.ShowInPOS ? 1 : 0)}," +
                            $"{item.DispalyOrder}," +
                            "0,@code,@parentCode,@type,@BranchId,@AllBranch)",
                            conn))
                        {
                            cmd.Parameters.Add("@id", SqlDbType.Int)
                                .Value = item.CategoryId;
                            cmd.Parameters.Add("@CategoryId", SqlDbType.Int)
                                .Value = item.CategoryId;
                            cmd.Parameters.Add("@name", SqlDbType.NVarChar)
                                .Value = item.Name;
                            cmd.Parameters.Add("@nameEN", SqlDbType.NVarChar)
                                .Value = item.NameEN;
                            cmd.Parameters.Add("@code", SqlDbType.NVarChar)
                                .Value = item.Code;
                            cmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar)
                                .Value = item.ParentCode;
                            cmd.Parameters.Add("@type", SqlDbType.Int)
                                .Value = typeVal;
                            cmd.Parameters.Add("@BranchId", SqlDbType.Int)
                                .Value = -1;
                            cmd.Parameters.Add("@AllBranch", SqlDbType.Int)
                                .Value = 1;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand(
                            "update itemsCategory set" +
                            " name=@name, nameEN=@nameEN," +
                            $" printer='{item.Printer}'," +
                            $" ShowInPOS={(item.ShowInPOS ? 1 : 0)}," +
                            $" DispalyOrder={item.DispalyOrder}," +
                            " IS_Deleted=0," +
                            " code=@code," +
                            " ParentCode=@ParentCode," +
                            " type=@type," +
                            " BranchId=@BranchId," +
                            " AllBranch=@AllBranch" +
                            $" where id={item.CategoryId}", conn))
                        {
                            cmd.Parameters.Add("@name", SqlDbType.NVarChar)
                                .Value = item.Name;
                            cmd.Parameters.Add("@nameEN", SqlDbType.NVarChar)
                                .Value = item.NameEN;
                            cmd.Parameters.Add("@code", SqlDbType.NVarChar)
                                .Value = item.Code;
                            cmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar)
                                .Value = item.ParentCode;
                            cmd.Parameters.Add("@type", SqlDbType.Int)
                                .Value = typeVal;
                            cmd.Parameters.Add("@BranchId", SqlDbType.Int)
                                .Value = -1;
                            cmd.Parameters.Add("@AllBranch", SqlDbType.Int)
                                .Value = 1;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show(
                    "تم تحديث بيانات المجموعات بنجاح",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errMsg = MainClass.Language == "ar"
                    ? "خطأ أثناء استيراد المجموعات" +
                      Environment.NewLine +
                      "تفاصيل الخطأ: " + ex.Message
                    : "error in saving" +
                      Environment.NewLine +
                      "Error details: " + ex.Message;

                MessageBox.Show(errMsg, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) conn.Close();
            }
        }

        #endregion

        #region Bind Methods

        public List<Product> BindProduct(List<int> itemList)
        {
            List<Product> list = new List<Product>();
            SqlConnection conn = MainClass.ConnObj();
            SqlConnection conn2 = MainClass.ConnObj();

            foreach (int id in itemList)
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select * from items where id={id}", conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        Product product = new Product();
                        product.ClientCode = Sync.ClientCode;
                        product.ProductId = ToInt(row["Id"]);
                        product.Code = ToStr(row["code"]);
                        product.Name = ToStr(row["name"]);
                        product.NameEN = ToStr(row["nameEN"]);
                        product.Category = ToStr(row["Grpcode"]);
                        product.CategoryID = ToStr(row["group_id"]);

                        if (row["ItemProperty"] != DBNull.Value)
                            product.Property =
                                (ProductProperty)ToInt(row["ItemProperty"]);

                        if (row["ItemType"] != DBNull.Value)
                            product.Type =
                                (ProductTypes)ToInt(row["ItemType"]);

                        product.MainUnit = ToInt(row["unit"]);
                        product.MainBarcode = ToStr(row["barcode"]);
                        product.ShowInPOS = ToBool(row["ShowInPOS"]);
                        product.PurchPrice = ToDouble(row["purch_price"]);
                        product.SalePrice = ToDouble(row["sale_price"]);
                        product.limitStock = ToInt(row["limit"]);
                        product.VAT = ToDouble(row["tax"]);
                        product.CreateDate = DateTime.Now;
                        product.LastUpdateDate = DateTime.Now;
                        product.Branch = MainClass.BranchNo;
                        product.DistBranch = Sync.DistBranch;
                        product.BranchType = Sync.BranchType;
                        product.Wscale = ToInt(row["Wscale"]);
                        product.Active = true;

                        if (row["FillValue"] != DBNull.Value)
                            product.PackingValue = ToDouble(row["FillValue"]);

                        if (row["MaxQtyLimit"] != DBNull.Value)
                            product.MaxQtyLimit = ToDouble(row["MaxQtyLimit"]);

                        // Units
                        using (SqlDataAdapter unitAdapter = new SqlDataAdapter(
                            "select ItemUnits.unit, ItemUnits.perc," +
                            " ItemUnits.purch, ItemUnits.sale," +
                            " ItemUnits.barcode, Units.name" +
                            " from ItemUnits" +
                            " left join units on ItemUnits.unit=units.id" +
                            $" where ItemId={product.ProductId}", conn2))
                        {
                            DataTable unitDt = new DataTable();
                            unitAdapter.Fill(unitDt);
                            foreach (DataRow uRow in unitDt.Rows)
                            {
                                product.ProductUnits.Add(new ProductUnit
                                {
                                    ProductId = product.ProductId,
                                    UnitId = ToInt(uRow["unit"]),
                                    UnitName = ToStr(uRow["name"]),
                                    UnitEquality = ToDouble(uRow["perc"]),
                                    PurchasePrice = ToDouble(uRow["purch"]),
                                    SalePrice = ToDouble(uRow["sale"]),
                                    Barcode = ToStr(uRow["barcode"]),
                                    ClientCode = Sync.ClientCode
                                });
                            }
                        }

                        // Components
                        using (SqlDataAdapter compAdapter = new SqlDataAdapter(
                            "Select ItemComponents.Id," +
                            " ItemComponents.ComponentId," +
                            " ItemComponents.store," +
                            " ItemComponents.itemId," +
                            " ItemComponents.quantity," +
                            " ItemComponents.price," +
                            " ItemComponents.total," +
                            " ItemComponents.type," +
                            " ItemComponents.unit as unit_id" +
                            " From ItemComponents" +
                            $" where itemId={product.ProductId}", conn2))
                        {
                            DataTable compDt = new DataTable();
                            compAdapter.Fill(compDt);
                            foreach (DataRow cRow in compDt.Rows)
                            {
                                if (ToInt(cRow["ComponentId"]) <= 0) continue;
                                product.ProductComponents.Add(
                                    new ProductComponent
                                    {
                                        ProductId = product.ProductId,
                                        UnitId = ToInt(cRow["unit_id"]),
                                        ComponentId = ToInt(cRow["ComponentId"]),
                                        ComponentCost = ToFloat(cRow["price"]),
                                        Quantity = ToStr(cRow["quantity"]),
                                        InvertoryId = ToInt(cRow["store"]),
                                        ComponentType = ToInt(cRow["type"]),
                                        ClientCode = Sync.ClientCode
                                    });
                            }
                        }

                        // Barcodes
                        using (SqlDataAdapter bcAdapter = new SqlDataAdapter(
                            "select * from Itembarcodes" +
                            $" where ItemId={product.ProductId}", conn))
                        {
                            DataTable bcDt = new DataTable();
                            bcAdapter.Fill(bcDt);
                            foreach (DataRow bRow in bcDt.Rows)
                            {
                                product.ProductBarcodes.Add(
                                    new ProductBarcode
                                    {
                                        Barcode = ToStr(bRow["barcode"]),
                                        ProductId = ToInt(bRow["ItemId"]),
                                        UnitId = 1,
                                        ClientCode = Sync.ClientCode
                                    });
                            }
                        }

                        list.Add(product);
                    }
                }
            }

            return list;
        }

        public List<Product> BindingProducts(int offset)
        {
            List<Product> list = new List<Product>();
            SqlConnection conn = MainClass.ConnObj();

            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select items.id as itemId," +
                " Items.code as ItemCode," +
                " Items.name as ItemName," +
                " Items.nameEn as ItemNameEn," +
                " Items.ItemType," +
                " items.unit as MainUnit," +
                " items.barcode as MainBarcode," +
                " Items.ItemProperty," +
                " items.ShowInPOS," +
                " Items.FillValue," +
                " items.discount," +
                " items.tax," +
                " items.limit," +
                " items.sale_price," +
                " Items.purch_price," +
                " items.CreatedDate," +
                " items.updatedDate," +
                " ItemsCategory.name as Categoryname," +
                " ItemsCategory.Id as CategoryId," +
                " ItemPrices.low_sale_price," +
                " ItemPrices.high_sale_price," +
                " ItemPrices.CompetitorPrice," +
                " isnull(ItemPrices.WholesalePrice,0) as WholesalePrice," +
                " isnull(ItemPrices.ConsumerPrice,0) as ConsumerPrice" +
                " from Items" +
                " left join ItemsCategory" +
                " on Items.group_id=ItemsCategory.id" +
                " LEFT JOIN ItemPrices ON Items.id=ItemPrices.ItemID" +
                $" order by Items.id" +
                $" OFFSET {offset} ROWS FETCH NEXT 100 ROWS ONLY", conn))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    if (row["CategoryName"] == DBNull.Value) continue;

                    Product product = new Product();
                    product.ClientCode = Sync.ClientCode;
                    product.ProductId = ToInt(row["itemId"]);
                    product.Code = ToStr(row["ItemCode"]);
                    product.Name = ToStr(row["ItemName"]);
                    product.NameEN = ToStr(row["ItemNameEn"]);
                    product.Category = ToStr(row["Categoryname"]);
                    product.CategoryID = ToStr(row["CategoryId"]);

                    if (row["ItemProperty"] != DBNull.Value)
                        product.Property =
                            (ProductProperty)ToInt(row["ItemProperty"]);

                    if (row["ItemType"] != DBNull.Value)
                        product.Type =
                            (ProductTypes)ToInt(row["ItemType"]);

                    product.MainUnit = ToInt(row["MainUnit"]);
                    product.MainBarcode = ToStr(row["MainBarcode"]);
                    product.ShowInPOS = ToBool(row["ShowInPOS"]);
                    product.PurchPrice = ToDouble(row["purch_price"]);
                    product.SalePrice = ToDouble(row["sale_price"]);
                    product.limitStock = ToInt(row["limit"]);
                    product.VAT = ToDouble(row["tax"]);
                    product.Branch = MainClass.BranchNo;
                    product.DistBranch = Sync.DistBranch;
                    product.BranchType = Sync.BranchType;
                    product.Active = true;

                    if (row["FillValue"] != DBNull.Value)
                        product.PackingValue = ToDouble(row["FillValue"]);

                    product.CreateDate = row["CreatedDate"] == DBNull.Value
                        ? DateTime.Now : Convert.ToDateTime(row["CreatedDate"]);
                    product.LastUpdateDate = row["updatedDate"] == DBNull.Value
                        ? DateTime.Now : Convert.ToDateTime(row["updatedDate"]);

                    product.productPrices = new ProductPrices
                    {
                        ProductId = product.ProductId,
                        ClientCode = Sync.ClientCode,
                        UnitId = product.MainUnit,
                        date = DateTime.Now.Date,
                        SalePrice = product.SalePrice,
                        PurchasePrice = product.PurchPrice,
                        LowPurchasePrice = 0,
                        HighPurchasePrice = 0,
                        LowSalePrice = row["low_sale_price"] != DBNull.Value
                            ? ToDouble(row["low_sale_price"]) : 0,
                        HighSalePrice = product.SalePrice,
                        CompetitorPrice = row["CompetitorPrice"] != DBNull.Value
                            ? ToDouble(row["CompetitorPrice"]) : 0,
                        ISDeleted = false,
                        ConsumerPrice = ToDouble(row["ConsumerPrice"]),
                        WholesalePrice = ToDouble(row["WholesalePrice"])
                    };

                    // Units
                    using (SqlDataAdapter unitAdapter = new SqlDataAdapter(
                        "select ItemUnits.unit, ItemUnits.perc," +
                        " ItemUnits.purch, ItemUnits.sale," +
                        " ItemUnits.barcode, Units.name" +
                        " from ItemUnits" +
                        " left join units on ItemUnits.unit=units.id" +
                        $" where ItemId={product.ProductId}", conn))
                    {
                        DataTable unitDt = new DataTable();
                        unitAdapter.Fill(unitDt);
                        foreach (DataRow uRow in unitDt.Rows)
                        {
                            product.ProductUnits.Add(new ProductUnit
                            {
                                ProductId = product.ProductId,
                                UnitId = ToInt(uRow["unit"]),
                                UnitName = ToStr(uRow["name"]),
                                UnitEquality = ToDouble(uRow["perc"]),
                                PurchasePrice = ToDouble(uRow["purch"]),
                                SalePrice = ToDouble(uRow["sale"]),
                                Barcode = ToStr(uRow["barcode"]),
                                ClientCode = Sync.ClientCode
                            });
                        }
                    }

                    // Components
                    using (SqlDataAdapter compAdapter = new SqlDataAdapter(
                        "Select ItemComponents.Id," +
                        " ItemComponents.ComponentId," +
                        " ItemComponents.store," +
                        " ItemComponents.itemId," +
                        " ItemComponents.quantity," +
                        " ItemComponents.price," +
                        " ItemComponents.total," +
                        " ItemComponents.type," +
                        " ItemComponents.unit as unit_id" +
                        " From ItemComponents" +
                        $" where itemId={product.ProductId}", conn))
                    {
                        DataTable compDt = new DataTable();
                        compAdapter.Fill(compDt);
                        foreach (DataRow cRow in compDt.Rows)
                        {
                            if (ToInt(cRow["ComponentId"]) <= 0) continue;
                            product.ProductComponents.Add(
                                new ProductComponent
                                {
                                    ProductId = product.ProductId,
                                    UnitId = ToInt(cRow["unit_id"]),
                                    ComponentId = ToInt(cRow["ComponentId"]),
                                    ComponentCost = ToFloat(cRow["price"]),
                                    Quantity = ToStr(cRow["quantity"]),
                                    InvertoryId = ToInt(cRow["store"]),
                                    ComponentType = ToInt(cRow["type"]),
                                    ClientCode = Sync.ClientCode
                                });
                        }
                    }

                    list.Add(product);
                }
            }

            return list;
        }

        public List<Category> BindToCategory()
        {
            List<Category> list = new List<Category>();
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select CategoryId, Code, Name, NameEn," +
                " ParentCode, printer, ShowInPOS, type," +
                " DispalyOrder," +
                " isnull(AllBranch,1) as AllBranch," +
                " isnull(BranchId,-1) as BranchId" +
                " from ItemsCategory", MainClass.ConnObj()))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new Category
                    {
                        ClientCode = Sync.ClientCode,
                        CategoryId = ToInt(row["CategoryId"]),
                        Code = ToStr(row["Code"]),
                        Name = ToStr(row["Name"]),
                        NameEN = ToStr(row["NameEn"]),
                        ParentCode = ToStr(row["ParentCode"]),
                        Printer = ToStr(row["printer"]),
                        ShowInPOS = ToBool(row["ShowInPOS"]),
                        ISLeaf = ToDouble(row["type"]) == 2,
                        Active = true,
                        DispalyOrder = ToInt(row["DispalyOrder"]),
                        AllBranch = ToBool(row["AllBranch"]),
                        BranchId = ToInt(row["BranchId"])
                    });
                }
            }
            return list;
        }

        public List<Unit> BindToUnit()
        {
            List<Unit> list = new List<Unit>();
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select * from units where IS_Deleted=0",
                MainClass.ConnObj()))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new Unit
                    {
                        ClientCode = Sync.ClientCode,
                        UnitId = ToInt(row["id"]),
                        Name = ToStr(row["name"]),
                        NameEn = ToStr(row["name"]),
                        DefaultInv = ToInt(row["defaultInv"])
                    });
                }
            }
            return list;
        }

        public List<Userapi> BindToUser()
        {
            List<Userapi> list = new List<Userapi>();
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select * from Users where IS_Deleted=0 and id>0",
                MainClass.ConnObj()))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new Userapi
                    {
                        ClientCode = Sync.ClientCode,
                        Id = ToInt(row["id"]),
                        emp = ToInt(row["emp"]),
                        UserName = ToStr(row["UserName"]),
                        LoginSecode = ToBool(row["LoginSecode"]),
                        IS_Deleted = ToBool(row["IS_Deleted"]),
                        pwd = ToStr(row["Pwd"]),
                        SecureCode = ToInt(row["SecureCode"]),
                        DeviceId = string.Empty,
                        DeviceName = string.Empty
                    });
                }
            }
            return list;
        }

        public List<SettingGeneralapi> BindToSettingGeneral()
        {
            List<SettingGeneralapi> list = new List<SettingGeneralapi>();
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "select * from SettingGeneral where Inv_Id=3",
                MainClass.ConnObj()))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new SettingGeneralapi
                    {
                        ClientCode = Sync.ClientCode,
                        Inv_Id = ToInt(row["Inv_Id"]),
                        PriceIncVAT = ToBool(row["PriceIncVAT"]),
                        MainVAT = ToDecimal(row["MainVAT"]),
                        DigitsNo = ToInt(row["DigitsNo"]),
                        InvoiceCode = ToStr(row["InvoiceCode"]),
                        SaleByMinus = ToBool(row["SaleByMinus"])
                    });
                }
            }
            return list;
        }

        public Foundationapi BindToFoundation()
        {
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT TOP 1 * FROM Foundation", MainClass.ConnObj()))
            {
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count == 0) return null;

                DataRow row = dt.Rows[0];
                Foundationapi foundation = new Foundationapi
                {
                    ClientCode = Sync.ClientCode,
                    FoundationName = row["nameA"].ToString(),
                    FieldAr = row["FieldA"].ToString(),
                    FieldEn = row["FieldE"].ToString(),
                    Address = row["Address"].ToString(),
                    Mobile = row["Mobile"].ToString(),
                    City = row["city"].ToString(),
                    BnNo = row["bsn_no"].ToString(),
                    VATNo = row["tax_no"].ToString(),
                    Tel = row["Tel"].ToString(),
                    Email = row["Email"].ToString(),
                    Country = row["Country"].ToString(),
                    Logo = row["Logo"].ToString(),
                    Area = row["area"].ToString(),
                    StreetName = row["StreetName"].ToString(),
                    PostalZone = row["PostalZone"].ToString(),
                    BuildingNumber = row["BuildingNumber"].ToString(),
                    District = row["District"].ToString(),
                    PlotIdentification = row["PlotIdentification"].ToString(),
                    AdditionalStreetName = row["AdditionalStreetName"].ToString(),
                    FoundationNameEn = row["nameE"].ToString(),
                    Status = Common.GetZatcaActive(),
                    IsDefault = false,
                    IsDeleted = false
                };

                if (string.IsNullOrEmpty(foundation.Email))
                    foundation.Email = "default@company.com";

                if (string.IsNullOrEmpty(foundation.Country))
                    foundation.Country = "SA";

                if (string.IsNullOrEmpty(foundation.Logo))
                    foundation.Logo =
                        "data:image/png;base64," +
                        "AAABAAEAEBAAAAAAIABoBAAAFgAAACgAAAAQAAAAIAAAAAEAGAAA" +
                        "AAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

                return foundation;
            }
        }

        #endregion
    }
}