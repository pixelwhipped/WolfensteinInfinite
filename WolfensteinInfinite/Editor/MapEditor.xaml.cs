using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameHelpers;
using WolfensteinInfinite.GameMap;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.Editor
{
    /// <summary>
    /// Interaction logic for MapEditor.xaml
    /// </summary>
    public partial class MapEditor : Window
    {
        public Mod? ActiveMod { get; set; }
        public Wolfenstein Wolfenstein { get; init; }
        public MapSection? ActiveSection { get; set; }

        private int SelectedWallIndex = -1;
        private int SelectedDecalIndex = -1;
        private int SelectedItemIndex = -1;
        private int SelectedEnemyIndex = -1;
        private int SelectedSpecialIndex = -1;
        private int SelectedDoorIndex = -1;
        private int SelectedDifficulty = 0;

        private readonly Dictionary<string, WriteableBitmap> SpecialMapCache = [];
        private readonly Dictionary<string, WriteableBitmap> TextureCache = [];
        private readonly Dictionary<string, WriteableBitmap> DecalCache = [];
        private readonly Dictionary<string, Direction> DecalDirectionCache = [];
        private readonly Dictionary<int, WriteableBitmap> ItermCache = [];
        private readonly Dictionary<string, WriteableBitmap> EnemyCache = [];
        private readonly Dictionary<int, WriteableBitmap> SpecialCache = [];
        private readonly Dictionary<string, WriteableBitmap> DoorCache = [];
        private readonly Dictionary<Mod, bool> ChangeStates = [];

        // Bitmap canvas fields — replaces per-tile SectionButton grid
        private const int TileSize = 64;
        private WriteableBitmap? _mapBitmap;
        private int _mapTilesW;
        private int _mapTilesH;

        // ──────────────────────────────────────────────────────────────
        // Remove handlers — no longer take SectionButton parameter
        // ──────────────────────────────────────────────────────────────

        private bool HandleMapTryRemoveItem(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Items[y][x] = -1;
            ActiveSection.Difficulty[y][x] = 0;
            changed = true;
            return true;
        }

        private bool HandleMapTryRemoveEnemy(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Enemy[y][x] = -1;
            ActiveSection.Difficulty[y][x] = 0;
            changed = true;
            return true;
        }

        private bool HandleMapTryRemoveDecals(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Decals[y][x] = -1;
            changed = true;
            return true;
        }

        private bool HandleMapTryRemoveDoors(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Doors[y][x] = -1;
            changed = true;
            return true;
        }

        private bool HandleMapTryRemoveSpecial(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Special[y][x] = -1;
            changed = true;
            return true;
        }

        private bool HandleMapTryRemoveWall(int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            if (ActiveSection.Special[y][x] >= 0) HandleMapTryRemoveSpecial(x, y, ref changed);
            if (IsDoorConnected(x, y, out List<(int x, int y)> where))
            {
                foreach (var d in where)
                {
                    HandleMapTryRemoveDoors(d.x, d.y, ref changed);
                    RedrawTile(d.x, d.y);
                }
            }
            ActiveSection.Walls[y][x] = -1;
            changed = true;
            return true;
        }

        private delegate bool TryRemoveDelegate(ref bool changed);

        // ──────────────────────────────────────────────────────────────
        // Main click handler — reads/writes ActiveSection data directly,
        // calls RedrawTile after via MapClick
        // ──────────────────────────────────────────────────────────────

        private void HandleMapClick(int x, int y, bool isLeft, ref bool changed)
        {
            if (ActiveSection == null) return;
            if (ActiveMod == null) return;

            bool IsApplyingChance() =>
                isLeft && LayerControl.SelectedItem == LayerSpecialControl
                && SelectedSpecialIndex >= 9 && SelectedSpecialIndex <= 12;

            var removers = new Dictionary<TabItem, TryRemoveDelegate>()
            {
                [LayerWallsControl] = (ref bool c) => LayerControl.SelectedItem != LayerSpecialControl && ActiveSection.Walls[y][x] >= 0 && !HandleMapTryRemoveWall(x, y, ref c),
                [LayerDecalsControl] = (ref bool c) => ActiveSection.Decals[y][x] >= 0
                    && !IsApplyingChance() && !HandleMapTryRemoveDecals(x, y, ref c),
                [LayerItemsControl] = (ref bool c) => ActiveSection.Items[y][x] >= 0
                    && !IsApplyingChance() && !HandleMapTryRemoveItem(x, y, ref c),
                [LayerDoorControl] = (ref bool c) => ActiveSection.Doors[y][x] >= 0
                    && !HandleMapTryRemoveDoors(x, y, ref c),
                [LayerEnemiesControl] = (ref bool c) => ActiveSection.Enemy[y][x] >= 0
                    && !IsApplyingChance() && !HandleMapTryRemoveEnemy(x, y, ref c),
                [LayerSpecialControl] = (ref bool c) => ActiveSection.Special[y][x] >= 0
                    && !HandleMapTryRemoveSpecial(x, y, ref c)
            };

            if (isLeft)
            {
                foreach (var r in removers.Where(p => p.Key != LayerControl.SelectedItem))
                    if (r.Value(ref changed)) return;

                if (LayerControl.SelectedItem == LayerWallsControl)
                {
                    ActiveSection.Walls[y][x] = SelectedWallIndex;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerDecalsControl)
                {
                    ActiveSection.Decals[y][x] = SelectedDecalIndex;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerItemsControl)
                {
                    ActiveSection.Items[y][x] = SelectedItemIndex;
                    ActiveSection.Difficulty[y][x] = SelectedDifficulty;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerDoorControl)
                {
                    var dir = IsDoorBetweenWalls(x, y);
                    if (dir == DoorDirection.NONE) return;
                    ActiveSection.Doors[y][x] = SelectedDoorIndex;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerEnemiesControl)
                {
                    if (SelectedEnemyIndex < 0)
                    {
                        ActiveSection.Enemy[y][x] = -1;
                        ActiveSection.Difficulty[y][x] = 0;
                    }
                    else if (SelectedEnemyIndex >= 1000) // Virtual: Random Enemy (1001) or Experimental (1002)
                    {
                        ActiveSection.Enemy[y][x] = SelectedEnemyIndex;
                        ActiveSection.Difficulty[y][x] = SelectedDifficulty;
                    }
                    else
                    {
                        ActiveSection.Enemy[y][x] = Wolfenstein.Mods[ActiveMod.Name].Enemies[SelectedEnemyIndex].MapID;
                        ActiveSection.Difficulty[y][x] = SelectedDifficulty;
                    }
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerSpecialControl)
                {
                    if (ActiveSection.Walls[y][x] >= 0 && SelectedSpecialIndex < 3) return;
                    if (ActiveSection.Walls[y][x] < 0 && SelectedSpecialIndex >= 3)
                    {
                        if (SelectedSpecialIndex >= 9 && SelectedSpecialIndex <= 12)
                        {
                            bool hasTarget = ActiveSection.Items[y][x] >= 0
                                || ActiveSection.Enemy[y][x] >= 0
                                || ActiveSection.Decals[y][x] >= 0;
                            if (hasTarget)
                            {
                                ActiveSection.Special[y][x] = SelectedSpecialIndex;
                                changed = true;
                            }
                            return;
                        }
                    }
                    if (ActiveSection.Doors[y][x] >= 0) return;

                    // Clear any existing duplicate player start
                    if (SelectedSpecialIndex == 0)
                    {
                        for (int sy = 0; sy < ActiveSection.Special.Length; sy++)
                        {
                            for (int sx = 0; sx < ActiveSection.Special[sy].Length; sx++)
                            {
                                if (sx == x && sy == y) continue;
                                if (ActiveSection.Special[sy][sx] == 0)
                                {
                                    ActiveSection.Special[sy][sx] = -1;
                                    RedrawTile(sx, sy);
                                    sy = ActiveSection.Special.Length;
                                    break;
                                }
                            }
                        }
                    }

                    ActiveSection.Special[y][x] = SelectedSpecialIndex;
                    changed = true;
                }
            }
            else if (LayerControl.SelectedItem is TabItem t && removers.TryGetValue(t, out var tryRemove))
            {
                tryRemove(ref changed);
            }
        }

        private void SectionCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ActiveSection == null || ActiveMod == null) return;
            var pos = e.GetPosition(SectionCanvas);
            int tx = (int)(pos.X / TileSize);
            int ty = (int)(pos.Y / TileSize);
            if (tx < 0 || ty < 0 || tx >= _mapTilesW || ty >= _mapTilesH) return;

            bool changed = false;
            HandleMapClick(tx, ty, e.LeftButton == MouseButtonState.Pressed, ref changed);

            if (changed)
            {
                RedrawTile(tx, ty);
                ChangeStates[ActiveMod] = true;
                SetSaveButtonStates();
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Bitmap canvas rendering
        // ──────────────────────────────────────────────────────────────

        private void BuildMapBitmap()
        {
            if (ActiveSection == null || ActiveMod == null) return;
            _mapTilesW = ActiveSection.Walls[0].Length;
            _mapTilesH = ActiveSection.Walls.Length;

            _mapBitmap = new WriteableBitmap(
                _mapTilesW * TileSize, _mapTilesH * TileSize,
                96, 96, PixelFormats.Pbgra32, null);

            SectionImage.Source = _mapBitmap;
            SectionImage.Width = _mapTilesW * TileSize;
            SectionImage.Height = _mapTilesH * TileSize;
            SectionCanvas.Width = _mapTilesW * TileSize;
            SectionCanvas.Height = _mapTilesH * TileSize;
            // Ensure GridCanvas matches exactly
            GridCanvas.Width = _mapTilesW * TileSize;
            GridCanvas.Height = _mapTilesH * TileSize;
            Canvas.SetLeft(GridCanvas, 0);
            Canvas.SetTop(GridCanvas, 0);

            for (int y = 0; y < _mapTilesH; y++)
                for (int x = 0; x < _mapTilesW; x++)
                    RedrawTile(x, y);
            DrawGridOverlay();
            CenterContent(CurrentMapView, SectionCanvas);
        }
        private void RedrawTile(int x, int y)
        {
            if (_mapBitmap == null || ActiveSection == null || ActiveMod == null) return;

            WriteableBitmap? tile = null;
            var sp = ActiveSection.Special[y][x];

            if (sp >= 0)
            {
                if (sp >= 9 && sp <= 12)
                {
                    if (ActiveSection.Decals[y][x] >= 0)
                        tile = GetSpecialBitmapDecal(ActiveMod.Name, sp, ActiveSection.Decals[y][x]);
                    else if (ActiveSection.Items[y][x] >= 0)
                        tile = GetSpecialBitmapItem(ActiveMod.Name, sp, ActiveSection.Items[y][x]);
                    else if (ActiveSection.Enemy[y][x] >= 0)
                    {
                        BlitEnemyTile(x, y, sp);
                        return;
                    }
                }
                else
                {
                    tile = GetSpecialBitmap(ActiveMod.Name, sp, ActiveSection.Walls[y][x]);
                }
            }
            else if (ActiveSection.Decals[y][x] >= 0)
                tile = GetDecalBitmap(ActiveMod.Name, ActiveSection.Decals[y][x]);
            else if (ActiveSection.Items[y][x] >= 0)
                tile = GetItemBitmap(ActiveSection.Items[y][x]);
            else if (ActiveSection.Enemy[y][x] >= 0)
            {
                BlitEnemyTile(x, y, -1);
                return;
            }
            else if (ActiveSection.Doors[y][x] >= 0)
                tile = GetDoorBitmap(ActiveSection.Doors[y][x], IsDoorBetweenWalls(x, y));
            else if (ActiveSection.Walls[y][x] >= 0)
                tile = GetTextureBitmap(ActiveMod.Name, ActiveSection.Walls[y][x]);

            BlitTile(_mapBitmap, tile, x * TileSize, y * TileSize);
        }

        private void BlitEnemyTile(int x, int y, int specialChance) //new
        {
            if (_mapBitmap == null || ActiveSection == null || ActiveMod == null) return;
            var enemyId = ActiveSection.Enemy[y][x];
            WriteableBitmap? tile;
            if (enemyId >= 1000)
                tile = specialChance >= 9 && specialChance <= 12
                    ? GetSpecialBitmapVirtualEnemy(enemyId, specialChance)
                    : GetVirtualEnemyBitmap(enemyId);
            else
                tile = specialChance >= 9 && specialChance <= 12
                    ? GetSpecialBitmapEnemy(ActiveMod.Name, specialChance, enemyId)
                    : GetEnemyBitmap(ActiveMod.Name, enemyId);
            BlitTile(_mapBitmap, tile, x * TileSize, y * TileSize);
            BlitDifficultyOverlay(_mapBitmap, ActiveSection.Difficulty[y][x], x * TileSize, y * TileSize);
        }

        // Uses the existing EditRandomEnemy / EditExperimentEnemy textures already in Wolfenstein.Special
        private WriteableBitmap GetVirtualEnemyBitmap(int mapId)
        {
            // Special[1] = EditRandomEnemy, Special[2] = EditExperimentEnemy
            int specialKey = mapId == 1001 ? 1 : 2;
            if (Wolfenstein.Special.TryGetValue(specialKey, out var tex))
                return GetBitmap(tex);
            // Fallback: should never be reached after normal initialisation
            var t = new Texture32(64, 64);
            t.RectFill(0, 0, 64, 64, mapId == 1001 ? (byte)160 : (byte)0, 32, mapId == 1001 ? (byte)200 : (byte)180, 255);
            return GetBitmap(t);
        }

        // Blends the chance % overlay onto a virtual enemy texture (1001/1002), cached like all other special blends
        private WriteableBitmap? GetSpecialBitmapVirtualEnemy(int mapId, int specialChance)
        {
            int specialKey = mapId == 1001 ? 1 : 2;
            if (!Wolfenstein.Special.TryGetValue(specialKey, out var tex)) return GetVirtualEnemyBitmap(mapId);
            var key = $"virtual{mapId},{specialChance}";
            if (SpecialMapCache.TryGetValue(key, out var bmp)) return bmp;
            return BlendSpecialTexture(tex, specialChance, key);
        }
        private void DrawGridOverlay()
        {
            GridCanvas.Children.Clear();
            int w = _mapTilesW;
            int h = _mapTilesH;

            for (int tx = 0; tx <= w; tx++)
            {
                // Major on 5-tile intervals and always on first/last edge
                bool major = tx % 5 == 0 || tx == w;
                var line = new System.Windows.Shapes.Line
                {
                    X1 = tx * TileSize,
                    Y1 = 0,
                    X2 = tx * TileSize,
                    Y2 = h * TileSize,
                    Stroke = major ? Brushes.Gray : Brushes.DimGray,
                    StrokeThickness = major ? 1.5 : 0.5,
                    IsHitTestVisible = false
                };
                GridCanvas.Children.Add(line);
            }

            for (int ty = 0; ty <= h; ty++)
            {
                bool major = ty % 5 == 0 || ty == h;
                var line = new System.Windows.Shapes.Line
                {
                    X1 = 0,
                    Y1 = ty * TileSize,
                    X2 = w * TileSize,
                    Y2 = ty * TileSize,
                    Stroke = major ? Brushes.Gray : Brushes.DimGray,
                    StrokeThickness = major ? 1.5 : 0.5,
                    IsHitTestVisible = false
                };
                GridCanvas.Children.Add(line);
            }
        }
        private static unsafe void BlitDifficultyOverlay(WriteableBitmap dest, int difficulty, int px, int py)
        {

            var (r, g, b) = GetDifficultyColor(difficulty);

            dest.Lock();
            byte* dst = (byte*)dest.BackBuffer + py * dest.BackBufferStride + px * 4;

            // Draw a 10x10 dot in top-left corner
            for (int row = 2; row < 12; row++)
            {
                byte* rowPtr = dst + row * dest.BackBufferStride;
                for (int col = 2; col < 12; col++)
                {
                    int offset = col * 4;
                    rowPtr[offset] = b; // BGRA
                    rowPtr[offset + 1] = g;
                    rowPtr[offset + 2] = r;
                    rowPtr[offset + 3] = 255;
                }
            }

            dest.AddDirtyRect(new Int32Rect(px, py, TileSize, TileSize));
            dest.Unlock();
        }
        private static unsafe void BlitTile(WriteableBitmap dest, WriteableBitmap? src, int px, int py)
        {
            dest.Lock();
            byte* dst = (byte*)dest.BackBuffer + py * dest.BackBufferStride + px * 4;

            if (src == null)
            {
                for (int row = 0; row < TileSize; row++)
                {
                    byte* rowPtr = dst + row * dest.BackBufferStride;
                    for (int col = 0; col < TileSize * 4; col += 4)
                    {
                        rowPtr[col] = rowPtr[col + 1] = rowPtr[col + 2] = 32;
                        rowPtr[col + 3] = 255;
                    }
                }
            }
            else
            {
                src.Lock();
                byte* s = (byte*)src.BackBuffer;
                for (int row = 0; row < TileSize; row++)
                {
                    Buffer.MemoryCopy(
                        s + row * src.BackBufferStride,
                        dst + row * dest.BackBufferStride,
                        TileSize * 4, TileSize * 4);
                }
                src.Unlock();
            }

            dest.AddDirtyRect(new Int32Rect(px, py, TileSize, TileSize));
            dest.Unlock();
        }

        // ──────────────────────────────────────────────────────────────
        // Constructor and initialization
        // ──────────────────────────────────────────────────────────────

        public MapEditor(Wolfenstein wolfenstein)
        {
            Wolfenstein = wolfenstein;
            InitializeComponent();
            InitializeOptions();

        }

        // Colour per difficulty: 0=green, 1=yellow, 2=orange, 3=red
        public static (byte r, byte g, byte b) GetDifficultyColor(int difficulty)
        {
            (byte r, byte g, byte b) colour = difficulty switch
            {
                0 => (0, 200, 0),
                1 => (200, 200, 0),
                2 => (255, 140, 0),
                _ => (200, 0, 0)
            };
            return colour;
        }
        private void InitializeOptions()
        {
            foreach (var mod in Wolfenstein.Mods)
            {
                if (ValidateMod(mod.Value))
                {
                    ModSelection.Items.Add(mod.Key);
                    ChangeStates.Add(mod.Value, false);
                }
            }
            foreach (var difficulty in Enum.GetValues<Difficulties>())
            {
                var st = new StackPanel() { Orientation = Orientation.Horizontal, Height = 16 };
                var (r, g, b) = GetDifficultyColor((int)difficulty);
                Rectangle myRgbRectangle = new()
                {
                    Width = 14,
                    Height = 414
                };
                Color myColor = Color.FromArgb(255, r, g, b);
                SolidColorBrush solidColorBrush = new(myColor);
                SolidColorBrush mySolidColorBrush = solidColorBrush;

                myRgbRectangle.Fill = mySolidColorBrush;
                var tb = new TextBlock
                {
                    Text = DifficultyHelpers.GetDifficultyString(difficulty)
                };
                st.Children.Add(myRgbRectangle);
                st.Children.Add(tb);
                DifficultySelection.Items.Add(st);
            }
            DifficultySelection.SelectedIndex = 0;

            SaveTargetSelection.Items.Add("map.json");
            SaveTargetSelection.Items.Add("specialmap.json");
            SaveTargetSelection.Items.Add("maptestlevel.json");
            SaveTargetSelection.SelectedIndex = 0;
            SaveTargetSelection.SelectionChanged += (s, e) =>
            {
                if (ActiveMod != null) SetMapSectionSelections();
            };
            SetSaveButtonStates();
        }

        private static bool ValidateMod(Mod mod) => mod.Textures.Length > 0;

        // ──────────────────────────────────────────────────────────────
        // Mod / section selection
        // ──────────────────────────────────────────────────────────────

        private void ModSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var m = ModSelection.SelectedItem as string;
            if (string.IsNullOrEmpty(m)) return;
            ActiveSection = null;
            if (Wolfenstein.Mods.TryGetValue(m, out Mod? value)) SetActiveMod(value);
            SetSaveButtonStates();
        }

        private void SetActiveMod(Mod mod)
        {
            if (mod == null) return;
            ActiveMod = mod;
            SelectedWallIndex = -1;
            SelectedDecalIndex = -1;
            SelectedItemIndex = -1;
            SelectedDoorIndex = -1;
            SelectedEnemyIndex = -1;
            SelectedSpecialIndex = -1;
            MigrateSpecialEnemiesToEnemyLayer(mod);
            SetMapSectionSelections();
            SetWallTextureImageGrid();
            SetDecalTextureImageGrid();
            SetItemTextureImageGrid();
            SetEnemyTextureImageGrid();
            SetDoorTextureImageGrid();
            SetSpecialTextureImageGrid();
        }

        // One-time migration: moves Special layer IDs 1 (Random Enemy) and 2 (Experimental Enemy)
        // to the Enemy layer as IDs 1001 and 1002, then clears them from Special.
        // Marks the mod dirty so the next save persists the migration automatically.
        private void MigrateSpecialEnemiesToEnemyLayer(Mod mod)
        {
            bool migrated = false;
            foreach (var target in new[] { "map.json", "specialmap.json", "maptestlevel.json" })
            {
                var builder = target switch
                {
                    "specialmap.json" when Wolfenstein.SpecialMaps.TryGetValue(mod.Name, out var sm) => sm,
                    "maptestlevel.json" when Wolfenstein.TestMaps.TryGetValue(mod.Name, out var tm) => tm,
                    _ when Wolfenstein.BuilderMods.TryGetValue(mod.Name, out var bm) => bm,
                    _ => null
                };
                if (builder == null) continue;

                foreach (var section in builder.MapSections)
                {
                    var special = section.GetLayout(MapArrayLayouts.SPECIAL);
                    var enemy = section.GetLayout(MapArrayLayouts.ENEMY);
                    var diff = section.GetLayout(MapArrayLayouts.DIFFICULTY);
                    for (int sy = 0; sy < special.Length; sy++)
                    {
                        for (int sx = 0; sx < special[sy].Length; sx++)
                        {
                            if (special[sy][sx] != 1 && special[sy][sx] != 2) continue;
                            enemy[sy][sx] = special[sy][sx] + 1000; // 1 -> 1001, 2 -> 1002
                            // difficulty was already stored on the special tile, preserve it
                            // (diff[sy][sx] is already set; no change needed)
                            special[sy][sx] = -1;
                            migrated = true;
                        }
                    }
                }
            }

            if (migrated)
            {
                ChangeStates[mod] = true;
                SetSaveButtonStates();
            }
        }

        private void SetMapSectionSelections()
        {
            MapSectionSelection.SelectionChanged -= MapSectionSelection_SelectionChanged;
            MapSectionSelection.Items.Clear();

            if (ActiveMod == null)
            {
                MapSectionSelection.IsEnabled = false;
                SetMapSectionSelections(null);
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                return;
            }

            var target = SaveTargetSelection.SelectedItem as string ?? "map.json";
            var targetSections = LoadSectionsForTarget(ActiveMod.Name, target);


            // Update BuilderMods so SetMapSectionSelections(int) can find the sections
            MapBuilder builder = GetSelectedBuilder(ActiveMod);
            builder.MapSections = targetSections;

            foreach (var section in targetSections)
                MapSectionSelection.Items.Add(section.Id.ToString());
            MapSectionSelection.Items.Add("New");

            if (targetSections.Length != 0)
            {
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                MapSectionSelection.SelectedIndex = 0;
                SetMapSectionSelections(MapSectionSelection.SelectedIndex);
                return;
            }
            else
            {
                SetMapSectionSelections(null);
            }
            MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
        }
        private static MapSection[] LoadSectionsForTarget(string modName, string target)
        {
            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{modName}\{target}");
            if (!System.IO.File.Exists(file)) return [];
            try
            {
                var b = FileHelpers.Shared.Deserialize<MapBuilder>(file);
                return b?.MapSections ?? [];
            }
            catch { return []; }
        }

        private void SetMapSectionSelectionsOld()
        {
            MapSectionSelection.SelectionChanged -= MapSectionSelection_SelectionChanged;
            MapSectionSelection.Items.Clear();

            if (ActiveMod == null)
            {
                MapSectionSelection.IsEnabled = false;
                SetMapSectionSelections(null);
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                return;
            }
            var builder = GetSelectedBuilder(ActiveMod);// Wolfenstein.BuilderMods[ActiveMod.Name];
            foreach (var section in builder.MapSections)
                MapSectionSelection.Items.Add(section.Id.ToString());
            MapSectionSelection.Items.Add("New");

            if (builder.MapSections.Length != 0)
            {
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                MapSectionSelection.SelectedIndex = 0;
                SetMapSectionSelections(MapSectionSelection.SelectedIndex);
                return;
            }
            else
            {
                SetMapSectionSelections(null);
            }
            MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
        }

        private void SetMapSectionSelections(int? selection)
        {
            if (ActiveMod == null || selection == null)
            {
                ActiveSection = null;
                _mapBitmap = null;
                SectionImage.Source = null;
                SectionCanvas.Width = 0;
                SectionCanvas.Height = 0;
                return;
            }
            var builder = GetSelectedBuilder(ActiveMod);
            if (ActiveSection != null)
            {
                var index = Array.FindIndex(builder.MapSections, p => p.Id == ActiveSection.Id);
                if (index >= 0)
                    builder.MapSections[index].Layers = MapSection.Trim(ActiveSection);
            }
            var s = builder.MapSections.FirstOrDefault(p => p.Id == selection);
            if (s == null)
            {
                ActiveSection = null;
                _mapBitmap = null;
                SectionImage.Source = null;
                return;
            }
            MinLevelSld.Value = s.IntendedMinLevel;
            LevelLabel.Content = $"Level {s.IntendedMinLevel}";
            IsRotatableChk.Checked -= IsRotatableChk_Changed;
            IsRotatableChk.Unchecked -= IsRotatableChk_Changed;
            IsRotatableChk.IsChecked = s.IsRotatable;
            IsFlippableChk.IsChecked = s.IsFlippable;
            IsRotatableChk.Checked += IsRotatableChk_Changed;
            IsRotatableChk.Unchecked += IsRotatableChk_Changed;
            if (s == ActiveSection)
            {
                ActiveSection.Layers = MapSection.Expand(ActiveSection);
                return;
            }
            ActiveSection = s;
            ActiveSection.Layers = MapSection.Expand(ActiveSection);
            BuildMapBitmap();
        }
        private void MapSectionSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveMod == null) return;
            var builder = GetSelectedBuilder(ActiveMod); //Wolfenstein.BuilderMods[ActiveMod.Name];
            var m = MapSectionSelection.SelectedItem as string;
            if (string.IsNullOrEmpty(m))
            {
                SetMapSectionSelections(null);
            }
            else if (m == "New")
            {
                var section = new MapSection
                {
                    Id = builder.MapSections.Length > 0 ? builder.MapSections.Max(s => s.Id) + 1 : 0,
                    IntendedMinLevel = Math.Clamp((int)MinLevelSld.Value, 1, 100)
                };
                builder.MapSections = [.. builder.MapSections, section];
                ChangeStates[ActiveMod] = true;
                RefreshSectionDropdown(section.Id);
            }
            else
            {
                if (int.TryParse(m, out int s)) SetMapSectionSelections(s);
                else SetMapSectionSelections(null);
            }
            SetSaveButtonStates();
        }

        private static void CenterContent(ScrollViewer scrollViewer, FrameworkElement content)
        {
            double offsetX = (content.Width - scrollViewer.ViewportWidth) / 2;
            double offsetY = (content.Height - scrollViewer.ViewportHeight) / 2;
            scrollViewer.ScrollToHorizontalOffset(offsetX);
            scrollViewer.ScrollToVerticalOffset(offsetY);
        }

        // ──────────────────────────────────────────────────────────────
        // Palette grids
        // ──────────────────────────────────────────────────────────────

        private void SetDoorTextureImageGrid()
        {
            if (ActiveMod == null) return;
            DoorTextureImageGrid.Children.Clear();
            if (SelectedDoorIndex == -1 && Wolfenstein.Doors.Count > 0) SelectedDoorIndex = 0;
            for (int i = 0; i < Wolfenstein.Doors.Count; i++)
            {
                var j = i;
                Image imageControl = new()
                {
                    Source = GetDoorBitmap(j, DoorDirection.EAST_WEST),
                    ToolTip = Wolfenstein.Doors[j].MapID,
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedDoorIndex = j; SetDoorTextureImageGrid(); };
                DoorTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = j == SelectedDoorIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        private static string GetNameForSpecial(int i) => i switch
        {
            0 => "Player Start",
            3 => "Exit",
            4 => "Secret North",
            5 => "Secret East",
            6 => "Secret South",
            7 => "Secret West",
            8 => "Wall can be any",
            9 => "5% Chance",
            10 => "25% Chance",
            11 => "50% Chance",
            12 => "75% Chance",
            _ => "Unknown"
        };

        private void SetSpecialTextureImageGrid()
        {
            if (ActiveMod == null) return;
            SpecialTextureImageGrid.Children.Clear();
            if (SelectedSpecialIndex == -1 && Wolfenstein.Special.Count > 0) SelectedSpecialIndex = 0;
            for (int i = 0; i < Wolfenstein.Special.Count; i++)
            {
                if (i == 1 || i == 2) continue; // now live on the Enemy layer as 1001/1002
                var j = i;
                Image imageControl = new()
                {
                    Source = GetSpecialBitmap(j),
                    ToolTip = GetNameForSpecial(j),
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedSpecialIndex = j; SetSpecialTextureImageGrid(); };
                SpecialTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = j == SelectedSpecialIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        private void SetEnemyTextureImageGrid()
        {
            if (ActiveMod == null) return;
            EnemyTextureImageGrid.Children.Clear();
            if (SelectedEnemyIndex == -1 && Wolfenstein.Mods[ActiveMod.Name].Enemies.Length > 0) SelectedEnemyIndex = 0;

            // Virtual entries: Random Enemy (1001) and Experimental Enemy (1002)
            foreach (var (virtualId, label) in new[] { (1001, "Random Enemy"), (1002, "Experimental Enemy") })
            {
                var vid = virtualId;
                Image imageControl = new()
                {
                    Source = GetVirtualEnemyBitmap(vid),
                    ToolTip = label,
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedEnemyIndex = vid; SetEnemyTextureImageGrid(); };
                EnemyTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = vid == SelectedEnemyIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }

            for (int i = 0; i < Wolfenstein.Mods[ActiveMod.Name].Enemies.Length; i++)
            {
                var j = i;
                Image imageControl = new()
                {
                    Source = GetEnemyBitmap(ActiveMod.Name, Wolfenstein.Mods[ActiveMod.Name].Enemies[i].MapID),
                    ToolTip = Wolfenstein.Mods[ActiveMod.Name].Enemies[i].Name,
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedEnemyIndex = j; SetEnemyTextureImageGrid(); };
                EnemyTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = i == SelectedEnemyIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        private void SetDecalTextureImageGrid()
        {
            if (ActiveMod == null) return;
            DecalTextureImageGrid.Children.Clear();
            if (SelectedDecalIndex == -1 && Wolfenstein.Decals[ActiveMod.Name].Count > 0) SelectedDecalIndex = 0;
            for (int i = 0; i < Wolfenstein.Decals[ActiveMod.Name].Count; i++)
            {
                var j = i;
                Image imageControl = new()
                {
                    Source = GetDecalBitmap(ActiveMod.Name, i),
                    ToolTip = Wolfenstein.Mods[ActiveMod.Name].Decals[j].MapID,
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedDecalIndex = j; SetDecalTextureImageGrid(); };
                DecalTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = i == SelectedDecalIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        private void SetItemTextureImageGrid()
        {
            if (ActiveMod == null) return;
            ItemTextureImageGrid.Children.Clear();
            if (SelectedItemIndex == -1 && Wolfenstein.PickupItems.Count > 0) SelectedItemIndex = 0;
            for (int i = 0; i < Wolfenstein.PickupItemTypes.Count; i++)
            {
                var j = i;
                if (Wolfenstein.PickupItemTypes[i].ItemType == PickupItemType.SPAWNER) continue;
                Image imageControl = new()
                {
                    Source = GetItemBitmap(j),
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedItemIndex = j; SetItemTextureImageGrid(); };
                ItemTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = j == SelectedItemIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        private void SetWallTextureImageGrid()
        {
            if (ActiveMod == null) return;
            WallTextureImageGrid.Children.Clear();
            if (SelectedWallIndex == -1 && Wolfenstein.Textures[ActiveMod.Name].Count > 0) SelectedWallIndex = 0;
            for (int i = 0; i < ActiveMod.Textures.Length; i++)
            {
                var j = ActiveMod.Textures[i].MapID;
                Image imageControl = new()
                {
                    Source = GetTextureBitmap(ActiveMod.Name, j),
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) => { SelectedWallIndex = j; SetWallTextureImageGrid(); };
                WallTextureImageGrid.Children.Add(new Border()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = j == SelectedWallIndex ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Door/wall helpers
        // ──────────────────────────────────────────────────────────────

        internal bool IsDoorConnected(int x, int y, out List<(int x, int y)> where)
        {
            where = [];
            if (ActiveSection == null) return false;
            if (ActiveSection.Items[y][x] >= 0) return false;
            if (ActiveSection.Decals[y][x] >= 0) return false;
            if (ActiveSection.Enemy[y][x] >= 0) return false;
            if (ActiveSection.Doors[y][x] >= 0) return false;
            if (x - 1 > 0 && ActiveSection.Doors[y][x - 1] >= 0) where.Add((x - 1, y));
            if (y - 1 > 0 && ActiveSection.Doors[y - 1][x] >= 0) where.Add((x, y - 1));
            if (x + 1 < ActiveSection.Doors[y].Length && ActiveSection.Doors[y][x + 1] >= 0) where.Add((x + 1, y));
            if (y + 1 < ActiveSection.Doors.Length && ActiveSection.Doors[y + 1][x] >= 0) where.Add((x, y + 1));
            return where.Count > 0;
        }

        private bool IsWall(int x, int y)
        {
            if (ActiveSection == null) return false;
            if (x < 0 || y < 0 || x >= ActiveSection.Walls[y].Length || y >= ActiveSection.Walls.Length) return false;
            return ActiveSection.Walls[y][x] >= 0;
        }

        private bool IsDoorEastWest(int x, int y) => IsWall(x - 1, y) && IsWall(x + 1, y);
        private bool IsDoorNorthSouth(int x, int y) => IsWall(x, y - 1) && IsWall(x, y + 1);
        private DoorDirection IsDoorBetweenWalls(int x, int y)
        {
            bool ew = IsDoorEastWest(x, y);
            bool ns = IsDoorNorthSouth(x, y);
            if (ew && !ns) return DoorDirection.EAST_WEST;
            if (ns && !ew) return DoorDirection.NORTH_SOUTH;
            return DoorDirection.NONE;
        }

        // ──────────────────────────────────────────────────────────────
        // Difficulty
        // ──────────────────────────────────────────────────────────────

        private void DifficultySelection_SelectionChanged(object sender, SelectionChangedEventArgs e) => SelectedDifficulty = DifficultySelection.SelectedIndex;

        // ──────────────────────────────────────────────────────────────
        // Bitmap helpers
        // ──────────────────────────────────────────────────────────────

        private static WriteableBitmap GetBitmapRotate90(Texture32 t)
        {
            var nt = new Texture32(t.Height, t.Width);
            for (int y = 0; y < t.Height; y++)
                for (int x = 0; x < t.Width; x++)
                    nt.PutPixel(y, x, t.GetPixel(x, y));
            return GetBitmap(nt);
        }

        private static WriteableBitmap GetBitmap(Texture32 t)
        {
            var bmp = new WriteableBitmap(t.Width, t.Height, 96, 96, PixelFormats.Pbgra32, null);
            bmp.Lock();
            unsafe
            {
                byte* pBackBuffer = (byte*)bmp.BackBuffer;
                for (int i = 0; i < t.Pixels.Length; i += 4)
                {
                    pBackBuffer[i] = t.Pixels[i + 2];
                    pBackBuffer[i + 1] = t.Pixels[i + 1];
                    pBackBuffer[i + 2] = t.Pixels[i + 0];
                    pBackBuffer[i + 3] = t.Pixels[i + 3];
                }
            }
            bmp.AddDirtyRect(new Int32Rect(0, 0, t.Width, t.Height));
            bmp.Unlock();
            return bmp;
        }

        public WriteableBitmap? GetSpecialBitmap(string mod, int v, int w)
        {
            if (!Wolfenstein.Mods.ContainsKey(mod)) return null;
            var key = $"{mod}{v},{w}w";
            if (SpecialMapCache.TryGetValue(key, out var bmp)) return bmp;
            Texture32 wall;
            if (!Wolfenstein.Textures[mod].Any(p => p.Key == w))
            {
                wall = new Texture32(64, 64);
                wall.RectFill(0, 0, 64, 64, 128, 128, 128, 255);
            }
            else
            {
                wall = Wolfenstein.Textures[mod].FirstOrDefault(p => p.Key == w).Value;
            }
            return BlendSpecialTexture(wall, v, key);
        }

        private WriteableBitmap? BlendSpecialTexture(Texture32 wall, int v, string key)
        {
            Texture32 special;
            if (!Wolfenstein.Special.Any(p => p.Key == v))
            {
                special = new Texture32(64, 64);
                special.RectFill(0, 0, 64, 64, 0, 0, 0, 0);
            }
            else
            {
                special = Wolfenstein.Special.FirstOrDefault(p => p.Key == v).Value;
            }
            WriteableBitmap bmp = new(wall.Width, wall.Height, 96, 96, PixelFormats.Pbgra32, null);
            bmp.Lock();
            unsafe
            {
                byte* pBackBuffer = (byte*)bmp.BackBuffer;
                for (int i = 0; i < wall.Pixels.Length; i += 4)
                {
                    if (special.Pixels[i + 3] == 0)
                    {
                        pBackBuffer[i] = wall.Pixels[i + 2];
                        pBackBuffer[i + 1] = wall.Pixels[i + 1];
                        pBackBuffer[i + 2] = wall.Pixels[i + 0];
                        pBackBuffer[i + 3] = wall.Pixels[i + 3];
                    }
                    else
                    {
                        pBackBuffer[i] = special.Pixels[i + 2];
                        pBackBuffer[i + 1] = special.Pixels[i + 1];
                        pBackBuffer[i + 2] = special.Pixels[i + 0];
                        pBackBuffer[i + 3] = special.Pixels[i + 3];
                    }
                }
            }
            bmp.AddDirtyRect(new Int32Rect(0, 0, wall.Width, wall.Height));
            bmp.Unlock();
            SpecialMapCache.Add(key, bmp);
            return bmp;
        }

        internal WriteableBitmap? GetSpecialBitmapItem(string mod, int v, int i)
        {
            if (!Wolfenstein.Mods.ContainsKey(mod)) return null;
            if (!Wolfenstein.PickupItems.TryGetValue(i, out Texture32? value)) return null;
            var key = $"{mod}{v},{i}i";
            if (SpecialMapCache.TryGetValue(key, out var bmp)) return bmp;
            return BlendSpecialTexture(value, v, key);
        }

        internal WriteableBitmap? GetSpecialBitmapEnemy(string mod, int v, int e)
        {
            if (!Wolfenstein.Mods.TryGetValue(mod, out Mod? value)) return null;
            if (!value.Enemies.Any(p => p.MapID == e)) return null;
            var enemy = value.Enemies.FirstOrDefault(p => p.MapID == e);
            if (enemy == null) return null;
            var t = Wolfenstein.CharacterSprites[mod][enemy.MapID].GetTexture(0);
            var key = $"{mod}{v},{e}e";
            if (SpecialMapCache.TryGetValue(key, out var bmp)) return bmp;
            return BlendSpecialTexture(t, v, key);
        }

        internal WriteableBitmap? GetSpecialBitmapDecal(string mod, int v, int d)
        {
            if (!Wolfenstein.Mods.ContainsKey(mod)) return null;
            if (!Wolfenstein.Decals[mod].Any(p => p.Key == d)) return null;
            var value = Wolfenstein.Decals[mod].FirstOrDefault(p => p.Key == d).Value;
            var key = $"{mod}{v},{d}d";
            if (SpecialMapCache.TryGetValue(key, out var bmp)) return bmp;
            return BlendSpecialTexture(value, v, key);
        }

        public WriteableBitmap? GetTextureBitmap(string mod, int v)
        {
            if (!Wolfenstein.Mods.ContainsKey(mod)) return null;
            var key = $"{mod}{v}";
            if (TextureCache.TryGetValue(key, out var bmp)) return bmp;
            if (!Wolfenstein.Textures[mod].Any(p => p.Key == v)) return null;
            var t = Wolfenstein.Textures[mod].FirstOrDefault(p => p.Key == v).Value;
            bmp = GetBitmap(t);
            TextureCache.Add(key, bmp);
            return bmp;
        }

        public Direction GetDecalDirection(string mod, int v)
        {
            if (!Wolfenstein.Mods.TryGetValue(mod, out Mod? value)) return Direction.NONE;
            var key = $"{mod}{v}";
            if (DecalDirectionCache.TryGetValue(key, out var dir)) return dir;
            if (!value.Decals.Any(p => p.MapID == v)) return DecalDirectionCache[key] = Direction.NONE;
            var decal = value.Decals.FirstOrDefault(p => p.MapID == v);
            if (decal == null) return DecalDirectionCache[key] = Direction.NONE;
            return DecalDirectionCache[key] = decal.Direction;
        }

        public WriteableBitmap? GetDecalBitmap(string mod, int v)
        {
            if (!Wolfenstein.Mods.ContainsKey(mod)) return null;
            var key = $"{mod}{v}";
            if (DecalCache.TryGetValue(key, out var bmp)) return bmp;
            if (!Wolfenstein.Decals[mod].Any(p => p.Key == v)) return null;
            var t = Wolfenstein.Decals[mod].FirstOrDefault(p => p.Key == v).Value;
            bmp = GetBitmap(t);
            DecalCache.Add(key, bmp);
            return bmp;
        }

        public WriteableBitmap? GetItemBitmap(int v)
        {
            if (ItermCache.TryGetValue(v, out var bmp)) return bmp;
            if (!Wolfenstein.PickupItems.TryGetValue(v, out Texture32? value)) return null;
            bmp = GetBitmap(value);
            ItermCache.Add(v, bmp);
            return bmp;
        }

        public WriteableBitmap? GetEnemyBitmap(string mod, int v)
        {
            if (!Wolfenstein.Mods.TryGetValue(mod, out Mod? value)) return null;
            var key = $"{mod}{v}";
            if (EnemyCache.TryGetValue(key, out var bmp)) return bmp;
            if (!value.Enemies.Any(p => p.MapID == v)) return null;
            var e = value.Enemies.FirstOrDefault(p => p.MapID == v);
            if (e == null) return null;
            var t = Wolfenstein.CharacterSprites[mod][e.MapID].GetTexture(0);
            bmp = GetBitmap(t);
            EnemyCache.Add(key, bmp);
            return bmp;
        }

        public WriteableBitmap? GetSpecialBitmap(int v)
        {
            if (SpecialCache.TryGetValue(v, out var bmp)) return bmp;
            if (!Wolfenstein.Special.TryGetValue(v, out Texture32? value)) return null;
            bmp = GetBitmap(value);
            SpecialCache.Add(v, bmp);
            return bmp;
        }

        public WriteableBitmap? GetDoorBitmap(int v, DoorDirection dir)
        {
            var k = $"{(int)dir},{v}";
            if (DoorCache.TryGetValue(k, out var bmp)) return bmp;
            if (!Wolfenstein.Doors.TryGetValue(v, out DoorType? value)) return null;
            bmp = dir == DoorDirection.NORTH_SOUTH ? GetBitmapRotate90(value.DoorTexture) : GetBitmap(value.DoorTexture);
            DoorCache.Add(k, bmp);
            return bmp;
        }

        // ──────────────────────────────────────────────────────────────
        // Save
        // ──────────────────────────────────────────────────────────────

        private void SetSaveButtonStates()
        {
            SaveBtn.IsEnabled = ActiveMod != null && ChangeStates[ActiveMod];
            SaveAllBtn.IsEnabled = ChangeStates.Any(p => p.Value);
            MinLevelSld.IsEnabled = IsRotatableChk.IsEnabled = TestBtn.IsEnabled = DeleteBtn.IsEnabled = DuplicateBtn.IsEnabled = ActiveSection != null;
        }
        public MapBuilder GetSelectedBuilder(Mod mod)
        {
            MapBuilder builder;
            var target = SaveTargetSelection.SelectedItem as string ?? "map.json";
            if (target == "maptestlevel.json")
            {
                if (!Wolfenstein.TestMaps.ContainsKey(mod.Name))
                    Wolfenstein.TestMaps.Add(mod.Name, new MapBuilder());
                builder = Wolfenstein.TestMaps[mod.Name];
            }
            else if (target == "specialmap.json")
            {
                if (!Wolfenstein.SpecialMaps.ContainsKey(mod.Name))
                    Wolfenstein.SpecialMaps.Add(mod.Name, new MapBuilder());
                builder = Wolfenstein.SpecialMaps[mod.Name];
            }
            else
            {
                if (!Wolfenstein.BuilderMods.ContainsKey(mod.Name))
                    Wolfenstein.BuilderMods.Add(mod.Name, new MapBuilder());
                builder = Wolfenstein.BuilderMods[mod.Name];
            }
            return builder;
        }
        private bool Save(Mod mod, out string[] errors)
        {
            errors = [];
            if (mod == null) return false;
            MapBuilder builder = GetSelectedBuilder(mod);
            foreach (var s in builder.MapSections)
                s.Layers = MapSection.Trim(s);
            var target = SaveTargetSelection.SelectedItem as string ?? "map.json";
            if (target == "map.json" && !builder.Validate(out errors))
                return false;
            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{mod.Name}\{target}");
            try
            {
                if (!FileHelpers.Shared.Serialize(builder, file)) throw new Exception();
                ChangeStates[mod] = false;
                if (ActiveSection != null) ActiveSection.Layers = MapSection.Expand(ActiveSection);

                // Always reload the base mod (map pool, textures, etc.)
                Wolfenstein.ReloadMod(mod.Name, [target]);

                return true;
            }
            catch
            {
                Logger.GetLogger(mod).Log($"Having an issue with {file}");
                return false;
            }
            finally
            {
                SetSaveButtonStates();
            }
        }
        private bool SaveOld(Mod mod, out string[] errors)
        {
            errors = [];
            if (mod == null) return false;
            var builder = Wolfenstein.BuilderMods[mod.Name];
            foreach (var s in builder.MapSections)
                s.Layers = MapSection.Trim(s);

            var target = SaveTargetSelection.SelectedItem as string ?? "map.json";
            if (!(target == "maptestlevel.json" || target == "specialmap.json"))
                if (!builder.Validate(out errors)) return false;

            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{mod.Name}\{target}");
            try
            {
                if (!FileHelpers.Shared.Serialize(builder, file)) throw new Exception();
                ChangeStates[mod] = false;
                if (ActiveSection != null) ActiveSection.Layers = MapSection.Expand(ActiveSection);

                // Always reload the base mod (map pool, textures, etc.)
                Wolfenstein.ReloadMod(mod.Name, [target]);

                return true;
            }
            catch
            {
                Logger.GetLogger(mod).Log($"Having an issue with {file}");
                return false;
            }
            finally
            {
                SetSaveButtonStates();
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod != null && Save(ActiveMod, out var _))
                SetSaveButtonStates();
            else
                MessageBox.Show("Unable to save mod changes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void SaveAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var v in ChangeStates.Keys)
            {
                if (!ChangeStates[v]) continue;
                ChangeStates[v] = !Save(v, out var _);
            }
            if (ChangeStates.Any(p => p.Value))
                MessageBox.Show("Unable to save all mod changes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SetSaveButtonStates();
        }

        // ──────────────────────────────────────────────────────────────
        // Other buttons
        // ──────────────────────────────────────────────────────────────

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod == null || ActiveSection == null) return;
            var builder = GetSelectedBuilder(ActiveMod);// Wolfenstein.BuilderMods[ActiveMod.Name];
            builder.MapSections = [.. builder.MapSections.Where(p => p.Id != ActiveSection.Id)];
            ChangeStates[ActiveMod] = true;
            RefreshSectionDropdown();
        }

        private void RefreshSectionDropdown(int? selectId = null)
        {
            if (ActiveMod == null) return;
            var builder = GetSelectedBuilder(ActiveMod);// Wolfenstein.BuilderMods[ActiveMod.Name];

            MapSectionSelection.SelectionChanged -= MapSectionSelection_SelectionChanged;
            MapSectionSelection.Items.Clear();
            foreach (var section in builder.MapSections)
                MapSectionSelection.Items.Add(section.Id.ToString());
            MapSectionSelection.Items.Add("New");

            if (builder.MapSections.Length > 0)
            {
                var targetId = selectId ?? builder.MapSections[0].Id;
                var idx = MapSectionSelection.Items.IndexOf(targetId.ToString());
                MapSectionSelection.SelectedIndex = idx >= 0 ? idx : 0;
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                SetMapSectionSelections(builder.MapSections[
                    MapSectionSelection.SelectedIndex < builder.MapSections.Length
                        ? MapSectionSelection.SelectedIndex : 0].Id);
            }
            else
            {
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                SetMapSectionSelections(null);
            }
            SetSaveButtonStates();
        }
        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod == null || ActiveSection == null) return;
            int[][]? area = ActiveSection.GetClosedSection(out bool closed, out bool noDoors, out bool multiple);
            MessageBox.Show($"null:{area == null} Closed: {closed} NoDoors: {noDoors} Multiple: {multiple}");
        }

        private void MinLevelSld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ActiveSection == null || ActiveMod == null) return;
            ActiveSection.IntendedMinLevel = Math.Clamp((int)MinLevelSld.Value, 1, 100);
            LevelLabel.Content = $"Level {ActiveSection.IntendedMinLevel}";
            ChangeStates[ActiveMod] = true;
            SetSaveButtonStates();
        }

        private void DuplicateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod == null || ActiveSection == null) return;
            var builder = GetSelectedBuilder(ActiveMod);// Wolfenstein.BuilderMods[ActiveMod.Name];
            var target = SaveTargetSelection.SelectedItem as string ?? "map.json";

            // Always derive the canonical section list from the correct file,
            // not from whatever BuilderMods.MapSections currently holds —
            // that gets swapped out when SaveTargetSelection changes.
            var currentSections = LoadSectionsForTarget(ActiveMod.Name, target);

            // Guard: ActiveSection must actually belong to the current target.
            // If the user switched targets without saving, ActiveSection is stale.
            if (!currentSections.Any(s => s.Id == ActiveSection.Id))
            {
                MessageBox.Show(
                    "Switch to the section's file before duplicating.",
                    "Wrong target", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var section = ActiveSection.Clone();
            section.Id = currentSections.Length > 0
                ? currentSections.Max(s => s.Id) + 1
                : 0;

            builder.MapSections = [.. currentSections, section];
            ChangeStates[ActiveMod] = true;
            RefreshSectionDropdown(section.Id);
        }
        private void CurrentMapView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // zoom reserved for later
        }

        private void IsRotatableChk_Changed(object sender, RoutedEventArgs e)
        {
            if (ActiveSection == null || ActiveMod == null) return;
            ActiveSection.IsRotatable = IsRotatableChk.IsChecked == true;
            ChangeStates[ActiveMod] = true;
            SetSaveButtonStates();
        }

        private void BtnIncLevel_Click(object sender, RoutedEventArgs e) => MinLevelSld.Value = Math.Clamp((int)MinLevelSld.Value + 1, 1, 100);

        private void BtnDecLevel_Click(object sender, RoutedEventArgs e) => MinLevelSld.Value = Math.Clamp((int)MinLevelSld.Value - 1, 1, 100);

        private void IsFlippableChk_Changed(object sender, RoutedEventArgs e)
        {
            if (ActiveSection == null || ActiveMod == null) return;
            ActiveSection.IsFlippable = IsFlippableChk.IsChecked == true;
            ChangeStates[ActiveMod] = true;
            SetSaveButtonStates();
        }
    }
}