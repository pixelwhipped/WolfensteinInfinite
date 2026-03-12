//Clean
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace WolfensteinInfinite.Engine.Audio
{
    public class AudioPlaybackEngine
    {
        private Thread? MidiThread;
        private readonly OutputDevice MidiOutputDevice;
        private Playback? MidiPlayback;
        public Dictionary<int, AudioSampleMixer> SampleMixers = [];
        private float _soundVolume;
        public float SoundVolume
        {
            get
            {
                return _soundVolume;
            }
            set
            {
                _soundVolume = Math.Clamp(value, 0f, 1f);
                foreach (var m in SampleMixers) { m.Value.OutputDevice.Volume = _soundVolume; }
            }
        }
        public bool SoundOn { get; set; }
        public float MusicVolume { get; set; }
        public bool _musicOn;
        public bool MusicOn { get => _musicOn;
            set
            {
                if (!value) StopMusic();
                _musicOn = value;
            }
        }
        public AudioPlaybackEngine()//int sampleRate = 44100, int channelCount = 2)
        {
            MidiOutputDevice = OutputDevice.GetAll().ToArray()[0];
        }
        private AudioSampleMixer GetMixer(int sampleRate)
        {
            if (SampleMixers.TryGetValue(sampleRate, out AudioSampleMixer? mixer)) return mixer;

            var od = new WaveOutEvent();
            var m = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2))
            {
                ReadFully = true
            };
            od.Init(m);
            od.Play();
            mixer = new(od, m);
            SampleMixers.Add(sampleRate, mixer);
            return mixer;
        }

        public void PlaySound(string fileName)
        {
            if (!SoundOn) return;
            var input = new AudioFileReader(fileName);
            AddMixerInput(new AutoDisposeFileReader(input));
        }

        private static ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == 2)
            {
                return input;
            }
            return new MonoToStereoSampleProvider(input);
        }

        public void PlaySound(CachedSound? sound)
        {
            if (!SoundOn || sound == null) return;
            AddMixerInput(new CachedSoundSampleProvider(sound));
        }

        private void AddMixerInput(ISampleProvider input)
        {
            var mixer = GetMixer(input.WaveFormat.SampleRate);
            mixer.Mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }
        public bool IsMusicPlaying => MidiPlayback != null && MidiPlayback.IsRunning;

        public void StopMusic()
        {
            MidiPlayback?.Stop();
            while (MidiThread != null && MidiThread.IsAlive)
                Thread.Sleep(200);
        }
        public void PlayMusic(string filePath)
        {
            var m = MidiFile.Read(filePath);
            PlayMusic(m);
        }
        public void PlayMusic(MidiFile midiFile)
        {
            StopMusic();
            MidiThread = new Thread(() =>
            {
                MidiPlayback = midiFile.GetPlayback(MidiOutputDevice);
                MidiPlayback.NoteCallback = UpdateNote;
                MidiPlayback.Loop = true;
                MidiPlayback.Start();
                while (MidiPlayback.IsRunning)
                    Thread.Sleep(200);
            });
            MidiThread.Start();
        }

        private NotePlaybackData UpdateNote(NotePlaybackData rawNoteData, long rawTime, long rawLength, TimeSpan playbackTime)
        {
            //rawNoteData.Velocity = new Melanchall.DryWetMidi.Common.SevenBitNumber(0);
            var v = (byte)Math.Clamp(rawNoteData.Velocity * MusicVolume,0,127);
            var velocity = new Melanchall.DryWetMidi.Common.SevenBitNumber(v);
            return new NotePlaybackData(rawNoteData.NoteNumber, velocity, rawNoteData.OffVelocity, rawNoteData.Channel);
        }

        public void ShutDown()
        {
            foreach (var m in SampleMixers)
            {
                m.Value.OutputDevice.Dispose();
            }
            StopMusic();
        }

        public static readonly AudioPlaybackEngine Instance = new();
    }
}
