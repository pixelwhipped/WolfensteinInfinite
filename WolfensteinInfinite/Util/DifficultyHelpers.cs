using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.Util
{
    public static class DifficultyHelpers
    {
        public static string GetDifficultyString(Difficulties difficulty) => difficulty switch
        {
            Difficulties.CAN_I_PLAY_DADDY => "Can I play, Daddy?",
            Difficulties.DONT_HURT_ME => "Don't hurt me.",
            Difficulties.BRING_EM_ON => "Bring 'em on!",
            Difficulties.I_AM_DEATH_INCARNATE => "I am Death incarnate!",
            _ => string.Empty,
        };
        public static Difficulties? GetStringDifficulty(string difficulty, StringComparison comparison = StringComparison.CurrentCulture)
        {
            foreach (var d in Enum.GetValues<Difficulties>())
            {
                if (GetDifficultyString(d).Equals(difficulty, comparison)) return d;
            }
            return null;
        }
        public static string GetDifficultyIconPath(Difficulties difficulty) => difficulty switch
        {
            Difficulties.CAN_I_PLAY_DADDY => $"GameData\\Base\\Pictures\\Diff0.png",
            Difficulties.DONT_HURT_ME => $"GameData\\Base\\Pictures\\Diff1.png",
            Difficulties.BRING_EM_ON => $"GameData\\Base\\Pictures\\Diff2.png",
            Difficulties.I_AM_DEATH_INCARNATE => $"GameData\\Base\\Pictures\\Diff3.png",
            _ => string.Empty,
        };
        public static Dictionary<Difficulties, Texture32> DifficultyIcons =
            new()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = FileHelpers.Shared.LoadSurface32(GetDifficultyIconPath(Difficulties.CAN_I_PLAY_DADDY)),
                [Difficulties.DONT_HURT_ME] = FileHelpers.Shared.LoadSurface32(GetDifficultyIconPath(Difficulties.DONT_HURT_ME)),
                [Difficulties.BRING_EM_ON] = FileHelpers.Shared.LoadSurface32(GetDifficultyIconPath(Difficulties.BRING_EM_ON)),
                [Difficulties.I_AM_DEATH_INCARNATE] = FileHelpers.Shared.LoadSurface32(GetDifficultyIconPath(Difficulties.I_AM_DEATH_INCARNATE))
            };

        public static Texture32 GetDifficultyIcon(Difficulties difficulty)
        {
            if (!DifficultyIcons.TryGetValue(difficulty, out Texture32? texture))
                DifficultyIcons[difficulty] = texture ?? new Texture32(24, 32);
            return texture ?? new Texture32(24, 32);
        }
    }

}
