using System.ComponentModel.DataAnnotations;

namespace MentalHealth.API.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Patient";

    public string? Speciality { get; set; } 
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relations
    public ICollection<MoodEntry> MoodEntries { get; set; } = new List<MoodEntry>();
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    public ICollection<Conversation> ConversationsAsPatient { get; set; } = new List<Conversation>();
    public ICollection<Conversation> ConversationsAsDoctor { get; set; } = new List<Conversation>();
}