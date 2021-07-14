using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessing
{
    public partial class ImgAnalyzeForm : Form
    {
        ImageAnalyzer.ImageAnalyzer imgAn;

        public ImgAnalyzeForm()
        {
            InitializeComponent();

            imgAn = new ImageAnalyzer.ImageAnalyzer(pictureBox1, pictureBox2);
        }

        public void SetBmp(Bitmap bmp)
        {
            imgAn.SetBitmap(bmp);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            imgAn.SetAnalyseMethod(ImageAnalyzer.ImageAnalyzer.AnalyseMethod.RGB);
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            imgAn.SetAnalyseMethod(ImageAnalyzer.ImageAnalyzer.AnalyseMethod.HCB);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            imgAn.MouseDown(e);
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            imgAn.MouseUp(e);
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            imgAn.MouseMove(e);
        }

        private void ImgAnalyzeForm_ResizeEnd(object sender, EventArgs e)
        {
        }

        private void ImgAnalyzeForm_SizeChanged(object sender, EventArgs e)
        {
            imgAn.Resize();

        }
    }
}
