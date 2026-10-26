using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Path = System.Windows.Shapes.Path;
using System.Windows.Threading;

namespace MacTopBar;

public partial class MainWindow : Window
{
    private readonly MonitorInfo _screen;
    private readonly AppSettings _settings;
    private readonly Action _quitApplication;
    private readonly DispatcherTimer _timer;
    private readonly int _ownProcessId = Process.GetCurrentProcess().Id;
    private IntPtr _lastActiveWindow = IntPtr.Zero;
    private IntPtr _handle;
    private bool _appBarRegistered;
    private StackPanel _left = null!;
    private StackPanel _right = null!;
    private Button _appNameButton = null!;
    private TextBlock _networkText = null!;
    private TextBlock _batteryText = null!;
    private TextBlock _bluetoothText = null!;
    private TextBlock _dateText = null!;
    private TextBlock _clockText = null!;
    private Brush _textBrush = Brushes.White;
    private Brush _hoverBrush = new SolidColorBrush(Color.FromArgb(38, 255, 255, 255));

    public MainWindow(MonitorInfo screen, AppSettings settings, Action quitApplication)
    {
        InitializeComponent();
        _screen = screen;
        _settings = settings;
        Left = screen.Left * 96.0 / Math.Max(96, screen.DpiX);
        Top = screen.Top * 96.0 / Math.Max(96, screen.DpiY);
        Width = screen.Width * 96.0 / Math.Max(96, screen.DpiX);
        Height = 34 * 96.0 / Math.Max(96, screen.DpiY);
        _quitApplication = quitApplication;
        BuildInterface();
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
        _timer.Tick += (_, _) => RefreshIndicators();
        _timer.Start();
        RefreshIndicators();
    }

