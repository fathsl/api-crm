using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crmApi.Models
{
    public class ProductInfo
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [ForeignKey("ProductCategory")]
        public int CategoryID { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ProductCategory Category { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }

}