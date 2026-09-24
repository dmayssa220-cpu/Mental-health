using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class MessageReaction
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Message))]
    public int MessageId { get; set; }
    public Message? Message { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(16)]
    public string Emoji { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
