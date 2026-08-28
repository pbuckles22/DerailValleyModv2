using System;
using System.Collections.Generic;
using System.IO;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Writes graph + corridor harvest files once (not per-frame) for headless replay.
    /// </summary>
    internal static class RouteHarvestDump
    {
        internal static Action<string>? EmitLog;

        internal static string DirectoryPath =>
            Path.Combine(Application.persistentDataPath, "YardMasterSuite", "harvest");

        internal static string? WriteGraph(PathGraphMapper graph)
        {
            if (graph == null || !graph.HasFrozenPathCheck)
            {
                return null;
            }

            var selected = new Dictionary<string, int>(StringComparer.Ordinal);
            graph.CopyJunctionSelected(selected);
            var junctions = graph.CopyJunctionWorld();
            var text = RouteHarvestCodec.Format(
                graph.PathCheckEdges,
                selected,
                junctions);
            return WriteFile("graph.txt", text, "T2 harvest: graph edges=" + graph.PathCheckEdges.Count.ToString());
        }

        internal static string? WriteCorridor(
            PathGraphMapper? graph,
            PathPlanResult plan,
            string? yardId,
            string? originTrackId,
            string? destTrackId)
        {
            if (graph == null || !graph.HasFrozenPathCheck)
            {
                return null;
            }

            var selected = new Dictionary<string, int>(StringComparer.Ordinal);
            graph.CopyJunctionSelected(selected);
            var pinId = RoutePinLatch.EffectivePin(plan);
            float? pinX = null, pinZ = null, noseX = null, noseZ = null, fwdX = null, fwdZ = null, length = null;
            bool? behind = null;
            if (!string.IsNullOrEmpty(pinId)
                && graph.TryGetJunction(pinId!, out var junction)
                && JunctionPinWorld.TryGet(junction, out var px, out _, out var pz))
            {
                pinX = px;
                pinZ = pz;
            }

            TryLoco(out noseX, out noseZ, out fwdX, out fwdZ, out length);
            if (pinX.HasValue && noseX.HasValue && fwdX.HasValue)
            {
                behind = DriveSetFacing.IsTargetBehind(
                    fwdX.Value,
                    fwdZ!.Value,
                    pinX.Value - noseX.Value,
                    pinZ!.Value - noseZ!.Value);
            }

            var mode = PathPlanModeSelect.ForTrip(originTrackId, destTrackId, yardId);
            var text = RouteHarvestCodec.Format(
                graph.PathCheckEdges,
                selected,
                graph.CopyJunctionWorld(),
                yardId,
                originTrackId,
                destTrackId,
                mode,
                pinId,
                pinX,
                pinZ,
                noseX,
                noseZ,
                fwdX,
                fwdZ,
                length,
                behind);
            return WriteFile("corridor.txt", text, "T2 harvest: corridor pin=" + (pinId ?? "none"));
        }

        private static readonly float[] LengthScratch = new float[64];

        private static void TryLoco(
            out float? noseX,
            out float? noseZ,
            out float? fwdX,
            out float? fwdZ,
            out float? lengthM)
        {
            noseX = noseZ = fwdX = fwdZ = lengthM = null;
            try
            {
                var car = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (car == null)
                {
                    return;
                }

                var cars = car.trainset != null ? car.trainset.cars : null;
                TrainCar lead = car;
                if (cars != null && cars.Count > 0)
                {
                    var best = int.MaxValue;
                    for (var i = 0; i < cars.Count; i++)
                    {
                        var c = cars[i];
                        if (c == null)
                        {
                            continue;
                        }

                        var idx = c.indexInTrainset;
                        if (idx < best)
                        {
                            best = idx;
                            lead = c;
                        }
                    }

                    var n = 0;
                    for (var i = 0; i < cars.Count && n < LengthScratch.Length; i++)
                    {
                        var c = cars[i];
                        if (c == null)
                        {
                            continue;
                        }

                        LengthScratch[n++] = ReadCarLength(c);
                    }

                    var sum = ConsistLengthMeters.Sum(LengthScratch, n);
                    if (sum > 0f)
                    {
                        lengthM = sum;
                    }
                }
                else
                {
                    var one = ReadCarLength(lead);
                    if (one > 0f)
                    {
                        lengthM = one;
                    }
                }

                var t = lead.transform;
                var pos = t.position;
                var fwd = t.forward;
                noseX = pos.x;
                noseZ = pos.z;
                fwdX = fwd.x;
                fwdZ = fwd.z;
            }
            catch
            {
                // omit pose
            }
        }

        private static float ReadCarLength(TrainCar car)
        {
            try
            {
                var len = car.InterCouplerDistance;
                if (len > 0f)
                {
                    return len;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                return car.Bounds.size.z;
            }
            catch
            {
                return 0f;
            }
        }

        private static string? WriteFile(string fileName, string text, string logLine)
        {
            try
            {
                var dir = DirectoryPath;
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, fileName);
                RouteHarvestFiles.Replace(path, text);
                EmitLog?.Invoke(logLine + " file=" + path);
                return path;
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke("T2 harvest: write " + ex.GetType().Name);
                return null;
            }
        }
    }
}
