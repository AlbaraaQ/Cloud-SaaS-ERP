using System;

namespace SmartAuditERP
{

	public class ListOffers
	{
		public int OfferID { get; set; }

		public int? OfferType { get; set; }

		public DateTime? OfferStartDate { get; set; }

		public DateTime? OfferExpire { get; set; }

		public string OfferName { get; set; }

		public DateTime? OfferDate { get; set; }

		public string OfferAccount { get; set; }

		public int? InvoiceID { get; set; }

		public double? OfferValue { get; set; }

		public double? OfferPercentage { get; set; }

		public double? OfferValueTarget { get; set; }

		public double? OfferQntyTarget { get; set; }

		public int? EmpId { get; set; }

		public bool? IsDeleted { get; set; }
	}
}