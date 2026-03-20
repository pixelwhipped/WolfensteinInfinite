using WolfensteinInfinite.Engine.Audio;

namespace WolfensteinInfinite.GameObjects
{
    public class ExitWall : IInteractable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsActivated { get; private set; } = false;

        public bool CanInteract(Game game) => !IsActivated;

        public InteractResult Interact(Game game,Wolfenstein wolfenstein)
        {
            foreach (var kvp in game.Map.Objectives)
                if (kvp.Value && !game.Map.ObjectivesComplete.GetValueOrDefault(kvp.Key))
                    return InteractResult.Locked;
            AudioPlaybackEngine.Instance.PlaySound(wolfenstein.GameResources.Effects["Door"]);
            IsActivated = true;
            return InteractResult.Activated;
        }
    }
}