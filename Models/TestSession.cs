using MoodleTestReader.Logic;
using MoodleTestReader.Models.Results;

namespace MoodleTestReader.Models;

public class TestSession
{
    public Test TestTemplate { get; }
    private List<Question> SelectedQuestions { get; }
    private int _currentQuestionIndex;

    // Бали по питаннях
    public Dictionary<int, int> Results { get; } = new Dictionary<int, int>();

    // Відповіді користувача по питаннях
    public Dictionary<int, UserAnswer> UserAnswers { get; } = new Dictionary<int, UserAnswer>();

    public int CurrentQuestionNumber => _currentQuestionIndex + 1;
    public int TotalQuestions => SelectedQuestions.Count;

    public TestSession(Test testTemplate, List<Question> selectedQuestions)
    {
        TestTemplate = testTemplate;
        SelectedQuestions = selectedQuestions;
        _currentQuestionIndex = 0;
    }

    public Question? GetCurrentQuestion()
    {
        if (_currentQuestionIndex >= SelectedQuestions.Count)
        {
            return null;
        }
        return SelectedQuestions[_currentQuestionIndex];
    }

    public void SubmitAnswer(object answer)
    {
        var currentQuestion = GetCurrentQuestion();
        if (currentQuestion == null) return;

        // Оцінювання
        var score = currentQuestion.ValidateAnswer(answer) ? currentQuestion.Points : 0;
        Results[currentQuestion.Id] = score;

        // Зберегти відповідь
        var ua = new UserAnswer();
        switch (currentQuestion)
        {
            case MultipleChoiceQuestion:
                ua.Type = "multi";
                ua.List = (answer as List<string>) ?? new List<string>();
                break;

            case FillInBlankQuestion:
                ua.Type = "text";
                ua.Text = answer as string ?? string.Empty;
                break;

            case TrueFalseQuestion:
                ua.Type = "bool";
                if (answer is bool b) ua.Bool = b;
                break;

            default:
                ua.Type = "single";
                ua.Text = answer as string ?? string.Empty;
                break;
        }
        UserAnswers[currentQuestion.Id] = ua;

        _currentQuestionIndex++;
    }

    public int GetScore() => Results.Values.Sum();
}