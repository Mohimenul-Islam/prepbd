using Microsoft.AspNetCore.Mvc;
using PrepBD.Api.Models;
using PrepBD.Api.Services;

namespace PrepBD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluateController : ControllerBase
{
    private readonly GeminiService _geminiService;

    public EvaluateController(GeminiService geminiService)
    {
        _geminiService = geminiService;
    }

    [HttpPost]
    public async Task<IActionResult> Evaluate([FromBody] EvaluationRequest request)
    {
        if (request.Answers == null || request.Answers.Count == 0)
        {
            return BadRequest("No answers provided.");
        }

        var result = await _geminiService.EvaluateAnswersAsync(request);
        return Ok(result);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            configured = _geminiService.IsConfigured,
            message = _geminiService.IsConfigured
                ? "Gemini AI evaluation is active."
                : "Gemini API key not configured. Add your key in appsettings.json. The app will show model answers as fallback."
        });
    }
}
