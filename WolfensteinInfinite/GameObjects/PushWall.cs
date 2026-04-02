using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public sealed class PushWall : IInteractable
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Direction Direction { get; set; }
        public int TextureIndex { get; set; }
        public bool IsMoving { get; private set; } = false;
        public bool IsComplete { get; private set; } = false;
        public float MoveOffset { get; private set; } = 0f;
        public const float MoveSpeed = 1.2f;
        // How many whole tiles this wall has moved from its starting position
        public int TilesMoved { get; private set; } = 0;
        // The visual world position during sliding — tile-aligned corner
        public float RenderX { get; set; } = 0f;
        public float RenderY { get; set; } = 0f;
        private float DirectionX => Direction switch
        {
            Direction.EAST => 1f,
            Direction.WEST => -1f,
            _ => 0f
        };
        private float DirectionY => Direction switch
        {
            Direction.SOUTH => 1f,
            Direction.NORTH => -1f,
            _ => 0f
        };
        public void InitRenderPos() { RenderX = X; RenderY = Y; }
        public bool CanInteract(InGameState state) => !IsMoving && !IsComplete;

        public InteractResult Interact(InGameState state)
        {
            if (IsMoving || IsComplete) return InteractResult.None;
            TilesMoved = 0;
            MoveOffset = 0f;
            RenderX = X;
            RenderY = Y;
            IsMoving = true;
            return InteractResult.None;
        }


        public void Update(float frameTime, Map map)
        {
            if (!IsMoving || IsComplete) return;

            MoveOffset += MoveSpeed * frameTime;

            // Advance whole-tile steps first so we never "overshoot then snap"
            while (MoveOffset >= 1.0f)
            {
                MoveOffset -= 1.0f;

                // Calculate where the wall wants to move next
                var (nextX, nextY) = Direction switch
                {
                    Direction.NORTH => (X, Y - 1),
                    Direction.SOUTH => (X, Y + 1),
                    Direction.EAST => (X + 1, Y),
                    Direction.WEST => (X - 1, Y),
                    _ => (X, Y)
                };

                // Stop if going out of bounds or if next tile is a wall
                bool blocked =
                    nextY < 0 || nextY >= map.WorldMap.Length ||
                    nextX < 0 || nextX >= map.WorldMap[0].Length ||
                    map.WorldMap[(int)nextY][(int)nextX] >= 0; // >= 0 means wall

                if (blocked)
                {
                    // Stop exactly at the current tile boundary (no residual fractional offset)
                    MoveOffset = 0f;
                    RenderX = X;
                    RenderY = Y;
                    IsMoving = false;
                    IsComplete = true;
                    return;
                }

                // Clear the tile the wall just left only when we actually move
                map.WorldMap[(int)Y][(int)X] = MapSection.ClosedSectionInterior;

                X = nextX;
                Y = nextY;
                TilesMoved++;

                // If the tile in front is blocked, stop immediately on this tile.
                // This prevents sliding "through" the last open tile and snapping back.
                var (peekX, peekY) = Direction switch
                {
                    Direction.NORTH => (X, Y - 1),
                    Direction.SOUTH => (X, Y + 1),
                    Direction.EAST => (X + 1, Y),
                    Direction.WEST => (X - 1, Y),
                    _ => (X, Y)
                };

                bool frontBlocked =
                    peekY < 0 || peekY >= map.WorldMap.Length ||
                    peekX < 0 || peekX >= map.WorldMap[0].Length ||
                    map.WorldMap[(int)peekY][(int)peekX] >= 0;

                if (frontBlocked)
                {
                    MoveOffset = 0f;
                    RenderX = X;
                    RenderY = Y;
                    IsMoving = false;
                    IsComplete = true;                    
                    return;
                }
            }

            // Smooth sub-tile movement (RenderX/RenderY are the tile corner)
            RenderX = X + DirectionX * MoveOffset;
            RenderY = Y + DirectionY * MoveOffset;
        }
    }
}