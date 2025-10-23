# Logic — бізнес-логіка: TestManager, TestSession

## Призначення шару
- Інкапсулює процес проходження тесту: старт, поточне питання, прийом відповіді, підрахунок і збереження результатів.
- Працює з моделями (Models) та репозиторієм результатів (Data).

## Класи

### TestManager
- Відповідальність:
  - завантажити шаблони тестів (у пам'ять);
  - керувати сесіями користувачів (UserId → TestSession);
  - на старті ініціалізувати TestSession для користувача;
  - на Submit — делегувати оцінювання TestSession;
  - на завершенні — зібрати `AnswerWithScore`, зберегти через репозиторій, оновити `User.TestResults` в пам'яті.
- Ключові методи:
  - `StartTestForUser(User user, int testId)`
  - `Question GetCurrentQuestionForUser(User user)`
  - `SubmitAnswerForUser(User user, object answer)`
  - `SaveResultsForUser(User user, out int score, DateTime startTime)`

Зв’язки: використовує DataLoader (для тестів) та ITestResultsRepository (для результатів).

### TestSession
- Зберігає:
  - список/порядок обраних питань;
  - індекс поточного;
  - `Results: Dictionary<QuestionId, Points>`;
  - `UserAnswers: Dictionary<QuestionId, UserAnswer>`.
- Методи:
  - `GetCurrentQuestion()`
  - `SubmitAnswer(object answer)` — рахує бали (`Question.ValidateAnswer`) і зберігає сирі відповіді у `UserAnswers`.
  - `GetScore()` — сума балів.