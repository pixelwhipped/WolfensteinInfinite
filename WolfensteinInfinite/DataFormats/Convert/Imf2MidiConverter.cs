//Clean
using System.IO;
using System.Text;

namespace WolfensteinInfinite.DataFormats.Convert
{
    public class Imf2MidiConverter
    {
        const ushort MIDI_PITCH_CENTER = 0x2000;
        const byte MIDI_CONTROLLER_VOLUME = 7;

        private static readonly ushort[] noteFrequencies =
            [
                345,
                363,
                385,
                408,
                432,
                458,
                485,
                514,
                544,
                577,
                611,
                647,
                686,
                731,
                774,
                820,
                869,
                921,
                975,
                1022,
                0
            ];

        private static readonly byte[] opl2OpChannel =
            [
                0,
                1,
                2,
                0,
                1,
                2,
                0,
                0,
                3,
                4,
                5,
                3,
                4,
                5,
                0,
                0,
                6,
                7,
                8,
                6,
                7,
                8,
                0
            ];

        private static readonly byte[] opl2Op =
            [
                0,
                0,
                0,
                1,
                1,
                1,
                0,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                0,
                0,
                0,
                0,
                0,
                1,
                1,
                1,
                0
            ];

        private readonly AdLibInstrument[] instruments = new AdLibInstrument[9];
        private readonly byte[] mapChannel = new byte[9];
        private readonly ushort[] lastPitch = new ushort[9];
        private readonly bool[] programChangesSent = new bool[9];
        private readonly byte[] activeNotes = new byte[9]; // Track which notes are actually playing
        private readonly bool[] noteIsActive = new bool[9]; // Track if a note is currently active

        private readonly MemoryStream output = new();
        private uint delta = 0;
        private byte eventCode = 255;
        private ushort tracksNum = 0;
        private uint trackBegin = 0;
        private readonly ushort resolution = 384;
        private readonly double tempo = 110.0;
        private readonly Dictionary<string, int> instMap = [];

        public Imf2MidiConverter() : this(AdLibInstrumentBank.GetDefaultInstruments())
        {
        }

        public Imf2MidiConverter(AdLibInstrument[] customInstruments)
        {
            if (customInstruments == null || customInstruments.Length != 9)
                throw new ArgumentException("Must provide exactly 9 instruments for the 9 OPL2 channels");

            for (int i = 0; i < 9; i++)
            {
                instruments[i] = customInstruments[i];
                mapChannel[i] = (byte)i;
                lastPitch[i] = MIDI_PITCH_CENTER;
                programChangesSent[i] = false;
                activeNotes[i] = 0;
                noteIsActive[i] = false;
            }
        }

        public Imf2MidiConverter(Dictionary<int, byte> channelToMidiProgram)
        {
            var defaultInstruments = AdLibInstrumentBank.GetDefaultInstruments();

            for (int i = 0; i < 9; i++)
            {
                instruments[i] = defaultInstruments[i];
                if (channelToMidiProgram.TryGetValue(i, out byte value))
                {
                    instruments[i].midiProgram = value;
                }
                mapChannel[i] = (byte)i;
                lastPitch[i] = MIDI_PITCH_CENTER;
                programChangesSent[i] = false;
                activeNotes[i] = 0;
                noteIsActive[i] = false;
            }
        }

        void Write8(byte b) => output.WriteByte(b);
        void WriteBE16(ushort v) { Write8((byte)(v >> 8)); Write8((byte)v); }
        void WriteBE32(uint v) { WriteBE16((ushort)(v >> 16)); WriteBE16((ushort)v); }
        void WriteBE24(uint v) { Write8((byte)(v >> 16)); Write8((byte)(v >> 8)); Write8((byte)v); }

        private void WriteVarLen(uint v)
        {
            var bytes = new List<byte>();
            bool first = true;
            for (int i = 3; i >= 0; i--)
            {
                byte b = (byte)(v >> 7 * i & 127);
                if (!first || b > 0 || i == 0)
                {
                    first = false;
                    if (i > 0) b |= 128;
                    bytes.Add(b);
                }
            }
            output.Write(bytes.ToArray());
        }

        private void AddDelta(uint d) => delta += d;

        private void WriteEventCode(byte code)
        {
            if (code != eventCode || code > 0x9f)
                Write8(code);
            eventCode = code;
        }

        private void WriteControlEvent(byte channel, byte controller, byte value)
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode((byte)(0xB0 + channel % 16));
            Write8(controller); Write8(value);
        }

