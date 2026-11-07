using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;
using PPTBoard.Core.Models;

namespace PPTBoard.App.Dock;

public partial class ColorPanelWindow : OverlayWindow
{
    private readonly LayoutSpec _layout;
    private bool _syncing;
    private double _hue;
    private double _brightness = 50;
    private double _alpha = 100;
    private InkColor _lastCustom = InkColor.Black;
    private bool _ready;

    public event Action<InkColor>? ColorChanged;

    public ColorPanelWindow(LayoutSpec layout)
    {
        InitializeComponent();
        _layout = layout;
        Wheel.HueChanged += hue => { _hue = hue; Emit(); };
        _ready = true;
        UpdatePreview();
    }

    public void Open(InkColor initial, RectD anchor)
    {
        LoadColor(initial, emit: false);

        double width = Width;
        double height = Height;
        double x = Math.Clamp(anchor.CenterX - width / 2, 12, _layout.ScreenWidth - width - 12);
        double y = anchor.Top - height - 12;
        if (y < 12) y = Math.Clamp(anchor.Bottom + 12, 12, _layout.ScreenHeight - height - 12);
        Left = x;
        Top = y;

        UpdatePreview();
        if (!IsVisible) Show();
        RaiseTopmost();
    }

    private void OnBrightnessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !_ready) return;
        _brightness = e.NewValue;
        BrightnessText.Text = $"{_brightness:0}%";
        Emit();
    }

    private void OnAlphaChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !_ready) return;
        _alpha = e.NewValue;
        AlphaText.Text = $"{_alpha:0}%";
        Emit();
    }

    private void OnPresetRed(object sender, RoutedEventArgs e) => ApplyPreset(InkColor.Red);
    private void OnPresetBlue(object sender, RoutedEventArgs e) => ApplyPreset(InkColor.Blue);
    private void OnPresetBlack(object sender, RoutedEventArgs e) => ApplyPreset(InkColor.Black);

    private void ApplyPreset(InkColor color)
    {
        LoadColor(color, emit: true);
    }

    private void LoadColor(InkColor color, bool emit)
    {
        var (h, s, v) = ColorMath.RgbToHsv(color.R, color.G, color.B);
        _hue = h;
        // 0=黑，50=色相本色，100=白；与 WithBrightness 的模型对齐
        _brightness = v <= 0.01 ? 0 : (s <= 0.01 && v >= 0.99 ? 100 : 50);
        _alpha = Math.Clamp(color.A * 100.0 / 255.0, 10, 100);

        _syncing = true;
        Wheel.Hue = _hue;
        BrightnessSlider.Value = _brightness;
        AlphaSlider.Value = _alpha;
        BrightnessText.Text = $"{_brightness:0}%";
        AlphaText.Text = $"{_alpha:0}%";
        _syncing = false;

        UpdatePreview();
        if (emit) Emit();
    }

    private void OnDoneClick(object sender, RoutedEventArgs e) => Hide();

    private void Emit()
    {
        var color = CurrentColor();
        _lastCustom = color;
        UpdatePreview(color);
        ColorChanged?.Invoke(color);
    }

    private InkColor CurrentColor()
    {
        var baseColor = ColorMath.HsvToRgb(_hue, 1, 1);
        byte alpha = (byte)Math.Round(Math.Clamp(_alpha, 10, 100) / 100.0 * 255);
        return baseColor.WithBrightness(_brightness / 100.0).WithAlpha(alpha);
    }

    private void UpdatePreview(InkColor? color = null)
    {
        var c = color ?? CurrentColor();
        Wheel.PreviewColor = Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
