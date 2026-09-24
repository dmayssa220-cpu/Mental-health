using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class Conversation
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public bool IsPatientBanned { get; set; }

    [ForeignKey(nameof(Patient))]
    public int PatientId { get; set; }
    public User? Patient { get; set; }

    [ForeignKey(nameof(Doctor))]
    public int DoctorId { get; set; }
    public User? Doctor { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}