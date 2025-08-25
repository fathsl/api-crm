using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

public enum ParticipantRole
{
    Participant = 0,
    Moderator = 1,
    Observer = 2
}

[Table("DiscussionParticipants")]
public class DiscussionParticipant
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("DiscussionId")]
    public int DiscussionId { get; set; }

    [Required]
    [Column("UserId")]
    public int UserId { get; set; }

    [Required]
    [Column("Role")]
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;

    [Required]
    [Column("JoinedAt")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("JoinedByUserId")]
    public int JoinedByUserId { get; set; }

    // Navigation properties
    [NotMapped]
    public Discussion Discussion { get; set; }

    [NotMapped]
    public User User { get; set; }

    [NotMapped]
    public User JoinedByUser { get; set; }
}
