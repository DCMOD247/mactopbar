using System.Windows;

namespace MacTopBar;

public partial class App : Application
{
    private readonly List<MainWindow> _bars = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = AppSettings.Load();
        foreach (var monitor in MonitorInfo.GetAll())
        {
            var bar = new MainWindow(monitor, settings, () => Shutdown());
            _bars.Add(bar);
            bar.Show();
        }
    }
}
