using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace WolfensteinInfinite.WolfMod
{

    public class MapSection(int w, int h)
    {
        public const int ClosedSectionWall = 0;
        public const int ClosedSectionWallAny = 3;
        public const int ClosedSectionDoor = 1;
        public const int ClosedSectionFill = 2;
        public const int ClosedSectionNothing = -1;
        public const int ClosedSectionExterior = -2;
        public const int ClosedSectionInterior = -3;

        public static T[][] CopyJaggedArray<T>(T[][] source) where T : struct
        {
            ArgumentNullException.ThrowIfNull(source);

            // 1. Initialize the destination jagged array with the same number of rows.
            T[][] destination = new T[source.Length][];

            for (int i = 0; i < source.Length; i++)
            {
                // Get the current inner array from the source
                T[] sourceInner = source[i];
                // 2. Initialize each inner array in the destination with the correct length.
                T[] destInner = new T[sourceInner.Length];

                // 3. Use Buffer.BlockCopy for fast byte-wise copying of primitive types.
                // The size of the type is needed because BlockCopy works in bytes.
                int typeSize = Marshal.SizeOf<T>();
                int lengthInBytes = sourceInner.Length * typeSize;

                Buffer.BlockCopy(sourceInner, 0, destInner, 0, lengthInBytes);

                destination[i] = destInner;
            }

            return destination;
        }
        public static int[][] Empty(int w = 64, int h = 64, int value = -1)
        {
            int[][] ret = new int[h][];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new int[w];
                Array.Fill(ret[i], value);
            }
            return ret;
        }
        public static KeyValuePair<MapArrayLayouts, int[][]>[] Expand(MapSection section, int w = 64, int h = 64)
        {
            int[][] array = section.Walls;
            if (array.Length == h && array[0].Length == w) return section.Layers; //This is ok all should be one size
            var ret = new Dictionary<MapArrayLayouts, int[][]>
            {
                [MapArrayLayouts.WALLS] = Empty(w, h),
                [MapArrayLayouts.DECALS] = Empty(w, h),
                [MapArrayLayouts.ITEMS] = Empty(w, h),
                [MapArrayLayouts.ENEMY] = Empty(w, h),
                [MapArrayLayouts.DIFFICULTY] = Empty(w, h),
                [MapArrayLayouts.DOORS] = Empty(w, h),
                [MapArrayLayouts.SPECIAL] = Empty(w, h)
            };

            if (Enum.GetValues<MapArrayLayouts>().All(p => section.GetLayout(p).All(j => j.All(k => k == -1)))) return [.. ret];

            var ys = (h / 2) - array.Length / 2;
            var xs = (w / 2) - array[0].Length / 2;
            foreach (var l in Enum.GetValues<MapArrayLayouts>())
            {
                array = section.GetLayout(l);
                for (int y = 0; y < array.Length; y++)
                {
                    for (int x = 0; x < array[0].Length; x++)
                    {
                        ret[l][ys + y][xs + x] = array[y][x];
                    }
                }
            }
            return [.. ret];
        }
        public static KeyValuePair<MapArrayLayouts, int[][]>[] Trim(MapSection section)
        {
            int[][] array = section.Walls;
            var ret = new Dictionary<MapArrayLayouts, int[][]>
            {
                [MapArrayLayouts.WALLS] = Empty(),
                [MapArrayLayouts.DECALS] = Empty(),
                [MapArrayLayouts.ITEMS] = Empty(),
                [MapArrayLayouts.ENEMY] = Empty(),
                [MapArrayLayouts.DIFFICULTY] = Empty(),
                [MapArrayLayouts.DOORS] = Empty(),
                [MapArrayLayouts.SPECIAL] = Empty()
            };
            if (Enum.GetValues<MapArrayLayouts>().All(p => section.GetLayout(p).All(j => j.All(k => k == -1)))) return [.. ret];

            int minX = array[0].Length;
            int minY = array.Length;
            int maxX = 0;
            int maxY = 0;
            foreach (var l in Enum.GetValues<MapArrayLayouts>())
            {
                array = section.GetLayout(l);
                for (int y = 0; y < array.Length; y++)
                {
                    for (int x = 0; x < array[0].Length; x++)
                    {
                        var v = array[y][x];
                        if (v < 0) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            maxY++;
            maxX++;
            var h = Math.Max(maxY - minY, 0);
            var w = Math.Max(maxX - minX, 0);


            foreach (var l in Enum.GetValues<MapArrayLayouts>())
            {
                array = section.GetLayout(l);
                var yOff = 0;
                int[][] lRet = new int[h][];
                for (int y = minY; y < maxY; y++)
                {
                    var xOff = 0;
                    lRet[yOff] = new int[w];
                    for (int x = minX; x < maxX; x++)
                    {
                        lRet[yOff][xOff] = array[y][x];
                        xOff++;
                    }
                    yOff++;
                }
                ret[l] = lRet;
            }
            return [.. ret];
        }
        public int Id { get; set; } = 0;
        public int IntendedMinLevel { get; set; } = 1;
        [JsonIgnore]
        public bool IsFullMap => HasPlayerStart && HasPlayerExit &&
    GetClosedSection(out bool closed, out _, out _) != null && closed;
        public MapSection() : this(64, 64) { }

        public KeyValuePair<MapArrayLayouts, int[][]>[] Layers { get; set; } = [
                new(MapArrayLayouts.WALLS, Empty(w, h)),
                new(MapArrayLayouts.DECALS, Empty(w, h)),
                new(MapArrayLayouts.ITEMS, Empty(w, h)),
                new(MapArrayLayouts.ENEMY, Empty(w, h)),
                new(MapArrayLayouts.DIFFICULTY, Empty(w, h)),
                new(MapArrayLayouts.DOORS, Empty(w, h)),
                new(MapArrayLayouts.SPECIAL, Empty(w, h))
            ];
        public MapSection Clone() => Clone(this);
        public static MapSection Clone(MapSection section)
        {
            var m = new MapSection(section.Width, section.Height);
            var layers = new Dictionary<MapArrayLayouts, int[][]>
            {
                [MapArrayLayouts.WALLS] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.WALLS)),
                [MapArrayLayouts.DECALS] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.DECALS)),
                [MapArrayLayouts.ITEMS] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.ITEMS)),
                [MapArrayLayouts.ENEMY] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.ENEMY)),
                [MapArrayLayouts.DIFFICULTY] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.DIFFICULTY)),
                [MapArrayLayouts.DOORS] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.DOORS)),
                [MapArrayLayouts.SPECIAL] = CopyJaggedArray(section.GetLayout(MapArrayLayouts.SPECIAL))
            };
            m.Layers = [.. layers];
            return m;
        }
        public int[][] GetLayout(MapArrayLayouts layout)
        {
            foreach (var item in Layers)
            {
                if (item.Key == layout) return item.Value;
            }
            return Empty(Width, Height);
        }
        [JsonIgnore]
        public int Width => GetLayout(MapArrayLayouts.WALLS)[0].Length;
        [JsonIgnore]
        public int Height => GetLayout(MapArrayLayouts.WALLS).Length;

        public (int X, int Y)[] GetConnections(int xOffset = 0, int yOffset = 0)
        {
            var connections = new List<(int X, int Y)>();
            var closedSection = GetClosedSection(out _, out _, out _);
            if (closedSection == null) return [.. connections];
            var array = GetLayout(MapArrayLayouts.DOORS);
            for (int y = 0; y < array.Length; y++)
            {
                for (int x = 0; x < array[0].Length; x++)
                {

                    if (array[y][x] >= 0)
                    {
                        //check is not door inside room. if the door is at an edge it is not inside room
                        //if a door is facing nothing -1 in the closed section it is not inside a room
                        bool hasVoid = false;
                        if (y == 0) hasVoid = true;
                        if (y == array.Length - 1) hasVoid = true;
                        if (x == 0) hasVoid = true;
                        if (x == array[0].Length - 1) hasVoid = true;

                        if (y + 1 < array.Length && closedSection[y + 1][x] < 0) hasVoid = true;
                        if (y - 1 >= 0 && closedSection[y - 1][x] < 0) hasVoid = true;

                        if (x + 1 < array[0].Length && closedSection[y][x + 1] < 0) hasVoid = true;
                        if (x - 1 >= 0 && closedSection[y][x - 1] < 0) hasVoid = true;

                        if (hasVoid) connections.Add((x + xOffset, y + yOffset));
                    }

                }
            }
            return [.. connections];
        }
        [JsonIgnore]
        public int[][] Walls => GetLayout(MapArrayLayouts.WALLS);
        [JsonIgnore]
        public int[][] Decals => GetLayout(MapArrayLayouts.DECALS);
        [JsonIgnore]
        public int[][] Items => GetLayout(MapArrayLayouts.ITEMS);
        [JsonIgnore]
        public int[][] Enemy => GetLayout(MapArrayLayouts.ENEMY);
        [JsonIgnore]
        public int[][] Difficulty => GetLayout(MapArrayLayouts.DIFFICULTY);
        [JsonIgnore]
        public int[][] Doors => GetLayout(MapArrayLayouts.DOORS);
        [JsonIgnore]
        public int[][] Special => GetLayout(MapArrayLayouts.SPECIAL);
        [JsonIgnore]
        public bool HasPlayerStart => Special.Any(y => y.Any(x => x == 0));
        [JsonIgnore]
        public bool HasPlayerExit => Special.Any(y => y.Any(x => x == 3));
        [JsonIgnore]
        public bool HasKeys => Items.Any(y => y.Any(x => x == 21));
        [JsonIgnore]
        public bool HasLockedDoor => Doors.Any(y => y.Any(x => x == 2));
        [JsonIgnore]
        public bool HasSecret => Items.Any(y => y.Any(x => x == 16));
        [JsonIgnore]
        public bool HasRadio => Items.Any(y => y.Any(x => x == 17));
        [JsonIgnore]
        public bool HasDynamite => Items.Any(y => y.Any(x => x == 18));
        [JsonIgnore]
        public bool HasDynamitePlacement => Items.Any(y => y.Any(x => x == 19));
        [JsonIgnore]
        public bool HasPow => Items.Any(y => y.Any(x => x == 15));
        public bool HasBoss(Mod mod)
        {
            if (Special.Any(y => y.Any(x => x == 2))) return true;
            var bossIDs = mod.Enemies.Where(p => (int)p.EnemyType >= 5 && (int)p.EnemyType <= 12).Select(p => p.MapID);
            return Enemy.Any(y => y.Any(x => bossIDs.Contains(x)));
        }
        public bool HasNothing(Mod mod) => !(HasKeys || HasLockedDoor || HasSecret || HasRadio || HasDynamite || HasDynamitePlacement || HasPow || HasBoss(mod));



        public int[][]? GetClosedSection(out bool closed, out bool noDoors, out bool multiple) => GetClosedSection(Walls, Doors, Special, out closed, out noDoors, out multiple);
        //Wall = 0, Door = 1, Fill = 2, nothing = -1


        public static int[][]? GetClosedSection(int[][] walls, int[][] doors, int[][] special, out bool closed, out bool noDoors, out bool multiple)
        {
            int height = walls.Length;
            int width = walls[0].Length;

            // Step 1: Flatten into single array: door=1, wall=0, empty=-1
            var grid = new int[height][];
            bool anyDoorFound = false;

            for (int y = 0; y < height; y++)
            {
                grid[y] = new int[width];
                for (int x = 0; x < width; x++)
                {
                    if (doors[y][x] >= 0)
                    {
                        grid[y][x] = ClosedSectionDoor;  // Door
                        anyDoorFound = true;
                    }
                    else if (walls[y][x] >= 0)
                    {
                        grid[y][x] = special[y][x] == 8 ? ClosedSectionWallAny : ClosedSectionWall;  // Wall
                    }
                    else
                    {

                        grid[y][x] = ClosedSectionNothing;  // Empty
                    }
                }
            }

            // Step 2: Find first DOOR that has a face connected to empty (-1)
            int startY = -1, startX = -1;
            int emptyY = -1, emptyX = -1;
            bool foundStart = false;

            for (int y = 0; y < height && !foundStart; y++)
            {
                for (int x = 0; x < width && !foundStart; x++)
                {
                    if (grid[y][x] == ClosedSectionDoor)  // Found a door
                    {
                        var neighbors = new[] {
                    (y - 1, x), // up
                    (y, x + 1), // right
                    (y + 1, x), // down
                    (y, x - 1)  // left
                };

                        foreach (var (ny, nx) in neighbors)
                        {
                            if (!IsInBounds(ny, nx, height, width) || grid[ny][nx] == ClosedSectionNothing)
                            {
                                startY = y;
                                startX = x;
                                emptyY = ny;
                                emptyX = nx;
                                foundStart = true;
                                break;
                            }
                        }
                    }
                }
            }

            // No valid starting door found
            if (!foundStart)
            {
                closed = false;
                noDoors = !anyDoorFound;
                multiple = false;
                return null;
            }

            // Step 3: Trace perimeter using wall-following (right-hand rule)
            var perimeter = new HashSet<(int y, int x)>();

            if (!TracePerimeterWallFollowing(grid, startY, startX, emptyY, emptyX, perimeter, height, width))
            {
                // Failed to trace back to start
                closed = false;
                noDoors = !anyDoorFound;
                multiple = false;
                return null;
            }

            // Need at least 8 nodes for a valid closed room (minimum 3x3)
            if (perimeter.Count < 8)
            {
                closed = false;
                noDoors = !anyDoorFound;
                multiple = false;
                return null;
            }

            // Step 4: Create isChecked array and mark perimeter as checked
            var isChecked = new bool[height][];
            for (int y = 0; y < height; y++)
            {
                isChecked[y] = new bool[width];
            }

            foreach (var (y, x) in perimeter)
            {
                isChecked[y][x] = true;
            }

            // Step 5b: Flood fill exterior (mark as 3 in grid)
            // Start from all boundary cells that are empty
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check boundaries
                    if ((y == 0 || y == height - 1 || x == 0 || x == width - 1) && grid[y][x] == -1)
                    {
                        FloodFillExterior(grid, y, x, height, width);
                    }
                }
            }

            // Step 5c: Fill any remaining -1 spaces (enclosed interior spaces like rooms within rooms)
            // These are spaces enclosed by interior walls
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[y][x] == ClosedSectionNothing)
                    {
                        // This is an enclosed interior space - mark it as interior
                        FloodFillEnclosedInterior(grid, y, x, height, width);
                    }
                }
            }

            // Step 6: Check for orphaned walls/doors
            // First, mark all walls/doors connected to the perimeter
            var connectedWalls = new HashSet<(int y, int x)>(perimeter);
            var wallQueue = new Queue<(int y, int x)>();

            foreach (var pos in perimeter)
            {
                wallQueue.Enqueue(pos);
            }

            while (wallQueue.Count > 0)
            {
                var (y, x) = wallQueue.Dequeue();

                var neighbors = new[] {
        (y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)
    };

                foreach (var (ny, nx) in neighbors)
                {
                    if (IsInBounds(ny, nx, height, width) &&
                        !connectedWalls.Contains((ny, nx)) &&
                        (grid[ny][nx] == ClosedSectionWallAny || grid[ny][nx] == ClosedSectionWall || grid[ny][nx] == ClosedSectionDoor))
                    {
                        connectedWalls.Add((ny, nx));
                        wallQueue.Enqueue((ny, nx));
                    }
                }
            }

            // Now check if there are any walls/doors NOT in connectedWalls
            // These must be enclosed within the interior to be valid
            bool hasOrphans = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((grid[y][x] == ClosedSectionWallAny || grid[y][x] == ClosedSectionWall || grid[y][x] == ClosedSectionDoor) && !connectedWalls.Contains((y, x)))
                    {
                        // This wall/door is not directly connected to perimeter
                        // Check if it's enclosed within the interior
                        bool isEnclosed = true;
                        bool hasInteriorNeighbor = false;

                        var neighbors = new[] { (y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1) };

                        foreach (var (ny, nx) in neighbors)
                        {
                            if (!IsInBounds(ny, nx, height, width))
                            {
                                // Adjacent to boundary - could be exterior
                                isEnclosed = false;
                                break;
                            }

                            int neighborVal = grid[ny][nx];

                            if (neighborVal == ClosedSectionInterior || neighborVal == ClosedSectionExterior)  // Interior or enclosed interior
                            {
                                hasInteriorNeighbor = true;
                            }
                            else if (neighborVal == ClosedSectionExterior)  // Exterior
                            {
                                isEnclosed = false;
                                break;
                            }
                        }

                        // If enclosed and has at least one interior neighbor, it's part of our room
                        if (!isEnclosed || !hasInteriorNeighbor)
                        {
                            hasOrphans = true;
                            break;
                        }
                    }
                }
                if (hasOrphans) break;
            }

            if (hasOrphans)
            {
                closed = true;
                noDoors = false;
                multiple = true;
                return null;
            }

            // Success!
            closed = true;
            noDoors = false;
            multiple = false;
            return grid;
        }

        // Flood fill enclosed interior spaces (mark as 4)
        private static void FloodFillEnclosedInterior(int[][] grid, int startY, int startX, int height, int width)
        {
            var stack = new Stack<(int y, int x)>();
            stack.Push((startY, startX));

            while (stack.Count > 0)
            {
                var (y, x) = stack.Pop();

                // Skip if out of bounds
                if (!IsInBounds(y, x, height, width))
                    continue;

                // Skip if not empty
                if (grid[y][x] != ClosedSectionNothing)
                    continue;

                // Mark as enclosed interior
                grid[y][x] = ClosedSectionInterior;

                // Add neighbors
                stack.Push((y - 1, x));
                stack.Push((y + 1, x));
                stack.Push((y, x - 1));
                stack.Push((y, x + 1));
            }
        }

        // Wall-following algorithm to trace perimeter
        private static bool TracePerimeterWallFollowing(int[][] grid, int startY, int startX,
    int emptyY, int emptyX, HashSet<(int y, int x)> perimeter, int height, int width)
        {
            // Direction: 0=up, 1=right, 2=down, 3=left
            int[] dy = [-1, 0, 1, 0];
            int[] dx = [0, 1, 0, -1];

            // Determine initial direction
            int direction = GetInitialDirection(startY, startX, emptyY, emptyX);

            int currentY = startY;
            int currentX = startX;
            int currentDir = direction;
            int steps = 0;
            int maxSteps = height * width * 4;

            do
            {
                perimeter.Add((currentY, currentX));
                steps++;

                if (steps > maxSteps)
                    return false;

                bool moved = false;

                // Try turns in order: left (-1), forward (0), right (+1), back (+2)
                for (int turn = -1; turn <= 2; turn++)
                {
                    int tryDir = (currentDir + turn + 4) % 4;
                    int nextY = currentY + dy[tryDir];
                    int nextX = currentX + dx[tryDir];

                    // Must be in bounds and be a wall or door
                    if (!IsInBounds(nextY, nextX, height, width) ||
                        (grid[nextY][nextX] != ClosedSectionWallAny && grid[nextY][nextX] != ClosedSectionWall && grid[nextY][nextX] != ClosedSectionDoor))
                        continue;

                    // Check what's on our left after this move
                    int leftDir = (tryDir + 3) % 4;
                    int leftY = nextY + dy[leftDir];
                    int leftX = nextX + dx[leftDir];

                    // Left should be empty, out of bounds, or already filled
                    bool leftOk = !IsInBounds(leftY, leftX, height, width) ||
                                 grid[leftY][leftX] == ClosedSectionNothing ||
                                 grid[leftY][leftX] == ClosedSectionFill;

                    // For forward and right turns, we're more lenient
                    // For left turns, we need to ensure we're properly following the wall
                    if (turn == -1 && !leftOk)
                        continue;

                    currentY = nextY;
                    currentX = nextX;
                    currentDir = tryDir;
                    moved = true;
                    break;
                }

                if (!moved)
                    return false;

            } while (currentY != startY || currentX != startX || steps < 2);

            return true;
        }

        // Get initial direction based on where the empty space is
        private static int GetInitialDirection(int wallY, int wallX, int emptyY, int emptyX)
        {
            // Determine which direction to face so empty is on our left
            if (emptyY < wallY) return 1;  // Empty is above, face right
            if (emptyY > wallY) return 3;  // Empty is below, face left
            if (emptyX < wallX) return 0;  // Empty is left, face up
            return 2;  // Empty is right, face down
        }

        private static bool IsInBounds(int y, int x, int height, int width)
        {
            return y >= 0 && y < height && x >= 0 && x < width;
        }

        // Flood fill exterior starting from boundary empty cells (mark as 3)
        private static void FloodFillExterior(int[][] grid, int startY, int startX, int height, int width)
        {
            var stack = new Stack<(int y, int x)>();
            stack.Push((startY, startX));

            while (stack.Count > 0)
            {
                var (y, x) = stack.Pop();

                // Skip if out of bounds
                if (!IsInBounds(y, x, height, width))
                    continue;

                // Skip if not empty or already processed
                if (grid[y][x] != ClosedSectionNothing)
                    continue;

                // Mark as exterior
                grid[y][x] = ClosedSectionExterior;

                // Add neighbors
                stack.Push((y - 1, x));
                stack.Push((y + 1, x));
                stack.Push((y, x - 1));
                stack.Push((y, x + 1));
            }
        }

        public int SectionHash
        {
            get
            {
                var hash = 0;
                foreach(var l in Layers)
                {
                    foreach(var r in l.Value)
                    {
                        foreach (var j in r)
                        {
                            hash = HashCode.Combine(hash, l.Key, j);
                        }
                    }

                }
                return hash;
            }
        }
    }
}
