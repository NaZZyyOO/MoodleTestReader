namespace MoodleTestReader.UI
{
    partial class TestEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.NumericUpDown numTime;
        private System.Windows.Forms.DataGridView gridQuestions;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblTime;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtName = new System.Windows.Forms.TextBox();
            this.numTime = new System.Windows.Forms.NumericUpDown();
            this.gridQuestions = new System.Windows.Forms.DataGridView();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblName = new System.Windows.Forms.Label();
            this.lblTime = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.numTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridQuestions)).BeginInit();
            this.SuspendLayout();

            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 12);
            this.lblName.Text = "Назва:";

            this.txtName.Location = new System.Drawing.Point(70, 10);
            this.txtName.Width = 400;

            this.lblTime.AutoSize = true;
            this.lblTime.Location = new System.Drawing.Point(480, 12);
            this.lblTime.Text = "Час (хв):";

            this.numTime.Location = new System.Drawing.Point(540, 10);
            this.numTime.Minimum = 1;
            this.numTime.Maximum = 300;
            this.numTime.Value = 30;

            this.gridQuestions.Location = new System.Drawing.Point(12, 45);
            this.gridQuestions.Size = new System.Drawing.Size(760, 360);
            this.gridQuestions.AllowUserToAddRows = true;
            this.gridQuestions.AllowUserToDeleteRows = true;
            this.gridQuestions.AutoGenerateColumns = false;
            this.gridQuestions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

            this.btnSave.Location = new System.Drawing.Point(687, 415);
            this.btnSave.Size = new System.Drawing.Size(85, 28);
            this.btnSave.Text = "Зберегти";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.ClientSize = new System.Drawing.Size(784, 451);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.numTime);
            this.Controls.Add(this.gridQuestions);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblTime);
            this.Text = "Редактор тесту";

            ((System.ComponentModel.ISupportInitialize)(this.numTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridQuestions)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}