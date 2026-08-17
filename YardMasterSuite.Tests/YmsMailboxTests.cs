using System.Collections.Generic;
using System.Threading;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Type B mailbox: workers enqueue structs; drain on the consuming thread
/// publishes Type A. Enqueue must not invoke subscribers.
/// </summary>
public class YmsMailboxTests
{
    [Fact]
    public void Enqueue_does_not_invoke_publish()
    {
        var box = new YmsMailbox<MailboxItem>();
        var calls = 0;
        void Publish(MailboxItem _) => calls++;

        box.Enqueue(new MailboxItem(1));

        Assert.Equal(0, calls);
        Assert.Equal(1, box.Drain(8, Publish));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Drain_delivers_fifo_payloads()
    {
        var box = new YmsMailbox<MailboxItem>();
        var got = new List<int>();

        box.Enqueue(new MailboxItem(3));
        box.Enqueue(new MailboxItem(7));
        box.Enqueue(new MailboxItem(11));

        Assert.Equal(3, box.Drain(8, item => got.Add(item.Sequence)));
        Assert.Equal(new[] { 3, 7, 11 }, got);
        Assert.Equal(0, box.Drain(8, item => got.Add(item.Sequence)));
    }

    [Fact]
    public void Drain_caps_items_per_call()
    {
        var box = new YmsMailbox<MailboxItem>();
        var got = new List<int>();
        for (var i = 1; i <= 10; i++)
        {
            box.Enqueue(new MailboxItem(i));
        }

        Assert.Equal(8, YmsMailbox<MailboxItem>.MaxDrainPerFrame);
        Assert.Equal(8, box.Drain(YmsMailbox<MailboxItem>.MaxDrainPerFrame, item => got.Add(item.Sequence)));
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, got);

        got.Clear();
        Assert.Equal(2, box.Drain(YmsMailbox<MailboxItem>.MaxDrainPerFrame, item => got.Add(item.Sequence)));
        Assert.Equal(new[] { 9, 10 }, got);
    }

    [Fact]
    public void Drain_empty_or_non_positive_max_is_a_noop()
    {
        var box = new YmsMailbox<MailboxItem>();
        var calls = 0;
        void Publish(MailboxItem _) => calls++;

        Assert.Equal(0, box.Drain(8, Publish));
        box.Enqueue(new MailboxItem(1));
        Assert.Equal(0, box.Drain(0, Publish));
        Assert.Equal(0, box.Drain(-1, Publish));
        Assert.Equal(0, calls);
        Assert.Equal(1, box.Drain(8, Publish));
    }

    [Fact]
    public void Drain_with_null_publish_still_dequeues()
    {
        var box = new YmsMailbox<MailboxItem>();
        box.Enqueue(new MailboxItem(1));
        box.Enqueue(new MailboxItem(2));

        Assert.Equal(2, box.Drain(8, publish: null));
        Assert.Equal(0, box.Drain(8, publish: null));
    }

    [Fact]
    public void Clear_drops_pending_items()
    {
        var box = new YmsMailbox<MailboxItem>();
        var calls = 0;
        box.Enqueue(new MailboxItem(1));
        box.Clear();

        Assert.Equal(0, box.Drain(8, _ => calls++));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Worker_enqueue_is_visible_only_after_drain()
    {
        var box = new YmsMailbox<MailboxItem>();
        var got = new List<int>();
        using var posted = new ManualResetEventSlim(false);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            box.Enqueue(new MailboxItem(42));
            posted.Set();
        });

        Assert.True(posted.Wait(TimeSpan.FromSeconds(5)));
        Assert.Empty(got);
        Assert.Equal(1, box.Drain(8, item => got.Add(item.Sequence)));
        Assert.Equal(new[] { 42 }, got);
    }

    [Fact]
    public void Concurrent_producers_all_arrive_on_drain()
    {
        var box = new YmsMailbox<MailboxItem>();
        const int producers = 4;
        const int each = 25;
        using var done = new CountdownEvent(producers);

        for (var p = 0; p < producers; p++)
        {
            var offset = p * each;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                for (var i = 1; i <= each; i++)
                {
                    box.Enqueue(new MailboxItem(offset + i));
                }

                done.Signal();
            });
        }

        Assert.True(done.Wait(TimeSpan.FromSeconds(5)));
        var got = new HashSet<int>();
        var n = 0;
        int batch;
        while ((batch = box.Drain(YmsMailbox<MailboxItem>.MaxDrainPerFrame, item => { got.Add(item.Sequence); })) > 0)
        {
            n += batch;
        }

        Assert.Equal(producers * each, n);
        Assert.Equal(producers * each, got.Count);
    }
}

[Collection("YmsEventBus")]
public class YmsMailboxBusTests : IDisposable
{
    public YmsMailboxBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void DrainMailbox_raises_Type_A_for_subscribers()
    {
        MailboxItem received = default;
        var calls = 0;
        void Handler(MailboxItem item)
        {
            received = item;
            calls++;
        }

        YmsEventBus.OnMailboxItem += Handler;
        YmsEventBus.Mailbox.Enqueue(new MailboxItem(9));

        Assert.Equal(0, calls);
        Assert.Equal(1, YmsEventBus.DrainMailbox(YmsMailbox<MailboxItem>.MaxDrainPerFrame));
        Assert.Equal(1, calls);
        Assert.Equal(9, received.Sequence);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_pending_mailbox_items_and_handlers()
    {
        var calls = 0;
        YmsEventBus.OnMailboxItem += _ => calls++;
        YmsEventBus.Mailbox.Enqueue(new MailboxItem(1));
        YmsEventBus.ClearAllSubscriptions();

        Assert.Equal(0, YmsEventBus.DrainMailbox(8));
        YmsEventBus.Mailbox.Enqueue(new MailboxItem(2));
        Assert.Equal(1, YmsEventBus.DrainMailbox(8));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void FormatDrain_is_silent_when_empty()
    {
        Assert.Null(MailboxTelemetry.FormatDrain(0));
        Assert.Equal("T2 mailbox: n=1", MailboxTelemetry.FormatDrain(1));
        Assert.Equal("T2 mailbox: n=8", MailboxTelemetry.FormatDrain(8));
    }
}
