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
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IHubContext<NotificationHub> _notifHub;

    public AppointmentsController(AppDbContext db, IEmailService email, IHubContext<NotificationHub> notifHub)
    {
        _db = db;
        _email = email;
        _notifHub = notifHub;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private string CurrentRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

    // ---------- Créneaux disponibles d'un docteur pour une date donnée ----------
    [HttpGet("doctors/{doctorId}/slots")]
    public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] DateTime date)
    {
        var availabilities = await _db.DoctorAvailabilities
            .Where(a => a.DoctorId == doctorId && a.DayOfWeek == date.DayOfWeek && a.IsActive)
            .ToListAsync();

        if (!availabilities.Any()) return Ok(Array.Empty<object>());

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var bookedSlots = await _db.Appointments
            .Where(a => a.DoctorId == doctorId
                        && a.ScheduledAt >= dayStart
                        && a.ScheduledAt < dayEnd
                        && a.Status != AppointmentStatus.Cancelled
                        && a.Status != AppointmentStatus.NoShow)
            .Select(a => a.ScheduledAt)
            .ToListAsync();

        var slots = new List<object>();
        foreach (var avail in availabilities)
        {
            var current = dayStart.Add(avail.StartTime);
            var end = dayStart.Add(avail.EndTime);
            while (current.AddMinutes(avail.SlotDurationMinutes) <= end)
            {
                if (!bookedSlots.Any(b => b == current) && current > DateTime.UtcNow)
                {
                    slots.Add(new { start = current, duration = avail.SlotDurationMinutes });
                }
                current = current.AddMinutes(avail.SlotDurationMinutes);
            }
        }

        return Ok(slots);
    }

    // ---------- Docteur : définir ses disponibilités ----------
    [HttpPost("availability")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> SetAvailability([FromBody] DoctorAvailability dto)
    {
        dto.DoctorId = CurrentUserId;
        dto.Id = 0;

        var exists = await _db.DoctorAvailabilities.AnyAsync(a =>
            a.DoctorId == dto.DoctorId &&
            a.DayOfWeek == dto.DayOfWeek &&
            a.StartTime == dto.StartTime);

        if (exists) return BadRequest(new { message = "Créneau déjà existant." });

        _db.DoctorAvailabilities.Add(dto);
        await _db.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpGet("availability")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetMyAvailability()
    {
        var list = await _db.DoctorAvailabilities
            .Where(a => a.DoctorId == CurrentUserId)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
            .ToListAsync();
        return Ok(list);
    }

    [HttpDelete("availability/{id}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DeleteAvailability(int id)
    {
        var a = await _db.DoctorAvailabilities.FindAsync(id);
        if (a == null || a.DoctorId != CurrentUserId) return NotFound();
        _db.DoctorAvailabilities.Remove(a);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Patient : prendre un RDV ----------
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Book([FromBody] Appointment dto)
    {
        if (dto.ScheduledAt <= DateTime.UtcNow)
            return BadRequest(new { message = "La date doit être dans le futur." });

        var doctorExists = await _db.Users.AnyAsync(u => u.Id == dto.DoctorId && u.Role == "Doctor");
        if (!doctorExists) return BadRequest(new { message = "Docteur introuvable." });

        // Vérifier le chevauchement
        var overlap = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == dto.DoctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.ScheduledAt < dto.ScheduledAt.AddMinutes(dto.DurationMinutes) &&
            a.ScheduledAt.AddMinutes(a.DurationMinutes) > dto.ScheduledAt);

        if (overlap) return BadRequest(new { message = "Ce créneau est déjà pris." });

        var appt = new Appointment
        {
            PatientId = CurrentUserId,
            DoctorId = dto.DoctorId,
            ScheduledAt = dto.ScheduledAt,
            DurationMinutes = dto.DurationMinutes,
            Type = dto.Type,
            Reason = dto.Reason,
            Status = AppointmentStatus.Pending
        };

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        // Notifier le docteur via SignalR
        await _notifHub.Clients.Group($"user-{appt.DoctorId}").SendAsync("AppointmentCreated", new
        {
            appointmentId = appt.Id,
            patientId = CurrentUserId,
            scheduledAt = appt.ScheduledAt
        });

        // Email de confirmation
        var doctor = await _db.Users.FindAsync(appt.DoctorId);
        var patient = await _db.Users.FindAsync(CurrentUserId);
        if (doctor != null && patient != null)
        {
            await _email.SendAsync(
                doctor.Email,
                "Nouvelle demande de rendez-vous",
                $"Le patient {patient.FullName} a demandé un RDV le {appt.ScheduledAt:g}."
            );
        }

        return Ok(appt);
    }

    // ---------- Liste des RDV (patient ou docteur) ----------
    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromQuery] string? status)
    {
        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .AsQueryable();

        query = CurrentRole == "Doctor"
            ? query.Where(a => a.DoctorId == CurrentUserId)
            : query.Where(a => a.PatientId == CurrentUserId);

        if (Enum.TryParse<AppointmentStatus>(status, true, out var s))
            query = query.Where(a => a.Status == s);

        var list = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Select(a => new
            {
                a.Id,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Type,
                a.Status,
                a.Reason,
                a.DoctorNotes,
                a.PaymentId,
                Patient = new { a.Patient!.Id, a.Patient.FullName },
                Doctor = new { a.Doctor!.Id, a.Doctor.FullName, a.Doctor.Speciality }
            })
            .ToListAsync();

        return Ok(list);
    }

    // ---------- Docteur : confirmer / refuser / compléter ----------
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == CurrentUserId);

        if (appt == null) return NotFound();

        appt.Status = req.Status;
        appt.DoctorNotes = req.DoctorNotes ?? appt.DoctorNotes;
        appt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notifier le patient
        await _notifHub.Clients.Group($"user-{appt.PatientId}").SendAsync("AppointmentUpdated", new
        {
            appointmentId = appt.Id,
            status = appt.Status.ToString(),
            scheduledAt = appt.ScheduledAt
        });

        if (appt.Patient != null)
        {
            await _email.SendAsync(
                appt.Patient.Email,
                $"Votre RDV du {appt.ScheduledAt:g} a été {appt.Status}",
                $"Statut : {appt.Status}. Note : {appt.DoctorNotes}"
            );
        }

        return Ok(appt);
    }

    // ---------- Patient : annuler ----------
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt == null) return NotFound();
        if (appt.PatientId != CurrentUserId && appt.DoctorId != CurrentUserId) return Forbid();

        appt.Status = AppointmentStatus.Cancelled;
        appt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var otherId = appt.PatientId == CurrentUserId ? appt.DoctorId : appt.PatientId;
        await _notifHub.Clients.Group($"user-{otherId}").SendAsync("AppointmentUpdated", new
        {
            appointmentId = appt.Id,
            status = "Cancelled",
            scheduledAt = appt.ScheduledAt
        });

        return NoContent();
    }
}

public class UpdateStatusRequest
{
    public AppointmentStatus Status { get; set; }
    public string? DoctorNotes { get; set; }
}