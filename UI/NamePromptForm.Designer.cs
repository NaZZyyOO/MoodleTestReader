namespace MoodleTestReader.UI
{
    partial class NamePromptForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lbl;
        private System.Windows.Forms.TextBox txt;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lbl = new System.Windows.Forms.Label();
            this.txt = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.lbl.AutoSize = true;
            this.lbl.Location = new System.Drawing.Point(12, 12);
            this.lbl.Text = "Введіть назву:";

            this.txt.Location = new System.Drawing.Point(15, 34);
            this.txt.Width = 360;

            this.btnOk.Location = new System.Drawing.Point(219, 70);
            this.btnOk.Text = "OK";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

            this.btnCancel.Location = new System.Drawing.Point(300, 70);
            this.btnCancel.Text = "Скасувати";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.ClientSize = new System.Drawing.Size(390, 110);
            this.Controls.Add(this.lbl);
            this.Controls.Add(this.txt);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Text = "Нова назва";

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}