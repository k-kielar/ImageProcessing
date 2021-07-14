using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using KKLib;

namespace ImageProcessing
{
    enum ActionResult
    {
        None,
        Close,
        Move,
        Resize,
        Internal,
        DragLine,
        DragEnd,
        Execute,
        PickColor
    }

    [StructLayout(LayoutKind.Explicit)]
    struct V1
    {
        [FieldOffset(0)]
        public uint Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    [StructLayout(LayoutKind.Explicit)]
    struct V2
    {
        [FieldOffset(0)]
        public F2 Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    unsafe struct F2
    {
        public fixed uint Fs[2];
    }
    [StructLayout(LayoutKind.Explicit)]
    struct V4
    {
        [FieldOffset(0)]
        public F4 Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    unsafe struct F4
    {
        public fixed uint Fs[4];
    }
    [StructLayout(LayoutKind.Explicit)]
    struct V8
    {
        [FieldOffset(0)]
        public F8 Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    unsafe struct F8
    {
        public fixed uint Fs[8];
    }
    [StructLayout(LayoutKind.Explicit)]
    struct V16
    {
        [FieldOffset(0)]
        public F16 Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    unsafe struct F16
    {
        public fixed uint Fs[16];
    }
    [StructLayout(LayoutKind.Explicit)]
    struct V32
    {
        [FieldOffset(0)]
        public F32 Fs;
        [FieldOffset(0)]
        public Vector<uint> V;
    }
    unsafe struct F32
    {
        public fixed uint Fs[32];
    }

    unsafe class PixelerVectoric : Pixeler
    {
        readonly int* ptr0;

        public PixelerVectoric(Bitmap BMP) : base(BMP)
        {
            ptr0 = (int*)raw.Scan0;
        }

        public void SetVector(int x, int y, uint col)
        {
            ((uint*)(ptr0 + y * stride + x))[0] = col;
        }
        public void SetVector(int x, int y, F2 col)
        {
            ((F2*)(ptr0 + y * stride + x))[0] = col;
        }
        public void SetVector(int x, int y, F4 col)
        {
            ((F4*)(ptr0 + y * stride + x))[0] = col;
        }
        public void SetVector(int x, int y, F8 col)
        {
            ((F8*)(ptr0 + y * stride + x))[0] = col;
        }
        public void SetVector(int x, int y, F16 col)
        {
            ((F16*)(ptr0 + y * stride + x))[0] = col;
        }
        public void SetVector(int x, int y, F32 col)
        {
            ((F32*)(ptr0 + y * stride + x))[0] = col;
        }
        public uint GetVector1(int x, int y)
        {
            return ((uint*)(ptr0 + y * stride + x))[0];
        }
        public F2 GetVector2(int x, int y)
        {
            return ((F2*)(ptr0 + y * stride + x))[0];
        }
        public F4 GetVector4(int x, int y)
        {
            return ((F4*)(ptr0 + y * stride + x))[0];
        }
        public F8 GetVector8(int x, int y)
        {
            return ((F8*)(ptr0 + y * stride + x))[0];
        }
        public F16 GetVector16(int x, int y)
        {
            return ((F16*)(ptr0 + y * stride + x))[0];
        }
        public F32 GetVector32(int x, int y)
        {
            return ((F32*)(ptr0 + y * stride + x))[0];
        }
    }

    public class Res
    {
        public static readonly System.Resources.ResourceManager Mngr = new System.Resources.ResourceManager("ImageProcessing.Lang", typeof(Form1).Assembly);
    }

    class Branch<T>
    {
        public readonly T Element;
        public readonly Branch<T> Parent;

        public Branch(T element, Branch<T> parent)
        {
            Element = element;
            Parent = parent;
        }

        public bool Exist(T el)
        {
            if (el.Equals(Element)) return true;
            if (Parent == null) return false;
            return Parent.Exist(el);
        }
    }

    class Various
    {
        public static string FileExtension(string path)
        {
            System.IO.Path.GetExtension(path);
            var dot = path.LastIndexOf('.');
            var end = "";
            if (dot >= 0)
            {
                end = path.Substring(dot + 1).ToLower();
            }
            return end;
        }
    }
}
