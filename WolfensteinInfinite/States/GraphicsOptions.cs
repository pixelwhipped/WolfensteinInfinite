using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.MenuUI;
using static WolfensteinInfinite.Engine.Graphics.Dithering;

namespace WolfensteinInfinite.States
{
    public class GraphicsOptions : GameState
    {
        //Window size 320x200 640x400(options) / full screen
        //Resolution 320x200 640x400
        //Quantization on off/64/128/256
        //Dithering
        private Menu Menu { get; init; }
        private string[] DitheringOptions { get; init; }
        public GraphicsOptions(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            DitheringOptions = Enum.GetNames<DitheringMethod>();
            var selectedDitheringOption = Array.IndexOf(DitheringOptions, Enum.GetName(Wolfenstein.Config.Dithering));
            Menu = new Menu(Wolfenstein.GameResources.TitleOptions,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                Wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            Menu.MenuItems.Add(new MenuItemOptionSelector("Window Size", OnMenuAction, ["320x200", "640x400", "Fullscreen"], wolfenstein.Config.WindowSize, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOptionSelector("Resolution", OnMenuAction, ["320x200", "640x400"], wolfenstein.Config.Resolution, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOptionSelector("Quantization", OnMenuAction,
                ["64", "128", "256"], Wolfenstein.Config.Quantization, 180, Wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOptionSelector("Dithering", OnMenuAction,
                DitheringOptions, selectedDitheringOption, 180, Wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOnOff("Weapon Bob", OnMenuAction, Wolfenstein.Config.WeaponBob, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOnOff("Extra Gore", OnMenuAction, Wolfenstein.Config.ExtraGore, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemOnOff("Light Blur", OnMenuAction, Wolfenstein.Config.LightBlur, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItem("Apply", OnApply, Wolfenstein.GameResources.TinyFont));
        }

        private void OnApply(IMenuItem item)
        {
            Wolfenstein.ResetGraphics(out _, out _, out _);
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item is MenuItemOnOff optionOnOff) switch (optionOnOff.Text)
                {
                    case "Weapon Bob":
                        {
                            Wolfenstein.Config.WeaponBob = ((MenuItemOnOff)item).State;
                            break;
                        }
                    case "Light Blur":
                        {
                            Wolfenstein.Config.LightBlur = ((MenuItemOnOff)item).State;
                            break;
                        }
                    case "Extra Gore":
                        {
                            Wolfenstein.Config.ExtraGore = ((MenuItemOnOff)item).State;
                            break;
                        }
                }
            if (item is MenuItemOptionSelector option) switch (option.Text)
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
                case "Dithering":
                    {
                        if(Enum.TryParse(DitheringOptions[option.Current], true, out DitheringMethod dither))
                            {
                                Wolfenstein.Config.Dithering = dither;
                            }
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