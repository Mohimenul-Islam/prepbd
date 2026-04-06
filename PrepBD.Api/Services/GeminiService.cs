using System.Text;
using System.Text.Json;
using PrepBD.Api.Models;

namespace PrepBD.Api.Services;

public class GeminiService
{
    private readonly string? _apiKey;
    private readonly HttpClient _httpClient;
    private readonly QuestionService _questionService;
    private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    public GeminiService(IConfiguration configuration, HttpClient httpClient, QuestionService questionService)
    {
        _apiKey = configuration["Gemini:ApiKey"];
        _httpClient = httpClient;
        _questionService = questionService;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "<configure later>";

    public async Task<EvaluationResponse> EvaluateAnswersAsync(EvaluationRequest request)
    {
        // Build question context with ideal answers
        var questionsWithAnswers = new List<(Question question, string userAnswer)>();
        foreach (var answer in request.Answers)
        {
            var question = _questionService.GetQuestionById(answer.QuestionId);
            if (question != null)
            {
                questionsWithAnswers.Add((question, answer.UserAnswer));
            }
        }

        if (!IsConfigured)
        {
            return BuildFallbackResponse(questionsWithAnswers);
        }

        try
        {
            return await CallGeminiApi(questionsWithAnswers);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini API error: {ex.Message}");
            return BuildFallbackResponse(questionsWithAnswers);
        }
    }

    private async Task<EvaluationResponse> CallGeminiApi(List<(Question question, string userAnswer)> questionsWithAnswers)
    {
        var prompt = BuildEvaluationPrompt(questionsWithAnswers);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "OBJECT",
                    properties = new Dictionary<string, object>
                    {
                        ["evaluations"] = new
                        {
                            type = "ARRAY",
                            items = new
                            {
                                type = "OBJECT",
                                properties = new Dictionary<string, object>
                                {
                                    ["questionId"] = new { type = "STRING" },
                                    ["accuracy"] = new { type = "STRING" },
                                    ["gaps"] = new { type = "STRING" },
                                    ["improvement"] = new { type = "STRING" },
                                    ["modelAnswer"] = new { type = "STRING" }
                                },
                                required = new[] { "questionId", "accuracy", "gaps", "improvement", "modelAnswer" }
                            }
                        },
                        ["summary"] = new { type = "STRING" }
                    },
                    required = new[] { "evaluations", "summary" }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{GeminiApiUrl}?key={_apiKey}";
        var response = await _httpClient.PostAsync(url, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Gemini API returned {response.StatusCode}: {responseText}");
            return BuildFallbackResponse(questionsWithAnswers);
        }

        // Parse Gemini response
        using var doc = JsonDocument.Parse(responseText);
        var candidateText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrEmpty(candidateText))
        {
            return BuildFallbackResponse(questionsWithAnswers);
        }

        var geminiResult = JsonSerializer.Deserialize<GeminiEvaluationResult>(candidateText);
        if (geminiResult == null)
        {
            return BuildFallbackResponse(questionsWithAnswers);
        }

        // Map back to our response format
        var evaluationResponse = new EvaluationResponse
        {
            Summary = geminiResult.Summary ?? "Evaluation complete.",
            Evaluations = new List<QuestionEvaluation>()
        };

        foreach (var qa in questionsWithAnswers)
        {
            var geminiEval = geminiResult.Evaluations?
                .FirstOrDefault(e => e.QuestionId == qa.question.Id);

            evaluationResponse.Evaluations.Add(new QuestionEvaluation
            {
                QuestionId = qa.question.Id,
                Question = qa.question.QuestionText,
                UserAnswer = qa.userAnswer,
                Accuracy = geminiEval?.Accuracy ?? "Unable to evaluate.",
                Gaps = geminiEval?.Gaps ?? "N/A",
                Improvement = geminiEval?.Improvement ?? "N/A",
                ModelAnswer = geminiEval?.ModelAnswer ?? qa.question.IdealAnswer
            });
        }

        return evaluationResponse;
    }

    private string BuildEvaluationPrompt(List<(Question question, string userAnswer)> questionsWithAnswers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert Computer Science examiner evaluating written test answers for a fresher Software Engineer position at a top Bangladeshi tech company (like bKash or Therap).");
        sb.AppendLine();
        sb.AppendLine("For each answer below, provide a detailed evaluation:");
        sb.AppendLine("1. ACCURACY: What the candidate got right. Be specific and encouraging about correct parts.");
        sb.AppendLine("2. GAPS: What was missing, incorrect, or incomplete. Be honest but constructive.");
        sb.AppendLine("3. IMPROVEMENT: Specific advice on how to write a better answer in an exam setting.");
        sb.AppendLine("4. MODEL_ANSWER: A concise, ideal answer that would score full marks.");
        sb.AppendLine();
        sb.AppendLine("Important guidelines:");
        sb.AppendLine("- The bar is fundamental CS knowledge for a fresher role — not expert level.");
        sb.AppendLine("- If the answer is empty or says 'skipped', note it was not attempted.");
        sb.AppendLine("- Be encouraging but honest. Point out what they did well first.");
        sb.AppendLine("- Keep each field to 2-4 sentences for readability.");
        sb.AppendLine();
        sb.AppendLine("Also provide a brief overall SUMMARY (2-3 sentences) assessing the candidate's overall performance.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        for (int i = 0; i < questionsWithAnswers.Count; i++)
        {
            var (question, userAnswer) = questionsWithAnswers[i];
            sb.AppendLine($"QUESTION {i + 1} (ID: {question.Id}):");
            sb.AppendLine($"Topic: {question.Topic} > {question.Subtopic}");
            sb.AppendLine($"Question: {question.QuestionText}");
            sb.AppendLine($"Key points to check: {string.Join(", ", question.KeyPoints)}");
            sb.AppendLine($"Reference answer: {question.IdealAnswer}");
            sb.AppendLine($"Candidate's answer: {(string.IsNullOrWhiteSpace(userAnswer) ? "[NOT ATTEMPTED]" : userAnswer)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private EvaluationResponse BuildFallbackResponse(List<(Question question, string userAnswer)> questionsWithAnswers)
    {
        return new EvaluationResponse
        {
            Summary = IsConfigured
                ? "AI evaluation encountered an error. Showing model answers for reference."
                : "AI evaluation is not configured. Showing model answers for reference. Add your Gemini API key in appsettings.json to enable detailed AI evaluation.",
            Evaluations = questionsWithAnswers.Select(qa => new QuestionEvaluation
            {
                QuestionId = qa.question.Id,
                Question = qa.question.QuestionText,
                UserAnswer = qa.userAnswer,
                Accuracy = "AI evaluation not available.",
                Gaps = "Compare your answer with the model answer below.",
                Improvement = "Review the model answer to identify areas for improvement.",
                ModelAnswer = qa.question.IdealAnswer
            }).ToList()
        };
    }
}

// Internal model for parsing Gemini's structured output
internal class GeminiEvaluationResult
{
    [System.Text.Json.Serialization.JsonPropertyName("evaluations")]
    public List<GeminiQuestionEval>? Evaluations { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

internal class GeminiQuestionEval
{
    [System.Text.Json.Serialization.JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("accuracy")]
    public string? Accuracy { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("gaps")]
    public string? Gaps { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("improvement")]
    public string? Improvement { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("modelAnswer")]
    public string? ModelAnswer { get; set; }
}
