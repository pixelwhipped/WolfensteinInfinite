//Clean
namespace WolfensteinInfinite.DataFormats
{
    public struct DynamicMapObject
    {
        public MapObjectTypes Type;
        public int SpawnX;
        public int SpawnY;
        public int X;
        public int Y;

        public int KeyNumber;
        public bool Activated;
        public MapDirection Direction;
        public int Progress;

        public DynamicMapObject()
        {
            Type = MapObjectTypes.MAPOBJECT_NONE;
            SpawnX = 0;
            SpawnY = 0;
            X = 0;
            Y = 0;
            KeyNumber = 0;
            Activated = false;
            Direction = MapDirection.DIR_NONE;
            Progress = 0;
        }
    }
}