using System.Data;
using System.Windows.Forms;
using MoodleTestReader.Data;
using MoodleTestReader.Models;

namespace MoodleTestReader.UI
{
    public partial class AdminRolesForm : Form
    {

        public AdminRolesForm()
        {
            InitializeComponent();
            BuildGrid();
            LoadUsers();
        }

        private void BuildGrid()
        {
            // критично: вимкнути автогенерацію, щоб грід не додавав дублікати колонок
            gridUsers.AutoGenerateColumns = false;

            gridUsers.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                DataPropertyName = "Id",
                Visible = false,
                ReadOnly = true
            };

            var colUsername = new DataGridViewTextBoxColumn
            {
                Name = "Username",
                HeaderText = "Логін",
                DataPropertyName = "Username",
                ReadOnly = true
            };

            var colStudent = new DataGridViewCheckBoxColumn
            {
                Name = "Student",
                HeaderText = "Студент",
                DataPropertyName = "Student",   // важливо: прив’язка до DataTable["Student"]
                TrueValue = true,
                FalseValue = false,
                ThreeState = false
            };

            gridUsers.Columns.AddRange(colId, colUsername, colStudent);
        }

        private void LoadUsers()
        {
            var users = DataLoader.GetAllUsers();

            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Username", typeof(string));
            table.Columns.Add("Student", typeof(bool));

            foreach (var u in users)
            {
                // Student = !IsProfessor
                table.Rows.Add(u.Id, u.Username, !u.IsProfessor);
            }

            // джерело даних після побудови колонок
            gridUsers.DataSource = table;
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
            // зафіксувати редагування клітинок перед читанням
            gridUsers.EndEdit();

            if (gridUsers.DataSource is not DataTable dt) return;

            foreach (DataRow row in dt.Rows)
            {
                var id = (int)row["Id"];
                var isStudent = row["Student"] is bool b && b;
                var isProfessor = !isStudent;
                DataLoader.UpdateUserProfessorFlag(id, isProfessor);
            }

            MessageBox.Show("Зміни збережені.");
            LoadUsers();
        }

        private void ButtonClose_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}