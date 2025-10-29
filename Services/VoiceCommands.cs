namespace MoodleTestReader.Services
{
    public enum VoiceCommandType
    {
        None = 0,

        // Екран вибору
        StartTest,
        ReviewTest,
        SelectTestByName,
        NextTest,
        PreviousTest,
        FirstTest,
        LastTest,
        EnableTts,
        DisableTts,
        ExitApp,

        // Під час тесту
        NextQuestion,          // “далі”, “наступне”
        PreviousQuestion,      // якщо реалізовано повернення
        SelectOptionIndex,     // вибрати N (для Single/TrueFalse)
        ToggleOptionIndex,     // позначити/зняти N (для MultipleChoice)
        ClearSelection,        // очистити вибір (для MultipleChoice)
        SetTrue,
        SetFalse,
        ReadQuestion,          // повторити питання
        ReadOptions,           // прочитати варіанти
        StopReading,           // зупинити озвучення
        ReadTime,              // сказати/показати час

        // Текстове питання
        InputTextAppend,       // додати розпізнаний текст у TextBox
        ClearText              // очистити поле
    }

    public class VoiceCommand
    {
        public VoiceCommandType Type { get; set; }
        public string? Argument { get; set; } // Назва тесту або текст для InputTextAppend
        public int? Index { get; set; }       // Номер варіанту (1..N)
    }
}