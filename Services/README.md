# Services — сервіс диктування

## TestDictationService
- Інкапсулює логіку TTS і керування чекбоксом “Озвучувати питання”.
- Працює з `ITtsEngine`.
- Основні методи:
  - `OnTestSelected()` — показати перемикач.
  - `OnTestStarted(totalQuestions)` — сховати перемикач, скинути TTS.
  - `OnQuestionShownAsync(question)` — прочитати питання (якщо увімкнено).
  - `OnNextQuestion()` — скасувати поточне читання.
  - `OnTestFinishedAsync(score)` — озвучити підсумок, повернути перемикач.