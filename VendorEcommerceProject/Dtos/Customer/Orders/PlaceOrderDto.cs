namespace VendorEcommerceProject.Dtos.Customer.Orders
{
    public class PlaceOrderDto
    {
        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public string Country { get; set; } = null!;

        public bool SetAsDefault { get; set; } = true;
    }
}
