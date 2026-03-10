namespace WolfensteinInfinite.GameBible
{
    public class Enemy(int id, string name, EnemyType enemyType, int speed, int points, Dictionary<Difficulties, int> hitPoints, string[] weapons, Dictionary<string, int> dropItemProbability, CharacterSpriteType animationType, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
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

    }
}
