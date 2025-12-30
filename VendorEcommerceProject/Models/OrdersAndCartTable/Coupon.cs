using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VendorEcommerceProject.Models.OrdersAndCartTable
{
    public class Coupon : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CouponId { get; set; }

        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

<<<<<<< HEAD
<<<<<<< HEAD
        [Required, MaxLength(20)]
        public string Type { get; set; } = "fixed";
=======
=======
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853
        [Required]
        public CouponType Type { get; set; } = CouponType.Fixed;


        //[Required, MaxLength(20)]
        //public string Type { get; set; } = "fixed";
<<<<<<< HEAD
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853
=======
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);

        [Range(0, int.MaxValue)]
        public int UsageLimit { get; set; }

        public IList<Orders> Orders { get; set; } = new List<Orders>();
    }
<<<<<<< HEAD
<<<<<<< HEAD
=======
=======
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853

    public enum CouponType
    {
        Fixed = 1,
        Percentage = 2
    }

<<<<<<< HEAD
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853
=======
>>>>>>> d7ac3668574d0ed4a9b4db5298cd76938fba2853
}
