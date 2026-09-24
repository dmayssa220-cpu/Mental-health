using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public enum AppointmentStatus
{
    Pending,      
    Confirmed,    
    Cancelled,    
    Completed,    
    NoShow        
}

public enum AppointmentType
{
    Video,
    InPerson,
    Chat
}

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Patient))]
    public int PatientId { get; set; }
    public User? Patient { get; set; }

    [ForeignKey(nameof(Doctor))]
    public int DoctorId { get; set; }
    public User? Doctor { get; set; }

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;

    public AppointmentType Type { get; set; } = AppointmentType.Video;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? DoctorNotes { get; set; }

    // Lien avec paiement
    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    // Rappels
    public bool Reminder24hSent { get; set; } = false;
    public bool Reminder1hSent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}