using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace KKLib
{
    public class ImageBox : Form
    {
        public static void Show(Bitmap bmp, string caption = "")
        {
            var ret = new ImageBox(bmp, caption);

            ret.ShowDialog();
        }

        protected PictureBox picbox;

        protected ImageBox(Bitmap bmp, string caption = "")
        {
            //picbox
            picbox = new PictureBox();
            picbox.Width = bmp.Width;
            picbox.Height = bmp.Height;
            picbox.Image = bmp;

            //button
            var btnok = new Button();
            btnok.Width = 88;
            btnok.Height = 26;
            btnok.Location = new Point(bmp.Width / 2 - 44, bmp.Height + 12);
            btnok.Text = "OK";
            btnok.Click += btnok_Click;

            //form
            Width = bmp.Width + 16;
            Height = bmp.Height + 89;
            Text = caption;

            Controls.Add(picbox);
            Controls.Add(btnok);
        }

        protected void btnok_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
    
    [StructLayout(LayoutKind.Explicit)][Serializable]
    public struct RawColor : IEquatable<RawColor>
    {
        const double FULL = 255.99999999999997;
        const float FULLF = 255.999985f;
        const float BRIGHTK = 1.0f / 255 / 3;
        
        public RawColor(int Argb)
        {
            A = 0;
            R = 0;
            G = 0;
            B = 0;
            ARGB = Argb;
        }
        public RawColor(byte r, byte g, byte b)
        {
            ARGB = 0;
            A = 255;
            R = r;
            G = g;
            B = b;
        }
        public RawColor(byte b)
        {
            ARGB = 0;
            A = 255;
            R = b;
            G = b;
            B = b;
        }
        public RawColor(byte a, byte r, byte g, byte b)
        {
            ARGB = 0;
            A = a;
            R = r;
            G = g;
            B = b;
        }
        public RawColor(Color col) : this(col.A, col.R, col.G, col.B) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Hue">0-360</param>
        /// <param name="Saturation">0-1</param>
        /// <param name="Brightness">0-1</param>
        public RawColor(float Hue, float Saturation, float Brightness) : this(255, Hue, Saturation, Brightness) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">0-255</param>
        /// <param name="Hue">0-360</param>
        /// <param name="Saturation">0-1</param>
        /// <param name="Lightness">0-1</param>
        public RawColor(byte a, float Hue, float Saturation, float Lightness)
        {
            ARGB = 0;
            A = a;
            float C = (1 - Math.Abs(2 * Lightness - 1)) * Saturation;
            float h = Hue / 60.0f;
            float X = C * (1 - Math.Abs(h % 2 - 1));
            float m = Lightness - C * 0.5f;
            int q = (int)h;
            float r, g, b;
            switch (q)
            {
                case 0:
                    r = C;
                    g = X;
                    b = 0;
                    break;
                case 1:
                    r = X;
                    g = C;
                    b = 0;
                    break;
                case 2:
                    r = 0;
                    g = C;
                    b = X;
                    break;
                case 3:
                    r = 0;
                    g = X;
                    b = C;
                    break;
                case 4:
                    r = X;
                    g = 0;
                    b = C;
                    break;
                default:
                    r = C;
                    g = 0;
                    b = X;
                    break;
            }
            R = (byte)((r + m) * 255);
            G = (byte)((g + m) * 255);
            B = (byte)((b + m) * 255);
        }

        [FieldOffset(0)]
        public int ARGB;

        [FieldOffset(0)]
        public byte B;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte R;
        [FieldOffset(3)]
        public byte A;
        
        public Color ColorV
        {
            get { return Color.FromArgb(ARGB); }
            set { ARGB = value.ToArgb(); }
        }
        /// <summary>
        /// Returns Hue <0,360), Saturation <0,1> and Lightness <0,1>
        /// </summary>
        /// <returns></returns>
        public float[] HSL()
        {
            var ret = new float[3];
            float r = R / 255.0f;
            float g = G / 255.0f;
            float b = B / 255.0f;
            float cmax;
            float cmin;
            int mod;
            if (r > g)
            {
                cmax = r;
                cmin = g;
                mod = 0;
            }
            else
            {
                cmax = g;
                cmin = r;
                mod = 1;
            }
            if (b > cmax)
            {
                cmax = b;
                mod = 2;
            }
            if (b < cmin) cmin = b;

            float delta = cmax - cmin;
            float l2 = cmax + cmin;
            if (delta > 0)
            {
                float h;
                switch (mod)
                {
                    case 0:
                        h = ((g - b) / delta + 6) % 6;
                        break;
                    case 1:
                        h = ((b - r) / delta) + 2;
                        break;
                    default:
                        h = ((r - g) / delta) + 4;
                        break;
                }

                ret[0] = 60 * h;
                ret[1] = delta / (1 - Math.Abs(l2 - 1));
            }
            ret[2] = l2 * 0.5f;
            return ret;
        }
        public string ToHex()
        {
            var sb = new StringBuilder(8);
            var ha = ToHex(A);
            sb.Append(ha.Item1);
            sb.Append(ha.Item2);
            var hb = ToHex(B);
            sb.Append(hb.Item1);
            sb.Append(hb.Item2);
            var hg = ToHex(G);
            sb.Append(hg.Item1);
            sb.Append(hg.Item2);
            var hr = ToHex(R);
            sb.Append(hr.Item1);
            sb.Append(hr.Item2);
            return sb.ToString();
        }
        public float Brightness()
        {
            return (R + G + B) * BRIGHTK;
        }

        public override bool Equals(object obj)
        {
            if (obj is RawColor) return Equals((RawColor)obj);
            return false;
        }
        public bool Equals(RawColor other)
        {
            return ARGB == other.ARGB;
        }
        public override int GetHashCode()
        {
            return ARGB.GetHashCode();
        }
        public override string ToString()
        {
            var ret = "R:" + R.ToString() + " G:" + G.ToString() + " B:" + B.ToString();
            if (A != 255) ret += " A:" + A.ToString();
            return ret;
        }
        
        public static bool operator ==(RawColor a, RawColor b)
        {
            return a.ARGB == b.ARGB;
        }
        public static bool operator !=(RawColor a, RawColor b)
        {
            return a.ARGB != b.ARGB;
        }

        public static Pair<char> ToHex(byte b)
        {
            return new Pair<char>(hex[b & 15], hex[b >> 4]);
        }
        static readonly char[] hex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        static readonly float MaxRad = (float)(Math.Sqrt(6.0) / 3);

        const float LXX = (float)(2.0 / 3);
        const float LXYZ = (float)(-1.0 / 3);
        const float LYYZ = (float)(0.5773502691896257);
        const float FromDegToRad = (float)(Math.PI / 180);
        const float RADTODEG = (float)(180 / Math.PI);
        const float K01 = (float)(1.0 / 255);
        const float THIRD = (float)(1.0 / 255 / 3);
        const float ToC = (float)(0.5 / 255);
        public static RawColor FromHSB(float H, float S, float B)
        {
            var dx = (float)Math.Cos(FromDegToRad * H);
            var dy = Math.Sign(180 - H) * (float)Math.Sqrt(1 - dx * dx);
            dx *= MaxRad;
            dy = dy * MaxRad * LYYZ;
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
            return new RawColor((byte)(FULLF * (B + S * vx / kd * km)), (byte)(FULLF * (B + S * vy / kd * km)), (byte)(FULLF * (B + S * vz / kd * km)));
        }
        public static RawColor FromHCB(float H, float C, float B)
        {
            var dx = (float)Math.Cos(FromDegToRad * H);
            var dy = Math.Sign(180 - H) * (float)Math.Sqrt(1 - dx * dx);
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
            return new RawColor((byte)(FULLF * (B + vx / kd * km)), (byte)(FULLF * (B + vy / kd * km)), (byte)(FULLF * (B + vz / kd * km)));
        }
        public Tripl<float> HSB()
        {
            var a = B - G;
            var b = R - B;
            var c = G - R;

            var s2 = (float)Math.Sqrt((a * a + b * b + c * c) * 2);
            float H;
            if (s2 == 0) H = 0;
            else
            {
                H = (float)Math.Acos((b - c) / s2) * RADTODEG;
                if (a > 0) H = 360 - H;
            }

            var Bri = (R + G + B) * THIRD;

            var vx = R * K01 - Bri;
            var vy = G * K01 - Bri;
            var vz = B * K01 - Bri;

            var sx = (float)Math.Sign(vx);
            var sy = (float)Math.Sign(vy);
            var sz = (float)Math.Sign(vz);

            var maxx = (sx + 1) * 0.5f - sx * Bri;
            var maxy = (sy + 1) * 0.5f - sy * Bri;
            var maxz = (sz + 1) * 0.5f - sz * Bri;

            var kx = Math.Abs(vx) / maxx;
            var ky = Math.Abs(vy) / maxy;
            var kz = Math.Abs(vz) / maxz;

            var S = Math.Max(kx, Math.Max(ky, kz));

            return new Tripl<float>(H, S, Bri);
        }
        public Tripl<float> HCB()
        {
            var a = B - G;
            var b = R - B;
            var c = G - R;

            var s2 = (float)Math.Sqrt((a * a + b * b + c * c) * 2);
            float H;
            if (s2 == 0) H = 0;
            else
            {
                H = (float)Math.Acos((b - c) / s2) * RADTODEG;
                if (a > 0) H = 360 - H;
            }

            var Bri = (R + G + B) * THIRD;
            var C = s2 * ToC;
            
            return new Tripl<float>(H, C, Bri);
        }
    }

    public static class General
    {
        /// <summary>
        /// [Y][X]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static T[][] NewMatrix<T>(int height, int width)
        {
            var ret = new T[height][];
            for (int q = 0; q < ret.Length; q++)
            {
                ret[q] = new T[width];
            }
            return ret;
        }
        /// <summary>
        /// [Z][Y][X]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xdim"></param>
        /// <param name="ydim"></param>
        /// <param name="zdim"></param>
        /// <returns></returns>
        public static T[][][] NewMatrix<T>(int zdim, int ydim, int xdim )
        {
            var ret = new T[zdim][][];
            for (int q = 0; q < ret.Length; q++)
            {
                ret[q] = NewMatrix<T>(ydim, xdim);
            }
            return ret;
        }
    }

    public struct Pair<K> : IEquatable<Pair<K>>
    {
        public K Item1;
        public K Item2;

        public Pair(K I12)
        {
            Item1 = I12;
            Item2 = I12;
        }
        public Pair(K I1, K I2)
        {
            Item1 = I1;
            Item2 = I2;
        }

        public bool Equals(Pair<K> other)
        {
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2);
        }
        public override bool Equals(object obj)
        {
            if (obj is Pair<K>)
            {
                return Equals((Pair<K>)obj);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode();
        }
        public override string ToString()
        {
            return Item1.ToString() + " | " + Item2.ToString();
        }

        public IEnumerable<K> Enumerate()
        {
            yield return Item1;
            yield return Item2;
        }

        public static bool operator ==(Pair<K> a, Pair<K> b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Pair<K> a, Pair<K> b)
        {
            return !a.Equals(b);
        }
    }

    public struct Pair<K, V> : IEquatable<Pair<K, V>>
    {
        public K Item1;
        public V Item2;

        public Pair(K I1, V I2)
        {
            Item1 = I1;
            Item2 = I2;
        }

        public bool Equals(Pair<K, V> other)
        {
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2);
        }
        public override bool Equals(object obj)
        {
            if (obj is Pair<K>)
            {
                return Equals((Pair<K>)obj);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode();
        }
        public override string ToString()
        {
            return Item1.ToString() + " | " + Item2.ToString();
        }
    }

    public struct Tripl<K> : IEquatable<Tripl<K>>
    {
        public K Item1;
        public K Item2;
        public K Item3;

        public Tripl(K I123)
        {
            Item1 = I123;
            Item2 = I123;
            Item3 = I123;
        }
        public Tripl(K I1, K I2, K I3)
        {
            Item1 = I1;
            Item2 = I2;
            Item3 = I3;
        }

        public override bool Equals(object obj)
        {
            if (obj is Tripl<K>)
            {
                return Equals((Tripl<K>)obj);
            }
            return false;
        }
        public bool Equals(Tripl<K> other)
        {
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2) && Item3.Equals(other.Item3);
        }
        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
        }
        public override string ToString()
        {
            return Item1.ToString() + " | " + Item2.ToString() + " | " + Item3.ToString();
        }

        public static bool operator ==(Tripl<K> a, Tripl<K> b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Tripl<K> a, Tripl<K> b)
        {
            return !a.Equals(b);
        }
    }

    public class Pack<T, V>
    {
        public T Item1;
        public V Item2;

        public Pack() { }
        public Pack(T I1, V I2)
        {
            Item1 = I1;
            Item2 = I2;
        }
    }

    public static partial class Extentions
    {
        /// <summary>
        /// Puts last element in place n and deletes last place.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="n"></param>
        public static T FastRemove<T>(this List<T> list, int n)
        {
            int last = list.Count - 1;
            var ret = list[n];
            list[n] = list[last];
            list.RemoveAt(last);
            return ret;
        }
    }
}