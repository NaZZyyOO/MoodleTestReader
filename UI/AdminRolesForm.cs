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
            gridUsers.Columns.Clear();

            var colId = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "Id",
                DataPropertyName = "Id",
                Visible = false
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
                TrueValue = true,
                FalseValue = false
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
                // Student checkbox is checked when IsProfessor == false
                table.Rows.Add(u.Id, u.Username, !u.IsProfessor);
            }

            gridUsers.DataSource = table;
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
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