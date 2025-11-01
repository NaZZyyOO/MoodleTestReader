namespace MoodleTestReader.Logic
{
    public class Question
    {
        public int Id { get; set; }
        public string question { get; set; }
        public int Points { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public string CorrectAnswer { get; set; }

        public virtual bool ValidateAnswer(object answer)
        {
            if (answer is not string userAnswer)
            {
                return false;
            }
            return userAnswer.Trim().Equals(CorrectAnswer, StringComparison.OrdinalIgnoreCase);
        }
    }
}