namespace MoodleTestReader.Logic
{
    public class MultipleChoiceQuestion : Question
    {
        public List<string> CorrectAnswers { get; set; } = new List<string>();

        public override bool ValidateAnswer(object answer)
        {
            if (!(answer is List<string> selectedAnswers))
            {
                return false;
            }

            return selectedAnswers.OrderBy(x => x).SequenceEqual(CorrectAnswers.OrderBy(x => x));
        }
    }
}