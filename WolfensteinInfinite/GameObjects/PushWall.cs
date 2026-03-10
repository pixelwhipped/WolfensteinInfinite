using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class PushWall : IInteractable
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Direction Direction { get; set; }
        public int TextureIndex { get; set; }
        public bool IsMoving { get; private set; } = false;
        public bool IsComplete { get; private set; } = false;
        public float MoveOffset { get; private set; } = 0f;
        public const float MoveSpeed = 1.2f;

        public bool CanInteract(Game game) => !IsMoving && !IsComplete;

        public InteractResult Interact(Game game)
        {
            if (IsMoving || IsComplete) return InteractResult.None;
            IsMoving = true;
            return InteractResult.None;
        }

        public void Update(float frameTime, Map map)
        {
            if (!IsMoving || IsComplete) return;

            MoveOffset += MoveSpeed * frameTime;

            if (MoveOffset < 1.0f) return;

            MoveOffset -= 1.0f;

            // Clear the tile the wall just left
            map.WorldMap[Y][X] = MapSection.ClosedSectionInterior;

            // Calculate where the wall wants to move next
            var (nextX, nextY) = Direction switch
            {
                Direction.NORTH => (X, Y - 1),
                Direction.SOUTH => (X, Y + 1),
                Direction.EAST => (X + 1, Y),
                Direction.WEST => (X - 1, Y),
                _ => (X, Y)
            };

            // Check bounds and whether next tile is blocked
            bool blocked =
                nextY < 0 || nextY >= map.WorldMap.Length ||
                nextX < 0 || nextX >= map.WorldMap[0].Length ||
                map.WorldMap[nextY][nextX] >= 0; // >= 0 means wall

            if (blocked)
            {
                // Stop — wall stays at its current tile permanently
                map.WorldMap[Y][X] = TextureIndex;
                IsMoving = false;
                IsComplete = true;
            }
            else
            {
                // Advance — occupy the next tile
                X = nextX;
                Y = nextY;
                map.WorldMap[Y][X] = TextureIndex;
            }
        }
    }
}