//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class Options : GameState
    {
        private Menu Menu { get; init; }

        public Options(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleOptions,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            Menu.MenuItems.Add(new MenuItem("Mods", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Graphics", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Audio", OnMenuAction, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItem("Controls", OnMenuAction, wolfenstein.GameResources.SmallFont));            
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item.Text == "Graphics")
            {
                NextState = new GraphicsOptions(Wolfenstein, this);
            }else if (item.Text == "Audio")
            {
                NextState = new AudioOptions(Wolfenstein, this);
            }
            else if (item.Text == "Controls")
            {
                NextState = new ControlOptions(Wolfenstein, this);
            }
            else if (item.Text == "Mods")
            {
                NextState = new ModsOptions(Wolfenstein, this);
            }
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1f);
            Menu.Draw(buffer, Wolfenstein.Clock);
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                ReturnState.NextState = ReturnState;
                NextState = ReturnState;
                return;
            }
            Menu.OnKeyPressed(k);
        }
    }
}
