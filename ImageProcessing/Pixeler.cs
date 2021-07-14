using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace KKLib
{
    public unsafe partial class Pixeler : IDisposable
    {
        public readonly int Width;
        public readonly int Height;

        Bitmap bmp;
        protected BitmapData raw;
        int* ptri;
        protected int stride;

        public Pixeler(Bitmap BMP)
        {
            bmp = BMP;
            Width = bmp.Width;
            Height = bmp.Height;
            raw = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            ptri = (int*)raw.Scan0;
            stride = raw.Stride >> 2;
        }

        public void SetPixel(int x, int y, RawColor col)
        {
            ptri[y * stride + x] = col.ARGB;
        }
        public RawColor GetPixel(int x, int y)
        {
            return new RawColor(ptri[y * stride + x]);
        }

        public void Clear(RawColor col)
        {
            for (int q = 0; q < Height; q++)
            {
                for (int w = 0; w < Width; w++)
                {
                    SetPixel(w, q, col);
                }
            }
        }

        public void Dispose()
        {
            bmp.UnlockBits(raw);
        }
    }
}
