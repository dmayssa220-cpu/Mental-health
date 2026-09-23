using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentalHealth.API.Services;

namespace MentalHealth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _ai;
    public AIController(IAIService ai) => _ai = ai;

    public class ChatRequest { public string Text { get; set; } = string.Empty; }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req)
    {
        var advice = await _ai.GetAdviceAsync(req.Text);
        return Ok(new { response = advice });
    }

    [HttpPost("sentiment")]
    public async Task<IActionResult> Sentiment([FromBody] ChatRequest req)
    {
        var sentiment = await _ai.AnalyzeSentimentAsync(req.Text);
        return Ok(new { sentiment });
    }
}