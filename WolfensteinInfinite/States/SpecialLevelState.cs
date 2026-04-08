//Wip Work in Progress, Untested
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class SpecialLevelState : GameState
    {
        public readonly Player Player;
        public readonly Difficulties Difficulty;
        public readonly int Level;
        public readonly Guid GameGuid;
        public int Progress = 0;
        private readonly MapGenerator[] PreGenerated;
        public SpecialLevelState(Wolfenstein wolfenstein, Player player, Guid gameGuid,
            Difficulties difficulty, int level, string modName, MapSection section, MapGenerator[] preGenerated)
            : base(wolfenstein)
        {
            PreGenerated = preGenerated;
            Player = player;
            Difficulty = difficulty;
            Level = level;
            GameGuid = gameGuid;
            ReturnState = this;
            NextState = this;

            new Thread(() => BuildMap(modName, section)).Start();
        }

        private void BuildMap(string modName, MapSection section)
        {
            Progress = 10;
            Thread.Sleep(50);

            if (!Wolfenstein.Mods.TryGetValue(modName, out var mod))
            {
                NextState = new GameGenerationState(
                    Wolfenstein, Player, GameGuid, Difficulty, Level,64, PreGenerated);
                return;
            }

            Progress = 30;
            Thread.Sleep(50);

            // Special maps are self-contained single sections
            var sections = new Dictionary<Mod, MapSection[]>
            {
                { mod, [section] }
            };
            const int Border = 1;
            int genWidth = section.Width + Border * 2;
            int genHeight = section.Height + Border * 2;

            var builder = MapGenerator.GetMapGenerator(
                Wolfenstein,
                genWidth,
                genHeight,
                mod, section, sections,
                Level, 1, []);
            builder?.TryBuild();
            Progress = 60;
            Thread.Sleep(50);

            if (builder == null || !builder.Success)
            {
                NextState = new GameGenerationState(
                    Wolfenstein, Player, GameGuid, Difficulty, Level,64, PreGenerated);
                return;
            }

            var map = builder.ToGameMap(Player, Difficulty, Level);
            if (map == null)
            {
                NextState = new GameGenerationState(
                    Wolfenstein, Player, GameGuid, Difficulty, Level,64, PreGenerated);
                return;
            }

            Progress = 80;
            Thread.Sleep(50);

            map.LoadResources(Wolfenstein);
            Progress = 100;
            Thread.Sleep(50);            
            var game = new Game(Guid.NewGuid(), map, Player, Wolfenstein.ActiveMods);
            NextState = new InGameState(Wolfenstein, game);
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            // Dark red tint distinguishes from normal level loading
            buffer.Clear(40, 0, 0);
            var x = (buffer.Width / 2) - (Wolfenstein.GameResources.GetPsyched.Width / 2);
            var y = (buffer.Height / 2) - (Wolfenstein.GameResources.GetPsyched.Height / 2);
            buffer.Draw(x, y, Wolfenstein.GameResources.GetPsyched);
            buffer.RectFill(
                x,
                y + Wolfenstein.GameResources.GetPsyched.Height + 2,
                (int)(Wolfenstein.GameResources.GetPsyched.Width * (Progress / 100f)),
                4, 255, 0, 0);
            return NextState;
        }

        public override void OnKeyPressed(KeyEventArgs k)
        {
            // Block all input during loading
        }
    }
}