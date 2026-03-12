//Clean
using NAudio.Wave;

namespace WolfensteinInfinite.Engine.Audio
{
    public class AutoDisposeFileReader(AudioFileReader reader) : ISampleProvider
    {
        private readonly AudioFileReader Reader = reader;
        private bool IsDisposed;

        public int Read(float[] buffer, int offset, int count)
        {
            if (IsDisposed)
                return 0;
            int read = Reader.Read(buffer, offset, count);
            if (read == 0)
            {
                Reader.Dispose();
                IsDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; private set; } = reader.WaveFormat;
    }
}
