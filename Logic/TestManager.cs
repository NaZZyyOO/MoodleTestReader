using MoodleTestReader.Data;
using MoodleTestReader.Models;

namespace MoodleTestReader.Logic
{
    public class TestManager
    {
        private readonly DataLoader _dataLoader;
        private readonly Dictionary<int, Test> _testTemplates; // Шаблони тестів: Id -> Test з усіма запитаннями
        private readonly Dictionary<int, TestSession> _userSessions; // Сеанси користувачів: User -> TestSession

        public TestManager()
        {
            _dataLoader = new DataLoader();
            _testTemplates = new Dictionary<int, Test>();
            _userSessions = new Dictionary<int, TestSession>();
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
            
            var testResult = new TestResult
            {
                UserId = user.Id,
                TestId = session.TestTemplate.Id,
                StartTime = startTime,
                EndTime = DateTime.Now, // Фіксуємо час завершення
                Results = session.Results // Беремо детальні результати з сесії
            };
            
            _dataLoader.SaveTestResult(testResult);
            

            _userSessions.Remove(user.Id); // Очищаємо сеанс після збереження
        }
    }
    
}