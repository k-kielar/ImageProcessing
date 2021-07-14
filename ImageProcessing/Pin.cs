using System;
using System.Drawing;

namespace ImageProcessing
{
    [Serializable]
    class Pin
    {
        public readonly Point P;
        [NonSerialized]
        public IBox Box;
        public readonly int Order;
        public readonly IProcessor Proc;
        public readonly string Label;
        public bool IsInput
        {
            get { return Order >= 0; }
        }
        public Point GP
        {
            get
            {
                return Box.Rect.Location + (Size)P;
            }
        }
        public Type Tp
        {
            get
            {
                if (Order < 0)
                {
                    return Proc.OutputType;
                }
                else
                {
                    return Proc.InputTypes[Order];
                }
            }
        }

        public Pin(Point p, IBox box, int order, IProcessor proc, string label = null)
        {
            P = p;
            Box = box;
            Order = order;
            Proc = proc;
            Label = label;
        }
    }
}
