using System;
using System.Windows.Forms;
using System.Drawing;

namespace KKLib
{
    public class ColorPicker : Control
    {
        Color chosen;
        TabControl tc;
        PickerTab[] pts;
        int selectedTab;
        PictureBox pboxPrev;
        Bitmap PrevTransp;
        Graphics grPrev;
        SolidBrush brPrev;
        TrackBar tbA;
        NumericUpDown nur, nug, nub, nua;
        EventArgs ea = new EventArgs();
        bool blockUpdating = false;

        public Color Chosen
        {
            get { return chosen; }
            set
            {
                chosen = value;
                if (!blockUpdating)
                {
                    blockUpdating = true;

                    pts[selectedTab].ColorChanged(chosen);
                    updateNums();
                    updateTbA();
                    updateNumA();
                    updatePrev();

                    blockUpdating = false;
                }
            }
        }
        public event EventHandler Picked;
        
        public ColorPicker()
        {
            pts = new PickerTab[]
            {
                new SLTab(this),
                new HSTab(this),
                new HSNormTab(this),
            };

            tc = new TabControl();
            tc.Size = new Size(396, 327);
            foreach (var pt in pts)
            {
                tc.TabPages.Add(pt.TP);
            }
            tc.SelectedIndexChanged += Tc_SelectedIndexChanged;

            tbA = new TrackBar();
            tbA.Size = new Size(388, 45);
            tbA.Location = new Point(4, 327);
            tbA.Minimum = 0;
            tbA.Maximum = 255;
            tbA.Value = 255;
            tbA.TickFrequency = 16;
            tbA.Scroll += TbA_Scroll;

            pboxPrev = new PictureBox();
            var bmpPrev = new Bitmap(40, 40);
            PrevTransp = Painting.ChessBoard(bmpPrev.Width, bmpPrev.Height, 10, Color.FromArgb(192, 192, 192), Color.FromArgb(64, 64, 64));
            brPrev = new SolidBrush(Color.Black);
            grPrev = Graphics.FromImage(bmpPrev);
            pboxPrev.Size = bmpPrev.Size;
            pboxPrev.Location = new Point(396, 238);
            pboxPrev.Image = bmpPrev;

            var lr = new Label();
            lr.Text = "R";
            lr.Location = new Point(410, 25);
            var lg = new Label();
            lg.Text = "G";
            lg.Location = new Point(410, 75);
            var lb = new Label();
            lb.Text = "B";
            lb.Location = new Point(410, 125);
            var la = new Label();
            la.Text = "A";
            la.Location = new Point(410, 309);

            nur = new NumericUpDown();
            nur.Size = new Size(40, 20);
            nur.Location = new Point(396, 48);
            nur.Maximum = 255;
            nur.ValueChanged += Nur_ValueChanged;
            nug = new NumericUpDown();
            nug.Size = new Size(40, 20);
            nug.Location = new Point(396, 98);
            nug.Maximum = 255;
            nug.ValueChanged += Nug_ValueChanged;
            nub = new NumericUpDown();
            nub.Size = new Size(40, 20);
            nub.Location = new Point(396, 148);
            nub.Maximum = 255;
            nub.ValueChanged += Nub_ValueChanged;
            nua = new NumericUpDown();
            nua.Size = new Size(40, 20);
            nua.Location = new Point(396, 332);
            nua.Maximum = 255;
            nua.ValueChanged += Nua_ValueChanged;

            Controls.Add(tc);
            Controls.Add(lr);
            Controls.Add(lg);
            Controls.Add(lb);
            Controls.Add(la);
            Controls.Add(nur);
            Controls.Add(nug);
            Controls.Add(nub);
            Controls.Add(nua);
            Controls.Add(tbA);
            Controls.Add(pboxPrev);

            Size = new Size(436, 372);
            MinimumSize = Size;
            MaximumSize = Size;
        }

