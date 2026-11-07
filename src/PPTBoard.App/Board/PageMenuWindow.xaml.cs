using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PPTBoard.App.Host;
using PPTBoard.Core.Geometry;
using PPTBoard.Ink;

namespace PPTBoard.App.Board;

public partial class PageMenuWindow : OverlayWindow
{
    private readonly LayoutSpec _layout;
    private readonly InkEngine _engine;
    private bool _rebuilding;

    public PageMenuWindow(LayoutSpec layout, InkEngine engine)
    {
        InitializeComponent();
        _layout = layout;
        _engine = engine;
        Left = layout.PageMenu.X;
        Top = 0;
        Height = layout.PageMenu.Height;

        engine.Document.PagesChanged += ScheduleRefresh;
        engine.Document.ContentChanged += ScheduleRefresh;
    }

    public void Prepare() => Refresh();

    public void Open()
    {
        Refresh();
        if (!IsVisible) Show();
        RaiseTopmost();
    }

    private void ScheduleRefresh()
    {
        if (!IsVisible || _rebuilding) return;
        Dispatcher.BeginInvoke(new Action(Refresh));
    }

    private void Refresh()
    {
        if (_rebuilding) return;
        _rebuilding = true;
        try
        {
            ItemsPanel.Children.Clear();
            var doc = _engine.Document;
            double sourceWidth = Math.Max(1, _layout.DrawingArea.Width);
            double sourceHeight = Math.Max(1, _layout.DrawingArea.Height);

            for (int i = 0; i < doc.PageCount; i++)
            {
                int index = i;
                var page = doc.Pages[i];
                var thumb = PageThumbnail.Render(page, sourceWidth, sourceHeight);
                ItemsPanel.Children.Add(BuildItem(index, thumb, index == doc.CurrentIndex));
            }
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private FrameworkElement BuildItem(int index, BitmapSource thumb, bool current)
    {
        var border = new Border
        {
            Width = 140,
            Height = 108,
            Margin = new Thickness(4),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
            BorderBrush = current
                ? new SolidColorBrush(Color.FromRgb(0x2F, 0x7C, 0xF6))
                : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(current ? 2 : 1),
            Cursor = Cursors.Hand
        };

        var grid = new Grid();
        grid.Children.Add(new Image
        {
            Source = thumb,
            Stretch = Stretch.Fill,
            Margin = new Thickness(4, 4, 4, 22)
        });

        var number = new TextBlock
        {
            Text = (index + 1).ToString(),
            Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(8, 0, 0, 6)
        };
        grid.Children.Add(number);

        var delete = new Button
        {
            Content = "×",
            Width = 22,
            Height = 22,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            Cursor = Cursors.Hand,
            ToolTip = "删除本页（点两次）"
        };
        bool armed = false;
        delete.Click += (_, e) =>
        {
            e.Handled = true;
            if (!armed)
            {
                armed = true;
                delete.Content = "确";
                delete.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
                return;
            }

            if (_engine.Document.DeletePage(index))
            {
                ScheduleRefresh();
            }
        };
        grid.Children.Add(delete);

        border.Child = grid;
        border.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            _engine.Document.GoTo(index);
            Hide();
        };

        return border;
    }

    private void OnNewPageClick(object sender, RoutedEventArgs e)
    {
        _engine.Document.AddPageAfterCurrent();
        Refresh();
    }

    private void OnCollapseClick(object sender, RoutedEventArgs e) => Hide();
}
