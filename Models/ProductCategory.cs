using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class ProductCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

        public string Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<ProductInfo> Products { get; set; }
    }

    public class ProductCategoryDto
    {
        public string CategoryName { get; set; }
        public int Stock { get; set; }
    }

}