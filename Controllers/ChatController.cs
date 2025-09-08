using Amazon.S3;
using CloudinaryDotNet.Actions;
using crmApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Dapper;

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

                string query = @"
                    SELECT d.Id, d.Title, d.Description, d.CreatedByUserId, d.CreatedAt
                    FROM Discussions d
                    INNER JOIN DiscussionParticipants dp ON d.Id = dp.DiscussionId
                    WHERE dp.UserId = @userId
                    ORDER BY d.CreatedAt DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    discussions.Add(new DiscussionResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? "",
                        CreatedByUserId = Convert.ToInt32(reader["CreatedByUserId"]),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching discussions");
                return StatusCode(500, new { message = "Error fetching discussions", error = ex.Message });
            }

            return Ok(discussions);
        }

        // ✅ Get messages for a discussion
        [HttpGet("discussions/{discussionId}/messages")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessages(int discussionId)
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
                        d.FileName, d.OriginalFileName, d.MimeType, d.FileSize,d.IDriveUrl,d.BucketName,d.FileKey
                    FROM ChatMessages m
                    LEFT JOIN MessageDocuments d ON m.Id = d.MessageId
                    LEFT JOIN Tasks t ON m.TaskId = t.Id
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
                        IDriveUrl = reader["IdriveUrl"]?.ToString(),
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

                var voiceMessages = messages.Where(m => m.MessageType == (byte)MessageType.Voice).ToList();

                return Ok(messages);
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

                string query = @"INSERT INTO Discussions (Title, Description, CreatedByUserId, CreatedAt)
                                 VALUES (@title, @description, @createdByUserId, @createdAt);
                                 SELECT LAST_INSERT_ID();";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@title", request.Title);
                command.Parameters.AddWithValue("@description", request.Description ?? "");
                command.Parameters.AddWithValue("@createdByUserId", request.CreatedByUserId);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);

                _logger.LogInformation($"Creating discussion with Title: {request.Title}, CreatedByUserId: {request.CreatedByUserId}");

                var discussionId = Convert.ToInt32(await command.ExecuteScalarAsync());

                foreach (var userId in request.ParticipantUserIds)
                {
                    string participantQuery = @"INSERT INTO DiscussionParticipants (DiscussionId, UserId, Role, JoinedAt, JoinedByUserId)
                                                VALUES (@discussionId, @userId, 0, @joinedAt, @createdByUserId)";
                    using var participantCommand = new MySqlCommand(participantQuery, connection);
                    participantCommand.Parameters.AddWithValue("@discussionId", discussionId);
                    participantCommand.Parameters.AddWithValue("@userId", userId);
                    participantCommand.Parameters.AddWithValue("@joinedAt", DateTime.Now);
                    participantCommand.Parameters.AddWithValue("@createdByUserId", request.CreatedByUserId);
                    await participantCommand.ExecuteNonQueryAsync();
                }

                return Ok(new DiscussionResponse
                {
                    Id = discussionId,
                    Title = request.Title,
                    Description = request.Description ?? "",
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discussion");
                return StatusCode(500, new { message = "Error creating discussion", error = ex.Message });
            }
        }

        // ✅ Send a message
        [HttpPost("messages")]
        public async Task<ActionResult<MessageResponse>> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"INSERT INTO ChatMessages (DiscussionId, SenderId, ReceiverId, Content, MessageType, CreatedAt)
                                 VALUES (@discussionId, @senderId, @receiverId, @content, @messageType, @createdAt);
                                 SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", request.DiscussionId);
                command.Parameters.AddWithValue("@senderId", request.SenderId);
                command.Parameters.AddWithValue("@receiverId", request.ReceiverId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@content", request.Content);
                command.Parameters.AddWithValue("@messageType", request.MessageType);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);

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
                    CreatedAt = DateTime.Now
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
                    FileReference
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
                    @fileReference
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
                    messageCommand.Parameters.AddWithValue("@fileReference", request.FileReference ?? $"ref_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{file.FileName}");

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
                    docCommand.Parameters.AddWithValue("@fileKey", request.FileKey ?? $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}");

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
                        createdAt = DateTime.UtcNow,
                        fileName = file.FileName,
                        fileSize = file.Length,
                        mimeType = file.ContentType,
                        idriveUrl = cloudinaryUrl,
                        fileReference = request.FileReference,
                        bucketName = request.BucketName ?? "chat-files",
                        fileKey = request.FileKey ?? $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}",
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
                    var fileReference = request.FileReference ?? $"voice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{audioFile.FileName}";
                    var fileKey = request.FileKey ?? $"voice/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{audioFile.FileName}";

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
                    Duration
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
                    @duration
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
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow);
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
                        createdAt = DateTime.UtcNow,
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
                    Expires = DateTime.UtcNow.AddHours(24)
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
                    taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                    var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task created with ID: {taskId}");

                    if (createTaskMessage.AssignedUserIds?.Any() == true)
                    {
                        string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                        var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                        assignQuery += string.Join(", ", values);

                        using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                        assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                        assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
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

                    var fileReference = $"task_file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{file.FileName}";
                    var fileKey = $"task-files/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{file.FileName}";

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
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

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
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow);
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
                        CreatedAt = DateTime.UtcNow,
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
                    taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                    var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());
                    _logger.LogInformation($"Task created with ID: {taskId}");

                    if (createTaskMessage.AssignedUserIds?.Any() == true)
                    {
                        string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                        var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                        assignQuery += string.Join(", ", values);

                        using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                        assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                        assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
                        for (int i = 0; i < createTaskMessage.AssignedUserIds.Count; i++)
                        {
                            assignCommand.Parameters.AddWithValue($"@UserId{i}", createTaskMessage.AssignedUserIds[i]);
                        }
                        await assignCommand.ExecuteNonQueryAsync();
                    }

                    var fileReference = $"task_voice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{audioFile.FileName}";
                    var fileKey = $"task-voice/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{audioFile.FileName}";

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
                    messageCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

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
                    docCommand.Parameters.AddWithValue("@uploadedAt", DateTime.UtcNow);
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
                        CreatedAt = DateTime.UtcNow,
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
                taskCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                var taskId = Convert.ToInt32(await taskCommand.ExecuteScalarAsync());

                if (createTaskMessage.AssignedUserIds?.Any() == true)
                {
                    string assignQuery = "INSERT INTO TaskAssignments (TaskId, UserId, AssignedAt) VALUES ";
                    var values = createTaskMessage.AssignedUserIds.Select((_, index) => $"(@TaskId, @UserId{index}, @AssignedAt)");
                    assignQuery += string.Join(", ", values);

                    using var assignCommand = new MySqlCommand(assignQuery, connection, transaction);
                    assignCommand.Parameters.AddWithValue("@TaskId", taskId);
                    assignCommand.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
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
                    clientCommand.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
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
                    projectCommand.Parameters.AddWithValue("@AssignedAt", DateTime.UtcNow);
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
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

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
                    CreatedAt = DateTime.UtcNow
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
                        u1.Name AS SenderName, u2.Name AS ReceiverName
                    FROM ChatMessages m
                    LEFT JOIN MessageDocuments d ON m.Id = d.MessageId
                    LEFT JOIN Tasks t ON m.TaskId = t.Id
                    LEFT JOIN KullaniciBilgileri u1 ON m.SenderId = u1.Id
                    LEFT JOIN KullaniciBilgileri u2 ON m.ReceiverId = u2.Id
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
                    PublicId = $"voice-messages/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}",
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


    }

    public class EditMessageRequest
    {
        public int UserId { get; set; }
        public required string Content { get; set; }
    }
}