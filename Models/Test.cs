using MoodleTestReader.Logic;

namespace MoodleTestReader.Models
{
    public class Test
    {
        public int Id { get; set; }
        public string TestName { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();
        public int TimeLimit { get; set; } // в хвилинах

        public Test(int id, string testName, List<Question> questions, int timeLimit)
        {
            Id = id;
            TestName = testName;
            Questions = questions;
            TimeLimit = timeLimit;
            
        }
        public Test(int id, string testName, List<Question> questions)
        {
            Id = id;
            TestName = testName;
            Questions = questions;
        }
    }
}