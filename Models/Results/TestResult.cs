using MoodleTestReader.Models.Results;

namespace MoodleTestReader.Models.Results
{
    public class TestResult
    {
        public int Id { get; set; } // ID самого запису про результат
        public int UserId { get; set; }
        public int TestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Бали: [ID питання, отриманий бал] (для сумісності і швидких підрахунків)
        public Dictionary<int, int> Results { get; set; } = new Dictionary<int, int>();

        // Деталі (основне): [ID питання, {Answer, Points}]
        public Dictionary<int, AnswerWithScore> Details { get; set; } = new Dictionary<int, AnswerWithScore>();
    }
}