using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartAuditERP
{
    public class SupportTicketApiClient
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        #endregion

        #region Constructor

        public SupportTicketApiClient(string apiBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        #endregion

        #region Public Methods

        public async Task<TicketResponse> CreateTicketAsync(
            string customerName,
            string customerEmail,
            string customerPhone,
            string subject,
            string description,
            string priority,
            string category,
            string ticketType,
            int branch,
            List<string> attachments)
        {
            try
            {
                var attachmentDtos = new List<TicketAttachmentDto>();

                if (attachments != null)
                {
                    foreach (string filePath in attachments)
                    {
                        string fileContent =
                            Convert.ToBase64String(File.ReadAllBytes(filePath));

                        attachmentDtos.Add(new TicketAttachmentDto
                        {
                            FileName = Path.GetFileName(filePath),
                            FileType = Path.GetExtension(filePath).Replace(".", ""),
                            FileContent = fileContent
                        });
                    }
                }

                var dto = new CreateTicketDto
                {
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    CustomerPhone = customerPhone,
                    Subject = subject,
                    Description = description,
                    Priority = priority,
                    Category = category,
                    Type = ticketType,
                    Branch = branch,
                    Attachments = attachmentDtos
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient
                    .PostAsync($"{_apiBaseUrl}/api/Tickets/Create", content);

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse =
                        JsonConvert.DeserializeObject<ApiResponse>(responseBody);

                    return new TicketResponse
                    {
                        Success = true,
                        Message = apiResponse.Message,
                        TicketNumber = apiResponse.TicketNumber
                    };
                }

                return new TicketResponse
                {
                    Success = false,
                    Message = $"فشل إنشاء التذكرة ({response.StatusCode})"
                };
            }
            catch (Exception ex)
            {
                return new TicketResponse
                {
                    Success = false,
                    Message = "خطأ: " + ex.Message
                };
            }
        }

        public async Task<TicketDetailsResponse> GetTicketAsync(string ticketNumber)
        {
            try
            {
                var response = await _httpClient
                    .GetAsync($"{_apiBaseUrl}/api/Tickets/Number/{ticketNumber}");

                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JsonConvert.DeserializeObject<TicketDetailsApiResponse>(responseBody);
                    return new TicketDetailsResponse { Success = true };
                }

                return new TicketDetailsResponse
                {
                    Success = false,
                    Message = "التذكرة غير موجودة"
                };
            }
            catch (Exception ex)
            {
                return new TicketDetailsResponse
                {
                    Success = false,
                    Message = "حدث خطأ: " + ex.Message
                };
            }
        }

        public async Task<List<TicketDetails>> GetCustomerTicketsAsync(string customerName)
        {
            try
            {
                var response = await _httpClient
                    .GetAsync($"{_apiBaseUrl}/api/Tickets/GetTicketsBycustomer" +
                              $"?cust={customerName}");

                if (response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<TicketDetails>>(body)
                           ?? new List<TicketDetails>();
                }

                return new List<TicketDetails>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"خطأ في GetCustomerTicketsAsync: {ex.Message}");
                return new List<TicketDetails>();
            }
        }

        public async Task<ApiResponseBase> AddReplyAsync(
            int ticketID,
            string senderName,
            string senderType,
            string message)
        {
            try
            {
                var payload = new
                {
                    TicketId = ticketID,
                    SenderName = senderName,
                    SenderType = senderType,
                    Message = message
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient
                    .PostAsync($"{_apiBaseUrl}/api/Tickets/AddReply", content);

                await response.Content.ReadAsStringAsync();

                return response.IsSuccessStatusCode
                    ? new ApiResponseBase
                    {
                        Success = true,
                        Message = "تم إضافة الرد بنجاح"
                    }
                    : new ApiResponseBase
                    {
                        Success = false,
                        Message = "فشل إضافة الرد"
                    };
            }
            catch (Exception ex)
            {
                return new ApiResponseBase
                {
                    Success = false,
                    Message = "حدث خطأ: " + ex.Message
                };
            }
        }

        #endregion
    }
}