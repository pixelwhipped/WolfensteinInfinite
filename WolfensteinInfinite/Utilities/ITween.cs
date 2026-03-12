namespace WolfensteinInfinite.Utilities
{
    public interface ITween
    {
        public float Seconds { get; init; }
        public Action<ITween>? OnFinish { get; init; }
        public float Value { get; }
        public void End();
        public void Reset();
        public void Update(float frameTimeSeconds);
    }
}
