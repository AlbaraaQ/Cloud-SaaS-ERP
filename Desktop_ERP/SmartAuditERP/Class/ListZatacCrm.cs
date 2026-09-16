using System;

namespace SmartAuditERP
{

	public class ListZatacCrm
	{
		public string ClientName;

		public bool IsActive;

		public string DataBaseName;

		public DateTime startDate;

		public DateTime EndDate;

		public ListZatacCrm()
		{
			this.ClientName = "";
			this.IsActive = false;
			this.DataBaseName = "";
		}
	}
}