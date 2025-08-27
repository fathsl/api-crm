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
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string query = @"
                    SELECT 
                        m.Id, m.DiscussionId, m.SenderId, m.ReceiverId, m.Content, 
                        m.MessageType, m.IsEdited, m.EditedAt, m.CreatedAt, m.FileReference, m.Duration,
                        d.FileName, d.OriginalFileName, d.MimeType, d.FileSize
                    FROM ChatMessages m
                    LEFT JOIN MessageDocuments d ON m.Id = d.MessageId
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
                    
                    _logger.LogInformation($"Message ID: {reader["Id"]}, MessageType: {reader["MessageType"]}, FileReference exists: {!string.IsNullOrEmpty(fileReference)}, Length: {fileReference?.Length ?? 0}, Duration: {duration}");

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
                        FileReference = fileReference,
                        Duration = duration,
                        FileName = reader.IsDBNull(reader.GetOrdinal("OriginalFileName")) ? null : reader["OriginalFileName"].ToString(),
                        MimeType = reader.IsDBNull(reader.GetOrdinal("MimeType")) ? null : reader["MimeType"].ToString(),
                        FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize")) ? 0 : Convert.ToInt64(reader["FileSize"])
                    };
                    messages.Add(message);
                }
                
                var voiceMessages = messages.Where(m => m.MessageType == 3).ToList();
                _logger.LogInformation($"Retrieved {messages.Count} total messages, {voiceMessages.Count} voice messages for discussion {discussionId}");
                
                foreach (var vm in voiceMessages)
                {
                    _logger.LogInformation($"Voice Message ID: {vm.Id}, Duration: {vm.Duration}, FileReference length: {vm.FileReference?.Length ?? 0}");
                }
                
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
                    _logger.LogInformation($"Processing file: {file.FileName}, Size: {file.Length}, ContentType: {file.ContentType}");

                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(fileBytes);
                    
                    fileReference = $"data:{file.ContentType};base64,{base64String}";
                    
                    _logger.LogInformation($"Base64 string length: {base64String.Length}");
                    _logger.LogInformation($"Full data URI length: {fileReference.Length}");
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
                messageCommand.Parameters.AddWithValue("@fileReference", (object)fileReference ?? DBNull.Value);
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());

                if (file != null && file.Length > 0)
                {
                    string documentQuery = @"
                        INSERT INTO MessageDocuments (MessageId, FileName, OriginalFileName, FileSize, MimeType, FilePath, UploadedAt)
                        VALUES (@messageId, @fileName, @originalFileName, @fileSize, @mimeType, @filePath, @uploadedAt);";

                    using var documentCommand = new MySqlCommand(documentQuery, connection, transaction);
                    documentCommand.Parameters.AddWithValue("@messageId", messageId);
                    documentCommand.Parameters.AddWithValue("@fileName", file.FileName);
                    documentCommand.Parameters.AddWithValue("@originalFileName", file.FileName);
                    documentCommand.Parameters.AddWithValue("@fileSize", file.Length);
                    documentCommand.Parameters.AddWithValue("@mimeType", file.ContentType);
                    documentCommand.Parameters.AddWithValue("@filePath", "base64_stored_in_message");
                    documentCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);

                    await documentCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                _logger.LogInformation($"Message saved with ID: {messageId}, FileReference length: {fileReference?.Length ?? 0}");

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


        [HttpPost("messages/send-with-voice")]
        public async Task<ActionResult<MessageResponse>> SendMessageWithVoice([FromForm] int discussionId,
                                                                            [FromForm] int senderId,
                                                                            [FromForm] int? receiverId,
                                                                            [FromForm] string content,
                                                                            [FromForm] byte messageType,
                                                                            [FromForm] IFormFile? voiceFile,
                                                                            [FromForm] int duration)
        {
            try
            {
                _logger.LogInformation($"Received voice message request: DiscussionId={discussionId}, SenderId={senderId}, Duration={duration}");
                
                if (voiceFile == null || voiceFile.Length == 0)
                {
                    return BadRequest(new { message = "Voice file is required" });
                }
                
                if (duration <= 0)
                {
                    _logger.LogWarning($"Invalid duration received: {duration}");
                    return BadRequest(new { message = "Invalid duration" });
                }

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                string fileReference = null;

                _logger.LogInformation($"Processing voice file: {voiceFile.FileName}, Size: {voiceFile.Length}, ContentType: {voiceFile.ContentType}");

                const int maxFileSizeBytes = 2 * 1024 * 1024;
                if (voiceFile.Length > maxFileSizeBytes)
                {
                    return BadRequest(new { message = "Voice file too large. Maximum 2MB allowed." });
                }

                var allowedContentTypes = new[] { "audio/wav", "audio/webm", "audio/ogg", "audio/mp3", "audio/mpeg", "audio/m4a" };
                if (!allowedContentTypes.Contains(voiceFile.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "Invalid audio format. Supported formats: WAV, WebM, OGG, MP3, M4A" });
                }

                using var memoryStream = new MemoryStream();
                await voiceFile.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                
                if (fileBytes.Length == 0)
                {
                    return BadRequest(new { message = "Empty audio file" });
                }
                
                var base64String = Convert.ToBase64String(fileBytes);
                fileReference = $"data:{voiceFile.ContentType};base64,{base64String}";
                
                _logger.LogInformation($"Voice Base64 string length: {base64String.Length}");
                _logger.LogInformation($"Full voice data URI length: {fileReference.Length}");

                string messageQuery = @"INSERT INTO ChatMessages (DiscussionId, SenderId, ReceiverId, Content, MessageType, FileReference, Duration, CreatedAt)
                                    VALUES (@discussionId, @senderId, @receiverId, @content, @messageType, @fileReference, @duration, @createdAt);
                                    SELECT LAST_INSERT_ID();";

                using var messageCommand = new MySqlCommand(messageQuery, connection, transaction);
                messageCommand.Parameters.AddWithValue("@discussionId", discussionId);
                messageCommand.Parameters.AddWithValue("@senderId", senderId);
                messageCommand.Parameters.AddWithValue("@receiverId", receiverId ?? (object)DBNull.Value);
                messageCommand.Parameters.AddWithValue("@content", content ?? "Voice message");
                messageCommand.Parameters.AddWithValue("@messageType", messageType);
                messageCommand.Parameters.AddWithValue("@fileReference", fileReference);
                messageCommand.Parameters.AddWithValue("@duration", duration);
                messageCommand.Parameters.AddWithValue("@createdAt", DateTime.Now);

                var messageId = Convert.ToInt32(await messageCommand.ExecuteScalarAsync());

                string documentQuery = @"
                    INSERT INTO MessageDocuments (MessageId, FileName, OriginalFileName, FileSize, MimeType, FilePath, UploadedAt)
                    VALUES (@messageId, @fileName, @originalFileName, @fileSize, @mimeType, @filePath, @uploadedAt);";

                using var documentCommand = new MySqlCommand(documentQuery, connection, transaction);
                documentCommand.Parameters.AddWithValue("@messageId", messageId);
                documentCommand.Parameters.AddWithValue("@fileName", voiceFile.FileName ?? "voice_message.webm");
                documentCommand.Parameters.AddWithValue("@originalFileName", voiceFile.FileName ?? "voice_message.webm");
                documentCommand.Parameters.AddWithValue("@fileSize", voiceFile.Length);
                documentCommand.Parameters.AddWithValue("@mimeType", voiceFile.ContentType);
                documentCommand.Parameters.AddWithValue("@filePath", "base64_stored_in_message");
                documentCommand.Parameters.AddWithValue("@uploadedAt", DateTime.Now);

                await documentCommand.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Voice message saved successfully with ID: {messageId}, Duration: {duration}, FileReference length: {fileReference.Length}");

                return Ok(new MessageResponse
                {
                    Id = messageId,
                    DiscussionId = discussionId,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content ?? "Voice message",
                    MessageType = messageType,
                    IsEdited = false,
                    CreatedAt = DateTime.Now,
                    FileReference = fileReference,
                    Duration = duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice message");
                return StatusCode(500, new { message = "Error sending voice message", error = ex.Message });
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

                string query = @"SELECT FileName, MimeType, FileData FROM FileStorage WHERE MessageId = @messageId";
                
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@messageId", messageId);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return NotFound("File not found");

                var fileName = reader["FileName"].ToString();
                var mimeType = reader["MimeType"].ToString();
                var fileData = (byte[])reader["FileData"];

                return File(fileData, mimeType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file");
                return StatusCode(500, new { message = "Error retrieving file", error = ex.Message });
            }
        }

        [HttpGet("messages/{messageId}/voice")]
        public async Task<IActionResult> GetVoiceMessage(int messageId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
            SELECT cm.FileReference, md.FileName, md.MimeType 
            FROM ChatMessages cm
            LEFT JOIN MessageDocuments md ON cm.Id = md.MessageId
            WHERE cm.Id = @messageId AND cm.MessageType = @voiceMessageType";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@messageId", messageId);
                command.Parameters.AddWithValue("@voiceMessageType", 3);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return NotFound("Voice message not found");

                var fileReference = reader["FileReference"]?.ToString();
                var fileName = reader["FileName"]?.ToString() ?? "voice_message.webm";
                var mimeType = reader["MimeType"]?.ToString() ?? "audio/webm";

                if (string.IsNullOrEmpty(fileReference) || !fileReference.StartsWith("data:"))
                    return NotFound("Voice data not found");

                try
                {
                    var base64Data = fileReference.Split(',')[1];
                    var audioBytes = Convert.FromBase64String(base64Data);

                    return File(audioBytes, mimeType, fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error decoding voice data");
                    return StatusCode(500, new { message = "Error processing voice data" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving voice message");
                return StatusCode(500, new { message = "Error retrieving voice message", error = ex.Message });
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