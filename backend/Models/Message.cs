using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsFromAI { get; set; } = false;

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(Conversation))]
    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    [ForeignKey(nameof(Sender))]
    public int SenderId { get; set; }
    public User? Sender { get; set; }

    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}