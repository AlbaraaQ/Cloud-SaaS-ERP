using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using QLicense;
using SmartAuditERP.Form_WPF;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartAuditERP
{

	public class DeviceLicenseManager
	{
		private static readonly string LicenseFilePath = Path.Combine(Application.StartupPath, "device.lic");

		private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("F2A8E72D8A9F4A6DB7C38C59F8B92AD1");

		private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("16ByteInitVector");

		private static readonly string LastVerifiedFilePath = Path.Combine(Application.StartupPath, "last_verified_time.lic");

		public static void SaveLicenseFile(string json)
		{
			try
			{
				byte[] array;
				array = DeviceLicenseManager.Encrypt(json);
				if (array == null || array.Length == 0)
				{
					MessageBox.Show("❌ فشل في تشفير ملف الترخيص.", "خطأ في الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				}
				else
				{
					File.WriteAllBytes(DeviceLicenseManager.LicenseFilePath, array);
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ حدث خطأ أثناء حفظ ملف الترخيص: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
		}

		public static DeviceLicenseDto LoadAndValidateLicense()
		{
			DeviceLicenseDto result;
			if (!File.Exists(DeviceLicenseManager.LicenseFilePath))
			{
				Interaction.MsgBox("❌ ملف الترخيص غير موجود");
				result = null;
			}
			else
			{
				try
				{
					string value;
					value = DeviceLicenseManager.Decrypt(File.ReadAllBytes(DeviceLicenseManager.LicenseFilePath));
					if (string.IsNullOrWhiteSpace(value))
					{
						Interaction.MsgBox("❌ فشل في فك التشفير");
						result = null;
					}
					else
					{
						DeviceLicenseDto deviceLicenseDto;
						deviceLicenseDto = JsonConvert.DeserializeObject<DeviceLicenseDto>(value);
						if (deviceLicenseDto == null)
						{
							Interaction.MsgBox("❌ فشل في قراءة JSON");
							result = null;
						}
						else if (Operators.CompareString(Right: DeviceLicenseManager.GenerateHash(deviceLicenseDto.CompanyName + deviceLicenseDto.DeviceId + deviceLicenseDto.Email + deviceLicenseDto.ExpireDate.ToString("yyyyMMdd") + deviceLicenseDto.Features), Left: deviceLicenseDto.Signature, TextCompare: false) != 0)
						{
							Interaction.MsgBox("❌ التوقيع غير متطابق");
							result = null;
						}
						else
						{
							result = deviceLicenseDto;
						}
					}
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Interaction.MsgBox("❌ خطأ أثناء التحقق: " + ex.Message);
					result = null;
					ProjectData.ClearProjectError();
				}
			}
			return result;
		}

		public static string GenerateHash(string input)
		{
			using SHA256 sHA = SHA256.Create();
			return Convert.ToBase64String(sHA.ComputeHash(Encoding.UTF8.GetBytes(input)));
		}

		public static async Task<bool> ActivateOnlineLicense(string baseUrl, string companyName, string email)
		{
			try
			{
				string text;
				text = HardwareInfo.GenerateUID("SmartAuditERP", IncMac: true);
				if (string.IsNullOrWhiteSpace(text))
				{
					return false;
				}
				string machineName;
				machineName = Environment.MachineName;
				using HttpClient httpClient = new HttpClient();
				StringContent content;
				content = new StringContent(JsonConvert.SerializeObject((companyName, text, machineName, email)), Encoding.UTF8, "application/json");
				baseUrl += "/api/DeviceLicense/authorize";
				HttpResponseMessage httpResponseMessage;
				httpResponseMessage = await httpClient.PostAsync(baseUrl, content);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					if (File.Exists(DeviceLicenseManager.LicenseFilePath))
					{
						File.Delete(DeviceLicenseManager.LicenseFilePath);
					}
					DeviceLicenseManager.SaveLicenseFile(await httpResponseMessage.Content.ReadAsStringAsync());
					return true;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				if (File.Exists(DeviceLicenseManager.LicenseFilePath))
				{
					File.Delete(DeviceLicenseManager.LicenseFilePath);
				}
				ProjectData.ClearProjectError();
			}
			return false;
		}

		public static bool IsInternetAvailable()
		{
			bool result;
			try
			{
				using WebClient webClient = new WebClient();
				using (webClient.OpenRead("http://clients3.google.com/generate_204"))
				{
					result = true;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static async Task CheckAndValidateLicense(string baseUrl, string companyName, string email)
		{
			try
			{
				if (DeviceLicenseManager.IsInternetAvailable())
				{
					await DeviceLicenseManager.ActivateOnlineLicense(baseUrl, companyName, email);
				}
				await DeviceLicenseManager.ShowtrialOfflin();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

        public static async Task ShowtrialOfflin(Home homeForm = null)
        {
            try
            {
                DeviceLicenseDto lic = DeviceLicenseManager.LoadAndValidateLicense();

                // ✅ الحصول على Home الصحيح
                if (homeForm == null)
                {
                    homeForm = System.Windows.Application.Current.Windows
                        .OfType<Home>()
                        .FirstOrDefault();
                }

                // ✅ إذا لم نجد Home، اخرج
                if (homeForm == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ لم يتم العثور على نافذة Home");
                    MainClass.IsTrial = true;
                    MainClass.LicenseFeature = "";
                    return;
                }

                // ✅ معالجة null للترخيص
                if (lic == null)
                {
                    MainClass.IsTrial = true;
                    MainClass.LicenseFeature = "";

                    homeForm.Dispatcher.Invoke(() =>
                    {
                        homeForm.LblLicenseExpire.Text = "حالة الترخيص :  ❌ لم يتم التحقق من الترخيص";
                        homeForm.lblIsTrial1.Text =
                            (Operators.CompareString(MainClass.Language, "ar", false) == 0)
                            ? "نسخة تجريبية"
                            : "Trial version";
                    });
                    return;
                }

                MainClass.LicenseFeature = lic.Features ?? "";

                DateTime? nowTimeNullable = DeviceLicenseManager.GetInternetTime();
                DateTime nowTime = nowTimeNullable ??
                    DeviceLicenseManager.LoadLastVerifiedTime() ??
                    DateTime.Now;

                if (nowTimeNullable.HasValue)
                {
                    DeviceLicenseManager.SaveLastVerifiedTime(nowTime);
                }

                MainClass.LicenseExpire = lic.ExpireDate.Date;
                MainClass.LicenseFeature = lic.Features.Trim();

                DateTime expireDate;
                if (DateTime.TryParse(MainClass.LicenseExpire.ToString("yyyy-MM-dd"), out expireDate))
                {
                    int remainingDays = (expireDate - nowTime).Days;

                    if (DateTime.Compare(expireDate, nowTime) < 0)
                    {
                        MainClass.IsTrial = true;

                        homeForm.Dispatcher.Invoke(() =>
                        {
                            homeForm.LblLicenseExpire.Text =
                                " حالة الترخيص : عذراً، لقد انتهى اشتراكك  ";
                        });
                    }
                    else
                    {
                        MainClass.IsTrial = false;

                        homeForm.Dispatcher.Invoke(() =>
                        {
                            homeForm.LblLicenseExpire.Text =
                                "حالة الترخيص : ينتهي  بعد: " + remainingDays + " يوم";
                        });
                    }
                }
                else
                {
                    MainClass.IsTrial = true;

                    homeForm.Dispatcher.Invoke(() =>
                    {
                        homeForm.LblLicenseExpire.Text =
                            "حالة الترخيص :  ❌ خطأ في تاريخ انتهاء الترخيص";
                    });
                }

                homeForm.Dispatcher.Invoke(() =>
                {
                    if (MainClass.IsTrial)
                    {
                        homeForm.lblIsTrial1.Text =
                            (Operators.CompareString(MainClass.Language, "ar", false) == 0)
                            ? "نسخة تجريبية"
                            : "Trial version";
                    }
                    else
                    {
                        homeForm.lblIsTrial1.Text = "";
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في ShowtrialOfflin: {ex.Message}");
                MainClass.IsTrial = true;
                MainClass.LicenseFeature = "";
            }
        }

        private static byte[] Encrypt(string plainText)
		{
			using Aes aes = Aes.Create();
			aes.Key = DeviceLicenseManager.AES_KEY;
			aes.IV = DeviceLicenseManager.AES_IV;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			ICryptoTransform cryptoTransform;
			cryptoTransform = aes.CreateEncryptor();
			byte[] bytes;
			bytes = Encoding.UTF8.GetBytes(plainText);
			return cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
		}

		private static string Decrypt(byte[] cipherBytes)
		{
			using Aes aes = Aes.Create();
			aes.Key = DeviceLicenseManager.AES_KEY;
			aes.IV = DeviceLicenseManager.AES_IV;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.PKCS7;
			byte[] bytes;
			bytes = aes.CreateDecryptor().TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
			return Encoding.UTF8.GetString(bytes);
		}

		public static DateTime? GetInternetTime()
		{
			checked
			{
				DateTime? result;
				try
				{
					string hostNameOrAddress;
					hostNameOrAddress = "time.windows.com";
					byte[] array;
					array = new byte[48];
					array[0] = 27;
					IPEndPoint remoteEP;
					remoteEP = new IPEndPoint(Dns.GetHostEntry(hostNameOrAddress).AddressList[0], 123);
					using UdpClient udpClient = new UdpClient();
					udpClient.Client.ReceiveTimeout = 3000;
					udpClient.Connect(remoteEP);
					udpClient.Send(array, array.Length);
					byte[] value;
					value = udpClient.Receive(ref remoteEP);
					ulong x;
					x = BitConverter.ToUInt32(value, 40);
					ulong x2;
					x2 = BitConverter.ToUInt32(value, 44);
					x = DeviceLicenseManager.SwapEndianness(x);
					x2 = DeviceLicenseManager.SwapEndianness(x2);
					ulong num;
					num = x * 1000 + unchecked(checked(x2 * 1000) / 4294967296L);
					return new DateTime(1900, 1, 1).AddMilliseconds(num).ToLocalTime();
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					result = null;
					ProjectData.ClearProjectError();
				}
				return result;
			}
		}

		private static ulong SwapEndianness(ulong x)
		{
			return checked((ulong)((((long)x & 0xFF) << 24) | (((long)x & 0xFF00) << 8) | (((long)x & 0xFF0000) >> 8) | (((long)x & -16777216) >> 24)));
		}

		public static void SaveLastVerifiedTime(DateTime time)
		{
			try
			{
				byte[] array;
				array = DeviceLicenseManager.Encrypt(JsonConvert.SerializeObject(time));
				if (array != null && array.Length > 0)
				{
					File.WriteAllBytes(DeviceLicenseManager.LastVerifiedFilePath, array);
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ فشل في حفظ التاريخ المشفر: " + ex.Message);
				ProjectData.ClearProjectError();
			}
		}

		private static DateTime? LoadLastVerifiedTime()
		{
			DateTime? result;
			try
			{
				if (!File.Exists(DeviceLicenseManager.LastVerifiedFilePath))
				{
					result = null;
				}
				else
				{
					string value;
					value = DeviceLicenseManager.Decrypt(File.ReadAllBytes(DeviceLicenseManager.LastVerifiedFilePath));
					if (!string.IsNullOrWhiteSpace(value))
					{
						return JsonConvert.DeserializeObject<DateTime>(value);
					}
					MessageBox.Show("❌ فشل في فك تشفير ملف التاريخ");
					result = null;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ خطأ أثناء استرجاع تاريخ التحقق: " + ex.Message);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}
	}
}