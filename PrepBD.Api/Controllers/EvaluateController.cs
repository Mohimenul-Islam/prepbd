using Microsoft.AspNetCore.Mvc;
using PrepBD.Api.Models;
using PrepBD.Api.Services;

namespace PrepBD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluateController : ControllerBase
{
    private readonly EvaluationService _evaluationService;

    public EvaluateController(EvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpPost]
    public async Task<IActionResult> Evaluate([FromBody] EvaluationRequest request)
    {
        if (request.Answers == null || request.Answers.Count == 0)
        {
            return BadRequest("No answers provided.");
        }

        var result = await _evaluationService.EvaluateAnswersAsync(request);
        return Ok(result);
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            configured = _evaluationService.IsConfigured,
            message = _evaluationService.IsConfigured
                ? "AI evaluation is active."
                : "LLM API key not configured. Set Llm:ApiKey (e.g. via user-secrets). The app will show model answers as fallback."
        });
    }
}
