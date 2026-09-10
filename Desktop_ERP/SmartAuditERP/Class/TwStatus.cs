using System.Runtime.InteropServices;

namespace SmartAuditERP
{

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class TwStatus
	{
		public short ConditionCode;

		public short Reserved;
	}
}