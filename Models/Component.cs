using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crmApi.Models
{
    public class ComponentInfo
    {
        [Key]
        public int ComponentID { get; set; }

        [Required]
        [StringLength(200)]
        public string ComponentName { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int StockQuantity { get; set; }

        [StringLength(50)]
        public string Unit { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<OrderComponentRelation> OrderRelations { get; set; }
    }

    public class ComponentDto
    {
        public int ComponentId { get; set; }
        public string ComponentName { get; set; }
        public int Stock { get; set; }
    }

    public class ComponentKeyValueDto
    {
        public int Key { get; set; }
        public string Value { get; set; }
    }

}