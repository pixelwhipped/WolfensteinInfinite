using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.Util;
using WolfensteinInfinite.WolfMod;
using static SFML.Window.Keyboard;
using static WolfensteinInfinite.Editor.MapEditor;

namespace WolfensteinInfinite.Editor
{
    /// <summary>
    /// Interaction logic for MapEditor.xaml
    /// </summary
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

        private bool HandleMapTryRemoveItem(SectionButton item, int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            item.SetItemTexture(-1, string.Empty);
            ActiveSection.Items[y][x] = -1;
            ActiveSection.Difficulty[y][x] = 0;
            changed = true;
            return true;
        }
        private bool HandleMapTryRemoveEnemy(SectionButton item, int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            item.SetItemTexture(-1, string.Empty);
            ActiveSection.Enemy[y][x] = -1;
            ActiveSection.Difficulty[y][x] = 0;
            changed = true;
            return true;
        }
        private bool HandleMapTryRemoveDecals(SectionButton item, int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            item.SetItemTexture(-1, string.Empty);
            ActiveSection.Decals[y][x] = -1;
            changed = true;
            return true;
        }
        private bool HandleMapTryRemoveDoors(SectionButton item, int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            item.SetDoorTexture(-1, DoorDirection.NONE);
            ActiveSection.Doors[y][x] = -1;
            changed = true;
            return true;
        }
        private bool HandleMapTryRemoveSpecial(SectionButton item, int x, int y, ref bool changed)
        {
            if (ActiveSection == null) return false;
            ActiveSection.Special[y][x] = -1;
            if (ActiveSection.Walls[y][x] >= 0) item.SetWallTexture(ActiveSection.Walls[y][x]);
            else if (ActiveSection.Decals[y][x] >= 0)
            {
                item.SetDecalTexture(ActiveSection.Decals[y][x], item.Details.Text);
            }
            else if (ActiveSection.Items[y][x] >= 0)
            {
                item.SetItemTexture(ActiveSection.Items[y][x], item.Details.Text);
            }
            else if (ActiveSection.Enemy[y][x] >= 0)
            {
                item.SetEnemyTexture(ActiveSection.Enemy[y][x], item.Details.Text);
            }
            else item.Clear();
            changed = true;
            return true;
        }
        private bool HandleMapTryRemoveWall(SectionButton item, int x, int y, ref bool changed)
        {
            //Handle special and doors
            if (ActiveSection == null) return false;
            if (ActiveSection.Special[y][x] >= 0) HandleMapTryRemoveSpecial(item, x, y, ref changed);
            if (IsDoorConnected(x, y, out List<(int x, int y)> where))
            {
                foreach (var d in where)
                {
                    var i = GetMapButton(d.x, d.y);
                    if (i != null) HandleMapTryRemoveDoors(i, x, y, ref changed);
                }
            }
            item.SetWallTexture(-1);
            ActiveSection.Walls[y][x] = -1;
            changed = true;
            return true;
        }
        private delegate bool TryRemoveDelegate(ref bool chanaged);

