using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ImageProcessing
{
    public partial class ScriptForm : Form
    {
        ScriptOutVal proc;

        internal ScriptForm(ScriptOutVal Proc)
        {
            proc = Proc;

            InitializeComponent();

            textBox1.Text = proc.CoreScript;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            proc.CoreScript = textBox1.Text;
            Form1.Frm.Execute();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            proc.CoreScript = textBox1.Text;
            e.Cancel = true;
            Hide();
        }
    }
}
