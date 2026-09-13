using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{
	public class Accounting
	{
		public static string BranchCondition = " and (acc_branch=" + Conversions.ToString(MainClass.BranchNo) + " or acc_branch is null )";

		public static DataTable AccountTotalBalance(int? BranchID, DateTime ToDate, int AccType)
		{
			DataTable dataTable;
			dataTable = new DataTable();
			using (SqlConnection sqlConnection = MainClass.ConnObj())
			{
				using (SqlCommand sqlCommand = new SqlCommand("SELECT * FROM dbo.AccountTotalBalance(@branch, @date, @AccType)", sqlConnection))
				{
					sqlCommand.CommandType = CommandType.Text;
					sqlCommand.Parameters.Add("@branch", SqlDbType.Int).Value = RuntimeHelpers.GetObjectValue(BranchID.HasValue ? ((object)BranchID.Value) : DBNull.Value);
					sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = ToDate;
					sqlCommand.Parameters.Add("@AccType", SqlDbType.Int).Value = AccType;
					using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					sqlDataAdapter.Fill(dataTable);
				}
				if (sqlConnection.State == ConnectionState.Open)
				{
					sqlConnection.Close();
				}
			}
			return dataTable;
		}

		public static DataTable AccountsTotalBalance(int BranchID, DateTime Todate)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			checked
			{
				DataTable result = default(DataTable);
				try
				{
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					string text;
					text = "Select  * FROM dbo.AccountsTotalBalance(@branch,@date)";
					new SqlCommand();
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand(text, sqlConnection);
					sqlCommand.Parameters.Add(new SqlParameter("@branch", BranchID));
					sqlCommand.Parameters.Add(new SqlParameter("@date", Todate));
					new DataSet();
					DataSet dataSet;
					dataSet = new DataSet();
					new SqlDataAdapter();
					_ = (short)new SqlDataAdapter(sqlCommand).Fill(dataSet, text);
					result = dataSet.Tables[0];
					return result;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				finally
				{
					if (sqlConnection.State == ConnectionState.Open)
					{
						sqlConnection.Close();
					}
				}
				return result;
			}
		}

		public static DataTable BalanceSheet(int BranchID, DateTime Todate)
		{
			new DataTable();
			new DataTable();
			DataTable dataTable;
			dataTable = Accounting.AccountTotalBalance(BranchID, Todate, 1).Copy();
			DataTable dataTable2;
			dataTable2 = Accounting.AccountTotalBalance(BranchID, Todate, 2).Copy();
			double num;
			num = 0.0;
			double num2;
			num2 = 0.0;
			checked
			{
				int num3;
				num3 = dataTable2.Rows.Count - 1;
				for (int i = 0; i <= num3; i++)
				{
					double num4;
					double num5;
					if (Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Debet"])) >= Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Credit"])))
					{
						num4 = Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Debet"])) - Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Credit"]));
						num5 = 0.0;
					}
					else
					{
						num5 = Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Credit"])) - Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[i]["Debet"]));
						num4 = 0.0;
					}
					num += num4;
					num2 += num5;
				}
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				DataTable result = default(DataTable);
				try
				{
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					int num6;
					num6 = 2130001;
					new SqlDataAdapter();
					new DataTable();
					string text;
					text = "أرباح و خسائر";
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("select AName  from Accounts_Index where  Code =" + Conversions.ToString(2130001), sqlConnection);
					DataTable dataTable3;
					dataTable3 = new DataTable();
					sqlDataAdapter.Fill(dataTable3);
					if (dataTable3.Rows.Count > 0)
					{
						text = Conversions.ToString(dataTable3.Rows[0]["AName"]);
					}
					double num7;
					num7 = Inventory.InventoryCost(BranchID, Todate);
					num2 += num7;
					num += 0.0;
					DataRow[] array;
					array = dataTable.Select("AccountCode =" + Conversions.ToString(num6));
					if (array.Count() > 0)
					{
						num2 = Conversions.ToDouble(Operators.AddObject(num2, array[0]["Credit"]));
						num = Conversions.ToDouble(Operators.AddObject(num, array[0]["Debet"]));
					}
					if (num2 >= num)
					{
						if (array.Count() > 0)
						{
							array[0]["Debet"] = 0;
							array[0]["Credit"] = num2 - num;
						}
						else
						{
							dataTable.Rows.Add(num6, text, 0, num2 - num);
						}
					}
					else if (array.Count() > 0)
					{
						array[0]["Debet"] = num - num2;
						array[0]["Credit"] = 0;
					}
					else
					{
						dataTable.Rows.Add(num6, text, num - num2, 0);
					}
					num6 = 1270002;
					text = "حساب المخزون";
					sqlDataAdapter = new SqlDataAdapter("select AName  from Accounts_Index where  Code =" + Conversions.ToString(1270001), sqlConnection);
					dataTable3 = new DataTable();
					sqlDataAdapter.Fill(dataTable3);
					if (dataTable3.Rows.Count > 0)
					{
						text = Conversions.ToString(dataTable3.Rows[0]["AName"]);
					}
					DataRow[] array2;
					array2 = dataTable.Select("AccountCode =" + Conversions.ToString(num6));
					num2 = 0.0;
					num = num7;
					if (array2.Count() > 0)
					{
						num2 = Conversions.ToDouble(Operators.AddObject(num2, array2[0]["Credit"]));
						num = Conversions.ToDouble(Operators.AddObject(num, array2[0]["Debet"]));
					}
					if (num2 >= num)
					{
						if (array2.Count() > 0)
						{
							array2[0]["Debet"] = 0;
							array2[0]["Credit"] = num2 - num;
						}
						else
						{
							dataTable.Rows.Add(num6, text, 0, num2 - num);
						}
					}
					else if (array2.Count() > 0)
					{
						array2[0]["Debet"] = num - num2;
						array2[0]["Credit"] = 0;
					}
					else
					{
						dataTable.Rows.Add(num6, text, num - num2, 0);
					}
					result = dataTable;
					return result;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				finally
				{
					if (sqlConnection.State == ConnectionState.Open)
					{
						sqlConnection.Close();
					}
				}
				return result;
			}
		}
	}
}