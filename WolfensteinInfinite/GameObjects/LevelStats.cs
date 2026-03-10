namespace WolfensteinInfinite.GameObjects
{
    public record LevelStats(
        int EnemiesKilled, int EnemiesTotal,
        int ItemsCollected, int ItemsTotal,
        int SecretsFound, int SecretsTotal);
}