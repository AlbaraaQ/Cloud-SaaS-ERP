using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{

	public class CustomersManager
	{
        private readonly SallaAPI _api;
        public CustomersManager(SallaAPI api)
		{
			this._api = api;
		}

		public async Task<JObject> GetCustomers()
		{
			return await this._api.GetAsync("customers");
		}

		public async Task<JObject> GetCustomer(int customerId)
		{
			return await this._api.GetAsync($"customers/{customerId}");
		}
	}
}