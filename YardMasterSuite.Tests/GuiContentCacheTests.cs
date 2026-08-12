using System.Text;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class StringBuilderPoolTests
{
    [Fact]
    public void Rent_return_rent_reuses_cleared_builder()
    {
        var pool = new StringBuilderPool();
        var first = pool.Rent();
        first.Append("stale");
        pool.Return(first);

        var second = pool.Rent();

        Assert.Same(first, second);
        Assert.Equal(0, second.Length);
    }

    [Fact]
    public void Nested_rent_returns_distinct_builders()
    {
        var pool = new StringBuilderPool();
        var a = pool.Rent();
        var b = pool.Rent();

        Assert.NotSame(a, b);

        pool.Return(a);
        pool.Return(b);
    }
}

public class GuiContentCacheTests
{
    [Fact]
    public void TryCommit_first_write_returns_new_text()
    {
        var cache = new GuiContentCache(slotCount: 2);
        var sb = new StringBuilder();
        sb.Append("12 km/h");

        var changed = cache.TryCommit(0, sb, out var text);

        Assert.True(changed);
        Assert.Equal("12 km/h", text);
        Assert.Equal("12 km/h", cache.Get(0));
    }

    [Fact]
    public void TryCommit_same_text_does_not_allocate_a_new_string()
    {
        var cache = new GuiContentCache(slotCount: 1);
        var sb = new StringBuilder();
        sb.Append("12 km/h");
        cache.TryCommit(0, sb, out var first);

        sb.Clear();
        sb.Append("12 km/h");
        var changed = cache.TryCommit(0, sb, out var second);

        Assert.False(changed);
        Assert.Same(first, second);
    }

    [Fact]
    public void TryCommit_changed_text_replaces_cached_string()
    {
        var cache = new GuiContentCache(slotCount: 1);
        var sb = new StringBuilder();
        sb.Append("12 km/h");
        cache.TryCommit(0, sb, out _);

        sb.Clear();
        sb.Append("13 km/h");
        var changed = cache.TryCommit(0, sb, out var text);

        Assert.True(changed);
        Assert.Equal("13 km/h", text);
    }
}
