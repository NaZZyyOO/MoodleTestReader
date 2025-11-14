using System.Globalization;
using NAudio.Wave;
using Vosk;
using Newtonsoft.Json.Linq;

namespace MoodleTestReader.Speech
{
    // Розпізнавання вільного мовлення (диктування) через Vosk + NAudio
    public class VoskSpeechRecognizer : IDisposable
    {
        private readonly string _modelPath;
        private Model? _model;
        private VoskRecognizer? _recognizer;
        private WaveInEvent? _waveIn;

        public bool IsAvailable { get; private set; }
        public CultureInfo Culture { get; private set; } = new CultureInfo("uk-UA");

        public event EventHandler<string>? TextRecognized;

        public VoskSpeechRecognizer(string? modelPath = null)
        {
            _modelPath = modelPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpeechAPI", "vosk-model-small-uk-v3-small");
            TryInit();
        }

        private void TryInit()
        {
            try
            {
                if (!Directory.Exists(_modelPath))
                {
                    IsAvailable = false;
                    return;
                }

                Vosk.Vosk.SetLogLevel(0); // тихий лог
                _model = new Model(_modelPath);
                _recognizer = new VoskRecognizer(_model, 16000.0f);

                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1)
                };
                _waveIn.DataAvailable += WaveInOnDataAvailable;

                IsAvailable = true;
            }
            catch
            {
                IsAvailable = false;
                Cleanup();
            }
        }

        public void Start()
        {
            if (!IsAvailable || _waveIn == null) return;
            try { _waveIn.StartRecording(); } catch { }
        }

        public void Stop()
        {
            try { _waveIn?.StopRecording(); } catch { }
        }

        private void WaveInOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_recognizer == null || e.BytesRecorded <= 0) return;

            if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
            {
                var json = _recognizer.Result();
                EmitText(json);
            }
        }

        private void EmitText(string json)
        {
            try
            {
                var jo = JObject.Parse(json);
                var text = (string?)jo["text"];
                if (!string.IsNullOrWhiteSpace(text))
                {
                    TextRecognized?.Invoke(this, text.Trim());
                }
            }
            catch { }
        }

        private void Cleanup()
        {
            try { _waveIn?.Dispose(); } catch { }
            _waveIn = null;

            try { _recognizer?.Dispose(); } catch { }
            _recognizer = null;

            try { _model?.Dispose(); } catch { }
            _model = null;
        }

        public void Dispose()
        {
            Stop();
            Cleanup();
        }
    }
}