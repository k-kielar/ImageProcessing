using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Numerics;
using KKLib;

namespace ImageProcessing
{
    [Serializable]
    abstract class IBox
    {
        static Pen penBoxFrame = new Pen(Color.FromArgb(80, 80, 255));
        static Pen penPin = new Pen(Color.Brown);
        static Color colBar = Color.LightBlue;
        protected static SolidBrush brClear = new SolidBrush(Color.FromArgb(240, 240, 255));
        static Brush brBar = new SolidBrush(colBar);
        static Brush brPinVal = new SolidBrush(Color.LightGray);
        static Brush brPinCol = new SolidBrush(Color.Orange);
        static Brush brFont = new SolidBrush(Color.FromArgb(36, 83, 140));
        static Brush brPinLab = new SolidBrush(Color.FromArgb(36, 83, 140));
        static Brush brClose = new SolidBrush(Color.FromArgb(234, 28, 35));
        static Pen penClose = new Pen(Color.FromArgb(255, 255, 255), 2);
        protected static Font font = new Font("Arial", 8);
        static Point pBarText = new Point(2, 3);

        public const int PinClickRadius = 8;
        public const int PinClickRadiusSq = PinClickRadius * PinClickRadius;
        public const int BarHeight = 20;
        public const int ResizeBoxSize = 5;
        public const int CloseBoxSize = 10;
        public const int PinsD = 20;
        public const int PinsMargin = 5;
        public const int PinR = 4;
        public const int PinR2 = PinR * 2;
        public const int InteriorMargin = 10;
        protected static Size TLCorner = new Size(InteriorMargin, BarHeight);

        public Rectangle Rect;
        public Pin[][] pins;
        [NonSerialized]
        Bitmap bar;

        protected IBox() { }
        public IBox(Point start, IProcessor proc)
        {
            Rect = new Rectangle(start, new Size(100, RegularHeight(proc.Inputs.Length)));
            SetPinsFromProc(proc);
            PrepareBar();
        }

        protected void SetPinsFromProc(IProcessor proc, bool setOutput = true)
        {
            var ins = new Pin[proc.Inputs.Length];
            int y = BarHeight + PinsD / 2;
            var labs = proc.Labels;
            for (int q = 0; q < ins.Length; q++)
            {
                string lab = null;
                if (labs != null) lab = labs[0][q];
                ins[q] = new Pin(new Point(PinsMargin, y), this, q, proc, lab);
                y += PinsD;
            }
            Pin[] outs;
            if (setOutput)
            {
                string lab = null;
                if (labs != null && labs.Length > 1 && labs[1] != null && labs[1].Length > 0) lab = labs[1][0];
                outs = new Pin[] { new Pin(new Point(Rect.Width - PinsMargin, BarHeight + PinsD / 2), this, -1, proc, lab) };
            }
            else outs = new Pin[0];
            pins = new Pin[][] { ins, outs };
        }
        protected void SetPinsFromProcs(string[][] labels, IProcessor inp, params IProcessor[] outs)
        {
            int x = Rect.Width - PinsMargin;
            var pout = new Pin[outs.Length];
            for (int q = 0; q < pout.Length; q++)
            {
                string lab = null;
                if (labels != null && labels.Length > 1 && labels[1] != null && q < labels[1].Length) lab = labels[1][q];
                pout[q] = new Pin(new Point(x, PinY(q)), this, -1 - q, outs[q], lab);
            }
            Pin[] pinps;
            if (inp == null)
            {
                pinps = new Pin[0];
            }
            else
            {
                string lab = null;
                if (labels != null && labels.Length > 0 && labels[0] != null && labels[0].Length > 0) lab = labels[0][0];
                pinps = new Pin[] { new Pin(new Point(PinsMargin, PinY(0)), this, 0, inp) };
            }
            pins = new Pin[][] { pinps, pout };

        }
        protected void PrepareBar(string text = null)
        {
            bar = new Bitmap(Rect.Width, BarHeight);
            if (text == null) text = ToString();
            var l = Res.Mngr.GetString(text);
            if (l != null) text = l;
            using (var gr = Graphics.FromImage(bar))
            {
                gr.Clear(colBar);
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                gr.DrawLine(penBoxFrame, 0, 0, 0, BarHeight);
                gr.DrawLine(penBoxFrame, bar.Width, 0, bar.Width, BarHeight);
                gr.DrawLine(penBoxFrame, 0, 0, bar.Width, 0);
                gr.DrawString(text, font, brFont, pBarText);

                var rectClose = new Rectangle(Rect.Width - CloseBoxSize, 0, CloseBoxSize, CloseBoxSize);
                gr.FillRectangle(brClose, rectClose);
                var dx = 2;
                gr.DrawLine(penClose, rectClose.Left + dx, rectClose.Top + dx, rectClose.Right - dx, rectClose.Bottom - dx);
                gr.DrawLine(penClose, rectClose.Left + dx, rectClose.Bottom - dx, rectClose.Right - dx, rectClose.Top + dx);
            }
        }
        static protected int RegularHeight(int pinRows)
        {
            return BarHeight + PinsD * (Math.Max(1, pinRows));
        }
        static protected int PinY(int pos)
        {
            return BarHeight + pos * PinsD + PinsD / 2;
        }

