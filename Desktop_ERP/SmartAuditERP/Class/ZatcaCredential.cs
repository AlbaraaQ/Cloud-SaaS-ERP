namespace SmartAuditERP
{

	public class ZatcaCredential
	{
		public string CSR { get; set; }

		public string PrivateKey { get; set; }

		public string CSID { get; set; }

		public string Secret { get; set; }

		public ZatcaCredential()
		{
			this.CSR = "";
			this.PrivateKey = "";
			this.CSID = "";
			this.Secret = "";
		}
	}
}