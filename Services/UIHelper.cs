using MoodleTestReader.Logic;

namespace MoodleTestReader.Services
{
    // Допоміжні дії з UI-панеллю запитання
    public static class UIHelper
    {
        public static void SimulateNextClick(Panel questionPanel)
        {
            var btn = questionPanel.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Text.Contains("Наступ", StringComparison.OrdinalIgnoreCase));
            btn?.PerformClick();
        }

        public static object? ExtractAnswerFromQuestionPanel(Panel questionPanel, Question question)
        {
            switch (question)
            {
                case MultipleChoiceQuestion:
                {
                    var selected = new List<string>();
                    foreach (Control c in questionPanel.Controls)
                    {
                        if (c is CheckBox cb && cb.Enabled && cb.Checked)
                            selected.Add(cb.Text);
                    }
                    return selected;
                }
                case FillInBlankQuestion:
                {
                    foreach (Control c in questionPanel.Controls)
                        if (c is TextBox tb && !tb.ReadOnly)
                            return tb.Text;
                    return string.Empty;
                }
                case TrueFalseQuestion:
                {
                    foreach (Control c in questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return bool.Parse(rb.Text);
                    return null;
                }
                default:
                {
                    foreach (Control c in questionPanel.Controls)
                        if (c is RadioButton rb && rb.Enabled && rb.Checked)
                            return rb.Text;
                    return null;
                }
            }
        }

        public static void SelectSingleOptionByIndex(Panel questionPanel, int index1)
        {
            if (index1 <= 0) return;
            var radios = questionPanel.Controls.OfType<RadioButton>().ToList();
            if (radios.Count >= index1)
            {
                foreach (var r in radios) r.Checked = false;
                radios[index1 - 1].Checked = true;
                return;
            }

            if (radios.Count == 2 && index1 <= 2)
            {
                radios[0].Checked = index1 == 1;
                radios[1].Checked = index1 == 2;
            }
        }

        public static void ToggleMultiOptionByIndex(Panel questionPanel, int index1)
        {
            if (index1 <= 0) return;
            var checks = questionPanel.Controls.OfType<CheckBox>().ToList();
            if (checks.Count >= index1)
                checks[index1 - 1].Checked = !checks[index1 - 1].Checked;
        }

        public static void ClearSelection(Panel questionPanel)
        {
            foreach (var cb in questionPanel.Controls.OfType<CheckBox>())
                cb.Checked = false;
            foreach (var rb in questionPanel.Controls.OfType<RadioButton>())
                rb.Checked = false;
        }

        public static void SetTrueFalse(Panel questionPanel, bool value)
        {
            var radios = questionPanel.Controls.OfType<RadioButton>().ToList();
            if (radios.Count == 2)
            {
                radios[0].Checked = value;
                radios[1].Checked = !value;
            }
        }

        public static void AppendToTextBox(Panel questionPanel, string text)
        {
            var tb = questionPanel.Controls.OfType<TextBox>().FirstOrDefault();
            if (tb == null) return;

            if (tb.Text.Length > 0 && !char.IsWhiteSpace(tb.Text.Last()))
                tb.AppendText(" ");
            tb.AppendText(text);
        }

        public static void ClearTextBox(Panel questionPanel)
        {
            var tb = questionPanel.Controls.OfType<TextBox>().FirstOrDefault();
            tb?.Clear();
        }
    }
}