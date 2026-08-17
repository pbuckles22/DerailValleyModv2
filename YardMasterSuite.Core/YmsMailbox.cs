using System;
using System.Collections.Concurrent;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Type B probe payload (readonly struct). Later engines use their own
    /// structs with their own <see cref="YmsMailbox{T}"/> instances.
    /// </summary>
    public readonly struct MailboxItem
    {
        public readonly int Sequence;

        public MailboxItem(int sequence)
        {
            Sequence = sequence;
        }
    }

    /// <summary>
    /// Thread-safe mailbox. Workers <see cref="Enqueue"/>; the main thread
    /// <see cref="Drain"/>s and publishes Type A. Do not touch Unity APIs
    /// from the worker.
    /// </summary>
    public sealed class YmsMailbox<T> where T : struct
    {
        /// <summary>Hitch cap: at most this many items leave the queue per drain.</summary>
        public const int MaxDrainPerFrame = 8;

        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Enqueue(in T item)
        {
            _queue.Enqueue(item);
        }

        /// <summary>
        /// Main-thread only. Dequeues up to <paramref name="maxItems"/> and
        /// invokes <paramref name="publish"/> for each (Type A). Returns how
        /// many were dequeued. Empty or non-positive max is a no-op.
        /// </summary>
        public int Drain(int maxItems, Action<T>? publish)
        {
            if (maxItems <= 0)
            {
                return 0;
            }

            var n = 0;
            while (n < maxItems && _queue.TryDequeue(out var item))
            {
                publish?.Invoke(item);
                n++;
            }

            return n;
        }

        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
            }
        }
    }

    /// <summary>
    /// T2 line for a drain that actually moved items. Silent when n=0.
    /// </summary>
    public static class MailboxTelemetry
    {
        public static string? FormatDrain(int count)
        {
            if (count <= 0)
            {
                return null;
            }

            return "T2 mailbox: n=" + count.ToString();
        }
    }
}
