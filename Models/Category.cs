using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class Category
    {
        public int KategoriID { get; set; }
        [Required]
        public string KategoriAdi { get; set; } = null!;
        public float? Stok { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public float Fiyat { get; set; }
    }

    public class CategoryResponseDto
    {
        public int KategoriID { get; set; }
        public string? KategoriAdi { get; set; }
        public int? Stok { get; set; }
        public decimal Fiyat { get; set; }
        public string? Currency { get; set; } = "TRY";
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CategoryDetailDto
    {
        public int KategoriID { get; set; }
        public string? KategoriAdi { get; set; }
        public int? Stok { get; set; }
        public decimal Fiyat { get; set; }
        public string? Currency { get; set; } = "TRY";
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateCategoryDto
    {
        public string? KategoriAdi { get; set; }
        public int? Stok { get; set; }
        public string? Currency { get; set; } = "TRY";
        public decimal Fiyat { get; set; }
        public int CreatedBy { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class UpdateCategoryDto
    {
        public string? KategoriAdi { get; set; }
        public int? Stok { get; set; }
        public decimal Fiyat { get; set; }
        public string? Currency { get; set; } = "TRY";
        public int UpdatedBy { get; set; }
        public IFormFile? Image { get; set; }
    }

}