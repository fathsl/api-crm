using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using crmApi.Models;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly string _connectionString;

        public ClientsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetAllClients()
        {
            var clients = new List<Client>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.*, 
                           cu.Ad AS CreatedByUserName, cu.Soyad AS CreatedByUserSurname,
                           uu.Ad AS ModifiedByUserName, uu.Soyad AS ModifiedByUserSurname
                    FROM Clients c
                    LEFT JOIN KullaniciBilgileri cu ON c.CreatedBy = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON c.ModifiedBy = uu.KullaniciID
                    ORDER BY c.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var client = new Client
                    {
                        Id = reader.GetInt32("Id"),
                        First_name = reader.GetString("First_name"),
                        Last_name = reader.GetString("Last_name"),
                        Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        Country = reader.IsDBNull("Country") ? null : reader.GetString("Country"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt32("ModifiedBy"),
                        ModifiedAt = reader.IsDBNull("ModifiedAt") ? DateTime.MinValue : reader.GetDateTime("ModifiedAt"),
                        City = reader.IsDBNull("City") ? null : reader.GetString("City"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address")
                    };

                    clients.Add(client);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Müşteriler alınırken hata oluştu", error = ex.Message });
            }

            return Ok(clients);
        }

        // GET: api/Clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClientById(int id)
        {
            Client client = null;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM Clients WHERE Id = @Id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    client = new Client
                    {
                        Id = reader.GetInt32("Id"),
                        First_name = reader.GetString("First_name"),
                        Last_name = reader.GetString("Last_name"),
                        Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        Country = reader.IsDBNull("Country") ? null : reader.GetString("Country"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt32("ModifiedBy"),
                        ModifiedAt = reader.IsDBNull("ModifiedAt") ? DateTime.MinValue : reader.GetDateTime("ModifiedAt"),
                        City = reader.GetString("City"),
                        Address = reader.GetString("Address")
                    };
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Müşteri alınırken hata oluştu", error = ex.Message });
            }

            if (client == null)
                return NotFound(new { message = "Müşteri bulunamadı" });

            return Ok(client);
        }

        // POST: api/Clients
        [HttpPost]
        public async Task<ActionResult> CreateClient([FromBody] Client client)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Clients 
                        (First_name, Last_name, Phone, Email, Details, Country, CreatedBy, CreatedAt, City, Address) 
                    VALUES 
                        (@First_name, @Last_name, @Phone, @Email, @Details, @Country, @CreatedBy, @CreatedAt, @City, @Address)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@First_name", client.First_name);
                command.Parameters.AddWithValue("@Last_name", client.Last_name);
                command.Parameters.AddWithValue("@Phone", (object?)client.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)client.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@Details", (object?)client.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@Country", (object?)client.Country ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", client.CreatedBy);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@City", client.City);
                command.Parameters.AddWithValue("@Address", client.Address);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Müşteri oluşturulurken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Müşteri başarıyla oluşturuldu" });
        }

        // PUT: api/Clients/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateClient(int id, [FromBody] Client client)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE Clients
                    SET First_name = @First_name, 
                        Last_name = @Last_name, 
                        Phone = @Phone,
                        Email = @Email,
                        Details = @Details,
                        Country = @Country,
                        ModifiedBy = @ModifiedBy,
                        ModifiedAt = @ModifiedAt
                        City = @City,
                        Address = @Address
                    WHERE Id = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@First_name", client.First_name);
                command.Parameters.AddWithValue("@Last_name", client.Last_name);
                command.Parameters.AddWithValue("@Phone", (object?)client.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)client.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@Details", (object?)client.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@Country", (object?)client.Country ?? DBNull.Value);
                command.Parameters.AddWithValue("@ModifiedBy", client.ModifiedBy);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@City", client.City);
                command.Parameters.AddWithValue("@Address", client.Address);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Müşteri bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Müşteri güncellenirken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Müşteri başarıyla güncellendi" });
        }

        // DELETE: api/Clients/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteClient(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM Clients WHERE Id = @Id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Müşteri bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Müşteri silinirken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Müşteri başarıyla silindi" });
        }
    }
}
