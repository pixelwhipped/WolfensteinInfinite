using SFML.Window;

namespace WolfensteinInfinite
{
    public class Config(HighScore[] highScores,bool sound, int soundVolume, bool music, int musicVolume,
        Keyboard.Key keyUp, Keyboard.Key keyDown, Keyboard.Key keyLeft, Keyboard.Key keyRight,
        Keyboard.Key keyFire, Keyboard.Key keyOpen, Keyboard.Key keyStafe, Keyboard.Key keyWeaponUp, Keyboard.Key keyWeaponDown,
        Keyboard.Key keyMap, Keyboard.Key keyPause, int windowSize, int resolution, int quantization, bool lightBlur, int maxMapSize, ModConfig[] mods, bool weaponBob)
    {
        public bool LightBlur { get; set; } = lightBlur;
        //needs to be name/level/score
        public HighScore[] HighScores { get; set; } = highScores;
        public bool Sound { get; set; } = sound;
        public int SoundVolume { get; set; } = soundVolume;
        public bool Music { get; set; } = music;
        public int MusicVolume { get; set; } = musicVolume;        
        public Keyboard.Key KeyUp { get; set; } = keyUp;
        public Keyboard.Key KeyDown { get; set; } = keyDown;
        public Keyboard.Key KeyLeft { get; set; } = keyLeft;
        public Keyboard.Key KeyRight { get; set; } = keyRight;
        public Keyboard.Key KeyFire { get; set; } = keyFire;
        public Keyboard.Key KeyOpen { get; set; } = keyOpen;
        public Keyboard.Key KeyStafe { get; set; } = keyStafe;
        public Keyboard.Key KeyWeaponUp { get; set; } = keyWeaponUp;
        public Keyboard.Key KeyWeaponDown { get; set; } = keyWeaponDown;
        public Keyboard.Key KeyMap { get; set; } = keyMap;
        public Keyboard.Key KeyPause { get; set; } = keyPause;
        public int WindowSize { get; set; } = windowSize;
        public int Resolution { get; set; } = resolution;
        public int Quantization { get; set; } = quantization;
        public int MaxMapSize { get; set; } = maxMapSize;
        public bool WeaponBob { get; set; } = weaponBob;
        public ModConfig[] Mods { get; set; } = mods;
        public static Config GetDefault()
        {
            return new Config(                
                [
                    new(Guid.Empty,"id software-'92", 1, 10000),
                    new(Guid.Empty, "Adrian Carmack", 1, 10000),
                    new(Guid.Empty, "John Carmack", 1, 10000),
                    new(Guid.Empty, "Kevin Cloud", 1, 10000),
                    new(Guid.Empty, "Tom Hall", 1, 10000),
                    new(Guid.Empty, "John Romero", 1, 10000),
                    new(Guid.Empty, "Jay Wilbur", 1, 10000)
                ],true,100,true,100, Keyboard.Key.Up, Keyboard.Key.Down, Keyboard.Key.Left, Keyboard.Key.Right,
                Keyboard.Key.LControl, Keyboard.Key.Space, Keyboard.Key.LAlt, Keyboard.Key.Comma, Keyboard.Key.Period,
                Keyboard.Key.Tab, Keyboard.Key.Pause, 2, 0, 2, false, 128, [], false
                );
        }
    }
}
