namespace MoodleTestReader.Models.Results;

// Відповідь + Набрані бали за це питання
public class AnswerWithScore
{
    public UserAnswer Answer { get; set; } = new UserAnswer();
    public int Points { get; set; }
}