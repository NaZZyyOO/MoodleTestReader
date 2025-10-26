using System.Globalization;
using System.Speech.Recognition;

namespace MoodleTestReader.Services
{
    // Слухач голосових команд. Працює тільки коли активовано (TTS увімкнений)
    public class VoiceCommandService : IDisposable
    {
        private readonly Form _hostForm;
        private readonly Action<VoiceCommand> _onCommand;
        private readonly Func<List<string>> _getTestNames;

        private SpeechRecognitionEngine? _engine;
        private RecognizerInfo? _recognizer;
        private bool _available;   // є встановлений розпізнавач і вхідний аудіопристрій
        private bool _active;      // ввімкнений користувачем (через TTS)
        private bool _inSelectionMode = true;

        private List<string> _cachedNames = new();

        public bool IsAvailable => _available;

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
            try { _engine?.Dispose(); } catch { }
        }

        public void SetActive(bool active)
        {
            _active = active && _available;
            if (_active) Start();
            else Stop();
        }

        public void OnSelectionScreen(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
            UpdateSelectionGrammar();
        }

        public void OnTestStarted(bool activeNow)
        {
            _inSelectionMode = false;
            SetActive(activeNow);
            // наразі граматика тільки для екрана вибору
        }

        public void OnTestFinished(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
            UpdateSelectionGrammar();
        }

        public void UpdateSelectionGrammar()
        {
            if (!_available || _engine == null || !_inSelectionMode) return;

            var names = _getTestNames() ?? new List<string>();
            var normalized = names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count == _cachedNames.Count && normalized.All(_cachedNames.Contains))
                return;

            _cachedNames = normalized;
            RebuildSelectionGrammar();
        }

        private void TryInitEngine()
        {
            try
            {
                var installed = SpeechRecognitionEngine.InstalledRecognizers();
                if (installed == null || installed.Count == 0)
                {
                    _available = false;
                    _engine = null;
                    return;
                }

                // Пріоритет підбору розпізнавача
                string[] preferred = { "uk-UA", "uk", "en-US", "en" };

                _recognizer =
                    installed.FirstOrDefault(r => r.Culture.Name.Equals("uk-UA", StringComparison.OrdinalIgnoreCase)) ??
                    installed.FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName.Equals("uk", StringComparison.OrdinalIgnoreCase)) ??
                    installed.FirstOrDefault(r => r.Culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase)) ??
                    installed.FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase)) ??
                    installed.FirstOrDefault();

                if (_recognizer == null)
                {
                    _available = false;
                    _engine = null;
                    return;
                }

                _engine = new SpeechRecognitionEngine(_recognizer);

                // Спроба підключити мікрофон
                try
                {
                    _engine.SetInputToDefaultAudioDevice();
                }
                catch
                {
                    // Немає вхідного пристрою — відключаємо сервіс
                    _available = false;
                    _engine.Dispose();
                    _engine = null;
                    return;
                }

                _engine.SpeechRecognized += EngineOnSpeechRecognized;
                _engine.RecognizeCompleted += (_, __) => { };

                _available = true;

                // Початкова граматика (екран вибору)
                RebuildSelectionGrammar();
            }
            catch
            {
                _available = false;
                _engine = null;
            }
        }

        private void Start()
        {
            if (!_available || _engine == null) return;
            try
            {
                _engine.RecognizeAsync(RecognizeMode.Multiple);
            }
            catch
            {
                // Якщо не вдалося запустити — вимикаємо до наступної спроби
                _available = false;
            }
        }

        private void Stop()
        {
            if (_engine == null) return;
            try { _engine.RecognizeAsyncCancel(); } catch { }
            try { _engine.RecognizeAsyncStop(); } catch { }
        }

        private void RebuildSelectionGrammar()
        {
            if (!_available || _engine == null || _recognizer == null) return;

            try
            {
                foreach (var g in _engine.Grammars.ToList())
                    _engine.UnloadGrammar(g);
            }
            catch { }

            var culture = _recognizer.Culture;
            foreach (var g in BuildSelectionGrammars(_cachedNames, culture))
            {
                try { _engine.LoadGrammar(g); } catch { }
            }
        }

        private static IEnumerable<Grammar> BuildSelectionGrammars(List<string> testNames, CultureInfo culture)
        {
            var grammars = new List<Grammar>();

            // 1) Базові команди
            {
                var builder = new Choices();

                // UA
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

                // EN (fallback)
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

            // 2) Вибір за назвою
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
            if (!_available || !_active) return;
            if (e.Result == null || e.Result.Confidence < 0.70f) return;

            var cmd = new VoiceCommand { Type = VoiceCommandType.None };

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
            else if (e.Result.Semantics.ContainsKey("testName"))
            {
                cmd.Type = VoiceCommandType.SelectTestByName;
                cmd.Argument = e.Result.Semantics["testName"].Value?.ToString();
            }
            else
            {
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

            try { _hostForm.BeginInvoke(_onCommand, cmd); } catch { }
        }
    }
}