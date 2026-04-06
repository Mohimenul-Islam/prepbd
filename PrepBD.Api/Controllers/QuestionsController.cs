using Microsoft.AspNetCore.Mvc;
using PrepBD.Api.Services;

namespace PrepBD.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly QuestionService _questionService;

    public QuestionsController(QuestionService questionService)
    {
        _questionService = questionService;
    }

    [HttpGet("topics")]
    public IActionResult GetTopics()
    {
        var topics = _questionService.GetTopics();
        return Ok(topics);
    }

    [HttpGet]
    public IActionResult GetQuestions(
        [FromQuery] string mode = "mixed",
        [FromQuery] string? topics = null,
        [FromQuery] int count = 10)
    {
        if (count < 1 || count > 50)
        {
            return BadRequest("Count must be between 1 and 50.");
        }

        var questions = _questionService.GetQuestions(mode, topics, count);
        return Ok(questions);
    }
}
