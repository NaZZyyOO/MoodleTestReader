using System.Speech.Synthesis;

namespace MoodleTestReader.Speech;

public class TextToSpeech
{
    public void Speak(string text)
    {
        using (SpeechSynthesizer synth = new SpeechSynthesizer())
        {
            synth.Speak(text);
        }
    }
}