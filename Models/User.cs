namespace MoodleTestReader.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsProfessor { get; set; }
        public List<int> ProfessorsTests { get; set; }
        
        // Словник: айді теста = Словник(айді запитання, на яке дається відповідь = кількість набраних балів)
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();

        public User(int id, string username, string password)
        {
            this.Id = id;
            this.Username = username;
            this.Password = password;

        }
    }
}