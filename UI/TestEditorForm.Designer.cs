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

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 14);
            this.lblName.Text = "Назва:";

            // txtName
            this.txtName.Location = new System.Drawing.Point(70, 10);
            this.txtName.Size = new System.Drawing.Size(520, 22);
            this.txtName.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);

            // lblTime
            this.lblTime.AutoSize = true;
            this.lblTime.Location = new System.Drawing.Point(600, 14);
            this.lblTime.Text = "Час (хв):";

            // numTime
            this.numTime.Location = new System.Drawing.Point(660, 10);
            this.numTime.Minimum = 1;
            this.numTime.Maximum = 90;
            this.numTime.Value = 30;
            this.numTime.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);

            // gridQuestions
            this.gridQuestions.Location = new System.Drawing.Point(12, 45);
            this.gridQuestions.Size = new System.Drawing.Size(960, 560);
            this.gridQuestions.AllowUserToAddRows = true;
            this.gridQuestions.AllowUserToDeleteRows = true;
            this.gridQuestions.AutoGenerateColumns = false;
            this.gridQuestions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridQuestions.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(887, 615);
            this.btnSave.Size = new System.Drawing.Size(85, 28);
            this.btnSave.Text = "Зберегти";
            this.btnSave.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // Form
            this.ClientSize = new System.Drawing.Size(984, 661);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.numTime);
            this.Controls.Add(this.gridQuestions);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblTime);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Text = "Редактор тесту";

            ((System.ComponentModel.ISupportInitialize)(this.numTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridQuestions)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}