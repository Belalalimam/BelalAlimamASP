using NakhlaBelal.Models;

namespace NakhlaBelal.Utitlies
{
    public interface IPaymobService
    {
        Task<string> CreatePaymentIntentionAsync(Order order, string paymentMethod);
        Task UpdateOrderSuccessAsync(string merchantOrderId);
        Task UpdateOrderFailedAsync(string merchantOrderId);
        string ComputeHmacSHA512(string data, string secret);
    }
}
