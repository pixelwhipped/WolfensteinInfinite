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
        public int Progress = 0;

        public SpecialLevelState(Wolfenstein wolfenstein, Player player,
            Difficulties difficulty, int level, string modName, MapSection section)
            : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
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
                    Wolfenstein, Player, Difficulty, Level);
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

            var builder = new MapGenerator(
                Wolfenstein,
                genWidth,
                genHeight,
                mod, section, sections,
                Level, 1, [], out _);

            Progress = 60;
            Thread.Sleep(50);

            if (!builder.Success)
            {
                NextState = new GameGenerationState(
                    Wolfenstein, Player, Difficulty, Level);
                return;
            }

            var map = builder.ToGameMap(Player, Difficulty, Level);
            if (map == null)
            {
                NextState = new GameGenerationState(
                    Wolfenstein, Player, Difficulty, Level);
                return;
            }

            Progress = 80;
            Thread.Sleep(50);

            map.LoadResources(Wolfenstein);
            Progress = 100;
            Thread.Sleep(50);

            var game = new Game(Guid.NewGuid(), map, Player,
                [.. Wolfenstein.Mods.Keys]);
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