namespace WolfensteinInfinite.GameObjects
{
    public enum EnemyAIState
    {
        Idle,       // Standing, not yet aware of player
        Alert,      // Just spotted player, brief pause before chasing
        Chase,      // Moving toward player
        Attack,     // In range, attacking
        Dead        // Death animation playing
    }
}
