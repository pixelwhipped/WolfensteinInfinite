using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WolfensteinInfinite.WolfMod;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace WolfensteinInfinite.Editor
{
    /// <summary>
    /// Interaction logic for SectionButton.xaml
    /// </summary>
    public partial class SectionButton : UserControl
    {
        public int X { get; init; } = 0;
        public int Y { get; init; } = 0;
        public MapEditor? MapEditor { get; init; }
        public SectionButton()
        {
            MapEditor = null;
            InitializeComponent();
        }
        public SectionButton(MapEditor editor, int x, int y)
        {
            MapEditor = editor;
            X = x;
            Y = y;
            InitializeComponent();
        }
        public void Clear()
        {
            Image.Source = null;
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e) => 
            MapEditor?.MapClick(X, Y, e.GetPosition(this), e.LeftButton == MouseButtonState.Pressed);

        public void SetWallTexture(int v)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var wallBitmap = MapEditor.GetTextureBitmap(MapEditor.ActiveMod.Name, v);
            Image.Source = wallBitmap;
        }
        public void SetDoorTexture(int v, DoorDirection dir)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var doorBitmap = MapEditor.GetDoorBitmap(v, dir);
            Image.Source = doorBitmap;
        }
        public void SetSpecialTexture(int v, int w)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var specialBitmap = MapEditor.GetSpecialBitmap(MapEditor.ActiveMod.Name, v, w);
            Image.Source = specialBitmap;

        }


        internal void SetDecalTexture(int v, string detail)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var decalBitmap = MapEditor.GetDecalBitmap(MapEditor.ActiveMod.Name, v);
            Image.Source = decalBitmap;
            Details.Text = detail ?? string.Empty;
        }

        internal void SetItemTexture(int v, string detail)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var itemBitmap = MapEditor.GetItemBitmap(v);
            Image.Source = itemBitmap;
            Details.Text = detail ?? string.Empty;
        }
        internal void SetEnemyTexture(int v, string detail)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var itemBitmap = MapEditor.GetEnemyBitmap(MapEditor.ActiveMod.Name, v);
            Image.Source = itemBitmap;
            Details.Text = detail ?? string.Empty;
        }

        internal void SetSpecialItems(int v, int i)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var specialBitmap = MapEditor.GetSpecialBitmapItem(MapEditor.ActiveMod.Name, v, i);
            Image.Source = specialBitmap;
        }

        internal void SetSpecialEnemy(int v, int e)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var specialBitmap = MapEditor.GetSpecialBitmapEnemy(MapEditor.ActiveMod.Name, v, e);
            Image.Source = specialBitmap;
        }
        

        internal void SetSpecialDecal(int v, int d)
        {
            if (MapEditor == null) return;
            if (MapEditor.ActiveMod == null) return;
            var specialBitmap = MapEditor.GetSpecialBitmapDecal(MapEditor.ActiveMod.Name, v, d);
            Image.Source = specialBitmap;
        }
    }
}
