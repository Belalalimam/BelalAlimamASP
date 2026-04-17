using Microsoft.Extensions.Options;
using NakhlaBelal.Models;
using System.Text;
using System.Text.Json;

namespace NakhlaBelal.Utitlies
{
    /// <summary>
    /// تنفيذ خدمة إرسال الرسائل عبر WhatsApp Business Cloud API من Meta
    /// الوثائق الرسمية: https://developers.facebook.com/docs/whatsapp/cloud-api
    /// </summary>
    public class WhatsAppService : IWhatsAppService
    {
        private readonly WhatsAppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(
            IOptions<WhatsAppSettings> settings,
            HttpClient httpClient,
            ILogger<WhatsAppService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        // =========================================================================
        // ORDER CONFIRMATION (بعد إنشاء الطلب)
        // =========================================================================
        public async Task<(bool Success, string Message)> SendOrderConfirmationAsync(Order order, string trackingUrl)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("WhatsApp service is disabled. Skipping message for order {OrderNumber}.", order.OrderNumber);
                return (false, "WhatsApp service is disabled");
            }

            var phone = NormalizePhone(order.ShippingPhone);
            if (string.IsNullOrEmpty(phone))
                return (false, "Invalid phone number");

            var customerName = order.FullName?.Trim();
            if (string.IsNullOrWhiteSpace(customerName)) customerName = "عميلنا العزيز";

            // بناء payload لقالب معتمد من Meta
            // المتغيرات: {{1}} اسم العميل، {{2}} رقم الطلب، {{3}} الإجمالي، {{4}} رابط التتبع
            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "template",
                template = new
                {
                    name = _settings.OrderConfirmationTemplate,
                    language = new { code = _settings.TemplateLanguage },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = customerName },
                                new { type = "text", text = order.OrderNumber },
                                new { type = "text", text = order.TotalAmount.ToString("N2") },
                                new { type = "text", text = trackingUrl }
                            }
                        }
                    }
                }
            };

            return await SendAsync(payload, $"order-confirmation:{order.OrderNumber}");
        }

        // =========================================================================
        // STATUS UPDATE (تحديث حالة الطلب)
        // =========================================================================
        public async Task<(bool Success, string Message)> SendOrderStatusUpdateAsync(Order order, string newStatusArabic, string trackingUrl)
        {
            if (!_settings.Enabled)
                return (false, "WhatsApp service is disabled");

            var phone = NormalizePhone(order.ShippingPhone);
            if (string.IsNullOrEmpty(phone))
                return (false, "Invalid phone number");

            var customerName = order.FullName?.Trim();
            if (string.IsNullOrWhiteSpace(customerName)) customerName = "عميلنا العزيز";

            // المتغيرات: {{1}} اسم العميل، {{2}} رقم الطلب، {{3}} الحالة الجديدة، {{4}} رابط التتبع
            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "template",
                template = new
                {
                    name = _settings.OrderStatusTemplate,
                    language = new { code = _settings.TemplateLanguage },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = customerName },
                                new { type = "text", text = order.OrderNumber },
                                new { type = "text", text = newStatusArabic },
                                new { type = "text", text = trackingUrl }
                            }
                        }
                    }
                }
            };

            return await SendAsync(payload, $"status-update:{order.OrderNumber}");
        }

        // =========================================================================
        // FREE TEXT MESSAGE (ضمن نافذة 24 ساعة فقط)
        // =========================================================================
        public async Task<(bool Success, string Message)> SendTextMessageAsync(string phoneNumber, string text)
        {
            if (!_settings.Enabled)
                return (false, "WhatsApp service is disabled");

            var phone = NormalizePhone(phoneNumber);
            if (string.IsNullOrEmpty(phone))
                return (false, "Invalid phone number");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "text",
                text = new { body = text }
            };

            return await SendAsync(payload, $"text:{phone}");
        }

        // =========================================================================
        // HELPER: إرسال HTTP POST إلى Meta Graph API
        // =========================================================================
        private async Task<(bool Success, string Message)> SendAsync(object payload, string contextLabel)
        {
            try
            {
                var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";
                var json = JsonSerializer.Serialize(payload);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {_settings.AccessToken}");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp message sent successfully ({Context}). Response: {Response}", contextLabel, responseBody);
                    return (true, responseBody);
                }

                _logger.LogWarning("WhatsApp send failed ({Context}). Status: {Status}. Response: {Response}",
                    contextLabel, response.StatusCode, responseBody);
                return (false, $"Status {(int)response.StatusCode}: {responseBody}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending WhatsApp message ({Context})", contextLabel);
                return (false, ex.Message);
            }
        }

        // =========================================================================
        // HELPER: تنظيف رقم الهاتف — إزالة الرموز + إضافة كود الدولة إن لزم
        // =========================================================================
        private string NormalizePhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            // إزالة كل شيء ما عدا الأرقام
            var digits = new string(raw.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(digits)) return string.Empty;

            // إذا الرقم يبدأ بـ 00 (بعض الدول) نستبدلها
            if (digits.StartsWith("00"))
                digits = digits.Substring(2);

            // إذا الرقم يبدأ بصفر (رقم محلي) نضيف كود الدولة
            if (digits.StartsWith("0"))
                digits = _settings.DefaultCountryCode + digits.Substring(1);

            // إذا قصير جداً، غالباً ما أضاف العميل كود الدولة
            if (digits.Length <= 10 && !digits.StartsWith(_settings.DefaultCountryCode))
                digits = _settings.DefaultCountryCode + digits;

            return digits;
        }
    }
}
