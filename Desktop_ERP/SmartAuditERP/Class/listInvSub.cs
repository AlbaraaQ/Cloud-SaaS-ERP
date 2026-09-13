using System;

namespace SmartAuditERP
{

	public class listInvSub
	{
		public string InvGlobalID { get; set; }

		public int ID { get; set; }

		public int? ProcID { get; set; }

		public int? ProcType { get; set; }

		public int? Store { get; set; }

		public int? ItemId { get; set; }

		public int? Unit { get; set; }

		public double? UnitEquality { get; set; }

		public double? Val { get; set; }

		public double? Val1 { get; set; }

		public double? ExchangePrice { get; set; }

		public DateTime? ExpireDate { get; set; }

		public double? TaxPerc { get; set; }

		public double? TaxVal { get; set; }

		public double? Discount { get; set; }

		public string Notes { get; set; }

		public string Description { get; set; }

		public int? ProductId { get; set; }

		public double? AvrgCost { get; set; }

		public double? CurrentQnty { get; set; }

		public double? ItemAddedCost { get; set; }

		public double? ItemPriceWithoutVAT { get; set; }

		public double? WithholdingTax { get; set; }

		public double? WithholdingTaxPerc { get; set; }

		public string ItemCostCenter { get; set; }

		public double? ItemAdditionalTax { get; set; }

		public double? ItemAdditionalTaxPerc { get; set; }
	}
}