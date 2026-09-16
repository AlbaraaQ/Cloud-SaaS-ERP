using AuditorAPI.Models;
using DevExpress.XtraEditors.Controls;
using ETA_Invoice.Models;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Nancy.Json;
using Newtonsoft.Json;
using QLicense;
using SmartAuditERP.Form_WPF;
using SmartAuditERP.Updates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
//using System.Web.Script.Serialization;
using System.Windows;
using UtilitiesProj;
using MessageBox = System.Windows.MessageBox;

// ✅ استبدال WinForms MessageBox بـ WPF MessageBox
// ✅ إزالة FontAwesome.Sharp
// ✅ إزالة System.Windows.Forms
// ✅ إزالة Microsoft.VisualBasic.CompilerServices حيث أمكن
// ✅ الاحتفاظ بجميع الدوال public كما هي

namespace SmartAuditERP
{
    public class Common
    {
        #region Nested Classes

        public class DatabaseInfo
        {
            public string Name { get; set; }
            public int ID { get; set; }
        }

        #endregion

        #region Static Fields

        public static int CostType = 1;
        public static string DigitsNo = "N2";
        public static SqlConnection conn = MainClass.ConnObj();
        public static DataTable FoundationInfoDT = new DataTable();
        public static Branch CurrentBranch = new Branch();
        public static LicensingOrder licensing = new LicensingOrder();
        public static Image SupportBG;
        public static string AppSalesmaneAdress;
        public static string AppSalesmaneMobie;
        public static string AppSalesmaneWebsite;
        public static string AppSalesmaneEmail;
        public static string _TableNoLocal = "0";
        public static decimal _BalanceTable = default(decimal);
        public static string _TableInvglobalid = "";
        public static bool _StateTable = false;
        public static bool _ISNewTable = true;
        public static bool _ISShow = false;

        #endregion

        #region Version

