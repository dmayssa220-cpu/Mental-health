using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Hubs;
using MentalHealth.API.Models;

namespace MentalHealth.API.Services;

public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IServiceProvider services, ILogger<ReminderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var notifHub = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                var now = DateTime.UtcNow;

                // Rappel 24h
                var upcoming24 = await db.Appointments
                    .Include(a => a.Patient).Include(a => a.Doctor)
                    .Where(a => a.Status == AppointmentStatus.Confirmed
                                && !a.Reminder24hSent
                                && a.ScheduledAt <= now.AddHours(24)
                                && a.ScheduledAt > now)
                    .ToListAsync(stoppingToken);

                foreach (var a in upcoming24)
                {
                    a.Reminder24hSent = true;
                    if (a.Patient != null)
                        await email.SendAsync(a.Patient.Email, "Rappel RDV demain",
                            $"Votre RDV avec Dr. {a.Doctor?.FullName} est prévu le {a.ScheduledAt:g}.");
                    await notifHub.Clients.Group($"user-{a.PatientId}")
                        .SendAsync("AppointmentReminder", new { appointmentId = a.Id, when = a.ScheduledAt });
                }

                // Rappel 1h
                var upcoming1 = await db.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.Status == AppointmentStatus.Confirmed
                                && !a.Reminder1hSent
                                && a.ScheduledAt <= now.AddHours(1)
                                && a.ScheduledAt > now)
                    .ToListAsync(stoppingToken);

                foreach (var a in upcoming1)
                {
                    a.Reminder1hSent = true;
                    await notifHub.Clients.Group($"user-{a.PatientId}")
                        .SendAsync("AppointmentReminder", new { appointmentId = a.Id, when = a.ScheduledAt });
                }

                if (upcoming24.Any() || upcoming1.Any())
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur ReminderService");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}