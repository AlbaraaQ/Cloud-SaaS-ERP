using System;

namespace SmartAuditERP
{

	public class ListSyncDataAuditorManager
	{
		public int Id;

		public string ClientName;

		public int SyncCode;

		public DateTime StartDate;

		public DateTime EndDate;

		public bool Status;

		public ListSyncDataAuditorManager()
		{
			this.ClientName = "";
		}
	}
}