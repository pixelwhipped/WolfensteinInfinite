using SFML.Window;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{

    internal class ControlOptions : GameState
    {
        private Menu Menu { get; init; }
        public ControlOptions(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(wolfenstein.GameResources.TitleControls,
                wolfenstein.GameResources.MenuCommands,
                wolfenstein.GameResources.MenuSelect1,
                wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            Menu.MenuItems.Add(new MenuItemKeyBinder("Up", wolfenstein.Config.KeyUp, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Down", wolfenstein.Config.KeyDown, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Left", wolfenstein.Config.KeyLeft, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Right", wolfenstein.Config.KeyRight, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Fire", wolfenstein.Config.KeyFire, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Open/Use", wolfenstein.Config.KeyOpen, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Stafe", wolfenstein.Config.KeyStafe, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Weapon Next", wolfenstein.Config.KeyWeaponUp, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Weapon Prev", wolfenstein.Config.KeyWeaponDown, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Map", wolfenstein.Config.KeyMap, OnMenuAction, 180, wolfenstein.GameResources.TinyFont));
            Menu.MenuItems.Add(new MenuItemKeyBinder("Pause", wolfenstein.Config.KeyPause, OnMenuAction, 180,
                wolfenstein.GameResources.TinyFont));
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item is not MenuItemKeyBinder keyItem) return;
            if (keyItem.InputKey == null) return;
            switch (item.Text)
            {
                case "Up":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyUp = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Down":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyDown = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Left":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyLeft = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Right":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyRight = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Fire":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyFire = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Open/Use":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyOpen = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Stafe":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyStafe = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Weapon Next":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyWeaponUp = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Weapon Prev":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyWeaponDown = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Map":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyMap = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
                case "Pause":
                    {
                        if (CanSet((Keyboard.Key)keyItem.InputKey))
                            keyItem.Key = Wolfenstein.Config.KeyPause = (Keyboard.Key)keyItem.InputKey;
                        break;
                    }
            }
        }

        private bool CanSet(Keyboard.Key inputKey)
        {
            foreach (var i in Menu.MenuItems)
            {
                if (i is not MenuItemKeyBinder keyItem) continue;
                if (keyItem.Key == inputKey) return false;
            }
            return true;
        }
        private bool AwatingSelection()
        {
            foreach (var i in Menu.MenuItems)
            {
                if (i is not MenuItemKeyBinder keyItem) continue;
                if (keyItem.Selecting) return true;
            }
            return false;
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1f);
            Menu.Draw(buffer, Wolfenstein.Clock);
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (AwatingSelection())
            {
                Menu.OnKeyPressed(k);
                return;
            }
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