using Amazon.S3;
using CloudinaryDotNet.Actions;
using crmApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;
using crmApi.Models.crmApi.Models;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IConfiguration configuration, ILogger<ChatController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                "Server=b5l1shhnklxmq4ogmpjc-mysql.services.clever-cloud.com;Database=b5l1shhnklxmq4ogmpjc;User=udhzqgatlxfof1ji;Password=97PRxh88Uohomd51sVF;Port=21446;";
            _logger = logger;
        }

        // ✅ Get all discussions for a user
        [HttpGet("discussions/{userId}")]
        public async Task<ActionResult<List<DiscussionResponse>>> GetUserDiscussions(int userId)
        {
            var discussions = new List<DiscussionResponse>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string roleQuery = "SELECT YetkiTuru FROM KullaniciBilgileri WHERE KullaniciID = @userId";
                using var roleCommand = new MySqlCommand(roleQuery, connection);
                roleCommand.Parameters.AddWithValue("@userId", userId);
                var roleResult = await roleCommand.ExecuteScalarAsync();
                string userRole = roleResult?.ToString() ?? "";

                string query = @"
                    SELECT DISTINCT d.Id, d.Title, d.Description, d.Status, d.CreatedByUserId, d.CreatedAt, d.ClientId,
                        COALESCE(d.SenderId, cm.SenderId) as SenderId, 
                        COALESCE(d.ReceiverId, cm.ReceiverId) as ReceiverId, 
                        lt.Status as LastTaskStatus,
                        sender.KullaniciAdi as SenderName, 
                        receiver.KullaniciAdi as ReceiverName,
                        creator.KullaniciAdi as CreatorName
                    FROM Discussions d
                    LEFT JOIN ChatMessages cm ON d.Id = cm.DiscussionId
                    LEFT JOIN KullaniciBilgileri sender ON COALESCE(d.SenderId, cm.SenderId) = sender.KullaniciID
                    LEFT JOIN KullaniciBilgileri receiver ON COALESCE(d.ReceiverId, cm.ReceiverId) = receiver.KullaniciID
                    LEFT JOIN KullaniciBilgileri creator ON d.CreatedByUserId = creator.KullaniciID
                    LEFT JOIN DiscussionAssignedUsers dau ON d.Id = dau.DiscussionId
                    LEFT JOIN DiscussionParticipants dp ON d.Id = dp.DiscussionId
                    LEFT JOIN Clients c ON d.ClientId = c.Id
                    LEFT JOIN (
                        SELECT cm1.DiscussionId, t1.Status
                        FROM ChatMessages cm1
                        INNER JOIN Tasks t1 ON cm1.TaskId = t1.Id
                        WHERE cm1.TaskId IS NOT NULL
                        AND cm1.Id = (
                            SELECT cm2.Id 
                            FROM ChatMessages cm2 
                            WHERE cm2.DiscussionId = cm1.DiscussionId 
                            AND cm2.TaskId IS NOT NULL
                            ORDER BY cm2.CreatedAt DESC 
                            LIMIT 1
                        )
                    ) lt ON d.Id = lt.DiscussionId
                    WHERE d.CreatedByUserId = @userId 
                    OR COALESCE(d.SenderId, cm.SenderId) = @userId 
                    OR COALESCE(d.ReceiverId, cm.ReceiverId) = @userId
                    OR dau.AssignedUserId = @userId
                    OR dp.UserId = @userId
                    ORDER BY d.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    crmApi.Models.TaskStatus? lastTaskStatus = null;
                    if (reader["LastTaskStatus"] != DBNull.Value)
                    {
                        if (Enum.TryParse<crmApi.Models.TaskStatus>(reader["LastTaskStatus"].ToString(), out crmApi.Models.TaskStatus status))
                        {
                            lastTaskStatus = status;
                        }
                    }

                    byte discussionStatus = 0;
                    if (reader["Status"] != DBNull.Value)
                    {
                        discussionStatus = Convert.ToByte(reader["Status"]);
                    }

                    discussions.Add(new DiscussionResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        SenderId = reader["SenderId"] != DBNull.Value ? Convert.ToInt32(reader["SenderId"]) : 0,
                        ReceiverId = reader["ReceiverId"] != DBNull.Value ? Convert.ToInt32(reader["ReceiverId"]) : 0,
                        SenderName = reader["SenderName"]?.ToString() ?? "",
                        ReceiverName = reader["ReceiverName"]?.ToString() ?? "",
                        CreatorName = reader["CreatorName"]?.ToString() ?? "",
                        Status = discussionStatus,
                        ClientId = reader["ClientId"] != DBNull.Value ? Convert.ToInt32(reader["ClientId"]) : (int?)null,
                        LastTaskStatus = lastTaskStatus
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discussions for user {UserId}", userId);
                return StatusCode(500, new { message = "Error fetching discussions", error = ex.Message });
            }

            return Ok(discussions);
        }

        [HttpGet("discussions/{currentUserId}/{selectedUserId}")]
        public async Task<ActionResult<List<DiscussionResponse>>> GetUserDiscussions(int currentUserId, int selectedUserId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string roleQuery = "SELECT YetkiTuru FROM KullaniciBilgileri WHERE KullaniciID = @currentUserId";
                using var roleCommand = new MySqlCommand(roleQuery, connection);
                roleCommand.Parameters.AddWithValue("@currentUserId", currentUserId);
                var roleResult = await roleCommand.ExecuteScalarAsync();
                string userRole = roleResult?.ToString() ?? "";

                if (userRole == "Yonetici")
                {
                    return await GetAdminUserDiscussions(currentUserId, selectedUserId);
                }

                var discussions = new List<DiscussionResponse>();

                string query = @"
                SELECT DISTINCT d.Id, d.Title, d.Description, d.Status, d.CreatedByUserId, d.CreatedAt,d.ClientId,
                    COALESCE(d.SenderId, cm.SenderId) as SenderId, 
                    COALESCE(d.ReceiverId, cm.ReceiverId) as ReceiverId, 
                    lt.Status as LastTaskStatus,
                    sender.KullaniciAdi as SenderName, 
                    receiver.KullaniciAdi as ReceiverName,
                    creator.KullaniciAdi as CreatorName
                FROM Discussions d
                LEFT JOIN ChatMessages cm ON d.Id = cm.DiscussionId
                LEFT JOIN KullaniciBilgileri sender ON COALESCE(d.SenderId, cm.SenderId) = sender.KullaniciID
                LEFT JOIN KullaniciBilgileri receiver ON COALESCE(d.ReceiverId, cm.ReceiverId) = receiver.KullaniciID
                LEFT JOIN KullaniciBilgileri creator ON d.CreatedByUserId = creator.KullaniciID
                LEFT JOIN DiscussionAssignedUsers dau ON d.Id = dau.DiscussionId
                LEFT JOIN DiscussionParticipants dp ON d.Id = dp.DiscussionId
                LEFT JOIN Clients c ON d.ClientId = c.Id
                LEFT JOIN (
                    SELECT cm1.DiscussionId, t1.Status
                    FROM ChatMessages cm1
                    INNER JOIN Tasks t1 ON cm1.TaskId = t1.Id
                    WHERE cm1.TaskId IS NOT NULL
                    AND cm1.Id = (
                        SELECT cm2.Id 
                        FROM ChatMessages cm2 
                        WHERE cm2.DiscussionId = cm1.DiscussionId 
                        AND cm2.TaskId IS NOT NULL
                        ORDER BY cm2.CreatedAt DESC 
                        LIMIT 1
                    )
                ) lt ON d.Id = lt.DiscussionId
                WHERE (
                    d.CreatedByUserId = @currentUserId 
                    OR COALESCE(d.SenderId, cm.SenderId) = @currentUserId 
                    OR COALESCE(d.ReceiverId, cm.ReceiverId) = @currentUserId
                    OR dp.UserId = @currentUserId
                    OR dau.AssignedByUserId = @currentUserId
                )
                AND (
                    d.CreatedByUserId = @selectedUserId 
                    OR COALESCE(d.SenderId, cm.SenderId) = @selectedUserId 
                    OR COALESCE(d.ReceiverId, cm.ReceiverId) = @selectedUserId
                    OR dp.UserId = @selectedUserId
                    OR dau.AssignedUserId = @selectedUserId
                )
                ORDER BY d.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@currentUserId", currentUserId);
                command.Parameters.AddWithValue("@selectedUserId", selectedUserId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    crmApi.Models.TaskStatus? lastTaskStatus = null;
                    if (reader["LastTaskStatus"] != DBNull.Value)
                    {
                        if (Enum.TryParse<crmApi.Models.TaskStatus>(reader["LastTaskStatus"].ToString(), out crmApi.Models.TaskStatus status))
                        {
                            lastTaskStatus = status;
                        }
                    }

                    byte discussionStatus = 0;
                    if (reader["Status"] != DBNull.Value)
                    {
                        discussionStatus = Convert.ToByte(reader["Status"]);
                    }

                    discussions.Add(new DiscussionResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        Status = discussionStatus,
                        CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        SenderId = reader["SenderId"] != DBNull.Value ? Convert.ToInt32(reader["SenderId"]) : 0,
                        ReceiverId = reader["ReceiverId"] != DBNull.Value ? Convert.ToInt32(reader["ReceiverId"]) : 0,
                        SenderName = reader["SenderName"]?.ToString() ?? "",
                        ReceiverName = reader["ReceiverName"]?.ToString() ?? "",
                        CreatorName = reader["CreatorName"]?.ToString() ?? "",
                        ClientId = reader["ClientId"] != DBNull.Value ? Convert.ToInt32(reader["ClientId"]) : (int?)null,
                        LastTaskStatus = lastTaskStatus
                    });
                }

                return Ok(discussions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discussions for user {CurrentUserId} and selected user {SelectedUserId}", currentUserId, selectedUserId);
                return StatusCode(500, new { message = "Error fetching discussions", error = ex.Message });
            }
        }

        [HttpGet("discussions/admin/{currentUserId}/{selectedUserId}")]
        public async Task<ActionResult<List<DiscussionResponse>>> GetAdminUserDiscussions(int currentUserId, int selectedUserId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string roleQuery = "SELECT YetkiTuru FROM KullaniciBilgileri WHERE KullaniciID = @currentUserId";
                using var roleCommand = new MySqlCommand(roleQuery, connection);
                roleCommand.Parameters.AddWithValue("@currentUserId", currentUserId);
                var roleResult = await roleCommand.ExecuteScalarAsync();
                string userRole = roleResult?.ToString() ?? "";

                if (userRole != "Yonetici")
                {
                    return Forbid("Only administrators can access this endpoint");
                }

                var discussions = new List<DiscussionResponse>();

                string query = @"
                    SELECT DISTINCT d.Id, d.Title, d.Description, d.Status, d.CreatedByUserId, d.CreatedAt,d.ClientId,
                    COALESCE(d.SenderId, cm.SenderId) as SenderId, 
                    COALESCE(d.ReceiverId, cm.ReceiverId) as ReceiverId, 
                    lt.Status as LastTaskStatus,
                    sender.KullaniciAdi as SenderName, 
                    receiver.KullaniciAdi as ReceiverName,
                    creator.KullaniciAdi as CreatorName
                    FROM Discussions d
                    LEFT JOIN ChatMessages cm ON d.Id = cm.DiscussionId
                    LEFT JOIN KullaniciBilgileri sender ON COALESCE(d.SenderId, cm.SenderId) = sender.KullaniciID
                    LEFT JOIN KullaniciBilgileri receiver ON COALESCE(d.ReceiverId, cm.ReceiverId) = receiver.KullaniciID
                    LEFT JOIN KullaniciBilgileri creator ON d.CreatedByUserId = creator.KullaniciID
                    LEFT JOIN DiscussionAssignedUsers dau ON d.Id = dau.DiscussionId
                    LEFT JOIN DiscussionParticipants dp ON d.Id = dp.DiscussionId
                    LEFT JOIN Clients c ON d.ClientId = c.Id
                    LEFT JOIN (
                        SELECT cm1.DiscussionId, t1.Status
                        FROM ChatMessages cm1
                        INNER JOIN Tasks t1 ON cm1.TaskId = t1.Id
                        WHERE cm1.TaskId IS NOT NULL
                        AND cm1.Id = (
                            SELECT cm2.Id 
                            FROM ChatMessages cm2 
                            WHERE cm2.DiscussionId = cm1.DiscussionId 
                            AND cm2.TaskId IS NOT NULL
                            ORDER BY cm2.CreatedAt DESC 
                            LIMIT 1
                        )
                    ) lt ON d.Id = lt.DiscussionId
                    WHERE d.CreatedByUserId = @selectedUserId 
                    OR COALESCE(d.SenderId, cm.SenderId) = @selectedUserId 
                    OR COALESCE(d.ReceiverId, cm.ReceiverId) = @selectedUserId
                    OR dau.AssignedUserId = @selectedUserId
                    OR dp.UserId = @selectedUserId
                    ORDER BY d.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@selectedUserId", selectedUserId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    crmApi.Models.TaskStatus? lastTaskStatus = null;
                    if (reader["LastTaskStatus"] != DBNull.Value)
                    {
                        if (Enum.TryParse<crmApi.Models.TaskStatus>(reader["LastTaskStatus"].ToString(), out crmApi.Models.TaskStatus status))
                        {
                            lastTaskStatus = status;
                        }
                    }

                    byte discussionStatus = 0;
                    if (reader["Status"] != DBNull.Value)
                    {
                        discussionStatus = Convert.ToByte(reader["Status"]);
                    }

                    discussions.Add(new DiscussionResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        Status = discussionStatus,
                        CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        SenderId = reader["SenderId"] != DBNull.Value ? Convert.ToInt32(reader["SenderId"]) : 0,
                        ReceiverId = reader["ReceiverId"] != DBNull.Value ? Convert.ToInt32(reader["ReceiverId"]) : 0,
                        SenderName = reader["SenderName"]?.ToString() ?? "",
                        ReceiverName = reader["ReceiverName"]?.ToString() ?? "",
                        CreatorName = reader["CreatorName"]?.ToString() ?? "",
                        ClientId = reader["ClientId"] != DBNull.Value ? Convert.ToInt32(reader["ClientId"]) : (int?)null,
                        LastTaskStatus = lastTaskStatus
                    });
                }

                return Ok(discussions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discussions for selected user {SelectedUserId} by admin {CurrentUserId}", selectedUserId, currentUserId);
                return StatusCode(500, new { message = "Error fetching discussions", error = ex.Message });
            }
        }

        [HttpGet("discussions/all")]
        public async Task<ActionResult<List<DiscussionResponse>>> GetAllDiscussions()
        {
            var discussions = new List<DiscussionResponse>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                SELECT DISTINCT d.Id, d.Title, d.Description, d.Status, d.CreatedByUserId, d.CreatedAt,d.ClientId,
                COALESCE(d.SenderId, cm.SenderId) as SenderId, 
                COALESCE(d.ReceiverId, cm.ReceiverId) as ReceiverId, 
                lt.Status as LastTaskStatus,
                sender.KullaniciAdi as SenderName, 
                receiver.KullaniciAdi as ReceiverName,
                creator.KullaniciAdi as CreatorName
                FROM Discussions d
                LEFT JOIN ChatMessages cm ON d.Id = cm.DiscussionId
                LEFT JOIN KullaniciBilgileri sender ON COALESCE(d.SenderId, cm.SenderId) = sender.KullaniciID
                LEFT JOIN KullaniciBilgileri receiver ON COALESCE(d.ReceiverId, cm.ReceiverId) = receiver.KullaniciID
                LEFT JOIN KullaniciBilgileri creator ON d.CreatedByUserId = creator.KullaniciID
                LEFT JOIN Clients c ON d.ClientId = c.Id
                LEFT JOIN (
                    SELECT cm1.DiscussionId, t1.Status
                    FROM ChatMessages cm1
                    INNER JOIN Tasks t1 ON cm1.TaskId = t1.Id
                    WHERE cm1.TaskId IS NOT NULL
                    AND cm1.Id = (
                        SELECT cm2.Id 
                        FROM ChatMessages cm2 
                        WHERE cm2.DiscussionId = cm1.DiscussionId 
                        AND cm2.TaskId IS NOT NULL
                        ORDER BY cm2.CreatedAt DESC 
                        LIMIT 1
                    )
                ) lt ON d.Id = lt.DiscussionId
                ORDER BY d.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    crmApi.Models.TaskStatus? lastTaskStatus = null;
                    if (reader["LastTaskStatus"] != DBNull.Value)
                    {
                        if (Enum.TryParse<crmApi.Models.TaskStatus>(reader["LastTaskStatus"].ToString(), out crmApi.Models.TaskStatus status))
                        {
                            lastTaskStatus = status;
                        }
                    }

                    byte discussionStatus = 0;
                    if (reader["Status"] != DBNull.Value)
                    {
                        discussionStatus = Convert.ToByte(reader["Status"]);
                    }

                    discussions.Add(new DiscussionResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        Status = discussionStatus,
                        CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        SenderId = reader["SenderId"] != DBNull.Value ? Convert.ToInt32(reader["SenderId"]) : 0,
                        ReceiverId = reader["ReceiverId"] != DBNull.Value ? Convert.ToInt32(reader["ReceiverId"]) : 0,
                        SenderName = reader["SenderName"]?.ToString() ?? "",
                        ReceiverName = reader["ReceiverName"]?.ToString() ?? "",
                        CreatorName = reader["CreatorName"]?.ToString() ?? "",
                        ClientId = reader["ClientId"] != DBNull.Value ? Convert.ToInt32(reader["ClientId"]) : (int?)null,
                        LastTaskStatus = lastTaskStatus
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all discussions");
                return StatusCode(500, new { message = "Error fetching all discussions", error = ex.Message });
            }

            return Ok(discussions);
        }

        // ✅ Get messages for a discussion
        [HttpGet("discussions/{discussionId}/messages")]
        public async Task<ActionResult<object>> GetMessages(int discussionId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string assignmentQuery = @"
                    SELECT AssignedAt FROM DiscussionAssignedUsers 
                    WHERE DiscussionId = @discussionId AND AssignedUserId = @userId";

                DateTime? userAssignedAt = null;
                using var assignmentCommand = new MySqlCommand(assignmentQuery, connection);
                assignmentCommand.Parameters.AddWithValue("@discussionId", discussionId);
                assignmentCommand.Parameters.AddWithValue("@userId", userId);

                var assignmentResult = await assignmentCommand.ExecuteScalarAsync();
                if (assignmentResult != null)
                {
                    userAssignedAt = Convert.ToDateTime(assignmentResult);
                }

                string permissionQuery = @"
                        SELECT d.CreatedByUserId, kb.YetkiTuru 
                        FROM Discussions d 
                        LEFT JOIN KullaniciBilgileri kb ON kb.KullaniciID = @userId 
                        WHERE d.Id = @discussionId";

                using var permissionCommand = new MySqlCommand(permissionQuery, connection);
                permissionCommand.Parameters.AddWithValue("@discussionId", discussionId);
                permissionCommand.Parameters.AddWithValue("@userId", userId);

                bool isCreatorOrAdmin = false;
                using var permissionReader = await permissionCommand.ExecuteReaderAsync();
                if (await permissionReader.ReadAsync())
                {
                    var creatorId = Convert.ToInt32(permissionReader["CreatedByUserId"]);
                    var userRole = permissionReader["YetkiTuru"]?.ToString() ?? "";
                    isCreatorOrAdmin = creatorId == userId || userRole == "Yonetici";
                }
                permissionReader.Close();

                string query = @"
                    SELECT 
                        m.Id, m.DiscussionId, m.SenderId, m.ReceiverId, m.Content, 
                        m.MessageType, m.IsEdited, m.EditedAt, m.CreatedAt, m.FileReference, m.Duration,
                        m.IsSeen, m.SeenAt,
                        m.TaskId, t.Title AS TaskTitle, t.Description AS TaskDescription, t.Status AS TaskStatus,
                        t.Priority AS TaskPriority, t.DueDate, t.EstimatedTime,
                        d.FileName, d.OriginalFileName, d.MimeType, d.FileSize, d.IDriveUrl, d.BucketName, d.FileKey
                    FROM ChatMessages m
                    LEFT JOIN MessageDocuments d ON m.Id = d.MessageId
                    LEFT JOIN Tasks t ON m.TaskId = t.Id
                    WHERE m.DiscussionId = @discussionId";

                if (userAssignedAt.HasValue && !isCreatorOrAdmin)
                {
                    query += " AND m.CreatedAt >= @assignedAt";
                }

                query += " ORDER BY m.CreatedAt ASC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);
                if (userAssignedAt.HasValue && !isCreatorOrAdmin)
                {
                    command.Parameters.AddWithValue("@assignedAt", userAssignedAt.Value);
                }

                var messages = new List<MessageResponse>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var fileReference = reader.IsDBNull(reader.GetOrdinal("FileReference")) ? null : reader["FileReference"].ToString();
                    var duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Duration"));
                    var taskId = reader.IsDBNull(reader.GetOrdinal("TaskId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TaskId"));

                    var message = new MessageResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        DiscussionId = Convert.ToInt32(reader["DiscussionId"]),
                        SenderId = Convert.ToInt32(reader["SenderId"]),
                        ReceiverId = reader.IsDBNull(reader.GetOrdinal("ReceiverId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                        Content = reader["Content"].ToString(),
                        MessageType = Convert.ToByte(reader["MessageType"]),
                        IsEdited = Convert.ToBoolean(reader["IsEdited"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        EditedAt = reader.IsDBNull(reader.GetOrdinal("EditedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("EditedAt")),
                        IsSeen = Convert.ToBoolean(reader["IsSeen"]),
                        SeenAt = reader.IsDBNull(reader.GetOrdinal("SeenAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SeenAt")),
                        FileReference = fileReference,
                        Duration = duration,
                        FileName = reader.IsDBNull(reader.GetOrdinal("OriginalFileName")) ? null : reader["OriginalFileName"].ToString(),
                        MimeType = reader.IsDBNull(reader.GetOrdinal("MimeType")) ? null : reader["MimeType"].ToString(),
                        FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize")) ? 0 : Convert.ToInt64(reader["FileSize"]),
                        TaskId = taskId,
                        TaskTitle = reader.IsDBNull(reader.GetOrdinal("TaskTitle")) ? null : reader["TaskTitle"].ToString(),
                        TaskDescription = reader.IsDBNull(reader.GetOrdinal("TaskDescription")) ? null : reader["TaskDescription"].ToString(),
                        TaskStatus = reader.IsDBNull(reader.GetOrdinal("TaskStatus")) ? null : Enum.Parse<crmApi.Models.TaskStatus>(reader["TaskStatus"].ToString()),
                        TaskPriority = reader.IsDBNull(reader.GetOrdinal("TaskPriority")) ? null : Enum.Parse<TaskPriority>(reader["TaskPriority"].ToString()),
                        DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                        EstimatedTime = reader.IsDBNull(reader.GetOrdinal("EstimatedTime")) ? null : reader["EstimatedTime"].ToString(),
                        AssignedUserIds = new List<int>(),
                        IDriveUrl = reader["IDriveUrl"]?.ToString(),
                        BucketName = reader["BucketName"]?.ToString(),
                        FileKey = reader["FileKey"]?.ToString()
                    };

                    messages.Add(message);
                }

                reader.Close();

                foreach (var message in messages.Where(m => m.TaskId.HasValue))
                {
                    string assignQuery = @"
                SELECT UserId FROM TaskAssignments WHERE TaskId = @taskId";
                    using var assignCommand = new MySqlCommand(assignQuery, connection);
                    assignCommand.Parameters.AddWithValue("@taskId", message.TaskId);
                    using var assignReader = await assignCommand.ExecuteReaderAsync();
                    while (await assignReader.ReadAsync())
                    {
                        message.AssignedUserIds.Add(assignReader.GetInt32(assignReader.GetOrdinal("UserId")));
                    }
                }

                int unreadCount = messages.Count(m => m.ReceiverId == userId && m.IsSeen == false);

                int unseenCount = messages.Count(m => m.SenderId == userId && !(m.IsSeen ?? false));

                var voiceMessages = messages.Where(m => m.MessageType == (byte)MessageType.Voice).ToList();

                return Ok(new
                {
                    messages = messages,
                    unreadCount = unreadCount,
                    unseenCount = unseenCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages");
                return StatusCode(500, new { message = "Error fetching messages", error = ex.Message });
            }
        }

        [HttpPost("discussions")]
        public async Task<ActionResult<DiscussionResponse>> CreateDiscussion([FromBody] CreateDiscussionRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO Discussions (Title, Description, Status, CreatedByUserId, SenderId, ReceiverId,ClientId,createdAt)
                 VALUES (@title, @description, @status, @createdByUserId, @senderId, @receiverId ,@ClientId, @createdAt);
                 SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@title", request.Title);
                command.Parameters.AddWithValue("@description", request.Description ?? "");
                command.Parameters.AddWithValue("@status", request.Status);
                command.Parameters.AddWithValue("@createdByUserId", request.CreatedByUserId);
                command.Parameters.AddWithValue("@senderId", request.SenderId);
                command.Parameters.AddWithValue("@receiverId", request.ReceiverId);
                command.Parameters.AddWithValue("@ClientId", request.ClientId);
                command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

                _logger.LogInformation($"Creating discussion with Title: {request.Title}, Status: {request.Status}, SenderId: {request.SenderId}, ReceiverId: {request.ReceiverId}");

                var discussionId = Convert.ToInt32(await command.ExecuteScalarAsync());

                var participantUserIds = new List<int> { request.SenderId, request.ReceiverId };
                foreach (var userId in participantUserIds)
                {
                    string participantQuery = @"INSERT INTO DiscussionParticipants (DiscussionId, UserId, Role, JoinedAt, JoinedByUserId)
                                VALUES (@discussionId, @userId, 0, @joinedAt, @createdByUserId)";
                    using var participantCommand = new MySqlCommand(participantQuery, connection);
                    participantCommand.Parameters.AddWithValue("@discussionId", discussionId);
                    participantCommand.Parameters.AddWithValue("@userId", userId);
                    participantCommand.Parameters.AddWithValue("@joinedAt", DateTime.UtcNow);
                    participantCommand.Parameters.AddWithValue("@createdByUserId", request.CreatedByUserId);
                    await participantCommand.ExecuteNonQueryAsync();
                }

                return Ok(new DiscussionResponse
                {
                    Id = discussionId,
                    Title = request.Title,
                    Description = request.Description ?? "",
                    Status = request.Status,
                    CreatedByUserId = request.CreatedByUserId,
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    ClientId = request.ClientId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discussion");
                return StatusCode(500, new { message = "Error creating discussion", error = ex.Message });
            }
        }

        [HttpPut("discussions/{discussionId}/status")]
        public async Task<IActionResult> UpdateDiscussionStatus(int discussionId, [FromBody] UpdateDiscussionStatusRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"UPDATE Discussions 
                        SET Status = @status, UpdatedByUserId = @updatedByUserId, UpdatedAt = @updatedAt
                        WHERE Id = @discussionId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@status", request.Status);
                command.Parameters.AddWithValue("@updatedByUserId", request.UpdatedByUserId);
                command.Parameters.AddWithValue("@updatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    return NotFound(new { message = "Discussion not found" });
                }

                _logger.LogInformation($"Updated discussion {discussionId} status to {request.Status}");

                return Ok(new { message = "Discussion status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discussion status");
                return StatusCode(500, new { message = "Error updating discussion status", error = ex.Message });
            }
        }

        [HttpPost("discussions/assign")]
        public async Task<ActionResult> AssignUsersToDiscussion([FromBody] AssignDiscussionRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string discussionExistsQuery = "SELECT COUNT(*) FROM Discussions WHERE Id = @discussionId";
                using var existsCommand = new MySqlCommand(discussionExistsQuery, connection);
                existsCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                var discussionExists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync()) > 0;

                if (!discussionExists)
                {
                    return NotFound(new { message = "Discussion not found" });
                }

                string permissionQuery = @"
            SELECT d.CreatedByUserId, kb.YetkiTuru 
            FROM Discussions d 
            LEFT JOIN KullaniciBilgileri kb ON kb.KullaniciID = @assignedByUserId 
            WHERE d.Id = @discussionId";

                using var permissionCommand = new MySqlCommand(permissionQuery, connection);
                permissionCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                permissionCommand.Parameters.AddWithValue("@assignedByUserId", request.AssignedByUserId);

                using var permissionReader = await permissionCommand.ExecuteReaderAsync();
                bool hasPermission = false;

                if (await permissionReader.ReadAsync())
                {
                    var creatorId = Convert.ToInt32(permissionReader["CreatedByUserId"]);
                    var userRole = permissionReader["YetkiTuru"]?.ToString() ?? "";
                    hasPermission = creatorId == request.AssignedByUserId || userRole == "Yonetici";
                }
                permissionReader.Close();

                if (!hasPermission)
                {
                    return Forbid("You don't have permission to assign users to this discussion");
                }

                var assignedUsers = new List<int>();
                var alreadyAssigned = new List<int>();

                foreach (var userId in request.UserIds)
                {
                    string checkAssignedQuery = @"
                SELECT COUNT(*) FROM DiscussionAssignedUsers 
                WHERE DiscussionId = @discussionId AND AssignedUserId = @userId";

                    using var checkAssignedCommand = new MySqlCommand(checkAssignedQuery, connection);
                    checkAssignedCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    checkAssignedCommand.Parameters.AddWithValue("@userId", userId);

                    var isAlreadyAssigned = Convert.ToInt32(await checkAssignedCommand.ExecuteScalarAsync()) > 0;

                    if (isAlreadyAssigned)
                    {
                        alreadyAssigned.Add(userId);
                        continue;
                    }

                    string checkParticipantQuery = @"
                SELECT COUNT(*) FROM DiscussionParticipants 
                WHERE DiscussionId = @discussionId AND UserId = @userId";

                    using var checkCommand = new MySqlCommand(checkParticipantQuery, connection);
                    checkCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    checkCommand.Parameters.AddWithValue("@userId", userId);

                    var isAlreadyParticipant = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                    if (!isAlreadyParticipant)
                    {
                        string insertParticipantQuery = @"
                    INSERT INTO DiscussionParticipants (DiscussionId, UserId, Role, JoinedAt, JoinedByUserId)
                    VALUES (@discussionId, @userId, 0, @joinedAt, @assignedByUserId)";

                        using var insertCommand = new MySqlCommand(insertParticipantQuery, connection);
                        insertCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                        insertCommand.Parameters.AddWithValue("@userId", userId);
                        insertCommand.Parameters.AddWithValue("@joinedAt", DateTime.Now);
                        insertCommand.Parameters.AddWithValue("@assignedByUserId", request.AssignedByUserId);

                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    string systemMessageQuery = @"
                INSERT INTO ChatMessages (DiscussionId, SenderId, Content, MessageType, CreatedAt)
                VALUES (@discussionId, @assignedByUserId, @content, @messageType, @createdAt)";

                    using var messageCommand = new MySqlCommand(systemMessageQuery, connection);
                    messageCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    messageCommand.Parameters.AddWithValue("@assignedByUserId", request.AssignedByUserId);
                    messageCommand.Parameters.AddWithValue("@content", $"User has been assigned to this discussion");
                    messageCommand.Parameters.AddWithValue("@messageType", 1);
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                    await messageCommand.ExecuteNonQueryAsync();

                    string assignedUsersQuery = @"
                INSERT INTO DiscussionAssignedUsers (DiscussionId, AssignedUserId, AssignedByUserId, AssignedAt)
                VALUES (@discussionId, @assignedUserId, @assignedByUserId, @assignedAt)";

                    using var assignedCommand = new MySqlCommand(assignedUsersQuery, connection);
                    assignedCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    assignedCommand.Parameters.AddWithValue("@assignedUserId", userId);
                    assignedCommand.Parameters.AddWithValue("@assignedByUserId", request.AssignedByUserId);
                    assignedCommand.Parameters.AddWithValue("@assignedAt", DateTime.Now);

                    await assignedCommand.ExecuteNonQueryAsync();
                    assignedUsers.Add(userId);
                }

                var result = new
                {
                    message = "Assignment completed",
                    assignedUsers = assignedUsers,
                    alreadyAssigned = alreadyAssigned,
                    totalAssigned = assignedUsers.Count,
                    totalSkipped = alreadyAssigned.Count
                };

                _logger.LogInformation("Discussion {DiscussionId} assigned to {UserCount} users by user {AssignedByUserId}",
                    request.DiscussionId, assignedUsers.Count, request.AssignedByUserId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning users to discussion {DiscussionId}", request.DiscussionId);
                return StatusCode(500, new { message = "Error assigning users to discussion", error = ex.Message });
            }
        }

        [HttpGet("discussions/{discussionId}/assigned-users")]
        public async Task<ActionResult<List<object>>> GetDiscussionAssignedUsers(int discussionId)
        {
            var assignedUsers = new List<object>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT dau.AssignedUserId, dau.AssignedByUserId, dau.AssignedAt,
                        assigned.KullaniciAdi as AssignedUserName,
                        assigner.KullaniciAdi as AssignedByUserName
                    FROM DiscussionAssignedUsers dau
                    LEFT JOIN KullaniciBilgileri assigned ON dau.AssignedUserId = assigned.KullaniciID
                    LEFT JOIN KullaniciBilgileri assigner ON dau.AssignedByUserId = assigner.KullaniciID
                    WHERE dau.DiscussionId = @discussionId
                    ORDER BY dau.AssignedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    assignedUsers.Add(new
                    {
                        AssignedUserId = Convert.ToInt32(reader["AssignedUserId"]),
                        AssignedUserName = reader["AssignedUserName"]?.ToString() ?? "",
                        AssignedByUserId = Convert.ToInt32(reader["AssignedByUserId"]),
                        AssignedByUserName = reader["AssignedByUserName"]?.ToString() ?? "",
                        AssignedAt = Convert.ToDateTime(reader["AssignedAt"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching assigned users for discussion {DiscussionId}", discussionId);
                return StatusCode(500, new { message = "Error fetching assigned users", error = ex.Message });
            }

            return Ok(assignedUsers);
        }

        // ✅ Send a message
        [HttpPost("messages")]
        public async Task<ActionResult<MessageResponse>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO ChatMessages (DiscussionId, SenderId, ReceiverId, Content, MessageType, CreatedAt ,IsSeen, SeenAt)
                                 VALUES (@discussionId, @senderId, @receiverId, @content, @messageType, @createdAt, @isSeen, @seenAt);
                                 SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                command.Parameters.AddWithValue("@senderId", request.SenderId);
                command.Parameters.AddWithValue("@receiverId", request.ReceiverId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@content", request.Content);
                command.Parameters.AddWithValue("@messageType", request.MessageType);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@isSeen", false);
                command.Parameters.AddWithValue("@seenAt", DBNull.Value);

                var messageId = Convert.ToInt32(await command.ExecuteScalarAsync());

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = request.DiscussionId,
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    MessageType = request.MessageType,
                    IsEdited = false,
                    CreatedAt = DateTime.Now,
                    IsSeen = false,
                    SeenAt = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        // ✅ Edit a message
        [HttpPut("messages/{messageId}")]
        public async Task<ActionResult<MessageResponse>> EditMessage(int messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string updateQuery = @"UPDATE ChatMessages 
                                       SET Content = @content, IsEdited = 1, EditedAt = @editedAt
                                       WHERE Id = @messageId AND SenderId = @userId";

                using var command = new MySqlCommand(updateQuery, connection);
                command.Parameters.AddWithValue("@content", request.Content);
                command.Parameters.AddWithValue("@editedAt", DateTime.Now);
                command.Parameters.AddWithValue("@messageId", messageId);
                command.Parameters.AddWithValue("@userId", request.UserId);

                var affected = await command.ExecuteNonQueryAsync();
                if (affected == 0)
                    return Forbid("Only the sender can edit the message");

                return Ok(new { messageId, request.Content, IsEdited = true, EditedAt = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message");
                return StatusCode(500, new { message = "Error editing message", error = ex.Message });
            }
        }

        // ✅ Delete a message
        [HttpDelete("messages/{messageId}")]
        public async Task<ActionResult> DeleteMessage(int messageId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"DELETE FROM ChatMessages WHERE Id = @messageId AND SenderId = @userId";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@messageId", messageId);
                command.Parameters.AddWithValue("@userId", userId);

                var affected = await command.ExecuteNonQueryAsync();
                if (affected == 0)
                    return Forbid("Only the sender can delete the message");

                return Ok(new { message = "Message deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return StatusCode(500, new { message = "Error deleting message", error = ex.Message });
            }
        }

        private string ExtractBucketFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return pathSegments.Length > 0 ? pathSegments[0] : "";
            }
            catch
            {
                return "";
            }
        }

        [HttpPost("messages/send-with-file")]
        public async Task<IActionResult> SendMessageWithFile([FromForm] SendMessageWithFileRequest request, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                _logger.LogInformation($"Processing file upload: {file.FileName}, Size: {file.Length}");

                string cloudinaryUrl;
                try
                {
                    cloudinaryUrl = await UploadToCloudinary(file, request.FileName ?? file.FileName);
                    _logger.LogInformation($"File uploaded to Cloudinary: {cloudinaryUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cloudinary upload failed");
                    return StatusCode(500, new { message = $"File upload failed: {ex.Message}" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string messageQuery = @"
                INSERT INTO ChatMessages (
                    DiscussionId, 
                    SenderId, 
                    ReceiverId, 
                    Content, 
                    MessageType, 
                    HasFile, 
                    FileName, 
                    MimeType, 
                    FileSize, 
                    FileReference,
                    CreatedAt
                ) VALUES (
                    @discussionId, 
                    @senderId, 
                    @receiverId, 
                    @content, 
                    @messageType, 
                    @hasFile, 
                    @fileName, 
                    @mimeType, 
                    @fileSize, 
                    @fileReference,
                    @createdAt
                );
                SELECT LAST_INSERT_ID();";

                    using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                    messageCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    messageCommand.Parameters.AddWithValue("@senderId", request.SenderId);
                    messageCommand.Parameters.AddWithValue("@receiverId", request.ReceiverId ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@content", request.Content ?? $"File: {file.FileName}");
                    messageCommand.Parameters.AddWithValue("@messageType", request.MessageType);
                    messageCommand.Parameters.AddWithValue("@hasFile", 1);
                    messageCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    messageCommand.Parameters.AddWithValue("@mimeType", file.ContentType ?? "application/octet-stream");
                    messageCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    messageCommand.Parameters.AddWithValue("@fileReference", request.FileReference ?? $"ref_{DateTimeOffset.Now.ToUnixTimeSeconds()}_{file.FileName}");
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                    var messageIdResult = await messageCommand.ExecuteScalarAsync();
                    var messageId = Convert.ToInt32(messageIdResult);
                    _logger.LogInformation($"Message inserted with ID: {messageId}");

                    string docQuery = @"
                INSERT INTO MessageDocuments (
                    MessageId, 
                    FileName, 
                    OriginalFileName, 
                    FileSize, 
                    MimeType, 
                    FilePath, 
                    IDriveUrl, 
                    BucketName, 
                    FileKey
                ) VALUES (
                    @messageId, 
                    @fileName, 
                    @originalFileName, 
                    @fileSize, 
                    @mimeType, 
                    @filePath, 
                    @idriveUrl, 
                    @bucketName, 
                    @fileKey
                )";

                    using var docCommand = new MySqlCommand(docQuery, connection, transaction);
                    docCommand.Parameters.AddWithValue("@messageId", messageId);
                    docCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    docCommand.Parameters.AddWithValue("@originalFileName", file.FileName);
                    docCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    docCommand.Parameters.AddWithValue("@mimeType", file.ContentType ?? "application/octet-stream");
                    docCommand.Parameters.AddWithValue("@filePath", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@idriveUrl", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@bucketName", request.BucketName ?? "chat-files");
                    docCommand.Parameters.AddWithValue("@fileKey", request.FileKey ?? $"{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}");

                    await docCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("File upload completed successfully");

                    return Ok(new
                    {
                        id = messageId,
                        discussionId = request.DiscussionId,
                        senderId = request.SenderId,
                        receiverId = request.ReceiverId,
                        content = request.Content ?? $"File: {file.FileName}",
                        messageType = request.MessageType,
                        createdAt = DateTime.Now,
                        fileName = file.FileName,
                        fileSize = file.Length,
                        mimeType = file.ContentType,
                        idriveUrl = cloudinaryUrl,
                        fileReference = request.FileReference,
                        bucketName = request.BucketName ?? "chat-files",
                        fileKey = request.FileKey ?? $"{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}",
                        hasFile = true
                    });
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database transaction failed");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageWithFile endpoint");
                return StatusCode(500, new
                {
                    message = "Error sending message with file",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("messages/send-with-voice")]
        public async Task<IActionResult> SendVoiceMessage([FromForm] SendVoiceMessageRequest request, IFormFile audioFile)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    return BadRequest(new { message = "No audio file provided" });
                }

                _logger.LogInformation($"Processing voice message upload: {audioFile.FileName}, Size: {audioFile.Length}, Duration: {request.Duration}s");

                string cloudinaryUrl;
                try
                {
                    cloudinaryUrl = await UploadAudioToCloudinary(audioFile, request.FileName ?? audioFile.FileName);
                    _logger.LogInformation($"Audio uploaded to Cloudinary: {cloudinaryUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cloudinary audio upload failed");
                    return StatusCode(500, new { message = $"Audio upload failed: {ex.Message}" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var fileReference = request.FileReference ?? $"voice_{DateTimeOffset.Now.ToUnixTimeSeconds()}_{audioFile.FileName}";
                    var fileKey = request.FileKey ?? $"voice/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{audioFile.FileName}";

                    string messageQuery = @"
                INSERT INTO ChatMessages (
                    DiscussionId, 
                    SenderId, 
                    ReceiverId, 
                    Content, 
                    MessageType, 
                    HasFile, 
                    FileName, 
                    MimeType, 
                    FileSize, 
                    FileReference,
                    Duration,
                    CreatedAt
                ) VALUES (
                    @discussionId, 
                    @senderId, 
                    @receiverId, 
                    @content, 
                    @messageType, 
                    @hasFile, 
                    @fileName, 
                    @mimeType, 
                    @fileSize, 
                    @fileReference,
                    @duration,
                    @createdAt
                );
                SELECT LAST_INSERT_ID();";

                    using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                    messageCommand.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                    messageCommand.Parameters.AddWithValue("@senderId", request.SenderId);
                    messageCommand.Parameters.AddWithValue("@receiverId", request.ReceiverId ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@content", request.Content ?? "Voice message");
                    messageCommand.Parameters.AddWithValue("@messageType", request.MessageType);
                    messageCommand.Parameters.AddWithValue("@hasFile", 1);
                    messageCommand.Parameters.AddWithValue("@fileName", audioFile.FileName ?? "voice_message.webm");
                    messageCommand.Parameters.AddWithValue("@mimeType", audioFile.ContentType ?? "audio/webm");
                    messageCommand.Parameters.AddWithValue("@fileSize", audioFile.Length);
                    messageCommand.Parameters.AddWithValue("@fileReference", fileReference);
                    messageCommand.Parameters.AddWithValue("@duration", request.Duration ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                    var messageIdResult = await messageCommand.ExecuteScalarAsync();
                    var messageId = Convert.ToInt32(messageIdResult);
                    _logger.LogInformation($"Voice message inserted with ID: {messageId}");

                    string docQuery = @"
                INSERT INTO MessageDocuments (
                    MessageId, 
                    FileName, 
                    OriginalFileName, 
                    FileSize, 
                    MimeType, 
                    FilePath, 
                    UploadedAt,
                    IDriveUrl, 
                    BucketName, 
                    FileKey
                ) VALUES (
                    @messageId, 
                    @fileName, 
                    @originalFileName, 
                    @fileSize, 
                    @mimeType, 
                    @filePath, 
                    @uploadedAt,
                    @idriveUrl, 
                    @bucketName, 
                    @fileKey
                )";

                    using var docCommand = new MySqlCommand(docQuery, connection, transaction);
                    docCommand.Parameters.AddWithValue("@messageId", messageId);
                    docCommand.Parameters.AddWithValue("@fileName", audioFile.FileName ?? "voice_message.webm");
                    docCommand.Parameters.AddWithValue("@originalFileName", audioFile.FileName ?? "voice_message.webm");
                    docCommand.Parameters.AddWithValue("@fileSize", audioFile.Length);
                    docCommand.Parameters.AddWithValue("@mimeType", audioFile.ContentType ?? "audio/webm");
                    docCommand.Parameters.AddWithValue("@filePath", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);
                    docCommand.Parameters.AddWithValue("@idriveUrl", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@bucketName", request.BucketName ?? "voice-messages");
                    docCommand.Parameters.AddWithValue("@fileKey", fileKey);

                    await docCommand.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Voice message upload completed successfully");

                    return Ok(new
                    {
                        id = messageId,
                        discussionId = request.DiscussionId,
                        senderId = request.SenderId,
                        receiverId = request.ReceiverId,
                        content = request.Content ?? "Voice message",
                        messageType = request.MessageType,
                        createdAt = DateTime.Now,
                        fileName = audioFile.FileName,
                        originalFileName = audioFile.FileName,
                        fileSize = audioFile.Length,
                        mimeType = audioFile.ContentType,
                        idriveUrl = cloudinaryUrl,
                        fileReference = fileReference,
                        bucketName = request.BucketName ?? "voice-messages",
                        fileKey = fileKey,
                        duration = request.Duration,
                        hasFile = true,
                        filePath = cloudinaryUrl
                    });
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database transaction failed for voice message");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendVoiceMessage endpoint");
                return StatusCode(500, new
                {
                    message = "Error sending voice message",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("messages/{messageId}/documents")]
        public async Task<ActionResult<MessageResponse>> UploadDocument(int messageId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string insertQuery = @"
                    INSERT INTO MessageDocuments (MessageId, FileName, OriginalFileName, FileSize, MimeType, FilePath, UploadedAt)
                    VALUES (@messageId, @fileName, @originalFileName, @fileSize, @mimeType, @filePath, @uploadedAt);
                    SELECT LAST_INSERT_ID();";

                using var insertCmd = new MySqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("@messageId", messageId);
                insertCmd.Parameters.AddWithValue("@fileName", uniqueFileName);
                insertCmd.Parameters.AddWithValue("@originalFileName", file.FileName);
                insertCmd.Parameters.AddWithValue("@fileSize", file.Length);
                insertCmd.Parameters.AddWithValue("@mimeType", file.ContentType);
                insertCmd.Parameters.AddWithValue("@filePath", $"/uploads/{uniqueFileName}");
                insertCmd.Parameters.AddWithValue("@uploadedAt", DateTime.Now);

                var documentId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                string getMessageQuery = @"SELECT * FROM ChatMessages WHERE Id = @messageId";
                using var messageCmd = new MySqlCommand(getMessageQuery, connection);
                messageCmd.Parameters.AddWithValue("@messageId", messageId);
                using var reader = await messageCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return NotFound("Message not found");

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = Convert.ToInt32(reader["DiscussionId"]),
                    SenderId = Convert.ToInt32(reader["SenderId"]),
                    ReceiverId = reader.IsDBNull(reader.GetOrdinal("ReceiverId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                    Content = reader["Content"].ToString(),
                    MessageType = Convert.ToByte(reader["MessageType"]),
                    IsEdited = Convert.ToBoolean(reader["IsEdited"]),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                    DocumentId = documentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { message = "Error uploading document", error = ex.Message });
            }
        }

        [HttpGet("documents/{documentId}")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"SELECT FilePath, OriginalFileName, MimeType FROM MessageDocuments WHERE Id = @documentId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@documentId", documentId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return NotFound("Document not found");

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", reader["FilePath"].ToString()!.TrimStart('/'));
                var originalFileName = reader["OriginalFileName"].ToString()!;
                var mimeType = reader["MimeType"].ToString()!;

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on server");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, mimeType, originalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document");
                return StatusCode(500, new { message = "Error downloading document", error = ex.Message });
            }
        }

        [HttpGet("messages/{messageId}/file")]
        public async Task<IActionResult> GetMessageFile(int messageId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var document = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT IDriveUrl, FileName, FileSize, MimeType 
              FROM MessageDocuments 
              WHERE MessageId = @MessageId",
                    new { MessageId = messageId });

                if (document == null || string.IsNullOrEmpty(document.IDriveUrl))
                {
                    return NotFound("File not found or no URL available");
                }

                return Ok(new
                {
                    downloadUrl = document.IDriveUrl,
                    fileUrl = document.IDriveUrl,
                    idriveUrl = document.IDriveUrl,
                    fileName = document.FileName,
                    fileSize = document.FileSize,
                    mimeType = document.MimeType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting message file for MessageId: {messageId}, Error: {ex.Message}");
                return StatusCode(500, "Error retrieving file");
            }
        }

        [HttpGet("messages/{messageId}/voice")]
        public async Task<IActionResult> GetMessageVoice(int messageId)
        {
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var document = await connection.QueryFirstOrDefaultAsync<MessageDocument>(
                    "SELECT * FROM MessageDocuments WHERE MessageId = @MessageId",
                    new { MessageId = messageId });

                if (document == null)
                {
                    return NotFound("Voice message not found");
                }

                return Ok(new
                {
                    audioUrl = document.IDriveUrl,
                    fileUrl = document.IDriveUrl,
                    idriveUrl = document.IDriveUrl,
                    fileName = document.FileName,
                    fileSize = document.FileSize,
                    mimeType = document.MimeType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voice message");
                return StatusCode(500, "Error retrieving voice message");
            }
        }

        [HttpGet("files/access-url")]
        public async Task<IActionResult> GetFileAccessUrl([FromQuery] string bucketName, [FromQuery] string fileName)
        {
            try
            {
                var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                var serviceUrl = Environment.GetEnvironmentVariable("AWS_S3_SERVICE_URL");

                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(serviceUrl))
                {
                    return StatusCode(500, "Server configuration error");
                }

                using var client = new AmazonS3Client(
                    accessKey,
                    secretKey,
                    new AmazonS3Config
                    {
                        ServiceURL = serviceUrl,
                        ForcePathStyle = true
                    }
                );

                var presignedUrl = client.GetPreSignedURL(new Amazon.S3.Model.GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.Now.AddHours(24)
                });

                return Ok(new { accessUrl = presignedUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating file access URL");
                return StatusCode(500, new { message = "Error generating file access URL" });
            }
        }

        [HttpPost("messages/send-task-with-file")]
        public async Task<ActionResult<MessageResponse>> SendTaskWithFile([FromForm] CreateTaskMessageWithFileDto createTaskMessage, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                _logger.LogInformation($"Processing task with file upload: {file.FileName}, Size: {file.Length}");

                string cloudinaryUrl;
                try
                {
                    cloudinaryUrl = await UploadToCloudinary(file, file.FileName);
                    _logger.LogInformation($"Task file uploaded to Cloudinary: {cloudinaryUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cloudinary file upload failed for task");
                    return StatusCode(500, new { message = $"File upload failed: {ex.Message}" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var taskTitle = createTaskMessage.TaskTitle ?? createTaskMessage.Content ?? "File Task";

                    string taskQuery = @"
                INSERT INTO Tasks (Title, Description, Status, Priority, DueDate, EstimatedTime, SortOrder, CreatedByUserId, CreatedAt)
                VALUES (@Title, @Description, @Status, @Priority, @DueDate, @EstimatedTime, @SortOrder, @CreatedByUserId, @CreatedAt);
                SELECT LAST_INSERT_ID();";

                    using var taskCommand = new MySqlCommand(taskQuery, connection, transaction);
                    taskCommand.Parameters.AddWithValue("@Title", taskTitle);
                    taskCommand.Parameters.AddWithValue("@Description", createTaskMessage.TaskDescription ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@Status", createTaskMessage.TaskStatus);
                    taskCommand.Parameters.AddWithValue("@Priority", createTaskMessage.TaskPriority);
                    taskCommand.Parameters.AddWithValue("@DueDate", createTaskMessage.DueDate ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@EstimatedTime", createTaskMessage.EstimatedTime ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@SortOrder", 0);
                    taskCommand.Parameters.AddWithValue("@CreatedByUserId", createTaskMessage.SenderId);
                    taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task created with ID: {taskId}");

                    if (createTaskMessage.AssignedUserIds?.Any() == true)
                    {
                        string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                        var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                        assignQuery += string.Join(", ", values);

                        using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                        assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                        assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                        for (int i = 0; i < createTaskMessage.AssignedUserIds.Count; i++)
                        {
                            assignCommand.Parameters.AddWithValue($"@UserId{i}", createTaskMessage.AssignedUserIds[i]);
                        }
                        await assignCommand.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Assigned users to task ID: {taskId}");
                    }

                    if (createTaskMessage.ClientIds?.Any() == true)
                    {
                        string clientQuery = "INSERT INTO TaskClients (TaskId, ClientId) VALUES ";
                        var clientValues = createTaskMessage.ClientIds.Select((_, index) => $"(@TaskId, @ClientId{index})");
                        clientQuery += string.Join(", ", clientValues);

                        using var clientCommand = new MySqlCommand(clientQuery, connection, transaction);
                        clientCommand.Parameters.AddWithValue("@TaskId", taskId);
                        for (int i = 0; i < createTaskMessage.ClientIds.Count; i++)
                        {
                            clientCommand.Parameters.AddWithValue($"@ClientId{i}", createTaskMessage.ClientIds[i]);
                        }
                        await clientCommand.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Assigned clients to task ID: {taskId}");
                    }

                    if (createTaskMessage.ProjectIds?.Any() == true)
                    {
                        string projectQuery = "INSERT INTO TaskProjects (TaskId, ProjectId) VALUES ";
                        var projectValues = createTaskMessage.ProjectIds.Select((_, index) => $"(@TaskId, @ProjectId{index})");
                        projectQuery += string.Join(", ", projectValues);

                        using var projectCommand = new MySqlCommand(projectQuery, connection, transaction);
                        projectCommand.Parameters.AddWithValue("@TaskId", taskId);
                        for (int i = 0; i < createTaskMessage.ProjectIds.Count; i++)
                        {
                            projectCommand.Parameters.AddWithValue($"@ProjectId{i}", createTaskMessage.ProjectIds[i]);
                        }
                        await projectCommand.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Assigned projects to task ID: {taskId}");
                    }

                    var fileReference = $"task_file_{DateTimeOffset.Now.ToUnixTimeSeconds()}_{file.FileName}";
                    var fileKey = $"task-files/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}";

                    string messageQuery = @"
                INSERT INTO ChatMessages (
                    DiscussionId, 
                    SenderId, 
                    ReceiverId, 
                    Content, 
                    MessageType, 
                    TaskId, 
                    HasFile,
                    FileName,
                    MimeType,
                    FileSize,
                    FileReference,
                    CreatedAt
                ) VALUES (
                    @discussionId, 
                    @senderId, 
                    @receiverId, 
                    @content, 
                    @messageType, 
                    @taskId, 
                    @hasFile,
                    @fileName,
                    @mimeType,
                    @fileSize,
                    @fileReference,
                    @createdAt
                );
                SELECT LAST_INSERT_ID();";

                    using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                    messageCommand.Parameters.AddWithValue("@discussionId", createTaskMessage.DiscussionId);
                    messageCommand.Parameters.AddWithValue("@senderId", createTaskMessage.SenderId);
                    messageCommand.Parameters.AddWithValue("@receiverId", createTaskMessage.ReceiverId ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@content", createTaskMessage.Content);
                    messageCommand.Parameters.AddWithValue("@messageType", createTaskMessage.MessageType);
                    messageCommand.Parameters.AddWithValue("@taskId", taskId);
                    messageCommand.Parameters.AddWithValue("@hasFile", 1);
                    messageCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    messageCommand.Parameters.AddWithValue("@mimeType", file.ContentType ?? "application/octet-stream");
                    messageCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    messageCommand.Parameters.AddWithValue("@fileReference", fileReference);
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                    var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task message created with ID: {messageId}");

                    string docQuery = @"
                INSERT INTO MessageDocuments (
                    MessageId, 
                    FileName, 
                    OriginalFileName, 
                    FileSize, 
                    MimeType, 
                    FilePath, 
                    UploadedAt,
                    IDriveUrl, 
                    BucketName, 
                    FileKey
                ) VALUES (
                    @messageId, 
                    @fileName, 
                    @originalFileName, 
                    @fileSize, 
                    @mimeType, 
                    @filePath, 
                    @uploadedAt,
                    @idriveUrl, 
                    @bucketName, 
                    @fileKey
                );
                SELECT LAST_INSERT_ID();";

                    using var docCommand = new MySqlCommand(docQuery, connection, transaction);
                    docCommand.Parameters.AddWithValue("@messageId", messageId);
                    docCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    docCommand.Parameters.AddWithValue("@originalFileName", file.FileName);
                    docCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    docCommand.Parameters.AddWithValue("@mimeType", file.ContentType ?? "application/octet-stream");
                    docCommand.Parameters.AddWithValue("@filePath", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);
                    docCommand.Parameters.AddWithValue("@idriveUrl", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@bucketName", "task-files");
                    docCommand.Parameters.AddWithValue("@fileKey", fileKey);

                    var rowsAffected = await docCommand.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        throw new Exception("Failed to insert into MessageDocuments");
                    }
                    _logger.LogInformation($"MessageDocuments record created for message ID: {messageId}, IDriveUrl: {cloudinaryUrl}");

                    await transaction.CommitAsync();
                    _logger.LogInformation($"Task message with file completed successfully. MessageId: {messageId}, TaskId: {taskId}");

                    return Ok(new MessageResponse
                    {
                        Id = messageId,
                        DiscussionId = createTaskMessage.DiscussionId,
                        SenderId = createTaskMessage.SenderId,
                        ReceiverId = createTaskMessage.ReceiverId,
                        Content = createTaskMessage.Content,
                        MessageType = Convert.ToByte(createTaskMessage.MessageType),
                        TaskId = taskId,
                        TaskTitle = taskTitle,
                        TaskDescription = createTaskMessage.TaskDescription,
                        TaskStatus = Enum.TryParse<crmApi.Models.TaskStatus>(createTaskMessage.TaskStatus, out var status) ? status : (crmApi.Models.TaskStatus?)null,
                        TaskPriority = Enum.TryParse<crmApi.Models.TaskPriority>(createTaskMessage.TaskPriority, out var priority) ? priority : (crmApi.Models.TaskPriority?)null,
                        DueDate = createTaskMessage.DueDate,
                        EstimatedTime = createTaskMessage.EstimatedTime,
                        AssignedUserIds = createTaskMessage.AssignedUserIds,
                        ClientIds = createTaskMessage.ClientIds,
                        ProjectIds = createTaskMessage.ProjectIds,
                        FileReference = fileReference,
                        FileName = file.FileName,
                        MimeType = file.ContentType,
                        FileSize = file.Length,
                        CreatedAt = DateTime.Now,
                        HasFile = true,
                        FileUrl = cloudinaryUrl,
                        IDriveUrl = cloudinaryUrl
                    });
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, $"Database transaction failed for task with file. Message: {dbEx.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating task message with file: {ex.Message}");
                return StatusCode(500, new { message = "Error creating task message with file", error = ex.Message });
            }
        }


        [HttpPost("messages/send-task-with-voice")]
        public async Task<ActionResult<MessageResponse>> SendTaskWithVoice([FromForm] CreateTaskMessageWithVoiceDto createTaskMessage, IFormFile audioFile)
        {
            try
            {
                if (audioFile == null || audioFile.Length == 0)
                {
                    return BadRequest(new { message = "No audio file provided" });
                }

                _logger.LogInformation($"Processing task with voice upload: {audioFile.FileName}, Size: {audioFile.Length}");

                string cloudinaryUrl;
                try
                {
                    cloudinaryUrl = await UploadAudioToCloudinary(audioFile, audioFile.FileName);
                    _logger.LogInformation($"Task audio uploaded to Cloudinary: {cloudinaryUrl}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cloudinary audio upload failed for task");
                    return StatusCode(500, new { message = $"Audio upload failed: {ex.Message}" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    var taskTitle = createTaskMessage.TaskTitle ?? createTaskMessage.Content ?? "Voice Task";

                    string taskQuery = @"
                INSERT INTO Tasks (Title, Description, Status, Priority, DueDate, EstimatedTime, SortOrder, CreatedByUserId, CreatedAt)
                VALUES (@Title, @Description, @Status, @Priority, @DueDate, @EstimatedTime, @SortOrder, @CreatedByUserId, @CreatedAt);
                SELECT LAST_INSERT_ID();";

                    using var taskCommand = new MySqlCommand(taskQuery, connection, transaction);
                    taskCommand.Parameters.AddWithValue("@Title", taskTitle);
                    taskCommand.Parameters.AddWithValue("@Description", createTaskMessage.TaskDescription ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@Status", createTaskMessage.TaskStatus);
                    taskCommand.Parameters.AddWithValue("@Priority", createTaskMessage.TaskPriority);
                    taskCommand.Parameters.AddWithValue("@DueDate", createTaskMessage.DueDate ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@EstimatedTime", createTaskMessage.EstimatedTime ?? (object)DBNull.Value);
                    taskCommand.Parameters.AddWithValue("@SortOrder", 0);
                    taskCommand.Parameters.AddWithValue("@CreatedByUserId", createTaskMessage.SenderId);
                    taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task created with ID: {taskId}");

                    if (createTaskMessage.AssignedUserIds?.Any() == true)
                    {
                        string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                        var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                        assignQuery += string.Join(", ", values);

                        using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                        assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                        assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                        for (int i = 0; i < createTaskMessage.AssignedUserIds.Count; i++)
                        {
                            assignCommand.Parameters.AddWithValue($"@UserId{i}", createTaskMessage.AssignedUserIds[i]);
                        }
                        await assignCommand.ExecuteNonQueryAsync();
                    }

                    var fileReference = $"task_voice_{DateTimeOffset.Now.ToUnixTimeSeconds()}_{audioFile.FileName}";
                    var fileKey = $"task-voice/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{audioFile.FileName}";

                    string messageQuery = @"
                INSERT INTO ChatMessages (
                    DiscussionId, 
                    SenderId, 
                    ReceiverId, 
                    Content, 
                    MessageType, 
                    TaskId, 
                    HasFile,
                    FileName,
                    MimeType,
                    FileSize,
                    FileReference, 
                    Duration, 
                    CreatedAt
                ) VALUES (
                    @discussionId, 
                    @senderId, 
                    @receiverId, 
                    @content, 
                    @messageType, 
                    @taskId, 
                    @hasFile,
                    @fileName,
                    @mimeType,
                    @fileSize,
                    @fileReference, 
                    @duration, 
                    @createdAt
                );
                SELECT LAST_INSERT_ID();";

                    using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                    messageCommand.Parameters.AddWithValue("@discussionId", createTaskMessage.DiscussionId);
                    messageCommand.Parameters.AddWithValue("@senderId", createTaskMessage.SenderId);
                    messageCommand.Parameters.AddWithValue("@receiverId", createTaskMessage.ReceiverId ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@content", createTaskMessage.Content);
                    messageCommand.Parameters.AddWithValue("@messageType", createTaskMessage.MessageType);
                    messageCommand.Parameters.AddWithValue("@taskId", taskId);
                    messageCommand.Parameters.AddWithValue("@hasFile", 1);
                    messageCommand.Parameters.AddWithValue("@fileName", audioFile.FileName ?? "task_voice.webm");
                    messageCommand.Parameters.AddWithValue("@mimeType", audioFile.ContentType ?? "audio/webm");
                    messageCommand.Parameters.AddWithValue("@fileSize", audioFile.Length);
                    messageCommand.Parameters.AddWithValue("@fileReference", fileReference);
                    messageCommand.Parameters.AddWithValue("@duration", createTaskMessage.Duration ?? (object)DBNull.Value);
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                    var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task message created with ID: {messageId}");

                    string docQuery = @"
                INSERT INTO MessageDocuments (
                    MessageId, 
                    FileName, 
                    OriginalFileName, 
                    FileSize, 
                    MimeType, 
                    FilePath, 
                    UploadedAt,
                    IDriveUrl, 
                    BucketName, 
                    FileKey
                ) VALUES (
                    @messageId, 
                    @fileName, 
                    @originalFileName, 
                    @fileSize, 
                    @mimeType, 
                    @filePath, 
                    @uploadedAt,
                    @idriveUrl, 
                    @bucketName, 
                    @fileKey
                )";

                    using var docCommand = new MySqlCommand(docQuery, connection, transaction);
                    docCommand.Parameters.AddWithValue("@messageId", messageId);
                    docCommand.Parameters.AddWithValue("@fileName", audioFile.FileName ?? "task_voice.webm");
                    docCommand.Parameters.AddWithValue("@originalFileName", audioFile.FileName ?? "task_voice.webm");
                    docCommand.Parameters.AddWithValue("@fileSize", audioFile.Length);
                    docCommand.Parameters.AddWithValue("@mimeType", audioFile.ContentType ?? "audio/webm");
                    docCommand.Parameters.AddWithValue("@filePath", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);
                    docCommand.Parameters.AddWithValue("@idriveUrl", cloudinaryUrl);
                    docCommand.Parameters.AddWithValue("@bucketName", "task-voice-messages");
                    docCommand.Parameters.AddWithValue("@fileKey", fileKey);

                    await docCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation($"MessageDocuments record created for task voice message");

                    await transaction.CommitAsync();
                    _logger.LogInformation($"Task message with voice completed successfully. MessageId: {messageId}, TaskId: {taskId}");

                    return Ok(new MessageResponse
                    {
                        Id = messageId,
                        DiscussionId = createTaskMessage.DiscussionId,
                        SenderId = createTaskMessage.SenderId,
                        ReceiverId = createTaskMessage.ReceiverId,
                        Content = createTaskMessage.Content,
                        MessageType = Convert.ToByte(createTaskMessage.MessageType),
                        TaskId = taskId,
                        TaskTitle = taskTitle,
                        TaskDescription = createTaskMessage.TaskDescription,
                        TaskStatus = Enum.TryParse<crmApi.Models.TaskStatus>(createTaskMessage.TaskStatus, out var status) ? status : (crmApi.Models.TaskStatus?)null,
                        TaskPriority = Enum.TryParse<crmApi.Models.TaskPriority>(createTaskMessage.TaskPriority, out var priority) ? priority : (crmApi.Models.TaskPriority?)null,
                        DueDate = createTaskMessage.DueDate,
                        EstimatedTime = createTaskMessage.EstimatedTime,
                        AssignedUserIds = createTaskMessage.AssignedUserIds,
                        FileReference = fileReference,
                        FileName = audioFile.FileName,
                        MimeType = audioFile.ContentType,
                        FileSize = audioFile.Length,
                        Duration = createTaskMessage.Duration,
                        CreatedAt = DateTime.Now,
                        HasFile = true,
                        AudioUrl = cloudinaryUrl
                    });
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Database transaction failed for task with voice");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task message with voice");
                return StatusCode(500, new { message = "Error creating task message with voice", error = ex.Message });
            }
        }

        [HttpPost("messages/send-with-task")]
        public async Task<ActionResult<MessageResponse>> SendWithTask([FromBody] CreateTaskMessageDto createTaskMessage)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                var taskTitle = createTaskMessage.TaskTitle ?? createTaskMessage.Content ?? "Task";

                string taskQuery = @"
            INSERT INTO Tasks (Title, Description, Status, Priority, DueDate, EstimatedTime, SortOrder, CreatedByUserId, CreatedAt)
            VALUES (@Title, @Description, @Status, @Priority, @DueDate, @EstimatedTime, @SortOrder, @CreatedByUserId, @CreatedAt);
            SELECT LAST_INSERT_ID();";

                using var taskCommand = new MySqlCommand(taskQuery, connection, transaction);
                taskCommand.Parameters.AddWithValue("@Title", taskTitle);
                taskCommand.Parameters.AddWithValue("@Description", createTaskMessage.TaskDescription ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@Status", createTaskMessage.TaskStatus);
                taskCommand.Parameters.AddWithValue("@Priority", createTaskMessage.TaskPriority);
                taskCommand.Parameters.AddWithValue("@DueDate", createTaskMessage.DueDate ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@EstimatedTime", createTaskMessage.EstimatedTime ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@SortOrder", 0);
                taskCommand.Parameters.AddWithValue("@CreatedByUserId", createTaskMessage.SenderId);
                taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());

                if (createTaskMessage.AssignedUserIds?.Any() == true)
                {
                    string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                    var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                    assignQuery += string.Join(", ", values);

                    using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                    assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                    assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < createTaskMessage.AssignedUserIds.Count; i++)
                    {
                        assignCommand.Parameters.AddWithValue($"@UserId{i}", createTaskMessage.AssignedUserIds[i]);
                    }
                    await assignCommand.ExecuteNonQueryAsync();
                }

                if (createTaskMessage.ClientIds?.Any() == true)
                {
                    string clientQuery = "INSERT INTO TaskClients (TaskId, ClientId, AssignedAt) VALUES ";
                    var clientValues = createTaskMessage.ClientIds.Select((_, index) => $"(@TaskId, @ClientId{index}, @AssignedAt)");
                    clientQuery += string.Join(", ", clientValues);

                    using var clientCommand = new MySqlCommand(clientQuery, connection, transaction);
                    clientCommand.Parameters.AddWithValue("@TaskId", taskId);
                    clientCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < createTaskMessage.ClientIds.Count; i++)
                    {
                        clientCommand.Parameters.AddWithValue($"@ClientId{i}", createTaskMessage.ClientIds[i]);
                    }
                    await clientCommand.ExecuteNonQueryAsync();
                }

                if (createTaskMessage.ProjectIds?.Any() == true)
                {
                    string projectQuery = "INSERT INTO TaskProjects (TaskId, ProjectId, AssignedAt) VALUES ";
                    var projectValues = createTaskMessage.ProjectIds.Select((_, index) => $"(@TaskId, @ProjectId{index}, @AssignedAt)");
                    projectQuery += string.Join(", ", projectValues);

                    using var projectCommand = new MySqlCommand(projectQuery, connection, transaction);
                    projectCommand.Parameters.AddWithValue("@TaskId", taskId);
                    projectCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < createTaskMessage.ProjectIds.Count; i++)
                    {
                        projectCommand.Parameters.AddWithValue($"@ProjectId{i}", createTaskMessage.ProjectIds[i]);
                    }
                    await projectCommand.ExecuteNonQueryAsync();
                }

                string messageQuery = @"
            INSERT INTO ChatMessages (DiscussionId, SenderId, ReceiverId, Content, MessageType, TaskId, CreatedAt)
            VALUES (@discussionId, @senderId, @receiverId, @content, @messageType, @taskId, @createdAt);
            SELECT LAST_INSERT_ID();";

                using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                messageCommand.Parameters.AddWithValue("@discussionId", createTaskMessage.DiscussionId);
                messageCommand.Parameters.AddWithValue("@senderId", createTaskMessage.SenderId);
                messageCommand.Parameters.AddWithValue("@receiverId", createTaskMessage.ReceiverId ?? (object)DBNull.Value);
                messageCommand.Parameters.AddWithValue("@content", createTaskMessage.Content);
                messageCommand.Parameters.AddWithValue("@messageType", createTaskMessage.MessageType);
                messageCommand.Parameters.AddWithValue("@taskId", taskId);
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());

                await transaction.CommitAsync();

                _logger.LogInformation($"Task message created. MessageId: {messageId}, TaskId: {taskId}");

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = createTaskMessage.DiscussionId,
                    SenderId = createTaskMessage.SenderId,
                    ReceiverId = createTaskMessage.ReceiverId,
                    Content = createTaskMessage.Content,
                    MessageType = Convert.ToByte(createTaskMessage.MessageType),
                    TaskId = taskId,
                    TaskTitle = taskTitle,
                    TaskDescription = createTaskMessage.TaskDescription,
                    TaskStatus = Enum.TryParse<crmApi.Models.TaskStatus>(createTaskMessage.TaskStatus, out var status) ? status : (crmApi.Models.TaskStatus?)null,
                    TaskPriority = Enum.TryParse<crmApi.Models.TaskPriority>(createTaskMessage.TaskPriority, out var priority) ? priority : (crmApi.Models.TaskPriority?)null,
                    DueDate = createTaskMessage.DueDate,
                    EstimatedTime = createTaskMessage.EstimatedTime?.ToString(),
                    AssignedUserIds = createTaskMessage.AssignedUserIds ?? new List<int>(),
                    ClientIds = createTaskMessage.ClientIds ?? new List<int>(),
                    ProjectIds = createTaskMessage.ProjectIds ?? new List<int>(),
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task message");
                return StatusCode(500, new { message = "Error creating task message", error = ex.Message });
            }
        }

        [HttpGet("voice/tasks/{fileName}")]
        public async Task<ActionResult> GetTaskVoiceFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine("wwwroot", "Uploads", "voice", "tasks", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Voice file not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "audio/webm", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voice file: {fileName}", fileName);
                return StatusCode(500, new { message = "Error retrieving voice file", error = ex.Message });
            }
        }

        [HttpGet("discussions/{discussionId}/messages/with-tasks")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessagesWithTasks(int discussionId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        m.Id, m.DiscussionId, m.SenderId, m.ReceiverId, m.Content, 
                        m.MessageType, m.IsEdited, m.EditedAt, m.CreatedAt, m.FileReference, m.Duration,
                        m.TaskId, t.Title AS TaskTitle, t.Description AS TaskDescription, t.Status AS TaskStatus,
                        t.Priority AS TaskPriority, t.DueDate, t.EstimatedTime,
                        d.FileName, d.OriginalFileName, d.MimeType, d.FileSize,
                        u1.Ad AS SenderName, u2.Ad AS ReceiverName
                    FROM ChatMessages m
                    LEFT JOIN MessageDocuments d ON m.Id = d.MessageId
                    LEFT JOIN Tasks t ON m.TaskId = t.Id
                    LEFT JOIN KullaniciBilgileri u1 ON m.SenderId = u1.KullaniciID
                    LEFT JOIN KullaniciBilgileri u2 ON m.ReceiverId = u2.KullaniciID
                    WHERE m.DiscussionId = @discussionId
                    ORDER BY m.CreatedAt ASC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                var messages = new List<MessageResponse>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var fileReference = reader.IsDBNull(reader.GetOrdinal("FileReference")) ? null : reader["FileReference"].ToString();
                    var duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("Duration"));
                    var taskId = reader.IsDBNull(reader.GetOrdinal("TaskId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("TaskId"));

                    var message = new MessageResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        DiscussionId = Convert.ToInt32(reader["DiscussionId"]),
                        SenderId = Convert.ToInt32(reader["SenderId"]),
                        ReceiverId = reader.IsDBNull(reader.GetOrdinal("ReceiverId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                        Content = reader["Content"].ToString(),
                        MessageType = Convert.ToByte(reader["MessageType"]),
                        IsEdited = Convert.ToBoolean(reader["IsEdited"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        EditedAt = reader.IsDBNull(reader.GetOrdinal("EditedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("EditedAt")),
                        FileReference = fileReference,
                        Duration = duration,
                        FileName = reader.IsDBNull(reader.GetOrdinal("OriginalFileName")) ? null : reader["OriginalFileName"].ToString(),
                        MimeType = reader.IsDBNull(reader.GetOrdinal("MimeType")) ? null : reader["MimeType"].ToString(),
                        FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize")) ? 0 : Convert.ToInt64(reader["FileSize"]),
                        TaskId = taskId,
                        TaskTitle = reader.IsDBNull(reader.GetOrdinal("TaskTitle")) ? null : reader["TaskTitle"].ToString(),
                        TaskDescription = reader.IsDBNull(reader.GetOrdinal("TaskDescription")) ? null : reader["TaskDescription"].ToString(),
                        TaskStatus = reader.IsDBNull(reader.GetOrdinal("TaskStatus")) ? null : Enum.Parse<crmApi.Models.TaskStatus>(reader["TaskStatus"].ToString()),
                        TaskPriority = reader.IsDBNull(reader.GetOrdinal("TaskPriority")) ? null : Enum.Parse<TaskPriority>(reader["TaskPriority"].ToString()),
                        DueDate = reader.IsDBNull(reader.GetOrdinal("DueDate")) ? null : reader.GetDateTime(reader.GetOrdinal("DueDate")),
                        EstimatedTime = reader.IsDBNull(reader.GetOrdinal("EstimatedTime")) ? null : reader["EstimatedTime"].ToString(),
                        AssignedUserIds = new List<int>(),
                        SenderName = reader.IsDBNull(reader.GetOrdinal("SenderName")) ? null : reader["SenderName"].ToString(),
                        ReceiverName = reader.IsDBNull(reader.GetOrdinal("ReceiverName")) ? null : reader["ReceiverName"].ToString()
                    };

                    messages.Add(message);
                }

                reader.Close();

                foreach (var message in messages.Where(m => m.TaskId.HasValue))
                {
                    string assignQuery = @"
                        SELECT ta.UserId, u.Name 
                        FROM TaskAssignments ta
                        LEFT JOIN KullaniciBilgileri u ON ta.UserId = u.Id
                        WHERE ta.TaskId = @taskId";

                    using var assignCommand = new MySqlCommand(assignQuery, connection);
                    assignCommand.Parameters.AddWithValue("@taskId", message.TaskId);
                    using var assignReader = await assignCommand.ExecuteReaderAsync();

                    while (await assignReader.ReadAsync())
                    {
                        message.AssignedUserIds.Add(assignReader.GetInt32(assignReader.GetOrdinal("UserId")));
                    }
                }

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages with tasks");
                return StatusCode(500, new { message = "Error fetching messages with tasks", error = ex.Message });
            }
        }

        [HttpGet("discussions/{discussionId}/tasks")]
        public async Task<ActionResult<List<TaskDataResponse>>> GetDiscussionTasks(int discussionId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT 
                m.Id as MessageId,
                t.Id as TaskId,
                t.Title as TaskTitle,
                t.Description as TaskDescription,
                t.Status as TaskStatus,
                t.Priority as TaskPriority,
                m.Content,
                m.CreatedAt,
                t.UpdatedAt,
                t.DueDate,
                t.EstimatedTime,
                creator.Ad as CreatorFirstName,
                creator.Soyad as CreatorLastName,
                updater.Ad as UpdaterFirstName,
                updater.Soyad as UpdaterLastName,
                m.FileReference,
                CASE 
                    WHEN m.MessageType = 3 THEN CONCAT('/uploads/tasks/', m.FileReference)
                    ELSE NULL 
                END as FileUrl,
                CASE 
                    WHEN m.MessageType = 2 THEN CONCAT('/Uploads/voice/tasks/', m.FileReference)
                    ELSE NULL 
                END as VoiceRecordUrl
            FROM ChatMessages m
            INNER JOIN Tasks t ON m.TaskId = t.Id
            LEFT JOIN KullaniciBilgileri creator ON t.CreatedByUserId = creator.KullaniciID
            LEFT JOIN KullaniciBilgileri updater ON t.UpdatedByUserId = updater.KullaniciID
            WHERE m.DiscussionId = @discussionId 
                AND m.TaskId IS NOT NULL
            ORDER BY m.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                var tasks = new List<TaskDataResponse>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var task = new TaskDataResponse
                    {
                        Id = Convert.ToInt32("MessageId"),
                        TaskId = Convert.ToInt32("TaskId"),
                        TaskTitle = reader["TaskTitle"].ToString(),
                        TaskDescription = Convert.IsDBNull("TaskDescription") ? null : reader["TaskDescription"].ToString(),
                        TaskStatus = reader["TaskStatus"].ToString(),
                        TaskPriority = reader["TaskPriority"].ToString(),
                        Content = reader["Content"].ToString(),
                        CreatedAt = Convert.ToDateTime("CreatedAt").ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdatedAt = Convert.IsDBNull("UpdatedAt") ? null : Convert.ToDateTime("UpdatedAt").ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedBy = $"{reader["CreatorFirstName"]} {reader["CreatorLastName"]}".Trim(),
                        UpdatedBy = Convert.IsDBNull("UpdaterFirstName") ? null : $"{reader["UpdaterFirstName"]} {reader["UpdaterLastName"]}".Trim(),
                        FileUrl = Convert.IsDBNull("FileUrl") ? null : reader["FileUrl"].ToString(),
                        VoiceRecordUrl = Convert.IsDBNull("VoiceRecordUrl") ? null : reader["VoiceRecordUrl"].ToString(),
                        DueDate = Convert.IsDBNull("DueDate") ? null : Convert.ToDateTime("DueDate").ToString("yyyy-MM-dd HH:mm:ss"),
                        EstimatedTime = Convert.IsDBNull("EstimatedTime") ? null : reader["EstimatedTime"].ToString(),
                        AssignedUsers = new List<string>(),
                        Clients = new List<string>(),
                        Projects = new List<string>()
                    };
                    tasks.Add(task);
                }
                reader.Close();

                foreach (var task in tasks)
                {
                    string userQuery = @"
                SELECT u.Ad, u.Soyad 
                FROM TaskAssignments ta
                INNER JOIN KullaniciBilgileri u ON ta.UserId = u.UserId
                WHERE ta.TaskId = @taskId";

                    using var userCommand = new MySqlCommand(userQuery, connection);
                    userCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var userReader = await userCommand.ExecuteReaderAsync();
                    while (await userReader.ReadAsync())
                    {
                        task.AssignedUsers.Add($"{userReader["Ad"]} {userReader["Soyad"]}".Trim());
                    }
                    userReader.Close();

                    string clientQuery = @"
                SELECT c.Name 
                FROM TaskClients tc
                INNER JOIN Clients c ON tc.ClientId = c.Id
                WHERE tc.TaskId = @taskId";

                    using var clientCommand = new MySqlCommand(clientQuery, connection);
                    clientCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var clientReader = await clientCommand.ExecuteReaderAsync();
                    while (await clientReader.ReadAsync())
                    {
                        task.Clients.Add(clientReader["Name"].ToString());
                    }
                    clientReader.Close();

                    string projectQuery = @"
                SELECT p.Name 
                FROM TaskProjects tp
                INNER JOIN Projects p ON tp.ProjectId = p.Id
                WHERE tp.TaskId = @taskId";

                    using var projectCommand = new MySqlCommand(projectQuery, connection);
                    projectCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var projectReader = await projectCommand.ExecuteReaderAsync();
                    while (await projectReader.ReadAsync())
                    {
                        task.Projects.Add(projectReader["Name"].ToString());
                    }
                    projectReader.Close();
                }

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discussion tasks");
                return StatusCode(500, new { message = "Error fetching discussion tasks", error = ex.Message });
            }
        }

        [HttpGet("discussions/{discussionId}/tasks/export-excel")]
        public async Task<ActionResult> ExportDiscussionTasksToExcel(int discussionId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string discussionQuery = "SELECT Title FROM Discussions WHERE Id = @discussionId";
                using var discussionCommand = new MySqlCommand(discussionQuery, connection);
                discussionCommand.Parameters.AddWithValue("@discussionId", discussionId);
                var result = await discussionCommand.ExecuteScalarAsync();
                var discussionTitle = result?.ToString() ?? "Unknown Discussion";

                string query = @"
                                SELECT 
                                    m.Id as MessageId,
                                    t.Id as TaskId,
                                    t.Title as TaskTitle,
                                    t.Description as TaskDescription,
                                    t.Status as TaskStatus,
                                    t.Priority as TaskPriority,
                                    m.Content,
                                    m.CreatedAt,
                                    t.UpdatedAt,
                                    t.DueDate,
                                    t.EstimatedTime,
                                    creator.Ad as CreatorFirstName,
                                    creator.Soyad as CreatorLastName,
                                    updater.Ad as UpdaterFirstName,
                                    updater.Soyad as UpdaterLastName,
                                    m.FileReference,
                                    m.MessageType,
                                    m.FileName,
                                    m.HasFile,
                                    md.IDriveUrl,
                                    md.FileName as DocumentFileName
                                FROM ChatMessages m
                                INNER JOIN Tasks t ON m.TaskId = t.Id
                                LEFT JOIN KullaniciBilgileri creator ON t.CreatedByUserId = creator.KullaniciID
                                LEFT JOIN KullaniciBilgileri updater ON t.UpdatedByUserId = updater.KullaniciID
                                LEFT JOIN MessageDocuments md ON m.Id = md.MessageId
                                WHERE m.DiscussionId = @discussionId 
                                    AND m.TaskId IS NOT NULL
                                ORDER BY m.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                var tasks = new List<dynamic>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var messageType = Convert.ToInt32(reader["MessageType"]);
                    var hasFile = Convert.ToBoolean(reader["HasFile"]);
                    var DocumentFileName = reader["FileName"]?.ToString() ?? reader["DocumentFileName"]?.ToString() ?? "";
                    var idriveUrl = reader["IDriveUrl"]?.ToString() ?? "";
                    var content = reader["Content"]?.ToString() ?? "";

                    string fileDisplay = "";
                    string fileUrl = "";
                    string voiceDisplay = "";
                    string voiceUrl = "";

                    if (hasFile && !string.IsNullOrEmpty(idriveUrl))
                    {
                        if (messageType == 3 || DocumentFileName.Contains("voice") || DocumentFileName.Contains(".webm"))
                        {
                            voiceDisplay = !string.IsNullOrEmpty(DocumentFileName) ? DocumentFileName : "Voice Message";
                            voiceUrl = idriveUrl;
                        }
                        else
                        {
                            fileDisplay = !string.IsNullOrEmpty(DocumentFileName) ? DocumentFileName : "File Attachment";
                            fileUrl = idriveUrl;
                        }
                    }

                    tasks.Add(new
                    {
                        MessageId = Convert.ToInt32(reader["MessageId"]),
                        TaskId = Convert.ToInt32(reader["TaskId"]),
                        TaskTitle = reader["TaskTitle"].ToString(),
                        TaskDescription = Convert.IsDBNull(reader["TaskDescription"]) ? "" : reader["TaskDescription"].ToString(),
                        TaskStatus = reader["TaskStatus"].ToString(),
                        TaskPriority = reader["TaskPriority"].ToString(),
                        Content = content,
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                        UpdatedAt = Convert.IsDBNull(reader["UpdatedAt"]) ? (DateTime?)null : Convert.ToDateTime(reader["UpdatedAt"]),
                        CreatedBy = $"{reader["CreatorFirstName"]} {reader["CreatorLastName"]}".Trim(),
                        UpdatedBy = Convert.IsDBNull(reader["UpdaterFirstName"]) ? "" : $"{reader["UpdaterFirstName"]} {reader["UpdaterLastName"]}".Trim(),
                        FileDisplay = fileDisplay,
                        FileUrl = fileUrl,
                        VoiceDisplay = voiceDisplay,
                        VoiceUrl = voiceUrl,
                        DueDate = Convert.IsDBNull(reader["DueDate"]) ? (DateTime?)null : Convert.ToDateTime(reader["DueDate"]),
                        EstimatedTime = Convert.IsDBNull(reader["EstimatedTime"]) ? "" : reader["EstimatedTime"].ToString()
                    });
                }
                reader.Close();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Tasks");

                worksheet.Cells[1, 1].Value = "Task ID";
                worksheet.Cells[1, 2].Value = "Task Title";
                worksheet.Cells[1, 3].Value = "Task Description";
                worksheet.Cells[1, 4].Value = "Status";
                worksheet.Cells[1, 5].Value = "Priority";
                worksheet.Cells[1, 6].Value = "Content";
                worksheet.Cells[1, 7].Value = "Created By";
                worksheet.Cells[1, 8].Value = "Created At";
                worksheet.Cells[1, 9].Value = "Updated By";
                worksheet.Cells[1, 10].Value = "Updated At";
                worksheet.Cells[1, 11].Value = "Due Date";
                worksheet.Cells[1, 12].Value = "Estimated Time";
                worksheet.Cells[1, 13].Value = "File Attachment";
                worksheet.Cells[1, 14].Value = "Voice Recording";
                worksheet.Cells[1, 15].Value = "Assigned Users";
                worksheet.Cells[1, 16].Value = "Clients";
                worksheet.Cells[1, 17].Value = "Projects";

                using (var range = worksheet.Cells[1, 1, 1, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = task.TaskId;
                    worksheet.Cells[row, 2].Value = task.TaskTitle;
                    worksheet.Cells[row, 3].Value = task.TaskDescription;
                    worksheet.Cells[row, 4].Value = task.TaskStatus;
                    worksheet.Cells[row, 5].Value = task.TaskPriority;
                    worksheet.Cells[row, 6].Value = task.Content;
                    worksheet.Cells[row, 7].Value = task.CreatedBy;
                    worksheet.Cells[row, 8].Value = task.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 9].Value = task.UpdatedBy;
                    worksheet.Cells[row, 10].Value = task.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 11].Value = task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 12].Value = task.EstimatedTime;

                    if (!string.IsNullOrEmpty(task.FileDisplay) && !string.IsNullOrEmpty(task.FileUrl))
                    {
                        try
                        {
                            worksheet.Cells[row, 13].Hyperlink = new Uri(task.FileUrl);
                            worksheet.Cells[row, 13].Value = task.FileDisplay;
                            worksheet.Cells[row, 13].Style.Font.UnderLine = true;
                            worksheet.Cells[row, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        }
                        catch (UriFormatException)
                        {
                            worksheet.Cells[row, 13].Value = task.FileDisplay;
                        }
                    }
                    else
                    {
                        worksheet.Cells[row, 13].Value = "";
                    }

                    if (!string.IsNullOrEmpty(task.VoiceDisplay) && !string.IsNullOrEmpty(task.VoiceUrl))
                    {
                        try
                        {
                            worksheet.Cells[row, 14].Hyperlink = new Uri(task.VoiceUrl);
                            worksheet.Cells[row, 14].Value = task.VoiceDisplay;
                            worksheet.Cells[row, 14].Style.Font.UnderLine = true;
                            worksheet.Cells[row, 14].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        }
                        catch (UriFormatException)
                        {
                            worksheet.Cells[row, 14].Value = task.VoiceDisplay;
                        }
                    }
                    else
                    {
                        worksheet.Cells[row, 14].Value = "";
                    }

                    var assignedUsers = new List<string>();
                    string userQuery = @"
                                            SELECT u.Ad, u.Soyad 
                                            FROM TaskAssignments ta
                                            INNER JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                                            WHERE ta.TaskId = @taskId";

                    using var userCommand = new MySqlCommand(userQuery, connection);
                    userCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var userReader = await userCommand.ExecuteReaderAsync();
                    while (await userReader.ReadAsync())
                    {
                        assignedUsers.Add($"{userReader["Ad"]} {userReader["Soyad"]}".Trim());
                    }
                    userReader.Close();
                    worksheet.Cells[row, 15].Value = string.Join(", ", assignedUsers);

                    var clients = new List<string>();
                    string clientQuery = @"
                                            SELECT c.first_name 
                                            FROM TaskClients tc
                                            INNER JOIN Clients c ON tc.ClientId = c.Id
                                            WHERE tc.TaskId = @taskId";

                    using var clientCommand = new MySqlCommand(clientQuery, connection);
                    clientCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var clientReader = await clientCommand.ExecuteReaderAsync();
                    while (await clientReader.ReadAsync())
                    {
                        clients.Add(clientReader["first_name"].ToString());
                    }
                    clientReader.Close();
                    worksheet.Cells[row, 16].Value = string.Join(", ", clients);

                    var projects = new List<string>();
                    string projectQuery = @"
                                            SELECT p.title 
                                            FROM TaskProjects tp
                                            INNER JOIN Projects p ON tp.ProjectId = p.Id
                                            WHERE tp.TaskId = @taskId";

                    using var projectCommand = new MySqlCommand(projectQuery, connection);
                    projectCommand.Parameters.AddWithValue("@taskId", task.TaskId);
                    using var projectReader = await projectCommand.ExecuteReaderAsync();
                    while (await projectReader.ReadAsync())
                    {
                        projects.Add(projectReader["title"].ToString());
                    }
                    projectReader.Close();
                    worksheet.Cells[row, 17].Value = string.Join(", ", projects);
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"{discussionTitle}_Tasks_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting tasks to Excel");
                return StatusCode(500, new { message = "Error exporting tasks to Excel", error = ex.Message });
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
                    PublicId = $"chat-files/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}",
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
                _logger.LogError(ex, $"Cloudinary upload error for file: {fileName}");
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
                    PublicId = $"voice-messages/{DateTime.Now:yyyy/MM/dd}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}",
                };

                _logger.LogInformation($"Starting Cloudinary audio upload for: {fileName}");
                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary audio upload error: {uploadResult.Error.Message}");
                    throw new Exception($"Cloudinary audio upload failed: {uploadResult.Error.Message}");
                }

                _logger.LogInformation($"Cloudinary audio upload successful: {uploadResult.SecureUrl}");
                return uploadResult.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception in UploadAudioToCloudinary for file: {fileName}");
                throw;
            }
        }

        [HttpGet("discussion/{discussionId}/tasks-and-media")]
        public async Task<ActionResult<DiscussionTasksAndMediaResponseDto>> GetDiscussionTasksAndMedia(int discussionId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var taskIds = new List<int>();
                string chatTaskQuery = @"
                SELECT DISTINCT TaskId 
                FROM ChatMessages
                WHERE DiscussionId = @discussionId AND TaskId IS NOT NULL";

                using (var chatCommand = new MySqlCommand(chatTaskQuery, connection))
                {
                    chatCommand.Parameters.AddWithValue("@discussionId", discussionId);
                    using var reader = await chatCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int taskIdOrdinal = reader.GetOrdinal("TaskId");
                        if (!reader.IsDBNull(taskIdOrdinal))
                        {
                            taskIds.Add(reader.GetInt32(taskIdOrdinal));
                        }
                    }
                }

                var tasks = new List<TaskWithMediaDto>();

                if (taskIds.Any())
                {
                    string taskQuery = @"
                    SELECT t.*, 
                        cu.Ad as CreatedByUserName, cu.Soyad as CreatedByUserSurname,
                        uu.Ad as UpdatedByUserName, uu.Soyad as UpdatedByUserSurname
                    FROM Tasks t
                    LEFT JOIN KullaniciBilgileri cu ON t.CreatedByUserId = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri uu ON t.UpdatedByUserId = uu.KullaniciID
                    WHERE t.Id IN (" + string.Join(",", taskIds) + @")
                    ORDER BY t.SortOrder, t.CreatedAt DESC";

                    using (var taskCommand = new MySqlCommand(taskQuery, connection))
                    using (var taskReader = await taskCommand.ExecuteReaderAsync())
                    {
                        while (await taskReader.ReadAsync())
                        {
                            var task = new TaskWithMediaDto
                            {
                                Id = taskReader.GetInt32(taskReader.GetOrdinal("Id")),
                                Title = taskReader.GetString(taskReader.GetOrdinal("Title")),
                                Description = taskReader.IsDBNull(taskReader.GetOrdinal("Description"))
                                    ? null
                                    : taskReader.GetString(taskReader.GetOrdinal("Description")),
                                Status = Enum.Parse<Models.TaskStatus>(
                                    taskReader.GetString(taskReader.GetOrdinal("Status"))
                                ),
                                Priority = Enum.Parse<TaskPriority>(
                                    taskReader.GetString(taskReader.GetOrdinal("Priority"))
                                ),
                                DueDate = taskReader.IsDBNull(taskReader.GetOrdinal("DueDate"))
                                    ? null
                                    : taskReader.GetDateTime(taskReader.GetOrdinal("DueDate")),
                                EstimatedTime = taskReader.IsDBNull(taskReader.GetOrdinal("EstimatedTime"))
                                    ? null
                                    : taskReader.GetString(taskReader.GetOrdinal("EstimatedTime")),
                                SortOrder = taskReader.GetInt32(taskReader.GetOrdinal("SortOrder")),
                                CreatedByUserId = taskReader.GetInt32(taskReader.GetOrdinal("CreatedByUserId")),
                                CreatedByUserName = taskReader.IsDBNull(taskReader.GetOrdinal("CreatedByUserName"))
                                    ? ""
                                    : $"{taskReader.GetString(taskReader.GetOrdinal("CreatedByUserName"))} {taskReader.GetString(taskReader.GetOrdinal("CreatedByUserSurname"))}",
                                CreatedAt = taskReader.GetDateTime(taskReader.GetOrdinal("CreatedAt")),
                                UpdatedByUserId = taskReader.IsDBNull(taskReader.GetOrdinal("UpdatedByUserId"))
                                    ? null
                                    : taskReader.GetInt32(taskReader.GetOrdinal("UpdatedByUserId")),
                                UpdatedByUserName = taskReader.IsDBNull(taskReader.GetOrdinal("UpdatedByUserName"))
                                    ? null
                                    : $"{taskReader.GetString(taskReader.GetOrdinal("UpdatedByUserName"))} {taskReader.GetString(taskReader.GetOrdinal("UpdatedByUserSurname"))}",
                                UpdatedAt = taskReader.IsDBNull(taskReader.GetOrdinal("UpdatedAt"))
                                    ? null
                                    : taskReader.GetDateTime(taskReader.GetOrdinal("UpdatedAt")),
                                DiscussionId = discussionId
                            };
                            tasks.Add(task);
                        }
                    }

                    if (tasks.Any())
                    {
                        var allAssignedUsers = new Dictionary<int, List<UserResponseDto>>();
                        string userQuery = @"
                        SELECT ta.TaskId, u.KullaniciID, u.KullaniciAdi, u.Ad, u.Soyad, 
                            u.Email, u.Telefon, u.Durum, u.YetkiTuru
                        FROM TaskAssignments ta
                        JOIN KullaniciBilgileri u ON ta.UserId = u.KullaniciID
                        WHERE ta.TaskId IN (" + string.Join(",", taskIds) + ")";

                        using (var userCommand = new MySqlCommand(userQuery, connection))
                        using (var userReader = await userCommand.ExecuteReaderAsync())
                        {
                            while (await userReader.ReadAsync())
                            {
                                var taskId = (int)userReader["TaskId"];
                                var user = new UserResponseDto
                                {
                                    KullaniciID = (int)userReader["KullaniciID"],
                                    KullaniciAdi = userReader["KullaniciAdi"].ToString(),
                                    Ad = userReader["Ad"].ToString(),
                                    Soyad = userReader["Soyad"].ToString(),
                                    Email = userReader["Email"] as string,
                                    Telefon = userReader["Telefon"] as string,
                                    Durum = userReader["Durum"].ToString(),
                                    YetkiTuru = userReader["YetkiTuru"].ToString()
                                };

                                if (!allAssignedUsers.ContainsKey(taskId))
                                    allAssignedUsers[taskId] = new List<UserResponseDto>();

                                allAssignedUsers[taskId].Add(user);
                            }
                        }

                        var allTaskClients = new Dictionary<int, List<int>>();
                        string clientQuery = @"
                        SELECT TaskId, ClientId
                        FROM TaskClients
                        WHERE TaskId IN (" + string.Join(",", taskIds) + ")";

                        using (var clientCommand = new MySqlCommand(clientQuery, connection))
                        using (var clientReader = await clientCommand.ExecuteReaderAsync())
                        {
                            while (await clientReader.ReadAsync())
                            {
                                var taskId = (int)clientReader["TaskId"];
                                var clientId = (int)clientReader["ClientId"];

                                if (!allTaskClients.ContainsKey(taskId))
                                    allTaskClients[taskId] = new List<int>();

                                allTaskClients[taskId].Add(clientId);
                            }
                        }

                        var allTaskProjects = new Dictionary<int, List<int>>();
                        string projectQuery = @"
                        SELECT TaskId, ProjectId
                        FROM TaskProjects
                        WHERE TaskId IN (" + string.Join(",", taskIds) + ")";

                        using (var projectCommand = new MySqlCommand(projectQuery, connection))
                        using (var projectReader = await projectCommand.ExecuteReaderAsync())
                        {
                            while (await projectReader.ReadAsync())
                            {
                                var taskId = (int)projectReader["TaskId"];
                                var projectId = (int)projectReader["ProjectId"];

                                if (!allTaskProjects.ContainsKey(taskId))
                                    allTaskProjects[taskId] = new List<int>();

                                allTaskProjects[taskId].Add(projectId);
                            }
                        }

                        var discussionParticipants = new Dictionary<int, List<UserResponseDto>>();
                        string participantQuery = @"
                        SELECT DISTINCT cm.DiscussionId, cm.ReceiverId, u.KullaniciID, u.KullaniciAdi, u.Ad, u.Soyad, 
                            u.Email, u.Telefon, u.Durum, u.YetkiTuru
                        FROM ChatMessages cm
                        JOIN KullaniciBilgileri u ON cm.ReceiverId = u.KullaniciID
                        WHERE cm.DiscussionId = @discussionId AND cm.ReceiverId IS NOT NULL";

                        using (var participantCommand = new MySqlCommand(participantQuery, connection))
                        {
                            participantCommand.Parameters.AddWithValue("@discussionId", discussionId);
                            using (var participantReader = await participantCommand.ExecuteReaderAsync())
                            {
                                while (await participantReader.ReadAsync())
                                {
                                    var discId = (int)participantReader["DiscussionId"];
                                    var user = new UserResponseDto
                                    {
                                        KullaniciID = (int)participantReader["KullaniciID"],
                                        KullaniciAdi = participantReader["KullaniciAdi"].ToString(),
                                        Ad = participantReader["Ad"].ToString(),
                                        Soyad = participantReader["Soyad"].ToString(),
                                        Email = participantReader["Email"] as string,
                                        Telefon = participantReader["Telefon"] as string,
                                        Durum = participantReader["Durum"].ToString(),
                                        YetkiTuru = participantReader["YetkiTuru"].ToString()
                                    };

                                    if (!discussionParticipants.ContainsKey(discId))
                                        discussionParticipants[discId] = new List<UserResponseDto>();

                                    if (!discussionParticipants[discId].Any(u => u.KullaniciID == user.KullaniciID))
                                        discussionParticipants[discId].Add(user);
                                }
                            }
                        }

                        foreach (var task in tasks)
                        {
                            if (allAssignedUsers.TryGetValue(task.Id, out var users))
                                task.AssignedUsers = users;
                            else
                                task.AssignedUsers = new List<UserResponseDto>();

                            if (allTaskClients.TryGetValue(task.Id, out var clientIds))
                                task.ClientIds = clientIds;
                            else
                                task.ClientIds = new List<int>();

                            if (allTaskProjects.TryGetValue(task.Id, out var projectIds))
                                task.ProjectIds = projectIds;
                            else
                                task.ProjectIds = new List<int>();

                            if (discussionParticipants.TryGetValue(discussionId, out var participants))
                            {
                                foreach (var participant in participants)
                                {
                                    if (!task.AssignedUsers.Any(u => u.KullaniciID == participant.KullaniciID))
                                    {
                                        task.AssignedUsers.Add(participant);
                                    }
                                }
                            }
                        }
                    }
                }

                var documents = new List<ChatDocumentDto>();
                string documentQuery = @"
                    SELECT Id, FileName, FileReference as FilePath, FileSize, CreatedAt, SenderId, MimeType
                    FROM ChatMessages
                    WHERE DiscussionId = @discussionId 
                    AND HasFile = 1 
                    AND MessageType = 1
                    AND FileName IS NOT NULL
                    ORDER BY CreatedAt DESC";

                using (var docCommand = new MySqlCommand(documentQuery, connection))
                {
                    docCommand.Parameters.AddWithValue("@discussionId", discussionId);
                    using (var docReader = await docCommand.ExecuteReaderAsync())
                    {
                        while (await docReader.ReadAsync())
                        {
                            documents.Add(new ChatDocumentDto
                            {
                                Id = (int)docReader["Id"],
                                FileName = docReader["FileName"].ToString(),
                                FilePath = docReader["FilePath"] as string,
                                FileSize = docReader["FileSize"] == DBNull.Value ? 0 : (long)docReader["FileSize"],
                                UploadedAt = (DateTime)docReader["CreatedAt"],
                                UploadedByUserId = (int)docReader["SenderId"],
                                ContentType = docReader["MimeType"] as string
                            });
                        }
                    }
                }

                var voiceRecords = new List<ChatVoiceRecordDto>();
                string voiceQuery = @"
                    SELECT Id, FileName, FileReference as FilePath, Duration, CreatedAt, SenderId, FileSize
                    FROM ChatMessages
                    WHERE DiscussionId = @discussionId 
                    AND HasFile = 1 
                    AND MessageType = 2
                    AND FileName IS NOT NULL
                    ORDER BY CreatedAt DESC";

                using (var voiceCommand = new MySqlCommand(voiceQuery, connection))
                {
                    voiceCommand.Parameters.AddWithValue("@discussionId", discussionId);
                    using (var voiceReader = await voiceCommand.ExecuteReaderAsync())
                    {
                        while (await voiceReader.ReadAsync())
                        {
                            voiceRecords.Add(new ChatVoiceRecordDto
                            {
                                Id = (int)voiceReader["Id"],
                                FileName = voiceReader["FileName"].ToString(),
                                FilePath = voiceReader["FilePath"] as string,
                                Duration = voiceReader["Duration"] == DBNull.Value ? null : (double?)voiceReader["Duration"],
                                RecordedAt = (DateTime)voiceReader["CreatedAt"],
                                RecordedByUserId = (int)voiceReader["SenderId"],
                                FileSize = voiceReader["FileSize"] == DBNull.Value ? null : (long?)voiceReader["FileSize"]
                            });
                        }
                    }
                }

                var response = new DiscussionTasksAndMediaResponseDto
                {
                    DiscussionId = discussionId,
                    Tasks = tasks,
                    Documents = documents,
                    VoiceRecords = voiceRecords
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Tartışma görevleri ve medya alınırken hata oluştu", error = ex.Message });
            }
        }

        [HttpPost("discussions/{discussionId}/create-task-with-message")]
        public async Task<ActionResult<MessageResponse>> CreateTaskWithMessage(int discussionId, [FromBody] CreateTaskMessageDto createTaskMessage)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                var taskTitle = createTaskMessage.TaskTitle ?? createTaskMessage.Content ?? "Task";

                string taskQuery = @"
            INSERT INTO Tasks (Title, Description, Status, Priority, DueDate, EstimatedTime, SortOrder, CreatedByUserId, CreatedAt)
            VALUES (@Title, @Description, @Status, @Priority, @DueDate, @EstimatedTime, @SortOrder, @CreatedByUserId, @CreatedAt);
            SELECT LAST_INSERT_ID();";

                using var taskCommand = new MySqlCommand(taskQuery, connection, transaction);
                taskCommand.Parameters.AddWithValue("@Title", taskTitle);
                taskCommand.Parameters.AddWithValue("@Description", createTaskMessage.TaskDescription ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@Status", createTaskMessage.TaskStatus);
                taskCommand.Parameters.AddWithValue("@Priority", createTaskMessage.TaskPriority);
                taskCommand.Parameters.AddWithValue("@DueDate", createTaskMessage.DueDate ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@EstimatedTime",
                                                    createTaskMessage.EstimatedTime?.ToString() ?? (object)DBNull.Value);
                taskCommand.Parameters.AddWithValue("@SortOrder", createTaskMessage.SortOrder ?? 0);
                taskCommand.Parameters.AddWithValue("@CreatedByUserId", createTaskMessage.SenderId);
                taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());
                _logger.LogInformation($"Task created with ID: {taskId}");

                var discussionParticipants = new List<int>();
                string participantQuery = @"
            SELECT DISTINCT ReceiverId
            FROM ChatMessages
            WHERE DiscussionId = @discussionId AND ReceiverId IS NOT NULL AND ReceiverId != @senderId";

                using var participantCommand = new MySqlCommand(participantQuery, connection, transaction);
                participantCommand.Parameters.AddWithValue("@discussionId", discussionId);
                participantCommand.Parameters.AddWithValue("@senderId", createTaskMessage.SenderId);
                using var participantReader = await participantCommand.ExecuteReaderAsync();

                while (await participantReader.ReadAsync())
                {
                    discussionParticipants.Add((int)participantReader["ReceiverId"]);
                }
                participantReader.Close();

                var allAssignedUserIds = new List<int>();
                if (createTaskMessage.AssignedUserIds?.Any() == true)
                {
                    allAssignedUserIds.AddRange(createTaskMessage.AssignedUserIds);
                }
                allAssignedUserIds.AddRange(discussionParticipants);
                allAssignedUserIds = allAssignedUserIds.Distinct().ToList();

                if (allAssignedUserIds.Any())
                {
                    string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                    var values = allAssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                    assignQuery += string.Join(", ", values);

                    using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                    assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                    assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < allAssignedUserIds.Count; i++)
                    {
                        assignCommand.Parameters.AddWithValue($"@UserId{i}", allAssignedUserIds[i]);
                    }
                    await assignCommand.ExecuteNonQueryAsync();
                }

                if (createTaskMessage.ClientIds?.Any() == true)
                {
                    string clientQuery = "INSERT INTO TaskClients (TaskId, ClientId, AssignedAt) VALUES ";
                    var clientValues = createTaskMessage.ClientIds.Select((_, index) => $"(@TaskId, @ClientId{index}, @AssignedAt)");
                    clientQuery += string.Join(", ", clientValues);

                    using var clientCommand = new MySqlCommand(clientQuery, connection, transaction);
                    clientCommand.Parameters.AddWithValue("@TaskId", taskId);
                    clientCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < createTaskMessage.ClientIds.Count; i++)
                    {
                        clientCommand.Parameters.AddWithValue($"@ClientId{i}", createTaskMessage.ClientIds[i]);
                    }
                    await clientCommand.ExecuteNonQueryAsync();
                }

                if (createTaskMessage.ProjectIds?.Any() == true)
                {
                    string projectQuery = "INSERT INTO TaskProjects (TaskId, ProjectId, AssignedAt) VALUES ";
                    var projectValues = createTaskMessage.ProjectIds.Select((_, index) => $"(@TaskId, @ProjectId{index}, @AssignedAt)");
                    projectQuery += string.Join(", ", projectValues);

                    using var projectCommand = new MySqlCommand(projectQuery, connection, transaction);
                    projectCommand.Parameters.AddWithValue("@TaskId", taskId);
                    projectCommand.Parameters.AddWithValue("@AssignedAt", DateTime.Now);
                    for (int i = 0; i < createTaskMessage.ProjectIds.Count; i++)
                    {
                        projectCommand.Parameters.AddWithValue($"@ProjectId{i}", createTaskMessage.ProjectIds[i]);
                    }
                    await projectCommand.ExecuteNonQueryAsync();
                }

                string messageQuery = @"
                INSERT INTO ChatMessages (DiscussionId, SenderId, Content, MessageType, TaskId, CreatedAt)
                VALUES (@discussionId, @senderId, @content, @messageType, @taskId, @createdAt);
                SELECT LAST_INSERT_ID();";

                using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                messageCommand.Parameters.AddWithValue("@discussionId", createTaskMessage.DiscussionId);
                messageCommand.Parameters.AddWithValue("@senderId", createTaskMessage.SenderId);
                messageCommand.Parameters.AddWithValue("@content", createTaskMessage.Content);
                messageCommand.Parameters.AddWithValue("@messageType", createTaskMessage.MessageType);
                messageCommand.Parameters.AddWithValue("@taskId", taskId);
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());
                _logger.LogInformation($"Task message created with ID: {messageId}");

                await transaction.CommitAsync();

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = createTaskMessage.DiscussionId,
                    SenderId = createTaskMessage.SenderId,
                    Content = createTaskMessage.Content,
                    MessageType = Convert.ToByte(createTaskMessage.MessageType),
                    TaskId = taskId,
                    TaskTitle = taskTitle,
                    TaskDescription = createTaskMessage.TaskDescription,
                    TaskStatus = Enum.TryParse<crmApi.Models.TaskStatus>(createTaskMessage.TaskStatus, out var status) ? status : (crmApi.Models.TaskStatus?)null,
                    TaskPriority = Enum.TryParse<crmApi.Models.TaskPriority>(createTaskMessage.TaskPriority, out var priority) ? priority : (crmApi.Models.TaskPriority?)null,
                    DueDate = createTaskMessage.DueDate,
                    EstimatedTime = createTaskMessage.EstimatedTime?.ToString(),
                    AssignedUserIds = allAssignedUserIds,
                    ClientIds = createTaskMessage.ClientIds ?? new List<int>(),
                    ProjectIds = createTaskMessage.ProjectIds ?? new List<int>(),
                    SortOrder = createTaskMessage.SortOrder ?? 0,
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task with message");
                return StatusCode(500, new { message = "Error creating task with message", error = ex.Message });
            }
        }

        [HttpPut("discussions/{discussionId}/mark-all-seen")]
        public async Task<ActionResult> MarkDiscussionMessagesAsSeen(int discussionId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM ChatMessages 
                        WHERE DiscussionId = @discussionId 
                        AND ReceiverId = @userId 
                        AND IsSeen = 0";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@discussionId", discussionId);
                checkCommand.Parameters.AddWithValue("@userId", userId);

                var unseenCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (unseenCount == 0)
                {
                    return Ok(new
                    {
                        message = "No unseen messages to update",
                        messagesUpdated = 0,
                        hasUpdates = false
                    });
                }

                string updateQuery = @"
                    UPDATE ChatMessages
                    SET IsSeen = 1, SeenAt = @seenAt
                    WHERE DiscussionId = @discussionId
                    AND ReceiverId = @userId
                    AND IsSeen = 0";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@discussionId", discussionId);
                updateCommand.Parameters.AddWithValue("@userId", userId);
                updateCommand.Parameters.AddWithValue("@seenAt", DateTime.Now);

                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                _logger.LogInformation($"Marked {rowsAffected} messages as seen for discussion {discussionId} and user {userId}");

                return Ok(new
                {
                    message = "Discussion messages marked as seen successfully",
                    messagesUpdated = rowsAffected,
                    hasUpdates = true
                });
            }
            catch (MySqlException mysqlEx)
            {
                _logger.LogError(mysqlEx, $"MySQL error marking discussion messages as seen. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Database error occurred while marking messages as seen",
                    error = mysqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking discussion messages as seen. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Error marking discussion messages as seen",
                    error = ex.Message
                });
            }
        }

        [HttpPut("messages/{messageId}/mark-seen")]
        public async Task<ActionResult> MarkMessageAsSeen(int messageId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string checkQuery = @"
                    SELECT COUNT(*) 
                    FROM ChatMessages 
                    WHERE Id = @messageId 
                    AND ReceiverId = @userId";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@messageId", messageId);
                checkCommand.Parameters.AddWithValue("@userId", userId);

                var messageExists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!messageExists)
                {
                    return NotFound(new { message = "Message not found or user not authorized" });
                }

                string updateQuery = @"
                    UPDATE ChatMessages 
                    SET IsSeen = 1, SeenAt = @seenAt 
                    WHERE Id = @messageId 
                    AND ReceiverId = @userId 
                    AND IsSeen = 0";

                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@messageId", messageId);
                updateCommand.Parameters.AddWithValue("@userId", userId);
                updateCommand.Parameters.AddWithValue("@seenAt", DateTime.Now);

                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    string alreadySeenQuery = @"
                SELECT IsSeen 
                FROM ChatMessages 
                WHERE Id = @messageId 
                AND ReceiverId = @userId";

                    using var seenCheckCommand = new MySqlCommand(alreadySeenQuery, connection);
                    seenCheckCommand.Parameters.AddWithValue("@messageId", messageId);
                    seenCheckCommand.Parameters.AddWithValue("@userId", userId);

                    var result = await seenCheckCommand.ExecuteScalarAsync();
                    if (result != null && Convert.ToBoolean(result))
                    {
                        return Ok(new { message = "Message was already marked as seen" });
                    }

                    return BadRequest(new { message = "Message could not be updated" });
                }

                return Ok(new { message = "Message marked as seen successfully" });
            }
            catch (MySqlException mysqlEx)
            {
                _logger.LogError(mysqlEx, $"MySQL error marking message as seen. Message: {messageId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Database error occurred while marking message as seen",
                    error = mysqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message as seen. Message: {messageId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Error marking message as seen",
                    error = ex.Message
                });
            }
        }

        [HttpGet("discussions/{discussionId}/unreadcount")]
        public async Task<ActionResult<int>> GetUnreadMessageCount(int discussionId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = @"
            SELECT COUNT(*)
            FROM ChatMessages
            WHERE DiscussionId = @discussionId
            AND ReceiverId = @userId
            AND IsSeen = 0";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);
                command.Parameters.AddWithValue("@userId", userId);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return Ok(count);
            }
            catch (MySqlException mysqlEx)
            {
                _logger.LogError(mysqlEx, $"MySQL error getting unread message count. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Database error occurred while getting unread count",
                    error = mysqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unread message count. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Error getting unread message count",
                    error = ex.Message
                });
            }
        }

        [HttpGet("discussions/{discussionId}/unseencount")]
        public async Task<ActionResult<int>> GetUnseenMessageCount(int discussionId, [FromQuery] int userId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                string query = @"
            SELECT COUNT(*)
            FROM ChatMessages
            WHERE DiscussionId = @discussionId
            AND SenderId = @userId
            AND IsSeen = 0";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);
                command.Parameters.AddWithValue("@userId", userId);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return Ok(count);
            }
            catch (MySqlException mysqlEx)
            {
                _logger.LogError(mysqlEx, $"MySQL error getting unseen message count. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Database error occurred while getting unseen count",
                    error = mysqlEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting unseen message count. Discussion: {discussionId}, User: {userId}");
                return StatusCode(500, new
                {
                    message = "Error getting unseen message count",
                    error = ex.Message
                });
            }
        }


    }

    public class EditMessageRequest
    {
        public int UserId { get; set; }
        public required string Content { get; set; }
    }
}