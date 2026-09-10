using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;

namespace SmartAuditERP
{

	[StandardModule]
	internal sealed class SendData
	{
		public static MqttClient mqttClient;

		public static string ClientCode = "1005";

		public static string connString = "";

		public static SqlConnection conn = new SqlConnection(global::SmartAuditERP.SendData.connString);

		public static string lastErrorMsg;

		public static AESencDEC ConEncrypt = new AESencDEC();

		public static string _passcode = "";

		private static global::System.Timers.Timer sendTimer;

		private static string clientId = "";

		public static string GetItems(string Cond)
		{
			string result;
			try
			{
				string cmdText;
				cmdText = "SELECT  [id], ISNULL([name],'') AS name, ISNULL([nameEN],'') AS nameEn, ISNULL([code],'') AS code,\r\n                                    ISNULL([barcode],'') AS barcode, ISNULL([Grpcode],1) AS Grpcode, ISNULL([group_id],1) AS group_id,\r\n                                    ISNULL([unit],1) AS unit, ISNULL([ShowInPOS],1) AS ShowInPOS, ISNULL([Wscale],0) AS Wscale,\r\n                                    ISNULL([purch_price],0) AS purch_price, ISNULL([sale_price],0) AS sale_price, ISNULL([limit],0) AS limit,\r\n                                    ISNULL([tax],0) AS tax, ISNULL([discount],0) AS discount, ISNULL([tax_group],0) AS tax_group,\r\n                                    ISNULL([store],1) AS store, ISNULL([IS_Deleted],0) AS IS_Deleted, ISNULL([ItemType],1) AS ItemType,\r\n                                    ISNULL([ItemProperty],'') AS ItemProperty, ISNULL([FillValue],'') AS FillValue, ISNULL([MaxQtyLimit],0) AS MaxQtyLimit,\r\n                                    ISNULL([WithholdingTax],0) AS WithholdingTax, ISNULL([EgyItemCode],'') AS EgyItemCode,\r\n                                    ISNULL([EgyCodeType],'') AS EgyCodeType, ISNULL([MaxDicountParcent],0) AS MaxDicountParcent,\r\n                                    ISNULL([is_extra_tax_applied],0) AS is_extra_tax_applied,\r\n                                    ISNULL(MaxDiscountAmount,0)as MaxDiscountAmount ,ISNULL(showInAndroid,0)as showInAndroid,ISNULL(Item_Sort,0)as Item_Sort FROM Items " + Cond + ";";
				List<ListItems> list;
				list = new List<ListItems>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListItems listItems;
						listItems = new ListItems();
						listItems.id = Conversions.ToInteger(sqlDataReader["id"]);
						listItems.name = Conversions.ToString(sqlDataReader["name"]);
						listItems.nameEN = Conversions.ToString(sqlDataReader["nameEn"]);
						listItems.code = Conversions.ToString(sqlDataReader["code"]);
						listItems.barcode = Conversions.ToString(sqlDataReader["barcode"]);
						listItems.Grpcode = Conversions.ToString(sqlDataReader["Grpcode"]);
						listItems.group_id = Conversions.ToInteger(sqlDataReader["group_id"]);
						listItems.unit = Conversions.ToInteger(sqlDataReader["unit"]);
						listItems.ShowInPOS = Conversions.ToBoolean(sqlDataReader["ShowInPOS"]);
						listItems.Wscale = Conversions.ToInteger(sqlDataReader["Wscale"]);
						listItems.purch_price = Conversions.ToDecimal(sqlDataReader["purch_price"]);
						listItems.sale_price = Conversions.ToDecimal(sqlDataReader["sale_price"]);
						listItems.limit = Conversions.ToInteger(sqlDataReader["limit"]);
						listItems.tax = Conversions.ToDecimal(sqlDataReader["tax"]);
						listItems.discount = Conversions.ToDecimal(sqlDataReader["discount"]);
						listItems.tax_group = Conversions.ToDecimal(sqlDataReader["tax_group"]);
						listItems.store = Conversions.ToInteger(sqlDataReader["store"]);
						listItems.IS_Deleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]);
						listItems.ItemType = Conversions.ToInteger(sqlDataReader["ItemType"]);
						listItems.ItemProperty = Conversions.ToInteger(sqlDataReader["ItemProperty"]);
						listItems.FillValue = Conversions.ToDecimal(sqlDataReader["FillValue"]);
						listItems.MaxQtyLimit = Conversions.ToDecimal(sqlDataReader["MaxQtyLimit"]);
						listItems.WithholdingTax = Conversions.ToDecimal(sqlDataReader["WithholdingTax"]);
						listItems.EgyItemCode = Conversions.ToString(sqlDataReader["EgyItemCode"]);
						listItems.EgyCodeType = Conversions.ToString(sqlDataReader["EgyCodeType"]);
						listItems.MaxDicountParcent = Conversions.ToDecimal(sqlDataReader["MaxDicountParcent"]);
						listItems.is_extra_tax_applied = Conversions.ToBoolean(sqlDataReader["is_extra_tax_applied"]);
						listItems.MaxDiscountAmount = Conversions.ToDecimal(sqlDataReader["MaxDiscountAmount"]);
						listItems.showInAndroid = Conversions.ToBoolean(sqlDataReader["showInAndroid"]);
						listItems.Item_Sort = Conversions.ToInteger(sqlDataReader["Item_Sort"]);
						list.Add(listItems);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				global::SmartAuditERP.SendData.lastErrorMsg = "broadcastItem: \r\n" + Conversions.ToString(DateTime.Now) + "\r\n" + ex.Message;
				Console.WriteLine(global::SmartAuditERP.SendData.lastErrorMsg);
				result = string.Empty;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetItemsUnit(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = " \r\nSELECT isnull([ItemId],0) as Itemid\r\n      ,isnull([unit],1)as unit\r\n      ,isnull([perc],0) as perc\r\n      ,isnull([purch],0)as purch\r\n      ,isnull([sale],1) as sale\r\n      ,isnull([barcode],'')as barcode\r\n      FROM ItemUnits " + Cond + " ;\r\n";
				List<ListItemunit> list;
				list = new List<ListItemunit>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListItemunit listItemunit;
					listItemunit = new ListItemunit();
					listItemunit.Itemid = Conversions.ToInteger(sqlDataReader["Itemid"]);
					listItemunit.unite = Conversions.ToString(sqlDataReader["unit"]);
					listItemunit.perc = Conversions.ToString(sqlDataReader["perc"]);
					listItemunit.purch = Conversions.ToString(sqlDataReader["purch"]);
					listItemunit.sale = Conversions.ToString(sqlDataReader["sale"]);
					listItemunit.barcode = Conversions.ToString(sqlDataReader["barcode"]);
					list.Add(listItemunit);
				}
				sqlDataReader.Close();
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				global::SmartAuditERP.SendData.lastErrorMsg = "broadcastItem:\r\n" + Conversions.ToString(DateTime.Now) + "\r\n" + ex.Message;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetItemPrices(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\nSELECT \r\n    ISNULL([Proc_id], 0) AS Proc_id,\r\n    ISNULL([ItemID], 0) AS ItemID,\r\n    ISNULL([UnitID], 0) AS UnitID,\r\n    ISNULL([time], '') AS time,\r\n    ISNULL([date], '') AS date,\r\n    ISNULL([purch_price], 0) AS purch_price,\r\n    ISNULL([sale_price], 0) AS sale_price,\r\n    ISNULL([low_purch_price], 0) AS low_purch_price,\r\n    ISNULL([high_purch_price], 0) AS high_purch_price,\r\n    ISNULL([low_sale_price], 0) AS low_sale_price,\r\n    ISNULL([high_sale_price], 0) AS high_sale_price,\r\n    ISNULL([CompetitorPrice], 0) AS CompetitorPrice,\r\n    ISNULL([Emp], '') AS Emp,\r\n    ISNULL([IS_Deleted], 0) AS IS_Deleted,\r\n    ISNULL([ConsumerPrice], 0) AS ConsumerPrice,\r\n    ISNULL([WholesalePrice], 0) AS WholesalePrice\r\nFROM \r\n    ItemPrices " + Cond + " ;\r\n";
				List<ItemPrices> list;
				list = new List<ItemPrices>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ItemPrices itemPrices;
					itemPrices = new ItemPrices();
					itemPrices.Proc_id = Conversions.ToInteger(sqlDataReader["Proc_id"]);
					itemPrices.ItemID = Conversions.ToInteger(sqlDataReader["ItemID"]);
					itemPrices.UnitID = Conversions.ToInteger(sqlDataReader["UnitID"]);
					itemPrices.Time = Conversions.ToString(sqlDataReader["time"]);
					itemPrices.Date = Conversions.ToDate(sqlDataReader["date"]);
					itemPrices.PurchPrice = Conversions.ToDecimal(sqlDataReader["purch_price"]);
					itemPrices.SalePrice = Conversions.ToDecimal(sqlDataReader["sale_price"]);
					itemPrices.LowPurchPrice = Conversions.ToDecimal(sqlDataReader["low_purch_price"]);
					itemPrices.HighPurchPrice = Conversions.ToDecimal(sqlDataReader["high_purch_price"]);
					itemPrices.LowSalePrice = Conversions.ToDecimal(sqlDataReader["low_sale_price"]);
					itemPrices.HighSalePrice = Conversions.ToDecimal(sqlDataReader["high_sale_price"]);
					itemPrices.CompetitorPrice = Conversions.ToDecimal(sqlDataReader["CompetitorPrice"]);
					itemPrices.Emp = Conversions.ToString(sqlDataReader["Emp"]);
					itemPrices.IS_Deleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]);
					itemPrices.ConsumerPrice = Conversions.ToDecimal(sqlDataReader["ConsumerPrice"]);
					itemPrices.WholesalePrice = Conversions.ToDecimal(sqlDataReader["WholesalePrice"]);
					list.Add(itemPrices);
				}
				sqlDataReader.Close();
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				global::SmartAuditERP.SendData.lastErrorMsg = "broadcastItem:\r\n" + Conversions.ToString(DateTime.Now) + "\r\n" + ex.Message;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetItemComponents(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\n        SELECT \r\n            ISNULL([Id], 0) AS [Id],\r\n            ISNULL([itemId], 0) AS [itemId],\r\n            ISNULL([ComponentId], 0) AS [ComponentId],\r\n            ISNULL([price], 0) AS [price],\r\n            ISNULL([quantity], 0) AS [quantity],\r\n            ISNULL([unit], '') AS [unit],\r\n            ISNULL([total], 0) AS [total],\r\n            ISNULL([type], 0) AS [type],\r\n            ISNULL([store], '') AS [store]\r\n        FROM ItemComponents " + Cond + ";\r\n    ";
				List<ItemComponent> list;
				list = new List<ItemComponent>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ItemComponent itemComponent;
					itemComponent = new ItemComponent();
					itemComponent.Id = Conversions.ToInteger(sqlDataReader["Id"]);
					itemComponent.ItemId = Conversions.ToInteger(sqlDataReader["itemId"]);
					itemComponent.ComponentId = Conversions.ToInteger(sqlDataReader["ComponentId"]);
					itemComponent.Price = Conversions.ToDecimal(sqlDataReader["price"]);
					itemComponent.Quantity = Conversions.ToInteger(sqlDataReader["quantity"]);
					itemComponent.Unit = sqlDataReader["unit"].ToString();
					itemComponent.Total = Conversions.ToDecimal(sqlDataReader["total"]);
					itemComponent.Type = Conversions.ToInteger(sqlDataReader["type"]);
					itemComponent.Store = sqlDataReader["store"].ToString();
					list.Add(itemComponent);
				}
				sqlDataReader.Close();
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetItemSerialNo(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\n        SELECT \r\n    ISNULL([id], 0) AS [id], \r\n    ISNULL([ItemId], 0) AS [ItemId], \r\n    ISNULL([SerialNo], '') AS [SerialNo], \r\n    ISNULL([InvGlobalID], 0) AS [InvGlobalID]\r\nFROM \r\n    ItemSerialNo " + Cond + ";\r\n;\r\n    ";
				List<ItemSerialNo> list;
				list = new List<ItemSerialNo>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ItemSerialNo itemSerialNo;
					itemSerialNo = new ItemSerialNo();
					itemSerialNo.Id = Conversions.ToInteger(sqlDataReader["Id"]);
					itemSerialNo.ItemId = Conversions.ToInteger(sqlDataReader["itemId"]);
					itemSerialNo.SerialNo = Conversions.ToInteger(sqlDataReader["SerialNo"]);
					itemSerialNo.InvGlobalID = Conversions.ToDecimal(sqlDataReader["InvGlobalID"]);
					list.Add(itemSerialNo);
				}
				sqlDataReader.Close();
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetCustomers(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string idScan;
				idScan = string.Empty;
				string cmdText;
				cmdText = "\r\n        SELECT \r\n            ISNULL([id], 0) AS [id],\r\n            ISNULL([name], '') AS [name],\r\n            ISNULL([country], '') AS [country],\r\n            ISNULL([city], '') AS [city],\r\n            ISNULL([area], '') AS [area],\r\n            ISNULL([act], 0) AS [act],\r\n            ISNULL([national_id], '') AS [national_id],\r\n            ISNULL([tel], '') AS [tel],\r\n            ISNULL([mobile], '') AS [mobile],\r\n            ISNULL([fax], '') AS [fax],\r\n            ISNULL([email], '') AS [email],\r\n            ISNULL([notes], '') AS [notes],\r\n            ISNULL([IS_Deleted], 0) AS [IS_Deleted],\r\n            ISNULL([maxdepit], 0) AS [maxdepit],\r\n            ISNULL([type], 0) AS [type],\r\n            ISNULL([tax_no], '') AS [tax_no],\r\n            ISNULL([AccountCode], '') AS [AccountCode],\r\n            ISNULL([IdScan], '') AS [IdScan],\r\n            ISNULL([Branch], '') AS [Branch],\r\n            ISNULL([ISCredit], 0) AS [ISCredit],\r\n            ISNULL([PlotIdentification], '') AS [PlotIdentification],\r\n            ISNULL([BuildingNumber], '') AS [BuildingNumber],\r\n            ISNULL([StreetName], '') AS [StreetName],\r\n            ISNULL([AdditionalStreetName], '') AS [AdditionalStreetName],\r\n            ISNULL([District], '') AS [District],\r\n            ISNULL([PostalZone], '') AS [PostalZone],\r\n            ISNULL([CrNo], '') AS [CrNo],\r\n            ISNULL([Pricing], 0) AS [Pricing]\r\n        FROM Customers " + Cond + ";\r\n    ";
				List<ListCustomers> list;
				list = new List<ListCustomers>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListCustomers listCustomers;
					listCustomers = new ListCustomers();
					listCustomers.Id = Conversions.ToInteger(sqlDataReader["id"]);
					listCustomers.Name = sqlDataReader["name"].ToString();
					listCustomers.Country = sqlDataReader["country"].ToString();
					listCustomers.City = sqlDataReader["city"].ToString();
					listCustomers.Area = sqlDataReader["area"].ToString();
					listCustomers.Act = Conversions.ToInteger(sqlDataReader["act"]);
					listCustomers.NationalId = sqlDataReader["national_id"].ToString();
					listCustomers.Tel = sqlDataReader["tel"].ToString();
					listCustomers.Mobile = sqlDataReader["mobile"].ToString();
					listCustomers.Fax = sqlDataReader["fax"].ToString();
					listCustomers.Email = sqlDataReader["email"].ToString();
					listCustomers.Notes = sqlDataReader["notes"].ToString();
					listCustomers.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IS_Deleted"]));
					listCustomers.MaxDepit = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["maxdepit"]));
					listCustomers.Type = Conversions.ToInteger(sqlDataReader["type"]);
					listCustomers.TaxNo = sqlDataReader["tax_no"].ToString();
					listCustomers.AccountCode = sqlDataReader["AccountCode"].ToString();
					listCustomers.Branch = sqlDataReader["Branch"].ToString();
					listCustomers.IsCredit = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["ISCredit"]));
					listCustomers.PlotIdentification = sqlDataReader["PlotIdentification"].ToString();
					listCustomers.BuildingNumber = sqlDataReader["BuildingNumber"].ToString();
					listCustomers.StreetName = sqlDataReader["StreetName"].ToString();
					listCustomers.AdditionalStreetName = sqlDataReader["AdditionalStreetName"].ToString();
					listCustomers.District = sqlDataReader["District"].ToString();
					listCustomers.PostalZone = sqlDataReader["PostalZone"].ToString();
					listCustomers.CrNo = sqlDataReader["CrNo"].ToString();
					listCustomers.Pricing = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Pricing"]));
					byte[] array;
					array = (byte[])sqlDataReader["IdScan"];
					if (array != null)
					{
						idScan = global::SmartAuditERP.SendData.ConvertImageToBase64(array);
						global::SmartAuditERP.SendData.GetImageSize(array);
					}
					listCustomers.IdScan = idScan;
					list.Add(listCustomers);
				}
				sqlDataReader.Close();
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string ConvertToBase64(byte[] imageData)
		{
			return Convert.ToBase64String(imageData);
		}

		public static long GetImageSize(byte[] imageData)
		{
			return imageData.Length;
		}

		public static string ConvertImageToBase64(byte[] imageData)
		{
			return Convert.ToBase64String(imageData);
		}

		public static string ImageToBase64(string imagePath)
		{
			return Convert.ToBase64String(File.ReadAllBytes(imagePath));
		}

		public static string GetItemsCategory(string Cond)
		{
			string result = default(string);
			try
			{
				string image;
				image = string.Empty;
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\n        SELECT \r\n            ISNULL([CategoryId], 0) AS [CategoryId],\r\n            ISNULL([Code], '') AS [Code],\r\n            ISNULL([ParentCode], '') AS [ParentCode],\r\n            ISNULL([type], 0) AS [type],\r\n            ISNULL([name], '') AS [name],\r\n            ISNULL([nameEN], '') AS [nameEN],\r\n            ISNULL([color], '') AS [color],\r\n            ISNULL([printer], '') AS [printer],\r\n            ISNULL([image], 0x0) AS [image],\r\n            ISNULL([ShowInPOS], 0) AS [ShowInPOS],\r\n            ISNULL([DispalyOrder], 0) AS [DispalyOrder],\r\n            ISNULL([IS_Deleted], 0) AS [IS_Deleted],\r\n            ISNULL([PrintAllItems], 0) AS [PrintAllItems],\r\n            ISNULL([PrintItemsSeparately], 0) AS [PrintItemsSeparately],\r\n            ISNULL([BranchId], 0) AS [BranchId],\r\n            ISNULL([AllBranch], 0) AS [AllBranch],\r\n            ISNULL([Id], 0) AS [Id]\r\n        FROM ItemsCategory " + Cond + " ;";
				List<ListItemsCategory> list;
				list = new List<ListItemsCategory>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListItemsCategory listItemsCategory;
					listItemsCategory = new ListItemsCategory();
					listItemsCategory.CategoryId = Conversions.ToInteger(sqlDataReader["CategoryId"]);
					listItemsCategory.Code = sqlDataReader["Code"].ToString();
					listItemsCategory.ParentCode = sqlDataReader["ParentCode"].ToString();
					listItemsCategory.Type = Conversions.ToInteger(sqlDataReader["type"]);
					listItemsCategory.Name = sqlDataReader["name"].ToString();
					listItemsCategory.NameEN = sqlDataReader["nameEN"].ToString();
					listItemsCategory.Color = sqlDataReader["color"].ToString();
					listItemsCategory.Printer = sqlDataReader["printer"].ToString();
					listItemsCategory.ShowInPOS = Conversions.ToBoolean(sqlDataReader["ShowInPOS"]);
					listItemsCategory.DisplayOrder = Conversions.ToInteger(sqlDataReader["DispalyOrder"]);
					listItemsCategory.IS_Deleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]);
					listItemsCategory.PrintAllItems = Conversions.ToBoolean(sqlDataReader["PrintAllItems"]);
					listItemsCategory.PrintItemsSeparately = Conversions.ToBoolean(sqlDataReader["PrintItemsSeparately"]);
					listItemsCategory.BranchId = Conversions.ToInteger(sqlDataReader["BranchId"]);
					listItemsCategory.AllBranch = Conversions.ToBoolean(sqlDataReader["AllBranch"]);
					listItemsCategory.Id = Conversions.ToInteger(sqlDataReader["Id"]);
					byte[] array;
					array = (byte[])sqlDataReader["Image"];
					if (array != null)
					{
						image = global::SmartAuditERP.SendData.ConvertImageToBase64(array);
						global::SmartAuditERP.SendData.GetImageSize(array);
					}
					listItemsCategory.Image = image;
					list.Add(listItemsCategory);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetItemBarcode(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\n        SELECT \r\n            ISNULL([ItemId], 0) AS [ItemId],\r\n            ISNULL([Barcode], '') AS [Barcode]\r\nfrom Itembarcodes " + Cond + ";";
				List<listItemBarcode> list;
				list = new List<listItemBarcode>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					listItemBarcode listItemBarcode2;
					listItemBarcode2 = new listItemBarcode();
					listItemBarcode2.ItemId = Conversions.ToInteger(sqlDataReader["ItemId"]);
					listItemBarcode2.Barcode = sqlDataReader["Barcode"].ToString();
					list.Add(listItemBarcode2);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetUnit(string Cond)
		{
			string result = default(string);
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\n        SELECT \r\n    ISNULL([name], '') AS [name],\r\n    ISNULL([defaultInv], 0) AS [defaultInv],\r\n    ISNULL([IS_Deleted], 0) AS [IS_Deleted],\r\n    ISNULL([UnitId], 0) AS [UnitId],\r\n    ISNULL([Id], 0) AS [Id],\r\n    ISNULL([UnitCode], '') AS [UnitCode]\r\nFROM units " + Cond + ";\r\n";
				List<ListUnit> list;
				list = new List<ListUnit>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListUnit listUnit;
					listUnit = new ListUnit();
					listUnit.Name = sqlDataReader["name"].ToString();
					listUnit.DefaultInv = Conversions.ToInteger(sqlDataReader["defaultInv"]);
					listUnit.IS_Deleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]);
					listUnit.UnitId = Conversions.ToInteger(sqlDataReader["UnitId"]);
					listUnit.Id = Conversions.ToInteger(sqlDataReader["Id"]);
					listUnit.UnitCode = Conversions.ToString(sqlDataReader["UnitCode"]);
					list.Add(listUnit);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetAccountCust(string Cond)
		{
			string result;
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\nSELECT ISNULL(A.[Code], '') AS Code,\r\n               ISNULL(A.[AName], '') AS AName,\r\n               ISNULL(CAST(A.[Nature] AS NVARCHAR(50)), '0') AS Nature,  -- التأكد من تحويل Nature إلى NVARCHAR\r\n               ISNULL(A.[Type], '') AS AccountType,\r\n               ISNULL(A.[ParentCode], '') AS ParentCode,\r\n               ISNULL(A.[Date], GETDATE()) AS [Date],\r\n               ISNULL(A.[UserName], '') AS UserName,\r\n               ISNULL(A.[IValue], 0) AS IValue,\r\n               ISNULL(A.[FinalAcc], 0) AS FinalAcc,\r\n               ISNULL(A.[Total_Debts], 0) AS Total_Debts,\r\n               ISNULL(A.[Total_Credits], 0) AS Total_Credits,\r\n               ISNULL(A.[Account_Value], 0) AS Account_Value,\r\n               ISNULL(A.[Acc_branch], '') AS Acc_branch,\r\n               ISNULL(A.[CostCenter], '') AS CostCenter,\r\n               ISNULL(A.[IsDeleted], 0) AS IsDeleted\r\n        FROM Accounts_Index A\r\n        LEFT JOIN Customers C ON A.Code = C.AccountCode\r\n        WHERE C.AccountCode = A.Code " + Cond + ";\r\n        ";
				List<ListAccounCust> list;
				list = new List<ListAccounCust>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListAccounCust listAccounCust;
					listAccounCust = new ListAccounCust();
					listAccounCust.Code = sqlDataReader["Code"].ToString();
					listAccounCust.AName = sqlDataReader["AName"].ToString();
					listAccounCust.Nature = Conversions.ToInteger(Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["Nature"])) ? ((object)0) : sqlDataReader["Nature"]);
					listAccounCust.AccountType = Conversions.ToInteger(sqlDataReader["AccountType"].ToString());
					listAccounCust.ParentCode = Conversions.ToInteger(sqlDataReader["ParentCode"].ToString());
					listAccounCust.AccDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["Date"]));
					listAccounCust.UserName = sqlDataReader["UserName"].ToString();
					listAccounCust.IValue = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["IValue"]));
					listAccounCust.Total_Debts = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Debts"]));
					listAccounCust.Total_Credits = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Credits"]));
					listAccounCust.Account_Value = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Account_Value"]));
					listAccounCust.Acc_branch = Conversions.ToInteger(sqlDataReader["Acc_branch"].ToString());
					listAccounCust.CostCenter = Conversions.ToInteger(sqlDataReader["CostCenter"].ToString());
					listAccounCust.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IsDeleted"]));
					listAccounCust.FinalAcc = Convert.ToDouble(Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["FinalAcc"])));
					list.Add(listAccounCust);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetAccountEmp(string Cond)
		{
			string result;
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "\r\nSELECT ISNULL(A.[Code], '') AS Code,\r\n               ISNULL(A.[AName], '') AS AName,\r\n               ISNULL(CAST(A.[Nature] AS NVARCHAR(50)), '0') AS Nature,  -- التأكد من تحويل Nature إلى NVARCHAR\r\n               ISNULL(A.[Type], '') AS AccountType,\r\n               ISNULL(A.[ParentCode], '') AS ParentCode,\r\n               ISNULL(A.[Date], GETDATE()) AS [Date],\r\n               ISNULL(A.[UserName], '') AS UserName,\r\n               ISNULL(A.[IValue], 0) AS IValue,\r\n               ISNULL(A.[FinalAcc], 0) AS FinalAcc,\r\n               ISNULL(A.[Total_Debts], 0) AS Total_Debts,\r\n               ISNULL(A.[Total_Credits], 0) AS Total_Credits,\r\n               ISNULL(A.[Account_Value], 0) AS Account_Value,\r\n               ISNULL(A.[Acc_branch], '') AS Acc_branch,\r\n               ISNULL(A.[CostCenter], '') AS CostCenter,\r\n               ISNULL(A.[IsDeleted], 0) AS IsDeleted\r\n        FROM Accounts_Index A\r\n        LEFT JOIN Employees E ON A.Code = E.AccCode\r\n        WHERE E.AccCode = A.Code " + Cond + ";\r\n        ";
				List<ListAccounCust> list;
				list = new List<ListAccounCust>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListAccounCust listAccounCust;
					listAccounCust = new ListAccounCust();
					listAccounCust.Code = sqlDataReader["Code"].ToString();
					listAccounCust.AName = sqlDataReader["AName"].ToString();
					listAccounCust.Nature = Conversions.ToInteger(Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["Nature"])) ? ((object)0) : sqlDataReader["Nature"]);
					listAccounCust.AccountType = Conversions.ToInteger(sqlDataReader["AccountType"].ToString());
					listAccounCust.ParentCode = Conversions.ToInteger(sqlDataReader["ParentCode"].ToString());
					listAccounCust.AccDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["Date"]));
					listAccounCust.UserName = sqlDataReader["UserName"].ToString();
					listAccounCust.IValue = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["IValue"]));
					listAccounCust.Total_Debts = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Debts"]));
					listAccounCust.Total_Credits = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Credits"]));
					listAccounCust.Account_Value = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Account_Value"]));
					listAccounCust.Acc_branch = Conversions.ToInteger(sqlDataReader["Acc_branch"].ToString());
					listAccounCust.CostCenter = Conversions.ToInteger(sqlDataReader["CostCenter"].ToString());
					listAccounCust.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IsDeleted"]));
					listAccounCust.FinalAcc = Convert.ToDouble(Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["FinalAcc"])));
					list.Add(listAccounCust);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetAccountIndex(string Cond)
		{
			string result;
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "SELECT\r\n    ISNULL([Code], 0) AS [Code],\r\n    ISNULL([AName], '') AS [AName],\r\n    ISNULL([Nature], 0) AS [Nature],\r\n    ISNULL([Type], 0) AS [Type],\r\n    ISNULL([ParentCode], 0) AS [ParentCode],\r\n    ISNULL([Date], GETDATE()) AS [Date], -- تعويض التاريخ الحالي إذا كانت القيمة NULL\r\n    ISNULL([UserName], '') AS [UserName],\r\n    ISNULL([IValue], 0) AS [IValue],\r\n    ISNULL([FinalAcc], 0) AS [FinalAcc],\r\n    ISNULL([Total_Debts], 0) AS [Total_Debts],\r\n    ISNULL([Total_Credits], 0) AS [Total_Credits],\r\n    ISNULL([Account_Value], 0) AS [Account_Value],\r\n    ISNULL([Acc_branch], '') AS [Acc_branch],\r\n    ISNULL([CostCenter], 0) AS [CostCenter],\r\n    ISNULL([IsDeleted], 0) AS [IsDeleted]\r\nFROM Accounts_Index " + Cond + ";";
				List<ListAccountIndex> list;
				list = new List<ListAccountIndex>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListAccountIndex listAccountIndex;
					listAccountIndex = new ListAccountIndex();
					listAccountIndex.Code = sqlDataReader["Code"].ToString();
					listAccountIndex.AName = sqlDataReader["AName"].ToString();
					listAccountIndex.Nature = Conversions.ToInteger(Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["Nature"])) ? ((object)0) : sqlDataReader["Nature"]);
					listAccountIndex.AccountType = Conversions.ToInteger(sqlDataReader["Type"].ToString());
					listAccountIndex.ParentCode = sqlDataReader["ParentCode"].ToString();
					listAccountIndex.AccDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["Date"]));
					listAccountIndex.UserName = sqlDataReader["UserName"].ToString();
					listAccountIndex.IValue = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["IValue"]));
					listAccountIndex.FinalAcc = Conversions.ToInteger(sqlDataReader["FinalAcc"]);
					listAccountIndex.Total_Debts = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Debts"]));
					listAccountIndex.Total_Credits = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Total_Credits"]));
					listAccountIndex.Account_Value = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Account_Value"]));
					listAccountIndex.Acc_branch = Conversions.ToInteger(sqlDataReader["Acc_branch"].ToString());
					listAccountIndex.CostCenter = Conversions.ToInteger(sqlDataReader["CostCenter"].ToString());
					listAccountIndex.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IsDeleted"]));
					listAccountIndex.FinalAcc = Convert.ToDouble(Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["FinalAcc"])));
					list.Add(listAccountIndex);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		private static void SendItemDataByid(int Item_ID)
		{
			string cond;
			cond = ("where id=" + Conversions.ToString(Item_ID)) ?? "";
			string cond2;
			cond2 = "where Itemid=" + Conversions.ToString(Item_ID) + " ";
			string cond3;
			cond3 = "";
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Items", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItems(cond)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemsUnit", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemsUnit(cond2)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemPrices", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemPrices(cond2)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemComponents", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemComponents(cond2)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemSerialNo", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemSerialNo(cond2)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemBarcode", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemBarcode(cond2)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Units", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetUnit(cond3)), 0, retain: true);
			ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "ItemsCategory", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetItemsCategory(cond3)), 0, retain: true);
		}

		public static void SendStoredItems()
		{
			try
			{
				int item_ID;
				item_ID = 0;
				if (!ConnectBroker.IsConnectedToInternet())
				{
					return;
				}
				string text;
				text = Application.StartupPath + "\\Data\\";
				string path;
				path = text + "Itemid.json";
				if (File.Exists(path))
				{
					if (string.IsNullOrEmpty(item_ID.ToString()))
					{
						return;
					}
					string value;
					value = File.ReadAllText(path);
					if (!string.IsNullOrWhiteSpace(value))
					{
						foreach (ListItems item in JsonConvert.DeserializeObject<List<ListItems>>(value))
						{
							item_ID = item.id;
							global::SmartAuditERP.SendData.SendItemDataByid(item_ID);
						}
					}
					File.WriteAllText(path, string.Empty);
				}
				path = text + "InvGlobalID.json";
				if (File.Exists(path))
				{
					if (string.IsNullOrEmpty(item_ID.ToString()))
					{
						return;
					}
					string value2;
					value2 = File.ReadAllText(path);
					if (!string.IsNullOrWhiteSpace(value2))
					{
						foreach (ListInvoice item2 in JsonConvert.DeserializeObject<List<ListInvoice>>(value2))
						{
							string invGlobalID;
							invGlobalID = item2.InvGlobalID;
							if (ConnectBroker.CheckConnectionAndBroker())
							{
								string cond;
								cond = "where InvGlobalID=N'" + invGlobalID + "'";
								ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Inv", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetInv(cond)), 0, retain: true);
							}
						}
						foreach (listInvSub item3 in JsonConvert.DeserializeObject<List<listInvSub>>(value2))
						{
							string invGlobalID2;
							invGlobalID2 = "WHERE TRY_CAST(InvGlobalID AS nvarchar) IN (\r\n                SELECT InvGlobalID FROM inv \r\n                WHERE InvGlobalID=N'" + item3.InvGlobalID + "')";
							if (ConnectBroker.CheckConnectionAndBroker())
							{
								ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "InvSub", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetInvSub(invGlobalID2)), 0, retain: true);
							}
						}
						foreach (ListEntry item4 in JsonConvert.DeserializeObject<List<ListEntry>>(value2))
						{
							string cond2;
							cond2 = "where GlobalID=N'" + item4.GlobalID + "'";
							if (ConnectBroker.CheckConnectionAndBroker())
							{
								ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "Entry", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetEntryData(cond2)), 0, retain: true);
							}
						}
						foreach (ListEntrySub item5 in JsonConvert.DeserializeObject<List<ListEntrySub>>(value2))
						{
							string cond3;
							cond3 = "where EntryGlobalID=N'" + item5.EntryGlobalID + "'";
							if (ConnectBroker.CheckConnectionAndBroker())
							{
								ConnectBroker.mqttClient.Publish(ConnectBroker.ClientCode + "EntryGlobalID", Encoding.UTF8.GetBytes(global::SmartAuditERP.SendData.GetEntrySubData(cond3)), 0, retain: true);
							}
						}
					}
					File.WriteAllText(path, string.Empty);
				}
				ReceivedData.LoadReceived(global::SmartAuditERP.SendData.connString);
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static string GeteEmployee(string Cond)
		{
			string result;
			try
			{
				global::SmartAuditERP.SendData.conn = new SqlConnection(global::SmartAuditERP.SendData.connString);
				string cmdText;
				cmdText = "SELECT  \r\n      ISNULL([id], 0) AS [id],\r\n      ISNULL([name], '') AS [name],\r\n      ISNULL([manag], '') AS [manag],\r\n      ISNULL([dep], '') AS [dep],\r\n      ISNULL([state], '') AS [state],\r\n      ISNULL([job], '') AS [job],\r\n      ISNULL([branch], '') AS [branch],\r\n      ISNULL([birth_date], '1990-01-01') AS [birth_date],\r\n      ISNULL([insurance_no], '') AS [insurance_no],\r\n      ISNULL([work_date], '1990-01-01') AS [work_date],\r\n      ISNULL([marital_state], '') AS [marital_state],\r\n      ISNULL([nationality], '') AS [nationality],\r\n      ISNULL([sex], '') AS [sex],\r\n      ISNULL([tel], '') AS [tel],\r\n      ISNULL([mobile], '') AS [mobile],\r\n      ISNULL([email], '') AS [email],\r\n      ISNULL([address], '') AS [address],\r\n      ISNULL([notes], '') AS [notes],\r\n      ISNULL([image], '') AS [image], \r\n      ISNULL([salary_basic], 0) AS [salary_basic],\r\n      ISNULL([salary_add], 0) AS [salary_add],\r\n      ISNULL([salary_other], 0) AS [salary_other],\r\n      ISNULL([house], 0) AS [house],\r\n      ISNULL([food], 0) AS [food],\r\n      ISNULL([travel], 0) AS [travel],\r\n      ISNULL([medical], 0) AS [medical],\r\n      ISNULL([IS_Deleted], 0) AS [IS_Deleted],\r\n      ISNULL([AccCode], '') AS [AccCode],\r\n      ISNULL([CardNo], '') AS [CardNo],\r\n      ISNULL([BankNo], '') AS [BankNo],\r\n      ISNULL([BankName], '') AS [BankName]\r\nFROM Employees " + Cond + ";";
				List<ListEmployee> list;
				list = new List<ListEmployee>();
				if (global::SmartAuditERP.SendData.conn.State != ConnectionState.Open)
				{
					global::SmartAuditERP.SendData.conn.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = new SqlCommand(cmdText, global::SmartAuditERP.SendData.conn).ExecuteReader();
				while (sqlDataReader.Read())
				{
					ListEmployee listEmployee;
					listEmployee = new ListEmployee();
					listEmployee.Id = Conversions.ToInteger(sqlDataReader["id"]);
					listEmployee.Name = sqlDataReader["name"].ToString();
					listEmployee.Manag = sqlDataReader["manag"].ToString();
					listEmployee.Dep = sqlDataReader["dep"].ToString();
					listEmployee.State = sqlDataReader["state"].ToString();
					listEmployee.Job = sqlDataReader["job"].ToString();
					listEmployee.Branch = sqlDataReader["branch"].ToString();
					listEmployee.BirthDate = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["birth_date"])) ? DateTime.MinValue : Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["birth_date"])));
					listEmployee.InsuranceNo = sqlDataReader["insurance_no"].ToString();
					listEmployee.WorkDate = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["work_date"])) ? DateTime.MinValue : Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["work_date"])));
					listEmployee.MaritalState = sqlDataReader["marital_state"].ToString();
					listEmployee.Nationality = sqlDataReader["nationality"].ToString();
					listEmployee.Sex = sqlDataReader["sex"].ToString();
					listEmployee.Tel = sqlDataReader["tel"].ToString();
					listEmployee.Mobile = sqlDataReader["mobile"].ToString();
					listEmployee.Email = sqlDataReader["email"].ToString();
					listEmployee.Address = sqlDataReader["address"].ToString();
					listEmployee.Notes = sqlDataReader["notes"].ToString();
					listEmployee.SalaryBasic = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_basic"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_basic"])));
					listEmployee.SalaryAdd = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_add"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_add"])));
					listEmployee.SalaryOther = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_other"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["salary_other"])));
					listEmployee.House = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["house"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["house"])));
					listEmployee.Food = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["food"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["food"])));
					listEmployee.Travel = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["travel"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["travel"])));
					listEmployee.Medical = (Information.IsDBNull(RuntimeHelpers.GetObjectValue(sqlDataReader["medical"])) ? 0m : Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["medical"])));
					listEmployee.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IS_Deleted"]));
					listEmployee.AccCode = sqlDataReader["AccCode"].ToString();
					listEmployee.CardNo = sqlDataReader["CardNo"].ToString();
					listEmployee.BankNo = sqlDataReader["BankNo"].ToString();
					listEmployee.BankName = sqlDataReader["BankName"].ToString();
					list.Add(listEmployee);
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine(ex.Message);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetEmpBranch(string cond)
		{
			string result = default(string);
			try
			{
				List<ListEmpBranch> list;
				list = new List<ListEmpBranch>();
				string cmdText;
				cmdText = ("\r\n    SELECT ISNULL([emp],0) as Emp\r\n      ,ISNULL([branch],1)as Branch\r\n  FROM EmpBranches" + cond) ?? "";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListEmpBranch listEmpBranch;
						listEmpBranch = new ListEmpBranch();
						listEmpBranch.Emp = Conversions.ToInteger(sqlDataReader["Emp"].ToString());
						listEmpBranch.Branch = Conversions.ToInteger(sqlDataReader["Branch"].ToString());
						list.Add(listEmpBranch);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetUsers(string cond)
		{
			string result = default(string);
			try
			{
				List<ListUsers> list;
				list = new List<ListUsers>();
				string cmdText;
				cmdText = ("\r\n    SELECT \r\n        ISNULL([id], 0) AS [id],\r\n        ISNULL([emp], '') AS [emp],\r\n        ISNULL([username], '') AS [username],\r\n        ISNULL([pwd], '') AS [pwd],\r\n        ISNULL([IS_Deleted], 0) AS [IS_Deleted],\r\n        ISNULL([SecureCode], '') AS [SecureCode],\r\n        ISNULL([LoginSecode], '') AS [LoginSecode]\r\n    FROM \r\n        Users " + cond) ?? "";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListUsers listUsers;
						listUsers = new ListUsers();
						listUsers.Id = Conversions.ToInteger(sqlDataReader["id"]);
						listUsers.Emp = sqlDataReader["emp"].ToString();
						listUsers.Username = sqlDataReader["username"].ToString();
						listUsers.Pwd = sqlDataReader["pwd"].ToString();
						listUsers.IsDeleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IS_Deleted"]));
						listUsers.SecureCode = sqlDataReader["SecureCode"].ToString();
						listUsers.LoginSecode = sqlDataReader["LoginSecode"].ToString();
						list.Add(listUsers);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetUserPermissions(string cond)
		{
			string result = default(string);
			try
			{
				List<ListUserPermission> list;
				list = new List<ListUserPermission>();
				string cmdText;
				cmdText = ("\r\n    SELECT \r\n        ISNULL([user_id], 0) AS [user_id],\r\n        ISNULL([Form_id], 0) AS [Form_id],\r\n        ISNULL([IS_New], 0) AS [IS_New],\r\n        ISNULL([IS_Save], 0) AS [IS_Save],\r\n        ISNULL([IS_Delete], 0) AS [IS_Delete],\r\n        ISNULL([IS_Search], 0) AS [IS_Search],\r\n        ISNULL([IS_Print], 0) AS [IS_Print],\r\n        ISNULL([IS_Edit], 0) AS [IS_Edit]\r\n    FROM \r\n        User_Permissions " + cond) ?? "";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListUserPermission listUserPermission;
						listUserPermission = new ListUserPermission();
						listUserPermission.UserId = Conversions.ToInteger(sqlDataReader["user_id"]);
						listUserPermission.FormId = Conversions.ToInteger(sqlDataReader["Form_id"]);
						listUserPermission.IsNew = Conversions.ToBoolean(sqlDataReader["IS_New"]);
						listUserPermission.IsSave = Conversions.ToBoolean(sqlDataReader["IS_Save"]);
						listUserPermission.IsDelete = Conversions.ToBoolean(sqlDataReader["IS_Delete"]);
						listUserPermission.IsSearch = Conversions.ToBoolean(sqlDataReader["IS_Search"]);
						listUserPermission.IsPrint = Conversions.ToBoolean(sqlDataReader["IS_Print"]);
						listUserPermission.IsEdit = Conversions.ToBoolean(sqlDataReader["IS_Edit"]);
						list.Add(listUserPermission);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetOperationPermission(string cond)
		{
			string result = default(string);
			try
			{
				List<ListOperationPermission> list;
				list = new List<ListOperationPermission>();
				string cmdText;
				cmdText = ("\r\n    SELECT Isnull([id],0)as id\r\n      ,Isnull([emp],0) as emp\r\n      ,Isnull([OperNo],0) as OperNo\r\n      ,Isnull([pwd],0) as pwd\r\n      ,Isnull([lastChanged],0) as lastChanged\r\n      ,Isnull([IS_Deleted],0) as IS_Deleted\r\n      ,Isnull([OperVal],0) as OperVal\r\n      ,Isnull([Activated],0) as Activated\r\n  FROM  [OperationPermission] " + cond) ?? "";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListOperationPermission listOperationPermission;
						listOperationPermission = new ListOperationPermission();
						listOperationPermission.id = Conversions.ToInteger(sqlDataReader["id"]);
						listOperationPermission.emp = Conversions.ToInteger(sqlDataReader["emp"]);
						listOperationPermission.OperNo = Conversions.ToInteger(sqlDataReader["OperNo"]);
						listOperationPermission.Pwd = Conversions.ToInteger(sqlDataReader["Pwd"]);
						listOperationPermission.lastChanged = Conversions.ToDate(sqlDataReader["lastChanged"]);
						listOperationPermission.IS_Deleted = Conversions.ToInteger(sqlDataReader["IS_Deleted"]);
						listOperationPermission.OperVal = Conversions.ToInteger(sqlDataReader["OperVal"]);
						listOperationPermission.Activated = Conversions.ToInteger(sqlDataReader["Activated"]);
						list.Add(listOperationPermission);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetInv(string cond)
		{
			string result = default(string);
			try
			{
				List<ListInvoice> list;
				list = new List<ListInvoice>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = new SqlCommand(("\r\n        SELECT ISNULL([InvGlobalID], '') AS InvGlobalID, ISNULL([proc_id], 0) AS proc_id, ISNULL([proc_type], 0) AS proc_type, ISNULL([id], 0) AS id, \r\n               ISNULL([date], GETDATE()) AS [date], ISNULL([inv_type], 0) AS inv_type, ISNULL([OrderType], 0) AS OrderType, \r\n               ISNULL([safe], 0) AS safe, ISNULL([stock], 0) AS stock, ISNULL([cust_id], 0) AS cust_id, ISNULL([sales_emp], 0) AS sales_emp, \r\n               ISNULL([InvTotal], 0) AS InvTotal, ISNULL([tot_purch], 0) AS tot_purch, ISNULL([AdditionsTot], 0) AS AdditionsTot, \r\n               ISNULL([Insurance], 0) AS Insurance, ISNULL([tot_net], 0) AS tot_net, ISNULL([EntryID], '') AS EntryID, \r\n               ISNULL([purch_rest_id], 0) AS purch_rest_id, ISNULL([Reff_No], '') AS Reff_No, ISNULL([Reff_date], GETDATE()) AS Reff_date, \r\n               ISNULL([branch], 0) AS branch, ISNULL([IS_Buy], 0) AS IS_Buy, ISNULL([minus], 0) AS minus, ISNULL([paid], 0) AS paid, \r\n               ISNULL([salesman], 0) AS salesman, ISNULL([tax], 0) AS tax, ISNULL([ExtraVAT], 0) AS ExtraVAT, ISNULL([pay_type], 0) AS pay_type, \r\n               ISNULL([bank], 0) AS bank, ISNULL([cash], 0) AS cash, ISNULL([visa], 0) AS visa, ISNULL([notes], '') AS notes, \r\n               ISNULL([IS_Deleted], 0) AS IS_Deleted, ISNULL([Sync], 0) AS Sync, ISNULL([InvProfit], 0) AS InvProfit, \r\n               ISNULL([InvoiceStatus], 0) AS InvoiceStatus, ISNULL([AdditionalCost], 0) AS AdditionalCost, \r\n               ISNULL([PriceIncVAT], 0) AS PriceIncVAT, ISNULL([PaymentStatus], 0) AS PaymentStatus, ISNULL([IssueDate], GETDATE()) AS IssueDate, \r\n               ISNULL([InvCombinedId], 0) AS InvCombinedId, ISNULL([ItemsDiscount], 0) AS ItemsDiscount, ISNULL([InvCost], 0) AS InvCost, \r\n               ISNULL([FreeVATSales], 0) AS FreeVATSales, ISNULL([InvSum], 0) AS InvSum, ISNULL([VATPercent], 0) AS VATPercent, \r\n               ISNULL([CurrencyCode], '') AS CurrencyCode, ISNULL([CloudID], '') AS CloudID, ISNULL([CashCustomerName], '') AS CashCustomerName, \r\n               ISNULL([CashCustomerMobile], '') AS CashCustomerMobile, ISNULL([QRCode], '') AS QRCode, ISNULL([InvoiceHash], '') AS InvoiceHash, \r\n               ISNULL([UUID], '') AS UUID, ISNULL([ZatcaSent], 0) AS ZatcaSent, ISNULL([TotalWithholdingTax], 0) AS TotalWithholdingTax, \r\n               ISNULL([TableNo], '') AS TableNo, ISNULL([Balance_previews], 0) AS Balance_previews\r\n        FROM Inv " + cond) ?? "", sqlConnection).ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListInvoice listInvoice;
						listInvoice = new ListInvoice();
						ListInvoice listInvoice2;
						listInvoice2 = listInvoice;
						listInvoice2.InvGlobalID = sqlDataReader["InvGlobalID"].ToString();
						listInvoice2.ProcID = Conversions.ToInteger(sqlDataReader["proc_id"]);
						listInvoice2.ProcType = Conversions.ToInteger(sqlDataReader["proc_type"]);
						listInvoice2.ID = Conversions.ToInteger(sqlDataReader["id"]);
						listInvoice2.DateInv = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["date"]));
						listInvoice2.InvType = Conversions.ToInteger(sqlDataReader["inv_type"]);
						listInvoice2.OrderType = Conversions.ToInteger(sqlDataReader["OrderType"]);
						listInvoice2.Safe = Conversions.ToInteger(sqlDataReader["safe"]);
						listInvoice2.Stock = Conversions.ToInteger(sqlDataReader["stock"]);
						listInvoice2.CustID = Conversions.ToInteger(sqlDataReader["cust_id"]);
						listInvoice2.SalesEmp = Conversions.ToInteger(sqlDataReader["sales_emp"]);
						listInvoice2.InvTotal = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["InvTotal"]));
						listInvoice2.TotPurch = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["tot_purch"]));
						listInvoice2.AdditionsTot = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["AdditionsTot"]));
						listInvoice2.Insurance = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Insurance"]));
						listInvoice2.TotNet = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["tot_net"]));
						listInvoice2.EntryID = sqlDataReader["EntryID"].ToString();
						listInvoice2.PurchRestID = Conversions.ToInteger(sqlDataReader["purch_rest_id"]);
						listInvoice2.ReffNo = sqlDataReader["Reff_No"].ToString();
						listInvoice2.ReffDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["Reff_date"]));
						listInvoice2.Branch = Conversions.ToInteger(sqlDataReader["branch"]);
						listInvoice2.IS_Buy = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IS_Buy"]));
						listInvoice2.Minus = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["minus"]));
						listInvoice2.Paid = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["paid"]));
						listInvoice2.Salesman = Conversions.ToInteger(sqlDataReader["salesman"]);
						listInvoice2.Tax = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["tax"]));
						listInvoice2.ExtraVAT = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["ExtraVAT"]));
						listInvoice2.PayType = Conversions.ToInteger(sqlDataReader["pay_type"]);
						listInvoice2.Bank = Conversions.ToInteger(sqlDataReader["bank"]);
						listInvoice2.Cash = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["cash"]));
						listInvoice2.Visa = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["visa"]));
						listInvoice2.Notes = sqlDataReader["notes"].ToString();
						listInvoice2.IS_Deleted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IS_Deleted"]));
						listInvoice2.Sync = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["Sync"]));
						listInvoice2.InvProfit = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["InvProfit"]));
						listInvoice2.InvoiceStatus = Conversions.ToInteger(sqlDataReader["InvoiceStatus"]);
						listInvoice2.AdditionalCost = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["AdditionalCost"]));
						listInvoice2.PriceIncVAT = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["PriceIncVAT"]));
						listInvoice2.PaymentStatus = Conversions.ToInteger(sqlDataReader["PaymentStatus"]);
						listInvoice2.IssueDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["IssueDate"]));
						listInvoice2.InvCombinedID = sqlDataReader["InvCombinedId"].ToString();
						listInvoice2.ItemsDiscount = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["ItemsDiscount"]));
						listInvoice2.InvCost = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["InvCost"]));
						listInvoice2.FreeVATSales = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["FreeVATSales"]));
						listInvoice2.InvSum = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["InvSum"]));
						listInvoice2.VATPercent = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["VATPercent"]));
						listInvoice2.CurrencyCode = sqlDataReader["CurrencyCode"].ToString();
						listInvoice2.CloudID = sqlDataReader["CloudID"].ToString();
						listInvoice2.CashCustomerName = sqlDataReader["CashCustomerName"].ToString();
						listInvoice2.CashCustomerMobile = sqlDataReader["CashCustomerMobile"].ToString();
						listInvoice2.QRCode = sqlDataReader["QRCode"].ToString();
						listInvoice2.InvoiceHash = sqlDataReader["InvoiceHash"].ToString();
						listInvoice2.UUID = sqlDataReader["UUID"].ToString();
						listInvoice2.ZatcaSent = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["ZatcaSent"]));
						listInvoice2.TotalWithholdingTax = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["TotalWithholdingTax"]));
						listInvoice2.TableNo = sqlDataReader["TableNo"].ToString();
						listInvoice2.BalancePreviews = Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["Balance_previews"]));
						list.Add(listInvoice);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetInvSub(string InvGlobalID)
		{
			string result = default(string);
			try
			{
				List<listInvSub> list;
				list = new List<listInvSub>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("\r\n            SELECT \r\n                ISNULL([InvGlobalID], '') AS InvGlobalID,\r\n                ISNULL([id], 0) AS id,\r\n                ISNULL([proc_id], 0) AS proc_id,\r\n                ISNULL([proc_type], 0) AS proc_type,\r\n                ISNULL([store], 0) AS store,\r\n                ISNULL([ItemId], 0) AS ItemId,\r\n                ISNULL([unit], 0) AS unit,\r\n                ISNULL([UnitEquality], 0) AS UnitEquality,\r\n                ISNULL([val], 0) AS val,\r\n                ISNULL([val1], 0) AS val1,\r\n                ISNULL([exchange_price], 0) AS exchange_price,\r\n                ISNULL([expire_date], GETDATE()) AS expire_date,\r\n                ISNULL([taxperc], 0) AS taxperc,\r\n                ISNULL([taxval], 0) AS taxval,\r\n                ISNULL([discount], 0) AS discount,\r\n                ISNULL([notes], '') AS notes,\r\n                ISNULL([Description], '') AS Description,\r\n                ISNULL([ProductId], 0) AS ProductId,\r\n                ISNULL([AvrgCost], 0) AS AvrgCost,\r\n                ISNULL([CurrentQnty], 0) AS CurrentQnty,\r\n                ISNULL([ItemAddedCost], 0) AS ItemAddedCost,\r\n                ISNULL([ItemPriceWithoutVAT], 0) AS ItemPriceWithoutVAT,\r\n                ISNULL([WithholdingTax], 0) AS WithholdingTax,\r\n                ISNULL([WithholdingTaxPerc], 0) AS WithholdingTaxPerc,\r\n                ISNULL([ItemCostCenter], '') AS ItemCostCenter,\r\n                ISNULL([ItemAdditionalTax], 0) AS ItemAdditionalTax,\r\n                ISNULL([ItemAdditionalTaxPerc], 0) AS ItemAdditionalTaxPerc\r\n            FROM [dbo].[Inv_Sub] " + InvGlobalID + " ", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						listInvSub listInvSub2;
						listInvSub2 = new listInvSub();
						listInvSub listInvSub3;
						listInvSub3 = listInvSub2;
						listInvSub3.InvGlobalID = sqlDataReader["InvGlobalID"].ToString();
						listInvSub3.ID = Conversions.ToInteger(sqlDataReader["id"]);
						listInvSub3.ProcID = (int?)sqlDataReader["proc_id"];
						listInvSub3.ProcType = (int?)sqlDataReader["proc_type"];
						listInvSub3.Store = (int?)sqlDataReader["store"];
						listInvSub3.ItemId = (int?)sqlDataReader["ItemId"];
						listInvSub3.Unit = (int?)sqlDataReader["unit"];
						listInvSub3.UnitEquality = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["UnitEquality"]));
						listInvSub3.Val = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["val"]));
						listInvSub3.Val1 = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["val1"]));
						listInvSub3.ExchangePrice = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["exchange_price"]));
						listInvSub3.ExpireDate = Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["expire_date"]));
						listInvSub3.TaxPerc = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["taxperc"]));
						listInvSub3.TaxVal = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["taxval"]));
						listInvSub3.Discount = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["discount"]));
						listInvSub3.Notes = sqlDataReader["notes"].ToString();
						listInvSub3.Description = sqlDataReader["Description"].ToString();
						listInvSub3.ProductId = (int?)sqlDataReader["ProductId"];
						listInvSub3.AvrgCost = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["AvrgCost"]));
						listInvSub3.CurrentQnty = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["CurrentQnty"]));
						listInvSub3.ItemAddedCost = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["ItemAddedCost"]));
						listInvSub3.ItemPriceWithoutVAT = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["ItemPriceWithoutVAT"]));
						listInvSub3.WithholdingTax = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["WithholdingTax"]));
						listInvSub3.WithholdingTaxPerc = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["WithholdingTaxPerc"]));
						listInvSub3.ItemCostCenter = sqlDataReader["ItemCostCenter"].ToString();
						listInvSub3.ItemAdditionalTax = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["ItemAdditionalTax"]));
						listInvSub3.ItemAdditionalTaxPerc = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["ItemAdditionalTaxPerc"]));
						list.Add(listInvSub2);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetEntryData(string cond)
		{
			string result = default(string);
			try
			{
				List<ListEntry> list;
				list = new List<ListEntry>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand(("\r\n            SELECT \r\n                ISNULL([GlobalID], '') AS GlobalID,\r\n                ISNULL([id], 0) AS id,\r\n                ISNULL([date], GETDATE()) AS date,\r\n                ISNULL([doc_no], '') AS doc_no,\r\n                ISNULL([type], '') AS type,\r\n                ISNULL([state], 0) AS state,\r\n                ISNULL([notes], '') AS notes,\r\n                ISNULL([branch], 0) AS branch,\r\n                ISNULL([IS_Deleted], 0) AS IS_Deleted,\r\n                ISNULL([EmpID], 0) AS EmpID,\r\n                ISNULL([Sync], 0) AS Sync,\r\n                ISNULL([IsVAT], 0) AS IsVAT\r\n            FROM Entry " + cond) ?? "", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListEntry
						{
							GlobalID = sqlDataReader["GlobalID"].ToString(),
							ID = Conversions.ToInteger(sqlDataReader["id"]),
							EntryDate = Conversions.ToDate(sqlDataReader["date"]),
							DocNo = sqlDataReader["doc_no"].ToString(),
							Type = sqlDataReader["type"].ToString(),
							State = Conversions.ToInteger(sqlDataReader["state"]),
							Notes = sqlDataReader["notes"].ToString(),
							Branch = Conversions.ToInteger(sqlDataReader["branch"]),
							IS_Deleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]),
							EmpID = Conversions.ToInteger(sqlDataReader["EmpID"]),
							Sync = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["Sync"])),
							IsVAT = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["IsVAT"]))
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetEntrySubData(string cond)
		{
			string result = default(string);
			try
			{
				List<ListEntrySub> list;
				list = new List<ListEntrySub>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand(("\r\n            SELECT \r\n                ISNULL([EntryGlobalID], '') AS EntryGlobalID,\r\n                ISNULL([res_id], 0) AS res_id,\r\n                ISNULL([dept], 0) AS dept,\r\n                ISNULL([credit], 0) AS credit,\r\n                ISNULL([acc_no], '') AS acc_no,\r\n                ISNULL([CCcode], '') AS CCcode,\r\n                ISNULL([notes], '') AS notes,\r\n                ISNULL([branch], 0) AS branch\r\n            FROM [dbo].[Entry_sub] " + cond) ?? "", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListEntrySub
						{
							EntryGlobalID = sqlDataReader["EntryGlobalID"].ToString(),
							ResID = Conversions.ToInteger(sqlDataReader["res_id"]),
							Dept = Conversions.ToDecimal(sqlDataReader["dept"]),
							Credit = Conversions.ToDecimal(sqlDataReader["credit"]),
							AccNo = sqlDataReader["acc_no"].ToString(),
							CCCode = sqlDataReader["CCcode"].ToString(),
							Notes = sqlDataReader["notes"].ToString(),
							Branch = Conversions.ToInteger(sqlDataReader["branch"])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetOffers(string cond)
		{
			string result = default(string);
			try
			{
				List<ListOffers> list;
				list = new List<ListOffers>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand(("\r\n   SELECT\r\n      ISNULL([OfferID], 0) AS OfferID,\r\n      ISNULL([OfferType], '') AS OfferType,\r\n      ISNULL([OfferStartDate], '') AS OfferStartDate,\r\n      ISNULL([OfferExpire], '') AS OfferExpire,\r\n      ISNULL([OfferName], '') AS OfferName,\r\n      ISNULL([OfferDate], '') AS OfferDate,\r\n      ISNULL([OfferAccount], '') AS OfferAccount,\r\n      ISNULL([InvoiceID], 0) AS InvoiceID,\r\n      ISNULL([OfferValue], 0) AS OfferValue,\r\n      ISNULL([OfferPercentage], 0) AS OfferPercentage,\r\n      ISNULL([OfferValueTarget], 0) AS OfferValueTarget,\r\n      ISNULL([OfferQntyTarget], 0) AS OfferQntyTarget,\r\n      ISNULL([EmpId], 0) AS EmpId,\r\n      ISNULL([IsDeleted], 0) AS IsDeleted\r\n  FROM Offer\r\n" + cond) ?? "", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListOffers listOffers;
						listOffers = new ListOffers();
						listOffers.OfferID = Conversions.ToInteger(sqlDataReader["OfferID"]);
						listOffers.OfferType = (int?)sqlDataReader["OfferType"];
						listOffers.OfferStartDate = (DateTime?)sqlDataReader["OfferStartDate"];
						listOffers.OfferExpire = (DateTime?)sqlDataReader["OfferExpire"];
						listOffers.OfferName = sqlDataReader["OfferName"].ToString();
						listOffers.OfferDate = Conversions.ToDate(sqlDataReader["OfferDate"].ToString());
						listOffers.OfferAccount = sqlDataReader["OfferAccount"].ToString();
						listOffers.InvoiceID = Conversions.ToInteger(sqlDataReader["InvoiceID"].ToString());
						listOffers.OfferValue = Conversions.ToDouble(sqlDataReader["OfferValue"].ToString());
						listOffers.OfferPercentage = Conversions.ToDouble(sqlDataReader["OfferPercentage"].ToString());
						listOffers.OfferValueTarget = Conversions.ToDouble(sqlDataReader["OfferValueTarget"].ToString());
						listOffers.OfferQntyTarget = Conversions.ToDouble(sqlDataReader["OfferQntyTarget"].ToString());
						listOffers.EmpId = Conversions.ToInteger(sqlDataReader["EmpId"].ToString());
						listOffers.IsDeleted = (bool?)sqlDataReader["IsDeleted"];
						list.Add(listOffers);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetOfferForClientList()
		{
			string result = default(string);
			try
			{
				List<ListOfferForClient> list;
				list = new List<ListOfferForClient>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT \r\n    ISNULL([id], 0) AS id,\r\n    ISNULL([OfferId], 0) AS OfferId,\r\n    ISNULL([ClientID], 0) AS ClientID,\r\n    ISNULL([CatatogryID], 0) AS CatatogryID,\r\n    ISNULL([ItemID], 0) AS ItemID,\r\n    ISNULL([IsForAllItems], 0) AS IsForAllItems,\r\n    ISNULL([unit], '') AS unit,\r\n    ISNULL([OfferTargetQnty], 0.0) AS OfferTargetQnty,\r\n    ISNULL([OfferItemValue], 0.0) AS OfferItemValue,\r\n    ISNULL([OfferItemPercentage], 0.0) AS OfferItemPercentage\r\nFROM OfferForClient", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListOfferForClient listOfferForClient;
						listOfferForClient = new ListOfferForClient();
						listOfferForClient.Id = Conversions.ToInteger(sqlDataReader["id"]);
						listOfferForClient.OfferId = (int?)sqlDataReader["OfferId"];
						listOfferForClient.ClientID = Conversions.ToInteger(sqlDataReader["ClientID"].ToString());
						listOfferForClient.CatatogryID = Conversions.ToInteger(sqlDataReader["CatatogryID"].ToString());
						listOfferForClient.ItemID = Conversions.ToInteger(sqlDataReader["ItemID"].ToString());
						listOfferForClient.IsForAllItems = Conversions.ToBoolean(sqlDataReader["IsForAllItems"].ToString());
						listOfferForClient.Unit = sqlDataReader["unit"].ToString();
						listOfferForClient.OfferTargetQnty = Conversions.ToDouble(sqlDataReader["OfferTargetQnty"].ToString());
						listOfferForClient.OfferItemValue = Conversions.ToDouble(sqlDataReader["OfferItemValue"].ToString());
						listOfferForClient.OfferItemPercentage = Conversions.ToDouble(sqlDataReader["OfferItemPercentage"].ToString());
						list.Add(listOfferForClient);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetOfferItems()
		{
			string result = default(string);
			try
			{
				List<OfferItem> list;
				list = new List<OfferItem>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT \r\n    ISNULL([id], 0) AS id,\r\n    ISNULL([OfferId], 0) AS OfferId,\r\n    ISNULL([CatatogryID], 0) AS CatatogryID,\r\n    ISNULL([ItemID], 0) AS ItemID,\r\n    ISNULL([OfferNatural], 0) AS OfferNatural,\r\n    ISNULL([IsGroupedItems], 0) AS IsGroupedItems,\r\n    ISNULL([unit], '') AS unit,\r\n    ISNULL([OfferTargetQnty], 0.0) AS OfferTargetQnty,\r\n    ISNULL([OfferItemTargetQnty], 0.0) AS OfferItemTargetQnty,\r\n    ISNULL([OfferItemPrice], 0.0) AS OfferItemPrice,\r\n    ISNULL([OfferTotalPrice], 0.0) AS OfferTotalPrice,\r\n    ISNULL([OfferItemValue], 0.0) AS OfferItemValue,\r\n    ISNULL([OfferItemPercentage], 0.0) AS OfferItemPercentage,\r\n    ISNULL([OfferItemStock], 0.0) AS OfferItemStock\r\nFROM [dbo].[OfferItems]", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new OfferItem
						{
							id = sqlDataReader.GetInt32(Conversions.ToInteger("id")),
							OfferId = Conversions.ToInteger(sqlDataReader["OfferId"]),
							CatatogryID = Conversions.ToInteger(sqlDataReader["CatatogryID"].ToString()),
							ItemID = Conversions.ToInteger(sqlDataReader["ItemID"].ToString()),
							OfferNatural = Conversions.ToInteger(sqlDataReader["OfferNatural"].ToString()),
							IsGroupedItems = Conversions.ToBoolean(sqlDataReader["IsGroupedItems"].ToString()),
							unit = Conversions.ToInteger(sqlDataReader["unit"].ToString()),
							OfferTargetQnty = Conversions.ToSingle(sqlDataReader["OfferTargetQnty"].ToString()),
							OfferItemTargetQnty = Conversions.ToSingle(sqlDataReader["OfferItemTargetQnty"].ToString()),
							OfferItemPrice = Conversions.ToSingle(sqlDataReader["OfferItemPrice"].ToString()),
							OfferTotalPrice = Conversions.ToSingle(sqlDataReader["OfferTotalPrice"].ToString()),
							OfferItemValue = Conversions.ToSingle(sqlDataReader["OfferItemValue"].ToString()),
							OfferItemPercentage = Conversions.ToSingle(sqlDataReader["OfferItemPercentage"].ToString()),
							OfferItemStock = Conversions.ToSingle(sqlDataReader["OfferItemStock"].ToString())
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetReceipts(string cond)
		{
			string result = default(string);
			try
			{
				List<ListRecipt> list;
				list = new List<ListRecipt>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand(("SELECT \r\n            ISNULL([AutoID], 0) AS AutoID,\r\n            ISNULL([GlobalID], 0) AS GlobalID,\r\n            ISNULL([ReceiptNo], 0) AS ReceiptNo,\r\n            ISNULL([ReceiptDate], '1900-01-01') AS ReceiptDate,\r\n            ISNULL([EmpId], 0) AS EmpId,\r\n            ISNULL([ClientID], 0) AS ClientID,\r\n            ISNULL([CreditAcc], '') AS CreditAcc,\r\n            ISNULL([DebitAcc], '') AS DebitAcc,\r\n            ISNULL([Payment], 0.0) AS Payment,\r\n            ISNULL([VAT], 0.0) AS VAT,\r\n            ISNULL([NetVal], 0.0) AS NetVal,\r\n            ISNULL([ReceiptType], 0) AS ReceiptType,\r\n            ISNULL([PaymentType], 0) AS PaymentType,\r\n            ISNULL([BankId], 0) AS BankId,\r\n            ISNULL([TreasuryID], 0) AS TreasuryID,\r\n            ISNULL([SalesManID], 0) AS SalesManID,\r\n            ISNULL([CheckNo], '') AS CheckNo,\r\n            ISNULL([CheckDate], '1900-01-01') AS CheckDate,\r\n            ISNULL([Checkbank], '') AS Checkbank,\r\n            ISNULL([CheckState], 0) AS CheckState,\r\n            ISNULL([State], 0) AS State,\r\n            ISNULL([Notes], '') AS Notes,\r\n            ISNULL([EntryGlobalID], 0) AS EntryGlobalID,\r\n            ISNULL([BranchID], 0) AS BranchID,\r\n            ISNULL([ISDeleted], 0) AS ISDeleted,\r\n            ISNULL([Cccode], '') AS Cccode,\r\n            ISNULL([ReffNo], '') AS ReffNo,\r\n            ISNULL([Reffdate], '1900-01-01') AS Reffdate,\r\n            ISNULL([Sync], 0) AS Sync,\r\n            ISNULL([MobPOSID], 0) AS MobPOSID\r\n            FROM Receipts " + cond) ?? "", sqlConnection);
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						ListRecipt listRecipt;
						listRecipt = new ListRecipt();
						listRecipt.AutoID = Conversions.ToInteger(sqlDataReader[0]);
						listRecipt.GlobalID = Conversions.ToString(sqlDataReader[1]);
						listRecipt.ReceiptNo = Conversions.ToInteger(sqlDataReader[2]);
						listRecipt.ReceiptDate = (DateTime?)sqlDataReader[3];
						listRecipt.EmpId = (int?)sqlDataReader[4];
						listRecipt.ClientID = (int?)sqlDataReader[5];
						listRecipt.CreditAcc = Conversions.ToString(sqlDataReader[6]);
						listRecipt.DebitAcc = Conversions.ToString(sqlDataReader[7]);
						listRecipt.Payment = (double?)sqlDataReader[8];
						listRecipt.VAT = (double?)sqlDataReader[9];
						listRecipt.NetVal = (double?)sqlDataReader[10];
						listRecipt.ReceiptType = (int?)sqlDataReader[11];
						listRecipt.PaymentType = (int?)sqlDataReader[12];
						listRecipt.BankId = (int?)sqlDataReader[13];
						listRecipt.TreasuryID = (int?)sqlDataReader[14];
						listRecipt.SalesManID = (int?)sqlDataReader[15];
						listRecipt.CheckNo = Conversions.ToString(sqlDataReader[16]);
						listRecipt.CheckDate = (DateTime?)sqlDataReader[17];
						listRecipt.Checkbank = Conversions.ToString(sqlDataReader[18]);
						listRecipt.CheckState = (int?)sqlDataReader[19];
						listRecipt.State = (int?)sqlDataReader[20];
						listRecipt.Notes = sqlDataReader[21].ToString();
						listRecipt.EntryGlobalID = sqlDataReader[22].ToString();
						listRecipt.BranchID = Conversions.ToInteger(sqlDataReader.GetInt32(23).ToString());
						listRecipt.ISDeleted = (bool?)sqlDataReader[24];
						listRecipt.Cccode = Conversions.ToString(sqlDataReader[25]);
						listRecipt.ReffNo = Conversions.ToString(sqlDataReader[26]);
						listRecipt.Reffdate = (DateTime?)sqlDataReader[27];
						listRecipt.Sync = (bool?)sqlDataReader[28];
						listRecipt.MobPOSID = Conversions.ToString(sqlDataReader[29]);
						list.Add(listRecipt);
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetSafes()
		{
			string result = default(string);
			try
			{
				List<ListSafes> list;
				list = new List<ListSafes>();
				string cmdText;
				cmdText = "SELECT ISNULL([id], 0) AS id, ISNULL([name], '') AS name, ISNULL([branch], '') AS branch, ISNULL([status], 0) AS status, ISNULL([IS_Default], 0) AS IS_Default, ISNULL([notes], '') AS notes, ISNULL([IS_Deleted], 0) AS IS_Deleted FROM Safes";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListSafes
						{
							Id = Conversions.ToInteger(sqlDataReader[0]),
							Name = sqlDataReader[1].ToString(),
							Branch = Conversions.ToString(sqlDataReader[2]),
							Status = Conversions.ToInteger(sqlDataReader[3]),
							IsDefault = Conversions.ToInteger(sqlDataReader[4]),
							Notes = sqlDataReader[5].ToString(),
							IsDeleted = sqlDataReader.GetBoolean(6)
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetSafesEmp()
		{
			string result = default(string);
			try
			{
				List<ListSafeEmp> list;
				list = new List<ListSafeEmp>();
				string cmdText;
				cmdText = "SELECT ISNULL(safe_id, 0) AS safe_id, ISNULL(emp_id, 0)  FROM Safe_Emps";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListSafeEmp
						{
							safe_id = Conversions.ToInteger(sqlDataReader[0]),
							emp_id = Conversions.ToInteger(sqlDataReader[1])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetStocks()
		{
			string result = default(string);
			try
			{
				List<ListStock> list;
				list = new List<ListStock>();
				string cmdText;
				cmdText = "SELECT ISNULL([id], 0) AS id, ISNULL([name], '') AS name, ISNULL([Acc_Code], '') AS Acc_Code, ISNULL([branch], '') AS branch, ISNULL([status], 0) AS status, ISNULL([IS_Default], 0) AS IS_Default, ISNULL([notes], '') AS notes, ISNULL([IS_Deleted], 0) AS IS_Deleted FROM Stocks";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListStock
						{
							Id = Conversions.ToInteger(sqlDataReader["id"]),
							Name = sqlDataReader["name"].ToString(),
							Acc_Code = sqlDataReader["Acc_Code"].ToString(),
							Branch = Conversions.ToString(sqlDataReader["branch"]),
							Status = Conversions.ToInteger(sqlDataReader[4]),
							IsDefault = Conversions.ToInteger(sqlDataReader[5]),
							Notes = sqlDataReader[6].ToString(),
							IsDeleted = Conversions.ToBoolean(sqlDataReader[7])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetStockEmps()
		{
			string result = default(string);
			try
			{
				List<ListStockEmp> list;
				list = new List<ListStockEmp>();
				string cmdText;
				cmdText = "SELECT ISNULL([stock_id], 0) AS stock_id, ISNULL([emp_id], 0) AS emp_id FROM Stock_Emps";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListStockEmp
						{
							StockId = Conversions.ToInteger(sqlDataReader[0]),
							EmpId = Conversions.ToInteger(sqlDataReader[1])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetBranches()
		{
			string result = default(string);
			try
			{
				List<ListBranches> list;
				list = new List<ListBranches>();
				string cmdText;
				cmdText = "SELECT ISNULL([id], 0) AS id,isnull(BranchId,0)as BranchId ,ISNULL([name], '') AS name, ISNULL([Tel], '') AS Tel, ISNULL([Mobile], '') AS Mobile, ISNULL([Fax], '') AS Fax, ISNULL([Email], '') AS Email, ISNULL([Address], '') AS Address, ISNULL([notes], '') AS notes, ISNULL([IS_Deleted], 0) AS IS_Deleted, ISNULL([IsDefault], 0) AS IsDefault, ISNULL([Code], '') AS Code,  ISNULL([CustomersAcc], '') AS CustomersAcc, ISNULL([SupliersAcc], '') AS SupliersAcc, ISNULL([TreasuriesAcc], '') AS TreasuriesAcc, ISNULL([BanksAcc], '') AS BanksAcc, ISNULL([InventoryAcc], '') AS InventoryAcc, ISNULL([AppAcc], '') AS AppAcc, ISNULL([IsActive], 0) AS IsActive, ISNULL([CostCenter], '') AS CostCenter, ISNULL([EmployeeAcc], '') AS EmployeeAcc FROM Branches";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListBranches
						{
							Id = Conversions.ToInteger(sqlDataReader[0]),
							BranchId = Conversions.ToInteger(sqlDataReader[1]),
							Name = sqlDataReader[2].ToString(),
							Tel = sqlDataReader[3].ToString(),
							Mobile = Conversions.ToString(sqlDataReader[4]),
							Fax = Conversions.ToString(sqlDataReader[5]),
							Email = Conversions.ToString(sqlDataReader[6]),
							Address = sqlDataReader[7].ToString(),
							Notes = sqlDataReader[8].ToString(),
							IsDeleted = Conversions.ToBoolean(sqlDataReader[9]),
							IsDefault = Conversions.ToBoolean(sqlDataReader[10]),
							Code = Conversions.ToString(sqlDataReader[11]),
							CustomersAcc = sqlDataReader[12].ToString(),
							SupliersAcc = sqlDataReader[13].ToString(),
							TreasuriesAcc = Conversions.ToString(sqlDataReader[14]),
							BanksAcc = Conversions.ToString(sqlDataReader[15]),
							InventoryAcc = Conversions.ToString(sqlDataReader[16]),
							AppAcc = Conversions.ToString(sqlDataReader[17]),
							IsActive = Conversions.ToBoolean(sqlDataReader[18]),
							CostCenter = Conversions.ToString(sqlDataReader[19]),
							EmployeeAcc = Conversions.ToString(sqlDataReader[20])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetBanks()
		{
			string result = default(string);
			try
			{
				List<ListBank> list;
				list = new List<ListBank>();
				string cmdText;
				cmdText = "SELECT ISNULL([id], 0) AS id, ISNULL([name], '') AS name, ISNULL([country], '') AS country, ISNULL([city], '') AS city, ISNULL([Acc_Code], '') AS Acc_Code, ISNULL([area], '') AS area, ISNULL([tel], '') AS tel, ISNULL([mobile], '') AS mobile, ISNULL([notes], '') AS notes, ISNULL([IS_Deleted], 0) AS IS_Deleted, ISNULL([DisPre], 0) AS DisPre, ISNULL([ChangeInPOS], 0) AS ChangeInPOS FROM Banks";
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
					sqlConnection.Open();
					using SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListBank
						{
							Id = Conversions.ToInteger(sqlDataReader[0]),
							Name = sqlDataReader[1].ToString(),
							Country = sqlDataReader[2].ToString(),
							City = sqlDataReader[3].ToString(),
							AccCode = sqlDataReader[4].ToString(),
							Area = sqlDataReader[5].ToString(),
							Tel = sqlDataReader[6].ToString(),
							Mobile = sqlDataReader[7].ToString(),
							Notes = sqlDataReader[8].ToString(),
							IsDeleted = Conversions.ToBoolean(sqlDataReader[9]),
							DisPre = Conversions.ToDecimal(sqlDataReader[10]),
							ChangeInPOS = Conversions.ToBoolean(sqlDataReader[11])
						});
					}
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetCasherClosed()
		{
			string result = default(string);
			try
			{
				List<ListCasherClosed> list;
				list = new List<ListCasherClosed>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					SqlDataReader sqlDataReader;
					sqlDataReader = new SqlCommand("SELECT ISNULL([id], 0) AS [id], ISNULL([type], '') AS [type], ISNULL([user_id], 0) AS [user_id], ISNULL([startTime], GETDATE()) AS [startTime], ISNULL([endTime], GETDATE()) AS [endTime], ISNULL([ToT], 0) AS [ToT], ISNULL([CasherValue], 0) AS [CasherValue], ISNULL([diff], 0) AS [diff], ISNULL([GlobalID], '') AS [GlobalID], ISNULL([Branch], '') AS [Branch], ISNULL([ClosedID], 0) AS [ClosedID] FROM CasherClosed", sqlConnection).ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListCasherClosed
						{
							Id = Conversions.ToInteger(sqlDataReader[0]),
							Type = Conversions.ToString(sqlDataReader[1]),
							UserId = Conversions.ToInteger(sqlDataReader[2]),
							StartTime = Conversions.ToDate(sqlDataReader[3]),
							EndTime = Conversions.ToDate(sqlDataReader[4]),
							ToT = Conversions.ToDecimal(sqlDataReader[5]),
							CasherValue = Conversions.ToDecimal(sqlDataReader[6]),
							Diff = Conversions.ToDecimal(sqlDataReader[7]),
							GlobalID = sqlDataReader[8].ToString(),
							Branch = Conversions.ToString(sqlDataReader[9]),
							ClosedID = Conversions.ToInteger(sqlDataReader[10])
						});
					}
					sqlDataReader.Close();
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetCasherClosedSub()
		{
			string result = default(string);
			try
			{
				List<ListCasherClosedSub> list;
				list = new List<ListCasherClosedSub>();
				using (SqlConnection sqlConnection = new SqlConnection(global::SmartAuditERP.SendData.connString))
				{
					sqlConnection.Open();
					SqlDataReader sqlDataReader;
					sqlDataReader = new SqlCommand("SELECT ISNULL([id], 0) AS [id], ISNULL([ClosedId], 0) AS [ClosedId], ISNULL([CashTotal], 0) AS [CashTotal], ISNULL([SAfeNetVal], 0) AS [SAfeNetVal], ISNULL([ReturnSum], 0) AS [ReturnSum], ISNULL([NetworkSum], 0) AS [NetworkSum], ISNULL([AdditionVal], 0) AS [AdditionVal], ISNULL([PostPoneSales], 0) AS [PostPoneSales], ISNULL([PostPoneRet], 0) AS [PostPoneRet], ISNULL([InsurVal], 0) AS [InsurVal], ISNULL([Discount], 0) AS [Discount], ISNULL([AllVAT], 0) AS [AllVAT], ISNULL([ExtraTax], 0) AS [ExtraTax], ISNULL([Expenses], 0) AS [Expenses], ISNULL([Purchases], 0) AS [Purchases], ISNULL([HostingVal], 0) AS [HostingVal], ISNULL([IsDeleted], 0) AS [IsDeleted], ISNULL([GlobalID], '') AS [GlobalID] FROM CasherClosed_Sub", sqlConnection).ExecuteReader();
					while (sqlDataReader.Read())
					{
						list.Add(new ListCasherClosedSub
						{
							Id = Conversions.ToInteger(sqlDataReader[0]),
							ClosedId = Conversions.ToInteger(sqlDataReader[1]),
							CashTotal = Conversions.ToDecimal(sqlDataReader[2]),
							SAfeNetVal = Conversions.ToDecimal(sqlDataReader[3]),
							ReturnSum = Conversions.ToDecimal(sqlDataReader[4]),
							NetworkSum = Conversions.ToDecimal(sqlDataReader[5]),
							AdditionVal = Conversions.ToDecimal(sqlDataReader[6]),
							PostPoneSales = Conversions.ToDecimal(sqlDataReader[7]),
							PostPoneRet = Conversions.ToDecimal(sqlDataReader[8]),
							InsurVal = Conversions.ToDecimal(sqlDataReader[9]),
							Discount = Conversions.ToDecimal(sqlDataReader[10]),
							AllVAT = Conversions.ToDecimal(sqlDataReader[11]),
							ExtraTax = Conversions.ToDecimal(sqlDataReader[12]),
							Expenses = Conversions.ToDecimal(sqlDataReader[13]),
							Purchases = Conversions.ToDecimal(sqlDataReader[14]),
							HostingVal = Conversions.ToDecimal(sqlDataReader[15]),
							IsDeleted = Conversions.ToBoolean(sqlDataReader[16]),
							GlobalID = sqlDataReader[17].ToString()
						});
					}
					sqlDataReader.Close();
				}
				string plainText;
				plainText = string.Concat(str1: JsonConvert.SerializeObject(list), str0: global::SmartAuditERP.SendData._passcode);
				result = global::SmartAuditERP.SendData.ConEncrypt.EncryptString(plainText, global::SmartAuditERP.SendData._passcode);
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static byte[] ImageToByteArray(Image image)
		{
			using MemoryStream memoryStream = new MemoryStream();
			image.Save(memoryStream, image.RawFormat);
			return memoryStream.ToArray();
		}

		public static void SendDataa(string topicType, string recordId, string jsonData)
		{
			try
			{
				if (!ConnectBroker.mqttClient.IsConnected)
				{
					try
					{
						ConnectBroker.mqttClient.Connect(Guid.NewGuid().ToString());
					}
					catch (Exception ex)
					{
						ProjectData.SetProjectError(ex);
						Console.WriteLine("Error connecting to broker: " + ex.Message);
						ProjectData.ClearProjectError();
						return;
					}
				}
				if (ConnectBroker.mqttClient.IsConnected)
				{
					string topic;
					topic = $"{ConnectBroker.ClientCode.Trim()}{topicType}/{recordId}";
					ConnectBroker.mqttClient.Publish(topic, Encoding.UTF8.GetBytes(jsonData), 1, retain: true);
					Console.WriteLine($"{topicType} {recordId} sent and retained.");
				}
				else
				{
					Console.WriteLine("Failed to connect to the broker.");
				}
			}
			catch (Exception ex2)
			{
				ProjectData.SetProjectError(ex2);
				Console.WriteLine("Error: " + ex2.Message);
				ProjectData.ClearProjectError();
			}
		}
	}
}