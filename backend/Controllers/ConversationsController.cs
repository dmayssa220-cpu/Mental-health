using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Models;
using MentalHealth.API.Services;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAIService _ai;

    public ConversationsController(AppDbContext db, IAIService ai)
    {
        _db = db;
        _ai = ai;
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
            .Where(c => c.PatientId == CurrentUserId || c.DoctorId == CurrentUserId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                c.CreatedAt,
                c.LastMessageAt,
                Patient = new { c.Patient!.Id, c.Patient.FullName },
                Doctor = new { c.Doctor!.Id, c.Doctor.FullName, c.Doctor.Speciality }
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("start/{doctorId}")]
    public async Task<IActionResult> Start(int doctorId)
    {
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
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.SentAt)
            .Select(m => new
            {
                m.Id,
                m.Content,
                m.IsFromAI,
                m.SentAt,
                m.ConversationId,
                m.SenderId
            })
            .ToListAsync();
        return Ok(msgs);
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] Message msg)
    {
        var conv = await _db.Conversations.FindAsync(id);
        if (conv == null) return NotFound();
        if (conv.PatientId != CurrentUserId && conv.DoctorId != CurrentUserId) return Forbid();

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
            msg.SenderId
        });
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
    });
}
}