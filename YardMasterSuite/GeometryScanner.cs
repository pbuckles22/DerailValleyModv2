using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Samples the usable loco's current <see cref="RailTrack"/> bezier once per
    /// segment enter (boarded or look-at). Publishes Type A <see cref="GeometryScanResult"/>.
    /// No HUD chip.
    /// </summary>
    public sealed class GeometryScanner : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static Func<bool>? IsWorldSession;

        private const float ArcTolerance = 0.5f;

        private readonly List<BezierArcApproximation.Arc> _arcScratch = new List<BezierArcApproximation.Arc>(32);

        private readonly List<SpeedLimitGeometryZones.ArcSample> _samples =
            new List<SpeedLimitGeometryZones.ArcSample>(32);

        private readonly GeometrySegmentStore _store = new GeometrySegmentStore();

        private GeometryScanCache _cache;

        private void OnEnable()
        {
            _cache = default;
            _store.Clear();
        }

        private void OnDisable()
        {
            _cache = default;
            _store.Clear();
        }

        private void Update()
        {
            var inWorld = IsWorldSession?.Invoke() ?? false;
            var car = inWorld ? UsableTrainProbe.TryGetUsableLoco() : null;
            var segmentId = inWorld ? ResolveSegmentId(car) : 0;
            if (!GeometryScan.ShouldRescan(segmentId, in _cache))
            {
                return;
            }

            var result = Scan(segmentId, car);
            GeometryScan.Remember(result, ref _cache);
            YmsEventBus.RaiseGeometryScan(result);
            EmitLog?.Invoke(GeometryTelemetry.Format(result));
        }

        private GeometryScanResult Scan(int segmentId, TrainCar? car)
        {
            if (segmentId == 0)
            {
                return GeometryScanResult.None;
            }

            if (_store.TryGet(segmentId, out var cached))
            {
                return cached;
            }

            FillSamples(car);
            var result = GeometryScan.Evaluate(segmentId, _samples);
            _store.Remember(result);
            return result;
        }

        private void FillSamples(TrainCar? car)
        {
            _samples.Clear();
            var track = ResolveTrack(car);
            if (track == null)
            {
                return;
            }

            try
            {
                var curve = track.curve;
                if (curve == null)
                {
                    return;
                }

                _arcScratch.Clear();
                BezierArcApproximation.CalculateArcs(curve, ArcTolerance, _arcScratch);
                for (var i = 0; i < _arcScratch.Count; i++)
                {
                    var arc = _arcScratch[i];
                    _samples.Add(new SpeedLimitGeometryZones.ArcSample(arc.r, arc.Length));
                }
            }
            catch
            {
                _samples.Clear();
            }
        }

        internal static int ResolveSegmentId(TrainCar? car)
        {
            var track = ResolveTrack(car);
            return track == null ? 0 : track.GetInstanceID();
        }

        internal static RailTrack? ResolveTrack(TrainCar? car)
        {
            if (car == null || !car.IsLoco)
            {
                return null;
            }

            try
            {
                var bogie = car.FrontBogie ?? car.RearBogie;
                return bogie != null ? bogie.track : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
