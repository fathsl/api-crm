using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly string _connectionString;

        public ProjectController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Server=b4bjbho5nklwlxnxrmko-mysql.services.clever-cloud.com; " +
                "Database=b4bjbho5nklwlxnxrmko; " +
                "User=utc8e2dbwov6tshf; " +
                "Password=39a1Yh2sgUjWC60fe2mR; " +
                "Port=3306;";
        }

        // GET: api/Project
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetAllProjects()
        {
            var projects = new List<Project>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT p.*, 
                           cu.Ad AS CreatedByUserName, cu.Soyad AS CreatedByUserSurname,
                           uu.Ad AS UpdatedByUserName, uu.Soyad AS UpdatedByUserSurname
                    FROM Projects p
                    LEFT JOIN KullaniciBilgileri cu ON p.CreatedByUserId = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON p.UpdatedByUserId = uu.KullaniciID
                    ORDER BY p.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var project = new Project
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedByUserId = reader.IsDBNull("UpdatedByUserId") ? null : reader.GetInt32("UpdatedByUserId"),
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
                        Status = Enum.Parse<ProjectStatus>(reader.GetString("Status")),
                        EstimationTime = reader.IsDBNull("EstimationTime") ? null : reader.GetString("EstimationTime"),
                        StartDate = reader.IsDBNull("StartDate") ? null : reader.GetDateTime("StartDate"),
                        EndDate = reader.IsDBNull("EndDate") ? null : reader.GetDateTime("EndDate")
                    };

                    projects.Add(project);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Projeler alınırken hata oluştu", error = ex.Message });
            }

            return Ok(projects);
        }

        // GET: api/Project/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProjectById(int id)
        {
            Project project = null;

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT * FROM Projects WHERE Id = @Id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    project = new Project
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedByUserId = reader.IsDBNull("UpdatedByUserId") ? null : reader.GetInt32("UpdatedByUserId"),
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
                        Status = Enum.Parse<ProjectStatus>(reader.GetString("Status")),
                        EstimationTime = reader.IsDBNull("EstimationTime") ? null : reader.GetString("EstimationTime"),
                        StartDate = reader.IsDBNull("StartDate") ? null : reader.GetDateTime("StartDate"),
                        EndDate = reader.IsDBNull("EndDate") ? null : reader.GetDateTime("EndDate")
                    };
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Proje alınırken hata oluştu", error = ex.Message });
            }

            if (project == null)
                return NotFound(new { message = "Proje bulunamadı" });

            return Ok(project);
        }

        // POST: api/Project
        [HttpPost]
        public async Task<ActionResult> CreateProject([FromBody] Project project)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Projects (Title, Details, CreatedByUserId, CreatedAt, Status, EstimationTime, StartDate, EndDate) 
                    VALUES (@Title, @Details, @CreatedByUserId, @CreatedAt, @Status, @EstimationTime, @StartDate, @EndDate)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", project.Title);
                command.Parameters.AddWithValue("@Details", (object?)project.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedByUserId", project.CreatedByUserId);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Status", project.Status.ToString());
                command.Parameters.AddWithValue("@EstimationTime", (object?)project.EstimationTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartDate", (object?)project.StartDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@EndDate", (object?)project.EndDate ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Proje oluşturulurken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Proje başarıyla oluşturuldu" });
        }

        // PUT: api/Project/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateProject(int id, [FromBody] Project project)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    UPDATE Projects
                    SET Title = @Title, 
                        Details = @Details, 
                        UpdatedByUserId = @UpdatedByUserId,
                        UpdatedAt = @UpdatedAt,
                        Status = @Status,
                        EstimationTime = @EstimationTime,
                        StartDate = @StartDate,
                        EndDate = @EndDate
                    WHERE Id = @Id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", project.Title);
                command.Parameters.AddWithValue("@Details", (object?)project.Details ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedByUserId", project.UpdatedByUserId);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Status", project.Status.ToString());
                command.Parameters.AddWithValue("@EstimationTime", (object?)project.EstimationTime ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartDate", (object?)project.StartDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@EndDate", (object?)project.EndDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@Id", id);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Proje bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Proje güncellenirken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Proje başarıyla güncellendi" });
        }

        // DELETE: api/Project/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProject(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = "DELETE FROM Projects WHERE Id = @Id";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { message = "Proje bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Proje silinirken hata oluştu", error = ex.Message });
            }

            return Ok(new { message = "Proje başarıyla silindi" });
        }
    }
}
