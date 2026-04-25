using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Api.DTOs.Checkout;

public class PlaceOrderRequest
{
    [Required] public string ShippingFirstName { get; set; } = "";
    [Required] public string ShippingLastName { get; set; } = "";
    [Required, EmailAddress] public string ShippingEmail { get; set; } = "";
    [Required, Phone] public string ShippingPhone { get; set; } = "";
    [Required] public string ShippingAddress { get; set; } = "";
    [Required] public string ShippingCity { get; set; } = "";
    [Required] public string ShippingState { get; set; } = "";
    [Required] public string ShippingZipCode { get; set; } = "";
    public string ShippingCountry { get; set; } = "Egypt";

    public bool BillingSameAsShipping { get; set; } = true;
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZipCode { get; set; }
    public string? BillingCountry { get; set; }

    [Required] public string PaymentMethod { get; set; } = "COD";
    public string? PromotionCode { get; set; }
    public decimal ShippingCost { get; set; } = 0;
    public string? CustomerNotes { get; set; }
}