        public Pin GetPin(Point pos)
        {
            foreach (var pinSet in pins)
            {
                foreach (var pin in pinSet)
                {
                    var p = pin.P;
                    int dx = p.X - pos.X;
                    int dy = p.Y - pos.Y;
                    if (dx * dx + dy * dy < PinClickRadiusSq)
                    {
                        return pin;
                    }
                }
            }
            return null;
        }
        public void ClearDraw(Graphics gr)
        {
            gr.FillRectangle(brClear, Rect);
            Draw(gr);
        }
        public void Draw(Graphics gr)
        {
            DrawInside(gr);
            gr.DrawRectangle(penBoxFrame, Rect);
            gr.DrawImage(bar, Rect.Location);
            for (int w = 0; w < pins.Length; w++)
            {
                var pinsset = pins[w];
                for (int q = 0; q < pinsset.Length; q++)
                {
                    var pin = pinsset[q];
                    var p = pin.P;
                    var rect = new Rectangle(Rect.X + p.X - PinR, Rect.Y + p.Y - PinR, PinR2, PinR2);
                    Brush brpin;
                    if (pin.Tp == IProcessor.TVal) brpin = brPinVal;
                    else brpin = brPinCol;
                    gr.FillEllipse(brpin, rect);
                    gr.DrawEllipse(penPin, rect);
                    if (pin.Label != null)
                    {
                        var l = Res.Mngr.GetString(pin.Label);
                        if (l == null) l = pin.Label;
                        if (w == 0)
                        {
                            gr.DrawString(l, font, brPinLab, new Point(rect.X + rect.Width + 2, rect.Y - 2));
                        }
                        else
                        {
                            var dim = gr.MeasureString(l, font);
                            gr.DrawString(l, font, brPinLab, new Point((int)(rect.X - dim.Width - 2), rect.Y - 2));
                        }
                    }
                }
            }
        }

