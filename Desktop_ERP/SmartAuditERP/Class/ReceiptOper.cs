using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuditorAPI.Models;
using SmartAuditERP.Form_WPF;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class ReceiptOper
	{
		public static string[] filePathDocument;

		public Entry BindReceiptToEntry(Receipt Receipt)
		{
			Entry result;
			try
			{
				Entry entry;
				entry = new Entry();
				List<Account> list;
				list = new List<Account>();
				new Account();
				entry.EntryGlobalID = Receipt.EntryGlobalID;
				entry.ClientCode = Sync.ClientCode;
				entry.EntryNo = Conversions.ToInteger(Receipt.EntryGlobalID.Substring(checked(Receipt.EntryGlobalID.LastIndexOf("-") + 1)));
				entry.EntryDate = Receipt.ReceiptDate.Value;
				entry.ReffNo = Conversions.ToString(Receipt.ReceiptNo);
				entry.RefDate = Receipt.ReceiptDate.Value;
				entry.Type = (EntryType)Receipt.ReceiptType.Value;
				entry.State = 1;
				entry.Note = Receipt.Notes;
				entry.Branch = Receipt.BranchID.Value;
				entry.EmpID = Receipt.EmpId.Value;
				entry.DistBranch = Sync.DistBranch;
				entry.BranchType = Sync.BranchType;
				double? payment;
				payment = Receipt.Payment;
				if ((payment.HasValue ? new bool?(payment.GetValueOrDefault() > 0.0) : ((bool?)null)) == true)
				{
					Account account;
					account = new Account();
					account.EntryGlobalID = entry.EntryGlobalID;
					account.EntryNo = entry.EntryNo;
					account.Name = Receipt.CreditAcc;
					account.Code = Receipt.CreditAcc;
					account.Debt = 0.0;
					account.Credit = Receipt.NetVal.Value;
					account.Note = Receipt.Notes;
					int? receiptType;
					receiptType = Receipt.ReceiptType;
					bool? flag;
					flag = (receiptType.HasValue ? new bool?(receiptType == 5) : ((bool?)null));
					receiptType = Receipt.ReceiptType;
					bool? flag2;
					flag2 = (receiptType.HasValue ? new bool?(receiptType == 7) : ((bool?)null));
					if (((flag ?? false) ? new bool?(true) : ((!flag2.HasValue) ? ((bool?)null) : ((flag2 == true) | flag))) == true)
					{
						account.CCcode = Receipt.Cccode;
					}
					else
					{
						account.CCcode = Conversions.ToString(-1);
					}
					receiptType = Receipt.ReceiptType;
					if ((receiptType.HasValue ? new bool?(receiptType == 10) : ((bool?)null)) == true)
					{
						entry.Type = EntryType.ClientVoucher;
					}
					receiptType = Receipt.SalesManID;
					if ((receiptType.HasValue ? new bool?(receiptType != -1) : ((bool?)null)) == true)
					{
						account.salesman = Receipt.SalesManID.Value;
					}
					else
					{
						account.salesman = -1;
					}
					list.Add(account);
					account = new Account();
					account.EntryGlobalID = entry.EntryGlobalID;
					account.EntryNo = entry.EntryNo;
					account.Name = Receipt.DebitAcc;
					account.Code = Receipt.DebitAcc;
					account.Debt = Receipt.Payment.Value;
					account.Credit = 0.0;
					account.Note = Receipt.Notes;
					receiptType = Receipt.ReceiptType;
					bool? flag3;
					flag3 = (receiptType.HasValue ? new bool?(receiptType == 6) : ((bool?)null));
					receiptType = Receipt.ReceiptType;
					bool? flag4;
					flag4 = (receiptType.HasValue ? new bool?(receiptType == 8) : ((bool?)null));
					flag2 = ((flag3 ?? false) ? new bool?(true) : ((!flag4.HasValue) ? ((bool?)null) : ((flag4 == true) | flag3)));
					receiptType = Receipt.ReceiptType;
					flag = (receiptType.HasValue ? new bool?(receiptType == 9) : ((bool?)null));
					if (((flag2 ?? false) ? new bool?(true) : ((!flag.HasValue) ? ((bool?)null) : ((flag == true) | flag2))) == true)
					{
						account.CCcode = Receipt.Cccode;
					}
					else
					{
						account.CCcode = Conversions.ToString(-1);
					}
					receiptType = Receipt.SalesManID;
					if ((receiptType.HasValue ? new bool?(receiptType != -1) : ((bool?)null)) == true)
					{
						account.salesman = Receipt.SalesManID.Value;
					}
					else
					{
						account.salesman = -1;
					}
					list.Add(account);
					payment = Receipt.VAT;
					if ((payment.HasValue ? new bool?(payment.GetValueOrDefault() > 0.0) : ((bool?)null)) == true)
					{
						account = new Account();
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = "الضريبة المضافة ";
						account.Code = Receipt.VATCode;
						account.Debt = Receipt.VAT.Value;
						account.Credit = 0.0;
						account.Note = Receipt.Notes;
						account.CCcode = Conversions.ToString(-1);
						account.salesman = -1;
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

		public bool SaveReceipt(Receipt Receipt, Entry Entr, bool IsNew)
		{
			bool result;
			if (MainClass.IsTrial)
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				new SqlCommand();
				new SqlCommand();
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand("select max(id) from Entry ", sqlConnection);
				SqlCommand sqlCommand2;
				sqlCommand2 = new SqlCommand("select max(Proc_id) from Inv ", sqlConnection);
				if ((Conversion.Val(Operators.ConcatenateObject("", sqlCommand.ExecuteScalar())) >= 50.0) | (Conversion.Val(Operators.ConcatenateObject("", sqlCommand2.ExecuteScalar())) >= 100.0))
				{
					string text;
					text = "نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية، يمكنك شراء البرنامج وتفعيله من خلال بيانات الدعم الفني بالبرنامج";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text = "Sorry,You reach the maximum of entries, you can purchase the app and activate it from support data in the app";
					}
					MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					MainClass.IsTrial = true;
					result = false;
					goto IL_069c;
				}
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Close();
				}
			}
			new SqlCommand();
			SqlConnection sqlConnection2;
			sqlConnection2 = MainClass.ConnObj();
			if (sqlConnection2.State != ConnectionState.Open)
			{
				sqlConnection2.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection2.BeginTransaction();
			try
			{
				SqlCommand sqlCommand3;
				sqlCommand3 = ((!IsNew) ? new SqlCommand(StoredQueries.UpdateReceipt, sqlConnection2, sqlTransaction) : new SqlCommand(StoredQueries.InsertReceipt, sqlConnection2, sqlTransaction));
				sqlCommand3.Parameters.Add("@GlobalID", SqlDbType.VarChar).Value = Receipt.GlobalID;
				sqlCommand3.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = Receipt.ReceiptNo;
				sqlCommand3.Parameters.Add("@ReceiptDate", SqlDbType.DateTime).Value = Receipt.ReceiptDate;
				sqlCommand3.Parameters.Add("@EmpId", SqlDbType.Int).Value = Receipt.EmpId;
				sqlCommand3.Parameters.Add("@ClientID", SqlDbType.Int).Value = Receipt.ClientID;
				sqlCommand3.Parameters.Add("@state", SqlDbType.Int).Value = Receipt.State;
				sqlCommand3.Parameters.Add("@CreditAcc", SqlDbType.NVarChar).Value = Receipt.CreditAcc;
				sqlCommand3.Parameters.Add("@DebitAcc", SqlDbType.NVarChar).Value = Receipt.DebitAcc;
				sqlCommand3.Parameters.Add("@Payment", SqlDbType.Float).Value = Receipt.Payment;
				sqlCommand3.Parameters.Add("@VAT", SqlDbType.Float).Value = Receipt.VAT;
				sqlCommand3.Parameters.Add("@NetVal", SqlDbType.Float).Value = Receipt.NetVal;
				sqlCommand3.Parameters.Add("@BranchID", SqlDbType.Int).Value = Receipt.BranchID;
				sqlCommand3.Parameters.Add("@ReceiptType", SqlDbType.Int).Value = Receipt.ReceiptType;
				sqlCommand3.Parameters.Add("@PaymentType", SqlDbType.Int).Value = Receipt.PaymentType;
				sqlCommand3.Parameters.Add("@BankID", SqlDbType.Int).Value = Receipt.BankId;
				sqlCommand3.Parameters.Add("@TreasuryID", SqlDbType.Int).Value = Receipt.TreasuryID;
				sqlCommand3.Parameters.Add("@SalesManID", SqlDbType.Int).Value = Receipt.SalesManID;
				sqlCommand3.Parameters.Add("@CheckNo", SqlDbType.NVarChar).Value = Receipt.CheckNo;
				sqlCommand3.Parameters.Add("@Checkbank", SqlDbType.NVarChar).Value = Receipt.Checkbank;
				sqlCommand3.Parameters.Add("@CheckDate", SqlDbType.DateTime).Value = Receipt.CheckDate;
				sqlCommand3.Parameters.Add("@CheckState", SqlDbType.NVarChar).Value = Receipt.CheckState;
				sqlCommand3.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = Receipt.EntryGlobalID;
				sqlCommand3.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = Receipt.Notes;
				sqlCommand3.Parameters.Add("@Cccode", SqlDbType.NVarChar).Value = Receipt.Cccode;
				sqlCommand3.Parameters.Add("@ReffNo", SqlDbType.NVarChar).Value = Receipt.ReffNo;
				sqlCommand3.Parameters.Add("@Reffdate", SqlDbType.DateTime).Value = Receipt.Reffdate;
				sqlCommand3.Parameters.Add("@Sync", SqlDbType.Bit).Value = Receipt.Sync;
				sqlCommand3.Parameters.Add("@ISDeleted", SqlDbType.Bit).Value = Receipt.ISDeleted;
				sqlCommand3.Parameters.Add("@Recipient", SqlDbType.NVarChar).Value = Receipt.Recipient;
				sqlCommand3.ExecuteNonQuery();
				if (ReceiptOper.filePathDocument != null)
				{
					string[] array;
					array = ReceiptOper.filePathDocument;
					for (int i = 0; i < array.Length; i = checked(i + 1))
					{
						_ = array[i];
						ReceiptOper.insertDocument(Receipt.GlobalID);
					}
				}
				if (Entr == null)
				{
					goto IL_05d1;
				}
				if (new EntryOper().SaveEnty(Entr))
				{
					sqlTransaction.Commit();
					goto IL_05d1;
				}
				sqlTransaction.Rollback();
				string text2;
				text2 = "خطأ أثناء الحفظ";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text2 = "error in saving";
				}
				MessageBox.Show(text2, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				goto end_IL_013a;
			IL_05d1:
				result = true;
			end_IL_013a:;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text3;
				text3 = "خطأ أثناء حفظ القيد";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text3 = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text3 + Environment.NewLine + "Error details: " + ex2.Message) : (text3 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection2.State != ConnectionState.Closed)
				{
					sqlConnection2.Close();
				}
				sqlTransaction.Dispose();
			}
			goto IL_069c;
		IL_069c:
			return result;
		}

		public bool SaveReceipt2(Receipt Receipt)
		{
			new SqlCommand();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection.BeginTransaction();
			bool result;
			try
			{
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand(StoredQueries.InsertReceipt, sqlConnection, sqlTransaction);
				sqlCommand.Parameters.Add("@GlobalID", SqlDbType.VarChar).Value = Receipt.GlobalID;
				sqlCommand.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = Receipt.ReceiptNo;
				sqlCommand.Parameters.Add("@ReceiptDate", SqlDbType.DateTime).Value = Receipt.ReceiptDate;
				sqlCommand.Parameters.Add("@EmpId", SqlDbType.Int).Value = Receipt.EmpId;
				sqlCommand.Parameters.Add("@ClientID", SqlDbType.Int).Value = Receipt.ClientID;
				sqlCommand.Parameters.Add("@state", SqlDbType.Int).Value = Receipt.State;
				sqlCommand.Parameters.Add("@CreditAcc", SqlDbType.NVarChar).Value = Receipt.CreditAcc;
				sqlCommand.Parameters.Add("@DebitAcc", SqlDbType.NVarChar).Value = Receipt.DebitAcc;
				sqlCommand.Parameters.Add("@Payment", SqlDbType.Float).Value = Receipt.Payment;
				sqlCommand.Parameters.Add("@VAT", SqlDbType.Float).Value = Receipt.VAT;
				sqlCommand.Parameters.Add("@NetVal", SqlDbType.Float).Value = Receipt.NetVal;
				sqlCommand.Parameters.Add("@BranchID", SqlDbType.Int).Value = Receipt.BranchID;
				sqlCommand.Parameters.Add("@ReceiptType", SqlDbType.Int).Value = Receipt.ReceiptType;
				sqlCommand.Parameters.Add("@PaymentType", SqlDbType.Int).Value = Receipt.PaymentType;
				sqlCommand.Parameters.Add("@BankID", SqlDbType.Int).Value = Receipt.BankId;
				sqlCommand.Parameters.Add("@TreasuryID", SqlDbType.Int).Value = Receipt.TreasuryID;
				sqlCommand.Parameters.Add("@SalesManID", SqlDbType.Int).Value = Receipt.SalesManID;
				sqlCommand.Parameters.Add("@CheckNo", SqlDbType.NVarChar).Value = Receipt.CheckNo;
				sqlCommand.Parameters.Add("@Checkbank", SqlDbType.NVarChar).Value = Receipt.Checkbank;
				sqlCommand.Parameters.Add("@CheckDate", SqlDbType.DateTime).Value = Receipt.CheckDate;
				sqlCommand.Parameters.Add("@CheckState", SqlDbType.NVarChar).Value = Receipt.CheckState;
				sqlCommand.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = Receipt.EntryGlobalID;
				sqlCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = Receipt.Notes;
				sqlCommand.Parameters.Add("@Cccode", SqlDbType.NVarChar).Value = Receipt.Cccode;
				sqlCommand.Parameters.Add("@ReffNo", SqlDbType.NVarChar).Value = Receipt.ReffNo;
				sqlCommand.Parameters.Add("@Reffdate", SqlDbType.DateTime).Value = Receipt.Reffdate;
				sqlCommand.Parameters.Add("@Sync", SqlDbType.Bit).Value = Receipt.Sync;
				sqlCommand.Parameters.Add("@ISDeleted", SqlDbType.Bit).Value = Receipt.ISDeleted;
				sqlCommand.ExecuteNonQuery();
				sqlTransaction.Commit();
				result = true;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text;
				text = "خطأ اثناء عمل تحديث السندات ";
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

		public bool DeleteReceipt(string ReceiptGID, string EntryGID)
		{
			new SqlCommand();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection.BeginTransaction();
			string text;
			text = "";
			bool result = default(bool);
			try
			{
				if (Sync.ActiveSync & (Sync.SyncType > 0))
				{
					text = " , Sync=0 ";
				}
				new SqlCommand("update Receipts set ISDeleted=1 " + text + " where branchID=" + Conversions.ToString(MainClass.BranchNo) + " and GlobalID=N'" + ReceiptGID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
				new SqlCommand("update Entry set state=2 ,IS_Deleted=1 " + text + "  where branch=" + Conversions.ToString(MainClass.BranchNo) + " and GlobalID=N'" + EntryGID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
				sqlTransaction.Commit();
				result = true;
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				sqlTransaction.Rollback();
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
				sqlTransaction.Dispose();
			}
			return result;
		}

		public bool DeleteCreditNote(int Doc_No, int Doc_Type, string EntryGID)
		{
			new SqlCommand();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection.BeginTransaction();
			bool result = default(bool);
			try
			{
				new SqlCommand("update CreditDeptNotes set IS_Deleted=1 where Doc_Type=" + Conversions.ToString(Doc_Type) + " and  Doc_No=" + Conversions.ToString(Doc_No), sqlConnection, sqlTransaction).ExecuteNonQuery();
				new SqlCommand("update Entry set state=2 ,IS_Deleted=1 where branch=" + Conversions.ToString(MainClass.BranchNo) + " and GlobalID=N'" + EntryGID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
				sqlTransaction.Commit();
				result = true;
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				sqlTransaction.Rollback();
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
				sqlTransaction.Dispose();
			}
			return result;
		}

		public static int ReceiptNo(int ReceiptType)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			new SqlCommand();
			return checked((int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("select ISNULL(MAX(ReceiptNo), 0) from Receipts where ReceiptType=" + Conversions.ToString(ReceiptType) + " and branchID=" + Conversions.ToString(MainClass.BranchNo), sqlConnection).ExecuteScalar())) + 1.0));
		}

		public static string ReceipTypeName(int ReceiptType)
		{
			string result;
			result = "";
			switch (ReceiptType)
			{
				case 5:
					result = "سند قبض من عميل";
					break;
				case 6:
					result = "سند صرف لمورد";
					break;
				case 7:
					result = "سند قبض ";
					break;
				case 8:
					result = "سند صرف";
					break;
				case 9:
					result = "سند صرف ضريبة";
					break;
				case 26:
					result = "سند صرف راتب ";
					break;
				case 10:
					result = "سند قبض أندرويد";
					break;
			}
			return result;
		}

		public static DataTable LoadReceipts(string cond, DateTime StartDate, DateTime EndDate)
		{
			try
			{
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select Receipts.GlobalID, Receipts.ReceiptNo,Receipts.ReceiptDate,Receipts.Payment,Employees.name, Receipts.ClientID from Receipts,Employees  where Receipts.ISDeleted=0 and " + cond + "  Receipts.EmpId=Employees.id order by ReceiptNo ");
				sqlDataAdapter.SelectCommand.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = StartDate.Date + new TimeSpan(0, 0, 0);
				sqlDataAdapter.SelectCommand.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = EndDate.Date + new TimeSpan(23, 59, 59);
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				return dataTable;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				ProjectData.ClearProjectError();
			}
			return new DataTable();
		}

		public async Task SyncReceipt(Receipt receipt, Entry Eny, bool IsNew)
		{
			ReceiptCRUD receiptCRUD;
			receiptCRUD = new ReceiptCRUD(Sync.APIUrl);
			EntryCRUD entryCRUD;
			entryCRUD = new EntryCRUD(Sync.APIUrl);
			List<Receipt> list;
			list = new List<Receipt>();
			List<Entry> list2;
			list2 = new List<Entry>();
			Eny.Received = false;
			receipt.Received = false;
			if (Sync.ValidAPIUrl)
			{
				list = (List<Receipt>)(await receiptCRUD.PostReceiptsOnline(receipt, IsNew));
				list2 = (List<Entry>)(await entryCRUD.PostEntriesOnline(Eny, IsNew));
			}
			else
			{
				receiptCRUD.AddReceiptLocally(receipt, IsNew);
				entryCRUD.AddEntryLocally(Eny, IsNew);
			}
			new SqlCommand();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (list.Count > 0)
			{
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				foreach (Receipt item in list)
				{
					receipt = item;
					new SqlCommand("update Receipts set Sync=1 where GlobalID=N'" + receipt.GlobalID + "'", sqlConnection).ExecuteNonQuery();
				}
			}
			if (list2.Count <= 0)
			{
				return;
			}
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			foreach (Entry item2 in list2)
			{
				new SqlCommand("update Entry set Sync=1 where GlobalID=N'" + item2.EntryGlobalID + "'", sqlConnection).ExecuteNonQuery();
			}
		}

		public void ReadReceiptOnline(Receipt Receipt)
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
			bool flag;
			flag = true;
			try
			{
				if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Receipts where GlobalID= N'" + Receipt.GlobalID + "'", sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0)
				{
					flag = false;
				}
				SqlCommand sqlCommand;
				sqlCommand = ((!flag) ? new SqlCommand(StoredQueries.UpdateReceipt, sqlConnection, sqlTransaction) : new SqlCommand(StoredQueries.InsertReceipt, sqlConnection, sqlTransaction));
				sqlCommand.Parameters.Add("@GlobalID", SqlDbType.NVarChar).Value = Receipt.GlobalID;
				sqlCommand.Parameters.Add("@ReceiptNo", SqlDbType.Int).Value = Receipt.ReceiptNo;
				sqlCommand.Parameters.Add("@ReceiptDate", SqlDbType.DateTime).Value = Receipt.ReceiptDate;
				sqlCommand.Parameters.Add("@EmpId", SqlDbType.Int).Value = Receipt.EmpId;
				sqlCommand.Parameters.Add("@ClientID", SqlDbType.Int).Value = Receipt.ClientID;
				sqlCommand.Parameters.Add("@state", SqlDbType.Int).Value = Receipt.State;
				sqlCommand.Parameters.Add("@CreditAcc", SqlDbType.NVarChar).Value = Receipt.CreditAcc;
				sqlCommand.Parameters.Add("@DebitAcc", SqlDbType.NVarChar).Value = Receipt.DebitAcc;
				sqlCommand.Parameters.Add("@Payment", SqlDbType.Float).Value = Receipt.Payment;
				sqlCommand.Parameters.Add("@VAT", SqlDbType.Float).Value = Receipt.VAT;
				sqlCommand.Parameters.Add("@NetVal", SqlDbType.Float).Value = Receipt.NetVal;
				sqlCommand.Parameters.Add("@BranchID", SqlDbType.Int).Value = Receipt.BranchID;
				sqlCommand.Parameters.Add("@ReceiptType", SqlDbType.Int).Value = Receipt.ReceiptType;
				sqlCommand.Parameters.Add("@PaymentType", SqlDbType.Int).Value = Receipt.PaymentType;
				sqlCommand.Parameters.Add("@BankID", SqlDbType.Int).Value = Receipt.BankId;
				sqlCommand.Parameters.Add("@TreasuryID", SqlDbType.Int).Value = Receipt.TreasuryID;
				sqlCommand.Parameters.Add("@SalesManID", SqlDbType.Int).Value = Receipt.SalesManID;
				sqlCommand.Parameters.Add("@CheckNo", SqlDbType.NVarChar).Value = Receipt.CheckNo;
				sqlCommand.Parameters.Add("@Checkbank", SqlDbType.NVarChar).Value = Receipt.Checkbank;
				sqlCommand.Parameters.Add("@CheckDate", SqlDbType.DateTime).Value = Receipt.CheckDate;
				sqlCommand.Parameters.Add("@CheckState", SqlDbType.NVarChar).Value = Receipt.CheckState;
				sqlCommand.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = Receipt.EntryGlobalID;
				sqlCommand.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = Receipt.Notes;
				sqlCommand.Parameters.Add("@Cccode", SqlDbType.NVarChar).Value = Receipt.Cccode;
				sqlCommand.Parameters.Add("@ReffNo", SqlDbType.NVarChar).Value = Receipt.ReffNo;
				sqlCommand.Parameters.Add("@Reffdate", SqlDbType.DateTime).Value = Receipt.Reffdate;
				sqlCommand.Parameters.Add("@Sync", SqlDbType.Bit).Value = 1;
				sqlCommand.Parameters.Add("@ISDeleted", SqlDbType.Bit).Value = Receipt.ISDeleted;
				sqlCommand.Parameters.Add("@Recipient", SqlDbType.NVarChar).Value = Receipt.Recipient;
				sqlCommand.ExecuteNonQuery();
				if (string.IsNullOrEmpty(Receipt.EntryGlobalID))
				{
					int? receiptType;
					receiptType = Receipt.ReceiptType;
					if ((receiptType.HasValue ? new bool?(receiptType == 10) : ((bool?)null)) == true)
					{
						if (string.IsNullOrEmpty(Receipt.CreditAcc))
						{
							Receipt.CreditAcc = Conversions.ToString(ReceiptOper.GetCodeCustomerbyid(Receipt.ClientID.Value));
						}
						string EntryGlobalId;
						EntryGlobalId = "";
						int EntryNo;
						EntryNo = 0;
						new Entry();
						ReceiptOper receiptOper;
						receiptOper = new ReceiptOper();
						if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand(("Select COUNT(*) from Entry where type=5 and doc_no=" + Conversions.ToString(Receipt.ReceiptNo)) ?? "", sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0)
						{
							SqlDataReader sqlDataReader;
							sqlDataReader = new SqlCommand(("Select GlobalID from Entry where type=5 and doc_no=" + Conversions.ToString(Receipt.ReceiptNo)) ?? "", sqlConnection, sqlTransaction).ExecuteReader();
							sqlDataReader.Read();
							if (sqlDataReader.HasRows)
							{
								EntryGlobalId = sqlDataReader["GlobalID"].ToString();
							}
							sqlDataReader.Close();
						}
						if (string.IsNullOrEmpty(EntryGlobalId))
						{
							EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
						}
						Receipt.EntryGlobalID = EntryGlobalId;
						Entry entry;
						entry = receiptOper.BindReceiptToEntry(Receipt);
						if (!new EntryOper().SaveEnty(entry))
						{
							sqlTransaction.Rollback();
							string text;
							text = "خطأ أثناء الحفظ";
							if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
							{
								text = "error in saving";
							}
							MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
						}
					}
				}
				sqlTransaction.Commit();
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text2;
				text2 = "خطأ أثناء المزامنة";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text2 = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text2 + Environment.NewLine + "Error details: " + ex2.Message) : (text2 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
		}

		public Receipt BindReceiptByID(string GlID)
		{
			Receipt receipt;
			receipt = new Receipt();
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select * from Receipts where  GlobalID=N'" + GlID + "' ");
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				receipt.GlobalID = Conversions.ToString(dataTable.Rows[0]["GlobalID"]);
				receipt.EntryGlobalID = Conversions.ToString(dataTable.Rows[0]["EntryGlobalID"]);
				receipt.ReceiptNo = Conversions.ToInteger(dataTable.Rows[0]["ReceiptNo"]);
				receipt.BranchID = checked((int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["branchID"]))));
				receipt.ReceiptType = (int?)dataTable.Rows[0]["ReceiptType"];
				receipt.State = (int?)dataTable.Rows[0]["State"];
				receipt.Payment = (double?)dataTable.Rows[0]["Payment"];
				receipt.VAT = (double?)dataTable.Rows[0]["VAT"];
				receipt.NetVal = (double?)dataTable.Rows[0]["NetVal"];
				receipt.ReceiptDate = (DateTime?)dataTable.Rows[0]["ReceiptDate"];
				receipt.ReffNo = Conversions.ToString(dataTable.Rows[0]["ReffNo"]);
				receipt.Reffdate = (DateTime?)dataTable.Rows[0]["Reffdate"];
				receipt.ISDeleted = (bool?)dataTable.Rows[0]["ISDeleted"];
				receipt.Notes = Conversions.ToString(dataTable.Rows[0]["Notes"]);
				receipt.EmpId = (int?)dataTable.Rows[0]["EmpId"];
				receipt.CreditAcc = Conversions.ToString(dataTable.Rows[0]["CreditAcc"]);
				receipt.DebitAcc = Conversions.ToString(dataTable.Rows[0]["DebitAcc"]);
				receipt.ClientID = (int?)dataTable.Rows[0]["ClientID"];
				receipt.BankId = (int?)dataTable.Rows[0]["BankId"];
				receipt.TreasuryID = (int?)dataTable.Rows[0]["TreasuryID"];
				receipt.SalesManID = (int?)dataTable.Rows[0]["SalesManID"];
				receipt.PaymentType = (int?)dataTable.Rows[0]["PaymentType"];
				receipt.CheckState = (int?)dataTable.Rows[0]["CheckState"];
				receipt.CheckDate = (DateTime?)dataTable.Rows[0]["CheckDate"];
				receipt.CheckNo = Conversions.ToString(dataTable.Rows[0]["CheckNo"]);
				receipt.Checkbank = Conversions.ToString(dataTable.Rows[0]["Checkbank"]);
				receipt.Cccode = Conversions.ToString(dataTable.Rows[0]["Cccode"]);
				receipt.Sync = (bool?)dataTable.Rows[0]["Sync"];
				receipt.ClientCode = Sync.ClientCode;
				receipt.BranchType = Sync.BranchType;
				receipt.DistBranch = Sync.DistBranch;
			}
			return receipt;
		}

		public static void insertDocument(string InvGlobalID)
		{
			string docsRootPath;
			docsRootPath = Properties.Settings.Default.DocsRootPath;
			if (!Directory.Exists(docsRootPath))
			{
				Directory.CreateDirectory(docsRootPath);
			}
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			string text;
			text = docsRootPath;
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string[] array;
			array = ReceiptOper.filePathDocument;
			foreach (string text2 in array)
			{
				if (sqlConnection.State == ConnectionState.Open)
				{
					sqlConnection.Close();
				}
				sqlConnection.Open();
				string text3;
				text3 = Path.Combine(text, "Receipt" + Conversions.ToString(Convert.ToInt32(RuntimeHelpers.GetObjectValue(new SqlCommand("SELECT ISNULL(MAX(id), 0) + 1 FROM Documents", sqlConnection).ExecuteScalar()))) + Path.GetExtension(text2));
				string fileName;
				fileName = Path.GetFileName(text2);
				File.Copy(text2, text3, overwrite: true);
				using SqlCommand sqlCommand = new SqlCommand("INSERT INTO Documents (FileName, FileUrl,GlobalID,type) VALUES (@FileName, @FileUrl,@GlobalID,3)", sqlConnection);
				sqlCommand.Parameters.AddWithValue("@FileName", fileName);
				sqlCommand.Parameters.AddWithValue("@FileUrl", text3);
				sqlCommand.Parameters.AddWithValue("@GlobalID", InvGlobalID);
				sqlCommand.ExecuteNonQuery();
			}
			sqlConnection.Close();
		}

		public static object getdocuments(ref DataTable dtt, string InvGlobalID)
		{
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("SELECT Id,FileName, Fileurl FROM Documents WHERE type=3 and GlobalID = @GlobalID", sqlConnection);
			sqlCommand.Parameters.AddWithValue("@GlobalID", InvGlobalID);
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlCommand);
			DataTable dataTable;
			dataTable = new DataTable();
			try
			{
				sqlConnection.Open();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					dtt = dataTable;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ أثناء استرجاع البيانات: " + ex.Message);
				ProjectData.ClearProjectError();
			}
			finally
			{
				sqlConnection.Close();
			}
			object result = default(object);
			return result;
		}

		public static object GetCodeCustomerbyid(int id)
		{
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("SELECT AccountCode FROM Customers WHERE id=@id ", sqlConnection);
			sqlCommand.Parameters.AddWithValue("@id", id);
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlCommand);
			DataTable dataTable;
			dataTable = new DataTable();
			object result = default(object);
			try
			{
				sqlConnection.Open();
				sqlDataAdapter.Fill(dataTable);
				result = ((dataTable.Rows.Count <= 0) ? Conversions.ToInteger("-1") : Conversions.ToInteger(dataTable.Rows[0]["AccountCode"]));
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ أثناء استرجاع البيانات: " + ex.Message);
				ProjectData.ClearProjectError();
			}
			finally
			{
				sqlConnection.Close();
			}
			return result;
		}
	}
}