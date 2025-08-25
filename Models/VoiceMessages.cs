using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("VoiceMessages")]
public class VoiceMessage
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("MessageId")]
    public int MessageId { get; set; }

    [Required]
    [StringLength(255)]
    [Column("FileName")]
    public string FileName { get; set; }

    [Required]
    [Column("Duration")]
    public int Duration { get; set; }

    [Required]
    [StringLength(500)]
    [Column("FilePath")]
    public string FilePath { get; set; }

    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [NotMapped]
    public ChatMessage Message { get; set; }
}

public class VoiceMessageDto
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedAt { get; set; }
}
