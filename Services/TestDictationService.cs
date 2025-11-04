using MoodleTestReader.Logic;
using MoodleTestReader.Speech;

namespace MoodleTestReader.Services
{
    public class TestDictationService : IDisposable
    {
        private readonly Form _hostForm;
        private readonly GoogleTranslateTts _tts = new GoogleTranslateTts();
        private readonly CheckBox _toggle;
        private CancellationTokenSource _cts = new();
        private bool _enabled;

        // Налаштування
        private const int PauseAfterQuestionMs = 700;
        private const int PauseBetweenOptionsMs = 250;
        private const bool AnnounceCounts = false;

        private int _questionNumber;
        private int _totalQuestions;
        
        public bool IsEnabled => _enabled;
        public event EventHandler<bool>? EnabledChanged;

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
                EnabledChanged?.Invoke(this, _enabled);

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

            // Макс. швидкість
            if (_tts is GoogleTranslateTts g) g.Speed = 1.0;
        }

        // Дозволити керувати зовнішньо (напр., з голосових команд)
        public void SetEnabled(bool value)
        {
            if (_toggle.Checked == value) return;
            _toggle.Checked = value; // тригерне подію
        }

        private void RepositionToggle()
        {
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
            _toggle.Visible = false;

            _totalQuestions = totalQuestions;
            _questionNumber = 1;

            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            _ = _tts.CancelAsync();
        }

        public async Task OnQuestionShownAsync(Question q)
        {
            if (!_enabled || q == null) return;

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
                    PauseAfterQuestionMs,
                    PauseBetweenOptionsMs,
                    AnnounceCounts,
                    _cts.Token
                );
            }
            catch (OperationCanceledException) { }
            catch { }
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
}