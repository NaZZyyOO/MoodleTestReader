# MoodleTestReader — тестовий модуль з озвучкою та оглядом

Курсова робота з програмування на C#. Додаток дозволяє проходити тести з озвученням запитань, збереженням результатів у MySQL та подальшим нередагованим оглядом відповідей користувача.

## Можливості
- Типи питань: Single Choice, Multiple Choice, True/False, Fill in the Blank.
- Озвучення (TTS) без встановлення додаткових програм: неофіційний Google Translate TTS (uk/en), програвання через MCI (winmm).
- Автовизначення мови фрагментів (uk/en), префікси та нумерація — завжди українською.
- Огляд виконаного тесту: показ обраних відповідей користувача й балів за кожне питання (тільки перегляд).
- Збереження результатів у MySQL:
  - Results — “шапка” спроби (UserId, TestId, Start/EndTime).
  - ResultDetails — окремий рядок по кожному питанню: відповідь користувача та бали.

## Архітектура (шари)
- Models — доменні моделі (Test, Question*, User, TestResult, UserAnswer, AnswerWithScore).
- Data — доступ до БД (ініціалізація схеми, збереження/читання результатів, репозиторії).
- Logic — бізнес-логіка (TestManager, TestSession).
- Speech — рушій озвучення (ITtsEngine, GoogleTranslateTts).
- Services — сервіс диктування (TestDictationService) — інкапсулює TTS для UI.
- UI — WinForms-форма Test: відображення, навігація, взаємодія із сервісами.
- UI/Rendering — QuestionRenderer: уніфікований рендеринг питань у режимах Play/Review.

## Потік виконання
1. Логін → завантаження доступних тестів.
2. Вибір тесту:
   - якщо є попередні результати — показується “Огляд тесту”;
   - якщо немає — “Почати тест”.
3. Під час проходження:
   - UI рендерить питання (QuestionRenderer), збирає відповідь;
   - TestManager оцінює й зберігає у TestSession.
4. Озвучення керується TestDictationService:
   - перемикач “Озвучувати питання” у правому верхньому куті;
   - під час проходження — приховано.
5. Завершення:
   - запис у Results + ResultDetails;
   - оновлення User.TestResults у пам’яті;
   - кнопка “Огляд тесту” стає доступною.
6. Огляд:
   - Review-режим (усі контролі disabled/read-only);
   - показ відповіді користувача та балів за кожне питання.

## Синтез мовлення (TTS)
- Реалізація: `Speech/GoogleTranslateTts : ITtsEngine`.
- Особливості:
  - санітизація тексту, авто-визначення мови (uk/en);
  - максимальна швидкість, контрольовані паузи;
  - скасування відтворення без винятків у UI.
- Інтеграція — через `Services/TestDictationService`.

## База даних (MySQL)
Таблиці:
- Users (Id, Username, Password)
- Tests (Id, TestName, TimeLimit)
- Questions (Id, TestId, Type, Description, CorrectAnswer, CorrectAnswers, Points, Options)
- Results (Id, UserId, TestId, StartTime, EndTime, DetailedResults)
- ResultDetails (Id, ResultId, QuestionId, AnswerType, AnswerText, AnswerBool, AnswerList, Points)

Результати зберігаються нормалізовано: на кожне питання — один запис у ResultDetails із типом відповіді та набраними балами. DetailedResults містить JSON-пейлоад для сумісності.

## Налаштування
- Рядок підключення до БД у `App.config` (connectionStrings → name="MoodleDb").
- За потреби змініть параметри MySQL (сервер/порт/логін/пароль).

## Запуск локально
1. Встановіть MySQL і створіть БД `MoodleTestReader`.
2. Налаштуйте connectionStrings у App.config.
3. Відкрийте рішення у Visual Studio / Rider, зберіть і запустіть.
4. Під час першого запуску Data/ініціалізація створить таблиці.

## Структура
```
Data/                   // Доступ до БД, репозиторії, ініціалізація
Logic/                  // TestManager, TestSession
Models/                 // Доменно-орієнтовані моделі
Services/               // TestDictationService
Speech/                 // ITtsEngine, GoogleTranslateTts
UI/                     // Форма Test
UI/Rendering/           // QuestionRenderer, режими відображення
```

## Документування коду
- Увімкніть генерацію XML-коментарів у .csproj: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`.
- Додавайте `///` над публічними класами та методами (див. коментарі у файлах).

## Ліцензія
Навчальний проєкт. Використовуйте на власний розсуд.