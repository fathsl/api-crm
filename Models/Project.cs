using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

namespace crmApi.Models
{
    public enum ProjectStatus
    {
        NotStarted,
        InProgress,
        OnHold,
        Completed
    }

    [Table("Projects")]
    public class Project
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("Title")]
        public string Title { get; set; }

        [Column("Details")]
        public string Details { get; set; }

        [Column("CreatedByUserId")]
        public int CreatedByUserId { get; set; }

        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedByUserId")]
        public int? UpdatedByUserId { get; set; }

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [Required]
        [Column("Status")]
        public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

        [Column("EstimationTime")]
        public string EstimationTime { get; set; }

        [Column("StartDate")]
        public DateTime? StartDate { get; set; }

        [Column("EndDate")]
        public DateTime? EndDate { get; set; }
    }
}
