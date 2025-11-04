using MoodleTestReader.Logic;

namespace MoodleTestReader.Models
{
    public class Test
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();
        public int TimeLimit { get; set; } // в хвилинах
        public int AuthorId { get; set; }

        public Test(int authorId, int id, string testName, List<Question> questions, int timeLimit)
        {
            AuthorId = authorId;
            Id = id;
            TestName = testName;
            Questions = questions;
            TimeLimit = timeLimit;
        }
    }
}