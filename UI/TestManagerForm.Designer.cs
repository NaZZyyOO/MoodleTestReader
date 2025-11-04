namespace MoodleTestReader.UI
{
    partial class TestManagerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView gridTests;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnEdit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gridTests = new System.Windows.Forms.DataGridView();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.gridTests)).BeginInit();
            this.SuspendLayout();

            // gridTests
            this.gridTests.AllowUserToAddRows = false;
            this.gridTests.AllowUserToDeleteRows = false;
            this.gridTests.AutoGenerateColumns = false;
            this.gridTests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridTests.Location = new System.Drawing.Point(12, 12);
            this.gridTests.Name = "gridTests";
            this.gridTests.Size = new System.Drawing.Size(560, 360);
            this.gridTests.TabIndex = 0;

            // btnCreate
            this.btnCreate.Location = new System.Drawing.Point(585, 12);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(160, 32);
            this.btnCreate.Text = "Створити тест";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);

            // btnEdit
            this.btnEdit.Location = new System.Drawing.Point(585, 56);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(160, 32);
            this.btnEdit.Text = "Редагувати тест";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);

            // TestManagerForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(760, 384);
            this.Controls.Add(this.gridTests);
            this.Controls.Add(this.btnCreate);
            this.Controls.Add(this.btnEdit);
            this.Name = "TestManagerForm";
            this.Text = "Менеджер тестів";
            ((System.ComponentModel.ISupportInitialize)(this.gridTests)).EndInit();
            this.ResumeLayout(false);
        }
    }
}