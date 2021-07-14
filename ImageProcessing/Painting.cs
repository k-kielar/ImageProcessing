using System;
using System.Drawing;

namespace KKLib
{
    class Painting
    {
        public static Bitmap MeanColorSquare(Bitmap bmp, int rad)
        {
            var pxlr = new Pixeler(bmp);
            var ret = new Bitmap(pxlr.Width, pxlr.Height);
            var res = new Pixeler(ret);

            var sums = SummedTables(pxlr);
            var sumr = sums[0];
            var sumg = sums[1];
            var sumb = sums[2];
            
            var wx = pxlr.Width;
            var qx = pxlr.Height;

            int q = 0;
            int qc = 1;
            int qsub = 0;
            int qadd = rad + 1;
            int qm = qx - rad;
            int wm = wx - rad;
            while (true)
            {
                for (; q < qc; q++)
                {

                    var t = q - qsub;
                    var b = q + qadd;
                    var sumrt = sumr[t];
                    var sumrb = sumr[b];
                    var sumgt = sumg[t];
                    var sumgb = sumg[b];
                    var sumbt = sumb[t];
                    var sumbb = sumb[b];

                    int wsub = 0;
                    int wadd = rad + 1;
                    int w = 0;
                    int wc = 1;
                    var s = (qadd + qsub) * (wadd + wsub);
                    var kk = 1.0 / s;
                    while (true)
                    {
                        for (; w < wc; w++)
                        {

                            var l = w - wsub;
                            var r = w + wadd;
                            var R = sumrt[l] - sumrt[r] - sumrb[l] + sumrb[r];
                            var G = sumgt[l] - sumgt[r] - sumgb[l] + sumgb[r];
                            var B = sumbt[l] - sumbt[r] - sumbb[l] + sumbb[r];

                            res.SetPixel(w, q, new RawColor((byte)(R * kk), (byte)(G * kk), (byte)(B * kk)));
                        }
                        if (w >= wx) break;
                        if (w < rad)
                        {
                            wc = w + 1;
                            wsub = w;
                        }
                        else
                        {
                            wc = wx - rad;
                            wsub = rad;
                        }
                        if (w >= wm)
                        {
                            wc = w + 1;
                            wadd = wx - w;
                        }
                        s = (qadd + qsub) * (wadd + wsub);
                        kk = 1.0 / s;
                    }

                }
                if (q >= qx) break;
                if (q < rad)
                {
                    qc = q + 1;
                    qsub = q;
                }
                else
                {
                    qc = qx - rad;
                    qsub = rad;
                }
                if (q >= qm)
                {
                    qc = q + 1;
                    qadd = qx - q;
                }
            }

            pxlr.Dispose();
            res.Dispose();
            return ret;
        }
        public static Bitmap MeanColorGaussSquare(Bitmap bmp, int rad, int step)
        {
            var pxlr = new Pixeler(bmp);
            var ret = new Bitmap(pxlr.Width, pxlr.Height);
            var res = new Pixeler(ret);

            var sums = SummedTables(pxlr);
            var sumr = sums[0];
            var sumg = sums[1];
            var sumb = sums[2];

            var wx = pxlr.Width;
            var qx = pxlr.Height;
            
            for (int q = 0; q < qx; q++)
            {
                for (int w = 0; w < wx; w++)
                {
                    var s = 0;
                    var R = 0;
                    var G = 0;
                    var B = 0;
                    for (int d = rad; d >= 0; d -= step)
                    {
                        var t = Math.Max(q - d, 0);
                        var b = Math.Min(q + d + 1, qx);
                        var l = Math.Max(w - d, 0);
                        var r = Math.Min(w + d + 1, wx);
                        s += (b - t) * (r - l);
                        R += sumr[t][l] - sumr[t][r] - sumr[b][l] + sumr[b][r];
                        G += sumg[t][l] - sumg[t][r] - sumg[b][l] + sumg[b][r];
                        B += sumb[t][l] - sumb[t][r] - sumb[b][l] + sumb[b][r];
                    }

                    res.SetPixel(w, q, new RawColor((byte)(R / s), (byte)(G / s), (byte)(B / s)));
                }
            }

            pxlr.Dispose();
            res.Dispose();
            return ret;
        }

        public static int[][][] SummedTables(Pixeler pxlr)
        {
            var wid = pxlr.Width + 1;
            var hei = pxlr.Height + 1;
            var rs = General.NewMatrix<int>(hei, wid);
            var gs = General.NewMatrix<int>(hei, wid);
            var bs = General.NewMatrix<int>(hei, wid);

            var rup = rs[0];
            var gup = gs[0];
            var bup = bs[0];
            for (int q = 1; q < hei; q++)
            {
                int r = 0, g = 0, b = 0;
                var rrow = rs[q];
                var grow = gs[q];
                var brow = bs[q];
                var qm1 = q - 1;
                for (int w = 1; w < wid; w++)
                {
                    var p = pxlr.GetPixel(w - 1, qm1);
                    r += p.R;
                    g += p.G;
                    b += p.B;
                    rrow[w] = rup[w] + r;
                    grow[w] = gup[w] + g;
                    brow[w] = bup[w] + b;
                }
                rup = rrow;
                gup = grow;
                bup = brow;
            }

            return new int[][][] { rs, gs, bs };
        }
        
        public static Bitmap ChessBoard(int width, int height, int sqSize, Color c1, Color c2)
        {
            var ret = new Bitmap(width, height);
            using (var gr = Graphics.FromImage(ret))
            {
                gr.Clear(c1);
                var brPrev = new SolidBrush(c2);
                for (int y = 0, q = 0; y < ret.Height; y += sqSize, q++)
                {
                    for (int x = 0, w = 0; x < ret.Width; x += sqSize, w++)
                    {
                        if ((q & 1) != (w & 1))
                        {
                            gr.FillRectangle(brPrev, x, y, sqSize, sqSize);
                        }
                    }
                }
            }
            return ret;
        }
    }
}