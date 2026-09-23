using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _db.Users
            .Where(u => u.Role == "Doctor")
            .Select(u => new { u.Id, u.FullName, u.Email, u.Speciality, u.Bio })
            .ToListAsync();
        return Ok(doctors);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return Ok(new
        {
            user.Id, user.FullName, user.Email,
            user.Role, user.Speciality, user.Bio, user.CreatedAt
        });
    }
}