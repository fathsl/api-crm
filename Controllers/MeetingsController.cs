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
                           mbu.Ad AS ModifiedByFirstName, mbu.Soyad AS ModifiedByLastName,
                           COUNT(DISTINCT mp.user_id) AS ParticipantCount
                    FROM Meetings m
                    LEFT JOIN Clients c ON m.ClientId = c.Id
                    LEFT JOIN KullaniciBilgileri cu ON m.CreatedBy = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri cbu ON m.CreatedBy = cbu.KullaniciID
                    LEFT JOIN KullaniciBilgileri mbu ON m.ModifiedBy = mbu.KullaniciID
                    LEFT JOIN MeetingParticipants mp ON m.meeting_id = mp.meeting_id
                    GROUP BY m.meeting_id, c.Id, cu.KullaniciID, cbu.KullaniciID, mbu.KullaniciID
                    ORDER BY m.MeetingDate DESC";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var meeting = new MeetingResponseDto
                    {
                        Id = Convert.ToInt32(reader["meeting_id"]),
                        Title = reader["Title"].ToString(),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        MeetingDate = reader.GetDateTime("MeetingDate"),
                        DurationMinutes = reader.GetInt32("DurationMinutes"),
                        Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString("Location"),
                        MeetingType = reader.GetString("MeetingType"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        ClientId = reader.IsDBNull(reader.GetOrdinal("ClientId")) ? null : reader.GetInt32("ClientId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedAt = reader.GetDateTime("ModifiedAt"),
                        ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? 0 : reader.GetInt32("ModifiedBy"),
                        ParticipantCount = reader.GetInt32("ParticipantCount"),
                    };
                    meetings.Add(meeting);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving meetings", error = ex.Message });
            }

            return Ok(meetings);
        }

        // GET: api/Meeting/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MeetingDetailDto>> GetMeeting(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string meetingQuery = @"
                    SELECT m.*,
                           c.First_name AS ClientFirstName, c.Last_name AS ClientLastName,
                           c.Email AS ClientEmail, c.Details AS ClientCompanyName,
                           cu.Ad AS OrganizerFirstName, cu.Soyad AS OrganizerLastName,
                           cbu.Ad AS CreatedByFirstName, cbu.Soyad AS CreatedByLastName,
                           mbu.Ad AS ModifiedByFirstName, mbu.Soyad AS ModifiedByLastName
                    FROM Meetings m
                    LEFT JOIN Clients c ON m.ClientId = c.Id
                    LEFT JOIN KullaniciBilgileri cu ON m.CreatedBy = cu.KullaniciID
                    LEFT JOIN KullaniciBilgileri cbu ON m.CreatedBy = cbu.KullaniciID
                    LEFT JOIN KullaniciBilgileri mbu ON m.ModifiedBy = mbu.KullaniciID
                    WHERE m.meeting_id = @Id";

                using var command = new MySqlCommand(meetingQuery, connection);
                command.Parameters.AddWithValue("@Id", id);

                MeetingDetailDto? meeting = null;

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    meeting = new MeetingDetailDto
                    {
                        Id = Convert.ToInt32(reader["meeting_id"]),
                        Title = reader["Title"].ToString(),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                        MeetingDate = reader.GetDateTime("MeetingDate"),
                        DurationMinutes = reader.GetInt32("DurationMinutes"),
                        Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString("Location"),
                        MeetingType = reader.GetString("MeetingType"),
                        Status = reader.GetString("Status"),
                        CreatedBy = reader.GetInt32("CreatedBy"),
                        ClientId = reader.IsDBNull(reader.GetOrdinal("ClientId")) ? null : reader.GetInt32("ClientId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        ModifiedAt = reader.GetDateTime("ModifiedAt"),
                        ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? 0 : reader.GetInt32("ModifiedBy"),
                        Participants = new List<MeetingParticipantDto>()
                    };
                }

                if (meeting == null)
                {
                    return NotFound(new { message = "Meeting not found" });
                }

                string participantsQuery = @"
                    SELECT mp.*, k.Ad AS FirstName, k.Soyad AS LastName, k.Email
                    FROM MeetingParticipants mp
                    INNER JOIN KullaniciBilgileri k ON mp.user_id = k.KullaniciID
                    WHERE mp.meeting_id = @MeetingId";

                using var participantCommand = new MySqlCommand(participantsQuery, connection);
                participantCommand.Parameters.AddWithValue("@MeetingId", id);

                using var participantReader = await participantCommand.ExecuteReaderAsync();
                while (await participantReader.ReadAsync())
                {
                    meeting.Participants.Add(new MeetingParticipantDto
                    {
                        UserId = participantReader.GetInt32("user_id"),
                        UserName = $"{participantReader.GetString("FirstName")} {participantReader.GetString("LastName")}",
                        Email = participantReader.IsDBNull(participantReader.GetOrdinal("Email")) ? null : participantReader.GetString("Email")
                    });
                }

                return Ok(meeting);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving meeting", error = ex.Message });
            }
        }

        // POST: api/Meeting
        [HttpPost]
        public async Task<ActionResult<MeetingResponseDto>> CreateMeeting([FromBody] CreateMeetingDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string insertMeetingQuery = @"
                        INSERT INTO Meetings (Title, Description, MeetingDate, DurationMinutes, Location, 
                                            MeetingType, Status, CreatedBy, ClientId, CreatedAt, ModifiedAt)
                        VALUES (@Title, @Description, @MeetingDate, @DurationMinutes, @Location, 
                                @MeetingType, @Status, @CreatedBy, @ClientId, @CreatedAt, @ModifiedAt);
                        SELECT LAST_INSERT_ID();";

                    int meetingId;
                    using (var command = new MySqlCommand(insertMeetingQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Title", createDto.Title);
                        command.Parameters.AddWithValue("@Description", createDto.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MeetingDate", createDto.MeetingDate);
                        command.Parameters.AddWithValue("@DurationMinutes", createDto.DurationMinutes);
                        command.Parameters.AddWithValue("@Location", createDto.Location ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MeetingType", createDto.MeetingType);
                        command.Parameters.AddWithValue("@Status", createDto.Status);
                        command.Parameters.AddWithValue("@CreatedBy", createDto.CreatedBy);
                        command.Parameters.AddWithValue("@ClientId", createDto.ClientId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);

                        meetingId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    }

                    if (createDto.ParticipantIds != null && createDto.ParticipantIds.Any())
                    {
                        string insertParticipantQuery = @"
                            INSERT INTO MeetingParticipants (meeting_id, user_id)
                            VALUES (@MeetingId, @UserId)";

                        foreach (var participantId in createDto.ParticipantIds)
                        {
                            using var participantCommand = new MySqlCommand(insertParticipantQuery, connection, transaction);
                            participantCommand.Parameters.AddWithValue("@MeetingId", meetingId);
                            participantCommand.Parameters.AddWithValue("@UserId", participantId);
                            await participantCommand.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    var createdMeeting = await GetMeetingById(meetingId, connection);
                    return CreatedAtAction(nameof(GetMeeting), new { id = meetingId }, createdMeeting);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating meeting", error = ex.Message });
            }
        }

        // PUT: api/Meeting/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMeeting(int id, [FromBody] UpdateMeetingDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    string checkQuery = "SELECT COUNT(*) FROM Meetings WHERE meeting_id = @Id";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@Id", id);
                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            return NotFound(new { message = "Meeting not found" });
                        }
                    }

                    string updateQuery = @"
                        UPDATE Meetings 
                        SET Title = @Title,
                            Description = @Description,
                            MeetingDate = @MeetingDate,
                            DurationMinutes = @DurationMinutes,
                            Location = @Location,
                            MeetingType = @MeetingType,
                            Status = @Status,
                            ClientId = @ClientId,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE meeting_id = @Id";

                    using (var command = new MySqlCommand(updateQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Title", updateDto.Title);
                        command.Parameters.AddWithValue("@Description", updateDto.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MeetingDate", updateDto.MeetingDate);
                        command.Parameters.AddWithValue("@DurationMinutes", updateDto.DurationMinutes);
                        command.Parameters.AddWithValue("@Location", updateDto.Location ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MeetingType", updateDto.MeetingType);
                        command.Parameters.AddWithValue("@Status", updateDto.Status);
                        command.Parameters.AddWithValue("@ClientId", updateDto.ClientId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ModifiedAt", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@ModifiedBy", updateDto.ModifiedBy);

                        await command.ExecuteNonQueryAsync();
                    }

                    if (updateDto.ParticipantIds != null)
                    {
                        string deleteParticipantsQuery = "DELETE FROM MeetingParticipants WHERE meeting_id = @MeetingId";
                        using (var deleteCommand = new MySqlCommand(deleteParticipantsQuery, connection, transaction))
                        {
                            deleteCommand.Parameters.AddWithValue("@MeetingId", id);
                            await deleteCommand.ExecuteNonQueryAsync();
                        }

                        if (updateDto.ParticipantIds.Any())
                        {
                            string insertParticipantQuery = @"
                                INSERT INTO MeetingParticipants (meeting_id, user_id)
                                VALUES (@MeetingId, @UserId)";

                            foreach (var participantId in updateDto.ParticipantIds)
                            {
                                using var participantCommand = new MySqlCommand(insertParticipantQuery, connection, transaction);
                                participantCommand.Parameters.AddWithValue("@MeetingId", id);
                                participantCommand.Parameters.AddWithValue("@UserId", participantId);
                                await participantCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating meeting", error = ex.Message });
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
                    string checkQuery = "SELECT COUNT(*) FROM Meetings WHERE meeting_id = @Id";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@Id", id);
                        var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            return NotFound(new { message = "Meeting not found" });
                        }
                    }

                    string deleteParticipantsQuery = "DELETE FROM MeetingParticipants WHERE meeting_id = @MeetingId";
                    using (var deleteParticipantsCommand = new MySqlCommand(deleteParticipantsQuery, connection, transaction))
                    {
                        deleteParticipantsCommand.Parameters.AddWithValue("@MeetingId", id);
                        await deleteParticipantsCommand.ExecuteNonQueryAsync();
                    }

                    string deleteNotesQuery = "DELETE FROM MeetingNotes WHERE meeting_id = @MeetingId";
                    using (var deleteNotesCommand = new MySqlCommand(deleteNotesQuery, connection, transaction))
                    {
                        deleteNotesCommand.Parameters.AddWithValue("@MeetingId", id);
                        await deleteNotesCommand.ExecuteNonQueryAsync();
                    }

                    string deleteDocumentsQuery = "DELETE FROM MeetingDocuments WHERE meeting_id = @MeetingId";
                    using (var deleteDocumentsCommand = new MySqlCommand(deleteDocumentsQuery, connection, transaction))
                    {
                        deleteDocumentsCommand.Parameters.AddWithValue("@MeetingId", id);
                        await deleteDocumentsCommand.ExecuteNonQueryAsync();
                    }

                    string deleteMeetingQuery = "DELETE FROM Meetings WHERE meeting_id = @Id";
                    using (var deleteMeetingCommand = new MySqlCommand(deleteMeetingQuery, connection, transaction))
                    {
                        deleteMeetingCommand.Parameters.AddWithValue("@Id", id);
                        await deleteMeetingCommand.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    return NoContent();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting meeting", error = ex.Message });
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
        
        private async Task<MeetingResponseDto> GetMeetingById(int id, MySqlConnection connection)
        {
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
                LEFT JOIN MeetingParticipants mp ON m.meeting_id = mp.meeting_id
                WHERE m.meeting_id = @Id
                GROUP BY m.meeting_id, c.Id, cu.KullaniciID, cbu.KullaniciID";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new MeetingResponseDto
                {
                    Id = Convert.ToInt32(reader["meeting_id"]),
                    Title = reader["Title"].ToString(),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                    MeetingDate = reader.GetDateTime("MeetingDate"),
                    DurationMinutes = reader.GetInt32("DurationMinutes"),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString("Location"),
                    MeetingType = reader.GetString("MeetingType"),
                    Status = reader.GetString("Status"),
                    CreatedBy = reader.GetInt32("CreatedBy"),
                    ModifiedBy = reader.GetInt32("ModifiedBy"),
                    ClientId = reader.IsDBNull(reader.GetOrdinal("ClientId")) ? null : reader.GetInt32("ClientId"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    ModifiedAt = reader.GetDateTime("ModifiedAt"),
                    ParticipantCount = reader.GetInt32("ParticipantCount")
                };
            }

            return null;
        }
    }
}