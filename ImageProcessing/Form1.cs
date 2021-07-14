using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Numerics;
using KKLib;

namespace ImageProcessing
{
    public partial class Form1 : Form
    {
        Bitmap bmpB;
        Graphics grB;
        Bitmap bmpBM;
        Graphics grBM;
        Bitmap bmpL;
        Graphics grL;
        Bitmap bmpLM;
        Graphics grLM;
        List<Pair<Pin>> movingPins = new List<Pair<Pin>>();
        Region movingLinesReg;
        Region connReg;

        List<IBox> boxes = new List<IBox>();
        List<BoxResult> boxsRes = new List<BoxResult>();
        List<Static> boxsStatic = new List<Static>();
        List<Pair<Pin>> connections = new List<Pair<Pin>>();

        OpenFileDialog ofd = new OpenFileDialog();
        SaveFileDialog sfd = new SaveFileDialog();
        OpenFileDialog ofds = new OpenFileDialog();
        SaveFileDialog sfds = new SaveFileDialog();
        FolderBrowserDialog fbd = new FolderBrowserDialog();

        Pen penConnections = new Pen(Color.FromArgb(36, 185, 234), 3);
        Pen penDrag = new Pen(Color.FromArgb(13, 98, 125), 3);
        Pen penWiden = new Pen(Color.FromArgb(36, 185, 234), 4.5f);

        Point Start = new Point(200, 200);

        public static Form1 Frm { get; private set; }

