namespace NakhlaBelal.ViewModels
{
    public class FilterVM
    {
        public string? Name { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public int? CategoryId { get; set; }
        public bool IsHot { get; set; }

        // الحقول الجديدة لربط الـ Navbar
        public int? FabricTypeId { get; set; }      // للفترة حسب "By Type"
        public int? ProjectCategoryId { get; set; } // للفلترة حسب "By Project"
        public int? ColorId { get; set; }           // للفلترة حسب "By Color"
        public int? CompositionId { get; set; }     // للفلترة حسب "By Composition"
    }
}