        public virtual ActionResult MouseDown(Point pos, MouseButtons btn) { return ActionResult.None; }
        public virtual ActionResult MouseUp(Point pos) { return ActionResult.None; }
        public virtual void MouseMove(Point pos) { }
        public virtual void Close() { }
        protected virtual void DrawInside(Graphics gr) { }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var pinn in pins)
            {
                foreach (var pin in pinn)
                {
                    pin.Box = this;
                }
            }
            PrepareBar();
        }
    }
    abstract class BoxImg : IBox
    {
        protected static Bitmap CHESSBOARD;
        static BoxImg()
        {
            CHESSBOARD = Painting.ChessBoard(200, 200, 10, Color.FromArgb(192, 192, 192), Color.FromArgb(64, 64, 64));
        }

        protected Bitmap bmp;
        protected Bitmap preview;
        protected int drawWid, drawHei;
        protected Bitmap back;

        public BoxImg(Point start, Bitmap Bmp)
        {
            Rect = new Rectangle(start, new Size(200, 200));
            bmp = Bmp;

            PrepareBar();
            PrepareIcon(bmp.Size);
        }
        protected void PrepareIcon(Size siz)
        {
            var insideWid = Rect.Width - 1;
            var insideHei = Rect.Height - BarHeight;
            var xratio = (float)insideWid / siz.Width;
            var yratio = (float)insideHei / siz.Height;
            if (xratio > yratio)
            {
                drawWid = (int)Math.Ceiling(siz.Width * yratio);
                drawHei = insideHei;
            }
            else
            {
                drawWid = insideWid;
                drawHei = (int)Math.Ceiling(siz.Height * xratio);
            }
            preview = new Bitmap(drawWid, drawHei);
        }

        Point mclik;
        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            mclik = pos;
            return ActionResult.Internal;
        }
        public override ActionResult MouseUp(Point pos)
        {
            if (Mth.Dsq(pos, mclik) < 10)
            {
                Dispose();
                EasyImgBox.ShowEasy(GetBmp());
                Renew();
            }
            return ActionResult.None;
        }

        protected virtual void UpdatePreview() { }
        protected virtual void UpdateBlank() { }
        protected override void DrawInside(Graphics gr)
        {
            var srect = new Rectangle(Rect.X + 1, Rect.Y + BarHeight, preview.Width, preview.Height);
            if (back != null) gr.DrawImageUnscaledAndClipped(back, srect);
            gr.DrawImageUnscaled(preview, srect);
        }

        protected virtual void Dispose() { }
        protected virtual void Renew() { }
        protected abstract Bitmap GetBmp();

        class EasyImgBox : ImageBox
        {
            public static void ShowEasy(Bitmap bmp)
            {
                var ret = new EasyImgBox(bmp);

                ret.ShowDialog();
            }

            EasyImgBox(Bitmap bmp) : base(bmp)
            {
                picbox.Click += btnok_Click;
                Icon = Properties.Resources.icon16_32_64_256;
            }
        }
    }
    class BoxSource : BoxImg
    {
        ImgSource src;
        Graphics grPrev;

        public BoxSource(Point start, Bitmap Bmp) : base(start, Bmp)
        {
            back = CHESSBOARD;
            grPrev = Graphics.FromImage(preview);
            UpdatePreview();

            src = ImgSource.New(Bmp);
            var a = new ColorToA();
            a.Inputs[0] = src;
            var r = new ColorToR();
            r.Inputs[0] = src;
            var g = new ColorToG();
            g.Inputs[0] = src;
            var b = new ColorToB();
            b.Inputs[0] = src;

            SetPinsFromProcs(new string[][] { null, new string[] { null, "A", "R", "G", "B", } }, null, src, a, r, g, b);

            PrepareBar("Source");
        }

        protected override void UpdatePreview()
        {
            grPrev.DrawImageUnscaledAndClipped(back, new Rectangle(0, 0, drawWid, drawHei));
            grPrev.DrawImage(bmp, 0, 0, drawWid, drawHei);
        }
        protected override void UpdateBlank()
        {
            grPrev.Clear(Color.White);
        }
        protected override void Dispose()
        {
            src.Dispose();
        }
        protected override void Renew()
        {
            src.Renew();
        }
        protected override Bitmap GetBmp()
        {
            return src.bmp;
        }
        public override void Close()
        {
            src.Close();
        }
    }
    class BoxEndPoint : BoxImg
    {
#if SEQUENTIAL
        public const int THREADN = 1;
#else
        public const int THREADN = 8;
#endif

        static readonly Vector<float> ai = new Vector<float>(InternalColor.I * 0x1000000);
        static readonly Vector<float> ak = new Vector<float>(255f * 0x1000000 - InternalColor.I * 0x1000000);
        static readonly Vector<float> ri = new Vector<float>(InternalColor.I * 0x10000);
        static readonly Vector<float> rk = new Vector<float>(255f * 0x10000 - InternalColor.I * 0x10000);
        static readonly Vector<float> gi = new Vector<float>(InternalColor.I * 0x100);
        static readonly Vector<float> gk = new Vector<float>(255f * 0x100 - InternalColor.I * 0x100);
        static readonly Vector<float> bi = new Vector<float>(InternalColor.I);
        static readonly Vector<float> bk = new Vector<float>(255f - InternalColor.I);

        protected bool justChanged = true;

        public BoxEndPoint(Point start, Bitmap Bmp) : base(start, Bmp) { }

        public void Update()
        {
#if SEQUENTIAL
            UpdateSequential();
#else
            UpdateParallel();
#endif
        }
        void UpdateParallel()
        {
            if (Ready())
            {
                var proc = pins[0][0].Proc.Inputs[0];
                PrepareUpdating();
                using (var pxlr = new PixelerVectoric(bmp))
                {
                    if (pxlr.Width < VectorColor.Count * 2)
                    {
                        UpdateSimple(pxlr, proc);
                    }
                    else
                    {
                        //var timer = new System.Diagnostics.Stopwatch();
                        //timer.Start();

                        int dq = pxlr.Height / THREADN;
                        var tasks = new Task[THREADN];
                        var tlim = THREADN - 1;
                        for (int l = 0; l < tlim; l++)
                        {
                            var q0 = l * dq;
                            var qlim = q0 + dq;
                            var th = l + 0;
                            var t = new Task(() => Updater.UpdateRange(pxlr, proc, q0, qlim, th));
                            t.Start();
                            tasks[l] = t;
                        }
                        var tt = new Task(() => Updater.UpdateRange(pxlr, proc, tlim * dq, pxlr.Height, tlim));
                        tt.Start();
                        tasks[tlim] = tt;
                        Task.WaitAll(tasks);

                        //timer.Stop();
                        //MessageBox.Show(timer.Elapsed.ToString());
                    }
                }
            }
            else
            {
                UpdateBlank();
            }
        }
        void UpdateSequential()
        {
            if (Ready())
            {
                var proc = pins[0][0].Proc.Inputs[0];
                PrepareUpdating();
                using (var pxlr = new PixelerVectoric(bmp))
                {
                    Updater.UpdateRange(pxlr, proc, 0, pxlr.Height, 0);
                }
            }
            else
            {
                UpdateBlank();
            }
        }
        void UpdateSimple(Pixeler pxlr, IProcessor proc)
        {
            for (int q = 0; q < pxlr.Height; q++)
            {
                for (int w = 0; w < pxlr.Width; w++)
                {
                    pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                }
            }
        }

        protected override Bitmap GetBmp()
        {
            if (justChanged)
            {
                Update();
                justChanged = false;
            }
            return bmp;
        }

        protected bool Ready()
        {
            var inps = pins[0];
            for (int q = 0; q < inps.Length; q++)
            {
                var ii = inps[q].Proc.Inputs;
                for (int w = 0; w < ii.Length; w++)
                {
                    var i = ii[w];
                    if (i == null || !i.CanEvaluate(new Branch<IProcessor>(null, null))) return false;
                }
            }
            return true;
        }
        void PrepareUpdating()
        {
            var s = pins[0][0].Proc.GetSize();
            if (s.Width == int.MaxValue || s.Height == int.MaxValue) return;
            if (s != bmp.Size)
            {
                bmp.Dispose();
                bmp = new Bitmap(s.Width, s.Height);
            }
        }

        void UpdateRange(Pixeler pxlr, IProcessor proc, int q0, int qlim)
        {
            for (int q = q0; q < qlim; q++)
            {
                for (int w = 0; w < pxlr.Width; w++)
                {
                    pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                }
            }
        }

        static UpdaterV Updater;
        static BoxEndPoint()
        {
            switch (VectorColor.Count)
            {
                case 2: Updater = new UpdaterV2(); break;
                case 4: Updater = new UpdaterV4(); break;
                case 8: Updater = new UpdaterV8(); break;
                case 16: Updater = new UpdaterV16(); break;
                case 32: Updater = new UpdaterV32(); break;
                default: Updater = new UpdaterV1(); break;
            }
        }
        abstract class UpdaterV
        {
            public abstract void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th);
        }
        class UpdaterV1 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w++)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V1();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                }
            }
        }
        class UpdaterV2 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width / VectorColor.Count * VectorColor.Count;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w += VectorColor.Count)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V2();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                    for (; w < pxlr.Width; w++)
                    {
                        pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                    }
                }
            }
        }
        class UpdaterV4 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width / VectorColor.Count * VectorColor.Count;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w += VectorColor.Count)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V4();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                    for (; w < pxlr.Width; w++)
                    {
                        pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                    }
                }
            }
        }
        class UpdaterV8 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width / VectorColor.Count * VectorColor.Count;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w += VectorColor.Count)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V8();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                    for (; w < pxlr.Width; w++)
                    {
                        pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                    }
                }
            }
        }
        class UpdaterV16 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width / VectorColor.Count * VectorColor.Count;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w += VectorColor.Count)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V16();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                    for (; w < pxlr.Width; w++)
                    {
                        pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                    }
                }
            }
        }
        class UpdaterV32 : UpdaterV
        {
            public override void UpdateRange(PixelerVectoric pxlr, IProcessor proc, int q0, int qlim, int th)
            {
                int wlim = pxlr.Width / VectorColor.Count * VectorColor.Count;
                for (int q = q0; q < qlim; q++)
                {
                    int w = 0;
                    for (; w < wlim; w += VectorColor.Count)
                    {
                        var vs = proc.EvaluateCv(w, q, th);
                        var va = Vector.ConvertToUInt32(vs.As * ak + ai) & MthVector.MASK_ALPHA;
                        var vr = Vector.ConvertToUInt32(vs.Rs * rk + ri) & MthVector.MASK_RED;
                        var vg = Vector.ConvertToUInt32(vs.Gs * gk + gi) & MthVector.MASK_GREEN;
                        var vb = Vector.ConvertToUInt32(vs.Bs * bk + bi) & MthVector.MASK_BLUE;
                        var vstr = new V32();
                        vstr.V = va | vr | vg | vb;
                        pxlr.SetVector(w, q, vstr.Fs);
                    }
                    for (; w < pxlr.Width; w++)
                    {
                        pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w, q)));
                    }
                }
            }
        }
    }
    class BoxResult : BoxEndPoint
    {
        static SaveFileDialog sfd;

        static BoxResult()
        {
            sfd = new SaveFileDialog();
            sfd.Filter = "Png|*.png|Jpg|*.jpg|Bmp|*.bmp|Gif|*.gif";
        }

        Bitmap blankPrev;
        Bitmap totalPrev;
        Bitmap zoomPrev;
        float wk, qk;
        bool total = true;
        PointF zoomP;
        int IconDim = 20;
        int btnDwnIndx = int.MaxValue;
        Size size;
        Size zoomMaxSize;

        public BoxResult(Point start, Bitmap Bmp) : base(start, Bmp)
        {
            blankPrev = new Bitmap(preview.Width, preview.Height);
            totalPrev = new Bitmap(drawWid, drawHei);
            zoomPrev = new Bitmap(Rect.Width, Rect.Height - BarHeight);
            UpdatePreview();

            wk = (bmp.Width - 1) / (float)totalPrev.Width;
            qk = (bmp.Height - 1) / (float)totalPrev.Height;
            using (var grpr = Graphics.FromImage(blankPrev))
            {
                grpr.Clear(Color.White);
            }

            SetPinsFromProc(new ImgResult(), false);

            back = CHESSBOARD;
            PrepareBar("Result");

            UpdateBlank();

            zoomMaxSize = new Size(Rect.Width - 1, Rect.Height - BarHeight);
        }

        public virtual void Preview()
        {
            if (Ready())
            {
                PreparePreview();
                Bitmap bmpop;
                float kw, kq;
                int w0, q0;
                if (total)
                {
                    kw = wk;
                    kq = qk;
                    w0 = 0;
                    q0 = 0;
                    bmpop = totalPrev;
                }
                else
                {
                    kw = 1;
                    kq = 1;
                    w0 = I0(size.Width, zoomPrev.Width, zoomP.X);
                    q0 = I0(size.Height, zoomPrev.Height, zoomP.Y);
                    bmpop = zoomPrev;
                }
                var proc = pins[0][0].Proc.Inputs[0];
                using (var pxlr = new Pixeler(bmpop))
                {
                    for (int q = 0; q < pxlr.Height; q++)
                    {
                        for (int w = 0; w < pxlr.Width; w++)
                        {
                            pxlr.SetPixel(w, q, proc.EvaluateC(new Point(w0 + (int)(w * kw), q0 + (int)(q * kq))));
                        }
                    }
                }
                UpdatePreview();
                justChanged = true;
            }
            else
            {
                UpdateBlank();
            }
        }

        public void Save(string path)
        {
            GetBmp();
            try
            {
                switch (Various.FileExtension(path))
                {
                    case ".jpg":
                        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        var encParams = new EncoderParameters(1);
                        var q = Math.Min(Math.Max((byte)1, Properties.Settings.Default.JPEGSaveQuality), (byte)100);
                        encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)q);
                        bmp.Save(path, codec, encParams);
                        break;
                    case ".bmp":
                        bmp.Save(path, ImageFormat.Bmp);
                        break;
                    case ".gif":
                        bmp.Save(path, ImageFormat.Gif);
                        break;
                    default:
                        bmp.Save(path);
                        break;
                }
            }
            catch (Exception)
            {

            }
        }
        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            if (btn == MouseButtons.Right)
            {
                zoomP = new PointF(Math.Min((float)pos.X / zoomPrev.Width, 1), Math.Min((float)pos.Y / zoomPrev.Height, 1));
                return ActionResult.Execute;
            }
            else
            {
                if (pos.Y > Rect.Height - IconDim)
                {
                    btnDwnIndx = pos.X / IconDim;
                    return ActionResult.Internal;
                }
                else
                {
                    btnDwnIndx = int.MaxValue;
                    return base.MouseDown(pos, btn);
                }
            }
        }
        public override ActionResult MouseUp(Point pos)
        {
            var ret = ActionResult.None;
            if (btnDwnIndx < 3 && pos.Y > Rect.Height - IconDim && pos.X / IconDim == btnDwnIndx)
            {
                ret = ActionResult.Internal;
                switch (btnDwnIndx)
                {
                    case 0:
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            Save(sfd.FileName);
                        }
                        break;
                    case 1:
                        ShowAnalyzer();
                        break;
                    case 2:
                        total = !total;
                        ret = ActionResult.Execute;
                        break;
                    default:
                        break;
                }
            }
            btnDwnIndx = int.MaxValue;
            if (ret == ActionResult.None) return base.MouseUp(pos);
            else return ret;
        }
        public override void Close()
        {
            bmp.Dispose();
            bmp = null;
        }

        protected override void UpdatePreview()
        {
            if (total)
            {
                preview = totalPrev;
            }
            else
            {
                preview = zoomPrev;
            }
        }
        protected override void UpdateBlank()
        {
            preview = blankPrev;
        }
        protected override void DrawInside(Graphics gr)
        {
            base.DrawInside(gr);
            gr.DrawImageUnscaled(Properties.Resources.save, Rect.X + 1, Rect.Y + Rect.Height - IconDim);
            gr.DrawImageUnscaled(Properties.Resources.stats, Rect.X + 1 + IconDim, Rect.Y + Rect.Height - IconDim);
            Bitmap bmp3;
            if (total) bmp3 = Properties.Resources.full;
            else bmp3 = Properties.Resources.zoom;
            gr.DrawImageUnscaled(bmp3, Rect.X + 1 + 2 * IconDim, Rect.Y + Rect.Height - IconDim);
        }
        void PreparePreview()
        {
            var s = pins[0][0].Proc.GetSize();
            if (s.Width == int.MaxValue || s.Height == int.MaxValue) return;
            if (s != size)
            {
                PrepareIcon(s);
                totalPrev = preview;
                wk = (s.Width - 1) / (float)totalPrev.Width;
                qk = (s.Height - 1) / (float)totalPrev.Height;
                size = s;

                var nZoomSize = new Size(Math.Min(s.Width, zoomMaxSize.Width), Math.Min(s.Height, zoomMaxSize.Height));
                if (zoomPrev.Size != nZoomSize)
                {
                    zoomPrev = new Bitmap(nZoomSize.Width, nZoomSize.Height);
                }
            }
        }
        int I0(int imgDim, int prevDim, float click01)
        {
            if (prevDim > imgDim) return 0;
            return (int)(click01 * (imgDim - prevDim));
        }

        void ShowAnalyzer()
        {
            GetBmp();
            var frm = new ImgAnalyzeForm();
            frm.SetBmp(bmp);
            frm.Show();
        }
    }
    [Serializable]
    class BoxRegular : IBox
    {
        public BoxRegular(Point start, IProcessor proc, int inputsN) : this(start, new Size(100, RegularHeight(inputsN)), proc) { }
        public BoxRegular(Point start, Size size, IProcessor proc)
        {
            Rect = new Rectangle(start, size);
            SetPinsFromProc(proc);
            PrepareBar();
        }
        public BoxRegular(Point start, IProcessor proc) : base(start, proc) { }

        public override string ToString()
        {
            var ret = pins[1][0].Proc.ToString();
            ret = ret.Substring(ret.LastIndexOf('.') + 1);
            return ret;
        }
    }
    [Serializable]
    class BoxAdjust : BoxRegular
    {
        const int WID = 200;
        const int HEI = 200;
        static Size TL = new Size(InteriorMargin, BarHeight + InteriorMargin);
        static int dotR = 3;
        static int dotD = dotR * 2;
        static Brush br = new SolidBrush(Color.Black);
        static Pen pen = new Pen(Color.DarkGray, 2);
        static Bitmap back;

        static BoxAdjust()
        {
            back = new Bitmap(WID, HEI);
            using (var gr = Graphics.FromImage(back))
            {
                gr.Clear(brClear.Color);
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var cn = Color.FromArgb(225, 225, 240);
                var c5 = Color.FromArgb(210, 210, 225);
                var c0 = Color.FromArgb(195, 195, 210);
                var pn = new Pen(c0);
                var iwid = 200 - 2 * InteriorMargin;
                var ihei = 200 - BarHeight - 2 * InteriorMargin;
                for (int q = 0; q < 11; q++)
                {
                    if (q % 5 == 0)
                    {
                        if (q == 5) pn.Color = c5;
                        else pn.Color = c0;
                    }
                    else pn.Color = cn;
                    var y = BarHeight + InteriorMargin + (int)(ihei * (float)q / 10);
                    var x = InteriorMargin + (int)(iwid * (float)q / 10);
                    gr.DrawLine(pn, InteriorMargin, y, WID - InteriorMargin, y);
                    gr.DrawLine(pn, x, BarHeight + InteriorMargin, x, HEI - InteriorMargin);
                }
            }
        }

        Adjust Proc;
        int Width;
        int Height;
        float xstep;

        public BoxAdjust(Point start, Adjust proc) : base(start, new Size(WID, HEI), proc)
        {
            Proc = proc;
            Width = Rect.Width - 2 * InteriorMargin;
            Height = Rect.Height - 2 * InteriorMargin - BarHeight;
            xstep = (float)Width / (Proc.vs.Length - 1);
        }

        Point mclick;
        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            mclick = pos - TLCorner;
            return ActionResult.Internal;
        }
        public override void MouseMove(Point pos)
        {
            var p = pos - TL;
            int i = Math.Max(Math.Min((int)(((p.X + xstep * 0.5f) / Width) * (Proc.vs.Length - 1)), Proc.vs.Length - 1), 0);
            var v = Math.Max(Math.Min(1 - (p.Y / (float)Height), 1), 0);
            Proc.Set(v, i);
        }
        public override ActionResult MouseUp(Point pos)
        {
            return ActionResult.Execute;
        }

        protected override void DrawInside(Graphics gr)
        {
            gr.DrawImageUnscaled(back, Rect.Location);

            var vs = Proc.vs;
            float x0 = Rect.X + InteriorMargin;
            float x = x0;

            var y0 = Rect.Y + BarHeight + InteriorMargin;

            var prev = new PointF(x, y0 + Y(vs[0]));
            for (int q = 1; q < vs.Length; q++)
            {
                x += xstep;
                var p = new PointF(x, y0 + Y(vs[q]));
                gr.DrawLine(pen, prev, p);
                prev = p;
            }

            x = x0;
            for (int q = 0; q < vs.Length; q++)
            {
                int y = Y(vs[q]);
                gr.FillEllipse(br, x - dotR, y0 + y - dotR, dotD, dotD);
                x += xstep;
            }
            int Y(float v)
            {
                return (int)((1 - v) * Height);
            }
        }
    }
    [Serializable]
    class BoxValue : BoxRegular
    {
        static int dotR = 3;
        static int dotD = dotR * 2;
        static Brush brValText = new SolidBrush(Color.FromArgb(90, 90, 90));

        Value Proc;
        int Width;
        int Height;
        float k;
        static Brush br = new SolidBrush(Color.Black);

        public BoxValue(Point start, Value proc, float K = 1) : base(start, new Size(200, BarHeight + PinsD + InteriorMargin + 10), proc)
        {
            Proc = proc;
            Width = Rect.Width - 2 * InteriorMargin;
            Height = Rect.Height - InteriorMargin - BarHeight;
            k = K;
        }

        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            return ActionResult.Internal;
        }
        public override void MouseMove(Point pos)
        {
            var p = pos - TLCorner;
            if (p.X < 0) p.X = 0;
            else if (p.X > Width) p.X = Width;
            Proc.V = p.X * k / Width;
        }
        protected override void DrawInside(Graphics gr)
        {
            var s = Proc.V.ToString();
            var meas = gr.MeasureString(s, font);
            gr.DrawString(s, font, brValText, (Rect.Right + Rect.Left) * 0.5f - meas.Width * 0.5f, Rect.Y + BarHeight);
            gr.FillEllipse(br, Rect.X + InteriorMargin + (int)(Proc.V / k * Width) - dotR, Rect.Y + BarHeight + (PinsD >> 1) + 10, dotD, dotD);
        }
        public override ActionResult MouseUp(Point pos)
        {
            return ActionResult.Execute;
        }
    }
    [Serializable]
    class BoxColor : BoxRegular
    {
        static int pickBthHeight = 20;

        SingleColor Proc;
        Rectangle interior;
        [NonSerialized]
        ColorPickerDialog dialog;
        [NonSerialized]
        Form1 frm;
        [NonSerialized]
        Bitmap back;

        public BoxColor(Point start, SingleColor proc, Form1 form) : base(start, new Size(50, BarHeight + 50), proc)
        {
            Proc = proc;
            proc.V = new InternalColor(0, 0, 0);
            interior = new Rectangle(InteriorMargin, BarHeight, Rect.Width - 2 * InteriorMargin, Rect.Height - BarHeight - pickBthHeight);

            Init();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            dialog = new ColorPickerDialog();
            dialog.StartPosition = FormStartPosition.Manual;
            frm = Form1.Frm;
            back = Painting.ChessBoard(interior.Width - 1, interior.Height - 1, 5, Color.FromArgb(192, 192, 192), Color.FromArgb(64, 64, 64));
        }

        public void SetColor(RawColor col)
        {
            Proc.V = col;
        }

        protected override void DrawInside(Graphics gr)
        {
            var nrect = interior;
            nrect.Location += (Size)Rect.Location;
            gr.DrawImageUnscaled(back, new Point(nrect.X + 1, nrect.Y + 1));
            using (var br = new SolidBrush(((RawColor)(Proc.V)).ColorV))
            {
                gr.FillRectangle(br, nrect);
            }
            gr.DrawImageUnscaled(Properties.Resources.colSel2, Rect.X + (Rect.Width - 20) / 2, Rect.Y + Rect.Height - pickBthHeight);
        }

        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            return ActionResult.Internal;
        }
        public override ActionResult MouseUp(Point pos)
        {
            if (pos.Y > Rect.Height - pickBthHeight)
            {
                return ActionResult.PickColor;
            }
            else
            {
                dialog.Value = Proc.V;
                var p = frm.PointToScreen(Rect.Location);
                p.X -= dialog.Width / 2;
                p.Y -= dialog.Height / 2;
                if (p.X < 0) p.X = 0;
                if (p.Y < 0) p.Y = 0;
                dialog.Location = p;

                var res = dialog.ShowDialog();
                if (res == DialogResult.OK)
                {
                    Proc.V = dialog.Value;
                    return ActionResult.Execute;
                }
                else return ActionResult.None;
            }
        }
    }
    [Serializable]
    class BoxColToARGB : IBox
    {
        public BoxColToARGB(Point start)
        {
            Rect = new Rectangle(start, new Size(100, BarHeight + PinsD * 4));
            Init();
            PrepareBar();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            var rgb = new ColorHolder();

            var a = new ColorToA();
            a.Inputs[0] = rgb;
            var r = new ColorToR();
            r.Inputs[0] = rgb;
            var g = new ColorToG();
            g.Inputs[0] = rgb;
            var b = new ColorToB();
            b.Inputs[0] = rgb;

            SetPinsFromProcs(new string[][] { null, new string[] { "A", "R", "G", "B", } }, rgb, a, r, g, b);
        }

        public override string ToString()
        {
            return "BoxColToARGB";
        }
    }
    [Serializable]
    class BoxColToHSB : IBox
    {
        public BoxColToHSB(Point start)
        {
            Rect = new Rectangle(start, new Size(100, RegularHeight(3)));
            Init();
            PrepareBar();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            var rgb = new ColorHolder();
            var h = new ColorToHue();
            h.Inputs[0] = rgb;
            var s = new ColorToSaturation();
            s.Inputs[0] = rgb;
            var b = new ColorToBrightness();
            b.Inputs[0] = rgb;

            SetPinsFromProcs(new string[][] { null, new string[] { "Hue", "Saturation", "Brightness" } }, rgb, h, s, b);
        }

        public override string ToString()
        {
            return "BoxColToHSB";
        }
    }
    [Serializable]
    class BoxColToHCB : IBox
    {
        public BoxColToHCB(Point start)
        {
            Rect = new Rectangle(start, new Size(100, RegularHeight(3)));
            Init();
            PrepareBar();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            var rgb = new ColorHolder();
            var h = new ColorToHue();
            h.Inputs[0] = rgb;
            var c = new ColorToColorful();
            c.Inputs[0] = rgb;
            var b = new ColorToBrightness();
            b.Inputs[0] = rgb;

            SetPinsFromProcs(new string[][] { null, new string[] { "Hue", "Colorfulness", "Brightness" } }, rgb, h, c, b);
        }

        public override string ToString()
        {
            return "BoxColToHCB";
        }
    }
    [Serializable]
    class BoxScript : BoxRegular
    {
        const string icoText = "</>";
        static Bitmap ico;
        static Point icop = new Point(PinsD, BarHeight);
        
        static BoxScript()
        {
            ico = new Bitmap(100 - 2 * PinsD, 80);
            using (var gr = Graphics.FromImage(ico))
            {
                gr.Clear(brClear.Color);
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                gr.DrawString(icoText, new Font("Arial", 20), Brushes.LightGreen, new Point(5, 15));
            }
        }

        [NonSerialized]
        ScriptForm form;

        public BoxScript(Point start, ScriptOutVal proc) : base(start, proc, 5)
        {
            form = new ScriptForm(proc);
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            form = new ScriptForm(pins[0][0].Proc as ScriptOutVal);
        }

        public override ActionResult MouseDown(Point pos, MouseButtons btn)
        {
            return ActionResult.Internal;
        }
        public override ActionResult MouseUp(Point pos)
        {
            form.Show();

            return ActionResult.None;
        }

        protected override void DrawInside(Graphics gr)
        {
            gr.DrawImageUnscaled(ico, Rect.Location.X + icop.X, Rect.Location.Y + icop.Y + 20);
        }
    }
}
