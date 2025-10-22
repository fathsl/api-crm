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
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "TRY" : reader.GetString("Currency"),
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
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "TRY" : reader.GetString("Currency"),
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
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "TRY" : reader.GetString("Currency"),
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

            if (request.KategoriID <= 0)
            {
                return BadRequest(new { message = "Valid KategoriID is required" });
            }

            MySqlConnection connection = null;
            MySqlTransaction transaction = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                transaction = await connection.BeginTransactionAsync();

                string insertBilesenQuery = @"
                        INSERT INTO BilesenBilgileri
                        (BilesenAdi, Birim, Stok, Fiyat,Currency ,CreatedAt,Currency)
                        VALUES
                        (@BilesenAdi, @Birim, @Stok, @Fiyat,@Currency, @CreatedAt,@Currency);
                        SELECT LAST_INSERT_ID();";

                int newBilesenId;
                using (var command = new MySqlCommand(insertBilesenQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@BilesenAdi", request.BilesenAdi);
                    command.Parameters.AddWithValue("@Birim", request.Birim ?? "Set");
                    command.Parameters.AddWithValue("@Stok", request.Stok ?? 0);
                    command.Parameters.AddWithValue("@Fiyat", request.Fiyat ?? 0);
                    command.Parameters.AddWithValue("@Currency", request.Currency ?? "TRY");
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                    newBilesenId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                string insertJunctionQuery = @"
                        INSERT INTO KategoriBilesenleri
                        (KategoriID, BilesenID, Adet)
                        VALUES
                        (@KategoriID, @BilesenID, @Adet);";

                using (var command = new MySqlCommand(insertJunctionQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                    command.Parameters.AddWithValue("@BilesenID", newBilesenId);
                    command.Parameters.AddWithValue("@Adet", request.Adet);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                var result = await GetBilesenById(newBilesenId);
                return result.Result is OkObjectResult okResult ? okResult : result;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                return StatusCode(500, new { message = "Error creating bilesen", error = ex.Message });
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
                if (connection != null)
                {
                    await connection.DisposeAsync();
                }
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

            if (request.KategoriID <= 0)
            {
                return BadRequest(new { message = "Valid KategoriID is required" });
            }

            MySqlConnection connection = null;
            MySqlTransaction transaction = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = await connection.BeginTransactionAsync();

                string checkQuery = "SELECT COUNT(*) FROM BilesenBilgileri WHERE BilesenID = @BilesenID";
                using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction))
                {
                    checkCommand.Parameters.AddWithValue("@BilesenID", id);
                    var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                    if (!exists)
                    {
                        return NotFound(new { message = "Bilesen not found" });
                    }
                }

                string updateQuery = @"
                    UPDATE BilesenBilgileri
                    SET BilesenAdi = @BilesenAdi,
                        Birim = @Birim,
                        Stok = @Stok,
                        Fiyat = @Fiyat,
                        Currency = @Currency,
                        UpdatedAt = @UpdatedAt
                    WHERE BilesenID = @BilesenID";

                using (var command = new MySqlCommand(updateQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@BilesenID", id);
                    command.Parameters.AddWithValue("@BilesenAdi", request.BilesenAdi);
                    command.Parameters.AddWithValue("@Birim", request.Birim ?? "Set");
                    command.Parameters.AddWithValue("@Currency", request.Currency ?? "TRY");
                    command.Parameters.AddWithValue("@Stok", request.Stok ?? 0);
                    command.Parameters.AddWithValue("@Fiyat", request.Fiyat ?? 0);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                    await command.ExecuteNonQueryAsync();
                }

                string updateJunctionQuery = @"
                    UPDATE KategoriBilesenleri
                    SET KategoriID = @KategoriID,
                        Adet = @Adet
                    WHERE BilesenID = @BilesenID";

                using (var command = new MySqlCommand(updateJunctionQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                    command.Parameters.AddWithValue("@BilesenID", id);
                    command.Parameters.AddWithValue("@Adet", request.Adet);
                    await command.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                var result = await GetBilesenById(id);
                return result.Result is OkObjectResult okResult ? okResult : result;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                return StatusCode(500, new { message = "Error updating bilesen", error = ex.Message });
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
                if (connection != null)
                {
                    await connection.DisposeAsync();
                }
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

        [HttpDelete("kategori/{kategoriId}/bilesen/{bilesenId}")]
        public async Task<ActionResult> DeleteBilesenFromKategori(int kategoriId, int bilesenId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = @"SELECT COUNT(*) FROM KategoriBilesenleri 
                             WHERE KategoriID = @KategoriID AND BilesenID = @BilesenID";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@KategoriID", kategoriId);
                checkCommand.Parameters.AddWithValue("@BilesenID", bilesenId);

                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Relationship not found in KategoriBilesenleri" });
                }

                string query = @"DELETE FROM KategoriBilesenleri 
                        WHERE KategoriID = @KategoriID AND BilesenID = @BilesenID";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@KategoriID", kategoriId);
                command.Parameters.AddWithValue("@BilesenID", bilesenId);

                await command.ExecuteNonQueryAsync();

                return Ok(new { message = "Bilesen removed from kategori successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing bilesen from kategori", error = ex.Message });
            }
        }

        [HttpGet("ByKategori/{kategoriId}")]
        public async Task<ActionResult<IEnumerable<BilesenResponseDto>>> GetBilesenlerByKategori(int kategoriId)
        {
            if (kategoriId <= 0)
            {
                return BadRequest(new { message = "Valid KategoriID is required" });
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        b.BilesenID,
                        b.BilesenAdi,
                        b.Birim,
                        b.Stok,
                        b.Fiyat,
                        b.Currency,
                        b.CreatedAt,
                        b.UpdatedAt,
                        kb.Adet
                    FROM BilesenBilgileri b
                    INNER JOIN KategoriBilesenleri kb ON b.BilesenID = kb.BilesenID
                    WHERE kb.KategoriID = @KategoriID
                    ORDER BY b.BilesenAdi;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@KategoriID", kategoriId);

                using var reader = await command.ExecuteReaderAsync();
                var bilesenler = new List<BilesenResponseDto>();

                while (await reader.ReadAsync())
                {
                    bilesenler.Add(new BilesenResponseDto
                    {
                        BilesenID = reader.GetInt32("BilesenID"),
                        BilesenAdi = reader.GetString("BilesenAdi"),
                        Birim = reader.IsDBNull(reader.GetOrdinal("Birim")) ? null : reader.GetString("Birim"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? 0 : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? null : reader.GetString("Currency"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime("UpdatedAt"),
                        Adet = reader.IsDBNull(reader.GetOrdinal("Adet")) ? 1 : reader.GetInt32("Adet")
                    });
                }

                return Ok(bilesenler);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bilesenler", error = ex.Message });
            }
        }

        [HttpPost("AddComponentsToCategory")]
        public async Task<ActionResult> AddComponentsToCategory([FromBody] AddComponentsToCategoryRequest request)
        {
            if (request.KategoriID <= 0)
            {
                return BadRequest(new { message = "Valid KategoriID is required" });
            }

            if (request.Components == null || !request.Components.Any())
            {
                return BadRequest(new { message = "At least one component is required" });
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    foreach (var component in request.Components)
                    {
                        string checkQuery = @"
                    SELECT COUNT(*) 
                    FROM KategoriBilesenleri 
                    WHERE KategoriID = @KategoriID AND BilesenID = @BilesenID";

                        using var checkCommand = new MySqlCommand(checkQuery, connection, transaction);
                        checkCommand.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                        checkCommand.Parameters.AddWithValue("@BilesenID", component.BilesenID);

                        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                        if (!exists)
                        {
                            string insertQuery = @"
                        INSERT INTO KategoriBilesenleri (KategoriID, BilesenID, Adet)
                        VALUES (@KategoriID, @BilesenID, @Adet)";

                            using var insertCommand = new MySqlCommand(insertQuery, connection, transaction);
                            insertCommand.Parameters.AddWithValue("@KategoriID", request.KategoriID);
                            insertCommand.Parameters.AddWithValue("@BilesenID", component.BilesenID);
                            insertCommand.Parameters.AddWithValue("@Adet", component.Adet);

                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    return Ok(new { message = "Components added successfully", count = request.Components.Count });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("Transaction failed", ex);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding components to category", error = ex.Message });
            }
        }

        [HttpDelete("RemoveComponentFromCategory")]
        public async Task<ActionResult> RemoveComponentFromCategory([FromQuery] int kategoriId, [FromQuery] int bilesenId)
        {
            if (kategoriId <= 0 || bilesenId <= 0)
            {
                return BadRequest(new { message = "Valid KategoriID and BilesenID are required" });
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = @"
            SELECT COUNT(*) 
            FROM KategoriBilesenleri 
            WHERE KategoriID = @KategoriID AND BilesenID = @BilesenID";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@KategoriID", kategoriId);
                checkCommand.Parameters.AddWithValue("@BilesenID", bilesenId);

                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return NotFound(new { message = "Component relationship not found in this category" });
                }

                string deleteQuery = @"
            DELETE FROM KategoriBilesenleri 
            WHERE KategoriID = @KategoriID AND BilesenID = @BilesenID";

                using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@KategoriID", kategoriId);
                deleteCommand.Parameters.AddWithValue("@BilesenID", bilesenId);

                var rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Component removed from category successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to remove component from category" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing component from category", error = ex.Message });
            }
        }

    }
}