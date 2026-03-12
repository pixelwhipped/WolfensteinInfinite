//Clean
using SFML.System;
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.States
{
    public abstract class GameState
    {
        protected GameState(Wolfenstein wolfenstein)
        {
            Wolfenstein = wolfenstein;
            ReturnState = this;
        }

        public Wolfenstein Wolfenstein { get; init; }
        public Clock Clock => Wolfenstein.Clock;
        public GameResources GameResources => Wolfenstein.GameResources;
        public GameState? NextState { get; set; }
        public GameState ReturnState { get; init; }
        public List<Keyboard.Key> Keys => Wolfenstein.Graphics.Keys;
        public bool IsKeyDown() => Wolfenstein.Graphics.IsKeyDown();
        public bool IsKeyDown(Keyboard.Key code) => Wolfenstein.Graphics.IsKeyDown(code);
        public virtual void OnKeyPressed(KeyEventArgs k) { }
        public virtual void OnKeyReleased(KeyEventArgs k) { }
        public abstract GameState? Update(Texture32 buffer, float frameTime);
    }
}
