using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace SmartAuditERP
{

	public class Gdip
	{
		private static ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		internal static extern int GdipCreateBitmapFromGdiDib(IntPtr bminfo, IntPtr pixdat, ref IntPtr image);

		[DllImport("gdiplus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		internal static extern int GdipSaveImageToFile(IntPtr image, string filename, [In] ref Guid clsid, IntPtr encparams);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		internal static extern int GdipDisposeImage(IntPtr image);

		private static bool GetCodecClsid(string filename, ref Guid clsid)
		{
			clsid = Guid.Empty;
			string extension;
			extension = Path.GetExtension(filename);
			if (Information.IsNothing(extension))
			{
				return false;
			}
			extension = "*" + extension.ToUpper();
			ImageCodecInfo[] array;
			array = Gdip.codecs;
			foreach (ImageCodecInfo imageCodecInfo in array)
			{
				if (imageCodecInfo.FilenameExtension.IndexOf(extension) >= 0)
				{
					clsid = imageCodecInfo.Clsid;
					return true;
				}
			}
			return false;
		}

		public static bool SaveDIBAs(string picname, IntPtr bminfo, IntPtr pixdat)
		{
			SaveFileDialog saveFileDialog;
			saveFileDialog = new SaveFileDialog();
			saveFileDialog.FileName = picname;
			Guid clsid = default(Guid);
			if (!Gdip.GetCodecClsid(saveFileDialog.FileName, ref clsid))
			{
				MessageBox.Show("Unknown picture format for extension " + Path.GetExtension(saveFileDialog.FileName), "Image Codec", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return false;
			}
			IntPtr image;
			image = IntPtr.Zero;
			if ((Gdip.GdipCreateBitmapFromGdiDib(bminfo, pixdat, ref image) != 0) | object.Equals(image, IntPtr.Zero))
			{
				return false;
			}
			int num;
			num = Gdip.GdipSaveImageToFile(image, saveFileDialog.FileName, ref clsid, IntPtr.Zero);
			Gdip.GdipDisposeImage(image);
			return num == 0;
		}
	}
}