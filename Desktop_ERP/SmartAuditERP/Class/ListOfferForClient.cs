namespace SmartAuditERP
{

	public class ListOfferForClient
	{
		public int Id { get; set; }

		public int? OfferId { get; set; }

		public int? ClientID { get; set; }

		public int? CatatogryID { get; set; }

		public int? ItemID { get; set; }

		public bool? IsForAllItems { get; set; }

		public string Unit { get; set; }

		public double? OfferTargetQnty { get; set; }

		public double? OfferItemValue { get; set; }

		public double? OfferItemPercentage { get; set; }
	}
}