using System.Text.Json;
using PrepBD.Api.Models;

namespace PrepBD.Api.Services;

public class QuestionService
{
    private readonly List<Question> _questions = new();
    private readonly Dictionary<string, TopicInfo> _topics;
    private static readonly Random _random = new();

    private static readonly Dictionary<string, (string Name, string Icon, string Description)> TopicMeta = new()
    {
        ["data-structures"] = ("Data Structures", "🏗️", "Arrays, Linked Lists, Trees, Graphs, Sorting & Searching"),
        ["basic-programming"] = ("Basic Programming", "💻", "Variables, Loops, Functions, Recursion, Output Tracing"),
        ["oop"] = ("OOP", "🎯", "Encapsulation, Inheritance, Polymorphism, SOLID Principles"),
        ["dbms"] = ("DBMS", "🗄️", "SQL Queries, Normalization, ER Diagrams, ACID, Transactions"),
        ["operating-systems"] = ("Operating Systems", "⚙️", "Processes, Scheduling, Memory, Deadlocks, Synchronization"),
        ["networking"] = ("Networking", "🌐", "OSI Model, TCP/UDP, HTTP, DNS, Subnetting"),
        ["linux-commands"] = ("Linux Commands", "🐧", "File Operations, Permissions, Piping, Process Management"),
        ["git-github"] = ("Git & GitHub", "🔀", "Branching, Merging, Rebasing, Pull Requests, Workflows"),
        ["ai-ml"] = ("AI & ML Basics", "🤖", "Supervised/Unsupervised, Overfitting, Neural Networks"),
        ["analytical-reasoning"] = ("Analytical Reasoning", "🧩", "Logic Puzzles, Patterns, Quantitative Aptitude")
    };

    public QuestionService(IWebHostEnvironment env)
    {
        _topics = new Dictionary<string, TopicInfo>();
        var dataPath = Path.Combine(env.ContentRootPath, "Data", "Questions");

        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
            return;
        }

        foreach (var file in Directory.GetFiles(dataPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var questions = JsonSerializer.Deserialize<List<Question>>(json);
                if (questions != null)
                {
                    var slug = Path.GetFileNameWithoutExtension(file);
                    foreach (var q in questions)
                    {
                        q.TopicSlug = slug;
                        if (TopicMeta.TryGetValue(slug, out var meta))
                        {
                            q.Topic = meta.Name;
                        }
                    }
                    _questions.AddRange(questions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading question file {file}: {ex.Message}");
            }
        }

        // Build topic info
        foreach (var (slug, meta) in TopicMeta)
        {
            var count = _questions.Count(q => q.TopicSlug == slug);
            _topics[slug] = new TopicInfo
            {
                Name = meta.Name,
                Slug = slug,
                Icon = meta.Icon,
                Description = meta.Description,
                QuestionCount = count
            };
        }
    }

    public List<TopicInfo> GetTopics()
    {
        return _topics.Values.OrderByDescending(t => t.QuestionCount).ToList();
    }

    public List<QuestionResponse> GetQuestions(string mode, string? topics, int count)
    {
        IEnumerable<Question> pool = _questions;

        switch (mode.ToLower())
        {
            case "single":
            case "selected":
                if (!string.IsNullOrEmpty(topics))
                {
                    var topicSlugs = topics.Split(',').Select(t => t.Trim().ToLower()).ToHashSet();
                    pool = pool.Where(q => topicSlugs.Contains(q.TopicSlug));
                }
                break;
            case "mixed":
            default:
                // Use all questions
                break;
        }

        var selected = pool
            .OrderBy(_ => _random.Next())
            .Take(count)
            .Select(q => new QuestionResponse
            {
                Id = q.Id,
                Topic = q.Topic,
                TopicSlug = q.TopicSlug,
                Subtopic = q.Subtopic,
                Type = q.Type,
                Difficulty = q.Difficulty,
                QuestionText = q.QuestionText,
                Hints = q.Hints,
                Examples = q.Examples,
                LeetCodeUrl = q.LeetCodeUrl
            })
            .ToList();

        return selected;
    }

    public Question? GetQuestionById(string id)
    {
        return _questions.FirstOrDefault(q => q.Id == id);
    }
}
