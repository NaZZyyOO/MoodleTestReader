using System.Data;
using System.Windows.Forms;
using MoodleTestReader.Data;
using MoodleTestReader.Models;

namespace MoodleTestReader.UI
{
    public partial class TestManagerForm : Form
    {
        private readonly User _currentUser;

        public TestManagerForm(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            BuildGrid();
            LoadTests();
        }

        private void BuildGrid()
        {
            gridTests.Columns.Clear();

            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "",
                Width = 40
            };
            var colId = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                DataPropertyName = "Id",
                ReadOnly = true,
                Width = 60
            };
            var colName = new DataGridViewTextBoxColumn
            {
                Name = "TestName",
                HeaderText = "Назва",
                DataPropertyName = "TestName",
                ReadOnly = true
            };
            var colAuthor = new DataGridViewTextBoxColumn
            {
                Name = "Author",
                HeaderText = "Автор",
                DataPropertyName = "Author",
                ReadOnly = true,
                Width = 120
            };

            gridTests.Columns.AddRange(colSelect, colId, colName, colAuthor);
            gridTests.CellContentClick += GridTests_CellContentClick;
        }

        private void GridTests_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridTests.Columns[e.ColumnIndex].Name == "Select")
            {
                // дозволяти рівно один обраний чекбокс
                foreach (DataGridViewRow row in gridTests.Rows)
                {
                    if (row.Index != e.RowIndex)
                    {
                        row.Cells["Select"].Value = false;
                    }
                }
                var cell = gridTests.Rows[e.RowIndex].Cells["Select"];
                var cur = cell.Value is bool b && b;
                cell.Value = !cur;
            }
        }

        private void LoadTests()
        {
            var tests = DataLoader.GetAvailableTests();
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("TestName", typeof(string));
            table.Columns.Add("Author", typeof(string));

            foreach (var t in tests)
            {
                if (_currentUser.ProfessorsTests.Contains(t.Id))
                {
                    var userName = DataLoader.GetUserById(t.AuthorId)?.Username;
                    table.Rows.Add(t.Id, t.TestName, userName);
                }
            }

            gridTests.DataSource = table;
        }

        private void btnCreate_Click(object? sender, EventArgs e)
        {
            using var dlg = new NamePromptForm("Назва нового тесту:");
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var name = dlg.ResultText?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Введіть назву тесту.");
                    return;
                }

                // Створити тест з автором = поточний користувач
                var t = DataLoader.CreateTest(name, _currentUser.Id, timeLimit: 30);

                // Додати в in-memory поле викладача
                _currentUser.ProfessorsTests ??= new List<int>();
                if (!_currentUser.ProfessorsTests.Contains(t.Id))
                    _currentUser.ProfessorsTests.Add(t.Id);

                LoadTests();

                // Запропонувати одразу редагувати
                if (MessageBox.Show("Тест створено. Відкрити редактор?", "Успіх", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    OpenEditorForTest(t.Id);
                }
            }
        }

        private void btnEdit_Click(object? sender, EventArgs e)
        {
            // Знайти єдиний рядок з Select = true
            var selected = gridTests.Rows
                .Cast<DataGridViewRow>()
                .FirstOrDefault(r => r.Cells["Select"].Value is bool b && b);

            if (selected == null)
            {
                MessageBox.Show("Оберіть рівно один тест для редагування (прапорець у першій колонці).");
                return;
            }

            var testId = (int)(selected.Cells["Id"].Value ?? 0);
            if (testId <= 0)
            {
                MessageBox.Show("Некоректний Id тесту.");
                return;
            }

            OpenEditorForTest(testId);
        }

        private void OpenEditorForTest(int testId)
        {
            // Підтягнемо актуальний тест
            var t = DataLoader.GetAvailableTests().FirstOrDefault(x => x.Id == testId);
            if (t == null)
            {
                MessageBox.Show("Тест не знайдено.");
                return;
            }

            if (t.AuthorId != _currentUser.Id)
            {
                MessageBox.Show("Ви не є автором цього тесту!");
                return;
            }

            using var editor = new TestEditorForm(t);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                // Після збереження — оновити таблицю
                LoadTests();
            }
        }
    }
}