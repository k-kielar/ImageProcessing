using System.Drawing;
using KKLib;

namespace ImageProcessing
{
    abstract class Static : BoxEndPoint
    {
        public Static(Point start, Size size) : base(start, new Bitmap(100, 100))
        {
            Rect = new Rectangle(start, size);

            PrepareBar();
        }
        public Static(Point start, IProcessor[] inps, string[] labels = null) : base(start, new Bitmap(100, 100))
        {
            Rect = new Rectangle(start, new Size(100, RegularHeight(inps.Length)));

            int y = BarHeight + PinsD / 2;
            var ins = new Pin[inps.Length];
            for (int q = 0; q < ins.Length; q++)
            {
                string lab = null;
                if (labels != null) lab = labels[q];
                ins[q] = new Pin(new Point(PinsMargin, y), this, 0, inps[q], lab);
                y += PinsD;
            }
            pins = new Pin[][]
            {
                ins,
                new Pin[0]
            };

            PrepareBar();
        }

        protected float ValueAt(int i)
        {
            return pins[0][i].Proc.Evaluate(new Point());
        }
        public bool IsNonNegReal(float v)
        {
            return v >= 0 && !float.IsInfinity(v);
        }

        public BoxSource Convert()
        {
            if (Ready())
            {
                Update();
                var nbmp = Process(bmp);
                if (nbmp == null) return null;
                return new BoxSource(Rect.Location, nbmp);
            }
            else
            {
                return null;
            }
        }
        protected abstract Bitmap Process(Bitmap bmp);

        public override string ToString()
        {
            var ret = base.ToString();
            ret = ret.Substring(ret.LastIndexOf('.') + 1);
            return ret;
        }
    }
    class BoxIntoSource : Static
    {
        public BoxIntoSource(Point start) : base(start, new IProcessor[] { new ImgResult() }) { }
        
        protected override Bitmap Process(Bitmap bmp)
        {
            return bmp.Clone(new Rectangle(new Point(), bmp.Size), bmp.PixelFormat);
        }
    }
    abstract class StaticRegular : Static
    {
        public StaticRegular(Point start, string[] labels = null) : base(start, new IProcessor[] { new ImgResult(), new ValueKeeper() }, labels) { }
    }
    abstract class StaticRegular2 : Static
    {
        public StaticRegular2(Point start, string[] labels = null) : base(start, new IProcessor[] { new ImgResult(), new ValueKeeper(), new ValueKeeper() }, labels) { }
    }
    class Blur : StaticRegular
    {
        public Blur(Point start) : base(start, new string[] { null, "Range" }) { }

        protected override Bitmap Process(Bitmap bmp)
        {
            var v = ValueAt(1);
            if (IsNonNegReal(v))
            {
                return Painting.MeanColorSquare(bmp, (int)v);
            }
            else return null;
        }
    }
    class NearBlur : StaticRegular
    {
        public NearBlur(Point start) : base(start, new string[] { null, "Range" }) { }

        protected override Bitmap Process(Bitmap bmp)
        {
            var v = ValueAt(1);
            if (IsNonNegReal(v))
            {
                var v2 = v * 0.1f + 1;
                return Painting.MeanColorGaussSquare(bmp, (int)v, (int)v2);
            }
            else return null;
        }
    }
}
