using System.ComponentModel.DataAnnotations;

namespace crmApi.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = null!;
        
        public string? Description { get; set; }
        
        [Required]
        public DateTime MeetingDate { get; set; }
        
        public int DurationMinutes { get; set; } = 60;
        
        [StringLength(255)]
        public string? Location { get; set; }
        
        [StringLength(50)]
        public string MeetingType { get; set; } = "in-person";
        
        [StringLength(50)]
        public string Status { get; set; } = "scheduled";
        
        public int CreatedBy { get; set; }
        
        public int? ClientId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        
        public int? ModifiedBy { get; set; }
        
        public Client? Client { get; set; }
        public ICollection<MeetingParticipant> MeetingParticipants { get; set; } = new List<MeetingParticipant>();
        public ICollection<MeetingDocument> MeetingDocuments { get; set; } = new List<MeetingDocument>();
        public ICollection<MeetingNote> MeetingNotes { get; set; } = new List<MeetingNote>();
    }

    public class MeetingParticipant
    {
        public int Id { get; set; }
        
        public int MeetingId { get; set; }
        
        public int UserId { get; set; }
        
        [StringLength(50)]
        public string Role { get; set; } = "participant";
        
        [StringLength(50)]
        public string AttendanceStatus { get; set; } = "pending";
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public Meeting Meeting { get; set; } = null!;
    }

    public class MeetingDocument
    {
        public int Id { get; set; }
        
        public int MeetingId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string DocumentName { get; set; } = null!;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = null!;
        
        public long? FileSize { get; set; }
        
        [StringLength(100)]
        public string? FileType { get; set; }
        
        public int? UploadedBy { get; set; }
        
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        public Meeting Meeting { get; set; } = null!;
    }

    public class MeetingNote
    {
        public int Id { get; set; }
        
        public int MeetingId { get; set; }
        
        [Required]
        public string NoteContent { get; set; } = null!;
        
        public int CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        
        public Meeting Meeting { get; set; } = null!;
    }

    public class MeetingResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime MeetingDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Location { get; set; }
        public string MeetingType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int CreatedBy { get; set; }
        public int? ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        
        public string? ClientName { get; set; }
        public string? ClientCompanyName { get; set; }
        public string? ClientEmail { get; set; }
        public string? OrganizerName { get; set; }
        public int ParticipantCount { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? ModifiedByUserName { get; set; }
    }

    public class MeetingCreateDto
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = null!;
        
        public string? Description { get; set; }
        
        [Required]
        public DateTime MeetingDate { get; set; }
        
        public int DurationMinutes { get; set; } = 60;
        
        [StringLength(255)]
        public string? Location { get; set; }
        
        [StringLength(50)]
        public string MeetingType { get; set; } = "in-person";
        
        public int? ClientId { get; set; }
        
        public List<int>? ParticipantUserIds { get; set; }
    }

    public class MeetingUpdateDto
    {
        [StringLength(255)]
        public string? Title { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime? MeetingDate { get; set; }
        
        public int? DurationMinutes { get; set; }
        
        [StringLength(255)]
        public string? Location { get; set; }
        
        [StringLength(50)]
        public string? MeetingType { get; set; }
        
        [StringLength(50)]
        public string? Status { get; set; }
        
        public int? ClientId { get; set; }
    }

    public class MeetingDocumentDto
    {
        [Required]
        [StringLength(255)]
        public string DocumentName { get; set; } = null!;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = null!;
        
        public long? FileSize { get; set; }
        
        [StringLength(100)]
        public string? FileType { get; set; }
    }
}