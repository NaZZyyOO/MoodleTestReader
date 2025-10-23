# Models — доменні сутності

## Основні моделі
- `Test` — тест (Id, TestName, Questions, TimeLimit).
- `Question` (базовий) — поля: Id, question (текст), Points, Options, CorrectAnswer/CorrectAnswers; метод `ValidateAnswer(object answer)`.
  - Спадкоємці:
    - `MultipleChoiceQuestion` — кілька правильних;
    - `FillInBlankQuestion` — введення тексту;
    - `TrueFalseQuestion` — булеве;
    - `Question` як Single Choice — одна правильна відповідь.
- `User` — користувач (Id, Username, Password, IsProfessor, TestResults кешем у пам’яті).
- `UserAnswer` — уніфікована відповідь користувача:
  - Type: single|multi|text|bool
  - Text/List/Bool — залежно від типу.
- `AnswerWithScore` — пара { `Answer: UserAnswer`, `Points: int` }.
- `TestResult` — збережений результат спроби:
  - Results: Dictionary<QuestionId, Points>
  - Details: Dictionary<QuestionId, AnswerWithScore>.