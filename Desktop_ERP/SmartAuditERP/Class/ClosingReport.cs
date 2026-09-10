using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace SmartAuditERP
{
	
	public class ClosingReport
	{
		private SqlConnection conn;

		private SqlConnection conn1;

		private string Username;

		public ClosingReport()
		{
			this.conn = MainClass.ConnObj();
			this.conn1 = MainClass.ConnObj();
			this.Username = "";
		}

		public void SaveCasherClosedsToLocal(List<CasherClosedDto> list)
		{
			using SqlConnection sqlConnection = new SqlConnection(MainClass.connstr);
			sqlConnection.Open();
			using SqlTransaction sqlTransaction = sqlConnection.BeginTransaction();
			try
			{
				foreach (CasherClosedDto item in list)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("DELETE FROM CasherClosed WHERE GlobalID=@GlobalID", sqlConnection, sqlTransaction);
					sqlCommand.Parameters.AddWithValue("@GlobalID", item.GlobalID);
					sqlCommand.ExecuteNonQuery();
					SqlCommand sqlCommand2;
					sqlCommand2 = new SqlCommand("DELETE FROM CasherClosed_Sub WHERE GlobalID=@GlobalID", sqlConnection, sqlTransaction);
					sqlCommand2.Parameters.AddWithValue("@GlobalID", item.GlobalID);
					sqlCommand2.ExecuteNonQuery();
					SqlCommand sqlCommand3;
					sqlCommand3 = new SqlCommand("\r\n                        INSERT INTO CasherClosed (type,user_id,startTime,endTime,ToT,CasherValue,diff,GlobalID,branch,ClosedId)\r\n                        VALUES (@type,@user_id,@startTime,@endTime,@ToT,@CasherValue,@diff,@GlobalID,@branch,@ClosedId)", sqlConnection, sqlTransaction);
					sqlCommand3.Parameters.AddWithValue("@type", item.Type);
					sqlCommand3.Parameters.AddWithValue("@user_id", item.UserId);
					sqlCommand3.Parameters.AddWithValue("@startTime", item.StartTime);
					sqlCommand3.Parameters.AddWithValue("@endTime", item.EndTime);
					sqlCommand3.Parameters.AddWithValue("@ToT", item.ToT);
					sqlCommand3.Parameters.AddWithValue("@CasherValue", item.CasherValue);
					sqlCommand3.Parameters.AddWithValue("@diff", item.Diff);
					sqlCommand3.Parameters.AddWithValue("@GlobalID", item.GlobalID);
					sqlCommand3.Parameters.AddWithValue("@branch", item.Branch);
					sqlCommand3.Parameters.AddWithValue("@ClosedId", item.ClosedId);
					sqlCommand3.ExecuteNonQuery();
					foreach (CasherClosedSubDto casherClosedSub in item.CasherClosedSubs)
					{
						SqlCommand sqlCommand4;
						sqlCommand4 = new SqlCommand("\r\n                            INSERT INTO CasherClosed_Sub \r\n                            (ClosedId,CashTotal,SAfeNetVal,ReturnSum,NetworkSum,AdditionVal,PostPoneSales,PostPoneRet,InsurVal,Discount,AllVAT,HostingVal,Expenses,Purchases,ExtraTax,GlobalID,IsDeleted)\r\n                            VALUES (@ClosedId,@CashTotal,@SAfeNetVal,@ReturnSum,@NetworkSum,@AdditionVal,@PostPoneSales,@PostPoneRet,@InsurVal,@Discount,@AllVAT,@HostingVal,@Expenses,@Purchases,@ExtraTax,@GlobalID,0)", sqlConnection, sqlTransaction);
						sqlCommand4.Parameters.AddWithValue("@ClosedId", item.ClosedId);
						sqlCommand4.Parameters.AddWithValue("@CashTotal", casherClosedSub.CashTotal);
						sqlCommand4.Parameters.AddWithValue("@SAfeNetVal", casherClosedSub.SAfeNetVal);
						sqlCommand4.Parameters.AddWithValue("@ReturnSum", casherClosedSub.ReturnSum);
						sqlCommand4.Parameters.AddWithValue("@NetworkSum", casherClosedSub.NetworkSum);
						sqlCommand4.Parameters.AddWithValue("@AdditionVal", casherClosedSub.AdditionVal);
						sqlCommand4.Parameters.AddWithValue("@PostPoneSales", casherClosedSub.PostPoneSales);
						sqlCommand4.Parameters.AddWithValue("@PostPoneRet", casherClosedSub.PostPoneRet);
						sqlCommand4.Parameters.AddWithValue("@InsurVal", casherClosedSub.InsurVal);
						sqlCommand4.Parameters.AddWithValue("@Discount", casherClosedSub.Discount);
						sqlCommand4.Parameters.AddWithValue("@AllVAT", casherClosedSub.AllVAT);
						sqlCommand4.Parameters.AddWithValue("@HostingVal", casherClosedSub.HostingVal);
						sqlCommand4.Parameters.AddWithValue("@Expenses", casherClosedSub.Expenses);
						sqlCommand4.Parameters.AddWithValue("@Purchases", casherClosedSub.Purchases);
						sqlCommand4.Parameters.AddWithValue("@ExtraTax", casherClosedSub.ExtraTax);
						sqlCommand4.Parameters.AddWithValue("@GlobalID", casherClosedSub.GlobalID);
						sqlCommand4.ExecuteNonQuery();
					}
				}
				sqlTransaction.Commit();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				sqlTransaction.Rollback();
				throw;
			}
		}

		public async Task<List<CasherClosedDto>> FetchCasherClosedsFromApi(string clientCode)
		{
			string requestUri;
			requestUri = string.Format("{0}?clientCode={1}", Sync.APIUrl + "/api/CasherClosed", clientCode);
			using HttpClient httpClient = new HttpClient();
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await httpClient.GetAsync(requestUri);
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				return JsonConvert.DeserializeObject<List<CasherClosedDto>>(await httpResponseMessage.Content.ReadAsStringAsync());
			}
			throw new Exception($"فشل في جلب البيانات من API: {httpResponseMessage.StatusCode}");
		}
	}
}