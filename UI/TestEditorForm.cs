using System.Data;
using MoodleTestReader.Data;
using MoodleTestReader.Logic;

namespace MoodleTestReader.UI
{
    public partial class TestEditorForm : Form
    {
        private readonly Models.Test _test;

        public TestEditorForm(Models.Test test)
        {
            InitializeComponent();
            _test = test;
            txtName.Text = _test.TestName;
            numTime.Value = Math.Max(1, _test.TimeLimit <= 0 ? 30 : _test.TimeLimit);
            BuildQuestionGrid();
            LoadQuestions();
            
            gridQuestions.RowHeadersWidth = 50;
            gridQuestions.RowPostPaint += GridQuestions_RowPostPaint;
        }
        
        private void GridQuestions_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            // малюємо номер (1-based) у заголовку рядка
            var index = (e.RowIndex + 1).ToString();
            var bounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, gridQuestions.RowHeadersWidth, e.RowBounds.Height);

            TextRenderer.DrawText(e.Graphics, index,
                gridQuestions.Font, bounds,
                SystemColors.ControlText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }

        private void BuildQuestionGrid()
        {
            gridQuestions.Columns.Clear();

            // Тип запитання
            var colType = new DataGridViewComboBoxColumn
            {
                Name = "Type",
                HeaderText = "Тип",
                DataPropertyName = "Type",
                DataSource = new[] { "SingleChoice", "MultipleChoice", "TrueFalse", "FillInBlank" }
            };
            var colDesc = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Питання",
                DataPropertyName = "Description"
            };
            var colPoints = new DataGridViewTextBoxColumn
            {
                Name = "Points",
                HeaderText = "Бали",
                DataPropertyName = "Points",
                Width = 60
            };
            var colOptions = new DataGridViewTextBoxColumn
            {
                Name = "Options",
                HeaderText = "Опції (; розділювач)",
                DataPropertyName = "Options"
            };
            var colCorrect = new DataGridViewTextBoxColumn
            {
                Name = "Correct",
                HeaderText = "Правильна(і) відповідь(і) (; розділювач / для TF: true|false)",
                DataPropertyName = "Correct"
            };

            gridQuestions.Columns.AddRange(colType, colDesc, colPoints, colOptions, colCorrect);
        }

        private void LoadQuestions()
        {
            var table = new DataTable();
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Points", typeof(int));
            table.Columns.Add("Options", typeof(string));
            table.Columns.Add("Correct", typeof(string));

            foreach (var q in _test.Questions)
            {
                switch (q)
                {
                    case MultipleChoiceQuestion mc:
                        table.Rows.Add("MultipleChoice", q.question, q.Points,
                            string.Join("; ", q.Options ?? new List<string>()),
                            string.Join("; ", mc.CorrectAnswers ?? new List<string>()));
                        break;
                    case FillInBlankQuestion fib:
                        table.Rows.Add("FillInBlank", q.question, q.Points,
                            string.Join("; ", q.Options ?? new List<string>()),
                            string.Join("; ", fib.CorrectAnswers ?? new List<string>()));
                        break;
                    case TrueFalseQuestion tf:
                        table.Rows.Add("TrueFalse", q.question, q.Points,
                            "", tf.Answer ? "true" : "false");
                        break;
                    default:
                        table.Rows.Add("SingleChoice", q.question, q.Points,
                            string.Join("; ", q.Options ?? new List<string>()),
                            q.CorrectAnswer ?? "");
                        break;
                }
            }

            gridQuestions.DataSource = table;
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введіть назву тесту.");
                return;
            }

            _test.TestName = txtName.Text.Trim();
            _test.TimeLimit = (int)numTime.Value;

            var table = gridQuestions.DataSource as DataTable;
            if (table == null)
            {
                MessageBox.Show("Таблиця питань порожня або некоректна.");
                return;
            }

            var newQuestions = new List<Question>();

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                var type = (row["Type"]?.ToString() ?? "SingleChoice").Trim();
                var desc = row["Description"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(desc)) continue;

                var points = 0;
                int.TryParse(row["Points"]?.ToString(), out points);
                points = Math.Max(1, points);

                var options = (row["Options"]?.ToString() ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

                var correctRaw = (row["Correct"]?.ToString() ?? "").Trim();

                switch (type)
                {
                    case "MultipleChoice":
                    {
                        var corrects = correctRaw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                        newQuestions.Add(new MultipleChoiceQuestion
                        {
                            question = desc,
                            Points = points,
                            Options = options,
                            CorrectAnswers = corrects
                        });
                        break;
                    }
                    case "FillInBlank":
                    {
                        var corrects = correctRaw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                        newQuestions.Add(new FillInBlankQuestion
                        {
                            question = desc,
                            Points = points,
                            Options = options,
                            CorrectAnswers = corrects
                        });
                        break;
                    }
                    case "TrueFalse":
                    {
                        var tf = string.Equals(correctRaw, "true", StringComparison.OrdinalIgnoreCase);
                        newQuestions.Add(new TrueFalseQuestion
                        {
                            question = desc,
                            Points = points,
                            Options = options,
                            Answer = tf
                        });
                        break;
                    }
                    default:
                    {
                        newQuestions.Add(new Question
                        {
                            question = desc,
                            Points = points,
                            Options = options,
                            CorrectAnswer = correctRaw
                        });
                        break;
                    }
                }
            }

            _test.Questions = newQuestions;

            try
            {
                DataLoader.SaveTest(_test);
                MessageBox.Show("Збережено.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка збереження: " + ex.Message);
            }
        }
    }
}