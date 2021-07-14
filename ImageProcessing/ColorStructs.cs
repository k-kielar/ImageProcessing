using System;
using System.Numerics;
using KKLib;

namespace ImageProcessing
{
    [Serializable]
    struct InternalColor
    {
        public const float I = 0.9999999f;
        public const float DIV = 1.0f / 255.0f;
        public const float K = 255 - I;
        public const float COLORFULK = 0.70710685f;
        public const float RADTO01 = (float)(0.5 / Math.PI);
        public const float FROM01TORAD = (float)(Math.PI * 2);
        public const float LXX = (float)(2.0 / 3);
        public const float LXYZ = (float)(-1.0 / 3);
        public const float LYYZ = (float)0.5773502691896257;
        public const float SINK1 = 1.0f / 6;
        public const float SINK2 = 1.0f / 120;
        public const float SINK3 = 1.0f / 5040;
        public const float ACOSK1 = 1.5707288f;
        public const float ACOSK2 = -0.2121144f;
        public const float ACOSK3 = 0.0742610f;
        public const float ACOSK4 = -0.0187293f;
        public static readonly float MAXRAD = (float)(Math.Sqrt(6.0) / 3);
        const float PI = (float)Math.PI;
        const float THIRD = 1.0f / 3;

        public float A;
        public float R;
        public float G;
        public float B;

