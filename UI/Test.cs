using System.Threading.Tasks;
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

        // Диктування винесене у сервіс
        private TestDictationService _dictation;

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

        private async void StartTestButton_Click(object? sender, EventArgs e)
        {
            if (comboBoxTests.SelectedIndex == -1)
            {
                MessageBox.Show("Оберіть тест.");
                return;
            }

            var selectedTestName = comboBoxTests.SelectedItem.ToString();
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

            // Сховати прапорець і підготувати сервіс
            _dictation.OnTestStarted(_totalQuestions);

            await ShowCurrentQuestionAsync();
        }

        private async Task ShowCurrentQuestionAsync()
        {
            _questionPanel.Controls.Clear();
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
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

                MessageBox.Show($"Тест завершено. Ваш результат: {score} балів. Залишковий час: {TimeSpan.FromSeconds(_remainingTime):mm\\:ss}");
                return;
            }

            // ... всередині ShowCurrentQuestionAsync, після перевірки на завершення:
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
            await _dictation.OnQuestionShownAsync(question);
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
            var answer = question != null ? ExtractAnswerFromQuestionPanel(question) : null;

            if (answer == null || (answer is List<string> list && list.Count == 0) || (answer is string s && string.IsNullOrWhiteSpace(s)))
            {
                MessageBox.Show("Оберіть відповідь.");
                return;
            }

            _dictation.OnNextQuestion();
            _testManager.SubmitAnswerForUser(_currentUser, answer!);
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
                Console.WriteLine("Поточне айді тесту для перевірки: " + currentTest.Id);

                if (_currentUser.TestResults.Any(result => result.TestId == currentTest.Id))
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
            }
        }

        private void TestReview_Click(object? sender, EventArgs e)
        {
            if (comboBoxTests.SelectedItem == null)
            {
                MessageBox.Show("Оберіть тест для огляду.");
                return;
            }

            var selectedTestName = comboBoxTests.SelectedItem.ToString();
            var currentTest = _testManager.GetAvailableTests().FirstOrDefault(t => t.TestName == selectedTestName);
            if (currentTest == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }

            var result = _currentUser.TestResults?
                .Where(r => r.TestId == currentTest.Id)
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

            // Прокручуваний контейнер, щоб усе влізло
            var reviewFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10),
            };

            reviewFlow.Resize += (_, __) =>
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

                // ВАЖЛИВО: заголовок малюємо ТУТ, а не окремим Label’ом вище, щоб уникнути дублю
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
            btnClose.Click += (_, __) =>
            {
                _questionPanel.Controls.Clear();
                comboBoxTests.Visible = true;
                TestReview(null, EventArgs.Empty);
                labelTime.Visible = false;
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

                // Озвучення результату і повернення перемикача
                _ = _dictation.OnTestFinishedAsync(score);

                MessageBox.Show($"Час вичерпано. Тест завершено. Ваш результат: {score} балів.");
                _questionPanel.Controls.Clear();

                buttonStartTest.Visible = true;
                comboBoxTests.Visible = true;
                labelTime.Visible = false;

                // ОНОВИТИ СТАН КНОПОК ЗА ПОТОЧНИМ ВИБОРОМ
                TestReview(null, EventArgs.Empty);
            }
        }
    }
}