using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class LoginBranch1
	{
		public static int ID;

		public static int BranchNo = -1;

		public static string BranchCode = Conversions.ToString(1);

		public static string PreID = Conversions.ToString(1);

		public static string BranchName = "";

		public static string CustomersAcc = "1231";

		public static string SupliersAcc = "22111";

		public static string TreasuriesAcc = "1211";

		public static string BanksAcc = "1221";

		public static string InventoryAcc = "1221";

		public LoginBranch1(int Id)
		{
			if (Id <= -1)
			{
			}
		}
	}
}