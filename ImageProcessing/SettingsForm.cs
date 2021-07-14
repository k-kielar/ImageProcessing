using System;
using System.Windows.Forms;

namespace ImageProcessing
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            numericUpDown1.Value = Properties.Settings.Default.JPEGSaveQuality;
            if (Properties.Settings.Default.UserLang == "pl") comboBox1.SelectedIndex = 1;
            else comboBox1.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.JPEGSaveQuality = Math.Max(Math.Min((byte)numericUpDown1.Value, (byte)100), (byte)1);
            string setLang;
            if (comboBox1.SelectedIndex == 1)
            {
                setLang = "pl";
            }
            else
            {
                setLang = "en";
            }
            if (setLang != Properties.Settings.Default.UserLang)
            {
                Properties.Settings.Default.UserLang = setLang;
                string msg;
                switch (setLang)
                {
                    case "pl":
                        msg = "Język zostanie zmieniony przy następnym uruchomieniu programu";
                        break;
                    default:
                        msg = "Language will be changed on next program startup";
                        break;
                }
                MessageBox.Show(msg);
            }
        }
    }
}
