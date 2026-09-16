using System.Net.Http;
using System.Threading.Tasks;

namespace SmartAuditERP
{

	public class GetDataAPI
	{
		public async Task<string> GetItems()
		{
			HttpResponseMessage httpResponseMessage;
			httpResponseMessage = await new HttpClient().GetAsync("http://update.auditorerp.cloud/CashierApp/CashierApp.asmx/GetItems");
			string result;
			result = "";
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				result = await httpResponseMessage.Content.ReadAsStringAsync();
			}
			return result;
		}
	}
}