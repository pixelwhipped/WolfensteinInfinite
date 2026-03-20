using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.GameMap;

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

        public bool CanInteract(Game game) => OpenAmount == 0f;

        public InteractResult Interact(Game game, Wolfenstein wolfenstein)
        {
            if (IsFake) return InteractResult.Opened;
            if (IsLocked)
            {
                    if (!game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_LOCKED_DOOR))
                    return InteractResult.Locked;
            }
            IsOpening = true;
            AudioPlaybackEngine.Instance.PlaySound(wolfenstein.GameResources.Effects["Door"]);
            return InteractResult.Opened;
        }
    }
}