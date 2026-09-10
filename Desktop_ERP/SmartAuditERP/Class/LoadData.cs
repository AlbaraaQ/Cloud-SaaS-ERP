using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class LoadData
	{
		public static int CostType = 1;

		public static DataTable Invertories(int EmpNo)
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			if (EmpNo == 0)
			{
				EmpNo = 1;
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter((EmpNo != 1) ? ("select id,name from Safes,Safe_Emps where Safes.id=Safe_Emps.safe_id and emp_id=" + Conversions.ToString(EmpNo) + " and IS_Deleted=0 and status<>2 order by IS_Default desc") : "select id,name from Safes where  IS_Deleted=0 and status<>2 order by IS_Default desc", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}

		public static DataTable Customers(int Type)
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			_ = MainClass.EmpNo;
			if (MainClass.EmpNo == 0)
			{
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select id,name from Customers where (type=" + Conversions.ToString(Type) + " or type=3) and IS_Deleted=0 order by id", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}

		public static DataTable Treasury()
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			int num;
			num = MainClass.EmpNo;
			if (MainClass.EmpNo == 0)
			{
				num = 1;
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter((num != 1) ? ("select id,name,Acc_Code from Stocks,Stock_Emps where Stocks.id=Stock_Emps.stock_id and emp_id=" + Conversions.ToString(num) + "  and IS_Deleted=0 and status<>2 order by id") : "select id,name ,Acc_Code from Stocks where  IS_Deleted=0 and status<>2 order by id", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}

		public static DataTable SalesMen()
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			_ = MainClass.EmpNo;
			if (MainClass.EmpNo == 0)
			{
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select id,name from salesmen where IS_Deleted=0 order by id", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}

		public static DataTable Banks()
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			_ = MainClass.EmpNo;
			if (MainClass.EmpNo == 0)
			{
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select id,name from Banks where  IS_Deleted=0", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}

		public static DataTable CostCenters()
		{
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			_ = MainClass.EmpNo;
			if (MainClass.EmpNo == 0)
			{
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select code, name  from cost_center where type=2 and Is_Deleted=0 order by code", selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			return dataTable;
		}
	}
}