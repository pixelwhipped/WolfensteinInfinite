namespace WolfensteinInfinite.GameObjects
{
    public class ExitWall : IInteractable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsActivated { get; private set; } = false;

        public bool CanInteract(Game game) => !IsActivated;

        public InteractResult Interact(Game game)
        {
            foreach (var kvp in game.Map.Objectives)
                if (kvp.Value && !game.Map.ObjectivesComplete.GetValueOrDefault(kvp.Key))
                    return InteractResult.Locked;

            IsActivated = true;
            return InteractResult.Activated;
        }
    }
}