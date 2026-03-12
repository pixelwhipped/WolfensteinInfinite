//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class GraphicsOptions : GameState
    {
        //Window size 320x200 640x400(options) / full screen
        //Resolution 320x200 640x400
        //Quantization on off/64/128/256
        private Menu Menu { get; init; }

        public GraphicsOptions(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleOptions,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            Menu.MenuItems.Add(new MenuItemOptionSelector("Window Size", OnMenuAction, ["320x200", "640x400", "Fullscreen"], wolfenstein.Config.WindowSize, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOptionSelector("Resolution", OnMenuAction, ["320x200", "640x400"], wolfenstein.Config.Resolution, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOptionSelector("Quantization", OnMenuAction, 
                ["64", "128", "256"], wolfenstein.Config.Quantization, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItem("Apply", OnApply, wolfenstein.GameResources.TinyFont));
        }

        private void OnApply(IMenuItem item)
        {
            Wolfenstein.ResetGraphics(out _, out _, out _);
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item is not MenuItemOptionSelector option) return;
            switch (option.Text)
            {
                case "Window Size":
                    {
                        Wolfenstein.Config.WindowSize = option.Current;
                        break;
                    }
                case "Resolution":
                    {
                        Wolfenstein.Config.Resolution = option.Current;
                        break;
                    }
                case "Quantization":
                    {
                        Wolfenstein.Config.Quantization = option.Current;
                        break;
                    }
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