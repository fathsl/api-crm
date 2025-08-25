using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

[Table("MessageReadStatus")]
public class MessageReadStatus
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("MessageId")]
    public int MessageId { get; set; }

    [Required]
    [Column("UserId")]
    public int UserId { get; set; }

    [Required]
    [Column("ReadAt")]
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public ChatMessage Message { get; set; }

    [NotMapped]
    public User User { get; set; }
}