        private void HandleMapClick(SectionButton item, int x, int y, Point insidePosition, bool IsLeft, ref bool changed)
        {
            if (ActiveSection == null) return;
            if (ActiveMod == null) return;
            bool IsApplyingChance()
            {
                return IsLeft && LayerControl.SelectedItem == LayerSpecialControl && SelectedSpecialIndex >= 9 && SelectedSpecialIndex <= 12;
            }
            var removers = new Dictionary<TabItem, TryRemoveDelegate>()
            {
                [LayerWallsControl] = (ref bool c) => LayerControl.SelectedItem == LayerSpecialControl ? false : ActiveSection.Walls[y][x] >= 0 && !HandleMapTryRemoveWall(item, x, y, ref c),
                [LayerDecalsControl] = (ref bool c) => ActiveSection.Decals[y][x] >= 0 && !IsApplyingChance() && !HandleMapTryRemoveDecals(item, x, y, ref c),
                [LayerItemsControl] = (ref bool c) => ActiveSection.Items[y][x] >= 0 && !IsApplyingChance()  && !HandleMapTryRemoveItem(item, x, y, ref c),
                [LayerDoorControl] = (ref bool c) => ActiveSection.Doors[y][x] >= 0 && !HandleMapTryRemoveDoors(item, x, y, ref c),
                [LayerEnemiesControl] = (ref bool c) => ActiveSection.Enemy[y][x] >= 0 && !IsApplyingChance() && !HandleMapTryRemoveEnemy(item, x, y, ref c),
                [LayerSpecialControl] = (ref bool c) => ActiveSection.Special[y][x] >= 0 && !HandleMapTryRemoveSpecial(item, x, y, ref c)
            };
            if (IsLeft)//Handle normal
            {
                foreach (var r in removers.Where(p => p.Key != LayerControl.SelectedItem))
                    if (r.Value(ref changed)) return;
                if (LayerControl.SelectedItem == LayerWallsControl)
                {
                    ActiveSection.Walls[y][x] = SelectedWallIndex;
                    item.SetWallTexture(SelectedWallIndex);
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerDecalsControl)
                {
                    var dir = GetDecalDirection(ActiveMod.Name, SelectedDecalIndex);
                    item.SetDecalTexture(SelectedDecalIndex, dir == Direction.NONE ? string.Empty : dir.ToString());
                    ActiveSection.Decals[y][x] = SelectedDecalIndex;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerItemsControl)
                {
                    item.SetItemTexture(SelectedItemIndex, $"ID:{SelectedItemIndex}");
                    ActiveSection.Items[y][x] = SelectedItemIndex;
                    ActiveSection.Difficulty[y][x] = SelectedDifficulty;
                    changed = true;
                }
                else if (LayerControl.SelectedItem == LayerDoorControl)
                {
                    var dir = IsDoorBetweenWalls(x, y);
                    if (dir == DoorDirection.NONE) return;
                    item.SetDoorTexture(SelectedDoorIndex, dir);
                    ActiveSection.Doors[y][x] = SelectedDoorIndex;
                    changed = true;

                }
                else if (LayerControl.SelectedItem == LayerEnemiesControl)
                {
                    var diff = SelectedEnemyIndex < 0 ? 0 : SelectedDifficulty;
                    item.SetEnemyTexture(SelectedEnemyIndex < 0 ? -1 : Wolfenstein.Mods[ActiveMod.Name].Enemies[SelectedEnemyIndex].MapID, $"D:{diff}");
                    ActiveSection.Enemy[y][x] = SelectedEnemyIndex < 0 ? -1 : Wolfenstein.Mods[ActiveMod.Name].Enemies[SelectedEnemyIndex].MapID;
                    ActiveSection.Difficulty[y][x] = diff;
                }
                else if (LayerControl.SelectedItem == LayerSpecialControl)
                {
                    if (ActiveSection.Walls[y][x] >= 0 && SelectedSpecialIndex < 3) { return; }
                    if (ActiveSection.Walls[y][x] < 0 && SelectedSpecialIndex >= 3)
                    {
                        if (SelectedSpecialIndex >= 9 && SelectedSpecialIndex <= 12)
                        {
                            if (ActiveSection.Items[y][x] >= 0)
                            {
                                ActiveSection.Special[y][x] = SelectedSpecialIndex;
                                item.SetSpecialItems(SelectedSpecialIndex, ActiveSection.Items[y][x]);
                                changed = true;
                            }
                            else if (ActiveSection.Enemy[y][x] >= 0)
                            {
                                ActiveSection.Special[y][x] = SelectedSpecialIndex;
                                item.SetSpecialEnemy(SelectedSpecialIndex, ActiveSection.Enemy[y][x]);
                                changed = true;
                            }
                            else if (ActiveSection.Decals[y][x] >= 0)
                            {
                                ActiveSection.Special[y][x] = SelectedSpecialIndex;
                                item.SetSpecialDecal(SelectedSpecialIndex, ActiveSection.Decals[y][x]);
                                changed = true;
                            }

                            return;
                        }
                    }
                    if (ActiveSection.Doors[y][x] >= 0) return;
                    if (SelectedSpecialIndex == 0) //check for multiple player starts
                    {
                        for (int sy = 0; sy < ActiveSection.Special.Length; sy++)
                        {
                            for (int sx = 0; sx < ActiveSection.Special[sy].Length; sx++)
                            {
                                if (sx == x && sy == y) continue;
                                if (ActiveSection.Special[sy][sx] == 0)
                                {
                                    GetMapButton(sx, sy)?.Clear();
                                    sy = ActiveSection.Special.Length;
                                    break;
                                }
                            }
                        }
                    }
                    ActiveSection.Special[y][x] = SelectedSpecialIndex;
                    if (SelectedSpecialIndex >= 9 && SelectedSpecialIndex <= 12)
                    {
                        if (ActiveSection.Items[y][x] >= 0)
                        {
                            item.SetSpecialItems(SelectedSpecialIndex, ActiveSection.Items[y][x]);
                        }
                        else if (ActiveSection.Enemy[y][x] >= 0)
                        {
                            item.SetSpecialEnemy(SelectedSpecialIndex, ActiveSection.Enemy[y][x]);
                        }
                        else if (ActiveSection.Decals[y][x] >= 0)
                        {
                            item.SetSpecialDecal(SelectedSpecialIndex, ActiveSection.Decals[y][x]);
                        }
                        return;
                    }
                    else
                    {

                        item.SetSpecialTexture(SelectedSpecialIndex, ActiveSection.Walls[y][x]);
                    }
                    changed = true;

                }

            }
            else if (LayerControl.SelectedItem is TabItem t && removers.TryGetValue(t, out var tryRemove)) tryRemove(ref changed);

        }
        internal void MapClick(int x, int y, Point insidePosition, bool IsLeft)
        {
            bool changed = false;
            if (ActiveSection == null) return;
            if (ActiveMod == null) return;
            var item = GetMapButton(x, y);
            if (item == null) return;
            HandleMapClick(item, x, y, insidePosition, IsLeft, ref changed);

            if (changed)
            {
                ChangeStates[ActiveMod] = true;
                SetSaveButtonStates();
            }
            return;
        }
        public MapEditor(Wolfenstein wolfenstein)
        {
            Wolfenstein = wolfenstein;
            InitializeComponent();
            InitializeOptions();

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
                DifficultySelection.Items.Add(DifficultyHelpers.GetDifficultyString(difficulty));
            }
            DifficultySelection.SelectedIndex = 0;
            SetSaveButtonStates();
        }
        private static bool ValidateMod(Mod mod) => mod.Textures.Length > 0;

        private void ModSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var m = ModSelection.SelectedItem as string;
            if (string.IsNullOrEmpty(m)) return;
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
            SetMapSectionSelections();
            SetWallTextureImageGrid();
            SetDecalTextureImageGrid();
            SetItemTextureImageGrid();
            SetEnemyTextureImageGrid();
            SetDoorTextureImageGrid();
            SetSpecialTextureImageGrid();
        }

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
                    Stretch = Stretch.None // Adjust as needed
                };

                // Attach a click event handler for selection
                imageControl.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedDoorIndex = j;
                    SetDoorTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (j == SelectedDoorIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                DoorTextureImageGrid.Children.Add(border);
            }
        }

        private string GetNameForSpecial(int i)
        {
            switch (i)
            {
                case 0: return "Player Start";
                case 1: return "Random Enemy";
                case 2: return "Experiment Enemy";
                case 3: return "Exit";
                case 4: return "Secret North";
                case 5: return "Secret East";
                case 6: return "Secret South";
                case 7: return "Secret West";
                case 8: return "Wall can be any";
                case 9: return "5% Chance";
                case 10: return "25% Chance";
                case 11: return "50% Chance";
                case 12: return "75% Chance";
                default: return "Unknown";
            }
        }
        private void SetSpecialTextureImageGrid()
        {
            if (ActiveMod == null) return;
            SpecialTextureImageGrid.Children.Clear();
            if (SelectedSpecialIndex == -1 && Wolfenstein.Special.Count > 0) SelectedSpecialIndex = 0;
            for (int i = 0; i < Wolfenstein.Special.Count; i++)
            {
                var j = i;
                Image imageControl = new()
                {
                    Source = GetSpecialBitmap(j),
                    ToolTip = GetNameForSpecial(j),
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedSpecialIndex = j;
                    SetSpecialTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (j == SelectedSpecialIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                SpecialTextureImageGrid.Children.Add(border);
            }
        }
        private void SetEnemyTextureImageGrid()
        {
            if (ActiveMod == null) return;
            EnemyTextureImageGrid.Children.Clear();
            if (SelectedEnemyIndex == -1 && Wolfenstein.Mods[ActiveMod.Name].Enemies.Length > 0) SelectedEnemyIndex = 0;
            for (int i = 0; i < Wolfenstein.Mods[ActiveMod.Name].Enemies.Length; i++)
            {
                var j = i;
                Image imageControl = new()
                {
                    Source = GetEnemyBitmap(ActiveMod.Name, Wolfenstein.Mods[ActiveMod.Name].Enemies[i].MapID),
                    ToolTip = Wolfenstein.Mods[ActiveMod.Name].Enemies[i].Name,
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedEnemyIndex = j;
                    SetEnemyTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (i == SelectedEnemyIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                EnemyTextureImageGrid.Children.Add(border);
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
                imageControl.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedDecalIndex = j;
                    SetDecalTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (i == SelectedDecalIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                DecalTextureImageGrid.Children.Add(border);
            }
        }
        private void SetItemTextureImageGrid()
        {
            if (ActiveMod == null) return;
            ItemTextureImageGrid.Children.Clear();
            if (SelectedItemIndex == -1 && Wolfenstein.PickupItems.Count > 0) SelectedItemIndex = 0;
            for (int i = 0; i < Wolfenstein.PickupItems.Count; i++)
            {
                var j = i;
                if (Wolfenstein.PickupItemTypes[i].ItemType == PickupItemType.SPAWNER) continue;
                Image imageControl = new()
                {
                    Source = GetItemBitmap(j),
                    Stretch = Stretch.None
                };
                imageControl.MouseLeftButtonUp += (s, e) =>

                {
                    SelectedItemIndex = j;
                    SetItemTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (j == SelectedItemIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                ItemTextureImageGrid.Children.Add(border);
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
                imageControl.MouseLeftButtonUp += (s, e) =>
                {
                    SelectedWallIndex = j;
                    SetWallTextureImageGrid();
                };
                Border border = new()
                {
                    BorderThickness = new Thickness(5),
                    BorderBrush = (j == SelectedWallIndex) ? Brushes.Blue : Brushes.Gray,
                    Child = imageControl
                };
                WallTextureImageGrid.Children.Add(border);
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
            var builder = Wolfenstein.BuilderMods[ActiveMod.Name];
            foreach (var section in builder.MapSections)
            {
                MapSectionSelection.Items.Add(section.Id.ToString());
            }
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
                SectionGrid.Children.Clear();
                return;
            }
            var builder = Wolfenstein.BuilderMods[ActiveMod.Name];
            if (ActiveSection != null)
            {

                var index = Array.FindIndex(builder.MapSections, p => p.Id == ActiveSection.Id);
                if (index >= 0)
                {
                    builder.MapSections[index].Layers = MapSection.Trim(ActiveSection);  //Trim when not editing
                }
            }
            var s = builder.MapSections.FirstOrDefault(p => p.Id == selection);
            if (s == null)
            {
                ActiveSection = null;
                SectionGrid.Children.Clear();
                return;
            }
            MinLevelSld.Value = s.IntendedMinLevel;
            if (s == ActiveSection)
            {
                ActiveSection.Layers = MapSection.Expand(ActiveSection);
                return;
            }
            ;
            ActiveSection = s;
            ActiveSection.Layers = MapSection.Expand(ActiveSection);  //Expand for editing
            SectionGrid.Children.Clear();
            SectionGrid.Width = 4096;
            SectionGrid.Height = 4096;
            for (int y = 0; y < ActiveSection.Walls.Length; y++)
                SectionGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(64) });
            for (int y = 0; y < ActiveSection.Walls[0].Length; y++)
                SectionGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(64) });

            for (int y = 0; y < ActiveSection.Walls.Length; y++)
            {
                for (int x = 0; x < ActiveSection.Walls[0].Length; x++)
                {
                    var child = new SectionButton(this, x, y);
                    if (ActiveSection.Decals[y][x] >= 0)
                    {
                        var dir = GetDecalDirection(ActiveMod.Name, ActiveSection.Decals[y][x]);
                        child.SetDecalTexture(ActiveSection.Decals[y][x], dir == Direction.NONE ? string.Empty : dir.ToString());
                    }
                    else if (ActiveSection.Items[y][x] >= 0)
                    {
                        var diff = ActiveSection.Difficulty[y][x];
                        child.SetItemTexture(ActiveSection.Items[y][x], diff > 0 ? $"D:{diff}" : string.Empty);
                    }
                    else if (ActiveSection.Enemy[y][x] >= 0)
                    {
                        var diff = ActiveSection.Difficulty[y][x];
                        child.SetEnemyTexture(ActiveSection.Items[y][x], diff > 0 ? $"D:{diff}" : string.Empty);
                    }
                    else if (ActiveSection.Doors[y][x] >= 0)
                    {
                        var dir = IsDoorBetweenWalls(x, y);
                        child.SetDoorTexture(ActiveSection.Doors[y][x], dir);
                    }
                    else if (ActiveSection.Walls[y][x] >= 0)
                    {
                        child.SetWallTexture(ActiveSection.Walls[y][x]);
                    }

                    if (ActiveSection.Special[y][x] >= 0)
                    {
                        if (ActiveSection.Special[y][x] >= 9 && ActiveSection.Special[y][x] <= 12)
                        {
                            if (ActiveSection.Decals[y][x] >= 0)
                            {
                                child.SetSpecialDecal(ActiveSection.Special[y][x], ActiveSection.Decals[y][x]);
                            }else if (ActiveSection.Items[y][x] >= 0)
                            {
                                child.SetSpecialItems(ActiveSection.Special[y][x], ActiveSection.Items[y][x]);
                            }
                            else if (ActiveSection.Enemy[y][x] >= 0)
                            {
                                child.SetSpecialEnemy(ActiveSection.Special[y][x], ActiveSection.Enemy[y][x]);
                            }
                        }
                        else
                        {
                            child.SetSpecialTexture(ActiveSection.Special[y][x], ActiveSection.Walls[y][x]);
                        }
                    }

                    Grid.SetRow(child, y);
                    Grid.SetColumn(child, x);
                    SectionGrid.Children.Add(child);
                }
            }
            SectionGrid.UpdateLayout();
            CenterContent(CurrentMapView, SectionGrid);

        }
        private static void CenterContent(ScrollViewer scrollViewer, FrameworkElement content)
        {
            double offsetX = (content.Width - scrollViewer.ViewportWidth) / 2;
            double offsetY = (content.Height - scrollViewer.ViewportHeight) / 2;

            scrollViewer.ScrollToHorizontalOffset(offsetX);
            scrollViewer.ScrollToVerticalOffset(offsetY);
        }
        private void MapSectionSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActiveMod == null) return;
            var builder = Wolfenstein.BuilderMods[ActiveMod.Name];
            var m = MapSectionSelection.SelectedItem as string;
            if (string.IsNullOrEmpty(m))
            {
                SetMapSectionSelections(null);
            }
            else if (m == "New")
            {
                MapSectionSelection.SelectionChanged -= MapSectionSelection_SelectionChanged;
                var section = new MapSection()
                {
                    Id = builder.MapSections.Length
                };
                MapSectionSelection.Items.Insert(MapSectionSelection.Items.IndexOf("New"), section.Id.ToString());
                MapSectionSelection.SelectedIndex = MapSectionSelection.Items.IndexOf(section.Id.ToString());
                MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
                builder.MapSections = [.. builder.MapSections, section];
                SetMapSectionSelections(section.Id);
            }
            else
            {
                if (int.TryParse(m, out int s)) SetMapSectionSelections(s);
                else SetMapSectionSelections(null);
            }
            SetSaveButtonStates();
        }
        private void DifficultySelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dStr = DifficultySelection.SelectedItem.ToString() ?? DifficultyHelpers.GetDifficultyString(Difficulties.CAN_I_PLAY_DADDY);
            SelectedDifficulty = (int)(DifficultyHelpers.GetStringDifficulty(dStr) ?? Difficulties.CAN_I_PLAY_DADDY);
        }
        private SectionButton? GetMapButton(int x, int y)
        {
            foreach (UIElement child in SectionGrid.Children)
            {
                // Check if the child is in the desired row and column
                if (Grid.GetRow(child) == y && Grid.GetColumn(child) == x)
                {
                    return child as SectionButton;
                }
            }
            return null;
        }
        internal bool IsDoorConnected(int x, int y, out List<(int x, int y)> where)
        {
            where = new List<(int, int)>();
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



        private static WriteableBitmap GetBitmapRotate90(Texture32 t)
        {
            var nt = new Texture32(t.Height, t.Width);
            for (int y = 0; y < t.Height; y++)
            {
                for (int x = 0; x < t.Width; x++)
                {
                    nt.PutPixel(y, x, t.GetPixel(x, y));
                }
            }
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
            bmp.AddDirtyRect(new Int32Rect(0, 0, t.Width, t.Height)); // Mark the entire bitmap as dirty
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
            /*
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

            bmp = new WriteableBitmap(wall.Width, wall.Height, 96, 96, PixelFormats.Pbgra32, null);
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
            bmp.AddDirtyRect(new Int32Rect(0, 0, wall.Width, wall.Height)); // Mark the entire bitmap as dirty
            bmp.Unlock();
            SpecialMapCache.Add(key, bmp);*/
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

            WriteableBitmap bmp = new WriteableBitmap(wall.Width, wall.Height, 96, 96, PixelFormats.Pbgra32, null);
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
            bmp.AddDirtyRect(new Int32Rect(0, 0, wall.Width, wall.Height)); // Mark the entire bitmap as dirty
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
            var t = value;
            bmp = GetBitmap(t);
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
            var t = value;
            bmp = GetBitmap(t);
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
        private void CurrentMapView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            return;/*
            if (Keyboard.GetKeyStates(Key.LeftCtrl) == KeyStates.Down)
            {
                SectionGridScale.ScaleX = Math.Clamp(SectionGridScale.ScaleX + ((e.Delta > 0) ? 0.1 : -0.1), 0.25, 2.0);
                SectionGridScale.ScaleY = Math.Clamp(SectionGridScale.ScaleY + ((e.Delta > 0) ? 0.1 : -0.1), 0.25, 2.0);
                e.Handled = true;
            }*/
        }
        private void SetSaveButtonStates()
        {
            if (ActiveMod != null)
            {
                SaveBtn.IsEnabled = ChangeStates[ActiveMod];
            }
            else
            {
                SaveBtn.IsEnabled = false;
            }
            SaveAllBtn.IsEnabled = ChangeStates.Any(p => p.Value == true);
            MinLevelSld.IsEnabled = TestBtn.IsEnabled = DeleteBtn.IsEnabled = DuplicateBtn.IsEnabled = ActiveSection != null;
        }

        private bool Save(Mod mod, out string[] errors)
        {
            errors = [];
            if (mod == null) return false;
            var builder = Wolfenstein.BuilderMods[mod.Name];
            foreach (var s in builder.MapSections)
                s.Layers = MapSection.Trim(s);
            if (!builder.Validate(out errors)) return false;

            var file = FileHelpers.Shared.GetDataFilePath(@$"Mods\{mod.Name}\map.json");
            try
            {
                if (!FileHelpers.Shared.Serialize(builder, file)) throw new Exception();
                ChangeStates[mod] = false;
                if (ActiveSection != null) ActiveSection.Layers = MapSection.Expand(ActiveSection);
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
            if (ChangeStates.Any(p => p.Value == true)) MessageBox.Show("Unable to save all mod changes", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SetSaveButtonStates();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod == null || ActiveSection == null) return;
            var builder = Wolfenstein.BuilderMods[ActiveMod.Name];
            builder.MapSections = builder.MapSections.Where(p => p.Id != ActiveSection.Id).ToArray();
            ChangeStates[ActiveMod] = true;
            SetActiveMod(ActiveMod);
        }

        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            /*
[0,0,1,0,0,0,0],
[0,-1,-1,-1,-1,-1,0],
[0,-1,-1,-1,-1,-1,0],
[0,-1,-1,-1,-1,-1,0],
[0,0,0,-1,0,0,0],
[0,-1,-1,-1,-1,-1,0],
[0,0,0,0,0,0,0]
             
             */
            if (ActiveMod == null) return;
            if (ActiveSection == null) return;
            var s = MapSection.Trim(ActiveSection);
            //int[][]? area = MapSection.GetClosedSection(s[0].Value,s[5].Value, out bool closed, out bool noDoors, out bool multiple);
            int[][]? area = ActiveSection.GetClosedSection(out bool closed, out bool noDoors, out bool multiple);
            MessageBox.Show($"Closed: {closed} NoDoors: {noDoors} Multiple: {multiple}");
        }

        private void MinLevelSld_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ActiveSection == null) return;
            ActiveSection.IntendedMinLevel = Math.Clamp((int)MinLevelSld.Value, 1, 100);
        }

        private void DuplicateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveMod == null || ActiveSection == null) return;
            MapSectionSelection.SelectionChanged -= MapSectionSelection_SelectionChanged;
            var builder = Wolfenstein.BuilderMods[ActiveMod.Name];
            var section = ActiveSection.Clone();
            section.Id = builder.MapSections.Length;

            MapSectionSelection.Items.Insert(MapSectionSelection.Items.IndexOf("New"), section.Id.ToString());
            MapSectionSelection.SelectedIndex = MapSectionSelection.Items.IndexOf(section.Id.ToString());
            MapSectionSelection.SelectionChanged += MapSectionSelection_SelectionChanged;
            builder.MapSections = [.. builder.MapSections, section];
            SetMapSectionSelections(section.Id);
        }


    }
}
