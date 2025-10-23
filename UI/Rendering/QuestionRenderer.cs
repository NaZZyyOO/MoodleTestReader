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
                            chk.Checked = userAnswer?.Type == "multi" && userAnswer.List != null && userAnswer.List.Contains(option);
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
                        Width = 400
                    };

                    if (mode == QuestionRenderMode.Review)
                    {
                        tb.ReadOnly = true;
                        tb.Text = userAnswer?.Type == "text" ? (userAnswer.Text ?? string.Empty) : string.Empty;
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
                        rTrue.Checked = userAnswer?.Type == "bool" && userAnswer.Bool == true;
                        rFalse.Checked = userAnswer?.Type == "bool" && userAnswer.Bool == false;
                    }

                    container.Controls.Add(rTrue);
                    container.Controls.Add(rFalse);
                    y += 60;
                    break;
                }

                default:
                {
                    foreach (var option in q.Options)
                    {
                        var rb = new RadioButton { Text = option, Location = new Point(10, y), AutoSize = true };
                        if (mode == QuestionRenderMode.Review)
                        {
                            rb.Enabled = false;
                            rb.Checked = userAnswer?.Type == "single"
                                         && string.Equals(userAnswer.Text, option, StringComparison.OrdinalIgnoreCase);
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