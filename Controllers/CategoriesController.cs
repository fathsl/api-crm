using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly string _connectionString;

        public CategoriesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAllCategories()
        {
            var categories = new List<CategoryResponseDto>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT uk.*,
                           cb.Ad AS CreatedByFirstName, cb.Soyad AS CreatedByLastName,
                           ub.Ad AS UpdatedByFirstName, ub.Soyad AS UpdatedByLastName
                    FROM UrunKategorileri uk
                    LEFT JOIN KullaniciBilgileri cb ON uk.CreatedBy = cb.KullaniciID
                    LEFT JOIN KullaniciBilgileri ub ON uk.UpdatedBy = ub.KullaniciID
                    ORDER BY uk.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var createdByFirstName = reader.IsDBNull(reader.GetOrdinal("CreatedByFirstName")) ? "Unknown" : reader.GetString("CreatedByFirstName");
                    var createdByLastName = reader.IsDBNull(reader.GetOrdinal("CreatedByLastName")) ? "" : reader.GetString("CreatedByLastName");
                    var updatedByFirstName = reader.IsDBNull(reader.GetOrdinal("UpdatedByFirstName")) ? null : reader.GetString("UpdatedByFirstName");
                    var updatedByLastName = reader.IsDBNull(reader.GetOrdinal("UpdatedByLastName")) ? null : reader.GetString("UpdatedByLastName");

                    var category = new CategoryResponseDto
                    {
                        KategoriID = reader.GetInt32("KategoriID"),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? null : reader.GetString("KategoriAdi"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? null : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "TRY" : reader.GetString("Currency"),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? 0 : reader.GetInt32("CreatedBy"),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetInt32("UpdatedBy"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.UtcNow : reader.GetDateTime("UpdatedAt"),
                        CreatedByName = $"{createdByFirstName} {createdByLastName}".Trim(),
                        UpdatedByName = updatedByFirstName != null && updatedByLastName != null ?
                                       $"{updatedByFirstName} {updatedByLastName}".Trim() : null
                    };
                    categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
            }

            return Ok(categories);
        }

        // GET: api/Categories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDetailDto>> GetCategory(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT uk.*,
                           cb.Ad AS CreatedByFirstName, cb.Soyad AS CreatedByLastName,
                           ub.Ad AS UpdatedByFirstName, ub.Soyad AS UpdatedByLastName
                    FROM UrunKategorileri uk
                    LEFT JOIN KullaniciBilgileri cb ON uk.CreatedBy = cb.KullaniciID
                    LEFT JOIN KullaniciBilgileri ub ON uk.UpdatedBy = ub.KullaniciID
                    WHERE uk.KategoriID = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var createdByFirstName = reader.IsDBNull(reader.GetOrdinal("CreatedByFirstName")) ? "Unknown" : reader.GetString("CreatedByFirstName");
                    var createdByLastName = reader.IsDBNull(reader.GetOrdinal("CreatedByLastName")) ? "" : reader.GetString("CreatedByLastName");
                    var updatedByFirstName = reader.IsDBNull(reader.GetOrdinal("UpdatedByFirstName")) ? null : reader.GetString("UpdatedByFirstName");
                    var updatedByLastName = reader.IsDBNull(reader.GetOrdinal("UpdatedByLastName")) ? null : reader.GetString("UpdatedByLastName");

                    var category = new CategoryDetailDto
                    {
                        KategoriID = reader.GetInt32("KategoriID"),
                        KategoriAdi = reader.IsDBNull(reader.GetOrdinal("KategoriAdi")) ? null : reader.GetString("KategoriAdi"),
                        Stok = reader.IsDBNull(reader.GetOrdinal("Stok")) ? null : reader.GetInt32("Stok"),
                        Fiyat = reader.IsDBNull(reader.GetOrdinal("Fiyat")) ? 0 : reader.GetDecimal("Fiyat"),
                        Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? "TRY" : reader.GetString("Currency"),
                        CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? 0 : reader.GetInt32("CreatedBy"),
                        UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetInt32("UpdatedBy"),
                        CreatedAt = reader.IsDBNull(reader.GetOrdinal("CreatedAt")) ? DateTime.UtcNow : reader.GetDateTime("CreatedAt"),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? DateTime.UtcNow : reader.GetDateTime("UpdatedAt"),
                        CreatedByName = $"{createdByFirstName} {createdByLastName}".Trim(),
                        UpdatedByName = updatedByFirstName != null && updatedByLastName != null ?
                                       $"{updatedByFirstName} {updatedByLastName}".Trim() : null
                    };
                    return Ok(category);
                }

                return NotFound(new { message = "Category not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving category", error = ex.Message });
            }
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<CategoryResponseDto>> CreateCategory([FromBody] CreateCategoryDto createDto)
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
                    INSERT INTO UrunKategorileri (KategoriAdi, Stok, Fiyat, CreatedBy, CreatedAt, UpdatedAt, Currency)
                    VALUES (@KategoriAdi, @Stok, @Fiyat, @CreatedBy, @CreatedAt, @UpdatedAt, @Currency);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@KategoriAdi", createDto.KategoriAdi);
                command.Parameters.AddWithValue("@Stok", createDto.Stok ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fiyat", createDto.Fiyat);
                command.Parameters.AddWithValue("@Currency", createDto.Currency ?? "TRY");
                command.Parameters.AddWithValue("@CreatedBy", createDto.CreatedBy);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                int categoryId = Convert.ToInt32(await command.ExecuteScalarAsync());

                return CreatedAtAction(nameof(GetCategory), new { id = categoryId }, 
                    new { KategoriID = categoryId, message = "Category created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category", error = ex.Message });
            }
        }

        // PUT: api/Categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM UrunKategorileri WHERE KategoriID = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        return NotFound(new { message = "Category not found" });
                    }
                }

                string updateQuery = @"
                    UPDATE UrunKategorileri 
                    SET KategoriAdi = @KategoriAdi,
                        Stok = @Stok,
                        Fiyat = @Fiyat,
                        Currency = @Currency,
                        UpdatedBy = @UpdatedBy,
                        UpdatedAt = @UpdatedAt
                    WHERE KategoriID = @Id";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@KategoriAdi", updateDto.KategoriAdi);
                command.Parameters.AddWithValue("@Stok", updateDto.Stok ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fiyat", updateDto.Fiyat);
                command.Parameters.AddWithValue("@Currency", updateDto.Currency ?? "TRY");
                command.Parameters.AddWithValue("@UpdatedBy", updateDto.UpdatedBy);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category", error = ex.Message });
            }
        }

        // DELETE: api/Categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM UrunKategorileri WHERE KategoriID = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        return NotFound(new { message = "Category not found" });
                    }
                }

                string deleteQuery = "DELETE FROM UrunKategorileri WHERE KategoriID = @Id";
                using var command = new MySqlCommand(deleteQuery, connection);
                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category", error = ex.Message });
            }
        }
    }
}