using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.ViewModels
{
    public class LoginVM
    {
        public int Id { get; set; }

        [Required]
        public string UserNameOREmail { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
