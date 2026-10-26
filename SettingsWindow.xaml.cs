using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacTopBar;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        AppearanceBox.SelectedIndex = settings.Appearance == "Light" ? 1 : 0;
        OpacitySlider.Value = settings.Opacity;
        StartupBox.IsChecked = settings.RunAtStartup;
        ApplyDialogTheme();
    }

    private void ApplyDialogTheme()
    {
        bool light = _settings.Appearance == "Light";
        Background = light ? new SolidColorBrush(Color.FromRgb(246, 247, 250)) : new SolidColorBrush(Color.FromRgb(34, 35, 40));
        var foreground = light ? Brushes.Black : Brushes.White;
        foreach (var item in FindVisualChildren<TextBlock>(this)) item.Foreground = foreground;
        StartupBox.Foreground = foreground;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) yield return typed;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel is not null) OpacityLabel.Text = $"{(int)OpacitySlider.Value}%";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings.Appearance = (AppearanceBox.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Light" ? "Light" : "Dark";
            _settings.Opacity = Math.Clamp((int)Math.Round(OpacitySlider.Value), 55, 100);
            _settings.RunAtStartup = StartupBox.IsChecked == true;
            _settings.Save();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save settings. If startup registration failed, check Windows permissions and try again.\n\n" + ex.Message,
                "MacTopBar Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
