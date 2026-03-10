using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class Door : IInteractable
    {
        public int X { get; set; }
        public int Y { get; set; }
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

        public InteractResult Interact(Game game)
        {
            if (IsFake) return InteractResult.Opened;
            if (IsLocked)
            {
                if (!game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_LOCKED_DOOR))
                    return InteractResult.Locked;
                // Key used — mark objective complete
                game.Map.ObjectivesComplete[MapFlags.HAS_LOCKED_DOOR] = true;
            }
            IsOpening = true;
            return InteractResult.Opened;
        }
    }
}