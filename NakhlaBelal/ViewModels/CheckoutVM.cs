namespace NakhlaBelal.ViewModels
{
    public class CheckoutVM
    {
        public CartVM CartData { get; set; } = new();

        // بيانات الشحن التي سيتم إدخالها في الفورم
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
