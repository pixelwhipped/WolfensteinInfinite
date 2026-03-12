//Clean
using Newtonsoft.Json;
using SFML.Graphics;
using System.IO;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.Utilities
{
    public class FileHelpers
    {        
        public readonly static FileHelpers Shared = new();
        public string BaseDirectory { get; init; }
        private FileHelpers() => BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public Font LoadFont(string path) => new(Path.Combine(BaseDirectory, path));
        public Texture LoadTexture(string path, bool repeated, bool smooth) => new(Path.Combine(BaseDirectory, path)) { Repeated = repeated, Smooth = smooth };
        public CachedSound LoadAudio(string path) => new(Path.Combine(BaseDirectory, path));
        public Image LoadImage(string path) => new(Path.Combine(BaseDirectory, path));
        public string GetModDataFilePath(string path)
        {
            return GetDataFilePath(Path.Combine("Mods", path));
        }
        public string GetDataFilePath(string path)
        {
            return new(Path.Combine(Path.Combine(BaseDirectory, "GameData"), path));
        }
        public T? Deserialize<T>(string file)
        {
            var serializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
            using var jsonTextReader = new JsonTextReader(new StreamReader(new FileStream(Path.Combine(BaseDirectory, file), FileMode.Open, FileAccess.Read)));
            return serializer.Deserialize<T>(jsonTextReader);
        }
        public Texture32 LoadSurface32(string path)
        {
            if (!File.Exists(path)) Logger.GetLogger().Log($"Missing file {path}");
            var img = LoadImage(path);
            return new Texture32((int)img.Size.X, (int)img.Size.Y, img.Pixels);
        }

        public bool Serialize<T>(T data, string file)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
            using var sw = new StreamWriter(new FileStream(Path.Combine(BaseDirectory, file), FileMode.Create, FileAccess.ReadWrite));
            using var writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, data);
            return File.Exists(Path.Combine(BaseDirectory, file));
        }

        
    }
}
