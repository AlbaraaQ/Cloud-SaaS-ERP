namespace SmartAuditERP
{

	public class MainSetting
	{
		public static bool IsGediaActive { get; set; } = false;

		public static int GediaPort { get; set; } = 0;

		public static bool GediaEnableReceiptPrint { get; set; } = true;

		public static bool ZatcaIntegerationActive { get; set; } = false;

		public static bool IsProductionZatca { get; set; } = false;

		public static bool IsSimulationZatca { get; set; } = false;

		public static bool ZatcaSyncManual { get; set; } = false;
	}
}