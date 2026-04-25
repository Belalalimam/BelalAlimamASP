namespace NakhlaBelal.Api.DTOs.Checkout;

public class OrderResultDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string? StripeCheckoutUrl { get; set; }
}
