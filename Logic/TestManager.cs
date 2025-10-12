using MoodleTestReader.Data;
using MoodleTestReader.Models;

namespace MoodleTestReader.Logic
{
    public class TestManager
    {
        private readonly DataLoader _dataLoader;
        private readonly Dictionary<int, Test> _testTemplates; // Шаблони тестів: Id -> Test з усіма запитаннями
        private readonly Dictionary<User, TestSession> _userSessions; // Сеанси користувачів: User -> TestSession

        public TestManager()
        {
            _dataLoader = new DataLoader();
            _testTemplates = new Dictionary<int, Test>();
            _userSessions = new Dictionary<User, TestSession>();
            LoadTestTemplates();
        }

        private void LoadTestTemplates()
        {
            var tests = _dataLoader.GetAvailableTests();
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
                throw new Exception("Користувач є викладачем. Тести можуть проходити лише студенти.");
            }
            if (!_testTemplates.TryGetValue(testId, out var testTemplate))
            {
                throw new Exception("Тест не знайдено.");
            }

            // Вибираємо випадково певну кількість запитань (наприклад, 5)
            var random = new Random();
            var selectedQuestions = testTemplate.Questions;

            var session = new TestSession(testTemplate, selectedQuestions);
            _userSessions[user] = session;
        }

        public Question GetCurrentQuestionForUser(User user)
        {
            if (!_userSessions.TryGetValue(user, out var session))
            {
                return null;
            }

            return session.GetCurrentQuestion();
        }

        public void SubmitAnswerForUser(User user, object answer)
        {
            if (!_userSessions.TryGetValue(user, out var session))
            {
                return;
            }

            session.SubmitAnswer(answer);
        }

        public void SaveResultsForUser(User user, out int score)
        {
            if (!_userSessions.TryGetValue(user, out var session))
            {   
                score = 0;
                return;
            }

            score = session.GetScore();
            _dataLoader.SaveTestResult(user.Id, session.TestTemplate.Id, score);

            if (!user.TestResults.TryGetValue(session.TestTemplate.Id, out var value))
            {
                value = new Dictionary<int, int>();
                user.TestResults[session.TestTemplate.Id] = value;
            }
            foreach (var result in session.Results)
            {
                value[result.Key] = result.Value;
            }

            _userSessions.Remove(user); // Очищаємо сеанс після збереження
        }
    }
    
}