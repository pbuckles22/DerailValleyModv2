using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Infrastructure Type A payload (readonly struct, zero alloc).
    /// </summary>
    public readonly struct YmsSignal
    {
        public readonly int Id;
        public readonly float Value;

        public YmsSignal(int id, float value)
        {
            Id = id;
            Value = value;
        }
    }

    /// <summary>
    /// Central Type A event bus. Payloads are primitives or readonly structs.
    /// Subscribers must unsubscribe in OnDisable/OnDestroy; UMM deactivate
    /// also calls <see cref="ClearAllSubscriptions"/>.
    /// </summary>
    public static class YmsEventBus
    {
        /// <summary>Placeholder Type A event with a readonly-struct payload.</summary>
        public static event Action<YmsSignal>? OnSignal;

        /// <summary>Placeholder Type A event with a primitive payload.</summary>
        public static event Action<int>? OnCount;

        /// <summary>
        /// Boarded loco changed. <see cref="LocoPresence.InstanceId"/> 0 = unboarded.
        /// </summary>
        public static event Action<LocoPresence>? OnPlayerBoardedTrain;

        /// <summary>Cab levers changed (throttle, indy, train, optional engine, reverser).</summary>
        public static event Action<CabControlsState>? OnCabControlsChanged;

        /// <summary>Boarded consist cars / tonnes changed (couple or uncouple).</summary>
        public static event Action<ConsistSnapshot>? OnConsistChanged;

        /// <summary>Look heading 16-point bucket changed (camera / player forward).</summary>
        public static event Action<CompassHeading>? OnHeadingChanged;

        /// <summary>
        /// Type B probe channel. Workers enqueue; <see cref="DrainMailbox"/>
        /// on the main thread raises <see cref="OnMailboxItem"/>.
        /// </summary>
        public static readonly YmsMailbox<MailboxItem> Mailbox = new YmsMailbox<MailboxItem>();

        /// <summary>Type B probe item reached the main thread.</summary>
        public static event Action<MailboxItem>? OnMailboxItem;

        /// <summary>
        /// Track graph ready (after time-sliced build + worker A*). Drain on main thread.
        /// </summary>
        public static readonly YmsMailbox<PathGraphReady> PathGraph = new YmsMailbox<PathGraphReady>();

        /// <summary>Type B path-graph snapshot reached the main thread.</summary>
        public static event Action<PathGraphReady>? OnPathGraphReady;

        /// <summary>
        /// Current-track geometry limit changed (segment enter / unboard).
        /// </summary>
        public static event Action<GeometryScanResult>? OnGeometryScan;

        private static readonly Action<MailboxItem> PublishMailboxItem = RaiseMailboxItem;

        private static readonly Action<PathGraphReady> PublishPathGraphReady = RaisePathGraphReady;

        public static void RaiseSignal(in YmsSignal signal)
        {
            OnSignal?.Invoke(signal);
        }

        public static void RaiseCount(int count)
        {
            OnCount?.Invoke(count);
        }

        public static void RaisePlayerBoardedTrain(in LocoPresence presence)
        {
            OnPlayerBoardedTrain?.Invoke(presence);
        }

        public static void RaiseCabControlsChanged(in CabControlsState state)
        {
            OnCabControlsChanged?.Invoke(state);
        }

        public static void RaiseConsistChanged(in ConsistSnapshot snapshot)
        {
            OnConsistChanged?.Invoke(snapshot);
        }

        public static void RaiseHeadingChanged(in CompassHeading heading)
        {
            OnHeadingChanged?.Invoke(heading);
        }

        public static void RaiseMailboxItem(MailboxItem item)
        {
            OnMailboxItem?.Invoke(item);
        }

        public static void RaisePathGraphReady(PathGraphReady item)
        {
            OnPathGraphReady?.Invoke(item);
        }

        public static void RaiseGeometryScan(in GeometryScanResult result)
        {
            OnGeometryScan?.Invoke(result);
        }

        /// <summary>
        /// Main-thread drain of <see cref="Mailbox"/>. Raises
        /// <see cref="OnMailboxItem"/> per item. Returns count drained.
        /// </summary>
        public static int DrainMailbox(int maxItems)
        {
            return Mailbox.Drain(maxItems, PublishMailboxItem);
        }

        public static int DrainPathGraph(int maxItems)
        {
            return PathGraph.Drain(maxItems, PublishPathGraphReady);
        }

        /// <summary>
        /// Null every Type A event and drop pending Type B items.
        /// Add new events here when they ship.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSignal = null;
            OnCount = null;
            OnPlayerBoardedTrain = null;
            OnCabControlsChanged = null;
            OnConsistChanged = null;
            OnHeadingChanged = null;
            OnMailboxItem = null;
            OnPathGraphReady = null;
            OnGeometryScan = null;
            Mailbox.Clear();
            PathGraph.Clear();
        }
    }
}
