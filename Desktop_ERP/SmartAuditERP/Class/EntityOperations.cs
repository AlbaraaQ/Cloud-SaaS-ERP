using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{ 
public class EntityOperations
{
    public bool SaveBranch(List<Branch> branches)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (Branch branch in branches)
            {
                if (branch == null)
                {
                    continue;
                }
                SqlCommand sqlCommand;
                if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Branches where BranchId= " + Conversions.ToString(branch.BranchId), sqlConnection).ExecuteScalar())) == 0.0)
                {
                    if (branch.BranchId < 1)
                    {
                        branch.BranchId = checked((int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", new SqlCommand("select max(BranchId) from Branches is_deleted=0", sqlConnection).ExecuteScalar())) + 1.0));
                    }
                    sqlCommand = new SqlCommand("insert into Branches(BranchId,name,tel,mobile,fax,email,address,notes,is_deleted,IsDefault,code,CustomersAcc,SupliersAcc,BanksAcc,InventoryAcc,TreasuriesAcc,IsActive,CostCenter,AppAcc,EmployeeAcc) values (@BranchId,@name,@tel,@mobile,@fax,@email,@address,@notes,@is_deleted,@IsDefault,@code,@CustomersAcc,@SupliersAcc,@BanksAcc,@InventoryAcc,@TreasuriesAcc,@IsActive,@CostCenter,@AppAcc,@EmployeeAcc)", sqlConnection);
                }
                else
                {
                    sqlCommand = new SqlCommand("update Branches set  name=@name ,tel=@tel ,mobile=@mobile ,fax=@fax ,email=@email ,address=@address,notes=@notes,IsDefault=@IsDefault,CustomersAcc=@CustomersAcc,SupliersAcc=@SupliersAcc,BanksAcc=@BanksAcc,InventoryAcc=@InventoryAcc,TreasuriesAcc=@TreasuriesAcc,IsActive=@IsActive,CostCenter=@CostCenter,AppAcc= @AppAcc , EmployeeAcc=@EmployeeAcc where BranchId=" + Conversions.ToString(branch.BranchId), sqlConnection);
                }
                if (branch.Code == null)
                {
                    branch.Code = Conversions.ToString(branch.BranchId);
                }
                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = branch.BranchId;
                sqlCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = branch.BranchId;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = branch.BranchName;
                sqlCommand.Parameters.Add("@code", SqlDbType.NVarChar).Value = branch.Code;
                sqlCommand.Parameters.Add("@tel", SqlDbType.NVarChar).Value = branch.BranchTel;
                sqlCommand.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = branch.BranchMobile;
                sqlCommand.Parameters.Add("@fax", SqlDbType.NVarChar).Value = 0;
                sqlCommand.Parameters.Add("@email", SqlDbType.NVarChar).Value = branch.BranchEmail;
                sqlCommand.Parameters.Add("@address", SqlDbType.NVarChar).Value = branch.BranchAddress;
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = branch.Notes;
                sqlCommand.Parameters.Add("@is_deleted", SqlDbType.Bit).Value = Convert.ToInt16(branch.IsDeleted);
                sqlCommand.Parameters.Add("@IsDefault", SqlDbType.Bit).Value = Convert.ToInt16(branch.IsDefault);
                sqlCommand.Parameters.Add("@CustomersAcc", SqlDbType.NVarChar).Value = branch.CustomersAcc;
                sqlCommand.Parameters.Add("@SupliersAcc", SqlDbType.NVarChar).Value = branch.SupliersAcc;
                sqlCommand.Parameters.Add("@BanksAcc", SqlDbType.NVarChar).Value = branch.BanksAcc;
                sqlCommand.Parameters.Add("@InventoryAcc", SqlDbType.NVarChar).Value = branch.InventoryAcc;
                sqlCommand.Parameters.Add("@TreasuriesAcc", SqlDbType.NVarChar).Value = branch.TreasuriesAcc;
                sqlCommand.Parameters.Add("@AppAcc", SqlDbType.NVarChar).Value = branch.AppAccount;
                sqlCommand.Parameters.Add("@EmployeeAcc", SqlDbType.NVarChar).Value = branch.EmployeeAcc;
                sqlCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = branch.IsActive;
                if ((branch.CostCenter != null) & ((object)branch.CostCenter != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@CostCenter", SqlDbType.NVarChar).Value = branch.CostCenter;
                }
                else
                {
                    sqlCommand.Parameters.Add("@CostCenter", SqlDbType.NVarChar).Value = "-1";
                }
                sqlCommand.ExecuteNonQuery();
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<Branch> ReadBranch()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<Branch> list;
        list = new List<Branch>();
        checked
        {
            List<Branch> result;
            try
            {
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("select isnull(BranchId,1)as BranchId,name,tel,mobile,fax,email,address,notes,isNull(is_deleted,0)as is_deleted,IsNull(IsDefault,1)as IsDefault,code,CustomersAcc,SupliersAcc,BanksAcc,InventoryAcc,TreasuriesAcc,IsNull(IsActive,0)as IsActive, AppAcc,IsNull(EmployeeAcc,'')as EmployeeAcc from Branches order by id", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    Branch branch;
                    branch = new Branch();
                    branch.ClientCode = Sync.ClientCode;
                    branch.Code = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["code"]));
                    branch.BranchId = Conversions.ToInteger(dataTable.Rows[i]["BranchId"]);
                    branch.BranchName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["name"]));
                    branch.BranchTel = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["tel"]));
                    branch.BranchMobile = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["mobile"]));
                    branch.BranchEmail = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["email"]));
                    branch.BranchAddress = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["address"]));
                    branch.Notes = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
                    branch.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["is_deleted"]));
                    branch.IsDefault = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["IsDefault"]));
                    branch.CreateDate = DateTime.Now;
                    branch.LastUpdateDate = DateTime.Now;
                    branch.CustomersAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["CustomersAcc"]));
                    branch.SupliersAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["SupliersAcc"]));
                    branch.BanksAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["BanksAcc"]));
                    branch.InventoryAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["InventoryAcc"]));
                    branch.TreasuriesAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["TreasuriesAcc"]));
                    branch.AppAccount = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["AppAcc"]));
                    branch.IsActive = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["IsActive"]));
                    branch.EmployeeAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["EmployeeAcc"]));
                    SqlDataAdapter sqlDataAdapter2;
                    sqlDataAdapter2 = new SqlDataAdapter("select * from EmpBranches where branch=" + Conversions.ToString(branch.BranchId), sqlConnection);
                    DataTable dataTable2;
                    dataTable2 = new DataTable();
                    sqlDataAdapter2.Fill(dataTable2);
                    int num2;
                    num2 = dataTable2.Rows.Count - 1;
                    for (int j = 0; j <= num2; j++)
                    {
                        BranchEmployee branchEmployee;
                        branchEmployee = new BranchEmployee();
                        branchEmployee.BranchId = branch.BranchId;
                        branchEmployee.ClientCode = branch.ClientCode;
                        branchEmployee.EmployeeId = Conversions.ToString(dataTable2.Rows[j]["emp"]);
                        branch.Employees.Add(branchEmployee);
                    }
                    list.Add(branch);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public bool SavePriceType(List<PricingType> priceTypes)
    {
        bool result;
        try
        {
            SqlConnection sqlConnection;
            sqlConnection = MainClass.ConnObj();
            using (sqlConnection)
            {
                sqlConnection.Open();
                foreach (PricingType priceType in priceTypes)
                {
                    SqlCommand sqlCommand;
                    sqlCommand = new SqlCommand("SELECT COUNT(*) FROM [dbo].[priceTypes] WHERE [TypeId] = @TypeId", sqlConnection);
                    sqlCommand.Parameters.Add("@TypeId", SqlDbType.Int).Value = priceType.TypeId;
                    sqlCommand = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) != 0) ? new SqlCommand("UPDATE [dbo].[priceTypes] SET [TypeName] = @TypeName WHERE [TypeId] = @TypeId", sqlConnection) : new SqlCommand("INSERT INTO [dbo].[priceTypes] ([TypeId], [TypeName]) VALUES (@TypeId, @TypeName)", sqlConnection));
                    sqlCommand.Parameters.AddWithValue("@TypeId", priceType.TypeId);
                    sqlCommand.Parameters.AddWithValue("@TypeName", priceType.TypeName);
                    sqlCommand.ExecuteNonQuery();
                }
                result = true;
            }
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        return result;
    }

    public bool SaveOffers(List<Offer> Offers)
    {
        bool result;
        try
        {
            SqlConnection sqlConnection;
            sqlConnection = MainClass.ConnObj();
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (Offer Offer in Offers)
            {
                SqlCommand sqlCommand;
                sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Offer where OfferID=" + Conversions.ToString(Offer.OfferID), sqlConnection).ExecuteScalar())) != 0.0) ? new SqlCommand("update  [dbo].[Offer] set [OfferType]=@OfferType,[OfferStartDate]=@OfferStartDate,[OfferExpire]=@OfferExpire,[OfferName]=@OfferName,[OfferDate]=@OfferDate,[OfferAccount]=@OfferAccount,[InvoiceID]=@InvoiceID,[OfferValue]=@OfferValue,[OfferPercentage]=@OfferPercentage,[OfferValueTarget]=@OfferValueTarget,[OfferQntyTarget]=@OfferQntyTarget,[EmpId]=@EmpId where [OfferID]=@OfferID ", sqlConnection) : new SqlCommand("INSERT INTO [dbo].[Offer]([OfferID],[OfferType],[OfferStartDate],[OfferExpire],[OfferName],[OfferDate],[OfferAccount],[InvoiceID],[OfferValue],[OfferPercentage],[OfferValueTarget],[OfferQntyTarget],[EmpId],[ISDeleted]) VALUES(@OfferID,@OfferType,@OfferStartDate,@OfferExpire,@OfferName,@OfferDate,@OfferAccount,@InvoiceID,@OfferValue,@OfferPercentage,@OfferValueTarget,@OfferQntyTarget,@EmpId,@ISDeleted)", sqlConnection));
                sqlCommand.Parameters.Add("@OfferID", SqlDbType.Int).Value = Offer.OfferID;
                sqlCommand.Parameters.Add("@OfferType", SqlDbType.Int).Value = Offer.OfferType;
                sqlCommand.Parameters.Add("@OfferStartDate", SqlDbType.DateTime).Value = Offer.OfferStartDate;
                sqlCommand.Parameters.Add("@OfferExpire", SqlDbType.DateTime).Value = Offer.OfferExpire;
                sqlCommand.Parameters.Add("@OfferName", SqlDbType.NVarChar).Value = Offer.OfferName;
                sqlCommand.Parameters.Add("@OfferDate", SqlDbType.DateTime).Value = Offer.OfferStartDate;
                sqlCommand.Parameters.Add("@OfferAccount", SqlDbType.NVarChar).Value = Offer.OfferAccount;
                sqlCommand.Parameters.Add("@InvoiceID", SqlDbType.NVarChar).Value = Offer.InvoiceID;
                sqlCommand.Parameters.Add("@OfferValue", SqlDbType.Float).Value = Offer.OfferValue;
                sqlCommand.Parameters.Add("@OfferPercentage", SqlDbType.Float).Value = Offer.OfferPercentage;
                sqlCommand.Parameters.Add("@OfferValueTarget", SqlDbType.Float).Value = Offer.OfferValueTarget;
                sqlCommand.Parameters.Add("@OfferQntyTarget", SqlDbType.Float).Value = Offer.OfferQntyTarget;
                sqlCommand.Parameters.Add("@EmpId", SqlDbType.Int).Value = MainClass.EmpNo;
                sqlCommand.Parameters.Add("@ISDeleted", SqlDbType.Bit).Value = Offer.IsDeleted;
                sqlCommand.ExecuteNonQuery();
                new SqlCommand("delete from OfferItems where OfferId= " + Conversions.ToString(Offer.OfferID), sqlConnection).ExecuteNonQuery();
                foreach (OfferItem offerItem in Offer.OfferItems)
                {
                    sqlCommand = new SqlCommand("INSERT INTO [dbo].[OfferItems]\r\n                                            ([OfferId],[CatatogryID],[ItemID],[OfferNatural],[IsGroupedItems],[unit],[OfferTargetQnty],[OfferItemTargetQnty],[OfferItemPrice],[OfferTotalPrice],[OfferItemValue],[OfferItemPercentage],[OfferItemStock])\r\n                                            VALUES(@OfferId,@CatatogryID, @ItemID, @OfferNatural,@IsGroupedItems,@unit, @OfferTargetQnty,@OfferItemTargetQnty, @OfferItemPrice, @OfferTotalPrice, @OfferItemValue,@OfferItemPercentage, @OfferItemStock)", sqlConnection);
                    sqlCommand.Parameters.Add("@OfferId", SqlDbType.Int).Value = Offer.OfferID;
                    sqlCommand.Parameters.Add("@CatatogryID", SqlDbType.Int).Value = Common.GetItemCategoryID(offerItem.ItemID);
                    sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = offerItem.ItemID;
                    sqlCommand.Parameters.Add("@OfferNatural", SqlDbType.Int).Value = offerItem.OfferNatural;
                    sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = Common.GetUnitID(Conversions.ToString(offerItem.ItemID));
                    sqlCommand.Parameters.Add("@IsGroupedItems", SqlDbType.Bit).Value = offerItem.IsGroupedItems;
                    sqlCommand.Parameters.Add("@OfferTargetQnty", SqlDbType.Float).Value = offerItem.OfferTargetQnty;
                    sqlCommand.Parameters.Add("@OfferItemTargetQnty", SqlDbType.Float).Value = offerItem.OfferItemTargetQnty;
                    sqlCommand.Parameters.Add("@OfferItemPrice", SqlDbType.Float).Value = Common.GetItemPrice(offerItem.ItemID);
                    sqlCommand.Parameters.Add("@OfferTotalPrice", SqlDbType.Float).Value = Common.GetItemPrice(offerItem.ItemID) * (double)offerItem.OfferItemTargetQnty;
                    sqlCommand.Parameters.Add("@OfferItemValue", SqlDbType.Float).Value = offerItem.OfferItemValue;
                    sqlCommand.Parameters.Add("@OfferItemPercentage", SqlDbType.Float).Value = offerItem.OfferItemPercentage;
                    sqlCommand.Parameters.Add("@OfferItemStock", SqlDbType.Float).Value = offerItem.OfferItemStock;
                    sqlCommand.ExecuteNonQuery();
                }
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        return result;
    }

    public bool SaveCloudPayment(List<PaymentType> PaymentTypes)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            new SqlCommand("Delete from CloudPayment ", sqlConnection).ExecuteNonQuery();
            foreach (PaymentType PaymentType in PaymentTypes)
            {
                SqlCommand sqlCommand;
                sqlCommand = new SqlCommand("Insert into CloudPayment(Name,CloudID) values (@name,@CloudID)", sqlConnection);
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = PaymentType.Name;
                sqlCommand.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = PaymentType.CloudPaytypeID;
                sqlCommand.ExecuteNonQuery();
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        return result;
    }

    public bool SaveInvertory(List<Invertory> Invertories, bool Addlocally)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (Invertory Invertory in Invertories)
            {
                SqlCommand sqlCommand;
                if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Safes where id= " + Conversions.ToString(Invertory.InvertoryId), sqlConnection).ExecuteScalar())) == 0.0)
                {
                    sqlCommand = new SqlCommand("SET IDENTITY_INSERT [dbo].[Safes] ON insert into Safes(id,name,branch,status,IS_Default,notes,IS_Deleted)values(@id,@name,@branch,@status,@IS_Default,@notes,@IS_Deleted)SET IDENTITY_INSERT [dbo].[Safes] OFF", sqlConnection);
                }
                else
                {
                    if (Addlocally)
                    {
                        new SqlCommand("delete from Safe_Emps where safe_id=" + Conversions.ToString(Invertory.InvertoryId), sqlConnection).ExecuteNonQuery();
                    }
                    sqlCommand = new SqlCommand("update Safes set name=@name,branch=@branch,status=@status,IS_Default=@IS_Default,notes=@notes,IS_Deleted=@IS_Deleted where id=" + Conversions.ToString(Invertory.InvertoryId), sqlConnection);
                }
                sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = Invertory.InvertoryId;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = Invertory.InvertoryName;
                sqlCommand.Parameters.Add("@branch", SqlDbType.Int).Value = Invertory.BranchId;
                sqlCommand.Parameters.Add("@status", SqlDbType.Int).Value = Convert.ToInt16(Invertory.Status);
                sqlCommand.Parameters.Add("@IS_Default", SqlDbType.Bit).Value = Convert.ToInt16(Invertory.IsDefault);
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = Invertory.Notes;
                sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = Convert.ToInt16(Invertory.IsDeleted);
                sqlCommand.ExecuteNonQuery();
                if (Invertory.Employees == null)
                {
                    continue;
                }
                foreach (InvertoryEmployee employee in Invertory.Employees)
                {
                    new SqlCommand("insert into Safe_Emps(safe_id,emp_id)values(" + Conversions.ToString(Invertory.InvertoryId) + "," + employee.EmployeeId + ")", sqlConnection).ExecuteNonQuery();
                }
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        return result;
    }

    public List<Invertory> ReadInvertory()
    {
        SqlConnection selectConnection;
        selectConnection = MainClass.ConnObj();
        List<Invertory> list;
        list = new List<Invertory>();
        checked
        {
            List<Invertory> result;
            try
            {
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("select * from Safes order by id", selectConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    Invertory invertory;
                    invertory = new Invertory();
                    invertory.InvertoryId = Conversions.ToInteger(dataTable.Rows[i]["id"]);
                    invertory.ClientCode = Sync.ClientCode;
                    invertory.InvertoryName = Conversions.ToString(dataTable.Rows[i]["name"]);
                    invertory.BranchId = Conversions.ToInteger(dataTable.Rows[i]["branch"]);
                    invertory.Status = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["status"]));
                    invertory.IsDefault = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["IS_Default"]));
                    invertory.Notes = Conversions.ToString(dataTable.Rows[i]["notes"]);
                    invertory.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["IS_Deleted"]));
                    invertory.CreateDate = DateTime.Now;
                    invertory.LastUpdateDate = DateTime.Now;
                    SqlDataAdapter sqlDataAdapter2;
                    sqlDataAdapter2 = new SqlDataAdapter("select * from Safe_Emps where safe_id=" + Conversions.ToString(invertory.InvertoryId), selectConnection);
                    DataTable dataTable2;
                    dataTable2 = new DataTable();
                    sqlDataAdapter2.Fill(dataTable2);
                    int num2;
                    num2 = dataTable2.Rows.Count - 1;
                    for (int j = 0; j <= num2; j++)
                    {
                        InvertoryEmployee invertoryEmployee;
                        invertoryEmployee = new InvertoryEmployee();
                        invertoryEmployee.EmployeeId = Conversions.ToString(dataTable2.Rows[j]["emp_id"]);
                        invertoryEmployee.ClientCode = invertory.ClientCode;
                        invertoryEmployee.InvertoryId = invertory.InvertoryId;
                        invertory.Employees.Add(invertoryEmployee);
                    }
                    list.Add(invertory);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            return result;
        }
    }

    public bool SaveProductsPrice(List<ProductPrices> ProductPrice)
    {
        bool result;
        try
        {
            new SqlCommand();
            SqlConnection sqlConnection;
            sqlConnection = MainClass.ConnObj();
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            foreach (ProductPrices item in ProductPrice)
            {
                bool flag;
                flag = !(Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from ItemPrices where ItemID= " + Conversions.ToString(item.ProductId), sqlConnection).ExecuteScalar())) > 0.0);
                if (item.ProductId == 3)
                {
                    Console.Write("");
                }
                if (!flag)
                {
                    SqlCommand sqlCommand;
                    sqlCommand = new SqlCommand("update ItemPrices set CompetitorPrice=@CompetitorPrice, WholesalePrice=@WholesalePrice,ConsumerPrice=@ConsumerPrice ,\r\n                                         time=@time, date=@date where ItemID=@ItemID", sqlConnection);
                    sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = item.ProductId;
                    sqlCommand.Parameters.Add("@CompetitorPrice", SqlDbType.Float).Value = item.CompetitorPrice;
                    sqlCommand.Parameters.Add("@WholesalePrice", SqlDbType.Float).Value = item.WholesalePrice;
                    sqlCommand.Parameters.Add("@ConsumerPrice", SqlDbType.Float).Value = item.ConsumerPrice;
                    sqlCommand.Parameters.Add("@time", SqlDbType.NVarChar).Value = DateTime.Now.ToShortTimeString();
                    sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                    sqlCommand.ExecuteNonQuery();
                }
            }
            result = true;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Interaction.MsgBox($"An error occurred: {ex.Message}");
            result = false;
            ProjectData.ClearProjectError();
        }
        return result;
    }

    private bool ProductExists(SqlConnection conn, int productId)
    {
        using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemPrices WHERE ItemID = @ItemID", conn);
        sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = productId;
        return Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0;
    }

    private void UpdateProductPrice(SqlConnection conn, ProductPrices prod)
    {
        using SqlCommand sqlCommand = new SqlCommand("UPDATE ItemPrices SET CompetitorPrice = @CompetitorPrice, WholesalePrice = @WholesalePrice, ConsumerPrice = @ConsumerPrice WHERE ItemID = @ItemID", conn);
        sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = prod.ProductId;
        sqlCommand.Parameters.Add("@CompetitorPrice", SqlDbType.Float).Value = prod.CompetitorPrice;
        sqlCommand.Parameters.Add("@WholesalePrice", SqlDbType.Float).Value = prod.WholesalePrice;
        sqlCommand.Parameters.Add("@ConsumerPrice", SqlDbType.Float).Value = prod.ConsumerPrice;
        sqlCommand.ExecuteNonQuery();
    }

    public bool SaveSyncProducts(List<Product> Products)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        if (sqlConnection.State != ConnectionState.Open)
        {
            sqlConnection.Open();
        }
        SqlTransaction sqlTransaction;
        sqlTransaction = sqlConnection.BeginTransaction();
        new SqlCommand();
        bool result;
        try
        {
            if (Products.Count == 0)
            {
                result = false;
            }
            else
            {
                foreach (Product Product in Products)
                {
                    if (!Sync.ActiveSync || Sync.BranchType == 4 || Operators.CompareString(Product.ClientCode, Sync.ClientCode, TextCompare: false) == 0)
                    {
                        SqlCommand sqlCommand;
                        sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from items where id= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0) ? new SqlCommand("update Items set Code=@Code,GrpCode=@GrpCode, name=@name,nameEN=@nameEN,barcode=@barcode,group_id=@group_id,unit=@unit,purch_price=@purch_price,sale_price=@sale_price,limit=@limit,discount=@discount,tax_group=@tax_group,tax=@tax,IS_Deleted=@IS_Deleted  , ShowInPOS=@ShowInPOS,Wscale=@Wscale,store=@store,ItemType=@ItemType,ItemProperty=@ItemProperty,FillValue=@FillValue,MaxQtyLimit=@MaxQtyLimit,EgyItemCode=@EgyItemCode,WithholdingTax= @WithholdingTax,EgyCodeType=@EgyCodeType,\r\n                                        MaxDicountParcent=@MaxDicountParcent,MaxDiscountAmount=@MaxDiscountAmount ,showInAndroid=@showInAndroid  where id=@id", sqlConnection, sqlTransaction) : new SqlCommand("insert into Items(id,Code,GrpCode,name,nameEN,barcode,group_id,unit,purch_price,sale_price,limit,discount,tax_group,tax,IS_Deleted,image,ShowInPOS,Wscale,store,ItemType,ItemProperty,FillValue,MaxQtyLimit,EgyItemCode,WithholdingTax,EgyCodeType,MaxDicountParcent,MaxDiscountAmount,showInAndroid)values(@id,@Code,@GrpCode,@name,@nameEN,@barcode,@group_id,@unit,@purch_price,@sale_price,@limit,@discount,@tax_group,@tax,@IS_Deleted,@ShowInPOS,@Wscale,@store,@ItemType,@ItemProperty,@FillValue,@MaxQtyLimit,@EgyItemCode,@WithholdingTax,@EgyCodeType,@MaxDicountParcent,@MaxDiscountAmount,@showInAndroid)", sqlConnection, sqlTransaction));
                        sqlCommand.Parameters.Add("@id", SqlDbType.NVarChar).Value = Product.ProductId;
                        sqlCommand.Parameters.Add("@Code", SqlDbType.NVarChar).Value = Product.Code;
                        sqlCommand.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = ItemOper.GetGroupCode(Conversions.ToInteger(Product.CategoryID));
                        sqlCommand.Parameters.Add("@group_id", SqlDbType.Int).Value = Product.CategoryID;
                        sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = Product.Name;
                        sqlCommand.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = Product.NameEN;
                        if ((object)Product.MainBarcode == DBNull.Value)
                        {
                            Product.MainBarcode = "";
                        }
                        sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = Product.MainBarcode;
                        sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = Product.MainUnit;
                        sqlCommand.Parameters.Add("@purch_price", SqlDbType.Float).Value = Product.PurchPrice;
                        sqlCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = Product.SalePrice;
                        sqlCommand.Parameters.Add("@limit", SqlDbType.Int).Value = Product.limitStock;
                        sqlCommand.Parameters.Add("@discount", SqlDbType.Float).Value = 0;
                        sqlCommand.Parameters.Add("@tax_group", SqlDbType.Int).Value = 1;
                        sqlCommand.Parameters.Add("@tax", SqlDbType.Float).Value = Product.VAT;
                        sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                        sqlCommand.Parameters.Add("@ShowInPOS", SqlDbType.Bit).Value = Convert.ToInt16(Product.ShowInPOS);
                        sqlCommand.Parameters.Add("@IsStock", SqlDbType.Bit).Value = true;
                        sqlCommand.Parameters.Add("@ItemType", SqlDbType.Int).Value = Product.Type;
                        sqlCommand.Parameters.Add("@Wscale", SqlDbType.Int).Value = Product.Wscale;
                        sqlCommand.Parameters.Add("@ItemProperty", SqlDbType.Int).Value = Product.Property;
                        sqlCommand.Parameters.Add("@FillValue", SqlDbType.Float).Value = Product.PackingValue;
                        sqlCommand.Parameters.Add("@MaxQtyLimit", SqlDbType.Float).Value = Product.MaxQtyLimit;
                        sqlCommand.Parameters.Add("@store", SqlDbType.Int).Value = 1;
                        sqlCommand.Parameters.Add("@MaxDicountParcent", SqlDbType.Float).Value = Product.MaxDicountParcent;
                        sqlCommand.Parameters.Add("@MaxDiscountAmount", SqlDbType.Float).Value = Product.MaxDiscountAmount;
                        sqlCommand.Parameters.Add("@EgyItemCode", SqlDbType.NVarChar).Value = "";
                        sqlCommand.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = 0;
                        sqlCommand.Parameters.Add("@EgyCodeType", SqlDbType.NVarChar).Value = "";
                        sqlCommand.Parameters.Add("@showInAndroid", SqlDbType.Bit).Value = Convert.ToInt16(Product.showInAndroid);
                        sqlCommand.ExecuteNonQuery();
                        new SqlCommand("delete  from ItemUnits where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductUnit productUnit in Product.ProductUnits)
                        {
                            sqlCommand = new SqlCommand("insert into ItemUnits(ItemId,unit,perc,purch,sale,barcode)values(@ItemId,@unit,@perc,@purch,@sale,@barcode)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = Product.ProductId;
                            sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = productUnit.UnitId;
                            sqlCommand.Parameters.Add("@perc", SqlDbType.Int).Value = productUnit.UnitEquality;
                            sqlCommand.Parameters.Add("@purch", SqlDbType.Float).Value = productUnit.PurchasePrice;
                            sqlCommand.Parameters.Add("@sale", SqlDbType.Float).Value = productUnit.SalePrice;
                            sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = productUnit.Barcode;
                            sqlCommand.ExecuteNonQuery();
                        }
                        new SqlCommand("delete  from ItemComponents where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductComponent productComponent in Product.ProductComponents)
                        {
                            sqlCommand = new SqlCommand("insert into ItemComponents(itemId,ComponentId,price,quantity,unit,total,type,store)values(@ItemID,@compID,@price,@quantity,@unit,@total,@type,@store)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = productComponent.ProductId;
                            sqlCommand.Parameters.Add("@compID", SqlDbType.Int).Value = productComponent.ComponentId;
                            sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = productComponent.UnitId;
                            sqlCommand.Parameters.Add("@price", SqlDbType.Float).Value = productComponent.ComponentCost;
                            sqlCommand.Parameters.Add("@quantity", SqlDbType.Float).Value = productComponent.Quantity;
                            sqlCommand.Parameters.Add("@total", SqlDbType.Float).Value = Conversions.ToDouble(productComponent.Quantity) * (double)productComponent.ComponentCost;
                            sqlCommand.Parameters.Add("@store", SqlDbType.Int).Value = productComponent.InvertoryId;
                            sqlCommand.Parameters.Add("@type", SqlDbType.Bit).Value = productComponent.ComponentType;
                            sqlCommand.ExecuteNonQuery();
                        }
                        new SqlCommand("delete  from Itembarcodes where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductBarcode productBarcode in Product.ProductBarcodes)
                        {
                            sqlCommand = new SqlCommand("insert into Itembarcodes(ItemId,barcode)values(@ItemId,@barcode)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = productBarcode.ProductId;
                            sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = productBarcode.Barcode;
                            sqlCommand.ExecuteNonQuery();
                        }
                        if (Product.productPrices != null)
                        {
                            new SqlCommand("delete from ItemPrices where ItemId=" + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                            sqlCommand = new SqlCommand("insert into ItemPrices(ItemId,UnitId,time,date,purch_price,sale_price,low_purch_price,high_purch_price,low_sale_price,high_sale_price,CompetitorPrice,IS_Deleted,Emp) values (@ItemId,@UnitId,@time,@date,@purch_price,@sale_price,@low_purch_price,@high_purch_price,@low_sale_price,@high_sale_price,@CompetitorPrice,@IS_Deleted,@Emp)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = Product.productPrices.ProductId;
                            sqlCommand.Parameters.Add("@UnitId", SqlDbType.Int).Value = Product.productPrices.UnitId;
                            sqlCommand.Parameters.Add("@time", SqlDbType.NVarChar).Value = DateTime.Now.ToShortTimeString();
                            sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                            sqlCommand.Parameters.Add("@purch_price", SqlDbType.Float).Value = Product.productPrices.PurchasePrice;
                            sqlCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = Product.productPrices.SalePrice;
                            sqlCommand.Parameters.Add("@low_purch_price", SqlDbType.Float).Value = Product.productPrices.LowPurchasePrice;
                            sqlCommand.Parameters.Add("@high_purch_price", SqlDbType.Float).Value = Product.productPrices.HighPurchasePrice;
                            sqlCommand.Parameters.Add("@low_sale_price", SqlDbType.Float).Value = Product.productPrices.LowSalePrice;
                            sqlCommand.Parameters.Add("@high_sale_price", SqlDbType.Float).Value = Product.productPrices.HighSalePrice;
                            sqlCommand.Parameters.Add("@CompetitorPrice", SqlDbType.Float).Value = Product.productPrices.CompetitorPrice;
                            sqlCommand.Parameters.Add("@Emp", SqlDbType.Int).Value = MainClass.EmpNo;
                            sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                            sqlCommand.ExecuteNonQuery();
                        }
                        continue;
                    }
                    result = false;
                    goto end_IL_0030;
                }
                sqlTransaction.Commit();
                result = true;
            }
        end_IL_0030:;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Exception ex2;
            ex2 = ex;
            sqlTransaction.Rollback();
            string text;
            text = "خطأ أثناء استيراد المواد";
            if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
            {
                text = "error in saving";
            }
            MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public bool SaveProducts(List<Product> Products)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        if (sqlConnection.State != ConnectionState.Open)
        {
            sqlConnection.Open();
        }
        SqlTransaction sqlTransaction;
        sqlTransaction = sqlConnection.BeginTransaction();
        new SqlCommand();
        bool result;
        try
        {
            if (Products.Count == 0)
            {
                result = false;
            }
            else
            {
                foreach (Product Product in Products)
                {
                    if (!Sync.ActiveSync || Sync.BranchType == 4 || Operators.CompareString(Product.ClientCode, Sync.ClientCode, TextCompare: false) == 0)
                    {
                        SqlCommand sqlCommand;
                        sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from items where id= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0) ? new SqlCommand(StoredQueries.UpdateItem, sqlConnection, sqlTransaction) : new SqlCommand(StoredQueries.InsertItem, sqlConnection, sqlTransaction));
                        sqlCommand.Parameters.Add("@id", SqlDbType.NVarChar).Value = Product.ProductId;
                        sqlCommand.Parameters.Add("@Code", SqlDbType.NVarChar).Value = Product.Code;
                        sqlCommand.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = ItemOper.GetGroupCode(Conversions.ToInteger(Product.CategoryID));
                        sqlCommand.Parameters.Add("@group_id", SqlDbType.Int).Value = Product.CategoryID;
                        sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = Product.Name;
                        sqlCommand.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = Product.NameEN;
                        if ((object)Product.MainBarcode == DBNull.Value)
                        {
                            Product.MainBarcode = "";
                        }
                        sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = Product.MainBarcode;
                        sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = Product.MainUnit;
                        sqlCommand.Parameters.Add("@purch_price", SqlDbType.Float).Value = Product.PurchPrice;
                        sqlCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = Product.SalePrice;
                        sqlCommand.Parameters.Add("@limit", SqlDbType.Int).Value = Product.limitStock;
                        sqlCommand.Parameters.Add("@discount", SqlDbType.Float).Value = 0;
                        sqlCommand.Parameters.Add("@tax_group", SqlDbType.Int).Value = 1;
                        sqlCommand.Parameters.Add("@tax", SqlDbType.Float).Value = Product.VAT;
                        sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                        sqlCommand.Parameters.Add("@ShowInPOS", SqlDbType.Bit).Value = Convert.ToInt16(Product.ShowInPOS);
                        sqlCommand.Parameters.Add("@IsStock", SqlDbType.Bit).Value = true;
                        sqlCommand.Parameters.Add("@ItemType", SqlDbType.Int).Value = Product.Type;
                        sqlCommand.Parameters.Add("@Wscale", SqlDbType.Int).Value = Product.Wscale;
                        sqlCommand.Parameters.Add("@ItemProperty", SqlDbType.Int).Value = Product.Property;
                        sqlCommand.Parameters.Add("@FillValue", SqlDbType.Float).Value = Product.PackingValue;
                        sqlCommand.Parameters.Add("@MaxQtyLimit", SqlDbType.Float).Value = Product.MaxQtyLimit;
                        sqlCommand.Parameters.Add("@store", SqlDbType.Int).Value = 1;
                        if (Product.ProductImage != null)
                        {
                            sqlCommand.Parameters.Add("@image", SqlDbType.Image).Value = MainClass.Image2Arr(Product.ProductImage);
                        }
                        else
                        {
                            sqlCommand.Parameters.Add("@image", SqlDbType.Image).Value = DBNull.Value;
                        }
                        sqlCommand.Parameters.Add("@MaxDicountParcent", SqlDbType.Float).Value = Product.MaxDicountParcent;
                        sqlCommand.Parameters.Add("@MaxDiscountAmount", SqlDbType.Float).Value = Product.MaxDiscountAmount;
                        sqlCommand.Parameters.Add("@EgyItemCode", SqlDbType.NVarChar).Value = "";
                        sqlCommand.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = 0;
                        sqlCommand.Parameters.Add("@EgyCodeType", SqlDbType.NVarChar).Value = "";
                        sqlCommand.Parameters.Add("@showInAndroid", SqlDbType.Bit).Value = ((!Information.IsDBNull(Product.showInAndroid)) ? Convert.ToInt16(Product.showInAndroid) : 0);
                        sqlCommand.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = Product.CreateDate;
                        sqlCommand.Parameters.Add("@CreatedBy", SqlDbType.NVarChar).Value = MainClass.UserName;
                        sqlCommand.Parameters.Add("@updatedDate", SqlDbType.DateTime).Value = Product.LastUpdateDate;
                        sqlCommand.Parameters.Add("@updatedBy", SqlDbType.NVarChar).Value = MainClass.UserName;
                        sqlCommand.ExecuteNonQuery();
                        new SqlCommand("delete  from ItemUnits where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductUnit productUnit in Product.ProductUnits)
                        {
                            sqlCommand = new SqlCommand("insert into ItemUnits(ItemId,unit,perc,purch,sale,barcode)values(@ItemId,@unit,@perc,@purch,@sale,@barcode)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = Product.ProductId;
                            sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = productUnit.UnitId;
                            sqlCommand.Parameters.Add("@perc", SqlDbType.Int).Value = productUnit.UnitEquality;
                            sqlCommand.Parameters.Add("@purch", SqlDbType.Float).Value = productUnit.PurchasePrice;
                            sqlCommand.Parameters.Add("@sale", SqlDbType.Float).Value = productUnit.SalePrice;
                            sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = productUnit.Barcode;
                            sqlCommand.ExecuteNonQuery();
                        }
                        new SqlCommand("delete  from ItemComponents where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductComponent productComponent in Product.ProductComponents)
                        {
                            sqlCommand = new SqlCommand("insert into ItemComponents(itemId,ComponentId,price,quantity,unit,total,type,store)values(@ItemID,@compID,@price,@quantity,@unit,@total,@type,@store)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemID", SqlDbType.Int).Value = productComponent.ProductId;
                            sqlCommand.Parameters.Add("@compID", SqlDbType.Int).Value = productComponent.ComponentId;
                            sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = productComponent.UnitId;
                            sqlCommand.Parameters.Add("@price", SqlDbType.Float).Value = productComponent.ComponentCost;
                            sqlCommand.Parameters.Add("@quantity", SqlDbType.Float).Value = productComponent.Quantity;
                            sqlCommand.Parameters.Add("@total", SqlDbType.Float).Value = Conversions.ToDouble(productComponent.Quantity) * (double)productComponent.ComponentCost;
                            sqlCommand.Parameters.Add("@store", SqlDbType.Int).Value = productComponent.InvertoryId;
                            sqlCommand.Parameters.Add("@type", SqlDbType.Bit).Value = productComponent.ComponentType;
                            sqlCommand.ExecuteNonQuery();
                        }
                        new SqlCommand("delete  from Itembarcodes where ItemId= " + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                        foreach (ProductBarcode productBarcode in Product.ProductBarcodes)
                        {
                            sqlCommand = new SqlCommand("insert into Itembarcodes(ItemId,barcode)values(@ItemId,@barcode)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = productBarcode.ProductId;
                            sqlCommand.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = productBarcode.Barcode;
                            sqlCommand.ExecuteNonQuery();
                        }
                        if (Product.productPrices != null)
                        {
                            new SqlCommand("delete from ItemPrices where ItemId=" + Conversions.ToString(Product.ProductId), sqlConnection, sqlTransaction).ExecuteNonQuery();
                            sqlCommand = new SqlCommand("insert into ItemPrices(ItemId,UnitId,time,date,purch_price,sale_price,low_purch_price,high_purch_price,low_sale_price,high_sale_price,CompetitorPrice,IS_Deleted,Emp) values (@ItemId,@UnitId,@time,@date,@purch_price,@sale_price,@low_purch_price,@high_purch_price,@low_sale_price,@high_sale_price,@CompetitorPrice,@IS_Deleted,@Emp)", sqlConnection, sqlTransaction);
                            sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = Product.productPrices.ProductId;
                            sqlCommand.Parameters.Add("@UnitId", SqlDbType.Int).Value = Product.productPrices.UnitId;
                            sqlCommand.Parameters.Add("@time", SqlDbType.NVarChar).Value = DateTime.Now.ToShortTimeString();
                            sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now.ToShortDateString();
                            sqlCommand.Parameters.Add("@purch_price", SqlDbType.Float).Value = Product.productPrices.PurchasePrice;
                            sqlCommand.Parameters.Add("@sale_price", SqlDbType.Float).Value = Product.productPrices.SalePrice;
                            sqlCommand.Parameters.Add("@low_purch_price", SqlDbType.Float).Value = Product.productPrices.LowPurchasePrice;
                            sqlCommand.Parameters.Add("@high_purch_price", SqlDbType.Float).Value = Product.productPrices.HighPurchasePrice;
                            sqlCommand.Parameters.Add("@low_sale_price", SqlDbType.Float).Value = Product.productPrices.LowSalePrice;
                            sqlCommand.Parameters.Add("@high_sale_price", SqlDbType.Float).Value = Product.productPrices.HighSalePrice;
                            sqlCommand.Parameters.Add("@CompetitorPrice", SqlDbType.Float).Value = Product.productPrices.CompetitorPrice;
                            sqlCommand.Parameters.Add("@Emp", SqlDbType.Int).Value = MainClass.EmpNo;
                            sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                            sqlCommand.ExecuteNonQuery();
                        }
                        continue;
                    }
                    result = false;
                    goto end_IL_0030;
                }
                sqlTransaction.Commit();
                result = true;
            }
        end_IL_0030:;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Exception ex2;
            ex2 = ex;
            sqlTransaction.Rollback();
            string text;
            text = "خطأ أثناء استيراد المواد";
            if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
            {
                text = "error in saving";
            }
            MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public bool SaveCategories(List<Category> Categories)
    {
        new CategoryCRUD(Sync.APIUrl);
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        if (sqlConnection.State != ConnectionState.Open)
        {
            sqlConnection.Open();
        }
        new SqlCommand();
        bool flag;
        flag = true;
        bool result;
        try
        {
            foreach (Category Category in Categories)
            {
                if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from itemsCategory where id= " + Conversions.ToString(Category.CategoryId), sqlConnection).ExecuteScalar())) > 0.0)
                {
                    flag = false;
                }
                if (Category.ParentCode == null)
                {
                    Category.ParentCode = "";
                }
                if (Category.NameEN == null)
                {
                    Category.NameEN = "";
                }
                if (Category.Printer == null)
                {
                    Category.Printer = "";
                }
                if (flag)
                {
                    SqlCommand sqlCommand;
                    sqlCommand = new SqlCommand("insert into itemsCategory(id,CategoryId,name,nameEN,printer,ShowInPOS,DispalyOrder,IS_Deleted,code,parentCode,type,BranchId,AllBranch)\r\n                                                values(@id,@CategoryId,@name,@nameEN ,'" + Category.Printer + "' ," + Conversions.ToString((int)Convert.ToInt16(Category.ShowInPOS)) + "," + Conversions.ToString(Category.DispalyOrder) + ",0, @code, @parentCode, @type,@BranchId,@AllBranch)", sqlConnection);
                    sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = Category.CategoryId;
                    sqlCommand.Parameters.Add("@CategoryId", SqlDbType.Int).Value = Category.CategoryId;
                    sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = Category.Name;
                    sqlCommand.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = Category.NameEN;
                    sqlCommand.Parameters.Add("@code", SqlDbType.NVarChar).Value = Category.Code;
                    sqlCommand.Parameters.Add("@ParentCode", SqlDbType.NVarChar).Value = Category.ParentCode;
                    if (Category.ISLeaf)
                    {
                        sqlCommand.Parameters.Add("@type", SqlDbType.Int).Value = 2;
                    }
                    else
                    {
                        sqlCommand.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                    }
                    sqlCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = -1;
                    sqlCommand.Parameters.Add("@AllBranch", SqlDbType.Int).Value = 1;
                    sqlCommand.ExecuteNonQuery();
                }
                else
                {
                    SqlCommand sqlCommand;
                    sqlCommand = new SqlCommand("update itemsCategory set  name =@name,nameEN=@nameEN ,ShowInPOS=" + Conversions.ToString((int)Convert.ToInt16(Category.ShowInPOS)) + ",DispalyOrder=" + Conversions.ToString(Category.DispalyOrder) + ",IS_Deleted=0,code=@code,ParentCode=@ParentCode,type=@type,BranchId=@BranchId,AllBranch=@AllBranch  where id=" + Conversions.ToString(Category.CategoryId), sqlConnection);
                    sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = Category.Name;
                    sqlCommand.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = Category.NameEN;
                    sqlCommand.Parameters.Add("@code", SqlDbType.NVarChar).Value = Category.Code;
                    sqlCommand.Parameters.Add("@ParentCode", SqlDbType.NVarChar).Value = Category.ParentCode;
                    if (Category.ISLeaf)
                    {
                        sqlCommand.Parameters.Add("@type", SqlDbType.Int).Value = 2;
                    }
                    else
                    {
                        sqlCommand.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                    }
                    sqlCommand.Parameters.Add("@BranchId", SqlDbType.Int).Value = -1;
                    sqlCommand.Parameters.Add("@AllBranch", SqlDbType.Int).Value = 1;
                    sqlCommand.ExecuteNonQuery();
                }
                flag = true;
            }
            result = true;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Exception ex2;
            ex2 = ex;
            string text;
            text = "خطأ أثناء استيراد المجموعات";
            if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
            {
                text = "error in saving";
            }
            MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public void SaveCurrentStocks(List<ProductStock> stocks)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        if (sqlConnection.State != ConnectionState.Open)
        {
            sqlConnection.Open();
        }
        SqlTransaction sqlTransaction;
        sqlTransaction = sqlConnection.BeginTransaction();
        try
        {
            new SqlCommand();
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            if (Sync.ActiveSync & (Sync.BranchType == 4))
            {
                new SqlCommand("delete from ProductStocks", sqlConnection, sqlTransaction).ExecuteNonQuery();
            }
            foreach (ProductStock stock in stocks)
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                if (!Sync.ActiveSync & (Sync.BranchType != 4))
                {
                    new SqlCommand(("delete from ProductStocks where ProductId= " + Conversions.ToString(stock.ProductId) + " and InventoryID= " + Conversions.ToString(stock.InventoryId) + " and branchId= " + Conversions.ToString(stock.BranchId)) ?? "", sqlConnection, sqlTransaction).ExecuteNonQuery();
                }
                SqlCommand sqlCommand;
                sqlCommand = new SqlCommand("Insert into ProductStocks(ProductId,InventoryID,branchId,UnitId,Quantity,AvrgCost,LastUpdate) values(@ProductId,@InventoryID,@branchId,@UnitId,@Quantity,@AvrgCost,@LastUpdate)", sqlConnection, sqlTransaction);
                sqlCommand.Parameters.Add("@ProductId", SqlDbType.NVarChar).Value = stock.ProductId;
                sqlCommand.Parameters.Add("@InventoryId", SqlDbType.Int).Value = stock.InventoryId;
                sqlCommand.Parameters.Add("@branchId", SqlDbType.VarChar).Value = stock.BranchId;
                sqlCommand.Parameters.Add("@UnitId", SqlDbType.VarChar).Value = stock.UnitId;
                sqlCommand.Parameters.Add("@Quantity", SqlDbType.VarChar).Value = stock.Quantity;
                sqlCommand.Parameters.Add("@AvrgCost", SqlDbType.VarChar).Value = stock.AvrgCost;
                sqlCommand.Parameters.Add("@LastUpdate", SqlDbType.DateTime).Value = stock.LastUpdate;
                sqlCommand.ExecuteNonQuery();
            }
            sqlTransaction.Commit();
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            ProjectData.ClearProjectError();
        }
    }

    public bool SaveCustomer(List<global::AuditorAPI.Models.Customer> customers, bool AddedLocally)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (global::AuditorAPI.Models.Customer customer in customers)
            {
                if (customer == null)
                {
                    goto IL_0b02;
                }
                SqlCommand sqlCommand;
                sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Customers where id= " + customer.CustomerID, sqlConnection).ExecuteScalar())) != 0.0) ? new SqlCommand("update Customers set name=@name,country=@country,city=@city,area=@area,act=@act,national_id=@national_id,tel=@tel,mobile=@mobile,fax=@fax,email=@email,notes=@notes,type=@type,tax_no=@tax_no,AccountCode=@AccountCode,IdScan=@IdScan,Branch=@Branch,ISCredit=@ISCredit,PlotIdentification=@PlotIdentification,BuildingNumber = @BuildingNumber, StreetName=@StreetName, AdditionalStreetName=@AdditionalStreetName, District=@District,PostalZone=@PostalZone,CrNo=@CrNo,maxdepit=@maxdepit,area2=@area2,city2=@city2 where id=" + customer.CustomerID, sqlConnection) : new SqlCommand("SET IDENTITY_INSERT [dbo].[Customers] ON insert into Customers(id,name,country,city,area,act,national_id,tel,mobile,fax,email,notes,type,tax_no,IS_Deleted,AccountCode,IdScan,Branch,ISCredit,PlotIdentification,BuildingNumber,StreetName,AdditionalStreetName,District,PostalZone,CrNo,maxdepit,area2,city2) values (@id,@name,@country,@city,@area,@act,@national_id,@tel,@mobile,@fax,@email,@notes,@type,@tax_no,@IS_Deleted,@AccountCode,@IdScan,@Branch,@ISCredit,@PlotIdentification,@BuildingNumber,@StreetName,@AdditionalStreetName,@District,@PostalZone,@CrNo,@maxdepit,@area2,@city2) SET IDENTITY_INSERT [dbo].[Customers] off", sqlConnection));
                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = customer.CustomerID;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = customer.Name;
                if (((double)customer.Country != Conversions.ToDouble("-1")) & (customer.Country.ToString() != null) & ((object)customer.Country.ToString() != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@country", SqlDbType.Int).Value = customer.Country;
                }
                else
                {
                    sqlCommand.Parameters.Add("@country", SqlDbType.Int).Value = -1;
                }
                if (((double)customer.City != Conversions.ToDouble("-1")) & (customer.City.ToString() != null) & ((object)customer.City.ToString() != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@city", SqlDbType.Int).Value = customer.City;
                }
                else
                {
                    sqlCommand.Parameters.Add("@city", SqlDbType.Int).Value = -1;
                }
                if (((double)customer.Region != Conversions.ToDouble("-1")) & (customer.Region.ToString() != null) & ((object)customer.Region.ToString() != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@area", SqlDbType.Int).Value = customer.Region;
                }
                else
                {
                    sqlCommand.Parameters.Add("@area", SqlDbType.Int).Value = -1;
                }
                sqlCommand.Parameters.Add("@act", SqlDbType.Int).Value = 0;
                if ((Operators.CompareString(customer.AccCode, "-1", TextCompare: false) != 0) & (Operators.CompareString(customer.AccCode, "", TextCompare: false) != 0) & (customer.AccCode != null) & ((object)customer.AccCode != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@AccountCode", SqlDbType.Int).Value = customer.AccCode;
                }
                else
                {
                    sqlCommand.Parameters.Add("@AccountCode", SqlDbType.Int).Value = -1;
                }
                sqlCommand.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = customer.NatianalID;
                sqlCommand.Parameters.Add("@tel", SqlDbType.NVarChar).Value = customer.Telephone;
                sqlCommand.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = customer.Mobile;
                sqlCommand.Parameters.Add("@fax", SqlDbType.NVarChar).Value = 0;
                sqlCommand.Parameters.Add("@email", SqlDbType.NVarChar).Value = customer.Email;
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = customer.Note;
                sqlCommand.Parameters.Add("@type", SqlDbType.Int).Value = customer.Type;
                sqlCommand.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = customer.VATno;
                sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = Convert.ToInt16(customer.IsDeleted);
                sqlCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = customer.BranchId;
                if (Convert.ToInt32(customer.Maxdept) == 1)
                {
                    sqlCommand.Parameters.Add("@maxdepit", SqlDbType.Decimal).Value = 1;
                }
                else
                {
                    sqlCommand.Parameters.Add("@maxdepit", SqlDbType.Decimal).Value = 0;
                }
                if ((Operators.CompareString(customer.AccCode, "-1", TextCompare: false) != 0) & (Operators.CompareString(customer.AccCode, "", TextCompare: false) != 0) & (customer.AccCode != null) & ((object)customer.AccCode != DBNull.Value))
                {
                    sqlCommand.Parameters.Add("@ISCredit", SqlDbType.Bit).Value = 1;
                }
                else
                {
                    sqlCommand.Parameters.Add("@ISCredit", SqlDbType.Bit).Value = 0;
                }
                sqlCommand.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;
                if (customer.PlotIdentification != null)
                {
                    sqlCommand.Parameters.Add("@PlotIdentification", SqlDbType.NVarChar).Value = customer.PlotIdentification;
                }
                else
                {
                    sqlCommand.Parameters.Add("@PlotIdentification", SqlDbType.NVarChar).Value = "";
                }
                if (customer.BuildingNumber != null)
                {
                    sqlCommand.Parameters.Add("@BuildingNumber", SqlDbType.NVarChar).Value = customer.BuildingNumber;
                }
                else
                {
                    sqlCommand.Parameters.Add("@BuildingNumber", SqlDbType.NVarChar).Value = "";
                }
                if (customer.StreetName != null)
                {
                    sqlCommand.Parameters.Add("@StreetName", SqlDbType.NVarChar).Value = customer.StreetName;
                }
                else
                {
                    sqlCommand.Parameters.Add("@StreetName", SqlDbType.NVarChar).Value = "";
                }
                if (customer.AdditionalStreetName != null)
                {
                    sqlCommand.Parameters.Add("@AdditionalStreetName", SqlDbType.NVarChar).Value = customer.AdditionalStreetName;
                }
                else
                {
                    sqlCommand.Parameters.Add("@AdditionalStreetName", SqlDbType.NVarChar).Value = "";
                }
                if (customer.District != null)
                {
                    sqlCommand.Parameters.Add("@District", SqlDbType.NVarChar).Value = customer.District;
                }
                else
                {
                    sqlCommand.Parameters.Add("@District", SqlDbType.NVarChar).Value = "";
                }
                if (customer.PostalZone != null)
                {
                    sqlCommand.Parameters.Add("@PostalZone", SqlDbType.NVarChar).Value = customer.PostalZone;
                }
                else
                {
                    sqlCommand.Parameters.Add("@PostalZone", SqlDbType.NVarChar).Value = "";
                }
                if (customer.CrNo != null)
                {
                    sqlCommand.Parameters.Add("@CrNo", SqlDbType.NVarChar).Value = customer.CrNo;
                }
                else
                {
                    sqlCommand.Parameters.Add("@CrNo", SqlDbType.NVarChar).Value = "";
                }
                if (customer.CrNo != null)
                {
                    sqlCommand.Parameters.Add("@area2", SqlDbType.NVarChar).Value = customer.area2;
                }
                else
                {
                    sqlCommand.Parameters.Add("@area2", SqlDbType.NVarChar).Value = "";
                }
                if (customer.CrNo != null)
                {
                    sqlCommand.Parameters.Add("@city2", SqlDbType.NVarChar).Value = customer.city2;
                }
                else
                {
                    sqlCommand.Parameters.Add("@city2", SqlDbType.NVarChar).Value = "";
                }
                sqlCommand.ExecuteNonQuery();
                string parentCode;
                parentCode = Common.CurrentBranch.CustomersAcc;
                if (!customer.isClient)
                {
                    parentCode = Common.CurrentBranch.SupliersAcc;
                }
                if (AddedLocally)
                {
                    if (!customer.IsCredit)
                    {
                        goto IL_0b02;
                    }
                    TreeAccount treeAccount;
                    treeAccount = new TreeAccount();
                    treeAccount.AccName = customer.Name;
                    treeAccount.AccNature = 1;
                    treeAccount.Code = customer.AccCode;
                    treeAccount.AccountID = Conversions.ToInteger(customer.AccCode);
                    treeAccount.ParentCode = parentCode;
                    treeAccount.IntialBalance = 0m;
                    treeAccount.EmpId = MainClass.EmpNo;
                    treeAccount.AccType = 2;
                    treeAccount.IsDeleted = false;
                    treeAccount.BranchId = customer.BranchId;
                    treeAccount.CreateDate = DateTime.Now;
                    treeAccount.ClientCode = Sync.ClientCode;
                    treeAccount.LastUpdateDate = DateTime.Now;
                    List<TreeAccount> list;
                    list = new List<TreeAccount>();
                    list.Add(treeAccount);
                    if (this.SaveAccounts(list))
                    {
                        if (Sync.ActiveSync & (Sync.SyncType > 0))
                        {
                            new AccountCRUD(Sync.APIUrl).AddTreeAccount(list);
                        }
                        goto IL_0b02;
                    }
                    result = false;
                }
                else
                {
                    if (!(customer.IsCredit & (Convert.ToInt32(customer.Maxdept) == 1)))
                    {
                        goto IL_0b02;
                    }
                    customer.isClient = true;
                    parentCode = Common.CurrentBranch.CustomersAcc;
                    TreeAccount treeAccount2;
                    treeAccount2 = new TreeAccount();
                    treeAccount2.AccName = customer.Name;
                    treeAccount2.AccNature = 1;
                    treeAccount2.Code = customer.AccCode;
                    treeAccount2.AccountID = Conversions.ToInteger(customer.AccCode);
                    treeAccount2.ParentCode = parentCode;
                    treeAccount2.IntialBalance = 0m;
                    treeAccount2.EmpId = MainClass.EmpNo;
                    treeAccount2.AccType = 2;
                    treeAccount2.IsDeleted = false;
                    treeAccount2.BranchId = customer.BranchId;
                    treeAccount2.CreateDate = DateTime.Now;
                    treeAccount2.ClientCode = Sync.ClientCode;
                    treeAccount2.LastUpdateDate = DateTime.Now;
                    List<TreeAccount> list2;
                    list2 = new List<TreeAccount>();
                    list2.Add(treeAccount2);
                    if (this.SaveAccounts(list2))
                    {
                        if (Sync.ActiveSync & (Sync.SyncType > 0))
                        {
                            new AccountCRUD(Sync.APIUrl).AddTreeAccount(list2);
                        }
                        goto IL_0b02;
                    }
                    result = false;
                }
                goto end_IL_0009;
            IL_0b02:
                if (Sync.ActiveSync & (Sync.SyncType > 0))
                {
                    new CustomerCRUD(Sync.APIUrl).AddCustomer(customers);
                }
            }
            result = true;
        end_IL_0009:;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<global::AuditorAPI.Models.Customer> ReadCustomer()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<global::AuditorAPI.Models.Customer> list;
        list = new List<global::AuditorAPI.Models.Customer>();
        checked
        {
            List<global::AuditorAPI.Models.Customer> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                new SqlCommand();
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("Select * from Customers where IS_Deleted=0 ", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    global::AuditorAPI.Models.Customer customer;
                    customer = new global::AuditorAPI.Models.Customer();
                    customer.CustomerID = Conversions.ToString(dataTable.Rows[i]["id"]);
                    customer.Name = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["name"]));
                    customer.Country = Conversions.ToInteger(dataTable.Rows[i]["country"]);
                    customer.City = Conversions.ToInteger(dataTable.Rows[i]["city"]);
                    customer.Region = Conversions.ToInteger(dataTable.Rows[i]["area"]);
                    customer.AccCode = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["AccountCode"]));
                    customer.NatianalID = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["national_id"]));
                    customer.Telephone = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["tel"]));
                    customer.Mobile = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["mobile"]));
                    customer.Email = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["email"]));
                    customer.Note = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
                    customer.Type = Conversions.ToInteger(dataTable.Rows[i]["type"]);
                    customer.VATno = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["tax_no"]));
                    customer.IsDeleted = false;
                    customer.BranchId = Conversions.ToInteger(dataTable.Rows[i]["Branch"]);
                    customer.IsCredit = Conversions.ToBoolean(dataTable.Rows[i]["ISCredit"]);
                    customer.CreateDate = DateTime.Now;
                    customer.LastUpdateDate = DateTime.Now;
                    customer.ClientCode = Sync.ClientCode;
                    list.Add(customer);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public bool SaveTreasuries(List<global::AuditorAPI.Models.Treasury> treasuries, bool AddedLocally)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (global::AuditorAPI.Models.Treasury treasury in treasuries)
            {
                if (treasury == null)
                {
                    continue;
                }
                SqlCommand sqlCommand;
                if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Stocks where id= " + Conversions.ToString(treasury.TreasuryId), sqlConnection).ExecuteScalar())) == 0.0)
                {
                    sqlCommand = new SqlCommand("SET IDENTITY_INSERT [dbo].[Stocks] ON insert into Stocks(id, name,branch,Acc_Code,status,IS_Default,notes,IS_Deleted)values (@id,@name,@branch,@Acc_Code,@status,@IS_Default,@notes,@IS_Deleted) SET IDENTITY_INSERT [dbo].[Stocks] off", sqlConnection);
                }
                else
                {
                    if (AddedLocally)
                    {
                        new SqlCommand("delete from Stock_Emps where stock_id=" + Conversions.ToString(treasury.TreasuryId), sqlConnection).ExecuteNonQuery();
                    }
                    sqlCommand = new SqlCommand("update Stocks set name=@name,branch=@branch,status=@status,IS_Default=@IS_Default,notes=@notes,IS_Deleted=@IS_Deleted where id=" + Conversions.ToString(treasury.TreasuryId), sqlConnection);
                }
                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = treasury.TreasuryId;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = treasury.TreasuryName;
                sqlCommand.Parameters.Add("@branch", SqlDbType.Int).Value = treasury.BranchId;
                sqlCommand.Parameters.Add("@Acc_Code", SqlDbType.Int).Value = treasury.AccId;
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                sqlCommand.Parameters.Add("@IS_Default", SqlDbType.Bit).Value = Convert.ToInt16(treasury.IsDefault);
                sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = Convert.ToInt16(treasury.IsDeleted);
                sqlCommand.Parameters.Add("@status", SqlDbType.Bit).Value = Convert.ToInt16(treasury.IsActive);
                sqlCommand.ExecuteNonQuery();
                foreach (TreasuryEmployee employee in treasury.Employees)
                {
                    new SqlCommand("insert into Stock_Emps(stock_id,emp_id)values(" + Conversions.ToString(employee.TreasuryId) + "," + employee.EmployeeId + ")", sqlConnection).ExecuteNonQuery();
                }
                if (!AddedLocally)
                {
                    continue;
                }
                TreeAccount treeAccount;
                treeAccount = new TreeAccount();
                treeAccount.AccName = treasury.TreasuryName;
                treeAccount.AccNature = 1;
                treeAccount.Code = treasury.AccId;
                treeAccount.AccountID = Conversions.ToInteger(treasury.AccId);
                treeAccount.ParentCode = Common.CurrentBranch.TreasuriesAcc;
                treeAccount.IntialBalance = 0m;
                treeAccount.EmpId = MainClass.EmpNo;
                treeAccount.AccType = 2;
                treeAccount.IsDeleted = false;
                treeAccount.BranchId = treasury.BranchId;
                treeAccount.CreateDate = DateTime.Now;
                treeAccount.ClientCode = Sync.ClientCode;
                treeAccount.LastUpdateDate = DateTime.Now;
                List<TreeAccount> list;
                list = new List<TreeAccount>();
                list.Add(treeAccount);
                if (this.SaveAccounts(list))
                {
                    if (Sync.ActiveSync & (Sync.SyncType > 0))
                    {
                        new AccountCRUD(Sync.APIUrl).AddTreeAccount(list);
                    }
                    continue;
                }
                result = false;
                goto end_IL_0009;
            }
            result = true;
        end_IL_0009:;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<global::AuditorAPI.Models.Treasury> ReadTreasuries()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<global::AuditorAPI.Models.Treasury> list;
        list = new List<global::AuditorAPI.Models.Treasury>();
        checked
        {
            List<global::AuditorAPI.Models.Treasury> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                new SqlCommand();
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("Select id, name,branch,Acc_Code,status,IS_Default,notes,IS_Deleted from Stocks where IS_Deleted=0 ", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    global::AuditorAPI.Models.Treasury treasury;
                    treasury = new global::AuditorAPI.Models.Treasury();
                    treasury.TreasuryId = Conversions.ToInteger(dataTable.Rows[i]["id"]);
                    treasury.TreasuryName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["name"]));
                    treasury.AccId = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Acc_Code"]));
                    treasury.IsDeleted = false;
                    treasury.BranchId = Conversions.ToInteger(dataTable.Rows[i]["Branch"]);
                    treasury.IsActive = Conversions.ToBoolean(dataTable.Rows[i]["status"]);
                    treasury.IsDefault = Conversions.ToBoolean(dataTable.Rows[i]["IS_Default"]);
                    treasury.CreateDate = DateTime.Now;
                    treasury.LastUpdateDate = DateTime.Now;
                    treasury.ClientCode = Sync.ClientCode;
                    SqlDataAdapter sqlDataAdapter2;
                    sqlDataAdapter2 = new SqlDataAdapter("select * from Stock_Emps where stock_id=" + Conversions.ToString(treasury.TreasuryId), sqlConnection);
                    DataTable dataTable2;
                    dataTable2 = new DataTable();
                    sqlDataAdapter2.Fill(dataTable2);
                    int num2;
                    num2 = dataTable2.Rows.Count - 1;
                    for (int j = 0; j <= num2; j++)
                    {
                        TreasuryEmployee treasuryEmployee;
                        treasuryEmployee = new TreasuryEmployee();
                        treasuryEmployee.EmployeeId = Conversions.ToString(dataTable2.Rows[j]["emp_id"]);
                        treasuryEmployee.ClientCode = Sync.ClientCode;
                        treasuryEmployee.TreasuryId = treasury.TreasuryId;
                        treasury.Employees.Add(treasuryEmployee);
                    }
                    list.Add(treasury);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public bool SaveBanks(List<global::AuditorAPI.Models.Bank> banks, bool AddedLocally)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (global::AuditorAPI.Models.Bank bank in banks)
            {
                if (bank == null)
                {
                    continue;
                }
                SqlCommand sqlCommand;
                sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Banks where id= " + Conversions.ToString(bank.BankId), sqlConnection).ExecuteScalar())) != 0.0) ? new SqlCommand("update Banks set name=@name ,country=@country ,city=@city ,area=@area ,tel=@tel ,mobile=@mobile,notes=@notes ,IS_Deleted=@IS_Deleted , DisPre=@DisPre,ChangeInPOS=@ChangeInPOS where id=" + Conversions.ToString(bank.BankId), sqlConnection) : new SqlCommand("SET IDENTITY_INSERT [dbo].[Banks] ON insert into Banks(id,name,country,city,Acc_Code,area,tel,mobile,notes,IS_Deleted,DisPre,ChangeInPOS) values (@id,@name,@country,@city,@Acc_Code,@area,@tel,@mobile,@notes,@IS_Deleted,@DisPre,@ChangeInPOS) SET IDENTITY_INSERT [dbo].[Banks] off", sqlConnection));
                sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = bank.BankId;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = bank.Bankname;
                sqlCommand.Parameters.Add("@country", SqlDbType.Int).Value = bank.Country;
                sqlCommand.Parameters.Add("@city", SqlDbType.Int).Value = bank.City;
                sqlCommand.Parameters.Add("@area", SqlDbType.Int).Value = 1;
                sqlCommand.Parameters.Add("@tel", SqlDbType.NVarChar).Value = bank.telphone;
                sqlCommand.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = bank.mobile;
                sqlCommand.Parameters.Add("@Acc_Code", SqlDbType.Int).Value = bank.AccId;
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = Convert.ToInt16(bank.IS_Deleted);
                sqlCommand.Parameters.Add("@DisPre", SqlDbType.Float).Value = bank.DisPre;
                sqlCommand.Parameters.Add("@ChangeInPOS", SqlDbType.Bit).Value = bank.ChangeInPOS;
                sqlCommand.ExecuteNonQuery();
                if (!AddedLocally)
                {
                    continue;
                }
                TreeAccount treeAccount;
                treeAccount = new TreeAccount();
                treeAccount.AccName = bank.Bankname;
                treeAccount.AccNature = 1;
                treeAccount.Code = Conversions.ToString(bank.AccId);
                treeAccount.AccountID = bank.AccId;
                treeAccount.ParentCode = Common.CurrentBranch.BanksAcc;
                treeAccount.IntialBalance = 0m;
                treeAccount.EmpId = MainClass.EmpNo;
                treeAccount.AccType = 2;
                treeAccount.IsDeleted = false;
                treeAccount.BranchId = bank.BranchId;
                treeAccount.CreateDate = DateTime.Now;
                treeAccount.ClientCode = Sync.ClientCode;
                treeAccount.LastUpdateDate = DateTime.Now;
                List<TreeAccount> list;
                list = new List<TreeAccount>();
                list.Add(treeAccount);
                if (this.SaveAccounts(list))
                {
                    if (Sync.ActiveSync & (Sync.SyncType > 0))
                    {
                        new AccountCRUD(Sync.APIUrl).AddTreeAccount(list);
                    }
                    continue;
                }
                result = false;
                goto end_IL_0009;
            }
            result = true;
        end_IL_0009:;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<global::AuditorAPI.Models.Bank> ReadBanks()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<global::AuditorAPI.Models.Bank> list;
        list = new List<global::AuditorAPI.Models.Bank>();
        checked
        {
            List<global::AuditorAPI.Models.Bank> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                new SqlCommand();
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("Select id,name,country,city,Acc_Code,area,tel,mobile,notes,DisPre  from banks where IS_Deleted=0 ", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    global::AuditorAPI.Models.Bank bank;
                    bank = new global::AuditorAPI.Models.Bank();
                    bank.BankId = Conversions.ToInteger(dataTable.Rows[i]["id"]);
                    bank.Bankname = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["name"]));
                    bank.Country = Conversions.ToInteger(dataTable.Rows[i]["country"]);
                    bank.City = Conversions.ToInteger(dataTable.Rows[i]["city"]);
                    bank.AccId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["Acc_Code"]));
                    bank.telphone = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["tel"]));
                    bank.mobile = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["mobile"]));
                    bank.DisPre = Conversions.ToDecimal(Operators.ConcatenateObject("", dataTable.Rows[i]["DisPre"]));
                    bank.IS_Deleted = false;
                    bank.BranchId = MainClass.BranchNo;
                    bank.CreateDate = DateTime.Now;
                    bank.LastUpdateDate = DateTime.Now;
                    bank.ClientCode = Sync.ClientCode;
                    list.Add(bank);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public bool SaveAccounts(List<TreeAccount> Accounts)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (TreeAccount Account in Accounts)
            {
                if (Account != null)
                {
                    SqlCommand sqlCommand;
                    sqlCommand = ((Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("select count(*) from Accounts_Index where code=" + Account.Code, sqlConnection).ExecuteScalar())) != 0.0) ? new SqlCommand("update Accounts_Index set AName=N'" + Account.AccName + "',ParentCode=N'" + Account.ParentCode + "',Type=" + Conversions.ToString(Account.AccType) + ",Nature=" + Conversions.ToString(Account.AccNature) + ",IValue=0, Acc_branch=" + Conversions.ToString(Account.BranchId) + ",IsDeleted=" + Conversions.ToString((int)Convert.ToInt16(Account.IsDeleted)) + " ,CostCenter=" + Conversions.ToString(Account.CostCenter) + ", FinalAcc=" + Conversions.ToString(Account.FinalAcc) + " where Code=" + Account.Code, sqlConnection) : new SqlCommand("insert into Accounts_Index(Code,AName,Type,ParentCode,FinalAcc,Acc_branch,Nature,IValue,UserName,date,CostCenter,IsDeleted) values (N'" + Account.Code + "',N'" + Account.AccName + "'," + Conversions.ToString(Account.AccType) + ",N'" + Account.ParentCode + "'," + Conversions.ToString(Account.FinalAcc) + "," + Conversions.ToString(Account.BranchId) + "," + Conversions.ToString(1) + "," + Conversions.ToString(0) + "," + Conversions.ToString(Account.EmpId) + ",@date," + Conversions.ToString(Account.CostCenter) + ",0)", sqlConnection));
                    sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                    sqlCommand.ExecuteNonQuery();
                }
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<TreeAccount> ReadAccounts()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<TreeAccount> list;
        list = new List<TreeAccount>();
        checked
        {
            List<TreeAccount> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter(" Select Code,AName,isnull(Type,2)as type,ParentCode,Isnull(FinalAcc,1)as FinalAcc,isnull(Acc_branch,1)as Acc_branch,IsNull(Nature,0)as Nature,isNull(IValue,0)as IValue ,isNull(UserName,1)as UserName,isnull(date,GETDATE()) as date,isnull(CostCenter,0)as CostCenter,IsDeleted from Accounts_Index where ISDeleted=0", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                new TreeAccount();
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    try
                    {
                        TreeAccount treeAccount;
                        treeAccount = new TreeAccount();
                        treeAccount.AccName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["AName"]));
                        treeAccount.AccNature = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["Nature"]));
                        treeAccount.Code = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Code"]));
                        treeAccount.AccountID = Conversions.ToInteger(treeAccount.Code);
                        treeAccount.ParentCode = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["ParentCode"]));
                        treeAccount.IntialBalance = Conversions.ToDecimal(Operators.ConcatenateObject("", dataTable.Rows[i]["IValue"]));
                        if (Operators.ConditionalCompareObjectEqual(Operators.ConcatenateObject("", dataTable.Rows[i]["UserName"]), "", TextCompare: false))
                        {
                            treeAccount.EmpId = 1;
                        }
                        else
                        {
                            treeAccount.EmpId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["UserName"]));
                        }
                        treeAccount.AccType = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["Type"]));
                        treeAccount.IsDeleted = Conversions.ToBoolean(Operators.ConcatenateObject("", dataTable.Rows[i]["IsDeleted"]));
                        treeAccount.FinalAcc = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["FinalAcc"]));
                        treeAccount.LastUpdateDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["Date"]));
                        treeAccount.CreateDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["Date"]));
                        treeAccount.ClientCode = Sync.ClientCode;
                        treeAccount.CostCenter = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["CostCenter"]));
                        treeAccount.BranchId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["Acc_branch"]));
                        list.Add(treeAccount);
                    }
                    catch (Exception projectError)
                    {
                        ProjectData.SetProjectError(projectError);
                        ProjectData.ClearProjectError();
                    }
                }
                result = list;
            }
            catch (Exception projectError2)
            {
                ProjectData.SetProjectError(projectError2);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public async void PostProducts()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        if (sqlConnection.State != ConnectionState.Open)
        {
            sqlConnection.Open();
        }
        new SqlCommand();
        checked
        {
            int num;
            num = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", new SqlCommand("select ISNUll(count(*),0) as ItemsNo from [Items] ", sqlConnection).ExecuteScalar())));
            int i;
            i = 0;
            ItemOper itemOper;
            itemOper = new ItemOper();
            for (; i < num; i += 100)
            {
                List<Product> products;
                products = itemOper.BindingProducts(i);
                ProductCRUD productCRUD;
                productCRUD = new ProductCRUD(Sync.APIUrl);
                if (!Sync.ValidAPIUrl || await productCRUD.PostProductsOnlineManually(products))
                {
                }
            }
            Interaction.MsgBox("تمت مزامنة الأصناف بنجاح ");
        }
    }

    public bool SaveEmployee(List<Employee> employees, bool AddedLocally, List<BranchEmployee> EmbBranches)
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        bool result;
        try
        {
            if (sqlConnection.State != ConnectionState.Open)
            {
                sqlConnection.Open();
            }
            new SqlCommand();
            foreach (Employee employee in employees)
            {
                if (employee == null)
                {
                    continue;
                }
                SqlCommand sqlCommand;
                sqlCommand = new SqlCommand("Select COUNT(*) from Employees where id= " + Conversions.ToString(employee.EmpId), sqlConnection);
                if (Convert.ToDouble(Operators.ConcatenateObject("", sqlCommand.ExecuteScalar())) == 0.0)
                {
                    sqlCommand = new SqlCommand("SET IDENTITY_INSERT [dbo].[Employees] ON insert into Employees(id,name,manag,dep,state,job,birth_date,insurance_no,work_date,marital_state,nationality,sex,tel,mobile,email,address,notes,image,salary_basic,house,travel,food,medical,salary_add,salary_other,IS_Deleted,AccCode,CardNo,BankNo,BankName) values (@id,@name,@manag,@dep,@state,@job,@birth_date,@insurance_no,@work_date,@marital_state,@nationality,@sex,@tel,@mobile,@email,@address,@notes,@image,@salary_basic,@house,@travel,@food,@medical,@salary_add,@salary_other,@IS_Deleted,@AccCode,@CardNo,@BankNo,@BankName)  SET IDENTITY_INSERT [dbo].[Employees] off", sqlConnection);
                }
                else if (AddedLocally)
                {
                    new SqlCommand("delete from EmpBranches where emp=" + Conversions.ToString(employee.EmpId), sqlConnection).ExecuteNonQuery();
                    foreach (BranchEmployee EmbBranch in EmbBranches)
                    {
                        new SqlCommand("insert into EmpBranches(emp,branch)values(" + EmbBranch.EmployeeId + "," + Conversions.ToString(EmbBranch.BranchId) + ")", sqlConnection).ExecuteNonQuery();
                    }
                    sqlCommand = new SqlCommand("update Employees set name=@name ,manag=@manag ,dep=@dep ,state=@state ,job=@job  ,birth_date=@birth_date ,insurance_no=@insurance_no ,work_date=@work_date ,marital_state=@marital_state ,nationality=@nationality ,sex=@sex ,tel=@tel ,mobile=@mobile ,email=@email ,address=@address ,notes=@notes ,image=@image ,salary_basic=@salary_basic ,house=@house, travel=@travel, food=@food, medical=@medical, salary_add=@salary_add ,salary_other=@salary_other ,IS_Deleted=@IS_Deleted ,AccCode=@AccCode , CardNo=@CardNo , BankNo=@BankNo , BankName=@BankName where id=" + Conversions.ToString(employee.EmpId), sqlConnection);
                }
                sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = employee.EmpId;
                sqlCommand.Parameters.Add("@name", SqlDbType.NVarChar).Value = employee.EmpName;
                sqlCommand.Parameters.Add("@manag", SqlDbType.Int).Value = -1;
                sqlCommand.Parameters.Add("@dep", SqlDbType.Int).Value = employee.Department;
                sqlCommand.Parameters.Add("@state", SqlDbType.Int).Value = employee.Status;
                sqlCommand.Parameters.Add("@job", SqlDbType.Int).Value = employee.Role;
                sqlCommand.Parameters.Add("@birth_date", SqlDbType.DateTime).Value = employee.BrithDate;
                sqlCommand.Parameters.Add("@insurance_no", SqlDbType.NVarChar).Value = employee.InsuranceNo;
                sqlCommand.Parameters.Add("@work_date", SqlDbType.DateTime).Value = employee.JobStartDate;
                sqlCommand.Parameters.Add("@marital_state", SqlDbType.Int).Value = employee.MaritalStatus;
                sqlCommand.Parameters.Add("@nationality", SqlDbType.Int).Value = employee.Nationality;
                sqlCommand.Parameters.Add("@sex", SqlDbType.Char).Value = employee.Gender;
                sqlCommand.Parameters.Add("@tel", SqlDbType.NVarChar).Value = employee.EmpTel;
                sqlCommand.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = employee.EmpMobile;
                sqlCommand.Parameters.Add("@email", SqlDbType.NVarChar).Value = employee.EmpEmail;
                sqlCommand.Parameters.Add("@address", SqlDbType.NVarChar).Value = employee.EmpAddress;
                sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                sqlCommand.Parameters.Add("@image", SqlDbType.Image).Value = DBNull.Value;
                sqlCommand.Parameters.Add("@salary_basic", SqlDbType.Float).Value = employee.Salary;
                sqlCommand.Parameters.Add("@house", SqlDbType.Float).Value = employee.Housing;
                sqlCommand.Parameters.Add("@travel", SqlDbType.Float).Value = employee.Travel;
                sqlCommand.Parameters.Add("@food", SqlDbType.Float).Value = 0;
                sqlCommand.Parameters.Add("@medical", SqlDbType.Float).Value = 0;
                sqlCommand.Parameters.Add("@salary_add", SqlDbType.Float).Value = 0;
                sqlCommand.Parameters.Add("@salary_other", SqlDbType.Float).Value = employee.salary_other;
                sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = employee.IsDeleted;
                sqlCommand.Parameters.Add("@AccCode", SqlDbType.NVarChar).Value = employee.AccCode;
                sqlCommand.Parameters.Add("@CardNo", SqlDbType.NVarChar).Value = ((employee.CardNo == null || (object)employee.CardNo == DBNull.Value) ? "" : employee.CardNo);
                sqlCommand.Parameters.Add("@BankNo", SqlDbType.NVarChar).Value = ((employee.BankNo == null || (object)employee.BankNo == DBNull.Value) ? "" : employee.BankNo);
                sqlCommand.Parameters.Add("@BankName", SqlDbType.NVarChar).Value = ((employee.BankName == null || (object)employee.BankName == DBNull.Value) ? "" : employee.BankName);
                sqlCommand.ExecuteNonQuery();
            }
            result = true;
        }
        catch (Exception projectError)
        {
            ProjectData.SetProjectError(projectError);
            result = false;
            ProjectData.ClearProjectError();
        }
        finally
        {
            if (sqlConnection.State != ConnectionState.Closed)
            {
                sqlConnection.Close();
            }
        }
        return result;
    }

    public List<Employee> ReadEmployee()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<Employee> list;
        list = new List<Employee>();
        checked
        {
            List<Employee> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                new SqlCommand();
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("Select * from Employees where IS_Deleted=0 ", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    Employee employee;
                    employee = new Employee();
                    employee.EmpId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["id"]));
                    employee.EmpName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["name"]));
                    employee.Department = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["dep"]));
                    employee.Status = Conversions.ToBoolean(Operators.ConcatenateObject("", dataTable.Rows[i]["state"]));
                    employee.Role = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["job"]));
                    employee.BrithDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["birth_date"]));
                    employee.InsuranceNo = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["insurance_no"]));
                    employee.JobStartDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["work_date"]));
                    employee.LastUpdateDate = DateTime.Now;
                    employee.MaritalStatus = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["marital_state"]));
                    employee.NationalId = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["nationality"]));
                    if (Operators.ConditionalCompareObjectEqual(Operators.ConcatenateObject("", dataTable.Rows[i]["sex"]), "M", TextCompare: false))
                    {
                        employee.Gender = 1;
                    }
                    else if (Operators.ConditionalCompareObjectEqual(Operators.ConcatenateObject("", dataTable.Rows[i]["sex"]), "F", TextCompare: false))
                    {
                        employee.Gender = 2;
                    }
                    else
                    {
                        employee.Gender = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["sex"]));
                    }
                    employee.EmpTel = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["tel"]));
                    employee.EmpMobile = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["mobile"]));
                    employee.EmpEmail = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["email"]));
                    employee.EmpAddress = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["address"]));
                    employee.Salary = Conversions.ToDecimal(Operators.ConcatenateObject("", dataTable.Rows[i]["salary_basic"]));
                    employee.IsDeleted = Conversions.ToBoolean(Operators.ConcatenateObject("", dataTable.Rows[i]["IS_Deleted"]));
                    employee.BranchId = MainClass.BranchNo;
                    employee.ClientCode = Sync.ClientCode;
                    employee.AccCode = Conversions.ToDecimal(Operators.ConcatenateObject("", dataTable.Rows[i]["AccCode"]));
                    list.Add(employee);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public List<UserTreasury> ReadTreasuryuser()
    {
        SqlConnection sqlConnection;
        sqlConnection = MainClass.ConnObj();
        List<UserTreasury> list;
        list = new List<UserTreasury>();
        checked
        {
            List<UserTreasury> result;
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                new SqlCommand();
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("select * from Stock_Emps", sqlConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    UserTreasury userTreasury;
                    userTreasury = new UserTreasury();
                    userTreasury.TreasuryId = Conversions.ToInteger(dataTable.Rows[i]["stock_id"]);
                    userTreasury.UserId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["emp_id"]));
                    userTreasury.ClientCode = Sync.ClientCode;
                    list.Add(userTreasury);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            finally
            {
                if (sqlConnection.State != ConnectionState.Closed)
                {
                    sqlConnection.Close();
                }
            }
            return result;
        }
    }

    public List<UserWarehouse> ReadInvertoryuser()
    {
        SqlConnection selectConnection;
        selectConnection = MainClass.ConnObj();
        List<UserWarehouse> list;
        list = new List<UserWarehouse>();
        checked
        {
            List<UserWarehouse> result;
            try
            {
                SqlDataAdapter sqlDataAdapter;
                sqlDataAdapter = new SqlDataAdapter("select * from Safe_Emps", selectConnection);
                DataTable dataTable;
                dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                int num;
                num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    UserWarehouse userWarehouse;
                    userWarehouse = new UserWarehouse();
                    userWarehouse.UserId = Conversions.ToInteger(dataTable.Rows[i]["emp_id"]);
                    userWarehouse.ClientCode = Sync.ClientCode;
                    userWarehouse.WarehouseId = Conversions.ToInteger(dataTable.Rows[i]["safe_id"]);
                    list.Add(userWarehouse);
                }
                result = list;
            }
            catch (Exception projectError)
            {
                ProjectData.SetProjectError(projectError);
                result = list;
                ProjectData.ClearProjectError();
            }
            return result;
        }
    }
}
}