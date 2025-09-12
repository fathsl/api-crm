using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using crmApi.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using crmApi.Models.crmApi.Models;

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
                    SELECT c.Id, c.First_name, c.Last_name, c.Phone, c.Email, c.Details, 
                        c.Country, c.City, c.Address, c.ZipCode, c.VatNumber, c.ImageUrl,
                        c.CreatedBy, c.CreatedAt, c.ModifiedBy, c.ModifiedAt,
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
                        City = reader.IsDBNull("City") ? null : reader.GetString("City"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        ZipCode = reader.IsDBNull("ZipCode") ? null : reader.GetString("ZipCode"),
                        VATNumber = reader.IsDBNull("VatNumber") ? null : reader.GetString("VatNumber"),
                        ImageUrl = reader.IsDBNull("ImageUrl") ? null : reader.GetString("ImageUrl"),
                        CreatedBy = reader.IsDBNull("CreatedBy") ? null : reader.GetInt32("CreatedBy"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedBy = reader.IsDBNull("ModifiedBy") ? null : reader.GetInt32("ModifiedBy"),
                        ModifiedAt = reader.IsDBNull("ModifiedAt") ? DateTime.MinValue : reader.GetDateTime("ModifiedAt")
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
        public async Task<ActionResult<ClientResponse>> GetClient(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT c.*, GROUP_CONCAT(cp.project_id) as ProjectIds
                    FROM Clients c
                    LEFT JOIN ClientProjects cp ON c.Id = cp.client_id
                    WHERE c.Id = @id
                    GROUP BY c.Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var projectIds = new List<int>();
                    if (!reader.IsDBNull("ProjectIds"))
                    {
                        var projectIdsStr = reader.GetString("ProjectIds");
                        projectIds = projectIdsStr.Split(',')
                                                .Where(x => int.TryParse(x, out _))
                                                .Select(int.Parse)
                                                .ToList();
                    }

                    var client = new ClientResponse
                    {
                        Id = reader.GetInt32("Id"),
                        First_name = reader.GetString("First_name"),
                        Last_name = reader.GetString("Last_name"),
                        Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        Country = reader.IsDBNull("Country") ? null : reader.GetString("Country"),
                        City = reader.IsDBNull("City") ? null : reader.GetString("City"),
                        Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
                        ProjectIds = projectIds,
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    };

                    return Ok(client);
                }

                return NotFound(new { message = "Client not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching client", error = ex.Message });
            }
        }

        // POST: api/Clients
        [HttpPost]
        public async Task<ActionResult> CreateClient([FromForm] Client clientDto, IFormFile? file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clientDto.First_name))
                {
                    return BadRequest(new { message = "First name is required" });
                }

                if (string.IsNullOrWhiteSpace(clientDto.Last_name))
                {
                    return BadRequest(new { message = "Last name is required" });
                }

                if (string.IsNullOrWhiteSpace(clientDto.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }

                string? cloudinaryUrl = null;
                if (file != null && file.Length > 0)
                {
                    if (!file.ContentType.StartsWith("image/"))
                    {
                        return BadRequest(new { message = "Only image files are allowed" });
                    }
                    try
                    {
                        cloudinaryUrl = await UploadToCloudinary(file, file.FileName);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { message = $"File upload failed: {ex.Message}" });
                    }
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            INSERT INTO Clients
                (First_name, Last_name, Phone, Email, Details, Country, CreatedBy, CreatedAt, City, Address, ZipCode, VATNumber, ImageUrl)
            VALUES
                (@First_name, @Last_name, @Phone, @Email, @Details, @Country, @CreatedBy, @CreatedAt, @City, @Address, @ZipCode, @VATNumber, @ImageUrl)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@First_name", clientDto.First_name);
                command.Parameters.AddWithValue("@Last_name", clientDto.Last_name);
                command.Parameters.AddWithValue("@Phone", (object?)clientDto.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", clientDto.Email);
                command.Parameters.AddWithValue("@Details", (object?)clientDto.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@Country", (object?)clientDto.Country ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", clientDto.CreatedBy);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@City", (object?)clientDto.City ?? DBNull.Value);
                command.Parameters.AddWithValue("@Address", (object?)clientDto.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@ZipCode", (object?)clientDto.ZipCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@VATNumber", (object?)clientDto.VATNumber ?? DBNull.Value);
                command.Parameters.AddWithValue("@ImageUrl", (object?)cloudinaryUrl ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();

                return Ok(new
                {
                    message = "Müşteri başarıyla oluşturuldu",
                    imageUrl = cloudinaryUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating client: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    message = "Müşteri oluşturulurken hata oluştu",
                    error = ex.Message
                });
            }
        }


        // PUT: api/Clients/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateClient(int id, [FromBody] UpdateClientDto clientDto)
        {
            using var connection = new MySqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
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
                        ModifiedAt = @ModifiedAt,
                        City = @City,
                        Address = @Address,
                        projectId = @projectId
                    WHERE Id = @Id";

                using var command = new MySqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@First_name", clientDto.First_name);
                command.Parameters.AddWithValue("@Last_name", clientDto.Last_name);
                command.Parameters.AddWithValue("@Phone", (object?)clientDto.Phone ?? DBNull.Value);
                command.Parameters.AddWithValue("@Email", (object?)clientDto.Email ?? DBNull.Value);
                command.Parameters.AddWithValue("@Details", (object?)clientDto.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@Country", (object?)clientDto.Country ?? DBNull.Value);
                command.Parameters.AddWithValue("@ModifiedBy", clientDto.ModifiedBy);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@City", (object?)clientDto.City ?? DBNull.Value);
                command.Parameters.AddWithValue("@Address", (object?)clientDto.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@projectId", clientDto.projectId ?? (object)DBNull.Value);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Client not found" });
                }

                // Update project associations
                string deleteProjects = "DELETE FROM ClientProjects WHERE client_id = @clientId;";
                using var deleteCmd = new MySqlCommand(deleteProjects, connection, transaction);
                deleteCmd.Parameters.AddWithValue("@clientId", id);
                await deleteCmd.ExecuteNonQueryAsync();

                if (clientDto.ProjectIds?.Any() == true)
                {
                    string insertProjects = "INSERT INTO ClientProjects (client_id, project_id) VALUES ";
                    var values = clientDto.ProjectIds.Select((pId, index) => $"(@clientId, @projectId{index})");
                    insertProjects += string.Join(", ", values);

                    using var insertCmd = new MySqlCommand(insertProjects, connection, transaction);
                    insertCmd.Parameters.AddWithValue("@clientId", id);
                    for (int i = 0; i < clientDto.ProjectIds.Count; i++)
                    {
                        insertCmd.Parameters.AddWithValue($"@projectId{i}", clientDto.ProjectIds[i]);
                    }
                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { message = "Client updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error updating client", error = ex.Message });
            }
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


        [HttpPost("{clientId}/resources")]
        public async Task<ActionResult<ResourceResponse>> AddResource(int clientId, [FromForm] AddResourceDto resourceDto, IFormFile? file, IFormFile? audioFile)
        {
            try
            {
                string? fileUrl = null;
                string? voiceUrl = null;

                if (file != null)
                {
                    fileUrl = await UploadToCloudinary(file, file.FileName);
                }
                else if (audioFile != null)
                {
                    voiceUrl = await UploadAudioToCloudinary(audioFile, audioFile.FileName);
                }
                else
                {
                    return BadRequest(new { message = "No file or audio provided" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string insertQuery = @"
                    INSERT INTO ClientResources (client_id, title, description, fileUrl, voiceUrl, createdBy)
                    VALUES (@clientId, @title, @description, @fileUrl, @voiceUrl, @createdBy);
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@clientId", clientId);
                command.Parameters.AddWithValue("@title", resourceDto.Title ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@description", resourceDto.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@fileUrl", fileUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@voiceUrl", voiceUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@createdBy", resourceDto.CreatedBy);

                var resourceId = Convert.ToInt32(await command.ExecuteScalarAsync());

                return Ok(new ResourceResponse { Id = resourceId, FileUrl = fileUrl, VoiceUrl = voiceUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding resource", error = ex.Message });
            }
        }

        [HttpGet("{clientId}/resources")]
        public async Task<ActionResult<List<ResourceResponse>>> GetResources(int clientId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT id, title, description, fileUrl, voiceUrl, createdAt, createdBy
                    FROM ClientResources WHERE client_id = @clientId ORDER BY createdAt DESC;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientId", clientId);

                var resources = new List<ResourceResponse>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resources.Add(new ResourceResponse
                    {
                        Id = reader.GetInt32("id"),
                        Title = reader.IsDBNull("title") ? null : reader.GetString("title"),
                        Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                        FileUrl = reader.IsDBNull("fileUrl") ? null : reader.GetString("fileUrl"),
                        VoiceUrl = reader.IsDBNull("voiceUrl") ? null : reader.GetString("voiceUrl"),
                        CreatedAt = reader.GetDateTime("createdAt"),
                        CreatedBy = reader.GetInt32("createdBy")
                    });
                }

                return Ok(resources);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching resources", error = ex.Message });
            }
        }

        [HttpGet("{clientId}/tasks")]
        public async Task<ActionResult<List<TaskResponse>>> GetClientTasks(int clientId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.EstimatedTime, t.SortOrder,
                        t.CreatedByUserId, t.CreatedAt, t.UpdatedByUserId, t.UpdatedAt,
                        u.KullaniciAdi AS CreatedByName, u2.KullaniciAdi AS UpdatedByName
                    FROM Tasks t
                    JOIN TaskClients tc ON t.Id = tc.TaskId
                    LEFT JOIN KullaniciBilgileri u ON t.CreatedByUserId = u.KullaniciID
                    LEFT JOIN KullaniciBilgileri u2 ON t.UpdatedByUserId = u2.KullaniciID
                    WHERE tc.ClientId = @clientId
                    ORDER BY t.CreatedAt DESC;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@clientId", clientId);

                var tasks = new List<TaskResponse>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Enum.TryParse<crmApi.Models.TaskStatus>(reader["Status"].ToString(), out var status);
                    if (!Enum.IsDefined(typeof(crmApi.Models.TaskStatus), status))
                        status = crmApi.Models.TaskStatus.Backlog;

                    Enum.TryParse<TaskPriority>(reader["Priority"].ToString(), out var priority);
                    if (!Enum.IsDefined(typeof(TaskPriority), priority))
                        priority = TaskPriority.Medium;

                    var task = new TaskResponse
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader["Title"].ToString() ?? string.Empty,
                        Description = reader["Description"].ToString() ?? string.Empty,
                        Status = status,
                        Priority = priority,
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        EstimatedTime = reader["EstimatedTime"].ToString() ?? string.Empty,
                        SortOrder = reader.GetInt32("SortOrder"),
                        CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        AssignedUsers = new List<AssignedUser>()
                    };
                    tasks.Add(task);
                }
                reader.Close();

                foreach (var task in tasks)
                {
                    string assignQuery = @"
                        SELECT ta.UserId, u.KullaniciID, u.KullaniciAdi, u.Ad, u.Soyad, u.Email, u.Telefon, u.Durum
                        FROM TaskAssignments ta 
                        LEFT JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID 
                        WHERE ta.TaskId = @taskId;";

                    using var assignCmd = new MySqlCommand(assignQuery, connection);
                    assignCmd.Parameters.AddWithValue("@taskId", task.Id);
                    using var assignReader = await assignCmd.ExecuteReaderAsync();
                    while (await assignReader.ReadAsync())
                    {
                        if (!assignReader.IsDBNull("KullaniciID"))
                        {
                            task.AssignedUsers.Add(new AssignedUser
                            {
                                KullaniciID = assignReader.GetInt32("KullaniciID"),
                                KullaniciAdi = assignReader["KullaniciAdi"]?.ToString() ?? string.Empty,
                                Ad = assignReader["Ad"]?.ToString() ?? string.Empty,
                                Soyad = assignReader["Soyad"]?.ToString() ?? string.Empty,
                                Email = assignReader["Email"]?.ToString() ?? string.Empty,
                                Telefon = assignReader["Telefon"]?.ToString() ?? string.Empty,
                                Durum = assignReader["Durum"]?.ToString() ?? string.Empty,
                            });
                        }
                    }
                    assignReader.Close();
                }

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching client tasks", error = ex.Message });
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
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(fileName, stream),
                    PublicId = $"chat-files/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}",
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
                throw;
            }
        }

        private async Task<string> UploadAudioToCloudinary(IFormFile audioFile, string fileName)
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

                using var stream = audioFile.OpenReadStream();
                var uploadParams = new VideoUploadParams()
                {
                    File = new FileDescription(fileName, stream),
                    PublicId = $"voice-messages/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}",
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary audio upload failed: {uploadResult.Error.Message}");
                }

                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        

    }
}
