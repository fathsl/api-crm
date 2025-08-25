using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscussionsController : ControllerBase
{
    private readonly IDiscussionService _discussionService;
    private readonly IHubContext<ChatHub> _hubContext;

    public DiscussionsController(IDiscussionService discussionService, IHubContext<ChatHub> hubContext)
    {
        _discussionService = discussionService;
        _hubContext = hubContext;
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<ActionResult<List<DiscussionDto>>> GetDiscussions()
    {
        var userId = GetCurrentUserId();
        var discussions = await _discussionService.GetUserDiscussionsAsync(userId);
        return Ok(discussions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DiscussionDto>> GetDiscussion(int id)
    {
        var userId = GetCurrentUserId();
        var discussion = await _discussionService.GetDiscussionAsync(id, userId);
        
        if (discussion == null)
            return NotFound();

        return Ok(discussion);
    }

    [HttpPost]
    public async Task<ActionResult<DiscussionDto>> CreateDiscussion([FromBody] CreateDiscussionDto dto)
    {
        var userId = GetCurrentUserId();
        var discussion = await _discussionService.CreateDiscussionAsync(dto, userId);
        
        foreach (var participantId in dto.ParticipantUserIds)
        {
            await _hubContext.Clients.User(participantId.ToString())
                .SendAsync("NewDiscussion", discussion);
        }

        return CreatedAtAction(nameof(GetDiscussion), new { id = discussion.Id }, discussion);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DiscussionDto>> UpdateDiscussion(int id, [FromBody] UpdateDiscussionDto dto)
    {
        var userId = GetCurrentUserId();
        var discussion = await _discussionService.UpdateDiscussionAsync(id, dto, userId);
        
        if (discussion == null)
            return NotFound();

        await _hubContext.Clients.Group($"Discussion_{id}")
            .SendAsync("DiscussionUpdated", discussion);

        return Ok(discussion);
    }

    [HttpPost("{id}/participants")]
    public async Task<ActionResult> AddParticipant(int id, [FromBody] AddParticipantDto dto)
    {
        var userId = GetCurrentUserId();
        var success = await _discussionService.AddParticipantAsync(id, dto, userId);
        
        if (!success)
            return NotFound();

        var participant = await _discussionService.GetParticipantAsync(id, dto.UserId);
        
        await _hubContext.Clients.Group($"Discussion_{id}")
            .SendAsync("ParticipantAdded", participant);

        return Ok();
    }

    [HttpDelete("{id}/participants/{userId}")]
    public async Task<ActionResult> RemoveParticipant(int id, int userId)
    {
        var currentUserId = GetCurrentUserId();
        var success = await _discussionService.RemoveParticipantAsync(id, userId, currentUserId);
        
        if (!success)
            return NotFound();

        await _hubContext.Clients.Group($"Discussion_{id}")
            .SendAsync("ParticipantRemoved", new { UserId = userId });

        return Ok();
    }

    [HttpPost("{id}/tasks")]
    public async Task<ActionResult> LinkTask(int id, [FromBody] int taskId)
    {
        var userId = GetCurrentUserId();
        var success = await _discussionService.LinkTaskToDiscussionAsync(id, taskId, userId);
        
        if (!success)
            return NotFound();

        var task = await _discussionService.GetDiscussionTaskAsync(id, taskId);
        
        await _hubContext.Clients.Group($"Discussion_{id}")
            .SendAsync("TaskLinked", task);

        return Ok();
    }
}


