namespace WolfensteinInfinite.Utilities
{
    public class Tween(float seconds, Action<ITween>? onFinish) : ITween
    {
        public float Seconds { get; init; } = seconds;
        public Action<ITween>? OnFinish { get; init; } = onFinish;
        public float Value => CurrentFrameTime / Seconds;
        private bool OnFinishCalled = false;
        private float CurrentFrameTime = 0f;
        public void End() => CurrentFrameTime = Seconds;
        public void Reset()
        {
            CurrentFrameTime = 0;
            OnFinishCalled = false;
        }
        public void Update(float frameTimeSeconds)
        {
            CurrentFrameTime = Math.Clamp(CurrentFrameTime + frameTimeSeconds, 0, Seconds);
            if (!OnFinishCalled && CurrentFrameTime == Seconds)
            {
                OnFinishCalled = true;
                OnFinish?.Invoke(this);
            }
        }
    }
}
