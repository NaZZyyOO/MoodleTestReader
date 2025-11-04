using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Runtime.InteropServices;

namespace MoodleTestReader.Speech;

// Неофіційний Google Translate TTS (tl=uk|en), віддає MP3.
// Відтворення через MCI (winmm) — без COM, без NuGet, без встановлення програм.
//
// Загальна схема роботи:
// 1) Вхідний текст проходить санітизацію (CleanForTts) і розбиття на шматки до ~180 символів (SplitToChunks).
// 2) Для кожного шматка визначається мова (авто uk/en або примусово) — DetermineLangFor.
// 3) Будується URL до неофіційної TTS-ендпоінт Google Translate (BuildUrl) із вибраною швидкістю.
// 4) MP3-байти завантажуються через HttpClient (DownloadMp3Async).
// 5) MP3 тимчасово зберігається у файл і відтворюється через MCI-команди winmm (PlayMp3Async).
// 6) Поки триває програвання, очікуємо стан "stopped"/"ready". Після — закриваємо і прибираємо тимчасовий файл.
// 7) CancelAsync дозволяє зупинити поточне відтворення і прибрати файли у будь-який момент.
public class GoogleTranslateTts
{
    // Браузерний клієнт із ввімкненою автоматичною декомпресією, щоб поводитись як звичайний браузер.
    private readonly HttpClient _http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

    // Примітив синхронізації доступу до _currentTempFile/_currentAlias між програвачем і скасуванням.
    private readonly Lock _playLock = new Lock();

    // Шлях до поточного тимчасового MP3-файлу, який відтворюється.
    private string? _currentTempFile;

    // Поточний MCI-аліас відкритого медіафайлу (для команди stop/close).
    private string? _currentAlias;

    // 0.24..1.0 — множник швидкості озвучення для Google TTS (вище 1.0 не підтримується Google).
    public double Speed { get; set; } = 1.0;

