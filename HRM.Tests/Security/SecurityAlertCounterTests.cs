using HRM.Services.Security;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace HRM.Tests.Security;

// ตัวนับเหตุการณ์ความปลอดภัย: แจ้งครั้งเดียวเมื่อถึงเกณฑ์พอดี และกันอีเมลซ้ำ
public class SecurityAlertCounterTests
{
    private static SecurityAlertCounter New() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void Reaches_threshold_exactly_once()
    {
        var c = New();
        var w = TimeSpan.FromMinutes(15);
        Assert.False(c.ReachedNow("k", w, 3));
        Assert.False(c.ReachedNow("k", w, 3));
        Assert.True(c.ReachedNow("k", w, 3));    // ครั้งที่ 3 = ถึงเกณฑ์
        Assert.False(c.ReachedNow("k", w, 3));   // ครั้งที่ 4 ไม่แจ้งซ้ำในกรอบเดียวกัน
    }

    [Fact]
    public void Keys_are_independent()
    {
        var c = New();
        var w = TimeSpan.FromMinutes(15);
        c.Hit("a", w); c.Hit("a", w);
        Assert.Equal(1, c.Hit("b", w));
        Assert.Equal(3, c.Hit("a", w));
    }

    [Fact]
    public void Throttle_blocks_repeats_within_period()
    {
        var c = New();
        Assert.False(c.Throttled("t", TimeSpan.FromMinutes(30)));
        Assert.True(c.Throttled("t", TimeSpan.FromMinutes(30)));
    }

    [Fact]
    public void Zero_threshold_never_fires()
    {
        var c = New();
        Assert.False(c.ReachedNow("z", TimeSpan.FromMinutes(1), 0));
    }
}
