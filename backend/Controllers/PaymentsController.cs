using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MentalHealth.API.Data;
using MentalHealth.API.Models;
using MentalHealth.API.Services;
using Stripe;
using Stripe.Checkout;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStripeService _stripe;
    private readonly IConfiguration _config;

    public PaymentsController(AppDbContext db, IStripeService stripe, IConfiguration config)
    {
        _db = db;
        _stripe = stripe;
        _config = config;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    // ----- Créer une session de paiement pour un RDV -----
    [HttpPost("appointment/{appointmentId}/checkout")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateCheckout(int appointmentId)
    {
        var appt = await _db.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == CurrentUserId);

        if (appt == null) return NotFound();
        if (appt.PaymentId.HasValue) return BadRequest(new { message = "Ce RDV a déjà un paiement." });

        // Tarif du docteur (par défaut 50 EUR — à personnaliser côté User)
        var amount = 50m;

        var payment = new Payment
        {
            PatientId = CurrentUserId,
            DoctorId = appt.DoctorId,
            Amount = amount,
            Currency = "eur",
            Status = PaymentStatus.Pending
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var frontendUrl = _config["Frontend:BaseUrl"] ?? "http://localhost";
        Session session;
        try
        {
            session = await _stripe.CreateCheckoutSessionAsync(
                payment.Id,
                amount,
                "eur",
                $"{frontendUrl}/payment/success?paymentId={payment.Id}&appointmentId={appt.Id}",
                $"{frontendUrl}/payment/cancel?appointmentId={appt.Id}",
                $"Consultation avec Dr. {appt.Doctor?.FullName}"
            );
        }
        catch (StripeException)
        {
            _db.Payments.Remove(payment);
            await _db.SaveChangesAsync();
            return StatusCode(503, new { message = "Le paiement est temporairement indisponible. Configurez une clé Stripe de test valide." });
        }

        payment.StripeSessionId = session.Id;
        appt.PaymentId = payment.Id;
        await _db.SaveChangesAsync();

        return Ok(new { sessionId = session.Id, url = session.Url, paymentId = payment.Id });
    }

    // ----- Webhook Stripe -----
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var secret = _config["Stripe:WebhookSecret"]!;

        var stripeEvent = _stripe.ConstructWebhookEvent(json, signature, secret);
        if (stripeEvent == null) return BadRequest();

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session?.Metadata != null && session.Metadata.TryGetValue("paymentId", out var pidStr))
            {
                var paymentId = int.Parse(pidStr);
                var payment = await _db.Payments.FindAsync(paymentId);
                if (payment != null && payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Succeeded;
                    payment.PaidAt = DateTime.UtcNow;
                    payment.StripePaymentIntentId = session.PaymentIntentId;
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok();
    }

    // ----- Statut d'un paiement -----
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _db.Payments.FindAsync(id);
        if (p == null) return NotFound();
        if (p.PatientId != CurrentUserId && p.DoctorId != CurrentUserId) return Forbid();
        return Ok(new { p.Id, p.Amount, p.Currency, p.Status, p.CreatedAt, p.PaidAt });
    }
}