using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.GameBible
{
    public class Enemy(int id, string name, EnemyType enemyType, int speed, int points, Dictionary<Difficulties, int> hitPoints, string[] weapons, Dictionary<string, int> dropItemProbability, CharacterSpriteType animationType, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds, int[] fireFrames , float reactionDelay, float alertPauseDuration, float meleeAttackRange, float alertRadius, float lineOfSightDistance, bool canFlee, float fleeDuration, float fleeHealthThreshold, RGBA8 bloodColor)
    {
        public int MapID { get; init; } = id;
        public CharacterSpriteType AnimationType { get; init; } = animationType;
        public string SpritePath { get; init; } = spritePath;
        public int StartSprite { get; init; } = startSprite;
        public EnemyType EnemyType { get; init; } = enemyType;
        public string Name { get; init; } = name;
        public Dictionary<Difficulties, int> HitPoints { get; init; } = hitPoints;
        public Dictionary<string, int> DropItemProbability { get; init; } = dropItemProbability; //Name of pickupitem
        public int Points { get; init; } = points;
        public int Speed { get; init; } = speed;
        public string[] Weapons { get; init; } = weapons;
        public string[] AlertSounds { get; init; } = alertSounds;
        public string[] DeathSounds { get; init; } = deathSounds;
        public string[] TauntSounds { get; init; } = tauntSounds;
        public int[] FireFrames { get; init; } = fireFrames;

        // How long the enemy pauses the first time it spots the player before firing.        
        public float ReactionDelay { get; init; } = reactionDelay; //1f

        public float AlertPauseDuration { get; init; } = alertPauseDuration; // 0.5f;

        public float MeleeAttackRange { get; init; } = meleeAttackRange; // 1.5f;
        public float AlertRadius { get; init; } = alertRadius; // 5f;
        public float LineOfSightDistance { get; init; } = lineOfSightDistance; // 12f;
        public bool CanFlee { get; init; } = canFlee;
        public float FleeDuration { get; init; } = fleeDuration;// 1.5f;
        public float FleeHealthThreshold { get; init; } = fleeHealthThreshold; //0.25f;
        public RGBA8 BloodColor { get; init; } = bloodColor;
    }
}
