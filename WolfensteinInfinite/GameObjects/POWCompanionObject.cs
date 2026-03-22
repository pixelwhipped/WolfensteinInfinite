using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // POWCompanionObject — follows player, completes objective at exit
    // -------------------------------------------------------------------------
    public class POWCompanionObject(float x, float y, ISprite sprite) : DynamicObject(x, y, DynamicObjectType.Enemy, sprite)
    {

        private const float FollowRange = 10f;
        private const float MoveSpeed = 3.5f;
        private const int MaxDoorsAllowed = 1;
        private float _doorOpenCooldown = 0f;

        private int CountDoorsBetween(InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - base.X;
            var dy = py - base.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return 0;

            var steps = (int)(dist * 2);
            var stepX = dx / steps;
            var stepY = dy / steps;
            var rx = base.X;
            var ry = base.Y;
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

            int cx = (int)base.X;
            int cy = (int)base.Y;
            int[] dx = [-1, 1, 0, 0];
            int[] dy = [0, 0, -1, 1];

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];
                var door = state.Game.Map.Doors
                    .FirstOrDefault(d => d.X == nx && d.Y == ny);
                if (door == null) continue;
                if (door.IsFake) continue;
                // POW cannot open prison doors (TextureIndex == 3) or locked without key
                if (door.TextureIndex == 3) continue;
                if (door.IsLocked &&
                    !state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_LOCKED_DOOR))
                    continue;

                door.IsOpening = true;
                _doorOpenCooldown = 1f;
                break;
            }
        }

        private void TryMove(float frameTime, InGameState state)
        {
            var px = state.Game.Player.PosX;
            var py = state.Game.Player.PosY;
            var dx = px - base.X;
            var dy = py - base.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 1.0f) return; // close enough

            var nx = dx / dist;
            var ny = dy / dist;
            var speed = MoveSpeed * frameTime;

            var newX = base.X + nx * speed;
            var newY = base.Y + ny * speed;
            var mapX = (int)newX;
            var mapY = (int)newY;
            var curX = (int)base.X;
            var curY = (int)base.Y;

            if (mapY >= 0 && mapY < state.Game.Map.WorldMap.Length &&
                mapX >= 0 && mapX < state.Game.Map.WorldMap[0].Length)
            {
                // Pushwalls always block
                bool pushwallBlocksX = state.Game.Map.PushWalls.Any(w => (int)w.X == mapX && (int)w.Y == curY);
                bool pushwallBlocksY = state.Game.Map.PushWalls.Any(w => (int)w.X == curX && (int)w.Y == mapY);

                var tileX = state.Game.Map.WorldMap[curY][mapX];
                var tileY = state.Game.Map.WorldMap[mapY][curX];

                if ((tileX == MapSection.ClosedSectionInterior && !pushwallBlocksX ||
                    tileX == InGameState.DOOR_TILE) && !pushwallBlocksX)
                    base.X = newX;

                if ((tileY == MapSection.ClosedSectionInterior && !pushwallBlocksY ||
                    tileY == InGameState.DOOR_TILE) && !pushwallBlocksY)
                    base.Y = newY;
            }

            X = (int)base.X;
            Y = (int)base.Y;
        }

        private void CheckAtExit(InGameState state)
        {
            var atExit = state.Game.Map.Exits
                .Any(e => e.X == X && e.Y == Y);
            var playerAtExit = state.Game.Map.Exits
                .Any(e => e.X == (int)state.Game.Player.PosX &&
                          e.Y == (int)state.Game.Player.PosY);

            if (atExit && playerAtExit)
                state.Game.Map.ObjectivesComplete[MapFlags.HAS_POW] = true;
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);

            var dx = state.Game.Player.PosX - base.X;
            var dy = state.Game.Player.PosY - base.Y;
            var dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist <= FollowRange && CountDoorsBetween(state) <= MaxDoorsAllowed)
            {
                TryOpenAdjacentDoor(frameTime, state);
                TryMove(frameTime, state);
            }

            CheckAtExit(state);
        }
    }
}