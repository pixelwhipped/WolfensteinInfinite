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

        private readonly ISprite _unplacedSprite;
        private readonly ISprite _placedSprite;

        public DynamitePlacementObject(int x, int y, ISprite unplacedSprite, ISprite placedSprite)
            : base(x + 0.5f, y + 0.5f, DynamicObjectType.PickupItem, unplacedSprite)
        {
            X = x;
            Y = y;
            _unplacedSprite = unplacedSprite;
            _placedSprite = placedSprite;
        }

        public bool CanInteract(Game game) =>
            !IsPlaced &&
            game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_BOOM) &&
            !game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM);

        public InteractResult Interact(Game game, Wolfenstein wolfenstein)
        {
            if (!CanInteract(game)) return InteractResult.None;
            IsPlaced = true;
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

                if (allPlaced &&
                    !state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM))
                {
                    state.StartDynamiteCountdown();
                }
            }
        }
    }
}