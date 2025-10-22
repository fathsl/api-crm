namespace crmApi.Models
{
    public class BilesenRequestDto
    {
        public int? BilesenID { get; set; }
        public int KategoriID { get; set; }
        public string BilesenAdi { get; set; }
        public string? Birim { get; set; }
        public int? Stok { get; set; }
        public string? Currency { get; set; }
        public int Adet { get; set; } = 1;
        public decimal? Fiyat { get; set; }
    }

    public class BilesenResponseDto
    {
        public int BilesenID { get; set; }
        public int KategoriID { get; set; }
        public string KategoriAdi { get; set; }
        public string BilesenAdi { get; set; }
        public string? Birim { get; set; }
        public int? Adet { get; set; }
        public int? Stok { get; set; }
        public string Currency { get; set; }
        public decimal? Fiyat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ComponentToAdd
    {
        public int BilesenID { get; set; }
        public int Adet { get; set; }
    }

    public class AddComponentsToCategoryRequest
    {
        public int KategoriID { get; set; }
        public List<ComponentToAdd> Components { get; set; }
    }

}