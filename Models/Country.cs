using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class Country
    {
        [Key]
        public int CountryId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CountryName { get; set; }
        
        [StringLength(100)]
        public string Region { get; set; }
    }

    public class CountryDto
    {
        public string CountryName { get; set; }
        public string Region { get; set; }
    }
}