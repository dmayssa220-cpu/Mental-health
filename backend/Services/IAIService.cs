namespace MentalHealth.API.Services;

public interface IAIService
{
    Task<string> GetAdviceAsync(string userMessage);
    Task<string> AnalyzeSentimentAsync(string text);
    Task<bool> IsMessageInappropriateAsync(string text);
}