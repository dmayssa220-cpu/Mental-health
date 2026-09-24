using Stripe;
using Stripe.Checkout;

namespace MentalHealth.API.Services;

public interface IStripeService
{
    Task<Session> CreateCheckoutSessionAsync(int paymentId, decimal amount, string currency, string successUrl, string cancelUrl, string description);
    Event? ConstructWebhookEvent(string json, string signature, string secret);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration config)
    {
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    public async Task<Session> CreateCheckoutSessionAsync(int paymentId, decimal amount, string currency, string successUrl, string cancelUrl, string description)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(amount * 100), // en centimes
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = description
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "paymentId", paymentId.ToString() }
            }
        };

        var service = new SessionService();
        return await service.CreateAsync(options);
    }

    public Event? ConstructWebhookEvent(string json, string signature, string secret)
    {
        try
        {
            return EventUtility.ConstructEvent(json, signature, secret);
        }
        catch
        {
            return null;
        }
    }
}