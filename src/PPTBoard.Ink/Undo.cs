using PPTBoard.Core.Geometry;

namespace PPTBoard.Ink;

internal interface IUndoAction
{
    RectD Undo(InkPage page);
}

internal sealed class UndoStack
{
    private const int MaxDepth = 100;
    private readonly List<IUndoAction> _items = new();

    public int Count => _items.Count;

    public void Push(IUndoAction action)
    {
        _items.Add(action);
        if (_items.Count > MaxDepth)
            _items.RemoveAt(0);
    }

    public RectD? Undo(InkPage page)
    {
        if (_items.Count == 0) return null;
        int last = _items.Count - 1;
        var action = _items[last];
        _items.RemoveAt(last);
        return action.Undo(page);
    }
}

internal sealed class AddStrokeAction(InkStroke stroke) : IUndoAction
{
    public RectD Undo(InkPage page)
    {
        page.Remove(stroke);
        return stroke.Bounds;
    }
}

/// <summary>一次擦除手势里删掉的所有笔迹；按删除的逆序插回原位。</summary>
internal sealed class RemoveStrokesAction : IUndoAction
{
    private readonly List<(int Index, InkStroke Stroke)> _removed = new();

    public bool HasRemovals => _removed.Count > 0;

    public RectD Dirty { get; private set; } = RectD.Empty;

    public void Add(int index, InkStroke stroke)
    {
        _removed.Add((index, stroke));
        Dirty = RectD.Union(Dirty, stroke.Bounds);
    }

    public RectD Undo(InkPage page)
    {
        RectD dirty = RectD.Empty;
        for (int i = _removed.Count - 1; i >= 0; i--)
        {
            var (index, stroke) = _removed[i];
            page.Insert(Math.Clamp(index, 0, page.Strokes.Count), stroke);
            dirty = RectD.Union(dirty, stroke.Bounds);
        }
        return dirty;
    }
}

internal sealed class ClearPageAction(IReadOnlyList<InkStroke> snapshot) : IUndoAction
{
    public RectD Undo(InkPage page)
    {
        RectD dirty = RectD.Empty;
        foreach (var stroke in snapshot)
        {
            page.Add(stroke);
            dirty = RectD.Union(dirty, stroke.Bounds);
        }
        return dirty;
    }
}
