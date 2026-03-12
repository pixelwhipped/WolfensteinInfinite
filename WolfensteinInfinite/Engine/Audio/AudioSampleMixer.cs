//Clean
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace WolfensteinInfinite.Engine.Audio
{
    public record AudioSampleMixer(IWavePlayer OutputDevice, MixingSampleProvider Mixer);
}
