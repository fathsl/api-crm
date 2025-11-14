using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class Offer
    {
        public int SiparisAlID { get; set; }
        public string? SiparisNo { get; set; }
        public string? TeklifNo { get; set; }
        public int? MusteriId { get; set; }
        public string? SiparisNotu { get; set; }
        public int? KullaniciID { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? MusteriAd { get; set; }
        
        // Supplier/Seller Information
        public string? STelefon { get; set; }
        public string? SFax { get; set; }
        public string? SWeb { get; set; }
        public string? SE_Mail { get; set; }
        public string? SVatNumarasi { get; set; }
        public string? SUlke { get; set; }
        public string? SAdres { get; set; }
        
        // Delivery Information
        public string? Montaj { get; set; }
        public string? Teslimat { get; set; }
        public decimal? MontajFiyat { get; set; }
        public int PesinYüzde { get; set; }
        public decimal? TeslimatFiyat { get; set; }
        public string? TeslimatÇeşiti { get; set; }
        
        // Financial Information
        public string? ParaTipi { get; set; }
        public decimal Indirim { get; set; }
        public decimal Kdv { get; set; }
        public decimal ToplamFiyat { get; set; }
        public decimal BrutToplamFiyat { get; set; }
        public decimal OdenenMiktar { get; set; }
        public string OdemeDurum { get; set; } = "Beklemede";
        
        // Quantities
        public int MAdet { get; set; } = 1;
        public int MTeslimat { get; set; } = 1;
        
        // Delivery Address Information
        public string? TTeslimAlanAdi { get; set; }
        public string? TIlgiliKisi { get; set; }
        public string? TTelefon { get; set; }
        public string? TAltTelefon { get; set; }
        public string? TE_Mail { get; set; }
        public string? TVATNumarasi { get; set; }
        public string? TZipKod { get; set; }
        public string TUlke { get; set; } = string.Empty;
        public string? TAdres { get; set; }
        
        // Status and Control
        public DateTime Tarih { get; set; }
        public string Kontrol { get; set; } = "Kontrol";
        public byte[]? Ek { get; set; }
        public byte[]? MuhasebeEk { get; set; }
        public bool Muhasebe { get; set; }
        public bool Fabrika { get; set; }
        public bool SatinAlma { get; set; }
        public bool Uretim { get; set; }
        public bool Lojistik { get; set; }
        public string? Red { get; set; }
        public string? SiparisMiTeklifMi { get; set; }
        public string Sirket { get; set; } = "Unixpadel";
        public DateTime? STarih { get; set; }
        public string? KontrolNot { get; set; }
        public string? FabrikaNot { get; set; }
        public string? LojistikNot { get; set; }
    }

    public class CreateOfferDto
    {
        public int? MusteriId { get; set; }
        public int? ClientId { get; set; }
        public string? SiparisNo { get; set; }
        public string? TeklifNo { get; set; }
        public string? SiparisNotu { get; set; }
        public int? KullaniciID { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? STelefon { get; set; }
        public string? SFax { get; set; }
        public string? SWeb { get; set; }
        public string? SE_Mail { get; set; }
        public string? SVatNumarasi { get; set; }
        public string? SUlke { get; set; }
        public string? SAdres { get; set; }
        public string? Montaj { get; set; }
        public string? Teslimat { get; set; }
        public decimal? MontajFiyat { get; set; }
        public int PesinYüzde { get; set; }
        public decimal? TeslimatFiyat { get; set; }
        public string? TeslimatÇeşiti { get; set; }
        public string? ParaTipi { get; set; }
        public decimal? Indirim { get; set; }
        public decimal? Kdv { get; set; }
        public decimal ToplamFiyat { get; set; }
        public decimal? BrutToplamFiyat { get; set; }
        public decimal? OdenenMiktar { get; set; }
        public string? OdemeDurum { get; set; }
        public int? MAdet { get; set; }
        public int? MTeslimat { get; set; }
        public DateTime? Tarih { get; set; }
        public string? Kontrol { get; set; }
        public string? Ek { get; set; }
        public string? MuhasebeEk { get; set; }
        public bool? Muhasebe { get; set; }
        public bool? Fabrika { get; set; }
        public bool? SatinAlma { get; set; }
        public bool? Uretim { get; set; }
        public bool? Lojistik { get; set; }
        public string? Red { get; set; }
        public string? SiparisMiTeklifMi { get; set; }
        public string? Sirket { get; set; }
        public DateTime? STarih { get; set; }
        public string? KontrolNot { get; set; }
        public string? FabrikaNot { get; set; }
        public string? LojistikNot { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateOfferDto
    {
        public string? SiparisNo { get; set; }
        public string? TeklifNo { get; set; }
        
        public int? MusteriId { get; set; }
        public string? MusteriAd { get; set; }
        public string? MTelefon { get; set; }
        public string? MMail { get; set; }
        public string? MUlke { get; set; }
        public string? MVATNumarasi { get; set; }
        public string? MZipKod { get; set; }
        public string? MAdres { get; set; }
        
        public string? SiparisNotu { get; set; }
        
        [Required]
        public int KullaniciID { get; set; }
        
        public string? STelefon { get; set; }
        public string? SFax { get; set; }
        public string? SWeb { get; set; }
        public string? SE_Mail { get; set; }
        public string? SVatNumarasi { get; set; }
        public string? SUlke { get; set; }
        public string? SAdres { get; set; }
        
        public string? Montaj { get; set; }
        public string? Teslimat { get; set; }
        public decimal? MontajFiyat { get; set; }
        
        [Required]
        public int PesinYüzde { get; set; }
        
        public decimal? TeslimatFiyat { get; set; }
        public string? TeslimatÇeşiti { get; set; }
        public string? ParaTipi { get; set; }
        public decimal Indirim { get; set; }
        public decimal Kdv { get; set; }
        
        [Required]
        public decimal ToplamFiyat { get; set; }
        
        public decimal BrutToplamFiyat { get; set; }
        public decimal OdenenMiktar { get; set; }
        public string OdemeDurum { get; set; } = "Beklemede";
        
        public int MAdet { get; set; }
        public int MTeslimat { get; set; }
        
        public string? TTeslimAlanAdi { get; set; }
        public string? TIlgiliKisi { get; set; }
        public string? TTelefon { get; set; }
        public string? TAltTelefon { get; set; }
        public string? TE_Mail { get; set; }
        public string? TVATNumarasi { get; set; }
        public string? TZipKod { get; set; }
        
        [Required]
        public string TUlke { get; set; } = string.Empty;
        
        public string? TAdres { get; set; }
        public string Kontrol { get; set; } = "Kontrol";
        public bool Muhasebe { get; set; }
        public bool Fabrika { get; set; }
        public bool SatinAlma { get; set; }
        public bool Uretim { get; set; }
        public bool Lojistik { get; set; }
        public string? Red { get; set; }
        public string? SiparisMiTeklifMi { get; set; }
        public string Sirket { get; set; } = "Unixpadel";
        public DateTime? STarih { get; set; }
        public string? KontrolNot { get; set; }
        public string? FabrikaNot { get; set; }
        public string? LojistikNot { get; set; }
    }

    public class OfferResponseDto
    {
        public int SiparisAlID { get; set; }
        public int? MusteriId { get; set; }
        public int? ClientId { get; set; }
        public string? SiparisNo { get; set; }
        public string? TeklifNo { get; set; }
        public string? MusteriAd { get; set; }
        public string? MTelefon { get; set; }
        public string? MMail { get; set; }
        public string? MUlke { get; set; }
        public string? MVATNumarasi { get; set; }
        public string? MZipKod { get; set; }
        public string? MAdres { get; set; }
        public string? SiparisNotu { get; set; }
        public int? KullaniciID { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Supplier Information
        public string? STelefon { get; set; }
        public string? SFax { get; set; }
        public string? SWeb { get; set; }
        public string? SE_Mail { get; set; }
        public string? SVatNumarasi { get; set; }
        public string? SUlke { get; set; }
        public string? SAdres { get; set; }
        
        // Montaj and Teslimat
        public string? Montaj { get; set; }
        public string? Teslimat { get; set; }
        public decimal? MontajFiyat { get; set; }
        public int PesinYüzde { get; set; }
        public decimal? TeslimatFiyat { get; set; }
        public string? TeslimatÇeşiti { get; set; }
        
        // Financial Information
        public string? ParaTipi { get; set; }
        public decimal? Indirim { get; set; }
        public decimal? Kdv { get; set; }
        public decimal ToplamFiyat { get; set; }
        public decimal? BrutToplamFiyat { get; set; }
        public decimal? OdenenMiktar { get; set; }
        public string OdemeDurum { get; set; }
        public decimal KaparoFiyat { get; set; }
        public decimal KalanBakiye { get; set; }
        
        // Order Details
        public int? MAdet { get; set; }
        public int? MTeslimat { get; set; }
        
        // Delivery Information
        public string? TTeslimAlanAdi { get; set; }
        public string? TIlgiliKisi { get; set; }
        public string? TTelefon { get; set; }
        public string? TAltTelefon { get; set; }
        public string? TE_Mail { get; set; }
        public string? TVATNumarasi { get; set; }
        public string? TZipKod { get; set; }
        public string? TUlke { get; set; }
        public string? TAdres { get; set; }
        
        // Dates
        public string Tarih { get; set; }
        public string? STarih { get; set; }
        
        // Status and Control
        public string Kontrol { get; set; }
        public bool Muhasebe { get; set; }
        public bool Fabrika { get; set; }
        public bool SatinAlma { get; set; }
        public bool Uretim { get; set; }
        public bool Lojistik { get; set; }
        public string? Red { get; set; }
        public string? SiparisMiTeklifMi { get; set; }
        public string? Sirket { get; set; }
        public string? Status { get; set; }
        
        // Notes
        public string? KontrolNot { get; set; }
        public string? FabrikaNot { get; set; }
        public string? LojistikNot { get; set; }
        
        // User Information
        public string? KullaniciAdi { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class OfferDetailDto
    {
        public int SiparisAlID { get; set; }
        public string? SiparisNo { get; set; }
        public string? TeklifNo { get; set; }
        public int? MusteriId { get; set; }
        public string? MusteriAd { get; set; }
        public string? MTelefon { get; set; }
        public string? MMail { get; set; }
        public string? MUlke { get; set; }
        public string? MVATNumarasi { get; set; }
        public string? MZipKod { get; set; }
        public string? MAdres { get; set; }
        public string? SiparisNotu { get; set; }
        public int? KullaniciID { get; set; }
        public string? KullaniciAdi { get; set; }
        
        public string? STelefon { get; set; }
        public string? SFax { get; set; }
        public string? SWeb { get; set; }
        public string? SE_Mail { get; set; }
        public string? SVatNumarasi { get; set; }
        public string? SUlke { get; set; }
        public string? SAdres { get; set; }
        
        public string? Montaj { get; set; }
        public string? Teslimat { get; set; }
        public decimal? MontajFiyat { get; set; }
        public int PesinYüzde { get; set; }
        public decimal? TeslimatFiyat { get; set; }
        public string? TeslimatÇeşiti { get; set; }
        
        public string? ParaTipi { get; set; }
        public decimal Indirim { get; set; }
        public decimal Kdv { get; set; }
        public decimal ToplamFiyat { get; set; }
        public decimal BrutToplamFiyat { get; set; }
        public decimal OdenenMiktar { get; set; }
        public decimal KaparoFiyat { get; set; }
        public decimal KalanBakiye { get; set; }
        public string OdemeDurum { get; set; } = "Beklemede";
        
        public int MAdet { get; set; }
        public int MTeslimat { get; set; }
        
        public string? TTeslimAlanAdi { get; set; }
        public string? TIlgiliKisi { get; set; }
        public string? TTelefon { get; set; }
        public string? TAltTelefon { get; set; }
        public string? TE_Mail { get; set; }
        public string? TVATNumarasi { get; set; }
        public string? TZipKod { get; set; }
        public string? TUlke { get; set; }
        public string? TAdres { get; set; }
        
        public DateTime Tarih { get; set; }
        public string Kontrol { get; set; } = "Kontrol";
        public bool Muhasebe { get; set; }
        public bool Fabrika { get; set; }
        public bool SatinAlma { get; set; }
        public bool Uretim { get; set; }
        public bool Lojistik { get; set; }
        public string? Red { get; set; }
        public string? SiparisMiTeklifMi { get; set; }
        public string Sirket { get; set; } = "Unixpadel";
        public DateTime? STarih { get; set; }
        public string? KontrolNot { get; set; }
        public string? FabrikaNot { get; set; }
        public string? LojistikNot { get; set; }
    }
}
