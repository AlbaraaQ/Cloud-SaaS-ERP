using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{

	public class ProductsManager
	{
        private readonly SallaAPI _api;
        public ProductsManager(SallaAPI api)
		{
			this._api = api;
		}

		public async Task<JObject> GetProducts()
		{
			return await this._api.GetAsync("products");
		}

		public async Task<JObject> GetProduct(int productId)
		{
			return await this._api.GetAsync($"products/{productId}");
		}

		public async Task<JObject> CreateProduct(object productData)
		{
			return await this._api.PostAsync("products", RuntimeHelpers.GetObjectValue(productData));
		}

		public async Task<JObject> UpdateProduct(int productId, object productData)
		{
			return await this._api.PutAsync($"products/{productId}", RuntimeHelpers.GetObjectValue(productData));
		}

		public async Task<bool> DeleteProduct(int productId)
		{
			return await this._api.DeleteAsync($"products/{productId}");
		}
	}
}