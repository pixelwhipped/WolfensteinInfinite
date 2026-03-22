using WolfensteinInfinite.States;

namespace WolfensteinInfinite.GameObjects
{

    public interface IInteractable
    {
        float X { get; }
        float Y { get; }
        bool CanInteract(InGameState state);
        InteractResult Interact(InGameState state);
    }
}