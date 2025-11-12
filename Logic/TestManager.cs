using MoodleTestReader.Data;
using MoodleTestReader.Models;
using MoodleTestReader.Models.Results;

namespace MoodleTestReader.Logic
{
    public class TestManager
    {
        private readonly Dictionary<int, Test> _testTemplates; // Шаблони тестів: Id -> Test з усіма запитаннями
        private readonly Dictionary<int, TestSession> _userSessions; // Сеанси користувачів: User -> TestSession

        public TestManager()
        {
            _testTemplates = new Dictionary<int, Test>();
            _userSessions = new Dictionary<int, TestSession>();
            LoadTestTemplates();
        }

        private void LoadTestTemplates()
        {
            var tests = DataLoader.GetAvailableTests();
            foreach (var test in tests)
            {
                _testTemplates[test.Id] = test;
            }
        }

        public List<Test> GetAvailableTests()
        {
            return _testTemplates.Values.ToList();
        }

        public void StartTestForUser(User user, int testId)
        {
            if (user.IsProfessor)
            {
                MessageBox.Show("Користувач є викладачем. Тести можуть проходити лише студенти.");
                return;
            }
            if (!_testTemplates.TryGetValue(testId, out var testTemplate))
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }

            var selectedQuestions = testTemplate.Questions;

            var session = new TestSession(testTemplate, selectedQuestions);
            _userSessions[user.Id] = session;
        }

        public Question GetCurrentQuestionForUser(User user)
        {
            if (!_userSessions.TryGetValue(user.Id, out var session))
            {
                return null;
            }

            return session.GetCurrentQuestion();
        }

        public void SubmitAnswerForUser(User user, object answer)
        {
            if (!_userSessions.TryGetValue(user.Id, out var session))
            {
                return;
            }

            session.SubmitAnswer(answer);
        }

        public void SaveResultsForUser(User user, out int score, DateTime startTime)
        {
            if (!_userSessions.TryGetValue(user.Id, out var session))
            {   
                score = 0;
                return;
            }

            score = session.GetScore();

            // Заповнюємо деталі з сесії
            var details = new Dictionary<int, AnswerWithScore>();
            foreach (var kv in session.Results) // questionId -> points
            {
                var qId = kv.Key;
                var pts = kv.Value;
                session.UserAnswers.TryGetValue(qId, out var ua);
                details[qId] = new AnswerWithScore
                {
                    Answer = ua ?? new UserAnswer(),
                    Points = pts
                };
            }
            
            var testResult = new TestResult
            {
                UserId = user.Id,
                TestId = session.TestTemplate.Id,
                StartTime = startTime,
                EndTime = DateTime.Now,
                Results = session.Results,
                Details = details
            };
            
            DataLoader.SaveTestResult(testResult);

            // Оновлюємо кеш користувача в пам’яті
            if (user.TestResults == null)
            {
                user.TestResults = new List<TestResult>();
            }

            var existing = user.TestResults.FirstOrDefault(r => r.TestId == testResult.TestId);
            if (existing != null)
            {
                existing.StartTime = testResult.StartTime;
                existing.EndTime = testResult.EndTime;
                existing.Results = testResult.Results;
                existing.Details = testResult.Details;
            }
            else
            {
                user.TestResults.Add(testResult);
            }

            _userSessions.Remove(user.Id);
        }
    }
}