        public Form1()
        {
            System.Globalization.CultureInfo cul;
            if (Properties.Settings.Default.UserLang == "pl") cul = new System.Globalization.CultureInfo("pl");
            else cul = new System.Globalization.CultureInfo("en");
            System.Threading.Thread.CurrentThread.CurrentCulture = cul;
            System.Threading.Thread.CurrentThread.CurrentUICulture = cul;

            InitializeComponent();

            Frm = this;

            pictureBox1.Controls.Add(pictureBox2);
            pictureBox2.Controls.Add(pictureBox3);
            pictureBox3.Controls.Add(pictureBox4);
            pictureBox1.BackColor = Color.White;
            pictureBox2.BackColor = Color.Transparent;
            pictureBox2.Location = new Point();
            pictureBox3.BackColor = Color.Transparent;
            pictureBox3.Location = new Point();
            pictureBox4.BackColor = Color.Transparent;
            pictureBox4.Location = new Point();

            InitPBox();

            movingLinesReg = new Region();
            connReg = new Region();
            penConnections.StartCap = LineCap.Round;
            penConnections.EndCap = LineCap.Round;
            penDrag.StartCap = LineCap.Round;
            penDrag.EndCap = LineCap.Round;

            ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;";
            sfd.Filter = "Png|*.png;";
            ofds.Filter = "ImageProcessingSave|*.ips";
            sfds.Filter = "ImageProcessingSave|*.ips";

            FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Redraw();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        public void Execute()
        {
            foreach (var br in boxsRes)
            {
                br.Preview();
                RedrawBox(br);
            }
            for (int q = 0; q < boxsStatic.Count; q++)
            {
                var bis = boxsStatic[q];
                var nsrc = bis.Convert();
                if (nsrc != null)
                {
                    var pos = boxes.IndexOf(bis);
                    boxes[pos] = nsrc;
                    RemoveConnsFromBox(bis);
                    boxsStatic.RemoveAt(q--);
                    bis.Close();
                }
            }
        }
        void Redraw()
        {
            grB.Clear(Color.Transparent);
            grBM.Clear(Color.Transparent);
            grL.Clear(Color.Transparent);
            grLM.Clear(Color.Transparent);

            foreach (var box in boxes)
            {
                box.ClearDraw(grB);
            }

            foreach (var conn in connections)
            {
                var pa = conn.Item1.Box.Rect.Location + (Size)conn.Item1.P;
                var pb = conn.Item2.Box.Rect.Location + (Size)conn.Item2.P;
                DrawLine(grL, penConnections, pa, pb);
            }

            pictureBox1.Refresh();
        }
        void RedrawBox(IBox box)
        {
            box.ClearDraw(grB);
            pictureBox1.Invalidate(box.Rect);
            pictureBox1.Update();
        }
        void RedrawStartMoving(IBox active)
        {
            RedrawStart(active);
            active.ClearDraw(grBM);

            movingPins.Clear();
            foreach (var conn in connections)
            {
                if (conn.Item1.Box == active || conn.Item2.Box == active) movingPins.Add(conn);
            }

            RedrawUpdateMoving(active, active.Rect.Location, active.Rect.Location);
        }
        void RedrawStartConn()
        {
            RedrawStart();
        }
        void RedrawStart(IBox active = null)
        {
            grL.Clear(Color.Transparent);
            RedrawLines(grL, active);
            grB.Clear(Color.White);
            RedrawBoxes(grB, active);
        }
        void RedrawUpdateMoving(IBox active, Point oldPos, Point newPos)
        {
            var siz = new Size(active.Rect.Size.Width + 1, active.Rect.Size.Height + 1);
            var oldRect = new Rectangle(oldPos, siz);
            var newRect = new Rectangle(newPos, siz);
            
            grBM.Clip = new Region(oldRect);
            grBM.Clear(Color.Transparent);
            grBM.Clip = new Region(pictureBox1.DisplayRectangle);
            active.ClearDraw(grBM);
            pictureBox1.Invalidate(oldRect);
            pictureBox1.Invalidate(newRect);

            grLM.Clip = movingLinesReg;
            grLM.Clear(Color.Transparent);
            grLM.Clip = new Region(pictureBox1.DisplayRectangle);
            pictureBox1.Invalidate(movingLinesReg);

            var path = new GraphicsPath();
            foreach (var item in movingPins)
            {
                var pa = item.Item1.GP;
                var pb = item.Item2.GP;
                DrawLine(grLM, penConnections, pa, pb);
                AddLine(path, penConnections, pa, pb);
            }

            var todisp = movingLinesReg;
            path.Flatten();
            path.Widen(penWiden);
            movingLinesReg = new Region(path);
            todisp.Dispose();

            pictureBox1.Invalidate(movingLinesReg);
            pictureBox1.Update();
        }
        void RedrawUpdateConn(Point pa, Point pb)
        {
            grLM.Clip = connReg;
            grLM.Clear(Color.Transparent);
            grLM.Clip = new Region(pictureBox1.DisplayRectangle);
            pictureBox1.Invalidate(connReg);

            var gp = new GraphicsPath();
            AddLine(gp, penDrag, pa, pb);
            gp.Flatten();
            gp.Widen(penWiden);
            connReg = new Region(gp);

            DrawLine(grLM, penDrag, pa, pb);

            pictureBox1.Invalidate(connReg);
            pictureBox1.Update();
        }

        void RedrawBoxes(Graphics gra, IBox active = null)
        {
            foreach (var box in boxes)
            {
                if (box != active)
                {
                    box.ClearDraw(gra);
                }
            }
        }
        void RedrawLines(Graphics gra, IBox active = null)
        {
            foreach (var conn in connections)
            {
                var ba = conn.Item1.Box;
                if (ba == active) continue;
                var bb = conn.Item2.Box;
                if (bb == active) continue;
                var pa = ba.Rect.Location + (Size)conn.Item1.P;
                var pb = bb.Rect.Location + (Size)conn.Item2.P;
                DrawLine(gra, penConnections, pa, pb);
            }
        }

        void DrawLine(Graphics gr, Pen pen, PointF pa, PointF pb)
        {
            var ddir = DirectionDist(pa, pb);
            gr.DrawBezier(pen, pa, new PointF(pa.X + ddir, pa.Y), new PointF(pb.X - ddir, pb.Y), pb);
        }
        void AddLine(GraphicsPath path, Pen pen, PointF pa, PointF pb)
        {
            var ddir = DirectionDist(pa, pb);
            path.AddBezier(pa, new PointF(pa.X + ddir, pa.Y), new PointF(pb.X - ddir, pb.Y), pb);
            path.CloseFigure();
            path.AddEllipse(pa.X - pen.Width, pa.Y - pen.Width, pen.Width * 2, pen.Width * 2);
            path.CloseFigure();
            path.AddEllipse(pb.X - pen.Width, pb.Y - pen.Width, pen.Width * 2, pen.Width * 2);
            path.CloseFigure();
        }
        float DirectionDist(PointF pa, PointF pb)
        {
            return Math.Abs(pb.X - pa.X) * 0.5f + Math.Min(Math.Abs(pb.Y - pa.Y) * 0.25f, 100);
        }
        void DrawLineStr8(Graphics gr, Pen pen, PointF pa, PointF pb)
        {
            gr.DrawLine(pen, pa, pb);
        }
        void AddLineSrt8(GraphicsPath path, Pen pen, PointF pa, PointF pb)
        {
            path.AddLine(pa, pb);
            path.AddEllipse(pa.X - pen.Width, pa.Y - pen.Width, pen.Width * 2, pen.Width * 2);
            path.AddEllipse(pb.X - pen.Width, pb.Y - pen.Width, pen.Width * 2, pen.Width * 2);
        }

        private void pictureBox1_SizeChanged(object sender, EventArgs e)
        {
            if (pictureBox1.Width > 0 && pictureBox1.Height > 0)
            {
                InitPBox();
                Redraw();
            }
        }
        void InitPBox()
        {
            if (grB != null)
            {
                grB.Dispose();
                grB = null;
                bmpB.Dispose();
                grBM.Dispose();
                bmpBM.Dispose();
                grL.Dispose();
                bmpL.Dispose();
                grLM.Dispose();
                bmpLM.Dispose();
            }

            bmpL = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            grL = Graphics.FromImage(bmpL);
            bmpLM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            grLM = Graphics.FromImage(bmpLM);
            bmpB = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            grB = Graphics.FromImage(bmpB);
            bmpBM = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            grBM = Graphics.FromImage(bmpBM);
            pictureBox1.Image = bmpB;
            pictureBox2.Image = bmpBM;
            pictureBox3.Image = bmpL;
            pictureBox4.Image = bmpLM;
            grL.SmoothingMode = SmoothingMode.AntiAlias;
            grLM.SmoothingMode = SmoothingMode.AntiAlias;
            grB.SmoothingMode = SmoothingMode.AntiAlias;
            grBM.SmoothingMode = SmoothingMode.AntiAlias;
        }

        Point? mclick;
        bool firstPinIsOutput;
        ActionResult res;
        IBox boxInUse;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (res == ActionResult.PickColor)
            {
                return;
            }
            var p = e.Location;
            mclick = p;
            for (int w = boxes.Count - 1; w >= 0; w--)
            {
                var box = boxes[w];
                var rect = box.Rect;
                if (rect.Contains(p))
                {
                    BringBoxToFront(w);

                    p = p - (Size)rect.Location;
                    if (p.Y < IBox.BarHeight)
                    {
                        if (p.Y < IBox.CloseBoxSize && p.X > rect.Width - IBox.CloseBoxSize)
                        {
                            res = ActionResult.Close;
                        }
                        else
                        {
                            res = ActionResult.Move;
                            RedrawStartMoving(box);
                        }
                    }
                    else if (p.X > + rect.Width - IBox.ResizeBoxSize && p.Y > + rect.Height - IBox.ResizeBoxSize) res = ActionResult.Resize;
                    else
                    {
                        var pin = box.GetPin(p);
                        if (pin == null)
                        {
                            res = box.MouseDown(p, e.Button);
                        }
                        else
                        {
                            res = ActionResult.DragLine;
                            mclick = pin.GP;
                            firstPinIsOutput = !pin.IsInput;
                            for (int q = 0; q < connections.Count; q++)
                            {
                                var conn = connections[q];
                                if (pin == conn.Item1 || pin == conn.Item2)
                                {//unplug
                                    Unplug(pin, conn, q);
                                    Execute();
                                    RedrawStartConn();
                                    pictureBox1_MouseMove(sender, e);
                                    return;
                                }
                            }
                            RedrawStartConn();
                        }
                    }
                    boxInUse = box;
                    break;
                }
            }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            switch (res)
            {
                case ActionResult.None:
                    break;
                case ActionResult.Close:
                    var p = e.Location - (Size)boxInUse.Rect.Location;
                    if (p.X > boxInUse.Rect.Width - IBox.CloseBoxSize && p.Y < IBox.CloseBoxSize && p.X < boxInUse.Rect.Width && p.Y > 0)
                    {
                        if (e.Button == MouseButtons.Right)
                        {
                            RemoveConnsFromBox(boxInUse);
                        }
                        else
                        {
                            if (IsConnectedToAny(boxInUse)) return;
                        }

                        RemoveBoxInUse();
                        Execute();
                        Redraw();
                    }
                    break;
                case ActionResult.Move:
                    TryHookBoxes();
                    Redraw();
                    break;
                case ActionResult.Resize:
                    break;
                case ActionResult.Internal:
                    var resu = boxInUse.MouseUp(e.Location - (Size)boxInUse.Rect.Location);
                    if (resu == ActionResult.Execute)
                    {
                        Execute();
                        Redraw();
                    }
                    else if (resu == ActionResult.PickColor)
                    {
                        res = ActionResult.PickColor;
                        Cursor = Cursors.Hand;
                        return;
                    }
                    break;
                case ActionResult.DragLine:
                    TryToPlug(e.Location);
                    Redraw();
                    break;
                case ActionResult.DragEnd:
                    break;
                case ActionResult.Execute:
                    Execute();
                    break;
                case ActionResult.PickColor:
                    if (e.Button == MouseButtons.Left)
                    {
                        var bc = boxInUse as BoxColor;
                        if (bc != null)
                        {
                            bc.SetColor(new RawColor(bmpB.GetPixel(e.X, e.Y)));
                            Execute();
                            Redraw();
                        }
                    }
                    Cursor = Cursors.Default;
                    break;
                default:
                    break;
            }
            res = ActionResult.None;
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            switch (res)
            {
                case ActionResult.Move:
                    var p = e.Location;
                    var oldp = boxInUse.Rect.Location;
                    boxInUse.Rect.X += p.X - mclick.Value.X;
                    boxInUse.Rect.Y += p.Y - mclick.Value.Y;
                    mclick = p;
                    RedrawUpdateMoving(boxInUse, oldp, boxInUse.Rect.Location);
                    break;
                case ActionResult.Resize:
                    break;
                case ActionResult.Internal:
                    boxInUse.MouseMove(e.Location - (Size)boxInUse.Rect.Location);
                    RedrawBox(boxInUse);
                    break;
                case ActionResult.DragLine:
                    if (firstPinIsOutput)
                    {
                        RedrawUpdateConn(mclick.Value, e.Location);
                    }
                    else
                    {
                        RedrawUpdateConn(e.Location, mclick.Value);
                    }
                    break;
                default:
                    break;
            }
        }

