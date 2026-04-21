using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.ViewModels
{
    /// <summary>
    /// إعدادات الموقع العامة — تُخزّن كملف JSON في wwwroot/app-data/site-settings.json
    /// </summary>
    public class SiteSettingsVM
    {
        // ============ General ============
        [Display(Name = "اسم الموقع (إنجليزي)")]
        public string SiteName { get; set; } = "Nakhla";

        [Display(Name = "اسم الموقع (عربي)")]
        public string SiteNameArabic { get; set; } = "نخلة";

        [Display(Name = "شعار التذييل")]
        public string Tagline { get; set; } = "متجر الأقمشة والمفروشات الأول";

        [Display(Name = "رابط اللوجو")]
        public string? LogoUrl { get; set; }

        [Display(Name = "رابط الأيقونة (Favicon)")]
        public string? FaviconUrl { get; set; }

        // ============ Contact ============
        [Display(Name = "البريد الإلكتروني للتواصل")]
        [EmailAddress(ErrorMessage = "بريد إلكتروني غير صحيح")]
        public string ContactEmail { get; set; } = "info@nakhla.com";

        [Display(Name = "رقم الهاتف الرئيسي")]
        public string ContactPhone { get; set; } = "+20 111 029 8574";

        [Display(Name = "رقم واتساب للدعم")]
        public string WhatsAppNumber { get; set; } = "+201110298574";

        [Display(Name = "العنوان")]
        public string Address { get; set; } = "القاهرة، مصر";

        [Display(Name = "ساعات العمل")]
        public string WorkingHours { get; set; } = "السبت - الخميس: 9:00 ص - 10:00 م";

        // ============ Social Media ============
        [Display(Name = "فيسبوك")]
        public string? FacebookUrl { get; set; }

        [Display(Name = "إنستجرام")]
        public string? InstagramUrl { get; set; }

        [Display(Name = "تويتر / X")]
        public string? TwitterUrl { get; set; }

        [Display(Name = "تيك توك")]
        public string? TikTokUrl { get; set; }

        [Display(Name = "يوتيوب")]
        public string? YouTubeUrl { get; set; }

        // ============ Business ============
        [Display(Name = "نسبة الضريبة (%)")]
        [Range(0, 100, ErrorMessage = "يجب أن تكون بين 0 و 100")]
        public decimal TaxRate { get; set; } = 8m;

        [Display(Name = "تكلفة الشحن الافتراضية")]
        [Range(0, 10000, ErrorMessage = "قيمة غير صالحة")]
        public decimal DefaultShippingCost { get; set; } = 50m;

        [Display(Name = "الحد الأدنى للشحن المجاني")]
        public decimal FreeShippingThreshold { get; set; } = 1000m;

        [Display(Name = "العملة")]
        public string Currency { get; set; } = "ج.م";

        [Display(Name = "كود العملة")]
        public string CurrencyCode { get; set; } = "EGP";

        // ============ SEO ============
        [Display(Name = "وصف meta للموقع")]
        public string? MetaDescription { get; set; }

        [Display(Name = "كلمات مفتاحية")]
        public string? MetaKeywords { get; set; }

        // ============ Maintenance ============
        [Display(Name = "تفعيل وضع الصيانة")]
        public bool MaintenanceMode { get; set; } = false;

        [Display(Name = "رسالة وضع الصيانة")]
        public string MaintenanceMessage { get; set; } = "الموقع تحت الصيانة حالياً. سنعود قريباً.";

        // ============ Footer ============
        [Display(Name = "نص حقوق النشر")]
        public string CopyrightText { get; set; } = "© 2025 نخلة. جميع الحقوق محفوظة.";

        // ============ Theme / Branding Colors ============
        [Display(Name = "اللون الأساسي")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "يجب أن يكون كود HEX صالح (مثل #013220)")]
        public string PrimaryColor { get; set; } = "#013220";

        [Display(Name = "اللون الثانوي")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "يجب أن يكون كود HEX صالح")]
        public string SecondaryColor { get; set; } = "#ffd700";

        [Display(Name = "لون التمييز (Accent)")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "يجب أن يكون كود HEX صالح")]
        public string AccentColor { get; set; } = "#e91e63";

        [Display(Name = "لون الأزرار")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "يجب أن يكون كود HEX صالح")]
        public string ButtonColor { get; set; } = "#013220";

        [Display(Name = "لون نص الأزرار")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "يجب أن يكون كود HEX صالح")]
        public string ButtonTextColor { get; set; } = "#ffffff";

        // ============ Metadata ============
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }
    }
}