        private void Nur_ValueChanged(object sender, EventArgs e)
        {
            numsUpdated();
        }
        private void Nug_ValueChanged(object sender, EventArgs e)
        {
            numsUpdated();
        }
        private void Nub_ValueChanged(object sender, EventArgs e)
        {
            numsUpdated();
        }
        private void Nua_ValueChanged(object sender, EventArgs e)
        {
            numAUpdated();
        }

        private void Tc_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedTab = tc.SelectedIndex;
            pts[selectedTab].ColorChanged(chosen);
        }

        void TbA_Scroll(object sender, EventArgs e)
        {
            tbAUpdated();

        }
        
        void pickerUpdated(Color col)
        {
            if (!blockUpdating)
            {
                blockUpdating = true;

                chosen = Color.FromArgb((byte)nua.Value, col);
                updateNums();
                updatePrev();

                Picked?.Invoke(this, ea);

                blockUpdating = false;
            }
        }
        void numsUpdated()
        {
            if (!blockUpdating)
            {
                blockUpdating = true;

                chosen = Color.FromArgb((byte)nua.Value, (byte)nur.Value, (byte)nug.Value, (byte)nub.Value);
                updatePicker();
                updatePrev();

                Picked?.Invoke(this, ea);

                blockUpdating = false;
            }
        }
        void tbAUpdated()
        {
            if (!blockUpdating)
            {
                blockUpdating = true;

                chosen = Color.FromArgb((byte)tbA.Value, (byte)nur.Value, (byte)nug.Value, (byte)nub.Value);
                updateNumA();
                updatePrev();

                Picked?.Invoke(this, ea);

                blockUpdating = false;
            }
        }
        void numAUpdated()
        {
            if (!blockUpdating)
            {
                blockUpdating = true;

                chosen = Color.FromArgb((byte)nua.Value, (byte)nur.Value, (byte)nug.Value, (byte)nub.Value);
                updateTbA();
                updatePrev();

                Picked?.Invoke(this, ea);

                blockUpdating = false;
            }
        }
        void updatePicker()
        {
            pts[selectedTab].ColorChanged(chosen);
        }
        void updateNums()
        {
            nur.Value = chosen.R;
            nug.Value = chosen.G;
            nub.Value = chosen.B;
        }
        void updateNumA()
        {
            nua.Value = chosen.A;
        }
        void updateTbA()
        {
            tbA.Value = chosen.A;
        }
        void updatePrev()
        {
            grPrev.DrawImageUnscaled(PrevTransp, new Point());
            brPrev.Color = chosen;
            grPrev.FillRectangle(brPrev, 0, 0, PrevTransp.Width, PrevTransp.Height);
            pboxPrev.Invalidate();
        }

        abstract class PickerTab
        {
            protected ColorPicker Parent;
            protected TabPage tp;
            protected PictureBox pbox;
            protected Bitmap back;
            protected Bitmap mark;
            protected int markHalf;
            protected Graphics gr;
            protected TrackBar tb;
            protected int pboxWidth;
            protected int pboxHeight;
            protected float pboxHeightF;
            protected Point Loc;
            protected EventArgs ea = new EventArgs();
            public virtual TabPage TP
            {
                get
                {
                    return tp;
                }
            }

            public PickerTab(ColorPicker parent)
            {
                Parent = parent;

                int len = 6;
                mark = new Bitmap(len * 2 + 5, len * 2 + 5);
                using (var grm = Graphics.FromImage(mark))
                {
                    float half = mark.Width * 0.5f;
                    grm.Clear(Color.Transparent);
                    using (var pen = new Pen(Color.Red, 3))
                    {
                        grm.DrawLine(pen, 0, half, len, half);
                        grm.DrawLine(pen, mark.Width, half, mark.Width - len, half);
                        grm.DrawLine(pen, half, 0, half, len);
                        grm.DrawLine(pen, half, mark.Height, half, mark.Height - len);
                    }
                    markHalf = (int)half;
                }

            }

