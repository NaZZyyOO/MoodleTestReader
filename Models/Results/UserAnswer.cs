namespace MoodleTestReader.Models.Results;

// Універсальний контейнер для відповіді користувача
// Type: "single" | "multi" | "text" | "bool"
public class UserAnswer
{
    public string Type { get; set; } = "single";
    public string? Text { get; set; }            // для single/text
    public List<string>? List { get; set; }      // для multi
    public bool? Bool { get; set; }              // для bool
}