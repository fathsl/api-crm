using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace crmApi.Models
{
    public class OrderDetail
    {
        [Key]
        public int DetailID { get; set; }

        [ForeignKey("OrderInfo")]
        public int OrderID { get; set; }

        [ForeignKey("ProductInfo")]
        public int ProductID { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation Properties
        public virtual OrderInfo Order { get; set; }
        public virtual ProductInfo Product { get; set; }
    }

    public class OrderComponentRelation
    {
        [Key]
        public int RelationID { get; set; }

        [ForeignKey("OrderInfo")]
        public int OrderID { get; set; }

        [ForeignKey("ComponentInfo")]
        public int ComponentID { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public virtual OrderInfo Order { get; set; }
        public virtual ComponentInfo Component { get; set; }
    }
}