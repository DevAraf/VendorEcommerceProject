namespace VendorEcommerceProject.Dtos.Vendor.ProductVariants
{
    public class VendorProductVariantListDto
    {
        public long ProductVariantId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
        public int Quantity { get; set; }
    }
}
