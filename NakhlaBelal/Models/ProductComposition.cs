using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NakhlaBelal.Models
{
    public class ProductComposition
    {
        public int id { get; set; }
        public int ProductId { get; set; }
        [ValidateNever]
        public Product Product { get; set; }

        public int CompositionId { get; set; }
        [ValidateNever]
        public Composition Composition { get; set; }

        public decimal Percentage { get; set; } // النسبة المئوية (مثلاً 58.00)

    }
}
