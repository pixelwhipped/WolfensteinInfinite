//Clean
namespace WolfensteinInfinite.DataFormats
{
    /// <summary>
    /// Structure describing how to find the individual levels in the GAMEAPS file.
    /// </summary>  
    public class LevelAtlas(short topRLEW, short bottomRLEW, int[] offsets)
    {
        public const int MAX_LEVELS = 100;      // Maximum number of levels in the game.
        public const int EPISODE_LEVELS = 10;   // Number of levels per episode.

        public readonly short TopRLEW = topRLEW;          // Signature for RLEW decompression.
        public readonly short BottomRLEW = bottomRLEW;          // Signature for RLEW decompression.
        public readonly int[] HeaderOffsets = offsets;    // Offsets to the individual level headers.
        public int Levels => HeaderOffsets.Length;
    };
}