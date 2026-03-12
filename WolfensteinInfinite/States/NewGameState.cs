//Clean
using SFML.Window;
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.GameHelpers;
using WolfensteinInfinite.GameObjects;
using WolfensteinInfinite.MenuUI;

namespace WolfensteinInfinite.States
{
    public class NewGameState : GameState
    {
        private Menu Menu { get; init; }
        public NewGameState(Wolfenstein wolfenstein, GameState? returnState) : base(wolfenstein)
        {
            ReturnState = returnState ?? this;
            NextState = this;
            Menu = new Menu(Wolfenstein.GameResources.TitleNewGame,
                Wolfenstein.GameResources.MenuCommands,
                Wolfenstein.GameResources.MenuSelect1,
                Wolfenstein.GameResources.MenuSelect2,
                wolfenstein.GameResources.Effects["ChangeMenu"], 
                Wolfenstein.GameResources.SmallFont
                );
            foreach (var d in Enum.GetValues<Difficulties>())
            {
                Menu.MenuItems.Add(new MenuItem(DifficultyHelpers.GetDifficultyString(d), OnMenuAction, GameResources.SmallFont));                
            }
            SetSideIcon();
        }
        public void SetSideIcon()
        {
            var t = Menu.MenuItems[Menu.Selected].Text;
            foreach (var d in Enum.GetValues<Difficulties>())
            {
                if(t== DifficultyHelpers.GetDifficultyString(d))
                {
                    Menu.SideIcon = DifficultyHelpers.GetDifficultyIcon(d);
                    break;
                }
            }
        }
        private void OnMenuAction(IMenuItem item)
        {
            var t = Menu.MenuItems[Menu.Selected].Text;
            foreach (var d in Enum.GetValues<Difficulties>())
            {
                if (t == DifficultyHelpers.GetDifficultyString(d))
                {
                    var player = new Player(Menu.InputString);
                    NextState = new GameGenerationState(Wolfenstein,player, d,1);
                    break;
                }
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
            SetSideIcon();
        }
    }
}
