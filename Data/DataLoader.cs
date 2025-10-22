using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using MoodleTestReader.Models;
using MoodleTestReader.Logic;
using MoodleTestReader.Models.Results;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MoodleTestReader.Data
{
    public class DataLoader
    {
        private const string ConnectionString = "Server=localhost;Database=MoodleTestReader;Uid=root;Pwd=XP18ruthenian;Port=3306";

        public DataLoader()
        {
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Username VARCHAR(50) NOT NULL UNIQUE,
                        Password VARCHAR(64) NOT NULL
                    )";
                const string createTestsTable = @"
                    CREATE TABLE IF NOT EXISTS Tests (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        TestName VARCHAR(100) NOT NULL,
                        TimeLimit INT NOT NULL
                    )";
                const string createQuestionsTable = @"
                    CREATE TABLE IF NOT EXISTS Questions (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        TestId INT,
                        Type VARCHAR(50),
                        Description TEXT,
                        CorrectAnswer TEXT,
                        CorrectAnswers TEXT,
                        Points INT NOT NULL,
                        Options TEXT,
                        FOREIGN KEY (TestId) REFERENCES Tests(Id)
                    )";
                const string createResultsTable = @"
                    CREATE TABLE IF NOT EXISTS Results (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        UserId INT NOT NULL,
                        TestId INT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME NOT NULL,
                        DetailedResults TEXT,
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    )";
                // НОВА нормалізована таблиця з деталями по кожному питанню
                const string createResultDetailsTable = @"
                    CREATE TABLE IF NOT EXISTS ResultDetails (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        ResultId INT NOT NULL,
                        QuestionId INT NOT NULL,
                        AnswerType VARCHAR(16) NOT NULL,
                        AnswerText TEXT NULL,
                        AnswerBool TINYINT(1) NULL,
                        AnswerList TEXT NULL, -- JSON масив
                        Points INT NOT NULL,
                        FOREIGN KEY (ResultId) REFERENCES Results(Id)
                    )";

                using (var command = new MySqlConnection(ConnectionString))
                {
                    command.Open();
                    foreach (var sql in new[] { createUsersTable, createTestsTable, createQuestionsTable, createResultsTable, createResultDetailsTable })
                    {
                        using var cmd = new MySqlCommand(sql, command);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void CreateUser(string username, string password)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string query = "INSERT INTO Users (Username, Password) VALUES (@username, @password)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", HashPassword(password));
                    command.ExecuteNonQuery();
                }
            }
        }

        public User AuthenticateUser(string username, string password)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string query = "SELECT Id, Username, Password FROM Users WHERE Username = @username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var storedHash = reader["Password"].ToString();
                            if (storedHash == HashPassword(password))
                            {
                                return new User
                                (
                                    Convert.ToInt32(reader["Id"]),
                                    reader["Username"].ToString(),
                                    storedHash
                                );
                            }
                        }
                    }
                }
            }
            return null;
        }

        public bool UserExists(string username)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string query = "SELECT COUNT(*) FROM Users WHERE Username = @username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    return Convert.ToInt64(command.ExecuteScalar()) > 0;
                }
            }
        }

        public List<Test> GetAvailableTests()
        {
            var tests = new List<Test>();
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string query = "SELECT Id, TestName, TimeLimit FROM Tests";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var test = new Test(
                                Convert.ToInt32(reader["Id"]),
                                reader["TestName"].ToString(),
                                new List<Question>(),
                                Convert.ToInt32(reader["TimeLimit"])
                            );
                            tests.Add(test);
                        }
                    }
                }

                foreach (var test in tests)
                {
                    const string questionQuery = "SELECT Id, Type, Description, CorrectAnswer, CorrectAnswers, Points, Options FROM Questions WHERE TestId = @testId";
                    using (var command = new MySqlCommand(questionQuery, connection))
                    {
                        command.Parameters.AddWithValue("@testId", test.Id);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Question question;
                                var type = reader["Type"].ToString();
                                var options = !string.IsNullOrEmpty(reader["Options"].ToString())
                                    ? JsonSerializer.Deserialize<List<string>>(reader["Options"].ToString())
                                    : new List<string>();

                                switch (type)
                                {
                                    case "SingleChoice":
                                        question = new Question
                                        {
                                            Id = Convert.ToInt32(reader["Id"]),
                                            question = reader["Description"].ToString(),
                                            Points = Convert.ToInt32(reader["Points"]),
                                            Options = options,
                                            CorrectAnswer = reader["CorrectAnswer"].ToString()
                                        };
                                        break;
                                    case "MultipleChoice":
                                        question = new MultipleChoiceQuestion
                                        {
                                            Id = Convert.ToInt32(reader["Id"]),
                                            question = reader["Description"].ToString(),
                                            Points = Convert.ToInt32(reader["Points"]),
                                            Options = options,
                                            CorrectAnswers = !string.IsNullOrEmpty(reader["CorrectAnswers"].ToString())
                                                ? JsonSerializer.Deserialize<List<string>>(reader["CorrectAnswers"].ToString())
                                                : new List<string>()
                                        };
                                        break;
                                    case "FillInBlank":
                                        question = new FillInBlankQuestion
                                        {
                                            Id = Convert.ToInt32(reader["Id"]),
                                            question = reader["Description"].ToString(),
                                            Points = Convert.ToInt32(reader["Points"]),
                                            Options = options,
                                            CorrectAnswers = !string.IsNullOrEmpty(reader["CorrectAnswers"].ToString())
                                                ? JsonSerializer.Deserialize<List<string>>(reader["CorrectAnswers"].ToString())
                                                : new List<string>()
                                        };
                                        break;
                                    case "TrueFalse":
                                        question = new TrueFalseQuestion
                                        {
                                            Id = Convert.ToInt32(reader["Id"]),
                                            question = reader["Description"].ToString(),
                                            Points = Convert.ToInt32(reader["Points"]),
                                            Options = options,
                                            Answer = bool.Parse(reader["CorrectAnswer"].ToString())
                                        };
                                        break;
                                    default:
                                        continue;
                                }
                                test.Questions.Add(question);
                            }
                        }
                    }
                    Console.WriteLine($"Test {test.TestName} (Id: {test.Id}) loaded with {test.Questions.Count} questions");
                }
            }
            return tests;
        }

        public void SaveTests(List<Test> tests)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                foreach (var test in tests)
                {
                    const string testQuery = @"
                        INSERT INTO Tests (Id, TestName, TimeLimit) VALUES (@id, @testName, @timeLimit)
                        ON DUPLICATE KEY UPDATE TestName = @testName, TimeLimit = @timeLimit";
                    using (var command = new MySqlCommand(testQuery, connection))
                    {
                        command.Parameters.AddWithValue("@id", test.Id == 0 ? null : test.Id);
                        command.Parameters.AddWithValue("@testName", test.TestName);
                        command.Parameters.AddWithValue("@timeLimit", test.TimeLimit);
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Test {test.TestName} saved with Id {test.Id}");

                        if (test.Id == 0)
                        {
                            command.CommandText = "SELECT LAST_INSERT_ID()";
                            test.Id = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }

                    Console.WriteLine($"Saving {test.Questions.Count} questions for Test {test.TestName} (Id: {test.Id})");
                    foreach (var question in test.Questions)
                    {
                        string type;
                        string correctAnswer = null;
                        string correctAnswers = null;
                        var options = JsonSerializer.Serialize(question.Options);

                        switch (question)
                        {
                            case MultipleChoiceQuestion mcq:
                                type = "MultipleChoice";
                                correctAnswers = JsonSerializer.Serialize(mcq.CorrectAnswers);
                                break;
                            case FillInBlankQuestion fibq:
                                type = "FillInBlank";
                                correctAnswers = JsonSerializer.Serialize(fibq.CorrectAnswers);
                                break;
                            case TrueFalseQuestion tfq:
                                type = "TrueFalse";
                                correctAnswer = tfq.Answer.ToString();
                                break;
                            default:
                                type = "SingleChoice";
                                correctAnswer = question.CorrectAnswer;
                                break;
                        }

                        const string questionQuery = @"
                            INSERT INTO Questions (Id, TestId, Type, Description, CorrectAnswer, CorrectAnswers, Points, Options)
                            VALUES (@id, @testId, @type, @description, @correctAnswer, @correctAnswers, @points, @options)
                            ON DUPLICATE KEY UPDATE
                                TestId = @testId,
                                Type = @type,
                                Description = @description,
                                CorrectAnswer = @correctAnswer,
                                CorrectAnswers = @correctAnswers,
                                Points = @points,
                                Options = @options";
                        using (var command = new MySqlCommand(questionQuery, connection))
                        {
                            command.Parameters.AddWithValue("@id", question.Id == 0 ? null : question.Id);
                            command.Parameters.AddWithValue("@testId", test.Id);
                            command.Parameters.AddWithValue("@type", type);
                            command.Parameters.AddWithValue("@description", question.question);
                            command.Parameters.AddWithValue("@correctAnswer", (object)correctAnswer ?? DBNull.Value);
                            command.Parameters.AddWithValue("@correctAnswers", (object)correctAnswers ?? DBNull.Value);
                            command.Parameters.AddWithValue("@points", question.Points);
                            command.Parameters.AddWithValue("@options", options);
                            try
                            {
                                command.ExecuteNonQuery();
                                if (question.Id == 0)
                                {
                                    command.CommandText = "SELECT LAST_INSERT_ID()";
                                    question.Id = Convert.ToInt32(command.ExecuteScalar());
                                }
                                Console.WriteLine($"Question '{question.question}' saved with Id {question.Id} for TestId {test.Id}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error saving question '{question.question}': {ex.Message}");
                                throw;
                            }
                        }
                    }
                }
            }
        }

        public void SaveTestResult(TestResult result)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                // 1) Вставляємо заголовок Results
                const string insertResult = @"
                    INSERT INTO Results (UserId, TestId, StartTime, EndTime, DetailedResults)
                    VALUES (@userId, @testId, @startTime, @endTime, @detailedResults)";
                using (var cmd = new MySqlCommand(insertResult, connection))
                {
                    // детальний JSON залишимо для резерву (можна ставити NULL)
                    var payload = new { Scores = result.Results, Details = result.Details };
                    var json = JsonConvert.SerializeObject(payload);

                    cmd.Parameters.AddWithValue("@userId", result.UserId);
                    cmd.Parameters.AddWithValue("@testId", result.TestId);
                    cmd.Parameters.AddWithValue("@startTime", result.StartTime);
                    cmd.Parameters.AddWithValue("@endTime", result.EndTime);
                    cmd.Parameters.AddWithValue("@detailedResults", json);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT LAST_INSERT_ID()";
                    result.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) Вставляємо деталізацію по кожному питанню
                const string insertDetail = @"
                    INSERT INTO ResultDetails (ResultId, QuestionId, AnswerType, AnswerText, AnswerBool, AnswerList, Points)
                    VALUES (@resultId, @questionId, @type, @text, @bool, @list, @points)";
                foreach (var kv in result.Details)
                {
                    var questionId = kv.Key;
                    var aws = kv.Value;
                    var ua = aws.Answer ?? new UserAnswer();

                    using var dcmd = new MySqlCommand(insertDetail, connection);
                    dcmd.Parameters.AddWithValue("@resultId", result.Id);
                    dcmd.Parameters.AddWithValue("@questionId", questionId);
                    dcmd.Parameters.AddWithValue("@type", ua.Type ?? "single");
                    dcmd.Parameters.AddWithValue("@text", (object?)ua.Text ?? DBNull.Value);
                    dcmd.Parameters.AddWithValue("@bool", ua.Bool.HasValue ? (ua.Bool.Value ? 1 : 0) : (object)DBNull.Value);
                    dcmd.Parameters.AddWithValue("@list", ua.List != null ? JsonConvert.SerializeObject(ua.List) : (object)DBNull.Value);
                    dcmd.Parameters.AddWithValue("@points", aws.Points);
                    dcmd.ExecuteNonQuery();
                }
            }
        }

        public List<TestResult> GetUserTestResults(int userId)
        {
            var testResults = new List<TestResult>();

            using (var connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                const string selectResults =
                    "SELECT Id, TestId, StartTime, EndTime, DetailedResults FROM Results WHERE UserId = @userId";
                using (var command = new MySqlCommand(selectResults, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var result = new TestResult
                        {
                            Id = reader.GetInt32("Id"),
                            UserId = userId,
                            TestId = reader.GetInt32("TestId"),
                            StartTime = reader.GetDateTime("StartTime"),
                            EndTime = reader.GetDateTime("EndTime")
                        };

                        // Опційно: з DetailedResults можна зробити fallback, але основне — тягнемо з ResultDetails
                        testResults.Add(result);
                    }
                }

                if (testResults.Count == 0) return testResults;

                // Підтягнути деталі для всіх знайдених ResultId одним запитом
                var ids = string.Join(",", testResults.Select(r => r.Id));
                var detailsSql = $@"
                    SELECT ResultId, QuestionId, AnswerType, AnswerText, AnswerBool, AnswerList, Points
                    FROM ResultDetails
                    WHERE ResultId IN ({ids})";

                using (var dcmd = new MySqlCommand(detailsSql, connection))
                using (var dreader = dcmd.ExecuteReader())
                {
                    var map = testResults.ToDictionary(r => r.Id, r => r);
                    while (dreader.Read())
                    {
                        var resId = dreader.GetInt32("ResultId");
                        if (!map.TryGetValue(resId, out var res)) continue;

                        var qId = dreader.GetInt32("QuestionId");
                        var type = dreader.GetString("AnswerType");
                        string? text = dreader["AnswerText"] == DBNull.Value ? null : dreader.GetString("AnswerText");
                        int? b = dreader["AnswerBool"] == DBNull.Value ? null : dreader.GetInt32("AnswerBool");
                        string? listJson = dreader["AnswerList"] == DBNull.Value ? null : dreader.GetString("AnswerList");
                        var points = dreader.GetInt32("Points");

                        var ua = new UserAnswer { Type = type };
                        if (type == "bool") ua.Bool = b.HasValue ? b.Value != 0 : null;
                        if (type == "text" || type == "single") ua.Text = text;
                        if (type == "multi") ua.List = !string.IsNullOrEmpty(listJson)
                            ? JsonConvert.DeserializeObject<List<string>>(listJson) ?? new List<string>()
                            : new List<string>();

                        res.Details[qId] = new AnswerWithScore { Answer = ua, Points = points };
                        res.Results[qId] = points; // для сумісності
                    }
                }
            }

            return testResults;
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}