using System;

namespace SmartAuditERP
{

	public class UserDevice
	{
		public int Id { get; set; }

		public int UserId { get; set; }

		public string UserName { get; set; }

		public string ClientCode { get; set; }

		public string DeviceId { get; set; }

		public string DeviceName { get; set; }

		public DateTime CreatedAt { get; set; }

		public bool IsActive { get; set; }
	}
}