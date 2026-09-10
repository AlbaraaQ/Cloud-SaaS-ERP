using System;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Timers;
using AuditorAPI.Models;
using Microsoft.VisualBasic.CompilerServices;
using uPLibrary.Networking.M2Mqtt;

namespace SmartAuditERP
{

	[StandardModule]
	internal sealed class ConnectBroker
	{
		public static MqttClient mqttClient;

		public static string ClientCode = "1006";

		public static string connString = "";

		public static SqlConnection conn = new SqlConnection(ConnectBroker.connString);

		public static string lastErrorMsg;

		public static AESencDEC ConEncrypt = new AESencDEC();

		public static string _passcode = "";

		private static System.Windows.Forms.Timer sendTimer;

		private static string clientId = "xxxx";

		public static string _IpServer = "";

		public static bool _syncItems = false;

		public static bool _syncOper = false;

		public static bool _syncDefinitions = false;

		public static bool IsConnectedToBroker(string Connstring)
		{
			bool result = default(bool);
			try
			{
				ConnectBroker.mqttClient = new MqttClient(ConnectBroker._IpServer, 1883, secure: false, null, null, MqttSslProtocols.None);
				ConnectBroker.clientId = Guid.NewGuid().ToString();
				ConnectBroker.mqttClient.Connect(ConnectBroker.clientId);
				if (ConnectBroker.mqttClient.IsConnected)
				{
					result = true;
					return result;
				}
				Console.WriteLine("Failed to connect to the broker.");
				result = false;
				return result;
			}
			catch (OutOfMemoryException ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine("Memory error: " + ex.Message);
				GC.Collect();
				ProjectData.ClearProjectError();
			}
			catch (Exception ex2)
			{
				ProjectData.SetProjectError(ex2);
				Console.WriteLine("Error: " + ex2.Message);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static bool IsConnectedToInternet()
		{
			bool result;
			try
			{
				result = new Ping().Send("8.8.8.8").Status == IPStatus.Success;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static bool CheckConnectionAndBroker()
		{
			bool result;
			if (ConnectBroker.IsConnectedToInternet())
			{
				if (!ConnectBroker.IsConnectedToBroker(ConnectBroker.connString))
				{
					try
					{
						ConnectBroker.mqttClient.Connect(ConnectBroker.clientId);
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						result = false;
						ProjectData.ClearProjectError();
						goto IL_004b;
					}
				}
				result = true;
			}
			else
			{
				result = false;
			}
			goto IL_004b;
		IL_004b:
			return result;
		}
	}
}