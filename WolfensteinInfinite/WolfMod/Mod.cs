using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.WolfMod
{
    public class    Mod(string name)
    {
        public RGBA8 FloorColor { get; set; } = new RGBA8() { R = 128, B = 128, G = 128, A = 255 };
        public string FloorTexture { get; set; } = string.Empty;
        public RGBA8 CeilingColor { get; set; } = new RGBA8() { R = 96, B = 96, G = 96, A = 96 };
        public string CeilingTexture { get; set; } = string.Empty;
        public string EndLevelMusic { get; set; } = string.Empty;
        public string TitleMusic { get; set; } = string.Empty;
        public string Name { get; init; } = name;
        public MusicTrack[] MusicTracks { get; set; } = [];
        public Texture[] Textures { get; set; } = [];
        public DecalSprite[] Decals { get; set; } = [];
        public Enemy[] Enemies { get; set; } = [];
        public Projectile[] Projectiles { get; set; } = [];
        public SpriteAnimation[] Animations { get; set; } = [];
        public Weapon[] Weapons { get; set; } = [];
        public ExperimentalEnemy[] ExperimentalEnemy { get; set; } = [];

        public bool Validate()
        {
            void Log(string message)=> Logger.GetLogger(this).Log(message);
            bool LogFail(string message)
            {
                Logger.GetLogger(this).Log(message);
                return false;
            }
            bool ValidateNumberedFiles(string path, int start, int count, string ext)
            {
                for (var i = start; i < start + count; i++)
                    if (!System.IO.File.Exists(System.IO.Path.Combine(FileHelpers.Shared.GetModDataFilePath(path), $"{i}.{ext}"))) LogFail($"File Not Found {i}.{ext}");
                return true;
            }
            bool ValidateFiles(string path, string[] files)
            {
                foreach (var file in files)
                    if (!System.IO.File.Exists(System.IO.Path.Combine(FileHelpers.Shared.GetModDataFilePath(path), file))) return LogFail($"File Not Found {path}");
                return true;
            }
            Log($"Validating {Name} {DateTime.Today}");

            if (!string.IsNullOrEmpty(EndLevelMusic) && !System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(EndLevelMusic)))
                return LogFail($"File Not Found {EndLevelMusic}");
            Log("Checking MusicTracks");
            foreach (var asset in MusicTracks)
                if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(asset.File))) return LogFail($"File Not Found {asset.File}");
            if (!string.IsNullOrEmpty(TitleMusic))
            {
                Log("Checking TitleMusic");
                if (!MusicTracks.Any(p => p.Name == TitleMusic)) return LogFail($"File Not Found {TitleMusic}");
            }
            Log("Checking Textures");
            foreach (var asset in Textures)
            {
                if (asset.MapID < 0 || asset.MapID>=1000) return LogFail($"Invalid MapId {asset.MapID}");
                if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(asset.File))) return LogFail($"File Not Found {asset.File}");
                if (Textures.Count(p => p.MapID == asset.MapID) > 1) return LogFail($"Duplicate MapId {asset.MapID}");
            }
            Log("Checking Animations");
            foreach (var asset in Animations)
            {
                if (string.IsNullOrWhiteSpace(asset.Name)) return LogFail($"Invalid Name (missing)");
                if (Animations.Count(p => p.Name == asset.Name) > 1) LogFail($"Duplicate Name {asset.Name}");
                if (!ValidateFiles(asset.SpritePath, asset.Sprites)) return false;
            }
            Log("Checking Decals");
            foreach (var asset in Decals)
            {
                if (asset.MapID < 0) return LogFail($"Invalid MapId {asset.MapID}");
                if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(asset.File))) return LogFail($"File Not Found {asset.File}");
                if (Decals.Count(p => p.MapID == asset.MapID) > 1) LogFail($"Duplicate MapId {asset.MapID}");
            }
            Log("Checking Projectiles");
            foreach (var asset in Projectiles)
            {
                Log($"Validating {asset.Name}");
                if (!string.IsNullOrEmpty(asset.ImpactAnimation))
                {
                    if(!Animations.Any(p=>p.Name == asset.ImpactAnimation)) return LogFail($"Projectile {asset.Name} missing {asset.ImpactAnimation}");
                }
                if (!string.IsNullOrEmpty(asset.TrailAnimation))
                {
                    if (!Animations.Any(p => p.Name == asset.TrailAnimation)) return LogFail($"Projectile {asset.Name} missing {asset.TrailAnimation}");
                }
                if (!string.IsNullOrEmpty(asset.SpritePath))
                {
                    switch (asset.SpriteType)
                    {
                        case ProjectileSpriteType.NONE: break;
                        case ProjectileSpriteType.BULLET: break;
                        case ProjectileSpriteType.ROCKET:
                            {
                                if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 8 + 4 + 3, "png")) return false;
                                break;
                            }
                        case ProjectileSpriteType.SERUM:
                            {
                                if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 4, "png")) return false;
                                break;
                            }
                        case ProjectileSpriteType.FLAME:
                            {
                                if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 2, "png")) return false;
                                break;
                            }
                    }
                }

                if (!string.IsNullOrWhiteSpace(asset.HitSound))
                    if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(asset.HitSound))) return LogFail($"File Not Found {asset.HitSound}");

            }
            Log("Checking Weapons");
            foreach (var asset in Weapons)
            {
                if (!Projectiles.Any(p => p.Name == asset.Projectile)) return LogFail($"Weapon {asset.Name} missing {asset.Projectile}");
                if (!string.IsNullOrEmpty(asset.Sound))
                {
                    if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(asset.Sound))) return LogFail($"Weapon {asset.Name} missing {asset.Sound}");
                }

            }
            Log("Checking Enemies");
            foreach (var asset in Enemies)
            {
                if(asset.FireFrames == null || asset.FireFrames.Length==0) return LogFail($"Enemy {asset.Name} missing fire frames");
                foreach (var w in asset.Weapons)
                    if (!Weapons.Any(p => p.Name == w)) return LogFail($"Enemy {asset.Name} missing {w}");
                switch (asset.AnimationType)
                {
                    case CharacterSpriteType.GHOST:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 2, "png")) return false;
                        break;
                    case CharacterSpriteType.GUARD:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 49, "png")) return false;
                        break;
                    case CharacterSpriteType.DOG:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 39, "png")) return false;
                        break;
                    case CharacterSpriteType.MUTANT:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 51, "png")) return false;
                        break;
                    case CharacterSpriteType.OFFICER:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 50, "png")) return false;
                        break;
                    case CharacterSpriteType.BOSS:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 11, "png")) return false;
                        break;
                    case CharacterSpriteType.DOCTOR_SCHABBS:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 10, "png")) return false;
                        break;
                    case CharacterSpriteType.MECHA_HITLER:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 11, "png")) return false;
                        break;
                    case CharacterSpriteType.ADOLF_HITLER:
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 15, "png")) return false;
                        break;
                    case CharacterSpriteType.HITLER_GHOST: //2 are actually the ammo 326 & 327
                        if (!ValidateNumberedFiles(asset.SpritePath, asset.StartSprite, 12, "png")) return false;
                        break;
                }
                if (Enemies.Count(p => p.MapID == asset.MapID) > 1) return LogFail($"Duplicate MapId {asset.MapID}");
            }
            Log("Checking Experimental Enemies");

            bool CheckExperimentalEnemyTexture(string b, string[] o)
            {
                foreach (var option in o)
                {
                    if (!System.IO.File.Exists(FileHelpers.Shared.GetModDataFilePath(System.IO.Path.Combine(b, option)))) return LogFail($"Experimental Enemy missing {option}"); ;
                }
                return true;
            }
            foreach (var e in ExperimentalEnemy)
            {
                if(!CheckExperimentalEnemyTexture(e.SpritePath, e.TopSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.BottomSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.MidSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.GoreSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.ShadowSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.DecalSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.EyeSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.FaceSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.HairSpriteOptions)) return false;
                if (!CheckExperimentalEnemyTexture(e.SpritePath, e.SkinSpriteOptions)) return false;
                foreach (var lw in e.LeftWeaponOptions)
                {
                    if (!CheckExperimentalEnemyTexture(e.SpritePath, lw.WeaponSpriteOptions)) return false;
                    if (!CheckExperimentalEnemyTexture( e.SpritePath, lw.WeaponFireSpriteOptions)) return false;
                }
                foreach (var rw in e.RightWeaponOptions)
                {
                    if (!CheckExperimentalEnemyTexture(e.SpritePath, rw.WeaponSpriteOptions)) return false;
                    if (!CheckExperimentalEnemyTexture(e.SpritePath, rw.WeaponFireSpriteOptions)) return false;
                }
            }

            return true;
        }
    }
}
