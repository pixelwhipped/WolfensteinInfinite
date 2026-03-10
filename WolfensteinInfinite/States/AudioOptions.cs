using SFML.Window;
using WolfensteinInfinite.GameAudio;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{

    public class AudioOptions : GameState
    {
        private Menu Menu { get; init; }
        public AudioOptions(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleOptions,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"]
                );
            Menu.MenuItems.Add(new MenuItemOnOff("Sounds", SoundOnOff, wolfenstein.Config.Sound, 180, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItemNumber("Volume", "%", SoundVolume, wolfenstein.Config.SoundVolume, 0, 100, 180, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItemOnOff("Music", MusicOnOff, wolfenstein.Config.Music, 180, wolfenstein.GameResources.SmallFont));
            Menu.MenuItems.Add(new MenuItemNumber("Volume", "%", MusicVolume, wolfenstein.Config.MusicVolume, 0, 100, 180, wolfenstein.GameResources.SmallFont));
        }
        private void SoundVolume(IMenuItem item)
        {
            if (item is MenuItemNumber i)
            {
                Wolfenstein.Config.SoundVolume = i.Number;
                AudioPlaybackEngine.Instance.SoundVolume = i.Number / 100f;
            }
        }
        private void MusicVolume(IMenuItem item)
        {
            if (item is MenuItemNumber i)
            {
                Wolfenstein.Config.MusicVolume = i.Number;
                AudioPlaybackEngine.Instance.MusicVolume = i.Number / 100f;
            }
        }
        private void SoundOnOff(IMenuItem item)
        {
            if (item is MenuItemOnOff i)
            {
                Wolfenstein.Config.Sound = i.State;
                AudioPlaybackEngine.Instance.SoundOn = i.State;
            }
        }
        private void MusicOnOff(IMenuItem item)
        {
            if (item is MenuItemOnOff i)
            {
                Wolfenstein.Config.Music = i.State;
                AudioPlaybackEngine.Instance.MusicOn = i.State;
                if (Wolfenstein.CurrentMusic != null)
                    AudioPlaybackEngine.Instance.PlayMusic(Wolfenstein.CurrentMusic);
            }
        }
        public override GameState? Update(Texture32 buffer, float frameTime)
        {
            CommonGraphics.DrawTtileAnim(buffer, GameResources, Clock, 1f);
            Menu.Draw(buffer, Wolfenstein.Clock);
            return NextState;
        }
        public override void OnKeyPressed(KeyEventArgs k)
        {
            if (k.Code == Keyboard.Key.Escape)
            {
                ReturnState.NextState = ReturnState;
                NextState = ReturnState;
                return;
            }
            Menu.OnKeyPressed(k);
        }
    }
}