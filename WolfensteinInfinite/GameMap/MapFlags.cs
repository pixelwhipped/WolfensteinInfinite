namespace WolfensteinInfinite.GameMap
{    
    public enum MapFlags
    {
        HAS_POW,    //Requires Item Pow(15)
        HAS_LOCKED_DOOR,    //Requires Item Key(21) and Door(2)
        HAS_BOSS,   //Requires and Experimental or Any Enemy Type 5 to 12
        HAS_SECRET_MESSAGE, //Requires Item Secret(16) and Radio(17)
        HAS_BOOM    //Requres Item Dynamite(18) and DynamiteToPlace(19)
    }    
}
