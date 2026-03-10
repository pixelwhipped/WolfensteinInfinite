using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class ExitWall : IInteractable
    {
        public int X { get; set; }
        public int Y { get; set; }

        public bool CanInteract(Game game) => true;
        public InteractResult Interact(Game game)
        {
            // All required objectives must be complete
            foreach (var kvp in game.Map.Objectives)
                if (kvp.Value && !game.Map.ObjectivesComplete.GetValueOrDefault(kvp.Key))
                    return InteractResult.Locked;
            return InteractResult.Exited;
        }
    }

    public class Decal
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int TextureIndex { get; set; }
        public bool LightSource { get; set; } = false;
        public bool Passable { get; set; } = true;
        public Direction Direction { get; set; } = Direction.NONE;
    }

    public class Item
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int ItemType { get; set; }
        public int TextureIndex { get; set; }
    }
}