# Speech — синтез мовлення (TTS)

## Інтерфейс
- `ITtsEngine` — контракт рушія озвучення:
  - `SpeakQuestionAsync(...)`
  - `SpeakScoreAsync(score)`
  - `CancelAsync()`

## Реалізація
- `GoogleTranslateTts : ITtsEngine`
  - HTTP до неофіційного Google Translate TTS (tl=uk|en, client=tw-ob, ttsspeed).
  - Відтворення через MCI (winmm) із тимчасових MP3.
  - Санітизація тексту; авто-визначення мови фрагментів; префікси і нумерацію озвучує українською.
  - Скасування без винятків у UI.