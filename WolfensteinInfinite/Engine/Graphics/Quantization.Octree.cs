//Clean
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite.Engine.Graphics
{
    public static partial class Quantization
    {
        public static (byte[] pixels, byte[] pallet) Quantize8BitOctree(byte[] pixels, byte[] pallet, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = new RGBA8[pixels.Length];
            ref byte buffer = ref MemoryMarshal.GetReference(memory);
            var off = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = Unsafe.Add(ref buffer, i) * 3;
                pixelsrgba[off++] = new RGBA8
                {
                    R = pallet[c],
                    G = pallet[c + 1],
                    B = pallet[c + 2],
                    A = 255
                };
            }
            return Quantize32BitOctree(pixelsrgba.ToArray(), colourCount);
        }
        public static (byte[] pixels, byte[] pallet) Quantize32BitOctree(byte[] pixels, int colourCount)
        {
            Span<byte> memory = pixels;
            Span<RGBA8> pixelsrgba = MemoryMarshal.Cast<byte, RGBA8>(memory);
            return Quantize32BitOctree(pixelsrgba.ToArray(), colourCount);
        }
        public static (byte[] pixels, byte[] pallet) Quantize32BitOctree(RGBA8[] pixels, int colourCount)
        {
            var quantizer = new PaletteQuantizer();
            Span<RGBA8> px = pixels;
            for (var i = 0; i < px.Length; i++) quantizer.AddColour(px[i]);


            quantizer.Quantize(colourCount);
            var ret = new byte[pixels.Length];
            var rgbPalllet = new List<RGBA8>();
            var pallet = new List<byte>();

            for (var i = 0; i < px.Length; i++)
            {
                var color = quantizer.GetQuantizedColour(px[i]);
                var index = rgbPalllet.IndexOf(color);
                if (index < 0)
                {
                    rgbPalllet.Add(color);
                    var p = rgbPalllet[^1];
                    pallet.Add(p.R);
                    pallet.Add(p.G);
                    pallet.Add(p.B);
                }
                ret[i] = (byte)index;
            }
            var pal = pallet.ToArray();
            Array.Resize<byte>(ref pal, colourCount*3);
            return (ret, pal);
        }

        private class PaletteQuantizer
        {
            private readonly Node Root;
            private readonly Dictionary<int, List<Node>> levelNodes;
            public PaletteQuantizer()
            {
                Root = new Node(this);
                levelNodes = [];
                for (int i = 0; i < 8; i++) levelNodes[i] = [];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddColour(RGBA8 colour) => Root.AddColour(colour, 0);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddLevelNode(Node node, int level) => levelNodes[level].Add(node);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RGBA8 GetQuantizedColour(RGBA8 colour) => Root.GetColour(colour, 0);

            public void Quantize(int colourCount)
            {
                var nodesToRemove = levelNodes[7].Count - colourCount;
                int level = 6;
                var toBreak = false;
                while (level >= 0 && nodesToRemove > 0)
                {

                    var leaves = levelNodes[level]
                        .Where(n => n.ChildrenCount - 1 <= nodesToRemove)
                        .OrderBy(n => n.ChildrenCount);
                    foreach (var leaf in leaves)
                    {
                        if (leaf.ChildrenCount > nodesToRemove)
                        {
                            toBreak = true;
                            continue;
                        }
                        nodesToRemove -= (leaf.ChildrenCount - 1);
                        leaf.Merge();
                        if (nodesToRemove <= 0)
                        {
                            break;
                        }
                    }
                    levelNodes.Remove(level + 1);
                    level--;
                    if (toBreak)
                    {
                        break;
                    }
                }
            }
        }

        private class Node(PaletteQuantizer parent)
        {
            private readonly PaletteQuantizer Parent = parent;
            private Node[] Children = new Node[8];
            private RGBA8 Color { get; set; }
            private int Count { get; set; }

            public int ChildrenCount => Children.Count(c => c != null);

            public void AddColour(RGBA8 colour, int level)
            {
                if (level < 8)
                {
                    var index = GetIndex(colour, level);
                    if (Children[index] == null)
                    {
                        var newNode = new Node(Parent);
                        Children[index] = newNode;
                        Parent.AddLevelNode(newNode, level);
                    }
                    Children[index].AddColour(colour, level + 1);
                }
                else
                {
                    Color = colour;
                    Count++;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RGBA8 GetColour(RGBA8 colour, int level) => (ChildrenCount == 0) ? Color : Children[GetIndex(colour, level)].GetColour(colour, level + 1);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static byte GetIndex(RGBA8 colour, int level)
            {
                var shift = 7 - level;
                return (byte)(
                    ((colour.R >> shift) & 1) << 2 |
                    ((colour.G >> shift) & 1) << 1 |
                    ((colour.B >> shift) & 1)
                );
            }
            public void Merge()
            {
                int totalR = 0, totalG = 0, totalB = 0, totalCount = 0;
                Span<Node> c = Children;
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == null) continue;
                    var (color, count) = (c[i].Color, c[i].Count);
                    totalR += color.R * count;
                    totalG += color.G * count;
                    totalB += color.B * count;
                    totalCount += count;
                }
                Count = totalCount;
                Children = new Node[8];
                /*if (totalCount == 0)
                {
                    Color = new RGBA8 { R = 0, G = 0, B = 0, A = 255 };
                    return;
                }*/

                Color = new RGBA8
                {
                    R = (byte)(totalR / totalCount),
                    G = (byte)(totalG / totalCount),
                    B = (byte)(totalB / totalCount),
                    A = 255
                };
                
            }
        }
    }
}
