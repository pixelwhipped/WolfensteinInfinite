using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.States
{    
    public class WeaponTransitionState(PlayerWeapon initialWeapon, Action onComplete)
    {
        public PlayerWeapon CurrentWeapon { get; set; } = initialWeapon;
        public PlayerWeapon TransitionWeapon { get; set; } = initialWeapon;
        public int CurrentHeightOffset { get; set; } = 0;
        public bool TransitioningOut { get; set; } = false;
        public bool Transitioning { get; set; } = false;
        public Action OnComplete { get; init; } = onComplete;
        public const float Seconds = 0.25f;
        public float CurrentSeconds = 0f;
        public void TranstionTo(PlayerWeapon weapon)
        {
            if (weapon.Name == CurrentWeapon.Name && !Transitioning) return; //Already current
            if(!TransitioningOut) CurrentSeconds = 0;
            if (weapon.Name == CurrentWeapon.Name && Transitioning) //cancel last transition
            {
                if (TransitioningOut) //Didn't start bringing in new weapon so just reverse
                {
                    TransitioningOut = false;
                    TransitionWeapon = CurrentWeapon;
                }
                else //Reverse the transition assuming we had finished, early exit
                {
                    TransitioningOut = true;
                    (CurrentWeapon, TransitionWeapon) = (TransitionWeapon, CurrentWeapon);
                }
                return;
            }
            Transitioning = true;
            TransitioningOut = true;
            TransitionWeapon = weapon;
        }
        public void Update(float frameTime)
        {
            if (!Transitioning) return;
            
            CurrentSeconds = Math.Clamp(CurrentSeconds + frameTime, 0, Seconds);
            if (TransitioningOut)
            {
                CurrentHeightOffset = (int)((CurrentSeconds / Seconds) * Math.Max( CurrentWeapon.BaseHeight, TransitionWeapon.BaseHeight));
                if (CurrentSeconds == Seconds)
                {
                    TransitioningOut = false;
                    CurrentSeconds = 0;
                }                
            }
            else
            {
                CurrentHeightOffset = (int)((1f - (CurrentSeconds / Seconds)) * Math.Max(CurrentWeapon.BaseHeight, TransitionWeapon.BaseHeight));
                if (CurrentSeconds == Seconds)
                {
                    Transitioning = false;
                    TransitioningOut = true;
                    CurrentWeapon = TransitionWeapon;
                    CurrentHeightOffset = 0;
                    OnComplete?.Invoke();
                }               
            }
        }
    }
}

