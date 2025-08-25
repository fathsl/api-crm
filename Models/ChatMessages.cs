using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

public enum MessageType
{
    Text = 0,
    Document = 1,
    Voice = 2,
    Task = 3,
    System = 4
}

[Table("ChatMessages")]
public class ChatMessage
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("DiscussionId")]
    public int DiscussionId { get; set; }

    [Required]
    [Column("SenderId")]
    public int SenderId { get; set; }

    [Column("ReceiverId")]
    public int? ReceiverId { get; set; }

    [Required]
    [Column("Content")]
    public string Content { get; set; }

    [Required]
    [Column("MessageType")]
    public MessageType MessageType { get; set; } = MessageType.Text;

    [Required]
    [Column("IsEdited")]
    public bool IsEdited { get; set; } = false;

    [Column("EditedAt")]
    public DateTime? EditedAt { get; set; }

    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [NotMapped]
    public Discussion Discussion { get; set; }

    [NotMapped]
    public User Sender { get; set; }

    [NotMapped]
    public User Receiver { get; set; }

    [NotMapped]
    public List<MessageDocument> Documents { get; set; } = new List<MessageDocument>();

    [NotMapped]
    public VoiceMessage VoiceMessage { get; set; }

    [NotMapped]
    public List<MessageReadStatus> ReadStatus { get; set; } = new List<MessageReadStatus>();
}

public class SendMessageDto
{
    [Required]
    public int DiscussionId { get; set; }

    public int? ReceiverId { get; set; }

    [Required]
    public string Content { get; set; }

    public MessageType MessageType { get; set; } = MessageType.Text;
}

public class MessageDto
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; }
    public int? ReceiverId { get; set; }
    public string ReceiverName { get; set; }
    public string Content { get; set; }
    public MessageType MessageType { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MessageDocumentDto> Documents { get; set; } = new List<MessageDocumentDto>();
    public VoiceMessageDto VoiceMessage { get; set; }
    public bool IsRead { get; set; }
}

public class MessageDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
    public long FileSize { get; set; }
    public string FilePath { get; set; }
    public string MimeType { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class ParticipantDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsOnline { get; set; }
}

public class AddParticipantDto
{
    [Required]
    public int UserId { get; set; }

    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
}