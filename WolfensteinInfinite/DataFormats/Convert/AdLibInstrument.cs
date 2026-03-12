//Clean
namespace WolfensteinInfinite.DataFormats.Convert
{
    public struct AdLibInstrument(byte midiProgram = 0)
    {
        public byte[] reg20 = new byte[2]; // [2]
        public byte[] reg40 = new byte[2]; // [2] 
        public byte[] reg60 = new byte[2]; // [2]
        public byte[] reg80 = new byte[2]; // [2]
        public byte regC0 = 0;
        public byte[] regE0 = new byte[2]; // [2]
        public byte midiProgram = midiProgram; // MIDI program number (0-127)
    }
}