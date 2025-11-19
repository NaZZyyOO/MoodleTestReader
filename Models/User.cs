using MoodleTestReader.Models.Results;

namespace MoodleTestReader.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsProfessor { get; set; }
        public List<int> ProfessorsTests { get; set; } =  new List<int>();
        
        // Словник: айді теста = Словник(айді запитання, на яке дається відповідь = кількість набраних балів)
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();

        public User(int id, string username, string password, bool isProfessor = false)
        {
            Id = id;
            Username = username;
            Password = password;
            IsProfessor = isProfessor;

        }
        
    }
}