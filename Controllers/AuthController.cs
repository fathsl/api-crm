using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = new MySqlCommand(
            "SELECT * FROM KullaniciBilgileri WHERE Email = @email AND Sifre = @sifre", 
            connection
        );
        command.Parameters.AddWithValue("@email", request.Email?.Trim());
        command.Parameters.AddWithValue("@sifre", request.Password);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            string durum = reader["Durum"]?.ToString();
            if (durum != "Aktif")
            {
                return Ok(new LoginResponse
                {
                    Success = false,
                    Message = "Account is inactive.",
                    Status = durum,
                });
            }

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                UserId = Convert.ToInt32(reader["KullaniciID"]),
                PermissionType = reader["YetkiTuru"]?.ToString(),
                Status = durum,
                Email = reader["Email"]?.ToString()
            });
        }

        return Unauthorized(new LoginResponse
        {
            Success = false,
            Message = "Incorrect email or password."
        });
        }
    }
}
