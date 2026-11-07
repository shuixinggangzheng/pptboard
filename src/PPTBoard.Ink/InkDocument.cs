using PPTBoard.Core.Geometry;

namespace PPTBoard.Ink;

/// <summary>多页白板文档。页与页之间笔迹、撤销栈完全独立。</summary>
public sealed class InkDocument
{
    private readonly List<InkPage> _pages = new();
    private int _current;

    public InkDocument()
    {
        var first = new InkPage { Index = 0 };
        _pages.Add(first);
    }

    public event Action<RectD>? Invalidated;
    public event Action? ContentChanged;
    public event Action? PagesChanged;
    public event Action? CurrentPageChanged;

    public IReadOnlyList<InkPage> Pages => _pages;
    public InkPage Current => _pages[_current];
    public int CurrentIndex => _current;
    public int PageCount => _pages.Count;

    public int AddPageAfterCurrent()
    {
        var page = new InkPage();
        int at = _current + 1;
        _pages.Insert(at, page);
        Reindex();
        _current = at;
        PagesChanged?.Invoke();
        CurrentPageChanged?.Invoke();
        ContentChanged?.Invoke();
        return _current;
    }

    public bool DeleteCurrentPage() => DeletePage(_current);

    public bool DeletePage(int index)
    {
        if (_pages.Count <= 1) return false;
        if (index < 0 || index >= _pages.Count) return false;

        _pages.RemoveAt(index);
        if (index < _current) _current--;
        if (_current >= _pages.Count) _current = _pages.Count - 1;
        if (_current < 0) _current = 0;
        Reindex();
        PagesChanged?.Invoke();
        CurrentPageChanged?.Invoke();
        ContentChanged?.Invoke();
        return true;
    }

    /// <summary>清空所有页，只留一张空白页（退出放映时调用）。</summary>
    public void ResetAll()
    {
        _pages.Clear();
        _pages.Add(new InkPage { Index = 0 });
        _current = 0;
        PagesChanged?.Invoke();
        CurrentPageChanged?.Invoke();
        ContentChanged?.Invoke();
    }

    public bool GoTo(int index)
    {
        if (index < 0 || index >= _pages.Count) return false;
        if (index == _current) return false;
        _current = index;
        CurrentPageChanged?.Invoke();
        return true;
    }

    internal void RaiseInvalidated(RectD region)
    {
        if (!region.IsEmpty) Invalidated?.Invoke(region);
    }

    internal void RaiseContentChanged() => ContentChanged?.Invoke();

    private void Reindex()
    {
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].Index = i;
    }
}