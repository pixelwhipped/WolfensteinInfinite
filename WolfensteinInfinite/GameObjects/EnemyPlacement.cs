using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;

namespace WolfensteinInfinite.GameObjects
{
    public class EnemyPlacement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int EnemyMapId { get; set; } // -1 for random, -2 for experimental
        public string Mod { get; set; } = string.Empty;
        public bool IsRandom => EnemyMapId == -1;
        public bool IsExperimental => EnemyMapId == -2;
        // Stored generated experimental data — not in mod so must travel with placement
        public Enemy? ExperimentalEnemy { get; set; }
        public CharacterSprite? ExperimentalSprite { get; set; }
    }
}