        void BringBoxToFront(int boxAt)
        {
            int last = boxes.Count - 1;
            var temp = boxes[last];
            boxes[last] = boxes[boxAt];
            boxes[boxAt] = temp;
        }
        void Unplug(Pin pin, Pair<Pin> conn, int connAt)
        {
            Pin pinStart;
            if (pin == conn.Item1) pinStart = conn.Item2;
            else pinStart = conn.Item1;
            boxInUse = pinStart.Box;
            mclick = pinStart.GP;
            firstPinIsOutput = !firstPinIsOutput;
            Unconnect(connAt);
        }
        void TryToPlug(Point loc)
        {
            foreach (var box in boxes)
            {
                var rect = box.Rect;
                if (rect.Contains(loc))
                {
                    if (box != boxInUse)
                    {
                        Pin left, right;
                        var a = box.GetPin(loc - (Size)rect.Location);
                        if (a != null)
                        {
                            var b = boxInUse.GetPin(mclick.Value - (Size)boxInUse.Rect.Location);
                            if (a.IsInput != b.IsInput)
                            {
                                if (a.IsInput)
                                {
                                    left = b;
                                    right = a;
                                }
                                else
                                {
                                    left = a;
                                    right = b;
                                }
                                if (right.Proc.InputTypes[right.Order] == left.Proc.OutputType)
                                {
                                    connections.Add(new Pair<Pin>(left, right));
                                    if (right.Proc.Inputs[right.Order] != null)
                                    {//unplug old
                                        for (int q = 0; q < connections.Count; q++)
                                        {
                                            if (right == connections[q].Item2)
                                            {
                                                connections.RemoveAt(q);
                                                break;
                                            }
                                        }
                                    }
                                    right.Proc.Inputs[right.Order] = left.Proc;
                                    Execute();
                                }
                            }
                        }
                    }
                    break;
                }
            }
        }
        bool IsConnectedToAny(IBox box)
        {
            foreach (var conn in connections)
            {
                if (conn.Item1.Box == box || conn.Item2.Box == box) return true;
            }
            return false;
        }
        void RemoveBoxInUse()
        {
            boxes.Remove(boxInUse);
            var br = boxInUse as BoxResult;
            if (br != null) boxsRes.Remove(br);
            var bs = boxInUse as Static;
            if (bs != null) boxsStatic.Remove(bs);
            boxInUse.Close();
        }

