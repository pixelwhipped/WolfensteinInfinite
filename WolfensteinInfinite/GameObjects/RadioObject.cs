using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // RadioObject — stays on map, becomes interactable when player has secret
    // -------------------------------------------------------------------------
    public class RadioObject(int x, int y, ISprite sprite) : DynamicObject(x + 0.5f, y + 0.5f, DynamicObjectType.PickupItem, sprite), IInteractable
    {

        public bool CanInteract(Game game) =>
            game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE) &&
            !game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE);

        public InteractResult Interact(Game game)
        {
            if (!CanInteract(game)) return InteractResult.None;
            game.Map.ObjectivesComplete[MapFlags.HAS_SECRET_MESSAGE] = true;
            return InteractResult.None;
        }

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);
        }
    }
}