# Data — доступ до БД, репозиторії, ініціалізація

Цей шар відповідає за:
- створення/ініціалізацію схеми БД (MySQL);
- читання/запис тестів та питань;
- збереження/читання результатів тестів (Results + ResultDetails);
- абстракцію доступу до результатів через репозиторій.

## Класи та інтерфейси

### DataLoader
- Призначення: ініціалізація БД (CREATE TABLE IF NOT EXISTS), завантаження тестів/питань, збереження тестів.
- Ключові методи:
  - `InitializeDatabase()` — створює таблиці Users, Tests, Questions, Results, ResultDetails.
  - `GetAvailableTests()` — завантажує Tests і пов'язані Questions.
  - `SaveTests(List<Test>)` — upsert тестів і питань.
- Примітки:
  - Questions.Options/CorrectAnswers серіалізуються JSON-ом.
  - Type питання: SingleChoice | MultipleChoice | FillInBlank | TrueFalse.

### ITestResultsRepository (інтерфейс)
- Контракт для роботи з результатами:
  - `SaveTestResult(TestResult result)` — зберігає “шапку” Results і рядки ResultDetails.
  - `GetUserTestResults(int userId)` — повертає всі результати користувача зі збиранням Details.

### MySqlTestResultsRepository (реалізація)
- Параметри підключення: з `App.config` → connectionStrings["MoodleDb"].
- Збереження:
  1) INSERT у Results (з JSON fallback в DetailedResults).
  2) INSERT усіх відповідних рядків у ResultDetails.
- Читання:
  - SELECT Results по UserId → потім SELECT ResultDetails по списку ResultId → збір у `TestResult.Details` + синхронізація `TestResult.Results`.

## Таблиці
- Results:
  - Id (PK), UserId, TestId, StartTime, EndTime, DetailedResults (JSON fallback).
- ResultDetails:
  - Id (PK), ResultId (FK → Results.Id), QuestionId,
  - AnswerType (single|multi|text|bool),
  - AnswerText (nullable),
  - AnswerBool (nullable, 0/1),
  - AnswerList (nullable JSON-масив),
  - Points (int).
- Індекси/рекомендації:
  - Results(UserId, TestId, EndTime).
  - ResultDetails(ResultId).
  - Унікальність (ResultId, QuestionId) — бажано, щоб виключити дублікати.

## Потоки даних
- Збереження результату:
  - Logic/TestManager збирає `AnswerWithScore` для кожного питання → репозиторій записує заголовок + деталі.
- Огляд результату:
  - UI читає `User.TestResults` (оновлюється після збереження) і рендерить Review через Details.

## Налаштування
- ConnectionString винесено у App.config. Не зберігайте пароль у коді.
- Для міграцій розгляньте перехід на EF Core або SQL-скрипти у каталозі db/migrations.