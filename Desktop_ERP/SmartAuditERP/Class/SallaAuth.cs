using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{

	public class SallaAuth
	{
		private const string AuthUrl = "https://accounts.salla.sa/oauth2/token";

		public async Task<string> GetAccessToken(string clientId, string clientSecret, string code)
		{
			using HttpClient httpClient = new HttpClient();
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await httpClient.PostAsync("https://accounts.salla.sa/oauth2/token", new FormUrlEncodedContent(new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "client_id", clientId },
			{ "client_secret", clientSecret },
			{ "code", code },
			{ "redirect_uri", "YOUR_REDIRECT_URI" }
		}));
			string text;
			text = await httpResponseMessage.Content.ReadAsStringAsync();
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				return JObject.Parse(text)["access_token"].ToString();
			}
			throw new Exception($"خطأ في الحصول على Access Token: {text}");
		}
	}
}