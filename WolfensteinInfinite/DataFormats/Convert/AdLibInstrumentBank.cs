//Clean
namespace WolfensteinInfinite.DataFormats.Convert
{
    public static class AdLibInstrumentBank
    {
        // Predefined AdLib instrument definitions with MIDI program mappings
        public static readonly Dictionary<string, AdLibInstrument> Instruments = new()
        {
            ["Piano"] = new AdLibInstrument(GeneralMidiInstruments.Piano)
            {
                reg20 = [0x01, 0x01],
                reg40 = [0x10, 0x10],
                reg60 = [0xF0, 0xF0],
                reg80 = [0x77, 0x77],
                regC0 = 0x01,
                regE0 = [0x00, 0x00]
            },
            ["Organ"] = new AdLibInstrument(GeneralMidiInstruments.DrawbarOrgan)
            {
                reg20 = [0x31, 0x31],
                reg40 = [0x00, 0x00],
                reg60 = [0xF0, 0xF0],
                reg80 = [0x54, 0x74],
                regC0 = 0x01,
                regE0 = [0x00, 0x00]
            },
            ["Trumpet"] = new AdLibInstrument(55) // Trumpet
            {
                reg20 = [0x21, 0x21],
                reg40 = [0x15, 0x00],
                reg60 = [0x99, 0x8A],
                reg80 = [0x46, 0x7A],
                regC0 = 0x00,
                regE0 = [0x00, 0x00]
            },
            ["Flute"] = new AdLibInstrument(73) // Flute
            {
                reg20 = [0x01, 0x01],
                reg40 = [0x1F, 0x15],
                reg60 = [0x40, 0x73],
                reg80 = [0x35, 0x16],
                regC0 = 0x00,
                regE0 = [0x00, 0x00]
            },
            ["Strings"] = new AdLibInstrument(GeneralMidiInstruments.Violin)
            {
                reg20 = [0x31, 0x31],
                reg40 = [0x16, 0x16],
                reg60 = [0x99, 0x99],
                reg80 = [0x72, 0x72],
                regC0 = 0x01,
                regE0 = [0x00, 0x00]
            },
            ["Bass"] = new AdLibInstrument(GeneralMidiInstruments.AcousticBass)
            {
                reg20 = [0x01, 0x00],
                reg40 = [0x1A, 0x0F],
                reg60 = [0x85, 0xCA],
                reg80 = [0x4D, 0x4D],
                regC0 = 0x00,
                regE0 = [0x00, 0x00]
            }
        };

        public static AdLibInstrument[] GetDefaultInstruments()
        {
            return
            [
                Instruments["Piano"],
                Instruments["Organ"],
                Instruments["Trumpet"],
                Instruments["Flute"],
                Instruments["Strings"],
                Instruments["Bass"],
                Instruments["Piano"], // Channel 6
                Instruments["Piano"], // Channel 7
                Instruments["Piano"]  // Channel 8
            ];
        }
    }
}