    // P/Invoke для надсилання текстових команд MCI (winmm.dll).
    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);
    
    // Ініціалізація: задаємо User-Agent і Referrer, щоб ендпоінт Google не виглядав підозріло.
    public GoogleTranslateTts()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _http.DefaultRequestHeaders.Referrer = new Uri("https://translate.google.com/");
    }

    // Високорівнева процедура озвучення тестового питання:
    // - за бажанням оголошує «Питання X з Y»
    // - читає саме питання
    // - робить паузу
    // - читає «Варіанти відповідей» і кожен варіант із нумерацією українською
    // - для тексту кожного варіанту автоматично підбирає мову (uk або en)
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

        // 2) Озвучуємо саме питання (попередньо прибравши зайве сміття, URL і т.д.)
        var cleanQuestion = CleanForTts(questionText);
        if (!string.IsNullOrWhiteSpace(cleanQuestion))
        {
            await SpeakWithLangAsync($"Запитання: {cleanQuestion}", "uk", token);
        }

        // Пауза після запитання, щоб слухач устиг сприйняти текст.
        if (pauseAfterQuestionMs > 0)
            await Task.Delay(pauseAfterQuestionMs, token);

        // 3) Варіанти відповідей:
        // - Спочатку фраза «Варіанти відповідей.» українською
        // - Далі кожен варіант читається після озвучення номера «1.», «2.» ... українською
        // - Сам текст варіанту читається мовою uk або en (визначається автоматично по латиниці/кирилиці)
        var optionsList = options ?? Array.Empty<string>();
        if (optionsList.Count > 0)
        {
            await SpeakWithLangAsync("Варіанти відповідей.", "uk", token);

            foreach (var (text, idx) in optionsList.Select((t, i) => (t, i)))
            {
                // Номер варіанту читаємо українською (стабільна вимова цифр).
                await SpeakWithLangAsync($"{idx + 1}.", "uk", token);

                // Сам варіант — санітизуємо і автоматично підбираємо мову для більш якісного читання.
                var cleaned = CleanForTts(text);
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    await SpeakAutoLangAsync(cleaned, token);
                }

                // Невелика пауза між варіантами, якщо задано.
                if (pauseBetweenOptionsMs > 0)
                    await Task.Delay(pauseBetweenOptionsMs, token);
            }
        }
    }

    // Коротка утиліта для озвучення результату тесту.
    public async Task SpeakScoreAsync(int score, CancellationToken token)
    {
        var text = $"Тест завершено. Ваш результат: {score} балів.";
        await SpeakWithLangAsync(text, "uk", token);
    }

    // Озвучення заданим кодом мови (uk або en).
    // Важливо: Google Translate TTS має обмеження на довжину фрагмента,
    // тому текст розбиваємо на шматки (до 180 символів), кожен — окремий запит і відтворення.
    private async Task SpeakWithLangAsync(string text, string lang, CancellationToken token)
    {
        foreach (var chunk in SplitToChunks(CleanForTts(text), 180))
        {
            token.ThrowIfCancellationRequested();

            // 1) Завантажуємо MP3 для цього шматка
            var bytes = await DownloadMp3Async(chunk, lang, token);

            // 2) Відтворюємо MP3 і чекаємо завершення
            await PlayMp3Async(bytes, token);
        }
    }

    // Те саме, що вище, але мова визначається автоматично для кожного шматка.
    // Евристика: якщо у фрагменті більше латинських символів — вважаємо en, інакше uk.
    private async Task SpeakAutoLangAsync(string text, CancellationToken token)
    {
        foreach (var chunk in SplitToChunks(CleanForTts(text), 180))
        {
            token.ThrowIfCancellationRequested();
            var lang = DetermineLangFor(chunk);

            var bytes = await DownloadMp3Async(chunk, lang, token);
            await PlayMp3Async(bytes, token);
        }
    }

    // Примусове скасування поточного відтворення:
    // - надсилає stop/close на активний MCI-аліас (якщо є)
    // - видаляє тимчасовий файл (якщо є)
    // - метод безпечний до повторних викликів
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

    // Завантаження MP3-байтів з Google Translate TTS.
    // Ключові моменти:
    // - Будується URL з параметрами q=текст, tl=мова, ttsspeed=швидкість
    // - Перевіряємо HTTP-успіх і мінімальний розмір відповіді (захист від порожніх/блокованих відповідей)
    private async Task<byte[]> DownloadMp3Async(string text, string lang, CancellationToken token)
    {
        var url = BuildUrl(text, lang, Speed);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseContentRead, token);
        resp.EnsureSuccessStatusCode();

        var bytes = await resp.Content.ReadAsByteArrayAsync(token);

        // Невеликий sanity-check: надто маленький буфер — схоже на помилку або блокування.
        if (bytes.Length < 128) throw new Exception("Google TTS: empty or blocked response.");
        return bytes;
    }

    // Побудова URL для неофіційного ендпоінту TTS Google Translate.
    // Параметри:
    // - ie=UTF-8         — кодування
    // - q=<text>         — сам текст
    // - tl=uk|en         — мова синтезу (target language)
    // - client=tw-ob     — клієнт без ключа (неофіційний режим)
    // - ttsspeed=0.24..1 — множник швидкості
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

    // Відтворення MP3-буфера через MCI:
    // - записуємо байти у тимчасовий .mp3 файл (MCI відтворює лише з файлів)
    // - відкриваємо файл: "open <path> type mpegvideo alias <id>"
    // - запускаємо: "play <alias>"
    // - циклічно опитуємо "status <alias> mode" до стану stopped/ready
    // - "close <alias>" і видалення тимчасового файлу
    private async Task PlayMp3Async(byte[] mp3, CancellationToken token)
    {
        // 1) Пишемо тимчасовий файл
        var temp = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid():N}.mp3");
        await File.WriteAllBytesAsync(temp, mp3, token);

        // 2) Готуємо унікальний MCI-аліас для керування сеансом відтворення
        var alias = $"tts_{Guid.NewGuid():N}";
        lock (_playLock)
        {
            _currentTempFile = temp;
            _currentAlias = alias;
        }

        // 3) Відкриваємо і запускаємо через MCI
        mciSendString($"open \"{temp}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
        mciSendString($"play {alias}", null, 0, IntPtr.Zero);

        // 4) Чекаємо завершення, опитуючи поточний режим
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

        // 5) Закриваємо і прибираємо сміття
        mciSendString($"close {alias}", null, 0, IntPtr.Zero);
        try { File.Delete(temp); } catch { }

        // 6) Акуратно очищаємо посилання на поточні ресурси
        lock (_playLock)
        {
            if (_currentAlias == alias) _currentAlias = null;
            if (_currentTempFile == temp) _currentTempFile = null;
        }
    }

    // Санітизація тексту перед синтезом:
    // - прибирає HTML-ентіті (&nbsp;, &amp; ...)
    // - видаляє URL (щоб не тараторити посилання)
    // - прибирає короткі службові фрагменти в дужках (примітки, технічні вставки)
    // - залишає лише букви/цифри/базову пунктуацію
    // - зменшує повторні пробіли, нормалізує тире/лапки
    private static string CleanForTts(string? input)
    {
        var text = (input ?? string.Empty).Trim();

        // Розкодування кількох типових HTML-ентіті
        text = text.Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&quot;", "\"").Replace("&apos;", "'");

        // Прибрати URL-и цілком
        text = Regex.Replace(text, @"https?://\S+", "", RegexOptions.IgnoreCase);

        // Прибрати відносно короткі службові фрагменти у будь-яких дужках
        text = Regex.Replace(text, @"[\(\[\{][^\)\]\}]{0,80}[\)\]\}]", "", RegexOptions.Multiline);

        // Лишаємо букви, цифри, пробіли і базову пунктуацію; решту — в пробіл
        text = Regex.Replace(text, @"[^\p{L}\p{Nd}\s\.,:;!\?-]", " ");

        // Стиснути пробіли і нормалізувати популярні типографські символи
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        text = text.Replace("—", "-").Replace("–", "-").Replace("“", "\"").Replace("”", "\"").Replace("’", "'");

        return text;
    }

    // Проста евристика вибору мови:
    // - якщо латиниці більше, ніж кирилиці, вважаємо, що текст англійською → "en"
    // - в інших випадках → "uk"
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

    // Розбиття довгого тексту на шматки не довші за maxLen символів.
    // Розбиваємо по словах, щоб не рвати слова й не погіршувати вимову.
    private static IEnumerable<string> SplitToChunks(string text, int maxLen)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length <= maxLen) { yield return text; yield break; }

        var sb = new StringBuilder();
        foreach (var word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            // Якщо наступне слово вже не влізе — віддаємо поточний буфер і починаємо новий
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

    // Коректне вивільнення ресурсів:
    // - зупинка програвання і видалення тимчасового файлу
    // - Dispose HttpClient
    public void Dispose()
    {
        try { CancelAsync().GetAwaiter().GetResult(); } catch { }
        _http.Dispose();
    }
}