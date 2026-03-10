namespace WolfensteinInfinite.DataFormats
{
    public class PictureAtlas((ushort width, ushort Height)[] picTable, int[] chunkOffsets)
    {
        //havent found any source indicating how to comput these but keeping seperate for now.
        public int PicStart { get; init; } = 3;
        public int PicEnd { get; init; } = 134;
        public int PicCount { get; init; } = 132; //??? shouldnt this be PicEnd - PicStart

        public (ushort Width, ushort Height)[] PicTable { get; init; } = picTable;
        public int[] ChunkOffsets { get; init; } = chunkOffsets;
    }
}