using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    public class POWCompanionObject : DynamicObject
    {
        private const float FollowRange = 10f;
        private const float MoveSpeed = 3.5f;
        private const int MaxDoorsAllowed = 2;
        private float _doorOpenCooldown = 0f;

        private readonly AnimatedSprite _animatedSprite;

        public POWCompanionObject(float x, float y, Animation walkAnimation)
            : base(x, y, DynamicObjectType.Enemy, new AnimatedSprite(walkAnimation))
        {
            _animatedSprite = (AnimatedSprite)Sprite;
            _animatedSprite.IsPlaying = false; // start idle until moving
        }

        // Returns true if the POW actually moved this frame
        private bool TryMove(float frameTime, InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 1.0f) return false; // close enough

            var nx = dx / dist;
            var ny = dy / dist;
            var speed = MoveSpeed * frameTime;

            var newX = X + nx * speed;
            var newY = Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)X;
            var curY = (int)Y;

            bool moved = false;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length)
            {
                bool pushwallBlocksX = state.Game.Map.PushWalls.Any(w => (int)w.X == mapX && (int)w.Y == curY);
                bool pushwallBlocksY = state.Game.Map.PushWalls.Any(w => (int)w.X == curX && (int)w.Y == mapY);

                var tileX = state.Game.Map.WorldMap[curY][mapX];
                var tileY = state.Game.Map.WorldMap[mapY][curX];

                if (!pushwallBlocksX && (tileX == MapSection.ClosedSectionInterior || tileX == InGameState.DOOR_TILE))
                {
                    X = newX;
                    moved = true;
                }

                if (!pushwallBlocksY && (tileY == MapSection.ClosedSectionInterior || tileY == InGameState.DOOR_TILE))
                {
                    Y = newY;
                    moved = true;
                }
            }

            return moved;
        }

        // CountDoorsBetween and TryOpenAdjacentDoor are unchanged from original
        private int CountDoorsBetween(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - X;
            var dy = py - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return 0;

            var steps = (int)(dist * 2);
            var stepX = dx / steps;
            var stepY = dy / steps;
            var rx = X;
            var ry = Y;
            var count = 0;

            for (int i = 0; i < steps; i++)
            {
                rx += stepX;
                ry += stepY;
                var mx = (int)rx;
                var my = (int)ry;
                if (my < 0 || my >= state.Game.Map.WorldMap.Length ||
                    mx < 0 || mx >= state.Game.Map.WorldMap[0].Length) break;
                if (state.Game.Map.WorldMap[my][mx] == InGameState.DOOR_TILE)
                    count++;
            }
            return count;
        }

        private void TryOpenAdjacentDoor(float frameTime, InGameState state)
        {
            _doorOpenCooldown -= frameTime;
            if (_doorOpenCooldown > 0) return;

            int cx = (int)X;
            int cy = (int)Y;
            int[] dx = [-1, 1, 0, 0];
            int[] dy = [0, 0, -1, 1];

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];
                var door = state.Game.Map.Doors.FirstOrDefault(d => d.X == nx && d.Y == ny);
                if (door == null) continue;
                if (door.IsFake) continue;
                if (door.TextureIndex == 3) continue;
                if (door.IsLocked &&
                    !state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_LOCKED_DOOR))
                    continue;

                door.IsOpening = true;
                _doorOpenCooldown = 1f;
                break;
            }
        }

        private void CheckAtExit(InGameState state)
        {
            var atExit = state.Game.Map.Exits.Any(e => e.X == (int)X && e.Y == (int)Y);
            var playerAtExit = state.Game.Map.Exits
                .Any(e => e.X == (int)state.Game.Player.PosX &&
                          e.Y == (int)state.Game.Player.PosY);

            if (atExit && playerAtExit)
                state.Game.Map.ObjectivesComplete[MapFlags.HAS_POW] = true;
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;

            var dx = state.Game.Player.PosX - X;
            var dy = state.Game.Player.PosY - Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);

            bool moved = false;
            if (dist <= FollowRange && CountDoorsBetween(state) <= MaxDoorsAllowed)
            {
                TryOpenAdjacentDoor(frameTime, state);
                moved = TryMove(frameTime, state);
            }

            // Drive animation from actual movement — plays walk cycle while moving,
            // freezes on current frame when standing still
            _animatedSprite.IsPlaying = moved;
            Sprite.Update(frameTime);

            CheckAtExit(state);
        }
    }
}