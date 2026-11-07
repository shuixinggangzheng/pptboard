using PPTBoard.Ink;
using Xunit;

namespace PPTBoard.Ink.Tests;

public class InkDocumentTests
{
    [Fact]
    public void AddPageAfterCurrent_JumpsToNewPage()
    {
        var doc = new InkDocument();
        int index = doc.AddPageAfterCurrent();
        Assert.Equal(1, index);
        Assert.Equal(2, doc.PageCount);
        Assert.Equal(1, doc.CurrentIndex);
    }

    [Fact]
    public void DeletePage_AdjustsCurrentAndReindexes()
    {
        var doc = new InkDocument();
        doc.AddPageAfterCurrent();
        doc.AddPageAfterCurrent();
        Assert.Equal(3, doc.PageCount);
        Assert.Equal(2, doc.CurrentIndex);

        Assert.True(doc.DeletePage(0));
        Assert.Equal(2, doc.PageCount);
        Assert.Equal(1, doc.CurrentIndex);
        Assert.Equal(0, doc.Pages[0].Index);
        Assert.Equal(1, doc.Pages[1].Index);
    }

    [Fact]
    public void DeletePage_RefusesLastPage()
    {
        var doc = new InkDocument();
        Assert.False(doc.DeletePage(0));
        Assert.Equal(1, doc.PageCount);
    }

    [Fact]
    public void DeletePage_OutOfRange_ReturnsFalse()
    {
        var doc = new InkDocument();
        doc.AddPageAfterCurrent();
        Assert.False(doc.DeletePage(5));
        Assert.Equal(2, doc.PageCount);
    }

    [Fact]
    public void GoTo_ClampsToValidRange()
    {
        var doc = new InkDocument();
        doc.AddPageAfterCurrent();
        Assert.True(doc.GoTo(0));
        Assert.Equal(0, doc.CurrentIndex);
        Assert.False(doc.GoTo(9));
        Assert.False(doc.GoTo(-1));
        Assert.Equal(0, doc.CurrentIndex);
    }
}
