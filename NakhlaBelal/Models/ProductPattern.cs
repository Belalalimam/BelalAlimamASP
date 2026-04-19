namespace NakhlaBelal.Models
{
    public enum ProductPattern
    {
        None = 0,
        Solid = 1,
        Striped = 2,
        Checkered = 3,
        Floral = 4,
        Printed = 5,
        Embroidered = 6
    }

    public static class ProductPatternExtensions
    {
        public static string ArabicLabel(this ProductPattern p) => p switch
        {
            ProductPattern.Solid       => "سادة",
            ProductPattern.Striped     => "مخطط",
            ProductPattern.Checkered   => "كاروهات",
            ProductPattern.Floral      => "ورود",
            ProductPattern.Printed     => "مطبوع",
            ProductPattern.Embroidered => "مطرز",
            _                          => "—"
        };

        public static string EnglishLabel(this ProductPattern p) => p switch
        {
            ProductPattern.Solid       => "Solid",
            ProductPattern.Striped     => "Striped",
            ProductPattern.Checkered   => "Checkered",
            ProductPattern.Floral      => "Floral",
            ProductPattern.Printed     => "Printed",
            ProductPattern.Embroidered => "Embroidered",
            _                          => "—"
        };
    }
}
