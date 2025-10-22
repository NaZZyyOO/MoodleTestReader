using MoodleTestReader.Logic;

namespace MoodleTestReader.Models;

public class TestSession
{
    public Test TestTemplate { get; }
    private List<Question> SelectedQuestions { get; }
    private int _currentQuestionIndex;
    public Dictionary<int, int> Results { get; } = new Dictionary<int, int>();
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
        var score = currentQuestion.ValidateAnswer(answer) ? currentQuestion.Points : 0;
        Results[currentQuestion.Id] = score;
        _currentQuestionIndex++;
    }

    public int GetScore() => Results.Values.Sum();
}