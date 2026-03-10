namespace WolfensteinInfinite.GameObjects
{
    public class ExitWall : IInteractable
    {
        public int X { get; set; }
        public int Y { get; set; }
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