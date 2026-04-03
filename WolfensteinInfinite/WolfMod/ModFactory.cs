using WolfensteinInfinite.DataFormats;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameHelpers;
//sounds
//digi2 some sort of explode/slap
//digi3 door open
//digi4 Change Menu
//digi5 & 6 small bangs
//digi 10/11 gun chain or machine? hans gross
//digi 21 pistol
//digi 15 pushwall
//digi39 fart?
//digi22 gurgle
//digo30 switch?
//digi smash glass?
//pickup pcs 31/12/16 ?

namespace WolfensteinInfinite.WolfMod
{
    public class ExperimentalEnemyWeapon(string[] weaponOptions, string[] weaponFireOptions, string[] weapons)
    {
        public string[] WeaponSpriteOptions { get; init; } = weaponOptions;
        public string[] WeaponFireSpriteOptions { get; init; } = weaponFireOptions;
        public string[] WeaponsOptions { get; init; } = weapons;
    }
    public class ExperimentalEnemy(string experimentalName, string spritePath, string[] bottomOptions, string[] midOptions, string[] topOptions,
        string[] hairOptions, string[] skinOptions, string[] eyeOptions, string[] faceOptions, string[] decalOptions,
        string[] shadowOptions, string[] goreOptions, ExperimentalEnemyWeapon[] leftWeaponOptions, ExperimentalEnemyWeapon[] rightWeaponOptions)
    {
        public string ExperimentalName { get; init; } = experimentalName;
        public string SpritePath { get; init; } = spritePath;
        public string[] BottomSpriteOptions { get; init; } = bottomOptions;
        public string[] MidSpriteOptions { get; init; } = midOptions;
        public string[] TopSpriteOptions { get; init; } = topOptions;
        public string[] HairSpriteOptions { get; init; } = hairOptions;
        public string[] SkinSpriteOptions { get; init; } = skinOptions;
        public string[] EyeSpriteOptions { get; init; } = eyeOptions;
        public string[] FaceSpriteOptions { get; init; } = faceOptions;
        public string[] DecalSpriteOptions { get; init; } = decalOptions;
        public string[] ShadowSpriteOptions { get; init; } = shadowOptions;
        public string[] GoreSpriteOptions { get; init; } = goreOptions;
        public ExperimentalEnemyWeapon[] LeftWeaponOptions { get; init; } = leftWeaponOptions;
        public ExperimentalEnemyWeapon[] RightWeaponOptions { get; init; } = rightWeaponOptions;
    }
    public static class ModFactory
    {
        public static Mod? CreateMod(GameVersion version)
        {
            return version.Name switch
            {
                "Demo" => CreateDemoMod(version),
                "Wolfenstein3D" => CreateWolfensteinMod(version),
                "SpearOfDestiny" => null,
                "ReturnToDanger" => null,
                "UltimateChallenge" => null,
                _ => null,
            };
        }

