using System.Globalization;
using System.Speech.Recognition;

namespace MoodleTestReader.Services
{
    // Слухач голосових команд. Працює тільки коли активовано (TTS увімкнений).
    // Режим Selection: команди для вибору/старту/огляду тесту.
    public class VoiceCommandService : IDisposable
    {
        private readonly Form _hostForm;
        private readonly Action<VoiceCommand> _onCommand;
        private readonly Func<List<string>> _getTestNames;

        private SpeechRecognitionEngine? _engine;
        private CultureInfo _culture = new CultureInfo("uk-UA");
        private bool _active;
        private bool _inSelectionMode = true;
        private List<string> _cachedNames = new();

        public VoiceCommandService(Form hostForm, Action<VoiceCommand> onCommand, Func<List<string>> getTestNames)
        {
            _hostForm = hostForm;
            _onCommand = onCommand;
            _getTestNames = getTestNames;

            TryInitEngine();
        }

        public void Dispose()
        {
            try { Stop(); } catch { }
            _engine?.Dispose();
        }

        // Вмикання/вимикання слухача (керується прапорцем TTS)
        public void SetActive(bool active)
        {
            _active = active;
            if (_active)
            {
                EnsureSelectionMode(); // за замовчуванням — на екрані вибору
                Start();
            }
            else
            {
                Stop();
            }
        }

        // Викликати на екрані вибору тесту
        public void OnSelectionScreen(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
            UpdateSelectionGrammar();
        }

        // Викликати на старті тесту
        public void OnTestStarted(bool activeNow)
        {
            _inSelectionMode = false;
            SetActive(activeNow);
            // наразі не додаємо in-test команди — вимога була про екран вибору
        }

        // Викликати після завершення тесту (повернулися на екран вибору)
        public void OnTestFinished(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
            UpdateSelectionGrammar();
        }

        // Оновити граматику імен тестів (при зміні списку)
        public void UpdateSelectionGrammar()
        {
            if (_engine == null) return;
            if (!_inSelectionMode) return;

            var names = _getTestNames() ?? new List<string>();
            var normalized = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Якщо назви не змінилися — не перебудовуємо
            if (normalized.Count == _cachedNames.Count &&
                normalized.All(_cachedNames.Contains))
            {
                return;
            }

            _cachedNames = normalized;
            RebuildSelectionGrammar();
        }

        private void TryInitEngine()
        {
            try
            {
                // Пробуємо українську; якщо відсутній мовний пакет — fallback на en-US
                _engine = new SpeechRecognitionEngine(_culture);
            }
            catch
            {
                _culture = new CultureInfo("en-US");
                _engine = new SpeechRecognitionEngine(_culture);
            }

            _engine.SpeechRecognized += EngineOnSpeechRecognized;
            _engine.RecognizeCompleted += (_, __) => { /* ignore */ };

            _engine.SetInputToDefaultAudioDevice();

            // Початкова граматика — для екрана вибору
            RebuildSelectionGrammar();
        }

        private void Start()
        {
            if (_engine == null) return;
            try
            {
                if (_engine.AudioState == AudioState.Stopped || _engine.AudioState == AudioState.Silence)
                {
                    _engine.RecognizeAsync(RecognizeMode.Multiple);
                }
            }
            catch { /* already started or no device */ }
        }

        private void Stop()
        {
            if (_engine == null) return;
            try { _engine.RecognizeAsyncCancel(); } catch { }
            try { _engine.RecognizeAsyncStop(); } catch { }
        }

        private void EnsureSelectionMode()
        {
            _inSelectionMode = true;
        }

        private void RebuildSelectionGrammar()
        {
            if (_engine == null) return;

            try
            {
                foreach (var g in _engine.Grammars.ToList())
                    _engine.UnloadGrammar(g);
            }
            catch { }

            var grammars = BuildSelectionGrammars(_cachedNames, _culture);
            foreach (var g in grammars)
            {
                try { _engine.LoadGrammar(g); } catch { }
            }
        }

