using System.Text.RegularExpressions;
using MoodleTestReader.Logic;
using MoodleTestReader.Speech;

namespace MoodleTestReader.Services
{
    // Єдиний слухач мікрофона: і для команд, і для диктації текстових відповідей (Fill in the Blank).
    // Активується тільки коли увімкнений TTS (щоб не заважати користувачам).
    public class VoskCommandService : IDisposable
    {
        private readonly Form _hostForm;
        private readonly VoskSpeechRecognizer _recognizer;

        // Постачальники стану (інжектимо з UI)
        private readonly Func<List<string>> _getTestNames;
        private readonly Func<Question?> _getCurrentQuestion;
        private readonly Func<Panel> _getQuestionPanel;

        private readonly Action<VoiceCommand> _onCommand;

        private bool _active;
        private bool _inSelectionMode = true;

        public bool IsAvailable => _recognizer.IsAvailable;

        public VoskCommandService(
            Form hostForm,
            Action<VoiceCommand> onCommand,
            Func<List<string>> getTestNames,
            Func<Question?> getCurrentQuestion,
            Func<Panel> getQuestionPanel,
            string? modelPath = null)
        {
            _hostForm = hostForm;
            _onCommand = onCommand;
            _getTestNames = getTestNames;
            _getCurrentQuestion = getCurrentQuestion;
            _getQuestionPanel = getQuestionPanel;

            _recognizer = new VoskSpeechRecognizer(modelPath);
            if (_recognizer.IsAvailable)
                _recognizer.TextRecognized += OnTextRecognized;
        }

        public void Dispose()
        {
            try { Stop(); } catch { }
            try { _recognizer?.Dispose(); } catch { }
        }

        public void SetActive(bool active)
        {
            _active = active && IsAvailable;
            if (_active) Start();
            else Stop();
        }

        public void OnSelectionScreen(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
        }

        public void OnTestStarted(bool activeNow)
        {
            _inSelectionMode = false;
            SetActive(activeNow);
        }

        public void OnTestFinished(bool activeNow)
        {
            _inSelectionMode = true;
            SetActive(activeNow);
        }

        private void Start()
        {
            if (!IsAvailable) return;
            _recognizer.Start();
        }

        private void Stop()
        {
            _recognizer.Stop();
        }

        private void OnTextRecognized(object? sender, string text)
        {
            if (!_active) return;

            var phrase = Normalize(text);

            // 1) Парсимо “сильні” команди
            var cmd = TryParseCommand(phrase);
            if (cmd.Type != VoiceCommandType.None)
            {
                _hostForm.BeginInvoke(_onCommand, cmd);
                return;
            }

            // 2) Якщо ми у тесті і питання текстове — усе, що не команда, підставляємо у TextBox
            if (!_inSelectionMode)
            {
                var q = _getCurrentQuestion();
                if (q is FillInBlankQuestion)
                {
                    // Проксі через команду, щоб UI безпечно оновив TextBox
                    _hostForm.BeginInvoke(_onCommand, new VoiceCommand
                    {
                        Type = VoiceCommandType.InputTextAppend,
                        Argument = text
                    });
                }
            }
        }

        private static string Normalize(string s)
        {
            s = s.ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }

