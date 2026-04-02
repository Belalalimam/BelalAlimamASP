using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.ViewModels
{
    public class ApplicationUserVM
    {
        // عرض فقط
        public string Email { get; set; } = string.Empty;

        // حقول قابلة للتعديل
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? FullName => $"{FirstName} {LastName}"; // خاصية للقراءة فقط

        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }

        // تغيير كلمة المرور
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
