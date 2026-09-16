using System.Runtime.InteropServices;

namespace SmartAuditERP
{

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal class BITMAPINFOHEADER
	{
		public int biSize;

		public int biWidth;

		public int biHeight;

		public short biPlanes;

		public short biBitCount;

		public int biCompression;

		public int biSizeImage;

		public int biXPelsPerMeter;

		public int biYPelsPerMeter;

		public int biClrUsed;

		public int biClrImportant;
	}
}