using System.Text.Json.Serialization;

namespace PrepBD.Api.Models;

public class EvaluationRequest
{
    [JsonPropertyName("answers")]
    public List<AnswerSubmission> Answers { get; set; } = new();
}

public class AnswerSubmission
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("userAnswer")]
    public string UserAnswer { get; set; } = string.Empty;
}

public class EvaluationResponse
{
    [JsonPropertyName("evaluations")]
    public List<QuestionEvaluation> Evaluations { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

public class QuestionEvaluation
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("userAnswer")]
    public string UserAnswer { get; set; } = string.Empty;

    [JsonPropertyName("accuracy")]
    public string Accuracy { get; set; } = string.Empty;

    [JsonPropertyName("gaps")]
    public string Gaps { get; set; } = string.Empty;

    [JsonPropertyName("improvement")]
    public string Improvement { get; set; } = string.Empty;

    [JsonPropertyName("modelAnswer")]
    public string ModelAnswer { get; set; } = string.Empty;
}
