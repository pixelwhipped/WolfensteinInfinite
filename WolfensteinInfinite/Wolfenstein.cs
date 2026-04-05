using Melanchall.DryWetMidi.Core;
using SFML.System;
using SFML.Window;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using WolfensteinInfinite.DataFormats;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameHelpers;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite
{
    public class Wolfenstein
    {
        public App? Application;
        public Graphics Graphics;
        public bool IsRunning => Graphics.IsOpen && CurrentState != null;
        public Random Random = new();
        public GameResources GameResources { get; init; }
        public readonly Clock Clock;
        private Texture32 Buffer;
        private Time LastTime;
        private float FrameTime;
        public float UIScale { get; set; } = 1f;
        private Action DoRender;
        public Dictionary<string, Mod> Mods { get; private set; }
        public Dictionary<string, MapBuilder> BuilderMods { get; init; }
        public Dictionary<string, MapBuilder> SpecialMaps { get; init; }
        public Dictionary<string, MapBuilder> TestMaps { get; init; }
        public (string mod, MapSection section)[]? TestMapSections { get; init; }
        public Dictionary<string, Dictionary<string, Texture32>> ExperimentalEnemyTexture { get; init; } = [];
        public Dictionary<string, Dictionary<int, Texture32>> Textures { get; init; } = [];
        public Dictionary<string, Dictionary<int, Texture32>> Decals { get; init; } = [];
        public Dictionary<string, Dictionary<int, CharacterSprite>> CharacterSprites { get; init; } = [];
        public Dictionary<string, Dictionary<string, ProjectileSprite>> ProjectileSprites { get; init; } = [];
        public Dictionary<string, Dictionary<string, Animation>> SpriteAnimations { get; init; } = [];
        public Dictionary<string, PlayerWeapon> PlayerWeapons { get; init; } = [];
        public Dictionary<string, WeaponAnimation> WeaponAnimations { get; init; } = [];
        public Dictionary<string, Texture32> WeaponHudTextures { get; init; } = [];
        public Dictionary<string, CachedSound> WeaponAudio { get; init; } = [];
        public Dictionary<string, CachedSound> EnemySounds { get; init; } = [];
        public Dictionary<int, PickupItem> PickupItemTypes { get; init; } = [];
        public Dictionary<int, Texture32> PickupItems { get; init; } = [];
        public Dictionary<int, DoorType> Doors { get; init; } = [];
        public Dictionary<int, Texture32> Special { get; init; } = [];
        private readonly Dictionary<string, MidiFile> _musicCache = [];
        public GameState? CurrentState { get; set; }
        public List<MidiFile> TitleScreenMusic { get; init; } = [];
        public MidiFile LevelCompleteMusic { get; init; }
        public MidiFile? CurrentMusic { get; set; }
        public Config Config { get; init; }

        public Dictionary<string, Texture32> Floors { get; init; } = [];
        public Dictionary<string, Texture32> Ceilings { get; init; } = [];
        public Wolfenstein(App app)
        {
            Log("Cracking knuckles");
            Application = app;
            Log("Remebering what I was doing");
            Config = LoadConfig();
            Log("Remebering who I am");
            LoadBaseModItems(out Animation pow, out Texture32 bloodPool);
            BloodPool = bloodPool;
            POWAnimation = pow;
            Mods = LoadMods(false);
            BuilderMods = LoadBuilderMods();
            SpecialMaps = LoadSpecialMaps();
            TestMaps = LoadTestMaps();
            TestMapSections = LoadTestMapSections();

            AudioPlaybackEngine.Instance.SoundOn = Config.Sound;
            AudioPlaybackEngine.Instance.SoundVolume = Config.SoundVolume / 100f;
            AudioPlaybackEngine.Instance.MusicOn = Config.Music;
            AudioPlaybackEngine.Instance.MusicVolume = Config.MusicVolume / 100f;
            Log("Remembering to good tunes");
            LevelCompleteMusic = MidiFile.Read("GameData\\Base\\Music\\Complete.mid");
            foreach (var m in Mods.Values)
            {
                if (string.IsNullOrWhiteSpace(m.TitleMusic)) continue;
                var midi = m.MusicTracks.FirstOrDefault(p => p.Name == m.TitleMusic);
                if (midi != null) TitleScreenMusic.Add(MidiFile.Read(FileHelpers.Shared.GetModDataFilePath(midi.File)));
            }
            if (TitleScreenMusic.Count > 0)
                CurrentMusic = TitleScreenMusic[Random.Shared.Next(0, TitleScreenMusic.Count - 1)];

            foreach (var m in Mods.Values)
                foreach (var track in m.MusicTracks)
                {
                    var path = FileHelpers.Shared.GetModDataFilePath(track.File);
                    if (!_musicCache.ContainsKey(path))
                        _musicCache[path] = MidiFile.Read(path);
                }

            var pallet = Pallets.ToByteArray(Pallets.Wolfenstein3D);
            GameResources = new GameResources(pallet);
            AddSpecialTextures();
            Clock = new Clock();
            LastTime = Clock.ElapsedTime;
            ResetGraphics(out Graphics, out Buffer, out DoRender);
            CurrentState = new TitleScreen(this);
            if (CurrentMusic != null)
            {
                Log("Listening to good tunes");
                AudioPlaybackEngine.Instance.PlayMusic(CurrentMusic);
            }
        }

        public void PlayMusic(string filePath)
        {
            if (!AudioPlaybackEngine.Instance.MusicOn) return;
            if (!_musicCache.TryGetValue(filePath, out var midi))
            {
                midi = MidiFile.Read(filePath);
                _musicCache[filePath] = midi;
            }
            CurrentMusic = midi;
            AudioPlaybackEngine.Instance.PlayMusic(midi);
        }

        public void PlayMusic(MidiFile midi)
        {
            if (!AudioPlaybackEngine.Instance.MusicOn) return;
            CurrentMusic = midi;
            AudioPlaybackEngine.Instance.PlayMusic(midi);
        }

        public void PlayLevelMusic(IEnumerable<string> modNames)
        {
            var tracks = modNames
                .Where(n => Mods.TryGetValue(n, out _))
                .SelectMany(n => Mods[n].MusicTracks)
                .ToList();

            if (tracks.Count == 0)
            {
                // Fall back to current music if no level tracks
                if (CurrentMusic != null)
                    PlayMusic(CurrentMusic);
                return;
            }

            var track = tracks[Random.Shared.Next(tracks.Count)];
            PlayMusic(FileHelpers.Shared.GetModDataFilePath(track.File));
        }

        public void PlayTitleMusic()
        {
            if (TitleScreenMusic.Count == 0) return;
            PlayMusic(TitleScreenMusic[Random.Shared.Next(TitleScreenMusic.Count)]);
        }

        public void PlayLevelCompleteMusic() => PlayMusic(LevelCompleteMusic);
        private (string mod, MapSection section)[]? LoadTestMapSections()
        {
            if (!Args.TestMode) return null;

            var mods = Config.Mods.Where(p => p.Enabled);
            var modBuilders = TestMaps
                .Where(p => mods.Any(mo => mo.Name == p.Key) && p.Value.MapSections.Length > 0)
                .ToArray();
            if (modBuilders.Length == 0) return null;
            var t = new List<(string mod, MapSection section)>();
            foreach (var builder in modBuilders)
            {
                foreach (var section in builder.Value.MapSections)
                {
                    t.Add((builder.Key, section));
                }
            }
            return [.. t];
        }

        private void LoadBaseModItems(out Animation pow, out Texture32 bloodPool)
        {
            PickupItemTypes.Add(0, new PickupItem("Clip", PickupItemType.AMMO, 8, 0, "GameData\\Base\\Sprites\\Ammo.png", AmmoType.BULLET));
            PickupItemTypes.Add(1, new PickupItem("UsedClip", PickupItemType.AMMO, 4, 0, "GameData\\Base\\Sprites\\Ammo.png", AmmoType.BULLET));
            PickupItemTypes.Add(2, new PickupItem("AmmoBox", PickupItemType.AMMO, 25, 0, "GameData\\Base\\Sprites\\AmmoBox.png", AmmoType.BULLET));
            PickupItemTypes.Add(3, new PickupItem("MachineGun", PickupItemType.WEAPON, 6, 0, "GameData\\Base\\Sprites\\MachineGun.png", null));
            PickupItemTypes.Add(4, new PickupItem("ChainGun", PickupItemType.WEAPON, 0, 0, "GameData\\Base\\Sprites\\ChainGun.png", null));
            PickupItemTypes.Add(5, new PickupItem("Cross", PickupItemType.POINTS, 100, 0, "GameData\\Base\\Sprites\\Cross.png", null));
            PickupItemTypes.Add(6, new PickupItem("Chalice", PickupItemType.POINTS, 500, 0, "GameData\\Base\\Sprites\\Chalice.png", null));
            PickupItemTypes.Add(7, new PickupItem("Chest", PickupItemType.POINTS, 1000, 0, "GameData\\Base\\Sprites\\Chest.png", null));
            PickupItemTypes.Add(8, new PickupItem("Crown", PickupItemType.POINTS, 5000, 0, "GameData\\Base\\Sprites\\Crown.png", null));
            PickupItemTypes.Add(9, new PickupItem("DogFood", PickupItemType.HEALTH, 4, 0, "GameData\\Base\\Sprites\\DogFood.png", null));
            PickupItemTypes.Add(10, new PickupItem("Food", PickupItemType.HEALTH, 10, 0, "GameData\\Base\\Sprites\\Food.png", null));
            PickupItemTypes.Add(11, new PickupItem("HealthKit", PickupItemType.HEALTH, 25, 0, "GameData\\Base\\Sprites\\HealthKit.png", null));
            PickupItemTypes.Add(12, new PickupItem("Blood", PickupItemType.HEALTH, 1, 11, "GameData\\Base\\Sprites\\Blood.png", null));
            PickupItemTypes.Add(13, new PickupItem("BloodBones", PickupItemType.HEALTH, 1, 11, "GameData\\Base\\Sprites\\BloodBones.png", null));
            PickupItemTypes.Add(14, new PickupItem("OneUp", PickupItemType.LIFE, 1, 0, "GameData\\Base\\Sprites\\Life.png", null));
            PickupItemTypes.Add(15, new PickupItem("POW", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\0.png", null));
            PickupItemTypes.Add(16, new PickupItem("Secret", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\Secret.png", null));
            PickupItemTypes.Add(17, new PickupItem("Radio", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\Radio.png", null));
            PickupItemTypes.Add(18, new PickupItem("Dynamite", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\Dynamite.png", null));
            PickupItemTypes.Add(19, new PickupItem("DynamiteToPlace", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\DynamiteToPlace.png", null));
            PickupItemTypes.Add(20, new PickupItem("DynamitePlaced", PickupItemType.SPAWNER, 0, 0, "GameData\\Base\\Sprites\\DynamitePlaced.png", null));
            PickupItemTypes.Add(21, new PickupItem("Key", PickupItemType.MISSION_OBJECTIVE, 0, 0, "GameData\\Base\\Sprites\\Key.png", null));
            PickupItemTypes.Add(22, new PickupItem("Adolf Hitler", PickupItemType.SPAWNER, 0, 0, "GameData\\Base\\Sprites\\Empty.png", null));
            PickupItemTypes.Add(23, new PickupItem("GodMode", PickupItemType.GODMODE, 1, 0, "GameData\\Base\\Sprites\\God.png", null));
            PickupItemTypes.Add(24, new PickupItem("Gas", PickupItemType.AMMO, 25, 0, "GameData\\Base\\Sprites\\Gas.png", AmmoType.FLAME));
            PickupItemTypes.Add(25, new PickupItem("Rockets", PickupItemType.AMMO, 3, 0, "GameData\\Base\\Sprites\\Rockets.png", AmmoType.ROCKET));
            PickupItemTypes.Add(26, new PickupItem("Backpack", PickupItemType.BACKPACK, 3, 0, "GameData\\Base\\Sprites\\Backpack.png", null));
            PickupItemTypes.Add(27, new PickupItem("RocketLauncher", PickupItemType.WEAPON, 1, 3, "GameData\\Base\\Sprites\\RocketLauncher.png", null));
            PickupItemTypes.Add(28, new PickupItem("FlameThrower", PickupItemType.WEAPON, 1, 25, "GameData\\Base\\Sprites\\FlameThrower.png", null));
            PickupItemTypes.Add(29, new PickupItem("RandomItem", PickupItemType.SPECIAL, 1, 0, "GameData\\Base\\Pictures\\RandomItem.png", null));
            PickupItemTypes.Add(30, new PickupItem("RandomWeapon", PickupItemType.SPECIAL, 1, 0, "GameData\\Base\\Pictures\\RandomWeapon.png", null));


            foreach (var i in PickupItemTypes)
            {
                if (i.Value.SpritePath != null)
                    PickupItems.Add(i.Key, FileHelpers.Shared.LoadSurface32(i.Value.SpritePath));
            }

            // maxFireTime=0 makes it single-shot
            PlayerWeapons.Add("Knife", new PlayerWeapon("Knife", 0, WeaponType.KNIFE, AmmoType.MELEE, null, 9, 3, 0, 0f, "GameData\\Base\\Pictures\\HudKnife.png", "GameData\\Base\\Sprites", 416, 5, 2, 1, 4));
            PlayerWeapons.Add("Pistol", new PlayerWeapon("Pistol", 10, WeaponType.PISTOL, AmmoType.BULLET, "GameData\\Base\\Sounds\\Pistol.wav", 18, 3, 0, 0f, "GameData\\Base\\Pictures\\HudPistol.png", "GameData\\Base\\Sprites", 421, 5, 2, 1, 4));
            PlayerWeapons.Add("MachineGun", new PlayerWeapon("MachineGun", 20, WeaponType.MACHINE_GUN, AmmoType.BULLET, "GameData\\Base\\Sounds\\MachineGun.wav", 12, 3, 1f, 4f, "GameData\\Base\\Pictures\\HudMachineGun.png", "GameData\\Base\\Sprites", 426, 5, 2, 2, 3));
            PlayerWeapons.Add("ChainGun", new PlayerWeapon("ChainGun", 30, WeaponType.CHAIN_GUN, AmmoType.BULLET, "GameData\\Base\\Sounds\\ChainGun.wav", 11, 4, 1f, 3f, "GameData\\Base\\Pictures\\HudChainGun.png", "GameData\\Base\\Sprites", 431, 5, 2, 2, 3));            
            PlayerWeapons.Add("RocketLauncher", new PlayerWeapon("RocketLauncher", 40, WeaponType.ROCKET_LAUNCHER, AmmoType.ROCKET, "GameData\\Base\\Sounds\\Rocket.wav", 11, 3, 2f, 1f, "GameData\\Base\\Pictures\\HudRocketLauncher.png", "GameData\\Base\\Sprites", 1105, 4, 2, 1, 3));
            PlayerWeapons.Add("FlameThrower", new PlayerWeapon("FlameThrower", 50, WeaponType.FLAME_THROWER, AmmoType.FLAME, "GameData\\Base\\Sounds\\Flame.wav", 11, 8, 1f, 4f, "GameData\\Base\\Pictures\\HudFlameThrower.png", "GameData\\Base\\Sprites", 1100, 5, 2, 1, 4));

            WeaponAnimations.Add("Knife", AnimationHelpers.Create(PlayerWeapons["Knife"]));
            WeaponAnimations.Add("Pistol", AnimationHelpers.Create(PlayerWeapons["Pistol"]));
            WeaponAnimations.Add("MachineGun", AnimationHelpers.Create(PlayerWeapons["MachineGun"]));
            WeaponAnimations.Add("ChainGun", AnimationHelpers.Create(PlayerWeapons["ChainGun"]));
            WeaponAnimations.Add("RocketLauncher", AnimationHelpers.Create(PlayerWeapons["RocketLauncher"]));
            WeaponAnimations.Add("FlameThrower", AnimationHelpers.Create(PlayerWeapons["FlameThrower"]));

            pow = new Animation(
            [
                FileHelpers.Shared.LoadSurface32("GameData\\Base\\Sprites\\0.png"),
                FileHelpers.Shared.LoadSurface32("GameData\\Base\\Sprites\\1.png"),
                FileHelpers.Shared.LoadSurface32("GameData\\Base\\Sprites\\2.png"),
                FileHelpers.Shared.LoadSurface32("GameData\\Base\\Sprites\\4.png"),
            ], 1, 4, 4);

            foreach (var w in PlayerWeapons.Values)
            {
                if (w.Sound != null)
                    WeaponAudio.Add(w.Name, FileHelpers.Shared.LoadAudio(w.Sound));
                WeaponHudTextures.Add(w.Name, FileHelpers.Shared.LoadSurface32(PlayerWeapons[w.Name].HudSprite));
            }

            Doors.Add(0, new DoorType(0, DoorTypes.NORMAL, FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\Door.png"), FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\DoorSide.png")));
            Doors.Add(1, new DoorType(1, DoorTypes.ELEVATOR, FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\ElevatorDoor.png"), FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\LockedDoorSide.png")));
            Doors.Add(2, new DoorType(2, DoorTypes.LOCKED, FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\LockedDoor.png"), FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\LockedDoorSide.png")));
            Doors.Add(3, new DoorType(3, DoorTypes.PRISON, FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\CellDoor.png"), FileHelpers.Shared.LoadSurface32("GameData\\Base\\Textures\\LockedDoorSide.png")));

            bloodPool = FileHelpers.Shared.LoadSurface32("GameData\\Base\\Sprites\\BloodPool.png");

        }
        private void AddSpecialTextures()
        {

            foreach (var mod in Mods)
            {
                var t = new List<Texture>(mod.Value.Textures)
                {
                    new(1001, "ElevatorDoor", "GameData\\Base\\Textures\\ElevatorDoor.png",-1),
                    new(1002, "ElevatorSide", "GameData\\Base\\Textures\\ElevatorSide.png",-1),
                    new(1003, "ElevatorSwitch", "GameData\\Base\\Textures\\ElevatorUp.png",-1)
                };
                Textures[mod.Key].Add(1001, GameResources.ElevatorDoor);
                Textures[mod.Key].Add(1002, GameResources.ElevatorSide);
                Textures[mod.Key].Add(1003, GameResources.ElevatorSwitchUp);
                mod.Value.Textures = [.. t];

            }
            Special.Add(0, GameResources.PlayerStart);
            Special.Add(1, GameResources.EditRandomEnemy); //Move to enemy 1001
            Special.Add(2, GameResources.EditExperimentEnemy); //Move to enemy 1002
            Special.Add(3, GameResources.EditExit);
            Special.Add(4, GameResources.EditNorth);
            Special.Add(5, GameResources.EditEast);
            Special.Add(6, GameResources.EditSouth);
            Special.Add(7, GameResources.EditWest);
            Special.Add(8, GameResources.EditWallAny);
            Special.Add(9, GameResources.Chance5);
            Special.Add(10, GameResources.Chance25);
            Special.Add(11, GameResources.Chance50);
            Special.Add(12, GameResources.Chance75);
            
        }

        private static void Log(string message) => Logger.GetLogger().Log(message);
        public void ResetGraphics(out Graphics graphics, out Texture32 buffer, out Action doRender)
        {
            var parameters = new SystemParameters();
            switch (Config.WindowSize)
            {
                case 0:
                    {
                        parameters.WindowWidth = 320;
                        parameters.WindowHeight = 200;
                        parameters.Fullscreen = false;
                        break;
                    }
                case 1:
                    {
                        parameters.WindowWidth = 640;
                        parameters.WindowHeight = 400;
                        parameters.Fullscreen = false;
                        break;
                    }
                case 2:
                    {
                        parameters.WindowWidth = (int)VideoMode.DesktopMode.Width;
                        parameters.WindowHeight = (int)VideoMode.DesktopMode.Height;
                        parameters.Fullscreen = true;
                        break;
                    }
            }

            switch (Config.Quantization)
            {
                case 0:
                    {
                        doRender = DoRender = new Action(() => RenderQuantize(63));
                        break;
                    }
                case 1:
                    {
                        doRender = DoRender = new Action(() => RenderQuantize(127));
                        break;
                    }
                case 2:
                    {
                        doRender = DoRender = new Action(() => RenderQuantize(256));
                        break;
                    }
                default:
                    {
                        doRender = DoRender = new Action(() => RenderQuantize(256));
                        break;
                    }
            }
            switch (Config.Resolution)
            {
                case 0:
                    {
                        parameters.Width = 320;
                        parameters.Height = 200;
                        break;
                    }
                case 1:
                    {
                        parameters.Width = 640;
                        parameters.Height = 400;
                        break;
                    }
            }
            UIScale = parameters.Width / 320f;
            var pallet = Pallets.ToByteArray(Pallets.Wolfenstein3D);
            Graphics?.ShutDown();
            graphics = Graphics = new Graphics(parameters, GameResources.WindowIcon, "Wolfenstein 3D Infinite", GameResources.DebugFont, pallet)
            {
                ShowFPS = false
            };

            buffer = Buffer = new Texture32(Graphics.Width, Graphics.Height);

            Graphics.KeyPressed += (k) => CurrentState?.OnKeyPressed(k);
            Graphics.KeyReleased += (k) => CurrentState?.OnKeyReleased(k);
        }
        private static Config LoadConfig()
        {
            var file = Path.Combine(FileHelpers.Shared.BaseDirectory, "config.json");
            if (!File.Exists(file))
            {
                FileHelpers.Shared.Serialize(Config.GetDefault(), file);
            }
            return FileHelpers.Shared.Deserialize<Config>(file) ?? Config.GetDefault();
        }

        public void SaveConfig()
        {
            var file = Path.Combine(FileHelpers.Shared.BaseDirectory, "config.json");
            FileHelpers.Shared.Serialize(Config, file);
        }
        public static void CheckInfiniteMod(bool rebuild)
        {
            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\Infinite\mod.json");
            try
            {
                if (!rebuild && File.Exists(file)) return;
                var mod = ModFactory.CreateInfiniteMod();
                if (mod != null)
                    FileHelpers.Shared.Serialize(mod, file);
            }
            catch
            {
                Log($"Having an issue with {file}");
            }
        }

        private Dictionary<string, MapBuilder> LoadTestMaps()
        {
            var tests = new Dictionary<string, MapBuilder>();
            if (!Args.TestMode) return tests;
            foreach (var mod in Mods)
            {
                var file = FileHelpers.Shared.GetDataFilePath(
                    @$"Mods\{mod.Key}\maptestlevel.json");
                if (!File.Exists(file)) continue;
                var m = FileHelpers.Shared.Deserialize<MapBuilder>(file);
                if (m != null && m.MapSections.Length > 0)
                    tests.Add(mod.Key, m);
            }
            return tests;
        }
        private Dictionary<string, MapBuilder> LoadSpecialMaps()
        {
            var specials = new Dictionary<string, MapBuilder>();
            foreach (var mod in Mods)
            {
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{mod.Key}\specialmap.json");
                if (!File.Exists(file)) continue;
                var m = FileHelpers.Shared.Deserialize<MapBuilder>(file);
                if (m != null && m.MapSections.Length > 0)
                    specials.Add(mod.Key, m);
            }
            return specials;
        }

        private Dictionary<string, MapBuilder> LoadBuilderMods()
        {
            var builders = new Dictionary<string, MapBuilder>();
            foreach (var mod in Mods)
            {
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{mod.Key}\map.json");
                if (!File.Exists(file))
                {
                    try
                    {
                        FileHelpers.Shared.Serialize(new MapBuilder(), file);
                    }
                    catch
                    {
                        Log($"Having an issue with {file}");
                    }
                }
                var m = FileHelpers.Shared.Deserialize<MapBuilder>(file);
                if (m != null && !builders.ContainsKey(mod.Key) && m.Validate(out var _))
                {
                    builders.Add(mod.Key, m);
                }
            }
            return builders;
        }
        public static void SaveEmbeddedResource(string resourceName, string outputPath)
        {
            try
            {
                // Get the current assembly
                var assembly = Assembly.GetExecutingAssembly();

                // Open the resource stream
                using Stream resourceStream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

                // Copy the resource to the output file
                using FileStream fileStream = new(outputPath, FileMode.Create, FileAccess.Write);
                resourceStream.CopyTo(fileStream);

            }
            catch
            {
                //Lets not fuss for now
            }
        }
        private Dictionary<string, Mod> LoadMods(bool forceRebuild)
        {
            var res = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            if (forceRebuild)
            {
                Ceilings.Clear();
                Floors.Clear();
                Textures.Clear();
                Decals.Clear();
                CharacterSprites.Clear();
                EnemySounds.Clear();
                ProjectileSprites.Clear();
                SpriteAnimations.Clear();
                ExperimentalEnemyTexture.Clear();
                Special.Clear();
            }
            CheckInfiniteMod(forceRebuild || Args.Rebuild || Args.RebuildWithMapImage);
            var mods = new Dictionary<string, Mod>();
            //Check original versions and export mod files
            foreach (var version in new Extractor(forceRebuild || Args.Rebuild || Args.RebuildWithMapImage).GameVersions)
            {
                var map = $"WolfensteinInfinite.GameData.Mods.{version.Name}.map.json";
                var mFile = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\map.json");
                var special = $"WolfensteinInfinite.GameData.Mods.{version.Name}.specialmap.json";
                var sFile = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\specialmap.json");
                var test = $"WolfensteinInfinite.GameData.Mods.{version.Name}.maptestlevel.json";
                var tFile = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\maptestlevel.json");

                if ((forceRebuild || !File.Exists(mFile)) && res.Any(p => p.Equals(map))) SaveEmbeddedResource(map, mFile);
                if ((forceRebuild || !File.Exists(sFile)) && res.Any(p => p.Equals(special))) SaveEmbeddedResource(special, sFile);
                if ((forceRebuild || !File.Exists(tFile)) && res.Any(p => p.Equals(test))) SaveEmbeddedResource(test, tFile);

                if(version.Name== "Wolfenstein3D") //Patched rockets
                {
                    for(var i = 0; i < 8; i++)
                    {
                        var rfile = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\Sprites\100{i}.png");
                        var efile = $"WolfensteinInfinite.GameData.Mods.{version.Name}.Sprites.100{i}.png";
                        if ((forceRebuild || !File.Exists(rfile)) && res.Any(p => p.Equals(efile))) SaveEmbeddedResource(efile, rfile);
                    }
                }
                //Need to copy internal mod/map/test/special json file on rebuild requst
                var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{version.Name}\mod.json");
                if (!File.Exists(file) || Debugger.IsAttached || forceRebuild)
                {
                    try
                    {
                        var mod = ModFactory.CreateMod(version);
                        if (mod != null)
                            FileHelpers.Shared.Serialize(mod, file);
                    }
                    catch
                    {
                        Log($"Having an issue with {file}");
                    }
                }

            }
            var modList = new List<ModConfig>();
            //Read Mods
            foreach (var d in Directory.GetDirectories(FileHelpers.Shared.GetDataFilePath("Mods")))
            {
                var file = Path.Combine(d, "mod.json");

                if (File.Exists(file))
                {
                    Log($"Understaning {file}");
                    try
                    {
                        var m = FileHelpers.Shared.Deserialize<Mod>(file);
                        if (m != null && !mods.ContainsKey(m.Name) && m.Validate())
                        {
                            mods.Add(m.Name, m);
                            if (!string.IsNullOrWhiteSpace(m.CeilingTexture))
                            {
                                Ceilings.Add(m.Name, FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(m.CeilingTexture)));
                            }
                            else
                            {
                                var ceiling = new Texture32(64, 64);
                                ceiling.Clear(m.CeilingColor.R, m.CeilingColor.G, m.CeilingColor.B);
                                Ceilings.Add(m.Name, ceiling);
                            }
                            if (!string.IsNullOrWhiteSpace(m.FloorTexture))
                            {
                                Floors.Add(m.Name, FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(m.FloorTexture)));
                            }
                            else
                            {
                                var floor = new Texture32(64, 64);
                                floor.Clear(m.FloorColor.R, m.FloorColor.G, m.FloorColor.B);
                                Floors.Add(m.Name, floor);
                            }
                            var i = Array.FindIndex(Config.Mods, p => p.Name == m.Name);
                            if (i >= 0)
                            {
                                modList.Add(new(Config.Mods[i].Name, Config.Mods[i].Enabled));
                            }
                            else
                            {
                                modList.Add(new(m.Name, true));
                            }
                            Textures.Add(m.Name, []);
                            for (int j = 0; j < m.Textures.Length; j++)
                            {
                                Textures[m.Name].Add(m.Textures[j].MapID, FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(m.Textures[j].File)));
                            }
                            Decals.Add(m.Name, []);
                            for (int j = 0; j < m.Decals.Length; j++)
                            {
                                Decals[m.Name].Add(m.Decals[j].MapID, FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(m.Decals[j].File)));
                            }
                            CharacterSprites.Add(m.Name, []);
                            for (int j = 0; j < m.Enemies.Length; j++)
                            {
                                var enemy = m.Enemies[j];
                                CharacterSprites[m.Name].Add(enemy.MapID, CharacterHelpers.ReadChatacterAnimations(enemy.SpritePath, enemy.StartSprite, enemy.AnimationType));
                            }

                            foreach (var enemy in m.Enemies)
                            {
                                foreach (var sound in enemy.AlertSounds
                                    .Concat(enemy.DeathSounds)
                                    .Concat(enemy.TauntSounds))
                                {
                                    var key = $"{m.Name}:{sound}";
                                    if (!EnemySounds.ContainsKey(key))
                                    {
                                        var path = FileHelpers.Shared.GetModDataFilePath(
                                            $"{m.Name}\\Sounds\\{sound}.wav");
                                        if (File.Exists(path))
                                            EnemySounds.Add(key, FileHelpers.Shared.LoadAudio(path));
                                    }
                                }
                            }

                            ProjectileSprites.Add(m.Name, []);
                            for (int j = 0; j < m.Projectiles.Length; j++)
                            {
                                var projectile = m.Projectiles[j];
                                ProjectileSprites[m.Name].Add(projectile.Name, new ProjectileSprite(projectile.SpritePath, projectile.StartSprite, projectile.SpriteType));
                            }
                            SpriteAnimations.Add(m.Name, []);
                            for (int j = 0; j < m.Animations.Length; j++)
                            {
                                var animation = m.Animations[j];
                                var frames = new List<Texture32>();
                                foreach (var f in animation.Sprites)
                                {
                                    frames.Add(FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(Path.Combine(animation.SpritePath, f))));
                                }
                                SpriteAnimations[m.Name].Add(animation.Name, new([.. frames], 1, frames.Count, animation.FramesPerSecond));
                            }
                            ExperimentalEnemyTexture.Add(m.Name, []);
                            var emtD = ExperimentalEnemyTexture[m.Name];

                            static void AddExperimentalEnemyTexture(Dictionary<string, Texture32> d, string b, string[] o)
                            {
                                foreach (var option in o)
                                {
                                    if (d.ContainsKey(option)) continue;
                                    d.Add(option, FileHelpers.Shared.LoadSurface32(FileHelpers.Shared.GetModDataFilePath(Path.Combine(b, option))));
                                }
                            }
                            foreach (var e in m.ExperimentalEnemy)
                            {
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.TopSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.BottomSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.MidSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.GoreSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.ShadowSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.DecalSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.EyeSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.FaceSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.HairSpriteOptions);
                                AddExperimentalEnemyTexture(emtD, e.SpritePath, e.SkinSpriteOptions);
                                foreach (var lw in e.LeftWeaponOptions)
                                {
                                    AddExperimentalEnemyTexture(emtD, e.SpritePath, lw.WeaponSpriteOptions);
                                    AddExperimentalEnemyTexture(emtD, e.SpritePath, lw.WeaponFireSpriteOptions);
                                }
                                foreach (var rw in e.RightWeaponOptions)
                                {
                                    AddExperimentalEnemyTexture(emtD, e.SpritePath, rw.WeaponSpriteOptions);
                                    AddExperimentalEnemyTexture(emtD, e.SpritePath, rw.WeaponFireSpriteOptions);
                                }
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        Log($"Evil detected in {file} listing sins");
                        Log(e.Message);
                    }
                }
            }
            Config.Mods = [.. modList];
            var demoIndex = Array.FindIndex(Config.Mods, p => p.Name == "Demo");
            var fullIndex = Array.FindIndex(Config.Mods, p => p.Name == "Wolfenstein3D");
            if (demoIndex >= 0 && fullIndex >= 0 && Config.Mods[demoIndex].Enabled && Config.Mods[fullIndex].Enabled)
            {
                Config.Mods[demoIndex].Enabled = false;
                Config.Mods[fullIndex].Enabled = true;
            }

            if (forceRebuild)
            {
                AddSpecialTextures();
            }
            return mods;
        }


        internal void Run()
        {
            while (IsRunning)
            {
                var currentTime = Clock.ElapsedTime;
                FrameTime = (currentTime.AsSeconds() - LastTime.AsSeconds());
                LastTime = currentTime;
                if (Graphics.Active)
                {
                    Buffer.Clear(0, 0, 0);
                    CurrentState = CurrentState?.Update(Buffer, FrameTime);

                }
                Graphics.UpdateKeys();
                Render();
            }
        }
        public RGBA8[]? PreserveColors { get; set; }
        public Animation POWAnimation { get; init; }
        public Texture32 BloodPool { get; init; }
        public string[] ActiveMods => [.. Config.Mods.Where(p => p.Enabled).Select(p=>p.Name)];

        private void RenderQuantize(int colors)
        {
            var originalPallet = Graphics.Pallet;
            (byte[] pixels, byte[] pallet) = PreserveColors == null ? Quantization.Quantize32BitAI(Buffer.Pixels, colors) : Quantization.Quantize32BitAI(Buffer.Pixels, colors, PreserveColors);
            //Dithering.Dither(Buffer.Pixels, ref pixels, pallet, Buffer.Width);
            Graphics.Pallet = pallet;
            Graphics.Blit(0, 0, new Texture8(Buffer.Width, Buffer.Height, pixels, pallet));
            Graphics.Render();
            Graphics.Pallet = originalPallet;
            PreserveColors = null;
        }
        public void Render() => DoRender();

        internal void ShutDown()
        {
            Graphics.ShutDown();
            AudioPlaybackEngine.Instance.ShutDown();
            SaveConfig();
            Logger.Shutdown();
        }

        internal PlayerWeapon GetWeapon(string weapon) => PlayerWeapons[weapon];
        internal Texture32 GetWeaponTexture(string weapon) => WeaponAnimations[weapon].GetTexture(0);
        internal Texture32 GetWeaponHudTexture(string weapon) => WeaponHudTextures[weapon];

        internal void GenerateExperiment(Mod tryMod, int level, out Enemy? experiment, out CharacterSprite? experimentSprite)
        {
            experiment = null;
            experimentSprite = null;

            var mod = tryMod.ExperimentalEnemy.Length > 0
                ? tryMod
                : Mods.TryGetValue("Infinite", out var inf) && inf.ExperimentalEnemy.Length > 0
                    ? inf
                    : null;

            if (mod == null) return;

            Dictionary<CharacterAnimationState, GameGraphics.Animation> Animations = [];
            var e = mod.ExperimentalEnemy[Random.Shared.Next(0, mod.ExperimentalEnemy.Length)];
            var composit = new Texture32(704, 64);
            var top = ExperimentalEnemyTexture[mod.Name][e.TopSpriteOptions[Random.Shared.Next(0, e.TopSpriteOptions.Length)]];
            var mid = ExperimentalEnemyTexture[mod.Name][e.MidSpriteOptions[Random.Shared.Next(0, e.MidSpriteOptions.Length)]];
            var bottom = ExperimentalEnemyTexture[mod.Name][e.BottomSpriteOptions[Random.Shared.Next(0, e.BottomSpriteOptions.Length)]];
            var gid = Random.Shared.Next(0, e.GoreSpriteOptions.Length);
            var blood = gid > 0 ? new RGBA8() { R = 0, G = 112, B = 0, A = 112 } : EnemyHelpers.RedBlood;
            var gore = ExperimentalEnemyTexture[mod.Name][e.GoreSpriteOptions[gid]];
            var shadow = ExperimentalEnemyTexture[mod.Name][e.ShadowSpriteOptions[Random.Shared.Next(0, e.ShadowSpriteOptions.Length)]];
            var decal = ExperimentalEnemyTexture[mod.Name][e.DecalSpriteOptions[Random.Shared.Next(0, e.DecalSpriteOptions.Length)]];
            var eyes = ExperimentalEnemyTexture[mod.Name][e.EyeSpriteOptions[Random.Shared.Next(0, e.EyeSpriteOptions.Length)]];
            var face = ExperimentalEnemyTexture[mod.Name][e.FaceSpriteOptions[Random.Shared.Next(0, e.FaceSpriteOptions.Length)]];
            var hair = ExperimentalEnemyTexture[mod.Name][e.HairSpriteOptions[Random.Shared.Next(0, e.HairSpriteOptions.Length)]];
            var skin = ExperimentalEnemyTexture[mod.Name][e.SkinSpriteOptions[Random.Shared.Next(0, e.SkinSpriteOptions.Length)]];

            var wlo = e.LeftWeaponOptions[Random.Shared.Next(0, e.LeftWeaponOptions.Length)];
            var wlt = ExperimentalEnemyTexture[mod.Name][wlo.WeaponSpriteOptions[Random.Shared.Next(0, wlo.WeaponSpriteOptions.Length)]];
            var wlft = ExperimentalEnemyTexture[mod.Name][wlo.WeaponFireSpriteOptions[Random.Shared.Next(0, wlo.WeaponFireSpriteOptions.Length)]];

            var wro = e.RightWeaponOptions[Random.Shared.Next(0, e.RightWeaponOptions.Length)];
            var wrt = ExperimentalEnemyTexture[mod.Name][wro.WeaponSpriteOptions[Random.Shared.Next(0, wro.WeaponSpriteOptions.Length)]];
            var wrft = ExperimentalEnemyTexture[mod.Name][wro.WeaponFireSpriteOptions[Random.Shared.Next(0, wro.WeaponFireSpriteOptions.Length)]];

            top = GraphicsHelpers.Colorize((float)Random.NextDouble() * 360, top);
            bottom = GraphicsHelpers.Colorize((float)Random.NextDouble() * 360, bottom);
            mid = GraphicsHelpers.Colorize((float)Random.NextDouble() * 360, mid);

            composit.Draw(0, 0, shadow);
            composit.Draw(0, 0, mid);
            composit.Draw(0, 0, top);
            composit.Draw(0, 0, bottom);
            composit.Draw(0, 0, skin);
            composit.Draw(0, 0, face);
            composit.Draw(0, 0, eyes);
            composit.Draw(0, 0, hair);
            composit.Draw(0, 0, wlt);
            composit.Draw(0, 0, wrt);
            if (Random.Shared.Next(0, 9) >= 5)
                composit.Draw(0, 0, decal);
            composit.Draw(0, 0, gore);
            composit.Draw(0, 0, wlft);
            composit.Draw(0, 0, wrft);
            var animation = new List<Texture32>();
            for (int i = 0; i < 4; i++)
            {
                var tex = new Texture32(64, 64);
                tex.Draw(i * -64, 0, composit);
                animation.Add(tex);
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 4, 3.5f));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 4, 3.5f));
            animation.Clear();
            for (int i = 4; i < 7; i++)
            {
                var tex = new Texture32(64, 64);
                tex.Draw(i * -64, 0, composit);
                animation.Add(tex);
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
            animation.Clear();

            for (int i = 7; i < 8; i++)
            {
                var tex = new Texture32(64, 64);
                tex.Draw(i * -64, 0, composit);
                animation.Add(tex);
            }
            Animations.Add(CharacterAnimationState.DEAD_LEFT, new Animation([.. animation], 1, 1, 1));
            Animations.Add(CharacterAnimationState.DEAD_RIGHT, new Animation([.. animation], 1, 1, 1));
            animation.Clear();

            for (int i = 8; i < 11; i++)
            {
                var tex = new Texture32(64, 64);
                tex.Draw(i * -64, 0, composit);
                animation.Add(tex);
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f, loop: false));
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 3, 3.5f, loop: false));

            animation.Clear();
            var hitTex = new Texture32(64, 64);
            hitTex.Draw(8 * -64, 0, composit);
            animation.Add(hitTex);
            Animations.Add(CharacterAnimationState.HIT, new Animation([.. animation], 1, 2, 8f, loop: false));

            experimentSprite = new CharacterSprite(Animations);

            var wr = wro.WeaponsOptions[Random.Shared.Next(0, wro.WeaponsOptions.Length)];
            var wl = wro.WeaponsOptions[Random.Shared.Next(0, wro.WeaponsOptions.Length)];
            var drop = new Dictionary<string, int>();
            if (PickupItemTypes.Any(p => p.Value.Name == wr))
            {
                drop.Add(wr, Random.Shared.Next(0, 50));
            }
            if (PickupItemTypes.Any(p => p.Value.Name == wl))
            {
                if (drop.TryGetValue(wl, out int value))                
                    drop[wl] = Math.Min(value * 2, 100);                
                else                
                    drop.Add(wl, Random.Shared.Next(0, 50));                
            }            
            var eTypes = new[] { EnemyType.ADOLF_HITLER, EnemyType.HITLER_GHOST, EnemyType.DOCTOR_SCHABBS, EnemyType.GENERAL_FETTGESICHT, EnemyType.GRETEL_GROSSE, EnemyType.HANS_GROSS, EnemyType.MECHA_HITLER, EnemyType.OTTO_GIFTMACHER };
            var bosses = new List<Enemy>();
            foreach (var m in Mods.Values)
            {
                foreach (var enemy in m.Enemies)
                {
                    if (eTypes.Contains(enemy.EnemyType)) bosses.Add(enemy);
                }
            }
            var addPoint = level * 5;
            experiment = new Enemy(-1, e.ExperimentalName, EnemyType.HANS_GROSS, 512 * 3, 5000 + Math.Min(level * 2, 1000),
                new Dictionary<Difficulties, int>()
                {
                    [Difficulties.CAN_I_PLAY_DADDY] = 850 + addPoint,
                    [Difficulties.DONT_HURT_ME] = 950 + addPoint,
                    [Difficulties.BRING_EM_ON] = 1050 + addPoint,
                    [Difficulties.I_AM_DEATH_INCARNATE] = 1200 + addPoint
                }, [wr, wl],
                drop, CharacterSpriteType.BOSS, e.SpritePath, 0,
                bosses.Count != 0 ? bosses[Random.Shared.Next(0, bosses.Count)].AlertSounds : [],
                bosses.Count != 0 ? bosses[Random.Shared.Next(0, bosses.Count)].DeathSounds : [],
                bosses.Count != 0 ? bosses[Random.Shared.Next(0, bosses.Count)].TauntSounds : [], [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, blood,0);

        }

        public void RebuildMods()
        {
            CheckInfiniteMod(true);
            Mods = LoadMods(true);
            foreach (var mod in Mods)
            {
                ReloadMod(mod.Key, ["map.json", "specialmap.json", "maptestlevel.json"]);
            }
        }
        public void ReloadMod(string modName, string[] mapTypes)
        {
            if (mapTypes.Contains("map.json"))
            {
                var modPath = FileHelpers.Shared.GetDataFilePath($@"Mods\{modName}\map.json");
                if (File.Exists(modPath))
                {
                    var builder = FileHelpers.Shared.Deserialize<MapBuilder>(modPath);
                    if (builder != null) BuilderMods[modName] = builder;
                }
            }
            if (mapTypes.Contains("specialmap.json"))
            {
                var specialPath = FileHelpers.Shared.GetDataFilePath($@"Mods\{modName}\specialmap.json");
                if (File.Exists(specialPath))
                {
                    var sections = FileHelpers.Shared.Deserialize<MapBuilder>(specialPath);
                    if (sections != null) SpecialMaps[modName] = sections;
                }
            }
            if (mapTypes.Contains("maptestlevel.json"))
            {
                var testPath = FileHelpers.Shared.GetDataFilePath($@"Mods\{modName}\maptestlevel.json");
                if (File.Exists(testPath))
                {
                    var sections = FileHelpers.Shared.Deserialize<MapBuilder>(testPath);
                    if (sections != null) TestMaps[modName] = sections;
                }
            }
        }

    }
}