        public static Mod CreateInfiniteMod()
        {
            var mod = new Mod("Infinite")
            {
                FloorTexture = "Infinite\\Textures\\Floor.png",
                CeilingTexture = "Infinite\\Textures\\Ceiling.png",
                ExperimentalEnemy = [
                    new("Experiment Alpha", "Infinite\\Sprites",
                    ["ExBottom01.png"],
                    ["ExMid01.png", "ExMid02.png"],
                    ["ExTop01.png", "ExTop02.png"],
                    ["ExHair01.png", "ExHair02.png", "ExHair03.png", "ExHair04.png"],
                    ["ExSkin01.png", "ExSkin02.png"],
                    ["ExEyes01.png", "ExEyes02.png", "ExEyes03.png"],
                    ["ExFace01.png"],
                    ["ExDecal01.png", "ExDecal02.png"],
                    ["ExShadows01.png"],
                    ["ExGore01.png", "ExGore02.png"],
                    [
                         new ExperimentalEnemyWeapon(["ExWeaponLeft01.png", "ExWeaponLeft02.png"],
                            ["ExWeaponFireLeft01.png"],
                            ["MachineGun","ChainGun"])
                    ],
                    [
                         new ExperimentalEnemyWeapon(["ExWeaponRight01.png", "ExWeaponRight02.png"],
                            ["ExWeaponFireRight01.png"],
                            ["MachineGun","ChainGun"])
                    ]
                    )
                ]
            };
            return mod;
        }
        public static Mod CreateDemoMod(GameVersion version)
        {
            var mod = new Mod(version.Name)
            {
                MusicTracks = [
                new("CORNER", $"Demo\\Music\\0.mid"),
                    new("WARMARCH", $"Demo\\Music\\2.mid"),
                    new("GETTHEM", $"Demo\\Music\\3.mid"),
                    new("NAZI_NOR", $"Demo\\Music\\7.mid"),
                    new("POW", $"Demo\\Music\\9.mid"),
                    new("SEARCHN", $"Demo\\Music\\11.mid"),
                    new("SUSPENSE", $"Demo\\Music\\12.mid"),
                    new("WONDERIN", $"Demo\\Music\\14.mid")
            //new("ENDLEVEL", $"\\Demo\\Music\\16.mid")
            //new("ROSTER", $"\\Demo\\Music\\23.mid")
            //new("URAHERO", $"\\Demo\\Music\\24.mid")
            ],
                EndLevelMusic = $"Demo\\Music\\16.mid",
                TitleMusic = "WONDERIN",
                Textures = [
                new(0, "Rock", $"Demo\\Textures\\0.png",0),
                    new(1, "RockChunky", $"Demo\\Textures\\2.png",0),
                    new(2, "RockHorizontal", $"Demo\\Textures\\52.png",0),
                    new(3, "RockFlag", $"Demo\\Textures\\4.png",1),
                    new(4, "RockHitler", $"Demo\\Textures\\6.png",1),
                    new(5, "RockEagle", $"Demo\\Textures\\10.png",1),
                    new(6, "RockLiteMoss", $"Demo\\Textures\\46.png",2),
                    new(7, "RockHeavyMoss", $"Demo\\Textures\\50.png",2),
                    new(8, "RockWarning", $"Demo\\Textures\\54.png",1),
                    new(9, "BlueBrick", $"Demo\\Textures\\16.png",3),
                    new(10, "BlueBrickChunky", $"Demo\\Textures\\14.png",3),
                    new(11, "BlueBrickCell", $"Demo\\Textures\\8.png",4),
                    new(12, "BlueBrickCellSkeleton", $"Demo\\Textures\\12.png",4),
                    new(13, "Wood", $"Demo\\Textures\\22.png",5),
                    new(14, "WoodHitler", $"Demo\\Textures\\20.png",6),
                    new(15, "WoodEagle", $"Demo\\Textures\\18.png",6),
                    new(16, "WoodCross", $"Demo\\Textures\\44.png",6),
                    new(17, "Steel", $"Demo\\Textures\\28.png",7),
                    new(18, "SteelWarning", $"Demo\\Textures\\26.png",8),
                    new(19, "RedBrick", $"Demo\\Textures\\32.png",9),
                    new(20, "RedSwas", $"Demo\\Textures\\34.png",10),
                    new(21, "RedFlag", $"Demo\\Textures\\38.png",10),
                    new(22, "PurpleRock", $"Demo\\Textures\\36.png",11),
                    new(23, "PurpleRockGore", $"Demo\\Textures\\48.png",11),
                    new(24, "OutsideDay", $"Demo\\Textures\\30.png",12),
                    new(25, "OutsideNight", $"Demo\\Textures\\31.png",12)
            ],
                Decals = [
                new(0, "Puddle", $"Demo\\Sprites\\2.png", true, false, Direction.NONE),
                    new(1, "Barrel", $"Demo\\Sprites\\3.png", true, false, Direction.NONE),
                    new(2, "TableChairs", $"Demo\\Sprites\\4.png", true, false, Direction.NONE),
                    new(3, "FloorLamp", $"Demo\\Sprites\\5.png", true, true, Direction.NONE),
                    new(4, "Chandelier", $"Demo\\Sprites\\6.png", true, true, Direction.NONE),
                    new(5, "HangingMan", $"Demo\\Sprites\\7.png", true, true, Direction.NONE),
                    new(6, "Pillar", $"Demo\\Sprites\\9.png", false, false, Direction.NONE),
                    new(7, "PottedPlant", $"Demo\\Sprites\\10.png", true, false, Direction.NONE),
                    new(8, "Skeleton", $"Demo\\Sprites\\11.png", true, false, Direction.NONE),
                    new(9, "Sink", $"Demo\\Sprites\\12.png", true, false, Direction.NONE),
                    new(10, "DyingPlant", $"Demo\\Sprites\\13.png", true, false, Direction.NONE),
                    new(11, "Urn", $"Demo\\Sprites\\14.png", true, false, Direction.NONE),
                    new(12, "Table", $"Demo\\Sprites\\15.png", true, false, Direction.NONE),
                    new(13, "Light", $"Demo\\Sprites\\16.png", true, true, Direction.NONE),
                    new(14, "PotsPansShelf", $"Demo\\Sprites\\17.png", true, false, Direction.NONE),
                    new(15, "Armor", $"Demo\\Sprites\\18.png", true, false, Direction.NONE),
                    new(16, "HangingCage", $"Demo\\Sprites\\19.png", true, false, Direction.NONE),
                    new(17, "SkelitonInCage", $"Demo\\Sprites\\20.png", true, false, Direction.NONE),
                    new(18, "PileOfBones", $"Demo\\Sprites\\21.png", true, false, Direction.NONE),
                    new(19, "Bed", $"Demo\\Sprites\\24.png", true, false, Direction.NONE),
                    new(20, "Pot", $"Demo\\Sprites\\25.png", true, false, Direction.NONE),
                    new(21, "WoodBarrel", $"Demo\\Sprites\\37.png", true, false, Direction.NONE),
                    new(22, "Well", $"Demo\\Sprites\\38.png", true, false, Direction.NONE),
                    new(23, "EmptyWell", $"Demo\\Sprites\\39.png", true, false, Direction.NONE),
                    //new(24, "BloodPool", $"Demo\\Sprites\\40.png", true, false, Direction.NONE),
                    new(24, "Flag", $"Demo\\Sprites\\41.png", true, false, Direction.NONE),
                    new(25, "Aaaardwolf", $"Demo\\Sprites\\42.png", true, false, Direction.NONE),
                    new(26, "Gibs1", $"Demo\\Sprites\\43.png", true, false, Direction.NONE),
                    new(27, "Gibs2", $"Demo\\Sprites\\44.png", true, false, Direction.NONE),
                    new(28, "Gibs3", $"Demo\\Sprites\\45.png", true, false, Direction.NONE),
                    new(29, "PotsPans", $"Demo\\Sprites\\46.png", true, false, Direction.NONE),
                    new(30, "Stove", $"Demo\\Sprites\\47.png", true, false, Direction.NONE),
                    new(31, "Spears", $"Demo\\Sprites\\48.png", true, false, Direction.NONE),
                    new(32, "WeedsNorth", $"Demo\\Sprites\\49.png", true, false, Direction.NORTH),
                    new(33, "WeedsEast", $"Demo\\Sprites\\49.png", true, false, Direction.EAST),
                    new(34, "WeedsSouth", $"Demo\\Sprites\\49.png", true, false, Direction.SOUTH),
                    new(35, "WeedsWest", $"Demo\\Sprites\\49.png", true, false, Direction.WEST)
            ],
                Animations = [],
                Projectiles =
            [
                ProjectileHelpers.CreateBullet("Bullet", null, null, null),
                ProjectileHelpers.CreateBite("Bite", null, null, null)
            ],
                Weapons =
            [
            WeaponHelpers.CreatePistol($"Demo\\Sounds\\digi21.wav"),
                WeaponHelpers.CreateBite($"Demo\\Sounds\\digi16.wav"),
                WeaponHelpers.CreateMachineGun($"Demo\\Sounds\\digi10.wav"),
                WeaponHelpers.CreateChainGun($"Demo\\Sounds\\digi11.wav")
            ]
            };
            var deaths = new string[] { "digi11", "digi12", "digi13", "digi14", "digi20" };
            mod.Enemies = [
                EnemyHelpers.CreateGuard(0, $"Demo\\Sprites", 50, ["digi0"], deaths, []),
                EnemyHelpers.CreateDog(1, $"Demo\\Sprites", 99, ["digi1"], ["digi16"], []),
                EnemyHelpers.CreateSSSoldier(2, $"Demo\\Sprites", 138, ["digi7"], deaths, []),
                EnemyHelpers.CreateHansGross(5, $"Demo\\Sprites", 296, ["digi8"], ["digi9"], [])
            ];

            return mod;
        }
        public static Mod CreateWolfensteinMod(GameVersion version)
        {
            var mod = new Mod(version.Name)
            {
                MusicTracks = [
                new("CORNER", $"Wolfenstein3D\\Music\\0.mid"),
                    new("DUNGEON", $"Wolfenstein3D\\Music\\1.mid"),
                    new("WARMARCH", $"Wolfenstein3D\\Music\\2.mid"),
                    new("GETTHEM", $"Wolfenstein3D\\Music\\3.mid"),
                    new("HEADACHE", $"Wolfenstein3D\\Music\\4.mid"),
                    new("HITLWLTZ", $"Wolfenstein3D\\Music\\5.mid"),
                    new("INTROCW3", $"Wolfenstein3D\\Music\\6.mid"),
                    new("NAZI_NOR", $"Wolfenstein3D\\Music\\7.mid"),
                    new("NAZI_OMI", $"Wolfenstein3D\\Music\\8.mid"),
                    new("POW", $"Wolfenstein3D\\Music\\9.mid"),
                    //new("SALUTE_MUS", $"\\Wolfenstein3D\\Music\\10.mid"),
                    new("SEARCHN", $"Wolfenstein3D\\Music\\11.mid"),
                    new("SUSPENSE", $"Wolfenstein3D\\Music\\12.mid"),
                    new("VICTORS", $"Wolfenstein3D\\Music\\13.mid"),
                    new("WONDERIN", $"Wolfenstein3D\\Music\\14.mid"),
                    new("FUNKYOU", $"Wolfenstein3D\\Music\\15.mid"),
                    //new("ENDLEVEL", $"\\Demo\\Music\\16.mid")
                    new("GOINGAFT", $"Wolfenstein3D\\Music\\17.mid"),
                    new("PREGNAN", $"Wolfenstein3D\\Music\\18.mid"),
                    new("ULTIMATE", $"Wolfenstein3D\\Music\\19.mid"),
                    new("NAZI_RAP", $"Wolfenstein3D\\Music\\20.mid"),
                    new("ZEROHOUR", $"Wolfenstein3D\\Music\\21.mid"),
                    new("TWELFTH", $"Wolfenstein3D\\Music\\22.mid"),
                    //new("ROSTER", $"\\Demo\\Music\\23.mid")
                    //new("URAHERO", $"\\Demo\\Music\\24.mid")
                    new("VICMARCH", $"Wolfenstein3D\\Music\\25.mid"),
                    new("PACMAN", $"Wolfenstein3D\\Music\\26.mid")
            ],
                EndLevelMusic = $"Demo\\Music\\16.mid",
                TitleMusic = "WONDERIN",
                Textures = [
                new(0, "Rock", $"Wolfenstein3D\\Textures\\0.png",0),
                    new(1, "RockChunky", $"Wolfenstein3D\\Textures\\2.png",0),
                    new(2, "RockHorizontal", $"Wolfenstein3D\\Textures\\52.png",0),
                    new(3, "RockFlag", $"Wolfenstein3D\\Textures\\4.png",1),
                    new(4, "RockHitler", $"Wolfenstein3D\\Textures\\6.png",1),
                    new(5, "RockEagle", $"Wolfenstein3D\\Textures\\10.png",1),
                    new(6, "RockLiteMoss", $"Wolfenstein3D\\Textures\\46.png",2),
                    new(7, "RockHeavyMoss", $"Wolfenstein3D\\Textures\\50.png",2),
                    new(8, "RockWarning", $"Wolfenstein3D\\Textures\\54.png",1),
                    new(9, "BlueBrick", $"Wolfenstein3D\\Textures\\16.png",3),
                    new(10, "BlueBrickChunky", $"Wolfenstein3D\\Textures\\14.png",3),
                    new(11, "BlueBrickCell", $"Wolfenstein3D\\Textures\\8.png",4),
                    new(12, "BlueBrickCellSkeleton", $"Wolfenstein3D\\Textures\\12.png",4),
                    new(13, "Wood", $"Wolfenstein3D\\Textures\\22.png",5),
                    new(14, "WoodHitler", $"Wolfenstein3D\\Textures\\20.png",6),
                    new(15, "WoodEagle", $"Wolfenstein3D\\Textures\\18.png",6),
                    new(16, "WoodCross", $"Wolfenstein3D\\Textures\\44.png",6),
                    new(17, "Steel", $"Wolfenstein3D\\Textures\\28.png",7),
                    new(18, "SteelWarning", $"Wolfenstein3D\\Textures\\26.png",8),
                    new(19, "RedBrick", $"Wolfenstein3D\\Textures\\32.png",9),
                    new(20, "RedSwas", $"Wolfenstein3D\\Textures\\34.png",10),
                    new(21, "RedFlag", $"Wolfenstein3D\\Textures\\38.png",10),
                    new(22, "PurpleRock", $"Wolfenstein3D\\Textures\\36.png",11),
                    new(23, "PurpleRockGore", $"Wolfenstein3D\\Textures\\48.png",11),
                    new(24, "OutsideDay", $"Demo\\Textures\\30.png",12),
                    new(25, "OutsideNight", $"Demo\\Textures\\31.png",12),
                    new(26, "Bedrock", $"Wolfenstein3D\\Textures\\56.png",13),
                    new(27, "BedrockBlood", $"Wolfenstein3D\\Textures\\58.png",13),
                    new(28, "BedrockGore", $"Wolfenstein3D\\Textures\\60.png",13),
                    new(29, "BedrockSplat", $"Wolfenstein3D\\Textures\\62.png",13),
                    new(30, "HitlerMozaic", $"Wolfenstein3D\\Textures\\64.png",14),
                    new(31, "BluBlock", $"Wolfenstein3D\\Textures\\78.png",15),
                    new(32, "BluBlockSkull", $"Wolfenstein3D\\Textures\\66.png",16),
                    new(33, "BluBlockSwas", $"Wolfenstein3D\\Textures\\70.png",16),
                    new(34, "CinderBlock", $"Wolfenstein3D\\Textures\\68.png",17),
                    new(35, "CinderBlockDrain", $"Wolfenstein3D\\Textures\\72.png",17),
                    new(36, "CinderBlockCrack", $"Wolfenstein3D\\Textures\\76.png",17),
                    new(37, "CinderBlockMap", $"Wolfenstein3D\\Textures\\84.png",18),
                    new(38, "CinderBlockHitler", $"Wolfenstein3D\\Textures\\96.png",18),
                    new(39, "RedBrownGrayBrick", $"Wolfenstein3D\\Textures\\74.png",9),
                    new(40, "BlueWarning", $"Wolfenstein3D\\Textures\\80.png",19),
                    new(41, "Marble", $"Wolfenstein3D\\Textures\\82.png",20),
                    new(42, "MarbleAlternate", $"Wolfenstein3D\\Textures\\90.png",20),
                    new(43, "MarbleFlag", $"Wolfenstein3D\\Textures\\92.png",21),
                    new(44, "MDFPanel", $"Wolfenstein3D\\Textures\\94.png",21)
            ],
                Decals = [
                new(0, "Puddle", $"Wolfenstein3D\\Sprites\\2.png", true, false, Direction.NONE),
                    new(1, "Barrel", $"Wolfenstein3D\\Sprites\\3.png", true, false, Direction.NONE),
                    new(2, "TableChairs", $"Wolfenstein3D\\Sprites\\4.png", true, false, Direction.NONE),
                    new(3, "FloorLamp", $"Wolfenstein3D\\Sprites\\5.png", true, true, Direction.NONE),
                    new(4, "Chandelier", $"Wolfenstein3D\\Sprites\\6.png", true, true, Direction.NONE),
                    new(5, "HangingMan", $"Wolfenstein3D\\Sprites\\7.png", true, true, Direction.NONE),
                    new(6, "Pillar", $"Wolfenstein3D\\Sprites\\9.png", false, false, Direction.NONE),
                    new(7, "PottedPlant", $"Wolfenstein3D\\Sprites\\10.png", true, false, Direction.NONE),
                    new(8, "Skeleton", $"Wolfenstein3D\\Sprites\\11.png", true, false, Direction.NONE),
                    new(9, "Sink", $"Wolfenstein3D\\Sprites\\12.png", true, false, Direction.NONE),
                    new(10, "DyingPlant", $"Wolfenstein3D\\Sprites\\13.png", true, false, Direction.NONE),
                    new(11, "Urn", $"Wolfenstein3D\\Sprites\\14.png", true, false, Direction.NONE),
                    new(12, "Table", $"Wolfenstein3D\\Sprites\\15.png", true, false, Direction.NONE),
                    new(13, "Light", $"Wolfenstein3D\\Sprites\\16.png", true, true, Direction.NONE),
                    new(14, "PotsPansShelf", $"Wolfenstein3D\\Sprites\\17.png", true, false, Direction.NONE),
                    new(15, "Armor", $"Wolfenstein3D\\Sprites\\18.png", true, false, Direction.NONE),
                    new(16, "HangingCage", $"Wolfenstein3D\\Sprites\\19.png", true, false, Direction.NONE),
                    new(17, "SkelitonInCage", $"Wolfenstein3D\\Sprites\\20.png", true, false, Direction.NONE),
                    new(18, "PileOfBones", $"Wolfenstein3D\\Sprites\\21.png", true, false, Direction.NONE),
                    new(19, "Bed", $"Wolfenstein3D\\Sprites\\24.png", true, false, Direction.NONE),
                    new(20, "Pot", $"Wolfenstein3D\\Sprites\\25.png", true, false, Direction.NONE),
                    new(21, "WoodBarrel", $"Wolfenstein3D\\Sprites\\37.png", true, false, Direction.NONE),
                    new(22, "Well", $"Wolfenstein3D\\Sprites\\38.png", true, false, Direction.NONE),
                    new(23, "EmptyWell", $"Wolfenstein3D\\Sprites\\39.png", true, false, Direction.NONE),
                    //new(24, "BloodPool", $"Wolfenstein3D\\Sprites\\40.png", true, false, Direction.NONE),
                    new(24, "Flag", $"Wolfenstein3D\\Sprites\\41.png", true, false, Direction.NONE),
                    new(25, "Aaaardwolf", $"Wolfenstein3D\\Sprites\\42.png", true, false, Direction.NONE),
                    new(26, "Gibs1", $"Wolfenstein3D\\Sprites\\43.png", true, false, Direction.NONE),
                    new(27, "Gibs2", $"Wolfenstein3D\\Sprites\\44.png", true, false, Direction.NONE),
                    new(28, "Gibs3", $"Wolfenstein3D\\Sprites\\45.png", true, false, Direction.NONE),
                    new(29, "PotsPans", $"Wolfenstein3D\\Sprites\\46.png", true, false, Direction.NONE),
                    new(30, "Stove", $"Wolfenstein3D\\Sprites\\47.png", true, false, Direction.NONE),
                    new(31, "Spears", $"Wolfenstein3D\\Sprites\\48.png", true, false, Direction.NONE),
                    new(32, "WeedsNorth", $"Wolfenstein3D\\Sprites\\49.png", true, false, Direction.NORTH),
                    new(33, "WeedsEast", $"Wolfenstein3D\\Sprites\\49.png", true, false, Direction.EAST),
                    new(34, "WeedsSouth", $"Wolfenstein3D\\Sprites\\49.png", true, false, Direction.SOUTH),
                    new(35, "WeedsWest", $"Wolfenstein3D\\Sprites\\49.png", true, false, Direction.WEST)
            ],
                Animations = [
                AnimationHelpers.Create("RocketTrail", $"Wolfenstein3D\\Sprites", 378, 4, 8f),
                    AnimationHelpers.Create("RocketImpact", $"Wolfenstein3D\\Sprites", 382, 3, 3f)
            ],
                Projectiles =
            [
                ProjectileHelpers.CreateBullet("Bullet", null, null, null),
                ProjectileHelpers.CreateBite("Bite", null, null, null),
                ProjectileHelpers.CreateDrain("DrainLife", null, null, null),
                //ProjectileHelpers.CreateRocket("Rocket", ProjectileSpriteType.ROCKET, $"Wolfenstein3D\\Sprites", 370, null, "RocketTrail", "RocketImpact"),
                ProjectileHelpers.CreateRocket("Rocket", ProjectileSpriteType.ROCKET, $"Wolfenstein3D\\Sprites", 1000, null, "RocketTrail", "RocketImpact"),
                ProjectileHelpers.CreateSerum("KorpsokineticSerum", ProjectileSpriteType.SERUM, $"Wolfenstein3D\\Sprites", 317, null, null, null),
                ProjectileHelpers.CreateFlame("Flame", ProjectileSpriteType.FLAME, $"Wolfenstein3D\\Sprites", 326, null, null, null)
            ],
                Weapons = [
                WeaponHelpers.CreatePistol($"Wolfenstein3D\\Sounds\\digi21.wav"),
                    WeaponHelpers.CreateBite($"Wolfenstein3D\\Sounds\\digi16.wav"),
                    WeaponHelpers.CreateMachineGun($"Wolfenstein3D\\Sounds\\digi10.wav"),
                    WeaponHelpers.CreateChainGun($"Wolfenstein3D\\Sounds\\digi11.wav"),
                    WeaponHelpers.CreateKorpsokineticSerum(null),
                    WeaponHelpers.CreateRocketLauncher(null),
                    WeaponHelpers.CreateFlameThrower(null),
                    WeaponHelpers.CreateDrainLife(null)
            ]
            };


            var deaths = new string[] { "digi11", "digi12", "digi13", "digi14", "digi17", "digi20", "digi28", "digi33", "digi34", "digi29", "digi40", "digi41", "digi42" };
            mod.Enemies = [
                EnemyHelpers.CreateGuard(0, $"Wolfenstein3D\\Sprites", 50, ["digi0"], deaths, []),
                EnemyHelpers.CreateDog(1, $"Wolfenstein3D\\Sprites", 99, ["digi1"], ["digi16"], ["digi29"]),
                EnemyHelpers.CreateSSSoldier(2, $"Wolfenstein3D\\Sprites", 138, ["digi7"], deaths, []),
                EnemyHelpers.CreateMutant(3, $"Wolfenstein3D\\Sprites", 187, [], ["digi17"], []),
                EnemyHelpers.CreateOfficer(4, $"Wolfenstein3D\\Sprites", 238, ["digi27"], deaths, []),
                EnemyHelpers.CreateHansGross(5, $"Wolfenstein3D\\Sprites", 296, ["digi8"], ["digi9"], []),
                EnemyHelpers.CreateDoctorSchabbs(6, $"Wolfenstein3D\\Sprites", 307, ["digi25"], ["digi24"], []),
                EnemyHelpers.CreateMechaHitler(7, $"Wolfenstein3D\\Sprites", 334, ["digi18", "digi23"], ["digi26"], []),
                EnemyHelpers.CreateAdolfHitler(8, $"Wolfenstein3D\\Sprites", 345, ["digi18", "digi23"], ["digi19"], []),
                EnemyHelpers.CreateOttoGiftmacher(9, $"Wolfenstein3D\\Sprites", 360, ["digi37"], ["digi36"], []),
                EnemyHelpers.CreateGretelGross(10, $"Wolfenstein3D\\Sprites", 385, ["digi43"], ["digi44"], []),
                EnemyHelpers.CreateGeneralFettgesicht(11, $"Wolfenstein3D\\Sprites", 396, ["digi38"], ["digi45"], []),
                EnemyHelpers.CreateHitlerGhost(12, $"Wolfenstein3D\\Sprites", 321, [], ["digi26"], []),
                EnemyHelpers.CreateBlinky(13, $"Wolfenstein3D\\Sprites", 288, [], [], []),
                EnemyHelpers.CreatePinky(14, $"Wolfenstein3D\\Sprites", 290, [], [], []),
                EnemyHelpers.CreateInky(15, $"Wolfenstein3D\\Sprites", 294, [], [], []),
                EnemyHelpers.CreateClyde(16, $"Wolfenstein3D\\Sprites", 292, [], [], [])
            ];

            return mod;
        }
    }
}
