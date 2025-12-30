namespace VendorEcommerceProject.Dtos.Vendor.Dashboard
{
    public class VendorRecentOrderDto
    {
        public long OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
