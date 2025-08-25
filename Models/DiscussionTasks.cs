using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

[Table("DiscussionTasks")]
public class DiscussionTask
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("DiscussionId")]
    public int DiscussionId { get; set; }

    [Required]
    [Column("TaskId")]
    public int TaskId { get; set; }

    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("CreatedByUserId")]
    public int CreatedByUserId { get; set; }

    [NotMapped]
    public Discussion Discussion { get; set; }

    [NotMapped]
    public Task Task { get; set; }

    [NotMapped]
    public User CreatedByUser { get; set; }
}