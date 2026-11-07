namespace PPTBoard.Ink;

/// <summary>一页白板：笔迹列表 + 独立撤销栈。</summary>
public sealed class InkPage
{
    private readonly List<InkStroke> _strokes = new();

    public int Index { get; internal set; }
    public IReadOnlyList<InkStroke> Strokes => _strokes;
    public bool IsEmpty => _strokes.Count == 0;

    internal UndoStack History { get; } = new();

    internal void Add(InkStroke stroke) => _strokes.Add(stroke);

    internal void Insert(int index, InkStroke stroke) => _strokes.Insert(index, stroke);

    internal bool Remove(InkStroke stroke) => _strokes.Remove(stroke);

    internal void RemoveAt(int index) => _strokes.RemoveAt(index);

    internal void Clear() => _strokes.Clear();
}
