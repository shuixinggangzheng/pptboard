using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

public partial class PenDockWindow : OverlayWindow
{
    private readonly InkEngine _engine;
    private bool _syncing;
    private bool _ready;
    private Button? _selectedColorButton;

    public event EventHandler? CustomColorRequested;
    public event EventHandler? StyleChanged;

    public PenDockWindow(LayoutSpec layout, InkEngine engine)
    {
        InitializeComponent();
        Left = layout.PenDock.X;
        Top = layout.PenDock.Y;
        _engine = engine;
        _ready = true;
        Refresh();
    }

    public void Refresh()
    {
        _syncing = true;
        var style = _engine.Style;
        WidthSlider.Value = style.Width;
        _syncing = false;
        SelectColor(style.Color);
        UpdateWidthSelection(style.Width);
    }

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag) return;
        int index = int.Parse(tag);
        var color = InkColor.Presets[index];
        _engine.SetTool(InkTool.Pen);
        _engine.SetStyle(new PenStyle(color, _engine.Style.Width));
        SelectColor(color);
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCustomClick(object sender, RoutedEventArgs e)
    {
        Log.Info("pen dock custom click");
        foreach (var button in new[] { Preset0, Preset1, Preset2 })
            SetRing(button, false);
        SetRing(CustomButton, true);
        _selectedColorButton = CustomButton;
        CustomColorRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnWidthPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag) return;
        double width = double.Parse(tag);
        _syncing = true;
        WidthSlider.Value = width;
        _syncing = false;
        ApplyWidth(width);
    }

    private void OnWidthSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !_ready) return;
        double width = e.NewValue;
        double nearest = PenStyle.WidthPresets
            .OrderBy(p => Math.Abs(p - width))
            .First();
        if (Math.Abs(nearest - width) <= 1.5) width = nearest;
        ApplyWidth(width);
    }

    private void ApplyWidth(double width)
    {
        _engine.SetTool(InkTool.Pen);
        _engine.SetStyle(new PenStyle(_engine.Style.Color, width));
        UpdateWidthSelection(width);
        StyleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectColor(InkColor color)
    {
        var buttons = new[] { Preset0, Preset1, Preset2 };
        Button? match = null;
        for (int i = 0; i < InkColor.Presets.Length; i++)
        {
            var preset = InkColor.Presets[i];
            if (preset.A == color.A && preset.R == color.R && preset.G == color.G && preset.B == color.B)
            {
                match = buttons[i];
                break;
            }
        }

        foreach (var button in buttons) SetRing(button, ReferenceEquals(button, match));
        SetRing(CustomButton, match is null);
        _selectedColorButton = match ?? CustomButton;
    }

    private void UpdateWidthSelection(double width)
    {
        var buttons = new[] { Width0, Width1, Width2, Width3, Width4 };
        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = Math.Abs(PenStyle.WidthPresets[i] - width) < 0.5;
            buttons[i].Background = Brushes.Transparent;
            if (buttons[i].Content is Ellipse ellipse)
            {
                ellipse.Stroke = selected
                    ? new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xF6))
                    : Brushes.Transparent;
                ellipse.StrokeThickness = selected ? 2 : 0;
            }
        }
    }

    private static void SetRing(Button button, bool selected)
    {
        if (button.Content is not Ellipse ellipse) return;
        ellipse.Stroke = selected ? new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xF6)) : new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
        ellipse.StrokeThickness = selected ? 2.5 : 1;
    }
}
