using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Ws_Auditor.CashierSerivce;

namespace SmartAuditERP
{

	public class User
	{
		public static int Id { get; set; }

		public static int TreasuryID { get; set; } = 1;

		public static int InvertoryId { get; set; } = 1;

		public static bool BuyLessAvrgCost { get; set; } = false;

		public static bool EditInvDate { get; set; } = false;

		public static bool EditPrice { get; set; } = false;

		public static bool DisableDiscPassword { get; set; } = false;

		public static bool ShowCosts { get; set; } = false;

		public static bool PassDefDiscount { get; set; } = false;

		public static bool PassHeighestSalePrice { get; set; } = true;

		public static bool PassLowestSalePrice { get; set; } = false;

		public static bool PassHeighestPurchasePrice { get; set; } = false;

		public static bool PassLowestPurchasePrice { get; set; } = false;

		public static bool ShowBranchsAccounts { get; set; } = false;

		public static bool ShowItemsInvertory { get; set; } = false;

		public static bool EditItemInfo { get; set; } = false;

		public static string TreasuryAcc { get; set; } = "";

		public static CashierAppLoggedInUser CurrentCloudUser { get; set; }

		public static void LoadUserOperPermission()
		{
			User.ResetPermissions();
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			Accounting.BranchCondition = " and (acc_branch=" + Conversions.ToString(MainClass.BranchNo) + " or acc_branch is null )";
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select OperNo,OperVal,IsNull(Activated,0)as Activated  from OperationPermission where emp=" + Conversions.ToString(MainClass.EmpNo) + " and IS_Deleted=0", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						if (Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["Activated"])))
						{
							double num2;
							num2 = Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["OperNo"]));
							if (num2 == 8.0)
							{
								User.PassHeighestSalePrice = true;
							}
							else if (num2 == 9.0)
							{
								User.PassLowestSalePrice = true;
							}
							else if (num2 == 10.0)
							{
								User.BuyLessAvrgCost = true;
							}
							else if (num2 == 11.0)
							{
								User.PassHeighestPurchasePrice = true;
							}
							else if (num2 == 12.0)
							{
								User.PassLowestPurchasePrice = true;
							}
							else if (num2 == 13.0)
							{
								User.EditPrice = true;
							}
							else if (num2 == 14.0)
							{
								User.EditInvDate = true;
							}
							else if (num2 == 15.0)
							{
								User.ShowCosts = true;
							}
							else if (num2 == 16.0)
							{
								User.PassDefDiscount = true;
							}
							else if (num2 == 17.0)
							{
								User.ShowBranchsAccounts = true;
								Accounting.BranchCondition = " ";
							}
							else if (num2 == 18.0)
							{
								User.ShowItemsInvertory = true;
							}
							else if (num2 == 19.0)
							{
								User.EditItemInfo = true;
							}
							else if (num2 == 20.0)
							{
								User.DisableDiscPassword = true;
							}
						}
					}
				}
				dataTable = LoadData.Invertories(MainClass.EmpNo);
				if (dataTable.Rows.Count > 0)
				{
					User.InvertoryId = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["id"])));
				}
				dataTable = LoadData.Treasury();
				if (dataTable.Rows.Count > 0)
				{
					User.TreasuryID = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["id"])));
					if (Operators.ConditionalCompareObjectNotEqual(Operators.ConcatenateObject("", dataTable.Rows[0]["Acc_Code"]), "", TextCompare: false))
					{
						User.TreasuryAcc = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["Acc_Code"]));
					}
				}
			}
		}

		private static void ResetPermissions()
		{
			User.TreasuryID = 1;
			User.InvertoryId = 1;
			User.TreasuryAcc = "1211001";
			User.BuyLessAvrgCost = false;
			User.EditInvDate = false;
			User.EditPrice = false;
			User.ShowCosts = false;
			User.PassDefDiscount = false;
			User.PassHeighestSalePrice = true;
			User.PassLowestSalePrice = false;
			User.PassHeighestPurchasePrice = false;
			User.PassLowestPurchasePrice = false;
			User.ShowBranchsAccounts = false;
			User.ShowItemsInvertory = false;
			User.EditItemInfo = false;
			User.DisableDiscPassword = false;
		}
	}
}