using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Api.DTOs.Cart;

public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public class ApplyPromoRequest
{
    [Required]
    public string Code { get; set; } = "";
}
