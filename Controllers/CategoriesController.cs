using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;
using System.Data;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

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
                        ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString("ImageUrl"),
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
                        ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString("ImageUrl"),
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
        public async Task<ActionResult<CategoryResponseDto>> CreateCategory([FromForm] CreateCategoryDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string? imageUrl = null;

                if (createDto.Image != null && createDto.Image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var extension = Path.GetExtension(createDto.Image.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { message = "Invalid file type. Only image files are allowed." });
                    }

                    if (createDto.Image.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "File size exceeds 5MB limit." });
                    }

                    imageUrl = await UploadToCloudinary(createDto.Image, createDto.Image.FileName);
                }
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string insertQuery = @"
                    INSERT INTO UrunKategorileri (KategoriAdi, Stok, Fiyat, CreatedBy, CreatedAt, UpdatedAt, Currency, ImageUrl)
                    VALUES (@KategoriAdi, @Stok, @Fiyat, @CreatedBy, @CreatedAt, @UpdatedAt, @Currency, @ImageUrl);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@KategoriAdi", createDto.KategoriAdi);
                command.Parameters.AddWithValue("@Stok", createDto.Stok ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fiyat", createDto.Fiyat);
                command.Parameters.AddWithValue("@Currency", createDto.Currency ?? "TRY");
                command.Parameters.AddWithValue("@ImageUrl", imageUrl ?? (object)DBNull.Value);
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT ImageUrl FROM UrunKategorileri WHERE KategoriID = @Id";
                string? existingImageUrl = null;

                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var result = await checkCommand.ExecuteScalarAsync();
                    
                    if (result == null)
                    {
                        return NotFound(new { message = "Category not found" });
                    }

                    existingImageUrl = result == DBNull.Value ? null : result.ToString();
                }

                string? imageUrl = existingImageUrl;

                if (updateDto.Image != null && updateDto.Image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var extension = Path.GetExtension(updateDto.Image.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { message = "Invalid file type. Only image files are allowed." });
                    }

                    if (updateDto.Image.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "File size exceeds 5MB limit." });
                    }

                    imageUrl = await UploadToCloudinary(updateDto.Image, updateDto.Image.FileName);

                }

                string updateQuery = @"
                    UPDATE UrunKategorileri 
                    SET KategoriAdi = @KategoriAdi,
                        Stok = @Stok,
                        Fiyat = @Fiyat,
                        Currency = @Currency,
                        ImageUrl = @ImageUrl,
                        UpdatedBy = @UpdatedBy,
                        UpdatedAt = @UpdatedAt
                    WHERE KategoriID = @Id";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@KategoriAdi", updateDto.KategoriAdi);
                command.Parameters.AddWithValue("@Stok", updateDto.Stok ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Fiyat", updateDto.Fiyat);
                command.Parameters.AddWithValue("@Currency", updateDto.Currency ?? "TRY");
                command.Parameters.AddWithValue("@ImageUrl", imageUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedBy", updateDto.UpdatedBy);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync();

                return Ok(new { message = "Category updated successfully", ImageUrl = imageUrl });
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

        private async Task<string> UploadToCloudinary(IFormFile file, string fileName)
        {
            try
            {
                var cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
                var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
                var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

                if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    throw new Exception("Cloudinary credentials not configured");
                }

                var account = new Account(cloudName, apiKey, apiSecret);
                var cloudinary = new Cloudinary(account);

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileName, stream),
                    PublicId = $"category-images/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Image upload failed: {ex.Message}", ex);
            }
        }

    }
}