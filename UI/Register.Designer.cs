using System.ComponentModel;

namespace MoodleTestReader.UI;

partial class Register
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
        this.labelNewUsername = new System.Windows.Forms.Label();
        this.labelNewPassword = new System.Windows.Forms.Label();
        this.labelConfirmPassword = new System.Windows.Forms.Label();
        this.textBoxNewUsername = new System.Windows.Forms.TextBox();
        this.textBoxNewPassword = new System.Windows.Forms.TextBox();
        this.textBoxConfirmPassword = new System.Windows.Forms.TextBox();
        this.registerButton = new System.Windows.Forms.Button();
        this.SuspendLayout();

        // labelNewUsername
        this.labelNewUsername.AutoSize = true;
        this.labelNewUsername.Location = new System.Drawing.Point(30, 30);
        this.labelNewUsername.Name = "labelNewUsername";
        this.labelNewUsername.Size = new System.Drawing.Size(55, 13);
        this.labelNewUsername.Text = "Логін:";

        // textBoxNewUsername
        this.textBoxNewUsername.Location = new System.Drawing.Point(100, 27);
        this.textBoxNewUsername.Name = "textBoxNewUsername";
        this.textBoxNewUsername.Size = new System.Drawing.Size(150, 20);
        this.textBoxNewUsername.TabIndex = 0;

        // labelNewPassword
        this.labelNewPassword.AutoSize = true;
        this.labelNewPassword.Location = new System.Drawing.Point(30, 60);
        this.labelNewPassword.Name = "labelNewPassword";
        this.labelNewPassword.Size = new System.Drawing.Size(55, 13);
        this.labelNewPassword.Text = "Пароль:";

        // textBoxNewPassword
        this.textBoxNewPassword.Location = new System.Drawing.Point(100, 57);
        this.textBoxNewPassword.Name = "textBoxNewPassword";
        this.textBoxNewPassword.Size = new System.Drawing.Size(150, 20);
        this.textBoxNewPassword.TabIndex = 1;
        this.textBoxNewPassword.UseSystemPasswordChar = true;

        // labelConfirmPassword
        this.labelConfirmPassword.AutoSize = true;
        this.labelConfirmPassword.Location = new System.Drawing.Point(30, 90);
        this.labelConfirmPassword.Name = "labelConfirmPassword";
        this.labelConfirmPassword.Size = new System.Drawing.Size(80, 13);
        this.labelConfirmPassword.Text = "Підтвердити пароль:";

        // textBoxConfirmPassword
        this.textBoxConfirmPassword.Location = new System.Drawing.Point(100, 87);
        this.textBoxConfirmPassword.Name = "textBoxConfirmPassword";
        this.textBoxConfirmPassword.Size = new System.Drawing.Size(150, 20);
        this.textBoxConfirmPassword.TabIndex = 2;
        this.textBoxConfirmPassword.UseSystemPasswordChar = true;

        // registerButton
        this.registerButton.Location = new System.Drawing.Point(100, 120);
        this.registerButton.Name = "registerButton";
        this.registerButton.Size = new System.Drawing.Size(100, 30);
        this.registerButton.Text = "Зареєструватися";
        this.registerButton.UseVisualStyleBackColor = true;
        this.registerButton.Click += new System.EventHandler(this.RegisterButton_Click);

        // RegisterForm
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(300, 180);
        this.Controls.Add(this.labelNewUsername);
        this.Controls.Add(this.textBoxNewUsername);
        this.Controls.Add(this.labelNewPassword);
        this.Controls.Add(this.textBoxNewPassword);
        this.Controls.Add(this.labelConfirmPassword);
        this.Controls.Add(this.textBoxConfirmPassword);
        this.Controls.Add(this.registerButton);
        this.Name = "RegisterForm";
        this.Text = "Реєстрація";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.Label labelNewUsername;
    private System.Windows.Forms.TextBox textBoxNewUsername;
    private System.Windows.Forms.Label labelNewPassword;
    private System.Windows.Forms.TextBox textBoxNewPassword;
    private System.Windows.Forms.Label labelConfirmPassword;
    private System.Windows.Forms.TextBox textBoxConfirmPassword;
    private System.Windows.Forms.Button registerButton;
}