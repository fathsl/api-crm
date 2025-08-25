using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models;
using crmApi.Models.crmApi.Models;

namespace crmApi.Models
{
    public enum TaskStatus
    {
        Backlog,
        Todo,
        InProgress,
        InReview,
        Done
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    [Table("Tasks")]
    public class Task
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
        public TaskStatus Status { get; set; } = TaskStatus.Backlog;

        [Required]
        [Column("Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Column("DueDate")]
        public DateTime? DueDate { get; set; }

        [StringLength(50)]
        [Column("EstimatedTime")]
        public string EstimatedTime { get; set; }

        [Column("SortOrder")]
        public int SortOrder { get; set; } = 0;

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

        [NotMapped]
        public User CreatedByUser { get; set; }

        [NotMapped]
        public User UpdatedByUser { get; set; }

        [NotMapped]
        public List<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();

        [NotMapped]
        public List<User> AssignedUsers { get; set; } = new List<User>();
    }

    [Table("TaskAssignments")]
    public class TaskAssignment
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("TaskId")]
        public int TaskId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("AssignedAt")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public Task Task { get; set; }

        [NotMapped]
        public User User { get; set; }
    }

    public class CreateTaskDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Backlog;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        [StringLength(50)]
        public string EstimatedTime { get; set; }

        public int SortOrder { get; set; } = 0;

        public List<int> AssignedUserIds { get; set; } = new List<int>();
    }

    public class UpdateTaskDto
    {
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public TaskStatus? Status { get; set; }

        public TaskPriority? Priority { get; set; }

        public DateTime? DueDate { get; set; }

        [StringLength(50)]
        public string EstimatedTime { get; set; }

        public int? SortOrder { get; set; }

        public List<int> AssignedUserIds { get; set; }
    }

    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public string EstimatedTime { get; set; }
        public int SortOrder { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByUserName { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<UserResponseDto> AssignedUsers { get; set; } = new List<UserResponseDto>();
    }

    public class UpdateTaskStatusDto
    {
        [Required]
        public TaskStatus Status { get; set; }

        public int SortOrder { get; set; }
    }

    public class BulkUpdateTaskOrderDto
    {
        public List<TaskOrderDto> Tasks { get; set; } = new List<TaskOrderDto>();
    }

    public class TaskOrderDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public TaskStatus Status { get; set; }
    }

    public class TaskAssignmentDto
    {
        public int TaskId { get; set; }
        public List<int> UserIds { get; set; } = new List<int>();
    }
    
    public class TaskAssignmentResponseDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public DateTime AssignedAt { get; set; }

        public string TaskTitle { get; set; }
        public string UserName { get; set; }
        public string UserUsername { get; set; }
        public string UserEmail { get; set; }
    }
}

public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string EstimatedTime { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public string UpdatedByUserName { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<UserResponseDto> AssignedUsers { get; set; } = new List<UserResponseDto>();
}