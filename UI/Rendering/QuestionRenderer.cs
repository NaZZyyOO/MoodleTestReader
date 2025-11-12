using MoodleTestReader.Logic;
using MoodleTestReader.Models;
using MoodleTestReader.Models.Results;

namespace MoodleTestReader.UI.Rendering
{
    /// <summary>
    /// Відповідає за побудову UI для питання в залежності від режиму (проходження/огляд).
    /// Усі контролі додаються у переданий container.
    /// </summary>
    public static class QuestionRenderer
    {
        /// <summary>
        /// Рендерить питання у вказаний контейнер.
        /// </summary>
        /// <param name="container">Панель/контейнер форми.</param>
        /// <param name="q">Питання для рендерингу.</param>
        /// <param name="mode">Режим (Play/Review).</param>
        /// <param name="userAnswer">Опційно: відповідь користувача (для Review).</param>
        /// <param name="includeQuestionTitle">Чи виводити заголовок питання у верхній частині.</param>
        /// <param name="headerPrefix">Необов’язковий префікс перед текстом питання (наприклад, "1. ").</param>
        public static void RenderQuestion(
            Control container,
            Question q,
            QuestionRenderMode mode,
            UserAnswer? userAnswer = null,
            bool includeQuestionTitle = true,
            string? headerPrefix = null)
        {
            container.Controls.Clear();

            // Нормалізація типу відповіді, приймаємо як короткі ("multi"), так і повні ("MultipleChoice") назви
            string uaType = (userAnswer?.Type ?? "").Trim().ToLowerInvariant();
            bool isSingle = uaType == "single" || uaType == "singlechoice";
            bool isMulti = uaType == "multi" || uaType == "multiplechoice" || uaType == "mcq";
            bool isText = uaType == "text" || uaType == "fillinblank" || uaType == "fib";
            bool isBool = uaType == "bool" || uaType == "truefalse" || uaType == "tf";

            int y;
            if (includeQuestionTitle)
            {
                var labelQuestion = new Label
                {
                    Text = $"{headerPrefix}{q.question}",
                    Location = new Point(10, 10),
                    AutoSize = true
                };
                container.Controls.Add(labelQuestion);
                y = labelQuestion.Bottom + 10;
            }
            else
            {
                y = 10;
            }

            switch (q)
            {
                case MultipleChoiceQuestion mcq:
                {
                    foreach (var option in mcq.Options)
                    {
                        var chk = new CheckBox
                        {
                            Text = option,
                            Location = new Point(10, y),
                            AutoSize = true
                        };

                        if (mode == QuestionRenderMode.Review)
                        {
                            chk.Enabled = false;

                            // Позначення без урахування регістру; приймаємо тільки коли це multi-відповідь
                            bool selected = false;
                            if (isMulti && userAnswer?.List != null)
                            {
                                selected = userAnswer.List.Any(v =>
                                    string.Equals(v ?? "", option ?? "", StringComparison.OrdinalIgnoreCase));
                            }
                            chk.Checked = selected;
                        }

                        container.Controls.Add(chk);
                        y += 28;
                    }
                    break;
                }

                case FillInBlankQuestion:
                {
                    var tb = new TextBox
                    {
                        Location = new Point(10, y),
                        Width = 500
                    };

                    if (mode == QuestionRenderMode.Review)
                    {
                        tb.ReadOnly = true;
                        // Приймаємо "text" та також синонім "fillinblank"
                        if (isText)
                            tb.Text = userAnswer?.Text ?? string.Empty;
                        else
                            tb.Text = userAnswer?.Text ?? string.Empty; // фолбек, якщо тип не виставили
                    }

                    container.Controls.Add(tb);
                    y += 30;
                    break;
                }

                case TrueFalseQuestion:
                {
                    var rTrue = new RadioButton { Text = "True", Location = new Point(10, y), AutoSize = true };
                    var rFalse = new RadioButton { Text = "False", Location = new Point(10, y + 28), AutoSize = true };

                    if (mode == QuestionRenderMode.Review)
                    {
                        rTrue.Enabled = false;
                        rFalse.Enabled = false;

                        bool? val = null;
                        if (isBool)
                        {
                            val = userAnswer?.Bool;
                            // фолбек: іноді в текст може прийти "true"/"false"
                            if (val == null && !string.IsNullOrWhiteSpace(userAnswer?.Text))
                            {
                                if (bool.TryParse(userAnswer.Text, out var parsed))
                                    val = parsed;
                            }
                        }

                        rTrue.Checked = val == true;
                        rFalse.Checked = val == false;
                    }

                    container.Controls.Add(rTrue);
                    container.Controls.Add(rFalse);
                    y += 60;
                    break;
                }

                default:
                {
                    // SingleChoice
                    foreach (var option in q.Options)
                    {
                        var rb = new RadioButton { Text = option, Location = new Point(10, y), AutoSize = true };
                        if (mode == QuestionRenderMode.Review)
                        {
                            rb.Enabled = false;

                            // Позначаємо, якщо це single-відповідь, або фолбек якщо тип не виставлений, але є текст
                            bool selected = false;
                            var uaText = userAnswer?.Text ?? string.Empty;

                            if (isSingle || string.IsNullOrWhiteSpace(uaType))
                            {
                                selected = !string.IsNullOrEmpty(uaText) &&
                                           string.Equals(uaText, option, StringComparison.OrdinalIgnoreCase);
                            }

                            rb.Checked = selected;
                        }
                        container.Controls.Add(rb);
                        y += 28;
                    }
                    break;
                }
            }
        }
    }
}