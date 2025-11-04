using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Runtime.InteropServices;

namespace MoodleTestReader.Speech;

// Неофіційний Google Translate TTS (tl=uk|en), віддає MP3.
// Відтворення через MCI (winmm) — без COM, без NuGet, без встановлення програм.
public class GoogleTranslateTts
{
    // Браузерний клієнт
    private readonly HttpClient _http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

    private readonly Lock _playLock = new Lock();
    private string? _currentTempFile;
    private string? _currentAlias;

    // 0.24..1.0 — 1.0 = стандартна макс. швидкість у Google (вище 1.0 не підтримується)
    public double Speed { get; set; } = 1.0;

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);
    
    // Ініціалізуємо об'єкт, уточнивши браузер та URL 
    public GoogleTranslateTts()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _http.DefaultRequestHeaders.Referrer = new Uri("https://translate.google.com/");
    }

    // Озвучує запитання і варіанти у потрібній структурі
    public async Task SpeakQuestionAsync(
        string questionText,
        IList<string>? options,
        int questionNumber,
        int totalQuestions,
        int pauseAfterQuestionMs,
        int pauseBetweenOptionsMs,
        bool announceCounts,
        CancellationToken token)
    {
        // 1) Оголошуємо номер питання (без слова "Запитання:", щоб не було дублювань)
        if (announceCounts && questionNumber > 0 && totalQuestions > 0)
        {
            await SpeakWithLangAsync($"Питання {questionNumber} з {totalQuestions}.", "uk", token);
        }

        // 2) Запитання:
        var cleanQuestion = CleanForTts(questionText);
        if (!string.IsNullOrWhiteSpace(cleanQuestion))
        {
            await SpeakWithLangAsync($"Запитання: {cleanQuestion}", "uk", token);
        }

        // Пауза після запитання (зменшена для пришвидшення)
        if (pauseAfterQuestionMs > 0)
            await Task.Delay(pauseAfterQuestionMs, token);

        // 3) Варіанти відповідей + кожен рядок: номер українською, текст варіанта — auto uk/en
        var optionsList = options ?? Array.Empty<string>();
        if (optionsList.Count > 0)
        {
            await SpeakWithLangAsync("Варіанти відповідей.", "uk", token);

            foreach (var (text, idx) in optionsList.Select((t, i) => (t, i)))
            {
                // Префікс "1.", "2.", ... українською — щоб числа і мова нумерації завжди були uk
                await SpeakWithLangAsync($"{idx + 1}.", "uk", token);

                // Сам варіант — санітизуємо і автоматично підбираємо мову для більш якісного читання
                var cleaned = CleanForTts(text);
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    await SpeakAutoLangAsync(cleaned, token);
                }

                if (pauseBetweenOptionsMs > 0)
                    await Task.Delay(pauseBetweenOptionsMs, token);
            }
        }
    }

    public async Task SpeakScoreAsync(int score, CancellationToken token)
    {
        var text = $"Тест завершено. Ваш результат: {score} балів.";
        await SpeakWithLangAsync(text, "uk", token);
    }

    // Примусова мова (uk або en) з розбиттям на шматки
    public async Task SpeakWithLangAsync(string text, string lang, CancellationToken token)
    {
        foreach (var chunk in SplitToChunks(CleanForTts(text), 180))
        {
            token.ThrowIfCancellationRequested();
            var bytes = await DownloadMp3Async(chunk, lang, token);
            await PlayMp3Async(bytes, token);
        }
    }

    // Автовизначення мови (uk/en) по кожному шматку
    public async Task SpeakAutoLangAsync(string text, CancellationToken token)
    {
        foreach (var chunk in SplitToChunks(CleanForTts(text), 180))
        {
            token.ThrowIfCancellationRequested();
            var lang = DetermineLangFor(chunk);
            var bytes = await DownloadMp3Async(chunk, lang, token);
            await PlayMp3Async(bytes, token);
        }
    }

    public async Task CancelAsync()
    {
        lock (_playLock)
        {
            if (!string.IsNullOrEmpty(_currentAlias))
            {
                try { mciSendString($"stop {_currentAlias}", null, 0, IntPtr.Zero); } catch { }
                try { mciSendString($"close {_currentAlias}", null, 0, IntPtr.Zero); } catch { }
                _currentAlias = null;
            }
            if (!string.IsNullOrEmpty(_currentTempFile))
            {
                try { File.Delete(_currentTempFile); } catch { }
                _currentTempFile = null;
            }
        }
        await Task.CompletedTask;
    }

    private async Task<byte[]> DownloadMp3Async(string text, string lang, CancellationToken token)
    {
        var url = BuildUrl(text, lang, Speed);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, token);
        resp.EnsureSuccessStatusCode();
        var bytes = await resp.Content.ReadAsByteArrayAsync(token);
        if (bytes.Length < 128) throw new Exception("Google TTS: empty or blocked response.");
        return bytes;
    }

    private static string BuildUrl(string text, string lang, double speed)
    {
        // tl=uk|en, client=tw-ob — без ключа; ttsspeed 0.24..1.0
        speed = Math.Clamp(speed, 0.24, 1.0);
        var q = HttpUtility.ParseQueryString(string.Empty);
        q["ie"] = "UTF-8";
        q["q"] = text;
        q["tl"] = string.IsNullOrWhiteSpace(lang) ? "uk" : lang;
        q["client"] = "tw-ob";
        q["ttsspeed"] = speed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "https://translate.google.com/translate_tts?" + q.ToString();
    }

    private async Task PlayMp3Async(byte[] mp3, CancellationToken token)
    {
        // MCI грає лише з файлу — тимчасовий
        var temp = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid():N}.mp3");
        await File.WriteAllBytesAsync(temp, mp3, token);

        var alias = $"tts_{Guid.NewGuid():N}";
        lock (_playLock)
        {
            _currentTempFile = temp;
            _currentAlias = alias;
        }

        mciSendString($"open \"{temp}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
        mciSendString($"play {alias}", null, 0, IntPtr.Zero);

        var status = new StringBuilder(128);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            status.Clear();
            mciSendString($"status {alias} mode", status, status.Capacity, IntPtr.Zero);
            var mode = status.ToString();
            if (mode.Equals("stopped", StringComparison.OrdinalIgnoreCase) ||
                mode.Equals("ready", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            await Task.Delay(100, token);
        }

        mciSendString($"close {alias}", null, 0, IntPtr.Zero);
        try { File.Delete(temp); } catch { }

        lock (_playLock)
        {
            if (_currentAlias == alias) _currentAlias = null;
            if (_currentTempFile == temp) _currentTempFile = null;
        }
    }

    // Санітизація: прибрати URL/примітки/сміття, нормалізувати символи
    public static string CleanForTts(string? input)
    {
        var text = (input ?? string.Empty).Trim();

        text = text.Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");

        // Прибрати URL-и
        text = Regex.Replace(text, @"https?://\S+", "", RegexOptions.IgnoreCase);

        // Прибрати короткі службові фрагменти у дужках
        text = Regex.Replace(text, @"[\(\[\{][^\)\]\}]{0,80}[\)\]\}]", "", RegexOptions.Multiline);

        // Залишити літери/цифри/базову пунктуацію
        text = Regex.Replace(text, @"[^\p{L}\p{Nd}\s\.,:;!\?-]", " ");

        // Стиснути пробіли і нормалізувати символи
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        text = text.Replace("—", "-").Replace("–", "-").Replace("“", "\"").Replace("”", "\"").Replace("’", "'");

        return text;
    }

    // Якщо більше латиниці — читаємо en, інакше uk
    private static string DetermineLangFor(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "uk";
        int latin = 0, cyr = 0;
        foreach (var ch in text)
        {
            if (ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z') latin++;
            else if (('А' <= ch && ch <= 'я') || ch is 'І' or 'і' or 'Ї' or 'ї' or 'Є' or 'є' or 'Ґ' or 'ґ') cyr++;
        }
        return latin > cyr ? "en" : "uk";
    }

    private static IEnumerable<string> SplitToChunks(string text, int maxLen)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length <= maxLen) { yield return text; yield break; }

        var sb = new StringBuilder();
        foreach (var word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (sb.Length + 1 + word.Length > maxLen)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(word);
        }
        if (sb.Length > 0) yield return sb.ToString();
    }

    public void Dispose()
    {
        try { CancelAsync().GetAwaiter().GetResult(); } catch { }
        _http.Dispose();
    }
}