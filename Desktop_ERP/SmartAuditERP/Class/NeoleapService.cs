using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using neoleapconnector;
using neoleapconnector.Main;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{

	public class NeoleapService
	{
        private readonly ConnectionManagerFactory _connection;
        public NeoleapService(Action<string> notifier)
		{
			this._connection = ConnectionManagerFactory.GetInstance([SpecialName] (string msg) =>
			{
				notifier(msg);
			});
		}

		public async Task<TransactionResponse> ProcessSale(double amount, string merchantToken, string ecrRef = null)
		{
			try
			{
				RequestBuilder.Build build;
				build = RequestBuilder.Builder();
				build.requestType = "SALE";
				build.merchantToken = merchantToken;
				build.amount = amount;
				build.ecrRef = ecrRef ?? Guid.NewGuid().ToString();
				build.ecrToken = "";
				build.printFlag = "1";
				build.cashBack = 0.0;
				return this.ParseResponse(await this._connection.SendRequest(build));
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				TransactionResponse result;
				result = new TransactionResponse
				{
					Success = false,
					ErrorMessage = ex2.Message
				};
				ProjectData.ClearProjectError();
				return result;
			}
		}

		public async Task<TransactionResponse> TestConnection()
		{
			try
			{
				RequestBuilder.Build build;
				build = RequestBuilder.Builder();
				build.requestType = "SALE";
				build.merchantToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJCZXN0R2FzVGVzdCIsImlhdCI6MTc2NzA4Mjk3Mn0.m1RBo7kgOT3J0qFmeW2oevkgukukEklWw6C4njMfMS4";
				build.amount = 0.5;
				build.ecrRef = Guid.NewGuid().ToString();
				build.ecrToken = "";
				build.printFlag = "0";
				build.cashBack = 0.0;
				return this.ParseResponse(await this._connection.SendRequest(build));
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				TransactionResponse result;
				result = new TransactionResponse
				{
					Success = false,
					ErrorMessage = "Connection Test Failed: " + ex2.Message
				};
				ProjectData.ClearProjectError();
				return result;
			}
		}

		private TransactionResponse ParseResponse(string response)
		{
			TransactionResponse result;
			try
			{
				JObject jObject;
				jObject = JObject.Parse(response.Substring(response.IndexOf("{")));
				string text;
				text = jObject["ErrorMsg"]?.ToString() ?? "";
				if (!string.IsNullOrEmpty(text))
				{
					TransactionResponse transactionResponse;
					transactionResponse = new TransactionResponse();
					transactionResponse.Success = false;
					transactionResponse.StatusCode = jObject["StatusCode"]?.ToString();
					transactionResponse.ErrorMessage = text;
					transactionResponse.Uuid = jObject["uuid"]?.ToString();
					transactionResponse.RawResponse = jObject.ToString();
					result = transactionResponse;
				}
				else
				{
					JToken jToken;
					jToken = jObject["TransactionResult"];
					string text2;
					text2 = jToken?["StatusCode"]?.ToString();
					TransactionResponse transactionResponse;
					transactionResponse = new TransactionResponse();
					transactionResponse.StatusCode = text2;
					transactionResponse.Uuid = jObject["uuid"]?.ToString();
					transactionResponse.RawResponse = jObject.ToString();
					TransactionResponse transactionResponse2;
					transactionResponse2 = transactionResponse;
					switch (text2)
					{
						case "00":
							transactionResponse2.Success = true;
							transactionResponse2.Message = "Approved";
							break;
						case "01":
							transactionResponse2.Success = false;
							transactionResponse2.Message = "Declined";
							break;
						case "02":
							transactionResponse2.Success = false;
							transactionResponse2.Message = "Cancelled or Error";
							break;
						default:
							transactionResponse2.Success = false;
							transactionResponse2.Message = "Unknown Status";
							break;
					}
					if (jToken != null)
					{
						transactionResponse2.Amount = jToken["Amount"]?["PurchaseAmount"]?.ToString();
						transactionResponse2.ApprovalCode = jToken["ApprovalCode"]?.ToString();
						transactionResponse2.RRN = jToken["RRN"]?.ToString();
						transactionResponse2.STAN = jToken["STAN"]?.ToString();
						transactionResponse2.CardScheme = jToken["CardScheme"]?["English"]?.ToString();
						transactionResponse2.PAN = jToken["PAN"]?.ToString();
						transactionResponse2.TransactionType = jToken["TransactionType"]?["English"]?.ToString();
					}
					result = transactionResponse2;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				TransactionResponse transactionResponse;
				transactionResponse = new TransactionResponse();
				transactionResponse.Success = false;
				transactionResponse.ErrorMessage = $"Failed to parse response: {ex2.Message}";
				transactionResponse.RawResponse = response;
				result = transactionResponse;
				ProjectData.ClearProjectError();
			}
			return result;
		}
	}
}