        private VoiceCommand TryParseCommand(string s)
        {
            // Спільні ключі
            if (HasAny(s, "увімкнути озвучення", "включити озвучення", "enable speech"))
                return new VoiceCommand { Type = VoiceCommandType.EnableTts };
            if (HasAny(s, "вимкнути озвучення", "відключити озвучення", "disable speech"))
                return new VoiceCommand { Type = VoiceCommandType.DisableTts };
            if (HasAny(s, "вийти", "закрити застосунок", "exit", "close app"))
                return new VoiceCommand { Type = VoiceCommandType.ExitApp };

            if (_inSelectionMode)
            {
                // Екран вибору
                if (HasAny(s, "почати тест", "почати", "start test", "start"))
                    return new VoiceCommand { Type = VoiceCommandType.StartTest };
                if (HasAny(s, "огляд тесту", "огляд", "перегляд тесту", "перегляд", "review test", "review"))
                    return new VoiceCommand { Type = VoiceCommandType.ReviewTest };
                if (HasAny(s, "наступний тест", "далі тест", "next test"))
                    return new VoiceCommand { Type = VoiceCommandType.NextTest };
                if (HasAny(s, "попередній тест", "previous test"))
                    return new VoiceCommand { Type = VoiceCommandType.PreviousTest };
                if (HasAny(s, "перший тест", "first test"))
                    return new VoiceCommand { Type = VoiceCommandType.FirstTest };
                if (HasAny(s, "останній тест", "last test"))
                    return new VoiceCommand { Type = VoiceCommandType.LastTest };

                // “вибрати/обрати тест {назва}”
                var m = Regex.Match(s, @"(?:вибрати|обрати|select)\s+тест\s+(.+)$");
                if (m.Success)
                {
                    var name = m.Groups[1].Value.Trim();
                    var best = FindBestTestName(name, _getTestNames());
                    if (!string.IsNullOrWhiteSpace(best))
                        return new VoiceCommand { Type = VoiceCommandType.SelectTestByName, Argument = best };
                }
            }
            else
            {
                // Під час тесту
                if (HasAny(s, "далі", "наступне питання", "next question", "підтвердити", "підтвердити відповідь"))
                    return new VoiceCommand { Type = VoiceCommandType.NextQuestion };

                if (HasAny(s, "повернутись", "попереднє питання", "previous question"))
                    return new VoiceCommand { Type = VoiceCommandType.PreviousQuestion };

                if (HasAny(s, "повторити питання", "прочитати питання", "read question"))
                    return new VoiceCommand { Type = VoiceCommandType.ReadQuestion };

                if (HasAny(s, "прочитати варіанти", "варіанти", "read options"))
                    return new VoiceCommand { Type = VoiceCommandType.ReadOptions };

                if (HasAny(s, "стоп озвучення", "зупинити озвучення", "stop reading"))
                    return new VoiceCommand { Type = VoiceCommandType.StopReading };

                if (HasAny(s, "скільки часу", "залишок часу", "час"))
                    return new VoiceCommand { Type = VoiceCommandType.ReadTime };

                if (HasAny(s, "очистити вибір", "скинути вибір", "clear selection"))
                    return new VoiceCommand { Type = VoiceCommandType.ClearSelection };

                if (HasAny(s, "істина", "правда", "true"))
                    return new VoiceCommand { Type = VoiceCommandType.SetTrue };

                if (HasAny(s, "хиба", "неправда", "false"))
                    return new VoiceCommand { Type = VoiceCommandType.SetFalse };

                if (HasAny(s, "очистити поле", "стерти текст", "clear text"))
                    return new VoiceCommand { Type = VoiceCommandType.ClearText };

                // Вибір варіанту: “вибрати варіант 2”, “позначити варіант 3”, “зняти варіант 4”
                var choose = Regex.Match(s, @"(?:вибрати|обрати|choose).*(?:варіант|option)\s+(\d+)");
                if (choose.Success)
                {
                    var idx = SafeInt(choose.Groups[1].Value);
                    return new VoiceCommand { Type = VoiceCommandType.SelectOptionIndex, Index = idx };
                }

                var toggle = Regex.Match(s, @"(?:позначити|зняти|toggle).*(?:варіант|option)\s+(\d+)");
                if (toggle.Success)
                {
                    var idx = SafeInt(toggle.Groups[1].Value);
                    return new VoiceCommand { Type = VoiceCommandType.ToggleOptionIndex, Index = idx };
                }

                // Також підтримка порядкових: перший/другий/третій/четвертий/п'ятий/шостий
                var ordinalIndex = TryParseOrdinalIndex(s);
                if (ordinalIndex.HasValue)
                    return new VoiceCommand { Type = VoiceCommandType.SelectOptionIndex, Index = ordinalIndex.Value };
            }

            return new VoiceCommand { Type = VoiceCommandType.None };
        }

        private static bool HasAny(string s, params string[] keys) =>
            keys.Any(k => s.Contains(k, StringComparison.OrdinalIgnoreCase));

        private static int SafeInt(string v) =>
            int.TryParse(v, out var n) ? n : -1;

        private static int? TryParseOrdinalIndex(string s)
        {
            // мінімальний словник
            if (s.Contains("перш", StringComparison.OrdinalIgnoreCase)) return 1;
            if (s.Contains("друг", StringComparison.OrdinalIgnoreCase)) return 2;
            if (s.Contains("трет", StringComparison.OrdinalIgnoreCase)) return 3;
            if (s.Contains("четвер", StringComparison.OrdinalIgnoreCase)) return 4;
            if (s.Contains("п'ят", StringComparison.OrdinalIgnoreCase) || s.Contains("пят", StringComparison.OrdinalIgnoreCase)) return 5;
            if (s.Contains("шост", StringComparison.OrdinalIgnoreCase)) return 6;
            return null;
        }

        private static string? FindBestTestName(string spoken, List<string> names)
        {
            if (names == null || names.Count == 0) return null;
            spoken = Normalize(spoken);
            // Спочатку точний contains
            var exact = names.FirstOrDefault(n => Normalize(n).Contains(spoken));
            if (!string.IsNullOrEmpty(exact)) return exact;

            // Проста найкраща схожість за довжиною спільного префікса
            int BestScore(string a, string b)
            {
                int m = Math.Min(a.Length, b.Length);
                int i = 0;
                for (; i < m; i++)
                    if (a[i] != b[i]) break;
                return i;
            }

            string? best = null;
            int bestScore = -1;
            foreach (var n in names)
            {
                var ns = Normalize(n);
                var sc = BestScore(spoken, ns);
                if (sc > bestScore)
                {
                    bestScore = sc;
                    best = n;
                }
            }
            return best;
        }
    }
}