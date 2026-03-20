//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Audio;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.MenuUI;
using WolfensteinInfinite.Utilities;
using WolfensteinInfinite.WolfMod;

namespace WolfensteinInfinite.States
{
    public class WikiState : GameState
    {
        private Menu Menu { get; init; }
        private MenuItemOptionSelector ModSelector { get; init; }
        private PickupItem[] PickupItems { get; init; }
        private PlayerWeapon[] Attacks { get; init; }
        public Mod CurrentMod;
        private int MusicIndex = 0;
        private int TextureIndex = 0;
        private int EnemyIndex = 0;
        private int DecalIndex = 0;
        private int WeaponIndex = 0;
        private int ProjectileIndex = 0;
        private int AttackIndex = 0;
        private int PickupIndex = 0;

        private Enemy? Experiment;
        private CharacterSprite? ExperimentSprite;

        private readonly CharacterAnimationState[] CharacterAnimationStates = (CharacterAnimationState[])Enum.GetValues(typeof(CharacterAnimationState));
        private int CharacterAnimationStateIndex = 1;
        private float angle = 0;
        public WikiState(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(null,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                Wolfenstein.GameResources.Effects["ChangeMenu"]
                )
            {
                FixedXPosition = 0
            };
            PickupItems = [.. wolfenstein.PickupItemTypes.Values.Where(p => p.ItemType != PickupItemType.SPAWNER && p.ItemType != PickupItemType.MISSION_OBJECTIVE)];
            Attacks = [.. wolfenstein.PlayerWeapons.Values.OrderBy(p => p.PreferedOrder)];
            var modOptions = Wolfenstein.Mods.Keys.ToArray();
            ModSelector = new MenuItemOptionSelector("Mod", OnMenuAction, modOptions, 0, 140, wolfenstein.GameResources.TinyFont);
            SetMenu(out CurrentMod);
        }

        public void SetMenu(out Mod mod)
        {
            Menu.MenuItems.Clear();
            Menu.MenuItems.Add(ModSelector);
            mod = CurrentMod = Wolfenstein.Mods[ModSelector.Options[ModSelector.Current]];
            Menu.MenuItems.Add(new MenuItem($"Music({CurrentMod.MusicTracks.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.MusicTracks.Length > 0, CurrentMod.MusicTracks.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Textures({CurrentMod.Textures.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.Textures.Length > 0, CurrentMod.Textures.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Enemies({CurrentMod.Enemies.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.Enemies.Length > 0, CurrentMod.Enemies.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Decals({CurrentMod.Decals.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.Decals.Length > 0, CurrentMod.Decals.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Weapons({CurrentMod.Weapons.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.Weapons.Length > 0, CurrentMod.Weapons.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Projectiles({CurrentMod.Projectiles.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.Projectiles.Length > 0, CurrentMod.Projectiles.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Items({PickupItems.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, PickupItems.Length > 0, PickupItems.Length > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Attacks({Wolfenstein.PlayerWeapons.Count})", OnMenuAction, Wolfenstein.GameResources.TinyFont, Wolfenstein.PlayerWeapons.Count > 0, Wolfenstein.PlayerWeapons.Count > 0 ? null : RGBA8.STEEL_BLUE));
            Menu.MenuItems.Add(new MenuItem($"Experiments({CurrentMod.ExperimentalEnemy.Length})", OnMenuAction, Wolfenstein.GameResources.TinyFont, CurrentMod.ExperimentalEnemy.Length > 0, CurrentMod.ExperimentalEnemy.Length > 0 ? null : RGBA8.STEEL_BLUE));
        }

        private void OnMenuAction(IMenuItem item)
        {
            if (item is MenuItemOptionSelector)
            {
                MusicIndex = 0;
                TextureIndex = 0;
                EnemyIndex = 0;
                DecalIndex = 0;
                WeaponIndex = 0;
                ProjectileIndex = 0;
                AttackIndex = 0;
                PickupIndex = 0;
                SetMenu(out _);
                return;
            }
            if (item.Text.StartsWith("Music"))
            {
                var mFile = CurrentMod.MusicTracks[MusicIndex].File;
                AudioPlaybackEngine.Instance.PlayMusic(FileHelpers.Shared.GetModDataFilePath(mFile));
            }
        }

        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            angle++;
            if (angle > 359) angle = 0;

