using MoodleTestReader.Data;
using MoodleTestReader.Models;

namespace MoodleTestReader.UI
{
    public partial class Login : Form
    {
        private User? _user;

        public Login()
        {
            InitializeComponent();
        }

        public User? GetUser() => _user;

        private void LoginButton_Click(object sender, EventArgs e)
        {
            string username = textBoxUsername.Text;
            string password = textBoxPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Заповніть усі поля.");
                return;
            }

            var dataLoader = new DataLoader();
            _user = dataLoader.AuthenticateUser(username, password);
            if (_user != null)
            {   
                _user.TestResults = dataLoader.GetUserTestResults(_user.Id);
                
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Неправильний логін або пароль.");
            }
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            var registerForm = new Register();
            if (registerForm.ShowDialog() == DialogResult.OK)
            {
                MessageBox.Show("Реєстрація успішна. Увійдіть, використовуючи нові дані.");
            }
        }
    }
}