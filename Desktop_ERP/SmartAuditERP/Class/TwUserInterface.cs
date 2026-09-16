using System;
using System.Runtime.InteropServices;

namespace SmartAuditERP
{

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class TwUserInterface
	{
		public short ShowUI;

		public short ModalUI;

		public IntPtr ParentHand;
	}
}