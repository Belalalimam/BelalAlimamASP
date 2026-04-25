namespace NakhlaBelal.Api.DTOs.Wishlist;

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductImage { get; set; } = "";
    public string ProductSlug { get; set; } = "";
    public decimal FinalPrice { get; set; }
    public bool IsOutOfStock { get; set; }
}
