using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    public class Door : IInteractable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float OpenAmount { get; set; }
        public bool IsOpening { get; set; }
        public bool IsClosing { get; set; }
        public float OpenSpeed { get; set; } = 2.0f;
        public float CloseDelay { get; set; } = 3.0f;
        public float CloseTimer { get; set; }
        public int TextureIndex { get; set; }
        public bool IsVertical { get; set; }
        public bool IsLocked { get; set; }
        public bool IsFake { get; set; }

        public bool CanInteract(InGameState state) => OpenAmount == 0f;

        public InteractResult Interact(InGameState state)
        {
            if (IsFake) return InteractResult.Opened;
            if (IsLocked)
            {
                if (!state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_LOCKED_DOOR))
                {
                    state.ShowHudMessage("KEY REQUIRED");
                    return InteractResult.Locked;
                }
            }
            IsOpening = true;
            AudioPlaybackEngine.Instance.PlaySound(state.GameResources.Effects["Door"]);
            return InteractResult.Opened;
        }
    }
}