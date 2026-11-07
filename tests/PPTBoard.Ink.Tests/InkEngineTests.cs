using PPTBoard.Core.Models;
using PPTBoard.Ink;
using Xunit;

namespace PPTBoard.Ink.Tests;

public class InkEngineTests
{
    private static InkEngine NewEngine() => new();

    private static void DrawLine(InkEngine e, int pointerId, double x1, double y1, double x2, double y2)
    {
        e.PointerDown(pointerId, x1, y1);
        e.PointerMove(pointerId, (x1 + x2) / 2, (y1 + y2) / 2);
        e.PointerMove(pointerId, x2, y2);
        e.PointerUp(pointerId);
    }

    [Fact]
    public void SinglePointer_CreatesOneCompletedStroke()
    {
        var e = NewEngine();
        DrawLine(e, 1, 10, 10, 110, 10);

        Assert.Single(e.Document.Current.Strokes);
        var stroke = e.Document.Current.Strokes[0];
        Assert.False(stroke.IsActive);
        Assert.Equal(3, stroke.Points.Count);
        Assert.Equal(InkColor.Black, stroke.Color);
        Assert.Equal(6, stroke.Width);
    }

    [Fact]
    public void MultiTouch_TwoPointers_TwoIndependentStrokes()
    {
        var e = NewEngine();
        e.PointerDown(1, 10, 10);
        e.PointerDown(2, 10, 100);
        e.PointerMove(1, 90, 10);
        e.PointerMove(2, 190, 100);
        e.PointerUp(1);
        e.PointerMove(2, 290, 100);
        e.PointerUp(2);

        Assert.Equal(2, e.Document.Current.Strokes.Count);
        Assert.All(e.Document.Current.Strokes, s => Assert.False(s.IsActive));
        Assert.Equal(2, e.Document.Current.Strokes[0].Points.Count);
        Assert.Equal(3, e.Document.Current.Strokes[1].Points.Count);
    }

    [Fact]
    public void StyleChange_DoesNotAffectExistingStroke()
    {
        var e = NewEngine();
        e.PointerDown(1, 10, 10);
        e.SetStyle(new PenStyle(InkColor.Red, 16));
        e.PointerMove(1, 60, 10);
        e.PointerUp(1);

        Assert.Equal(InkColor.Black, e.Document.Current.Strokes[0].Color);
        Assert.Equal(6, e.Document.Current.Strokes[0].Width);
    }

    [Fact]
    public void Undo_RemovesLastCompletedStroke_ButNotActiveOne()
    {
        var e = NewEngine();
        DrawLine(e, 1, 10, 10, 60, 10);
        e.PointerDown(2, 100, 100);
        e.PointerMove(2, 150, 100);

        bool undone = e.Undo();

        Assert.True(undone);
        Assert.Single(e.Document.Current.Strokes);
        Assert.True(e.Document.Current.Strokes[0].IsActive);
    }

    [Fact]
    public void Eraser_RemovesWholeStroke_AndUndoRestoresIt()
    {
        var e = NewEngine();
        DrawLine(e, 1, 100, 100, 300, 100);
        Assert.Single(e.Document.Current.Strokes);

        e.SetTool(InkTool.Eraser);
        e.PointerDown(2, 200, 100);
        e.PointerMove(2, 210, 100);
        e.PointerUp(2);

        Assert.Empty(e.Document.Current.Strokes);

        Assert.True(e.Undo());
        Assert.Single(e.Document.Current.Strokes);
        Assert.Equal(3, e.Document.Current.Strokes[0].Points.Count);
    }

    [Fact]
    public void EraserGesture_IsOneUndoStep()
    {
        var e = NewEngine();
        DrawLine(e, 1, 100, 100, 300, 100);
        DrawLine(e, 1, 100, 200, 300, 200);

        e.SetTool(InkTool.Eraser);
        e.PointerDown(2, 200, 100);
        e.PointerMove(2, 200, 200);
        e.PointerUp(2);
        Assert.Empty(e.Document.Current.Strokes);

        e.Undo();
        Assert.Equal(2, e.Document.Current.Strokes.Count);
    }

    [Fact]
    public void ClearPage_IsUndoable()
    {
        var e = NewEngine();
        DrawLine(e, 1, 10, 10, 60, 10);
        DrawLine(e, 1, 10, 40, 60, 40);

        Assert.True(e.ClearCurrentPage());
        Assert.Empty(e.Document.Current.Strokes);

        Assert.True(e.Undo());
        Assert.Equal(2, e.Document.Current.Strokes.Count);
    }

    [Fact]
    public void Pages_HaveIndependentStrokesAndUndoStacks()
    {
        var e = NewEngine();
        DrawLine(e, 1, 10, 10, 60, 10);

        int page2 = e.Document.AddPageAfterCurrent();
        Assert.Equal(1, page2);
        DrawLine(e, 1, 10, 10, 60, 10);

        e.Undo();
        Assert.Empty(e.Document.Current.Strokes);

        Assert.True(e.Document.GoTo(0));
        Assert.Single(e.Document.Current.Strokes);
    }

    [Fact]
    public void DeleteLastPage_IsRefused()
    {
        var e = NewEngine();
        Assert.False(e.Document.DeleteCurrentPage());
        Assert.Equal(1, e.Document.PageCount);
    }

    [Fact]
    public void Clear_OnEmptyPage_ReturnsFalse()
    {
        var e = NewEngine();
        Assert.False(e.ClearCurrentPage());
    }

    [Fact]
    public void EraserMiss_DoesNotRemove()
    {
        var e = NewEngine();
        DrawLine(e, 1, 100, 100, 300, 100);

        e.SetTool(InkTool.Eraser);
        e.PointerDown(2, 200, 300);
        e.PointerUp(2);

        Assert.Single(e.Document.Current.Strokes);
    }
}
