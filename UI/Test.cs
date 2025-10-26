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
        private Panel _questionPanel;

        // Диктування
        private readonly TestDictationService _dictation;
        // Голосові команди
        private VoiceCommandService _voice;

        // Лічильники питань
        private int _questionNumber;
        private int _totalQuestions;

        public Test()
        {
            InitializeComponent();
            InitializeComponents();
            ShowLoginScreen();

            _testTimer = new Timer { Interval = 1000 };
            _testTimer.Tick += TestTimer_Tick;

            comboBoxTests.Click += TestReview;
            comboBoxTests.SelectedIndexChanged += TestReview;
            comboBoxTests.SelectedValueChanged += TestReview;

            _dictation = new TestDictationService(this);
            
            // Голосові команди: передаємо спосіб отримати назви тестів і колбек
            _voice = new VoiceCommandService(
                this,
                OnVoiceCommand,
                () => _testManager?.GetAvailableTests().Select(t => t.TestName).ToList() ?? new List<string>());

            // Голосові команди вмикаються/вимикаються разом із TTS (без окремого прапорця)
            _dictation.EnabledChanged += (_, enabled) =>
            {
                // На екрані вибору
                _voice.OnSelectionScreen(enabled);
            };
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
                
                // Оновити граматику вибору тестів голосом
                _voice.UpdateSelectionGrammar();
                // Показати перемикач TTS; і відповідно увімкнути/вимкнути слухач
                _dictation.OnTestSelected();
                _voice.OnSelectionScreen(_dictation.IsEnabled);
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
            
            // Після оновлення списку — оновити граматику
            _voice.UpdateSelectionGrammar();
        }
        
        // Один з головних методів: початок тесту
        private async void StartTestButton_Click(object? sender, EventArgs e)
        {
            // Перевіряємо, чи був обраний який небудь тест
            if (comboBoxTests.SelectedIndex == -1)
            {
                MessageBox.Show("Оберіть тест.");
                return;
            }
            
            // Перевіряємо чи такий тест існує
            var selectedTestName = comboBoxTests.SelectedItem?.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);
            if (currentTest == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }
            
            // Розпочинаємо сесію тесту: показуємо та ховаємо певні кнопки, запускаємо таймер
            _testManager.StartTestForUser(_currentUser, currentTest.Id);
            _remainingTime = currentTest.TimeLimit * 60;
            labelTime.Text = $"Залишилось: {currentTest.TimeLimit}:00";
            comboBoxTests.Visible = false;
            buttonStartTest.Visible = false;
            buttonReviewTest.Visible = false;
            labelTime.Visible = true;
            _startTime = DateTime.Now;
            _testTimer.Start();
            
            // Перше запитання та загальна кількість питань
            _questionNumber = 1;
            _totalQuestions = currentTest.Questions.Count;

            // Сховати прапорець про озвучку і підготувати сервіс
            _dictation.OnTestStarted();
            
            // Голосові команди: під час тесту — активні тільки якщо TTS увімкнений
            _voice.OnTestStarted(_dictation.IsEnabled);
            
            // Асинхронно показуємо поточне питання для користувача
            await ShowCurrentQuestionAsync();
        }

        private async Task ShowCurrentQuestionAsync()
        {
            _questionPanel.Controls.Clear();
            
            // Отримуємо поточне запитання з тестової сесії користувача
            if (_currentUser != null)
            {
                var question = _testManager.GetCurrentQuestionForUser(_currentUser);
                // Якщо далі немає запитань - тест закінчується
                if (question == null)
                {
                    _testTimer.Stop();
                    _testManager.SaveResultsForUser(_currentUser, out int score, _startTime);

                    // Повернути UI вибору тесту
                    comboBoxTests.Visible = true;
                    buttonStartTest.Visible = true;
                    labelTime.Visible = false;

                    // Озвучити результат і повернути перемикач
                    await _dictation.OnTestFinishedAsync(score);

                    // ОНОВИТИ СТАН КНОПОК ЗА ПОТОЧНИМ ВИБОРОМ
                    TestReview(null, EventArgs.Empty);
                    
                    _voice.OnTestFinished(_dictation.IsEnabled);
                    _voice.UpdateSelectionGrammar();

                    MessageBox.Show($"Тест завершено. Ваш результат: {score} балів. Залишковий час: {TimeSpan.FromSeconds(_remainingTime):mm\\:ss}");
                    return;
                }
            
                // Рендер запитання
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

                // Озвучення
                await _dictation.OnQuestionShownAsync(question, _totalQuestions, _questionNumber);
            }
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            // Отримуємо поточне запитання з тестової сесії користувача
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
            
            // Зберігаємо відповідь на запитання
            object? answer = ExtractAnswerFromQuestionPanel(question);
            
            // Якщо варіант/варіанти не обраний(і)/введена
            if (answer == null || (answer is List<string> list && list.Count == 0) || (answer is string s && string.IsNullOrWhiteSpace(s)))
            {
                MessageBox.Show("Оберіть відповідь.");
                return;
            }
            
            // Перевірка запитання, нарахування балів
            _testManager.SubmitAnswerForUser(_currentUser, answer);
            // Наступне запитання
            _questionNumber++;
            await ShowCurrentQuestionAsync();
        }
        
        /// <summary>
        /// Збирає відповідь користувача з елементів _questionPanel згідно з типом питання.
        /// </summary>
        private object? ExtractAnswerFromQuestionPanel(Question question)
        {
            switch (question)
            {
                // Вибір декількох відповідей
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
                // Вписати відповідь
                case FillInBlankQuestion:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is TextBox tb && !tb.ReadOnly)
                            return tb.Text;
                    return string.Empty;
                }
                // Так/Ні
                case TrueFalseQuestion:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return bool.Parse(rb.Text);
                    return null;
                }
                // Звичайне запитання, одна відповідь
                default:
                {
                    foreach (Control c in _questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return rb.Text;
                    return null;
                }
            }
        }
        
        // Певні дії з інтерфейсом коли була дія з вибором тесту
        private void TestReview(object? sender, EventArgs e)
        {
            // Перевірка чи обраний тест, якщо так, шукаємо тест з такою назвою
            if (comboBoxTests.SelectedItem == null) return;
            var selectedTestName = comboBoxTests.SelectedItem.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);

            if (currentTest != null)
            {
                if (_currentUser != null && _currentUser.TestResults.Any(result => result.TestId == currentTest.Id))
                {
                    // Є результати — показуємо Огляд, ховаємо Старт
                    buttonStartTest.Visible = false;
                    buttonReviewTest.Visible = true;
                }
                else
                {
                    // Нема результатів — показуємо Старт, ховаємо Огляд
                    buttonStartTest.Visible = true;
                    buttonReviewTest.Visible = false;
                }

                // Показуємо перемикач диктування на екрані вибору
                _dictation.OnTestSelected();
                
                // На екрані вибору — голосові команди активні відповідно до стану TTS
                _voice.OnSelectionScreen(_dictation.IsEnabled);
                _voice.UpdateSelectionGrammar();
            }
        }
        
        // Один з головних методів: огляд тесту
        private void TestReview_Click(object? sender, EventArgs e)
        {
            
            // Перевіряємо, чи був обраний який небудь тест
            if (comboBoxTests.SelectedIndex == -1)
            {
                MessageBox.Show("Оберіть тест.");
                return;
            }
            
            // Перевіряємо чи такий тест існує
            var selectedTestName = comboBoxTests.SelectedItem?.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);
            if (currentTest == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }
            
            // Шукаємо результати тесту цього користувача
            var result = _currentUser.TestResults.Where(r => r.TestId == currentTest.Id)
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

            // Прокручуваний контейнер де відображаються всі запитання 
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
                // Картка питання
                var card = new Panel
                {
                    Margin = new Padding(0, 0, 0, 10),
                    BorderStyle = BorderStyle.None,
                    Width = reviewFlow.ClientSize.Width - reviewFlow.Padding.Horizontal - 10
                };

                // Контент питання (заголовок + варіанти рендерить рендерер)
                var contentPanel = new Panel
                {
                    Location = new Point(0, 0),
                    Width = card.Width
                };

                result.Details.TryGetValue(q.Id, out var aws);
                var userAnswer = aws?.Answer;

                // ВАЖЛИВО: заголовок малюємо ТУТ, а не окремим Label’ом вище, щоб уникнути дублювання
                QuestionRenderer.RenderQuestion(
                    contentPanel,
                    q,
                    QuestionRenderMode.Review,
                    userAnswer,
                    includeQuestionTitle: true,
                    headerPrefix: $"{index}. "
                );

                // Висота за найнижчим контролом
                var contentBottom = contentPanel.Controls.Cast<Control>().Select(c => c.Bottom).DefaultIfEmpty(0).Max();
                contentPanel.Height = contentBottom;

                card.Controls.Add(contentPanel);

                // Бали за питання
                var pts = new Label
                {
                    Text = $"Бали: {(aws?.Points ?? 0)} з {q.Points}",
                    Location = new Point(0, contentPanel.Bottom + 6),
                    AutoSize = true
                };
                card.Controls.Add(pts);

                // Підіб’ємо висоту картки
                card.Height = pts.Bottom;

                reviewFlow.Controls.Add(card);
                index++;
            }

            // Кнопка виходу з огляду
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
            };
            reviewFlow.Controls.Add(btnClose);
        }
        
        // Метод для відображення змін в таймері
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

                // Озвучення результату і повернення перемикача
                _ = _dictation.OnTestFinishedAsync(score);

                MessageBox.Show($"Час вичерпано. Тест завершено. Ваш результат: {score} балів.");
                _questionPanel.Controls.Clear();

                buttonStartTest.Visible = true;
                comboBoxTests.Visible = true;
                labelTime.Visible = false;

                // ОНОВИТИ СТАН КНОПОК ЗА ПОТОЧНИМ ВИБОРОМ
                TestReview(null, EventArgs.Empty);
                
                // Повернути слухач голосу в режим вибору
                _voice.OnTestFinished(_dictation.IsEnabled);
                _voice.UpdateSelectionGrammar();
            }
        }
        // Обробка голосових команд (у UI-потоці)
        private void OnVoiceCommand(VoiceCommand cmd)
        {
            switch (cmd.Type)
            {
                case VoiceCommandType.StartTest:
                    if (buttonStartTest.Visible)
                        StartTestButton_Click(this, EventArgs.Empty);
                    break;

                case VoiceCommandType.ReviewTest:
                    if (buttonReviewTest.Visible)
                        TestReview_Click(this, EventArgs.Empty);
                    break;

                case VoiceCommandType.SelectTestByName:
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
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = Math.Min(comboBoxTests.SelectedIndex + 1, comboBoxTests.Items.Count - 1);
                    break;

                case VoiceCommandType.PreviousTest:
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = Math.Max(comboBoxTests.SelectedIndex - 1, 0);
                    break;

                case VoiceCommandType.FirstTest:
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = 0;
                    break;

                case VoiceCommandType.LastTest:
                    if (comboBoxTests.Items.Count > 0)
                        comboBoxTests.SelectedIndex = comboBoxTests.Items.Count - 1;
                    break;

                case VoiceCommandType.EnableTts:
                    _dictation.SetEnabled(true);
                    break;

                case VoiceCommandType.DisableTts:
                    _dictation.SetEnabled(false);
                    break;

                case VoiceCommandType.ExitApp:
                    Close();
                    break;
            }
        }
    }
}