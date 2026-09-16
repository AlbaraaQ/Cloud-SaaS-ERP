using System;

namespace SmartAuditERP
{

	public class ItemPrices
	{
		public int Proc_id { get; set; }

		public int ItemID { get; set; }

		public int UnitID { get; set; }

		public string Time { get; set; }

		public DateTime Date { get; set; }

		public decimal PurchPrice { get; set; }

		public decimal SalePrice { get; set; }

		public decimal LowPurchPrice { get; set; }

		public decimal HighPurchPrice { get; set; }

		public decimal LowSalePrice { get; set; }

		public decimal HighSalePrice { get; set; }

		public decimal CompetitorPrice { get; set; }

		public string Emp { get; set; }

		public bool IS_Deleted { get; set; }

		public decimal ConsumerPrice { get; set; }

		public decimal WholesalePrice { get; set; }
	}
}