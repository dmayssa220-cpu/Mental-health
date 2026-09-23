using System.Text;
using System.Text.Json;

namespace MentalHealth.API.Services;

public class AIService : IAIService
{
    private readonly HttpClient _http;

    public AIService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetAdviceAsync(string userMessage)
    {
        var payload = JsonSerializer.Serialize(new { text = userMessage });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var resp = await _http.PostAsync("/chat", content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("response").GetString() ?? "";
        }
        catch
        {
            return "Je suis désolé, je n'ai pas pu répondre pour le moment. Prenez un moment pour respirer profondément.";
        }
    }

    public async Task<string> AnalyzeSentimentAsync(string text)
    {
        var payload = JsonSerializer.Serialize(new { text });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            var resp = await _http.PostAsync("/sentiment", content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("sentiment").GetString() ?? "neutral";
        }
        catch
        {
            return "neutral";
        }
    }
}