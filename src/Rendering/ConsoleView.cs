using Godot;
using System;
using System.Collections.Generic;

namespace Terrabellum.Rendering;

public partial class ConsoleView : PanelContainer
{
    [Signal]
    public delegate void CommandSubmittedEventHandler(string command);

    private VBoxContainer _logContainer = new();
    private ScrollContainer _scrollContainer = new();
    private LineEdit _inputLine = new();
    private Button _toggleButton = new();
    private const int MaxLogLines = 100;
    private const float ExpandedHeight = 180f; // Slightly taller to fit input
    private const float MinimizedHeight = 18f;
    private bool _isMinimized = false;

    public bool IsMouseOver { get; private set; }

    public override void _Ready()
    {
        Name = "ConsoleView";

        // Setup Styling
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0.5f); // Semi-transparent black
        styleBox.SetContentMarginAll(2);
        AddThemeStyleboxOverride("panel", styleBox);

        // Layout: Bottom-centered, spanning most of the width
        SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        UpdateLayout();

        OffsetLeft = 50;
        OffsetRight = -50;
        OffsetBottom = -2;

        // Main Layout (VBox for Scroll + Input)
        var mainLayout = new VBoxContainer();
        mainLayout.AddThemeConstantOverride("separation", 2);
        AddChild(mainLayout);

        // Scroll Container
        _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.ShowAlways;
        _scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        mainLayout.AddChild(_scrollContainer);

        _scrollContainer.MouseEntered += () => IsMouseOver = true;
        _scrollContainer.MouseExited += () => IsMouseOver = false;

        // VBox for log lines
        _logContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scrollContainer.AddChild(_logContainer);

        // Input Line
        _inputLine.PlaceholderText = "Enter command...";
        _inputLine.Hide();
        _inputLine.TextSubmitted += (text) => 
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                EmitSignal(SignalName.CommandSubmitted, text);
            }
            _inputLine.Clear();
            _inputLine.Hide();
        };
        _inputLine.GuiInput += (@event) => 
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
            {
                _inputLine.Clear();
                _inputLine.Hide();
                GetViewport().SetInputAsHandled();
            }
        };
        mainLayout.AddChild(_inputLine);

        // Toggle Button (Overlay)
        var buttonContainer = new Control();
        buttonContainer.MouseFilter = MouseFilterEnum.Ignore;
        buttonContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(buttonContainer);

        _toggleButton.Text = "▼";
        _toggleButton.CustomMinimumSize = new Vector2(20, 16);
        _toggleButton.AddThemeFontSizeOverride("font_size", 10);
        _toggleButton.Pressed += ToggleMinimized;

        // Remove button background
        var emptyStyle = new StyleBoxEmpty();
        _toggleButton.AddThemeStyleboxOverride("normal", emptyStyle);
        _toggleButton.AddThemeStyleboxOverride("hover", emptyStyle);
        _toggleButton.AddThemeStyleboxOverride("pressed", emptyStyle);
        _toggleButton.AddThemeStyleboxOverride("focus", emptyStyle);
        _toggleButton.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        _toggleButton.AddThemeColorOverride("font_hover_color", Colors.White);

        _toggleButton.SetAnchorsPreset(LayoutPreset.TopRight);
        _toggleButton.GrowHorizontal = GrowDirection.Begin;
        _toggleButton.GrowVertical = GrowDirection.End;
        buttonContainer.AddChild(_toggleButton);

        // Initial message
        AddEvent("System: Tabletop console initialized.", 1);
    }

    public void ActivateInput()
    {
        if (_isMinimized) ToggleMinimized();
        _inputLine.Show();
        _inputLine.GrabFocus();
    }

    private void ToggleMinimized()
    {
        _isMinimized = !_isMinimized;
        _scrollContainer.Visible = !_isMinimized;
        _toggleButton.Text = _isMinimized ? "▲" : "▼";
        if (_isMinimized) _inputLine.Hide();
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        OffsetTop = _isMinimized ? -MinimizedHeight : -ExpandedHeight;
    }

    public void AddEvent(string text, int turn = 1)
    {
        var label = new RichTextLabel();
        label.BbcodeEnabled = true;
        label.Text = $"[color=#cccccc][Turn {turn}][/color] {text}";
        label.FitContent = true;
        label.SelectionEnabled = true;
        
        _logContainer.AddChild(label);

        // Maintain max lines
        if (_logContainer.GetChildCount() > MaxLogLines)
        {
            var first = _logContainer.GetChild(0);
            _logContainer.RemoveChild(first);
            first.QueueFree();
        }

        // Auto-scroll to bottom
        CallDeferred(MethodName.ScrollToBottom);
    }

    private void ScrollToBottom()
    {
        var vScroll = _scrollContainer.GetVScrollBar();
        _scrollContainer.ScrollVertical = (int)vScroll.MaxValue;
    }
}