        public InternalColor(float a, float r, float g, float b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
        public InternalColor(float r, float g, float b)
        {
            A = 1;
            R = r;
            G = g;
            B = b;
        }

        public static byte Flt2Byte(float d)
        {
            return (byte)(d * K + I);
        }
        public static float Byte2Flt(byte b)
        {
            return b * DIV;
        }

        public float Hue()
        {
            var a = B - G;
            var b = R - B;
            var c = G - R;

            var s2 = (float)Math.Sqrt((a * a + b * b + c * c) * 2);
            if (s2 == 0) return 0;
            var ang = ACos((b - c) / s2) * RADTO01;
            if (a > 0) ang = 1 - ang;

            return ang;
        }
        public float Saturation()
        {
            var Bri = Brightness();

            var vx = R - Bri;
            var vy = G - Bri;
            var vz = B - Bri;

            var sx = (float)Math.Sign(vx);
            var sy = (float)Math.Sign(vy);
            var sz = (float)Math.Sign(vz);
            var maxx = (sx + 1) * 0.5f - sx * Bri;
            var maxy = (sy + 1) * 0.5f - sy * Bri;
            var maxz = (sz + 1) * 0.5f - sz * Bri;

            var kx = Math.Abs(vx) / maxx;
            var ky = Math.Abs(vy) / maxy;
            var kz = Math.Abs(vz) / maxz;

            return Math.Max(Math.Max(kx, ky), kz);
        }
        public float Colorfulness()
        {
            var cpx = B - G;
            var cpy = R - B;
            var cpz = G - R;
            return (float)Math.Sqrt(cpx * cpx + cpy * cpy + cpz * cpz) * COLORFULK;
        }
        public float Brightness()
        {
            return (R + G + B) * THIRD;
        }
        
        public static InternalColor FromHSB(float H, float S, float B)
        {
            //var dx = (float)Math.Cos(FROM01TORAD * H);
            //var dy = Math.Sign(0.5f - H) * (float)Math.Sqrt(1 - dx * dx);
            var dy = Sin01(H);
            var dx = Math.Sign(Math.Abs(0.5f - H) - 0.25f) * (float)Math.Sqrt(1 - dy * dy);
            dx *= MAXRAD;
            dy = dy * MAXRAD * LYYZ;
            var vx = dx * LXX;
            var vy = dx * LXYZ + dy;
            var vz = dx * LXYZ - dy;

            var sx = (float)Math.Sign(vx);
            var sy = (float)Math.Sign(vy);
            var sz = (float)Math.Sign(vz);

            var maxx = (sx + 1) * 0.5f - sx * B;
            var maxy = (sy + 1) * 0.5f - sy * B;
            var maxz = (sz + 1) * 0.5f - sz * B;

            var kx = Math.Abs(maxx / vx);
            var ky = Math.Abs(maxy / vy);
            var kz = Math.Abs(maxz / vz);
            float km, kd;
            if (kx < ky)
            {
                if (kx < kz)
                {
                    km = maxx;
                    kd = vx;
                }
                else
                {
                    km = maxz;
                    kd = vz;
                }
            }
            else
            {
                if (ky < kz)
                {
                    km = maxy;
                    kd = vy;
                }
                else
                {
                    km = maxz;
                    kd = vz;
                }
            }

            kd = Math.Abs(kd);
            return new InternalColor(B + S * vx / kd * km, B + S * vy / kd * km, B + S * vz / kd * km);
        }
        public static InternalColor FromHCB(float H, float C, float B)
        {
            var dy = Sin01(H);
            var dx = Math.Sign(Math.Abs(0.5f - H) - 0.25f) * (float)Math.Sqrt(1 - dy * dy);
            dx *= C;
            dy = dy * C * LYYZ;
            var vx = dx * LXX;
            var vy = dx * LXYZ + dy;
            var vz = dx * LXYZ - dy;

            var sx = (float)Math.Sign(vx);
            var sy = (float)Math.Sign(vy);
            var sz = (float)Math.Sign(vz);

            var maxx = (sx + 1) * 0.5f - sx * B;
            var maxy = (sy + 1) * 0.5f - sy * B;
            var maxz = (sz + 1) * 0.5f - sz * B;

            var kx = Math.Abs(maxx / vx);
            var ky = Math.Abs(maxy / vy);
            var kz = Math.Abs(maxz / vz);
            var km = 1.0f;
            var kd = 1.0f;
            if (kx < ky)
            {
                if (kx < kz)
                {
                    if (kx < 1)
                    {
                        km = maxx;
                        kd = vx;
                    }
                }
                else
                {
                    if (kz < 1)
                    {
                        km = maxz;
                        kd = vz;
                    }
                }
            }
            else
            {
                if (ky < kz)
                {
                    if (ky < 1)
                    {
                        km = maxy;
                        kd = vy;
                    }
                }
                else
                {
                    if (kz < 1)
                    {
                        km = maxz;
                        kd = vz;
                    }
                }
            }

            kd = Math.Abs(kd);
            return new InternalColor(B + vx / kd * km, B + vy / kd * km, B + vz / kd * km);
        }

        public static implicit operator InternalColor(RawColor rc)
        {
            return new InternalColor(Byte2Flt(rc.A), Byte2Flt(rc.R), Byte2Flt(rc.G), Byte2Flt(rc.B));
        }
        public static implicit operator RawColor(InternalColor ic)
        {
            return new RawColor(Flt2Byte(ic.A), Flt2Byte(ic.R), Flt2Byte(ic.G), Flt2Byte(ic.B));
        }

        static float Sin01(float x)
        {
            var k = ((int)((x + 0.25f) * 2)) * 0.5f;
            var si = (float)Math.Sign(Math.Abs(0.5f - x) - 0.25f);
            var ks = Math.Sign(0.75f - x);
            x = x * si + ks * k;
            x *= FROM01TORAD;
            var x3 = x * x * x;
            return x - x3 * SINK1 + x3 * x * x * SINK2 - x3 * x3 * x * SINK3;
        }
        static float ACos(float x)
        {
            var s = x;
            x = Math.Abs(x);
            var xx = x * x;
            var r = (ACOSK1 + ACOSK2 * x + ACOSK3 * xx + ACOSK4 * xx * x) * (float)Math.Sqrt(1 - x);
            if (s < 0) return PI - r;
            else return r;
        }
    }

    struct VectorColor
    {
        public static readonly int Count = Vector<float>.Count;
        public static readonly Vector<float> I = new Vector<float>(InternalColor.I);
        static readonly Vector<float> DIV = new Vector<float>(InternalColor.DIV);
        static readonly Vector<float> K = new Vector<float>(InternalColor.K);
        static readonly Vector<float> COLORFULK = new Vector<float>(InternalColor.COLORFULK);
        static readonly Vector<float> RADTO01 = new Vector<float>(InternalColor.RADTO01);
        static readonly Vector<float> LXX = new Vector<float>(InternalColor.LXX);
        static readonly Vector<float> LXYZ = new Vector<float>(InternalColor.LXYZ);
        static readonly Vector<float> LYYZ = new Vector<float>(InternalColor.LYYZ);
        static readonly Vector<float> SINK1 = new Vector<float>(InternalColor.SINK1);
        static readonly Vector<float> SINK2 = new Vector<float>(InternalColor.SINK2);
        static readonly Vector<float> SINK3 = new Vector<float>(InternalColor.SINK3);
        static readonly Vector<float> ACOSK1 = new Vector<float>(InternalColor.ACOSK1);
        static readonly Vector<float> ACOSK2 = new Vector<float>(InternalColor.ACOSK2);
        static readonly Vector<float> ACOSK3 = new Vector<float>(InternalColor.ACOSK3);
        static readonly Vector<float> ACOSK4 = new Vector<float>(InternalColor.ACOSK4);
        static readonly Vector<float> MAXRAD = new Vector<float>(InternalColor.MAXRAD);
        static readonly Vector<float> VNEGHALF = new Vector<float>(-0.5f);
        static readonly Vector<int> ISIX = new Vector<int>(6);

