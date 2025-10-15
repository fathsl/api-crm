using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BilesenController : ControllerBase
    {
        private readonly string _connectionString;

        public BilesenController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Bilesen
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BilesenResponseDto>>> GetAllBilesen()
        {
            var bilesenList = new List<BilesenResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT bb.*, uk.KategoriAdi
                    FROM BilesenBilgileri bb
                    LEFT JOIN UrunKategorileri uk ON bb.KategoriID = uk.KategoriID
                    ORDER BY bb.BilesenID DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var bilesen = new BilesenResponseDto
                    {
                        BilesenID = reader.GetInt32("BilesenID"),
                        KategoriID = reader.GetInt32("KategoriID"),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? "Unknown" : reader.GetString("KategoriAdi"),
                        BilesenAdi = reader.GetString("BilesenAdi"),
                        Birim = reader.IsDBNull(reader.GetOrdinal("Birim")) ? "Set" : reader.GetString("Birim"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? 0 : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime("UpdatedAt")
                    };
                    bilesenList.Add(bilesen);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bilesen", error = ex.Message });
            }

            return Ok(bilesenList);
        }

        // GET: api/Bilesen/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BilesenResponseDto>> GetBilesenById(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT bb.*, uk.KategoriAdi
                    FROM BilesenBilgileri bb
                    LEFT JOIN UrunKategorileri uk ON bb.KategoriID = uk.KategoriID
                    WHERE bb.BilesenID = @BilesenID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@BilesenID", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var bilesen = new BilesenResponseDto
                    {
                        BilesenID = reader.GetInt32("BilesenID"),
                        KategoriID = reader.GetInt32("KategoriID"),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? "Unknown" : reader.GetString("KategoriAdi"),
                        BilesenAdi = reader.GetString("BilesenAdi"),
                        Birim = reader.IsDBNull(reader.GetOrdinal("Birim")) ? "Set" : reader.GetString("Birim"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? 0 : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime("UpdatedAt")
                    };
                    return Ok(bilesen);
                }

                return NotFound(new { message = "Bilesen not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bilesen", error = ex.Message });
            }
        }

        // GET: api/Bilesen/ByCategory/5
        [HttpGet("ByCategory/{kategoriId}")]
        public async Task<ActionResult<IEnumerable<BilesenResponseDto>>> GetBilesenByCategory(int kategoriId)
        {
            var bilesenList = new List<BilesenResponseDto>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = @"
            SELECT bb.*, uk.KategoriAdi, kb.Adet
            FROM KategoriBilesenleri kb
            INNER JOIN BilesenBilgileri bb ON kb.BilesenID = bb.BilesenID
            LEFT JOIN UrunKategorileri uk ON kb.KategoriID = uk.KategoriID
            WHERE kb.KategoriID = @KategoriID
            ORDER BY bb.BilesenID DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@KategoriID", kategoriId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var bilesen = new BilesenResponseDto
                    {
                        BilesenID = reader.GetInt32("BilesenID"),
                        KategoriID = kategoriId,
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? "Unknown" : reader.GetString("KategoriAdi"),
                        BilesenAdi = reader.GetString("BilesenAdi"),
                        Birim = reader.IsDBNull(reader.GetOrdinal("Birim")) ? "Set" : reader.GetString("Birim"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? 0 : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime("UpdatedAt"),
                        Adet = reader.IsDBNull(reader.GetOrdinal("Adet")) ? 1 : reader.GetInt32("Adet")
                    };
                    bilesenList.Add(bilesen);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bilesen for category", error = ex.Message });
            }
            return Ok(bilesenList);
        }

        // POST: api/Bilesen
        [HttpPost]
        public async Task<ActionResult<BilesenResponseDto>> CreateBilesen([FromBody] BilesenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.BilesenAdi))
            {
                return BadRequest(new { message = "BilesenAdi is required" });
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO BilesenBilgileri 
                    (KategoriID, BilesenAdi, Birim, Stok, Fiyat, CreatedAt) 
                    VALUES 
                    (@KategoriID, @BilesenAdi, @Birim, @Stok, @Fiyat, @CreatedAt);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                command.Parameters.AddWithValue("@BilesenAdi", request.BilesenAdi);
                command.Parameters.AddWithValue("@Birim", request.Birim ?? "Set");
                command.Parameters.AddWithValue("@Stok", request.Stok ?? 0);
                command.Parameters.AddWithValue("@Fiyat", request.Fiyat ?? 0);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var newId = Convert.ToInt32(await command.ExecuteScalarAsync());

                var result = await GetBilesenById(newId);
                return result.Result is OkObjectResult okResult ? okResult : result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating bilesen", error = ex.Message });
            }
        }

        // PUT: api/Bilesen/5
        [HttpPut("{id}")]
        public async Task<ActionResult<BilesenResponseDto>> UpdateBilesen(int id, [FromBody] BilesenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.BilesenAdi))
            {
                return BadRequest(new { message = "BilesenAdi is required" });
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM BilesenBilgileri WHERE BilesenID = @BilesenID";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@BilesenID", id);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return NotFound(new { message = "Bilesen not found" });
                }

                string query = @"
                    UPDATE BilesenBilgileri 
                    SET KategoriID = @KategoriID,
                        BilesenAdi = @BilesenAdi,
                        Birim = @Birim,
                        Stok = @Stok,
                        Fiyat = @Fiyat,
                        UpdatedAt = @UpdatedAt
                    WHERE BilesenID = @BilesenID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@BilesenID", id);
                command.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                command.Parameters.AddWithValue("@BilesenAdi", request.BilesenAdi);
                command.Parameters.AddWithValue("@Birim", request.Birim ?? "Set");
                command.Parameters.AddWithValue("@Stok", request.Stok ?? 0);
                command.Parameters.AddWithValue("@Fiyat", request.Fiyat ?? 0);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();

                var result = await GetBilesenById(id);
                return result.Result is OkObjectResult okResult ? okResult : result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating bilesen", error = ex.Message });
            }
        }

        // DELETE: api/Bilesen/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBilesen(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM BilesenBilgileri WHERE BilesenID = @BilesenID";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@BilesenID", id);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return NotFound(new { message = "Bilesen not found" });
                }

                string query = "DELETE FROM BilesenBilgileri WHERE BilesenID = @BilesenID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@BilesenID", id);

                await command.ExecuteNonQueryAsync();

                return Ok(new { message = "Bilesen deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting bilesen", error = ex.Message });
            }
        }
    }
}