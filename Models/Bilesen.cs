namespace crmApi.Models
{
    public class BilesenRequestDto
    {
        public int? BilesenID { get; set; }
        public int KategoriID { get; set; }
        public string BilesenAdi { get; set; }
        public string? Birim { get; set; }
        public int? Stok { get; set; }
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
        public decimal? Fiyat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}