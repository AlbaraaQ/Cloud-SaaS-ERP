using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{

	public class SallaAPI
	{
        private readonly string _baseUrl;

        private readonly string _accessToken;

        private readonly HttpClient _httpClient;
        public SallaAPI(string accessToken)
		{
			this._baseUrl = "https://api.salla.dev/admin/v2";
			this._accessToken = accessToken;
			this._httpClient = new HttpClient();
			this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this._accessToken);
			this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		public async Task<JObject> GetAsync(string endpoint)
		{
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await this._httpClient.GetAsync($"{this._baseUrl}/{endpoint}");
			string text;
			text = await httpResponseMessage.Content.ReadAsStringAsync();
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				return JObject.Parse(text);
			}
			throw new Exception($"خطأ في الطلب: {httpResponseMessage.StatusCode} - {text}");
		}

		public async Task<JObject> PostAsync(string endpoint, object data)
		{
			StringContent content;
			content = new StringContent(JsonConvert.SerializeObject(RuntimeHelpers.GetObjectValue(data)), Encoding.UTF8, "application/json");
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await this._httpClient.PostAsync($"{this._baseUrl}/{endpoint}", content);
			string text;
			text = await httpResponseMessage.Content.ReadAsStringAsync();
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				return JObject.Parse(text);
			}
			throw new Exception($"خطأ في الطلب: {httpResponseMessage.StatusCode} - {text}");
		}

		public async Task<JObject> PutAsync(string endpoint, object data)
		{
			StringContent content;
			content = new StringContent(JsonConvert.SerializeObject(RuntimeHelpers.GetObjectValue(data)), Encoding.UTF8, "application/json");
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await this._httpClient.PutAsync($"{this._baseUrl}/{endpoint}", content);
			string text;
			text = await httpResponseMessage.Content.ReadAsStringAsync();
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				return JObject.Parse(text);
			}
			throw new Exception($"خطأ في الطلب: {httpResponseMessage.StatusCode} - {text}");
		}

		public async Task<bool> DeleteAsync(string endpoint)
		{
			return (await this._httpClient.DeleteAsync($"{this._baseUrl}/{endpoint}")).IsSuccessStatusCode;
		}
	}
}