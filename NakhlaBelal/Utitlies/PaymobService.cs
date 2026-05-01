using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NakhlaBelal.Utitlies
{
    public class PaymobService : IPaymobService
    {
        private readonly PaymobSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymobService(IOptions<PaymobSettings> settings, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> CreatePaymentIntentionAsync(Order order, string paymentMethod)
        {
            var integrationId = DetermineIntegrationId(paymentMethod);
            var specialReference = RandomNumberGenerator.GetInt32(1000000, 9999999) + order.Id;
            var amountCents = (int)(order.TotalAmount * 100);

            var billingData = new
            {
                apartment = "N/A",
                first_name = order.ShippingFirstName ?? "N/A",
                last_name = order.ShippingLastName ?? "N/A",
                street = order.ShippingAddress ?? "N/A",
                building = "N/A",
                phone_number = order.ShippingPhone ?? "N/A",
                country = order.ShippingCountry ?? "EG",
                email = order.ShippingEmail ?? "N/A",
                floor = "N/A",
                state = order.ShippingState ?? "N/A",
                city = order.ShippingCity ?? "N/A"
            };

            var payload = new
            {
                amount = amountCents,
                currency = "EGP",
                payment_methods = new[] { integrationId },
                billing_data = billingData,
                items = new[]
                {
                    new
                    {
                        name = $"Order #{order.OrderNumber}",
                        amount = amountCents,
                        description = $"NakhlaBelal Order #{order.OrderNumber}",
                        quantity = 1
                    }
                },
                customer = new
                {
                    first_name = billingData.first_name,
                    last_name = billingData.last_name,
                    email = billingData.email,
                    extras = new { orderId = order.Id }
                },
                extras = new { orderId = order.Id },
                special_reference = specialReference,
                expiration = 3600,
                merchant_order_id = specialReference.ToString()
            };

            var httpClient = _httpClientFactory.CreateClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://accept.paymob.com/v1/intention/");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Token", _settings.SecretKey);
            requestMessage.Content = JsonContent.Create(payload);

            var response = await httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Paymob API error {response.StatusCode}: {responseContent}");

            var resultJson = JsonDocument.Parse(responseContent);
            var clientSecret = resultJson.RootElement.GetProperty("client_secret").GetString();

            order.PaymentTransactionId = specialReference.ToString();

            return $"https://accept.paymob.com/unifiedcheckout/?publicKey={_settings.PublicKey}&clientSecret={clientSecret}";
        }

        public async Task UpdateOrderSuccessAsync(string merchantOrderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentTransactionId == merchantOrderId);

            if (order == null) return;

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Processing";
            order.PaymentDate = DateTime.Now;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderFailedAsync(string merchantOrderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentTransactionId == merchantOrderId);

            if (order == null) return;

            order.PaymentStatus = "Failed";
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public string ComputeHmacSHA512(string data, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private int DetermineIntegrationId(string paymentMethod)
        {
            var idStr = paymentMethod?.ToLower() switch
            {
                "card" => _settings.CardIntegrationId,
                "wallet" => _settings.MobileIntegrationId,
                _ => throw new ArgumentException($"Invalid payment method: {paymentMethod}")
            };

            if (!int.TryParse(idStr, out int id))
                throw new ArgumentException($"Integration ID '{idStr}' is not a valid integer.");

            return id;
        }
    }
}
