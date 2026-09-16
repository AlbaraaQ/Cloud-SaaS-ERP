namespace SmartAuditERP
{

	public class ListOfferItem
	{
		public int Id { get; set; }

		public int? OfferId { get; set; }

		public int? CatatogryID { get; set; }

		public int? ItemID { get; set; }

		public bool? OfferNatural { get; set; }

		public bool? IsGroupedItems { get; set; }

		public string Unit { get; set; }

		public double? OfferTargetQnty { get; set; }

		public double? OfferItemTargetQnty { get; set; }

		public double? OfferItemPrice { get; set; }

		public double? OfferTotalPrice { get; set; }

		public double? OfferItemValue { get; set; }

		public double? OfferItemPercentage { get; set; }

		public double? OfferItemStock { get; set; }
	}
}