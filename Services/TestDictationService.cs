using System.Drawing;
using MoodleTestReader.Logic;
using MoodleTestReader.Speech;

namespace MoodleTestReader.Services;

// Сервіс диктування: інкапсулює TTS і прапорець; керує хованням на час тесту.
public class TestDictationService : IDisposable
{
    private readonly Form _hostForm;
    private readonly GoogleTranslateTts _tts = new();
    private readonly CheckBox _toggle;
    private CancellationTokenSource _cts = new();
    private bool _enabled;

    // Швидше читання — коротші паузи
    private int _pauseAfterQuestionMs = 700;
    private int _pauseBetweenOptionsMs = 250;

    // Оголошення "Питання N з M" — вимкнено, щоб не було дублювань "Запитання"
    private bool _announceCounts = false;

    // Лічильники
    private int _questionNumber;
    private int _totalQuestions;

    public TestDictationService(Form hostForm)
    {
        _hostForm = hostForm;

        _toggle = new CheckBox
        {
            Text = "Озвучувати питання",
            AutoSize = true,
            Checked = false,
            Visible = false
        };
        _toggle.CheckedChanged += async (s, e) =>
        {
            _enabled = _toggle.Checked;
            if (!_enabled)
            {
                try { await _tts.CancelAsync(); } catch { }
                _cts.Cancel();
            }
        };

        _hostForm.Controls.Add(_toggle);
        _toggle.BringToFront();
        _hostForm.Resize += (_, __) => RepositionToggle();
        RepositionToggle();

        // Максимально швидко
        _tts.Speed = 1.0;
    }

    private void RepositionToggle()
    {
        // ПРАВИЙ ВЕРХ: 12 px від правого краю, 8 px зверху
        var x = _hostForm.ClientSize.Width - _toggle.Width - 12;
        var y = 8;
        _toggle.Location = new Point(Math.Max(8, x), y);
    }

    public void OnTestSelected()
    {
        _toggle.Visible = true;
        RepositionToggle();
    }

    public void OnTestStarted(int totalQuestions)
    {
        // Ховаємо прапорець на час проходження тесту
        _toggle.Visible = false;

        _totalQuestions = totalQuestions;
        _questionNumber = 1;

        // Скинемо будь-яке активне озвучення
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _ = _tts.CancelAsync();
    }

    public async Task OnQuestionShownAsync(Question q)
    {
        if (!_enabled || q == null) return;

        // Скасувати попереднє озвучення, не спливаючи помилками
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            await _tts.CancelAsync();
            await _tts.SpeakQuestionAsync(
                q.question,
                q.Options,
                _questionNumber,
                _totalQuestions,
                _pauseAfterQuestionMs,
                _pauseBetweenOptionsMs,
                _announceCounts,
                _cts.Token
            );
        }
        catch (OperationCanceledException) { /* тихо ігноруємо */ }
        catch { /* інші помилки — не валимо UI */ }
    }

    public void OnNextQuestion()
    {
        _questionNumber++;
        _cts.Cancel();
        _ = _tts.CancelAsync();
    }

    public async Task OnTestFinishedAsync(int score)
    {
        if (_enabled)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                await _tts.CancelAsync();
                await _tts.SpeakScoreAsync(score, _cts.Token);
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        // Повернути перемикач після завершення
        _toggle.Visible = true;
        RepositionToggle();
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            _cts.Dispose();
            _tts.Dispose();
        }
        catch { }
    }
}