        private void WriteProgramChangeEvent(byte channel, byte program)
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode((byte)(0xC0 + channel % 16));
            Write8(program);
        }

        private void WritePitchEvent(byte channel, ushort value)
        {
            channel = (byte)(channel % 9);
            if (lastPitch[channel] == value) return;

            WriteVarLen(delta); delta = 0;
            WriteEventCode((byte)(0xE0 + channel));
            Write8((byte)(value & 0x7F));
            Write8((byte)(value >> 7 & 0x7F));
            lastPitch[channel] = value;
        }

        private void WriteNoteEvent(byte channel, byte key, byte velocity, bool on)
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode((byte)((on ? 0x90 : 0x80) + channel % 16));
            Write8(key); Write8(velocity);
        }

        private void WriteTempoEvent(uint ticks)
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode(0xFF);
            Write8(0x51); Write8(0x03);
            WriteBE24(ticks);
        }

        private void WriteMetricKeyEvent(byte nom, byte denom, byte key1, byte key2)
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode(0xFF);
            Write8(0x58); Write8(0x04);
            Write8(nom); Write8((byte)(Math.Log(denom) / Math.Log(2)));
            Write8(key1); Write8(key2);
        }

        private void BeginTrack()
        {
            delta = 0; eventCode = 255;
            output.Write(Encoding.ASCII.GetBytes("MTrk"));
            trackBegin = (uint)output.Position;
            WriteBE32(0);
            tracksNum++;
        }

        private void EndTrack()
        {
            WriteVarLen(delta); delta = 0;
            WriteEventCode(0xFF);
            Write8(0x2f); Write8(0x00);

            uint pos = (uint)output.Position;
            output.Position = trackBegin;
            WriteBE32(pos - trackBegin - 4);
            output.Position = pos;
        }

        private static sbyte NearestFreq(ushort hz)
        {
            sbyte nearest = -1;
            ushort nearestDist = 0;
            for (sbyte i = 0; i < 21; i++)
            {
                ushort dist = (ushort)Math.Abs(hz - noteFrequencies[i]);
                if (i == 0 || dist < nearestDist)
                {
                    nearest = i; nearestDist = dist;
                }
            }
            return nearest;
        }

        private static short RelativeFreq(short i, short halfNotes)
        {
            short dir = (short)(halfNotes > 0 ? 1 : -1);
            if (i < 0 || noteFrequencies[i] == 0) return -1;

            while (i >= 0 && noteFrequencies[i] != 0 && halfNotes != 0)
            {
                halfNotes -= dir; i += dir;
            }
            return halfNotes == 0 ? (short)noteFrequencies[i] : (short)-1;
        }

        private static byte HzToKey(ushort hz, byte octave, byte multL, byte multH, byte wsL, byte wsH)
        {
            byte mult = (byte)(Math.Min(multL, multH) - wsL - wsH);
            octave++;
            if (mult == 0) octave--; else if (mult > 1) octave += (byte)(mult - 1);
            if (octave > 9) octave = 9;
            octave = (byte)(octave * 12);
            if (hz == 0) return 0;

            sbyte nearestIndex = NearestFreq(hz);
            return nearestIndex < 0 ? (byte)0 : (byte)(octave + nearestIndex);
        }

        private ushort MakePitch(short freq, byte channel)
        {
            short nextfreqIndex = NearestFreq((ushort)freq);
            short nextfreq = (short)noteFrequencies[nextfreqIndex];

            if (nextfreq == freq) return MIDI_PITCH_CENTER;
            if (freq == 0) return lastPitch[channel];

            if (nextfreq > freq)
            {
                short freqR = RelativeFreq(nextfreqIndex, 2);
                if (freqR >= 0)
                    return (ushort)(MIDI_PITCH_CENTER + 0x2000 * ((freq - nextfreq) / (double)(freqR - nextfreq)));
            }
            else
            {
                short freqR = RelativeFreq(nextfreqIndex, -2);
                if (freqR >= 0)
                    return (ushort)(MIDI_PITCH_CENTER - 0x2000 * ((nextfreq - freq) / (double)(nextfreq - freqR)));
            }
            return lastPitch[channel];
        }

        public byte[] Convert(byte[] imfData)
        {
            if (imfData.Length < 4) throw new ArgumentException("Invalid IMF file");

            uint length = BitConverter.ToUInt32(imfData, 0) - 4;
            int pos = 4;

            // MIDI Header
            output.Write(Encoding.ASCII.GetBytes("MThd"));
            WriteBE32(6); WriteBE16(0); WriteBE16(0); WriteBE16(resolution);

            BeginTrack();
            WriteTempoEvent((uint)(60000000.0 / tempo));
            WriteMetricKeyEvent(4, 4, 24, 8);

            // Set up initial volume and program changes for all channels
            for (byte c = 0; c < 9; c++)
            {
                WriteControlEvent(c, MIDI_CONTROLLER_VOLUME, 127);
                WriteProgramChangeEvent(c, instruments[c].midiProgram);
                programChangesSent[c] = true;
            }

            var freq = new ushort[9];
            var octs = new byte[9];
            var keySt = new bool[9];
            var keys = new byte[9];
            var pitchs = new ushort[9];
            Array.Fill(pitchs, MIDI_PITCH_CENTER);

            while (length > 0 && pos + 3 < imfData.Length)
            {
                ushort delay = (ushort)(imfData[pos] | imfData[pos + 1] << 8);
                byte regKey = imfData[pos + 2];
                byte regVal = imfData[pos + 3];
                pos += 4; length -= 4;

                if (delay > 0 || length == 0)
                {
                    for (byte c = 0; c < 9; c++)
                    {
                        // Send program change if not already sent for this channel
                        if (!programChangesSent[c])
                        {
                            WriteProgramChangeEvent(c, instruments[c].midiProgram);
                            programChangesSent[c] = true;
                        }

                        byte multL = (byte)(instruments[c].reg20[0] & 0x0F);
                        byte multH = (byte)(instruments[c].reg20[1] & 0x0F);
                        byte wsL = (byte)(instruments[c].regE0[0] & 0x07);
                        byte wsH = (byte)(instruments[c].regE0[1] & 0x07);

                        keys[c] = HzToKey(freq[c], octs[c], multL, multH, wsL, wsH);
                        pitchs[c] = MakePitch((short)freq[c], c);

                        // Fixed note state management logic
                        if (keySt[c]) // Key should be on
                        {
                            if (noteIsActive[c] && keys[c] != activeNotes[c])
                            {
                                // Different note, turn off the old one first
                                WriteNoteEvent(mapChannel[c], activeNotes[c], 0, false);
                                noteIsActive[c] = false;
                            }

                            if (!noteIsActive[c] || keys[c] != activeNotes[c])
                            {
                                // Turn on the new note
                                WritePitchEvent(mapChannel[c], pitchs[c]);
                                byte vel = (byte)(instruments[c].reg40[0] & 0x3F);
                                WriteNoteEvent(mapChannel[c], keys[c], (byte)(0x3f - vel << 1), true);
                                activeNotes[c] = keys[c];
                                noteIsActive[c] = true;
                            }
                        }
                        else // Key should be off
                        {
                            if (noteIsActive[c])
                            {
                                WriteNoteEvent(mapChannel[c], activeNotes[c], 0, false);
                                noteIsActive[c] = false;
                                activeNotes[c] = 0;
                            }
                        }
                    }
                    AddDelta(delay);
                }

                byte channel;
                if (regKey >= 0xA0 && regKey <= 0xA8)
                {
                    channel = (byte)(regKey - 0xA0);
                    freq[channel] = (ushort)(freq[channel] & 0x0F00 | regVal);
                }
                else if (regKey >= 0xB0 && regKey <= 0xB8)
                {
                    channel = (byte)(regKey - 0xB0);
                    freq[channel] = (ushort)(freq[channel] & 0x00FF | (regVal & 0x03) << 8);
                    octs[channel] = (byte)(regVal >> 2 & 0x07);
                    keySt[channel] = (regVal >> 5 & 1) != 0;
                }
                else if (regKey >= 0x20 && regKey <= 0x35)
                {
                    channel = opl2OpChannel[(regKey - 0x20) % 0x15];
                    instruments[channel].reg20[opl2Op[(regKey - 0x20) % 0x15]] = regVal;
                }
                else if (regKey >= 0x40 && regKey <= 0x55)
                {
                    byte op = opl2Op[(regKey - 0x40) % 0x15];
                    channel = opl2OpChannel[(regKey - 0x40) % 0x15];
                    instruments[channel].reg40[op] = regVal;
                }
                else if (regKey >= 0x60 && regKey <= 0x75)
                {
                    channel = opl2OpChannel[(regKey - 0x60) % 0x15];
                    instruments[channel].reg60[opl2Op[(regKey - 0x60) % 0x15]] = regVal;
                }
                else if (regKey >= 0x80 && regKey <= 0x95)
                {
                    channel = opl2OpChannel[(regKey - 0x80) % 0x15];
                    instruments[channel].reg80[opl2Op[(regKey - 0x80) % 0x15]] = regVal;
                }
                else if (regKey >= 0xC0 && regKey <= 0xC8)
                {
                    channel = (byte)(regKey - 0xC0);
                    instruments[channel].regC0 = regVal;
                }
                else if (regKey >= 0xE0 && regKey <= 0xF5)
                {
                    channel = opl2OpChannel[(regKey - 0xE0) % 0x15];
                    instruments[channel].regE0[opl2Op[(regKey - 0xE0) % 0x15]] = regVal;
                }
            }

            // Stop all active notes
            for (byte c = 0; c < 9; c++)
            {
                if (noteIsActive[c])
                {
                    WriteNoteEvent(mapChannel[c], activeNotes[c], 0, false);
                }
            }

            EndTrack();

            // Update track count in header
            output.Position = 10;
            WriteBE16(tracksNum);

            return output.ToArray();
        }
    }
}