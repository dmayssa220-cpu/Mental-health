using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class VideoSession
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Appointment))]
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [MaxLength(200)]
    public string RoomName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}