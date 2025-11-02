namespace MoodleTestReader.UI
{
    partial class Test
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
            this.comboBoxTests = new System.Windows.Forms.ComboBox();
            this.buttonStartTest = new System.Windows.Forms.Button();
            this.buttonReviewTest = new System.Windows.Forms.Button();
            this.questionPanel = new System.Windows.Forms.Panel();
            this.labelTime = new System.Windows.Forms.Label();
            this.recognitionLabel = new System.Windows.Forms.Label();
            this.buttonManageRoles = new Button();
            this.SuspendLayout();

            // comboBoxTests
            this.comboBoxTests.Location = new System.Drawing.Point(10, 10);
            this.comboBoxTests.Width = 200;
            this.comboBoxTests.Name = "comboBoxTests";
            this.comboBoxTests.Size = new System.Drawing.Size(200, 21);
            this.comboBoxTests.TabIndex = 0;

            // buttonStartTest
            this.buttonStartTest.Location = new System.Drawing.Point(220, 10);
            this.buttonStartTest.Width = 100;
            this.buttonStartTest.Name = "buttonStartTest";
            this.buttonStartTest.Size = new System.Drawing.Size(100, 23);
            this.buttonStartTest.Text = "Почати тест";
            this.buttonStartTest.UseVisualStyleBackColor = true;
            this.buttonStartTest.Click += new System.EventHandler(this.StartTestButton_Click);
            
            // buttonReviewTest
            this.buttonReviewTest.Location = new  System.Drawing.Point(220, 10);
            this.buttonReviewTest.Name = "buttonReviewTest";
            this.buttonReviewTest.Width = 100;
            this.buttonReviewTest.Size = new System.Drawing.Size(100, 23);
            this.buttonReviewTest.Text = "Огляд тесту";
            this.buttonReviewTest.UseVisualStyleBackColor = true;
            this.buttonReviewTest.Visible = false;
            this.buttonReviewTest.Click += new System.EventHandler(this.TestReview_Click);
            
            // buttonManageRoles (visible only for admin in runtime)
            this.buttonManageRoles.Location = new System.Drawing.Point(400, 60);
            this.buttonManageRoles.Name = "buttonManageRoles";
            this.buttonManageRoles.Size = new System.Drawing.Size(160, 23);
            this.buttonManageRoles.Text = "Управління ролями";
            this.buttonManageRoles.UseVisualStyleBackColor = true;
            this.buttonManageRoles.Visible = false;
            this.buttonManageRoles.Click += new System.EventHandler(this.ButtonManageRoles_Click);
            
            // recognitionLabel
            this.recognitionLabel.Location = new System.Drawing.Point(400, 40);
            this.recognitionLabel.Name = "recognitionLabel";
            this.recognitionLabel.AutoSize = true;
            this.recognitionLabel.ForeColor = Color.DimGray;
            this.recognitionLabel.Text = "Розпізнавання…";
            this.recognitionLabel.Visible = true;
            
            // questionPanel
            this.questionPanel.Name = "questionPanel";
            this.questionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.questionPanel.Location = new System.Drawing.Point(0, 0);
            this.questionPanel.Margin = new Padding(0);
            this.questionPanel.AutoScroll = true;
            this.questionPanel.BackColor = SystemColors.Window;

            // labelTime
            this.labelTime.Location = new System.Drawing.Point(400, 10);
            this.labelTime.Name = "labelTime";
            this.labelTime.AutoSize = true;
            this.labelTime.ForeColor = Color.Black;
            this.labelTime.Text = "Залишилось: 00:00";
            this.labelTime.Visible = false;

            // TestForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.comboBoxTests);
            this.Controls.Add(this.buttonStartTest);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.buttonReviewTest);
            this.Controls.Add(this.recognitionLabel);
            this.Controls.Add(this.questionPanel);
            this.Controls.Add(this.buttonManageRoles);
            this.Name = "Test";
            this.Text = "Тестовий модуль";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ComboBox comboBoxTests;
        private System.Windows.Forms.Button buttonStartTest;
        private System.Windows.Forms.Button buttonReviewTest;
        private System.Windows.Forms.Panel questionPanel;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.Label recognitionLabel;
        private System.Windows.Forms.Button buttonManageRoles;
    }
}