namespace SmartAuditERP
{

	public class TransactionResponse
	{
		public bool Success { get; set; }

		public string StatusCode { get; set; }

		public string Message { get; set; }

		public string ErrorMessage { get; set; }

		public string Uuid { get; set; }

		public string Amount { get; set; }

		public string ApprovalCode { get; set; }

		public string RRN { get; set; }

		public string STAN { get; set; }

		public string CardScheme { get; set; }

		public string PAN { get; set; }

		public string TransactionType { get; set; }

		public string RawResponse { get; set; }
	}
}