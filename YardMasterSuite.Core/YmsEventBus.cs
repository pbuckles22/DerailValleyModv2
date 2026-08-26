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

        /// <summary>Consist cars / tonnes changed (board, look-at usable train, couple, uncouple).</summary>
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

        /// <summary>Boarded speed (rounded km/h) changed.</summary>
        public static event Action<SpeedSnapshot>? OnSpeedChanged;

        /// <summary>Active speed limit for HUD changed.</summary>
        public static event Action<SpeedLimitSnapshot>? OnSpeedLimitChanged;

        /// <summary>Posted-board sticky Limit + Next (6.10).</summary>
        public static event Action<PostedLimitSnapshot>? OnPostedLimitChanged;

        /// <summary>Usable loco train gate changed (**4.3**).</summary>
        public static event Action<UsableTrainState>? OnUsableTrainChanged;

        /// <summary>Look-at / local car bar line changed.</summary>
        public static event Action<HudBarSnapshot>? OnLookAtBarChanged;

        /// <summary>Active job bar line changed.</summary>
        public static event Action<HudBarSnapshot>? OnJobBarChanged;

        /// <summary>Always-on extras (marked, station, path, clock) changed.</summary>
        public static event Action<HudBarSnapshot>? OnAlwaysOnExtrasChanged;

        /// <summary>Loco gadget chips (fuel, grade, MU, …) changed.</summary>
        public static event Action<TrainGadgetSnapshot>? OnTrainGadgetsChanged;

        /// <summary>Rear/Front proximity chip (**6.18**). Empty = omit.</summary>
        public static event Action<HudBarSnapshot>? OnBackupProximityChanged;

        /// <summary>7.5 Limit-gov HUD flash: which levers the governor is moving.</summary>
        public static event Action<LimitGovCue>? OnLimitGovCue;

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

        public static void RaiseSpeedChanged(in SpeedSnapshot snapshot)
        {
            OnSpeedChanged?.Invoke(snapshot);
        }

        public static void RaiseSpeedLimitChanged(in SpeedLimitSnapshot snapshot)
        {
            OnSpeedLimitChanged?.Invoke(snapshot);
        }

        public static void RaisePostedLimitChanged(in PostedLimitSnapshot snapshot)
        {
            OnPostedLimitChanged?.Invoke(snapshot);
        }

        public static void RaiseUsableTrainChanged(in UsableTrainState state)
        {
            OnUsableTrainChanged?.Invoke(state);
        }

        public static void RaiseLookAtBarChanged(in HudBarSnapshot snapshot)
        {
            OnLookAtBarChanged?.Invoke(snapshot);
        }

        public static void RaiseJobBarChanged(in HudBarSnapshot snapshot)
        {
            OnJobBarChanged?.Invoke(snapshot);
        }

        public static void RaiseAlwaysOnExtrasChanged(in HudBarSnapshot snapshot)
        {
            OnAlwaysOnExtrasChanged?.Invoke(snapshot);
        }

        public static void RaiseTrainGadgetsChanged(in TrainGadgetSnapshot snapshot)
        {
            OnTrainGadgetsChanged?.Invoke(snapshot);
        }

        public static void RaiseBackupProximityChanged(in HudBarSnapshot snapshot)
        {
            OnBackupProximityChanged?.Invoke(snapshot);
        }

        public static void RaiseLimitGovCue(in LimitGovCue cue)
        {
            OnLimitGovCue?.Invoke(cue);
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
            OnSpeedChanged = null;
            OnSpeedLimitChanged = null;
            OnPostedLimitChanged = null;
            OnUsableTrainChanged = null;
            OnLookAtBarChanged = null;
            OnJobBarChanged = null;
            OnAlwaysOnExtrasChanged = null;
            OnTrainGadgetsChanged = null;
            OnBackupProximityChanged = null;
            OnLimitGovCue = null;
            Mailbox.Clear();
            PathGraph.Clear();
        }
    }
}
