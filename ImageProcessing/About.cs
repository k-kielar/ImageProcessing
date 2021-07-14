using System;
using System.Windows.Forms;

namespace ImageProcessing
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();

            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            label4.Text = ver.Major.ToString() + "." + ver.Minor.ToString() + "." + ver.Revision.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
