using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SmartAuditERP
{

	public class WhatsAppSender
	{
		private IWebDriver driver;

		private WebDriverWait wait;

		public WhatsAppSender()
		{
			try
			{
				Process[] processesByName;
				processesByName = Process.GetProcessesByName("chrome");
				foreach (Process process in processesByName)
				{
					try
					{
						process.Kill();
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
				}
				string driverPath;
				driverPath = Path.Combine(Application.StartupPath, "chromedriver-win64");
				string text;
				text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "chrome-profile");
				ChromeOptions chromeOptions;
				chromeOptions = new ChromeOptions();
				chromeOptions.AddArgument("--start-maximized");
				chromeOptions.AddArgument("--disable-notifications");
				chromeOptions.AddArgument("--user-data-dir=" + text);
				chromeOptions.AddArgument("--no-sandbox");
				chromeOptions.AddArgument("--disable-dev-shm-usage");
				chromeOptions.AddArgument("--disable-gpu");
				ChromeDriverService chromeDriverService;
				chromeDriverService = ChromeDriverService.CreateDefaultService(driverPath);
				chromeDriverService.HideCommandPromptWindow = true;
				this.driver = new ChromeDriver(chromeDriverService, chromeOptions);
				this.wait = new WebDriverWait(this.driver, TimeSpan.FromSeconds(30.0));
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ فشل في تهيئة واتساب Web: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
		}

		public async Task InitializeWhatsAppAsync()
		{
			try
			{
				this.driver.Navigate().GoToUrl("https://web.whatsapp.com");
				await Task.Delay(3000);
				bool flag;
				flag = false;
				DateTime t;
				t = DateTime.Now.AddSeconds(15.0);
				while (DateTime.Compare(DateTime.Now, t) < 0)
				{
					try
					{
						if (this.driver.FindElements(By.CssSelector("canvas[aria-label='Scan me!']")).Count > 0)
						{
							flag = true;
							break;
						}
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					await Task.Delay(1000);
				}
				if (flag)
				{
					MessageBox.Show("يرجى مسح رمز QR ثم اضغط موافق", "WhatsApp Web", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					await Task.Delay(20000);
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ خطأ في تحميل WhatsApp Web: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
		}

		public async Task<bool> SendInvoiceAsync(string customerPhone, string invoiceFilePath, string message = "")
		{
			try
			{
				string text;
				text = customerPhone.Trim();
				if (!text.StartsWith("966"))
				{
					text = "966" + text.TrimStart('0');
				}
				this.driver.Navigate().GoToUrl($"https://web.whatsapp.com/send?phone={text}");
				await Task.Delay(10000);
				bool flag;
				flag = false;
				DateTime t;
				t = DateTime.Now.AddSeconds(25.0);
				while (DateTime.Compare(DateTime.Now, t) < 0)
				{
					try
					{
						if (this.driver.FindElements(By.CssSelector("div[contenteditable='true'][data-tab='10']")).Count > 0)
						{
							flag = true;
							break;
						}
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					await Task.Delay(1000);
				}
				if (!flag)
				{
					MessageBox.Show("❌ الرقم غير مرتبط بحساب WhatsApp أو لم يتم تحميل المحادثة.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				if (!string.IsNullOrWhiteSpace(message))
				{
					message = this.RemoveNonBmpCharacters(message);
					IWebElement webElement;
					webElement = this.wait.Until([SpecialName] (IWebDriver d) => d.FindElement(By.CssSelector("div[contenteditable='true'][data-tab='10']")));
					webElement.Click();
					webElement.SendKeys(message);
					await Task.Delay(500);
					webElement.SendKeys(global::OpenQA.Selenium.Keys.Enter);
					await Task.Delay(1000);
				}
				IWebElement webElement2;
				webElement2 = null;
				t = DateTime.Now.AddSeconds(15.0);
				while (DateTime.Compare(DateTime.Now, t) < 0)
				{
					try
					{
						webElement2 = this.driver.FindElement(By.XPath("//button[@data-tab='10']"));
						if (webElement2 != null)
						{
							break;
						}
					}
					catch (Exception projectError2)
					{
						ProjectData.SetProjectError(projectError2);
						ProjectData.ClearProjectError();
					}
					await Task.Delay(1000);
				}
				if (webElement2 == null)
				{
					MessageBox.Show("❌ لم يتم العثور على زر الإرفاق (\ud83d\udcce).", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				((IJavaScriptExecutor)this.driver).ExecuteScript("arguments[0].click();", webElement2);
				await Task.Delay(2000);
				if (!File.Exists(invoiceFilePath))
				{
					MessageBox.Show("❌ لم يتم العثور على الملف: " + invoiceFilePath, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				this.wait.Until([SpecialName] (IWebDriver d) => d.FindElement(By.CssSelector("input[type='file']"))).SendKeys(invoiceFilePath);
				await Task.Delay(4000);
				IWebElement webElement3;
				webElement3 = null;
				t = DateTime.Now.AddSeconds(20.0);
				while (DateTime.Compare(DateTime.Now, t) < 0)
				{
					try
					{
						webElement3 = this.driver.FindElement(By.XPath("//span[contains(@data-icon, 'send')]"));
						if (webElement3 != null)
						{
							break;
						}
					}
					catch (Exception projectError3)
					{
						ProjectData.SetProjectError(projectError3);
						ProjectData.ClearProjectError();
					}
					await Task.Delay(1000);
				}
				if (webElement3 == null)
				{
					MessageBox.Show("❌ لم يتم العثور على زر الإرسال بعد رفع الملف.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return false;
				}
				webElement3.Click();
				await Task.Delay(3000);
				return true;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("❌ خطأ أثناء إرسال الفاتورة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				bool result;
				result = false;
				ProjectData.ClearProjectError();
				return result;
			}
		}

		public bool IsSessionValid()
		{
			bool result;
			try
			{
				_ = this.driver.WindowHandles;
				result = true;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public void Close()
		{
			try
			{
				if (this.driver != null)
				{
					this.driver.Quit();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private string RemoveNonBmpCharacters(string input)
		{
			return new string(input.Where([SpecialName] (char c) => c <= '\uffff').ToArray());
		}
	}
}