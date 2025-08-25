using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MessageDocuments")]
public class MessageDocument
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
    [StringLength(255)]
    [Column("OriginalFileName")]
    public string OriginalFileName { get; set; }

    [Required]
    [Column("FileSize")]
    public long FileSize { get; set; }

    [Required]
    [StringLength(100)]
    [Column("MimeType")]
    public string MimeType { get; set; }

    [Required]
    [StringLength(500)]
    [Column("FilePath")]
    public string FilePath { get; set; }

    [Required]
    [Column("UploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public ChatMessage Message { get; set; }
}