    private void BuildInterface()
    {
        Root.Children.Clear();
        Root.Height = 34;
        var layout = new Grid { Height = 34 };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        _right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 12, 0) };
        Grid.SetColumn(_left, 0);
        Grid.SetColumn(_right, 2);
        layout.Children.Add(_left);
        layout.Children.Add(_right);

        var logo = new Button { Content = CreateAppleMark(), Padding = new Thickness(4, 1, 5, 1), ToolTip = "MacTopBar menu", MinWidth = 27 };
        StyleButton(logo, true);
        logo.Click += (_, _) => ShowMenu(logo, BuildAppleMenu());
        _left.Children.Add(logo);

        _appNameButton = MakeButton("Desktop", bold: true, horizontalPadding: 8);
        _appNameButton.Click += (_, _) => ShowMenu(_appNameButton, BuildAppMenu());
        _left.Children.Add(_appNameButton);
        AddMenuButton("File", BuildFileMenu);
        AddMenuButton("Edit", BuildEditMenu);
        AddMenuButton("View", BuildViewMenu);
        AddMenuButton("Window", BuildWindowMenu);
        AddMenuButton("Help", BuildHelpMenu);

        _networkText = MakeIndicator("", "Wi-Fi and network status");
        _batteryText = MakeIndicator("▰", "Battery status");
        _bluetoothText = MakeIndicator("ᛒ", "Bluetooth status");
        _dateText = MakeIndicator("", "Current date");
        _clockText = MakeIndicator("", "Current time");
        _right.Children.Add(_networkText);
        _right.Children.Add(_batteryText);
        // Bluetooth is hidden unless an actual Bluetooth radio is found by the system query.
        _right.Children.Add(_bluetoothText);
        _right.Children.Add(_dateText);
        _right.Children.Add(_clockText);

        Root.Children.Add(layout);
        ApplyAppearance();
    }

    private Canvas CreateAppleMark()
    {
        var canvas = new Canvas { Width = 17, Height = 20 };
        var body = new Path
        {
            Fill = _textBrush,
            Data = Geometry.Parse("M 8.8,5.2 C 6.3,3.7 2.0,5.0 1.7,9.4 C 1.4,13.4 4.0,18.3 6.5,18.5 C 7.6,18.6 8.5,17.7 9.5,17.7 C 10.5,17.7 11.4,18.6 12.5,18.4 C 15.3,18.1 17.2,13.5 17.0,10.5 C 16.9,7.7 14.5,5.1 11.7,5.4 C 10.5,5.6 9.7,6.0 8.8,5.2 Z")
        };
        var leaf = new Path
        {
            Fill = _textBrush,
            Data = Geometry.Parse("M 10.0,4.2 C 10.0,1.5 12.2,0.2 14.8,0.0 C 14.8,2.6 13.0,4.3 10.0,4.2 Z")
        };
        canvas.Children.Add(body);
        canvas.Children.Add(leaf);
        return canvas;
    }

    private void AddMenuButton(string label, Func<ContextMenu> menuFactory)
    {
        var button = MakeButton(label, false, 8);
        button.Click += (_, _) => ShowMenu(button, menuFactory());
        _left.Children.Add(button);
    }

    private Button MakeButton(string label, bool bold = false, double horizontalPadding = 7)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(horizontalPadding, 3, horizontalPadding, 3),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = _textBrush,
            FocusVisualStyle = null,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand,
            IsTabStop = false
        };
        StyleButton(button);
        return button;
    }

    private void StyleButton(Button button, bool logo = false)
    {
        button.BorderBrush = Brushes.Transparent;
        button.Background = Brushes.Transparent;
        button.MouseEnter += (_, _) => { button.Background = _hoverBrush; AnimateButtonOpacity(button, 0.96); };
        button.MouseLeave += (_, _) => { button.Background = Brushes.Transparent; AnimateButtonOpacity(button, 1.0); };
        button.MouseDown += (_, _) => button.Background = _hoverBrush;
    }

    private TextBlock MakeIndicator(string text, string tooltip)
    {
        return new TextBlock
        {
            Text = text,
            ToolTip = tooltip,
            FontFamily = new FontFamily(text == "" ? "Segoe MDL2 Assets" : "Segoe UI Symbol"),
            FontSize = 12,
            Foreground = _textBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 5, 0)
        };
    }

    private void ShowMenu(Button owner, ContextMenu menu)
    {
        menu.PlacementTarget = owner;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private ContextMenu NewMenu()
    {
        var menu = new ContextMenu
        {
            Background = _settings.Appearance == "Light" ? new SolidColorBrush(Color.FromRgb(250, 250, 252)) : new SolidColorBrush(Color.FromRgb(35, 36, 40)),
            Foreground = _settings.Appearance == "Light" ? new SolidColorBrush(Color.FromRgb(32, 32, 36)) : Brushes.White,
            BorderBrush = _settings.Appearance == "Light" ? new SolidColorBrush(Color.FromRgb(215, 216, 220)) : new SolidColorBrush(Color.FromRgb(75, 76, 82)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5),
            HasDropShadow = true,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };
        var itemStyle = new Style(typeof(MenuItem));
        itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, menu.Foreground));
        itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 6, 25, 6)));
        itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        itemStyle.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Segoe UI")));
        itemStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        var template = new ControlTemplate(typeof(MenuItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Bd";
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        var dockPanel = new FrameworkElementFactory(typeof(DockPanel));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(7, 2, 7, 2));
        presenter.SetValue(DockPanel.DockProperty, Dock.Left);
        var gesture = new FrameworkElementFactory(typeof(TextBlock));
        gesture.SetBinding(TextBlock.TextProperty, new Binding("InputGestureText") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });
        gesture.SetValue(DockPanel.DockProperty, Dock.Right);
        gesture.SetValue(TextBlock.MarginProperty, new Thickness(26, 2, 8, 2));
        gesture.SetValue(TextBlock.OpacityProperty, 0.72);
        dockPanel.AppendChild(presenter);
        dockPanel.AppendChild(gesture);
        border.AppendChild(dockPanel);
        template.VisualTree = border;
        var hover = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, _settings.Appearance == "Light" ? new SolidColorBrush(Color.FromRgb(225, 232, 244)) : new SolidColorBrush(Color.FromRgb(70, 85, 111)), "Bd"));
        template.Triggers.Add(hover);
        itemStyle.Setters.Add(new Setter(Control.TemplateProperty, template));
        menu.Resources[typeof(MenuItem)] = itemStyle;
        var separatorStyle = new Style(typeof(Separator));
        separatorStyle.Setters.Add(new Setter(Separator.MarginProperty, new Thickness(5, 4, 5, 4)));
        separatorStyle.Setters.Add(new Setter(Separator.BackgroundProperty, _settings.Appearance == "Light" ? new SolidColorBrush(Color.FromRgb(220, 220, 224)) : new SolidColorBrush(Color.FromRgb(78, 79, 84))));
        separatorStyle.Setters.Add(new Setter(Separator.HeightProperty, 1.0));
        menu.Resources[typeof(Separator)] = separatorStyle;
        return menu;
    }

    private MenuItem Item(string label, Action action, string? gesture = null)
    {
        var item = new MenuItem { Header = label, InputGestureText = gesture };
        item.Click += (_, _) => SafeAction(action);
        return item;
    }

    private static Separator Separator() => new();

    private ContextMenu BuildAppleMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("About MacTopBar", ShowAbout));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Settings…", ShowSettings, "⌘,"));
        menu.Items.Add(Item("Open Windows Settings", () => StartProcess("ms-settings:")));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("File Explorer", () => StartProcess("explorer.exe")));
        menu.Items.Add(Item("Task Manager", () => StartProcess("taskmgr.exe")));
        menu.Items.Add(Item("Lock PC", () => StartProcess("rundll32.exe", "user32.dll,LockWorkStation")));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Quit MacTopBar", _quitApplication, "Alt+F4"));
        return menu;
    }

    private ContextMenu BuildAppMenu()
    {
        var menu = NewMenu();
        var name = GetActiveApplicationName();
        menu.Items.Add(Item("About " + name, () => MessageBox.Show(this, $"Active application: {name}\n\nMacTopBar shows the currently focused Windows application.", "About " + name, MessageBoxButton.OK, MessageBoxImage.Information)));
        menu.Items.Add(Item("Show in Task Manager", () => StartProcess("taskmgr.exe")));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Minimize " + name, () => WithActiveWindow(h => NativeMethods.ShowWindow(h, NativeMethods.SW_MINIMIZE))));
        menu.Items.Add(Item("Quit " + name, () => WithActiveWindow(h => NativeMethods.PostMessage(h, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero))));
        return menu;
    }

    private ContextMenu BuildFileMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("New Explorer Window", () => StartProcess("explorer.exe"), "Ctrl+N"));
        menu.Items.Add(Item("Open Downloads", () => StartProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads")));
        menu.Items.Add(Item("Open Documents", () => StartProcess("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Open Windows Settings", () => StartProcess("ms-settings:")));
        menu.Items.Add(Item("Close Front Window", () => SendShortcut("F4", alt: true), "Alt+F4"));
        return menu;
    }

    private ContextMenu BuildEditMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("Undo", () => SendShortcut("Z", ctrl: true), "Ctrl+Z"));
        menu.Items.Add(Item("Redo", () => SendShortcut("Y", ctrl: true), "Ctrl+Y"));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Cut", () => SendShortcut("X", ctrl: true), "Ctrl+X"));
        menu.Items.Add(Item("Copy", () => SendShortcut("C", ctrl: true), "Ctrl+C"));
        menu.Items.Add(Item("Paste", () => SendShortcut("V", ctrl: true), "Ctrl+V"));
        menu.Items.Add(Item("Select All", () => SendShortcut("A", ctrl: true), "Ctrl+A"));
        return menu;
    }

    private ContextMenu BuildViewMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("Refresh", () => SendShortcut("F5"), "F5"));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Zoom In", () => SendShortcut("ADD", ctrl: true), "Ctrl++"));
        menu.Items.Add(Item("Zoom Out", () => SendShortcut("SUBTRACT", ctrl: true), "Ctrl+-"));
        menu.Items.Add(Item("Reset Zoom", () => SendShortcut("0", ctrl: true), "Ctrl+0"));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Toggle Full Screen", () => SendShortcut("F11"), "F11"));
        return menu;
    }

    private ContextMenu BuildWindowMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("Minimize", () => WithActiveWindow(h => NativeMethods.ShowWindow(h, NativeMethods.SW_MINIMIZE)), "Win+↓"));
        menu.Items.Add(Item("Maximize / Restore", () => WithActiveWindow(h => NativeMethods.ShowWindow(h, NativeMethods.IsZoomed(h) ? NativeMethods.SW_RESTORE : 3))));
        menu.Items.Add(Item("Bring Front Window Forward", () => WithActiveWindow(h => NativeMethods.SetForegroundWindow(h))));
        menu.Items.Add(Separator());
        menu.Items.Add(Item("Show Desktop", () => SendShortcut("D", win: true), "Win+D"));
        menu.Items.Add(Item("Switch Application", () => SendShortcut("TAB", alt: true), "Alt+Tab"));
        return menu;
    }

    private ContextMenu BuildHelpMenu()
    {
        var menu = NewMenu();
        menu.Items.Add(Item("MacTopBar Help", () => MessageBox.Show(this, "MacTopBar provides a macOS-inspired menu bar for Windows 11.\n\n• Click the Apple menu for Settings and Windows tools.\n• File, Edit, View and Window actions target the last focused application.\n• Use Settings to change appearance or enable startup.\n• Choose Quit MacTopBar to exit.", "MacTopBar Help", MessageBoxButton.OK, MessageBoxImage.Information)));
        menu.Items.Add(Item("Keyboard Shortcuts", () => MessageBox.Show(this, "Edit: Ctrl+Z / Ctrl+Y / Ctrl+X / Ctrl+C / Ctrl+V / Ctrl+A\nView: F5, Ctrl++, Ctrl+-, Ctrl+0, F11\nWindow: Win+D, Alt+Tab, Alt+F4", "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information)));
        menu.Items.Add(Item("About MacTopBar", ShowAbout));
        return menu;
    }

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            foreach (Window window in Application.Current.Windows)
                if (window is MainWindow bar) bar.ApplyAppearance();
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(this, "MacTopBar\nNative WPF menu bar for Windows 11\n\nSystem indicators use Windows APIs. Preferences are stored in your AppData folder.", "About MacTopBar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshIndicators()
    {
        CaptureForegroundWindow();
        var active = GetActiveApplicationName();
        _appNameButton.Content = active;
        var wifi = NativeMethods.GetWifiState();
        var network = NativeMethods.GetNetworkState();
        bool wifiConnected = wifi.Equals("Wi-Fi connected", StringComparison.OrdinalIgnoreCase);
        _networkText.Text = "";
        _networkText.Opacity = wifiConnected ? 1.0 : 0.52;
        _networkText.ToolTip = wifi + " • " + network;

        if (NativeMethods.GetSystemPowerStatus(out var power) && power.BatteryFlag != 128 && power.BatteryLifePercent != 255)
        {
            var charging = power.ACLineStatus == 1 || (power.BatteryFlag & 8) != 0;
            _batteryText.Text = (charging ? "⚡ " : "▰ ") + power.BatteryLifePercent + "%";
            _batteryText.ToolTip = NativeMethods.GetBatteryState();
            _batteryText.Visibility = Visibility.Visible;
        }
        else
        {
            _batteryText.Text = "";
            _batteryText.ToolTip = "No battery detected";
            _batteryText.Visibility = Visibility.Collapsed;
        }
        _bluetoothText.Visibility = Visibility.Collapsed; // only show a state when the OS query supports it (see note in README)
        _dateText.Text = DateTime.Now.ToString("ddd d MMM", System.Globalization.CultureInfo.CurrentCulture);
        _dateText.ToolTip = DateTime.Now.ToString("dddd, d MMMM yyyy");
        _clockText.Text = DateTime.Now.ToString("h:mm tt", System.Globalization.CultureInfo.CurrentCulture).ToLowerInvariant();
        _clockText.ToolTip = DateTime.Now.ToString("F");
    }

    private void CaptureForegroundWindow()
    {
        try
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return;
            NativeMethods.GetWindowThreadProcessId(foreground, out var processId);
            if (processId == 0 || processId == (uint)_ownProcessId) return;
            var className = new StringBuilder(128);
            NativeMethods.GetClassName(foreground, className, className.Capacity);
            if (className.ToString() == "#32768") return; // Native popup menu; preserve the real foreground app.
            _lastActiveWindow = foreground;
        }
        catch { }
    }

    private string GetActiveApplicationName()
    {
        try
        {
            var hwnd = _lastActiveWindow;
            if (hwnd == IntPtr.Zero) return "Desktop";
            NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0 || processId == (uint)_ownProcessId) return "Desktop";
            using var process = Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            return name.ToLowerInvariant() switch
            {
                "explorer" => "File Explorer", "msedge" => "Microsoft Edge", "chrome" => "Google Chrome",
                "firefox" => "Firefox", "notepad" => "Notepad", "taskmgr" => "Task Manager",
                "devenv" => "Visual Studio", "code" => "Visual Studio Code", "winword" => "Microsoft Word",
                "excel" => "Microsoft Excel", "powerpnt" => "Microsoft PowerPoint", "outlook" => "Microsoft Outlook",
                "teams" or "ms-teams" => "Microsoft Teams", "spotify" => "Spotify", "discord" => "Discord",
                "applicationframehost" => "Windows app", "systemsettings" => "Settings",
                _ => name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : "Desktop"
            };
        }
        catch { return "Desktop"; }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _handle = new WindowInteropHelper(this).Handle;
        var stylePtr = GetWindowLongPtrCompat(_handle, NativeMethods.GWL_EXSTYLE);
        var style = unchecked((int)stylePtr.ToInt64()) | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GWL_EXSTYLE, new IntPtr(style));
        HwndSource.FromHwnd(_handle)?.AddHook(WindowProc);
        PlaceOnMonitor();
        RegisterAppBar();
        ApplyAcrylic();
    }

    private void PlaceOnMonitor()
    {
        var bounds = _screen;
        double scaleX = Math.Max(96, bounds.DpiX) / 96.0;
        double scaleY = Math.Max(96, bounds.DpiY) / 96.0;
        Width = bounds.Width / scaleX;
        Height = 34 / scaleY;
        Left = bounds.Left / scaleX;
        Top = bounds.Top / scaleY;
        NativeMethods.SetWindowPos(_handle, new IntPtr(-1), bounds.Left, bounds.Top, bounds.Width, 34,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_APPBAR_CALLBACK && wParam.ToInt32() == NativeMethods.ABN_POSCHANGED)
        {
            PositionAppBar();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void RegisterAppBar()
    {
        try
        {
            var bounds = _screen;
            var data = new NativeMethods.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = _handle,
                uCallbackMessage = NativeMethods.WM_APPBAR_CALLBACK,
                uEdge = NativeMethods.ABE_TOP,
                rc = new NativeMethods.RECT { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Top + 34 }
            };
            _appBarRegistered = NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref data) != IntPtr.Zero;
            if (_appBarRegistered) PositionAppBar();
        }
        catch { _appBarRegistered = false; }
    }

    private void PositionAppBar()
    {
        try
        {
            var bounds = _screen;
            var data = new NativeMethods.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                hWnd = _handle,
                uCallbackMessage = NativeMethods.WM_APPBAR_CALLBACK,
                uEdge = NativeMethods.ABE_TOP,
                rc = new NativeMethods.RECT { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Top + 34 }
            };
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref data);
            data.rc.Bottom = data.rc.Top + 34;
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref data);
            NativeMethods.SetWindowPos(_handle, new IntPtr(-1), data.rc.Left, data.rc.Top,
                data.rc.Right - data.rc.Left, data.rc.Bottom - data.rc.Top,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);
        }
        catch { /* Keep the bar topmost even if AppBar positioning is unsupported on this shell. */ }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_appBarRegistered && _handle != IntPtr.Zero)
        {
            var data = new NativeMethods.APPBARDATA { cbSize = (uint)Marshal.SizeOf<NativeMethods.APPBARDATA>(), hWnd = _handle };
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref data);
        }
    }

    public void ApplyAppearance()
    {
        bool light = _settings.Appearance == "Light";
        var baseColor = light ? Color.FromRgb(245, 246, 249) : Color.FromRgb(27, 28, 32);
        var alpha = (byte)Math.Clamp((int)(255 * _settings.Opacity / 100.0), 0, 255);
        Root.Background = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
        _textBrush = light ? new SolidColorBrush(Color.FromRgb(26, 27, 31)) : new SolidColorBrush(Color.FromRgb(247, 247, 250));
        _hoverBrush = light ? new SolidColorBrush(Color.FromArgb(35, 20, 25, 35)) : new SolidColorBrush(Color.FromArgb(42, 255, 255, 255));
        Foreground = _textBrush;
        foreach (var button in _left.Children.OfType<Button>())
        {
            button.Foreground = _textBrush;
            button.MouseEnter -= Button_MouseEnter;
            button.MouseEnter += Button_MouseEnter;
        }
        foreach (var indicator in _right.Children.OfType<TextBlock>()) indicator.Foreground = _textBrush;
        // Rebuild the Apple glyph to use the current foreground brush.
        if (_left.Children.Count > 0 && _left.Children[0] is Button apple) apple.Content = CreateAppleMark();
        ApplyAcrylic();
    }

    private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Button button)
        {
            button.Background = _hoverBrush;
            AnimateButtonOpacity(button, 0.96);
        }
    }

    private static void AnimateButtonOpacity(Button button, double target)
    {
        button.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(110))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private static IntPtr GetWindowLongPtrCompat(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64Local(hWnd, nIndex) : new IntPtr(GetWindowLong32Local(hWnd, nIndex));
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)] private static extern IntPtr GetWindowLongPtr64Local(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)] private static extern int GetWindowLong32Local(IntPtr hWnd, int nIndex);

    private void ApplyAcrylic()
    {
        if (_handle == IntPtr.Zero) return;
        try
        {
            bool light = _settings.Appearance == "Light";
            byte a = (byte)Math.Clamp((int)(255 * _settings.Opacity / 100.0), 0, 255);
            byte r = light ? (byte)245 : (byte)27, g = light ? (byte)246 : (byte)28, b = light ? (byte)249 : (byte)32;
            var policy = new NativeMethods.ACCENT_POLICY { AccentState = 4, AccentFlags = 2, GradientColor = unchecked((int)((uint)a << 24 | (uint)b << 16 | (uint)g << 8 | r)) };
            var size = Marshal.SizeOf<NativeMethods.ACCENT_POLICY>();
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, ptr, false);
                var data = new NativeMethods.WINDOWCOMPOSITIONATTRIBDATA { Attribute = 19, Data = ptr, SizeOfData = size };
                NativeMethods.SetWindowCompositionAttribute(_handle, ref data);
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        catch { /* Older Windows builds keep the translucent solid-color background. */ }
    }

    private void WithActiveWindow(Func<IntPtr, bool> action)
    {
        var h = _lastActiveWindow;
        if (h == IntPtr.Zero || !IsWindowStillValid(h)) return;
        try { action(h); } catch { }
    }

    private static bool IsWindowStillValid(IntPtr h)
    {
        NativeMethods.GetWindowThreadProcessId(h, out var pid);
        return pid != 0;
    }

    private void SendShortcut(string key, bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
    {
        var target = _lastActiveWindow;
        if (target != IntPtr.Zero) NativeMethods.SetForegroundWindow(target);
        try
        {
            var modifiers = new List<ushort>();
            if (ctrl) modifiers.Add(0x11);
            if (alt) modifiers.Add(0x12);
            if (shift) modifiers.Add(0x10);
            if (win) modifiers.Add(0x5B);
            ushort vk = KeyToVk(key);
            var inputs = new List<INPUT>();
            foreach (var mod in modifiers) inputs.Add(INPUT.Keyboard(mod, 0));
            inputs.Add(INPUT.Keyboard(vk, 0));
            inputs.Add(INPUT.Keyboard(vk, 2));
            for (int i = modifiers.Count - 1; i >= 0; i--) inputs.Add(INPUT.Keyboard(modifiers[i], 2));
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        }
        catch { }
    }

    private static ushort KeyToVk(string key) => key.ToUpperInvariant() switch
    {
        "F4" => 0x73, "F5" => 0x74, "F11" => 0x7A, "TAB" => 0x09,
        "ADD" => 0x6B, "SUBTRACT" => 0x6D, "0" => 0x30,
        _ when key.Length == 1 && char.IsLetterOrDigit(key[0]) => (ushort)char.ToUpperInvariant(key[0]),
        _ => (ushort)0x20
    };

    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; public static INPUT Keyboard(ushort vk, uint flags) => new() { type = 1, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } } }; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private static void StartProcess(string fileName, string? args = null)
    {
        try
        {
            if (fileName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(args) && !args.StartsWith("/", StringComparison.Ordinal))
                args = $"\"{args.Trim('\"')}\"";
            Process.Start(new ProcessStartInfo(fileName, args ?? "") { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show($"Could not open {fileName}.\n\n{ex.Message}", "MacTopBar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private static void SafeAction(Action action)
    {
        try { action(); }
        catch (Exception ex) { MessageBox.Show($"The action could not be completed.\n\n{ex.Message}", "MacTopBar", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
