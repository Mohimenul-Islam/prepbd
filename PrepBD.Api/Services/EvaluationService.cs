using System.Text;
using System.Text.Json;
using PrepBD.Api.Models;

namespace PrepBD.Api.Services;

// Provider-agnostic LLM evaluator. Talks to any OpenAI-compatible chat-completions
// endpoint (OpenRouter, OpenAI, Groq, Together, Gemini's OpenAI endpoint, ...).
// Switch providers by editing Llm:BaseUrl / Llm:Model / Llm:ApiKey in config — no code change.
public class EvaluationService
{
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly HttpClient _httpClient;
    private readonly QuestionService _questionService;

    public EvaluationService(IConfiguration configuration, HttpClient httpClient, QuestionService questionService)
    {
        _apiKey = configuration["Llm:ApiKey"];
        _baseUrl = (configuration["Llm:BaseUrl"] ?? "https://openrouter.ai/api/v1").TrimEnd('/');
        _model = configuration["Llm:Model"] ?? "meta-llama/llama-3.3-70b-instruct:free";
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
            return await CallLlmApi(questionsWithAnswers);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LLM API error: {ex.Message}");
            return BuildFallbackResponse(questionsWithAnswers);
        }
    }

    private async Task<EvaluationResponse> CallLlmApi(List<(Question question, string userAnswer)> questionsWithAnswers)
    {
        var prompt = BuildEvaluationPrompt(questionsWithAnswers);

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            // "give me valid JSON" — widely supported by capable models. If a chosen model rejects
            // this field, remove it; the prompt still pins the exact shape and ExtractJson cleans up.
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(httpRequest);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"LLM API returned {response.StatusCode}: {responseText}");
            return BuildFallbackResponse(questionsWithAnswers);
        }

        // Pull the assistant message out of the OpenAI-compatible response shape
        string? messageContent;
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not parse LLM envelope: {ex.Message}");
            return BuildFallbackResponse(questionsWithAnswers);
        }

        if (string.IsNullOrWhiteSpace(messageContent))
        {
            return BuildFallbackResponse(questionsWithAnswers);
        }

        LlmEvaluationResult? llmResult;
        try
        {
            llmResult = JsonSerializer.Deserialize<LlmEvaluationResult>(ExtractJson(messageContent));
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Could not parse LLM JSON content: {ex.Message}");
            return BuildFallbackResponse(questionsWithAnswers);
        }

        if (llmResult == null)
        {
            return BuildFallbackResponse(questionsWithAnswers);
        }

        // Map back to our response format
        var evaluationResponse = new EvaluationResponse
        {
            Summary = llmResult.Summary ?? "Evaluation complete.",
            Evaluations = new List<QuestionEvaluation>()
        };

        foreach (var qa in questionsWithAnswers)
        {
            var eval = llmResult.Evaluations?
                .FirstOrDefault(e => e.QuestionId == qa.question.Id);

            evaluationResponse.Evaluations.Add(new QuestionEvaluation
            {
                QuestionId = qa.question.Id,
                Question = qa.question.QuestionText,
                UserAnswer = qa.userAnswer,
                Score = eval?.Score,
                Accuracy = eval?.Accuracy ?? "Unable to evaluate.",
                Gaps = eval?.Gaps ?? "N/A",
                Improvement = eval?.Improvement ?? "N/A",
                ModelAnswer = eval?.ModelAnswer ?? qa.question.IdealAnswer
            });
        }

        return evaluationResponse;
    }

    // Free models sometimes wrap the JSON in ```json ... ``` fences or add stray prose.
    // Strip fences and keep the outermost { ... } object so deserialization succeeds.
    private static string ExtractJson(string text)
    {
        var t = text.Trim();
        if (t.StartsWith("```"))
        {
            int firstNewline = t.IndexOf('\n');
            if (firstNewline >= 0) t = t[(firstNewline + 1)..];
            if (t.EndsWith("```")) t = t[..^3];
            t = t.Trim();
        }
        int start = t.IndexOf('{');
        int end = t.LastIndexOf('}');
        return (start >= 0 && end > start) ? t[start..(end + 1)] : t;
    }

    private string BuildEvaluationPrompt(List<(Question question, string userAnswer)> questionsWithAnswers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert Computer Science examiner evaluating written test answers for a fresher Software Engineer position at a top Bangladeshi tech company (like bKash, Therap, Optimizely, or Cefalo).");
        sb.AppendLine();
        sb.AppendLine("For each answer below, provide:");
        sb.AppendLine("1. SCORE: an integer 0-10 (0 = not attempted or completely wrong, 10 = full marks for a fresher). The bar is fundamental CS knowledge for a fresher role, not expert level.");
        sb.AppendLine("2. ACCURACY: what the candidate got right. Be specific and encouraging about correct parts.");
        sb.AppendLine("3. GAPS: what was missing, incorrect, or incomplete. Be honest but constructive.");
        sb.AppendLine("4. IMPROVEMENT: specific advice on how to write a better answer in an exam setting.");
        sb.AppendLine("5. MODEL_ANSWER: a concise, ideal answer that would score full marks.");
        sb.AppendLine();
        sb.AppendLine("For CODING or SQL answers that include sample examples: mentally execute the candidate's code against each example. In ACCURACY, state which examples it would pass; in GAPS, note any failing cases, missed edge cases, and time/space-complexity issues. You do not have a compiler, so reason step by step and flag any uncertainty.");
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- If the answer is empty or says 'skipped', score it 0 and note it was not attempted.");
        sb.AppendLine("- Be encouraging but honest. Point out what they did well first.");
        sb.AppendLine("- Keep each text field to 2-4 sentences for readability.");
        sb.AppendLine();
        sb.AppendLine("Also provide a brief overall SUMMARY (2-3 sentences) assessing the candidate's overall performance.");
        sb.AppendLine();
        sb.AppendLine("Respond with ONLY a JSON object in exactly this shape (no markdown, no extra text):");
        sb.AppendLine("{\"evaluations\":[{\"questionId\":\"string\",\"score\":0,\"accuracy\":\"string\",\"gaps\":\"string\",\"improvement\":\"string\",\"modelAnswer\":\"string\"}],\"summary\":\"string\"}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        for (int i = 0; i < questionsWithAnswers.Count; i++)
        {
            var (question, userAnswer) = questionsWithAnswers[i];
            sb.AppendLine($"QUESTION {i + 1} (ID: {question.Id}):");
            sb.AppendLine($"Topic: {question.Topic} > {question.Subtopic}");
            sb.AppendLine($"Type: {question.Type}");
            sb.AppendLine($"Question: {question.QuestionText}");
            if (question.Examples != null && question.Examples.Count > 0)
            {
                sb.AppendLine("Examples:");
                foreach (var ex in question.Examples)
                {
                    sb.AppendLine($"  input: {ex.Input}  ->  output: {ex.Output}");
                }
            }
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
                : "AI evaluation is not configured. Showing model answers for reference. Set your LLM API key (Llm:ApiKey) to enable detailed AI evaluation and scoring.",
            Evaluations = questionsWithAnswers.Select(qa => new QuestionEvaluation
            {
                QuestionId = qa.question.Id,
                Question = qa.question.QuestionText,
                UserAnswer = qa.userAnswer,
                Score = null,
                Accuracy = "AI evaluation not available.",
                Gaps = "Compare your answer with the model answer below.",
                Improvement = "Review the model answer to identify areas for improvement.",
                ModelAnswer = qa.question.IdealAnswer
            }).ToList()
        };
    }
}

// Internal model for parsing the LLM's JSON output
internal class LlmEvaluationResult
{
    [System.Text.Json.Serialization.JsonPropertyName("evaluations")]
    public List<LlmQuestionEval>? Evaluations { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

internal class LlmQuestionEval
{
    [System.Text.Json.Serialization.JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("score")]
    public int? Score { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("accuracy")]
    public string? Accuracy { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("gaps")]
    public string? Gaps { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("improvement")]
    public string? Improvement { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("modelAnswer")]
    public string? ModelAnswer { get; set; }
}
