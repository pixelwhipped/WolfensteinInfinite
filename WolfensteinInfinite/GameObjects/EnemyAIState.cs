namespace WolfensteinInfinite.GameObjects
{
    public enum EnemyAIState
    {
        Idle,       // Standing, not yet aware of player
        Alert,      // Just spotted player, brief pause before chasing
        Chase,      // Moving toward player
        Attack,     // In range, attacking
        Flee,       // Low health, retreating from player
        Dead        // Death animation playing
    }
}
