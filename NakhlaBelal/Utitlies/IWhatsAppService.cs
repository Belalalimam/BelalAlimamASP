using NakhlaBelal.Models;

namespace NakhlaBelal.Utitlies
{
    /// <summary>
    /// واجهة خدمة إرسال رسائل واتساب عبر WhatsApp Business Cloud API
    /// </summary>
    public interface IWhatsAppService
    {
        /// <summary>
        /// إرسال رسالة تأكيد استلام الطلب للعميل بعد إتمام عملية الشراء.
        /// </summary>
        /// <param name="order">الطلب الذي تم إنشاؤه</param>
        /// <param name="trackingUrl">رابط كامل لصفحة تتبع الطلب</param>
        /// <returns>(success, messageOrError) — true إذا نجح الإرسال</returns>
        Task<(bool Success, string Message)> SendOrderConfirmationAsync(Order order, string trackingUrl);

        /// <summary>
        /// إرسال رسالة تحديث حالة الطلب (تم الشحن، تم التوصيل، إلخ)
        /// </summary>
        Task<(bool Success, string Message)> SendOrderStatusUpdateAsync(Order order, string newStatusArabic, string trackingUrl);

        /// <summary>
        /// إرسال رسالة نصية حرة (فقط للعملاء الذين راسلونا خلال آخر 24 ساعة — سياسة Meta)
        /// </summary>
        Task<(bool Success, string Message)> SendTextMessageAsync(string phoneNumber, string text);

        /// <summary>
        /// إرسال قالب hello_world الافتراضي (معتمد تلقائياً من Meta — للاختبار فقط)
        /// </summary>
        Task<(bool Success, string Message)> SendHelloWorldAsync(string phoneNumber);
    }
}
