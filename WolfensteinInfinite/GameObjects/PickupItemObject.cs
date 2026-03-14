using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.States;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.GameObjects
{
    // -------------------------------------------------------------------------
    // PickupItemObject
    // -------------------------------------------------------------------------
    public class PickupItemObject : DynamicObject
    {
        public PickupItem Item { get; }
        private readonly LinearPointTween? _tween;

        // Spawned normally — no toss
        public PickupItemObject(float x, float y, ISprite sprite, PickupItem item)
            : base(x, y, DynamicObjectType.PickupItem, sprite)
        {
            Item = item;
        }

        // Dropped from enemy — toss arc
        public PickupItemObject(float x, float y, ISprite sprite, PickupItem item, bool dropped)
            : base(x, y, DynamicObjectType.PickupItem, sprite)
        {
            Item = item;
            if (dropped)
            {
                _tween = new LinearPointTween(
                    seconds: 2f,
                    onFinish: null,
                    points: [-0.45f, -0.6f, 0f]  // up then back down (negative = above center)
                );
            }
        }

        public override void Update(float frameTime, InGameState state)
        {
            if (!IsAlive) return;
            Sprite.Update(frameTime);

            if (_tween != null)
            {
                _tween.Update(frameTime);
                YOffset = _tween.Value;
            }

            if ((int)state.Game.Player.PosX == (int)X &&
                (int)state.Game.Player.PosY == (int)Y)
            {
                if (state.TryPickupItem(Item))
                {
                    state.OnItemCollected();
                    IsAlive = false;
                }
            }
        }
    }
}