        void TryHookBoxes()
        {
            var pinsToUse = new HashSet<Pin>();
            var connsToAdd = new List<Pair<Pin>>();

            for (int q = 0; q < boxes.Count; q++)
            {
                var box = boxes[q];
                if (box != boxInUse && box.Rect.IntersectsWith(boxInUse.Rect))
                {
                    foreach (var pinsColumn in boxInUse.pins)
                    {
                        foreach (var pin in pinsColumn)
                        {
                            var hook = box.GetPin(boxInUse.Rect.Location + (Size)pin.P - (Size)box.Rect.Location);
                            if (hook != null)
                            {
                                connsToAdd.Add(new Pair<Pin>(pin, hook));
                                pinsToUse.Add(pin);
                                pinsToUse.Add(hook);
                                continue;
                            }
                        }
                    }
                }
            }

            foreach (var conn in connections)
            {
                if (pinsToUse.Contains(conn.Item2)) pinsToUse.Remove(conn.Item2);
            }

            var anyConnected = false;
            foreach (var item in connsToAdd)
            {
                if (pinsToUse.Contains(item.Item1) && pinsToUse.Contains(item.Item2))
                {
                    var a = item.Item1;
                    var b = item.Item2;
                    Pin left, right;
                    if (a.Order < 0 != b.Order < 0)
                    {
                        if (a.Order < 0)
                        {
                            left = a;
                            right = b;
                        }
                        else
                        {
                            left = b;
                            right = a;
                        }
                        if (right.Proc.InputTypes[right.Order] == left.Proc.OutputType)
                        {
                            connections.Add(new Pair<Pin>(left, right));
                            right.Proc.Inputs[right.Order] = left.Proc;
                            anyConnected = true;
                        }
                    }
                }
            }

            if (anyConnected) Execute();
        }
        void Unconnect(int connAt)
        {
            var conn = connections[connAt];
            conn.Item2.Proc.Inputs[conn.Item2.Order] = null;
            connections.RemoveAt(connAt);
        }
        void RemoveConnsFromBox(IBox box)
        {
            for (int q = 0; q < connections.Count; q++)
            {
                var conn = connections[q];
                if (conn.Item1.Box == box || conn.Item2.Box == box)
                {
                    Unconnect(q--);
                }
            }
        }

