//Clean
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.GameHelpers
{
    
    public static class EnemyHelpers
    {
        public static RGBA8 RedBlood => new() { R = 176, G = 0, B = 0, A = 255 };
        public static bool IsBoss(this EnemyType enemyType) => enemyType switch
        {
            EnemyType.HANS_GROSS or EnemyType.DOCTOR_SCHABBS or EnemyType.MECHA_HITLER or EnemyType.ADOLF_HITLER or EnemyType.OTTO_GIFTMACHER or EnemyType.GRETEL_GROSSE or EnemyType.GENERAL_FETTGESICHT or EnemyType.HITLER_GHOST => true,
            _ => false,
        };

        public static Enemy CreateGuard(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Guard", EnemyType.GUARD, 512 * 3, 100,
                new Dictionary<Difficulties, int>()
                {
                    [Difficulties.CAN_I_PLAY_DADDY] = 25,
                    [Difficulties.DONT_HURT_ME] = 25,
                    [Difficulties.BRING_EM_ON] = 25,
                    [Difficulties.I_AM_DEATH_INCARNATE] = 25
                },
                ["Pistol"],
                new Dictionary<string, int>()
                {
                    ["UsedClip"] = 100
                },
                CharacterSpriteType.GUARD, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood, 0);
        }
        public static Enemy CreateDog(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Dog", EnemyType.DOG, 1500 * 2, 200,
             new Dictionary<Difficulties, int>()
             {
                 [Difficulties.CAN_I_PLAY_DADDY] = 1,
                 [Difficulties.DONT_HURT_ME] = 1,
                 [Difficulties.BRING_EM_ON] = 1,
                 [Difficulties.I_AM_DEATH_INCARNATE] = 1
             },
             ["Bite"], [], CharacterSpriteType.DOG, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [1], 1f, 0.5f, 1.8f, 5f, 14f, true, 1.5f, 0.25f, RedBlood, 0);
        }
        public static Enemy CreateSSSoldier(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "SS Soldier", EnemyType.SS_SOLDIER, 512 * 4, 500,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 50,
                [Difficulties.DONT_HURT_ME] = 50,
                [Difficulties.BRING_EM_ON] = 50,
                [Difficulties.I_AM_DEATH_INCARNATE] = 50
            }, ["MachineGun"],
            new Dictionary<string, int>()
            {
                ["UsedClip"] = 50,
                ["MachineGun"] = 50
            }, CharacterSpriteType.GUARD, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, true, 1.5f, 0.25f, RedBlood, 2);
        }
        public static Enemy CreateMutant(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Mutant", EnemyType.MUTANT, 512 * 3, 700,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 45,
                [Difficulties.DONT_HURT_ME] = 55,
                [Difficulties.BRING_EM_ON] = 55,
                [Difficulties.I_AM_DEATH_INCARNATE] = 65
            }, ["Pistol"],
            new Dictionary<string, int>()
            {
                ["UsedClip"] = 100
            }, CharacterSpriteType.MUTANT, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [1,3], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, new RGBA8() { R = 108, G = 0, B = 112, A = 255 }, 11);
        }
        public static Enemy CreateOfficer(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Officer", EnemyType.OFFICER, 512 * 5, 400,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 100,
                [Difficulties.DONT_HURT_ME] = 100,
                [Difficulties.BRING_EM_ON] = 100,
                [Difficulties.I_AM_DEATH_INCARNATE] = 100
            }, ["Pistol"],
            new Dictionary<string, int>()
            {
                ["UsedClip"] = 100
            }, CharacterSpriteType.OFFICER, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2] , 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,5);
        }
        public static Enemy CreateHansGross(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Hans Gross", EnemyType.HANS_GROSS, 512 * 3, 5000,
                new Dictionary<Difficulties, int>()
                {
                    [Difficulties.CAN_I_PLAY_DADDY] = 850,
                    [Difficulties.DONT_HURT_ME] = 950,
                    [Difficulties.BRING_EM_ON] = 1050,
                    [Difficulties.I_AM_DEATH_INCARNATE] = 1200
                }, ["ChainGun", "ChainGun"],
                []  //Drop was gold key but irrelevent now
                , CharacterSpriteType.BOSS, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,10);
        }
        public static Enemy CreateDoctorSchabbs(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Doctor Schabbs", EnemyType.DOCTOR_SCHABBS, 512 * 3, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 850,
                [Difficulties.DONT_HURT_ME] = 950,
                [Difficulties.BRING_EM_ON] = 1550,
                [Difficulties.I_AM_DEATH_INCARNATE] = 2400
            }, ["KorpsokineticSerum"], []
            , CharacterSpriteType.DOCTOR_SCHABBS, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [1], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood, 21);
        }
        public static Enemy CreateMechaHitler(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Mecha Hitler", EnemyType.MECHA_HITLER, 512 * 3, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 850,
                [Difficulties.DONT_HURT_ME] = 950,
                [Difficulties.BRING_EM_ON] = 1050,
                [Difficulties.I_AM_DEATH_INCARNATE] = 1200
            }, ["ChainGun", "ChainGun", "ChainGun", "ChainGun"],
            new Dictionary<string, int>()
            {
                ["AdolfHitler"] = 100
            }, CharacterSpriteType.MECHA_HITLER, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, new RGBA8() { R=0,G=0,B=0,A=0},39);
        }
        public static Enemy CreateAdolfHitler(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Adolf Hitler", EnemyType.MECHA_HITLER, 512 * 5, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 500,
                [Difficulties.DONT_HURT_ME] = 700,
                [Difficulties.BRING_EM_ON] = 800,
                [Difficulties.I_AM_DEATH_INCARNATE] = 900
            }, ["ChainGun", "ChainGun"],
            new Dictionary<string, int>() { ["ChainGun"] = 100 }, CharacterSpriteType.ADOLF_HITLER, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,39);
        }
        public static Enemy CreateOttoGiftmacher(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Otto Giftmacher", EnemyType.OTTO_GIFTMACHER, 512 * 3, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 850,
                [Difficulties.DONT_HURT_ME] = 950,
                [Difficulties.BRING_EM_ON] = 1050,
                [Difficulties.I_AM_DEATH_INCARNATE] = 1200
            }, ["RocketLauncher"],
            [], CharacterSpriteType.BOSS, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [1], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,49);
        }
        public static Enemy CreateGretelGross(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Gretel Gross", EnemyType.GRETEL_GROSSE, 512 * 3, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 850,
                [Difficulties.DONT_HURT_ME] = 950,
                [Difficulties.BRING_EM_ON] = 1050,
                [Difficulties.I_AM_DEATH_INCARNATE] = 1200
            }, ["ChainGun", "ChainGun"],
            new Dictionary<string, int>() { ["ChainGun"] = 100 }, CharacterSpriteType.BOSS, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,59);
        }
        public static Enemy CreateGeneralFettgesicht(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "General Fettgesicht", EnemyType.GENERAL_FETTGESICHT, 512 * 3, 5000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 850,
                [Difficulties.DONT_HURT_ME] = 950,
                [Difficulties.BRING_EM_ON] = 1050,
                [Difficulties.I_AM_DEATH_INCARNATE] = 1200
            }, ["RocketLauncher", "ChainGun"],
            new Dictionary<string, int>() { ["ChainGun"] = 100 }, CharacterSpriteType.BOSS, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [2], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, RedBlood,69);
        }
        public static Enemy CreateHitlerGhost(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, "Hitler Ghost", EnemyType.HITLER_GHOST, 512 * 5, 2000,
            new Dictionary<Difficulties, int>()
            {
                [Difficulties.CAN_I_PLAY_DADDY] = 200,
                [Difficulties.DONT_HURT_ME] = 300,
                [Difficulties.BRING_EM_ON] = 400,
                [Difficulties.I_AM_DEATH_INCARNATE] = 500
            }, ["FlameThrower"],
            [], CharacterSpriteType.HITLER_GHOST, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [1], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f,new RGBA8() { R = 0, G = 0, B = 0, A = 0 },40);
        }
        public static Enemy CreatePacManGhost(int id, string name, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        {
            return new(id, name, EnemyType.BLINKY, 512 * 2, 0,
            new Dictionary<Difficulties, int>() //Invincible
            {
                [Difficulties.CAN_I_PLAY_DADDY] = -1,
                [Difficulties.DONT_HURT_ME] = -1,
                [Difficulties.BRING_EM_ON] = -1,
                [Difficulties.I_AM_DEATH_INCARNATE] = -1
            },
            ["DrainLife"], [], CharacterSpriteType.GHOST, spritePath, startSprite, alertSounds, deathSounds, tauntSounds, [0], 1f, 0.5f, 1.5f, 5f, 12f, false, 1.5f, 0.25f, new RGBA8() { R = 0, G = 0, B = 0, A = 0 }, 99999);
        }
        public static Enemy CreateBlinky(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        => CreatePacManGhost(id, "Blinky", spritePath, startSprite, alertSounds, deathSounds, tauntSounds);
        public static Enemy CreatePinky(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        => CreatePacManGhost(id, "Pinky", spritePath, startSprite, alertSounds, deathSounds, tauntSounds);
        public static Enemy CreateInky(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        => CreatePacManGhost(id, "Inky", spritePath, startSprite, alertSounds, deathSounds, tauntSounds);
        public static Enemy CreateClyde(int id, string spritePath, int startSprite, string[] alertSounds, string[] deathSounds, string[] tauntSounds)
        => CreatePacManGhost(id, "Clyde", spritePath, startSprite, alertSounds, deathSounds, tauntSounds);
    }
}
