using System.IO;

namespace WolfensteinInfinite.DataFormats
{
    public class GameVersion(string name, string extenstion, RGBA8[] pallet, int picStart, int picEnd, int sounds, int music, int digiSounds)
    {
        public static GameVersion[] KnownVersion => [
            new GameVersion("Demo", "WL1", Pallets.Wolfenstein3D, 3, 134, 87, 27, 46),
            new GameVersion("Wolfenstein3D", "WL6", Pallets.Wolfenstein3D, 3, 134, 87, 27, 46),
            new GameVersion("SpearOfDestiny", "SOD", Pallets.SpearOfDestiny, 3, 80, 81, 24, 82),
            new GameVersion("ReturnToDanger", "SD2", Pallets.SpearOfDestiny, 3, 80, 81, 24, 82),
            new GameVersion("UltimateChallenge", "SD3", Pallets.SpearOfDestiny, 3, 80, 81, 24, 82)];

        private const string AUDIOHED_FILE = "AUDIOHED";        // File containing the audio offsets
        private const string AUDIOT_FILE = "AUDIOT";            // File containing the audio chunks
        private const string VSWAP_FILE = "VSWAP";              // File containing the bitmaps and digitised sound effects
        private const string ATLAS_FILE = "MAPHEAD";            // File containing the levels atlas.
        private const string MAPS_FILE = "GAMEMAPS";            // File containing the levels
        private const string TREE_FILE = "VGADICT";             // File containing the Huffman tree.        
        private const string HEAD_FILE = "VGAHEAD";             // File containing the picture offsets.
        private const string GRAPH_FILE = "VGAGRAPH";           // File containing the pictures.
        public int PictureStarts { get; init; } = picStart;
        public int PictureEnds { get; init; } = picEnd;
        public int PictureCounts => (PictureEnds - PictureStarts) + 1;

        public int NumberOfSounds { get; init; } = sounds;
        public int NumberOfMusic { get; init; } = music;
        public int NumberOfDigiSounds { get; init; } = digiSounds;

        public RGBA8[] Pallet { get; init; } = pallet;
        public string Name { get; init; } = name;
        public string Extension { get; init; } = extenstion;
        public string AudioOffsets => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(AUDIOHED_FILE, Extension));
        public string AudioChunks => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(AUDIOT_FILE, Extension));
        public string VideoAudio => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(VSWAP_FILE, Extension));
        public string LevelAtlas => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(ATLAS_FILE, Extension));
        public string LevelMaps => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(MAPS_FILE, Extension));
        public string VGAHuffman => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(TREE_FILE, Extension));
        public string VGAOffsets => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(HEAD_FILE, Extension));
        public string VGATextures => FileHelpers.Shared.GetDataFilePath(Path.ChangeExtension(GRAPH_FILE, Extension));
        public string[] FileList => new string[] { AudioOffsets, AudioChunks, VideoAudio, LevelAtlas, LevelMaps, VGAHuffman, VGAOffsets, VGATextures };
        public bool IsAvailable => FileList.All(p => File.Exists(p));
    }
}