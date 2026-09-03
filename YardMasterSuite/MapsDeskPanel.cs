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
    /// Dispatch Desk: Route (**8.1–8.2**) + Per job (**8.3**) + Loco yard (**8.6**).
    /// Ctrl+Insert. Set dest publishes Type A; route + Align are **8.2**.
    /// </summary>
    public sealed class MapsDeskPanel : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private enum DeskMode
        {
            Route,
            SwitchList,
            LocoYard,
        }

        private DeskMode _mode = DeskMode.Route;
        private bool _visible;
        private bool _worldSessionActive;
        private bool _yardDropOpen;
        private bool _trackDropOpen;
        private bool _jobDropOpen;
        private bool _locoTypeDropOpen;
        private enum LocoSubMode
        {
            Turn,
            Bring,
        }

        private LocoSubMode _locoSub = LocoSubMode.Turn;
        private Vector2 _yardScroll;
        private Vector2 _trackScroll;
        private Vector2 _jobScroll;
        private Vector2 _stepScroll;
        private Vector2 _locoTypeScroll;
        private int _yardIndex;
        private int _trackIndex;
        private int _jobIndex;
        private int _locoTypeIndex;
        private string _status = string.Empty;
        private const float DeskLabelSeconds = 0.25f;
        private const float PrepArrivalSampleSeconds = 0.1f;
        private float _nextDeskLabelAt;
        private float _nextPrepArrivalAt;
        private bool _dispatcherOk = true;
        private float _nextLicenseAt;
        private string _deskYardBtn = "— pick city — ▼";
        private string _deskTrackBtn = "— pick track — ▼";
        private string _deskPathLicenseLine = string.Empty;
        private string _deskFacingEtaLine = string.Empty;
        private string _deskCoach1 = string.Empty;
        private string _deskCoach2 = string.Empty;
        private bool _deskCoachShow;
        private string _deskLicenseChip = "Dispatcher ok";
        private string _deskJobBtn = "— no jobs (taken / held) — ▼";
        private string _deskSlPathFacingLine = string.Empty;
        private readonly List<string> _deskStepLines = new(8);
        private IReadOnlyList<string> _yards = Array.Empty<string>();
        private IReadOnlyList<string> _tracks = Array.Empty<string>();
        private IReadOnlyList<string> _locoTypes = Array.Empty<string>();
        private List<Job> _jobs = new(8);

        internal static MapsDeskPanel? Instance { get; private set; }

        internal static bool IsDeskOpen => Instance != null && Instance._visible;

        /// <summary>**13.2.1:** 7.4 Done during Prep → same Next as the desk button.</summary>
        internal static void TryAdvanceAfterCoupleSuccess() =>
            Instance?.AdvanceFromCoupleSuccess();

        /// <summary>Desk stays open across pause; OnGUI skips draw while blocking overlay is up.</summary>
        internal static bool ShouldDrawDesk =>
            IsDeskOpen && !ScreenOverlayGate.IsBlocking();

        private static bool QuietCabPinReverse()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return false;
            }

            var boarded = PlayerManager.Car;
            var moving = boarded != null
                && RouteReverseHitchGate.ConsistIsMoving(
                    SpeedDisplay.ToKilometersPerHour(boarded.GetAbsSpeed()));
            return RouteReverseHitchGate.QuietCabDuringPinReverse(
                boarded != null && boarded.IsLoco,
                RoutePinLatch.TravelUsesReverse,
                RouteClearanceSession.Phase,
                moving);
        }

        private void OnEnable() => Instance = this;

        private void OnDisable()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }

            _visible = false;
            LocoRerailSession.Clear();
            MapsDeskCatalog.Invalidate();
            _yards = Array.Empty<string>();
            _tracks = Array.Empty<string>();
            _locoTypes = Array.Empty<string>();
            _jobs.Clear();
        }

        private void Update()
        {
            var world = WorldSessionGate.IsActive();
            if (!world)
            {
                if (_worldSessionActive)
                {
                    // Save/load leaves static dest + Switch List armed → PID drove
                    // without Set dest (wrong facing). Wipe drive sessions on leave.
                    YmsRouteSessions.ClearAll();
                    MapsDeskCatalog.Invalidate();
                    _worldSessionActive = false;
                }

                if (_visible)
                {
                    ApplyDeskMouseMode(false);
                }

                _visible = false;
                return;
            }

            _worldSessionActive = true;
            MaybePollPrepArrival();

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

            if (_visible && QuietCabPinReverse())
            {
                EmitLog?.Invoke("T2 maps-desk: hitch hide reverse");
                SetVisible(false);
            }
            else if (_visible)
            {
                MaybeRefreshDeskLabels();
            }

            if (LocoRerailSession.IsActive)
            {
                LocoRerailGovernor.PollPlaceTarget();
            }

            if (!HudWorldSession.IsActive(
                    PlayerManager.PlayerTransform != null,
                    ScreenOverlayGate.WorldReady())
                || ScreenOverlayGate.BlocksToolHotkeys())
            {
                return;
            }

            var control = YmsHotkeyPolicy.ControlHeld(
                Input.GetKey(KeyCode.LeftControl),
                Input.GetKey(KeyCode.RightControl));
            if (LocoRerailSession.IsActive
                && YmsHotkeyPolicy.ShouldAcceptToolChord(
                    control,
                    Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                _status = LocoRerailGovernor.ConfirmPlace(this);
                return;
            }

            if (YmsHotkeyPolicy.ShouldAcceptToolChord(control, Input.GetKeyDown(KeyCode.PageUp)))
            {
                TryChordAlign();
                return;
            }

            if (YmsHotkeyPolicy.ShouldAcceptToolChord(control, Input.GetKeyDown(KeyCode.PageDown)))
            {
                TryChordNext();
                return;
            }

            if (!YmsHotkeyPolicy.ShouldAcceptToolChord(control, Input.GetKeyDown(KeyCode.Insert)))
            {
                return;
            }

            if (!_visible && QuietCabPinReverse())
            {
                EmitLog?.Invoke("T2 maps-desk: hitch hold reverse");
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

            if (!_visible || !ShouldDrawDesk)
            {
                return;
            }

            const float w = 420f;
            var stepCount = SwitchListSession.Steps?.Count ?? 0;
            var h = _mode == DeskMode.SwitchList
                ? 380f
                : _mode == DeskMode.LocoYard
                    ? 360f
                    : MapsDeskCatalog.IsMapping
                        ? 300f
                        : stepCount > 0
                            ? 420f
                            : 320f;
            var x = (Screen.width - w) * 0.5f;
            var y = Screen.height * 0.12f;
            GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher)");

            var row = y + 26f;
            if (GUI.Button(new Rect(x + 12, row, 90, 22), _mode == DeskMode.Route ? "● Route" : "Route"))
            {
                _mode = DeskMode.Route;
                _jobDropOpen = false;
                _locoTypeDropOpen = false;
            }

            if (GUI.Button(new Rect(x + 108, row, 100, 22), _mode == DeskMode.SwitchList ? "● Per job" : "Per job"))
            {
                _mode = DeskMode.SwitchList;
                _yardDropOpen = _trackDropOpen = false;
                _locoTypeDropOpen = false;
                RefreshJobs();
                _status = FormatSelectedJobStatus();
            }

            if (GUI.Button(new Rect(x + 214, row, 90, 22), _mode == DeskMode.LocoYard ? "● Loco" : "Loco"))
            {
                _mode = DeskMode.LocoYard;
                _yardDropOpen = _trackDropOpen = _jobDropOpen = false;
                RefreshLocoTypes();
                _status = LocoRerailSession.IsActive
                    ? LocoRerailGovernor.FormatActiveChip()
                    : "loco turn / place";
            }

            row += 28f;
            if (_mode == DeskMode.SwitchList)
            {
                DrawSwitchList(x, ref row, w);
            }
            else if (_mode == DeskMode.LocoYard)
            {
                DrawLocoYard(x, ref row, w);
            }
            else
            {
                DrawRoute(x, ref row, w);
            }
        }

        private void DrawLocoYard(float x, ref float row, float w)
        {
            if (GUI.Button(new Rect(x + 12, row, 90, 22), _locoSub == LocoSubMode.Turn ? "● Turn" : "Turn"))
            {
                _locoSub = LocoSubMode.Turn;
                _locoTypeDropOpen = false;
                if (LocoRerailSession.IsActive)
                {
                    LocoRerailGovernor.CancelPlace();
                }

                _status = "turn look-at loco only";
            }

            if (GUI.Button(new Rect(x + 108, row, 100, 22), _locoSub == LocoSubMode.Bring ? "● Bring" : "Bring"))
            {
                _locoSub = LocoSubMode.Bring;
                RefreshLocoTypes();
                _status = "pick type · look at rail · Lock · Bring";
            }

            row += 28f;

            if (_locoSub == LocoSubMode.Turn)
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), LocoRerailGovernor.FormatLookAtLocoChip());
                row += 26f;
                if (GUI.Button(new Rect(x + 12, row, 200, 28), "Turn look-at loco"))
                {
                    _status = LocoRerailGovernor.TurnLookAtInPlace(this);
                }

                row += 34f;
                GUI.Label(new Rect(x + 12, row, w - 24, 40), "Point at the loco nose, then click. Solo only.");
                row += 44f;
            }
            else
            {
                var typeLabel = _locoTypes.Count > 0 && _locoTypeIndex >= 0 && _locoTypeIndex < _locoTypes.Count
                    ? _locoTypes[_locoTypeIndex]
                    : "— pick type —";
                GUI.Label(new Rect(x + 12, row, 50, 22), "Type");
                if (GUI.Button(new Rect(x + 70, row, 200, 24), typeLabel + " ▼"))
                {
                    _locoTypeDropOpen = !_locoTypeDropOpen;
                    if (_locoTypes.Count == 0)
                    {
                        RefreshLocoTypes();
                    }
                }

                if (GUI.Button(new Rect(x + 280, row, 70, 24), "Refresh"))
                {
                    RefreshLocoTypes();
                    _status = _locoTypes.Count + " loco types";
                }

                row += 28f;
                if (_locoTypeDropOpen && _locoTypes.Count > 0)
                {
                    var dropH = Mathf.Min(140f, 22f * _locoTypes.Count + 8f);
                    _locoTypeScroll = GUI.BeginScrollView(
                        new Rect(x + 70, row, 200, dropH),
                        _locoTypeScroll,
                        new Rect(0, 0, 180, 22f * _locoTypes.Count));
                    for (var i = 0; i < _locoTypes.Count; i++)
                    {
                        if (GUI.Button(new Rect(0, i * 22f, 180, 22), _locoTypes[i]))
                        {
                            _locoTypeIndex = i;
                            _locoTypeDropOpen = false;
                            _status = LocoRerailGovernor.BeginPlace(_locoTypes[i]);
                        }
                    }

                    GUI.EndScrollView();
                    row += dropH + 4f;
                }

                var chip = LocoRerailGovernor.FormatActiveChip();
                if (string.IsNullOrEmpty(chip))
                {
                    chip = "pick a type, then look at a rail";
                }

                GUI.Label(new Rect(x + 12, row, w - 24, 22), chip);
                row += 26f;

                if (GUI.Button(new Rect(x + 12, row, 90, 28), "Lock aim"))
                {
                    if (!LocoRerailSession.IsActive
                        && _locoTypes.Count > 0
                        && _locoTypeIndex >= 0
                        && _locoTypeIndex < _locoTypes.Count)
                    {
                        LocoRerailGovernor.BeginPlace(_locoTypes[_locoTypeIndex]);
                    }

                    _status = LocoRerailGovernor.LockAim();
                }

                if (GUI.Button(new Rect(x + 108, row, 110, 28), "Bring now"))
                {
                    if (!LocoRerailSession.IsActive
                        && _locoTypes.Count > 0
                        && _locoTypeIndex >= 0
                        && _locoTypeIndex < _locoTypes.Count)
                    {
                        LocoRerailGovernor.BeginPlace(_locoTypes[_locoTypeIndex]);
                    }

                    if (LocoRerailSession.HasLatchedTarget && !LocoRerailSession.IsTargetLocked)
                    {
                        LocoRerailSession.LockTarget();
                    }

                    _status = LocoRerailGovernor.ConfirmPlace(this);
                }

                if (GUI.Button(new Rect(x + 224, row, 70, 28), "Cancel"))
                {
                    _status = LocoRerailGovernor.CancelPlace();
                }

                row += 34f;
                GUI.Label(
                    new Rect(x + 12, row, w - 24, 36),
                    "Look at rail → Lock → Bring (or " + YmsHotkeyPolicy.LocoBringConfirmLegend
                    + "). Facing: use Turn tab after place.");
                row += 40f;
            }

            if (GUI.Button(new Rect(x + 12, row, 70, 28), "Hide"))
            {
                SetVisible(false);
            }

            row += 32f;
            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 40), _status);
            }
        }

        private void RefreshLocoTypes()
        {
            _locoTypes = LocoRerailGovernor.ListTypesOnMap();
            if (_locoTypeIndex >= _locoTypes.Count)
            {
                _locoTypeIndex = 0;
            }
        }

        private void DrawRoute(float x, ref float row, float w)
        {
            if (MapsDeskCatalog.IsMapping)
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), MapsDeskCatalog.MappingBanner);
                row += 26f;
            }

            GUI.Label(new Rect(x + 12, row, 50, 22), "City");
            if (GUI.Button(new Rect(x + 70, row, 200, 24), _deskYardBtn))
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
                        InvalidateDeskLabels();
                    }
                }

                GUI.EndScrollView();
                row += dropH + 4f;
            }

            GUI.Label(new Rect(x + 12, row, 50, 22), "Track");
            if (GUI.Button(new Rect(x + 70, row, 280, 24), _deskTrackBtn))
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
                        InvalidateDeskLabels();
                    }
                }

                GUI.EndScrollView();
                row += dropH + 4f;
            }

            var hasSteps = SwitchListSession.Steps != null && SwitchListSession.Steps.Count > 0;
            GUI.Label(new Rect(x + 12, row, w - 24, 22), _deskPathLicenseLine);
            row += 24f;

            if (hasSteps)
            {
                DrawActiveSteps(x, ref row, w, emptyHint: null, compact: true);
            }
            else if (_deskCoachShow)
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 20), _deskCoach1);
                row += 20f;
                GUI.Label(new Rect(x + 12, row, w - 24, 20), _deskCoach2);
                row += 22f;
            }

            if (!string.IsNullOrEmpty(_deskFacingEtaLine))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), _deskFacingEtaLine);
                row += hasSteps ? 22f : 26f;
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
                else if (!TryResolveSelectedTrack(out var city, out var trackId, out var resolveErr))
                {
                    _status = resolveErr ?? "pick city + track";
                    Publish(MapsDestKind.RejectEmpty);
                }
                else
                {
                    var wantTt = _trackIndex >= 0
                        && _trackIndex < _tracks.Count
                        && MapsTurntableDest.IsToken(_tracks[_trackIndex]);
                    SwitchListSession.Clear();
                    MapsTurntableMultiLeg.Disarm();
                    RouteSwitchListBinder.Disarm();

                    Publish(MapsDestApply.SetDest(city, trackId));
                    RouteSwitchListBinder.ArmForNextPlan();
                    if (wantTt)
                    {
                        MapsTurntableMultiLeg.Arm(city, trackId);
                    }

                    SyncIndicesFromSession();
                }
            }

            if (GUI.Button(new Rect(x + 118, row, 100, 28), "Recheck"))
            {
                _yardDropOpen = _trackDropOpen = false;
                if (!TryResolveSelectedTrack(out var city, out var trackId, out var resolveErr))
                {
                    _status = resolveErr ?? "pick city + track";
                    Publish(MapsDestKind.RejectEmpty);
                }
                else
                {
                    Publish(MapsDestApply.Recheck(city, trackId));
                }
            }

            if (!hasSteps && GUI.Button(new Rect(x + 226, row, 100, 28), "Align Route"))
            {
                _yardDropOpen = _trackDropOpen = false;
                var alignMsg = MapsRouteListener.Instance?.TryAlignRoute() ?? "T2 align: unavailable";
                _status = alignMsg;
            }

            row += 34f;

            if (hasSteps)
            {
                if (GUI.Button(new Rect(x + 12, row, 100, 28), "Align step"))
                {
                    _yardDropOpen = _trackDropOpen = false;
                    AlignCurrentStep();
                }

                DrawStepRunnerButtons(x, ref row, 118f);
                row += 34f;
            }

            if (GUI.Button(new Rect(x + 12, row, 70, 28), "Clear"))
            {
                MapsTurntableMultiLeg.Disarm();
                Publish(MapsDestApply.Clear());
                _status = "cleared";
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

            var cruise = PidCruiseSession.Enabled;
            var nextCruise = GUI.Toggle(new Rect(x + 268, row, 130, 28), cruise, "Cruise");
            if (nextCruise != cruise)
            {
                PidCruiseSession.SetEnabled(nextCruise);
                EmitLog?.Invoke(PidSpeedTelemetry.FormatCruise(nextCruise));
                _status = nextCruise ? "cruise on" : "cruise off — sit still";
            }

            row += 32f;
            if (!string.IsNullOrEmpty(_status) && !HasLongStatus(_status))
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 22), _status);
                row += 24f;
            }
        }

        private static bool HasLongStatus(string status) =>
            status.IndexOf(" until CLEARED", System.StringComparison.OrdinalIgnoreCase) >= 0
            || status.IndexOf("Reverse into", System.StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawSwitchList(float x, ref float row, float w)
        {
            GUI.Label(new Rect(x + 12, row, w - 24, 20), _deskLicenseChip);
            row += 22f;

            GUI.Label(new Rect(x + 12, row, 40, 22), "Job");
            if (GUI.Button(new Rect(x + 55, row, 240, 24), _deskJobBtn))
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

            if (CanOfferRemoteTake() && GUI.Button(new Rect(x + 150, row, 70, 26), "Take"))
            {
                TryRemoteTake(RemoteTakeSource.Desk);
            }

            if (GUI.Button(new Rect(x + 300, row, 70, 26), "Clear"))
            {
                MapsTurntableMultiLeg.Disarm();
                Publish(MapsDestApply.Clear());
                _status = FormatSelectedJobStatus();
                EmitLog?.Invoke("T2 switch-list: cleared");
            }

            row += 30f;

            if (GUI.Button(new Rect(x + 12, row, 100, 26), "Align step"))
            {
                AlignCurrentStep();
            }

            DrawStepRunnerButtons(x, ref row, 118f, buttonHeight: 26f);
            row += 30f;

            var cruise = PidCruiseSession.Enabled;
            var nextCruise = GUI.Toggle(new Rect(x + 12, row, 130, 26), cruise, "Cruise");
            if (nextCruise != cruise)
            {
                PidCruiseSession.SetEnabled(nextCruise);
                EmitLog?.Invoke(PidSpeedTelemetry.FormatCruise(nextCruise));
                _status = nextCruise ? "cruise on" : "cruise off — manual drive";
            }

            row += 30f;

            DrawActiveSteps(
                x,
                ref row,
                w,
                emptyHint: "Pick a taken or held job → Load list → Align step per leg.");

            GUI.Label(new Rect(x + 12, row, w - 24, 20), _deskSlPathFacingLine);
            row += 22f;

            if (_deskCoachShow)
            {
                GUI.Label(new Rect(x + 12, row, w - 24, 18), _deskCoach1);
                row += 18f;
                GUI.Label(new Rect(x + 12, row, w - 24, 18), _deskCoach2);
                row += 20f;
            }

            if (GUI.Button(new Rect(x + 12, row, 70, 26), "Hide"))
            {
                SetVisible(false);
            }

            if (!string.IsNullOrEmpty(_status))
            {
                GUI.Label(new Rect(x + 90, row, w - 102, 26), _status);
            }
        }

        private void DrawActiveSteps(float x, ref float row, float w, string? emptyHint, bool compact = false)
        {
            var steps = SwitchListSession.Steps;
            if (steps != null && steps.Count > 0)
            {
                if (!compact)
                {
                    var active = SwitchListSession.JobId ?? "";
                    var cur = SwitchListSession.IsComplete
                        ? "done"
                        : FormatCurrentStepLabel();
                    GUI.Label(new Rect(x + 12, row, w - 24, 20), active + " · " + cur);
                    row += 22f;
                }

                var listH = SwitchListStepDisplay.DeskListViewHeightPx(steps.Count, compact);
                _stepScroll = GUI.BeginScrollView(
                    new Rect(x + 12, row, w - 24, listH),
                    _stepScroll,
                    new Rect(0, 0, w - 48, (steps.Count * SwitchListStepDisplay.DeskLinePx) + 4));
                for (var i = 0; i < steps.Count; i++)
                {
                    var activeStep = i == SwitchListSession.CurrentIndex && !SwitchListSession.IsComplete;
                    var line = i < _deskStepLines.Count
                        ? _deskStepLines[i]
                        : SwitchListStepDisplay.FormatDeskLine(
                            steps[i], i, steps.Count, activeStep);
                    GUI.Label(new Rect(0, i * 20f, w - 48, 20), line);
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

            SwitchListOrientationInject.Apply(summary, MapsRouteListener.Instance?.Graph);

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

            var stalePin = RoutePinLatch.ResetForNewSwitchList();
            RouteClearanceSession.Clear();
            var drop = SwitchListRunner.FormatDropStalePinLog(stalePin);
            if (drop != null)
            {
                EmitLog?.Invoke(drop);
            }

            var step = SwitchListSession.CurrentStep;
            if (step != null
                && RouteStepDestPolicy.TryPinCorridorDest(
                    steps,
                    SwitchListSession.CurrentIndex,
                    out var pinYard,
                    out var pinTrack))
            {
                var kind = MapsDestApply.SetDest(pinYard ?? step.DestYardId, pinTrack);
                Publish(kind);
                EmitLog?.Invoke(
                    "T2 switch-list: dest list-load pin-corridor → " + pinTrack);
            }
            else if (step != null)
            {
                ApplyStepDest(step, "list-load");
            }

            InvalidateDeskLabels();
        }

        internal void ApplyRouteListStepDest(string reason)
        {
            var step = SwitchListSession.CurrentStep;
            if (step != null)
            {
                ApplyStepDest(step, reason);
                var n = SwitchListSession.Steps?.Count ?? 0;
                _status = "Step " + step.Index + "/" + n;
                InvalidateDeskLabels();
            }
        }

        private void TryChordAlign()
        {
            EmitLog?.Invoke("T2 maps-desk: chord align");
            if (SwitchListSession.HasActive && !SwitchListSession.IsComplete)
            {
                AlignCurrentStep();
                return;
            }

            var alignMsg = MapsRouteListener.Instance?.TryAlignRoute() ?? "T2 align: unavailable";
            _status = alignMsg;
        }

        private void AdvanceFromCoupleSuccess()
        {
            if (!SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
                    SwitchListSession.CurrentStep?.Kind,
                    SwitchListRunnerSession.Mode,
                    SwitchListSession.PeekNext != null,
                    coupleSuccess: true))
            {
                return;
            }

            EmitLog?.Invoke(SwitchListRunnerTelemetry.CoupleNext);
            AdvanceSwitchListStep();
        }

        private void TryChordNext()
        {
            EmitLog?.Invoke("T2 maps-desk: chord next");
            if (SwitchListSession.HasActive)
            {
                AdvanceSwitchListStep();
                return;
            }

            DismissRoutePinAfterCleared();
        }

        private void DismissRoutePinAfterCleared()
        {
            var pinForNext = RoutePinLatch.IsArmedForClearance(RoutePlanSession.Plan)
                || RouteClearanceSession.HasPin;
            if (RouteClearanceGate.Next(
                    pinForNext,
                    RouteClearanceSession.Phase) == RouteClearanceGateReason.NeedCleared)
            {
                _status = RouteClearanceGate.DenyNextLog;
                EmitLog?.Invoke(_status);
                return;
            }

            RoutePinLatch.DismissDisplay();
            RouteClearanceSession.Clear();
            _status = "pin hidden";
            EmitLog?.Invoke("T2 route-pin: hide next");
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

            if (SwitchListRunner.TryManualNext(SwitchListRunnerSession.Mode) != SwitchListRunnerResult.Ok)
            {
                _status = "next blocked — GO or Done first";
                EmitLog?.Invoke(SwitchListRunnerTelemetry.NextBlocked);
                return;
            }

            var pinForNext = PinBlocksAlignOrNext(SwitchListSession.CurrentStep);
            if (pinForNext
                && RouteClearanceGate.Next(
                    pinForNext,
                    RouteClearanceSession.Phase) == RouteClearanceGateReason.NeedCleared)
            {
                _status = RouteClearanceGate.DenyNextLog;
                EmitLog?.Invoke(_status);
                return;
            }

            var pinStays = SwitchListRunner.PinStaysAfterNext(
                SwitchListSession.CurrentStep,
                SwitchListSession.PeekNext);
            if (!SwitchListSession.TryAdvance())
            {
                _status = SwitchListSession.IsComplete ? "list complete" : "no list";
                if (SwitchListSession.IsComplete)
                {
                    EmitLog?.Invoke("T2 switch-list: complete");
                }

                return;
            }

            if (!pinStays)
            {
                RoutePinLatch.DismissDisplay();
                RouteClearanceSession.Clear();
                EmitLog?.Invoke("T2 route-pin: hide next");
            }

            var step = SwitchListSession.CurrentStep;
            if (step != null && !string.IsNullOrEmpty(step.DestTrackId))
            {
                ApplyStepDest(step, "list-next");
            }

            if (step != null && !string.IsNullOrEmpty(step.DestTrackId))
            {
                _status = "step " + step.Index + ": " + FormatStepLiveLabel(step);
                EmitLog?.Invoke("T2 switch-list: next · " + _status);
                InvalidateDeskLabels();
                return;
            }

            _status = step != null ? "step " + step.Index + ": " + FormatStepLiveLabel(step) : "advanced";
            EmitLog?.Invoke("T2 switch-list: next · " + _status);
            InvalidateDeskLabels();
        }

        private bool CanOfferRemoteTake()
        {
            if (_jobs.Count == 0 || _jobIndex >= _jobs.Count)
            {
                return false;
            }

            var job = _jobs[_jobIndex];
            if (!RemoteTakeWriter.TryReadPaper(job, out var previewHeld, out var alreadyTaken))
            {
                return false;
            }

            return RemoteTakeGate.CanOfferDeskTake(
                previewHeld,
                alreadyTaken,
                SwitchListSession.HasActive,
                RemoteTakeGate.ListJobMatches(job.ID, SwitchListSession.JobId),
                RemoteTakeWriter.ApiAllowsTake());
        }

        private void TryRemoteTake(RemoteTakeSource source)
        {
            RefreshJobs();
            if (_jobs.Count == 0 || _jobIndex >= _jobs.Count)
            {
                return;
            }

            var job = _jobs[_jobIndex];
            if (!RemoteTakeWriter.TryReadPaper(job, out var previewHeld, out var alreadyTaken))
            {
                return;
            }

            var input = new RemoteTakeInput(
                previewHeld,
                alreadyTaken,
                SwitchListSession.HasActive,
                RemoteTakeGate.ListJobMatches(job.ID, SwitchListSession.JobId),
                goArm: source == RemoteTakeSource.Go,
                deskTake: source == RemoteTakeSource.Desk,
                RemoteTakeWriter.ApiAllowsTake(),
                previewMetersRemaining: null);
            var decision = RemoteTakeGate.Evaluate(in input);
            if (decision == RemoteTakeDecision.NoOp)
            {
                return;
            }

            if (decision == RemoteTakeDecision.Request)
            {
                EmitLog?.Invoke(RemoteTakeTelemetry.FormatRequest(job.ID, source));
                if (RemoteTakeWriter.TryTake(job))
                {
                    EmitLog?.Invoke(RemoteTakeTelemetry.FormatTaken(job.ID));
                    _status = "taken " + (job.ID ?? "job");
                    RefreshJobs();
                    InvalidateDeskLabels();
                    return;
                }

                EmitLog?.Invoke(RemoteTakeTelemetry.Fail);
                _status = "take failed";
                return;
            }

            var refuse = RemoteTakeTelemetry.FormatRefuse(decision);
            if (refuse != null)
            {
                EmitLog?.Invoke(refuse);
            }

            _status = decision == RemoteTakeDecision.RefuseOfficeRequired
                ? "take needs office"
                : "take needs loaded list";
        }

        private void TrySetGoStep()
        {
            TryRemoteTake(RemoteTakeSource.Go);
            var step = SwitchListSession.CurrentStep;
            var pinForAlign = PinBlocksAlignOrNext(step);
            var result = SwitchListRunnerSession.TrySetGo(
                step,
                RoutePlanSession.HasPlan,
                pinForAlign,
                RouteClearanceSession.Phase);
            if (result != SwitchListRunnerResult.Ok)
            {
                var line = SwitchListRunnerTelemetry.FormatResult(result);
                if (!string.IsNullOrEmpty(line))
                {
                    EmitLog?.Invoke(line);
                }

                _status = result switch
                {
                    SwitchListRunnerResult.NeedPlan => "GO needs route plan",
                    SwitchListRunnerResult.NeedCleared => RouteClearanceGate.DenyAlignLog,
                    SwitchListRunnerResult.WrongStepKind => "GO only on Transit",
                    _ => "GO blocked",
                };
                return;
            }

            EmitLog?.Invoke(SwitchListRunnerTelemetry.Go);
            _status = "GO · step " + (step?.Index ?? 0);
        }

        private void TryMarkDoneStep()
        {
            var result = SwitchListRunnerSession.TryMarkDone();
            if (result != SwitchListRunnerResult.Ok)
            {
                EmitLog?.Invoke(SwitchListRunnerTelemetry.FormatResult(result));
                _status = "not on human step";
                return;
            }

            EmitLog?.Invoke(SwitchListRunnerTelemetry.Done);
            _status = "done — Next when ready";
        }

        private void TryStopGoStep()
        {
            var result = SwitchListRunnerSession.TryStopGo();
            if (result != SwitchListRunnerResult.Ok)
            {
                EmitLog?.Invoke(SwitchListRunnerTelemetry.FormatResult(result));
                _status = "not in GO";
                return;
            }

            EmitLog?.Invoke(SwitchListRunnerTelemetry.GoStop);
            _status = "GO stopped";
        }

        private void AlignCurrentStep()
        {
            if (!SwitchListSession.HasActive || SwitchListSession.IsComplete)
            {
                _status = SwitchListSession.HasActive && SwitchListSession.IsComplete
                    ? "list complete"
                    : "Load Switch List first";
                return;
            }

            var step = SwitchListSession.CurrentStep;
            if (step == null || string.IsNullOrEmpty(step.DestTrackId))
            {
                _status = "no step track";
                return;
            }

            var pinForAlign = PinBlocksAlignOrNext(step);
            if (pinForAlign
                && RouteClearanceGate.Align(
                    pinForAlign,
                    RouteClearanceSession.Phase) == RouteClearanceGateReason.NeedCleared)
            {
                _status = RouteClearanceGate.DenyAlignLog;
                EmitLog?.Invoke(_status);
                return;
            }

            ApplyStepDest(step, "list-align");
            var line = MapsRouteListener.Instance?.TryAlignRoute() ?? "T2 align: unavailable";
            _status = line;
            EmitLog?.Invoke("T2 switch-list: align step " + step.Index + " " + step.Kind + " · " + _status);
            InvalidateDeskLabels();
        }

        private void ApplyStepDest(SwitchListStep step, string reason)
        {
            if (!RouteStepDestPolicy.ShouldRetargetMapsDest(reason, RouteClearanceSession.Phase, step.Kind))
            {
                EmitLog?.Invoke("T2 switch-list: dest " + reason + " held until CLEARED");
                return;
            }

            if (RouteStepDestPolicy.ShouldSetPinCorridorDest(reason)
                && RouteStepDestPolicy.TryPinCorridorDest(
                    SwitchListSession.Steps,
                    SwitchListSession.CurrentIndex,
                    out var pinYard,
                    out var pinTrack))
            {
                var kind = MapsDestApply.SetDest(pinYard ?? step.DestYardId, pinTrack);
                SyncIndicesFromSession();
                Publish(kind);
                EmitLog?.Invoke(
                    "T2 switch-list: dest " + reason + " pin-corridor → " + pinTrack);
                return;
            }

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
            ApplyDeskMouseMode(_visible);
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

            RefreshDeskLabels();
            _nextDeskLabelAt = Time.unscaledTime + DeskLabelSeconds;
        }

        /// <summary>
        /// Pointer mode (click UI, no free-look) while desk is open — same path as
        /// vanilla pause/inventory via <see cref="DV.UI.CanvasProviderDV.RequirePointer"/>.
        /// </summary>
        private static void ApplyDeskMouseMode(bool deskOpen)
        {
            var canvas = UnityEngine.Object.FindObjectOfType<DV.UI.CanvasProviderDV>();
            if (canvas == null)
            {
                return;
            }

            canvas.RequirePointer(deskOpen);
        }

        private void RefreshFromCatalog()
        {
            var catalog = MapsDeskCatalog.Catalog;
            _yards = DestinationCatalog.ListYards(catalog);
            _yardIndex = MapsDeskDefaults.ResolveYardIndex(
                _yards,
                RouteDestSession.YardId,
                _yardIndex);
            RefreshTracks();
            SyncIndicesFromSession();
            if (RouteDestSession.TrackId == null)
            {
                _trackIndex = MapsDeskDefaults.ResolveTrackIndex(_tracks, null, _trackIndex);
            }

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
            InvalidateDeskLabels();
        }

        private void RefreshTracks()
        {
            var yard = _yards.Count > 0 ? _yards[_yardIndex] : null;
            var listed = DestinationCatalog.ListTracksInYard(MapsDeskCatalog.Catalog, yard);
            // Always offer Town TT even when catalog has no named tracks yet.
            _tracks = yard != null
                ? MapsTurntableDest.WithTokenFirst(listed)
                : listed;
            _trackIndex = MapsDeskDefaults.ResolveTrackIndex(
                _tracks,
                RouteDestSession.TrackId,
                _trackIndex);
        }

        /// <summary>
        /// City + Track dropdown → real graph id. <see cref="MapsTurntableDest.Token"/>
        /// resolves via FoT once per catalog session (cached).
        /// </summary>
        private bool TryResolveSelectedTrack(out string city, out string trackId, out string? error)
        {
            city = string.Empty;
            trackId = string.Empty;
            error = null;
            if (_yards.Count == 0 || _tracks.Count == 0)
            {
                error = _yards.Count == 0 ? "no cities — reopen in world" : "pick city + track";
                return false;
            }

            city = _yards[_yardIndex];
            var selected = _tracks[_trackIndex];
            float ox = 0f;
            float oz = 0f;
            try
            {
                var player = PlayerManager.PlayerTransform;
                if (player != null)
                {
                    var p = player.position;
                    ox = p.x;
                    oz = p.z;
                }
            }
            catch
            {
                // origin stays 0,0 — distance tie-break only
            }

            return MapsTurntableDest.TryResolveTrackId(
                city,
                selected,
                yard => TurntableLocator.TryResolveTrackId(yard, ox, oz),
                out trackId,
                out error);
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
            if (RouteDestSession.TrackId == null || _tracks.Count == 0)
            {
                return;
            }

            // Anonymous TT dest is not in the named catalog — keep Turntable selected.
            if (PathRouteConstraints.IsAnonymousTrack(RouteDestSession.TrackId)
                && MapsTurntableDest.IsToken(_tracks[0]))
            {
                _trackIndex = 0;
                return;
            }

            for (var i = 0; i < _tracks.Count; i++)
            {
                if (string.Equals(_tracks[i], RouteDestSession.TrackId, StringComparison.OrdinalIgnoreCase))
                {
                    _trackIndex = i;
                    return;
                }
            }
        }

        private void InvalidateDeskLabels() => _nextDeskLabelAt = 0f;

        private void MaybePollPrepArrival()
        {
            if (Time.unscaledTime < _nextPrepArrivalAt)
            {
                return;
            }

            _nextPrepArrivalAt = Time.unscaledTime + PrepArrivalSampleSeconds;
            if (!SwitchListSession.HasActive || SwitchListSession.IsComplete)
            {
                return;
            }

            var step = SwitchListSession.CurrentStep;
            if (step == null || step.Kind != SwitchListStepKind.Prep)
            {
                if (PrepTrackArrivalSession.AtSpur)
                {
                    PrepTrackArrivalSession.TryArrive(PrepTrackArrival.OffTrack);
                    InvalidateDeskLabels();
                }

                return;
            }

            var wasAt = PrepTrackArrivalSession.AtSpur;
            var loco = UsableTrainProbe.TryGetUsableLoco();
            if (!LocoTrackProbe.TryResolvePrepPose(
                    loco,
                    out var locoTrackId,
                    out var spanMeters,
                    out var trackLengthMeters,
                    out var uniqueTrack))
            {
                PrepTrackArrivalSession.TryArrive(PrepTrackArrival.Ambiguous);
                return;
            }

            var rose = SwitchListSession.TryArrivePrepTrack(
                step.DestTrackId,
                locoTrackId,
                spanMeters,
                trackLengthMeters,
                uniqueTrack);
            if (rose)
            {
                EmitLog?.Invoke(SwitchListRunnerTelemetry.PrepAtTrack);
                _status = PrepTrackArrivalGate.FormatDeskCue(step.DestTrackId);
                InvalidateDeskLabels();
                return;
            }

            if (wasAt != PrepTrackArrivalSession.AtSpur)
            {
                InvalidateDeskLabels();
            }
        }

        private void MaybeRefreshDeskLabels()
        {
            var now = Time.unscaledTime;
            if (now < _nextDeskLabelAt)
            {
                return;
            }

            _nextDeskLabelAt = now + DeskLabelSeconds;
            RefreshDeskLabels();
        }

        private bool CachedDispatcherOk()
        {
            var now = Time.unscaledTime;
            if (now >= _nextLicenseAt)
            {
                _dispatcherOk = HasDispatcherLicense();
                _nextLicenseAt = now + 2f;
            }

            return _dispatcherOk;
        }

        private static string JoinChips(string left, string? mid, string right) =>
            string.IsNullOrEmpty(mid) ? left + "  |  " + right : left + "  |  " + mid + "  |  " + right;

        private void RefreshDeskLabels()
        {
            var yard = _yards.Count > 0 && _yardIndex >= 0 && _yardIndex < _yards.Count
                ? _yards[_yardIndex]
                : "— pick city —";
            var track = _tracks.Count > 0 && _trackIndex >= 0 && _trackIndex < _tracks.Count
                ? _tracks[_trackIndex]
                : "— pick track —";
            _deskYardBtn = yard + " ▼";
            _deskTrackBtn = track + " ▼";
            _deskLicenseChip = RouteAlignAccess.DeniedChip(CachedDispatcherOk()) ?? "Dispatcher ok";
            var pathChip = FormatRoutePathChip();
            var pinChip = PinCaptionForDesk();
            _deskPathLicenseLine = JoinChips(pathChip, pinChip, _deskLicenseChip);
            var facing = FormatRouteFacing();
            var etaRem = FormatRouteEtaRem();
            var hasSteps = SwitchListSession.Steps != null && SwitchListSession.Steps.Count > 0;
            if (!hasSteps && (!string.IsNullOrEmpty(facing) || !string.IsNullOrEmpty(etaRem)))
            {
                _deskFacingEtaLine = (facing ?? "—") + "  |  " + (etaRem ?? "—");
            }
            else if (!string.IsNullOrEmpty(etaRem))
            {
                _deskFacingEtaLine = etaRem ?? string.Empty;
            }
            else
            {
                _deskFacingEtaLine = string.Empty;
            }

            var coach = FormatSwitchCoach();
            _deskCoachShow = coach.Show;
            _deskCoach1 = coach.Step1 ?? string.Empty;
            _deskCoach2 = coach.Step2 ?? string.Empty;

            var jobLabel = _jobs.Count > 0 && _jobIndex < _jobs.Count
                ? (_jobs[_jobIndex].ID ?? "job")
                : "— no jobs (taken / held) —";
            _deskJobBtn = jobLabel + " ▼";
            _deskSlPathFacingLine = JoinChips(pathChip, pinChip, facing ?? "Facing —");

            _deskStepLines.Clear();
            var steps = SwitchListSession.Steps;
            if (steps != null)
            {
                var plan = RoutePlanSession.Plan;
                var graph = MapsRouteListener.Instance?.Graph;
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var activeStep = i == SwitchListSession.CurrentIndex && !SwitchListSession.IsComplete;
                    bool? driveReverse = null;
                    if (activeStep)
                    {
                        driveReverse = SwitchListStepDisplay.ResolveDriveNeedsReverse(
                            step,
                            RouteClearanceSession.Phase,
                            RoutePinLatch.IsArmedForClearance(plan),
                            RouteClearanceSession.HasPin,
                            RoutePinLatch.HasLatch,
                            RoutePinLatch.TravelUsesReverse,
                            RouteFacingResolver.IsPinBehind(plan, graph),
                            ResolveStepDestBehind(step, plan, graph));
                    }

                    _deskStepLines.Add(SwitchListStepDisplay.FormatDeskLine(
                        step, i, steps.Count, activeStep, driveReverse,
                        atTrack: activeStep
                            && step.Kind == SwitchListStepKind.Prep
                            && PrepTrackArrivalSession.AtSpur));
                }
            }
        }

        private static bool ResolveStepDestBehind(
            SwitchListStep step,
            PathPlanResult? plan,
            PathGraphMapper? graph)
        {
            var destTrack = step.DestTrackId;
            if (!string.IsNullOrWhiteSpace(destTrack)
                && plan != null
                && plan.TrackIds.Count > 0
                && string.Equals(
                    plan.TrackIds[plan.TrackIds.Count - 1],
                    destTrack,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return RouteFacingResolver.IsDestBehind(plan, graph);
            }

            return RouteFacingResolver.IsTrackBehind(graph, destTrack);
        }

        private static bool? ResolveActiveStepDriveReverse(SwitchListStep? step)
        {
            if (step == null)
            {
                return null;
            }

            var plan = RoutePlanSession.Plan;
            var graph = MapsRouteListener.Instance?.Graph;
            return SwitchListStepDisplay.ResolveDriveNeedsReverse(
                step,
                RouteClearanceSession.Phase,
                RoutePinLatch.IsArmedForClearance(plan),
                RouteClearanceSession.HasPin,
                RoutePinLatch.HasLatch,
                RoutePinLatch.TravelUsesReverse,
                RouteFacingResolver.IsPinBehind(plan, graph),
                ResolveStepDestBehind(step, plan, graph));
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

        private static string FormatCurrentStepLabel()
        {
            var step = SwitchListSession.CurrentStep;
            return step == null ? "—" : FormatStepLiveLabel(step);
        }

        private static string FormatStepLiveLabel(SwitchListStep step) =>
            SwitchListStepDisplay.LiveLabel(step, ResolveActiveStepDriveReverse(step));

        private static string? FormatRouteFacing()
        {
            var plan = RoutePlanSession.Plan;
            if (plan == null)
            {
                return null;
            }

            var behind = RouteFacingResolver.DeskFacingNeedsReverse(plan, MapsRouteListener.Instance?.Graph);
            return RouteFacingDisplay.Format(plan, behind);
        }

        private static string? PinCaptionForDesk()
        {
            var step = SwitchListSession.CurrentStep;
            if (step != null && !SwitchListRunner.StepUsesApproachPinFacing(step.Kind))
            {
                return null;
            }

            return RouteClearanceSession.Caption;
        }

        private static bool PinBlocksAlignOrNext(SwitchListStep? step) =>
            SwitchListRunner.PinBlocksAlignOrNext(
                step,
                RoutePinLatch.IsArmedForClearance(RoutePlanSession.Plan),
                RouteClearanceSession.HasPin);

        private static RouteSwitchCoachLines FormatSwitchCoach()
        {
            var plan = RoutePlanSession.Plan;
            var graph = MapsRouteListener.Instance?.Graph;
            var step = SwitchListSession.CurrentStep;
            var pinLeg = SwitchListRunner.PinDisplayAllowed(
                step,
                SwitchListSession.HasActive);
            return RouteSwitchCoach.Format(
                pinArmed: pinLeg
                    && SwitchListRouteLeg.ShouldArmPin(plan)
                    && RoutePinLatch.ShowPin,
                phase: RouteClearanceSession.Phase,
                pinIsBehind: RoutePinLatch.TravelUsesReverse,
                destIsBehind: RouteFacingResolver.IsDestBehind(plan, graph));
        }

        private void DrawStepRunnerButtons(
            float x,
            ref float row,
            float startX,
            float buttonHeight = 28f)
        {
            var step = SwitchListSession.CurrentStep;
            var runMode = SwitchListRunnerSession.Mode;
            var nextX = startX;

            if (runMode == SwitchListRunMode.HumanHold)
            {
                if (GUI.Button(new Rect(x + nextX, row, 70, buttonHeight), "Done"))
                {
                    TryMarkDoneStep();
                }

                nextX += 78f;
            }
            else if (runMode == SwitchListRunMode.Go)
            {
                if (GUI.Button(new Rect(x + nextX, row, 70, buttonHeight), "Stop GO"))
                {
                    TryStopGoStep();
                }

                nextX += 78f;
            }
            else if (step != null && SwitchListRunner.StepSupportsGo(step.Kind))
            {
                if (GUI.Button(new Rect(x + nextX, row, 50, buttonHeight), "GO"))
                {
                    TrySetGoStep();
                }

                nextX += 58f;
            }

            if (SwitchListRunnerSession.AllowsManualNext
                && GUI.Button(new Rect(x + nextX, row, 70, buttonHeight), "Next"))
            {
                AdvanceSwitchListStep();
            }
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
            InvalidateDeskLabels();
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
