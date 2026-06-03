using System.Text.Json.Serialization;

namespace PrepBD.Api.Models;

public class Question
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("topicSlug")]
    public string TopicSlug { get; set; } = string.Empty;

    [JsonPropertyName("subtopic")]
    public string Subtopic { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // conceptual, code-writing, sql, output-tracing, comparison, scenario

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium"; // easy, medium

    [JsonPropertyName("question")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("hints")]
    public List<string> Hints { get; set; } = new();

    [JsonPropertyName("examples")]
    public List<QuestionExample> Examples { get; set; } = new();

    [JsonPropertyName("leetcodeUrl")]
    public string? LeetCodeUrl { get; set; }

    [JsonPropertyName("idealAnswer")]
    public string IdealAnswer { get; set; } = string.Empty;

    [JsonPropertyName("keyPoints")]
    public List<string> KeyPoints { get; set; } = new();

    [JsonPropertyName("companyRelevance")]
    public List<string> CompanyRelevance { get; set; } = new();
}

public class QuestionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("topicSlug")]
    public string TopicSlug { get; set; } = string.Empty;

    [JsonPropertyName("subtopic")]
    public string Subtopic { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium";

    [JsonPropertyName("question")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("hints")]
    public List<string> Hints { get; set; } = new();

    [JsonPropertyName("examples")]
    public List<QuestionExample> Examples { get; set; } = new();

    [JsonPropertyName("leetcodeUrl")]
    public string? LeetCodeUrl { get; set; }
}

// A sample input/output pair for coding & SQL questions. Shown to the candidate and
// fed to the evaluator so it can mentally run the submitted code against concrete cases.
public class QuestionExample
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}

public class TopicInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
