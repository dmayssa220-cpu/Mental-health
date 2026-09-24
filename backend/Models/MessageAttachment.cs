using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealth.API.Models;

public class MessageAttachment
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Message))]
    public int MessageId { get; set; }
    public Message? Message { get; set; }

    [Required, MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string StoredName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long Size { get; set; }

    [Required, MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
