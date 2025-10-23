# UI — WinForms форма Test

Форма керує життєвим циклом UI та інтеграцією з сервісами.
- `StartTestButton_Click` — старт тесту, таймер, сховати вибір, підготувати диктування.
- `ShowCurrentQuestionAsync` — рендер питання (Play), кнопка “Наступне”.
- `ExtractAnswerFromQuestionPanel` — збір відповіді з контролів.
- `NextButton_Click` — надіслати відповідь у TestManager.
- `TestReview` — перемикання “Почати”/“Огляд”.
- `TestReview_Click` — огляд (Review) у FlowLayoutPanel.
- `TestTimer_Tick` — завершення по часу.