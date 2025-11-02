using MoodleTestReader.Logic;
using MoodleTestReader.Models;
using MoodleTestReader.Services;
using MoodleTestReader.UI.Rendering;
using Timer = System.Windows.Forms.Timer;

namespace MoodleTestReader.UI
{
    public partial class Test : Form
    {
        private User _currentUser;
        private TestManager _testManager;
        private readonly Timer _testTimer;
        private int _remainingTime; // в секундах
        private DateTime _startTime;
        private Panel _questionPanel;

        // Диктування та голосові команди
        private readonly TestDictationService _dictation;
        private readonly VoskCommandService _voiceCmd;

        // Лічильники питань
        private int _questionNumber;
        private int _totalQuestions;

        public Test()
        {
            InitializeComponent();
            InitializeComponents();
            
            _dictation = new TestDictationService(this);

            // Vosk-команди: постачаємо назви тестів, поточне питання і панель з контролами
            _voiceCmd = new VoskCommandService(
                this,
                OnVoiceCommand,
                () => _testManager?.GetAvailableTests().Select(t => t.TestName).ToList() ?? new List<string>(),
                () => _testManager?.GetCurrentQuestionForUser(_currentUser),
                () => _questionPanel
            );

            // ПОКАЗАТИ ІНДИКАТОР З ПОЧАТКОВИМ ТЕКСТОМ
            if (recognitionLabel != null)
            {
                recognitionLabel.Visible = true;
                recognitionLabel.Text = "Розпізнавання…";
                recognitionLabel.ForeColor = Color.DimGray;
            }

            // Показувати НЕ-командний текст користувача у лейблі
            _voiceCmd.RecognizedNonCommandText += (_, txt) =>
            {
                try
                {
                    BeginInvoke(() =>
                    {
                        if (recognitionLabel == null) return;
                        // На екрані вибору — завжди показуємо; під час тесту — тільки якщо TTS увімкнений
                        var shouldShow = comboBoxTests.Visible || _dictation.IsEnabled;
                        recognitionLabel.Visible = shouldShow;
                        if (!shouldShow) return;

                        recognitionLabel.ForeColor = Color.ForestGreen;
                        recognitionLabel.Text = $"Розпізнано: {txt}";
                    });
                }
                catch { }
            };

            // Команди активні разом з TTS
            _dictation.EnabledChanged += (_, enabled) =>
            {
                if (comboBoxTests.Visible)
                {
                    _voiceCmd.OnSelectionScreen();
                    if (recognitionLabel != null)
                    {
                        recognitionLabel.Visible = true;
                        recognitionLabel.Text = "Розпізнавання…";
                        recognitionLabel.ForeColor = Color.DimGray;
                    }
                }
                else
                {
                    _voiceCmd.OnTestStarted(enabled);
                    if (recognitionLabel != null)
                    {
                        recognitionLabel.Visible = enabled;
                        recognitionLabel.Text = "Розпізнавання…";
                        recognitionLabel.ForeColor = Color.DimGray;
                    }
                }
            };

            ShowLoginScreen();

            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += TestTimer_Tick;

            comboBoxTests.Click += TestReview;
            comboBoxTests.SelectedIndexChanged += TestReview;
            comboBoxTests.SelectedValueChanged += TestReview;
        }

        private void InitializeComponents()
        {
            _questionPanel = new Panel { Dock = DockStyle.Fill, Location = new Point(0, 0) };
            Controls.Add(_questionPanel);
        }

