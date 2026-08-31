using Godot;
using System;
using System.IO;
using System.Text.Json;
using Deckrinth.Model;

namespace Deckrinth.Services;

public static class SaveManager
{
    // Godot's user:// path resolves to %APPDATA%\Godot\app_userdata\Deckrinth on Windows
    private static readonly string SAVE_DIR = ProjectSettings.GlobalizePath("user://Saves");
    
    // Formatting options for clean, readable JSON files (helpful for debugging your thesis)
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
    { 
        WriteIndented = true 
    };

    public static void Initialize()
    {
        if (!Directory.Exists(SAVE_DIR))
        {
            Directory.CreateDirectory(SAVE_DIR);
        }
    }

    // --- META-PROGRESSION (SLOT SAVES) ---

    public static void SaveProfile(PlayerProfile profile, int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"profile_slot_{slotIndex}.json");
        try
        {
            string jsonString = JsonSerializer.Serialize(profile, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to save profile: {e.Message}");
        }
    }

    public static PlayerProfile LoadProfile(int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"profile_slot_{slotIndex}.json");
        if (!File.Exists(filePath)) return null;

        try
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PlayerProfile>(jsonString, _jsonOptions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load profile: {e.Message}");
            return null;
        }
    }

    // --- MID-RUN SAVES ---

    public static void SaveActiveRun(RunState runState, int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"active_run_slot_{slotIndex}.json");
        try
        {
            string jsonString = JsonSerializer.Serialize(runState, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to save active run: {e.Message}");
        }
    }

    public static RunState LoadActiveRun(int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"active_run_slot_{slotIndex}.json");
        if (!File.Exists(filePath)) return null;

        try
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<RunState>(jsonString, _jsonOptions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Failed to load active run: {e.Message}");
            return null;
        }
    }

    public static void DeleteActiveRun(int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"active_run_slot_{slotIndex}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    public static bool HasActiveRun(int slotIndex)
    {
        string filePath = Path.Combine(SAVE_DIR, $"active_run_slot_{slotIndex}.json");
        return File.Exists(filePath);
    }
}