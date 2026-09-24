using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Models;

namespace MentalHealth.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<VideoSession> VideoSessions => Set<VideoSession>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

       
        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Patient)
            .WithMany(u => u.ConversationsAsPatient)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Doctor)
            .WithMany(u => u.ConversationsAsDoctor)
            .HasForeignKey(c => c.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

       
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageReaction>()
            .HasIndex(r => new { r.MessageId, r.UserId, r.Emoji })
            .IsUnique();

        modelBuilder.Entity<MessageReaction>()
            .HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageAttachment>()
            .HasOne(a => a.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Appointment>()
    .HasOne(a => a.Patient)
    .WithMany()
    .HasForeignKey(a => a.PatientId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Appointment>()
    .HasOne(a => a.Doctor)
    .WithMany()
    .HasForeignKey(a => a.DoctorId)
    .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Appointment>()
    .HasIndex(a => new { a.DoctorId, a.ScheduledAt });

    modelBuilder.Entity<DoctorAvailability>()
    .HasOne(d => d.Doctor)
    .WithMany()
    .HasForeignKey(d => d.DoctorId)
    .OnDelete(DeleteBehavior.Cascade);

     modelBuilder.Entity<DoctorAvailability>()
    .HasIndex(d => new { d.DoctorId, d.DayOfWeek, d.StartTime })
    .IsUnique();
    }
}