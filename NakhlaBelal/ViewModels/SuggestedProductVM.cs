// ViewModels/SuggestedProductVM.cs
namespace NakhlaBelal.ViewModels
{
    public class SuggestedProductVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Plain fabrics matching" or "Fabrics on sale"
        public string Description { get; set; }
        public string Code { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsOnSale { get; set; }
        public string Category { get; set; } = "Fabric";
    }
}