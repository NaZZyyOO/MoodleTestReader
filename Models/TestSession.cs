using MoodleTestReader.Logic;
using MoodleTestReader.Models.Results;

namespace MoodleTestReader.Models
{
    public class TestSession
    {
        public Test TestTemplate { get; }
        private readonly List<Question> SelectedQuestions;
        private int _currentQuestionIndex;

        public Dictionary<int, int> Results { get; } = new();
        public Dictionary<int, UserAnswer> UserAnswers { get; } = new();

        public bool IsFinished => _currentQuestionIndex >= SelectedQuestions.Count;

        public int CurrentQuestionNumber
            => SelectedQuestions.Count == 0 ? 0 : Math.Min(_currentQuestionIndex + 1, SelectedQuestions.Count);

        public int TotalQuestions => SelectedQuestions.Count;

        public TestSession(Test testTemplate, List<Question> selectedQuestions)
        {
            TestTemplate = testTemplate ?? throw new ArgumentNullException(nameof(testTemplate));
            if (selectedQuestions is null) throw new ArgumentNullException(nameof(selectedQuestions));
            if (selectedQuestions.Any(q => q is null))
                throw new ArgumentException("Selected questions contain null entries.", nameof(selectedQuestions));

            // Захисна копія
            SelectedQuestions = selectedQuestions.ToList();

            // Перевірка унікальності ID (опційно)
            var duplicateId = SelectedQuestions
                .GroupBy(q => q.Id)
                .FirstOrDefault(g => g.Count() > 1)?.Key;
            if (duplicateId != null)
                throw new ArgumentException($"Duplicate question Id detected: {duplicateId}");
        }

        public Question? GetCurrentQuestion()
        {
            if (IsFinished) return null;
            return SelectedQuestions[_currentQuestionIndex];
        }

        public void SubmitAnswer(object? answer)
        {
            var currentQuestion = GetCurrentQuestion();
            if (currentQuestion == null) return;

            var score = 0;
            try
            {
                score = currentQuestion.ValidateAnswer(answer) ? Math.Max(0, currentQuestion.Points) : 0;
            }
            catch
            {
                // Логування доречно, а тут — трактуємо як неправильну відповідь
                score = 0;
            }

            Results[currentQuestion.Id] = score;

            var ua = BuildUserAnswer(currentQuestion, answer);
            UserAnswers[currentQuestion.Id] = ua;

            _currentQuestionIndex++;
        }

        private static UserAnswer BuildUserAnswer(Question q, object? answer)
        {
            var ua = new UserAnswer();
            switch (q)
            {
                case MultipleChoiceQuestion:
                    ua.Type = "multi";
                    ua.List = answer as List<string> ?? new List<string>();
                    break;

                case FillInBlankQuestion:
                    ua.Type = "text";
                    ua.Text = answer as string ?? string.Empty;
                    break;

                case TrueFalseQuestion:
                    ua.Type = "bool";
                    if (answer is bool b) ua.Bool = b;
                    else if (bool.TryParse(answer?.ToString(), out var parsed)) ua.Bool = parsed;
                    break;

                default:
                    ua.Type = "single";
                    ua.Text = answer as string ?? string.Empty;
                    break;
            }

            return ua;
        }

        public int GetScore() => Results.Values.Sum();
    }
}