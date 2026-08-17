using System;
using System.Threading;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Main-thread Type B drain. Workers enqueue; Update publishes Type A.
    /// Posts one probe item from the thread pool on enable so smoke has a T2 line.
    /// </summary>
    public sealed class YmsMailboxDrain : MonoBehaviour
    {
        /// <summary>UMM logger sink; Main sets this on activate and clears on deactivate.</summary>
        internal static Action<string>? EmitLog;

        private static int _generation;

        private void OnEnable()
        {
            var gen = Interlocked.Increment(ref _generation);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Volatile.Read(ref _generation) != gen)
                {
                    return;
                }

                YmsEventBus.Mailbox.Enqueue(new MailboxItem(1));
            });
        }

        private void OnDisable()
        {
            Interlocked.Increment(ref _generation);
            YmsEventBus.Mailbox.Clear();
        }

        private void Update()
        {
            var n = YmsEventBus.DrainMailbox(YmsMailbox<MailboxItem>.MaxDrainPerFrame);
            var line = MailboxTelemetry.FormatDrain(n);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }
    }
}
