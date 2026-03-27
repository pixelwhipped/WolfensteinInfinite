using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class LevelSelectState : GameState
    {
        private readonly InGameState InGameState;
        private string _charBuffer = string.Empty;
        public LevelSelectState(Wolfenstein wolfenstein, InGameState inGameState) : base(wolfenstein)
        {
            InGameState = inGameState;
            ReturnState = inGameState;
            NextState = this;

        }

        private void Resume()
        {
            InGameState.NextState = InGameState;
            NextState = InGameState;
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            // Draw the frozen game scene behind the menu
            InGameState.UpdateScene(buffer, 0f);
            var str = $"Enter Level and press\nEnter to start\nor Esc to resume\n{_charBuffer}";
            var (Width, Height) = Wolfenstein.GameResources.TinyFont.MeasureString(str);
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
                $"{str}{((((int)Wolfenstein.Clock.ElapsedTime.AsSeconds()) % 2 == 1) ? "_" : "")}", Wolfenstein.GameResources.TinyFont, RGBA8.WHITE);        

            return NextState;
        }

        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape || k.Code == Wolfenstein.Config.KeyPause)
            {
                Resume();
                return;
            }
            if (k.Code == Keyboard.Key.Backspace && _charBuffer.Length >= 0)
            {

                if (_charBuffer.Length == 1) _charBuffer = string.Empty;
                else _charBuffer = new string(_charBuffer.Take(_charBuffer.Length - 1).ToArray());
            }
            if(k.Code == Keyboard.Key.Enter)
            {
                if(!int.TryParse(_charBuffer, out int level))
                {
                    _charBuffer = string.Empty;
                    return;
                }
                InGameState.Game.Map.Level = Math.Max(level,1); //Pretend where we were to allow specials 
                NextState = new LevelCompleteState(Wolfenstein, InGameState.Game, InGameState.BuildLevelStats());
            }
            var keyChar = k.Code switch
            {
                Keyboard.Key.Numpad0 => "0",
                Keyboard.Key.Numpad1 => "1",
                Keyboard.Key.Numpad2 => "2",
                Keyboard.Key.Numpad3 => "3",
                Keyboard.Key.Numpad4 => "4",
                Keyboard.Key.Numpad5 => "5",
                Keyboard.Key.Numpad6 => "6",
                Keyboard.Key.Numpad7 => "7",
                Keyboard.Key.Numpad8 => "8",
                Keyboard.Key.Numpad9 => "9",
                Keyboard.Key.Num0 => "0",
                Keyboard.Key.Num1 => "1",
                Keyboard.Key.Num2 => "2",
                Keyboard.Key.Num3 => "3",
                Keyboard.Key.Num4 => "4",
                Keyboard.Key.Num5 => "5",
                Keyboard.Key.Num6 => "6",
                Keyboard.Key.Num7 => "7",
                Keyboard.Key.Num8 => "8",
                Keyboard.Key.Num9 => "9",
                _ => null
            };
            if (keyChar == null) return;
            if(!int.TryParse(keyChar,out _)) return;
            _charBuffer += keyChar;
        }
    }
}