            Menu.Draw(buffer, Wolfenstein.Clock);
            if (Menu.MenuItems[Menu.Selected] is MenuItem sel)
            {
                if (sel.Text.StartsWith("Music"))
                {
                    var track = CurrentMod.MusicTracks[MusicIndex].Name;
                    var (Width, Height) = Wolfenstein.GameResources.SmallFont.MeasureString(track);

                    var x = ((buffer.Width - 180) / 2) - (Width / 2);
                    buffer.DrawString(x + 180, (buffer.Height / 2) - (Height / 2), track, Wolfenstein.GameResources.SmallFont, null);
                }
                else if (sel.Text.StartsWith("Textures"))
                {
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;
                    buffer.Draw(x, y, Wolfenstein.Textures[CurrentMod.Name][TextureIndex + (CurrentMod.Name=="Infinite"?1001 : 0)]);
                    y += 64;
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, CurrentMod.Textures[TextureIndex].Name, Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"ID:{CurrentMod.Textures[TextureIndex].MapID}", Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Items"))
                {
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;

                    buffer.Draw(x, y, Wolfenstein.PickupItems[PickupIndex]);
                    y += 64;
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, PickupItems[PickupIndex].Name, Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Attacks"))
                {
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;

                    buffer.Draw(x, y, Wolfenstein.WeaponHudTextures[Attacks[AttackIndex].Name]);
                    y += 64;
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, Attacks[AttackIndex].Name, Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Enemies"))
                {

                    var enemy = Wolfenstein.Mods[CurrentMod.Name].Enemies[EnemyIndex];
                    var spr = Wolfenstein.CharacterSprites[CurrentMod.Name][enemy.MapID];

                    spr.Update(frameTime);
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;
                    buffer.Draw(x, y, spr.GetTexture(angle));
                    y += 64;
                    buffer.DrawString(x, y, $"{enemy.Name}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"ID:{enemy.MapID}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"HP:{enemy.HitPoints[0]} P:{enemy.Points}", Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Decals"))
                {
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;
                    buffer.Draw(x, y, Wolfenstein.Decals[CurrentMod.Name][DecalIndex]);
                    y += 64;
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, CurrentMod.Decals[DecalIndex].Name, Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"D:{CurrentMod.Decals[DecalIndex].Direction}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"P:{CurrentMod.Decals[DecalIndex].Passable} L:{CurrentMod.Decals[DecalIndex].LightSource}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"ID:{CurrentMod.Decals[DecalIndex].MapID}", Wolfenstein.GameResources.TinyFont, null);

                }
                else if (sel.Text.StartsWith("Weapons"))
                {
                    var w = CurrentMod.Weapons[WeaponIndex];
                    var x = ((buffer.Width - 140) / 2) - 32;
                    var y = (buffer.Height / 2) - 32;
                    x += 140;
                    buffer.DrawString(x, y, w.Name, Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"P:{w.Projectile}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"FR:{w.FireRate}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"CD:{w.Cooldown}", Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Experiments") && CurrentMod.ExperimentalEnemy.Length > 0)
                {
                    if (Experiment == null || ExperimentSprite == null)
                        GenerateExperiment();
                    if (Experiment == null) return NextState;
                    if (ExperimentSprite == null) return NextState;
                    ExperimentSprite.Update(frameTime);
                    var x = ((buffer.Width - 140) / 2) - 32;
                    x += 140;
                    var y = (buffer.Height / 2) - 32;
                    buffer.Draw(x, y, ExperimentSprite.GetTexture(angle));
                    y += 64;
                    buffer.DrawString(x, y, $"{Experiment.Name}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"HP:{Experiment.HitPoints[0]} P:{Experiment.Points}", Wolfenstein.GameResources.TinyFont, null);
                }
                else if (sel.Text.StartsWith("Projectiles"))
                {
                    var w = CurrentMod.Projectiles[ProjectileIndex];
                    if (Wolfenstein.ProjectileSprites[CurrentMod.Name].TryGetValue(w.Name, out var spr))
                        spr?.Update(frameTime);

                    var x = ((buffer.Width - 140) / 2) - 32;
                    var y = (buffer.Height / 2) - 32;
                    x += 140;
                    if (spr != null)
                        buffer.Draw(x, y, spr.GetTexture(angle));
                    y += 64;
                    buffer.DrawString(x, y, w.Name, Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"DM:{w.DamageMod}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"RM:{w.RangeMod}", Wolfenstein.GameResources.TinyFont, null);
                    y += Wolfenstein.GameResources.TinyFont.Height;
                    buffer.DrawString(x, y, $"A:{w.AmmoType}", Wolfenstein.GameResources.TinyFont, null);
                }
            }
            return NextState;
        }

        private void GenerateExperiment() => Wolfenstein.GenerateExperiment(CurrentMod, 1, out Experiment, out ExperimentSprite);

        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                ReturnState.NextState = ReturnState;
                NextState = ReturnState;
                AudioPlaybackEngine.Instance.StopMusic();
                return;
            }
            var sel = Menu.MenuItems[Menu.Selected] as MenuItem;
            if (sel != null && k.Code == Keyboard.Key.Enter)
            {
                if (sel.Text.StartsWith("Enemies"))
                {
                    var enemy = Wolfenstein.Mods[CurrentMod.Name].Enemies[EnemyIndex];
                    var spr = Wolfenstein.CharacterSprites[CurrentMod.Name][enemy.MapID];
                    CharacterAnimationStateIndex = (CharacterAnimationStateIndex + 1) % CharacterAnimationStates.Length;
                    spr.AnimationState = CharacterAnimationStates[CharacterAnimationStateIndex];
                }
                else if (sel.Text.StartsWith("Experiments"))
                {
                    if (ExperimentSprite != null)
                    {
                        CharacterAnimationStateIndex = (CharacterAnimationStateIndex + 1) % CharacterAnimationStates.Length;
                        ExperimentSprite.AnimationState = CharacterAnimationStates[CharacterAnimationStateIndex];
                    }
                }
            }
            if (sel != null && (k.Code == Keyboard.Key.Left || k.Code == Keyboard.Key.Right))
            {
                var dir = (k.Code == Keyboard.Key.Left) ? -1 : 1;
                if (sel.Text.StartsWith("Music"))
                {
                    MusicIndex = Math.Clamp(MusicIndex + dir, 0, CurrentMod.MusicTracks.Length - 1);
                }
                else if (sel.Text.StartsWith("Textures"))
                {
                    TextureIndex = Math.Clamp(TextureIndex + dir, 0, CurrentMod.Textures.Length - 1);
                }
                else if (sel.Text.StartsWith("Experiments"))
                {
                    GenerateExperiment();
                }
                else if (sel.Text.StartsWith("Enemies"))
                {
                    var ki = Math.Clamp(EnemyIndex + dir, 0, CurrentMod.Enemies.Length - 1);
                    EnemyIndex = ki;// CurrentMod.Enemies[ki].MapID;
                }
                else if (sel.Text.StartsWith("Decals"))
                {
                    DecalIndex = Math.Clamp(DecalIndex + dir, 0, CurrentMod.Decals.Length - 1);
                }
                else if (sel.Text.StartsWith("Attacks"))
                {
                    AttackIndex = Math.Clamp(AttackIndex + dir, 0, Attacks.Length - 1);
                }
                else if (sel.Text.StartsWith("Items"))
                {
                    PickupIndex = Math.Clamp(PickupIndex + dir, 0, PickupItems.Length - 1);
                }
                else if (sel.Text.StartsWith("Weapons"))
                {
                    WeaponIndex = Math.Clamp(WeaponIndex + dir, 0, CurrentMod.Weapons.Length - 1);
                }
                else if (sel.Text.StartsWith("Projectiles"))
                {
                    ProjectileIndex = Math.Clamp(ProjectileIndex + dir, 0, CurrentMod.Projectiles.Length - 1);
                }

            }
            Menu.OnKeyPressed(k);
        }
    }
}
