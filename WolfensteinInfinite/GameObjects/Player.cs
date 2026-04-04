using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite.GameObjects
{
    public sealed class Player(string name)
    {
        public string Name { get; init; } = name;
        public int Health { get; set; } = 100;
        public int Score { get; set; } = 0;
        public int Lives { get; set; } = 3;
        public Dictionary<AmmoType, int> Ammo { get; set; } = new Dictionary<AmmoType, int>
        {
            { AmmoType.BULLET, 16 } //two clips
        };
        public string Weapon { get; set; } = "Pistol";
        public List<string> Weapons { get; set; } = ["Knife", "Pistol"];
        public bool GodMode { get; set; } = false;
        public bool HasBackpack { get; set; } = false;
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float DirX { get; set; }
        public float DirY { get; set; }
        public float PlaneX { get; set; }
        public float PlaneY { get; set; }
        
    }
}

