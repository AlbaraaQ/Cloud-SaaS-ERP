using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmartAuditERP.Form_WPF;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using QLicense;

namespace SmartAuditERP
{

	[StandardModule]
	internal sealed class ManagerOnline
	{
		public static string connString = "server=" + MainClass.Server.Trim() + ";database=master;trusted_connection=True";

		public static SqlConnection conn = new SqlConnection(ManagerOnline.connString);

		private static SqlConnection conn1 = MainClass.ConnObj();

		private static string url = "https://app-cloud-rmxb.onrender.com";

		public static bool IsConnectedInternet()
		{
			bool result;
			try
			{
				result = new Ping().Send("8.8.8.8").Status == IPStatus.Success;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static async Task SendDataFoundationAsyncnew()
		{
			try
			{
				string requestUri;
				requestUri = ManagerOnline.url + "/api/Customer/Cust";
				HttpClient httpClient;
				httpClient = new HttpClient();
				List<ClListManagerOnline> foudationName;
				foudationName = ManagerOnline.GetFoudationName11();
				string lastEntry;
				lastEntry = ManagerOnline.GetLastEntry();
				if (foudationName == null || foudationName.Count == 0)
				{
					return;
				}
				foreach (ClListManagerOnline item in foudationName)
				{
					await (await httpClient.PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(new Dictionary<string, object>
				{
					{ "CompanyName", item.CompanyName },
					{
						"LastConnect",
						DateAndTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
					},
					{ "Mobile", item.Mobile },
					{ "City", item.City },
					{ "Salsman", item.Salsman },
					{ "Email", item.Email },
					{ "DeviceNo", item.DeviceNo },
					{
						"LicenseDate",
						item.LicenseDate.ToString("yyyy-MM-ddTHH:mm:ss")
					},
					{ "LastEntry", lastEntry },
					{ "DeviceId", item.licenseID },
					{ "LicenseFeatures", item.LicenseFeatures }
				}), Encoding.UTF8, "application/json"))).Content.ReadAsStringAsync();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static List<ClListManagerOnline> GetFoudationName11()
		{
			List<ClListManagerOnline> result;
			try
			{
				List<ClListManagerOnline> list;
				list = new List<ClListManagerOnline>();
				using (SqlConnection sqlConnection = new SqlConnection(ManagerOnline.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT\r\n                                        ISNULL([ClientName],'') AS ClientName,\r\n                                        ISNULL(ClientMobile,'0') AS ClientMobile,\r\n                                        ISNULL(ClientEmail,'-') AS ClientEmail,\r\n                                        ISNULL(city,'-') AS city,\r\n                                        ISNULL(Salesman,'DarH') AS Salesman,\r\n                                        ISNULL(LiciensesNo,1) AS LiciensesNo,\r\n                                        isnull(LicenseDate,'')as LicenseDate,\r\n                                        isnull(licenseID,'-')as licenseID,\r\n                                        isnull(LicenseFeatures,'000001') as LicenseFeatures\r\n                                    FROM License;", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ClListManagerOnline
						{
							CompanyName = sqlDataReader["ClientName"].ToString(),
							LastContact = DateAndTime.Now,
							City = sqlDataReader["city"].ToString(),
							Mobile = sqlDataReader["ClientMobile"].ToString(),
							DeviceNo = Conversions.ToInteger(sqlDataReader["LiciensesNo"]),
							Email = sqlDataReader["ClientEmail"].ToString(),
							Salsman = sqlDataReader["Salesman"].ToString(),
							LicenseDate = Conversions.ToDate(sqlDataReader["LicenseDate"]),
							licenseID = Conversions.ToString(sqlDataReader["licenseID"]),
							LicenseFeatures = Conversions.ToString(sqlDataReader["LicenseFeatures"])
						});
					}
				}
				result = list;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetClientName()
		{
			string result = default(string);
			try
			{
				if (MainClass.Conn_type == 2)
				{
					ManagerOnline.connString = "server=" + MainClass.Server.Trim() + ";database=master;MultipleActiveResultSets=true;user id=" + MainClass.NetUserId + "; pwd=" + MainClass.NetPwd;
				}
				using (SqlConnection sqlConnection = new SqlConnection(ManagerOnline.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT\r\n                                        ISNULL([ClientName],'') AS ClientName                                     \r\n                                    FROM License;", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					if (!sqlDataReader.Read())
					{
						goto end_IL_0082;
					}
					result = sqlDataReader["ClientName"].ToString();
					goto end_IL_0001;
				end_IL_0082:;
				}
				ManagerOnline.conn.Close();
			end_IL_0001:;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetClientEmail()
		{
			string result = default(string);
			try
			{
				if (MainClass.Conn_type == 2)
				{
					ManagerOnline.connString = "server=" + MainClass.Server.Trim() + ";database=master;MultipleActiveResultSets=true;user id=" + MainClass.NetUserId + "; pwd=" + MainClass.NetPwd;
				}
				using (SqlConnection sqlConnection = new SqlConnection(ManagerOnline.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT\r\n                                        ISNULL([ClientEmail],'') AS ClientEmail                                     \r\n                                    FROM License;", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					if (!sqlDataReader.Read())
					{
						goto end_IL_0082;
					}
					result = sqlDataReader["ClientEmail"].ToString();
					goto end_IL_0001;
				end_IL_0082:;
				}
				ManagerOnline.conn.Close();
			end_IL_0001:;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static async Task GetEndDateFromAPI(string syncCode)
		{
			try
			{
				string requestUri;
				requestUri = $"{ManagerOnline.url}/api/sync/getEndDate/{syncCode}";
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await new HttpClient().GetAsync(requestUri);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					Dictionary<string, object> dictionary;
					dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(await httpResponseMessage.Content.ReadAsStringAsync());
					if (dictionary.ContainsKey("endDate") && dictionary.ContainsKey("status"))
					{
						DateTime dateTime;
						dateTime = DateTime.Parse(dictionary["endDate"].ToString());
						bool flag;
						flag = bool.Parse(dictionary["status"].ToString());
						int num;
						num = ((DateTime.Compare(dateTime, DateAndTime.Now) >= 0 && flag) ? 1 : 0);
						if (num == 1)
						{
							Sync.ActiveSync = true;
						}
						else
						{
							Sync.ActiveSync = false;
						}
						if (ManagerOnline.conn1.State == ConnectionState.Open)
						{
							ManagerOnline.conn1.Close();
						}
						ManagerOnline.conn1.Open();
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand($"UPDATE SettingSync SET SyncType={num}, EndDate=@EndDate", ManagerOnline.conn1);
						sqlCommand.Parameters.AddWithValue("@EndDate", dateTime);
						sqlCommand.ExecuteNonQuery();
						ManagerOnline.conn1.Close();
						if (DateTime.Compare(dateTime, DateAndTime.Now) < 0)
						{
							MessageBox.Show(string.Format("✅  حالة المزامنة: {0}", (num == 1) ? "نشطة" : "متوقفة لأسباب إدارية") + "\r\n" + $"\ud83d\udcc5 تاريخ الانتهاء: {dateTime:yyyy-MM-dd}", "تم التحديث بنجاح");
						}
					}
				}
				else
				{
					ManagerOnline.sendSyncData();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static async Task GetLicenseLicenceEndDate(string CompanyName)
		{
			try
			{
				string requestUri;
				requestUri = $"{ManagerOnline.url}/api/Customer/getLicenceEndDate/{CompanyName}";
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await new HttpClient().GetAsync(requestUri);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					List<Dictionary<string, object>> list;
					list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(await httpResponseMessage.Content.ReadAsStringAsync());
					if (list != null && list.Count > 0)
					{
						DateTime dateTime;
						dateTime = DateTime.Parse(list[0]["endDate"].ToString());
						int days;
						days = (dateTime - DateTime.Now).Days;
						MainClass.LicenseExpire = Conversions.ToDate(dateTime.ToString("yyyy-MM-dd"));
						MainClass.RemainingDays = days;
						ManagerOnline.UpdateLicenseEndDate(dateTime);
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void UpdateLicenseEndDate(DateTime endDate)
		{
			try
			{
				if (ManagerOnline.conn.State != ConnectionState.Open)
				{
					ManagerOnline.conn.Open();
				}
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand("UPDATE License SET LicenseExpire = @LicenseExpire", ManagerOnline.conn);
				sqlCommand.Parameters.AddWithValue("@LicenseExpire", endDate);
				sqlCommand.ExecuteNonQuery();
				ManagerOnline.conn.Close();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void GetLicenseEndDate()
		{
			try
			{
				if (ManagerOnline.conn.State != ConnectionState.Open)
				{
					ManagerOnline.conn.Open();
				}
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter("Select LicenseExpire from License", ManagerOnline.conn);
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					MainClass.LicenseExpire = Conversions.ToDate(dataTable.AsEnumerable().ElementAtOrDefault(0)["LicenseExpire"].ToString());
				}
				ManagerOnline.conn.Close();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static async Task StartDailyCheck()
		{
			if (!MainClass.CheckForInternetConnection())
			{
				await DeviceLicenseManager.ShowtrialOfflin();
			}
			else
			{
				await ManagerOnline.OnLoginSucceeded();
			}
		}

		private static async Task OnLoginSucceeded()
		{
			DateTime today;
			today = DateTime.Today;
			if ((today - Properties.Settings.Default.LastDailyCheckDate.Date).TotalDays >= 30.0)
			{
				await ManagerOnline.CheckLicenseStatus();
				Properties.Settings.Default.LastDailyCheckDate = today;
				Properties.Settings.Default.Save();
			}
			await DeviceLicenseManager.ShowtrialOfflin();
		}

		public static async Task CheckLicenseStatus()
		{
			try
			{
				await ManagerOnline.SendDataFoundationAsyncnew();
				await ManagerOnline.SendZatcaActiveToAPI();
				await DeviceLicenseManager.CheckAndValidateLicense(ManagerOnline.url, ManagerOnline.GetClientName(), ManagerOnline.GetClientEmail());
				await ManagerOnline.GetzatcaEndDate(ManagerOnline.GetClientName());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static async Task sendSyncData()
		{
			List<ListSyncDataAuditorManager> syncDataFromDatabase;
			syncDataFromDatabase = ManagerOnline.GetSyncDataFromDatabase();
			if (syncDataFromDatabase != null && syncDataFromDatabase.Count > 0)
			{
				await ManagerOnline.SendSyncDataSYNCToAPI22(syncDataFromDatabase);
			}
		}

		private static async Task SendSyncDataSYNCToAPI22(List<ListSyncDataAuditorManager> syncList)
		{
			try
			{
				string requestUri;
				requestUri = ManagerOnline.url + "/api/sync/syncdata";
				using HttpClient httpClient = new HttpClient();
				string clientName;
				clientName = ManagerOnline.GetClientName();
				if (string.IsNullOrEmpty(clientName))
				{
					return;
				}
				List<object> list;
				list = new List<object>();
				foreach (ListSyncDataAuditorManager sync in syncList)
				{
					list.Add( (clientName, sync.SyncCode.ToString(), sync.StartDate.ToString("yyyy-MM-ddTHH:mm:ss"), sync.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"), sync.Status));
				}
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await httpClient.PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(list), Encoding.UTF8, "application/json")).ConfigureAwait(continueOnCapturedContext: false);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static List<ListSyncDataAuditorManager> GetSyncDataFromDatabase()
		{
			List<ListSyncDataAuditorManager> list;
			list = new List<ListSyncDataAuditorManager>();
			try
			{
				using (SqlCommand selectCommand = new SqlCommand("SELECT  ClientCode, startDate, endDate, SyncType FROM SettingSync", ManagerOnline.conn1))
				{
					using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					string clientName;
					clientName = ManagerOnline.GetClientName();
					foreach (DataRow row in dataTable.Rows)
					{
						ListSyncDataAuditorManager listSyncDataAuditorManager;
						listSyncDataAuditorManager = new ListSyncDataAuditorManager();
						listSyncDataAuditorManager.ClientName = clientName;
						listSyncDataAuditorManager.SyncCode = Conversions.ToInteger(row["ClientCode"]);
						listSyncDataAuditorManager.StartDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(row["startDate"]));
						listSyncDataAuditorManager.EndDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(row["endDate"]));
						listSyncDataAuditorManager.Status = Convert.ToInt32(RuntimeHelpers.GetObjectValue(row["SyncType"])) != 0;
						list.Add(listSyncDataAuditorManager);
					}
				}
				ManagerOnline.conn1.Close();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return list;
		}

		public static string GetLastEntry()
		{
			string result;
			try
			{
				string text;
				text = "-";
				using (SqlConnection sqlConnection = new SqlConnection(MainClass.connstr))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("select top 1 * from Entry where IS_Deleted=0 order by date desc;", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						if (sqlDataReader.HasRows)
						{
							text = Conversions.ToString(Operators.ConcatenateObject(Common.ResrirectionType(checked((int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", sqlDataReader["type"]))))) + " رقم  ", sqlDataReader["id"]));
						}
					}
				}
				result = text;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static List<ListZatacCrm> GetActiveZatca()
		{
			List<ListZatacCrm> result;
			try
			{
				List<ListZatacCrm> list;
				list = new List<ListZatacCrm>();
				using (SqlConnection sqlConnection = new SqlConnection(MainClass.connstr))
				{
					sqlConnection.Open();
					using SqlCommand selectCommand = new SqlCommand("select * from SettingZatca where IsActive=1;", ManagerOnline.conn1);
					using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					string clientName;
					clientName = ManagerOnline.GetClientName();
					foreach (DataRow row in dataTable.Rows)
					{
						list.Add(new ListZatacCrm
						{
							ClientName = clientName,
							IsActive = Conversions.ToBoolean(row["IsActive"]),
							DataBaseName = MainClass.DataBaseName,
							startDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(row["startDate"])),
							EndDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(row["EndDate"]))
						});
					}
				}
				result = list;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		private static async Task SendZatcaActiveToAPI()
		{
			try
			{
				string requestUri;
				requestUri = ManagerOnline.url + "/api/Customer/Zatca";
				List<ListZatacCrm> activeZatca;
				activeZatca = ManagerOnline.GetActiveZatca();
				using HttpClient httpClient = new HttpClient();
				string clientName;
				clientName = ManagerOnline.GetClientName();
				if (string.IsNullOrEmpty(clientName))
				{
					return;
				}
				List<object> list;
				list = new List<object>();
				foreach (ListZatacCrm item in activeZatca)
				{
					list.Add((clientName, item.IsActive, item.DataBaseName, item.startDate.ToString("yyyy-MM-ddTHH:mm:ss"), item.EndDate.ToString("yyyy-MM-ddTHH:mm:ss")));
				}
				if (list.Count != 0)
				{
					HttpResponseMessage httpResponseMessage;
					httpResponseMessage = await httpClient.PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(RuntimeHelpers.GetObjectValue(RuntimeHelpers.GetObjectValue(list[0]))), Encoding.UTF8, "application/json")).ConfigureAwait(continueOnCapturedContext: false);
					if (httpResponseMessage.IsSuccessStatusCode)
					{
						await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static async Task GetzatcaEndDate(string CompanyName)
		{
			try
			{
				string requestUri;
				requestUri = $"{ManagerOnline.url}/api/Customer/getzatca-EndDate/{CompanyName}?DataBaseName={MainClass.DataBaseName}";
				using HttpClient httpClient = new HttpClient();
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await httpClient.GetAsync(requestUri);
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					return;
				}
				CustZatcaEndDate custZatcaEndDate;
				custZatcaEndDate = JsonConvert.DeserializeObject<CustZatcaEndDate>(await httpResponseMessage.Content.ReadAsStringAsync());
				if (custZatcaEndDate != null)
				{
					DateTime endDate;
					endDate = DateTime.Parse(custZatcaEndDate.endDate);
					int remainingDays;
					remainingDays = custZatcaEndDate.remainingDays;
					switch (custZatcaEndDate.status)
					{
						case "Expired":
							ManagerOnline.UpdatezatcaEndDate(2, endDate);
							MessageBox.Show("نأسف، لقد انتهت صلاحية اشتراككم في النظام.\r\nتم تعليق بعض الوظائف مؤقت\u064bا، .\r\nيرجى تجديد الاشتراك لمواصلة استخدام النظام بشكل كامل.", "انتهاء الاشتراك", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
						case "ExpiringSoon":
							ManagerOnline.UpdatezatcaEndDate(1, endDate);
							MessageBox.Show($"⚠\ufe0f تنبيه: تبقى {remainingDays} يوم\u064bا فقط على انتهاء الاشتراك." + "\r\nقد يتم تعليق بعض الوظائف مؤقت\u064bا، .", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
						case "Active":
							ManagerOnline.UpdatezatcaEndDate(1, endDate);
							break;
						case "Inactive":
							MessageBox.Show("نأسف، لقد انتهت صلاحية اشتراككم في النظام.\r\nتم تعليق بعض الوظائف مؤقت\u064bا .\r\nيرجى تجديد الاشتراك لمواصلة استخدام النظام بشكل كامل.", "انتهاء الاشتراك", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
		}

		private static void UpdatezatcaEndDate(int type, DateTime endDate)
		{
			try
			{
				ManagerOnline.GetDataBaseName(MainClass.DataBaseName);
				if (ManagerOnline.conn1.State != ConnectionState.Open)
				{
					ManagerOnline.conn1.Open();
				}
				if (type == 1)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("UPDATE SettingZatca SET EndDate= @EndDate,IsActive=1", ManagerOnline.conn1);
					sqlCommand.Parameters.AddWithValue("@EndDate", endDate);
					sqlCommand.ExecuteNonQuery();
					ManagerOnline.conn.Close();
				}
				else
				{
					new SqlCommand("UPDATE SettingZatca SET IsActive=0", ManagerOnline.conn1).ExecuteNonQuery();
					ManagerOnline.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static string GetDataBaseName(string Dbname)
		{
			string result;
			try
			{
				string text = default(string);
				using (SqlConnection sqlConnection = new SqlConnection(ManagerOnline.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT \r\n      [DbAutoName]\r\n      ,[Dbname]\r\n  FROM [master].[dbo].[DatabasesManagment] \r\n  where Dbname=" + Dbname + ";", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						text = sqlDataReader["DbAutoName"].ToString();
					}
				}
				result = text;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static async Task GetUuidDevice()
		{
			try
			{
				string text;
				text = HardwareInfo.GenerateUID("SmartAuditERP", IncMac: true);
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				string clientName;
				clientName = ManagerOnline.GetClientName();
				if (string.IsNullOrEmpty(clientName))
				{
					return;
				}
				DeviceInfoDto value;
				value = new DeviceInfoDto
				{
					CompanyName = clientName,
					DeviceId = text,
					DeviceName = Environment.MachineName,
					ConnType = true
				};
				string requestUri;
				requestUri = ManagerOnline.url + "/api/device/add";
				using HttpClient httpClient = new HttpClient();
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await httpClient.PostAsync(requestUri, new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json"));
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					await httpResponseMessage.Content.ReadAsStringAsync();
				}
				else
				{
					await httpResponseMessage.Content.ReadAsStringAsync();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}
}