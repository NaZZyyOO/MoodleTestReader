using MoodleTestReader.Logic;
using MoodleTestReader.Models;
using MoodleTestReader.Services;
using MoodleTestReader.UI.Rendering;
using Timer = System.Windows.Forms.Timer;

namespace MoodleTestReader.UI
{
    public partial class Test : Form
    {
        private User? _currentUser;
        private TestManager _testManager;
        private readonly Timer _testTimer;
        private int _remainingTime; // в секундах
        private DateTime _startTime;
        

        // Диктування та голосові команди
        private readonly TestDictationService _dictation;
        private readonly VoskCommandService _voiceCmd;

        // Лічильники питань
        private int _questionNumber;
        private int _totalQuestions;

        public Test()
        {
            InitializeComponent();

            _dictation = new TestDictationService(this);

            _voiceCmd = new VoskCommandService(
                this,
                OnVoiceCommand,
                () => _testManager?.GetAvailableTests().Select(t => t.TestName).ToList() ?? new List<string>(),
                () => _testManager?.GetCurrentQuestionForUser(_currentUser),
                () => questionPanel
            );

            if (recognitionLabel != null)
            {
                recognitionLabel.Visible = true;
                recognitionLabel.Text = "Розпізнавання…";
                recognitionLabel.ForeColor = Color.DimGray;
            }

            _voiceCmd.RecognizedNonCommandText += (_, txt) =>
            {
                try
                {
                    BeginInvoke(() =>
                    {
                        if (recognitionLabel == null) return;
                        var shouldShow = comboBoxTests.Visible || _dictation.IsEnabled;
                        recognitionLabel.Visible = shouldShow;
                        if (!shouldShow) return;

                        recognitionLabel.ForeColor = Color.DimGray;
                        recognitionLabel.Text = $"Розпізнано: {txt}";
                    });
                }
                catch { }
            };

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
        
        // Користувач "admin" має право редагувати ролі користувачів(студент, викладач) 
        private bool IsAdmin() =>
            _currentUser != null &&
            string.Equals(_currentUser.Username, "admin", StringComparison.OrdinalIgnoreCase);


        private void ShowLoginScreen()
        {
            var loginForm = new Login();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                _currentUser = loginForm.GetUser();
                _testManager = new TestManager();
                LoadAvailableTests();
                
                _voiceCmd.OnSelectionScreen();
                _voiceCmd.SetActive(_dictation.IsEnabled);
                _dictation.OnTestSelected();
                
                // Показати кнопку управління ролями лише для адміна
                buttonManageRoles.Visible = IsAdmin();
            }
            else
            {
                Close();
            }
        }
        
        private void ButtonManageRoles_Click(object? sender, EventArgs e)
        {
            using (var dlg = new AdminRolesForm())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ShowDialog(this);
            }
        }
        
        // Завантажити всі наявні тести в ComboBox
        // Можна покращити змінивши отримання всіх наявних тестів
        // на отримання всіх доступних тестів для користувача
        private void LoadAvailableTests()
        {
            var tests = _testManager.GetAvailableTests();
            comboBoxTests.Items.Clear();
            foreach (var test in tests)
            {
                comboBoxTests.Items.Add(test.TestName);
            }
        }
        
        // Запустити тест, якщо вибрано
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
            
            // Розпочинаємо сесію тесту за допомогою тестового менеджера
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
            
            // Запускаємо диктування та встановлюємо розпізнавання голосу як активне
            _dictation.OnTestStarted(_totalQuestions);
            _voiceCmd.SetActive(_dictation.IsEnabled);

            if (recognitionLabel != null)
            {
                recognitionLabel.Visible = _dictation.IsEnabled;
                recognitionLabel.Text = "Розпізнавання…";
                recognitionLabel.ForeColor = Color.DimGray;
            }
            
