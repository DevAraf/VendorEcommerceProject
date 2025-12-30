namespace VendorEcommerceProject.Dtos.Vendor.Dashboard
{
    public class VendorDashboardSummaryDto
    {
        public int TotalProducts { get; set; }
        public int ApprovedProducts { get; set; }
        public int PendingProducts { get; set; }

        public decimal TotalEarnings { get; set; }
        public decimal PayableAmount { get; set; }
    }
}
