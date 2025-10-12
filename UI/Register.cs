using MoodleTestReader.Data;

namespace MoodleTestReader.UI;

public partial class Register : Form
{
    public Register()
    {
        InitializeComponent();
    }
    
    private void RegisterButton_Click(object sender, EventArgs e)
    {
        var newUsername = textBoxNewUsername.Text;
        var newPassword = textBoxNewPassword.Text;
        var confirmPassword = textBoxConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
        {
            MessageBox.Show("Заповніть усі поля.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Паролі не співпадають.");
            return;
        }

        var dataLoader = new DataLoader();
        if (dataLoader.UserExists(newUsername))
        {
            MessageBox.Show("Користувач з таким логіном уже існує.");
            return;
        }

        dataLoader.CreateUser(newUsername, newPassword);
        DialogResult = DialogResult.OK;
        Close();
    }
}