            // Відображення запитання в UI
            await ShowCurrentQuestionAsync();
        }

        private async Task ShowCurrentQuestionAsync()
        {
            questionPanel.Controls.Clear();

            var question = _testManager.GetCurrentQuestionForUser(_currentUser);

            // Якщо наступного запитання немає, то тест завершено.
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

                MessageBox.Show(
                    $"Тест завершено. Ваш результат: {score} балів. Залишковий час: {TimeSpan.FromSeconds(_remainingTime):mm\\:ss}");
                return;
            }

            // Використовуємо рендерер для рендеру запитання
            QuestionRenderer.RenderQuestion(
                questionPanel,
                question,
                QuestionRenderMode.Play,
                userAnswer: null,
                includeQuestionTitle: true,
                headerPrefix: null
            );

            var buttonNext = new Button { Text = "Наступне", Location = new Point(10, 240), Width = 150, Height = 30 };
            buttonNext.Click += NextButton_Click;
            questionPanel.Controls.Add(buttonNext);

            // Асинхронне диктування поточного питання
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

        // Перехід до наступного запитання, логіка зміни поточного запитання
        // Його перевірки, збереження відповіді та показ наступного запитання
        private async void NextButton_Click(object? sender, EventArgs e)
        {
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
            var answer = question != null 
                ? UIHelper.ExtractAnswerFromQuestionPanel(questionPanel, question)
                : null;
            
            if (answer == null || (answer is List<string> list && list.Count == 0) || (answer is string s && string.IsNullOrWhiteSpace(s)))
            {
                MessageBox.Show("Оберіть відповідь.");
                return;
            }
            
            _testManager.SubmitAnswerForUser(_currentUser, answer!);
            _questionNumber++;
            await ShowCurrentQuestionAsync();
        }
        
        // Метод для візуального відображення кнопок у випадку
        // Якщо у користувача цей тест завершений, 
        // Тоді тест повинен перейти в режим огляду для користувача
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
        
        // Огляд тесту
        // За допомогою рендереру генеруємо меню з 
        // панеллю яке можна прокручувати, де знаходяться
        // всі запитання тесту з відповідями користувача
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

            questionPanel.Controls.Clear();
            
            // Панель яку можна крутити горизонтально та вертикально
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

            questionPanel.Controls.Add(reviewFlow);
            
            // Генеруємо панелі питань та додаємо на панель з прокруткою
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
                questionPanel.Controls.Clear();
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
                questionPanel.Controls.Clear();

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
                    UIHelper.SimulateNextClick(questionPanel);
                    break;

                case VoiceCommandType.PreviousQuestion:
                    ShowCmd("команда — Попереднє питання");
                    break;

                case VoiceCommandType.SelectOptionIndex:
                    ShowCmd($"вибрати варіант — {cmd.Index}");
                    if (cmd.Index.HasValue) UIHelper.SelectSingleOptionByIndex(questionPanel, cmd.Index.Value);
                    break;

                case VoiceCommandType.ToggleOptionIndex:
                    ShowCmd($"перемкнути варіант — {cmd.Index}");
                    if (cmd.Index.HasValue) UIHelper.ToggleMultiOptionByIndex(questionPanel, cmd.Index.Value);
                    break;

                case VoiceCommandType.ClearSelection:
                    ShowCmd("команда — Очистити вибір");
                    UIHelper.ClearSelection(questionPanel);
                    break;

                case VoiceCommandType.SetTrue:
                    ShowCmd("команда — Істина");
                    UIHelper.SetTrueFalse(questionPanel, true);
                    break;

                case VoiceCommandType.SetFalse:
                    ShowCmd("команда — Хиба");
                    UIHelper.SetTrueFalse(questionPanel, false);
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
                        if (recognitionLabel != null)
                        {
                            recognitionLabel.Visible = true;
                            recognitionLabel.ForeColor = Color.ForestGreen;
                            recognitionLabel.Text = $"Розпізнано: {cmd.Argument}";
                        }
                        UIHelper.AppendToTextBox(questionPanel, cmd.Argument);
                    }
                    break;

                case VoiceCommandType.ClearText:
                    ShowCmd("команда — Очистити поле");
                    UIHelper.ClearTextBox(questionPanel);
                    break;

                case VoiceCommandType.None:
                default:
                    ShowCmd("не розпізнано");
                    break;
            }
        }
    }
}