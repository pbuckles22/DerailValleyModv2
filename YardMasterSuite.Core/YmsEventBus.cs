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

        /// <summary>
        /// Null every Type A event. Add new events here when they ship.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnSignal = null;
            OnCount = null;
            OnPlayerBoardedTrain = null;
            OnCabControlsChanged = null;
        }
    }
}
