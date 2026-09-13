using System.Runtime.InteropServices;

namespace SmartAuditERP
{

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class TwPendingXfers
	{
		public short Count;

		public int EOJ;
	}
}