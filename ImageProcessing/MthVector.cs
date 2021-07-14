using System;
using System.Numerics;

namespace KKLib
{
    class MthVector
    {
        public static readonly Vector<float> FONE = Vector<float>.One;
        public static readonly Vector<float> FZERO = Vector<float>.Zero;
        public static readonly Vector<float> FNEGONE = new Vector<float>(-1);
        public static readonly Vector<float> FHALF = new Vector<float>(0.5f);
        public static readonly Vector<float> FNEGHALF = new Vector<float>(-0.5f);
        public static readonly Vector<float> FTHIRD = new Vector<float>(1.0f / 3);
        public static readonly Vector<float> FQUART = new Vector<float>(0.25f);
        public static readonly Vector<float> FTWO = new Vector<float>(2);
        public static readonly Vector<float> FFOUR = new Vector<float>(4);
        public static readonly Vector<float> FPI = new Vector<float>((float)Math.PI);
        public static readonly Vector<float> FPI2 = new Vector<float>((float)(Math.PI * 2));
        public static readonly Vector<float> FPIHALF = new Vector<float>((float)(Math.PI * 0.5f));
        public static readonly Vector<int> IZERO = new Vector<int>(0);
        public static readonly Vector<int> IONE = new Vector<int>(1);
        public static readonly Vector<int> ITWO = new Vector<int>(2);
        public static readonly Vector<int> IFOUR = new Vector<int>(4);
        public static readonly Vector<int> INEGONE = new Vector<int>(-1);
        public static readonly Vector<int> ISIGNMASK = new Vector<int>(1 << 31);
        public static readonly Vector<int> INOSIGNMASK = new Vector<int>((int)(((uint)1 << 31) - 1));
        public static readonly Vector<int> FONEASI = Vector.AsVectorInt32(FONE);
        public static readonly Vector<float> FNOSIGNMASK = Vector.AsVectorSingle(new Vector<int>(0x7fffffff));
        public static readonly Vector<float> FSIGNMASK = Vector.AsVectorSingle(new Vector<uint>(0x80000000));
        public static readonly Vector<uint> MASK_BLUE = new Vector<uint>(0xff);
        public static readonly Vector<uint> MASK_GREEN = new Vector<uint>(0xff00);
        public static readonly Vector<uint> MASK_RED = new Vector<uint>(0xff0000);
        public static readonly Vector<uint> MASK_ALPHA = new Vector<uint>(0xff000000);
        public static readonly int Count = Vector<float>.Count;
        public static readonly int Shift;

        static MthVector()
        {
            var c = Count;
            while (c > 1)
            {
                c >>= 1;
                Shift++;
            }
        }

        public static Vector<float> Dist(Vector<float> dxs, Vector<float> dys, Vector<float> dzs)
        {
            return Vector.SquareRoot(dxs * dxs + dys * dys + dzs * dzs);
        }
        /// <summary>
        /// Returns -1.0f for negative and -0.0f, 1.0f for positive and 0.0f
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector<float> SignBinary(Vector<float> v)
        {
            return (v & FSIGNMASK) | FONE;
        }
        /// <summary>
        /// Returns -1.0f for negative, 0.0f for 0.0f and -0.0f, 1.0f for positive
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector<float> Sign(Vector<float> v)
        {
            var i = Vector.AsVectorInt32(v);
            var z = Vector.ConditionalSelect(Vector.Equals(v, FZERO), FZERO, FONE);
            var posneg = Vector.AsVectorSingle((i & ISIGNMASK) | FONEASI);
            return z * posneg;
        }
    }
}