        public Vector<float> As;
        public Vector<float> Rs;
        public Vector<float> Gs;
        public Vector<float> Bs;

        public VectorColor(Vector<float> als, Vector<float> rs, Vector<float> gs, Vector<float> bs)
        {
            As = als;
            Rs = rs;
            Gs = gs;
            Bs = bs;
        }

        public Vector<float> Hue()
        {
            var a = Bs - Gs;
            var b = Rs - Bs;
            var c = Gs - Rs;

            var s2 = Vector.SquareRoot((a * a + b * b + c * c) * 2);
            var ang = Vector.ConditionalSelect(Vector.Equals(s2, MthVector.FZERO), MthVector.FZERO, ACos((b - c) / s2) * RADTO01);
            return Vector.ConditionalSelect(Vector.GreaterThan(a, MthVector.FZERO), MthVector.FONE - ang, ang);
        }
        public Vector<float> Saturation()
        {
            var Bri = Brightness();

            var vx = Rs - Bri;
            var vy = Gs - Bri;
            var vz = Bs - Bri;

            var sx = MthVector.Sign(vx);
            var sy = MthVector.Sign(vy);
            var sz = MthVector.Sign(vz);
            var maxx = (sx + MthVector.FONE) * MthVector.FHALF - sx * Bri;
            var maxy = (sy + MthVector.FONE) * MthVector.FHALF - sy * Bri;
            var maxz = (sz + MthVector.FONE) * MthVector.FHALF - sz * Bri;

            var kx = Vector.Abs(vx) / maxx;
            var ky = Vector.Abs(vy) / maxy;
            var kz = Vector.Abs(vz) / maxz;

            return Vector.Max(Vector.Max(kx, ky), kz);
        }
        public Vector<float> Colorfulness()
        {
            var cpx = Bs - Gs;
            var cpy = Rs - Bs;
            var cpz = Gs - Rs;
            return Vector.SquareRoot(cpx * cpx + cpy * cpy + cpz * cpz) * COLORFULK;
        }
        public Vector<float> Brightness()
        {
            return (Rs + Gs + Bs) * MthVector.FTHIRD;
        }

