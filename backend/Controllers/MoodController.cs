using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Models;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodController : ControllerBase
{
    private readonly AppDbContext _db;
    public MoodController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] MoodEntry entry)
    {
        entry.UserId = CurrentUserId;
        entry.CreatedAt = DateTime.UtcNow;
        _db.MoodEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var list = await _db.MoodEntries
            .Where(m => m.UserId == CurrentUserId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var entries = await _db.MoodEntries
            .Where(m => m.UserId == CurrentUserId)
            .ToListAsync();

        if (!entries.Any())
            return Ok(new { average = 0, count = 0 });

        return Ok(new
        {
            average = entries.Average(e => e.Score),
            count = entries.Count
        });
    }
}