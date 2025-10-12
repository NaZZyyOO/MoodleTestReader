namespace MoodleTestReader.Logic
{
    public class TrueFalseQuestion : Question
    {
        public bool Answer { get; set; }

        public override bool ValidateAnswer(object answer)
        {
            return bool.TryParse(answer?.ToString(), out var userAnswer) && userAnswer == (bool)answer;
        }
    }
}