        public static VectorColor FromHSB(Vector<float> H, Vector<float> S, Vector<float> B)
        {
            var dy = Sin(H);
            var dx = MthVector.SignBinary(Vector.Abs(MthVector.FHALF - H) - MthVector.FQUART) * Vector.SquareRoot(MthVector.FONE - dy * dy);
            dx *= MAXRAD;
            dy = dy * MAXRAD * LYYZ;
            var vx = dx * LXX;
            var vy = dx * LXYZ + dy;
            var vz = dx * LXYZ - dy;

            var sx = MthVector.SignBinary(vx);
            var sy = MthVector.SignBinary(vy);
            var sz = MthVector.SignBinary(vz);

            var maxx = (sx + MthVector.FONE) * MthVector.FHALF - sx * B;
            var maxy = (sy + MthVector.FONE) * MthVector.FHALF - sy * B;
            var maxz = (sz + MthVector.FONE) * MthVector.FHALF - sz * B;

            var kx = Vector.Abs(maxx / vx);
            var ky = Vector.Abs(maxy / vy);
            var kz = Vector.Abs(maxz / vz);

            var sel = Vector.LessThan(kx, ky);
            var k = Vector.ConditionalSelect(sel, kx, ky);
            var km = Vector.ConditionalSelect(sel, maxx, maxy);
            var kd = Vector.ConditionalSelect(sel, vx, vy);
            sel = Vector.LessThan(k, kz);
            k = Vector.ConditionalSelect(sel, k, kz);
            km = Vector.ConditionalSelect(sel, km, maxz);
            kd = Vector.ConditionalSelect(sel, kd, vz);
            kd = Vector.Abs(kd);

            return new VectorColor(MthVector.FONE, B + S * vx / kd * km, B + S * vy / kd * km, B + S * vz / kd * km);
        }
        public static VectorColor FromHCB(Vector<float> H, Vector<float> C, Vector<float> B)
        {
            var dy = Sin(H);
            var dx = MthVector.SignBinary(Vector.Abs(MthVector.FHALF - H) - MthVector.FQUART) * Vector.SquareRoot(MthVector.FONE - dy * dy);
            dx *= C;
            dy = dy * C * LYYZ;
            var vx = dx * LXX;
            var vy = dx * LXYZ + dy;
            var vz = dx * LXYZ - dy;

            var sx = MthVector.SignBinary(vx);
            var sy = MthVector.SignBinary(vy);
            var sz = MthVector.SignBinary(vz);

            var maxx = (sx + MthVector.FONE) * MthVector.FHALF - sx * B;
            var maxy = (sy + MthVector.FONE) * MthVector.FHALF - sy * B;
            var maxz = (sz + MthVector.FONE) * MthVector.FHALF - sz * B;

            var kx = Vector.Abs(maxx / vx);
            var ky = Vector.Abs(maxy / vy);
            var kz = Vector.Abs(maxz / vz);

            var sel = Vector.LessThan(kx, ky);
            var k = Vector.ConditionalSelect(sel, kx, ky);
            var km = Vector.ConditionalSelect(sel, maxx, maxy);
            var kd = Vector.ConditionalSelect(sel, vx, vy);
            sel = Vector.LessThan(k, kz);
            k = Vector.ConditionalSelect(sel, k, kz);
            km = Vector.ConditionalSelect(sel, km, maxz);
            kd = Vector.ConditionalSelect(sel, kd, vz);
            sel = Vector.LessThan(k, MthVector.FONE);
            km = Vector.ConditionalSelect(sel, km, MthVector.FONE);
            kd = Vector.ConditionalSelect(sel, kd, MthVector.FONE);
            kd = Vector.Abs(kd);

            return new VectorColor(MthVector.FONE, B + vx / kd * km, B + vy / kd * km, B + vz / kd * km);
        }

        public void RescaleToByte()
        {
            As = Flt2Byte(As);
            Rs = Flt2Byte(Rs);
            Gs = Flt2Byte(Gs);
            Bs = Flt2Byte(Bs);
        }

        public static Vector<float> Flt2Byte(Vector<float> vs)
        {
            return vs * K + I;
        }
        public static Vector<float> Byte2Flt(Vector<float> vs)
        {
            return vs * DIV;
        }

        static Vector<float> Sin(Vector<float> x)
        {
            var i4 = Vector.ConvertToInt32(x * MthVector.FFOUR);
            var i3 = i4 - MthVector.IONE;
            var i5 = i4 + MthVector.IONE;
            var i01 = Vector.BitwiseAnd(i3, MthVector.ITWO);
            var sign = Vector.ConvertToSingle(i01 - MthVector.IONE);
            var ks = Vector.ConvertToSingle(Vector.BitwiseAnd(i5, ISIX)) * MthVector.FQUART;
            var ki = Vector.ConvertToSingle(Vector.BitwiseAnd(i5, MthVector.IFOUR) - MthVector.ITWO) * VNEGHALF;
            x = x * sign + ks * ki;
            x *= MthVector.FPI2;
            var x3 = x * x * x;
            return x - x3 * SINK1 + x3 * x * x * SINK2 - x3 * x3 * x * SINK3;
        }
        static Vector<float> ACos(Vector<float> vs)
        {
            var x = Vector.Abs(vs);
            var xx = vs * vs;
            var r = (ACOSK1 + x * ACOSK2 + xx * ACOSK3 + xx * x * ACOSK4) * Vector.SquareRoot(MthVector.FONE - x);
            return Vector.ConditionalSelect(Vector.GreaterThanOrEqual(vs, MthVector.FZERO), r, MthVector.FPI - r);
        }
    }
}
