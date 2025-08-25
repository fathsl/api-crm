using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    // Customer Models
    public class CustomerInfo
    {
        [Key]
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        [StringLength(100)]
        public string Email { get; set; }
        
        [StringLength(50)]
        public string VatNumber { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        public int CountryId { get; set; }
        
        // Navigation property
        public virtual Country Country { get; set; }
    }
    
     public class CustomerDto
    {
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string VatNumber { get; set; }
        public string Address { get; set; }
        public string CountryName { get; set; }
        public string Region { get; set; }
    }

   
}