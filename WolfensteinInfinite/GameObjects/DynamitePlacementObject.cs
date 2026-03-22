using SFML.Graphics;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // DynamitePlacementObject — interactable spot, converts to placed on use
    // -------------------------------------------------------------------------
    public class DynamitePlacementObject : DynamicObject, IInteractable
    {
        public bool IsPlaced { get; private set; } = false;

        private readonly ISprite _placedSprite;

        public DynamitePlacementObject(float x, float y, ISprite unplacedSprite, ISprite placedSprite)
            : base(x + 0.5f, y + 0.5f, DynamicObjectType.PickupItem, unplacedSprite)
        {
            X = x;
            Y = y;
            _placedSprite = placedSprite;
        }

        public bool CanInteract(InGameState state) =>
            !IsPlaced && state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM);

        public InteractResult Interact(InGameState state)
        {
            if (!CanInteract(state)) return InteractResult.None;
            IsPlaced = true;
            Sprite = _placedSprite;
            return InteractResult.None;
        }

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);

            if (IsPlaced)
            {
                // Check if all placement spots are filled
                var allPlaced = state.DynamicObjects
                    .OfType<DynamitePlacementObject>()
                    .All(d => d.IsPlaced);

                if (allPlaced && state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM))
                {
                    if (!state.Game.Map.ObjectivesComplete.TryAdd(MapFlags.HAS_EXPLOSIVE_SET, true))
                    {
                        state.Game.Map.ObjectivesComplete[MapFlags.HAS_EXPLOSIVE_SET] = true;
                    }

                    state.StartDynamiteCountdown();
                }
            }

        }
    }
}