            public abstract void ColorChanged(Color col);
        }
        class HSTab : PickerTab
        {
            public HSTab(ColorPicker parent) : base(parent)
            {
                string tnam;
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "pl") tnam = "Barwa i Nasycenie";
                else tnam = "Hue & Saturation";
                tp = new TabPage(tnam);
                back = new Bitmap(360, 256);
                using (var pxlr = new Pixeler(back))
                {
                    float div = back.Height;
                    for (int q = 0; q < pxlr.Height; q++)
                    {
                        for (int w = 0; w < pxlr.Width; w++)
                        {
                            pxlr.SetPixel(w, q, new RawColor(w, q / div, 0.5f));
                        }
                    }
                }

                pbox = new PictureBox();
                pbox.Size = new Size(360, 256);
                pbox.Location = new Point(14, 0);
                pboxWidth = back.Width;
                pboxHeight = back.Height;
                pboxHeightF = back.Height;
                var bmp = new Bitmap(back.Width, back.Height);
                gr = Graphics.FromImage(bmp);
                pbox.Image = bmp;
                gr.DrawImageUnscaled(back, 0, 0);
                pbox.MouseDown += Pbox_MouseDown;
                pbox.MouseUp += Pbox_MouseUp;
                pbox.MouseMove += Pbox_MouseMove;

                tb = new TrackBar();
                tb.Size = new Size(388, 45);
                tb.Location = new Point(0, 256);
                tb.Minimum = 0;
                tb.Maximum = 359;
                tb.Value = 179;
                tb.TickFrequency = 36;
                tb.Scroll += Tb_Scroll;

                tp.Controls.Add(pbox);
                tp.Controls.Add(tb);
            }

            public override void ColorChanged(Color col)
            {
                updatePbox(col);
                updateTb(col);
            }

            bool mDown = false;
            void Pbox_MouseDown(object sender, MouseEventArgs e)
            {
                mDown = true;
                Pbox_MouseMove(sender, e);
            }
            void Pbox_MouseUp(object sender, MouseEventArgs e)
            {
                mDown = false;
            }
            void Pbox_MouseMove(object sender, MouseEventArgs e)
            {
                if (mDown)
                {
                    Loc = new Point(Math.Min(Math.Max(e.X, 0), pboxWidth), Math.Min(Math.Max(e.Y, 0), pboxHeight));
                    redrawPbox();
                    Parent.pickerUpdated(new RawColor(Loc.X, Loc.Y / pboxHeightF, (float)tb.Value / tb.Maximum).ColorV);
                }
            }
            void Tb_Scroll(object sender, EventArgs e)
            {
                Parent.pickerUpdated(new RawColor(Loc.X, Loc.Y / pboxHeightF, (float)tb.Value / tb.Maximum).ColorV);
            }

