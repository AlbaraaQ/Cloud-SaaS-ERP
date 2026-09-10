using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	[StandardModule]
	public sealed class Session
	{
		public static WhatsAppSender waSender;

		public static async Task EnsureWhatsAppSessionAsync()
		{
			if (Session.waSender == null || !Session.waSender.IsSessionValid())
			{
				try
				{
					Session.waSender = new WhatsAppSender();
					await Session.waSender.InitializeWhatsAppAsync();
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					MessageBox.Show("❌ فشل في إعادة تهيئة جلسة WhatsApp: " + ex.Message);
					ProjectData.ClearProjectError();
				}
			}
		}
	}
}