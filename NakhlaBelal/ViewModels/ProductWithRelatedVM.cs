using Microsoft.AspNetCore.Mvc.Rendering;

namespace NakhlaBelal.ViewModels
{
    public class ProductWithRelatedVM
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = null!;
        public List<Product> TopProducts { get; set; } = null!;
        public List<Product> SimilarProducts { get; set; } = null!;
        public List<Product> RecentlyViewed { get; set; }
        public List<Product> MatchingProducts { get; set; }
        public List<ProductImage> ProductImages { get; set; }

        public IEnumerable<SelectListItem>? CompositionList { get; set; }

        public List<ProductCompositionVM> SelectedCompositions { get; set; } = new List<ProductCompositionVM>();

    }
}
