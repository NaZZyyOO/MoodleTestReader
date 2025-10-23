namespace MoodleTestReader.Speech
{
    /// <summary>
    /// Контракт для рушія синтезу мовлення.
    /// </summary>
    public interface ITtsEngine : IDisposable
    {
        /// <summary>
        /// Озвучує питання та варіанти з паузами.
        /// </summary>
        Task SpeakQuestionAsync(
            string questionText,
            IList<string>? options,
            int questionNumber,
            int totalQuestions,
            int pauseAfterQuestionMs,
            int pauseBetweenOptionsMs,
            bool announceCounts,
            CancellationToken token);

        /// <summary>
        /// Озвучує підсумок балів.
        /// </summary>
        Task SpeakScoreAsync(int score, CancellationToken token);

        /// <summary>
        /// Скасовує поточне озвучення.
        /// </summary>
        Task CancelAsync();
    }
}