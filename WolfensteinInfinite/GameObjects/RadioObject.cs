using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // RadioObject — stays on map, becomes interactable when player has secret
    // -------------------------------------------------------------------------
    public class RadioObject(float x, float y, ISprite sprite) : DynamicObject(x, y, DynamicObjectType.PickupItem, sprite), IInteractable
    {

        public bool CanInteract(InGameState state) =>
            state.Game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE) &&
            state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE);

        public InteractResult Interact(InGameState state)
        {
            if (!CanInteract(state)) return InteractResult.None;
            state.Game.Map.ObjectivesComplete[MapFlags.HAS_SECRET_MESSAGE] = true;
            if (!state.Game.Map.ObjectivesComplete.TryAdd(MapFlags.HAS_SENT_SECRET, true))
            {
                state.Game.Map.ObjectivesComplete[MapFlags.HAS_SENT_SECRET] = true;
            }
            state.ShowHudMessage("CHARLIE BRAVO ECHO");
            return InteractResult.None;
        }

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);
        }
    }
}