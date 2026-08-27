using System;
using System.Collections.Generic;
using DV;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Dispatch Desk: Route tab (**8.1–8.2**) + Per job Switch List (**8.3**).
    /// Ctrl+Insert. Set dest publishes Type A; route + Align are **8.2**.
    /// </summary>
    public sealed class MapsDeskPanel : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private enum DeskMode
        {
            Route,
            SwitchList,
        }

        private DeskMode _mode = DeskMode.Route;
        private bool _visible;
        private bool _worldSessionActive;
        private bool _yardDropOpen;
        private bool _trackDropOpen;
        private bool _jobDropOpen;
        private Vector2 _yardScroll;
        private Vector2 _trackScroll;
        private Vector2 _jobScroll;
        private Vector2 _stepScroll;
        private int _yardIndex;
        private int _trackIndex;
        private int _jobIndex;
        private string _status = string.Empty;
        private IReadOnlyList<string> _yards = Array.Empty<string>();
        private IReadOnlyList<string> _tracks = Array.Empty<string>();
        private List<Job> _jobs = new(8);

        private void OnDisable()
        {
            _visible = false;
            MapsDeskCatalog.Invalidate();
            _yards = Array.Empty<string>();
            _tracks = Array.Empty<string>();
            _jobs.Clear();
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
            var stepCount = SwitchListSession.Steps?.Count ?? 0;
            var h = _mode == DeskMode.SwitchList
                ? 380f
                : MapsDeskCatalog.IsMapping
                    ? 300f
                    : stepCount > 0
                        ? 420f
                        : 320f;
            var x = (Screen.width - w) * 0.5f;
            var y = Screen.height * 0.12f;
            GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher)");

            var row = y + 26f;
            if (GUI.Button(new Rect(x + 12, row, 100, 22), _mode == DeskMode.Route ? "● Route" : "Route"))
            {
                _mode = DeskMode.Route;
                _jobDropOpen = false;
            }

            if (GUI.Button(new Rect(x + 118, row, 120, 22), _mode == DeskMode.SwitchList ? "● Per job" : "Per job"))
            {
                _mode = DeskMode.SwitchList;
                _yardDropOpen = _trackDropOpen = false;
                RefreshJobs();
                _status = FormatSelectedJobStatus();
            }

            row += 28f;
            if (_mode == DeskMode.SwitchList)
            {
                DrawSwitchList(x, ref row, w);
            }
            else
            {
                DrawRoute(x, ref row, w);
            }
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

            var hasSteps = SwitchListSession.Steps != null && SwitchListSession.Steps.Count > 0;
            if (hasSteps)
            {
                if (GUI.Button(new Rect(x + 12, row, 100, 28), "Align step"))
                {
                    _yardDropOpen = _trackDropOpen = false;
                    AlignCurrentStep();
                }

                if (GUI.Button(new Rect(x + 118, row, 70, 28), "Next"))
                {
                    _yardDropOpen = _trackDropOpen = false;
                    AdvanceSwitchListStep();
                }

                row += 34f;
            }

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
                row += 28f;
            }

            DrawActiveSteps(x, ref row, w, emptyHint: null);
        }

        private void DrawSwitchList(float x, ref float row, float w)
        {
            var license = RouteAlignAccess.DeniedChip(HasDispatcherLicense())
                ?? "Dispatcher ok";
            GUI.Label(new Rect(x + 12, row, w - 24, 20), license);
            row += 22f;

            var jobLabel = _jobs.Count > 0 && _jobIndex < _jobs.Count
                ? (_jobs[_jobIndex].ID ?? "job")
                : "— no jobs (taken / held) —";
            GUI.Label(new Rect(x + 12, row, 40, 22), "Job");
            if (GUI.Button(new Rect(x + 55, row, 240, 24), jobLabel + " ▼"))
            {
                _jobDropOpen = !_jobDropOpen;
                RefreshJobs();
            }

            if (GUI.Button(new Rect(x + 300, row, 100, 24), "Refresh"))
            {
                RefreshJobs();
                _status = FormatSelectedJobStatus();
            }

            row += 28f;
            if (_jobDropOpen && _jobs.Count > 0)
            {
                var dropH = Mathf.Min(110f, 22f * _jobs.Count + 8f);
                _jobScroll = GUI.BeginScrollView(
                    new Rect(x + 55, row, 240, dropH),
                    _jobScroll,
                    new Rect(0, 0, 220, 22f * _jobs.Count));
                for (var i = 0; i < _jobs.Count; i++)
                {
                    var id = _jobs[i].ID ?? $"job{i}";
                    if (GUI.Button(new Rect(0, i * 22f, 220, 22), id))
                    {
                        _jobIndex = i;
                        _jobDropOpen = false;
                        _status = FormatSelectedJobStatus();
                    }
                }

                GUI.EndScrollView();
                row += dropH + 4f;
            }

            if (GUI.Button(new Rect(x + 12, row, 130, 26), "Load Switch List"))
            {
                _jobDropOpen = false;
                LoadSelectedJob();
            }

            if (GUI.Button(new Rect(x + 148, row, 100, 26), "Align step"))
            {
                AlignCurrentStep();
            }

            if (GUI.Button(new Rect(x + 254, row, 70, 26), "Next"))
            {
                AdvanceSwitchListStep();
            }

            if (GUI.Button(new Rect(x + 330, row, 70, 26), "Clear"))
            {
                SwitchListSession.Clear();
                _status = FormatSelectedJobStatus();
                EmitLog?.Invoke("T2 switch-list: cleared");
            }

            row += 30f;

            DrawActiveSteps(
                x,
                ref row,
                w,
                emptyHint: "Pick a taken or held job → Load list → Align step per leg.");

            var pathChip = FormatRoutePathChip();
            var facing = FormatRouteFacing() ?? "Facing —";
            GUI.Label(new Rect(x + 12, row, w - 24, 20), pathChip + "  |  " + facing);
            row += 22f;

            if (GUI.Button(new Rect(x + 12, row, 70, 26), "Hide"))
            {
                SetVisible(false);
            }

            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x + 90, row, w - 102, 26), _status);
            }
        }

        private void DrawActiveSteps(float x, ref float row, float w, string? emptyHint)
        {
            var steps = SwitchListSession.Steps;
            if (steps != null && steps.Count > 0)
            {
                var active = SwitchListSession.JobId ?? "";
                var cur = SwitchListSession.IsComplete
                    ? "done"
                    : (SwitchListSession.CurrentStep?.Label ?? "—");
                GUI.Label(new Rect(x + 12, row, w - 24, 20), active + " · " + cur);
                row += 22f;

                var listH = Mathf.Min(120f, 20f * steps.Count + 4f);
                _stepScroll = GUI.BeginScrollView(
                    new Rect(x + 12, row, w - 24, listH),
                    _stepScroll,
                    new Rect(0, 0, w - 48, 20f * steps.Count));
                for (var i = 0; i < steps.Count; i++)
                {
                    var mark = i == SwitchListSession.CurrentIndex && !SwitchListSession.IsComplete ? "▶ " : "  ";
                    GUI.Label(new Rect(0, i * 20f, w - 48, 20), mark + steps[i].Label);
                }

                GUI.EndScrollView();
                row += listH + 4f;
            }
            else if (!string.IsNullOrEmpty(emptyHint))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 40), emptyHint);
                row += 44f;
            }
        }

        private void RefreshJobs()
        {
            _jobs = new List<Job>(SwitchListJobReader.ListCandidateJobs());
            if (_jobIndex >= _jobs.Count)
            {
                _jobIndex = 0;
            }
        }

        /// <summary>
        /// Per job footer shows the HUD job id — not Route catalog "N cities / M tracks".
        /// </summary>
        private string FormatSelectedJobStatus()
        {
            if (SwitchListSession.HasActive && !string.IsNullOrEmpty(SwitchListSession.JobId))
            {
                return SwitchListSession.IsComplete
                    ? SwitchListSession.JobId + " · done"
                    : SwitchListSession.JobId!;
            }

            if (_jobs.Count > 0 && _jobIndex < _jobs.Count)
            {
                var id = _jobs[_jobIndex].ID?.Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    return id!;
                }
            }

            return _jobs.Count == 0 ? "no jobs" : _jobs.Count + " jobs";
        }

        private void LoadSelectedJob()
        {
            RefreshJobs();
            if (_jobs.Count == 0 || _jobIndex >= _jobs.Count)
            {
                _status = "no jobs";
                EmitLog?.Invoke("T2 switch-list: no jobs");
                return;
            }

            var job = _jobs[_jobIndex];
            if (!SwitchListJobReader.TryBuildSummary(job, out var summary, out var error) || summary == null)
            {
                _status = error ?? "cannot read job tracks";
                EmitLog?.Invoke("T2 switch-list: " + _status);
                SwitchListSession.Clear();
                return;
            }

            var steps = SwitchListPlanner.Build(summary);
            if (steps == null || steps.Count == 0)
            {
                _status = "planner fail-closed";
                EmitLog?.Invoke("T2 switch-list: planner fail-closed · " + summary.JobId);
                SwitchListSession.Clear();
                return;
            }

            SwitchListSession.Bind(summary.JobId, steps);
            _status = "loaded " + steps.Count + " steps · " + summary.JobId;
            EmitLog?.Invoke(
                "T2 switch-list: loaded " + summary.JobId + " · " + steps.Count + " steps · "
                + summary.OriginTrackId + " → " + summary.DestTrackId);

            var step = SwitchListSession.CurrentStep;
            if (step != null)
            {
                ApplyStepDest(step, "list-load");
            }
        }

        private void AdvanceSwitchListStep()
        {
            if (!SwitchListSession.HasActive)
            {
                _status = "no list";
                return;
            }

            if (SwitchListSession.IsComplete)
            {
                _status = "list complete";
                return;
            }

            if (!SwitchListSession.TryAdvance())
            {
                _status = SwitchListSession.IsComplete ? "list complete" : "no list";
                if (SwitchListSession.IsComplete)
                {
                    EmitLog?.Invoke("T2 switch-list: complete");
                }

                return;
            }

            var step = SwitchListSession.CurrentStep;
            if (step != null && !string.IsNullOrEmpty(step.DestTrackId))
            {
                ApplyStepDest(step, "list-next");
                _status = "step " + step.Index + ": " + step.Label;
                EmitLog?.Invoke("T2 switch-list: next · " + _status);
                return;
            }

            _status = step != null ? "step " + step.Index + ": " + step.Label : "advanced";
            EmitLog?.Invoke("T2 switch-list: next · " + _status);
        }

        private void AlignCurrentStep()
        {
            if (!SwitchListSession.HasActive || SwitchListSession.IsComplete)
            {
                _status = "no active step";
                return;
            }

            var step = SwitchListSession.CurrentStep;
            if (step == null || string.IsNullOrEmpty(step.DestTrackId))
            {
                _status = "no step track";
                return;
            }

            ApplyStepDest(step, "list-align");
            var line = MapsRouteListener.Instance?.TryAlignRoute() ?? "T2 align: unavailable";
            _status = line;
            EmitLog?.Invoke("T2 switch-list: align step " + step.Index + " " + step.Kind + " · " + _status);
        }

        private void ApplyStepDest(SwitchListStep step, string reason)
        {
            RouteDestSession.Set(step.DestYardId, step.DestTrackId);
            SyncIndicesFromSession();
            YmsEventBus.RaiseMapsDestCommand(new MapsDestCommand(MapsDestKind.Recheck));
            EmitLog?.Invoke("T2 switch-list: dest " + reason + " → " + step.DestTrackId);
        }

        private void ToggleDesk()
        {
            SetVisible(!_visible);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            _yardDropOpen = _trackDropOpen = _jobDropOpen = false;
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
