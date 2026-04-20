using NakhlaBelal.Models;

namespace NakhlaBelal.Utitlies
{
    public interface IAdminEmailNotifier
    {
        Task NotifyNewOrderAsync(Order order);
    }
}
