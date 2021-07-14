using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using KKLib;

namespace ImageAnalyzer
{
    class ImageAnalyzer
    {
        static Pair<Point> SharedLine = new Pair<Point>(new Point(100, 100), new Point(300, 100));

        PictureBox pictureBox1;
        PictureBox pictureBox1Line;
        PictureBox pictureBox2;

        Bitmap bmp;
        Bitmap bmpLine;
        Bitmap bmp2;
        Graphics gr;
        Graphics grLine;
        Graphics gr2;
        Bitmap img;
        Point pa, pb;

        AMethod ActiveMeth;

        readonly float dotR = 3;
        readonly Brush brDot;
        readonly Pen penLine;
        readonly Pen penWiden;
        readonly Pen penR;
        readonly Pen penG;
        readonly Pen penB;
        readonly float snapR = 10;

        public ImageAnalyzer(PictureBox pboxImg, PictureBox pboxChart, AnalyseMethod aMeth = AnalyseMethod.RGB)
        {
            pictureBox1 = pboxImg;
            pictureBox2 = pboxChart;

            pictureBox1Line = new PictureBox();
            pictureBox1Line.Size = pictureBox1.Size;
            pictureBox1Line.Location = new Point();
            pictureBox1Line.BackColor = Color.Transparent;
            pictureBox1.Controls.Add(pictureBox1Line);
            pictureBox1Line.MouseDown += PictureBox1Line_MouseDown;
            pictureBox1Line.MouseMove += PictureBox1Line_MouseMove;
            pictureBox1Line.MouseUp += PictureBox1Line_MouseUp;

            pa = SharedLine.Item1;
            pb = SharedLine.Item2;
            brDot = new SolidBrush(Color.Red);
            penLine = new Pen(Color.Orange, 3);
            penWiden = new Pen(Color.Black, 4.5f);
            penR = new Pen(Color.Red, 1);
            penG = new Pen(Color.Green, 1);
            penB = new Pen(Color.Blue, 1);

            NewSize();
            SetAnalyseMethod(aMeth);
        }

        private void PictureBox1Line_MouseUp(object sender, MouseEventArgs e)
        {
            MouseUp(e);
        }
        private void PictureBox1Line_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMove(e);
        }
        private void PictureBox1Line_MouseDown(object sender, MouseEventArgs e)
        {
            MouseDown(e);
        }

        public void SetBitmap(Bitmap Img)
        {
            img = Img;
            Draw();
        }
        public void SetAnalyseMethod(AnalyseMethod meth)
        {
            switch (meth)
            {
                case AnalyseMethod.RGB:
                    ActiveMeth = new RGBMethod();
                    penR.Color = Color.Red;
                    penG.Color = Color.Green;
                    penB.Color = Color.Blue;
                    break;
                case AnalyseMethod.HCB:
                    ActiveMeth = new HCBMethod();
                    penR.Color = Color.Orange;
                    penG.Color = Color.Purple;
                    penB.Color = Color.Gray;
                    break;
                default:
                    break;
            }
            Draw();
        }
        public void Resize()
        {
            NewSize();
            Draw();
        }

        Point? mclick;
        bool paMove;
        public void MouseDown(MouseEventArgs e)
        {
            var da = Mth.Dist(e.Location, pa);
            var db = Mth.Dist(e.Location, pb);
            if (da < snapR || db < snapR)
            {
                paMove = da < db;
                mclick = e.Location;
            }
        }
        public void MouseUp(MouseEventArgs e)
        {
            mclick = null;
        }
        public void MouseMove(MouseEventArgs e)
        {
            if (mclick != null)
            {
                if (paMove)
                {
                    pa = e.Location;
                    SharedLine.Item1 = e.Location;
                }
                else
                {
                    pb = e.Location;
                    SharedLine.Item2 = e.Location;
                }
                DrawQuick();
            }
        }

