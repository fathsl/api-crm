using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

public enum DiscussionStatus
{
    Active = 0,
    Closed = 1,
    Archived = 2
}

[Table("Discussions")]
public class Discussion
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Column("Title")]
    public string Title { get; set; }

    [Column("Description")]
    public string Description { get; set; }

    [Required]
    [Column("Status")]
    public DiscussionStatus Status { get; set; } = DiscussionStatus.Active;

    [Required]
    [Column("CreatedByUserId")]
    public int CreatedByUserId { get; set; }

    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedByUserId")]
    public int? UpdatedByUserId { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [NotMapped]
    public User CreatedByUser { get; set; }

    [NotMapped]
    public User UpdatedByUser { get; set; }

    [NotMapped]
    public List<DiscussionParticipant> Participants { get; set; } = new List<DiscussionParticipant>();

    [NotMapped]
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    [NotMapped]
    public List<DiscussionTask> DiscussionTasks { get; set; } = new List<DiscussionTask>();
}

public class CreateDiscussionDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    public string Description { get; set; }

    public List<int> ParticipantUserIds { get; set; } = new List<int>();
}

public class UpdateDiscussionDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    public string Description { get; set; }

    public DiscussionStatus Status { get; set; }
}

public class DiscussionDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DiscussionStatus Status { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string UpdatedByUserName { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
    public List<TaskDto> Tasks { get; set; } = new List<TaskDto>();
    public int UnreadMessagesCount { get; set; }
    public MessageDto LastMessage { get; set; }
}
