using crmApi.Models;
using crmApi.Models.crmApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly string _connectionString;

        public TaskController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Server=b4bjbho5nklwlxnxrmko-mysql.services.clever-cloud.com; " +
                "Database=b4bjbho5nklwlxnxrmko; " +
                "User=utc8e2dbwov6tshf; " +
                "Password=39a1Yh2sgUjWC60fe2mR; " +
                "Port=3306;";
        }

        // GET: api/Task
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAllTasks()
        {
            var tasks = new List<TaskResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT t.*, 
                           cu.Ad as CreatedByUserName, cu.Soyad as CreatedByUserSurname,
                           uu.Ad as UpdatedByUserName, uu.Soyad as UpdatedByUserSurname
                    FROM Tasks t
                    LEFT JOIN KullaniciBilgileri cu ON t.CreatedByUserId = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON t.UpdatedByUserId = uu.KullaniciID
                    ORDER BY t.SortOrder, t.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using (var reader = await command.ExecuteReaderAsync())

                    while (await reader.ReadAsync())
                    {
                        var task = new TaskResponseDto
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                            Status = Enum.Parse<Models.TaskStatus>(reader.GetString("Status")),
                            Priority = Enum.Parse<TaskPriority>(reader.GetString("Priority")),
                            DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                            EstimatedTime = reader.IsDBNull("EstimatedTime") ? null : reader.GetString("EstimatedTime"),
                            SortOrder = reader.GetInt32("SortOrder"),
                            CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                            CreatedByUserName = reader.IsDBNull("CreatedByUserName") ? "" : $"{reader.GetString("CreatedByUserName")} {reader.GetString("CreatedByUserSurname")}",
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedByUserId = reader.IsDBNull("UpdatedByUserId") ? null : reader.GetInt32("UpdatedByUserId"),
                            UpdatedByUserName = reader.IsDBNull("UpdatedByUserName") ? null : $"{reader.GetString("UpdatedByUserName")} {reader.GetString("UpdatedByUserSurname")}",
                            UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt")
                        };
                        tasks.Add(task);
                    }

                var taskIds = tasks.Select(t => t.Id).ToList();
                var allAssignedUsers = new Dictionary<int, List<UserResponseDto>>();

                if (taskIds.Any())
                {
                    string userQuery = @"
                            SELECT ta.TaskId, u.KullaniciID, u.KullaniciAdi, u.Ad, u.Soyad, 
                                u.Email, u.Telefon, u.Durum, u.YetkiTuru
                            FROM TaskAssignments ta
                            JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                            WHERE ta.TaskId IN (" + string.Join(",", taskIds) + ")";

                    using var userCommand = new MySqlCommand(userQuery, connection);
                    using var userReader = await userCommand.ExecuteReaderAsync();

                    while (await userReader.ReadAsync())
                    {
                        var taskId = userReader.GetInt32("TaskId");
                        var user = new UserResponseDto { };

                        if (!allAssignedUsers.ContainsKey(taskId))
                            allAssignedUsers[taskId] = new List<UserResponseDto>();

                        allAssignedUsers[taskId].Add(user);
                    }
                }


                foreach (var task in tasks)
                {
                    if (allAssignedUsers.TryGetValue(task.Id, out var users))
                        task.AssignedUsers = users;
                    else
                        task.AssignedUsers = new List<UserResponseDto>();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görevler alınırken hata oluştu", error = ex.Message });
            }

            return Ok(tasks);
        }

        // GET: api/Task/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT t.*, 
                           cu.Ad as CreatedByUserName, cu.Soyad as CreatedByUserSurname,
                           uu.Ad as UpdatedByUserName, uu.Soyad as UpdatedByUserSurname
                    FROM Tasks t
                    LEFT JOIN KullaniciBilgileri cu ON t.CreatedByUserId = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON t.UpdatedByUserId = uu.KullaniciID
                    WHERE t.Id = @id";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var task = new TaskResponseDto
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        Status = Enum.Parse<Models.TaskStatus>(reader.GetString("Status")),
                        Priority = Enum.Parse<TaskPriority>(reader.GetString("Priority")),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        EstimatedTime = reader.IsDBNull("EstimatedTime") ? null : reader.GetString("EstimatedTime"),
                        SortOrder = reader.GetInt32("SortOrder"),
                        CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                        CreatedByUserName = reader.IsDBNull("CreatedByUserName") ? "" : $"{reader.GetString("CreatedByUserName")} {reader.GetString("CreatedByUserSurname")}",
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedByUserId = reader.IsDBNull("UpdatedByUserId") ? null : reader.GetInt32("UpdatedByUserId"),
                        UpdatedByUserName = reader.IsDBNull("UpdatedByUserName") ? null : $"{reader.GetString("UpdatedByUserName")} {reader.GetString("UpdatedByUserSurname")}",
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt")
                    };

                    reader.Close();
                    task.AssignedUsers = await GetTaskAssignedUsers(connection, task.Id);
                    return Ok(task);
                }

                return NotFound(new { message = "Görev bulunamadı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev alınırken hata oluştu", error = ex.Message });
            }
        }

        // POST: api/Task
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                int createdByUserId = createTaskDto.AssignedUserIds.FirstOrDefault();
                if (createdByUserId == 0)
                {
                    return BadRequest(new { message = "CreatedByUserId gerekli" });
                }

                string insertQuery = @"
                    INSERT INTO Tasks (Title, Description, Status, Priority, DueDate, EstimatedTime, SortOrder, CreatedByUserId, CreatedAt)
                    VALUES (@Title, @Description, @Status, @Priority, @DueDate, @EstimatedTime, @SortOrder, @CreatedByUserId, @CreatedAt);
                    SELECT LAST_INSERT_ID();";

                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Title", createTaskDto.Title);
                insertCommand.Parameters.AddWithValue("@Description", createTaskDto.Description ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Status", createTaskDto.Status.ToString());
                insertCommand.Parameters.AddWithValue("@Priority", createTaskDto.Priority.ToString());
                insertCommand.Parameters.AddWithValue("@DueDate", createTaskDto.DueDate ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@EstimatedTime", createTaskDto.EstimatedTime ?? (object)DBNull.Value);
                insertCommand.Parameters.AddWithValue("@SortOrder", createTaskDto.SortOrder);
                insertCommand.Parameters.AddWithValue("@CreatedByUserId", createdByUserId);
                insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var newTaskId = Convert.ToInt32(await insertCommand.ExecuteScalarAsync());

                if (createTaskDto.AssignedUserIds.Any())
                {
                    await AssignUsersToTask(connection, newTaskId, createTaskDto.AssignedUserIds);
                }

                var result = await GetTask(newTaskId);
                return CreatedAtAction(nameof(GetTask), new { id = newTaskId }, ((OkObjectResult)result.Result).Value);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev oluşturulurken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Task/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateTaskDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Tasks WHERE Id = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Görev bulunamadı" });
                }

                var updateFields = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(updateTaskDto.Title))
                {
                    updateFields.Add("Title = @Title");
                    parameters.Add(new MySqlParameter("@Title", updateTaskDto.Title));
                }
                if (updateTaskDto.Description != null)
                {
                    updateFields.Add("Description = @Description");
                    parameters.Add(new MySqlParameter("@Description", updateTaskDto.Description));
                }
                if (updateTaskDto.Status.HasValue)
                {
                    updateFields.Add("Status = @Status");
                    parameters.Add(new MySqlParameter("@Status", updateTaskDto.Status.Value.ToString()));
                }
                if (updateTaskDto.Priority.HasValue)
                {
                    updateFields.Add("Priority = @Priority");
                    parameters.Add(new MySqlParameter("@Priority", updateTaskDto.Priority.Value.ToString()));
                }
                if (updateTaskDto.DueDate.HasValue)
                {
                    updateFields.Add("DueDate = @DueDate");
                    parameters.Add(new MySqlParameter("@DueDate", updateTaskDto.DueDate.Value));
                }
                if (!string.IsNullOrEmpty(updateTaskDto.EstimatedTime))
                {
                    updateFields.Add("EstimatedTime = @EstimatedTime");
                    parameters.Add(new MySqlParameter("@EstimatedTime", updateTaskDto.EstimatedTime));
                }
                if (updateTaskDto.SortOrder.HasValue)
                {
                    updateFields.Add("SortOrder = @SortOrder");
                    parameters.Add(new MySqlParameter("@SortOrder", updateTaskDto.SortOrder.Value));
                }

                if (updateFields.Count > 0)
                {
                    updateFields.Add("UpdatedAt = @UpdatedAt");
                    parameters.Add(new MySqlParameter("@UpdatedAt", DateTime.UtcNow));

                    string updateQuery = $"UPDATE Tasks SET {string.Join(", ", updateFields)} WHERE Id = @id";
                    using var updateCommand = new MySqlCommand(updateQuery, connection);

                    foreach (var param in parameters)
                    {
                        updateCommand.Parameters.Add(param);
                    }
                    updateCommand.Parameters.AddWithValue("@id", id);

                    await updateCommand.ExecuteNonQueryAsync();
                }

                if (updateTaskDto.AssignedUserIds != null)
                {
                    await UpdateTaskAssignments(connection, id, updateTaskDto.AssignedUserIds);
                }

                return Ok(new { message = "Görev başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Task/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, UpdateTaskStatusDto updateStatusDto)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string updateQuery = "UPDATE Tasks SET Status = @Status, SortOrder = @SortOrder, UpdatedAt = @UpdatedAt WHERE Id = @id";
                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@Status", updateStatusDto.Status.ToString());
                updateCommand.Parameters.AddWithValue("@SortOrder", updateStatusDto.SortOrder);
                updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                updateCommand.Parameters.AddWithValue("@id", id);

                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Görev bulunamadı" });
                }

                return Ok(new { message = "Görev durumu başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev durumu güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // PUT: api/Task/bulk-update-order
        [HttpPut("bulk-update-order")]
        public async Task<IActionResult> BulkUpdateTaskOrder(BulkUpdateTaskOrderDto bulkUpdateDto)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    foreach (var taskOrder in bulkUpdateDto.Tasks)
                    {
                        string updateQuery = "UPDATE Tasks SET Status = @Status, SortOrder = @SortOrder, UpdatedAt = @UpdatedAt WHERE Id = @id";
                        using var updateCommand = new MySqlCommand(updateQuery, connection, transaction);
                        updateCommand.Parameters.AddWithValue("@Status", taskOrder.Status.ToString());
                        updateCommand.Parameters.AddWithValue("@SortOrder", taskOrder.SortOrder);
                        updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                        updateCommand.Parameters.AddWithValue("@id", taskOrder.Id);

                        await updateCommand.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    return Ok(new { message = "Görev sıralaması başarıyla güncellendi" });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev sıralaması güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // DELETE: api/Task/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Tasks WHERE Id = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Görev bulunamadı" });
                }

                string deleteQuery = "DELETE FROM Tasks WHERE Id = @id";
                using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@id", id);

                await deleteCommand.ExecuteNonQueryAsync();

                return Ok(new { message = "Görev başarıyla silindi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev silinirken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Task/by-status/{status}
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasksByStatus(Models.TaskStatus status)
        {
            var tasks = new List<TaskResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT t.*, 
                           cu.Ad as CreatedByUserName, cu.Soyad as CreatedByUserSurname,
                           uu.Ad as UpdatedByUserName, uu.Soyad as UpdatedByUserSurname
                    FROM Tasks t
                    LEFT JOIN KullaniciBilgileri cu ON t.CreatedByUserId = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON t.UpdatedByUserId = uu.KullaniciID
                    WHERE t.Status = @status
                    ORDER BY t.SortOrder, t.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@status", status.ToString());

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var task = new TaskResponseDto
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        Status = Enum.Parse<Models.TaskStatus>(reader.GetString("Status")),
                        Priority = Enum.Parse<TaskPriority>(reader.GetString("Priority")),
                        DueDate = reader.IsDBNull("DueDate") ? null : reader.GetDateTime("DueDate"),
                        EstimatedTime = reader.IsDBNull("EstimatedTime") ? null : reader.GetString("EstimatedTime"),
                        SortOrder = reader.GetInt32("SortOrder"),
                        CreatedByUserId = reader.GetInt32("CreatedByUserId"),
                        CreatedByUserName = reader.IsDBNull("CreatedByUserName") ? "" : $"{reader.GetString("CreatedByUserName")} {reader.GetString("CreatedByUserSurname")}",
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        UpdatedByUserId = reader.IsDBNull("UpdatedByUserId") ? null : reader.GetInt32("UpdatedByUserId"),
                        UpdatedByUserName = reader.IsDBNull("UpdatedByUserName") ? null : $"{reader.GetString("UpdatedByUserName")} {reader.GetString("UpdatedByUserSurname")}",
                        UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt")
                    };
                    tasks.Add(task);
                }

                // Get assigned users for each task
                foreach (var task in tasks)
                {
                    task.AssignedUsers = await GetTaskAssignedUsers(connection, task.Id);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görevler alınırken hata oluştu", error = ex.Message });
            }

            return Ok(tasks);
        }

        // POST: api/Task/5/assign
        [HttpPost("{id}/assign")]
        public async Task<IActionResult> AssignUsersToTask(int id, TaskAssignmentDto assignmentDto)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Tasks WHERE Id = @id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;
                if (!exists)
                {
                    return NotFound(new { message = "Görev bulunamadı" });
                }

                await UpdateTaskAssignments(connection, id, assignmentDto.UserIds);

                return Ok(new { message = "Kullanıcılar göreve başarıyla atandı" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı atama işleminde hata oluştu", error = ex.Message });
            }
        }

        // Helper Methods
        private async Task<List<UserResponseDto>> GetTaskAssignedUsers(MySqlConnection connection, int taskId)
        {
            var users = new List<UserResponseDto>();

            string query = @"
                SELECT u.KullaniciID, u.KullaniciAdi, u.Ad, u.Soyad, u.Email, u.Telefon, u.Durum, u.YetkiTuru
                FROM TaskAssignments ta
                JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                WHERE ta.TaskId = @taskId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@taskId", taskId);

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

            return users;
        }

        private async System.Threading.Tasks.Task AssignUsersToTask(MySqlConnection connection, int taskId, List<int> userIds)
        {
            if (!userIds.Any()) return;

            string insertQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
            var values = userIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
            insertQuery += string.Join(", ", values);

            using var command = new MySqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@TaskId", taskId);
            command.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);

            for (int i = 0; i < userIds.Count; i++)
            {
                command.Parameters.AddWithValue($"@UserId{i}", userIds[i]);
            }

            await command.ExecuteNonQueryAsync();
        }

        private async System.Threading.Tasks.Task UpdateTaskAssignments(MySqlConnection connection, int taskId, List<int> userIds)
        {
            string deleteQuery = "DELETE FROM TaskAssignments WHERE TaskId = @TaskId";
            using var deleteCommand = new MySqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@TaskId", taskId);
            await deleteCommand.ExecuteNonQueryAsync();

            if (userIds.Any())
            {
                await AssignUsersToTask(connection, taskId, userIds);
            }
        }
        
        [HttpGet("Assignments")]
        public async Task<ActionResult<IEnumerable<TaskAssignmentResponseDto>>> GetAllTaskAssignments()
        {
            var assignments = new List<TaskAssignmentResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT ta.Id, ta.TaskId, ta.UserId, ta.AssignedAt,
                        t.Title as TaskTitle,
                        u.KullaniciAdi, u.Ad, u.Soyad, u.Email
                    FROM TaskAssignments ta
                    JOIN Tasks t ON ta.TaskId = t.Id
                    JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                    ORDER BY ta.AssignedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    assignments.Add(new TaskAssignmentResponseDto
                    {
                        Id = reader.GetInt32("Id"),
                        TaskId = reader.GetInt32("TaskId"),
                        UserId = reader.GetInt32("UserId"),
                        AssignedAt = reader.GetDateTime("AssignedAt"),
                        TaskTitle = reader.GetString("TaskTitle"),
                        UserName = $"{reader.GetString("Ad")} {reader.GetString("Soyad")}",
                        UserUsername = reader.GetString("KullaniciAdi"),
                        UserEmail = reader.GetString("Email")
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev atamaları alınırken hata oluştu", error = ex.Message });
            }

            return Ok(assignments);
        }

        // GET: api/Task/Assignments/{taskId}
        [HttpGet("Assignments/{taskId}")]
        public async Task<ActionResult<IEnumerable<TaskAssignmentResponseDto>>> GetTaskAssignmentsByTaskId(int taskId)
        {
            var assignments = new List<TaskAssignmentResponseDto>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT ta.Id, ta.TaskId, ta.UserId, ta.AssignedAt,
                        t.Title as TaskTitle,
                        u.KullaniciAdi, u.Ad, u.Soyad, u.Email
                    FROM TaskAssignments ta
                    JOIN Tasks t ON ta.TaskId = t.Id
                    JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                    WHERE ta.TaskId = @TaskId
                    ORDER BY ta.AssignedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@TaskId", taskId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    assignments.Add(new TaskAssignmentResponseDto
                    {
                        Id = reader.GetInt32("Id"),
                        TaskId = reader.GetInt32("TaskId"),
                        UserId = reader.GetInt32("UserId"),
                        AssignedAt = reader.GetDateTime("AssignedAt"),
                        TaskTitle = reader.GetString("TaskTitle"),
                        UserName = $"{reader.GetString("Ad")} {reader.GetString("Soyad")}",
                        UserUsername = reader.GetString("KullaniciAdi"),
                        UserEmail = reader.GetString("Email")
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Görev atamaları alınırken hata oluştu", error = ex.Message });
            }

            return Ok(assignments);
        }
    }
}
