using System.Windows;

namespace WolfensteinInfinite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var wolfenstein = new Wolfenstein(this);
            wolfenstein.Run();
            wolfenstein.ShutDown();
            Shutdown();
        }
    }

}