        public static string GetVersion()
        {
            try
            {
                // ✅ بدل ApplicationDeployment في WPF نستخدم Assembly
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                    return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
            catch { }
            return "20.0.6.4";
        }

        #endregion

        #region Name Lookup Methods

        public static string GetEmpName(int Id)
        {
            try
            {
                if (Id <= 0) return MainClass.UserName;

                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select users.username from users where users.emp=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetEmployeeName(int Id)
        {
            try
            {
                if (Id <= 0) return MainClass.UserName;

                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select name from Employees where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetStoreName(int Id)
        {
            try
            {
                if (Id <= 0) return "";

                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select Safes.name from Safes where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetSalesManName(int Id)
        {
            try
            {
                if (Id <= 0) return "";

                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select salesmen.name from salesmen where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetClientName(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select name from Customers where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetItemName(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select name from Items where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetItemBarcode(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select barcode from Items where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetItemCode(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Code from Items where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetUnitName(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select name from units where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static int GetUnitID(string name)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select id from units where name=N'@Name'",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Name", name);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch
            {
                return -1;
            }
        }

        public static string GetAccountName(int Code)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select AName from Accounts_Index where Code=@Code",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Code", Code);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetAccountCode(string Aname)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Code from Accounts_Index where AName=N'" + Aname + "'",
                    connection);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetCostCenterName(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code)) return "";

                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select name from cost_center where code=@Code",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Code", code);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["name"].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetBranchName(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Name from Branches where BranchId=@Id and IS_Deleted=0",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["Name"].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetBranchCode(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Code from Branches where id=@Id and IS_Deleted=0",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["Code"].ToString() : id.ToString();
            }
            catch
            {
                return id.ToString();
            }
        }

        public static string GetBranchCostCenter(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select CostCenter from Branches where BranchId=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return "-1";
                if (dt.Rows[0][0] == DBNull.Value
                    || string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                    return "-1";

                return dt.Rows[0][0].ToString();
            }
            catch
            {
                return "-1";
            }
        }

        public static string GetTaxNo(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select tax_no from Customers where IS_Deleted=0 and id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string ResrirectionType(int typ)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "Select * from EntryTypes where Id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", typ);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["Name"].ToString() : "";
            }
            catch
            {
                return "";
            }
        }

        public static string GetCurrency(int Curr)
        {
            if (Curr < 2) return "SAR";
            if (Curr == 2) return "USD";
            return "";
        }

        #endregion

        #region Item Methods

        public static int GetItemCategoryID(object ItemId)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select group_id from Items where IS_Deleted=0 and id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", ItemId);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["group_id"]) : 0;
            }
            catch
            {
                return 0;
            }
        }

        public static void GetItemNames(ref string nameAr, ref string nameEn, int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select name, nameEN from Items where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    nameAr = dt.Rows[0]["name"].ToString();
                    nameEn = dt.Rows[0]["nameEN"].ToString();
                }
                else
                {
                    nameAr = "";
                    nameEn = "";
                }
            }
            catch
            {
                nameAr = "";
                nameEn = "";
            }
        }

        public static void GetItemCategory(
            ref string CategoryAr,
            ref string CategoryEn,
            ref string CategoryCode,
            ref int CategoryID,
            ref string ItemGroupPrinter,
            int ItemId)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter1 = new SqlDataAdapter(
                    "select group_id from Items where IS_Deleted=0 and id=@Id",
                    connection);
                adapter1.SelectCommand.Parameters.AddWithValue("@Id", ItemId);
                var dt1 = new DataTable();
                adapter1.Fill(dt1);

                if (dt1.Rows.Count == 0)
                {
                    CategoryAr = CategoryEn = CategoryCode = ItemGroupPrinter = "";
                    return;
                }

                CategoryID = Convert.ToInt32(dt1.Rows[0]["group_id"]);

                using var adapter2 = new SqlDataAdapter(
                    "select name,nameEN,Code,printer from ItemsCategory "
                    + "where IS_Deleted=0 and id=@CatId",
                    connection);
                adapter2.SelectCommand.Parameters.AddWithValue("@CatId", CategoryID);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    CategoryAr = dt2.Rows[0]["name"].ToString();
                    CategoryEn = dt2.Rows[0]["nameEN"]?.ToString() ?? "";
                    CategoryCode = dt2.Rows[0]["Code"]?.ToString() ?? "";
                    ItemGroupPrinter = dt2.Rows[0]["printer"]?.ToString() ?? "";
                }
                else
                {
                    CategoryAr = CategoryEn = CategoryCode = ItemGroupPrinter = "";
                }
            }
            catch
            {
                CategoryAr = CategoryEn = CategoryCode = ItemGroupPrinter = "";
            }
        }

        public static double GetItemPrice(int Id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select isnull(sale_price,0) as sale_price from Items where id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0
                    ? Convert.ToDouble(dt.Rows[0][0])
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        #endregion

        #region City / Region / Country

        public static void GetCityName(
            ref string RegionName,
            ref string CountryName,
            ref string CityName,
            string RegionId,
            string CountryId,
            string CityId)
        {
            try
            {
                using var connection = MainClass.ConnObj();

                // City
                if (!string.IsNullOrEmpty(CityId))
                {
                    using var a1 = new SqlDataAdapter(
                        "select name from Cities where id=@Id", connection);
                    a1.SelectCommand.Parameters.AddWithValue("@Id", CityId);
                    var dt1 = new DataTable();
                    a1.Fill(dt1);
                    CityName = dt1.Rows.Count > 0 ? dt1.Rows[0]["name"].ToString() : "";
                }
                else { CityName = ""; }

                // Country
                if (!string.IsNullOrEmpty(CountryId))
                {
                    using var a2 = new SqlDataAdapter(
                        "select name from Countries where id=@Id", connection);
                    a2.SelectCommand.Parameters.AddWithValue("@Id", CountryId);
                    var dt2 = new DataTable();
                    a2.Fill(dt2);
                    CountryName = dt2.Rows.Count > 0 ? dt2.Rows[0]["name"].ToString() : "";
                }
                else { CountryName = ""; }

                // Region
                if (!string.IsNullOrEmpty(RegionId))
                {
                    using var a3 = new SqlDataAdapter(
                        "select name from Areas where id=@Id", connection);
                    a3.SelectCommand.Parameters.AddWithValue("@Id", RegionId);
                    var dt3 = new DataTable();
                    a3.Fill(dt3);
                    RegionName = dt3.Rows.Count > 0 ? dt3.Rows[0]["name"].ToString() : "";
                }
                else { RegionName = ""; }
            }
            catch
            {
                CountryName = CityName = RegionName = "";
            }
        }

        #endregion

        #region Financial Methods

        public static int GetUserTreasuryAcc()
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Stocks.Acc_Code from Stocks,Stock_Emps "
                    + "where Stocks.id=Stock_Emps.stock_id "
                    + "and Stock_Emps.emp_id=@EmpId "
                    + "and Stocks.branch=@Branch",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@EmpId", MainClass.EmpNo);
                adapter.SelectCommand.Parameters.AddWithValue("@Branch", MainClass.BranchNo);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 && MainClass.EmpNo > 0)
                    return Convert.ToInt32(dt.Rows[0]["Acc_Code"]);
                return -1;
            }
            catch
            {
                return -1;
            }
        }

        public static int GetUserTreasuryID(short EmpID)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select Stocks.Id from Stocks,Stock_Emps "
                    + "where Stocks.id=Stock_Emps.stock_id "
                    + "and Stock_Emps.emp_id=@EmpId "
                    + "and Stocks.branch=@Branch",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@EmpId", (int)EmpID);
                adapter.SelectCommand.Parameters.AddWithValue("@Branch", MainClass.BranchNo);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["id"]) : -1;
            }
            catch
            {
                return -1;
            }
        }

        public static double GetAccountBalance(int AccCode)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                string branchCond = MainClass.BranchNo != -1
                    ? "Entry.branch=" + MainClass.BranchNo
                      + " and Entry_sub.branch=" + MainClass.BranchNo + " and "
                    : "";

                using var adapter = new SqlDataAdapter(
                    "select sum(Entry_sub.dept) as dept, "
                    + "sum(Entry_sub.credit) as credit "
                    + "from Entry,Entry_sub where "
                    + branchCond
                    + " Entry.IS_Deleted=0 and Entry.state=1 "
                    + "and Entry.date<=@date2 "
                    + "and Entry.GlobalId=Entry_sub.EntryGlobalId "
                    + "and Entry_sub.acc_no=@AccCode",
                    connection);
                adapter.SelectCommand.Parameters
                    .AddWithValue("@date2", DateTime.Now);
                adapter.SelectCommand.Parameters
                    .AddWithValue("@AccCode", AccCode);

                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0
                    && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                {
                    double dept = Convert.ToDouble(dt.Rows[0][0]);
                    double credit = Convert.ToDouble(dt.Rows[0][1]);
                    return Math.Round(credit - dept, 3);
                }
                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        public static double DivisionOperation(decimal Numerator, decimal Denumirator)
        {
            try
            {
                if (Denumirator == 0) return 0.0;
                double result = (double)(Numerator / Denumirator);
                return (double.IsInfinity(result) || double.IsNaN(result))
                    ? 0.0 : result;
            }
            catch
            {
                return 0.0;
            }
        }

        public static int GetInventoryBranch(int id)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select branch from Safes where id=@Id", connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", id);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 1;
            }
            catch
            {
                return 1;
            }
        }

        public static int GetCurrencytosand(int Doctype)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select isnull(Currency,1) as Currency "
                    + "from SettingGeneral where inv_id=@InvId",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@InvId", Doctype);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0
                    ? Convert.ToInt32(dt.Rows[0]["Currency"])
                    : 1;
            }
            catch
            {
                return 1;
            }
        }

        #endregion

        #region ID Generators

        public static int GetCustomerId()
        {
            using var connection = MainClass.ConnObj();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            int num = (int)(Convert.ToDouble(
                new SqlCommand(
                    "Select MAX(id) from Customers where Branch=@Branch",
                    connection)
                { }
                .AddParam("@Branch", MainClass.BranchNo)
                .ExecuteScalarSafe()) + (MainClass.BranchNo * 10000) + 1);

            while (Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Customers where id=@Id", connection)
                    .AddParam("@Id", num).ExecuteScalarSafe()) != 0)
                num++;

            return num;
        }

        public static int GetTreasuryId()
        {
            using var connection = MainClass.ConnObj();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            int num = (int)(Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Stocks where branch=@Branch",
                    connection).AddParam("@Branch", MainClass.BranchNo)
                    .ExecuteScalarSafe()) + (MainClass.BranchNo * 100) + 1);

            while (Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Stocks where id=@Id", connection)
                    .AddParam("@Id", num).ExecuteScalarSafe()) != 0)
                num++;

            return num;
        }

        public static int GetEmployeeId()
        {
            using var connection = MainClass.ConnObj();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            int num = (int)(Convert.ToDouble(
                new SqlCommand("Select COUNT(*) from Employees", connection)
                    .ExecuteScalarSafe()) + (MainClass.BranchNo * 100) + 1);

            while (Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Employees where id=@Id", connection)
                    .AddParam("@Id", num).ExecuteScalarSafe()) != 0)
                num++;

            return num;
        }

        public static int GetInventoryId()
        {
            using var connection = MainClass.ConnObj();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            int num = (int)(Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Safes where branch=@Branch",
                    connection).AddParam("@Branch", MainClass.BranchNo)
                    .ExecuteScalarSafe()) + (MainClass.BranchNo * 100) + 1);

            while (Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from Safes where id=@Id", connection)
                    .AddParam("@Id", num).ExecuteScalarSafe()) != 0)
                num++;

            return num;
        }

        public static int GetBankId()
        {
            using var connection = MainClass.ConnObj();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            int num = (int)(Convert.ToDouble(
                new SqlCommand("Select COUNT(*) from banks", connection)
                    .ExecuteScalarSafe()) + (MainClass.BranchNo * 100) + 1);

            while (Convert.ToDouble(
                new SqlCommand(
                    "Select COUNT(*) from banks where id=@Id", connection)
                    .AddParam("@Id", num).ExecuteScalarSafe()) != 0)
                num++;

            return num;
        }

        #endregion

        #region Settings Loading

        public static void LoadDefualtInfo()
        {
            try
            {
                using var connection = MainClass.ConnObj();

                // ══ إعدادات الطباعة ══
                using (var adapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=0", connection))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 1)
                    {
                        string rptUrl = dt.Rows[0]["RptUrl"].ToString();
                        MainClass.ReportsPath =
                            string.IsNullOrEmpty(rptUrl)
                            ? System.IO.Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory, "Reports")
                            : System.IO.Path.GetDirectoryName(rptUrl);

                        MainClass.ReportsPrinter =
                            dt.Rows[0]["CasherPrinter"].ToString().Trim();
                        if (string.IsNullOrEmpty(MainClass.ReportsPrinter))
                            MainClass.ReportsPrinter = GetDefaultPrinter();
                    }
                    else
                    {
                        MainClass.ReportsPath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, "Reports");
                        MainClass.ReportsPrinter = GetDefaultPrinter();
                    }

                    if (!Directory.Exists(MainClass.ReportsPath))
                        MainClass.ReportsPath = System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, "Reports");
                }

                // ══ الإعدادات العامة ══
                using (var adapter = new SqlDataAdapter(
                    "select * from SettingGeneral where Inv_Id=0", connection))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 1)
                    {
                        try
                        {
                            if (dt.Rows[0]["CostType"] != null
                                && dt.Rows[0]["CostType"] != DBNull.Value)
                            {
                                double costTypeVal = Convert.ToDouble(
                                    dt.Rows[0]["CostType"]);
                                if (costTypeVal > 0)
                                    CostType = (int)Math.Round(costTypeVal);
                            }

                            if (dt.Rows[0]["DigitsNo"] != null
                                && dt.Rows[0]["DigitsNo"] != DBNull.Value)
                                DigitsNo = "N" + dt.Rows[0]["DigitsNo"];
                        }
                        catch { }
                    }
                }

                // ══ بيانات المؤسسة ══
                using (var adapter = new SqlDataAdapter(
                    "Select [nameA],[nameE],[FieldA],[FieldE],[bsn_no],[country],"
                    + "[Address],[Tel],[Mobile],[city],[Email],[website],"
                    + "[Logo] as LogoImage,[tax_no],Area,PlotIdentification,"
                    + "BuildingNumber,StreetName,AdditionalStreetName,"
                    + "District,PostalZone,crtype from Foundation",
                    connection))
                {
                    var dt = new DataTable();
                    dt.Columns.Add("Logo", typeof(Image));
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 1)
                    {
                        Image image = ByteImageConverter
                            .FromByteArray(dt.Rows[0]["LogoImage"] as byte[]);
                        if (image != null)
                            dt.Rows[0]["Logo"] = new Bitmap(image);
                        FoundationInfoDT = dt.Copy();
                    }
                }

                // ══ بيانات الفرع ══
                using (var adapter = new SqlDataAdapter(
                    "select BranchId,name,tel,mobile,fax,email,address,notes,"
                    + "isnull(is_deleted,0) as is_deleted,"
                    + "Isnull(IsDefault,1) as IsDefault,code,"
                    + "CustomersAcc,SupliersAcc,BanksAcc,InventoryAcc,"
                    + "TreasuriesAcc,AppAcc,"
                    + "isnull(EmployeeAcc,2241) as EmployeeAcc "
                    + "from Branches where BranchId=@BranchId",
                    connection))
                {
                    adapter.SelectCommand.Parameters
                        .AddWithValue("@BranchId", MainClass.BranchNo);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        try
                        {
                            CurrentBranch.ClientCode = Sync.ClientCode;
                            CurrentBranch.BranchId = Convert.ToInt32(row["BranchId"]);
                            CurrentBranch.BranchName = row["name"].ToString();
                            CurrentBranch.BranchTel = row["tel"].ToString();
                            CurrentBranch.BranchMobile = row["mobile"].ToString();
                            CurrentBranch.BranchEmail = row["email"].ToString();
                            CurrentBranch.BranchAddress = row["address"].ToString();
                            CurrentBranch.Notes = row["notes"].ToString();
                            CurrentBranch.IsDeleted = Convert.ToBoolean(row["is_deleted"]);
                            CurrentBranch.IsDefault = Convert.ToBoolean(row["IsDefault"]);
                            CurrentBranch.CreateDate = DateTime.Now;
                            CurrentBranch.LastUpdateDate = DateTime.Now;
                            CurrentBranch.CustomersAcc = row["CustomersAcc"].ToString();
                            CurrentBranch.SupliersAcc = row["SupliersAcc"].ToString();
                            CurrentBranch.BanksAcc = row["BanksAcc"].ToString();
                            CurrentBranch.InventoryAcc = row["InventoryAcc"].ToString();
                            CurrentBranch.TreasuriesAcc = row["TreasuriesAcc"].ToString();
                            CurrentBranch.EmployeeAcc = row["EmployeeAcc"].ToString();

                            if (row["AppAcc"] != DBNull.Value)
                                CurrentBranch.AppAccount = row["AppAcc"].ToString();

                            // مستودعات الفرع
                            using var adpSafes = new SqlDataAdapter(
                                "select id,name from Safes where IS_Deleted=0 and branch=@BId",
                                connection);
                            adpSafes.SelectCommand.Parameters
                                .AddWithValue("@BId", CurrentBranch.BranchId);
                            var dtSafes = new DataTable();
                            adpSafes.Fill(dtSafes);

                            foreach (DataRow safeRow in dtSafes.Rows)
                            {
                                CurrentBranch.Invertories.Add(new BranchInvertory
                                {
                                    BranchId = CurrentBranch.BranchId,
                                    InvertoryId = safeRow["id"].ToString()
                                });
                            }
                        }
                        catch { }
                    }
                }

                // ══ اسم طريقة الدفع ══
                using (var adapter = new SqlDataAdapter(
                    "select name from Banks where id=1 "
                    + "and IS_Deleted=0 and ChangeInPOS=1", connection))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                        MainClass.PaymentName = dt.Rows[0]["name"].ToString();
                }

                // ══ إعدادات زاتكا ══
                using (var adapter = new SqlDataAdapter(
                    "select isnull(filePath,'') as filePath,"
                    + "isnull(isProduction,0) as isProduction,"
                    + "isnull(IsActive,0) as IsActive,"
                    + "isnull(IsSimulation,0) as IsSimulation,"
                    + "isnull(SyncManual,0) as SyncManual "
                    + "from SettingZatca where ID=1", connection))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0 && dt.Rows[0]["filePath"] != DBNull.Value)
                    {
                        MainClass.ZatcafilePath = dt.Rows[0]["filePath"].ToString();
                        MainSetting.IsProductionZatca =
                            Convert.ToBoolean(dt.Rows[0]["isProduction"]);
                        MainSetting.ZatcaIntegerationActive =
                            Convert.ToBoolean(dt.Rows[0]["IsActive"]);
                        MainSetting.IsSimulationZatca =
                            Convert.ToBoolean(dt.Rows[0]["IsSimulation"]);
                        MainSetting.ZatcaSyncManual =
                            Convert.ToBoolean(dt.Rows[0]["SyncManual"]);
                    }
                }

                LoadGediaSetting();
                LoadEtaSetting();
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadDefualtInfo Error: " + ex.Message);
            }
        }

        public static void LoadEtaSetting()
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select * from EtaSetting where Id=1", connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count <= 0) return;

                EtaSetting.Active = Convert.ToBoolean(dt.Rows[0]["Active"]);
                EtaSetting.Receipt = Convert.ToBoolean(dt.Rows[0]["Receipt"]);
                EtaSetting.BranchCode = dt.Rows[0]["BranchCode"].ToString().Trim();
                EtaSetting.ActivityCode = dt.Rows[0]["ActivityCode"].ToString().Trim();
                EtaSetting.EinvoieType = dt.Rows[0]["EinvoieType"].ToString().Trim();
                EtaSetting.SignType = dt.Rows[0]["SignType"].ToString().Trim();
                EtaSetting.DocumentTypeVersion = dt.Rows[0]["DocumentTypeVersion"].ToString().Trim();
                EtaSetting.EtaCode = dt.Rows[0]["EtaCode"].ToString().Trim();
                EtaSetting.EtaClientId = dt.Rows[0]["EtaClientId"].ToString().Trim();
                EtaSetting.EtaClientSecret1 = dt.Rows[0]["EtaClientSecret1"].ToString().Trim();
                EtaSetting.EtaClientSecret2 = dt.Rows[0]["EtaClientSecret2"].ToString().Trim();
                EtaSetting.PinPass = dt.Rows[0]["Pinpass"].ToString().Trim();

                string path = @"C:\EtaConfigFile\PosEtaSerial.txt";
                EtaSetting.PosEtaSerial = File.Exists(path)
                    ? File.ReadAllText(path).Trim()
                    : "1000";
            }
            catch { }
        }

        public static void LoadGediaSetting()
        {
            try
            {
                string connectionString = BuildMasterConnectionString();
                using var sqlConnection = new SqlConnection(connectionString);
                if (sqlConnection.State == ConnectionState.Closed)
                    sqlConnection.Open();

                using var adapter = new SqlDataAdapter(
                    "select * from GediaSetting where id=1", sqlConnection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    MainSetting.IsGediaActive =
                        Convert.ToBoolean(dt.Rows[0]["IsGediaActive"]);
                    MainSetting.GediaPort =
                        Convert.ToInt32(dt.Rows[0]["GediaPort"]);
                    MainSetting.GediaEnableReceiptPrint =
                        Convert.ToBoolean(dt.Rows[0]["GediaEnableReceiptPrint"]);
                }
            }
            catch { }
        }

        #endregion

        #region Printer

        public static string GetDefaultPrinter()
        {
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    var settings = new PrinterSettings { PrinterName = printer };
                    if (settings.IsDefaultPrinter)
                        return printer;
                }
            }
            catch { }
            return "";
        }

        #endregion

        #region Validation

        public static bool ISValidTaxNo(string TaxNo)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                string msgAr = "الرقم الضريبي تم إدخاله مسبقاً، هل أنت متأكد من الاستمرار؟";
                string msgEn = "Tax No is previously inserted, Do you want to continue?";
                string msg = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase) ? msgAr : msgEn;

                // Customers
                using (var a1 = new SqlDataAdapter(
                    "select id from Customers where tax_no=N'" + TaxNo
                    + "' and IS_Deleted=0", connection))
                {
                    var dt1 = new DataTable();
                    a1.Fill(dt1);
                    if (dt1.Rows.Count > 0)
                        return MessageBox.Show(msg, "",
                            MessageBoxButton.YesNo, MessageBoxImage.Question)
                            != MessageBoxResult.No;
                }

                // Owners
                using (var a2 = new SqlDataAdapter(
                    "select id from Owners where tax_no=N'" + TaxNo
                    + "' and IS_Deleted=0", connection))
                {
                    var dt2 = new DataTable();
                    a2.Fill(dt2);
                    if (dt2.Rows.Count > 0)
                        return MessageBox.Show(msg, "",
                            MessageBoxButton.YesNo, MessageBoxImage.Question)
                            != MessageBoxResult.No;
                }

                // PM_Contractor
                using (var a3 = new SqlDataAdapter(
                    "select id from PM_Contractor where tax_no=N'" + TaxNo
                    + "' and IS_Deleted=0", connection))
                {
                    var dt3 = new DataTable();
                    a3.Fill(dt3);
                    if (dt3.Rows.Count > 0)
                        return MessageBox.Show(msg, "",
                            MessageBoxButton.YesNo, MessageBoxImage.Question)
                            != MessageBoxResult.No;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidEmailFormat(string s)
        {
            return Regex.IsMatch(s,
                @"^([0-9a-zA-Z]([-\.\\w]*[0-9a-zA-Z])*@"
                + @"([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$");
        }

        public static bool AllowEdit(string frmName)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select ISNULL(IS_Edit,1) IS_Edit "
                    + "from User_Permissions,Forms "
                    + "where User_Permissions.Form_id=Forms.id "
                    + "and Forms.FormName=N'" + frmName + "' "
                    + "and user_id=" + MainClass.UserID,
                    connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                return dt.Rows.Count > 0
                    ? Convert.ToBoolean(dt.Rows[0]["IS_Edit"])
                    : true;
            }
            catch
            {
                return true;
            }
        }

        public static bool CheckProiedAcc(DateTime InvoiceDate)
        {
            try
            {
                string connStr = BuildMasterConnectionString();
                using var sqlConnection = new SqlConnection(connStr);
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();
                var frmUserLogin = new frmUserLogin();
                using var adapter = new SqlDataAdapter(
                    "SELECT * FROM master.dbo.DatabasesManagment "
                    + "where IsActive=1 and IsDeleted=0 "
                    + "and DbAutoName=N'" + frmUserLogin.txtDatabase + "'",
                    sqlConnection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    DateTime end = Convert.ToDateTime(row["AccountingPeriodEnd"]);
                    DateTime start = Convert.ToDateTime(row["AccountingPeriodStart"]);
                    return !(InvoiceDate > end || InvoiceDate < start) || true;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        public static bool CheckReffNo(string ReffNo, int Cust_id, int InvNo)
        {
            try
            {
                using var connection = new SqlConnection(MainClass.connstr);
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using var cmd = new SqlCommand(
                    "SELECT * FROM inv where Reff_No=@Reff_No "
                    + "and inv_type=1 and proc_type=1 "
                    + "and Cust_id=@Cust_id and id<>@invNo",
                    connection);
                cmd.Parameters.AddWithValue("@Reff_No", ReffNo);
                cmd.Parameters.AddWithValue("@Cust_id", Cust_id);
                cmd.Parameters.AddWithValue("@invNo", InvNo);

                using var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region License & Device

        public static string CheckDeviceLicense()
        {
            try
            {
                string connStr = "server=" + MainClass.Server
                    + ";database=master;MultipleActiveResultSets=True;"
                    + "user id=" + MainClass.NetUserId
                    + "; pwd=" + MainClass.NetPwd;
                using var connection = new SqlConnection(connStr);
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string uid1 = HardwareInfo.GenerateUID("SmartAuditERP", true);
                string uid2 = HardwareInfo.GenerateUID("SmartAuditERP", false);

                string sql =
                    "select License.LicenseDate,License.LicenseExpire,"
                    + "License.LicenseType,License.LicenseStatus,"
                    + "DeviceStatus,License.LicenseFeatures "
                    + "from License,LicenseDevices "
                    + "where License.licenseID=LicenseDevices.licenseID "
                    + "and License.LicenseStatus=1 "
                    + "and LicenseDevices.IsDeleted=0 "
                    + "and LicenseDevices.DeviceID=@Uid";

                using var adapter = new SqlDataAdapter(sql, connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Uid", uid1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    adapter.SelectCommand.Parameters["@Uid"].Value = uid2;
                    dt.Clear();
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    DateTime expire = Convert.ToDateTime(dt.Rows[0]["LicenseExpire"]);
                    return DateTime.Compare(expire, DateTime.Now) >= 0
                        ? dt.Rows[0]["LicenseFeatures"].ToString()
                        : "False";
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public static LicensingOrder GetLicenseClient()
        {
            try
            {
                string connStr = BuildMasterConnectionString();
                using var connection = new SqlConnection(connStr);
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using var adapter = new SqlDataAdapter(
                    "select * from License", connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    licensing.licenseCode = row["licenseID"].ToString().Trim();
                    licensing.ClientName = row["ClientName"].ToString().Trim();
                    licensing.ClientEmail = row["ClientEmail"].ToString().Trim();
                    licensing.ClientMobile = row["ClientMobile"].ToString().Trim();
                    licensing.country = row["country"].ToString().Trim();
                    licensing.city = row["City"].ToString().Trim();
                    licensing.Address = row["Address"].ToString().Trim();
                    licensing.bsn_no = row["bsn_no"].ToString().Trim();
                    licensing.LicenseType = row["LicenseType"].ToString().Trim();
                    licensing.LiciensesNo = row["LiciensesNo"] as int?;
                    licensing.LicenseDate = row["LicenseDate"] as DateTime?;
                    licensing.LicenseExpire = Convert.ToDateTime(
                        row["LicenseExpire"].ToString().Trim());
                    licensing.Salesman = row["LicenseEmp"].ToString().Trim();
                }
                return licensing;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Permissions (WPF)

        /// <summary>
        /// تطبيق صلاحيات التعديل/الإضافة على النافذة - متوافق مع WPF
        /// </summary>
        public static void ApplyEditAddPermission(
            object frm,
            bool IsNew,
            FormPermission frmPer)
        {
            if (frm == null || frmPer == null) return;

            if (frm is System.Windows.Window window)
                ApplyEditAddPermissionInternal(window, IsNew, frmPer);
            else if (frm is System.Windows.FrameworkElement fe)
                ApplyEditAddPermissionInternal(fe, IsNew, frmPer);
        }

        private static void ApplyEditAddPermissionInternal(
            System.Windows.FrameworkElement root,
            bool IsNew,
            FormPermission frmPer)
        {
            var buttons = FindVisualChildren<System.Windows.Controls.Button>(root);

            foreach (var button in buttons)
            {
                if (string.IsNullOrWhiteSpace(button.Name)) continue;

                string name = button.Name.Trim();

                bool isSaveBtn =
                    string.Equals(name, "btnSave", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "btnSavePrint",
                        StringComparison.OrdinalIgnoreCase);

                if (!isSaveBtn) continue;

                if (!frmPer.Addable && IsNew)
                {
                    button.IsEnabled = false;
                    button.Background = System.Windows.Media.Brushes.Gray;
                }
                else if (frmPer.Addable && IsNew)
                {
                    button.IsEnabled = true;
                }
                else if (!frmPer.Editable && !IsNew)
                {
                    button.IsEnabled = false;
                    button.Background = System.Windows.Media.Brushes.Gray;
                }
                else if (frmPer.Editable && !IsNew)
                {
                    button.IsEnabled = true;
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(
            System.Windows.DependencyObject dependencyObject)
            where T : System.Windows.DependencyObject
        {
            if (dependencyObject == null) yield break;

            int count = System.Windows.Media.VisualTreeHelper
                .GetChildrenCount(dependencyObject);

            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper
                    .GetChild(dependencyObject, i);

                if (child is T typedChild)
                    yield return typedChild;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        #endregion

        #region Database Management

        public static void AddPreviousDBs()
        {
            try
            {
                var databasesWithTable = GetDatabasesWithTable("Accounts_Index");
                conn = new SqlConnection(BuildMasterConnectionString());
                if (conn.State != ConnectionState.Open) conn.Open();

                // إنشاء جدول DatabasesManagment إن لم يوجد
                new SqlCommand(@"
                    IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME='DatabasesManagment')
                    BEGIN
                    CREATE TABLE [dbo].[DatabasesManagment](
                        [DbId] [int] NOT NULL,
                        [DbAutoName] [nvarchar](50) NOT NULL,
                        [Dbname] [nvarchar](50) NULL,
                        [AccountingPeriodStart] [datetime] NULL,
                        [AccountingPeriodEnd] [datetime] NULL,
                        [UserName] [nvarchar](50) NULL,
                        [UseFullName] [nvarchar](50) NULL,
                        [passward] [nvarchar](20) NULL,
                        [DefaultCoin] [nvarchar](30) NULL,
                        [IsActive] [bit] NULL,
                        [IsDeleted] [bit] NULL,
                        [ImportanceOrder] [int] NULL,
                        [CreateTime] [datetime] NULL,
                        [UserCreate] [int] NULL,
                        CONSTRAINT [PK_DatabasesManagment] PRIMARY KEY CLUSTERED ([DbId] ASC)
                    )
                    End", conn).ExecuteNonQuery();

                foreach (var item in databasesWithTable)
                {
                    var cmd = new SqlCommand(@"
                        insert into master.dbo.DatabasesManagment(
                            [DbId],[DbAutoName],[Dbname],[CreateTime],[UserCreate],
                            [AccountingPeriodStart],[AccountingPeriodEnd],
                            [UserName],[UseFullName],[passward],[DefaultCoin],
                            [IsActive],[IsDeleted],[ImportanceOrder])
                        VALUES(@DbId,@DbAutoName,@Dbname,@CreateTime,@UserCreate,
                            @AccountingPeriodStart,@AccountingPeriodEnd,
                            @UserName,@UseFullName,@passward,@DefaultCoin,
                            @IsActive,@IsDeleted,@ImportanceOrder)", conn);

                    cmd.Parameters.AddWithValue("@DbId", item.ID);
                    cmd.Parameters.AddWithValue("@DbAutoName", item.Name);
                    cmd.Parameters.AddWithValue("@Dbname", item.Name);
                    cmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@UserCreate", 1);
                    cmd.Parameters.AddWithValue("@AccountingPeriodStart", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AccountingPeriodEnd",
                        DateTime.Now.AddYears(1));
                    cmd.Parameters.AddWithValue("@UserName", "1");
                    cmd.Parameters.AddWithValue("@UseFullName", "1");
                    cmd.Parameters.AddWithValue("@passward", "-1");
                    cmd.Parameters.AddWithValue("@DefaultCoin", "");
                    cmd.Parameters.AddWithValue("@IsActive", 1);
                    cmd.Parameters.AddWithValue("@IsDeleted", 0);
                    cmd.Parameters.AddWithValue("@ImportanceOrder", 1);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddPreviousDBs Error: " + ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        public static List<DatabaseInfo> GetDatabasesWithTable(string tableName)
        {
            var list = new List<DatabaseInfo>();
            try
            {
                string connStr = BuildMasterConnectionString();
                using var masterConn = new SqlConnection(connStr);
                masterConn.Open();

                using var cmd = new SqlCommand(
                    "SELECT name,dbid FROM master.dbo.sysdatabases "
                    + "WHERE dbid > 4 and dbid Not in ("
                    + "SELECT DatabasesManagment.dbid "
                    + "FROM master.dbo.DatabasesManagment)",
                    masterConn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string dbName = reader["name"].ToString();
                    int dbId = Convert.ToInt32(reader["dbid"]);

                    using var checkConn = new SqlConnection(connStr);
                    checkConn.Open();
                    using var checkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM [{dbName}].INFORMATION_SCHEMA.TABLES "
                        + "WHERE TABLE_NAME=@TableName",
                        checkConn);
                    checkCmd.Parameters.AddWithValue("@TableName", tableName);

                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                        list.Add(new DatabaseInfo { Name = dbName, ID = dbId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDatabasesWithTable Error: " + ex.Message);
            }
            return list;
        }

        public static void GetValidDBname(ref string Dbname, ref int Id)
        {
            string connStr = BuildMasterConnectionString();
            using var conn = new SqlConnection(connStr);
            if (conn.State != ConnectionState.Open) conn.Open();

            using var adapter = new SqlDataAdapter(
                "SELECT max(dbid) FROM master.dbo.sysdatabases", conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            double maxId = Convert.ToDouble(dt.Rows[0][0]);
            string numStr = (maxId + 1).ToString();
            Dbname = "Data" + numStr;

            bool found;
            do
            {
                using var checkAdp = new SqlDataAdapter(
                    "SELECT dbid FROM master.dbo.sysdatabases where name=N'"
                    + Dbname + "'", conn);
                var dtCheck = new DataTable();
                checkAdp.Fill(dtCheck);

                if (dtCheck.Rows.Count == 1)
                {
                    numStr = (Convert.ToDouble(numStr) + 1).ToString();
                    Dbname = "Data" + numStr;
                    found = false;
                }
                else
                {
                    found = true;
                }
            }
            while (!found);

            Id = Convert.ToInt32(numStr);
        }

        #endregion

        #region Trial Check

        public static bool CheckIsTrial()
        {
            try
            {
                using var connection = MainClass.ConnObj();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                double maxEntry = Convert.ToDouble(
                    new SqlCommand("select max(id) from Entry", connection)
                        .ExecuteScalar());
                double maxInv = Convert.ToDouble(
                    new SqlCommand("select max(Proc_id) from Inv", connection)
                        .ExecuteScalar());

                return maxEntry >= 50.0 || maxInv >= 100.0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Close Day / Cashier

        public static CloseShiftSetting CloseDaySetting()
        {
            var setting = new CloseShiftSetting();
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    "select * from SettingCloseShift", connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    setting.IncSaleInv = Convert.ToBoolean(row["SaleInv"]);
                    setting.IncPurchaseInv = Convert.ToBoolean(row["PurchaseInv"]);
                    setting.IncReceipts = Convert.ToBoolean(row["Receipts"]);
                    setting.IncExpenses = Convert.ToBoolean(row["Expenses"]);
                    setting.QuantPrint = Convert.ToBoolean(row["PrintTotItem"]);
                    setting.PrintGroups = Convert.ToBoolean(row["PrintTotGroup"]);
                    setting.printNo = Convert.ToInt32(row["printNo"]);
                    setting.IncPOS = Convert.ToBoolean(row["POS"]);
                    setting.SendClose = Convert.ToBoolean(row["SendEmail"]);
                    setting.BalanceRequired = Convert.ToBoolean(row["BalanceRequired"]);
                    setting.ShowCloseDetails = row["ShowCloseDetails"] != DBNull.Value
                        && Convert.ToBoolean(row["ShowCloseDetails"]);
                }
                return setting;
            }
            catch
            {
                return null;
            }
        }

        public static void CheckCashierClose(int type)
        {
            try
            {
                // ══ جلب إعدادات إغلاق الشيفت ══
                CloseShiftSetting closeShiftSetting = CloseDaySetting();

                if (closeShiftSetting == null)
                {
                    closeShiftSetting = new CloseShiftSetting
                    {
                        IncPOS = true,
                        IncSaleInv = false
                    };
                }

                using var connection = MainClass.ConnObj();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                // ══ الخطوة 1: جلب آخر إغلاق للمستخدم ══
                var cmdMax = new SqlCommand(
                    "SELECT ISNULL(MAX(id), 0) FROM CasherClosed WHERE user_id=@EmpId",
                    connection);
                cmdMax.Parameters.AddWithValue("@EmpId", MainClass.EmpNo);

                int maxCloseId = Convert.ToInt32(cmdMax.ExecuteScalar());

                if (maxCloseId > 0)
                {
                    // ✅ يوجد إغلاق سابق - جلب وقت الإغلاق
                    using var adapter = new SqlDataAdapter(
                        "SELECT endTime FROM CasherClosed WHERE id=@Id",
                        connection);
                    adapter.SelectCommand.Parameters.AddWithValue("@Id", maxCloseId);

                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count == 1 && dt.Rows[0]["endTime"] != DBNull.Value)
                    {
                        MainClass.lastCashercloseDate =
                            Convert.ToDateTime(dt.Rows[0]["endTime"]);
                    }
                    else
                    {
                        // ✅ استخدام وقت الدخول المحفوظ في MainClass
                        MainClass.lastCashercloseDate = MainClass.LoginDate;
                    }
                }
                else
                {
                    // ══ لا يوجد إغلاق - البحث عن أول فاتورة ══
                    string sqlFirstInv = type == 1
                        ? "SELECT TOP 1 date FROM inv " +
                          "WHERE IS_Deleted=0 AND sales_emp=@EmpId " +
                          "ORDER BY id ASC"
                        : "SELECT TOP 1 date FROM RentInvoice " +
                          "WHERE IS_Deleted=0 " +
                          "ORDER BY id ASC";

                    using var adapter2 = new SqlDataAdapter(sqlFirstInv, connection);
                    if (type == 1)
                        adapter2.SelectCommand.Parameters
                            .AddWithValue("@EmpId", MainClass.EmpNo);

                    var dt2 = new DataTable();
                    adapter2.Fill(dt2);

                    if (dt2.Rows.Count > 0 && dt2.Rows[0]["date"] != DBNull.Value)
                    {
                        try
                        {
                            MainClass.lastCashercloseDate =
                                Convert.ToDateTime(dt2.Rows[0]["date"]);
                        }
                        catch
                        {
                            // ✅ إذا فشل التحويل - استخدام وقت الدخول
                            MainClass.lastCashercloseDate = MainClass.LoginDate;
                        }
                    }
                    else
                    {
                        // ✅ لا توجد فواتير - استخدام وقت الدخول
                        MainClass.lastCashercloseDate = MainClass.LoginDate;
                    }
                }

                // ══ الخطوة 2: بناء شرط البحث ══
                string branchCond = "";
                if (MainClass.BranchNo != -1)
                    branchCond = $"inv.branch={MainClass.BranchNo} AND ";

                // ✅ إضافة شرط inv_type المفقود - مطابق للكود الأصلي
                string invTypeCond = "";
                if (closeShiftSetting.IncSaleInv && closeShiftSetting.IncPOS)
                    invTypeCond = "(inv.inv_type=3 OR inv.inv_type=2) AND ";
                else if (!closeShiftSetting.IncSaleInv && closeShiftSetting.IncPOS)
                    invTypeCond = "inv.inv_type=3 AND ";

                // ══ الخطوة 3: البحث عن فواتير غير مغلقة ══
                string sqlUnclosed;
                if (type == 1)
                {
                    sqlUnclosed =
                        $"SELECT MIN(date) AS Invdate FROM Inv " +
                        $"WHERE {branchCond}" +
                        $"{invTypeCond}" +
                        $"(inv.proc_type=1 OR inv.proc_type=2) " +
                        $"AND Inv.date > @date1 " +
                        $"AND inv.pay_type <> -1 " +
                        $"AND inv.pay_type <> 5 " +
                        $"AND Inv.IS_Deleted=0 " +
                        $"AND sales_emp={MainClass.EmpNo}";
                }
                else
                {
                    sqlUnclosed =
                        "SELECT MIN(date) AS Invdate FROM RentInvoice " +
                        "WHERE date > @date1 AND IS_Deleted=0";
                }

                using var adapter3 = new SqlDataAdapter(sqlUnclosed, connection);

                // ✅ تمرير التاريخ بنفس طريقة الكود الأصلي
                adapter3.SelectCommand.Parameters
                    .Add("@date1", SqlDbType.DateTime)
                    .Value = MainClass.lastCashercloseDate
                                 .ToString("yyyy-MM-dd'T'HH:mm:ss zzz");

                var dt3 = new DataTable();
                adapter3.Fill(dt3);

                // ══ الخطوة 4: عرض النتيجة ══
                if (dt3.Rows.Count > 0 && dt3.Rows[0]["Invdate"] != DBNull.Value)
                {
                    MainClass.lastCashercloseDate =
                        Convert.ToDateTime(dt3.Rows[0]["Invdate"]);

                    // ✅ تحديث الـ Home الفعلي
                    if (System.Windows.Application.Current?.MainWindow is Home homeWindow)
                        homeWindow.IsCashierClosed = false;

                    MessageBox.Show(
                        "يوجد فواتير لم يتم إغلاقها",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else
                {
                    MainClass.lastCashercloseDate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckCashierClose Error: {ex.Message}");
                MessageBox.Show(
                    "وقت إغلاق اليومية غير صحيح",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Salesman Settings

        public static void SalesmanSetting()
        {
            try
            {
                conn = new SqlConnection(BuildMasterConnectionString());
                if (conn.State != ConnectionState.Open) conn.Open();

                using var adapter = new SqlDataAdapter(
                    "select code,ISNULL(address,'') as address,"
                    + "ISNULL(mobileNo,'') as mobileNo,"
                    + "ISNULL(website,'') as website,"
                    + "ISNULL(email,'') as email "
                    + "from SalesmanSetting where IS_Active=1", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                string code = "100";
                if (dt.Rows.Count > 0)
                {
                    code = dt.Rows[0]["code"].ToString();
                    AppSalesmaneAdress = dt.Rows[0]["address"].ToString();
                    AppSalesmaneMobie = dt.Rows[0]["mobileNo"].ToString();
                    AppSalesmaneWebsite = dt.Rows[0]["website"].ToString();
                    AppSalesmaneEmail = dt.Rows[0]["email"].ToString();
                }

                switch (code)
                {
                    case "100":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.MainBG1);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.LoginBG2);
                        SupportBG = Properties.Resources.SupportPn;
                        break;
                    case "0030":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.HomeBG101);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.HomeBG101);
                        SupportBG = Properties.Resources.HomeBG101;
                        break;
                    case "0036":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.NADUS);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.NADUS);
                        break;
                    case "0038":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.NadusEgypt);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.NadusEgypt);
                        break;
                    case "0050":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.PARROT);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.PARROT);
                        break;
                    case "0048":
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.Qsmart);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.Qsmart);
                        break;
                    default:
                        MainClass.MainBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.MainBG1);
                        MainClass.LoginBG = MainClass.DrawingBitmapToBitmapImage(Properties.Resources.LoginBG2);
                        SupportBG = Properties.Resources.SupportPn;
                        break;
                }
            }
            catch { }
        }

        public static void AddSalesmanSettings()
        {
            try
            {
                conn = new SqlConnection(BuildMasterConnectionString());
                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(@"
                    IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME='SalesmanSetting')
                    BEGIN
                    CREATE TABLE [dbo].[SalesmanSetting](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        [SalesmanName] [nvarchar](100) NULL,
                        [code] [nvarchar](50) NULL,
                        [is_Active] [bit] NULL,
                        [email] [nvarchar](50) NULL,
                        [mobileNo] [nvarchar](50) NULL,
                        [address] [nvarchar](50) NULL,
                        [website] [nvarchar](100) NULL,
                        CONSTRAINT [PK_SalesmanSetting] PRIMARY KEY CLUSTERED
                        ([Id] ASC)
                    )
                    End", conn).ExecuteNonQuery();

                using var adapter = new SqlDataAdapter(
                    "select * from [dbo].[SalesmanSetting]", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    string insertSql =
                        "INSERT INTO [dbo].[SalesmanSetting] ([SalesmanName],[code],[is_Active])"
                        + "VALUES(N'المدقق',N'100',0) "
                        + "INSERT INTO [dbo].[SalesmanSetting] ([SalesmanName],[code],[is_Active])"
                        + "VALUES(N'درب الريادة',N'0030',0) "
                        + "INSERT INTO [dbo].[SalesmanSetting] ([SalesmanName],[code],[is_Active])"
                        + "VALUES(N'الشبكة العلمية',N'0031',0) "
                        + "INSERT INTO [dbo].[SalesmanSetting] ([SalesmanName],[code],[is_Active])"
                        + "VALUES(N'Parrot programming',N'0050',0) "
                        + "INSERT INTO [dbo].[SalesmanSetting] ([SalesmanName],[code],[is_Active])"
                        + "VALUES(N'Qsmart',N'0048',0)";
                    new SqlCommand(insertSql, conn).ExecuteNonQuery();
                }
            }
            catch { }
        }

        public static void AddSettingNotify()
        {
            try
            {
                conn = new SqlConnection(BuildMasterConnectionString());
                if (conn.State != ConnectionState.Open) conn.Open();

                new SqlCommand(@"
                    IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME='SettingNotify')
                    BEGIN
                    CREATE TABLE [dbo].[SettingNotify](
                        [id] [int] IDENTITY(1,1) NOT NULL,
                        [clientid] [nvarchar](50) NULL,
                        [nodeid] [nvarchar](50) NULL,
                        [frequency] [nvarchar](50) NULL,
                        [sale] [nvarchar](50) NULL,
                        [receipt] [nvarchar](50) NULL,
                        [income] [nvarchar](50) NULL,
                        [finance] [nvarchar](50) NULL,
                        [passcode] [nvarchar](50) NULL,
                        [servername] [nvarchar](50) NULL,
                        [usrname] [nvarchar](50) NULL,
                        [pwd] [nvarchar](50) NULL,
                        [DB] [nvarchar](50) NULL,
                        [publickey] [nvarchar](50) NULL,
                        [privatekey] [nvarchar](50) NULL,
                        [IS_Play] [bit] NULL,
                        [inventory] [nvarchar](50) NULL,
                        [is_active] [bit] null,
                        CONSTRAINT [PK_SettingNotify] PRIMARY KEY ([id] ASC)
                    )
                    End", conn).ExecuteNonQuery();
            }
            catch { }
        }

        public static void AddGediaSettins()
        {
            try
            {
                string connStr = BuildMasterConnectionString();
                using var connection = new SqlConnection(connStr);
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                new SqlCommand(StoredQueries.CreateGediaSettingTb,
                    connection).ExecuteNonQuery();
                new SqlCommand(StoredQueries.InsertGediaSettingTb,
                    connection).ExecuteNonQuery();
            }
            catch { }
        }

        #endregion

        #region Logging

        public static void SetUpLogDbConnection(string connectionString, string logConfig)
        {
            try
            {
                var hierarchy = LogManager.GetRepository() as Hierarchy;
                XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfig));

                if (hierarchy == null) return;

                foreach (var appender in hierarchy.GetAppenders()
                    .OfType<AdoNetAppender>())
                {
                    appender.ConnectionString = connectionString;
                    appender.ActivateOptions();
                }
            }
            catch { }
        }

        #endregion

        #region Online / Version


        /// <summary>
        /// قراءة معلومات الإصدار من Version.txt المحلي
        /// </summary>
        public static VersionInfo CurrentVersion()
        {
            try
            {
                string versionFilePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Version.txt");

                if (!File.Exists(versionFilePath))
                    return new VersionInfo
                    {
                        Version = "0",
                        VersionText = "0.0.0.0",
                        Description = "",
                        TypeUpdates = 0,
                        PublishDate = ""
                    };

                string json = File.ReadAllText(versionFilePath,
                    System.Text.Encoding.UTF8);

                // ✅ Newtonsoft.Json (الموجود في مشروعك)
                return JsonConvert.DeserializeObject<VersionInfo>(json);
            }
            catch
            {
                return new VersionInfo
                {
                    Version = "0",
                    VersionText = "0.0.0.0",
                    Description = "",
                    TypeUpdates = 0,
                    PublishDate = ""
                };
            }
        }

        /// <summary>
        /// التحقق من الإصدار الأحدث عبر الإنترنت
        /// </summary>
        public static void GetOnlineVersionInfo()
        {
            try
            {
                // قراءة الإصدار المحلي
                var currentVer = CurrentVersion();

                // التحقق من الاتصال بالإنترنت
                if (!MainClass.CheckForInternetConnection()) return;

                // جلب الإصدار من الإنترنت
                var onlineVer = OnlineVersion();
                if (onlineVer == null) return;

                // مقارنة: إذا كان التحديث Critical والإصدار أحدث
                if (onlineVer.TypeUpdates == (int)TypeUpdate.Critical)
                {
                    decimal onlineVersionNo = 0;
                    decimal currentVersionNo = 0;

                    decimal.TryParse(onlineVer.Version, out onlineVersionNo);
                    decimal.TryParse(currentVer.Version, out currentVersionNo);

                    if (onlineVersionNo > currentVersionNo)
                    {
                        // ✅ WPF MessageBox
                        System.Windows.MessageBox.Show(
                            "نتمنى التواصل مع الدعم الفني لتحديث النظام " +
                            "على الرقم الموحد 920009656",
                            "تحديث مهم متوفر",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                }
            }
            catch
            {
                // صامت
            }
        }

        /// <summary>
        /// جلب معلومات الإصدار من الإنترنت (GitHub أو سيرفر خاص)
        /// </summary>
        public static VersionInfo OnlineVersion()
        {
            try
            {
                string url =
                    $"https://raw.githubusercontent.com/" +
                    $"{MainClass.GITHUB_USER}/{MainClass.GITHUB_REPO}" +
                    $"/main/Version.txt";

                var request = (System.Net.HttpWebRequest)
                    System.Net.WebRequest.Create(url);
                request.UserAgent = "AccountingStandard-VersionCheck";
                request.Method = "GET";
                request.Timeout = 10000;

                string json;
                using (var response = (System.Net.HttpWebResponse)
                           request.GetResponse())
                using (var reader = new StreamReader(
                           response.GetResponseStream(),
                           System.Text.Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                return JsonConvert.DeserializeObject<VersionInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Misc

        public static bool GetZatcaActive()
        {
            try
            {
                using var conn = new SqlConnection(MainClass.connstr);
                conn.Open();
                int count = Convert.ToInt32(
                    new SqlCommand(
                        "SELECT COUNT(*) FROM ZatcaCredential WHERE ID=1",
                        conn).ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        public static object GetMaX_ID(string ColumnName, string TableName)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                using var adapter = new SqlDataAdapter(
                    $"select MAX({ColumnName}) from {TableName}", connection);
                var dt = new DataTable();
                adapter.Fill(dt);

                var val = dt.Rows[0][0];
                return val == DBNull.Value
                    ? 1
                    : Convert.ToInt32(val) + 1;
            }
            catch
            {
                return 1;
            }
        }

        public static bool Is_Empty(string text, string msg)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(" " + msg, "", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return true;
            }
            return false;
        }

        public static object OpenReportDesgin(string RptName, int Invid)
        {
            try
            {
                string reportsPath = MainClass.ReportsPath;
                using var connection = new SqlConnection(MainClass.connstr);
                connection.Open();

                using var adapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=@Id",
                    connection);
                adapter.SelectCommand.Parameters.AddWithValue("@Id", Invid);
                var dt = new DataTable();
                adapter.Fill(dt);

                reportsPath = dt.Rows.Count > 0
                    ? dt.Rows[0]["RptUrl"].ToString()
                    : System.IO.Path.Combine(reportsPath, RptName);

                var designer = new frm_ReportDesign();
                designer.ReportDesigner1.OpenReport(reportsPath);
                designer.Show();
            }
            catch { }
            return null;
        }

        /// <summary>
        /// مساعد لتعيين نص على txtAlarm سواء TextBox أو RichTextBox
        /// </summary>
        private static void SetTextOnAlarm(object alarmControl, string text)
        {
            if (alarmControl is System.Windows.Controls.TextBox tb)
            {
                tb.Text = text;
            }
            else if (alarmControl is System.Windows.Controls.RichTextBox rtb)
            {
                rtb.Document.Blocks.Clear();
                rtb.Document.Blocks.Add(
                    new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(text)));
            }
        }

        #endregion

        #region Connection String Helper

        private static string BuildMasterConnectionString()
        {
            if (MainClass.Conn_type == 1)
            {
                return MainClass.UseServerAuth
                    ? $"server={MainClass.Server};database=master;"
                      + $"MultipleActiveResultSets=True;"
                      + $"user id='{MainClass.NetUserId}';"
                      + $"pwd='{MainClass.NetPwd}'"
                    : $"server={MainClass.Server.Trim()};"
                      + "database=master;trusted_connection=true";
            }
            return $"server={MainClass.Server};database=master;"
                + $"MultipleActiveResultSets=True;"
                + $"user id='{MainClass.NetUserId}';"
                + $"pwd='{MainClass.NetPwd}'";
        }

        public static void fillcmb_All(System.Windows.Controls.ComboBox cmb, string displayMember, string valueMember, string tableName)
        {
            if (cmb == null)
                return;

            using (SqlConnection sqlConnection = MainClass.ConnObj())
            {
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

                DataTable dataTable = new DataTable();

                string query = $"SELECT {displayMember}, {valueMember} FROM {tableName}";

                using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(query, sqlConnection))
                {
                    sqlDataAdapter.Fill(dataTable);
                }

                cmb.ItemsSource = null;
                cmb.Items.Clear();

                if (dataTable.Rows.Count > 0)
                {
                    cmb.DisplayMemberPath = displayMember;
                    cmb.SelectedValuePath = valueMember;
                    cmb.ItemsSource = dataTable.DefaultView;
                }
                else
                {
                    cmb.ItemsSource = null;
                }

                cmb.SelectedIndex = -1;
            }
        }
        #endregion
    }

    #region SqlCommand Extension Helper

    internal static class SqlCommandExtensions
    {
        public static SqlCommand AddParam(
            this SqlCommand cmd, string name, object value)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return cmd;
        }

        public static object ExecuteScalarSafe(this SqlCommand cmd)
        {
            try
            {
                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();
                return cmd.ExecuteScalar() ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    #endregion
}