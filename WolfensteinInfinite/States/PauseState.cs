//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class PauseState : GameState
    {
        private readonly InGameState InGameState;
        private readonly Menu Menu;
        private string? _statusMessage = null;
        private float _statusTimer = 0f;
        private const float StatusDuration = 2f;

        public PauseState(Wolfenstein wolfenstein, InGameState inGameState) : base(wolfenstein)
        {
            InGameState = inGameState;
            ReturnState = inGameState;
            NextState = this;

            Menu = new Menu(null,
                wolfenstein.GameResources.MenuCommands,
                wolfenstein.GameResources.MenuSelect1,
                wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]);

            Menu.MenuItems.Add(new MenuItem("Resume", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Save Game", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Exit to Menu", OnMenuAction, wolfenstein.GameResources.SmallFont));
        }

        private void OnMenuAction(IMenuItem item)
        {
            switch (item.Text)
            {
                case "Resume":
                    Resume();
                    break;

                case "Save Game":
                    InGameState.AutoSave();
                    ShowStatus("Game Saved");
                    break;

                case "Exit to Menu":
                    InGameState.RecordHighScore();
                    InGameState.AutoSave();
                    // Fresh MenuState so Continue is enabled with the new save
                    NextState = new MenuState(Wolfenstein, null);
                    break;
            }
        }

        private void Resume()
        {
            InGameState.NextState = InGameState;
            NextState = InGameState;
        }

        private void ShowStatus(string message)
        {
            _statusMessage = message;
            _statusTimer = StatusDuration;
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            // Draw the frozen game scene behind the menu
            InGameState.UpdateScene(buffer, 0f);

            // Dark overlay
            buffer.RectFill(0, 0, buffer.Width, buffer.Height, 0, 0, 0, 160);

            // Menu
            Menu.Draw(buffer, Wolfenstein.Clock);

            // Status message (e.g. "Game Saved")
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

            return NextState;
        }

        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape || k.Code == Wolfenstein.Config.KeyPause)
            {
                Resume();
                return;
            }
            Menu.OnKeyPressed(k);
        }
    }
}