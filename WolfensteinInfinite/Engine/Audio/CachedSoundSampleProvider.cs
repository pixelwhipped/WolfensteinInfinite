using NAudio.Wave;

namespace WolfensteinInfinite.Engine.Audio
{
    public class CachedSoundSampleProvider(CachedSound cachedSound) : ISampleProvider
    {
        private readonly CachedSound CachedSound = cachedSound;
        private long Position;

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = CachedSound.AudioData.Length - Position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(CachedSound.AudioData, Position, buffer, offset, samplesToCopy);
            Position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return CachedSound.WaveFormat; } }
    }
}
