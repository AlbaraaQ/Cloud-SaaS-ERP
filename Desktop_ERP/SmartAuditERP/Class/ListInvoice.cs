using System;

namespace SmartAuditERP
{

	public class ListInvoice
	{
		public string InvGlobalID { get; set; }

		public int ProcID { get; set; }

		public int ProcType { get; set; }

		public int ID { get; set; }

		public DateTime DateInv { get; set; }

		public int InvType { get; set; }

		public int OrderType { get; set; }

		public int Safe { get; set; }

		public int Stock { get; set; }

		public int CustID { get; set; }

		public int SalesEmp { get; set; }

		public decimal InvTotal { get; set; }

		public decimal TotPurch { get; set; }

		public decimal AdditionsTot { get; set; }

		public decimal Insurance { get; set; }

		public decimal TotNet { get; set; }

		public string EntryID { get; set; }

		public int PurchRestID { get; set; }

		public string ReffNo { get; set; }

		public DateTime ReffDate { get; set; }

		public int Branch { get; set; }

		public bool IS_Buy { get; set; }

		public decimal Minus { get; set; }

		public decimal Paid { get; set; }

		public int Salesman { get; set; }

		public decimal Tax { get; set; }

		public decimal ExtraVAT { get; set; }

		public int PayType { get; set; }

		public int Bank { get; set; }

		public decimal Cash { get; set; }

		public decimal Visa { get; set; }

		public string Notes { get; set; }

		public bool IS_Deleted { get; set; }

		public bool Sync { get; set; }

		public decimal InvProfit { get; set; }

		public int InvoiceStatus { get; set; }

		public decimal AdditionalCost { get; set; }

		public decimal PriceIncVAT { get; set; }

		public int PaymentStatus { get; set; }

		public DateTime IssueDate { get; set; }

		public string InvCombinedID { get; set; }

		public decimal ItemsDiscount { get; set; }

		public decimal InvCost { get; set; }

		public decimal FreeVATSales { get; set; }

		public decimal InvSum { get; set; }

		public decimal VATPercent { get; set; }

		public string CurrencyCode { get; set; }

		public string CloudID { get; set; }

		public string CashCustomerName { get; set; }

		public string CashCustomerMobile { get; set; }

		public string QRCode { get; set; }

		public string InvoiceHash { get; set; }

		public string UUID { get; set; }

		public bool ZatcaSent { get; set; }

		public decimal TotalWithholdingTax { get; set; }

		public string TableNo { get; set; }

		public decimal BalancePreviews { get; set; }
	}
}