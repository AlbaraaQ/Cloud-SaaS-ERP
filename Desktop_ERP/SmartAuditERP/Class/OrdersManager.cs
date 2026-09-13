using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SmartAuditERP
{
    public class OrdersManager
    {
        #region Fields

        private readonly SallaAPI _api;

        #endregion

        #region Constructor

        public OrdersManager(SallaAPI api)
        {
            _api = api;
        }

        #endregion

        #region Public Methods

        public async Task<JObject> GetOrders()
        {
            return await _api.GetAsync("orders");
        }

        public async Task<JObject> GetOrder(int orderId)
        {
            return await _api.GetAsync($"orders/{orderId}");
        }

        public async Task<JObject> UpdateOrderStatus(int orderId, string status)
        {
            var data = new { status };
            return await _api.PutAsync($"orders/{orderId}/status", data);
        }

        #endregion
    }
}