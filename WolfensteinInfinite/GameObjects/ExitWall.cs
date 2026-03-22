using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{
    public class ExitWall : IInteractable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsActivated { get; private set; } = false;

        public bool CanInteract(InGameState state) => !IsActivated;

        public InteractResult Interact(InGameState state)
        {
            if (IsActivated) return InteractResult.Exited;
            foreach (var kvp in state.Game.Map.Objectives.Where(p=>p.Key!= GameMap.MapFlags.HAS_LOCKED_DOOR))//If you got past the locked door thats fine
                if (kvp.Value && !state.Game.Map.ObjectivesComplete.GetValueOrDefault(kvp.Key))
                {
                    state.ShowHudMessage("COMPLETE OBJECTIVES FIRST");
                    return InteractResult.Locked;
                }
            var exitTextureIdx = Array.FindIndex(
                       state.Game.Map.WallSourceIndicies, k => k.Index == 1003);
            if (exitTextureIdx >= 0)
                state.Game.Map.WallTextures[exitTextureIdx] =
                    state.Wolfenstein.GameResources.ElevatorSwitchDown;
            AudioPlaybackEngine.Instance.PlaySound(state.GameResources.Effects["Door"]);
            state.ExitLevel();
            IsActivated = true;
            return InteractResult.Exited;
        }
    }
}