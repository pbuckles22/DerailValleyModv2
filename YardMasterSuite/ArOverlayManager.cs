using System;
using System.IO;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Office + own-loco + Home pin + other-loco radar + job-car pickup pins.
    /// Fixed buffers; LateUpdate projects; OnGUI draws cached GUIContent.
    /// </summary>
    public sealed class ArOverlayManager : MonoBehaviour
    {
        private const float IconPixels = ArMarkerDisplay.IconPixels;
        private const float LabelHeight = ArMarkerDisplay.GlyphLabelHeightPixels;
        private const float RadarLabelHeight = ArMarkerDisplay.RadarLabelHeightPixels;
        private const float VerticalLiftMeters = 3.5f;
        private const float PinVerticalLiftMeters = 0.6f;
        private static readonly Color OfficeColor = new Color(0.25f, 0.85f, 0.35f, 0.95f);
        private static readonly Color LocoColor = new Color(0.31f, 0.76f, 0.97f, 0.95f);
        private static readonly Color OtherLocoColor = new Color(1f, 0.72f, 0.28f, 0.95f);
        private static readonly Color PinColor = new Color(1f, 0.84f, 0.31f, 0.95f);
        private static readonly Color RouteClearedPinColor = new Color(0.25f, 0.9f, 0.4f, 0.95f);
        private static readonly Color JobCarColor = new Color(0.78f, 0.49f, 1f, 1f);
        private const int StackSlotCount =
            ArMarkerBuffer.Capacity
            + LocoRadarSelection.DefaultMaxResults
            + JobCarMarkerDisplay.DefaultMaxMarkers;

        internal static Action<string>? EmitLog;

        private readonly ArMarkerSlot[] _slots = ArMarkerBuffer.Create();
        private readonly ArMarkerSlot[] _radarSlots = CreateRadarSlots();
        private readonly ArMarkerSlot[] _jobCarSlots = CreateJobCarSlots();
        private readonly ArMarkerSlot[] _stackSlots = new ArMarkerSlot[StackSlotCount];
        private readonly float[] _stackCaptionWidths = new float[StackSlotCount];
        private readonly GUIContent _officeGlyph = new GUIContent("");
        private readonly GUIContent _locoGlyph = new GUIContent("");
        private readonly GUIContent _pinGlyph = new GUIContent("");
        private readonly GUIContent[] _radarGlyphs = CreateRadarGlyphs();
        private readonly GUIContent[] _jobCarGlyphs = CreateJobCarGlyphs();
        private readonly GuiContentCache _radarCaptions = new GuiContentCache(LocoRadarSelection.DefaultMaxResults);
        private readonly GuiContentCache _jobCarCaptions = new GuiContentCache(JobCarMarkerDisplay.DefaultMaxMarkers);

        private GUIStyle? _style;
        private Texture2D? _officeIcon;
        private Texture2D? _locoIcon;
        private Texture2D? _radarIcon;
        private Texture2D? _pinIcon;
        private Texture2D? _jobCarIcon;
        private ArOverlaySnapshot? _previous;
        private bool _officeBehind;
        private ArHorizontalEdge _officeEdge = ArHorizontalEdge.None;
        private bool _locoBehind;
        private ArHorizontalEdge _locoEdge = ArHorizontalEdge.None;
        private bool _pinBehind;
        private ArHorizontalEdge _pinEdge = ArHorizontalEdge.None;
        private readonly bool[] _radarBehind = new bool[LocoRadarSelection.DefaultMaxResults];
        private readonly ArHorizontalEdge[] _radarEdge =
            new ArHorizontalEdge[LocoRadarSelection.DefaultMaxResults];
        private readonly bool[] _jobCarBehind = new bool[JobCarMarkerDisplay.DefaultMaxMarkers];
        private readonly ArHorizontalEdge[] _jobCarEdge =
            new ArHorizontalEdge[JobCarMarkerDisplay.DefaultMaxMarkers];
        private ArPlacementHistogram _placeHist;
        private bool _wasInWorld;
        private float _lastArLogAt = -999f;
        private static ArOverlayManager? _live;

        internal static void FlushPending()
        {
            _live?.EmitPlaceSummary(force: true);
        }

        private void OnEnable()
        {
            _previous = null;
            _officeBehind = false;
            _officeEdge = ArHorizontalEdge.None;
            _locoBehind = false;
            _locoEdge = ArHorizontalEdge.None;
            _pinBehind = false;
            _pinEdge = ArHorizontalEdge.None;
            for (var i = 0; i < _radarBehind.Length; i++)
            {
                _radarBehind[i] = false;
                _radarEdge[i] = ArHorizontalEdge.None;
            }

            for (var i = 0; i < _jobCarBehind.Length; i++)
            {
                _jobCarBehind[i] = false;
                _jobCarEdge[i] = ArHorizontalEdge.None;
            }

            _placeHist = default;
            _wasInWorld = false;
            _lastArLogAt = -999f;
            _live = this;
            StationOfficeAnchor.Clear();
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)]);
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)]);
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Pin)]);
            HideAllRadar();
            HideAllJobCars();
            LocoRadarProbe.Clear();
            JobCarArProbe.Clear();
            _officeGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Station);
            _locoGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Loco);
            _pinGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Pin);
        }

        private void OnDisable()
        {
            EmitPlaceSummary(force: true);
            if (_live == this)
            {
                _live = null;
            }

            StationOfficeAnchor.Clear();
            LocoRadarProbe.Clear();
            JobCarArProbe.Clear();
            DestroyIcons();
        }

        private void LateUpdate()
        {
            var playerPresent = PlayerManager.PlayerTransform != null;
            var worldSession = WorldSessionGate.IsActive();
            if (!worldSession)
            {
                HideOffice();
                HideLoco();
                HidePin();
                HideAllRadar();
                HideAllJobCars();
                if (_previous != null)
                {
                    EmitIfChanged();
                    _previous = null;
                }

                if (_wasInWorld)
                {
                    EmitPlaceSummary(force: true);
                    ScreenOverlayGate.InvalidateHandles();
                    if (LocoRadarScanPolicy.ShouldInvalidateCache(
                            wasInWorld: true,
                            inWorld: false))
                    {
                        LocoRadarProbe.Clear();
                    }
                }

                _wasInWorld = false;
                return;
            }

            if (!ArVisible(playerPresent))
            {
                HideOffice();
                HideLoco();
                HidePin();
                HideAllRadar();
                HideAllJobCars();
                return;
            }

            if (LocoRadarScanPolicy.ShouldForceScanOnWorldEnter(_wasInWorld, inWorld: true))
            {
                LocoRadarProbe.MarkWorldEnter();
            }

            _wasInWorld = true;

            var cam = PlayerManager.ActiveCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                HideOffice();
                HideLoco();
                HidePin();
                HideAllRadar();
                HideAllJobCars();
                EmitIfChanged();
                return;
            }

            UpdateOffice(cam);
            UpdateLoco(cam);
            UpdatePin(cam);
            UpdateRadar(cam);
            UpdateJobCars(cam);
            RefreshCaptionWidths(measureWithStyle: false);
            ApplyCombinedEdgeStack();
            ArPlacementStats.Record(
                _slots,
                Screen.height,
                Time.unscaledTime,
                ref _placeHist,
                HudStackLayout.LastBottomGuiY);
            EmitIfChanged();
            EmitPlaceSummary(force: false);
        }

        private void UpdateOffice(Camera cam)
        {
            if (!StationOfficeAnchor.TryGet(out var office, out var px, out var pz))
            {
                HideOffice();
                return;
            }

            var atOffice = ArOfficeGate.IsAtOffice(office.x, office.z, px, pz);
            if (!ArOfficeGate.ShouldShow(hasInZoneStation: true, atOffice))
            {
                HideOffice();
                return;
            }

            var world = office;
            world.y += VerticalLiftMeters;
            ProjectIntoSlot(
                cam,
                world,
                px,
                pz,
                ArWaypointKind.Station,
                ref _officeBehind,
                ref _officeEdge);
        }

        private void UpdateLoco(Camera cam)
        {
            var lastLoco = PlayerManager.LastLoco;
            var usableLoco = ArLocoMarkerSource.ShouldProbeUsableLoco(lastLoco != null)
                ? UsableTrainProbe.TryGetUsableLoco()
                : null;
            var pick = ArLocoMarkerSource.Pick(lastLoco != null, usableLoco != null);
            var loco = pick switch
            {
                ArLocoMarkerPick.LastLoco => lastLoco,
                ArLocoMarkerPick.UsableLoco => usableLoco,
                _ => null,
            };
            var player = PlayerManager.PlayerTransform;
            if (loco == null
                || player == null
                || !ArLocoGate.ShouldShow(
                    hasLoco: true,
                    playerIsOnThatLoco: IsExactlyLastLoco(PlayerManager.Car, loco)))
            {
                HideLoco();
                return;
            }

            var pos = player.position;
            var world = loco.transform.position;
            world.y += VerticalLiftMeters;
            ProjectIntoSlot(
                cam,
                world,
                pos.x,
                pos.z,
                ArWaypointKind.Loco,
                ref _locoBehind,
                ref _locoEdge);
        }

        private void UpdatePin(Camera cam)
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                HidePin();
                return;
            }

            // Route pin (**8.7**) wins over park mark while Maps/SL has a junction pin.
            if (RouteClearanceSession.TryGetPinWorld(out var routeX, out var routeY, out var routeZ))
            {
            var caption = RouteClearanceSession.Caption;
            var pinText = string.IsNullOrEmpty(caption) ? "PIN" : caption;
            if (_pinGlyph.text != pinText)
            {
                _pinGlyph.text = pinText;
            }
                var world = new Vector3(routeX, routeY, routeZ);
                world.y += PinVerticalLiftMeters;
                var pos = player.position;
                ProjectIntoSlot(
                    _slots,
                    ArMarkerBuffer.SlotOf(ArWaypointKind.Pin),
                    cam,
                    world,
                    pos.x,
                    pos.z,
                    ArWaypointKind.Pin,
                    ref _pinBehind,
                    ref _pinEdge);
                return;
            }

            _pinGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Pin);
            if (!ParkMarkSession.TryGet(out var pinX, out var pinY, out var pinZ))
            {
                HidePin();
                return;
            }

            var parkPos = player.position;
            var atPin = ArPinGate.IsAtPin(pinX, pinZ, parkPos.x, parkPos.z);
            if (!ArPinGate.ShouldShow(hasMark: true, atPin))
            {
                HidePin();
                return;
            }

            var parkWorld = new Vector3(pinX, pinY, pinZ);
            parkWorld.y += PinVerticalLiftMeters;
            ProjectIntoSlot(
                _slots,
                ArMarkerBuffer.SlotOf(ArWaypointKind.Pin),
                cam,
                parkWorld,
                parkPos.x,
                parkPos.z,
                ArWaypointKind.Pin,
                ref _pinBehind,
                ref _pinEdge);
        }

        private void UpdateRadar(Camera cam)
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                HideAllRadar();
                return;
            }

            LocoRadarProbe.Ensure(EmitLog);
            var pos = player.position;
            var n = LocoRadarProbe.Count;
            for (var i = 0; i < _radarSlots.Length; i++)
            {
                if (i >= n || !LocoRadarProbe.TryGet(i, out var world, out var caption))
                {
                    HideRadar(i);
                    continue;
                }

                world.y += VerticalLiftMeters;
                ProjectIntoSlot(
                    _radarSlots,
                    i,
                    cam,
                    world,
                    pos.x,
                    pos.z,
                    ArWaypointKind.OtherLoco,
                    ref _radarBehind[i],
                    ref _radarEdge[i]);
                if (_radarCaptions.TryCommit(i, caption, out var text))
                {
                    _radarGlyphs[i].text = text;
                }
            }
        }

        private void UpdateJobCars(Camera cam)
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                HideAllJobCars();
                return;
            }

            JobCarArProbe.Ensure(EmitLog);
            var pos = player.position;
            var n = JobCarArProbe.Count;
            for (var i = 0; i < _jobCarSlots.Length; i++)
            {
                if (i >= n || !JobCarArProbe.TryGet(i, out var world, out var caption))
                {
                    HideJobCar(i);
                    continue;
                }

                world.y += VerticalLiftMeters;
                ProjectIntoSlot(
                    _jobCarSlots,
                    i,
                    cam,
                    world,
                    pos.x,
                    pos.z,
                    ArWaypointKind.JobCar,
                    ref _jobCarBehind[i],
                    ref _jobCarEdge[i]);
                if (_jobCarCaptions.TryCommit(i, caption, out var text))
                {
                    _jobCarGlyphs[i].text = text;
                }
            }
        }

        private void ApplyCombinedEdgeStack()
        {
            var nPrimary = ArMarkerBuffer.Capacity;
            var nRadar = _radarSlots.Length;
            var nJob = _jobCarSlots.Length;
            for (var i = 0; i < nPrimary; i++)
            {
                _stackSlots[i] = _slots[i];
            }

            for (var i = 0; i < nRadar; i++)
            {
                _stackSlots[nPrimary + i] = _radarSlots[i];
            }

            for (var i = 0; i < nJob; i++)
            {
                _stackSlots[nPrimary + nRadar + i] = _jobCarSlots[i];
            }

            ArEdgeStackLayout.Apply(
                _stackSlots,
                Screen.width,
                Screen.height,
                hudBottomGuiY: HudStackLayout.LastBottomGuiY,
                iconPixels: IconPixels,
                captionWidths: _stackCaptionWidths);

            for (var i = 0; i < nPrimary; i++)
            {
                _slots[i].GuiX = _stackSlots[i].GuiX;
            }

            for (var i = 0; i < nRadar; i++)
            {
                _radarSlots[i].GuiX = _stackSlots[nPrimary + i].GuiX;
            }

            for (var i = 0; i < nJob; i++)
            {
                _jobCarSlots[i].GuiX = _stackSlots[nPrimary + nRadar + i].GuiX;
            }
        }

        private void RefreshCaptionWidths(bool measureWithStyle)
        {
            SetStackCaptionWidth(
                ArMarkerBuffer.SlotOf(ArWaypointKind.Station),
                _officeGlyph,
                measureWithStyle);
            SetStackCaptionWidth(
                ArMarkerBuffer.SlotOf(ArWaypointKind.Loco),
                _locoGlyph,
                measureWithStyle);
            SetStackCaptionWidth(
                ArMarkerBuffer.SlotOf(ArWaypointKind.Pin),
                _pinGlyph,
                measureWithStyle);
            var nPrimary = ArMarkerBuffer.Capacity;
            for (var i = 0; i < _radarGlyphs.Length; i++)
            {
                SetStackCaptionWidth(nPrimary + i, _radarGlyphs[i], measureWithStyle);
            }

            var nRadar = _radarGlyphs.Length;
            for (var i = 0; i < _jobCarGlyphs.Length; i++)
            {
                SetStackCaptionWidth(nPrimary + nRadar + i, _jobCarGlyphs[i], measureWithStyle);
            }
        }

        private void SetStackCaptionWidth(int index, GUIContent glyph, bool measureWithStyle)
        {
            float width;
            if (measureWithStyle && _style != null)
            {
                width = _style.CalcSize(glyph).x;
            }
            else
            {
                width = ArEdgeStackLayout.EstimateCaptionWidthPixels(glyph.text);
            }

            _stackCaptionWidths[index] = width;
        }

        private float CaptionWidthForStack(int stackIndex, ArWaypointKind kind)
        {
            var width = _stackCaptionWidths[stackIndex];
            return width > 0f ? width : ArMarkerDisplay.LabelWidthPixels(kind);
        }

        private void ProjectIntoSlot(
            Camera cam,
            Vector3 world,
            float playerX,
            float playerZ,
            ArWaypointKind kind,
            ref bool wasBehind,
            ref ArHorizontalEdge edge)
        {
            ProjectIntoSlot(
                _slots,
                ArMarkerBuffer.SlotOf(kind),
                cam,
                world,
                playerX,
                playerZ,
                kind,
                ref wasBehind,
                ref edge);
        }

        private void ProjectIntoSlot(
            ArMarkerSlot[] slots,
            int index,
            Camera cam,
            Vector3 world,
            float playerX,
            float playerZ,
            ArWaypointKind kind,
            ref bool wasBehind,
            ref ArHorizontalEdge edge)
        {
            var screen = cam.WorldToScreenPoint(world);
            var local = cam.transform.InverseTransformPoint(world);
            var pixelRect = cam.pixelRect;
            var previousPlace = slots[index].Place;
            ArMarkerPlacement.Resolve(
                local.z,
                local.x,
                screen.x + pixelRect.x,
                screen.y + pixelRect.y,
                screen.z,
                Screen.width,
                Screen.height,
                wasBehind,
                edge,
                previousPlace,
                out wasBehind,
                out var place,
                out var guiX,
                out var guiY,
                out edge);

            var dx = world.x - playerX;
            var dz = world.z - playerZ;
            var meters = (int)Mathf.Round(Mathf.Sqrt((dx * dx) + (dz * dz)));
            var sortKey = ArEdgeStackLayout.OutwardSortKey(
                edge,
                ArEdgeHysteresis.BehindBearingRadians(local.x, local.z));
            guiY = ArStickyRowPlacement.ResolveSlotGuiY(
                place,
                guiY,
                HudStackLayout.LastBottomGuiY,
                IconPixels);

            ArMarkerBuffer.Show(
                ref slots[index],
                kind,
                guiX,
                guiY,
                place,
                meters,
                edge,
                sortKey);
        }

        private static bool ArVisible(bool playerPresent) =>
            YmsOnScreenVisibility.ShouldDraw(playerPresent);

        private void OnGUI()
        {
            if (!ArVisible(PlayerManager.PlayerTransform != null))
            {
                return;
            }

            EnsureStyle();
            RefreshCaptionWidths(measureWithStyle: true);
            ApplyCombinedEdgeStack();
            DrawSlot(ArWaypointKind.Station, _officeIcon, _officeGlyph, OfficeColor);
            DrawSlot(ArWaypointKind.Loco, _locoIcon, _locoGlyph, LocoColor);
            var pinTint = RouteClearanceSession.Phase == RouteClearancePhase.Cleared
                ? RouteClearedPinColor
                : PinColor;
            DrawSlot(ArWaypointKind.Pin, _pinIcon, _pinGlyph, pinTint);
            for (var i = 0; i < _radarSlots.Length; i++)
            {
                DrawRadarSlot(i, _radarIcon);
            }

            for (var i = 0; i < _jobCarSlots.Length; i++)
            {
                DrawJobCarSlot(i, _jobCarIcon);
            }
        }

        private void DrawSlot(ArWaypointKind kind, Texture2D? icon, GUIContent glyph, Color tint)
        {
            var slot = _slots[ArMarkerBuffer.SlotOf(kind)];
            if (!ArMarkerBuffer.ShouldDrawSlot(in slot))
            {
                return;
            }

            var capW = CaptionWidthForStack(ArMarkerBuffer.SlotOf(kind), kind);
            DrawMarker(slot.GuiX, slot.GuiY, icon, tint, glyph, capW, LabelHeight);
        }

        private void DrawRadarSlot(int index, Texture2D? icon)
        {
            var slot = _radarSlots[index];
            if (!ArMarkerBuffer.ShouldDrawSlot(in slot))
            {
                return;
            }

            var capW = CaptionWidthForStack(ArMarkerBuffer.Capacity + index, ArWaypointKind.OtherLoco);
            DrawMarker(
                slot.GuiX,
                slot.GuiY,
                icon,
                OtherLocoColor,
                _radarGlyphs[index],
                capW,
                RadarLabelHeight);
        }

        private void DrawJobCarSlot(int index, Texture2D? icon)
        {
            var slot = _jobCarSlots[index];
            if (!ArMarkerBuffer.ShouldDrawSlot(in slot))
            {
                return;
            }

            var stackIndex = ArMarkerBuffer.Capacity + _radarSlots.Length + index;
            var capW = CaptionWidthForStack(stackIndex, ArWaypointKind.JobCar);
            DrawMarker(
                slot.GuiX,
                slot.GuiY,
                icon,
                JobCarColor,
                _jobCarGlyphs[index],
                capW,
                LabelHeight);
        }

        private void DrawMarker(
            float guiX,
            float guiY,
            Texture2D? icon,
            Color tint,
            GUIContent glyph,
            float capW,
            float labelHeight)
        {
            var occ = ArEdgeStackLayout.OccupancyWidthPixels(IconPixels, capW);
            var plate = new Rect(
                guiX - occ * 0.5f,
                guiY - IconPixels - ArMarkerPlate.ExpandY,
                occ,
                ArMarkerPlate.OuterHeightPixels(IconPixels, labelHeight));
            DrawQuad(plate, new Color(0.08f, 0.08f, 0.08f, ArMarkerPlate.FillAlpha));

            var iconRect = new Rect(
                guiX - IconPixels * 0.5f,
                guiY - IconPixels,
                IconPixels,
                IconPixels);
            var prev = GUI.color;
            GUI.color = tint;
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
            }

            GUI.color = Color.white;
            GUI.Label(
                new Rect(guiX - occ * 0.5f, guiY, occ, labelHeight),
                glyph,
                _style);
            GUI.color = prev;
        }

        private static void DrawQuad(Rect rect, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void HideOffice()
        {
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)]);
            _officeBehind = false;
            _officeEdge = ArHorizontalEdge.None;
        }

        private void HideLoco()
        {
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)]);
            _locoBehind = false;
            _locoEdge = ArHorizontalEdge.None;
        }

        private void HidePin()
        {
            ArMarkerBuffer.Hide(ref _slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Pin)]);
            _pinGlyph.text = ArMarkerDisplay.Glyph(ArWaypointKind.Pin);
            _pinBehind = false;
            _pinEdge = ArHorizontalEdge.None;
        }

        private void HideAllRadar()
        {
            for (var i = 0; i < _radarSlots.Length; i++)
            {
                HideRadar(i);
            }
        }

        private void HideRadar(int index)
        {
            ArMarkerBuffer.Hide(ref _radarSlots[index]);
            _radarBehind[index] = false;
            _radarEdge[index] = ArHorizontalEdge.None;
        }

        private void HideAllJobCars()
        {
            for (var i = 0; i < _jobCarSlots.Length; i++)
            {
                HideJobCar(i);
            }
        }

        private void HideJobCar(int index)
        {
            ArMarkerBuffer.Hide(ref _jobCarSlots[index]);
            _jobCarBehind[index] = false;
            _jobCarEdge[index] = ArHorizontalEdge.None;
        }

        private static ArMarkerSlot[] CreateRadarSlots()
        {
            var slots = new ArMarkerSlot[LocoRadarSelection.DefaultMaxResults];
            for (var i = 0; i < slots.Length; i++)
            {
                ArMarkerBuffer.Hide(ref slots[i]);
            }

            return slots;
        }

        private static GUIContent[] CreateRadarGlyphs()
        {
            var glyphs = new GUIContent[LocoRadarSelection.DefaultMaxResults];
            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphs[i] = new GUIContent("");
            }

            return glyphs;
        }

        private static ArMarkerSlot[] CreateJobCarSlots()
        {
            var slots = new ArMarkerSlot[JobCarMarkerDisplay.DefaultMaxMarkers];
            for (var i = 0; i < slots.Length; i++)
            {
                ArMarkerBuffer.Hide(ref slots[i]);
            }

            return slots;
        }

        private static GUIContent[] CreateJobCarGlyphs()
        {
            var glyphs = new GUIContent[JobCarMarkerDisplay.DefaultMaxMarkers];
            for (var i = 0; i < glyphs.Length; i++)
            {
                glyphs[i] = new GUIContent("");
            }

            return glyphs;
        }

        private void EmitIfChanged()
        {
            var snap = ArMarkerBuffer.Snapshot(_slots);
            var line = ArTelemetry.NextLog(_previous, in snap, Time.unscaledTime, ref _lastArLogAt);
            _previous = snap;
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void EmitPlaceSummary(bool force)
        {
            var line = ArPlacementStats.MaybeSummary(Time.unscaledTime, force, ref _placeHist);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private static bool IsExactlyLastLoco(TrainCar? standing, TrainCar loco) =>
            standing != null && ReferenceEquals(standing, loco);

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            var officePng = TryLoadPng(ArWaypointKind.Station, out _officeIcon);
            var locoPng = TryLoadPng(ArWaypointKind.Loco, out _locoIcon);
            var radarPng = TryLoadPng(ArWaypointKind.OtherLoco, out _radarIcon);
            var pinPng = TryLoadPng(ArWaypointKind.Pin, out _pinIcon);
            var jobPng = TryLoadPng(ArWaypointKind.JobCar, out _jobCarIcon);
            if (!officePng)
            {
                _officeIcon = MakeSwatch(Color.white);
            }

            if (!locoPng)
            {
                _locoIcon = MakeSwatch(Color.white);
            }

            if (!radarPng)
            {
                _radarIcon = MakeSwatch(Color.white);
            }

            if (!pinPng)
            {
                _pinIcon = MakeSwatch(Color.white);
            }

            if (!jobPng)
            {
                _jobCarIcon = MakeSwatch(Color.white);
            }

            EmitLog?.Invoke(
                "T2 ar-icons loco=" + (locoPng ? "png" : "quad")
                + " station=" + (officePng ? "png" : "quad")
                + " pin=" + (pinPng ? "png" : "quad")
                + " radar=" + (radarPng ? "png" : "quad")
                + " job=" + (jobPng ? "png" : "quad"));
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };
            _style.normal.textColor = Color.white;
        }

        private static bool TryLoadPng(ArWaypointKind kind, out Texture2D? tex)
        {
            tex = null;
            var file = ArPngIcons.FileName(kind);
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }

            var root = Main.Instance?.Path;
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            var path = Path.Combine(root, ArPngIcons.FolderName, file);
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                var bytes = File.ReadAllBytes(path);
                var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                if (!loaded.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(loaded);
                    return false;
                }

                tex = loaded;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Texture2D MakeSwatch(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void DestroyIcons()
        {
            if (_officeIcon != null)
            {
                Destroy(_officeIcon);
                _officeIcon = null;
            }

            if (_locoIcon != null)
            {
                Destroy(_locoIcon);
                _locoIcon = null;
            }

            if (_radarIcon != null)
            {
                Destroy(_radarIcon);
                _radarIcon = null;
            }

            if (_pinIcon != null)
            {
                Destroy(_pinIcon);
                _pinIcon = null;
            }

            if (_jobCarIcon != null)
            {
                Destroy(_jobCarIcon);
                _jobCarIcon = null;
            }

            _style = null;
        }
    }
}
