using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    public DoctorsController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("patients")]
    public async Task<IActionResult> MyPatients()
    {
        var patientIds = await _db.Conversations
            .Where(c => c.DoctorId == CurrentUserId)
            .Select(c => c.PatientId)
            .Distinct()
            .ToListAsync();

        var patients = await _db.Users
            .Where(u => patientIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id, u.FullName, u.Email,
                MoodAverage = u.MoodEntries.Any() ? u.MoodEntries.Average(m => m.Score) : 0,
                MoodCount = u.MoodEntries.Count(),
                JournalCount = u.JournalEntries.Count(),
                LastMood = u.MoodEntries
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpGet("patients/{patientId}/moods")]
    public async Task<IActionResult> PatientMoods(int patientId)
    {
        var hasAccess = await _db.Conversations
            .AnyAsync(c => c.DoctorId == CurrentUserId && c.PatientId == patientId);
        if (!hasAccess) return Forbid();

        var moods = await _db.MoodEntries
            .Where(m => m.UserId == patientId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .ToListAsync();
        return Ok(moods);
    }

    [HttpGet("patients/{patientId}/journals")]
    public async Task<IActionResult> PatientJournals(int patientId)
    {
        var hasAccess = await _db.Conversations
            .AnyAsync(c => c.DoctorId == CurrentUserId && c.PatientId == patientId);
        if (!hasAccess) return Forbid();

        var journals = await _db.JournalEntries
            .Where(j => j.UserId == patientId)
            .OrderByDescending(j => j.CreatedAt)
            .Take(20)
            .ToListAsync();
        return Ok(journals);
    }
}