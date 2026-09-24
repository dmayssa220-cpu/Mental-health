using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class DoctorAvailability
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Doctor))]
    public int DoctorId { get; set; }
    public User? Doctor { get; set; }

    // 0 = Dimanche, 1 = Lundi, ..., 6 = Samedi
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int SlotDurationMinutes { get; set; } = 30;

    public bool IsActive { get; set; } = true;
}