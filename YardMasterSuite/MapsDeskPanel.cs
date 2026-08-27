using System;
using System.Collections.Generic;
using DV;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// v1 Dispatch Desk Route tab: City / Track / Set dest / Recheck / Align Route.
    /// Set dest publishes Type A; route + Align are **8.2**.
    /// </summary>
    public sealed class MapsDeskPanel : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private bool _visible;
        private bool _worldSessionActive;
        private bool _yardDropOpen;
        private bool _trackDropOpen;
        private Vector2 _yardScroll;
        private Vector2 _trackScroll;
        private int _yardIndex;
        private int _trackIndex;
        private string _status = string.Empty;
        private IReadOnlyList<string> _yards = Array.Empty<string>();
        private IReadOnlyList<string> _tracks = Array.Empty<string>();

        private void OnDisable()
        {
            _visible = false;
            MapsDeskCatalog.Invalidate();
            _yards = Array.Empty<string>();
            _tracks = Array.Empty<string>();
        }

        private void Update()
        {
            var world = WorldSessionGate.IsActive();
            if (!world)
            {
                if (_worldSessionActive)
                {
                    MapsDeskCatalog.Invalidate();
                    _worldSessionActive = false;
                }

                _visible = false;
                return;
            }

            _worldSessionActive = true;

            if (MapsDeskCatalog.IsMapping)
            {
                var finished = MapsDeskCatalog.Tick();
                if (_visible)
                {
                    _status = MapsDeskCatalog.MappingBanner;
                }

                if (finished)
                {
                    RefreshFromCatalog();
                }
            }

            if (!HudWorldSession.IsActive(
                    PlayerManager.PlayerTransform != null,
                    ScreenOverlayGate.WorldReady())
                || ScreenOverlayGate.IsBlocking())
            {
                return;
            }

            var control = YmsHotkeyPolicy.ControlHeld(
                Input.GetKey(KeyCode.LeftControl),
                Input.GetKey(KeyCode.RightControl));
            if (!YmsHotkeyPolicy.ShouldAcceptToolChord(control, Input.GetKeyDown(KeyCode.Insert)))
            {
                return;
            }

            ToggleDesk();
        }

        private void OnGUI()
        {
            if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
            {
                return;
            }

            if (!_visible)
            {
                return;
            }

            const float w = 420f;
            var h = MapsDeskCatalog.IsMapping ? 300f : 320f;
            var x = (Screen.width - w) * 0.5f;
            var y = Screen.height * 0.12f;
            GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher)");

            var row = y + 26f;
            DrawRoute(x, ref row, w);
        }

        private void DrawRoute(float x, ref float row, float w)
        {
            var yard = _yards.Count > 0 ? _yards[_yardIndex] : "— pick city —";
            var track = _tracks.Count > 0 ? _tracks[_trackIndex] : "— pick track —";
            var pathChip = FormatRoutePathChip();
            var license = RouteAlignAccess.DeniedChip(HasDispatcherLicense())
                ?? "Dispatcher ok";
            var facing = FormatRouteFacing();
            var etaRem = FormatRouteEtaRem();

            if (MapsDeskCatalog.IsMapping)
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), MapsDeskCatalog.MappingBanner);
                row += 26f;
            }

            GUI.Label(new Rect(x + 12, row, 50, 22), "City");
            if (GUI.Button(new Rect(x + 70, row, 200, 24), yard + " ▼"))
            {
                _yardDropOpen = !_yardDropOpen;
                _trackDropOpen = false;
                if (_yards.Count == 0 && !MapsDeskCatalog.IsMapping)
                {
                    MapsDeskCatalog.EnsureStarted();
                    _status = MapsDeskCatalog.MappingBanner;
                }
            }

            row += 28f;
            if (_yardDropOpen && _yards.Count > 0)
            {
                var dropH = Mathf.Min(140f, 22f * _yards.Count + 8f);
                _yardScroll = GUI.BeginScrollView(
                    new Rect(x + 70, row, 200, dropH),
                    _yardScroll,
                    new Rect(0, 0, 180, 22f * _yards.Count));
                for (var i = 0; i < _yards.Count; i++)
                {
                    if (GUI.Button(new Rect(0, i * 22f, 180, 22), _yards[i]))
                    {
                        _yardIndex = i;
                        _trackIndex = 0;
                        RefreshTracks();
                        _yardDropOpen = false;
                    }
                }

                GUI.EndScrollView();
                row += dropH + 4f;
            }

            GUI.Label(new Rect(x + 12, row, 50, 22), "Track");
            if (GUI.Button(new Rect(x + 70, row, 280, 24), track + " ▼"))
            {
                _trackDropOpen = !_trackDropOpen;
                _yardDropOpen = false;
                if (_tracks.Count == 0)
                {
                    RefreshTracks();
                }
            }

            row += 28f;
            if (_trackDropOpen && _tracks.Count > 0)
            {
                var dropH = Mathf.Min(160f, 22f * _tracks.Count + 8f);
                _trackScroll = GUI.BeginScrollView(
                    new Rect(x + 70, row, 280, dropH),
                    _trackScroll,
                    new Rect(0, 0, 260, 22f * _tracks.Count));
                for (var i = 0; i < _tracks.Count; i++)
                {
                    if (GUI.Button(new Rect(0, i * 22f, 260, 22), _tracks[i]))
                    {
                        _trackIndex = i;
                        _trackDropOpen = false;
                    }
                }

                GUI.EndScrollView();
                row += dropH + 4f;
            }

            GUI.Label(new Rect(x + 12, row, w - 24, 22), pathChip + "  |  " + license);
            row += 24f;
            if (!string.IsNullOrEmpty(facing) || !string.IsNullOrEmpty(etaRem))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), (facing ?? "—") + "  |  " + (etaRem ?? "—"));
                row += 26f;
            }
            else
            {
                row += 4f;
            }

            row += 0f;

            if (GUI.Button(new Rect(x + 12, row, 100, 28), "Set dest"))
            {
                _yardDropOpen = _trackDropOpen = false;
                if (_yards.Count == 0 || _tracks.Count == 0)
                {
                    _status = _yards.Count == 0 ? "no cities — reopen in world" : "pick city + track";
                    MapsDeskCatalog.EnsureStarted();
                    Publish(MapsDestKind.RejectEmpty);
                }
                else
                {
                    Publish(MapsDestApply.SetDest(_yards[_yardIndex], _tracks[_trackIndex]));
                    SyncIndicesFromSession();
                }
            }

            if (GUI.Button(new Rect(x + 118, row, 100, 28), "Recheck"))
            {
                _yardDropOpen = _trackDropOpen = false;
                var city = _yards.Count > 0 ? _yards[_yardIndex] : null;
                var tr = _tracks.Count > 0 ? _tracks[_trackIndex] : null;
                Publish(MapsDestApply.Recheck(city, tr));
            }

            if (GUI.Button(new Rect(x + 226, row, 100, 28), "Align Route"))
            {
                _yardDropOpen = _trackDropOpen = false;
                var alignMsg = MapsRouteListener.Instance?.TryAlignRoute() ?? "T2 align: unavailable";
                _status = alignMsg;
            }

            row += 34f;
            if (GUI.Button(new Rect(x + 12, row, 70, 28), "Clear"))
            {
                Publish(MapsDestApply.Clear());
            }

            if (GUI.Button(new Rect(x + 90, row, 70, 28), "Hide"))
            {
                SetVisible(false);
            }

            if (GUI.Button(new Rect(x + 170, row, 90, 28), "Reload list"))
            {
                MapsDeskCatalog.Invalidate();
                MapsDeskCatalog.EnsureStarted();
                _yards = Array.Empty<string>();
                _tracks = Array.Empty<string>();
                _status = MapsDeskCatalog.MappingBanner;
            }

            row += 32f;
            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 28), _status);
            }
        }

        private void ToggleDesk()
        {
            SetVisible(!_visible);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _yardDropOpen = _trackDropOpen = false;
            if (!_visible)
            {
                EmitLog?.Invoke(MapsDestTelemetry.DeskClose);
                return;
            }

            EmitLog?.Invoke(MapsDestTelemetry.DeskOpen);
            if (MapsDeskCatalog.HasReady)
            {
                RefreshFromCatalog();
            }
            else
            {
                _yards = Array.Empty<string>();
                _tracks = Array.Empty<string>();
                MapsDeskCatalog.EnsureStarted();
                _status = MapsDeskCatalog.MappingBanner.Length > 0
                    ? MapsDeskCatalog.MappingBanner
                    : "Station mapping…";
            }
        }

        private void RefreshFromCatalog()
        {
            var catalog = MapsDeskCatalog.Catalog;
            _yards = DestinationCatalog.ListYards(catalog);
            if (_yardIndex >= _yards.Count)
            {
                _yardIndex = 0;
            }

            RefreshTracks();
            SyncIndicesFromSession();
            var trackCount = 0;
            for (var i = 0; i < _yards.Count; i++)
            {
                trackCount += DestinationCatalog.ListTracksInYard(catalog, _yards[i]).Count;
            }

            var line = MapsDestTelemetry.FormatCatalog(_yards.Count, trackCount);
            _status = _yards.Count > 0
                ? _yards.Count + " cities / " + _tracks.Count + " tracks"
                : "no cities — reopen in world";
            EmitLog?.Invoke(line);
        }

        private void RefreshTracks()
        {
            var yard = _yards.Count > 0 ? _yards[_yardIndex] : null;
            _tracks = DestinationCatalog.ListTracksInYard(MapsDeskCatalog.Catalog, yard);
            if (_trackIndex >= _tracks.Count)
            {
                _trackIndex = 0;
            }
        }

        private void SyncIndicesFromSession()
        {
            if (RouteDestSession.YardId != null && _yards.Count > 0)
            {
                for (var i = 0; i < _yards.Count; i++)
                {
                    if (string.Equals(_yards[i], RouteDestSession.YardId, StringComparison.OrdinalIgnoreCase))
                    {
                        _yardIndex = i;
                        break;
                    }
                }
            }

            RefreshTracks();
            if (RouteDestSession.TrackId != null && _tracks.Count > 0)
            {
                for (var i = 0; i < _tracks.Count; i++)
                {
                    if (string.Equals(_tracks[i], RouteDestSession.TrackId, StringComparison.OrdinalIgnoreCase))
                    {
                        _trackIndex = i;
                        return;
                    }
                }
            }
        }

        private static string FormatRoutePathChip()
        {
            if (!RouteDestSession.HasDestination)
            {
                return "Path —";
            }

            if (RoutePlanSession.HasPlan)
            {
                return RoutePlanDisplay.FormatPathChip(RoutePlanSession.Plan) ?? "Path —";
            }

            return RoutePlanSession.IsStale
                ? RoutePlanSession.StatusMessage ?? "Path stale"
                : "Path …";
        }

        private static string? FormatRouteFacing()
        {
            var plan = RoutePlanSession.Plan;
            if (plan == null)
            {
                return null;
            }

            var behind = RouteFacingResolver.IsTargetBehind(plan, MapsRouteListener.Instance?.Graph);
            return RouteFacingDisplay.Format(plan, behind);
        }

        private static string? FormatRouteEtaRem()
        {
            var eta = RoutePlanSession.EtaCostSeconds;
            if (eta is not float seconds || !RoutePlanSession.HasPlan)
            {
                return null;
            }

            var etaChip = RouteEtaDisplay.Format(seconds);
            var rem = RouteEtaDisplay.FormatRemainingDistance(RoutePlanSession.RemainingMeters);
            if (rem == null)
            {
                return etaChip;
            }

            return etaChip + " | " + rem;
        }

        private void Publish(MapsDestKind kind)
        {
            YmsEventBus.RaiseMapsDestCommand(new MapsDestCommand(kind));
            var line = MapsDestTelemetry.Format(
                kind,
                RouteDestSession.YardId,
                RouteDestSession.TrackId);
            if (kind == MapsDestKind.RejectEmpty)
            {
                if (string.IsNullOrEmpty(_status) || _status.StartsWith("T2 ", StringComparison.Ordinal))
                {
                    _status = _yards.Count == 0 ? "no cities — reopen in world" : "pick city + track";
                }
            }
            else
            {
                _status = line;
            }

            EmitLog?.Invoke(line);
        }

        internal static bool HasDispatcherLicense()
        {
            try
            {
                var lm = LicenseManager.Instance;
                if (lm == null)
                {
                    return false;
                }

                var v2 = TransitionHelpers.ToV2(GeneralLicenseType.Dispatcher1);
                return v2 != null && lm.IsGeneralLicenseAcquired(v2);
            }
            catch
            {
                return false;
            }
        }
    }
}
