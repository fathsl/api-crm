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
                        t.TeslimatID,
                        t.KullaniciID,
                        t.MusteriID,
                        t.KategoriID,
                        t.Miktar,
                        t.Fiyat,
                        t.TeslimatTarihi,
                        t.TeslimatBilgisi,
                        CONCAT(k.Ad, ' ', k.Soyad) AS KullaniciAdi,
                        m.MusteriAd,
                        uk.KategoriAdi
                    FROM TeslimatBilgileri t
                    LEFT JOIN KullaniciBilgileri k ON t.KullaniciID = k.KullaniciID
                    LEFT JOIN MusteriBilgileri m ON t.MusteriID = m.MusteriID
                    LEFT JOIN UrunKategorileri uk ON t.KategoriID = uk.KategoriID
                    ORDER BY t.TeslimatTarihi DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    offers.Add(new OfferResponseDto
                    {
                        TeslimatID = reader.GetInt32(reader.GetOrdinal("TeslimatID")),
                        KullaniciID = reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                        KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi")) ? null : reader.GetString(reader.GetOrdinal("KullaniciAdi")),
                        MusteriID = reader.GetInt32(reader.GetOrdinal("MusteriID")),
                        MusteriAd = reader.IsDBNull(reader.GetOrdinal("MusteriAd")) ? null : reader.GetString(reader.GetOrdinal("MusteriAd")),
                        KategoriID = reader.GetInt32(reader.GetOrdinal("KategoriID")),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? null : reader.GetString(reader.GetOrdinal("KategoriAdi")),
                        Miktar = reader.GetInt32(reader.GetOrdinal("Miktar")),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : (int)reader.GetDecimal(reader.GetOrdinal("Fiyat")),
                        TeslimatTarihi = reader.GetDateTime(reader.GetOrdinal("TeslimatTarihi")),
                        TeslimatBilgisi = reader.IsDBNull(reader.GetOrdinal("TeslimatBilgisi")) ? null : reader.GetString(reader.GetOrdinal("TeslimatBilgisi"))
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
                        t.TeslimatID,
                        t.KullaniciID,
                        t.MusteriID,
                        t.KategoriID,
                        t.Miktar,
                        t.Fiyat,
                        t.TeslimatTarihi,
                        t.TeslimatBilgisi,
                        CONCAT(k.Ad, ' ', k.Soyad) AS KullaniciAdi,
                        m.MusteriAd,
                        uk.KategoriAdi,
                        uk.Fiyat AS KategoriFiyat
                    FROM TeslimatBilgileri t
                    LEFT JOIN KullaniciBilgileri k ON t.KullaniciID = k.KullaniciID
                    LEFT JOIN MusteriBilgileri m ON t.MusteriID = m.MusteriID
                    LEFT JOIN UrunKategorileri uk ON t.KategoriID = uk.KategoriID
                    WHERE t.TeslimatID = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Fiyat"));
                    var miktar = reader.GetInt32(reader.GetOrdinal("Miktar"));

                    var offer = new OfferDetailDto
                    {
                        TeslimatID = reader.GetInt32(reader.GetOrdinal("TeslimatID")),
                        KullaniciID = reader.GetInt32(reader.GetOrdinal("KullaniciID")),
                        KullaniciAdi = reader.IsDBNull(reader.GetOrdinal("KullaniciAdi")) ? null : reader.GetString(reader.GetOrdinal("KullaniciAdi")),
                        MusteriID = reader.GetInt32(reader.GetOrdinal("MusteriID")),
                        MusteriAd = reader.IsDBNull(reader.GetOrdinal("MusteriAd")) ? null : reader.GetString(reader.GetOrdinal("MusteriAd")),
                        KategoriID = reader.GetInt32(reader.GetOrdinal("KategoriID")),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? null : reader.GetString(reader.GetOrdinal("KategoriAdi")),
                        KategoriFiyat = reader.IsDBNull(reader.GetOrdinal("KategoriFiyat")) ? 0 : reader.GetDecimal(reader.GetOrdinal("KategoriFiyat")),
                        Miktar = miktar,
                        Fiyat = (int)fiyat,
                        ToplamTutar = fiyat,
                        TeslimatTarihi = reader.GetDateTime(reader.GetOrdinal("TeslimatTarihi")),
                        TeslimatBilgisi = reader.IsDBNull(reader.GetOrdinal("TeslimatBilgisi")) ? null : reader.GetString(reader.GetOrdinal("TeslimatBilgisi"))
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

            MySqlConnection connection = null;
            MySqlTransaction transaction = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                // Check stock availability
                string checkStockQuery = "SELECT Stok FROM UrunKategorileri WHERE KategoriID = @KategoriID";
                using (var checkCommand = new MySqlCommand(checkStockQuery, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@KategoriID", createDto.KategoriID);
                    var stockResult = await checkCommand.ExecuteScalarAsync();

                    if (stockResult == null || stockResult == DBNull.Value)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Category not found" });
                    }

                    int currentStock = Convert.ToInt32(stockResult);
                    if (currentStock < createDto.Miktar)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"Insufficient stock. Available: {currentStock}, Requested: {createDto.Miktar}" });
                    }
                }

                // Insert offer
                string insertQuery = @"
                    INSERT INTO TeslimatBilgileri 
                    (KullaniciID, MusteriID, KategoriID, Miktar, Fiyat, TeslimatTarihi, TeslimatBilgisi)
                    VALUES (@KullaniciID, @MusteriID, @KategoriID, @Miktar, @Fiyat, @TeslimatTarihi, @TeslimatBilgisi);
                    SELECT LAST_INSERT_ID();";

                int offerId;
                using (var insertCommand = new MySqlCommand(insertQuery, connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@KullaniciID", createDto.KullaniciID);
                    insertCommand.Parameters.AddWithValue("@MusteriID", createDto.MusteriID);
                    insertCommand.Parameters.AddWithValue("@KategoriID", createDto.KategoriID);
                    insertCommand.Parameters.AddWithValue("@Miktar", createDto.Miktar);
                    insertCommand.Parameters.AddWithValue("@Fiyat", createDto.Fiyat);
                    insertCommand.Parameters.AddWithValue("@TeslimatTarihi", DateTime.UtcNow);
                    insertCommand.Parameters.AddWithValue("@TeslimatBilgisi", createDto.TeslimatBilgisi ?? (object)DBNull.Value);

                    offerId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());
                }

                string updateStockQuery = "UPDATE UrunKategorileri SET Stok = Stok - @Miktar WHERE KategoriID = @KategoriID";
                using (var updateCommand = new MySqlCommand(updateStockQuery, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@KategoriID", createDto.KategoriID);
                    updateCommand.Parameters.AddWithValue("@Miktar", createDto.Miktar);
                    await updateCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetOffer), new { id = offerId },
                    new { TeslimatID = offerId, message = "Offer created successfully" });
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return StatusCode(500, new { message = "Error creating offer", error = ex.Message });
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

        // PUT: api/Offers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOffer(int id, [FromBody] UpdateOfferDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            MySqlConnection connection = null;
            MySqlTransaction transaction = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                // Get current offer data
                string getCurrentQuery = "SELECT KategoriID, Miktar FROM TeslimatBilgileri WHERE TeslimatID = @Id";
                int oldKategoriID;
                int oldMiktar;

                using (var getCommand = new MySqlCommand(getCurrentQuery, connection, transaction))
                {
                    getCommand.Parameters.AddWithValue("@Id", id);
                    using var reader = await getCommand.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = "Offer not found" });
                    }

                    oldKategoriID = reader.GetInt32(reader.GetOrdinal("KategoriID"));
                    oldMiktar = reader.GetInt32(reader.GetOrdinal("Miktar"));
                }

                string restoreStockQuery = "UPDATE UrunKategorileri SET Stok = Stok + @Miktar WHERE KategoriID = @KategoriID";
                using (var restoreCommand = new MySqlCommand(restoreStockQuery, connection, transaction))
                {
                    restoreCommand.Parameters.AddWithValue("@KategoriID", oldKategoriID);
                    restoreCommand.Parameters.AddWithValue("@Miktar", oldMiktar);
                    await restoreCommand.ExecuteNonQueryAsync();
                }

                string checkStockQuery = "SELECT Stok FROM UrunKategorileri WHERE KategoriID = @KategoriID";
                using (var checkCommand = new MySqlCommand(checkStockQuery, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@KategoriID", updateDto.KategoriID);
                    var stockResult = await checkCommand.ExecuteScalarAsync();

                    if (stockResult == null || stockResult == DBNull.Value)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Category not found" });
                    }

                    int currentStock = Convert.ToInt32(stockResult);
                    if (currentStock < updateDto.Miktar)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = $"Insufficient stock. Available: {currentStock}, Requested: {updateDto.Miktar}" });
                    }
                }

                string updateQuery = @"
                    UPDATE TeslimatBilgileri 
                    SET KullaniciID = @KullaniciID,
                        MusteriID = @MusteriID,
                        KategoriID = @KategoriID,
                        Miktar = @Miktar,
                        Fiyat = @Fiyat,
                        TeslimatBilgisi = @TeslimatBilgisi
                    WHERE TeslimatID = @Id";

                using (var updateCommand = new MySqlCommand(updateQuery, connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("@Id", id);
                    updateCommand.Parameters.AddWithValue("@KullaniciID", updateDto.KullaniciID);
                    updateCommand.Parameters.AddWithValue("@MusteriID", updateDto.MusteriID);
                    updateCommand.Parameters.AddWithValue("@KategoriID", updateDto.KategoriID);
                    updateCommand.Parameters.AddWithValue("@Miktar", updateDto.Miktar);
                    updateCommand.Parameters.AddWithValue("@Fiyat", updateDto.Fiyat);
                    updateCommand.Parameters.AddWithValue("@TeslimatBilgisi", updateDto.TeslimatBilgisi ?? (object)DBNull.Value);
                    await updateCommand.ExecuteNonQueryAsync();
                }

                string deductStockQuery = "UPDATE UrunKategorileri SET Stok = Stok - @Miktar WHERE KategoriID = @KategoriID";
                using (var deductCommand = new MySqlCommand(deductStockQuery, connection, transaction))
                {
                    deductCommand.Parameters.AddWithValue("@KategoriID", updateDto.KategoriID);
                    deductCommand.Parameters.AddWithValue("@Miktar", updateDto.Miktar);
                    await deductCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return StatusCode(500, new { message = "Error updating offer", error = ex.Message });
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