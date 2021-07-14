using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Scripting.Hosting;
using KKLib;

namespace ImageProcessing
{
    [Serializable]
    class IProcessor
    {
        public static readonly Type TVal = typeof(float);
        public static readonly Type TCol = typeof(InternalColor);

        public Type[] InputTypes;
        public Type OutputType;
        [NonSerialized]
        public IProcessor[] Inputs;
        [NonSerialized]
        int[] lastXs;
        [NonSerialized]
        Vector<float>[] lastVals;
        [NonSerialized]
        VectorColor[] lastCols;

        public IProcessor()
        {
            InitLasts();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Inputs = new IProcessor[InputTypes.Length];
            InitLasts();
        }
        void InitLasts()
        {
            lastVals = new Vector<float>[BoxEndPoint.THREADN];
            lastCols = new VectorColor[BoxEndPoint.THREADN];
            lastXs = new int[BoxEndPoint.THREADN];
            for (int q = 0; q < lastXs.Length; q++)
            {
                lastXs[q] = -1;
            }
        }

        public virtual float Evaluate(Point p)
        {
            return 0;
        }
        public virtual InternalColor EvaluateC(Point p)
        {
            return new InternalColor();
        }
        public virtual Vector<float> Evaluate(int x, int y, int th)
        {
            var ret = new float[Vector<float>.Count];
            for (int q = 0; q < ret.Length; q++)
            {
                ret[q] = (float)Evaluate(new Point(x + q, y));
            }
            return new Vector<float>(ret);
        }
        public Vector<float> Evaluatev(int x, int y, int th)
        {
            if (lastXs[th] == x)
            {
                return lastVals[th];
            }
            else
            {
                var ret = Evaluate(x, y, th);
                lastXs[th] = x;
                lastVals[th] = ret;
                return ret;
            }
        }
        public virtual VectorColor EvaluateC(int x, int y, int th)
        {
            var al = new float[Vector<float>.Count];
            var re = new float[Vector<float>.Count];
            var gr = new float[Vector<float>.Count];
            var bl = new float[Vector<float>.Count];
            for (int q = 0; q < Vector<float>.Count; q++)
            {
                var v = EvaluateC(new Point(x + q, y));
                al[q] = v.A;
                re[q] = v.R;
                gr[q] = v.G;
                bl[q] = v.B;
            }
            return new VectorColor(new Vector<float>(al), new Vector<float>(re), new Vector<float>(gr), new Vector<float>(bl));
        }
        public VectorColor EvaluateCv(int x, int y, int th)
        {
            if (lastXs[th] == x)
            {
                return lastCols[th];
            }
            else
            {
                var ret = EvaluateC(x, y, th);
                lastXs[th] = x;
                lastCols[th] = ret;
                return ret;
            }
        }
        public virtual bool CanEvaluate(Branch<IProcessor> inLine)
        {
            if (inLine.Exist(this)) return false;
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) return false;
                if (!inp.CanEvaluate(new Branch<IProcessor>(this, inLine))) return false;
            }
            return true;
        }
        public virtual Size GetSize()
        {
            var ret = new Size(int.MaxValue, int.MaxValue);
            for (int q = 0; q < Inputs.Length; q++)
            {
                var s = Inputs[q].GetSize();
                if (s.Width < ret.Width) ret.Width = s.Width;
                if (s.Height < ret.Height) ret.Height = s.Height;
            }
            return ret;
        }
        public virtual string[][] Labels
        {
            get
            {
                return null;
            }
        }
    }
    [Serializable]
    abstract class ImgSource : IProcessor, IDisposable
    {
        protected static readonly Vector<float> bdiv = new Vector<float>((float)(1.0 / 0xff));
        protected static readonly Vector<float> gdiv = new Vector<float>((float)(1.0 / 0xff00));
        protected static readonly Vector<float> rdiv = new Vector<float>((float)(1.0 / 0xff0000));
        protected static readonly Vector<float> adiv = new Vector<float>((float)(1.0 / 0xff000000));

        public Bitmap bmp;
        protected PixelerVectoric pxlr;

        public ImgSource(Bitmap Bmp)
        {
            bmp = Bmp;
            pxlr = new PixelerVectoric(bmp);
            OutputType = TCol;
            Inputs = new IProcessor[0];
        }

        public override InternalColor EvaluateC(Point p)
        {
            return pxlr.GetPixel(p.X, p.Y);
        }

        public override bool CanEvaluate(Branch<IProcessor> inLine)
        {
            return true;
        }
        public override Size GetSize()
        {
            return bmp.Size;
        }

        public void Dispose()
        {
            pxlr.Dispose();
        }
        public void Renew()
        {
            pxlr = new PixelerVectoric(bmp);
        }
        public void Close()
        {
            pxlr.Dispose();
            bmp.Dispose();
            bmp = null;
        }

        static Ctor Creator;
        static ImgSource()
        {
            switch (VectorColor.Count)
            {
                case 2: Creator = new Ctor2(); break;
                case 4: Creator = new Ctor4(); break;
                case 8: Creator = new Ctor8(); break;
                case 16: Creator = new Ctor16(); break;
                case 32: Creator = new Ctor32(); break;
                default: Creator = new Ctor1(); break;
            }
        }
        public static ImgSource New(Bitmap bmp)
        {
            return Creator.Create(bmp);
        }
        abstract class Ctor
        {
            public abstract ImgSource Create(Bitmap bmp);
        }
        class Ctor1 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource1(bmp);
            }
        }
        class Ctor2 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource2(bmp);
            }
        }
        class Ctor4 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource4(bmp);
            }
        }
        class Ctor8 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource8(bmp);
            }
        }
        class Ctor16 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource16(bmp);
            }
        }
        class Ctor32 : Ctor
        {
            public override ImgSource Create(Bitmap bmp)
            {
                return new ImgSource32(bmp);
            }
        }
    }
    class ImgSource1 : ImgSource
    {
        public ImgSource1(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V1();
            vstr.Fs = pxlr.GetVector1(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class ImgSource2 : ImgSource
    {
        public ImgSource2(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V2();
            vstr.Fs = pxlr.GetVector2(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class ImgSource4 : ImgSource
    {
        public ImgSource4(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V4();
            vstr.Fs = pxlr.GetVector4(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class ImgSource8 : ImgSource
    {
        public ImgSource8(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V8();
            vstr.Fs = pxlr.GetVector8(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class ImgSource16 : ImgSource
    {
        public ImgSource16(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V16();
            vstr.Fs = pxlr.GetVector16(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class ImgSource32 : ImgSource
    {
        public ImgSource32(Bitmap bmp) : base(bmp) { }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var vstr = new V32();
            vstr.Fs = pxlr.GetVector32(x, y);
            var va = Vector.ConvertToSingle(vstr.V & MthVector.MASK_ALPHA) * adiv;
            var vr = Vector.ConvertToSingle(vstr.V & MthVector.MASK_RED) * rdiv;
            var vg = Vector.ConvertToSingle(vstr.V & MthVector.MASK_GREEN) * gdiv;
            var vb = Vector.ConvertToSingle(vstr.V & MthVector.MASK_BLUE) * bdiv;
            return new VectorColor(va, vr, vg, vb);
        }
    }
    class FolderSource : Regular<InternalColor>
    {
        readonly float[][] As;
        readonly float[][] Rs;
        readonly float[][] Gs;
        readonly float[][] Bs;

        public FolderSource(string[] paths)
        {
            foreach (var path in paths)
            {
                var bmp = new Bitmap(path);
                var pxlr = new Pixeler(bmp);
                if (As == null)
                {
                    As = General.NewMatrix<float>(bmp.Height, bmp.Width);
                    Rs = General.NewMatrix<float>(bmp.Height, bmp.Width);
                    Gs = General.NewMatrix<float>(bmp.Height, bmp.Width);
                    Bs = General.NewMatrix<float>(bmp.Height, bmp.Width);
                }
                int limq = As.Length;
                int limw = As[0].Length;
                if (bmp.Height < limq) limq = bmp.Height;
                if (bmp.Width < limw) limw = bmp.Width;
                for (int q = 0; q < limq; q++)
                {
                    var arow = As[q];
                    var rrow = Rs[q];
                    var grow = Gs[q];
                    var brow = Bs[q];
                    for (int w = 0; w < limw; w++)
                    {
                        var pix = pxlr.GetPixel(w, q);
                        arow[w] += pix.A;
                        rrow[w] += pix.R;
                        grow[w] += pix.G;
                        brow[w] += pix.B;
                    }
                }
                pxlr.Dispose();
                bmp.Dispose();
            }
            var k = 1.0f / (255 * paths.Length);
            for (int q = 0; q < As.Length; q++)
            {
                var arow = As[q];
                var rrow = Rs[q];
                var grow = Gs[q];
                var brow = Bs[q];
                for (int w = 0; w < arow.Length; w++)
                {
                    arow[w] *= k;
                    rrow[w] *= k;
                    grow[w] *= k;
                    brow[w] *= k;
                }
            }

            OutputType = typeof(InternalColor);
            Inputs = new IProcessor[0];
        }

        public override InternalColor EvaluateC(Point p)
        {
            return new InternalColor(As[p.Y][p.X], Rs[p.Y][p.X], Gs[p.Y][p.X], Bs[p.Y][p.X]);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return new VectorColor(new Vector<float>(As[y], x), new Vector<float>(Rs[y], x), new Vector<float>(Gs[y], x), new Vector<float>(Bs[y], x));
        }
        public override bool CanEvaluate(Branch<IProcessor> inLine)
        {
            return true;
        }
        public override Size GetSize()
        {
            return new Size(As[0].Length, As.Length);
        }
    }
    [Serializable]
    class ImgResult : IProcessor
    {
        public ImgResult()
        {
            InputTypes = new Type[] { typeof(InternalColor) };
            Inputs = new IProcessor[1];
        }

        public override InternalColor EvaluateC(Point p)
        {
            return Inputs[0].EvaluateC(p);
        }
    }
    [Serializable]
    abstract class RegularProcessor : IProcessor
    {
        public abstract IBox GenerateBox(Point start);
    }
    [Serializable]
    abstract class Regular<O> : RegularProcessor
    {
        public Regular()
        {
            InputTypes = new Type[0];
            OutputType = typeof(O);
            Inputs = new IProcessor[0];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 0);
        }
    }
    [Serializable]
    abstract class Regular1<I, O> : RegularProcessor
    {
        public Regular1()
        {
            InputTypes = new Type[] { typeof(I) };
            OutputType = typeof(O);
            Inputs = new IProcessor[1];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 1);
        }
    }
    [Serializable]
    abstract class Regular2<I1, I2, O> : RegularProcessor
    {
        public Regular2()
        {
            InputTypes = new Type[] { typeof(I1), typeof(I2) };
            OutputType = typeof(O);
            Inputs = new IProcessor[2];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 2);
        }
    }
    [Serializable]
    abstract class Regular3<I1, I2, I3, O> : RegularProcessor
    {
        public Regular3()
        {
            InputTypes = new Type[] { typeof(I1), typeof(I2), typeof(I3) };
            OutputType = typeof(O);
            Inputs = new IProcessor[3];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 3);
        }
    }
    [Serializable]
    abstract class Regular4<I1, I2, I3, I4, O> : RegularProcessor
    {
        public Regular4()
        {
            InputTypes = new Type[] { typeof(I1), typeof(I2), typeof(I3), typeof(I4) };
            OutputType = typeof(O);
            Inputs = new IProcessor[4];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 4);
        }
    }
    [Serializable]
    abstract class Regular5<I1, I2, I3, I4, I5, O> : RegularProcessor
    {
        public Regular5()
        {
            InputTypes = new Type[] { typeof(I1), typeof(I2), typeof(I3), typeof(I4), typeof(I5) };
            OutputType = typeof(O);
            Inputs = new IProcessor[5];
        }

        public override IBox GenerateBox(Point start)
        {
            return new BoxRegular(start, this, 5);
        }
    }
    [Serializable]
    class ColorToA : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].EvaluateC(p).A;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].EvaluateCv(x, y, th).As;
        }
    }
    [Serializable]
    class ColorToR : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].EvaluateC(p).R;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].EvaluateCv(x, y, th).Rs;
        }
    }
    [Serializable]
    class ColorToG : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].EvaluateC(p).G;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].EvaluateCv(x, y, th).Gs;
        }
    }
    [Serializable]
    class ColorToB : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].EvaluateC(p).B;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].EvaluateCv(x, y, th).Bs;
        }
    }
    [Serializable]
    class ARGBToColor : Regular4<float, float, float, float, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            return new InternalColor(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p), Inputs[2].Evaluate(p), Inputs[3].Evaluate(p));
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return new VectorColor(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th), Inputs[2].Evaluatev(x, y, th), Inputs[3].Evaluatev(x, y, th));
        }
        public override string[][] Labels => new string[][] { new string[] { "A", "R", "G", "B" } };
    }
    [Serializable]
    class RGBToColor : Regular3<float, float, float, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            return new InternalColor(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p), Inputs[2].Evaluate(p));
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return new VectorColor(MthVector.FONE, Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th), Inputs[2].Evaluatev(x, y, th));
        }
        public override string[][] Labels => new string[][] { new string[] { "R", "G", "B" } };
    }
    [Serializable]
    class HSBToColor : Regular3<float, float, float, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            return InternalColor.FromHSB(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p), Inputs[2].Evaluate(p));
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return VectorColor.FromHSB(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th), Inputs[2].Evaluatev(x, y, th));
        }
        public override string[][] Labels => new string[][] { new string[] { "Hue", "Saturation", "Brightness" } };
    }
    [Serializable]
    class HCBToColor : Regular3<float, float, float, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            return InternalColor.FromHCB(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p), Inputs[2].Evaluate(p));
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return VectorColor.FromHCB(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th), Inputs[2].Evaluatev(x, y, th));
        }
        public override string[][] Labels => new string[][] { new string[] { "Hue", "Colorfulness", "Brightness" } };
    }
    [Serializable]
    class ColorHolder : Regular1<InternalColor, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            return Inputs[0].EvaluateC(p);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return Inputs[0].EvaluateC(x, y, th);
        }
    }
    [Serializable]
    class ColorToHue : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            var col = Inputs[0].EvaluateC(p);
            return col.Hue();
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var cols = Inputs[0].EvaluateCv(x, y, th);
            return cols.Hue();
        }
    }
    [Serializable]
    class ColorToSaturation : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            var col = Inputs[0].EvaluateC(p);
            return col.Saturation();
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var cols = Inputs[0].EvaluateCv(x, y, th);
            return cols.Saturation();
        }
    }
    [Serializable]
    class ColorToColorful : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            var col = Inputs[0].EvaluateC(p);
            return col.Colorfulness();
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var cols = Inputs[0].EvaluateCv(x, y, th);
            return cols.Colorfulness();
        }
    }
    [Serializable]
    class ColorToBrightness : Regular1<InternalColor, float>
    {
        public override float Evaluate(Point p)
        {
            var col = Inputs[0].EvaluateC(p);
            return col.Brightness();
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var cols = Inputs[0].EvaluateCv(x, y, th);
            return cols.Brightness();
        }
    }
    [Serializable]
    class ValueToGrayScale : Regular1<float, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            var v = Inputs[0].Evaluate(p);
            return new InternalColor(v, v, v);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var v = Inputs[0].Evaluatev(x, y, th);
            return new VectorColor(MthVector.FONE, v, v, v);
        }
    }
    [Serializable]
    abstract class OneValueInput : Regular1<float, float>
    {

    }
    [Serializable]
    class Invert : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            return 1.0f - Inputs[0].Evaluate(p);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return MthVector.FONE - Inputs[0].Evaluatev(x, y, th);
        }
    }
    [Serializable]
    class Trim : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            var v = Inputs[0].Evaluate(p);
            //return Math.Min(Math.Max(v, 0), 1);
            if (!(v >= 0)) return 0.0f;
            if (v > 1) return 1.0f;
            return v;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var vs = Inputs[0].Evaluatev(x, y, th);
            vs = Vector.Max(vs, MthVector.FZERO);
            return Vector.Min(vs, MthVector.FONE);
        }
    }
    [Serializable]
    class SqRoot : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            return (float)Math.Sqrt(Inputs[0].Evaluate(p));
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Vector.SquareRoot(Inputs[0].Evaluatev(x, y, th));
        }
    }
    [Serializable]
    class Sin : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            return (float)Math.Sin(Mth.PI2 * Inputs[0].Evaluate(p)) * 0.5f + 0.5f;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var inp = Inputs[0].Evaluatev(x, y, th) * MthVector.FPI2;
            var vs = new float[VectorColor.Count];
            for (int q = 0; q < vs.Length; q++)
            {
                vs[q] = (float)Math.Sin(inp[q]);
            }
            return new Vector<float>(vs) * MthVector.FHALF + MthVector.FHALF;
        }
    }
    [Serializable]
    class ASin : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            return (float)(Math.Asin(2 * Inputs[0].Evaluate(p) - 1) / Math.PI);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var inp = Inputs[0].Evaluatev(x, y, th) * MthVector.FTWO - MthVector.FONE;
            var vs = new float[VectorColor.Count];
            for (int q = 0; q < vs.Length; q++)
            {
                vs[q] = (float)Math.Asin(inp[q]);
            }
            return new Vector<float>(vs) / MthVector.FPI;
        }
    }
    [Serializable]
    class SlopeTan : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            return (float)Math.Tan(Mth.PIHalf * Inputs[0].Evaluate(p));
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var inp = Inputs[0].Evaluatev(x, y, th) * MthVector.FPIHALF;
            var vs = new float[VectorColor.Count];
            for (int q = 0; q < vs.Length; q++)
            {
                vs[q] = (float)Math.Tan(inp[q]);
            }
            return new Vector<float>(vs);
        }
    }
    [Serializable]
    class RoundTo01 : OneValueInput
    {
        public override float Evaluate(Point p)
        {
            var v = Inputs[0].Evaluate(p);
            if (v < 0.5f) return 0.0f;
            else return 1.0f;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Vector.ConditionalSelect(Vector.LessThan(Inputs[0].Evaluatev(x, y, th), MthVector.FHALF), MthVector.FZERO, MthVector.FONE);
        }
    }
    [Serializable]
    class SafeAdd : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            var ret = Inputs[0].Evaluate(p) + Inputs[1].Evaluate(p);
            if (ret > 1) ret = 1;
            return ret;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var sum = Vector.Add(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th));
            return Vector.Min(sum, MthVector.FONE);
        }
    }
    [Serializable]
    class SafeSubtract : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            var ret = Inputs[0].Evaluate(p) - Inputs[1].Evaluate(p);
            if (ret < 0) ret = 0;
            return ret;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var sum = Vector.Subtract(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th));
            return Vector.Max(sum, MthVector.FZERO);
        }
    }
    [Serializable]
    class CircularAdd : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            var ret = Inputs[0].Evaluate(p) + Inputs[1].Evaluate(p);
            if (ret > 1) ret -= 1;
            return ret;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var sum = Inputs[0].Evaluatev(x, y, th) + Inputs[1].Evaluatev(x, y, th);
            var summ1 = sum - MthVector.FONE;
            return Vector.ConditionalSelect(Vector.GreaterThan(sum, MthVector.FONE), summ1, sum);
        }
    }
    [Serializable]
    class Add : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].Evaluate(p) + Inputs[1].Evaluate(p);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].Evaluatev(x, y, th) + Inputs[1].Evaluatev(x, y, th);
        }
    }
    [Serializable]
    class Subtract : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].Evaluate(p) - Inputs[1].Evaluate(p);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].Evaluatev(x, y, th) - Inputs[1].Evaluatev(x, y, th);
        }
    }
    [Serializable]
    class Multiply : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].Evaluate(p) * Inputs[1].Evaluate(p);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].Evaluatev(x, y, th) * Inputs[1].Evaluatev(x, y, th);
        }
    }
    [Serializable]
    class Divide : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Inputs[0].Evaluate(p) / Inputs[1].Evaluate(p);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Inputs[0].Evaluatev(x, y, th) / Inputs[1].Evaluatev(x, y, th);
        }
    }
    [Serializable]
    class Min : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Math.Min(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p));
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Vector.Min(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th));
        }
    }
    [Serializable]
    class Max : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return Math.Max(Inputs[0].Evaluate(p), Inputs[1].Evaluate(p));
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return Vector.Max(Inputs[0].Evaluatev(x, y, th), Inputs[1].Evaluatev(x, y, th));
        }
    }
    [Serializable]
    class Mean : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            return (Inputs[0].Evaluate(p) + Inputs[1].Evaluate(p)) * 0.5f;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return (Inputs[0].Evaluatev(x, y, th) + Inputs[1].Evaluatev(x, y, th)) * MthVector.FHALF;
        }
    }
    [Serializable]
    class Mean3 : Regular3<float, float, float, float>
    {
        const float THIRD = 1.0f / 3;
        public override float Evaluate(Point p)
        {
            return (Inputs[0].Evaluate(p) + Inputs[1].Evaluate(p) + Inputs[2].Evaluate(p)) * THIRD;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return (Inputs[0].Evaluatev(x, y, th) + Inputs[1].Evaluatev(x, y, th) + Inputs[2].Evaluatev(x, y, th)) * MthVector.FTHIRD;
        }
    }
    [Serializable]
    class Rand : Regular<float>
    {
        [NonSerialized]
        Random rnd;
        [NonSerialized]
        Random[] rnds;
        [NonSerialized]
        Point lastp;
        [NonSerialized]
        float lastv;

        public Rand()
        {
            Init();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            lastp = new Point(-1, -1);
            rnd = new Random();
            rnds = new Random[BoxResult.THREADN];
            var t = (int)DateTime.Now.Ticks;
            for (int q = 0; q < rnds.Length; q++)
            {
                rnds[q] = new Random(t + q);
            }
        }

        public override float Evaluate(Point p)
        {
            if (p == lastp) return lastv;
            lastv = (float)rnd.NextDouble();
            lastp = p;
            return lastv;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var vs = new float[VectorColor.Count];
            var rrr = rnds[th];
            for (int q = 0; q < vs.Length; q++)
            {
                vs[q] = (float)rrr.NextDouble();
            }
            return new Vector<float>(vs);
        }
    }
    [Serializable]
    class Value : Regular<float>
    {
        float v;
        [NonSerialized]
        Vector<float> vs;

        public float V
        {
            get
            {
                return v;
            }
            set
            {
                v = value;
                vs = new Vector<float>(value);
            }
        }

        public override float Evaluate(Point p)
        {
            return v;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            return vs;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            vs = new Vector<float>(v);
        }
    }
    [Serializable]
    class SingleColor : Regular<InternalColor>
    {
        InternalColor v;
        [NonSerialized]
        VectorColor vc;

        public InternalColor V
        {
            get
            {
                return v;
            }
            set
            {
                v = value;
                vc = new VectorColor(new Vector<float>((float)value.A), new Vector<float>((float)value.R), new Vector<float>((float)value.G), new Vector<float>((float)value.B));
            }
        }

        public override InternalColor EvaluateC(Point p)
        {
            return v;
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            return vc;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            vc = new VectorColor(new Vector<float>((float)v.A), new Vector<float>((float)v.R), new Vector<float>((float)v.G), new Vector<float>((float)v.B));
        }
    }
    [Serializable]
    class Opaque : Regular1<InternalColor, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            var col = Inputs[0].EvaluateC(p);
            return new InternalColor(1, col.R, col.G, col.B);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var col = Inputs[0].EvaluateCv(x, y, th);
            col.As = MthVector.FONE;
            return col;
        }
    }
    [Serializable]
    class Merge : Regular3<InternalColor, float, InternalColor, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            var col1 = Inputs[0].EvaluateC(p);
            var col2 = Inputs[2].EvaluateC(p);
            var fac = Inputs[1].Evaluate(p);
            var inv = 1 - fac;
            return new InternalColor(col1.A * inv + col2.A * fac, col1.R * inv + col2.R * fac, col1.G * inv + col2.G * fac, col1.B * inv + col2.B * fac);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var col1 = Inputs[0].EvaluateCv(x, y, th);
            var col2 = Inputs[2].EvaluateCv(x, y, th);
            var fac = Inputs[1].Evaluatev(x, y, th);
            var inv = MthVector.FONE - fac;
            return new VectorColor(col1.As * inv + col2.As * fac, col1.Rs * inv + col2.Rs * fac, col1.Gs * inv + col2.Gs * fac, col1.Bs * inv + col2.Bs * fac);
        }
        public override string[][] Labels => new string[][] { new string[] { null, "Factor", null } };
    }
    [Serializable]
    class Paint : Regular2<InternalColor, InternalColor, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            var col1 = Inputs[0].EvaluateC(p);
            var col2 = Inputs[1].EvaluateC(p);
            var inv = col2.A;
            var fac = 1 - inv;
            return new InternalColor(1 - (1 - col1.A) * col2.A, col1.R * fac + col2.R * inv, col1.G * fac + col2.G * inv, col1.B * fac + col2.B * inv);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var col1 = Inputs[0].EvaluateCv(x, y, th);
            var col2 = Inputs[1].EvaluateCv(x, y, th);
            var inv = col2.As;
            var fac = MthVector.FONE - inv;
            var na = MthVector.FONE - (MthVector.FONE - col1.As) * col2.As;
            return new VectorColor(na, col1.Rs * fac + col2.Rs * inv, col1.Gs * fac + col2.Gs * inv, col1.Bs * fac + col2.Bs * inv);
        }
        public override string[][] Labels => new string[][] { new string[] { "Back", "Top" } };
    }
    [Serializable]
    class Adjust : OneValueInput
    {
        static readonly float[] STARTVS = new float[] { 0, 1 };
        public float[] vs;
        [NonSerialized]
        Vector<float>[] lims;
        [NonSerialized]
        Vector<float>[] vvs;
        [NonSerialized]
        Vector<float> vn;

        public Adjust(int N)
        {
            vs = STARTVS;
            ResetN(N);
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            ResetN(vs.Length);
        }
        public void ResetN(int N)
        {
            lims = new Vector<float>[N - 2];
            var step = 1.0f / (N - 1);
            var x = step;
            for (int q = 0; q < lims.Length; q++)
            {
                lims[q] = new Vector<float>(x);
                x += step;
            }
            vn = new Vector<float>(N - 1);
            var nvs = new float[N];
            vvs = new Vector<float>[N];
            for (int q = 0; q < nvs.Length; q++)
            {
                var ev = Evaluate((float)q / (nvs.Length - 1));
                nvs[q] = ev;
                vvs[q] = new Vector<float>(ev);
            }
            vs = nvs;
        }

        public void Set(float v, int i)
        {
            vs[i] = v;
            vvs[i] = new Vector<float>(v);
        }
        public override float Evaluate(Point p)
        {
            var d = Inputs[0].Evaluate(p);
            return Evaluate(d);
        }
        float Evaluate(float d)
        {
            if (!(d >= 0)) return vs[0];
            if (d >= 1) return vs[vs.Length - 1];
            int i = (int)(d * (vs.Length - 1));
            var low = vs[i];
            return low + (d - (float)i / (vs.Length - 1)) * (vs[i + 1] - low) * (vs.Length - 1);
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var inp = Inputs[0].Evaluatev(x, y, th);
            var lo = vvs[0];
            var hi = vvs[1];
            for (int q = 0; q < lims.Length; q++)
            {
                var sel = Vector.GreaterThan(inp, lims[q]);
                lo = Vector.ConditionalSelect(sel, vvs[q + 1], lo);
                hi = Vector.ConditionalSelect(sel, vvs[q + 2], hi);
            }
            var inpv = inp * vn;
            inpv = inpv - Vector.ConvertToSingle(Vector.ConvertToInt32(inpv));
            inp = Vector.ConditionalSelect(Vector.Equals(inp, MthVector.FONE), inp, inpv);

            return inp * hi + (MthVector.FONE - inp) * lo;
        }
    }
    [Serializable]
    class Intensifier : Regular2<float, float, float>
    {
        public override float Evaluate(Point p)
        {
            var a = Inputs[0].Evaluate(p);
            var i = Inputs[1].Evaluate(p);
            if (i < 0.5f)
            {
                return a * i * 2;
            }
            else
            {
                a = 1 - a;
                i = 1 - i;
                return 1 - a * i * 2;
            }
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var a = Inputs[0].Evaluatev(x, y, th);
            var i = Inputs[1].Evaluatev(x, y, th);
            return Vector.ConditionalSelect(Vector.LessThan(i, MthVector.FHALF), a * i * MthVector.FTWO, MthVector.FONE - (MthVector.FONE - a) * (MthVector.FONE - i) * MthVector.FTWO);
        }
    }
    [Serializable]
    class Resizer : Regular2<InternalColor, float, InternalColor>
    {
        [NonSerialized]
        float[][][] arrss;

        public Resizer()
        {
            Init();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            arrss = General.NewMatrix<float>(BoxResult.THREADN, 4, VectorColor.Count);
        }

        public override InternalColor EvaluateC(Point p)
        {
            var k = Inputs[1].Evaluate(new Point());
            p.X = (int)(p.X / k);
            p.Y = (int)(p.Y / k);
            return Inputs[0].EvaluateC(p);
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var k = Inputs[1].Evaluate(new Point());
            var arr = arrss[th];
            var aa = arr[0];
            var rr = arr[1];
            var gg = arr[2];
            var bb = arr[3];
            var yy = (int)(y / k);
            for (int q = 0; q < aa.Length; q++)
            {
                var inp = Inputs[0].EvaluateC(new Point((int)((x + q) / k), yy));
                aa[q] = inp.A;
                rr[q] = inp.R;
                gg[q] = inp.G;
                bb[q] = inp.B;
            }
            return new VectorColor(new Vector<float>(aa), new Vector<float>(rr), new Vector<float>(gg), new Vector<float>(bb));
        }
        public override Size GetSize()
        {
            return (Size)resize((Point)base.GetSize());
        }
        Point resize(Point p)
        {
            var k = Inputs[1].Evaluate(new Point());
            p.X = (int)(p.X * k);
            p.Y = (int)(p.Y * k);
            return p;
        }
        public override bool CanEvaluate(Branch<IProcessor> inLine)
        {
            var item = Inputs[1];
            if (item != null && item.CanEvaluate(new Branch<IProcessor>(this, null)))
            {
                var sc = item.Evaluate(new Point());
                if (!(sc > 0) || float.IsInfinity(sc))
                {
                    return false;
                }
            }
            return base.CanEvaluate(inLine);
        }
        public override string[][] Labels => new string[][] { new string[] { null, "Scale" } };
    }
    [Serializable]
    class ColorEdge : Regular1<InternalColor, InternalColor>
    {
        public override InternalColor EvaluateC(Point p)
        {
            var c = Inputs[0].EvaluateC(p);
            var relx = c.R - 0.5f;
            var rely = c.G - 0.5f;
            var relz = c.B - 0.5f;
            var arelx = Math.Abs(relx);
            var arely = Math.Abs(rely);
            var arelz = Math.Abs(relz);
            var max = Math.Max(arelx, arely);
            max = Math.Max(arelz, max);
            var k = 0.5f / max;
            relx *= k;
            rely *= k;
            relz *= k;
            c.R = 0.5f + relx;
            c.G = 0.5f + rely;
            c.B = 0.5f + relz;
            return c;
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var c = Inputs[0].EvaluateCv(x, y, th);
            var relx = c.Rs - MthVector.FHALF;
            var rely = c.Gs - MthVector.FHALF;
            var relz = c.Bs - MthVector.FHALF;
            var max = Vector.Max(Vector.Max(Vector.Abs(relx), Vector.Abs(rely)), Vector.Abs(relz));
            var k = MthVector.FHALF / max;
            return new VectorColor(c.As, MthVector.FHALF + relx * k, MthVector.FHALF + rely * k, MthVector.FHALF + relz * k);
        }
    }
    [Serializable]
    class ScriptOutVal : Regular5<float, float, float, InternalColor, InternalColor, float>
    {
        static readonly string NL = Environment.NewLine;
        static readonly string[] SEP = new string[] { Environment.NewLine };
        static readonly string[] VARNAMES = new string[] { "i0", "i1", "i2", "i3", "i4" };
        static readonly string[][] COLNAMES = new string[][] { null, null, null, new string[] { "i3a", "i3r", "i3g", "i3b" }, new string[] { "i4a", "i4r", "i4g", "i4b" } };
        static readonly InternalColor COLMID = new InternalColor(0.5f, 0.5f, 0.5f, 0.5f);
        static readonly string[] KEYWORDS = new string[] { "exceptionThrown", "vectorN", "length", "os", "is0", "is1", "is2", "is3a", "is3r", "is3g", "is3b", "is4a", "is4r", "is4g", "is4b", "x", "y" };
        const string INPUTS = " i0 , i1 , i2 , i3a , i3r , i3g , i3b , i4a , i4r , i4g , i4b";
        const string EXSCR = " o = (i0 + i1 + i2) / 3";

        public string CoreScript = "";
        [NonSerialized]
        ScriptEngine eng;
        [NonSerialized]
        ScriptSource script;
        [NonSerialized]
        ScriptScope scope;
        [NonSerialized]
        ScriptSource vscript;
        [NonSerialized]
        ScriptScope[] vscope_;
        [NonSerialized]
        int[] lasty;
        [NonSerialized]
        float[][] o_;
        [NonSerialized]
        Vector<float>[][][] varrs_;
        [NonSerialized]
        Vector<float>[][][][] varrss_;

        public ScriptOutVal()
        {
            CoreScript = "# " + Res.Mngr.GetString("scrl1") + Environment.NewLine + 
                "# " + Res.Mngr.GetString("scrl2") + INPUTS + Environment.NewLine + 
                "# " + Res.Mngr.GetString("scrl3") + " o" + Environment.NewLine + 
                "# " + Res.Mngr.GetString("scrl4") + Environment.NewLine +
                "# " + Res.Mngr.GetString("scrl5") + ":" + EXSCR + Environment.NewLine;
            Init();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            eng = IronPython.Hosting.Python.CreateEngine();
            scope = eng.CreateScope();
            o_ = new float[BoxResult.THREADN][];
            varrs_ = new Vector<float>[BoxResult.THREADN][][];
            varrss_ = new Vector<float>[BoxResult.THREADN][][][];
            for (int q = 0; q < varrss_.Length; q++)
            {
                varrs_[q] = new Vector<float>[3][];
                varrss_[q] = new Vector<float>[2][][];
            }
            lasty = new int[BoxResult.THREADN];
            vscope_ = new ScriptScope[BoxResult.THREADN];
        }

        public override bool CanEvaluate(Branch<IProcessor> inLine)
        {
            scope = eng.CreateScope();
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) continue;
                if (!inp.CanEvaluate(new Branch<IProcessor>(this, inLine))) return false;
                if (q < 3) setVal(scope, q);
                else setCol(scope, q, COLMID);
            }
            if (CoreScript.Trim().Length == 0) return false;

            var scr = new StringBuilder();
            scr.Append("try:" + NL);
            scr.Append(indent(CoreScript, 1));
            scr.Append("except:" + NL + " exceptionThrown=True");

            var scriptCanEval = eng.CreateScriptSourceFromString(scr.ToString());

            try
            {
                scriptCanEval.Execute(scope);
            }
            catch (Exception)
            {
                return false;
            }

            if (!scope.ContainsVariable("o")) return false;
            foreach (var keyword in KEYWORDS)
            {
                if (scope.ContainsVariable(keyword)) return false;
            }

            script = eng.CreateScriptSourceFromString(CoreScript);

            //script for vectors
            var n = GetSize().Width >> MthVector.Shift;
            for (int t = 0; t < lasty.Length; t++)
            {
                resetTh(t, n);
            }
            var vscr = new StringBuilder();
            vscr.Append("for x in range(length):" + NL + " for y in range(vectorN):" + NL);
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) continue;
                var sq = q.ToString();
                if (q < 3)
                {
                    vscr.Append("  i" + sq + "=is" + sq + "[x][y]" + NL);
                }
                else
                {
                    vscr.Append("  i" + sq + "a=is" + sq + "a[x][y]" + NL);
                    vscr.Append("  i" + sq + "r=is" + sq + "r[x][y]" + NL);
                    vscr.Append("  i" + sq + "g=is" + sq + "g[x][y]" + NL);
                    vscr.Append("  i" + sq + "b=is" + sq + "b[x][y]" + NL);
                }
            }
            vscr.Append(indent(CoreScript, 2));
            vscr.Append("  os[x*" + MthVector.Count.ToString() + "+y]=o");

            vscript = eng.CreateScriptSourceFromString(vscr.ToString());

            for (int q = 0; q < lasty.Length; q++) lasty[q] = -1;

            return true;
        }
        void resetTh(int th, int n)
        {
            var vscope = eng.CreateScope();
            vscope_[th] = vscope;
            vscope.SetVariable("vectorN", MthVector.Count);
            vscope.SetVariable("length", n);
            var o = new float[n << MthVector.Shift];
            o_[th] = o;
            vscope.SetVariable("os", o);
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) continue;
                var sq = q.ToString();
                if (q < 3)
                {
                    var varrs = new Vector<float>[n];
                    varrs_[th][q] = varrs;
                    vscope.SetVariable("is" + sq, varrs);
                }
                else
                {
                    var mtrx = General.NewMatrix<Vector<float>>(4, n);
                    varrss_[th][q - 3] = mtrx;
                    vscope.SetVariable("is" + sq + "a", mtrx[0]);
                    vscope.SetVariable("is" + sq + "r", mtrx[1]);
                    vscope.SetVariable("is" + sq + "g", mtrx[2]);
                    vscope.SetVariable("is" + sq + "b", mtrx[3]);
                }
            }
        }
        void setVal(ScriptScope scope, int q, float v = 0.5f)
        {
            scope.SetVariable(VARNAMES[q], v);
        }
        void setCol(ScriptScope scope, int q, InternalColor col)
        {
            var nams = COLNAMES[q];
            scope.SetVariable(nams[0], col.A);
            scope.SetVariable(nams[1], col.R);
            scope.SetVariable(nams[2], col.G);
            scope.SetVariable(nams[3], col.B);
        }
        string indent(string str, int n)
        {
            var lines = str.Split(SEP, StringSplitOptions.None);
            var ind = "";
            for (int q = 0; q < n; q++) ind += " ";
            var ret = new StringBuilder(str.Length + lines.Length * n);
            foreach (var line in lines)
            {
                ret.Append(ind + line + NL);
            }
            return ret.ToString();
        }
        public override float Evaluate(Point p)
        {
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) continue;
                if (q < 3) setVal(scope, q, inp.Evaluate(p));
                else setCol(scope, q, inp.EvaluateC(p));
            }

            script.Execute(scope);

            return scope.GetVariable<float>("o");
        }
        public override string[][] Labels => new string[][] { new string[] { "i0", "i1", "i2", "i3", "i4"}, new string[] { "o" } };

        public override Vector<float> Evaluate(int x, int y, int th)
        {
            if (y != lasty[th])
            {
                lasty[th] = y;
                for (int q = 0; q < Inputs.Length; q++)
                {
                    var inp = Inputs[q];
                    if (inp == null) continue;
                    if (q < 3)
                    {
                        var arr = varrs_[th][q];
                        for (int w = 0; w < arr.Length; w++)
                        {
                            arr[w] = inp.Evaluatev(w * 4, y, th);
                        }
                    }
                    else
                    {
                        var arrs = varrss_[th][q - 3];
                        var arra = arrs[0];
                        var arrr = arrs[1];
                        var arrg = arrs[2];
                        var arrb = arrs[3];
                        for (int w = 0; w < arra.Length; w++)
                        {
                            var vc = inp.EvaluateCv(w * 4, y, th);
                            arra[w] = vc.As;
                            arrr[w] = vc.Rs;
                            arrg[w] = vc.Gs;
                            arrb[w] = vc.Bs;
                        }
                    }
                }
                vscript.Execute(vscope_[th]);
            }
            return new Vector<float>(o_[th], x);
        }
        public override Size GetSize()
        {
            var ret = new Size(int.MaxValue, int.MaxValue);
            for (int q = 0; q < Inputs.Length; q++)
            {
                var inp = Inputs[q];
                if (inp == null) continue;
                var s = inp.GetSize();
                if (s.Width < ret.Width) ret.Width = s.Width;
                if (s.Height < ret.Height) ret.Height = s.Height;
            }
            return ret;
        }
    }
    [Serializable]
    class Tile : Regular1<InternalColor, InternalColor>
    {
        [NonSerialized]
        Size siz;
        [NonSerialized]
        int lim;
        [NonSerialized]
        float[][][] manipulators;

        public Tile()
        {
            Init();
        }
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Init();
        }
        void Init()
        {
            manipulators = General.NewMatrix<float>(BoxEndPoint.THREADN, 4, VectorColor.Count);
        }

        public override InternalColor EvaluateC(Point p)
        {
            return Inputs[0].EvaluateC(new Point(p.X % siz.Width, p.Y % siz.Height));
        }
        public override VectorColor EvaluateC(int x, int y, int th)
        {
            var posx = x % siz.Width;
            var posy = y % siz.Height;
            if (posx < lim)
            {
                return Inputs[0].EvaluateC(posx, posy, th);
            }
            else
            {
                var x1 = siz.Width - VectorColor.Count;
                if (x1 >= 0)
                {
                    var v0 = Inputs[0].EvaluateC(x1, posy, th);
                    var v1 = Inputs[0].EvaluateC(0, posy, th);
                    var manip = manipulators[th];
                    var va = manip[0];
                    var vr = manip[1];
                    var vg = manip[2];
                    var vb = manip[3];
                    int q = 0;
                    for (int w = posx - lim + 1; w < VectorColor.Count; q++, w++)
                    {
                        va[q] = v0.As[w];
                        vr[q] = v0.Rs[w];
                        vg[q] = v0.Gs[w];
                        vb[q] = v0.Bs[w];
                    }
                    for (int w = 0; q < VectorColor.Count; q++, w++)
                    {
                        va[q] = v1.As[w];
                        vr[q] = v1.Rs[w];
                        vg[q] = v1.Gs[w];
                        vb[q] = v1.Bs[w];
                    }
                    return new VectorColor(new Vector<float>(va), new Vector<float>(vr), new Vector<float>(vg), new Vector<float>(vb));
                }
                else
                {
                    return base.EvaluateC(x, y, th);
                }
            }
        }
        public override Size GetSize()
        {
            siz = Inputs[0].GetSize();
            lim = siz.Width / VectorColor.Count * VectorColor.Count;
            return new Size(int.MaxValue, int.MaxValue);
        }

    }
    [Serializable]
    class ColDistance : Regular2<InternalColor, InternalColor, float>
    {
        const float K = (float)0.5773502691896257;
        const float C = 1.0000001f;
        static readonly Vector<float> VK = new Vector<float>(K);
        static readonly Vector<float> VC = new Vector<float>(C);

        public override float Evaluate(Point p)
        {
            var col1 = Inputs[0].EvaluateC(p);
            var col2 = Inputs[1].EvaluateC(p);
            var dr = col2.R - col1.R;
            var dg = col2.G - col1.G;
            var db = col2.B - col1.B;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db) * K * C;
        }
        public override Vector<float> Evaluate(int x, int y, int th)
        {
            var col1 = Inputs[0].EvaluateCv(x, y, th);
            var col2 = Inputs[1].EvaluateCv(x, y, th);
            var dr = col2.Rs - col1.Rs;
            var dg = col2.Gs - col1.Gs;
            var db = col2.Bs - col1.Bs;
            return Vector.SquareRoot(dr * dr + dg * dg + db * db) * VK * VC;
        }
    }
    
    class ValueKeeper : IProcessor
    {
        public ValueKeeper()
        {
            InputTypes = new Type[] { typeof(float) };
            Inputs = new IProcessor[1];
        }
        public override float Evaluate(Point p)
        {
            return Inputs[0].Evaluate(p);
        }
    }
}