        void NewSize()
        {
            var todisp = new IDisposable[] { bmp, bmp2, gr, gr2 };
            bmp = new Bitmap(Math.Max(pictureBox1.Width, 1), Math.Max(pictureBox1.Height, 1));
            bmpLine = new Bitmap(bmp.Width, bmp.Height);
            bmp2 = new Bitmap(Math.Max(pictureBox2.Width, 1), Math.Max(pictureBox2.Height, 1));
            gr = Graphics.FromImage(bmp);
            grLine = Graphics.FromImage(bmpLine);
            gr2 = Graphics.FromImage(bmp2);
            pictureBox1.Image = bmp;
            pictureBox1Line.Image = bmpLine;
            pictureBox1Line.Size = pictureBox1.Size;
            pictureBox2.Image = bmp2;
            grLine.SmoothingMode = SmoothingMode.AntiAlias;
            gr2.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (var item in todisp)
            {
                if (item != null) item.Dispose();
            }
        }

        void Draw()
        {
            Draw1();
            DrawLine();
            Draw2();
        }
        void DrawQuick()
        {
            DrawLine();
            Draw2();
        }
        void Draw1()
        {
            if (img != null) gr.DrawImage(img, 0, 0, img.Width, img.Height);

            pictureBox1.Invalidate();
        }
        Region regLine = new Region();
        void DrawLine()
        {
            grLine.Clear(Color.Transparent);
            pictureBox1.Invalidate(regLine);

            var rec1 = new RectangleF(pa.X - dotR, pa.Y - dotR, dotR * 2, dotR * 2);
            var rec2 = new RectangleF(pb.X - dotR, pb.Y - dotR, dotR * 2, dotR * 2);

            var gp = new GraphicsPath();
            gp.AddLine(pa, pb);
            gp.AddEllipse(rec1);
            gp.AddEllipse(rec2);
            gp.Widen(penWiden);
            regLine.Dispose();
            regLine = new Region(gp);

            grLine.DrawLine(penLine, pa, pb);
            grLine.FillEllipse(brDot, rec1);
            grLine.FillEllipse(brDot, rec2);

            pictureBox1.Invalidate(regLine);
            pictureBox1.Update();
        }
        void Draw2()
        {
            if (img != null)
            {
                gr2.Clear(Color.White);
                using (var pxlr = new Pixeler(img))
                {
                    var pad = (PointF)pa;
                    var pbd = (PointF)pb;
                    var k = Mth.Dist(pad, pbd) / bmp2.Width;
                    float h = pictureBox2.Height;

                    Tripl<float>? prevCol = null;
                    for (int w = 0; w < bmp2.Width; w++)
                    {
                        var pf = Mth.OnLine(pad, pbd, w * k);
                        var p = new Point((int)pf.X, (int)pf.Y);
                        Tripl<float>? col;
                        if (p.X >= 0 && p.Y >= 0 && p.X < pxlr.Width && p.Y < pxlr.Height) col = ActiveMeth.Eval(pxlr.GetPixel(p.X, p.Y));
                        else col = null;
                        if (prevCol != null && col != null)
                        {
                            gr2.DrawLine(penR, w - 1, h - prevCol.Value.Item1 * h, w, h - col.Value.Item1 * h);
                            gr2.DrawLine(penG, w - 1, h - prevCol.Value.Item2 * h, w, h - col.Value.Item2 * h);
                            gr2.DrawLine(penB, w - 1, h - prevCol.Value.Item3 * h, w, h - col.Value.Item3 * h);
                        }
                        prevCol = col;
                    }
                }
                pictureBox2.Invalidate();
            }
        }

        abstract class AMethod
        {
            public abstract Tripl<float> Eval(RawColor col);
        }
        class RGBMethod : AMethod
        {
            readonly float k = 1.0f / 255;

            public override Tripl<float> Eval(RawColor col)
            {
                return new Tripl<float>(col.R * k, col.G * k, col.B * k);
            }
        }
        class HCBMethod : AMethod
        {
            readonly float hk = 1f / 360;
            public override Tripl<float> Eval(RawColor col)
            {
                var ret = col.HCB();
                ret.Item1 *= hk;
                return ret;
            }
        }

        public enum AnalyseMethod
        {
            RGB,
            HCB
        }
    }
}
