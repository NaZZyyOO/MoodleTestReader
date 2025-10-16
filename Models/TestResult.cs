namespace MoodleTestReader.Models
{
    public class TestResult
    {
        public int Id { get; set; } // ID самого запису про результат
        public int UserId { get; set; }
        public int TestId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Результати: [ID питання, отриманий бал]
        public Dictionary<int, int> Results { get; set; } = new Dictionary<int, int>();
    }
}