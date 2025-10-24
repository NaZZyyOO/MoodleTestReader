using System.Text;
using MoodleTestReader.Data;
using MoodleTestReader.UI;

namespace MoodleTestReader;

static class MoodleTestReader
{
    [STAThread]
    static void Main()
    {   
        // Встановити кодування консолі на UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var tests = new List<Models.Test>();

        /*var test = new Models.Test(
            1,
            "Тест з C#",
            new List<Question> {
                new Question
                {
                    question = "Який модифікатор доступу найширший?", Points = 5,
                    Options = new List<string> { "public", "private", "protected", "internal" },
                    CorrectAnswer = "public"
                },
                new FillInBlankQuestion
                    { question = "Яка мова в .NET?", Points = 5, CorrectAnswers = new List<string> { "C#", "c#" } },
                new MultipleChoiceQuestion
                {
                    question = "Які модифікатори є в C#?", Points = 6,
                    Options = new List<string> { "public", "private", "protected", "internal" },
                    CorrectAnswers = new List<string> { "public", "private" }
                },
                new TrueFalseQuestion
                {
                    question = "C# є об’єктно-орієнтованою мовою?", Points = 4, Answer = true,
                    Options = new List<string> { "True", "False" }
                }
            },
            10
            );
        tests.Add(test);*/
        var dataLoader = new DataLoader();
        dataLoader.SaveTests(tests);
        Console.WriteLine("Тести збережено в БД.");
        dataLoader.InitializeDatabase();
        ApplicationConfiguration.Initialize();
        Application.Run(new Test());
    }
}