namespace WolfensteinInfinite.DataFormats
{
    public class MapData
    {

        private static readonly Dictionary<byte, StaticObjectInteraction> StaticObjectClassification = new()
        {
            { 24, StaticObjectInteraction.OBJ_BLOCKING }, // Green Barrel
            { 25, StaticObjectInteraction.OBJ_BLOCKING }, // Table/chairs
            { 26, StaticObjectInteraction.OBJ_BLOCKING }, // Floor Lamp
            { 28, StaticObjectInteraction.OBJ_BLOCKING }, // Hanged Man
            { 29, StaticObjectInteraction.OBJ_OBTAINABLE }, // Bad Food
            { 30, StaticObjectInteraction.OBJ_BLOCKING }, // Red Pillar
            { 31, StaticObjectInteraction.OBJ_BLOCKING }, // Tree
            { 33, StaticObjectInteraction.OBJ_BLOCKING }, // Sink
            { 34, StaticObjectInteraction.OBJ_BLOCKING }, // Potted Plant
            { 35, StaticObjectInteraction.OBJ_BLOCKING }, // Urn
            { 36, StaticObjectInteraction.OBJ_BLOCKING }, // Bare Table
            { 39, StaticObjectInteraction.OBJ_BLOCKING }, // Suit of armor
            { 38, StaticObjectInteraction.OBJ_BLOCKING }, // Gibs!
            { 40, StaticObjectInteraction.OBJ_BLOCKING }, // Hanging Cage
            { 41, StaticObjectInteraction.OBJ_BLOCKING }, // Skeleton in Cage
            { 43, StaticObjectInteraction.OBJ_OBTAINABLE }, // Gold Key
            { 44, StaticObjectInteraction.OBJ_OBTAINABLE }, // Silver Key
            { 45, StaticObjectInteraction.OBJ_BLOCKING }, // STUFF
            { 47, StaticObjectInteraction.OBJ_OBTAINABLE }, // Good Food
            { 48, StaticObjectInteraction.OBJ_OBTAINABLE }, // First Aid
            { 49, StaticObjectInteraction.OBJ_OBTAINABLE }, // Clip
            { 50, StaticObjectInteraction.OBJ_OBTAINABLE }, // Machine Gun
            { 51, StaticObjectInteraction.OBJ_OBTAINABLE }, // Gatling Gun
            { 52, StaticObjectInteraction.OBJ_OBTAINABLE }, // Cross
            { 53, StaticObjectInteraction.OBJ_OBTAINABLE }, // Chalice
            { 54, StaticObjectInteraction.OBJ_OBTAINABLE }, // Bible
            { 55, StaticObjectInteraction.OBJ_OBTAINABLE }, // Crown
            { 56, StaticObjectInteraction.OBJ_OBTAINABLE }, // One Up
            { 57, StaticObjectInteraction.OBJ_OBTAINABLE }, // Gibs food
            { 58, StaticObjectInteraction.OBJ_BLOCKING }, // Barrel
            { 59, StaticObjectInteraction.OBJ_BLOCKING }, // Well
            { 60, StaticObjectInteraction.OBJ_BLOCKING }, // Empty Well
            { 61, StaticObjectInteraction.OBJ_OBTAINABLE }, // Edible Gibs 2
            { 62, StaticObjectInteraction.OBJ_BLOCKING }, // Flag
            { 63, StaticObjectInteraction.OBJ_BLOCKING }, // Aaaardwolf!
            { 67, StaticObjectInteraction.OBJ_BLOCKING }, // Gibs!
            { 68, StaticObjectInteraction.OBJ_BLOCKING }, // Stove
            { 69, StaticObjectInteraction.OBJ_BLOCKING }, // Spears
            { 71, StaticObjectInteraction.OBJ_BLOCKING}, // Marble Pillar
            { 72, StaticObjectInteraction.OBJ_OBTAINABLE}, // Box of ammo
            { 73, StaticObjectInteraction.OBJ_BLOCKING }, // Truck
            { 74, StaticObjectInteraction.OBJ_OBTAINABLE} // Spear of Destiny
            };
        //Mappings
        private readonly byte[] _architecturePlane;
        private readonly byte[] _objectsPlane;
        private readonly byte[] _logicPlane;

