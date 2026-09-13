using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// List-load pin board: walk every pin-leg once. World XYZ is
    /// <see cref="JunctionPinWorld"/> — same as live At switch. No GO.
    /// </summary>
    internal static class RoutePinBoardArProbe
    {
        private static readonly Dictionary<string, int> Selected = new Dictionary<string, int>(64);
        private static readonly string?[] PinIds = new string?[RoutePinBoard.Capacity];
        private static readonly string?[] Captions = new string?[RoutePinBoard.Capacity];
        private static readonly bool[] MapsLeg = new bool[RoutePinBoard.Capacity];
        private static int _count;
        private static int _generation;

        internal static int Count => _count;

        internal static int Generation => _generation;

        internal static void Clear()
        {
            _count = 0;
            _generation++;
            RoutePinBoardSession.Clear();
        }

        internal static string? CaptionForPinId(string? pinId) =>
            RoutePinBoardSession.CaptionForPin(pinId);

        internal static bool TryGet(
            int index,
            PathGraphMapper? graph,
            out Vector3 world,
            out string caption,
            out bool mapsLeg)
        {
            world = default;
            caption = "";
            mapsLeg = false;
            if (index < 0 || index >= _count || graph == null)
            {
                return false;
            }

            var id = PinIds[index];
            if (string.IsNullOrEmpty(id)
                || !graph.TryGetJunction(id!, out var junction)
                || !JunctionPinWorld.TryGet(junction, out var x, out var y, out var z))
            {
                return false;
            }

            world = new Vector3(x, y, z);
            caption = Captions[index] ?? "";
            mapsLeg = MapsLeg[index];
            return true;
        }

        internal static bool IsLivePinDuplicate(int index)
        {
            if (index < 0 || index >= _count || !RoutePinLatch.ShowPin)
            {
                return false;
            }

            var live = RouteClearanceSession.PinJunctionId ?? RoutePinLatch.Id;
            return !string.IsNullOrEmpty(live)
                && string.Equals(PinIds[index], live, StringComparison.Ordinal);
        }

        internal static int Refresh(PathGraphMapper? graph, Action<string>? log)
        {
            _count = 0;
            _generation++;
            if (graph == null || !graph.HasFrozenPathCheck)
            {
                RoutePinBoardSession.Clear();
                log?.Invoke("T2 pin-board: skip graph-not-frozen");
                return 0;
            }

            if (!SwitchListSession.HasActive)
            {
                RoutePinBoardSession.Clear();
                log?.Invoke("T2 pin-board: skip no-list");
                return 0;
            }

            Selected.Clear();
            graph.CopyJunctionSelected(Selected);
            var yard = SwitchListSession.CurrentStep?.DestYardId ?? RouteDestSession.YardId;
            var n = RoutePinBoardSession.Rebuild(graph.PathCheckEdges, Selected, yard);
            log?.Invoke(
                "T2 pin-board: n="
                + n
                + " markers="
                + RoutePinBoardSession.MarkerCount
                + " split="
                + RoutePinBoardSession.SplitCount);
            for (var i = 0; i < RoutePinBoardSession.EntryCount; i++)
            {
                if (!RoutePinBoardSession.TryGetEntry(i, out var entry))
                {
                    continue;
                }

                var line = RoutePinBoard.FormatEntryLog(entry);
                if (line != null)
                {
                    log?.Invoke(line);
                }
            }

            var shown = 0;
            for (var i = 0; i < RoutePinBoardSession.MarkerCount && shown < PinIds.Length; i++)
            {
                if (!RoutePinBoardSession.TryGetMarker(i, out var marker)
                    || string.IsNullOrEmpty(marker.PinId))
                {
                    continue;
                }

                if (!graph.TryGetJunction(marker.PinId, out var junction)
                    || !JunctionPinWorld.TryGet(junction, out _, out _, out _))
                {
                    log?.Invoke("T2 pin-board: miss id=" + marker.PinId);
                    continue;
                }

                PinIds[shown] = marker.PinId;
                Captions[shown] = marker.Caption;
                MapsLeg[shown] = IsMapsOnlyCaption(marker.Caption);
                shown++;
            }

            _count = shown;
            return shown;
        }

        private static bool IsMapsOnlyCaption(string? caption)
        {
            if (string.IsNullOrEmpty(caption))
            {
                return false;
            }

            var hasL = false;
            var hasM = false;
            for (var i = 0; i < caption!.Length; i++)
            {
                var c = caption[i];
                if (c == 'L')
                {
                    hasL = true;
                }
                else if (c == 'M')
                {
                    hasM = true;
                }
            }

            return hasM && !hasL;
        }
    }
}
