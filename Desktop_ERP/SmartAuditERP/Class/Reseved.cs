using System;
using System.Collections;
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

	public class Reseved
	{
		public class resvd
		{
			public string GlobalID;

			public int _id;

			public int BranchID;

			public string EntryGlobalID;

			public int ReceiptType;

			public bool State;

			public DateTime ReceiptDate;

			public bool ISDeleted;

			public string Notes;

			public string _name;

			public string CreditAcc;

			public string DebitAcc;

			public decimal _Basic;

			public decimal _Houses;

			public decimal _Travel;

			public decimal _saladd;

			public decimal _salsub;

			public decimal _Net;

			public string Cccode;

			public string Sync;

			public int _res_emp;

			public int _Month;

			public int _year;

			public resvd()
			{
				this.GlobalID = "";
				this._id = 0;
				this.BranchID = 1;
				this.EntryGlobalID = "";
				this.ReceiptType = 27;
				this.State = true;
				this.ReceiptDate = DateAndTime.Now.Date;
				this.ISDeleted = false;
				this.Notes = "";
				this._name = "";
				this.CreditAcc = "";
				this.DebitAcc = "";
				this._Basic = default(decimal);
				this._Houses = default(decimal);
				this._Travel = default(decimal);
				this._saladd = default(decimal);
				this._salsub = default(decimal);
				this._Net = default(decimal);
				this.Cccode = "";
				this.Sync = "";
				this._res_emp = 0;
				this._Month = 0;
				this._year = 0;
			}
		}

		public bool insertSalaryres()
		{
			new SqlCommand();
			new SqlDataAdapter();
			DataTable dataTable;
			dataTable = new DataTable();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("select GlobalID from Salary_Res where id=@id", sqlConnection);
			resvd resvd = default(resvd);
			sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = resvd._id;
			new SqlDataAdapter(sqlCommand).Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				sqlCommand = new SqlCommand("delete from Salary_Res where GlobalID=@GlobalID", sqlConnection);
				sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = resvd.GlobalID;
				sqlCommand.ExecuteNonQuery();
				sqlCommand = new SqlCommand("insert into Salary_Res(GlobalID,date,month,year,IS_Deleted,branch,notes,EntryGlobalID) values(@GlobalID,@date,@month,@year,@IS_Deleted,@branch,@notes,@EntryGlobalID)", sqlConnection);
				sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = resvd._id;
				sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = resvd.GlobalID;
				sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = resvd.ReceiptDate;
				sqlCommand.Parameters.Add("@month", SqlDbType.Int).Value = resvd._Month;
				sqlCommand.Parameters.Add("@year", SqlDbType.Int).Value = resvd._year;
				sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = resvd.ISDeleted;
				sqlCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = resvd.BranchID;
				sqlCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = resvd.Notes;
				sqlCommand.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = resvd.EntryGlobalID;
				sqlCommand.ExecuteNonQuery();
				Interaction.MsgBox("تمت العمليات بنجاح ");
			}
			return true;
		}

		public bool SaveReceipt1(resvd rs, Entry Entr, bool IsNew)
		{
			new SqlCommand();
			new SqlDataAdapter();
			DataTable dataTable;
			dataTable = new DataTable();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			bool result;
			try
			{
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand("select GlobalID from Salary_Res where id=@id", sqlConnection);
				sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = rs._id;
				new SqlDataAdapter(sqlCommand).Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					IsNew = false;
				}
				if (IsNew)
				{
					sqlCommand = new SqlCommand("insert into Salary_Res(id,GlobalID,date,month,year,IS_Deleted,branch,notes,EntryGlobalID) values(@id,@GlobalID,@date,@month,@year,@IS_Deleted,@branch,@notes,@EntryGlobalID)", sqlConnection);
					sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = rs._id;
					sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = rs.GlobalID;
					sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = rs.ReceiptDate;
					sqlCommand.Parameters.Add("@month", SqlDbType.Int).Value = rs._Month;
					sqlCommand.Parameters.Add("@year", SqlDbType.Int).Value = rs._year;
					sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = rs.ISDeleted;
					sqlCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = rs.BranchID;
					sqlCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = rs.Notes;
					sqlCommand.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = rs.EntryGlobalID;
					sqlCommand.ExecuteNonQuery();
				}
				else
				{
					sqlCommand = new SqlCommand("update  Salary_Res set date=@date,month=@month,year=@year,branch=@branch,notes=@notes,EntryGlobalID=@EntryGlobalID where GlobalID=@GlobalID", sqlConnection);
					sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = rs.GlobalID;
					sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = rs.ReceiptDate;
					sqlCommand.Parameters.Add("@month", SqlDbType.Int).Value = rs._Month;
					sqlCommand.Parameters.Add("@year", SqlDbType.Int).Value = rs._year;
					sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = rs.ISDeleted;
					sqlCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = rs.BranchID;
					sqlCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = rs.Notes;
					sqlCommand.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = rs.EntryGlobalID;
					sqlCommand.ExecuteNonQuery();
				}
				sqlCommand = new SqlCommand("select GlobalID from Salary_Res_Details where GlobalID=@GlobalID and res_emp=@res_emp", sqlConnection);
				sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = rs.GlobalID;
				sqlCommand.Parameters.Add("@res_emp", SqlDbType.Int).Value = rs._res_emp;
				new SqlDataAdapter();
				DataTable dataTable2;
				dataTable2 = new DataTable();
				bool flag;
				flag = true;
				new SqlDataAdapter(sqlCommand).Fill(dataTable2);
				if (dataTable2.Rows.Count > 0)
				{
					flag = false;
				}
				if (flag)
				{
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					sqlCommand = new SqlCommand("insert into Salary_Res_Details(GlobalID,res_emp,Basic,Houses,Travel,sal_add,sal_sub,Net) values(@GlobalID,@res_emp,@Basic,@Houses,@Travel,@sal_add,@sal_sub,@Net)", sqlConnection);
					sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = rs.GlobalID;
					sqlCommand.Parameters.Add("@res_emp", SqlDbType.Int).Value = rs._res_emp;
					sqlCommand.Parameters.Add("@Basic", SqlDbType.Float).Value = rs._Basic;
					sqlCommand.Parameters.Add("@Houses", SqlDbType.Float).Value = rs._Houses;
					sqlCommand.Parameters.Add("@Travel", SqlDbType.Float).Value = rs._Travel;
					sqlCommand.Parameters.Add("@sal_add", SqlDbType.Float).Value = rs._saladd;
					sqlCommand.Parameters.Add("@sal_sub", SqlDbType.Float).Value = rs._salsub;
					sqlCommand.Parameters.Add("@Net", SqlDbType.Float).Value = rs._Net;
				}
				else
				{
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					sqlCommand = new SqlCommand("update  Salary_Res_Details set res_emp=@res_emp,Basic=@Basic,Houses=@Houses,Travel=@Travel,sal_add=@sal_add,sal_sub=@sal_sub,Net=@Net where GlobalID=@GlobalID and res_emp=@res_emp ", sqlConnection);
					sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = rs.GlobalID;
					sqlCommand.Parameters.Add("@res_emp", SqlDbType.Int).Value = rs._res_emp;
					sqlCommand.Parameters.Add("@Basic", SqlDbType.Float).Value = rs._Basic;
					sqlCommand.Parameters.Add("@Houses", SqlDbType.Float).Value = rs._Houses;
					sqlCommand.Parameters.Add("@Travel", SqlDbType.Float).Value = rs._Travel;
					sqlCommand.Parameters.Add("@sal_add", SqlDbType.Float).Value = rs._saladd;
					sqlCommand.Parameters.Add("@sal_sub", SqlDbType.Float).Value = rs._salsub;
					sqlCommand.Parameters.Add("@Net", SqlDbType.Float).Value = rs._Net;
				}
				sqlCommand.ExecuteNonQuery();
				if (Entr != null && !new EntryOper().SaveEnty(Entr))
				{
					string text;
					text = "خطأ أثناء الحفظ";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text = "error in saving";
					}
					MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					result = false;
				}
				else
				{
					result = true;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				string text2;
				text2 = "خطأ أثناء حفظ القيد";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text2 = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text2 + Environment.NewLine + "Error details: " + ex2.Message) : (text2 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
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

		public Entry BindreservedToEntry(resvd Receipt, ArrayList dt)
		{
			checked
			{
				Entry result;
				try
				{
					Entry entry;
					entry = new Entry();
					List<Account> list;
					list = new List<Account>();
					new Account();
					new resvd();
					entry.EntryGlobalID = Receipt.EntryGlobalID;
					entry.ClientCode = Sync.ClientCode;
					entry.EntryNo = Conversions.ToInteger(Receipt.EntryGlobalID.Substring(Receipt.EntryGlobalID.LastIndexOf("-") + 1));
					entry.EntryDate = Receipt.ReceiptDate;
					entry.ReffNo = Conversions.ToString(Receipt._id);
					entry.RefDate = Receipt.ReceiptDate;
					entry.Type = unchecked((EntryType)Receipt.ReceiptType);
					entry.State = 1;
					entry.Note = Receipt.Notes;
					entry.Branch = Receipt.BranchID;
					entry.EmpID = MainClass.EmpNo;
					entry.DistBranch = Sync.DistBranch;
					entry.BranchType = Sync.BranchType;
					if (decimal.Compare(Receipt._Net, 0m) > 0)
					{
						int num;
						num = dt.Count - 1;
						for (int i = 0; i <= num; i++)
						{
							DataRow dataRow;
							dataRow = (DataRow)dt[i];
							Account account;
							account = new Account();
							account.EntryGlobalID = entry.EntryGlobalID;
							account.EntryNo = entry.EntryNo;
							account.Name = Conversions.ToString(dataRow["AccCode"]);
							account.Code = Conversions.ToString(dataRow["AccCode"]);
							account.Debt = 0.0;
							account.Credit = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataRow["Net_Sal"]));
							account.Note = Receipt.Notes;
							account.CCcode = Conversions.ToString(-1);
							list.Add(account);
							account = new Account();
							account.EntryGlobalID = entry.EntryGlobalID;
							account.EntryNo = entry.EntryNo;
							account.Name = Receipt.DebitAcc;
							account.Code = Receipt.DebitAcc;
							account.Debt = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataRow["Net_Sal"]));
							account.Credit = 0.0;
							account.Note = Receipt.Notes;
							account.CCcode = Receipt.Cccode;
							list.Add(account);
						}
					}
					entry.Accounts = list;
					result = entry;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2;
					ex2 = ex;
					string text;
					text = "error in Binding data";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text = "error in Binding data";
					}
					MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					result = null;
					ProjectData.ClearProjectError();
				}
				return result;
			}
		}
	}
}