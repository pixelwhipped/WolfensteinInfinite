using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.States;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // RadioObject — stays on map, becomes interactable when player has secret
    // -------------------------------------------------------------------------
    public class RadioObject : DynamicObject, IInteractable
    {

        // Float position for rendering
        private readonly float _renderX;
        private readonly float _renderY;

        public RadioObject(int x, int y, ISprite sprite)
            : base(x + 0.5f, y + 0.5f, DynamicObjectType.PickupItem, sprite)
        {
            X = x;
            Y = y;
            _renderX = x + 0.5f;
            _renderY = y + 0.5f;
        }

        public bool CanInteract(Game game) =>
            game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE) &&
            !game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_SECRET_MESSAGE);

        public InteractResult Interact(Game game)
        {
            if (!CanInteract(game)) return InteractResult.None;
            game.Map.ObjectivesComplete[MapFlags.HAS_SECRET_MESSAGE] = true;
            return InteractResult.None;
        }

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);
        }
    }

    // -------------------------------------------------------------------------
    // DynamitePlacementObject — interactable spot, converts to placed on use
    // -------------------------------------------------------------------------
    public class DynamitePlacementObject : DynamicObject, IInteractable
    {
        public bool IsPlaced { get; private set; } = false;

        private readonly ISprite _unplacedSprite;
        private readonly ISprite _placedSprite;

        public DynamitePlacementObject(int x, int y, ISprite unplacedSprite, ISprite placedSprite)
            : base(x + 0.5f, y + 0.5f, DynamicObjectType.PickupItem, unplacedSprite)
        {
            X = x;
            Y = y;
            _unplacedSprite = unplacedSprite;
            _placedSprite = placedSprite;
        }

        public bool CanInteract(Game game) =>
            !IsPlaced &&
            game.Map.Objectives.GetValueOrDefault(MapFlags.HAS_BOOM) &&
            !game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM);

        public InteractResult Interact(Game game)
        {
            if (!CanInteract(game)) return InteractResult.None;
            IsPlaced = true;
            return InteractResult.None;
        }

        public override void Update(float frameTime, InGameState state)
        {
            Sprite.Update(frameTime);

            if (IsPlaced)
            {
                // Check if all placement spots are filled
                var allPlaced = state.DynamicObjects
                    .OfType<DynamitePlacementObject>()
                    .All(d => d.IsPlaced);

                if (allPlaced &&
                    !state.Game.Map.ObjectivesComplete.GetValueOrDefault(MapFlags.HAS_BOOM))
                {
                    state.StartDynamiteCountdown();
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // POWCompanionObject — follows player, completes objective at exit
    // -------------------------------------------------------------------------
    public class POWCompanionObject : DynamicObject
    {

        private const float FollowRange = 10f;
        private const float MoveSpeed = 3.5f;
        private const int MaxDoorsAllowed = 1;
        private float _doorOpenCooldown = 0f;

        public POWCompanionObject(float x, float y, ISprite sprite)
            : base(x, y, DynamicObjectType.Enemy, sprite)
        {
            X = (int)x;
            Y = (int)y;
        }

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
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

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
                var tileX = state.Game.Map.WorldMap[curY][mapX];
                var tileY = state.Game.Map.WorldMap[mapY][curX];

                if (tileX == MapSection.ClosedSectionInterior ||
                    tileX == InGameState.DOOR_TILE)
                    base.X = newX;

                if (tileY == MapSection.ClosedSectionInterior ||
                    tileY == InGameState.DOOR_TILE)
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