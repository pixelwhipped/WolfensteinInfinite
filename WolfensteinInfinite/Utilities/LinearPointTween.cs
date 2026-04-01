namespace WolfensteinInfinite.Utilities
{
    public sealed class LinearPointTween(float seconds, Action<ITween>? onFinish, float[] points) : ITween
    {
        public float Seconds { get; init; } = seconds;
        public Action<ITween>? OnFinish { get; init; } = onFinish;

        private bool OnFinishCalled = false;
        private float CurrentFrameTime = 0f;
        private readonly float[] Points = points;
        private readonly int numSegments = points.Length - 1;
        public float Value
        {
            get
            {
                if (Points.Length == 1) return Points[0];
                if (CurrentFrameTime >= Seconds) return Points[numSegments];

                var p = CurrentFrameTime / Seconds;
                var segmentFloat = (Points.Length - 1) * p;
                var segmentIndex = (int)Math.Min(segmentFloat, Points.Length - 2);
                var progressPercent = segmentFloat - segmentIndex; // This gives 0.0 to 1.0 directly

                return Points[segmentIndex] * (1f - progressPercent) + Points[segmentIndex + 1] * progressPercent;
            }
        }

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
