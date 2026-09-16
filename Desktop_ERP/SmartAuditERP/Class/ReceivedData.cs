using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SmartAuditERP
{

	[StandardModule]
	internal sealed class ReceivedData
	{
		public static AESencDEC ConEncrypt = new AESencDEC();

		public static string connString = "";

		public static SqlConnection conn = new SqlConnection(ReceivedData.connString);

		public static string lastErrorMsg;

		public static string _passcode = "";

		public static void LoadReceived(string Cnstring)
		{
			try
			{
				if (ConnectBroker.mqttClient == null)
				{
					Interaction.MsgBox("mqttClient غير مهيأ!");
					return;
				}
				if (string.IsNullOrEmpty(ConnectBroker.ClientCode))
				{
					Interaction.MsgBox("ClientCode غير مضبوط!");
					return;
				}
				if (!ConnectBroker.CheckConnectionAndBroker())
				{
					Interaction.MsgBox("فشل الاتصال بالوسيط!");
					return;
				}
				ReceivedData.connString = Cnstring;
				string[] source;
				source = new string[20]
				{
				"Items", "ItemsUnit", "ItemPrices", "ItemComponents", "ItemSerialNo", "ItemBarcode", "ItemsCategory", "Units", "Customers", "AccountCust",
				"AccountIndex", "CasherClosed", "CasherClosed_Sub", "Inv/#", "InvSub/#", "entry/#", "entryDetails/#", "Receipts/#", "CasherClosed/#", "CasherClosed_Sub/#"
				};
				try
				{
					ConnectBroker.mqttClient.Unsubscribe(source.Select([SpecialName] (string t) => ConnectBroker.ClientCode + t).ToArray());
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				if (ConnectBroker._syncItems)
				{
					string[] array;
					array = new string[8] { "Items", "ItemsUnit", "ItemPrices", "ItemComponents", "ItemSerialNo", "ItemBarcode", "ItemsCategory", "Units" };
					foreach (string text in array)
					{
						ConnectBroker.mqttClient.Subscribe(new string[1] { ConnectBroker.ClientCode + text }, new byte[1] { 1 });
					}
					ConnectBroker.mqttClient.MqttMsgPublishReceived -= MqttMsgReceivedItems;
					ConnectBroker.mqttClient.MqttMsgPublishReceived += MqttMsgReceivedItems;
				}
				if (ConnectBroker._syncOper)
				{
					string[] array2;
					array2 = new string[12]
					{
					"Customers", "AccountCust", "AccountIndex", "CasherClosed", "CasherClosed_Sub", "Inv/#", "InvSub/#", "entry/#", "entryDetails/#", "Receipts/#",
					"CasherClosed/#", "CasherClosed_Sub/#"
					};
					foreach (string text2 in array2)
					{
						ConnectBroker.mqttClient.Subscribe(new string[1] { ConnectBroker.ClientCode + text2 }, new byte[1] { 1 });
					}
					ConnectBroker.mqttClient.MqttMsgPublishReceived -= MqttMsgReceivedOper;
					ConnectBroker.mqttClient.MqttMsgPublishReceived += MqttMsgReceivedOper;
				}
				if (ConnectBroker._syncDefinitions)
				{
					string[] array3;
					array3 = new string[11]
					{
					"Banks", "Branches", "StockEmp", "Stock", "SafeEmp", "OperationPermission", "UserPermissions", "Users", "AccountEmp", "EmpBranches",
					"Employee"
					};
					foreach (string text3 in array3)
					{
						ConnectBroker.mqttClient.Subscribe(new string[1] { ConnectBroker.ClientCode + text3 }, new byte[1] { 1 });
					}
					ConnectBroker.mqttClient.MqttMsgPublishReceived -= MqttMsgReceiveddefinitions;
					ConnectBroker.mqttClient.MqttMsgPublishReceived += MqttMsgReceiveddefinitions;
				}
			}
			catch (OutOfMemoryException ex)
			{
				ProjectData.SetProjectError(ex);
				Interaction.MsgBox("خطأ في الذاكرة: " + ex.Message);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				ProjectData.ClearProjectError();
			}
			catch (Exception ex2)
			{
				ProjectData.SetProjectError(ex2);
				Exception ex3;
				ex3 = ex2;
				Interaction.MsgBox("خطأ: " + ex3.Message + "\r\nتفاصيل الخطأ: " + ex3.StackTrace, MsgBoxStyle.Critical);
				ProjectData.ClearProjectError();
			}
		}

		private static void UnsubscribeFromTopics()
		{
			string[] source;
			source = new string[20]
			{
			"Items", "ItemsUnit", "ItemPrices", "ItemComponents", "ItemSerialNo", "ItemBarcode", "ItemsCategory", "Units", "Customers", "AccountCust",
			"AccountIndex", "CasherClosed", "CasherClosed_Sub", "Inv/#", "InvSub/#", "entry/#", "entryDetails/#", "Receipts/#", "CasherClosed/#", "CasherClosed_Sub/#"
			};
			ConnectBroker.mqttClient.Unsubscribe(source.Select([SpecialName] (string t) => ConnectBroker.ClientCode + t).ToArray());
		}

		private static void SubscribeToTopics(string[] topics, MqttClient.MqttMsgPublishEventHandler handler)
		{
			ConnectBroker.mqttClient.MqttMsgPublishReceived -= handler;
			foreach (string text in topics)
			{
				ConnectBroker.mqttClient.Subscribe(new string[1] { ConnectBroker.ClientCode + text }, new byte[1] { 1 });
			}
			ConnectBroker.mqttClient.MqttMsgPublishReceived += handler;
		}

		private static void MqttMsgReceivedOper(object sender, MqttMsgPublishEventArgs e)
		{
			try
			{
				string cipherText;
				cipherText = Encoding.UTF8.GetString(e.Message);
				cipherText = ReceivedData.ConEncrypt.DecryptString(cipherText, ReceivedData._passcode).Replace(ReceivedData._passcode, "");
				if (string.IsNullOrWhiteSpace(cipherText))
				{
					Console.WriteLine("Received an empty or invalid message.");
					return;
				}
				string text;
				text = e.Topic.Replace(ConnectBroker.ClientCode, "");
				string text2;
				text2 = text.Split('/')[0];
				_ = text.Split('/')[1];
				switch (text2)
				{
					case "Inv":
						try
						{
							ReceivedData.InsertInv(JsonConvert.DeserializeObject<List<ListInvoice>>(cipherText));
							break;
						}
						catch (Exception projectError7)
						{
							ProjectData.SetProjectError(projectError7);
							ProjectData.ClearProjectError();
							break;
						}
					case "InvSub":
						try
						{
							ReceivedData.InsertInvSub(JsonConvert.DeserializeObject<List<listInvSub>>(cipherText));
							break;
						}
						catch (Exception projectError6)
						{
							ProjectData.SetProjectError(projectError6);
							ProjectData.ClearProjectError();
							break;
						}
					case "entry":
						try
						{
							ReceivedData.InsertOrUpdateEntry(JsonConvert.DeserializeObject<List<ListEntry>>(cipherText));
							break;
						}
						catch (Exception projectError5)
						{
							ProjectData.SetProjectError(projectError5);
							ProjectData.ClearProjectError();
							break;
						}
					case "entryDetails":
						try
						{
							ReceivedData.InsertEntrySub(JsonConvert.DeserializeObject<List<ListEntrySub>>(cipherText));
							break;
						}
						catch (Exception projectError4)
						{
							ProjectData.SetProjectError(projectError4);
							ProjectData.ClearProjectError();
							break;
						}
					case "Receipts":
						try
						{
							ReceivedData.InsertOrUpdateReceipts(JsonConvert.DeserializeObject<List<ListRecipt>>(cipherText));
							break;
						}
						catch (Exception projectError3)
						{
							ProjectData.SetProjectError(projectError3);
							ProjectData.ClearProjectError();
							break;
						}
					case "CasherClosed":
						try
						{
							ReceivedData.InsertOrUpdateCasherClosed(JsonConvert.DeserializeObject<List<ListCasherClosed>>(cipherText));
							break;
						}
						catch (Exception projectError2)
						{
							ProjectData.SetProjectError(projectError2);
							ProjectData.ClearProjectError();
							break;
						}
					case "CasherClosed_Sub":
						try
						{
							ReceivedData.InsertOrUpdateCasherClosedSub(JsonConvert.DeserializeObject<List<ListCasherClosedSub>>(cipherText));
							break;
						}
						catch (Exception projectError)
						{
							ProjectData.SetProjectError(projectError);
							ProjectData.ClearProjectError();
							break;
						}
					default:
						Console.WriteLine($"Unknown topic type: {text2}");
						break;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine("Error processing message: " + ex.Message);
				ProjectData.ClearProjectError();
			}
		}

		public static void MqttMsgReceiveddefinitions(object sender, MqttMsgPublishEventArgs e)
		{
			try
			{
				string cipherText;
				cipherText = Encoding.UTF8.GetString(e.Message);
				_ = ReceivedData._passcode;
				cipherText = ReceivedData.ConEncrypt.DecryptString(cipherText, ReceivedData._passcode).Replace(ReceivedData._passcode, "");
				string topic;
				topic = e.Topic;
				if (topic.EndsWith("AccountCust"))
				{
					ReceivedData.InsertAccounts_IndexCust(JsonConvert.DeserializeObject<List<ListAccounCust>>(cipherText));
				}
				else if (topic.EndsWith("AccountIndex"))
				{
					ReceivedData.InsertAccounts_Index(JsonConvert.DeserializeObject<List<ListAccountIndex>>(cipherText));
				}
				else if (topic.EndsWith("Employee"))
				{
					ReceivedData.InsertEmployees(JsonConvert.DeserializeObject<List<ListEmployee>>(cipherText));
				}
				else if (topic.EndsWith("EmpBranches"))
				{
					ReceivedData.insertEmpBranch(JsonConvert.DeserializeObject<List<ListEmpBranch>>(cipherText));
				}
				else if (topic.EndsWith("AccountEmp"))
				{
					ReceivedData.InsertAccounts_IndexCust(JsonConvert.DeserializeObject<List<ListAccounCust>>(cipherText));
				}
				else if (topic.EndsWith("Users"))
				{
					ReceivedData.InsertUsers(JsonConvert.DeserializeObject<List<ListUsers>>(cipherText));
				}
				else if (topic.EndsWith("UserPermissions"))
				{
					ReceivedData.InserUserPermissions(JsonConvert.DeserializeObject<List<ListUserPermission>>(cipherText));
				}
				else if (topic.EndsWith("OperationPermission"))
				{
					ReceivedData.InsertOperPrimisson(JsonConvert.DeserializeObject<List<ListOperationPermission>>(cipherText));
				}
				else if (topic.EndsWith("Offer"))
				{
					ReceivedData.InsertOffer(JsonConvert.DeserializeObject<List<ListOffers>>(cipherText));
				}
				else if (topic.EndsWith("OfferClient"))
				{
					ReceivedData.InsertOfferClinet(JsonConvert.DeserializeObject<List<ListOfferForClient>>(cipherText));
				}
				else if (topic.EndsWith("OfferItem"))
				{
					ReceivedData.InsertOfferItems(JsonConvert.DeserializeObject<List<ListOfferItem>>(cipherText));
				}
				else if (topic.EndsWith("Safe"))
				{
					ReceivedData.InsertOrUpdateSafes(JsonConvert.DeserializeObject<List<ListSafes>>(cipherText));
				}
				else if (topic.EndsWith("SafeEmp"))
				{
					ReceivedData.InsertOrUpdateSafesEmp(JsonConvert.DeserializeObject<List<ListSafeEmp>>(cipherText));
				}
				else if (topic.EndsWith("Stock"))
				{
					ReceivedData.InsertOrUpdateStocks(JsonConvert.DeserializeObject<List<ListStock>>(cipherText));
				}
				else if (topic.EndsWith("StockEmp"))
				{
					ReceivedData.InsertOrUpdateStockEmps(JsonConvert.DeserializeObject<List<ListStockEmp>>(cipherText));
				}
				else if (topic.EndsWith("Branches"))
				{
					ReceivedData.InsertOrUpdateBranches(JsonConvert.DeserializeObject<List<ListBranches>>(cipherText));
				}
				else if (topic.EndsWith("Banks"))
				{
					ReceivedData.InsertOrUpdateBanks(JsonConvert.DeserializeObject<List<ListBank>>(cipherText));
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Interaction.MsgBox("Error processing message: " + ex.Message);
				ProjectData.ClearProjectError();
			}
		}

		public static void MqttMsgReceivedItems(object sender, MqttMsgPublishEventArgs e)
		{
			try
			{
				string cipherText;
				cipherText = Encoding.UTF8.GetString(e.Message);
				_ = ReceivedData._passcode;
				cipherText = ReceivedData.ConEncrypt.DecryptString(cipherText, ReceivedData._passcode).Replace(ReceivedData._passcode, "");
				string topic;
				topic = e.Topic;
				if (topic.EndsWith("Items"))
				{
					ReceivedData.InsertItems(JsonConvert.DeserializeObject<List<ListItems>>(cipherText));
				}
				else if (topic.EndsWith("ItemsUnit"))
				{
					ReceivedData.InsertItemUnits(JsonConvert.DeserializeObject<List<ListItemunit>>(cipherText), ReceivedData.connString);
				}
				else if (topic.EndsWith("ItemPrices"))
				{
					ReceivedData.InsertItemPrice(JsonConvert.DeserializeObject<List<ItemPrices>>(cipherText));
				}
				else if (topic.EndsWith("ItemComponents"))
				{
					ReceivedData.InsertItemComponents(JsonConvert.DeserializeObject<List<ItemComponent>>(cipherText));
				}
				else if (topic.EndsWith("ItemSerialNo"))
				{
					ReceivedData.InsertItemSerialNo(JsonConvert.DeserializeObject<List<ItemSerialNo>>(cipherText));
				}
				else if (topic.EndsWith("Customers"))
				{
					ReceivedData.InsertCustomers(JsonConvert.DeserializeObject<List<ListCustomers>>(cipherText));
				}
				else if (topic.EndsWith("ItemsCategory"))
				{
					ReceivedData.insertItemCategory(JsonConvert.DeserializeObject<List<ListItemsCategory>>(cipherText));
				}
				else if (topic.EndsWith("ItemBarcode"))
				{
					ReceivedData.insertItemBarcode(JsonConvert.DeserializeObject<List<listItemBarcode>>(cipherText));
				}
				else if (topic.EndsWith("Units"))
				{
					ReceivedData.InsertUnits(JsonConvert.DeserializeObject<List<ListUnit>>(cipherText));
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Interaction.MsgBox("Error processing message: " + ex.Message);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertItems(List<ListItems> items)
		{
			ReceivedData.connString = ReceivedData.connString;
			using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
			{
				try
				{
					sqlConnection.Open();
					foreach (ListItems item in items)
					{
						new SqlCommand();
						SqlCommand sqlCommand;
						sqlCommand = ((!ReceivedData.IsItemExists(item.id)) ? new SqlCommand("INSERT INTO Items (id, name, nameEN, code, barcode, Grpcode, group_id, unit, ShowInPOS, Wscale, purch_price, sale_price, [limit], tax, discount, tax_group, store, IS_Deleted, ItemType, ItemProperty, FillValue, MaxQtyLimit, WithholdingTax, EgyItemCode, EgyCodeType, MaxDicountParcent, is_extra_tax_applied,MaxDiscountAmount,showInAndroid,Item_sort) \r\n                                               VALUES (@id, @name, @nameEN, @code, @barcode, @Grpcode, @group_id, @unit, @ShowInPOS, @Wscale, @purch_price, @sale_price, @limit, @tax, @discount, @tax_group, @store, @IS_Deleted, @ItemType, @ItemProperty, @FillValue, @MaxQtyLimit, @WithholdingTax, @EgyItemCode, @EgyCodeType, @MaxDicountParcent, @is_extra_tax_applied,@MaxDiscountAmount,@showInAndroid,@Item_sort)", sqlConnection) : new SqlCommand(("update Items set Code=@Code,GrpCode=@GrpCode, name=@name,nameEN=@nameEN,barcode=@barcode,group_id=@group_id,unit=@unit,\r\n                                      purch_price=@purch_price,sale_price=@sale_price,limit=@limit,discount=@discount,tax_group=@tax_group,tax=@tax,IS_Deleted=@IS_Deleted,\r\n                                     ShowInPOS=@ShowInPOS,Wscale=@Wscale,store=@store,ItemType=@ItemType,ItemProperty=@ItemProperty,FillValue=@FillValue,\r\n                                      MaxQtyLimit=@MaxQtyLimit  , EgyItemCode=@EgyItemCode,WithholdingTax=@WithholdingTax,EgyCodeType=@EgyCodeType,MaxDicountParcent=@MaxDicountParcent, \r\n                                      is_extra_tax_applied=@is_extra_tax_applied,MaxDiscountAmount=@MaxDiscountAmount,showInAndroid=@showInAndroid ,Item_sort=@Item_sort where id=" + Conversions.ToString(item.id)) ?? "", sqlConnection));
						sqlCommand.Parameters.AddWithValue("@id", item.id);
						sqlCommand.Parameters.AddWithValue("@name", item.name);
						sqlCommand.Parameters.AddWithValue("@nameEN", item.nameEN);
						sqlCommand.Parameters.AddWithValue("@code", item.code);
						sqlCommand.Parameters.AddWithValue("@barcode", item.barcode);
						sqlCommand.Parameters.AddWithValue("@Grpcode", item.Grpcode);
						sqlCommand.Parameters.AddWithValue("@group_id", item.group_id);
						sqlCommand.Parameters.AddWithValue("@unit", item.unit);
						sqlCommand.Parameters.AddWithValue("@ShowInPOS", item.ShowInPOS);
						sqlCommand.Parameters.AddWithValue("@Wscale", item.Wscale);
						sqlCommand.Parameters.AddWithValue("@purch_price", item.purch_price);
						sqlCommand.Parameters.AddWithValue("@sale_price", item.sale_price);
						sqlCommand.Parameters.AddWithValue("@limit", item.limit);
						sqlCommand.Parameters.AddWithValue("@tax", item.tax);
						sqlCommand.Parameters.AddWithValue("@discount", item.discount);
						sqlCommand.Parameters.AddWithValue("@tax_group", item.tax_group);
						sqlCommand.Parameters.AddWithValue("@store", item.store);
						sqlCommand.Parameters.AddWithValue("@IS_Deleted", item.IS_Deleted);
						sqlCommand.Parameters.AddWithValue("@ItemType", item.ItemType);
						sqlCommand.Parameters.AddWithValue("@ItemProperty", item.ItemProperty);
						sqlCommand.Parameters.AddWithValue("@FillValue", item.FillValue);
						sqlCommand.Parameters.AddWithValue("@MaxQtyLimit", item.MaxQtyLimit);
						sqlCommand.Parameters.AddWithValue("@WithholdingTax", item.WithholdingTax);
						sqlCommand.Parameters.AddWithValue("@EgyItemCode", item.EgyItemCode);
						sqlCommand.Parameters.AddWithValue("@EgyCodeType", item.EgyCodeType);
						sqlCommand.Parameters.AddWithValue("@MaxDicountParcent", item.MaxDicountParcent);
						sqlCommand.Parameters.AddWithValue("@is_extra_tax_applied", item.is_extra_tax_applied);
						sqlCommand.Parameters.AddWithValue("@MaxDiscountAmount", item.MaxDiscountAmount);
						sqlCommand.Parameters.AddWithValue("@showInAndroid", item.showInAndroid);
						sqlCommand.Parameters.AddWithValue("@Item_Sort", item.Item_Sort);
						sqlCommand.ExecuteNonQuery();
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			ReceivedData.conn.Close();
		}

		private static void InsertItemUnits(List<ListItemunit> units, string Connstring)
		{
			using (SqlConnection sqlConnection = new SqlConnection(Connstring))
			{
				try
				{
					new SqlCommand();
					sqlConnection.Open();
					foreach (ListItemunit unit in units)
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemUnits WHERE Itemid = @Itemid AND unit = @unit", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@Itemid", unit.Itemid);
						sqlCommand.Parameters.AddWithValue("@unit", unit.unite);
						if (Conversions.ToInteger(sqlCommand.ExecuteScalar()) > 0)
						{
							SqlCommand sqlCommand2;
							sqlCommand2 = new SqlCommand("UPDATE ItemUnits SET perc = @perc, purch = @purch, sale = @sale, barcode = @barcode WHERE Itemid = @Itemid AND unit = @unit", sqlConnection);
							sqlCommand2.Parameters.AddWithValue("@Itemid", unit.Itemid);
							sqlCommand2.Parameters.AddWithValue("@unit", unit.unite);
							sqlCommand2.Parameters.AddWithValue("@perc", unit.perc);
							sqlCommand2.Parameters.AddWithValue("@purch", unit.purch);
							sqlCommand2.Parameters.AddWithValue("@sale", unit.sale);
							sqlCommand2.Parameters.AddWithValue("@barcode", unit.barcode);
							sqlCommand2.ExecuteNonQuery();
						}
						else
						{
							SqlCommand sqlCommand2;
							sqlCommand2 = new SqlCommand("INSERT INTO ItemUnits (Itemid, unit, perc, purch, sale, barcode) \r\n                                           VALUES (@Itemid, @unit, @perc, @purch, @sale, @barcode)", sqlConnection);
							sqlCommand2.Parameters.AddWithValue("@Itemid", unit.Itemid);
							sqlCommand2.Parameters.AddWithValue("@unit", unit.unite);
							sqlCommand2.Parameters.AddWithValue("@perc", unit.perc);
							sqlCommand2.Parameters.AddWithValue("@purch", unit.purch);
							sqlCommand2.Parameters.AddWithValue("@sale", unit.sale);
							sqlCommand2.Parameters.AddWithValue("@barcode", unit.barcode);
							sqlCommand2.ExecuteNonQuery();
						}
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			ReceivedData.conn.Close();
		}

		public static void InsertItemPrice(List<ItemPrices> prices)
		{
			ReceivedData.connString = ReceivedData.connString;
			new SqlCommand();
			using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
			try
			{
				sqlConnection.Open();
				foreach (ItemPrices price in prices)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemPrices WHERE Proc_id = @Proc_id AND ItemID = @ItemID AND UnitID = @UnitID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Proc_id", price.Proc_id);
					sqlCommand.Parameters.AddWithValue("@ItemID", price.ItemID);
					sqlCommand.Parameters.AddWithValue("@UnitID", price.UnitID);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Conversions.ToInteger(sqlCommand.ExecuteScalar()) <= 0) ? new SqlCommand("SET IDENTITY_INSERT ItemPrices ON \r\n                                                 INSERT INTO ItemPrices\r\n                                                 (Proc_id, ItemID, UnitID, [time], [date], purch_price, sale_price, \r\n                                                  low_purch_price, high_purch_price, low_sale_price, high_sale_price, \r\n                                                  CompetitorPrice, Emp, IS_Deleted, ConsumerPrice, WholesalePrice) \r\n                                                 VALUES (@Proc_id, @ItemID, @UnitID, @Time, @Date, @PurchPrice, @SalePrice, \r\n                                                         @LowPurchPrice, @HighPurchPrice, @LowSalePrice, @HighSalePrice, \r\n                                                         @CompetitorPrice, @Emp, @IS_Deleted, @ConsumerPrice, @WholesalePrice)", sqlConnection) : new SqlCommand("UPDATE ItemPrices \r\n                                                 SET [time] = @Time, [date] = @Date, purch_price = @PurchPrice, sale_price = @SalePrice, \r\n                                                     low_purch_price = @LowPurchPrice, high_purch_price = @HighPurchPrice, \r\n                                                     low_sale_price = @LowSalePrice, high_sale_price = @HighSalePrice, \r\n                                                     CompetitorPrice = @CompetitorPrice, Emp = @Emp, IS_Deleted = @IS_Deleted, \r\n                                                     ConsumerPrice = @ConsumerPrice, WholesalePrice = @WholesalePrice\r\n                                                 WHERE Proc_id = @Proc_id AND ItemID = @ItemID AND UnitID = @UnitID", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@Proc_id", price.Proc_id);
					sqlCommand2.Parameters.AddWithValue("@ItemID", price.ItemID);
					sqlCommand2.Parameters.AddWithValue("@UnitID", price.UnitID);
					sqlCommand2.Parameters.AddWithValue("@Time", price.Time);
					sqlCommand2.Parameters.AddWithValue("@Date", price.Date);
					sqlCommand2.Parameters.AddWithValue("@PurchPrice", price.PurchPrice);
					sqlCommand2.Parameters.AddWithValue("@SalePrice", price.SalePrice);
					sqlCommand2.Parameters.AddWithValue("@LowPurchPrice", price.LowPurchPrice);
					sqlCommand2.Parameters.AddWithValue("@HighPurchPrice", price.HighPurchPrice);
					sqlCommand2.Parameters.AddWithValue("@LowSalePrice", price.LowSalePrice);
					sqlCommand2.Parameters.AddWithValue("@HighSalePrice", price.HighSalePrice);
					sqlCommand2.Parameters.AddWithValue("@CompetitorPrice", price.CompetitorPrice);
					sqlCommand2.Parameters.AddWithValue("@Emp", price.Emp);
					sqlCommand2.Parameters.AddWithValue("@IS_Deleted", price.IS_Deleted);
					sqlCommand2.Parameters.AddWithValue("@ConsumerPrice", price.ConsumerPrice);
					sqlCommand2.Parameters.AddWithValue("@WholesalePrice", price.WholesalePrice);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static bool IsItemExists(int itemId)
		{
			bool result;
			result = false;
			string cmdText;
			cmdText = "SELECT COUNT(*) FROM Items WHERE id = @id";
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				using SqlCommand sqlCommand = new SqlCommand(cmdText, sqlConnection);
				sqlCommand.Parameters.AddWithValue("@id", itemId);
				if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
				{
					result = true;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static void InsertItemComponents(List<ItemComponent> itemComponents)
		{
			ReceivedData.connString = ReceivedData.connString;
			new SqlCommand();
			using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
			try
			{
				sqlConnection.Open();
				foreach (ItemComponent itemComponent in itemComponents)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemComponents WHERE Id = @Id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Id", itemComponent.Id);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("\r\n                            INSERT INTO ItemComponents \r\n                            (Id, itemId, ComponentId, price, quantity, unit, total, type, store) \r\n                            VALUES \r\n                            (@Id, @ItemId, @ComponentId, @Price, @Quantity, @Unit, @Total, @Type, @Store)", sqlConnection) : new SqlCommand("\r\n                            UPDATE ItemComponents \r\n                            SET \r\n                                itemId = @ItemId, \r\n                                ComponentId = @ComponentId, \r\n                                price = @Price, \r\n                                quantity = @Quantity, \r\n                                unit = @Unit, \r\n                                total = @Total, \r\n                                type = @Type, \r\n                                store = @Store \r\n                            WHERE Id = @Id", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@ItemId", itemComponent.ItemId);
					sqlCommand2.Parameters.AddWithValue("@ComponentId", itemComponent.ComponentId);
					sqlCommand2.Parameters.AddWithValue("@Price", itemComponent.Price);
					sqlCommand2.Parameters.AddWithValue("@Quantity", itemComponent.Quantity);
					sqlCommand2.Parameters.AddWithValue("@Unit", itemComponent.Unit);
					sqlCommand2.Parameters.AddWithValue("@Total", itemComponent.Total);
					sqlCommand2.Parameters.AddWithValue("@Type", itemComponent.Type);
					sqlCommand2.Parameters.AddWithValue("@Store", itemComponent.Store);
					sqlCommand2.Parameters.AddWithValue("@Id", itemComponent.Id);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertItemSerialNo(List<ItemSerialNo> itemSerialNoList)
		{
			ReceivedData.connString = ReceivedData.connString;
			new SqlCommand();
			using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
			try
			{
				sqlConnection.Open();
				foreach (ItemSerialNo itemSerialNo in itemSerialNoList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemSerialNo WHERE ItemId = @ItemId and SerialNo=@SerialNo", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@ItemId", itemSerialNo.Id);
					sqlCommand.Parameters.AddWithValue("@SerialNo", itemSerialNo.SerialNo);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("INSERT INTO ItemSerialNo (Id, ItemId, SerialNo, InvGlobalID) VALUES (@Id, @ItemId, @SerialNo, @InvGlobalID)", sqlConnection) : new SqlCommand("UPDATE ItemSerialNo SET ItemId = @ItemId, SerialNo = @SerialNo, InvGlobalID = @InvGlobalID WHERE Id = @Id", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@ItemId", itemSerialNo.ItemId);
					sqlCommand2.Parameters.AddWithValue("@SerialNo", itemSerialNo.SerialNo);
					sqlCommand2.Parameters.AddWithValue("@InvGlobalID", itemSerialNo.InvGlobalID);
					sqlCommand2.Parameters.AddWithValue("@Id", itemSerialNo.Id);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertCustomers(List<ListCustomers> customers)
		{
			ReceivedData.connString = ReceivedData.connString;
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				new SqlCommand();
				sqlConnection.Open();
				foreach (ListCustomers customer in customers)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Customers WHERE Id = @Id and type=@type", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Id", customer.Id);
					sqlCommand.Parameters.AddWithValue("@type", customer.Type);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("SET IDENTITY_INSERT [dbo].[Customers] ON\r\n                        INSERT INTO Customers (id,\r\n                            [name], [country], [city], [area], [act], [national_id], [tel], [mobile], [fax], [email], \r\n                            [notes], [IS_Deleted], [maxdepit], [type], [tax_no], [AccountCode], [Branch], \r\n                            [ISCredit], [PlotIdentification], [BuildingNumber], [StreetName], [AdditionalStreetName], \r\n                            [District], [PostalZone], [CrNo], [Pricing])\r\n                        VALUES (@id,\r\n                            @Name, @Country, @City, @Area, @Act, @NationalId, @Tel, @Mobile, @Fax, @Email, \r\n                            @Notes, @IsDeleted, @MaxDepit, @Type, @TaxNo, @AccountCode, @Branch, \r\n                            @IsCredit, @PlotIdentification, @BuildingNumber, @StreetName, @AdditionalStreetName, \r\n                            @District, @PostalZone, @CrNo, @Pricing)", sqlConnection) : new SqlCommand("\r\n                        UPDATE Customers SET \r\n                            [name] = @Name,\r\n                            [country] = @Country,\r\n                            [city] = @City,\r\n                            [area] = @Area,\r\n                            [act] = @Act,\r\n                            [national_id] = @NationalId,\r\n                            [tel] = @Tel,\r\n                            [mobile] = @Mobile,\r\n                            [fax] = @Fax,\r\n                            [email] = @Email,\r\n                            [notes] = @Notes,\r\n                            [IS_Deleted] = @IsDeleted,\r\n                            [maxdepit] = @MaxDepit,\r\n                            [type] = @Type,\r\n                            [tax_no] = @TaxNo,\r\n                            [AccountCode] = @AccountCode,                           \r\n                            [Branch] = @Branch,\r\n                            [ISCredit] = @IsCredit,\r\n                            [PlotIdentification] = @PlotIdentification,\r\n                            [BuildingNumber] = @BuildingNumber,\r\n                            [StreetName] = @StreetName,\r\n                            [AdditionalStreetName] = @AdditionalStreetName,\r\n                            [District] = @District,\r\n                            [PostalZone] = @PostalZone,\r\n                            [CrNo] = @CrNo,\r\n                            [Pricing] = @Pricing\r\n                        WHERE [Id] = @Id", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@Id", customer.Id);
					sqlCommand2.Parameters.AddWithValue("@Name", customer.Name);
					sqlCommand2.Parameters.AddWithValue("@Country", customer.Country);
					sqlCommand2.Parameters.AddWithValue("@City", customer.City);
					sqlCommand2.Parameters.AddWithValue("@Area", customer.Area);
					sqlCommand2.Parameters.AddWithValue("@Act", customer.Act);
					sqlCommand2.Parameters.AddWithValue("@NationalId", customer.NationalId);
					sqlCommand2.Parameters.AddWithValue("@Tel", customer.Tel);
					sqlCommand2.Parameters.AddWithValue("@Mobile", customer.Mobile);
					sqlCommand2.Parameters.AddWithValue("@Fax", customer.Fax);
					sqlCommand2.Parameters.AddWithValue("@Email", customer.Email);
					sqlCommand2.Parameters.AddWithValue("@Notes", customer.Notes);
					sqlCommand2.Parameters.AddWithValue("@IsDeleted", customer.IsDeleted);
					sqlCommand2.Parameters.AddWithValue("@MaxDepit", customer.MaxDepit);
					sqlCommand2.Parameters.AddWithValue("@Type", customer.Type);
					sqlCommand2.Parameters.AddWithValue("@TaxNo", customer.TaxNo);
					sqlCommand2.Parameters.AddWithValue("@AccountCode", customer.AccountCode);
					sqlCommand2.Parameters.AddWithValue("@Branch", customer.Branch);
					sqlCommand2.Parameters.AddWithValue("@IsCredit", customer.IsCredit);
					sqlCommand2.Parameters.AddWithValue("@PlotIdentification", customer.PlotIdentification);
					sqlCommand2.Parameters.AddWithValue("@BuildingNumber", customer.BuildingNumber);
					sqlCommand2.Parameters.AddWithValue("@StreetName", customer.StreetName);
					sqlCommand2.Parameters.AddWithValue("@AdditionalStreetName", customer.AdditionalStreetName);
					sqlCommand2.Parameters.AddWithValue("@District", customer.District);
					sqlCommand2.Parameters.AddWithValue("@PostalZone", customer.PostalZone);
					sqlCommand2.Parameters.AddWithValue("@CrNo", customer.CrNo);
					sqlCommand2.Parameters.AddWithValue("@Pricing", customer.Pricing);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void insertItemCategory(List<ListItemsCategory> categories)
		{
			new SqlCommand();
			ReceivedData.connString = ReceivedData.connString;
			using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
			try
			{
				sqlConnection.Open();
				foreach (ListItemsCategory category in categories)
				{
					int num;
					using (SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM ItemsCategory WHERE CategoryId=@CategoryId and Code= @Code ", sqlConnection))
					{
						sqlCommand.Parameters.AddWithValue("@Code", category.Code);
						sqlCommand.Parameters.AddWithValue("@CategoryId", category.CategoryId);
						num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
					}
					if (num > 0)
					{
						SqlCommand sqlCommand2;
						sqlCommand2 = new SqlCommand("\r\n                        delete from ItemsCategory WHERE Code = @Code", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@Code", category.Code);
						sqlCommand2.ExecuteNonQuery();
						sqlCommand2 = new SqlCommand("\r\n                        INSERT INTO ItemsCategory\r\n                            (CategoryId, Code, ParentCode, [type], [name], nameEN, color, printer, ShowInPOS, DispalyOrder, IS_Deleted, PrintAllItems, PrintItemsSeparately, BranchId, AllBranch, Id)\r\n                        VALUES\r\n                            (@CategoryId, @Code, @ParentCode, @Type, @Name, @NameEN, @Color, @Printer, @ShowInPOS, @DisplayOrder, @IS_Deleted, @PrintAllItems, @PrintItemsSeparately, @BranchId, @AllBranch, @Id)", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@CategoryId", category.CategoryId);
						sqlCommand2.Parameters.AddWithValue("@Code", category.Code);
						sqlCommand2.Parameters.AddWithValue("@ParentCode", category.ParentCode);
						sqlCommand2.Parameters.AddWithValue("@Type", category.Type);
						sqlCommand2.Parameters.AddWithValue("@Name", category.Name);
						sqlCommand2.Parameters.AddWithValue("@NameEN", category.NameEN);
						sqlCommand2.Parameters.AddWithValue("@Color", category.Color);
						sqlCommand2.Parameters.AddWithValue("@Printer", category.Printer);
						sqlCommand2.Parameters.AddWithValue("@ShowInPOS", category.ShowInPOS);
						sqlCommand2.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", category.IS_Deleted);
						sqlCommand2.Parameters.AddWithValue("@PrintAllItems", category.PrintAllItems);
						sqlCommand2.Parameters.AddWithValue("@PrintItemsSeparately", category.PrintItemsSeparately);
						sqlCommand2.Parameters.AddWithValue("@BranchId", category.BranchId);
						sqlCommand2.Parameters.AddWithValue("@AllBranch", category.AllBranch);
						sqlCommand2.Parameters.AddWithValue("@Id", category.Id);
						sqlCommand2.ExecuteNonQuery();
					}
					else
					{
						SqlCommand sqlCommand2;
						sqlCommand2 = new SqlCommand("\r\n                        INSERT INTO ItemsCategory\r\n                            (CategoryId, Code, ParentCode, [type], [name], nameEN, color, printer, ShowInPOS, DispalyOrder, IS_Deleted, PrintAllItems, PrintItemsSeparately, BranchId, AllBranch, Id)\r\n                        VALUES\r\n                            (@CategoryId, @Code, @ParentCode, @Type, @Name, @NameEN, @Color, @Printer, @ShowInPOS, @DisplayOrder, @IS_Deleted, @PrintAllItems, @PrintItemsSeparately, @BranchId, @AllBranch, @Id)", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@CategoryId", category.CategoryId);
						sqlCommand2.Parameters.AddWithValue("@Code", category.Code);
						sqlCommand2.Parameters.AddWithValue("@ParentCode", category.ParentCode);
						sqlCommand2.Parameters.AddWithValue("@Type", category.Type);
						sqlCommand2.Parameters.AddWithValue("@Name", category.Name);
						sqlCommand2.Parameters.AddWithValue("@NameEN", category.NameEN);
						sqlCommand2.Parameters.AddWithValue("@Color", category.Color);
						sqlCommand2.Parameters.AddWithValue("@Printer", category.Printer);
						sqlCommand2.Parameters.AddWithValue("@ShowInPOS", category.ShowInPOS);
						sqlCommand2.Parameters.AddWithValue("@DisplayOrder", category.DisplayOrder);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", category.IS_Deleted);
						sqlCommand2.Parameters.AddWithValue("@PrintAllItems", category.PrintAllItems);
						sqlCommand2.Parameters.AddWithValue("@PrintItemsSeparately", category.PrintItemsSeparately);
						sqlCommand2.Parameters.AddWithValue("@BranchId", category.BranchId);
						sqlCommand2.Parameters.AddWithValue("@AllBranch", category.AllBranch);
						sqlCommand2.Parameters.AddWithValue("@Id", category.Id);
						sqlCommand2.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void insertItemBarcode(List<listItemBarcode> ItemId)
		{
			ReceivedData.connString = ReceivedData.connString;
			using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
			try
			{
				sqlConnection.Open();
				foreach (listItemBarcode item in ItemId)
				{
					bool flag;
					using (SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Itembarcodes WHERE ItemId = @ItemId and Barcode=@Barcode", sqlConnection))
					{
						sqlCommand.Parameters.AddWithValue("@ItemId", item.ItemId);
						sqlCommand.Parameters.AddWithValue("@Barcode", item.Barcode);
						flag = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) != 0;
					}
					if (0 - (flag ? 1 : 0) > 0)
					{
						using SqlCommand sqlCommand2 = new SqlCommand("\r\n                        UPDATE Itembarcodes\r\n                        SET \r\n                            ItemId = @ItemId,\r\n                            Barcode = @Barcode                         \r\n                        WHERE ItemId = @ItemId and Barcode=@Barcode", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@ItemId", item.ItemId);
						sqlCommand2.Parameters.AddWithValue("@Barcode", item.Barcode);
						sqlCommand2.ExecuteNonQuery();
					}
					else
					{
						using SqlCommand sqlCommand3 = new SqlCommand("\r\n                        INSERT INTO Itembarcodes\r\n                            (ItemId,Barcode)\r\n                        VALUES\r\n                            (@ItemId, @Barcode)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@ItemId", item.ItemId);
						sqlCommand3.Parameters.AddWithValue("@Barcode", item.Barcode);
						sqlCommand3.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertUnits(List<ListUnit> units)
		{
			ReceivedData.connString = ReceivedData.connString;
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListUnit unit in units)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("SELECT COUNT(*) FROM units WHERE Id = @Id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Id", unit.Id);
					if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
					{
						SqlCommand sqlCommand2;
						sqlCommand2 = new SqlCommand("\r\n                    UPDATE units \r\n                    SET \r\n                        name = @name,\r\n                        defaultInv = @defaultInv,\r\n                        IS_Deleted = @IS_Deleted,\r\n                        UnitId = @UnitId,\r\n                        UnitCode = @UnitCode\r\n                    WHERE Id = @Id", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@name", unit.Name);
						sqlCommand2.Parameters.AddWithValue("@defaultInv", unit.DefaultInv);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", unit.IS_Deleted);
						sqlCommand2.Parameters.AddWithValue("@UnitId", unit.UnitId);
						sqlCommand2.Parameters.AddWithValue("@UnitCode", unit.UnitCode);
						sqlCommand2.Parameters.AddWithValue("@Id", unit.Id);
						sqlCommand2.ExecuteNonQuery();
					}
					else
					{
						SqlCommand sqlCommand3;
						sqlCommand3 = new SqlCommand("\r\n                    INSERT INTO units (name, defaultInv, IS_Deleted, UnitId, Id, UnitCode) \r\n                    VALUES (@name, @defaultInv, @IS_Deleted, @UnitId, @Id, @UnitCode)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@name", unit.Name);
						sqlCommand3.Parameters.AddWithValue("@defaultInv", unit.DefaultInv);
						sqlCommand3.Parameters.AddWithValue("@IS_Deleted", unit.IS_Deleted);
						sqlCommand3.Parameters.AddWithValue("@UnitId", unit.UnitId);
						sqlCommand3.Parameters.AddWithValue("@Id", unit.Id);
						sqlCommand3.Parameters.AddWithValue("@UnitCode", unit.UnitCode);
						sqlCommand3.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static byte[] ConvertBase64ToImage(string base64String)
		{
			if (!string.IsNullOrEmpty(base64String))
			{
				return Convert.FromBase64String(base64String);
			}
			return null;
		}

		public static void InsertAccounts_IndexCust(List<ListAccounCust> Account)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListAccounCust item in Account)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("IF EXISTS (SELECT 1 FROM Accounts_Index WHERE Code = @Code)\r\n                       BEGIN \r\n                            DELETE FROM Accounts_Index WHERE Code = @Code \r\n                            INSERT INTO Accounts_Index (Code, AName, Nature, Type, ParentCode, [Date], UserName, IValue, FinalAcc, Total_Debts, Total_Credits, Account_Value, Acc_branch, CostCenter, IsDeleted)\r\n                            VALUES (@Code, @AName, @Nature, @Type, @ParentCode, @Date, @UserName, @IValue, @FinalAcc, @Total_Debts, @Total_Credits, @Account_Value, @Acc_branch, @CostCenter, @IsDeleted)\r\n                       END \r\n                       ELSE \r\n                       BEGIN \r\n                           INSERT INTO Accounts_Index (Code, AName, Nature, Type, ParentCode, [Date], UserName, IValue, FinalAcc, Total_Debts, Total_Credits, Account_Value, Acc_branch, CostCenter, IsDeleted)\r\n                           VALUES (@Code, @AName, @Nature, @Type, @ParentCode, @Date, @UserName, @IValue, @FinalAcc, @Total_Debts, @Total_Credits, @Account_Value, @Acc_branch, @CostCenter, @IsDeleted)                       \r\n                       END\r\n                    ", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Code", item.Code);
					sqlCommand.Parameters.AddWithValue("@AName", item.AName);
					sqlCommand.Parameters.AddWithValue("@Nature", item.Nature);
					sqlCommand.Parameters.AddWithValue("@Type", item.AccountType);
					sqlCommand.Parameters.AddWithValue("@ParentCode", item.ParentCode);
					sqlCommand.Parameters.AddWithValue("@Date", item.AccDate);
					sqlCommand.Parameters.AddWithValue("@UserName", item.UserName);
					sqlCommand.Parameters.AddWithValue("@IValue", item.IValue);
					sqlCommand.Parameters.AddWithValue("@FinalAcc", item.FinalAcc);
					sqlCommand.Parameters.AddWithValue("@Total_Debts", item.Total_Debts);
					sqlCommand.Parameters.AddWithValue("@Total_Credits", item.Total_Credits);
					sqlCommand.Parameters.AddWithValue("@Account_Value", item.Account_Value);
					sqlCommand.Parameters.AddWithValue("@Acc_branch", item.Acc_branch);
					sqlCommand.Parameters.AddWithValue("@CostCenter", item.CostCenter);
					sqlCommand.Parameters.AddWithValue("@IsDeleted", item.IsDeleted);
					sqlCommand.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (ReceivedData.conn != null && ReceivedData.conn.State == ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
		}

		public static void InsertAccounts_Index(List<ListAccountIndex> Account)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListAccountIndex item in Account)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Accounts_Index WHERE Code =@Code", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@Code", item.Code);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("\r\n                    INSERT INTO Accounts_Index (Code, AName, Nature, Type, ParentCode, [Date], UserName, IValue, FinalAcc, Total_Debts, Total_Credits, Account_Value, Acc_branch, CostCenter, IsDeleted)\r\n                    VALUES (@Code, @AName, @Nature, @Type, @ParentCode, @Date, @UserName, @IValue, @FinalAcc, @Total_Debts, @Total_Credits, @Account_Value, @Acc_branch, @CostCenter, @IsDeleted)\r\n                    ", sqlConnection) : new SqlCommand("\r\n                    UPDATE Accounts_Index \r\n                    SET AName = @AName, \r\n                        Nature = @Nature, \r\n                        Type = @Type, \r\n                        ParentCode = @ParentCode, \r\n                        [Date] = @Date, \r\n                        UserName = @UserName, \r\n                        IValue = @IValue,\r\n                        Total_Debts = @Total_Debts,\r\n                        Total_Credits = @Total_Credits,\r\n                        Account_Value = @Account_Value,\r\n                        Acc_branch = @Acc_branch,\r\n                        CostCenter = @CostCenter,\r\n                        IsDeleted = @IsDeleted\r\n                    WHERE Code = @Code", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@Code", item.Code);
					sqlCommand2.Parameters.AddWithValue("@AName", item.AName);
					sqlCommand2.Parameters.AddWithValue("@Nature", item.Nature);
					sqlCommand2.Parameters.AddWithValue("@Type", item.AccountType);
					sqlCommand2.Parameters.AddWithValue("@ParentCode", item.ParentCode);
					sqlCommand2.Parameters.AddWithValue("@Date", item.AccDate);
					sqlCommand2.Parameters.AddWithValue("@UserName", item.UserName);
					sqlCommand2.Parameters.AddWithValue("@IValue", item.IValue);
					sqlCommand2.Parameters.AddWithValue("@FinalAcc", item.FinalAcc);
					sqlCommand2.Parameters.AddWithValue("@Total_Debts", item.Total_Debts);
					sqlCommand2.Parameters.AddWithValue("@Total_Credits", item.Total_Credits);
					sqlCommand2.Parameters.AddWithValue("@Account_Value", item.Account_Value);
					sqlCommand2.Parameters.AddWithValue("@Acc_branch", item.Acc_branch);
					sqlCommand2.Parameters.AddWithValue("@CostCenter", item.CostCenter);
					sqlCommand2.Parameters.AddWithValue("@IsDeleted", item.IsDeleted);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (ReceivedData.conn != null && ReceivedData.conn.State == ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
		}

		public static void InsertEmployees(List<ListEmployee> employees)
		{
			try
			{
				new SqlCommand();
				using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
				{
					sqlConnection.Open();
					foreach (ListEmployee employee in employees)
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Employees WHERE id=@id", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@id", employee.Id);
						SqlCommand sqlCommand2;
						sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("SET IDENTITY_INSERT [dbo].[Employees] ON\r\n                   INSERT INTO Employees \r\n                (id,name, manag, dep, state, job, branch, birth_date, insurance_no, work_date, marital_state, nationality, sex, tel, mobile, email, address, notes, salary_basic, salary_add, salary_other, house, food, travel, medical, IS_Deleted, AccCode, CardNo, BankNo, BankName) \r\n                VALUES \r\n                (@id,@name, @manag, @dep, @state, @job, @branch, @birth_date, @insurance_no, @work_date, @marital_state, @nationality, @sex, @tel, @mobile, @email, @address, @notes, @salary_basic, @salary_add, @salary_other, @house, @food, @travel, @medical, @IS_Deleted, @AccCode, @CardNo, @BankNo, @BankName)", sqlConnection) : new SqlCommand("UPDATE Employees SET \r\n                name = @name, \r\n                manag = @manag, \r\n                dep = @dep, \r\n                state = @state, \r\n                job = @job, \r\n                branch = @branch, \r\n                birth_date = @birth_date, \r\n                insurance_no = @insurance_no, \r\n                work_date = @work_date, \r\n                marital_state = @marital_state, \r\n                nationality = @nationality, \r\n                sex = @sex, \r\n                tel = @tel, \r\n                mobile = @mobile, \r\n                email = @email, \r\n                address = @address, \r\n                notes = @notes,               \r\n                salary_basic = @salary_basic, \r\n                salary_add = @salary_add, \r\n                salary_other = @salary_other, \r\n                house = @house, \r\n                food = @food, \r\n                travel = @travel, \r\n                medical = @medical, \r\n                IS_Deleted = @IS_Deleted, \r\n                AccCode = @AccCode, \r\n                CardNo = @CardNo, \r\n                BankNo = @BankNo, \r\n                BankName = @BankName\r\n                WHERE id = @id", sqlConnection));
						sqlCommand2.Parameters.AddWithValue("@id", employee.Id);
						sqlCommand2.Parameters.AddWithValue("@name", employee.Name);
						sqlCommand2.Parameters.AddWithValue("@manag", employee.Manag);
						sqlCommand2.Parameters.AddWithValue("@dep", employee.Dep);
						sqlCommand2.Parameters.AddWithValue("@state", employee.State);
						sqlCommand2.Parameters.AddWithValue("@job", employee.Job);
						sqlCommand2.Parameters.AddWithValue("@branch", employee.Branch);
						sqlCommand2.Parameters.AddWithValue("@birth_date", employee.BirthDate);
						sqlCommand2.Parameters.AddWithValue("@insurance_no", employee.InsuranceNo);
						sqlCommand2.Parameters.AddWithValue("@work_date", employee.WorkDate);
						sqlCommand2.Parameters.AddWithValue("@marital_state", employee.MaritalState);
						sqlCommand2.Parameters.AddWithValue("@nationality", employee.Nationality);
						sqlCommand2.Parameters.AddWithValue("@sex", employee.Sex);
						sqlCommand2.Parameters.AddWithValue("@tel", employee.Tel);
						sqlCommand2.Parameters.AddWithValue("@mobile", employee.Mobile);
						sqlCommand2.Parameters.AddWithValue("@email", employee.Email);
						sqlCommand2.Parameters.AddWithValue("@address", employee.Address);
						sqlCommand2.Parameters.AddWithValue("@notes", employee.Notes);
						sqlCommand2.Parameters.AddWithValue("@salary_basic", employee.SalaryBasic);
						sqlCommand2.Parameters.AddWithValue("@salary_add", employee.SalaryAdd);
						sqlCommand2.Parameters.AddWithValue("@salary_other", employee.SalaryOther);
						sqlCommand2.Parameters.AddWithValue("@house", employee.House);
						sqlCommand2.Parameters.AddWithValue("@food", employee.Food);
						sqlCommand2.Parameters.AddWithValue("@travel", employee.Travel);
						sqlCommand2.Parameters.AddWithValue("@medical", employee.Medical);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", employee.IsDeleted);
						sqlCommand2.Parameters.AddWithValue("@AccCode", employee.AccCode);
						sqlCommand2.Parameters.AddWithValue("@CardNo", employee.CardNo);
						sqlCommand2.Parameters.AddWithValue("@BankNo", employee.BankNo);
						sqlCommand2.Parameters.AddWithValue("@BankName", employee.BankName);
						sqlCommand2.ExecuteNonQuery();
					}
				}
				if (ReceivedData.conn.State == ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InserUserPermissions(List<ListUserPermission> permissionsList)
		{
			try
			{
				new SqlCommand();
				using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
				{
					sqlConnection.Open();
					foreach (ListUserPermission permissions in permissionsList)
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("SELECT COUNT(*) FROM User_Permissions WHERE user_id=@user_id AND Form_id=@Form_id", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@user_id", permissions.UserId);
						sqlCommand.Parameters.AddWithValue("@Form_id", permissions.FormId);
						SqlCommand sqlCommand2;
						sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("\r\n            INSERT INTO User_Permissions \r\n                (user_id, Form_id, IS_New, IS_Save, IS_Delete, IS_Search, IS_Print, IS_Edit)\r\n            VALUES \r\n                (@user_id, @Form_id, @IS_New, @IS_Save, @IS_Delete, @IS_Search, @IS_Print, @IS_Edit)", sqlConnection) : new SqlCommand("\r\n            UPDATE User_Permissions \r\n            SET \r\n                IS_New = @IS_New, \r\n                IS_Save = @IS_Save, \r\n                IS_Delete = @IS_Delete, \r\n                IS_Search = @IS_Search, \r\n                IS_Print = @IS_Print, \r\n                IS_Edit = @IS_Edit\r\n            WHERE \r\n                user_id = @user_id \r\n                AND Form_id = @Form_id", sqlConnection));
						sqlCommand2.Parameters.AddWithValue("@user_id", permissions.UserId);
						sqlCommand2.Parameters.AddWithValue("@Form_id", permissions.FormId);
						sqlCommand2.Parameters.AddWithValue("@IS_New", permissions.IsNew);
						sqlCommand2.Parameters.AddWithValue("@IS_Save", permissions.IsSave);
						sqlCommand2.Parameters.AddWithValue("@IS_Delete", permissions.IsDelete);
						sqlCommand2.Parameters.AddWithValue("@IS_Search", permissions.IsSearch);
						sqlCommand2.Parameters.AddWithValue("@IS_Print", permissions.IsPrint);
						sqlCommand2.Parameters.AddWithValue("@IS_Edit", permissions.IsEdit);
						sqlCommand2.ExecuteNonQuery();
					}
				}
				if (ReceivedData.conn.State == ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertOperPrimisson(List<ListOperationPermission> permissionsList)
		{
			try
			{
				new SqlCommand();
				using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
				{
					sqlConnection.Open();
					foreach (ListOperationPermission permissions in permissionsList)
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("delete from OperationPermission where OperNo>7 and  emp=" + Conversions.ToString(permissions.emp), sqlConnection);
						sqlCommand.Parameters.AddWithValue("@emp", permissions.emp);
						sqlCommand.ExecuteNonQuery();
						sqlCommand = new SqlCommand("update  OperationPermission set Activated= 0  where  emp=" + Conversions.ToString(permissions.emp), sqlConnection);
						sqlCommand.Parameters.AddWithValue("@emp", permissions.emp);
						sqlCommand.ExecuteNonQuery();
						sqlCommand = new SqlCommand("insert into OperationPermission(emp,OperNo,pwd,lastChanged,IS_Deleted,OperVal,Activated)values(@emp,@OperNo,@pwd,@lastChanged,@IS_Deleted,@OperVal,@Activated)", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@id", permissions.id);
						sqlCommand.Parameters.AddWithValue("@emp", permissions.emp);
						sqlCommand.Parameters.AddWithValue("@Pwd", permissions.Pwd);
						sqlCommand.Parameters.AddWithValue("@OperNo", permissions.OperNo);
						sqlCommand.Parameters.AddWithValue("@OperVal", permissions.OperVal);
						sqlCommand.Parameters.AddWithValue("@lastChanged", permissions.lastChanged);
						sqlCommand.Parameters.AddWithValue("@IS_Deleted", permissions.IS_Deleted);
						sqlCommand.Parameters.AddWithValue("@Activated", permissions.Activated);
						sqlCommand.ExecuteNonQuery();
					}
				}
				if (ReceivedData.conn.State == ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertUsers(List<ListUsers> usersList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListUsers users in usersList)
				{
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Users WHERE id = @id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", users.Id);
					sqlCommand = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("SET IDENTITY_INSERT [dbo].[Users] ON\r\n            INSERT INTO Users \r\n                (id, emp, username, pwd, IS_Deleted, SecureCode, LoginSecode)\r\n            VALUES \r\n                (@id, @emp, @username, @pwd, @IS_Deleted, @SecureCode, @LoginSecode)", sqlConnection) : new SqlCommand("\r\n            UPDATE Users \r\n            SET \r\n                emp = @emp, \r\n                username = @username, \r\n                pwd = @pwd, \r\n                IS_Deleted = @IS_Deleted, \r\n                SecureCode = @SecureCode, \r\n                LoginSecode = @LoginSecode\r\n            WHERE \r\n                id = @id", sqlConnection));
					sqlCommand.Parameters.AddWithValue("@id", users.Id);
					sqlCommand.Parameters.AddWithValue("@emp", users.Emp);
					sqlCommand.Parameters.AddWithValue("@username", users.Username);
					sqlCommand.Parameters.AddWithValue("@pwd", users.Pwd);
					sqlCommand.Parameters.AddWithValue("@IS_Deleted", users.IsDeleted);
					sqlCommand.Parameters.AddWithValue("@SecureCode", users.SecureCode);
					sqlCommand.Parameters.AddWithValue("@LoginSecode", users.LoginSecode);
					sqlCommand.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			if (ReceivedData.conn.State == ConnectionState.Open)
			{
				ReceivedData.conn.Close();
			}
		}

		private static void insertEmpBranch(List<ListEmpBranch> empBranch)
		{
			try
			{
				new SqlCommand();
				using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
				{
					sqlConnection.Open();
					foreach (ListEmpBranch item in empBranch)
					{
						if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(new SqlCommand(("select COUNT(*) FROM EmpBranches WHERE Emp =" + Conversions.ToString(item.Emp) + " and Branch=" + Conversions.ToString(item.Branch)) ?? "", sqlConnection).ExecuteScalar())) > 0)
						{
							new SqlCommand(("Delete from EmpBranches where emp=" + Conversions.ToString(item.Emp) + " and branch=" + Conversions.ToString(item.Branch)) ?? "", sqlConnection).ExecuteNonQuery();
							SqlCommand sqlCommand;
							sqlCommand = new SqlCommand("insert into EmpBranches(emp,Branch)values(@emp,@Branch)", sqlConnection);
							sqlCommand.Parameters.AddWithValue("@emp", item.Emp);
							sqlCommand.Parameters.AddWithValue("@branch", item.Branch);
							sqlCommand.ExecuteNonQuery();
						}
						else
						{
							SqlCommand sqlCommand;
							sqlCommand = new SqlCommand("insert into EmpBranches(emp,Branch)values(@emp,@Branch)", sqlConnection);
							sqlCommand.Parameters.AddWithValue("@emp", item.Emp);
							sqlCommand.Parameters.AddWithValue("@branch", item.Branch);
							sqlCommand.ExecuteNonQuery();
						}
					}
				}
				if (ReceivedData.conn.State != ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertInv(List<ListInvoice> invoices)
		{
			try
			{
				new SqlCommand();
				using (SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString))
				{
					sqlConnection.Open();
					foreach (ListInvoice invoice in invoices)
					{
						if (!invoice.InvGlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
						{
							SqlCommand sqlCommand;
							sqlCommand = new SqlCommand("select COUNT(*) FROM Inv WHERE InvGlobalID = @InvGlobalID", sqlConnection);
							sqlCommand.Parameters.AddWithValue("@InvGlobalID", invoice.InvGlobalID);
							sqlCommand = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("SET IDENTITY_INSERT [dbo].[Inv] ON  INSERT Into Inv (InvGlobalID, proc_id, proc_type, id, [date], inv_type, OrderType, safe, stock, cust_id, sales_emp, InvTotal, tot_purch, AdditionsTot, Insurance, tot_net, EntryID, purch_rest_id, Reff_No, Reff_date, branch, IS_Buy, minus, paid, salesman, tax, ExtraVAT, pay_type, bank, cash, visa, notes, IS_Deleted, Sync, InvProfit, InvoiceStatus, AdditionalCost, PriceIncVAT, PaymentStatus, IssueDate, InvCombinedId, ItemsDiscount, InvCost, FreeVATSales, InvSum, VATPercent, CurrencyCode, CloudID, CashCustomerName, CashCustomerMobile, QRCode, InvoiceHash, UUID, ZatcaSent, TotalWithholdingTax, TableNo, Balance_previews)\r\n                    VALUES (@InvGlobalID, @proc_id, @proc_type, @id, @date, @inv_type, @OrderType, @safe, @stock, @cust_id, @sales_emp, @InvTotal, @tot_purch, @AdditionsTot, @Insurance, @tot_net, @EntryID, @purch_rest_id, @Reff_No, @Reff_date, @branch, @IS_Buy, @minus, @paid, @salesman, @tax, @ExtraVAT, @pay_type, @bank, @cash, @visa, @notes, @IS_Deleted, @Sync, @InvProfit, @InvoiceStatus, @AdditionalCost, @PriceIncVAT, @PaymentStatus, @IssueDate, @InvCombinedId, @ItemsDiscount, @InvCost, @FreeVATSales, @InvSum, @VATPercent, @CurrencyCode, @CloudID, @CashCustomerName, @CashCustomerMobile, @QRCode, @InvoiceHash, @UUID, @ZatcaSent, @TotalWithholdingTax, @TableNo, @Balance_previews)", sqlConnection) : new SqlCommand("             \r\n                    UPDATE Inv SET proc_type = @proc_type, id = @id, date = @date, inv_type =@inv_type, OrderType = @OrderType, safe = @safe, stock = @stock, cust_id = @cust_id, sales_emp = @sales_emp, InvTotal = @InvTotal, tot_purch = @tot_purch, AdditionsTot = @AdditionsTot, Insurance = @Insurance, tot_net = @tot_net, EntryID =@EntryID, purch_rest_id = @purch_rest_id, Reff_No = @Reff_No, Reff_date = @Reff_date, branch = @branch, IS_Buy = @IS_Buy, minus = @minus, paid = @paid,\r\n                    salesman = @salesman, tax = @tax, ExtraVAT = @ExtraVAT, pay_type = @pay_type, bank = @bank, cash = @cash, visa = @visa, notes = @notes, IS_Deleted = @IS_Deleted, Sync = @Sync, InvProfit = @InvProfit, InvoiceStatus = @InvoiceStatus, AdditionalCost = @AdditionalCost, PriceIncVAT = @PriceIncVAT, PaymentStatus = @PaymentStatus, IssueDate = @IssueDate, InvCombinedId = @InvCombinedId, ItemsDiscount = @ItemsDiscount, InvCost = @InvCost, FreeVATSales = @FreeVATSales, InvSum = @InvSum, VATPercent = @VATPercent,\r\n                    CurrencyCode = @CurrencyCode, CloudID = @CloudID, CashCustomerName = @CashCustomerName, CashCustomerMobile = @CashCustomerMobile, QRCode = @QRCode, InvoiceHash = @InvoiceHash, UUID = @UUID, ZatcaSent = @ZatcaSent, TotalWithholdingTax = @TotalWithholdingTax, TableNo = @TableNo, Balance_previews = @Balance_previews where InvGlobalID=@InvGlobalID", sqlConnection));
							sqlCommand.Parameters.AddWithValue("@InvGlobalID", invoice.InvGlobalID);
							sqlCommand.Parameters.AddWithValue("@proc_id", invoice.ProcID);
							sqlCommand.Parameters.AddWithValue("@proc_type", invoice.ProcType);
							sqlCommand.Parameters.AddWithValue("@id", invoice.ID);
							sqlCommand.Parameters.AddWithValue("@date", invoice.DateInv);
							sqlCommand.Parameters.AddWithValue("@inv_type", invoice.InvType);
							sqlCommand.Parameters.AddWithValue("@OrderType", invoice.OrderType);
							sqlCommand.Parameters.AddWithValue("@safe", invoice.Safe);
							sqlCommand.Parameters.AddWithValue("@stock", invoice.Stock);
							sqlCommand.Parameters.AddWithValue("@cust_id", invoice.CustID);
							sqlCommand.Parameters.AddWithValue("@sales_emp", invoice.SalesEmp);
							sqlCommand.Parameters.AddWithValue("@InvTotal", invoice.InvTotal);
							sqlCommand.Parameters.AddWithValue("@tot_purch", invoice.TotPurch);
							sqlCommand.Parameters.AddWithValue("@AdditionsTot", invoice.AdditionsTot);
							sqlCommand.Parameters.AddWithValue("@Insurance", invoice.Insurance);
							sqlCommand.Parameters.AddWithValue("@tot_net", invoice.TotNet);
							sqlCommand.Parameters.AddWithValue("@EntryID", invoice.EntryID);
							sqlCommand.Parameters.AddWithValue("@purch_rest_id", invoice.PurchRestID);
							sqlCommand.Parameters.AddWithValue("@Reff_No", invoice.ReffNo);
							sqlCommand.Parameters.AddWithValue("@Reff_date", invoice.ReffDate);
							sqlCommand.Parameters.AddWithValue("@branch", invoice.Branch);
							sqlCommand.Parameters.AddWithValue("@IS_Buy", invoice.IS_Buy);
							sqlCommand.Parameters.AddWithValue("@minus", invoice.Minus);
							sqlCommand.Parameters.AddWithValue("@paid", invoice.Paid);
							sqlCommand.Parameters.AddWithValue("@salesman", invoice.Salesman);
							sqlCommand.Parameters.AddWithValue("@tax", invoice.Tax);
							sqlCommand.Parameters.AddWithValue("@ExtraVAT", invoice.ExtraVAT);
							sqlCommand.Parameters.AddWithValue("@pay_type", invoice.PayType);
							sqlCommand.Parameters.AddWithValue("@bank", invoice.Bank);
							sqlCommand.Parameters.AddWithValue("@cash", invoice.Cash);
							sqlCommand.Parameters.AddWithValue("@visa", invoice.Visa);
							sqlCommand.Parameters.AddWithValue("@notes", invoice.Notes);
							sqlCommand.Parameters.AddWithValue("@IS_Deleted", invoice.IS_Deleted);
							sqlCommand.Parameters.AddWithValue("@Sync", invoice.Sync);
							sqlCommand.Parameters.AddWithValue("@InvProfit", invoice.InvProfit);
							sqlCommand.Parameters.AddWithValue("@InvoiceStatus", invoice.InvoiceStatus);
							sqlCommand.Parameters.AddWithValue("@AdditionalCost", invoice.AdditionalCost);
							sqlCommand.Parameters.AddWithValue("@PriceIncVAT", invoice.PriceIncVAT);
							sqlCommand.Parameters.AddWithValue("@PaymentStatus", invoice.PaymentStatus);
							sqlCommand.Parameters.AddWithValue("@IssueDate", invoice.IssueDate);
							sqlCommand.Parameters.AddWithValue("@InvCombinedId", invoice.InvCombinedID);
							sqlCommand.Parameters.AddWithValue("@ItemsDiscount", invoice.ItemsDiscount);
							sqlCommand.Parameters.AddWithValue("@InvCost", invoice.InvCost);
							sqlCommand.Parameters.AddWithValue("@FreeVATSales", invoice.FreeVATSales);
							sqlCommand.Parameters.AddWithValue("@InvSum", invoice.InvSum);
							sqlCommand.Parameters.AddWithValue("@VATPercent", invoice.VATPercent);
							sqlCommand.Parameters.AddWithValue("@CurrencyCode", invoice.CurrencyCode);
							sqlCommand.Parameters.AddWithValue("@CloudID", invoice.CloudID);
							sqlCommand.Parameters.AddWithValue("@CashCustomerName", invoice.CashCustomerName);
							sqlCommand.Parameters.AddWithValue("@CashCustomerMobile", invoice.CashCustomerMobile);
							sqlCommand.Parameters.AddWithValue("@QRCode", invoice.QRCode);
							sqlCommand.Parameters.AddWithValue("@InvoiceHash", invoice.InvoiceHash);
							sqlCommand.Parameters.AddWithValue("@UUID", invoice.UUID);
							sqlCommand.Parameters.AddWithValue("@ZatcaSent", invoice.ZatcaSent);
							sqlCommand.Parameters.AddWithValue("@TotalWithholdingTax", invoice.TotalWithholdingTax);
							sqlCommand.Parameters.AddWithValue("@TableNo", invoice.TableNo);
							sqlCommand.Parameters.AddWithValue("@Balance_previews", invoice.BalancePreviews);
							sqlCommand.ExecuteNonQuery();
						}
					}
				}
				if (ReceivedData.conn.State != ConnectionState.Open)
				{
					ReceivedData.conn.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertInvSub(List<listInvSub> invSubList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (listInvSub invSub in invSubList)
				{
					if (!invSub.InvGlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("delete FROM Inv_Sub WHERE InvGlobalID =N'" + invSub.InvGlobalID + "'", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@InvGlobalID", invSub.InvGlobalID);
						sqlCommand.ExecuteNonQuery();
					}
				}
				foreach (listInvSub invSub2 in invSubList)
				{
					if (!invSub2.InvGlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("\r\n                        INSERT INTO Inv_Sub \r\n                        (InvGlobalID, proc_id, proc_type, store, ItemId, unit, UnitEquality, val, val1, exchange_price, expire_date, taxperc, taxval, discount, notes, Description, ProductId, AvrgCost, CurrentQnty, ItemAddedCost, ItemPriceWithoutVAT, WithholdingTax, WithholdingTaxPerc, ItemCostCenter, ItemAdditionalTax, ItemAdditionalTaxPerc)\r\n                        VALUES \r\n                        (@InvGlobalID, @proc_id, @proc_type, @store, @ItemId, @unit, @UnitEquality, @val, @val1, @exchange_price, @expire_date, @taxperc, @taxval, @discount, @notes, @Description, @ProductId, @AvrgCost, @CurrentQnty, @ItemAddedCost, @ItemPriceWithoutVAT, @WithholdingTax, @WithholdingTaxPerc, @ItemCostCenter, @ItemAdditionalTax, @ItemAdditionalTaxPerc)", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@InvGlobalID", invSub2.InvGlobalID);
						sqlCommand.Parameters.AddWithValue("@ItemId", invSub2.ItemId);
						int? procID;
						sqlCommand.Parameters.AddWithValue("@proc_id", RuntimeHelpers.GetObjectValue(((int?)(procID = invSub2.ProcID)).HasValue ? ((object)procID.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@proc_type", RuntimeHelpers.GetObjectValue(((int?)(procID = invSub2.ProcType)).HasValue ? ((object)procID.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@store", RuntimeHelpers.GetObjectValue(((int?)(procID = invSub2.Store)).HasValue ? ((object)procID.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@unit", RuntimeHelpers.GetObjectValue(((int?)(procID = invSub2.Unit)).HasValue ? ((object)procID.GetValueOrDefault()) : DBNull.Value));
						double? unitEquality;
						sqlCommand.Parameters.AddWithValue("@UnitEquality", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.UnitEquality)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@val", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.Val)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@val1", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.Val1)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@exchange_price", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.ExchangePrice)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						DateTime? expireDate;
						sqlCommand.Parameters.AddWithValue("@expire_date", RuntimeHelpers.GetObjectValue(((DateTime?)(expireDate = invSub2.ExpireDate)).HasValue ? ((object)expireDate.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@taxperc", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.TaxPerc)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@taxval", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.TaxVal)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@discount", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.Discount)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@notes", RuntimeHelpers.GetObjectValue(((object)invSub2.Notes) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@Description", RuntimeHelpers.GetObjectValue(((object)invSub2.Description) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@ProductId", RuntimeHelpers.GetObjectValue(((int?)(procID = invSub2.ProductId)).HasValue ? ((object)procID.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@AvrgCost", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.AvrgCost)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@CurrentQnty", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.CurrentQnty)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@ItemAddedCost", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.ItemAddedCost)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@ItemPriceWithoutVAT", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.ItemPriceWithoutVAT)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@WithholdingTax", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.WithholdingTax)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@WithholdingTaxPerc", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.WithholdingTaxPerc)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@ItemCostCenter", RuntimeHelpers.GetObjectValue(((object)invSub2.ItemCostCenter) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@ItemAdditionalTax", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.ItemAdditionalTax)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.Parameters.AddWithValue("@ItemAdditionalTaxPerc", RuntimeHelpers.GetObjectValue(((double?)(unitEquality = invSub2.ItemAdditionalTaxPerc)).HasValue ? ((object)unitEquality.GetValueOrDefault()) : DBNull.Value));
						sqlCommand.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertEntrySub(List<ListEntrySub> entrySubList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListEntrySub entrySub in entrySubList)
				{
					if (!entrySub.EntryGlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						new SqlCommand("DELETE FROM Entry_sub WHERE EntryGlobalID =N'" + entrySub.EntryGlobalID + "'", sqlConnection).ExecuteNonQuery();
					}
				}
				foreach (ListEntrySub entrySub2 in entrySubList)
				{
					if (!entrySub2.EntryGlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("\r\n                    INSERT INTO Entry_sub \r\n                    (EntryGlobalID, res_id, dept, credit, acc_no, CCcode, notes, branch)\r\n                    VALUES \r\n                    (@EntryGlobalID, @res_id, @dept, @credit, @acc_no, @CCcode, @notes, @branch)", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@EntryGlobalID", entrySub2.EntryGlobalID);
						sqlCommand.Parameters.AddWithValue("@res_id", entrySub2.ResID);
						sqlCommand.Parameters.AddWithValue("@dept", entrySub2.Dept);
						sqlCommand.Parameters.AddWithValue("@credit", entrySub2.Credit);
						sqlCommand.Parameters.AddWithValue("@acc_no", entrySub2.AccNo);
						sqlCommand.Parameters.AddWithValue("@CCcode", entrySub2.CCCode);
						sqlCommand.Parameters.AddWithValue("@notes", entrySub2.Notes);
						sqlCommand.Parameters.AddWithValue("@branch", entrySub2.Branch);
						sqlCommand.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static void InsertOrUpdateEntry(List<ListEntry> entryList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListEntry entry in entryList)
				{
					if (!entry.GlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("select Count(*) from Entry WHERE GlobalID = @GlobalID ", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@GlobalID", entry.GlobalID);
						sqlCommand = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("\r\n                    INSERT INTO Entry \r\n                    (GlobalID, id, date, doc_no, type, state, notes, branch, IS_Deleted, EmpID, Sync, IsVAT)\r\n                    VALUES \r\n                    (@GlobalID, @id, @date, @doc_no, @type, @state, @notes, @branch, @IS_Deleted, @EmpID, @Sync, @IsVAT)", sqlConnection) : new SqlCommand("Update Entry set  id=@id, date=@date, doc_no=doc_no, type=@type, state=@state,\r\n                                              notes=@notes, branch=@branch, IS_Deleted=@IS_Deleted, EmpID=@EmpID, Sync=@Sync, IsVAT=@IsVAT\r\n                                              where GlobalID=@GlobalID ", sqlConnection));
						sqlCommand.Parameters.AddWithValue("@GlobalID", entry.GlobalID);
						sqlCommand.Parameters.AddWithValue("@id", entry.ID);
						sqlCommand.Parameters.AddWithValue("@date", entry.EntryDate);
						sqlCommand.Parameters.AddWithValue("@doc_no", RuntimeHelpers.GetObjectValue(((object)entry.DocNo) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@type", RuntimeHelpers.GetObjectValue(((object)entry.Type) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@state", entry.State);
						sqlCommand.Parameters.AddWithValue("@notes", RuntimeHelpers.GetObjectValue(((object)entry.Notes) ?? ((object)DBNull.Value)));
						sqlCommand.Parameters.AddWithValue("@branch", entry.Branch);
						sqlCommand.Parameters.AddWithValue("@IS_Deleted", entry.IS_Deleted);
						sqlCommand.Parameters.AddWithValue("@EmpID", entry.EmpID);
						sqlCommand.Parameters.AddWithValue("@Sync", entry.Sync);
						sqlCommand.Parameters.AddWithValue("@IsVAT", entry.IsVAT);
						sqlCommand.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOffer(List<ListOffers> offersList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListOffers offers in offersList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Offer WHERE OfferID= @OfferID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@OfferID", offers.OfferID);
					int num;
					num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
					string cmdText;
					cmdText = "INSERT INTO [dbo].[Offer] \r\n                        ([OfferID], [OfferType], [OfferStartDate], [OfferExpire], [OfferName], [OfferDate], [OfferAccount], [InvoiceID], [OfferValue], [OfferPercentage], [OfferValueTarget], [OfferQntyTarget], [EmpId], [IsDeleted]) \r\n                        VALUES (@OfferID, @OfferType, @OfferStartDate, @OfferExpire, @OfferName, @OfferDate, @OfferAccount, @InvoiceID, @OfferValue, @OfferPercentage, @OfferValueTarget, @OfferQntyTarget, @EmpId, @IsDeleted)";
					SqlCommand sqlCommand2;
					if (num > 0)
					{
						new SqlCommand("delete from Offer where OfferID=" + Conversions.ToString(offers.OfferID) + " ", sqlConnection).ExecuteNonQuery();
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					else
					{
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					sqlCommand2.Parameters.AddWithValue("@OfferID", offers.OfferID);
					int? offerType;
					sqlCommand2.Parameters.AddWithValue("@OfferType", RuntimeHelpers.GetObjectValue(((int?)(offerType = offers.OfferType)).HasValue ? ((object)offerType.GetValueOrDefault()) : DBNull.Value));
					DateTime? offerStartDate;
					sqlCommand2.Parameters.AddWithValue("@OfferStartDate", RuntimeHelpers.GetObjectValue(((DateTime?)(offerStartDate = offers.OfferStartDate)).HasValue ? ((object)offerStartDate.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferExpire", RuntimeHelpers.GetObjectValue(((DateTime?)(offerStartDate = offers.OfferExpire)).HasValue ? ((object)offerStartDate.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferName", RuntimeHelpers.GetObjectValue(((object)offers.OfferName) ?? ((object)DBNull.Value)));
					sqlCommand2.Parameters.AddWithValue("@OfferDate", RuntimeHelpers.GetObjectValue(((DateTime?)(offerStartDate = offers.OfferDate)).HasValue ? ((object)offerStartDate.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferAccount", RuntimeHelpers.GetObjectValue(((object)offers.OfferAccount) ?? ((object)DBNull.Value)));
					sqlCommand2.Parameters.AddWithValue("@InvoiceID", RuntimeHelpers.GetObjectValue(((int?)(offerType = offers.InvoiceID)).HasValue ? ((object)offerType.GetValueOrDefault()) : DBNull.Value));
					double? offerValue;
					sqlCommand2.Parameters.AddWithValue("@OfferValue", RuntimeHelpers.GetObjectValue(((double?)(offerValue = offers.OfferValue)).HasValue ? ((object)offerValue.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferPercentage", RuntimeHelpers.GetObjectValue(((double?)(offerValue = offers.OfferPercentage)).HasValue ? ((object)offerValue.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferValueTarget", RuntimeHelpers.GetObjectValue(((double?)(offerValue = offers.OfferValueTarget)).HasValue ? ((object)offerValue.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferQntyTarget", RuntimeHelpers.GetObjectValue(((double?)(offerValue = offers.OfferQntyTarget)).HasValue ? ((object)offerValue.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@EmpId", RuntimeHelpers.GetObjectValue(((int?)(offerType = offers.EmpId)).HasValue ? ((object)offerType.GetValueOrDefault()) : DBNull.Value));
					bool? isDeleted;
					sqlCommand2.Parameters.AddWithValue("@IsDeleted", RuntimeHelpers.GetObjectValue(((bool?)(isDeleted = offers.IsDeleted)).HasValue ? ((object)(isDeleted == true)) : DBNull.Value));
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOfferClinet(List<ListOfferForClient> offersList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListOfferForClient offers in offersList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM OfferForClient WHERE [OfferId] = @OfferId AND [ClientID] = @ClientID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@OfferId", offers.OfferId);
					sqlCommand.Parameters.AddWithValue("@ClientID", offers.ClientID);
					int num;
					num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
					string cmdText;
					cmdText = "INSERT INTO OfferForClient\r\n                        ([OfferId], [ClientID], [CatatogryID], [ItemID], [IsForAllItems], [unit], [OfferTargetQnty], [OfferItemValue], [OfferItemPercentage]) \r\n                        VALUES (@OfferId, @ClientID, @CatatogryID, @ItemID, @IsForAllItems, @Unit, @OfferTargetQnty, @OfferItemValue, @OfferItemPercentage)";
					SqlCommand sqlCommand2;
					int? offerId;
					if (num > 0)
					{
						new SqlCommand("delete from OfferForClient where offerid=" + (((int?)(offerId = offers.OfferId)).HasValue ? Conversions.ToString(offerId.GetValueOrDefault()) : null) + " ", sqlConnection).ExecuteNonQuery();
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					else
					{
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					sqlCommand2.Parameters.AddWithValue("@OfferId", offers.OfferId);
					sqlCommand2.Parameters.AddWithValue("@ClientID", offers.ClientID);
					sqlCommand2.Parameters.AddWithValue("@CatatogryID", RuntimeHelpers.GetObjectValue(((int?)(offerId = offers.CatatogryID)).HasValue ? ((object)offerId.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@ItemID", RuntimeHelpers.GetObjectValue(((int?)(offerId = offers.ItemID)).HasValue ? ((object)offerId.GetValueOrDefault()) : DBNull.Value));
					bool? isForAllItems;
					sqlCommand2.Parameters.AddWithValue("@IsForAllItems", RuntimeHelpers.GetObjectValue(((bool?)(isForAllItems = offers.IsForAllItems)).HasValue ? ((object)(isForAllItems == true)) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@Unit", RuntimeHelpers.GetObjectValue(((object)offers.Unit) ?? ((object)DBNull.Value)));
					double? offerTargetQnty;
					sqlCommand2.Parameters.AddWithValue("@OfferTargetQnty", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offers.OfferTargetQnty)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemValue", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offers.OfferItemValue)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemPercentage", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offers.OfferItemPercentage)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOfferItems(List<ListOfferItem> offerItemsList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListOfferItem offerItems in offerItemsList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM OfferItems WHERE [OfferId] = @OfferId AND [ItemID] = @ItemID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@OfferId", offerItems.OfferId);
					sqlCommand.Parameters.AddWithValue("@ItemID", offerItems.ItemID);
					int num;
					num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
					string cmdText;
					cmdText = "INSERT INTO [dbo].[OfferItems] \r\n                        ([OfferId], [CatatogryID], [ItemID], [OfferNatural], [IsGroupedItems], [unit], [OfferTargetQnty], [OfferItemTargetQnty], [OfferItemPrice], [OfferTotalPrice], [OfferItemValue], [OfferItemPercentage], [OfferItemStock]) \r\n                        VALUES (@OfferId, @CatatogryID, @ItemID, @OfferNatural, @IsGroupedItems, @Unit, @OfferTargetQnty, @OfferItemTargetQnty, @OfferItemPrice, @OfferTotalPrice, @OfferItemValue, @OfferItemPercentage, @OfferItemStock)";
					SqlCommand sqlCommand2;
					int? offerId;
					if (num > 0)
					{
						new SqlCommand(("delete from OfferItems where OfferId=" + (((int?)(offerId = offerItems.OfferId)).HasValue ? Conversions.ToString(offerId.GetValueOrDefault()) : null)) ?? "", ReceivedData.conn);
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					else
					{
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					sqlCommand2.Parameters.AddWithValue("@OfferId", offerItems.OfferId);
					sqlCommand2.Parameters.AddWithValue("@CatatogryID", RuntimeHelpers.GetObjectValue(((int?)(offerId = offerItems.CatatogryID)).HasValue ? ((object)offerId.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@ItemID", RuntimeHelpers.GetObjectValue(((int?)(offerId = offerItems.ItemID)).HasValue ? ((object)offerId.GetValueOrDefault()) : DBNull.Value));
					bool? offerNatural;
					sqlCommand2.Parameters.AddWithValue("@OfferNatural", RuntimeHelpers.GetObjectValue(((bool?)(offerNatural = offerItems.OfferNatural)).HasValue ? ((object)(offerNatural == true)) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@IsGroupedItems", RuntimeHelpers.GetObjectValue(((bool?)(offerNatural = offerItems.IsGroupedItems)).HasValue ? ((object)(offerNatural == true)) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@Unit", RuntimeHelpers.GetObjectValue(((object)offerItems.Unit) ?? ((object)DBNull.Value)));
					double? offerTargetQnty;
					sqlCommand2.Parameters.AddWithValue("@OfferTargetQnty", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferTargetQnty)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemTargetQnty", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferItemTargetQnty)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemPrice", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferItemPrice)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferTotalPrice", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferTotalPrice)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemValue", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferItemValue)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemPercentage", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferItemPercentage)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.Parameters.AddWithValue("@OfferItemStock", RuntimeHelpers.GetObjectValue(((double?)(offerTargetQnty = offerItems.OfferItemStock)).HasValue ? ((object)offerTargetQnty.GetValueOrDefault()) : DBNull.Value));
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateReceipts(List<ListRecipt> receiptsList)
		{
			new SqlCommand();
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListRecipt receipts in receiptsList)
				{
					if (receipts.GlobalID.StartsWith((Conversions.ToString(MainClass.BranchNo) ?? "") ?? ""))
					{
						continue;
					}
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM [dbo].[Receipts] WHERE [GlobalID] =@GlobalID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@GlobalID", receipts.GlobalID);
					int num;
					num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
					string cmdText;
					cmdText = "INSERT INTO  Receipts\r\n                        ([GlobalID], [ReceiptNo], [ReceiptDate], [EmpId], [ClientID], [CreditAcc], [DebitAcc], [Payment], [VAT], [NetVal], [ReceiptType], [PaymentType], [BankId], [TreasuryID], [SalesManID], [CheckNo], [CheckDate], [Checkbank], [CheckState], [State], [Notes], [EntryGlobalID], [BranchID], [ISDeleted], [Cccode], [ReffNo], [Reffdate], [Sync], [MobPOSID])\r\n                        VALUES \r\n                        (@GlobalID, @ReceiptNo, @ReceiptDate, @EmpId, @ClientID, @CreditAcc, @DebitAcc, @Payment, @VAT, @NetVal, @ReceiptType, @PaymentType, @BankId, @TreasuryID, @SalesManID, @CheckNo, @CheckDate, @Checkbank, @CheckState, @State, @Notes, @EntryGlobalID, @BranchID, @ISDeleted, @Cccode, @ReffNo, @Reffdate, @Sync, @MobPOSID)";
					SqlCommand sqlCommand2;
					if (num > 0)
					{
						new SqlCommand("delete from Receipts where GlobalID=N'" + receipts.GlobalID + "' ", sqlConnection).ExecuteNonQuery();
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					else
					{
						sqlCommand2 = new SqlCommand(cmdText, sqlConnection);
					}
					sqlCommand2.Parameters.AddWithValue("@GlobalID", receipts.GlobalID);
					sqlCommand2.Parameters.AddWithValue("@ReceiptNo", receipts.ReceiptNo);
					sqlCommand2.Parameters.AddWithValue("@ReceiptDate", receipts.ReceiptDate);
					sqlCommand2.Parameters.AddWithValue("@EmpId", receipts.EmpId);
					sqlCommand2.Parameters.AddWithValue("@ClientID", receipts.ClientID);
					sqlCommand2.Parameters.AddWithValue("@CreditAcc", receipts.CreditAcc);
					sqlCommand2.Parameters.AddWithValue("@DebitAcc", receipts.DebitAcc);
					sqlCommand2.Parameters.AddWithValue("@Payment", receipts.Payment);
					sqlCommand2.Parameters.AddWithValue("@VAT", receipts.VAT);
					sqlCommand2.Parameters.AddWithValue("@NetVal", receipts.NetVal);
					sqlCommand2.Parameters.AddWithValue("@ReceiptType", receipts.ReceiptType);
					sqlCommand2.Parameters.AddWithValue("@PaymentType", receipts.PaymentType);
					sqlCommand2.Parameters.AddWithValue("@BankId", receipts.BankId);
					sqlCommand2.Parameters.AddWithValue("@TreasuryID", receipts.TreasuryID);
					sqlCommand2.Parameters.AddWithValue("@SalesManID", receipts.SalesManID);
					sqlCommand2.Parameters.AddWithValue("@CheckNo", receipts.CheckNo);
					sqlCommand2.Parameters.AddWithValue("@CheckDate", receipts.CheckDate);
					sqlCommand2.Parameters.AddWithValue("@Checkbank", receipts.Checkbank);
					sqlCommand2.Parameters.AddWithValue("@CheckState", receipts.CheckState);
					sqlCommand2.Parameters.AddWithValue("@State", receipts.State);
					sqlCommand2.Parameters.AddWithValue("@Notes", receipts.Notes);
					sqlCommand2.Parameters.AddWithValue("@EntryGlobalID", receipts.EntryGlobalID);
					sqlCommand2.Parameters.AddWithValue("@BranchID", receipts.BranchID);
					sqlCommand2.Parameters.AddWithValue("@ISDeleted", receipts.ISDeleted);
					sqlCommand2.Parameters.AddWithValue("@Cccode", receipts.Cccode);
					sqlCommand2.Parameters.AddWithValue("@ReffNo", receipts.ReffNo);
					sqlCommand2.Parameters.AddWithValue("@Reffdate", receipts.Reffdate);
					sqlCommand2.Parameters.AddWithValue("@Sync", receipts.Sync);
					sqlCommand2.Parameters.AddWithValue("@MobPOSID", receipts.MobPOSID);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateSafes(List<ListSafes> safesList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListSafes safes in safesList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Safes WHERE id = @id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", safes.Id);
					if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
					{
						using SqlCommand sqlCommand2 = new SqlCommand("UPDATE Safes SET name = @name, branch = @branch, status = @status, IS_Default = @IS_Default, notes = @notes, IS_Deleted = @IS_Deleted WHERE id = @id", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@name", safes.Name);
						sqlCommand2.Parameters.AddWithValue("@branch", safes.Branch);
						sqlCommand2.Parameters.AddWithValue("@status", safes.Status);
						sqlCommand2.Parameters.AddWithValue("@IS_Default", safes.IsDefault);
						sqlCommand2.Parameters.AddWithValue("@notes", safes.Notes);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", safes.IsDeleted);
						sqlCommand2.Parameters.AddWithValue("@id", safes.Id);
						sqlCommand2.ExecuteNonQuery();
					}
					else
					{
						using SqlCommand sqlCommand3 = new SqlCommand("SET IDENTITY_INSERT [dbo].[Safes] ON \r\nINSERT INTO Safes (id, name, branch, status, IS_Default, notes, IS_Deleted) VALUES (@id, @name, @branch, @status, @IS_Default, @notes, @IS_Deleted)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@id", safes.Id);
						sqlCommand3.Parameters.AddWithValue("@name", safes.Name);
						sqlCommand3.Parameters.AddWithValue("@branch", safes.Branch);
						sqlCommand3.Parameters.AddWithValue("@status", safes.Status);
						sqlCommand3.Parameters.AddWithValue("@IS_Default", safes.IsDefault);
						sqlCommand3.Parameters.AddWithValue("@notes", safes.Notes);
						sqlCommand3.Parameters.AddWithValue("@IS_Deleted", safes.IsDeleted);
						sqlCommand3.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateSafesEmp(List<ListSafeEmp> safesList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListSafeEmp safes in safesList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Safe_Emps WHERE emp_id = @emp_id  and safe_id=@safe_id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@emp_id", safes.emp_id);
					sqlCommand.Parameters.AddWithValue("@safe_id", safes.safe_id);
					if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
					{
						using (SqlCommand sqlCommand2 = new SqlCommand("Delete from Safe_Emps WHERE emp_id = @emp_id and safe_id=@safe_id ", sqlConnection))
						{
							sqlCommand2.Parameters.AddWithValue("@emp_id", safes.emp_id);
							sqlCommand2.Parameters.AddWithValue("@safe_id", safes.safe_id);
							sqlCommand2.ExecuteNonQuery();
						}
						using SqlCommand sqlCommand3 = new SqlCommand("INSERT INTO Safe_Emps (safe_id, emp_id) VALUES (@safe_id, @emp_id)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@emp_id", safes.emp_id);
						sqlCommand3.Parameters.AddWithValue("@safe_id", safes.safe_id);
						sqlCommand3.ExecuteNonQuery();
					}
					else
					{
						using SqlCommand sqlCommand4 = new SqlCommand("INSERT INTO Safe_Emps (safe_id, emp_id) VALUES (@safe_id, @emp_id)", sqlConnection);
						sqlCommand4.Parameters.AddWithValue("@emp_id", safes.emp_id);
						sqlCommand4.Parameters.AddWithValue("@safe_id", safes.safe_id);
						sqlCommand4.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateStocks(List<ListStock> stocksList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListStock stocks in stocksList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Stocks WHERE id = @id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", stocks.Id);
					if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
					{
						using SqlCommand sqlCommand2 = new SqlCommand("UPDATE Stocks SET name = @name, Acc_Code = @Acc_Code, branch = @branch, status = @status, IS_Default = @IS_Default, notes = @notes, IS_Deleted = @IS_Deleted WHERE id = @id", sqlConnection);
						sqlCommand2.Parameters.AddWithValue("@name", stocks.Name);
						sqlCommand2.Parameters.AddWithValue("@Acc_Code", stocks.Acc_Code);
						sqlCommand2.Parameters.AddWithValue("@branch", stocks.Branch);
						sqlCommand2.Parameters.AddWithValue("@status", stocks.Status);
						sqlCommand2.Parameters.AddWithValue("@IS_Default", stocks.IsDefault);
						sqlCommand2.Parameters.AddWithValue("@notes", stocks.Notes);
						sqlCommand2.Parameters.AddWithValue("@IS_Deleted", stocks.IsDeleted);
						sqlCommand2.Parameters.AddWithValue("@id", stocks.Id);
						sqlCommand2.ExecuteNonQuery();
					}
					else
					{
						using SqlCommand sqlCommand3 = new SqlCommand("SET IDENTITY_INSERT [dbo].[Stocks] ON \r\nINSERT INTO Stocks (id, name, Acc_Code, branch, status, IS_Default, notes, IS_Deleted) VALUES (@id, @name, @Acc_Code, @branch, @status, @IS_Default, @notes, @IS_Deleted)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@id", stocks.Id);
						sqlCommand3.Parameters.AddWithValue("@name", stocks.Name);
						sqlCommand3.Parameters.AddWithValue("@Acc_Code", stocks.Acc_Code);
						sqlCommand3.Parameters.AddWithValue("@branch", stocks.Branch);
						sqlCommand3.Parameters.AddWithValue("@status", stocks.Status);
						sqlCommand3.Parameters.AddWithValue("@IS_Default", stocks.IsDefault);
						sqlCommand3.Parameters.AddWithValue("@notes", stocks.Notes);
						sqlCommand3.Parameters.AddWithValue("@IS_Deleted", stocks.IsDeleted);
						sqlCommand3.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateStockEmps(List<ListStockEmp> stockEmpList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListStockEmp stockEmp in stockEmpList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Stock_Emps WHERE stock_id = @stock_id AND emp_id = @emp_id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@stock_id", stockEmp.StockId);
					sqlCommand.Parameters.AddWithValue("@emp_id", stockEmp.EmpId);
					if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) > 0)
					{
						using (SqlCommand sqlCommand2 = new SqlCommand("Delete from Stock_Emps WHERE emp_id = @emp_id and stock_id = @stock_id ", sqlConnection))
						{
							sqlCommand2.Parameters.AddWithValue("@stock_id", stockEmp.StockId);
							sqlCommand2.Parameters.AddWithValue("@emp_id", stockEmp.EmpId);
							sqlCommand2.ExecuteNonQuery();
						}
						using SqlCommand sqlCommand3 = new SqlCommand("INSERT INTO Stock_Emps (stock_id, emp_id) VALUES (@stock_id, @emp_id)", sqlConnection);
						sqlCommand3.Parameters.AddWithValue("@stock_id", stockEmp.StockId);
						sqlCommand3.Parameters.AddWithValue("@emp_id", stockEmp.EmpId);
						sqlCommand3.ExecuteNonQuery();
					}
					else
					{
						using SqlCommand sqlCommand4 = new SqlCommand("INSERT INTO Stock_Emps (stock_id, emp_id) VALUES (@stock_id, @emp_id)", sqlConnection);
						sqlCommand4.Parameters.AddWithValue("@stock_id", stockEmp.StockId);
						sqlCommand4.Parameters.AddWithValue("@emp_id", stockEmp.EmpId);
						sqlCommand4.ExecuteNonQuery();
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateBranches(List<ListBranches> branchList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListBranches branch in branchList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Branches WHERE BranchId = @BranchId", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@BranchId", branch.BranchId);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("INSERT INTO Branches (BranchId, name, tel, mobile, fax, email, address, notes, is_deleted, IsDefault, code, CustomersAcc, SupliersAcc, BanksAcc, InventoryAcc, TreasuriesAcc, IsActive, CostCenter, AppAcc, EmployeeAcc) VALUES (@BranchId, @name, @tel, @mobile, @fax, @email, @address, @notes, @is_deleted, @IsDefault, @code, @CustomersAcc, @SupliersAcc, @BanksAcc, @InventoryAcc, @TreasuriesAcc, @IsActive, @CostCenter, @AppAcc, @EmployeeAcc)", sqlConnection) : new SqlCommand("UPDATE Branches SET name=@name, tel=@tel, mobile=@mobile, fax=@fax, email=@email, address=@address, notes=@notes, IsDefault=@IsDefault, CustomersAcc=@CustomersAcc, SupliersAcc=@SupliersAcc, BanksAcc=@BanksAcc, InventoryAcc=@InventoryAcc, TreasuriesAcc=@TreasuriesAcc, IsActive=@IsActive, CostCenter=@CostCenter, AppAcc=@AppAcc, EmployeeAcc=@EmployeeAcc WHERE BranchId=@BranchId", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@BranchId", branch.BranchId);
					sqlCommand2.Parameters.AddWithValue("@name", branch.Name);
					sqlCommand2.Parameters.AddWithValue("@tel", branch.Tel);
					sqlCommand2.Parameters.AddWithValue("@mobile", branch.Mobile);
					sqlCommand2.Parameters.AddWithValue("@fax", branch.Fax);
					sqlCommand2.Parameters.AddWithValue("@email", branch.Email);
					sqlCommand2.Parameters.AddWithValue("@address", branch.Address);
					sqlCommand2.Parameters.AddWithValue("@notes", branch.Notes);
					sqlCommand2.Parameters.AddWithValue("@is_deleted", branch.IsDeleted);
					sqlCommand2.Parameters.AddWithValue("@IsDefault", branch.IsDefault);
					sqlCommand2.Parameters.AddWithValue("@code", branch.Code);
					sqlCommand2.Parameters.AddWithValue("@CustomersAcc", branch.CustomersAcc);
					sqlCommand2.Parameters.AddWithValue("@SupliersAcc", branch.SupliersAcc);
					sqlCommand2.Parameters.AddWithValue("@BanksAcc", branch.BanksAcc);
					sqlCommand2.Parameters.AddWithValue("@InventoryAcc", branch.InventoryAcc);
					sqlCommand2.Parameters.AddWithValue("@TreasuriesAcc", branch.TreasuriesAcc);
					sqlCommand2.Parameters.AddWithValue("@IsActive", branch.IsActive);
					sqlCommand2.Parameters.AddWithValue("@CostCenter", branch.CostCenter);
					sqlCommand2.Parameters.AddWithValue("@AppAcc", branch.AppAcc);
					sqlCommand2.Parameters.AddWithValue("@EmployeeAcc", branch.EmployeeAcc);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateBanks(List<ListBank> bankList)
		{
			try
			{
				new SqlCommand();
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListBank bank in bankList)
				{
					using SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Banks WHERE id = @id", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", bank.Id);
					SqlCommand sqlCommand2;
					sqlCommand2 = ((Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) <= 0) ? new SqlCommand("SET IDENTITY_INSERT [dbo].[Banks] ON \r\nINSERT INTO Banks (id, name, country, city, Acc_Code, area, tel, mobile, notes, IS_Deleted, DisPre, ChangeInPOS) VALUES (@id, @name, @country, @city, @Acc_Code, @area, @tel, @mobile, @notes, @IS_Deleted, @DisPre, @ChangeInPOS)", sqlConnection) : new SqlCommand("UPDATE Banks SET name = @name, country = @country, city = @city, Acc_Code = @Acc_Code, area = @area, tel = @tel, mobile = @mobile, notes = @notes, IS_Deleted = @IS_Deleted, DisPre = @DisPre, ChangeInPOS = @ChangeInPOS WHERE id = @id", sqlConnection));
					sqlCommand2.Parameters.AddWithValue("@id", bank.Id);
					sqlCommand2.Parameters.AddWithValue("@name", bank.Name);
					sqlCommand2.Parameters.AddWithValue("@country", bank.Country);
					sqlCommand2.Parameters.AddWithValue("@city", bank.City);
					sqlCommand2.Parameters.AddWithValue("@Acc_Code", bank.AccCode);
					sqlCommand2.Parameters.AddWithValue("@area", bank.Area);
					sqlCommand2.Parameters.AddWithValue("@tel", bank.Tel);
					sqlCommand2.Parameters.AddWithValue("@mobile", bank.Mobile);
					sqlCommand2.Parameters.AddWithValue("@notes", bank.Notes);
					sqlCommand2.Parameters.AddWithValue("@IS_Deleted", bank.IsDeleted);
					sqlCommand2.Parameters.AddWithValue("@DisPre", bank.DisPre);
					sqlCommand2.Parameters.AddWithValue("@ChangeInPOS", bank.ChangeInPOS);
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateCasherClosed(List<ListCasherClosed> casherList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListCasherClosed casher in casherList)
				{
					using SqlCommand sqlCommand = new SqlCommand("\r\n    IF EXISTS (SELECT 1 FROM CasherClosed WHERE GlobalID = @GlobalID) \r\n    BEGIN \r\n        UPDATE CasherClosed SET \r\n            [type] = @type, \r\n            [user_id] = @user_id, \r\n            [startTime] = @startTime, \r\n            [endTime] = @endTime, \r\n            [ToT] = @ToT, \r\n            [CasherValue] = @CasherValue, \r\n            [diff] = @diff, \r\n            [GlobalID] = @GlobalID, \r\n            [Branch] = @Branch, \r\n            [ClosedID] = @ClosedID \r\n        WHERE GlobalID= @GlobalID \r\n    END \r\n    ELSE \r\n    BEGIN \r\n        SET IDENTITY_INSERT [dbo].[CasherClosed] ON;\r\n        INSERT INTO CasherClosed ([id], [type], [user_id], [startTime], [endTime], [ToT], [CasherValue], [diff], [GlobalID], [Branch], [ClosedID]) \r\n        VALUES (@id, @type, @user_id, @startTime, @endTime, @ToT, @CasherValue, @diff, @GlobalID, @Branch, @ClosedID);\r\n        SET IDENTITY_INSERT [dbo].[CasherClosed] OFF;\r\n    END", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", casher.Id);
					sqlCommand.Parameters.AddWithValue("@type", casher.Type);
					sqlCommand.Parameters.AddWithValue("@user_id", casher.UserId);
					sqlCommand.Parameters.AddWithValue("@startTime", casher.StartTime);
					sqlCommand.Parameters.AddWithValue("@endTime", casher.EndTime);
					sqlCommand.Parameters.AddWithValue("@ToT", casher.ToT);
					sqlCommand.Parameters.AddWithValue("@CasherValue", casher.CasherValue);
					sqlCommand.Parameters.AddWithValue("@diff", casher.Diff);
					sqlCommand.Parameters.AddWithValue("@GlobalID", casher.GlobalID);
					sqlCommand.Parameters.AddWithValue("@Branch", casher.Branch);
					sqlCommand.Parameters.AddWithValue("@ClosedID", casher.ClosedID);
					sqlCommand.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InsertOrUpdateCasherClosedSub(List<ListCasherClosedSub> casherSubList)
		{
			try
			{
				using SqlConnection sqlConnection = new SqlConnection(ReceivedData.connString);
				sqlConnection.Open();
				foreach (ListCasherClosedSub casherSub in casherSubList)
				{
					using SqlCommand sqlCommand = new SqlCommand("\r\n    IF EXISTS (SELECT 1 FROM CasherClosed_Sub WHERE GlobalID = @GlobalID) \r\n    BEGIN \r\n        DELETE FROM CasherClosed_Sub WHERE GlobalID = @GlobalID  \r\n        SET IDENTITY_INSERT [dbo].[CasherClosed_Sub] ON\r\n        INSERT INTO CasherClosed_Sub ([id], [ClosedId], [CashTotal], [SAfeNetVal], [ReturnSum], [NetworkSum], \r\n                                      [AdditionVal], [PostPoneSales], [PostPoneRet], [InsurVal], [Discount], [AllVAT], [ExtraTax], \r\n                                      [Expenses], [Purchases], [HostingVal], [IsDeleted], [GlobalID]) \r\n        VALUES (@id, @ClosedId, @CashTotal, @SAfeNetVal, @ReturnSum, @NetworkSum, \r\n                @AdditionVal, @PostPoneSales, @PostPoneRet, @InsurVal, @Discount, @AllVAT, @ExtraTax, \r\n                @Expenses, @Purchases, @HostingVal, @IsDeleted, @GlobalID)\r\n        SET IDENTITY_INSERT [dbo].[CasherClosed_Sub] OFF\r\n    END \r\n    ELSE \r\n    BEGIN \r\n        SET IDENTITY_INSERT [dbo].[CasherClosed_Sub] ON\r\n        INSERT INTO CasherClosed_Sub ([id], [ClosedId], [CashTotal], [SAfeNetVal], [ReturnSum], [NetworkSum], \r\n                                      [AdditionVal], [PostPoneSales], [PostPoneRet], [InsurVal], [Discount], [AllVAT], [ExtraTax], \r\n                                      [Expenses], [Purchases], [HostingVal], [IsDeleted], [GlobalID]) \r\n        VALUES (@id, @ClosedId, @CashTotal, @SAfeNetVal, @ReturnSum, @NetworkSum, \r\n                @AdditionVal, @PostPoneSales, @PostPoneRet, @InsurVal, @Discount, @AllVAT, @ExtraTax, \r\n                @Expenses, @Purchases, @HostingVal, @IsDeleted, @GlobalID)\r\n        SET IDENTITY_INSERT [dbo].[CasherClosed_Sub] OFF\r\n    END", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@id", casherSub.Id);
					sqlCommand.Parameters.AddWithValue("@ClosedId", casherSub.ClosedId);
					sqlCommand.Parameters.AddWithValue("@CashTotal", casherSub.CashTotal);
					sqlCommand.Parameters.AddWithValue("@SAfeNetVal", casherSub.SAfeNetVal);
					sqlCommand.Parameters.AddWithValue("@ReturnSum", casherSub.ReturnSum);
					sqlCommand.Parameters.AddWithValue("@NetworkSum", casherSub.NetworkSum);
					sqlCommand.Parameters.AddWithValue("@AdditionVal", casherSub.AdditionVal);
					sqlCommand.Parameters.AddWithValue("@PostPoneSales", casherSub.PostPoneSales);
					sqlCommand.Parameters.AddWithValue("@PostPoneRet", casherSub.PostPoneRet);
					sqlCommand.Parameters.AddWithValue("@InsurVal", casherSub.InsurVal);
					sqlCommand.Parameters.AddWithValue("@Discount", casherSub.Discount);
					sqlCommand.Parameters.AddWithValue("@AllVAT", casherSub.AllVAT);
					sqlCommand.Parameters.AddWithValue("@ExtraTax", casherSub.ExtraTax);
					sqlCommand.Parameters.AddWithValue("@Expenses", casherSub.Expenses);
					sqlCommand.Parameters.AddWithValue("@Purchases", casherSub.Purchases);
					sqlCommand.Parameters.AddWithValue("@HostingVal", casherSub.HostingVal);
					sqlCommand.Parameters.AddWithValue("@IsDeleted", casherSub.IsDeleted);
					sqlCommand.Parameters.AddWithValue("@GlobalID", casherSub.GlobalID);
					sqlCommand.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}
}