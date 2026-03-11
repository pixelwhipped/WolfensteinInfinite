namespace WolfensteinInfinite.GameObjects
{

    public interface IInteractable
    {
        float X { get; }
        float Y { get; }
        bool CanInteract(Game game);
        InteractResult Interact(Game game);
    }
}