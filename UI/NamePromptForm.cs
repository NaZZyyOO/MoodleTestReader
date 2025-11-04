using System.ComponentModel;

namespace MoodleTestReader.UI
{
    public partial class NamePromptForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? ResultText { get; private set; }

        public NamePromptForm(string labelText)
        {
            InitializeComponent();
            this.lbl.Text = labelText;
        }

        private void btnOk_Click(object? sender, EventArgs e)
        {
            ResultText = txt.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}