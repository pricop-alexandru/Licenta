using Godot;
using System;
using Deckrinth.Services;

namespace Deckrinth.View;

public partial class SettingsMenuUI : Control
{
    [Export] public HSlider VolumeSlider { get; set; }
    [Export] public CheckBox CrtToggle { get; set; }
    [Export] public HSlider CrtSlider { get; set; }
    [Export] public OptionButton WindowModeDropdown { get; set; }
    [Export] public OptionButton FpsDropdown { get; set; }
    [Export] public OptionButton AnimSpeedDropdown { get; set; }
    [Export] public CheckBox HighlightToggle { get; set; }
    [Export] public Button CloseButton { get; set; }
    
    // Material Shader that will be updated based on the CRT settings
    [Export] public ShaderMaterial CrtMaterial { get; set; }

    public event Action OnCloseRequested;

    public void Setup()
    {
        SettingsManager.LoadAndApply();
        UpdateUIFromState();
        UpdateShader();

        // Connecting events
        VolumeSlider.ValueChanged += (val) => { SettingsManager.Current.Volume = (float)val; };
        
        CrtToggle.Toggled += (val) => { 
            SettingsManager.Current.CrtEnabled = val; 
            UpdateShader(); 
        };
        
        CrtSlider.ValueChanged += (val) => { 
            SettingsManager.Current.CrtIntensity = (float)val; 
            UpdateShader(); 
        };

        WindowModeDropdown.ItemSelected += (idx) => {
            SettingsManager.Current.WindowModeIndex = (int)idx;
            SettingsManager.ApplyEngineSettings();
        };

        FpsDropdown.ItemSelected += (idx) => {
            int fps = idx == 0 ? 60 : (idx == 1 ? 120 : 144);
            SettingsManager.Current.FpsLimit = fps;
            SettingsManager.ApplyEngineSettings();
        };

        AnimSpeedDropdown.ItemSelected += (idx) => {
            float speed = idx == 0 ? 1f : (idx == 1 ? 2f : 4f);
            SettingsManager.Current.AnimSpeedMultiplier = speed;
        };

        HighlightToggle.Toggled += (val) => { SettingsManager.Current.ShowHighlights = val; };

        CloseButton.Pressed += () => {
            SettingsManager.Save();
            Visible = false;
            OnCloseRequested?.Invoke();
        };
    }

    private void UpdateUIFromState()
    {
        var state = SettingsManager.Current;
        VolumeSlider.Value = state.Volume;
        CrtToggle.ButtonPressed = state.CrtEnabled;
        CrtSlider.Value = state.CrtIntensity;
        WindowModeDropdown.Selected = state.WindowModeIndex;
        
        FpsDropdown.Selected = state.FpsLimit == 60 ? 0 : (state.FpsLimit == 120 ? 1 : 2);
        AnimSpeedDropdown.Selected = state.AnimSpeedMultiplier == 1f ? 0 : (state.AnimSpeedMultiplier == 2f ? 1 : 2);
        
        HighlightToggle.ButtonPressed = state.ShowHighlights;
    }

    private void UpdateShader()
    {
        if (CrtMaterial != null)
        {
            // We set the shader parameters based on the current settings
            CrtMaterial.SetShaderParameter("enabled", SettingsManager.Current.CrtEnabled);
            CrtMaterial.SetShaderParameter("intensity", SettingsManager.Current.CrtIntensity);
        }
    }
}