        private void ShowLoginScreen()
        {
            var loginForm = new Login();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                _currentUser = loginForm.GetUser();
                _testManager = new TestManager();
                LoadAvailableTests();
                
                // Показати перемикач TTS; і відповідно увімкнути/вимкнути слухач
                _voiceCmd.OnSelectionScreen();
                _voiceCmd.SetActive(_dictation.IsEnabled);
                _dictation.OnTestSelected();

                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = true;
                    recognitionLabel.Text = "Розпізнавання…";
                    recognitionLabel.ForeColor = Color.DimGray;
                }
            }
            else
            {
                Close();
            }
        }

        private void LoadAvailableTests()
        {
            var tests = _testManager.GetAvailableTests();
            comboBoxTests.Items.Clear();
            foreach (var test in tests)
            {
                comboBoxTests.Items.Add(test.TestName);
            }
        }
        
        // Один з головних методів: початок тесту
        private async void StartTestButton_Click(object? sender, EventArgs e)
        {
            if (comboBoxTests.SelectedIndex == -1)
            {
                MessageBox.Show("Оберіть тест.");
                return;
            }
            
            var selectedTestName = comboBoxTests.SelectedItem?.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);
            if (currentTest == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }
            
            _testManager.StartTestForUser(_currentUser, currentTest.Id);
            _remainingTime = currentTest.TimeLimit * 60;
            labelTime.Text = $"Залишилось: {currentTest.TimeLimit}:00";
            comboBoxTests.Visible = false;
            buttonStartTest.Visible = false;
            buttonReviewTest.Visible = false;
            labelTime.Visible = true;
            _startTime = DateTime.Now;
            _testTimer.Start();
            
            _questionNumber = 1;
            _totalQuestions = currentTest.Questions.Count;

            _dictation.OnTestStarted(_totalQuestions);
            _voiceCmd.SetActive(_dictation.IsEnabled);

            if (recognitionLabel != null)
            {
                recognitionLabel.Visible = _dictation.IsEnabled;
                recognitionLabel.Text = "Розпізнавання…";
                recognitionLabel.ForeColor = Color.DimGray;
            }
            
            await ShowCurrentQuestionAsync();
        }

        private async Task ShowCurrentQuestionAsync()
        {
            _questionPanel.Controls.Clear();
            
            if (_currentUser != null)
            {
                var question = _testManager.GetCurrentQuestionForUser(_currentUser);
                if (question == null)
                {
                    _testTimer.Stop();
                    _testManager.SaveResultsForUser(_currentUser, out int score, _startTime);

                    comboBoxTests.Visible = true;
                    buttonStartTest.Visible = true;
                    labelTime.Visible = false;

                    await _dictation.OnTestFinishedAsync(score);

                    TestReview(null, EventArgs.Empty);
                    _voiceCmd.OnSelectionScreen();
                    _voiceCmd.SetActive(_dictation.IsEnabled);

                    if (recognitionLabel != null)
                    {
                        recognitionLabel.Visible = true;
                        recognitionLabel.Text = "Розпізнавання…";
                        recognitionLabel.ForeColor = Color.DimGray;
                    }

                    MessageBox.Show($"Тест завершено. Ваш результат: {score} балів. Залишковий час: {TimeSpan.FromSeconds(_remainingTime):mm\\:ss}");
                    return;
                }
            
                QuestionRenderer.RenderQuestion(
                    _questionPanel,
                    question,
                    QuestionRenderMode.Play,
                    userAnswer: null,
                    includeQuestionTitle: true,
                    headerPrefix: null
                );

                var buttonNext = new Button { Text = "Наступне", Location = new Point(10, 240), Width = 150, Height = 30 };
                buttonNext.Click += NextButton_Click;
                _questionPanel.Controls.Add(buttonNext);

                await _dictation.OnQuestionShownAsync(question);
                
                _voiceCmd.OnTestStarted(_dictation.IsEnabled);
                _voiceCmd.SetActive(_dictation.IsEnabled);

                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = _dictation.IsEnabled;
                    recognitionLabel.Text = "Розпізнавання…";
                    recognitionLabel.ForeColor = Color.DimGray;
                }
            }
        }

        private async void NextButton_Click(object? sender, EventArgs e)
        {
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
            var answer = ExtractAnswerFromQuestionPanel(question);
            
            if (answer == null || (answer is List<string> list && list.Count == 0) || (answer is string s && string.IsNullOrWhiteSpace(s)))
            {
                MessageBox.Show("Оберіть відповідь.");
                return;
            }
            
            _testManager.SubmitAnswerForUser(_currentUser, answer);
            _questionNumber++;
            await ShowCurrentQuestionAsync();
        }
        
        private object? ExtractAnswerFromQuestionPanel(Question question)
        {
            switch (question)
            {
                case MultipleChoiceQuestion:
                {
                    var selected = new List<string>();
                    foreach (Control c in _questionPanel.Controls)
                    {
                        if (c is CheckBox cb && cb.Enabled && cb.Checked)
                            selected.Add(cb.Text);
                    }
                    return selected;
                }
                case FillInBlankQuestion:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is TextBox tb && !tb.ReadOnly)
                            return tb.Text;
                    return string.Empty;
                }
                case TrueFalseQuestion:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return bool.Parse(rb.Text);
                    return null;
                }
                default:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return rb.Text;
                    return null;
                }
            }
        }
        
        private void TestReview(object? sender, EventArgs e)
        {
            if (comboBoxTests.SelectedItem == null) return;
            var selectedTestName = comboBoxTests.SelectedItem.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);

            if (currentTest != null)
            {
                if (_currentUser != null && _currentUser.TestResults.Any(result => result.TestId == currentTest.Id))
                {
                    buttonStartTest.Visible = false;
                    buttonReviewTest.Visible = true;
                }
                else
                {
                    buttonStartTest.Visible = true;
                    buttonReviewTest.Visible = false;
                }

                _dictation.OnTestSelected();

                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = true;
                    recognitionLabel.Text = "Розпізнавання…";
                    recognitionLabel.ForeColor = Color.DimGray;
                }
            }
        }
        
        private void TestReview_Click(object? sender, EventArgs e)
        {
            if (comboBoxTests.SelectedIndex == -1)
            {
                MessageBox.Show("Оберіть тест.");
                return;
            }
            
            var selectedTestName = comboBoxTests.SelectedItem?.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);
            if (currentTest == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }
            
            var result = _currentUser?.TestResults.Where(r => r.TestId == currentTest.Id)
                .OrderByDescending(r => r.EndTime)
                .FirstOrDefault();

            if (result == null)
            {
                MessageBox.Show("Немає збережених результатів для цього тесту.");
                return;
            }

            comboBoxTests.Visible = false;
            buttonStartTest.Visible = false;
            buttonReviewTest.Visible = false;
            labelTime.Visible = false;

            _questionPanel.Controls.Clear();

            var reviewFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
            };

            reviewFlow.Resize += (_, _) =>
            {
                foreach (Control c in reviewFlow.Controls)
                {
                    if (c is Panel card)
                    {
                        card.Width = reviewFlow.ClientSize.Width - reviewFlow.Padding.Horizontal - 10;
                    }
                }
            };

            _questionPanel.Controls.Add(reviewFlow);

            int index = 1;
            foreach (var q in currentTest.Questions)
            {
                var card = new Panel
                {
                    Margin = new Padding(0, 0, 0, 10),
                    BorderStyle = BorderStyle.None,
                    Width = reviewFlow.ClientSize.Width - reviewFlow.Padding.Horizontal - 10
                };

                var contentPanel = new Panel
                {
                    Location = new Point(0, 0),
                    Width = card.Width
                };

                result.Details.TryGetValue(q.Id, out var aws);
                var userAnswer = aws?.Answer;

                QuestionRenderer.RenderQuestion(
                    contentPanel,
                    q,
                    QuestionRenderMode.Review,
                    userAnswer,
                    includeQuestionTitle: true,
                    headerPrefix: $"{index}. "
                );

                var contentBottom = contentPanel.Controls.Cast<Control>().Select(c => c.Bottom).DefaultIfEmpty(0).Max();
                contentPanel.Height = contentBottom;

                card.Controls.Add(contentPanel);

                var pts = new Label
                {
                    Text = $"Бали: {(aws?.Points ?? 0)} з {q.Points}",
                    Location = new Point(0, contentPanel.Bottom + 6),
                    AutoSize = true
                };
                card.Controls.Add(pts);

                card.Height = pts.Bottom;

                reviewFlow.Controls.Add(card);
                index++;
            }

            var btnClose = new Button
            {
                Text = "Закрити огляд",
                Width = 160,
                Height = 30,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnClose.Click += (_, _) =>
            {
                _questionPanel.Controls.Clear();
                comboBoxTests.Visible = true;
                TestReview(null, EventArgs.Empty);
                labelTime.Visible = false;

                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = true;
                    recognitionLabel.Text = "Розпізнавання…";
                    recognitionLabel.ForeColor = Color.DimGray;
                }
            };
            reviewFlow.Controls.Add(btnClose);
        }
        
        private void TestTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingTime > 0)
            {
                _remainingTime--;
                var timeSpan = TimeSpan.FromSeconds(_remainingTime);
                labelTime.Text = $"Залишилось: {timeSpan:mm\\:ss}";
                if (_remainingTime <= 30)
                {
                    labelTime.ForeColor = Color.Red;
                }
            }
            else
            {
                _testTimer.Stop();
                _testManager.SaveResultsForUser(_currentUser, out int score, _startTime);

                _ = _dictation.OnTestFinishedAsync(score);

                MessageBox.Show($"Час вичерпано. Тест завершено. Ваш результат: {score} балів.");
                _questionPanel.Controls.Clear();

                buttonStartTest.Visible = true;
                comboBoxTests.Visible = true;
                labelTime.Visible = false;

                TestReview(null, EventArgs.Empty);

                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = true;
                    recognitionLabel.Text = "Розпізнавання…";
                    recognitionLabel.ForeColor = Color.DimGray;
                }
            }
        }
        
        // Обробка команд, додатково цей метод уже показує у лейблі тексти для команд/диктування
        private async void OnVoiceCommand(VoiceCommand cmd)
        {
            void ShowCmd(string text)
            {
                if (recognitionLabel != null)
                {
                    recognitionLabel.Visible = true;
                    recognitionLabel.ForeColor = Color.ForestGreen;
                    recognitionLabel.Text = $"Розпізнано: {text}";
                }
            }

            switch (cmd.Type)
            {
                case VoiceCommandType.StartTest:
                    ShowCmd("команда — Почати тест");
                    if (buttonStartTest.Visible)
                        StartTestButton_Click(this, EventArgs.Empty);
                    break;

                case VoiceCommandType.ReviewTest:
                    ShowCmd("команда — Огляд тесту");
                    if (buttonReviewTest.Visible)
                        TestReview_Click(this, EventArgs.Empty);
                    break;

                case VoiceCommandType.SelectTestByName:
                    ShowCmd($"вибрати тест — {cmd.Argument}");
                    if (!string.IsNullOrWhiteSpace(cmd.Argument))
                    {
                        var name = cmd.Argument;
                        for (int i = 0; i < comboBoxTests.Items.Count; i++)
                        {
                            var item = comboBoxTests.Items[i]?.ToString();
                            if (string.Equals(item, name, StringComparison.OrdinalIgnoreCase))
                            {
                                comboBoxTests.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    break;

                case VoiceCommandType.NextTest:
                    ShowCmd("команда — Наступний тест");
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = Math.Min(comboBoxTests.SelectedIndex + 1, comboBoxTests.Items.Count - 1);
                    break;

                case VoiceCommandType.PreviousTest:
                    ShowCmd("команда — Попередній тест");
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = Math.Max(comboBoxTests.SelectedIndex - 1, 0);
                    break;

                case VoiceCommandType.FirstTest:
                    ShowCmd("команда — Перший тест");
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = 0;
                    break;

                case VoiceCommandType.LastTest:
                    ShowCmd("команда — Останній тест");
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = comboBoxTests.Items.Count - 1;
                    break;

                case VoiceCommandType.EnableTts:
                    ShowCmd("команда — Увімкнути озвучення");
                    _dictation.SetEnabled(true);
                    break;

                case VoiceCommandType.DisableTts:
                    ShowCmd("команда — Вимкнути озвучення");
                    _dictation.SetEnabled(false);
                    break;

                case VoiceCommandType.ExitApp:
                    ShowCmd("команда — Вийти");
                    Close();
                    break;

                case VoiceCommandType.NextQuestion:
                    ShowCmd("команда — Далі");
                    SimulateNextClick();
                    break;

                case VoiceCommandType.PreviousQuestion:
                    ShowCmd("команда — Попереднє питання");
                    break;

                case VoiceCommandType.SelectOptionIndex:
                    ShowCmd($"вибрати варіант — {cmd.Index}");
                    if (cmd.Index.HasValue) SelectSingleOptionByIndex(cmd.Index.Value);
                    break;

                case VoiceCommandType.ToggleOptionIndex:
                    ShowCmd($"перемкнути варіант — {cmd.Index}");
                    if (cmd.Index.HasValue) ToggleMultiOptionByIndex(cmd.Index.Value);
                    break;

                case VoiceCommandType.ClearSelection:
                    ShowCmd("команда — Очистити вибір");
                    ClearSelection();
                    break;

                case VoiceCommandType.SetTrue:
                    ShowCmd("команда — Істина");
                    SetTrueFalse(true);
                    break;

                case VoiceCommandType.SetFalse:
                    ShowCmd("команда — Хиба");
                    SetTrueFalse(false);
                    break;

                case VoiceCommandType.ReadQuestion:
                {
                    ShowCmd("команда — Прочитати питання");
                    var q = _testManager.GetCurrentQuestionForUser(_currentUser);
                    if (q != null) await _dictation.OnQuestionShownAsync(q);
                    break;
                }

                case VoiceCommandType.ReadOptions:
                {
                    ShowCmd("команда — Прочитати варіанти");
                    var q = _testManager.GetCurrentQuestionForUser(_currentUser);
                    if (q != null) await _dictation.OnQuestionShownAsync(q);
                    break;
                }

                case VoiceCommandType.StopReading:
                    ShowCmd("команда — Зупинити озвучення");
                    _dictation.OnNextQuestion();
                    break;

                case VoiceCommandType.ReadTime:
                    ShowCmd($"команда — Час ({labelTime.Text})");
                    break;

                case VoiceCommandType.InputTextAppend:
                    if (!string.IsNullOrWhiteSpace(cmd.Argument))
                    {
                        // Показати сам розпізнаний текст (не команда) і додати в поле
                        if (recognitionLabel != null)
                        {
                            recognitionLabel.Visible = true;
                            recognitionLabel.ForeColor = Color.ForestGreen;
                            recognitionLabel.Text = $"Розпізнано: {cmd.Argument}";
                        }
                        AppendToTextBox(cmd.Argument);
                    }
                    break;

                case VoiceCommandType.ClearText:
                    ShowCmd("команда — Очистити поле");
                    ClearTextBox();
                    break;
                case VoiceCommandType.None:
                    ShowCmd("не розпізнано");
                    break;
            }
        }
        
        // Симуляція натискання кнопки Наступне
        private void SimulateNextClick()
        {
            var btn = _questionPanel.Controls.OfType<Button>().FirstOrDefault(b => b.Text.Contains("Наступ", StringComparison.OrdinalIgnoreCase));
            if (btn != null)
                btn.PerformClick();
        }

        private void SelectSingleOptionByIndex(int index1)
        {
            if (index1 <= 0) return;
            var radios = _questionPanel.Controls.OfType<RadioButton>().ToList();
            if (radios.Count >= index1)
            {
                foreach (var r in radios) r.Checked = false;
                radios[index1 - 1].Checked = true;
                return;
            }

            var tf = radios;
            if (tf.Count == 2 && index1 <= 2)
            {
                tf[0].Checked = index1 == 1;
                tf[1].Checked = index1 == 2;
            }
        }

        private void ToggleMultiOptionByIndex(int index1)
        {
            if (index1 <= 0) return;
            var checks = _questionPanel.Controls.OfType<CheckBox>().ToList();
            if (checks.Count >= index1)
                checks[index1 - 1].Checked = !checks[index1 - 1].Checked;
        }

        private void ClearSelection()
        {
            foreach (var cb in _questionPanel.Controls.OfType<CheckBox>())
                cb.Checked = false;
            foreach (var rb in _questionPanel.Controls.OfType<RadioButton>())
                rb.Checked = false;
        }

        private void SetTrueFalse(bool value)
        {
            var radios = _questionPanel.Controls.OfType<RadioButton>().ToList();
            if (radios.Count == 2)
            {
                radios[0].Checked = value;
                radios[1].Checked = !value;
            }
        }

        private void AppendToTextBox(string text)
        {
            var tb = _questionPanel.Controls.OfType<TextBox>().FirstOrDefault();
            if (tb == null) return;

            if (tb.Text.Length > 0 && !char.IsWhiteSpace(tb.Text.Last()))
                tb.AppendText(" ");
            tb.AppendText(text);
        }

        private void ClearTextBox()
        {
            var tb = _questionPanel.Controls.OfType<TextBox>().FirstOrDefault();
            if (tb == null) return;
            tb.Clear();
        }
    }
}