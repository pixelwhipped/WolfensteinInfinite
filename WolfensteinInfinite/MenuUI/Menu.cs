using SFML.System;
using SFML.Window;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.MenuUI
{
    public class Menu(Texture32? title, Texture32? menuCommands, Texture32? menuSelect1, Texture32? menuSelect2, CachedSound? change, IGameFont? inputFont = null)
    {
        public int? FixedXPosition;
        public CachedSound? Change { get; init; } = change;
        public Texture32? MenuSelect1 { get; init; } = menuSelect1;
        public Texture32? MenuSelect2 { get; init; } = menuSelect2;
        public Texture32? SideIcon { get; set; }
        public Texture32? MenuCommands { get; init; } = menuCommands;
        public Texture32? Title { get; init; } = title;
        public List<IMenuItem> MenuItems { get; init; } = [];
        public int Selected = 0;
        public IGameFont? InputFont { get; init; } = inputFont;
        public string InputString = "B.J Blazkowicz";
        public void Draw(Texture32 buffer, Clock clock)
        {
            var yOff = 0;
            if (Title != null)
            {
                yOff += 1;
                buffer.Draw((buffer.Width - Title.Width) / 2, yOff, Title);
                yOff += Title.Height + 1;
            }

            if (MenuItems.Count == 0) return;
            var rHeight = 0;
            var maxItems = 0;
            foreach (var i in MenuItems)
            {
                var nHight = (i.Height + i.Height / 6);
                if (nHight + rHeight + yOff > (buffer.Height - 20)) break;
                maxItems++;
                rHeight += nHight;
            }
            rHeight += 6;
            var selectViewOffset = Math.Max(Selected - maxItems + 1, 0);
            var rWidth = (MenuSelect1 == null) ? 0 : MenuSelect1.Width + 20 + MenuItems.Max(p => p.GetWidth());
            var xOff = FixedXPosition ?? (buffer.Width - rWidth) / 2;
            if (InputFont != null)
            {
                if ((int)clock.ElapsedTime.AsSeconds() % 2 == 1)
                    buffer.DrawString(xOff, yOff, InputString, InputFont, RGBA8.STEEL_BLUE);
                else
                    buffer.DrawString(xOff, yOff, $"{InputString}_", InputFont, RGBA8.STEEL_BLUE);
                yOff += InputFont.Height;
            }
            else
            {
                yOff += (buffer.Height - yOff - rHeight) / 2;
            }
            buffer.RectFill(xOff, yOff, rWidth, rHeight, 20, 20, 20);
            buffer.Line(xOff, yOff, xOff + rWidth, yOff, 52, 52, 52);
            buffer.Line(xOff, yOff, xOff, yOff + rHeight, 52, 52, 52);
            buffer.Line(xOff, yOff + rHeight, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            buffer.Line(xOff + rWidth, yOff, xOff + rWidth, yOff + rHeight, 16, 16, 16);
            yOff += 2;
            if (SideIcon != null)
            {
                buffer.Draw(xOff + rWidth - (SideIcon.Width + 6), yOff, SideIcon);
            }
            if (MenuSelect1 != null && MenuSelect2 != null)
                xOff += MenuSelect1.Width;
            for (int i = selectViewOffset; i < Math.Min(maxItems + selectViewOffset, MenuItems.Count); i++)
            {
                if (i == Selected && MenuSelect1 != null && MenuSelect2 != null)
                {
                    if ((int)clock.ElapsedTime.AsSeconds() % 2 == 1)
                        buffer.Draw(xOff - MenuSelect1.Width, yOff, MenuSelect1);
                    else
                        buffer.Draw(xOff - MenuSelect2.Width, yOff, MenuSelect2);
                }
                IMenuItem? item = MenuItems[i];
                yOff += item.Draw(xOff + 10, yOff, buffer);

            }
            if (MenuCommands != null)
                buffer.Draw((buffer.Width - MenuCommands.Width) / 2, buffer.Height - MenuCommands.Height, MenuCommands);

        }
        public bool InAwaitImput = false;
        internal void OnKeyPressed(KeyEventArgs k)
        {
            if (MenuItems[Selected] is MenuItemKeyBinder inputAwaiter)
            {
                if (InAwaitImput)
                {
                    inputAwaiter.InputKey = k.Code;
                    inputAwaiter.Action(inputAwaiter);
                    InAwaitImput = inputAwaiter.Selecting = false;
                    return;
                }
                else
                {
                    if (k.Code == Keyboard.Key.Enter)
                    {
                        InAwaitImput = true;
                        inputAwaiter.Selecting = true;
                        return;
                    }
                }
            }
            if (k.Code == Keyboard.Key.Enter)
            {
                if (MenuItems[Selected] is MenuItemOnOff item) item.State = !item.State;
                AudioPlaybackEngine.Instance.PlaySound(Change);
                MenuItems[Selected]?.Action(MenuItems[Selected]);
            }
            else if (k.Code == Keyboard.Key.Up)
            {
                AudioPlaybackEngine.Instance.PlaySound(Change);
                Selected--;
                if (Selected < 0) Selected = MenuItems.Count - 1;
            }
            else if (k.Code == Keyboard.Key.Down)
            {
                AudioPlaybackEngine.Instance.PlaySound(Change);
                Selected++;
                if (Selected == MenuItems.Count) Selected = 0;
            }
            else if (k.Code == Keyboard.Key.Left)
            {
                if (MenuItems[Selected] is MenuItemNumber item)
                {
                    item.Number = Math.Max(item.Number - 1, item.Min);
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                    AudioPlaybackEngine.Instance.PlaySound(Change);
                }
                if (MenuItems[Selected] is MenuItemOnOff item2)
                {
                    item2.State = !item2.State;
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                }
                if (MenuItems[Selected] is MenuItemOptionSelector item3)
                {
                    item3.Current--;
                    if (item3.Current < 0) item3.Current = item3.Options.Length - 1;
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                    AudioPlaybackEngine.Instance.PlaySound(Change);
                }
            }
            else if (k.Code == Keyboard.Key.Right)
            {
                if (MenuItems[Selected] is MenuItemNumber item)
                {
                    item.Number = Math.Min(item.Number + 1, item.Max);
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                    AudioPlaybackEngine.Instance.PlaySound(Change);
                }
                if (MenuItems[Selected] is MenuItemOnOff item2)
                {
                    item2.State = !item2.State;
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                }
                if (MenuItems[Selected] is MenuItemOptionSelector item3)
                {
                    item3.Current++;
                    if (item3.Current > item3.Options.Length - 1) item3.Current = 0;
                    MenuItems[Selected]?.Action(MenuItems[Selected]);
                    AudioPlaybackEngine.Instance.PlaySound(Change);
                }
            }
            if (InputFont != null)
            {
                if (k.Code == Keyboard.Key.Backspace && InputString.Length >= 0)
                {

                    if (InputString.Length == 1) InputString = string.Empty;
                    else InputString = new string(InputString.Take(InputString.Length - 1).ToArray());
                }
                else
                {
                    var s = Keyboard.GetDescription(k.Scancode);
                    var shift = Keyboard.IsKeyPressed(Keyboard.Key.LShift) || Keyboard.IsKeyPressed(Keyboard.Key.RShift);
                    var upper = Console.CapsLock && !shift || shift && !Console.CapsLock;
                    var c = upper ? char.ToUpper(s[0]) : char.ToLower(s[0]);
                    if (s.Length == 1 && InputFont.HasChar(c))
                        InputString += c;
                    InputString = new string(InputString.Take(16).ToArray()).Trim();
                }
            }
            if (!MenuItems[Selected].Enabled && MenuItems.Any(p => p.Enabled)) OnKeyPressed(k);
        }
    }
}
