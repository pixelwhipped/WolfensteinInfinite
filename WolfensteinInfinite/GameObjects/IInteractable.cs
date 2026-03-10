namespace WolfensteinInfinite.GameObjects
{
    public enum InteractResult { None, Opened, Locked, Exited }

    public interface IInteractable
    {
        int X { get; }
        int Y { get; }
        bool CanInteract(Game game);
        InteractResult Interact(Game game);
    }
}