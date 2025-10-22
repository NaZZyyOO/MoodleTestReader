using System.Threading.Tasks;
using MoodleTestReader.Logic;
using MoodleTestReader.Models;
using MoodleTestReader.Services;
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

                MessageBox.Show($"Тест завершено. Ваш результат: {score} балів. Залишковий час: {TimeSpan.FromSeconds(_remainingTime):mm\\:ss}");
                return;
            }

            var labelQuestion = new Label { Text = question.question, Location = new Point(10, 10), AutoSize = true };
            _questionPanel.Controls.Add(labelQuestion);

            switch (question)
            {
                case MultipleChoiceQuestion mcq:
                {
                    int y = 50;
                    foreach (var option in mcq.Options)
                    {
                        var checkBox = new CheckBox { Text = option, Location = new Point(10, y), AutoSize = true };
                        _questionPanel.Controls.Add(checkBox);
                        y += 30;
                    }
                    break;
                }
                case FillInBlankQuestion:
                {
                    var textBox = new TextBox { Location = new Point(10, 50), Width = 200 };
                    _questionPanel.Controls.Add(textBox);
                    break;
                }
                case TrueFalseQuestion:
                {
                    var radioTrue = new RadioButton { Text = "True", Location = new Point(10, 50), AutoSize = true };
                    var radioFalse = new RadioButton { Text = "False", Location = new Point(10, 80), AutoSize = true };
                    _questionPanel.Controls.Add(radioTrue);
                    _questionPanel.Controls.Add(radioFalse);
                    break;
                }
                default:
                {
                    int y = 50;
                    foreach (var option in question.Options)
                    {
                        var radio = new RadioButton { Text = option, Location = new Point(10, y), AutoSize = true };
                        _questionPanel.Controls.Add(radio);
                        y += 30;
                    }
                    break;
                }
            }

            var buttonNext = new Button { Text = "Наступне", Location = new Point(10, 200), Width = 150, Height = 30 };
            buttonNext.Click += NextButton_Click;
            _questionPanel.Controls.Add(buttonNext);

            // Запустити озвучення для поточного питання (якщо увімкнено у сервісі)
            await _dictation.OnQuestionShownAsync(question);
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            var question = _testManager.GetCurrentQuestionForUser(_currentUser);
            object answer = null;

            switch (question)
            {
                case MultipleChoiceQuestion mcq:
                {
                    answer = new List<string>();
                    foreach (Control control in _questionPanel.Controls)
                    {
                        if (control is CheckBox checkBox && checkBox.Checked)
                        {
                            ((List<string>)answer).Add(checkBox.Text);
                        }
                    }
                    break;
                }
                case FillInBlankQuestion:
                {
                    foreach (Control control in _questionPanel.Controls)
                    {
                        if (control is TextBox textBox)
                        {
                            answer = textBox.Text;
                            break;
                        }
                    }
                    break;
                }
                case TrueFalseQuestion:
                {
                    foreach (Control control in _questionPanel.Controls)
                    {
                        if (control is RadioButton radio && radio.Checked)
                        {
                            answer = bool.Parse(radio.Text);
                            break;
                        }
                    }
                    break;
                }
                default:
                {
                    foreach (Control control in _questionPanel.Controls)
                    {
                        if (control is RadioButton radio && radio.Checked)
                        {
                            answer = radio.Text;
                            break;
                        }
                    }
                    break;
                }
            }

            if (answer == null)
            {
                MessageBox.Show("Оберіть відповідь.");
                return;
            }

            // Повідомляємо сервіс про перехід (він скасовує поточне читання і збільшує номер)
            _dictation.OnNextQuestion();
            _testManager.SubmitAnswerForUser(_currentUser, answer);

            _questionNumber++;
            await ShowCurrentQuestionAsync();
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
                    buttonStartTest.Visible = false;
                    buttonReviewTest.Visible = true;
                }

                // Показуємо перемикач диктування на екрані вибору
                _dictation.OnTestSelected();
            }
        }

        private void TestReview_Click(object? sender, EventArgs e)
        {

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
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { _dictation?.Dispose(); } catch { }
            base.OnFormClosing(e);
        }
    }
}