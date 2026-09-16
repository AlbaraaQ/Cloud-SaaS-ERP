using System;

namespace SmartAuditERP
{

	public class ListRecipt
	{
		public int AutoID { get; set; }

		public string GlobalID { get; set; }

		public int ReceiptNo { get; set; }

		public DateTime? ReceiptDate { get; set; }

		public int? EmpId { get; set; }

		public int? ClientID { get; set; }

		public string CreditAcc { get; set; }

		public string DebitAcc { get; set; }

		public double? Payment { get; set; }

		public double? VAT { get; set; }

		public double? NetVal { get; set; }

		public int? ReceiptType { get; set; }

		public int? PaymentType { get; set; }

		public int? BankId { get; set; }

		public int? TreasuryID { get; set; }

		public int? SalesManID { get; set; }

		public string CheckNo { get; set; }

		public DateTime? CheckDate { get; set; }

		public string Checkbank { get; set; }

		public int? CheckState { get; set; }

		public int? State { get; set; }

		public string Notes { get; set; }

		public string EntryGlobalID { get; set; }

		public int? BranchID { get; set; }

		public bool? ISDeleted { get; set; }

		public string Cccode { get; set; }

		public string ReffNo { get; set; }

		public DateTime? Reffdate { get; set; }

		public bool? Sync { get; set; }

		public string MobPOSID { get; set; }
	}
}