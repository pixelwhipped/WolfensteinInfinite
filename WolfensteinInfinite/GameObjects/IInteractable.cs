namespace WolfensteinInfinite.GameObjects
{

    public interface IInteractable
    {
        int X { get; }
        int Y { get; }
        bool CanInteract(Game game);
        InteractResult Interact(Game game);
    }
}