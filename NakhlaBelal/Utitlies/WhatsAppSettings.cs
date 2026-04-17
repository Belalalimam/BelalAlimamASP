namespace NakhlaBelal.Utitlies
{
    /// <summary>
    /// إعدادات WhatsApp Business Cloud API من Meta
    /// تُقرأ من appsettings.json → قسم "WhatsApp"
    /// </summary>
    public class WhatsAppSettings
    {
        /// <summary>
        /// رمز الوصول الدائم (System User Access Token) من Meta Business Manager
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// معرّف رقم الهاتف (Phone Number ID) من WhatsApp Business Platform
        /// </summary>
        public string PhoneNumberId { get; set; } = string.Empty;

        /// <summary>
        /// إصدار Graph API المستخدم (افتراضي v21.0)
        /// </summary>
        public string ApiVersion { get; set; } = "v21.0";

        /// <summary>
        /// كود الدولة الافتراضي إذا رقم العميل بدون كود (مثل 20 لمصر، 963 لسوريا)
        /// </summary>
        public string DefaultCountryCode { get; set; } = "20";

        /// <summary>
        /// اسم قالب الرسالة المعتمد من Meta لتأكيد الطلب
        /// </summary>
        public string OrderConfirmationTemplate { get; set; } = "order_confirmation";

        /// <summary>
        /// اسم قالب الرسالة المعتمد لتحديث حالة الطلب
        /// </summary>
        public string OrderStatusTemplate { get; set; } = "order_status_update";

        /// <summary>
        /// لغة القالب (مثل ar أو en_US)
        /// </summary>
        public string TemplateLanguage { get; set; } = "ar";

        /// <summary>
        /// تفعيل/تعطيل خدمة الواتساب (مفيد لتعطيلها في وضع التطوير)
        /// </summary>
        public bool Enabled { get; set; } = false;
    }
}