            void updatePbox(Color col)
            {
                var hsl = new RawColor(col).HSL();
                Loc = new Point((int)hsl[0], (int)(hsl[1] * pboxHeightF));
                redrawPbox();
            }
            void updateTb(Color col)
            {
                tb.Value = (int)(new RawColor(col).HSL()[2] * tb.Maximum);
            }
            void redrawPbox()
            {
                gr.DrawImageUnscaled(back, 0, 0);
                gr.DrawImageUnscaled(mark, Loc.X - markHalf, Loc.Y - markHalf);
                pbox.Invalidate();
            }

        }
        class HSNormTab : PickerTab
        {
            public HSNormTab(ColorPicker parent) : base(parent)
            {
                string tnam;
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "pl") tnam = "Znormalizowane";
                else tnam = "Normalized";
                tp = new TabPage(tnam);
                back = new Bitmap(360, 256);
                using (var pxlr = new Pixeler(back))
                {
                    float div = back.Height;
                    for (int q = 0; q < pxlr.Height; q++)
                    {
                        var qdiv = q / div;
                        for (int w = 0; w < pxlr.Width; w++)
                        {
                            pxlr.SetPixel(w, q, RawColor.FromHSB(w, qdiv, 0.5f));
                        }
                    }
                }

                pbox = new PictureBox();
                pbox.Size = new Size(360, 256);
                pbox.Location = new Point(14, 0);
                pboxWidth = back.Width;
                pboxHeight = back.Height;
                pboxHeightF = back.Height;
                var bmp = new Bitmap(back.Width, back.Height);
                gr = Graphics.FromImage(bmp);
                pbox.Image = bmp;
                gr.DrawImageUnscaled(back, 0, 0);
                pbox.MouseDown += Pbox_MouseDown;
                pbox.MouseUp += Pbox_MouseUp;
                pbox.MouseMove += Pbox_MouseMove;

                tb = new TrackBar();
                tb.Size = new Size(388, 45);
                tb.Location = new Point(0, 256);
                tb.Minimum = 0;
                tb.Maximum = 359;
                tb.Value = 179;
                tb.TickFrequency = 36;
                tb.Scroll += Tb_Scroll;

                tp.Controls.Add(pbox);
                tp.Controls.Add(tb);
            }

            public override void ColorChanged(Color col)
            {
                updatePbox(col);
                updateTb(col);
            }

            bool mDown = false;
            void Pbox_MouseDown(object sender, MouseEventArgs e)
            {
                mDown = true;
                Pbox_MouseMove(sender, e);
            }
            void Pbox_MouseUp(object sender, MouseEventArgs e)
            {
                mDown = false;
            }
            void Pbox_MouseMove(object sender, MouseEventArgs e)
            {
                if (mDown)
                {
                    Loc = new Point(Math.Min(Math.Max(e.X, 0), pboxWidth), Math.Min(Math.Max(e.Y, 0), pboxHeight));
                    redrawPbox();
                    Parent.pickerUpdated(RawColor.FromHSB(Loc.X, Loc.Y / pboxHeightF, (float)tb.Value / tb.Maximum).ColorV);
                }
            }
            void Tb_Scroll(object sender, EventArgs e)
            {
                Parent.pickerUpdated(RawColor.FromHSB(Loc.X, Loc.Y / pboxHeightF, (float)tb.Value / tb.Maximum).ColorV);
            }

            void updatePbox(Color col)
            {
                var hsl = new RawColor(col).HSB();
                Loc = new Point((int)hsl.Item1, (int)(hsl.Item2 * pboxHeightF));
                redrawPbox();
            }
            void updateTb(Color col)
            {
                tb.Value = (int)(new RawColor(col).HSB().Item3 * tb.Maximum);
            }
            void redrawPbox()
            {
                gr.DrawImageUnscaled(back, 0, 0);
                gr.DrawImageUnscaled(mark, Loc.X - markHalf, Loc.Y - markHalf);
                pbox.Invalidate();
            }

        }
        class SLTab : PickerTab
        {
            float[] sats;

            public SLTab(ColorPicker parent) : base(parent)
            {
                string tnam;
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "pl") tnam = "Nasycenie i Jasność";
                else tnam = "Saturation & Lightness";
                tp = new TabPage(tnam);
                back = new Bitmap(360, 256);

                pbox = new PictureBox();
                pbox.Size = new Size(360, 256);
                pbox.Location = new Point(14, 0);
                pboxWidth = back.Width;
                pboxHeight = back.Height;
                pboxHeightF = back.Height;
                var bmp = new Bitmap(back.Width, back.Height);
                gr = Graphics.FromImage(bmp);
                pbox.Image = bmp;
                gr.DrawImageUnscaled(back, 0, 0);
                pbox.MouseDown += Pbox_MouseDown;
                pbox.MouseUp += Pbox_MouseUp;
                pbox.MouseMove += Pbox_MouseMove;

                tb = new TrackBar();
                tb.Size = new Size(388, 45);
                tb.Location = new Point(0, 256);
                tb.Minimum = 0;
                tb.Maximum = 359;
                tb.Value = 0;
                tb.TickFrequency = 60;
                tb.Scroll += Tb_Scroll;

                tp.Controls.Add(pbox);
                tp.Controls.Add(tb);

                sats = new float[360];
                for (int q = 0; q < sats.Length; q++)
                {
                    sats[q] = (float)q / (sats.Length - 1);
                }
            }

            public override void ColorChanged(Color col)
            {
                updateTb(col);
                updatePbox(col);
            }

            bool mDown = false;
            void Pbox_MouseDown(object sender, MouseEventArgs e)
            {
                mDown = true;
                Pbox_MouseMove(sender, e);
            }
            void Pbox_MouseUp(object sender, MouseEventArgs e)
            {
                mDown = false;
            }
            void Pbox_MouseMove(object sender, MouseEventArgs e)
            {
                if (mDown)
                {
                    Loc = new Point(Math.Min(Math.Max(e.X, 0), pboxWidth), Math.Min(Math.Max(e.Y, 0), pboxHeight));
                    drawPbox();
                    Parent.pickerUpdated(new RawColor(tb.Value, (float)Loc.X / pboxWidth, Loc.Y / pboxHeightF).ColorV);
                }
            }
            void Tb_Scroll(object sender, EventArgs e)
            {
                redrawPbox();

                Parent.pickerUpdated(new RawColor(tb.Value, (float)Loc.X / pboxWidth, Loc.Y / pboxHeightF).ColorV);
            }

            void updatePbox(Color col)
            {
                var hsl = new RawColor(col).HSL();
                Loc = new Point((int)(hsl[1] * pboxWidth), (int)(hsl[2] * pboxHeightF));
                redrawPbox();
            }
            void updateTb(Color col)
            {
                tb.Value = (int)new RawColor(col).HSL()[0];
            }
            void redrawPbox()
            {
                float h = tb.Value;
                using (var pxlr = new Pixeler(back))
                {
                    float div = back.Height;
                    for (int q = 0; q < pxlr.Height; q++)
                    {
                        float l = (float)q / (pxlr.Height - 1);
                        for (int w = 0; w < pxlr.Width; w++)
                        {
                            pxlr.SetPixel(w, q, new RawColor(h, sats[w], l));
                        }
                    }
                }
                drawPbox();
            }
            void drawPbox()
            {
                gr.DrawImageUnscaled(back, 0, 0);
                gr.DrawImageUnscaled(mark, Loc.X - markHalf, Loc.Y - markHalf);
                pbox.Invalidate();
            }
        }
        class SBTab : PickerTab
        {
            float[] sats;

            public SBTab(ColorPicker parent) : base(parent)
            {
                string tnam;
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "pl") tnam = "Nasycenie i Jasność";
                else tnam = "Saturation & Lightness";
                tp = new TabPage(tnam);
                back = new Bitmap(360, 256);

                pbox = new PictureBox();
                pbox.Size = new Size(360, 256);
                pbox.Location = new Point(14, 0);
                pboxWidth = back.Width;
                pboxHeight = back.Height;
                pboxHeightF = back.Height;
                var bmp = new Bitmap(back.Width, back.Height);
                gr = Graphics.FromImage(bmp);
                pbox.Image = bmp;
                gr.DrawImageUnscaled(back, 0, 0);
                pbox.MouseDown += Pbox_MouseDown;
                pbox.MouseUp += Pbox_MouseUp;
                pbox.MouseMove += Pbox_MouseMove;

                tb = new TrackBar();
                tb.Size = new Size(388, 45);
                tb.Location = new Point(0, 256);
                tb.Minimum = 0;
                tb.Maximum = 359;
                tb.Value = 0;
                tb.TickFrequency = 60;
                tb.Scroll += Tb_Scroll;

                tp.Controls.Add(pbox);
                tp.Controls.Add(tb);

                sats = new float[360];
                for (int q = 0; q < sats.Length; q++)
                {
                    sats[q] = (float)q / (sats.Length - 1);
                }
            }

            public override void ColorChanged(Color col)
            {
                updateTb(col);
                updatePbox(col);
            }

            bool mDown = false;
            void Pbox_MouseDown(object sender, MouseEventArgs e)
            {
                mDown = true;
                Pbox_MouseMove(sender, e);
            }
            void Pbox_MouseUp(object sender, MouseEventArgs e)
            {
                mDown = false;
            }
            void Pbox_MouseMove(object sender, MouseEventArgs e)
            {
                if (mDown)
                {
                    Loc = new Point(Math.Min(Math.Max(e.X, 0), pboxWidth), Math.Min(Math.Max(e.Y, 0), pboxHeight));
                    drawPbox();
                    Parent.pickerUpdated(RawColor.FromHSB(tb.Value, (float)Loc.X / pboxWidth, Loc.Y / pboxHeightF).ColorV);
                }
            }
            void Tb_Scroll(object sender, EventArgs e)
            {
                redrawPbox();

                Parent.pickerUpdated(new RawColor(tb.Value, (float)Loc.X / pboxWidth, Loc.Y / pboxHeightF).ColorV);
            }

            void updatePbox(Color col)
            {
                var hsl = new RawColor(col).HSL();
                Loc = new Point((int)(hsl[1] * pboxWidth), (int)(hsl[2] * pboxHeightF));
                redrawPbox();
            }
            void updateTb(Color col)
            {
                tb.Value = (int)new RawColor(col).HSL()[0];
            }
            void redrawPbox()
            {
                float h = tb.Value;
                using (var pxlr = new Pixeler(back))
                {
                    float div = back.Height;
                    for (int q = 0; q < pxlr.Height; q++)
                    {
                        float l = (float)q / (pxlr.Height - 1);
                        for (int w = 0; w < pxlr.Width; w++)
                        {
                            pxlr.SetPixel(w, q, RawColor.FromHSB(h, sats[w], l));
                        }
                    }
                }
                drawPbox();
            }
            void drawPbox()
            {
                gr.DrawImageUnscaled(back, 0, 0);
                gr.DrawImageUnscaled(mark, Loc.X - markHalf, Loc.Y - markHalf);
                pbox.Invalidate();
            }
        }
    }
    public class ColorPickerDialog : Form
    {
        public RawColor val;
        ColorPicker cp;

        public RawColor Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                cp.Chosen = val.ColorV;
            }
        }

        public ColorPickerDialog()
        {
            int btnWidth = 86;
            int btnHeight = 24;
            int btnMargin = 20;
            cp = new ColorPicker();

            ClientSize = new Size(cp.Width, cp.Height + btnHeight + 2 * btnMargin);
            ShowIcon = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            MinimumSize = Size;
            MaximumSize = Size;
            SizeGripStyle = SizeGripStyle.Hide;

            Controls.Add(cp);

            var btnok = new Button();
            btnok.Width = btnWidth;
            btnok.Height = btnHeight;
            btnok.Text = "OK";
            btnok.Location = new Point(Width / 2 - btnWidth - btnWidth / 2, cp.Height + btnMargin);
            btnok.Click += Btnok_Click;
            btnok.DialogResult = DialogResult.OK;
            Controls.Add(btnok);

            var btncn = new Button();
            btncn.Width = btnWidth;
            btncn.Height = btnHeight;
            string lab;
            if (System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "pl") lab = "Anuluj";
            else lab = "Cancel";
            btncn.Text = lab;
            btncn.Location = new Point(Width / 2 + btnWidth / 2, cp.Height + btnMargin);
            btncn.Click += Btncn_Click;
            btncn.DialogResult = DialogResult.Cancel;
            Controls.Add(btncn);
        }

        private void Btnok_Click(object sender, EventArgs e)
        {
            val = new RawColor(cp.Chosen);
            Close();
        }
        private void Btncn_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