        public List<DynamicMapObject> DynamicMapObjects = [];
        public List<StaticMapObject> StaticMapObjects = [];
        public List<ActorMapObject> ActorMapObjects = [];
        public byte[][] levelTileMap;
        public int PlayerSpawnY { get; set; } = 0;
        public int PlayerSpawnX { get; set; } = 0;
        public string Name { get; init; }
        public ushort Width { get; init; }
        public ushort Height { get; init; }
        public byte[] GetPlane(MapPlanes plane)
        {
            return plane switch
            {
                MapPlanes.ARCHITECTURE => _architecturePlane,
                MapPlanes.OBJECTS => _objectsPlane,
                MapPlanes.LOGIC => _logicPlane,
                _ => [],
            };
        }
        public MapData(string name, ushort width, ushort height, byte[] architecturePlane, byte[] objectsPlane, byte[] logicPlane)
        {
            _architecturePlane = architecturePlane;
            _objectsPlane = objectsPlane;
            _logicPlane = logicPlane;
            Name = name;
            Width = width;
            Height = height;
            levelTileMap = new byte[Height][];
            for (int y = 0; y < Height; y++)
            {
                levelTileMap[y] = new byte[Width];
                for (int x = 0; x < Width; x++)
                {
                    //Stored as 16 bit word
                    byte hiByte = _architecturePlane[(y * Height + x) * 2];
                    //byte loByte = _architecturePlane[((y * Height + x) * 2)+1]; // Always 0
                    // We're only storing tiles that are floors. If we decide to use the sound prop tiles we'll store them elsewhere.
                    if (hiByte < 90)
                        levelTileMap[y][x] = hiByte;

                    if (hiByte >= 90 && hiByte <= 101)
                    {   // It's a door, spawn a dynamic object for it so we can track it.
                        levelTileMap[y][x] = hiByte;
                        SpawnDoorObject(hiByte, x, y);
                    }

                    hiByte = _objectsPlane[(y * Height + x) * 2];
                    SpawnMapObject(hiByte, y, x);

                }
            }
        }
        private static int AngleFromSpawnID(int spawnID)
        {
            // Convert the spawn ID to an angle.
            // The spawn ID is 19-22, which corresponds to angles 0, 90, 180, and 270 degrees.
            return spawnID switch
            {
                0 => 180,// North
                1 => 90,// East
                2 => 0,// South
                3 => 270,// West
                _ => 0,// Invalid spawn ID
            };
        }

        private void SpawnDoorObject(int tileNumber, int x, int y)
        {

            // Determine which type of door.
            byte doorType = 0;

            DynamicMapObject newObject = new()
            {
                Type = MapObjectTypes.MAPOBJECT_DOOR
            };

            switch (tileNumber)
            {
                case 90:
                case 92:
                case 94:
                case 96:
                case 98:
                case 100:
                    newObject.Direction = MapDirection.DIR_NORTH;
                    doorType = (byte)((tileNumber - 90) / 2);
                    break;
                case 91:
                case 93:
                case 95:
                case 97:
                case 99:
                case 101:
                    newObject.Direction = MapDirection.DIR_EAST;
                    doorType = (byte)((tileNumber - 91) / 2);
                    break;
            }

            newObject.SpawnX = x;
            newObject.SpawnY = y;
            newObject.X = x;
            newObject.Y = y;

            // It's a locked door, set the appropriate key type.
            if (doorType > 0 && doorType < 5)
            {
                newObject.KeyNumber = doorType;
            }

            DynamicMapObjects.Add(newObject);
        }
        public byte GetTileData(int x, int y)
        {
            if (x < 0 || y >= Height || x < 0 || x >= Width)
                return 1; //outside
            return levelTileMap[y][x];
        }
        public void SpawnMapObject(int objNumber, int y, int x)
        {
            if (objNumber == 98) // It's a pushwall.
            {
                DynamicMapObject newObject = new()
                {
                    Type = MapObjectTypes.MAPOBJECT_PUSHWALL,
                    SpawnX = x,
                    SpawnY = y,
                    X = x,
                    Y = y
                };
                DynamicMapObjects.Add(newObject);
            }
            else if (objNumber >= 19 && objNumber <= 22)
            { // It's a player spawn.
                PlayerSpawnY = y;
                PlayerSpawnX = x;
            }
            else if (objNumber >= 23 && objNumber <= 74)
            { // It's a static object.
                StaticMapObject newObject = new()
                {
                    X = x,
                    Y = y,
                    ObjectID = objNumber
                };

                // Determine if it's blocking or obtainable.
                if (StaticObjectClassification.ContainsKey((byte)objNumber))
                {
                    switch (StaticObjectClassification[(byte)objNumber])
                    {
                        case StaticObjectInteraction.OBJ_BLOCKING:
                            newObject.Blocking = true;
                            break;
                        case StaticObjectInteraction.OBJ_OBTAINABLE:
                            newObject.Obtainable = true;
                            break;
                    }
                }

                StaticMapObjects.Add(newObject);
            }
            else if (objNumber > 98)
            {
                ActorMapObject newActor;
                // It's an actor.
                switch (objNumber)
                {
                    // --= Guards =-- //
                    case 180: // Hard Skill Guard
                    case 181:
                    case 182:
                    case 183:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 180), "GuardStandHard");
                        ActorMapObjects.Add(newActor);
                        break;

                    case 144: // Medium Skill Guard
                    case 145:
                    case 146:
                    case 147:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 144), "GuardStandMedium");
                        ActorMapObjects.Add(newActor);
                        break;

                    case 108: // Easy Skill Guard
                    case 109:
                    case 110:
                    case 111:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 108), "GuardStandEasy");
                        ActorMapObjects.Add(newActor);
                        break;

                    case 184: // Hard Skill Guard Patrol
                    case 185:
                    case 186:
                    case 187:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 184), "GuardPathHard");
                        ActorMapObjects.Add(newActor);
                        break;
                    case 148: // Medium Skill Guard Patrol
                    case 149:
                    case 150:
                    case 151:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 148), "GuardPathMedium");
                        ActorMapObjects.Add(newActor);
                        break;
                    case 112: // Easy Skill Guard Patrol
                    case 113:
                    case 114:
                    case 115:
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 112), "GuardPathEasy");
                        ActorMapObjects.Add(newActor);
                        break;
                    case 124: // Dead Guard
                        newActor = new ActorMapObject(x, y, AngleFromSpawnID(objNumber - 124), "GuardDead");
                        ActorMapObjects.Add(newActor);
                        break;

                    default:
                        break;
                }
            }

        }

    }
}