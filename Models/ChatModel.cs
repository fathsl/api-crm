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
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

    public class SendMessageRequest
    {
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string Content { get; set; }
        public byte MessageType { get; set; } = 0;
    }

    public class CreateDiscussionRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int CreatedByUserId { get; set; }
        public List<int> ParticipantUserIds { get; set; } = new List<int>();
    }

    public class MessageResponse
    {
        public int Id { get; set; }
        public int DiscussionId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public int? ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Content { get; set; }
        public byte MessageType { get; set; }
        public bool IsEdited { get; set; }
        public int? DocumentId { get; set; }
        public string FileReference { get; set; }
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
    }
}