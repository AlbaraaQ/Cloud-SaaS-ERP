using System;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;
using ETA_Invoice.Models;
using Newtonsoft.Json.Linq;
using RestSharp;
using Valley;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSentEinvoice : ThemedWindow
    {
        #region Fields

        // تُبقي على أسماء المتغيرات الأصلية للتوافق
        private DataTable dt;

        #endregion

        #region Constructor

        public frmSentEinvoice()
        {
            InitializeComponent();
            dt = new DataTable();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // يمكن تحميل بيانات أولية هنا إذا لزم
        }

        #endregion

        #region Button Events

        /// <summary>
        /// SimpleButton1 - عرض الفواتير المرفوعة
        /// </summary>
        private void SimpleButton1_Click(object sender, RoutedEventArgs e)
        {
            EtaAPIToken etaAPIToken = new EtaAPIToken();

            if (!EtaSetting.Active)
            {
                DXMessageBox.Show("الرجاء تفعيل الفوترة الإلكترونية المصرية من الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string baseUrl;

            if (string.Equals(EtaSetting.EinvoieType, "production", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "https://api.invoicing.eta.gov.eg";
                etaAPIToken.TokenAPIUrl = "https://id.eta.gov.eg/connect/token";
            }
            else
            {
                baseUrl = "https://api.preprod.invoicing.eta.gov.eg";
                etaAPIToken.TokenAPIUrl = "https://id.preprod.eta.gov.eg/connect/token";
            }

            try
            {
                RestClient restClient = new RestClient(baseUrl);
                RestRequest restRequest = new RestRequest(
                    $"api/v1.0/documents/recent?pageNo={pageNo.Text}&pageSize={pageSize.Text}",
                    Method.GET);

                etaAPIToken.client_id = EtaSetting.EtaClientId;
                etaAPIToken.client_secret_1 = EtaSetting.EtaClientSecret1;
                etaAPIToken.client_secret_2 = EtaSetting.EtaClientSecret2;

                string bearerToken = etaAPIToken.GetBearerToken();

                restRequest.AddHeader("Accept", "application/json");
                restRequest.AddHeader("authorization", "Bearer " + bearerToken);
                restRequest.AddHeader("cache-control", "no-cache");
                restRequest.RequestFormat = RestSharp.DataFormat.Json;

                IRestResponse restResponse = restClient.Execute(restRequest);

                if (string.IsNullOrEmpty(restResponse.Content))
                    return;

                try
                {
                    JObject jObject = JObject.Parse(restResponse.Content);
                    DataSet resultDs = SignerHelper.ReadDataFromJson(jObject.ToString());

                    if (resultDs != null && resultDs.Tables.Count > 0)
                        GridControl1.ItemsSource = resultDs.Tables[0].DefaultView;
                }
                catch
                {
                    // تجاهل خطأ التحليل
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في جلب البيانات: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// SimpleButton2 - بيانات الفاتورة
        /// </summary>
        private void SimpleButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EtaSetting.Active)
                {
                    DXMessageBox.Show("الرجاء تفعيل الفوترة الإلكترونية المصرية من الإعدادات",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txt_UUID.Text))
                {
                    DXMessageBox.Show("الرجاء تحديد فاتورة من القائمة أولًا",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SendEinvoice sendEinvoice = new SendEinvoice
                {
                    ActivationKey = "",
                    varEinvoiceType = EtaSetting.EinvoieType,
                    client_id = EtaSetting.EtaClientId,
                    client_secret_1 = EtaSetting.EtaClientSecret1,
                    client_secret_2 = EtaSetting.EtaClientSecret2
                };

                sendEinvoice.print_invoice(txt_UUID.Text);
                var EtaResultData = new EtaResultData();
                EtaResultData.GridControl1.DataSource = sendEinvoice.returnds?.Tables[0];
                EtaResultData.ShowDialog();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// SimpleButton3 - طباعة (فتح الرابط العام)
        /// </summary>
        private void SimpleButton3_Click(object sender, RoutedEventArgs e)
        {
            if (!EtaSetting.Active)
            {
                DXMessageBox.Show("الرجاء تفعيل الفوترة الإلكترونية المصرية من الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(publicUrl.Text))
            {
                DXMessageBox.Show("لا يوجد رابط للفاتورة المحددة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(publicUrl.Text)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في فتح الرابط: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// SimpleButton4 - رفض الفاتورة
        /// </summary>
        private void SimpleButton4_Click(object sender, RoutedEventArgs e)
        {
            if (!EtaSetting.Active)
            {
                DXMessageBox.Show("الرجاء تفعيل الفوترة الإلكترونية المصرية من الإعدادات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txt_UUID.Text))
            {
                DXMessageBox.Show("الرجاء تحديد فاتورة من القائمة أولًا",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من رفض هذه الفاتورة؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                SendEinvoice sendEinvoice = new SendEinvoice
                {
                    ActivationKey = "",
                    varEinvoiceType = EtaSetting.EinvoieType,
                    client_id = EtaSetting.EtaClientId,
                    client_secret_1 = EtaSetting.EtaClientSecret1,
                    client_secret_2 = EtaSetting.EtaClientSecret2
                };

                sendEinvoice.reject_invoice(txt_UUID.Text);

                DXMessageBox.Show("تم رفض الفاتورة بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// SimpleButton5 - إلغاء الفاتورة
        /// </summary>
        private void SimpleButton5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!EtaSetting.Active)
                {
                    DXMessageBox.Show("الرجاء تفعيل الفوترة الإلكترونية المصرية من الإعدادات",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txt_UUID.Text))
                {
                    DXMessageBox.Show("الرجاء تحديد فاتورة من القائمة أولًا",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من إلغاء هذه الفاتورة؟", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                SendEinvoice sendEinvoice = new SendEinvoice
                {
                    ActivationKey = "",
                    varEinvoiceType = EtaSetting.EinvoieType,
                    client_id = EtaSetting.EtaClientId,
                    client_secret_1 = EtaSetting.EtaClientSecret1,
                    client_secret_2 = EtaSetting.EtaClientSecret2
                };

                sendEinvoice.Cancel_invoice(txt_UUID.Text);

                DXMessageBox.Show("تم إلغاء الفاتورة بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DataGrid Events

        /// <summary>
        /// عند تغيير الصف المختار، تُحدَّث حقول UUID وpublicUrl
        /// (بديل GridView1_Click)
        /// </summary>
        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridControl1.SelectedItem is DataRowView selectedRow)
            {
                txt_UUID.Text = selectedRow.Row.Table.Columns.Contains("uuid")
                    ? selectedRow["uuid"]?.ToString() ?? ""
                    : "";

                publicUrl.Text = selectedRow.Row.Table.Columns.Contains("publicUrl")
                    ? selectedRow["publicUrl"]?.ToString() ?? ""
                    : "";
            }
        }

        #endregion
    }
}