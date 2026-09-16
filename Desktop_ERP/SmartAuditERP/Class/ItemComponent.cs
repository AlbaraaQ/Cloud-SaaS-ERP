namespace SmartAuditERP
{

	public class ItemComponent
	{
		public int Id { get; set; }

		public int ItemId { get; set; }

		public int ComponentId { get; set; }

		public decimal Price { get; set; }

		public int Quantity { get; set; }

		public string Unit { get; set; }

		public decimal Total { get; set; }

		public int Type { get; set; }

		public string Store { get; set; }
	}
}