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
public class JournalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAIService _ai;

    public JournalController(AppDbContext db, IAIService ai)
    {
        _db = db;
        _ai = ai;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] JournalEntry entry)
    {
        entry.UserId = CurrentUserId;
        entry.CreatedAt = DateTime.UtcNow;
        entry.Sentiment = await _ai.AnalyzeSentimentAsync(entry.Content);
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var list = await _db.JournalEntries
            .Where(j => j.UserId == CurrentUserId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        return Ok(list);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _db.JournalEntries.FindAsync(id);
        if (entry == null || entry.UserId != CurrentUserId) return NotFound();
        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}