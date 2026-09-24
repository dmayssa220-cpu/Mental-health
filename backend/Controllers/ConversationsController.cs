using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Hubs;
using MentalHealth.API.Models;
using MentalHealth.API.Services;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "application/pdf",
        "text/plain", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    private readonly AppDbContext _db;
    private readonly IAIService _ai;
    private readonly IHubContext<ChatHub> _hub;

    public ConversationsController(AppDbContext db, IAIService ai, IHubContext<ChatHub> hub)
    {
        _db = db;
        _ai = ai;
        _hub = hub;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private string CurrentRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    [HttpGet]
    public async Task<IActionResult> Mine()
    {
        var list = await _db.Conversations
            .Include(c => c.Patient).Include(c => c.Doctor)
            .Where(c => c.PatientId != c.DoctorId &&
                (c.PatientId == CurrentUserId || c.DoctorId == CurrentUserId))
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                c.LastMessageAt,
                Patient = new { c.Patient!.Id, c.Patient.FullName },
                Doctor = new { c.Doctor!.Id, c.Doctor.FullName, c.Doctor.Speciality },
                UnreadCount = _db.Messages.Count(m => m.ConversationId == c.Id && !m.IsRead && m.SenderId != CurrentUserId)
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("start/{doctorId}")]
    public async Task<IActionResult> Start(int doctorId)
    {
        if (CurrentRole != "Patient") return Forbid();
        if (doctorId == CurrentUserId) return BadRequest(new { message = "Impossible de démarrer une conversation avec soi-même." });

        var doctorExists = await _db.Users.AnyAsync(u => u.Id == doctorId && u.Role == "Doctor");
        if (!doctorExists) return BadRequest(new { message = "Le destinataire doit être un docteur." });

        var existing = await _db.Conversations
            .FirstOrDefaultAsync(c => c.PatientId == CurrentUserId && c.DoctorId == doctorId);
        if (existing != null) return Ok(existing);

        var conv = new Conversation
        {
            PatientId = CurrentUserId,
            DoctorId = doctorId,
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();
        return Ok(conv);
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> Messages(int id)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();

        var msgs = await _db.Messages
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
        return Ok(msgs.Select(ToMessageDto));
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] Message msg)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();
        if (conv.IsPatientBanned && conv.PatientId == CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous ne pouvez plus envoyer de messages dans cette conversation." });
        if (await _ai.IsMessageInappropriateAsync(msg.Content))
        {
            if (conv.PatientId == CurrentUserId)
            {
                conv.IsPatientBanned = true;
                await _db.SaveChangesAsync();
            }
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Message bloqué par la modération automatique." });
        }

        msg.ConversationId = id;
        msg.SenderId = CurrentUserId;
        msg.SentAt = DateTime.UtcNow;
        msg.IsFromAI = false;
        _db.Messages.Add(msg);

        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new
        {
            msg.Id,
            msg.Content,
            msg.IsFromAI,
            msg.SentAt,
            msg.ConversationId,
            msg.SenderId,
            IsRead = msg.IsRead,
            ReadAt = msg.ReadAt,
            Reactions = Array.Empty<object>(),
            Attachments = Array.Empty<object>()
        });
    }

    [HttpPost("{id}/messages/with-attachments")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SendMessageWithAttachments(int id, [FromForm] string? content, [FromForm] List<IFormFile>? files)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();
        if (conv.IsPatientBanned && conv.PatientId == CurrentUserId) return Forbid();
        if (string.IsNullOrWhiteSpace(content) && (files == null || files.Count == 0))
            return BadRequest(new { message = "Un message ou un fichier est requis." });
        if (!string.IsNullOrWhiteSpace(content) && await _ai.IsMessageInappropriateAsync(content))
        {
            if (conv.PatientId == CurrentUserId)
            {
                conv.IsPatientBanned = true;
                await _db.SaveChangesAsync();
            }
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Message bloqué par la modération automatique." });
        }
        if (files?.Any(f => f.Length > MaxFileSize || !AllowedContentTypes.Contains(f.ContentType)) == true)
            return BadRequest(new { message = "Type ou taille de fichier non autorisé." });

        var message = new Message
        {
            ConversationId = id,
            SenderId = CurrentUserId,
            Content = content?.Trim() ?? string.Empty,
            SentAt = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var uploadDirectory = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(uploadDirectory);
        foreach (var file in files ?? new List<IFormFile>())
        {
            var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploadDirectory, storedName);
            await using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);
            _db.MessageAttachments.Add(new MessageAttachment
            {
                MessageId = message.Id,
                OriginalName = Path.GetFileName(file.FileName),
                StoredName = storedName,
                ContentType = file.ContentType,
                Size = file.Length,
                StoragePath = path
            });
        }
        conv.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(message).Collection(m => m.Attachments).LoadAsync();
        var payload = ToMessageDto(message);
        await _hub.Clients.Group($"conversation-{id}").SendAsync("ReceiveMessage", payload);
        var otherId = conv.PatientId == CurrentUserId ? conv.DoctorId : conv.PatientId;
        await _hub.Clients.Group($"user-{otherId}").SendAsync("NewMessageNotification", new
        {
            conversationId = id,
            preview = string.IsNullOrWhiteSpace(message.Content) ? "Pièce jointe" : message.Content,
            from = User.Identity?.Name,
            sentAt = message.SentAt
        });
        return Ok(ToMessageDto(message));
    }

    [HttpPost("{conversationId}/messages/{messageId}/reactions")]
    public async Task<IActionResult> AddReaction(int conversationId, int messageId, [FromBody] ReactionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Emoji) || request.Emoji.Length > 16) return BadRequest();
        var message = await _db.Messages.Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (message == null) return NotFound();
        if (message.Conversation!.PatientId != CurrentUserId && message.Conversation.DoctorId != CurrentUserId) return Forbid();
        var exists = await _db.MessageReactions.AnyAsync(r => r.MessageId == messageId && r.UserId == CurrentUserId && r.Emoji == request.Emoji);
        if (!exists)
        {
            message.IsRead = false;
            message.ReadAt = null;
            _db.MessageReactions.Add(new MessageReaction { MessageId = messageId, UserId = CurrentUserId, Emoji = request.Emoji });
            await _db.SaveChangesAsync();
            var otherId = message.Conversation.PatientId == CurrentUserId ? message.Conversation.DoctorId : message.Conversation.PatientId;
            await _hub.Clients.Group($"user-{otherId}").SendAsync("NewMessageNotification", new
            {
                conversationId,
                preview = $"Réaction {request.Emoji}",
                from = User.Identity?.Name,
                sentAt = DateTime.UtcNow
            });
        }
        return Ok();
    }

    [HttpDelete("{conversationId}/messages/{messageId}/reactions/{emoji}")]
    public async Task<IActionResult> RemoveReaction(int conversationId, int messageId, string emoji)
    {
        var reaction = await _db.MessageReactions.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == CurrentUserId && r.Emoji == emoji);
        if (reaction == null) return NotFound();
        _db.MessageReactions.Remove(reaction);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/ban-patient")]
    public async Task<IActionResult> BanPatient(int id)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.DoctorId != CurrentUserId) return Forbid();
        conv.IsPatientBanned = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/ban-patient")]
    public async Task<IActionResult> UnbanPatient(int id)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.DoctorId != CurrentUserId) return Forbid();
        conv.IsPatientBanned = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{conversationId}/attachments/{attachmentId}")]
    public async Task<IActionResult> DownloadAttachment(int conversationId, int attachmentId)
    {
        var attachment = await _db.MessageAttachments.Include(a => a.Message)
            .ThenInclude(m => m!.Conversation)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.Message!.ConversationId == conversationId);
        if (attachment == null) return NotFound();
        var conversation = attachment.Message!.Conversation!;
        if (conversation.PatientId != CurrentUserId && conversation.DoctorId != CurrentUserId) return Forbid();
        if (!System.IO.File.Exists(attachment.StoragePath)) return NotFound();
        return PhysicalFile(attachment.StoragePath, attachment.ContentType, attachment.OriginalName);
    }

    private static object ToMessageDto(Message message) => new
    {
        message.Id,
        message.Content,
        message.IsFromAI,
        message.SentAt,
        message.ConversationId,
        message.SenderId,
        message.IsRead,
        message.ReadAt,
        Reactions = message.Reactions.GroupBy(r => r.Emoji).Select(g => new { emoji = g.Key, count = g.Count() }),
        Attachments = message.Attachments.Select(a => new { a.Id, a.OriginalName, a.ContentType, a.Size, url = $"/api/conversations/{message.ConversationId}/attachments/{a.Id}" })
    };

    public class ReactionRequest
    {
        public string Emoji { get; set; } = string.Empty;
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _db.Messages
            .Where(m => !m.IsRead && m.SenderId != CurrentUserId &&
                m.Conversation!.PatientId != m.Conversation.DoctorId &&
                (m.Conversation.PatientId == CurrentUserId || m.Conversation.DoctorId == CurrentUserId))
            .CountAsync();

        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();

        await _db.Messages
            .Where(m => m.ConversationId == id && m.SenderId != CurrentUserId && !m.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsRead, true)
                .SetProperty(m => m.ReadAt, DateTime.UtcNow));

        return NoContent();
    }
    [HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var conv = await _db.Conversations
        .Include(c => c.Patient)
        .Include(c => c.Doctor)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (conv == null) return NotFound();
    if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();

    var isPatient = conv.PatientId == CurrentUserId;
    var other = isPatient ? conv.Doctor! : conv.Patient!;

    return Ok(new
    {
        conv.Id,
        conv.CreatedAt,
        conv.LastMessageAt,
        OtherUser = new
        {
            other.Id,
            other.FullName,
            other.Role,
            other.Speciality,
            other.Bio
        },
        IsPatient = isPatient
        ,IsPatientBanned = conv.IsPatientBanned
    });
}
}