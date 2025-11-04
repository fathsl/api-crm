using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class Offer
    {
        public int TeslimatID { get; set; }
        public int KullaniciID { get; set; }
        public int MusteriID { get; set; }
        public int KategoriID { get; set; }
        public int Miktar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int Fiyat { get; set; }
        public DateTime TeslimatTarihi { get; set; }
        public string? TeslimatBilgisi { get; set; }
    }

    public class CreateOfferDto
    {
        [Required(ErrorMessage = "KullaniciID is required")]
        public int KullaniciID { get; set; }

        [Required(ErrorMessage = "MusteriID is required")]
        public int MusteriID { get; set; }

        [Required(ErrorMessage = "KategoriID is required")]
        public int KategoriID { get; set; }

        [Required(ErrorMessage = "Miktar is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar must be at least 1")]
        public int Miktar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int Fiyat { get; set; }
        public string? TeslimatBilgisi { get; set; }
    }

    public class UpdateOfferDto
    {
        [Required(ErrorMessage = "KullaniciID is required")]
        public int KullaniciID { get; set; }

        [Required(ErrorMessage = "MusteriID is required")]
        public int MusteriID { get; set; }

        [Required(ErrorMessage = "KategoriID is required")]
        public int KategoriID { get; set; }

        [Required(ErrorMessage = "Miktar is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar must be at least 1")]
        public int Miktar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int Fiyat { get; set; }
        public string? TeslimatBilgisi { get; set; }
    }

    public class OfferResponseDto
    {
        public int TeslimatID { get; set; }
        public int KullaniciID { get; set; }
        public string? KullaniciAdi { get; set; }
        public int MusteriID { get; set; }
        public string? MusteriAd { get; set; }
        public int KategoriID { get; set; }
        public string? KategoriAdi { get; set; }
        public int Miktar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int Fiyat { get; set; }
        public DateTime TeslimatTarihi { get; set; }
        public string? TeslimatBilgisi { get; set; }
    }

    public class OfferDetailDto
    {
        public int TeslimatID { get; set; }
        public int KullaniciID { get; set; }
        public string? KullaniciAdi { get; set; }
        public int MusteriID { get; set; }
        public string? MusteriAd { get; set; }
        public int KategoriID { get; set; }
        public string? KategoriAdi { get; set; }
        public decimal? KategoriFiyat { get; set; }
        public int Miktar { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int Fiyat { get; set; }
        public decimal? ToplamTutar { get; set; }
        public DateTime TeslimatTarihi { get; set; }
        public string? TeslimatBilgisi { get; set; }
    }
}
