using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace WolfensteinInfinite.GameAudio
{
    public record AudioSampleMixer(IWavePlayer OutputDevice, MixingSampleProvider Mixer);
}
