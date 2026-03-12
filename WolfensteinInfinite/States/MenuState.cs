//Clean
using SFML.Window;
using WolfensteinInfinite.Editor;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class MenuState : GameState
    {
        private static readonly string[] ExitStrings = [
            "Dost thou wish to\nleave with such hasty\nabandon?",
            "Chickening out...\nalready?",
            "Press N for more carnage.\nPress Y to be a weenie.",
            "So, you think you can\nquit this easily, huh?",
            "Press N to save the world.\nPress Y to abandon it in\nits hour of need.",
            "Press N if you are brave.\nPress Y to cower in shame.",
            "Heroes, press N.\nWimps, press Y.",
            "You are at an intersection.\nA sign says, 'Press Y to quit.'\n>",
            "For guns and glory, press N.\nFor work and worry, press Y.",
            "Heroes don't quit, but\ngo ahead and press Y\nif you aren't one.",
            "Press Y to quit,\nor press N to enjoy\nmore violent diversion.",
            "Depressing the Y key means\nyou must return to the\nhumdrum workday world.",
            "Hey, quit or play,\nY or N:\nit's your choice.",
            "Sure you don't want to\nwaste a few more\nproductive hours?",
            "I think you had better\nplay some more. Please\npress N...please?",
            "If you are tough, press N.\nIf not, press Y daintily.",
            "I'm thinkin' that\nyou might wanna press N\nto play more. You do it.",
            "Sure. Fine. Quit.\nSee if we care.\nGet it over with.\nPress Y."
        ];
        public bool ConfirmExit = false;
        public string ExitString = ExitStrings[Random.Shared.Next(ExitStrings.Length)];

        private float MenuFade = 1f;
        private Menu Menu { get; init; }
        private MapEditor? MapEditor;
        private MenuItem EditorItem { get; init; }

        private string? _statusMessage = null;
        private float _statusTimer = 0f;
        private const float StatusDuration = 3f;
        public MenuState(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleOptions,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]);

            var hasSave = SaveGame.Exists();
            Menu.MenuItems.Add(new MenuItem("New Game", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Continue", OnMenuAction, wolfenstein.GameResources.SmallFont,
                hasSave, hasSave ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem("Options", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("View Score", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Quit", OnMenuAction, wolfenstein.GameResources.SmallFont));
            EditorItem = new MenuItem("Editor", OnMenuAction, wolfenstein.GameResources.SmallFont);
            if (Args.EditorEnabled)
                Menu.MenuItems.Add(EditorItem);
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item.Text == "Quit")
            {
                ExitString = ExitStrings[Random.Shared.Next(ExitStrings.Length)];
                ConfirmExit = true;
            }
            else if (item.Text == "View Score")
            {
                NextState = new HighScores(Wolfenstein, this);
            }
            else if (item.Text == "Options")
            {
                NextState = new Options(Wolfenstein, this);
            }
            else if (item.Text == "New Game")
            {
                if (Args.TestMode)
                {
                    var testMap = Wolfenstein.TestMaps
                        .FirstOrDefault(m => m.Value.Length > 0);
                    if (testMap.Value != null)
                    {
                        NextState = new SpecialLevelState(
                            Wolfenstein,
                            new Player("TEST"),
                            Difficulties.BRING_EM_ON,
                            1,
                            testMap.Key,
                            testMap.Value[0]);
                        return;
                    }
                }
                NextState = new NewGameState(Wolfenstein, this);
            }
            else if (item.Text == "Continue")
            {
                var save = SaveGame.Load();
                if (save == null)
                {
                    ShowStatus("No save found.");
                    return;
                }
                if (!save.ValidateMods(Wolfenstein, out var missing))
                {
                    ShowStatus($"Missing mods: {string.Join(", ", missing)}");
                    return;
                }
                save.Map.LoadResources(Wolfenstein);
                var px = save.Player.PlaneX;
                var py = save.Player.PlaneY;
                NextState = new InGameState(Wolfenstein, new Game(save.GameId, save.Map, save.Player, save.Mods));
                //Restore plane
                save.Player.PlaneX = px;
                save.Player.PlaneY = py;
            }
            else if (item.Text == "Editor")
            {
                if (MapEditor != null) return;
                EditorItem.Color = RGBA8.STEEL_BLUE;
                MapEditor = new MapEditor(Wolfenstein);
                MapEditor.Closing += (_, _) =>
                {
                    EditorItem.Color = null;
                    MapEditor = null;
                };
                MapEditor.Show();
            }
        }

        private void ShowStatus(string message)
        {
            _statusMessage = message;
            _statusTimer = StatusDuration;
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            MenuFade += frameTime;
            MenuFade = Math.Clamp(MenuFade, 0f, 1f);
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, MenuFade);
            Menu.Draw(buffer, Wolfenstein.Clock);

            if (_statusMessage != null)
            {
                _statusTimer -= frameTime;
                if (_statusTimer <= 0f)
                {
                    _statusMessage = null;
                }
                else
                {
                    var (w, h) = Wolfenstein.GameResources.TinyFont.MeasureString(_statusMessage);
                    var x = (buffer.Width - w) / 2;
                    var y = buffer.Height - h - 10;
                    buffer.RectFill(x - 5, y - 5, w + 10, h + 10, 20, 20, 20);
                    buffer.DrawString(x, y, _statusMessage, Wolfenstein.GameResources.TinyFont, RGBA8.YELLOW);
                }
            }

            if (ConfirmExit)
            {
                var (Width, Height) = Wolfenstein.GameResources.TinyFont.MeasureString(ExitString);
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
                    $"{ExitString}{((((int)Wolfenstein.Clock.ElapsedTime.AsSeconds()) % 2 == 1) ? "_" : "")}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);
            }
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                if (ReturnState == this)
                {
                    if (ConfirmExit)
                    {
                        ConfirmExit = false;
                    }
                    else
                    {
                        ConfirmExit = true;
                        ExitString = ExitStrings[Random.Shared.Next(ExitStrings.Length)];
                    }
                }
                else
                {
                    NextState = ReturnState;
                }
                return;
            }
            if (ConfirmExit && k.Code == Keyboard.Key.Y)
            {
                NextState = ReturnState == this ? null : ReturnState;
                return;
            }
            if (ConfirmExit && k.Code == Keyboard.Key.N)
            {
                ConfirmExit = false;
                return;
            }
            if (!ConfirmExit)
                Menu.OnKeyPressed(k);
        }
    }
}
