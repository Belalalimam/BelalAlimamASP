namespace NakhlaBelal.Models
{
    public enum ProductUnit
    {
        Piece = 0,
        Meter = 1,
        Kilogram = 2,
        Yard = 3
    }

    public static class ProductUnitExtensions
    {
        public static string ShortLabel(this ProductUnit unit) => unit switch
        {
            ProductUnit.Meter => "m",
            ProductUnit.Kilogram => "kg",
            ProductUnit.Yard => "yd",
            ProductUnit.Piece => "pc",
            _ => ""
        };
    }
}
