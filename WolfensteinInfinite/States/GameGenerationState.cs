using SFML.Window;
using System.Windows.Controls.Primitives;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class GameGenerationRetryState : GameState
    {
        public readonly Player Player;
        public readonly Difficulties Difficulty;
        public readonly int Level;
        public const string FailedString = "Defeated by map generation.\nPress Y to try again.\nPress N to give up.";
        public GameGenerationRetryState(Wolfenstein wolfenstein, Player player, Difficulties difficulty, int level) : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
            ReturnState = this;
            NextState = this;
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {

            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1);

            var (Width, Height) = Wolfenstein.GameResources.TinyFont.MeasureString(FailedString);
            var uw = Wolfenstein.GameResources.TinyFont.MeasureString("_").Width;

            var rWidth = Width + uw + 10;
            var rHeight = Height + 10;
            var xOff = (buffer.Width - rWidth) / 2;
            var yOff = (buffer.Height - rHeight) / 2; ;
            buffer.RectFill(xOff, yOff, rWidth, rHeight, 20, 20, 20);
            buffer.Line(xOff, yOff, xOff + rWidth, yOff, 52, 52, 52);
            buffer.Line(xOff, yOff, xOff, yOff + rHeight, 52, 52, 52);
            buffer.Line(xOff, yOff + rHeight, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            buffer.Line(xOff + rWidth, yOff, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            buffer.DrawString(xOff + 5, yOff + 5,
                $"{FailedString}{((((int)Wolfenstein.Clock.ElapsedTime.AsSeconds()) % 2 == 1) ? "_" : "")}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);

            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {

            if (k.Code == Keyboard.Key.Y)
            {
                NextState = new GameGenerationState(Wolfenstein, Player, Difficulty, Level);
                return;
            }
            if (k.Code == Keyboard.Key.N)
            {
                NextState = new MenuState(Wolfenstein, null);
                return;
            }
        }
    }
    public class GameGenerationState : GameState
    {
        public readonly Player Player;
        public readonly Difficulties Difficulty;
        public readonly int Level;
        public int Progress = 0;

        public GameGenerationState(Wolfenstein wolfenstein, Player player, Difficulties difficulty, int level) : base(wolfenstein)
        {
            Player = player;
            Difficulty = difficulty;
            Level = level;
            ReturnState = this;
            NextState = this;
            new Thread(new ThreadStart(() => { GenerateMap(); })).Start();
        }
        public void GenerateMap()
        {
            int maxAttempts = 20;
            MapFlags[] attemptObjectives = [];
            var mods = Wolfenstein.Config.Mods.Where(p => p.Enabled);
            var modBuilders = Wolfenstein.BuilderMods.Where(p => mods.Any(mo => mo.Name == p.Key) && p.Value.MapSections.Length > 0).ToArray();
            if (modBuilders.Length == 0) throw new Exception("No Mods with Level Sections");
            Progress = 10;
            Thread.Sleep(50);

            bool hasExit = false;
            var sections = new Dictionary<Mod, MapSection[]>();
            var rootOptions = new List<(Mod m, MapSection s)>();
            foreach (var mod in modBuilders)
            {
                var cm = Wolfenstein.Mods[mod.Key];
                var cs = mod.Value.MapSections.OrderBy(x => Random.Shared.Next()).ToArray();
                sections.Add(cm, cs);
                if (cs.Any(p => p.HasPlayerExit)) hasExit = true;
                foreach (var section in cs.Where(p => p.HasPlayerStart)) rootOptions.Add((cm, section));

            }
            if (rootOptions.Count == 0) throw new Exception("No Mods with Level Sections");
            if (!hasExit) throw new Exception("No Exits");

            var (m, s) = rootOptions[Random.Shared.Next(0, rootOptions.Count)];

            var avgRoomDim = Math.Ceiling(sections.Average(p => p.Value.Average(k => k.Width * k.Height)));
            var maxRooms = Math.Ceiling((Wolfenstein.Config.MaxMapSize * Wolfenstein.Config.MaxMapSize) / avgRoomDim);

            var targetRooms = Math.Max((int)Math.Ceiling((Math.Clamp(Level, 1, 100) / 100f) * maxRooms), 15);
            Progress = 20;
            Thread.Sleep(50);
            ///Will need a test to check if new level differnt from previous level.
            MapGenerator builder = new MapGenerator(Wolfenstein, Wolfenstein.Config.MaxMapSize, Wolfenstein.Config.MaxMapSize, m, s, sections, Level, targetRooms, attemptObjectives, out string[] finalPassErrors);
            if (!builder.Success) //Retries
            {
                var inc = 40 / maxAttempts;
                for (int i = 0; i < maxAttempts; i++)
                {
                    builder = new MapGenerator(Wolfenstein, Wolfenstein.Config.MaxMapSize, Wolfenstein.Config.MaxMapSize, m, s, sections, Level, targetRooms, attemptObjectives, out finalPassErrors);
                    if (builder.Success) break;
                    Progress += inc;
                    Thread.Sleep(50);
                }
            }
            if (!builder.Success)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, Difficulty, Level);
                return;
            }
            Progress = 60;
            Thread.Sleep(50);

            var map = builder.ToGameMap(Player, Difficulty, Level);
            if (map == null)
            {
                NextState = new GameGenerationRetryState(Wolfenstein, Player, Difficulty, Level);
                return;
            }
            Progress = 80;
            Thread.Sleep(50);
            map.LoadResources(Wolfenstein);
            Progress = 100;
            Thread.Sleep(50);

            var game = new Game(Guid.NewGuid(), map, Player, [.. mods.Select(p=>p.Name)]);
            NextState = new InGameState(Wolfenstein, game);
        }
        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            var x = (buffer.Width / 2) - (Wolfenstein.GameResources.GetPsyched.Width / 2);
            var y = (buffer.Height / 2) - (Wolfenstein.GameResources.GetPsyched.Height / 2);
            buffer.Draw(x, y, Wolfenstein.GameResources.GetPsyched);
            buffer.RectFill(x, y + Wolfenstein.GameResources.GetPsyched.Height + 2,
                (int)(Wolfenstein.GameResources.GetPsyched.Width * (Progress / 100f)), 4,
                RGBA8.DARK_PURPLE.R,
                RGBA8.DARK_PURPLE.G,
                RGBA8.DARK_PURPLE.B);

            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                return;
            }
        }
    }
}
