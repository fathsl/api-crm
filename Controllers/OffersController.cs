using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OffersController : ControllerBase
    {
        private readonly string _connectionString;

        public OffersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Offers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OfferResponseDto>>> GetAllOffers()
        {
            var offers = new List<OfferResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        s.SiparisAlID,
                        s.MusteriId,
                        s.ClientId,
                        s.SiparisNo,
                        s.TeklifNo,
                        s.MusteriAd,
                        s.MTelefon,
                        s.MMail,
                        s.MUlke,
                        s.MVATNumarasi,
                        s.MZipKod,
                        s.MAdres,
                        s.SiparisNotu,
                        s.KullaniciID,
                        s.UpdatedBy,
                        s.UpdatedAt,
                        s.STelefon,
                        s.SFax,
                        s.SWeb,
                        s.SE_Mail,
                        s.SVatNumarasi,
                        s.SUlke,
                        s.SAdres,
                        s.Montaj,
                        s.Teslimat,
                        s.MontajFiyat,
                        s.PesinYüzde,
                        s.TeslimatFiyat,
                        s.TeslimatÇeşiti,
                        s.ParaTipi,
                        s.Indirim,
                        s.Kdv,
                        s.ToplamFiyat,
                        s.BrutToplamFiyat,
                        s.OdenenMiktar,
                        s.OdemeDurum,
                        s.MAdet,
                        s.MTeslimat,
                        s.TTeslimAlanAdi,
                        s.TIlgiliKisi,
                        s.TTelefon,
                        s.TAltTelefon,
                        s.TE_Mail,
                        s.TVATNumarasi,
                        s.TZipKod,
                        s.TUlke,
                        s.TAdres,
                        s.Tarih,
                        s.Kontrol,
                        s.Muhasebe,
                        s.Fabrika,
                        s.SatinAlma,
                        s.Uretim,
                        s.Lojistik,
                        s.Red,
                        s.SiparisMiTeklifMi,
                        s.Sirket,
                        s.STarih,
                        s.KontrolNot,
                        s.FabrikaNot,
                        s.LojistikNot,
                        s.status,
                        GREATEST((s.ToplamFiyat * (s.PesinYüzde / 100)) - COALESCE(s.OdenenMiktar, 0), 0) AS KaparoFiyat,
                        s.ToplamFiyat - COALESCE(s.OdenenMiktar, 0) AS KalanBakiye,
                        CASE 
                            WHEN s.Tarih IS NULL THEN 'HATA' 
                            ELSE DATE_FORMAT(s.Tarih, '%d/%m/%Y') 
                        END AS FormattedTarih,
                        CASE 
                            WHEN s.STarih IS NULL THEN NULL 
                            ELSE DATE_FORMAT(s.STarih, '%d/%m/%Y') 
                        END AS FormattedSTarih,
                        CONCAT(k.Ad, ' ', k.Soyad) AS KullaniciAdi,
                        CONCAT(u.Ad, ' ', u.Soyad) AS UpdatedByName,
                        m.MusteriAd AS MusteriAdFromTable
                    FROM SiparisAlTablo s
                    LEFT JOIN KullaniciBilgileri k ON s.KullaniciID = k.KullaniciID
                    LEFT JOIN KullaniciBilgileri u ON s.UpdatedBy = u.KullaniciID
                    LEFT JOIN MusteriBilgileri m ON s.MusteriId = m.MusteriID
                    ORDER BY s.SiparisAlID DESC";
                    //WHERE s.Kontrol = 'Onay' AND s.SiparisMiTeklifMi != 'Teklif'

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    offers.Add(new OfferResponseDto
                    {
                        SiparisAlID = reader.GetInt32(reader.GetOrdinal("SiparisAlID")),
                        MusteriId = reader.IsDBNull(reader.GetOrdinal("MusteriId")) ? null : reader.GetInt32(reader.GetOrdinal("MusteriId")),
                        ClientId = reader.IsDBNull(reader.GetOrdinal("ClientId")) ? null : reader.GetInt32(reader.GetOrdinal("ClientId")),
                        SiparisNo = reader.IsDBNull(reader.GetOrdinal("SiparisNo")) ? null : reader.GetString(reader.GetOrdinal("SiparisNo")),
                        TeklifNo = reader.IsDBNull(reader.GetOrdinal("TeklifNo")) ? null : reader.GetString(reader.GetOrdinal("TeklifNo")),
                        MusteriAd = reader.IsDBNull(reader.GetOrdinal("MusteriAdFromTable")) ?
                            (reader.IsDBNull(reader.GetOrdinal("MusteriAd")) ? null : reader.GetString(reader.GetOrdinal("MusteriAd"))) :
                            reader.GetString(reader.GetOrdinal("MusteriAdFromTable")),
                        MTelefon = reader.IsDBNull(reader.GetOrdinal("MTelefon")) ? null : reader.GetString(reader.GetOrdinal("MTelefon")),
                        MMail = reader.IsDBNull(reader.GetOrdinal("MMail")) ? null : reader.GetString(reader.GetOrdinal("MMail")),
                        MUlke = reader.IsDBNull(reader.GetOrdinal("MUlke")) ? null : reader.GetString(reader.GetOrdinal("MUlke")),
                        MVATNumarasi = reader.IsDBNull(reader.GetOrdinal("MVATNumarasi")) ? null : reader.GetString(reader.GetOrdinal("MVATNumarasi")),
                        MZipKod = reader.IsDBNull(reader.GetOrdinal("MZipKod")) ? null : reader.GetString(reader.GetOrdinal("MZipKod")),
                        MAdres = reader.IsDBNull(reader.GetOrdinal("MAdres")) ? null : reader.GetString(reader.GetOrdinal("MAdres")),
                        SiparisNotu = reader.IsDBNull(reader.GetOrdinal("SiparisNotu")) ? null : reader.GetString(reader.GetOrdinal("SiparisNotu")),
                        KullaniciID = reader.IsDBNull(reader.GetOrdinal("KullaniciID")) ? null : reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetInt32(reader.GetOrdinal("UpdatedBy")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                        STelefon = reader.IsDBNull(reader.GetOrdinal("STelefon")) ? null : reader.GetString(reader.GetOrdinal("STelefon")),
                        SFax = reader.IsDBNull(reader.GetOrdinal("SFax")) ? null : reader.GetString(reader.GetOrdinal("SFax")),
                        SWeb = reader.IsDBNull(reader.GetOrdinal("SWeb")) ? null : reader.GetString(reader.GetOrdinal("SWeb")),
                        SE_Mail = reader.IsDBNull(reader.GetOrdinal("SE_Mail")) ? null : reader.GetString(reader.GetOrdinal("SE_Mail")),
                        SVatNumarasi = reader.IsDBNull(reader.GetOrdinal("SVatNumarasi")) ? null : reader.GetString(reader.GetOrdinal("SVatNumarasi")),
                        SUlke = reader.IsDBNull(reader.GetOrdinal("SUlke")) ? null : reader.GetString(reader.GetOrdinal("SUlke")),
                        SAdres = reader.IsDBNull(reader.GetOrdinal("SAdres")) ? null : reader.GetString(reader.GetOrdinal("SAdres")),
                        Montaj = reader.IsDBNull(reader.GetOrdinal("Montaj")) ? null : reader.GetString(reader.GetOrdinal("Montaj")),
                        Teslimat = reader.IsDBNull(reader.GetOrdinal("Teslimat")) ? null : reader.GetString(reader.GetOrdinal("Teslimat")),
                        MontajFiyat = reader.IsDBNull(reader.GetOrdinal("MontajFiyat")) ? null : reader.GetDecimal(reader.GetOrdinal("MontajFiyat")),
                        PesinYüzde = reader.GetInt32(reader.GetOrdinal("PesinYüzde")),
                        TeslimatFiyat = reader.IsDBNull(reader.GetOrdinal("TeslimatFiyat")) ? null : reader.GetDecimal(reader.GetOrdinal("TeslimatFiyat")),
                        TeslimatÇeşiti = reader.IsDBNull(reader.GetOrdinal("TeslimatÇeşiti")) ? null : reader.GetString(reader.GetOrdinal("TeslimatÇeşiti")),
                        ParaTipi = reader.IsDBNull(reader.GetOrdinal("ParaTipi")) ? null : reader.GetString(reader.GetOrdinal("ParaTipi")),
                        Indirim = reader.IsDBNull(reader.GetOrdinal("Indirim")) ? null : reader.GetDecimal(reader.GetOrdinal("Indirim")),
                        Kdv = reader.IsDBNull(reader.GetOrdinal("Kdv")) ? null : reader.GetDecimal(reader.GetOrdinal("Kdv")),
                        ToplamFiyat = reader.GetDecimal(reader.GetOrdinal("ToplamFiyat")),
                        BrutToplamFiyat = reader.IsDBNull(reader.GetOrdinal("BrutToplamFiyat")) ? null : reader.GetDecimal(reader.GetOrdinal("BrutToplamFiyat")),
                        OdenenMiktar = reader.IsDBNull(reader.GetOrdinal("OdenenMiktar")) ? null : reader.GetDecimal(reader.GetOrdinal("OdenenMiktar")),
                        OdemeDurum = reader.GetString(reader.GetOrdinal("OdemeDurum")),
                        MAdet = reader.IsDBNull(reader.GetOrdinal("MAdet")) ? null : reader.GetInt32(reader.GetOrdinal("MAdet")),
                        MTeslimat = reader.IsDBNull(reader.GetOrdinal("MTeslimat")) ? null : reader.GetInt32(reader.GetOrdinal("MTeslimat")),
                        TTeslimAlanAdi = reader.IsDBNull(reader.GetOrdinal("TTeslimAlanAdi")) ? null : reader.GetString(reader.GetOrdinal("TTeslimAlanAdi")),
                        TIlgiliKisi = reader.IsDBNull(reader.GetOrdinal("TIlgiliKisi")) ? null : reader.GetString(reader.GetOrdinal("TIlgiliKisi")),
                        TTelefon = reader.IsDBNull(reader.GetOrdinal("TTelefon")) ? null : reader.GetString(reader.GetOrdinal("TTelefon")),
                        TAltTelefon = reader.IsDBNull(reader.GetOrdinal("TAltTelefon")) ? null : reader.GetString(reader.GetOrdinal("TAltTelefon")),
                        TE_Mail = reader.IsDBNull(reader.GetOrdinal("TE_Mail")) ? null : reader.GetString(reader.GetOrdinal("TE_Mail")),
                        TVATNumarasi = reader.IsDBNull(reader.GetOrdinal("TVATNumarasi")) ? null : reader.GetString(reader.GetOrdinal("TVATNumarasi")),
                        TZipKod = reader.IsDBNull(reader.GetOrdinal("TZipKod")) ? null : reader.GetString(reader.GetOrdinal("TZipKod")),
                        TUlke = reader.IsDBNull(reader.GetOrdinal("TUlke")) ? null : reader.GetString(reader.GetOrdinal("TUlke")),
                        TAdres = reader.IsDBNull(reader.GetOrdinal("TAdres")) ? null : reader.GetString(reader.GetOrdinal("TAdres")),
                        Kontrol = reader.GetString(reader.GetOrdinal("Kontrol")),
                        Muhasebe = reader.IsDBNull(reader.GetOrdinal("Muhasebe")) ? false : reader.GetBoolean(reader.GetOrdinal("Muhasebe")),
                        Fabrika = reader.IsDBNull(reader.GetOrdinal("Fabrika")) ? false : reader.GetBoolean(reader.GetOrdinal("Fabrika")),
                        SatinAlma = reader.IsDBNull(reader.GetOrdinal("SatinAlma")) ? false : reader.GetBoolean(reader.GetOrdinal("SatinAlma")),
                        Uretim = reader.IsDBNull(reader.GetOrdinal("Uretim")) ? false : reader.GetBoolean(reader.GetOrdinal("Uretim")),
                        Lojistik = reader.IsDBNull(reader.GetOrdinal("Lojistik")) ? false : reader.GetBoolean(reader.GetOrdinal("Lojistik")),
                        Red = reader.IsDBNull(reader.GetOrdinal("Red")) ? null : reader.GetString(reader.GetOrdinal("Red")),
                        SiparisMiTeklifMi = reader.IsDBNull(reader.GetOrdinal("SiparisMiTeklifMi")) ? null : reader.GetString(reader.GetOrdinal("SiparisMiTeklifMi")),
                        Sirket = reader.IsDBNull(reader.GetOrdinal("Sirket")) ? null : reader.GetString(reader.GetOrdinal("Sirket")),
                        STarih = reader.IsDBNull(reader.GetOrdinal("FormattedSTarih")) ? null : reader.GetString(reader.GetOrdinal("FormattedSTarih")),
                        KontrolNot = reader.IsDBNull(reader.GetOrdinal("KontrolNot")) ? null : reader.GetString(reader.GetOrdinal("KontrolNot")),
                        FabrikaNot = reader.IsDBNull(reader.GetOrdinal("FabrikaNot")) ? null : reader.GetString(reader.GetOrdinal("FabrikaNot")),
                        LojistikNot = reader.IsDBNull(reader.GetOrdinal("LojistikNot")) ? null : reader.GetString(reader.GetOrdinal("LojistikNot")),
                        Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                        KaparoFiyat = reader.GetDecimal(reader.GetOrdinal("KaparoFiyat")),
                        KalanBakiye = reader.GetDecimal(reader.GetOrdinal("KalanBakiye")),
                        Tarih = reader.GetString(reader.GetOrdinal("FormattedTarih")),
                        KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi")) ? null : reader.GetString(reader.GetOrdinal("KullaniciAdi")),
                        UpdatedByName = reader.IsDBNull(reader.GetOrdinal("UpdatedByName")) ? null : reader.GetString(reader.GetOrdinal("UpdatedByName"))
                    });
                }

                return Ok(offers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving offers", error = ex.Message });
            }
        }

        // GET: api/Offers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OfferDetailDto>> GetOffer(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        s.*,
                        GREATEST((s.ToplamFiyat * (s.PesinYüzde / 100)) - COALESCE(s.OdenenMiktar, 0), 0) AS KaparoFiyat,
                        s.ToplamFiyat - COALESCE(s.OdenenMiktar, 0) AS KalanBakiye,
                        CONCAT(k.Ad, ' ', k.Soyad) AS KullaniciAdi
                    FROM SiparisAlTablo s
                    LEFT JOIN KullaniciBilgileri k ON s.KullaniciID = k.KullaniciID
                    WHERE s.SiparisAlID = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var offer = new OfferDetailDto
                    {
                        SiparisAlID = reader.GetInt32(reader.GetOrdinal("SiparisAlID")),
                        SiparisNo = reader.IsDBNull(reader.GetOrdinal("SiparisNo")) ? null : reader.GetString(reader.GetOrdinal("SiparisNo")),
                        TeklifNo = reader.IsDBNull(reader.GetOrdinal("TeklifNo")) ? null : reader.GetString(reader.GetOrdinal("TeklifNo")),
                        MusteriId = reader.IsDBNull(reader.GetOrdinal("MusteriId")) ? null : reader.GetInt32(reader.GetOrdinal("MusteriId")),
                        MusteriAd = reader.IsDBNull(reader.GetOrdinal("MusteriAd")) ? null : reader.GetString(reader.GetOrdinal("MusteriAd")),
                        MTelefon = reader.IsDBNull(reader.GetOrdinal("MTelefon")) ? null : reader.GetString(reader.GetOrdinal("MTelefon")),
                        MMail = reader.IsDBNull(reader.GetOrdinal("MMail")) ? null : reader.GetString(reader.GetOrdinal("MMail")),
                        MUlke = reader.IsDBNull(reader.GetOrdinal("MUlke")) ? null : reader.GetString(reader.GetOrdinal("MUlke")),
                        MVATNumarasi = reader.IsDBNull(reader.GetOrdinal("MVATNumarasi")) ? null : reader.GetString(reader.GetOrdinal("MVATNumarasi")),
                        MZipKod = reader.IsDBNull(reader.GetOrdinal("MZipKod")) ? null : reader.GetString(reader.GetOrdinal("MZipKod")),
                        MAdres = reader.IsDBNull(reader.GetOrdinal("MAdres")) ? null : reader.GetString(reader.GetOrdinal("MAdres")),
                        SiparisNotu = reader.IsDBNull(reader.GetOrdinal("SiparisNotu")) ? null : reader.GetString(reader.GetOrdinal("SiparisNotu")),
                        KullaniciID = reader.IsDBNull(reader.GetOrdinal("KullaniciID")) ? null : reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                        KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi")) ? null : reader.GetString(reader.GetOrdinal("KullaniciAdi")),

                        STelefon = reader.IsDBNull(reader.GetOrdinal("STelefon")) ? null : reader.GetString(reader.GetOrdinal("STelefon")),
                        SFax = reader.IsDBNull(reader.GetOrdinal("SFax")) ? null : reader.GetString(reader.GetOrdinal("SFax")),
                        SWeb = reader.IsDBNull(reader.GetOrdinal("SWeb")) ? null : reader.GetString(reader.GetOrdinal("SWeb")),
                        SE_Mail = reader.IsDBNull(reader.GetOrdinal("SE_Mail")) ? null : reader.GetString(reader.GetOrdinal("SE_Mail")),
                        SVatNumarasi = reader.IsDBNull(reader.GetOrdinal("SVatNumarasi")) ? null : reader.GetString(reader.GetOrdinal("SVatNumarasi")),
                        SUlke = reader.IsDBNull(reader.GetOrdinal("SUlke")) ? null : reader.GetString(reader.GetOrdinal("SUlke")),
                        SAdres = reader.IsDBNull(reader.GetOrdinal("SAdres")) ? null : reader.GetString(reader.GetOrdinal("SAdres")),

                        Montaj = reader.IsDBNull(reader.GetOrdinal("Montaj")) ? null : reader.GetString(reader.GetOrdinal("Montaj")),
                        Teslimat = reader.IsDBNull(reader.GetOrdinal("Teslimat")) ? null : reader.GetString(reader.GetOrdinal("Teslimat")),
                        MontajFiyat = reader.IsDBNull(reader.GetOrdinal("MontajFiyat")) ? null : reader.GetDecimal(reader.GetOrdinal("MontajFiyat")),
                        PesinYüzde = reader.GetInt32(reader.GetOrdinal("PesinYüzde")),
                        TeslimatFiyat = reader.IsDBNull(reader.GetOrdinal("TeslimatFiyat")) ? null : reader.GetDecimal(reader.GetOrdinal("TeslimatFiyat")),
                        TeslimatÇeşiti = reader.IsDBNull(reader.GetOrdinal("TeslimatÇeşiti")) ? null : reader.GetString(reader.GetOrdinal("TeslimatÇeşiti")),

                        ParaTipi = reader.IsDBNull(reader.GetOrdinal("ParaTipi")) ? null : reader.GetString(reader.GetOrdinal("ParaTipi")),
                        Indirim = reader.GetDecimal(reader.GetOrdinal("Indirim")),
                        Kdv = reader.GetDecimal(reader.GetOrdinal("Kdv")),
                        ToplamFiyat = reader.GetDecimal(reader.GetOrdinal("ToplamFiyat")),
                        BrutToplamFiyat = reader.GetDecimal(reader.GetOrdinal("BrutToplamFiyat")),
                        OdenenMiktar = reader.GetDecimal(reader.GetOrdinal("OdenenMiktar")),
                        KaparoFiyat = reader.GetDecimal(reader.GetOrdinal("KaparoFiyat")),
                        KalanBakiye = reader.GetDecimal(reader.GetOrdinal("KalanBakiye")),
                        OdemeDurum = reader.GetString(reader.GetOrdinal("OdemeDurum")),

                        MAdet = reader.GetInt32(reader.GetOrdinal("MAdet")),
                        MTeslimat = reader.GetInt32(reader.GetOrdinal("MTeslimat")),

                        TTeslimAlanAdi = reader.IsDBNull(reader.GetOrdinal("TTeslimAlanAdi")) ? null : reader.GetString(reader.GetOrdinal("TTeslimAlanAdi")),
                        TIlgiliKisi = reader.IsDBNull(reader.GetOrdinal("TIlgiliKisi")) ? null : reader.GetString(reader.GetOrdinal("TIlgiliKisi")),
                        TTelefon = reader.IsDBNull(reader.GetOrdinal("TTelefon")) ? null : reader.GetString(reader.GetOrdinal("TTelefon")),
                        TAltTelefon = reader.IsDBNull(reader.GetOrdinal("TAltTelefon")) ? null : reader.GetString(reader.GetOrdinal("TAltTelefon")),
                        TE_Mail = reader.IsDBNull(reader.GetOrdinal("TE_Mail")) ? null : reader.GetString(reader.GetOrdinal("TE_Mail")),
                        TVATNumarasi = reader.IsDBNull(reader.GetOrdinal("TVATNumarasi")) ? null : reader.GetString(reader.GetOrdinal("TVATNumarasi")),
                        TZipKod = reader.IsDBNull(reader.GetOrdinal("TZipKod")) ? null : reader.GetString(reader.GetOrdinal("TZipKod")),
                        TUlke = reader.GetString(reader.GetOrdinal("TUlke")),
                        TAdres = reader.IsDBNull(reader.GetOrdinal("TAdres")) ? null : reader.GetString(reader.GetOrdinal("TAdres")),

                        Tarih = reader.GetDateTime(reader.GetOrdinal("Tarih")),
                        Kontrol = reader.GetString(reader.GetOrdinal("Kontrol")),
                        Muhasebe = reader.GetBoolean(reader.GetOrdinal("Muhasebe")),
                        Fabrika = reader.GetBoolean(reader.GetOrdinal("Fabrika")),
                        SatinAlma = reader.GetBoolean(reader.GetOrdinal("SatinAlma")),
                        Uretim = reader.GetBoolean(reader.GetOrdinal("Uretim")),
                        Lojistik = reader.GetBoolean(reader.GetOrdinal("Lojistik")),
                        Red = reader.IsDBNull(reader.GetOrdinal("Red")) ? null : reader.GetString(reader.GetOrdinal("Red")),
                        SiparisMiTeklifMi = reader.IsDBNull(reader.GetOrdinal("SiparisMiTeklifMi")) ? null : reader.GetString(reader.GetOrdinal("SiparisMiTeklifMi")),
                        Sirket = reader.GetString(reader.GetOrdinal("Sirket")),
                        STarih = reader.IsDBNull(reader.GetOrdinal("STarih")) ? null : reader.GetDateTime(reader.GetOrdinal("STarih")),
                        KontrolNot = reader.IsDBNull(reader.GetOrdinal("KontrolNot")) ? null : reader.GetString(reader.GetOrdinal("KontrolNot")),
                        FabrikaNot = reader.IsDBNull(reader.GetOrdinal("FabrikaNot")) ? null : reader.GetString(reader.GetOrdinal("FabrikaNot")),
                        LojistikNot = reader.IsDBNull(reader.GetOrdinal("LojistikNot")) ? null : reader.GetString(reader.GetOrdinal("LojistikNot"))
                    };

                    return Ok(offer);
                }

                return NotFound(new { message = "Offer not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving offer", error = ex.Message });
            }
        }

        // POST: api/Offers
        [HttpPost]
        public async Task<ActionResult> CreateOffer([FromBody] CreateOfferDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string insertQuery = @"
                    INSERT INTO SiparisAlTablo 
                    (MusteriId, ClientId, SiparisNo, TeklifNo, SiparisNotu, KullaniciID, UpdatedBy, UpdatedAt,
                    STelefon, SFax, SWeb, SE_Mail, SVatNumarasi, SUlke, SAdres,
                    Montaj, Teslimat, MontajFiyat, PesinYüzde, TeslimatFiyat, TeslimatÇeşiti, ParaTipi,
                    Indirim, Kdv, ToplamFiyat, BrutToplamFiyat, OdenenMiktar, OdemeDurum, MAdet, MTeslimat,
                    Tarih, Kontrol, Ek, MuhasebeEk, Muhasebe, Fabrika, SatinAlma, Uretim, Lojistik, Red,
                    SiparisMiTeklifMi, Sirket, STarih, KontrolNot, FabrikaNot, LojistikNot, status)
                    VALUES 
                    (@MusteriId, @ClientId, @SiparisNo, @TeklifNo, @SiparisNotu, @KullaniciID, @UpdatedBy, @UpdatedAt,
                    @STelefon, @SFax, @SWeb, @SE_Mail, @SVatNumarasi, @SUlke, @SAdres,
                    @Montaj, @Teslimat, @MontajFiyat, @PesinYüzde, @TeslimatFiyat, @TeslimatÇeşiti, @ParaTipi,
                    @Indirim, @Kdv, @ToplamFiyat, @BrutToplamFiyat, @OdenenMiktar, @OdemeDurum, @MAdet, @MTeslimat,
                    @Tarih, @Kontrol, @Ek, @MuhasebeEk, @Muhasebe, @Fabrika, @SatinAlma, @Uretim, @Lojistik, @Red,
                    @SiparisMiTeklifMi, @Sirket, @STarih, @KontrolNot, @FabrikaNot, @LojistikNot, @status);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(insertQuery, connection);
                
                // Note: SiparisAlID is auto-increment, so we don't insert it
                command.Parameters.AddWithValue("@MusteriId", createDto.MusteriId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClientId", createDto.ClientId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SiparisNo", createDto.SiparisNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TeklifNo", createDto.TeklifNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SiparisNotu", createDto.SiparisNotu ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@KullaniciID", createDto.KullaniciID ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedBy", createDto.UpdatedBy ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", createDto.UpdatedAt ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@STelefon", createDto.STelefon ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SFax", createDto.SFax ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SWeb", createDto.SWeb ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SE_Mail", createDto.SE_Mail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SVatNumarasi", createDto.SVatNumarasi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SUlke", createDto.SUlke ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SAdres", createDto.SAdres ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Montaj", createDto.Montaj ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Teslimat", createDto.Teslimat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MontajFiyat", createDto.MontajFiyat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PesinYüzde", createDto.PesinYüzde);
                command.Parameters.AddWithValue("@TeslimatFiyat", createDto.TeslimatFiyat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TeslimatÇeşiti", createDto.TeslimatÇeşiti ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParaTipi", createDto.ParaTipi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Indirim", createDto.Indirim ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Kdv", createDto.Kdv ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToplamFiyat", createDto.ToplamFiyat);
                command.Parameters.AddWithValue("@BrutToplamFiyat", createDto.BrutToplamFiyat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OdenenMiktar", createDto.OdenenMiktar ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@OdemeDurum", createDto.OdemeDurum ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MAdet", createDto.MAdet ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MTeslimat", createDto.MTeslimat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Tarih", createDto.Tarih ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Kontrol", createDto.Kontrol ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Ek", createDto.Ek ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MuhasebeEk", createDto.MuhasebeEk ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Muhasebe", createDto.Muhasebe ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fabrika", createDto.Fabrika ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SatinAlma", createDto.SatinAlma ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Uretim", createDto.Uretim ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Lojistik", createDto.Lojistik ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Red", createDto.Red ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SiparisMiTeklifMi", createDto.SiparisMiTeklifMi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Sirket", createDto.Sirket ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@STarih", createDto.STarih ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@KontrolNot", createDto.KontrolNot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FabrikaNot", createDto.FabrikaNot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LojistikNot", createDto.LojistikNot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@status", createDto.Status ?? (object)DBNull.Value);

                int offerId = Convert.ToInt32(await command.ExecuteScalarAsync());

                return CreatedAtAction(nameof(GetOffer), new { id = offerId },
                    new { SiparisAlID = offerId, message = "Offer created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating offer", error = ex.Message });
            }
        }

        // PUT: api/Offers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOffer(int id, [FromBody] UpdateOfferDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string updateQuery = @"
                    UPDATE SiparisAlTablo 
                    SET SiparisNo = @SiparisNo, TeklifNo = @TeklifNo, MusteriId = @MusteriId,
                        MusteriAd = @MusteriAd, MTelefon = @MTelefon, MMail = @MMail, MUlke = @MUlke,
                        MVATNumarasi = @MVATNumarasi, MZipKod = @MZipKod, MAdres = @MAdres,
                        SiparisNotu = @SiparisNotu, KullaniciID = @KullaniciID,
                        STelefon = @STelefon, SFax = @SFax, SWeb = @SWeb, SE_Mail = @SE_Mail,
                        SVatNumarasi = @SVatNumarasi, SUlke = @SUlke, SAdres = @SAdres,
                        Montaj = @Montaj, Teslimat = @Teslimat, MontajFiyat = @MontajFiyat,
                        PesinYüzde = @PesinYüzde, TeslimatFiyat = @TeslimatFiyat,
                        TeslimatÇeşiti = @TeslimatÇeşiti, ParaTipi = @ParaTipi,
                        Indirim = @Indirim, Kdv = @Kdv, ToplamFiyat = @ToplamFiyat,
                        BrutToplamFiyat = @BrutToplamFiyat, OdenenMiktar = @OdenenMiktar,
                        OdemeDurum = @OdemeDurum, MAdet = @MAdet, MTeslimat = @MTeslimat,
                        TTeslimAlanAdi = @TTeslimAlanAdi, TIlgiliKisi = @TIlgiliKisi,
                        TTelefon = @TTelefon, TAltTelefon = @TAltTelefon, TE_Mail = @TE_Mail,
                        TVATNumarasi = @TVATNumarasi, TZipKod = @TZipKod, TUlke = @TUlke, TAdres = @TAdres,
                        Kontrol = @Kontrol, Muhasebe = @Muhasebe, Fabrika = @Fabrika,
                        SatinAlma = @SatinAlma, Uretim = @Uretim, Lojistik = @Lojistik,
                        Red = @Red, SiparisMiTeklifMi = @SiparisMiTeklifMi, Sirket = @Sirket,
                        STarih = @STarih, KontrolNot = @KontrolNot, FabrikaNot = @FabrikaNot,
                        LojistikNot = @LojistikNot
                    WHERE SiparisAlID = @Id";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@SiparisNo", updateDto.SiparisNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TeklifNo", updateDto.TeklifNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MusteriId", updateDto.MusteriId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MusteriAd", updateDto.MusteriAd ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MTelefon", updateDto.MTelefon ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MMail", updateDto.MMail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MUlke", updateDto.MUlke ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MVATNumarasi", updateDto.MVATNumarasi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MZipKod", updateDto.MZipKod ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MAdres", updateDto.MAdres ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SiparisNotu", updateDto.SiparisNotu ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@KullaniciID", updateDto.KullaniciID);
                command.Parameters.AddWithValue("@STelefon", updateDto.STelefon ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SFax", updateDto.SFax ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SWeb", updateDto.SWeb ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SE_Mail", updateDto.SE_Mail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SVatNumarasi", updateDto.SVatNumarasi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SUlke", updateDto.SUlke ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SAdres", updateDto.SAdres ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Montaj", updateDto.Montaj ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Teslimat", updateDto.Teslimat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@MontajFiyat", updateDto.MontajFiyat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PesinYüzde", updateDto.PesinYüzde);
                command.Parameters.AddWithValue("@TeslimatFiyat", updateDto.TeslimatFiyat ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TeslimatÇeşiti", updateDto.TeslimatÇeşiti ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ParaTipi", updateDto.ParaTipi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Indirim", updateDto.Indirim);
                command.Parameters.AddWithValue("@Kdv", updateDto.Kdv);
                command.Parameters.AddWithValue("@ToplamFiyat", updateDto.ToplamFiyat);
                command.Parameters.AddWithValue("@BrutToplamFiyat", updateDto.BrutToplamFiyat);
                command.Parameters.AddWithValue("@OdenenMiktar", updateDto.OdenenMiktar);
                command.Parameters.AddWithValue("@OdemeDurum", updateDto.OdemeDurum);
                command.Parameters.AddWithValue("@MAdet", updateDto.MAdet);
                command.Parameters.AddWithValue("@MTeslimat", updateDto.MTeslimat);
                command.Parameters.AddWithValue("@TTeslimAlanAdi", updateDto.TTeslimAlanAdi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TIlgiliKisi", updateDto.TIlgiliKisi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TTelefon", updateDto.TTelefon ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TAltTelefon", updateDto.TAltTelefon ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TE_Mail", updateDto.TE_Mail ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TVATNumarasi", updateDto.TVATNumarasi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TZipKod", updateDto.TZipKod ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TUlke", updateDto.TUlke);
                command.Parameters.AddWithValue("@TAdres", updateDto.TAdres ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Kontrol", updateDto.Kontrol);
                command.Parameters.AddWithValue("@Muhasebe", updateDto.Muhasebe);
                command.Parameters.AddWithValue("@Fabrika", updateDto.Fabrika);
                command.Parameters.AddWithValue("@SatinAlma", updateDto.SatinAlma);
                command.Parameters.AddWithValue("@Uretim", updateDto.Uretim);
                command.Parameters.AddWithValue("@Lojistik", updateDto.Lojistik);
                command.Parameters.AddWithValue("@Red", updateDto.Red ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SiparisMiTeklifMi", updateDto.SiparisMiTeklifMi ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Sirket", updateDto.Sirket);
                command.Parameters.AddWithValue("@STarih", updateDto.STarih ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@KontrolNot", updateDto.KontrolNot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FabrikaNot", updateDto.FabrikaNot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LojistikNot", updateDto.LojistikNot ?? (object)DBNull.Value);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Offer not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating offer", error = ex.Message });
            }
        }

        // DELETE: api/Offers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                string getOfferQuery = "SELECT KategoriID, Miktar FROM TeslimatBilgileri WHERE TeslimatID = @Id";
                int kategoriID;
                int miktar;

                using (var getCommand = new MySqlCommand(getOfferQuery, connection, transaction))
                {
                    getCommand.Parameters.AddWithValue("@Id", id);
                    using var reader = await getCommand.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = "Offer not found" });
                    }

                    kategoriID = reader.GetInt32(reader.GetOrdinal("KategoriID"));
                    miktar = reader.GetInt32(reader.GetOrdinal("Miktar"));
                }

                string restoreStockQuery = "UPDATE UrunKategorileri SET Stok = Stok + @Miktar WHERE KategoriID = @KategoriID";
                using (var restoreCommand = new MySqlCommand(restoreStockQuery, connection, transaction))
                {
                    restoreCommand.Parameters.AddWithValue("@KategoriID", kategoriID);
                    restoreCommand.Parameters.AddWithValue("@Miktar", miktar);
                    await restoreCommand.ExecuteNonQueryAsync();
                }

                string deleteQuery = "DELETE FROM TeslimatBilgileri WHERE TeslimatID = @Id";
                using (var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@Id", id);
                    await deleteCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return StatusCode(500, new { message = "Error deleting offer", error = ex.Message });
            }
            finally
            {
                if (connection != null)
                {
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }
        }
    }
}