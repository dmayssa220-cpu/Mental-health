using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class MoodEntry
{
    [Key]
    public int Id { get; set; }

    [Range(1, 10)]
    public int Score { get; set; } 

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }
}