using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded
}

public class Payment
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Patient))]
    public int PatientId { get; set; }
    public User? Patient { get; set; }

    [ForeignKey(nameof(Doctor))]
    public int DoctorId { get; set; }
    public User? Doctor { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}