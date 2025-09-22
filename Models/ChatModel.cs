using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using crmApi.Models.crmApi.Models;

namespace crmApi.Models
{
    public class Discussion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public byte Status { get; set; } = 0;

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? UpdatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class DiscussionParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscussionId { get; set; }

        [Required]
        public int UserId { get; set; }

        public byte Role { get; set; } = 0;

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public int JoinedByUserId { get; set; }
    }

    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscussionId { get; set; }

        [Required]
        public int SenderId { get; set; }

        public int? ReceiverId { get; set; }

        [Required]
        public string Content { get; set; }

        public byte MessageType { get; set; } = 0;

        public bool IsEdited { get; set; } = false;

        public DateTime? EditedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class MessageDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; }

        public long FileSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string MimeType { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        public string BucketName { get; set; }

        public string FileKey { get; set; }
        public string? IDriveUrl { get; set; }       
        public int? Duration { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

    public class SendMessageRequest
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public byte MessageType { get; set; } = 1;
    }

    public class SendMessageWithFileRequest
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public string? FileReference { get; set; }
        public string? BucketName { get; set; }
        public string? FileKey { get; set; }
        public string? IdriveUrl { get; set; }
        public byte MessageType { get; set; } = 2;
    }

    public class SendVoiceMessageRequest
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string FileReference { get; set; }
        public string BucketName { get; set; }
        public string FileKey { get; set; }
        public string? IdriveUrl { get; set; }
        public int? Duration { get; set; }
        public byte MessageType { get; set; } = 3;
    }


    public class CreateDiscussionRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CreatedByUserId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public byte Status { get; set; }
        public List<int> ParticipantUserIds { get; set; } = new List<int>();
    }

    public class UpdateDiscussionStatusRequest
    {
        public byte Status { get; set; }
        public int UpdatedByUserId { get; set; }
    }


    public class MessageResponse
    {
        public int Id { get; set; }
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public string? SenderName { get; set; }
        public int? ReceiverId { get; set; }
        public string? ReceiverName { get; set; }
        public string Content { get; set; }
        public byte MessageType { get; set; }
        public bool IsEdited { get; set; }
        public int? DocumentId { get; set; }
        public bool HasFile { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string MimeType { get; set; }
        public string FileReference { get; set; }
        public int? Duration { get; set; }
        public int? TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public string IDriveUrl { get; set; }
        public string AudioUrl { get; set; }
        public string FileUrl { get; set; }
        public string BucketName { get; set; }
        public string FileKey { get; set; }
        public int SortOrder { get; set; }
        public TaskStatus? TaskStatus { get; set; }
        public TaskPriority? TaskPriority { get; set; }
        public DateTime? DueDate { get; set; }
        public string? EstimatedTime { get; set; }
        public List<int> AssignedUserIds { get; set; }
        public List<int> ClientIds { get; set; }
        public List<int> ProjectIds { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DiscussionResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public byte Status { get; set; }
        [Column("KullanciId")]
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<User> Participants { get; set; } = new List<User>();
        public MessageResponse LastMessage { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string CreatorName { get; set; }
        

        public TaskStatus? LastTaskStatus { get; set; }
    }

    public class AssignDiscussionRequest
    {
        public int DiscussionId { get; set; }
        public List<int> UserIds { get; set; } = new List<int>();
        public int AssignedByUserId { get; set; }
    }

    public class TaskMessageResponse : MessageResponse
    {
        public int? TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public TaskStatus? TaskStatus { get; set; }
        public TaskPriority? TaskPriority { get; set; }
        public DateTime? DueDate { get; set; }
        public string EstimatedTime { get; set; }
        public List<int> AssignedUserIds { get; set; }
    }

    public class UpdateTaskStatusInChatDto
    {
        public TaskStatus Status { get; set; }
        public int UpdatedByUserId { get; set; }
    }

    public enum MessageType
    {
        Text = 1,
        File = 2,
        Voice = 3,
        Task = 4
    }


    public class ChatMessageWithTaskDto
    {
        public int Id { get; set; }
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public MessageType MessageType { get; set; }
        public string FileReference { get; set; }
        public int? TaskId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SenderName { get; set; }

        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public TaskStatus? TaskStatus { get; set; }
        public TaskPriority? TaskPriority { get; set; }
        public DateTime? DueDate { get; set; }
        public string EstimatedTime { get; set; }
    }

    public class CreateTaskMessageWithFileDto
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public string? TaskTitle { get; set; }
        public string? TaskDescription { get; set; }
        public string TaskStatus { get; set; }
        public string TaskPriority { get; set; }
        public List<int>? ClientIds { get; set; } = new List<int>();
        public List<int>? ProjectIds { get; set; } = new List<int>();
        public DateTime? DueDate { get; set; }
        public string? EstimatedTime { get; set; }
        public List<int> AssignedUserIds { get; set; } = new List<int>();
    }

    public class CreateTaskMessageWithVoiceDto
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public string? TaskTitle { get; set; }
        public string? TaskDescription { get; set; }
        public string TaskStatus { get; set; }
        public string TaskPriority { get; set; }
        public List<int>? ClientIds { get; set; } = new List<int>();
        public List<int>? ProjectIds { get; set; } = new List<int>();
        public DateTime? DueDate { get; set; }
        public string? EstimatedTime { get; set; }
        public List<int> AssignedUserIds { get; set; } = new List<int>();
        public int? Duration { get; set; }
    }

    public class CreateTaskMessageDto
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public int MessageType { get; set; }
        public string? TaskTitle { get; set; }
        public string? TaskDescription { get; set; }
        public string TaskStatus { get; set; }
        public string TaskPriority { get; set; }
        public List<int>? ClientIds { get; set; } = new List<int>();
        public List<int>? ProjectIds { get; set; } = new List<int>();
        public DateTime? DueDate { get; set; }
        public int? EstimatedTime { get; set; }
        public int? SortOrder { get; set; }
        public List<int>? AssignedUserIds { get; set; } = new List<int>();
    }

    public class TaskDataResponse
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string TaskDescription { get; set; }
        public string TaskStatus { get; set; }
        public string TaskPriority { get; set; }
        public string Content { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string FileUrl { get; set; }
        public string VoiceRecordUrl { get; set; }
        public string DueDate { get; set; }
        public string EstimatedTime { get; set; }
        public List<string> AssignedUsers { get; set; }
        public List<string> Clients { get; set; }
        public List<string> Projects { get; set; }
    }

    public class PresignedUrlRequest
    {
        public string BucketName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }

    public class DiscussionTasksAndMediaResponseDto
    {
        public int DiscussionId { get; set; }
        public List<TaskWithMediaDto> Tasks { get; set; } = new();
        public List<ChatDocumentDto> Documents { get; set; } = new();
        public List<ChatVoiceRecordDto> VoiceRecords { get; set; } = new();
    }


    public class TaskWithMediaDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public string? EstimatedTime { get; set; }
        public int SortOrder { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<int> ClientIds { get; set; }
        public List<int> ProjectIds { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? DiscussionId { get; set; }
        public List<UserResponseDto> AssignedUsers { get; set; } = new();
    }


    public class ChatDocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UploadedByUserId { get; set; }
        public string? ContentType { get; set; }
    }


    public class ChatVoiceRecordDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public double? Duration { get; set; }
        public DateTime RecordedAt { get; set; }
        public int RecordedByUserId { get; set; }
        public long? FileSize { get; set; }
    }
    
    

}