using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace MacTopBar;

public sealed class AppSettings
{
    public string Appearance { get; set; } = "Dark";
    public int Opacity { get; set; } = 88;
    public bool RunAtStartup { get; set; } = false;

    private static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MacTopBar");
    private static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    loaded.Opacity = Math.Clamp(loaded.Opacity, 55, 100);
                    loaded.Appearance = loaded.Appearance == "Light" ? "Light" : "Dark";
                    return loaded;
                }
            }
        }
        catch { /* Corrupt preferences are replaced with safe defaults. */ }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        SetStartup(this.RunAtStartup);
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (!enabled)
        {
            key?.DeleteValue("MacTopBar", false);
            return;
        }

        var executable = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "MacTopBar.exe");
        string command;
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(entry)) throw new InvalidOperationException("Could not determine the application entry point. Publish MacTopBar.exe first, then enable startup.");
            command = $"\"{executable}\" \"{entry}\"";
        }
        else
        {
            command = $"\"{executable}\"";
        }
        key?.SetValue("MacTopBar", command, RegistryValueKind.String);
    }
}
