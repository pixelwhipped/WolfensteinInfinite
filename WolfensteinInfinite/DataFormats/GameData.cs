//Render map not used, for debug and initial testing only.  commented use out in ReadMapData
//Clean
using SFML.Audio;
using System.IO;
using WolfensteinInfinite.DataFormats.Convert;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.DataFormats
{
    public class GameData
    {
        private const int HUFFMAN_TREE_NODE_COUNT = 255;
        private const int ROOT = 254;
        // Audio constants
        public const int BASE_TIMER = 1193181;
        public const int PCS_RATE = 140;
        public const int PCS_VOLUME = 20;

        public GameVersion Version { get; init; }
        public LevelAtlas LevelAtlas { get; init; }
        public int Levels => LevelAtlas.Levels;
        public LevelHeader[] LevelHeaders { get; init; }
        public MapData[] Maps { get; init; }
        public VSWAPHeader VSWAPHeader { get; init; }
        public PictureAtlas PictureAtlas { get; init; }
        public byte[] AudioOffsetsData { get; init; }
        public byte[] AudioChunksData { get; init; }
        public byte[] VideoAudioData { get; init; }
        public byte[] LevelAtlasData { get; init; }
        public byte[] LevelMapsData { get; init; }
        public byte[] VGAHuffmanData { get; init; }
        public byte[] VGAOffsetsData { get; init; }
        public byte[] VGATexturesData { get; init; }

        public Dictionary<int, Texture32> Textures { get; init; }
        public Dictionary<int, Texture32> Sprites { get; init; }
        public Dictionary<int, Texture32> Pictures { get; init; }
        private (ushort Node0, ushort Node1)[] HuffmanTree { get; init; }
        private static int START_PC_SOUND => 0;
        private int START_ADLIB_SOUND => 1 * Version.NumberOfSounds;
        private int START_DIGI_SOUND => 2 * Version.NumberOfSounds;
        private int START_MUSIC => 3 * Version.NumberOfSounds;
        private int NUMBER_OF_CHUNKS => 3 * Version.NumberOfSounds + Version.NumberOfMusic + 1;
        public Sound[] Sounds { get; init; }
        public Sound[] DigiSounds { get; init; }
        public string[] Music { get; init; }

        public readonly int[] CeilingColours =
            [
                // Episode 1
                29,
                29,
                29,
                29,
                29,
                29,
                29,
                29,
                29,
                191,
                // Episode 2
                78,
                78,
                78,
                29,
                141,
                78,
                29,
                45,
                29,
                141,
                // Episode 3
                29,
                29,
                29,
                29,
                29,
                45,
                221,
                29,
                29,
                152,
                // Episode 4
                29,
                157,
                45,
                221,
                221,
                157,
                45,
                77,
                29,
                221,
                // Episode 5
                125,
                29,
                45,
                45,
                221,
                215,
                29,
                29,
                29,
                45,
                // Episode 6
                29,
                29,
                29,
                29,
                221,
                221,
                125,
                221,
                221,
                221
        ];
        public RGBA8 GetFloorColor() => Version.Pallet[25];
        public RGBA8 GetCeilingColor(int level)
        {
            if (level < 0 || level >= CeilingColours.Length) return RGBA8.BLACK;
            return Version.Pallet[CeilingColours[level]];
        }
        public int DoorTexture => VSWAPHeader.SpriteStart - 8;
        public bool IsValid { get; init; }
        public static bool GameDataExists(GameVersion version)
        {
            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\mod.json");
            return File.Exists(file);
        }
        public GameData(GameVersion version)
        {
            Version = version;
            AudioOffsetsData = File.ReadAllBytes(Version.AudioOffsets);
            AudioChunksData = File.ReadAllBytes(Version.AudioChunks);
            VideoAudioData = File.ReadAllBytes(Version.VideoAudio);
            LevelAtlasData = File.ReadAllBytes(Version.LevelAtlas);
            LevelMapsData = File.ReadAllBytes(Version.LevelMaps);
            VGAHuffmanData = File.ReadAllBytes(Version.VGAHuffman);
            VGAOffsetsData = File.ReadAllBytes(Version.VGAOffsets);
            VGATexturesData = File.ReadAllBytes(Version.VGATextures);

            var audioOffsets = LoadAudioChunkOffsets();
            Music = ReadMusic(audioOffsets);
            Sounds = ReadSounds(audioOffsets);
            DigiSounds = ReadDigiSounds(audioOffsets);

            VSWAPHeader = ReadVSWAPData();
            Sprites = ReadSprites();
            Textures = ReadtTextures();

            HuffmanTree = ReadHuffmanTree();
            PictureAtlas = ReadPictureAtlas();
            Pictures = ReadPictures();

            LevelAtlas = ReadLevelAtlaslData();
            LevelHeaders = ReadLevelHeaderData();
            Maps = ReadMapData();
            IsValid = Maps.Length > 0
                && Pictures.Count > 0
                && Sprites.Count > 0
                && Music.Length > 0
                && Sounds.Length > 0
                && DigiSounds.Length > 0;
        }
        private Sound[] ReadSounds(uint[] offsets)
        {
            var sounds = new List<Sound>();
            // Extract a PC Speaker sound
            for (int i = 0; i < Version.NumberOfSounds; i++)
            {
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Sounds\pcs{i}.wav");
                var sound = ExtractSound(i, offsets, SoundFormat.PcSpeaker);
                if (sound != null)
                {
                    byte[] wavFile = ConvertToWav(sound.Value);
                    if (!File.Exists(file))
                        File.WriteAllBytes(file, wavFile);
                    sounds.Add(new Sound(new SoundBuffer(file)));
                }
            }
            return [.. sounds];
        }
        private Sound[] ReadDigiSounds(uint[] offsets)
        {
            var sounds = new List<Sound>();
            for (int i = 0; i < Version.NumberOfDigiSounds; i++)
            {
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Sounds\digi{i}.wav");
                var sound = ExtractSound(i, offsets, SoundFormat.Digitized);
                if (sound != null)
                {
                    byte[] wavFile = ConvertToWav(sound.Value);
                    if (!File.Exists(file))
                        File.WriteAllBytes(file, wavFile);
                    sounds.Add(new Sound(new SoundBuffer(file)));
                }
            }
            return [.. sounds];
        }
        private string[] ReadMusic(uint[] offsets)
        {
            var music = new List<string>();
            for (int i = 0; i < Version.NumberOfMusic; i++)
            {
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Music\{i}.mid");
                var sound = ExtractMusic(i, offsets);
                if (sound != null)
                {
                    if (!File.Exists(file))
                        File.WriteAllBytes(file, sound);
                    music.Add(file);
                }
            }
            return [.. music];
        }
        private uint[] LoadAudioChunkOffsets()
        {
            var offsets = new uint[NUMBER_OF_CHUNKS];

            for (int i = 0; i < NUMBER_OF_CHUNKS; i++)
            {
                offsets[i] = BitConverter.ToUInt32(AudioOffsetsData, i * 4);
            }

            return offsets;
        }
        private AudioChunk? ExtractSound(int soundNumber, uint[] offsets, SoundFormat format)
        {
            if (offsets == null)
                return null;

            int magicNumber = soundNumber;
            int chunkSize;

            switch (format)
            {
                case SoundFormat.PcSpeaker:
                    chunkSize = (int)(offsets[magicNumber + 1] - offsets[magicNumber]);
                    if (chunkSize == 0) return null;
                    return LoadPcsSoundFromBytes(magicNumber, chunkSize, offsets);

                case SoundFormat.AdLib:
                    magicNumber += START_ADLIB_SOUND;
                    chunkSize = (int)(offsets[magicNumber + 1] - offsets[magicNumber]);
                    if (chunkSize == 0) return null;
                    return LoadPcsSoundFromBytes(magicNumber, chunkSize, offsets);

                case SoundFormat.Digitized:
                    return LoadDigiSoundFromBytes((int)soundNumber);

                default:
                    return null;
            }
        }
        private byte[]? ExtractMusic(int musicNumber, uint[] offsets)
        {
            if (offsets == null)
                return null;

            int magicNumber = musicNumber + START_MUSIC;
            int chunkSize = (int)(offsets[magicNumber + 1] - offsets[magicNumber]);

            if (chunkSize == 0)
                return null;

            var data = LoadPcsSoundFromBytes(magicNumber, chunkSize, offsets);
            return new Imf2MidiConverter().Convert(data.Data);
        }
        private AudioChunk LoadPcsSoundFromBytes(int offset, int length, uint[] offsets)
        {
            byte[] buffer = new byte[length];
            Array.Copy(AudioChunksData, (int)offsets[offset], buffer, 0, length);

            return new AudioChunk
            {
                Data = buffer,
                Length = length,
                Format = SoundFormat.PcSpeaker
            };
        }
        private AudioChunk? LoadDigiSoundFromBytes(int soundNumber)
        {
            if (soundNumber >= Version.NumberOfDigiSounds)
                return null;

            if (VideoAudioData.Length < 6)
                return null;

            // Read header information
            ushort numberOfChunks = BitConverter.ToUInt16(VideoAudioData, 0);
            ushort soundStart = BitConverter.ToUInt16(VideoAudioData, 4); // Skip sprite start at offset 2

            // Get list offset
            int listOffsetPos = 6 + (numberOfChunks - 1) * 4;
            if (listOffsetPos + 4 > VideoAudioData.Length)
                return null;

            uint listOffset = BitConverter.ToUInt32(VideoAudioData, listOffsetPos);

            // Read chunk index and length
            int chunkInfoPos = (int)(listOffset + soundNumber * 4);
            if (chunkInfoPos + 4 > VideoAudioData.Length)
                return null;

            ushort chunkIndex = BitConverter.ToUInt16(VideoAudioData, chunkInfoPos);
            ushort chunkLength = BitConverter.ToUInt16(VideoAudioData, chunkInfoPos + 2);

            // Get chunk offset
            int chunkOffsetPos = 6 + (soundStart + chunkIndex) * 4;
            if (chunkOffsetPos + 4 > VideoAudioData.Length)
                return null;

            uint chunkOffset = BitConverter.ToUInt32(VideoAudioData, chunkOffsetPos);

            // Extract the audio data
            if (chunkOffset + chunkLength > VideoAudioData.Length)
                return null;

            byte[] buffer = new byte[chunkLength];
            Array.Copy(VideoAudioData, (int)chunkOffset, buffer, 0, chunkLength);

            return new AudioChunk
            {
                Data = buffer,
                Length = chunkLength,
                Format = SoundFormat.Digitized
            };
        }
        public static byte[] ConvertToWav(AudioChunk audioChunk, uint sampleRate = 40000)
        {
            byte[] waveData;

            if (audioChunk.Format == SoundFormat.Digitized)
            {
                waveData = DigiToWave(audioChunk.Data);
                sampleRate = 7000; // Digitized sounds use 7kHz
            }
            else
            {
                waveData = PcsToWave(audioChunk.Data, sampleRate);
            }

            return CreateWavFile(waveData, sampleRate);
        }
        private static byte[] DigiToWave(byte[] sourceData)
        {
            // Digitized sound is already in the correct format
            byte[] destination = new byte[sourceData.Length];
            Array.Copy(sourceData, destination, sourceData.Length);
            return destination;
        }
        private static byte[] PcsToWave(byte[] sourceData, uint sampleRate)
        {
            uint samplesPerByte = sampleRate / PCS_RATE;
            int wavLength = sourceData.Length * (int)samplesPerByte;
            byte[] destination = new byte[wavLength];

            int writePos = 0;
            int sign = -1;
            int phaseTick = 0;

            for (int i = 0; i < sourceData.Length; i++)
            {
                int tone = sourceData[i] * 60;
                int phaseLength = (int)((sampleRate * tone) / (2 * BASE_TIMER));

                for (int j = 0; j < samplesPerByte; j++)
                {
                    if (tone > 0)
                    {
                        destination[writePos++] = (byte)(128 + sign * PCS_VOLUME);
                        if (phaseTick++ >= phaseLength)
                        {
                            sign *= -1;
                            phaseTick = 0;
                        }
                    }
                    else
                    {
                        phaseTick = 0;
                        destination[writePos++] = 128;
                    }
                }
            }

            return destination;
        }
        private static byte[] CreateWavFile(byte[] audioData, uint sampleRate)
        {
            uint fileSize = (uint)(36 + audioData.Length);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // RIFF header
            writer.Write("RIFF".ToCharArray());
            writer.Write(fileSize);
            writer.Write("WAVE".ToCharArray());

            // Format chunk
            writer.Write("fmt ".ToCharArray());
            writer.Write((uint)16);           // Format length
            writer.Write((ushort)1);          // Format type (PCM)
            writer.Write((ushort)1);          // Channels
            writer.Write(sampleRate);         // Sample rate
            writer.Write(sampleRate);         // Byte rate
            writer.Write((ushort)1);          // Block align
            writer.Write((ushort)8);          // Bit rate

            // Data chunk
            writer.Write("data".ToCharArray());
            writer.Write((uint)audioData.Length);
            writer.Write(audioData);

            return ms.ToArray();
        }
        private (ushort Node0, ushort Node1)[] ReadHuffmanTree()
        {
            var tree = new (ushort Node0, ushort Node1)[HUFFMAN_TREE_NODE_COUNT];
            var offset = 0;
            for (int i = 0; i < HUFFMAN_TREE_NODE_COUNT; i++)
            {
                tree[i].Node0 = BitConverter.ToUInt16(VGAHuffmanData, offset); ;
                tree[i].Node1 = BitConverter.ToUInt16(VGAHuffmanData, offset + 2);
                offset += 4;
            }
            return tree;
        }
        private (ushort Width, ushort Height)[] LoadPictureTable(int[] offsets)
        {
            var table = new (ushort Width, ushort Height)[Version.PictureCounts];
            int compressedLength = offsets[1] - offsets[0] - 4;
            int expandedLength = BitConverter.ToInt32([.. VGATexturesData.Take(4)], 0);
            byte[] expandedChunk = HuffmanExpand([.. VGATexturesData.Skip(4).Take(compressedLength)], expandedLength);
            for (int i = 0; i < Version.PictureCounts; i++)
            {
                table[i].Width = BitConverter.ToUInt16(expandedChunk, 4 * i);
                table[i].Height = BitConverter.ToUInt16(expandedChunk, (4 * i) + 2);
            }
            return table;
        }
        private byte[] HuffmanExpand(byte[] source, int length)
        {
            byte[] destination = new byte[length];
            int readPos = 0;
            int writePos = 0;
            byte mask = 0x01;
            byte input = source[readPos++];

            var node = HuffmanTree[ROOT];
            int bytesWritten = 0;

            while (true)
            {
                ushort nodeValue;

                if ((input & mask) == 0)
                    nodeValue = node.Node0;
                else
                    nodeValue = node.Node1;

                if (mask == 0x80)
                {
                    if (readPos < source.Length)
                        input = source[readPos++];
                    mask = 0x01;
                }
                else
                {
                    mask <<= 1;
                }

                if (nodeValue <= 0xFF)
                {
                    destination[writePos++] = (byte)nodeValue;
                    node = HuffmanTree[ROOT];

                    if (++bytesWritten == length)
                        break;
                }
                else
                {
                    node = HuffmanTree[nodeValue - 256];
                }
            }

            return destination;
        }
        private int[] LoadPictureOffsets()
        {
            int chunkCount = (int)(VGAOffsetsData.Length / 3);
            var offsets = new int[chunkCount];
            var offset = 0;
            for (int i = 0; i < chunkCount; i++)
            {
                offsets[i] = VGAOffsetsData[offset] | (VGAOffsetsData[offset + 1] << 8) | (VGAOffsetsData[offset + 2] << 16);
                if (offsets[i] == 0x00FFFFFF)
                    offsets[i] = -1;
                offset += 3;
            }
            return offsets;

        }
        private PictureAtlas ReadPictureAtlas()
        {
            var offsets = LoadPictureOffsets();
            var table = LoadPictureTable(offsets);
            return new PictureAtlas(table, offsets);
        }
        //return (j * (width >> 2) + (i >> 2)) + (i & 3) * (width >> 2) * height;
        private static int CalculateTextureIndex(int width, int height, int x, int y)
        {
            // Break down the calculation into understandable parts
            int blockWidth = width / 4;    // width >> 2 is dividing by 4

            int blockX = x / 4;            // i >> 2 is dividing by 4 to get block coordinate
            int pixelInBlock = x % 4;      // i & 3 is getting remainder when dividing by 4

            // Calculate the index step by step
            int baseIndex = y * blockWidth + blockX;  // j is used as-is (full Y coordinate)
            int planeOffset = pixelInBlock * blockWidth * height; // Offset for color plane

            return baseIndex + planeOffset;
        }
        public Texture32? GetPicture(int magicNumber)
        {
            magicNumber += Version.PictureStarts;
            if (magicNumber < Version.PictureStarts)
                return null;
            if (PictureAtlas.ChunkOffsets[magicNumber] == -1)
                return null;


            // Find next valid chunk
            int nextIndex = magicNumber + 1;
            while (PictureAtlas.ChunkOffsets[nextIndex] == -1 && nextIndex < Version.PictureCounts)
                nextIndex++;

            if (PictureAtlas.ChunkOffsets[nextIndex] == -1)
                return null;

            int compressedLength = PictureAtlas.ChunkOffsets[nextIndex] - PictureAtlas.ChunkOffsets[magicNumber];

            // Read compressed data
            byte[] compressedData = [.. VGATexturesData.Skip(PictureAtlas.ChunkOffsets[magicNumber]).Take(compressedLength)];

            // Get expanded length from first 4 bytes
            int expandedLength = BitConverter.ToInt32(compressedData, 0);

            // Extract compressed chunk (skip first 4 bytes)
            byte[] compressedChunk = new byte[compressedLength - 4];
            Array.Copy(compressedData, 4, compressedChunk, 0, compressedLength - 4);

            // Expand using Huffman
            byte[] expandedChunk = HuffmanExpand(compressedChunk, expandedLength);

            var texture = new Texture32(PictureAtlas.PicTable[magicNumber - Version.PictureStarts].Width, PictureAtlas.PicTable[magicNumber - Version.PictureStarts].Height);
            for (int y = 0; y < texture.Height; y++)
            {
                for (int x = 0; x < texture.Width; x++)
                {
                    RGBA8 colourRGBA = Version.Pallet[expandedChunk[CalculateTextureIndex((ushort)texture.Width, (ushort)texture.Height, x, y)]];
                    texture.PutPixel(x, y, colourRGBA.R, colourRGBA.G, colourRGBA.B, colourRGBA.A);
                }
            }
            return texture;

        }
        private Dictionary<int, Texture32> ReadPictures()
        {
            var pics = Version.PictureEnds - Version.PictureStarts;
            var ret = new Dictionary<int, Texture32>();
            for (var i = 0; i < pics; i++)
            {
                var pic = GetPicture(i);
                if (pic == null) continue;
                var image = new SFML.Graphics.Image((uint)pic.Width, (uint)pic.Height, pic.Pixels);
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Pictures\{i}.png");
                if (!File.Exists(file))
                    image.SaveToFile(file);
                ret.Add(i, pic);
            }
            return ret;
        }
        public Texture32? GetSprite(int magicNumber)
        {
            try
            {
                magicNumber += VSWAPHeader.SpriteStart;
                if (magicNumber < VSWAPHeader.SpriteStart || magicNumber >= VSWAPHeader.SoundStart)
                    return null;

                Texture32 bitmap = new(64, 64);

                var rawSpriteData = VideoAudioData.Skip((int)VSWAPHeader.ChunkOffsets[magicNumber]).Take((int)VSWAPHeader.ChunkLengths[magicNumber]).ToArray();

                ushort xStart = BitConverter.ToUInt16(rawSpriteData, 0);
                ushort xEnd = BitConverter.ToUInt16(rawSpriteData, 2);


                int numColumns = xEnd - xStart + 1;

                ushort[] columnOffsets = new ushort[numColumns];

                for (int i = 0; i < numColumns; i++)
                    columnOffsets[i] = BitConverter.ToUInt16(rawSpriteData, 4 + (i * 2));

                int pixelDataIterator = 4 + (numColumns * 2);

                for (int xDraw = xStart; xDraw <= xEnd; xDraw++)
                {
                    ushort yStart, yEnd;
                    int instructionOffset = columnOffsets[xDraw - xStart];
                    while (true)
                    {
                        yEnd = BitConverter.ToUInt16(rawSpriteData, instructionOffset);
                        if (yEnd == 0)
                            break;
                        yStart = BitConverter.ToUInt16(rawSpriteData, instructionOffset + 4);
                        instructionOffset += 6;
                        yStart = (ushort)(yStart / 2);
                        yEnd = (ushort)(yEnd / 2);

                        for (int yDraw = yStart; yDraw < yEnd; yDraw++)
                        {
                            byte color = rawSpriteData[pixelDataIterator];
                            pixelDataIterator++;
                            RGBA8 colourRGBA = Version.Pallet[color];
                            bitmap.PutPixel(xDraw, yDraw, colourRGBA.R, colourRGBA.G, colourRGBA.B, colourRGBA.A);
                        }
                    }
                }


                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        private Dictionary<int, Texture32> ReadSprites()
        {
            var sprites = VSWAPHeader.SoundStart - VSWAPHeader.SpriteStart;
            var ret = new Dictionary<int, Texture32>();
            for (var i = 0; i < sprites; i++)
            {
                var sprite = GetSprite(i);
                if (sprite == null) continue;
                var image = new SFML.Graphics.Image((uint)sprite.Width, (uint)sprite.Height, sprite.Pixels);
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Sprites\{i}.png");
                if (!File.Exists(file))
                    image.SaveToFile(file);
                ret.Add(i, sprite);
            }
            return ret;
        }
        private Texture32? GetTexture(int magicNumber)
        {
            try
            {
                if (magicNumber >= VSWAPHeader.SpriteStart)
                    return null;
                Texture32 bitmap = new(64, 64);

                // Render the texture to our Bitmap.
                byte[] chunkData = new byte[VSWAPHeader.ChunkLengths[magicNumber]];
                for (int i = 0; i < VSWAPHeader.ChunkLengths[magicNumber]; i++)
                {
                    chunkData[i] = VideoAudioData[VSWAPHeader.ChunkOffsets[magicNumber] + i];
                }

                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        byte color = chunkData[x * 64 + y];
                        RGBA8 colourRGBA = Version.Pallet[color];
                        bitmap.PutPixel(x, y, colourRGBA.R, colourRGBA.G, colourRGBA.B, colourRGBA.A);
                    }
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
        private Dictionary<int, Texture32> ReadtTextures()
        {
            var textures = VSWAPHeader.SpriteStart - 1;
            var ret = new Dictionary<int, Texture32>();
            for (var i = 0; i < textures; i++)
            {
                var texture = GetTexture(i);
                if (texture == null) continue;
                var image = new SFML.Graphics.Image((uint)texture.Width, (uint)texture.Height, texture.Pixels);
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Textures\{i}.png");
                if (!File.Exists(file))
                    image.SaveToFile(file);
                ret.Add(i, texture);
            }
            return ret;
        }
        private VSWAPHeader ReadVSWAPData()
        {
            var chunkCount = BitConverter.ToUInt16(VideoAudioData, 0);
            var spriteStart = BitConverter.ToUInt16(VideoAudioData, 2);
            var soundStart = BitConverter.ToUInt16(VideoAudioData, 4);

            var chunkOffsets = new uint[chunkCount];
            var chunkLengths = new ushort[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                chunkOffsets[i] = BitConverter.ToUInt32(VideoAudioData, 6 + (i * 4));
                chunkLengths[i] = BitConverter.ToUInt16(VideoAudioData, 6 + (chunkCount * 4) + (i * 2));
            }
            return new VSWAPHeader(chunkCount, spriteStart, soundStart, chunkOffsets, chunkLengths);
        }
        // Handles the decompression of data using RLEW compression scheme
        private static byte[] RLEWDecompress(short topRLEW, short bottomRLEW, byte[] input)
        {
            List<byte> result = [];
            int inputIterator = 2;

            while (inputIterator < input.Length)
            {
                byte topinput = input[inputIterator];
                byte bottominput = input[inputIterator + 1];
                inputIterator += 2;

                if (topinput == topRLEW && bottominput == bottomRLEW)
                {
                    byte topcount = input[inputIterator];
                    byte bottomcount = input[inputIterator + 1];
                    inputIterator += 2;

                    int count = bottomcount * 256 + topcount;

                    byte topvalue = input[inputIterator];
                    byte bottomvalue = input[inputIterator + 1];
                    inputIterator += 2;

                    while (count > 0)
                    {
                        result.Add(topvalue);
                        result.Add(bottomvalue);
                        count--;
                    }
                }
                else
                {
                    result.Add(topinput);
                    result.Add(bottominput);
                }
            }
            return [.. result];
        }
        // Handles the decompression of data using ID Software's LZ compression scheme
        private static byte[] CarmackDecompress(byte[] input)
        {
            List<byte> result = [];

            // We're starting at byte 2 because the first two bytes are the uncompressed size of the data.
            int inputIterator = 2;

            // Original source uses length/2 to do this, and subtracts from length everytime a WORD (2 bytes)
            // is passed. We're going to use the length of the input array instead as we easily have access to it.
            while (inputIterator < input.Length)
            {
                // Grab two bytes, topend and bottomend in sequence.
                byte topend = input[inputIterator];
                byte bottomend = input[inputIterator + 1];
                inputIterator += 2;

                // The bottom end tags whether or not this section is compressed, and if so with which form.
                if (bottomend == 0xA7) // One Byte "Near pointer"
                {
                    if (topend == 0x00) // Signals that the original trigger is actually part of the data.
                    {                   // So we grab one more byte, as it is the true topend of this sequence.
                        topend = input[inputIterator];
                        inputIterator++;
                        result.Add(topend);
                        result.Add(bottomend);
                        continue;
                    }
                    else
                    {
                        byte count = topend; // The number of words to copy.
                        int offset = input[inputIterator]; // The offset (in words) to copy from.
                        inputIterator++;
                        offset *= 2; // We multiply by two because we're dealing with bytes, not words.

                        while (count > 0)
                        {   // We copy the words, count times, from the offset.
                            int outOffset = result.Count - offset;

                            topend = result[outOffset];
                            bottomend = result[outOffset + 1];
                            result.Add(topend);
                            result.Add(bottomend);
                            count--;
                        }
                        continue;
                    }
                }
                else if (bottomend == 0xA8) // One WORD "Far Pointer"
                {
                    if (topend == 0x00)
                    {
                        topend = input[inputIterator];
                        inputIterator++;
                        result.Add(topend);
                        result.Add(bottomend);
                        continue;
                    }
                    else
                    {
                        byte count = topend;
                        byte offsettop = input[inputIterator];
                        byte offsetbottom = input[inputIterator + 1];
                        inputIterator += 2;

                        short offset = (short)(offsetbottom * 256 + offsettop);

                        offset *= 2;

                        while (count > 0)
                        {
                            count--;
                            topend = result[offset];
                            bottomend = result[offset + 1];
                            offset += 2;
                            result.Add(topend);
                            result.Add(bottomend);
                        }
                        continue;
                    }

                }
                else
                {   // There is no compression, just add these bytes to the output.
                    result.Add(topend);
                    result.Add(bottomend);
                }

            }
            return [.. result];
        }
        private LevelAtlas ReadLevelAtlaslData()
        {
            var offsets = new List<int>();
            // Load all the mapOffsets 
            for (int i = 2; i < LevelAtlasData.Length; i += 4)
            {
                var offset = BitConverter.ToInt32(LevelAtlasData, i);
                if (offset == 0) break;
                offsets.Add(offset);
            }
            return new LevelAtlas(LevelAtlasData[0], LevelAtlasData[1], [.. offsets]);
        }
        private LevelHeader[] ReadLevelHeaderData()
        {
            var maps = new List<LevelHeader>();
            foreach (var offset in LevelAtlas.HeaderOffsets)
            {
                /// Offset Type    Name Description
                /// 0   int         offPlane0   Offset in GAMEMAPS to beginning of compressed plane 0 data(or <= 0 if plane is not present)
                /// 4   int         offPlane1   Offset in GAMEMAPS to beginning of compressed plane 1 data(or <= 0 if plane is not present)
                /// 8   int         offPlane2   Offset in GAMEMAPS to beginning of compressed plane 2 data(or <= 0 if plane is not present)
                /// 12  ushort      lenPlane0   Length of compressed plane 0 data(in bytes)
                /// 14  ushort      lenPlane1   Length of compressed plane 1 data(in bytes)
                /// 16  ushort      lenPlane2   Length of compressed plane 2 data(in bytes)
                /// 18  ushort      width   Width of level(in tiles)
                /// 20  ushort      height  Height of level(in tiles)
                /// 22  byte[16]    name    Internal name for level(used only by editor, not displayed in -game. null - terminated)
                var map = new LevelHeader();
                map.MapOffsets[(int)MapPlanes.ARCHITECTURE] = BitConverter.ToInt32(LevelMapsData, offset);
                map.MapOffsets[(int)MapPlanes.OBJECTS] = BitConverter.ToInt32(LevelMapsData, offset + 4);
                map.MapOffsets[(int)MapPlanes.LOGIC] = BitConverter.ToInt32(LevelMapsData, offset + 8);
                map.CCLenght[(int)MapPlanes.ARCHITECTURE] = BitConverter.ToUInt16(LevelMapsData, offset + 12);
                map.CCLenght[(int)MapPlanes.OBJECTS] = BitConverter.ToUInt16(LevelMapsData, offset + 14);
                map.CCLenght[(int)MapPlanes.LOGIC] = BitConverter.ToUInt16(LevelMapsData, offset + 16);
                map.Width = BitConverter.ToUInt16(LevelMapsData, offset + 18);
                map.Height = BitConverter.ToUInt16(LevelMapsData, offset + 20);
                for (int i = 0; i < 16; i++)
                {
                    map.NameData[i] = LevelMapsData[offset + 22 + i];
                }
                maps.Add(map);
            }
            return [.. maps];
        }
        private MapData[] ReadMapData()
        {
            var maps = new List<MapData>();
            for (int i = 0; i < LevelHeaders.Length; i++)
            {
                LevelHeader? level = LevelHeaders[i];
                var architecturePlane = LevelMapsData.Skip(level.MapOffsets[(int)MapPlanes.ARCHITECTURE]).Take(level.CCLenght[(int)MapPlanes.ARCHITECTURE]).ToArray();
                var objectsPlane = LevelMapsData.Skip(level.MapOffsets[(int)MapPlanes.OBJECTS]).Take(level.CCLenght[(int)MapPlanes.OBJECTS]).ToArray();
                var logicPlane = LevelMapsData.Skip(level.MapOffsets[(int)MapPlanes.LOGIC]).Take(level.CCLenght[(int)MapPlanes.LOGIC]).ToArray();
                var map = new MapData(level.Name, level.Width, level.Height,
                    RLEWDecompress(LevelAtlas.TopRLEW, LevelAtlas.BottomRLEW, CarmackDecompress(architecturePlane)),
                    RLEWDecompress(LevelAtlas.TopRLEW, LevelAtlas.BottomRLEW, CarmackDecompress(objectsPlane)),
                    RLEWDecompress(LevelAtlas.TopRLEW, LevelAtlas.BottomRLEW, CarmackDecompress(logicPlane))
                    );

                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{Version.Name}\Levels\{i}.json");
                if (!File.Exists(file))
                    FileHelpers.Shared.Serialize(map, file);
                //file = FileHelper.GetDataFilePath(@$"Mods\{Version.Name}\Levels\{i}.png");
                //RenderMap(map, file);
                maps.Add(map);
            }
            return [.. maps];
        }
        private void RenderMap(MapData map, string file)
        {
            var basicFontChars = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{}~|".ToCharArray();
            var SmallFont = new BasicGameFont(FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\FontSmall.png"), basicFontChars);
            var sod = !Version.Extension.StartsWith("W", StringComparison.InvariantCultureIgnoreCase);
            var floor = new Texture32(64, 64);
            var fc = GetFloorColor();
            floor.Clear(fc.R, fc.G, fc.B);
            var texture = new Texture32(map.Width * 64, map.Height * 64);
            texture.Clear(0, 0, 0);

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var t = map.GetTileData(x, y);

                    var d = MapDirection.DIR_NONE;
                    if (x == 0 && map.GetTileData(x + 1, y) == 0)
                    {
                        d = MapDirection.DIR_EAST;
                    }
                    else if (x == map.Width && map.GetTileData(x - 1, y) == 0)
                    {
                        d = MapDirection.DIR_WEST;
                    }
                    else if (map.GetTileData(x - 1, y) == 0 || map.GetTileData(x + 1, y) == 0)
                    {
                        d = MapDirection.DIR_WEST; //dosnt matter really
                    }
                    var mt = GetTextureIndex(t, d);

                    if (t == 0 || t == 85) //Inside  85 odd in r2d
                    {
                        texture.Draw(x * 64, y * 64, floor);
                        continue;
                    }
                    else if (!Textures.ContainsKey(mt))
                    {
                        texture.RectFill(x * 64, y * 64, 64, 64, 0, 0, 0);
                        texture.DrawString(x * 64, y * 64, t.ToString(), SmallFont, RGBA8.RED);
                        continue;
                    }
                    texture.Draw(x * 64, y * 64, Textures[mt]);
                    texture.DrawString(x * 64, y * 64, t.ToString(), SmallFont, t == 11 ? RGBA8.BLUE : RGBA8.BLUE);
                }

            }

            foreach (var obj in map.StaticMapObjects)
            {
                var s = GetSpriteIndex(obj.ObjectID);
                if (Sprites.TryGetValue(s, out Texture32? value))
                {
                    texture.Draw(obj.X * 64, obj.Y * 64, value);
                }
            }
            texture.RectFill(map.PlayerSpawnX * 64, map.PlayerSpawnY * 64, 64, 64, 76, 0, 0);
            texture.Draw(map.PlayerSpawnX * 64, map.PlayerSpawnY * 64, Pictures[sod ? 20 : 30]);
            foreach (var obj in map.ActorMapObjects)
            {
                var s = GetActorSprite(obj.ActorType);
                if (Sprites.TryGetValue(s, out Texture32? value))
                {
                    texture.Draw(obj.X * 64, obj.Y * 64, value);
                }
            }

            var image = new SFML.Graphics.Image((uint)texture.Width, (uint)texture.Height, texture.Pixels);
            image.SaveToFile(file);
        }
        private static int GetActorSprite(string str)
        {
            return (str ?? string.Empty) switch
            {
                "GuardStandEasy" => 50,
                "GuardStandMedium" => 50,
                "GuardStandHard" => 50,
                "GuardPathEasy" => 60,
                "GuardPathMedium" => 60,
                "GuardPathHard" => 60,
                "GuardDead" => 95,
                _ => -1,
            };
        }
        //Add 1 for direction East West
        private static int GetTextureIndex(int i, MapDirection d)
        {
            int Get()
            {
                return i switch
                {
                    0 => -1,//Nothing 
                    90 => 98,//Door
                    91 => 98,//Door
                    100 => 102,//Door
                    101 => 102,//Door
                    9 => 16,//blue
                    8 => 14,//blue fat bottom left brick
                    7 => 12,//Prison with bones
                    5 => 8,//Prison
                    1 => 0,//Brick
                    2 => 2,//Brick Fat
                    4 => 6,//Hitler
                    3 => 4,//Swas Flag Brick
                    6 => 10,//Eagle
                    10 => 18,//Wood Eagle
                    12 => 22,//Wood
                    21 => 40,//End
                    11 => 20,// wood hitler
                    13 => 24,// exit door
                    15 => 28,// steel
                    14 => 26,// steel warning 
                    92 => 104,//locked door
                    93 => 104,//locked door
                    94 => 104,//locked door
                    95 => 104,//locked door
                    17 => 32,//red brick
                    18 => 34,//red brick swas
                    38 => 74,//reb blick with mixed
                    19 => 36,//purple
                    25 => 49,//purple splat
                    20 => 38,//Brick eagle
                    16 => 30,//freedom daytime
                    24 => 46,//slime wall
                    26 => 50,//lite slime wall
                    28 => 54,// Rock Attention
                    23 => 44,// Wood Cross
                    27 => 0,// blocks
                    35 => 68,// cement brick
                    33 => 64,// cement moazic
                    39 => 76,// cement brick crack
                    29 => 56,// rock
                    49 => 96,// rock hitler
                    30 => 58,// rock splat 1
                    31 => 60,// rock splat 2
                    32 => 62,// rock splat 3
                    37 => 72,// cement brick drain
                    43 => 84,// cement world map
                    34 => 66,// clean blue skull
                    36 => 70,// clean blue swas
                    41 => 80,// blue warning
                    40 => 78,// clean blue
                    47 => 92,// marb swas
                    42 => 82,//marb
                    46 => 90,//marb
                    48 => 94,//arche thing wood?
                    44 => 86,//brown stone
                    45 => 88,//brown stone
                    63 => 124,//SOD purple brick
                    50 => 98,//SOD Rock
                    51 => 100,//SOD Rock
                    52 => 102,//SOD Rock
                    53 => 102,//SOD Rock swas
                    55 => 108,//SOD Rock
                    54 => 106,//SOD Rock
                    57 => 112,//SOD Rock
                    56 => 110,//SOD blood Rock
                    58 => 114,//SOD Rock
                    59 => 116,//SOD Rock
                    62 => 122,//SOD Rock
                    60 => 118,//SOD Lift piping
                    22 => 40,//SOD Lift
                    61 => 120,//R2D purble blue
                    _ => -1,
                };
            }
            var w = Get();
            if (w < 0 || w == 104) return w;
            return w + ((d == MapDirection.DIR_EAST || d == MapDirection.DIR_WEST) ? 1 : 0);




        }

        private int GetSpriteIndex(int i)
        {
            var sod = !Version.Extension.StartsWith("W", StringComparison.InvariantCultureIgnoreCase);
            switch (i)
            {
                case 19: // Player Start
                case 20:
                case 21:
                case 22: return -1; // Player Start
                case 23: return 2; // Puddle
                case 24: return 3; // Green Barrel
                case 25: return 4; // Table/Chairs
                case 26: return 5; // Floor Lamp
                case 27: return 6; // Hanging light
                case 28: return 7; // Hanged Man
                case 29: return 8; // bad food
                case 30: return 9; // Pillar
                case 31: return 10; // Tree;
                case 32: return 11; // Bones
                case 33: return 12; // Tree;
                case 34: return 13; // Potted plant
                case 35: return 14; // Urn
                case 36: return 15; // Table
                case 37: return 16; // Lights
                case 38: return 17; // Hanging pots and pans
                case 39: return 18; // Suit of armor
                case 40: return 19; // Cage
                case 41: return 20; // Skelliton in cage
                case 42: return 21; // Pile of Bones
                case 43: return 22; // Gold Key
                case 44: return 23; // Solver Key
                case 45: return 24; // Bed
                case 46: return 25; // Pot
                case 47: return 26; // Good Food
                case 48: return 27; // First Aid
                case 49: return 28; // Clip
                case 50: return 29; // Machine Gun
                case 51: return 30; // Gatling Gun
                case 52: return 31; // Cross
                case 53: return 32; // Chalice
                case 54: return 33; // Treasure
                case 55: return 34; // Crown    
                case 56: return 35; // One Up
                case 57: return 36; // Blood pool bones
                case 58: return 37; // Barrel
                case 59: return 38; // Well
                case 60: return 39; // Empty Well
                case 61: return 40; // Blood pool
                case 62: return 41; // Flag
                case 63: return 42; // Aaaardwolf
                case 64: return 43; // Gibs
                case 65: return 44; // Gibs
                case 66: return 45; // Gibs
                case 67: return 46; // pots pand
                case 68: return 47; // Stove
                case 69: return 48; // Spears
                case 70: return 49; // Weeds
                // Spear of destiny 
                case 71: return 49; // Marble Pillar
                case 72: return 49; // Box of ammo
                case 73: return 49; // Truck
                case 74: return 49; // Spear of Destiny

                case 98: return -1; //Pushwall

                // --= Guards =-- //
                case 180: // Hard Skill Guard
                case 181:
                case 182:
                case 183: return sod ? 54 : 50;

                case 144: // Medium Skill Guard
                case 145:
                case 146:
                case 147: return sod ? 54 : 50;

                case 108: // Easy Skill Guard
                case 109:
                case 110:
                case 111: return sod ? 54 : 50;

                case 184: // Hard Skill Guard Patrol
                case 185:
                case 186:
                case 187: return sod ? 54 : 50;
                case 148: // Medium Skill Guard Patrol
                case 149:
                case 150:
                case 151: return sod ? 54 : 50;
                case 112: // Easy Skill Guard Patrol
                case 113:
                case 114:
                case 115: return sod ? 54 : 50;
                case 124: return sod ? 99 : 95; // Dead Guard


                default:
                    break;
            }
            return -1;
        }
    }
}