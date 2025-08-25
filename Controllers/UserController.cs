using crmApi.Models.crmApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                "Server=b5l1shhnklxmq4ogmpjc-mysql.services.clever-cloud.com; " +
                "Database=b5l1shhnklxmq4ogmpjc; " +
                "User=udhzqgatlxfof1ji; " +
                "Password=97PRxh88Uohomd51sVF; " +
                "Port=21446;";
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
        {
            var users = new List<UserResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT KullaniciID, Durum, KullaniciAdi, Ad, Soyad, Email, Telefon, YetkiTuru FROM KullaniciBilgileri";
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new UserResponseDto
                    {
                        UserId = Convert.ToInt32(reader["KullaniciID"]),
                        KullaniciAdi = reader.GetString("KullaniciAdi"),
                        Ad = reader.GetString("Ad"),
                        Soyad = reader.GetString("Soyad"),
                        Email = reader.GetString("Email"),
                        Telefon = reader.GetString("Telefon"),
                        Durum = reader.GetString("Durum"),
                        YetkiTuru = reader.GetString("YetkiTuru"),
                        FullName = $"{reader.GetString("Ad")} {reader.GetString("Soyad")}"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcılar alınırken hata oluştu", error = ex.Message });
            }

            return Ok(users);
        }

        // GET: api/User/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT KullaniciID, Durum, KullaniciAdi, Ad, Soyad, Email, Telefon, YetkiTuru FROM KullaniciBilgileri WHERE KullaniciID = @id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var user = new UserResponseDto
                    {
                        KullaniciID = reader.GetInt32("KullaniciID"),
                        KullaniciAdi = reader.GetString("KullaniciAdi"),
                        Ad = reader.GetString("Ad"),
                        Soyad = reader.GetString("Soyad"),
                        Email = reader.GetString("Email"),
                        Telefon = reader.GetString("Telefon"),
                        Durum = reader.GetString("Durum"),
                        YetkiTuru = reader.GetString("YetkiTuru"),
                        FullName = $"{reader.GetString("Ad")} {reader.GetString("Soyad")}"
                    };
                    return Ok(user);
                }

                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı alınırken hata oluştu", error = ex.Message });
            }
        }

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if username already exists
                string checkQuery = "SELECT COUNT(*) FROM KullaniciBilgileri WHERE KullaniciAdi = @KullaniciAdi";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@KullaniciAdi", createUserDto.KullaniciAdi);
                
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                if (count > 0)
                {
                    return Conflict(new { message = "Bu kullanıcı adı zaten kayıtlı. Lütfen farklı bir kullanıcı adı girin." });
                }

                // Insert new user
                string insertQuery = @"INSERT INTO KullaniciBilgileri 
                                     (Durum, KullaniciAdi, Ad, Soyad, Sifre, Email, Telefon, YetkiTuru)
                                     VALUES 
                                     (@Durum, @KullaniciAdi, @Ad, @Soyad, @Sifre, @Email, @Telefon, @YetkiTuru);
                                     SELECT LAST_INSERT_ID();";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Durum", createUserDto.Durum);
                insertCommand.Parameters.AddWithValue("@KullaniciAdi", createUserDto.KullaniciAdi);
                insertCommand.Parameters.AddWithValue("@Ad", createUserDto.Ad);
                insertCommand.Parameters.AddWithValue("@Soyad", createUserDto.Soyad);
                insertCommand.Parameters.AddWithValue("@Sifre", createUserDto.Sifre);
                insertCommand.Parameters.AddWithValue("@Email", createUserDto.Email);
                insertCommand.Parameters.AddWithValue("@Telefon", createUserDto.Telefon);
                insertCommand.Parameters.AddWithValue("@YetkiTuru", createUserDto.YetkiTuru);

                var newUserId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

                // Return the created user
                var createdUser = new UserResponseDto
                {
                    KullaniciID = newUserId,
                    KullaniciAdi = createUserDto.KullaniciAdi,
                    Ad = createUserDto.Ad,
                    Soyad = createUserDto.Soyad,
                    Email = createUserDto.Email,
                    Telefon = createUserDto.Telefon,
                    Durum = createUserDto.Durum,
                    YetkiTuru = createUserDto.YetkiTuru,
                    FullName = $"{createUserDto.Ad} {createUserDto.Soyad}"
                };

                return CreatedAtAction(nameof(GetUser), new { id = newUserId }, createdUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı oluşturulurken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if user exists
                string checkQuery = "SELECT COUNT(*) FROM KullaniciBilgileri WHERE KullaniciID = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);
                
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı" });
                }

                // Check if new username is already taken (if username is being updated)
                if (!string.IsNullOrEmpty(updateUserDto.KullaniciAdi))
                {
                    string usernameCheckQuery = "SELECT COUNT(*) FROM KullaniciBilgileri WHERE KullaniciAdi = @KullaniciAdi AND KullaniciID != @id";
                    using var usernameCheckCommand = new MySqlCommand(usernameCheckQuery, connection);
                    usernameCheckCommand.Parameters.AddWithValue("@KullaniciAdi", updateUserDto.KullaniciAdi);
                    usernameCheckCommand.Parameters.AddWithValue("@id", id);
                    
                    var usernameExists = Convert.ToInt32(await usernameCheckCommand.ExecuteScalarAsync()) > 0;
                    if (usernameExists)
                    {
                        return Conflict(new { message = "Bu kullanıcı adı zaten kayıtlı" });
                    }
                }

                // Build dynamic update query
                var updateFields = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(updateUserDto.KullaniciAdi))
                {
                    updateFields.Add("KullaniciAdi = @KullaniciAdi");
                    parameters.Add(new MySqlParameter("@KullaniciAdi", updateUserDto.KullaniciAdi));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Ad))
                {
                    updateFields.Add("Ad = @Ad");
                    parameters.Add(new MySqlParameter("@Ad", updateUserDto.Ad));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Soyad))
                {
                    updateFields.Add("Soyad = @Soyad");
                    parameters.Add(new MySqlParameter("@Soyad", updateUserDto.Soyad));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Sifre))
                {
                    updateFields.Add("Sifre = @Sifre");
                    parameters.Add(new MySqlParameter("@Sifre", updateUserDto.Sifre));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Email))
                {
                    updateFields.Add("Email = @Email");
                    parameters.Add(new MySqlParameter("@Email", updateUserDto.Email));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Telefon))
                {
                    updateFields.Add("Telefon = @Telefon");
                    parameters.Add(new MySqlParameter("@Telefon", updateUserDto.Telefon));
                }
                if (!string.IsNullOrEmpty(updateUserDto.Durum))
                {
                    updateFields.Add("Durum = @Durum");
                    parameters.Add(new MySqlParameter("@Durum", updateUserDto.Durum));
                }
                if (!string.IsNullOrEmpty(updateUserDto.YetkiTuru))
                {
                    updateFields.Add("YetkiTuru = @YetkiTuru");
                    parameters.Add(new MySqlParameter("@YetkiTuru", updateUserDto.YetkiTuru));
                }

                if (updateFields.Count == 0)
                {
                    return BadRequest(new { message = "Güncellenecek alan bulunamadı" });
                }

                string updateQuery = $"UPDATE KullaniciBilgileri SET {string.Join(", ", updateFields)} WHERE KullaniciID = @id";
                using var updateCommand = new MySqlCommand(updateQuery, connection);
                
                foreach (var param in parameters)
                {
                    updateCommand.Parameters.Add(param);
                }
                updateCommand.Parameters.AddWithValue("@id", id);

                await updateCommand.ExecuteNonQueryAsync();

                return Ok(new { message = "Kullanıcı başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // DELETE: api/User/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if user exists
                string checkQuery = "SELECT COUNT(*) FROM KullaniciBilgileri WHERE KullaniciID = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);
                
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı" });
                }

                // Delete user
                string deleteQuery = "DELETE FROM KullaniciBilgileri WHERE KullaniciID = @id";
                using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@id", id);

                await deleteCommand.ExecuteNonQueryAsync();

                return Ok(new { message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı silinirken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/User/search?query=searchterm
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { message = "Arama terimi boş olamaz" });
            }

            var users = new List<UserResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string searchQuery = @"SELECT KullaniciID, Durum, KullaniciAdi, Ad, Soyad, Email, Telefon, YetkiTuru 
                                     FROM KullaniciBilgileri 
                                     WHERE KullaniciAdi LIKE @query 
                                        OR Ad LIKE @query 
                                        OR Soyad LIKE @query 
                                        OR Email LIKE @query 
                                        OR CONCAT(Ad, ' ', Soyad) LIKE @query";

                using var command = new MySqlCommand(searchQuery, connection);
                command.Parameters.AddWithValue("@query", $"%{query}%");

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    users.Add(new UserResponseDto
                    {
                        KullaniciID = reader.GetInt32("KullaniciID"),
                        KullaniciAdi = reader.GetString("KullaniciAdi"),
                        Ad = reader.GetString("Ad"),
                        Soyad = reader.GetString("Soyad"),
                        Email = reader.GetString("Email"),
                        Telefon = reader.GetString("Telefon"),
                        Durum = reader.GetString("Durum"),
                        YetkiTuru = reader.GetString("YetkiTuru"),
                        FullName = $"{reader.GetString("Ad")} {reader.GetString("Soyad")}"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı araması yapılırken hata oluştu", error = ex.Message });
            }

            return Ok(users);
        }
    }
}
