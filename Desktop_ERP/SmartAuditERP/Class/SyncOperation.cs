using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Ws_Auditor;

namespace SmartAuditERP
{

	public class SyncOperation
	{
		public async Task<bool> PostCardDataOnline()
		{
			try
			{
				bool result = default(bool);
				if (!Sync.ValidAPIUrl)
				{
					return result;
				}
				EntityOperations entityOperations;
				entityOperations = new EntityOperations();
				InvertoryCRUD invertoryCRUD;
				invertoryCRUD = new InvertoryCRUD(Sync.APIUrl);
				new List<Invertory>();
				List<Invertory> list;
				list = entityOperations.ReadInvertory();
				if (list.Count > 0)
				{
					await invertoryCRUD.AddInvertory(list);
				}
				BranchCRUD branchCRUD;
				branchCRUD = new BranchCRUD(Sync.APIUrl);
				new List<Branch>();
				List<Branch> list2;
				list2 = entityOperations.ReadBranch();
				if (list2.Count > 0)
				{
					await branchCRUD.AddBranch(list2);
				}
				return true;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				bool result;
				result = false;
				ProjectData.ClearProjectError();
				return result;
			}
		}

		public async Task ReadCardsDataCloud()
		{
			EntityOperations entityOperations;
			entityOperations = new EntityOperations();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			ClsCategory clsCategory;
			clsCategory = new ClsCategory(Sync.APIUrl, "");
			List<Category> list;
			list = new List<Category>();
			list.AddRange(clsCategory.GetCategories());
			entityOperations.SaveCategories(list);
			new ItemOper().ReadUnitsOnline();
			ClsCustomer clsCustomer;
			clsCustomer = new ClsCustomer(Sync.APIUrl, "");
			List<global::AuditorAPI.Models.Customer> list2;
			list2 = new List<global::AuditorAPI.Models.Customer>();
			list2.AddRange(clsCustomer.GetCustomers(MainClass.BranchNo));
			entityOperations.SaveCustomer(list2, AddedLocally: false);
			ClsItem clsItem;
			clsItem = new ClsItem(Sync.APIUrl, "");
			List<Product> list3;
			list3 = new List<Product>();
			if (((object)User.CurrentCloudUser != DBNull.Value) & (User.CurrentCloudUser != null))
			{
				list3.AddRange(clsItem.GetItems(User.CurrentCloudUser.UserID, MainClass.BranchNo, BranchSpecialSalePrice: true));
				new EntityOperations().SaveProducts(list3);
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				try
				{
					new SqlCommand();
					new SqlCommand("update SettingSync set CardsLastSync=N'" + Conversions.ToString(DateTime.Now) + "'", sqlConnection).ExecuteNonQuery();
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
				{
					Interaction.MsgBox("تم تحميل البيانات");
				}
				else
				{
					Interaction.MsgBox("Data has been uploaded");
				}
				Sync.CardsLastSync = DateTime.Now;
			}
			else
			{
				MessageBox.Show("الرجاء الدخول بمستخدم اخر");
			}
		}

		public async Task<bool> ReadStockOnline()
		{
			try
			{
				EntityOperations entityOperations;
				entityOperations = new EntityOperations();
				List<ProductStock> list;
				list = new List<ProductStock>();
				StockCRUD stockCRUD;
				stockCRUD = new StockCRUD(Sync.APIUrl);
				if (Sync.ActiveSync & (Sync.SyncType == 1))
				{
					if (Sync.BranchType == 4)
					{
						if (User.CurrentCloudUser == null)
						{
							return false;
						}
						list = new ClsItem(Sync.APIUrl, "").GetItemstocks(User.CurrentCloudUser.Store_ID.Value, MainClass.BranchNo, Sync.ClientCode);
					}
					else
					{
						list = (List<ProductStock>)(await stockCRUD.GetStocks(Sync.ClientCode));
					}
				}
				if (list.Count > 0)
				{
					entityOperations.SaveCurrentStocks(list);
				}
				return true;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				bool result;
				result = false;
				ProjectData.ClearProjectError();
				return result;
			}
		}

		public async Task ReadCardsDataOnline()
		{
			if (Sync.ValidAPIUrl)
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				EntityOperations entityOperations;
				entityOperations = new EntityOperations();
				BranchCRUD branchCRUD;
				branchCRUD = new BranchCRUD(Sync.APIUrl);
				new List<Branch>();
				List<Branch> list;
				list = (List<Branch>)(await branchCRUD.GetBranches(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list.Count > 0)
				{
					entityOperations.SaveBranch(list);
				}
				InvertoryCRUD invertoryCRUD;
				invertoryCRUD = new InvertoryCRUD(Sync.APIUrl);
				new List<Invertory>();
				List<Invertory> list2;
				list2 = (List<Invertory>)(await invertoryCRUD.GetInvertories(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list2.Count > 0)
				{
					entityOperations.SaveInvertory(list2, Addlocally: false);
				}
				EmployeeCRUD employeeCRUD;
				employeeCRUD = new EmployeeCRUD(Sync.APIUrl);
				new List<Employee>();
				List<Employee> list3;
				list3 = (List<Employee>)(await employeeCRUD.GetEmployees(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				new List<BranchEmployee>();
				List<BranchEmployee> embBranches;
				embBranches = (List<BranchEmployee>)(await branchCRUD.GetBranchEmployee(Sync.ClientCode));
				if (list3.Count > 0)
				{
					entityOperations.SaveEmployee(list3, AddedLocally: false, embBranches);
				}
				CustomerCRUD customerCRUD;
				customerCRUD = new CustomerCRUD(Sync.APIUrl);
				new List<global::AuditorAPI.Models.Customer>();
				List<global::AuditorAPI.Models.Customer> list4;
				list4 = (List<global::AuditorAPI.Models.Customer>)(await customerCRUD.GetCustomers(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list4.Count > 0)
				{
					entityOperations.SaveCustomer(list4, AddedLocally: false);
				}
				AccountCRUD accountCRUD;
				accountCRUD = new AccountCRUD(Sync.APIUrl);
				new List<TreeAccount>();
				List<TreeAccount> list5;
				list5 = (List<TreeAccount>)(await accountCRUD.GetTreeAccounts(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list5.Count > 0)
				{
					entityOperations.SaveAccounts(list5);
				}
				TreasuryCRUD treasuryCRUD;
				treasuryCRUD = new TreasuryCRUD(Sync.APIUrl);
				new List<global::AuditorAPI.Models.Treasury>();
				List<global::AuditorAPI.Models.Treasury> list6;
				list6 = (List<global::AuditorAPI.Models.Treasury>)(await treasuryCRUD.GetTreasurys(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list6.Count > 0)
				{
					entityOperations.SaveTreasuries(list6, AddedLocally: false);
				}
				BankCRUD bankCRUD;
				bankCRUD = new BankCRUD(Sync.APIUrl);
				new List<global::AuditorAPI.Models.Bank>();
				List<global::AuditorAPI.Models.Bank> list7;
				list7 = (List<global::AuditorAPI.Models.Bank>)(await bankCRUD.GetBanks(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				if (list7.Count > 0)
				{
					entityOperations.SaveBanks(list7, AddedLocally: false);
				}
				ProductCRUD productCRUD;
				productCRUD = new ProductCRUD(Sync.APIUrl);
				new List<Product>();
				List<Product> products;
				products = (List<Product>)(await productCRUD.GetProducts(Sync.ClientCode, Sync.BranchId, Sync.BranchType, Sync.CardsLastSync));
				new EntityOperations().SaveProducts(products);
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				try
				{
					new SqlCommand();
					new SqlCommand("update SettingSync set CardsLastSync=N'" + Conversions.ToString(DateTime.Now) + "'", sqlConnection).ExecuteNonQuery();
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				Sync.CardsLastSync = DateTime.Now;
			}
		}

		public double CalcItemStock(int StoreId, int itemID, int BranchID)
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			double result;
			result = 0.0;
			new SqlDataAdapter();
			DataTable dataTable;
			dataTable = new DataTable();
			new SqlDataAdapter("\r\n    SELECT ISNULL(SUM(stockIn - stockOut),0) AS ItemStock \r\n    FROM (\r\n        -- دخول مخزون\r\n        SELECT inv_sub.ItemId, SUM(inv_sub.val) AS stockIn, 0 AS stockOut\r\n        FROM inv\r\n        LEFT JOIN inv_sub ON inv.InvGlobalId = inv_sub.InvGlobalId\r\n        WHERE inv_sub.proc_type = 1 \r\n          AND inv_sub.ItemId = " + Conversions.ToString(itemID) + " \r\n          AND (" + Conversions.ToString(StoreId) + " IS NULL OR inv_sub.store = " + Conversions.ToString(StoreId) + ") \r\n          AND (" + Conversions.ToString(BranchID) + " IS NULL OR inv.branch = " + Conversions.ToString(BranchID) + ") \r\n          AND inv.IS_Deleted = 0\r\n        GROUP BY inv_sub.ItemId, inv_sub.store\r\n\r\n        UNION ALL\r\n\r\n        -- خروج مخزون\r\n        SELECT inv_sub.ItemId, 0 AS stockIn, SUM(inv_sub.val) AS stockOut\r\n        FROM inv\r\n        LEFT JOIN inv_sub ON inv.InvGlobalId = inv_sub.InvGlobalId\r\n        WHERE inv_sub.proc_type = 2\r\n          AND inv_sub.ItemId = " + Conversions.ToString(itemID) + "   -- ✅ هنا كان الخطأ\r\n          AND (" + Conversions.ToString(StoreId) + " IS NULL OR inv_sub.store = " + Conversions.ToString(StoreId) + ") \r\n          AND (" + Conversions.ToString(BranchID) + " IS NULL OR inv.branch = " + Conversions.ToString(BranchID) + ") \r\n          AND inv.IS_Deleted = 0\r\n        GROUP BY inv_sub.ItemId, inv_sub.store\r\n    ) AS tb\r\n    ", selectConnection).Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				result = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["ItemStock"]));
			}
			return result;
		}
	}
}