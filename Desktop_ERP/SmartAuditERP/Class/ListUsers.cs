namespace SmartAuditERP
{

	public class ListUsers
	{
		public int Id { get; set; }

		public string Emp { get; set; }

		public string Username { get; set; }

		public string Pwd { get; set; }

		public bool IsDeleted { get; set; }

		public string SecureCode { get; set; }

		public string LoginSecode { get; set; }
	}
}