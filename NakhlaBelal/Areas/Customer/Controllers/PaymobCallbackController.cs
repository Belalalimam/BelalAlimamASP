using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NakhlaBelal.Utitlies;
using System.Text;
using System.Text.Json;

namespace NAKHLA.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class PaymobCallbackController : Controller
    {
        private readonly IPaymobService _paymobService;
        private readonly PaymobSettings _settings;

        public PaymobCallbackController(IPaymobService paymobService, IOptions<PaymobSettings> settings)
        {
            _paymobService = paymobService;
            _settings = settings.Value;
        }

        // Browser redirect after payment on Paymob page
        // Configure in Paymob Dashboard as: https://yourdomain.com/Customer/PaymobCallback/Callback
        [HttpGet]
        public IActionResult Callback()
        {
            var query = Request.Query;

            string[] fields = new[]
            {
                "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
                "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
                "is_standalone_payment", "is_voided", "order", "owner", "pending",
                "source_data.pan", "source_data.sub_type", "source_data.type", "success"
            };

            var concatenated = new StringBuilder();
            foreach (var field in fields)
            {
                concatenated.Append(query.TryGetValue(field, out var value) ? value.ToString() : "");
            }

            string receivedHmac = query["hmac"].ToString();
            string calculatedHmac = _paymobService.ComputeHmacSHA512(concatenated.ToString(), _settings.HMAC);

            if (!receivedHmac.Equals(calculatedHmac, StringComparison.OrdinalIgnoreCase))
            {
                return Content(BuildHtml("خطأ أمني", "تحقق HMAC فشل. يرجى التواصل مع الدعم.", false), "text/html");
            }

            bool.TryParse(query["success"], out bool isSuccess);

            if (isSuccess)
                return Content(BuildHtml("تم الدفع بنجاح", "شكراً! تمت عملية الدفع بنجاح. سيتم معالجة طلبك قريباً.", true), "text/html");

            return Content(BuildHtml("فشل الدفع", "عذراً، فشلت عملية الدفع. يرجى المحاولة مرة أخرى.", false), "text/html");
        }

        // Server-to-server callback from Paymob (authoritative DB update)
        // Configure in Paymob Dashboard as: https://yourdomain.com/Customer/PaymobCallback/ServerCallback
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ServerCallback([FromBody] JsonElement payload)
        {
            try
            {
                string receivedHmac = Request.Query["hmac"].ToString();

                if (!payload.TryGetProperty("obj", out var obj))
                    return BadRequest("Missing 'obj'");

                string[] fields = new[]
                {
                    "amount_cents", "created_at", "currency", "error_occured", "has_parent_transaction",
                    "id", "integration_id", "is_3d_secure", "is_auth", "is_capture", "is_refunded",
                    "is_standalone_payment", "is_voided", "order.id", "owner", "pending",
                    "source_data.pan", "source_data.sub_type", "source_data.type", "success"
                };

                var concatenated = new StringBuilder();
                foreach (var field in fields)
                {
                    string[] parts = field.Split('.');
                    JsonElement current = obj;
                    bool found = true;

                    foreach (var part in parts)
                    {
                        if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
                            current = next;
                        else { found = false; break; }
                    }

                    if (!found || current.ValueKind == JsonValueKind.Null)
                        concatenated.Append("");
                    else if (current.ValueKind == JsonValueKind.True || current.ValueKind == JsonValueKind.False)
                        concatenated.Append(current.GetBoolean() ? "true" : "false");
                    else
                        concatenated.Append(current.ToString());
                }

                string calculatedHmac = _paymobService.ComputeHmacSHA512(concatenated.ToString(), _settings.HMAC);

                if (!receivedHmac.Equals(calculatedHmac, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized("Invalid HMAC");

                string? merchantOrderId = null;
                if (obj.TryGetProperty("order", out var order) &&
                    order.TryGetProperty("merchant_order_id", out var merchantOrderIdEl) &&
                    merchantOrderIdEl.ValueKind != JsonValueKind.Null)
                {
                    merchantOrderId = merchantOrderIdEl.ToString();
                }

                bool isSuccess = obj.TryGetProperty("success", out var successEl) && successEl.GetBoolean();

                if (!string.IsNullOrEmpty(merchantOrderId))
                {
                    if (isSuccess)
                        await _paymobService.UpdateOrderSuccessAsync(merchantOrderId);
                    else
                        await _paymobService.UpdateOrderFailedAsync(merchantOrderId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private static string BuildHtml(string title, string message, bool success)
        {
            string color = success ? "#22c55e" : "#ef4444";
            string icon = success ? "✓" : "✗";
            return $@"<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1.0'>
  <title>{title}</title>
  <style>
    body {{ font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #f9fafb; }}
    .card {{ background: white; border-radius: 12px; padding: 48px 40px; text-align: center; box-shadow: 0 4px 24px rgba(0,0,0,0.1); max-width: 400px; }}
    .icon {{ font-size: 64px; color: {color}; margin-bottom: 16px; }}
    h1 {{ color: {color}; margin: 0 0 12px; }}
    p {{ color: #6b7280; margin: 0 0 24px; }}
    a {{ background: {color}; color: white; padding: 12px 32px; border-radius: 8px; text-decoration: none; font-weight: bold; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='icon'>{icon}</div>
    <h1>{title}</h1>
    <p>{message}</p>
    <a href='/'>العودة للرئيسية</a>
  </div>
</body>
</html>";
        }
    }
}
