using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Models
{
    public class Notification
    {
        public int Id { get; set; }

        // المستلم (يوزر واحد per notification — منسوي record لكل admin/employee)
        [Required]
        public string RecipientUserId { get; set; } = string.Empty;
        public ApplicationUser? RecipientUser { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        // نوع الإشعار — NewOrder, OrderStatusChanged, ... (للتوسع لاحقاً)
        [MaxLength(50)]
        public string Type { get; set; } = "NewOrder";

        // رابط للإشعار (مثلاً صفحة تفاصيل الأوردر)
        [MaxLength(500)]
        public string? Url { get; set; }

        // ربط اختياري بالـ Order (NO ACTION عشان ما يصير cascade cycle مع SQL Server)
        public int? OrderId { get; set; }
        public Order? Order { get; set; }


        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