        void AddSource(string imgFile, Point start)
        {
            Bitmap img = null;
            try
            {
                img = new Bitmap(imgFile);
            }
            catch (Exception)
            {

            }
            if (img == null) return;
            AddSource(start, img);
        }
        void AddSource(Point start, Bitmap img)
        {
            if (boxsRes.Count == 0)
            {
                AddResult(new Point(Start.X + 400, Start.Y), img.Size);
            }
            boxes.Add(new BoxSource(start, img));
            Redraw();
        }
        void AddResult(Point pos, Size siz)
        {
            var br = new BoxResult(pos, new Bitmap(siz.Width, siz.Height));
            boxsRes.Add(br);
            boxes.Add(br);
        }
        void AddBoxAndRedraw(RegularProcessor proc)
        {
            AddBoxAndRedraw(proc.GenerateBox(Start));
        }
        void AddBoxAndRedraw(IBox box)
        {
            boxes.Add(box);
            Redraw();
        }
        void AddStaticAndRedraw(Static bis)
        {
            boxsStatic.Add(bis);
            boxes.Add(bis);
            Redraw();
        }

        //MENU
        //File
        private void newToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            boxes.Clear();
            boxsRes.Clear();
            boxsStatic.Clear();
            connections.Clear();
            Redraw();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofds.ShowDialog() == DialogResult.OK)
            {
                newToolStripMenuItem_Click_1(sender, e);
                AddSaved(ofds.FileName);
            }
        }
        private void addToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (ofds.ShowDialog() == DialogResult.OK)
            {
                AddSaved(ofds.FileName);
            }
        }
        void AddSaved(string path, Point? p0 = null)
        {
            var nboxes = new List<IBox>();
            var nconnections = new List<Pair<Pin>>();
            try
            {
                using (var imp = File.Open(path, FileMode.Open))
                {
                    var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    int bn = (int)bf.Deserialize(imp);
                    for (int q = 0; q < bn; q++)
                    {
                        nboxes.Add((IBox)bf.Deserialize(imp));
                    }

                    var pinPairs = (List<int>)bf.Deserialize(imp);
                    for (int q = 0; q < pinPairs.Count; q += 4)
                    {
                        var left = nboxes[pinPairs[q]].pins[1][pinPairs[q + 1]];
                        var right = nboxes[pinPairs[q + 2]].pins[0][pinPairs[q + 3]];
                        nconnections.Add(new Pair<Pin>(left, right));
                        right.Proc.Inputs[right.Order] = left.Proc;
                    }
                }
            }
            catch (Exception)
            {
                nboxes.Clear();
                nconnections.Clear();
            }
            if (p0 != null)
            {
                var left = 0;
                var top = 0;
                foreach (var item in nboxes)
                {
                    left = Math.Min(left, item.Rect.X);
                    top = Math.Min(top, item.Rect.Y);
                }

                var dx = p0.Value.X - left;
                var dy = p0.Value.Y - top;

                foreach (var item in nboxes)
                {
                    item.Rect.X += dx;
                    item.Rect.Y += dy;
                }
            }
            boxes.AddRange(nboxes);
            connections.AddRange(nconnections);
            Redraw();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (sfds.ShowDialog() == DialogResult.OK)
                {
                    var pinDets = new Dictionary<Pin, int[]>();
                    foreach (var con in connections)
                    {
                        if (!pinDets.ContainsKey(con.Item1)) pinDets.Add(con.Item1, null);
                        pinDets.Add(con.Item2, null);
                    }

                    using (var exp = File.Create(sfds.FileName))
                    {
                        var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                        int bn = 0;
                        foreach (var box in boxes)
                        {
                            if (!(box is BoxImg)) bn++;
                        }
                        bf.Serialize(exp, bn);

                        bn = 0;
                        foreach (var box in boxes)
                        {
                            if (box is BoxImg) continue;

                            bf.Serialize(exp, box);

                            foreach (var pins in box.pins)
                            {
                                int pn = 0;
                                foreach (var pin in pins)
                                {
                                    if (pinDets.ContainsKey(pin))
                                    {
                                        pinDets[pin] = new int[] { bn, pn };
                                    }
                                    pn++;
                                }
                            }
                            bn++;
                        }

                        var pinPairs = new List<int>(connections.Count * 4);
                        for (int q = 0; q < connections.Count; q++)
                        {
                            var con = connections[q];
                            var a = pinDets[con.Item1];
                            var b = pinDets[con.Item2];
                            if (a != null && b != null)
                            {
                                pinPairs.AddRange(a);
                                pinPairs.AddRange(b);
                            }
                        }
                        bf.Serialize(exp, pinPairs);
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }
        private void imageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AddSource(ofd.FileName, Start);
            }
        }
        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                TryAddFolder(fbd.SelectedPath);
            }
        }
        private void screenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var img = Clipboard.GetImage();
            if (img != null)
            {
                var bmp = new Bitmap(img.Width, img.Height);
                using (var gr = Graphics.FromImage(bmp))
                {
                    gr.DrawImageUnscaled(img, 0, 0);
                }
                AddSource(Start, bmp);
            }
        }
        void TryAddFolder(string folderPath)
        {
            try
            {
                var paths = new List<string>();
                var di = new DirectoryInfo(folderPath);
                foreach (var item in di.EnumerateFiles())
                {
                    var end = Various.FileExtension(item.Name);
                    if (end == "png" || end == "bmp" || end == "jpg" || end == "jpeg" || end == "gif")
                    {
                        paths.Add(item.FullName);
                    }
                }
                if (paths.Count > 0)
                {
                    boxes.Add(new FolderSource(paths.ToArray()).GenerateBox(Start));
                    Redraw();
                }
            }
            catch (Exception)
            {
                
            }
        }
        //Result
        private void resultBoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddResult(Start, new Size(100, 100));
            Redraw();
        }
        //Math
        private void invertToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Invert());
        }
        private void safeAddToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new SafeAdd());
        }
        private void safeSubtractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new SafeSubtract());
        }
        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Add());
        }
        private void subtractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Subtract());
        }
        private void multiplyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Multiply());
        }
        private void divideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Divide());
        }
        private void minToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Min());
        }
        private void maxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Max());
        }
        private void meanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Mean());
        }
        private void circularAddToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new CircularAdd());
        }
        private void mean3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Mean3());
        }
        private void squareRootToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new SqRoot());
        }
        private void sinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Sin());
        }
        private void aSinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new ASin());
        }
        private void slopeTangensToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new SlopeTan());
        }
        private void roundTo0Or1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new RoundTo01());
        }
        private void colorDistanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new ColDistance());
        }
        //Converter
        private void aRGBToColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new ARGBToColor());
        }
        private void rGBToColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new RGBToColor());
        }
        private void colorToARGBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxColToARGB(Start));
        }
        private void hSBToColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new HSBToColor());
        }
        private void colorToHSBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxColToHSB(Start));
        }
        private void hCBToColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new HCBToColor());
        }
        private void colorToHCBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxColToHCB(Start));
        }
        private void opaqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Opaque());
        }
        private void doubleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new ValueToGrayScale());
        }
        private void edgeColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new ColorEdge());
        }
        //Process
        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Merge());
        }
        private void paintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Paint());
        }
        private void trimValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Trim());
        }
        //Function
        private void adjustToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxAdjust(Start, new Adjust(9)));
        }
        private void intensifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Intensifier());
        }
        //Input
        private void valueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxValue(Start, new Value()));
        }
        private void randomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Rand());
        }
        private void Value0_10ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxValue(Start, new Value(), 10));
        }
        private void colorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxColor(Start, new SingleColor(), this));
        }
        //Other
        private void resizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Resizer());
        }
        private void tileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new Tile());
        }
        private void scriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddBoxAndRedraw(new BoxScript(Start, new ScriptOutVal()));
        }
        private void intoSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStaticAndRedraw(new BoxIntoSource(Start));
        }
        //Static
        private void blurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStaticAndRedraw(new Blur(Start));
        }
        private void nearBlurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddStaticAndRedraw(new NearBlur(Start));
        }
        //Program
        private void settingsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            new SettingsForm().Show();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().Show();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            var end = Various.FileExtension(file);
            if (end == "png" || end == "bmp" || end == "jpg" || end == "jpeg" || end == "gif")
            {
                AddSource(file, PointToClient(new Point(e.X, e.Y)) - (Size)pictureBox1.Location);
            }
            else
            {
                if (end == "ips")
                {
                    AddSaved(file, PointToClient(new Point(e.X, e.Y)) - (Size)pictureBox1.Location);
                }
                else
                {
                    TryAddFolder(file);
                }
            }
        }
    }
}
