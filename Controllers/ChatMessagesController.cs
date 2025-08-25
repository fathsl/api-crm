using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatMessagesController : ControllerBase
{
    private readonly IChatMessageService _chatMessageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatMessagesController(IChatMessageService chatMessageService, IHubContext<ChatHub> hubContext)
    {
        _chatMessageService = chatMessageService;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("discussion/{discussionId}")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(int discussionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        var messages = await _chatMessageService.GetDiscussionMessagesAsync(discussionId, userId, page, pageSize);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = GetCurrentUserId();
        var message = await _chatMessageService.SendMessageAsync(dto, userId);
        
        if (message == null)
            return BadRequest("Cannot send message to this discussion");

        await _chatMessageService.MarkMessageAsReadAsync(message.Id, userId);

        if (dto.ReceiverId.HasValue)
        {
            await _hubContext.Clients.User(dto.ReceiverId.Value.ToString())
                .SendAsync("NewMessage", message);
        }
        else
        {
            await _hubContext.Clients.Group($"Discussion_{dto.DiscussionId}")
                .SendAsync("NewMessage", message);
        }

        return Ok(message);
    }

    [HttpPost("upload-document")]
    public async Task<ActionResult<MessageDto>> UploadDocument([FromForm] IFormFile file, [FromForm] SendMessageDto messageDto)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var userId = GetCurrentUserId();
        var message = await _chatMessageService.SendDocumentMessageAsync(messageDto, file, userId);
        
        if (message == null)
            return BadRequest("Cannot send document to this discussion");

        await _hubContext.Clients.Group($"Discussion_{messageDto.DiscussionId}")
            .SendAsync("NewMessage", message);

        return Ok(message);
    }

    [HttpPost("upload-voice")]
    public async Task<ActionResult<MessageDto>> UploadVoice([FromForm] IFormFile voiceFile, [FromForm] SendMessageDto messageDto, [FromForm] int duration)
    {
        if (voiceFile == null || voiceFile.Length == 0)
            return BadRequest("No voice file uploaded");

        if (duration > 40)
            return BadRequest("Voice message cannot exceed 40 seconds");

        var userId = GetCurrentUserId();
        var message = await _chatMessageService.SendVoiceMessageAsync(messageDto, voiceFile, duration, userId);
        
        if (message == null)
            return BadRequest("Cannot send voice message to this discussion");

        await _hubContext.Clients.Group($"Discussion_{messageDto.DiscussionId}")
            .SendAsync("NewMessage", message);

        return Ok(message);
    }

    [HttpPost("{messageId}/mark-read")]
    public async Task<ActionResult> MarkAsRead(int messageId)
    {
        var userId = GetCurrentUserId();
        await _chatMessageService.MarkMessageAsReadAsync(messageId, userId);
        return Ok();
    }

    [HttpPut("{messageId}")]
    public async Task<ActionResult<MessageDto>> EditMessage(int messageId, [FromBody] string newContent)
    {
        var userId = GetCurrentUserId();
        var message = await _chatMessageService.EditMessageAsync(messageId, newContent, userId);
        
        if (message == null)
            return NotFound();

        await _hubContext.Clients.Group($"Discussion_{message.DiscussionId}")
            .SendAsync("MessageEdited", message);

        return Ok(message);
    }

    [HttpGet("download-document/{documentId}")]
    public async Task<ActionResult> DownloadDocument(int documentId)
    {
        var userId = GetCurrentUserId();
        var document = await _chatMessageService.GetDocumentAsync(documentId, userId);
        
        if (document == null)
            return NotFound();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);
        return File(fileBytes, document.MimeType, document.OriginalFileName);
    }

    [HttpGet("download-voice/{voiceId}")]
    public async Task<ActionResult> DownloadVoice(int voiceId)
    {
        var userId = GetCurrentUserId();
        var voice = await _chatMessageService.GetVoiceMessageAsync(voiceId, userId);
        
        if (voice == null)
            return NotFound();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(voice.FilePath);
        return File(fileBytes, "audio/wav", voice.FileName);
    }
}

[Authorize]
public class ChatHub : Hub
{
    private readonly IDiscussionService _discussionService;
    private readonly IChatMessageService _chatMessageService;

    public ChatHub(IDiscussionService discussionService, IChatMessageService chatMessageService)
    {
        _discussionService = discussionService;
        _chatMessageService = chatMessageService;
    }

    private int GetCurrentUserId() => int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        
        var discussions = await _discussionService.GetUserDiscussionsAsync(userId);
        foreach (var discussion in discussions)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Discussion_{discussion.Id}");
        }

        await Clients.All.SendAsync("UserOnline", userId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = GetCurrentUserId();
        
        await Clients.All.SendAsync("UserOffline", userId);
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinDiscussion(int discussionId)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _discussionService.UserHasAccessToDiscussionAsync(discussionId, userId);
        
        if (hasAccess)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Discussion_{discussionId}");
            await Clients.Group($"Discussion_{discussionId}").SendAsync("UserJoinedDiscussion", userId);
        }
    }

    public async Task LeaveDiscussion(int discussionId)
    {
        var userId = GetCurrentUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Discussion_{discussionId}");
        await Clients.Group($"Discussion_{discussionId}").SendAsync("UserLeftDiscussion", userId);
    }

    public async Task StartTyping(int discussionId)
    {
        var userId = GetCurrentUserId();
        await Clients.Group($"Discussion_{discussionId}").SendAsync("UserStartedTyping", userId);
    }

    public async Task StopTyping(int discussionId)
    {
        var userId = GetCurrentUserId();
        await Clients.Group($"Discussion_{discussionId}").SendAsync("UserStoppedTyping", userId);
    }
}

public interface IDiscussionService
{
    Task<List<DiscussionDto>> GetUserDiscussionsAsync(int userId);
    Task<DiscussionDto> GetDiscussionAsync(int discussionId, int userId);
    Task<DiscussionDto> CreateDiscussionAsync(CreateDiscussionDto dto, int createdByUserId);
    Task<DiscussionDto> UpdateDiscussionAsync(int discussionId, UpdateDiscussionDto dto, int updatedByUserId);
    Task<bool> AddParticipantAsync(int discussionId, AddParticipantDto dto, int addedByUserId);
    Task<bool> RemoveParticipantAsync(int discussionId, int userId, int removedByUserId);
    Task<bool> LinkTaskToDiscussionAsync(int discussionId, int taskId, int linkedByUserId);
    Task<ParticipantDto> GetParticipantAsync(int discussionId, int userId);
    Task<TaskDto> GetDiscussionTaskAsync(int discussionId, int taskId);
    Task<bool> UserHasAccessToDiscussionAsync(int discussionId, int userId);
}

public interface IChatMessageService
{
    Task<List<MessageDto>> GetDiscussionMessagesAsync(int discussionId, int userId, int page, int pageSize);
    Task<MessageDto> SendMessageAsync(SendMessageDto dto, int senderId);
    Task<MessageDto> SendDocumentMessageAsync(SendMessageDto dto, IFormFile file, int senderId);
    Task<MessageDto> SendVoiceMessageAsync(SendMessageDto dto, IFormFile voiceFile, int duration, int senderId);
    Task<MessageDto> EditMessageAsync(int messageId, string newContent, int userId);
    Task MarkMessageAsReadAsync(int messageId, int userId);
    Task<MessageDocumentDto> GetDocumentAsync(int documentId, int userId);
    Task<VoiceMessageDto> GetVoiceMessageAsync(int voiceId, int userId);
}