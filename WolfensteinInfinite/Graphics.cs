using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WolfensteinInfinite
{
    public partial class Graphics : IGraphics
    {
        private readonly GraphicsSurface GraphicsSurface;
        private readonly RenderWindow Window;

        private readonly byte[] VideoMemoryRGBA;
        private readonly Texture VideoMemoryOutputTexture;
        private readonly Sprite VideoMemoryOutputSprite;
        private readonly Sprite OutputSprite;
        private readonly SystemParameters SystemParameters;
        private readonly Image WindowIcon;
        private readonly string WindowTitle;
        private Time LastTime;
        private readonly Clock Clock;
        public Font DebugFont { get; init; }
        public float FPS { get; private set; }
        public bool ShowFPS { get; set; }
        public string Debug { get; set; } = string.Empty;
        public bool IsOpen => Window.IsOpen;
        public int Width => GraphicsSurface.Width;
        public int Height => GraphicsSurface.Height;
        public byte[] Pixels => GraphicsSurface.Pixels;
        public byte[] Pallet { get { return GraphicsSurface.Pallet; } set { GraphicsSurface.Pallet = value; } }
        public List<Keyboard.Key> Keys = [];
        public Dictionary<Keyboard.Key, Time> CheckKeys = [];
        public Action<KeyEventArgs>? KeyPressed { get; set; }
        public Action<KeyEventArgs>? KeyReleased { get; set; }
        public int KeyRefireDelay { get; set; } = 250;
        public BitDepth Bits => GraphicsSurface.Bits;

        public bool HasTransparency => GraphicsSurface.HasTransparency;

        public byte TransparencyIndex => GraphicsSurface.TransparencyIndex;
        public bool Active => Window.HasFocus();

        public Graphics(SystemParameters systemParameters, Image windowIcon, string windowTitle, Font debugFont, byte[] pallet)
        {
            WindowTitle = windowTitle;
            WindowIcon = windowIcon;
            SystemParameters = systemParameters;
            DebugFont = debugFont;
            GraphicsSurface = new GraphicsSurface(SystemParameters.Width, SystemParameters.Height, pallet);
            VideoMemoryRGBA = new byte[GraphicsSurface.Pixels.Length * 4];
            SystemParameters = systemParameters;
            OutputSprite = new Sprite();
            Clock = new Clock();
            LastTime = Clock.ElapsedTime;

            VideoMemoryOutputTexture = new Texture((uint)SystemParameters.Width, (uint)SystemParameters.Height);
            VideoMemoryOutputSprite = new Sprite(VideoMemoryOutputTexture);
            Styles styles = systemParameters.Fullscreen ? Styles.Fullscreen : Styles.Titlebar | Styles.Close;
            Window = new RenderWindow(new VideoMode((uint)SystemParameters.WindowWidth, (uint)SystemParameters.WindowHeight), WindowTitle, styles);            
            Window.Closed += (obj, e) => { Window.Close(); };
            
            Window.SetFramerateLimit((uint)SystemParameters.FPS);
            Window.SetIcon(WindowIcon.Size.X, WindowIcon.Size.Y, WindowIcon.Pixels);
            Window.SetKeyRepeatEnabled(false);
            Window.SetView(new View(new Vector2f(Window.Size.X / 2.0f, Window.Size.Y / 2.0f),
                         new Vector2f(Window.Size.X, Window.Size.Y)));

            OutputSprite.Scale = (Window.GetView().Size.X / VideoMemoryOutputSprite.Texture.Size.X, Window.GetView().Size.Y / VideoMemoryOutputSprite.Texture.Size.Y);

            Window.KeyPressed += (o, e) =>
            {
                if (!Keys.Contains(e.Code)) Keys.Add(e.Code);
                CheckKeys[e.Code] = Clock.ElapsedTime + Time.FromMilliseconds(KeyRefireDelay);
                KeyPressed?.Invoke(e);
            };
            Window.KeyReleased += (o, e) =>
            {
            Keys.Remove(e.Code);
                CheckKeys.Remove(e.Code);
                KeyReleased?.Invoke(e);
            };
        }
        public bool IsKeyDown() => Keys.Count != 0;
        public bool IsKeyDown(Keyboard.Key code) => Keys.Contains(code);
        public void Render()
        {
            Window.DispatchEvents();
            var currentTime = Clock.ElapsedTime;
            FPS = 1.0f / (currentTime.AsSeconds() - LastTime.AsSeconds());
            FPS = MathF.Round(FPS, 1);
            LastTime = currentTime;

            Span<byte> vm = GraphicsSurface.Pixels;
            Span<byte> vmrgb = VideoMemoryRGBA;
            ref byte buffer = ref MemoryMarshal.GetReference(vm);
            var off = 0;
            Span<byte> pallet = GraphicsSurface.Pallet;

            for (int i = 0; i < vm.Length; i++)
            {
                var c = Unsafe.Add(ref buffer, i) * 3;
                vmrgb[off++] = pallet[c];
                vmrgb[off++] = pallet[c + 1];
                vmrgb[off++] = pallet[c + 2];
                vmrgb[off++] = 255;
            }

            VideoMemoryOutputTexture.Update(VideoMemoryRGBA);
            //VideoMemoryOutputTexture.Smooth = true;
            OutputSprite.Texture = VideoMemoryOutputTexture;
            Window.Draw(OutputSprite);
            if ((ShowFPS || !string.IsNullOrWhiteSpace(Debug)) && DebugFont != null)
            {
                Window.Draw(new Text(ShowFPS ? $"{FPS}{Environment.NewLine}{Debug ?? string.Empty}" : Debug ?? string.Empty, DebugFont));
            }
            Debug = string.Empty;
            Window.Display();
        }
        public void ShutDown()
        {
            if (Window.IsOpen) Window.Close();
        }
        public void GetPelletIndex(byte index, out byte r, out byte g, out byte b) => GraphicsSurface.GetPelletIndex(index, out r, out g, out b);

        public void PutPixel(int x, int y, int index) => GraphicsSurface.PutPixel(x, y, index);

        public byte FindNearestColor(byte r, byte g, byte b) => GraphicsSurface.FindNearestColor(r, g, b);

        public void PutPixel(int x, int y, byte r, byte g, byte b, byte a) => GraphicsSurface.PutPixel(x, y, r, g, b, a);

        public void Clear(byte r, byte g, byte b) => GraphicsSurface.Clear(r, g, b);
        public void Clear(byte index) => GraphicsSurface.Clear(index);
        public void Line(int x, int y, int x2, int y2, byte index) => GraphicsSurface.Line(x, y, x2, y2, index);

        public void LineGradient(int x, int y, int x2, int y2, byte[] indicies) => GraphicsSurface.LineGradient(x, y, x2, y2, indicies);

        public void LineStrip(int x, int y, int x2, int y2, byte[] indicies) => GraphicsSurface.LineStrip(x, y, x2, y2, indicies);

        //Midpoint
        public void Circle(int cx, int cy, int r, byte index) => GraphicsSurface.Circle(cx, cy, r, index);

        public void CircleFill(int x0, int y0, int r, byte index) => GraphicsSurface.CircleFill(x0, y0, r, index);

        public void CircleGradient(int x0, int y0, int r, byte[] indicies) => GraphicsSurface.CircleGradient(x0, y0, r, indicies);

        //MidPoint
        public void Ellipse(int xc, int yc, int rx, int ry, byte index) => GraphicsSurface.Ellipse(xc, yc, rx, ry, index);


        public void EllipseFill(int xc, int yc, int rx, int ry, byte index) => GraphicsSurface.EllipseFill(xc, yc, rx, ry, index);


        public void EllipseGradient(int xc, int yc, int rx, int ry, byte[] indicies) => GraphicsSurface.EllipseGradient(xc, yc, rx, ry, indicies);

        public void Rect(int x, int y, int width, int height, byte index) => GraphicsSurface.Rect(x, y, width, height, index);

        public void RectFill(int x, int y, int width, int height, byte index) => GraphicsSurface.RectFill(x, y, width, height, index);

        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte index) => GraphicsSurface.Triangle(x1, y1, x2, y2, x3, y3, index);

        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte index) => GraphicsSurface.TriangleFill(x1, y1, x2, y2, x3, y3, index);

        public void TriangleGradient(int x1, int y1, int x2, int y2, int x3, int y3, byte c1, byte c2, byte c3) => GraphicsSurface.TriangleGradient(x1, y1, x2, y2, x3, y3, c1, c2, c3);

        public int GetPixel(int x, int y) => GraphicsSurface.GetPixel(x, y);

        public void GetPixel(int x, int y, out byte r, out byte g, out byte b, out byte a) => GraphicsSurface.GetPixel(x, y, out r, out g, out b, out a);

        public void Blit(int x, int y, ISurface surface) => GraphicsSurface.Blit(x, y, surface);

        public void Draw(int x1, int y1, ISurface surface, IImageTransformation[] transformations) => GraphicsSurface.Draw(x1, y1, surface, transformations);

        public void Line(int x, int y, int x2, int y2, byte r, byte g, byte b) => GraphicsSurface.Line(x, y, x2, y2, r, g, b);

        public void Circle(int centerX, int centerY, int rad, byte r, byte g, byte b) => GraphicsSurface.Circle(centerX, centerY, rad, r, g, b);

        public void CircleFill(int centerX, int centerY, int rad, byte r, byte g, byte b) => GraphicsSurface.CircleFill(centerX, centerY, rad, r, g, b);

        public void Ellipse(int xc, int yc, int rx, int ry, byte r, byte g, byte b) => GraphicsSurface.Ellipse(xc, yc, rx, ry, r, g, b);

        public void EllipseFill(int xc, int yc, int rx, int ry, byte r, byte g, byte b) => GraphicsSurface.EllipseFill(xc, yc, rx, ry, r, g, b);

        public void Rect(int x, int y, int width, int height, byte r, byte g, byte b) => GraphicsSurface.Rect(x, y, width, height, r, g, b);

        public void RectFill(int x, int y, int width, int height, byte r, byte g, byte b) => GraphicsSurface.RectFill(x, y, width, height, r, g, b);

        public void Triangle(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b) => GraphicsSurface.Triangle(x1, y1, x2, y2, x3, y3, FindNearestColor(r, g, b));

        public void TriangleFill(int x1, int y1, int x2, int y2, int x3, int y3, byte r, byte g, byte b) => GraphicsSurface.TriangleFill(x1, y1, x2, y2, x3, y3, FindNearestColor(r, g, b));

        public void LineGradient(int x1, int y1, int x2, int y2, RGBA8[] colors) => GraphicsSurface.LineGradient(x1, y1, x2, y2, colors);

        public void LineStrip(int x1, int y1, int x2, int y2, RGBA8[] colors) => GraphicsSurface.LineGradient(x1, y1, x2, y2, colors);

        internal void UpdateKeys()
        {
            if (!Active)
            {
                CheckKeys.Clear();
                Keys.Clear();
                return;
            }
            foreach (var key in CheckKeys.Keys)
            {
                if (!Keys.Contains(key))
                {
                    CheckKeys.Remove(key);
                    continue;
                }
                if (CheckKeys[key] < Clock.ElapsedTime)
                {
                    CheckKeys[key] = Clock.ElapsedTime + Time.FromMilliseconds(KeyRefireDelay);
                    KeyPressed?.Invoke(new KeyEventArgs(new KeyEvent() { Code = key }));
                }
            }
        }
    }
}


