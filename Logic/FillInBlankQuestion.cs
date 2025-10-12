namespace MoodleTestReader.Logic
{
    public class FillInBlankQuestion : Question
    {
        public List<string> CorrectAnswers { get; set; } = new List<string>();

        public override bool ValidateAnswer(object answer)
        {
            if (answer is not string userAnswer)
            {
                return false;
            }

            return CorrectAnswers.Any(c => c.Trim().Equals(userAnswer.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}