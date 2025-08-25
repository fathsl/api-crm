using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using crmApi.Models;
using System.Data;

namespace crmApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly string _connectionString;

        public MeetingController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/Meeting
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MeetingResponseDto>>> GetAllMeetings()
        {
            var meetings = new List<MeetingResponseDto>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT m.*,
                           c.First_name AS ClientFirstName, c.Last_name AS ClientLastName,
                           c.Email AS ClientEmail, c.Details AS ClientCompanyName,
                           cu.Ad AS OrganizerFirstName, cu.Soyad AS OrganizerLastName,
                           cbu.Ad AS CreatedByFirstName, cbu.Soyad AS CreatedByLastName,
                           COUNT(DISTINCT mp.user_id) AS ParticipantCount
                    FROM Meetings m
                    LEFT JOIN Clients c ON m.ClientId = c.Id
                    LEFT JOIN KullaniciBilgileri cu ON m.CreatedBy = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri cbu ON m.CreatedBy = cbu.KullaniciID
                    LEFT JOIN MeetingParticipants mp ON m.meeting_id  = mp.meeting_id
                    GROUP BY m.meeting_id , c.Id, cu.KullaniciID, cbu.KullaniciID, mbu.KullaniciID
                    ORDER BY m.MeetingDate DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var meeting = new MeetingResponseDto
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString(),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        MeetingDate = reader.GetDateTime("MeetingDate"),
                        DurationMinutes = reader.GetInt32("DurationMinutes"),
                        Location = reader.IsDBNull("Location") ? null : reader.GetString("Location"),
                        MeetingType = reader.GetString("MeetingType"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        ClientId = reader.IsDBNull("ClientId") ? null : reader.GetInt32("ClientId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedAt = reader.GetDateTime("ModifiedAt"),
                        ClientName = reader.IsDBNull("ClientFirstName") ? null :
                                   $"{reader.GetString("ClientFirstName")} {reader.GetString("ClientLastName")}",
                        ClientCompanyName = reader.IsDBNull("ClientCompanyName") ? null : reader.GetString("ClientCompanyName"),
                        ClientEmail = reader.IsDBNull("ClientEmail") ? null : reader.GetString("ClientEmail"),
                        OrganizerName = reader.IsDBNull("OrganizerFirstName") ? null :
                                       $"{reader.GetString("OrganizerFirstName")} {reader.GetString("OrganizerLastName")}",
                        ParticipantCount = reader.GetInt32("ParticipantCount"),
                        CreatedByUserName = reader.IsDBNull("CreatedByFirstName") ? null :
                                           $"{reader.GetString("CreatedByFirstName")} {reader.GetString("CreatedByLastName")}",
                        ModifiedByUserName = reader.IsDBNull("ModifiedByFirstName") ? null :
                                            $"{reader.GetString("ModifiedByFirstName")} {reader.GetString("ModifiedByLastName")}"
                    };
                    meetings.Add(meeting);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplantılar alınırken hata oluştu", error = ex.Message });
            }

            return Ok(meetings);
        }

        // GET: api/Meeting/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeetingResponseDto>> GetMeetingById(int id)
        {
            MeetingResponseDto meeting = null;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT m.*,
                           c.First_name AS ClientFirstName, c.Last_name AS ClientLastName,
                           c.Email AS ClientEmail, c.Details AS ClientCompanyName,
                           cu.Ad AS OrganizerFirstName, cu.Soyad AS OrganizerLastName,
                           COUNT(DISTINCT mp.user_id) AS ParticipantCount
                    FROM Meetings m
                    LEFT JOIN Clients c ON m.ClientId = c.Id
                    LEFT JOIN KullaniciBilgileri cu ON m.CreatedBy = cu.KullaniciID
                    LEFT JOIN MeetingParticipants mp ON m.Id = mp.meeting_id 
                    WHERE m.client_id = @client_id
                    GROUP BY m.client_id, c.client_id, cu.KullaniciID";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    meeting = new MeetingResponseDto
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        MeetingDate = reader.GetDateTime("MeetingDate"),
                        DurationMinutes = reader.GetInt32("DurationMinutes"),
                        Location = reader.IsDBNull("Location") ? null : reader.GetString("Location"),
                        MeetingType = reader.GetString("MeetingType"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        ClientId = reader.IsDBNull("ClientId") ? null : reader.GetInt32("ClientId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedAt = reader.GetDateTime("ModifiedAt"),
                        ClientName = reader.IsDBNull("ClientFirstName") ? null :
                                   $"{reader.GetString("ClientFirstName")} {reader.GetString("ClientLastName")}",
                        ClientCompanyName = reader.IsDBNull("ClientCompanyName") ? null : reader.GetString("ClientCompanyName"),
                        ClientEmail = reader.IsDBNull("ClientEmail") ? null : reader.GetString("ClientEmail"),
                        OrganizerName = reader.IsDBNull("OrganizerFirstName") ? null :
                                       $"{reader.GetString("OrganizerFirstName")} {reader.GetString("OrganizerLastName")}",
                        ParticipantCount = reader.GetInt32("ParticipantCount")
                    };
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplantı alınırken hata oluştu", error = ex.Message });
            }

            if (meeting == null)
            {
                return NotFound(new { message = "Toplantı bulunamadı" });
            }

            return Ok(meeting);
        }

        // POST: api/Meeting
       [HttpPost]
        public async Task<ActionResult<MeetingResponseDto>> CreateMeeting([FromBody] MeetingCreateDto meetingDto, [FromQuery] int createdBy = 1)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int meetingId = 0;
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string insertQuery = @"
                        INSERT INTO Meetings (Title, Description, MeetingDate, DurationMinutes, Location, 
                                            MeetingType, Status, CreatedBy, ClientId, CreatedAt, ModifiedAt)
                        VALUES (@Title, @Description, @MeetingDate, @DurationMinutes, @Location, 
                            @MeetingType, 'scheduled', @CreatedBy, @ClientId, @CreatedAt, @ModifiedAt);
                        SELECT LAST_INSERT_ID();";

                    using var command = new MySqlCommand(insertQuery, connection, transaction);
                    command.Parameters.AddWithValue("@Title", meetingDto.Title);
                    command.Parameters.AddWithValue("@Description", meetingDto.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MeetingDate", meetingDto.MeetingDate);
                    command.Parameters.AddWithValue("@DurationMinutes", meetingDto.DurationMinutes);
                    command.Parameters.AddWithValue("@Location", meetingDto.Location ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@MeetingType", meetingDto.MeetingType);
                    command.Parameters.AddWithValue("@CreatedBy", createdBy);
                    command.Parameters.AddWithValue("@ClientId", meetingDto.ClientId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

                    meetingId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // Add participants if provided
                    if (meetingDto.ParticipantUserIds != null && meetingDto.ParticipantUserIds.Any())
                    {
                        foreach (var userId in meetingDto.ParticipantUserIds)
                        {
                            string participantQuery = @"
                                INSERT INTO MeetingParticipants (MeetingId, UserId, Role, AttendanceStatus, AddedAt)
                                VALUES (@MeetingId, @UserId, @Role, 'pending', @AddedAt)";

                            using var participantCommand = new MySqlCommand(participantQuery, connection, transaction);
                            participantCommand.Parameters.AddWithValue("@MeetingId", meetingId);
                            participantCommand.Parameters.AddWithValue("@UserId", userId);
                            participantCommand.Parameters.AddWithValue("@Role", 
                                userId == createdBy ? "organizer" : "participant");
                            participantCommand.Parameters.AddWithValue("@AddedAt", DateTime.UtcNow);

                            await participantCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplantı oluşturulurken hata oluştu", error = ex.Message });
            }

            // Return the created meeting
            var result = await GetMeetingById(meetingId);
            return CreatedAtAction(nameof(GetMeetingById), new { id = meetingId }, result.Value);
        }
        // PUT: api/Meeting/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, [FromBody] MeetingUpdateDto meetingDto)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if meeting exists
                string checkQuery = "SELECT COUNT(*) FROM Meetings WHERE Id = @Id";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@Id", id);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return NotFound(new { message = "Toplantı bulunamadı" });
                }

                // Build update query dynamically
                var updateFields = new List<string>();
                var parameters = new List<MySqlParameter>();

                if (!string.IsNullOrEmpty(meetingDto.Title))
                {
                    updateFields.Add("Title = @Title");
                    parameters.Add(new MySqlParameter("@Title", meetingDto.Title));
                }

                if (meetingDto.Description != null)
                {
                    updateFields.Add("Description = @Description");
                    parameters.Add(new MySqlParameter("@Description", meetingDto.Description));
                }

                if (meetingDto.MeetingDate.HasValue)
                {
                    updateFields.Add("MeetingDate = @MeetingDate");
                    parameters.Add(new MySqlParameter("@MeetingDate", meetingDto.MeetingDate.Value));
                }

                if (meetingDto.DurationMinutes.HasValue)
                {
                    updateFields.Add("DurationMinutes = @DurationMinutes");
                    parameters.Add(new MySqlParameter("@DurationMinutes", meetingDto.DurationMinutes.Value));
                }

                if (meetingDto.Location != null)
                {
                    updateFields.Add("Location = @Location");
                    parameters.Add(new MySqlParameter("@Location", meetingDto.Location));
                }

                if (!string.IsNullOrEmpty(meetingDto.MeetingType))
                {
                    updateFields.Add("MeetingType = @MeetingType");
                    parameters.Add(new MySqlParameter("@MeetingType", meetingDto.MeetingType));
                }

                if (!string.IsNullOrEmpty(meetingDto.Status))
                {
                    updateFields.Add("Status = @Status");
                    parameters.Add(new MySqlParameter("@Status", meetingDto.Status));
                }

                if (meetingDto.ClientId.HasValue)
                {
                    updateFields.Add("ClientId = @ClientId");
                    parameters.Add(new MySqlParameter("@ClientId", meetingDto.ClientId.Value));
                }

                if (!updateFields.Any())
                {
                    return BadRequest(new { message = "Güncellenecek alan bulunamadı" });
                }

                updateFields.Add("ModifiedAt = @ModifiedAt");
                updateFields.Add("ModifiedBy = @ModifiedBy");
                parameters.Add(new MySqlParameter("@ModifiedAt", DateTime.UtcNow));
                // parameters.Add(new MySqlParameter("@ModifiedBy", GetCurrentUserId()));

                string updateQuery = $"UPDATE Meetings SET {string.Join(", ", updateFields)} WHERE Id = @Id";
                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@Id", id);
                updateCommand.Parameters.AddRange(parameters.ToArray());

                await updateCommand.ExecuteNonQueryAsync();

                return Ok(new { message = "Toplantı başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplantı güncellenirken hata oluştu", error = ex.Message });
            }
        }

        // DELETE: api/Meeting/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Check if meeting exists
                    string checkQuery = "SELECT COUNT(*) FROM Meetings WHERE Id = @Id";
                    using var checkCommand = new MySqlCommand(checkQuery, connection, transaction);
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                    if (!exists)
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = "Toplantı bulunamadı" });
                    }

                    // Delete related records first (due to foreign key constraints)
                    string deleteParticipantsQuery = "DELETE FROM MeetingParticipants WHERE MeetingId = @Id";
                    using var deleteParticipantsCommand = new MySqlCommand(deleteParticipantsQuery, connection, transaction);
                    deleteParticipantsCommand.Parameters.AddWithValue("@Id", id);
                    await deleteParticipantsCommand.ExecuteNonQueryAsync();

                    string deleteDocumentsQuery = "DELETE FROM MeetingDocuments WHERE MeetingId = @Id";
                    using var deleteDocumentsCommand = new MySqlCommand(deleteDocumentsQuery, connection, transaction);
                    deleteDocumentsCommand.Parameters.AddWithValue("@Id", id);
                    await deleteDocumentsCommand.ExecuteNonQueryAsync();

                    string deleteNotesQuery = "DELETE FROM MeetingNotes WHERE MeetingId = @Id";
                    using var deleteNotesCommand = new MySqlCommand(deleteNotesQuery, connection, transaction);
                    deleteNotesCommand.Parameters.AddWithValue("@Id", id);
                    await deleteNotesCommand.ExecuteNonQueryAsync();

                    // Delete the meeting
                    string deleteMeetingQuery = "DELETE FROM Meetings WHERE Id = @Id";
                    using var deleteMeetingCommand = new MySqlCommand(deleteMeetingQuery, connection, transaction);
                    deleteMeetingCommand.Parameters.AddWithValue("@Id", id);
                    await deleteMeetingCommand.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    return Ok(new { message = "Toplantı başarıyla silindi" });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Toplantı silinirken hata oluştu", error = ex.Message });
            }
        }

        // GET: api/Meeting/{id}/documents
        [HttpGet("{id}/documents")]
        public async Task<ActionResult<IEnumerable<MeetingDocument>>> GetMeetingDocuments(int id)
        {
            var documents = new List<MeetingDocument>();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string query = @"
                    SELECT md.*,
                           u.Ad AS UploaderFirstName, u.Soyad AS UploaderLastName
                    FROM MeetingDocuments md
                    LEFT JOIN KullaniciBilgileri u ON md.UploadedBy = u.KullaniciID
                    WHERE md.MeetingId = @MeetingId AND md.IsActive = 1
                    ORDER BY md.UploadDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@MeetingId", id);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var document = new MeetingDocument
                    {
                        Id = reader.GetInt32("Id"),
                        MeetingId = reader.GetInt32("MeetingId"),
                        DocumentName = reader.GetString("DocumentName"),
                        FilePath = reader.GetString("FilePath"),
                        FileSize = reader.IsDBNull("FileSize") ? null : reader.GetInt64("FileSize"),
                        FileType = reader.IsDBNull("FileType") ? null : reader.GetString("FileType"),
                        UploadedBy = reader.IsDBNull("UploadedBy") ? null : reader.GetInt32("UploadedBy"),
                        UploadDate = reader.GetDateTime("UploadDate"),
                        IsActive = reader.GetBoolean("IsActive")
                    };
                    documents.Add(document);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Dökümanlar alınırken hata oluştu", error = ex.Message });
            }

            return Ok(documents);
        }
    }
}