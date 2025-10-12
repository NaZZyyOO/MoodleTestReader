using System.ComponentModel;

namespace MoodleTestReader.UI;

partial class Login
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.labelUsername = new System.Windows.Forms.Label();
        this.labelPassword = new System.Windows.Forms.Label();
        this.textBoxUsername = new System.Windows.Forms.TextBox();
        this.textBoxPassword = new System.Windows.Forms.TextBox();
        this.loginButton = new System.Windows.Forms.Button();
        this.registerButton = new System.Windows.Forms.Button();
        this.SuspendLayout();

        // labelUsername
        this.labelUsername.AutoSize = true;
        this.labelUsername.Location = new System.Drawing.Point(30, 30);
        this.labelUsername.Name = "labelUsername";
        this.labelUsername.Size = new System.Drawing.Size(55, 13);
        this.labelUsername.Text = "Логін:";

        // textBoxUsername
        this.textBoxUsername.Location = new System.Drawing.Point(100, 27);
        this.textBoxUsername.Name = "textBoxUsername";
        this.textBoxUsername.Size = new System.Drawing.Size(150, 20);
        this.textBoxUsername.TabIndex = 0;

        // labelPassword
        this.labelPassword.AutoSize = true;
        this.labelPassword.Location = new System.Drawing.Point(30, 60);
        this.labelPassword.Name = "labelPassword";
        this.labelPassword.Size = new System.Drawing.Size(55, 13);
        this.labelPassword.Text = "Пароль:";

        // textBoxPassword
        this.textBoxPassword.Location = new System.Drawing.Point(100, 57);
        this.textBoxPassword.Name = "textBoxPassword";
        this.textBoxPassword.Size = new System.Drawing.Size(150, 20);
        this.textBoxPassword.TabIndex = 1;
        this.textBoxPassword.UseSystemPasswordChar = true;

        // loginButton
        this.loginButton.Location = new System.Drawing.Point(100, 90);
        this.loginButton.Name = "loginButton";
        this.loginButton.Size = new System.Drawing.Size(100, 30);
        this.loginButton.Text = "Увійти";
        this.loginButton.UseVisualStyleBackColor = true;
        this.loginButton.Click += new System.EventHandler(this.LoginButton_Click);

        // registerButton
        this.registerButton.Location = new System.Drawing.Point(100, 130);
        this.registerButton.Name = "registerButton";
        this.registerButton.Size = new System.Drawing.Size(100, 30);
        this.registerButton.Text = "Зареєструватися";
        this.registerButton.UseVisualStyleBackColor = true;
        this.registerButton.Click += new System.EventHandler(this.RegisterButton_Click);

        // LoginForm
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(400, 300);
        this.Controls.Add(this.labelUsername);
        this.Controls.Add(this.textBoxUsername);
        this.Controls.Add(this.labelPassword);
        this.Controls.Add(this.textBoxPassword);
        this.Controls.Add(this.loginButton);
        this.Controls.Add(this.registerButton);
        this.Name = "Login";
        this.Text = "Вхід";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label labelUsername;
    private System.Windows.Forms.TextBox textBoxUsername;
    private System.Windows.Forms.Label labelPassword;
    private System.Windows.Forms.TextBox textBoxPassword;
    private System.Windows.Forms.Button loginButton;
    private System.Windows.Forms.Button registerButton;
}