        // Команди екрана вибору: старт, огляд, навігація, вибір тесту за назвою, керування TTS, вихід
        private static IEnumerable<Grammar> BuildSelectionGrammars(List<string> testNames, CultureInfo culture)
        {
            var grammars = new List<Grammar>();

            // 1) Старт/Огляд/Вихід/Увімкнути/Вимкнути/Навігація
            {
                var builder = new Choices();

                // Українські варіанти
                builder.Add(new SemanticResultValue("почати тест", "start"));
                builder.Add(new SemanticResultValue("почати", "start"));

                builder.Add(new SemanticResultValue("огляд тесту", "review"));
                builder.Add(new SemanticResultValue("огляд", "review"));
                builder.Add(new SemanticResultValue("перегляд тесту", "review"));
                builder.Add(new SemanticResultValue("перегляд", "review"));

                builder.Add(new SemanticResultValue("наступний тест", "next"));
                builder.Add(new SemanticResultValue("попередній тест", "prev"));
                builder.Add(new SemanticResultValue("перший тест", "first"));
                builder.Add(new SemanticResultValue("останній тест", "last"));

                builder.Add(new SemanticResultValue("увімкнути озвучення", "tts_on"));
                builder.Add(new SemanticResultValue("вимкнути озвучення", "tts_off"));

                builder.Add(new SemanticResultValue("вийти", "exit"));
                builder.Add(new SemanticResultValue("закрити застосунок", "exit"));

                // Англійські дублікати (fallback на en-US)
                builder.Add(new SemanticResultValue("start test", "start"));
                builder.Add(new SemanticResultValue("start", "start"));
                builder.Add(new SemanticResultValue("review test", "review"));
                builder.Add(new SemanticResultValue("review", "review"));
                builder.Add(new SemanticResultValue("next test", "next"));
                builder.Add(new SemanticResultValue("previous test", "prev"));
                builder.Add(new SemanticResultValue("first test", "first"));
                builder.Add(new SemanticResultValue("last test", "last"));
                builder.Add(new SemanticResultValue("enable speech", "tts_on"));
                builder.Add(new SemanticResultValue("disable speech", "tts_off"));
                builder.Add(new SemanticResultValue("exit", "exit"));
                builder.Add(new SemanticResultValue("close app", "exit"));

                var gb = new GrammarBuilder { Culture = culture };
                gb.Append(new SemanticResultKey("cmd", builder));
                grammars.Add(new Grammar(gb) { Name = "core_commands" });
            }

            // 2) Вибір тесту за назвою: "вибрати тест {назва}" / "обрати тест {назва}" / "select test {name}"
            if (testNames.Count > 0)
            {
                var namesChoices = new Choices(testNames.ToArray());

                foreach (var phrase in new[] { "вибрати тест", "обрати тест", "select test" })
                {
                    var gb = new GrammarBuilder { Culture = culture };
                    gb.Append(phrase);
                    gb.Append(new SemanticResultKey("testName", namesChoices));
                    grammars.Add(new Grammar(gb) { Name = $"select_by_name_{phrase}" });
                }
            }

            return grammars;
        }

        private void EngineOnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            if (!_active) return; // вимкнено правилами
            if (e.Result == null) return;
            if (e.Result.Confidence < 0.70f) return; // простий поріг впевненості

            var cmd = new VoiceCommand { Type = VoiceCommandType.None };

            // Команди з SemanticResultKey "cmd"
            if (e.Result.Semantics.ContainsKey("cmd"))
            {
                var val = e.Result.Semantics["cmd"].Value?.ToString() ?? string.Empty;
                cmd.Type = val switch
                {
                    "start" => VoiceCommandType.StartTest,
                    "review" => VoiceCommandType.ReviewTest,
                    "next" => VoiceCommandType.NextTest,
                    "prev" => VoiceCommandType.PreviousTest,
                    "first" => VoiceCommandType.FirstTest,
                    "last" => VoiceCommandType.LastTest,
                    "tts_on" => VoiceCommandType.EnableTts,
                    "tts_off" => VoiceCommandType.DisableTts,
                    "exit" => VoiceCommandType.ExitApp,
                    _ => VoiceCommandType.None
                };
            }
            // Вибір тесту за назвою
            else if (e.Result.Semantics.ContainsKey("testName"))
            {
                cmd.Type = VoiceCommandType.SelectTestByName;
                cmd.Argument = e.Result.Semantics["testName"].Value?.ToString();
            }
            else
            {
                // як fallback — пробуємо за текстом
                var text = e.Result.Text?.ToLowerInvariant() ?? string.Empty;
                if (text.Contains("почати") || text.Contains("start")) cmd.Type = VoiceCommandType.StartTest;
                else if (text.Contains("огляд") || text.Contains("review")) cmd.Type = VoiceCommandType.ReviewTest;
                else if (text.Contains("наступ") || text.Contains("next")) cmd.Type = VoiceCommandType.NextTest;
                else if (text.Contains("поперед") || text.Contains("previous")) cmd.Type = VoiceCommandType.PreviousTest;
                else if (text.Contains("увімк") || text.Contains("enable")) cmd.Type = VoiceCommandType.EnableTts;
                else if (text.Contains("вимк") || text.Contains("disable")) cmd.Type = VoiceCommandType.DisableTts;
                else if (text.Contains("вийти") || text.Contains("exit") || text.Contains("close")) cmd.Type = VoiceCommandType.ExitApp;
            }

            if (cmd.Type == VoiceCommandType.None) return;

            try
            {
                // Перекидаємо в UI-потік форми
                _hostForm.BeginInvoke(_onCommand, cmd);
            }
            catch { /* ignore */ }
        }
    }
}