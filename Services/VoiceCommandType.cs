namespace MoodleTestReader.Services
{
    public enum VoiceCommandType
    {
        None = 0,
        StartTest,
        ReviewTest,
        SelectTestByName,
        NextTest,
        PreviousTest,
        FirstTest,
        LastTest,
        EnableTts,
        DisableTts,
        ExitApp
    }

    public class VoiceCommand
    {
        public VoiceCommandType Type { get; set; }
        public string? Argument { get; set; } // Напр., назва тесту для SelectTestByName
    }
}