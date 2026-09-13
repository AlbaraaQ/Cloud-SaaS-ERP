using System;

namespace SmartAuditERP
{

	public class ListOperationPermission
	{
		public int id { get; set; }

		public int emp { get; set; }

		public int OperNo { get; set; }

		public int Pwd { get; set; }

		public DateTime lastChanged { get; set; }

		public int IS_Deleted { get; set; }

		public int OperVal { get; set; }

		public int Activated { get; set; }
	}
}