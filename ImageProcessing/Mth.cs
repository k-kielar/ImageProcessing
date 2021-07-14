using System;
using System.Drawing;

namespace KKLib
{
    public class Mth
    {
        public const double PI2 = Math.PI * 2;
        public const double PIHalf = Math.PI * 0.5;

        public static float Dsq(PointF pa, PointF pb)
        {
            var dx = pb.X - pa.X;
            var dy = pb.Y - pa.Y;
            return dx * dx + dy * dy;
        }
        public static float Dist(PointF pa, PointF pb)
        {
            return Dist(pb.X - pa.X, pb.Y - pa.Y);
        }
        public static float Dist(float dx, float dy)
        {
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static PointF OnLine(PointF pa, PointF pb, float d)
        {
            float Dx = pb.X - pa.X;
            float Dy = pb.Y - pa.Y;
            float D = (float)Dist(Dx, Dy);
            var ret = new PointF();
            ret.X = pa.X + Dx * d / D;
            ret.Y = pa.Y + Dy * d / D;
            return ret;
        }
    }
}
