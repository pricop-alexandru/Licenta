using Godot;
using System;

namespace Deckrinth.View;

public partial class CameraController : Camera2D
{
    [ExportCategory("Zoom Settings")]
    [Export] public float ZoomStep = 0.15f; // How much it zooms per scroll
    [Export] public float MinZoom = 0.5f;   // Max zoom out (half size)
    [Export] public float MaxZoom = 2.0f;   // Max zoom in (double size)
    [Export] public float ZoomSmoothness = 10f; // Higher is faster/snappier

    [ExportCategory("Pan Settings")]
    [Export] public MouseButton PanButton = MouseButton.Middle;

    private Vector2 _targetZoom = Vector2.One;
    private bool _isPanning = false;

    public override void _Ready()
    {
        // Initialize target zoom to the camera's starting zoom
        _targetZoom = Zoom;
        
        // Ensure the camera follows its own position rather than staying glued to a parent
        PositionSmoothingEnabled = true; 
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // 1. Handle Mouse Button Presses (Scroll & Pan Start/Stop)
        if (@event is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.WheelUp && mouseBtn.Pressed)
            {
                ApplyZoom(ZoomStep);
            }
            else if (mouseBtn.ButtonIndex == MouseButton.WheelDown && mouseBtn.Pressed)
            {
                ApplyZoom(-ZoomStep);
            }

            if (mouseBtn.ButtonIndex == PanButton)
            {
                _isPanning = mouseBtn.Pressed;
            }
        }
        // 2. Handle Mouse Movement (Panning)
        else if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            // Move the camera in the opposite direction of the mouse drag.
            // We divide by current Zoom so panning feels consistent regardless of zoom level.
            Position -= mouseMotion.Relative / Zoom;
        }
    }

    public override void _Process(double delta)
    {
        // Smoothly interpolate the current zoom towards the target zoom
        if (Zoom.DistanceTo(_targetZoom) > 0.001f)
        {
            Zoom = Zoom.Lerp(_targetZoom, (float)delta * ZoomSmoothness);
        }
    }

    private void ApplyZoom(float amount)
    {
        _targetZoom += new Vector2(amount, amount);
        
        // Clamp the zoom to prevent breaking the pixel art readability
        _targetZoom.X = Math.Clamp(_targetZoom.X, MinZoom, MaxZoom);
        _targetZoom.Y = Math.Clamp(_targetZoom.Y, MinZoom, MaxZoom);
    }
    
    // Public method to be called by GameLoopManager if we want to snap to a specific event
    public void FocusOnPosition(Vector2 globalPos)
    {
        Position = globalPos;
    }
    public void ResetCamera()
    {
        Position = Vector2.Zero;
        _targetZoom = Vector2.One; 
        Zoom = Vector2.One;
    }
}