using SFML.Graphics;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameHelpers;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite
{
    public class GameResources
    {
        public Dictionary<string, CachedSound> Effects { get; init; } = [];
        public Image WindowIcon { get; init; }
        public byte[] Pallet { get; init; }
        public Font DebugFont { get; init; }
        public GameFont MenuFont { get; init; }
        public GameFont NumberFont { get; init; }
        public BasicGameFont LargeFont { get; init; }
        public BasicGameFont SmallFont { get; init; }
        public BasicGameFont TinyFont { get; init; }

        public Texture32 TitleWolfenstein { get; init; }
        public Texture32 Title3D { get; init; }
        public Texture32 TitleInfinite { get; init; }
        public Texture32 TitleAni1 { get; init; }
        public Texture32 TitleAni2 { get; init; }
        public Texture32 TitleOptions { get; init; }
        public Texture32 TitleHighScores { get; init; }
        public Texture32 TitleNewGame { get; init; }

        public Texture32 TitleControls { get; init; }
        public Texture32 TitleCustomize { get; init; }
        public Texture32 TitlePause { get; init; }
        public Texture32 TitleLoadGame { get; init; }
        public Texture32 TitleSaveGame { get; init; }
        public Texture32 MenuCommands { get; init; }
        public Texture32 MenuSelect1 { get; init; }
        public Texture32 MenuSelect2 { get; init; }
        public Texture32 GetPsyched { get; init; }

        public Texture32 Hud { get; init; }
        public Texture32 KeyOn { get; init; }
        public Texture32 KeyOff { get; init; }
        public Texture32 DynamiteOn { get; init; }
        public Texture32 DynamiteOff { get; init; }
        public Texture32 PrisonerOfWarOn { get; init; }
        public Texture32 PrisonerOfWarOff { get; init; }
        public Texture32 SecretOn { get; init; }
        public Texture32 SecretOff { get; init; }
        public Texture32 EditRandomEnemy { get; init; }
        public Texture32 EditExperimentEnemy { get; init; }
        public Texture32 EditWallAny { get; init; }
        public Texture32 Chance5 { get; init; }
        public Texture32 Chance25 { get; init; }
        public Texture32 Chance50 { get; init; }
        public Texture32 Chance75 { get; init; }

        public Texture32 EditExit { get; init; }
        public Texture32 EditNorth { get; init; }
        public Texture32 EditEast { get; init; }
        public Texture32 EditSouth { get; init; }
        public Texture32 EditWest { get; init; }
        public Texture32 ElevatorSwitchUp { get; init; }
        public Texture32 ElevatorSwitchDown { get; init; }
        public Texture32 ElevatorSide { get; init; }
        public Texture32 ElevatorDoor { get; init; }
        public Texture32 PlayerStart { get; init; }
        public GameGraphics.Animation[] HudFaces { get; init; }
        public GameResources(byte[] pallet)
        {
            Pallet = pallet;
            // ICON
            WindowIcon = FileHelpers.Shared.LoadImage($"GameData\\Base\\Pictures\\Icon.png");

            // FONTS
            DebugFont = FileHelpers.Shared.LoadFont("arial.ttf");
            MenuFont = new GameFont();
            var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            foreach (var c in chars)
                MenuFont.AddChar(c, FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\{c}.png"));
            MenuFont.AddChar('!', FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Exclamation.png"));
            MenuFont.AddChar('%', FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Percent.png"));
            MenuFont.AddChar(':', FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Colon.png"));
            MenuFont.AddChar('.', FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Period.png"));
            NumberFont = new GameFont();
            chars = "0123456789".ToCharArray();
            foreach (var c in chars)
                NumberFont.AddChar(c, FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\S{c}.png"));

            var basicFontChars = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{}~|".ToCharArray();
            SmallFont = new BasicGameFont(FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\FontSmall.png"), basicFontChars);
            LargeFont = new BasicGameFont(FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\FontLarge.png"), basicFontChars);
            TinyFont = new BasicGameFont(FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\FontTiny.png"), basicFontChars);

            // Images
            TitleWolfenstein = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Wolfenstein.png");
            Title3D = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\3D.png");
            TitleInfinite = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Infinite.png");
            TitleAni1 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\TitleAni1.png");
            TitleAni2 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\TitleAni2.png");
            TitleOptions = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Options.png");
            MenuCommands = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\MenuCommands.png");
            MenuSelect1 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\MenuSelect1.png");
            MenuSelect2 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\MenuSelect2.png");
            TitleHighScores = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\HighScores.png");
            TitleCustomize = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Customize.png");
            TitleControls = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Control.png");
            TitleNewGame = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\NewGame.png");
            GetPsyched = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\GetPsyched.png");

            Hud = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Hud.png");
            KeyOn = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\KeyOn.png");
            KeyOff = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\KeyOff.png");
            DynamiteOn = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\DynamiteOn.png");
            DynamiteOff = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\DynamiteOff.png");
            SecretOn = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\SecretOn.png");
            SecretOff = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\SecretOff.png");
            PrisonerOfWarOn = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\PowOn.png");
            PrisonerOfWarOff = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\PowOff.png");
            TitlePause = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Paused.png");
            TitleSaveGame = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\SaveGame.png");
            TitleLoadGame = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\LoadGame.png");
            HudFaces =
            [
                AnimationHelpers.Create(AnimationHelpers.Create("GodMode", "GameData\\Base\\Pictures", ["GodMode.png"], 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("90+", "GameData\\Base\\Pictures", 115, 3, 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("70+", "GameData\\Base\\Pictures", 118, 3, 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("55+", "GameData\\Base\\Pictures", 121, 3, 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("35+", "GameData\\Base\\Pictures", 124, 3, 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("20+", "GameData\\Base\\Pictures", 127, 3, 1)),
                AnimationHelpers.Create(AnimationHelpers.Create("0+", "GameData\\Base\\Pictures", 130, 2, 1))
            ];
            EditRandomEnemy = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\RandomEnemy.png");
            EditExperimentEnemy = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\ExperimentEnemy.png");

            EditExit = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Exit.png");
            EditWallAny = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\WallAny.png");
            EditNorth = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\North.png");
            EditEast = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\East.png");
            EditSouth = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\South.png");
            EditWest = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\West.png");
            Chance5 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Chance5.png");
            Chance25 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Chance25.png");
            Chance50 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Chance50.png");
            Chance75 = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\Chance75.png");

            PlayerStart = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Pictures\\PlayerStart.png");
            ElevatorSwitchUp = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Textures\\ElevatorUp.png");
            ElevatorSwitchDown = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Textures\\ElevatorDown.png");
            ElevatorSide = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Textures\\ElevatorSide.png");
            ElevatorDoor = FileHelpers.Shared.LoadSurface32($"GameData\\Base\\Textures\\ElevatorDoor.png");

            //Sounds
            Effects.Add("ChangeMenu", FileHelpers.Shared.LoadAudio($"GameData\\Base\\Sounds\\ChangeMenu.wav"));
            Effects.Add("Door", FileHelpers.Shared.LoadAudio($"GameData\\Base\\Sounds\\Door.wav"));
            Effects.Add("Pushwall", FileHelpers.Shared.LoadAudio($"GameData\\Base\\Sounds\\Pushwall.wav"));
            Effects.Add("Pickup", FileHelpers.Shared.LoadAudio($"GameData\\Base\\Sounds\\Pickup.wav"));
        }
    }
}
