namespace WolfensteinInfinite.DataFormats
{
    public class VSWAPHeader(ushort chunkCount, ushort spriteStart, ushort soundStart, uint[] chunkOffsets, ushort[] chunkLengths)
    {
        public ushort ChunkCount { get; init; } = chunkCount;
        public ushort SpriteStart { get; init; } = spriteStart;
        public ushort SoundStart { get; init; } = soundStart;

        public uint[] ChunkOffsets { get; init; } = chunkOffsets;
        public ushort[] ChunkLengths { get; init; } = chunkLengths;

    }
}