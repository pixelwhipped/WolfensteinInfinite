//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class ModsOptions : GameState
    {
        private Menu Menu { get; init; }
        public ModsOptions(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleCustomize,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                Wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            var hasMods = Wolfenstein.Mods.Keys.Count != 0;
            Menu.MenuItems.Add(new MenuItem("Wiki", OnMenuAction, Wolfenstein.GameResources.SmallFont, hasMods, hasMods ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem("Rebuild", OnMenuAction, Wolfenstein.GameResources.SmallFont, true));
            foreach (var mod in Wolfenstein.Config.Mods)
            {
                Menu.MenuItems.Add(new MenuItemOnOff(mod.Name, OnMenuAction, mod.Enabled, 180, Wolfenstein.GameResources.TinyFont));
            }
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item.Text == "Wiki")
            {
                NextState = new WikiState(Wolfenstein, this);
                return;
            }
            else if (item.Text == "Rebuild")
            {
                NextState = new RebuildState(Wolfenstein, this);
            }
            if (item is not MenuItemOnOff mItem) return;
            foreach (var mod in Wolfenstein.Config.Mods)
            {
                if (mod.Name == mItem.Text)
                {
                    mod.Enabled = mItem.State;
                    if (mod.Name == "Demo" && mItem.State)
                    {
                        Disable("Wolfenstein3D");
                    }
                    if (mod.Name == "Wolfenstein3D" && mItem.State)
                    {
                        Disable("Demo");
                    }
                    break;
                }
            }
        }
        public void Disable(string mod)
        {
            foreach (var m in Wolfenstein.Config.Mods)
            {
                if (m.Name == mod)
                {
                    m.Enabled = false;
                    break;
                }
            }
            foreach (var m in Menu.MenuItems)
            {
                if (m is not MenuItemOnOff mItem) continue;
                if (mItem.Text == mod)
                {
                    mItem.State = false;
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