namespace WolfensteinInfinite.GameBible
{
    public enum CharacterSpriteType
    {
        //Ghost Blinky, Pinky, Inky, and Clyde
        //  walking 2
        GHOST,
        //Guard/SS
        //  standing 1x8D, walking 4x8D, attack 3, Dying Flippable 4 right + 1 left(3 to flip) +1 right + dead (flip)
        GUARD,
        //Dog
        //  walking 4x8D, attack 3, Dying 3 left + dead 1 left can add flippable
        DOG,
        //Mutant
        //  standing 1x8D waling 4x8D, attack 4, Dying 6 + dead 1
        MUTANT,
        //Officer
        //  standing 1x8D, walking 4x8D, attack 3, Dying Flippable 5 right + 1 left(3 to flip) +1 right + dead
        OFFICER,
        //(Boss) Hans Gross,Gretel Grosse
        //  walking 4, attack 3 dying 3 dead 1  
        BOSS,
        //(Boss) Doctor Schabbs
        //  walking 4, attack 2, dying 3 dead 1, weapon 4
        DOCTOR_SCHABBS,
        //(Boss) Hitler 4 chain gun then 2
        //  walking 4, attack 2, dying 4 -> walking 4, attack 2, dying 7 + dead 1
        MECHA_HITLER,
        ADOLF_HITLER,
        //(Boss) Hitler Ghost
        //  walking 3, attack 2, dying 5, weapon 2
        HITLER_GHOST,
        //Otto Giftmacher
        OTTO_GIFTMACHERE,
        //General Fettgesicht
        GENERAL_FETTGESICHTC
    }
}
