using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Models;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VideoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public VideoController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // Créer ou récupérer la session vidéo d'un RDV
    [HttpPost("appointment/{appointmentId}/session")]
    public async Task<IActionResult> CreateOrGet(int appointmentId)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient).Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appt == null) return NotFound();
        if (appt.PatientId != CurrentUserId && appt.DoctorId != CurrentUserId) return Forbid();
        if (appt.Type != AppointmentType.Video) return BadRequest(new { message = "Ce RDV n'est pas en visio." });
        if (appt.Status == AppointmentStatus.Cancelled) return BadRequest(new { message = "RDV annulé." });
        if (appt.Status != AppointmentStatus.Confirmed)
            return BadRequest(new { message = "Le RDV doit être confirmé pour lancer la visio." });

        // Fenêtre autorisée : 10 min avant → durée + 15 min après
        var now = DateTime.UtcNow;
        var start = appt.ScheduledAt.AddMinutes(-10);
        var end = appt.ScheduledAt.AddMinutes(appt.DurationMinutes + 15);
        if (now < start || now > end)
            return BadRequest(new { message = "La session n'est pas disponible actuellement." });

        var session = await _db.VideoSessions.FirstOrDefaultAsync(v => v.AppointmentId == appointmentId);
        if (session == null)
        {
            session = new VideoSession
            {
                AppointmentId = appointmentId,
                RoomName = $"mindcare-appt-{appointmentId}-{Guid.NewGuid():N}".Substring(0, 40),
                CreatedAt = DateTime.UtcNow
            };
            _db.VideoSessions.Add(session);
            await _db.SaveChangesAsync();
        }

        var domain = _config["Jitsi:Domain"] ?? "meet.jit.si";
        var roomUrl = $"https://{domain}/{session.RoomName}";

        return Ok(new
        {
            session.Id,
            session.RoomName,
            RoomUrl = roomUrl,
            Appointment = new
            {
                appt.Id,
                appt.ScheduledAt,
                appt.DurationMinutes,
                Patient = new { appt.Patient!.Id, appt.Patient.FullName },
                Doctor = new { appt.Doctor!.Id, appt.Doctor.FullName }
            }
        });
    }

    [HttpPost("session/{sessionId}/start")]
    public async Task<IActionResult> Start(int sessionId)
    {
        var s = await _db.VideoSessions.Include(v => v.Appointment).FirstOrDefaultAsync(v => v.Id == sessionId);
        if (s == null) return NotFound();
        if (s.Appointment!.PatientId != CurrentUserId && s.Appointment.DoctorId != CurrentUserId) return Forbid();

        s.StartedAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { s.Id, s.StartedAt });
    }

    [HttpPost("session/{sessionId}/end")]
    public async Task<IActionResult> End(int sessionId)
    {
        var s = await _db.VideoSessions.Include(v => v.Appointment).FirstOrDefaultAsync(v => v.Id == sessionId);
        if (s == null) return NotFound();
        if (s.Appointment!.PatientId != CurrentUserId && s.Appointment.DoctorId != CurrentUserId) return Forbid();

        s.EndedAt = DateTime.UtcNow;
        s.Appointment.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();
        return Ok();
    }
}