using crmApi.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

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
            var messages = new List<MessageResponse>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT Id, DiscussionId, SenderId, ReceiverId, Content, MessageType, IsEdited, EditedAt, CreatedAt
                    FROM ChatMessages
                    WHERE DiscussionId = @discussionId
                    ORDER BY CreatedAt ASC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@discussionId", discussionId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    messages.Add(new MessageResponse
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        DiscussionId = Convert.ToInt32(reader["DiscussionId"]),
                        SenderId = Convert.ToInt32(reader["SenderId"]),
                        ReceiverId = reader.IsDBNull(reader.GetOrdinal("ReceiverId")) ? null : reader.GetInt32(reader.GetOrdinal("ReceiverId")),
                        Content = reader["Content"].ToString() ?? "",
                        MessageType = Convert.ToByte(reader["MessageType"]),
                        IsEdited = Convert.ToBoolean(reader["IsEdited"]),
                        EditedAt = reader.IsDBNull(reader.GetOrdinal("EditedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("EditedAt")),
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages");
                return StatusCode(500, new { message = "Error fetching messages", error = ex.Message });
            }

            return Ok(messages);
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

        [HttpPost("messages/send-with-file")]
        public async Task<ActionResult<MessageResponse>> SendMessageWithFile([FromForm] int discussionId,
                                                                            [FromForm] int senderId,
                                                                            [FromForm] int? receiverId,
                                                                            [FromForm] string content,
                                                                            [FromForm] byte messageType,
                                                                            [FromForm] IFormFile? file)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                string fileReference = null;

                if (file != null && file.Length > 0)
                {
                    // Convert file to Base64 and store in database
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(fileBytes);
                    
                    // Create a data URL
                    fileReference = $"data:{file.ContentType};base64,{base64String}";
                }

                string messageQuery = @"INSERT INTO ChatMessages (DiscussionId, SenderId, ReceiverId, Content, MessageType, FileReference, CreatedAt)
                                    VALUES (@discussionId, @senderId, @receiverId, @content, @messageType, @fileReference, @createdAt);
                                    SELECT LAST_INSERT_ID();";

                using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                messageCommand.Parameters.AddWithValue("@discussionId", discussionId);
                messageCommand.Parameters.AddWithValue("@senderId", senderId);
                messageCommand.Parameters.AddWithValue("@receiverId", receiverId ?? (object)DBNull.Value);
                messageCommand.Parameters.AddWithValue("@content", content);
                messageCommand.Parameters.AddWithValue("@messageType", messageType);
                messageCommand.Parameters.AddWithValue("@fileReference", fileReference ?? (object)DBNull.Value);
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());

                if (file != null && file.Length > 0)
                {
                    // Store metadata for filename
                    string documentQuery = @"
                        INSERT INTO MessageDocuments (MessageId, FileName, OriginalFileName, FileSize, MimeType, FilePath, UploadedAt)
                        VALUES (@messageId, @fileName, @originalFileName, @fileSize, @mimeType, @filePath, @uploadedAt);";

                    using var documentCommand = new MySqlCommand(documentQuery, connection, transaction);
                    documentCommand.Parameters.AddWithValue("@messageId", messageId);
                    documentCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    documentCommand.Parameters.AddWithValue("@originalFileName", file.FileName);
                    documentCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    documentCommand.Parameters.AddWithValue("@mimeType", file.ContentType);
                    documentCommand.Parameters.AddWithValue("@filePath", fileReference);
                    documentCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);

                    await documentCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = discussionId,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    MessageType = messageType,
                    IsEdited = false,
                    CreatedAt = DateTime.Now,
                    FileReference = fileReference
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message with file");
                return StatusCode(500, new { message = "Error sending message with file", error = ex.Message });
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

        [HttpGet("files/{*filePath}")]
        public async Task<IActionResult> GetFile(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));
                
                if (!System.IO.File.Exists(fullPath))
                    return NotFound("File not found");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                
                var contentType = "application/octet-stream";
                var ext = Path.GetExtension(fullPath).ToLowerInvariant();
                if (ext == ".png") contentType = "image/png";
                else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                else if (ext == ".gif") contentType = "image/gif";
                else if (ext == ".pdf") contentType = "application/pdf";
                else if (ext == ".doc") contentType = "application/msword";
                else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                
                return File(fileBytes, contentType, Path.GetFileName(fullPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving file");
                return StatusCode(500, new { message = "Error serving file", error = ex.Message });
            }
        }
    }

    public class EditMessageRequest
    {
        public int UserId { get; set; }
        public required string Content { get; set; }
    }
}