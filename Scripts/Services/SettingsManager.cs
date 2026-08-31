using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace Deckrinth.Services;

public class SettingsState
{
    public float Volume { get; set; } = 50f;
    public bool CrtEnabled { get; set; } = true;
    public float CrtIntensity { get; set; } = 0.5f;
    public int WindowModeIndex { get; set; } = 1; // 0 = Window, 1 = Borderless, 2 = Fullscreen
    public int FpsLimit { get; set; } = 120; // 60, 120, 144
    public float AnimSpeedMultiplier { get; set; } = 1.0f; // 1, 2, 4
    public bool ShowHighlights { get; set; } = true;
}

public static class SettingsManager
{
    private static readonly string SETTINGS_FILE = ProjectSettings.GlobalizePath("user://settings.json");
    public static SettingsState Current { get; private set; }

    // Quick accessors for convenience
    public static float AnimDurationMultiplier => 1f / Current.AnimSpeedMultiplier; // If speed is 2x, duration is halved
    public static bool ShowHighlights => Current.ShowHighlights;

    public static void LoadAndApply()
    {
        if (File.Exists(SETTINGS_FILE))
        {
            try
            {
                string json = File.ReadAllText(SETTINGS_FILE);
                Current = JsonSerializer.Deserialize<SettingsState>(json) ?? new SettingsState();
            }
            catch { Current = new SettingsState(); }
        }
        else
        {
            Current = new SettingsState();
        }
        
        ApplyEngineSettings();
    }

    public static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SETTINGS_FILE, json);
        }
        catch (Exception e) { GD.PrintErr($"Failed to save settings: {e.Message}"); }
    }

    public static void ApplyEngineSettings()
    {
        Engine.MaxFps = Current.FpsLimit;

        switch (Current.WindowModeIndex)
        {
            case 0:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                break;
            case 1:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                // Center the window on the screen
                Vector2I screenSize = DisplayServer.ScreenGetSize();
                Vector2I windowSize = DisplayServer.WindowGetSize();
                DisplayServer.WindowSetPosition((screenSize - windowSize) / 2);
                break;
            case 2:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
        }
    }
}